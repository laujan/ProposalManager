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
import { Workflow } from '../components-teams/Proposal/Workflow';
import { TeamUpdate } from '../components-teams/Proposal/TeamUpdate';
import { getQueryVariable } from '../common';
import { GroupEmployeeStatusCard } from '../components-teams/general/Opportunity/GroupEmployeeStatusCard';
import { Trans } from "react-i18next";
import { Spinner, SpinnerSize } from 'office-ui-fabric-react/lib/Spinner';
import { OpportunitySummary } from '../components-teams/general/Opportunity/OpportunitySummary';
import { OpportunityNotes } from '../components-teams/general/Opportunity/OpportunityNotes';
import Accessdenied from '../helpers/AccessDenied';

export class RootTab extends Component {
    displayName = RootTab.name;

    constructor(props) {
        super(props);

        this.apiService = this.props.apiService;
        this.authHelper = window.authHelper;
        this.utils = window.utils;
        this.accessGranted = false;

        try {
            microsoftTeams.initialize();
        }
        catch (err) {
            console.log(err);
        }
        finally {
            this.state = {
                teamMembers: [],
                oppName: "",
                oppDetails: "",
                otherRoleTeamMembers: [],
                loading: true,
                haveGranularAccess: false,
                isAuthenticated: false
            };
        }
    }

    componentDidMount() {
        console.log("Dashboard_componentDidMount isauth: " + this.authHelper.isAuthenticated());
        this.fnGetOpportunityData();
    }

    fnGetOpportunityData() {
        const teamName = getQueryVariable('teamName');
        this.apiService.callApi('Opportunity', 'GET', { query: `name=${teamName}` })
            .then(async (response) => {
                if (response.ok) {
                    let data = await response.json();
                    if (data.error && data.error.code.toLowerCase() === "badrequest") {
                        this.setState({
                            loading: false,
                            haveGranularAccess: false
                        });
                    }
                    else {
                        this.teamMembers = data.teamMembers;
                        // Getting processtypes from opportunity dealtype object
                        let processList = data.template.processes;

                        // Get Other role officers list
                        let otherRolesMapping = processList.filter(function (k) {
                            return k.processType.toLowerCase() !== "new opportunity" && k.processType.toLowerCase() !== "start process" && k.processType.toLowerCase() !== "customerdecisiontab" && k.processType.toLowerCase() !== "proposalstatustab";
                        });

                        let otherRolesArr1 = [];
                        for (let j = 0; j < otherRolesMapping.length; j++) {

                            let processTeamMember = [];

                            processTeamMember = data.teamMembers.filter(function (k) {
                                if (k.processStep.toLowerCase() === otherRolesMapping[j].processStep.toLowerCase()) {
                                    //ProcessStep
                                    k.processStep = otherRolesMapping[j].processStep;
                                    //ProcessStatus
                                    k.processStatus = otherRolesMapping[j].status;
                                    k.status = otherRolesMapping[j].status;
                                    return k.processStep.toLowerCase() === otherRolesMapping[j].processStep.toLowerCase();
                                }
                                else {
                                    return false;
                                }
                            });
                            if (processTeamMember.length === 0) {
                                processTeamMember = [{
                                    "displayName": "",
                                    "assignedRole": {
                                        "displayName": otherRolesMapping[j].roleName,
                                        "adGroupName": otherRolesMapping[j].adGroupName
                                    },
                                    "processStep": otherRolesMapping[j].processStep,
                                    "processStatus": 0,
                                    "status": 0
                                }];
                            }

                            otherRolesArr1 = otherRolesArr1.concat(processTeamMember);
                        }

                        let otherRolesArr = otherRolesArr1.reduce(function (res, currentValue) {
                            if (res.indexOf(currentValue.processStep) === -1) {
                                res.push(currentValue.processStep);
                            }
                            return res;
                        }, []).map(function (group) {
                            return {
                                group: group,
                                users: otherRolesArr1.filter(function (_el) {
                                    return _el.processStep === group;
                                }).map(function (_el) { return _el; })
                            };
                        });
                        let otherRolesObj = [];
                        if (otherRolesArr.length > 1) {
                            for (let r = 0; r < otherRolesArr.length; r++) {
                                otherRolesObj.push(otherRolesArr[r].users);
                            }
                            this.otherRoleTeamMembers = otherRolesObj;
                        }
                        this.setState({
                            loading: false,
                            teamMembers: this.teamMembers,
                            oppDetails: data,
                            oppStatus: data.opportunityState,
                            oppName: data.displayName,
                            otherRoleTeamMembers: otherRolesObj,
                            haveGranularAccess: true
                        });
                    }
                }
            })
            .catch(err => {
                console.log("Error: OpportunityGetByName--", err);
            });
    }

