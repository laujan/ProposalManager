/* 
*  Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. 
*  See LICENSE in the source repository root for complete license information. 
*/


import React from 'react';
import { Trans } from "react-i18next";


export default function Accessdenied() {
    return (
        <div className='ms-Grid bg-white  p-10 tabviewUpdates'>
            <div className='ms-Grid-row'>
                <div className="ms-Grid-col ms-sm6 ms-md8 ms-lg12 p-10">
                    <h4><Trans>accessDenied</Trans></h4>
                </div>
            </div>
        </div>
    );
}
