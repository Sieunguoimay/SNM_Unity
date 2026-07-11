namespace Snm.GrassSystemV2
{
    /// <summary>
    /// Named wind "moods" applied to a <see cref="GrassWorldConfig"/> with one
    /// click. Presets touch only the atmosphere fields (wind speed / noise /
    /// lean / coherence) — never structural (chunk size) or performance
    /// (distances, budgets, resolution) fields, so applying one to a tuned scene
    /// re-flavours the wind without breaking painted data or the platform budget.
    ///
    /// Code-defined on purpose: no extra asset to manage per prototype, matching
    /// the "self-contained, zero extra assets" design of the config itself.
    /// </summary>
    public static class GrassWindPresets
    {
        public readonly struct Preset
        {
            public readonly string Name;
            public readonly float Speed;
            public readonly float NoiseScale;
            public readonly float Lean;
            public readonly float Coherence;

            public Preset(string name, float speed, float noiseScale, float lean, float coherence)
            {
                Name = name;
                Speed = speed;
                NoiseScale = noiseScale;
                Lean = lean;
                Coherence = coherence;
            }
        }

        public static readonly Preset[] All =
        {
            //         name       speed  noise  lean  coherence
            new("Still",   0.15f, 0.06f, 0.05f, 0.4f),  // barely-there air, almost static
            new("Calm",    0.5f,  0.06f, 0.15f, 0.6f),  // light breeze, gentle sway
            new("Meadow",  1.0f,  0.08f, 0.30f, 0.55f), // the everyday default
            new("Windy",   2.0f,  0.05f, 0.50f, 0.75f), // clear leaning waves
            new("Storm",   3.5f,  0.04f, 0.70f, 0.85f), // strong, coherent, laid-over gusts
        };

        /// <summary>Writes a preset's atmosphere fields into the config, leaving direction and everything else intact.</summary>
        public static void Apply(GrassWorldConfig config, in Preset preset)
        {
            config.windSpeed = preset.Speed;
            config.windNoiseScale = preset.NoiseScale;
            config.windLean = preset.Lean;
            config.windCoherence = preset.Coherence;
        }
    }
}
