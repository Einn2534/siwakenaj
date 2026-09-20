param(
    [switch]$InitializeSigning,
    [switch]$SkipDependencyReport,
    [string]$SigningDirectory = (Join-Path $env:LOCALAPPDATA 'SiwakenjaSigning'),
    [string]$KeytoolPath = 'C:\Program Files\Unity\Hub\Editor\6000.3.10f1\Editor\Data\PlaybackEngines\AndroidPlayer\OpenJDK\bin\keytool.exe'
)

$ErrorActionPreference = 'Stop'
$projectDirectory = Split-Path -Parent $PSScriptRoot
$keystorePath = Join-Path $SigningDirectory 'upload.keystore'
$passwordPath = Join-Path $SigningDirectory 'password.dpapi'
$environmentNames = @('SIWAKENJA_KEYSTORE_PATH', 'SIWAKENJA_KEY_ALIAS', 'SIWAKENJA_STORE_PASSWORD', 'SIWAKENJA_KEY_PASSWORD', 'SIWAKENJA_SKIP_DEPENDENCY_REPORT')
$previousEnvironment = @{}
foreach ($name in $environmentNames) { $previousEnvironment[$name] = [Environment]::GetEnvironmentVariable($name, 'Process') }

try {
    if ($InitializeSigning) {
        if ((Test-Path -LiteralPath $keystorePath) -or (Test-Path -LiteralPath $passwordPath)) {
            throw 'Signing material already exists. Run without -InitializeSigning to reuse it; existing files are never overwritten.'
        }
        if (!(Test-Path -LiteralPath $KeytoolPath)) { throw 'Unity Android keytool was not found.' }
        New-Item -ItemType Directory -Force -Path $SigningDirectory | Out-Null
        $passwordBytes = [byte[]]::new(32)
        [System.Security.Cryptography.RandomNumberGenerator]::Fill($passwordBytes)
        $passwordText = [Convert]::ToBase64String($passwordBytes)
        $passwordText | ConvertTo-SecureString -AsPlainText -Force | ConvertFrom-SecureString | Set-Content -LiteralPath $passwordPath
        $env:SIWAKENJA_STORE_PASSWORD = $passwordText
        $env:SIWAKENJA_KEY_PASSWORD = $passwordText
        & $KeytoolPath -genkeypair -noprompt -keystore $keystorePath -storetype JKS -alias upload -keyalg RSA -keysize 2048 -validity 10000 -dname 'CN=KibundeSeisakujo' -storepass:env SIWAKENJA_STORE_PASSWORD -keypass:env SIWAKENJA_KEY_PASSWORD
        if ($LASTEXITCODE -ne 0) { throw 'Upload key generation failed. Inspect the signing directory before retrying.' }
    }
    if (!(Test-Path -LiteralPath $keystorePath) -or !(Test-Path -LiteralPath $passwordPath)) {
        throw 'Signing material is missing. Use -InitializeSigning once for a new app, or provide the existing signing directory.'
    }
    $protectedPassword = (Get-Content -LiteralPath $passwordPath -Raw).Trim() | ConvertTo-SecureString
    $passwordText = [System.Net.NetworkCredential]::new('', $protectedPassword).Password
    $env:SIWAKENJA_KEYSTORE_PATH = [IO.Path]::GetFullPath($keystorePath)
    $env:SIWAKENJA_KEY_ALIAS = 'upload'
    $env:SIWAKENJA_STORE_PASSWORD = $passwordText
    $env:SIWAKENJA_KEY_PASSWORD = $passwordText
    $env:SIWAKENJA_SKIP_DEPENDENCY_REPORT = if ($SkipDependencyReport) { '1' } else { '0' }
    $outputDirectory = Join-Path $projectDirectory 'Builds/GooglePlay'
    New-Item -ItemType Directory -Force -Path $outputDirectory | Out-Null
    $outputPath = Join-Path $outputDirectory ('siwakenja-' + (Get-Date -Format 'yyyyMMdd-HHmmss') + '.aab')
    & unity run $projectDirectory --timeout 1800 -- -executeMethod CommandLineAndroidBuild.BuildGooglePlayAab -outputPath $outputPath -logFile (Join-Path $projectDirectory 'Logs/GooglePlayBuild.log')
    if ($LASTEXITCODE -ne 0) { throw 'Google Play build failed. See Logs/GooglePlayBuild.log.' }
    Get-FileHash -Algorithm SHA256 -LiteralPath $outputPath
}
finally {
    foreach ($name in $environmentNames) { [Environment]::SetEnvironmentVariable($name, $previousEnvironment[$name], 'Process') }
    $passwordText = $null
}
