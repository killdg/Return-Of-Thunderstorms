using System;
using System.IO;
using System.Linq;
using BepInEx.Configuration;
using UnityEngine;

namespace ReturnOfThunderstorms
{
    // SPT's bundled Configuration Manager recognizes this exact attribute class
    // name and its public fields. There is no extra configuration-menu dependency.
    internal sealed class ConfigurationManagerAttributes
    {
        public bool? Browsable;
        public int Order;
        public bool? HideDefaultButton;
        public bool? HideSettingName;
        public Action<ConfigEntryBase>? CustomDrawer;
    }

    public sealed partial class Plugin
    {
        private ConfigEntry<WeatherPresetChoice> preset = null!;
        private ConfigEntry<RainFrequency> frequency = null!;
        private ConfigEntry<float> summerChance = null!;
        private ConfigEntry<float> otherChance = null!;
        private bool applyingPreset;
        private volatile bool serverSummer;
        private volatile bool seasonKnown;
        private volatile string syncStatus = "Connecting weather settings to your SPT server...";

        private static ConfigurationManagerAttributes Hidden() =>
            new ConfigurationManagerAttributes { Browsable = false };

        private ConfigEntry<float> Percentage(string section, string name, float value, string description, bool hidden = false)
        {
            return Config.Bind(section, name, value, new ConfigDescription(
                description, new AcceptableValueRange<float>(0, 100),
                hidden ? Hidden() : new ConfigurationManagerAttributes
                {
                    Order = name.StartsWith("Summer", StringComparison.Ordinal) ? 1 : 2
                }));
        }

        private T Legacy<T>(string section, string key, T defaultValue)
        {
            return Config.Bind(section, key, defaultValue,
                new ConfigDescription("Retained only for migration from the old menu.", null, Hidden())).Value;
        }

