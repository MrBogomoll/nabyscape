using System;
using System.Windows;
using System.Windows.Controls;
namespace NabyScape;
public sealed partial class SettingsWindow
{
    private void SpotifySettings()
    {
        var spotify=app.Spotify;
        var client=new SpotifyClientIdInput(spotify.ClientId);
        var feedback=UI.Caption(spotify.Status,12,UI.Muted);Accessibility.Live(feedback);
        var connect=UI.CaptionButton("Connect Spotify",()=>{},true);connect.Click+=async (_,_)=>{await spotify.ConnectAsync(client.Value);};
        var cancel=UI.CaptionButton("Cancel Spotify sign-in",spotify.CancelLogin);
        var disconnect=UI.CaptionButton("Disconnect Spotify",()=>spotify.Disconnect());
        var actions=new WrapPanel();actions.Children.Add(connect);actions.Children.Add(cancel);actions.Children.Add(disconnect);
        var guide=new StackPanel();guide.Children.Add(UI.Caption("One-time Spotify setup",14));
        guide.Children.Add(UI.Caption("1. Open Spotify Developer Dashboard and sign in with your Premium account. Choose Create app. Use NabyScape for the name and select Web API.",12,UI.Muted));
        guide.Children.Add(UI.Caption("2. Add the redirect address below exactly as shown. Accept Spotify's terms yourself, then save the app.",12,UI.Muted));
        var redirect=UI.Input(Localization.Text("Spotify redirect address"),SpotifyProtocol.Redirect);redirect.IsReadOnly=true;redirect.SetResourceReference(Control.ForegroundProperty,"ControlText");guide.Children.Add(redirect);
        guide.Children.Add(UI.CaptionButton("Copy Spotify redirect address",()=>Clipboard.SetText(SpotifyProtocol.Redirect)));
        guide.Children.Add(UI.Caption("3. Copy Client ID from the app settings into the field above. Client Secret is not needed. Press Connect Spotify and approve library access in your browser.",12,UI.Muted));
        guide.Children.Add(UI.Caption("If Spotify denies access, add your Spotify account to the app's User Management list. Development Mode is limited to the accounts approved in that list.",11,UI.Muted));
        guide.Children.Add(UI.CaptionButton("Open Spotify Developer Dashboard",()=>ShortcutService.Open(new(){Target="https://developer.spotify.com/dashboard"})));
        var help=new Expander{Header=Localization.Text("How to connect Spotify"),Content=guide,IsExpanded=spotify.ClientId.Length==0,Margin=new(0,10,0,0)};
        var section=Section("Spotify",UI.Caption("Connect once to add and remove the current song in Spotify Liked Songs using the heart on the music panel.",12,UI.Muted),UI.Caption("Spotify Client ID",12),client,actions,feedback,help);
        bool subscribed=false;
        void Refresh(){feedback.Text=Localization.Text(spotify.Status);client.CanEdit=!spotify.Connected && !spotify.Busy && !spotify.NeedsDisconnectCleanup;connect.IsEnabled=!spotify.Connected && !spotify.Busy && !spotify.NeedsDisconnectCleanup;disconnect.IsEnabled=spotify.Connected || spotify.NeedsDisconnectCleanup;cancel.Visibility=spotify.Connecting?Visibility.Visible:Visibility.Collapsed;}
        section.Loaded+=(_,_)=>{if(!subscribed){spotify.Changed+=Refresh;subscribed=true;}Refresh();};section.Unloaded+=(_,_)=>{if(subscribed){spotify.Changed-=Refresh;subscribed=false;}};
        Refresh();page.Children.Add(section);
    }
}
