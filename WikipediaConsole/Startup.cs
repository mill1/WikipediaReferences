using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Net.Http;
using WikipediaConsole.UI;
using WikipediaReferences.Interfaces;
using WikipediaReferences.Services;

namespace WikipediaConsole
{
    public class Startup
    {
        public IConfiguration Configuration { get; }
        public Startup()
        {
            var builder = new ConfigurationBuilder()
                .AddJsonFile($"appsettings.json", true, true);

            Configuration = builder.Build();
        }

        public void ConfigureServices(IServiceCollection services)
        {           
            services.AddSingleton(Configuration);
            services.AddSingleton<HttpClient>();
            services.AddSingleton<Util>();
            services.AddSingleton<Runner>();
            services.AddScoped<ListArticleGenerator>();
            services.AddScoped<ReferencesEditor>();
            services.AddScoped<AssemblyInfo>();
        }
    }
}
