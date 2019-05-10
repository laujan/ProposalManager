// Copyright(c) Microsoft Corporation. 
// All rights reserved.
//
// Licensed under the MIT license. See LICENSE file in the solution root folder for full license information.

using ApplicationCore.Interfaces;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Net.Http;

namespace ApplicationCore.Helpers
{
	public class ContactNameAccessor
	{
		private readonly Dynamics365Configuration dynamicsConfiguration;
		private readonly IDynamicsClientFactory dynamicsClientFactory;

		public ContactNameAccessor(Dynamics365Configuration dynamicsConfiguration, IDynamicsClientFactory dynamicsClientFactory)
		{
			this.dynamicsConfiguration = dynamicsConfiguration;
			this.dynamicsClientFactory = dynamicsClientFactory;
		}

		public string this[string id] => GetNameById(id);

		private string GetNameById(string id)
		{
			var result = dynamicsClientFactory.GetDynamicsAuthorizedWebClientAsync().Result.GetAsync($"/api/data/v9.0/contacts({id})?$select=fullname").Result;
            JObject responseJObject = result.Content.ReadAsAsync<JObject>().Result;

            if (responseJObject == null || responseJObject["fullname"] == null)
            {
                throw new Exception($"Invalid or null response from Dynamics when querying for contact id {id}.");
            }

            return responseJObject["fullname"].ToString();
		}
	}
}
