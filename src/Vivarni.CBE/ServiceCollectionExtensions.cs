using Microsoft.Extensions.DependencyInjection;
using Vivarni.CBE.DataSources;
using Vivarni.CBE.DataSources.Security;
using Vivarni.CBE.DataStorage;

namespace Vivarni.CBE;

/// <summary>
/// Extension methods for <see cref="IServiceCollection"/>, which allow registration of services for Vivarni CBE.
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddVivarniCBE(this IServiceCollection services, Action<CbeIntegrationOptions> configureOptions)
    {
        var options = new CbeIntegrationOptions();
        configureOptions(options);

        if (options.DataStorageFactory == null)
            throw new InvalidOperationException("Vivarni CBE: Missing data storage configuration.");

        if (options.DataSourceFactory == null && options.DataSourceCacheFactory == null)
            throw new InvalidOperationException("Vivarni CBE: Missing data source and data source cache configuration. At least one of DataSourceFactory or DataSourceCacheFactory must be provided.");

        if (options.SynchronisationStateRegistryFactory == null)
            throw new InvalidOperationException("Vivarni CBE: Missing data source configuration.");

        if (options.DatabaseObjectNameProviderFactory != null)
            services.AddScoped(options.DatabaseObjectNameProviderFactory);
        if (options.DataSourceFactory != null)
            services.AddScoped(options.DataSourceFactory);
        if (options.DataSourceCacheFactory != null)
            services.AddScoped(options.DataSourceCacheFactory);

        services.AddScoped(options.SynchronisationStateRegistryFactory);
        services.AddScoped(options.DataStorageFactory);

        services.AddScoped<ICbeService, CbeService>();
        services.AddScoped<CbeDataSourceProxy>();

        return services;
    }
}

public class CbeIntegrationOptions
{
    // Data sources (HTTP/FTP + Caching)
    internal Func<IServiceProvider, ICbeDataSource>? DataSourceFactory { get; set; }
    internal Func<IServiceProvider, ICbeDataSourceCache>? DataSourceCacheFactory { get; set; }

    // Data storage (tables + synchronisation state)
    public Func<IServiceProvider, ICbeDataStorage>? DataStorageFactory { get; set; }
    public Func<IServiceProvider, ICbeStateRegistry>? SynchronisationStateRegistryFactory { get; set; }
    public Func<IServiceProvider, IDatabaseObjectNameProvider>? DatabaseObjectNameProviderFactory { get; set; }

    /// <summary>
    /// Configures the CBE integration to use the web interface from the Belgian Federal Government to
    /// download the CBE ZIP files (both FULL and UPDATE files).
    /// https://economie.fgov.be/en/themes/enterprises/crossroads-bank-enterprises/services-everyone/public-data-available-reuse/cbe-open-data
    /// </summary>
    /// <param name="userName">Username for access to the CBE Open Data.</param>
    /// <param name="password">Password for access to the CBE Open Data.</param>
    /// <returns>The same <see cref="CbeIntegrationOptions"/> instance, so that configuration can be chained.</returns>
    public CbeIntegrationOptions UseHttpSource(string userName, string password)
    {
        var credentialProvider = new SimpleCredentialProvider(userName, System.Text.Encoding.UTF8.GetBytes(password));
        var cbeDataSource = new HttpCbeDataSource(credentialProvider);

        DataSourceFactory = (s) => cbeDataSource;
        return this;
    }

    /// <summary>
    /// Configures the CBE integration to use the FTPS server from the Belgian Federal Government to
    /// download the CBE ZIP files (both FULL and UPDATE files).
    /// https://economie.fgov.be/en/themes/enterprises/crossroads-bank-enterprises/services-everyone/public-data-available-reuse/cbe-open-data
    /// </summary>
    /// <param name="userName">Username for access to the CBE Open Data.</param>
    /// <param name="password">Password for access to the CBE Open Data.</param>
    /// <returns>The same <see cref="CbeIntegrationOptions"/> instance, so that configuration can be chained.</returns>
    public CbeIntegrationOptions UseFtpsSource(string userName, string password)
    {
        var credentialProvider = new SimpleCredentialProvider(userName, System.Text.Encoding.UTF8.GetBytes(password));
        var cbeDataSource = new FtpsDataSource(credentialProvider);

        DataSourceFactory = (s) => cbeDataSource;
        return this;
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
