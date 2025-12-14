using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Vivarni.CBE.Sqlite.DDL;
using Vivarni.CBE.DataStorage;

namespace Vivarni.CBE.Sqlite.Setup;

public static class ConfigurationBuilderExtensions
{
    /// <summary>
    /// Configures the CBE integration to use Sqlite as the <see cref="ICbeDataStorage"/>.
    /// </summary>
    /// <param name="options"></param>
    /// <param name="connectionString">Connection string to the Sqlite database.</param>
    /// <returns>The same <see cref="CbeIntegrationOptions"/> instance, so that configuration can be chained.</returns>
    public static CbeIntegrationOptions UseSqlite(this CbeIntegrationOptions options, string connectionString)
    {
        options.DataStorageFactory = (s) =>
        {
            var logger = s.GetRequiredService<ILogger<SqliteCbeDataStorage>>();
            return new SqliteCbeDataStorage(logger, connectionString);
        };

        options.SynchronisationStateRegistryFactory = (s) =>
        {
            var logger = s.GetRequiredService<ILogger<SqliteCbeDataStorage>>();
            return new SqliteCbeDataStorage(logger, connectionString);
        };

        options.DatabaseObjectNameProviderFactory = (s) => new SqliteDatabaseObjectNameProvider();

        return options;
    }
}
