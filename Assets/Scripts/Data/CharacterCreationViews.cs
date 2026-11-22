using System;
using System.Collections.Generic;
using System.Linq;

namespace Obscure
{
    public struct CharacterSheetGearRow
    {
        public EquipmentSlot slot;
        public string slotName;
        public string itemName;
        public string summary;
    }

    public struct CharacterSheetSkillRow
    {
        public string skillId;
        public string name;
        public int rank;
    }

    // Read-only full hero sheet built from CharacterCreationSession (F7: Character_Creation_Audit_Handoff.md).
    // Uses Session.FinalScores and Session.PreviewDerivedStats so every value matches the battle Unit.
    public sealed class CharacterSheetView
    {
        public string heroName;
        public string raceName;
        public string racialBonusSummary;
        public string lineageSummary;
        public string className;
        public string classRole;
        public string backgroundName;

        public AbilityScores baseScores;
        public AbilityScores racialModifiers;
        public AbilityScores equipmentBonuses;
        public AbilityScores finalScores;
        public AbilityScores abilityModifiers;
        public DerivedStats derived;
        public Affinities affinities;

        public readonly List<CharacterSheetGearRow> gear = new List<CharacterSheetGearRow>();
        public readonly List<CharacterSheetSkillRow> activeSkills = new List<CharacterSheetSkillRow>();

        public string starterPassiveName, starterPassiveDesc;
        public string racialFeatName, racialFeatDesc;
        public string racialUtilityName, racialUtilityDesc;
        public string backgroundPerkName, backgroundPerkDesc;
    }

    public static class CharacterSheetBuilder
    {
        public static CharacterSheetView Build(CharacterCreationSession session)
        {
            if (session == null) return null;
            var race = session.Race;
            var cls = session.Class;
            var bg = session.Background;
            var rMod = session.RacialModifiers;
            var bonusParts = AbilityScores.AllAbilities
                .Where(a => rMod[a] != 0)
                .Select(a => $"+{rMod[a]} {a}")
                .ToList();

            string lineageText = "";
            if (race != null && race.usesLineage && !string.IsNullOrEmpty(session.SelectedLineageId)
                && Database.DragonbornLineages.TryGetValue(session.SelectedLineageId, out var lin))
            {
                lineageText = $"{lin.name} ({lin.element})";
            }

            var bgRank = bg?.GetRank(1);
            var view = new CharacterSheetView
            {
                heroName = session.CharacterName ?? cls?.name ?? "Hero",
                raceName = race?.name ?? "-",
                racialBonusSummary = bonusParts.Count > 0 ? string.Join(", ", bonusParts) : "None",
                lineageSummary = lineageText,
                className = cls?.name ?? "-",
                classRole = cls?.role ?? "-",
                backgroundName = bg?.name ?? "-",
                baseScores = session.BaseScores,
                racialModifiers = rMod,
                equipmentBonuses = session.EquipmentAbilityBonuses,
                finalScores = session.FinalScores,
                abilityModifiers = session.AbilityModifiers,
                derived = session.PreviewDerivedStats,
                affinities = cls?.affinities ?? Affinities.None,
                starterPassiveName = session.StarterPassive?.name ?? "None",
                starterPassiveDesc = session.StarterPassive?.description ?? "",
                racialFeatName = race?.racialFeat?.name ?? "None",
                racialFeatDesc = race?.racialFeat?.description ?? "",
                racialUtilityName = race?.racialUtility?.name ?? "None",
                racialUtilityDesc = race?.racialUtility?.description ?? "",
                backgroundPerkName = bgRank?.perkName ?? bg?.perkName ?? "None",
                backgroundPerkDesc = bgRank?.perkDescription ?? bg?.description ?? "",
            };

            foreach (var slot in EquipmentSlots.All)
            {
                var eq = session.GetEquipped(slot);
                string summary = "Empty";
                if (eq != null)
                {
                    var stats = new List<string>();
                    if (eq.weaponPower > 0) stats.Add($"WP +{eq.weaponPower}");
                    if (eq.physicalDefense > 0) stats.Add($"DEF +{eq.physicalDefense}");
                    if (eq.magicDefense > 0) stats.Add($"MDEF +{eq.magicDefense}");
                    if (eq.breakPowerModifier != 0) stats.Add($"BRK {(eq.breakPowerModifier > 0 ? "+" : "")}{eq.breakPowerModifier}");
                    summary = stats.Count > 0 ? string.Join(" · ", stats) : (eq.description ?? eq.weaponType.ToString());
                }
                view.gear.Add(new CharacterSheetGearRow
                {
                    slot = slot,
                    slotName = EquipmentSlots.DisplayName(slot),
                    itemName = eq?.name ?? "Empty",
                    summary = summary,
                });
            }

            foreach (var sid in session.Loadout.GetEquippedActives())
            {
                if (!Database.Skills.TryGetValue(sid, out var sk)) continue;
                view.activeSkills.Add(new CharacterSheetSkillRow
                {
                    skillId = sid,
                    name = sk.name,
                    rank = Math.Max(1, session.Loadout.GetRank(sid)),
                });
            }

            return view;
        }
    }

