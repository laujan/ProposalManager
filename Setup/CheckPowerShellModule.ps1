

Function Verify-RequiredModules{
    [CmdletBinding()]
    param( 
        [Parameter(Mandatory = $false)]
        [switch]$AddInsOnly
    )

    Write-Information "Checking required PS modules"

    $modules = [ordered]@{
        'Azure' = @{ Version = [System.Version]'5.3.0' }
        'AzureAD' = @{ Version = [System.Version]'2.0.2.4' }
        'AzureRM' = @{ Version = [System.Version]'5.7.0' }
        'MicrosoftTeams' = @{ Version = [System.Version]'0.9.6'}
    }
    
    # These modules are only required when installing the main PM instance
    if (!$AddInsOnly)
    {
        $modules['Microsoft.Online.SharePoint.Powershell'] = @{ Version = [System.Version]'16.0.8615.1200' }
        $modules['SharePointPnPPowerShellOnline'] = @{ Version = [System.Version]'3.6.1902.2' }
    }

    foreach ($module in $modules.GetEnumerator()) {
        Verify-Module -ModuleName $module.Name -ModuleVersion $module.Value.Version
    }
}

Function Verify-Module{
    [CmdletBinding()]
    param( 
        [Parameter(Mandatory = $true)] 
        [string]$ModuleName,
        [Parameter(Mandatory = $true)] 
        [System.Version]$ModuleVersion
    )

    # The Module is available
    if (Get-Module | Where-Object {$_.Name -eq $ModuleName -and $_.Version -eq $ModuleVersion}) {
        Write-Information "Module $ModuleName $ModuleVersion is available."
    } else {
        # If module is not imported, but available on disk then import
        if (Get-Module -ListAvailable | Where-Object {$_.Name -eq $ModuleName -and $_.Version -eq $ModuleVersion}) {
            Import-Module $ModuleName -RequiredVersion $ModuleVersion -Verbose:$VerbosePreference
        } else {
            # If module is not imported, not available on disk, but is in online gallery then install and import
            if (Find-Module -Name $ModuleName -RequiredVersion $ModuleVersion | Where-Object {$_.Name -eq $ModuleName -and $_.Version -eq $ModuleVersion}) {
                Install-Module -Name $ModuleName -RequiredVersion $ModuleVersion -Force -Scope CurrentUser -Verbose:$VerbosePreference
                Write-Information "Module $ModuleName $ModuleVersion installed."
                
                Import-Module $ModuleName -RequiredVersion $ModuleVersion -Verbose:$VerbosePreference
            } else {
                # If module is not imported, not available and not in online gallery then abort
                Write-Information "Module $ModuleName $ModuleVersion not imported, not available and not in online gallery, exiting."
                EXIT 1
            }
        }
    }
}

