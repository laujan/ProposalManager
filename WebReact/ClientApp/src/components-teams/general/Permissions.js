/*
*  Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license.
*  See LICENSE in the source repository root for complete license information.
*/

import React, { Component } from 'react';
import { TextField } from 'office-ui-fabric-react/lib/TextField';
import { IconButton, PrimaryButton } from 'office-ui-fabric-react/lib/Button';
import { DetailsList, DetailsListLayoutMode, SelectionMode } from 'office-ui-fabric-react/lib/DetailsList';
import {
    Spinner,
    SpinnerSize
} from 'office-ui-fabric-react/lib/Spinner';
import { MessageBar, MessageBarType } from 'office-ui-fabric-react/lib/MessageBar';
import { Trans } from "react-i18next";
import { Dropdown } from 'office-ui-fabric-react/lib/Dropdown';

export class Permissions extends Component {
    displayName = Permissions.name

    constructor(props) {
        super(props);

        this.sdkHelper = window.sdkHelper;
        this.authHelper = window.authHelper;

        const columns = [
            {
                key: 'column1',
                name: <Trans>adGroupName</Trans>,
                headerClassName: 'ms-List-th browsebutton',
                className: 'docs-TextFieldExample ms-Grid-col ms-sm12 ms-md12 ms-lg8',
                fieldName: 'adGroupName',
                minWidth: 150,
                maxWidth: 250,
                isRowHeader: true,
                onRender: (item) => {
                    return (
                        <TextField
                            id={'txtAdGroupName' + item.id}
                            value={item.adGroupName}
                            onBlur={(e) => this.onBlurAdGroupName(e, item, "agGroupName")}
                        />
                    );
                }
            },
            {
                key: 'column2',
                name: <Trans>role</Trans>,
                headerClassName: 'ms-List-th browsebutton',
                className: 'docs-TextFieldExample ms-Grid-col ms-sm12 ms-md12 ms-lg8',
                fieldName: 'role',
                minWidth: 150,
                maxWidth: 200,
                isRowHeader: true,
                onRender: (item) => {
                    return (
                        <Dropdown
                            id={'txtRole' + item.id}
                            ariaLabel='Role'
                            options={this.state.roles}
                            defaultSelectedKey={item.role.displayName}
                            onChanged={(e) => this.onBlurAdGroupName(e, item, "role")}

                        />
                    );
                }
            },
            {
                key: 'column3',
                name: <Trans>permissions</Trans>,
                headerClassName: 'ms-List-th browsebutton',
                className: 'docs-TextFieldExample ms-Grid-col ms-sm12 ms-md12 ms-lg8',
                fieldName: 'permissions',
                minWidth: 150,
                maxWidth: 300,
                isRowHeader: true,
                onRender: (item) => {
                    return (
                        <div className="docs-DropdownExample">
                            <Dropdown
                                id={'txtPermissions' + item.id}
                                ariaLabel='Permissions'
                                multiSelect
                                options={this.state.permissionTypes}
                                defaultSelectedKeys={item.selPermissions}
                                onChanged={(e) => this.onChangePermissions(e, item)}
                                onBlur={(e) => this.onBlurPermissions(e, item)}
                            //onChanged={(e) => this.onBlurAdGroupName(e, item, "permission")}
                            />
                        </div>
                    );
                }
            },
            {
                key: 'column4',
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
            isUpdateMsg: false,
            roles: [],
            permissionTypes: []
        };
    }

    async componentWillMount() {
        let rolesList = await this.getAllRoles();
        let permissionsList = await this.getAllPemissionTypes();
        let data = await this.getAllPermissionsList();
        this.setState({ items: data.items, loading: data.loading, rowItemCounter: data.rowItemCounter, permissionTypes: permissionsList, roles: rolesList });
    }

    async getAllRoles() {
        this.setState({ loading: true });
        let requestUrl = 'api/Roles';
        const response = await fetch(requestUrl, { method: "GET", headers: { 'authorization': 'Bearer ' + this.authHelper.getWebApiToken() } });
        const data = await response.json();

        try {
            let allRoles = data;
            let rolesList = allRoles.map(role => { return { "key": role.displayName, "text": role.displayName }; });
            return rolesList;
        }
        catch (err) {
            console.log("Permission.js getAllRoles :, ", err);
            return false;
        }
    }

    async getAllPemissionTypes() {
        this.setState({ loading: true });
        let requestUrl = 'api/Permissions';
        const response = await fetch(requestUrl, { method: "GET", headers: { 'authorization': 'Bearer ' + this.authHelper.getWebApiToken() } });
        const data = await response.json();
        try {
            let allPermissions = data;
            let permissionsList = allPermissions.map(permission => { return { "key": permission.name, "text": permission.name }; });
            return permissionsList;
        }
        catch (err) {
            console.log("Permission.js getAllPemissionTypes :, ", err);
            return false;
        }
    }

    async getAllPermissionsList(shadowLoading = false) {
        if (!shadowLoading)
            this.setState({ loading: true });
        let requestUrl = 'api/RoleMapping';
        const response = await fetch(requestUrl, { method: "GET", headers: { 'authorization': 'Bearer ' + this.authHelper.getWebApiToken() } });
        const data = await response.json();
        try {
            let allPermissions = data;

            for (let p = 0; p < allPermissions.length; p++) {
                let permissionsList = [];
                for (let i = 0; i < allPermissions[p].permissions.length; i++) {
                    permissionsList.push(allPermissions[p].permissions[i].name);
                }
                allPermissions[p].selPermissions = permissionsList;
            }

            return { items: allPermissions, loading: false, rowItemCounter: allPermissions.length };
        }
        catch (err) {
            console.log("Permission.js getAllPermissionsList :, ", err);
            return false;
        }
    }

    createRowItem(key) {
        return {
            id: "", //key.toString(),
            adGroupName: "",
            role: "",
            permissions: ""
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

        //deletePermission
        this.deletePermission(item);
    }

    //Permissions List - Details
    permissionsList(columns, isCompactMode, items, selectionDetails) {
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
    copyArray = (array) => {
        const newArray = [];
        for (let i = 0; i < array.length; i++) {
            newArray[i] = array[i];
        }
        return newArray;
    };

    onChangePermissions(e, item) {
        console.log("vishnu:onChangePermissions: ", e);
        let updatedItems = [];
        if (this.state.updatedItems.length > 0) {
            updatedItems = this.copyArray(this.state.updatedItems);
        }

        if (e.selected) {
            if (Array.isArray(item["permissions"]))
                item["permissions"].push({ "id": "", "name": e.key });
            else {
                item["permissions"] = [];
                item["permissions"].push({ "id": "", "name": e.key });
            }
            updatedItems.push(item);
        }
        else {
            console.log("vishnu:onChangePermissions: ", item);
            if (Array.isArray(item["permissions"])) {
                let index = item["permissions"].findIndex(obj => obj["name"] === e.key);
                console.log("vishnu onChangePermissions: ", index);
                if (index >= -1) {
                    item["permissions"].splice(index, 1);
                    updatedItems.push(item);
                }
            } else
                return false;
        }
        console.log("vishnu:onChangePermissions: ", updatedItems);
        this.setState({ updatedItems });
    }

    onBlurPermissions(e, item) {
        let updatedItems = [];
        if (this.state.updatedItems.length > 0) {
            updatedItems = this.copyArray(this.state.updatedItems);
        }

        let itemIdx = updatedItems.indexOf(item);
        if (itemIdx === -1) {
            return false;
            if (!updatedItems[itemIdx].adGroupName || !updatedItems[itemIdx].role || !updatedItems[itemIdx].permissions) {
                return false;
            }
            //return false;
        }

        this.setState({ isUpdate: true });
        let postOrPatchObject = {};
        postOrPatchObject["id"] = updatedItems[itemIdx].id;
        postOrPatchObject["adGroupName"] = updatedItems[itemIdx].adGroupName;
        postOrPatchObject["role"] = updatedItems[itemIdx].role;
        postOrPatchObject["permissions"] = updatedItems[itemIdx].permissions;
        console.log("vishnu onChangePermissions: ", updatedItems);
        if (item.id.length === 0) {
            postOrPatchObject["role"] = { "id": "", "displayName": postOrPatchObject["role"] };
            this.addPermission(postOrPatchObject);
        } else if (item.id.length > 0) {
            this.updatePermission(postOrPatchObject);
        }
    }

    onBlurAdGroupName(e, item, colName) {
        let updatedItems = this.state.items;
        let itemIdx = updatedItems.indexOf(item);
        updatedItems[itemIdx] = item;
        if (e.key) {
            if (colName === "role")
                updatedItems[itemIdx].role = e.text;
        } else {
            if (e.target.id.match("txtAdGroupName")) {
                if (item.id.length === 0) {
                    //Check permission already exist while add new
                    let isProcessExist = this.state.items.some(obj => obj.adGroupName.toLowerCase() === e.target.value.toLowerCase());
                    if (isProcessExist) {
                        this.setState({
                            MessagebarText: <Trans>permissionAlreadyExist</Trans>,
                            MessageBarType: MessageBarType.error,
                            isUpdate: false,
                            isUpdateMsg: true
                        });
                        setTimeout(function () { this.setState({ isUpdateMsg: false, MessageBarType: MessageBarType.error, MessagebarText: "" }); }.bind(this), 3000);
                        return false;
                    }
                }
                updatedItems[itemIdx].adGroupName = e.target.value;
            }


        }

        if (!updatedItems[itemIdx].adGroupName || !updatedItems[itemIdx].role || !updatedItems[itemIdx].permissions) {
            return false;
        }

        this.setState({
            isUpdate: true,
            updatedItems: updatedItems
        });

        if (item.id.length === 0) {
            this.addPermission(updatedItems[itemIdx]);
        } else if (item.id.length > 0) {
            this.updatePermission(updatedItems[itemIdx]);
        }

    }

    async addPermission(permissionItem) {
        this.requestUpdUrl = 'api/RoleMapping';
        let options = {
            method: "POST",
            headers: {
                'Accept': 'application/json',
                'Content-Type': 'application/json',
                'authorization': 'Bearer    ' + window.authHelper.getWebApiToken()
            },
            body: JSON.stringify(permissionItem)
        };
        try {
            let response = await fetch(this.requestUpdUrl, options);
            if (response.ok) {
                let some = await this.getAllPermissionsList(true);
                this.setState({
                    items: some.items,
                    MessagebarText: <Trans>permissionAddSuccess</Trans>,
                    MessageBarType: MessageBarType.success,
                    isUpdate: false,
                    isUpdateMsg: true
                });
                setTimeout(function () { this.setState({ isUpdateMsg: false, MessagebarText: "" }); }.bind(this), 3000);
                return "done";
            } else {
                this.setState({
                    MessagebarText: <Trans>errorOoccuredPleaseTryAgain</Trans>,
                    MessageBarType: MessageBarType.error,
                    isUpdate: false,
                    isUpdateMsg: true
                });
                setTimeout(function () { this.setState({ isUpdateMsg: false, MessagebarText: "" }); }.bind(this), 3000);
            }
        } catch (error) {
            console.log("Permission.js getAllPermissionsList :, ", error);
            return false;
        }
    }

    async updatePermission(permissionItem) {
        this.requestUpdUrl = 'api/RoleMapping';
        let options = {
            method: "PATCH",
            headers: {
                'Accept': 'application/json',
                'Content-Type': 'application/json',
                'authorization': 'Bearer    ' + window.authHelper.getWebApiToken()
            },
            body: JSON.stringify(permissionItem)
        };

        try {
            let response = await fetch(this.requestUpdUrl, options);
            if (response.ok) {
                let some = await this.getAllPermissionsList(true);
                this.setState({
                    items: some.items,
                    MessagebarText: <Trans>permissionUpdatedSuccess</Trans>,
                    MessageBarType: MessageBarType.success,
                    isUpdate: false,
                    isUpdateMsg: true
                });
                setTimeout(function () { this.setState({ isUpdateMsg: false, MessagebarText: "" }); }.bind(this), 3000);
                return "done";
            } else {
                this.setState({
                    MessagebarText: <Trans>errorOoccuredPleaseTryAgain</Trans>,
                    MessageBarType: MessageBarType.error,
                    isUpdate: false,
                    isUpdateMsg: true
                });
                setTimeout(function () { this.setState({ isUpdateMsg: false, MessagebarText: "" }); }.bind(this), 3000);
            }
        } catch (error) {
            console.log("Permission.js getAllPermissionsList :, ", error);
            return false;
        }

    }

    async deletePermission(permissionItem) {
        // API Update call        
        this.requestUpdUrl = 'api/RoleMapping/' + permissionItem.id;
        let options = {
            method: "DELETE",
            headers: {
                'authorization': 'Bearer ' + window.authHelper.getWebApiToken() 
            }
        };
        try {
            let response = await fetch(this.requestUpdUrl, options);
            if (response.ok) {
                // let currentItems = this.state.items.filter(x => x.id !== permissionItem.id);
                let updatedPermissions = await this.getAllPermissionsList(true);
                this.setState({
                    items: updatedPermissions.items,
                    MessagebarText: <Trans>permissionDeletedSuccess</Trans>,
                    MessageBarType: MessageBarType.success,
                    isUpdate: false,
                    isUpdateMsg: true
                });

                setTimeout(function () { this.setState({ isUpdateMsg: false, MessagebarText: "" }); }.bind(this), 3000);
                return response.json;
            } else {
                this.setState({
                    MessagebarText: <Trans>errorOoccuredPleaseTryAgain</Trans>,
                    MessageBarType: MessageBarType.error,
                    isUpdate: false,
                    isUpdateMsg: true
                });
                setTimeout(function () { this.setState({ isUpdateMsg: false, MessagebarText: "" }); }.bind(this), 3000);
            }
        } catch (error) {
            this.setState({ isUpdate: false });
            console.log("Permission getAllPermissionsList :, ", error);
            return false;
        }
    }

    render() {
        const { columns, isCompactMode, items, selectionDetails } = this.state;
        const permissionsList = this.permissionsList(columns, isCompactMode, items, selectionDetails);
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
                            {permissionsList}
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