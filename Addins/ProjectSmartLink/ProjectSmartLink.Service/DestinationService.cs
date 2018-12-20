// Copyright(c) Microsoft Corporation. 
// All rights reserved.
//
// Licensed under the MIT license. See LICENSE file in the solution root folder for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ProjectSmartLink.Entity;
using AutoMapper;
using ProjectSmartLink.Common;
using Microsoft.EntityFrameworkCore;

namespace ProjectSmartLink.Service
{
    public class DestinationService : IDestinationService
    {
        protected readonly SmartlinkDbContext _dbContext;
        protected readonly IMapper _mapper;
        protected readonly ILogService _logService;
        protected readonly IUserProfileService _userProfileService;

        public DestinationService(SmartlinkDbContext dbContext, IMapper mapper, ILogService logService, IUserProfileService userProfileService)
        {
            _dbContext = dbContext;
            _mapper = mapper;
            _logService = logService;
            _userProfileService = userProfileService;
        }

        public async Task<DestinationPoint> AddDestinationPoint(string fileName, string documentId, DestinationPoint destinationPoint)
        {
            try
            {
                var destinationCatalog = _dbContext.DestinationCatalogs.FirstOrDefault(o => o.DocumentId == documentId);
                bool addDestinationCatalog = (destinationCatalog == null);
                if (addDestinationCatalog)
                {
                    try
                    {
                        destinationCatalog = new DestinationCatalog() { Name = fileName, DocumentId = documentId };
                        _dbContext.DestinationCatalogs.Add(destinationCatalog);
                    }
                    catch (Exception ex)
                    {
                        var entity = new LogEntity()
                        {
                            LogId = "40002",
                            Action = Constant.ACTIONTYPE_ADD,
                            ActionType = ActionTypeEnum.ErrorLog,
                            PointType = Constant.POINTYTPE_DESTINATIONCATALOG,
                            Message = ".Net Error",
                        };
                        entity.Subject = $"{entity.LogId} - {entity.Action} - {entity.PointType} - Error";
                        await _logService.WriteLog(entity);

                        throw new ApplicationException("Add Source Catalog failed", ex);
                    }
                }

                destinationPoint.Created = DateTime.Now.ToUniversalTime().ToPSTDateTime();
                destinationPoint.Creator = _userProfileService.GetCurrentUser().Username;

                destinationCatalog.DestinationPoints.Add(destinationPoint);

                _dbContext.SourcePoints.Attach(destinationPoint.ReferencedSourcePoint);
                _dbContext.DestinationPoints.Add(destinationPoint);

                await _dbContext.Entry(destinationPoint.ReferencedSourcePoint).ReloadAsync();

                // Add recent files
                var recentFile = new RecentFile();
                recentFile.User = _userProfileService.GetCurrentUser().Username;
                recentFile.Date = DateTime.Now.ToUniversalTime().ToPSTDateTime();
                recentFile.CatalogId = (await _dbContext.SourcePoints.FirstOrDefaultAsync(o => o.Id == destinationPoint.ReferencedSourcePoint.Id)).CatalogId;
                _dbContext.RecentFiles.Add(recentFile);

                await _dbContext.SaveChangesAsync();

                if (addDestinationCatalog)
                {
                    await _logService.WriteLog(new LogEntity()
                    {
                        LogId = "40001",
                        Action = Constant.ACTIONTYPE_ADD,
                        PointType = Constant.POINTYTPE_DESTINATIONCATALOG,
                        ActionType = ActionTypeEnum.AuditLog,
                        Message = $"Add destination catalog {destinationCatalog.Name}."
                    });
                }
                await _logService.WriteLog(new LogEntity()
                {
                    LogId = "20001",
                    Action = Constant.ACTIONTYPE_ADD,
                    PointType = Constant.POINTTYPE_DESTINATIONPOINT,
                    ActionType = ActionTypeEnum.AuditLog,
                    Message = $"Create destination point value: {destinationPoint.ReferencedSourcePoint.Value} in the word file named:{destinationCatalog.FileName} by {_userProfileService.GetCurrentUser().Username}"
                });

                destinationPoint = await _dbContext.DestinationPoints
                    .Include(o => o.ReferencedSourcePoint)
                    .Include(o => o.ReferencedSourcePoint.Catalog)
                    .Include(o => o.DestinationPointCustomFormats).ThenInclude(x => x.CustomFormat).FirstAsync(o => o.Id == destinationPoint.Id);
                destinationPoint.ReferencedSourcePoint.PublishedHistories = (await _dbContext.PublishedHistories.Where(o => o.SourcePointId == destinationPoint.ReferencedSourcePoint.Id).ToArrayAsync()).OrderByDescending(p => p.PublishedDate).ToArray();
                destinationPoint.ReferencedSourcePoint.SerializeCatalog = true;
                destinationPoint.ReferencedSourcePoint.Catalog.SerializeSourcePoints = false;
                destinationPoint.DestinationPointCustomFormats = destinationPoint.DestinationPointCustomFormats;

                await _logService.WriteLog(new LogEntity()
                {
                    LogId = "30002",
                    Action = Constant.ACTIONTYPE_GET,
                    PointType = Constant.POINTTYPE_SOURCECATALOGLIST,
                    ActionType = ActionTypeEnum.AuditLog,
                    Message = $"Get source catalogs."
                });
                return destinationPoint;
            }
            catch (ApplicationException ex)
            {
                throw ex.InnerException;
            }
            catch (Exception ex)
            {
                var logEntity = new LogEntity()
                {
                    LogId = "20003",
                    Action = Constant.ACTIONTYPE_ADD,
                    ActionType = ActionTypeEnum.ErrorLog,
                    PointType = Constant.POINTTYPE_DESTINATIONPOINT,
                    Message = ".Net Error",
                    Detail = ex.ToString()
                };
                logEntity.Subject = $"{logEntity.LogId} - {logEntity.Action} - {logEntity.PointType} - Error";
                await _logService.WriteLog(logEntity);
                throw ex;
            }
        }

