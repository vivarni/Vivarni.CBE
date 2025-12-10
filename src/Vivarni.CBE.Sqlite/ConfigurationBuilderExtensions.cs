using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Vivarni.CBE.Sqlite;

public static class ConfigurationBuilderExtensions
{
    public static VivarniCbeOptions WithSqliteDatabase(this VivarniCbeOptions builder, string connectionString)
    {
        builder.DataStorageFactory = (s) =>
        {
            var logger = s.GetRequiredService<ILogger<SqliteCbeDataStorage>>();
            return new SqliteCbeDataStorage(logger, connectionString);
        };

        builder.SynchronisationStateRegistryFactory = (s) =>
        {
            var logger = s.GetRequiredService<ILogger<SqliteCbeDataStorage>>();
            return new SqliteCbeDataStorage(logger, connectionString);
        };

        return builder;
    }
}
