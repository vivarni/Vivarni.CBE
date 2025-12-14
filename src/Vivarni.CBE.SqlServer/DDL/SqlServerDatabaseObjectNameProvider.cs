using Vivarni.CBE.DataSources;
using Vivarni.CBE.DataStorage;

namespace Vivarni.CBE.SqlServer.DDL;

internal class SqlServerDatabaseObjectNameProvider : IDatabaseObjectNameProvider
{
    public string GetTableName<T>() where T : ICbeEntity
        => GetObjectName(typeof(T).Name);

    internal static string GetObjectName(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            throw new ArgumentException("Input cannot be null or empty.", nameof(input));

        return input;
    }
}
