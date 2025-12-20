using System.Data;
using System.Globalization;
using System.IO.Compression;
using System.Reflection;
using System.Text;
using CsvHelper;
using Microsoft.Extensions.Logging;
using Vivarni.CBE.DataSources;
using Vivarni.CBE.DataStorage;
using Vivarni.CBE.Util;

namespace Vivarni.CBE;

public interface ICbeService
{
    Task UpdateCbeDataAsync(CancellationToken cancellationToken = default);
}

internal class CbeService : ICbeService
{
    private readonly ICbeStateRegistry _applicationStateRepository;
    private readonly CbeDataSourceProxy _source;
    private readonly ICbeDataStorage _storage;
    private readonly ILogger _logger;

    public CbeService(
        ILogger<CbeService> logger,
        CbeDataSourceProxy source,
        ICbeStateRegistry applicationStateRepository,
        ICbeDataStorage database)
    {
        _logger = logger;
        _source = source;
        _applicationStateRepository = applicationStateRepository;
        _storage = database;
    }

    public async Task UpdateCbeDataAsync(CancellationToken cancellationToken = default)
    {
        // Make sure the storage is ready to receive data.
        await _storage.InitializeAsync(cancellationToken);

        // Determine which files we're going to use
        var files = await GetExecutionPlan(cancellationToken);

        foreach (var item in files)
        {
            if (item.ExtractType == CbeExtractType.Full)
                await ExecuteFullSync(item, cancellationToken);

            if (item.ExtractType == CbeExtractType.Update)
                await ExecuteUpdateSync(item, cancellationToken);

            _logger.LogInformation("CBE sync: Successfully processed {CbeOpenDataFileName}. Saving state to registry..", item.Filename);
            await _applicationStateRepository.SetCurrentExtractNumber(item.ExtractNumber, cancellationToken);
        }
    }

    private async Task<List<CbeOpenDataFile>> GetExecutionPlan(CancellationToken cancellationToken)
    {
        var onlineFiles = await _source.GetOpenDataFilesAsync(cancellationToken);
        var targetExtractNumber = onlineFiles.Max(s => s.ExtractNumber);
        var currentExtractNumber = await _applicationStateRepository.GetCurrentExtractNumber(cancellationToken);

        if (currentExtractNumber == -1)
        {
            var latestFull = onlineFiles
                .Where(s => s.ExtractType == CbeExtractType.Full)
                .OrderByDescending(s => s.ExtractNumber)
                .First();

            return [latestFull];
        }
        else
        {
            var updates = new List<CbeOpenDataFile>();
            for (int i = currentExtractNumber + 1; i <= targetExtractNumber; i++)
            {
                var partial = onlineFiles.Single(s => s.ExtractType == CbeExtractType.Update && s.ExtractNumber == i);
                updates.Add(partial);
            }

            return updates;
        }
    }

    private async Task ExecuteUpdateSync(CbeOpenDataFile openDataFile, CancellationToken cancellationToken)
    {
        _logger.LogInformation("CBE sync: Processing {CbeOpenDataFileName}", openDataFile.Filename);
        using var stream = await _source.ReadAsync(openDataFile, cancellationToken);
        using var zipArchive = new ZipArchive(stream);

        var types = typeof(ICbeEntity)
            .Assembly
            .GetTypes()
            .Where(t => t.GetInterfaces().Contains(typeof(ICbeEntity)) && t.IsClass);

        foreach (var type in types)
        {
            var csvBaseName = type.GetCustomAttribute<CsvFileMapping>()?.CsvBaseName;
            var deleteEntry = zipArchive.Entries.SingleOrDefault(s => s.Name.Equals($"{csvBaseName}_delete.csv", StringComparison.CurrentCultureIgnoreCase));
            var insertEntry = zipArchive.Entries.SingleOrDefault(s => s.Name.Equals($"{csvBaseName}_insert.csv", StringComparison.CurrentCultureIgnoreCase));

            if (deleteEntry != null)
                await DeleteCsvRecords(deleteEntry, type, cancellationToken);

            if (insertEntry != null)
                await InsertCsvRecords(insertEntry, type, false, cancellationToken);
        }
    }

