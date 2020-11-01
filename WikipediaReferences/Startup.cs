using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WikipediaReferences.Data;
using WikipediaReferences.Interfaces;
using WikipediaReferences.Services;

namespace WikipediaReferences
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
            string webApiConnectionString = Configuration.GetConnectionString("WikipediaReferencesDBConnection");

            Action<DbContextOptionsBuilder> optionActionCreator(string connectionString)
            {
                return options => options.UseSqlServer(connectionString);
            }

            services.AddDbContext<WRContext>(optionActionCreator(webApiConnectionString));
            services.AddScoped< IWikipediaService, WikipediaService>();
            services.AddScoped<INYTimesService, NYTimesService>();
            services.AddControllers();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
