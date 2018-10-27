// Copyright(c) Microsoft Corporation. 
// All rights reserved.
//
// Licensed under the MIT license. See LICENSE file in the solution root folder for full license information.

using System;

namespace SmartLink.Entity
{
    public class PublishSourcePointForm
    {
        public Guid SourcePointId { get; set; }
        public string CurrentValue { get; set; }
        public string Position { get; set; }
        public string Name { get; set; }
        public string NamePosition { get; set; }
    }
}