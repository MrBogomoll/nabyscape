using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
namespace NabyScape;
internal static class SpotifyUiDiagnostics
{
    private sealed class Handler : HttpMessageHandler
    {
        internal bool Saved;internal int Mutations;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken cancel)
        {
            object result;
            if(request.RequestUri!.Host=="accounts.spotify.com")result=new{access_token="demo-access",refresh_token="demo-refresh",expires_in=3600,scope=SpotifyProtocol.Scopes};
            else if(request.RequestUri.AbsolutePath.EndsWith("currently-playing"))result=new{item=new{type="track",uri="spotify:track:0123456789ABCDEFGHIJKL",name="Тестовая песня",artists=new[]{new{name="Демонстрация"}},duration_ms=180000}};
            else if(request.RequestUri.AbsolutePath.EndsWith("/contains"))result=new[]{Saved};
            else{Saved=request.Method==HttpMethod.Put;Mutations++;return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));}
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK){Content=new StringContent(JsonSerializer.Serialize(result))});
        }
    }
    internal static async Task Run(App app,OverlayWindow island,string directory,Action<string,bool> check)
    {
        void Capture(FrameworkElement view,string name)
        {
            view.UpdateLayout();var bitmap=new RenderTargetBitmap((int)view.ActualWidth,(int)view.ActualHeight,96,96,PixelFormats.Pbgra32);bitmap.Render(view);var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(bitmap));using var file=File.Create(Path.Combine(directory,name+".png"));encoder.Save(file);
        }
        island.Hide();var original=app.Spotify;var handler=new Handler();var local=new SpotifyMediaSnapshot("Spotify.exe","Тестовая песня","Демонстрация",180);
        string root=Path.Combine(app.Store.DirectoryPath,"spotify-ui");Directory.CreateDirectory(root);
        using var demo=new SpotifyService(new Store(root),new AssistantMemoryKeys(),()=>local,new HttpClient(handler),(_,_,_)=>Task.FromResult("demo-code"));
        await demo.ConnectAsync(new string('a',32));typeof(App).GetProperty("Spotify",BindingFlags.Instance|BindingFlags.NonPublic)!.SetValue(app,demo);
        Window? host=null;
        try
        {
            var heart=new SpotifyLikeButton(app);var panel=new StackPanel();panel.Children.Add(UI.Caption("Music",15));
            var media=new MediaControls(panel,true,true,()=>new[]{new MediaSourceChoice(MediaSourceSelection.AutoId,"Spotify",()=>{})},()=>MediaSourceSelection.AutoId,()=>new StackPanel(),heart);media.Refresh(50,false,true);
            var surface=new Border{Child=media,Padding=new(16),CornerRadius=new(16)};surface.SetResourceReference(Border.BackgroundProperty,"SettingsCard");
            host=new Window{Content=surface,Width=320,Height=150,ShowActivated=false,Title="NabyScape · Spotify UI",WindowStyle=WindowStyle.None,AllowsTransparency=true,Background=Brushes.Transparent};host.Show();await Task.Delay(90);
            check("Spotify heart reads initial library state",heart.IsVisible && heart.IsEnabled && demo.Saved==false);
            check("Spotify heart uses its own chrome without native square states",heart.ReadLocalValue(Control.TemplateProperty)!=DependencyProperty.UnsetValue);
            heart.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));await Task.Delay(80);
            check("Spotify heart saves the exact track once",handler.Saved && handler.Mutations==1 && demo.Saved==true && heart.IsEnabled);
            foreach(string theme in new[]{"Light","Dark"})
            {
                host.Background=UI.Brush(theme=="Light"?"#EEEEEE":"#242424");host.Resources.MergedDictionaries.Clear();host.Resources.MergedDictionaries.Add(SettingsPalette.Create(new Settings{WindowTheme=theme}));await Task.Delay(60);host.UpdateLayout();
                var bitmap=new RenderTargetBitmap((int)host.ActualWidth,(int)host.ActualHeight,96,96,PixelFormats.Pbgra32);bitmap.Render(host);var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(bitmap));using var file=File.Create(Path.Combine(directory,"spotify-heart-"+theme+".png"));encoder.Save(file);
                check("Spotify footer fits "+theme+" at narrow width",heart.ActualWidth==32 && heart.TransformToAncestor(host).Transform(new Point(32,28)).X<host.ActualWidth);
                var heartPath=(System.Windows.Shapes.Path)heart.Content;check("Spotify heart remains readable in "+theme,SettingsThemeDiagnostics.Contrast(((SolidColorBrush)heartPath.Fill).Color,((SolidColorBrush)host.Background).Color)>4.5);
            }
            heart.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));await Task.Delay(80);check("Spotify filled heart removes the track once",!handler.Saved && handler.Mutations==2 && demo.Saved==false);
            island.PreviewActivity();island.SelectPeek("media");await Task.Delay(650);island.UpdateLayout();
            var hoverHeart=Diagnostics.Descendants(island).OfType<SpotifyLikeButton>().FirstOrDefault(b=>b.IsVisible && b.ActualWidth==28);
            check("Spotify heart is available in the middle music panel",hoverHeart!=null);
            if(hoverHeart!=null)
            {
                double height=island.PaintedHeight;int before=handler.Mutations;
                var click=new RoutedEventArgs(Button.ClickEvent);hoverHeart.RaiseEvent(click);await Task.Delay(650);
                check("Liking in middle panel keeps its size and consumes the click",click.Handled && island.IsPeeking && !island.IsExpanded && System.Math.Abs(island.PaintedHeight-height)<.5 && handler.Mutations==before+1);
                check("Middle heart fits inside the panel",hoverHeart.TransformToAncestor(island).Transform(new Point(28,26)).X<island.ActualWidth);Capture(island,"spotify-middle");
            }
            island.Collapse();island.Hide();
            local=local with{Source="Chrome.exe"};demo.MediaChanged();check("Spotify heart hides for a different media source",!heart.IsVisible);
            local=local with{Source="Spotify.exe"};demo.MediaChanged();await Task.Delay(80);demo.Disconnect();check("Spotify disconnect clears the heart but leaves connection action available",demo.Saved==null && heart.IsVisible && heart.IsEnabled);
        }
        finally{host?.Close();typeof(App).GetProperty("Spotify",BindingFlags.Instance|BindingFlags.NonPublic)!.SetValue(app,original);}
        var settings=new SettingsWindow(app){Width=1000,Height=850,ShowActivated=false};
        try
        {
            settings.Show();settings.OpenCategory("Audio");await Task.Delay(90);
            var elements=Diagnostics.Descendants(settings).ToArray();
            check("Spotify Client ID is masked by default",elements.OfType<PasswordBox>().Any(t=>UI.Identity(t)=="Spotify Client ID" && t.IsVisible) && !elements.OfType<TextBox>().Any(t=>UI.Identity(t)=="Spotify Client ID" && t.IsVisible));
            check("Spotify connection settings are available in music settings",elements.OfType<Button>().Any(b=>UI.Identity(b)=="Connect Spotify"));
            check("Settings have a visible Done button",elements.OfType<Button>().Any(b=>UI.Identity(b)=="Done with settings" && b.IsVisible));
            var password=elements.OfType<PasswordBox>().Single(t=>UI.Identity(t)=="Spotify Client ID");password.Password=new string('b',32);
            var eye=elements.OfType<Button>().Single(b=>UI.Identity(b)==Localization.Text("Show Spotify Client ID"));eye.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            var plain=elements.OfType<TextBox>().Single(t=>UI.Identity(t)=="Spotify Client ID");
            check("Client ID eye reveals the current value",plain.IsVisible && plain.Text==new string('b',32) && !password.IsVisible);
            plain.Text=new string('c',32);eye.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            check("Client ID eye masks edits and clears the plain field",password.IsVisible && password.Password==new string('c',32) && plain.Text.Length==0 && !plain.IsVisible);Capture(settings,"spotify-settings-masked");
        }
        finally{settings.Close();}
    }
}
