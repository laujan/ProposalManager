// Copyright(c) Microsoft Corporation. 
// All rights reserved.
//
// Licensed under the MIT license. See LICENSE file in the solution root folder for full license information.

using AutoMapper;
using Microsoft.EntityFrameworkCore;
using ProjectSmartLink.Common;
using ProjectSmartLink.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ProjectSmartLink.Service
{
    public class SourceService : ISourceService
    {
        private readonly SmartlinkDbContext _dbContext;
        private readonly IMapper _mapper;
        private readonly ILogService _logService;
        private readonly IUserProfileService _userProfileService;

        public SourceService(SmartlinkDbContext dbContext, IMapper mapper, ILogService logService, IUserProfileService userProfileService)
        {
            _dbContext = dbContext;
            _mapper = mapper;
            _logService = logService;
            _userProfileService = userProfileService;
        }

        public async Task<SourcePoint> AddSourcePoint(string fileName, string documentId, SourcePoint sourcePoint)
        {
            try
            {
                var sourceCatalog = _dbContext.SourceCatalogs.FirstOrDefault(o => o.DocumentId == documentId);
                bool addSourceCatalog = (sourceCatalog == null);
                if (addSourceCatalog)
                {
                    try
                    {
                        var isExternal = Guid.TryParse(documentId, out Guid guid);
                        sourceCatalog = new SourceCatalog() { Name = fileName, DocumentId = documentId, IsExternal = isExternal };
                        _dbContext.SourceCatalogs.Add(sourceCatalog);
                    }
                    catch (Exception ex)
                    {
                        var entity = new LogEntity()
                        {
                            LogId = "30006",
                            Action = Constant.ACTIONTYPE_ADD,
                            ActionType = ActionTypeEnum.ErrorLog,
                            PointType = Constant.POINTTYPE_SOURCECATALOG,
                            Message = ".Net Error",
                        };
                        entity.Subject = $"{entity.LogId} - {entity.Action} - {entity.PointType} - Error";
                        _logService.WriteLog(entity);

                        throw new ApplicationException("Add Source Catalog failed", ex);
                    }
                }

                sourcePoint.Created = DateTime.Now.ToUniversalTime().ToPSTDateTime();
                sourcePoint.Creator = string.IsNullOrWhiteSpace(sourcePoint.Creator) ? _userProfileService.GetCurrentUser().Username : sourcePoint.Creator;

                sourceCatalog.SourcePoints.Add(sourcePoint);

                await _dbContext.SaveChangesAsync();

                _dbContext.PublishedHistories.Add(new PublishedHistory()
                {
                    Name = sourcePoint.Name,
                    Position = sourcePoint.Position,
                    Value = sourcePoint.Value,
                    PublishedDate = sourcePoint.Created,
                    PublishedUser = sourcePoint.Creator,
                    SourcePointId = sourcePoint.Id
                });

                await _dbContext.SaveChangesAsync();

                if (addSourceCatalog)
                {
                    _logService.WriteLog(new LogEntity()
                    {
                        LogId = "30003",
                        Action = Constant.ACTIONTYPE_ADD,
                        PointType = Constant.POINTTYPE_SOURCECATALOG,
                        ActionType = ActionTypeEnum.AuditLog,
                        Message = $"Add Source Catalog named {sourceCatalog.Name}."
                    });
                }
                _logService.WriteLog(new LogEntity()
                {
                    LogId = "10001",
                    Action = Constant.ACTIONTYPE_ADD,
                    PointType = Constant.POINTTYPE_SOURCEPOINT,
                    ActionType = ActionTypeEnum.AuditLog,
                    Message = $"Create source point named {sourcePoint.Name} in the location: {sourcePoint.Position}, value: {sourcePoint.Value} in the excel file named: {sourceCatalog.FileName} by {sourcePoint.Creator}"
                });

            }
            catch (ApplicationException ex)
            {
                throw ex.InnerException;
            }
            catch (Exception ex)
            {
                var logEntity = new LogEntity()
                {
                    LogId = "10005",
                    Action = Constant.ACTIONTYPE_ADD,
                    ActionType = ActionTypeEnum.ErrorLog,
                    PointType = Constant.POINTTYPE_SOURCEPOINT,
                    Message = ".Net Error",
                    Detail = ex.ToString()
                };
                logEntity.Subject = $"{logEntity.LogId} - {logEntity.Action} - {logEntity.PointType} - Error";
                _logService.WriteLog(logEntity);
                throw;
            }
            return sourcePoint;
        }

        public async Task<SourcePoint> EditSourcePoint(SourcePoint sourcePoint)
        {
            try
            {
                var previousSourcePoint = await _dbContext.SourcePoints.Include(o => o.Catalog).FirstOrDefaultAsync(o => o.Id == sourcePoint.Id);

                if (previousSourcePoint != null)
                {
                    previousSourcePoint.Name = sourcePoint.Name;
                    previousSourcePoint.Position = sourcePoint.Position;
                    previousSourcePoint.RangeId = sourcePoint.RangeId;
                    previousSourcePoint.Value = sourcePoint.Value;
                    previousSourcePoint.NamePosition = sourcePoint.NamePosition;
                    previousSourcePoint.NameRangeId = sourcePoint.NameRangeId;
                    previousSourcePoint.PublishedHistories = (await _dbContext.PublishedHistories.Where(o => o.SourcePointId == previousSourcePoint.Id).ToArrayAsync()).OrderByDescending(o => o.PublishedDate).ToArray();
                }

                await _dbContext.SaveChangesAsync();
                _logService.WriteLog(new LogEntity()
                {
                    LogId = "10002",
                    Action = Constant.ACTIONTYPE_EDIT,
                    PointType = Constant.POINTTYPE_SOURCEPOINT,
                    ActionType = ActionTypeEnum.AuditLog,
                    Message = $"Edit source point by {_userProfileService.GetCurrentUser().Username} Previous value: source point named: {previousSourcePoint.Name} in the location: {previousSourcePoint.Position} value: {previousSourcePoint.Value} in the excel file named: {previousSourcePoint.Catalog.FileName} " +
                              $"Current value: source point named: {sourcePoint.Name} in the location {sourcePoint.Position} value: {sourcePoint.Value} in the excel file name: {previousSourcePoint.Catalog.FileName}"
                });

                return previousSourcePoint;
            }
            catch (Exception ex)
            {
                var logEntity = new LogEntity()
                {
                    LogId = "10008",
                    Action = Constant.ACTIONTYPE_EDIT,
                    ActionType = ActionTypeEnum.ErrorLog,
                    PointType = Constant.POINTTYPE_SOURCEPOINT,
                    Message = ".Net Error",
                    Detail = ex.ToString()
                };
                logEntity.Subject = $"{logEntity.LogId} - {logEntity.Action} - {logEntity.PointType} - Error";
                _logService.WriteLog(logEntity);
                throw;
            }
        }
        public async Task<int> DeleteSourcePoint(Guid sourcePointId)
        {
            try
            {
                var sourcePoint = _dbContext.SourcePoints.Include(o => o.Catalog).FirstOrDefault(o => o.Id == sourcePointId);
                if (sourcePoint == null)
                {
                    throw new NullReferenceException(string.Format("Sourcepoint: {0} is not existed", sourcePointId));
                }
                if (sourcePoint.Status == SourcePointStatus.Deleted)
                {
                    return await Task.FromResult<int>(0);
                }
                else
                {
                    sourcePoint.Status = SourcePointStatus.Deleted;
                }
                var task = await _dbContext.SaveChangesAsync();
                _logService.WriteLog(new LogEntity()
                {
                    LogId = "10003",
                    Action = Constant.ACTIONTYPE_DELETE,
                    PointType = Constant.POINTTYPE_SOURCEPOINT,
                    ActionType = ActionTypeEnum.AuditLog,
                    Message = $"Delete source point named: {sourcePoint.Name} in the excel file named: {sourcePoint.Catalog.FileName}"
                });
                return task;
            }
            catch (Exception ex)
            {
                var logEntity = new LogEntity()
                {
                    LogId = "10006",
                    Action = Constant.ACTIONTYPE_DELETE,
                    ActionType = ActionTypeEnum.ErrorLog,
                    PointType = Constant.POINTTYPE_SOURCEPOINT,
                    Message = ".Net Error",
                    Detail = ex.ToString()
                };
                logEntity.Subject = $"{logEntity.LogId} - {logEntity.Action} - {logEntity.PointType} - Error";
                _logService.WriteLog(logEntity);
                throw;
            }

        }

        public async Task DeleteSelectedSourcePoint(IEnumerable<Guid> selectedSourcePointIds)
        {
            try
            {
                foreach (var sourcePointId in selectedSourcePointIds)
                {
                    var sourcePoint = _dbContext.SourcePoints.Include(o => o.Catalog).FirstOrDefault(o => o.Id == sourcePointId);
                    if (sourcePoint == null)
                    {
                        throw new NullReferenceException($"Sourcepoint: {sourcePointId} is not existed");
                    }
                    if (sourcePoint.Status != SourcePointStatus.Deleted)
                    {
                        sourcePoint.Status = SourcePointStatus.Deleted;
                    }
                    var task = await _dbContext.SaveChangesAsync();
                    _logService.WriteLog(new LogEntity()
                    {
                        LogId = "10003",
                        Action = Constant.ACTIONTYPE_DELETE,
                        PointType = Constant.POINTTYPE_SOURCEPOINT,
                        ActionType = ActionTypeEnum.AuditLog,
                        Message = $"Delete source point named: {sourcePoint.Name} in the excel file named: {sourcePoint.Catalog.FileName}"
                    });
                }
            }
            catch (Exception ex)
            {
                var logEntity = new LogEntity()
                {
                    LogId = "10006",
                    Action = Constant.ACTIONTYPE_DELETE,
                    ActionType = ActionTypeEnum.ErrorLog,
                    PointType = Constant.POINTTYPE_SOURCEPOINT,
                    Message = ".Net Error",
                    Detail = ex.ToString()
                };
                logEntity.Subject = $"{logEntity.LogId} - {logEntity.Action} - {logEntity.PointType} - Error";
                _logService.WriteLog(logEntity);
                throw;
            }

        }

        public async Task<SourceCatalog> GetSourceCatalog(string fileName, string documentId)
        {
            try
            {
                var sourceCatalog = await _dbContext.SourceCatalogs.Where(o => o.DocumentId == documentId).FirstOrDefaultAsync();
                if (sourceCatalog != null)
                {
                    var sourcePointArray = (await _dbContext.SourcePoints.Where(o => o.Status == SourcePointStatus.Created && o.CatalogId == sourceCatalog.Id)
                         .Include(o => o.DestinationPoints).ToArrayAsync())
                         .OrderByDescending(o => o.Name).ToArray();
                    var sourcePointIds = sourcePointArray.Select(point => point.Id).ToArray();
                    var publishedHistories = await (from pb in _dbContext.PublishedHistories
                                                    where sourcePointIds.Contains(pb.SourcePointId)
                                                    select pb).ToArrayAsync();
                    foreach (var item in sourcePointArray)
                    {
                        item.PublishedHistories = publishedHistories.Where(pb => pb.SourcePointId == item.Id)
                                                                    .OrderByDescending(pb => pb.PublishedDate).ToArray();
                    }
                    sourceCatalog.SourcePoints = sourcePointArray;

                    if (!sourceCatalog.Name.Equals(fileName, StringComparison.OrdinalIgnoreCase))
                    {
                        sourceCatalog.Name = fileName;
                        await _dbContext.SaveChangesAsync();
                    }

                    _logService.WriteLog(new LogEntity()
                    {
                        LogId = "30001",
                        Action = Constant.ACTIONTYPE_GET,
                        PointType = Constant.POINTTYPE_SOURCECATALOG,
                        ActionType = ActionTypeEnum.AuditLog,
                        Message = $"Get source catalog named {sourceCatalog.Name}"
                    });
                }
                return sourceCatalog;
            }
            catch (Exception ex)
            {
                var logEntity = new LogEntity()
                {
                    LogId = "30004",
                    Action = Constant.ACTIONTYPE_GET,
                    ActionType = ActionTypeEnum.ErrorLog,
                    PointType = Constant.POINTTYPE_SOURCECATALOG,
                    Message = ".Net Error",
                    Detail = ex.ToString()
                };
                logEntity.Subject = $"{logEntity.LogId} - {logEntity.Action} - {logEntity.PointType} - Error";
                _logService.WriteLog(logEntity);
                throw;
            }
        }

        public async Task<IEnumerable<SourceCatalog>> GetSourceCatalogs(bool external)
        {
            try
            {
                var allCatalogs = _dbContext.SourceCatalogs.Where(d => d.IsExternal == external);
                return await allCatalogs.OrderBy(x => x.Name).ToListAsync();
            }
            catch (Exception ex)
            {
                var logEntity = new LogEntity()
                {
                    LogId = "30004",
                    Action = Constant.ACTIONTYPE_GET,
                    ActionType = ActionTypeEnum.ErrorLog,
                    PointType = Constant.POINTTYPE_SOURCECATALOG,
                    Message = ".Net Error",
                    Detail = ex.ToString()
                };
                logEntity.Subject = $"{logEntity.LogId} - {logEntity.Action} - {logEntity.PointType} - Error";
                _logService.WriteLog(logEntity);
                throw;
            }
        }

        public async Task<SourceCatalog> GetSourceCatalog(string documentId)
        {
            try
            {
                var sourceCatalog = await _dbContext.SourceCatalogs.Where(o => o.DocumentId == documentId).FirstOrDefaultAsync();
                if (sourceCatalog != null)
                {
                    var sourcePointArray = (await _dbContext.SourcePoints.Where(o => o.Status == SourcePointStatus.Created && o.CatalogId == sourceCatalog.Id)
                         .Include(o => o.DestinationPoints).ToArrayAsync())
                         .OrderByDescending(o => o.Name).ToArray();
                    var sourcePointIds = sourcePointArray.Select(point => point.Id).ToArray();
                    var publishedHistories = await (from pb in _dbContext.PublishedHistories
                                                    where sourcePointIds.Contains(pb.SourcePointId)
                                                    select pb).ToArrayAsync();
                    foreach (var item in sourcePointArray)
                    {
                        item.PublishedHistories = publishedHistories.Where(pb => pb.SourcePointId == item.Id)
                                                                    .OrderByDescending(pb => pb.PublishedDate).ToArray();
                    }
                    sourceCatalog.SourcePoints = sourcePointArray;

                    _logService.WriteLog(new LogEntity()
                    {
                        LogId = "30001",
                        Action = Constant.ACTIONTYPE_GET,
                        PointType = Constant.POINTTYPE_SOURCECATALOG,
                        ActionType = ActionTypeEnum.AuditLog,
                        Message = $"Get source catalog named {sourceCatalog.Name}"
                    });
                }
                return sourceCatalog;
            }
            catch (Exception ex)
            {
                var logEntity = new LogEntity()
                {
                    LogId = "30004",
                    Action = Constant.ACTIONTYPE_GET,
                    ActionType = ActionTypeEnum.ErrorLog,
                    PointType = Constant.POINTTYPE_SOURCECATALOG,
                    Message = ".Net Error",
                    Detail = ex.ToString()
                };
                logEntity.Subject = $"{logEntity.LogId} - {logEntity.Action} - {logEntity.PointType} - Error";
                _logService.WriteLog(logEntity);
                throw;
            }
        }

        public async Task<PublishSourcePointResult> PublishSourcePointList(IEnumerable<PublishSourcePointForm> publishSourcePointForms)
        {
            try
            {
                var sourcePointIdList = publishSourcePointForms.Select(o => o.SourcePointId).ToArray();
                var sourcePointList = _dbContext.SourcePoints.Include(o => o.Catalog).Where(o => sourcePointIdList.Contains(o.Id)).ToList();
                var currentUser = _userProfileService.GetCurrentUser();

                //Update database
                IList<PublishedHistory> histories = new List<PublishedHistory>();
                foreach (var sourcePoint in sourcePointList)
                {
                    var sourcePointForm = publishSourcePointForms.First(o => o.SourcePointId == sourcePoint.Id);
                    sourcePoint.Value = sourcePointForm.CurrentValue;
                    sourcePoint.Position = sourcePointForm.Position;
                    sourcePoint.Name = sourcePointForm.Name;
                    sourcePoint.NamePosition = sourcePointForm.NamePosition;

                    var history = new PublishedHistory()
                    {
                        Name = sourcePoint.Name,
                        Position = sourcePoint.Position,
                        Value = sourcePoint.Value,
                        PublishedDate = DateTime.Now.ToUniversalTime().ToPSTDateTime(),
                        PublishedUser = currentUser.Username,
                        SourcePointId = sourcePoint.Id
                    };

                    _dbContext.PublishedHistories.Add(history);

                    histories.Add(history);
                }
                await _dbContext.SaveChangesAsync();

                return new PublishSourcePointResult() { BatchId = new Guid(), SourcePoints = sourcePointList };
            }
            catch (Exception ex)
            {
                var logEntity = new LogEntity()
                {
                    LogId = "10007",
                    Action = Constant.ACTIONTYPE_PUBLISH,
                    ActionType = ActionTypeEnum.ErrorLog,
                    PointType = Constant.POINTTYPE_SOURCEPOINT,
                    Message = ".Net Error",
                    Detail = ex.ToString()
                };
                logEntity.Subject = $"{logEntity.LogId} - {logEntity.Action} - {logEntity.PointType} - Error";
                _logService.WriteLog(logEntity);
                throw;
            }
        }

        public async Task<PublishedHistory> GetPublishHistoryById(Guid publishHistoryId)
        {
            var publishHistory = await _dbContext.PublishedHistories.Include(o => o.SourcePoint.Catalog).FirstOrDefaultAsync(o => o.Id == publishHistoryId);
            if (publishHistory != null)
            {
                publishHistory.SourcePoint.SerializeCatalog = true;
                publishHistory.SourcePoint.Catalog.SerializeSourcePoints = false;
            }
            return publishHistory;
        }

        public async Task<IEnumerable<DocumentCheckResult>> GetAllCatalogs()
        {
            var sourceCatalogs = await _dbContext.SourceCatalogs.Where(o => !string.IsNullOrEmpty(o.DocumentId)).ToListAsync();
            var destinationCatalogs = await _dbContext.DestinationCatalogs.Where(o => !string.IsNullOrEmpty(o.DocumentId)).ToListAsync();
            var catalog = new List<DocumentCheckResult>();
            catalog.AddRange(sourceCatalogs.Select(o => new DocumentCheckResult() { DocumentId = o.DocumentId, DocumentType = DocumentType.SourcePoint }).ToList());
            catalog.AddRange(destinationCatalogs.Select(o => new DocumentCheckResult() { DocumentId = o.DocumentId, DocumentType = DocumentType.DestinationPoint }).ToList());
            return catalog;
        }

        public async Task<IEnumerable<DocumentCheckResult>> UpdateDocumentUrlById(IEnumerable<DocumentCheckResult> documents)
        {
            foreach (var document in documents)
            {
                if (document.DocumentType == DocumentType.SourcePoint)
                {
                    var catalog = await _dbContext.SourceCatalogs.Where(o => o.DocumentId == document.DocumentId).FirstOrDefaultAsync();
                    if (catalog != null)
                    {
                        if (document.IsDeleted)
                        {
                            document.IsUpdated = true;
                            document.Message = string.Format("DocumentId: {0}, The file {1} has been deleted", document.DocumentId, catalog.Name);
                            catalog.IsDeleted = true;
                        }
                        else
                        {
                            if (!catalog.Name.Equals(document.DocumentUrl, StringComparison.OrdinalIgnoreCase))
                            {
                                document.IsUpdated = true;
                                document.Message = string.Format("DocumentId: {0}, Updated from {1} to {2}", document.DocumentId, catalog.Name, document.DocumentUrl);
                                catalog.Name = document.DocumentUrl;
                                catalog.IsDeleted = false;
                            }
                            else
                            {
                                if (catalog.IsDeleted)
                                {
                                    document.IsUpdated = true;
                                    catalog.IsDeleted = false;
                                }
                            }
                        }
                    }
                }
                else if (document.DocumentType == DocumentType.DestinationPoint)
                {
                    var catalog = await _dbContext.DestinationCatalogs.Where(o => o.DocumentId == document.DocumentId).FirstOrDefaultAsync();
                    if (catalog != null)
                    {
                        if (document.IsDeleted)
                        {
                            document.IsUpdated = true;
                            document.Message = $"DocumentId: {document.DocumentId}, The file {catalog.Name} has been deleted";
                            catalog.IsDeleted = true;
                        }
                        else
                        {
                            if (!catalog.Name.Equals(document.DocumentUrl, StringComparison.OrdinalIgnoreCase))
                            {
                                document.IsUpdated = true;
                                document.Message = $"DocumentId: {document.DocumentId}, Updated from {catalog.Name} to {document.DocumentUrl}";
                                catalog.Name = document.DocumentUrl;
                                catalog.IsDeleted = false;
                            }
                            else
                            {
                                if (catalog.IsDeleted)
                                {
                                    document.IsUpdated = true;
                                    catalog.IsDeleted = false;
                                }
                            }
                        }
                    }
                }
            }
            if (documents.Any(o => o.IsUpdated))
            {
                await _dbContext.SaveChangesAsync();
            }
            return documents;
        }

        public async Task<IEnumerable<CloneForm>> CheckCloneFileStatus(IEnumerable<CloneForm> files)
        {
            var destinationCatalogIds = new List<string>();
            foreach (var item in files)
            {
                if (item.IsExcel)
                {
                    var catalog = await _dbContext.SourceCatalogs.Where(o => o.DocumentId.Equals(item.DocumentId, StringComparison.CurrentCultureIgnoreCase)).FirstOrDefaultAsync();
                    if (catalog != null)
                    {
                        var sourcePoints = await _dbContext.SourcePoints.Where(o => o.Status == SourcePointStatus.Created && o.CatalogId == catalog.Id)
                        .Include(o => o.DestinationPoints)
                        .ToArrayAsync();
                        if (sourcePoints.Count() > 0)
                        {
                            item.Clone = true;
                            foreach (var sourcePoint in sourcePoints)
                            {
                                foreach (var destinationPoint in sourcePoint.DestinationPoints)
                                {
                                    var destinationCatalog = await _dbContext.DestinationCatalogs.Where(o => o.Id.Equals(destinationPoint.CatalogId)).FirstOrDefaultAsync();
                                    if (destinationCatalog != null)
                                    {
                                        destinationCatalogIds.Add(destinationCatalog.DocumentId);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            foreach (var item in files)
            {
                if (item.IsWord)
                {
                    if (destinationCatalogIds.Any(o => o.Equals(item.DocumentId, StringComparison.CurrentCultureIgnoreCase)))
                    {
                        item.Clone = true;
                    }
                }
            }
            return files;
        }
    }
}
