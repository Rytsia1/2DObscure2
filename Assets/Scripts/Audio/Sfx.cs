using System;
using System.Collections.Generic;
using UnityEngine;

// Synthesized UI & combat sound effects, with optional Resources/Sfx/<name> audio asset override.
namespace Obscure
{
    public enum Sound { Move, Confirm, Back, Deny, Success } // Success: progress made (hero added, prize claimed, rest taken)

    public static class Sfx
    {
        public const int Rate = 44100;
        public const float UiHoverDuration = 0.25f;
        public const string UiHoverResourcePath = "Sfx/ui_hover";
        public const float UiClickDuration = 0.3f;
        public const string UiClickResourcePath = "Sfx/ui_click";
        public const float UiConfirmDuration = 0.6f;
        public const string UiConfirmResourcePath = "Sfx/ui_confirm";

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
            var clickAsset = Resources.Load<AudioClip>(UiClickResourcePath);
            Clips[Sound.Confirm] = clickAsset != null ? clickAsset : SynthesizeUiClick();
            var confirmAsset = Resources.Load<AudioClip>(UiConfirmResourcePath);
            Clips[Sound.Success] = confirmAsset != null ? confirmAsset : SynthesizeUiConfirm();
            Clips[Sound.Back] = Tone((440, 0.05f), (294, 0.08f));
            Clips[Sound.Deny] = Tone((110, 0.14f));
        }

        public static AudioClip GetClip(Sound s) =>
            Clips.TryGetValue(s, out var clip) ? clip : null;

