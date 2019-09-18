/* 
*  Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. 
*  See LICENSE in the source repository root for complete license information. 
*/
import React, { Component } from 'react';
import { TextField } from 'office-ui-fabric-react/lib/TextField';
import { IconButton, ActionButton, PrimaryButton } from 'office-ui-fabric-react/lib/Button';
import { DetailsList, DetailsListLayoutMode, SelectionMode } from 'office-ui-fabric-react/lib/DetailsList';
import { Spinner, SpinnerSize } from 'office-ui-fabric-react/lib/Spinner';
import { MessageBar, MessageBarType } from 'office-ui-fabric-react/lib/MessageBar';
import { Trans } from "react-i18next";
import { Dropdown } from 'office-ui-fabric-react/lib/Dropdown';
import Utils from '../../../helpers/Utils';
import { Modal } from 'office-ui-fabric-react/lib/Modal';
import { Link } from 'office-ui-fabric-react/lib/Link';
import { Checkbox } from 'office-ui-fabric-react/lib/Checkbox';

export class MetaData extends Component {
    displayName = MetaData.name

    constructor(props) {
        super(props);
        this.utils = new Utils();
        this.authHelper = window.authHelper;
        this.logService = this.props.logService;
        this.apiService = this.props.apiService;
        this.accessGranted = false;
        let rowCounter = 0;

        this.schema = {
            id: "",
            displayName: "",
            fieldType: { name: "", value: "" },
            screen: "",
            values: ""
        };

        const columns = [
            {
                key: 'column1',
                name: <Trans>displayName</Trans>,
                headerClassName: 'ms-List-th browsebutton',
                className: 'docs-TextFieldExample ms-Grid-col ms-sm12 ms-md12 ms-lg8',
                fieldName: 'displayName',
                minWidth: 150,
                maxWidth: 240,
                isRowHeader: true,
                onRender: (item) => {
                    return (
                        <TextField
                            id={'txtDisplayName' + item.id}
                            value={item.displayName}
                            onBlur={(e) => this.onBlurColName(e, item, "displayName")}
                            required={true}
                        />
                    );
                }
            },
            {
                key: 'column2',
                name: <Trans>fieldType</Trans>,
                headerClassName: 'ms-List-th browsebutton',
                className: 'docs-TextFieldExample ms-Grid-col ms-sm12 ms-md12 ms-lg8',
                fieldName: 'fieldType',
                minWidth: 150,
                maxWidth: 290,
                isRowHeader: true,
                onRender: (item) => {
                    return (
                        <div className="docs-DropdownExample">
                            <Dropdown
                                id={'txtFieldType' + item.id}
                                ariaLabel='fieldType'
                                options={[
                                    { key: 'String', text: 'String', value: 1 },
                                    { key: 'DropDown', text: 'DropDown', value: 2 },
                                    { key: 'Int', text: 'Int', value: 3 },
                                    { key: 'Double', text: 'Double', value: 4 },
                                    { key: 'Date', text: 'Date', value: 5 }
                                ]}
                                defaultSelectedKey={item.fieldType.name}
                                onChanged={(e) => this.onBlurColName(e, item, "fieldType")}
                            />
                        </div>
                    );
                }
            },
            {
                key: 'column3',
                name: <Trans>defaultValue</Trans>,
                headerClassName: 'ms-List-th browsebutton',
                className: 'docs-TextFieldExample ms-Grid-col ms-sm6 ms-md6 ms-lg8',
                fieldName: 'values',
                minWidth: 150,
                maxWidth: 240,
                isRowHeader: true,
                onRender: (item) => {
                    return (
                        <div>
                            {
                                item.fieldType.name.toLowerCase() === "dropdown" ?
                                    <div className="ms-Grid-row">
                                        <div className="ms-Grid-col ms-sm4 ms-md4 ms-lg10">
                                            <Dropdown
                                                id={'ddlDefaultValue' + item.id}
                                                ariaLabel='fieldType'
                                                options={item.values.map(x => { return { 'key': x.id, 'text': x.name }; })}
                                                onChanged={(e) => this.onBlurColName(e, item, "values")}
                                            />
                                        </div>
                                        <div className="ms-Grid-col ms-sm2 ms-md2 ms-lg2">
                                            &nbsp;{item.fieldType.name.toLowerCase() === "dropdown" ? <IconButton iconProps={{ iconName: 'Edit' }} onClick={e => this.editDropDownValues(item)} /> : ""}
                                        </div>
                                    </div>
                                    :
                                    <TextField
                                        id={'txtValue' + item.id}
                                        value={item.values}
                                        onBlur={(e) => this.onBlurColName(e, item,"values")}
                                    />
                            }
                        </div>
                    );
                }
            },
            {
                key: 'column4',
                name: <Trans>screen</Trans>,
                headerClassName: 'ms-List-th browsebutton',
                className: 'docs-TextFieldExample ms-Grid-col ms-sm12 ms-md12 ms-lg8',
                fieldName: 'screen',
                minWidth: 150,
                maxWidth: 150,
                isRowHeader: true,
                onRender: (item) => {
                    return (
                        <TextField
                            id={'txtScreen' + item.id}
                            value={item.screen ? item.screen : ""}
                            onBlur={(e) => this.onBlurColName(e, item, "screen")}
                            required={true}
                        />
                    );
                }
            },
            {
                key: 'column5',
                name: <Trans>required</Trans>,
                headerClassName: 'ms-List-th browsebutton',
                className: 'docs-TextFieldExample ms-Grid-col ms-sm12 ms-md12 ms-lg8',
                fieldName: 'screen',
                minWidth: 70,
                maxWidth: 70,
                isRowHeader: true,
                onRender: (item) => {
                    return (
                        <Checkbox id={'chkCompleted' + item.id}
                            onChange={(e) => this.onBlurColName(e, item, "required")}
                            ariaDescribedBy={'descriptionID'}
                            checked={item.required}
                        />
                    );
                }
            },
            {
                key: 'column5',
                name: <Trans>required</Trans>,
                headerClassName: 'ms-List-th browsebutton',
                className: 'docs-TextFieldExample ms-Grid-col ms-sm12 ms-md12 ms-lg8',
                fieldName: 'screen',
                minWidth: 70,
                maxWidth: 70,
                isRowHeader: true,
                onRender: (item) => {
                    return (
                        <Checkbox id={'chkCompleted' + item.id}
                            onChange={(e) => this.onBlurColName(e, item, "required")}
                            ariaDescribedBy={'descriptionID'}
                            checked={item.required}
                        />
                    );
                }
            },
            {
                key: 'column4',
                name: <Trans>actions</Trans>,
                headerClassName: 'ms-List-th',
                className: 'ms-Grid-col ms-sm12 ms-md12 ms-lg4',
                minWidth: 16,
                maxWidth: 30,
                onRender: (item) => {
                    return (
                        <div>
                            <IconButton iconProps={{ iconName: 'Save' }} onClick={e => this.saveRow(e, item)} /> &nbsp;&nbsp;&nbsp;
                            <IconButton iconProps={{ iconName: 'Delete' }} onClick={e => this.deleteRow(item)} /> &nbsp;&nbsp;&nbsp;
                        </div>
                    );
                }
            }
        ];

        const dpListColumns = [
            {
                key: 'column1',
                name: <Trans>name</Trans>,
                headerClassName: 'ms-List-th',
                className: 'docs-TextFieldExample ms-Grid-col ms-sm12 ms-md12 ms-lg4 RegionCol',
                fieldName: 'name',
                minWidth: 100,
                maxWidth: 250,
                isRowHeader: true,
                onRender: (item) => {
                    return (
                        <TextField
                            id={'txtName' + item.id}
                            value={item.name}
                            onBlur={(e) => this.onBlurTxtName(e, item)}
                        />
                    );
                }
            },
            {
                key: 'column2',
                name: <Trans>action</Trans>,
                headerClassName: 'ms-List-th',
                className: 'ms-Grid-col ms-sm12 ms-md12 ms-lg4 categoryaction',
                minWidth: 30,
                maxWidth: 30,
                onRender: (item) => {
                    return (
                        <div>
                            <IconButton iconProps={{ iconName: 'Delete' }} onClick={e => this.dropDownValueDeleteRow(item)} />
                        </div>
                    );
                }
            }
        ];

        this.state = {
            items: [],
            rowItemCounter: rowCounter,
            columns: columns,
            isCompactMode: false,
            loading: true,
            isUpdate: false,
            MessagebarText: "",
            MessageBarType: MessageBarType.success,
            isUpdateMsg: false,
            showDropdownValuesModel: false,
            selectedDropdownItem: [],
            dpListColumns: dpListColumns,
            isModelUpdate: false,
            isModelUpdateMsg: "",
            modelMessagebarText: "",
            modelMessageBarType: MessageBarType.success,
            currentItem:this.schema
        };
    }

