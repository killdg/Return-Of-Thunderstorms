using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using BepInEx;
using BepInEx.Configuration;
using Comfort.Common;
using EFT;
using EFT.Weather;
using HarmonyLib;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ReturnOfThunderstorms
{
    [DefaultExecutionOrder(10000)]
    [BepInPlugin("com.killdg.returnofthunderstorms", "Return Of Thunderstorms", "1.2.3")]
    public sealed class Plugin : BaseUnityPlugin
    {
        private static Plugin? instance;
        internal static bool StormActive;
        internal static float Strength;
        private ConfigEntry<bool> enabledSetting = null!;
        private ConfigEntry<bool> muffle = null!;
        private ConfigEntry<bool> hurricaneEnabled = null!;
        private ConfigEntry<bool> extraDrops = null!;
        private ConfigEntry<bool> lightFlicker = null!;
        private ConfigEntry<bool> strikeTest = null!;
        private readonly LightningLightFlicker lighting = new();
        private ConfigEntry<int> debrisLimit = null!;
        private readonly HurricaneDirector hurricane = new();
        private ConfigEntry<float> chance = null!;
        private ConfigEntry<float> boost = null!;
        private ConfigEntry<float> outdoorRain = null!;
        private ConfigEntry<float> indoorRain = null!;
        private ConfigEntry<float> outdoorSound = null!;
        private ConfigEntry<float> indoorSound = null!;
        private readonly Harmony harmony = new("com.killdg.returnofthunderstorms");
        private readonly StormAudioAssets assets = new();
        private StormtideRecordedRain rain = null!;
        private StormtideThunder thunder = null!;
        private readonly WorldAudioDucking ducking = new();
        private float nextAudioLog;
        private float lastFrame;
        private float nextHitchLog;
        private readonly RainEpisode episode = new();
        private GameWorld? world;
        private float nextDecision;
        private float nextScan;
        private float started;
        private bool wasActive;
        private AudioSource[] sources = Array.Empty<AudioSource>();
        private volatile bool syncing;
        private volatile string sent = "";
        private float retrySync;
        // ROT-prefixed messages go only to the local BepInEx log for diagnosis.
        internal static void Trace(string message) => instance?.Logger.LogInfo(message);
        private void Awake()
        {
            instance = this;
            AudioSettings.OnAudioConfigurationChanged += assets.AudioConfigurationChanged;
            enabledSetting = Config.Bind(
                "1. Settings",
                "Enabled",
                true,
                "Enable Return Of Thunderstorms. Turning this off immediately restores normal rain audio and stops its thunder effects."
            );
            chance = Config.Bind(
                "1. Settings",
                "Thunderstorm chance (%)",
                50f,
                new ConfigDescription(
                    "Chance that a new normal rain period becomes a thunderstorm. Rolled once per rain period; " +
                    "0 never replaces rain, 100 replaces every eligible rain period.",
                    new AcceptableValueRange<float>(0, 100)
                )
            );
            boost = Config.Bind(
                "1. Settings",
                "Rain chance boost (%)",
                0f,
                new ConfigDescription(
                    "Increase the native rainy-weather preset odds toward 100%. Zero preserves standard odds; " +
                    "100 selects rainy presets exclusively. Applies to future raids; winter snow is preserved. " +
                    "Requires this mod's server DLL.",
                    new AcceptableValueRange<float>(0, 100)
                )
            );
            muffle = Config.Bind(
                "1. Settings",
                "Rain muffles other game sounds",
                true,
                "During this mod's thunderstorms, gradually reduce other sounds to 25% outdoors; shelter " +
                "defaults to 60% gameplay and 40% recorded rain. Customize both indoor/outdoor levels in " +
                "Sound balance. Restores their normal levels when the storm ends or the mod is disabled."
            );
            hurricaneEnabled = Config.Bind(
                "2. Hurricane",
                "Hurricane effects",
                true,
                "Add gusting wind, cosmetic newspapers, leaves, cardboard and wind audio during this mod's " +
                "storms. No damage, loot or clock changes."
            );
            debrisLimit = Config.Bind(
                "2. Hurricane",
                "Flying debris limit",
                48,
                new ConfigDescription(
                    "Maximum pooled pieces near the player. Larger cardboard pieces are limited to six; debris " +
                    "stays outdoors even when viewed from inside. Lower for slower computers.",
                    new AcceptableValueRange<int>(12, 96)
                )
            );
            extraDrops = Config.Bind(
                "2. Hurricane",
                "Extra rain on eyes and goggles",
                true,
                "During hurricane weather, shorten native screen rain-drop intervals outdoors: up to 2.75 " +
                "times normal during gusts. Native eyewear behavior and indoor clearing are retained."
            );
            lightFlicker = Config.Bind(
                "3. Lightning lights",
                "Night lightning flicker",
                true,
                "Experimental: every lightning strike briefly flickers cached controllable map lights at " +
                "night (20:00ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â€šÂ¬Ã…Â¡Ãƒâ€šÃ‚Â¬ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬Ãƒâ€¦Ã¢â‚¬Å“06:00). Restores their prior brightness. Excludes sun/moon, player equipment " +
                "and baked lighting."
            );
            strikeTest = Config.Bind(
                "3. Lightning lights",
                "Play test lightning",
                false,
                "Action: enable once during an active storm to request another lightning/thunder strike. " +
                "Automatically resets. Map lights flicker only at night; playback waits for any current " +
                "thunder to finish."
            );
            strikeTest.Value = false;
            strikeTest.SettingChanged += (_, _) =>
            {
                if (!strikeTest.Value)
                {
                    return;
                }

                strikeTest.Value = false;
                if (StormActive)
                {
                    thunder.RequestTest();
                }
                else
                {
                    Trace("[ROT-LIGHTS] test requires an active outdoor storm");
                }
            };
            outdoorRain = Config.Bind(
                "4. Sound balance",
                "Outdoor rain volume (%)",
                70f,
                new ConfigDescription(
                    "Maximum recorded heavy-rain level outdoors. Builds toward this level with the storm; " +
                    "transitions smoothly at doorways. Thunder and wind have separate fixed levels.",
                    new AcceptableValueRange<float>(0, 100)
                )
            );
            indoorRain = Config.Bind(
                "4. Sound balance",
                "Indoor rain volume (%)",
                40f,
                new ConfigDescription(
                    "Maximum recorded heavy-rain level under shelter. Transitions smoothly from the outdoor setting.",
                    new AcceptableValueRange<float>(0, 100)
                )
            );
            outdoorSound = Config.Bind(
                "4. Sound balance",
                "Outdoor gameplay volume (%)",
                25f,
                new ConfigDescription(
                    "Gameplay sound level at full outdoor storm strength, relative to your normal volume. Used " +
                    "only when Rain muffles other game sounds is enabled.",
                    new AcceptableValueRange<float>(0, 100)
                )
            );
            indoorSound = Config.Bind(
                "4. Sound balance",
                "Indoor gameplay volume (%)",
                60f,
                new ConfigDescription(
                    "Gameplay sound level at full storm strength under shelter. Used only when Rain muffles other game sounds is enabled.",
                    new AcceptableValueRange<float>(0, 100)
                )
            );
            enabledSetting.SettingChanged += (_, _) => retrySync = 0;
            muffle.SettingChanged += (_, _) => Trace("[ROT-AUDIO] muffling setting=" + muffle.Value);
            boost.SettingChanged += (_, _) => retrySync = Time.unscaledTime + 0.4f;
            rain = new(assets);
            thunder = new(
                assets,
                () =>
                {
                    if (lightFlicker.Value)
                    {
                        lighting.Strike();
                    }
                });
            harmony.PatchAll(typeof(Plugin).Assembly);
            Logger.LogInfo("Return Of Thunderstorms 1.2.3 by killdg. Settings use SPT's standard F1 configuration panel.");
        }

        private void Update()
        {
            float frameNow = Time.realtimeSinceStartup;
            if (lastFrame > 0 && frameNow - lastFrame > 0.75f && frameNow >= nextHitchLog)
            {
                nextHitchLog = frameNow + 10;
                var observedGame = Singleton<AbstractGame>.Instantiated ? Singleton<AbstractGame>.Instance : null;
                Trace($"[ROT-PERF] update-gap={frameNow - lastFrame:0.000}s gameStatus={observedGame?.Status} storm={StormActive}");
            }

            lastFrame = frameNow;
            Sync();
            try
            {
                Tick();
            }
            catch (Exception e)
            {
                Stop();
                if (Time.unscaledTime >= nextDecision)
                {
                    Logger.LogWarning(e.GetBaseException().Message);
                    nextDecision = Time.unscaledTime + 10;
                }
            }
        }

        // A rain episode rolls once when natural rain begins. Indoor-only maps,
        // winter precipitation and native debug weather bypass the replacement.
        // The mod changes presentation; it never accelerates the raid clock.
        private void Tick()
        {
            var current = Singleton<GameWorld>.Instantiated ? Singleton<GameWorld>.Instance : null;
            if (current is HideoutGameWorld)
            {
                current = null;
            }

            if (!ReferenceEquals(world, current))
            {
                lighting.Dispose();
                hurricane.Dispose();
                Stop();
                world = current;
                episode.Reset();
                nextScan = 0;
                nextDecision = 0;
            }

            var game = Singleton<AbstractGame>.Instantiated ? Singleton<AbstractGame>.Instance : null;
            var weather = WeatherController.Instance;
            bool eligible = enabledSetting.Value
                && current != null
                && current.MainPlayer?.HealthController?.IsAlive == true
                && game?.GameTimer?.StartDateTime != null
                && game.Status == GameStatus.Started
                && weather != null
                && weather.RainController != null
                && weather.RainController.enabled
                && weather.RainController.gameObject.activeInHierarchy
                && weather.RainController.State is not RainController.AbstractStateWinter
                && StormPolicy.Outdoor(current.LocationId)
                && !weather.WeatherDebug.Enabled;
            if (!eligible)
            {
                episode.Reset();
                Stop();
                return;
            }

            assets.Tick();
            if (Time.unscaledTime >= nextDecision)
            {
                nextDecision = Time.unscaledTime + 0.5f;
                StormActive = episode.Tick(Time.unscaledTime, true, weather!.WeatherCurve.Rain >= 0.03f, chance.Value, UnityEngine.Random.value);
                if (StormActive && !wasActive)
                {
                    started = Time.unscaledTime;
                    Trace("[ROT] Rain selected for a thunderstorm; chosen raid time is retained.");
                }

                if (!StormActive && wasActive)
                {
                    Stop();
                }

                wasActive = StormActive;
            }

            if (!StormActive)
            {
                return;
            }

            Strength = (float)StormAudioPolicy.StormBuild(Time.unscaledTime - started);
            if (lightFlicker.Value)
            {
                lighting.Discover();
            }
            else
            {
                lighting.Stop();
            }

            rain.Tick(Strength, 1, indoorRain.Value / 100f, outdoorRain.Value / 100f);
            thunder.Tick(Strength, Time.unscaledTime - started);
            if (hurricaneEnabled.Value)
            {
                hurricane.Tick(Strength, Time.unscaledTime - started, rain.Exposure, debrisLimit.Value, extraDrops.Value);
            }
            else
            {
                hurricane.Stop();
            }

            StormtideSound.Active = true;
            StormtideSound.Gain = 1;
            StormtideSound.RainGain = (float)StormAudioPolicy.NativeRain(rain.Blend, rain.Playing);
            if (Time.unscaledTime >= nextScan)
            {
                nextScan = Time.unscaledTime + 5;
                sources = Object.FindObjectsOfType<AudioSource>();
            }
        }

        private void LateUpdate()
        {
            if (!StormActive)
            {
                return;
            }

            if (lightFlicker.Value)
            {
                lighting.LateTick();
            }
            else
            {
                lighting.Stop();
            }

            ducking.Tick(muffle.Value && rain.Playing ? (float)StormAudioPolicy.WorldGain(Strength, rain.Exposure, indoorSound.Value / 100f, outdoorSound.Value / 100f) : 1);
            foreach (var source in sources)
            {
                StormtideSound.Apply(source);
            }

            if (Time.unscaledTime >= nextAudioLog)
            {
                nextAudioLog = Time.unscaledTime + 20;
                float masterDb = 0;
                float worldDb = 0;
                var audio = Singleton<BetterAudio>.Instantiated ? Singleton<BetterAudio>.Instance.Master : null;
                if (audio != null)
                {
                    audio.GetFloat("MasterVolume", out masterDb);
                    audio.GetFloat(AudioGainPolicy.WorldParameter, out worldDb);
                }

                Trace($"[ROT-AUDIO] muffle={muffle.Value} worldDb={worldDb:0.0} masterDb={masterDb:0.0} listener={AudioListener.volume:0.00} paused={AudioListener.pause} {rain.Diagnostics}");
            }
        }

        // Every exit path restores the native mixer, light brightness and weather
        // presentation. Turning the mod off must not leave modified audio behind.
        private void Stop()
        {
            lighting.Stop();
            hurricane.Stop();
            ducking.Stop();
            wasActive = false;
            StormActive = false;
            Strength = 0;
            rain?.Stop();
            thunder?.Stop();
            StormtideSound.Stop();
            sources = Array.Empty<AudioSource>();
        }

        // Synchronize only Enabled and RainBoost with the user's configured SPT
        // server. The relative route below is registered by SettingsRouter.
        // No external host, account identifier, inventory or telemetry is sent.
        private void Sync()
        {
            string request = JsonConvert.SerializeObject(new StormSettings { Enabled = enabledSetting.Value, RainBoost = boost.Value });
            if (syncing || request == sent || Time.unscaledTime < retrySync)
            {
                return;
            }

            syncing = true;
            Task.Run(async () =>
            {
                try
                {
                    // Resolve SPT's existing HTTP helper at runtime instead of shipping
                    // a second HTTP client or adding a separate SPT.Common DLL.
                    var type = AppDomain.CurrentDomain.GetAssemblies()
                        .Select(assembly => assembly.GetType("SPT.Common.Http.RequestHandler", false))
                        .FirstOrDefault(requestHandler => requestHandler != null)
                        ?? throw new InvalidOperationException("SPT HTTP client is not ready.");
                    var method = type.GetMethod(
                        "PostJsonAsync",
                        BindingFlags.Public | BindingFlags.Static,
                        null,
                        new[] { typeof(string), typeof(string) },
                        null
                    ) ?? throw new MissingMethodException("SPT PostJsonAsync not found.");
                    var response = await (Task<string>)method.Invoke(null, new object[] { "/client/returnofthunderstorms/settings", request })!;
                    var result = Newtonsoft.Json.Linq.JObject.Parse(response);
                    if (result["data"] is Newtonsoft.Json.Linq.JObject data)
                    {
                        result = data;
                    }

                    if (result["ok"]?.Value<bool>() != true)
                    {
                        throw new InvalidOperationException("Install the matching Return Of Thunderstorms server DLL for rain-frequency settings.");
                    }

                    sent = request;
                }
                catch (Exception e)
                {
                    Trace("[ROT] Weather settings sync: " + e.GetBaseException().Message);
                }
                finally
                {
                    syncing = false;
                }
            });
            retrySync = Time.unscaledTime + 15;
        }

        private void OnDestroy()
        {
            AudioSettings.OnAudioConfigurationChanged -= assets.AudioConfigurationChanged;
            Stop();
            lighting.Dispose();
            hurricane.Dispose();
            assets.Dispose();
            harmony.UnpatchSelf();
            instance = null;
        }
    }

    // Patch the concrete native rain-control method rather than an inlinable getter.
    // The backend weather curve remains unchanged, allowing natural rain to end.
    [HarmonyPatch(typeof(WeatherController), nameof(WeatherController.ControlRain))]
    internal static class HeavyRain
    {
        private static bool Prefix(WeatherController __instance)
        {
            if (!Plugin.StormActive || __instance.WeatherDebug.Enabled || __instance.RainController == null)
            {
                return true;
            }

            var curve = __instance.WeatherCurve;
            Vector2 wind = (HurricaneDirector.Active ? HurricaneDirector.Wind : curve.Wind) * __instance.WindController.RainWindMultiplier;
            var direction = new Vector3(wind.x, -25, wind.y);
            __instance.RainController.ControlRain(
                in direction,
                Mathf.Lerp(curve.Rain, 1, Plugin.Strength),
                Mathf.Max(__instance.MinLyingWater, RainController.Wetting),
                Mathf.Max(curve.Cloudiness, 0.7f),
                TODSkyProvider.Instance.Cycle.Hour
            );
            return false;
        }
    }
}
