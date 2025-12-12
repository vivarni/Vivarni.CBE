using Vivarni.CBE.DataStorage;
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
    }

    private static void PrintHeader(string title, string subtitle)
    {
        var width = Math.Max(Console.BufferWidth, 60);
        var top = "╔" + new string('═', width - 2) + "╗";
        var bottom = "╚" + new string('═', width - 2) + "╝";
        var titleLine = "║  " + title.PadRight(width - 4) + "║";
        var subtitleLine = "║  " + subtitle.PadRight(width - 4) + "║";
        Console.WriteLine(top);
        Console.WriteLine(titleLine);
        Console.WriteLine(subtitleLine);
        Console.WriteLine(bottom);
        Console.WriteLine();
    }
}
