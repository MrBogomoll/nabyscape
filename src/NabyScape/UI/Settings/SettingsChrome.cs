using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shell;
namespace NabyScape;
public sealed partial class SettingsWindow
{
    private FrameworkElement SettingsFrame(UIElement content)
    {
        WindowStyle=WindowStyle.None;AllowsTransparency=true;ResizeMode=ResizeMode.CanResize;UseLayoutRounding=true;
        WindowChrome.SetWindowChrome(this,new WindowChrome{CaptionHeight=58,ResizeBorderThickness=new(6),CornerRadius=new(28),GlassFrameThickness=new(0),UseAeroCaptionButtons=false});
        var frame=new Grid();windowShell=frame;frame.RowDefinitions.Add(new(){Height=new GridLength(58)});frame.RowDefinitions.Add(new());
        // A separate caption makes the draggable part of the window visible.
        var bar=new Grid{VerticalAlignment=VerticalAlignment.Top,Height=58};bar.SetResourceReference(Panel.BackgroundProperty,"SettingsCaption");bar.ColumnDefinitions.Add(new());bar.ColumnDefinitions.Add(new(){Width=GridLength.Auto});
        var caption=UI.Row(BrandMark.Create(32),UI.Caption("NabyScape Settings",14));caption.Margin=new(26,0,0,0);caption.VerticalAlignment=VerticalAlignment.Center;((FrameworkElement)caption.Children[1]).Margin=new(12,0,0,0);caption.IsHitTestVisible=false;bar.Children.Add(caption);
        var grip=new Grid{Width=24,Height=14,HorizontalAlignment=HorizontalAlignment.Center,VerticalAlignment=VerticalAlignment.Center,IsHitTestVisible=false};
        for(int i=0;i<6;i++){var dot=new System.Windows.Shapes.Ellipse{Width=3,Height=3,HorizontalAlignment=HorizontalAlignment.Left,VerticalAlignment=VerticalAlignment.Top,Margin=new((i%3)*9,(i/3)*9,0,0)};dot.SetResourceReference(System.Windows.Shapes.Shape.FillProperty,"SettingsQuiet");grip.Children.Add(dot);}Grid.SetColumnSpan(grip,2);bar.Children.Add(grip);
        var actions=new StackPanel{Orientation=Orientation.Horizontal};Grid.SetColumn(actions,1);bar.Children.Add(actions);
        Button Caption(string name,string data,Action action)
        {
            var button=UI.CaptionButton(name,action);button.Width=48;button.Height=58;button.MinHeight=58;button.Padding=new(0);button.Margin=new(0);button.Tag=new CornerRadius(0);button.Background=Brushes.Transparent;button.ToolTip=Localization.Text(name);
            button.Content=new System.Windows.Shapes.Path{Data=Geometry.Parse(data),Stroke=SettingsTheme.Quiet,StrokeThickness=1.15,Width=11,Height=11,Stretch=Stretch.Uniform,HorizontalAlignment=HorizontalAlignment.Center,VerticalAlignment=VerticalAlignment.Center};
            WindowChrome.SetIsHitTestVisibleInChrome(button,true);actions.Children.Add(button);return button;
        }
        Caption("Minimize Settings","M0,5 H10",()=>SystemCommands.MinimizeWindow(this));
        var maximize=Caption("Maximize Settings","M0,0 H10 V10 H0 Z",()=>{if(WindowState==WindowState.Maximized)SystemCommands.RestoreWindow(this);else SystemCommands.MaximizeWindow(this);});
        StateChanged+=(_,_)=>{frame.Margin=WindowState==WindowState.Maximized?new Thickness(7):new Thickness(0);UI.LocalizedName(maximize,WindowState==WindowState.Maximized?"Restore Settings":"Maximize Settings");};
        Caption("Close Settings","M0,0 L10,10 M10,0 L0,10",()=>SystemCommands.CloseWindow(this));
        Grid.SetRow(content,1);frame.Children.Add(content);frame.Children.Add(bar);
        var outer=new Border{BorderThickness=new(1),CornerRadius=new(app.Settings.WindowLargeCorners?28:12),Child=frame};windowFrame=outer;
        outer.SetResourceReference(Border.BackgroundProperty,"SettingsCanvas");outer.SetResourceReference(Border.BorderBrushProperty,"SettingsLine");
        void Clip(){double radius=WindowState==WindowState.Maximized?0:outer.CornerRadius.TopLeft;frame.Clip=new RectangleGeometry(new Rect(0,0,Math.Max(0,frame.ActualWidth),Math.Max(0,frame.ActualHeight)),radius,radius);}
        refreshWindowOutline=()=>{Clip();SettingsWindowOutline.Update(this,outer.CornerRadius.TopLeft);};
        frame.SizeChanged+=(_,_)=>refreshWindowOutline();SourceInitialized+=(_,_)=>refreshWindowOutline();StateChanged+=(_,_)=>refreshWindowOutline();DpiChanged+=(_,_)=>refreshWindowOutline();
        return outer;
    }
}
