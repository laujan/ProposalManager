// Copyright(c) Microsoft Corporation. 
// All rights reserved.
//
// Licensed under the MIT license. See LICENSE file in the solution root folder for full license information.

using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.SpaServices.ReactDevelopmentServer;
using Microsoft.Bot.Connector;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using ApplicationCore;
using ApplicationCore.Interfaces;
using Infrastructure.Identity;
using Infrastructure.Identity.Extensions;
using Infrastructure.GraphApi;
using Infrastructure.Services;
using ApplicationCore.Services;
using Infrastructure.OfficeApi;
using ApplicationCore.Helpers;
using Microsoft.AspNetCore.Authorization;
using Infrastructure.DealTypeServices;
using Infrastructure.Helpers;
using Infrastructure.Authorization;
using System.Collections.Generic;
using System.Linq;
using ApplicationCore.Models;
using System.Reflection;
using System.IO;
using System;
using Newtonsoft.Json.Linq;
using WebReact.ModelExamples;
using ApplicationCore.Interfaces.SmartLink;
using Infrastructure.Services.SmartLink;

using Microsoft.Extensions.Options;

namespace WebReact
{
    /// Startup class
    public class Startup
	{
        /// Startup constructor
		public Startup(IConfiguration configuration)
		{
			Configuration = configuration;
		}

        /// Configuration property
		public IConfiguration Configuration { get; }

