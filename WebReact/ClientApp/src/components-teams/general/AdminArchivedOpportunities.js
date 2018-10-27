/* 
*  Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. 
*  See LICENSE in the source repository root for complete license information. 
*/
import React, { Component } from 'react';
import * as microsoftTeams from '@microsoft/teams-js';

import { Spinner, SpinnerSize } from 'office-ui-fabric-react/lib/Spinner';
import { MessageBar } from 'office-ui-fabric-react/lib/MessageBar';
import { DefaultButton } from 'office-ui-fabric-react/lib/Button';
import { SearchBox } from 'office-ui-fabric-react/lib/SearchBox';

import '../teams.css';
import Utils from '../../helpers/Utils';
import { I18n, Trans } from "react-i18next";
import { oppStatusText, oppStatusClassName } from '../../common';
import { DetailsList, DetailsListLayoutMode, Selection, SelectionMode, IColumn } from 'office-ui-fabric-react/lib/DetailsList';
import { MarqueeSelection } from 'office-ui-fabric-react/lib/MarqueeSelection';


export class AdminArchivedOpportunities extends Component {
    displayName = AdminArchivedOpportunities.name

    constructor(props) {
        super(props);

        this.authHelper = window.authHelper;
        this.sdkHelper = window.sdkHelper;
        this.utils = new Utils();

        const userProfile = { id: "", displayName: "", mail: "", phone: "", picture: "", userPrincipalName: "", roles: [] };

        const columns = [
            {
                key: 'column1',
                name: <Trans>name</Trans>,
                headerClassName: 'DetailsListExample-header',
                className: 'docs-TextFieldExample ms-Grid-col ms-sm12 ms-md12 ms-lg3',
                fieldName: 'name',
                minWidth: 150,
                maxWidth: 350,
                isRowHeader: true,
                isResizable: true,
                onColumnClick: this.onColumnClick,
                onRender: (item) => {
                    return (
                        <div className='ms-List-itemName'>{item.opportunity}</div>
                    );
                }
            },
            {
                key: 'column2',
                name: <Trans>client</Trans>,
                headerClassName: 'DetailsListExample-header',
                className: 'docs-TextFieldExample ms-Grid-col ms-sm12 ms-md12 ms-lg3 clientcolum',
                fieldName: 'client',
                minWidth: 150,
                maxWidth: 350,
                isRowHeader: true,
                isResizable: true,
                onColumnClick: this.onColumnClick,
                onRender: (item) => {
                    return (
                        <div className='ms-List-itemClient'>{item.client}</div>
                    );
                },
                isPadded: true
            },
            {
                key: 'column3',
                name: <Trans>openedDate</Trans>,
                headerClassName: 'DetailsListExample-header',
                className: 'docs-TextFieldExample ms-Grid-col ms-sm12 ms-md12 ms-lg3',
                fieldName: 'openedDate',
                minWidth: 150,
                maxWidth: 350,
                isRowHeader: true,
                isResizable: true,
                onColumnClick: this.onColumnClick,
                onRender: (item) => {
                    return (
                        <div className='ms-List-itemDate AdminDate'>{new Date(item.openedDate).toLocaleDateString(I18n.language)}</div>
                    );
                },
                isPadded: true
            },
            {
                key: 'column4',
                name: <Trans>status</Trans>,
                headerClassName: 'DetailsListExample-header',
                className: 'docs-TextFieldExample ms-Grid-col ms-sm12 ms-md12 ms-lg2',
                fieldName: 'staus',
                minWidth: 150,
                maxWidth: 350,
                isRowHeader: true,
                isResizable: true,
                onColumnClick: this.onColumnClick,
                onRender: (item) => {
                    return (
                        <div className={"ms-List-itemState" + oppStatusClassName[item.statusValue].toLowerCase()}><Trans>{oppStatusText[item.statusValue]}</Trans></div>
                    );
                },
                isPadded: true
            }
        ];

        const itemsOriginal = this.props.items;
        const userRoleList = this.props.userRoleList;

        //let filteredItems = itemsOriginal;
        //TODO Below line commented to show loading of data
        let filteredItems = itemsOriginal.filter(item => item.status.toLowerCase() === "archived");


        this.state = {
            userProfile: userProfile,
            loading: false,
            refreshing: false,
            items: filteredItems,
            itemsOriginal: itemsOriginal,
            userRoleList: userRoleList,
            channelCounter: 0,
            isCompactMode: false,
            columns: columns,
            filterOpportunityName: ""

        };

        this.unArchiveOpportunity = this.unArchiveOpportunity.bind(this);
        this._onFilterByOpportunityName = this._onFilterByOpportunityName.bind(this);

    }

