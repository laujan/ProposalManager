// Copyright(c) Microsoft Corporation. 
// All rights reserved.
//
// Licensed under the MIT license. See LICENSE file in the solution root folder for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using ProjectSmartLink.Entity;
using Microsoft.EntityFrameworkCore;

namespace ProjectSmartLink.Service
{
    public class RecentFileService : IRecentFileService
    {
        private readonly SmartlinkDbContext _dbContext;
        private readonly IMapper _mapper;
        private readonly ILogService _logService;
        private readonly IUserProfileService _userProfileService;

        public RecentFileService(SmartlinkDbContext dbContext, IMapper mapper, ILogService logService, IUserProfileService userProfileService)
        {
            _dbContext = dbContext;
            _mapper = mapper;
            _logService = logService;
            _userProfileService = userProfileService;
        }

        public async Task<IEnumerable<SourceCatalog>> GetRecentFiles()
        {
            var userName = _userProfileService.GetCurrentUser().Username;
            var allRecentFiles = await _dbContext.RecentFiles.Where(o => o.User.Equals(userName, StringComparison.CurrentCultureIgnoreCase))
                .Include(o => o.Catalog)
                .OrderByDescending(o => o.Date)
                .Select(o => o.Catalog).Where(o => !o.IsDeleted && !o.IsExternal).ToArrayAsync();

            var recentFiles = allRecentFiles.Distinct().Take(5);

            foreach (var catalog in recentFiles)
            {
                catalog.SourcePoints = (await _dbContext.SourcePoints.Where(o => o.Status == SourcePointStatus.Created && o.CatalogId == catalog.Id)
                    .Include(o => o.DestinationPoints).ToArrayAsync())
                    .OrderByDescending(o => o.Name).ToArray();
            }
            return recentFiles;
        }

        public async Task<IEnumerable<SourceCatalog>> AddRecentFile(SourceCatalog sourceCatalog)
        {
            var catalog = await _dbContext.SourceCatalogs.FirstOrDefaultAsync(o => o.DocumentId == sourceCatalog.DocumentId);
            if (catalog == null)
            {
                catalog = new SourceCatalog() { Name = sourceCatalog.Name, DocumentId = sourceCatalog.DocumentId };
                _dbContext.SourceCatalogs.Add(catalog);
            }
            var recentFile = new RecentFile();
            recentFile.User = _userProfileService.GetCurrentUser().Username;
            recentFile.Date = DateTime.Now.ToUniversalTime().ToPSTDateTime();
            recentFile.CatalogId = catalog.Id;
            _dbContext.RecentFiles.Add(recentFile);

            await _dbContext.SaveChangesAsync();

            return await GetRecentFiles();
        }
    }

}
