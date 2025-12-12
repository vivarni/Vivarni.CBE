using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Vivarni.CBE.Oracle.Setup;

public static class ConfigurationBuilderExtensions
{
    public static VivarniCbeOptions UseOracle(this VivarniCbeOptions builder, string connectionString, OracleCbeOptions? opts = null)
    {
        builder.DataStorageFactory = (s) =>
        {
            var logger = s.GetRequiredService<ILogger<OracleCbeDataStorage>>();
            var storage = new OracleCbeDataStorage(logger, connectionString, opts);
            return storage;
        };

        builder.SynchronisationStateRegistryFactory = (s) =>
        {
            var logger = s.GetRequiredService<ILogger<OracleCbeDataStorage>>();
            var storage = new OracleCbeDataStorage(logger, connectionString, opts);
            return storage;
        };

        return builder;
    }
}
