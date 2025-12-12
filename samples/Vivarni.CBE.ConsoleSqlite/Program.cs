using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Vivarni.CBE.Sqlite.Setup;

namespace Vivarni.CBE.ConsoleSqlite;

internal class Program
{
    public static async Task Main()
    {
        // Configure Serilog for minimal output
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .MinimumLevel.Information()
            .CreateLogger();

        try
        {
            var configuration = new ConfigurationBuilder()
                .AddJsonFile($"appsettings.json", true, true)
                .AddUserSecrets<Program>()
                .Build();

            var cbeUser = configuration["cbe:login"] ?? string.Empty;
            var cbePassword = configuration["cbe:password"] ?? string.Empty;
            var connectionString = configuration.GetConnectionString("sqlite") ?? string.Empty;

            var serviceProvider = new ServiceCollection()
                .AddLogging(builder => builder.AddSerilog())
                .AddSingleton<IConfiguration>(configuration)
                .AddSingleton<SearchDemo>()
                .AddVivarniCBE(s => s
                    .UseSqliteDatabase(connectionString)
                    .UseFileSystemCache("c:/temp/kbo-files"))
                .BuildServiceProvider();

            var cbe = serviceProvider.GetRequiredService<ICbeService>();
            var demo = serviceProvider.GetRequiredService<SearchDemo>();

            await cbe.Sync();
            await demo.Run();
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
