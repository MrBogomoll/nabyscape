using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
namespace NabyScape;
internal static class PanelWheelDiagnostics
{
    internal static async Task Run(App app,OverlayWindow dock,Action<string,bool> check)
    {
        var s=app.Settings;s.CompactLayout=s.PanelSwipe=s.WheelWrapTabs=true;s.ReduceMotion=true;s.SmartActivities=false;
        s.HideFullscreen=s.HideMaximized=s.AlwaysTucked=false;s.PanelWidth=526;s.PanelMaxHeight=417;s.PanelScale=1.04;s.PanelPadding=32;
        foreach(var w in s.Widgets)w.Enabled=w.Id is "media" or "notes" or "tasks";
        app.Productivity.Tasks.Clear();app.Productivity.Note="";app.ChangeSettings();
        async Task Open(string id){dock.ShowTools(id,false);await Task.Delay(100);dock.UpdateLayout();}
        bool Wheel(DependencyObject source,bool horizontal=false)
        {
            ((PeekWheelAccumulator)typeof(OverlayWindow).GetField("panelWheel",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(dock)!).Reset();
            string before=dock.ActivePage;return dock.RoutePanelWheel(-120,horizontal,source) && dock.ActivePage!=before;
        }
        TextBox Editor()=>Diagnostics.Descendants(dock).OfType<TextBox>().First(t=>t.IsVisible && t.AcceptsReturn);
        await Open("notes");var note=Editor();Keyboard.ClearFocus();
        check("Hovering an unfocused note allows panel navigation",Wheel(note));
        await Open("notes");var title=Diagnostics.Descendants(dock).OfType<TextBox>().First(t=>t.IsVisible && !t.AcceptsReturn);title.Focus();
        check("Single-line title does not trap the wheel even when focused",Wheel(title));
        await Open("notes");note=Editor();Keyboard.ClearFocus();note.ApplyTemplate();
        var inside=Diagnostics.Descendants(note).OfType<ScrollViewer>().First();
        check("Text input template descendants do not create a wheel dead zone",Wheel(inside));
        await Open("notes");note=Editor();Keyboard.ClearFocus();
        check("The area beside the note input allows navigation at the user's panel size",Wheel((DependencyObject)note.Parent));
        await Open("notes");note=Editor();Keyboard.ClearFocus();string previous=dock.ActivePage;
        ((PeekWheelAccumulator)typeof(OverlayWindow).GetField("panelWheel",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(dock)!).Reset();
        var routed=new MouseWheelEventArgs(Mouse.PrimaryDevice,Environment.TickCount,-120){RoutedEvent=Mouse.PreviewMouseWheelEvent};note.RaiseEvent(routed);
        check("Real routed wheel input over a note reaches page navigation",routed.Handled && dock.ActivePage!=previous);
        await Open("notes");note=Editor();note.Focus();
        check("A short focused note with nothing to scroll permits navigation",Wheel(note));
        await Open("tasks");var task=Diagnostics.Descendants(dock).OfType<TextBox>().First(t=>t.IsVisible);task.Focus();
        check("Task input allows wheel navigation",Wheel(task));
        await Open("tasks");var empty=Diagnostics.Descendants(dock).OfType<ScrollViewer>().First(v=>v.Tag as string=="panel-content-scroll");
        check("An empty task list does not trap nearby wheel input",empty.ScrollableHeight<1 && Wheel(empty));
        await Open("notes");note=Editor();string draft=string.Join("\n",Enumerable.Range(1,80).Select(i=>"Строка заметки "+i));note.Text=draft;note.Focus();await Task.Delay(100);dock.UpdateLayout();
        string page=dock.ActivePage;
        check("A focused overflowing note retains native vertical scrolling",note.IsKeyboardFocusWithin && note.ExtentHeight>note.ViewportHeight+1 && !dock.RoutePanelWheel(-120,false,note) && dock.ActivePage==page);
        check("Horizontal wheel can leave a focused multiline editor",Wheel(note,true));
        check("Wheel navigation preserves the note draft",app.Productivity.Note==draft);
        var rows=new StackPanel();for(int i=0;i<30;i++)rows.Children.Add(new TextBlock{Text="Row "+i,Height=24});
        var list=new ScrollViewer{Content=rows,Height=96,Width=240,Tag="panel-content-scroll"};list.Measure(new Size(240,96));list.Arrange(new Rect(0,0,240,96));list.UpdateLayout();page=dock.ActivePage;
        check("A real overflowing content list keeps vertical scrolling",list.ScrollableHeight>1 && !dock.RoutePanelWheel(-120,false,rows.Children[0]) && dock.ActivePage==page);
    }
}
