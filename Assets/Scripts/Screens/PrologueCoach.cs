using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using UiLabel = UnityEngine.UIElements.Label;

// Tutorial Prologue prompts & visual indicators (Assets/Docs/Tutorial_Prologue_Design.md §3).
// One short line at the moment a mechanic first matters, paired with a pulsing highlight on the relevant HUD region.
// Everything here is a no-op outside the Prologue (except QTE variant tips).
namespace Obscure
{
    public partial class CombatView
    {
        UiLabel coachLabel;
        VisualElement coachHighlight;
        int coachTipVersion;
        static readonly System.Collections.Generic.HashSet<string> shownVariantTips = new();

        static PrologueScript Tut => Game.I?.Run?.Tutorial;
        static bool Coaching => Tut != null;
        static bool BlockingPrompts => Coaching && !(Game.I?.Settings?.prologueCleared ?? false);
        static string QteKeyName => Controls.QteKey.ToString().ToUpperInvariant();
        static string GuardKeyName => Controls.GuardKey.ToString().ToUpperInvariant();

        void InitCoach(Battle battle)
        {
            coachHighlight = new VisualElement
            {
                pickingMode = PickingMode.Ignore,
                style =
                {
                    position = Position.Absolute,
                    display = DisplayStyle.None,
                    borderTopWidth = 2, borderBottomWidth = 2, borderLeftWidth = 2, borderRightWidth = 2,
                    borderTopColor = Palette.Gold1, borderBottomColor = Palette.Gold1,
                    borderLeftColor = Palette.Gold1, borderRightColor = Palette.Gold1,
                    backgroundColor = Palette.Hex(0xF6D365, 22),
                },
            };
            screenRoot.Add(coachHighlight);

            coachLabel = new UiLabel("")
            {
                pickingMode = PickingMode.Ignore,
                style =
                {
                    position = Position.Absolute, left = 100, top = 432, width = 1080,
                    paddingLeft = 14, paddingRight = 14, paddingTop = 6, paddingBottom = 6,
                    fontSize = 14, color = Palette.Gold1, backgroundColor = Palette.Hex(0x070A14, 234),
                    borderTopWidth = 1, borderBottomWidth = 1, borderLeftWidth = 1, borderRightWidth = 1,
                    borderTopColor = Palette.Gold2, borderBottomColor = Palette.Gold2,
                    borderLeftColor = Palette.Gold2, borderRightColor = Palette.Gold2,
                    unityTextAlign = TextAnchor.MiddleCenter, whiteSpace = WhiteSpace.Normal, display = DisplayStyle.None,
                },
            };
            screenRoot.Add(coachLabel);
            if (!Coaching) return;

            PrologueBanner.AttachStepBadge(screenRoot);

            battle.Logged += line =>
            {
                if (line.Contains("weakness!") || line.Contains("- resisted"))
                    Tip("affinity", "WEAK takes 1.5x damage, RESISTED takes 0.5x. Open ANALYSIS (free, no turn) to inspect weaknesses.");
                else if (line.Contains("charge is interrupted"))
                    Tip("interrupt", "INTERRUPTED! A Broken enemy drops its charged attack and takes bonus damage.");
                else if (line.Contains("Aetherion opens the rift"))
                    Tip("phase2", "Phase 2: Rift Pulse hits the whole party. One ward protects everyone it hits.");
                else if (line.Contains("Bone Mending"))
                    Tip("healer_heal", "Ossuary Acolyte just healed an ally — focus down support enemies first!", "enemies");
                else if (line.StartsWith("Stalker") && line.EndsWith("is destroyed.") && Tut != null && !Tut.FearScripted)
                    Tip("intent_win", "You stopped it before it acted. Reading intent pays.");

                // Lesson B: a single hero falling does not end the fight or run.
                var downHero = battle.Party.FirstOrDefault(p => !p.Alive);
                if (downHero != null && battle.Party.Any(p => p.Alive))
                    Tip("hero_down", $"{downHero.name} is DOWN! As long as one hero stands, the fight continues — revive them or win to recover.", "party");
            };
            battle.StatusApplied += (target, id, isBuff) =>
            {
                // Lesson D: connect status icons & popups to buff/debuff meaning.
                if (id == StatusId.Fear && target.IsPlayer)
                    Tip("fear", $"{target.name} is afraid: FEAR lowers damage dealt for a few turns. Statuses show as icons on the hero card.", "party");
                else
                    Tip("status_visual", "Status icons & popups on a unit card show active buffs (blue/gold) or debuffs (red/purple).");
            };
        }

