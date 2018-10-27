// Copyright(c) Microsoft Corporation. 
// All rights reserved.
//
// Licensed under the MIT license. See LICENSE file in the solution root folder for full license information

using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;

namespace ApplicationCore.Interfaces
{
    public interface ISetupService
    {
        Task<StatusCodes> UpdateAppOpptionsAsync(string key, string value, string requestId = "");
        Task<StatusCodes> UpdateDocumentIdActivatorOptionsAsync(string key, string value, string requestId = "");
        Task CreateSitePermissionsAsync(string requestId = "");
        Task CreateAllListsAsync(string siteRootId,string requestId = "");
        Task CreateSiteRolesAsync(string requestId = "");
        Task CreateSiteProcessesAsync(string requestId = "");
        Task CreateProposalManagerTeamAsync(string name, string requestId = "");
        Task CreateAdminGroupAsync(string name, string requestId = "");
        Task<string> GetAppId(string name, string requestId = "");
        Task CreateSiteAdminPermissionsAsync(string adGroupName, string requestId = "");
    }

}