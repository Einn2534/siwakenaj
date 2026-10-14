param(
    [Parameter(Mandatory)][string]$BackupDirectory,
    [Parameter(Mandatory)][string]$RecoveryKeyPath,
    [Parameter(Mandatory)][string]$DestinationDirectory
)
$ErrorActionPreference = 'Stop'
if (Test-Path -LiteralPath $DestinationDirectory) { throw 'Destination already exists; restoration never overwrites signing material.' }
$manifest = Get-Content -LiteralPath (Join-Path $BackupDirectory 'manifest.json') -Raw | ConvertFrom-Json
if ($manifest.format -ne 'SiwakenjaSigning-v1') { throw 'Unsupported backup format.' }
foreach ($name in 'upload.keystore','password.aes','Restore-Signing.ps1') {
    $entry = @($manifest.files | Where-Object name -EQ $name)
    if ($entry.Count -ne 1 -or (Get-FileHash -Algorithm SHA256 -LiteralPath (Join-Path $BackupDirectory $name)).Hash -ne $entry[0].sha256) {
        throw "Backup integrity check failed: $name"
    }
}
$recoveryKey = [IO.File]::ReadAllBytes([IO.Path]::GetFullPath($RecoveryKeyPath))
if ($recoveryKey.Length -ne 32) { throw 'The recovery key must contain exactly 32 bytes.' }
$restoredPassword = $null
try {
    $restoredPassword = (Get-Content -LiteralPath (Join-Path $BackupDirectory 'password.aes') -Raw).Trim() | ConvertTo-SecureString -Key $recoveryKey
    New-Item -ItemType Directory -Path $DestinationDirectory | Out-Null
    $identity = [Security.Principal.WindowsIdentity]::GetCurrent().User
    $acl = [Security.AccessControl.DirectorySecurity]::new()
    $acl.SetAccessRuleProtection($true, $false)
    $acl.AddAccessRule([Security.AccessControl.FileSystemAccessRule]::new($identity,'FullControl','ContainerInherit,ObjectInherit','None','Allow'))
    Set-Acl -LiteralPath $DestinationDirectory -AclObject $acl
    Copy-Item -LiteralPath (Join-Path $BackupDirectory 'upload.keystore') -Destination $DestinationDirectory
    $restoredPassword | ConvertFrom-SecureString | Set-Content -LiteralPath (Join-Path $DestinationDirectory 'password.dpapi') -Encoding utf8
    [pscustomobject]@{RestoredDirectory=[IO.Path]::GetFullPath($DestinationDirectory); KeystoreSha256=(Get-FileHash -Algorithm SHA256 -LiteralPath (Join-Path $DestinationDirectory 'upload.keystore')).Hash}
}
finally {
    [Array]::Clear($recoveryKey,0,$recoveryKey.Length)
    if ($null -ne $restoredPassword) { $restoredPassword.Dispose() }
}
