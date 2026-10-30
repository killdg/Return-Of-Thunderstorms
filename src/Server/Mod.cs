using System.Reflection;
using HarmonyLib;
using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Models.Spt.Mod;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Generators.Weather;
using SPTarkov.Server.Core.Services.InRaid;
using SPTarkov.Server.Core.Services.Server;
using SPTarkov.Server.Core.Utils;
namespace ReturnOfThunderstorms;

public record ModMetadata:IModMetadata
{
 public string ModGuid{get;init;}="com.killdg.returnofthunderstorms";
 public string Name{get;init;}="Return Of Thunderstorms";public string Author{get;init;}="killdg";public List<string>? Contributors{get;init;}
 public SemanticVersioning.Version Version{get;init;}=new("1.2.2");public SemanticVersioning.Range SptVersion{get;init;}=new("~4.1.6");
 public List<string>? Incompatibilities{get;init;}public Dictionary<string,SemanticVersioning.Range>? ModDependencies{get;init;}
 public string? Url{get;init;}public bool HasPrepatcher{get;init;}public string License{get;init;}="MIT";
}
public sealed class SettingsRequest:IRequestData{public bool Enabled{get;set;}=true;public float RainBoost{get;set;}}
[Injectable(InjectionType.Singleton)]
public sealed class StormControl(JsonUtil json,RaidWeatherService weather,SeasonalEventService seasons)
{
 internal static readonly object Gate=new();internal static StormSettings Options=new();private string path="";private bool synchronized;
 public void Initialize(string folder)
 {
  path=Path.Combine(folder,"config.json");if(File.Exists(path)){var value=json.Deserialize<StormSettings>(File.ReadAllText(path));if(value!=null&&StormSettings.Valid(value))Options=value;}
 }
 public object Update(SettingsRequest request)
 {
  var value=new StormSettings{Enabled=request.Enabled,RainBoost=request.RainBoost};if(!StormSettings.Valid(value))return new{ok=false,error="Rain boost must be between 0 and 100."};
  lock(Gate)
  {
   bool changed=value.Enabled!=Options.Enabled||value.RainBoost!=Options.RainBoost||!synchronized&&value.Enabled&&value.RainBoost>0;Options=value;synchronized=true;
   if(changed)
   {
    // Rebuild the native forecast so a setting changed before a raid is not
    // ignored by the already-generated 24-hour weather cache.
    var cache=(System.Collections.IList)AccessTools.Field(typeof(RaidWeatherService),"WeatherForecast").GetValue(weather)!;
    cache.Clear();weather.GenerateFutureWeatherAndCache(seasons.GetActiveWeatherSeason());
   }
   File.WriteAllText(path,json.Serialize(value));return new{ok=true,enabled=value.Enabled,rainBoost=value.RainBoost};
  }
 }
}
[Injectable(TypePriority=OnLoadOrder.Routers+1)]
public sealed class SettingsRouter(JsonUtil json,StormControl control):StaticRouter(json,[new RouteAction<SettingsRequest>("/client/returnofthunderstorms/settings",(_,request,_,_,_)=>ValueTask.FromResult(json.Serialize(control.Update(request))!))]){}
[HarmonyPatch(typeof(WeatherGenerator),nameof(WeatherGenerator.GetWeatherPresetWeightsBySeason))]
public static class RainWeights
{
 public static Dictionary<WeatherPreset,double> Apply(Dictionary<WeatherPreset,double> original,StormSettings options,bool winter)
 {
  if(!options.Enabled||options.RainBoost<=0||winter)return original;
  var result=new Dictionary<WeatherPreset,double>(original);double total=original.Values.Sum();
  if(total<=0||!original.TryGetValue(WeatherPreset.RAINY,out var rainy))return original;
  double baseline=rainy/total,target=StormPolicy.RainProbability(baseline,options.RainBoost);
  foreach(var key in original.Keys)result[key]=key==WeatherPreset.RAINY?target*total:baseline>=1?0:original[key]*(1-target)/(1-baseline);
  return result;
 }
 private static void Postfix(Season currentSeason,ref Dictionary<WeatherPreset,double> __result)
 {lock(StormControl.Gate)__result=Apply(__result,StormControl.Options,currentSeason==Season.WINTER);}
}
// Native forecast reads and settings regeneration share the same gate.
[HarmonyPatch]
internal static class ForecastGate
{
 private static IEnumerable<MethodBase> TargetMethods()=>new[]{"GetCurrentWeather","GetUpcomingWeather","GenerateFutureWeatherAndCache"}.Select(n=>(MethodBase)AccessTools.Method(typeof(RaidWeatherService),n));
 private static void Prefix()=>Monitor.Enter(StormControl.Gate);
 private static Exception? Finalizer(Exception? __exception){Monitor.Exit(StormControl.Gate);return __exception;}
}
[HarmonyPatch(typeof(RaidWeatherService),nameof(RaidWeatherService.GetUpcomingWeather))]
internal static class ForecastSnapshot
{
 private static void Postfix(ref IEnumerable<SPTarkov.Server.Core.Models.Eft.Weather.Weather> __result)=>__result=__result.ToArray();
}
[HarmonyPatch(typeof(WeatherGenerator),nameof(WeatherGenerator.GenerateWeather))]
internal static class RainOnlyBudget
{
 private static void Prefix(WeatherGenerator __instance,Season currentSeason,ref Dictionary<WeatherPreset,double> presetWeights)
 {
  // SPT subtracts from the previous preset on each generated node. Refill a
  // rain-only budget before it reaches zero and falls back to a sunny preset.
  lock(StormControl.Gate)if(StormControl.Options.Enabled&&StormControl.Options.RainBoost==100&&currentSeason!=Season.WINTER)
   presetWeights=new(__instance.GetWeatherPresetWeightsBySeason(currentSeason));
 }
}
[Injectable(TypePriority=OnLoadOrder.PostLoad+50)]
public sealed class Mod(ISptLogger<Mod> logger,StormControl control):IOnLoad
{
 public Task OnLoadAsync(CancellationToken cancellationToken)
 {
  control.Initialize(Path.GetDirectoryName(typeof(Mod).Assembly.Location)!);new Harmony("com.killdg.returnofthunderstorms").PatchAll(typeof(Mod).Assembly);
  logger.Success("[Return Of Thunderstorms] 1.2.2 loaded; independent weather mod by killdg.");return Task.CompletedTask;
 }
}
