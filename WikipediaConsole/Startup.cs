using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http;
using Wikimedia.Utilities.Interfaces;
using Wikimedia.Utilities.Services;
using WikipediaConsole.Services;
using WikipediaConsole.UI;

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
            services.AddScoped<ArticleAnalyzer>();
            services.AddScoped<AssemblyInfo>();            
            services.AddScoped<IToolforgeService, ToolforgeService>();
        }
    }
}
