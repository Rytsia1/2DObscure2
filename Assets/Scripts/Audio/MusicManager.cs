using System.Collections;
using UnityEngine;

// Background music player, one per game, living on the Game object. It owns a single AudioSource, so two tracks
// are never audible together: a change fades the current track out, waits a short gap, then fades the next one in.
namespace Obscure
{
    public class MusicManager : MonoBehaviour
    {
        public static MusicManager I { get; private set; }

        public MusicLibrary Library { get; private set; }
        public AudioSource Source { get; private set; }
        public string CurrentId => target?.id;   // the track playing or being faded to; null = silence
        public bool Fading => fade != null;

        MusicTrack target, playing;
        float level;                // fade envelope 0..1
        float settingsVolume = 1f;  // music volume from GameSettings (independent of sound effects)
        Coroutine fade;
        readonly System.Random rng = new System.Random(); // battle theme picks; separate from gameplay RNG

        public static MusicManager Create(GameObject host)
        {
            if (I != null) return I;
            var m = host.AddComponent<MusicManager>();
            m.Library = Resources.Load<MusicLibrary>(MusicLibrary.ResourcePath);
            if (m.Library == null) Debug.LogWarning($"No Resources/{MusicLibrary.ResourcePath} asset: music is disabled.");
            return m;
        }

        void Awake()
        {
            if (I != null && I != this) { Destroy(this); return; }
            I = this;
            Source = gameObject.AddComponent<AudioSource>();
            Source.playOnAwake = false;
            Source.spatialBlend = 0f;
        }

        void OnDestroy()
        {
            if (I == this) I = null;
        }

        public void PlayFor(GameState state, RunState run)
        {
            if (Library == null) return;
            var id = Library.MusicFor(state, run, rng);
            if (id != null) Play(id);
        }

        // Requesting the track already playing (or already being faded to) never restarts it.
        public void Play(string id)
        {
            var track = Library != null ? Library.Get(id) : null;
            if (!string.IsNullOrEmpty(id) && track == null) Debug.LogWarning($"Music '{id}' is not in the MusicLibrary.");
            if (track == target) return;
            target = track;
            if (fade != null) StopCoroutine(fade);
            fade = StartCoroutine(SwitchTo(track));
        }

        public void Stop() => Play(null);

        public void SetVolume(float volume)
        {
            settingsVolume = Mathf.Clamp01(volume);
            Apply();
        }

        // Interrupting is safe: every step starts from the current envelope level and the current clip.
        IEnumerator SwitchTo(MusicTrack next)
        {
            if (playing != null && (next == null || playing.clip != next.clip))
            {
                yield return Ramp(0f, Library.FadeOut(playing));
                Source.Stop();
                playing = null;
                if (next != null && Library.transitionGap > 0) yield return new WaitForSecondsRealtime(Library.transitionGap);
            }
            if (next != null)
            {
                if (playing == null)
                {
                    playing = next;
                    Source.clip = next.clip;
                    Source.loop = next.loop;
                    level = 0f;
                    Apply();
                    Source.Play();
                }
                yield return Ramp(1f, Library.FadeIn(next)); // also brings back a track that was fading out
            }
            fade = null;
        }

        IEnumerator Ramp(float to, float seconds)
        {
            float from = level, duration = seconds * Mathf.Abs(to - from); // a half-done fade finishes in half the time
            for (float t = 0; t < duration; t += Time.unscaledDeltaTime)
            {
                level = Mathf.Lerp(from, to, t / duration);
                Apply();
                yield return null;
            }
            level = to;
            Apply();
        }

        void Apply()
        {
            if (Source != null) Source.volume = level * settingsVolume * (playing?.volume ?? 1f);
        }
    }
}
