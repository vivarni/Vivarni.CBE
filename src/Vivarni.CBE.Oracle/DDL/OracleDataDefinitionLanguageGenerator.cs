using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Text;
using Vivarni.CBE.DataAnnotations;
using Vivarni.CBE.DataSources;
using Vivarni.CBE.DataStorage;
using Vivarni.CBE.Oracle.Setup;

namespace Vivarni.CBE.Oracle.DDL;

public class OracleDataDefinitionLanguageGenerator : IDataDefinitionLanguageGenerator
{
    private readonly string _tablePrefix;

    public OracleDataDefinitionLanguageGenerator(OracleCbeOptions opts)
    {
        _tablePrefix = opts.TablePrefix;
    }

    public string GenerateDDL()
    {
        var types = typeof(ICbeEntity)
            .Assembly
            .GetTypes()
            .Where(t => t.GetInterfaces().Contains(typeof(ICbeEntity)) && t.IsClass);

        var sb = new StringBuilder();

        sb.AppendLine($"DECLARE");
        sb.AppendLine($"    table_exists NUMBER := 0;");
        sb.AppendLine($"    index_exists NUMBER := 0;");
        sb.AppendLine($"BEGIN");

        // /////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // Create 'StateRegistry' table manually
        // /////////////////////////////////////////////////////////////////////////////////////////////////////////////

        var stateTableName = OracleDatabaseObjectNameProvider.GetObjectName(_tablePrefix + "StateRegistry");
        sb.AppendLine();
        sb.AppendLine($"    ----------------------------------------------------------------------------------");
        sb.AppendLine($"    -- TABLE :: {stateTableName}");
        sb.AppendLine($"    ----------------------------------------------------------------------------------");
        sb.AppendLine($"    SELECT COUNT(*) INTO table_exists FROM user_tables WHERE table_name = '{stateTableName}';");
        sb.AppendLine();
        sb.AppendLine($"    IF table_exists = 0 THEN");
        sb.AppendLine($"        EXECUTE IMMEDIATE 'CREATE TABLE {stateTableName} (");
        sb.AppendLine($"            Variable VARCHAR2(100) NOT NULL PRIMARY KEY,");
        sb.AppendLine($"            Value CLOB");
        sb.AppendLine($"        )';");
        sb.AppendLine($"    END IF;");
        sb.AppendLine();

        // /////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // Reflection for ICbeEntity tables
        // /////////////////////////////////////////////////////////////////////////////////////////////////////////////

        foreach (var type in types)
        {
            var indexStatements = new List<string>();
            var tableName = OracleDatabaseObjectNameProvider.GetObjectName(_tablePrefix + type.Name);
            var properties = type.GetProperties();

            sb.AppendLine($"    ----------------------------------------------------------------------------------");
            sb.AppendLine($"    -- TABLE :: {tableName}");
            sb.AppendLine($"    ----------------------------------------------------------------------------------");
            sb.AppendLine($"    SELECT COUNT(*) INTO table_exists FROM user_tables WHERE table_name = '{tableName}';");
            sb.AppendLine();
            sb.AppendLine($"    IF table_exists = 0 THEN");
            sb.AppendLine($"        EXECUTE IMMEDIATE 'CREATE TABLE {tableName} (");

            var columnDefinitions = new List<string>();
            foreach (var prop in properties)
            {
                var columnName = OracleDatabaseObjectNameProvider.GetObjectName(prop.Name);
                var sqlType = GetOracleType(prop);
                columnDefinitions.Add($"            {columnName} {sqlType}");

                // Check for IndexColumn attribute and collect index statements
                if (prop.GetCustomAttribute<IndexColumnAttribute>() != null)
                {
                    var indexName = OracleDatabaseObjectNameProvider.GetObjectName($"IX_{type.Name}_{prop.Name}");
                    var indexStatement = GenerateIndexStatement(indexName, tableName, columnName);
                    indexStatements.Add(indexStatement);
                }
            }

            sb.AppendLine(string.Join(",\n", columnDefinitions));
            sb.AppendLine($"        )';");
            sb.AppendLine($"    END IF;");
            sb.AppendLine();

            foreach (var indexStatement in indexStatements)
                sb.AppendLine(indexStatement);
        }

        // /////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // The end
        // /////////////////////////////////////////////////////////////////////////////////////////////////////////////

        sb.AppendLine($"END;");
        sb.AppendLine();

        var result = sb.ToString();
        return result;
    }

    private static string GenerateIndexStatement(string indexName, string tableName, string columnName)
    {
        var sb = new StringBuilder();

        sb.AppendLine($"    SELECT COUNT(*) INTO index_exists FROM user_indexes WHERE index_name = '{indexName}';");
        sb.AppendLine($"    IF index_exists = 0 THEN");
        sb.AppendLine($"        EXECUTE IMMEDIATE 'CREATE INDEX {indexName} ON {tableName} ({columnName})';");
        sb.AppendLine($"    END IF;");

        return sb.ToString();
    }

    private string GetOracleType(PropertyInfo prop)
    {
        var maxLength = prop.GetCustomAttribute<MaxLengthAttribute>()?.Length;
        var propertyType = prop.PropertyType;

        // Handle nullable types
        var underlyingType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;
        var isNullable = new NullabilityInfoContext().Create(prop).WriteState == NullabilityState.Nullable;

        var sqlType = underlyingType.Name switch
        {
            nameof(String) => maxLength.HasValue && maxLength <= 4000 ? $"VARCHAR2({maxLength})" : "CLOB",
            nameof(DateTime) => "TIMESTAMP",
            nameof(Int32) => "NUMBER(10)",
            nameof(Int64) => "NUMBER(19)",
            nameof(Byte) => "NUMBER(3)",
            nameof(Boolean) => "NUMBER(1)",
            nameof(Decimal) => "NUMBER(18,2)",
            nameof(Double) => "BINARY_DOUBLE",
            nameof(DateOnly) => "DATE",
            nameof(Guid) => "RAW(16)",
            _ => "CLOB" // Default fallback
        };

        var constraints = new List<string>();
        if (prop.GetCustomAttribute<PrimaryKeyColumn>() != null)
            constraints.Add("PRIMARY KEY");

        if (!isNullable)
            constraints.Add("NOT NULL");

        return constraints.Count > 0 ? $"{sqlType} {string.Join(" ", constraints)}" : sqlType;
    }
}
