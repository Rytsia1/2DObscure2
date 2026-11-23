using System;
using System.Collections.Generic;
using System.Linq;

namespace Obscure
{
    // Run tuning: reward sizes and rest strength.
    public static class RunConfig
    {
        public const float RestRecover = 0.5f;            // Recover restores this share of missing HP and MP
        // Gold. Target (design decision 2026-11-20): clearing Dungeon 1 pays for about one full-party Temple Recovery
        // (4 x 300), so after each dungeon the player chooses between healing everyone and buying gear.
        // Old values (10-18 / 30-42 / 20-35 / 100) gave about 385 gold per Dungeon 1 path (BalanceSimulation.ExpPacingReport,
        // 200 maps, 2026-11-20); these are roughly 3x. Starting values: re-measure with the report after changes to maps or prices.
        public static readonly (int min, int max) BattleGold = (30, 55), EliteGold = (90, 125), TreasureGold = (60, 105);
        public const int BossGold = 350;
        public const int RelicFallbackGold = 90;           // when every relic is owned

        // Retry budget per expedition (GDD failure model C): a wipe or abandon spends one; a wipe with none left ends the expedition.
        // Starting value, untested. Test: playtest full runs. Most testers never spend one -> lower it;
        // testers quit right after running out -> raise it or let a retry be bought back.
        public const int RetriesPerExpedition = 3;

        // EXP. Target (design decision 2026-11-20): level 13 by the end of Dungeon 1, level 15 during Dungeon 2.
        // Measured on 200 generated maps (BalanceSimulation.ExpPacingReport, 2026-11-20): a Dungeon 1 path has 10-13
        // normal fights plus two Elite stage bosses and Zoroa, so even a fight-avoiding path reaches level 13 on average.
        // Regression test: BalanceSimulation.RandomPathThroughDungeon1ReachesLevel13.
        public const int MaxLevel = 15;
        public const int BattleExp = 150, EliteExp = 350, BossExp = 750;
        public static int ExpToNext(int level) => 100 + 25 * (Math.Max(1, level) - 1);
        public static int ExpFor(Tier tier) => tier == Tier.Boss ? BossExp : tier == Tier.Elite ? EliteExp : BattleExp;
    }

    // Builds reward offers from pools. Generation only: RunState applies, RewardScreen presents.
    public static class RewardGenerator
    {
        static readonly string[] CommonItems = { "draught", "draught", "ether" };

        public static RewardOffer ForBattle(Tier tier, Random rng, ICollection<string> ownedRelics, int act = 1)
        {
            switch (tier)
            {
                case Tier.Boss:
                {
                    // The biggest fight gets the biggest choice: the boss relic (another relic once it is owned), a second
                    // relic, and Rare/Epic gear. Without a free relic slot, relic picks become gear instead of a little gold.
                    var boss = new RewardOffer { title = "The Warden Falls", text = "The abyss shudders and goes silent.", gold = RunConfig.BossGold };
                    bool slotFree = ownedRelics == null || ownedRelics.Count < RelicManager.MaxRelicCapacity;
                    var offered = new HashSet<string>();
                    if (slotFree && (ownedRelics == null || !ownedRelics.Contains(Database.BossRelic)))
                        offered.Add(Database.BossRelic);
                    while (slotFree && offered.Count < 2)
                    {
                        var pool = Database.Relics.Keys
                            .Where(id => id != Database.BossRelic && !offered.Contains(id) && (ownedRelics == null || !ownedRelics.Contains(id)))
                            .OrderBy(id => id).ToList();
                        if (pool.Count == 0) break;
                        offered.Add(pool[rng.Next(pool.Count)]);
                    }
                    foreach (var id in offered) boss.choices.Add(new RewardOption { kind = RewardKind.Relic, id = id });
                    var gearIds = new HashSet<string>();
                    while (boss.choices.Count < 3)
                    {
                        var gear = Gear(rng, EquipmentRarity.Rare, EquipmentRarity.Epic, gearIds);
                        gearIds.Add(gear.id);
                        boss.choices.Add(gear);
                    }
                    return boss;
                }
                case Tier.Elite:
                    return new RewardOffer
                    {
                        title = "Elite Vanquished", text = "Its oath is broken. Claim a prize.", gold = Roll(RunConfig.EliteGold, rng),
                        choices = { Relic(rng, ownedRelics), Upgrade(rng), new RewardOption { kind = RewardKind.Item, id = "elixir" } },
                    };
                default:
                {
                    // Three different kinds of reward: something to heal with now, gear for the build, or gold for the
                    // Temple and shops. Gear quality rises from Dungeon 3.
                    var normal = new RewardOffer { title = "Victory", text = "The dead lie still once more.", gold = Roll(RunConfig.BattleGold, rng) };
                    normal.choices.Add(new RewardOption { kind = RewardKind.Item, id = CommonItems[rng.Next(CommonItems.Length)] });
                    normal.choices.Add(Gear(rng, EquipmentRarity.Common, act >= 3 ? EquipmentRarity.Rare : EquipmentRarity.Uncommon));
                    normal.choices.Add(new RewardOption { kind = RewardKind.Gold, amount = Roll(RunConfig.BattleGold, rng) });
                    return normal;
                }
            }
        }

