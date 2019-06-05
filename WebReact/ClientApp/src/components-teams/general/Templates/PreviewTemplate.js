/* 
*  Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. 
*  See LICENSE in the source repository root for complete license information. 
*/

/* eslint-disable radix */

import React, { Component } from 'react';
import { Trans } from "react-i18next";
import { Persona, PersonaSize } from 'office-ui-fabric-react/lib/Persona';

export class PreviewTemplate extends Component {
    displayName = PreviewTemplate.name

    constructor(props) {
        super(props);
    }

    templateGroupList() {
        let tempObj = {};
        var groupByOrder = function (xs, key) {
            return xs.reduce(function (rv, x) {
                (rv[x[key]] = rv[x[key]] || []).push(x);
                return rv;
            }, {});
        };
        tempObj = groupByOrder(this.props.templateObject.processes, 'groupNumber');
        return tempObj;
    }

    displayPersonaCard(stepName) {
        return (
            <div>
                <i className="ms-Icon ms-Icon--ArrangeBringForward" aria-hidden="true" />
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
        let processGroupObject = this.templateGroupList();
        let keys = Object.keys(processGroupObject);
        let templateObj = this.props.templateObject;
        let totalGroups = Object.keys(processGroupObject).length;

        return (
            <div className='ms-Grid bg-white'>
                <div className='ms-Grid-row hScrollDealType'>
                    {totalGroups === 0 ?
                        <div className='ms-Grid-col ms-sm6 ms-md6 ms-lg12'>
                            <h4><Trans>previewNotAvailable</Trans></h4>
                        </div>
                        :
                        <div className="mainDivScroll">
                            <div className='ms-Grid-row'>
                                <div className='ms-Grid-col ms-sm6 ms-md6 ms-lg12'>
                                    <h4>{templateObj.templateName}</h4>
                                </div>
                            </div>
                            <div className="dynamicProcess">
                                <div className='ms-Grid-row'>
                                    {
                                        <div className="">
                                            {
                                                keys.map((key) => {
                                                    return (
                                                        <div className={parseInt(keys.length) === parseInt(key)+1 ? 'ms-Grid-col ms-sm3 ms-md3 ms-lg3 columnwidth' : 'ms-Grid-col ms-sm3 ms-md3 ms-lg3 divUserRolegroup-arrow columnwidth'} key={key} >
                                                            <div className="ms-Grid-row bg-white GreyBorder">
                                                                <div className="ms-Grid-col ms-sm6 ms-md6 ms-lg12">
                                                                    {processGroupObject[key].map((process, index) => <div className="ms-Grid-col ms-sm12 ms-md12 ms-lg12" key={index}>{this.displayPersonaCard(process.processStep !== "None" ? process.processStep : process.channel)}</div>)}
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
                    }
                </div>
            </div>
        );
    }
}