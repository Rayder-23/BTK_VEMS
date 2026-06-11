# Regenerates Docs/db.txt from the live SQL Server schema.
# Requires: sqlcmd (SQL Server Command Line Tools)
# Connection: reads ConnectionStrings:DefaultConnection from appsettings.Development.json

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path $PSScriptRoot -Parent
if (-not (Test-Path (Join-Path $repoRoot 'VEMS.csproj'))) {
    throw "Run this script from the VEMS repo (expected VEMS.csproj next to Scripts folder)."
}

. "$PSScriptRoot\Get-SqlConnectionFromAppSettings.ps1"
$sql = Get-SqlConnectionFromAppSettings -RepoRoot $repoRoot

$sqlFile = Join-Path $PSScriptRoot 'Generate_DbTxt.sql'
$tempOut = [System.IO.Path]::GetTempFileName()
& sqlcmd -S $sql.Server -d $sql.Database -U $sql.User -P $sql.Password -C -h-1 -W -i $sqlFile -o $tempOut
if ($LASTEXITCODE -ne 0) { throw "sqlcmd failed with exit code $LASTEXITCODE" }

$lines = Get-Content $tempOut | Where-Object { $null -ne $_ -and $_.Trim() -ne '' }
Remove-Item $tempOut -Force

$tableCount = ($lines | Where-Object { $_ -match '^Table: ' }).Count
$today = Get-Date -Format 'yyyy-MM-dd'
$outPath = Join-Path $repoRoot 'Docs\db.txt'
$versionLine = "Version: 1.6  (Increment 'Version' by 0.1 at every change)"
if (Test-Path $outPath) {
    $verMatch = Select-String -Path $outPath -Pattern '^Version:\s*([\d.]+)' | Select-Object -First 1
    if ($verMatch -and $verMatch.Matches[0].Groups[1].Success) {
        $v = [double]$verMatch.Matches[0].Groups[1].Value
        $newV = [math]::Round($v + 0.1, 1)
        $versionLine = "Version: $newV  (Increment 'Version' by 0.1 at every change)"
    }
}
$nl = [Environment]::NewLine
$header = @(
    "Virtual Education Management System - Database Schema"
    "Generated: $today (from live database using ConnectionStrings:DefaultConnection in appsettings.Development.json)"
    ''
    $versionLine
    "UpdatedAt: $today  (Update 'UpdatedAt' timestamp at every change)"
    ''
    'READ @Database.md, @DBworkflows.md and @database-rules.mdc for implementation details.'
    'IF UPDATING this file, make sure to UPDATE @Database.md aswell while following its formatting.'
    ''
    'This file lists database tables and columns only. It does not and should not include data rows or connection credentials.'
    ''
) -join $nl

$footer = $nl + 'Total tables: ' + $tableCount + $nl
$utf8NoBom = New-Object System.Text.UTF8Encoding $false
[System.IO.File]::WriteAllText($outPath, $header + ($lines -join $nl) + $footer, $utf8NoBom)
Write-Host ('Wrote {0} ({1} tables).' -f $outPath, $tableCount)
