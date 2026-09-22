$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$themeIconDir = Join-Path (Split-Path -Parent $PSScriptRoot) 'Assets/Art/Sprites/SortingThemes'
New-Item -ItemType Directory -Force -Path $themeIconDir | Out-Null
$navy = [System.Drawing.ColorTranslator]::FromHtml('#102642')
$teal = [System.Drawing.ColorTranslator]::FromHtml('#21B9C6')
$gold = [System.Drawing.ColorTranslator]::FromHtml('#FFBF43')
$cream = [System.Drawing.ColorTranslator]::FromHtml('#FFF9DE')
$navyBrush = [System.Drawing.SolidBrush]::new($navy)
$tealBrush = [System.Drawing.SolidBrush]::new($teal)
$goldBrush = [System.Drawing.SolidBrush]::new($gold)
$creamBrush = [System.Drawing.SolidBrush]::new($cream)
$outline = [System.Drawing.Pen]::new($navy, 14)
$format = [System.Drawing.StringFormat]::new()
$format.Alignment = [System.Drawing.StringAlignment]::Center
$format.LineAlignment = [System.Drawing.StringAlignment]::Center
$numberFont = [System.Drawing.Font]::new('Arial', 34, [System.Drawing.FontStyle]::Bold, [System.Drawing.GraphicsUnit]::Pixel)
foreach ($hour in @(3, 6, 9)) {
    $bitmap = [System.Drawing.Bitmap]::new(512,512,[System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $canvas = [System.Drawing.Graphics]::FromImage($bitmap)
    $canvas.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $canvas.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::AntiAliasGridFit
    $canvas.Clear([System.Drawing.Color]::Transparent)
    $canvas.FillEllipse($navyBrush, 25,25,462,462)
    $canvas.FillEllipse($tealBrush, 39,39,434,434)
    $canvas.FillEllipse($goldBrush, 53,53,406,406)
    $canvas.FillEllipse($navyBrush, 64,64,384,384)
    $canvas.FillEllipse($creamBrush, 74,74,364,364)
    for ($mark=1; $mark -le 12; $mark++) {
        $angle = $mark * [Math]::PI / 6
        if (($mark % 3) -eq 0) {
            $x=256+[Math]::Sin($angle)*143
            $y=256-[Math]::Cos($angle)*143
            $canvas.DrawString([string]$mark,$numberFont,$navyBrush,[System.Drawing.PointF]::new($x,$y),$format)
        } else {
            $x=256+[Math]::Sin($angle)*161
            $y=256-[Math]::Cos($angle)*161
            $canvas.FillEllipse($navyBrush,[float]($x-5),[float]($y-5),10,10)
        }
    }
    $minutePen=[System.Drawing.Pen]::new($navy,14)
    $minutePen.StartCap=$minutePen.EndCap=[System.Drawing.Drawing2D.LineCap]::Round
    $canvas.DrawLine($minutePen,256,256,256,149)
    $angle=$hour*[Math]::PI/6
    $hourPen=[System.Drawing.Pen]::new($navy,22)
    $hourPen.StartCap=$hourPen.EndCap=[System.Drawing.Drawing2D.LineCap]::Round
    $canvas.DrawLine($hourPen,256,256,[float](256+[Math]::Sin($angle)*81),[float](256-[Math]::Cos($angle)*81))
    $canvas.FillEllipse($goldBrush,245,245,22,22)
    $bitmap.Save((Join-Path $themeIconDir "clock-$hour.png"),[System.Drawing.Imaging.ImageFormat]::Png)
    $minutePen.Dispose(); $hourPen.Dispose(); $canvas.Dispose(); $bitmap.Dispose()
}
# A sealed parcel hides every non-vehicle category behind the same silhouette.
$bitmap=[System.Drawing.Bitmap]::new(512,512,[System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$canvas=[System.Drawing.Graphics]::FromImage($bitmap)
$canvas.SmoothingMode=[System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$canvas.TextRenderingHint=[System.Drawing.Text.TextRenderingHint]::AntiAliasGridFit
$canvas.Clear([System.Drawing.Color]::Transparent)
$canvas.FillRectangle($navyBrush,64,79,384,360)
$canvas.FillRectangle($tealBrush,78,93,356,332)
$canvas.FillRectangle($goldBrush,221,93,70,332)
$canvas.FillRectangle($navyBrush,54,73,404,82)
$canvas.FillRectangle($goldBrush,68,87,376,54)
$canvas.FillEllipse($navyBrush,163,192,186,186)
$canvas.FillEllipse($creamBrush,177,206,158,158)
$questionFont=[System.Drawing.Font]::new('Arial',118,[System.Drawing.FontStyle]::Bold,[System.Drawing.GraphicsUnit]::Pixel)
$canvas.DrawString('?',$questionFont,$navyBrush,[System.Drawing.PointF]::new(256,287),$format)
$bitmap.Save((Join-Path $themeIconDir 'mystery-parcel.png'),[System.Drawing.Imaging.ImageFormat]::Png)
$canvas.Dispose(); $bitmap.Dispose(); $questionFont.Dispose()
$navyBrush.Dispose(); $tealBrush.Dispose(); $goldBrush.Dispose(); $creamBrush.Dispose(); $numberFont.Dispose(); $format.Dispose(); $outline.Dispose()
Write-Output "Created exact 3/6/9 o'clock icons and mystery parcel in $themeIconDir"
