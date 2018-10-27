/* 
*  Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. 
*  See LICENSE in the source repository root for complete license information. 
*/

import React, { Component } from 'react';
import * as ReactDOM from 'react-dom';
import * as microsoftTeams from '@microsoft/teams-js';
import { DefaultButton, PrimaryButton, IconButton, ActionButton } from 'office-ui-fabric-react/lib/Button';
import { I18n, Trans } from "react-i18next";
import { Link as LinkRoute } from 'react-router-dom';
import {
    Spinner,
    SpinnerSize
} from 'office-ui-fabric-react/lib/Spinner';
import { MessageBar, MessageBarType } from 'office-ui-fabric-react/lib/MessageBar';
import {
    Persona,
    PersonaSize,
    PersonaPresence
} from 'office-ui-fabric-react/lib/Persona';

export class PreviewDealType extends Component {
    displayName = PreviewDealType.name

    constructor(props) {
        super(props);

        this.sdkHelper = window.sdkHelper;
        this.authHelper = window.authHelper;
        this.state = {
            loading: true,
            totalGroups: 0,
            dealTypeGroups: {}
        };

    }

    componentWillMount() {
        // Get the teams context
        //if (this.state.channelName.length === 0) {
            // this.getTeamsContext();
        //}
        
        this.setState({
            dealTypeObject: this.props.dealTypeObject,
            loading: true
        });

        this.dealTypeGroupList();
    }

    dealTypeGroupList() {
        let dealTypeObj = this.props.dealTypeObject;
        let processObj = dealTypeObj.processes;

        if (Object.keys(dealTypeObj).length > 0) {
            let dealTypeProcess = processObj.filter(function (k) {
                return k.processType.toLowerCase() !== "base" && k.processType.toLowerCase() !== "customerdecisiontab" && k.processType.toLowerCase() !== "start process" && k.processType.toLowerCase() !== "new opportunity"
                 && k.processType.toLowerCase() !== "proposalstatustab";
            });
            Array.prototype.selectedProcessGroupBy = function (prop) {
                return this.reduce(function (groups, item) {
                    const val = parseInt(item[prop])
                    groups[val] = groups[val] || []
                    groups[val].push(item)
                    return groups
                }, {})
            };

            let groupedByOrder = dealTypeProcess.selectedProcessGroupBy('order');
            //console.log(groupedByOrder);
            this.setState({
                dealTypeObject: this.props.dealTypeObject,
                totalGroups: Object.keys(groupedByOrder).length,
                dealTypeGroups: groupedByOrder
            });
        }
        this.setState({
            loading: false
        });
        
    }

    displayPersonaCard(stepName) {
        return (
            <div>
                <i className="ms-Icon ms-Icon--ArrangeBringForward" aria-hidden="true"></i>
                &nbsp;&nbsp;<span><Trans>{stepName}</Trans></span>
                <div className=' ms-Grid-col ms-sm6 ms-md8 ms-lg12 bg-grey p-5'>
                    <div className='ms-PersonaExample'>
                        <Persona
                            {...{
                                imageUrl: "data:image/svg+xml;base64,PD94bWwgdmVyc2lvbj0iMS4wIiBlbmNvZGluZz0iVVRGLTgiPz4NCjwhRE9DVFlQRSBzdmcgIFBVQkxJQyAnLS8vVzNDLy9EVEQgU1ZHIDEuMS8vRU4nICAnaHR0cDovL3d3dy53My5vcmcvR3JhcGhpY3MvU1ZHLzEuMS9EVEQvc3ZnMTEuZHRkJz4NCjxzdmcgd2lkdGg9IjQwMXB4IiBoZWlnaHQ9IjQwMXB4IiBlbmFibGUtYmFja2dyb3VuZD0ibmV3IDMxMi44MDkgMCA0MDEgNDAxIiB2ZXJzaW9uPSIxLjEiIHZpZXdCb3g9IjMxMi44MDkgMCA0MDEgNDAxIiB4bWw6c3BhY2U9InByZXNlcnZlIiB4bWxucz0iaHR0cDovL3d3dy53My5vcmcvMjAwMC9zdmciPg0KPGcgdHJhbnNmb3JtPSJtYXRyaXgoMS4yMjMgMCAwIDEuMjIzIC00NjcuNSAtODQzLjQ0KSI+DQoJPHJlY3QgeD0iNjAxLjQ1IiB5PSI2NTMuMDciIHdpZHRoPSI0MDEiIGhlaWdodD0iNDAxIiBmaWxsPSIjRTRFNkU3Ii8+DQoJPHBhdGggZD0ibTgwMi4zOCA5MDguMDhjLTg0LjUxNSAwLTE1My41MiA0OC4xODUtMTU3LjM4IDEwOC42MmgzMTQuNzljLTMuODctNjAuNDQtNzIuOS0xMDguNjItMTU3LjQxLTEwOC42MnoiIGZpbGw9IiNBRUI0QjciLz4NCgk8cGF0aCBkPSJtODgxLjM3IDgxOC44NmMwIDQ2Ljc0Ni0zNS4xMDYgODQuNjQxLTc4LjQxIDg0LjY0MXMtNzguNDEtMzcuODk1LTc4LjQxLTg0LjY0MSAzNS4xMDYtODQuNjQxIDc4LjQxLTg0LjY0MWM0My4zMSAwIDc4LjQxIDM3LjkgNzguNDEgODQuNjR6IiBmaWxsPSIjQUVCNEI3Ii8+DQo8L2c+DQo8L3N2Zz4NCg==",
                                imageInitials: ""
                            }}
                            size={PersonaSize.size40}
                            text={<Trans>name</Trans>}
                            secondaryText={<Trans>role</Trans>}
                        />
                    </div>
                </div>
            </div>
            );

    }

