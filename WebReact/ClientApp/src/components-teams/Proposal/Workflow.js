/* 
*  Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. 
*  See LICENSE in the source repository root for complete license information. 
*/

import React, { Component } from 'react';
import { Label } from 'office-ui-fabric-react/lib/Label';
import { EmployeeStatusCard } from '../../components-teams/general/Opportunity/EmployeeStatusCard';
import { GroupEmployeeStatusCard } from '../../components-teams/general/Opportunity/GroupEmployeeStatusCard';
import {
    Spinner,
    SpinnerSize
} from 'office-ui-fabric-react/lib/Spinner';
import {
    Persona,
    PersonaSize
} from 'office-ui-fabric-react/lib/Persona';
import '../teams.css';
import { Trans } from "react-i18next";


export class Workflow extends Component {
    displayName = Workflow.name

    constructor(props) {
        super(props);
        this.authHelper = window.authHelper;
        this.sdkHelper = window.sdkHelper;

        this.state = {
            TeamMembers: [],
            UserRoleList: [],
            totalGroups: 0
        };
    }

    componentWillMount() {
        this.getUserRoles();
    }
    getUserRoles() {
        // call to API fetch data
        let requestUrl = 'api/RoleMapping';
        fetch(requestUrl, {
            method: "GET",
            headers: { 'authorization': 'Bearer ' + this.authHelper.getWebApiToken() }
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
                }
                catch (err) {
                    return false;
                }

            });
    }

    displayPersonaCard(p) {
        let oppDetails = this.props.oppDetails;
        let oppStatus = this.props.oppStaus;
        let userRole = "";
        let userRoleArry = [];
        let processRole = "";
        let isDispOppStatus = false;
        let processStatus = "";
        
        userRole = p.processStep;
        // filter from UserRole array
        processRole = oppDetails.dealType.processes.filter(function (k) {
            return k.processStep.toLowerCase() === p.processStep.toLowerCase();
        });
        if (processRole.length > 0) {
            userRole = processRole[0].processStep;
        } else {
            userRole = p.processStep;
        }
        if (p.processStep.toLowerCase() === "customer decision") {
            userRoleArry = this.props.memberslist.filter(function (k) {
                return k.assignedRole.displayName.toLowerCase() === "loanofficer";
            });
            processStatus = this.props.oppStaus;
            isDispOppStatus = true;
        } else {
            userRoleArry = this.props.memberslist.filter(function (k) {
                return k.processStep.toLowerCase() === userRole.toLowerCase();
            });
            processStatus = p.status;
        }
        

        return (
            <div className="">
                <i className="ms-Icon ms-Icon--ArrangeBringForward" aria-hidden="true" />
                &nbsp;&nbsp;<span><Trans>{p.processStep}</Trans></span>
                <div className=' ms-Grid-col ms-sm12 ms-md8 ms-lg12 bg-grey p-5'>
                    {
                        userRoleArry.length > 1 ?
                            <GroupEmployeeStatusCard members={userRoleArry} status={processStatus} isDispOppStatus={isDispOppStatus} role={userRole} />
                            :
                            userRoleArry.length === 1 ?
                                userRoleArry.map(officer => {
                                    return (
                                        <div key={officer.id}>
                                            <EmployeeStatusCard key={officer.id}
                                                {...{
                                                    id: officer.id,
                                                    name: officer.displayName,
                                                    image: "",
                                                    role: officer.assignedRole.adGroupName,
                                                    status: processStatus,
                                                    isDispOppStatus: isDispOppStatus
                                                }
                                                }
                                            />
                                        </div>
                                    );
                                }

                                )
                                :
                                <div className=' ms-Grid-col ms-sm12 ms-md8 ms-lg12 bg-grey p-5'>
                                    <div className='ms-PersonaExample'>
                                        <div className='ms-Grid-row'>
                                            <div className='ms-Grid-col ms-sm6 ms-md8 ms-lg4'>
                                                <Label>Status</Label>
                                            </div>
                                            <div className=' ms-Grid-col ms-sm6 ms-md8 ms-lg8'>
                                                <Label><span className='notstarted'> Not Started </span></Label>
                                            </div>
                                        </div>
                                        <div className='ms-Grid-row'>
                                            <div className=' ms-Grid-col ms-sm12 ms-md8 ms-lg12'>
                                                <Persona
                                                    {...{
                                                        imageUrl: "data:image/svg+xml;base64,PD94bWwgdmVyc2lvbj0iMS4wIiBlbmNvZGluZz0iVVRGLTgiPz4NCjwhRE9DVFlQRSBzdmcgIFBVQkxJQyAnLS8vVzNDLy9EVEQgU1ZHIDEuMS8vRU4nICAnaHR0cDovL3d3dy53My5vcmcvR3JhcGhpY3MvU1ZHLzEuMS9EVEQvc3ZnMTEuZHRkJz4NCjxzdmcgd2lkdGg9IjQwMXB4IiBoZWlnaHQ9IjQwMXB4IiBlbmFibGUtYmFja2dyb3VuZD0ibmV3IDMxMi44MDkgMCA0MDEgNDAxIiB2ZXJzaW9uPSIxLjEiIHZpZXdCb3g9IjMxMi44MDkgMCA0MDEgNDAxIiB4bWw6c3BhY2U9InByZXNlcnZlIiB4bWxucz0iaHR0cDovL3d3dy53My5vcmcvMjAwMC9zdmciPg0KPGcgdHJhbnNmb3JtPSJtYXRyaXgoMS4yMjMgMCAwIDEuMjIzIC00NjcuNSAtODQzLjQ0KSI+DQoJPHJlY3QgeD0iNjAxLjQ1IiB5PSI2NTMuMDciIHdpZHRoPSI0MDEiIGhlaWdodD0iNDAxIiBmaWxsPSIjRTRFNkU3Ii8+DQoJPHBhdGggZD0ibTgwMi4zOCA5MDguMDhjLTg0LjUxNSAwLTE1My41MiA0OC4xODUtMTU3LjM4IDEwOC42MmgzMTQuNzljLTMuODctNjAuNDQtNzIuOS0xMDguNjItMTU3LjQxLTEwOC42MnoiIGZpbGw9IiNBRUI0QjciLz4NCgk8cGF0aCBkPSJtODgxLjM3IDgxOC44NmMwIDQ2Ljc0Ni0zNS4xMDYgODQuNjQxLTc4LjQxIDg0LjY0MXMtNzguNDEtMzcuODk1LTc4LjQxLTg0LjY0MSAzNS4xMDYtODQuNjQxIDc4LjQxLTg0LjY0MWM0My4zMSAwIDc4LjQxIDM3LjkgNzguNDEgODQuNjR6IiBmaWxsPSIjQUVCNEI3Ii8+DQo8L2c+DQo8L3N2Zz4NCg==",
                                                        imageInitials: ""
                                                    }}
                                                    size={PersonaSize.size40}
                                                    primaryText="User Not Selected"
                                                    secondaryText={userRole}
                                                />
                                            </div>
                                        </div>
                                    </div>
                                </div>
                    }
                </div>
            </div>
        );
    }



    render() {
        let loading = true;
        let oppDetails = this.props.oppDetails;
        let oppStatus = this.props.oppStaus;
        let teamMembersAll = [];
        teamMembersAll = this.props.memberslist;

        //if (this.props.memberslist.length > 0) {
        //    loading = false;
        //}

        let loanOfficerObj = teamMembersAll.filter(function (k) {
            return k.assignedRole.displayName === "LoanOfficer";
        });

        let relShipManagerObj = teamMembersAll.filter(function (k) {
            return k.assignedRole.displayName === "RelationshipManager";
        });

        let dealTypeObj = oppDetails.dealType;
        //let processObj = dealTypeObj.processes;
        let groupsArry = [];

        if (Object.keys(dealTypeObj).length > 0) {
            let processObj = dealTypeObj.processes.filter(k => k.processType.toLowerCase() !== "proposalstatustab");

            Array.prototype.selectedProcessGroupBy = function (prop) {
                return this.reduce(function (groups, item) {
                    const val = parseInt(item[prop]);
                    groups[val] = groups[val] || [];
                    groups[val].push(item);
                    return groups;
                }, {});
            };

            let groupedByOrder = processObj.selectedProcessGroupBy('order');

            let groups = groupedByOrder; // this.state.dealTypeGroups;
            groupsArry = Object.keys(groups).map(i => groups[i]);
        }


        let otherRolesObj = [];
        if (otherRolesObj.length > 0) {
            loading = false;
        }
        loading = false;


        return (
            <div>
                {
                    loading ?
                        <div className='ms-BasicSpinnersExample pull-center'>
                            <br /><br /> loading...
                            <Spinner size={SpinnerSize.medium} label={<Trans>loading</Trans>} ariaLive='assertive' />
                        </div>
                        :
                        <div className='ms-Grid'>
                            <div className='ms-Grid-row'>
                                <div className=' ms-Grid-col ms-sm12 ms-md12 ms-lg12 p-r-10 '>
                                    <div className='ms-Grid-row p-10 hScrollDealType ScrollHeight'>
                                        <div className="mainDivScroll">
                                            <div className="dynamicProcess">
                                                <div className='ms-Grid-row'>
                                                    {
                                                        this.state.totalGroups === 1 ?
                                                            <div className='ms-Grid-col ms-sm3 ms-md3 ms-lg3 divUserRolegroup-arrow columnwidth'>
                                                                <div className='divUserRolegroup'>
                                                                    <div className="ms-Grid-row bg-white p-10">
                                                                        {
                                                                            groupsArry.map((k, i) => {
                                                                                return (
                                                                                    <div className="ms-Grid-row bg-white" key={i}>
                                                                                        <div className="ms-Grid-col ms-sm6 ms-md6 ms-lg12">
                                                                                            {k.map((m, n) => <div className="ms-Grid-col ms-sm12 ms-md12 ms-lg12" key={n}>{this.displayPersonaCard(m)}</div>)}
                                                                                        </div><br /><br />
                                                                                    </div>
                                                                                );
                                                                            }

                                                                            )
                                                                        }
                                                                    </div>


                                                                </div>
                                                            </div>
                                                            :
                                                            <div className="">
                                                                {
                                                                    groupsArry.map((k, i) => {
                                                                        return (
                                                                            <div className={i === groupsArry.length - 1 ? 'ms-Grid-col ms-sm3 ms-md3 ms-lg3 columnwidth' : 'ms-Grid-col ms-sm3 ms-md3 ms-lg3 divUserRolegroup-arrow columnwidth'} key={i} >
                                                                                <div className="ms-Grid-row bg-white">
                                                                                    <div className="ms-Grid-col ms-sm12 ms-md6 ms-lg12 GreyBorder">
                                                                                        {k.map((m, n) => <div className="ms-Grid-col ms-sm12 ms-md12 ms-lg12" key={n}>{this.displayPersonaCard(m)}</div>)}
                                                                                    </div><br /><br />
                                                                                </div>
                                                                            </div>
                                                                        );
                                                                    }
                                                                    )
                                                                }

                                                            </div>

                                                    }
                                                </div>

                                            </div>
                                        </div>
                                    </div>

                                </div>
                            </div>
                            <div className='ms-Grid-row'>
                                <div className=' ms-Grid-col ms-sm6 ms-md8 ms-lg12  '>
                                    <hr />
                                </div>
                            </div>
                        </div>
                }
            </div>
        );
    }
}