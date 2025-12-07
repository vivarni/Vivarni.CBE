namespace Vivarni.CBE.DataSources;

public interface ICbeDataSource
{
    Task<IReadOnlyList<CbeOpenDataFile>> GetOpenDataFilesAsync(CancellationToken cancellationToken);
    Task<Stream> ReadAsync(CbeOpenDataFile file, CancellationToken cancellationToken);
}
