namespace Vivarni.CBE.DataSources;

internal class FileSystemCbeDataSource : ICbeDataSourceCache
{
    private readonly Lazy<string> _path;

    /// <summary>
    /// Creates a new instance of this class.
    /// </summary>
    /// <param name="path">File system path where the ZIP files are stored. The application needs read and write access to this path.</param>
    public FileSystemCbeDataSource(string path)
    {
        _path = new(() =>
        {
            Directory.CreateDirectory(path);
            return path;
        });
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<CbeOpenDataFile>> GetOpenDataFilesAsync(CancellationToken cancellationToken)
    {
        var files = Directory.GetFiles(_path.Value, "*.zip", SearchOption.TopDirectoryOnly);
        var result = files.Select(filePath => new CbeOpenDataFile(filePath)).ToList();

        return Task.FromResult<IReadOnlyList<CbeOpenDataFile>>(result);
    }

    /// <inheritdoc/>
    public Task<Stream> ReadAsync(CbeOpenDataFile file, CancellationToken cancellationToken)
    {
        var filePath = Path.Combine(_path.Value, file.Filename);
        return Task.FromResult<Stream>(File.OpenRead(filePath));
    }

    /// <inheritdoc/>
    public async Task WriteAsync(CbeOpenDataFile file, Stream sourceStream, CancellationToken cancellationToken)
    {
        var filePath = Path.Combine(_path.Value, file.Filename);

        // Don't overwrite if file already exists
        if (File.Exists(filePath))
            return;

        var tempPath = filePath + ".tmp";

        try
        {
            // Use streaming copy to avoid loading entire file into memory,
            // then use an atomic move in order to ensure the file is never
            // only partially created.
            using (var fileStream = File.Create(tempPath))
            {
                await sourceStream.CopyToAsync(fileStream, cancellationToken);
                await fileStream.FlushAsync(cancellationToken);
            } // Ensure file stream is properly disposed before move

            File.Move(tempPath, filePath);
        }
        catch
        {
            if (File.Exists(tempPath))
                File.Delete(tempPath);
            throw;
        }
    }

    /// <inheritdoc/>
    public Task<bool> ExistsAsync(CbeOpenDataFile file, CancellationToken cancellationToken)
    {
        var filePath = Path.Combine(_path.Value, file.Filename);
        return Task.FromResult(File.Exists(filePath));
    }
}