    componentWillMount() {
        this.acquireGraphAdminTokenSilent(); // Call acquire token so it is ready when calling graph using admin token

        console.log("WillMount Archived Opps");
        console.log(this.authHelper.isAuthenticated);
        console.log(this.state.isAuthenticated);

        if (this.authHelper.isAuthenticated()) {
            if (!this.state.isAuthenticated) {
                this.authHelper.callGetUserProfile()
                    .then(userProfile => {
                        this.setState({
                            userProfile: userProfile

                        });
                    });
            }
        }
		/*
		if (this.state.itemsOriginal.length === 0) {
			console.log("Administration_componentWillMount getOpportunityIndex");
			this.getOpportunityIndex()
				.then(data => {
					console.log("Administration_componentWillMount getUserRoles");
					this.getUserRoles()
						.then(res => {
							console.log("Administration_componentWillMount getUserRoles done" + res);
							this.setState({
								loading: false
							});
						})
						.catch(err => {
							// TODO: Add error message
							this.errorHandler(err, "Administration_componentWillMount_getUserRoles");
						});
				})
				.catch(err => {
					// TODO: Add error message
					this.errorHandler(err, "Administration_componentWillMount_getOpportunityIndex");
				});
		}
		*/

    }

    fetchResponseHandler(response, referenceCall) {
        if (response.status === 401) {
            //TODO: Handle refresh token in vNext;
        }
    }

    errorHandler(err, referenceCall) {
        console.log("Administration Ref: " + referenceCall + " error: " + JSON.stringify(err));
    }

    acquireGraphAdminTokenSilent() {
        if (this.utils.getQueryVariable("admin_consent")) {
            let isAdmin = this.state.userProfile.roles.filter(x => x.displayName === "Administrator");
            if (isAdmin) {
                this.authHelper.loginPopupGraphAdmin()
                    .then(access_token => {
                        // TODO: For future expansion sice the toke has been handled by authHelper
                    })
                    .catch(err => {
                        console.log(err);
                        this.errorHandler(err, "Administration_acquireGraphAdminTokenSilent");
                    });
            }
        } else {
            let isAdmin = this.state.userProfile.roles.filter(x => x.displayName === "Administrator");
            if (isAdmin) {
                this.authHelper.acquireGraphAdminTokenSilent()
                    .then(access_token => {
                        // TODO: For future expansion sice the toke has been handled by authHelper
                    })
                    .catch(err => {
                        console.log(err);
                        this.errorHandler(err, "Administration_acquireGraphAdminTokenSilent");
                        //this.showMessageBar("Error while requesting an admin token for Graph API, please try refreshing your browser and sign-in again.", MessageBarType.error);
                    });
            }
        }
    }

    getOpportunityIndex() {
        return new Promise((resolve, reject) => {
            // To get the List of Opportunities to Display on Dashboard page
            let requestUrl = 'api/Opportunity?page=1';

            fetch(requestUrl, {
                method: "GET",
                headers: { 'authorization': 'Bearer ' + this.authHelper.getWebApiToken() }
            })
                .then(response => {
                    if (response.ok) {
                        return response.json();
                    } else {
                        this.fetchResponseHandler(response, "Administration_getOpportunityIndex");
                        reject(response);
                    }
                })
                .then(data => {
                    let itemslist = [];

                    if (data.ItemsList.length > 0) {
                        for (let i = 0; i < data.ItemsList.length; i++) {

                            let item = data.ItemsList[i];

                            let newItem = {};

                            newItem.id = item.id;
                            newItem.opportunity = item.displayName;
                            newItem.client = item.customer.displayName;
                            newItem.dealsize = item.dealSize;
                            newItem.openedDate = new Date(item.openedDate).toLocaleDateString();
                            newItem.statusValue = item.opportunityState;
                            newItem.status = oppStatusClassName[item.opportunityState];
                            itemslist.push(newItem);
                        }
                    }

                    let filteredItems = itemslist.filter(itm => itm.status.toLowerCase() === "archived");


                    this.setState({
                        items: itemslist,
                        itemsOriginal: itemslist
                    });

                    resolve(true);
                })
                .catch(err => {
                    this.errorHandler(err, "Administration_getOpportunityIndex");
                    this.setState({
                        loading: false,
                        items: [],
                        itemsOriginal: []
                    });
                    reject(err);
                });
        });
    }

    getUserRoles() {
        // call to API fetch data
        return new Promise((resolve, reject) => {
            let requestUrl = 'api/RoleMapping';
            fetch(requestUrl, {
                method: "GET",
                headers: { 'authorization': 'Bearer ' + this.authHelper.getWebApiToken() }
            })
                .then(response => response.json())
                .then(data => {
                    try {
                        let userRoleList = [];
                        //console.log(data);
                        for (let i = 0; i < data.length; i++) {
                            let userRole = {};
                            userRole.id = data[i].id;
                            userRole.adGroupName = data[i].adGroupName;
                            userRole.roleName = data[i].roleName;
                            userRole.processStep = data[i].processStep;
                            userRole.channel = data[i].channel;
                            userRole.adGroupId = data[i].adGroupId;
                            userRole.processType = data[i].processType;

                            userRoleList.push(userRole);
                        }
                        this.setState({ userRoleList: userRoleList });
                        console.log("Administration_getUserRoles userRoleList lenght: " + userRoleList.length);
                        resolve(true);
                    }
                    catch (err) {
                        reject(err);
                    }

                });
        });
    }