    // F5 (Character Creation Audit): read-only soft gap warnings computed from PendingParty alone.
    // Never reads elements, enemy affinities, or enemy names. Never blocks party confirmation.
    public static class PartyOverviewEvaluator
    {
        public const string GapNoHealer = "No healer";
        public const string GapNoTank = "No tank";
        public const string GapNoBreakPressure = "No Break pressure";
        public const string GapNoStatusCleanse = "No status cleanse";
        public const string NoGapsText = "No gaps found";

        public static List<string> Evaluate(IReadOnlyList<CharacterDef> party)
        {
            var gaps = new List<string>();
            if (party == null || party.Count == 0)
            {
                gaps.Add(GapNoHealer);
                gaps.Add(GapNoTank);
                gaps.Add(GapNoBreakPressure);
                gaps.Add(GapNoStatusCleanse);
                return gaps;
            }

            bool hasHealer = false;
            bool hasTank = false;
            bool hasBreakSkill = false;
            bool hasStatusCleanse = false;
            var breakPowers = new List<int>(party.Count);

            for (int i = 0; i < party.Count; i++)
            {
                var c = party[i];
                if (c == null) continue;

                Database.Classes.TryGetValue(c.classId ?? "", out var cls);
                string role = cls?.role ?? c.role ?? "";
                if (role.IndexOf("Defender", StringComparison.OrdinalIgnoreCase) >= 0
                    || role.IndexOf("Tank", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    hasTank = true;
                }

                // Healing racial feats: Liichtian (Lightbound) or Merfolk (Ocean's Embrace)
                if (string.Equals(c.raceId, "liichtian", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(c.raceId, "merfolk", StringComparison.OrdinalIgnoreCase))
                {
                    hasHealer = true;
                }

                // Inspect active/starting skills
                var skillIds = new HashSet<string>();
                if (c.activeSkillLoadout != null) foreach (var s in c.activeSkillLoadout) if (!string.IsNullOrEmpty(s)) skillIds.Add(s);
                if (c.skills != null) foreach (var s in c.skills) if (!string.IsNullOrEmpty(s)) skillIds.Add(s);
                if (cls?.startingSkills != null) foreach (var s in cls.startingSkills) if (!string.IsNullOrEmpty(s)) skillIds.Add(s);

                foreach (var sid in skillIds)
                {
                    if (!Database.Skills.TryGetValue(sid, out var sk)) continue;
                    if (sk.kind == SkillKind.Heal) hasHealer = true;
                    if (sk.breakMultiplier >= 1.20f) hasBreakSkill = true;
                    if (IsCleanseSkill(sk)) hasStatusCleanse = true;
                }

                // Also check early class unlock tree (tier <= 2) for status cleanse (e.g. Cleric Purify)
                if (!hasStatusCleanse && !string.IsNullOrEmpty(c.classId))
                {
                    foreach (var sk in Database.Skills.Values)
                    {
                        if (string.Equals(sk.classId, c.classId, StringComparison.OrdinalIgnoreCase)
                            && sk.tier <= 2 && IsCleanseSkill(sk))
                        {
                            hasStatusCleanse = true;
                            break;
                        }
                    }
                }

                var unit = new Unit(c);
                breakPowers.Add(Math.Max(unit.Derived.physicalBreakPower, unit.Derived.magicBreakPower));
            }

            bool hasAboveAvgBreak = false;
            if (breakPowers.Count > 1)
            {
                double avg = breakPowers.Average();
                hasAboveAvgBreak = breakPowers.Any(bp => bp > avg + 1.0);
            }

            if (!hasHealer) gaps.Add(GapNoHealer);
            if (!hasTank) gaps.Add(GapNoTank);
            if (!hasBreakSkill && !hasAboveAvgBreak) gaps.Add(GapNoBreakPressure);
            if (!hasStatusCleanse) gaps.Add(GapNoStatusCleanse);

            return gaps;
        }

        public static string FormatDisplay(IReadOnlyList<CharacterDef> party)
        {
            var gaps = Evaluate(party);
            return gaps.Count == 0 ? NoGapsText : string.Join("\n", gaps.Select(g => "• " + g));
        }

        static bool IsCleanseSkill(SkillDef sk)
        {
            if (sk == null) return false;
            if (string.Equals(sk.id, "purify", StringComparison.OrdinalIgnoreCase)) return true;
            string desc = sk.description ?? "";
            return desc.IndexOf("cleanse", StringComparison.OrdinalIgnoreCase) >= 0
                || desc.IndexOf("remove all negative status", StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
