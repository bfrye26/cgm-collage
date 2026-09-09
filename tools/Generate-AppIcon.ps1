param(
    [string]$OutputPath = (Join-Path $PSScriptRoot '..\src\CGMCollage\Assets\app.ico'),
    [string]$PreviewPath = (Join-Path $PSScriptRoot '..\artifacts\app-icon-preview.png')
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing.Common

function New-RoundedRectanglePath([float]$X, [float]$Y, [float]$Width, [float]$Height, [float]$Radius) {
    $path = [System.Drawing.Drawing2D.GraphicsPath]::new()
    $diameter = $Radius * 2
    $path.AddArc($X, $Y, $diameter, $diameter, 180, 90)
    $path.AddArc($X + $Width - $diameter, $Y, $diameter, $diameter, 270, 90)
    $path.AddArc($X + $Width - $diameter, $Y + $Height - $diameter, $diameter, $diameter, 0, 90)
    $path.AddArc($X, $Y + $Height - $diameter, $diameter, $diameter, 90, 90)
    $path.CloseFigure()
    return $path
}

function New-IconPng([int]$Size) {
    $bitmap = [System.Drawing.Bitmap]::new($Size, $Size, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $graphics.CompositingQuality = [System.Drawing.Drawing2D.CompositingQuality]::HighQuality
    $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
    $graphics.Clear([System.Drawing.Color]::Transparent)

    $inset = [Math]::Max(1, $Size * 0.035)
    $background = New-RoundedRectanglePath $inset $inset ($Size - 2 * $inset) ($Size - 2 * $inset) ($Size * 0.21)
    $backgroundBrush = [System.Drawing.Drawing2D.LinearGradientBrush]::new(
        [System.Drawing.PointF]::new(0, 0),
        [System.Drawing.PointF]::new($Size, $Size),
        [System.Drawing.Color]::FromArgb(255, 31, 36, 44),
        [System.Drawing.Color]::FromArgb(255, 12, 15, 20)
    )
    $graphics.FillPath($backgroundBrush, $background)

    $borderPen = [System.Drawing.Pen]::new([System.Drawing.Color]::FromArgb(150, 91, 101, 117), [Math]::Max(1, $Size * 0.012))
    $graphics.DrawPath($borderPen, $background)

    $bars = @(
        @{ Left = 0.20; Right = 0.36; Color = [System.Drawing.Color]::FromArgb(255, 245, 27, 87) },
        @{ Left = 0.42; Right = 0.58; Color = [System.Drawing.Color]::FromArgb(255, 216, 25, 77) },
        @{ Left = 0.64; Right = 0.80; Color = [System.Drawing.Color]::FromArgb(255, 165, 18, 59) }
    )
    foreach ($bar in $bars) {
        $points = [System.Drawing.PointF[]]@(
            [System.Drawing.PointF]::new($Size * ($bar.Left + 0.055), $Size * 0.22),
            [System.Drawing.PointF]::new($Size * ($bar.Right + 0.055), $Size * 0.22),
            [System.Drawing.PointF]::new($Size * ($bar.Right - 0.055), $Size * 0.78),
            [System.Drawing.PointF]::new($Size * ($bar.Left - 0.055), $Size * 0.78)
        )
        $brush = [System.Drawing.SolidBrush]::new($bar.Color)
        $graphics.FillPolygon($brush, $points)
        $brush.Dispose()
    }

    $stream = [System.IO.MemoryStream]::new()
    $bitmap.Save($stream, [System.Drawing.Imaging.ImageFormat]::Png)
    $bytes = $stream.ToArray()
    if ($Size -eq 256) {
        $previewDirectory = Split-Path -Parent $PreviewPath
        [System.IO.Directory]::CreateDirectory($previewDirectory) | Out-Null
        $bitmap.Save($PreviewPath, [System.Drawing.Imaging.ImageFormat]::Png)
    }

    $stream.Dispose()
    $borderPen.Dispose()
    $backgroundBrush.Dispose()
    $background.Dispose()
    $graphics.Dispose()
    $bitmap.Dispose()
    return ,$bytes
}

$sizes = @(16, 20, 24, 32, 40, 48, 64, 128, 256)
$images = [System.Collections.Generic.List[byte[]]]::new()
foreach ($size in $sizes) { $images.Add((New-IconPng $size)) }
$outputDirectory = Split-Path -Parent $OutputPath
[System.IO.Directory]::CreateDirectory($outputDirectory) | Out-Null

$stream = [System.IO.File]::Create($OutputPath)
$writer = [System.IO.BinaryWriter]::new($stream)
$writer.Write([uint16]0)
$writer.Write([uint16]1)
$writer.Write([uint16]$sizes.Count)
$offset = 6 + 16 * $sizes.Count
for ($index = 0; $index -lt $sizes.Count; $index++) {
    $sizeByte = if ($sizes[$index] -eq 256) { 0 } else { $sizes[$index] }
    $writer.Write([byte]$sizeByte)
    $writer.Write([byte]$sizeByte)
    $writer.Write([byte]0)
    $writer.Write([byte]0)
    $writer.Write([uint16]1)
    $writer.Write([uint16]32)
    $writer.Write([uint32]$images[$index].Length)
    $writer.Write([uint32]$offset)
    $offset += $images[$index].Length
}
foreach ($image in $images) { $writer.Write([byte[]]$image) }
$writer.Dispose()

Write-Output "Generated $OutputPath with $($sizes.Count) sizes."
