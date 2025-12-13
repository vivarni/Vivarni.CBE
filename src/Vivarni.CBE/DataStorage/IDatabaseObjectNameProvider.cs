using Vivarni.CBE.DataSources;

namespace Vivarni.CBE.DataStorage;

public interface IDatabaseObjectNameProvider
{
    string GetTableName<T>() where T : ICbeEntity;
}
