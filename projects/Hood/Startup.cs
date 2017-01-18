﻿using Hood.Extensions;
using Hood.Filters;
using Hood.Infrastructure;
using Hood.Models;
using Hood.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Serialization;
using System;
using System.Globalization;
using Hood.IO;

namespace Hood
{
    public static class HoodStartup
    {
        /// <summary>
        /// Generate the config file for use with HoodCMS.      
        /// </summary>
        /// <param name="env">The current IHostingEnvironment. Standard parameter of Startup().</param>
        /// <param name="addCustomSettings"></param>
        public static IConfigurationRoot Config(IHostingEnvironment env, Action<IConfigurationBuilder> addCustomSettings = null)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true);

            addCustomSettings(builder);

            if (env.IsEnvironment("Development") || env.IsEnvironment("PreProduction") || env.IsEnvironment("ErrorTesting"))
            {
                // For more details on using the user secret store see http://go.microsoft.com/fwlink/?LinkID=532709
                builder.AddUserSecrets();
                builder.AddApplicationInsightsSettings(developerMode: true);
            }

            builder.AddJsonFile("connection.json", optional: true, reloadOnChange: true);
            builder.AddEnvironmentVariables();

            IConfigurationRoot config = builder.Build();

            // Check required app settings
            config.ConfigSetup("Installed:DB", "Data:ConnectionString");

            config.ConfigSetup("Installed:ApplicationInsights", "ApplicationInsights:Key");

            // Set the installed flag
            config.ConfigSetup("Installed", "Installed:DB");

            // Check optional app settings
            config.ConfigSetup("Installed:Facebook", "Authentication:Facebook:AppId", "Authentication:Facebook:Secret");
            config.ConfigSetup("Installed:Google", "Authentication:Google:AppId", "Authentication:Google:Secret");

