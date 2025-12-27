using Microsoft.Extensions.Logging;
using Npgsql;
using Testcontainers.PostgreSql;
using Vivarni.CBE.Postgres;
using Vivarni.CBE.Postgres.Setup;
using Xunit;

namespace Vivarni.CBE.Test.Fixtures;

public class PostgresTestFixture : IAsyncLifetime
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly PostgreSqlContainer _postgreSqlContainer = new PostgreSqlBuilder()
        .WithCleanUp(true)
        .Build();

    public PostgresTestFixture()
    {
        _loggerFactory = LoggerFactory.Create(builder => builder.SetMinimumLevel(LogLevel.Information));
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
