using Microsoft.Extensions.Logging;
using Oracle.ManagedDataAccess.Client;
using Testcontainers.Oracle;
using Vivarni.CBE.Oracle;
using Vivarni.CBE.Oracle.Setup;
using Xunit;

namespace Vivarni.CBE.Test.Fixtures;

public class OracleTestFixture : IAsyncLifetime
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly OracleContainer _oracleContainer = new OracleBuilder()
        .WithImage("gvenzl/oracle-xe:21.3.0-slim-faststart")
        .WithCleanUp(true)
        .Build();

    public OracleTestFixture()
    {
        _loggerFactory = LoggerFactory.Create(builder => builder.SetMinimumLevel(LogLevel.Information));
    }

    public async ValueTask InitializeAsync()
    {
        await _oracleContainer.StartAsync();
    }

    public async ValueTask DisposeAsync()
    {
        _loggerFactory?.Dispose();
        await _oracleContainer.DisposeAsync();
    }

    internal OracleCbeDataStorage NewOracleCbeDataStorage(OracleCbeOptions? opts = null)
    {
        var logger = _loggerFactory.CreateLogger<OracleCbeDataStorage>();
        var connectionString = _oracleContainer.GetConnectionString();
        return new OracleCbeDataStorage(logger, connectionString, opts);
    }

    internal OracleConnection NewDbConnection()
    {
        var connectionString = _oracleContainer.GetConnectionString();
        return new OracleConnection(connectionString);
    }
}
