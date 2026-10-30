using System;
using System.ComponentModel;

namespace ReturnOfThunderstorms
{
    public enum WeatherPresetChoice
    {
        [Description("Occasional storms")]
        OccasionalStorms,
        [Description("Vanilla rain + thunderstorms")]
        VanillaRainWithThunderstorms,
        [Description("Frequent storms")]
        FrequentStorms,
        Custom
    }

    public enum RainFrequency
    {
        Never,
        [Description("Less rain")]
        LessRain,
        [Description("Tarkov default")]
        TarkovDefault,
        [Description("More rain")]
        MoreRain,
        [Description("Always rain")]
        AlwaysRain
    }

    // Shared by the server, client and tests. These are forecast weights, not a
    // promise that a fixed percentage of raids will contain rain.
    public static class WeatherPreferences
    {
        public static double RainProbability(double baseline, RainFrequency frequency, double increase = 50)
        {
            switch (frequency)
            {
                case RainFrequency.Never:
                    return 0;
                case RainFrequency.LessRain:
                    return baseline * 0.5;
                case RainFrequency.MoreRain:
                    return StormPolicy.RainProbability(baseline, increase);
                case RainFrequency.AlwaysRain:
                    return 1;
                default:
                    return baseline;
            }
        }

        public static bool RainOnly(StormSettings settings) => settings.Enabled
            && (settings.Frequency == RainFrequency.AlwaysRain
                || settings.Frequency == null && settings.RainBoost == 100);

        public static bool IsDefined(RainFrequency? frequency) => frequency == null
            || Enum.IsDefined(typeof(RainFrequency), frequency.Value);

        public static double ThunderstormChance(bool summer, double summerChance, double otherChance) =>
            summer ? summerChance : otherChance;

        public static RainFrequency FrequencyFor(WeatherPresetChoice preset)
        {
            switch (preset)
            {
                case WeatherPresetChoice.OccasionalStorms:
                    return RainFrequency.LessRain;
                case WeatherPresetChoice.FrequentStorms:
                    return RainFrequency.MoreRain;
                default:
                    return RainFrequency.TarkovDefault;
            }
        }

        public static float SummerChanceFor(WeatherPresetChoice preset) =>
            preset == WeatherPresetChoice.FrequentStorms ? 75 : 30;

        public static float OtherChanceFor(WeatherPresetChoice preset) =>
            preset == WeatherPresetChoice.FrequentStorms ? 50 : 15;
    }
}
