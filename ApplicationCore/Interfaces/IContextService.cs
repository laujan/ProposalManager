// Copyright(c) Microsoft Corporation. 
// All rights reserved.
//
// Licensed under the MIT license. See LICENSE file in the solution root folder for full license information

using System.Threading.Tasks;
using ApplicationCore.Models;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;

namespace ApplicationCore.Interfaces
{
    public interface IContextService
    {
        Task<JObject> GetTeamGroupDriveAsync(string teamGroupName);

        Task<JObject> GetSiteDriveAsync(string siteName);

        Task<JObject> GetSiteIdAsync(string siteName);

		Task<JArray> GetOpportunityStatusAllAsync();

		Task<JArray> GetActionStatusAllAsync();

        Task<ClientSettingsModel> GetClientSetingsAsync();

        //Task<JArray> GetPermissionsAllAsync(string requestId);
    }

}