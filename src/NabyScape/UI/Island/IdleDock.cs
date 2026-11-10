using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace NabyScape;

public sealed partial class OverlayWindow
{
    private readonly bool idleDockReady;
    private readonly TextBlock idleDate=new(){FontSize=11,FontWeight=FontWeights.Medium,Foreground=UI.Muted,HorizontalAlignment=HorizontalAlignment.Center,VerticalAlignment=VerticalAlignment.Center,TextAlignment=TextAlignment.Center,TextWrapping=TextWrapping.NoWrap,ClipToBounds=true,Visibility=Visibility.Collapsed};
    private DateTime idleDateDay;
    private bool idleClosed;
    private double lastClosedWidth=double.NaN;
    private bool? idleLayoutState;
    private bool ClosedVoiceActivity=>app.Assistant.VoiceConnected || app.Assistant.Busy || app.Assistant.Desktop.Sharing;
    private bool ClosedStopwatchActive=>WidgetOn("stopwatch") && app.Session.Stopwatch.Running;
    private bool IdleContent=>!ClosedStopwatchActive &&!FeedbackVisible && app.Notifications.Current==null && !ClosedVoiceActivity &&
        !(ClosedMediaActive) &&
        !(WidgetOn("focus") && app.Session.Focus.Remaining(DateTimeOffset.UtcNow)>0) &&
        !(WidgetOn("timer") && app.Session.Timer.Remaining(DateTimeOffset.UtcNow)>0);
    private double ClosedSurfaceWidth=>ClosedLyricsWidth;
    private double ClosedContentWidth=>CompactDockPresentation.ContentWidth(ClosedSurfaceWidth,expandedWidth,surface.Notch);
    private string ClosedVoiceLabel=>app.Assistant.State switch
    {
        AssistantState.Listening=>Localization.Text("Listening"),AssistantState.Thinking=>Localization.Text("Thinking…"),AssistantState.Speaking=>Localization.Text("Speaking"),AssistantState.Connecting=>Localization.Text("Connecting…"),AssistantState.Error=>Localization.Text("Voice needs attention"),_=>app.Assistant.Desktop.Sharing?Localization.Text("Screen sharing"):Localization.Text("Naby voice")
    };
    private void RefreshIdleDock()
    {
        if(!idleDockReady)return;
        idleClosed=IdleContent && !TuckRequested && !dockEditing;
        compactClock.Visibility=CompactDockPresentation.ShowClock(WidgetOn("clock"),IdleContent,FeedbackVisible)?Visibility.Visible:Visibility.Collapsed;
        if(idleLayoutState!=idleClosed)
        {
            idleLayoutState=idleClosed;
            Grid.SetColumn(compactClock,4);compactClock.Margin=new(12,0,0,0);
            Accessibility.SetFontSize(compactClock,idleClosed?11:12);compactClock.FontWeight=idleClosed?FontWeights.Medium:FontWeights.Bold;
            if(idleClosed){activity.Visibility=closedTitleWave.Visibility=closedCover.Visibility=summary.Visibility=Visibility.Collapsed;}
            else summary.Visibility=closedTitleWave.Visibility==Visibility.Visible?Visibility.Collapsed:Visibility.Visible;
            UpdateAmbientMotion();
        }
        if(idleClosed)summary.Visibility=Visibility.Collapsed;
        // Equal side clearance keeps the date on the dock's center, independent of the clock or character.
        double dateRoom=CompactDockPresentation.DateRoom(ClosedContentWidth,compactClock.ActualWidth);
        idleDate.MaxWidth=Math.Max(0,dateRoom);
        idleDate.Visibility=idleClosed && app.Settings.ShowDate && WidgetOn("clock") && compactClock.Visibility==Visibility.Visible && dateRoom>=80?Visibility.Visible:Visibility.Collapsed;
        if(idleDate.Visibility==Visibility.Visible && idleDateDay!=DateTime.Today)
        {
            idleDateDay=DateTime.Today;CompactDockPresentation.FillDate(idleDate,idleDateDay);
        }
        compact.Width=ClosedContentWidth;assistantCharacterHost.Width=ClosedContentWidth;
        if(!expanded && !peeking && !userHidden && !TuckRequested && !dockEditing)
        {
            double target=ClosedSurfaceWidth;
            if(lastClosedWidth!=target || !motion.Active && Math.Abs(surface.SurfaceWidth-target)>.5)
            {lastClosedWidth=target;motion.Go(surface,IslandSurface.SurfaceWidthProperty,target,app.Settings.AnimationMs);WakeLiveRendering();}
        }
    }
}
