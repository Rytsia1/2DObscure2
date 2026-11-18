using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using UiLabel = UnityEngine.UIElements.Label;

namespace Obscure
{
    // Renders an active Qte inside the UITK combat QTE host and feeds keyboard / button input.
    public class QtePanel : MonoBehaviour
    {
        const float TrackW = 480f, TrackH = 26f, Impact = 0.82f;

        Qte qte;
        int bornFrame, lastResolved = -1, lastTypingIndex, lastMistakes;
        bool defenseActiveStarted, heavyTelegraphed, resultSfxPlayed;
        Keyboard keyboard;
        UiLabel infoLbl, resultLbl;
        VisualElement host, timeFill;
        readonly List<VisualElement> markers = new List<VisualElement>();
        readonly List<UiLabel> letters = new List<UiLabel>();

        public Qte Qte => qte;

        public void Init(Qte q, string heading, VisualElement qteHost = null)
        {
            qte = q;
            host = qteHost;
            bornFrame = Time.frameCount;
            lastResolved = -1;
            lastTypingIndex = (q as TypingQte)?.Index ?? 0;
            lastMistakes = (q as TypingQte)?.Mistakes ?? 0;
            defenseActiveStarted = false;
            heavyTelegraphed = false;
            resultSfxPlayed = false;

            if (host != null)
            {
                host.Clear();
                host.style.paddingLeft = 24;
                host.style.paddingRight = 24;
                host.style.paddingTop = 16;
                host.style.paddingBottom = 16;
                host.style.alignItems = UnityEngine.UIElements.Align.Center;
                host.style.justifyContent = Justify.SpaceBetween;

                host.Add(new UiLabel(heading ?? "") { style = { fontSize = 22, color = Palette.Gold1 } });
                infoLbl = new UiLabel("") { style = { fontSize = 14, color = Palette.Mist2 } };
                host.Add(infoLbl);

                switch (q)
                {
                    case TimingQte t:
                        infoLbl.text = $"PRESS [{Controls.QteKey.ToString().ToUpperInvariant()}] IN THE GOLD ZONE";
                        BuildTrack(t.target / t.Sweep, t.EffectiveGood / t.Sweep, t.EffectivePerfect / t.Sweep, 1);
                        break;
                    case DefenseQte d:
                        float speed = Impact / d.profile.windup;
                        float goodScaled = QteConfig.ScaleGood(d.profile, d.TimingBonusFraction);
                        float perfScaled = QteConfig.ScalePerfect(d.profile, d.TimingBonusFraction);
                        BuildTrack(Impact, goodScaled * speed, perfScaled * speed, d.Hits);
                        break;
                    case TypingQte ty:
                        BuildTyping(ty);
                        break;
                    default:
                        infoLbl.text = "QTE ASSIST: AUTO";
                        host.Add(new VisualElement { style = { height = 40 } });
                        break;
                }

                resultLbl = new UiLabel("") { style = { fontSize = 28, color = Palette.Gold1, height = 34 } };
                host.Add(resultLbl);
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
            if (keyboard == null && Keyboard.current != null)
            {
                keyboard = Keyboard.current;
                keyboard.onTextInput += OnText;
            }
            if (!qte.Done && Time.frameCount != bornFrame)
            {
                qte.Tick(Time.unscaledDeltaTime);
                if (Controls.QtePress) qte.Press();
            }
            Render();
        }

        void BuildTrack(float zone, float goodHalf, float perfectHalf, int markerCount)
        {
            var track = new VisualElement
            {
                style =
                {
                    width = TrackW, height = TrackH,
                    backgroundColor = Palette.Ink1,
                    borderTopWidth = 2, borderBottomWidth = 2, borderLeftWidth = 2, borderRightWidth = 2,
                    borderTopColor = Palette.Mist2, borderBottomColor = Palette.Mist2,
                    borderLeftColor = Palette.Mist2, borderRightColor = Palette.Mist2,
                    overflow = Overflow.Visible,
                }
            };

            float cx = zone * TrackW;
            float gw = Mathf.Max(12f, goodHalf * 2f * TrackW);
            float pw = Mathf.Max(4f, perfectHalf * 2f * TrackW);

            track.Add(new VisualElement
            {
                style = { position = Position.Absolute, left = cx - gw / 2f, top = 0, width = gw, bottom = 0, backgroundColor = Palette.Hex(0x2C5A8F) }
            });
            track.Add(new VisualElement
            {
                style = { position = Position.Absolute, left = cx - pw / 2f, top = 0, width = pw, bottom = 0, backgroundColor = Palette.Gold2 }
            });

            markers.Clear();
            for (int i = 0; i < markerCount; i++)
            {
                var m = new VisualElement
                {
                    style = { position = Position.Absolute, left = 0, top = -6, width = 6, height = TrackH + 12, backgroundColor = Palette.WarmIvory }
                };
                markers.Add(m);
                track.Add(m);
            }
            host.Add(track);
        }

        void BuildTyping(TypingQte t)
        {
            var row = new VisualElement { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.Center, marginBottom = 8 } };
            letters.Clear();
            for (int i = 0; i < t.sequence.Length; i++)
            {
                var l = new UiLabel(t.sequence[i].ToString()) { style = { fontSize = 32, width = 32, unityTextAlign = TextAnchor.MiddleCenter, color = Palette.Mist3 } };
                letters.Add(l);
                row.Add(l);
            }
            host.Add(row);

            var bar = new VisualElement
            {
                style =
                {
                    width = TrackW, height = 14, backgroundColor = Palette.Ink0,
                    borderTopWidth = 2, borderBottomWidth = 2, borderLeftWidth = 2, borderRightWidth = 2,
                    borderTopColor = Palette.Ink4, borderBottomColor = Palette.Ink4, borderLeftColor = Palette.Ink4, borderRightColor = Palette.Ink4,
                }
            };
            timeFill = new VisualElement { style = { width = TrackW, height = Length.Percent(100), backgroundColor = Palette.ArcaneViolet } };
            bar.Add(timeFill);
            bar.Add(new VisualElement { style = { position = Position.Absolute, left = (1f - QteConfig.TypingPerfectFraction) * TrackW, top = -4, width = 4, height = 18, backgroundColor = Palette.Gold1 } });
            host.Add(bar);
        }

