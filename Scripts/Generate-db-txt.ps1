param(
    [string]$SchemaTsv = "$PSScriptRoot\_schema_export.tsv",
    [string]$OutputPath = "$PSScriptRoot\..\Docs\db.txt"
)

$ErrorActionPreference = 'Stop'
. "$PSScriptRoot\Get-SqlConnectionFromAppSettings.ps1"
$sql = Get-SqlConnectionFromAppSettings

$lines = Get-Content $SchemaTsv

$uniqueMap = @{}
$uqFile = Join-Path $PSScriptRoot "_unique_export.tsv"
$uqSql = @"
SET NOCOUNT ON;
SELECT ku.TABLE_NAME + '|' + ku.COLUMN_NAME
FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS tc
INNER JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE ku
    ON tc.CONSTRAINT_NAME = ku.CONSTRAINT_NAME AND tc.TABLE_SCHEMA = ku.TABLE_SCHEMA
WHERE tc.CONSTRAINT_TYPE = 'UNIQUE'
  AND tc.TABLE_SCHEMA = 'dbo'
  AND (
      SELECT COUNT(*)
      FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE ku2
      WHERE ku2.CONSTRAINT_NAME = tc.CONSTRAINT_NAME
        AND ku2.TABLE_SCHEMA = tc.TABLE_SCHEMA
  ) = 1;
"@
& sqlcmd -S $sql.Server -d $sql.Database -U $sql.User -P $sql.Password -C -h -1 -W -Q $uqSql -o $uqFile | Out-Null
if ($LASTEXITCODE -ne 0) { throw "sqlcmd failed with exit code $LASTEXITCODE" }

Get-Content $uqFile |
    Where-Object { $_ -and $_ -notmatch 'rows affected' -and $_.Trim() -ne '' } |
    ForEach-Object { $uniqueMap[$_.Trim()] = $true }

$tables = [ordered]@{}
foreach ($line in $lines) {
    if ([string]::IsNullOrWhiteSpace($line)) { continue }
    $parts = $line -split '\s+', 7
    if ($parts.Count -lt 7) { continue }

    $tableFull = $parts[0]
    if ($tableFull -eq 'dbo.sysdiagrams') { continue }

    $col = $parts[2]
    $type = $parts[3].ToLower()
    $nullable = if ($parts[4] -eq 'NO') { 'NOT NULL' } else { 'NULL' }
    $isPk = $parts[5] -eq '1'
    $def = $parts[6]

    if (-not $tables.Contains($tableFull)) {
        $tables[$tableFull] = New-Object System.Collections.Generic.List[string]
    }

    $suffix = ''
    if ($isPk) { $suffix += ' PK' }

    $shortTable = ($tableFull -split '\.')[-1]
    $uniqueKey = "${shortTable}|${col}"
    if ($uniqueMap.ContainsKey($uniqueKey)) { $suffix += ' UNIQUE' }
    if ($def -and $def -ne 'NULL') { $suffix += " DEFAULT $def" }

    [void]$tables[$tableFull].Add("  - ${col}: $type $nullable$suffix")
}

$today = Get-Date -Format 'yyyy-MM-dd'
$versionLine = "Version: 2.7  (Increment 'Version' by 0.1 at every change)"
if (Test-Path $OutputPath) {
    $verMatch = Select-String -Path $OutputPath -Pattern '^Version:\s*([\d.]+)' | Select-Object -First 1
    if ($verMatch -and $verMatch.Matches[0].Groups[1].Success) {
        $v = [double]$verMatch.Matches[0].Groups[1].Value
        $newV = [math]::Round($v + 0.1, 1)
        $versionLine = "Version: $newV  (Increment 'Version' by 0.1 at every change)"
    }
}

$sb = New-Object System.Text.StringBuilder
[void]$sb.AppendLine('Virtual Education Management System - Database Schema')
[void]$sb.AppendLine("Generated: $today (from live database using ConnectionStrings:DefaultConnection in appsettings.Development.json)")
[void]$sb.AppendLine('')
[void]$sb.AppendLine($versionLine)
[void]$sb.AppendLine("UpdatedAt: $today  (Update 'UpdatedAt' timestamp at every change)")
[void]$sb.AppendLine('')
[void]$sb.AppendLine('READ @Database.md, @DBworkflows.md and @database-rules.mdc for implementation details.')
[void]$sb.AppendLine('IF UPDATING this file, make sure to UPDATE @Database.md aswell while following its formatting.')
[void]$sb.AppendLine('')
[void]$sb.AppendLine('This file lists database tables and columns only. It does not and should not include data rows or connection credentials.')

foreach ($t in $tables.Keys) {
    [void]$sb.AppendLine("Table: $t")
    foreach ($colLine in $tables[$t]) {
        [void]$sb.AppendLine($colLine)
    }
}

[void]$sb.AppendLine("Total tables: $($tables.Count)")
Set-Content -Path $OutputPath -Value $sb.ToString() -Encoding UTF8
Write-Host "Wrote $($tables.Count) tables to $OutputPath"