        public async Task DeleteDestinationPoint(Guid destinationPointId)
        {
            try
            {
                var destinationPoint = await _dbContext.DestinationPoints
                    .Include(o => o.ReferencedSourcePoint)
                    .Include(o => o.Catalog).FirstOrDefaultAsync(o => o.Id == destinationPointId);

                if (destinationPoint != null)
                {
                    var logResult = new
                    {
                        location = destinationPoint.RangeId,
                        value = destinationPoint.ReferencedSourcePoint.Value,
                        fileName = destinationPoint.Catalog.FileName,
                        user = _userProfileService.GetCurrentUser().Username
                    };

                    _dbContext.DestinationPoints.Remove(destinationPoint);
                    await _dbContext.SaveChangesAsync();
                    await _logService.WriteLog(new LogEntity()
                    {
                        LogId = "20002",
                        Action = Constant.ACTIONTYPE_DELETE,
                        PointType = Constant.POINTTYPE_DESTINATIONPOINT,
                        ActionType = ActionTypeEnum.AuditLog,
                        Message = $"Delete destination point in the location {logResult.location}, value: {logResult.value} in the word file named:{logResult.fileName} by {logResult.user}"
                    });
                }
            }
            catch (Exception ex)
            {
                var logEntity = new LogEntity()
                {
                    LogId = "20003",
                    Action = Constant.ACTIONTYPE_DELETE,
                    ActionType = ActionTypeEnum.ErrorLog,
                    PointType = Constant.POINTTYPE_DESTINATIONPOINT,
                    Message = ".Net Error",
                    Detail = ex.ToString()
                };
                logEntity.Subject = $"{logEntity.LogId} - {logEntity.Action} - {logEntity.PointType} - Error";
                await _logService.WriteLog(logEntity);
                throw;
            }
        }