    private async Task ExecuteFullSync(CbeOpenDataFile openDataFile, CancellationToken cancellationToken)
    {
        _logger.LogInformation("CBE sync: Processing {CbeOpenDataFileName}", openDataFile.Filename);
        using var stream = await _source.ReadAsync(openDataFile, cancellationToken);
        using var zipArchive = new ZipArchive(stream);

        var types = typeof(ICbeEntity)
            .Assembly
            .GetTypes()
            .Where(t => t.GetInterfaces().Contains(typeof(ICbeEntity)) && t.IsClass);

        var missingFiles = types
            .Select(s => (s.GetCustomAttribute<CsvFileMapping>()?.CsvBaseName + ".csv").ToLower())
            .Except(zipArchive.Entries.Select(entry => entry.Name.ToLower()))
            .ToList();

        if (missingFiles.Count != 0)
            throw new FileNotFoundException($"Full ECB sync failed: The following required files are missing in the zip archive: {string.Join(',', missingFiles)}");

        foreach (var type in types)
        {
            var csvFileName = type.GetCustomAttribute<CsvFileMapping>()?.CsvBaseName + ".csv";
            var zipEntry = zipArchive.Entries.Single(s => s.Name.Equals(csvFileName, StringComparison.CurrentCultureIgnoreCase));
            await InsertCsvRecords(zipEntry, type, true, cancellationToken);
        }
    }

    private async Task InsertCsvRecords(ZipArchiveEntry zipEntry, Type type, bool truncateFirst, CancellationToken cancellationToken)
    {
        var method = GetType()
            .GetMethod(nameof(InsertCsvRecordsGeneric), BindingFlags.NonPublic | BindingFlags.Instance)!
            .MakeGenericMethod(type)!;

        await (Task)method.Invoke(this, [zipEntry, truncateFirst])!;
    }

    private async Task InsertCsvRecordsGeneric<T>(ZipArchiveEntry zipEntry, bool truncateFirst, CancellationToken cancellationToken)
        where T : class, ICbeEntity
    {
        using var sr = new StreamReader(zipEntry.Open(), Encoding.UTF8, true);
        using var csv = CreateCsvReader(sr);
        var records = csv.GetRecords<T>();

        if (truncateFirst)
        {
            await _storage.ClearAsync<T>();
            _logger.LogInformation("CBE sync: Cleared {CbeEntity}", typeof(T).Name);
        }

        await _storage.AddRangeAsync(records);
    }

    private async Task DeleteCsvRecords(ZipArchiveEntry zipEntry, Type type, CancellationToken cancellationToken)
    {
        using var sr = new StreamReader(zipEntry.Open(), Encoding.UTF8, true);
        using var csv = CreateCsvReader(sr);

        csv.Read();
        csv.ReadHeader();

        var headers = csv.Context.Reader?.HeaderRecord;
        if (headers?.Length != 1)
            throw new Exception("The file contains more than one column.");

        var mapping = type
            .GetProperties()
            .Where(s => s.Name == headers[0])
            .ToList();

        if (mapping == null || mapping.Count != 1)
            throw new Exception("The delete file contains an unexpected header: " + headers[0]);

        var deleteIdentifierCount = 0;
        var deleteCountActual = 0;
        var deleteOnMatchingProperty = mapping[0];

        foreach (var batch in csv.GetRecords<dynamic>().Batch(1000))
        {
            var method = typeof(ICbeDataStorage)
                .GetMethod(nameof(ICbeDataStorage.RemoveAsync), BindingFlags.Public | BindingFlags.Instance)!
                .MakeGenericMethod(type);

            var values = batch.Select(s => ((IDictionary<string, object>)s)[deleteOnMatchingProperty.Name]);
            deleteCountActual += await (Task<int>)method.Invoke(_storage, [values, deleteOnMatchingProperty, cancellationToken])!;
            deleteIdentifierCount += values.Count();
        }

        _logger.LogInformation(
            "Deleted {CbeDeleteActualCount} records for {CbeDeleteIdentifierCount} identifiers in database for {zipfile}",
            deleteIdentifierCount, deleteCountActual, zipEntry.FullName);
    }

    private static CsvReader CreateCsvReader(StreamReader sr)
    {
        var csvReader = new CsvReader(sr, CultureInfo.InvariantCulture);
        csvReader.Context.TypeConverterCache.AddConverter<DateOnly>(new DateOnlyConverter());
        csvReader.Context.TypeConverterCache.AddConverter<DateOnly?>(new DateOnlyConverter());

        return csvReader;
    }
}
