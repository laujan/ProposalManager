/* 
*  Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. 
*  See LICENSE in the source repository root for complete license information. 
*/
import appSettingsObject from '../helpers/AppSettings';

export default class LoggingService {
    constructor() {
    }

    log() {
        if (appSettingsObject.logEnabled === true) {
            console.log("LOGSERVICE:", ...arguments);
        }
    }
}

