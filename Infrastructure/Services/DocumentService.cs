﻿// Copyright(c) Microsoft Corporation. 
// All rights reserved.
//
// Licensed under the MIT license. See LICENSE file in the solution root folder for full license information

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ApplicationCore.Interfaces;
using ApplicationCore;
using ApplicationCore.Helpers;
using Newtonsoft.Json.Linq;
using Microsoft.AspNetCore.Http;
using ApplicationCore.Helpers.Exceptions;

namespace Infrastructure.Services
{
    public class DocumentService : BaseService<DocumentService>, IDocumentService
    {
        private readonly IDocumentRepository _documentRepository;

        public DocumentService(
            ILogger<DocumentService> logger, 
            IOptionsMonitor<AppOptions> appOptions,
            IDocumentRepository documentRepository) : base(logger, appOptions)
        {
            Guard.Against.Null(documentRepository, nameof(documentRepository));
            _documentRepository = documentRepository;
        }

        public async Task<JObject> UploadDocumentAsync(string siteId, string folder, IFormFile file, string requestId = "")
        {
            _logger.LogInformation($"RequestId: {requestId} - DocumentService_UploadDocumentAsync called.");

            try
            {
                Guard.Against.NullOrEmpty(siteId, nameof(siteId), requestId);
                Guard.Against.NullOrEmpty(folder, nameof(folder), requestId);
                Guard.Against.Null(file, nameof(file), requestId);

                return await _documentRepository.UploadDocumentAsync(siteId, folder, file, requestId);
            }
            catch (Exception ex)
            {
                _logger.LogError($"RequestId: {requestId} - DocumentService_UploadDocumentAsync Service Exception: {ex}");
                throw new ResponseException($"RequestId: {requestId} - DocumentService_UploadDocumentAsync Service Exception: {ex}");
            }
        }

        public async Task<JObject> CreateTempFolderAsync(string siteId, string folderName, string requestId = "")
        {
            _logger.LogInformation($"RequestId: {requestId} - DocumentService_CreateFolderAsync called.");

            try
            {
                Guard.Against.NullOrEmpty(siteId, nameof(siteId), requestId);
                Guard.Against.NullOrEmpty(folderName, nameof(folderName), requestId);

                return await _documentRepository.CreateTempFolderAsync(siteId, folderName);
            }
            catch (Exception ex)
            {
                _logger.LogError($"RequestId: {requestId} - DocumentService_CreateFolderAsync Service Exception: {ex}");
                throw new ResponseException($"RequestId: {requestId} - DocumentService_CreateFolderAsync Service Exception: {ex}");
            }
        }

        public async Task<JObject> UploadDocumentTeamAsync(string opportunityName, string docType, IFormFile file, string requestId = "")
        {
            _logger.LogInformation($"RequestId: {requestId} - DocumentService_UploadDocumentTeamAsync called.");

            try
            {
                Guard.Against.NullOrEmpty(opportunityName, nameof(opportunityName), requestId);
                Guard.Against.NullOrEmpty(docType, nameof(docType), requestId);
                Guard.Against.Null(file, nameof(file), requestId);

                return await _documentRepository.UploadDocumentTeamAsync(opportunityName, docType, file, requestId);

            }
            catch (Exception ex)
            {
                _logger.LogError($"RequestId: {requestId} - DocumentService_UploadDocumentTeamAsync Service Exception: {ex}");
                throw new ResponseException($"RequestId: {requestId} - DocumentService_UploadDocumentTeamAsync Service Exception: {ex}");
            }
        }
    }
}
