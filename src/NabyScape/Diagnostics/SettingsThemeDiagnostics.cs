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
            var timerResult=((SettingsSearchItem[])typeof(SettingsWindow).GetMethod("BuildSettingsSearchIndex",BindingFlags.Static|BindingFlags.NonPublic)!.Invoke(null,null)!).Single(i=>i.Title=="Timer & focus duration");
            typeof(SettingsWindow).GetMethod("OpenSettingsSearchResult",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(window,new object[]{timerResult});await Task.Delay(100);
            check("Timer search opens the current timers page and focus section",timerResult.Category=="Timers" && Diagnostics.CurrentSettingsCategory(window)=="Timers" && Tree(window).OfType<Border>().Any(b=>b.Tag as string=="Focus sessions"));
            window.OpenCategory("Notes");await Task.Delay(80);
            var draft=Tree(window).OfType<TextBox>().Single(t=>UI.Identity(t)=="Quick draft");draft.Text="Заметка для проверки библиотеки";
            var draftTitle=Tree(window).OfType<TextBox>().Single(t=>UI.Identity(t)=="Note title");draftTitle.Text="Из настроек";
            Tree(window).OfType<Button>().Single(b=>UI.Identity(b)=="Save to library").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));await Task.Delay(80);
            check("Notes page saves a draft in the persistent library",app.Productivity.Notes.Any(n=>n.Text==draft.Text) && app.Store.Load<ProductivityData>("productivity").Notes.Any(n=>n.Text==draft.Text));
            check("Settings draft title is saved with the note",app.Productivity.Notes.Single(n=>n.Text==draft.Text).Title=="Из настроек");app.Productivity.NoteTitle="";
            for(int noteIndex=1;noteIndex<=3;noteIndex++){app.Productivity.Note="Заметка "+noteIndex+". Текст для предпросмотра карточки.";app.Productivity.SaveDraftToLibrary();}
            await Task.Delay(80);window.UpdateLayout();
            var savedCard=Tree(window).OfType<Expander>().First(e=>UI.Identity(e).StartsWith("Saved note card "));
            var cardHeader=(System.Windows.Controls.Primitives.ToggleButton)savedCard.Template.FindName("HeaderSite",savedCard);
            check("Saved notes use compact rounded cards in a responsive grid",savedCard.Parent is Border surface && surface.CornerRadius.TopLeft==18 && surface.ActualWidth>250 && surface.ActualWidth<500 && Tree(window).OfType<WrapPanel>().Any(p=>UI.Identity(p)=="Saved note cards"));
            Save(window,Path.Combine(directory,"notes-cards-collapsed.png"));
            var togglePeer=new System.Windows.Automation.Peers.ToggleButtonAutomationPeer(cardHeader);
            ((System.Windows.Automation.Provider.IToggleProvider)togglePeer.GetPattern(System.Windows.Automation.Peers.PatternInterface.Toggle)).Toggle();await Task.Delay(60);
            check("Clicking the note card opens its editor",savedCard.IsExpanded && Tree(savedCard).OfType<TextBox>().Any(t=>UI.Identity(t)=="Note title" && t.IsVisible));
            var noteTitle=Tree(savedCard).OfType<TextBox>().Single(t=>UI.Identity(t)=="Note title");var noteBody=Tree(savedCard).OfType<TextBox>().Single(t=>UI.Identity(t)=="Saved note text");
            string noteId=UI.Identity(savedCard)["Saved note card ".Length..];noteTitle.Text="Мой заголовок";await Task.Delay(450);
            check("Card title edits autosave and update the header",app.Store.Load<ProductivityData>("productivity").Notes.Single(n=>n.Id==noteId).Title=="Мой заголовок" && Tree(savedCard).OfType<TextBlock>().Any(t=>t.Text=="Мой заголовок"));
            Save(window,Path.Combine(directory,"notes-card-expanded.png"));
            noteTitle.Clear();noteBody.Text="Первая фраза. Остальной текст заметки.";await Task.Delay(450);
            check("Blank title updates automatically from the first phrase",app.Productivity.Notes.Single(n=>n.Id==noteId).DisplayTitle=="Первая фраза." && Tree(savedCard).OfType<TextBlock>().Any(t=>t.Text=="Первая фраза."));
            savedCard.IsExpanded=false;
            foreach(string noteTheme in new[]{"Light","Dark"}){app.Settings.WindowTheme=noteTheme;window.ApplyWindowAppearance();await Task.Delay(60);Save(window,Path.Combine(directory,"notes-cards-"+noteTheme+".png"));}
            window.Width=600;await Task.Delay(80);window.UpdateLayout();
            check("Narrow note cards use one column without clipping",Tree(window).OfType<Expander>().Where(e=>UI.Identity(e).StartsWith("Saved note card ")).All(e=>e.Parent is Border b && b.ActualWidth>400 && b.ActualWidth<580));Save(window,Path.Combine(directory,"notes-cards-narrow.png"));
            window.Width=1100;await Task.Delay(80);
            Save(window,Path.Combine(directory,"notes-library.png"));
            var choices=Diagnostics.Descendants(window).OfType<CheckBox>().Where(c=>UI.Identity(c).StartsWith("Select note ")).ToArray();
            var batchDelete=Tree(window).OfType<Button>().Single(b=>UI.Identity(b)=="Delete selected notes");
            void ToggleChoice(CheckBox choice)=>((System.Windows.Automation.Provider.IToggleProvider)new System.Windows.Automation.Peers.CheckBoxAutomationPeer(choice).GetPattern(System.Windows.Automation.Peers.PatternInterface.Toggle)).Toggle();
            bool AllCollapsed()=>Tree(window).OfType<Expander>().Where(e=>UI.Identity(e).StartsWith("Saved note card ")).All(e=>!e.IsExpanded);
            check("Collapsed notes expose selection marks and individual delete icons",choices.Length==app.Productivity.Notes.Count && choices.All(c=>c.IsVisible) && Diagnostics.Descendants(window).OfType<Button>().Count(b=>UI.Identity(b).StartsWith("Delete note ") && b.IsVisible)==choices.Length && !batchDelete.IsEnabled);
            bool ReceivesClick(FrameworkElement control,Point inside){var target=window.InputHitTest(control.TransformToAncestor(window).Transform(inside)) as DependencyObject;return target!=null && (ReferenceEquals(control,target) || control.IsAncestorOf(target));}
            check("Selection mark receives its own mouse clicks",ReceivesClick(choices[0],new Point(8,16)));
            check("Card body receives mouse clicks independently of its controls",ReceivesClick(cardHeader,new Point(100,40)));
            ToggleChoice(choices[0]);ToggleChoice(choices[1]);
            check("Choosing notes enables group deletion without expanding cards",choices.Count(c=>c.IsChecked==true)==2 && batchDelete.IsEnabled && AllCollapsed());
            foreach(string selectedTheme in new[]{"Light","Dark"}){app.Settings.WindowTheme=selectedTheme;window.ApplyWindowAppearance();await Task.Delay(60);Save(window,Path.Combine(directory,"notes-selected-"+selectedTheme+".png"));}
            window.Width=600;await Task.Delay(80);Save(window,Path.Combine(directory,"notes-selected-narrow.png"));window.Width=1100;await Task.Delay(80);
            var chosenIds=choices.Where(c=>c.IsChecked==true).Select(c=>UI.Identity(c)["Select note ".Length..]).ToArray();var beforeDelete=app.Productivity.Notes.Select(n=>n.Id).ToArray();string draftBefore=app.Productivity.Note,titleBefore=app.Productivity.NoteTitle;
            check("Cancelling group deletion preserves notes and selection",ClickNoteDeletion(window,batchDelete,false,"Delete notes") && app.Productivity.Notes.Select(n=>n.Id).SequenceEqual(beforeDelete) && choices.Count(c=>c.IsChecked==true)==2 && AllCollapsed());
            Tree(window).OfType<Button>().Single(b=>UI.Identity(b)=="Clear selection").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            check("Clear selection disables deletion",choices.All(c=>c.IsChecked!=true) && !batchDelete.IsEnabled);
            Tree(window).OfType<Button>().Single(b=>UI.Identity(b)=="Select all").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            check("Select all checks every card without expanding",choices.All(c=>c.IsChecked==true) && AllCollapsed());
            Tree(window).OfType<Button>().Single(b=>UI.Identity(b)=="Clear selection").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));ToggleChoice(choices[0]);ToggleChoice(choices[1]);
            using(var lockedNotes=new FileStream(Path.Combine(app.Store.DirectoryPath,"productivity.json"),FileMode.Open,FileAccess.Read,FileShare.Read))
                check("Failed group deletion preserves all notes and selection for retry",ClickNoteDeletion(window,batchDelete,true,"Delete notes") && app.Productivity.Notes.Select(n=>n.Id).SequenceEqual(beforeDelete) && choices.Count(c=>c.IsChecked==true)==2 && batchDelete.IsEnabled);
            check("Group deletion removes only chosen notes and keeps the draft",ClickNoteDeletion(window,batchDelete,true,"Delete notes") && app.Productivity.Notes.Select(n=>n.Id).SequenceEqual(beforeDelete.Where(id=>!chosenIds.Contains(id))) && app.Store.Load<ProductivityData>("productivity").Notes.All(n=>!chosenIds.Contains(n.Id)) && app.Productivity.Note==draftBefore && app.Productivity.NoteTitle==titleBefore && !batchDelete.IsEnabled);
            var directDelete=Diagnostics.Descendants(window).OfType<Button>().First(b=>UI.Identity(b).StartsWith("Delete note "));string directId=UI.Identity(directDelete)["Delete note ".Length..];
            check("Card trash is a separate mouse target",ReceivesClick(directDelete,new Point(16,16)));
            check("Cancelling a card trash icon keeps the collapsed note",ClickNoteDeletion(window,directDelete,false,"Delete note") && app.Productivity.Notes.Any(n=>n.Id==directId) && AllCollapsed());
            check("Card trash deletes without opening its editor",ClickNoteDeletion(window,directDelete,true,"Delete note") && !app.Productivity.Notes.Any(n=>n.Id==directId) && AllCollapsed());
            window.OpenCategory("Tasks");await Task.Delay(60);
            var taskInput=Tree(window).OfType<TextBox>().Single(t=>UI.Identity(t)=="New task");taskInput.Text="Проверить библиотеку заметок";
            Tree(window).OfType<Button>().Single(b=>UI.Identity(b)=="Add task").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));await Task.Delay(60);
            var task=app.Productivity.Tasks.Last(t=>t.Text=="Проверить библиотеку заметок");
            var taskCheck=Tree(window).OfType<CheckBox>().Single(c=>UI.Identity(c)==task.Text);taskCheck.IsChecked=true;taskCheck.RaiseEvent(new RoutedEventArgs(CheckBox.ClickEvent));
            taskCheck=Tree(window).OfType<CheckBox>().Single(c=>UI.Identity(c)==task.Text);taskCheck.IsChecked=false;window.UpdateLayout();var taskHit=window.InputHitTest(taskCheck.TransformToAncestor(window).Transform(new Point(8,taskCheck.ActualHeight/2))) as DependencyObject;
            check("Empty task mark accepts a click through its center",taskHit!=null && (ReferenceEquals(taskHit,taskCheck) || taskCheck.IsAncestorOf(taskHit)));taskCheck.IsChecked=true;
            check("Tasks page completion survives a store reload",app.Store.Load<ProductivityData>("productivity").Tasks.Any(t=>t.Id==task.Id && t.Done));Save(window,Path.Combine(directory,"tasks-list.png"));
            window.Hide();app.Settings.HideFullscreen=app.Settings.HideMaximized=app.Settings.AlwaysTucked=false;
            app.Settings.Widgets.Single(w=>w.Id=="clock").Enabled=false;app.Settings.Widgets.Single(w=>w.Id=="notes").Enabled=true;
            app.Session.Timer.Reset();app.Session.Focus.Reset();app.Session.Stopwatch.Reset();app.Media.DiagnosticPlayback(false);app.ChangeSettings();island.Show();island.Collapse();app.PublishUpdate(RuntimeChange.Media);await Task.Delay(160);
            var dockClock=(TextBlock)typeof(OverlayWindow).GetField("compactClock",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(island)!;
            check("Idle dock shows time instead of an empty-playback message",dockClock.Visibility==Visibility.Visible && island.HeaderLabel.Text=="" && island.HeaderLabel.Visibility==Visibility.Collapsed);Save(island,Path.Combine(directory,"idle-without-clock.png"));
            window.Show();window.OpenCategory("Appearance");shared.IsExpanded=true;contentChoice.SelectedItem="Music";
            Tree(shared).OfType<Button>().First(b=>UI.Identity(b)=="Preview Closed").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));await Task.Delay(650);
            TextBlock PreviewClock()=>(TextBlock)typeof(SettingsWindow).GetField("previewClockLabel",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(window)!;
            TextBlock PreviewSummary()=>(TextBlock)typeof(SettingsWindow).GetField("previewSummaryLabel",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(window)!;
            var previewSurface=(Border)typeof(SettingsWindow).GetField("preview",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(window)!;
            check("Paused music preview matches the idle clock",PreviewClock().Visibility==Visibility.Visible && PreviewClock().Text==dockClock.Text && PreviewSummary().Text=="" && !Tree(previewSurface).OfType<Image>().Any());Save(window,Path.Combine(directory,"idle-preview.png"));
            app.Settings.Widgets.Single(w=>w.Id=="stopwatch").Enabled=true;app.Settings.Widgets.Single(w=>w.Id=="timer").Enabled=true;
            long stopwatchTicks=0;var stopwatchNow=DateTimeOffset.UtcNow;app.Session.Stopwatch.UseClock(()=>stopwatchNow,()=>stopwatchTicks);app.Session.Stopwatch.Toggle();stopwatchTicks=65000;app.ChangeSettings();await Task.Delay(80);
            check("Running stopwatch time matches between dock and preview",island.HeaderLabel.Visibility==Visibility.Visible && island.HeaderLabel.Text.EndsWith("01:05") && PreviewSummary().Text==island.HeaderLabel.Text);Save(window,Path.Combine(directory,"stopwatch-preview.png"));
            stopwatchTicks=66000;app.PublishUpdate(RuntimeChange.Countdown);check("Small stopwatch and preview advance together",island.HeaderLabel.Text.EndsWith("01:06") && PreviewSummary().Text==island.HeaderLabel.Text);
            app.Session.Timer.Start(120,DateTimeOffset.UtcNow);app.PublishUpdate(RuntimeChange.Countdown);await Task.Delay(60);
            check("Timer gets priority and matches the preview",island.HeaderLabel.Text.StartsWith(Localization.Text("Timer  ")) && PreviewSummary().Text==island.HeaderLabel.Text);Save(window,Path.Combine(directory,"timer-preview.png"));
            app.Session.Timer.Reset();app.Session.Stopwatch.Reset();app.PublishUpdate(RuntimeChange.Countdown);await Task.Delay(60);
            check("Stopping counters restores matching idle clocks",island.HeaderLabel.Text=="" && dockClock.Visibility==Visibility.Visible && PreviewClock().Visibility==Visibility.Visible && PreviewSummary().Text=="");window.Hide();

            island.PreviewActivity();island.SelectPeek("stopwatch");await Task.Delay(200);
            var pageCaption=(TextBlock)typeof(OverlayWindow).GetField("peekPageName",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(island)!;
            check("Russian stopwatch caption stays on one line",pageCaption.TextWrapping==TextWrapping.NoWrap && pageCaption.ActualHeight<20 && pageCaption.ActualWidth>=90);Save(island,Path.Combine(directory,"stopwatch-caption.png"));
            island.SelectPeek("notes");await Task.Delay(350);
            var peekDraft=Diagnostics.Descendants(island).OfType<TextBox>().Single(t=>UI.Identity(t)==Localization.Text("Preview note draft"));
            var peekTitle=Diagnostics.Descendants(island).OfType<TextBox>().Single(t=>UI.Identity(t)=="Note title" && t.IsVisible);peekTitle.Text="Заголовок из быстрого окна";
            peekDraft.Text="Быстрая заметка из свёрнутой панели";
            var peekSave=Diagnostics.Descendants(island).OfType<Button>().Single(b=>UI.Identity(b)==Localization.Text("Save preview note"));peekSave.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            check("Quick preview Save note archives text in the library",app.Store.Load<ProductivityData>("productivity").Notes.Any(n=>n.Text==peekDraft.Text) && (string?)peekSave.Content==Localization.Text("Saved"));
            check("Quick preview saved feedback is localized",(string?)peekSave.Content=="Сохранено");
            check("Quick preview saves the title and keeps its editor within the panel",app.Productivity.Notes.Single(n=>n.Text==peekDraft.Text).Title==peekTitle.Text && peekTitle.IsVisible && peekSave.TransformToAncestor(island).Transform(new Point(0,peekSave.ActualHeight)).Y<island.ActualHeight);
            int savedCount=app.Productivity.Notes.Count;peekSave.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));check("Quick preview repeated save does not duplicate the note",app.Productivity.Notes.Count==savedCount);Save(island,Path.Combine(directory,"quick-note-saved.png"));
            Diagnostics.Descendants(island).OfType<Button>().Single(b=>UI.Identity(b)==Localization.Text("Open all notes")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));await Task.Delay(80);
            var noteSettings=Application.Current.Windows.OfType<SettingsWindow>().Single(w=>!ReferenceEquals(w,window));
            check("Quick preview All notes opens the saved library",(string?)typeof(SettingsWindow).GetField("current",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(noteSettings)=="Notes");noteSettings.Close();island.Hide();window.Show();
            window.Hide();island.Show();island.OpenFeaturePage("notes");await Task.Delay(350);
            var mainTitle=(TextBox)typeof(OverlayWindow).GetField("mainNoteTitleEditor",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(island)!;
            check("Expanded panel has the same optional title",mainTitle.IsVisible && mainTitle.Text==peekTitle.Text);
            app.Previews.Open("notes");await Task.Delay(100);var detachedNotes=Application.Current.Windows.Cast<Window>().Single(w=>w.Title=="NabyScape · "+DetachedPreviewWindow.NameFor("notes"));
            var detachedTitle=Tree(detachedNotes).OfType<TextBox>().Single(t=>UI.Identity(t)=="Note title");
            mainTitle.Text="Заголовок с панели";await Task.Delay(450);
            check("Main panel title synchronizes all editors without losing typed spaces",app.Productivity.NoteTitle==mainTitle.Text && peekTitle.Text==mainTitle.Text && detachedTitle.Text==mainTitle.Text && app.Store.Load<ProductivityData>("productivity").NoteTitle==mainTitle.Text);
            Tree(island).OfType<Button>().Single(b=>UI.Identity(b)=="Save to library" && b.IsVisible).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            check("Expanded panel Save to library includes the chosen title",app.Store.Load<ProductivityData>("productivity").Notes.Any(n=>n.Text==peekDraft.Text && n.Title==mainTitle.Text));Save(island,Path.Combine(directory,"main-notes-title.png"));
            detachedTitle.Text="Общий ";await Task.Delay(450);check("Detached title edits preserve a trailing space in the shared draft",mainTitle.Text=="Общий " && peekTitle.Text=="Общий " && app.Productivity.NoteTitle=="Общий ");detachedNotes.Close();
            island.Collapse();island.Hide();window.Show();window.OpenCategory("Notes");await Task.Delay(80);
            check("Settings shows the shared title from the panel",Tree(window).OfType<TextBox>().Single(t=>UI.Identity(t)=="Note title").Text=="Общий ");
            window.OpenCategory("Home");await Task.Delay(60);
            System.Windows.SystemCommands.MaximizeWindow(window);await Task.Delay(250);window.UpdateLayout();
            var outer=(Border)typeof(SettingsWindow).GetField("windowFrame",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(window)!;
            check("Maximized settings have square corners and no inner frame gap",window.WindowState==WindowState.Maximized && outer.CornerRadius==new CornerRadius(0) && ((Grid)outer.Child).Margin==new Thickness(0));
            var hwnd=new System.Windows.Interop.WindowInteropHelper(window).Handle;var work=System.Windows.Forms.Screen.FromHandle(hwnd).WorkingArea;
            Native.GetWindowRect(hwnd,out var actualBounds);
            check("Maximized settings fit the monitor working area",Math.Abs(actualBounds.Left-work.Left)<=1 && Math.Abs(actualBounds.Top-work.Top)<=1 && Math.Abs(actualBounds.Right-work.Right)<=1 && Math.Abs(actualBounds.Bottom-work.Bottom)<=1);
            check("Settings form uses the available centered width on this monitor",Tree(window).OfType<StackPanel>().Any(p=>p.HorizontalAlignment==HorizontalAlignment.Center && p.ActualWidth>=Math.Min(1000,Math.Max(240,window.ActualWidth-(window.ActualWidth<760?0:252)-100))-2));
            Save(window,Path.Combine(directory,"settings-maximized.png"));
            // The background fixture does not activate the native caption. Request
            // the WPF state directly when checking the restored outline.
            window.WindowState=WindowState.Normal;
            for(int frame=0;frame<125 && (window.WindowState!=WindowState.Normal || outer.CornerRadius.TopLeft==0);frame++)await Task.Delay(16);
            window.UpdateLayout();
            check("Restoring settings restores the rounded outline ("+window.WindowState+", "+outer.CornerRadius.TopLeft+")",window.WindowState==WindowState.Normal && outer.CornerRadius.TopLeft>0);
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
            window.OpenCategory("Application");await Task.Delay(80);
            check("About and updates show the fork version without build hash",Version.TryParse(AppVersion.Display.Split('-')[0],out _) && !AppVersion.Display.Contains('+') && Tree(window).OfType<TextBlock>().Count(t=>t.Text.Contains(AppVersion.Display))>=2);
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
    // Answer only this isolated fixture's native confirmation; never target another process.
    private static bool ClickNoteDeletion(Window owner,Button button,bool accept,string caption)
    {
        bool answered=false,defaultNo=false;
        var timer=new System.Windows.Threading.DispatcherTimer{Interval=TimeSpan.FromMilliseconds(25)};
        timer.Tick+=(_,_)=>
        {
            IntPtr dialog=IntPtr.Zero;
            while((dialog=FindWindowEx(IntPtr.Zero,dialog,"#32770",Localization.Text(caption)))!=IntPtr.Zero)
            {
                GetWindowThreadProcessId(dialog,out uint pid);if(pid!=Environment.ProcessId || GetWindow(dialog,4)!=new System.Windows.Interop.WindowInteropHelper(owner).Handle)continue;
                defaultNo=(GetWindowLong(GetDlgItem(dialog,7),-16)&0xF)==1;answered=true;timer.Stop();SendMessage(GetDlgItem(dialog,accept?6:7),0xF5,IntPtr.Zero,IntPtr.Zero);break;
            }
        };
        timer.Start();try{button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));}finally{timer.Stop();}
        return answered && defaultNo;
    }
    [DllImport("user32.dll",CharSet=CharSet.Unicode)] private static extern IntPtr FindWindowEx(IntPtr parent,IntPtr after,string className,string title);
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr hwnd,out uint pid);
    [DllImport("user32.dll")] private static extern IntPtr GetWindow(IntPtr hwnd,uint command);
    [DllImport("user32.dll")] private static extern IntPtr GetDlgItem(IntPtr hwnd,int id);
    [DllImport("user32.dll",EntryPoint="GetWindowLongW")] private static extern int GetWindowLong(IntPtr hwnd,int index);
    [DllImport("user32.dll")] private static extern IntPtr SendMessage(IntPtr hwnd,int message,IntPtr wparam,IntPtr lparam);
}
