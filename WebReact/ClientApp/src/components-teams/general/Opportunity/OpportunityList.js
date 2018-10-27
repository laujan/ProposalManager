/*
*  Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license.
*  See LICENSE in the source repository root for complete license information.
*/

import React, { Component } from 'react';
import { Link } from 'react-router-dom';
import { PrimaryButton, IconButton } from 'office-ui-fabric-react/lib/Button';
import { SearchBox } from 'office-ui-fabric-react/lib/SearchBox';
import { MessageBar, MessageBarType } from 'office-ui-fabric-react/lib/MessageBar';
import { FocusZone, FocusZoneDirection } from 'office-ui-fabric-react/lib/FocusZone';
import { List } from 'office-ui-fabric-react/lib/List';
import { oppStatusText, oppStatusClassName } from '../../../common';
import '../../../Style.css';
import { DetailsList, DetailsListLayoutMode, SelectionMode } from 'office-ui-fabric-react/lib/DetailsList';
import { TooltipHost } from 'office-ui-fabric-react/lib/Tooltip';
import { I18n, Trans } from "react-i18next";
import i18n from '../../../i18n';

export class OpportunityList extends Component {
    displayName = OpportunityList.name

    constructor(props) {
        super(props);
        this.sdkHelper = window.sdkHelper;
        this.authHelper = window.authHelper;

        //const userProfile = this.props.userProfile;
        const dashboardList = this.props.dashboardList;

        let columns = [
            {
                key: 'column1',
                name: <Trans>opportunity</Trans>,
                headerClassName: 'ms-List-th',
                className: 'docs-TextFieldExample ms-Grid-col ms-sm12 ms-md12 ms-lg3',
                fieldName: 'name',
                minWidth: 150,
                maxWidth: 350,
                isRowHeader: true,
                isResizable: true,
                onColumnClick: this.onColumnClick,
                onRender: (item) => {
                    return (
                        <div className='ms-List-itemName'>
                            <Link to={'./OpportunityDetails?opportunityId=' + item.id} >
                                {item.opportunity}
                            </Link>
                        </div>
                    );
                }
            },
            {
                key: 'column2',
                name: <Trans>client</Trans>,
                headerClassName: 'ms-List-th',
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
                name: <Trans>dealSize</Trans>,
                headerClassName: 'ms-List-th',
                className: 'docs-TextFieldExample ms-Grid-col ms-sm12 ms-md12 ms-lg3 clientcolum',
                fieldName: 'client',
                minWidth: 100,
                maxWidth: 250,
                isRowHeader: true,
                isResizable: true,
                onColumnClick: this.onColumnClick,
                onRender: (item) => {
                    return (
                        <div className='ms-List-itemClient'>{item.dealsize}</div>
                    );
                },
                isPadded: true
            },
            {
                key: 'column4',
                name: <Trans>openedDate</Trans>,
                headerClassName: 'ms-List-th',
                className: 'docs-TextFieldExample ms-Grid-col ms-sm12 ms-md12 ms-lg3',
                fieldName: 'openedDate',
                minWidth: 150,
                maxWidth: 350,
                isRowHeader: true,
                isResizable: true,
                onColumnClick: this.onColumnClick,
                onRender: (item) => {
                    return (
                        <div className='ms-List-itemDate AdminDate'>{new Date(item.openedDate).toLocaleDateString(i18n.language)}</div>
                    );
                },
                isPadded: true
            },
            {
                key: 'column5',
                name: <Trans>status</Trans>,
                headerClassName: 'ms-List-th',
                className: 'docs-TextFieldExample ms-Grid-col ms-sm12 ms-md12 ms-lg2',
                fieldName: 'staus',
                minWidth: 150,
                maxWidth: 350,
                isRowHeader: true,
                isResizable: true,
                onColumnClick: this.onColumnClick,
                onRender: (item) => {
                    return (
                        <div className={oppStatusClassName[item.stausValue].toLowerCase()}><Trans>{oppStatusText[item.stausValue]}</Trans></div>
                    );
                },
                isPadded: true
            }
        ];

        const actionColumn = {
            key: 'column6',
            name: <Trans>action</Trans>,
            headerClassName: 'ms-List-th delectBTNwidth',
            className: 'DetailsListExample-cell--FileIcon actioniconAlign ',
            minWidth: 30,
            maxWidth: 30,
            onColumnClick: this.onColumnClick,
            onRender: (item) => {
                return (
                    <div className='OpportunityDelete'>
                        <TooltipHost content={<Trans>delete</Trans>} calloutProps={{ gapSpace: 0 }} closeDelay={200}>
                            <IconButton iconProps={{ iconName: 'Delete' }} onClick={e => this.deleteRow(item)} />
                        </TooltipHost>
                    </div>
                );
            }
        };
        this.checkReadWrite = ["Opportunities_ReadWrite_All", "Opportunity_ReadWrite_All", "Opportunity_ReadWrite_Partial"];
        // if (this.props.userProfile.roles.filter(x => x.displayName === "RelationshipManager").length > 0) {
        //     columns.push(actionColumn);
        // }
        this.actionColumn = actionColumn;

        this.state = {
            filterClient: '',
            filterDeal: '',
            items: dashboardList,
            itemsOriginal: dashboardList,
            loading: true,
            reverseList: false, //Seems there are issues with Reverse function on arrays
            authUserId: this.props.userProfile.id,
            authUserDisplayName: this.props.userProfile.displayName,
            authUserMail: this.props.userProfile.mail,
            authUserPhone: this.props.userProfile.phone,
            authUserPicture: this.props.userProfile.picture,
            authUserUPN: this.props.userProfile.userPrincipalName,
            authUserRoles: this.props.userProfile.roles,
            authUserPermissions: this.props.userProfile.permissions,
            messageBarEnabled: false,
            messageBarText: "",
            MessagebarTextOpp: "",
            MessagebarTexCust: "",
            MessagebarTexDealSize: "",
            loadSpinner: true,
            columns: columns,
            isCompactMode: false,
            isDelteOpp: false,
            MessageDeleteOpp: "",
            MessageBarTypeDeleteOpp: "",
            haveGranularAccess: false
        };

        this._onFilterByNameChanged = this._onFilterByNameChanged.bind(this);
        this._onFilterByDealChanged = this._onFilterByDealChanged.bind(this);
    }

