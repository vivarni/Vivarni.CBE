using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Text;
using Vivarni.CBE.DataAnnotations;
using Vivarni.CBE.DataSources;
using Vivarni.CBE.DataStorage;
using Vivarni.CBE.Postgres.Setup;

namespace Vivarni.CBE.Postgres.DDL;

public class PostgresDataDefinitionLanguageGenerator : IDataDefinitionLanguageGenerator, IDatabaseObjectNameProvider
{
    private readonly string _schema;
    private readonly string _tablePrefix;

    public PostgresDataDefinitionLanguageGenerator(PostgresCbeOptions opts)
    {
        _schema = PostgresDatabaseObjectNameProvider.GetObjectName(opts.Schema);
        _tablePrefix = opts.TablePrefix;
    }

    public string GetTableName<T>() where T : ICbeEntity
        => PostgresDatabaseObjectNameProvider.GetObjectName(typeof(T).Name);

    public string GenerateDDL()
    {
        var types = typeof(ICbeEntity)
            .Assembly
            .GetTypes()
            .Where(t => t.GetInterfaces().Contains(typeof(ICbeEntity)) && t.IsClass);

        var sb = new StringBuilder();

        // Create schema if it doesn't exist
        sb.AppendLine($"CREATE SCHEMA IF NOT EXISTS {_schema};");
        sb.Append("\n\n");

        var indexStatements = new List<string>();

        foreach (var type in types)
        {
            var tableName = PostgresDatabaseObjectNameProvider.GetObjectName(_tablePrefix + type.Name);
            var properties = type.GetProperties();

            sb.AppendLine($"CREATE TABLE IF NOT EXISTS {_schema}.{tableName} (");

            var columnDefinitions = new List<string>();
            foreach (var prop in properties)
            {
                var columnName = PostgresDatabaseObjectNameProvider.GetObjectName(_tablePrefix + prop.Name);
                var sqlType = GetSqlType(prop);
                columnDefinitions.Add($"    {columnName} {sqlType}");

                // Check for IndexColumn attribute and collect index statements
                if (prop.GetCustomAttribute<IndexColumnAttribute>() != null)
                {
                    var indexName = $"IX_{type.Name}_{prop.Name}";
                    var indexStatement = $"CREATE INDEX IF NOT EXISTS {indexName} ON {_schema}.{tableName} ({columnName});";
                    indexStatements.Add(indexStatement);
                }
            }

            sb.AppendLine(string.Join(",\n", columnDefinitions));
            sb.AppendLine(");\n");
        }

        // Add all index statements after table creation
        if (indexStatements.Count > 0)
        {
            sb.AppendLine("-- Create indexes");
            sb.AppendLine(string.Join("\n", indexStatements));
            sb.AppendLine();
        }

        // Create state registry table
        var stateTableName = PostgresDatabaseObjectNameProvider.GetObjectName(_tablePrefix + "StateRegistry");
        sb.AppendLine($"CREATE TABLE IF NOT EXISTS {_schema}.{stateTableName} (");
        sb.AppendLine($"    Variable VARCHAR(100) NOT NULL PRIMARY KEY,");
        sb.AppendLine($"    Value TEXT NULL");
        sb.AppendLine(");\n");

        return sb.ToString();
    }

    private string GetSqlType(PropertyInfo prop)
    {
        var maxLength = prop.GetCustomAttribute<MaxLengthAttribute>()?.Length;
        var propertyType = prop.PropertyType;

        // Handle nullable types
        var underlyingType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;
        var isNullable = new NullabilityInfoContext().Create(prop).WriteState == NullabilityState.Nullable;

        var sqlType = underlyingType.Name switch
        {
            nameof(String) => maxLength.HasValue ? $"VARCHAR({maxLength})" : "TEXT",
            nameof(DateTime) => "TIMESTAMP",
            nameof(Int32) => "INTEGER",
            nameof(Int64) => "BIGINT",
            nameof(Byte) => "SMALLINT",
            nameof(Boolean) => "BOOLEAN",
            nameof(Decimal) => "DECIMAL(18,2)",
            nameof(Double) => "DOUBLE PRECISION",
            nameof(DateOnly) => "DATE",
            nameof(Guid) => "UUID",
            _ => "TEXT" // Default fallback
        };

        var constraints = new List<string>();
        if (prop.GetCustomAttribute<PrimaryKeyColumn>() != null)
            constraints.Add("PRIMARY KEY");

        if (!isNullable)
            constraints.Add("NOT NULL");

        return constraints.Count > 0 ? $"{sqlType} {string.Join(" ", constraints)}" : sqlType;
    }
}
