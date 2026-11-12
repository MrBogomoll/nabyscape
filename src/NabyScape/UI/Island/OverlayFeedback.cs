using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace NabyScape;

public sealed partial class OverlayWindow
{
    private bool feedbackBrightness,diagnosticFeedbackOnly;
    internal void IsolateDiagnosticFeedback()
    {
        if(!DiagnosticRun)throw new InvalidOperationException("Feedback isolation requires diagnostics.");
        diagnosticFeedbackOnly=true;ClearFeedback();
    }
    private readonly StackPanel feedbackLeft=new(){Orientation=Orientation.Horizontal,VerticalAlignment=VerticalAlignment.Center,Visibility=Visibility.Collapsed};
    private readonly TextBlock feedbackInlineValue=new(){FontSize=12,Foreground=Brushes.White,Width=38,Margin=new(0,0,10,0),VerticalAlignment=VerticalAlignment.Center,Visibility=Visibility.Collapsed};
    // Music keeps its artwork and content beside volume; other priority content keeps its feedback.
    private bool InlineVolumeFeedback=>FeedbackVisible && !feedbackBrightness && ClosedMediaActive &&
        app.Notifications.Current==null && !ClosedVoiceActivity && ActiveCountdown()==null && !ClosedStopwatchActive;
    private bool PrimaryFeedbackVisible=>FeedbackVisible && !InlineVolumeFeedback;
    private readonly TextBlock feedbackIcon=new(){FontFamily=new FontFamily("Segoe MDL2 Assets"),FontSize=15,Foreground=Brushes.White,Width=27,VerticalAlignment=VerticalAlignment.Center,Visibility=Visibility.Collapsed};
    private readonly ProgressBar feedbackLevel=new(){Width=34,Height=3,Minimum=0,Maximum=1,Margin=new(7,0,0,0),VerticalAlignment=VerticalAlignment.Center,Visibility=Visibility.Collapsed,IsHitTestVisible=false};
    private readonly DispatcherTimer feedbackTick=new(){Interval=TimeSpan.FromMilliseconds(80)};
    private void InitializeFeedback(){app.Brightness.Changed+=BrightnessChanged;feedbackTick.Tick+=(_,_)=>{if(!FeedbackVisible)feedbackTick.Stop();Update();};}
    private void BrightnessChanged(int percent)
    {
        if(Dispatcher.HasShutdownStarted)return;
        Dispatcher.BeginInvoke(()=>{ShowFeedback(true,percent/100d,false);Update();});
    }
    private void ShowFeedback(bool brightness,double level,bool muted)
    {
        if(!app.Settings.ReactiveFeedback || !(brightness?app.Settings.BrightnessFeedback:app.Settings.VolumeFeedback) || expanded || peeking || !IsVisible || TuckRequested || userHidden)return;
        feedbackBrightness=brightness;
        feedbackText=muted?Localization.Text("Muted"):$"{Math.Clamp(level,0,1)*100:0}%";
        feedbackInlineValue.Text=muted?"0%":feedbackText;feedbackLeft.ToolTip=muted?Localization.Text("Muted"):Localization.Text("Volume")+" · "+feedbackInlineValue.Text;
        feedbackIcon.Text=brightness?"\uE706":muted?"\uE74F":"\uE767";
        feedbackIcon.ToolTip=brightness?Localization.Text("Brightness"):Localization.Text("Volume");
        feedbackUntil=DateTimeOffset.UtcNow.AddSeconds(2);
        double previous=feedbackLevel.Value;feedbackLevel.BeginAnimation(ProgressBar.ValueProperty,null);feedbackLevel.Value=muted?0:level;
        if(Motion.Enabled)feedbackLevel.BeginAnimation(ProgressBar.ValueProperty,new DoubleAnimation(previous,feedbackLevel.Value,TimeSpan.FromMilliseconds(170)){FillBehavior=FillBehavior.Stop});
        feedbackTick.Start();RefreshFeedback();
    }
    private void RefreshFeedback()
    {
        bool visible=FeedbackVisible;feedbackLeft.Visibility=visible?Visibility.Visible:Visibility.Collapsed;
        feedbackLevel.Visibility=visible || ClosedMediaActive?Visibility.Visible:Visibility.Collapsed;
        if(!visible){feedbackLevel.BeginAnimation(ProgressBar.ValueProperty,null);feedbackLevel.Value=app.Audio.Muted?0:Math.Clamp(app.Audio.Volume,0,1);}
        // At minimum width retain space for Naby and the standalone percentage too.
        double reserve=27+40+(AssistantCharacterAvailable?Math.Clamp(app.Settings.Height-4,24,32)+9:0)+compact.Padding.Left+compact.Padding.Right+7;
        feedbackLevel.Width=visible && !InlineVolumeFeedback?Math.Clamp(ClosedContentWidth-reserve,0,34):34;
        feedbackIcon.Visibility=visible && !InlineVolumeFeedback?Visibility.Visible:Visibility.Collapsed;
        feedbackInlineValue.Visibility=InlineVolumeFeedback?Visibility.Visible:Visibility.Collapsed;UpdateClosedMediaIcon();
        // Standalone feedback owns the summary; shared feedback keeps the lyric column flexible.
        summary.MinWidth=PrimaryFeedbackVisible?40:0;RefreshCompactOptionalVisibility();
        if(!visible){feedbackUntil=default;feedbackTick.Stop();}
    }
    private void RefreshCompactOptionalVisibility()
    {
        RefreshAssistantCharacter();bool feedback=FeedbackVisible;
        compactClock.Visibility=CompactDockPresentation.ShowClock(WidgetOn("clock"),IdleContent,feedback,ClosedMediaActive)?Visibility.Visible:Visibility.Collapsed;
        awakeMark.Visibility=!feedback && app.Awake?.Active==true?Visibility.Visible:Visibility.Collapsed;
        RefreshIdleDock();
    }
    private void ClearFeedback(){feedbackUntil=default;feedbackTick.Stop();RefreshFeedback();}
    private void StopFeedback(){feedbackTick.Stop();app.Brightness.Changed-=BrightnessChanged;}
    internal void DiagnosticFeedFeedback(bool brightness,int percent,bool muted=false)
    {
        if(!DiagnosticRun)throw new InvalidOperationException("Feedback injection is only available in diagnostics.");
        ShowFeedback(brightness,Math.Clamp(percent,0,100)/100d,muted);Update();
    }
}
