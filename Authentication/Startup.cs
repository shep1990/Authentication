using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Authentication.Domain.Data;
using System.Reflection;
using Authentication.Domain.Model;
using IdentityServer4.Services;
using Authentication.Domain.Services;
using IdentityServer4.EntityFramework.DbContexts;
using Authentication.Data;
using Authetication.WebApiClient;
using Microsoft.Extensions.Logging;

namespace Authentication
{
    public class Startup
    {
        public Startup(IConfiguration configuration, ILoggerFactory loggerFactory)
        {
            Configuration = configuration;
            loggerFactory.AddLog4Net("log4net.config");
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddDbContext<PlatformDbContext>(options =>
                options.UseSqlServer(
                    Configuration.GetConnectionString("AuthenticationConnectionString"),
                    sqlServerOptionsAction: sqlOptions =>
                    {
                        sqlOptions.MigrationsAssembly(typeof(Startup).GetTypeInfo().Assembly.GetName().Name);
                        sqlOptions.EnableRetryOnFailure(maxRetryCount: 15, maxRetryDelay: TimeSpan.FromSeconds(30), errorNumbersToAdd: null);
                    }
                )
            );


            //todo: set up auth api url
            services.AddTransient(x => AuthWebApiClientFactory.Create(Configuration.GetSection("AuthApi").Value));

            services.AddIdentity<PlatformUser, PlatformRole>(options =>
            {
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(Convert.ToDouble(Configuration.GetSection("DefaultLockoutTimeSpanValue").Value));
                options.Lockout.MaxFailedAccessAttempts = Convert.ToInt32(Configuration.GetSection("MaxFailedAccessAttemptValue").Value);
            })
            .AddEntityFrameworkStores<PlatformDbContext>()
            .AddDefaultTokenProviders();

            services.Configure<DataProtectionTokenProviderOptions>(o => o.TokenLifespan = TimeSpan.FromMinutes(Convert.ToInt32(Configuration.GetSection("TokenLifeSpan").Value)));

            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);

            services.AddTransient<ILoginService<PlatformUser>, LoginService>();

            services.AddIdentityServer(x =>
            {
                x.IssuerUri = "null";
                x.Authentication.CookieLifetime = TimeSpan.FromHours(2);
            })
           .AddDeveloperSigningCredential()
           .AddAspNetIdentity<PlatformUser>()
           .AddConfigurationStore(options =>
           {
               options.ConfigureDbContext = builder => builder.UseSqlServer(Configuration.GetConnectionString("AuthenticationConnectionString"),
                   sqlServerOptionsAction: sqlOptions =>
                   {
                       sqlOptions.MigrationsAssembly(typeof(Startup).GetTypeInfo().Assembly.GetName().Name);
                        //Configuring Connection Resiliency: https://docs.microsoft.com/en-us/ef/core/miscellaneous/connection-resiliency 
                        sqlOptions.EnableRetryOnFailure(maxRetryCount: 15, maxRetryDelay: TimeSpan.FromSeconds(30), errorNumbersToAdd: null);
                   });
           })
           .AddOperationalStore(options =>
           {
               options.ConfigureDbContext = builder => builder.UseSqlServer(Configuration.GetConnectionString("AuthenticationConnectionString"),
                   sqlServerOptionsAction: sqlOptions =>
                   {
                       sqlOptions.MigrationsAssembly(typeof(Startup).GetTypeInfo().Assembly.GetName().Name);
                        //Configuring Connection Resiliency: https://docs.microsoft.com/en-us/ef/core/miscellaneous/connection-resiliency 
                        sqlOptions.EnableRetryOnFailure(maxRetryCount: 15, maxRetryDelay: TimeSpan.FromSeconds(30), errorNumbersToAdd: null);
                   });
           })
           .Services.AddTransient<IProfileService, ProfileService>();

            services.AddCors(options =>
            {
                options.AddPolicy("CorsPolicy",
                    builder => builder.AllowAnyOrigin()
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials());
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {

            app.UseExceptionHandler("/Error/Exception");


            app.UseHsts();
            app.UseHttpsRedirection();

            app.UseIdentityServer();

            app.UseStaticFiles();
            app.UseAuthentication();

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Account}/{action=Login}/{id?}");
            });
            app.UseCors("CorsPolicy");


            ConfigureDb(app);
        }

        protected virtual void ConfigureDb(IApplicationBuilder app)
        {
            try
            {
                using (var scope = app.ApplicationServices.GetService<IServiceScopeFactory>().CreateScope())
                {
                    scope.ServiceProvider.GetRequiredService<PlatformDbContext>().Database.Migrate();
                    scope.ServiceProvider.GetRequiredService<PersistedGrantDbContext>().Database.Migrate();
                    scope.ServiceProvider.GetRequiredService<ConfigurationDbContext>().Database.Migrate();

                    new ConfigurationDbContextSeed()
                        .SeedAsync(scope.ServiceProvider.GetRequiredService<ConfigurationDbContext>(), Configuration)
                        .Wait();
                }
            }
            catch { }
        }
    }
}

