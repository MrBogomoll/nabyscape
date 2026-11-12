using System;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
namespace NabyScape;
// Shared by settings, both island layouts and detached windows. No private heartbeat.
internal sealed class CountdownView : StackPanel
{
    private readonly App app;private readonly bool focus;
    private readonly TextBlock digits=UI.Text("",26),status=UI.Caption("Ready",10,UI.Muted),hint=UI.Text("",10,UI.Muted),durationLabel=UI.Text("",10,UI.Muted);
    private readonly ProgressRing progress=new();
    private readonly DurationPicker duration,rest;
    private readonly Button primary,reset;
    private bool syncing,subscribed;private int knownDuration,knownRest;private Guid shownRun;private double shownDuration=-1;
    private Countdown State=>focus?app.Session.Focus:app.Session.Timer;
    internal int Refreshes {get;private set;}
    internal bool Subscribed=>subscribed;
    internal CountdownView(App app,bool focus,bool heading=true)
    {
        this.app=app;this.focus=focus;knownDuration=focus?app.Settings.FocusSeconds:app.Settings.TimerDurationSeconds;knownRest=app.Settings.BreakSeconds;
        string id=focus?"focus":"timer";
        duration=new DurationPicker(id,knownDuration);rest=new DurationPicker("break",knownRest,true);
        var header=new DockPanel{Margin=new(0,0,0,6)};status.HorizontalAlignment=HorizontalAlignment.Right;DockPanel.SetDock(status,Dock.Right);header.Children.Add(status);
        if(heading)header.Children.Add(UI.Heading(Localization.Text(focus?"Focus":"Timer"),13));Children.Add(header);
        progress.Height=progress.MinHeight=progress.MaxHeight=176;progress.Accent(focus);UI.LocalizedName(progress,focus?"Focus session progress":"Timer progress");
        var instrument=new Grid{Margin=new(0,0,0,10)};instrument.Children.Add(progress);
        var center=new StackPanel{VerticalAlignment=VerticalAlignment.Center,HorizontalAlignment=HorizontalAlignment.Center};digits.FontWeight=FontWeights.Medium;digits.HorizontalAlignment=HorizontalAlignment.Center;System.Windows.Documents.Typography.SetNumeralAlignment(digits,FontNumeralAlignment.Tabular);center.Children.Add(digits);center.Children.Add(duration);
        durationLabel.HorizontalAlignment=HorizontalAlignment.Center;durationLabel.Margin=new(0,3,0,0);center.Children.Add(durationLabel);instrument.Children.Add(center);Children.Add(instrument);
        primary=UI.CaptionButton("Start",Activate,true);AutomationProperties.SetAutomationId(primary,id+"-primary");
        reset=UI.CaptionButton("Reset",()=>{State.Reset();app.SaveSession();});UI.LocalizedName(reset,focus?"Reset focus":"Reset timer");AutomationProperties.SetAutomationId(reset,id+"-reset");
        var actions=new StackPanel{Orientation=Orientation.Horizontal,HorizontalAlignment=HorizontalAlignment.Center,Margin=new(0,0,0,10)};
        foreach(var b in new[]{primary,reset}){b.Width=b.Height=b.MinHeight=36;b.Margin=new(0);b.Padding=new(9);b.Tag=new CornerRadius(18);}
        primary.Margin=new(0,0,12,0);reset.Background=Brushes.Transparent;reset.Content=Icons.Create("reset",17,UI.Muted);actions.Children.Add(primary);actions.Children.Add(reset);Children.Add(actions);
        if(focus){var label=UI.Caption("Break duration",11,UI.Muted);label.HorizontalAlignment=HorizontalAlignment.Center;Children.Add(label);Children.Add(rest);}
        hint.MinHeight=16;hint.Margin=new(0,5,0,0);Children.Add(hint);
        foreach(var picker in new[]{duration,rest}){picker.Changed+=()=>{if(!syncing)Refresh();};picker.Committed+=CommitPreferences;}
        Loaded+=(_,_)=>Subscribe();Unloaded+=(_,_)=>Unsubscribe();IsVisibleChanged+=(_,_)=>{if(IsVisible)Subscribe();else Unsubscribe();};Refresh();
    }
    private void Subscribe(){if(!IsLoaded || !IsVisible || subscribed)return;app.Updated+=OnUpdate;subscribed=true;Refresh();}
    private void Unsubscribe(){if(!subscribed)return;app.Updated-=OnUpdate;subscribed=false;}
    private void OnUpdate(){if((app.CurrentUpdate&(RuntimeChange.Countdown|RuntimeChange.Settings))!=0)Refresh();}
    private bool Valid(out int seconds)=>duration.TrySeconds(out seconds) && seconds>0 && (!focus || rest.TrySeconds(out int pause) && pause>0);
    private void CommitPreferences()
    {
        if(syncing || State.Remaining(DateTimeOffset.UtcNow)>0 || !duration.TrySeconds(out int seconds) || !rest.TrySeconds(out int pause))return;
        bool changed=focus?(app.Settings.FocusSeconds!=seconds || app.Settings.BreakSeconds!=pause):app.Settings.TimerDurationSeconds!=seconds;
        if(!changed)return;
        if(focus){app.Settings.FocusSeconds=seconds;app.Settings.BreakSeconds=pause;}else app.Settings.TimerDurationSeconds=seconds;
        app.ChangeSettings();
    }
    private void Activate()
    {
        var now=DateTimeOffset.UtcNow;
        if(State.Running)State.Pause(now);else if(State.Remaining(now)>0)State.Resume(now);
        else{duration.Commit();rest.Commit();if(!Valid(out int seconds)){Refresh();return;}CommitPreferences();State.Start(seconds,now,focus?"work":"");}
        app.SaveSession();Refresh();
    }
    private void Refresh()
    {
        if(Window.GetWindow(this)?.WindowState==WindowState.Minimized)return;
        Refreshes++;var now=DateTimeOffset.UtcNow;var state=State;bool active=state.Remaining(now)>0,complete=!active && state.DurationSeconds>0;
        syncing=true;
        if(state.DurationSeconds>0 && (shownRun!=state.RunId || shownDuration!=state.DurationSeconds))
        {if(!focus || state.Phase!="break")duration.SetSeconds((int)Math.Round(state.DurationSeconds));shownRun=state.RunId;shownDuration=state.DurationSeconds;}
        int current=focus?app.Settings.FocusSeconds:app.Settings.TimerDurationSeconds;
        if(!active){if(knownDuration!=current && !duration.IsKeyboardFocusWithin && duration.Seconds==knownDuration)duration.SetSeconds(current);if(knownRest!=app.Settings.BreakSeconds && !rest.IsKeyboardFocusWithin && rest.Seconds==knownRest)rest.SetSeconds(app.Settings.BreakSeconds);}
        knownDuration=current;knownRest=app.Settings.BreakSeconds;syncing=false;
        bool valid=Valid(out _);duration.IsEnabled=rest.IsEnabled=!active;duration.Visibility=active || complete?Visibility.Collapsed:Visibility.Visible;digits.Visibility=active || complete?Visibility.Visible:Visibility.Collapsed;
        digits.Text=state.Display(now);durationLabel.Text=active || complete?Localization.Text("of ")+Countdown.FormatSeconds(state.DurationSeconds):"";
        Accessibility.Announce(status,Localization.Text(active?(state.Running?(focus?(state.Phase=="break"?"Break":"Focusing"):"Running"):"Paused"):complete?"Complete":"Ready"));
        string action=Localization.Text(state.Running?"Pause":active?"Resume":"Start");
        if(AutomationProperties.GetItemStatus(primary)!=action){primary.Content=Icons.Create(state.Running?"pause":"play",17,Brushes.Black);AutomationProperties.SetItemStatus(primary,action);}
        UI.Named(primary,Localization.Text(state.Running?(focus?"Pause focus":"Pause timer"):active?(focus?"Resume focus":"Resume timer"):(focus?"Start focus":"Start")));
        primary.IsEnabled=active || valid;reset.IsEnabled=active || complete;progress.Value=active || complete?state.Progress(now):0;progress.Accent(focus && state.Phase!="break");
        hint.Text=!active && !valid?Localization.Text("Set a duration of at least one second."):complete?Localization.Text("Time is up."):focus?Localization.Format("Break after focus: {0}",Countdown.FormatSeconds(rest.Seconds)):"";
    }
}
