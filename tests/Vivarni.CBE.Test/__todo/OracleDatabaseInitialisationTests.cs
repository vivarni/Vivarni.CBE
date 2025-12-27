using Oracle.ManagedDataAccess.Client;
using Vivarni.CBE.Oracle.Setup;
using Vivarni.CBE.Oracle.Test.Fixtures;
using Xunit;

namespace Vivarni.CBE.Oracle.Test;

[Collection("OracleCollection")]
public class OracleDatabaseInitialisationTests
{
    private readonly OracleTestFixture _fixture;

    public OracleDatabaseInitialisationTests(OracleTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task OracleStorage_DoesNotThrowException()
    {
        var storage = _fixture.NewOracleCbeDataStorage();
        await storage.InitializeAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task DataStorage_ShouldCreateUniqueTablesForDifferentPrefixes()
    {
        // Arrange
        var options1 = new OracleCbeOptions { TablePrefix = "TEST1_" };
        var options2 = new OracleCbeOptions { TablePrefix = "TEST2_" };

        var dataStorage1 = _fixture.NewOracleCbeDataStorage(options1);
        var dataStorage2 = _fixture.NewOracleCbeDataStorage(options2);

        // Act
        await dataStorage1.InitializeAsync(TestContext.Current.CancellationToken);
        await dataStorage2.InitializeAsync(TestContext.Current.CancellationToken);
        var realTableNames = await GetRealTableNames();

        // Assert
        Assert.Contains(realTableNames, name => name.StartsWith("TEST1_"));
        Assert.Contains(realTableNames, name => name.StartsWith("TEST2_"));
    }

    private async Task<List<string>> GetRealTableNames()
    {
        await using var connection = _fixture.NewDbConnection();
        await connection.OpenAsync(TestContext.Current.CancellationToken);

        const string CheckTablesQuery = @"
            SELECT TABLE_NAME 
            FROM USER_TABLES 
            ORDER BY TABLE_NAME";

        await using var command = new OracleCommand(CheckTablesQuery, connection);
        await using var reader = await command.ExecuteReaderAsync(TestContext.Current.CancellationToken);

        var tableNames = new List<string>();
        while (await reader.ReadAsync(TestContext.Current.CancellationToken))
            tableNames.Add(reader.GetString(0));

        return tableNames;
    }
}
