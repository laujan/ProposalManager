Function Verify-Module{
    [CmdletBinding()]
    param( [Parameter(Mandatory = $true)] [string]$ModuleName)
    # The Module is available
    if (Get-Module | Where-Object {$_.Name -eq $ModuleName}) {
        Write-Information "Module $ModuleName is available."
    } else {
        # If module is not imported, but available on disk then import
        if (Get-Module -ListAvailable | Where-Object {$_.Name -eq $ModuleName}) {
            Import-Module $ModuleName -Verbose
        } else {
            # If module is not imported, not available on disk, but is in online gallery then install and import
            if (Find-Module -Name $ModuleName | Where-Object {$_.Name -eq $ModuleName}) {
                Install-Module -Name $ModuleName -Force -Verbose -Scope CurrentUser
                Import-Module $ModuleName -Verbose
            } else {
                # If module is not imported, not available and not in online gallery then abort
                Write-Information "Module $ModuleName not imported, not available and not in online gallery, exiting."
                EXIT 1
            }
        }
    }
}

