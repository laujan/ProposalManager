// Copyright(c) Microsoft Corporation. 
// All rights reserved.
//
// Licensed under the MIT license. See LICENSE file in the solution root folder for full license information.

using Autofac;
using AutoMapper;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Azure;
using Microsoft.Azure.WebJobs;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.SqlServer.Management.AlwaysEncrypted.AzureKeyVaultProvider;
using Smartlink.WebJob;
using SmartLink.Service;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Net;
using System.Threading.Tasks;

namespace SmartLink.WebJob
{
    // To learn more about Microsoft Azure WebJobs SDK, please see https://go.microsoft.com/fwlink/?LinkID=320976
    class Program
    {
        static ClientCredential _clientCredential;

        // Please set the following connection strings in app.config for this WebJob to run:
        // AzureWebJobsDashboard and AzureWebJobsStorage
        static void Main()
        {
            try
            {
                // Set the maximum number of concurrent connections
                ServicePointManager.DefaultConnectionLimit = 1;
                TelemetryConfiguration.Active.InstrumentationKey = CloudConfigurationManager.GetSetting("InstrumentationKey");

                var builder = new ContainerBuilder();

                builder.RegisterType<MessageWorker>().InstancePerDependency();
                builder.RegisterType<SourceService>().As<ISourceService>().InstancePerDependency();
                builder.RegisterType<DestinationService>().As<IDestinationService>().InstancePerDependency();
                builder.RegisterType<RecentFileService>().As<IRecentFileService>().InstancePerDependency();
                builder.RegisterType<SmartlinkDbContext>().AsSelf().InstancePerDependency();
                builder.RegisterType<ConfigService>().As<IConfigService>().SingleInstance();
                builder.RegisterType<AzureStorageService>().As<IAzureStorageService>().SingleInstance();
                builder.RegisterType<LogService>().As<ILogService>().SingleInstance();
                builder.RegisterType<MailService>().As<IMailService>().SingleInstance();
                builder.RegisterType<UserProfileService>().As<IUserProfileService>().InstancePerDependency();
                builder.RegisterType<DocumentService>().As<IDocumentService>().InstancePerDependency();
                var mapperConfiguration = new MapperConfiguration(cfg =>
                {
                    cfg.AddProfile(new ServiceMappingProfile());
                    //This list is keep on going...

                });
                var mapper = mapperConfiguration.CreateMapper();
                builder.RegisterInstance(mapper).As<IMapper>().SingleInstance();

                var container = builder.Build();
                try
                {
                    InitializeAzureKeyVaultProvider();

                    var config = new JobHostConfiguration()
                    {
                        DashboardConnectionString = container.Resolve<IConfigService>().AzureWebJobDashboard,
                        StorageConnectionString = container.Resolve<IConfigService>().AzureWebJobsStorage,
                        JobActivator = new AutofacJobActivator(container)
                    };
                    config.Queues.BatchSize = 1;
                    var host = new JobHost(config);
                    Console.Out.WriteLineAsync("Smartlink.WebJob is running");
                    host.RunAndBlock();
                }
                catch (Exception ex)
                {
                    throw ex;
                }

            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.ToString());
            }
        }


        static void InitializeAzureKeyVaultProvider()
        {
            string clientId = CloudConfigurationManager.GetSetting("ida:ClientId");
            string clientSecret = CloudConfigurationManager.GetSetting("ida:ClientSecret");
            _clientCredential = new ClientCredential(clientId, clientSecret);
            SqlColumnEncryptionAzureKeyVaultProvider azureKeyVaultProvider = new SqlColumnEncryptionAzureKeyVaultProvider(GetToken);
            Dictionary<string, SqlColumnEncryptionKeyStoreProvider> providers = new Dictionary<string, SqlColumnEncryptionKeyStoreProvider>();
            providers.Add(SqlColumnEncryptionAzureKeyVaultProvider.ProviderName, azureKeyVaultProvider);
            SqlConnection.RegisterColumnEncryptionKeyStoreProviders(providers);
        }

        async static Task<string> GetToken(string authority, string resource, string scope)
        {
            var authContext = new AuthenticationContext(authority);
            AuthenticationResult result = await authContext.AcquireTokenAsync(resource, _clientCredential);

            if (result == null)
                throw new InvalidOperationException("Failed to obtain the JWT token");

            return result.AccessToken;
        }
    }
}
