﻿// Copyright(c) Microsoft Corporation. 
// All rights reserved.
//
// Licensed under the MIT license. See LICENSE file in the solution root folder for full license information.

using ApplicationCore;
using ApplicationCore.Entities.GraphServices;
using ApplicationCore.Helpers;
using ApplicationCore.Helpers.Exceptions;
using ApplicationCore.Interfaces;
using Infrastructure.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Graph;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.GraphApi
{
    public abstract class GraphSharePointBaseService : BaseService<GraphSharePointBaseService>
    {
        protected readonly IGraphClientContext _graphClientContext;
        private readonly IMemoryCache memoryCache;
        private const string SharePointRootSiteIdKey = "rootId";

        public GraphSharePointBaseService(
            ILogger<GraphSharePointBaseService> logger,
            IOptionsMonitor<AppOptions> appOptions,
            IGraphClientContext graphClientContext,
            IMemoryCache memoryCache) : base(logger, appOptions)
        {
            Guard.Against.Null(graphClientContext, nameof(graphClientContext));
            _graphClientContext = graphClientContext;
            this.memoryCache = memoryCache;
        }

        /// <summary>
        /// Graph Service client
        /// </summary>
        public GraphServiceClient GraphClient => _graphClientContext?.GraphClient;

        public async Task<JObject> GetDefaultSiteAsync(string requestId = "")
        {
            // GET: https://graph.microsoft.com/v1.0/sites/root
            // EXAMPLE: https://graph.microsoft.com/v1.0/sites/root

            _logger.LogInformation($"RequestId: {requestId} - GetDefaultSiteAsync called.");
            try
            {
                var requestUrl = $"{_appOptions.GraphRequestUrl}sites/root";

                // Create the request message and add the content.
                HttpRequestMessage hrm = new HttpRequestMessage(HttpMethod.Get, requestUrl);

                var response = new HttpResponseMessage();

                // Authenticate (add access token) our HttpRequestMessage
                await GraphClient.AuthenticationProvider.AuthenticateRequestAsync(hrm);

                // Send the request and get the response.
                response = await GraphClient.HttpProvider.SendAsync(hrm);

                // Get the status response and throw if is not 200.
                Guard.Against.NotStatus200OK(response.StatusCode, "GetDefaultSiteAsync", requestId);

                JObject responseJObject = await response.Content.ReadAsAsync<JObject>();

                _logger.LogInformation($"RequestId: {requestId} - GetDefaultSiteAsync end.");
                return responseJObject;
            }
            catch (Exception ex)
            {
                _logger.LogError($"RequestId: {requestId} - GetDefaultSiteAsync Service Exception: {ex}");
                throw new ResponseException($"RequestId: {requestId} - GetDefaultSiteAsync Service Exception: {ex}");
            }
        }

        public async Task<JObject> GetSiteIdAsync(string hostName, string path, string requestId = "")
        {
            // GET: https://graph.microsoft.com/v1.0/sites/{hostname}:/sites/{path}?$select=id
            // EXAMPLE: https://graph.microsoft.com/v1.0/sites/onterawe.sharepoint.com:/sites/XYZMotors?$select=id

            _logger.LogInformation($"RequestId: {requestId} - GetSiteIdAsync called.");
            try
            {
                Guard.Against.Null(hostName, nameof(hostName), requestId);
                Guard.Against.Null(path, nameof(path), requestId);

                var requestUrl = $"{_appOptions.GraphRequestUrl}sites/{hostName}:/sites/{path}?$select=id";

                // Create the request message and add the content.
                HttpRequestMessage hrm = new HttpRequestMessage(HttpMethod.Get, requestUrl);

                var response = new HttpResponseMessage();

                // Authenticate (add access token) our HttpRequestMessage
                await GraphClient.AuthenticationProvider.AuthenticateRequestAsync(hrm);

                // Send the request and get the response.
                response = await GraphClient.HttpProvider.SendAsync(hrm);

                // Get the status response and throw if is not 200.
                Guard.Against.NotStatus200OK(response.StatusCode, "GetSiteIdAsync", requestId);

                JObject responseJObject = await response.Content.ReadAsAsync<JObject>();

                _logger.LogInformation($"RequestId: {requestId} - GetSiteIdAsync end.");
                return responseJObject;
            }
            catch (Exception ex)
            {
                _logger.LogError($"RequestId: {requestId} - GetSiteIdAsync Service Exception: {ex}");
                throw new ResponseException($"RequestId: {requestId} - GetSiteIdAsync Service Exception: {ex}");
            }
        }


        // List Management
        public async Task<JObject> CreateSiteListAsync(string htmlBody, string rootId, string requestId = "")
        {
            // POST: https://graph.microsoft.com/beta/sites/{site-id}/lists
            // EXAMPLE: https://graph.microsoft.com/v1.0/sites/onterawe.sharepoint.com,988079b1-450c-44ae-bad2-41aeffe2fadb,7028bf8f-4174-4578-96cc-e5a9f52e542c/lists

            _logger.LogInformation($"RequestId: {requestId} - CreateSiteListAsync called.");
            try
            {
                if (String.IsNullOrEmpty(htmlBody)) throw new ArgumentNullException(nameof(htmlBody));
                if (String.IsNullOrEmpty(rootId)) throw new ArgumentNullException(nameof(rootId));

                var requestUrl = _appOptions.GraphRequestUrl + "/sites/" + rootId + "/lists";

                // Create the request message and add the content.
                HttpRequestMessage hrm = new HttpRequestMessage(HttpMethod.Post, requestUrl);
                hrm.Content = new StringContent(htmlBody, Encoding.UTF8, "application/json");

                var response = new HttpResponseMessage();

                // Authenticate (add access token) our HttpRequestMessage
                await GraphClient.AuthenticationProvider.AuthenticateRequestAsync(hrm);

                // Send the request and get the response.
                response = await GraphClient.HttpProvider.SendAsync(hrm);

                JObject responseJObject = await response.Content.ReadAsAsync<JObject>();

                // Get the content from the response.
                if (response.StatusCode != System.Net.HttpStatusCode.Created)
                {
                    // TODO: Depending on code, rise proper exception for now invalid request is
                    throw new ServiceException(new Error { Code = ErrorConstants.Codes.InvalidRequest, Message = response.StatusCode.ToString() });
                }
                return responseJObject;

            }
            catch (Exception ex)
            {
                _logger.LogError($"RequestId: {requestId} - CreateSiteListAsync Service Exception: {ex}");
                throw new ResponseException($"RequestId: {requestId} - CreateSiteListAsync Service Exception: {ex}");
            }
        }

        public async Task<JObject> GetSiteListAsync(string siteId, string listId, string requestId = "")
        {
            // GET: https://graph.microsoft.com/v1.0/sites/{site-id}/lists/{list-id}
            // EXAMPLE: https://graph.microsoft.com/v1.0/sites/onterawe.sharepoint.com,e4330185-7583-4b11-bb2c-2a0a9196d7f6,3830a01b-ed62-4c22-bd9c-283ba275622c/lists/UserRoles

            _logger.LogInformation($"RequestId: {requestId} - GetSiteListAsync_siteId_listId called.");
            try
            {
                Guard.Against.Null(siteId, nameof(siteId), requestId);
                Guard.Against.Null(listId, nameof(listId), requestId);

                var requestUrl = $"{_appOptions.GraphRequestUrl}sites/{siteId}/lists/{listId}";

                // Create the request message and add the content.
                HttpRequestMessage hrm = new HttpRequestMessage(HttpMethod.Get, requestUrl);

                var response = new HttpResponseMessage();

                // Authenticate (add access token) our HttpRequestMessage
                await GraphClient.AuthenticationProvider.AuthenticateRequestAsync(hrm);

                // Send the request and get the response.
                response = await GraphClient.HttpProvider.SendAsync(hrm);

                // Get the status response and throw if is not 200.
                Guard.Against.NotStatus200OK(response.StatusCode, "GetSiteListAsync_siteId_listId", requestId);

                JObject responseJObject = await response.Content.ReadAsAsync<JObject>();

                _logger.LogInformation($"RequestId: {requestId} - GetSiteListAsync_siteId_listId end.");
                return responseJObject;
            }
            catch (Exception ex)
            {
                _logger.LogError($"RequestId: {requestId} - GetSiteListAsync_siteId_listId Service Exception: {ex}");
                throw new ResponseException($"RequestId: {requestId} - GetSiteListAsync_siteId_listId Service Exception: {ex}");
            }
        }

        public async Task<JObject> GetSiteListAsync(SiteList siteList, string requestId = "")
        {
            _logger.LogInformation($"RequestId: {requestId} - GetSiteListAsync_siteList called.");

            Guard.Against.Null(siteList, nameof(siteList), requestId);

            return await GetSiteListAsync(siteList.SiteId, siteList.ListId, requestId);
        }

        // List Item Management
        public async Task<JObject> CreateListItemAsync(SiteList siteList, string siteListItemJson, string requestId = "")
        {
            // POST: https://graph.microsoft.com/beta/sites/{site-id}/lists/{list-id}/items
            // EXAMPLE: https://graph.microsoft.com/v1.0/sites/onterawe.sharepoint.com,988079b1-450c-44ae-bad2-41aeffe2fadb,7028bf8f-4174-4578-96cc-e5a9f52e542c/lists

            _logger.LogInformation($"RequestId: {requestId} - CreateListItemAsync called.");
            try
            {
                Guard.Against.Null(siteList, nameof(siteList), requestId);
                Guard.Against.NullOrEmpty(siteList.ListId, nameof(siteList.ListId), requestId);
                Guard.Against.NullOrEmpty(siteList.SiteId, nameof(siteList.SiteId), requestId);
                Guard.Against.NullOrEmpty(siteListItemJson, nameof(siteListItemJson), requestId);

                var requestUrl = $"{_appOptions.GraphRequestUrl}sites/{siteList.SiteId}/lists/{siteList.ListId}/items";

                // Create the request message and add the content.
                HttpRequestMessage hrm = new HttpRequestMessage(HttpMethod.Post, requestUrl);
                hrm.Content = new StringContent(siteListItemJson, Encoding.UTF8, "application/json");

                var response = new HttpResponseMessage();

                // Authenticate (add access token) our HttpRequestMessage
                await GraphClient.AuthenticationProvider.AuthenticateRequestAsync(hrm);

                // Send the request and get the response.
                response = await GraphClient.HttpProvider.SendAsync(hrm);

                // Get the status response and throw if is not 201.
                Guard.Against.NotStatus201Created(response.StatusCode, "CreateListItemAsync", requestId);

                JObject responseJObject = await response.Content.ReadAsAsync<JObject>();

                _logger.LogInformation($"RequestId: {requestId} - CreateListItemAsync end.");
                return responseJObject;
            }
            catch (Exception ex)
            {
                _logger.LogError($"RequestId: {requestId} - CreateListItemAsync Service Exception: {ex}");
                throw new ResponseException($"RequestId: {requestId} - CreateListItemAsync Service Exception: {ex}");
            }
        }

        public async Task<JObject> UpdateListItemAsync(SiteList siteList, string itemId, string siteListItemJson, string requestId = "")
        {
            // PATCH: https://graph.microsoft.com/v1.0/sites/{site-id}/lists/{list-id}/items/{item-id}/fields
            // EXAMPLE: https://graph.microsoft.com/v1.0/sites/onterawe.sharepoint.com,e4330185-7583-4b11-bb2c-2a0a9196d7f6,3830a01b-ed62-4c22-bd9c-283ba275622c/lists/UserRoles

            _logger.LogInformation($"RequestId: {requestId} - UpdateListItemAsync called.");
            try
            {
                Guard.Against.Null(siteList, nameof(siteList), requestId);
                Guard.Against.NullOrEmpty(siteList.ListId, nameof(siteList.ListId), requestId);
                Guard.Against.NullOrEmpty(siteList.SiteId, nameof(siteList.SiteId), requestId);
                Guard.Against.NullOrEmpty(itemId, nameof(itemId), requestId);
                Guard.Against.NullOrEmpty(siteListItemJson, nameof(siteListItemJson), requestId);

                var method = new HttpMethod("PATCH");
                var requestUrl = $"{_appOptions.GraphRequestUrl}sites/{siteList.SiteId}/lists/{siteList.ListId}/items/{itemId}/fields";

                // Create the request message and add the content.
                HttpRequestMessage hrm = new HttpRequestMessage(method, requestUrl);
                hrm.Content = new StringContent(siteListItemJson, Encoding.UTF8, "application/json");

                var response = new HttpResponseMessage();

                // Authenticate (add access token) our HttpRequestMessage
                await GraphClient.AuthenticationProvider.AuthenticateRequestAsync(hrm);

                _logger.LogInformation($"RequestId: {requestId} - UpdateListItemAsync call to graph: " + requestUrl);

                // Send the request and get the response.
                response = await GraphClient.HttpProvider.SendAsync(hrm);

                // Get the status response and throw if is not 200.
                Guard.Against.NotStatus200OK(response.StatusCode, "UpdateListItemAsync", requestId);

                JObject responseJObject = await response.Content.ReadAsAsync<JObject>();

                _logger.LogInformation($"RequestId: {requestId} - UpdateListItemAsync end.");
                return responseJObject;
            }
            catch (Exception ex)
            {
                _logger.LogError($"RequestId: {requestId} - UpdateListItemAsync Service Exception: {ex}");
                throw new ResponseException($"RequestId: {requestId} - UpdateListItemAsync Service Exception: {ex}");
            }
        }

        public async Task<JObject> GetListItemsAsync(SiteList siteList, string expand = "", string requestId = "")
        {

            _logger.LogInformation($"RequestId: {requestId} - GetListItemsAsync_noOptions called.");
            try
            {
                Guard.Against.Null(siteList, nameof(siteList), requestId);
                Guard.Against.NullOrEmpty(siteList.ListId, nameof(siteList.ListId), requestId);
                Guard.Against.NullOrEmpty(siteList.SiteId, nameof(siteList.SiteId), requestId);

                var queryOptions = new List<QueryParam>();
                var responseJObject = await GetListItemsAsync(siteList, queryOptions, expand, requestId);

                _logger.LogInformation($"RequestId: {requestId} - GetListItemsAsync_noOptions end.");
                return responseJObject;
            }
            catch (Exception ex)
            {
                _logger.LogError($"RequestId: {requestId} - GetListItemsAsync_noOptions Service Exception: {ex}");
                throw new ResponseException($"RequestId: {requestId} - GetListItemsAsync_noOptions Service Exception: {ex}");
            }
        }

        public async Task<JObject> GetListItemsAsync(SiteList siteList, IList<QueryParam> queryOptions, string expand = "", string requestId = "")
        {
            // GET: https://graph.microsoft.com/v1.0/sites/{site-id}/lists/{list-id}/items
            // EXAMPLE: https://graph.microsoft.com/v1.0/sites/onterawe.sharepoint.com,e4330185-7583-4b11-bb2c-2a0a9196d7f6,3830a01b-ed62-4c22-bd9c-283ba275622c/lists/UserRoles

            _logger.LogInformation($"RequestId: {requestId} - GetListItemsAsync called.");
            try
            {
                Guard.Against.Null(siteList, nameof(siteList), requestId);
                Guard.Against.NullOrEmpty(siteList.ListId, nameof(siteList.ListId), requestId);
                Guard.Against.NullOrEmpty(siteList.SiteId, nameof(siteList.SiteId), requestId);

                if (!String.IsNullOrEmpty(expand))
                {
                    queryOptions.Add(new QueryParam("expand", expand));
                }

                var requestOptions = string.Empty;
                foreach (var item in queryOptions)
                {
                    if (String.IsNullOrEmpty(requestOptions))
                    {
                        requestOptions = $"/?{item.Name}={item.Value}";
                    }
                    else
                    {
                        requestOptions = requestOptions + $"&{item.Name}={item.Value}";
                    }
                }

                var requestUrl = $"{_appOptions.GraphRequestUrl}sites/{siteList.SiteId}/lists/{siteList.ListId}/items{requestOptions}";

                // Create the request message and add the content.
                HttpRequestMessage hrm = new HttpRequestMessage(HttpMethod.Get, requestUrl);

                var response = new HttpResponseMessage();

                // Authenticate (add access token) our HttpRequestMessage
                await GraphClient.AuthenticationProvider.AuthenticateRequestAsync(hrm);

                _logger.LogInformation($"RequestId: {requestId} - GetListItemsAsync call to graph: " + requestUrl);

                // Send the request and get the response.
                response = await GraphClient.HttpProvider.SendAsync(hrm);

                // Get the status response and throw if is not 200.
                Guard.Against.NotStatus200OK(response.StatusCode, "GetListItemsAsync", requestId);

                //TODO: Handle SkipToken 
                JObject responseJObject = await response.Content.ReadAsAsync<JObject>();

                _logger.LogInformation($"RequestId: {requestId} - GetListItemsAsync end.");
                return responseJObject;
            }
            catch (Exception ex)
            {
                _logger.LogError($"RequestId: {requestId} - GetListItemsAsync Service Exception: {ex}");
                throw new ResponseException($"RequestId: {requestId} - GetListItemsAsync Service Exception: {ex}");
            }
        }

        public async Task<JObject> GetListItemByIdAsync(SiteList siteList, string itemId, string expand = "", string requestId = "")
        {
            // GET: https://graph.microsoft.com/v1.0/sites/{site-id}/lists/{list-id}/items/{item-id}
            // EXAMPLE: https://graph.microsoft.com/v1.0/sites/onterawe.sharepoint.com,e4330185-7583-4b11-bb2c-2a0a9196d7f6,3830a01b-ed62-4c22-bd9c-283ba275622c/lists/UserRoles

            _logger.LogInformation($"RequestId: {requestId} - GetListItemByIdAsync called.");
            try
            {
                Guard.Against.Null(siteList, nameof(siteList), requestId);
                Guard.Against.NullOrEmpty(siteList.ListId, nameof(siteList.ListId), requestId);
                Guard.Against.NullOrEmpty(siteList.SiteId, nameof(siteList.SiteId), requestId);
                Guard.Against.NullOrEmpty(itemId, nameof(itemId));

                var expandOption = String.Empty;
                if (!String.IsNullOrEmpty(expand))
                {
                    expandOption = $"?expand={expand}";
                }

                var requestUrl = $"{_appOptions.GraphRequestUrl}sites/{siteList.SiteId}/lists/{siteList.ListId}/items/{itemId}{expandOption}";

                // Create the request message and add the content.
                HttpRequestMessage hrm = new HttpRequestMessage(HttpMethod.Get, requestUrl);

                var response = new HttpResponseMessage();

                // Authenticate (add access token) our HttpRequestMessage
                await GraphClient.AuthenticationProvider.AuthenticateRequestAsync(hrm);

                // Send the request and get the response.
                response = await GraphClient.HttpProvider.SendAsync(hrm);


                // Get the status response and throw if is not 200.
                Guard.Against.NotStatus200OK(response.StatusCode, "GetListItemByIdAsync", requestId);

                JObject responseJObject = await response.Content.ReadAsAsync<JObject>();

                _logger.LogInformation($"RequestId: {requestId} - GetListItemByIdAsync end.");
                return responseJObject;
            }
            catch (Exception ex)
            {
                _logger.LogError($"RequestId: {requestId} - GetListItemByIdAsync Service Exception: {ex}");
                throw new ResponseException($"RequestId: {requestId} - GetListItemByIdAsync Service Exception: {ex}");
            }
        }

        public async Task<JObject> GetListItemAsync(SiteList siteList, IList<QueryParam> queryOptions, string expand = "", string requestId = "")
        {
            // GET: https://graph.microsoft.com/v1.0/sites/{site-id}/lists/{list-id}/items/{item-id}
            // EXAMPLE: https://graph.microsoft.com/v1.0/sites/onterawe.sharepoint.com,e4330185-7583-4b11-bb2c-2a0a9196d7f6,3830a01b-ed62-4c22-bd9c-283ba275622c/lists/UserRoles

            _logger.LogInformation($"RequestId: {requestId} - GetListItemAsync called.");
            try
            {
                Guard.Against.Null(siteList, nameof(siteList), requestId);
                Guard.Against.NullOrEmpty(siteList.ListId, nameof(siteList.ListId), requestId);
                Guard.Against.NullOrEmpty(siteList.SiteId, nameof(siteList.SiteId), requestId);
                Guard.Against.Null(queryOptions, nameof(queryOptions), requestId);

                if (!String.IsNullOrEmpty(expand))
                {
                    queryOptions.Add(new QueryParam("expand", expand));
                }

                var requestOptions = string.Empty;
                foreach (var item in queryOptions)
                {
                    if (String.IsNullOrEmpty(requestOptions))
                    {
                        requestOptions = $"/?{item.Name}={item.Value}";
                    }
                    else
                    {
                        requestOptions = requestOptions + $"&{item.Name}={item.Value}";
                    }
                }

                var requestUrl = $"{_appOptions.GraphRequestUrl}sites/{siteList.SiteId}/lists/{siteList.ListId}/items{requestOptions}";

                // Create the request message and add the content.
                HttpRequestMessage hrm = new HttpRequestMessage(HttpMethod.Get, requestUrl);

                var response = new HttpResponseMessage();

                // Authenticate (add access token) our HttpRequestMessage
                await GraphClient.AuthenticationProvider.AuthenticateRequestAsync(hrm);

                // Send the request and get the response.
                response = await GraphClient.HttpProvider.SendAsync(hrm);

                // Get the status response and throw if is not 200.
                Guard.Against.NotStatus200OK(response.StatusCode, "GetListItemAsync", requestId);

                JObject responseJObject = await response.Content.ReadAsAsync<JObject>();

                _logger.LogInformation($"RequestId: {requestId} - GetListItemAsync end.");
                return responseJObject;
            }
            catch (Exception ex)
            {
                _logger.LogError($"RequestId: {requestId} - GetListItemAsync Service Exception: {ex}");
                throw new ResponseException($"RequestId: {requestId} - GetListItemAsync Service Exception: {ex}");
            }
        }

        public async Task<JObject> DeleteListItemAsync(SiteList siteList, string itemId, string requestId = "")
        {
            // DELETE: https://graph.microsoft.com/v1.0/sites/{site-id}/lists/{list-id}/items/{item-id}
            // EXAMPLE: 

            _logger.LogInformation($"RequestId: {requestId} - DeleteListItemAsync called.");
            try
            {
                Guard.Against.Null(siteList, nameof(siteList), requestId);
                Guard.Against.NullOrEmpty(siteList.ListId, nameof(siteList.ListId), requestId);
                Guard.Against.NullOrEmpty(siteList.SiteId, nameof(siteList.SiteId), requestId);
                Guard.Against.NullOrEmpty(itemId, nameof(itemId));


                var requestUrl = $"{_appOptions.GraphRequestUrl}sites/{siteList.SiteId}/lists/{siteList.ListId}/items/{itemId}";

                // Create the request message and add the content.
                HttpRequestMessage hrm = new HttpRequestMessage(HttpMethod.Delete, requestUrl);

                var response = new HttpResponseMessage();

                // Authenticate (add access token) our HttpRequestMessage
                await GraphClient.AuthenticationProvider.AuthenticateRequestAsync(hrm);

                // Send the request and get the response.
                response = await GraphClient.HttpProvider.SendAsync(hrm);


                // Get the status response and throw if is not 204.
                Guard.Against.NotStatus204NoContent(response.StatusCode, "DeleteListItemAsync", requestId);

                JObject responseJObject = JObject.FromObject(ApplicationCore.StatusCodes.Status204NoContent);

                _logger.LogInformation($"RequestId: {requestId} - DeleteListItemAsync end.");
                return responseJObject;
            }
            catch (Exception ex)
            {
                _logger.LogError($"RequestId: {requestId} - DeleteListItemAsync Service Exception: {ex}");
                throw new ResponseException($"RequestId: {requestId} - DeleteListItemAsync Service Exception: {ex}");
            }
        }

        // OneDrive
        public async Task<JObject> GetSiteDriveAsync(string siteId, string requestId = "")
        {
            // GET: https://graph.microsoft.com/v1.0/sites/{site-id}/drive
            // EXAMPLE: https://graph.microsoft.com/v1.0/sites/onterawe.sharepoint.com,e4330185-7583-4b11-bb2c-2a0a9196d7f6,3830a01b-ed62-4c22-bd9c-283ba275622c/drive

            _logger.LogInformation($"RequestId: {requestId} - GetSiteDriveAsync called.");
            try
            {
                Guard.Against.Null(siteId, nameof(siteId), requestId);

                var requestUrl = $"{_appOptions.GraphRequestUrl}sites/{siteId}/drive";

                // Create the request message and add the content.
                HttpRequestMessage hrm = new HttpRequestMessage(HttpMethod.Get, requestUrl);

                var response = new HttpResponseMessage();

                // Authenticate (add access token) our HttpRequestMessage
                await GraphClient.AuthenticationProvider.AuthenticateRequestAsync(hrm);

                // Send the request and get the response.
                response = await GraphClient.HttpProvider.SendAsync(hrm);

                // Get the status response and throw if is not 200.
                Guard.Against.NotStatus200OK(response.StatusCode, "GetSiteDriveAsync", requestId);

                JObject responseJObject = await response.Content.ReadAsAsync<JObject>();

                _logger.LogInformation($"RequestId: {requestId} - GetSiteDriveAsync end.");
                return responseJObject;
            }
            catch (Exception ex)
            {
                _logger.LogError($"RequestId: {requestId} - GetSiteDriveAsync Service Exception: {ex}");
                throw new ResponseException($"RequestId: {requestId} - GetSiteDriveAsync Service Exception: {ex}");
            }
        }

        public async Task<JObject> GetSiteDriveChildrensAsync(string siteId, string requestId = "")
        {
            // GET: https://graph.microsoft.com/v1.0/sites/{site-id}/drive/root/children
            // EXAMPLE: https://graph.microsoft.com/v1.0/sites/onterawe.sharepoint.com,69f40286-aa2c-4959-a604-98e9b28f6d0c,ae164d4d-cfde-41b9-9715-ff4cd0f3cc57/drive/root/children

            _logger.LogInformation($"RequestId: {requestId} - GetSiteDriveChildrensAsync called.");
            try
            {
                Guard.Against.Null(siteId, nameof(siteId), requestId);

                var requestUrl = $"{_appOptions.GraphRequestUrl}sites/{siteId}/drive/root/children";

                // Create the request message and add the content.
                HttpRequestMessage hrm = new HttpRequestMessage(HttpMethod.Get, requestUrl);

                var response = new HttpResponseMessage();

                // Authenticate (add access token) our HttpRequestMessage
                await GraphClient.AuthenticationProvider.AuthenticateRequestAsync(hrm);

                // Send the request and get the response.
                response = await GraphClient.HttpProvider.SendAsync(hrm);

                // Get the status response and throw if is not 200.
                Guard.Against.NotStatus200OK(response.StatusCode, "GetSiteDriveChildrensAsync", requestId);

                JObject responseJObject = await response.Content.ReadAsAsync<JObject>();

                _logger.LogInformation($"RequestId: {requestId} - GetSiteDriveChildrensAsync end.");
                return responseJObject;
            }
            catch (Exception ex)
            {
                _logger.LogError($"RequestId: {requestId} - GetSiteDriveChildrensAsync Service Exception: {ex}");
                throw new ResponseException($"RequestId: {requestId} - GetSiteDriveChildrensAsync Service Exception: {ex}");
            }
        }

        public async Task<JObject> GetFileOrFolderAsync(string siteId, string itemPath, string requestId = "")
        {
            // GET
            // EXAMPLE: https://graph.microsoft.com/v1.0/sites/onterawe.sharepoint.com,e4330185-7583-4b11-bb2c-2a0a9196d7f6,4c366d80-b803-4f4e-8ccf-a58384fa35ec/drive/items/root:/children

            _logger.LogInformation($"RequestId: {requestId} - DeleteFolderAsync called.");
            try
            {
                Guard.Against.NullOrEmpty(siteId, nameof(siteId), requestId);
                Guard.Against.NullOrEmpty(itemPath, nameof(itemPath), requestId);

                //var requestUrl = $"{_appOptions.GraphRequestUrl}sites/{siteId}/drive/root:/{folder}/{file.FileName}:/content";

                // Get the file or folder.
                var response = await GraphClient.Sites[siteId].Drive.Root.ItemWithPath(itemPath).Request().GetAsync();

                return JObject.FromObject(response);
            }
            catch (Exception ex)
            {
                _logger.LogError($"RequestId: {requestId} - DeleteFolderAsync Service Exception: {ex}");
                throw new ResponseException($"RequestId: {requestId} - DeleteFolderAsync Service Exception: {ex}");
            }
        }

        public async Task<JObject> CreateFolderAsync(string siteId, string folderName, string path, string requestId = "")
        {
            // PUT (replace file): https://graph.microsoft.com/v1.0/sites/{site-id}/drive/root:/{folder}/{filename}:/content
            // POST: POST https://graph.microsoft.com/v1.0/sites/{site-id}/drive/items/{parent-item-id}/children
            // POST: POST https://graph.microsoft.com/v1.0/sites/{site-id}/drive/items/root:/children
            // EXAMPLE: https://graph.microsoft.com/v1.0/sites/onterawe.sharepoint.com,e4330185-7583-4b11-bb2c-2a0a9196d7f6,4c366d80-b803-4f4e-8ccf-a58384fa35ec/drive/items/root:/children

            _logger.LogInformation($"RequestId: {requestId} - CreateFolderAsync called.");
            try
            {
                Guard.Against.NullOrEmpty(siteId, nameof(siteId), requestId);
                Guard.Against.NullOrEmpty(folderName, nameof(folderName), requestId);
                Guard.Against.Null(path, nameof(path), requestId);

                //var requestUrl = $"{_appOptions.GraphRequestUrl}sites/{siteId}/drive/root:/{folder}/{file.FileName}:/content";

                // Add the folder.
                DriveItem folder = await GraphClient.Sites[siteId].Drive.Root.ItemWithPath(path).Children.Request().AddAsync(new DriveItem
                {
                    Name = folderName,
                    Folder = new Folder()
                });


                if (folder != null)
                {
                    var responseJObject = JObject.FromObject(folder);

                    _logger.LogInformation($"RequestId: {requestId} - CreateFolderAsync end.");
                    return responseJObject;
                }

                _logger.LogError($"RequestId: {requestId} - CreateFolderAsync error: response foler null for folder: {folder}");
                var errorResponse = JsonErrorResponse.BadRequest($"CreateFolderAsync error: response foler null for folder: {folder}", requestId);
                return errorResponse;
            }
            catch (Exception ex)
            {
                _logger.LogError($"RequestId: {requestId} - CreateFolderAsync Service Exception: {ex}");
                throw new ResponseException($"RequestId: {requestId} - CreateFolderAsync Service Exception: {ex}");
            }
        }

        public async Task<JObject> CreateTempFolderAsync(string siteId, string folderName, string requestId = "")
        {
           _logger.LogInformation($"RequestId: {requestId} - CreateTempFolderAsync called.");
            try
            {
                Guard.Against.NullOrEmpty(siteId, nameof(siteId), requestId);
                Guard.Against.NullOrEmpty(folderName, nameof(folderName), requestId);

                // Add the folder.
                DriveItem folder = await GraphClient.Sites[siteId].Drive.Root.Children.Request().AddAsync(new DriveItem
                {
                    Name = folderName,
                    Folder = new Folder()
                });

                if (folder != null)
                {
                    _logger.LogInformation($"RequestId: {requestId} - CreateFolderAsync end.");
                    return JObject.FromObject(folder); ;
                }

                _logger.LogError($"RequestId: {requestId} - CreateTempFolderAsync error: response foler null for folder: {folder}");
                var errorResponse = JsonErrorResponse.BadRequest($"CreateTempFolderAsync error: response foler null for folder: {folder}", requestId);
                return errorResponse;
            }
            catch (Exception ex)
            {
                _logger.LogError($"RequestId: {requestId} - CreateTempFolderAsync Service Exception: {ex}");
                throw new ResponseException($"RequestId: {requestId} - CreateTempFolderAsync Service Exception: {ex}");
            }
        }

        public async Task<JObject> DeleteFileOrFolderAsync(string siteId, string itemPath, string requestId = "")
        {
            // DELETE https://graph.microsoft.com/v1.0/sites/{siteId}/drive/items/{itemId}
            // EXAMPLE: https://graph.microsoft.com/v1.0/sites/onterawe.sharepoint.com,e4330185-7583-4b11-bb2c-2a0a9196d7f6,4c366d80-b803-4f4e-8ccf-a58384fa35ec/drive/items/root:/children

            _logger.LogInformation($"RequestId: {requestId} - DeleteFolderAsync called.");
            try
            {
                Guard.Against.NullOrEmpty(siteId, nameof(siteId), requestId);
                Guard.Against.NullOrEmpty(itemPath, nameof(itemPath), requestId);

                //var requestUrl = $"{_appOptions.GraphRequestUrl}sites/{siteId}/drive/root:/{folder}/{file.FileName}:/content";

                // Delete the file or folder.
                await GraphClient.Sites[siteId].Drive.Root.ItemWithPath(itemPath).Request().DeleteAsync();

                return JObject.FromObject(ApplicationCore.StatusCodes.Status204NoContent);
            }
            catch (Exception ex)
            {
                _logger.LogError($"RequestId: {requestId} - DeleteFolderAsync Service Exception: {ex}");
                throw new ResponseException($"RequestId: {requestId} - DeleteFolderAsync Service Exception: {ex}");
            }
        }

        public async Task<JObject> UploadFileAsync(string siteId, string folder, IFormFile file, string requestId = "")
        {
            // PUT (replace file): https://graph.microsoft.com/v10.0/sites/{site-id}/drive/root:/{folder}/{filename}:/content
            // PUT (new file): https://graph.microsoft.com/v1.0/sites/onterawe.sharepoint.com,69f40286-aa2c-4959-a604-98e9b28f6d0c,ae164d4d-cfde-41b9-9715-ff4cd0f3cc57/drive/root:/General/FileB.txt:/content
            // EXAMPLE: 

            _logger.LogInformation($"RequestId: {requestId} - UploadFileAsync called.");
            try
            {
                Guard.Against.NullOrEmpty(siteId, nameof(siteId), requestId);
                Guard.Against.NullOrEmpty(folder, nameof(folder), requestId);
                Guard.Against.Null(file, nameof(file), requestId);

                var path = $"{folder}/{file.FileName}";

                // Check whether document exists, is so then make a copy of the existing one.
                try
                {
                    //File upload Error
                    var existingFile = await GraphClient.Sites[siteId].Drive.Root.ItemWithPath(path).Request().GetAsync();

                    if (existingFile != null)
                    {
                        var newName = $"{Path.GetFileNameWithoutExtension(file.FileName)}-{DateTime.Now.Ticks}{Path.GetExtension(file.FileName)}";
                        await GraphClient.Sites[siteId].Drive.Root.ItemWithPath(path).Copy(newName).Request().PostAsync();
                        _logger.LogInformation($"RequestId: {requestId} - UploadFileAsync old file created {newName}.");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogInformation($"RequestId: {requestId} - UploadFileAsync failed to create backup of file {file.FileName}: {ex.Message}.");
                }

                using (var fileStream = file.OpenReadStream())
                {
                    var resp = await GraphClient.Sites[siteId].Drive.Root.ItemWithPath(path).Content.Request().PutAsync<DriveItem>(fileStream);
                    var responseJObject = JObject.FromObject(resp);

                    _logger.LogInformation($"RequestId: {requestId} - UploadFileAsync end.");
                    return responseJObject;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"RequestId: {requestId} - UploadFileAsync Service Exception: {ex}");
                throw new ResponseException($"RequestId: {requestId} - UploadFileAsync Service Exception: {ex}");
            }
        }

        public async Task<JObject> MoveFileAsync(string fromSiteId, string fromItemPath, string toSiteId, string toItemPath, string requestId = "")
        {
            // GET
            // EXAMPLE: https://graph.microsoft.com/v1.0/sites/onterawe.sharepoint.com,e4330185-7583-4b11-bb2c-2a0a9196d7f6,4c366d80-b803-4f4e-8ccf-a58384fa35ec/drive/items/root:/children

            _logger.LogInformation($"RequestId: {requestId} - MoveFileAsync called.");
            try
            {
                Guard.Against.NullOrEmpty(fromSiteId, nameof(fromSiteId), requestId);
                Guard.Against.NullOrEmpty(fromItemPath, nameof(fromItemPath), requestId);
                Guard.Against.NullOrEmpty(toSiteId, nameof(toSiteId), requestId);
                Guard.Against.NullOrEmpty(toItemPath, nameof(toItemPath), requestId);

                //var requestUrl = $"{_appOptions.GraphRequestUrl}sites/{siteId}/drive/root:/{folder}/{file.FileName}:/content";

                // Get the file or folder.
                var file = await GraphClient.Sites[fromSiteId].Drive.Root.ItemWithPath(fromItemPath).Request().GetAsync();

                var resp = new DriveItem();
                if (file.File != null)
                {

                    // Get the file content.
                    using (Stream stream = await GraphClient.Sites[fromSiteId].Drive.Root.ItemWithPath(fromItemPath).Content.Request().GetAsync())
                    {
                        resp = await GraphClient.Sites[toSiteId].Drive.Root.ItemWithPath(toItemPath).Content.Request().PutAsync<DriveItem>(stream);
                    }

                    return JObject.FromObject(resp);
                }
                else
                {
                    // Selected item is not a file.
                    return JObject.FromObject(ApplicationCore.StatusCodes.Status405MethodNotAllowed);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"RequestId: {requestId} - MoveFileAsync Service Exception: {ex}");
                throw new ResponseException($"RequestId: {requestId} - MoveFileAsync Service Exception: {ex}");
            }
        }

        public async Task<string> GetSharePointRootId()
        {
            try
            {
                return await memoryCache.GetOrCreateAsync(SharePointRootSiteIdKey,
                    async x =>
                    {
                        var requestUrl = _appOptions.GraphRequestUrl + $"sites/{_appOptions.SharePointHostName}:/sites/{_appOptions.SharePointSiteRelativeName}?$select=id";

                        // Create the request message and add the content.
                        var hrm = new HttpRequestMessage(HttpMethod.Get, requestUrl);

                        // Authenticate (add access token) our HttpRequestMessage
                        await GraphClient.AuthenticationProvider.AuthenticateRequestAsync(hrm);

                        // Send the request and get the response.
                        var response = await GraphClient.HttpProvider.SendAsync(hrm);

                        if (response.IsSuccessStatusCode)
                        {
                            JObject result = await response.Content.ReadAsAsync<JObject>();
                            var token = result.SelectToken("id")?.ToObject<string>();
                            if (!string.IsNullOrWhiteSpace(token))
                            {
                                return token;
                            }
                            else
                            {
                                throw new Exception($"The SharePoint root site Id has not been found for '{_appOptions.SharePointSiteRelativeName}'");
                            }
                        }
                        else
                        {
                            throw new Exception($"Error retrieving SharePoint root id for '{_appOptions.SharePointSiteRelativeName}': {response.ReasonPhrase}");
                        }
                    });
            }
            catch (Exception ex)
            {
                _logger.LogError("Error retrieving SharePoint root id for '{_appOptions.SharePointSiteRelativeName}': " + ex.Message);
                throw ex;
            }
        }

        // Private methods
        private async Task<bool> TryGetSiteListAsync(SiteList siteList, string requestId = "")
        {
            try
            {
                // Call to Graph API to check if SharePoint List exists.
                var graphRequest = new List();
                graphRequest = await GraphClient.Sites[siteList.SiteId].Lists[siteList.ListId].Request().GetAsync();

                throw new ResponseException($"RequestId: {requestId} - TryGetSiteListAsync not implemented");
            }
            catch (Exception ex)
            {
                _logger.LogError($"RequestId: {requestId} - TryGetSiteListAsync Service Exception: {ex}");
                throw new ResponseException($"RequestId: {requestId} - TryGetSiteListAsync Service Exception: {ex}");
            }
        }
    }
}