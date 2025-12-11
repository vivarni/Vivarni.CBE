namespace Vivarni.CBE.DataSources;

internal class CbeDataSourceProxy : ICbeDataSource
{
    private readonly ICbeDataSource? _source;
    private readonly ICbeDataSource? _cache;

    public CbeDataSourceProxy(ICbeDataSource? source, ICbeDataSource? cache)
    {
        if (source == null && cache == null)
            throw new ArgumentException("At least one " + nameof(ICbeDataSource) + " is required.");

        _source = source;
        _cache = cache;
    }

    public async Task<IReadOnlyList<CbeOpenDataFile>> GetOpenDataFilesAsync(CancellationToken cancellationToken)
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

    public Task<Stream> ReadAsync(CbeOpenDataFile file, CancellationToken cancellationToken)
    {
        var exceptions = new List<Exception>();

        try { if (_cache != null) return _cache.ReadAsync(file, cancellationToken); } catch (Exception ex) { exceptions.Add(ex); }
        try { if (_source != null) return _source.ReadAsync(file, cancellationToken); } catch (Exception ex) { exceptions.Add(ex); }

        throw new AggregateException("Failed to read the OpenDataFile. See inner exceptions for more details.", exceptions);
    }
}
