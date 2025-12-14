using Vivarni.CBE.DataSources;
using Vivarni.CBE.DataStorage;

namespace Vivarni.CBE.Sqlite.DDL;

public class SqliteDatabaseObjectNameProvider : IDatabaseObjectNameProvider
{
    public string GetTableName<T>() where T : ICbeEntity
        => QuoteIdentifier(typeof(T).Name);

    internal static string QuoteIdentifier(string identifier)
    {
        if (string.IsNullOrWhiteSpace(identifier))
            throw new ArgumentException("Identifier cannot be null/empty.", nameof(identifier));

        var safe = identifier.Replace("\"", "\"\"");
        return $"\"{safe}\"";
    }
}
