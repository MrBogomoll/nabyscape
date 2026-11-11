using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
namespace NabyScape;
internal sealed record SpotifyMediaSnapshot(string Source,string Title,string Artist,double Duration);
internal sealed record SpotifyTrack(string Uri,string Title,string[] Artists,double Duration);
internal static class SpotifyProtocol
{
    internal const string Redirect="http://127.0.0.1:43821/callback";
    internal const string Scopes="user-library-read user-library-modify user-read-currently-playing";
    internal static string RandomSecret()=>Base64Url(RandomNumberGenerator.GetBytes(32));
    private static string Base64Url(byte[] bytes)=>Convert.ToBase64String(bytes).TrimEnd('=').Replace('+','-').Replace('/','_');
    internal static string PkceChallenge(string verifier)=>Base64Url(SHA256.HashData(Encoding.ASCII.GetBytes(verifier)));
    internal static bool ValidClientId(string id)=>id!=null && Regex.IsMatch(id,"\\A[a-fA-F0-9]{32}\\z");
    internal static bool IsTrackUri(string uri)=>uri!=null && Regex.IsMatch(uri,"\\Aspotify:track:[A-Za-z0-9]{22}\\z");
    internal static bool IsSpotify(SpotifyMediaSnapshot? local)=>local!=null && local.Source.Contains("Spotify",StringComparison.OrdinalIgnoreCase);
    private static string Normalize(string text)=>Regex.Replace(text.Normalize(NormalizationForm.FormKC).ToLowerInvariant(),"[^\\p{L}\\p{N}]+"," ").Trim();
    internal static bool Matches(SpotifyTrack track,SpotifyMediaSnapshot local)
    {
        if(!IsSpotify(local) || !IsTrackUri(track.Uri) || string.IsNullOrWhiteSpace(local.Title) || string.IsNullOrWhiteSpace(local.Artist) || Normalize(track.Title)!=Normalize(local.Title))return false;
        bool artist=Normalize(string.Join(" ",track.Artists))==Normalize(local.Artist) || local.Artist.Split(new[]{',',';','•'},StringSplitOptions.RemoveEmptyEntries).Any(a=>track.Artists.Any(b=>Normalize(a)==Normalize(b)));
        return artist && double.IsFinite(local.Duration) && double.IsFinite(track.Duration) && local.Duration>0 && track.Duration>0 && Math.Abs(local.Duration-track.Duration)<=3;
    }
    internal static Dictionary<string,string>? CallbackFields(Uri uri,string state)
    {
        if(uri.Scheme!="http" || uri.Host!="127.0.0.1" || uri.Port!=43821 || uri.AbsolutePath!="/callback" || uri.Fragment.Length!=0 || uri.UserInfo.Length!=0)return null;
        var fields=new Dictionary<string,string>(StringComparer.Ordinal);
        try{foreach(string pair in uri.Query.TrimStart('?').Split('&',StringSplitOptions.RemoveEmptyEntries)){var parts=pair.Split('=',2);string key=Uri.UnescapeDataString(parts[0]),value=parts.Length==2?Uri.UnescapeDataString(parts[1].Replace('+',' ')):"";if(!fields.TryAdd(key,value))return null;}}catch(UriFormatException){return null;}
        if(!fields.TryGetValue("state",out var received) || !CryptographicOperations.FixedTimeEquals(SHA256.HashData(Encoding.UTF8.GetBytes(received)),SHA256.HashData(Encoding.UTF8.GetBytes(state))))return null;
        return fields;
    }
    internal static string? CallbackCode(Uri uri,string state)
    {
        var fields=CallbackFields(uri,state);
        return fields!=null && !fields.ContainsKey("error") && fields.TryGetValue("code",out var code) && code.Length is >0 and <=2048 && !code.Any(char.IsControl)?code:null;
    }
}
