using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Text;
using Vivarni.CBE.DataAnnotations;
using Vivarni.CBE.DataSources;
using Vivarni.CBE.DataStorage;

namespace Vivarni.CBE.SqlServer.DDL;

public class SqlServerDataDefinitionLanguageGenerator : IDataDefinitionLanguageGenerator
{
    private readonly string _schema;
    private readonly string _tablePrefix;

    public SqlServerDataDefinitionLanguageGenerator(string schema, string tablePrefix)
    {
        _schema = SqlServerDatabaseObjectNameProvider.GetObjectName(schema);
        _tablePrefix = tablePrefix;
    }

    public string GenerateDDL()
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

        var indexStatements = new List<string>();

        foreach (var type in types)
        {
            var tableName = SqlServerDatabaseObjectNameProvider.GetObjectName(_tablePrefix + type.Name);
            var properties = type.GetProperties();
            var primaryKeyColumns = type.GetCustomAttribute<CbePrimaryKeyAttribute>()?.PropertyNames;

            sb.AppendLine($"IF OBJECT_ID('{_schema}.{tableName}', 'U') IS NULL\nBEGIN");
            sb.AppendLine($"CREATE TABLE [{_schema}].[{tableName}] (");

            foreach (var prop in properties)
            {
                var columnName = prop.Name;
                var sqlType = GetSqlType(prop);
                sb.AppendLine($"    [{columnName}] {sqlType},");

                // Check for IndexColumn attribute and collect index statements
                if (prop.GetCustomAttribute<CbeIndexAttribute>() != null)
                {
                    var indexName = $"IX_{type.Name}_{prop.Name}";
                    var indexStatement = $"IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = '{indexName}' AND object_id = OBJECT_ID('[{_schema}].[{tableName}]'))\n" +
                                        $"    CREATE INDEX [{indexName}] ON [{_schema}].[{tableName}] ([{columnName}]);";
                    indexStatements.Add(indexStatement);
                }
            }

            if (primaryKeyColumns != null)
                sb.AppendLine("    PRIMARY KEY (" + string.Join(',', primaryKeyColumns) + ")");
            sb.AppendLine(")");
            sb.AppendLine("END\n\n");
        }

        // Add all index statements after table creation
        if (indexStatements.Count > 0)
        {
            sb.AppendLine("-- Create indexes");
            sb.AppendLine(string.Join("\n\n", indexStatements));
            sb.AppendLine();
        }

        // Create state registry table
        var stateTableName = _tablePrefix + "StateRegistry";
        sb.AppendLine($"IF OBJECT_ID('{_schema}.{stateTableName}', 'U') IS NULL");
        sb.AppendLine("BEGIN");
        sb.AppendLine($"CREATE TABLE [{_schema}].[{stateTableName}] (");
        sb.AppendLine($"    [Variable] NVARCHAR(100) NOT NULL PRIMARY KEY,");
        sb.AppendLine($"    [Value] NVARCHAR(MAX) NULL");
        sb.AppendLine(")");
        sb.AppendLine("END\n\n");

        return sb.ToString();
    }

    private string GetSqlType(PropertyInfo prop)
    {
        var maxMaxLength = prop.GetCustomAttribute<MaxLengthAttribute>()?.Length.ToString() ?? "MAX";
        var propertyType = prop.PropertyType;

        var underlyingType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;
        var isNullable = new NullabilityInfoContext().Create(prop).WriteState == NullabilityState.Nullable;

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

        var constraints = new List<string>();

        if (!isNullable)
            constraints.Add("not null");

        return constraints.Count > 0 ? $"{sqlType} {string.Join(" ", constraints)}" : sqlType;
    }
}
