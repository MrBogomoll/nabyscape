using System.Windows.Controls;
using System.Windows.Markup;
namespace NabyScape;
public sealed partial class SettingsWindow
{
    private static ControlTemplate? savedNoteCardTemplate;
    private static ControlTemplate SavedNoteCardTemplate()=>savedNoteCardTemplate??=(ControlTemplate)XamlReader.Parse("""
<ControlTemplate xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml" TargetType="{x:Type Expander}">
  <StackPanel>
    <ToggleButton x:Name="HeaderSite" Style="{x:Null}" IsChecked="{Binding IsExpanded, RelativeSource={RelativeSource TemplatedParent}, Mode=TwoWay}" Content="{TemplateBinding Header}" Foreground="{DynamicResource ControlText}" Background="Transparent" BorderThickness="0" HorizontalContentAlignment="Stretch" FocusVisualStyle="{DynamicResource KeyboardFocusOutline}">
      <ToggleButton.Template><ControlTemplate TargetType="{x:Type ToggleButton}">
        <Border x:Name="HeaderFill" Background="Transparent" CornerRadius="18" Padding="18,14" MinHeight="86">
          <Grid><Grid.ColumnDefinitions><ColumnDefinition/><ColumnDefinition Width="18"/></Grid.ColumnDefinitions>
            <ContentPresenter VerticalAlignment="Center" HorizontalAlignment="Stretch"/>
            <Path x:Name="Arrow" Grid.Column="1" Data="M1,1 L5,5 1,9" Width="5" Height="9" Stroke="{DynamicResource SettingsQuiet}" StrokeThickness="1.2" StrokeStartLineCap="Round" StrokeEndLineCap="Round" StrokeLineJoin="Round" VerticalAlignment="Center" HorizontalAlignment="Right" RenderTransformOrigin="0.5,0.5"/>
          </Grid>
        </Border>
        <ControlTemplate.Triggers>
          <Trigger Property="IsChecked" Value="True"><Setter TargetName="Arrow" Property="RenderTransform"><Setter.Value><RotateTransform Angle="90"/></Setter.Value></Setter></Trigger>
          <Trigger Property="IsMouseOver" Value="True"><Setter TargetName="HeaderFill" Property="Background" Value="{DynamicResource HoverOverlay}"/></Trigger>
        </ControlTemplate.Triggers>
      </ControlTemplate></ToggleButton.Template>
    </ToggleButton>
    <ContentPresenter x:Name="Body" ContentSource="Content" Margin="18,0,18,16" Visibility="Collapsed"/>
  </StackPanel>
  <ControlTemplate.Triggers><Trigger Property="IsExpanded" Value="True"><Setter TargetName="Body" Property="Visibility" Value="Visible"/></Trigger></ControlTemplate.Triggers>
</ControlTemplate>
""");
}
