. .\RegisterAppV2.ps1

function New-PMSharePointSite {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [string]$AdminSiteUrl,
        [Parameter(Mandatory = $true)]
        [string]$PMAdminUpn,
        [Parameter(Mandatory = $true)]
        [string]$PMSiteAlias,
        [Parameter(Mandatory = $true)]
        [string]$OfficeTenantName,
        [Parameter(Mandatory = $true)]
        [pscredential]$Credential,
        [Parameter(Mandatory = $false)]
        [switch]$Force
    )
    process {
        Connect-SPOService -Url $AdminSiteUrl -Credential $Credential
        try
        {
            $pmSiteUrl = Get-SPOSite -Identity "https://$OfficeTenantName.sharepoint.com/sites/$PMSiteAlias" -ErrorAction SilentlyContinue
        } catch { }
        if(!$pmSiteUrl)
        {
            # We open a PnP connection and do all we need in sequence to avoid having many connections open unnecessarily
            Connect-PnPOnline -Url $AdminSiteUrl -Credentials $Credential
            # First we create the site with the specified alias and store the url of the created site
            Write-Information "Creating the Proposal Manager SharePoint site..."
            $pmSiteUrl = New-PnPSite -Type TeamSite -Title "Proposal Management" -Alias $PMSiteAlias
            Write-Information "SharePoint site succesfully created"
            # And we immediately close the PnP connection
            Disconnect-PnPOnline

            # Afterwards, we use a regular SPO connection to make the desired PM admin the primary admin of the new site
            Write-Information "Setting the SharePoint site administrator..."
            Set-SPOUser -Site $pmSiteUrl -LoginName $PMAdminUpn -IsSiteCollectionAdmin $true
            Write-Information "SharePoint site administrator set correctly."
            Disconnect-SPOService
            return $pmSiteUrl
        }
        else
        {
            Disconnect-SPOService
            if($Force)
            {
                Write-Warning "SharePoint site for Proposal Manager already exists. The -Force flag was specified so the existing site will be used."
                return $pmSiteUrl
            }
            else
            {
                Write-Error "A SharePoint site with the name $PMSiteAlias already exists in tenant $OfficeTenantName. If you want to overwrite an existing installation of Proposal Manager, use the -Force flag."
            }
        }
    }
}

function New-PMGroupStructure {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [pscredential]$Credential,
        [Parameter(Mandatory = $false)]
        [switch]$Force
    )
    process {
        # Common attributes that should be applied to all Office 365 groups being created
        $groupsCommonAttributes = @{ }
        # Then we create the Office 365 groups corresponding to the PM roles (as the Getting Started guide asks)
        Write-Information "Connecting to Office 365 to create unified groups..."
        $exchangeSession = New-PSSession -ConfigurationName Microsoft.Exchange -ConnectionUri https://outlook.office365.com/powershell-liveid/ -Credential $Credential -Authentication Basic -AllowRedirection
        Import-PSSession $exchangeSession -DisableNameChecking
        Write-Information "Group creation has started"
        $groups = @(
            "Relationship Managers",
            "Loan Officers",
            "Legal Counsel",
            "Risk Officers",
            "Credit Analysts"
        )
        foreach($group in $groups)
        {
            Write-Information "Checking pre-existance of the $group group."
            if(!(Get-UnifiedGroup -Identity $group -ErrorAction SilentlyContinue))
            {
                Write-Information "$group group does not exist. Creating..."
                New-UnifiedGroup @groupsCommonAttributes -DisplayName $group
                Write-Information "$group group successfully created."
            }
            else
            {
                if($Force)
                {
                    Write-Warning "$group group already exists. The -Force flag was specified so the existing group will be used."
                }
                else
                {
                    Write-Error "A group with the name $group already exists in your tenant. If you want to overwrite an existing installation of Proposal Manager, use the -Force flag."
                }
            }
        }
        Write-Information "Group creation has been succesfully completed"
        Remove-PSSession $exchangeSession
    }
}

