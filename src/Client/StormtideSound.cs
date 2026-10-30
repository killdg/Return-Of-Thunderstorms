using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace ReturnOfThunderstorms
{
    internal static class StormtideSound
    {
        internal static bool Active;
        internal static float Gain = 1;
        internal static float RainGain = 1;
        private sealed class Volume
        {
            public float Native;
            public float Applied;
            public AudioClip? Clip;
        }

        private static readonly Dictionary<AudioSource, Volume> levels = new();
        // Native rain sources are identified by clip/object labels or these exact
        // mixer group names. Only native rain is cross-faded against our recording.
        internal static bool Rain(AudioSource source) =>
            source.clip != null
            && (AudioGainPolicy.RainLabel(source.clip.name)
                || AudioGainPolicy.RainLabel(source.gameObject.name)
                || source.outputAudioMixerGroup != null
                && (source.outputAudioMixerGroup.name == "Rain"
                    || source.outputAudioMixerGroup.name.StartsWith("PrecipitationAmbient", StringComparison.Ordinal)));
        internal static void Before(AudioSource? source)
        {
            if (source == null || !levels.TryGetValue(source, out var level))
            {
                return;
            }

            if (Mathf.Abs(source.volume - level.Applied) < 0.0001f)
            {
                source.volume = level.Native;
            }

            levels.Remove(source);
        }

        internal static void Apply(AudioSource? source)
        {
            if (!Active || source == null)
            {
                return;
            }

            if (source.gameObject.name == ThunderPolicy.SourceName || source.gameObject.name == StormAudioPolicy.RainSourceName)
            {
                Before(source);
                return;
            }

            // Gameplay attenuation is handled once on the native World mixer bus.
            if (!Rain(source))
            {
                Before(source);
                return;
            }

            float value = source.volume;
            if (!levels.TryGetValue(source, out var level))
            {
                level = new()
                {
                    Native = value,
                    Clip = source.clip
                };
                levels[source] = level;
            }
            else if (Mathf.Abs(value - level.Applied) > 0.0001f || level.Clip != source.clip)
            {
                level.Native = value;
                level.Clip = source.clip;
            }

            level.Applied = Mathf.Clamp01(level.Native * (Rain(source) ? RainGain : Gain));
            source.volume = level.Applied;
        }

        internal static void Stop()
        {
            Active = false;
            StormtideNativeAudio.Clear();
            foreach (var source in levels.Keys.ToArray())
            {
                Before(source);
            }

            levels.Clear();
            RainGain = 1;
            Gain = 1;
        }
    }

    [HarmonyPatch]
    internal static class StormtideNativeAudio
    {
        private static readonly Dictionary<BetterSource, AudioSource[]> sources = new();
        internal static void Clear() => sources.Clear();
        private static AudioSource[] Sources(BetterSource source)
        {
            if (!sources.TryGetValue(source, out var values))
            {
                values = source.GetComponentsInChildren<AudioSource>(true);
                sources[source] = values;
            }

            return values;
        }

        // These two concrete volume-update bodies are inspected in the installed game. Do not patch throwing playback stubs.
        private static IEnumerable<MethodBase> TargetMethods() => new MethodBase[]
        {
            AccessTools.Method(typeof(BetterSource), "UpdateSourceVolume", new[] { typeof(float) }),
            AccessTools.Method(typeof(SuperSourceDistant), "UpdateSourceVolume", new[] { typeof(float) })
        };
        private static void Prefix(BetterSource __instance)
        {
            if (StormtideSound.Active)
            {
                foreach (var source in Sources(__instance))
                {
                    StormtideSound.Before(source);
                }
            }
        }

        private static void Postfix(BetterSource __instance)
        {
            if (StormtideSound.Active)
            {
                foreach (var source in Sources(__instance))
                {
                    StormtideSound.Apply(source);
                }
            }
        }
    }
}
