using System.Windows.Controls;
using System.Windows.Markup;
namespace NabyScape;
public sealed partial class SettingsWindow
{
    private static ControlTemplate? noteSelectionTemplate;
    private static ControlTemplate NoteSelectionTemplate()=>noteSelectionTemplate??=(ControlTemplate)XamlReader.Parse("""
<ControlTemplate xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml" TargetType="{x:Type CheckBox}">
  <Grid Background="Transparent">
    <Border x:Name="Ring" Width="19" Height="19" CornerRadius="10" Background="Transparent" BorderBrush="{DynamicResource ControlBorder}" BorderThickness="1.2" HorizontalAlignment="Left" VerticalAlignment="Center">
      <Path x:Name="Tick" Data="M4,9 L7,12 13,5" Stroke="{DynamicResource AccentText}" StrokeThickness="1.8" StrokeStartLineCap="Round" StrokeEndLineCap="Round" Visibility="Hidden"/>
    </Border>
  </Grid>
  <ControlTemplate.Triggers>
    <Trigger Property="IsChecked" Value="True"><Setter TargetName="Ring" Property="Background" Value="{DynamicResource Accent}"/><Setter TargetName="Ring" Property="BorderBrush" Value="{DynamicResource Accent}"/><Setter TargetName="Tick" Property="Visibility" Value="Visible"/></Trigger>
    <Trigger Property="IsKeyboardFocused" Value="True"><Setter TargetName="Ring" Property="BorderBrush" Value="{DynamicResource ControlText}"/><Setter TargetName="Ring" Property="BorderThickness" Value="2"/></Trigger>
  </ControlTemplate.Triggers>
</ControlTemplate>
""");
    private static ControlTemplate? savedNoteCardTemplate;
    private static ControlTemplate SavedNoteCardTemplate()=>savedNoteCardTemplate??=(ControlTemplate)XamlReader.Parse("""
<ControlTemplate xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml" TargetType="{x:Type Expander}">
  <StackPanel>
   <Grid>
    <Grid.ColumnDefinitions><ColumnDefinition Width="50"/><ColumnDefinition/><ColumnDefinition Width="48"/></Grid.ColumnDefinitions>
    <ContentPresenter Content="{Binding Tag[0], RelativeSource={RelativeSource TemplatedParent}}" Margin="16,0,0,0" HorizontalAlignment="Left" VerticalAlignment="Center"/>
    <ToggleButton x:Name="HeaderSite" Grid.Column="1" Style="{x:Null}" IsChecked="{Binding IsExpanded, RelativeSource={RelativeSource TemplatedParent}, Mode=TwoWay}" Content="{TemplateBinding Header}" Foreground="{DynamicResource ControlText}" Background="Transparent" BorderThickness="0" HorizontalContentAlignment="Stretch" FocusVisualStyle="{DynamicResource KeyboardFocusOutline}">
      <ToggleButton.Template><ControlTemplate TargetType="{x:Type ToggleButton}">
        <Border x:Name="HeaderFill" Background="Transparent" CornerRadius="12" Padding="0,14,4,14" MinHeight="86">
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
    <ContentPresenter Grid.Column="2" Content="{Binding Tag[1], RelativeSource={RelativeSource TemplatedParent}}" Margin="0,0,12,0" HorizontalAlignment="Right" VerticalAlignment="Center"/>
   </Grid>
    <ContentPresenter x:Name="Body" ContentSource="Content" Margin="18,0,18,16" Visibility="Collapsed"/>
  </StackPanel>
  <ControlTemplate.Triggers><Trigger Property="IsExpanded" Value="True"><Setter TargetName="Body" Property="Visibility" Value="Visible"/></Trigger></ControlTemplate.Triggers>
</ControlTemplate>
""");
}
