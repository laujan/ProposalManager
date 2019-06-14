/* 
*  Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. 
*  See LICENSE in the source repository root for complete license information. 
*/

import React, { Component } from 'react';
import { PrimaryButton, IconButton } from 'office-ui-fabric-react/lib/Button';
import { TextField } from 'office-ui-fabric-react/lib/TextField';
import { Dropdown } from 'office-ui-fabric-react/lib/Dropdown';
import { DetailsList, DetailsListLayoutMode, SelectionMode } from 'office-ui-fabric-react/lib/DetailsList';
import { Link } from 'office-ui-fabric-react/lib/Link';
import { FilePicker } from '../FilePicker';
import Utils from '../../../helpers/Utils';
import '../../../Style.css';
import { Trans } from "react-i18next";
import { DatePicker } from 'office-ui-fabric-react/lib/DatePicker';
import { DayPickerStrings } from '../../../common';


export class NewOpportunityDocuments extends Component {
    displayName = NewOpportunityDocuments.name

    constructor(props) {
        super(props);

        this.logService = this.props.logService;
        this.utils = new Utils();
        this.opportunity = this.props.opportunity;
        this.metaData = this.props.metaDataList.length > 0 ? this.props.metaDataList.filter(prop => prop.screen === "Screen2") : [];
        const columns = [
            {
                key: 'column1',
                name: <Trans>file</Trans>,
                headerClassName: 'ms-List-th browsebutton',
                className: 'docs-TextFieldExample ms-Grid-col ms-sm12 ms-md12 ms-lg4 browsebutton',
                fieldName: 'file',
                minWidth: 150,
                maxWidth: 250,
                isRowHeader: true,
                onRender: (item) => {
                    let itemFileUri = "";
                    return (
                        <FilePicker
                            id={'fp' + item.id}
                            fileUri={itemFileUri}
                            file={item.file}
                            showBrowse='true'
                            showLabel='true'
                            onChange={(e) => this.onChangeFile(e, item)}
                        />
                    );
                }
            },
            {
                key: 'column2',
                name: <Trans>notes</Trans>,
                headerClassName: 'ms-List-th',
                className: 'docs-TextFieldExample ms-Grid-col ms-sm12 ms-md12 ms-lg3',
                fieldName: 'notes',
                minWidth: 150,
                maxWidth: 550,
                isRowHeader: false,
                isResizable: true,
                isCollapsable: true,
                onRender: (item) => {
                    return (
                        <TextField
                            id={'txtNotes' + item.id}
                            value={item.note}
                            onBlur={(e) => this.onBlurNotes(e, item)}
                        />
                    );
                },
                isPadded: true
            },
            {
                key: 'column3',
                name: <Trans>category</Trans>,
                headerClassName: 'ms-List-th',
                className: 'docs-TextFieldExample ms-Grid-col ms-sm12 ms-md12 ms-lg2 categoryResponssive',
                fieldName: 'category',
                minWidth: 150,
                maxWidth: 550,
                isRowHeader: false,
                isResizable: true,
                onRender: (item) => {
                    return (
                        <div>
                            <Dropdown
                                id={'ddCat' + item.id}
                                ariaLabel='Category'
                                options={this.props.categories}
                                defaultSelectedKey={item.category.id}
                                onChanged={(e) => this.onChangeCategory(e, item)}
                            />
                        </div>
                    );
                },
                isPadded: true
            },
            {
                key: 'column4',
                name: <Trans>tags</Trans>,
                headerClassName: 'ms-List-th',
                className: 'docs-TextFieldExample ms-Grid-col ms-sm12 ms-md12 ms-lg2 tagsfield',
                fieldName: 'tags',
                minWidth: 150,
                maxWidth: 550,
                isRowHeader: false,
                isResizable: true,
                isCollapsable: true,
                onRender: (item) => {
                    return (
                        <TextField
                            id={'txtTags' + item.id}
                            value={item.tags}
                            onBlur={(e) => this.onBlurTags(e, item)}
                        />
                    );
                },
                isPadded: true
            },
            {
                key: 'column5',
                name: <Trans>action</Trans>,
                headerClassName: 'ms-List-th',
                className: 'DetailsListExample-cell--FileIcon',
                iconClassName: 'DetailsListExample-Header-FileTypeIcon',
                iconName: 'Page',
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

        let rowCounter = 1;
        if (this.opportunity.documentAttachments.length > 0) {
            rowCounter = this.opportunity.documentAttachments.length + 1;
        } else {
            let currentItems = this.opportunity.documentAttachments;
            currentItems.push(this.createListItem(rowCounter));
            this.opportunity.documentAttachments = currentItems;
        }

        this.state = {
            items: this.opportunity.documentAttachments,
            rowItemCounter: rowCounter,
            columns: columns,
            isCompactMode: false
        };
    }

    componentDidMount() {
        this.opportunity = this.props.opportunity;
    }

    // Class methods
    onAddRow() {
        let rowCounter = this.state.rowItemCounter + 1;
        let newItems = [];
        newItems.push(this.createListItem(rowCounter));

        let currentItems = newItems.concat(this.state.items);
        this.opportunity.documentAttachments = currentItems;

        this.setState({
            items: currentItems,
            rowItemCounter: rowCounter
        });
    }

    deleteRow(item) {
        let currentItems = this.state.items.filter(x => x.id !== item.id);

        this.opportunity.documentAttachments = currentItems;
        this.setState({
            items: currentItems
        });
    }

    createListItem(key) {
        return {
            key: key,
            id: this.utils.guid(),
            file: {},
            fileName: "",
            note: "",
            category: {
                id: "",
                displayName: ""
            },
            tags: "",
            documentUri: ""
        };
    }

    onChangeFile(e, item) {
        let updatedItems = this.state.items;
        let itemIdx = updatedItems.indexOf(item);
        updatedItems[itemIdx].file = e;
        updatedItems[itemIdx].fileName = e.name;
        this.opportunity.documentAttachments = updatedItems;
        this.setState({
            items: updatedItems
        });
    }

    onChangeCategory(e, item) {
        let updatedItems = this.state.items;
        let itemIdx = updatedItems.indexOf(item);
        updatedItems[itemIdx].category.id = e.key;
        updatedItems[itemIdx].category.name = e.text;
        this.opportunity.documentAttachments = updatedItems;
        this.setState({
            items: updatedItems
        });
    }

    onBlurNotes(e, item) {
        let updatedItems = this.state.items;
        let itemIdx = updatedItems.indexOf(item);
        updatedItems[itemIdx].note = e.target.value;
        this.opportunity.documentAttachments = updatedItems;
        this.setState({
            items: updatedItems
        });
    }

    onBlurTags(e, item) {
        let updatedItems = this.state.items;
        let itemIdx = updatedItems.indexOf(item);
        updatedItems[itemIdx].tags = e.target.value;
        this.opportunity.documentAttachments = updatedItems;
        this.setState({
            items: updatedItems
        });
    }

    // For DeatlsList
    documentsList(columns, isCompactMode, items) {
        return (
            <div className='ms-Grid-row ibox-content'>
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

    onBlurProperty(e, key) {
        if (e.target.value.length !== 0) {
            this.opportunity.metaDataFields.forEach(obj => {
                if (obj.id === key) {
                    this.logService.log("NewOpportunityDocuments_onBlurProperty : ", obj.id);
                    obj.values = e.target.value;
                }
            });
        }
    }

    _onSelectTargetDate = (date, key) => {
        this.opportunity.metaDataFields.forEach(obj => {
            if (obj.id === key) {
                this.logService.log("NewOpportunity_onChangeDropDown : ", obj.id, this._onFormatDate(date));
                obj.values = this._onFormatDate(date);
            }
        });
        this._checkNextEnabled();
    }

    _onFormatDate = (date) => {
        return (
            date.getMonth() + 1 +
            '/' +
            date.getDate() +
            '/' +
            date.getFullYear()
        );
    }

    _onParseDateFromString = (value) => {
        const date = this.state.value || new Date();
        const values = (value || '').trim().split('/');
        const day =
            values.length > 0
                ? Math.max(1, Math.min(31, parseInt(values[0], 10)))
                : date.getDate();
        const month =
            values.length > 1
                ? Math.max(1, Math.min(12, parseInt(values[1], 10))) - 1
                : date.getMonth();
        let year = values.length > 2 ? parseInt(values[2], 10) : date.getFullYear();
        if (year < 100) {
            year += date.getFullYear() - date.getFullYear() % 100;
        }
        return new Date(year, month, day);
    }

    _setItemDate(dt) {
        let lmDate = new Date(dt);
        if (lmDate.getFullYear() === 1 || lmDate.getFullYear() === 0) {
            return new Date();
        } else return new Date(dt);
    }

    onChangeDropDown(e, key) {
        if (e.text.length > 0) {
            this.opportunity.metaDataFields.forEach(obj => {
                if (obj.id === key) {
                    this.logService.log("NewOpportunity_onChangeDropDown : ", obj.id);
                    obj.values = e.text;
                }
            });
        }
    }

    _rendermetaData() {
        let metaDataComponents = null;
        if (this.metaData.length > 0) {
            this.metaData = this.metaData.filter(x => x.displayName !== "Category" && x.fieldType.name !== "DropDown");
            metaDataComponents = this.metaData.map((metaDataObj) => {
                let component = null;
                let id = metaDataObj.displayName.toLowerCase().replace(/\s/g, '');
                switch (metaDataObj.fieldType.name) {
                    case "Date":
                        let tardate = this.opportunity.metaDataFields.find(x => x.id === id).values;
                        component = (<div className='docs-TextFieldExample ms-Grid-col ms-sm12 ms-md12 ms-lg6' key={metaDataObj.id}>
                            <DatePicker strings={DayPickerStrings}
                                label={metaDataObj.displayName}
                                showWeekNumbers={false}
                                firstWeekOfYear={1}
                                showMonthPickerAsOverlay='true'
                                iconProps={{ iconName: 'Calendar' }}
                                value={tardate ? this._setItemDate(tardate) : this._setItemDate(Date.now())}
                                onSelectDate={(date) => this._onSelectTargetDate(date, id)}
                                formatDate={this._onFormatDate}
                                parseDateFromString={this._onParseDateFromString}
                                minDate={new Date()}
                                isRequired={true}
                            />
                        </div>);
                        break;

                    case "DropDown":
                        let placeHolder = `Select ${metaDataObj.displayName}`;
                        let dropvalue = this.opportunity.metaDataFields.find(x => x.id === id).values;
                        component = (<div className='docs-TextFieldExample ms-Grid-col ms-sm12 ms-md12 ms-lg6' key={metaDataObj.id}>
                            <Dropdown
                                placeHolder={placeHolder}
                                label={metaDataObj.displayName}
                                id={id}
                                ariaLabel={metaDataObj.displayName}
                                value={dropvalue}
                                options={metaDataObj.values.map(x => { return { 'key': x.id, 'text': x.name }; })}
                                defaultSelectedKey={metaDataObj.values.map(x => { if (x.name === dropvalue) return x.id; })}
                                componentRef=''
                                onChanged={(e) => this.onChangeDropDown(e, id)}
                            />
                        </div>);
                        break;

                    default:
                        let textvalue = this.opportunity.metaDataFields.find(x => x.id === id).values;
                        component = (<div className='docs-TextFieldExample ms-Grid-col ms-sm12 ms-md12 ms-lg6' key={metaDataObj.id}>
                            <TextField
                                id={id}
                                value={textvalue}
                                label={metaDataObj.displayName}
                                onBlur={(e) => this.onBlurProperty(e, id)}
                            />
                        </div>);
                        break;
                }
                return component;
            });
        }
        return metaDataComponents;
    }


    render() {
        const { columns, isCompactMode, items } = this.state;
        const documentsList = this.documentsList(columns, isCompactMode, items);

        return (
            <div className='ms-Grid'>
                <div className='ms-Grid-row'>
                    <div className='ms-Grid-col ms-sm12 ms-md12 ms-lg6 pageheading'>
                        <h3><Trans>addDocuments</Trans></h3>
                    </div>
                    <div className='ms-Grid-col ms-sm12 ms-md12 ms-lg6 pt15 pr18 '>
                        <h5><Link href='' className='pull-right' onClick={() => this.onAddRow()} >+ <Trans>addNew</Trans></Link></h5>
                    </div>
                </div>
                {documentsList}

                {this.metaData.length > 1 ?
                    <div className='ms-Grid-row'>
                        <div className='ms-Grid-col ms-sm12 ms-md12 ms-lg6 pageheading'>
                            <h3 className="pageheading"><Trans>opportunityProperties</Trans></h3>
                        </div>
                        <div className='ms-lg12 ibox-content pb20'>
                            {this._rendermetaData()}
                        </div>
                    </div>
                    : ""
                }

                <div className='ms-grid-row '>
                    <div className='ms-Grid-col ms-sm6 ms-md6 ms-lg6 pl0 pb20'><br />
                        <PrimaryButton className='backbutton pull-left' onClick={this.props.onClickBack}><Trans>back</Trans></PrimaryButton>
                    </div>
                    <div className='ms-Grid-col ms-sm6 ms-md6 ms-lg6 pb20'><br />
                        <PrimaryButton className='pull-right' onClick={this.props.onClickNext}><Trans>next</Trans></PrimaryButton>
                    </div>
                </div><br /><br />
            </div>
        );
    }
}