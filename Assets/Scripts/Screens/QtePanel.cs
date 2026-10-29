using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Obscure
{
    // Draws a running Qte in the combat command panel and feeds it input. Owns no rules.
    public class QtePanel : MonoBehaviour
    {
        const int X = 160, W = 150, CenterX = 235, TrackY = 138, TrackH = 9;
        const float Impact = 0.82f; // defense track: where hits land, as a fraction of the bar

        Qte qte;
        int bornFrame, lastResolved = -1;
        Keyboard keyboard;
        Label info, result;
        SpriteRenderer timeFill;
        readonly List<SpriteRenderer> markers = new List<SpriteRenderer>();
        readonly List<Label> letters = new List<Label>();

        public Qte Qte => qte;

        public void Init(Qte q, string heading)
        {
            qte = q;
            bornFrame = Time.frameCount; // the key that opened this QTE must not count as input
            Ui.Label(transform, heading, CenterX, 116, Palette.Bone, Align.Center, order: Ui.Widget + 2);
            info = Ui.Label(transform, "", CenterX, 126, Palette.Ash, Align.Center, order: Ui.Widget + 2);
            result = Ui.Label(transform, "", CenterX, 160, Palette.Gold, Align.Center, 2, Ui.Widget + 3);
            string key = Controls.QteKey.ToString().ToUpperInvariant();

            switch (q)
            {
                case TimingQte t:
                    info.Set($"PRESS {key} IN THE ZONE");
                    BuildTrack(t.target / t.Sweep, t.profile.good / t.Sweep, t.profile.perfect / t.Sweep, 1);
                    break;
                case DefenseQte d:
                    float speed = Impact / d.profile.windup; // track fraction per second
                    BuildTrack(Impact, d.profile.good * speed, d.profile.perfect * speed, d.Hits);
                    break;
                case TypingQte ty:
                    BuildTyping(ty);
                    break;
                default:
                    info.Set("QTE AUTO");
                    break;
            }

            keyboard = Keyboard.current;
            if (keyboard != null) keyboard.onTextInput += OnText;
        }

        void OnDestroy()
        {
            if (keyboard != null) keyboard.onTextInput -= OnText;
        }

        void OnText(char c)
        {
            if (qte != null && Time.frameCount != bornFrame) qte.Type(c);
        }

        void Update()
        {
            if (qte == null) return;
            if (!qte.Done && Time.frameCount != bornFrame)
            {
                qte.Tick(Time.unscaledDeltaTime); // real time: QTE timing ignores timeScale
                if (Controls.QtePress) qte.Press();
            }
            Render();
        }

        // ---- building ----

        SpriteRenderer Rect(int x, int y, int w, int h, Color c, int layer)
        {
            var sr = Ui.Image(transform, PixelArt.Pixel, x, y, Ui.Widget + layer, c);
            sr.transform.localScale = new Vector3(Mathf.Max(1, w), h, 1);
            return sr;
        }

        static int Px(float fraction) => Mathf.RoundToInt(fraction * W);

        void BuildTrack(float zone, float goodHalf, float perfectHalf, int markerCount)
        {
            Rect(X - 1, TrackY - 1, W + 2, TrackH + 2, Palette.Shade, 0);
            Rect(X, TrackY, W, TrackH, Palette.Void, 1);
            int cx = X + Px(zone), gw = Mathf.Max(3, Px(goodHalf * 2)), pw = Mathf.Max(1, Px(perfectHalf * 2));
            Rect(cx - gw / 2, TrackY + 1, gw, TrackH - 2, Palette.Moss, 2);
            Rect(cx - pw / 2, TrackY + 1, pw, TrackH - 2, Palette.Gold, 3);
            for (int i = 0; i < markerCount; i++) markers.Add(Rect(X, TrackY - 3, 2, TrackH + 6, Palette.Bone, 4));
        }

        void BuildTyping(TypingQte t)
        {
            int x0 = CenterX - t.sequence.Length * 6;
            for (int i = 0; i < t.sequence.Length; i++)
                letters.Add(Ui.Label(transform, t.sequence[i].ToString(), x0 + i * 12 + 6, 135, Palette.Ash, Align.Center, 2, Ui.Widget + 2));
            Rect(X, 153, W, 3, Palette.Void, 0);
            timeFill = Rect(X, 153, W, 3, Palette.Lilac, 1);
            // Perfect requires finishing before the bar drains past this mark.
            Rect(X + Px(1f - QteConfig.TypingPerfectFraction), 151, 1, 7, Palette.Gold, 2);
        }

        // ---- per-frame ----

        void Render()
        {
            switch (qte)
            {
                case TimingQte t: RenderTiming(t); break;
                case DefenseQte d: RenderDefense(d); break;
                case TypingQte ty: RenderTyping(ty); break;
            }
        }

        void SetMarker(int i, float fraction, Color c)
        {
            markers[i].enabled = true;
            markers[i].color = c;
            markers[i].transform.localPosition = new Vector3(X + Mathf.Clamp(Px(fraction), 0, W - 2) - 1, -(TrackY - 3), 0);
        }

        void RenderTiming(TimingQte t)
        {
            SetMarker(0, t.T / t.Sweep, Palette.Bone);
            if (t.Done && !t.Cancelled) ShowResult(t.Result, "PERFECT!", "GOOD", "MISS");
        }

        void RenderDefense(DefenseQte d)
        {
            string key = Controls.QteKey.ToString().ToUpperInvariant();
            if (d.InTelegraph) info.Set("GET READY...");
            int next = -1;
            for (int i = 0; i < d.Hits; i++)
            {
                float e = d.T - d.SpawnTime(i), w = d.profile.windup, hold = d.delays[i];
                if (e < 0 || d.HitResult(i) != null) { markers[i].enabled = false; continue; }
                if (next < 0) next = i;
                // A delayed swing parks the cue halfway (flashing) before it continues.
                bool holding = hold > 0 && e >= w / 2 && e < w / 2 + hold;
                float p = holding ? Impact / 2 : (e >= w / 2 + hold ? e - hold : e) / w * Impact;
                var c = holding ? (Mathf.Repeat(d.T, 0.2f) < 0.1f ? Palette.Ember : Palette.Bone) : i == next ? Palette.Bone : Palette.Ash;
                SetMarker(i, p, c);
            }
            if (!d.InTelegraph) info.Set(d.Hits > 1 ? $"PRESS {key} ON EACH HIT" : $"PRESS {key} AS IT LANDS");

            for (int i = d.Hits - 1; i >= 0; i--)
                if (d.HitResult(i) is QteResult r)
                {
                    if (i != lastResolved) { lastResolved = i; ShowResult(r, "PARRY!", "BLOCK", "HIT"); }
                    break;
                }
        }

        void RenderTyping(TypingQte t)
        {
            for (int i = 0; i < letters.Count; i++)
                letters[i].Color = i < t.Index ? Palette.Gold : i == t.Index ? Palette.Bone : i >= t.wordLength ? Palette.Lilac : Palette.Ash;
            float left = Mathf.Clamp01(1f - t.T / t.timeLimit);
            timeFill.transform.localScale = new Vector3(Mathf.RoundToInt(W * left), 3, 1);
            info.Set(t.Mistakes > 0 ? $"MISTAKES {t.Mistakes}/{QteConfig.TypingMaxMistakes}" : "TYPE THE INCANTATION");
            info.Color = t.Mistakes > 0 ? Palette.Crimson : Palette.Ash;
            if (t.Done && !t.Cancelled) ShowResult(t.Result, "PERFECT!", "GOOD", "MISS");
        }

        void ShowResult(QteResult r, string perfect, string good, string miss)
        {
            result.Set(r == QteResult.Perfect ? perfect : r == QteResult.Good ? good : miss);
            result.Color = r == QteResult.Perfect ? Palette.Gold : r == QteResult.Good ? Palette.Bone : Palette.Crimson;
        }
    }
}
