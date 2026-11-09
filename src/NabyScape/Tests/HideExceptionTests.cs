using System;
using System.Linq;
using System.IO;
using System.Text.Json;
namespace NabyScape;
internal static class HideExceptionTests
{
    internal static void Run(Action<string,Action> test)
    {
        static void Check(bool ok){if(!ok)throw new Exception("Hide exception assertion failed.");}
        test("Hide exceptions match exact executable names regardless of case",()=>{var names=new[]{"ChatGPT.exe","explorer.exe"};Check(VisibilityExceptionPolicy.IsExcluded(names,"CHATGPT") && VisibilityExceptionPolicy.IsExcluded(names,"Explorer.exe") && !VisibilityExceptionPolicy.IsExcluded(names,"vivaldi.exe") && !VisibilityExceptionPolicy.IsExcluded(names,"FakeChatGPT.exe") && !VisibilityExceptionPolicy.IsExcluded(names,null));});
        test("Hide exceptions normalize duplicates and reject invalid executable identities",()=>{var s=new Settings{HideAppExceptions=new(){" ChatGPT.EXE ","chatgpt","explorer.exe","*.exe","bad/name.exe",""}};s.Normalize();Check(s.HideAppExceptions.SequenceEqual(new[]{"chatgpt.exe","explorer.exe"}));});
        test("Hide exceptions preserve browser hiding and bypass only excluded app rules",()=>{var names=new[]{"chatgpt.exe","explorer.exe"};Check(!VisibilityExceptionPolicy.ShouldHide(true,names,"ChatGPT") && !VisibilityExceptionPolicy.ShouldHide(true,names,"explorer") && VisibilityExceptionPolicy.ShouldHide(true,names,"chrome") && !VisibilityExceptionPolicy.ShouldHide(false,names,"chrome"));});
        test("Hide exceptions accept valid executable punctuation",()=>{Check(VisibilityExceptionPolicy.NormalizeName("notepad++.exe")=="notepad++.exe" && VisibilityExceptionPolicy.NormalizeName("My App (x64) & Tools@2.exe")=="my app (x64) & tools@2.exe" && VisibilityExceptionPolicy.NormalizeName(".exe")=="");});
        test("Legacy preference backups restore without newly added exception and wheel fields",()=>
        {
            string folder=Path.Combine(Path.GetTempPath(),"NabyScape-exceptions-"+Guid.NewGuid().ToString("N"));
            try
            {
                var store=new Store(folder);var old=System.Text.Json.Nodes.JsonNode.Parse(JsonSerializer.Serialize(new Settings{Width=333}))!.AsObject();old.Remove("HideAppExceptions");old.Remove("WheelWrapTabs");
                File.WriteAllText(Path.Combine(folder,"preference-backup.json"),old.ToJsonString());
                var target=new Settings{Width=350,WheelWrapTabs=true,HideAppExceptions=new(){"chatgpt.exe"}};PreferenceReset.Restore(store,target);
                Check(target.Width==333 && target.HideAppExceptions.Count==0 && !target.WheelWrapTabs);
            }
            finally{if(Directory.Exists(folder))Directory.Delete(folder,true);}
        });
        test("Hide exceptions persist and old profiles remain compatible",()=>{var s=new Settings{HideAppExceptions=new(){"chatgpt.exe"},Width=333};s=JsonSerializer.Deserialize<Settings>(JsonSerializer.Serialize(s))!;s.Normalize();Check(s.Width==333 && s.HideAppExceptions.Single()=="chatgpt.exe" && JsonSerializer.Deserialize<Settings>("{}")!.HideAppExceptions.Count==0);});
    }
}
