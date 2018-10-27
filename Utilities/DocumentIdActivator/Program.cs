// Copyright(c) Microsoft Corporation. 
// All rights reserved.
//
// Licensed under the MIT license. See LICENSE file in the solution root folder for full license information.

using Microsoft.Online.SharePoint.TenantAdministration;
using Microsoft.SharePoint.Client;
using System;
using System.Security;
using System.Threading;
using static System.Configuration.ConfigurationManager;

namespace DocumentIdActivator
{
    class Program
    {

        private const string docFeatureIdString = "b50e3104-6812-424f-a011-cc90e6327318";

        private static readonly Guid docIdFeatureId = new Guid(docFeatureIdString);

        static void Main(string[] args)
        {
            Thread.Sleep(30000);
            // Grab site from arguments
            var site = args[0];
            // Grab credentials and admin site from configuration
            var username = AppSettings["AdminUsername"];
            var password = AppSettings["AdminPassword"];
            var adminSite = AppSettings["AdminSite"];
            // Create secure password string
            var securepassword = new SecureString();
            foreach (var @char in password) securepassword.AppendChar(@char);
            // We set up 2 contexts: one for the tenant administration, and one for the site we want to modify
            var adminContext = new ClientContext(adminSite)
            {
                Credentials = new SharePointOnlineCredentials(username, securepassword)
            };
            var clientContext = new ClientContext(site)
            {
                Credentials = new SharePointOnlineCredentials(username, securepassword)
            };
            // Now, we allow CSOM clients (like this one) to modify property bags so that we can set up document id appropriately.
            var tenant = new Tenant(adminContext);
            var siteProperties = tenant.GetSitePropertiesByUrl(site, true);
            adminContext.Load(siteProperties);
            adminContext.ExecuteQuery();
            var priorValue = siteProperties.DenyAddAndCustomizePages;
            siteProperties.DenyAddAndCustomizePages = DenyAddAndCustomizePagesStatus.Disabled;
            siteProperties.Update();
            adminContext.ExecuteQuery();
            // Then, we activate document id.
            clientContext.Site.Features.Add(docIdFeatureId, true, FeatureDefinitionScope.Farm);
            clientContext.ExecuteQuery();
            clientContext.Web.AllProperties["docid_msft_hier_siteprefix"] = AppSettings["DocumentIdPrefix"];
            clientContext.Web.AllProperties["docid_enabled"] = "1";
            clientContext.Web.Update();
            clientContext.ExecuteQuery();
            // Lastly, we set the permissions like they were before to avoid security problems
            siteProperties.DenyAddAndCustomizePages = priorValue;
            siteProperties.Update();
            adminContext.ExecuteQuery();
        }
    }
}