// Copyright(c) Microsoft Corporation. 
// All rights reserved.
//
// Licensed under the MIT license. See LICENSE file in the solution root folder for full license information.

using ApplicationCore;
using ApplicationCore.Interfaces;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;

namespace Infrastructure.Services
{
    public class ConnectionRoleRepository : IConnectionRoleRepository
	{
		private readonly IConnectionRolesCache connectionRolesCache;
		private IReadOnlyDictionary<string, string> _connectionRoles;
		private readonly Dynamics365Configuration dynamicsConfiguration;
		private readonly HttpClient dynamicsClient;

		public ConnectionRoleRepository(
			IConfiguration configuration,
			IDynamicsClientFactory dynamicsClientFactory,
			IConnectionRolesCache connectionRolesCache)
		{
			dynamicsConfiguration = new Dynamics365Configuration();
			configuration.Bind(Dynamics365Configuration.ConfigurationName, dynamicsConfiguration);
			this.connectionRolesCache = connectionRolesCache;
			if (connectionRolesCache.ConnectionRoles is null)
			{
				dynamicsClient = dynamicsClientFactory.GetDynamicsAuthorizedWebClientAsync().Result;
				var result = dynamicsClient.GetAsync($"/api/data/v9.0/connectionroles?$filter=category eq {dynamicsConfiguration.ProposalManagerCategoryId}&$select=name").Result;
                JObject responseJObject = result.Content.ReadAsAsync<JObject>().Result;

                if (responseJObject == null || responseJObject["value"] == null)
                {
                    throw new Exception("Invalid or null response from Dynamics when querying for connection roles.");
                }

                var connectionRoles = responseJObject["value"].AsJEnumerable();
				connectionRolesCache.ConnectionRoles = new Dictionary<string, string>(from cr in connectionRoles
																					  select new KeyValuePair<string, string>(cr["connectionroleid"].ToString(), cr["name"].ToString()));
			}
		}

		public IReadOnlyDictionary<string, string> ConnectionRoles => _connectionRoles ?? (_connectionRoles = new ReadOnlyDictionary<string, string>(connectionRolesCache.ConnectionRoles));
	}
}