    render() {
        const { teamMembers, otherRoleTeamMembers, oppDetails, groupId, oppStatus, loading, haveGranularAccess, oppName } = this.state;
        const { teamsContext, userProfile, apiService } = this.props;
        const channelId = teamsContext.channelId;
        
        let loanOfficerRealManagerArr = [];

        let loanOfficerRealManagerArr1 = teamMembers.filter(x => x.assignedRole.displayName === "LoanOfficer");
        if (loanOfficerRealManagerArr1.length === 0) {
            loanOfficerRealManagerArr1 = [{
                "displayName": "",
                "assignedRole": {
                    "displayName": "LoanOfficer"
                }
            }];
        }

        let loanOfficerRealManagerArr2 = teamMembers.filter(x => x.assignedRole.displayName === "RelationshipManager");
        loanOfficerRealManagerArr = loanOfficerRealManagerArr1.concat(loanOfficerRealManagerArr2);

        const OpportunitySummaryView = () => {
            return <OpportunitySummary teamsContext={teamsContext} opportunityData={oppDetails} opportunityId={oppDetails.id} userProfile={userProfile} apiService={apiService} />;
        };

        return (

            <div className='ms-Grid'>
                <div className='ms-Grid-row'>
                    <div className='ms-Grid-col ms-sm6 ms-md8 ms-lg12 bgwhite tabviewUpdates' >
                        {
                            loading ?
                                <div>
                                    <div className='ms-BasicSpinnersExample pull-center'>
                                        <br /><br />
                                        <Spinner size={SpinnerSize.medium} label={<Trans>loading</Trans>} ariaLive='assertive' />
                                    </div>
                                </div>
                                :
                                haveGranularAccess
                                    ?
                                    <div>
                                        <Pivot className='tabcontrols ' linkFormat={PivotLinkFormat.tabs} linkSize={PivotLinkSize.large}>
                                            <PivotItem linkText={<Trans>summary</Trans>} width='100%' itemKey="Summary" >
                                                <div className='ms-Grid-row'>
                                                    <OpportunitySummaryView userProfile={[]} />
                                                </div>
                                            </PivotItem>
                                            <PivotItem linkText={<Trans>workflow</Trans>} width='100%' >
                                                <div className='ms-Grid-row mt20 pl15 bg-white'>
                                                    <Workflow memberslist={teamMembers} oppStaus={oppStatus} oppDetails={oppDetails}/>
                                                </div>
                                            </PivotItem>
                                            <PivotItem linkText={<Trans>teamUpdate</Trans>}>
                                                <div className='ms-Grid-row bg-white'>
                                                    {
                                                        otherRoleTeamMembers.map((obj, ind) =>
                                                            obj.length > 1
                                                                ?
                                                                <div className=' ms-Grid-col ms-sm12 ms-md8 ms-lg4 p-5' key={ind}>
                                                                    <GroupEmployeeStatusCard members={obj} status={obj[0].status} isDispOppStatus={false} role={obj[0].assignedRole.adGroupName} isTeam='true' />
                                                                </div>
                                                                :
                                                                obj.map((member, j) => {
                                                                    return (
                                                                        <div className=' ms-Grid-col ms-sm12 ms-md8 ms-lg4 p-5' key={j}>
                                                                            <TeamUpdate memberslist={member} channelId={channelId} groupId={groupId} oppName={oppName} />
                                                                        </div>
                                                                    );
                                                                }
                                                                )
                                                        )
                                                    }

                                                </div>
                                                <div className='ms-Grid-row'>
                                                    <div className=' ms-Grid-col ms-sm12 ms-md8 ms-lg12' >
                                                        <hr />
                                                    </div>

                                                </div>

                                                <div className='ms-Grid-row  bg-white'>
                                                    {loanOfficerRealManagerArr.map((member, ind) => {
                                                        return (
                                                            <div className=' ms-Grid-col ms-sm12 ms-md8 ms-lg4 p-5' key={ind} >
                                                                <TeamUpdate memberslist={member} channelId={channelId} groupId={groupId} oppName={oppName} />
                                                            </div>
                                                        );
                                                    }
                                                    )
                                                    }
                                                </div>

                                            </PivotItem>
                                            <PivotItem linkText={<Trans>notes</Trans>} width='100%' itemKey="Notes" >
                                                <div className='ms-Grid-col ms-sm12 ms-md8 ms-lg12' >
                                                    <OpportunityNotes userProfile={[]} opportunityData={oppDetails} opportunityId={oppDetails.id} />
                                                </div>
                                            </PivotItem>
                                        </Pivot>
                                    </div>
                                    :
                                    <Accessdenied />
                        }
                    </div>
                </div>
                <div className='ms-Grid-row'>
                    <div className='ms-Grid-col ms-sm6 ms-md8 ms-lg10' />
                </div>
            </div>
        );
    }
}