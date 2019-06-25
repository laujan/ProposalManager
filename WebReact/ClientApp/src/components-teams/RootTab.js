/* 
*  Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. 
*  See LICENSE in the source repository root for complete license information. 
*/
import React, { Component } from 'react';
import { Pivot, PivotItem, PivotLinkFormat, PivotLinkSize } from 'office-ui-fabric-react/lib/Pivot';
import { PrimaryButton } from 'office-ui-fabric-react/lib/Button';
import { Label } from 'office-ui-fabric-react/lib/Label';
import { Workflow } from './Proposal/Workflow';
import { TeamUpdate } from './Proposal/TeamUpdate';
import { TeamsComponentContext } from 'msteams-ui-components-react';
import './teams.css';
import { GroupEmployeeStatusCard } from '../components-teams/general/Opportunity/GroupEmployeeStatusCard';
import { Trans } from "react-i18next";
import { Spinner, SpinnerSize } from 'office-ui-fabric-react/lib/Spinner';
import { getQueryVariable } from '../common';
import { OpportunitySummary } from './general/Opportunity/OpportunitySummary';
import { OpportunityNotes } from './general/Opportunity/OpportunityNotes';
import Accessdenied from '../helpers/AccessDenied';

/**
 * 
 * Route Component, 
 * This component shows Workflow page, Teamupdate page and opportunitynotes page.
 * Along with Path to general page (Opportunity Content) in Teams
 */

export class RootTab extends Component {
    displayName = RootTab.name

    constructor(props) {
        super(props);

        this.apiService = this.props.apiService;
        this.logService = this.props.logService;
        this.authHelper = window.authHelper;
        this.utils = window.utils;
        this.accessGranted = false;

        this.state = {
            teamMembers: [],
            errorLoading: false,
            oppName: "",
            oppDetails: "",
            otherRoleTeamMembers: [],
            loading: true,
            haveGranularAccess: false,
            isAuthenticated: false
        };
    }

