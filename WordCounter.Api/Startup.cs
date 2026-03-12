using System;
using Microsoft.Extensions.DependencyInjection;
using WordCounter.Api.Interfaces;
using WordCounter.Api.Services;
using WordCounter.Domain.Interfaces;
using WordCounter.Domain.Models;
using WordCounter.Infrastructure.Counters;
using WordCounter.Infrastructure.Factories;
using WordCounter.Infrastructure.Providers;

namespace WordCounter.Api
{
    public class Startup
    {
        public IConfiguration Configuration { get; }

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            var settings = new WordCounterSettings();
            Configuration.GetSection("WordCounterSettings").Bind(settings);
            services.AddSingleton(settings);

            services.Configure<WordCounterSettings>(Configuration.GetSection("WordCounterSettings"));

            services.AddControllers();
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen();

            services.AddHttpClient(string.Empty, client =>
            {
                client.Timeout = TimeSpan.FromSeconds(settings.TimeoutSeconds);
                client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36");
            });
            services.AddSingleton<IContentProvider, HttpContentProvider>();
            services.AddSingleton<IWordCounterFactory, WordCounterFactory>();

            services.AddTransient<HtmlWordCounter>();
            services.AddTransient<JsonWordCounter>();
            services.AddTransient<XmlWordCounter>();

            services.AddSingleton<IWordCountingService, WordCountingService>();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
