using Microsoft.Extensions.DependencyInjection;
using Vivarni.CBE.DataSources;
using Vivarni.CBE.DataSources.Security;
using Vivarni.CBE.DataStorage;

namespace Vivarni.CBE;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddVivarniCBE(this IServiceCollection services, Func<CbeIntegrationOptions, CbeIntegrationOptions> clientBuilder)
    {
        var options = new CbeIntegrationOptions();
        options = clientBuilder(options);

        if (options.DataStorageFactory == null)
            throw new InvalidOperationException("Vivarni CBE: Missing data storage configuration.");

        if (options.DataSourceFactory == null && options.DataSourceCacheFactory == null)
            throw new InvalidOperationException("Vivarni CBE: Missing data source and data source cache configuration. At least one of DataSourceFactory or DataSourceCacheFactory must be provided.");

        if (options.SynchronisationStateRegistryFactory == null)
            throw new InvalidOperationException("Vivarni CBE: Missing data source configuration.");

        if (options.DatabaseObjectNameProviderFactory != null)
            services.AddScoped(options.DatabaseObjectNameProviderFactory);

        services.AddScoped(options.SynchronisationStateRegistryFactory);
        services.AddScoped(options.DataStorageFactory);

        services.AddScoped<ICbeService, CbeService>();
        services.AddScoped<ICbeDataSource>(s =>
        {
            var source = options.DataSourceFactory == null ? null : options.DataSourceFactory(s);
            var cache = options.DataSourceCacheFactory == null ? null : options.DataSourceCacheFactory(s);
            return new CbeDataSourceProxy(source, cache);
        });

        return services;
    }
}

public class CbeIntegrationOptions
{
    // Data sources (HTTP/FTP + Caching)
    public Func<IServiceProvider, ICbeDataSource>? DataSourceFactory { get; set; }
    public Func<IServiceProvider, ICbeDataSource>? DataSourceCacheFactory { get; set; }

    // Data storage (tables + synchronisation state)
    public Func<IServiceProvider, ICbeDataStorage>? DataStorageFactory { get; set; }
    public Func<IServiceProvider, ICbeStateRegistry>? SynchronisationStateRegistryFactory { get; set; }
    public Func<IServiceProvider, IDatabaseObjectNameProvider>? DatabaseObjectNameProviderFactory { get; set; }

    public CbeIntegrationOptions UseHttpSource(string userName, string password)
    {
        var credentialProvider = new SimpleCredentialProvider(userName, System.Text.Encoding.UTF8.GetBytes(password));
        var cbeDataSource = new HttpCbeDataSource(credentialProvider);

        DataSourceFactory = (s) => cbeDataSource;
        return this;
    }

    public CbeIntegrationOptions UseFTPS(string userName, string password)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Configures the CBE integration to use the file system as a cache for all ZIP files from the
    /// CBE. This includes both FULL and UPDATE files. The cache is also can be used without active
    /// <see cref="ICbeDataSource"/> such as the FTP or HTTP clients. Usefull if you have other
    /// systems responsible for obtaining the CBE Zip files.
    /// </summary>
    /// <param name="path">File system path where the ZIP files should be stored.</param>
    /// <returns>The same <see cref="CbeIntegrationOptions"/> instance, so that configuration can be chained.</returns>
    public CbeIntegrationOptions UseFileSystemCache(string path)
    {
        DataSourceCacheFactory = (s) => new FileSystemCbeDataSource(path);
        return this;
    }
}
