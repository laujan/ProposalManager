// Copyright(c) Microsoft Corporation. 
// All rights reserved.
//
// Licensed under the MIT license. See LICENSE file in the solution root folder for full license information

using System;
using System.Threading.Tasks;
using ApplicationCore;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ApplicationCore.Interfaces;
using ApplicationCore.Helpers;
using Microsoft.AspNetCore.Authorization;

namespace WebReact.Api
{
    [Authorize(AuthenticationSchemes = "AzureAdBearer")]
	public class DocumentController : BaseApiController<DocumentController>
    {
        private readonly IDocumentService _documentService;

        public DocumentController(
            ILogger<DocumentController> logger, 
            IOptions<AppOptions> appOptions,
            IDocumentService documentService) : base(logger, appOptions)
        {
            Guard.Against.Null(documentService, nameof(documentService));

            _documentService = documentService;
        }

        [Authorize]
        [HttpPost("CreateTempFolder/{siteId}/{folder}")]
        public async Task<IActionResult> CreateTempFolderAsync(string siteId, string folder)
        {
            var requestId = Guid.NewGuid().ToString();
            _logger.LogInformation($"RequestID:{requestId} - Document_UploadFile called.");

            try
            {
                Guard.Against.NullOrUndefined(siteId, "Document_UploadFile_siteId", requestId);
                Guard.Against.NullOrUndefined(folder, "Document_UploadFile_folder", requestId);

                return Ok(await _documentService.CreateTempFolderAsync(siteId, folder, requestId));
            }
            catch (Exception ex)
            {
                _logger.LogError($"RequestID:{requestId} - Document_UploadFile error: {ex.Message}");
                return BadRequest(JsonErrorResponse.BadRequest($"Document_UploadFile error: {ex.Message} ", requestId));
            }
        }

        // Put: /Document/UploadFile
        [Authorize]
        [HttpPut("UploadFile/{opportunityName}/{docType}")]
        public async Task<IActionResult> UploadFile(IFormFile file, string opportunityName, string docType)
        {
            var requestId = Guid.NewGuid().ToString();
            _logger.LogInformation($"RequestID:{requestId} - Document_UploadFile called.");

            try
            {
                Guard.Against.Null(file, "Document_UploadFile_file is null", requestId);
                Guard.Against.NullOrUndefined(opportunityName, "Document_UploadFile_opportunityName", requestId);
                Guard.Against.NullOrUndefined(docType, "Document_UploadFile_docType", requestId);

                var resp = await _documentService.UploadDocumentTeamAsync(opportunityName, docType, file, requestId);

                // TODO: check content of resp to see if there is an error

                return Ok(resp);
            }
            catch (Exception ex)
            {
                _logger.LogError($"RequestID:{requestId} - Document_UploadFile error: {ex.Message}");
                var errorResponse = JsonErrorResponse.BadRequest($"Document_UploadFile error: {ex.Message} ", requestId);

                return BadRequest(errorResponse);
            }
        }
    }
}
