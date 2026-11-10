using System.Linq;
using System.Windows;
using System.Windows.Controls;
namespace NabyScape;
public sealed partial class SettingsWindow
{
    private void TimersSettings()
    {
        page.Children.Add(Section("Timer",new CountdownView(app,false)));
        page.Children.Add(Section("Focus sessions",new CountdownView(app,true)));
        page.Children.Add(Section("Stopwatch",new StopwatchView(app)));
    }
    private void WidgetsSettings()
    {
        var tiles=new System.Windows.Controls.Primitives.UniformGrid{Columns=2,Margin=new(-5,0,-5,0)};
        foreach(var item in SettingsCategories.Where(c=>c.Group=="Features"))
        {
            var category=item;
            var body=new StackPanel();var title=UI.Caption(category.Title,15);title.FontWeight=FontWeights.SemiBold;body.Children.Add(title);
            var description=UI.Caption(category.Description,12,UI.Muted);description.Margin=new(0,6,0,0);body.Children.Add(description);
            var button=UI.CaptionButton(category.Title,()=>Navigate(category.Id));button.Content=body;button.Padding=new(20,18,20,18);button.Margin=new(5,0,5,10);button.HorizontalContentAlignment=HorizontalAlignment.Stretch;
            button.SetResourceReference(Control.BackgroundProperty,"SettingsCard");button.SetResourceReference(Control.BorderBrushProperty,"SettingsLineSoft");button.BorderThickness=new(1);button.Tag=new CornerRadius(18);tiles.Children.Add(button);
        }
        tiles.SizeChanged+=(_,_)=>tiles.Columns=tiles.ActualWidth<550?1:tiles.ActualWidth>1050?3:2;
        page.Children.Add(tiles);
    }
}
