using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
namespace NabyScape;
internal static class SettingsWindowOutline
{
    internal static void Update(Window window,double radius)
    {
        if(window.AllowsTransparency)return;
        var handle=new WindowInteropHelper(window).Handle;if(handle==IntPtr.Zero)return;
        if(window.WindowState==WindowState.Maximized){SetWindowRgn(handle,IntPtr.Zero,true);return;}
        var dpi=VisualTreeHelper.GetDpi(window);int width=(int)Math.Ceiling(window.ActualWidth*dpi.DpiScaleX),height=(int)Math.Ceiling(window.ActualHeight*dpi.DpiScaleY);
        if(width<1 || height<1)return;
        var region=CreateRoundRectRgn(0,0,width+1,height+1,(int)(radius*2*dpi.DpiScaleX),(int)(radius*2*dpi.DpiScaleY));
        if(region!=IntPtr.Zero && SetWindowRgn(handle,region,true)==0)DeleteObject(region);
    }
    [DllImport("gdi32.dll")] private static extern IntPtr CreateRoundRectRgn(int left,int top,int right,int bottom,int width,int height);
    [DllImport("user32.dll")] private static extern int SetWindowRgn(IntPtr window,IntPtr region,bool redraw);
    [DllImport("gdi32.dll")] private static extern bool DeleteObject(IntPtr value);
}
