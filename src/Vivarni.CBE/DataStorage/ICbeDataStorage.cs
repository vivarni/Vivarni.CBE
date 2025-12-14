using System.Reflection;
using Vivarni.CBE.DataSources;

namespace Vivarni.CBE.DataStorage;

public interface ICbeDataStorage
{
    Task InitializeAsync(CancellationToken cancellationToken = default);

    Task AddRangeAsync<T>(IEnumerable<T> entities, CancellationToken cancellationToken = default)
        where T : class, ICbeEntity;

    Task ClearAsync<T>(CancellationToken cancellationToken = default)
        where T : class, ICbeEntity;

    Task<int> RemoveAsync<T>(IEnumerable<object> entityIds, PropertyInfo deleteOnProperty, CancellationToken cancellationToken = default)
        where T : class, ICbeEntity;
}
