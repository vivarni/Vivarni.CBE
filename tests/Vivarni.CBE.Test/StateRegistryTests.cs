using SQLitePCL;
using Vivarni.CBE.DataStorage;
using Vivarni.CBE.Test.Fixtures;
using Vivarni.CBE.Test.TheoryDataProviders;
using Xunit;

namespace Vivarni.CBE.Test;

[Collection("rdbms")]
public class StateRegistryTests
{
    private readonly OracleTestFixture _oracle;
    private readonly SqliteTestFixture _sqlite;
    private readonly PostgresTestFixture _postgres;
    private readonly SqlServerTestFixture _sqlServer;

    public StateRegistryTests(OracleTestFixture oracle, PostgresTestFixture postgres, SqlServerTestFixture sqlServer)
    {
        _oracle = oracle;
        _sqlite = new();
        _postgres = postgres;
        _sqlServer = sqlServer;
    }

    [Theory(DisplayName = "CbeStateRegistry Read/Write")]
    [ClassData(typeof(RelationalDatabaseNameProvider))]
    public async Task CbeStateRegistry_CanReadAndWrite(string db)
    {
        ICbeStateRegistry registry = db switch
        {
            "postgres" => _postgres.NewPostgresCbeDataStorage(),
            "oracle" => _oracle.NewOracleCbeDataStorage(),
            "sqlserver" => _sqlServer.NewSqlServerCbeDataStorage(),
            "sqlite" => _sqlite.CbeDataStorage,
            _ => throw new ArgumentException($"Unknown dbName: {db}"),
        };

        var storage = (ICbeDataStorage)registry;
        await storage.InitializeAsync(TestContext.Current.CancellationToken);

        // Act
        var r1 = await registry.GetCurrentExtractNumber(TestContext.Current.CancellationToken);
        await registry.SetCurrentExtractNumber(100, TestContext.Current.CancellationToken);
        var r2 = await registry.GetCurrentExtractNumber(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(-1, r1);
        Assert.Equal(100, r2);
    }
}
