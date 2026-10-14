param(
    [string]$WebsitePath = 'F:\Unity\siwakenja-policy-github'
)

$ErrorActionPreference = 'Stop'
$legalPath = Join-Path (Split-Path -Parent $PSScriptRoot) 'Assets/Resources/Legal'
$websiteRoot = (Resolve-Path -LiteralPath $WebsitePath).Path

function Convert-LegalText {
    param([string]$SourceName, [string]$Title, [string]$DestinationName)
    $body = Get-Content -LiteralPath (Join-Path $legalPath $SourceName) -Raw -Encoding utf8
    $parts = ($body -replace "`r`n", "`n").Trim() -split "`n`n+"
    $date = $parts | Where-Object { $_ -like '最終更新日：*' } | Select-Object -First 1
    $article = foreach ($part in $parts) {
        if ($part -eq $date -or $part -eq '気分で製作所' -or $part -eq 'しわ賢者' -or $part -eq 'プライバシーポリシー' -or $part -eq "しわ賢者`n購入条件・特定商取引法に基づく表示") { continue }
        $encoded = [System.Net.WebUtility]::HtmlEncode($part)
        $encoded = [regex]::Replace($encoded, 'https://[^\s)）<]+', '<a href="$0">$0</a>')
        $encoded = $encoded.Replace('kibundeseisakujo@gmail.com', '<a href="mailto:kibundeseisakujo@gmail.com">kibundeseisakujo@gmail.com</a>')
        $encoded = $encoded.Replace(' (mailto:<a href="mailto:kibundeseisakujo@gmail.com">kibundeseisakujo@gmail.com</a>)', '')
        $tag = if ($part.Length -lt 25 -and !$part.Contains("`n") -and !$part.Contains('：')) { 'h2' } else { 'p' }
        '<' + $tag + '>' + $encoded.Replace("`n", '<br>') + '</' + $tag + '>'
    }
    $html = @"
<!doctype html>
<html lang="ja">
<head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">
<title>しわ賢者 $Title | 気分で製作所</title>
<meta name="description" content="Androidゲーム「しわ賢者」の$Title">
<link rel="stylesheet" href="style.css"><link rel="icon" href="app-icon.png"></head>
<body><main><header><p class="publisher">気分で製作所</p><h1>しわ賢者<br>$Title</h1><p class="updated">$date</p></header>
<nav aria-label="法定表示とポリシー"><a href="index.html">プライバシーポリシー</a> / <a href="purchase.html">購入条件・特定商取引法に基づく表示</a></nav>
<article>
$($article -join "`n")
</article></main></body></html>
"@
    [System.IO.File]::WriteAllText((Join-Path $websiteRoot $DestinationName), $html, [System.Text.UTF8Encoding]::new($false))
}

Convert-LegalText 'privacy-policy-ja.txt' 'プライバシーポリシー' 'index.html'
Convert-LegalText 'purchase-terms-ja.txt' '購入条件・特定商取引法に基づく表示' 'purchase.html'
Write-Output "Synced legal documents to $websiteRoot (local files only)."
