# Render the fork's code-native wave mark into a multi-resolution Windows icon.
$ErrorActionPreference='Stop'
Add-Type -AssemblyName PresentationCore,WindowsBase
$iconTarget=Join-Path (Split-Path $PSScriptRoot -Parent) 'src/NabyScape/Assets/island.ico'
$sizes=@(16,24,32,48,64,128,256)
$frames=@()
foreach($size in $sizes) {
    $visual=[Windows.Media.DrawingVisual]::new()
    $context=$visual.RenderOpen()
    $context.PushTransform([Windows.Media.ScaleTransform]::new($size/256.0,$size/256.0))
    $background=[Windows.Media.BrushConverter]::new().ConvertFromString('#292929')
    $context.DrawRoundedRectangle($background,$null,[Windows.Rect]::new(0,0,256,256),52,52)
    $context.PushTransform([Windows.Media.TranslateTransform]::new(29,66))
    $context.PushTransform([Windows.Media.ScaleTransform]::new(5.2,5.2))
    $geometry=[Windows.Media.Geometry]::Parse('M0,17 C2,4 10,1 18,7 C25,13 31,12 38,6 C36,19 28,23 20,17 C13,11 6,11 0,17 Z')
    $context.DrawGeometry([Windows.Media.Brushes]::White,$null,$geometry)
    $context.Pop();$context.Pop();$context.Pop();$context.Close()
    $bitmap=[Windows.Media.Imaging.RenderTargetBitmap]::new($size,$size,96,96,[Windows.Media.PixelFormats]::Pbgra32)
    $bitmap.Render($visual)
    $encoder=[Windows.Media.Imaging.PngBitmapEncoder]::new()
    $encoder.Frames.Add([Windows.Media.Imaging.BitmapFrame]::Create($bitmap))
    $stream=[IO.MemoryStream]::new();$encoder.Save($stream)
    $frames+=,@($size,$stream.ToArray());$stream.Dispose()
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
