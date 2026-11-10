using System;
using System.Linq;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Media;
namespace NabyScape;
public sealed partial class SettingsWindow
{
    private static System.Collections.Generic.IEnumerable<DependencyObject> SettingTree(DependencyObject node)
    {foreach(var child in LogicalTreeHelper.GetChildren(node).OfType<DependencyObject>()){yield return child;foreach(var item in SettingTree(child))yield return item;}}
    private void OrganizeSettings()
    {
        // The page title and description live above the form, outside its scroll area.
        var sections=page.Children.OfType<Border>().Where(b=>b.Tag is string).ToArray();
        sectionIndexHost.Child=null;sectionIndexHost.Visibility=Visibility.Collapsed;
        foreach(var box in SettingTree(page).OfType<ComboBox>())
        {
            if(current is not ("AI" or "Weather"))UI.LocalizeOptions(box);string name=UI.Identity(box);if(name.Length==0)continue;
            string help=SettingDescription(name);if(help.Length>0)box.ToolTip=Localization.Text(help);
        }
        foreach(var section in sections)
        {
            if(section.Child is not StackPanel content || content.Children.Count==0)continue;
            var title=(FrameworkElement)content.Children[0];content.Children.RemoveAt(0);
            var head=new DockPanel{Margin=new(0,0,0,12)};
            title.Margin=new(0);head.Children.Add(title);content.Children.Insert(0,head);
            AddSectionHint(content,(string)section.Tag);
            CompactSettingRows(section);
        }
        // Every settings section remains directly visible; the sidebar supplies navigation.
    }

    private FrameworkElement VisibilityExample()
    {
        var panel=new StackPanel();var scene=new Grid{Height=108,ClipToBounds=true,Background=UI.Brush("#24272D"),Margin=new(0,0,0,12)};scene.SetValue(SettingsPalette.PreserveProperty,true);
        var window=new Border{Margin=new(9,17,9,8),CornerRadius=new(5),Background=UI.Brush("#17191D"),Child=UI.Caption("Your maximized app",11,UI.Muted)};((TextBlock)window.Child).HorizontalAlignment=HorizontalAlignment.Center;scene.Children.Add(window);
        bool tuck=app.Settings.AlwaysTucked || app.Settings.HideFullscreen || app.Settings.HideMaximized;double offset=23-app.Settings.TuckEdge;
        var slide=new TranslateTransform(0,tuck?-offset:0);var island=new Border{Width=110,Height=23,CornerRadius=new(0,0,9,9),VerticalAlignment=VerticalAlignment.Top,HorizontalAlignment=HorizontalAlignment.Center,Background=Brushes.Black,RenderTransform=slide};
        var time=UI.Text(DateTime.Now.ToString("HH:mm"),10);time.FontWeight=FontWeights.Bold;time.HorizontalAlignment=HorizontalAlignment.Center;island.Child=time;scene.Children.Add(island);
        scene.MouseEnter+=(_,_)=>{slide.BeginAnimation(TranslateTransform.YProperty,null);slide.Y=0;};scene.MouseLeave+=(_,_)=>slide.Y=tuck?-offset:0;
        panel.Children.Add(scene);panel.Children.Add(UI.Text(tuck?Localization.Format("Hover to reveal · {0} DIP tucked edge. Manual hide removes it entirely.",app.Settings.TuckEdge.ToString("0")):Localization.Text("Auto-tuck is off. The island stays visible."),12));
        panel.Children.Add(UI.Text(Localization.Format("Fullscreen: {0} · Maximized: {1}",Localization.Text(app.Settings.HideFullscreen?"tuck":"visible"),Localization.Text(app.Settings.HideMaximized?"tuck":"visible")),11,UI.Muted));return panel;
    }
}
