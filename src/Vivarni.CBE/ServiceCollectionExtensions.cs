using Microsoft.Extensions.DependencyInjection;
using Vivarni.CBE.DataSources;
using Vivarni.CBE.DataSources.Security;
using Vivarni.CBE.DataStorage;

namespace Vivarni.CBE;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddVivarniCBE(this IServiceCollection services, Func<VivarniCbeOptions, VivarniCbeOptions> clientBuilder)
    {
        var options = new VivarniCbeOptions();
        options = clientBuilder(options);

        if (options.DataStorageFactory == null)
            throw new InvalidOperationException("Vivarni CBE: Missing data storage configuration.");

        if (options.DataSourceFactory == null && options.DataSourceCacheFactory == null)
            throw new InvalidOperationException("Vivarni CBE: Missing data source and data source cache configuration. At least one of DataSourceFactory or DataSourceCacheFactory must be provided.");

        if (options.SynchronisationStateRegistryFactory == null)
            throw new InvalidOperationException("Vivarni CBE: Missing data source configuration.");

        services.AddScoped<ICbeService, CbeService>();
        services.AddScoped<ICbeStateRegistry>(s => options.SynchronisationStateRegistryFactory(s));
        services.AddScoped<ICbeDataStorage>(s => options.DataStorageFactory(s));
        services.AddScoped<ICbeDataSource>(s =>
        {
            var source = options.DataSourceFactory == null ? null : options.DataSourceFactory(s);
            var cache = options.DataSourceCacheFactory == null ? null : options.DataSourceCacheFactory(s);
            return new CbeDataSourceProxy(source, cache);
        });

        return services;
    }
}

public class VivarniCbeOptions
{
    public Func<IServiceProvider, ICbeDataStorage>? DataStorageFactory { get; set; }
    public Func<IServiceProvider, ICbeDataSource>? DataSourceFactory { get; set; }
    public Func<IServiceProvider, ICbeDataSource>? DataSourceCacheFactory { get; set; }
    public Func<IServiceProvider, ICbeStateRegistry>? SynchronisationStateRegistryFactory { get; set; }

    public VivarniCbeOptions UseHttpSource(string userName, string password)
    {
        var credentialProvider = new SimpleCredentialProvider(userName, System.Text.Encoding.UTF8.GetBytes(password));
        var cbeDataSource = new HttpCbeDataSource(credentialProvider);

        DataSourceFactory = (s) => cbeDataSource;
        return this;
    }

    public VivarniCbeOptions UseFileSystemCache(string path)
    {
        DataSourceCacheFactory = (s) => new FileSystemCbeDataSource(path);
        return this;
    }
}
