// Copyright(c) Microsoft Corporation. 
// All rights reserved.
//
// Licensed under the MIT license. See LICENSE file in the solution root folder for full license information.

using ApplicationCore.Entities;
using ApplicationCore.Interfaces;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ApplicationCore.Helpers
{
	public class UserDataAccessor
	{
		private readonly Dynamics365Configuration dynamicsConfiguration;
		private readonly IDynamicsClientFactory dynamicsClientFactory;

		public UserDataAccessor(Dynamics365Configuration dynamicsConfiguration, IDynamicsClientFactory dynamicsClientFactory)
		{
			this.dynamicsConfiguration = dynamicsConfiguration;
			this.dynamicsClientFactory = dynamicsClientFactory;
		}

		public UserData this[string id] => GetDataById(id);

		private UserData GetDataById(string id)
		{
			var result = dynamicsClientFactory.GetDynamicsAuthorizedWebClientAsync().Result.GetAsync($"/api/data/v9.0/systemusers({id})?$select=domainname,azureactivedirectoryobjectid,fullname").Result;
			var user = JsonConvert.DeserializeObject<JObject>(result.Content.ReadAsStringAsync().Result);
			return new UserData
			{
				Email = user["domainname"].ToString(),
				Id = user["azureactivedirectoryobjectid"].ToString(),
				DisplayName = user["fullname"].ToString()
			};
		}
	}
}
