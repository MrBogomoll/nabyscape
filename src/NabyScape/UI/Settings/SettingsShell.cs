using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
namespace NabyScape;

public sealed partial class SettingsWindow
{
    // Object tags: Settings diagnostics treat string-tagged borders as setting sections.
    private static readonly object NavIndicatorTag=new();
    private const double SidebarWidth=252,MaxContentWidth=1400,StageHeight=160;

    // Chrome Naby beside the stacked NABY / SCAPE wordmark used on the website.
    private FrameworkElement SidebarBrand()
    {
        var row=new Grid{Margin=new(22,12,16,20)};row.ColumnDefinitions.Add(new(){Width=GridLength.Auto});row.ColumnDefinitions.Add(new());
        var naby=BrandMark.Create(38);naby.Margin=new(0,0,12,0);row.Children.Add(naby);
        var words=new StackPanel{VerticalAlignment=VerticalAlignment.Center};Grid.SetColumn(words,1);
        TextBlock Word(string text,Brush color){var t=new TextBlock{Text=text,FontFamily=SettingsTheme.Display,FontWeight=FontWeights.Bold,FontSize=17,Foreground=color,LineHeight=17,LineStackingStrategy=LineStackingStrategy.BlockLineHeight};return t;}
        words.Children.Add(Word("NABY",SettingsTheme.Bone));words.Children.Add(Word("SCAPE",SettingsTheme.Quiet));
        row.Children.Add(words);
        System.Windows.Automation.AutomationProperties.SetName(row,"NabyScape");
        return row;
    }
    private FrameworkElement SidebarFooter()
    {
        var footer=new StackPanel{Margin=new(0)};
        var border=new Border{BorderBrush=SettingsTheme.LineSoft,BorderThickness=new(0,1,0,0),Padding=new(22,10,18,10),Child=footer};
        if(showAllSwitch!=null){if(showAllSwitch.Parent is Panel old)old.Children.Remove(showAllSwitch);showAllSwitch.Margin=new(0);footer.Children.Add(showAllSwitch);}
        
        return border;
    }
    private Border StageFrame(Border previewStage,StackPanel controls,Expander advanced)
    {
        var body=new Grid();body.ColumnDefinitions.Add(new());body.ColumnDefinitions.Add(new(){Width=new GridLength(145)});
        body.Children.Add(previewStage);Grid.SetColumn(controls,1);body.Children.Add(controls);
        var column=new StackPanel();column.Children.Add(body);column.Children.Add(advanced);
        var frame=new Border{Background=SettingsTheme.CardRaised,BorderBrush=SettingsTheme.Line,BorderThickness=new(.8),CornerRadius=new(16),Child=column};
        column.SizeChanged+=(_,e)=>column.Clip=new RectangleGeometry(new Rect(e.NewSize),15,15);
        return frame;
    }
    private void UpdatePreviewStageHeight()
    {
        if(previewStage==null)return;
        previewStage.Height=StageHeight;
    }
    private static Brush Brush(string hex)=>UI.Brush(hex);
    private static string CategoryEyebrow(string id)=>Localization.Text(id=="Home"?"Welcome":Category(id).Group).ToUpperInvariant();
    private void StyleNavigationSelection(string selected)
    {
        foreach(var entry in navigationButtons)
        {
            bool on=entry.Key==selected;
            entry.Value.FontWeight=on?FontWeights.SemiBold:FontWeights.Normal;
            if(on)entry.Value.SetResourceReference(Control.BackgroundProperty,"SettingsSelected");else entry.Value.Background=Brushes.Transparent;
            entry.Value.SetResourceReference(Control.ForegroundProperty,on?"SettingsSelectedText":"ControlText");
            if(entry.Value.Content is Grid row)foreach(var marker in row.Children.OfType<Border>().Where(b=>ReferenceEquals(b.Tag,NavIndicatorTag)))marker.Visibility=Visibility.Collapsed;
        }
    }
}
