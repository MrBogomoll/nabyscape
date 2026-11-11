using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
namespace NabyScape;
internal sealed class SpotifyConnectionData {public string ClientId {get;set;}="";public bool Enabled {get;set;}}
internal sealed record SpotifyStoredToken(string ClientId,string RefreshToken);
internal sealed class SpotifyService : IDisposable
{
    // Reserved app-owned generic credential. Existing uninstall cleanup removes it too.
    internal const string CredentialId="ac96988c4f794f91b3798d1a44349f20";
    private readonly Store store;
    private readonly IAssistantKeys keys;
    private readonly HttpClient http;
    private readonly Func<SpotifyMediaSnapshot?> snapshot;
    private readonly Func<Uri,string,CancellationToken,Task<string>> authorize;
    private readonly bool networkEnabled;
    private readonly HashSet<object> consumers=new();
    private readonly SemaphoreSlim gate=new(1,1);
    private readonly DispatcherTimer poll=new(){Interval=TimeSpan.FromSeconds(6)};
    private CancellationTokenSource lifetime=new(),login=new();
    private string access="",refresh="";
    private DateTimeOffset expires,nextRequest;
    private SpotifyMediaSnapshot? observed;
    private SpotifyTrack? track;
    private int generation;
    private bool disposed,suspended,refreshQueued;
    private CancellationTokenSource? activeRead;
    private int syncRetries;
    private DateTimeOffset lastChecked;
    internal event Action? Changed;
    internal string ClientId {get;private set;}="";
    internal bool Connected=>!disposed && refresh.Length>0;
    internal bool Busy {get;private set;}
    internal bool Connecting {get;private set;}
    internal bool NeedsDisconnectCleanup {get;private set;}
    internal string Status {get;private set;}="Spotify is not connected.";
    internal bool? Saved {get;private set;}
    internal bool SelectedSpotify=>SpotifyProtocol.IsSpotify(snapshot());
    internal bool CanLike=>Connected && !suspended && !Busy && Saved.HasValue && track!=null && snapshot() is {} local && SpotifyProtocol.Matches(track,local) && DateTimeOffset.UtcNow>=nextRequest;
    internal SpotifyService(Store store,IAssistantKeys keys,Func<SpotifyMediaSnapshot?> snapshot,HttpClient? http=null,Func<Uri,string,CancellationToken,Task<string>>? authorize=null,bool networkEnabled=true)
    {
        this.store=store;this.keys=keys;this.snapshot=snapshot;this.http=http??new HttpClient(new HttpClientHandler{AllowAutoRedirect=false}){Timeout=TimeSpan.FromSeconds(15)};this.authorize=authorize??SpotifyOAuth.Authorize;this.networkEnabled=networkEnabled;
        var config=store.Load<SpotifyConnectionData>("spotify-connection");ClientId=config.ClientId??"";
        try
        {
            if(config.Enabled && SpotifyProtocol.ValidClientId(ClientId) && keys.Read(CredentialId) is {} json && JsonSerializer.Deserialize<SpotifyStoredToken>(json) is {} token && token.ClientId==ClientId && ValidToken(token.RefreshToken)){refresh=token.RefreshToken;Status="Connected to Spotify.";}
            else if(!config.Enabled && SpotifyProtocol.ValidClientId(ClientId))NeedsDisconnectCleanup=keys.Exists(CredentialId);
        }
        catch{Status="Could not read the saved Spotify connection. Connect again.";}
        poll.Tick+=(_,_)=>{if(consumers.Count>0 && Connected && SelectedSpotify)_=RefreshAsync();};
    }
    internal void SetDemand(object consumer,bool visible)
    {
        if(disposed)return;bool added=visible?consumers.Add(consumer):false;if(!visible)consumers.Remove(consumer);
        if(consumers.Count>0 && Connected && !suspended && networkEnabled){poll.Start();if(added && (!Saved.HasValue || DateTimeOffset.UtcNow-lastChecked>TimeSpan.FromSeconds(5)))_=RefreshAsync();}else poll.Stop();
    }
    internal void MediaChanged()
    {
        if(disposed)return;var current=snapshot();if(current==observed)return;observed=current;track=null;Saved=null;syncRetries=0;
        // Resolve the new song while the panel is closed too; cancel obsolete reads,
        // never mutations, and do not lose a refresh behind the single-request gate.
        bool resolve=Connected && SelectedSpotify && networkEnabled;
        if(resolve)refreshQueued=true;activeRead?.Cancel();Changed?.Invoke();
        if(resolve && !Busy){refreshQueued=false;_=RefreshAsync();}
    }
    internal void CancelLogin()=>login.Cancel();
    internal async Task<bool> ConnectAsync(string id)
    {
        if(disposed || Busy || Connected || NeedsDisconnectCleanup || !networkEnabled)return false;
        id=id.Trim();if(!SpotifyProtocol.ValidClientId(id)){Status="Enter the Client ID from Spotify Developer Dashboard.";Changed?.Invoke();return false;}
        if(!gate.Wait(0))return false;Busy=Connecting=true;int epoch=generation;bool tokenSaved=false;login.Dispose();login=CancellationTokenSource.CreateLinkedTokenSource(lifetime.Token);login.CancelAfter(TimeSpan.FromMinutes(3));
        try
        {
            Status="Finish signing in to Spotify in your browser.";Changed?.Invoke();
            store.Save("spotify-connection",new SpotifyConnectionData{ClientId=id});ClientId=id;
            string verifier=SpotifyProtocol.RandomSecret(),state=SpotifyProtocol.RandomSecret();
            var uri=new Uri("https://accounts.spotify.com/authorize?response_type=code&client_id="+id+"&redirect_uri="+Uri.EscapeDataString(SpotifyProtocol.Redirect)+"&scope="+Uri.EscapeDataString(SpotifyProtocol.Scopes)+"&state="+state+"&code_challenge_method=S256&code_challenge="+SpotifyProtocol.PkceChallenge(verifier));
            string code=await authorize(uri,state,login.Token);Check(epoch);
            using var body=new FormUrlEncodedContent(new Dictionary<string,string>{{"grant_type","authorization_code"},{"code",code},{"redirect_uri",SpotifyProtocol.Redirect},{"client_id",id},{"code_verifier",verifier}});
            using var request=new HttpRequestMessage(HttpMethod.Post,"https://accounts.spotify.com/api/token"){Content=body};
            using var response=await http.SendAsync(request,HttpCompletionOption.ResponseHeadersRead,login.Token);Check(epoch);await CheckResponse(response,login.Token);
            using var json=JsonDocument.Parse(await ReadBody(response.Content,login.Token));var root=json.RootElement;
            var granted=(root.TryGetProperty("scope",out var scopes)?scopes.GetString()??"":"").Split(' ',StringSplitOptions.RemoveEmptyEntries);
            if(!SpotifyProtocol.Scopes.Split(' ').All(granted.Contains))throw new SpotifyFailure("Spotify did not grant the required permissions. Connect again.");
            string nextRefresh=root.GetProperty("refresh_token").GetString()??"",nextAccess=root.GetProperty("access_token").GetString()??"";
            if(!ValidToken(nextRefresh) || !ValidToken(nextAccess))throw new SpotifyFailure("Spotify returned an invalid connection. Try again.");
            var expiry=Expiration(root);Check(epoch);keys.Write(CredentialId,JsonSerializer.Serialize(new SpotifyStoredToken(id,nextRefresh)));
            tokenSaved=true;store.Save("spotify-connection",new SpotifyConnectionData{ClientId=id,Enabled=true});
            refresh=nextRefresh;access=nextAccess;expires=expiry;suspended=false;Status="Connected to Spotify.";return true;
        }
        catch(Exception ex)
        {
            if(epoch==generation)
            {
                if(tokenSaved)try{keys.Remove(CredentialId);}catch{NeedsDisconnectCleanup=true;}
                access=refresh="";Status=FailureMessage(ex,Connecting);
            }
            return false;
        }
        finally{Connecting=Busy=false;gate.Release();if(!disposed){Changed?.Invoke();if(consumers.Count>0 && Connected)poll.Start();}}
    }
    internal bool Disconnect()
    {
        if(disposed)return false;
        // Stop processing immediately, even when either durable store is unavailable.
        generation++;lifetime.Cancel();lifetime.Dispose();lifetime=new();login.Cancel();access=refresh="";track=null;Saved=null;nextRequest=default;suspended=false;poll.Stop();refreshQueued=false;
        bool saved=true,removed=true;
        try{store.Save("spotify-connection",new SpotifyConnectionData{ClientId=ClientId,Enabled=false});}catch{saved=false;}
        try{keys.Remove(CredentialId);}catch{removed=false;}
        NeedsDisconnectCleanup=!saved || !removed;
        Status=NeedsDisconnectCleanup?"Spotify is disconnected, but saved access could not be fully removed. Retry Disconnect Spotify.":"Spotify is not connected.";Changed?.Invoke();return !NeedsDisconnectCleanup;
    }
    internal Task<bool> RefreshAsync()=>Run(false);
    internal Task<bool> ToggleAsync()=>Run(true);
    private async Task<bool> Run(bool toggle)
    {
        if(disposed || !Connected || suspended || !networkEnabled || DateTimeOffset.UtcNow<nextRequest || !SelectedSpotify)return false;
        if(!gate.Wait(0)){if(!toggle)refreshQueued=true;return false;}
        int epoch=generation;var local=snapshot()!;Busy=true;Changed?.Invoke();
        using var cancel=CancellationTokenSource.CreateLinkedTokenSource(lifetime.Token);cancel.CancelAfter(TimeSpan.FromSeconds(20));if(!toggle)activeRead=cancel;
        try
        {
            using var playing=await Send(HttpMethod.Get,"me/player/currently-playing",epoch,cancel.Token);
            Check(epoch);SameSong(local);
            if(playing.StatusCode==HttpStatusCode.NoContent){track=null;Saved=null;Status="Spotify has no current track.";await RetryTrackLookup(toggle,cancel.Token);return false;}
            using var json=JsonDocument.Parse(await ReadBody(playing.Content,cancel.Token));
            if(!json.RootElement.TryGetProperty("item",out var item) || item.ValueKind!=JsonValueKind.Object || !item.TryGetProperty("type",out var type) || type.GetString()!="track"){track=null;Saved=null;Status="Spotify has no current track.";await RetryTrackLookup(toggle,cancel.Token);return false;}
            var found=new SpotifyTrack(item.GetProperty("uri").GetString()??"",item.GetProperty("name").GetString()??"",item.GetProperty("artists").EnumerateArray().Select(a=>a.GetProperty("name").GetString()??"").ToArray(),item.GetProperty("duration_ms").GetDouble()/1000);
            if(!SpotifyProtocol.Matches(found,local)){track=null;Saved=null;Status="Spotify is playing a different track. Wait or select Spotify.";await RetryTrackLookup(toggle,cancel.Token);return false;}
            string query="?uris="+Uri.EscapeDataString(found.Uri);
            using var contains=await Send(HttpMethod.Get,"me/library/contains"+query,epoch,cancel.Token);
            using var flags=JsonDocument.Parse(await ReadBody(contains.Content,cancel.Token));var array=flags.RootElement;
            if(array.ValueKind!=JsonValueKind.Array || array.GetArrayLength()!=1 || array[0].ValueKind is not (JsonValueKind.True or JsonValueKind.False))throw new SpotifyFailure("Spotify returned an invalid response. Try again.");
            bool saved=array[0].GetBoolean();Check(epoch);SameSong(local);
            if(toggle){using var changed=await Send(saved?HttpMethod.Delete:HttpMethod.Put,"me/library"+query,epoch,cancel.Token,local);Check(epoch);SameSong(local);saved=!saved;}
            track=found;Saved=saved;lastChecked=DateTimeOffset.UtcNow;syncRetries=0;Status=toggle?(saved?"Added to Liked Songs.":"Removed from Liked Songs."):"Connected to Spotify.";return true;
        }
        catch(Exception ex){if(epoch==generation){Saved=null;track=null;Status=FailureMessage(ex,false);}return false;}
        finally
        {
            if(ReferenceEquals(activeRead,cancel))activeRead=null;
            Busy=false;gate.Release();bool again=refreshQueued;refreshQueued=false;
            if(!disposed){Changed?.Invoke();if(again)_=RefreshAsync();}
        }
    }
    private async Task RetryTrackLookup(bool toggle,CancellationToken cancel)
    {
        // Spotify metadata can trail Windows at a track boundary. Retry only reads,
        // briefly and a bounded number of times; library writes are never replayed.
        if(toggle || syncRetries++>=3)return;
        await Task.Delay(300,cancel);refreshQueued=true;
    }
    private void SameSong(SpotifyMediaSnapshot original){if(snapshot()!=original)throw new SpotifyFailure("The track changed. Try again.");}
    private void Check(int epoch){if(disposed || epoch!=generation)throw new OperationCanceledException();}
    private async Task<HttpResponseMessage> Send(HttpMethod method,string path,int epoch,CancellationToken cancel,SpotifyMediaSnapshot? expected=null)
    {
        for(int attempt=0;attempt<2;attempt++)
        {
            if(access.Length==0 || expires<=DateTimeOffset.UtcNow)await RefreshToken(epoch,cancel);
            Check(epoch);if(expected!=null)SameSong(expected);using var request=new HttpRequestMessage(method,"https://api.spotify.com/v1/"+path);request.Headers.Authorization=new AuthenticationHeaderValue("Bearer",access);
            var response=await http.SendAsync(request,HttpCompletionOption.ResponseHeadersRead,cancel);
            try{Check(epoch);if(response.StatusCode==HttpStatusCode.Unauthorized && attempt==0){response.Dispose();access="";continue;}await CheckResponse(response,cancel);return response;}catch{response.Dispose();throw;}
        }
        throw new SpotifyFailure("Spotify sign-in expired. Disconnect and connect again.");
    }
    private async Task RefreshToken(int epoch,CancellationToken cancel)
    {
        Check(epoch);using var body=new FormUrlEncodedContent(new Dictionary<string,string>{{"grant_type","refresh_token"},{"refresh_token",refresh},{"client_id",ClientId}});
        using var request=new HttpRequestMessage(HttpMethod.Post,"https://accounts.spotify.com/api/token"){Content=body};
        using var response=await http.SendAsync(request,HttpCompletionOption.ResponseHeadersRead,cancel);Check(epoch);await CheckResponse(response,cancel);
        using var json=JsonDocument.Parse(await ReadBody(response.Content,cancel));var root=json.RootElement;
        string token=root.GetProperty("access_token").GetString()??"";if(!ValidToken(token))throw new SpotifyFailure("Spotify sign-in expired. Disconnect and connect again.");
        string rotated=root.TryGetProperty("refresh_token",out var next)?next.GetString()??"":refresh;if(!ValidToken(rotated))throw new SpotifyFailure("Spotify returned an invalid connection. Try again.");
        Check(epoch);if(rotated!=refresh)keys.Write(CredentialId,JsonSerializer.Serialize(new SpotifyStoredToken(ClientId,rotated)));
        access=token;refresh=rotated;expires=Expiration(root);
    }
    private static DateTimeOffset Expiration(JsonElement root)=>DateTimeOffset.UtcNow.AddSeconds(Math.Clamp(root.GetProperty("expires_in").GetDouble()-30,1,86400));
    private async Task CheckResponse(HttpResponseMessage response,CancellationToken cancel)
    {
        if(response.IsSuccessStatusCode)return;
        if((int)response.StatusCode==429){var after=response.Headers.RetryAfter;nextRequest=after?.Date??DateTimeOffset.UtcNow.Add(after?.Delta??TimeSpan.FromSeconds(60));if(nextRequest<DateTimeOffset.UtcNow.AddSeconds(1))nextRequest=DateTimeOffset.UtcNow.AddSeconds(1);throw new SpotifyFailure("Spotify is limiting requests. Wait before trying again.");}
        if(response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.BadRequest)suspended=true;
        if(response.StatusCode==HttpStatusCode.Forbidden)nextRequest=DateTimeOffset.UtcNow.AddMinutes(5);
        // Never expose/log token bodies or provider error descriptions.
        await Task.CompletedTask;
        throw new SpotifyFailure(response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.BadRequest?"Spotify sign-in expired. Disconnect and connect again.":response.StatusCode==HttpStatusCode.Forbidden?"Spotify denied access. Check Premium and the app user list in Developer Dashboard.":"Spotify is unavailable. Try again later.");
    }
    private static bool ValidToken(string token)=>!string.IsNullOrEmpty(token) && token.Length<=2048 && !token.Any(char.IsControl) && !token.Any(char.IsWhiteSpace);
    private static string FailureMessage(Exception ex,bool connecting)=>ex is SpotifyFailure?ex.Message:ex is OperationCanceledException?(connecting?"Spotify sign-in was cancelled or timed out.":"Spotify request timed out. Try again."):"Could not complete the Spotify request. Try again.";
    private static async Task<byte[]> ReadBody(HttpContent content,CancellationToken cancel)
    {
        using var stream=await content.ReadAsStreamAsync(cancel);using var output=new MemoryStream();byte[] buffer=new byte[4096];int read;
        while((read=await stream.ReadAsync(buffer,cancel))>0){if(output.Length+read>65536)throw new SpotifyFailure("Spotify returned an invalid response. Try again.");output.Write(buffer,0,read);}return output.ToArray();
    }
    public void Dispose(){if(disposed)return;disposed=true;generation++;poll.Stop();consumers.Clear();login.Cancel();lifetime.Cancel();login.Dispose();lifetime.Dispose();http.Dispose();access=refresh="";}
}
