/*
*  Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license.
*  See LICENSE in the source repository root for complete license information.
*/

import React, { Component } from 'react';
import { Pivot, PivotItem, PivotLinkFormat, PivotLinkSize } from 'office-ui-fabric-react/lib/Pivot';
import { Label } from 'office-ui-fabric-react/lib/Label';

import { Category } from '../Administration/Category';
import { Industry } from '../Administration/Industry';
import { Region } from '../Administration/Region';

import '../../Style.css';
import { Trans } from "react-i18next";

export class Settings extends Component { 
    displayName = Settings.name

    constructor(props) {
        super(props);
    }

 

    render() {

        return (
            <div className='ms-Grid'> 
                <div className='ms-Grid-row'>
                    <div className='ms-Grid-col ms-sm12 ms-md12 ms-lg6 pageheading'>
                        <h3><Trans>settings</Trans></h3>
                    </div>
                </div>
                <div className='ms-Grid-row ibox-content'>
                    <div className='ms-Grid-col ms-sm12 ms-md12 ms-lg12'>
                        <Pivot linkFormat={PivotLinkFormat.tabs} linkSize={PivotLinkSize.large}>
                            <PivotItem linkText={<Trans>category</Trans>} className="TabBorder">
                                <Category/> 
                                </PivotItem>    
                            <PivotItem linkText={<Trans>industry</Trans>} className="TabBorder">
                                <Industry/>
                                </PivotItem>
                            <PivotItem linkText={<Trans>region</Trans>} className="TabBorder">
                                <Region/>
                            </PivotItem>
                        </Pivot>
                    </div>
                </div>
            </div>

        );


    }
}