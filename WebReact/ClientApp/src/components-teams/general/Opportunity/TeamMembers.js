/* 
*  Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. 
*  See LICENSE in the source repository root for complete license information. 
*/

import React, { Component } from 'react';
import { Label } from 'office-ui-fabric-react/lib/Label';
import { Glyphicon } from 'react-bootstrap';
import { PrimaryButton } from 'office-ui-fabric-react/lib/Button';
import { Link } from 'office-ui-fabric-react/lib/Link';
import { LinkContainer } from 'react-router-bootstrap';
import { Persona, PersonaSize } from 'office-ui-fabric-react/lib/Persona';
import { Trans } from "react-i18next";

export class TeamMembers extends Component {
    displayName = TeamMembers.name
	constructor(props) {
        super(props);

        this.state = {
            redirect: false,
            teamName: this.props.opportunityName,
            channelId: "",
			teamWebUrl: "",
			isAdmin: this.props.isAdmin
        };
	}

    render() {
        console.log("TeamMembers_render props :", this.props);
        let enableEditTeam = this.props.haveAccessToEditTeam;
        
        return (
            <div className='ms-Grid'>
                {typeof this.props.memberslist === 'undefined' ? "" :
                    this.props.memberslist.map((member, index) =>
                        member.displayName !== "" ?
                            <div className='ms-Grid-row bg-grey p-5 mr5A' key={index}>
                                <div className=' ms-Grid-col ms-sm12 ms-md8 ms-lg12'>
                                    <Persona
                                        {...{ imageUrl: member.UserPicture, imageInitials: member.displayName ? member.displayName.match(/\b(\w)/g) ? member.displayName.match(/\b(\w)/g).join('') : "" : "" }}
                                        size={PersonaSize.size40}
                                        text={member.displayName}
                                        secondaryText={member.adGroupName}
                                    />
                                    <span>
                                        <p className="pull-right">
                                            <Link href={"mailto:" + member.userPrincipalName}> <Glyphicon glyph='envelope' /></Link>&nbsp;&nbsp;&nbsp;
                                           
                                        </p>
                                    </span>
                                </div>
                            </div>
                            : ""
                    )
                }
                {
                    <div className='ms-Grid-row p-10'> 
						<div className='ms-Grid ms-sm12 ms-md12 ms-lg12'>
							{
							enableEditTeam
								?
								<LinkContainer to={'./ChooseTeam?opportunityId=' + this.props.createTeamId} >
									<PrimaryButton className='ModifyButton'><Trans>editTeamCollaboration</Trans> </PrimaryButton>
								</LinkContainer>
								:
								<PrimaryButton className='ModifyButton' disabled><Trans>editTeamCollaboration</Trans></PrimaryButton>
							}
							<br />
						</div>
						<div className='ms-Grid ms-sm12 ms-md12 ms-lg12'>
							{this.props.opportunityState === 1
								?
                                <Label><Trans>editTeamSetupMessage</Trans> </Label>

								:
								""
							}
							</div>
                        </div>
                }
            </div>
        );
    }
}
export default TeamMembers;