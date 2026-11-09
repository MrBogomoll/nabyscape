using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
namespace NabyScape;
public sealed partial class SettingsWindow
{
    private sealed record OpenExceptionApp(string Executable,string Title)
    {public override string ToString()=>Title+" · "+Executable;}
    private void HideExceptionSettings()
    {
        var s=app.Settings;var content=new StackPanel();
        content.Children.Add(UI.Caption("Selected apps keep the dock visible when maximized or fullscreen. Other apps still follow your hiding rules. Manual hiding and Always tucked take priority.",12,UI.Muted));
        var entries=new StackPanel{Margin=new(0,12,0,10)};content.Children.Add(entries);
        var feedback=UI.Caption("",11,UI.Muted);Accessibility.Live(feedback);
        void Refresh()
        {
            entries.Children.Clear();
            if(s.HideAppExceptions.Count==0)entries.Children.Add(UI.Caption("No app exceptions yet.",12,UI.Muted));
            foreach(string executable in s.HideAppExceptions.ToArray())
            {
                var row=new DockPanel{Margin=new(0,3,0,3)};
                var remove=UI.CaptionButton("Remove",()=>{s.HideAppExceptions.Remove(executable);Save();Refresh();});DockPanel.SetDock(remove,Dock.Right);row.Children.Add(remove);var label=UI.Text(executable,13);label.FontWeight=FontWeights.SemiBold;row.Children.Add(label);entries.Children.Add(new Border{Child=row,Background=SettingsTheme.LimeWash,BorderBrush=SettingsTheme.Line,BorderThickness=new(3,0,0,0),CornerRadius=new(8),Padding=new(12,3,8,3),Margin=new(0,3,0,3)});
            }
        }
        void Add(string value)
        {
            string name=VisibilityExceptionPolicy.NormalizeName(value);
            if(name.Length==0){Accessibility.Announce(feedback,Localization.Text("Enter an executable name, such as ChatGPT.exe or explorer.exe."));return;}
            if(s.HideAppExceptions.Contains(name,StringComparer.OrdinalIgnoreCase)){Accessibility.Announce(feedback,Localization.Text("This app is already in the exception list."));return;}
            if(s.HideAppExceptions.Count>=100){Accessibility.Announce(feedback,Localization.Text("You can add up to 100 app exceptions."));return;}
            s.HideAppExceptions.Add(name);Save();Refresh();Accessibility.Announce(feedback,Localization.Text("App exception added."));
        }
        Refresh();
        var apps=new ComboBox{MinHeight=34};UI.LocalizedName(apps,"Open application");
        void LoadApps()
        {
            var list=new List<OpenExceptionApp>();
            foreach(var process in Process.GetProcesses())using(process)
            {
                try{if(process.Id==Environment.ProcessId || process.MainWindowHandle==IntPtr.Zero || string.IsNullOrWhiteSpace(process.MainWindowTitle))continue;string name=VisibilityExceptionPolicy.NormalizeName(process.ProcessName);if(name.Length>0)list.Add(new(name,process.MainWindowTitle));}
                catch(Exception ex) when(ex is InvalidOperationException or System.ComponentModel.Win32Exception or NotSupportedException){}
            }
            apps.ItemsSource=list.DistinctBy(a=>a.Executable,StringComparer.OrdinalIgnoreCase).OrderBy(a=>a.Title).ToArray();apps.SelectedIndex=apps.Items.Count>0?0:-1;
        }
        LoadApps();content.Children.Add(UI.Caption("Choose an open application",12));content.Children.Add(apps);
        content.Children.Add(UI.Row(UI.CaptionButton("Add selected app",()=>{if(apps.SelectedItem is OpenExceptionApp selected)Add(selected.Executable);}),UI.CaptionButton("Refresh app list",LoadApps)));
        content.Children.Add(UI.Caption("Or enter its executable name",12));var input=UI.CaptionInput("Executable name","","For example: ChatGPT.exe or explorer.exe");content.Children.Add(input);
        content.Children.Add(UI.Row(UI.CaptionButton("Add exception",()=>Add(input.Text)),UI.CaptionButton("Choose executable…",()=>{var picker=new Microsoft.Win32.OpenFileDialog{Title=Localization.Text("Choose an application executable"),Filter=Localization.Text("Applications (*.exe)|*.exe"),CheckFileExists=true};if(picker.ShowDialog(this)==true)Add(Path.GetFileName(picker.FileName));})));
        content.Children.Add(feedback);page.Children.Add(Section("Hide exceptions",content));
    }
}
