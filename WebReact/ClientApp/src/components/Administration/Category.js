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


export class Category extends Component {
    displayName = Category.name

    constructor(props) {
        super(props);

        this.sdkHelper = window.sdkHelper;
        this.authHelper = window.authHelper;

        this.utils = new Utils();
        const columns = [
            {
                key: 'column1', 
                name: <Trans>category</Trans>,
                headerClassName: 'ms-List-th browsebutton CategoryCol',
                className: 'docs-TextFieldExample ms-Grid-col ms-sm12 ms-md12 ms-lg8 CategoryCol',
                fieldName: 'Category',
                minWidth: 150,
                maxWidth: 250,
                isRowHeader: true,
                onRender: (item) => {
                    return (
                        <TextField
                            id={'txtCategory' + item.id}
                            value={item.name}
                            onBlur={(e) => this.onBlurCategoryName(e, item, item.operation)}
                        />
                    );
                }
            },
            {
                key: 'column2',
                name: <Trans>action</Trans>,
                headerClassName: 'ms-List-th Categoryaction',
                className: 'ms-Grid-col ms-sm12 ms-md12 ms-lg4 Categoryaction',
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

        this.category = [];

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

        this.getCategories().then();
    }

    componentWillMount() {
        //this.getCategories();
    }

    async getCategories() {
        let categoryList = [];
        let categoryList_length = 0;
        try{
            // call to API fetch Categories
            let requestUrl = 'api/Category';
            let response = await fetch(requestUrl, {
                method: "GET",
                headers: { 'authorization': 'Bearer ' + this.authHelper.getWebApiToken() }
            });
            let data = await response.json();
            if(typeof data === 'string'){
                console.log("Categorty_getCategories : ", data);
            }else if(typeof data === 'object'){
                console.log("Categorty_getCategories : ", data);
                for (let i = 0; i < data.length; i++) {
                    let category = {};
                    category.id = data[i].id;
                    category.name = data[i].name;
                    category.operation = "update";
                    categoryList.push(category);
                }
                categoryList_length = categoryList.length;
            }
            else{
                throw new Error("response is not an expected type : ", data);
            }
        }catch(error){
            console.log("Categorty_getCategories Error: ", error.message);
        }finally{
            this.setState({ items: categoryList, loading: false, rowItemCounter:  categoryList_length});
        }
    }

    createCategoryItem(key) {
        return {
            id: key,
            name: "",
            operation:"add"
        };
    }

    onAddRow() {
        let rowCounter = this.state.rowItemCounter + 1;
        let newItems = [];
        newItems.push(this.createCategoryItem(rowCounter));

        let currentItems = this.state.items.concat(newItems);

        this.setState({
            items: currentItems,
            rowItemCounter: rowCounter
        });
    }

    deleteRow(item) {
        this.setState({ isUpdate: true });

        //deleteCategory
        this.deleteCategory(item);
    }

    //Category List - Details
    categoryList(columns, isCompactMode, items, selectionDetails) {
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

    onBlurCategoryName(e, item, operation) {
        this.setState({ isUpdate: true });
        //check Category already exist in items
        for (let p = 0; p < this.state.items.length; p++) {
            if (this.state.items[p].name.toLowerCase() === e.target.value.toLowerCase()) {
                this.setState({
                    isUpdate: false,
                    isUpdateMsg: true,
                    MessagebarText: <Trans>categoryExist</Trans>,
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
            this.addCategory(updatedItems[itemIdx]);
        } else if (operation === "update") {
            this.updateCategory(updatedItems[itemIdx]);
        }
        
    }

    addCategory(categoryItem) {
        console.log(categoryItem);
        let categoriesObj = categoryItem;
        // API Update call        
        this.requestUpdUrl = 'api/Category';
        let options = {
            method: "POST",
            headers: {
                'Accept': 'application/json',
                'Content-Type': 'application/json',
                'authorization': 'Bearer    ' + window.authHelper.getWebApiToken()
            },
            body: JSON.stringify(categoriesObj)
        };

        fetch(this.requestUpdUrl, options)
            .catch(error => console.error('Error:', error))
            .then(response => {
                if (response.ok) {
                    this.setState({
                        items: this.state.updatedItems,
                        MessagebarText: <Trans>categoryAddSuccess</Trans>,
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

    updateCategory(categoryItem) {
        let categoriesObj = categoryItem;
        // API Update call        
        this.requestUpdUrl = 'api/Category';
        let options = {
            method: "PATCH",
            headers: {
                'Accept': 'application/json',
                'Content-Type': 'application/json',
                'authorization': 'Bearer    ' + window.authHelper.getWebApiToken()
            },
            body: JSON.stringify(categoriesObj)
        };

        fetch(this.requestUpdUrl, options)
            .catch(error => console.error('Error:', error))
            .then(response => {
                if (response.ok) {
                    this.setState({
                        items: this.state.updatedItems,
                        MessagebarText: <Trans>categoryUpdatedSuccess</Trans>,
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

    deleteCategory(categoryItem) {
        // API Update call        
        this.requestUpdUrl = 'api/Category/'+categoryItem.id;

        fetch(this.requestUpdUrl, {
            method: "DELETE",
            headers: { 'authorization': 'Bearer ' + this.authHelper.getWebApiToken() }
        })
            .catch(error => console.error('Error:', error))
            .then(response => {
                if (response.ok) {
                    let currentItems = this.state.items.filter(x => x.id !== categoryItem.id);
                    this.category = currentItems;
                    this.setState({
                        items: currentItems,
                        MessagebarText: <Trans>categoryDeletedSuccess</Trans>,
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
        const categoryList = this.categoryList(columns, isCompactMode, items, selectionDetails);
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
                            {categoryList}
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