		/// This method gets called by the runtime. Use this method to add services to the container.
		public void ConfigureServices(IServiceCollection services)
		{
            // Add CORS exception to enable authentication in Teams Client
            //services.AddCors(options =>
            //{
            //    options.AddPolicy("AllowSpecificOrigins",
            //    builder =>
            //    {
            //        builder.WithOrigins("https://webreact20180403042343.azurewebsites.net", "https://login.microsoftonline.com");
            //    });

            //    options.AddPolicy("AllowAllMethods",
            //        builder =>
            //        {
            //            builder.WithOrigins("https://webreact20180403042343.azurewebsites.net")
            //                   .AllowAnyMethod();
            //        });

            //    options.AddPolicy("ExposeResponseHeaders",
            //        builder =>
            //        {
            //            builder.WithOrigins("https://webreact20180403042343.azurewebsites.net")
            //                   .WithExposedHeaders("X-Frame-Options");
            //        });
            //});

            // Add in-mem cache service
            services.AddMemoryCache();

            // Credentials for bot authentication
            var credentialProvider = new StaticCredentialProvider(
                Configuration.GetSection("ProposalManagement:" + MicrosoftAppCredentials.MicrosoftAppIdKey)?.Value,
                Configuration.GetSection("ProposalManagement:" + MicrosoftAppCredentials.MicrosoftAppPasswordKey)?.Value);

            // Add authentication services
            services.AddAuthentication()
                .AddAzureAdBearer("AzureAdBearer", "AzureAdBearer for web api calls", options => Configuration.Bind("AzureAd", options))
                .AddBotAuthentication(credentialProvider);

            services.AddSingleton(typeof(ICredentialProvider), credentialProvider);

            // Add Authorization services
            services.AddScoped<Infrastructure.Authorization.IAuthorizationService, AuthorizationService>();

            // Add MVC services
            services.AddMvc(options =>
            {
                options.Filters.Add(typeof(TrustServiceUrlAttribute));
            })
            .SetCompatibilityVersion(CompatibilityVersion.Version_2_1)
			.AddDynamicsCRMWebHooks();


            // This sample uses an in-memory cache for tokens and subscriptions. Production apps will typically use some method of persistent storage.

			services.AddSession();

			// Register configuration options
			services.Configure<AppOptions>(Configuration.GetSection("ProposalManagement"));

			// Add application infrastructure services.
			services.AddSingleton<IGraphAuthProvider, GraphAuthProvider>(); // Auth provider for Graph client, must be singleton
            services.AddSingleton<IWebApiAuthProvider, WebApiAuthProvider>(); // Auth provider for WebApi calls, must be singleton
            services.AddScoped<IGraphClientAppContext, GraphClientAppContext>();
			services.AddScoped<IGraphClientUserContext, GraphClientUserContext>();
			services.AddTransient<IUserContext, UserIdentityContext>();
            services.AddScoped<IWordParser, WordParser>();

            // Add core services
            services.AddScoped<IOpportunityFactory, OpportunityFactory>();
			services.AddScoped<IOpportunityRepository, OpportunityRepository>();
            services.AddScoped<IUserProfileRepository, UserProfileRepository>();
			services.AddScoped<IDocumentRepository, DocumentRepository>();
			services.AddScoped<IRegionRepository, RegionRepository>();
			services.AddScoped<IIndustryRepository, IndustryRepository>();
			services.AddScoped<ICategoryRepository, CategoryRepository>();
			services.AddScoped<IRoleMappingRepository,RoleMappingRepository>();
            services.AddScoped<ITemplateRepository, TemplateRepository>();
            services.AddScoped<IProcessRepository, ProcessRepository>();
            services.AddScoped<GraphSharePointAppService>();
            services.AddScoped<SharePointListsSchemaHelper>();
			services.AddScoped<GraphSharePointUserService>();
			services.AddScoped<GraphTeamUserService>();
			services.AddScoped<GraphUserAppService>();
            services.AddScoped<GraphTeamsAppService>();
            services.AddScoped<IAddInHelper, AddInHelper>();

            // FrontEnd services
            services.AddScoped<IOpportunityService, OpportunityService>();
			services.AddScoped<IDocumentService, DocumentService>();
			services.AddScoped<IUserProfileService, UserProfileService>();
			services.AddScoped<IRegionService, RegionService>();
			services.AddScoped<IIndustryService, IndustryService>();
			services.AddScoped<IRoleMappingService, RoleMappingService>();
            services.AddScoped<IContextService, ContextService>();
            services.AddScoped<ICategoryService, CategoryService>();
			services.AddScoped<ISetupService, SetupService>();
            services.AddScoped<UserProfileHelpers>();
            services.AddScoped<TemplateHelpers>();
            services.AddScoped<OpportunityHelpers>();
            services.AddScoped<CardNotificationService>();
            services.AddScoped<ITemplateService, TemplateService>();
            services.AddScoped<IProcessService, ProcessService>();
            services.AddScoped<IRoleRepository, RoleRepository>();
            services.AddScoped<IRoleService, RoleService>();
            services.AddScoped<IPowerBIService, PowerBIService>();
            services.AddScoped<IPermissionRepository, PermissionRepository>();
            services.AddScoped<IPermissionService, PermissionService>();

            // DealType Services
            services.AddScoped<CheckListProcessService>();
            services.AddScoped<CustomerDecisionProcessService>();
            services.AddScoped<ProposalStatusProcessService>();
            services.AddScoped<NewOpportunityProcessService>();
            services.AddScoped<StartProcessService>();

            // SmartLink
            services.AddScoped<IDocumentIdService, DocumentIdService>();

			// Dynamics CRM Integration
			services.AddScoped<IDynamicsClientFactory, DynamicsClientFactory>();
			services.AddScoped<IProposalManagerClientFactory, ProposalManagerClientFactory>();
			services.AddScoped<IAccountRepository, AccountRepository>();
			services.AddScoped<IUserRepository, UserRepository>();
			services.AddScoped<IConnectionRoleRepository, ConnectionRoleRepository>();
			services.AddScoped<ISharePointLocationRepository, SharePointLocationRepository>();
			services.AddScoped<IOneDriveLinkService, OneDriveLinkService>();
			services.AddScoped<IDynamicsLinkService, DynamicsLinkService>();
			services.AddSingleton<IConnectionRolesCache, ConnectionRolesCache>();
			services.AddSingleton<ISharePointLocationsCache, SharePointLocationsCache>();
			services.AddSingleton<IDeltaLinksStorage, DeltaLinksStorage>();

			//Dashboars services
			services.AddScoped<IDashboardRepository, DashBoardRepository>();
            services.AddScoped<IDashboardService, DashboardService>();
            services.AddScoped<IDashboardAnalysis, DashBoardAnalysis>();
            // In production, the React files will be served from this directory
            services.AddSpaStaticFiles(configuration =>
			{
				configuration.RootPath = "ClientApp/build";
			});



            //Configure writing to appsettings
            services.ConfigureWritable<AppOptions>(Configuration.GetSection("ProposalManagement"));
            services.ConfigureWritable<DocumentIdActivatorConfiguration>(Configuration.GetSection("DocumentIdActivator"));

            // Register the Swagger generator, defining 1 or more Swagger documents
            //services.AddSwaggerGen(c =>
            //{
            //    c.SwaggerDoc("v1", new Info
            //    {
            //        Version = "v1",
            //        Title = "Proposal Manager API",
            //        Description = "APIs to do CURD operations in Sharepoint schema using Microsoft Graph APIS for Proposal Management App"
            //    });

            //    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            //    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            //    c.IncludeXmlComments(xmlPath);

            //    c.OperationFilter<AddFileParamTypesOperationFilter>();
            //    c.OperationFilter<AddResponseHeadersFilter>(); // [SwaggerResponseHeader]
            //    c.OperationFilter<AppendAuthorizeToSummaryOperationFilter>();

            //    c.AddSecurityDefinition("Bearer", new ApiKeyScheme { In = "header", Description = "Please enter JWT with Bearer into field", Name = "Authorization", Type = "apiKey" });
            //    c.AddSecurityRequirement(new Dictionary<string, IEnumerable<string>>
            //        {
            //            { "Bearer", Enumerable.Empty<string>() },
            //        }
            //    );

            //});

            //services.AddSwaggerExamplesFromAssemblyOf<RoleMappingModelExample>();
            // Register the Swagger generator, defining 1 or more Swagger documents

        }

