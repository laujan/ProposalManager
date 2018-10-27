import React, { Component } from 'react';
import { PrimaryButton, Button } from 'office-ui-fabric-react/lib/Button';
import { Label } from 'office-ui-fabric-react/lib/Label';
import { Link } from 'office-ui-fabric-react/lib/Link';
import { Link as LinkRoute } from 'react-router-dom';

import { OpportunitySummary } from './OpportunitySummary';
import { OpportunityNotes } from './OpportunityNotes';
import { OpportunityStatus } from './OpportunityStatus';

import { getQueryVariable } from '../../common';

import {
	Spinner,
	SpinnerSize
} from 'office-ui-fabric-react/lib/Spinner';
import { MessageBar } from 'office-ui-fabric-react/lib/MessageBar';
import { Trans } from "react-i18next";


export class OpportunityDetails extends Component {
	displayName = OpportunityDetails.name

	constructor(props) {
		super(props);

		this.sdkHelper = window.sdkHelper;
		this.authHelper = window.authHelper;

		const userProfile = this.props.userProfile;

		const oppId = this.props.opportunityId;
		
		

		//const oppComponent = this.props.oppComponent;
		const oppComponent = getQueryVariable('oppComponent');
		
		let oppComponentValue = oppComponent;
		if (!oppComponentValue) {
			oppComponentValue = "Summary";
		}

        this.state = {
            loading: true,
            oppData: "",
            menuLevel: 'Level2',
            userProfile: userProfile,
            oppId: oppId,
            oppComponent: oppComponentValue,
            teamMembers: [],
            userAssignedRole: ""
        };
	}

	componentWillMount() {
		
		this.getOppDetails();
	}


	getOppDetails() {
		
		let requestUrl = 'api/Opportunity/?id=' + this.state.oppId;
		
		if (!this.state.oppData) {
			fetch(requestUrl, {
				method: "GET",
				headers: { 'authorization': 'Bearer ' + this.authHelper.getWebApiToken() }
			})
				.then(response => response.json())
				.then(data => {
					try {

						let currentUserId = this.state.userProfile.id;


						let teamMembers = data.teamMembers;
						let teamMemberDetails = data.teamMembers.filter(function (k) {
							return k.id === currentUserId;
						});

						//let userAssignedRole = teamMemberDetails[0].assignedRole.displayName;

						

						this.setState({
							loading: false,
							oppData: data,
							teamMembers: teamMembers,
							//userAssignedRole: userAssignedRole

						});

					}
					catch (err) {
						console.log(err);

					}
				});
		}
	}



	render() {
		const oppId = this.state.oppId;
		const userProfile = this.state.userProfile;
		const teamMembers = this.state.oppData.TeamMembers;	
		let oppData = this.state.oppData;
		
		
		const oppComponent = this.state.oppComponent;

		return (
			<div className='ms-Grid'>
				<div className='ms-Grid-row'>
					<div className='ms-Grid-col ms-sm12 ms-md8 ms-lg12' >
						</div>
					{this.state.loading
						?
                        <div className='ms-BasicSpinnersExample'>
                            <Spinner size={SpinnerSize.large} label={<Trans>loading</Trans>} ariaLive='assertive' />
						</div>
						:
							oppComponent === "Notes"
								?
								<div className='ms-Grid-col ms-sm12 ms-md8 ms-lg12' >

									<OpportunityNotes userProfile={userProfile} opportunityData={this.state.oppData} opportunityId={oppId} />


								</div>
								:
								oppComponent === "Status"
									?
									<div className='ms-Grid-col ms-sm12 ms-md8 ms-lg12' >

										<OpportunityStatus userProfile={userProfile} opportunityData={this.state.oppData} opportunityId={oppId} />
									</div>


									:
									<div className='ms-Grid-col ms-sm12 ms-md8 ms-lg12' >

										<OpportunitySummary userProfile={userProfile} opportunityData={this.state.oppData} opportunityId={oppId} />

									</div>
						
					}
					
				</div>
				</div>
			
		);
	}
}