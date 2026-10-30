using System;

namespace ReturnOfThunderstorms
{
    public static class AudioGainPolicy
    {
        // Exact exposed parameter in Tarkov's gameplay mixer. Lowering this bus
        // muffles gameplay without turning down our rain/thunder master route.
        // This is a native integration name, not an external service or file path.
        public const string WorldParameter = "WorldVolume";
        public static float ApplyDb(float original, float gain) => (float)Math.Min(original, Math.Max(-80, original + 20 * Math.Log10(Math.Max(0.0001, Math.Min(1, gain)))));
        public static bool RainLabel(string name)
        {
            for (int start = 0; start < name.Length;)
            {
                int index = name.IndexOf("rain", start, StringComparison.OrdinalIgnoreCase);
                if (index < 0)
                {
                    return false;
                }

                if (index == 0 || !char.IsLetter(name[index - 1]) || name[index] == 'R' && char.IsLower(name[index - 1]))
                {
                    return true;
                }

                start = index + 4;
            }

            return false;
        }
    }
}
