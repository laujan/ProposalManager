/*
*  Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license.
*  See LICENSE in the source repository root for complete license information.
*/

import React, { Component } from 'react';
import { oppStatusText, oppStatusClassName } from '../../common';
import { IconButton } from 'office-ui-fabric-react/lib/Button';
import { DetailsList, DetailsListLayoutMode, SelectionMode } from 'office-ui-fabric-react/lib/DetailsList';
import {
    TooltipHost
} from 'office-ui-fabric-react/lib/Tooltip';
import { Trans } from "react-i18next";
import i18n from '../../i18n';



export class OpportunityListCompact extends Component {
    displayName = OpportunityListCompact.name

    constructor(props) {
        super(props);

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
                        <div className='ms-List-itemDate AdminDate'>{new Date(item.openedDate).toLocaleDateString(i18n.language)}</div>
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
            },
            {
                key: 'column5',
                name: <Trans>action</Trans>,
                headerClassName: 'DetailsListExample-header--FileIcon actioniconAlign',
                className: 'DetailsListExample-cell--FileIcon actioniconAlign',
                //iconClassName: 'DetailsListExample-Header-FileTypeIcon',
                // iconName: 'Page',
                //isIconOnly: true,
                //fieldName: 'name',
                minWidth: 30,
                maxWidth: 30,
                onColumnClick: this.onColumnClick,
                onRender: (item) => {
                    return (
                        <div onMouseEnter={e => this.mouseEnter(item.createTeamDisable)}
                            onMouseLeave={e => this.mouseLeave(item.createTeamDisable)}>
                            <TooltipHost content={<Trans>addAddin</Trans>} calloutProps={{ gapSpace: 0 }} closeDelay={500}>
                                <IconButton iconProps={{ iconName: 'PeopleAdd' }} onClick={e => this.onActionItemClick(item)} disabled={item.createTeamDisable} />
                            </TooltipHost>
                        </div>
                    );
                }
            }
        ];

        let rowCounter = 1;
        if (this.props.opportunityIndex.length > 0) {
            rowCounter = this.props.opportunityIndex.length + 1;
        }

        this.state = {
            items: this.props.opportunityIndex,
            rowItemCounter: rowCounter,
            columns: columns,
            isCompactMode: false
        };
    }

    componentWillMount() {

    }


    // Class methods
    onActionItemClick(item) {
        this.props.onActionItemClick(item);
    }

    mouseEnter(flag) {
        this.props.mouseEnter(flag);
    }

    mouseLeave(flag) {
        this.props.mouseLeave(flag);
    }


    onColumnClick = (ev, column) => {
        const { columns, items } = this.state;

        let newItems = items.slice();
        const newColumns = columns.slice();
        const currColumn = newColumns.filter((currCol, idx) => {
            return column.key === currCol.key;
        })[0];

        newColumns.forEach((newCol) => {
            if (newCol === currColumn) {
                currColumn.isSortedDescending = !currColumn.isSortedDescending;
                currColumn.isSorted = true;
            } else {
                newCol.isSorted = false;
                newCol.isSortedDescending = true;
            }
        });

        newItems = this.sortItems(newItems, currColumn.fieldName, currColumn.isSortedDescending);

        this.setState({
            columns: newColumns,
            items: newItems
        });
    }

    sortItems = (items, sortBy, descending = false) => {
        if (descending) {
            return items.sort((a, b) => {
                if (a[sortBy] < b[sortBy]) {
                    return 1;
                }
                if (a[sortBy] > b[sortBy]) {
                    return -1;
                }
                return 0;
            });
        } else {
            return items.sort((a, b) => {
                if (a[sortBy] < b[sortBy]) {
                    return -1;
                }
                if (a[sortBy] > b[sortBy]) {
                    return 1;
                }
                return 0;
            });
        }
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

    render() {
        const { columns, isCompactMode } = this.state;

        return (
            <div className='ms-Grid-row'>
                <div className='ms-Grid-col ms-sm12 ms-md12 ms-lg12'>
                    <DetailsList
                        items={this.props.opportunityIndex}
                        compact={isCompactMode}
                        columns={columns}
                        selectionMode={SelectionMode.none}
                        setKey='key'
                        layoutMode={DetailsListLayoutMode.justified}
                        enterModalSelectionOnTouch='false'
                    />
                </div>
            </div>
        );
    }
}