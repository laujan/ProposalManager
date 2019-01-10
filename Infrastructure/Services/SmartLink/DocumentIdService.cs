// Copyright(c) Microsoft Corporation. 
// All rights reserved.
//
// Licensed under the MIT license. See LICENSE file in the solution root folder for full license information.

using ApplicationCore;
using ApplicationCore.Interfaces.SmartLink;
using Microsoft.Extensions.Configuration;
using System;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace Infrastructure.Services.SmartLink
{

    public class DocumentIdService : IDocumentIdService
    {

        private readonly DocumentIdActivatorConfiguration documentIdActivatorConfiguration;

        public DocumentIdService(IConfiguration configuration)
        {
            documentIdActivatorConfiguration = new DocumentIdActivatorConfiguration();
            configuration.Bind(DocumentIdActivatorConfiguration.ConfigurationName, documentIdActivatorConfiguration);
        }

        public async Task ActivateForSite(string site)
        {
            try
            {
                var webhookAddress = $"{documentIdActivatorConfiguration.WebhookAddress}?arguments={HttpUtility.UrlEncode(site)}";
                var request = (HttpWebRequest)WebRequest.Create(webhookAddress);
                request.Method = "POST";
                var byteArray = Encoding.ASCII.GetBytes($"{documentIdActivatorConfiguration.WebhookUsername}:{documentIdActivatorConfiguration.WebhookPassword}");
                request.Headers.Add("Authorization", "Basic " + Convert.ToBase64String(byteArray));
                request.ContentLength = 0;

                await request.GetResponseAsync();
            }
            catch (Exception e)
            {
                throw e;
            }
        }
    }
}