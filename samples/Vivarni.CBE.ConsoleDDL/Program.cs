using Vivarni.CBE.DataStorage;
using Vivarni.CBE.Oracle.DDL;
using Vivarni.CBE.Oracle.Setup;
using Vivarni.CBE.Postgres.DDL;
using Vivarni.CBE.Postgres.Setup;
using Vivarni.CBE.Sqlite.DDL;
using Vivarni.CBE.SqlServer.DDL;

namespace Vivarni.CBE.ConsoleDDL;

internal class Program
{
    public static async Task Main()
    {
        var collection = EnumerateGenerators();
        var directory = Path.GetFullPath("../../../../../dist/");
        Directory.CreateDirectory(Path.GetDirectoryName(directory)!);

        Console.WriteLine("Generating SQL scripts with DDL statements:");
        foreach (var (databaseName, ddl) in collection)
        {
            var sql = ddl.GenerateDDL();
            var destination = Path.GetFullPath($"{directory}/{databaseName}.sql");

            await File.WriteAllTextAsync(destination, sql);
            Console.WriteLine($"  - \u001b[4m{destination}\u001b[0m");
        }
    }

    private static IEnumerable<(string DatabaseName, IDataDefinitionLanguageGenerator ddl)> EnumerateGenerators()
    {
        yield return ("Sqlite", new SqliteDataDefinitionLanguageGenerator());
        yield return ("Postgres", new PostgresDataDefinitionLanguageGenerator(new PostgresCbeOptions()));
        yield return ("SqlServer", new SqlServerDataDefinitionLanguageGenerator("dbo", "Vivarni"));
        yield return ("Oracle", new OracleDataDefinitionLanguageGenerator(new OracleCbeOptions()));
    }
}
