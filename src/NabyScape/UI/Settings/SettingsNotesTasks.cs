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
        var draft=ProductivityEditor("Quick draft",service.Note,150);
        var feedback=UI.Caption("Draft saves automatically on this device.",11,UI.Muted);Accessibility.Live(feedback);
        var debounce=new DispatcherTimer{Interval=TimeSpan.FromMilliseconds(350)};bool syncing=false,subscribed=false;
        var rows=new Dictionary<string,(Expander Card,Action Refresh,Func<bool> Flush)>();
        var library=new StackPanel();var count=UI.Caption("",11,UI.Muted);
        void State(string message)=>feedback.Text=Localization.Text(message);
        void Failure()=>State(service.LastError!=null?"Could not save. Your text is still in the editor.":"The note library is full. Delete a saved note before adding another.");
        bool FlushDraft(){debounce.Stop();bool saved=service.Save();State(saved?"Draft saves automatically on this device.":"Could not save. Your text is still in the editor.");return saved;}
        debounce.Tick+=(_,_)=>FlushDraft();
        draft.TextChanged+=(_,_)=>{if(syncing)return;service.Note=draft.Text;debounce.Stop();debounce.Start();};
        var save=UI.CaptionButton("Save to library",()=>{debounce.Stop();if(service.SaveDraftToLibrary())State("Note saved to your library. The quick draft is unchanged.");else Failure();},true);
        var draftSection=Section("Quick draft",UI.Caption("The same draft appears in the dock and detached notes. Save a copy to keep it in your library.",12,UI.Muted),draft,save,feedback);
        host.Children.Add(draftSection);

        (Expander Card,Action Refresh,Func<bool> Flush) CreateRow(NoteItem item)
        {
            var title=UI.Text(item.Label,13);title.SetResourceReference(TextBlock.ForegroundProperty,"ControlText");
            var card=new Expander{Header=title,Margin=new(0,6,0,6)};UI.Named(card,item.Label);
            TextBox? editor=null;TextBlock? result=null;DispatcherTimer? timer=null;bool updating=false;
            bool Flush()
            {
                timer?.Stop();if(editor==null || editor.Text==item.Text)return true;
                bool okay=service.UpdateNote(item.Id,editor.Text);
                if(result!=null)result.Text=Localization.Text(okay?"Saved on this device":string.IsNullOrWhiteSpace(editor.Text)?"A saved note cannot be empty. Use Delete note to remove it.":"Could not save. Your text is still in the editor.");
                return okay;
            }
            void Refresh()
            {
                title.Text=item.Label;UI.Named(card,item.Label);
                if(editor!=null && !editor.IsKeyboardFocusWithin && !updating && timer?.IsEnabled!=true && editor.Text!=item.Text && string.IsNullOrWhiteSpace(editor.Text))return;
            }
            card.Expanded+=(_,_)=>
            {
                if(editor!=null)return;
                editor=ProductivityEditor("Saved note text",item.Text,150);result=UI.Caption("Saved notes save automatically while you edit.",11,UI.Muted);Accessibility.Live(result);
                timer=new DispatcherTimer{Interval=TimeSpan.FromMilliseconds(350)};timer.Tick+=(_,_)=>{updating=true;try{Flush();}finally{updating=false;}};
                editor.TextChanged+=(_,_)=>{timer.Stop();timer.Start();};
                var actions=new WrapPanel();
                actions.Children.Add(UI.CaptionButton("Save changes",()=>Flush()));
                actions.Children.Add(UI.CaptionButton("Open in quick draft",()=>
                {
                    if(!Flush())return;debounce.Stop();
                    if(service.OpenNoteInDraft(item.Id)){State("Opened in quick draft. Your previous draft was preserved in the library.");draft.Focus();}else Failure();
                }));
                actions.Children.Add(UI.CaptionButton("Delete note",()=>
                {
                    if(MessageBox.Show(this,Localization.Text("Delete this saved note? The quick draft will be kept."),Localization.Text("Delete note"),MessageBoxButton.YesNo,MessageBoxImage.Question)!=MessageBoxResult.Yes)return;
                    timer.Stop();if(!service.RemoveNote(item.Id))State("Could not save. Your text is still in the editor.");
                }));
                var content=new StackPanel();content.Children.Add(editor);content.Children.Add(actions);content.Children.Add(result);card.Content=content;
            };
            return(card,Refresh,Flush);
        }
        void Refresh()
        {
            syncing=true;try{if(draft.Text!=service.Note){int caret=draft.CaretIndex;draft.Text=service.Note;draft.CaretIndex=Math.Min(caret,draft.Text.Length);}}finally{syncing=false;}
            save.IsEnabled=!string.IsNullOrWhiteSpace(service.Note);
            count.Text=Localization.Format("{0} of {1} saved notes",service.Notes.Count,ProductivityService.MaxSavedNotes);
            var ids=service.Notes.Select(n=>n.Id).ToHashSet();
            foreach(var id in rows.Keys.Where(id=>!ids.Contains(id)).ToArray()){library.Children.Remove(rows[id].Card);rows.Remove(id);}
            foreach(var item in service.Notes)
            {
                if(!rows.TryGetValue(item.Id,out var row)){row=CreateRow(item);rows[item.Id]=row;library.Children.Insert(service.Notes.IndexOf(item),row.Card);}
                row.Refresh();
            }
            if(service.Notes.Count==0){library.Children.Clear();library.Children.Add(UI.Caption("No saved notes yet. Write a quick draft and choose Save to library.",12,UI.Muted));}
            else foreach(var empty in library.Children.OfType<TextBlock>().ToArray())library.Children.Remove(empty);
        }
        host.Children.Add(Section("Saved notes",count,UI.Caption("Open a note to edit it. Opening it in the quick draft preserves your previous draft in the library first.",12,UI.Muted),library));
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
                var remove=UI.CaptionButton("Delete task",()=>service.RemoveTask(item.Id));DockPanel.SetDock(remove,Dock.Right);row.Children.Add(remove);
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
            else feedback.Text=Localization.Text("Remove a task before adding another. Your draft is still here.");
        }
        var add=UI.CaptionButton("Add task",Add,true);add.IsEnabled=false;input.TextChanged+=(_,_)=>add.IsEnabled=!string.IsNullOrWhiteSpace(input.Text);
        input.KeyDown+=(_,e)=>{if(e.Key==Key.Enter){Add();e.Handled=true;}};
        host.Children.Add(Section("Task list",summary,list));host.Children.Add(Section("New task",input,add,feedback));
        host.Loaded+=(_,_)=>{if(!subscribed){service.Changed+=Refresh;subscribed=true;}Refresh();};
        host.Unloaded+=(_,_)=>{if(subscribed){service.Changed-=Refresh;subscribed=false;}};
        Refresh();page.Children.Add(host);
    }
}
