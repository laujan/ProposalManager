// Copyright(c) Microsoft Corporation. 
// All rights reserved.
//
// Licensed under the MIT license. See LICENSE file in the solution root folder for full license information.

using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using ProjectSmartLink.Entity;
using ProjectSmartLink.Service;
using ProjectSmartLink.Web.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace ProjectSmartLink.Web.Controllers
{
	[Authorize]
    public class SourcePointController : BaseController
    {
        protected readonly ISourceService _sourceService;
        protected readonly IMapper _mapper;
        public SourcePointController(IConfiguration config, ISourceService sourceService, IMapper mapper) :
			base(config)
        {
            _sourceService = sourceService;
            _mapper = mapper;
        }

        [HttpPost]
        [Route("api/SourcePoint")]
        public async Task<IActionResult> Post([FromForm]SourcePointForm sourcePointAdded)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest("Invalid posted data.");
            }

            try
            {
                var sourcePoint = _mapper.Map<SourcePoint>(sourcePointAdded);
                var catalogName = HttpUtility.UrlDecode(sourcePointAdded.CatalogName);
                var documentId = HttpUtility.UrlDecode(sourcePointAdded.DocumentId);
                return Ok(await _sourceService.AddSourcePoint(catalogName, documentId, sourcePoint));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.ToString());
            }
        }

        [HttpGet]
        [Route("api/SourcePointCatalog")]
        public async Task<IActionResult> GetSourcePointCatalog(string fileName, string documentId)
        {
			if(string.IsNullOrWhiteSpace(fileName))
			{
				return Ok(await _sourceService.GetSourceCatalog(HttpUtility.UrlDecode(documentId)));
			}


			return Ok(await _sourceService.GetSourceCatalog(HttpUtility.UrlDecode(fileName), HttpUtility.UrlDecode(documentId)));
        }

        [HttpGet]
        [Route("api/SourcePointCatalogs")]
        public async Task<IActionResult> GetSourcePointCatalogs(bool external = false)
        {
            var retValue = await _sourceService.GetSourceCatalogs(external);
            return Ok(retValue);
        }

        [HttpPost]
        [Route("api/PublishSourcePoints")]
        public async Task<IActionResult> PublishSourcePoints([FromForm]IEnumerable<PublishSourcePointForm> sourcePointPublishForm)
        {
            if (!ModelState.IsValid || sourcePointPublishForm.Count() == 0)
            {
                return BadRequest("Invalid posted data.");
            }

            var retValue = await _sourceService.PublishSourcePointList(sourcePointPublishForm);

            return Ok(retValue);
        }

        [HttpPut]
        [Route("api/SourcePoint")]
        public async Task<IActionResult> EditSourcePoint([FromForm]SourcePointForm sourcePointAdded)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest("Invalid posted data.");
            }

            try
            {
                var sourcePoint = _mapper.Map<SourcePoint>(sourcePointAdded);

                return Ok(await _sourceService.EditSourcePoint(sourcePoint));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.ToString());
            }
        }

        [HttpDelete]
        [Route("api/SourcePoint")]
        public async Task<IActionResult> DeleteSourcePoint(string id)
        {
            var retValue = await _sourceService.DeleteSourcePoint(new Guid(id));
            return Ok();
        }

        [HttpPost]
        [Route("api/DeleteSelectedSourcePoint")]
        public async Task<IActionResult> DeleteSelectedSourcePoint([FromForm]IEnumerable<Guid> seletedIds)
        {
            await _sourceService.DeleteSelectedSourcePoint(seletedIds);
            return Ok();
        }

        [HttpPost]
        [Route("api/CloneCheckFile")]
        public async Task<IActionResult> CloneCheckFile([FromForm]IEnumerable<CloneForm> files)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest("Invalid posted data.");
            }

            try
            {
                return Ok(await _sourceService.CheckCloneFileStatus(files));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.ToString());
            }
        }

        [HttpPost]
        [Route("api/CloneFiles")]
        public async Task<IActionResult> CloneFiles([FromForm]IEnumerable<CloneForm> files)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest("Invalid posted data.");
            }

            try
            {
                foreach (var item in files)
                {
                    item.DestinationFileUrl = HttpUtility.UrlDecode(item.DestinationFileUrl);
                }
                await _sourceService.CloneFiles(files);
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.ToString());
            }
        }
    }
}