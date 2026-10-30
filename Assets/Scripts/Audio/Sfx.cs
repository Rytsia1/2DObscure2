using System;
using System.Collections.Generic;
using UnityEngine;

// Synthesized UI & combat sound effects, with optional Resources/Sfx/<name> audio asset override.
namespace Obscure
{
    public enum Sound { Move, Confirm, Back, Deny }

    public static class Sfx
    {
        public const int Rate = 44100;
        public const float UiHoverDuration = 0.25f;
        public const string UiHoverResourcePath = "Sfx/ui_hover";

        static AudioSource source;
        static readonly Dictionary<Sound, AudioClip> Clips = new Dictionary<Sound, AudioClip>();

        public static void Init(GameObject host)
        {
            var existing = host.GetComponent<AudioSource>();
            source = existing != null ? existing : host.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;

            var hoverAsset = Resources.Load<AudioClip>(UiHoverResourcePath);
            Clips[Sound.Move] = hoverAsset != null ? hoverAsset : SynthesizeUiHover();
            Clips[Sound.Confirm] = Tone((520, 0.05f), (780, 0.08f));
            Clips[Sound.Back] = Tone((440, 0.05f), (294, 0.08f));
            Clips[Sound.Deny] = Tone((110, 0.14f));
        }

        public static AudioClip GetClip(Sound s) =>
            Clips.TryGetValue(s, out var clip) ? clip : null;

        public static void Play(Sound s)
        {
            if (source != null && Clips.TryGetValue(s, out var clip))
                source.PlayOneShot(clip, s == Sound.Move ? 0.32f : 0.25f);
        }

        // Subtle and elegant 0.25s UI hover sound effect for a dark fantasy anime JRPG:
        // a soft crystalline tick followed by a delicate, slightly mystical magical shimmer.
        public static float[] GenerateUiHoverSamples()
        {
            int count = Mathf.RoundToInt(UiHoverDuration * Rate); // 11,025 samples (0.25s at 44.1 kHz)
            var samples = new float[count];
            const float twoPi = Mathf.PI * 2f;

            for (int i = 0; i < count; i++)
            {
                float t = (float)i / Rate;

                // 1. Soft crystalline tick (0.000s - 0.035s):
                // Fast 1.2ms raised-cosine attack + resonant glassy transient + rapid exponential decay.
                float tickAttack = t < 0.0012f
                    ? 0.5f * (1f - Mathf.Cos(Mathf.PI * (t / 0.0012f)))
                    : 1f;
                float pitchDropHz = 2960f + 1840f * Mathf.Exp(-t / 0.0035f);
                float crystalTap =
                    0.34f * Mathf.Sin(twoPi * pitchDropHz * t) * Mathf.Exp(-t / 0.0045f) +
                    0.26f * Mathf.Sin(twoPi * 2637.02f * t) * Mathf.Exp(-t / 0.011f) +
                    0.16f * Mathf.Sin(twoPi * 3951.07f * t + 0.4f) * Mathf.Exp(-t / 0.0085f);
                float tick = tickAttack * crystalTap;

                // 2. Delicate magical shimmer (0.002s - 0.250s):
                // Mystical dark-fantasy anime JRPG crystalline voicing (E6 - B6 - F#7 detuned pair - A7 - E8)
                // with micro-staggered harmonic bloom and smooth exponential decay.
                float shimmer = 0f;
                shimmer += Partial(t, 0.0015f, 0.004f, 0.070f, 1318.51f, 0f, 0.26f); // E6 fundamental chime
                shimmer += Partial(t, 0.0045f, 0.006f, 0.082f, 1975.53f, 0.3f, 0.21f); // B6 crystalline fifth
                shimmer += Partial(t, 0.0080f, 0.008f, 0.092f, 2959.96f, 0.7f, 0.14f); // F#7 mystical ninth A
                shimmer += Partial(t, 0.0080f, 0.008f, 0.092f, 2963.40f, 1.9f, 0.13f); // F#7+3.4Hz shimmer beating B
                shimmer += Partial(t, 0.0120f, 0.009f, 0.068f, 3520.00f, 1.1f, 0.09f); // A7 astral fourth
                shimmer += Partial(t, 0.0150f, 0.008f, 0.055f, 5274.04f, 2.2f, 0.06f); // E8 airy crystal dust

                // Clean cosine fade-out over the last 25ms (0.225s -> 0.250s) so the 0.25s clip ends at exact zero.
                float endWindow = 1f;
                if (t > 0.225f)
                {
                    float u = Mathf.Clamp01((t - 0.225f) / 0.025f);
                    endWindow = 0.5f * (1f + Mathf.Cos(Mathf.PI * u));
                }

                samples[i] = Mathf.Clamp((tick + shimmer) * endWindow, -0.95f, 0.95f);
            }

            return samples;
        }

        static float Partial(float t, float delay, float attack, float decay, float hz, float phase, float amp)
        {
            float dt = t - delay;
            if (dt <= 0f) return 0f;
            float envAttack = dt < attack
                ? 0.5f * (1f - Mathf.Cos(Mathf.PI * (dt / attack)))
                : 1f;
            float envDecay = Mathf.Exp(-dt / decay);
            return amp * envAttack * envDecay * Mathf.Sin(Mathf.PI * 2f * hz * dt + phase);
        }

        public static AudioClip SynthesizeUiHover()
        {
            var data = GenerateUiHoverSamples();
            var clip = AudioClip.Create("ui_hover", data.Length, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        // Square wave notes in sequence with a linear decay per note.
        static AudioClip Tone(params (float hz, float seconds)[] notes)
        {
            var data = new List<float>();
            foreach (var (hz, seconds) in notes)
            {
                int n = (int)(seconds * Rate);
                for (int i = 0; i < n; i++)
                    data.Add(((int)(i * hz * 2 / Rate) % 2 == 0 ? 1f : -1f) * (1f - (float)i / n));
            }
            var clip = AudioClip.Create("sfx", data.Count, 1, Rate, false);
            clip.SetData(data.ToArray(), 0);
            return clip;
        }
    }
}