        // A random base equipment piece in a rarity band (never cursed or unarmed), granted at its own rarity and level.
        static RewardOption Gear(Random rng, EquipmentRarity min, EquipmentRarity max, ICollection<string> exclude = null)
        {
            var pool = Database.Equipment.Values
                .Where(e => !e.isUnarmed && !e.IsCursed && e.rarity >= min && e.rarity <= max && (exclude == null || !exclude.Contains(e.id)))
                .OrderBy(e => e.id).ToList();
            return pool.Count == 0
                ? new RewardOption { kind = RewardKind.Gold, amount = RunConfig.RelicFallbackGold }
                : new RewardOption { kind = RewardKind.Equipment, id = pool[rng.Next(pool.Count)].id };
        }

        public static RewardOffer ForTreasure(Random rng, ICollection<string> ownedRelics) => new RewardOffer
        {
            title = "Ossuary Cache", text = "Beneath the bones, something worth keeping.", gold = Roll(RunConfig.TreasureGold, rng),
            choices = { Relic(rng, ownedRelics), new RewardOption { kind = RewardKind.Item, id = "elixir" }, Upgrade(rng) },
        };

        // A relic the party does not have yet; null when every pool relic is owned or party capacity (5) is full.
        public static string RandomRelicId(Random rng, ICollection<string> owned)
        {
            if (owned != null && owned.Count >= RelicManager.MaxRelicCapacity)
                return null;

            var pool = Database.Relics.Keys.Where(id => id != Database.BossRelic && !owned.Contains(id)).ToList();
            return pool.Count == 0 ? null : pool[rng.Next(pool.Count)];
        }

        static RewardOption Relic(Random rng, ICollection<string> owned)
        {
            var id = RandomRelicId(rng, owned);
            return id == null
                ? new RewardOption { kind = RewardKind.Gold, amount = RunConfig.RelicFallbackGold }
                : new RewardOption { kind = RewardKind.Relic, id = id };
        }

        static RewardOption Upgrade(Random rng)
        {
            var ids = Database.Upgrades.Keys.ToList();
            return new RewardOption { kind = RewardKind.Upgrade, id = ids[rng.Next(ids.Count)] };
        }

        static int Roll((int min, int max) range, Random rng) => rng.Next(range.min, range.max + 1);

