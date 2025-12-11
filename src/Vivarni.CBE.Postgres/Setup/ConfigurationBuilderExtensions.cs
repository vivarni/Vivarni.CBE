using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Vivarni.CBE.Postgres.Setup;

public static class ConfigurationBuilderExtensions
{
    public static VivarniCbeOptions WithPostgres(this VivarniCbeOptions builder, string connectionString, PostgresCbeOptions? opts = null)
    {
        builder.DataStorageFactory = (s) =>
        {
            var logger = s.GetRequiredService<ILogger<PostgresCbeDataStorage>>();
            var storage = new PostgresCbeDataStorage(logger, connectionString, opts);
            return storage;
        };

        builder.SynchronisationStateRegistryFactory = (s) =>
        {
            var logger = s.GetRequiredService<ILogger<PostgresCbeDataStorage>>();
            var storage = new PostgresCbeDataStorage(logger, connectionString, opts);
            return storage;
        };

        return builder;
    }
}
