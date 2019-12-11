// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
//
// Generated with Bot Builder V4 SDK Template for Visual Studio EmptyBot v4.6.2

using AlexaBotApp.Adapters;
using AlexaBotApp.Bots;
using AlexaBotApp.Commands;
using AlexaBotApp.CommandHandlers;
using AlexaBotApp.Contracts;
using AlexaBotApp.Infrastructure;
using AlexaBotApp.Metrics;
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

            // Create the Bot Framework Adapter with error handling enabled.
            services.AddSingleton<IAdapterIntegration, BotAdapterWithErrorHandler>();
            services.AddSingleton<IBotFrameworkHttpAdapter, AlexaAdapterWithErrorHandler>();

            services.AddSingleton(sp => 
            {
                var environment = sp.GetRequiredService<IHostingEnvironment>();
                var logFolder = Path.GetFullPath(Path.Combine(environment.ContentRootPath, $"../../object-logs/"));

                return new ObjectLogger(logFolder);
            });

            services.AddSingleton<IStorage, MemoryStorage>();
            services.AddSingleton<UserState>();
            services.AddSingleton<BotStateAccessors>();

            services.AddSingleton<BotConversation>();

            // Create the bot as a transient. In this case the ASP Controller is expecting an IBot.
            services.AddTransient<IBot, AlexaBot>();

            services.AddDbContext<SpeechTherapyDbContext>(builder =>
            {
                builder.UseSqlServer(Configuration.GetConnectionString("DefaultConnection"));
            });

            services.AddTransient<ICommandHandler<CreatePhraseExerciseCommand, PhraseExercise>, CreatePhraseExerciseCommandHandler>();
            services.AddTransient<ICommandHandler<RegisterUtteranceCommand, PhraseExercise>, RegisterUtteranceCommandHandler>();
            services.AddTransient<ICommandHandler<EndExerciseCommand, PhraseExercise>, EndExerciseCommandHandler>();
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
