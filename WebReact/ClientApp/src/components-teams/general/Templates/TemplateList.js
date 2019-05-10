/* 
*  Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. 
*  See LICENSE in the source repository root for complete license information. 
*/

import React, { Component } from 'react';
import {
    DetailsList,
    DetailsListLayoutMode,
    Selection,
    SelectionMode
} from 'office-ui-fabric-react/lib/DetailsList';
import { MarqueeSelection } from 'office-ui-fabric-react/lib/MarqueeSelection';
import { DefaultButton, PrimaryButton, IconButton } from 'office-ui-fabric-react/lib/Button';
import { SearchBox } from 'office-ui-fabric-react/lib/SearchBox';
import { TooltipHost } from 'office-ui-fabric-react/lib/Tooltip';
import { I18n, Trans } from "react-i18next";
import {
    Spinner,
    SpinnerSize
} from 'office-ui-fabric-react/lib/Spinner';
import { MessageBar, MessageBarType } from 'office-ui-fabric-react/lib/MessageBar';
import { LinkContainer } from 'react-router-bootstrap';
import i18n from '../../../i18n';
import Accessdenied from '../../../helpers/AccessDenied';
import { getAllTemplatesList, getAllProcess } from './TemplatesCommon';

export class TemplateList extends Component {
    displayName = TemplateList.name

    constructor(props) {
        super(props);

        this.sdkHelper = window.sdkHelper;
        this.authHelper = window.authHelper;
        this.accessGranted = false;
        const columns = [
            {
                key: 'column1',
                name: <Trans>templateName</Trans>,
                fieldName: 'templateName',
                minWidth: 100,
                maxWidth: 200,
                isResizable: true
            },
            {
                key: 'column2',
                name: <Trans>lastUsed</Trans>,
                fieldName: 'lastUsed',
                minWidth: 100,
                maxWidth: 200,
                isResizable: true,
                ariaLabel: 'Last Used',
                onRender: (item) => {
                    return (
                        <div>
                            {new Date(item.lastUsed).toLocaleDateString(i18n.language)}
                        </div>
                    );
                }
            },
            {
                key: 'column3',
                name: <Trans>createdBy</Trans>,
                fieldName: 'createdDisplayName',
                minWidth: 100,
                maxWidth: 200,
                isResizable: true,
                ariaLabel: 'Created By'
            },
            {
                key: 'column4',
                name: <Trans>action</Trans>,
                headerClassName: 'ms-List-th dealTypeAction ',
                className: 'DetailsListExample-cell--FileIcon actioniconAlign  ',
                minWidth: 100,
                maxWidth: 100,
                onColumnClick: this.onColumnClick,
                onRender: (item) => {
                    return (
                        <div className=''>
                            <TooltipHost content={<Trans>edit</Trans>} calloutProps={{ gapSpace: 0 }} closeDelay={200}>
                                <IconButton iconProps={{ iconName: 'Edit' }} onClick={e => this.editTemplate(item)} />
                            </TooltipHost>
                        </div>
                    );
                }
            }
        ];

        this.state = {
            loading: true,
            channelName: "",
            columns: columns,
            selectedTemplateCount: 0,
            filterTemplateName: '',
            items: [],
            itemsOriginal: [],
            isUpdateMsg: false,
            MessageBarType: MessageBarType.success,
            haveGranularAccess: false,
            processItems: [],
            processItemsOriginal : []
        };

        this._onFilterByTemplateNameChanged = this._onFilterByTemplateNameChanged.bind(this);
        this.deleteTemplate = this.deleteTemplate.bind(this);
    }

    async componentDidMount() {
        console.log("code-review commments implementation");
        console.log("Templatelist_componentDidMount isauth: " + this.authHelper.isAuthenticated());
        if (this.authHelper.isAuthenticated() && !this.accessGranted) {
            try {
                await this.authHelper.callCheckAccess(["Administrator", "Opportunity_ReadWrite_Template", "Opportunities_ReadWrite_All"]);
                console.log("Templatelist_componentDidUpdate callCheckAccess success");
                this.accessGranted = true;
                await this.fnGetTemplates();
                await this.fnGetProcess();
                
            } catch (error) {
                this.accessGranted = false;
                console.log("Templatelist_componentDidUpdate error_callCheckAccess:");
                console.log(error);
            }
        }
    }


    componentDidUpdate() {
        console.log("Templatelist_componentDidUpdate isauth: " + this.authHelper.isAuthenticated() + " this.accessGranted: " + this.accessGranted);
    }

    async fnGetTemplates() {
        let templateObj = [];
        templateObj = await getAllTemplatesList();
        if (templateObj.length > 0) {
            this.setState({
                loading: false,
                items: templateObj,
                itemsOriginal: templateObj
            });
        } else {
            this.setState({
                loading: false,
                items: templateObj,
                itemsOriginal: templateObj
            });
        }
    }

    async fnGetProcess() {
        let processItems, processItemsOriginal = [];
        processItems = await getAllProcess();
        if (processItems.length > 0) {
            this.setState({
                loading: false,
                processItems,
                processItemsOriginal: processItems
            });
        } else {
            this.setState({
                loading: false,
                processItems,
                processItemsOriginal
            });
        }
    }