        void Render()
        {
            if (host == null) return;
            switch (qte)
            {
                case TimingQte t: RenderTiming(t); break;
                case DefenseQte d: RenderDefense(d); break;
                case TypingQte ty: RenderTyping(ty); break;
            }
        }

        void SetMarker(int i, float fraction, Color c)
        {
            if (i < 0 || i >= markers.Count) return;
            markers[i].style.display = DisplayStyle.Flex;
            markers[i].style.backgroundColor = c;
            markers[i].style.left = Mathf.Clamp(fraction * TrackW, 0f, TrackW - 6f) - 3f;
        }

        void RenderTiming(TimingQte t)
        {
            SetMarker(0, t.T / t.Sweep, Palette.WarmIvory);
            if (t.Done && !t.Cancelled) ShowResult(t.Result, "PERFECT!", "GOOD", "POOR");
        }

        void RenderDefense(DefenseQte d)
        {
            string key = Controls.QteKey.ToString().ToUpperInvariant();
            if (d.InTelegraph) infoLbl.text = "GET READY TO DEFEND...";
            else if (!defenseActiveStarted)
            {
                defenseActiveStarted = true;
                Sfx.Play(Sound.QteStart);
            }
            int next = -1;
            for (int i = 0; i < d.Hits; i++)
            {
                float e = d.T - d.SpawnTime(i), w = d.profile.windup, hold = d.delays[i];
                if (e < 0 || d.HitResult(i) != null)
                {
                    if (i < markers.Count) markers[i].style.display = DisplayStyle.None;
                    continue;
                }
                if (next < 0) next = i;
                if (d.heavy[i] && !heavyTelegraphed)
                {
                    heavyTelegraphed = true;
                    Sfx.Play(Sound.QteTelegraph);
                }
                bool holding = hold > 0 && e >= w / 2 && e < w / 2 + hold;
                float p = holding ? Impact / 2 : (e >= w / 2 + hold ? e - hold : e) / w * Impact;
                var c = holding ? (Mathf.Repeat(d.T, 0.2f) < 0.1f ? Palette.SpecCrimson : Palette.WarmIvory)
                    : d.heavy[i] ? Palette.CrimsonBright
                    : i == next ? Palette.WarmIvory : Palette.MutedGray;
                SetMarker(i, p, c);
            }
            if (!d.InTelegraph)
                infoLbl.text = (d.Hits > 1 ? $"PRESS [{key}] ON EACH HIT" : $"PRESS [{key}] AS IT LANDS") + (System.Array.IndexOf(d.heavy, true) >= 0 ? " - RED = HEAVY" : "");

            for (int i = d.Hits - 1; i >= 0; i--)
                if (d.HitResult(i) is QteResult r)
                {
                    if (i != lastResolved) { lastResolved = i; resultSfxPlayed = false; ShowResult(r, "PARRY!", "BLOCK", "HIT", true); }
                    break;
                }
        }

        void RenderTyping(TypingQte t)
        {
            if (t.Index > lastTypingIndex)
            {
                lastTypingIndex = t.Index;
                Sfx.Play(Sound.RuneType);
            }
            if (t.Mistakes > lastMistakes)
            {
                lastMistakes = t.Mistakes;
                Sfx.Play(Sound.RuneError);
            }
            for (int i = 0; i < letters.Count; i++)
                letters[i].style.color = i < t.Index ? Palette.Gold1 : i == t.Index ? Palette.WarmIvory : i >= t.wordLength ? Palette.ArcaneViolet : Palette.MutedGray;
            float left = Mathf.Clamp01(1f - t.T / t.timeLimit);
            if (timeFill != null) timeFill.style.width = left * TrackW;
            infoLbl.text = t.Mistakes > 0 ? $"MISTAKES {t.Mistakes}/{QteConfig.TypingMaxMistakes}" : "RECITE THE ANCIENT WORDS";
            infoLbl.style.color = t.Mistakes > 0 ? Palette.SpecCrimson : Palette.Mist2;
            if (t.Done && !t.Cancelled) ShowResult(t.Result, "PERFECT!", "GOOD", "POOR");
        }

        void ShowResult(QteResult r, string perfect, string good, string miss, bool defense = false)
        {
            if (!resultSfxPlayed)
            {
                resultSfxPlayed = true;
                Sfx.Play(defense
                    ? (r == QteResult.Perfect ? Sound.Parry : r == QteResult.Good ? Sound.Block : Sound.QteMiss)
                    : (r == QteResult.Perfect ? Sound.QtePerfect : r == QteResult.Good ? Sound.QteGood : Sound.QteMiss));
            }
            if (resultLbl == null) return;
            resultLbl.text = r == QteResult.Perfect ? perfect : r == QteResult.Good ? good : miss;
            resultLbl.style.color = r == QteResult.Perfect ? Palette.Gold1 : r == QteResult.Good ? Palette.Hex(0xB8D6FF) : Palette.Mist3;
        }
    }
}
