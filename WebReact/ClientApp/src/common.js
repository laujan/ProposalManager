Object.defineProperty(exports, "__esModule", { value: true });
exports.oppStatus = ['Not Started',
	'In Progress',
	'Blocked',
	'Completed'
];

/* Dashboard list */
exports.oppStatusText = [
	'None Empty',
	'Creating',
	'In Progress',
	'Assigned',
	'Draft',
	'Not Started',
	'In Review',
	'Blocked',
	'Completed',
	'Submitted',
	'Accepted',
	'Archived'
];

exports.oppStatusClassName = [
	'NoneEmpty',
	'Creating',
	'InProgress',
	'Assigned',
	'Draft',
	'NotStarted',
	'InReview',
	'Blocked',
	'Completed',
	'Submitted',
	'Accepted',
	'Archived'
];

exports.oppStatusTextOld = [{
	'NotStarted': 'Not Started',
	'InProgress': 'In Progress',
	'Blocked': 'Blocked',
	'Completed': 'Completed'
}];

exports.channels = [
	{
		name: "Risk Assessment",
		description: "Risk Assessment channel"
	},
	{
		name: "Credit Check",
		description: "Credit Check channel"
	},
	{
		name: "Compliance",
		description: "Compliance channel"
	},
	{
		name: "Formal Proposal",
		description: "Formal Proposal channel"
	},
	{
		name: "Customer Decision",
		description: "Customer Decision channel"
	}
];



exports.userRoles = ['Loan Officer', 'Relationship Manager', 'Credit Analyst', 'Legal Counsel', 'Senior Risk Officer'];

// Get the value of query parameter
exports.getQueryVariable = (variable) => {
	const query = window.location.search.substring(1);
	const vars = query.split('&');
	for (const varPairs of vars) {
		const pair = varPairs.split('=');
		if (decodeURIComponent(pair[0]) === variable) {
			return decodeURIComponent(pair[1]);
		}
	}
	return null;
};

// Permissions Object
exports.getAllPemissionTypes = () => {
	let requestUrl = 'api/Permissions';
	try {
		fetch(requestUrl, {
			method: "GET",
			headers: { 'authorization': 'Bearer ' + window.authHelper.getWebApiToken() }
		})
			.then(response => response.json())
			.then(data => {
				try {
					let allPermissions = data;
					let permissionsList = [];
					for (let i = 0; i < allPermissions.length; i++) {
						let item = {};
						//item.id = allPermissions[i].id;
						item.name = allPermissions[i].name;
						permissionsList.push(item);
					}
					console.log("Config load permissions");
					console.log(permissionsList);
					//this.setState({ permissionTypes: permissionsList });

				}
				catch (err) {
					console.log(err);
				}

			});
	} catch (err) {
		console.log(err);
	}
}

exports.permissions = [
	{
		"value": 0,
		"name": "Opportunity_Create"
	},
	{
		"value": 1,
		"name": "Opportunity_Read_All"
	},
	{
		"value": 2,
		"name": "Opportunity_ReadWrite_All"
	},
	{
		"value": 3,
		"name": "Opportunity_Read_Partial"
	},
	{
		"value": 4,
		"name": "Opportunity_ReadWrite_Partial"
	},
	{
		"value": 5,
		"name": "Opportunities_Read_All"
	},
	{
		"value": 6,
		"name": "Opportunities_ReadWrite_All"
	},
	{
		"value": 7,
		"name": "Opportunity_ReadWrite_Team"
	},
	{
		"value": 8,
		"name": "Opportunity_ReadWrite_Dealtype"
	},
	{
		"value": 9,
		"name": "Administrator"
	},
	{
		"value": 10,
		"name": "CustomerDecision_Read"
	},
	{
		"value": 11,
		"name": "CustomerDecision_ReadWrite"
	},
	{
		"value": 12,
		"name": "CreditCheck_Read"
	},
	{
		"value": 13,
		"name": "CreditCheck_ReadWrite"
	},
	{
		"value": 14,
		"name": "Compliance_Read"
	},
	{
		"value": 15,
		"name": "Compliance_ReadWrite"
	},
	{
		"value": 16,
		"name": "RiskAssessement_Read"
	},
	{
		"value": 17,
		"name": "RiskAssessement_ReadWrite"
	},
	{
		"value": 18,
		"name": "ProposalDocument_Read"
	},
	{
		"value": 19,
		"name": "ProposalDocument_ReadWrite"
	}
];

// check permission exist
exports.isPermissionExist = (permissionArry) => {
	console.log(permissionArry);
	for (let p = 0; p < permissionArry.length; p++) {
		return exports.permissions.some(function (el) {
			return el.value === permissionArry[p];
		});
	}
	return false;
};

exports.userPermissionsAll = (permissionArray) => {
	console.log(permissionArray);
	//let userPermissionArray = exports.permissions;
	let userPermissionArray = exports.getAllPemissionTypes();
	let userPermissionObj = {};
	for (let l = 0; l < exports.permissions.length; l++) {
		// check value exist
		let roleExist = permissionArray.some(function (e) {
			return e === userPermissionArray[l].name;
		});
		if (roleExist) {
			//exports.permissions[l].hasAccess = true;
			let pText = userPermissionArray[l].name;
			userPermissionObj[pText] = true;

		} else {
			let pText = userPermissionArray[l].name;
			userPermissionObj[pText] = false;
		}

	}

	return userPermissionObj;
};

exports.checkAccess = (userPermissions, checkRole) => {
    //return false;
    //if ((userPermissions.filter(x => x.name.toLowerCase() === checkRole.toLowerCase())).length > 0) {
    if ((userPermissions.filter(x => x.toLowerCase() === checkRole.toLowerCase())).length > 0) {
        return true;
    } else return false;
};