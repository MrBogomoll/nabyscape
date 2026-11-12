using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
namespace NabyScape;
internal static class VolumeLyricsDiagnostics
{
    internal static async Task Run(App app,OverlayWindow dock,string directory,Action<string,bool> check)
    {
        var s=app.Settings;s.ClosedMediaContent="Lyrics";s.HideFullscreen=s.HideMaximized=s.AlwaysTucked=false;s.SmartActivities=false;
        s.ReactiveFeedback=s.VolumeFeedback=s.BrightnessFeedback=true;s.ClosedMediaIcon="Artwork";s.ReduceMotion=true;s.LyricsOffset=0;s.Width=400;
        app.Assistant.Options.IslandCharacter=true;
        app.Session.Timer.Reset();app.Session.Focus.Reset();app.Session.Stopwatch.Reset();
        foreach(var w in s.Widgets)w.Enabled=w.Id is "media" or "clock";
        app.Media.DiagnosticPlayback(true);
        var art=new DrawingGroup();using(var dc=art.Open()){dc.DrawRectangle(Brushes.LightSteelBlue,null,new Rect(0,0,20,20));dc.DrawEllipse(Brushes.SlateBlue,null,new Point(10,10),6,6);}art.Freeze();
        typeof(MediaService).GetProperty("Artwork")!.SetValue(app.Media,new DrawingImage(art));
        void Import(string text)=>app.Lyrics.Import(app.Media.Title,app.Media.Artist,240,"[00:00]"+text);
        void Update()=>typeof(OverlayWindow).GetMethod("Update",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(dock,null);
        FrameworkElement? Field(string name)=>typeof(OverlayWindow).GetField(name,BindingFlags.Instance|BindingFlags.NonPublic)?.GetValue(dock) as FrameworkElement;
        void Capture(string name)
        {
            dock.UpdateLayout();var bmp=new RenderTargetBitmap((int)dock.ActualWidth,(int)dock.ActualHeight,96,96,PixelFormats.Pbgra32);bmp.Render(dock);
            var png=new PngBitmapEncoder();int width=Math.Min(bmp.PixelWidth,(int)Math.Ceiling(dock.PaintedWidth)),height=Math.Min(bmp.PixelHeight,(int)Math.Ceiling(dock.PaintedHeight));png.Frames.Add(BitmapFrame.Create(new CroppedBitmap(bmp,new Int32Rect(Math.Max(0,(bmp.PixelWidth-width)/2),0,width,height))));using var file=File.Create(Path.Combine(directory,name+".png"));png.Save(file);
        }
        Import("Строка песни");app.ChangeSettings();dock.Collapse();await Task.Delay(250);
        check("Volume lyrics fixture starts with a synced vocal line",dock.HeaderLabel.Text=="Строка песни");
        check("Music shows the volume bar instead of the clock",Field("feedbackLevel")?.IsVisible==true && Field("compactClock")?.IsVisible!=true);
        check("Music shows its volume percentage before any feedback event",Field("feedbackInlineValue") is TextBlock initial && initial.IsVisible && initial.Text.EndsWith("%",StringComparison.Ordinal));
        dock.DiagnosticFeedFeedback(false,43);dock.UpdateLayout();
        check("Volume feedback preserves the current lyric",dock.HeaderLabel.Text=="Строка песни");
        check("Shared feedback preserves track artwork and hides the volume icon",dock.ClosedCoverVisible && Field("feedbackIcon")?.IsVisible!=true);
        check("Naby remains visible during volume changes",Field("closedAssistant")?.IsVisible==true);
        var value=Field("feedbackInlineValue") as TextBlock;
        check("Volume percent is visible to the left of the lyric",value?.IsVisible==true && value.Text=="43%" && value.TransformToAncestor(dock).Transform(new Point(value.ActualWidth,0)).X<=dock.HeaderLabel.TransformToAncestor(dock).Transform(new Point()).X);
        bool continuous=true;for(int i=0;i<30;i++){dock.DiagnosticFeedFeedback(false,20+i);continuous&=dock.HeaderLabel.Text=="Строка песни";}
        check("Repeated limiter changes never replace the lyric",continuous && app.Lyrics.Result.Lines.Length==1);
        Import("Следующая строка");await Task.Delay(90);
        check("Lyrics continue updating while volume feedback is active",dock.HeaderLabel.Text=="Следующая строка");Capture("lyrics-volume-left");
        dock.DiagnosticFeedFeedback(false,0,true);check("Mute preserves lyrics with a compact left readout",dock.HeaderLabel.Text=="Следующая строка" && value?.Text=="0%");
        s.Width=180;app.ChangeSettings();dock.DiagnosticFeedFeedback(false,100);await Task.Delay(120);dock.UpdateLayout();
        check("Narrow dock keeps lyric and volume in separate non-overlapping columns",dock.HeaderLabel.Text.Length>0 && value?.IsVisible==true && value.TransformToAncestor(dock).Transform(new Point(value.ActualWidth,0)).X<=dock.HeaderLabel.TransformToAncestor(dock).Transform(new Point()).X);Capture("lyrics-volume-narrow");
        dock.DiagnosticFeedFeedback(true,70);check("Brightness retains standalone feedback",dock.HeaderLabel.Text=="70%" && value?.IsVisible!=true);
        typeof(OverlayWindow).GetField("feedbackUntil",BindingFlags.Instance|BindingFlags.NonPublic)!.SetValue(dock,DateTimeOffset.MinValue);Update();await Task.Delay(120);
        check("Feedback expiry preserves lyrics and the persistent music percentage",dock.HeaderLabel.Text=="Следующая строка" && value?.IsVisible==true && value.Text.EndsWith("%",StringComparison.Ordinal));
        s.ClosedMediaContent="Title";app.ChangeSettings();dock.DiagnosticFeedFeedback(false,42);
        check("Song-title mode also keeps artwork beside volume",dock.HeaderLabel.Text==app.Media.Title && dock.ClosedCoverVisible && value?.Text=="42%");
        app.Media.DiagnosticPlayback(false);typeof(OverlayWindow).GetField("feedbackUntil",BindingFlags.Instance|BindingFlags.NonPublic)!.SetValue(dock,DateTimeOffset.MinValue);Update();await Task.Delay(100);
        check("Stopping music restores the clock, hides the volume bar and keeps Naby",Field("compactClock")?.IsVisible==true && Field("feedbackLevel")?.IsVisible!=true && value?.IsVisible!=true && Field("closedAssistant")?.IsVisible==true);
        dock.DiagnosticFeedFeedback(false,43);check("Idle volume feedback still shows its percentage and keeps Naby",dock.HeaderLabel.Text=="43%" && Field("closedAssistant")?.IsVisible==true);
    }
}
