using Microsoft.EntityFrameworkCore;
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
            .MinimumLevel.Debug()
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

            var connectionString = GetConnectionString();
            var serviceProvider = new ServiceCollection()
                .AddLogging(builder => builder.AddSerilog())
                .AddSingleton<IConfiguration>(configuration)
                .AddDbContext<SearchDbContext>()
                .AddSingleton<SearchDemo>()
                .AddVivarniCBE(s => s
                    .UseSqlite(connectionString)
                    //.UseHttpSource(httpUser, httpPassword)
                    .UseFtpsSource(ftpUser, ftpPassword)
                    .UseFileSystemCache("c:/temp/kbo-cache"))
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

    private static string GetConnectionString()
    {
        var currentDirectory = Directory.GetCurrentDirectory();
        var solutionDirectory = currentDirectory;
        while (solutionDirectory != null && !Directory.GetFiles(solutionDirectory, "*.slnx").Any())
        {
            solutionDirectory = Directory.GetParent(solutionDirectory)?.FullName;
        }
        solutionDirectory ??= currentDirectory; // Fallback to current directory if .slnx not found
        var dbPath = Path.Combine(solutionDirectory, "kbo.db");

        return $"Data Source={dbPath}";
    }
}