        public async Task DeleteSelectedDestinationPoint(IEnumerable<Guid> seletedDestinationPointIds)
        {
            try
            {
                foreach (var destinationPointId in seletedDestinationPointIds)
                {
                    var destinationPoint = await _dbContext.DestinationPoints
                        .Include(o => o.ReferencedSourcePoint)
                        .Include(o => o.Catalog).FirstOrDefaultAsync(o => o.Id == destinationPointId);

                    if (destinationPoint != null)
                    {
                        var logResult = new
                        {
                            location = destinationPoint.RangeId,
                            value = destinationPoint.ReferencedSourcePoint.Value,
                            fileName = destinationPoint.Catalog.FileName,
                            user = _userProfileService.GetCurrentUser().Username
                        };

                        _dbContext.DestinationPoints.Remove(destinationPoint);
                        await _dbContext.SaveChangesAsync();
                        await _logService.WriteLog(new LogEntity()
                        {
                            LogId = "20002",
                            Action = Constant.ACTIONTYPE_DELETE,
                            PointType = Constant.POINTTYPE_DESTINATIONPOINT,
                            ActionType = ActionTypeEnum.AuditLog,
                            Message = $"Delete destination point in the location {logResult.location}, value: {logResult.value} in the word file named:{logResult.fileName} by {logResult.user}"
                        });
                    }
                }
            }
            catch (Exception ex)

            {
                var logEntity = new LogEntity()
                {
                    LogId = "20003",
                    Action = Constant.ACTIONTYPE_DELETE,
                    ActionType = ActionTypeEnum.ErrorLog,
                    PointType = Constant.POINTTYPE_DESTINATIONPOINT,
                    Message = ".Net Error",
                    Detail = ex.ToString()
                };
                logEntity.Subject = $"{logEntity.LogId} - {logEntity.Action} - {logEntity.PointType} - Error";
                await _logService.WriteLog(logEntity);
                throw;
            }
        }

        public async Task<DestinationCatalog> GetDestinationCatalog(string fileName, string documentId)
        {
            try
            {
                var destinationCatalog = await _dbContext.DestinationCatalogs
					.Include(o => o.DestinationPoints).ThenInclude(x => x.DestinationPointCustomFormats).ThenInclude(x => x.CustomFormat)
					.Include(o => o.DestinationPoints).ThenInclude(x => x.ReferencedSourcePoint)
                    .FirstOrDefaultAsync(o => o.DocumentId == documentId);
                if (destinationCatalog != null)
                {
					
                    var sourcePoints = _dbContext.DestinationPoints.Where(x => x.CatalogId == destinationCatalog.Id).Select(o => o.ReferencedSourcePoint);
                    var sourcePointIds = sourcePoints.Select(point => point.Id).ToArray();
                    var publishedHistories = await (from pb in _dbContext.PublishedHistories
                                                    where sourcePointIds.Contains(pb.SourcePointId)
                                                    select pb).ToArrayAsync();
                    foreach (var sourcePoint in sourcePoints)
                    {
						var sourceCatalog = _dbContext.SourceCatalogs.FirstOrDefault(x => x.Id == sourcePoint.CatalogId);
						sourcePoint.PublishedHistories = publishedHistories.Where(pb => pb.SourcePointId == sourcePoint.Id)
                                                                    .OrderByDescending(p => p.PublishedDate).ToArray();
                        sourcePoint.SerializeCatalog = true;
                        sourceCatalog.SerializeSourcePoints = false;
						sourcePoint.Catalog = sourceCatalog;

					}
                    foreach (var destinationPoint in destinationCatalog.DestinationPoints)
                    {
                        destinationPoint.DestinationPointCustomFormats = destinationPoint.DestinationPointCustomFormats.ToArray();
                    }

                    if (!destinationCatalog.Name.Equals(fileName))
                    {
                        destinationCatalog.Name = fileName;
                        await _dbContext.SaveChangesAsync();
                    }
                }

                await _logService.WriteLog(new LogEntity()
                {
                    LogId = "20005",
                    Action = Constant.ACTIONTYPE_GET,
                    PointType = Constant.POINTTYPE_DESTINATIONPOINT,
                    ActionType = ActionTypeEnum.AuditLog,
                    Message = $"Get destination points list by {documentId}"
                });
                return destinationCatalog;

            }
            catch (Exception ex)
            {
                var logEntity = new LogEntity()
                {
                    LogId = "20006",
                    Action = Constant.ACTIONTYPE_GET,
                    ActionType = ActionTypeEnum.ErrorLog,
                    PointType = Constant.POINTTYPE_DESTINATIONLIST,
                    Message = ".Net Error",
                    Detail = ex.ToString()
                };
                logEntity.Subject = $"{logEntity.LogId} - {logEntity.Action} - {logEntity.PointType} - Error";
                await _logService.WriteLog(logEntity);
                throw ex;
            }
        }

