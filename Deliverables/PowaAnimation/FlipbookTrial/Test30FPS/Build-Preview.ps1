param(
    [string]$Ffmpeg = 'C:/Program Files/ImageMagick-7.1.1-Q16-HDRI/ffmpeg.exe',
    [string]$TimelineFile = 'timeline.json',
    [string]$FilterFile = 'preview-filter.txt',
    [string]$OutputFile = 'Powa-success-30fps.mp4'
)
$ErrorActionPreference = 'Stop'
$trialRoot = [IO.Path]::GetFullPath($PSScriptRoot)
$renderStage = Join-Path $trialRoot ('.render-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $renderStage | Out-Null
try {
    $timeline = Get-Content -LiteralPath (Join-Path $trialRoot $TimelineFile) -Raw | ConvertFrom-Json
    foreach ($entry in $timeline) {
        $source = Join-Path $trialRoot ('frame-{0:D2}.png' -f [int]$entry.source)
        $target = Join-Path $renderStage ('frame-{0:D4}.png' -f [int]$entry.frame)
        New-Item -ItemType HardLink -Path $target -Target $source | Out-Null
    }
    & $Ffmpeg -hide_banner -loglevel warning -y -framerate 30 -i (Join-Path $renderStage 'frame-%04d.png') -filter_complex_script (Join-Path $trialRoot $FilterFile) -map '[out]' -frames:v $timeline.Count -c:v libx264 -crf 18 -pix_fmt yuv420p -movflags +faststart (Join-Path $trialRoot $OutputFile)
    if ($LASTEXITCODE -ne 0) { throw 'Preview encoding failed.' }
} finally {
    $resolvedStage = [IO.Path]::GetFullPath($renderStage)
    $requiredPrefix = $trialRoot.TrimEnd([IO.Path]::DirectorySeparatorChar) + [IO.Path]::DirectorySeparatorChar
    if (-not $resolvedStage.StartsWith($requiredPrefix, [StringComparison]::OrdinalIgnoreCase) -or -not ([IO.Path]::GetFileName($resolvedStage)).StartsWith('.render-')) {
        throw 'Refusing to remove a render directory outside this trial.'
    }
    Remove-Item -LiteralPath $resolvedStage -Recurse -Force
}
