// Copyright(c) Microsoft Corporation. 
// All rights reserved.
//
// Licensed under the MIT license. See LICENSE file in the solution root folder for full license information.

using Microsoft.Extensions.Configuration;

namespace ProjectSmartLink.Service
{
    public class ConfigService : IConfigService
    {
        private IEncryptService _encryptService;
		private readonly IConfiguration configuration;
        public ConfigService(IConfiguration configuration)
        {
			this.configuration = configuration;
            _encryptService = new EncryptionService(configuration);
        }
        public string ClientId
        {
            get { return configuration.GetSection("AzureAd:ClientId").Value; }
        }

        public string ClientSecret
        {
            get { return configuration.GetSection("AzureAd:ClientSecret").Value; }
        }
        public string AzureAdInstance
        {
            get { return configuration.GetSection("AzureAd:Instance").Value; }
        }

        public string AzureAdTenantId
        {
            get { return configuration.GetSection("AzureAd:TenantId").Value; }
        }

        public string GraphResourceUrl
        {
            get { return "https://graph.microsoft.com/v1.0/"; }
        }

        public string AzureAdGraphResourceURL
        {
            get { return "https://graph.microsoft.com/"; }
        }

        public string AzureAdAuthority
        {
            get { return AzureAdInstance + AzureAdTenantId; }
        }

        public string ClaimTypeObjectIdentifier
        {
            get { return "http://schemas.microsoft.com/identity/claims/objectidentifier"; }
        }

        public string SharePointUrl
        {
            get
            {
				return configuration.GetSection("AzureAd:SharePointUrl").Value;
            }
        }

        public string DatabaseConnectionString
        {
            get
            {
                return _encryptService.DecryptString(configuration.GetSection("ConnectionStrings:DefaultConnection:ConnectionString").Value);
            }
        }
    }
}
