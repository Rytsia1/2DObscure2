using System;
using System.Collections.Generic;
using System.Linq;

namespace Obscure
{
    // Run tuning: reward sizes and rest strength.
    public static class RunConfig
    {
        public const float RestRecover = 0.5f;            // Recover restores this share of missing HP and MP
        public static readonly (int min, int max) BattleGold = (10, 18), EliteGold = (30, 42), TreasureGold = (20, 35);
        public const int BossGold = 100;
        public const int RelicFallbackGold = 30;           // when every relic is owned
    }

    // Builds reward offers from pools. Generation only: RunState applies, RewardScreen presents.
    public static class RewardGenerator
    {
        static readonly string[] CommonItems = { "draught", "draught", "ether" };

        public static RewardOffer ForBattle(Tier tier, Random rng, ICollection<string> ownedRelics)
        {
            switch (tier)
            {
                case Tier.Boss:
                    bool canTakeBossRelic = ownedRelics == null
                        || (!ownedRelics.Contains(Database.BossRelic) && ownedRelics.Count < RelicManager.MaxRelicCapacity);
                    var bossChoice = canTakeBossRelic
                        ? new RewardOption { kind = RewardKind.Relic, id = Database.BossRelic }
                        : new RewardOption { kind = RewardKind.Gold, amount = RunConfig.RelicFallbackGold };
                    return new RewardOffer
                    {
                        title = "The Warden Falls", text = "The abyss shudders and goes silent.", gold = RunConfig.BossGold,
                        choices = { bossChoice },
                    };
                case Tier.Elite:
                    return new RewardOffer
                    {
                        title = "Elite Vanquished", text = "Its oath is broken. Claim a prize.", gold = Roll(RunConfig.EliteGold, rng),
                        choices = { Relic(rng, ownedRelics), Upgrade(rng), new RewardOption { kind = RewardKind.Item, id = "elixir" } },
                    };
                default:
                    var a = CommonItems[rng.Next(CommonItems.Length)];
                    var b = a == "ether" ? "draught" : "ether";
                    return new RewardOffer
                    {
                        title = "Victory", text = "The dead lie still once more.", gold = Roll(RunConfig.BattleGold, rng),
                        choices = { new RewardOption { kind = RewardKind.Item, id = a }, new RewardOption { kind = RewardKind.Item, id = b } },
                    };
            }
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
        // Already owned modifiers remain in the pool per Bab 15 §2 & §3.2 so they can be stacked or re-acquired.
        public static List<RewardOption> GenerateModifierChoices(Random rng, int count = 3, bool includeCursed = false, ModifierCategory? categoryFilter = null)
        {
            var rand = rng ?? new Random();
            var pool = Database.DungeonModifiers.Values
                .Where(m => m.polarity != ModifierPolarity.Debuff)
                .Where(m => includeCursed || m.polarity == ModifierPolarity.Buff)
                .Where(m => !categoryFilter.HasValue || m.category == categoryFilter.Value)
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

                result.Add(new RewardOption { kind = RewardKind.Modifier, id = picked.modifierId, element = elem });
            }
            return result;
        }

        // Player-facing text for an option (shared by the reward, event and summary screens).
        public static (string label, string detail) Describe(RewardOption o) => Describe(o, null);

        public static (string label, string detail) Describe(RewardOption o, DungeonModifierManager modMgr) => o.kind switch
        {
            RewardKind.Item => Database.Items.TryGetValue(o.id, out var itm) ? (itm.name, itm.description) : (o.id, ""),
            RewardKind.Equipment => Database.Equipment.TryGetValue(o.id, out var eq) ? ($"{eq.name} ({EquipmentSlots.DisplayName(eq.slot)})", eq.description) : (o.id, ""),
            RewardKind.Relic => ("Relic: " + Database.Relics[o.id].name, Database.Relics[o.id].description),
            RewardKind.Upgrade => ($"{Database.Characters[o.id].name}: {Database.Upgrades[o.id].name}",
                Database.Characters[o.id].stats.Preview(Database.Upgrades[o.id].bonus)),
            RewardKind.Modifier => DescribeModifier(o, modMgr),
            _ => ($"{o.amount} Gold", $"Take {o.amount} extra gold."),
        };

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
