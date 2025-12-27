using Microsoft.Data.SqlClient;
using Vivarni.CBE.DataStorage;
using Vivarni.CBE.Test.Fixtures;
using Vivarni.CBE.Test.TheoryDataProviders;
using Xunit;

namespace Vivarni.CBE.Test;

[Collection("rdbms")]
public class DatabaseInitialisationTests
{
    private readonly OracleTestFixture _oracle;
    private readonly SqliteTestFixture _sqlite;
    private readonly PostgresTestFixture _postgres;
    private readonly SqlServerTestFixture _sqlServer;

    public DatabaseInitialisationTests(OracleTestFixture oracle, PostgresTestFixture postgres, SqlServerTestFixture sqlServer)
    {
        _oracle = oracle;
        _sqlite = new();
        _postgres = postgres;
        _sqlServer = sqlServer;
    }

    [Theory(DisplayName = "InitializeAsync does not throw")]
    [ClassData(typeof(RelationalDatabaseNameProvider))]
    public async Task CbeDataStorage_DoesNotThrowException(string db)
    {
        ICbeDataStorage storage = db switch
        {
            "postgres" => _postgres.NewPostgresCbeDataStorage(),
            "oracle" => _oracle.NewOracleCbeDataStorage(),
            "sqlserver" => _sqlServer.NewSqlServerCbeDataStorage(),
            "sqlite" => _sqlite.CbeDataStorage,
            _ => throw new ArgumentException($"Unknown dbName: {db}"),
        };

        await storage.InitializeAsync(TestContext.Current.CancellationToken);
    }

    [Fact(DisplayName = "SqlServer Should use TablePrefix")]
    public async Task SqlServerDataStorage_ShouldCreateUniqueTablesForDifferentPrefixes()
    {
        // Arrange
        var dataStorage1 = _sqlServer.NewSqlServerCbeDataStorage(schema: "dbo", tablePrefix: "test1_");
        var dataStorage2 = _sqlServer.NewSqlServerCbeDataStorage(schema: "dbo", tablePrefix: "test2_");

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
        await using var connection = _sqlServer.NewDbConnection();
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
