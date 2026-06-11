# Publish and deploy VEMS to dlpuae.com (Hostinger VPS)
# Requires: SSH key and host configured locally (see deploy.local.ps1.example)

param(
    [string]$SshHost = $env:VEMS_DEPLOY_SSH_HOST,
    [string]$SshKey = $env:VEMS_DEPLOY_SSH_KEY
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent

$localSettings = Join-Path $PSScriptRoot 'deploy.local.ps1'
if (Test-Path $localSettings) {
    . $localSettings
    if ([string]::IsNullOrWhiteSpace($SshHost) -and $DeploySshHost) { $SshHost = $DeploySshHost }
    if ([string]::IsNullOrWhiteSpace($SshKey) -and $DeploySshKey) { $SshKey = $DeploySshKey }
}

if ([string]::IsNullOrWhiteSpace($SshHost)) {
    throw 'Deploy SSH host is not set. Copy Scripts/deploy/deploy.local.ps1.example to deploy.local.ps1, or set VEMS_DEPLOY_SSH_HOST, or pass -SshHost.'
}

if ([string]::IsNullOrWhiteSpace($SshKey)) {
    throw 'Deploy SSH key path is not set. Copy Scripts/deploy/deploy.local.ps1.example to deploy.local.ps1, or set VEMS_DEPLOY_SSH_KEY, or pass -SshKey.'
}

if (-not (Test-Path $SshKey)) {
    throw "SSH key not found: $SshKey"
}

# Bypass broken ~/.ssh/config permissions on Windows (OWNER RIGHTS)
$sshBase = @('-F', 'NUL', '-i', $SshKey, '-o', 'IdentitiesOnly=yes', '-o', 'StrictHostKeyChecking=accept-new')
$publishDir = Join-Path $repoRoot 'publish\linux-x64'
$tarball = Join-Path $repoRoot 'vems-linux-x64.tar.gz'

Write-Host 'Publishing Release (linux-x64)...'
dotnet publish (Join-Path $repoRoot 'VEMS.csproj') -c Release -r linux-x64 --self-contained false -o $publishDir

Write-Host 'Creating tarball...'
Push-Location (Join-Path $repoRoot 'publish')
tar -czf $tarball -C 'linux-x64' .
Pop-Location

Write-Host 'Uploading...'
scp @sshBase $tarball "${SshHost}:/tmp/vems-linux-x64.tar.gz"
scp @sshBase (Join-Path $PSScriptRoot 'nginx-dlpuae.conf') "${SshHost}:/etc/nginx/sites-available/dlpuae"
scp @sshBase (Join-Path $PSScriptRoot 'vems.service') "${SshHost}:/etc/systemd/system/vems.service"

Write-Host 'Remote install...'
$remote = @'
#!/usr/bin/env bash
set -euo pipefail
mkdir -p /var/www/dlpuae /etc/vems
rm -rf /var/www/dlpuae/*
tar -xzf /tmp/vems-linux-x64.tar.gz -C /var/www/dlpuae
chown -R www-data:www-data /var/www/dlpuae
test -f /etc/vems/environment || { echo "Missing /etc/vems/environment on server"; exit 1; }
ln -sf /etc/nginx/sites-available/dlpuae /etc/nginx/sites-enabled/dlpuae
nginx -t
systemctl daemon-reload
systemctl enable vems
systemctl restart vems
systemctl reload nginx
systemctl is-active vems
'@ -replace "`r`n", "`n"
$remoteScript = Join-Path $env:TEMP 'vems-remote-install.sh'
[System.IO.File]::WriteAllText($remoteScript, $remote, [System.Text.UTF8Encoding]::new($false))
scp @sshBase $remoteScript "${SshHost}:/tmp/vems-remote-install.sh"
ssh @sshBase $SshHost 'chmod +x /tmp/vems-remote-install.sh && bash /tmp/vems-remote-install.sh'
if ($LASTEXITCODE -ne 0) {
    throw "Remote install failed with exit code $LASTEXITCODE"
}
Write-Host 'Done. Verify: https://dlpuae.com'
