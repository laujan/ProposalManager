// Copyright(c) Microsoft Corporation. 
// All rights reserved.
//
// Licensed under the MIT license. See LICENSE file in the solution root folder for full license information

using ApplicationCore;
using ApplicationCore.Entities.GraphServices;
using Audit.Core;
using Audit.WebApi;
using Infrastructure.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using System;
using System.Threading.Tasks;

namespace WebReact.Api
{
    public class SharePointListDataProvider : AuditDataProvider
    {
        private readonly GraphSharePointAppService graphSharePointAppService;
        private readonly IHttpContextAccessor httpContextAccessor;
        private SiteList siteList;
        private readonly ILogger<SharePointListDataProvider> logger;

        public SharePointListDataProvider(GraphSharePointAppService graphSharePointAppService,
            IHttpContextAccessor httpContextAccessor, IOptionsMonitor<AppOptions> appOptions, ILogger<SharePointListDataProvider> logger)
        {
            this.httpContextAccessor = httpContextAccessor;
            this.graphSharePointAppService = graphSharePointAppService;
            this.logger = logger;

            siteList = new SiteList
            {
                SiteId = appOptions.CurrentValue.ProposalManagementRootSiteId,
                ListId = "Audit"
            };
        }
        public async override Task<object> InsertEventAsync(AuditEvent auditEvent)
        {
            try
            {
                var webApiAuditEvent = auditEvent as AuditEventWebApi;

                if (webApiAuditEvent == null)
                {
                    return auditEvent;
                }

                var userName = httpContextAccessor.HttpContext.User.FindFirst("preferred_username")?.Value;
                if (string.IsNullOrWhiteSpace(userName))
                {
                    return auditEvent;
                }

                webApiAuditEvent.Action.UserName = userName;

                dynamic itemFieldsJson = new JObject();
                itemFieldsJson.Log = webApiAuditEvent.ToJson();
                itemFieldsJson.User = userName;
                itemFieldsJson.Action = webApiAuditEvent.Action.ActionName;
                itemFieldsJson.Controller = webApiAuditEvent.Action.ControllerName;
                itemFieldsJson.Method = webApiAuditEvent.Action.HttpMethod;

                dynamic itemJson = new JObject();
                itemJson.fields = itemFieldsJson;

                await graphSharePointAppService.CreateListItemAsync(siteList, itemJson.ToString());
                System.Diagnostics.Trace.WriteLine(auditEvent);
                return auditEvent;
            }
            catch (Exception ex)
            {
                logger.LogError($"Error writing audit log: {ex}");
                return auditEvent;
            }
        }

        public override object InsertEvent(AuditEvent auditEvent)
        {
            return auditEvent;
        }
    }
}
