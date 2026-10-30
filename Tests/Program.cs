using ReturnOfThunderstorms;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Generators.Weather;
using SPTarkov.Server.Core.Services.InRaid;
using System.Reflection;

int checks = 0;
void Check(bool good, string label)
{
    if (!good)
    {
        throw new Exception(label);
    }

    checks++;
    Console.WriteLine("PASS " + label);
}

var original = new Dictionary<WeatherPreset, double>
{
    {
        WeatherPreset.SUNNY,
        13
    },
    {
        WeatherPreset.RAINY,
        7
    },
    {
        WeatherPreset.CLOUDY,
        7
    }
};
Check(
    ReferenceEquals(original, RainWeights.Apply(original, new() { RainBoost = 0 }, false)),
    "Zero boost preserves the native weights without mutation"
);
Check(
    ReferenceEquals(original, RainWeights.Apply(original, new() { Enabled = false, RainBoost = 100 }, false)),
    "Disabled mod leaves native weather alone"
);
Check(
    ReferenceEquals(original, RainWeights.Apply(original, new() { RainBoost = 100 }, true)),
    "Winter snow is preserved"
);
var boosted = RainWeights.Apply(original, new() { RainBoost = 50 }, false);
Check(
    Math.Abs(boosted[WeatherPreset.RAINY] / boosted.Values.Sum() - (7d / 27 + (1 - 7d / 27) * 0.5)) < 1e-9,
    "Rain boost increases native rainy preset probability proportionally"
);
Check(
    Math.Abs(boosted[WeatherPreset.SUNNY] / boosted[WeatherPreset.CLOUDY] - 13d / 7) < 1e-9 && Math.Abs(boosted.Values.Sum() - 27) < 1e-9,
    "Other preset ratios and total weight are retained"
);
var rainy = RainWeights.Apply(original, new() { RainBoost = 100 }, false);
Check(
    rainy[WeatherPreset.SUNNY] == 0 && rainy[WeatherPreset.CLOUDY] == 0 && rainy[WeatherPreset.RAINY] > 0,
    "Maximum rain boost selects rainy presets only"
);
Check(
    original[WeatherPreset.RAINY] == 7 && original[WeatherPreset.SUNNY] == 13,
    "Applying boost never edits the native configuration dictionary"
);
Check(
    !StormSettings.Valid(new() { RainBoost = float.NaN }) && !StormSettings.Valid(new() { RainBoost = -1 }) && !StormSettings.Valid(new() { RainBoost = 101 }),
    "Malformed frequency settings are rejected"
);
Check(
    !StormPolicy.Select(0, 0) && StormPolicy.Select(100, 0.999999) && !StormPolicy.Select(50, 0.5),
    "Storm replacement percentage boundaries are exact"
);
var episode = new RainEpisode();
Check(
    !episode.Tick(0, true, false, 100, 0),
    "Dry weather never becomes a storm merely from the replacement setting"
);
Check(!episode.Tick(1, true, true, 0, 0), "Zero storm chance leaves native rain intact");
Check(!episode.Tick(2, true, true, 100, 0), "Changing chance during a rain episode does not reroll repeatedly");
episode.Tick(3, true, false, 100, 0);
episode.Tick(34, true, false, 100, 0);
Check(episode.Tick(35, true, true, 100, 0.9), "A later rain episode makes a fresh replacement roll");
Check(
    episode.Tick(36, true, false, 100, 0.9) && episode.Tick(37, true, true, 0, 0.9),
    "Brief weather-curve gaps retain the chosen storm"
);
Check(
    !episode.Tick(38, false, true, 100, 0) && !episode.Observed,
    "Disable immediately ends and resets the storm"
);
Check(
    !StormPolicy.Outdoor("laboratory") && !StormPolicy.Outdoor("factory4_day") && StormPolicy.Outdoor("TarkovStreets") && StormPolicy.Outdoor("Sandbox"),
    "Rain replacement is restricted to outdoor maps"
);
var schedule = new ThunderSchedule();
Check(
    !schedule.Tick(29.9, true, 0) && schedule.Tick(30, true, 0.99),
    "Selected storms receive their first thunder strike after thirty seconds"
);
Check(
    !schedule.Tick(31, true, 0) && !schedule.Tick(210, true, 0.5) && schedule.Tick(390, true, 0.1),
    "Later strikes use three-minute intervals and a thirty-three percent roll"
);
for (int last = 0; last < 3; last++)
{
    for (int n = 0; n <= 100; n++)
    {
        Check(StormAudioPolicy.ChooseThunder(last, 3, n / 100d) != last, "Three-clip rotation avoids an immediate repeat");
    }
}