        // Rolls distinct Dungeon Modifier offers (Buffs, plus rare Cursed Buff+Drawback) deterministically.
        // Already owned modifiers remain in the pool per Bab 15 §2 & §3.2 so they can be stacked or re-acquired,
        // but ones already at their stack cap (isMaxed) are skipped: a pick there would add nothing.
        public static List<RewardOption> GenerateModifierChoices(Random rng, int count = 3, bool includeCursed = false, ModifierCategory? categoryFilter = null,
            Func<string, ElementType, bool> isMaxed = null, Func<DungeonModifierDef, bool> filter = null)
        {
            var rand = rng ?? new Random();
            var pool = Database.DungeonModifiers.Values
                .Where(m => m.polarity != ModifierPolarity.Debuff)
                .Where(m => includeCursed || m.polarity == ModifierPolarity.Buff)
                .Where(m => !categoryFilter.HasValue || m.category == categoryFilter.Value)
                .Where(m => filter == null || filter(m))
                .ToList();

            var result = new List<RewardOption>();
            var usedIds = new HashSet<string>();
            for (int i = 0; i < count && pool.Count > 0; i++)
            {
                var remaining = pool.Where(m => !usedIds.Contains(m.modifierId)).ToList();
                if (remaining.Count == 0) break;

                // Weight slightly by rarity: Common 3, Uncommon 2, Rare 1
                int totalWeight = remaining.Sum(m => m.rarity == ModifierRarity.Common ? 3 : m.rarity == ModifierRarity.Uncommon ? 2 : 1);
                int roll = rand.Next(totalWeight);
                DungeonModifierDef picked = remaining[0];
                int acc = 0;
                foreach (var m in remaining)
                {
                    acc += m.rarity == ModifierRarity.Common ? 3 : m.rarity == ModifierRarity.Uncommon ? 2 : 1;
                    if (roll < acc) { picked = m; break; }
                }

                usedIds.Add(picked.modifierId);
                ElementType elem = ElementType.None;
                if (picked.isElementSpecific)
                    elem = ElementTypes.All[rand.Next(ElementTypes.All.Length)];
                if (isMaxed != null && isMaxed(picked.modifierId, elem)) { i--; continue; } // try another; usedIds shrinks the pool

                result.Add(new RewardOption { kind = RewardKind.Modifier, id = picked.modifierId, element = elem });
            }
            return result;
        }

        // Player-facing text for an option (shared by the reward, event and summary screens).
        public static (string label, string detail) Describe(RewardOption o) => Describe(o, null);

        // With the party, an Upgrade names the hero it actually lands on (RunState.ResolveUpgrade), not the preset key.
        public static (string label, string detail) Describe(RewardOption o, DungeonModifierManager modMgr, IList<Unit> party = null) => o.kind switch
        {
            RewardKind.Upgrade when party != null && party.Count > 0 => DescribeUpgrade(RunState.ResolveUpgrade(party, o.id)),
            RewardKind.Item => Database.Items.TryGetValue(o.id, out var itm) ? (itm.name, itm.description) : (o.id, ""),
            RewardKind.Equipment => Database.Equipment.TryGetValue(o.id, out var eq) ? ($"{eq.name} ({EquipmentSlots.DisplayName(eq.slot)})", eq.description) : (o.id, ""),
            RewardKind.Relic => ("Relic: " + Database.Relics[o.id].name, Database.Relics[o.id].description),
            RewardKind.Upgrade => ($"{Database.Characters[o.id].name}: {Database.Upgrades[o.id].name}",
                Database.Characters[o.id].stats.Preview(Database.Upgrades[o.id].bonus)),
            RewardKind.Modifier => DescribeModifier(o, modMgr),
            _ => ($"{o.amount} Spoils", $"Take {o.amount} extra spoils."),
        };

        static (string label, string detail) DescribeUpgrade((Unit hero, UpgradeDef up) r) =>
            ($"{r.hero.name}: {r.up.name}", r.hero.stats.Preview(r.up.bonus));

        static (string label, string detail) DescribeModifier(RewardOption o, DungeonModifierManager modMgr)
        {
            if (!Database.DungeonModifiers.TryGetValue(o.id, out var def))
                return (o.id, "");
            string label = modMgr != null
                ? modMgr.FormatOfferHeading(o.id, o.element)
                : (def.isElementSpecific && o.element.IsValidElement() ? $"{def.displayName} ({o.element})" : def.displayName);
            string detail = def.HasDrawback && !string.IsNullOrEmpty(def.drawbackDescription)
                ? $"{def.description} Drawback: {def.drawbackDescription}"
                : def.description;
            return (label, detail);
        }
    }
}
