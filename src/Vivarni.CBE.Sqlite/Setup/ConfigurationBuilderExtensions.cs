using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Vivarni.CBE.Sqlite.DDL;

namespace Vivarni.CBE.Sqlite.Setup;

public static class ConfigurationBuilderExtensions
{
    public static VivarniCbeOptions UseSqlite(this VivarniCbeOptions builder, string connectionString)
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

        builder.DatabaseObjectNameProviderFactory = (s) => new SqliteDatabaseObjectNameProvider();

        return builder;
    }
}
