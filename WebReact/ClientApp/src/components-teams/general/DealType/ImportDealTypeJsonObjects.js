/*
*  Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license.
*  See LICENSE in the source repository root for complete license information.
*/

function getRandomInt(max) {
    return Math.floor(Math.random() * Math.floor(max));
};

export const getAdminRoleObject = (UserProfile,action="normal") =>{
    let permissions = [];

    if(action === "normal"){
        permissions = [
            {
                "id": "",
                "name": "Administrator"
            },
            {
                "id": "",
                "name": "Opportunity_ReadWrite_All"
            }
        ];
    }else{
        permissions = [
            {
                "id": "",
                "name": "Administrator"
            },
            {
                "id": "",
                "name": "Opportunity_Create"
            },
            {
                "id": "",
                "name": "Opportunity_ReadWrite_Dealtype"
            },
            {
                "id": "",
                "name": "Opportunity_ReadWrite_Team"
            },
            {
                "id": "",
                "name": "Opportunities_ReadWrite_All"
            }
        ]
    };

    return {
        "id": "1",
        "displayName": "Administrator",
        "adGroupName": UserProfile.roles[0].adGroupName,
        "permissions": permissions,
        "teamsMembership": {
            "name": "Owner",
            "value": 0
        },
        "selPermissions": [
            "Administrator",
            "Opportunity_Create",
            "Opportunity_ReadWrite_Dealtype",
            "Opportunity_ReadWrite_Team"
        ]
    };
};

export const getTemplateObject = (dealTypeJson) =>{
   return  {
        "id": "",
        "templateName": dealTypeJson.template.templateName,
        "description": "",
        "processes": dealTypeJson.template.processes.map(process => {
            return {
                "id": "",
                "processStep": process.processStep,
                "channel": process.channel,
                "processType": process.processType,
                "roleName": process.roleName,
                "roleId": process.roleId,
                "order": process.order,
                "status": 0,
                "daysEstimate": 0
            }
        }),
        "defaultTemplate": false,
        "initilaltemplate": false
    };
};

export const getRoleMappingObject = (role) =>{
    return {
        "adGroupName": role.adGroupName,
        "displayName": role.displayName,
        "id": role.id,
        "permissions": role.permissions,
        "teamsMembership": role.teamsMembership
    };
};

export const getProcessObject = (process) =>{
    return {
        "id": "",
        "processStep": process.processStep,
        "channel": process.channel,
        "processType": process.processType,
        "roleName": process.roleName,
        "roleId": process.roleId,
        "isDisable": false
    };
};

export const  getOpportuity = (templateName,UserProfile) => {
    let today = new Date();
    let dd = today.getDate();
    let mm = today.getMonth() + 1; //January is 0!

    let yyyy = today.getFullYear();
    if (dd < 10) {
        dd = '0' + dd;  
    } 
    if (mm < 10) {
        mm = '0' + mm;
    } 
    today = mm + '/' + dd + '/' + yyyy;
    let number = getRandomInt(1000);
    let displayName = `Test ${templateName}Team ${number}`;
    let opportunity = {
        "id": ``,
        "displayName": displayName,
        "customer": {
            "id": "",
            "displayName": displayName,
            "referenceId": ""
        },
        "metaDataFields": [
            {
                "id": "customer",
                "displayName": "Customer",
                "values": "Test Customer",
                "screen": "Screen1",
                "fieldType": {
                    "name": "String",
                    "value": 0
                }
            },
            {
                "id": "opportunity",
                "displayName": "Opportunity",
                "values": displayName,
                "screen": "Screen1",
                "fieldType": {
                    "name": "String",
                    "value": 0
                }
            },
            {
                "id": "openeddate",
                "displayName": "Opened Date",
                "values": today.toString(),
                "screen": "Screen1",
                "fieldType": {
                    "name": "Date",
                    "value": 2
                }
            }
        ],
        "teamMembers": [{
            "status": 0,
            "id": UserProfile.id,
            "displayName": UserProfile.displayName,
            "mail": UserProfile.mail,
            "userPrincipalName": UserProfile.userPrincipalName,
            "roleId": UserProfile.roles[0].id,
            "permissions": UserProfile.roles[0].permissions,
            "teamsMembership": UserProfile.roles[0].teamsMembership,
            "ProcessStep": "Start Process",
            "roleName": UserProfile.roles[0].displayName,
            "adGroupName": UserProfile.roles[0].adGroupName
        }],
        "notes": [],
        "documentAttachments": [
            {
                "id": "",
                "note": "",
                "category": {
                    "id": ""
                },
                "tags": "",
                "documentUri": ""
            }
        ],
        "template": {}
    };
    return opportunity;
}