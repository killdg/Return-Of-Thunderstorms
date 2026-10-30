using Comfort.Common;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ReturnOfThunderstorms
{
    internal sealed class StormtideRecordedRain(StormAudioAssets assets)
    {
        private GameObject? root;
        private AudioSource? source;
        private AudioLowPassFilter? shelterFilter;
        private double exposure;
        private float nextStart;
        private int attempts;
        public double Exposure => exposure;
        public float Blend { get; private set; }
        public bool Playing => source != null && source.isPlaying;

        public void Tick(float progress, float volume, float indoorGain = 0.4f, float outdoorGain = 1)
        {
            if (assets.Rain == null)
            {
                return;
            }

            if (root == null)
            {
                root = new GameObject(StormAudioPolicy.RainSourceName);
                source = root.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.loop = true;
                source.spatialBlend = 0;
                source.dopplerLevel = 0;
                source.volume = 0;
                source.clip = assets.Rain;
                exposure = RainController.IsCameraUnderRain ? 1 : 0;
                shelterFilter = root.AddComponent<AudioLowPassFilter>();
                shelterFilter.lowpassResonanceQ = 1;
            }

            if (source!.clip != assets.Rain)
            {
                source.Stop();
                source.clip = assets.Rain;
                nextStart = 0;
            }

            if (!WeatherAudioOutput.Attach(source!))
            {
                source!.volume = 0;
                return;
            }

            if (!source!.isPlaying && Time.unscaledTime >= nextStart)
            {
                nextStart = Time.unscaledTime + 2;
                attempts++;
                source.Play();
                Plugin.Trace("[ROT-AUDIO] rain-start attempt=" + attempts + " " + WeatherAudioOutput.Describe(source));
            }

            Blend = Mathf.MoveTowards(Blend, Mathf.Clamp01(progress), Time.unscaledDeltaTime * 0.5f);
            exposure = StormAudioPolicy.ShelterStep(exposure, RainController.IsCameraUnderRain, Time.unscaledDeltaTime);
            shelterFilter!.cutoffFrequency = (float)StormAudioPolicy.ShelterCutoff(exposure);
            source!.volume = (float)(StormAudioPolicy.RecordedRain(Blend) * StormAudioPolicy.ShelterGain(exposure, indoorGain, outdoorGain)) * Mathf.Clamp01(volume);
        }

        public void Stop()
        {
            if (source != null)
            {
                source.Stop();
            }

            if (root != null)
            {
                Object.Destroy(root);
            }

            root = null;
            source = null;
            shelterFilter = null;
            exposure = 0;
            Blend = 0;
            nextStart = 0;
            attempts = 0;
        }

        internal string Diagnostics => source == null
            ? "rainSource=missing"
            : $"rainPlaying={source.isPlaying} state=[{WeatherAudioOutput.Describe(source)}] " +
                $"rainVolume={source.volume:0.000} rainTime={source.time:0.0} exposure={exposure:0.00} " +
                $"rainMixer={source.outputAudioMixerGroup?.name ?? "none"}";
    }
}
