using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Vivarni.CBE.Oracle.Setup;

public static class ConfigurationBuilderExtensions
{
    public static CbeIntegrationOptions UseOracle(this CbeIntegrationOptions options, string connectionString, OracleCbeOptions? opts = null)
    {
        options.DataStorageFactory = (s) =>
        {
            var logger = s.GetRequiredService<ILogger<OracleCbeDataStorage>>();
            var storage = new OracleCbeDataStorage(logger, connectionString, opts);
            return storage;
        };

        options.SynchronisationStateRegistryFactory = (s) =>
        {
            var logger = s.GetRequiredService<ILogger<OracleCbeDataStorage>>();
            var storage = new OracleCbeDataStorage(logger, connectionString, opts);
            return storage;
        };

        return options;
    }
}
