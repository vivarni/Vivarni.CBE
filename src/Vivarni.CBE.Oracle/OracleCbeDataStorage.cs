using System.Data;
using System.Reflection;
using Microsoft.Extensions.Logging;
using Oracle.ManagedDataAccess.Client;
using Vivarni.CBE.DataSources;
using Vivarni.CBE.DataStorage;
using Vivarni.CBE.Oracle.DDL;
using Vivarni.CBE.Oracle.Setup;

namespace Vivarni.CBE.Oracle;

internal class OracleCbeDataStorage
    : ICbeDataStorage
    , ICbeStateRegistry
{
    private readonly string _connectionString;
    private readonly string _tablePrefix;
    private readonly int _batchSize;
    private readonly ILogger _logger;
    private readonly OracleDataDefinitionLanguageGenerator _ddl;

    public OracleCbeDataStorage(ILogger<OracleCbeDataStorage> logger, string connectionString, OracleCbeOptions? opts = null)
    {
        opts ??= new();

        _logger = logger;
        _connectionString = connectionString;
        _tablePrefix = opts.TablePrefix;
        _batchSize = opts.BulkInsertBatchSize;
        _ddl = new(opts);
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        using var conn = new OracleConnection(_connectionString);
        using var command = conn.CreateCommand();

        command.CommandText = _ddl.GenerateDDL();
        command.CommandType = CommandType.Text;

        await conn.OpenAsync(cancellationToken);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task AddRangeAsync<T>(IEnumerable<T> entities, CancellationToken cancellationToken = default)
        where T : class, ICbeEntity
    {
        var totalImportCount = 0;
        var entityType = typeof(T);
        var tableName = OracleDatabaseObjectNameProvider.GetObjectName(_tablePrefix + entityType.Name);
        var properties = entityType.GetProperties();

        using var connection = new OracleConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        // Build INSERT statement with all columns
        var columnNames = properties.Select(p => OracleDatabaseObjectNameProvider.GetObjectName(p.Name));
        var parameterNames = properties.Select(p => $":{p.Name}");
        var insertSql = $"INSERT INTO {tableName} ({string.Join(", ", columnNames)}) VALUES ({string.Join(", ", parameterNames)})";

        foreach (var batch in entities.Chunk(_batchSize))
        {
            using var transaction = connection.BeginTransaction();

            try
            {
                using var command = connection.CreateCommand();
                command.Transaction = transaction;
                command.CommandText = insertSql;

                // Add parameters based on properties
                foreach (var prop in properties)
                {
                    var param = command.CreateParameter();
                    param.ParameterName = prop.Name;
                    param.OracleDbType = GetOracleDbType(prop.PropertyType);
                    command.Parameters.Add(param);
                }

                foreach (var entity in batch)
                {
                    // Set parameter values for this entity
                    foreach (var prop in properties)
                    {
                        var param = command.Parameters[prop.Name];
                        var value = prop.GetValue(entity);
                        param.Value = value ?? DBNull.Value;
                    }

                    await command.ExecuteNonQueryAsync(cancellationToken);
                    totalImportCount++;
                }

                await transaction.CommitAsync(cancellationToken);
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
        var tableName = OracleDatabaseObjectNameProvider.GetObjectName(_tablePrefix + typeof(T).Name);
        using var conn = new OracleConnection(_connectionString);
        using var command = conn.CreateCommand();

        command.CommandText = $"DELETE FROM {tableName}";
        command.CommandType = CommandType.Text;

        await conn.OpenAsync(cancellationToken);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<int> RemoveAsync<T>(IEnumerable<object> entityIds, PropertyInfo deleteOnProperty, CancellationToken cancellationToken = default)
        where T : class, ICbeEntity
    {
        var ids = entityIds?.ToList() ?? [];
        if (ids.Count == 0)
            return 0;

        var tableName = OracleDatabaseObjectNameProvider.GetObjectName(_tablePrefix + typeof(T).Name);
        var columnName = OracleDatabaseObjectNameProvider.GetObjectName(deleteOnProperty.Name);

        using var conn = new OracleConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);
        using var command = conn.CreateCommand();

        // Oracle uses named parameters with colons
        var paramNames = new string[ids.Count];
        for (var i = 0; i < ids.Count; i++)
        {
            var pName = $"p{i}";
            paramNames[i] = $":{pName}";

            var param = command.CreateParameter();
            param.ParameterName = pName;
            param.Value = CoerceId(ids[i], deleteOnProperty.PropertyType);
            command.Parameters.Add(param);
        }

        command.CommandType = CommandType.Text;
        command.CommandText = $"DELETE FROM {tableName} WHERE {columnName} IN ({string.Join(",", paramNames)})";

        var affected = await command.ExecuteNonQueryAsync(cancellationToken);
        return affected;
    }

    private static OracleDbType GetOracleDbType(Type propertyType)
    {
        var underlyingType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;

        return underlyingType.Name switch
        {
            nameof(String) => OracleDbType.Varchar2,
            nameof(DateTime) => OracleDbType.TimeStamp,
            nameof(Int32) => OracleDbType.Int32,
            nameof(Int64) => OracleDbType.Int64,
            nameof(Byte) => OracleDbType.Byte,
            nameof(Boolean) => OracleDbType.Decimal, // Oracle doesn't have native boolean; NUMBER(1) is best mapped to Decimal
            nameof(Decimal) => OracleDbType.Decimal,
            nameof(Double) => OracleDbType.BinaryDouble,
            nameof(DateOnly) => OracleDbType.Date,
            nameof(Guid) => OracleDbType.Raw,
            _ => OracleDbType.Clob // Default fallback
        };
    }

    private static object CoerceId(object value, Type targetType)
    {
        if (value == null) return DBNull.Value;
        var nonNullType = Nullable.GetUnderlyingType(targetType) ?? targetType;

        if (nonNullType.IsInstanceOfType(value))
            return value;

        if (nonNullType == typeof(Guid))
            return value is Guid g ? g : Guid.Parse(value.ToString()!);

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
            return value.ToString()!;

        return value;
    }

    private const string SYNC_EXTRACT_NUMBER_VARIABLE = "SyncCurrentExtractNumber";

    public async Task<int> GetCurrentExtractNumber(CancellationToken cancellationToken = default)
    {
        using var conn = new OracleConnection(_connectionString);
        using var command = conn.CreateCommand();
        var tableName = OracleDatabaseObjectNameProvider.GetObjectName(_tablePrefix + "StateRegistry");
        command.CommandText = $"SELECT Value FROM {tableName} WHERE Variable = :Variable";
        command.Parameters.Add(new OracleParameter("Variable", SYNC_EXTRACT_NUMBER_VARIABLE));
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
        using var conn = new OracleConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);
        var tableName = OracleDatabaseObjectNameProvider.GetObjectName(_tablePrefix + "StateRegistry");
        using var command = conn.CreateCommand();
        command.CommandText = $@"
            MERGE INTO {tableName} t
            USING (SELECT :Variable AS Variable, :Value AS Value FROM dual) s
            ON (t.Variable = s.Variable)
            WHEN MATCHED THEN UPDATE SET t.Value = s.Value
            WHEN NOT MATCHED THEN INSERT (Variable, Value) VALUES (s.Variable, s.Value)";
        command.Parameters.Add(new OracleParameter("Variable", SYNC_EXTRACT_NUMBER_VARIABLE));
        command.Parameters.Add(new OracleParameter("Value", extractNumber.ToString()));
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
