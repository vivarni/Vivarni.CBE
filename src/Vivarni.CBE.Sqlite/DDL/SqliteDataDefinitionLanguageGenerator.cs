using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Text;
using Vivarni.CBE.DataAnnotations;
using Vivarni.CBE.DataSources;
using Vivarni.CBE.DataStorage;

namespace Vivarni.CBE.Sqlite.DDL;

public class SqliteDataDefinitionLanguageGenerator : IDataDefinitionLanguageGenerator
{
    public string GenerateDDL()
    {
        var types = typeof(ICbeEntity)
            .Assembly
            .GetTypes()
            .Where(t => t.GetInterfaces().Contains(typeof(ICbeEntity)) && t.IsClass);

        var sb = new StringBuilder();
        var indexStatements = new List<string>();

        foreach (var type in types)
        {
            var tableName = SqliteDatabaseObjectNameProvider.GetObjectName(type.Name);
            var idColumnName = SqliteDatabaseObjectNameProvider.GetObjectName("CbeId");
            var properties = type.GetProperties();
            var primaryKeyColumns = type.GetCustomAttribute<CbePrimaryKeyAttribute>()?.PropertyNames.Select(SqliteDatabaseObjectNameProvider.GetObjectName);

            sb.AppendLine($"CREATE TABLE IF NOT EXISTS {tableName} (");



            var columnDefinitions = new List<string>();
            // Add ID column as the first column, auto-incrementing
            columnDefinitions.Add($"    {idColumnName} INTEGER PRIMARY KEY AUTOINCREMENT");
            foreach (var prop in properties)
            {
                var columnName = SqliteDatabaseObjectNameProvider.GetObjectName(prop.Name);
                var sqlType = GetSqliteType(prop);
                columnDefinitions.Add($"    {columnName} {sqlType}");

                // Check for IndexColumn attribute and collect index statements
                if (prop.GetCustomAttribute<CbeIndexAttribute>() != null)
                {
                    var indexName = SqliteDatabaseObjectNameProvider.GetObjectName($"IX_{type.Name}_{prop.Name}");
                    var indexStatement = $"CREATE INDEX IF NOT EXISTS {indexName} ON {tableName} ({columnName});";
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

        var underlyingType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;
        var isNullable = new NullabilityInfoContext().Create(prop).WriteState == NullabilityState.Nullable;

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

        if (!isNullable)
            constraints.Add("NOT NULL");

        return constraints.Count > 0 ? $"{sqlType} {string.Join(" ", constraints)}" : sqlType;
    }
}
