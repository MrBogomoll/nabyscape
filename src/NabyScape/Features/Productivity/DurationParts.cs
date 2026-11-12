namespace NabyScape;
internal static class DurationParts
{
    internal const int MaximumSeconds=99*3600+59*60+59;
    internal static int Wrap(int value,int steps,int maximum)=>((value+steps)%(maximum+1)+(maximum+1))%(maximum+1);
}
