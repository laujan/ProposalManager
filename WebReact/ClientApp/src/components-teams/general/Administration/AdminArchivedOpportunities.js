/* 
*  Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. 
*  See LICENSE in the source repository root for complete license information. 
*/
import React, { Component } from 'react';

import { Spinner, SpinnerSize } from 'office-ui-fabric-react/lib/Spinner';
import { MessageBar } from 'office-ui-fabric-react/lib/MessageBar';
import { DefaultButton } from 'office-ui-fabric-react/lib/Button';
import { SearchBox } from 'office-ui-fabric-react/lib/SearchBox';

import '../../teams.css';
import Utils from '../../../helpers/Utils';
import { I18n, Trans } from "react-i18next";
import { oppStatusText, oppStatusClassName } from '../../../common';
import { DetailsList, DetailsListLayoutMode, Selection } from 'office-ui-fabric-react/lib/DetailsList';
import { MarqueeSelection } from 'office-ui-fabric-react/lib/MarqueeSelection';

export class AdminArchivedOpportunities extends Component {
    displayName = AdminArchivedOpportunities.name

    constructor(props) {
        super(props);

        this.apiService = this.props.apiService;
        this.authHelper = window.authHelper;
        this.utils = new Utils();

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
                maxWidth: 150,
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
                maxWidth: 150,
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
                maxWidth: 150,
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

        //TODO Below line commented to show loading of data

        this.state = {
            loading: true,
            refreshing: false,
            items: [],
            itemsOriginal: [],
            userRoleList: [],
            channelCounter: 0,
            isCompactMode: false,
            columns: columns,
            filterOpportunityName: ""
        };

        this.unArchiveOpportunity = this.unArchiveOpportunity.bind(this);
        this._onFilterByOpportunityName = this._onFilterByOpportunityName.bind(this);
    }

    componentDidMount() {
        console.log("AdminArchivedOpportunites_componentDidMount");
        this.getData();
    }

    fetchResponseHandler(response, referenceCall) {
        if (response.status === 401) {
            //TODO: Handle refresh token in vNext;
        }
    }

    errorHandler(err, referenceCall) {
        console.log("Administration Ref: " + referenceCall + " error: " + JSON.stringify(err));
    }

    getData() {
        // To get the List of Opportunities to Display on Dashboard page
        let itemslist = this.props.items;
        let filteredItems = itemslist.filter(item => item.status.toLowerCase() === "archived");

        this.setState({
            items: filteredItems,
            itemsOriginal: itemslist,
            loading: false,
            haveGranularAccess: true,
            userRoleList: this.props.userRoleList
        });
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

        const { columns, isCompactMode, items } = this.state;

        return (
            <div className='ms-Grid'>

                <div className='ms-Grid-row p-10'>
                    <div className='ms-Grid-col ms-sm6 ms-md6 ms-lg9'>
                        <DefaultButton iconProps={{ iconName: 'Undo' }} className={showActionButton ? "" : "hide"} onClick={this.unArchiveOpportunity(this._selection.getSelection())}><Trans>unarchive</Trans></DefaultButton>
                    </div>
                    <div className='ms-Grid-col ms-sm6 ms-md6 ms-lg3'>
                        <I18n>
                            {
                                t => {
                                    return (
                                        <SearchBox
                                            placeholder={t('search')}
                                            onChange={this._onFilterByOpportunityName}
                                        />
                                    );
                                }
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
                                        selectionPreservedOnEmptyClick='true'
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