    componentDidMount() {
        this.logService.log("Dashboard_componentDidMount");
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
                    } else {
                        let teamMembers = data.teamMembers;
                        let processList = data.template.processes;

                        this.logService.log("RootTab_fnGetOpportunityData teamMembers : ", teamMembers);
                        this.logService.log("RootTab_fnGetOpportunityData processList : ", processList);

                        //code refactored - display checklist page processtype users
                        let otherRolesMapping = processList.filter(function (k) {
                            return k.processType.toLowerCase() === "checklisttab";
                        });

                        this.logService.log("RootTab_fnGetOpportunityData otherRolesMapping : ", otherRolesMapping);

                        let otherRolesArr1 = [];
                        for (let j = 0; j < otherRolesMapping.length; j++) {

                            let processTeamMember = [];
                            processTeamMember = data.teamMembers.filter(function (k) {
                                if (k.processStep.toLowerCase() === otherRolesMapping[j].processStep.toLowerCase()) {
                                    k.processStep = otherRolesMapping[j].processStep;

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
                        }
                        this.setState({
                            loading: false,
                            teamMembers: teamMembers,
                            oppDetails: data,
                            oppStatus: data.opportunityState,
                            oppName: data.displayName,
                            otherRoleTeamMembers: otherRolesObj,
                            haveGranularAccess: true
                        });
                    }
                }
            })
            .catch(function (err) {
                this.logService.log("Error: OpportunityGetByName", err);
            });
    }


    resetToken() {
        this.authHelper.logout().then(() => {
            window.location.reload();
        });
    }

    render() {
        const { teamMembers, otherRoleTeamMembers, oppDetails, groupId, oppStatus, errorLoading, loading, haveGranularAccess, oppName } = this.state;
        const { teamsContext, userProfile, apiService, logService } = this.props;
        const channelId = teamsContext.channelId;
        let loanOfficerRealManagerArr = [];

        let loanOfficerRealManagerArr1 = this.utils.getLoanOficers(teamMembers);
        let loanOfficerRealManagerArr2 = this.utils.getRelationShipManagers(teamMembers);

        loanOfficerRealManagerArr = loanOfficerRealManagerArr1.concat(loanOfficerRealManagerArr2);
        this.logService.log("RootTab_fnGetOpportunityData loanOfficerRealManagerArr : ", loanOfficerRealManagerArr);
        this.logService.log("RootTab_fnGetOpportunityData OtherRoleTeamMembers : ", otherRoleTeamMembers);
        const OpportunitySummaryView = () => {
            return <OpportunitySummary teamsContext={teamsContext} opportunityData={oppDetails} opportunityId={oppDetails.id} userProfile={userProfile} apiService={apiService} logService={this.logService}/>;
        };
        return (

            <TeamsComponentContext>
                <div className='ms-Grid'>
                    <div className='ms-Grid-row'>
                        <div className='ms-Grid-col ms-sm6 ms-md8 ms-lg12 pL0' >
                            {
                                errorLoading ?
                                    <div>
                                        <Trans>errorLoadinOpportunityDataPleaseRefresh</Trans>
                                        <br /><br />
                                        <PrimaryButton className='pull-right refreshbutton' onClick={() => this.resetToken()}>
                                            <Trans>resetTab</Trans>
                                        </PrimaryButton>
                                    </div>
                                    :
                                    <div>
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
                                                        <Pivot className='tabcontrols' linkFormat={PivotLinkFormat.tabs} linkSize={PivotLinkSize.large}>

                                                            <br />
                                                            <br />
                                                            <PivotItem linkText={<Trans>summary</Trans>} width='100%' itemKey="Summary" >
                                                                <div className='ms-Grid-row mt20 mr20 pl15 bg-grey'>
                                                                    <OpportunitySummaryView userProfile={[]} />
                                                                </div>
                                                            </PivotItem>
                                                            <PivotItem linkText={<Trans>workflow</Trans>} width='100%' >
                                                                <div className='ms-Grid-row mt20 pl15 bg-white'>
                                                                    <Label><Workflow memberslist={teamMembers} oppStaus={oppStatus} oppDetails={oppDetails} logService={this.logService}/></Label>
                                                                </div>
                                                            </PivotItem>
                                                            <PivotItem linkText={<Trans>teamUpdate</Trans>}>
                                                                <div className='ms-Grid-row mt20 pl15 bg-white'>
                                                                    {
                                                                        otherRoleTeamMembers.map((obj, ind) =>
                                                                            obj.length > 1 ?
                                                                                <div className=' ms-Grid-col ms-sm12 ms-md8 ms-lg4 p-5' key={ind}>
                                                                                    <GroupEmployeeStatusCard members={obj} status={obj[0].status} isDispOppStatus={false} role={obj[0].adGroupName} isTeam='true' />
                                                                                </div>
                                                                                :
                                                                                obj.map((member, j) => {
                                                                                    return (
                                                                                        <div className=' ms-Grid-col ms-sm12 ms-md8 ms-lg4 p-5' key={j}>
                                                                                            <TeamUpdate memberslist={member} channelId={channelId} groupId={groupId} oppName={oppName} logService={this.logService}/>
                                                                                        </div>
                                                                                    );
                                                                                }
                                                                                )
                                                                        )
                                                                    }
                                                                </div>
                                                                <div className='ms-Grid-row'>
                                                                    <div className=' ms-Grid-col ms-sm6 ms-md8 ms-lg12' />
                                                                </div>
                                                                <div className='ms-Grid-row pl15 bg-white'>
                                                                    {
                                                                        loanOfficerRealManagerArr.map((member, ind) => {
                                                                            return (
                                                                                <div className=' ms-Grid-col ms-sm12 ms-md8 ms-lg4 p-5' key={ind} >
                                                                                    <TeamUpdate memberslist={member} channelId={channelId} groupId={groupId} oppName={oppName} logService={this.logService}/>
                                                                                </div>
                                                                            );
                                                                        })
                                                                    }
                                                                </div>
                                                            </PivotItem>
                                                            <PivotItem linkText={<Trans>notes</Trans>} width='100%' itemKey="Notes" >
                                                                <div className='ms-Grid-col ms-sm12 ms-md8 ms-lg12' >
                                                                    <OpportunityNotes userProfile={userProfile} apiService={apiService} opportunityData={oppDetails} opportunityId={oppDetails.id} logService={this.logService}/>
                                                                </div>
                                                            </PivotItem>
                                                        </Pivot>
                                                    </div>
                                                    :
                                                    <Accessdenied />
                                        }
                                    </div>
                            }
                        </div>
                    </div>
                    <div className='ms-Grid-row'>
                        <div className='ms-Grid-col ms-sm6 ms-md8 ms-lg10' />
                    </div>
                </div>
            </TeamsComponentContext>
        );
    }
}