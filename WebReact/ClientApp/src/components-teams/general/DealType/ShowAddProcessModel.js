/* 
*  Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. 
*  See LICENSE in the source repository root for complete license information. 
*/

/* eslint-disable radix */

import React from 'react';
import { PrimaryButton, IconButton, ActionButton } from 'office-ui-fabric-react/lib/Button';
import { Trans } from "react-i18next";
import { Modal } from 'office-ui-fabric-react/lib/Modal';
import { TextField } from 'office-ui-fabric-react/lib/TextField';
import { Checkbox } from 'office-ui-fabric-react/lib/Checkbox';
import { Label } from 'office-ui-fabric-react/lib/Label';
import { Dropdown } from 'office-ui-fabric-react/lib/Dropdown';

export default function ShowAddProcessModel() {
    console.log("ShowAddProcessModel==>,", this.state);
    return (
        <Modal isOpen={this.state.showModel}
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
                    <div className='ms-Grid-col ms-sm12 ms-md6 ms-lg7'>
                        <div className="ms-Grid-row bg-white">
                            <div className="ms-Grid-col ms-sm6 ms-md6 ms-lg12 p15">
                                {this.showMessageBar(this.state.isProcessExist, this.state.processAlreadyExistErrText)}
                            </div>
                        </div>
                        <div className="ms-Grid-row bg-white">
                            {
                                this.state.processList.map((process, idx) => {
                                    if (!this.processTypesNotToDisplay.includes(process.processType))
                                        return (
                                            <div className="ms-Grid-col ms-sm10 ms-md6 ms-lg4 p15" key={idx}>
                                                <div className="ms-Grid-row bg-grey GrayBorder text-center">
                                                    <div className="ms-Grid-col ms-sm12 ms-md6 ms-lg12 bg-white">
                                                        <IconButton
                                                            iconProps={{ iconName: 'Add' }}
                                                            onClick={this.addProcess.bind(this, process)} className={process.selected ? "hide" : ""}
                                                        />
                                                    </div>
                                                    <div className="ms-Grid-col ms-sm12 ms-md6 ms-lg12 purpleBG text-center">
                                                        <h5>{process.processStep}</h5>
                                                    </div>
                                                </div>

                                            </div>
                                        );
                                })
                            }
                        </div>
                    </div>
                    <div className='ms-Grid-col ms-sm12 ms-md6 ms-lg7'>
                        <div className="ms-Grid-row bg-white">
                            <Dropdown
                                label="Select Process Group Number"
                                onChanged={(e) => this.setProcessGroupNumberNo(e)}
                                disabled={this.state.disableGroupListDropDown}
                                id='processgrouplistdropdown'
                                options={this.state.processGroupNumberList.map(no => { return { key: no, text: `Group No ${no}` }; })}
                            />
                        </div>
                    </div>
                    <div className='ms-Grid-col ms-sm12 ms-md6 ms-lg5 CheckBoxWidth p-l-30'>
                        <Trans>Selected</Trans>
                        <Checkbox
                            label={<Trans>enableOrdering</Trans>}
                            onChange={this.enableOrdering.bind(this)}
                            defaultChecked={this.state.enbaleOrderingBtwnProcess}
                        />

                        {
                            this.state.selectedProcessGroup.map((process) => {
                                let idx = parseInt((process.order + "").split(".")[1]);
                                return (
                                    <div className="ms-Grid-row p-10 " key={idx}>
                                        <div className="ms-Grid-col ms-sm1 ms-md1 ms-lg1 ">
                                            <Label>{(process.order + "").split(".")[1]}</Label>
                                        </div>
                                        <div className="ms-Grid-col ms-sm6 ms-md6 ms-lg8 processBg">
                                            <div className="ms-Grid-row ">
                                                <div className="ms-Grid-col ms-sm6 ms-md6 ms-lg8">
                                                    <h5 className="font12 font-normal">{process.processStep}</h5>
                                                </div>
                                                <div className="ms-Grid-col ms-sm6 ms-md6 ms-lg4 ">
                                                    <IconButton iconProps={{ iconName: 'remove' }} className="pull-right" onClick={this.removeProcess.bind(this, process)} />
                                                </div>
                                            </div>
                                            <div className="ms-Grid-row processBg pb15">
                                                <div className="ms-Grid-col ms-sm6 ms-md6 ms-lg6 font10">
                                                    <Trans>estimateDays</Trans>
                                                    <TextField
                                                        className="textboxSize"
                                                        value={process.daysEstimate}
                                                        onBlur={this.onBlurEstimatedDays.bind(this, process)}
                                                    />
                                                </div>
                                            </div>

                                        </div>
                                        <div className="ms-Grid-col ms-sm6 ms-md6 ms-lg3 ">
                                            {this.state.selectedProcessGroup.length > 1 ? idx === 1 ?
                                                <ActionButton
                                                    onClick={this.swapProcess.bind(this, process, idx, "DOWN")}
                                                    disabled={!this.state.enbaleOrderingBtwnProcess}
                                                >
                                                    <i className="ms-Icon ms-Icon--SortDown f20" aria-hidden="true" />
                                                </ActionButton>
                                                : this.state.selectedProcessGroup.length === idx ?
                                                    <ActionButton
                                                        onClick={this.swapProcess.bind(this, process, idx, "UP")}
                                                        disabled={!this.state.enbaleOrderingBtwnProcess}
                                                    >
                                                        <i className="ms-Icon ms-Icon--SortUp f20" aria-hidden="true" />
                                                    </ActionButton>
                                                    :
                                                    <div>
                                                        <ActionButton
                                                            onClick={this.swapProcess.bind(this, process, idx, "UP")}
                                                            disabled={!this.state.enbaleOrderingBtwnProcess}
                                                        >
                                                            <i className="ms-Icon ms-Icon--SortUp f20" aria-hidden="true" />
                                                        </ActionButton><br /><br />
                                                        <ActionButton
                                                            onClick={this.swapProcess.bind(this, process, idx, "DOWN")}
                                                            disabled={!this.state.enbaleOrderingBtwnProcess}
                                                        >
                                                            <i className="ms-Icon ms-Icon--SortDown f20" aria-hidden="true" />
                                                        </ActionButton>
                                                    </div>
                                                : null}
                                        </div>
                                    </div>
                                );
                            })
                        }
                        <div className="ms-Grid-row p-10 ">
                            <PrimaryButton text={<Trans>save</Trans>}
                                onClick={this.saveGroupWithProcess.bind(this, this.state.selectedProcessGroup)}
                                disabled={this.state.selectedProcessGroup.length > 0 ? false : true}
                            />
                        </div>
                    </div>
                </div>
            </div>
        </Modal>);
}