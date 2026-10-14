param(
    [string]$SourceDirectory = (Join-Path $env:LOCALAPPDATA 'SiwakenjaSigning'),
    [Parameter(Mandatory)][string]$BackupDirectory,
    [Parameter(Mandatory)][string]$RecoveryKeyPath
)
$ErrorActionPreference = 'Stop'
if (Test-Path -LiteralPath $BackupDirectory) { throw 'Use a new backup directory; existing backups are never overwritten.' }
if (Test-Path -LiteralPath $RecoveryKeyPath) { throw 'Recovery key already exists; choose a new path.' }
foreach ($name in 'upload.keystore','password.dpapi') {
    if (!(Test-Path -LiteralPath (Join-Path $SourceDirectory $name))) { throw "Missing signing file: $name" }
}
$sourcePassword = (Get-Content -LiteralPath (Join-Path $SourceDirectory 'password.dpapi') -Raw).Trim() | ConvertTo-SecureString
$recoveryKey = [byte[]]::new(32)
[System.Security.Cryptography.RandomNumberGenerator]::Fill($recoveryKey)
try {
    $keyDirectory = Split-Path -Parent ([IO.Path]::GetFullPath($RecoveryKeyPath))
    New-Item -ItemType Directory -Force -Path $keyDirectory | Out-Null
    # The portable recovery key must be kept separately from the encrypted backup.
    $identity = [Security.Principal.WindowsIdentity]::GetCurrent().User
    $acl = [Security.AccessControl.DirectorySecurity]::new()
    $acl.SetAccessRuleProtection($true, $false)
    $acl.AddAccessRule([Security.AccessControl.FileSystemAccessRule]::new($identity,'FullControl','ContainerInherit,ObjectInherit','None','Allow'))
    Set-Acl -LiteralPath $keyDirectory -AclObject $acl
    [IO.File]::WriteAllBytes([IO.Path]::GetFullPath($RecoveryKeyPath), $recoveryKey)
    New-Item -ItemType Directory -Path $BackupDirectory | Out-Null
    Copy-Item -LiteralPath (Join-Path $SourceDirectory 'upload.keystore') -Destination $BackupDirectory
    $sourcePassword | ConvertFrom-SecureString -Key $recoveryKey | Set-Content -LiteralPath (Join-Path $BackupDirectory 'password.aes') -Encoding utf8
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'Restore-Signing.ps1') -Destination $BackupDirectory
    $files = foreach ($name in 'upload.keystore','password.aes','Restore-Signing.ps1') {
        @{name=$name; sha256=(Get-FileHash -Algorithm SHA256 -LiteralPath (Join-Path $BackupDirectory $name)).Hash}
    }
    @{format='SiwakenjaSigning-v1'; createdUtc=[DateTime]::UtcNow.ToString('o'); keyAlias='upload'; protection='PowerShell AES-256 SecureString with separate 32-byte key'; files=@($files)} |
        ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $BackupDirectory 'manifest.json') -Encoding utf8
    @'
Siwakenja signing recovery

This folder contains the original JKS upload key and an AES-encrypted password.
The separate 32-byte recovery key is required. It is intentionally NOT in this folder.
Keep the recovery key in a trusted password manager or separate protected offline storage.
Never commit, upload publicly, or send either file in chat.

On a Windows PC, use PowerShell 7:
  ./Restore-Signing.ps1 -BackupDirectory . -RecoveryKeyPath <separate-key-file> -DestinationDirectory <NEW-private-folder>

The destination must not already exist. Restore creates upload.keystore and a new
password.dpapi protected for the current Windows user. Pass that folder to
Tools/Build-GooglePlay.ps1 -SigningDirectory <NEW-private-folder>.
No password is printed or written as plaintext.

The backup plus its separate recovery key are both necessary for recovery.
'@ | Set-Content -LiteralPath (Join-Path $BackupDirectory 'RECOVERY.txt') -Encoding utf8
    [pscustomobject]@{BackupDirectory=[IO.Path]::GetFullPath($BackupDirectory); RecoveryKeyPath=[IO.Path]::GetFullPath($RecoveryKeyPath); KeystoreSha256=$files[0].sha256}
}
finally {
    [Array]::Clear($recoveryKey,0,$recoveryKey.Length)
    $sourcePassword.Dispose()
}
