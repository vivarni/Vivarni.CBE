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
using Vivarni.CBE.Util;

namespace Vivarni.CBE.Sqlite;

internal class SqliteCbeDataStorage
    : ICbeDataStorage
    , ICbeStateRegistry
{
    private const int INSERT_BATCH_SIZE = 250_000;

    private readonly string _connectionString;
    private readonly ILogger _logger;

    public SqliteCbeDataStorage(ILogger<SqliteCbeDataStorage> logger, string connectionString)
    {
        _logger = logger;
        _connectionString = connectionString
            ?? throw new InvalidOperationException("Missing connection string for CBE database");
    }

    public async Task AddRangeAsync<T>(IEnumerable<T> entities, CancellationToken cancellationToken = default)
        where T : ICbeEntity
    {
        var totalImportCount = 0;
        var entityType = typeof(T);
        var tableName = QuoteIdentifier(entityType.Name);
        var properties = entityType.GetProperties();

        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        // Build INSERT statement with all columns
        var columnNames = properties.Select(p => QuoteIdentifier(p.Name));
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
        var tableName = QuoteIdentifier(typeof(T).Name);
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
        var sql = GenerateInitialisationSqlScript();

        using var conn = new SqliteConnection(_connectionString);
        using var command = conn.CreateCommand();

        command.CommandText = sql;
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
        var tableName = QuoteIdentifier(typeof(T).Name);
        var columnName = QuoteIdentifier(deleteOnProperty.Name);

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

    private string GenerateInitialisationSqlScript()
    {
        var types = typeof(ICbeEntity)
            .Assembly
            .GetTypes()
            .Where(t => t.GetInterfaces().Contains(typeof(ICbeEntity)) && t.IsClass);

        var sb = new StringBuilder();
        var indexStatements = new List<string>();

        foreach (var type in types)
        {
            var tableName = type.Name;
            var properties = type.GetProperties();

            sb.AppendLine($"CREATE TABLE IF NOT EXISTS {QuoteIdentifier(tableName)} (");

            var columnDefinitions = new List<string>();
            foreach (var prop in properties)
            {
                var columnName = prop.Name;
                var sqlType = GetSqliteType(prop);
                columnDefinitions.Add($"    {QuoteIdentifier(columnName)} {sqlType}");

                // Check for IndexColumn attribute and collect index statements
                if (prop.GetCustomAttribute<IndexColumnAttribute>() != null)
                {
                    var indexName = $"IX_{type.Name}_{prop.Name}";
                    var indexStatement = $"CREATE INDEX IF NOT EXISTS {QuoteIdentifier(indexName)} ON {QuoteIdentifier(tableName)} ({QuoteIdentifier(columnName)});";
                    indexStatements.Add(indexStatement);
                }
            }

            sb.AppendLine(string.Join(",\n", columnDefinitions));
            sb.AppendLine(");");
            sb.AppendLine();
        }

        // Add all index statements after table creation
        if (indexStatements.Count > 0)
        {
            sb.AppendLine("-- Create indexes");
            sb.AppendLine(string.Join("\n", indexStatements));
            sb.AppendLine();
        }

        // Create state registry table
        sb.AppendLine("CREATE TABLE IF NOT EXISTS \"StateRegistry\" (");
        sb.AppendLine("    \"Variable\" TEXT PRIMARY KEY NOT NULL,");
        sb.AppendLine("    \"Value\" TEXT");
        sb.AppendLine(");");
        sb.AppendLine();

        return sb.ToString();
    }

    private string GetSqliteType(PropertyInfo prop)
    {
        var maxLength = prop.GetCustomAttribute<MaxLengthAttribute>()?.Length;
        var propertyType = prop.PropertyType;
        var isPrimaryKey = prop.GetCustomAttribute<PrimaryKeyColumn>() != null;

        // Handle nullable types
        var underlyingType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;
        var isNullable = Nullable.GetUnderlyingType(propertyType) != null || !propertyType.IsValueType;

        var sqlType = underlyingType.Name switch
        {
            nameof(String) => maxLength.HasValue ? $"TEXT({maxLength})" : "TEXT",
            nameof(DateTime) => "DATETIME",
            nameof(Int32) => "INTEGER",
            nameof(Int64) => "INTEGER",
            nameof(Byte) => "INTEGER",
            nameof(Boolean) => "INTEGER", // SQLite uses INTEGER for boolean
            nameof(Decimal) => "REAL",
            nameof(Double) => "REAL",
            nameof(DateOnly) => "DATE",
            _ => "TEXT" // Default fallback
        };

        var constraints = new List<string>();

        if (isPrimaryKey)
            constraints.Add("PRIMARY KEY");

        if (!isNullable)
            constraints.Add("NOT NULL");

        return constraints.Count > 0 ? $"{sqlType} {string.Join(" ", constraints)}" : sqlType;
    }

    private static string QuoteIdentifier(string identifier)
    {
        if (string.IsNullOrWhiteSpace(identifier))
            throw new ArgumentException("Identifier cannot be null/empty.", nameof(identifier));

        var safe = identifier.Replace("\"", "\"\"");
        return $"\"{safe}\"";
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
