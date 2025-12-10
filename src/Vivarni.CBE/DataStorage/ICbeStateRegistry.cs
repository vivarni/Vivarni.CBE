using Vivarni.CBE.DataSources;

namespace Vivarni.CBE.DataStorage;

public interface ICbeStateRegistry
{
    Task<IEnumerable<CbeOpenDataFile>> GetProcessedFiles(CancellationToken cancellationToken);
    Task UpdateProcessedFileList(List<CbeOpenDataFile> processedFiles, CancellationToken cancellationToken);
}
