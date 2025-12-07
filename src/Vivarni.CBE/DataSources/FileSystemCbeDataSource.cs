namespace Vivarni.CBE.DataSources;

internal class FileSystemCbeDataSource : ICbeDataSource
{
    private readonly string _path;

    public FileSystemCbeDataSource(string path)
    {
        _path = path;
    }

    public Task<IReadOnlyList<CbeOpenDataFile>> GetOpenDataFilesAsync(CancellationToken cancellationToken)
    {
        var files = Directory.GetFiles(_path, "*.zip", SearchOption.TopDirectoryOnly);
        var result = files.Select(filePath => new CbeOpenDataFile(filePath)).ToList();

        return Task.FromResult<IReadOnlyList<CbeOpenDataFile>>(result);
    }

    public Task<Stream> ReadAsync(CbeOpenDataFile file, CancellationToken cancellationToken)
    {
        return Task.FromResult<Stream>(File.OpenRead(file.Source));
    }
}
