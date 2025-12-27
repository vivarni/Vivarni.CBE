using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Vivarni.CBE.Postgres.Setup;

public static class ConfigurationBuilderExtensions
{
    public static CbeIntegrationOptions UsePostgres(this CbeIntegrationOptions options, string connectionString, PostgresCbeOptions? opts = null)
    {
        options.DataStorageFactory = (s) =>
        {
            var logger = s.GetRequiredService<ILogger<PostgresCbeDataStorage>>();
            var storage = new PostgresCbeDataStorage(logger, connectionString, opts);
            return storage;
        };

        options.SynchronisationStateRegistryFactory = (s) =>
        {
            var logger = s.GetRequiredService<ILogger<PostgresCbeDataStorage>>();
            var storage = new PostgresCbeDataStorage(logger, connectionString, opts);
            return storage;
        };

        return options;
    }
}
