using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
namespace NabyScape;
internal static class SpotifyTests
{
    internal static void Run(Action<string,Action> test)
    {
        static void Assert(bool okay){if(!okay)throw new Exception("Spotify assertion failed.");}
        test("Spotify PKCE matches the RFC 7636 SHA256 vector",()=>Assert(SpotifyProtocol.PkceChallenge("dBjftJeZ4CVP-mB92K27uhbUJU1p1r_wW1gFWFOEjXk")=="E9Melhoa2OwvFrEMTJguCHaoeK1t8URWbuGJSstw-cM"));
        test("Spotify accepts only public client IDs and exact track URIs",()=>
        {
            Assert(SpotifyProtocol.ValidClientId(new string('a',32)) && !SpotifyProtocol.ValidClientId("https://evil") && !SpotifyProtocol.ValidClientId(new string('a',31)));
            Assert(SpotifyProtocol.IsTrackUri("spotify:track:0123456789ABCDEFGHIJKL") && !SpotifyProtocol.IsTrackUri("spotify:album:0123456789ABCDEFGHIJKL") && !SpotifyProtocol.IsTrackUri("spotify:track:0123456789ABCDEFGHIJKL&uris=other"));
        });
        test("Spotify identity matches the selected session and rejects other songs artists versions and durations",()=>
        {
            var track=new SpotifyTrack("spotify:track:0123456789ABCDEFGHIJKL","Песня",new[]{"Артист"},180);
            Assert(SpotifyProtocol.Matches(track,new("Spotify.exe","Песня","Артист",181)));
            Assert(!SpotifyProtocol.Matches(track,new("Chrome.exe","Песня","Артист",180)) && !SpotifyProtocol.Matches(track,new("Spotify.exe","Песня live","Артист",180)) && !SpotifyProtocol.Matches(track,new("Spotify.exe","Песня","Другой артист",180)) && !SpotifyProtocol.Matches(track,new("Spotify.exe","Песня","Артист",200)));
        });
        test("Spotify OAuth callback requires exact loopback route state and unique query fields",()=>
        {
            string? Code(string url)=>SpotifyProtocol.CallbackCode(new Uri(url),"expected");
            Assert(Code("http://127.0.0.1:43821/callback?state=expected&code=valid")=="valid");
            foreach(string url in new[]{"http://127.0.0.1:43821/callback?state=wrong&code=valid","http://evil.example/callback?state=expected&code=valid","http://127.0.0.1:43822/callback?state=expected&code=valid","http://127.0.0.1:43821/other?state=expected&code=valid","http://127.0.0.1:43821/callback?state=expected&state=expected&code=valid","http://127.0.0.1:43821/callback?state=expected&error=access_denied","http://127.0.0.1:43821/callback?state=expected&code=a%0d%0ab"})Assert(Code(url)==null);
        });
        test("Spotify connects saves and removes the exact current track, persists no tokens in profile JSON and restores securely",()=>
        {
            using var f=new Fixture();Assert(f.Service.ConnectAsync(Fixture.Client).GetAwaiter().GetResult());Assert(f.Keys.Exists(SpotifyService.CredentialId));
            Assert(f.Service.RefreshAsync().GetAwaiter().GetResult() && f.Service.Saved==false && f.Service.CanLike);
            Assert(f.Service.ToggleAsync().GetAwaiter().GetResult() && f.Saved && f.Service.Saved==true);
            Assert(f.Service.ToggleAsync().GetAwaiter().GetResult() && !f.Saved && f.Service.Saved==false);
            Assert(f.Requests.Count(r=>r.Method==HttpMethod.Put)==1 && f.Requests.Count(r=>r.Method==HttpMethod.Delete)==1);
            foreach(var file in Directory.GetFiles(f.Root,"*.json")){string text=File.ReadAllText(file);Assert(!text.Contains("access-secret") && !text.Contains("refresh-secret"));}
            using var restored=new SpotifyService(new Store(f.Root),f.Keys,()=>f.Local,new HttpClient(new Handler(f.Respond)),f.Authorize);
            int requests=f.Requests.Count;Assert(restored.Connected && f.Requests.Count==requests);
            Assert(restored.RefreshAsync().GetAwaiter().GetResult());Assert(f.TokenCalls==2);
        });
        test("Spotify never saves a mismatched track or a track changed while the saved-state check was in flight",()=>
        {
            using var f=new Fixture();Assert(f.Service.ConnectAsync(Fixture.Client).GetAwaiter().GetResult());
            f.Local=f.Local with{Title="Другая песня"};Assert(!f.Service.ToggleAsync().GetAwaiter().GetResult() && !f.Service.CanLike);
            f.Local=f.Local with{Title="Песня"};f.Intercept=(r,c)=>{if(r.RequestUri!.AbsolutePath.EndsWith("/contains"))f.Local=f.Local with{Title="Следующая песня"};return null;};
            Assert(!f.Service.ToggleAsync().GetAwaiter().GetResult() && !f.Requests.Any(r=>r.Method==HttpMethod.Put || r.Method==HttpMethod.Delete));
        });
        test("Spotify rechecks song identity after token refresh immediately before a library mutation",()=>
        {
            using var f=new Fixture();Assert(f.Service.ConnectAsync(Fixture.Client).GetAwaiter().GetResult());
            f.Intercept=(r,c)=>
            {
                if(r.RequestUri!.AbsolutePath.EndsWith("/contains"))typeof(SpotifyService).GetField("expires",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)!.SetValue(f.Service,DateTimeOffset.MinValue);
                if(r.RequestUri.AbsolutePath=="/api/token")f.Local=f.Local with{Title="Сменилась во время обновления входа"};return null;
            };
            Assert(!f.Service.ToggleAsync().GetAwaiter().GetResult() && !f.Requests.Any(r=>r.Method==HttpMethod.Put));
        });
        test("Spotify checks a new track before the music panel is opened",()=>
        {
            using var f=new Fixture();Assert(f.Service.ConnectAsync(Fixture.Client).GetAwaiter().GetResult());f.Saved=true;
            f.Service.MediaChanged();Assert(f.Service.Saved==true && f.Service.CanLike && f.Requests.Any(r=>r.Path.EndsWith("/contains")));
        });
        test("Spotify briefly retries stale metadata and bounds retries without library writes",()=>Task.Run(async()=>
        {
            using var f=new Fixture();Assert(await f.Service.ConnectAsync(Fixture.Client));f.ApiTitle="Предыдущая песня";
            int lookups=0;f.Intercept=(r,c)=>{if(r.RequestUri!.AbsolutePath.EndsWith("currently-playing") && ++lookups==2)f.ApiTitle=f.Local.Title;return null;};
            await f.Service.RefreshAsync();for(int i=0;i<100 && f.Service.Saved==null;i++)await Task.Delay(10);
            Assert(lookups==2 && f.Service.Saved==false && !f.Requests.Any(r=>r.Method==HttpMethod.Put || r.Method==HttpMethod.Delete));
            using var wrong=new Fixture();Assert(await wrong.Service.ConnectAsync(Fixture.Client));wrong.ApiTitle="Всегда другой трек";
            await wrong.Service.RefreshAsync();await Task.Delay(1300);
            Assert(wrong.Service.Saved==null && !wrong.Service.CanLike && wrong.Requests.Count(r=>r.Path.EndsWith("currently-playing"))==4);
        }).GetAwaiter().GetResult());
        test("Spotify immediately follows a track change during an in-flight lookup",()=>Task.Run(async()=>
        {
            using var f=new Fixture();Assert(await f.Service.ConnectAsync(Fixture.Client));
            var waiting=new TaskCompletionSource<HttpResponseMessage>(TaskCreationOptions.RunContinuationsAsynchronously);var old=f.TrackResponse();bool first=true;
            f.Intercept=(r,c)=>{if(first && r.RequestUri!.AbsolutePath.EndsWith("currently-playing")){first=false;return waiting.Task;}return null;};
            var pending=f.Service.RefreshAsync();f.Local=f.Local with{Title="Следующая песня"};f.ApiTitle=f.Local.Title;f.Saved=true;f.Service.MediaChanged();waiting.SetResult(old);await pending;
            for(int i=0;i<50 && f.Service.Saved!=true;i++)await Task.Delay(10);
            Assert(f.Service.Saved==true && f.Service.CanLike && f.Requests.Count(r=>r.Path.EndsWith("currently-playing"))==2);
        }).GetAwaiter().GetResult());
        test("Spotify disconnect retires an old request and cannot restore its heart or connection",()=>
        {
            using var f=new Fixture();Assert(f.Service.ConnectAsync(Fixture.Client).GetAwaiter().GetResult());
            var waiting=new TaskCompletionSource<HttpResponseMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
            f.Intercept=(r,c)=>r.RequestUri!.AbsolutePath.EndsWith("currently-playing")?waiting.Task:null;
            var pending=f.Service.ToggleAsync();Assert(f.Service.Busy && f.Service.Disconnect());waiting.SetResult(f.TrackResponse());
            Assert(!pending.GetAwaiter().GetResult() && !f.Service.Connected && f.Service.Saved==null && !f.Keys.Exists(SpotifyService.CredentialId) && !f.Saved);
        });
        test("Spotify limits 401 refresh to once and suspends expired authorization until reconnect",()=>
        {
            using var f=new Fixture();Assert(f.Service.ConnectAsync(Fixture.Client).GetAwaiter().GetResult());
            f.Intercept=(r,c)=>r.RequestUri!.Host=="api.spotify.com"?Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized)):null;
            Assert(!f.Service.RefreshAsync().GetAwaiter().GetResult() && f.TokenCalls==2);int count=f.Requests.Count;
            Assert(!f.Service.RefreshAsync().GetAwaiter().GetResult() && f.Requests.Count==count && !f.Service.CanLike);
        });
        test("Spotify respects Retry-After without another request and hides unsynchronized heart state",()=>
        {
            using var f=new Fixture();Assert(f.Service.ConnectAsync(Fixture.Client).GetAwaiter().GetResult());
            f.Intercept=(r,c)=>{var response=new HttpResponseMessage((HttpStatusCode)429);response.Headers.RetryAfter=new System.Net.Http.Headers.RetryConditionHeaderValue(TimeSpan.FromSeconds(60));return Task.FromResult(response);};
            Assert(!f.Service.RefreshAsync().GetAwaiter().GetResult());int count=f.Requests.Count;
            Assert(!f.Service.ToggleAsync().GetAwaiter().GetResult() && f.Requests.Count==count && !f.Service.CanLike);
        });
        test("Spotify does not retry an ambiguous library mutation and later reads the actual saved state",()=>
        {
            using var f=new Fixture();Assert(f.Service.ConnectAsync(Fixture.Client).GetAwaiter().GetResult());
            f.Intercept=(r,c)=>{if(r.Method==HttpMethod.Put){f.Saved=true;return Task.FromException<HttpResponseMessage>(new HttpRequestException("mock transport interruption"));}return null;};
            Assert(!f.Service.ToggleAsync().GetAwaiter().GetResult() && f.Service.Saved==null && f.Requests.Count(r=>r.Method==HttpMethod.Put)==1);
            Assert(f.Service.RefreshAsync().GetAwaiter().GetResult() && f.Service.Saved==true);
        });
        test("Spotify rejects missing scopes and credential write failures without claiming connected",()=>
        {
            using var f=new Fixture();f.TokenScopes="user-read-currently-playing";Assert(!f.Service.ConnectAsync(Fixture.Client).GetAwaiter().GetResult() && !f.Service.Connected && !f.Keys.Exists(SpotifyService.CredentialId));
            f.TokenScopes=SpotifyProtocol.Scopes;f.Keys.FailWrite=true;Assert(!f.Service.ConnectAsync(Fixture.Client).GetAwaiter().GetResult() && !f.Service.Connected && !f.Keys.Exists(SpotifyService.CredentialId));
        });
        test("Spotify stops processing even when credential deletion fails and persists the disabled connection",()=>
        {
            using var f=new Fixture();Assert(f.Service.ConnectAsync(Fixture.Client).GetAwaiter().GetResult());f.Keys.FailRemove=true;Assert(!f.Service.Disconnect() && !f.Service.Connected && f.Keys.Exists(SpotifyService.CredentialId));
            int count=f.Requests.Count;Assert(!f.Service.RefreshAsync().GetAwaiter().GetResult() && f.Requests.Count==count);
            using var restored=new SpotifyService(new Store(f.Root),f.Keys,()=>f.Local,new HttpClient(new Handler(f.Respond)),f.Authorize);Assert(!restored.Connected);
        });
    }
    private sealed class Keys : IAssistantKeys
    {
        private readonly Dictionary<string,string> values=new();internal bool FailWrite,FailRemove;
        public string? Read(string id)=>values.GetValueOrDefault(id);public bool Exists(string id)=>values.ContainsKey(id);
        public void Write(string id,string value){if(FailWrite)throw new IOException("mock locked credential");values[id]=value;}
        public void Remove(string id){if(FailRemove)throw new IOException("mock locked credential");values.Remove(id);}
    }
    private sealed class Handler(Func<HttpRequestMessage,CancellationToken,Task<HttpResponseMessage>> respond) : HttpMessageHandler
    {protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken cancellationToken)=>respond(request,cancellationToken);}
    private sealed class Fixture : IDisposable
    {
        internal const string Client="aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
        internal readonly string Root=Path.Combine(Path.GetTempPath(),"NabyScape-Spotify-"+Guid.NewGuid().ToString("N"));
        internal readonly Keys Keys=new();internal readonly SpotifyService Service;
        internal SpotifyMediaSnapshot Local=new("Spotify.exe","Песня","Артист",180);
        internal readonly List<(HttpMethod Method,string Path)> Requests=new();
        internal Func<HttpRequestMessage,CancellationToken,Task<HttpResponseMessage>?>? Intercept;
        internal string ApiTitle="Песня";internal bool Saved;internal int TokenCalls;internal string TokenScopes=SpotifyProtocol.Scopes;
        internal Fixture(){Directory.CreateDirectory(Root);Service=new(new Store(Root),Keys,()=>Local,new HttpClient(new Handler(Respond)),Authorize);}
        internal Task<string> Authorize(Uri uri,string state,CancellationToken cancel)
        {if(uri.Host!="accounts.spotify.com" || !uri.Query.Contains("code_challenge_method=S256") || !uri.Query.Contains("state="+state))throw new Exception("Invalid authorization URI.");return Task.FromResult("mock-authorization-code");}
        private static HttpResponseMessage Json(object value)=>new(HttpStatusCode.OK){Content=new StringContent(JsonSerializer.Serialize(value))};
        internal HttpResponseMessage TrackResponse()=>Json(new{item=new{type="track",uri="spotify:track:0123456789ABCDEFGHIJKL",name=ApiTitle,artists=new[]{new{name="Артист"}},duration_ms=180000}});
        internal Task<HttpResponseMessage> Respond(HttpRequestMessage request,CancellationToken cancel)
        {
            Requests.Add((request.Method,request.RequestUri!.AbsolutePath));var intercepted=Intercept?.Invoke(request,cancel);if(intercepted!=null)return intercepted;
            if(request.RequestUri.Host=="accounts.spotify.com"){TokenCalls++;return Task.FromResult(Json(new{access_token="access-secret",refresh_token="refresh-secret",expires_in=3600,scope=TokenScopes,token_type="Bearer"}));}
            if(request.Headers.Authorization?.Parameter!="access-secret")throw new Exception("Missing mock authorization.");
            if(request.RequestUri.AbsolutePath.EndsWith("currently-playing"))return Task.FromResult(TrackResponse());
            if(request.RequestUri.AbsolutePath.EndsWith("/contains"))return Task.FromResult(Json(new[]{Saved}));
            if(request.RequestUri.AbsolutePath!="/v1/me/library" || request.RequestUri.Query!="?uris=spotify%3Atrack%3A0123456789ABCDEFGHIJKL")throw new Exception("Wrong library target.");
            if(request.Method==HttpMethod.Put)Saved=true;else if(request.Method==HttpMethod.Delete)Saved=false;else throw new Exception("Wrong library method.");
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        }
        public void Dispose(){Service.Dispose();Directory.Delete(Root,true);}
    }
}
