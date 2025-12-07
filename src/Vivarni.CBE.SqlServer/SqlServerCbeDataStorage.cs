using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Reflection;
using System.Text;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Vivarni.CBE.DataAnnotations;
using Vivarni.CBE.DataSources;
using Vivarni.CBE.DataStorage;
using Vivarni.CBE.Util;

namespace Vivarni.CBE.SqlServer
{
    internal class SqlServerCbeDataStorage : ICbeDataStorage
    {
        private const int INSERT_BATCH_SIZE = 100_000;

        private readonly string _connectionString;
        private readonly string _schema;
        private readonly string _tablePrefix;
        private readonly ILogger _logger;

        public SqlServerCbeDataStorage(ILogger<SqlServerCbeDataStorage> logger, string connectionString, string schema, string tablePrefix)
        {
            _logger = logger;
            _connectionString = connectionString;
            _schema = schema;
            _tablePrefix = tablePrefix;
        }

        public async Task InitializeAsync(CancellationToken cancellationToken = default)
        {
            var sql = GenerateInitialisationSqlScript();

            using var conn = new SqlConnection(_connectionString);
            using var command = conn.CreateCommand();

            command.CommandText = sql;
            command.CommandType = CommandType.Text;

            await conn.OpenAsync(cancellationToken);
            await command.ExecuteNonQueryAsync(cancellationToken);
            _logger.LogDebug("Executed initialisation SQL script");
        }

        public async Task AddRangeAsync<T>(IEnumerable<T> entities, CancellationToken cancellationToken = default)
            where T : ICbeEntity
        {
            var totalImportCount = 0;
            var tableName = $"[{_schema}].[{_tablePrefix + typeof(T).Name}]";
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            foreach (var batch in entities.Batch(INSERT_BATCH_SIZE))
            {
                var dataTable = batch.ToDataTable();
                if (dataTable.Rows.Count <= 0)
                    continue;

                using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(cancellationToken);

                try
                {
                    using var bulkCopy = new SqlBulkCopy(connection, SqlBulkCopyOptions.Default, transaction);
                    bulkCopy.DestinationTableName = tableName;
                    bulkCopy.BulkCopyTimeout = (int)TimeSpan.FromMinutes(5).TotalSeconds;

                    await bulkCopy.WriteToServerAsync(dataTable, cancellationToken);
                    transaction.Commit();

                    totalImportCount += dataTable.Rows.Count;
                    _logger.LogDebug("Imported {TotalImportCount:N0} records into {TableName}", totalImportCount, bulkCopy.DestinationTableName);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to import records into {TableName}", tableName);
                    transaction.Rollback();
                    throw;
                }
            }
        }

        private string GetSqlType(PropertyInfo prop)
        {
            var maxMaxLength = prop.GetCustomAttribute<MaxLengthAttribute>()?.Length.ToString() ?? "MAX";
            var propertyType = prop.PropertyType;

            // Handle nullable types
            var underlyingType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;
            var isNullable = Nullable.GetUnderlyingType(propertyType) != null || !propertyType.IsValueType;

            var sqlType = underlyingType.Name switch
            {
                nameof(String) => $"nvarchar({maxMaxLength})",
                nameof(DateTime) => "datetime2",
                nameof(Int32) => "int",
                nameof(Int64) => "bigint",
                nameof(Byte) => "tinyint",
                nameof(Boolean) => "bit",
                nameof(Decimal) => "decimal(18,2)",
                nameof(Double) => "float",
                nameof(DateOnly) => "date",
                _ => "NVARCHAR(MAX)" // Default fallback
            };

            var primaryKeySuffix = prop.GetCustomAttribute<PrimaryKeyColumn>() != null
                ? " primary key"
                : "";
            var nullSuffix = isNullable && propertyType.IsValueType && Nullable.GetUnderlyingType(propertyType) != null
                ? $" NULL"
                : $" NOT NULL";

            return $"{sqlType}{nullSuffix}{primaryKeySuffix}";
        }

        protected string GenerateInitialisationSqlScript()
        {
            var types = typeof(ICbeEntity)
                .Assembly
                .GetTypes()
                .Where(t => t.GetInterfaces().Contains(typeof(ICbeEntity)) && t.IsClass);

            var sb = new StringBuilder();

            sb.AppendLine($"IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = '{_schema}')");
            sb.AppendLine($"BEGIN");
            sb.AppendLine($"   EXEC('CREATE SCHEMA [{_schema}]')");
            sb.AppendLine($"END");
            sb.Append("\n\n");

            foreach (var type in types)
            {
                var tableName = _tablePrefix + type.Name;
                var properties = type.GetProperties();
                var columns = new List<string>();

                sb.AppendLine($"IF OBJECT_ID('{_schema}.{tableName}', 'U') IS NULL\nBEGIN");
                sb.AppendLine($"CREATE TABLE [{_schema}].[{tableName}] (");

                foreach (var prop in properties)
                {
                    var columnName = _tablePrefix + prop.Name;
                    var sqlType = GetSqlType(prop);
                    sb.AppendLine($"    [{columnName}] {sqlType},");
                }


                sb.AppendLine(")");
                sb.AppendLine("END\n\n");
            }

            return sb.ToString();
        }

        public async Task ClearAsync<T>(CancellationToken cancellationToken)
            where T : ICbeEntity
        {
            var tableName = $"[{_schema}].[{_tablePrefix + typeof(T).Name}]";
            using var conn = new SqlConnection(_connectionString);
            using var command = conn.CreateCommand();

            command.CommandText = $"TRUNCATE TABLE " + tableName;
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
            var ids = entityIds?.ToList() ?? new List<object>();
            if (ids.Count == 0)
                return 0;

            // 2) Prepare safe identifiers
            var tableName = _tablePrefix + typeof(T).Name;
            var quotedSchema = QuoteIdentifier(_schema);          // [schema]
            var quotedTable = QuoteIdentifier(tableName);        // [prefixTypeName]
            var quotedColumn = QuoteIdentifier(deleteOnProperty.Name);

            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync(cancellationToken);
            await using var command = conn.CreateCommand();

            // 3) Create one parameter per ID
            var paramNames = new string[ids.Count];
            for (int i = 0; i < ids.Count; i++)
            {
                var pName = $"@p{i}";
                paramNames[i] = pName;

                var p = new SqlParameter
                {
                    ParameterName = pName,
                    Value = CoerceId(ids[i], deleteOnProperty.PropertyType)
                };
                command.Parameters.Add(p);
            }

            command.CommandType = CommandType.Text;
            command.CommandText =
                $"DELETE FROM {quotedSchema}.{quotedTable} WHERE {quotedColumn} IN ({string.Join(",", paramNames)})";

            var affected = await command.ExecuteNonQueryAsync(cancellationToken);
            return affected;
        }

        private static string QuoteIdentifier(string identifier)
        {
            if (string.IsNullOrWhiteSpace(identifier))
                throw new ArgumentException("Identifier cannot be null/empty.", nameof(identifier));
            // Wrap in [ ] and escape any embedded ] by doubling it: ]] (SQL Server identifier escaping)
            return "[" + identifier.Replace("]", "]]") + "]";
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
    }
}
