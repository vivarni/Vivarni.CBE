using Microsoft.Extensions.DependencyInjection;

namespace Vivarni.CBE.Test;

internal class ServiceProviderFactory
{
    public static IServiceProvider Create(Action<IServiceCollection>? configure = null)
    {
        var services = new ServiceCollection();
        configure?.Invoke(services);

        return services.BuildServiceProvider();
    }
}