var audio = System.IO.Path.GetFullPath(args[0]);
foreach (var name in new[]
{
    "Thunder1.wav",
    "ThunderReplacement.wav",
    "Thunder3.wav",
    "HeavyRain.wav"
})
{
    var wave = WavePcm.Read(System.IO.Path.Combine(audio, name));
    Check(
        wave.Samples.Length > 0 && wave.Samples.All(v => float.IsFinite(v) && v >= -1 && v <= 1),
        "Valid bundled PCM audio: " + name
    );
}

Check(
    StormAudioPolicy.ShelterGain(0) == 0.40 && StormAudioPolicy.ShelterCutoff(0) == 6000,
    "Recorded rain is reduced and low-pass filtered under shelter"
);
Check(
    typeof(WeatherGenerator).GetMethod("GetWeatherPresetWeightsBySeason", [typeof(SPTarkov.Server.Core.Models.Enums.Season)]) != null && typeof(RaidWeatherService).GetField("WeatherForecast", BindingFlags.NonPublic | BindingFlags.Instance) != null,
    "Native SPT 4.1.6 weather hook and forecast cache exist"
);
Check(
    new ModMetadata().ModDependencies == null && new ModMetadata().Author == "killdg",
    "Standalone server has no Raid Roulette or third-party mod dependency"
);
Check(
    Math.Abs(AudioGainPolicy.ApplyDb(0, 0.25f) + 12.0412) < 0.001,
    "Quarter-volume world attenuation uses the correct decibel gain"
);
Check(
    AudioGainPolicy.ApplyDb(-6, 1) == -6 && AudioGainPolicy.ApplyDb(-80, 0.25f) == -80,
    "Muffling respects existing world levels and never raises a muted bus"
);
Check(AudioGainPolicy.ApplyDb(-90, 0.25f) == -90, "An already silent native bus is never raised");
Check(
    AudioGainPolicy.RainLabel("HeavyRain") && AudioGainPolicy.RainLabel("env_rain_loop") && !AudioGainPolicy.RainLabel("TRAIN_ENGINE") && !AudioGainPolicy.RainLabel("drain_pipe"),
    "Native rain matching does not accidentally silence train or drain sounds"
);
Check(
    StormAudioPolicy.NativeRain(1, false) == 1 && StormAudioPolicy.NativeRain(0.5, false) == 1,
    "Native rain remains audible whenever replacement playback is stopped"
);
Check(
    Math.Abs(StormAudioPolicy.NativeRain(1, true)) < 0.0001,
    "Confirmed replacement playback permits the completed native crossfade"
);
Check(
    Math.Abs(StormAudioPolicy.WorldGain(1, 0) - 0.6) < 0.00001 && Math.Abs(StormAudioPolicy.WorldGain(1, 1) - 0.25) < 0.00001,
    "Indoor gameplay restores to 60 percent and outdoor gameplay returns to 25 percent"
);
Check(
    StormAudioPolicy.WorldGain(0, 0) == 1 && StormAudioPolicy.WorldGain(0, 1) == 1,
    "No attenuation before storm buildup"
);
double doorway = 1;
for (int i = 0; i < 13; i++)
{
    doorway = StormAudioPolicy.ShelterStep(doorway, false, 0.25);
}

Check(
    Math.Abs(doorway) < 0.00001 && StormAudioPolicy.ShelterGain(doorway) == 0.4,
    "Entering shelter reaches forty percent rain smoothly"
);
for (int i = 0; i < 13; i++)
{
    doorway = StormAudioPolicy.ShelterStep(doorway, true, 0.25);
}

