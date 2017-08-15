using frontend.Migrations;
using frontend.Models;
using frontend.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NLog;
using NLog.Extensions.Logging;
using NLog.Web;

namespace frontend
{
    public class Startup
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        
        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();
            Configuration = builder.Build();

            logger.Info("Migrating database");
            using (var db = new Context())
            {
                db.Database.Migrate();
            }
        }

        public IConfigurationRoot Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            logger.Info("Configuring services");
            // Add framework services.
            services.AddMvc();
            services.AddSingleton(new AppState());
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            logger.Info("Configuring application");

            loggerFactory.AddNLog();

            app.UseDefaultFiles();
            app.UseStaticFiles();
            app.UseMvc();
            app.AddNLogWeb();
        }
    }
}
