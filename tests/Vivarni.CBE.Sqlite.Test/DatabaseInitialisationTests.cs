using Microsoft.Data.Sqlite;
using Vivarni.CBE.Sqlite.Test.Fixtures;
using Xunit;

namespace Vivarni.CBE.Sqlite.Test;

public class DatabaseInitialisationTests : IClassFixture<SqliteTestFixture>
{
    private readonly SqliteTestFixture _fixture;

    public DatabaseInitialisationTests(SqliteTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task SqliteDataStorage_DoesNotThrowException()
    {
        var storage = _fixture.CbeDataStorage;
        await storage.InitializeAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task SqliteDataStorage_ShouldCreateAllTables()
    {
        // Arrange
        var storage = _fixture.CbeDataStorage;

        // Act
        await storage.InitializeAsync(TestContext.Current.CancellationToken);

        // Assert
        using var connection = _fixture.DbConnection;
        await connection.OpenAsync(TestContext.Current.CancellationToken);

        const string CheckTablesQuery = @"
            SELECT name 
            FROM sqlite_master 
            WHERE type='table' 
            AND name NOT LIKE 'sqlite_%'
            ORDER BY name;";

        using var command = new SqliteCommand(CheckTablesQuery, connection);
        using var reader = await command.ExecuteReaderAsync(TestContext.Current.CancellationToken);

        var tableNames = new List<string>();
        while (await reader.ReadAsync(TestContext.Current.CancellationToken))
        {
            tableNames.Add(reader.GetString(0));
        }

        // Should have created tables from CBE entities and StateRegistry table
        var expectedTables = new[]
        {
            "CbeActivity",
            "CbeAddress",
            "CbeBranch",
            "CbeCode",
            "CbeContact",
            "CbeDenomination",
            "CbeEnterprise",
            "CbeEstablishment",
            "CbeMeta",
            "StateRegistry"
        };

        Assert.Equal(expectedTables.OrderBy(x => x), tableNames.OrderBy(x => x));
    }
}
