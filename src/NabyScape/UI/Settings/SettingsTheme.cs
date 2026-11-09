using System;
using System.Windows;
using System.Windows.Media;
namespace NabyScape;

// Settings chrome uses a neutral monochrome palette, independent of the island colors.
// These resources are scoped to the Settings chrome so the live island preview keeps the
// user's own island colors.
internal static class SettingsTheme
{
    internal static readonly Brush Void=Frozen("#141414");
    internal static readonly Brush Sidebar=Frozen("#1D1D1D");
    internal static readonly Brush Card=Frozen("#252525");
    internal static readonly Brush CardRaised=Frozen("#303030");
    internal static readonly Brush Line=Frozen("#515151");
    internal static readonly Brush LineSoft=Frozen("#393939");
    internal static readonly Brush Bone=Frozen("#F3F3F3");
    internal static readonly Brush Quiet=Frozen("#B0B0B0");
    internal static readonly Brush Lime=Frozen("#F1F1F1");
    internal static readonly Brush LimeInk=Frozen("#171717");
    internal static readonly Brush LimeWash=Frozen("#454545");
    internal static readonly Brush LimeGlow=Frozen("#33FFFFFF");
    internal static readonly FontFamily Display=new(new Uri("pack://application:,,,/"),"./Assets/Fonts/#Manrope, Segoe UI Variable Display, Segoe UI");
    internal static readonly FontFamily Body=new("Segoe UI Variable Text, Segoe UI");
    internal static readonly FontFamily Mono=new("Cascadia Mono, Consolas");
    internal static ResourceDictionary PreviewResources()
    {
        var d=new ResourceDictionary();
        foreach(string key in new[]{"Accent","AccentText","CheckedFill","CheckedText","SwitchKnob","TrackFill","ProgressFill","ProgressTrack","ControlFill","ControlBorder","ControlText","BodyText","MutedText","SelectionFill","SelectionText","HoverOverlay"})
            if(Application.Current?.TryFindResource(key) is Brush brush)d[key]=brush;
        return d;
    }
    private static Brush Frozen(string hex){var brush=UI.Brush(hex).CloneCurrentValue();brush.Freeze();return brush;}
    // Control template resources (see Theme.xaml) re-pointed for Settings only.
    internal static ResourceDictionary ChromeResources()
    {
        var d=new ResourceDictionary();
        if(Accessibility.HighContrast)return d;
        d["Accent"]=Lime;d["AccentText"]=LimeInk;
        d["CheckedFill"]=Lime;d["CheckedText"]=LimeInk;d["SwitchKnob"]=Frozen("#C9C9C9");
        d["TrackFill"]=Frozen("#404040");d["ProgressFill"]=Lime;d["ProgressTrack"]=Frozen("#404040");
        d["ControlFill"]=Frozen("#303030");d["ControlBorder"]=Frozen("#555555");d["ControlText"]=Bone;
        d["SelectionFill"]=LimeWash;d["SelectionText"]=Bone;
        return d;
    }
}
