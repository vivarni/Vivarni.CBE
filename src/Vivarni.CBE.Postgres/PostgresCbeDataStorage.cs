using System.Data;
using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Npgsql;
using Vivarni.CBE.DataSources;
using Vivarni.CBE.DataStorage;
using Vivarni.CBE.Postgres.DDL;
using Vivarni.CBE.Util;

namespace Vivarni.CBE.Postgres;

internal class PostgresCbeDataStorage
    : ICbeDataStorage
    , ICbeStateRegistry
{
    private const string SYNC_PROCESSED_FILES_VARIABLE = "SyncProcessedFiles";
    private static readonly JsonSerializerOptions s_jsonSerializerOptions = new() { WriteIndented = true };

    private readonly int _batchSize;
    private readonly string _connectionString;
    private readonly string _schema;
    private readonly string _tablePrefix;
    private readonly ILogger _logger;
    private readonly PostgresDataDefinitionLanguageGenerator _ddl;

    public PostgresCbeDataStorage(ILogger<PostgresCbeDataStorage> logger, string connectionString, PostgresCbeOptions? opts = null)
    {
        opts ??= new();

        _logger = logger;
        _connectionString = connectionString;
        _schema = PostgresDatabaseObjectNameProvider.GetObjectName(opts.Schema);
        _tablePrefix = opts.TablePrefix;
        _batchSize = opts.BinaryImporterBatchSize;
        _ddl = new(opts);
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        using var conn = new NpgsqlConnection(_connectionString);
        using var command = conn.CreateCommand();

        command.CommandText = _ddl.GenerateDDL();
        command.CommandType = CommandType.Text;

        await conn.OpenAsync(cancellationToken);
        await command.ExecuteNonQueryAsync(cancellationToken);
        _logger.LogDebug("Executed initialisation SQL script");
    }

    public async Task AddRangeAsync<T>(IEnumerable<T> entities, CancellationToken cancellationToken = default)
        where T : ICbeEntity
    {
        var totalImportCount = 0;
        var tableName = PostgresDatabaseObjectNameProvider.GetObjectName(_tablePrefix + typeof(T).Name);

        // Pre-compile property accessors for better performance
        var properties = typeof(T).GetProperties();
        var columnNames = properties.Select(p => PostgresDatabaseObjectNameProvider.GetObjectName(p.Name)).ToList();
        var copyCommand = $"COPY {_schema}.{tableName} ({string.Join(", ", columnNames)}) FROM STDIN (FORMAT BINARY)";

        // Use a single connection for all batches to avoid connection overhead
        using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        // Process in streaming fashion to avoid loading all data into memory
        foreach (var batch in entities.Batch(_batchSize))
        {
            var batchList = batch.ToList();
            if (batchList.Count <= 0)
                continue;

            using var transaction = await connection.BeginTransactionAsync(cancellationToken);
            NpgsqlBinaryImporter? writer = null;

            try
            {
                writer = await connection.BeginBinaryImportAsync(copyCommand, cancellationToken);

                // Use synchronous operations for better performance in tight loops
                foreach (var entity in batchList)
                {
                    writer.StartRow();
                    foreach (var prop in properties)
                    {
                        var value = prop.GetValue(entity);
                        writer.Write(value ?? DBNull.Value);
                    }
                }

                // Complete and dispose the writer to exit COPY mode before committing
                await writer.CompleteAsync(cancellationToken);
                await writer.DisposeAsync();
                writer = null; // Mark as disposed to prevent double disposal in catch block

                await transaction.CommitAsync(cancellationToken);

                totalImportCount += batchList.Count;
                _logger.LogDebug("Imported {TotalImportCount:N0} records into {TableName}", totalImportCount, tableName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to import records into {TableName}", tableName);

                // If the writer is still active, we need to cancel it to exit COPY mode
                if (writer != null)
                {
                    try
                    {
                        // Cancel the COPY operation to exit COPY mode
                        await writer.DisposeAsync();
                    }
                    catch (Exception cancelEx)
                    {
                        _logger.LogWarning(cancelEx, "Failed to dispose COPY operation");
                    }
                }

                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }
    }

    public async Task ClearAsync<T>(CancellationToken cancellationToken)
        where T : ICbeEntity
    {
        var tableName = PostgresDatabaseObjectNameProvider.GetObjectName(_tablePrefix + typeof(T).Name);

        using var conn = new NpgsqlConnection(_connectionString);
        using var command = conn.CreateCommand();

        command.CommandText = $"TRUNCATE TABLE {_schema}.{tableName}";
        command.CommandType = CommandType.Text;

        await conn.OpenAsync(cancellationToken);
        await command.ExecuteNonQueryAsync(cancellationToken);

        _logger.LogDebug("Truncated {TableName}", tableName);
    }

    public async Task<int> RemoveAsync<T>(
        IEnumerable<object> entityIds,
        PropertyInfo deleteOnProperty,
        CancellationToken cancellationToken = default)
        where T : ICbeEntity
    {
        // 1) Materialize IDs and short-circuit if empty
        var ids = entityIds?.ToList() ?? [];
        if (ids.Count == 0)
            return 0;

        // 2) Prepare safe identifiers
        var tableName = PostgresDatabaseObjectNameProvider.GetObjectName(_tablePrefix + typeof(T).Name);
        var quotedColumn = PostgresDatabaseObjectNameProvider.GetObjectName(deleteOnProperty.Name);

        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);
        await using var command = conn.CreateCommand();

        // 3) Create one parameter per ID
        var paramNames = new string[ids.Count];
        for (int i = 0; i < ids.Count; i++)
        {
            var pName = $"@p{i}";
            paramNames[i] = pName;

            var p = new NpgsqlParameter
            {
                ParameterName = pName,
                Value = CoerceId(ids[i], deleteOnProperty.PropertyType)
            };
            command.Parameters.Add(p);
        }

        command.CommandType = CommandType.Text;
        command.CommandText =
            $"DELETE FROM {_schema}.{tableName} WHERE {quotedColumn} IN ({string.Join(",", paramNames)})";

        var affected = await command.ExecuteNonQueryAsync(cancellationToken);
        _logger.LogDebug("Deleted {AffectedRows} records from {TableName}", affected, $"{_schema}.{tableName}");
        return affected;
    }

    private static object CoerceId(object value, Type targetType)
    {
        if (value == null) return DBNull.Value;
        var nonNullType = Nullable.GetUnderlyingType(targetType) ?? targetType;

        // If already assignable, return as-is
        if (nonNullType.IsInstanceOfType(value))
            return value;

        // Handle common conversions (string -> Guid/int/long, etc.)
        if (nonNullType == typeof(Guid))
            return value is Guid g ? g : Guid.Parse(value.ToString());

        if (nonNullType == typeof(int))
            return value is int i ? i : Convert.ToInt32(value);

        if (nonNullType == typeof(long))
            return value is long l ? l : Convert.ToInt64(value);

        if (nonNullType == typeof(short))
            return value is short s ? s : Convert.ToInt16(value);

        if (nonNullType == typeof(byte))
            return value is byte b ? b : Convert.ToByte(value);

        if (nonNullType == typeof(decimal))
            return value is decimal d ? d : Convert.ToDecimal(value);

        if (nonNullType == typeof(bool))
            return value is bool bb ? bb : Convert.ToBoolean(value);

        if (nonNullType == typeof(DateTime))
            return value is DateTime dt ? dt : Convert.ToDateTime(value);

        if (nonNullType == typeof(string))
            return value.ToString();

        // Last resort
        return value;
    }

    public async Task<IEnumerable<CbeOpenDataFile>> GetProcessedFiles(CancellationToken cancellationToken)
    {
        var tableName = PostgresDatabaseObjectNameProvider.GetObjectName($"{_tablePrefix}StateRegistry");

        using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);
        using var command = conn.CreateCommand();

        command.CommandText = $"SELECT value FROM {_schema}.{tableName} WHERE variable = @Variable";
        var parameter = command.CreateParameter();
        parameter.ParameterName = "@Variable";
        parameter.Value = SYNC_PROCESSED_FILES_VARIABLE;
        command.Parameters.Add(parameter);

        var result = await command.ExecuteScalarAsync(cancellationToken) as string;

        if (string.IsNullOrEmpty(result))
            return [];

        var list = JsonSerializer.Deserialize<List<string>>(result) ?? [];
        return list.Select(s => new CbeOpenDataFile(s));
    }

    public async Task UpdateProcessedFileList(List<CbeOpenDataFile> processedFiles, CancellationToken cancellationToken)
    {
        var tableName = PostgresDatabaseObjectNameProvider.GetObjectName($"{_tablePrefix}StateRegistry");

        using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);

        var data = processedFiles.Select(s => s.Filename);
        var json = JsonSerializer.Serialize(data, s_jsonSerializerOptions);

        using var command = conn.CreateCommand();
        command.CommandText = $@"
                INSERT INTO {_schema}.{tableName} (Variable, Value) VALUES (@Variable, @Value)
                ON CONFLICT (Variable)
                DO UPDATE SET Value = EXCLUDED.Value";

        var variableParam = command.CreateParameter();
        variableParam.ParameterName = "@Variable";
        variableParam.Value = SYNC_PROCESSED_FILES_VARIABLE;
        command.Parameters.Add(variableParam);

        var valueParam = command.CreateParameter();
        valueParam.ParameterName = "@Value";
        valueParam.Value = json;
        command.Parameters.Add(valueParam);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
