Function RegisterApp {
    [CmdletBinding()]
    param
        (
         [Parameter(Mandatory=$false)]
        $applicationName
        )

    Connect-AzureAD -Credential $global:credential

    # ADAL JSON token - necessary for making requests to Graph API
    $token = GetAuthToken

    $secretGuid = New-Guid
    $guidBytes = [System.Text.Encoding]::UTF8.GetBytes($secretGuid)
    $global:secretText =[System.Convert]::ToBase64String($guidBytes)
    $clientSecret = @{
        'endDateTime'=[DateTime]::UtcNow.AddDays(365).ToString('u').Replace(' ', 'T');
        'keyId'=$secretGuid;
        'hint'=$secretGuid;
        'startDateTime'=[DateTime]::UtcNow.AddDays(-1).ToString('u').Replace(' ', 'T');  
        'secretText'=$global:secretText;
    }

    $replyUrls = "[
        `"https://$applicationname.azurewebsites.net/Setup`",
        `"https://$applicationname.azurewebsites.net/tab/config`",
        `"https://$applicationname.azurewebsites.net/tab/tabauth`",
        `"https://$applicationname.azurewebsites.net/tab`",
        `"https://$applicationname.azurewebsites.net`"
      ]
      "

    # REST API header with auth token
    $authHeader = @{
        'Content-Type'='application/json';
        'Authorization'=$token.CreateAuthorizationHeader()
    }

    $data = "{
            `"displayName`": `"$applicationName`",
            `"passwordCredentials`": [$(ConvertTo-Json -InputObject $clientSecret)],
            `"web`": {
                `"logoutUrl`": `"https://$applicationname.azurewebsites.net`",
                `"redirectUris`": $replyUrls,
                `"implicitGrantSettings`": {
                    `"enableIdTokenIssuance`": true,
                    `"enableAccessTokenIssuance`": true
            }
        }
    }";

    Write-Host "Creating application $applicationName"
    $uri = "https://graph.microsoft.com/beta/applications"
    $result = Invoke-RestMethod -Uri $uri -Headers $authHeader -Body $data -Method POST

    $global:appId = $result.appId
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

    Write-Host "Setting oauth2 Permissions"
    $update = "{
        `"identifierUris`": [`"api://$global:appId`"],
        `"api`": $api
    }"

    Invoke-RestMethod -Uri "$($uri)/$($result.id)" -Headers $authHeader -Body $update -Method PATCH

    Write-Host "Setting Graph Permissions"
    #Delegated: Scope
    #Application: Role
    $requiredResourceAccess = "[
        {
          `"resourceAppId`": `"00000003-0000-0000-c000-000000000000`",
          `"resourceAccess`": [
            {
              `"id`": `"64a6cdd6-aab1-4aaf-94b8-3cc8405e90d0`", 
              `"type`": `"Scope`"
            },
            {
              `"id`": `"7427e0e9-2fba-42fe-b0c0-848c9e6a8182`",
              `"type`": `"Scope`"
            },
            {
              `"id`": `"37f7f235-527c-4136-accd-4a02d197296e`",
              `"type`": `"Scope`"
            },
            {
              `"id`": `"14dad69e-099b-42c9-810b-d002981feec1`",
              `"type`": `"Scope`"
            },
            {
              `"id`": `"e1fe6dd8-ba31-4d61-89e7-88639da4683d`",
              `"type`": `"Scope`"
            },
            {
              `"id`": `"75359482-378d-4052-8f01-80520e7db3cd`",
              `"type`": `"Role`"
            },
            {
              `"id`": `"62a82d76-70ea-41e2-9197-370581804d09`",
              `"type`": `"Role`"
            },
            {
              `"id`": `"0c0bf378-bf22-4481-8f81-9e89a9b4960a`",
              `"type`": `"Role`"
            },
            {
              `"id`": `"741f803b-c850-494e-b5df-cde7c675a1ca`",
              `"type`": `"Role`"
            }
          ]
        },
        {
            `"resourceAppId`": `"$global:appId`",
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

    Invoke-RestMethod -Uri "$($uri)/$($result.id)" -Headers $authHeader -Body $updatePerm -Method PATCH

    Write-Host "Add PreAuthorized apps"
    # Add PreAuthorized applications
    $preAuthorizedApps = "[ {
        `"appId`": `"$global:appId`",
        `"permissionIds`": [ `"$scopeGuid`" ]
    }]"

    $addPreAuth = "{
    `"api`": { 
            `"preAuthorizedApplications`": $preAuthorizedApps
        }
    }"

    Invoke-RestMethod -Uri "$($uri)/$($result.id)" -Headers $authHeader -Body $addPreAuth -Method PATCH

    Disconnect-AzureAD

    Write-Host "The application $applicationName has been successfully registered"
}

Function GetAuthToken
{
    Import-Module Azure
    $clientId = "1950a258-227b-4e31-a9cf-717495945fc2" 
    $redirectUri = "urn:ietf:wg:oauth:2.0:oob"
    $resourceAppIdURI = "https://graph.microsoft.com"
    $authority = "https://login.microsoftonline.com/common"
    $authContext = New-Object "Microsoft.IdentityModel.Clients.ActiveDirectory.AuthenticationContext" -ArgumentList $authority
    $Credential = $global:credential
    $AADCredential = New-Object "Microsoft.IdentityModel.Clients.ActiveDirectory.UserCredential" -ArgumentList $credential.UserName,$credential.Password
    $authResult = $authContext.AcquireToken($resourceAppIdURI, $clientId,$AADCredential)
    return $authResult
}