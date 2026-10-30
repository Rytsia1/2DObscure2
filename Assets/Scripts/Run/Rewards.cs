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
                    return new RewardOffer
                    {
                        title = "The Warden Falls", text = "The abyss shudders and goes silent.", gold = RunConfig.BossGold,
                        choices = { new RewardOption { kind = RewardKind.Relic, id = Database.BossRelic } },
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

        // A relic the party does not have yet; null when every pool relic is owned.
        public static string RandomRelicId(Random rng, ICollection<string> owned)
        {
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

        // Player-facing text for an option (shared by the reward, event and summary screens).
        public static (string label, string detail) Describe(RewardOption o) => o.kind switch
        {
            RewardKind.Item => Database.Items.TryGetValue(o.id, out var itm) ? (itm.name, itm.description) : (o.id, ""),
            RewardKind.Equipment => Database.Equipment.TryGetValue(o.id, out var eq) ? ($"{eq.name} ({EquipmentSlots.DisplayName(eq.slot)})", eq.description) : (o.id, ""),
            RewardKind.Relic => ("Relic: " + Database.Relics[o.id].name, Database.Relics[o.id].description),
            RewardKind.Upgrade => ($"{Database.Characters[o.id].name}: {Database.Upgrades[o.id].name}",
                Database.Characters[o.id].stats.Preview(Database.Upgrades[o.id].bonus)),
            _ => ($"{o.amount} Gold", $"Take {o.amount} extra gold."),
        };
    }
}
