using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Vivarni.CBE.SqlServer.Setup;

public static class ConfigurationBuilderExtensions
{
    public static VivarniCbeOptions UseSqlServer(this VivarniCbeOptions builder, string connectionString, string schema = "dbo", string tablePrefix = "")
    {
        builder.DataStorageFactory = (s) =>
        {
            var logger = s.GetRequiredService<ILogger<SqlServerCbeDataStorage>>();
            var storage = new SqlServerCbeDataStorage(logger, connectionString, schema, tablePrefix);
            return storage;
        };

        builder.SynchronisationStateRegistryFactory = (s) =>
        {
            var logger = s.GetRequiredService<ILogger<SqlServerCbeDataStorage>>();
            var storage = new SqlServerCbeDataStorage(logger, connectionString, schema, tablePrefix);
            return storage;
        };

        return builder;
    }
}
