// Copyright(c) Microsoft Corporation. 
// All rights reserved.
//
// Licensed under the MIT license. See LICENSE file in the solution root folder for full license information

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace ApplicationCore.Models
{
    public class ClientSettingsModel
    {
        public ClientSettingsModel()
        {
            ReportId = string.Empty;
            WorkSpaceId = string.Empty;
            TeamsAppInstanceId = string.Empty;
        }
        /// <summary>
        /// Power BI report identifier
        /// </summary>
        [JsonProperty("ReportId", Order = 1)]
        public string ReportId { get; set; }
        [JsonProperty("WorkspaceId", Order = 2)]
        public string WorkSpaceId { get; set; }
        [JsonProperty("TeamsAppInstanceId", Order = 3)]
        public string TeamsAppInstanceId { get; set; }
        [JsonProperty("SharePointListsPrefix", Order = 4)]
        public string SharePointListsPrefix { get; set; }
        [JsonProperty("GeneralProposalManagementTeam", Order = 5)]
        public string GeneralProposalManagementTeam { get; set; }
        [JsonProperty("SetupPage", Order = 6)]
        public string SetupPage { get; set; }
        [JsonProperty("SharePointHostName", Order = 7)]
        public string SharePointHostName { get; set; }
        [JsonProperty("ProposalManagementRootSiteId", Order = 8)]
        public string ProposalManagementRootSiteId { get; set; }
        [JsonProperty("CategoriesListId", Order = 9)]
        public string CategoriesListId { get; set; }
        [JsonProperty("TemplateListId", Order = 10)]
        public string TemplateListId { get; set; }
        [JsonProperty("RoleListId", Order = 11)]
        public string RoleListId { get; set; }
        [JsonProperty("Permissions", Order = 12)]
        public string Permissions { get; set; }
        [JsonProperty("ProcessListId", Order = 13)]
        public string ProcessListId { get; set; }
        [JsonProperty("IndustryListId", Order = 14)]
        public string IndustryListId { get; set; }
        [JsonProperty("RegionsListId", Order = 15)]
        public string RegionsListId { get; set; }
        [JsonProperty("DashboardListId", Order = 16)]
        public string DashboardListId { get; set; }
        [JsonProperty("RoleMappingsListId", Order = 17)]
        public string RoleMappingsListId { get; set; }
        [JsonProperty("OpportunitiesListId", Order = 18)]
        public string OpportunitiesListId { get; set; }
        [JsonProperty("MicrosoftAppId", Order = 19)]
        public string MicrosoftAppId { get; set; }
        [JsonProperty("MicrosoftAppPassword", Order = 20)]
        public string MicrosoftAppPassword { get; set; }
        [JsonProperty("AllowedTenants", Order = 21)]
        public string AllowedTenants { get; set; }
        [JsonProperty("BotServiceUrl", Order = 22)]
        public string BotServiceUrl { get; set; }
        [JsonProperty("BotName", Order = 23)]
        public string BotName { get; set; }
        [JsonProperty("BotId", Order = 24)]
        public string BotId { get; set; }
        [JsonProperty("PBIUserName", Order = 25)]
        public string PBIUserName { get; set; }
        [JsonProperty("PBIUserPassword", Order = 26)]
        public string PBIUserPassword { get; set; }
        [JsonProperty("PBIApplicationId", Order = 27)]
        public string PBIApplicationId { get; set; }
        [JsonProperty("PBIWorkSpaceId", Order = 28)]
        public string PBIWorkSpaceId { get; set; }
        [JsonProperty("PBIReportId", Order = 29)]
        public string PBIReportId { get; set; }
        [JsonProperty("PBITenantId", Order = 30)]
        public string PBITenantId { get; set; }
        [JsonProperty("ProposalManagerAddInName", Order = 31)]
        public string ProposalManagerAddInName { get; set; }
        [JsonProperty("ProposalManagerGroupID", Order = 32)]
        public string ProposalManagerGroupID { get; set; }
        [JsonProperty("UserProfileCacheExpiration", Order = 34)]
        public int UserProfileCacheExpiration { get; set; }
        [JsonProperty("GraphRequestUrl", Order = 35)]
        public string GraphRequestUrl { get; set; }
        [JsonProperty("GraphBetaRequestUrl", Order = 36)]
        public string GraphBetaRequestUrl { get; set; }
        [JsonProperty("SharePointSiteRelativeName",Order =37)]
        public string SharePointSiteRelativeName { get; set; }
        [JsonProperty("WebhookAddress", Order = 38)]
        public string WebhookAddress { get; set; }
        [JsonProperty("WebhookUsername", Order = 39)]
        public string WebhookUsername { get; set; }
        [JsonProperty("WebhookPassword", Order = 40)]
        public string WebhookPassword { get; set; }
    }
}
