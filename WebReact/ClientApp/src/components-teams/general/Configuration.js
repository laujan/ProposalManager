/* 
*  Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. 
*  See LICENSE in the source repository root for complete license information. 
*/
import React, { Component } from 'react';
import * as microsoftTeams from '@microsoft/teams-js';

import {
    Pivot,
    PivotItem,
    PivotLinkFormat,
    PivotLinkSize
} from 'office-ui-fabric-react/lib/Pivot';
import { Workflow } from '../../components-teams/Proposal/Workflow';
import { TeamUpdate } from '../../components-teams/Proposal/TeamUpdate';
import { TeamsComponentContext } from 'msteams-ui-components-react';
import { getQueryVariable } from '../../common';
import { GroupEmployeeStatusCard } from '../../components/Opportunity/GroupEmployeeStatusCard';
import { Trans } from "react-i18next";
import { DealTypeList } from './DealTypeList';
import { Category } from '../../components/Administration/Category';
import { Region } from '../../components/Administration/Region';
import { Industry } from '../../components/Administration/Industry';
import { UserRole } from '../../components/Administration/UserRole';
import { ProcessTypesList } from './ProcessTypesList';
import { Permissions } from './Permissions';


export class Configuration extends Component {
    displayName = Configuration.name

    constructor(props) {
        super(props);

        this.authHelper = window.authHelper;
        this.sdkHelper = window.sdkHelper;


        try {
            microsoftTeams.initialize();
        }
        catch (err) {
            console.log(err);
        }
        finally {
            this.state = {
                teamName: "",
                groupId: "",
                haveGranularAccess: false
            };
        }
    }

    componentWillMount() {
        let selectedTab = window.location.hash.substr(1).length > 0 ? window.location.hash.substr(1) : "";
        this.setState({
            selectedTabName: selectedTab
        });

        this.authHelper.callCheckAccess(["Administrator"]).then((data) => {
            let haveGranularAccess = data;
            this.setState({ haveGranularAccess: haveGranularAccess });
        });
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
                                    <PivotItem linkText={<Trans>category</Trans>} width='100%' itemKey="category" >
                                        <Category />
                                    </PivotItem>
                                    <PivotItem linkText={<Trans>region</Trans>} itemKey="region">
                                        <Region />
                                    </PivotItem>
                                    <PivotItem linkText={<Trans>industry</Trans>} itemKey="industry">
                                        <Industry />
                                    </PivotItem>
                                    <PivotItem linkText={<Trans>permissions</Trans>} itemKey="permissions">
                                        <Permissions />
                                    </PivotItem>
                                    <PivotItem linkText={<Trans>dealTypes</Trans>} itemKey="dealType">
                                        <DealTypeList />
                                    </PivotItem>
                                    <PivotItem linkText={<Trans>processTypes</Trans>} itemKey="processType">
                                        <ProcessTypesList />
                                    </PivotItem>
                                </Pivot>
                                :
                                <div className="bg-white p-10"><h2><Trans>accessDenied</Trans></h2></div>
                        }
                        
                    </div>
                </div>
            </div>
        );
    }

}