    async componentDidMount() {
        this.logService.log("MetaData_componentDidMount isauth: " + this.authHelper.isAuthenticated());
        if (this.authHelper.isAuthenticated() && !this.accessGranted) {
            try {
                await this.authHelper.callCheckAccess(["Administrator", "Opportunity_ReadWrite_Template", "Opportunities_ReadWrite_All"]);
                this.accessGranted = true;
                await this.getMetaDataList();
            } catch (error) {
                this.accessGranted = false;
                this.logService.log(error);
            }
        }
    }

    async getMetaDataList() {
        let items = [];
        this.apiService.callApi('MetaData', 'GET')
            .then(async (response) => {
                if (response.ok) {
                    let data = await response.json();
                    items = JSON.parse(JSON.stringify(data));
                }
            })
            .catch(error => {
                this.setMessage(false, true, MessageBarType.error, error.message);
            })
            .finally(() => {
                this.setState({ items, loading: false, rowItemCounter: items.length });

                return items;
            });        
    }

    metaDataList(columns, isCompactMode, items) {
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

    createRowItem() {
        return this.schema;
    }

    onAddRow() {
        let newItems = [];
        newItems.push(this.createRowItem());

        let items = this.state.items.concat(newItems);

        this.setState({items});
    }

    async deleteRow(item) {
        let items = this.state.items; 
        if (item.id && item.id.length > 0) {
            if (!window.confirm("Do you want to delete the item?")) {
                return;
            }

            this.setState({ isUpdate: true });
            items = items.filter(p => p.id !== item.id);
            await this.apiService.callApi('MetaData', 'DELETE', { id: item.id })
                .then(async (response) => {
                    if (response.ok) {
                        this.setMessage(false, true, MessageBarType.success, "Metadata deleted successfully.");
                    } else {
                        throw new Error("Metadata delete failed.");
                    }
                })
                .catch(error => {
                    this.setMessage(false, true, MessageBarType.error, error);
                    return false;
                });
        }
        else {
            items = items.reduce((result, element) => {
                if (element.displayName !== item.displayName) {
                    result.push(element);
                }
                else if (element.id !== item.id) {
                    result.push(element);
                }

                return result;
            }, []);
        }

        this.setState({ currentItem: this.schema, items: items, isUpdate: false });
    }

    setMessage(isUpdate, isUpdateMsg, MessageBarType, MessagebarText) {
        //Show message
        this.setState({ isUpdate, isUpdateMsg, MessageBarType, MessagebarText });

        //Schedule message hide
        setTimeout(function () {
            this.setState({ isUpdate: false, isUpdateMsg: false, MessageBarType: "", MessagebarText: "" });
        }.bind(this), 2000);
    }

    // edit dropdown values
    editDropDownValues(item) {
        let currentItem = item.id === this.state.currentItem.id ? JSON.parse(JSON.stringify(this.state.currentItem)) : item;
        this.setState({showDropdownValuesModel: true,currentItem });
    }

    closeModal() {
        let items = JSON.parse(JSON.stringify(this.state.items));
        let currentItem = JSON.parse(JSON.stringify(this.state.currentItem));
        
        this.setState({ showDropdownValuesModel: false, items, currentItem });
    }

    onAddRowModelItem() {
        let currentItem = JSON.parse(JSON.stringify(this.state.currentItem));

        if (currentItem.values.length === 0) {
            currentItem.values = [];
        }

        currentItem.values.push({ name: "", typeName: "DropDownMetaDataValue", id: currentItem.values.length + 1});

        this.setState({ currentItem });
    }

    onBlurTxtName(e, item) {
        let value = e.target.value;
        let currentItem = this.state.currentItem;
        let items = this.state.items;
        this.logService.log("MetaData_onBlurTxtName : ", currentItem , item ,value);
        if (value) {
            try {
                //Checking item is already present
                if (this.checkDropOptionValueIsAlreadyExist(value))
                {
                    this.setMessage(false, true, MessageBarType.error, <Trans>optionValueAlreadyExist</Trans>);
                    return;
                }

                currentItem.values.forEach((c) => {
                    if (c.id === item.id) {
                        c.name = value;
                        c.id = item.id;
                    }
                });

            }
            catch (error) {
                this.logService.log(error.message);
                this.setMessage(false, true, MessageBarType.error, error.message);
            }
            finally {
                if (currentItem.id.length === 0) {
                    items[items.length - 1] = currentItem;
                }
                else {
                    let index = items.findIndex(obj => obj.id === currentItem.id);
                    if (index !== -1) {
                        items[index] = currentItem;
                    }
                }
                this.setState({
                    currentItem, items
                });
            }
        }
    }

    checkDropOptionValueIsAlreadyExist(value) {
        let flag = false;
        let updatedItems = this.state.currentItem.values;
        let index = updatedItems.findIndex(opt => opt.name.toLowerCase() === value.toLowerCase());
        if (index !== -1) {
            this.setMessage(false, true, MessageBarType.error, <Trans>optionValueAlreadyExist</Trans>);

            flag = true;
        }
        return flag;
    }

    dropDownValueDeleteRow(dpValOptionItem) {
        let allItems = this.state.items;
        let selDpItem = this.state.currentItem;
        let dropListValues = selDpItem.values;
        dropListValues = dropListValues.filter(prop => prop.name !== dpValOptionItem.name);
        selDpItem.values = dropListValues;
        allItems[selDpItem.id] = selDpItem;
        this.setState({
            currentItem: selDpItem,
            items: allItems
        });
    }

    renderDropdownOptionsList(columns, isCompactMode, selDpItem) {
        let items = selDpItem.values;
        this.logService.log("Metadata_renderDropdownOptionsList items : ", items);
        return (
            <div className='ms-Grid-row'>
                <div className='ms-Grid-col ms-sm12 ms-md12 ms-lg12 p-10'>
                    <DetailsList
                        items={this.state.currentItem.values}
                        compact={isCompactMode}
                        columns={columns}
                        selectionMode={SelectionMode.none}
                        selectionPreservedOnEmptyClick='true'
                        setKey='set'
                        layoutMode={DetailsListLayoutMode.justified}
                        enterModalSelectionOnTouch='false'
                    />
                </div>
            </div>
        );
    }

    onBlurColName(e, item, colName) {
        let items = this.state.items;
        let currentItem = item.id === this.state.currentItem.id ? this.state.currentItem : item;
        
        let trackFlag = false;
        switch (colName.toLowerCase()) {
            case "displayname":
                    if (e.target.value.length>0) {
                        currentItem.displayName = e.target.value;
                        trackFlag = true;
                    }
                break;
            case "fieldtype":
                    currentItem.fieldType = { "id": e.key, "name": e.text };
                    currentItem.values = "";
                    if (e.key === "DropDown")currentItem.values = [];
                    trackFlag = true;
                break; 
            case "values":
                if (e.target.value) {
                    currentItem.values = e.target.value;
                    trackFlag = true;
                }
                break;
            case "screen":
                if (e.target.value) {
                    currentItem.screen = e.target.value;
                    trackFlag = true;
                }
                break;
            case "required":
                currentItem.required = !e.currentTarget.checked;
                trackFlag = true;
                break;
            default:
                break;
        }

        if (trackFlag) {
            if (item.id.length === 0) {
                items[items.length - 1] = currentItem;
            } else {
                let index = items.findIndex(obj => obj.id === currentItem.id);
                if (index !== -1) {
                    items[index] = currentItem;
                }
            }

            this.setState({ currentItem, items });
        }
    }

    async saveRow(e, item) {
        if (item.displayName.length === 0) {
            this.setMessage(false, true, MessageBarType.error, "You must enter a Display Name.");
            return;
        }

        if (item.fieldType.name.length === 0) {
            this.setMessage(false, true, MessageBarType.error, "You must select a Field Type.");
            return;
        }

        if (item.screen.length === 0) {
            this.setMessage(false, true, MessageBarType.error, "You must enter a Screen.");
            return;
        }

        let dispSuccessMsg = "";
        if (item.id.length === 0) {
            dispSuccessMsg = "Metadata added successfully.";
            await this.addOrUpdateMetaData(item, "POST", dispSuccessMsg);
        } else if (item.id.length > 0) {
            dispSuccessMsg = "Metadata updated successfully.";
            await this.addOrUpdateMetaData(item, "PATCH", dispSuccessMsg);
        }
    }

    async addOrUpdateMetaData(metaDataItem, methodType, dispSuccessMsg) {
        this.setState({ isUpdate: true });
        this.apiService.callApi('MetaData', methodType, { body: JSON.stringify(metaDataItem) })
            .then(async (response) => {
                if (response.ok) {

                    if (methodType === "POST") {
                        let newId = response.headers.get("location");
                        metaDataItem.id = newId;
                    }

                    this.setMessage(false, true, MessageBarType.success, dispSuccessMsg);
                } else {
                    dispSuccessMsg = <Trans>errorOoccuredPleaseTryAgain</Trans>;

                    this.setMessage(false, true, MessageBarType.error, dispSuccessMsg);
                }
            })
            .catch(() => {
                dispSuccessMsg = <Trans>errorOoccuredPleaseTryAgain</Trans>;
                this.setMessage(false, true, MessageBarType.error, dispSuccessMsg);
            })
            .finally(() => {                
                this.setState({ currentItem: this.schema, isUpdate: false });
            });
    }

    render() {
        const { columns, isCompactMode, items } = this.state;
        this.logService.log("MetaData_render items : ", items);
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
                        <div className='ms-Grid-col ms-sm4 ms-md4 ms-lg4'>
                            <h3 className='pageheading'><Trans>opportunityDataModel</Trans></h3>
                        </div>
                        <div className='ms-Grid-col ms-sm2 ms-md2 ms-lg8 p-10'>
                            <PrimaryButton iconProps={{ iconName: 'Add' }} className='pull-right mr20' onClick={() => this.onAddRow()} >&nbsp;<Trans>add</Trans></PrimaryButton>
                        </div>
                    </div>
                    <div className='ms-Grid-row'>
                        <div className='ms-Grid-col ms-sm12 ms-md12 ms-lg12'>
                            {this.metaDataList(columns, isCompactMode, items)}
                        </div>

                    </div>
                    <div className='ms-Grid-row'>
                        <div className='ms-Grid-col ms-sm12 ms-md12 ms-lg12'>
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
                            <div className='ms-BasicSpinnersExample p-10'>
                                {
                                    this.state.isUpdate ?
                                        <div className='overlay on'>
                                            <div className='overlayModal'>
                                                <Spinner size={SpinnerSize.large} className='savingSpinner' label='Saving data' />
                                            </div>
                                        </div>
                                        : ""
                                }
                            </div>
                        </div>
                    </div>
                    <div className="">
                        <div>
                            <Modal isOpen={this.state.showDropdownValuesModel}
                                onDismiss={this.closeModal}
                                isBlocking='true'
                                containerClassName="ms-modalExample-container"
                            >
                                <div className="ms-modalExample-header">
                                    <div className='ms-Grid-col ms-sm12 ms-md6 ms-lg12'>
                                        <ActionButton className="pull-right" onClick={this.closeModal.bind(this)}>
                                            <i className="ms-Icon ms-Icon--StatusCircleErrorX pull-right f30" aria-hidden="true" />
                                        </ActionButton>
                                    </div>
                                </div>
                                <div className="ms-modalExample-body">
                                    <div className="ms-Grid-row">
                                        <div className='ms-Grid-col ms-sm4 ms-md4 ms-lg6'>
                                            <Trans>manageDropdownOptionValues</Trans> <b> {this.state.currentItem.displayName} </b>
                                        </div>
                                    </div>
                                    <div className='ms-Grid-row'>
                                        <div className='ms-Grid-col ms-sm4 ms-md4 ms-lg6'>
                                            <Link href='' className='pull-right' onClick={() => this.onAddRowModelItem()} >+ <Trans>addNew</Trans></Link>
                                        </div>
                                    </div>
                                    <div className='ms-Grid-row'>
                                        <div className='ms-Grid-col ms-sm6 ms-md6 ms-lg6'>
                                            {this.renderDropdownOptionsList(this.state.dpListColumns, false, this.state.currentItem)}
                                        </div>
                                    </div>
                                    <div className='ms-Grid-row'>
                                        <div className='ms-Grid-col ms-sm6 ms-md6 ms-lg6 pull-left'/>

                                        <div className='ms-Grid-col ms-sm6 ms-md6 ms-lg6'>
                                            <div className='ms-BasicSpinnersExample p-10'>
                                                {
                                                    this.state.isModelUpdate ?
                                                        <div className='overlay on'>
                                                            <div className='overlayModal'>
                                                                <Spinner size={SpinnerSize.large} className='savingSpinner' label='Saving data' />
                                                            </div>
                                                        </div>
                                                        : ""
                                                }
                                                {
                                                    this.state.isModelUpdateMsg ?
                                                        <MessageBar
                                                            messageBarType={this.state.modelMessageBarType}
                                                            isMultiline={false}
                                                        >
                                                            {this.state.modelMessagebarText}
                                                        </MessageBar>
                                                        : ""
                                                }
                                            </div>
                                        </div>
                                    </div>
                                </div>
                            </Modal>
                        </div>
                    </div>
                </div>
            );
        }
    }
}