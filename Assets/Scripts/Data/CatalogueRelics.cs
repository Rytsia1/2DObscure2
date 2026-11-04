// Generated from Assets/Docs/Felna_Layer1_Relic_List.xlsx
using System;
using System.Collections.Generic;

namespace Obscure
{
    public static partial class Database
    {
        public static readonly Dictionary<string, RelicDef> Layer1Relics = Index(new[]
        {
            new RelicDef
            {
                id = "R001",
                name = "Ashen Signet",
                category = RelicCategory.Offensive,
                proposedRarity = RelicProposedRarity.Common,
                triggerType = RelicTriggerType.Combat,
                activationLimit = RelicActivationLimit.Passive,
                gameplayEffect = "Saat menyerang musuh dengan HP di atas 80%, damage +8%.",
                designIntent = "Mendorong pembukaan combat yang agresif.",
                description = "Saat menyerang musuh dengan HP di atas 80%, damage +8%.",
                value1 = 0.08f, value2 = 0.80f
            },
            new RelicDef
            {
                id = "R002",
                name = "Soulglass Shard",
                category = RelicCategory.Resource,
                proposedRarity = RelicProposedRarity.Uncommon,
                triggerType = RelicTriggerType.Combat,
                activationLimit = RelicActivationLimit.Counter,
                gameplayEffect = "Setiap 3 kali memperoleh Perfect Offensive QTE, pulihkan 2 MP kepada karakter aktif.",
                designIntent = "Menghubungkan performa QTE dengan ekonomi MP.",
                description = "Setiap 3 kali memperoleh Perfect Offensive QTE, pulihkan 2 MP kepada karakter aktif.",
                value1 = 2f, intValue = 3
            },
            new RelicDef
            {
                id = "R003",
                name = "Rustbound Medal",
                category = RelicCategory.Defensive,
                proposedRarity = RelicProposedRarity.Uncommon,
                triggerType = RelicTriggerType.Combat,
                activationLimit = RelicActivationLimit.Passive,
                gameplayEffect = "Karakter dengan HP di bawah 30% menerima 10% lebih sedikit direct damage.",
                designIntent = "Memberi ruang bertahan saat kondisi kritis.",
                description = "Karakter dengan HP di bawah 30% menerima 10% lebih sedikit direct damage.",
                value1 = 0.10f, value2 = 0.30f
            },
            new RelicDef
            {
                id = "R004",
                name = "Gravekeeper's Thread",
                category = RelicCategory.Survival,
                proposedRarity = RelicProposedRarity.Common,
                triggerType = RelicTriggerType.PostBattle,
                activationLimit = RelicActivationLimit.Passive,
                gameplayEffect = "Setelah battle, karakter dengan HP terendah memulihkan 3% Max HP.",
                designIntent = "Pemulihan kecil antarbattle tanpa menggantikan item heal.",
                description = "Setelah battle, karakter dengan HP terendah memulihkan 3% Max HP.",
                value1 = 0.03f
            },
            new RelicDef
            {
                id = "R005",
                name = "Warden's Broken Key",
                category = RelicCategory.Utility,
                proposedRarity = RelicProposedRarity.Rare,
                triggerType = RelicTriggerType.Exploration,
                activationLimit = RelicActivationLimit.Passive,
                gameplayEffect = "Saat memasuki event bertag hidden atau trap, tampilkan satu petunjuk tambahan jika tersedia.",
                designIntent = "Mendukung eksplorasi layer penjara/kerajaan runtuh tanpa auto-solve.",
                description = "Saat memasuki event bertag hidden atau trap, tampilkan satu petunjuk tambahan jika tersedia."
            },
            new RelicDef
            {
                id = "R006",
                name = "Crown of the Unremembered",
                category = RelicCategory.Offensive,
                proposedRarity = RelicProposedRarity.Rare,
                triggerType = RelicTriggerType.Combat,
                activationLimit = RelicActivationLimit.Passive,
                gameplayEffect = "Damage terhadap musuh yang sedang terkena status effect +7%.",
                designIntent = "Sinergi dengan party yang mengandalkan debuff/status.",
                description = "Damage terhadap musuh yang sedang terkena status effect +7%.",
                value1 = 0.07f
            },
            new RelicDef
            {
                id = "R007",
                name = "Mourner's Coin",
                category = RelicCategory.Economy,
                proposedRarity = RelicProposedRarity.Uncommon,
                triggerType = RelicTriggerType.Reward,
                activationLimit = RelicActivationLimit.Passive,
                gameplayEffect = "Saat menemukan Run Gold, peroleh tambahan 10% (dibulatkan ke bawah).",
                designIntent = "Membantu ekonomi run; Gold tetap hilang jika run gagal.",
                description = "Saat menemukan Run Gold, peroleh tambahan 10% (dibulatkan ke bawah).",
                value1 = 0.10f
            },
            new RelicDef
            {
                id = "R008",
                name = "Pale Lantern",
                category = RelicCategory.Exploration,
                proposedRarity = RelicProposedRarity.Rare,
                triggerType = RelicTriggerType.Map,
                activationLimit = RelicActivationLimit.Passive,
                gameplayEffect = "Saat memilih jalur node, tampilkan tipe satu node tersembunyi di depan jika sistem map mendukungnya.",
                designIntent = "Memberi informasi, bukan mengubah hasil encounter.",
                description = "Saat memilih jalur node, tampilkan tipe satu node tersembunyi di depan jika sistem map mendukungnya."
            },
            new RelicDef
            {
                id = "R009",
                name = "Soul-Tether Ring",
                category = RelicCategory.Defensive,
                proposedRarity = RelicProposedRarity.Epic,
                triggerType = RelicTriggerType.Combat,
                activationLimit = RelicActivationLimit.OncePerBattle,
                gameplayEffect = "Sekali per battle, ketika karakter akan menerima damage yang membuatnya Down, sisakan 1 HP. Tidak aktif jika sudah Down.",
                designIntent = "Efek penyelamat yang kuat dengan batas jelas.",
                description = "Sekali per battle, ketika karakter akan menerima damage yang membuatnya Down, sisakan 1 HP. Tidak aktif jika sudah Down.",
                value1 = 1f
            },
            new RelicDef
            {
                id = "R010",
                name = "Executioner's Bell",
                category = RelicCategory.Offensive,
                proposedRarity = RelicProposedRarity.Uncommon,
                triggerType = RelicTriggerType.Combat,
                activationLimit = RelicActivationLimit.OncePerBattle,
                gameplayEffect = "Serangan pertama party pada setiap battle mendapat Break Power +15%.",
                designIntent = "Membantu membuka peluang Break di awal pertarungan.",
                description = "Serangan pertama party pada setiap battle mendapat Break Power +15%.",
                value1 = 0.15f
            },
            new RelicDef
            {
                id = "R011",
                name = "Hollow Knight's Buckle",
                category = RelicCategory.Defensive,
                proposedRarity = RelicProposedRarity.Rare,
                triggerType = RelicTriggerType.Combat,
                activationLimit = RelicActivationLimit.Passive,
                gameplayEffect = "Saat party memiliki minimal 2 karakter Down, karakter yang masih aktif mendapat All Damage Taken −8%.",
                designIntent = "Efek comeback dengan kondisi berisiko tinggi.",
                description = "Saat party memiliki minimal 2 karakter Down, karakter yang masih aktif mendapat All Damage Taken −8%.",
                value1 = 0.08f, intValue = 2
            },
            new RelicDef
            {
                id = "R012",
                name = "Last King's Seal",
                category = RelicCategory.Offensive,
                proposedRarity = RelicProposedRarity.Rare,
                triggerType = RelicTriggerType.Combat,
                activationLimit = RelicActivationLimit.Passive,
                gameplayEffect = "Saat menyerang Boss, damage +5%.",
                designIntent = "Bonus boss sederhana agar relevan sepanjang layer.",
                description = "Saat menyerang Boss, damage +5%.",
                value1 = 0.05f
            },
            new RelicDef
            {
                id = "R013",
                name = "Dirge-Eater's Fang",
                category = RelicCategory.Resource,
                proposedRarity = RelicProposedRarity.Rare,
                triggerType = RelicTriggerType.Combat,
                activationLimit = RelicActivationLimit.CapPerBattle,
                gameplayEffect = "Ketika musuh dikalahkan, karakter yang memberi killing blow memulihkan 1 MP. Maksimal 3 MP per battle.",
                designIntent = "Reward untuk eksekusi target dan pengelolaan giliran.",
                description = "Ketika musuh dikalahkan, karakter yang memberi killing blow memulihkan 1 MP. Maksimal 3 MP per battle.",
                value1 = 1f, maxPerBattle = 3
            },
            new RelicDef
            {
                id = "R014",
                name = "Pallbearer's Gloves",
                category = RelicCategory.Utility,
                proposedRarity = RelicProposedRarity.Epic,
                triggerType = RelicTriggerType.Exploration,
                activationLimit = RelicActivationLimit.OncePerDungeon,
                gameplayEffect = "Interaksi dengan peti tidak memicu satu trap minor pertama dalam dungeon, jika trap tersebut dapat dinegasikan.",
                designIntent = "Utility eksplorasi terbatas; tidak meniadakan trap besar/event utama.",
                description = "Interaksi dengan peti tidak memicu satu trap minor pertama dalam dungeon, jika trap tersebut dapat dinegasikan."
            },
            new RelicDef
            {
                id = "R015",
                name = "Veil of Dust",
                category = RelicCategory.Defensive,
                proposedRarity = RelicProposedRarity.Uncommon,
                triggerType = RelicTriggerType.Combat,
                activationLimit = RelicActivationLimit.Passive,
                gameplayEffect = "Resistance terhadap Burn, Poison, dan Bleed +10%.",
                designIntent = "Membantu menghadapi ancaman status tanpa memberi kekebalan.",
                description = "Resistance terhadap Burn, Poison, dan Bleed +10%.",
                value1 = 0.10f
            },
            new RelicDef
            {
                id = "R016",
                name = "Royal Gravewax",
                category = RelicCategory.Survival,
                proposedRarity = RelicProposedRarity.Rare,
                triggerType = RelicTriggerType.Consumable,
                activationLimit = RelicActivationLimit.Passive,
                gameplayEffect = "Item pemulihan HP yang digunakan pada party memulihkan tambahan 5% dari nilai dasarnya.",
                designIntent = "Meningkatkan efisiensi consumable; tidak menciptakan heal gratis.",
                description = "Item pemulihan HP yang digunakan pada party memulihkan tambahan 5% dari nilai dasarnya.",
                value1 = 0.05f
            },
            new RelicDef
            {
                id = "R017",
                name = "Oathstone of the Fallen",
                category = RelicCategory.Offensive,
                proposedRarity = RelicProposedRarity.Uncommon,
                triggerType = RelicTriggerType.Combat,
                activationLimit = RelicActivationLimit.Conditional,
                gameplayEffect = "Jika karakter tidak menerima damage sejak giliran terakhirnya, serangan direct damage berikutnya mendapat +6% damage.",
                designIntent = "Mendorong positioning/defense dan timing serangan.",
                description = "Jika karakter tidak menerima damage sejak giliran terakhirnya, serangan direct damage berikutnya mendapat +6% damage.",
                value1 = 0.06f
            },
            new RelicDef
            {
                id = "R018",
                name = "Whispering Reliquary",
                category = RelicCategory.Exploration,
                proposedRarity = RelicProposedRarity.Rare,
                triggerType = RelicTriggerType.Event,
                activationLimit = RelicActivationLimit.Passive,
                gameplayEffect = "Pada event bertag relic atau cursed_reward, tampilkan indikator risiko tambahan sebelum memilih.",
                designIntent = "Selaras dengan racial utility Liichtian/Dragonborn tanpa menggantikannya.",
                description = "Pada event bertag relic atau cursed_reward, tampilkan indikator risiko tambahan sebelum memilih."
            },
            new RelicDef
            {
                id = "R019",
                name = "Black-Iron Rosary",
                category = RelicCategory.Defensive,
                proposedRarity = RelicProposedRarity.Epic,
                triggerType = RelicTriggerType.Combat,
                activationLimit = RelicActivationLimit.Conditional,
                gameplayEffect = "Setelah menerima Perfect Defensive QTE, karakter memperoleh Shield sebesar 2% Max HP. Maksimal sekali per giliran karakter.",
                designIntent = "Hadiah kecil untuk eksekusi defensif; dibatasi agar tidak snowball.",
                description = "Setelah menerima Perfect Defensive QTE, karakter memperoleh Shield sebesar 2% Max HP. Maksimal sekali per giliran karakter.",
                value1 = 0.02f
            },
            new RelicDef
            {
                id = "R020",
                name = "Sovereign's Last Breath",
                category = RelicCategory.Resource,
                proposedRarity = RelicProposedRarity.Epic,
                triggerType = RelicTriggerType.Boss,
                activationLimit = RelicActivationLimit.OncePerBattle,
                gameplayEffect = "Saat Boss mencapai 25% HP, seluruh party memulihkan 3 MP. Sekali per battle.",
                designIntent = "Momen pemulihan terbatas pada fase akhir boss.",
                description = "Saat Boss mencapai 25% HP, seluruh party memulihkan 3 MP. Sekali per battle.",
                value1 = 3f, value2 = 0.25f
            },
            new RelicDef
            {
                id = "R021",
                name = "Coffin Nail",
                category = RelicCategory.Offensive,
                proposedRarity = RelicProposedRarity.Rare,
                triggerType = RelicTriggerType.Combat,
                activationLimit = RelicActivationLimit.Passive,
                gameplayEffect = "Damage terhadap musuh yang sedang Broken +10%.",
                designIntent = "Mendukung strategi fokus Break dan burst window.",
                description = "Damage terhadap musuh yang sedang Broken +10%.",
                value1 = 0.10f
            },
            new RelicDef
            {
                id = "R022",
                name = "Zoroa's Dull Crown",
                category = RelicCategory.Offensive,
                proposedRarity = RelicProposedRarity.Epic,
                triggerType = RelicTriggerType.Combat,
                activationLimit = RelicActivationLimit.StackTrigger,
                gameplayEffect = "Setelah party mengalahkan 5 musuh dalam satu battle, seluruh party mendapat damage +4% sampai battle berakhir.",
                designIntent = "Relic kuat bertema pengumpulan jiwa, tetapi perlu battle panjang untuk aktif.",
                description = "Setelah party mengalahkan 5 musuh dalam satu battle, seluruh party mendapat damage +4% sampai battle berakhir.",
                value1 = 0.04f, intValue = 5
            },
        }, r => r.id);

        static void RegisterLayer1Relics()
        {
            foreach (var kv in Layer1Relics)
            {
                Relics[kv.Key] = kv.Value;
            }
        }
    }
}
