using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
namespace NabyScape;
internal static class CompactDockPresentation
{
    internal static string TimedKind(Settings settings,SessionState session,DateTimeOffset now)
    {
        bool On(string id)=>settings.Widgets.Any(w=>w.Id==id && w.Enabled);
        if(On("focus") && session.Focus.Remaining(now)>0)return "Focus";
        if(On("timer") && session.Timer.Remaining(now)>0)return "Timer";
        return On("stopwatch") && session.Stopwatch.Running?"Stopwatch":"";
    }
    internal static string TimedText(Settings settings,SessionState session,DateTimeOffset now)=>TimedKind(settings,session,now) switch
    {
        "Focus"=>(session.Focus.Phase=="break"?Localization.Text("Break  "):Localization.Text("Focus  "))+session.Focus.Display(now),
        "Timer"=>Localization.Text("Timer  ")+session.Timer.Display(now),
        "Stopwatch"=>Localization.Text("Stopwatch")+"  "+session.Stopwatch.Display,
        _=>""
    };
    internal static double ContentWidth(double surfaceWidth,double panelWidth,bool notch)=>Math.Max(120,Math.Min(surfaceWidth,panelWidth)-(notch?32:12));
    internal static double DateRoom(double contentWidth,double clockWidth)=>contentWidth-28-2*Math.Max(60,clockWidth+10);
    internal static bool ShowClock(bool enabled,bool idle,bool feedback)=>!feedback && (enabled || idle);
    internal static void FillDate(TextBlock target,DateTime day)
    {
        var culture=Localization.Culture;target.Inlines.Clear();
        target.Inlines.Add(new Run(day.ToString("ddd",culture).ToUpperInvariant()){FontSize=9*Accessibility.TextScale,Foreground=Accessibility.HighContrast?SystemColors.WindowTextBrush:UI.Brush("#9DA3AD")});
        target.Inlines.Add(new Run("  ·  "){FontSize=9*Accessibility.TextScale,Foreground=Accessibility.HighContrast?SystemColors.WindowTextBrush:UI.Brush("#626873")});
        target.Inlines.Add(new Run(day.ToString("dd",culture)){FontSize=12*Accessibility.TextScale,FontWeight=FontWeights.SemiBold,Foreground=Accessibility.HighContrast?SystemColors.WindowTextBrush:UI.Brush("#E5E8ED")});
        target.Inlines.Add(new Run(" "+day.ToString("MMM",culture).ToUpperInvariant()){FontSize=10*Accessibility.TextScale,Foreground=Accessibility.HighContrast?SystemColors.WindowTextBrush:UI.Brush("#B7BDC7")});
        target.ToolTip=day.ToString("dddd, dd MMMM yyyy",culture);
    }
}
