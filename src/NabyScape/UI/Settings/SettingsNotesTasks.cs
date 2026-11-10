using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace NabyScape;

public sealed partial class SettingsWindow
{
    private static TextBox ProductivityEditor(string name,string text,int height)
    {
        var editor=UI.CaptionInput(name,text);editor.AcceptsReturn=true;editor.TextWrapping=TextWrapping.Wrap;
        editor.Height=height;editor.VerticalContentAlignment=VerticalAlignment.Top;editor.VerticalScrollBarVisibility=ScrollBarVisibility.Auto;
        editor.MaxLength=ProductivityService.MaxNoteLength;editor.SetResourceReference(Control.ForegroundProperty,"ControlText");
        return editor;
    }

    private void NotesSettings()
    {
        var service=app.Productivity;var host=new StackPanel();
        var draftTitle=UI.CaptionInput("Note title",service.NoteTitle,"Leave blank to use the first phrase.");draftTitle.MaxLength=ProductivityService.MaxNoteTitleLength;draftTitle.Margin=new(0,4,0,10);
        var draft=ProductivityEditor("Quick draft",service.Note,150);
        var feedback=UI.Caption("Draft saves automatically on this device.",11,UI.Muted);Accessibility.Live(feedback);
        var debounce=new DispatcherTimer{Interval=TimeSpan.FromMilliseconds(350)};bool syncing=false,subscribed=false;
        var selected=new HashSet<string>(StringComparer.Ordinal);
        var rows=new Dictionary<string,(Border Surface,Expander Card,Action Refresh,Func<bool> Flush,Action Stop)>();
        Button? deleteSelected=null,selectAll=null,clearSelection=null;
        var selectionStatus=UI.Caption("",11,UI.Muted);Accessibility.Live(selectionStatus);
        void UpdateSelection()
        {
            foreach(var row in rows.Values)row.Refresh();
            selectionStatus.Text=Localization.Format("Selected notes: {0}",selected.Count);
            if(deleteSelected!=null){deleteSelected.Content=Localization.Text("Delete selected notes")+" ("+selected.Count+")";deleteSelected.IsEnabled=selected.Count>0;}
            if(clearSelection!=null)clearSelection.IsEnabled=selected.Count>0;
            if(selectAll!=null)selectAll.IsEnabled=service.Notes.Count>selected.Count;
        }
        var library=new WrapPanel{Margin=new(-5,0,-5,0)};UI.LocalizedName(library,"Saved note cards");var count=UI.Caption("",11,UI.Muted);
        void ArrangeCards(){double width=library.ActualWidth>0?library.ActualWidth:Math.Max(240,page.Width);int columns=width>1050?3:width>=600?2:1;foreach(var row in rows.Values)row.Surface.Width=Math.Max(200,width/columns-10);}
        library.SizeChanged+=(_,_)=>ArrangeCards();
        void State(string message)=>feedback.Text=Localization.Text(message);
        void Failure()=>State(service.LastError!=null?"Could not save. Your text is still in the editor.":"The note library is full. Delete a saved note before adding another.");
        bool FlushDraft(){debounce.Stop();bool saved=service.Save();State(saved?"Draft saves automatically on this device.":"Could not save. Your text is still in the editor.");return saved;}
        debounce.Tick+=(_,_)=>FlushDraft();
        draft.TextChanged+=(_,_)=>{if(syncing)return;service.Note=draft.Text;debounce.Stop();debounce.Start();};
        draftTitle.TextChanged+=(_,_)=>{if(syncing)return;service.NoteTitle=draftTitle.Text;debounce.Stop();debounce.Start();};
        var save=UI.CaptionButton("Save to library",()=>{debounce.Stop();if(service.SaveDraftToLibrary())State("Note saved to your library. The quick draft is unchanged.");else Failure();},true);
        var draftSection=Section("Quick draft",UI.Caption("The same draft appears in the dock and detached notes. Save a copy to keep it in your library.",12,UI.Muted),UI.Caption("Note title (optional)",11,UI.Muted),draftTitle,draft,save,feedback);
        host.Children.Add(draftSection);

        void DeleteChosen(IEnumerable<string> candidates)
        {
            var ids=candidates.Where(id=>rows.ContainsKey(id)).Distinct().ToArray();if(ids.Length==0)return;
            string caption=Localization.Text(ids.Length==1?"Delete note":"Delete notes");
            string prompt=ids.Length==1?Localization.Text("Delete this saved note? The quick draft will be kept."):Localization.Format("Delete {0} selected notes? The quick draft will be kept.",ids.Length);
            if(MessageBox.Show(this,prompt,caption,MessageBoxButton.YesNo,MessageBoxImage.Question,MessageBoxResult.No)!=MessageBoxResult.Yes)return;
            foreach(string id in ids)rows[id].Stop();
            if(service.RemoveNotes(ids))State("Notes deleted.");
            else{State("Could not save. Your text is still in the editor.");selectionStatus.Text=Localization.Text("Could not delete notes. Try again.");}
        }

        (Border Surface,Expander Card,Action Refresh,Func<bool> Flush,Action Stop) CreateRow(NoteItem item)
        {
            var title=UI.Text(item.Label,15);title.FontWeight=FontWeights.SemiBold;title.TextWrapping=TextWrapping.NoWrap;title.TextTrimming=TextTrimming.CharacterEllipsis;title.SetResourceReference(TextBlock.ForegroundProperty,"ControlText");
            var excerpt=UI.Text("",12,UI.Muted);excerpt.TextWrapping=TextWrapping.NoWrap;excerpt.TextTrimming=TextTrimming.CharacterEllipsis;excerpt.Margin=new(0,6,0,0);
            var header=new StackPanel();header.Children.Add(title);header.Children.Add(excerpt);
            var card=new Expander{Header=header,Margin=new(0),Template=SavedNoteCardTemplate()};UI.Named(card,item.DisplayTitle);System.Windows.Automation.AutomationProperties.SetAutomationId(card,"Saved note card "+item.Id);ControlFeedback.SetEnabled(card,false);
            var surface=new Border{Child=card,CornerRadius=new(18),BorderThickness=new(1),Margin=new(5,0,5,10),VerticalAlignment=VerticalAlignment.Top};surface.SetResourceReference(Border.BackgroundProperty,"SettingsCard");surface.SetResourceReference(Border.BorderBrushProperty,"SettingsLineSoft");
            var selection=new CheckBox{Width=28,MinHeight=32,HorizontalAlignment=HorizontalAlignment.Left,VerticalAlignment=VerticalAlignment.Center,Template=NoteSelectionTemplate()};selection.SetResourceReference(FrameworkElement.StyleProperty,"TaskToggle");ControlFeedback.SetEnabled(selection,false);System.Windows.Automation.AutomationProperties.SetAutomationId(selection,"Select note "+item.Id);
            selection.Checked+=(_,_)=>{if(selected.Add(item.Id))UpdateSelection();};selection.Unchecked+=(_,_)=>{if(selected.Remove(item.Id))UpdateSelection();};
            var delete=UI.CaptionButton("Delete note",()=>DeleteChosen(new[]{item.Id}));System.Windows.Automation.AutomationProperties.SetAutomationId(delete,"Delete note "+item.Id);delete.Width=delete.Height=32;delete.MinHeight=delete.MinWidth=0;delete.Padding=new(7);delete.Margin=new(0);delete.Background=Brushes.Transparent;delete.HorizontalAlignment=HorizontalAlignment.Right;ControlFeedback.SetEnabled(delete,false);
            var trash=new System.Windows.Shapes.Path{Data=Geometry.Parse("M3,6 H21 M9,6 V3 H15 V6 M6,6 L7,21 H17 L18,6 M10,10 V17 M14,10 V17"),StrokeThickness=1.5,StrokeStartLineCap=PenLineCap.Round,StrokeEndLineCap=PenLineCap.Round,StrokeLineJoin=PenLineJoin.Round,Stretch=Stretch.Uniform,Width=16,Height=18,IsHitTestVisible=false};trash.SetResourceReference(System.Windows.Shapes.Shape.StrokeProperty,"ControlText");delete.Content=trash;
            // Separate siblings of HeaderSite: selecting/deleting never toggles the Expander.
            card.Tag=new object[]{selection,delete};
            TextBox? editor=null,titleInput=null;TextBlock? result=null;DispatcherTimer? timer=null;bool updating=false,syncingEditor=false;
            bool Flush()
            {
                timer?.Stop();if(editor==null || titleInput==null || editor.Text==item.Text && NoteItem.NormalizeTitle(titleInput.Text)==item.Title)return true;
                bool okay=service.UpdateNote(item.Id,editor.Text,titleInput.Text);
                if(result!=null)result.Text=Localization.Text(okay?"Saved on this device":string.IsNullOrWhiteSpace(editor.Text)?"A saved note cannot be empty. Use Delete note to remove it.":"Could not save. Your text is still in the editor.");
                return okay;
            }
            void Refresh()
            {
                title.Text=item.Label;title.ToolTip=item.DisplayTitle;UI.Named(card,item.DisplayTitle);
                selection.IsChecked=selected.Contains(item.Id);string selectLabel=Localization.Format("Select note: {0}",item.DisplayTitle),deleteLabel=Localization.Format("Delete note: {0}",item.DisplayTitle);UI.Named(selection,selectLabel);selection.ToolTip=selectLabel;UI.Named(delete,deleteLabel);delete.ToolTip=deleteLabel;
                surface.SetResourceReference(Border.BackgroundProperty,selected.Contains(item.Id)?"SelectionFill":"SettingsCard");surface.SetResourceReference(Border.BorderBrushProperty,selected.Contains(item.Id)?"Accent":"SettingsLineSoft");
                string body=item.Text.Trim();if(string.IsNullOrWhiteSpace(item.Title) && body.StartsWith(item.DisplayTitle,StringComparison.Ordinal))body=body[item.DisplayTitle.Length..].Trim();
                excerpt.Text=body.Length==0?Localization.Text("Click to open this note"):body[..Math.Min(160,body.Length)].Replace('\r',' ').Replace('\n',' ');
                if(editor!=null && titleInput!=null && !editor.IsKeyboardFocusWithin && !titleInput.IsKeyboardFocusWithin && !updating && timer?.IsEnabled!=true)
                {
                    if(editor.Text!=item.Text && string.IsNullOrWhiteSpace(editor.Text))return;
                    syncingEditor=true;try{editor.Text=item.Text;titleInput.Text=item.Title;}finally{syncingEditor=false;}
                }
            }

            card.Expanded+=(_,_)=>
            {
                Dispatcher.BeginInvoke(DispatcherPriority.Loaded,()=>{if(card.IsExpanded && card.IsLoaded && surface.IsVisible)surface.BringIntoView();});
                if(editor!=null)return;
                titleInput=UI.CaptionInput("Note title",item.Title,"Leave blank to use the first phrase.");titleInput.MaxLength=ProductivityService.MaxNoteTitleLength;titleInput.Margin=new(0,4,0,10);
                editor=ProductivityEditor("Saved note text",item.Text,150);result=UI.Caption("Saved notes save automatically while you edit.",11,UI.Muted);Accessibility.Live(result);
                timer=new DispatcherTimer{Interval=TimeSpan.FromMilliseconds(350)};timer.Tick+=(_,_)=>{updating=true;try{Flush();}finally{updating=false;}};
                void Changed(){if(syncingEditor)return;timer.Stop();timer.Start();}
                editor.TextChanged+=(_,_)=>Changed();titleInput.TextChanged+=(_,_)=>Changed();
                var actions=new WrapPanel();
                actions.Children.Add(UI.CaptionButton("Save changes",()=>Flush()));
                actions.Children.Add(UI.CaptionButton("Open in quick draft",()=>
                {
                    if(!Flush())return;debounce.Stop();
                    if(service.OpenNoteInDraft(item.Id)){State("Opened in quick draft. Your previous draft was preserved in the library.");draft.Focus();}else Failure();
                }));
                var content=new StackPanel();content.Children.Add(UI.Caption("Note title (optional)",11,UI.Muted));content.Children.Add(titleInput);content.Children.Add(editor);content.Children.Add(actions);content.Children.Add(result);card.Content=content;
            };
            return(surface,card,Refresh,Flush,()=>timer?.Stop());
        }
        void Refresh()
        {
            syncing=true;try{foreach(var (input,text) in new[]{(draft,service.Note),(draftTitle,service.NoteTitle)})if(input.Text!=text){int caret=input.CaretIndex;input.Text=text;input.CaretIndex=Math.Min(caret,input.Text.Length);}}finally{syncing=false;}
            save.IsEnabled=!string.IsNullOrWhiteSpace(service.Note);
            count.Text=Localization.Format("{0} of {1} saved notes",service.Notes.Count,ProductivityService.MaxSavedNotes);
            var ids=service.Notes.Select(n=>n.Id).ToHashSet();
            selected.IntersectWith(ids);
            foreach(var id in rows.Keys.Where(id=>!ids.Contains(id)).ToArray()){rows[id].Stop();library.Children.Remove(rows[id].Surface);rows.Remove(id);}
            foreach(var item in service.Notes)
            {
                if(!rows.TryGetValue(item.Id,out var row)){row=CreateRow(item);rows[item.Id]=row;library.Children.Insert(service.Notes.IndexOf(item),row.Surface);}
                row.Refresh();
            }
            if(service.Notes.Count==0){library.Children.Clear();library.Children.Add(UI.Caption("No saved notes yet. Write a quick draft and choose Save to library.",12,UI.Muted));}
            else foreach(var empty in library.Children.OfType<TextBlock>().ToArray())library.Children.Remove(empty);
            ArrangeCards();UpdateSelection();
        }
        var libraryHeading=UI.Caption("Saved notes",16);libraryHeading.FontWeight=FontWeights.SemiBold;libraryHeading.Margin=new(0,6,0,6);host.Children.Add(libraryHeading);host.Children.Add(count);
        var libraryHint=UI.Caption("Click a card to open it. Choose a title inside, or leave it blank to use the first phrase.",12,UI.Muted);libraryHint.Margin=new(0,5,0,10);host.Children.Add(libraryHint);
        var selectionActions=new WrapPanel{Margin=new(0,0,0,4)};
        selectAll=UI.CaptionButton("Select all",()=>{selected.UnionWith(service.Notes.Select(n=>n.Id));UpdateSelection();});selectionActions.Children.Add(selectAll);
        clearSelection=UI.CaptionButton("Clear selection",()=>{selected.Clear();UpdateSelection();});selectionActions.Children.Add(clearSelection);
        deleteSelected=UI.CaptionButton("Delete selected notes",()=>DeleteChosen(selected.ToArray()));selectionActions.Children.Add(deleteSelected);
        host.Children.Add(selectionActions);selectionStatus.Margin=new(0,0,0,10);host.Children.Add(selectionStatus);host.Children.Add(library);
        host.Loaded+=(_,_)=>{if(!subscribed){service.Changed+=Refresh;subscribed=true;}Refresh();};
        host.Unloaded+=(_,_)=>
        {
            if(subscribed){service.Changed-=Refresh;subscribed=false;}FlushDraft();
            foreach(var row in rows.Values.ToArray())if(!row.Flush())app.Notify(Localization.Text("Notes"),Localization.Text("Could not save. Your text is still in the editor."));
        };
        Refresh();page.Children.Add(host);
    }

