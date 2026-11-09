using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace NabyScape;
public sealed partial class SettingsWindow
{
    private readonly ResourceDictionary windowPalette=new();
    private readonly List<(string Id,Button Button,Border Frame,Ellipse Radio)> themeChoices=new();
    private Border? windowFrame,sidebarSurface;
    private Grid? windowShell;
    private string? appearanceKey;
    private Action? refreshWindowOutline;
    private void InitializeWindowAppearance()
    {
        SettingsPalette.Hook();app.Settings.NormalizeWindowTheme();
        Resources.MergedDictionaries.Add(windowPalette);
        ApplyWindowAppearance();
    }
    internal void ApplyWindowAppearance()
    {
        var s=app.Settings;s.NormalizeWindowTheme();
        string key=$"{s.WindowTheme}/{s.WindowGlass}/{s.WindowEffects}/{s.WindowLargeCorners}/{s.WindowCompact}/{s.WindowHoverAccents}/{Accessibility.HighContrast}";
        if(key==appearanceKey)return;appearanceKey=key;
        var palette=SettingsPalette.Create(s);foreach(var resource in palette.Keys)windowPalette[resource]=palette[resource];
        if(windowFrame!=null)windowFrame.CornerRadius=new(s.WindowLargeCorners?28:12);
        foreach(var entry in navigationButtons.Values)
        {
            entry.MinHeight=s.WindowCompact?34:46;
            entry.Padding=s.WindowCompact?new(14,7,14,7):new(16,11,16,11);
        }
        foreach(var entry in themeChoices)
        {
            bool selected=entry.Id==s.WindowTheme;
            entry.Frame.SetResourceReference(Border.BorderBrushProperty,selected?"Accent":"SettingsLineSoft");
            entry.Frame.BorderThickness=new(selected?1.5:1);
            entry.Radio.SetResourceReference(Shape.FillProperty,selected?"Accent":"ControlFill");
            System.Windows.Automation.AutomationProperties.SetItemStatus(entry.Button,Localization.Text(selected?"Selected":"Not selected"));
        }
        if(sidebarSurface!=null)sidebarSurface.Effect=null;
        refreshWindowOutline?.Invoke();
        if(windowShell!=null)windowShell.InvalidateVisual();
        materialKey=null;
        if(IsLoaded)ApplyMaterial();
    }
    private static Path WaveMark(double width=30)
    {
        var mark=new Path{Data=Geometry.Parse("M 0,17 C 2,4 10,1 18,7 C 25,13 31,12 38,6 C 36,19 28,23 20,17 C 13,11 6,11 0,17 Z"),Width=width,Height=width*.6,Stretch=Stretch.Uniform,VerticalAlignment=VerticalAlignment.Center};
        mark.SetResourceReference(Shape.FillProperty,"ControlText");return mark;
    }
    private void WindowThemeControls()
    {
        themeChoices.Clear();var choices=new Grid{Margin=new(0,14,0,0)};choices.ColumnDefinitions.Add(new());choices.ColumnDefinitions.Add(new());
        int column=0;
        foreach(string id in new[]{"Light","Dark"})
        {
            bool light=id=="Light";
            var miniature=new Grid{MaxWidth=160,Height=86,Margin=new(0,6,0,10)};
            var miniFrame=new Border{Background=UI.Brush(light?"#DDDDDD":"#242424"),BorderBrush=UI.Brush(light?"#EEEEEE":"#666666"),BorderThickness=new(1),CornerRadius=new(10)};
            miniFrame.Effect=new System.Windows.Media.Effects.DropShadowEffect{BlurRadius=10,ShadowDepth=3,Opacity=.18};
            miniature.Children.Add(miniFrame);var windowButtons=new TextBlock{Text="−  □  ×",FontSize=8,Foreground=UI.Brush(light?"#555555":"#CCCCCC"),HorizontalAlignment=HorizontalAlignment.Right,VerticalAlignment=VerticalAlignment.Top,Margin=new(0,3,8,0)};miniature.Children.Add(windowButtons);miniature.SetValue(SettingsPalette.PreserveProperty,true);
            var mini=new Grid{Margin=new(9,16,9,9)};mini.ColumnDefinitions.Add(new(){Width=new GridLength(40)});mini.ColumnDefinitions.Add(new());
            var items=new StackPanel{Margin=new(0,0,8,0)};for(int i=0;i<3;i++)items.Children.Add(new Border{Height=12,CornerRadius=new(4),Background=UI.Brush(light?"#F5F5F5":"#555555"),Margin=new(0,0,0,5)});mini.Children.Add(items);
            var panel=new Border{Background=UI.Brush(light?"#F1F1F1":"#454545"),CornerRadius=new(9),Margin=new(2,8,0,0)};Grid.SetColumn(panel,1);mini.Children.Add(panel);miniature.Children.Add(mini);
            var label=new StackPanel{Orientation=Orientation.Horizontal,HorizontalAlignment=HorizontalAlignment.Center};
            var radio=new Ellipse{Width=15,Height=15,StrokeThickness=1,Margin=new(0,0,9,0)};radio.SetResourceReference(Shape.StrokeProperty,"ControlText");label.Children.Add(radio);
            var title=UI.Caption(light?"Light theme":"Dark theme",13);title.FontWeight=FontWeights.SemiBold;label.Children.Add(title);
            var body=new StackPanel();body.Children.Add(miniature);body.Children.Add(label);
            var frame=new Border{Child=body,CornerRadius=new(18),Padding=new(8,6,8,12),BorderThickness=new(1)};
            var button=UI.CaptionButton(light?"Light theme":"Dark theme",()=>{app.Settings.WindowTheme=id;ApplyWindowAppearance();Save();});
            button.Content=frame;button.Padding=new(0);button.Margin=new(column==0?0:6,0,column==0?6:0,0);button.Background=Brushes.Transparent;button.HorizontalContentAlignment=HorizontalAlignment.Stretch;Grid.SetColumn(button,column++);choices.Children.Add(button);themeChoices.Add((id,button,frame,radio));
        }
        var hint=UI.Caption("Choose the appearance of the app interface.",12,SettingsTheme.Quiet);
        page.Children.Add(Section("Window theme",hint,choices));
        Border Row(string title,string description,FrameworkElement control)
        {
            var text=new StackPanel{VerticalAlignment=VerticalAlignment.Center};var name=UI.Caption(title,13);name.FontWeight=FontWeights.SemiBold;text.Children.Add(name);text.Children.Add(UI.Caption(description,11,SettingsTheme.Quiet));
            var row=SettingRow(text,control,190);row.Margin=new(0);
            var card=new Border{Tag="Window appearance",Child=row,Background=SettingsTheme.Card,BorderBrush=SettingsTheme.LineSoft,BorderThickness=new(1),Margin=new(0,0,0,6)};card.SetResourceReference(Border.CornerRadiusProperty,"SettingsCardRadius");card.SetResourceReference(Border.PaddingProperty,"SettingsRowPadding");return card;
        }
        void Changed(){ApplyWindowAppearance();Save();}
        var glass=new Slider{Minimum=0,Maximum=100,Value=app.Settings.WindowGlass,TickFrequency=1,IsSnapToTickEnabled=true,Width=135,VerticalAlignment=VerticalAlignment.Center,Margin=new(0,0,12,0)};UI.LocalizedName(glass,"Transparency and blur");
        var percent=UI.Text(app.Settings.WindowGlass.ToString("0")+" %",11);percent.VerticalAlignment=VerticalAlignment.Center;
        glass.ValueChanged+=(_,_)=>{app.Settings.WindowGlass=glass.Value;app.Settings.SettingsMaterial=glass.Value>0?"Native Acrylic":"Solid";percent.Text=glass.Value.ToString("0")+" %";Changed();};
        page.Children.Add(Row("Transparency and blur","Softness and depth of the window surface.",UI.Row(glass,percent)));
        var corners=new ComboBox{ItemsSource=new[]{Localization.Text("Large corners"),Localization.Text("Small corners")},SelectedIndex=app.Settings.WindowLargeCorners?0:1,Width=156,HorizontalAlignment=HorizontalAlignment.Right};UI.LocalizedName(corners,"Window corners");corners.SelectionChanged+=(_,_)=>{app.Settings.WindowLargeCorners=corners.SelectedIndex==0;Changed();};
        page.Children.Add(Row("Window corners","Corner radius of the window and interface elements.",corners));
        void Toggle(string title,string description,bool initial,Action<bool> set)
        {
            var toggle=new CheckBox{IsChecked=initial,HorizontalAlignment=HorizontalAlignment.Right,Width=46};var state=UI.Caption(initial?"On":"Off",11);state.Margin=new(12,0,0,0);state.MinWidth=68;UI.LocalizedName(toggle,title);toggle.Click+=(_,_)=>{set(toggle.IsChecked==true);state.Text=Localization.Text(toggle.IsChecked==true?"On":"Off");Changed();};var controls=UI.Row(toggle,state);controls.HorizontalAlignment=HorizontalAlignment.Right;page.Children.Add(Row(title,description,controls));
        }
        Toggle("Interface effects","Soft depth and glass-like surfaces.",app.Settings.WindowEffects,v=>app.Settings.WindowEffects=v);
        Toggle("Compact mode","Smaller navigation rows and tighter spacing.",app.Settings.WindowCompact,v=>app.Settings.WindowCompact=v);
        Toggle("Show hover accents","Subtle highlights on buttons and menu items.",app.Settings.WindowHoverAccents,v=>app.Settings.WindowHoverAccents=v);
        appearanceKey=null;ApplyWindowAppearance();
    }
}
