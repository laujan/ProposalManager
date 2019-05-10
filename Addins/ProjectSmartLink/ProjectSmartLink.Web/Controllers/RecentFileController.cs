// Copyright(c) Microsoft Corporation. 
// All rights reserved.
//
// Licensed under the MIT license. See LICENSE file in the solution root folder for full license information.

using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using ProjectSmartLink.Entity;
using ProjectSmartLink.Service;
using ProjectSmartLink.Web.Models;
using System;
using System.Threading.Tasks;
using System.Web;

namespace ProjectSmartLink.Web.Controllers
{
    public class RecentFileController : Controller
    {
        private readonly IRecentFileService _recentFileService;
        private readonly IMapper _mapper;
        public RecentFileController(IRecentFileService recentFileService, IMapper mapper)
        {
            _recentFileService = recentFileService;
            _mapper = mapper;
        }

        [HttpGet]
        [Route("api/RecentFiles")]
        public async Task<IActionResult> GetRecentFilesAsync()
        {
            var retValue = await _recentFileService.GetRecentFiles();
            return Ok(retValue);
        }

        [HttpPost]
        [Route("api/RecentFile")]
        public async Task<IActionResult> PostAsync([FromForm]CatalogViewModel catalogAdded)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest("Invalid posted data.");
            }

            try
            {
                var catalogName = HttpUtility.UrlDecode(catalogAdded.Name);
                var documentId = HttpUtility.UrlDecode(catalogAdded.DocumentId);
                return Ok(await _recentFileService.AddRecentFile(new SourceCatalog() { Name = catalogName, DocumentId = documentId }));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.ToString());
            }
        }
    }
}