    private void TasksSettings()
    {
        var service=app.Productivity;var host=new StackPanel();var list=new StackPanel();bool subscribed=false;
        var input=UI.CaptionInput("New task","");input.MaxLength=20000;input.SetResourceReference(Control.ForegroundProperty,"ControlText");
        var summary=UI.Caption("",11,UI.Muted);var feedback=UI.Caption("Tasks are shared with the dock and saved on this device.",11,UI.Muted);Accessibility.Live(feedback);
        string signature="";
        void Refresh()
        {
            summary.Text=Localization.Format("{0} open · {1} completed",service.Tasks.Count(t=>!t.Done),service.Tasks.Count(t=>t.Done));
            string next=string.Join("\n",service.Tasks.Select(t=>t.Id+"|"+t.Done+"|"+t.Text));if(next==signature && list.Children.Count>0)return;signature=next;list.Children.Clear();
            foreach(var item in service.Tasks)
            {
                var row=new DockPanel{Margin=new(0,5,0,5)};
                var remove=UI.CaptionButton("Delete task",()=>{if(!service.RemoveTask(item.Id))feedback.Text=Localization.Text("Could not save. Try again.");});DockPanel.SetDock(remove,Dock.Right);row.Children.Add(remove);
                var label=UI.Text(item.Text,13);label.SetResourceReference(TextBlock.ForegroundProperty,"ControlText");if(item.Done)label.TextDecorations=TextDecorations.Strikethrough;
                var toggle=new CheckBox{IsChecked=item.Done,Content=label,VerticalAlignment=VerticalAlignment.Center};
                // A task is a checkbox, not a preference switch inherited from Settings.
                toggle.SetResourceReference(FrameworkElement.StyleProperty,"TaskToggle");UI.Named(toggle,item.Text);
                toggle.Click+=(_,_)=>{if(!service.SetTaskDone(item.Id,toggle.IsChecked==true)){toggle.IsChecked=item.Done;feedback.Text=Localization.Text("Could not save. Try again.");}};
                row.Children.Add(toggle);list.Children.Add(row);
            }
            if(service.Tasks.Count==0)list.Children.Add(UI.Caption("Add your first task below.",12,UI.Muted));
        }
        void Add()
        {
            if(string.IsNullOrWhiteSpace(input.Text))return;
            if(service.AddTask(input.Text)){input.Clear();feedback.Text=Localization.Text("Tasks are shared with the dock and saved on this device.");}
            else feedback.Text=Localization.Text(service.Tasks.Count>=100?"Remove a task before adding another. Your draft is still here.":"Could not save. Your text is still in the editor.");
        }
        var add=UI.CaptionButton("Add task",Add,true);add.IsEnabled=false;input.TextChanged+=(_,_)=>add.IsEnabled=!string.IsNullOrWhiteSpace(input.Text);
        input.KeyDown+=(_,e)=>{if(e.Key==Key.Enter){Add();e.Handled=true;}};
        host.Children.Add(Section("Task list",summary,list));host.Children.Add(Section("New task",input,add,feedback));
        host.Loaded+=(_,_)=>{if(!subscribed){service.Changed+=Refresh;subscribed=true;}Refresh();};
        host.Unloaded+=(_,_)=>{if(subscribed){service.Changed-=Refresh;subscribed=false;}};
        Refresh();page.Children.Add(host);
    }
}
