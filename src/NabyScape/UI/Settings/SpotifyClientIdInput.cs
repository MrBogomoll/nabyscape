using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
namespace NabyScape;

internal sealed class SpotifyClientIdInput : Grid
{
    private readonly PasswordBox masked=new(){MaxLength=32,MinHeight=34,Padding=new(10,8,10,8)};
    private readonly TextBox plain=UI.CaptionInput("Spotify Client ID","");
    private readonly Button reveal;
    private bool shown;
    internal string Value=>shown?plain.Text:masked.Password;
    internal bool CanEdit {set{masked.IsEnabled=plain.IsEnabled=value;}}
    internal SpotifyClientIdInput(string value)
    {
        ColumnDefinitions.Add(new());ColumnDefinitions.Add(new(){Width=GridLength.Auto});
        UI.LocalizedName(masked,"Spotify Client ID");masked.Password=value;
        foreach(var control in new Control[]{masked,plain})
        {control.SetResourceReference(Control.BackgroundProperty,"ControlFill");control.SetResourceReference(Control.ForegroundProperty,"ControlText");control.SetResourceReference(Control.BorderBrushProperty,"ControlBorder");}
        masked.SetResourceReference(PasswordBox.CaretBrushProperty,"ControlText");
        var frame=new FrameworkElementFactory(typeof(Border));frame.SetValue(Border.CornerRadiusProperty,new CornerRadius(8));frame.SetValue(Border.BorderThicknessProperty,new Thickness(1));
        frame.SetBinding(Border.BackgroundProperty,new System.Windows.Data.Binding("Background"){RelativeSource=System.Windows.Data.RelativeSource.TemplatedParent});
        frame.SetBinding(Border.BorderBrushProperty,new System.Windows.Data.Binding("BorderBrush"){RelativeSource=System.Windows.Data.RelativeSource.TemplatedParent});
        frame.SetBinding(Border.PaddingProperty,new System.Windows.Data.Binding("Padding"){RelativeSource=System.Windows.Data.RelativeSource.TemplatedParent});
        var host=new FrameworkElementFactory(typeof(ScrollViewer),"PART_ContentHost");frame.AppendChild(host);masked.Template=new ControlTemplate(typeof(PasswordBox)){VisualTree=frame};
        plain.MaxLength=32;plain.Margin=masked.Margin=new(0,3,6,3);plain.Visibility=Visibility.Collapsed;Children.Add(masked);Children.Add(plain);
        reveal=Icons.Button("eye",Localization.Text("Show Spotify Client ID"),()=>SetShown(!shown),false,34);reveal.Margin=new(0,3,0,3);Grid.SetColumn(reveal,1);Children.Add(reveal);
        Unloaded+=(_,_)=>SetShown(false);
        Loaded+=(_,_)=>{if(Window.GetWindow(this) is {} window){window.Deactivated+=Hide;Unloaded+=(_,_)=>window.Deactivated-=Hide;}};
    }
    private void Hide(object? sender,System.EventArgs args)=>SetShown(false);
    private void SetShown(bool visible)
    {
        if(shown==visible)return;
        if(visible)plain.Text=masked.Password;else{masked.Password=plain.Text;plain.Clear();}
        shown=visible;plain.Visibility=shown?Visibility.Visible:Visibility.Collapsed;masked.Visibility=shown?Visibility.Collapsed:Visibility.Visible;
        string label=Localization.Text(shown?"Hide Spotify Client ID":"Show Spotify Client ID");UI.Named(reveal,label);reveal.ToolTip=label;
        System.Windows.Automation.AutomationProperties.SetItemStatus(reveal,Localization.Text(shown?"Visible":"Hidden"));
    }
}
