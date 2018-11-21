/* 
*  Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. 
*  See LICENSE in the source repository root for complete license information. 
*/
import React, { Component } from 'react';
import * as microsoftTeams from '@microsoft/teams-js';
import { Pivot, PivotItem, PivotLinkFormat, PivotLinkSize } from 'office-ui-fabric-react/lib/Pivot';
import { PrimaryButton } from 'office-ui-fabric-react/lib/Button';
import { Label } from 'office-ui-fabric-react/lib/Label';
import { Workflow } from './Proposal/Workflow';
import { TeamUpdate } from './Proposal/TeamUpdate';
import { TeamsComponentContext } from 'msteams-ui-components-react';
import './teams.css';
import { GroupEmployeeStatusCard } from '../components/Opportunity/GroupEmployeeStatusCard';
import { Trans } from "react-i18next";
import { Spinner, SpinnerSize } from 'office-ui-fabric-react/lib/Spinner';
import { getQueryVariable } from '../common';
import { OpportunitySummary } from './general/Opportunity/OpportunitySummary';
import { OpportunityNotes } from './general/Opportunity/OpportunityNotes';
import Accessdenied from '../helpers/AccessDenied';

export class RootTab extends Component {
    displayName = RootTab.name

    constructor(props) {
        super(props);

        this.authHelper = window.authHelper;
        this.sdkHelper = window.sdkHelper;
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
                errorLoading: false,
                OppName: "",
                oppDetails: "",
                UserRoleList: [],
                OtherRoleTeamMembers: [],
                loading: true,
                haveGranularAccess: false,
                isAuthenticated: false
            };
        }
        this.fnGetOpportunityData = this.fnGetOpportunityData.bind(this);
    }

    componentWillMount() {
        console.log("Dashboard_componentWillMount isauth: " + this.authHelper.isAuthenticated());
    }

    componentDidMount() {
        console.log("Dashboard_componentDidMount isauth: " + this.authHelper.isAuthenticated());
        if (!this.state.isAuthenticated) {
            this.authHelper.callGetUserProfile()
                .then(userProfile => {
                    this.setState({
                        userProfile: userProfile,
                        loading: true
                    });
                });
        }
    }


    componentDidUpdate() {
        if (this.authHelper.isAuthenticated() && !this.accessGranted) {
            console.log("Dashboard_componentDidUpdate callCheckAccess");
            this.accessGranted = true;
            this.fnGetOpportunityData();
        }
    }

    fnGetOpportunityData() {
        return new Promise((resolve, reject) => {
            // API - Fetch call
            let teamName = getQueryVariable('teamName');
            this.requestUrl = `api/Opportunity?name=${teamName}`;
            fetch(this.requestUrl, {
                method: "GET",
                headers: { 'authorization': 'Bearer ' + window.authHelper.getWebApiToken() }

            })
                .then(response => response.json())
                .then(data => {
                    if (data.error && data.error.code.toLowerCase() === "badrequest") {
                        this.setState({
                            loading: false,
                            haveGranularAccess: false
                        });
                        resolve(true);
                    } else {
                        let loanOfficer = {};
                        let teamMembers = data.teamMembers;
                        // Getting processtypes from opportunity dealtype object
                        let processList = data.dealType.processes;
                        //let oppChannels = new Array();
                        //oppChannels = processList.filter(x => x.channel.toLowerCase() !== "none");
                        // Get Other role officers list
                        let otherRolesMapping = processList.filter(function (k) {
                            return k.processType.toLowerCase() !== "new opportunity" && k.processType.toLowerCase() !== "start process" && k.processType.toLowerCase() !== "customerdecisiontab" && k.processType.toLowerCase() !== "proposalstatustab";
                        });

                        let otherRolesArr1 = [];
                        for (let j = 0; j < otherRolesMapping.length; j++) {

                            let processTeamMember = [];
                            //processTeamMember = data.teamMembers.filter(t => t.processStep.toLowerCase() === otherRolesMapping[j].processStep.toLowerCase());
                            processTeamMember = data.teamMembers.filter(function (k) {
                                if (k.processStep.toLowerCase() === otherRolesMapping[j].processStep.toLowerCase()) {
                                    //ProcessStep
                                    k.processStep = otherRolesMapping[j].processStep;
                                    //ProcessStatus
                                    k.processStatus = otherRolesMapping[j].status;
                                    k.status = otherRolesMapping[j].status;
                                    return k.processStep.toLowerCase() === otherRolesMapping[j].processStep.toLowerCase();
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
                            //otherRolesArr1 = otherRolesArr1.concat(teamMember);
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
                            //let OtherRoleTeamMembers = otherRolesObj;
                        }
                        this.setState({
                            loading: false,
                            teamMembers: teamMembers,
                            LoanOfficer: loanOfficer,
                            oppDetails: data,
                            oppStatus: data.opportunityState,
                            OppName: data.displayName,
                            OtherRoleTeamMembers: otherRolesObj,
                            haveGranularAccess: true
                        });
                        resolve(true);
                    }
                })
                .catch(function (err) {
                    console.log("Error: OpportunityGetByName--");
                    reject(err);
                });
        });
    }


    resetToken() {
        this.authHelper.logout().then(() => {
            window.location.reload();
        });
    }

    render() {
        const team = this.state.teamMembers;
        const channelId = this.props.teamsContext.channelId;

        let loanOfficerRealManagerArr = [];

        let loanOfficerRealManagerArr1 = team.filter(x => x.assignedRole.displayName === "LoanOfficer");
        if (loanOfficerRealManagerArr1.length === 0) {
            loanOfficerRealManagerArr1 = [{
                "displayName": "",
                "assignedRole": {
                    "displayName": "LoanOfficer"
                }
            }];
        }
        let loanOfficerRealManagerArr2 = team.filter(x => x.assignedRole.displayName === "RelationshipManager");

        loanOfficerRealManagerArr = loanOfficerRealManagerArr1.concat(loanOfficerRealManagerArr2);

        const OpportunitySummaryView = ({ match }) => {
            return <OpportunitySummary teamsContext={this.props.teamsContext} opportunityData={this.state.oppDetails} opportunityId={this.state.oppDetails.id} />;
        };
        return (

            <TeamsComponentContext>
                <div className='ms-Grid'>
                    <div className='ms-Grid-row'>
                        <div className='ms-Grid-col ms-sm6 ms-md8 ms-lg12 pL0' >
                            {
                                this.state.errorLoading ?
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
                                            this.state.loading ?
                                                <div>
                                                    <div className='ms-BasicSpinnersExample pull-center'>
                                                        <br /><br />
                                                        <Spinner size={SpinnerSize.medium} label={<Trans>loading</Trans>} ariaLive='assertive' />
                                                    </div>
                                                </div>
                                                :
                                                this.state.haveGranularAccess
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
                                                                    <Label><Workflow memberslist={this.state.teamMembers} oppStaus={this.state.oppStatus} oppDetails={this.state.oppDetails} /></Label>
                                                                </div>
                                                            </PivotItem>
                                                            <PivotItem linkText={<Trans>teamUpdate</Trans>}>
                                                                <div className='ms-Grid-row mt20 pl15 bg-white'>
                                                                    {
                                                                        this.state.OtherRoleTeamMembers.map((obj, ind) =>
                                                                            obj.length > 1 ?
                                                                                <div className=' ms-Grid-col ms-sm12 ms-md8 ms-lg4 p-5' key={ind}>
                                                                                    <GroupEmployeeStatusCard members={obj} status={obj[0].status} isDispOppStatus={false} role={obj[0].assignedRole.adGroupName} isTeam='true' />
                                                                                </div>
                                                                                :
                                                                                obj.map((member, j) => {
                                                                                    return (
                                                                                        <div className=' ms-Grid-col ms-sm12 ms-md8 ms-lg4 p-5' key={j}>
                                                                                            <TeamUpdate memberslist={member} channelId={channelId} groupId={this.state.groupId} OppName={this.state.OppName} />
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
                                                                                    <TeamUpdate memberslist={member} channelId={channelId} groupId={this.state.groupId} OppName={this.state.OppName} />
                                                                                </div>
                                                                            );
                                                                        })
                                                                    }
                                                                </div>
                                                            </PivotItem>
                                                            <PivotItem linkText={<Trans>notes</Trans>} width='100%' itemKey="Notes" >
                                                                <div className='ms-Grid-col ms-sm12 ms-md8 ms-lg12' >
                                                                    <OpportunityNotes userProfile={[]} opportunityData={this.state.oppDetails} opportunityId={this.state.oppDetails.id} />
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