        public async Task<IEnumerable<DestinationPoint>> GetDestinationPointBySourcePoint(Guid sourcePointId)
        {
            var destinationPoints = await _dbContext.DestinationPoints
                .Include(o => o.Catalog)
                .Include(o => o.DestinationPointCustomFormats)
                .Include(o => o.ReferencedSourcePoint)
                .Where(o => o.SourcePointId == sourcePointId).ToArrayAsync();
            foreach (var destinationPoint in destinationPoints)
            {
                if (destinationPoint != null)
                {
                    destinationPoint.SerializeCatalog = true;
                    destinationPoint.Catalog.SerializeDestinationPoints = false;
                }
            }
            return destinationPoints;
        }

        public async Task<IEnumerable<CustomFormat>> GetCustomFormats()
        {
            var customFormats = await _dbContext.CustomFormats.Where(c => !c.IsDeleted).ToArrayAsync();
            return customFormats;
        }

        public async Task<DestinationPoint> UpdateDestinationPointCustomFormat(DestinationPoint destinationPoint)
        {
            try
            {
                var previousDestinationPoint = await _dbContext.DestinationPoints
                    .Include(o => o.ReferencedSourcePoint)
                    .Include(o => o.ReferencedSourcePoint.Catalog)
                    .Include(o => o.DestinationPointCustomFormats).ThenInclude(x => x.CustomFormat).FirstAsync(o => o.Id == destinationPoint.Id);

                if (previousDestinationPoint != null)
                {
                    previousDestinationPoint.DecimalPlace = destinationPoint.DecimalPlace;
                    // Remove old format
                    var oldFormats = previousDestinationPoint.DestinationPointCustomFormats.Where(x => x.DestinationPointId == destinationPoint.Id).ToList();

                    foreach (var item in oldFormats)
                    {
                        previousDestinationPoint.DestinationPointCustomFormats.Remove(item);
                    }
                    
                    // Add new format
                    var newFormat = new DestinationPointCustomFormats() { DestinationPointId = destinationPoint.Id, CustomFormatId = destinationPoint.DestinationPointCustomFormats.FirstOrDefault().CustomFormatId };
                    previousDestinationPoint.DestinationPointCustomFormats.Add(newFormat);

                    await _dbContext.SaveChangesAsync();
                    await _dbContext.Entry(previousDestinationPoint.ReferencedSourcePoint).ReloadAsync();

                    previousDestinationPoint.ReferencedSourcePoint.PublishedHistories = (await _dbContext.PublishedHistories.Where(o => o.SourcePointId == previousDestinationPoint.ReferencedSourcePoint.Id).ToArrayAsync())
                        .OrderByDescending(p => p.PublishedDate).ToArray();
                    previousDestinationPoint.ReferencedSourcePoint.SerializeCatalog = true;
                    previousDestinationPoint.ReferencedSourcePoint.Catalog.SerializeSourcePoints = false;
                    previousDestinationPoint.DestinationPointCustomFormats.FirstOrDefault().CustomFormat = await _dbContext.CustomFormats.FirstOrDefaultAsync(x => x.Id == destinationPoint.DestinationPointCustomFormats.FirstOrDefault().CustomFormatId);

                    await _logService.WriteLog(new LogEntity()
                    {
                        LogId = "30002",
                        Action = Constant.ACTIONTYPE_EDIT,
                        PointType = Constant.POINTTYPE_DESTINATIONPOINT,
                        ActionType = ActionTypeEnum.AuditLog,
                        Message = $"Edit destination point."
                    });
                }

                return previousDestinationPoint;
            }
            catch (ApplicationException ex)
            {
                throw ex.InnerException;
            }
            catch (Exception ex)
            {
                var logEntity = new LogEntity()
                {
                    LogId = "20003",
                    Action = Constant.ACTIONTYPE_ADD,
                    ActionType = ActionTypeEnum.ErrorLog,
                    PointType = Constant.POINTTYPE_DESTINATIONPOINT,
                    Message = ".Net Error",
                    Detail = ex.ToString()
                };
                logEntity.Subject = $"{logEntity.LogId} - {logEntity.Action} - {logEntity.PointType} - Error";
                await _logService.WriteLog(logEntity);
                throw ex;
            }
        }
    }
}
