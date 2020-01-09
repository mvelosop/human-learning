using AlexaBotApp.Adapters;
using AlexaBotApp.Bots;
using AlexaBotApp.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Integration;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.IO;
using MediatR;

namespace AlexaBotApp
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);

            // Bot adapters
            services.AddSingleton<IAdapterIntegration, BotAdapterWithErrorHandler>();
            services.AddSingleton<IBotFrameworkHttpAdapter, AlexaAdapterWithErrorHandler>();

            // Object logger
            services.AddSingleton(sp =>
            {
                var environment = sp.GetRequiredService<IHostingEnvironment>();
                var logFolder = Path.GetFullPath(Path.Combine(environment.ContentRootPath, $"../../object-logs/"));

                return new ObjectLogger(environment.EnvironmentName, logFolder);
            });

            // Bot state
            services.AddSingleton<IStorage, MemoryStorage>();
            services.AddSingleton<UserState>();
            services.AddSingleton<BotStateAccessors>();
            // Conversation reference temporal store
            services.AddSingleton<BotConversation>();

            // Bots
            services.AddTransient<AlexaBot>();
            services.AddTransient<MonitorBot>();

            // DbContext
            services.AddDbContext<SpeechTherapyDbContext>(builder =>
            {
                builder.UseSqlServer(Configuration.GetConnectionString("DefaultConnection"));
            });

            // Command processing pipeline
            services.AddMediatR(typeof(Startup).Assembly);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseHsts();
            }

            app.UseDefaultFiles();
            app.UseStaticFiles();
            app.UseWebSockets();
            app.UseMvc();
        }
    }
}
