# Publish and deploy VEMS to dlpuae.com (Hostinger VPS)
# Requires: D:\.ssh\hostinger_vps (see Instructions.txt)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$sshKey = 'D:\.ssh\hostinger_vps'
$sshHost = 'root@93.127.199.220'
# Bypass broken ~/.ssh/config permissions on Windows (OWNER RIGHTS)
$sshBase = @('-F', 'NUL', '-i', $sshKey, '-o', 'IdentitiesOnly=yes', '-o', 'StrictHostKeyChecking=accept-new')
$publishDir = Join-Path $repoRoot 'publish\linux-x64'
$tarball = Join-Path $repoRoot 'vems-linux-x64.tar.gz'

Write-Host 'Publishing Release (linux-x64)...'
dotnet publish (Join-Path $repoRoot 'VEMS.csproj') -c Release -r linux-x64 --self-contained false -o $publishDir

Write-Host 'Creating tarball...'
Push-Location (Join-Path $repoRoot 'publish')
tar -czf $tarball -C 'linux-x64' .
Pop-Location

Write-Host 'Uploading...'
scp @sshBase $tarball "${sshHost}:/tmp/vems-linux-x64.tar.gz"
scp @sshBase (Join-Path $PSScriptRoot 'nginx-dlpuae.conf') "${sshHost}:/etc/nginx/sites-available/dlpuae"
scp @sshBase (Join-Path $PSScriptRoot 'vems.service') "${sshHost}:/etc/systemd/system/vems.service"

Write-Host 'Remote install...'
$remote = @'
set -e
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
'@
ssh @sshBase $sshHost $remote
Write-Host 'Done. Verify: https://dlpuae.com'
