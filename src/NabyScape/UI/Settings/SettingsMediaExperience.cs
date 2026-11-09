using System;
using System.Windows;
using System.Windows.Controls;

namespace NabyScape;

public sealed partial class SettingsWindow
{
    private void MediaExperienceSettings()
    {
        var s=app.Settings;var mode=new ComboBox{ItemsSource=new[]{"Frequency spectrum","Amplitude history"},SelectedIndex=s.WaveformSource=="Spectrum"?0:1};UI.LocalizedName(mode,"Visualizer input");
        mode.SelectionChanged+=(_,_)=>{s.WaveformSource=mode.SelectedIndex==0?"Spectrum":"Envelope";Save();};
        var sensitivity=new StackPanel();AddSlider(sensitivity,"Visualizer sensitivity",s.WaveformSensitivity,.5,3,v=>s.WaveformSensitivity=v,"×",.05);
        page.Children.Add(Section("Visualizer",mode,sensitivity,UI.Caption("Spectrum separates real bass, midrange and treble from the selected media app's active output. Other apps sharing that output can influence the bands. Muting the selected app silences the display. Output samples stay in memory; microphone capture is never used. Amplitude history remains the fallback for unsupported output formats.",11,UI.Muted)));
        var presets=new ComboBox{ItemsSource=new[]{"Slide · flowing lines","Focus · soft emphasis","Karaoke · line sweep"},SelectedIndex=Array.IndexOf(new[]{"Slide","Focus","Karaoke"},s.LyricsAnimation)};UI.LocalizedName(presets,"Lyrics animation preset");presets.SelectionChanged+=(_,_)=>{if(presets.SelectedIndex<0)return;s.LyricsAnimation=new[]{"Slide","Focus","Karaoke"}[presets.SelectedIndex];Save();};
        var timing=new StackPanel();AddSlider(timing,"Lyrics timing offset",s.LyricsOffset,-10,10,v=>s.LyricsOffset=v," s",.1);var local=UI.Caption("Local LRC files can supply timings when the service is unavailable. Imports last for this app session.",11,UI.Muted);timing.Children.Add(UI.CaptionButton("Load local timed lyrics…",()=>local.Text=LyricsFiles.Import(app,this)));timing.Children.Add(local);
        var textMode=new ComboBox{ItemsSource=new[]{"Original script","Japanese Romaji · Latin letters"},SelectedIndex=s.LyricsTextMode=="Romaji"?1:0};UI.LocalizedName(textMode,"Lyrics writing system");
        textMode.SelectionChanged+=(_,_)=>{s.LyricsTextMode=textMode.SelectedIndex==1?"Romaji":"Original";app.Lyrics.SetTextMode(s.LyricsTextMode);Save();};
        timing.Children.Insert(0,UI.Caption("Lyric text",11,UI.Muted));timing.Children.Insert(1,textMode);
        page.Children.Add(Section("Lyrics",LyricsAnimationExample(),Check("Show synced lyrics in the media panel",s.LyricsEnabled,v=>s.LyricsEnabled=v),UI.Caption("Automatic finds timed lyrics across public catalogues using the current title, artist and duration. Choose a source from the Lyrics menu. Romaji is generated offline; availability varies by track.",11,UI.Muted),presets,timing,UI.Caption("The lyrics icon beside playback controls toggles this preference. Slide follows each new line; Focus softly emphasizes it; Karaoke sweeps across the line using line timing. Reduce Motion keeps the text static. Tab to the lyrics button and press Space to show or hide.",11,UI.Muted)));
    }
    private FrameworkElement LyricsAnimationExample()
    {
        var demo=new StackPanel{Margin=new(0,4,0,14)};
        demo.Children.Add(UI.Caption("A little closer to the music",11,UI.Muted));
        var line=UI.Caption("One line, right on time",18);line.FontWeight=FontWeights.SemiBold;line.Margin=new(0,8,0,8);demo.Children.Add(line);
        demo.Children.Add(UI.Caption("Let the next moment arrive",12,UI.Muted));
        var feedback=UI.Caption("Static sample; song lyrics animate on the dock.",11,UI.Muted);
        demo.Children.Add(new LyricsSourcesControl(app,()=>app.Lyrics.Retry(),()=>feedback.Text=LyricsFiles.Import(app,this)){Margin=new(0,10,0,5)});demo.Children.Add(feedback);return demo;
    }
    private void DockEditingSettings()
    {
        page.Children.Add(Section("Move & resize",new DockPositionEditor(app,Save),Check("Show island move and resize handles",app.Settings.DockUnlocked,value=>app.Settings.DockUnlocked=value),UI.Caption("Drag the top grip to reposition the island. Drag either lower corner to resize the current state: closed, hover or open panel. Position and each size are saved separately. Hold Alt while the island is focused to reveal handles temporarily; Esc cancels a drag. Dragging down detaches a notch into a pill; a drag that begins as a notch can snap back to the top edge.",11,UI.Muted)));
    }
    private void CompletionSoundSettings()
    {
        var s=app.Settings;var sounds=new ComboBox{ItemsSource=new[]{"Chime","Bell","Custom","Off"},SelectedItem=s.CountdownSound};UI.LocalizedName(sounds,"Timer and Focus completion sound");
        var path=UI.Text(s.CountdownSoundFile.Length>0?System.IO.Path.GetFileName(s.CountdownSoundFile):Localization.Text("No custom sound chosen"),11,UI.Muted);
        var volume=new StackPanel();AddSlider(volume,"Completion sound volume",s.CountdownSoundVolume*100,0,100,value=>s.CountdownSoundVolume=value/100,"%");
        sounds.SelectionChanged+=(_,_)=>{s.CountdownSound=sounds.SelectedItem?.ToString()??"Chime";Save();};
        var feedback=UI.Caption("",11,UI.Muted);
        var choose=UI.CaptionButton("Choose WAV…",()=>{var dialog=new Microsoft.Win32.OpenFileDialog{Filter=Localization.Text("Wave audio (*.wav)|*.wav"),Title=Localization.Text("Choose a completion sound (up to 15 seconds)")};if(dialog.ShowDialog(this)!=true)return;s.CountdownSoundFile=dialog.FileName;s.CountdownSound="Custom";sounds.SelectedItem="Custom";path.Text=System.IO.Path.GetFileName(dialog.FileName);Save();});
        page.Children.Add(Section("Completion sound",sounds,volume,path,choose,UI.CaptionButton("Preview sound",()=>{app.CountdownAudio.Play(s);feedback.Text=app.CountdownAudio.LastError??Localization.Text("Played through your Windows output. Sound does not depend on Windows notifications.");}),feedback,UI.Caption("Timer and Focus play this sound directly when their per-type notification toggle is on. It uses the current Windows output; Off silences it. Custom WAV files must be under 10 MB and at most 15 seconds. File paths are kept locally and excluded from layout exports.",11,UI.Muted)));
    }
}
