/*
*  Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license.
*  See LICENSE in the source repository root for complete license information.
*/

import React, { Component } from 'react';
import { TextField } from 'office-ui-fabric-react/lib/TextField';
import { IconButton, PrimaryButton } from 'office-ui-fabric-react/lib/Button';
import { DetailsList, DetailsListLayoutMode, SelectionMode } from 'office-ui-fabric-react/lib/DetailsList';
import { Link } from 'office-ui-fabric-react/lib/Link';
import {
    Spinner,
    SpinnerSize
} from 'office-ui-fabric-react/lib/Spinner';
import { MessageBar, MessageBarType } from 'office-ui-fabric-react/lib/MessageBar';
import { Trans } from "react-i18next";


export class ProcessTypesList extends Component {
    displayName = ProcessTypesList.name

    constructor(props) {
        super(props);

        this.sdkHelper = window.sdkHelper;
        this.authHelper = window.authHelper;

        const columns = [
            {
                key: 'column1',
                name: <Trans>ProcessType</Trans>,
                headerClassName: 'ms-List-th browsebutton',
                className: 'docs-TextFieldExample ms-Grid-col ms-sm12 ms-md12 ms-lg8',
                fieldName: 'processStep',
                minWidth: 150,
                maxWidth: 250,
                isRowHeader: true,
                onRender: (item) => {
                    return (
                        <TextField
                            id={'txtProcessType' + item.id}
                            value={item.processStep}
                            onBlur={(e) => this.onBlurProcessType(e, item, item.operation)}
                        />
                    );
                }
            },
            {
                key: 'column2',
                name: <Trans>action</Trans>,
                headerClassName: 'ms-List-th',
                className: 'ms-Grid-col ms-sm12 ms-md12 ms-lg4',
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
            isUpdate: false,
            updatedItems: [],
            MessagebarText: "",
            MessageBarType: MessageBarType.success,
            isUpdateMsg: false
        };
    }

    componentWillMount() {
        this.getProcessTypeList();
    }

    getProcessTypeList() {
        // call to API fetch Process
        let requestUrl = 'api/Process';
        fetch(requestUrl, {
            method: "GET",
            headers: { 'authorization': 'Bearer ' + this.authHelper.getWebApiToken() }
        })
            .then(response => response.json())
            .then(data => {
                try {
                    let allProcessTypes = data.itemsList;
                    let processTypes = allProcessTypes.filter(function (k) {
                        return k.processType.toLowerCase() !== "base" && k.processType.toLowerCase() !== "customerdecisiontab" && k.processType.toLowerCase() !== "proposalstatustab";
                    });
                    let processTypeList = [];
                    for (let i = 0; i < processTypes.length; i++) {
                        let processType = {};
                        //processType.id = processTypes[i].id;
                        //processType.processStep = processTypes[i].processStep;
                        //processType.channel = processTypes[i].channel;
                        //processType.processType = processTypes[i].processType;
                        //processType.operation = "update";
                        processTypes[i].operation = "update";
                        processTypeList.push(processTypes[i]);
                    }
                    this.setState({ items: processTypeList, loading: false, rowItemCounter: processTypeList.length });
                }
                catch (err) {
                    return false;
                }

            });
    }

    createRowItem(key) {
        return {
            id: key.toString(),
            processStep: "",
            channel: "",
            processType: "CheckListTab",
            operation: "add"
        };
    }

    onAddRow() {
        //let rowCounter = this.state.rowItemCounter+ 1;
        let rowCounter = parseInt(this.state.items[this.state.items.length - 1].id) + 1;
        let newItems = [];
        newItems.push(this.createRowItem(rowCounter));

        let currentItems = this.state.items.concat(newItems);

        this.setState({
            items: currentItems,
            rowItemCounter: rowCounter
        });
    }

    deleteRow(item) {
        this.setState({ isUpdate: true });

        //deleteProcessType
        this.deleteProcessType(item);
    }

    //ProcessType List - Details
    processTypeList(columns, isCompactMode, items, selectionDetails) {
        return (
            <div className='ms-Grid-row LsitBoxAlign p20ALL '>
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

    onBlurProcessType(e, item, operation) {
        //Check processType already exist
        this.setState({ isUpdate: true });

        let isProcessExist = this.state.items.some(obj => obj.processStep.toLowerCase() === e.target.value.toLowerCase());
        if (isProcessExist) {
            this.setState({
                MessagebarText: <Trans>processTypeAlreadyExist</Trans>,
                MessageBarType: MessageBarType.error,
                isUpdate: false,
                isUpdateMsg: true
            });
            setTimeout(function () { this.setState({  isUpdateMsg: false, MessageBarType: MessageBarType.error, MessagebarText: "" }); }.bind(this), 3000);
            return false;
        }

        delete item['operation'];
        let updatedItems = this.state.items;
        let itemIdx = updatedItems.indexOf(item);
        updatedItems[itemIdx].processStep = e.target.value;
        updatedItems[itemIdx].channel = e.target.value;
        this.setState({
            updatedItems: updatedItems
        });

        if (operation === "add") {
            this.addProcessType(updatedItems[itemIdx]);
        } else if (operation === "update") {
            this.updateProcessType(updatedItems[itemIdx]);
        }

    }

    addProcessType(processTypeItem) {
        // API Add call        
        this.requestUpdUrl = 'api/Process';
        let options = {
            method: "POST",
            headers: {
                'Accept': 'application/json',
                'Content-Type': 'application/json',
                'authorization': 'Bearer    ' + window.authHelper.getWebApiToken()
            },
            body: JSON.stringify(processTypeItem)
        };

        fetch(this.requestUpdUrl, options)
            .catch(error => console.error('Error:', error))
            .then(response => {
                if (response.ok) {
                    this.setState({
                        items: this.state.updatedItems,
                        MessagebarText: <Trans>processTypeAddSuccess</Trans>,
                        MessageBarType: MessageBarType.success,
                        isUpdate: false,
                        isUpdateMsg: true
                    });
                    setTimeout(function () { this.setState({ isUpdateMsg: false, MessageBarType: MessageBarType.success, MessagebarText: "" }); }.bind(this), 3000);
                    return response.json;
                } else {
                    this.setState({
                        MessagebarText: <Trans>errorOoccuredPleaseTryAgain</Trans>,
                        MessageBarType: MessageBarType.error,
                        isUpdate: false,
                        isUpdateMsg: true
                    });
                    setTimeout(function () { this.setState({ isUpdateMsg: false, MessageBarType: MessageBarType.error, MessagebarText: "" }); }.bind(this), 3000);
                }
            }).then(json => {
                //console.log(json);
                this.setState({ isUpdate: false });
            });


    }

    updateProcessType(processTypeItem) {
        // API Update call        
        this.requestUpdUrl = 'api/Process';
        let options = {
            method: "PATCH",
            headers: {
                'Accept': 'application/json',
                'Content-Type': 'application/json',
                'authorization': 'Bearer    ' + window.authHelper.getWebApiToken()
            },
            body: JSON.stringify(processTypeItem)
        };

        fetch(this.requestUpdUrl, options)
            .catch(error => console.error('Error:', error))
            .then(response => {
                if (response.ok) {
                    this.setState({
                        items: this.state.updatedItems,
                        MessagebarText: <Trans>processTypeUpdatedSuccess</Trans>,
                        MessageBarType: MessageBarType.success,
                        isUpdate: false,
                        isUpdateMsg: true
                    });
                    setTimeout(function () { this.setState({ isUpdateMsg: false, MessageBarType: MessageBarType.success, MessagebarText: "" }); }.bind(this), 3000);

                    return response.json;
                } else {
                    this.setState({
                        MessagebarText: <Trans>errorOoccuredPleaseTryAgain</Trans>,
                        MessageBarType: MessageBarType.error,
                        isUpdate: false,
                        isUpdateMsg: true
                    });
                    setTimeout(function () { this.setState({ isUpdateMsg: false, MessageBarType: MessageBarType.error, MessagebarText: "" }); }.bind(this), 3000);
                }
            }).then(json => {
                //console.log(json);
                this.setState({ isUpdate: false });
            });


    }

    deleteProcessType(processTypeItem) {
        // API Delete call        
        this.requestUpdUrl = 'api/Process/' + processTypeItem.id;

        fetch(this.requestUpdUrl, {
            method: "DELETE",
            headers: { 'authorization': 'Bearer ' + this.authHelper.getWebApiToken() }
        })
            .catch(error => console.error('Error:', error))
            .then(response => {
                if (response.ok) {
                    let currentItems = this.state.items.filter(x => x.id !== processTypeItem.id);
                    this.setState({
                        items: currentItems,
                        MessagebarText: <Trans>processTypeDeletedSuccess</Trans>,
                        isUpdate: false,
                        isUpdateMsg: true
                    });

                    setTimeout(function () { this.setState({ isUpdateMsg: false, MessageBarType: MessageBarType.success, MessagebarText: "" }); }.bind(this), 3000);
                    return response.json;
                } else {
                    this.setState({
                        MessagebarText: <Trans>errorOoccuredPleaseTryAgain</Trans>,
                        isUpdate: false,
                        isUpdateMsg: true
                    });
                    setTimeout(function () { this.setState({ isUpdateMsg: false, MessageBarType: MessageBarType.error, MessagebarText: "" }); }.bind(this), 3000);
                }
            }).then(json => {
                //console.log(json);
                this.setState({ isUpdate: false });
            });


    }

    render() {
        const { columns, isCompactMode, items, selectionDetails } = this.state;
        const processTypeList = this.processTypeList(columns, isCompactMode, items, selectionDetails);
        if (this.state.loading) {
            return (
                <div className='ms-BasicSpinnersExample ibox-content pt15 '>
                    <Spinner size={SpinnerSize.large} label={<Trans>loading</Trans>} ariaLive='assertive' />
                </div>
            );
        } else {
            return (

                <div className='ms-Grid bg-white ibox-content'>
                    <div className='ms-Grid-row'>
                        <div className='ms-Grid-col ms-sm12 ms-md12 ms-lg12 p-10'>
                            <PrimaryButton iconProps={{ iconName: 'Add' }} className='pull-right mr20' onClick={() => this.onAddRow()} >&nbsp;<Trans>add</Trans></PrimaryButton>
                        </div>
                    </div>
                    <div className='ms-Grid-row'>
                        <div className='ms-Grid-col ms-sm12 ms-md12 ms-lg12'>
                            {processTypeList}
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