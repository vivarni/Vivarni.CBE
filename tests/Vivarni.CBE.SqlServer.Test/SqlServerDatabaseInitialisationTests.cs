using Microsoft.Data.SqlClient;
using Vivarni.CBE.SqlServer.Test.Fixtures;
using Xunit;

namespace Vivarni.CBE.SqlServer.Test;

[Collection("SqlServerCollection")]
public class SqlServerDatabaseInitialisationTests
{
    private readonly SqlServerTestFixture _fixture;

    public SqlServerDatabaseInitialisationTests(SqlServerTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task SqlServerDataStorage_DoesNotThrowException()
    {
        var storage = _fixture.NewSqlServerCbeDataStorage();
        await storage.InitializeAsync(TestContext.Current.CancellationToken);
    }

    [Fact(DisplayName = "Should use TablePrefix")]
    public async Task SqlServerDataStorage_ShouldCreateUniqueTablesForDifferentPrefixes()
    {
        // Arrange
        var dataStorage1 = _fixture.NewSqlServerCbeDataStorage(schema: "dbo", tablePrefix: "test1_");
        var dataStorage2 = _fixture.NewSqlServerCbeDataStorage(schema: "dbo", tablePrefix: "test2_");

        // Act
        await dataStorage1.InitializeAsync(TestContext.Current.CancellationToken);
        await dataStorage2.InitializeAsync(TestContext.Current.CancellationToken);
        var realTableNames = await GetRealTableNames();

        // Assert
        Assert.Contains(realTableNames, name => name.StartsWith("test1_"));
        Assert.Contains(realTableNames, name => name.StartsWith("test2_"));
    }

    private async Task<List<string>> GetRealTableNames()
    {
        await using var connection = _fixture.NewDbConnection();
        await connection.OpenAsync(TestContext.Current.CancellationToken);

        const string CheckTablesQuery = @"
            SELECT TABLE_NAME 
            FROM INFORMATION_SCHEMA.TABLES 
            WHERE TABLE_SCHEMA = 'dbo' 
            AND (TABLE_NAME LIKE 'test1_%' OR TABLE_NAME LIKE 'test2_%')
            ORDER BY TABLE_NAME;";

        await using var command = new SqlCommand(CheckTablesQuery, connection);
        await using var reader = await command.ExecuteReaderAsync(TestContext.Current.CancellationToken);

        var tableNames = new List<string>();
        while (await reader.ReadAsync(TestContext.Current.CancellationToken))
        {
            tableNames.Add(reader.GetString(0));
        }

        return tableNames;
    }
}
