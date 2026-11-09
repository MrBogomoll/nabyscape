using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
namespace NabyScape;
public sealed partial class Settings
{
    public List<string> HideAppExceptions {get;set;}=new();
    internal void NormalizeHideExceptions()=>HideAppExceptions=(HideAppExceptions??new()).Select(VisibilityExceptionPolicy.NormalizeName).Where(n=>n.Length>0).Distinct(StringComparer.OrdinalIgnoreCase).Take(100).ToList();
}
internal static class VisibilityExceptionPolicy
{
    internal static string NormalizeName(string? name)
    {
        name=(name??"").Trim();if(name.Length==0)return "";
        if(!name.EndsWith(".exe",StringComparison.OrdinalIgnoreCase))name+=".exe";
        return name.Length>4 && name.Length<=255 && name[..^4].Trim().Length>0 && name.IndexOfAny(Path.GetInvalidFileNameChars())<0?name.ToLowerInvariant():"";
    }
    internal static bool IsExcluded(IEnumerable<string> exceptions,string? process)
    {string name=NormalizeName(process);return name.Length>0 && exceptions.Any(n=>string.Equals(n,name,StringComparison.OrdinalIgnoreCase));}
    internal static bool ShouldHide(bool requested,IEnumerable<string> exceptions,string? process)=>requested && !IsExcluded(exceptions,process);
}