function New-PMSite {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [string]$PMSiteLocation,
        [Parameter(Mandatory = $true)]
        [string]$ApplicationName,
        [Parameter(Mandatory = $true)]
        [string]$Subscription,
        [Parameter(Mandatory = $false)]
        [switch]$IncludeProposalCreation,
        [Parameter(Mandatory = $false)]
        [switch]$Force
    )
    process {
        Write-Information "Starting resource group deployment in Azure..."
        Connect-AzureRmAccount -Subscription $Subscription
        $existingResourceGroup = Get-AzureRmResourceGroup -Name $ApplicationName -ErrorAction SilentlyContinue
        if($existingResourceGroup)
        {
            if($Force)
            {
                Write-Warning "$ApplicationName resource group already exists. The -Force flag was specified so the existing resource group will be overwritten."
                $existingResourceGroup | Remove-AzureRmResourceGroup -Force
                ipconfig /flushdns #Doing this to ensure old ip address is not used when redeploying from this machine
                Write-Information "The existing resource group was successfully deleted to be able to redeploy with the same resource group name."
            }
            else
            {
                Write-Error "A resource group with the name $ApplicationName already exists. If you want to overwrite an existing installation of Proposal Manager, use the -Force flag."
            }
        }
        New-AzureRmResourceGroup -Name $ApplicationName -Location $PMSiteLocation
        New-AzureRmResourceGroupDeployment -ResourceGroupName $ApplicationName -TemplateFile .\ProposalManagerARMTemplate.json `
            -siteName $ApplicationName -siteLocation $PMSiteLocation -includeProposalCreation $(if($IncludeProposalCreation) {$true} else {$false})
        Write-Information "Resource group deployment succeeded"
        Write-Information "Retrieving deployment credentials..."
        $xml = [xml](Get-AzureRmWebAppPublishingProfile -ResourceGroupName $ApplicationName -Name $ApplicationName -OutputFile .\settings.xml)
        # Extract connection information from publishing profile
        $username = [System.Linq.Enumerable]::Last($xml.SelectNodes("//publishProfile[@publishMethod=`"FTP`"]/@userName").value.Split('\'))
        $password = $xml.SelectNodes("//publishProfile[@publishMethod=`"FTP`"]/@userPWD").value
        $url = $xml.SelectNodes("//publishProfile[@publishMethod=`"FTP`"]/@publishUrl").value
        
        $returnedInformation = @{ Username = $username; Password = $password; Url = $url }
        if($IncludeProposalCreation)
        {
            $pcxml = [xml](Get-AzureRmWebAppPublishingProfile -ResourceGroupName $ApplicationName -Name "$ApplicationName-propcreation" -OutputFile .\settings-propcreation.xml)
            $pcusername = [System.Linq.Enumerable]::Last($pcxml.SelectNodes("//publishProfile[@publishMethod=`"FTP`"]/@userName").value.Split('\'))
            $pcpassword = $pcxml.SelectNodes("//publishProfile[@publishMethod=`"FTP`"]/@userPWD").value
            $pcurl = $pcxml.SelectNodes("//publishProfile[@publishMethod=`"FTP`"]/@publishUrl").value
            $returnedInformation.PCUsername = $pcusername
            $returnedInformation.PCPassword = $pcpassword
            $returnedInformation.PCUrl = $pcurl
        }
        Disconnect-AzureRmAccount
        Write-Information "Deployment credentials successfully retrieved"
        return $returnedInformation
    }
}

function New-PMTeamsAddInManifest {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [string]$AppUrl,
        [Parameter(Mandatory = $true)]
        [string]$AppDomain,
        [Parameter(Mandatory = $false)]
        [string]$BotId
    )
    process {
        Write-Information "Creating teams add-in package..."
        if(!$BotId)
        {
            $BotId = (New-Guid).ToString()
        }
        $manifest = (Get-Content ..\TeamsAddinPackage\manifest.json).
                    Replace('<addInId>', (New-Guid).ToString()).
                    Replace('<webAppUrl>', $AppUrl).
                    Replace('<botId>', $BotId).
                    Replace('<webDomain>', $AppDomain)
        if(Get-Item ProposalManager -ErrorAction SilentlyContinue)
        {
            rd ProposalManager -Recurse
        }
        md ProposalManager
        New-Item -Path .\ProposalManager\ -Name manifest.json -ItemType File
        Set-Content .\ProposalManager\manifest.json $manifest
        copy ..\TeamsAddinPackage\outline.png .\ProposalManager\outline.png
        copy ..\TeamsAddinPackage\color.png .\ProposalManager\color.png
        Compress-Archive .\ProposalManager\* .\ProposalManager.zip -CompressionLevel Fastest -Force
        rd ProposalManager -Recurse
        Write-Information "Package created successfully"
    }
}

function New-PMBot {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [string]$Subscription,
        [Parameter(Mandatory = $true)]
        [string]$ApplicationName,
        [Parameter(Mandatory = $true)]
        [pscredential]$Credential,
        [Parameter(Mandatory = $true)]
        [string]$AppId,
        [Parameter(Mandatory = $true)]
        [string]$AppSecret
    )
    process {
        Write-Information "Beginning bot registration..."
        az login -u $Credential.UserName -p $Credential.GetNetworkCredential().Password
        az account set -s $Subscription
        $botJson = az bot create -k registration -v v3 -n $ApplicationName -g $ApplicationName --appid $AppId -p $AppSecret -e https://smba.trafficmanager.net/amer-client-ss.msg/
        $bot = $botJson | ConvertFrom-Json
        az bot msteams create -n $bot.name -g $bot.resourceGroup
        az logout
        return $bot
    }
}