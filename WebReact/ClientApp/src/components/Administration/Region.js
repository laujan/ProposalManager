/*
*  Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license.
*  See LICENSE in the source repository root for complete license information.
*/

import React, { Component } from 'react';
import { TextField } from 'office-ui-fabric-react/lib/TextField';
import { IconButton } from 'office-ui-fabric-react/lib/Button';
import { DetailsList, DetailsListLayoutMode, SelectionMode } from 'office-ui-fabric-react/lib/DetailsList';
import { Link } from 'office-ui-fabric-react/lib/Link';
import Utils from '../../helpers/Utils';
import {
	Spinner,
	SpinnerSize
} from 'office-ui-fabric-react/lib/Spinner';
import { MessageBar, MessageBarType } from 'office-ui-fabric-react/lib/MessageBar';
import { Trans } from "react-i18next";

export class Region extends Component {


	displayName = Region.name

	constructor(props) {
		super(props);

		this.sdkHelper = window.sdkHelper;
		this.authHelper = window.authHelper;

		this.utils = new Utils();
		const columns = [
			{
                key: 'column1',
                name: <Trans>region</Trans>,
                headerClassName: 'ms-List-th browsebutton RegionCol',
                className: 'docs-TextFieldExample ms-Grid-col ms-sm12 ms-md12 ms-lg8 RegionCol',
				fieldName: 'Region',
				minWidth: 150,
				maxWidth: 250,
				isRowHeader: true,
				onRender: (item) => {
					return (
                        <TextField
                            id={'txtRegion' + item.id} 
                            value={item.name}
                            onBlur={(e) => this.onBlurRegionName(e, item)}
                        />
					);
				}
			},
			{
				key: 'column2',
                name: <Trans>action</Trans>,
                headerClassName: 'ms-List-th Regionaction',
                className: 'ms-Grid-col ms-sm12 ms-md12 ms-lg4 Regionaction',
				minWidth: 16,
				maxWidth: 16,
				onRender: (item) => {
					return (
						<div>
							<IconButton iconProps={{ iconName: 'Delete' }} onClick={e => this.deleteRow(item)} />
						</div>
					);
				}
			}
		];

		let rowCounter = 0;

		this.state = {
			items: [],
			rowItemCounter: rowCounter,
			columns: columns,
			isCompactMode: false,
			loading: true,
			MessagebarText: "",
			MessageBarType: MessageBarType.success,
			isUpdateMsg: false
		};

		this.getRegions().then();
	}

	componentWillMount() {
		//this.getRegions();
	}

    async getRegions() {
        let regionList = [];
        let regionList_length = 0;
        try{
            // call to API fetch Categories
            let requestUrl = 'api/Region';
            let response = await fetch(requestUrl, {
                method: "GET",
                headers: { 'authorization': 'Bearer ' + this.authHelper.getWebApiToken() }
            });
            let data = await response.json();
            if(typeof data === 'string'){
                console.log("Region_getRegions : ", data);
            }else if(typeof data === 'object'){
                console.log("Region_getRegions : ", data);
                for (let i = 0; i < data.length; i++) {
                    let region = {};
                    region.id = data[i].id;
                    region.name = data[i].name;
                    region.operation = "update";
                    regionList.push(region);
                }
                regionList_length = regionList.length;
            }
            else{
                throw new Error("response is not an expected type : ", data);
            }
        }catch(error){
            console.log("Region_getRegions Error: ", error.message);
        }finally{
            this.setState({ items: regionList, loading: false, rowItemCounter:  regionList_length});
        }
    }

	createItem(key) {
		return {
			id: key,
			name: "",
			type: "new"
		
		};
	}

	onAddRow() {
		let rowCounter = this.state.rowItemCounter + 1;
		let newItems = [];
		newItems.push(this.createItem(rowCounter));

		let currentItems = this.state.items.concat(newItems);

		this.setState({
			items: currentItems,
			rowItemCounter: rowCounter
		});
	}

	deleteRow(item) {
		console.log(item);
		let currentItems = this.state.items.filter(x => x.id !== item.id);

		this.region = currentItems;
		this.deleteItem(item.id);
		this.setState({
            items: currentItems,
            MessagebarText: <Trans>regionDeletedSuccess</Trans>,
			isUpdate: false,
			isUpdateMsg: true
		});

		setTimeout(function () { this.setState({ isUpdateMsg: false, MessageBarType: MessageBarType.success, MessagebarText: "" }); }.bind(this), 3000);
	}

	deleteItem(id) {
		return new Promise((resolve, reject) => {

			let requestUrl = 'api/Region/'+id;
			let options = {
				method: "DELETE",
				headers: {
					'Accept': 'application/json',
					'Content-Type': 'application/json',
					'authorization': 'Bearer ' + this.authHelper.getWebApiToken()
				}
			};

			fetch(requestUrl, options)
				.then(response => {
					console.log("Delete Region response: " + response.status + " - " + response.statusText);
					if (response.status === 401) {
						// TODO: For v2 see how we pass to authHelper to force token refresh
					}
					return response;
				})
				.then(data => {
					
					resolve(data);
				})
				.catch(err => {
					//this.errorHandler(err, "");
					this.setState({
                        MessagebarText: <Trans>errorOoccuredPleaseTryAgain</Trans>,
						isUpdate: false,
						isUpdateMsg: true
					});
					setTimeout(function () { this.setState({ isUpdateMsg: false, MessageBarType: MessageBarType.error, MessagebarText: "" }); }.bind(this), 3000);
					//this.hideMessagebar();
					reject(err);
				});
		});
	}


