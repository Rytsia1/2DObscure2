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
        public const float ZoroaPhaseTransitionDuration = 6.0f;
        public const string ZoroaTransitionResourcePath = "Sfx/zoroa_phase_transition";

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

        // Soft harmonic tones in sequence with a smooth decay per note (replaces harsh unbandlimited square buzz).
        public static float[] GenerateToneSamples(params (float hz, float seconds)[] notes)
        {
            var data = new List<float>();
            const float twoPi = Mathf.PI * 2f;
            foreach (var (hz, seconds) in notes)
            {
                int n = (int)(seconds * Rate);
                for (int i = 0; i < n; i++)
                {
                    float t = (float)i / Rate;
                    float env = 1f - (float)i / n;
                    // Warm musical tone with gentle harmonics rather than harsh square wave
                    float wave = 0.72f * Mathf.Sin(twoPi * hz * t)
                               + 0.20f * Mathf.Sin(twoPi * hz * 2f * t)
                               + 0.08f * Mathf.Sin(twoPi * hz * 3f * t);
                    data.Add(Mathf.Clamp(wave * env, -0.95f, 0.95f));
                }
            }
            return data.ToArray();
        }

        static AudioClip Tone(params (float hz, float seconds)[] notes)
        {
            var data = GenerateToneSamples(notes);
            var clip = AudioClip.Create("sfx", data.Length, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        public static AudioClip GetZoroaPhaseTransitionClip()
        {
            var asset = Resources.Load<AudioClip>(ZoroaTransitionResourcePath);
            return asset != null ? asset : SynthesizeZoroaPhaseTransition();
        }

        public static AudioClip SynthesizeZoroaPhaseTransition()
        {
            var data = GenerateZoroaPhaseTransitionSamples();
            var clip = AudioClip.Create("zoroa_phase_transition", data.Length, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        public static float[] GenerateZoroaPhaseTransitionSamples()
        {
            int count = Mathf.RoundToInt(ZoroaPhaseTransitionDuration * Rate); // 264,600 samples (6.0s at 44.1 kHz)
            var samples = new float[count];
            const float twoPi = Mathf.PI * 2f;

            float SmoothStep(float edge0, float edge1, float x)
            {
                float val = Mathf.Clamp01((x - edge0) / (edge1 - edge0));
                return val * val * (3f - 2f * val);
            }

            float ChoirVoice(float t, float baseHz)
            {
                float vib = 1f + 0.008f * Mathf.Sin(twoPi * 5.2f * t);
                float f = baseHz * vib;
                return 0.28f * Mathf.Sin(twoPi * f * t) +
                       0.22f * Mathf.Sin(twoPi * 2f * f * t + 0.3f) +
                       0.18f * Mathf.Sin(twoPi * 3f * f * t + 0.7f) +
                       0.12f * Mathf.Sin(twoPi * 4f * f * t + 1.2f) +
                       0.08f * Mathf.Sin(twoPi * 5f * f * t + 1.8f) +
                       0.14f * Mathf.Sin(twoPi * (f + 1.4f) * t + 0.5f) +
                       0.14f * Mathf.Sin(twoPi * (f - 1.2f) * t + 1.1f);
            }

            for (int i = 0; i < count; i++)
            {
                float t = (float)i / Rate;
                float s = 0f;

                // 1. Stage 0.0s - 2.0s: Low-frequency impact & soul energy sound
                if (t < 2.5f)
                {
                    float fSub = 36f + 44f * Mathf.Exp(-t / 0.35f);
                    float impactEnv = Mathf.Exp(-t / 0.75f);
                    float subBass = 0.50f * Mathf.Sin(twoPi * fSub * t) * impactEnv;
                    float thump = 0.25f * Mathf.Sin(twoPi * 110f * t) * Mathf.Exp(-t / 0.09f);
                    float rumble = 0.18f * Mathf.Sin(twoPi * 42f * t) * Mathf.Exp(-t / 1.6f);

                    float soulEnv = (t < 0.1f ? t / 0.1f : 1f) * Mathf.Exp(-t / 2.0f);
                    float soulMod = Mathf.Sin(twoPi * 3.8f * t);
                    float wisp1 = 0.14f * Mathf.Sin(twoPi * (290f + 25f * soulMod) * t);
                    float wisp2 = 0.11f * Mathf.Sin(twoPi * (435f - 20f * soulMod) * t + 0.8f);
                    float wisp3 = 0.08f * Mathf.Sin(twoPi * (580f + 35f * soulMod) * t + 1.5f);
                    float shimmer = 0.06f * Mathf.Sin(twoPi * (1480f + 50f * soulMod) * t) +
                                    0.04f * Mathf.Sin(twoPi * (2240f - 30f * soulMod) * t);
                    float soulSound = soulEnv * (wisp1 + wisp2 + wisp3 + shimmer);

                    s += subBass + thump + rumble + soulSound;
                }

                // 2. Stage 2.0s - 4.0s: Choir begins swelling
                if (t >= 1.6f && t <= 5.8f)
                {
                    float choirEnv;
                    if (t < 3.8f) choirEnv = SmoothStep(1.6f, 3.8f, t);
                    else if (t <= 5.2f) choirEnv = 1f;
                    else choirEnv = SmoothStep(5.8f, 5.2f, t);

                    float cChord = 0.28f * ChoirVoice(t, 110.0f) +   // A2
                                   0.26f * ChoirVoice(t, 164.81f) +  // E3
                                   0.24f * ChoirVoice(t, 220.0f) +   // A3
                                   0.22f * ChoirVoice(t, 261.63f) +  // C4
                                   0.20f * ChoirVoice(t, 329.63f);   // E4
                    s += 0.70f * choirEnv * cChord;
                }

                // 3. Stage 4.0s - 5.5s+: Rising strings and brass
                if (t >= 3.6f)
                {
                    float orchEnv;
                    if (t < 5.5f) orchEnv = SmoothStep(3.6f, 5.5f, t);
                    else if (t <= 5.8f) orchEnv = 1f;
                    else orchEnv = SmoothStep(6.0f, 5.8f, t);

                    // Rising strings: 2 octaves ascending glissando
                    float strProgress = SmoothStep(3.6f, 5.5f, t);
                    float strHz = 220f * Mathf.Pow(2f, strProgress * 2f);
                    float strWave = Mathf.Sin(twoPi * strHz * t) +
                                    0.55f * Mathf.Sin(twoPi * strHz * 2f * t + 0.4f) +
                                    0.35f * Mathf.Sin(twoPi * strHz * 3f * t + 0.9f) +
                                    0.25f * Mathf.Sin(twoPi * strHz * 4f * t + 1.3f) +
                                    0.18f * Mathf.Sin(twoPi * (strHz + 1.8f) * t + 0.7f) +
                                    0.18f * Mathf.Sin(twoPi * (strHz - 1.6f) * t + 1.2f);
                    float tremolo = 0.85f + 0.15f * Mathf.Sin(twoPi * 12f * t);
                    float strings = 0.28f * orchEnv * tremolo * strWave;

                    // Rising brass: powerful rasp with harmonic saturation
                    float brassF = 130.81f + strProgress * 34f;
                    float brassWave = Mathf.Sin(twoPi * brassF * t) +
                                      0.75f * Mathf.Sin(twoPi * brassF * 2f * t + 0.2f) +
                                      0.50f * Mathf.Sin(twoPi * brassF * 3f * t + 0.6f) +
                                      0.35f * Mathf.Sin(twoPi * brassF * 4f * t + 1.1f);
                    float brassSat = (float)System.Math.Tanh(brassWave * (1.2f + 1.8f * orchEnv));
                    float brass = 0.32f * orchEnv * brassSat;

                    float swellNoise = Mathf.Sin(twoPi * 78f * t) * Mathf.Sin(twoPi * 38f * t);
                    float timpani = 0.20f * orchEnv * swellNoise;

                    s += strings + brass + timpani;
                }

                // Clean cosine window at the end
                if (t > 5.92f)
                {
                    float u = Mathf.Clamp01((t - 5.92f) / 0.08f);
                    s *= 0.5f * (1f + Mathf.Cos(Mathf.PI * u));
                }

                samples[i] = Mathf.Clamp(s, -0.95f, 0.95f);
            }

            return samples;
        }
    }
}
