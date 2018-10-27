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

export class Industry extends Component {
    displayName = Industry.name

    constructor(props) {
        super(props);

        this.sdkHelper = window.sdkHelper;
        this.authHelper = window.authHelper;

        this.utils = new Utils();
        const columns = [
            {
                key: 'column1', 
                name: <Trans>industry</Trans>,
                headerClassName: 'ms-List-th browsebutton RegionCol',
                className: 'docs-TextFieldExample ms-Grid-col ms-sm12 ms-md12 ms-lg8 RegionCol',
                fieldName: 'Region',
                minWidth: 150,
                maxWidth: 250,
                isRowHeader: true,
                onRender: (item) => {
                    return (
                        <TextField
                            id={'txtIndustry' + item.id}
                            value={item.name}
                            onBlur={(e) => this.onBlurIndustryName(e, item, item.operation)}
                        />
                    );
                }
            },
            {
                key: 'column2',
                name: <Trans>action</Trans>,
                headerClassName: 'ms-List-th industryaction',
                className: 'ms-Grid-col ms-sm12 ms-md12 ms-lg4 industryaction',
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

        this.getIndustries().then();
    }

    componentWillMount() {
        //this.getIndustries();
    }

    async getIndustries() {
        let industryList = [];
        let industryList_length = 0;
        try{
            // call to API fetch Categories
            let requestUrl = 'api/Industry';
            let response = await fetch(requestUrl, {
                method: "GET",
                headers: { 'authorization': 'Bearer ' + this.authHelper.getWebApiToken() }
            });
            let data = await response.json();
            if(typeof data === 'string'){
                console.log("Industry_getIndustries : ", data);
            }else if(typeof data === 'object'){
                console.log("Industry_getIndustries : ", data);
                for (let i = 0; i < data.length; i++) {
                    let region = {};
                    region.id = data[i].id;
                    region.name = data[i].name;
                    region.operation = "update";
                    industryList.push(region);
                }
                industryList_length = industryList.length;
            }
            else{
                throw new Error("response is not an expected type : ", data);
            }
        }catch(error){
            console.log("Region_getRegions Error: ", error.message);
        }finally{
            this.setState({ items: industryList, loading: false, rowItemCounter:  industryList_length});
        }
    }

    createItem(key) {
        return {
            id: key,
            name: "",
            operation: "add"
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
        this.setState({ isUpdate: true });
        //deleteIndustry
        this.deleteIndustry(item);
    }


    industryList(columns, isCompactMode, items, selectionDetails) {
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

    onBlurIndustryName(e, item, operation) {
        this.setState({ isUpdate: true });
        //check Industry already exist in items
        for (let p = 0; p < this.state.items.length; p++) {
            if (this.state.items[p].name.toLowerCase() === e.target.value.toLowerCase()) {
                this.setState({
                    isUpdate: false,
                    isUpdateMsg: true,
                    MessagebarText: <Trans>industryExist</Trans>,
                    MessageBarType: MessageBarType.error
                });
                setTimeout(function () { this.setState({ isUpdateMsg: false, MessageBarType: "", MessagebarText: "" }); }.bind(this), 3000);
                return false;
            }
        }
        delete item['operation'];

        let updatedItems = this.state.items;
        let itemIdx = updatedItems.indexOf(item);
        updatedItems[itemIdx].name = e.target.value;
        this.setState({
            updatedItems: updatedItems
        });

        if (operation === "add") {
            this.addIndustry(updatedItems[itemIdx]);
        } else if (operation === "update") {
            this.updateIndustry(updatedItems[itemIdx]);
        }

    }

    addIndustry(industryItem) {
        console.log(industryItem);
        let industryObj = industryItem;
        // API Update call        
        this.requestUpdUrl = 'api/Industry';
        let options = {
            method: "POST",
            headers: {
                'Accept': 'application/json',
                'Content-Type': 'application/json',
                'authorization': 'Bearer    ' + window.authHelper.getWebApiToken()
            },
            body: JSON.stringify(industryObj)
        };

        fetch(this.requestUpdUrl, options)
            .catch(error => console.error('Error:', error))
            .then(response => {
                if (response.ok) {
                    this.setState({
                        items: this.state.updatedItems,
                        MessagebarText: <Trans>industryAddedSuccess</Trans>,
                        isUpdate: false,
                        isUpdateMsg: true,
                        MessageBarType: MessageBarType.success
                    });
                    setTimeout(function () { this.setState({ isUpdateMsg: false, MessageBarType: "", MessagebarText: "" }); }.bind(this), 3000);
                    return response.json;
                } else {
                    this.setState({
                        MessagebarText: <Trans>errorOoccuredPleaseTryAgain</Trans>,
                        isUpdate: false,
                        isUpdateMsg: true,
                        MessageBarType: MessageBarType.error
                    });
                    setTimeout(function () { this.setState({ isUpdateMsg: false, MessageBarType: "", MessagebarText: "" }); }.bind(this), 3000);
                }
            }).then(json => {
                //console.log(json);
                this.setState({ isUpdate: false });
            });


    }

    updateIndustry(industryItem) {
        console.log(industryItem);
        let industryObj = industryItem;
        // API Update call        
        this.requestUpdUrl = 'api/Industry';
        let options = {
            method: "PATCH",
            headers: {
                'Accept': 'application/json',
                'Content-Type': 'application/json',
                'authorization': 'Bearer    ' + window.authHelper.getWebApiToken()
            },
            body: JSON.stringify(industryObj)
        };

        fetch(this.requestUpdUrl, options)
            .catch(error => console.error('Error:', error))
            .then(response => {
                if (response.ok) {
                    this.setState({
                        items: this.state.updatedItems,
                        MessagebarText: <Trans>industryUpdatedSuccess</Trans>,
                        isUpdate: false,
                        isUpdateMsg: true,
                        MessageBarType: MessageBarType.success
                    });
                    setTimeout(function () { this.setState({ isUpdateMsg: false, MessageBarType: "", MessagebarText: "" }); }.bind(this), 3000);

                    return response.json;
                } else {
                    this.setState({
                        MessagebarText: <Trans>errorOoccuredPleaseTryAgain</Trans>,
                        isUpdate: false,
                        isUpdateMsg: true,
                        MessageBarType: MessageBarType.error
                    });
                    setTimeout(function () { this.setState({ isUpdateMsg: false, MessageBarType: MessageBarType.error, MessagebarText: "" }); }.bind(this), 3000);
                }
            }).then(json => {
                //console.log(json);
                this.setState({ isUpdate: false });
            });


    }

    deleteIndustry(industryItem) {
        console.log(industryItem);
        let industryObj = industryItem;
        delete industryObj['operation'];
        // API Update call        
        this.requestUpdUrl = 'api/Industry/' + industryObj.id;
        console.log(industryObj);

        fetch(this.requestUpdUrl, {
            method: "DELETE",
            headers: { 'authorization': 'Bearer ' + this.authHelper.getWebApiToken() }
        })
            .catch(error => console.error('Error:', error))
            .then(response => {
                if (response.ok) {
                    let currentItems = this.state.items.filter(x => x.id !== industryObj.id);
                    this.industry = currentItems;
                    this.setState({
                        items: currentItems,
                        isUpdate: false
                    });
                    this.setState({
                        items: currentItems,
                        MessagebarText: <Trans>industryDeletedSuccess</Trans>,
                        isUpdate: false,
                        isUpdateMsg: true,
                        MessageBarType: MessageBarType.success
                    });

                    setTimeout(function () { this.setState({ isUpdateMsg: false, MessageBarType: "", MessagebarText: "" }); }.bind(this), 3000);
                    return response.json;
                } else {
                    this.setState({
                        MessagebarText: <Trans>errorOoccuredPleaseTryAgain</Trans>,
                        isUpdate: false,
                        isUpdateMsg: true,
                        MessageBarType: MessageBarType.error
                    });
                    setTimeout(function () { this.setState({ isUpdateMsg: false, MessageBarType: "", MessagebarText: "" }); }.bind(this), 3000);
                }
            }).then(json => {
                //console.log(json);
                this.setState({ isUpdate: false });
            });


    }


    render() {
        const { columns, isCompactMode, items, selectionDetails } = this.state;
        const industryList = this.industryList(columns, isCompactMode, items, selectionDetails);
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
                        <div className='ms-Grid-col ms-sm12 ms-md12 ms-lg12'>
                            <div className='ms-Grid-col ms-sm12 ms-md12 ms-lg12 pt10'>
                                <Link href='' className='pull-left' onClick={() => this.onAddRow()} >+ <Trans>addNew</Trans></Link>
                            </div>
                            {industryList}
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