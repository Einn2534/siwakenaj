param(
    [string]$UnityPath = 'C:\Program Files\Unity\Hub\Editor\6000.3.10f1\Editor\Unity.exe',
    [string]$ProjectPath = (Split-Path -Parent $PSScriptRoot),
    [string]$OutputRoot = (Join-Path (Split-Path -Parent $PSScriptRoot) 'RegressionArtifacts')
)

$ErrorActionPreference = 'Stop'

$resolvedUnityPath = (Resolve-Path -LiteralPath $UnityPath).Path
$resolvedProjectPath = (Resolve-Path -LiteralPath $ProjectPath).Path
New-Item -ItemType Directory -Force -Path $OutputRoot | Out-Null
$resolvedOutputRoot = (Resolve-Path -LiteralPath $OutputRoot).Path

function Invoke-UnityRegressionStep {
    param(
        [string]$Name,
        [string[]]$Arguments
    )

    Write-Host "[$Name] starting"
    $process = Start-Process `
        -FilePath $resolvedUnityPath `
        -ArgumentList $Arguments `
        -Wait `
        -PassThru `
        -WindowStyle Hidden

    if ($process.ExitCode -ne 0) {
        throw "[$Name] Unity exited with code $($process.ExitCode)."
    }

    Write-Host "[$Name] passed"
}

$editModeResults = Join-Path $resolvedOutputRoot 'editmode-results.xml'
$editModeLog = Join-Path $resolvedOutputRoot 'editmode.log'
$playModeResults = Join-Path $resolvedOutputRoot 'playmode-results.xml'
$playModeLog = Join-Path $resolvedOutputRoot 'playmode.log'
$captureLog = Join-Path $resolvedOutputRoot 'capture.log'

$quotedProjectPath = '"' + $resolvedProjectPath + '"'
$quotedEditModeResults = '"' + $editModeResults + '"'
$quotedEditModeLog = '"' + $editModeLog + '"'
$quotedPlayModeResults = '"' + $playModeResults + '"'
$quotedPlayModeLog = '"' + $playModeLog + '"'
$quotedOutputRoot = '"' + $resolvedOutputRoot + '"'
$quotedCaptureLog = '"' + $captureLog + '"'

Invoke-UnityRegressionStep -Name 'EditMode' -Arguments @(
    '-batchmode',
    '-nographics',
    '-projectPath', $quotedProjectPath,
    '-runTests',
    '-testPlatform', 'EditMode',
    '-testResults', $quotedEditModeResults,
    '-logFile', $quotedEditModeLog
)

Invoke-UnityRegressionStep -Name 'PlayMode' -Arguments @(
    '-batchmode',
    '-nographics',
    '-projectPath', $quotedProjectPath,
    '-runTests',
    '-testPlatform', 'PlayMode',
    '-testResults', $quotedPlayModeResults,
    '-logFile', $quotedPlayModeLog
)

Invoke-UnityRegressionStep -Name 'LayoutCapture' -Arguments @(
    '-batchmode',
    '-quit',
    '-projectPath', $quotedProjectPath,
    '-executeMethod', 'RegressionCaptureRunner.RunFromBatchMode',
    '-regressionOutput', $quotedOutputRoot,
    '-logFile', $quotedCaptureLog
)

Write-Host "Regression suite passed. Artifacts: $resolvedOutputRoot"
