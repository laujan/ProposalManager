/* 
*  Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. 
*  See LICENSE in the source repository root for complete license information. 
*/
import appSettingsObject from '../helpers/AppSettings';

/**
 * Responsible for performing the client-side logging. If logEnabled setting is true then logs are writen to the browser's console.
 * */
export default class LoggingService {
    constructor() {
    }

    log() {
        if (appSettingsObject.logEnabled === true) {
            console.log("LOGSERVICE:", ...arguments);
        }
    }
}

