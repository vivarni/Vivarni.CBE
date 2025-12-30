using Microsoft.EntityFrameworkCore;
using Serilog;
using Vivarni.CBE.Postgres.Setup;
using Vivarni.CBE.Samples.Common;
using Vivarni.CBE.Samples.Web.Components;
using Vivarni.CBE.Samples.Web.Elastic;

namespace Vivarni.CBE.Samples.Web;

public static class Program
{
    public static void Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .Enrich.WithProperty("InstanceId", Guid.NewGuid().ToString("n"))
            .WriteTo.Console()
            .CreateLogger();

        try
        {
            var builder = WebApplication.CreateBuilder(args);

            // Replace Microsoft.Extensions.Logging providers with Serilog
            builder.Host.UseSerilog((ctx, services, cfg) => cfg
                .ReadFrom.Configuration(ctx.Configuration)
                .ReadFrom.Services(services)
                .WriteTo.Console()
            );

            // --------------------------------------------------------------------------------
            // Add Entity Framework DbContext and ElasticSearch repository services

            var connectionString = builder.Configuration.GetConnectionString("postgres")!;
            var userName = builder.Configuration["cbe:http-login"] ?? string.Empty;
            var password = builder.Configuration["cbe:http-password"] ?? string.Empty;

            builder.Services
                .AddScoped<IElasticRepository, ElasticRepository>()
                .AddSingleton<IndexationProgressService>()
                .AddDbContext<SearchDbContext>(o => o.UseNpgsql(connectionString))
                .AddRazorComponents()
                    .AddInteractiveServerComponents();

            // --------------------------------------------------------------------------------
            // Configure Vivarni.CBE

            builder.Services.AddVivarniCBE(s => s
                .UsePostgres(connectionString, new() { Schema = SearchDbContext.SCHEMA_NAME })
                .UseHttpSource(userName, password)
                .UseFileSystemCache("c:/temp/kbo-cache"));

            // --------------------------------------------------------------------------------
            // Start Blazor

            var app = builder.Build();

            app.UseHsts();
            app.UseSerilogRequestLogging();
            app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
            app.UseHttpsRedirection();
            app.UseAntiforgery();
            app.MapStaticAssets();
            app.MapRazorComponents<App>().AddInteractiveServerRenderMode();
            app.Run();
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
