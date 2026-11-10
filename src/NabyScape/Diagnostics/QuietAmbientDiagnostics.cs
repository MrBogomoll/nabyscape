using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.IO;

namespace NabyScape;

// Isolated synthetic playback. No real media transport, microphone or provider.
internal static class QuietAmbientDiagnostics
{
    internal static async Task Run(App app,OverlayWindow island,string directory,Action<string,bool> check)
    {
        var s=app.Settings;s.CoverAmbient=s.ClosedAmbient=s.LiveActivity=true;s.AmbientStrength=.8;
        s.AmbientScope="All widgets";s.AmbientColorSource="Custom color";s.AmbientCustomColor="#64A6DE";
        s.AmbientMotion="Breathe";s.GlassStyle=true;s.Background="#263244";s.Width=400;s.Height=41;s.Preset="iPhone";s.SmartActivities=false;s.ReduceMotion=false;s.HideFullscreen=s.HideMaximized=s.AlwaysTucked=false;
        foreach(var widget in s.Widgets)widget.Enabled=true;
        app.Session.Focus.Reset();app.Session.Timer.Reset();app.Media.SelectAutomatic();app.Media.DiagnosticPlayback(false);app.ChangeSettings();
        var surface=(IslandSurface)typeof(OverlayWindow).GetField("surface",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(island)!;
        app.Notifications.Clear();island.Collapse();await Task.Delay(650);
        check("Idle dock preserves configured color glass and static ambience",surface.AmbientVisible && surface.GlassStyle && surface.Fill is SolidColorBrush fill && fill.Color==Color.FromRgb(0x26,0x32,0x44));
        check("Idle appearance does not start animation or audio sampling",!surface.AmbientAnimating && !app.AmbientAudio.Running && app.AmbientAudio.ConsumerCount==0);
        void Capture(FrameworkElement view,string name)
        {
            view.UpdateLayout();var bitmap=new RenderTargetBitmap((int)view.ActualWidth,(int)view.ActualHeight,96,96,PixelFormats.Pbgra32);bitmap.Render(view);
            BitmapSource result=bitmap;if(view==island){int width=(int)Math.Ceiling(island.PaintedWidth),height=(int)Math.Ceiling(island.PaintedHeight);result=new CroppedBitmap(bitmap,new Int32Rect(Math.Max(0,(bitmap.PixelWidth-width)/2),0,Math.Min(width,bitmap.PixelWidth),Math.Min(height,bitmap.PixelHeight)));}
            var png=new PngBitmapEncoder();png.Frames.Add(BitmapFrame.Create(result));using var file=File.Create(Path.Combine(directory,name+".png"));png.Save(file);
        }
        Capture(island,"idle-styled");
        string closedContent=s.ClosedMediaContent;s.ClosedMediaContent="None";app.Media.DiagnosticPlayback(true);app.ChangeSettings();await Task.Delay(100);
        check("Idle keeps static light when music is excluded from the compact display",surface.AmbientVisible && !surface.AmbientAnimating && !app.AmbientAudio.Running);
        s.ClosedMediaContent=closedContent;app.Media.DiagnosticPlayback(false);app.ChangeSettings();
        var settings=new SettingsWindow(app){ShowActivated=false};
        try
        {
            settings.Show();settings.OpenCategory("Appearance");await Task.Delay(80);
            Diagnostics.Descendants(settings).OfType<Expander>().Single(e=>UI.Identity(e)=="Settings live preview").IsExpanded=true;await Task.Delay(650);
            var preview=(Border)typeof(SettingsWindow).GetField("preview",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(settings)!;
            var previewLight=(IslandSurface)typeof(SettingsWindow).GetField("ambientPreview",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(settings)!;
            var material=(Border)typeof(SettingsWindow).GetField("previewMaterial",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(settings)!;
            check("Idle preview shares configured background material and light",preview.Background is SolidColorBrush color && color.Color==((SolidColorBrush)surface.Fill).Color && material.Visibility==Visibility.Visible && previewLight.AmbientVisible && previewLight.AmbientColor==surface.AmbientColor);
            Capture(settings,"idle-styled-preview");
        }
        finally{settings.Close();}
        s.ClosedAmbient=false;app.ChangeSettings();await Task.Delay(80);
        check("Closed ambient Off removes only the light without forcing a black background",!surface.AmbientVisible && surface.GlassStyle && surface.Fill is SolidColorBrush configured && configured.Color==Color.FromRgb(0x26,0x32,0x44));
        s.ClosedAmbient=true;s.CoverAmbient=false;app.ChangeSettings();check("Ambient master Off is respected in idle",!surface.AmbientVisible);
        s.CoverAmbient=true;app.ChangeSettings();
        app.Media.DiagnosticPlayback(true);app.ChangeSettings();island.Reveal(false);island.SelectPage("media",false);await Task.Delay(100);
        check("Active media retains configured ambience",surface.AmbientVisible);
        island.Collapse();await Task.Delay(100);app.Media.DiagnosticPlayback(false);app.ChangeSettings();await Task.Delay(100);
        check("Pausing from active compact media restores idle appearance immediately",surface.GlassStyle && surface.Fill is SolidColorBrush paused && paused.Color==Color.FromRgb(0x26,0x32,0x44) && surface.AmbientVisible && !surface.AmbientAnimating);
        app.Media.DiagnosticPlayback(true);app.ChangeSettings();island.Reveal(false);island.SelectPage("media",false);await Task.Delay(100);
        var detachedSurface=new IslandSurface{Width=220,Height=120,SurfaceWidth=220,SurfaceHeight=120,Fill=Brushes.Black,GlassStyle=false};
        var window=new Window{Content=detachedSurface,Width=220,Height=120,WindowStyle=WindowStyle.None,ShowActivated=false,ShowInTaskbar=false};
        using var presenter=new AmbientPresenter(app,detachedSurface,"notes");
        try
        {
            window.Show();await Task.Delay(100);check("Detached widget initially retains configured ambience",detachedSurface.AmbientVisible);
            app.Media.SelectNone();await app.Media.Refresh();await Task.Delay(100);
            check("None removes ambience from open media and detached widgets",!surface.AmbientVisible && !detachedSurface.AmbientVisible);
            app.Session.Focus.Start(300,DateTimeOffset.UtcNow);app.SaveSession();island.SelectPage("focus",false);await Task.Delay(100);
            check("None keeps ambience off with an active focus session",!surface.AmbientVisible && app.Session.Focus.Remaining(DateTimeOffset.UtcNow)>0);
            island.Collapse();await Task.Delay(650);check("None keeps closed focus dock dark",!surface.AmbientVisible);
            check("None releases every ambient audio consumer",app.AmbientAudio.ConsumerCount==0 && !app.AmbientAudio.Running);
            check("Quiet states preserve ambience preferences",s.CoverAmbient && s.ClosedAmbient && s.AmbientStrength==.8 && s.AmbientScope=="All widgets");
            app.Media.SelectAutomatic();app.Media.DiagnosticPlayback(true);app.ChangeSettings();island.Reveal(false);island.SelectPage("media",false);await Task.Delay(100);
            check("Selecting Auto restores ambience without toggling preferences",surface.AmbientVisible && detachedSurface.AmbientVisible);
        }
        finally{window.Close();app.Session.Focus.Reset();app.Session.Timer.Reset();app.Media.SelectAutomatic();}
    }
}
