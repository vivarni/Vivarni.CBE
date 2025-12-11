using Microsoft.Extensions.Logging;
using Npgsql;
using Testcontainers.PostgreSql;
using Xunit;

namespace Vivarni.CBE.Postgres.Test;

public class PostgresTestFixture : IAsyncLifetime
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly PostgreSqlContainer _postgreSqlContainer = new PostgreSqlBuilder()
        .WithDatabase("test_db")
        .WithUsername("test_user")
        .WithPassword("test_password")
        .WithCleanUp(true)
        .Build();

    public PostgresTestFixture()
    {
        _loggerFactory = LoggerFactory.Create(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Information));
    }

    public async ValueTask InitializeAsync()
    {
        await _postgreSqlContainer.StartAsync();
    }

    public async ValueTask DisposeAsync()
    {
        _loggerFactory?.Dispose();
        await _postgreSqlContainer.DisposeAsync();
    }

    internal PostgresCbeDataStorage NewPostgresCbeDataStorage(PostgresCbeOptions? opts = null)
    {
        var logger = _loggerFactory.CreateLogger<PostgresCbeDataStorage>();
        var connectionString = _postgreSqlContainer.GetConnectionString();
        return new PostgresCbeDataStorage(logger, connectionString, opts);
    }

    internal NpgsqlConnection NewDbConnection()
    {
        var connectionString = _postgreSqlContainer.GetConnectionString();
        return new NpgsqlConnection(connectionString);
    }
}

[CollectionDefinition("PostgresCollection")]
public class PostgresCollection : ICollectionFixture<PostgresTestFixture>
{
    // This class has no code, and is never created. Its purpose is simply
    // to be the place to apply [CollectionDefinition] and all the
    // ICollectionFixture<> interfaces.
}
