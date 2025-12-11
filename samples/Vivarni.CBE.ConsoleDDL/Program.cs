using Vivarni.CBE.DataStorage;
using Vivarni.CBE.Postgres;
using Vivarni.CBE.Postgres.DDL;
using Vivarni.CBE.Sqlite.DDL;
using Vivarni.CBE.SqlServer.DDL;

namespace Vivarni.CBE.ConsoleDDL;

internal class Program
{
    public static async Task Main()
    {
        var collection = EnumerateGenerators();
        foreach (var (databaseName, ddl) in collection)
        {
            var sql = ddl.GenerateDDL();
            PrintHeader(databaseName, "SQL Statements to prepare a database for CBE data");
            Console.WriteLine(sql);
        }
    }

    private static IEnumerable<(string DatabaseName, IDataDefinitionLanguageGenerator ddl)> EnumerateGenerators()
    {
        yield return ("Sqlite", new SqliteDataDefinitionLanguageGenerator());
        yield return ("Postgres", new PostgresDataDefinitionLanguageGenerator(new PostgresCbeOptions()));
        yield return ("SqlServer", new SqlServerDataDefinitionLanguageGenerator("dbo", "Vivarni"));
    }

    static void PrintHeader(string title, string subtitle)
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
