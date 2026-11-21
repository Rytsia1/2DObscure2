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
        UiLabel headingLbl, infoLbl, resultLbl;
        VisualElement host, timeFill, chargeFill;
        readonly List<VisualElement> markers = new List<VisualElement>();
        readonly List<(VisualElement good, VisualElement perfect)> zoneEls = new List<(VisualElement, VisualElement)>();
        float zoneGw, zonePw;
        readonly List<UiLabel> letters = new List<UiLabel>();

        public Qte Qte => qte;
        // Label for a non-Heavy defensive Perfect. Only a Guarding, un-Broken defender truly parries (Battle.Deal).
        public string perfectDefense = "PARRY!";

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

                headingLbl = new UiLabel(heading ?? "") { style = { fontSize = 22, color = Palette.Gold1 } };
                host.Add(headingLbl);
                infoLbl = new UiLabel("") { style = { fontSize = 14, color = Palette.Mist2 } };
                host.Add(infoLbl);

                switch (q)
                {
                    case TimingQte t:
                        infoLbl.text = $"PRESS [{Key}] IN THE GOLD ZONE";
                        BuildTrack(new[] { t.target / t.Sweep }, t.EffectiveGood / t.Sweep, t.EffectivePerfect / t.Sweep, 1);
                        break;
                    case ChargeQte ch:
                        infoLbl.text = $"HOLD [{Key}] - RELEASE IN THE GOLD ZONE";
                        var chTrack = BuildTrack(new[] { ch.ZoneFraction }, ch.EffectiveGood / ch.profile.sweep, ch.EffectivePerfect / ch.profile.sweep, 1);
                        chargeFill = new VisualElement { style = { position = Position.Absolute, left = 0, top = 0, bottom = 0, width = 0, backgroundColor = new Color(Palette.WarmIvory.r, Palette.WarmIvory.g, Palette.WarmIvory.b, 0.3f) } };
                        chTrack.Insert(0, chargeFill); // zones draw over the fill
                        break;
                    case MovingTargetQte mv:
                        infoLbl.text = $"PRESS [{Key}] WHEN THE MARK HITS GOLD";
                        BuildTrack(new[] { mv.ZoneFraction }, mv.EffectiveGood / mv.profile.sweep, mv.EffectivePerfect / mv.profile.sweep, 1);
                        break;
                    case ComboQte co:
                        infoLbl.text = $"PRESS [{Key}] ON EACH ZONE ({co.Zones} HITS)";
                        var zones = new float[co.Zones];
                        for (int i = 0; i < zones.Length; i++) zones[i] = co.ZoneFraction(i);
                        BuildTrack(zones, QteConfig.ScaleGood(co.profile, co.TimingBonusFraction) / co.duration,
                            QteConfig.ScalePerfect(co.profile, co.TimingBonusFraction) / co.duration, 1);
                        break;
                    case DefenseQte d:
                        float speed = Impact / d.profile.windup;
                        float goodScaled = QteConfig.ScaleGood(d.profile, d.TimingBonusFraction);
                        float perfScaled = QteConfig.ScalePerfect(d.profile, d.TimingBonusFraction);
                        BuildTrack(new[] { Impact }, goodScaled * speed, perfScaled * speed, d.Hits);
                        // Name the dodge direction from the first frame (the cue is known before the telegraph ends).
                        int arrowCount = System.Array.FindAll(d.cues, c => c != DodgeDir.None).Length;
                        var dodge = System.Array.Find(d.cues, c => c != DodgeDir.None);
                        if (arrowCount > 1) headingLbl.text = "DODGE EACH ARROW!";
                        else if (dodge != DodgeDir.None) headingLbl.text = $"DODGE {DirName(dodge)}!";
                        for (int i = 0; i < d.Hits && i < markers.Count; i++)
                        {
                            // rides just below the track, clear of the instruction text above it
                            if (d.cues[i] != DodgeDir.None)
                                markers[i].Add(new UiLabel(Arrow(d.cues[i]))
                                {
                                    style = { position = Position.Absolute, left = -11, top = TrackH + 14, width = 28, fontSize = 22, color = Palette.Gold1, unityTextAlign = TextAnchor.MiddleCenter }
                                });
                            else if (d.heavy[i])
                                markers[i].Add(new UiLabel("HOLD")
                                {
                                    style = { position = Position.Absolute, left = -22, top = TrackH + 16, width = 50, fontSize = 14, color = Palette.CrimsonBright, unityTextAlign = TextAnchor.MiddleCenter }
                                });
                        }
                        break;
                    case StruggleQte st:
                        headingLbl.text = "BREAK FREE!";
                        var stTrack = BuildTrack(new float[0], 0f, 0f, 0);
                        chargeFill = new VisualElement { style = { position = Position.Absolute, left = 0, top = 0, bottom = 0, width = 0, backgroundColor = Palette.Gold2 } };
                        stTrack.Add(chargeFill);
                        stTrack.Add(new VisualElement { style = { position = Position.Absolute, left = QteConfig.StruggleGood * TrackW - 2f, top = -6, width = 4, height = TrackH + 12, backgroundColor = Palette.Hex(0xB8D6FF) } });
                        break;
                    case WardQte w:
                        BuildTyping(w);
                        break;
                    case TypingQte ty:
                        BuildTyping(ty);
                        break;
                    default:
                        infoLbl.text = q.Result == QteResult.Poor ? "CAN'T REACT - TAKES THE FULL HIT" : "QTE ASSIST: AUTO";
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
                if (Controls.QteReleased) qte.Release();
                if (Controls.Up) qte.PressDirection(DodgeDir.Up);
                else if (Controls.Down) qte.PressDirection(DodgeDir.Down);
                else if (Controls.Left) qte.PressDirection(DodgeDir.Left);
                else if (Controls.Right) qte.PressDirection(DodgeDir.Right);
            }
            Render();
        }

        static string Key => Controls.QteKey.ToString().ToUpperInvariant();

        static string Arrow(DodgeDir d) => d switch
        {
            DodgeDir.Up => "▲",
            DodgeDir.Down => "▼",
            DodgeDir.Left => "◀",
            DodgeDir.Right => "▶",
            _ => "",
        };

        static string DirName(DodgeDir d) => d == DodgeDir.None ? $"[{Key}]" : $"{d.ToString().ToUpperInvariant()} {Arrow(d)}";

        VisualElement BuildTrack(float[] zones, float goodHalf, float perfectHalf, int markerCount)
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

            zoneGw = Mathf.Max(12f, goodHalf * 2f * TrackW);
            zonePw = Mathf.Max(4f, perfectHalf * 2f * TrackW);
            zoneEls.Clear();
            foreach (float zone in zones)
            {
                float cx = zone * TrackW;
                var good = new VisualElement
                {
                    style = { position = Position.Absolute, left = cx - zoneGw / 2f, top = 0, width = zoneGw, bottom = 0, backgroundColor = Palette.Hex(0x2C5A8F) }
                };
                var perfect = new VisualElement
                {
                    style = { position = Position.Absolute, left = cx - zonePw / 2f, top = 0, width = zonePw, bottom = 0, backgroundColor = Palette.Gold2 }
                };
                track.Add(good);
                track.Add(perfect);
                zoneEls.Add((good, perfect));
            }

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
            return track;
        }

        void MoveZone(int i, float fraction)
        {
            if (i < 0 || i >= zoneEls.Count) return;
            float cx = fraction * TrackW;
            zoneEls[i].good.style.left = cx - zoneGw / 2f;
            zoneEls[i].perfect.style.left = cx - zonePw / 2f;
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
                case ChargeQte ch: RenderCharge(ch); break;
                case MovingTargetQte mv: RenderMoving(mv); break;
                case ComboQte co: RenderCombo(co); break;
                case DefenseQte d: RenderDefense(d); break;
                case StruggleQte st: RenderStruggle(st); break;
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

        void RenderCharge(ChargeQte c)
        {
            if (chargeFill != null)
            {
                chargeFill.style.width = c.Fill * TrackW;
                bool overcharging = c.Hold > c.target + c.EffectiveGood; // past the zone: about to overcharge
                chargeFill.style.backgroundColor = overcharging
                    ? new Color(Palette.SpecCrimson.r, Palette.SpecCrimson.g, Palette.SpecCrimson.b, 0.45f)
                    : new Color(Palette.WarmIvory.r, Palette.WarmIvory.g, Palette.WarmIvory.b, 0.3f);
            }
            SetMarker(0, c.Fill, c.Charging ? Palette.WarmIvory : Palette.MutedGray);
            if (c.Done && !c.Cancelled) ShowResult(c.Result, "PERFECT!", "GOOD", c.Fill >= 1f ? "OVERCHARGED" : "POOR");
        }

        void RenderMoving(MovingTargetQte m)
        {
            if (!m.Done) MoveZone(0, m.ZoneFraction); // freeze where it was pressed
            SetMarker(0, m.MarkFraction, Palette.WarmIvory);
            if (m.Done && !m.Cancelled) ShowResult(m.Result, "PERFECT!", "GOOD", "POOR");
        }

        void RenderCombo(ComboQte c)
        {
            SetMarker(0, c.MarkFraction, Palette.WarmIvory);
            for (int i = 0; i < c.Zones && i < zoneEls.Count; i++)
            {
                if (!(c.ZoneResult(i) is QteResult r)) continue;
                var tint = r == QteResult.Perfect ? Palette.Gold1 : r == QteResult.Good ? Palette.Hex(0xB8D6FF) : Palette.MutedGray;
                zoneEls[i].good.style.backgroundColor = tint;
                zoneEls[i].perfect.style.backgroundColor = tint;
            }
            for (int i = c.Zones - 1; i >= 0; i--)
                if (c.ZoneResult(i) is QteResult r)
                {
                    if (i != lastResolved) { lastResolved = i; resultSfxPlayed = false; ShowResult(r, "PERFECT!", "GOOD", "POOR"); }
                    break;
                }
        }

        void RenderStruggle(StruggleQte s)
        {
            if (chargeFill != null) chargeFill.style.width = s.Gauge * TrackW;
            if (s.InTelegraph) infoLbl.text = $"GET READY: MASH [{Key}] TO RESIST THE SIPHON";
            else if (!defenseActiveStarted)
            {
                defenseActiveStarted = true;
                Sfx.Play(Sound.QteStart);
            }
            if (!s.InTelegraph && !s.Done) infoLbl.text = $"MASH [{Key}]! FILL THE BAR ({s.TimeLeft:0.0}s)";
            if (s.Done && !s.Cancelled) ShowResult(s.Result, "BROKE FREE!", "RESISTED", "DRAINED", true);
        }

        void RenderDefense(DefenseQte d)
        {
            string key = Controls.QteKey.ToString().ToUpperInvariant();
            var dodge = System.Array.Find(d.cues, c => c != DodgeDir.None);
            bool anyHeavy = System.Array.IndexOf(d.heavy, true) >= 0;
            if (d.InTelegraph)
                infoLbl.text = dodge != DodgeDir.None
                    ? $"GET READY: PRESS {DirName(dodge)} (ARROW KEY OR W/A/S/D)"
                    : anyHeavy ? $"GET READY: HOLD [{key}] THROUGH THE RED HIT"
                    : $"GET READY: PRESS [{key}] WHEN IT LANDS";
            else if (!defenseActiveStarted)
            {
                defenseActiveStarted = true;
                Sfx.Play(Sound.QteStart);
            }
            int next = -1;
            for (int i = 0; i < d.Hits; i++)
            {
                // Feint: the fake cue runs in like a real one, stops short, then slides back before the real cue comes.
                if (d.IsFeint(i) && d.HitResult(i) == null)
                {
                    float fe = d.T - d.FeintStart(i), stop = d.profile.windup * QteConfig.FeintStop;
                    if (fe >= 0f && fe < d.FeintTime)
                    {
                        if (next < 0) next = i;
                        float fp = fe < stop
                            ? fe / d.profile.windup * Impact
                            : Impact * QteConfig.FeintStop * (1f - (fe - stop) / QteConfig.FeintRecoil);
                        SetMarker(i, fp, Palette.WarmIvory);
                        if (fe >= stop && resultLbl != null && resultLbl.text != "FEINT!")
                        {
                            resultLbl.text = "FEINT!";
                            resultLbl.style.color = Palette.SpecCrimson;
                        }
                        continue;
                    }
                }
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
                float run = holding ? 0.5f : (e >= w / 2 + hold ? e - hold : e) / w;
                float p = d.CueProgress(i, run) * Impact; // stutter cues change speed; impact time is unchanged
                var c = holding ? (Mathf.Repeat(d.T, 0.2f) < 0.1f ? Palette.SpecCrimson : Palette.WarmIvory)
                    : d.heavy[i] ? Palette.CrimsonBright
                    : i == next ? Palette.WarmIvory : Palette.MutedGray;
                SetMarker(i, p, c);
            }
            if (!d.InTelegraph)
            {
                // Name the input for the cue that comes next: arrow, brace, or the plain key.
                int cur = next >= 0 ? next : 0;
                infoLbl.text = d.cues[cur] != DodgeDir.None ? $"PRESS {DirName(d.cues[cur])} AS IT LANDS"
                    : d.heavy[cur] ? $"HOLD [{key}] UNTIL THE RED HIT LANDS"
                    : (d.Hits > 1 ? $"PRESS [{key}] ON EACH HIT" : $"PRESS [{key}] AS IT LANDS") + (anyHeavy ? " - RED = HOLD" : "");
            }

            for (int i = d.Hits - 1; i >= 0; i--)
                if (d.HitResult(i) is QteResult r)
                {
                    string miss = d.WrongInput(i) ? $"WRONG KEY: {DirName(d.cues[i])}" : d.LetGo(i) ? "LET GO TOO SOON" : "HIT";
                    if (i != lastResolved) { lastResolved = i; resultSfxPlayed = false; ShowResult(r, d.heavy[i] ? "BRACED!" : perfectDefense, "BLOCK", miss, true); }
                    else if (d.LetGo(i) && resultLbl != null && resultLbl.text != miss) { resultSfxPlayed = false; ShowResult(r, "BRACED!", "BLOCK", miss, true); }
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
            var ward = t as WardQte;
            infoLbl.text = t.Mistakes > 0 ? $"WRONG LETTER - MISTAKES {t.Mistakes}/{QteConfig.TypingMaxMistakes}"
                : ward == null ? "RECITE THE ANCIENT WORDS"
                : ward.InLead ? "INCOMING SPELL: TYPE THESE LETTERS ON YOUR KEYBOARD"
                : "TYPE THE LETTERS BEFORE THE BAR EMPTIES";
            infoLbl.style.color = t.Mistakes > 0 ? Palette.SpecCrimson : Palette.Mist2;
            if (t.Done && !t.Cancelled)
            {
                if (ward != null) ShowResult(t.Result, "WARDED!", "DEFLECT", t.Mistakes >= QteConfig.TypingMaxMistakes ? "WARD BROKEN" : "TOO SLOW", true);
                else ShowResult(t.Result, "PERFECT!", "GOOD", "POOR");
            }
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
