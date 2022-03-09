using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Wikimedia.Utilities.Interfaces;
using Wikimedia.Utilities.Services;
using WikipediaReferences.Console.Services;
using WikipediaReferences.Console.UI;

namespace WikipediaReferences.Console
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
            services.AddSingleton<System.Net.Http.HttpClient>();
            services.AddSingleton<System.Net.WebClient>();
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
