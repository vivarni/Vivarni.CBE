using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Vivarni.CBE.Postgres.Setup;
using Vivarni.CBE.Samples.Common;

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

            var connectionString = configuration.GetConnectionString("postgres")!;
            var loggerFactory = LoggerFactory.Create(builder => builder.AddSerilog());

            var serviceProvider = new ServiceCollection()
                // ------------------------------------------------------------------
                // Boilerplate setup for an application using EF Core
                //
                .AddLogging(builder => builder.AddSerilog()) // This line remains unchanged
                .AddSingleton<IConfiguration>(configuration)
                .AddSingleton(loggerFactory)
                .AddScoped<IElasticRepository, ElasticRepository>()
                .AddDbContext<SearchDbContext>(o => o
                    .UseNpgsql(connectionString)
                    .UseLoggerFactory(loggerFactory))
                .AddSingleton<ElasticDemo>()

                // ------------------------------------------------------------------
                // Configure Vivarni.CBE
                // 
                .AddVivarniCBE(s => s
                    .UsePostgres(connectionString, new() { Schema = SearchDbContext.SCHEMA_NAME })
                    .UseHttpSource(userName, password)
                    .UseFileSystemCache("c:/temp/kbo-cache"))

                // ------------------------------------------------------------------
                .BuildServiceProvider();

            var cbe = serviceProvider.GetRequiredService<ICbeService>();
            var demo = serviceProvider.GetRequiredService<ElasticDemo>();
            var repository = serviceProvider.GetRequiredService<IElasticRepository>();

            //await cbe.UpdateCbeDataAsync();
            //await repository.ReindexAllEnterprisesAsync();
            //await repository.ReindexEnterpriseAsync("0735.770.635");
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
