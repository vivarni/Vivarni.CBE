using System.Data;
using System.Globalization;
using System.IO.Compression;
using System.Reflection;
using System.Text;
using CsvHelper;
using Microsoft.Extensions.Logging;
using Vivarni.CBE.DataSources;
using Vivarni.CBE.DataStorage;
using Vivarni.CBE.Entities;
using Vivarni.CBE.Util;

namespace Vivarni.CBE;

public interface ICbeService
{
    Task Sync(CancellationToken cancellationToken = default);
}

internal class CbeService : ICbeService
{
    private readonly ICbeStateRegistry _applicationStateRepository;
    private readonly ICbeDataStorage _storage;
    private readonly ICbeDataSource _source;
    private readonly ILogger _logger;

    public CbeService(
        ILogger<CbeService> logger,
        ICbeDataSource openDataProvider,
        ICbeStateRegistry applicationStateRepository,
        ICbeDataStorage database)
    {
        _logger = logger;
        _source = openDataProvider;
        _applicationStateRepository = applicationStateRepository;
        _storage = database;
    }

    public async Task Sync(CancellationToken cancellationToken = default)
    {
        // Make sure the storage is ready to receive data.
        await _storage.InitializeAsync(cancellationToken);

        // Fetch some data in order to determine the current synchronisation state
        var processedFiles = (await _applicationStateRepository.GetProcessedFiles(cancellationToken)).ToList();
        var onlineFiles = await _source.GetOpenDataFilesAsync(cancellationToken);

        var highestProcessedNumber = processedFiles.Select(s => s.ExtractNumber).Union([-1]).Max();
        var highestOnlineNumber = onlineFiles.Max(s => s.ExtractNumber);

        // Sort online files by number (ascending)
        var sortedOnlineFiles = onlineFiles.OrderBy(f => f.ExtractNumber).ToList();

        // Execute a FULL import if we've never done it before
        if (processedFiles.Count == 0 || !processedFiles.Any(s => s.ExtractType == CbeExtractType.Full))
        {
            _logger.LogInformation("Executing full sync with latest FULL file.");
            var full = sortedOnlineFiles
                .Where(f => f.ExtractType == CbeExtractType.Full)
                .OrderByDescending(f => f.ExtractNumber)
                .First();

            await ExecuteFullSync(full, cancellationToken);
            await _applicationStateRepository.UpdateProcessedFileList([full], cancellationToken);
            await Sync(cancellationToken); // We might need to process additional update files.

            return;
        }

        // Don't continue if we're already up-to-date
        if (highestProcessedNumber >= highestOnlineNumber)
        {
            _logger.LogInformation("Database is up-to-date. Highest processed: {HighestProcessed}, Highest online: {HighestOnline}", highestProcessedNumber, highestOnlineNumber);
            return;
        }

        // Check for inconsistencies in processed files that would require a full sync
        var hasInconsistency = DetectProcessedFilesInconsistency(processedFiles);
        if (hasInconsistency)
        {
            _logger.LogWarning("Inconsistency detected in processed files. Performing FULL sync to ensure data integrity.");
            var full = sortedOnlineFiles
                .Where(f => f.ExtractType == CbeExtractType.Full)
                .OrderByDescending(f => f.ExtractNumber)
                .First();

            await ExecuteFullSync(full, cancellationToken);
            await _applicationStateRepository.UpdateProcessedFileList([full], cancellationToken);
            await Sync(cancellationToken); // We might need to process additional update files.

            return;
        }

        // Evaluate the UPDATE files and see if there are any missing numbers.
        var missingNumbers = Enumerable.Range(highestProcessedNumber + 1, highestOnlineNumber - highestProcessedNumber)
            .Where(number => !sortedOnlineFiles.Any(f => f.ExtractNumber == number && f.ExtractType == CbeExtractType.Update))
            .ToHashSet();

        // Scenario 2b: Missing numbers, we need a FULL import
        if (missingNumbers.Count > 0)
        {
            _logger.LogInformation("Missing UPDATE file numbers detected: {MissingNumbers}. Falling back to FULL sync.", string.Join(", ", missingNumbers));
            var full = sortedOnlineFiles
                .Where(f => f.ExtractType == CbeExtractType.Full)
                .OrderByDescending(f => f.ExtractNumber)
                .First();

            await ExecuteFullSync(full, cancellationToken);
            await _applicationStateRepository.UpdateProcessedFileList([full], cancellationToken);
            await Sync(cancellationToken); // We might need to process additional update files.

            return;
        }

        // Scenario 2a: No missing numbers, we can update by applying one or more UPDATE files
        else
        {
            _logger.LogInformation("Performing incremental sync using UPDATE files from {StartNumber} to {EndNumber}",
                highestProcessedNumber + 1, highestOnlineNumber);

            var updateFiles = sortedOnlineFiles
                .Where(f => f.ExtractType == CbeExtractType.Update && f.ExtractNumber > highestProcessedNumber)
                .OrderBy(f => f.ExtractNumber)
                .ToList();

            foreach (var updateFile in updateFiles)
            {
                await ExecuteUpdateSync(updateFile, cancellationToken);

                // Update the registry immediately
                processedFiles.Add(updateFile);
                await _applicationStateRepository.UpdateProcessedFileList(processedFiles, cancellationToken);
            }

            return;
        }
    }