Check(
    doorway == 1 && StormAudioPolicy.ShelterGain(doorway) == 1 && Math.Abs(StormAudioPolicy.WorldGain(1, doorway) - 0.25) < 0.00001,
    "Returning outdoors restores the previous full rain and muffling"
);
Check(
    StormAudioPolicy.StormBuild(0) == 0 && StormAudioPolicy.StormBuild(45) == 1 && StormAudioPolicy.StormBuild(90) == 1,
    "Storm reaches full strength over 45 seconds"
);
Check(
    StormAudioPolicy.StormBuild(5) < 0.04 && StormAudioPolicy.StormBuild(22.5) == 0.5,
    "Storm fade starts gently and reaches half strength at midpoint"
);
Check(
    HurricanePolicy.Gust(0) == 0 && HurricanePolicy.Gust(8) == 0 && Math.Abs(HurricanePolicy.Gust(16) - 1) < 0.00001 && HurricanePolicy.Gust(24) == 0,
    "Hurricane gusts have a quiet interval and a smooth peak"
);
Check(
    Math.Abs(HurricanePolicy.Gust(16) - HurricanePolicy.Gust(48)) < 0.00001,
    "Gust timing is stable across cycles"
);
Check(
    HurricanePolicy.Wind(1, 1) <= 0.98 && HurricanePolicy.Wind(1, 0) < HurricanePolicy.Wind(1, 1),
    "Wind stays below native saturation and rises in gusts"
);
Check(
    HurricanePolicy.DropRate(0, 1) == 1 && HurricanePolicy.DropRate(1, 0) == 2 && HurricanePolicy.DropRate(1, 1) == 2.75,
    "Native drop rate scales from normal to 2.75 times normal"
);
Check(
    HurricanePolicy.Limit(0) == 12 && HurricanePolicy.Limit(999) == 96 && HurricanePolicy.MaxLarge == 6 && HurricanePolicy.RayBudget == 4,
    "Debris counts and collision queries have hard budgets"
);
Check(
    HurricanePolicy.Fade(0, 10) == 0 && HurricanePolicy.Fade(5, 10) == 1 && HurricanePolicy.Fade(10, 10) == 0,
    "Cosmetic debris fades at birth and expiry"
);
Check(
    HurricanePolicy.DistanceFade(30) == 1 && HurricanePolicy.DistanceFade(100) == 1 && HurricanePolicy.DistanceFade(125) == 0.5 && HurricanePolicy.DistanceFade(150) == 0,
    "Debris fades only at the distant field boundary"
);
Check(
    HurricanePolicy.Fade(1.5, 30) == 0.5 && HurricanePolicy.Fade(28.5, 30) == 0.5,
    "Debris uses a gradual three-second appearance and retirement"
);
Check(
    HurricanePolicy.DebrisHeight(12, 0) == 13.4 && HurricanePolicy.DebrisHeight(12, 1) == 20,
    "Debris elevation is based only on sampled ground and bounded scatter"
);
Check(
    LightFlickerPolicy.Night(20) && LightFlickerPolicy.Night(23.9) && LightFlickerPolicy.Night(0) && LightFlickerPolicy.Night(5.9) && !LightFlickerPolicy.Night(6) && !LightFlickerPolicy.Night(12),
    "Electrical flicker is restricted to night hours"
);
Check(
    LightFlickerPolicy.Gain(-1, 0) == 1 && LightFlickerPolicy.Gain(LightFlickerPolicy.Duration, 0.12) == 1,
    "Lights return to full native gain after every staggered pulse"
);
Check(
    Enumerable.Range(0, 150).All(i => LightFlickerPolicy.Gain(i / 100d, 0.12) >= 0 && LightFlickerPolicy.Gain(i / 100d, 0.12) <= 1),
    "Flicker only dims lights and never brightens or exceeds native levels"
);
Check(
    LightFlickerPolicy.Gain(0.03, 0) < 0.1 && LightFlickerPolicy.Gain(0.03, 0.12) == 1,
    "Electrical pulses are staggered across fixtures"
);
Check(
    StormAudioPolicy.ShelterGain(0, 0.2, 0.8) == 0.2 && StormAudioPolicy.ShelterGain(1, 0.2, 0.8) == 0.8,
    "Rain sliders define independent indoor and outdoor endpoints"
);
Check(
    Math.Abs(StormAudioPolicy.ShelterGain(0.5, 0.2, 0.8) - 0.5) < 0.00001,
    "Doorway fade blends user-selected rain levels"
);
Check(
    StormAudioPolicy.WorldGain(1, 0, 1, 0) == 1 && StormAudioPolicy.WorldGain(1, 1, 1, 0) == 0 && StormAudioPolicy.WorldGain(0, 1, 1, 0) == 1,
    "Gameplay sliders support full restoration, complete muffling and buildup from native volume"
);
Check(
    StormAudioPolicy.ShelterGain(0, -1, 2) == 0 && StormAudioPolicy.ShelterGain(1, -1, 2) == 1,
    "Sound gains stay within zero to full native volume"
);
Check(
    HurricanePolicy.NightDebris(21) && HurricanePolicy.NightDebris(3) && !HurricanePolicy.NightDebris(6) && !HurricanePolicy.NightDebris(15),
    "Flying debris is night-only while the hurricane wind remains independent"
);
// Frequency controls must reduce rain as well as increase it, without mutating
// native preset weights or changing the ratios of the remaining dry presets.
var lessRain = RainWeights.Apply(original, new() { Frequency = RainFrequency.LessRain }, false);
Check(Math.Abs(lessRain[WeatherPreset.RAINY] / lessRain.Values.Sum() - 7d / 54) < 1e-9,
    "Less rain halves the native rainy forecast probability");