        /// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
		{           
			// Add the console logger.
			loggerFactory.AddConsole(Configuration.GetSection("Logging"));
			loggerFactory.AddDebug();

			// Configure error handling middleware.
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                // Enable middleware to serve generated Swagger as a JSON endpoint.
                //app.UseSwagger();
                //app.UseSwaggerUI(c =>
                //{
                //    c.SwaggerEndpoint("/swagger/v1/swagger.json", "ProposalManager  V1");
                //});
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();

            // Add CORS policies
            //app.UseCors("ExposeResponseHeaders");

            // Add static files to the request pipeline.
            app.UseStaticFiles();
			app.UseSpaStaticFiles();

			// Add session to the request pipeline
			app.UseSession();

			// Add authentication to the request pipeline
			app.UseAuthentication();

			// Configure MVC routes
			app.UseMvc(routes =>
			{
				routes.MapRoute(
					name: "default",
					template: "{controller}/{action=Index}/{id?}");
			});

			app.UseSpa(spa =>
			{
				spa.Options.SourcePath = "ClientApp";

				if (env.IsDevelopment())
				{
					spa.UseReactDevelopmentServer(npmScript: "start");
				}
			});

            app.UseMvc();
        }
	}
    public static class ServiceCollectionExtensions
    {
        public static void ConfigureWritable<T>(
            this IServiceCollection services,
            IConfigurationSection section,
            string file = "appsettings.json") where T : class, new()
        {
            services.Configure<T>(section);
            services.AddTransient<IWritableOptions<T>>(provider =>
            {
                var environment = provider.GetService<IHostingEnvironment>();
                var options = provider.GetService<IOptionsMonitor<T>>();
                return new WritableOptions<T>(environment, options, section.Key, file);
            });
        }
    }
}
