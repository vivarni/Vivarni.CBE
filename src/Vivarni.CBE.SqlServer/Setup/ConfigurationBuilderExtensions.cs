using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Vivarni.CBE.SqlServer.Setup;

public static class ConfigurationBuilderExtensions
{
    public static CbeIntegrationOptions UseSqlServer(this CbeIntegrationOptions options, string connectionString, string schema = "dbo", string tablePrefix = "")
    {
        options.DataStorageFactory = (s) =>
        {
            var logger = s.GetRequiredService<ILogger<SqlServerCbeDataStorage>>();
            var storage = new SqlServerCbeDataStorage(logger, connectionString, schema, tablePrefix);
            return storage;
        };

        options.SynchronisationStateRegistryFactory = (s) =>
        {
            var logger = s.GetRequiredService<ILogger<SqlServerCbeDataStorage>>();
            var storage = new SqlServerCbeDataStorage(logger, connectionString, schema, tablePrefix);
            return storage;
        };

        return options;
    }
}
