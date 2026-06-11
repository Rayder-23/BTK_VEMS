# Parses SQL Server connection details from appsettings.Development.json or environment.
# Dot-source from other Scripts/*.ps1 files: . "$PSScriptRoot\Get-SqlConnectionFromAppSettings.ps1"

function Get-SqlConnectionFromAppSettings {
    [CmdletBinding()]
    param(
        [string]$RepoRoot = (Split-Path $PSScriptRoot -Parent),
        [string]$SettingsPath
    )

    if ([string]::IsNullOrWhiteSpace($SettingsPath)) {
        $SettingsPath = Join-Path $RepoRoot 'appsettings.Development.json'
    }

    $cs = $env:ConnectionStrings__DefaultConnection
    if ([string]::IsNullOrWhiteSpace($cs)) {
        $cs = $env:DefaultConnection
    }

    if ([string]::IsNullOrWhiteSpace($cs)) {
        if (-not (Test-Path $SettingsPath)) {
            throw "Missing $SettingsPath - add DefaultConnection there or set env ConnectionStrings__DefaultConnection."
        }

        $json = Get-Content $SettingsPath -Raw | ConvertFrom-Json
        $cs = $json.ConnectionStrings.DefaultConnection
    }

    if ([string]::IsNullOrWhiteSpace($cs)) {
        throw 'ConnectionStrings:DefaultConnection is empty in appsettings.Development.json and no env override is set.'
    }

    $m = [regex]::Match(
        $cs,
        'Server=(?<s>[^;]+);Database=(?<d>[^;]+);(?:User\s+Id|UID)=(?<u>[^;]+);(?:Password|PWD)=(?<p>[^;]+)',
        'IgnoreCase')

    if (-not $m.Success) {
        throw 'Could not parse Server;Database;User Id;Password from connection string.'
    }

    $server = $m.Groups['s'].Value.Trim()
    if ($server.StartsWith('tcp:', [StringComparison]::OrdinalIgnoreCase)) {
        $server = $server.Substring(4)
    }

    return [ordered]@{
        Server   = $server
        Database = $m.Groups['d'].Value.Trim()
        User     = $m.Groups['u'].Value.Trim()
        Password = $m.Groups['p'].Value.Trim()
    }
}
