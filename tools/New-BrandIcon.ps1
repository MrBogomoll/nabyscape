# Render the approved vector at each native icon size, without raster resizing.
$ErrorActionPreference='Stop'
Add-Type -AssemblyName PresentationCore,WindowsBase
$iconTarget=Join-Path (Split-Path $PSScriptRoot -Parent) 'src/NabyScape/Assets/island.ico'
$sourcePath=Join-Path (Split-Path $PSScriptRoot -Parent) 'src/NabyScape/Assets/brand-mark.svg'
[xml]$svg=Get-Content -LiteralPath $sourcePath -Raw
$art=[Windows.Media.GeometryGroup]::new();$art.FillRule=[Windows.Media.FillRule]::Nonzero
foreach($path in $svg.SelectNodes('//*[local-name()="path"]')){$art.Children.Add([Windows.Media.Geometry]::Parse($path.d))}
$art.Freeze();$bounds=$art.Bounds
$sizes=@(16,24,32,48,64,128,256)
$frames=@()
foreach($size in $sizes) {
    $visual=[Windows.Media.DrawingVisual]::new()
    [Windows.Media.RenderOptions]::SetBitmapScalingMode($visual,[Windows.Media.BitmapScalingMode]::HighQuality)
    $context=$visual.RenderOpen()
    $context.PushTransform([Windows.Media.ScaleTransform]::new($size/256.0,$size/256.0))
    $context.PushTransform([Windows.Media.TranslateTransform]::new(18,48))
    $context.PushTransform([Windows.Media.ScaleTransform]::new(220/$bounds.Width,220/$bounds.Width))
    $context.PushTransform([Windows.Media.TranslateTransform]::new(-$bounds.X,-$bounds.Y))
    $context.DrawGeometry([Windows.Media.Brushes]::White,$null,$art)
    $context.Pop();$context.Pop();$context.Pop();$context.Pop();$context.Close()
    $bitmap=[Windows.Media.Imaging.RenderTargetBitmap]::new($size,$size,96,96,[Windows.Media.PixelFormats]::Pbgra32)
    $bitmap.Render($visual)
    $encoder=[Windows.Media.Imaging.PngBitmapEncoder]::new()
    $encoder.Frames.Add([Windows.Media.Imaging.BitmapFrame]::Create($bitmap))
    $stream=[IO.MemoryStream]::new();$encoder.Save($stream)
    $frames+=,@($size,$stream.ToArray())
    if($size -eq 256){[IO.File]::WriteAllBytes((Join-Path (Split-Path $PSScriptRoot -Parent) 'src/NabyScape/Assets/brand-avatar.png'),$stream.ToArray())}
    $stream.Dispose()
}
$stream=[IO.File]::Create($iconTarget);$writer=[IO.BinaryWriter]::new($stream)
try {
    $writer.Write([uint16]0);$writer.Write([uint16]1);$writer.Write([uint16]$sizes.Count)
    $offset=6+16*$sizes.Count
    foreach($frame in $frames) {
        $dimension=if($frame[0] -eq 256){0}else{$frame[0]}
        $writer.Write([byte]$dimension);$writer.Write([byte]$dimension);$writer.Write([byte]0);$writer.Write([byte]0)
        $writer.Write([uint16]1);$writer.Write([uint16]32);$writer.Write([uint32]$frame[1].Length);$writer.Write([uint32]$offset)
        $offset+=$frame[1].Length
    }
    foreach($frame in $frames){$writer.Write([byte[]]$frame[1])}
} finally {$writer.Dispose();$stream.Dispose()}
Write-Output "Generated $($sizes.Count) icon sizes."
