using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Documents;
using System.Windows.Media;
namespace NabyScape;
internal sealed class DurationPicker : Grid
{
    private readonly TextBox[] inputs=new TextBox[3];
    private readonly int[] wheel=new int[3];
    internal event Action? Changed,Committed;
    internal DurationPicker(string prefix,int seconds,bool compact=false)
    {
        HorizontalAlignment=HorizontalAlignment.Center;
        string[] labels={"Hours","Minutes","Seconds"};
        for(int part=0;part<3;part++)
        {
            int index=part;int maximum=part==0?99:59;
            ColumnDefinitions.Add(new(){Width=new GridLength(40)});
            var column=new StackPanel();Grid.SetColumn(column,part*2);Children.Add(column);
            Button Arrow(bool up)
            {
                var b=UI.CaptionButton(up?"Increase duration":"Decrease duration",()=>Step(index,up?1:-1));
                UI.LocalizedName(b,prefix+" "+labels[index]+(up?" up":" down"));
                b.Content=new System.Windows.Shapes.Path{Data=Geometry.Parse(up?"M1,6 L6,1 11,6":"M1,1 L6,6 11,1"),StrokeThickness=1.4,Width=10,Height=6,Stretch=Stretch.Uniform,HorizontalAlignment=HorizontalAlignment.Center};
                ((System.Windows.Shapes.Path)b.Content).SetResourceReference(System.Windows.Shapes.Shape.StrokeProperty,"ControlText");
                b.Width=36;b.Height=b.MinHeight=compact?15:20;b.Padding=new(0);b.Margin=new(2,0,2,0);b.Background=Brushes.Transparent;ControlFeedback.SetEnabled(b,false);return b;
            }
            column.Children.Add(Arrow(true));
            var input=UI.CaptionInput(prefix+" "+labels[part],"00");inputs[part]=input;
            input.MaxLength=2;input.Width=40;input.FontSize=compact?18:22;input.FontWeight=FontWeights.Medium;input.Padding=new(0,2,0,2);input.Margin=new(0);input.MinHeight=compact?26:32;input.HorizontalContentAlignment=HorizontalAlignment.Center;
            Typography.SetNumeralAlignment(input,System.Windows.FontNumeralAlignment.Tabular);
            input.SetResourceReference(Control.ForegroundProperty,"BodyText");input.Background=Brushes.Transparent;input.BorderThickness=new(0);
            input.GotKeyboardFocus+=(_,_)=>input.SelectAll();
            input.PreviewTextInput+=(_,e)=>e.Handled=e.Text.Any(c=>c<'0' || c>'9');
            DataObject.AddPastingHandler(input,(_,e)=>{if(!e.DataObject.GetDataPresent(DataFormats.UnicodeText) || e.DataObject.GetData(DataFormats.UnicodeText) is not string text || text.Length>2 || text.Any(c=>c<'0' || c>'9'))e.CancelCommand();});
            input.TextChanged+=(_,_)=>Changed?.Invoke();
            input.LostKeyboardFocus+=(_,_)=>Commit();
            input.PreviewKeyDown+=(_,e)=>{if(e.Key is Key.Up or Key.Down){Step(index,e.Key==Key.Up?1:-1);e.Handled=true;}else if(e.Key==Key.Enter){Commit();input.SelectAll();e.Handled=true;}};
            column.Children.Add(input);column.Children.Add(Arrow(false));
            var label=UI.Caption(labels[part],9,UI.Muted);label.HorizontalAlignment=HorizontalAlignment.Center;column.Children.Add(label);
            column.PreviewMouseWheel+=(_,e)=>{e.Handled=true;wheel[index]+=e.Delta;int steps=wheel[index]/120;wheel[index]%=120;if(steps!=0)Step(index,steps);};
            if(part<2){ColumnDefinitions.Add(new(){Width=new GridLength(8)});var colon=UI.Text(":",compact?18:22);colon.VerticalAlignment=VerticalAlignment.Center;colon.Margin=new(0,0,0,10);Grid.SetColumn(colon,part*2+1);Children.Add(colon);}
        }
        SetSeconds(seconds);
    }
    internal bool TrySeconds(out int seconds)
    {
        seconds=0;int[] values=new int[3];
        for(int i=0;i<3;i++)if(!int.TryParse(inputs[i].Text,out values[i]) || values[i]<0 || values[i]>(i==0?99:59))return false;
        seconds=values[0]*3600+values[1]*60+values[2];return true;
    }
    internal int Seconds=>TrySeconds(out int seconds)?seconds:0;
    internal void SetSeconds(int seconds)
    {
        seconds=Math.Clamp(seconds,0,DurationParts.MaximumSeconds);
        int[] values={seconds/3600,seconds/60%60,seconds%60};
        for(int i=0;i<3;i++){string text=values[i].ToString("00",System.Globalization.CultureInfo.InvariantCulture);if(inputs[i].Text!=text)inputs[i].Text=text;}
    }
    internal void Commit()
    {
        for(int i=0;i<3;i++){int.TryParse(inputs[i].Text,out int value);string text=Math.Clamp(value,0,i==0?99:59).ToString("00");if(inputs[i].Text!=text)inputs[i].Text=text;}
        Committed?.Invoke();
    }
    private void Step(int part,int steps)
    {
        if(!IsEnabled)return;int.TryParse(inputs[part].Text,out int value);inputs[part].Text=DurationParts.Wrap(Math.Clamp(value,0,part==0?99:59),steps,part==0?99:59).ToString("00");Committed?.Invoke();
    }
}
