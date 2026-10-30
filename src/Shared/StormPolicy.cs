using System;
namespace ReturnOfThunderstorms;
public sealed class StormSettings
{
 public bool Enabled{get;set;}=true;
 public float RainBoost{get;set;}
 public static bool Valid(StormSettings value)=>!float.IsNaN(value.RainBoost)&&!float.IsInfinity(value.RainBoost)&&value.RainBoost>=0&&value.RainBoost<=100;
}
public static class StormPolicy
{
 public static double RainProbability(double baseline,double boost)=>baseline+(1-baseline)*Math.Max(0,Math.Min(100,boost))/100;
 public static bool Select(double percentage,double roll)=>percentage>0&&roll>=0&&roll<Math.Min(100,percentage)/100;
 public static bool Outdoor(string? map)=>!string.IsNullOrWhiteSpace(map)&&!map!.Equals("laboratory",StringComparison.OrdinalIgnoreCase)&&!map.Equals("labyrinth",StringComparison.OrdinalIgnoreCase)&&!map.StartsWith("factory",StringComparison.OrdinalIgnoreCase);
}
public sealed class RainEpisode
{
 public bool Observed{get;private set;}public bool Selected{get;private set;}
 private double drySince=-1;
 public void Reset(){Observed=Selected=false;drySince=-1;}
 public bool Tick(double now,bool enabled,bool raining,double chance,double roll)
 {
  if(!enabled){Reset();return false;}
  if(raining)
  {
   drySince=-1;if(!Observed){Observed=true;Selected=StormPolicy.Select(chance,roll);}return Selected;
  }
  if(!Observed)return false;
  if(drySince<0)drySince=now;
  if(now-drySince<30)return Selected;
  Reset();return false;
 }
}
