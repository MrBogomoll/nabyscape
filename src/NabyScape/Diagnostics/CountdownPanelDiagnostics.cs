using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
namespace NabyScape;
internal static class CountdownPanelDiagnostics
{
    internal static async Task Run(App app,OverlayWindow island,string directory,Action<string,bool> check)
    {
        var s=app.Settings;s.CompactLayout=true;s.PanelWidth=526;s.PanelMaxHeight=650;s.PanelPadding=26;s.PanelScale=1;s.ReduceMotion=true;s.HideMaximized=s.HideFullscreen=s.AlwaysTucked=false;s.SmartActivities=false;s.NotificationDelivery="Off";s.CountdownSound="Off";
        foreach(var w in s.Widgets)w.Enabled=w.Id is "timer" or "focus" or "media";
        s.FocusSeconds=1500;s.BreakSeconds=300;s.TimerDurationSeconds=300;app.Session.Timer.Reset();app.Session.Focus.Reset();app.ChangeSettings();island.Reveal(false);island.SelectPage("focus",false);await Task.Delay(150);
        CountdownView View(string id)=>Diagnostics.Descendants(island).OfType<CountdownView>().Single(v=>Diagnostics.Descendants(v).OfType<Button>().Any(b=>UI.Identity(b)==id+"-primary"));
        TextBox Input(string id,string part)=>Diagnostics.Descendants(View(id=="break"?"focus":id)).OfType<TextBox>().Single(t=>UI.Identity(t)==id+" "+part);
        Button Button(string id,string action="primary")=>Diagnostics.Descendants(View(id)).OfType<Button>().Single(b=>UI.Identity(b)==id+"-"+action);
        void Click(Button b)=>b.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Button.ClickEvent));
        void Set(string id,int h,int m,int sec){Input(id,"Hours").Text=h.ToString("00");Input(id,"Minutes").Text=m.ToString("00");Input(id,"Seconds").Text=sec.ToString("00");}
        void Wheel(TextBox field,int delta){field.RaiseEvent(new MouseWheelEventArgs(Mouse.PrimaryDevice,Environment.TickCount,delta){RoutedEvent=Mouse.PreviewMouseWheelEvent});}
        void Capture(FrameworkElement view,string name)
        {view.UpdateLayout();var bmp=new RenderTargetBitmap((int)view.ActualWidth,(int)view.ActualHeight,96,96,PixelFormats.Pbgra32);bmp.Render(view);var png=new PngBitmapEncoder();png.Frames.Add(BitmapFrame.Create(bmp));using var file=File.Create(Path.Combine(directory,name+".png"));png.Save(file);}
        foreach(string id in new[]{"timer","focus","break"})
        {
            Set(id,99,59,59);string page=island.ActivePage;Wheel(Input(id,"Seconds"),120);
            check(id+" seconds wrap to zero without carrying or switching pages",Input(id,"Seconds").Text=="00" && Input(id,"Minutes").Text=="59" && island.ActivePage==page);
            Wheel(Input(id,"Seconds"),-120);check(id+" seconds wrap back to 59",Input(id,"Seconds").Text=="59");
            Wheel(Input(id,"Minutes"),120);check(id+" minutes wrap to zero independently",Input(id,"Minutes").Text=="00" && Input(id,"Hours").Text=="99");
            var up=Diagnostics.Descendants(View(id=="break"?"focus":id)).OfType<Button>().Single(b=>UI.Identity(b)==id+" Hours up");Click(up);
            check(id+" hour button wraps 99 to zero",Input(id,"Hours").Text=="00");
            Set(id,0,0,58);Wheel(Input(id,"Seconds"),600);check(id+" multi-detent wheel preserves all five steps",Input(id,"Seconds").Text=="03");
        }
        Set("break",0,0,7);
        foreach(string id in new[]{"timer","focus"})
        {
            var state=id=="timer"?app.Session.Timer:app.Session.Focus;Set(id,1,2,3);Click(Button(id));var run=state.RunId;
            check(id+" starts with exact entered hours minutes seconds",state.Running && state.DurationSeconds==3723);
            check(id+" locks all duration fields while running",!Input(id,"Seconds").IsEnabled);
            Click(Button(id));double paused=state.Remaining(DateTimeOffset.UtcNow);await Task.Delay(60);
            check(id+" pauses without restarting",!state.Running && state.RunId==run && state.Remaining(DateTimeOffset.UtcNow)==paused);
            Click(Button(id));check(id+" resumes the same countdown",state.Running && state.RunId==run && state.DurationSeconds==3723);
            Click(Button(id,"reset"));Set(id,0,0,0);check(id+" zero duration cannot start",!Button(id).IsEnabled);
            Set(id,99,59,59);Click(Button(id));check(id+" accepts 99:59:59",state.DurationSeconds==DurationParts.MaximumSeconds && state.Display(DateTimeOffset.UtcNow).StartsWith("99:59:",StringComparison.Ordinal));Click(Button(id,"reset"));
            Set(id,0,0,1);Click(Button(id));check(id+" accepts a one-second countdown",state.Running && state.DurationSeconds==1);Click(Button(id,"reset"));
        }
        Set("timer",0,0,0);var hours=Input("timer","Hours");
        hours.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice,PresentationSource.FromVisual(island),0,Key.Down){RoutedEvent=Keyboard.PreviewKeyDownEvent});
        check("Keyboard down wraps zero hours to 99",hours.Text=="99");
        hours.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice,PresentationSource.FromVisual(island),0,Key.Up){RoutedEvent=Keyboard.PreviewKeyDownEvent});
        check("Keyboard up wraps 99 hours to zero",hours.Text=="00");
        var manual=Input("timer","Seconds");manual.Text="7";manual.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice,PresentationSource.FromVisual(island),0,Key.Enter){RoutedEvent=Keyboard.PreviewKeyDownEvent});check("Manual single digit commits as two digits",manual.Text=="07");
        app.Session.Timer.Reset();s.TimerDurationSeconds=0;
        typeof(OverlayWindow).GetMethod("TogglePeekClock",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)!.Invoke(island,new object[]{"timer"});
        check("Quick preview cannot start a zero duration",!app.Session.Timer.Running);
        app.Session.Timer.Start(DurationParts.MaximumSeconds,DateTimeOffset.UtcNow);
        typeof(OverlayWindow).GetMethod("ExtendPeekClock",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)!.Invoke(island,new object[]{"timer"});
        check("Quick extension respects the 99-hour limit",app.Session.Timer.DurationSeconds<=DurationParts.MaximumSeconds && app.Session.Timer.Remaining(DateTimeOffset.UtcNow)<=DurationParts.MaximumSeconds);
        app.Session.Timer.Reset();s.TimerDurationSeconds=300;app.SaveSession();
        Set("timer",0,7,9);var draft=Input("timer","Minutes");draft.Focus();draft.SelectAll();app.SaveSession();
        check("Heartbeat preserves manual input and selection",draft.Text=="07" && draft.SelectionLength==2);
        var paste=new DataObjectPastingEventArgs(new DataObject(DataFormats.UnicodeText,"bad"),false,DataFormats.UnicodeText){RoutedEvent=DataObject.PastingEvent};draft.RaiseEvent(paste);check("Invalid pasted duration text is rejected",paste.CommandCancelled);
        Set("focus",0,0,2);Set("break",0,0,7);Click(Button("focus"));app.Session.Focus.Deadline=DateTimeOffset.UtcNow.AddSeconds(-1);app.CheckCountdowns();
        check("Focus starts a seconds-precision break",app.Session.Focus.Phase=="break" && app.Session.Focus.DurationSeconds==7 && app.Session.Focus.Remaining(DateTimeOffset.UtcNow)>5);Click(Button("focus","reset"));
        app.Session.Timer.Start(3723,DateTimeOffset.UtcNow);app.SaveSession();check("Externally started countdown synchronizes all three components",Input("timer","Hours").Text=="01" && Input("timer","Minutes").Text=="02" && Input("timer","Seconds").Text=="03");Click(Button("timer","reset"));
        foreach(double width in new[]{300d,526d})
        {s.PanelWidth=width;app.ChangeSettings();await Task.Delay(100);island.SelectPage("focus",false);island.UpdateLayout();foreach(string id in new[]{"timer","focus"}){var v=View(id);check(id+" picker fits at panel width "+width,Diagnostics.Descendants(v).OfType<TextBox>().All(t=>{var r=t.TransformToAncestor(v).TransformBounds(new Rect(t.RenderSize));return r.Left>=-.5 && r.Right<=v.ActualWidth+.5;}));}Capture(island,"countdowns-"+width);}
        foreach(string theme in new[]{"Light","Dark"})
        {s.WindowTheme=theme;var window=new SettingsWindow(app){Width=1000,Height=850,ShowActivated=false};try{window.Show();window.OpenCategory("Timers");await Task.Delay(100);check("Settings "+theme+" uses shared hour minute second editors",Diagnostics.Descendants(window).OfType<DurationPicker>().Count()==3);Capture(window,"countdowns-settings-"+theme);}finally{window.Close();}}
        s.CompactLayout=false;app.ChangeSettings();island.Reveal(false);await Task.Delay(100);check("Classic panel uses the same duration picker",Diagnostics.Descendants(island).OfType<DurationPicker>().Any(p=>p.IsVisible));
    }
}
