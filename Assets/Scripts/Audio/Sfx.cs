using System.Collections.Generic;
using UnityEngine;

// Synthesized chiptune blips, so the slice has sound without audio assets. Replace clips with real files any time.
namespace Obscure
{
    public enum Sound { Move, Confirm, Back, Deny }

    public static class Sfx
    {
        const int Rate = 22050;
        static AudioSource source;
        static readonly Dictionary<Sound, AudioClip> Clips = new Dictionary<Sound, AudioClip>();

        public static void Init(GameObject host)
        {
            source = host.AddComponent<AudioSource>();
            Clips[Sound.Move] = Tone((660, 0.035f));
            Clips[Sound.Confirm] = Tone((520, 0.05f), (780, 0.08f));
            Clips[Sound.Back] = Tone((440, 0.05f), (294, 0.08f));
            Clips[Sound.Deny] = Tone((110, 0.14f));
        }

        public static void Play(Sound s)
        {
            if (source != null && Clips.TryGetValue(s, out var clip)) source.PlayOneShot(clip, 0.25f);
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
