using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using Object=UnityEngine.Object;
namespace ReturnOfThunderstorms;

internal sealed class StormAudioAssets
{
 private sealed class Loaded{public string Name="",Error="";public WavePcm? Wave;}
 private readonly string[] names={"Thunder1.wav","ThunderReplacement.wav","Thunder3.wav","HeavyRain.wav"};
 private Task<Loaded[]>? loading;private int uploaded;
 private readonly AudioClip?[] clips=new AudioClip?[4];
 public AudioClip[] Thunder{get;private set;}=Array.Empty<AudioClip>();
 public AudioClip? Rain=>clips[3]!=null&&clips[3]!.samples>0?clips[3]:null;
 private volatile bool invalidated;
 public void AudioConfigurationChanged(bool deviceWasChanged)=>invalidated=true;
 private void Rebuild()
 {
  foreach(var clip in clips)if(clip!=null)Object.Destroy(clip);
  Array.Clear(clips,0,clips.Length);Thunder=Array.Empty<AudioClip>();uploaded=0;invalidated=false;
  Plugin.Trace("[ROT-STORM-AUDIO] rebuilding procedural clips after audio reset or lost sample data");
 }
 public void Tick()
 {
  if(loading==null)
  {
   string folder=Path.Combine(Path.GetDirectoryName(typeof(Plugin).Assembly.Location)!,"Audio");
   loading=Task.Run(()=>names.Select(name=>{try{return new Loaded{Name=name,Wave=WavePcm.Read(Path.Combine(folder,name))};}catch(Exception e){return new Loaded{Name=name,Error=e.GetBaseException().Message};}}).ToArray());
  }
  if(!loading.IsCompleted)return;
  if(invalidated||uploaded>=clips.Length&&clips.Any(c=>c!=null&&c.samples<=0))Rebuild();
  if(uploaded>=clips.Length)return;
  if(loading.IsFaulted){Plugin.Trace("[ROT-STORM-AUDIO] load-error="+loading.Exception?.GetBaseException().Message);uploaded=clips.Length;return;}
  var item=loading.Result[uploaded];
  if(item.Wave is WavePcm wave)
  {
   var uploadTime=System.Diagnostics.Stopwatch.StartNew();
   var clip=AudioClip.Create(item.Name,wave.Samples.Length/wave.Channels,wave.Channels,wave.Rate,false);
   if(!clip.SetData(wave.Samples,0)||clip.samples<=0){Object.Destroy(clip);Plugin.Trace("[ROT-STORM-AUDIO] upload-failed="+item.Name);}
   else{clips[uploaded]=clip;Plugin.Trace("[ROT-STORM-AUDIO] loaded="+item.Name+" seconds="+clip.length.ToString("0.0")+" samples="+clip.samples+" uploadMs="+uploadTime.Elapsed.TotalMilliseconds.ToString("0.0"));}
   // Retain decoded PCM so Unity audio resets can be repaired without disk I/O.
  }
  else Plugin.Trace("[ROT-STORM-AUDIO] missing="+item.Name+" error="+item.Error);
  uploaded++;if(uploaded>=2)Thunder=clips.Take(3).Where(c=>c!=null).Cast<AudioClip>().ToArray();
 }
 public void Dispose(){foreach(var clip in clips)if(clip!=null)Object.Destroy(clip);Thunder=Array.Empty<AudioClip>();}
}