    private async Task ExecuteUpdateSync(CbeOpenDataFile item, CancellationToken cancellationToken)
    {
        using var stream = await _source.ReadAsync(item, cancellationToken);
        using var zipArchive = new ZipArchive(stream);

        var codeEntry = typeof(CbeCode).GetCustomAttribute<CsvFileMapping>()?.CsvBaseName
            ?? throw new Exception("Missing CbeCode CSV filename!");
        var zipEntry = zipArchive.Entries.SingleOrDefault(s => s.Name.Equals($"{codeEntry}.csv", StringComparison.CurrentCultureIgnoreCase))
            ?? throw new Exception("Missing CbeCode entry in zipfile!");

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
                await InsertCsvRecords(insertEntry, false, type);
        }

        _logger.LogInformation("CBE sync {CbeUpdateSyncFilename} complete.", item);
    }

    private async Task ExecuteFullSync(CbeOpenDataFile full, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Full ECB sync: Selected {CbeFullSyncFilename} for data refresh", full.Filename);
        using var stream = await _source.ReadAsync(full, cancellationToken);
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
            await InsertCsvRecords(zipEntry, true, type);
        }

        _logger.LogInformation("Full ECB sync complete. Saving application state with {CbeFullSyncFilename}.", full.Filename);
        await _applicationStateRepository.UpdateProcessedFileList([full], cancellationToken);
    }

    private async Task InsertCsvRecords(ZipArchiveEntry zipEntry, bool truncateFirst, Type type)
    {
        var method = GetType()
            .GetMethod(nameof(InsertCsvRecordsGeneric), BindingFlags.NonPublic | BindingFlags.Instance)!
            .MakeGenericMethod(type)!;

        await (Task)method.Invoke(this, [zipEntry, truncateFirst])!;
    }

    private async Task InsertCsvRecordsGeneric<T>(ZipArchiveEntry zipEntry, bool truncateFirst)
        where T : class, ICbeEntity
    {
        using var sr = new StreamReader(zipEntry.Open(), Encoding.UTF8, true);
        using var csv = CreateCsvReader(sr);
        var records = csv.GetRecords<T>();

        if (truncateFirst)
            await _storage.ClearAsync<T>();

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

    private bool DetectProcessedFilesInconsistency(List<CbeOpenDataFile> processedFiles)
    {
        var latestFull = processedFiles.Where(f => f.ExtractType == CbeExtractType.Full).MaxBy(f => f.ExtractNumber);
        if (latestFull == null) return false;

        var expected = latestFull.ExtractNumber + 1;
        foreach (var update in processedFiles.Where(f => f.ExtractType == CbeExtractType.Update && f.ExtractNumber > latestFull.ExtractNumber).OrderBy(f => f.ExtractNumber))
        {
            if (update.ExtractNumber != expected++)
            {
                _logger.LogWarning("Gap detected: missing files {Start}-{End}", expected - 1, update.ExtractNumber - 1);
                return true;
            }
        }
        return false;
    }

    private static CsvReader CreateCsvReader(StreamReader sr)
    {
        var csvReader = new CsvReader(sr, CultureInfo.InvariantCulture);
        csvReader.Context.TypeConverterCache.AddConverter<DateOnly>(new DateOnlyConverter());
        csvReader.Context.TypeConverterCache.AddConverter<DateOnly?>(new DateOnlyConverter());

        return csvReader;
    }
}
