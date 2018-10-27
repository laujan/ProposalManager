/* 
*  Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. 
*  See LICENSE in the source repository root for complete license information. 
*/

import React, { Component } from 'react';
import { Link as LinkRoute } from 'react-router-dom';
import { TeamMembers } from './TeamMembers';
import { Workflow } from '../../Proposal/Workflow';
import {
    Spinner,
    SpinnerSize
} from 'office-ui-fabric-react/lib/Spinner';

import { Trans } from "react-i18next";


export class OpportunityStatus extends Component {
    displayName = OpportunityStatus.name

    constructor(props) {
        super(props);

        this.sdkHelper = window.sdkHelper;
        this.authHelper = window.authHelper;

        const userProfile = this.props.userProfile;

        const oppId = this.props.opportunityId;

        const opportunityData = this.props.opportunityData;

        this.state = {
            oppId: oppId,
            loading: true,
            teamMembers: [],
            LoanOfficer: [],
            userId: userProfile.id,
            UserRoleList: [],
            OtherRolesMapping: [],
            oppData: opportunityData,
            isDealTypeExist: false
        };

    }

    componentWillMount() {
        this.getUserRoles();



    }

    getOppDetails() {
        let data = this.state.oppData;
        let loanOfficerObj = data.teamMembers.filter(function (k) {
            return k.assignedRole.displayName === "LoanOfficer"; // "loan officer";
        });

        let relManagerObj = data.teamMembers.filter(function (k) {
            return k.assignedRole.displayName === "RelationshipManager"; // "relationshipmanager";
        });

        let isDealTypeExist = data.dealType !== null && data.dealType.id !== null ? true : false;
        if (!isDealTypeExist) {
            this.setState({ isDealTypeExist: false, teamMembers: data.teamMembers, loading: false });
            return false;
        }

        // Get Other role officers list
        let processList = data.dealType.processes;
        // Get Other role officers list
        let otherRolesMapping = processList.filter(function (k) {
            return k.processType.toLowerCase() !== "new opportunity" && k.processType.toLowerCase() !== "start process" && k.processType.toLowerCase() !== "customerdecisiontab" && k.processType.toLowerCase() !== "proposalstatustab";
        });

        this.setState({ OtherRolesMapping: otherRolesMapping });
        let otherRolesArr1 = [];
        for (let j = 0; j < otherRolesMapping.length; j++) {
            let processTeamMember = new Array();
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
                    return _el.assignedRole.displayName === group;
                }).map(function (_el) { return _el; })
            };
        });

        let otherRolesObj = [];
        if (otherRolesArr.length > 1) {
            for (let r = 0; r < otherRolesArr.length; r++) {
                otherRolesObj.push(otherRolesArr[r].users);
            }
        }


        let userId = this.state.userId;
        let currentUser = data.teamMembers.filter(function (k) {
            return k.id === userId;
        });

        let assignedUserRole;
        if (currentUser.length > 0) {
            assignedUserRole = currentUser[0].assignedRole.displayName;
        }
        else {
            assignedUserRole = "";
        }

        this.setState({
            LoanOfficer: loanOfficerObj,
            RelationShipOfficer: relManagerObj,
            OtherRoleOfficers: otherRolesObj,
            teamMembers: data.teamMembers,
            //oppData: data,
            userRole: assignedUserRole,
            loading: false,
            isDealTypeExist: true
        });

        // });
    }

    getUserRoles() {
        // call to API fetch data
        let requestUrl = 'api/RoleMapping';
        fetch(requestUrl, {
            method: "GET",
            headers: { 'authorization': 'Bearer ' + window.authHelper.getWebApiToken() }
        })
            .then(response => response.json())
            .then(data => {
                try {
                    let userRoleList = [];
                    for (let i = 0; i < data.length; i++) {
                        let userRole = {};
                        userRole.id = data[i].id;
                        userRole.roleName = data[i].roleName;
                        userRole.adGroupName = data[i].adGroupName;
                        userRole.processStep = data[i].processStep;
                        userRole.processType = data[i].processType;
                        userRoleList.push(userRole);
                    }
                    this.setState({ UserRoleList: userRoleList });
                    this.getOppDetails();
                }


                catch (err) {
                    return false;
                }

            });

    }
    render() {
        if (this.state.loading) {
            return (
                <div className='ms-BasicSpinnersExample ibox-content pt15 '>
                    <Spinner size={SpinnerSize.large} label='loading...' ariaLive='assertive' />
                </div>
            );
        } else {
            return (
                <div className='ms-Grid'>
                    <div className='ms-Grid-row'>
                        <div className=' ms-Grid-col ms-sm12 ms-md12 ms-lg9 p-r-10'>
                            <div className='ms-Grid-row'>
                                <div className=' ms-Grid-col ms-sm12 ms-md12 ms-lg6 pageheading'>
                                    <h3><Trans>status</Trans></h3>
                                </div>
                                <div className=' ms-Grid-col ms-sm12 ms-md12 ms-lg6'><br />
                                    <LinkRoute to={'./generalDashboardTab'} className='pull-right'><Trans>backToDashboard</Trans> </LinkRoute>
                                </div>
                            </div>
                            <div className='ms-Grid-row p-5'>
                                <div className=' ms-Grid-col ms-sm12 ms-md12 ms-lg12 bg-white pb20'>
                                    <div className='ms-Grid-row pt35'>
                                        {
                                            this.state.isDealTypeExist ?
                                                <Workflow memberslist={this.state.teamMembers} oppStaus={this.state.oppData.opportunityState} oppDetails={this.state.oppData} />
                                                :
                                                <h3><Trans>dealTypeNotSelectedToThisOpportunity</Trans></h3>
                                        }




                                    </div>
                                </div>
                            </div>

                        </div>
                        <div className='ms-Grid-col ms-sm12 ms-md8 ms-lg3 p-l-10 TeamMembersBG'>
                            <h3>Team Members</h3>
                            <TeamMembers memberslist={this.state.teamMembers} createTeamId={this.state.oppId} opportunityState={this.state.oppData.opportunityState} userRole={this.state.userRole} />
                        </div>
                    </div>


                </div>

            );
        }
    }
}