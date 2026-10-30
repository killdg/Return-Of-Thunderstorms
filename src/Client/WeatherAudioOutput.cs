using Comfort.Common;
using UnityEngine;
namespace ReturnOfThunderstorms;

internal static class WeatherAudioOutput
{
 // Do not inherit a pooled source's route: it may retain an occluded/muted
 // group until native Play explicitly assigns the source group's mixer.
 internal static string Describe(AudioSource source)=>$"playing={source.isPlaying} enabled={source.enabled} active={source.gameObject.activeInHierarchy} mute={source.mute} virtual={source.isVirtual} pitch={source.pitch:0.00} priority={source.priority} clipState={source.clip?.loadState} samples={source.clip?.samples} dsp={AudioSettings.dspTime:0.000}";
 internal static bool Attach(AudioSource source)
 {
  if(!Singleton<BetterAudio>.Instantiated||Singleton<BetterAudio>.Instance.MasterMixerGroup==null)return false;
  var group=Singleton<BetterAudio>.Instance.MasterMixerGroup;
  if(source.outputAudioMixerGroup!=group)
  {
   source.outputAudioMixerGroup=group;source.spatialBlend=0;source.spatialize=false;
   Plugin.Trace("[ROT-AUDIO] output="+source.gameObject.name+" mixer="+group.name);
  }
  source.enabled=true;source.mute=false;source.pitch=1;source.priority=16;
  // Keep procedural weather near the active player as well as using a 2D route.
  // This also avoids origin-distance handling by native audio extensions.
  if(Singleton<EFT.GameWorld>.Instantiated&&Singleton<EFT.GameWorld>.Instance.MainPlayer is {} player)source.transform.position=player.Position;
  return true;
 }
}

internal sealed class WorldAudioDucking
{
 private UnityEngine.Audio.AudioMixer? mixer;private bool owned;
 private float original,applied;
 internal float AppliedDb=>owned?applied:0;
 internal void Tick(float gain)
 {
  if(gain>=0.9999f){Stop();return;}
  if(!Singleton<BetterAudio>.Instantiated)return;
  var current=Singleton<BetterAudio>.Instance.Master;if(current==null)return;
  if(mixer!=current){Stop();mixer=current;}
  if(!mixer.GetFloat(AudioGainPolicy.WorldParameter,out var value))return;
  if(!owned||Mathf.Abs(value-applied)>0.01f)original=value;
  applied=AudioGainPolicy.ApplyDb(original,gain);
  owned=mixer.SetFloat(AudioGainPolicy.WorldParameter,applied);
 }
 internal void Stop()
 {
  if(owned&&mixer!=null&&mixer.GetFloat(AudioGainPolicy.WorldParameter,out var value)&&Mathf.Abs(value-applied)<0.01f)
   mixer.SetFloat(AudioGainPolicy.WorldParameter,original);
  owned=false;mixer=null;
 }
}