        private void ConfigureMenu()
        {
            // Read only our own existing BepInEx file, before binding new entries.
            // This distinguishes upgrades from a fresh install without touching profiles.
            bool oldMenu = File.Exists(Config.ConfigFilePath)
                && File.ReadLines(Config.ConfigFilePath)
                    .Any(line => line.TrimStart().StartsWith("Rain chance boost (%) =", StringComparison.Ordinal));
            var schema = Config.Bind("Internal", "Settings schema", 0,
                new ConfigDescription("Menu migration version; not a gameplay control.", null, Hidden()));

            enabledSetting = Config.Bind("1. Settings", "Enabled", true,
                "Press F1 to open these settings. Off stops this mod's effects and restores normal audio.");
            preset = Config.Bind("1. Settings", "Weather preset", WeatherPresetChoice.OccasionalStorms,
                "Choose a preset for one-click setup. It fills in weather, effects and sound levels. Editing these switches to Custom.");
            frequency = Config.Bind("2. Weather", "Rain frequency", RainFrequency.LessRain,
                "Future forecasts: Never removes rain; Less halves native rain odds; Tarkov default keeps native odds; " +
                "More raises them; Always selects rain. Weather can persist across several raids. Winter snow is unchanged.");
            summerChance = Percentage("2. Weather", "Summer thunderstorm chance (%)", 30,
                "When it rains in summer, chance this rain period becomes a thunderstorm. This does not make rain more frequent.");
            otherChance = Percentage("2. Weather", "Other seasons thunderstorm chance (%)", 15,
                "When it rains outside summer, chance this rain period becomes a thunderstorm. Native winter snow is preserved.");
            muffle = Config.Bind("3. Storm effects", "Muffle other sounds during storms", true,
                "Reduce gameplay sound during thunderstorms; Advanced settings controls indoor/outdoor levels.");
            hurricaneEnabled = Config.Bind("3. Storm effects", "Hurricane effects", false,
                "Add gusting wind and night-only flying debris. Off keeps ordinary thunderstorm rain and lightning.");
            lightFlicker = Config.Bind("3. Storm effects", "Lights flicker during night lightning", true,
                "Briefly flicker controllable map lights during night strikes; restore their prior brightness afterward.");
            outdoorRain = Percentage("4. Sound balance", "Outdoor rain volume (%)", 70,
                "Maximum heavy-rain recording volume outdoors. New installs default to 70%; doorway transitions are smooth.");
            indoorRain = Percentage("4. Sound balance", "Indoor rain volume (%)", 40,
                "Heavy-rain level under shelter.", true);
            outdoorSound = Percentage("4. Sound balance", "Outdoor gameplay volume (%)", 25,
                "Gameplay level outdoors when storm muffling is enabled.", true);
            indoorSound = Percentage("4. Sound balance", "Indoor gameplay volume (%)", 60,
                "Gameplay level indoors when storm muffling is enabled.", true);
            extraDrops = Config.Bind("2. Hurricane", "Extra rain on eyes and goggles", true,
                new ConfigDescription("Extra outdoor screen droplets during hurricane gusts.", null, Hidden()));
            debrisLimit = Config.Bind("2. Hurricane", "Flying debris limit", 48,
                new ConfigDescription("Maximum pooled cosmetic debris pieces.", new AcceptableValueRange<int>(12, 96), Hidden()));
            boost = Percentage("Internal", "More rain increase (%)", 50,
                "Internal strength for More rain; preserves an older custom rain boost when upgrading.", true);
            strikeTest = Config.Bind("3. Lightning lights", "Play test lightning", false,
                new ConfigDescription("Action requested by the Testing foldout.", null, Hidden()));
            strikeTest.Value = false;

            applyingPreset = true;
            if (schema.Value < 2)
            {
                if (oldMenu)
                {
                    float oldBoost = Legacy("1. Settings", "Rain chance boost (%)", 0f);
                    float oldChance = Legacy("1. Settings", "Thunderstorm chance (%)", 50f);
                    frequency.Value = oldBoost >= 100 ? RainFrequency.AlwaysRain
                        : oldBoost > 0 ? RainFrequency.MoreRain : RainFrequency.TarkovDefault;
                    boost.Value = oldBoost > 0 ? oldBoost : 50;
                    summerChance.Value = oldChance;
                    otherChance.Value = oldChance;
                    muffle.Value = Legacy("1. Settings", "Rain muffles other game sounds", true);
                    hurricaneEnabled.Value = Legacy("2. Hurricane", "Hurricane effects", true);
                    lightFlicker.Value = Legacy("3. Lightning lights", "Night lightning flicker", true);
                    preset.Value = WeatherPresetChoice.Custom;
                    Trace("[ROT-MENU] Existing weather/effects/sound settings migrated to Custom. Choose Occasional storms to reduce rain.");
                }
                else
                {
                    ApplyPreset(WeatherPresetChoice.OccasionalStorms);
                }
                schema.Value = 2;
            }
            applyingPreset = false;

            preset.SettingChanged += (_, _) => ApplyPreset(preset.Value);
            ConfigEntryBase[] customSettings = { frequency, summerChance, otherChance, muffle,
                hurricaneEnabled, lightFlicker, outdoorRain, indoorRain, outdoorSound, indoorSound,
                extraDrops, debrisLimit };
            Config.SettingChanged += (_, change) =>
            {
                if (!customSettings.Contains(change.ChangedSetting))
                {
                    return;
                }
                if (!applyingPreset)
                {
                    preset.Value = WeatherPresetChoice.Custom;
                }
                retrySync = 0;
            };
            frequency.SettingChanged += (_, _) =>
            {
                if (!applyingPreset && frequency.Value == RainFrequency.MoreRain)
                {
                    boost.Value = 50;
                }
            };
            enabledSetting.SettingChanged += (_, _) => retrySync = 0;
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
                    syncStatus = "Test lightning needs an active outdoor thunderstorm. Select Always rain and 100% chances before a raid to test.";
                }
            };