        // Non-blocking: stays up until replaced or auto-hides after 7 seconds.
        void Tip(string key, string text, string highlightArea = null)
        {
            if (!Coaching || coachLabel == null || !Tut.FirstTime("tip:" + key)) return;
            ShowBannerText(text, highlightArea);
            AddLog("TIP: " + text);
            StartCoroutine(AutoHideTip(++coachTipVersion));
        }

        void ShowVariantTip(string key, string text)
        {
            if (coachLabel == null || !shownVariantTips.Add(key)) return;
            ShowBannerText(text, null);
            AddLog("TIP: " + text);
            StartCoroutine(AutoHideTip(++coachTipVersion));
        }

        void ShowBannerText(string text, string highlightArea)
        {
            coachLabel.text = text.ToUpperInvariant();
            coachLabel.style.display = DisplayStyle.Flex;
            ApplyHighlight(highlightArea);
        }

        void ApplyHighlight(string area)
        {
            if (coachHighlight == null) return;
            Rect? rect = area switch
            {
                "turns" => ElementRect(turnBar) ?? new Rect(380, 10, 880, 44),
                "menu" => new Rect(666, 484, 256, 228),
                "enemies" => new Rect(620, 86, 620, 336),
                "boss_break" => ElementRect(bossBrkGauge) ?? new Rect(220, 44, 840, 42),
                "party" => new Rect(14, 484, 648, 226),
                "qte" => new Rect(670, 484, 604, 232),
                _ => null,
            };
            if (!rect.HasValue)
            {
                coachHighlight.style.display = DisplayStyle.None;
                return;
            }
            var r = rect.Value;
            coachHighlight.style.left = r.x;
            coachHighlight.style.top = r.y;
            coachHighlight.style.width = r.width;
            coachHighlight.style.height = r.height;
            coachHighlight.style.display = DisplayStyle.Flex;
        }

        static Rect? ElementRect(VisualElement el)
        {
            if (el == null) return null;
            var wb = el.worldBound;
            if (wb.width <= 1f || wb.height <= 1f) return null;
            return new Rect(wb.x - 4f, wb.y - 4f, wb.width + 8f, wb.height + 8f);
        }

        void HideCoachBanner()
        {
            if (coachLabel != null) coachLabel.style.display = DisplayStyle.None;
            if (coachHighlight != null) coachHighlight.style.display = DisplayStyle.None;
        }

        IEnumerator AutoHideTip(int token)
        {
            yield return new WaitForSeconds(7f);
            if (coachTipVersion == token)
                HideCoachBanner();
        }

        // Blocking when BlockingPrompts is true: waits for Confirm before the QTE starts.
        IEnumerator Coach(string key, string text, string highlightArea = null)
        {
            if (coachLabel == null) yield break;
            if (key.StartsWith("var_"))
            {
                if (Tut != null ? !Tut.FirstTime("tip:" + key) : !shownVariantTips.Add(key)) yield break;
            }
            else if (!Coaching || !Tut.FirstTime("tip:" + key)) yield break;

            int token = ++coachTipVersion;
            AddLog("TIP: " + text);
            if (!BlockingPrompts)
            {
                ShowBannerText(text, highlightArea);
                StartCoroutine(AutoHideTip(token));
                yield break;
            }

            ShowBannerText($"{text}   ▸ CONFIRM [ENTER / {QteKeyName}]", highlightArea);
            yield return null;
            while (!Controls.Confirm) yield return null;
            yield return null; // the confirming press never reaches the QTE (edge-triggered, plus its grace period)
            HideCoachBanner();
        }

        IEnumerator CoachBattleStart()
        {
            if (!Coaching) yield break;
            if (Tut.Wipes >= Prologue.AssistOfferAfterWipes)
                yield return Coach("assist" + Tut.Floor, "The Threshold sent you back. If the timing feels too tight, Settings has QTE Assist (Relaxed or Auto).");
            switch (Tut.Floor)
            {
                case 0:
                    yield return Coach("turns", "Turn Order at the top shows who acts next (left to right, fastest first).", "turns");
                    break;
                case 2:
                    // Lesson E: reading enemy cards + healer priority.
                    yield return Coach("intent", "Read enemy cards for HP, gold BREAK bar & intent icons. Ossuary Acolyte heals allies — focus it down first!", "enemies");
                    break;
                default:
                    yield return Coach("break", "Hits wear down the gold BREAK gauge. Breaking an enemy while it charges CANCELS its big attack and boosts damage taken.", "boss_break");
                    break;
            }
        }