	onBlurRegionName(e, item) {
        //check Region already exist in items
        for (let p = 0; p < this.state.items.length; p++) {
            if (this.state.items[p].name.toLowerCase() === e.target.value.toLowerCase()) {
                this.setState({
                    isUpdate: false,
                    isUpdateMsg: true,
                    MessagebarText: <Trans>regionExist</Trans>,
                    MessageBarType: MessageBarType.error
                });
                setTimeout(function () { this.setState({ isUpdateMsg: false, MessageBarType: "", MessagebarText: "" }); }.bind(this), 3000);
                return false;
            }
        }
        let updatedItems = this.state.items;
		let itemIdx = updatedItems.indexOf(item);
		updatedItems[itemIdx].name = e.target.value;

		this.region = updatedItems;

		let currentRegion = {};
		currentRegion.id = item.id;
		currentRegion.name = item.name;
		let messagebarText;
		if (item.type === "new") {
            this.createNewItem(currentRegion);
            messagebarText = <Trans>regionAddedSuccess</Trans>;
		}
		else {
            this.updateItem(currentRegion);
            messagebarText = <Trans>regionUpdatedSuccess</Trans>;
		}
		this.setState({
			items: updatedItems,
			MessagebarText: messagebarText,
			isUpdate: false,
			isUpdateMsg: true
		});
		setTimeout(function () { this.setState({ isUpdateMsg: false, MessageBarType: MessageBarType.success, MessagebarText: "" }); }.bind(this), 3000);

		//console.log(updatedItems);

		
	}

	createNewItem(region) {
		return new Promise((resolve, reject) => {

			let requestUrl = 'api/Region';
			let options = {
				method: "POST",
				headers: {
					'Accept': 'application/json',
					'Content-Type': 'application/json',
					'authorization': 'Bearer ' + this.authHelper.getWebApiToken()
				},
				body: JSON.stringify(region)
			};

			fetch(requestUrl, options)
				.then(response => {
					console.log("Update Region response: " + response.status + " - " + response.statusText);
					if (response.status === 401) {
						// TODO: For v2 see how we pass to authHelper to force token refresh
					}
					return response;
				})
				.then(data => {
		
					resolve(data);
				})
				.catch(err => {
					//this.errorHandler(err, "");
					this.setState({
                        MessagebarText: <Trans>errorOoccuredPleaseTryAgain</Trans>,
						isUpdate: false,
						isUpdateMsg: true
					});
					setTimeout(function () { this.setState({ isUpdateMsg: false, MessageBarType: MessageBarType.error, MessagebarText: "" }); }.bind(this), 3000);
					//this.hideMessagebar();
					reject(err);
				});
		});
	}



	updateItem(region) {
		return new Promise((resolve, reject) => {
			
			let requestUrl = 'api/Region';
			let options = {
				method: "PATCH",
				headers: {
					'Accept': 'application/json',
					'Content-Type': 'application/json',
					'authorization': 'Bearer ' + this.authHelper.getWebApiToken()
				},
				body: JSON.stringify(region)
			};

			fetch(requestUrl, options)
				.then(response => {
					console.log("Update Region response: " + response.status + " - " + response.statusText);
					if (response.status === 401) {
						// TODO: For v2 see how we pass to authHelper to force token refresh
					}
					return response;
				})
				.then(data => {
					resolve(data);
				})
				.catch(err => {
					//this.errorHandler(err, "");
					this.setState({
						updateStatus: true,
                        MessagebarText: <Trans>errorOoccuredPleaseTryAgain</Trans>
					});
					this.hideMessagebar();
					reject(err);
				});
		});
	}

	
	regionList(columns, isCompactMode, items, selectionDetails) {
		return (
            <div className='ms-Grid-row LsitBoxAlign p20ALL'>
                <DetailsList
                    items={items}
                    compact={isCompactMode}
                    columns={columns}
                    selectionMode={SelectionMode.none}
                    selectionPreservedOnEmptyClick='true'
                    setKey='set'
                    layoutMode={DetailsListLayoutMode.justified}
                    enterModalSelectionOnTouch='false'
                />
			</div>
		);
	}



	render() {
		const { columns, isCompactMode, items, selectionDetails } = this.state;
		const regionList = this.regionList(columns, isCompactMode, items, selectionDetails);
		
		
		if (this.state.loading) {
			return (
				<div className='ms-BasicSpinnersExample ibox-content pt15 '>
                    <Spinner size={SpinnerSize.large} label={<Trans>loading</Trans>} ariaLive='assertive' />
				</div>
			);
		} else {
			return (

                <div className='ms-Grid bg-white ibox-content '>
					

					<div className='ms-Grid-row'>
						<div className='ms-Grid-col ms-sm12 ms-md12 ms-lg12'>
                            <div className='ms-Grid-col ms-sm12 ms-md12 ms-lg12 pt10'>
                                <Link href='' className='pull-left' onClick={() => this.onAddRow()} >+ <Trans>addNew</Trans></Link>
                            </div>
								{regionList}
						</div>
					</div>

					<div className='ms-Grid-row'>
						<div className='ms-Grid-col ms-sm12 ms-md12 ms-lg12'>
							<div className='ms-BasicSpinnersExample p-10'>
								{
									this.state.isUpdate ?
										<Spinner size={SpinnerSize.large} ariaLive='assertive' />
										: ""
								}
								{
									this.state.isUpdateMsg ?
                                        <MessageBar
                                            messageBarType={this.state.MessageBarType}
                                            isMultiline={false}
                                        >
											{this.state.MessagebarText}
										</MessageBar>
										: ""
								}
							</div>
						</div>
					</div>
				</div>
			);

		}
	}

}