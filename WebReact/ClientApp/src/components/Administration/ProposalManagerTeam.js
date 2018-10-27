/*
*  Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license.
*  See LICENSE in the source repository root for complete license information.
*/

import React, { Component } from 'react';


import { Spinner, SpinnerSize } from 'office-ui-fabric-react/lib/Spinner';
import { MessageBar, MessageBarType } from 'office-ui-fabric-react/lib/MessageBar';
import Utils from '../../helpers/Utils';
import '../../Style.css';
import { Trans } from "react-i18next";
import i18n from '../../i18n';
import { DefaultButton, IconButton } from 'office-ui-fabric-react/lib/Button';

export class ProposalManagerTeam extends Component {
	displayName = ProposalManagerTeam.name

	constructor(props) {
		super(props);

		this.sdkHelper = window.sdkHelper;
		this.authHelper = window.authHelper;
		this.utils = new Utils();

		this.state = {
			userProfile: this.props.userProfile,
			loading: true,
			refreshing: false,
			items: [],
			itemsOriginal: [],
			userRoleList: [],
			channelCounter: 0
		};
	}

	componentWillMount() {
		
	}

	fetchResponseHandler(response, referenceCall) {
		if (response.status === 401) {
			//TODO: Handle refresh token in vNext;
		}
	}

	errorHandler(err, referenceCall) {
		console.log("Administration Ref: " + referenceCall + " error: " + JSON.stringify(err));
	}

	createChannel(teamId, name, description) {
		return new Promise((resolve, reject) => {
			this.sdkHelper.createChannel(name, description, teamId)
				.then((res) => {
					resolve(res, null);
				})
				.catch(err => {
					console.log("Administration_createChannel error: ");
					console.log(err);
					resolve(null, err);
				});
		});
	}

	createNextChannel(teamId, item) {
		let channelCounter = this.state.channelCounter;
		const roleMappings = this.state.userRoleList;
		console.log("Administration_createNextChannel start channelCounter: " + channelCounter);
		if (roleMappings.length > channelCounter) {
			let channelName = roleMappings[channelCounter].channel;

			if (channelName !== "General" && channelName !== "None") {
				this.createChannel(teamId, channelName, channelName + " channel")
					.then((res, err) => {
						console.log("Administration_createNextChannel channelCounter: " + channelCounter + " lenght: " + roleMappings.length);
						this.setState({ channelCounter: channelCounter + 1 });
						this.createNextChannel(teamId, item);
					})
					.catch(err => {
						this.errorHandler(err, "createNextChannel_createChannel: " + channelName);
					});
			} else {
				this.setState({ channelCounter: channelCounter + 1 });
				this.createNextChannel(teamId, item);
			}
		} else {
			//this.setState({ channelCounter: 0 });
			console.log("Administration_createNextChannel finished channelCounter: " + channelCounter);
			this.showMessageBar(i18n.t('updatingOpportunityStateAndMovingFilesToTeam') + item.opportunity + "," + i18n.t('pleaseDoNotCloseOeBrowseToOtherItemsUntilCreationProcessIsComplete'), MessageBarType.warning);
			setTimeout(this.chngeOpportunityState, 4000, item.id);
			this.getOpportunity(item.id)
				.then(res => {
					res.opportunityState = 2;
					this.updateOpportunity(res)
						.then(res => {
							this.hideMessageBar();
							this.setState({
								loading: true
							});
							setTimeout(this.chngeOpportunityState, 2000, item.id);
							this.getOpportunityIndex()
								.then(data => {
									console.log("Administration_createNextChannel finished after getOpportunityIndex channelCounter: " + channelCounter);
									console.log("Administration_createNextChannel Adding team app to teamId: " + teamId);
									this.sdkHelper.addAppToTeam(teamId)
										.then(res => {
											this.setState({
												loading: false,
												channelCounter: 0
											});
										})
										.catch(err => {
											// TODO: Add error message
											this.errorHandler(err, "Administration_createNextChannel_addAppToTeam");
										});
								})
								.catch(err => {
									// TODO: Add error message
									this.errorHandler(err, "Administration_createNextChannel_getOpportunityIndex");
								});
						})
						.catch(err => {
							this.showMessageBar(i18n.t('thereWasaProblemTryingToUpdateOpportunityPleaseTryAgain'), MessageBarType.error);
							this.errorHandler(err, "createNextChannel_updateOpportunity");
						});
				})
				.catch(err => {
					this.showMessageBar(i18n.t('thereWasaProblemTryingToUpdateOpportunityPleaseTryAgain'), MessageBarType.error);
					this.errorHandler(err, "createNextChannel_getOpportunity");
				});
		}
	}

	showMessageBar(text, messageBarType) {
		this.setState({
			result: {
				type: messageBarType,
				text: text
			}
		});
		// MessageBar types:
		// MessageBarType.error
		// MessageBarType.info
		// MessageBarType.severeWarning
		// MessageBarType.success
		// MessageBarType.warning
	}

	hideMessageBar() {
		this.setState({
			result: null
		});
	}


	//Event handlers

	onActionItemClick(item) {
		if (this.state.items.length > 0) {
			this.showMessageBar(i18n.t('creatingTeamAndChannelsFor') + item.opportunity + ", " + i18n.t('pleaseDoNotCloseOeBrowseToOtherItemsUntilCreationProcessIsComplete'), MessageBarType.warning);
			this.createTeam(item.opportunity)
				.then((res, err) => {
					let teamId = res;
					if (err) {
						// Try to get teamId if error is due to existing team
					}
					console.log("onActionItemClick_createTeam start channel creation");
					this.createNextChannel(teamId, item);
				})
				.catch(err => {
					this.errorHandler(err, "onActionItemClick_createTeam");
				});
		}
	}

	render() {
		const items = this.state.items;
		let showActionButton = true;

		return (
			<div className='ms-Grid'>
				<div className='ms-Grid-row'>
					<DefaultButton iconProps={{ iconName: 'BuildQueueNew' }} className={showActionButton ? "" : "hide"} onClick={() => this.onActionItemClick()}>Create Channels</DefaultButton>
				</div>
			</div>
		);
	}
}