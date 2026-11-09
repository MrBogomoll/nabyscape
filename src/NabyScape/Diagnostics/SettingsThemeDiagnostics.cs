using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace NabyScape;
internal static class SettingsThemeDiagnostics
{
    internal static async Task Run(App app,OverlayWindow island,string directory,Action<string,bool> check)
    {
        island.Hide();app.Settings.UiLanguage="ru";app.Settings.SettingsShowAll=true;Localization.Configure(app.Settings);
        var window=new SettingsWindow(app){Width=1100,Height=900,ShowActivated=false};
        try
        {
            window.Show();window.OpenCategory("Appearance");await Task.Delay(180);
            var handle=new System.Windows.Interop.WindowInteropHelper(window).Handle;
            IntPtr Hit(double x,double y){var point=window.PointToScreen(new Point(x,y));return SendMessage(handle,0x84,IntPtr.Zero,new IntPtr(((int)point.Y<<16)|((int)point.X&0xffff)));}
            check("The visible title bar is a native drag area",Hit(window.ActualWidth/2,29)==new IntPtr(2));
            check("The close button remains clickable",Hit(window.ActualWidth-24,29)==new IntPtr(1));
            foreach(string theme in new[]{"Light","Dark","Light"})
            {
                string caption=theme=="Light"?"Light theme":"Dark theme";
                var button=Tree(window).OfType<Button>().First(b=>UI.Identity(b)==caption);
                button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));await Task.Delay(100);window.UpdateLayout();
                check("Window theme switches live to "+theme,app.Settings.WindowTheme==theme);
                var text=(SolidColorBrush)window.FindResource("ControlText");var selected=(SolidColorBrush)window.FindResource("SettingsSelected");var ink=(SolidColorBrush)window.FindResource("SettingsSelectedText");
                check(theme+" selected navigation remains readable",Contrast(selected.Color,ink.Color)>=7);
                check(theme+" text palette has correct polarity",theme=="Light"?text.Color.R<40:text.Color.R>220);
                check(theme+" visible theme captions use the selected palette",Tree(window).OfType<TextBlock>().Where(t=>t.Text==Localization.Text("Window theme") || t.Text==Localization.Text("Transparency and blur") || t.Text==Localization.Text("Window appearance")).All(t=>t.Foreground is SolidColorBrush color && color.Color==text.Color));
                Save(window,Path.Combine(directory,"theme-"+theme+".png"));
            }
            foreach(string theme in new[]{"Light","Dark"})
            {
                app.Settings.WindowTheme=theme;window.ApplyWindowAppearance();
                foreach(string category in new[]{"Home","Visibility","Audio","Application","AI","Weather"})
                {
                    window.OpenCategory(category);await Task.Delay(60);Save(window,Path.Combine(directory,theme+"-"+category+".png"));
                }
                window.OpenCategory("Appearance");window.Width=600;window.Height=650;await Task.Delay(60);Save(window,Path.Combine(directory,theme+"-narrow.png"));
                check(theme+" narrow window retains theme choice",Tree(window).OfType<Button>().Any(b=>UI.Identity(b)=="Light theme" && b.ActualWidth>100));
                window.Width=480;await Task.Delay(60);Save(window,Path.Combine(directory,theme+"-480.png"));
                check(theme+" minimum width retains both choices",Tree(window).OfType<Button>().Where(b=>UI.Identity(b) is "Light theme" or "Dark theme").All(b=>b.ActualWidth>100));
                window.Width=1100;window.Height=900;
            }
            var restored=app.Store.Load<Settings>("settings");
            check("Theme change saves to the isolated profile",restored.WindowTheme=="Light");
            window.Width=1100;window.Height=780;window.OpenCategory("Appearance");await Task.Delay(60);
            var glass=Tree(window).OfType<Slider>().First(s=>UI.Identity(s)=="Transparency and blur");glass.Value=40;await Task.Delay(70);
            check("Glass slider requests the real system blur",app.Settings.SettingsMaterial=="Native Acrylic" && app.Settings.WindowGlass==40);
            if(window.MaterialResult.Native)check("Native glass retains a translucent tint",((LinearGradientBrush)window.FindResource("SettingsCanvas")).GradientStops.All(s=>s.Color.A<255));
            var effects=Tree(window).OfType<CheckBox>().First(c=>UI.Identity(c)=="Interface effects");effects.IsChecked=false;effects.RaiseEvent(new RoutedEventArgs(CheckBox.ClickEvent));await Task.Delay(70);
            check("Disabling effects disables native blur",!window.MaterialResult.Native && ((LinearGradientBrush)window.FindResource("SettingsCanvas")).GradientStops.All(s=>s.Color.A==255));
            string originalFill=app.Settings.ControlFill;app.Settings.ControlFill="#FFFFFF";app.ChangeSettings();
            check("Light island controls do not recolor ordinary island text",((SolidColorBrush)app.FindResource("ControlText")).Color.R<40 && ((SolidColorBrush)app.FindResource("BodyText")).Color.R>220);
            app.Settings.ControlFill=originalFill;app.ChangeSettings();
        }
        finally{window.Close();}
    }
    private static System.Collections.Generic.IEnumerable<DependencyObject> Tree(DependencyObject node)
    {yield return node;foreach(var child in LogicalTreeHelper.GetChildren(node).OfType<DependencyObject>())foreach(var descendant in Tree(child))yield return descendant;}
    internal static double Contrast(Color a,Color b)
    {
        double L(Color c){double F(byte v){double x=v/255d;return x<=.04045?x/12.92:Math.Pow((x+.055)/1.055,2.4);}return .2126*F(c.R)+.7152*F(c.G)+.0722*F(c.B);}
        double x=L(a),y=L(b);return (Math.Max(x,y)+.05)/(Math.Min(x,y)+.05);
    }
    private static void Save(FrameworkElement element,string path)
    {
        element.UpdateLayout();var bitmap=new RenderTargetBitmap((int)element.ActualWidth,(int)element.ActualHeight,96,96,PixelFormats.Pbgra32);bitmap.Render(element);
        var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(bitmap));using var stream=File.Create(path);encoder.Save(stream);
    }
    [DllImport("user32.dll")] private static extern IntPtr SendMessage(IntPtr hwnd,int message,IntPtr wparam,IntPtr lparam);
}
