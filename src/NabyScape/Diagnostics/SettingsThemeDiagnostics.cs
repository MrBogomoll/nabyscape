using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace NabyScape;
internal static class SettingsThemeDiagnostics
{
    internal static async Task Run(App app,OverlayWindow island,string directory,Action<string,bool> check)
    {
        island.Hide();app.Settings.UiLanguage="ru";app.Settings.SettingsShowAll=true;app.Settings.HideAppExceptions=new(){"chatgpt.exe","explorer.exe"};Localization.Configure(app.Settings);
        var window=new SettingsWindow(app){Width=1100,Height=900,ShowActivated=false};
        try
        {
            window.Show();window.OpenCategory("Appearance");await Task.Delay(180);
            var shared=Tree(window).OfType<Expander>().Single(e=>UI.Identity(e)=="Settings live preview");
            var advanced=Tree(shared).OfType<Expander>().Single(e=>UI.Identity(e)=="Preview advanced options");
            check("Preview advanced controls start collapsed",!advanced.IsExpanded);
            check("Widget settings start in one collapsed group",!Tree(window).OfType<Expander>().Single(e=>UI.Identity(e)=="Widget settings group").IsExpanded);
            shared.IsExpanded=true;await Task.Delay(60);
            check("Closed preview leaves room for settings",((FrameworkElement)shared.Content).ActualHeight<260);
            Tree(window).OfType<Button>().First(b=>UI.Identity(b)=="Preview Panel").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            advanced.IsExpanded=true;await Task.Delay(60);
            var contentChoice=Tree(shared).OfType<ComboBox>().Single();contentChoice.SelectedItem="Timer";
            check("Advanced reveals content and replay",contentChoice.IsVisible && Tree(advanced).OfType<Button>().Any(b=>UI.Identity(b)=="Replay transition"));
            advanced.IsExpanded=false;
            window.OpenCategory("Visibility");await Task.Delay(60);
            check("Shared preview retains mode and content across sections",shared.IsExpanded && contentChoice.SelectedItem?.ToString()=="Timer" && (string)typeof(SettingsWindow).GetField("previewMode",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(window)! == "Panel");
            await Task.Delay(450);Save(window,Path.Combine(directory,"shared-preview-open.png"));
            shared.IsExpanded=false;window.OpenCategory("Appearance");await Task.Delay(60);
            check("Shared preview remains collapsed across sections",!shared.IsExpanded);
            check("Obsolete material controls are absent",!Tree(window).OfType<ComboBox>().Any(c=>UI.Identity(c)=="Settings material"));
            var search=Tree(window).OfType<TextBox>().First(t=>UI.Identity(t)=="Search settings");search.Focus();await Task.Delay(50);
            check("Search has no nested keyboard focus outline",search.FocusVisualStyle==null && search.BorderThickness==new Thickness(0));
            bool updateBlocked=false;try{await UpdateHandoff.Prepare(app);}catch(InvalidOperationException){updateBlocked=true;}
            check("Production update handoff is blocked before any installation",updateBlocked && !app.Updates.ChannelEnabled);
            var handle=new System.Windows.Interop.WindowInteropHelper(window).Handle;
            check("Window uses genuine per-pixel transparency",window.AllowsTransparency && (GetWindowLong(handle,-20)&0x80000)!=0);
            IntPtr Hit(double x,double y){var point=window.PointToScreen(new Point(x,y));return SendMessage(handle,0x84,IntPtr.Zero,new IntPtr(((int)point.Y<<16)|((int)point.X&0xffff)));}
            check("The visible title bar is a native drag area",Hit(window.ActualWidth/2,29)==new IntPtr(2));
            check("The close button remains clickable",Hit(window.ActualWidth-24,29)==new IntPtr(1));
            IntPtr trackInfo=Marshal.AllocHGlobal(40);
            try
            {
                Marshal.Copy(new byte[40],0,trackInfo,40);SendMessage(handle,0x0024,IntPtr.Zero,trackInfo);
                var display=System.Windows.Forms.Screen.FromHandle(handle);double trackScale=Native.Scale(display);
                check("Native resize minimum matches the WPF content minimum",Marshal.ReadInt32(trackInfo,24)>=Math.Min(Math.Ceiling(window.MinWidth*trackScale),display.WorkingArea.Width) && Marshal.ReadInt32(trackInfo,28)>=Math.Min(Math.Ceiling(window.MinHeight*trackScale),display.WorkingArea.Height));
            }
            finally{Marshal.FreeHGlobal(trackInfo);}
            var pointerScreen=System.Windows.Forms.Screen.FromPoint(System.Windows.Forms.Cursor.Position);var moveArea=pointerScreen.WorkingArea;
            IntPtr moveBuffer=Marshal.AllocHGlobal(Marshal.SizeOf<Native.Rect>());
            try
            {
                foreach(var proposed in new[]{new Native.Rect{Left=moveArea.Right-40,Top=moveArea.Top+40,Right=moveArea.Right+860,Bottom=moveArea.Top+740},new Native.Rect{Left=moveArea.Left-600,Top=moveArea.Top-200,Right=moveArea.Left+300,Bottom=moveArea.Top+500}})
                {
                    Marshal.StructureToPtr(proposed,moveBuffer,false);var result=SendMessage(handle,0x0216,IntPtr.Zero,moveBuffer);var constrained=Marshal.PtrToStructure<Native.Rect>(moveBuffer);
                    check("Native drag rectangle is constrained before painting",result==new IntPtr(1) && constrained.Left>=moveArea.Left && constrained.Top>=moveArea.Top && constrained.Right<=moveArea.Right && constrained.Bottom<=moveArea.Bottom);
                }
            }
            finally{Marshal.FreeHGlobal(moveBuffer);}

            foreach(string theme in new[]{"Light","Dark","Light"})
            {
                string caption=theme=="Light"?"Light theme":"Dark theme";
                var button=Tree(window).OfType<Button>().First(b=>UI.Identity(b)==caption);
                button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));await Task.Delay(100);window.UpdateLayout();
                check("Window theme switches live to "+theme,app.Settings.WindowTheme==theme);
                var text=(SolidColorBrush)window.FindResource("ControlText");var selected=(SolidColorBrush)window.FindResource("SettingsSelected");var ink=(SolidColorBrush)window.FindResource("SettingsSelectedText");
                check(theme+" selected navigation remains readable",Contrast(selected.Color,ink.Color)>=7);
                check(theme+" dropdown backgrounds are opaque",((SolidColorBrush)window.FindResource("PopupFill")).Color.A==255);
                button.RaiseEvent(new System.Windows.Input.MouseEventArgs(System.Windows.Input.Mouse.PrimaryDevice,0){RoutedEvent=System.Windows.Input.Mouse.MouseEnterEvent});await Task.Delay(250);
                check(theme+" hovered text retains its original scale",button.RenderTransform.Value.IsIdentity);
                var cornerImage=new RenderTargetBitmap((int)window.ActualWidth,(int)window.ActualHeight,96,96,PixelFormats.Pbgra32);cornerImage.Render(window);var pixel=new byte[4];bool clear=true;foreach(var corner in new[]{new Int32Rect(0,0,1,1),new Int32Rect((int)window.ActualWidth-1,0,1,1),new Int32Rect(0,(int)window.ActualHeight-1,1,1),new Int32Rect((int)window.ActualWidth-1,(int)window.ActualHeight-1,1,1)}){cornerImage.CopyPixels(corner,pixel,4,0);clear &= pixel[3]==0;}check(theme+" all four exterior corners are transparent",clear);
                check(theme+" text palette has correct polarity",theme=="Light"?text.Color.R<40:text.Color.R>220);
                check(theme+" visible theme captions use the selected palette",Tree(window).OfType<TextBlock>().Where(t=>t.Text==Localization.Text("Window theme") || t.Text==Localization.Text("Transparency and blur") || t.Text==Localization.Text("Window appearance")).All(t=>t.Foreground is SolidColorBrush color && color.Color==text.Color));
                Save(window,Path.Combine(directory,"theme-"+theme+".png"));
            }
            foreach(string theme in new[]{"Light","Dark"})
            {
                app.Settings.WindowTheme=theme;window.ApplyWindowAppearance();
                foreach(string category in new[]{"Home","Widgets","Notes","Tasks","Timers","Layout","Launch","Visibility","Audio","Notifications","Application","AI","Weather"})
                {
                    window.OpenCategory(category);await Task.Delay(60);
                    var primaryButtons=Tree(window).OfType<Button>().Where(b=>(bool)b.GetValue(UI.PrimaryActionProperty)).ToArray();
                    if(primaryButtons.Length>0)check(theme+" primary buttons follow the palette in "+category,primaryButtons.All(b=>b.Background==window.FindResource("SettingsPrimaryFill") && b.Foreground==window.FindResource("SettingsPrimaryText")));
                    Save(window,Path.Combine(directory,theme+"-"+category+".png"));
                }
                window.OpenCategory("Appearance");shared.IsExpanded=true;contentChoice.SelectedItem="Music";
                window.UpdateLayout();await Task.Delay(60);double stageHeight=((FrameworkElement)shared.Content).ActualHeight;
                foreach(string mode in new[]{"Closed","Hover","Panel"})
                {
                    Tree(shared).OfType<Button>().First(b=>UI.Identity(b)=="Preview "+mode).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));await Task.Delay(450);
                    check(theme+" compact "+mode+" preview fits above settings",((FrameworkElement)shared.Content).ActualHeight<260 && !contentChoice.IsVisible);
                    check(theme+" "+mode+" preview keeps fixed outer height",Math.Abs(((FrameworkElement)shared.Content).ActualHeight-stageHeight)<1);
                    Save(window,Path.Combine(directory,theme+"-compact-"+mode+".png"));
                }
                window.Width=600;await Task.Delay(60);Save(window,Path.Combine(directory,theme+"-compact-narrow.png"));
                check(theme+" narrow preview retains all mode buttons",Tree(shared).OfType<Button>().Where(b=>UI.Identity(b).StartsWith("Preview ")).All(b=>b.ActualWidth>70));
                advanced.IsExpanded=true;await Task.Delay(60);Save(window,Path.Combine(directory,theme+"-preview-advanced.png"));advanced.IsExpanded=false;shared.IsExpanded=false;
                window.OpenCategory("Appearance");window.Width=600;window.Height=650;await Task.Delay(60);Save(window,Path.Combine(directory,theme+"-narrow.png"));
                check(theme+" narrow window retains theme choice",Tree(window).OfType<Button>().Any(b=>UI.Identity(b)=="Light theme" && b.ActualWidth>100));
                window.Width=480;await Task.Delay(60);Save(window,Path.Combine(directory,theme+"-480.png"));
                check(theme+" minimum width retains both choices",Tree(window).OfType<Button>().Where(b=>UI.Identity(b) is "Light theme" or "Dark theme").All(b=>b.ActualWidth>100));
                window.Width=1100;window.Height=900;
            }
            window.Width=1100;window.Height=780;window.OpenCategory("Widgets");await Task.Delay(80);
            var group=Tree(window).OfType<Expander>().Single(e=>UI.Identity(e)=="Widget settings group");
            check("Widget navigation opens when entering its overview",group.IsExpanded);
            var searchField=Tree(window).OfType<TextBox>().Single(t=>UI.Identity(t)=="Search settings");
            var searchOrigin=searchField.TransformToAncestor(window).Transform(new Point());
            check("Search is inside the draggable window caption",searchOrigin.Y<58 && searchOrigin.X>300);
            check("Caption search is an input area rather than a drag area",Hit(searchOrigin.X+20,searchOrigin.Y+14)==new IntPtr(1));
            searchField.Text="заметки";await Task.Delay(70);Save(window,Path.Combine(directory,"caption-search.png"));
            check("Search finds nested notes settings",Tree(window).OfType<Button>().Any(b=>UI.Identity(b).Contains(Localization.Text("Notes")) && b.IsVisible));searchField.Clear();
            window.OpenCategory("Notes");await Task.Delay(80);
            var draft=Tree(window).OfType<TextBox>().Single(t=>UI.Identity(t)=="Quick draft");draft.Text="Заметка для проверки библиотеки";
            Tree(window).OfType<Button>().Single(b=>UI.Identity(b)=="Save to library").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));await Task.Delay(80);
            check("Notes page saves a draft in the persistent library",app.Productivity.Notes.Any(n=>n.Text==draft.Text) && app.Store.Load<ProductivityData>("productivity").Notes.Any(n=>n.Text==draft.Text));
            Save(window,Path.Combine(directory,"notes-library.png"));
            window.OpenCategory("Tasks");await Task.Delay(60);
            var taskInput=Tree(window).OfType<TextBox>().Single(t=>UI.Identity(t)=="New task");taskInput.Text="Проверить библиотеку заметок";
            Tree(window).OfType<Button>().Single(b=>UI.Identity(b)=="Add task").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));await Task.Delay(60);
            var task=app.Productivity.Tasks.Last(t=>t.Text=="Проверить библиотеку заметок");
            var taskCheck=Tree(window).OfType<CheckBox>().Single(c=>UI.Identity(c)==task.Text);taskCheck.IsChecked=true;taskCheck.RaiseEvent(new RoutedEventArgs(CheckBox.ClickEvent));
            check("Tasks page completion survives a store reload",app.Store.Load<ProductivityData>("productivity").Tasks.Any(t=>t.Id==task.Id && t.Done));Save(window,Path.Combine(directory,"tasks-list.png"));
            window.Hide();app.Settings.HideFullscreen=app.Settings.HideMaximized=app.Settings.AlwaysTucked=false;
            app.Settings.Widgets.Single(w=>w.Id=="clock").Enabled=false;app.Settings.Widgets.Single(w=>w.Id=="notes").Enabled=true;
            app.Session.Timer.Reset();app.Session.Focus.Reset();app.Media.DiagnosticPlayback(false);app.ChangeSettings();island.Show();island.Collapse();app.PublishUpdate(RuntimeChange.Media);await Task.Delay(160);
            check("Idle dock remains informative when clock is disabled",island.HeaderLabel.Visibility==Visibility.Visible && !string.IsNullOrWhiteSpace(island.HeaderLabel.Text));Save(island,Path.Combine(directory,"idle-without-clock.png"));
            island.PreviewActivity();island.SelectPeek("notes");await Task.Delay(350);
            var peekDraft=Diagnostics.Descendants(island).OfType<TextBox>().Single(t=>UI.Identity(t)==Localization.Text("Preview note draft"));
            peekDraft.Text="Быстрая заметка из свёрнутой панели";
            var peekSave=Diagnostics.Descendants(island).OfType<Button>().Single(b=>UI.Identity(b)==Localization.Text("Save preview note"));peekSave.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            check("Quick preview Save note archives text in the library",app.Store.Load<ProductivityData>("productivity").Notes.Any(n=>n.Text==peekDraft.Text) && (string?)peekSave.Content==Localization.Text("Saved"));
            check("Quick preview saved feedback is localized",(string?)peekSave.Content=="Сохранено");
            int savedCount=app.Productivity.Notes.Count;peekSave.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));check("Quick preview repeated save does not duplicate the note",app.Productivity.Notes.Count==savedCount);Save(island,Path.Combine(directory,"quick-note-saved.png"));
            Diagnostics.Descendants(island).OfType<Button>().Single(b=>UI.Identity(b)==Localization.Text("Open all notes")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));await Task.Delay(80);
            var noteSettings=Application.Current.Windows.OfType<SettingsWindow>().Single(w=>!ReferenceEquals(w,window));
            check("Quick preview All notes opens the saved library",(string?)typeof(SettingsWindow).GetField("current",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(noteSettings)=="Notes");noteSettings.Close();island.Hide();window.Show();
            window.OpenCategory("Home");await Task.Delay(60);
            System.Windows.SystemCommands.MaximizeWindow(window);await Task.Delay(250);window.UpdateLayout();
            var outer=(Border)typeof(SettingsWindow).GetField("windowFrame",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(window)!;
            check("Maximized settings have square corners and no inner frame gap",window.WindowState==WindowState.Maximized && outer.CornerRadius==new CornerRadius(0) && ((Grid)outer.Child).Margin==new Thickness(0));
            var hwnd=new System.Windows.Interop.WindowInteropHelper(window).Handle;var work=System.Windows.Forms.Screen.FromHandle(hwnd).WorkingArea;
            Native.GetWindowRect(hwnd,out var actualBounds);
            check("Maximized settings fit the monitor working area",Math.Abs(actualBounds.Left-work.Left)<=1 && Math.Abs(actualBounds.Top-work.Top)<=1 && Math.Abs(actualBounds.Right-work.Right)<=1 && Math.Abs(actualBounds.Bottom-work.Bottom)<=1);
            check("Wide settings use a wider centered form",Tree(window).OfType<StackPanel>().Any(p=>p.HorizontalAlignment==HorizontalAlignment.Center && p.ActualWidth>1000));
            Save(window,Path.Combine(directory,"settings-maximized.png"));
            System.Windows.SystemCommands.RestoreWindow(window);await Task.Delay(150);
            check("Restoring settings restores the rounded outline",window.WindowState==WindowState.Normal && outer.CornerRadius.TopLeft>0);
            var restored=app.Store.Load<Settings>("settings");
            check("Theme change saves to the isolated profile",restored.WindowTheme==app.Settings.WindowTheme);
            window.Width=1100;window.Height=780;window.OpenCategory("Appearance");await Task.Delay(60);
            var glass=Tree(window).OfType<Slider>().First(s=>UI.Identity(s)=="Transparency and blur");glass.Value=40;await Task.Delay(70);
            check("Glass slider requests the real system blur",app.Settings.SettingsMaterial=="Native Acrylic" && app.Settings.WindowGlass==40);
            if(window.MaterialResult.Native)check("Native glass retains a translucent tint",((LinearGradientBrush)window.FindResource("SettingsCanvas")).GradientStops.All(s=>s.Color.A<255));
            var effects=Tree(window).OfType<CheckBox>().First(c=>UI.Identity(c)=="Interface effects");effects.IsChecked=false;effects.RaiseEvent(new RoutedEventArgs(CheckBox.ClickEvent));await Task.Delay(70);
            check("Disabling effects disables native blur",!window.MaterialResult.Native && ((LinearGradientBrush)window.FindResource("SettingsCanvas")).GradientStops.All(s=>s.Color.A==255));
            string originalFill=app.Settings.ControlFill;app.Settings.ControlFill="#FFFFFF";app.ChangeSettings();
            check("Light island controls do not recolor ordinary island text",((SolidColorBrush)app.FindResource("ControlText")).Color.R<40 && ((SolidColorBrush)app.FindResource("BodyText")).Color.R>220);
            app.Settings.ControlFill=originalFill;app.ChangeSettings();
        }
        finally{window.Close();}
    }
    private static System.Collections.Generic.IEnumerable<DependencyObject> Tree(DependencyObject node)
    {yield return node;foreach(var child in LogicalTreeHelper.GetChildren(node).OfType<DependencyObject>())foreach(var descendant in Tree(child))yield return descendant;}
    internal static double Contrast(Color a,Color b)
    {
        double L(Color c){double F(byte v){double x=v/255d;return x<=.04045?x/12.92:Math.Pow((x+.055)/1.055,2.4);}return .2126*F(c.R)+.7152*F(c.G)+.0722*F(c.B);}
        double x=L(a),y=L(b);return (Math.Max(x,y)+.05)/(Math.Min(x,y)+.05);
    }
    private static void Save(FrameworkElement element,string path)
    {
        element.UpdateLayout();var bitmap=new RenderTargetBitmap((int)element.ActualWidth,(int)element.ActualHeight,96,96,PixelFormats.Pbgra32);bitmap.Render(element);
        var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(bitmap));using var stream=File.Create(path);encoder.Save(stream);
    }
    [DllImport("user32.dll",EntryPoint="GetWindowLongW")] private static extern int GetWindowLong(IntPtr hwnd,int index);
    [DllImport("user32.dll")] private static extern IntPtr SendMessage(IntPtr hwnd,int message,IntPtr wparam,IntPtr lparam);
}
