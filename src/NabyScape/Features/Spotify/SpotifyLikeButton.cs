using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
namespace NabyScape;
internal sealed class SpotifyLikeButton : Button
{
    private readonly App app;
    private readonly Path heart=new(){Data=Geometry.Parse("M12,21 C9,18 2,13 2,8 C2,2 9,1 12,6 C15,1 22,2 22,8 C22,13 15,18 12,21 Z"),Width=17,Height=17,Stretch=Stretch.Uniform,StrokeThickness=1.5,StrokeLineJoin=PenLineJoin.Round,IsHitTestVisible=false};
    private bool subscribed;
    internal SpotifyLikeButton(App app)
    {
        this.app=app;Content=heart;Width=MinWidth=32;Height=MinHeight=28;Padding=new(6);Margin=new(6,0,6,0);Background=Brushes.Transparent;BorderThickness=new(0);UI.LocalizedName(this,"Spotify favorite");ControlFeedback.SetEnabled(this,false);
        // Derived buttons fall back to native hover/disabled chrome unless explicitly templated.
        var area=new FrameworkElementFactory(typeof(Border));area.SetValue(Border.BackgroundProperty,Brushes.Transparent);
        var content=new FrameworkElementFactory(typeof(ContentPresenter));content.SetValue(HorizontalAlignmentProperty,HorizontalAlignment.Center);content.SetValue(VerticalAlignmentProperty,VerticalAlignment.Center);area.AppendChild(content);
        Template=new ControlTemplate(typeof(Button)){VisualTree=area};FocusVisualStyle=null;Cursor=System.Windows.Input.Cursors.Hand;
        IsEnabledChanged+=(_,_)=>heart.Opacity=IsEnabled?1:.5;
        GotKeyboardFocus+=(_,_)=>heart.StrokeThickness=2;LostKeyboardFocus+=(_,_)=>heart.StrokeThickness=1.5;
        Loaded+=(_,_)=>{if(!subscribed){app.Spotify.Changed+=Update;subscribed=true;}Update();};
        Unloaded+=(_,_)=>{if(subscribed){app.Spotify.Changed-=Update;subscribed=false;}app.Spotify.SetDemand(this,false);};
        IsVisibleChanged+=(_,_)=>app.Spotify.SetDemand(this,IsVisible && app.Spotify.Connected);
        Click+=async (_,e)=>{e.Handled=true;if(!app.Spotify.Connected){app.ShowSettings("Audio");return;}await app.Spotify.ToggleAsync();};
        Update();
    }
    private void Update()
    {
        var spotify=app.Spotify;Visibility=spotify.SelectedSpotify?Visibility.Visible:Visibility.Collapsed;
        IsEnabled=!spotify.Busy && (!spotify.Connected || spotify.CanLike);
        heart.SetResourceReference(Shape.StrokeProperty,"BodyText");if(spotify.Saved==true)heart.SetResourceReference(Shape.FillProperty,"BodyText");else heart.Fill=Brushes.Transparent;
        ToolTip=Localization.Text(!spotify.Connected?"Connect Spotify to save songs.":spotify.CanLike?(spotify.Saved==true?"Remove from Spotify Liked Songs":"Add to Spotify Liked Songs"):spotify.Status);
        UI.Named(this,(string)ToolTip);System.Windows.Automation.AutomationProperties.SetItemStatus(this,Localization.Text(spotify.Saved==true?"Saved":"Not saved"));
        app.Spotify.SetDemand(this,IsLoaded && IsVisible && spotify.Connected);
    }
}