    fetchResponseHandler(response, referenceCall) {
        if (response.status === 401) {
            // TODO: This has been deprecated with the new token refresh functionality leaving the code for future expansion
        }
    }

    errorHandler(err, referenceCall) {
        console.log("Dashboard Ref: " + referenceCall + " error: " + JSON.stringify(err));
    }

    

    opportunitiesListHeading() {
        return (
            <div className='ms-List-th'>
                <div className='ms-List-th-itemName'>Opportunity</div>
                <div className='ms-List-th-itemClient'>Client</div>
                <div className='ms-List-th-itemDealsize'>Deal Size</div>
                <div className='ms-List-th-itemDate'>Opened Date</div>
                <div className='ms-List-th-itemState'>Status</div>
            </div>
        );
    }

    delay(ms) {
        return new Promise(resolve => setTimeout(resolve, ms));
    }

    async hideMessagebar() {
        await this.delay(2000);
        this.setState({ isDelteOpp: false, MessageDeleteOpp: "",MessageBarTypeDeleteOpp: "" });
    }

    async deleteRow(item) {
        try {
            let fetchData = {
                method: 'delete',
                //body: JSON.stringify(item.id),
                headers: {
                    'authorization': 'Bearer ' + this.authHelper.getWebApiToken()
                }
            };
            this.requestUrl = 'api/opportunity/' + item.id;
            this.setState({ isDelteOpp: true, MessageDeleteOpp: " Deleting Opportunity - " + item.opportunity, MessageBarTypeDeleteOpp: MessageBarType.info });
            let response  = await fetch(this.requestUrl, fetchData);
            if(response){
                if(response.ok){
                    let currentItems = this.state.items.filter(x => x.id !== item.id);
                    this.setState({ 
                        MessageDeleteOpp: "Deleted opportunity " + item.opportunity, 
                        MessageBarTypeDeleteOpp: MessageBarType.success,
                        items: currentItems 
                    });
                }else
                    throw new Error("Parsing reposne error.")
            }else
                throw new Error("Server throwed error on deleting")
        } catch (error) {
            this.setState({ 
                MessageDeleteOpp: "Error " + error, 
                MessageBarTypeDeleteOpp: MessageBarType.error
            });
            console.log("Setup_ConfigureAppIDAndGroupID error : ", error);
        }
        await this.hideMessagebar();
    }

    _onFilterByNameChanged(text) {
        const items = this.state.itemsOriginal;

        this.setState({
            filterClient: text,
            items: text ?
                items.filter(item => item.client.toString().toLowerCase().indexOf(text.toString().toLowerCase()) > -1) :
                items
        });
    }

    _onFilterByDealChanged(value) {
        const items = this.state.itemsOriginal;

        this.setState({
            filterDeal: value,
            items: value ?
                items.filter(item => item.dealsize >= value) :
                items
        });
    }

    _onRenderCell(item, index) {


        return (
            <div className='ms-List-itemCell' data-is-focusable='true'>
                <div className='ms-List-itemContent'>
                    <div className='ms-List-itemName'>
                        <Link to={'/OpportunityDetails?opportunityId=' + item.id} >
                            {item.opportunity}
                        </Link>
                    </div>
                    <div className='ms-List-itemClient'>{item.client}</div>
                    <div className='ms-List-itemDealsize'>{item.dealsize}</div>
                    <div className='ms-List-itemDate'>{item.openedDate}</div>
                    <div className={"ms-List-itemState " + oppStatusClassName[item.stausValue].toLowerCase()}>{oppStatusText[item.stausValue]}</div>
                    <div className="OpportunityDelete ">
                        <IconButton iconProps={{ iconName: 'Delete' }} onClick={e => this.deleteRow(item)} />
                    </div>
                </div>
            </div>
        );
    }