    render() {
        const { loading } = this.state;
        let dealTypeObj = this.props.dealTypeObject;
        let groups = this.state.dealTypeGroups;
        const groupsArry = Object.keys(groups).map(i => groups[i]);

        
        if (this.state.loading) {
            return (
                <div className='ms-BasicSpinnersExample ibox-content pt15 '>
                    <Spinner size={SpinnerSize.large} label={<Trans>loading</Trans>} ariaLive='assertive' />
                </div>
            );
        } else {
            return (
                <div className='ms-Grid bg-white'>
                    <div className='ms-Grid-row hScrollDealType'>
                        {
                            this.state.totalGroups === 0 ?
                                <div className='ms-Grid-col ms-sm6 ms-md6 ms-lg12'>
                                    <h4><Trans>previewNotAvailable</Trans></h4>
                                </div>
                                :
                                <div className="mainDivScroll">
                                    <div className='ms-Grid-row'>
                                        <div className='ms-Grid-col ms-sm6 ms-md6 ms-lg12'>
                                            <h4>{dealTypeObj.templateName}</h4>
                                        </div>
                                    </div>
                                    <div className="staticProcess1">
                                        <div className='ms-Grid-row'>
                                            <div className=' ms-Grid-col ms-sm12 ms-md12 ms-lg6  bg-gray newOpBg columnwidth'>
                                                {this.displayPersonaCard('newOpportunity')}
                                            </div>
                                            <div className=' ms-Grid-col ms-sm12 ms-md12 ms-lg6 newOpBg columnwidth'>
                                                {this.displayPersonaCard('startProcess')}
                                            </div>
                                        </div>

                                    </div>
                                    <div className="dynamicProcess">
                                        <div className='ms-Grid-row'>
                                            {
                                                this.state.totalGroups === 1 ?
                                                    <div className='ms-Grid-col ms-sm3 ms-md3 ms-lg3 divUserRolegroup-arrow columnwidth'>
                                                        <div className='divUserRolegroup'>
                                                            <div className="ms-Grid-row bg-white p-10">
                                                                {
                                                                    groupsArry.map((k, i) =>
                                                                        <div className="ms-Grid-row bg-white GreyBorder" key={i}>
                                                                            <div className="ms-Grid-col ms-sm6 ms-md6 ms-lg12">
                                                                                {k.map((m, n) => <div className="ms-Grid-col ms-sm12 ms-md12 ms-lg12" key={n}>{this.displayPersonaCard(m.processStep)}</div>)}
                                                                            </div><br /><br />
                                                                        </div>
                                                                    )
                                                                }
                                                            </div>


                                                        </div>
                                                    </div>
                                                    :
                                                    <div className="">
                                                        {
                                                            groupsArry.map((k, i) =>
                                                                <div className='ms-Grid-col ms-sm3 ms-md3 ms-lg3 divUserRolegroup-arrow columnwidth' key={i}>
                                                                    <div className="ms-Grid-row bg-white GreyBorder">
                                                                        <div className="ms-Grid-col ms-sm6 ms-md6 ms-lg12">
                                                                            {k.map((m, n) => <div className="ms-Grid-col ms-sm12 ms-md12 ms-lg12" key={n}>{this.displayPersonaCard(m.processStep)}</div>)}
                                                                        </div><br /><br />
                                                                    </div>
                                                                </div>
                                                            )
                                                        }

                                                    </div>

                                            }
                                        </div>

                                    </div>
                                    <div className="staticProcess2 columnwidth divUserRolegroup-arrow">
                                        <div className='ms-Grid-row'>
                                            <div className='ms-Grid-col ms-sm6 ms-md6 ms-lg12'>
                                                {this.displayPersonaCard('formalProposal')}
                                            </div>
                                        </div>
                                    </div>
                                    <div className="staticProcess2 columnwidth">
                                        <div className='ms-Grid-row'>
                                            <div className='ms-Grid-col ms-sm6 ms-md6 ms-lg12'>
                                                {this.displayPersonaCard('customerDecision')}
                                            </div>
                                        </div>
                                    </div>

                                </div>

                               
                        }

                    </div>
                    
                </div>
            );
        }
    }
}