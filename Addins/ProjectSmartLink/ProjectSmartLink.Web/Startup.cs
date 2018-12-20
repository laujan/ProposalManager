// Copyright(c) Microsoft Corporation. 
// All rights reserved.
//
// Licensed under the MIT license. See LICENSE file in the solution root folder for full license information.

using AutoMapper;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ProjectSmartLink.Service;
using ProjectSmartLink.Web.Extensions;
using ProjectSmartLink.Web.Helpers;
using ProjectSmartLink.Web.Mappings;
using System.Globalization;

namespace ProjectSmartLink
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
            services.Configure<CookiePolicyOptions>(options =>
            {
                // This lambda determines whether user consent for non-essential cookies is needed for a given request.
                options.CheckConsentNeeded = context => true;
                options.MinimumSameSitePolicy = SameSiteMode.None;
            });

			// User Bearer token as user is authenticated in the client
			services.AddAuthentication(sharedOptions =>
			{
				sharedOptions.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
			})
		   .AddAzureAdBearer(options => Configuration.Bind("AzureAd", options));

			services.AddDbContext<SmartlinkDbContext>(
				options =>
				{
					options.UseSqlServer(Configuration.GetSection("ConnectionStrings:DefaultConnection:ConnectionString").Value);
				},
				ServiceLifetime.Scoped
			);

			// Register AutoMapper
			var mapperConfiguration = new MapperConfiguration(cfg =>
			{
				cfg.AddProfile(new MappingProfile());
				cfg.AddProfile(new ServiceMappingProfile());
				//This list is keep on going...

			});
			var mapper = mapperConfiguration.CreateMapper();

			services.AddSingleton(mapperConfiguration.CreateMapper());

            services.AddLocalization(options => options.ResourcesPath = "Resources");
            services.Configure<RequestLocalizationOptions>(options =>
            {
                var supportedCultures = new[]
                {
                    new CultureInfo("en-US"),
                    new CultureInfo("es-AR")
                };

                options.DefaultRequestCulture = new RequestCulture("en-US");
                options.SupportedCultures = supportedCultures;
                options.SupportedUICultures = supportedCultures;
            });

            services.AddSingleton<IGraphAuthProvider, GraphAuthProvider>();
			services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
			services.AddSingleton<ILogService, LogService>();
			services.AddSingleton<IConfigService, ConfigService>();
			services.AddTransient<ISourceService, SourceService>();
			services.AddTransient<IDestinationService, DestinationService>();
			services.AddTransient<IRecentFileService, RecentFileService>();
			services.AddTransient<IUserProfileService, UserProfileService>();
			services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1)
				.AddJsonOptions(options =>
				{
					options.SerializerSettings.ContractResolver= new Newtonsoft.Json.Serialization.DefaultContractResolver();
				});
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
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseCookiePolicy();
			app.UseAuthentication();

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
