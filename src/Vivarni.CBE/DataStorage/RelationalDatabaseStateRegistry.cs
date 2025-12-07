using System.Text.Json;
using Vivarni.CBE.DataSources;

namespace Vivarni.CBE.DataStorage;

public class RelationalDatabaseStateRegistry : ICbeSynchronisationStateRegistry
{
    private readonly RelationalDatabaseStorage _cbeDatabase;
    private const string SYNC_PROCESSED_FILES_VARIABLE = "SyncProcessedFiles";

    public RelationalDatabaseStateRegistry(RelationalDatabaseStorage cbeDatabase)
    {
        _cbeDatabase = cbeDatabase;
    }

    public async Task<IEnumerable<CbeOpenDataFile>> GetProcessedFiles(CancellationToken cancellationToken)
    {
        using var conn = _cbeDatabase.NewConnection();
        await conn.OpenAsync(cancellationToken);


        await using var command = conn.CreateCommand();
        command.CommandText = "SELECT [Value] FROM [kbo].[Meta] WHERE [Variable] = @Variable";

        var parameter = command.CreateParameter();
        parameter.ParameterName = "@Variable";
        parameter.Value = SYNC_PROCESSED_FILES_VARIABLE;
        command.Parameters.Add(parameter);

        var result = await command.ExecuteScalarAsync(cancellationToken) as string;

        if (string.IsNullOrEmpty(result))
        {
            return Enumerable.Empty<CbeOpenDataFile>();
        }

        var list = JsonSerializer.Deserialize<List<string>>(result) ?? [];
        return list.Select(s => new CbeOpenDataFile(s));
    }

    public async Task UpdateProcessedFileList(List<CbeOpenDataFile> processedFiles, CancellationToken cancellationToken)
    {
        await using var conn = _cbeDatabase.NewConnection();
        await conn.OpenAsync(cancellationToken);

        var data = processedFiles.Select(s => s.Filename);
        var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });

        const string upsertQuery = @"
            IF EXISTS (SELECT 1 FROM [kbo].[Meta] WHERE [Variable] = @Variable)
                UPDATE [kbo].[Meta] SET [Value] = @Value WHERE [Variable] = @Variable
            ELSE
                INSERT INTO [kbo].[Meta] ([Variable], [Value]) VALUES (@Variable, @Value)";

        await using var command = conn.CreateCommand();
        command.CommandText = upsertQuery;

        var variableParam = command.CreateParameter();
        variableParam.ParameterName = "@Variable";
        variableParam.Value = SYNC_PROCESSED_FILES_VARIABLE;
        command.Parameters.Add(variableParam);

        var valueParam = command.CreateParameter();
        valueParam.ParameterName = "@Value";
        valueParam.Value = json;
        command.Parameters.Add(valueParam);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
