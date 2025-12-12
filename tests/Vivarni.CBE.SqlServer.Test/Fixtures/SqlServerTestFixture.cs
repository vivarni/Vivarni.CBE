using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Testcontainers.MsSql;
using Xunit;

namespace Vivarni.CBE.SqlServer.Test.Fixtures;

public class SqlServerTestFixture : IAsyncLifetime
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly MsSqlContainer _sqlServerContainer = new MsSqlBuilder()
        .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
        .WithCleanUp(true)
        .Build();

    public SqlServerTestFixture()
    {
        _loggerFactory = LoggerFactory.Create(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Information));
    }

    public async ValueTask InitializeAsync()
    {
        await _sqlServerContainer.StartAsync();
    }

    public async ValueTask DisposeAsync()
    {
        _loggerFactory?.Dispose();
        await _sqlServerContainer.DisposeAsync();
    }

    public string ConnectionString => _sqlServerContainer.GetConnectionString();

    internal SqlServerCbeDataStorage NewSqlServerCbeDataStorage(string schema = "dbo", string tablePrefix = "")
    {
        var logger = _loggerFactory.CreateLogger<SqlServerCbeDataStorage>();
        var connectionString = _sqlServerContainer.GetConnectionString();
        return new SqlServerCbeDataStorage(logger, connectionString, schema, tablePrefix);
    }

    internal SqlConnection NewDbConnection()
    {
        var connectionString = _sqlServerContainer.GetConnectionString();
        return new SqlConnection(connectionString);
    }
}

[CollectionDefinition("SqlServerCollection")]
public class SqlServerCollection : ICollectionFixture<SqlServerTestFixture>
{
    // This class has no code, and is never created. Its purpose is simply
    // to be the place to apply [CollectionDefinition] and all the
    // ICollectionFixture<> interfaces.
}
