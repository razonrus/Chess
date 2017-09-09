﻿namespace Chess.Site
{
    using Dal;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;

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
            services.Configure<DbConnectionOption>(Configuration.GetSection("DbConnection"));
            services.AddTransient<SessionFactory>();

            services.AddMvc();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, SessionFactory sessionFactory)
        {
            CreateDateBaseIfNotExist(sessionFactory);

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseBrowserLink();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }

            app.UseStaticFiles();

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });
        }

        private void CreateDateBaseIfNotExist(SessionFactory sessionFactory)
        {
            sessionFactory.Execute(s =>
            {
                int tableCount =
                    s.ExecuteScalar<int>("select count(*) from sqlite_master as tables where type='table'");
                if (tableCount == 0)
                {
                    //TODO код создания таблиц
                }
            });
        }
    }
}