        public static void Play(Sound s)
        {
            if (source != null && Clips.TryGetValue(s, out var clip))
                source.PlayOneShot(clip, s == Sound.Move ? 0.32f : s == Sound.Confirm ? 0.4f : s == Sound.Success ? 0.45f : 0.25f);
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

        // Satisfying 0.3s button click for a dark fantasy anime JRPG: a crisp two-stage mechanical click
        // (sharp tick, softer latch, low thump for weight) with a short, slightly futuristic magical chime on top.
        public static float[] GenerateUiClickSamples()
        {
            int count = Mathf.RoundToInt(UiClickDuration * Rate); // 13,230 samples (0.3s at 44.1 kHz)
            var samples = new float[count];
            var noise = new System.Random(7); // fixed seed: the same click every time
            const float twoPi = Mathf.PI * 2f;

            for (int i = 0; i < count; i++)
            {
                float t = (float)i / Rate;
                float grit = (float)(noise.NextDouble() * 2.0 - 1.0);

                // 1. Mechanical click (0.000s - 0.040s): 0.6ms attack tick, a latch 9ms later, a low body thump.
                float attack = t < 0.0006f ? t / 0.0006f : 1f;
                float tick = 0.75f * Mathf.Exp(-t / 0.003f) * (0.6f * Mathf.Sin(twoPi * 3400f * t) + 0.4f * grit);
                float tl = t - 0.009f;
                float latch = tl > 0f ? 0.26f * Mathf.Exp(-tl / 0.005f) * (0.7f * Mathf.Sin(twoPi * 1800f * tl) + 0.3f * grit) : 0f;
                float thump = 0.30f * Mathf.Sin(twoPi * 160f * t) * Mathf.Exp(-t / 0.014f);
                float click = attack * (tick + latch + thump);

                // 2. Magical accent (0.006s - 0.300s): G-major crystal voicing with a detuned shimmer pair,
                // plus a quick rising "zip" that gives the futuristic edge. Short resonant tail.
                float chime = 0f;
                chime += Partial(t, 0.006f, 0.003f, 0.085f, 1567.98f, 0f, 0.24f);   // G6
                chime += Partial(t, 0.008f, 0.004f, 0.075f, 2349.32f, 0.5f, 0.17f); // D7
                chime += Partial(t, 0.010f, 0.005f, 0.060f, 3135.96f, 1.2f, 0.09f); // G7
                chime += Partial(t, 0.010f, 0.005f, 0.060f, 3140.20f, 2.0f, 0.08f); // G7 +4Hz beating shimmer
                chime += Partial(t, 0.012f, 0.005f, 0.045f, 3951.07f, 0.9f, 0.05f); // B7 sparkle
                float tz = t - 0.004f;
                if (tz > 0f && tz < 0.03f)
                    chime += 0.07f * Mathf.Exp(-tz / 0.01f) * Mathf.Sin(twoPi * (2200f * tz + 0.5f * 55000f * tz * tz)); // 2.2 -> 3.85 kHz

                // Clean cosine fade-out over the last 25ms so the clip ends at exact zero.
                float endWindow = 1f;
                if (t > UiClickDuration - 0.025f)
                {
                    float u = Mathf.Clamp01((t - (UiClickDuration - 0.025f)) / 0.025f);
                    endWindow = 0.5f * (1f + Mathf.Cos(Mathf.PI * u));
                }

                samples[i] = Mathf.Clamp((click + chime) * endWindow, -0.95f, 0.95f);
            }

            return samples;
        }

        // Rewarding 0.6s confirmation chime for a dark fantasy anime JRPG: a warm crystalline D sus2 arpeggio
        // rising D6 -> A6 -> D7 -> E7 over a soft D5 body (no major third, so it resolves without sounding cheery),
        // each bell note carrying a faint inharmonic overtone, then a soft sparkling tail of high glints.
        public static float[] GenerateUiConfirmSamples()
        {
            int count = Mathf.RoundToInt(UiConfirmDuration * Rate); // 26,460 samples (0.6s at 44.1 kHz)
            var samples = new float[count];
            const float twoPi = Mathf.PI * 2f;

            // (onset, hz, amp, decay): the ascending movement, ~55ms apart.
            var notes = new (float at, float hz, float amp, float decay)[]
            {
                (0.000f, 1174.66f, 0.22f, 0.20f), // D6
                (0.055f, 1760.00f, 0.19f, 0.20f), // A6
                (0.110f, 2349.32f, 0.17f, 0.22f), // D7
                (0.165f, 2637.02f, 0.13f, 0.24f), // E7 (sus2 colour)
            };

            // Sparkles: short high glints scattered through the tail, fixed seed so the sound never changes.
            var rng = new System.Random(11);
            var glints = new (float at, float hz, float amp)[14];
            for (int g = 0; g < glints.Length; g++)
            {
                float at = 0.14f + (float)rng.NextDouble() * 0.38f;
                glints[g] = (at, 4200f + (float)rng.NextDouble() * 3200f, 0.045f * (1f - (at - 0.14f) / 0.5f));
            }

            for (int i = 0; i < count; i++)
            {
                float t = (float)i / Rate;

                // 1. Warm body: a soft D5 bloom that holds the chime up.
                float body = Partial(t, 0f, 0.012f, 0.16f, 587.33f, 0f, 0.14f) + Partial(t, 0f, 0.012f, 0.12f, 880f, 0.6f, 0.06f);

                // 2. Crystalline ascending chime with a faint bell overtone (x2.76) on each note.
                float chime = 0f;
                foreach (var n in notes)
                {
                    chime += Partial(t, n.at, 0.004f, n.decay, n.hz, 0f, n.amp);
                    chime += Partial(t, n.at, 0.002f, n.decay * 0.35f, n.hz * 2.76f, 1.3f, n.amp * 0.18f);
                }
                chime += Partial(t, 0.165f, 0.006f, 0.26f, 2640.8f, 2.1f, 0.06f); // slow shimmer beating against E7

                // 3. Soft sparkling tail.
                float sparkle = 0f;
                foreach (var g in glints) sparkle += Partial(t, g.at, 0.0015f, 0.018f, g.hz, 0f, g.amp);
                sparkle *= 1f + 0.3f * Mathf.Sin(twoPi * 9f * t);

                // Clean cosine fade-out over the last 60ms so the clip ends at exact zero.
                float endWindow = 1f;
                if (t > UiConfirmDuration - 0.06f)
                {
                    float u = Mathf.Clamp01((t - (UiConfirmDuration - 0.06f)) / 0.06f);
                    endWindow = 0.5f * (1f + Mathf.Cos(Mathf.PI * u));
                }

                samples[i] = Mathf.Clamp((body + chime + sparkle) * endWindow, -0.95f, 0.95f);
            }

            return samples;
        }

        public static AudioClip SynthesizeUiConfirm()
        {
            var data = GenerateUiConfirmSamples();
            var clip = AudioClip.Create("ui_confirm", data.Length, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        public static AudioClip SynthesizeUiClick()
        {
            var data = GenerateUiClickSamples();
            var clip = AudioClip.Create("ui_click", data.Length, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
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
