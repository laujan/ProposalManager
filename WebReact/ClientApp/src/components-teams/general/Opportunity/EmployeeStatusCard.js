/* 
*  Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. 
*  See LICENSE in the source repository root for complete license information. 
*/

import React, { Component } from 'react';
import { Label } from 'office-ui-fabric-react/lib/Label';
import { Persona, PersonaSize } from 'office-ui-fabric-react/lib/Persona';
import '../../../Style.css';
import { oppStatus, oppStatusText } from '../../../common';
import { Trans } from "react-i18next";

export class EmployeeStatusCard extends Component {
    displayName = EmployeeStatusCard.name

    constructor(props) {
        super(props);
    }

    render() {
        const { status, image, name, role, isDispOppStatus } = this.props;
        let statusDisplay = "";
        let statusClassName = "";

        if (isDispOppStatus) {
            statusDisplay = oppStatusText[status];
            statusClassName = status === 0 ? "status" + status : "status" + (status - 1);
        } else {
            statusDisplay = oppStatus[status];
            statusClassName = "status" + status;
        }        
        
        return (
            <div className=' ms-Grid-col ms-sm6 ms-md8 ms-lg12 bg-grey p-5'>
                <div className='ms-PersonaExample'>
                    {name ?
                        <div>
                        <div className='ms-Grid-row'>
                            <div className='ms-Grid-col ms-sm6 ms-md8 ms-lg4'>
                                <Label><Trans>status</Trans></Label>

                            </div>
                            <div className=' ms-Grid-col ms-sm6 ms-md8 ms-lg8'>
                                    <Label><span className={statusClassName}> <Trans>{statusDisplay}</Trans> </span></Label>

                            </div>
                        </div>
                        <div className='ms-Grid-row'>
                            <div className=' ms-Grid-col ms-sm12 ms-md8 ms-lg12'>
                                <Persona
                                    { ...{ imageUrl: image, imageInitials: "" } }
                                    size={PersonaSize.size40}
                                    primaryText={name}
                                    secondaryText={role}
                                />

                            </div>
                            </div>
                        </div>
                        : 
                        <div>
                            <div className='ms-Grid-row'>
                                <div className='ms-Grid-col ms-sm6 ms-md8 ms-lg4'>
                                    <Label><Trans>status</Trans></Label>

                                </div>
                                <div className=' ms-Grid-col ms-sm6 ms-md8 ms-lg8'>
                                    <Label><span className={statusClassName}> <Trans>{status}</Trans> </span></Label>

                                </div>
                            </div>
                            <div className='ms-Grid-row'>
                                <div className=' ms-Grid-col ms-sm12 ms-md8 ms-lg12'>
                                    <Persona
                                        { ...{
											imageUrl: "data:image/svg+xml;base64,PD94bWwgdmVyc2lvbj0iMS4wIiBlbmNvZGluZz0iVVRGLTgiPz4NCjwhRE9DVFlQRSBzdmcgIFBVQkxJQyAnLS8vVzNDLy9EVEQgU1ZHIDEuMS8vRU4nICAnaHR0cDovL3d3dy53My5vcmcvR3JhcGhpY3MvU1ZHLzEuMS9EVEQvc3ZnMTEuZHRkJz4NCjxzdmcgd2lkdGg9IjQwMXB4IiBoZWlnaHQ9IjQwMXB4IiBlbmFibGUtYmFja2dyb3VuZD0ibmV3IDMxMi44MDkgMCA0MDEgNDAxIiB2ZXJzaW9uPSIxLjEiIHZpZXdCb3g9IjMxMi44MDkgMCA0MDEgNDAxIiB4bWw6c3BhY2U9InByZXNlcnZlIiB4bWxucz0iaHR0cDovL3d3dy53My5vcmcvMjAwMC9zdmciPg0KPGcgdHJhbnNmb3JtPSJtYXRyaXgoMS4yMjMgMCAwIDEuMjIzIC00NjcuNSAtODQzLjQ0KSI+DQoJPHJlY3QgeD0iNjAxLjQ1IiB5PSI2NTMuMDciIHdpZHRoPSI0MDEiIGhlaWdodD0iNDAxIiBmaWxsPSIjRTRFNkU3Ii8+DQoJPHBhdGggZD0ibTgwMi4zOCA5MDguMDhjLTg0LjUxNSAwLTE1My41MiA0OC4xODUtMTU3LjM4IDEwOC42MmgzMTQuNzljLTMuODctNjAuNDQtNzIuOS0xMDguNjItMTU3LjQxLTEwOC42MnoiIGZpbGw9IiNBRUI0QjciLz4NCgk8cGF0aCBkPSJtODgxLjM3IDgxOC44NmMwIDQ2Ljc0Ni0zNS4xMDYgODQuNjQxLTc4LjQxIDg0LjY0MXMtNzguNDEtMzcuODk1LTc4LjQxLTg0LjY0MSAzNS4xMDYtODQuNjQxIDc4LjQxLTg0LjY0MWM0My4zMSAwIDc4LjQxIDM3LjkgNzguNDEgODQuNjR6IiBmaWxsPSIjQUVCNEI3Ii8+DQo8L2c+DQo8L3N2Zz4NCg==",
                                            imageInitials: ""
                                        } }
                                        size={PersonaSize.size40}
                                        primaryText={<Trans>userNotSelected</Trans>}
                                        secondaryText={role}
                                    />
                                    
                                </div>
                            </div>
                            
                        </div>
                    }
                </div>
            </div>
        );
    }
}
export default EmployeeStatusCard;