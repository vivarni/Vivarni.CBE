using System.Text;
using System.Text.Json;
using Vivarni.CBE.DataSources;

namespace Vivarni.CBE.DataStorage;

public class SimpleStateRegistry : ICbeSynchronisationStateRegistry
{
    private const string PATH = "c:/temp/vivarni-cbe.json";

    public async Task<IEnumerable<CbeOpenDataFile>> GetProcessedFiles(CancellationToken cancellationToken)
    {
        if (!File.Exists(PATH))
        {
            return [];
        }

        await using var fs = new FileStream(PATH, FileMode.Open, FileAccess.Read, FileShare.Read);
        var list = JsonSerializer.Deserialize<List<string>>(fs) ?? [];
        return list.Select(s => new CbeOpenDataFile(s));
    }

    public Task UpdateProcessedFileList(List<CbeOpenDataFile> processedFiles, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(PATH)!);

        var data = processedFiles.Select(s => s.Filename);
        var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
        return File.WriteAllTextAsync(PATH, json, Encoding.UTF8, cancellationToken);
    }
}
