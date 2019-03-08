// Copyright(c) Microsoft Corporation. 
// All rights reserved.
//
// Licensed under the MIT license. See LICENSE file in the solution root folder for full license information

using Microsoft.Online.SharePoint.TenantAdministration;
using Microsoft.SharePoint.Client;
using OfficeDevPnP.Core;
using OfficeDevPnP.Core.Framework.Provisioning.Providers;
using OfficeDevPnP.Core.Framework.Provisioning.Providers.Xml;
using System;
using System.Threading;
using static System.Configuration.ConfigurationManager;

namespace OpportunitySiteProvisioner
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Document id activation started...");
            // Grab site from arguments
            var siteUrl = args[0];
            var adminSiteUrl = new Uri(siteUrl.Replace(".sharepoint.com", "-admin.sharepoint.com")).GetLeftPart(UriPartial.Authority);
            Console.WriteLine($"Site url is \"{siteUrl}\"");
            // Grab client credentials from configuration
            var appId = AppSettings["AppId"];
            var appSecret = AppSettings["AppSecret"];
            // Load the provisioning template
            Console.WriteLine("Loading template...");
            TemplateProviderBase templateProvider = new XMLFileSystemTemplateProvider(".\\", string.Empty);
            var template = templateProvider.GetTemplate("OpportunitySite.xml");
            Console.WriteLine("Template successfully parsed.");
            // Authenticate using the client credentials flow
            Console.WriteLine("Attempting OAuth authentication...");
            // Prepare site
            var retries = 0;
            do
            {
                try
                {
                    var adminAuthenticationManager = new AuthenticationManager();
                    using (var adminContext = adminAuthenticationManager.GetAppOnlyAuthenticatedContext(adminSiteUrl, appId, appSecret))
                    {
                        var tenant = new Tenant(adminContext);
                        tenant.SetSiteProperties(siteUrl, noScriptSite: false);
                        adminContext.ExecuteQuery();
                        var authenticationManager = new AuthenticationManager();
                        using (var context = authenticationManager.GetAppOnlyAuthenticatedContext(siteUrl, appId, appSecret))
                        {
                            Console.WriteLine("Succesfully authenticated. Provisioning...");
                            // Apply the provisioning template
                            context.Web.ApplyProvisioningTemplate(template);
                            Console.WriteLine("Provisioning was successful.");
                        }
                    }
                    return;
                }
                catch (Exception ex)
                {
                    var errorMessage = $"Provisioning failed: {ex.Message}. Retry #{retries}.";
                    Console.WriteLine(errorMessage);
                    retries++;
                    Thread.Sleep(5000);
                }
            } while (retries < 10);

            throw new Exception($"Document Id Activation failed after {retries}. Please activate it manually");
        }
    }
}