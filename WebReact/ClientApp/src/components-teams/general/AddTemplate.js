/* 
*  Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. 
*  See LICENSE in the source repository root for complete license information. 
*/

import React, { Component } from 'react';
import { IconButton } from 'office-ui-fabric-react/lib/Button';

export class AddTemplate extends Component {
    displayName = AddTemplate.name

    constructor(props) {
        super(props);
    }

    render() {
        return (
            <div>
                {
                    this.props.templatesObj.map((template, idx) =>
                        <div className="ms-Grid-col ms-sm4 ms-md4 ms-lg4 p15" key={idx}>
                            <div className="ms-Grid-row bg-grey ms-borderBase">
                                <div className="ms-Grid-col ms-sm6 ms-md6 ms-lg12 bg-white">
                                    <IconButton iconProps={{ iconName: 'Add' }} className={template.selected ? "hide" : ""} />
                                    <IconButton iconProps={{ iconName: 'Accept' }} className={template.selected ? "" : "hide"} />
                                </div>
                                <div className="ms-Grid-col ms-sm6 ms-md6 ms-lg12">
                                    <h4>{template.name}</h4>
                                </div>
                            </div>
                            
                        </div>

                    )
                }
            </div>

            );
    }
}