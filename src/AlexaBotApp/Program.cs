using AlexaBotApp.Infrastructure;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Events;
using System;

namespace AlexaBotApp
{
    public class Program
    {
        public static int Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .WriteTo.File(
                $@"D:\home\LogFiles\{typeof(Program).Assembly.GetName().Name}-.log",
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 15,
                shared: true,
                flushToDiskInterval: TimeSpan.FromSeconds(1))
            .CreateLogger();

            try
            {
                Log.Information("Creating web host...");
                var builder = CreateWebHostBuilder(args).Build();

                Log.Information("Migrating database...");
                using (var scope = builder.Services.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetService<HumanLearningDbContext>();
                    dbContext.Database.Migrate();
                }

                Log.Information("Starting web host...");
                builder.Run();

                return 0;
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Host terminated unexpectedly");
                return 1;
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>()
                .UseSerilog();
    }
}
