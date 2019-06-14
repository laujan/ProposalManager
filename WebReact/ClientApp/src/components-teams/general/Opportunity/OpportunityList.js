/*
*  Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license.
*  See LICENSE in the source repository root for complete license information.
*/

/* eslint-disable radix */

import React, { Component } from 'react';
import { Link } from 'react-router-dom';
import { PrimaryButton, IconButton } from 'office-ui-fabric-react/lib/Button';
import { SearchBox } from 'office-ui-fabric-react/lib/SearchBox';
import { MessageBar, MessageBarType } from 'office-ui-fabric-react/lib/MessageBar';
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
        this.apiService = this.props.apiService;
        this.logService = this.props.logService;
        this.authHelper = window.authHelper;
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
                maxWidth: 150,
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
                maxWidth: 150,
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
                className: 'docs-TextFieldExample ms-Grid-col ms-sm12 ms-md12 ms-lg3',
                fieldName: 'staus',
                minWidth: 150,
                maxWidth: 150,
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

        const actionColumn = [{
            key: 'column6',
            name: <Trans>action</Trans>,
            headerClassName: 'ms-List-th delectBTNwidth',
            className: 'DetailsListExample-cell--FileIcon actioniconAlign ',
            minWidth: 50,
            maxWidth: 50,
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
        },
        {
            key: 'column7',
            name: "",
            headerClassName: 'ms-List-th',
            className: 'docs-TextFieldExample ms-Grid-col ms-sm12 ms-md12 ms-lg3',
            minWidth: 30,
            maxWidth: 30,
            onRender: (item) => {
                return (
                    <div />
                );
            }
        }
        ];
        this.checkReadWrite = ["Administrator", "Opportunities_ReadWrite_All", "Opportunity_ReadWrite_All", "Opportunity_ReadWrite_Partial"];
        this.checkCreate = ["Opportunity_Create"];
        this.actionColumn = actionColumn;

        this.state = {
            filterClient: '',
            filterDeal: '',
            items: dashboardList,
            itemsOriginal: dashboardList,
            loading: true,
            messageBarEnabled: false,
            messageBarText: "",
            columns: columns,
            isCompactMode: false,
            isDelteOpp: false,
            messageDeleteOpp: "",
            messageBarTypeDeleteOpp: "",
            haveGranularAccess: false
        };

        this._onFilterByNameChanged = this._onFilterByNameChanged.bind(this);
        this._onFilterByDealChanged = this._onFilterByDealChanged.bind(this);
    }


    //Granular Access start:
    //Oppportunity create access
    async componentDidMount() {
        this.logService.log("OpportunityList componentDidMount: enter");
        let haveGranularAccess, canReadWrite = false;

        try
        {
            haveGranularAccess = await this.authHelper.callCheckAccess(this.checkCreate);
        }
        catch (e)
        {
            this.logService.log("OpportunityList checkCreate: error: ", e);
            haveGranularAccess = false;
        }

        try {
            canReadWrite = await this.authHelper.callCheckAccess(this.checkReadWrite);
        }
        catch (e)
        {
            this.logService.log("OpportunityList checkReadWrite: error: ", e);
            canReadWrite = false;
        }

        let columns = canReadWrite ? this.state.columns.concat(this.actionColumn) : [];
        this.setState({ haveGranularAccess: haveGranularAccess, columns: columns });
    }
    //Granular Access end:

    delay(ms) {
        return new Promise(resolve => setTimeout(resolve, ms));
    }

    async hideMessagebar() {
        await this.delay(2000);
        this.setState({ isDelteOpp: false, messageDeleteOpp: "", messageBarTypeDeleteOpp: "" });
    }

    async deleteRow(item) {
        try {
            this.setState({ isDelteOpp: true, messageDeleteOpp: " Deleting Opportunity - " + item.opportunity, messageBarTypeDeleteOpp: MessageBarType.info });

            let response = await this.apiService.callApi('Opportunity', 'DELETE', { id: item.id });
            if (response) {
                if (response.ok) {
                    let currentItems = this.state.items.filter(x => x.id !== item.id);
                    this.setState({
                        messageDeleteOpp: "Deleted opportunity " + item.opportunity,
                        messageBarTypeDeleteOpp: MessageBarType.success,
                        items: currentItems
                    });
                } else
                    throw new Error("Parsing reposne error.");
            } else
                throw new Error("Server throwed error on deleting");
        } catch (error) {
            this.setState({
                messageDeleteOpp: "Error " + error,
                messageBarTypeDeleteOpp: MessageBarType.error
            });
            this.logService.log("Setup_ConfigureAppIDAndGroupID error : ", error);
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
                items.filter(item => parseInt(item.dealsize) >= parseInt(value))
                : items
        });
    }

    _onRenderCell(item, index) {
        this.logService.log("General_getOpportunityIndex item : ", item);
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

    render() {
        this.logService.log("OpportunityList render: enter" + this.state.haveGranularAccess);
        const { columns, isCompactMode, items, haveGranularAccess } = this.state;

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
                    {
                        haveGranularAccess
                            ? <div className='ms-Grid-col ms-sm6 ms-md6 ms-lg6 createButton pt15 '>
                                {
                                    <PrimaryButton className='pull-right' onClick={this.props.onClickCreateOpp}> <i className="ms-Icon ms-Icon--Add pr10" aria-hidden="true" /><Trans>createNew</Trans></PrimaryButton>
                                }

                            </div>
                            : ""
                    }
                </div>
                <div className='ms-Grid'>
                    <div className='ms-Grid-row ms-SearchBoxSmallExample'>
                        <div className='ms-Grid-col ms-sm4 ms-md4 ms-lg3 pl0'>
                            <span><Trans>clientName</Trans></span>
                            <I18n>
                                {
                                    t => {
                                        return (
                                            <SearchBox
                                                placeholder={t('search')}
                                                onChange={this._onFilterByNameChanged}
                                            />
                                        );
                                    }

                                }
                            </I18n>
                        </div>
                        <div className='ms-Grid-col ms-sm4 ms-md4 ms-lg3'>
                            <span><Trans>dealSize</Trans></span>
                            <I18n>
                                {
                                    t => {
                                        return (
                                            <SearchBox
                                                placeholder={t('search')}
                                                onChange={this._onFilterByDealChanged}
                                            />
                                        );
                                    }
                                }
                            </I18n>
                        </div>
                    </div><br />
                    <div className='ms-Grid-row'>
                        <div className='ms-Grid-col ms-sm12 ms-md12 ms-lg12'>
                            {
                                this.state.isDelteOpp ?
                                    <MessageBar messageBarType={this.state.MessageBarTypeDeleteOpp} isMultiline={false}>
                                        {this.state.messageDeleteOpp}
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