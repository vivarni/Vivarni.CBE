using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Vivarni.CBE.Postgres.Setup;

namespace Vivarni.CBE.Samples.PostgresEntityFrameworkCore;

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

            var httpUser = configuration["cbe:http-login"] ?? string.Empty;
            var httpPassword = configuration["cbe:http-password"] ?? string.Empty;

            var ftpUser = configuration["cbe:ftp-login"] ?? string.Empty;
            var ftpPassword = configuration["cbe:ftp-password"] ?? string.Empty;

            var connectionString = configuration.GetConnectionString("postgres");
            var loggerFactory = LoggerFactory.Create(builder => builder.AddSerilog());

            var serviceProvider = new ServiceCollection()
                // ------------------------------------------------------------------
                // Boilerplate setup for an application using EF Core
                //
                .AddLogging(builder => builder.AddSerilog()) // This line remains unchanged
                .AddSingleton<IConfiguration>(configuration)
                .AddSingleton(loggerFactory)
                .AddDbContext<SearchDbContext>(o => o
                    .UseNpgsql(connectionString, x => x.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery))
                    .UseLoggerFactory(loggerFactory))
                .AddSingleton<SearchDemo>()

                // ------------------------------------------------------------------
                // Configure Vivarni.CBE
                // 
                .AddVivarniCBE(s => s
                    .UsePostgres(connectionString, new() { Schema = SearchDbContext.SCHEMA_NAME, BinaryImporterBatchSize = 2_500_000 })
                    .UseHttpSource(httpUser, httpPassword)
                    //.UseFtpsSource(ftpUser, ftpPassword)
                    .UseFileSystemCache("c:/temp/kbo-cache"))

                // ------------------------------------------------------------------
                .BuildServiceProvider();

            var cbe = serviceProvider.GetRequiredService<ICbeService>();
            var demo = serviceProvider.GetRequiredService<SearchDemo>();

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
