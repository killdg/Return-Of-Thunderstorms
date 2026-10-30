using Mono.Cecil;
using Mono.Cecil.Cil;
using var client=AssemblyDefinition.ReadAssembly(args[0]);
using var game=AssemblyDefinition.ReadAssembly(args[1]);
using var rr=AssemblyDefinition.ReadAssembly(args[2]);
int checks=0;void Check(bool ok,string label){if(!ok)throw new Exception(label);checks++;Console.WriteLine("PASS "+label);}
var plugin=client.MainModule.Types.Single(t=>t.Name=="Plugin");
Check(plugin.CustomAttributes.Single(a=>a.AttributeType.Name=="BepInPlugin").ConstructorArguments[1].Value.ToString()=="Return Of Thunderstorms","Standalone plugin appears under its own F1 title");
Check(plugin.Fields.Count(f=>f.FieldType is GenericInstanceType type&&type.ElementType.Name=="ConfigEntry`1")==13,"Weather, hurricane, flicker and four sound-balance sliders are exposed");
Check(!plugin.CustomAttributes.Any(a=>a.AttributeType.Name=="BepInDependency")&&!client.MainModule.AssemblyReferences.Any(a=>a.Name.StartsWith("RaidRoulette")||a.Name.StartsWith("Fika")),"Standalone client has no Raid Roulette, task-pack or Fika dependency");
var weather=game.MainModule.Types.Single(t=>t.FullName=="EFT.Weather.WeatherController");
Check(weather.Methods.Any(m=>m.Name=="ControlRain"&&m.Parameters.Count==0)&&weather.Fields.Any(f=>f.Name=="RainController"),"Heavy-rain hook matches the concrete native weather method");
foreach(string type in new[]{"BetterSource","SuperSourceDistant"})Check(game.MainModule.Types.Single(t=>t.Name==type).Methods.Any(m=>m.Name=="UpdateSourceVolume"&&m.Parameters.Count==1&&m.Parameters[0].ParameterType.FullName=="System.Single"),"Native volume-update hook exists: "+type);
Check(client.MainModule.Types.SelectMany(t=>t.Methods).Where(m=>m.HasBody).SelectMany(m=>m.Body.Instructions).All(i=>i.Operand is not MethodReference method||method.Name is not ("set_TimeFactor" or "set_TimeFactorMod" or "ResetForce")),"Standalone storm effects never modify the raid clock");
foreach(var assembly in new[]{client,rr})
{
 var assets=assembly.MainModule.Types.Single(t=>t.Name=="StormAudioAssets");var names=assets.Methods.Where(m=>m.HasBody).SelectMany(m=>m.Body.Instructions).Select(i=>i.Operand).OfType<string>().ToArray();
 Check(new[]{"Thunder1.wav","ThunderReplacement.wav","Thunder3.wav","HeavyRain.wav"}.All(names.Contains),assembly.Name.Name+" includes the three thunder clips and heavy rain");
 Check(assets.Methods.Single(m=>m.Name=="get_Rain").Body.Instructions.Any(i=>i.OpCode.Code==Code.Ldc_I4_3),assembly.Name.Name+" keeps heavy rain separate from the thunder rotation");
}
var output=client.MainModule.Types.Single(t=>t.Name=="WeatherAudioOutput");
Check(output.Methods.Single(m=>m.Name=="Attach").Body.Instructions.Any(i=>i.Operand is FieldReference f&&f.Name=="MasterMixerGroup"),"Weather output explicitly uses the native Master mixer group");
foreach(string name in new[]{"StormtideRecordedRain","StormtideThunder"})
 Check(client.MainModule.Types.Single(t=>t.Name==name).Methods.Where(m=>m.HasBody).SelectMany(m=>m.Body.Instructions).All(i=>i.Operand is not MethodReference method||method.Name!="GetSource"),"Weather does not inherit a pooled output route: "+name);
var late=plugin.Methods.Single(m=>m.Name=="LateUpdate");Check(late.Body.Instructions.Any(i=>i.Operand is MethodReference method&&method.DeclaringType.Name=="WorldAudioDucking"),"World mixer attenuation is applied during LateUpdate");
Check(game.MainModule.Types.Single(t=>t.Name=="BetterAudio").Fields.Any(f=>f.Name=="MasterMixerGroup"),"Explicit weather output field exists in the installed game");
Check(weather.Methods.Any(m=>m.Name=="ControlWind"&&m.Parameters.Count==0)&&weather.Methods.Any(m=>m.Name=="SetCloudWind"&&m.Parameters.Any(p=>p.Name=="layerASpeed")),"Native wind and cloud hooks match the installed game");
var placer=game.MainModule.Types.Single(t=>t.Name=="RainScreenDropsPlacer");
Check(placer.Fields.Any(f=>f.Name=="_rainDropsDelay"&&f.IsPublic)&&placer.Methods.Any(m=>m.Name=="Update"),"Native screen rain exposes a scoped spawn-delay hook");
var hurricane=client.MainModule.Types.Single(t=>t.Name=="HurricaneDirector");
Check(hurricane.Methods.Where(m=>m.HasBody).SelectMany(m=>m.Body.Instructions).All(i=>i.Operand is not MethodReference method||!method.FullName.Contains("Rigidbody")&&!method.FullName.Contains("AddForce")),"Hurricane debris never applies physical forces");
Check(hurricane.Methods.Where(m=>m.HasBody).SelectMany(m=>m.Body.Instructions).All(i=>i.Operand is not GenericInstanceMethod method||method.GenericArguments.All(t=>!t.Name.Contains("Collider")&&t.Name!="Rigidbody")),"Visual pool creates no damaging colliders or rigid bodies");
Check(client.MainModule.Types.Single(t=>t.Name=="HurricaneVisorRain").Methods.Any(m=>m.Name=="Finalizer"),"Native drop delay is restored even if its Update throws");
foreach(string name in new[]{"CullingLightObject","CullingAdvancedLightObject"})Check(game.MainModule.Types.Single(t=>t.Name==name).Methods.Any(m=>m.Name=="UpdateIntensity"),"Native light culling refresh exists: "+name);
Check(game.MainModule.Types.Single(t=>t.Name=="CullingAdvancedLightObject").Fields.Any(f=>f.Name=="_intensityMultiplier"),"Native area-light multiplier field exists");
var lighting=client.MainModule.Types.Single(t=>t.Name=="LightningLightFlicker");
Check(lighting.Methods.Where(m=>m.HasBody).SelectMany(m=>m.Body.Instructions).All(i=>i.Operand is not MethodReference method||method.Name is not ("TurnOn" or "TurnOff" or "TurnLights" or "SetLightState" or "set_LampState")),"Flicker never invokes map power, switch or destruction transitions");
Check(lighting.Methods.Single(m=>m.Name=="Inspect").Body.Instructions.Any(i=>i.Operand is GenericInstanceMethod method&&method.GenericArguments.Any(t=>t.Name=="LampController")),"Discovery includes native lamp emission controllers");
Console.WriteLine("Return Of Thunderstorms client checks passed: "+checks);
