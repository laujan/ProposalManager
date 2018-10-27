// Copyright(c) Microsoft Corporation. 
// All rights reserved.
//
// Licensed under the MIT license. See LICENSE file in the solution root folder for full license information.

using Microsoft.WindowsAzure.Storage.Table;
using System.Threading.Tasks;

namespace SmartLink.Service
{
    public interface IAzureStorageService
    {
        Task WriteMessageToQueue(string queueMessage, string queueName);
        CloudTable GetTable(string tableName);
    }
}
