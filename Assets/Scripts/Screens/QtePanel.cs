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
                host.style.overflow = Overflow.Hidden;
                host.style.paddingLeft = 24;
                host.style.paddingRight = 24;
                host.style.paddingTop = 12;
                host.style.paddingBottom = 12;
                host.style.alignItems = UnityEngine.UIElements.Align.Center;
                host.style.justifyContent = Justify.SpaceBetween;

                headingLbl = new UiLabel(heading ?? "") { style = { fontSize = 20, color = Palette.Gold1, whiteSpace = WhiteSpace.Normal, unityTextAlign = TextAnchor.MiddleCenter } };
                host.Add(headingLbl);
                infoLbl = new UiLabel("") { style = { fontSize = 13, color = Palette.Mist2, whiteSpace = WhiteSpace.Normal, unityTextAlign = TextAnchor.MiddleCenter } };
                host.Add(infoLbl);

                switch (q)
                {
                    case TimingQte t:
                        string curveHint = t.passes > 1 ? " (DOUBLE PASS)" : t.curve == SweepCurve.EaseIn ? " (EASE-IN)" : t.curve == SweepCurve.EaseOut ? " (EASE-OUT)" : "";
                        infoLbl.text = $"PRESS [{Key}] IN THE GOLD ZONE{curveHint}";
                        BuildTrack(new[] { t.ZoneFraction }, t.EffectiveGood / t.Sweep, t.EffectivePerfect / t.Sweep, 1);
                        break;
                    case ChargeQte ch:
                        infoLbl.text = ch.isThrust ? $"THRUST: SHORT HOLD [{Key}] - RELEASE IN GOLD" : $"HOLD [{Key}] - RELEASE IN THE GOLD ZONE";
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
                        if (d.isRemix) headingLbl.text = string.IsNullOrEmpty(heading) ? "REMIX!" : $"REMIX · {heading}";
                        // Name the dodge direction from the first frame (the cue is known before the telegraph ends).
                        int arrowCount = System.Array.FindAll(d.cues, c => c != DodgeDir.None).Length;
                        var dodge = System.Array.Find(d.cues, c => c != DodgeDir.None);
                        if (arrowCount > 1 && !d.isRemix) headingLbl.text = "DODGE EACH ARROW!";
                        else if (dodge != DodgeDir.None && !d.isRemix) headingLbl.text = $"DODGE {DirName(dodge)}!";
                        for (int i = 0; i < d.Hits && i < markers.Count; i++)
                        {
                            var v = d.VariantAt(i);
                            string label = v switch
                            {
                                QteVariant.GuardCue => "GUARD",
                                QteVariant.Slip => "SLIP",
                                QteVariant.Flurry => "x2",
                                QteVariant.Roll => $"ROLL {Arrow(d.cues[i])}",
                                _ => d.cues[i] != DodgeDir.None ? Arrow(d.cues[i]) : d.heavy[i] ? "HOLD" : null,
                            };
                            if (label != null)
                            {
                                bool useGuardStyle = v != QteVariant.None;
                                Color col = d.Distortion == QteDistortion.Confusion ? Palette.WarmIvory
                                    : useGuardStyle ? Palette.Hex(0x6EB5FF)
                                    : d.heavy[i] ? Palette.CrimsonBright
                                    : Palette.Gold1;
                                markers[i].Add(new UiLabel(label)
                                {
                                    style = { position = Position.Absolute, left = -31, top = TrackH + 10, width = 68, fontSize = useGuardStyle || d.heavy[i] ? 12 : 20, color = col, unityTextAlign = TextAnchor.MiddleCenter }
                                });
                            }
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
                if (qte is DefenseQte dq)
                {
                    if (Controls.GuardPress) dq.PressGuard();
                    if (Controls.GuardReleased) dq.ReleaseGuard();
                }
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
                    marginBottom = 20,
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
            ApplyDistortionChrome();
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

        void ApplyDistortionChrome()
        {
            if (host == null || qte == null) return;
            bool inClear = false;
            if (qte is DefenseQte dq)
            {
                for (int i = 0; i < dq.Hits; i++)
                    if (dq.HitResult(i) == null && QteConfig.IsInClearZone(dq.impacts[i] - dq.T)) { inClear = true; break; }
            }
            else if (qte is TimingQte tq)
                inClear = QteConfig.IsInClearZone(tq.IdealTime() - tq.T);

            if (qte.Distortion == QteDistortion.Shock && !inClear)
                host.style.opacity = Mathf.Repeat(Time.unscaledTime, 0.35f) < 0.06f ? 0.55f : 1f;
            else if (qte.Distortion == QteDistortion.Despair)
                host.style.opacity = 0.78f;
            else
                host.style.opacity = 1f;
        }

        void SetMarker(int i, float fraction, Color c)
        {
            if (i < 0 || i >= markers.Count) return;
            markers[i].style.display = DisplayStyle.Flex;
            markers[i].style.backgroundColor = c;
            float px = Mathf.Clamp(fraction * TrackW, 0f, TrackW - 6f) - 3f;
            float shakeOffset = 0f;
            if (qte != null && qte.Distortion == QteDistortion.Fear)
            {
                bool inClear = qte is DefenseQte dq && i < dq.impacts.Length && QteConfig.IsInClearZone(dq.impacts[i] - dq.T);
                if (!inClear)
                    shakeOffset = Mathf.Sin(Time.unscaledTime * 55f + i) * QteConfig.ShakePx;
            }
            markers[i].style.left = px + shakeOffset;
        }

        void RenderTiming(TimingQte t)
        {
            SetMarker(0, t.MarkFraction, Palette.WarmIvory);
            if (t.Done && !t.Cancelled) ShowResult(t.Result, "PERFECT!", "GOOD", "POOR");
        }

        void RenderCharge(ChargeQte c)
        {
            if (chargeFill != null)
            {
                chargeFill.style.width = Mathf.Clamp(c.Fill, 0f, 1f) * (TrackW - 4f);
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
            if (chargeFill != null) chargeFill.style.width = Mathf.Clamp01(s.Gauge) * (TrackW - 4f);
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
            {
                string guardName = Controls.GuardKey.ToString().ToUpperInvariant();
                var v0 = d.Hits > 0 ? d.VariantAt(0) : QteVariant.None;
                infoLbl.text = v0 switch
                {
                    QteVariant.GuardCue => $"GET READY: PRESS [{guardName}] AS IT LANDS",
                    QteVariant.Slip => $"GET READY: HOLD [{guardName}], RELEASE AT IMPACT",
                    QteVariant.Flurry => $"GET READY: PRESS [{guardName}] TWICE AT IMPACT",
                    QteVariant.Roll => $"GET READY: {DirName(d.cues[0])} + [{guardName}] AT IMPACT",
                    _ => dodge != DodgeDir.None
                        ? $"GET READY: PRESS {DirName(dodge)} (ARROW KEY OR W/A/S/D)"
                        : anyHeavy ? $"GET READY: HOLD [{key}] THROUGH THE RED HIT"
                        : $"GET READY: PRESS [{key}] WHEN IT LANDS",
                };
            }
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
                // Name the input for the cue that comes next: Guard variants, arrow, brace, or the plain key.
                int cur = next >= 0 ? next : 0;
                string guardName = Controls.GuardKey.ToString().ToUpperInvariant();
                var curVar = d.VariantAt(cur);
                infoLbl.text = curVar switch
                {
                    QteVariant.GuardCue => $"GUARD [{guardName}] AS IT LANDS",
                    QteVariant.Slip => $"HOLD GUARD [{guardName}] - RELEASE AT IMPACT",
                    QteVariant.Flurry => $"FLURRY [{guardName}] x2 AT IMPACT",
                    QteVariant.Roll => $"ROLL: {DirName(d.cues[cur])} + GUARD [{guardName}]",
                    _ => d.cues[cur] != DodgeDir.None ? $"PRESS {DirName(d.cues[cur])} AS IT LANDS"
                        : d.heavy[cur] ? $"HOLD [{key}] UNTIL THE RED HIT LANDS"
                        : (d.Hits > 1 ? $"PRESS [{key}] ON EACH HIT" : $"PRESS [{key}] AS IT LANDS") + (anyHeavy ? " - RED = HOLD" : ""),
                };
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
            if (timeFill != null) timeFill.style.width = left * (TrackW - 4f);
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
