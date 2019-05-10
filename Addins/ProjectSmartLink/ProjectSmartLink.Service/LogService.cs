// Copyright(c) Microsoft Corporation. 
// All rights reserved.
//
// Licensed under the MIT license. See LICENSE file in the solution root folder for full license information.

using Microsoft.ApplicationInsights;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ProjectSmartLink.Entity;

namespace ProjectSmartLink.Service
{
    public class LogService : ILogService
    {
        private TelemetryClient _telemetry;
        private readonly IConfigService _configService;
        public LogService(IConfigService configService)
        {
            _telemetry = new TelemetryClient();
            _configService = configService;
        }
        public void Flush()
        {
            _telemetry.Flush();
        }

        public void WriteLog(LogEntity entity)
        {
            var properties = new Dictionary<string, string>();
            properties.Add("Subject", entity.Subject);
            properties.Add("Message", entity.Message);
            properties.Add("Log ID", entity.LogId);
            properties.Add("Action", entity.Action);
            properties.Add("Point Type", entity.PointType);
            if (entity.ActionType == ActionTypeEnum.ErrorLog)
            {
                properties.Add("Detail", entity.Detail);
            }
            properties.Add("Action Type", entity.ActionType.ToString());

            //var metric = new Dictionary<string, double>();
            _telemetry.TrackEvent(string.Format("{0} {1}",entity.PointType,entity.ActionType), properties);

#if DEBUG
            Flush();
#endif
        }
    }
}
