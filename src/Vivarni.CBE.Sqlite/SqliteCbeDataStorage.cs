using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Reflection;
using System.Text;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Vivarni.CBE.DataAnnotations;
using Vivarni.CBE.DataSources;
using Vivarni.CBE.DataStorage;
using Vivarni.CBE.Sqlite.DDL;
using Vivarni.CBE.Util;

namespace Vivarni.CBE.Sqlite;

internal class SqliteCbeDataStorage
    : ICbeDataStorage
    , ICbeStateRegistry
{
    private const int INSERT_BATCH_SIZE = 250_000;

    private readonly string _connectionString;
    private readonly ILogger _logger;
    private readonly SqliteDataDefinitionLanguageGenerator _ddl;

    public SqliteCbeDataStorage(ILogger<SqliteCbeDataStorage> logger, string connectionString)
    {
        _logger = logger;
        _connectionString = connectionString ?? throw new InvalidOperationException("Missing connection string for CBE database");
        _ddl = new SqliteDataDefinitionLanguageGenerator();
    }

    public async Task AddRangeAsync<T>(IEnumerable<T> entities, CancellationToken cancellationToken = default)
        where T : ICbeEntity
    {
        var totalImportCount = 0;
        var entityType = typeof(T);
        var tableName = SqliteDatabaseObjectNameProvider.QuoteIdentifier(entityType.Name);
        var properties = entityType.GetProperties();

        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        // Build INSERT statement with all columns
        var columnNames = properties.Select(p => SqliteDatabaseObjectNameProvider.QuoteIdentifier(p.Name));
        var parameterNames = properties.Select(p => $"@{p.Name}");
        var insertSql = $"INSERT INTO {tableName} ({string.Join(", ", columnNames)}) VALUES ({string.Join(", ", parameterNames)})";

        foreach (var batch in entities.Batch(INSERT_BATCH_SIZE))
        {
            using var transaction = connection.BeginTransaction();
            using var command = connection.CreateCommand();
            command.CommandText = insertSql;

            var batchCount = 0;
            try
            {
                // Create parameters for all properties
                var parameters = new SqliteParameter[properties.Length];
                for (int i = 0; i < properties.Length; i++)
                {
                    var prop = properties[i];
                    var parameter = command.CreateParameter();
                    parameter.ParameterName = $"@{prop.Name}";
                    parameters[i] = parameter;
                    command.Parameters.Add(parameter);
                }

                // Insert each entity in the batch
                foreach (var entity in batch)
                {
                    for (int i = 0; i < properties.Length; i++)
                    {
                        var value = properties[i].GetValue(entity);
                        parameters[i].Value = value ?? DBNull.Value;
                    }
                    await command.ExecuteNonQueryAsync(cancellationToken);
                    batchCount++;
                }

                await transaction.CommitAsync(cancellationToken);
                totalImportCount += batchCount;
                _logger.LogDebug("Imported {TotalImportCount:N0} records into {TableName}", totalImportCount, tableName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to import records into {TableName}", tableName);
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }
    }

    public async Task ClearAsync<T>(CancellationToken cancellationToken = default)
        where T : ICbeEntity
    {
        var tableName = SqliteDatabaseObjectNameProvider.QuoteIdentifier(typeof(T).Name);
        using var conn = new SqliteConnection(_connectionString);
        using var command = conn.CreateCommand();

        await conn.OpenAsync(cancellationToken);
        command.CommandText = $"DELETE FROM {tableName}";
        command.CommandType = CommandType.Text;
        await command.ExecuteNonQueryAsync(cancellationToken);

        _logger.LogDebug("Cleared {TableName}", tableName);
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        using var conn = new SqliteConnection(_connectionString);
        using var command = conn.CreateCommand();

        command.CommandText = _ddl.GenerateDDL();
        command.CommandType = CommandType.Text;

        await conn.OpenAsync(cancellationToken);
        await command.ExecuteNonQueryAsync(cancellationToken);
        _logger.LogDebug("Executed initialisation SQL script");
    }

    public async Task<int> RemoveAsync<T>(IEnumerable<object> entityIds, PropertyInfo deleteOnProperty, CancellationToken cancellationToken = default)
        where T : ICbeEntity
    {
        using var conn = new SqliteConnection(_connectionString);
        using var command = conn.CreateCommand();

        var ids = entityIds.ToArray();
        var tableName = SqliteDatabaseObjectNameProvider.QuoteIdentifier(typeof(T).Name);
        var columnName = SqliteDatabaseObjectNameProvider.QuoteIdentifier(deleteOnProperty.Name);

        await conn.OpenAsync(cancellationToken);

        var paramNames = new string[ids.Length];
        for (var i = 0; i < ids.Length; i++)
        {
            var pName = $"@p{i}";
            paramNames[i] = pName;
            var p = command.CreateParameter();
            p.ParameterName = pName;
            p.DbType = DbType.Int64;
            p.Value = ids[i];
            command.Parameters.Add(p);
        }

        command.CommandType = CommandType.Text;
        command.CommandText = $"DELETE FROM {tableName} WHERE {columnName} IN ({string.Join(",", paramNames)})";

        return await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<IEnumerable<CbeOpenDataFile>> GetProcessedFiles(CancellationToken cancellationToken)
    {
        const string SYNC_PROCESSED_FILES_VARIABLE = "SyncProcessedFiles";

        using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);
        using var command = conn.CreateCommand();

        command.CommandText = "SELECT \"Value\" FROM \"StateRegistry\" WHERE \"Variable\" = @Variable";
        var parameter = command.CreateParameter();
        parameter.ParameterName = "@Variable";
        parameter.Value = SYNC_PROCESSED_FILES_VARIABLE;
        command.Parameters.Add(parameter);

        var result = await command.ExecuteScalarAsync(cancellationToken) as string;

        if (string.IsNullOrEmpty(result))
        {
            return Enumerable.Empty<CbeOpenDataFile>();
        }

        var list = JsonSerializer.Deserialize<List<string>>(result) ?? [];
        return list.Select(s => new CbeOpenDataFile(s));
    }

    public async Task UpdateProcessedFileList(List<CbeOpenDataFile> processedFiles, CancellationToken cancellationToken)
    {
        const string SYNC_PROCESSED_FILES_VARIABLE = "SyncProcessedFiles";

        using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);

        var data = processedFiles.Select(s => s.Filename);
        var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });

        const string upsertQuery = @"
                INSERT INTO ""StateRegistry""(""Variable"", ""Value"")
                VALUES(@Variable, @Value)
                ON CONFLICT(""Variable"")
                DO UPDATE SET ""Value"" = excluded.""Value""";

        using var command = conn.CreateCommand();
        command.CommandText = upsertQuery;

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