    opportunitiesList(itemsList, itemsListOriginal) {
        //const lenght = typeof itemsList !== 'undefined' ? itemsList.length : 0;
        //const lenghtOriginal = typeof itemsListOriginal !== 'undefined' ? itemsListOriginal.length : 0;
        //const originalItems = itemsListOriginal;
        const items = itemsList;
        //const resultCountText = lenght === lenghtOriginal ? '' : ` (${items.length} of ${originalItems.length} shown)`;

        return (
            <FocusZone direction={FocusZoneDirection.vertical}>
                <List
                    items={items}
                    onRenderCell={this._onRenderCell}
                    className='ms-List'
                />
            </FocusZone>
        );
    }

    //Granular Access start:
    //Oppportunity create access
    componentWillMount() {
        //this.getOpportunityIndex();
        this.authHelper.callCheckAccess(["Opportunity_Create"]).then((data) => {
            console.log("Granular Dashboard: ", data);
            let haveGranularAccess = data;
            this.setState({ haveGranularAccess });
        });

        this.authHelper.callCheckAccess(this.checkReadWrite).then((data) => {
            console.log("Granular Dashboard: ", data);
            if(data){
                let columns = this.state.columns;
                columns.push(this.actionColumn);
                this.setState({columns})
            }
            
        });
    }
    //Granular Access end:

    render() {
        const { columns, isCompactMode, items } = this.state;

        const isLoading = this.state.loading;

        let isRelationshipManager = false;
        if ((this.state.authUserRoles.filter(x => x.displayName === "RelationshipManager")).length > 0) {
            isRelationshipManager = true;
        }
        let userPermissions = this.state.authUserPermissions;


        const itemsOriginal = this.state.itemsOriginal;
        //const items = this.state.items;

        const lenghtOriginal = typeof itemsOriginal !== 'undefined' ? itemsOriginal.length : 0;
        const listHasItems = lenghtOriginal > 0 ? true : false;

        const opportunitiesListHeading = this.opportunitiesListHeading();
        const opportunitiesListComponent = this.opportunitiesList(items, itemsOriginal);

        return (
            <div className='ms-Grid pr18'>
                {
                    this.state.messageBarEnabled ?
                        <div className='ms-Grid-col ms-sm12 ms-md12 ms-lg12'>
                            <MessageBar messageBarType={this.props.context.messageBarType} isMultiline={false}>
                                {this.props.context.messageBarText}
                            </MessageBar>
                        </div>
                        : ""
                }


                <div className='ms-Grid-row'>
                    <div className='ms-Grid-col ms-sm6 ms-md6 ms-lg6 pageheading'>
                        &nbsp;&nbsp;
                    </div>
                    {	//Granular access start
                        this.state.haveGranularAccess
                            ? <div className='ms-Grid-col ms-sm6 ms-md6 ms-lg6 createButton pt15 '>
                                {
                                    <PrimaryButton className='pull-right' onClick={this.props.onClickCreateOpp}> <i className="ms-Icon ms-Icon--Add pr10" aria-hidden="true"></i><Trans>createNew</Trans></PrimaryButton>
                                }

                            </div>
                            : ""
                        //Granular access end
                    }

                </div>
                <div className='ms-Grid'>
                    <div className='ms-Grid-row ms-SearchBoxSmallExample'>
                        <div className='ms-Grid-col ms-sm4 ms-md4 ms-lg3 pl0'>
                            <span><Trans>clientName</Trans></span>
                            <I18n>
                                {
                                    t => <SearchBox
                                        placeholder={t('search')}
                                        onChange={this._onFilterByNameChanged}
                                    />

                                }
                            </I18n>
                        </div>
                        <div className='ms-Grid-col ms-sm4 ms-md4 ms-lg3'>
                            <span><Trans>dealSize</Trans></span>
                            <I18n>
                                {
                                    t => <SearchBox
                                        placeholder={t('search')}
                                        onChange={this._onFilterByDealChanged}
                                    />
                                }
                            </I18n>
                        </div>
                    </div><br />
                    <div className='ms-Grid-row'>
                        <div className='ms-Grid-col ms-sm12 ms-md12 ms-lg12'>
                            {
                                this.state.isDelteOpp ?
                                    <MessageBar messageBarType={this.state.MessageBarTypeDeleteOpp} isMultiline={false}>
                                        {this.state.MessageDeleteOpp}
                                    </MessageBar>
                                    : ""
                            }
                        </div>
                    </div>
                    <div className='ms-Grid-row'>
                        <div className='ms-Grid-col ms-sm12 ms-md12 ms-lg12'>
                        {
                            items.length > 0
                                ?
                                <DetailsList
                                    items={items}
                                    compact={isCompactMode}
                                    columns={columns}
                                    selectionMode={SelectionMode.none}
                                    setKey='key'
                                    layoutMode={DetailsListLayoutMode.justified}
                                    enterModalSelectionOnTouch='false'
                                />
                                :
                                <div><Trans>noOpportunities</Trans></div>
                            }
                        </div>

                    </div>
                    <br /><br />
                </div>
            </div>
        );
    }

}