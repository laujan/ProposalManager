// Copyright(c) Microsoft Corporation. 
// All rights reserved.
//
// Licensed under the MIT license. See LICENSE file in the solution root folder for full license information

using Microsoft.SharePoint.Client;
using OfficeDevPnP.Core;
using OfficeDevPnP.Core.Framework.Provisioning.Providers;
using OfficeDevPnP.Core.Framework.Provisioning.Providers.Xml;
using System;
using static System.Configuration.ConfigurationManager;

namespace OpportunitySiteProvisioner
{
    class Program
    {
        static void Main(string[] args)
        {
            // Grab site from arguments
            var siteUrl = args[0];
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
            var authenticationManager = new AuthenticationManager();
            using (var context = authenticationManager.GetAppOnlyAuthenticatedContext(siteUrl, appId, appSecret))
            {
                Console.WriteLine("Succesfully authenticated. Provisioning...");
                // Apply the provisioning template
                context.Web.ApplyProvisioningTemplate(template);
                Console.WriteLine("Provisioning was successful.");
            }
        }
    }
}
