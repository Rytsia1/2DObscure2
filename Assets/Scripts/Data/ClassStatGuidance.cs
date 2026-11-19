using System;
using System.Collections.Generic;
using System.Linq;

namespace Obscure
{
    public enum AbilityPriority
    {
        Optional = 0,
        Secondary = 1,
        Primary = 2
    }

    // Per-class Ability Score guidance for Character Creation QoL (advisory & opt-in).
    public sealed class ClassStatGuide
    {
        public string classId;
        public Ability[] primaryAbilities = Array.Empty<Ability>();
        public Ability[] secondaryAbilities = Array.Empty<Ability>();
        public AbilityScores recommendedBaseScores = AbilityScores.DefaultBase;
        public string summary = "";
        public Dictionary<Ability, string> abilityReasons = new Dictionary<Ability, string>();

        public AbilityPriority GetPriority(Ability ability)
        {
            if (primaryAbilities != null && Array.IndexOf(primaryAbilities, ability) >= 0)
                return AbilityPriority.Primary;
            if (secondaryAbilities != null && Array.IndexOf(secondaryAbilities, ability) >= 0)
                return AbilityPriority.Secondary;
            return AbilityPriority.Optional;
        }

        public bool IsPrimary(Ability ability) => GetPriority(ability) == AbilityPriority.Primary;
        public bool IsSecondary(Ability ability) => GetPriority(ability) == AbilityPriority.Secondary;

        public string GetReason(Ability ability, string className = null)
        {
            if (abilityReasons != null && abilityReasons.TryGetValue(ability, out var reason) && !string.IsNullOrEmpty(reason))
                return reason;

            string cls = string.IsNullOrEmpty(className) ? "this class" : className;
            string keys = FormatKeyAbilitiesSummary();
            return ability switch
            {
                Ability.STR => $"Low priority for {cls}: mainly boosts melee Power & Break. Focus on {keys} first.",
                Ability.DEX => $"Optional for {cls}: adds Initiative and Evasion, but {keys} scale core skills directly.",
                Ability.VIT => $"Optional survivability for {cls}: adds Max HP and Physical Defense alongside {keys}.",
                Ability.INT => $"Low priority for {cls}: mainly scales arcane spells. Focus on {keys} first.",
                Ability.WIS => $"Optional for {cls}: adds Max MP and Magic Defense, though {keys} drive core output.",
                _ => $"Optional for {cls}: adds Support Power and shop utility. Focus on {keys} first."
            };
        }

        public string FormatKeyAbilitiesSummary()
        {
            var ordered = new List<Ability>();
            if (primaryAbilities != null) ordered.AddRange(primaryAbilities);
            if (secondaryAbilities != null)
            {
                foreach (var a in secondaryAbilities)
                    if (!ordered.Contains(a)) ordered.Add(a);
            }
            return ordered.Count > 0 ? string.Join(" / ", ordered) : "STR / VIT";
        }

        public string FormatSpreadSummary() =>
            string.Join(" · ", AbilityScores.AllAbilities.Select(a => $"{a} {recommendedBaseScores[a]}"));
    }

    // Authoritative class stat priority & recommended 27-point spreads derived from Database.Classes & Skills.
    public static class ClassStatGuidance
    {
        static readonly ClassStatGuide DefaultGuide = new ClassStatGuide
        {
            classId = "default",
            primaryAbilities = new[] { Ability.STR, Ability.VIT },
            secondaryAbilities = new[] { Ability.DEX },
            recommendedBaseScores = new AbilityScores(15, 12, 15, 8, 10, 11),
            summary = "Prioritize STR for physical power and VIT for frontline survivability.",
            abilityReasons = new Dictionary<Ability, string>
            {
                [Ability.STR] = "Boosts melee weapon damage and Physical Break Power.",
                [Ability.VIT] = "Increases Max HP and Physical Defense for frontline endurance.",
                [Ability.DEX] = "Improves turn Initiative, Evasion, and Critical Chance.",
            }
        };