Check(Math.Abs(lessRain[WeatherPreset.SUNNY] / lessRain[WeatherPreset.CLOUDY] - 13d / 7) < 1e-9,
    "Less rain preserves the relative dry weather weights");
var noRain = RainWeights.Apply(original, new() { Frequency = RainFrequency.Never }, false);
Check(noRain[WeatherPreset.RAINY] == 0 && Math.Abs(noRain.Values.Sum() - 27) < 1e-9,
    "Never removes rain and retains the forecast weight budget");
Check(ReferenceEquals(original, RainWeights.Apply(original,
    new() { Frequency = RainFrequency.TarkovDefault, RainBoost = 100 }, false)),
    "Tarkov default ignores a retained legacy rain boost");
var alwaysRain = RainWeights.Apply(original, new() { Frequency = RainFrequency.AlwaysRain }, false);
Check(alwaysRain[WeatherPreset.SUNNY] == 0 && alwaysRain[WeatherPreset.CLOUDY] == 0
    && alwaysRain[WeatherPreset.RAINY] == 27, "Always rain assigns the full forecast budget to rain");
Check(ReferenceEquals(original, RainWeights.Apply(original,
    new() { Frequency = RainFrequency.Never }, true)), "Frequency controls preserve native winter weather");
var allRainNative = new Dictionary<WeatherPreset, double> { { WeatherPreset.RAINY, 10 } };
var dryFallback = RainWeights.Apply(allRainNative, new() { Frequency = RainFrequency.Never }, false);
Check(dryFallback[WeatherPreset.RAINY] == 0 && dryFallback[WeatherPreset.SUNNY] == 10,
    "Never rain has a dry fallback when native weights contain only rain");
Check(!StormSettings.Valid(new() { Frequency = (RainFrequency)99 }),
    "Unknown rain-frequency enum values are rejected by the server contract");
Check(WeatherPreferences.RainOnly(new() { Frequency = RainFrequency.AlwaysRain })
    && !WeatherPreferences.RainOnly(new() { Frequency = RainFrequency.Never, RainBoost = 100 })
    && !WeatherPreferences.RainOnly(new() { Enabled = false, Frequency = RainFrequency.AlwaysRain }),
    "Rain-only budget refresh honors the selected mode and master switch");
Check(WeatherPreferences.ThunderstormChance(true, 30, 15) == 30
    && WeatherPreferences.ThunderstormChance(false, 30, 15) == 15,
    "Summer and other seasons select their independent thunderstorm chances");
Check(WeatherPreferences.FrequencyFor(WeatherPresetChoice.OccasionalStorms) == RainFrequency.LessRain
    && WeatherPreferences.SummerChanceFor(WeatherPresetChoice.OccasionalStorms) == 30
    && WeatherPreferences.OtherChanceFor(WeatherPresetChoice.OccasionalStorms) == 15,
    "Occasional storms uses reduced rain and thirty/fifteen percent thunder chances");
Check(WeatherPreferences.FrequencyFor(WeatherPresetChoice.VanillaRainWithThunderstorms) == RainFrequency.TarkovDefault
    && WeatherPreferences.FrequencyFor(WeatherPresetChoice.FrequentStorms) == RainFrequency.MoreRain
    && WeatherPreferences.SummerChanceFor(WeatherPresetChoice.FrequentStorms) == 75
    && WeatherPreferences.OtherChanceFor(WeatherPresetChoice.FrequentStorms) == 50,
    "Vanilla and frequent weather presets have distinct forecast policies");
Check(Math.Abs(WeatherPreferences.RainProbability(.2, RainFrequency.MoreRain, 25) - .4) < 1e-9,
    "Migration can preserve an older custom twenty-five percent rain boost");
Console.WriteLine("Return Of Thunderstorms checks passed: " + checks);
