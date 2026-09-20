param([Parameter(Mandatory)][string]$Path)
$ErrorActionPreference = 'Stop'
$bundlePath = (Resolve-Path -LiteralPath $Path).Path
$javaTools = 'C:/Program Files/Unity/Hub/Editor/6000.3.10f1/Editor/Data/PlaybackEngines/AndroidPlayer/OpenJDK/bin'
& "$javaTools/jarsigner.exe" '-J-Duser.language=en' -verify $bundlePath
if ($LASTEXITCODE -ne 0) { throw 'Bundle signature verification failed.' }
$bundletool = "$javaTools/../../Tools/bundletool-all-1.17.2.jar"
$manifestText = (& "$javaTools/java.exe" -jar $bundletool dump manifest --bundle=$bundlePath --module=base) -join "`n"
if ($LASTEXITCODE -ne 0) { throw 'Manifest extraction failed.' }
[xml]$manifest = $manifestText
$android = 'http://schemas.android.com/apk/res/android'
if ($manifest.manifest.package -ne 'com.kibundeseisakujo.siwakenja') { throw 'Wrong package ID.' }
if ($manifest.manifest.'uses-sdk'.GetAttribute('targetSdkVersion', $android) -ne '36') { throw 'Unexpected target SDK.' }
$adId = @($manifest.manifest.application.'meta-data' | Where-Object { $_.GetAttribute('name', $android) -eq 'com.google.android.gms.ads.APPLICATION_ID' })
if ($adId.Count -ne 1 -or $adId[0].GetAttribute('value', $android) -ne 'ca-app-pub-7699373274594279~5747435059') { throw 'Wrong AdMob application ID.' }
if ($manifestText -match 'com\.unity3d\.ads') { throw 'Unity Ads remains in the manifest.' }
Write-Output 'Manifest: package, target SDK 36, AdMob ID verified; no Unity Ads components.'
$zip = [IO.Compression.ZipFile]::OpenRead($bundlePath)
try {
    $native = @($zip.Entries | Where-Object FullName -Match '^base/lib/.+\.so$')
    if ($native.Count -eq 0) { throw 'No native libraries found.' }
    foreach ($entry in $native) {
        if ($entry.FullName -notmatch '^base/lib/arm64-v8a/') { throw "Unexpected ABI: $($entry.FullName)" }
        $memory = [IO.MemoryStream]::new()
        $stream = $entry.Open()
        try { $stream.CopyTo($memory); $bytes = $memory.ToArray() }
        finally { $stream.Dispose(); $memory.Dispose() }
        if ($bytes[0] -ne 0x7f -or $bytes[1] -ne 0x45 -or $bytes[4] -ne 2 -or $bytes[5] -ne 1) { throw 'Expected ELF64 little endian.' }
        $offset = [BitConverter]::ToUInt64($bytes, 32)
        $size = [BitConverter]::ToUInt16($bytes, 54)
        $count = [BitConverter]::ToUInt16($bytes, 56)
        $loads = 0
        for ($i = 0; $i -lt $count; $i++) {
            $header = [int]($offset + $i * $size)
            if ([BitConverter]::ToUInt32($bytes, $header) -ne 1) { continue }
            $loads++
            $align = [BitConverter]::ToUInt64($bytes, $header + 48)
            if ($align -lt 16384) { throw "Insufficient ELF alignment: $($entry.FullName)" }
        }
        if ($loads -eq 0) { throw 'No ELF LOAD segments.' }
        Write-Output "$($entry.FullName): ELF LOAD alignment >= 16 KB"
    }
} finally { $zip.Dispose() }
$bundle = Get-Item -LiteralPath $bundlePath
[pscustomobject]@{ Path = $bundle.FullName; Bytes = $bundle.Length; SHA256 = (Get-FileHash -Algorithm SHA256 -LiteralPath $bundlePath).Hash } | Format-List
