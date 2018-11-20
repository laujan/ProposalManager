/* 
*  Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. 
*  See LICENSE in the source repository root for complete license information. 
*/

import React, { Component } from 'react';
import { Trans } from "react-i18next";

export class AddProcessTypeR extends Component {
    displayName = AddProcessTypeR.name

    constructor(props) {
        super(props);
    }
    render() {
        let process = this.props.displayProcess;
        return (
                <div className="ms-Grid-row bg-white">
                    <div className="ms-Grid-col ms-sm10 ms-md4 ms-lg11 ml15 mt5 ">
                        <div className="ms-Grid-row bg-grey">
                        <div className="ms-Grid-col ms-sm12 ms-md6 ms-lg12 processBg pb10">
                                <h5>{process.processStep}</h5>
                            <span className="font10 "><Trans>estimateDays</Trans> : {process.daysEstimate}</span>
                            </div>
                            
                        </div>
                    </div>
                    
                </div>

        );
    }
}