        static readonly Dictionary<string, ClassStatGuide> Guides = BuildGuides();

        static Dictionary<string, ClassStatGuide> BuildGuides()
        {
            var fighter = new ClassStatGuide
            {
                classId = "fighter",
                primaryAbilities = new[] { Ability.STR, Ability.VIT },
                secondaryAbilities = new[] { Ability.DEX, Ability.CHA },
                recommendedBaseScores = new AbilityScores(15, 12, 15, 8, 10, 11), // 9+4+9+0+2+3 = 27
                summary = "Build STR for weapon techniques & Break, VIT for Max HP, DEF & shield skills.",
                abilityReasons = new Dictionary<Ability, string>
                {
                    [Ability.STR] = "Primary for Fighter: scales 11 weapon skills, Block Power, and Physical Break.",
                    [Ability.VIT] = "Primary for Fighter: scales shield defense skills, Max HP, and Physical Defense.",
                    [Ability.DEX] = "Secondary for Fighter: raises turn Initiative, Evasion, and Critical Chance.",
                    [Ability.CHA] = "Secondary for Fighter: strengthens defensive guard & vanguard support buffs.",
                    [Ability.INT] = "Low priority for Fighter: melee skills do not scale with INT. Focus STR and VIT.",
                    [Ability.WIS] = "Optional for Fighter: adds modest Max MP and Magic Defense."
                }
            };

            var barbarian = new ClassStatGuide
            {
                classId = "barbarian",
                primaryAbilities = new[] { Ability.STR, Ability.VIT },
                secondaryAbilities = new[] { Ability.DEX },
                recommendedBaseScores = new AbilityScores(15, 12, 15, 8, 10, 11), // 9+4+9+0+2+3 = 27
                summary = "Maximize STR for brutal melee strikes (2.5x) and VIT for massive Berserk HP.",
                abilityReasons = new Dictionary<Ability, string>
                {
                    [Ability.STR] = "Primary for Barbarian: scales all 15 melee skills (2.5x weapon scaling) and Break.",
                    [Ability.VIT] = "Primary for Barbarian: fuels the highest HP growth (12/mod) for low-HP Berserk play.",
                    [Ability.DEX] = "Secondary for Barbarian: improves turn Initiative and Critical Chance.",
                    [Ability.INT] = "Low priority for Barbarian: no Barbarian skill scales with INT. Focus STR and VIT.",
                    [Ability.WIS] = "Optional for Barbarian: offers extra Magic Defense and Status Resistance.",
                    [Ability.CHA] = "Low priority for Barbarian: prioritize STR, VIT, and DEX for bruiser output."
                }
            };

            var martialArtist = new ClassStatGuide
            {
                classId = "martial_artist",
                primaryAbilities = new[] { Ability.DEX, Ability.STR },
                secondaryAbilities = new[] { Ability.VIT, Ability.WIS },
                recommendedBaseScores = new AbilityScores(14, 15, 13, 8, 13, 9), // 7+9+5+0+5+1 = 27
                summary = "Pair DEX for fast combo skills & crits with STR for strikes and Break Power.",
                abilityReasons = new Dictionary<Ability, string>
                {
                    [Ability.DEX] = "Primary for Martial Artist: scales 8 combo skills, Initiative, Evasion, and Crits.",
                    [Ability.STR] = "Primary for Martial Artist: scales 6 heavy strike skills and Physical Break Power.",
                    [Ability.VIT] = "Secondary for Martial Artist: provides frontline HP and Physical Defense.",
                    [Ability.WIS] = "Secondary for Martial Artist: boosts Max MP, Magic Defense, and focus skills.",
                    [Ability.INT] = "Low priority for Martial Artist: unarmed combos rely on DEX and STR instead.",
                    [Ability.CHA] = "Low priority for Martial Artist: focus on DEX, STR, and VIT first."
                }
            };

            var rogue = new ClassStatGuide
            {
                classId = "rogue",
                primaryAbilities = new[] { Ability.DEX },
                secondaryAbilities = new[] { Ability.STR, Ability.VIT },
                recommendedBaseScores = new AbilityScores(14, 15, 12, 9, 12, 10), // 7+9+4+1+4+2 = 27
                summary = "Maximize DEX to act first and scale all 15 Rogue skills; add STR & VIT for melee punch.",
                abilityReasons = new Dictionary<Ability, string>
                {
                    [Ability.DEX] = "Primary for Rogue: scales all 15 Rogue skills, Initiative, Evasion, and Crits.",
                    [Ability.STR] = "Secondary for Rogue: increases base melee Weapon Power and Physical Break.",
                    [Ability.VIT] = "Secondary for Rogue: reinforces fragile HP and Physical Defense.",
                    [Ability.WIS] = "Optional for Rogue: adds Max MP and Magic Defense for longer fights.",
                    [Ability.INT] = "Low priority for Rogue: Rogue skills scale with DEX rather than INT.",
                    [Ability.CHA] = "Optional for Rogue: modest utility, lower impact than DEX and STR."
                }
            };

            var ranger = new ClassStatGuide
            {
                classId = "ranger",
                primaryAbilities = new[] { Ability.DEX },
                secondaryAbilities = new[] { Ability.WIS, Ability.VIT },
                recommendedBaseScores = new AbilityScores(12, 15, 13, 10, 13, 10), // 4+9+5+2+5+2 = 27
                summary = "Maximize DEX for all 15 ranged skills (2.5x) & Crits; add WIS for MP and VIT for safety.",
                abilityReasons = new Dictionary<Ability, string>
                {
                    [Ability.DEX] = "Primary for Ranger: scales all 15 ranged skills (2.5x Ranged Power), Initiative & Crits.",
                    [Ability.WIS] = "Secondary for Ranger: expands Max MP for sustained volleys and Magic Defense.",
                    [Ability.VIT] = "Secondary for Ranger: strengthens Max HP and Physical Defense.",
                    [Ability.STR] = "Optional for Ranger: only affects melee strikes; bows & skills scale with DEX.",
                    [Ability.INT] = "Low priority for Ranger: ranged volleys do not scale with INT.",
                    [Ability.CHA] = "Low priority for Ranger: prioritize DEX, WIS, and VIT first."
                }
            };

            var cleric = new ClassStatGuide
            {
                classId = "cleric",
                primaryAbilities = new[] { Ability.WIS },
                secondaryAbilities = new[] { Ability.VIT, Ability.CHA },
                recommendedBaseScores = new AbilityScores(10, 10, 14, 10, 15, 13), // 2+2+7+2+9+5 = 27
                summary = "Maximize WIS for healing & divine skills (2.5x) plus Max MP; add VIT & CHA for support.",
                abilityReasons = new Dictionary<Ability, string>
                {
                    [Ability.WIS] = "Primary for Cleric: scales 14 healing & divine skills (2.5x), Max MP, and Magic Defense.",
                    [Ability.VIT] = "Secondary for Cleric: keeps your healer alive with higher Max HP and Physical Defense.",
                    [Ability.CHA] = "Secondary for Cleric: amplifies Support Power (2.5x) for party blessings and wards.",
                    [Ability.INT] = "Optional for Cleric: adds general Spell Power, though core Cleric spells use WIS.",
                    [Ability.STR] = "Low priority for Cleric: only boosts basic mace swings.",
                    [Ability.DEX] = "Optional for Cleric: adds Initiative for earlier heals, behind WIS and VIT."
                }
            };

            var wizard = new ClassStatGuide
            {
                classId = "wizard",
                primaryAbilities = new[] { Ability.INT },
                secondaryAbilities = new[] { Ability.WIS, Ability.VIT },
                recommendedBaseScores = new AbilityScores(8, 12, 13, 15, 13, 12), // 0+4+5+9+5+4 = 27
                summary = "Maximize INT for all 15 arcane spells (3.0x) & Magic Break; add WIS for MP and VIT for HP.",
                abilityReasons = new Dictionary<Ability, string>
                {
                    [Ability.INT] = "Primary for Wizard: scales all 15 arcane spells (3.0x Spell Power) and Magic Break.",
                    [Ability.WIS] = "Secondary for Wizard: increases Max MP for longer casting and Magic Defense.",
                    [Ability.VIT] = "Secondary for Wizard: cushions Wizard's low base HP and Physical Defense.",
                    [Ability.DEX] = "Optional for Wizard: improves Initiative and Evasion to cast before foes act.",
                    [Ability.CHA] = "Optional for Wizard: minor support utility; prioritize INT, WIS, and VIT.",
                    [Ability.STR] = "Low priority for Wizard: melee power is rarely used. Focus INT, WIS, and VIT."
                }
            };

            var warlock = new ClassStatGuide
            {
                classId = "warlock",
                primaryAbilities = new[] { Ability.INT },
                secondaryAbilities = new[] { Ability.CHA, Ability.VIT },
                recommendedBaseScores = new AbilityScores(8, 10, 13, 15, 10, 15), // 0+2+5+9+2+9 = 27
                summary = "Maximize INT for all 15 dark spells (3.0x) and CHA for Pact support (2.5x); add VIT for HP.",
                abilityReasons = new Dictionary<Ability, string>
                {
                    [Ability.INT] = "Primary for Warlock: scales all 15 eldritch & curse spells (3.0x Spell Power) and Magic Break.",
                    [Ability.CHA] = "Secondary for Warlock: amplifies Pact Support Power (2.5x) and occult utility.",
                    [Ability.VIT] = "Secondary for Warlock: offsets fragile defenses with higher Max HP.",
                    [Ability.WIS] = "Optional for Warlock: expands Max MP and Magic Defense for sustained hexing.",
                    [Ability.DEX] = "Optional for Warlock: raises Initiative to land curses early.",
                    [Ability.STR] = "Low priority for Warlock: pact spells scale with INT, not STR."
                }
            };

            var druid = new ClassStatGuide
            {
                classId = "druid",
                primaryAbilities = new[] { Ability.WIS },
                secondaryAbilities = new[] { Ability.VIT, Ability.INT },
                recommendedBaseScores = new AbilityScores(10, 12, 14, 10, 15, 11), // 2+4+7+2+9+3 = 27
                summary = "Maximize WIS for all 15 nature & healing skills (2.5x) plus MP; add VIT & INT for balance.",
                abilityReasons = new Dictionary<Ability, string>
                {
                    [Ability.WIS] = "Primary for Druid: scales all 15 nature & restorative skills (2.5x), Max MP, and MDEF.",
                    [Ability.VIT] = "Secondary for Druid: boosts Max HP and Physical Defense for versatile survivability.",
                    [Ability.INT] = "Secondary for Druid: enhances arcane Spell Power (2.5x) and Magic Break.",
                    [Ability.DEX] = "Optional for Druid: improves Initiative and Evasion.",
                    [Ability.CHA] = "Optional for Druid: adds modest Support Power.",
                    [Ability.STR] = "Low priority for Druid: nature magic scales with WIS rather than STR."
                }
            };

            return new Dictionary<string, ClassStatGuide>(StringComparer.OrdinalIgnoreCase)
            {
                ["fighter"] = fighter,
                ["barbarian"] = barbarian,
                ["martial_artist"] = martialArtist,
                ["rogue"] = rogue,
                ["ranger"] = ranger,
                ["cleric"] = cleric,
                ["wizard"] = wizard,
                ["warlock"] = warlock,
                ["druid"] = druid,
                // Legacy non-playable class aliases
                ["knight"] = fighter,
                ["mage"] = wizard,
                ["healer"] = cleric,
            };
        }

        public static ClassStatGuide ForClass(ClassDef classDef) =>
            ForClassId(classDef?.id);

        public static ClassStatGuide ForClassId(string classId)
        {
            if (!string.IsNullOrEmpty(classId) && Guides.TryGetValue(classId, out var guide))
                return guide;
            return DefaultGuide;
        }
    }
}
