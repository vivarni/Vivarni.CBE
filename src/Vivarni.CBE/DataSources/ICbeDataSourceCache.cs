namespace Vivarni.CBE.DataSources;

/// <summary>
/// Interface for data sources that support writing/caching files.
/// This extends ICbeDataSource with write capabilities for caching scenarios.
/// </summary>
public interface ICbeDataSourceCache : ICbeDataSource
{
    /// <summary>
    /// Writes a file to the cache using streaming operations.
    /// </summary>
    /// <param name="file">The data file to cache</param>
    /// <param name="sourceStream">The stream containing the file data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A task representing the async write operation</returns>
    Task WriteAsync(CbeOpenDataFile file, Stream sourceStream, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a file already exists in the cache.
    /// </summary>
    /// <param name="file">The data file to check</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if the file exists, false otherwise</returns>
    Task<bool> ExistsAsync(CbeOpenDataFile file, CancellationToken cancellationToken = default);
}
