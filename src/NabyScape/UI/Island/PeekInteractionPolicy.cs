using System;
using System.Windows;
namespace NabyScape;

// Preserve every standard wheel notch, including coalesced and high-resolution input.
internal sealed class PeekWheelAccumulator
{
    private long total;
    private long last;
    internal int Push(int delta,long now)
    {
        if(delta==0)return 0;
        if(now-last>=300 || total!=0 && Math.Sign(total)!=Math.Sign(delta))total=0;
        last=now;total+=delta;
        int steps=(int)(total/120);total-=steps*120L;
        return steps;
    }
    internal void Reset(){total=0;last=0;}
}
internal static class WheelTabNavigation
{
    internal static int Target(int current,int steps,int count,bool wrap)
    {
        if(count<=0)return -1;
        long target=(long)Math.Clamp(current,0,count-1)+steps;
        return wrap?(int)((target%count+count)%count):(int)Math.Clamp(target,0,count-1);
    }
}
internal static class VisibilityPolicy
{
    // Own/menu and shell foregrounds must not replace the external window sampled before them.
    internal static IntPtr LatestExternalForeground(IntPtr externalForeground,IntPtr previous,bool previousEligible)=>externalForeground!=IntPtr.Zero?externalForeground:previousEligible?previous:IntPtr.Zero;
    // While a transient shell surface or NabyScape itself is foreground, the last external app still covers the display.
    internal static IntPtr RuleWindow(IntPtr externalForeground,IntPtr lastExternal,bool transientForeground)=>externalForeground!=IntPtr.Zero?externalForeground:transientForeground?lastExternal:IntPtr.Zero;
    internal static IntPtr RetainOverride(IntPtr current,IntPtr externalForeground)=>current!=IntPtr.Zero && externalForeground!=IntPtr.Zero && current!=externalForeground?IntPtr.Zero:current;
    internal static bool CarouselOwnsWheel(bool peeking,bool horizontal,bool focusedNote,bool overNote)=>peeking && (horizontal || !focusedNote || !overNote);
}
internal static class PeekLayout
{
    internal static Rect SafeBounds(double width,double height,double radius,bool notch)
    {
        var geometry=IslandGeometry.Create(width,height,radius,notch);
        double top=16,bottom=height-20,inset=32;
        while(inset<width/2-1 && (!geometry.FillContains(new Point(inset,top)) || !geometry.FillContains(new Point(inset,bottom))))inset++;
        return new Rect(inset,top,Math.Max(1,width-2*inset),Math.Max(1,bottom-top));
    }
}
internal static class PeekInteractionTests
{
    internal static void Run(Action<string,bool> test)
    {
        var wheel=new PeekWheelAccumulator();
        test("Small wheel deltas accumulate into one standard notch",wheel.Push(30,1000)==0 && wheel.Push(30,1010)==0 && wheel.Push(60,1020)==1);
        wheel.Reset();int steps=0;for(int i=0;i<5;i++)steps+=wheel.Push(120,1100+i*10);
        test("Five rapid wheel notches advance five tabs",steps==5);
        wheel.Reset();test("A coalesced wheel event retains all five notches",wheel.Push(600,1300)==5);
        test("A rapid reversal responds immediately",wheel.Push(-120,1310)==-1);
        wheel.Reset();test("Fractional wheel remainder is retained",wheel.Push(150,1500)==1 && wheel.Push(90,1510)==1);
        wheel.Reset();test("Direction changes discard stale fractional deltas",wheel.Push(60,2000)==0 && wheel.Push(-60,2010)==0 && wheel.Push(-60,2020)==-1);
        wheel.Reset();test("A pause clears an unfinished wheel notch",wheel.Push(60,3000)==0 && wheel.Push(60,3400)==0 && wheel.Push(60,3410)==1);
        test("Wheel tabs stop at both edges when wrapping is off",WheelTabNavigation.Target(4,5,6,false)==5 && WheelTabNavigation.Target(1,-5,6,false)==0);
        test("Wheel tabs wrap forwards and backwards when enabled",WheelTabNavigation.Target(5,1,6,true)==0 && WheelTabNavigation.Target(0,-1,6,true)==5);
        test("Wheel wrapping retains all five rapid steps",WheelTabNavigation.Target(4,5,6,true)==3 && WheelTabNavigation.Target(1,-5,6,true)==2);
        test("Wheel navigation handles empty and single tab lists",WheelTabNavigation.Target(0,5,0,true)==-1 && WheelTabNavigation.Target(0,-5,1,true)==0);
        var wrapSettings=new Settings{WheelWrapTabs=true};
        test("Wheel wrap choice persists and defaults to stopping",!new Settings().WheelWrapTabs && System.Text.Json.JsonSerializer.Deserialize<Settings>(System.Text.Json.JsonSerializer.Serialize(wrapSettings))!.WheelWrapTabs);
        var appA=new IntPtr(101);var appB=new IntPtr(202);
        test("Tray show uses the last eligible external foreground",VisibilityPolicy.LatestExternalForeground(IntPtr.Zero,appA,true)==appA);
        test("External foreground tracking replaces the previous app",VisibilityPolicy.LatestExternalForeground(appB,appA,true)==appB);
        test("Stale retained foreground falls back to no override",VisibilityPolicy.LatestExternalForeground(IntPtr.Zero,appA,false)==IntPtr.Zero);
        test("Missing retained foreground falls back without an exception",VisibilityPolicy.LatestExternalForeground(IntPtr.Zero,IntPtr.Zero,false)==IntPtr.Zero);
        test("New external foreground wins over a stale retained window",VisibilityPolicy.LatestExternalForeground(appB,appA,false)==appB);
        test("Own windows retain foreground override",VisibilityPolicy.RetainOverride(appA,IntPtr.Zero)==appA);
        test("Same app retains foreground override",VisibilityPolicy.RetainOverride(appA,appA)==appA);
        test("Taskbar or own window keeps the covering app as the tuck rule",VisibilityPolicy.RuleWindow(IntPtr.Zero,appA,true)==appA);
        test("Desktop or no foreground releases the tuck rule",VisibilityPolicy.RuleWindow(IntPtr.Zero,appA,false)==IntPtr.Zero);
        test("A real external foreground always decides the tuck rule",VisibilityPolicy.RuleWindow(appB,appA,true)==appB);
        test("Another external app clears foreground override",VisibilityPolicy.RetainOverride(appA,appB)==IntPtr.Zero);
        test("Focused note keeps vertical wheel only over editor",!VisibilityPolicy.CarouselOwnsWheel(true,false,true,true) && VisibilityPolicy.CarouselOwnsWheel(true,false,true,false) && VisibilityPolicy.CarouselOwnsWheel(true,true,true,true));
        test("Expanded and closed bypass preview wheel",!VisibilityPolicy.CarouselOwnsWheel(false,false,false,false));
        foreach(bool notch in new[]{false,true})foreach(double radius in new[]{0d,16d,40d})foreach(double height in new[]{100d,184d,280d})
        {
            var bounds=PeekLayout.SafeBounds(300,height,radius,notch);var contour=IslandGeometry.Create(300,height,radius,notch);
            test($"Safe hover rectangle {notch}/{radius}/{height}",bounds.Width>=216 && contour.FillContains(bounds.TopLeft) && contour.FillContains(bounds.BottomRight) && (height!=184 || bounds.Height>=148));
        }
    }
}
