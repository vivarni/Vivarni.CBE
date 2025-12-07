using Vivarni.CBE.DataSources;

namespace Vivarni.CBE.DataStorage;

internal interface ICbeSynchronisationStateRegistry
{
    Task<IEnumerable<CbeOpenDataFile>> GetProcessedFiles(CancellationToken cancellationToken);
    Task UpdateProcessedFileList(List<CbeOpenDataFile> processedFiles, CancellationToken cancellationToken);
}
