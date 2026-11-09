using System;
namespace NabyScape;

public sealed partial class Settings
{
    internal static readonly string[] WindowPreferenceKeys={"WindowTheme","WindowGlass","WindowLargeCorners","WindowEffects","WindowCompact","WindowHoverAccents"};
    public string WindowTheme {get;set;}="Light";
    public double WindowGlass {get;set;}=70;
    public bool WindowLargeCorners {get;set;}=true;
    public bool WindowEffects {get;set;}=true;
    public bool WindowCompact {get;set;}
    public bool WindowHoverAccents {get;set;}=true;
    internal void NormalizeWindowTheme()
    {
        if(WindowTheme is not ("Light" or "Dark"))WindowTheme="Light";
        WindowGlass=double.IsFinite(WindowGlass)?Math.Clamp(WindowGlass,0,100):70;
    }
}
