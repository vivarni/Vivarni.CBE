using System.Data;
using System.Reflection;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Vivarni.CBE.DataSources;
using Vivarni.CBE.DataStorage;
using Vivarni.CBE.Sqlite.DDL;

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
        where T : class, ICbeEntity
    {
        var totalImportCount = 0;
        var entityType = typeof(T);
        var tableName = SqliteDatabaseObjectNameProvider.GetObjectName(entityType.Name);
        var properties = entityType.GetProperties();

        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        // Build INSERT statement with all columns
        var columnNames = properties.Select(p => SqliteDatabaseObjectNameProvider.GetObjectName(p.Name));
        var parameterNames = properties.Select(p => $"@{p.Name}");
        var insertSql = $"INSERT INTO {tableName} ({string.Join(", ", columnNames)}) VALUES ({string.Join(", ", parameterNames)})";

        foreach (var batch in entities.Chunk(INSERT_BATCH_SIZE))
        {
            using var transaction = connection.BeginTransaction();
            using var command = connection.CreateCommand();
            command.CommandText = insertSql;

            var batchCount = 0;
            try
            {
                // Create parameters for all properties
                var parameters = new SqliteParameter[properties.Length];
                for (var i = 0; i < properties.Length; i++)
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
                    for (var i = 0; i < properties.Length; i++)
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
        where T : class, ICbeEntity
    {
        var tableName = SqliteDatabaseObjectNameProvider.GetObjectName(typeof(T).Name);
        using var conn = new SqliteConnection(_connectionString);
        using var command = conn.CreateCommand();

        await conn.OpenAsync(cancellationToken);
        command.CommandText = $"DELETE FROM {tableName}";
        command.CommandType = CommandType.Text;
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        using var conn = new SqliteConnection(_connectionString);
        using var command = conn.CreateCommand();

        command.CommandText = _ddl.GenerateDDL();
        command.CommandType = CommandType.Text;

        await conn.OpenAsync(cancellationToken);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<int> RemoveAsync<T>(IEnumerable<object> entityIds, PropertyInfo deleteOnProperty, CancellationToken cancellationToken = default)
        where T : class, ICbeEntity
    {
        using var conn = new SqliteConnection(_connectionString);
        using var command = conn.CreateCommand();

        var ids = entityIds.ToArray();
        var tableName = SqliteDatabaseObjectNameProvider.GetObjectName(typeof(T).Name);
        var columnName = SqliteDatabaseObjectNameProvider.GetObjectName(deleteOnProperty.Name);

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

    private const string SYNC_EXTRACT_NUMBER_VARIABLE = "SyncCurrentExtractNumber";

    public async Task<int> GetCurrentExtractNumber(CancellationToken cancellationToken = default)
    {
        using var conn = new SqliteConnection(_connectionString);
        using var command = conn.CreateCommand();
        command.CommandText = "SELECT Value FROM StateRegistry WHERE Variable = @Variable";
        command.Parameters.AddWithValue("@Variable", SYNC_EXTRACT_NUMBER_VARIABLE);
        await conn.OpenAsync(cancellationToken);
        var result = await command.ExecuteScalarAsync(cancellationToken);
        if (result == null || result == DBNull.Value)
            return -1;
        if (int.TryParse(result.ToString(), out var value))
            return value;
        return -1;
    }

    public async Task SetCurrentExtractNumber(int extractNumber, CancellationToken cancellationToken)
    {
        using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);
        using var command = conn.CreateCommand();
        command.CommandText = @"INSERT INTO StateRegistry (Variable, Value) VALUES (@Variable, @Value)
            ON CONFLICT(Variable) DO UPDATE SET Value = excluded.Value;";
        command.Parameters.AddWithValue("@Variable", SYNC_EXTRACT_NUMBER_VARIABLE);
        command.Parameters.AddWithValue("@Value", extractNumber.ToString());
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
