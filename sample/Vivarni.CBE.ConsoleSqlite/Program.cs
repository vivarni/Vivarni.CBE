using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Vivarni.CBE.Sqlite;
using Vivarni.CBE.SqlServer;

namespace Vivarni.CBE.ConsoleSqlite;

internal class Program
{
    static async Task Main(string[] args)
    {
        // Configure Serilog
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .MinimumLevel.Debug()
            .CreateLogger();

        try
        {
            var configuration = new ConfigurationBuilder()
                .AddJsonFile($"appsettings.json", true, true)
                .AddUserSecrets<Program>()
                .Build();

            var cbeUser = configuration["cbe:login"] ?? string.Empty;
            var cbePassword = configuration["cbe:password"] ?? string.Empty;
            var sqlServer = configuration.GetConnectionString("sql") ?? string.Empty;
            var sqlite = configuration.GetConnectionString("sqlite") ?? string.Empty;

            var serviceProvider = new ServiceCollection()
                .AddLogging(builder => builder.AddSerilog())
                .AddVivarniCBE(s => s
                    .WithSqliteDatabase(sqlite)
                    .WithFileSystemSource("c:/temp/kbo-files"))
                .BuildServiceProvider();

            Log.Information("Starting CBE synchronization");
            var cbe = serviceProvider.GetRequiredService<ICbeService>();
            await cbe.Sync();
            Log.Information("CBE synchronization completed");
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Application terminated unexpectedly");
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }
}
