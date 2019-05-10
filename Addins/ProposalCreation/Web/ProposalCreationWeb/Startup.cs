// Copyright(c) Microsoft Corporation. 
// All rights reserved.
//
// Licensed under the MIT license. See LICENSE file in the solution root folder for full license information.

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ProposalCreation.Core.Extensions;
using ProposalCreation.Core.Helpers;
using ProposalCreation.Core.Interfaces;
using ProposalCreation.Core.Providers;
using System.Globalization;

namespace ProposalCreationWeb
{
    public class Startup
	{
		public Startup(IConfiguration configuration) => Configuration = configuration;

		public IConfiguration Configuration { get; }
		public const string ObjectIdentifierType = "http://schemas.microsoft.com/identity/claims/objectidentifier";
		public const string TenantIdType = "http://schemas.microsoft.com/identity/claims/tenantid";

		// This method gets called by the runtime. Use this method to add services to the container.
		public void ConfigureServices(IServiceCollection services)
		{
			// User Bearer token as user is authenticated in the client
			services.AddAuthentication(sharedOptions =>
			{
				sharedOptions.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
			})
		   .AddAzureAdBearer(options => Configuration.Bind("AzureAd", options));

			services.AddCors(options =>
			{
				options.AddPolicy("AllowProposalManager",
				builder =>
				{
					builder.WithOrigins("http://localhost:50262")
						.AllowAnyHeader()
						.AllowAnyMethod()
						.AllowCredentials();
				});
			});

			services.AddMvc();

			// This sample uses an in-memory cache for tokens and subscriptions. Production apps will typically use some method of persistent storage.
			services.AddMemoryCache();
			services.AddSession();

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

			// Add application services.
			services.AddSingleton<IGraphAuthProvider, GraphAuthProvider>();
			services.AddTransient<IGraphSdkHelper, GraphSdkHelper>();
			services.AddSingleton<IDaemonHelper, DaemonHelper>();
			services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
			services.AddSingleton(typeof(IConventionBasedConfigurationProvider<>), typeof(ConventionBasedConfigurationProvider<>));
			services.AddSingleton<IRootConfigurationProvider, RootConfigurationProvider>();
            services.AddSingleton<ITaskProvider, TaskProvider>();

            services.AddLogging(logging =>
            {
                logging.AddConsole();
                logging.AddDebug();
            });
        }

		// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
		public static void Configure(IApplicationBuilder app, IHostingEnvironment env)
		{
			if (env.IsDevelopment())
			{
				app.UseDeveloperExceptionPage();
			}
			else
			{
				app.UseExceptionHandler("/Home/Error");
			}

			app.UseRequestLocalization(new RequestLocalizationOptions()
			{

			});

			app.UseStaticFiles();

			app.UseSession();

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