/* 
*  Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. 
*  See LICENSE in the source repository root for complete license information. 
*/
import React, { Component } from 'react';
import * as microsoftTeams from '@microsoft/teams-js';
import { Pivot, PivotItem, PivotLinkFormat, PivotLinkSize } from 'office-ui-fabric-react/lib/Pivot';
import { Trans } from "react-i18next";
import { DealTypeListR } from './DealType/DealTypeListR';
import { ProcessTypesList } from './Configuration/ProcessTypesList';
import { Permissions } from './Configuration/Permissions';
import { TemplateList } from './Templates/TemplateList';
import Accessdenied from '../../helpers/AccessDenied';
import { MetaData } from './Configuration/MetaData';
import { isTemplateEnabled } from '../../helpers/AppSettings';
import { Tasks } from './Configuration/Tasks';

export class Configuration extends Component {
    displayName = Configuration.name;

    constructor(props) {
        super(props);

        this.apiService = this.props.apiService;
        this.logService = this.props.logService;
        this.authHelper = window.authHelper;

        try {
            microsoftTeams.initialize();
        }
        catch (err) {
            this.logService.log(err);
        }
        finally {
            this.state = {
                teamName: "",
                groupId: "",
                haveGranularAccess: false,
                selectedTabName: window.location.hash.substr(1).length > 0 ? window.location.hash.substr(1) : ""
            };
        }
    }

    componentDidMount() {
        this.authHelper.callCheckAccess(["Administrator"]).then((data) => {
            this.setState({ haveGranularAccess: data });
        }).catch(err => { this.setState({ haveGranularAccess: false }); });
    }

    render() {
        return (
            <div className='ms-Grid'>
                <div className='ms-Grid-row'>
                    <div className='ms-Grid-col ms-sm6 ms-md8 ms-lg12  tabviewUpdates' >
                        {
                            this.state.haveGranularAccess
                                ?
                                <Pivot className='tabcontrols pt35' linkFormat={PivotLinkFormat.tabs} linkSize={PivotLinkSize.large} selectedKey={this.state.selectedTabName}>
                                    <PivotItem linkText={<Trans>permissions</Trans>} itemKey="permissions">
                                        <Permissions apiService={this.apiService} logService={this.logService} />
                                    </PivotItem>
                                    <PivotItem linkText={<Trans>tasks</Trans>} itemKey="tasks">
                                        <Tasks apiService={this.apiService} logService={this.logService} />
                                    </PivotItem>
                                    <PivotItem linkText={<Trans>processTypes</Trans>} itemKey="processType">
                                        <ProcessTypesList apiService={this.apiService} logService={this.logService} />
                                    </PivotItem>
                                    {
                                        isTemplateEnabled ?
                                            <PivotItem linkText={<Trans>templates</Trans>} itemKey="templates">
                                                <TemplateList apiService={this.apiService} logService={this.logService} />
                                            </PivotItem>
                                            : <PivotItem linkText={<Trans>businessProcess</Trans>} itemKey="dealType">
                                                <DealTypeListR apiService={this.apiService} logService={this.logService} />
                                            </PivotItem>
                                    }
                                    <PivotItem linkText={<Trans>dataModel</Trans>} itemKey="metaData">
                                        <MetaData apiService={this.apiService} logService={this.logService} />
                                    </PivotItem>
                                </Pivot>
                                :
                                <Accessdenied />
                        }
                    </div>
                </div>
            </div>
        );
    }

}
