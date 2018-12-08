function New-PMSharePointSite {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [string]$AdminSiteUrl,
        [Parameter(Mandatory = $true)]
        [string]$PMAdminUpn,
        [Parameter(Mandatory = $true)]
        [string]$PMSiteAlias
    )
    process {
        $Credential = $global:credential
        # We open a PnP connection and do all we need in sequence to avoid having many connections open unnecessarily
        Connect-PnPOnline -Url $AdminSiteUrl -Credentials $Credential
        # First we create the site with the specified alias and store the url of the created site
        Write-Host "Creating the Proposal Manager SharePoint site..."
        $pmSiteUrl = New-PnPSite -Type TeamSite -Title "Proposal Management" -Alias $PMSiteAlias
        Write-Host "SharePoint site succesfully created" -ForegroundColor Green
        # And we immediately close the PnP connection
        Disconnect-PnPOnline

        # Afterwards, we open a regular SPO connection to make the desired PM admin the primary admin of the new site
        Connect-SPOService -Url $AdminSiteUrl -Credential $Credential
        Write-Host "Setting the SharePoint site administrator..."
        Set-SPOUser -Site $pmSiteUrl -LoginName $PMAdminUpn -IsSiteCollectionAdmin $true
        Write-Host "SharePoint site administrator set correctly." -ForegroundColor Green
        Disconnect-SPOService
        return $pmSiteUrl
    }
}

function New-PMGroupStructure {
    [CmdletBinding()]
    param()
    process {
        # Common attributes that should be applied to all Office 365 groups being created
        $groupsCommonAttributes = @{ }
        # Then we create the Office 365 groups corresponding to the PM roles (as the Getting Started guide asks)
        Write-Host "Connecting to Office 365 to create unified groups..."
        $exchangeSession = New-PSSession -ConfigurationName Microsoft.Exchange -ConnectionUri https://outlook.office365.com/powershell-liveid/ -Credential $global:credential -Authentication Basic -AllowRedirection
        Import-PSSession $exchangeSession -DisableNameChecking
        Write-Host "Group creation has started" -ForegroundColor Cyan
        New-UnifiedGroup @groupsCommonAttributes -DisplayName "Relationship Managers"
        New-UnifiedGroup @groupsCommonAttributes -DisplayName "Loan Officers"
        New-UnifiedGroup @groupsCommonAttributes -DisplayName "Legal Counsel"
        New-UnifiedGroup @groupsCommonAttributes -DisplayName "Risk Officers"
        New-UnifiedGroup @groupsCommonAttributes -DisplayName "Credit Analysts"
        Write-Host "Group creation has been succesfully completed" -ForegroundColor Green
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
        [string]$Subscription
    )
    process {
        Write-Host "Starting resource group deployment in Azure..." -ForegroundColor Cyan
        Connect-AzureRmAccount -Subscription $Subscription
        New-AzureRmResourceGroup -Name $ApplicationName -Location $PMSiteLocation
        New-AzureRmResourceGroupDeployment -ResourceGroupName $ApplicationName -TemplateFile .\ProposalManagerARMTemplate.json -siteName $ApplicationName -siteLocation $PMSiteLocation
        Write-Host "Resource group deployment succeeded" -ForegroundColor Green
        Write-Host "Retrieving deployment credentials..." -ForegroundColor Cyan
        $xml = [xml](Get-AzureRmWebAppPublishingProfile -ResourceGroupName $ApplicationName -Name $ApplicationName -OutputFile .\settings.xml)
        # Extract connection information from publishing profile
        $username = [System.Linq.Enumerable]::Last($xml.SelectNodes("//publishProfile[@publishMethod=`"FTP`"]/@userName").value.Split('\'))
        $password = $xml.SelectNodes("//publishProfile[@publishMethod=`"FTP`"]/@userPWD").value
        $url = $xml.SelectNodes("//publishProfile[@publishMethod=`"FTP`"]/@publishUrl").value
        Disconnect-AzureRmAccount
        Write-Host "Deployment credentials successfully retrieved" -ForegroundColor Cyan
        return @{Username = $username; Password = $password; Url = $url}
    }
}

function New-PMTeamsAddInManifest {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [string]$AppUrl,
        [Parameter(Mandatory = $true)]
        [string]$AppDomain
    )
    process {
        Write-Host "Creating teams add-in package..." -ForegroundColor Cyan
        $manifest = (Get-Content ..\TeamsAddinPackage\manifest.json).
                    Replace('<addInId>', [System.Guid]::NewGuid().ToString()).
                    Replace('<webAppUrl>', $AppUrl).
                    Replace('<botId>', [System.Guid]::NewGuid().ToString()).
                    Replace('<webDomain>', $AppDomain)
        md ProposalManager
        New-Item -Path .\ProposalManager\ -Name manifest.json -ItemType File
        Set-Content .\ProposalManager\manifest.json $manifest
        copy ..\TeamsAddinPackage\outline.png .\ProposalManager\outline.png
        copy ..\TeamsAddinPackage\color.png .\ProposalManager\color.png
        Compress-Archive .\ProposalManager\* .\ProposalManager.zip
        rd ProposalManager -Recurse
        Write-Host "Package created successfully" -ForegroundColor Green
    }
}