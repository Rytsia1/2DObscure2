// Generated from Assets/Docs/Felna_Layer1_Relic_Framework_31_Relics_No_Mourners_Coin.xlsx
// Authoritative 31 Layer 1 Relics (5 November 2026)
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
                gameplayEffect = "Serangan party terhadap musuh dengan HP di atas 80% memberikan +5% Damage Bonus.",
                designIntent = "Membantu membuka pertarungan dengan tekanan damage tanpa memperbesar damage sepanjang battle.",
                description = "Serangan party terhadap musuh dengan HP di atas 80% memberikan +5% Damage Bonus.",
                demerit = "— (tidak memiliki demerit)",
                buyPrice = 1000,
                sellPrice = 500
            },
            new RelicDef
            {
                id = "R002",
                name = "Soulglass Shard",
                category = RelicCategory.Resource,
                proposedRarity = RelicProposedRarity.Uncommon,
                triggerType = RelicTriggerType.Qte,
                activationLimit = RelicActivationLimit.Counter,
                gameplayEffect = "Setiap kali party mengumpulkan 3 Perfect Offensive QTE, karakter yang menghasilkan Perfect ketiga memulihkan MP sebesar 2% Max MP.",
                designIntent = "Memberi insentif pada eksekusi QTE yang baik dan sedikit membantu ekonomi MP.",
                description = "Setiap kali party mengumpulkan 3 Perfect Offensive QTE, karakter yang menghasilkan Perfect ketiga memulihkan MP sebesar 2% Max MP.",
                demerit = "— (tidak memiliki demerit)",
                buyPrice = 1500,
                sellPrice = 750
            },
            new RelicDef
            {
                id = "R003",
                name = "Rustbound Medal",
                category = RelicCategory.Defensive,
                proposedRarity = RelicProposedRarity.Uncommon,
                triggerType = RelicTriggerType.Combat,
                activationLimit = RelicActivationLimit.Passive,
                gameplayEffect = "Karakter party yang memiliki HP 30% atau kurang menerima 8% lebih sedikit All Damage Taken.",
                designIntent = "Membantu karakter kritis bertahan tanpa memberi perlindungan kepada seluruh party sepanjang waktu.",
                description = "Karakter party yang memiliki HP 30% atau kurang menerima 8% lebih sedikit All Damage Taken.",
                demerit = "— (tidak memiliki demerit)",
                buyPrice = 1500,
                sellPrice = 750
            },
            new RelicDef
            {
                id = "R004",
                name = "Gravekeeper's Thread",
                category = RelicCategory.Survival,
                proposedRarity = RelicProposedRarity.Common,
                triggerType = RelicTriggerType.PostBattle,
                activationLimit = RelicActivationLimit.Passive,
                gameplayEffect = "Setelah battle selesai, karakter hidup dengan persentase HP terendah memulihkan 2% Max HP.",
                designIntent = "Memberi pemulihan kecil antar battle tanpa menggantikan item atau layanan Recovery.",
                description = "Setelah battle selesai, karakter hidup dengan persentase HP terendah memulihkan 2% Max HP.",
                demerit = "— (tidak memiliki demerit)",
                buyPrice = 900,
                sellPrice = 450
            },
            new RelicDef
            {
                id = "R005",
                name = "Warden's Broken Key",
                category = RelicCategory.Utility,
                proposedRarity = RelicProposedRarity.Rare,
                triggerType = RelicTriggerType.Event,
                activationLimit = RelicActivationLimit.Passive,
                gameplayEffect = "Pada event bertag hidden atau trap, tampilkan satu petunjuk tambahan jika event tersebut menyediakan informasi tambahan.",
                designIntent = "Menghadiahi pemain yang memperhatikan informasi dan memilih risiko secara sadar.",
                description = "Pada event bertag hidden atau trap, tampilkan satu petunjuk tambahan jika event tersebut menyediakan informasi tambahan.",
                demerit = "— (tidak memiliki demerit)",
                buyPrice = 1400,
                sellPrice = 700
            },
            new RelicDef
            {
                id = "R006",
                name = "Crown of the Unremembered",
                category = RelicCategory.Offensive,
                proposedRarity = RelicProposedRarity.Rare,
                triggerType = RelicTriggerType.Combat,
                activationLimit = RelicActivationLimit.Passive,
                gameplayEffect = "Party memberikan +6% Damage Bonus terhadap musuh yang sedang terkena minimal satu Status Effect.",
                designIntent = "Mendorong sinergi antara debuff, status application, dan damage dealer.",
                description = "Party memberikan +6% Damage Bonus terhadap musuh yang sedang terkena minimal satu Status Effect.",
                demerit = "— (tidak memiliki demerit)",
                buyPrice = 1900,
                sellPrice = 950
            },
            new RelicDef
            {
                id = "R008",
                name = "Pale Lantern",
                category = RelicCategory.Exploration,
                proposedRarity = RelicProposedRarity.Rare,
                triggerType = RelicTriggerType.Exploration,
                activationLimit = RelicActivationLimit.Passive,
                gameplayEffect = "Saat memasuki dungeon, ungkap tipe satu node tersembunyi yang masih berada di jalur map, jika tersedia.",
                designIntent = "Memberi informasi untuk navigasi dan pengambilan risiko.",
                description = "Saat memasuki dungeon, ungkap tipe satu node tersembunyi yang masih berada di jalur map, jika tersedia.",
                demerit = "— (tidak memiliki demerit)",
                buyPrice = 1400,
                sellPrice = 700
            },
            new RelicDef
            {
                id = "R009",
                name = "Soul-Tether Ring",
                category = RelicCategory.Defensive,
                proposedRarity = RelicProposedRarity.Epic,
                triggerType = RelicTriggerType.Combat,
                activationLimit = RelicActivationLimit.OncePerBattle,
                gameplayEffect = "Sekali per battle, ketika direct damage akan membuat karakter hidup menjadi Down, karakter tersebut bertahan pada 1 HP.",
                designIntent = "Memberi satu kesempatan penyelamatan dalam battle yang buruk.",
                description = "Sekali per battle, ketika direct damage akan membuat karakter hidup menjadi Down, karakter tersebut bertahan pada 1 HP.",
                demerit = "— (tidak memiliki demerit)",
                buyPrice = 0,
                sellPrice = 1800
            },
            new RelicDef
            {
                id = "R010",
                name = "Executioner's Bell",
                category = RelicCategory.Offensive,
                proposedRarity = RelicProposedRarity.Uncommon,
                triggerType = RelicTriggerType.Combat,
                activationLimit = RelicActivationLimit.OncePerBattle,
                gameplayEffect = "Serangan pertama party pada setiap battle memperoleh +10% Break Power.",
                designIntent = "Mendorong pembukaan battle yang terencana dan memberi akses lebih cepat ke Break.",
                description = "Serangan pertama party pada setiap battle memperoleh +10% Break Power.",
                demerit = "— (tidak memiliki demerit)",
                buyPrice = 1700,
                sellPrice = 850
            },
            new RelicDef
            {
                id = "R011",
                name = "Hollow Knight's Buckle",
                category = RelicCategory.Defensive,
                proposedRarity = RelicProposedRarity.Rare,
                triggerType = RelicTriggerType.Combat,
                activationLimit = RelicActivationLimit.Passive,
                gameplayEffect = "Ketika minimal dua karakter party berada dalam kondisi Down, seluruh karakter party yang masih aktif menerima 8% lebih sedikit All Damage Taken.",
                designIntent = "Mendukung prinsip Brutal but Recoverable ketika party berada dalam kondisi sangat tertekan.",
                description = "Ketika minimal dua karakter party berada dalam kondisi Down, seluruh karakter party yang masih aktif menerima 8% lebih sedikit All Damage Taken.",
                demerit = "— (tidak memiliki demerit)",
                buyPrice = 1600,
                sellPrice = 800
            },
            new RelicDef
            {
                id = "R012",
                name = "Last King's Seal",
                category = RelicCategory.Offensive,
                proposedRarity = RelicProposedRarity.Rare,
                triggerType = RelicTriggerType.Combat,
                activationLimit = RelicActivationLimit.Passive,
                gameplayEffect = "Party memberikan +5% Damage Bonus terhadap Boss.",
                designIntent = "Memberi peningkatan kecil yang konsisten untuk menghadapi encounter puncak.",
                description = "Party memberikan +5% Damage Bonus terhadap Boss.",
                demerit = "— (tidak memiliki demerit)",
                buyPrice = 1900,
                sellPrice = 950
            },
            new RelicDef
            {
                id = "R013",
                name = "Dirge-Eater's Fang",
                category = RelicCategory.Resource,
                proposedRarity = RelicProposedRarity.Rare,
                triggerType = RelicTriggerType.Combat,
                activationLimit = RelicActivationLimit.CapPerBattle,
                gameplayEffect = "Karakter yang memberikan killing blow memulihkan MP sebesar 1% Max MP. Efek dapat terpicu maksimal dua kali per battle.",
                designIntent = "Mengubah keberhasilan menghabisi musuh menjadi pemulihan resource kecil.",
                description = "Karakter yang memberikan killing blow memulihkan MP sebesar 1% Max MP. Efek dapat terpicu maksimal dua kali per battle.",
                demerit = "— (tidak memiliki demerit)",
                buyPrice = 1700,
                sellPrice = 850
            },
            new RelicDef
            {
                id = "R014",
                name = "Pallbearer's Gloves",
                category = RelicCategory.Utility,
                proposedRarity = RelicProposedRarity.Rare,
                triggerType = RelicTriggerType.Exploration,
                activationLimit = RelicActivationLimit.OncePerDungeon,
                gameplayEffect = "Meniadakan efek satu Minor Chest Trap pertama yang dapat dihindari pada dungeon tersebut.",
                designIntent = "Mengurangi satu risiko eksplorasi tanpa membuat semua treasure aman.",
                description = "Meniadakan efek satu Minor Chest Trap pertama yang dapat dihindari pada dungeon tersebut.",
                demerit = "— (tidak memiliki demerit)",
                buyPrice = 1400,
                sellPrice = 700
            },
            new RelicDef
            {
                id = "R015",
                name = "Veil of Dust",
                category = RelicCategory.Defensive,
                proposedRarity = RelicProposedRarity.Uncommon,
                triggerType = RelicTriggerType.Combat,
                activationLimit = RelicActivationLimit.Passive,
                gameplayEffect = "Status Effect Resistance party terhadap Burn, Poison, dan Bleed meningkat 8 poin persentase.",
                designIntent = "Membantu party menghadapi damage-over-time dan tekanan status tertentu.",
                description = "Status Effect Resistance party terhadap Burn, Poison, dan Bleed meningkat 8 poin persentase.",
                demerit = "— (tidak memiliki demerit)",
                buyPrice = 1400,
                sellPrice = 700
            },
            new RelicDef
            {
                id = "R016",
                name = "Royal Gravewax",
                category = RelicCategory.Survival,
                proposedRarity = RelicProposedRarity.Rare,
                triggerType = RelicTriggerType.Consumable,
                activationLimit = RelicActivationLimit.Passive,
                gameplayEffect = "HP yang dipulihkan oleh consumable HP meningkat sebesar 5% dari nilai dasar consumable tersebut.",
                designIntent = "Meningkatkan efisiensi persediaan healing tanpa membuat recovery gratis.",
                description = "HP yang dipulihkan oleh consumable HP meningkat sebesar 5% dari nilai dasar consumable tersebut.",
                demerit = "— (tidak memiliki demerit)",
                buyPrice = 1700,
                sellPrice = 850
            },
            new RelicDef
            {
                id = "R017",
                name = "Oathstone of the Fallen",
                category = RelicCategory.Offensive,
                proposedRarity = RelicProposedRarity.Uncommon,
                triggerType = RelicTriggerType.Combat,
                activationLimit = RelicActivationLimit.Passive,
                gameplayEffect = "Jika karakter tidak menerima damage sejak awal giliran sebelumnya, direct-damage attack berikutnya memperoleh +5% Damage Bonus.",
                designIntent = "Menghadiahi pengaturan giliran dan perlindungan terhadap damage masuk.",
                description = "Jika karakter tidak menerima damage sejak awal giliran sebelumnya, direct-damage attack berikutnya memperoleh +5% Damage Bonus.",
                demerit = "— (tidak memiliki demerit)",
                buyPrice = 1600,
                sellPrice = 800
            },
            new RelicDef
            {
                id = "R018",
                name = "Whispering Reliquary",
                category = RelicCategory.Exploration,
                proposedRarity = RelicProposedRarity.Rare,
                triggerType = RelicTriggerType.Event,
                activationLimit = RelicActivationLimit.Passive,
                gameplayEffect = "Pada event bertag relic atau cursed_reward, tampilkan satu indikator risiko tambahan jika data risiko tersedia.",
                designIntent = "Memperkuat Knowledge Is Power dan membantu pemain menilai trade-off.",
                description = "Pada event bertag relic atau cursed_reward, tampilkan satu indikator risiko tambahan jika data risiko tersedia.",
                demerit = "— (tidak memiliki demerit)",
                buyPrice = 1400,
                sellPrice = 700
            },
            new RelicDef
            {
                id = "R019",
                name = "Black-Iron Rosary",
                category = RelicCategory.Defensive,
                proposedRarity = RelicProposedRarity.Rare,
                triggerType = RelicTriggerType.Qte,
                activationLimit = RelicActivationLimit.Passive,
                gameplayEffect = "Setelah karakter berhasil melakukan Perfect Defensive QTE, karakter tersebut memperoleh Shield sebesar 1.5% Max HP.",
                designIntent = "Memberi hadiah kecil atas eksekusi defensif yang baik.",
                description = "Setelah karakter berhasil melakukan Perfect Defensive QTE, karakter tersebut memperoleh Shield sebesar 1.5% Max HP.",
                demerit = "— (tidak memiliki demerit)",
                buyPrice = 1600,
                sellPrice = 800
            },
            new RelicDef
            {
                id = "R020",
                name = "Sovereign's Last Breath",
                category = RelicCategory.Resource,
                proposedRarity = RelicProposedRarity.Epic,
                triggerType = RelicTriggerType.Boss,
                activationLimit = RelicActivationLimit.OncePerBattle,
                gameplayEffect = "Ketika Boss pertama kali mencapai 25% HP atau kurang, setiap karakter party yang masih hidup memulihkan 2% Max MP.",
                designIntent = "Memberi sedikit ruang untuk menggunakan skill pada fase akhir boss.",
                description = "Ketika Boss pertama kali mencapai 25% HP atau kurang, setiap karakter party yang masih hidup memulihkan 2% Max MP.",
                demerit = "— (tidak memiliki demerit)",
                buyPrice = 0,
                sellPrice = 1500
            },
            new RelicDef
            {
                id = "R021",
                name = "Coffin Nail",
                category = RelicCategory.Offensive,
                proposedRarity = RelicProposedRarity.Rare,
                triggerType = RelicTriggerType.Combat,
                activationLimit = RelicActivationLimit.Passive,
                gameplayEffect = "Party memberikan +7% Damage Bonus terhadap musuh yang sedang Broken.",
                designIntent = "Mendorong party memanfaatkan jendela serangan setelah Break.",
                description = "Party memberikan +7% Damage Bonus terhadap musuh yang sedang Broken.",
                demerit = "— (tidak memiliki demerit)",
                buyPrice = 1900,
                sellPrice = 950
            },
            new RelicDef
            {
                id = "R022",
                name = "Zoroa's Dull Crown",
                category = RelicCategory.Offensive,
                proposedRarity = RelicProposedRarity.Rare,
                triggerType = RelicTriggerType.Combat,
                activationLimit = RelicActivationLimit.StackTrigger,
                gameplayEffect = "Setelah party mengalahkan tiga musuh dalam satu battle, party memperoleh +3% All Damage Bonus sampai battle berakhir.",
                designIntent = "Memberi momentum pada encounter panjang dan mempertahankan identitas Zoroa.",
                description = "Setelah party mengalahkan tiga musuh dalam satu battle, party memperoleh +3% All Damage Bonus sampai battle berakhir.",
                demerit = "— (tidak memiliki demerit)",
                buyPrice = 1500,
                sellPrice = 750
            },
            new RelicDef
            {
                id = "R023",
                name = "Crimson Testament",
                category = RelicCategory.Offensive,
                proposedRarity = RelicProposedRarity.Rare,
                triggerType = RelicTriggerType.Combat,
                activationLimit = RelicActivationLimit.Passive,
                gameplayEffect = "Seluruh party memperoleh +18% Damage Bonus untuk semua direct-damage attack.",
                designIntent = "Pilihan ofensif berisiko tinggi untuk mempercepat semua encounter.",
                description = "Seluruh party memperoleh +18% Damage Bonus untuk semua direct-damage attack.",
                demerit = "Seluruh party menerima +12% All Damage Taken.",
                buyPrice = 2300,
                sellPrice = 1150
            },
            new RelicDef
            {
                id = "R024",
                name = "Hollow Saint's Aegis",
                category = RelicCategory.Defensive,
                proposedRarity = RelicProposedRarity.Rare,
                triggerType = RelicTriggerType.Combat,
                activationLimit = RelicActivationLimit.Passive,
                gameplayEffect = "Seluruh party menerima 22% lebih sedikit All Damage Taken.",
                designIntent = "Membangun party yang sangat tahan lama, dengan konsekuensi tempo damage yang lebih lambat.",
                description = "Seluruh party menerima 22% lebih sedikit All Damage Taken.",
                demerit = "Seluruh party memberikan 15% lebih sedikit Damage Bonus.",
                buyPrice = 2300,
                sellPrice = 1150
            },
            new RelicDef
            {
                id = "R025",
                name = "Arcanum Unbound",
                category = RelicCategory.Hybrid,
                proposedRarity = RelicProposedRarity.Epic,
                triggerType = RelicTriggerType.Combat,
                activationLimit = RelicActivationLimit.Passive,
                gameplayEffect = "Skill yang menghasilkan direct damage memperoleh +25% Damage Bonus.",
                designIntent = "Mendorong build yang berfokus pada skill dan burst damage.",
                description = "Skill yang menghasilkan direct damage memperoleh +25% Damage Bonus.",
                demerit = "Biaya MP seluruh Skill meningkat 20%.",
                buyPrice = 0,
                sellPrice = 2000
            },
            new RelicDef
            {
                id = "R026",
                name = "The Empty Chalice",
                category = RelicCategory.Hybrid,
                proposedRarity = RelicProposedRarity.Rare,
                triggerType = RelicTriggerType.Combat,
                activationLimit = RelicActivationLimit.Passive,
                gameplayEffect = "Biaya MP seluruh Skill berkurang 30%.",
                designIntent = "Memungkinkan party lebih sering menggunakan skill dan memperpanjang kapasitas tempur.",
                description = "Biaya MP seluruh Skill berkurang 30%.",
                demerit = "HP yang dipulihkan dari Skill dan HP Consumable berkurang 25%; tidak memengaruhi Temple Recovery, efek pemulihan Relic, atau healing Racial Feat yang memiliki aturan khusus.",
                buyPrice = 2200,
                sellPrice = 1100
            },
            new RelicDef
            {
                id = "R027",
                name = "Breaker’s Due",
                category = RelicCategory.Break,
                proposedRarity = RelicProposedRarity.Rare,
                triggerType = RelicTriggerType.Combat,
                activationLimit = RelicActivationLimit.Passive,
                gameplayEffect = "Seluruh party memperoleh +30% Break Power.",
                designIntent = "Membangun party yang fokus memecahkan pertahanan musuh dan membuka jendela Broken lebih cepat.",
                description = "Seluruh party memperoleh +30% Break Power.",
                demerit = "Seluruh party memberikan 12% lebih sedikit Damage Bonus.",
                buyPrice = 2000,
                sellPrice = 1000
            },
            new RelicDef
            {
                id = "R028",
                name = "Wound-Crowned Idol",
                category = RelicCategory.Offensive,
                proposedRarity = RelicProposedRarity.Epic,
                triggerType = RelicTriggerType.Combat,
                activationLimit = RelicActivationLimit.Passive,
                gameplayEffect = "Saat HP karakter berada pada 50% atau kurang, karakter tersebut memperoleh +25% Damage Bonus.",
                designIntent = "Mendorong gaya bermain berisiko yang memanfaatkan kondisi HP rendah untuk menghasilkan damage besar.",
                description = "Saat HP karakter berada pada 50% atau kurang, karakter tersebut memperoleh +25% Damage Bonus.",
                demerit = "Max HP seluruh karakter party berkurang 12% selama relic dimiliki.",
                buyPrice = 0,
                sellPrice = 2000
            },
            new RelicDef
            {
                id = "R029",
                name = "Bell of the Last Stand",
                category = RelicCategory.Offensive,
                proposedRarity = RelicProposedRarity.Epic,
                triggerType = RelicTriggerType.Combat,
                activationLimit = RelicActivationLimit.Passive,
                gameplayEffect = "Ketika minimal dua anggota party berada dalam kondisi Down, seluruh karakter party yang masih aktif memperoleh +25% Damage Bonus.",
                designIntent = "Memberi peluang comeback ofensif ketika party berada di ambang kekalahan.",
                description = "Ketika minimal dua anggota party berada dalam kondisi Down, seluruh karakter party yang masih aktif memperoleh +25% Damage Bonus.",
                demerit = "Selama efek aktif, karakter party yang masih aktif menerima +15% All Damage Taken.",
                buyPrice = 0,
                sellPrice = 1800
            },
            new RelicDef
            {
                id = "R030",
                name = "The Butcher's Ledger",
                category = RelicCategory.Offensive,
                proposedRarity = RelicProposedRarity.Epic,
                triggerType = RelicTriggerType.Combat,
                activationLimit = RelicActivationLimit.StackTrigger,
                gameplayEffect = "Setiap kali party mengalahkan musuh, seluruh party memperoleh 6% Damage Bonus tambahan. Bonus dapat menumpuk hingga 4 kali dan bertahan sampai battle berakhir.",
                designIntent = "Mengubah encounter multi-musuh menjadi momentum ofensif yang semakin kuat.",
                description = "Setiap kali party mengalahkan musuh, seluruh party memperoleh 6% Damage Bonus tambahan. Bonus dapat menumpuk hingga 4 kali dan bertahan sampai battle berakhir.",
                demerit = "Setiap stack juga meningkatkan All Damage Taken party sebesar 4% (maksimal +16% saat 4 stack).",
                buyPrice = 0,
                sellPrice = 1800
            },
            new RelicDef
            {
                id = "R031",
                name = "Pilgrim's Burden",
                category = RelicCategory.Economy,
                proposedRarity = RelicProposedRarity.Rare,
                triggerType = RelicTriggerType.Reward,
                activationLimit = RelicActivationLimit.Passive,
                gameplayEffect = "EXP yang diperoleh party selama dungeon meningkat 15%.",
                designIntent = "Mempercepat perkembangan karakter dengan mengorbankan pendapatan Run Gold.",
                description = "EXP yang diperoleh party selama dungeon meningkat 15%.",
                demerit = "Run Gold yang diperoleh selama dungeon berkurang 15%.",
                buyPrice = 2800,
                sellPrice = 1400
            },
            new RelicDef
            {
                id = "R032",
                name = "Merchant's Burden",
                category = RelicCategory.Economy,
                proposedRarity = RelicProposedRarity.Rare,
                triggerType = RelicTriggerType.Reward,
                activationLimit = RelicActivationLimit.Passive,
                gameplayEffect = "Run Gold yang diperoleh party selama dungeon meningkat 15%.",
                designIntent = "Memperkuat ekonomi ekspedisi dan kemampuan membeli kebutuhan run, dengan mengorbankan perkembangan karakter.",
                description = "Run Gold yang diperoleh party selama dungeon meningkat 15%.",
                demerit = "EXP yang diperoleh party selama dungeon berkurang 15%.",
                buyPrice = 2800,
                sellPrice = 1400
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