            return config;
        }

        /// <summary>
        /// Configure Services for the application to run HoodCMS. 
        /// </summary>
        /// <param name="config">The site's conficuration object. In a default ASP.NET application template this is called Configuration.</param>
        /// <param name="services">The IServiceCollection. Standard parameter of Startup.ConfigureServices().</param>
        public static void ConfigureServices(IServiceCollection services, IConfigurationRoot config)
        {
            services.AddMvc();

            services.Configure<RazorViewEngineOptions>(options =>
            {
                options.FileProviders.Add(EmbeddedFiles.GetProvider());
                options.ViewLocationExpanders.Add(new ViewLocationExpander());
            });

            // Add framework services.
            if (config.CheckSetup("Installed:DB"))
            {
                services.AddDbContext<HoodDbContext>(options => options.UseSqlServer(config["Data:ConnectionString"], b => { b.UseRowNumberForPaging(); }));
            

                services.AddSingleton<ContentCategoryCache>();

                services.AddIdentity<ApplicationUser, IdentityRole>(o =>
                {
                    // configure identity options
                    o.Password.RequireDigit = config["Identity:Password:RequireDigit"].IsSet() ? bool.Parse(config["Identity:Password:RequireDigit"]) : true;
                    o.Password.RequireLowercase = config["Identity:Password:RequireLowercase"].IsSet() ? bool.Parse(config["Identity:Password:RequireLowercase"]) : false;
                    o.Password.RequireUppercase = config["Identity:Password:RequireUppercase"].IsSet() ? bool.Parse(config["Identity:Password:RequireUppercase"]) : false;
                    o.Password.RequireNonAlphanumeric = config["Identity:Password:RequireNonAlphanumeric"].IsSet() ? bool.Parse(config["Identity:Password:RequireNonAlphanumeric"]) : true;
                    o.Password.RequiredLength = config["Identity:Password:RequiredLength"].IsSet() ? int.Parse(config["Identity:Password:RequiredLength"]) : 6;
                })
                .AddEntityFrameworkStores<HoodDbContext>()
                .AddDefaultTokenProviders();

                services.AddSingleton<IRazorViewRenderer, RazorViewRenderer>();
                services.AddSingleton<IFTPService, FTPService>();
                services.AddSingleton<IPropertyImporter, PropertyImporter>();
                services.AddSingleton<IPropertyExporter, PropertyExporter>();
                services.AddSingleton<IContentExporter, ContentExporter>();
                services.AddSingleton<IThemesService, ThemesService>();
                services.AddScoped<ISiteConfiguration, SiteConfiguration>();
                services.AddScoped<IAuthenticationRepository, AuthenticationRepository>();
                services.AddScoped<IPropertyRepository, PropertyRepository>();
                services.AddScoped<IMediaManager<SiteMedia>, MediaManager<SiteMedia>>();
                services.AddScoped<IContentRepository, ContentRepository>();
                services.AddScoped<ISubscriptionRepository, SubscriptionRepository>();
                services.AddTransient<ISmsSender, SmsSender>();
                services.AddTransient<IPayPalService, PayPalService>();
                services.AddTransient<IShoppingCart, ShoppingCart>();
                services.AddTransient<IStripeService, StripeService>();
                services.AddTransient<ISubscriptionPlanService, SubscriptionPlanService>();
                services.AddTransient<ISubscriptionService, SubscriptionService>();
                services.AddTransient<ICardService, CardService>();
                services.AddTransient<ICustomerService, CustomerService>();
                services.AddTransient<IInvoiceService, InvoiceService>();
                services.AddTransient<IBillingService, BillingService>();
                services.AddTransient<IEmailSender, EmailSender>();

                services.Configure<RouteOptions>(options =>
                {
                    options.ConstraintMap.Add("cms", typeof(CmsUrlConstraint));
                    options.LowercaseUrls = true;
                    options.AppendTrailingSlash = true;
                });

                services.Configure<MvcJsonOptions>(opt =>
                {
                    var resolver = opt.SerializerSettings.ContractResolver;
                    if (resolver != null)
                    {
                        var res = resolver as DefaultContractResolver;
                        res.NamingStrategy = null;  // <<!-- this removes the camelcasing
                    }
                });

                services.Configure<MvcOptions>(options =>
                {
                    // Global filters
                    options.Filters.Add(typeof(AccountFilter));
                    options.CacheProfiles.Add("Year",
                        new CacheProfile
                        {
                            Location = ResponseCacheLocation.Client,
                            Duration = 31536000
                        });
                    options.CacheProfiles.Add("Month",
                        new CacheProfile
                        {
                            Location = ResponseCacheLocation.Client,
                            Duration = 2629000
                        });
                    options.CacheProfiles.Add("Week", 
                        new CacheProfile
                        {
                            Location = ResponseCacheLocation.Client,
                            Duration = 604800
                        });
                    options.CacheProfiles.Add("Day",
                        new CacheProfile
                        {
                            Location = ResponseCacheLocation.Client,
                            Duration = 86400
                        });
                    options.CacheProfiles.Add("Hour",
                        new CacheProfile
                        {
                            Location = ResponseCacheLocation.Client,
                            Duration = 3600
                        });
                    options.CacheProfiles.Add("HalfHour",
                         new CacheProfile
                         {
                             Location = ResponseCacheLocation.Client,
                             Duration = 1800
                         });
                    options.CacheProfiles.Add("TenMinutes",
                         new CacheProfile
                         {
                             Location = ResponseCacheLocation.Client,
                             Duration = 600
                         });

                });

            }

            services.AddDistributedMemoryCache();
            services.AddSession();

            services.AddSingleton<IConfiguration>(config);

            services.AddApplicationInsightsTelemetry(config);
        }

        /// <summary>
        /// Configure the application to run HoodCMS. 
        /// </summary>
        /// <param name="app">The IApplicationBuilder. Standard parameter of Startup.ConfigureServices().</param>
        /// <param name="env">The IHostingEnvironment. Standard parameter of Startup.ConfigureServices().</param>
        /// <param name="loggerFactory">The ILoggerFactory. Standard parameter of Startup.ConfigureServices().</param>
        /// <param name="config">The IConfigurationRoot. Standard parameter of Startup.ConfigureServices().</param>
        /// <param name="customRoutes">Routes to add to the sites route map. These will be added after the standard HoodCMS routes, but before the standard catch all route {area:exists}/{controller=Home}/{action=Index}/{id?}.</param>
        public static void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory, IConfigurationRoot config, Action<IRouteBuilder> customRoutes = null)
        {
            CultureInfo.DefaultThreadCurrentCulture = CultureInfo.GetCultureInfo("en-GB");
            CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.GetCultureInfo("en-GB");

            app.UseApplicationInsightsRequestTelemetry();

            if (env.IsEnvironment("Development") || env.IsEnvironment("PreProduction") || env.IsEnvironment("Staging"))
            {
                loggerFactory.AddDebug(LogLevel.Debug);
                loggerFactory.AddConsole(LogLevel.Debug);
                app.UseDeveloperExceptionPage();
                app.UseDatabaseErrorPage();
            }
            else
            {
                app.UseExceptionHandler("/error");
                app.UseStatusCodePagesWithReExecute("/error/{0}");
            }

            if (config.CheckSetup("Installed:DB"))
            {

                // For more details on creating database during deployment see http://go.microsoft.com/fwlink/?LinkID=615859
                using (var serviceScope = app.ApplicationServices.GetRequiredService<IServiceScopeFactory>().CreateScope())
                {
                    serviceScope.ServiceProvider.GetService<HoodDbContext>().Database.Migrate();
                    var userManager = app.ApplicationServices.GetService<UserManager<ApplicationUser>>();
                    var roleManager = app.ApplicationServices.GetService<RoleManager<IdentityRole>>();
                    serviceScope.ServiceProvider.GetService<HoodDbContext>().EnsureSetup(userManager, roleManager);
                }
            }

            app.UseStaticFiles(new StaticFileOptions()
            {
                OnPrepareResponse =
                    r =>
                    {
                        string path = r.File.PhysicalPath;
                        if (path.EndsWith(".css") || path.EndsWith(".js") || path.EndsWith(".gif") || path.EndsWith(".jpg") || path.EndsWith(".png") || path.EndsWith(".svg"))
                        {
                            TimeSpan maxAge = new TimeSpan(7, 0, 0, 0);
                            r.Context.Response.Headers.Append("Cache-Control", "max-age=" + maxAge.TotalSeconds.ToString("0"));
                        }
                    }
            });

            // Activate url helpers
            var httpContextAccessor = app.ApplicationServices.GetRequiredService<IHttpContextAccessor>();
            UrlHelpers.Configure(httpContextAccessor);

            if (config.CheckSetup("Installed:DB"))
            {
                app.UseIdentity();
            }

            app.UseSession();

            if (!config.CheckSetup("Installed"))
            {
               app.UseMvc(routes =>
                {
                    routes.MapRoute(
                        name: "SiteNotInstalled",
                        template: "{*url}",
                        defaults: new { controller = "Install", action = "Install" }
                    );
                });
            }
            else
            {
                app.UseMvc(routes =>
                {

                    //routes.MapHoodMaintenanceRoute(
                    //    name: "MaintenancePage",
                    //    template: "{*url}",
                    //    defaults: new { controller = "Home", action = "Maintenance" });

                    //routes.MapHoodHoldingRoute(
                    //    name: "HoldingPage",
                    //    template: "{*url}",
                    //    defaults: new { controller = "Home", action = "Holding" });

                    routes.MapRoute(
                        name: "ContentCheck",
                        template: "{lvl1:cms}/{lvl2:cms?}/{lvl3:cms?}/{lvl4:cms?}/{lvl5:cms?}",
                        defaults: new { controller = "Content", action = "Show" });

                    customRoutes?.Invoke(routes);

                    routes.MapRoute(
                        name: "areaRoute",
                        template: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

                    routes.MapRoute(
                        name: "default",
                        template: "{controller=Home}/{action=Index}/{id?}");

                });
            }
        }

    }
}