    errorHandler(err, referenceCall) {
        console.log("Get TemplatesList Ref: " + referenceCall + " error: " + JSON.stringify(err));
    }

    _selection = new Selection({
        onSelectionChanged: () => this.setState({ selectionDetails: this._getSelectionDetails() })
    });

    _getSelectionDetails() {
        const selectionCount = this._selection.getSelectedCount();
        return selectionCount;

    }

    // Filter by Templatename
    _onFilterByTemplateNameChanged(text) {
        const items = this.state.itemsOriginal;

        this.setState({
            filterTemplateName: text,
            items: text ?
                items.filter(item => item.templateName.toString().toLowerCase().indexOf(text.toString().toLowerCase()) > -1) :
                items
        });
    }

    deleteTemplate(items) {
        this.setState({ isUpdate: true });
        // API Delete call        
        this.requestUrl = 'api/Template/' + items[0].id;

        fetch(this.requestUrl, {
            method: "DELETE",
            headers: { 'authorization': 'Bearer ' + this.authHelper.getWebApiToken() }
        })
            .catch(error => console.error('Error:', error))
            .then(response => {
                if (response.ok) {
                    let currentItems = this.state.items.filter(x => x.id !== items[0].id);

                    this.setState({
                        items: currentItems,
                        itemsOriginal: currentItems,
                        MessagebarText: <Trans>dealTypeDeletedSuccess</Trans>,
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

    editTemplate(templateItem) {
        window.location = "/tab/generalAddTemplate?dealTypeId=" + templateItem.id;
    }

    render() {
        const { columns } = this.state;
        let showDeleteButton = this._selection.getSelection().length > 0 ? true : false;
        if (this.state.loading) {
            return (
                <div className='ms-BasicSpinnersExample ibox-content pt15 '>
                    <Spinner size={SpinnerSize.large} label={<Trans>loading</Trans>} ariaLive='assertive' />
                </div>
            );
        } else {
            return (
                this.accessGranted
                    ?
                    <div className='ms-Grid bg-white  p-10 ibox-content'>
                        <div className='ms-Grid-row'>
                            <div className='ms-Grid-col ms-sm12 ms-md12 ms-lg12 hide'>
                                <h2><Trans>dealTypeList</Trans></h2>
                            </div>
                        </div>
                        <div className='ms-Grid-row'>
                            <div className='ms-Grid-col ms-sm4 ms-md4 ms-lg2'>
                                <DefaultButton iconProps={{ iconName: 'Delete' }} className={showDeleteButton ? "" : "hide"} onClick={e => this.deleteTemplate(this._selection.getSelection())}>Delete</DefaultButton>
                            </div>
                            <div className='ms-Grid-col ms-sm6 ms-md6 ms-lg5'>
                                <div className='ms-BasicSpinnersExample'>
                                    {
                                        this.state.isUpdate ?
                                            <Spinner size={SpinnerSize.large} ariaLive='assertive' className='pull-left' />
                                            : ""
                                    }
                                    {
                                        this.state.isUpdateMsg ?
                                            <MessageBar
                                                messageBarType={this.state.MessageBarType}
                                                isMultiline={false}
                                                className='pull-left'
                                            >
                                                {this.state.MessagebarText}
                                            </MessageBar>
                                            : ""
                                    }
                                </div>
                            </div>
                            <div className='ms-Grid-col ms-sm12 ms-md6 ms-lg3 ml4percentage'>
                                <I18n>
                                    {
                                        t => {
                                            return (
                                                <SearchBox
                                                    placeholder={t('search')}
                                                    onChange={this._onFilterByTemplateNameChanged}
                                                />
                                            );
                                        }
                                    }
                                </I18n>
                            </div>
                            <div className='ms-Grid-col ms-sm12 ms-md6 ms-lg1'>
                                <LinkContainer to={'generalAddTemplate'} >
                                    <PrimaryButton iconProps={{ iconName: 'Add' }} >&nbsp;<Trans>add</Trans></PrimaryButton>
                                </LinkContainer>
                            </div>
                        </div>
                        <div className='ms-Grid-row LsitBoxAlign width102 '>
                            <div className='ms-Grid-col ms-sm12 ms-md12 ms-lg12'>
                                {
                                    this.state.items.length > 0
                                        ?
                                        <MarqueeSelection selection={this._selection}>
                                            <DetailsList
                                                componentRef={this._detailsList}
                                                items={this.state.items}
                                                columns={columns}
                                                setKey="set"
                                                layoutMode={DetailsListLayoutMode.fixedColumns}
                                                selection={this._selection}
                                                selectionPreservedOnEmptyClick={true}
                                                ariaLabelForSelectionColumn="Toggle selection"
                                                ariaLabelForSelectAllCheckbox="Toggle selection for all items"
                                                onItemInvoked={this._onItemInvoked}
                                                selectionMode={SelectionMode.single}
                                            />
                                        </MarqueeSelection>
                                        :
                                        <div><Trans>thereAreNoDealType</Trans></div>
                                }
                            </div>
                        </div>


                    </div>
                    :
                    <Accessdenied />
            );
        }
    }
}