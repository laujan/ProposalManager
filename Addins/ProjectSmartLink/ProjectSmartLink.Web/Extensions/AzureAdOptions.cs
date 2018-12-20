// Copyright(c) Microsoft Corporation. 
// All rights reserved.
//
// Licensed under the MIT license. See LICENSE file in the solution root folder for full license information.

namespace ProjectSmartLink.Web.Extensions
{
    public class AzureAdOptions
    {
        public string ClientId { get; set; }

        public string ClientSecret { get; set; }

        public string Instance { get; set; }

        public string TenantId { get; set; }
		
        public string SharePointUrl { get; set; }

		public string ResourceId { get; set; }
		public string GraphScopes { get; set; }
        public string[] AllowedTenants { get; set; }
	}
}
