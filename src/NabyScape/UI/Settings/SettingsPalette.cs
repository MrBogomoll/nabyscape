using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace NabyScape;

// Resources belong to one window. Never replace application/island resources.
internal static class SettingsPalette
{
    internal static readonly DependencyProperty PreserveProperty=DependencyProperty.RegisterAttached("Preserve",typeof(bool),typeof(SettingsPalette),new FrameworkPropertyMetadata(false,FrameworkPropertyMetadataOptions.Inherits));
    private static bool hooked;
    internal static void Hook()
    {
        if(hooked)return;hooked=true;
        EventManager.RegisterClassHandler(typeof(FrameworkElement),FrameworkElement.LoadedEvent,new RoutedEventHandler(OnLoaded),true);
    }
    internal static void BindTree(DependencyObject root)
    {
        if((bool)root.GetValue(PreserveProperty))return;
        if(root is FrameworkElement element)BindElement(element);
        foreach(var child in LogicalTreeHelper.GetChildren(root))if(child is DependencyObject node)BindTree(node);
    }
    private static void OnLoaded(object sender,RoutedEventArgs args)
    {
        if(sender is not FrameworkElement element || (bool)element.GetValue(PreserveProperty) || Window.GetWindow(element) is not (SettingsWindow or WelcomeWindow))return;
        BindElement(element);
    }
    private static void BindElement(FrameworkElement element)
    {
        // Bind the legacy construction helpers to semantic window resources once.
        // New controls and popup descendants receive the same treatment on load.
        var values=element.GetLocalValueEnumerator();var replacements=new List<(DependencyProperty,string)>();
        while(values.MoveNext())
        {
            var entry=values.Current;if(entry.Value is not Brush brush)continue;
            string? key=Role(brush);
            if(key==null && entry.Property.Name is "Foreground" or "CaretBrush")
            {
                if(ReferenceEquals(brush,UI.Muted))key="SettingsQuiet";
                else if(brush is SolidColorBrush color && color.Color.ToString() is "#FFF2F3F5" or "#FFF2F3F6" or "#FFECEEF3" or "#FFFFFFFF")key="ControlText";
            }
            if(key!=null)replacements.Add((entry.Property,key));
        }
        foreach(var (property,key) in replacements)element.SetResourceReference(property,key);
    }
    private static string? Role(Brush brush)
    {
        if(ReferenceEquals(brush,SettingsTheme.Void))return "SettingsVoid";
        if(ReferenceEquals(brush,SettingsTheme.Sidebar))return "SettingsSidebar";
        if(ReferenceEquals(brush,SettingsTheme.Card))return "SettingsCard";
        if(ReferenceEquals(brush,SettingsTheme.CardRaised))return "SettingsRaised";
        if(ReferenceEquals(brush,SettingsTheme.Line))return "SettingsLine";
        if(ReferenceEquals(brush,SettingsTheme.LineSoft))return "SettingsLineSoft";
        if(ReferenceEquals(brush,SettingsTheme.Bone))return "ControlText";
        if(ReferenceEquals(brush,SettingsTheme.Quiet))return "SettingsQuiet";
        if(ReferenceEquals(brush,SettingsTheme.Lime))return "Accent";
        if(ReferenceEquals(brush,SettingsTheme.LimeInk))return "AccentText";
        if(ReferenceEquals(brush,SettingsTheme.LimeWash))return "SettingsAccentWash";
        if(ReferenceEquals(brush,SettingsTheme.LimeGlow))return "SettingsGlow";
        return null;
    }
    internal static ResourceDictionary Create(Settings s,bool native=false)
    {
        bool light=s.WindowTheme!="Dark";var d=new ResourceDictionary();
        void Add(string key,string color)=>d[key]=UI.Brush(color);
        Add("SettingsVoid",light?"#DEDEDE":"#242424");
        Add("SettingsSidebar",light?"#CFFFFFFF":"#28FFFFFF");
        Add("SettingsCard",light?"#C8FFFFFF":"#24FFFFFF");Add("SettingsRaised",light?"#F4F4F4":"#424242");
        Add("SettingsLine",light?"#D9FFFFFF":"#70FFFFFF");Add("SettingsLineSoft",light?"#80FFFFFF":"#3AFFFFFF");
        Add("ControlText",light?"#171717":"#F5F5F5");Add("SettingsQuiet",light?"#505050":"#CBCBCB");
        d["MutedText"]=d["SettingsQuiet"];
        d["BodyText"]=d["ControlText"];
        Add("Accent",light?"#101010":"#F2F2F2");Add("AccentText",light?"#FFFFFF":"#151515");
        d["CheckedFill"]=d["Accent"];d["CheckedText"]=d["AccentText"];
        Add("SwitchKnob",light?"#909090":"#DDDDDD");Add("TrackFill",light?"#A2A2A2":"#777777");
        d["ProgressFill"]=d["Accent"];d["ProgressTrack"]=d["TrackFill"];
        Add("ControlFill",light?"#E6EAED":"#50565C");Add("ControlBorder",light?"#A5ADB3":"#747C84");
        Add("SelectionFill",light?"#DDDDDD":"#555555");d["SelectionText"]=d["ControlText"];
        Add("SettingsAccentWash",light?"#E2E2E2":"#505050");Add("SettingsGlow",light?"#16000000":"#24FFFFFF");
        d["SettingsSelected"]=d["Accent"];d["SettingsSelectedText"]=d["AccentText"];
        Add("HoverOverlay",!s.WindowHoverAccents?"#00000000":light?"#14000000":"#19FFFFFF");
        d["SettingsCardRadius"]=new CornerRadius(s.WindowLargeCorners?22:10);
        var glass=new LinearGradientBrush{StartPoint=new(0,0),EndPoint=new(1,1)};
        string[] colors=light?new[]{"#E8E8E8","#D0D0D0","#F2F2F2","#D6D6D6"}:new[]{"#3F3F3F","#232323","#353535","#1D1D1D"};
        double strength=s.WindowEffects?s.WindowGlass/100:0;
        for(int i=0;i<colors.Length;i++)
        {
            var color=UI.Brush(colors[i]).Color;var flat=UI.Brush(light?"#E4E4E4":"#292929").Color;
            byte Mix(byte a,byte b)=>(byte)(a*strength+b*(1-strength));
            byte alpha=native && !Accessibility.HighContrast?(byte)(255*(light?.94-.3*strength:.96-.2*strength)):(byte)255;
            glass.GradientStops.Add(new(Color.FromArgb(alpha,Mix(color.R,flat.R),Mix(color.G,flat.G),Mix(color.B,flat.B)),i/(double)(colors.Length-1)));
        }
        glass.Freeze();d["SettingsCanvas"]=glass;
        d["SettingsRowPadding"]=s.WindowCompact?new Thickness(18,5,16,5):new Thickness(20,10,18,10);
        Add("PopupFill",light?"#F7F7F7":"#303030");Add("SettingsFooter",light?"#20FFFFFF":"#12000000");
        Add("SettingsCaption",light?"#40FFFFFF":"#25000000");
        if(Accessibility.HighContrast)
        {
            foreach(string key in new[]{"SettingsVoid","SettingsSidebar","SettingsCard","SettingsRaised","SettingsCanvas","SettingsCaption","ControlFill"})d[key]=SystemColors.WindowBrush;
            foreach(string key in new[]{"ControlText","BodyText","SettingsQuiet","MutedText","SettingsLine","SettingsLineSoft","ControlBorder"})d[key]=SystemColors.WindowTextBrush;
            d["Accent"]=d["CheckedFill"]=d["SettingsSelected"]=d["SelectionFill"]=SystemColors.HighlightBrush;
            d["AccentText"]=d["CheckedText"]=d["SettingsSelectedText"]=d["SelectionText"]=SystemColors.HighlightTextBrush;
        }
        return d;
    }
}