        IEnumerator CoachPlayerTurn(Combatant c)
        {
            if (!Coaching) yield break;
            // Lesson C: HP, MP, Break gauge & resources.
            yield return Coach("menu", "Choose ATTACK or a SKILL. HP carries between fights; Skills cost MP (Basic Attack & PERFECT QTEs restore MP); hits drain BREAK.", "menu");

            if (Tut.Floor == 2)
                Tip("analysis", "Use ANALYSIS in the command menu (free, no turn spent) to inspect full enemy HP, Break & elemental weaknesses.", "menu");

            var threat = Battle.Enemies.FirstOrDefault(e => e.Alive && (e.Charge ?? e.Intent) != null && (e.Charge ?? e.Intent).move.HasHeavy);
            var plan = threat == null ? null : threat.Charge ?? threat.Intent;
            if (plan != null && plan.move.telegraph)
            {
                string who = plan.target?.name ?? "the party";
                Tip("charge", $"{threat.name} charges {plan.move.name} at {who}. Break it to cancel ({threat.CurrentBreakGauge} Break left), or have {who} GUARD.", "enemies");
            }
            if (plan != null && plan.target == c)
                yield return Coach("guard", $"{c.name} is the target. GUARD halves the damage; Guard plus a Perfect fully parries normal hits.", "menu");

            if (c.unit.hp < c.Stats.maxHp * 0.35f && Battle.Items.Count > 0)
                Tip("item", "Low HP: ITEM > Draught restores 40 HP. HP carries over between fights.", "party");
            else if (c.unit.mp < 10)
                Tip("low_mp", "Low MP: Basic ATTACK (and PERFECT QTEs) restore MP, or use an Ether from ITEM.", "party");
        }

        // Lesson A: Attack QTE vs Defensive QTE.
        IEnumerator CoachOffense() =>
            Coach("offense", $"ATTACK QTE (offense): press {QteKeyName} in the bracket — boosts damage, Break & refunds 1 MP on PERFECT.", "qte");

        IEnumerator CoachDefense(BattleAction act, Qte qte)
        {
            if (qte is DefenseQte dq && Battle?.run != null && !Battle.run.IsPrologue)
            {
                for (int i = 0; i < dq.Hits; i++)
                {
                    var v = dq.VariantAt(i);
                    if (v == QteVariant.None) continue;
                    string vText = v switch
                    {
                        QteVariant.GuardCue => $"GUARD CUE (blue shield): press {GuardKeyName} as it lands.",
                        QteVariant.Slip => $"SLIP (notched shield): hold {GuardKeyName} when it appears, then RELEASE at impact.",
                        QteVariant.Flurry => $"FLURRY (two pips): press {GuardKeyName} twice — at impact and again right after.",
                        QteVariant.Roll => $"ROLL: press the shown arrow AND {GuardKeyName} together at impact.",
                        _ => null,
                    };
                    if (vText != null)
                    {
                        ShowVariantTip("var_" + v, vText);
                        break;
                    }
                }
            }
            if (!Coaching) yield break;
            bool feint = qte is DefenseQte d && Enumerable.Range(0, d.Hits).Any(d.IsFeint);
            string kind = feint ? "feint" : PrologueScript.DefenseKind(act.attack, act.attack?.hits ?? 1);
            string text = kind switch
            {
                "arrow" => "DEFENSIVE QTE (defense): press the arrow shown as the cue reaches the mark to dodge incoming damage.",
                "feint" => "Feint cue: some cues fake you out — wait for the real arrow before pressing.",
                "ward" => "DEFENSIVE QTE (magic ward): type the runes shown before the spell lands.",
                "shared_ward" => "DEFENSIVE QTE (shared ward): one ward protects everyone this attack hits.",
                "collapse" => $"An arrow chain, then a HEAVY final hit: hold {QteKeyName} through that impact. Heavy hits can't be fully parried.",
                "brace" => $"HEAVY hit: hold {QteKeyName} through the impact.",
                "chain" => "Multi-hit attack: each hit has its own defensive cue.",
                "struggle" => $"Drain attack: mash {QteKeyName} to fill the gauge before it lands.",
                _ => null,
            };
            if (text != null) yield return Coach("def_" + kind, text, "qte");
        }
    }
}
