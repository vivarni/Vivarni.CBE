using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Vivarni.CBE.Samples.Common;
using Vivarni.CBE.SqlServer.Setup;

namespace Vivarni.CBE.Samples.Web;

internal class Program
{
    public static async Task Main()
    {
        // Configure Serilog for minimal output
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .MinimumLevel.Debug()
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore", Serilog.Events.LogEventLevel.Warning)
            .CreateLogger();

        try
        {
            var configuration = new ConfigurationBuilder()
                .AddJsonFile($"appsettings.json", true, true)
                .AddUserSecrets<Program>()
                .Build();

            var userName = configuration["cbe:http-login"] ?? string.Empty;
            var password = configuration["cbe:http-password"] ?? string.Empty;

            var connectionString = configuration.GetConnectionString("sql")!;
            var loggerFactory = LoggerFactory.Create(builder => builder.AddSerilog());

            var serviceProvider = new ServiceCollection()
                // ------------------------------------------------------------------
                // Boilerplate setup for an application using EF Core
                //
                .AddLogging(builder => builder.AddSerilog()) // This line remains unchanged
                .AddSingleton<IConfiguration>(configuration)
                .AddSingleton(loggerFactory)
                .AddDbContext<SearchDbContext>(o => o
                    .UseSqlServer(connectionString)
                    .UseLoggerFactory(loggerFactory))
                .AddSingleton<ConsoleDemo>()

                // ------------------------------------------------------------------
                // Configure Vivarni.CBE
                // 
                .AddVivarniCBE(s => s
                    .UseSqlServer(connectionString, schema: SearchDbContext.SCHEMA_NAME)
                    .UseHttpSource(userName, password)
                    .UseFileSystemCache("c:/temp/kbo-cache"))

                // ------------------------------------------------------------------
                .BuildServiceProvider();

            var cbe = serviceProvider.GetRequiredService<ICbeService>();
            var demo = serviceProvider.GetRequiredService<ConsoleDemo>();

            await cbe.UpdateCbeDataAsync();
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
