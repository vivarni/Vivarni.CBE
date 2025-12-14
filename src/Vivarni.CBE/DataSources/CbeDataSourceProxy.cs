using Microsoft.Extensions.Logging;

namespace Vivarni.CBE.DataSources;

internal class CbeDataSourceProxy : ICbeDataSource
{
    private readonly ICbeDataSource? _source;
    private readonly ICbeDataSourceCache? _cache;
    private readonly ILogger _logger;

    public CbeDataSourceProxy(ILogger<CbeDataSourceProxy> logger, ICbeDataSource? source, ICbeDataSourceCache? cache)
    {
        if (source == null && cache == null)
            throw new ArgumentException("At least one " + nameof(ICbeDataSource) + " is required.");

        _logger = logger;
        _source = source;
        _cache = cache;
    }

    public async Task<IReadOnlyList<CbeOpenDataFile>> GetOpenDataFilesAsync(CancellationToken cancellationToken = default)
    {
        var result = new HashSet<CbeOpenDataFile>();
        if (_source != null)
        {
            var items = await _source.GetOpenDataFilesAsync(cancellationToken);
            result.UnionWith(items);
        }

        if (_cache != null)
        {
            var items = await _cache.GetOpenDataFilesAsync(cancellationToken);
            result.UnionWith(items);
        }

        return [.. result];
    }

    public async Task<Stream> ReadAsync(CbeOpenDataFile file, CancellationToken cancellationToken = default)
    {
        const string LOG_PREFIX = "CBE sync: {CbeOpenDataFileName}: ";
        var exceptions = new List<Exception>();

        // First try to get the requested file from our cache. If no cache is configured, or if
        // the cache doesn't have the file, or it fails for some reason, we'll continue with
        // the upstream source later on.
        try
        {
            if (_cache != null && await _cache.ExistsAsync(file, cancellationToken))
            {
                _logger.LogInformation(LOG_PREFIX + "Reading from cache", file);
                return await _cache.ReadAsync(file, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, LOG_PREFIX + "Exception while reading from cache.", file);
            exceptions.Add(ex);
        }

        // We weren't able to download the file from the cache. So we'll use the upstream source
        // in order to download the requested file. In case there is a cache configured, we'll
        // first write the stream from the upstream source to the cache, and then use a new stream
        // from the cache to return to the calling method.
        try
        {
            if (_source != null)
            {
                var sourceStream = await _source.ReadAsync(file, cancellationToken);

                if (_cache != null)
                {
                    _logger.LogInformation(LOG_PREFIX + "Writing to cache.", file);
                    await _cache.WriteAsync(file, sourceStream, cancellationToken);

                    _logger.LogInformation(LOG_PREFIX + "Reading from cache.", file);
                    return await _cache.ReadAsync(file, cancellationToken);
                }

                _logger.LogInformation(LOG_PREFIX + "Read from upstream source.", file);
                return sourceStream;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, LOG_PREFIX + "Exception while reading from upstream source.", file);
            exceptions.Add(ex);
        }

        // Something went horribly wrong. Either both the cache and upstream source are not configured,
        // or they are both failing for some reason.
        throw new AggregateException("Failed to read the OpenDataFile. See inner exceptions for more details.", exceptions);
    }
}
