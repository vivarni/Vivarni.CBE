using Npgsql;
using Vivarni.CBE.Postgres.Setup;
using Vivarni.CBE.Postgres.Test.Fixtures;
using Xunit;

namespace Vivarni.CBE.Postgres.Test;

[Collection("PostgresCollection")]
public class PostgresDatabaseInitialisationTests
{
    private readonly PostgresTestFixture _fixture;

    public PostgresDatabaseInitialisationTests(PostgresTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task PostgresStorage_DoesNotThrowException()
    {
        var storage = _fixture.NewPostgresCbeDataStorage();
        await storage.InitializeAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task DataStorage_ShouldCreateUniqueTablesForDifferentPrefixes()
    {
        // Arrange
        var connectionString = _fixture.NewDbConnection;

        var options1 = new PostgresCbeOptions { TablePrefix = "test1_" };
        var options2 = new PostgresCbeOptions { TablePrefix = "test2_" };

        var dataStorage1 = _fixture.NewPostgresCbeDataStorage(options1);
        var dataStorage2 = _fixture.NewPostgresCbeDataStorage(options2);

        // Act
        await dataStorage1.InitializeAsync(TestContext.Current.CancellationToken);
        await dataStorage2.InitializeAsync(TestContext.Current.CancellationToken);

        // Assert
        await using var connection = _fixture.NewDbConnection();
        await connection.OpenAsync(TestContext.Current.CancellationToken);

        const string CheckTablesQuery = @"
            SELECT table_name 
            FROM information_schema.tables 
            WHERE table_schema = 'public' 
            AND table_name LIKE '%cbe%'
            ORDER BY table_name;";

        await using var command = new NpgsqlCommand(CheckTablesQuery, connection);
        await using var reader = await command.ExecuteReaderAsync(TestContext.Current.CancellationToken);

        var tableNames = new List<string>();
        while (await reader.ReadAsync(TestContext.Current.CancellationToken))
        {
            tableNames.Add(reader.GetString(0));
        }

        // Should have tables for both prefixes
        Assert.Contains(tableNames, name => name.StartsWith("test1_"));
        Assert.Contains(tableNames, name => name.StartsWith("test2_"));
    }

    [Fact]
    public async Task PostrgresDataStorage_ShouldCreateIndices()
    {

    }
}