            Config.Bind("5. Advanced", "Advanced settings", false,
                new ConfigDescription("Optional sound and hurricane details.", null,
                    new ConfigurationManagerAttributes
                    {
                        HideDefaultButton = true,
                        HideSettingName = true,
                        CustomDrawer = DrawAdvanced
                    }));
            Config.Bind("6. Testing", "Testing", false,
                new ConfigDescription("Optional storm test action and connection status.", null,
                    new ConfigurationManagerAttributes
                    {
                        HideDefaultButton = true,
                        HideSettingName = true,
                        CustomDrawer = DrawTesting
                    }));
        }

        private void ApplyPreset(WeatherPresetChoice choice)
        {
            if (choice == WeatherPresetChoice.Custom)
            {
                return;
            }
            applyingPreset = true;
            frequency.Value = WeatherPreferences.FrequencyFor(choice);
            boost.Value = 50;
            summerChance.Value = WeatherPreferences.SummerChanceFor(choice);
            otherChance.Value = WeatherPreferences.OtherChanceFor(choice);
            hurricaneEnabled.Value = choice == WeatherPresetChoice.FrequentStorms;
            muffle.Value = true;
            lightFlicker.Value = true;
            outdoorRain.Value = 70;
            indoorRain.Value = 40;
            outdoorSound.Value = 25;
            indoorSound.Value = 60;
            extraDrops.Value = true;
            debrisLimit.Value = 48;
            applyingPreset = false;
            retrySync = 0;
        }

        private static void DrawPercent(ConfigEntry<float> entry)
        {
            GUILayout.Label(entry.Definition.Key + ": " + entry.Value.ToString("0") + "%");
            GUILayout.BeginHorizontal();
            float value = Mathf.Round(GUILayout.HorizontalSlider(entry.Value, 0, 100));
            if (Mathf.Abs(value - entry.Value) >= 1)
            {
                entry.Value = value;
            }
            if (GUILayout.Button("Reset", GUILayout.Width(60)))
            {
                entry.Value = (float)entry.DefaultValue;
            }
            GUILayout.EndHorizontal();
        }

        private GUIStyle? menuHelpStyle;

        private void DrawHelp(string text)
        {
            menuHelpStyle ??= new GUIStyle(GUI.skin.label) { wordWrap = true };
            GUILayout.Label(text, menuHelpStyle);
        }

        private void DrawAdvanced(ConfigEntryBase setting)
        {
            // Configuration Manager draws values inside a horizontal row.
            // Own a vertical group so expanded controls stack instead of spilling sideways.
            GUILayout.BeginVertical(GUILayout.ExpandWidth(true));
            try
            {
                var expanded = (ConfigEntry<bool>)setting;
                expanded.Value = GUILayout.Toggle(expanded.Value, "Show advanced settings");
                if (!expanded.Value)
                {
                    return;
                }
                DrawPercent(indoorRain);
                DrawPercent(outdoorSound);
                DrawPercent(indoorSound);
                extraDrops.Value = GUILayout.Toggle(extraDrops.Value, "Extra rain on eyes and goggles during hurricanes");
                GUILayout.Label("Flying debris limit: " + debrisLimit.Value);
                int count = Mathf.RoundToInt(GUILayout.HorizontalSlider(debrisLimit.Value, 12, 96));
                if (count != debrisLimit.Value)
                {
                    debrisLimit.Value = count;
                }
            }
            finally
            {
                GUILayout.EndVertical();
            }
        }

        private void DrawTesting(ConfigEntryBase setting)
        {
            // Configuration Manager draws values inside a horizontal row.
            // Own a vertical group so expanded controls stack instead of spilling sideways.
            GUILayout.BeginVertical(GUILayout.ExpandWidth(true));
            try
            {
                var expanded = (ConfigEntry<bool>)setting;
                expanded.Value = GUILayout.Toggle(expanded.Value, "Show testing tools");
                if (!expanded.Value)
                {
                    return;
                }
                DrawHelp(syncStatus);
                DrawHelp("Rain frequency changes future forecasts. Thunderstorm chances apply only when it rains.");
                if (GUILayout.Button("Play test lightning (active storm required)"))
                {
                    strikeTest.Value = true;
                }
            }
            finally
            {
                GUILayout.EndVertical();
            }
        }
    }
}
