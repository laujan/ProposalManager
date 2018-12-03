// Copyright(c) Microsoft Corporation. 
// All rights reserved.
//
// Licensed under the MIT license. See LICENSE file in the solution root folder for full license information.

using ApplicationCore.Helpers;
using ApplicationCore.Interfaces;
using ApplicationCore.Services;
using Infrastructure.Authorization;
using Infrastructure.DealTypeServices;
using Infrastructure.GraphApi;
using Infrastructure.Identity;
using Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;
using System.Threading.Tasks;

namespace OneDriveSubscriptionRenewal
{
	internal class Program
	{
		static void Main(string[] args)
		{
			Console.WriteLine("Starting WebJob");

			ExecuteAsync().GetAwaiter().GetResult();

			Console.WriteLine("Finished executing WebJob");
		}

		private static async Task ExecuteAsync()
		{
			IServiceCollection serviceCollection = new ServiceCollection();
			ConfigureServices(serviceCollection);
			await serviceCollection.BuildServiceProvider().GetService<IOneDriveLinkService>().RenewAllSubscriptionsAsync();
		}

		private static void ConfigureServices(IServiceCollection serviceCollection)
		{
			var configuration = new ConfigurationBuilder()
				.SetBasePath(Directory.GetCurrentDirectory())
				.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
				.Build();

			serviceCollection.AddSingleton<IConfiguration>(configuration);
			serviceCollection.AddTransient<IUserContext, UserIdentityContext>();
			serviceCollection.AddSingleton<IGraphAuthProvider, GraphAuthProvider>();
			serviceCollection.AddScoped<IGraphClientAppContext, GraphClientAppContext>();
			serviceCollection.AddScoped<IGraphClientUserContext, GraphClientUserContext>();
			serviceCollection.AddScoped<IOpportunityFactory, OpportunityFactory>();
			serviceCollection.AddScoped<IOpportunityRepository, OpportunityRepository>();
			serviceCollection.AddSingleton<IOneDriveLinkService, OneDriveLinkService>();
			serviceCollection.AddScoped<IUserProfileRepository, UserProfileRepository>();
			serviceCollection.AddScoped<IRoleMappingRepository, RoleMappingRepository>();
			serviceCollection.AddScoped<CardNotificationService>();
			serviceCollection.AddScoped<CheckListProcessService>();
			serviceCollection.AddScoped<CustomerDecisionProcessService>();
			serviceCollection.AddScoped<GraphSharePointAppService>();
			serviceCollection.AddScoped<GraphSharePointUserService>();
			serviceCollection.AddScoped<ProposalDocumentProcessService>();
			serviceCollection.AddScoped<NewOpportunityProcessService>();
			serviceCollection.AddScoped<StartProcessService>();
			serviceCollection.AddScoped<IAuthorizationService, AuthorizationService>();
			serviceCollection.AddScoped<IDashboardService, DashboardService>();
			serviceCollection.AddScoped<IPermissionRepository, PermissionRepository>();
			serviceCollection.AddScoped<UserProfileHelpers>();
		}
	}
}