    updateOpportunity(opportunity) {
        return new Promise((resolve, reject) => {
            let requestUrl = 'api/opportunity';

            var options = {
                method: "PATCH",
                headers: {
                    'Accept': 'application/json',
                    'Content-Type': 'application/json',
                    'authorization': 'Bearer    ' + this.authHelper.getWebApiToken()
                },
                body: JSON.stringify(opportunity)
            };

            fetch(requestUrl, options)
                .then(response => this.fetchResponseHandler(response, "Administration_updateOpportunity_fetch"))
                .then(data => {
                    resolve(data);
                })
                .catch(err => {
                    this.errorHandler(err, "Administration_updateOpportunity");
                    reject(err);
                });
        });

    }

    chngeOpportunityState(id) {
        console.log("Administration_chngeOpportunityState timer for: " + id);
    }


    showMessageBar(text, messageBarType) {
        this.setState({
            result: {
                type: messageBarType,
                text: text
            }
        });

    }

    hideMessageBar() {
        this.setState({
            result: null
        });
    }


    //Event handlers

    // Filter by Templatename
    _onFilterByOpportunityName(text) {
        const items = this.state.itemsOriginal.filter(item => item.status.toLowerCase() === "archived");

        this.setState({
            filterOpportunityName: text,
            items: text ?
                items.filter(item => item.opportunity.toString().toLowerCase().indexOf(text.toString().toLowerCase()) > -1)
                :
                items
        });
    }

    getSelectionDetails() {
        const selectionCount = this.selection.getSelectedCount();
        switch (selectionCount) {
            case 0:
                return 'No items selected';
            case 1:
                return '1 item selected: ' + this.selection.getSelection()[0].name;
            default:
                return `${selectionCount} items selected`;
        }
    }

    _selection = new Selection({
        onSelectionChanged: () => this.setState({ selectionDetails: this._getSelectionDetails() })
    });

    _getSelectionDetails() {
        const selectionCount = this._selection.getSelectedCount();
        return selectionCount;

    }

    unArchiveOpportunity(items) {
        console.log(items);
    }

    render() {
        //const items = this.state.items;
        let showActionButton = this._selection.getSelection().length > 0 ? true : false;

        const { columns, isCompactMode, items, selectionDetails } = this.state;


        return (
            <div className='ms-Grid bg-white ibox-content'>

                <div className='ms-Grid-row p-10'>
                    <div className='ms-Grid-col ms-sm6 ms-md6 ms-lg9'>
                        <DefaultButton iconProps={{ iconName: 'Undo' }} className={showActionButton ? "" : "hide"} onClick={this.unArchiveOpportunity(this._selection.getSelection())}><Trans>unarchive</Trans></DefaultButton>
                    </div>
                    <div className='ms-Grid-col ms-sm6 ms-md6 ms-lg3'>
                        <I18n>
                            {
                                t => <SearchBox
                                    placeholder={t('search')}
                                    onChange={this._onFilterByOpportunityName}
                                />
                            }
                        </I18n>
                    </div>
                </div>
                <div className='ms-Grid-row p-10'>



                    {
                        this.state.result &&
                        <MessageBar
                            messageBarType={this.state.result.type}
                            onDismiss={this.hideMessageBar.bind(this)}
                            isMultiline={false}
                            dismissButtonAriaLabel='Close'
                        >
                            {this.state.result.text}
                        </MessageBar>
                    }

                    {

                        this.state.loading ?
                            <div>
                                <br /><br /><br />
                                <Spinner size={SpinnerSize.medium} label={<Trans>loadingOpportunities</Trans>} ariaLive='assertive' />
                            </div>
                            :
                            items.length > 0 ?


                                <MarqueeSelection selection={this._selection}>
                                    <DetailsList
                                        items={this.state.items}
                                        compact={isCompactMode}
                                        columns={columns}
                                        setKey='key'
                                        enterModalSelectionOnTouch='false'
                                        layoutMode={DetailsListLayoutMode.fixedColumns}
                                        selection={this._selection}
                                        selectionPreservedOnEmptyClick={true}
                                        ariaLabelForSelectionColumn="Toggle selection"
                                        ariaLabelForSelectAllCheckbox="Toggle selection for all items"
                                    />
                                </MarqueeSelection>
                                :
                                <div><Trans>noOpportunitiesWithStatusArchived</Trans></div>

                    }


                    <div className='ms-grid-row'>
                        <div className='ms-Grid-col ms-sm6 ms-md6 ms-lg6 pl0'><br />

                        </div>
                        <div className='ms-Grid-col ms-sm12 ms-md6 ms-lg6'><br />
                            {
                                this.state.refreshing ?
                                    <div className='ms-Grid-col ms-sm12 ms-md3 ms-lg6 pull-right'>
                                        <Spinner size={SpinnerSize.small} label={<Trans>loadingOpportunities</Trans>} ariaLive='assertive' />
                                    </div>
                                    :
                                    <br />
                            }
                            <br /><br /><br />
                        </div>
                    </div>
                </div>
            </div>
        );
    }

}