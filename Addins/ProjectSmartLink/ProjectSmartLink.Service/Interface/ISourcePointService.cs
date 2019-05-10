// Copyright(c) Microsoft Corporation. 
// All rights reserved.
//
// Licensed under the MIT license. See LICENSE file in the solution root folder for full license information.

using ProjectSmartLink.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectSmartLink.Service
{
    public interface ISourceService
    {
        Task<SourcePoint> AddSourcePoint(string fileName, string documentId, SourcePoint sourcePoint);
        Task<SourcePoint> EditSourcePoint(SourcePoint sourcePoint);
        Task<SourceCatalog> GetSourceCatalog(string fileName, string documentId);
        Task<SourceCatalog> GetSourceCatalog(string documentId);
        Task<IEnumerable<SourceCatalog>> GetSourceCatalogs(bool external);
        Task<int> DeleteSourcePoint(Guid sourcePointId);
        Task DeleteSelectedSourcePoint(IEnumerable<Guid> selectedSourcePointIds);

        Task<PublishSourcePointResult> PublishSourcePointList(IEnumerable<PublishSourcePointForm> publishSourcePointForms);
        Task<PublishedHistory> GetPublishHistoryById(Guid publishHistoryId);

        Task<IEnumerable<DocumentCheckResult>> GetAllCatalogs();
        Task<IEnumerable<DocumentCheckResult>> UpdateDocumentUrlById(IEnumerable<DocumentCheckResult> documents);
    }
}
