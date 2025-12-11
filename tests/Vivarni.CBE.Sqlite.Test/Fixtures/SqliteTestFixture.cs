using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;

namespace Vivarni.CBE.Sqlite.Test.Fixtures;

public class SqliteTestFixture : IDisposable
{
    private readonly string _databasePath;
    internal SqliteConnection DbConnection { get; }
    internal SqliteCbeDataStorage CbeDataStorage { get; }

    public SqliteTestFixture()
    {
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Information));
        var logger = loggerFactory.CreateLogger<SqliteCbeDataStorage>();

        // Use a temporary file database instead of in-memory to persist across connections
        _databasePath = Path.GetTempFileName();
        var connectionString = $"Data Source={_databasePath}";

        DbConnection = new SqliteConnection(connectionString);
        CbeDataStorage = new SqliteCbeDataStorage(logger, connectionString);
    }

    public void Dispose()
    {
        DbConnection?.Dispose();

        // Clean up the temporary database file
        if (File.Exists(_databasePath))
        {
            try
            {
                File.Delete(_databasePath);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }
}
