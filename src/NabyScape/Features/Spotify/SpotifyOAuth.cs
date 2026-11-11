using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
namespace NabyScape;
internal sealed class SpotifyFailure : Exception
{internal SpotifyFailure(string message):base(message){} }
internal static class SpotifyOAuth
{
    internal static async Task<string> Authorize(Uri authorize,string state,CancellationToken cancel)
    {
        var listener=new TcpListener(IPAddress.Loopback,43821);
        try
        {
            listener.Start();Process.Start(new ProcessStartInfo(authorize.AbsoluteUri){UseShellExecute=true});
            for(int attempt=0;attempt<24;attempt++)
            {
                using var client=await listener.AcceptTcpClientAsync(cancel);
                using var request=CancellationTokenSource.CreateLinkedTokenSource(cancel);request.CancelAfter(TimeSpan.FromSeconds(5));
                try
                {
                    using var stream=client.GetStream();byte[] buffer=new byte[8192];int used=0;
                    while(used<buffer.Length){int read=await stream.ReadAsync(buffer.AsMemory(used),request.Token);if(read==0)break;used+=read;if(Encoding.ASCII.GetString(buffer,0,used).Contains("\r\n\r\n",StringComparison.Ordinal))break;}
                    string first=Encoding.ASCII.GetString(buffer,0,used).Split("\r\n")[0];var pieces=first.Split(' ');
                    Uri? uri=null;if(pieces.Length==3 && pieces[0]=="GET" && pieces[1].StartsWith('/') && pieces[1].Length<6000)Uri.TryCreate("http://127.0.0.1:43821"+pieces[1],UriKind.Absolute,out uri);
                    var fields=uri==null?null:SpotifyProtocol.CallbackFields(uri,state);string? code=uri==null?null:SpotifyProtocol.CallbackCode(uri,state);
                    bool declined=fields?.ContainsKey("error")==true;bool valid=code!=null || declined;
                    byte[] body=Encoding.UTF8.GetBytes(valid?"<!doctype html><meta charset=utf-8><title>NabyScape</title><p>Запрос получен. Вернитесь в NabyScape. Это окно можно закрыть.</p>":"Invalid callback. Return to NabyScape.");
                    byte[] header=Encoding.ASCII.GetBytes("HTTP/1.1 "+(valid?"200 OK":"400 Bad Request")+"\r\nContent-Type: text/html; charset=utf-8\r\nCache-Control: no-store\r\nConnection: close\r\nContent-Length: "+body.Length+"\r\n\r\n");
                    await stream.WriteAsync(header,request.Token);await stream.WriteAsync(body,request.Token);
                    if(declined)throw new SpotifyFailure("Spotify connection was declined.");if(code!=null)return code;
                }
                catch(OperationCanceledException) when(!cancel.IsCancellationRequested){}
                catch(IOException){}
            }
            throw new SpotifyFailure("Could not receive Spotify authorization. Try connecting again.");
        }
        catch(SocketException){throw new SpotifyFailure("The Spotify connection port is busy. Close another connection window and retry.");}
        finally{listener.Stop();}
    }
}
