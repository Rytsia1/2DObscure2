using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using UiLabel = UnityEngine.UIElements.Label;

// Tutorial Prologue prompts (Assets/Docs/Tutorial_Prologue_Design.md §3). One short line at the moment a mechanic first
// matters; blocking prompts wait for Confirm before the QTE starts. Each prompt shows once per Prologue and never for
// a profile that has already cleared it (§7). Everything here is a no-op outside the Prologue.
namespace Obscure
{
    public partial class CombatView
    {
        UiLabel coachLabel;

        static PrologueScript Tut => Game.I?.Run?.Tutorial;
        static bool Coaching => Tut != null && !(Game.I?.Settings?.prologueCleared ?? false);
        static string QteKeyName => Controls.QteKey.ToString().ToUpperInvariant();

        void InitCoach(Battle battle)
        {
            if (!Coaching) return;
            coachLabel = new UiLabel("")
            {
                style =
                {
                    position = Position.Absolute, left = 120, top = 88, width = 1040,
                    paddingLeft = 12, paddingRight = 12, paddingTop = 6, paddingBottom = 6,
                    fontSize = 15, color = Palette.Gold1, backgroundColor = Palette.Hex(0x070A14, 220),
                    unityTextAlign = TextAnchor.MiddleCenter, whiteSpace = WhiteSpace.Normal, display = DisplayStyle.None,
                },
            };
            screenRoot.Add(coachLabel);

            battle.Logged += line =>
            {
                if (line.Contains("weakness!") || line.Contains("- resisted"))
                    Tip("affinity", "WEAK takes 1.5x damage, RESISTED takes 0.5x. Check an enemy's dossier before you strike.");
                else if (line.Contains("charge is interrupted"))
                    Tip("interrupt", "INTERRUPTED! A Broken enemy drops its charged attack.");
                else if (line.Contains("Aetherion opens the rift"))
                    Tip("phase2", "Phase 2: Rift Pulse hits the whole party. One ward protects everyone it hits.");
                else if (line.StartsWith("Stalker") && line.EndsWith("is destroyed.") && Tut != null && !Tut.FearScripted)
                    Tip("intent_win", "You stopped it before it acted. Reading intent pays.");
            };
            battle.StatusApplied += (target, id, isBuff) =>
            {
                if (id == StatusId.Fear && target.IsPlayer)
                    Tip("fear", $"{target.name} is afraid: FEAR lowers damage dealt for a few turns. Statuses show as icons on the hero card.");
            };
        }

        // Non-blocking: stays up until the next prompt.
        void Tip(string key, string text)
        {
            if (!Coaching || coachLabel == null || !Tut.FirstTime("tip:" + key)) return;
            coachLabel.text = text.ToUpperInvariant();
            coachLabel.style.display = DisplayStyle.Flex;
            AddLog("TIP: " + text);
        }

        // Blocking: the fight waits for Confirm, so the player reads it before the bar moves.
        IEnumerator Coach(string key, string text)
        {
            if (!Coaching || coachLabel == null || !Tut.FirstTime("tip:" + key)) yield break;
            coachLabel.text = (text + "   ▸ CONFIRM").ToUpperInvariant();
            coachLabel.style.display = DisplayStyle.Flex;
            AddLog("TIP: " + text);
            yield return null;
            while (!Controls.Confirm) yield return null;
            yield return null; // the confirming press never reaches the QTE (edge-triggered, plus its grace period)
            coachLabel.style.display = DisplayStyle.None;
        }

        IEnumerator CoachBattleStart()
        {
            if (!Coaching) yield break;
            if (Tut.Wipes >= Prologue.AssistOfferAfterWipes)
                yield return Coach("assist" + Tut.Floor, "The Threshold sent you back. If the timing feels too tight, Settings has QTE Assist (Relaxed or Auto).");
            switch (Tut.Floor)
            {
                case 0: yield return Coach("turns", "The bar at the top is the turn order: units act left to right, faster units first."); break;
                case 1: yield return Coach("intent", "Icons over an enemy show a dangerous move before it happens, and who it targets."); break;
                default: yield return Coach("break", "Hits wear down the BREAK gauge. At zero the enemy is Broken: it loses its next action and takes more damage."); break;
            }
        }

        IEnumerator CoachPlayerTurn(Combatant c)
        {
            if (!Coaching) yield break;
            yield return Coach("menu", "Your turn: choose ATTACK or a SKILL, then a target. Skills cost MP.");

            var threat = Battle.Enemies.FirstOrDefault(e => e.Alive && (e.Charge ?? e.Intent) != null && (e.Charge ?? e.Intent).move.HasHeavy);
            var plan = threat == null ? null : threat.Charge ?? threat.Intent;
            if (plan != null && plan.move.telegraph)
            {
                string who = plan.target?.name ?? "the party";
                Tip("charge", $"{threat.name} charges {plan.move.name} at {who}. Break it to cancel ({threat.CurrentBreakGauge} Break left), or have {who} GUARD.");
            }
            if (plan != null && plan.target == c)
                yield return Coach("guard", $"{c.name} is the target. GUARD halves the damage; Guard plus a Perfect fully parries normal hits.");

            if (c.unit.hp < c.Stats.maxHp * 0.35f && Battle.Items.Count > 0)
                Tip("item", "Low HP: ITEM > Draught restores 40 HP. HP carries over between fights.");
        }

        IEnumerator CoachOffense() =>
            Coach("offense", $"Attack QTE: press {QteKeyName} in the bracket. PERFECT hits harder, breaks more and refunds 1 MP.");

        IEnumerator CoachDefense(BattleAction act, Qte qte)
        {
            if (qte is DefenseQte dq && Battle?.run != null && !Battle.run.IsPrologue)
            {
                string guardName = Controls.GuardKey.ToString().ToUpperInvariant();
                for (int i = 0; i < dq.Hits; i++)
                {
                    var v = dq.VariantAt(i);
                    if (v == QteVariant.None) continue;
                    string vText = v switch
                    {
                        QteVariant.GuardCue => $"GUARD CUE (blue shield): press {guardName} as it lands.",
                        QteVariant.Slip => $"SLIP (notched shield): hold {guardName} when it appears, then RELEASE at impact.",
                        QteVariant.Flurry => $"FLURRY (two pips): press {guardName} twice — at impact and again right after.",
                        QteVariant.Roll => $"ROLL: press the shown arrow AND {guardName} together at impact.",
                        _ => null,
                    };
                    if (vText != null)
                    {
                        yield return Coach("var_" + v, vText);
                        break;
                    }
                }
            }
            if (!Coaching) yield break;
            bool feint = qte is DefenseQte d && Enumerable.Range(0, d.Hits).Any(d.IsFeint);
            string kind = feint ? "feint" : PrologueScript.DefenseKind(act.attack, act.attack?.hits ?? 1);
            string text = kind switch
            {
                "arrow" => "Dodge: press the arrow shown as the cue reaches the mark.",
                "feint" => "Some cues fake you out. Wait for the real one before pressing.",
                "ward" => "Magic is warded: type the runes shown before it lands.",
                "shared_ward" => "One ward protects everyone this attack hits.",
                "collapse" => $"An arrow chain, then a HEAVY final hit: hold {QteKeyName} through that impact. Heavy hits can't be fully parried.",
                "brace" => $"HEAVY: hold {QteKeyName} through the impact.",
                "chain" => "Each hit has its own cue.",
                "struggle" => $"Mash {QteKeyName} to fill the gauge before it lands.",
                _ => null,
            };
            if (text != null) yield return Coach("def_" + kind, text);
        }
    }
}
