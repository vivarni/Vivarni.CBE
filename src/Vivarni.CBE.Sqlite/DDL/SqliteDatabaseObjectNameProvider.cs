namespace Vivarni.CBE.Sqlite.DDL;

internal class SqliteDatabaseObjectNameProvider
{
    public static string QuoteIdentifier(string identifier)
    {
        if (string.IsNullOrWhiteSpace(identifier))
            throw new ArgumentException("Identifier cannot be null/empty.", nameof(identifier));

        var safe = identifier.Replace("\"", "\"\"");
        return $"\"{safe}\"";
    }
}
