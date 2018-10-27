// Copyright(c) Microsoft Corporation. 
// All rights reserved.
//
// Licensed under the MIT license. See LICENSE file in the solution root folder for full license information.

using System;

namespace SmartLink.Entity
{
    public class PublishedMessage
    {
        public Guid PublishBatchId { get; set; }
        public Guid PublishHistoryId { get; set; }
        public Guid SourcePointId { get; set; }
    }
}
