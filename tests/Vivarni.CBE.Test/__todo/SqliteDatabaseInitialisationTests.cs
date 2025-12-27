using Microsoft.Data.Sqlite;
using Vivarni.CBE.Sqlite.Test.Fixtures;
using Xunit;

namespace Vivarni.CBE.Sqlite.Test;

public class SqliteDatabaseInitialisationTests : IClassFixture<SqliteTestFixture>
{
    private readonly SqliteTestFixture _fixture;

    public SqliteDatabaseInitialisationTests(SqliteTestFixture fixture)
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

        // Act
        await storage.InitializeAsync(TestContext.Current.CancellationToken);
        var tableNames = GetRealTableNames();

        // Assert
        Assert.Equal(expectedTables.OrderBy(x => x), tableNames.OrderBy(x => x));
    }

    private async IAsyncEnumerable<string> GetRealTableNames()
    {
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

        while (await reader.ReadAsync(TestContext.Current.CancellationToken))
            yield return reader.GetString(0);
    }
}
