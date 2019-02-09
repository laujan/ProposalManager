function RegisterApp {
    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory=$false)]
        [string]$ApplicationName,
        [Parameter(Mandatory = $true)]
        [pscredential]$Credential,
        [Parameter(Mandatory=$false)]
        [string[]]$RelativeReplyUrls,
        [Parameter(Mandatory=$false)]
        [string[]]$DelegatedPermissions,
        [Parameter(Mandatory=$false)]
        [string[]]$ApplicationPermissions,
        [Parameter(Mandatory=$false)]
        [string[]]$AdditionalPreAuthorizedAppIds,
        [Parameter(Mandatory=$false)]
        [string]$AddTo
    )

    if(!$RelativeReplyUrls)
    {
        $RelativeReplyUrls = @('Setup', 'tab/config', 'tab/tabauth', 'tab', [string]::Empty)
    }

    Connect-AzureAD -Credential $Credential

    # ADAL JSON token - necessary for making requests to Graph API
    $token = GetAuthToken -Credential $Credential

    $secretGuid = New-Guid
    $guidBytes = [System.Text.Encoding]::UTF8.GetBytes($secretGuid)
    $secretText =[System.Convert]::ToBase64String($guidBytes)
    $clientSecret = @{
        'endDateTime'=[DateTime]::UtcNow.AddDays(365).ToString('u').Replace(' ', 'T');
        'keyId'=$secretGuid;
        'hint'=$secretGuid;
        'startDateTime'=[DateTime]::UtcNow.AddDays(-1).ToString('u').Replace(' ', 'T');  
        'secretText'=$secretText;
    }

    $replyUrls = $RelativeReplyUrls | % { "https://$ApplicationName.azurewebsites.net/" + $_ } | ConvertTo-Json
    Write-Debug $replyUrls

    # REST API header with auth token
    $authHeader = @{
        'Content-Type'='application/json';
        'Authorization'=$token.CreateAuthorizationHeader()
    }

    $data = "{
            `"displayName`": `"$ApplicationName`",
            `"passwordCredentials`": [$(ConvertTo-Json -InputObject $clientSecret)],
            `"web`": {
                `"logoutUrl`": `"https://$ApplicationName.azurewebsites.net`",
                `"redirectUris`": $replyUrls,
                `"implicitGrantSettings`": {
                    `"enableIdTokenIssuance`": true,
                    `"enableAccessTokenIssuance`": true
            }
        }
    }";

    Write-Information "Creating application $ApplicationName"
    $uri = "https://graph.microsoft.com/beta/applications"
    $result = Invoke-RestMethod -Uri $uri -Headers $authHeader -Body $data -Method POST

    $appId = $result.appId
    # Add permissions
    $scopeGuid = New-Guid
    $oauthPerms = "{
        `"adminConsentDescription`": `"Allow the client application to access Proposal Manager WebApi on behalf of the signed-in user.`",
        `"adminConsentDisplayName`": `"Access Proposal Manager WebApi`",
        `"id`": `"$scopeGuid`",
        `"isEnabled`": true,
        `"type`": `"User`",
        `"userConsentDescription`": `"Allow the client application to access Proposal Manager WebApi on your behalf.`",
        `"userConsentDisplayName`": `"Access Proposal Manager WebApi`",
        `"value`": `"access_as_user`"
    }"

    $api = "{
        `"requestedAccessTokenVersion`": 2,
        `"oauth2PermissionScopes`": [ $oauthPerms ]
    }"

    Write-Information "Setting oauth2 Permissions"
    $update = "{
        `"identifierUris`": [`"api://$appId`"],
        `"api`": $api
    }"

    Invoke-RestMethod -Uri "$($uri)/$($result.id)" -Headers $authHeader -Body $update -Method PATCH

    Write-Information "Setting Graph Permissions"

    if(!$DelegatedPermissions)
    {
        $DelegatedPermissions =
            '64a6cdd6-aab1-4aaf-94b8-3cc8405e90d0',
            '7427e0e9-2fba-42fe-b0c0-848c9e6a8182',
            '37f7f235-527c-4136-accd-4a02d197296e',
            '14dad69e-099b-42c9-810b-d002981feec1',
            'e1fe6dd8-ba31-4d61-89e7-88639da4683d'
    }

    if(!$ApplicationPermissions)
    {
        $ApplicationPermissions =
            '75359482-378d-4052-8f01-80520e7db3cd',
            '62a82d76-70ea-41e2-9197-370581804d09',
            '0c0bf378-bf22-4481-8f81-9e89a9b4960a',
            '741f803b-c850-494e-b5df-cde7c675a1ca'
    }

    $ofType = [System.Linq.Enumerable].GetMethod("OfType").MakeGenericMethod([System.Object]);
    
    #Delegated: Scope
    #Application: Role
    $graphPermissions = ConvertTo-Json (
        ($ApplicationPermissions | % { @{ id = $_ ; type = 'Role' } }) +
        ($DelegatedPermissions | % { @{ id = $_ ; type = 'Scope' } }))
        

    $requiredResourceAccess = "[
        {
          `"resourceAppId`": `"00000003-0000-0000-c000-000000000000`",
          `"resourceAccess`": $graphPermissions
        },
        {
            `"resourceAppId`": `"$appId`",
            `"resourceAccess`": [
                {
                    `"id`": `"$scopeGuid`",
                    `"type`": `"Scope`"
                }
            ]
        }
    ]"

    $updatePerm = "{
        `"requiredResourceAccess`": $requiredResourceAccess
    }"

    Write-Debug $updatePerm

    Invoke-RestMethod -Uri "$($uri)/$($result.id)" -Headers $authHeader -Body $updatePerm -Method PATCH

    Write-Information "Add PreAuthorized apps"
    # Add PreAuthorized applications
    $preAuthorizedApps = "[ {
        `"appId`": `"$appId`",
        `"permissionIds`": [ `"$scopeGuid`" ]
    }]"

    if($AdditionalPreAuthorizedAppIds)
    {
        $parsedPreAuthorizedApps = $preAuthorizedApps | ConvertFrom-Json
        $preAuthorizedApps = $parsedPreAuthorizedApps + ($AdditionalPreAuthorizedAppIds | % { @{ appId = $_ ; permissionIds = , $scopeGuid } }) | ConvertTo-Json
    }

    $addPreAuth = "{
    `"api`": { 
            `"preAuthorizedApplications`": $preAuthorizedApps
        }
    }"

    Invoke-RestMethod -Uri "$($uri)/$($result.id)" -Headers $authHeader -Body $addPreAuth -Method PATCH

    if($AddTo)
    {

        $resultAddTo = Invoke-RestMethod -Uri "$($uri)/?filter=appId eq '$AddTo'" -Headers $authHeader

        $authScope = $resultAddTo.value.api.oauth2PermissionScopes[0].id

        $newPreAuthorized = $resultAddTo.value.api.preAuthorizedApplications + @{ appId = $appId; permissionIds = @(, $authScope)}
                
        $addPreAuth = "{
        `"api`": { 
                `"preAuthorizedApplications`": $($newPreAuthorized | ConvertTo-Json)
            }
        }"

        Invoke-RestMethod -Uri "$($uri)/$($resultAddTo.value.id)" -Headers $authHeader -Body $addPreAuth -Method PATCH
    }

    Disconnect-AzureAD

    Write-Information "The application $ApplicationName has been successfully registered"

    return @{ AppId = $appId; AppSecret = $secretText }
}

Function GetAuthToken
{
    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $true)]
        [pscredential]$Credential
    )
    Import-Module Azure
    $clientId = "1950a258-227b-4e31-a9cf-717495945fc2" 
    $redirectUri = "urn:ietf:wg:oauth:2.0:oob"
    $resourceAppIdURI = "https://graph.microsoft.com"
    $authority = "https://login.microsoftonline.com/common"
    $authContext = New-Object "Microsoft.IdentityModel.Clients.ActiveDirectory.AuthenticationContext" -ArgumentList $authority
    $AADCredential = New-Object "Microsoft.IdentityModel.Clients.ActiveDirectory.UserCredential" -ArgumentList $credential.UserName,$credential.Password
    $authResult = $authContext.AcquireToken($resourceAppIdURI, $clientId,$AADCredential)
    return $authResult
}