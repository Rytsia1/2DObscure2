# GAME DESIGN DOCUMENT — FELNA

**Party-Based Turn-Based Roguelike RPG · *The World's Between***

| Item | Keterangan |
| --- | --- |
| Dokumen | GDD Terpadu v1.0 |
| Tanggal | 4 November 2026 |
| Status | Konsolidasi seluruh dokumen desain project ke dalam satu GDD. Isi bab mengikuti versi terbaru setiap dokumen sumber setelah sinkronisasi QTE, equipment, dan race pada November 2026. |
| Cakupan | Visi dan core loop, dunia dan cerita, karakter (character creation, race, background, class, talent), combat (struktur, damage dan status, QTE), item dan ekonomi, presentasi (art, style guide, UI, audio), serta status proyek |

## Ringkasan Game

| Aspek | Keputusan |
| --- | --- |
| Genre | Party-based turn-based roguelike RPG |
| Dunia | Felna; kerajaan terakhir Emerose, dengan hub di Geistwald City |
| Party | 4 karakter |
| Karakter | 7 race, 9 class, 10 background, 6 Ability Score |
| Struktur | 8 dungeon berurutan (8 layer The World's Between), masing-masing dengan boss |
| Difficulty | Hardcore Tactical Survival: party wipe mengakhiri run |
| Combat | Giliran individual per karakter dengan reaction-based QTE ofensif dan defensif |
| Equipment | 8 slot, 6 rarity, random affix, Legendary dan Mythic Effect |
| Ekonomi | Gold berbasis run, Dungeon Shop, dan fasilitas hub |
| Visual | Modern pixel art, anime JRPG, dark fantasy; kamera combat side-view |

## Cara Membaca Dokumen Ini

- Setiap bab berasal dari satu dokumen sumber, ditulis di bawah judul bab. Lampiran A memetakan semua sumber.
- Penomoran di dalam bab mengikuti dokumen sumbernya. Rujukan "§x" di dalam sebuah bab menunjuk ke bab itu sendiri; rujukan ke bab lain ditulis "Bab N §x".
- Beberapa bab berbahasa Inggris karena dokumen sumbernya berbahasa Inggris. Isinya tidak diterjemahkan agar maknanya tidak bergeser.
- Penanda status dari dokumen sumber (TBD, OPEN, RECOMMENDED, CONFIRMED, *tuning*) dipertahankan. Bab 19 merangkum konflik, asumsi, dan isu yang masih terbuka.
- Jika dua bab tampak berbeda, bab yang lebih spesifik berlaku. Contoh: aturan QTE di Bab 12 berlaku di atas ringkasan QTE di bab lain, dan nilai race di Bab 6 berlaku di atas ringkasan di bab lain.

## Daftar Isi

- **Bagian I — Visi dan Struktur Game**
    - Bab 1 — High Concept dan Pilar Desain
    - Bab 2 — Core Loop, Dungeon, dan Run
    - Bab 3 — Difficulty System
- **Bagian II — Dunia dan Cerita**
    - Bab 4 — Story dan World Lore: Felna
- **Bagian III — Karakter**
    - Bab 5 — Character Creation
    - Bab 6 — Race System
    - Bab 7 — Background System
    - Bab 8 — Class System
    - Bab 9 — Talents System
- **Bagian IV — Combat**
    - Bab 10 — Combat Structure dan Broken
    - Bab 11 — Damage dan Status Effect
    - Bab 12 — QTE System
- **Bagian V — Item dan Ekonomi**
    - Bab 13 — Inventory dan Equipment
    - Bab 14 — Shop dan Run Economy
- **Bagian VI — Presentasi**
    - Bab 15 — Art Direction dan Visual Production
    - Bab 16 — Visual Style Guide
    - Bab 17 — UI/UX Design
    - Bab 18 — Audio Direction
- **Bagian VII — Status Proyek**
    - Bab 19 — Konflik, Asumsi, dan Isu Terbuka
    - Lampiran A — Peta Dokumen Sumber
    - Lampiran B — Riwayat Sinkronisasi November 2026

---

# Bagian I — Visi dan Struktur Game

## Bab 1 — High Concept dan Pilar Desain

> Sumber: GDD Visual Direction v0.2 §1.

### 1. High Concept

Party-based turn-based roguelike RPG. Pemain membentuk party empat karakter, menjelajahi dungeon melalui node map, mengambil keputusan taktis dalam combat berbasis giliran dengan reaction QTE, mengembangkan karakter, dan mengelola Gold selama run. Progres dungeon berlangsung secara sequential melalui delapan dungeon.

|**Elemen**|**Deskripsi**|
|---|---|
|**Genre**|Party-based turn-based roguelike RPG|
|**Party**|4 karakter|
|**Combat**|Turn-based individual character turns + reaction QTE|
|**Level cap**|15|
|**Dungeon progression**|8 dungeon, Sequential Progression|
|**Run currency**|Gold hanya berlaku dalam run|
|**Failure**|Kembali ke Main Menu;harus New Game|

#### 1.1 Pilar Desain

- Combat taktis dengan pilihan aksi dan target yang bermakna.

- Identitas class kuat dengan build yang dibentuk lewat skill, Rank, dan equipment.

- Keputusan roguelike dari node, shop acak, dan sumber daya run-based.

- Risiko run nyata: kekalahan mengakhiri ekspedisi.

- Feedback combat yang jelas untuk damage type, status, resistance, dan QTE.

## Bab 2 — Core Loop, Dungeon, dan Run

> Sumber: GDD Visual Direction v0.2 §2 dan §9.

### 2. Core Loop dan Struktur Dungeon

|**Tahap**|**Aktivitas**|
|---|---|
|**New Game**|Mulai ekspedisi baru.|
|**Persiapan**|Party/loadout/equipment; lokasi dan detail preparation masih<br>TBD.|
|**Dungeon Map**|Pilih node untuk maju.|
|**Node**|Combat, event, shop, reward atau tipe node lain; daftar final<br>TBD.|
|**Progression**|EXP/SP, Gold, item; reward table TBD.|
|**Dungeon Clear**|Pertahankan progres run dan lanjut ke dungeon berikutnya.|
|**Final Clear**|Menyelesaikan dungeon ke-8 dan run.|
|**Run Failure**|Run dihapus,kembali ke Main Menu;New Game diperlukan.|

#### 2.1 Sequential Progression

Delapan dungeon diselesaikan dalam urutan yang telah ditentukan. Tidak ada pemilihan urutan dungeon. Tema, layout, node count, encounter, boss, dan reward per dungeon masih TBD.

#### 2.2 Gold dan persistensi antar dungeon

- Gold yang tersisa setelah menyelesaikan dungeon tetap tersedia selama run berlanjut.

- Gold dihapus ketika run berakhir.

- Detail persistensi level, SP, equipment, consumables, dan status party antar dungeon perlu dikonfirmasi; asumsi kerja: party berlanjut dalam run sukses yang sama.

### 9. Run Failure / Victory

#### 9.1 Failure

- Run gagal/kalah mengakhiri ekspedisi.

- Pemain kembali ke Main Menu.

- Dungeon/run state reset; harus New Game untuk mencoba lagi.

- Sisa Gold run dihapus.

- Checkpoint/resume belum disepakati.

#### 9.2 Victory

Dungeon clear melanjutkan progres ke dungeon berikutnya. Dungeon ke-8 menyelesaikan ekspedisi. Ending, victory screen, run summary, dan meta-progression belum ditentukan.

#### 9.3 Meta progression

Belum diputuskan apakah ada unlock permanen atau progres antar-run. Jangan mengasumsikan persistent upgrades sebelum disepakati.

## Bab 3 — Difficulty System

> Sumber: Difficulty System v0.2.

### 1. Design Overview

The game uses a **Hardcore Tactical Survival** difficulty direction.
Combat is intended to reward tactical decisions, resource management,
party composition, and careful navigation through dungeon nodes. A
failed run carries a meaningful cost: the expedition ends and the player
must begin a new game.

The difficulty is built around the following principles:

-   **Tactical combat:** Turn-based individual character actions with
    reaction QTEs.
-   **Resource pressure:** HP, MP, healing supplies, and run Gold must
    be managed.
-   **Roguelike risk:** Run failure ends the current expedition.
-   **Sequential challenge:** The player must clear all 8 dungeons in a
    fixed order.
-   **Successful progression persists:** Clearing a dungeon allows the
    current run to continue to the next dungeon.

This document records the rules currently established. Unconfirmed
values and details are labeled **TBD** rather than assumed.

### 2. Difficulty Identity

  Element                Current Design
  ---------------------- --------------------------------------------------
  Difficulty direction   Hardcore Tactical Survival
  Party size             4 characters
  Combat format          Turn-based, individual character turns
  Reaction mechanic      QTE
  Dungeon count          8
  Dungeon progression    Sequential; fixed order
  Run currency           Gold is run-based
  Failure                Run ends; return to Main Menu; New Game required

### 3. Core Difficulty Rules

#### 3.1 Combat and Party Survival

-   Combat is turn-based, with individual character turns.
-   Reaction QTEs are part of combat.
-   The party consists of 4 characters.
-   Character HP and MP are important combat resources.
-   The detailed HP 0 / Character Down / revival / recovery rules are
    maintained in the Combat System and remain subject to their defined
    specifications.

#### 3.2 Character Down and Party Wipe

-   If one or more party members are down, battle can continue as long
    as at least one party member remains active.
-   If the entire party is defeated, the run is considered failed.
-   A party wipe ends the current run and returns the player to the Main
    Menu.
-   The player must select **New Game** to begin another expedition.

> **Important:** A party wipe is not a checkpoint reload. The failed run
> is reset.

### 4. Dungeon Progression

#### 4.1 Sequential Progression

The expedition consists of **8 dungeons**, cleared in a predetermined
sequence.

-   Dungeon order is fixed.
-   The player cannot choose or skip the order of dungeons.
-   Clearing a dungeon advances the current run to the next dungeon.
-   Clearing Dungeon 8 completes the expedition.

Dungeon themes, layouts, node counts, encounter rosters, bosses, and
reward tables are **TBD**.

#### 4.2 Dungeon Clear

When a dungeon is successfully cleared:

-   The run continues rather than ending.
-   Remaining Gold is retained for the next dungeon.
-   The player advances to the next dungeon in the fixed sequence.
-   The party continues as part of the same successful expedition.

The exact persistence rules for character levels, SP, equipment,
consumables, and party HP/MP between cleared dungeons require
confirmation unless specified in their respective system documents.

#### 4.3 Run Failure

When the party is wiped:

-   The current expedition ends.
-   The current dungeon progression is reset.
-   Run Gold is lost.
-   The player returns to the Main Menu.
-   A new expedition requires **New Game**.

There is no continuation of the failed run from the current dungeon or a
checkpoint.

### 5. Run Economy and Resource Pressure

#### 5.1 Gold

Gold is a run-based currency.

-   Gold retained after a successful dungeon clear remains available
    during the same run.
-   Gold is removed when the run ends.
-   Gold does not persist after a failed expedition.

Shop pricing, sell values, refresh costs, and stock generation are
defined in the Shop System document and may contain additional rules.

#### 5.2 HP, MP, and Recovery

HP and MP management are central to the survival difficulty.

-   MP is spent to use Active Skills.
-   Healing and recovery opportunities are limited by the run's
    available resources and systems.
-   Recovery Camp and other recovery-node details should follow their
    dedicated design specifications.
-   Exact recovery amounts, availability, and whether resources are
    restored between dungeons are **TBD** unless explicitly specified
    elsewhere.

### 6. Run Loop

The intended high-level loop is:

1.  Select **New Game** to start an expedition.
2.  Prepare the party and its loadouts.
3.  Navigate the dungeon node map.
4.  Resolve nodes, such as combat, events, shops, and rewards.
5.  Manage party HP, MP, items, and Gold.
6.  Clear the dungeon to retain the run and advance.
7.  Repeat through the fixed sequence of 8 dungeons.
8.  Clear Dungeon 8 to complete the run.

If the party is wiped at any point, the loop ends in failure and the
player must start a New Game.

### 7. Difficulty Feedback and Player Clarity

The game should communicate the consequences of the Hardcore Tactical
Survival rules clearly.

-   Show current dungeon number and overall progress through the
    8-dungeon sequence.
-   Clearly communicate that Gold is run-based.
-   Communicate party member Down states and party-wipe danger.
-   Make the difference between a dungeon clear and a run failure clear.
-   Show node rewards and costs before the player commits when
    applicable.
-   Clearly indicate when the player must return to Main Menu and start
    a New Game after failure.

#### 7.1 QTE Difficulty and Assist Settings

QTE difficulty comes from two visible variables: timing window width per
enemy tier and cursor speed per dungeon group.

  ------------------------------------------------------------------------
  Tier         Perfect window   Good window   Lead time (Dungeon 1--3)
  ------------ ---------------- ------------- ----------------------------
  Normal       90 ms            240 ms        1000 ms

  Elite        80 ms            210 ms        900 ms

  Boss         72 ms            180 ms        800 ms
  ------------------------------------------------------------------------

-   Lead time multiplier: Dungeon 1--3 ×1.0, Dungeon 4--6 ×0.9,
    Dungeon 7--8 ×0.8. Window width does not change between dungeons.
-   Heavy attacks: Defensive Perfect reduces only 50% of damage. Heavy
    attacks are always telegraphed.
-   Enemy design constraints: maximum 6 hits per attack; per-hit status
    on at most 3 hits; per-target coefficient of multi-target attacks at
    most 60% of an equivalent single-target attack; base damage per hit
    at least 5.
-   Boss constraints: at most one attack with a Heavy marker per round;
    no Stun and Heavy attack on the same target in the same round; one
    signature attack and one phase change at 50% HP.
-   Assist settings (Auto-Good, window ×1.25/×1.5, speed ×1.25/×1.5,
    early-press tolerance) do not lock any content. They are recorded as
    "Assist" on the run summary. Visual and audio cue options are not
    recorded.

Full rules: Bab 12 (QTE System).

### 8. Out of Scope / Open Design Questions

The following details are not defined by this document and should not be
invented as part of the difficulty rules:

1.  Enemy stat scaling by dungeon or node.
2.  Exact enemy AI, targeting, and encounter composition.
3.  Exact HP and MP restoration between battles or between dungeons.
4.  Exact Recovery Camp functionality, costs, and limits.
5.  Whether level, SP, equipment, consumables, and character status
    persist between dungeon clears.
6.  Exact Character Down, revival, and recovery mechanics.
7.  Whether any meta-progression or unlocks persist after a failed run.
8.  Dungeon-specific reward tables and boss difficulty curves.

These should be decided in the relevant systems or through a dedicated
balancing pass.

------------------------------------------------------------------------

### Change Log

  -----------------------------------------------------------------------
  Version                             Notes
  ----------------------------------- -----------------------------------
  0.1                                 Initial Difficulty System draft
                                      assembled from current project
                                      decisions. Records Hardcore
                                      Tactical Survival, 8 sequential
                                      dungeons, run-based Gold,
                                      successful dungeon continuation,
                                      and full run failure requiring New
                                      Game.

  0.2                                 QTE sync: QTE difficulty scaling,
                                      enemy and boss design
                                      constraints, and assist settings
                                      (§7.1).

  -----------------------------------------------------------------------

# Bagian II — Dunia dan Cerita

## Bab 4 — Story dan World Lore: Felna

> Sumber: Felna — Game Story & World Lore. Delapan layer di bab ini adalah delapan dungeon pada Bab 2.

### Document Overview

Dokumen ini merangkum cerita utama dan lore yang telah ditetapkan dalam diskusi proyek. Dokumen berfokus pada dunia, latar sejarah, krisis Emerose, The World's Between, serta delapan layer dan penguasanya. Detail yang belum diputuskan tidak ditambahkan sebagai fakta canon.

Status: Canon reference — konsep dan fakta yang telah disepakati.

Catatan: Nama tematik layer yang belum disahkan sebagai nama resmi dicantumkan sebagai deskripsi, bukan nama proper.

### 1. Dunia Felna

Felna adalah dunia yang berada dalam keadaan kehancuran. Pada tahun 1631 AR (After Reason), sekitar 90% wilayah Felna telah hancur atau berada di bawah kendali daemon. Kerajaan Emerose menjadi satu-satunya kerajaan aman yang masih tersisa.

Emerose pada awalnya merupakan kerajaan manusia. Ketika kehancuran akibat daemon meluas dan berbagai tanah asal ras lain jatuh, Emerose menerima para pengungsi dari berbagai ras. Kini, kerajaan tersebut menjadi tempat perlindungan bagi ras-ras yang masih bertahan.

Masing-masing ras memiliki wilayah asal, budaya, tradisi, dan identitasnya sendiri. Namun, sebagian besar wilayah asal tersebut telah hilang atau dikuasai daemon. Detail mengenai pembagian wilayah, representasi politik, dan kehidupan antarras di Emerose belum ditetapkan.

### 2. Emerose dan Geistwald City

Emerose merupakan pusat perlindungan terakhir bagi penduduk Felna. Kota Geistwald adalah kota utama yang menjadi hub cerita. Di kota inilah portal menuju The World's Between muncul setiap bulan purnama.

Masyarakat Emerose hidup di tengah ancaman daemon dan keterbatasan sumber daya. Keberlangsungan kerajaan bergantung pada kemampuan mereka mempertahankan kehidupan di wilayah yang masih aman.

### 3. The World's Between

The World's Between adalah dunia misterius yang menjadi penghubung antara Felna dan Endworld, dunia asal para daemon. Portal menuju wilayah ini muncul secara tiba-tiba di Geistwald City pada tahun 750 AR.

Setiap kali portal muncul, pengaruh daemon di Felna menguat. Meskipun berbahaya, The World's Between menjadi sumber daya penting bagi Emerose, termasuk bahan pangan dan material untuk membuat senjata. Lingkungan di dalamnya berubah secara dinamis.

The World's Between terbagi menjadi delapan dungeon atau layer yang harus dijelajahi secara berurutan. Di setiap layer terdapat penguasa yang menjadi final boss. Pada layer terdalam, terdapat sarana untuk menutup koneksi antara Felna dan Endworld secara permanen.

### 4. Sejarah Ekspedisi Legendaris

Pada tahun 1076 AR, lima petualang legendaris berhasil mencapai Layer 8, yang dipercaya sebagai ujung The World's Between. Setelah mencapai wilayah tersebut, kelima petualang menghilang.

Tidak ditemukan jejak, laporan kematian, ataupun pesan yang menjelaskan nasib mereka. Keberadaan dan nasib kelima petualang tersebut tetap menjadi misteri.

### 5. Krisis Sumber Daya Emerose — 1631 AR

Pada tahun 1631 AR, Emerose menghadapi krisis sumber daya. Karena sebagian besar Felna telah hancur atau dikuasai daemon, kerajaan semakin kesulitan memenuhi kebutuhan pangan dan kebutuhan hidup penduduknya. The World's Between menjadi salah satu sumber daya yang masih dapat dijangkau, meskipun ekspedisi ke sana sangat berbahaya.

#### Peraturan Ekspedisi Kerajaan

Setiap keluarga di Emerose diwajibkan mengirimkan setidaknya satu orang anggota keluarga untuk bergabung dalam ekspedisi ke The World's Between.

Setiap peserta ekspedisi memiliki kuota sumber daya yang perlu dipenuhi. Besaran kuota ditentukan berdasarkan kemampuan peserta dan tingkat ekspedisi.

Dari sumber daya yang diperoleh, 70% diambil oleh kerajaan dan 30% sisanya boleh disimpan oleh peserta.

Sumber daya yang dikumpulkan kerajaan digunakan untuk memenuhi kebutuhan pangan dan kehidupan seluruh penduduk Emerose.

Peraturan ini menjadikan ekspedisi sebagai kewajiban sosial yang berkaitan langsung dengan kelangsungan hidup Emerose. Rincian administratif tambahan dari sistem kuota dan distribusi belum ditetapkan.

### 6. Delapan Layer dan Para Penguasa

Setiap layer memiliki lingkungan, atmosfer, dan identitas yang mencerminkan boss yang menguasainya. Deskripsi di bawah merangkum konsep wilayah yang telah disepakati.

| **Layer** | **Boss** | **Identitas Wilayah** | **Lore Ringkas** |
| --- | --- | --- | --- |
| **Layer 1** | Zoroa The Harvester of Soul | Kerajaan kuno yang runtuh (Ruined Kingdom) | Wilayah berupa kerajaan kuno yang telah runtuh. Zoroa mengumpulkan jiwa manusia dan ras lain dari Felna, serta mampu mengambil jiwa makhluk hidup secara paksa. |
| **Layer 2** | Lilia The Lady of Pain | Penjara raksasa yang terlupakan (The Forgotten Prison) | Wilayah berupa penjara raksasa yang telah terlupakan. Konsepnya berpusat pada kurungan dan penderitaan; asal-usul penjara dan identitas para tahanan belum ditetapkan. |
| **Layer 3** | Akasuna The False God's Champion | Colosseum raksasa | Wilayah menyerupai colosseum besar, dengan identitas arena pertarungan. Detail mengenai tujuan arena dan hubungan Akasuna dengan gelar False God's Champion belum ditetapkan. |
| **Layer 4** | Pharam The Archmage of Apocalypse | Dunia yang hancur akibat sihir | Wilayah berupa lanskap kehancuran magis. Reruntuhan dan kehancuran mencerminkan kekuatan Pharam sebagai Archmage of Apocalypse. Nama formal layer belum ditetapkan. |
| **Layer 5** | Berzelius Fallen Angel of the Golden Land | Negeri keemasan (The Golden Land) | Wilayah megah dengan identitas negeri keemasan. Kontras antara kemegahan dan kejatuhan tercermin pada gelar Berzelius. Detail sejarah Golden Land belum ditetapkan. |
| **Layer 6** | Lacrima The Mistress of All Evil | Tanah yang tercemar kekuatan jahat | Wilayah yang telah tercemar dan berubah menjadi sesuatu yang tidak lagi alami. Identitasnya berpusat pada korupsi dan kejahatan. |
| **Layer 7** | Vortigern The Dragon King of Calamity | Wilayah pegunungan dan sarang naga | Konsep wilayah berupa pegunungan besar dan sarang naga, sesuai dengan identitas Vortigern sebagai Dragon King of Calamity. |
| **Layer 8** | Helelios The Monarch of Endworld | Dunia asal para daemon — Endworld | Wilayah terakhir adalah Endworld, dunia asal para daemon. Helelios adalah raja Endworld dan berbeda dari tujuh penguasa layer sebelumnya. |

Catatan canon: Sebutan seperti “The Shattered Realm” dan “The Blighted Realm” pernah muncul sebagai usulan nama, tetapi belum disahkan sebagai nama resmi layer. Yang disepakati adalah konsep identitas wilayahnya.

### 7. Tujuan Besar Cerita

Tujuan utama perjalanan melalui delapan layer adalah menemukan cara untuk menutup koneksi antara Felna dan Endworld secara permanen. Sarana untuk melakukannya berada di layer terdalam The World's Between.

Perjalanan ini berlangsung di tengah krisis Emerose: ekspedisi diperlukan untuk memperoleh sumber daya demi kelangsungan hidup penduduk, sementara keberhasilan mencapai kedalaman The World's Between berpotensi mengakhiri hubungan yang memungkinkan ancaman daemon terus menjangkau Felna.

### 8. Hal yang Belum Ditentukan

Identitas, motivasi, dan latar belakang protagonis.

Peristiwa personal yang mengawali perjalanan protagonis pada tahun 1631 AR, di luar konteks krisis sumber daya dan peraturan ekspedisi.

Sejarah rinci masing-masing layer dan asal-usul wilayahnya.

Apakah tujuh penguasa pertama secara langsung melayani Helelios atau memiliki tujuan masing-masing.

Asal-usul dan makna gelar The False God's Champion milik Akasuna, serta hubungan apa pun dengan dewa Caelum.

Nasib lima petualang legendaris yang mencapai Layer 8 pada tahun 1076 AR.

Nama resmi setiap layer selain konsep wilayah yang telah disepakati.

### 9. Ringkasan Canon

Felna berada di ambang kehancuran, dan Emerose adalah kerajaan aman terakhir. Portal yang muncul di Geistwald City menghubungkan Felna dengan The World's Between, jalur berbahaya menuju Endworld. Pada 1631 AR, krisis sumber daya mendorong kerajaan mewajibkan setiap keluarga mengirimkan anggota ke ekspedisi. Delapan layer The World's Between dikuasai oleh delapan boss, masing-masing dengan identitas wilayah berbeda. Di kedalaman terdalam terdapat sarana untuk memutus koneksi Felna–Endworld secara permanen.

# Bagian III — Karakter

## Bab 5 — Character Creation

> Sumber: Character Creation System — Design Summary, dengan tambahan GDD v0.2 §3.1.

### 1. Character Creation Overview

Players create characters before beginning an expedition.

-   The party consists of **4 characters**.
-   Each character has a Race, Class, Ability Scores, Skills, and
    Equipment.
-   There is no appearance customization.
-   Character creation focuses on gameplay builds rather than visual
    customization.
-   Builds are shaped by Race, Class, Ability Scores, Skill Loadout, and
    Equipment.

### 2. Race dan Class

Race dijelaskan lengkap di Bab 6 dan class di Bab 8. Bagian Class pada dokumen sumber (§4) dipindahkan ke Bab 8, sehingga penomoran bab ini melompat dari §3 ke §5. Bagian ini hanya memuat aturan Character Creation yang tidak dibahas di kedua bab tersebut.

### 3. Ability Score System

The game uses a **hybrid Ability Score system** inspired by D&D, adapted
for RPG formulas and progression.

There are six Ability Scores:

  -----------------------------------------------------------------------
  Ability                             General Role
  ----------------------------------- -----------------------------------
  STR --- Strength                    Physical Power, melee damage, and
                                      physical strength

  DEX --- Dexterity                   Accuracy, Evasion, agility, and
                                      ranged combat

  VIT --- Vitality                    HP, durability, and resistance

  INT --- Intelligence                Magic Power and magical knowledge

  WIS --- Wisdom                      Healing, perception, and spiritual
                                      abilities

  CHA --- Charisma                    Social abilities, certain magical
                                      effects, and class mechanics
  -----------------------------------------------------------------------

No Ability Score affects QTE timing windows or grades; QTE measures
player execution, while Ability Scores measure character power.

#### Stat Allocation

-   Players allocate Ability Scores using **Point Buy**.
-   Ability Scores contribute to derived stats.
-   Races provide Ability Score Bonuses.
-   Classes define gameplay identity and skill access.

Racial bonuses and score limits (15 before racial bonus, 17 after, 20
through progression) are defined in Bab 6 (Race System). The Point Buy
budget and derived-stat formulas are not finalized.

### 5. Skill System

The game uses a **Skill Unlock + Loadout** system.

  -----------------------------------------------------------------------
  Feature                             Decision
  ----------------------------------- -----------------------------------
  Active Skill Slots                  3

  Passive Skill Slots                 3

  Skill Unlocking                     Hybrid: character levels unlock
                                      tiers; Skill Points unlock
                                      individual skills

  Loadout Changes                     At the Hub or Camp

  Skill Upgrades                      Rank-based

  Starter Passive                     At least one passive is unlocked at
                                      character creation
  -----------------------------------------------------------------------

#### Skill Progression

-   Character levels unlock access to higher skill tiers.
-   Skill Points are used to unlock individual skills.
-   Unlocked skills can be equipped in the character's loadout.
-   Active and Passive Skills use separate slots.
-   Skills can be upgraded through Ranks.

#### Combat Resources

Combat uses only two resource pools:

-   **HP**
-   **MP**

Classes do not have additional class-specific resource pools such as
Rage, Combo Points, or Charges. Unique class mechanics may use statuses,
triggers, cooldowns, or conditions, but not additional resource pools.

### 6. Equipment System

Each character has **8 Equipment Slots**.

  Slot          Description
  ------------- ------------------------------------------
  Main Hand     Main weapon
  Second Hand   Off-hand weapon, shield, or special item
  Head          Head protection
  Body          Body protection
  Leg           Leg protection
  Foot          Footwear
  Accessory 1   Accessory
  Accessory 2   Accessory

#### Weapon Compatibility --- Confirmed Rules

-   Each class has multiple valid Weapon Types for Main Hand and Second
    Hand.
-   A Weapon Type can only be used by a class that has access to it.
-   The system uses **Strict Compatibility**.
-   There are no weapon-compatibility exceptions based on rarity, level,
    or item effects.
-   Legendary Equipment must also follow Strict Compatibility.
-   Main Hand and Second Hand have separate compatibility lists.
-   Every class must have at least one Second Hand option.
-   Two-handed weapons (for example, a greatsword or bow) occupy only
    Main Hand; Second Hand can still be equipped according to the class
    compatibility list.

#### Armor and Accessories --- Confirmed Rules

-   Armor and Accessories can be used across classes.
-   Specific items may have special restrictions.
-   Restrictions may include Class, Race, Level, or Stat Requirements.

#### Weapon Properties --- Confirmed Rules

A weapon determines:

1.  **Base Damage**
2.  **Weapon Properties**
3.  **Weapon-Specific Effects**, where applicable

Weapon Properties may include attributes such as attack type (Melee or
Ranged), Accuracy Modifier, or Break Power. These are examples, not a
finalized list.

### 7. Accuracy System

Ranged Accuracy has been discussed as a stat used to determine the
chance of ranged attacks hitting a target.

#### Hit Chance Concept

Proposed basic formula:

`Hit Chance = Base Hit Chance + Attacker Accuracy - Target Evasion`

Hit Chance should have minimum and maximum limits to prevent attacks
from being guaranteed misses or guaranteed hits.

Values such as an 85% Base Hit Chance, 5% minimum, and 95% maximum are
examples only and remain subject to balancing.

#### QTE Interaction

QTE Performance and Accuracy are separate systems:

-   Accuracy determines the chance of an attack hitting its target.
-   QTE Performance determines the bonus earned from the player's
    execution.
-   QTE grade does not affect Hit Chance; it modifies direct damage and
    Break Power only (Bab 12 (QTE System)).
-   A successful QTE does not automatically guarantee a hit.

#### Ranger Starter Passive

**Hunter's Instinct** increases Ranged Accuracy for ranged basic attacks
and active skills.

Ranged Accuracy does not automatically apply to melee attacks or magic
attacks.

### 8. Character Creation Flow

Recommended structure based on the current design:

1.  Create a New Character.
2.  Select a Race.
3.  Select a Class.
4.  Allocate Ability Scores.
5.  Review Character Stats.
6.  Review the Starter Passive.
7.  Configure Equipment.
8.  Confirm the Character.
9.  Add the Character to the Party.
10. Repeat until the party has 4 characters.
11. Begin the Expedition.

Equipment configuration may use the starting equipment provided for the
selected class.

The initial Active and Passive Skill Loadouts may use a starter
configuration. Players can customize their loadouts as they unlock
additional skills.

### 9. Decision Summary

  System                     Current Decision
  -------------------------- ------------------------------------------
  Party Size                 4 characters
  Playable Races             7
  Playable Classes           9
  Ability Scores             6, using Point Buy
  Starter Passive            At least 1 per class
  Active Skill Slots         3
  Passive Skill Slots        3
  Skill Unlocking            Hybrid
  Skill Upgrades             Rank-based
  Equipment Slots            8
  Weapon Compatibility       Strict
  Armor and Accessories      Cross-class, except special restrictions
  Combat Resources           HP and MP
  Appearance Customization   None

### 10. Open Design Items

The following details still need to be designed or finalized:

-   Ability Score values, Point Buy budget, and derived-stat formulas
-   Final weapon-type lists for each class and each hand
-   Starting equipment for each class
-   Equipment rarity and its mechanical impact
-   Detailed equipment stat scaling
-   Accuracy formulas (QTE modifiers are specified in the QTE System
    Specification)
-   Skill values, Rank progression, and level-based tier unlocks

### 11. Resources dan Level (dari GDD v0.2 §3.1)

|**Sistem**|**Aturan / draft**|
|---|---|
|**HP**|Kelangsungan karakter; status HP 0 dan recovery rules perlu<br>finalisasi.|
|**MP**|Dibayar untuk Active Skills.|
|**Resource class**|Tidak ada resource pool khusus class; hanya HP dan MP.|
|**Level**|Maksimum 15.|
|**Point Buy**|Stat allocation menggunakan Point Buy;nilai dan batas TBD.|

## Bab 6 — Race System

> Sumber: Bab 6 (Race System).

### 1. Overview

Race System memberikan identitas mekanis dan naratif kepada setiap karakter yang dibuat pemain.

Setiap race memiliki keunggulan tersendiri yang memengaruhi:
- Character attributes.
- Combat strategy.
- Racial signature abilities.
- Exploration dan interaksi dengan dunia.
- Sinergi dengan party composition.

Race tidak boleh menjadi penentu mutlak class atau role karakter. Semua race harus tetap dapat digunakan untuk berbagai class tanpa menciptakan kombinasi yang wajib dipilih untuk mencapai efektivitas optimal.

Sistem dirancang agar seluruh racial effects dapat dikonfigurasi melalui data, sehingga balancing dapat dilakukan tanpa mengubah core combat code.

### 2. Design Goals

#### 2.1 Primary Goals

1. Setiap race memiliki identitas gameplay yang jelas.
2. Setiap race memiliki keunggulan yang relevan dalam combat maupun exploration.
3. Racial Feat harus unik dan tidak dimiliki race lain.
4. Tidak ada race yang wajib digunakan untuk class tertentu.
5. Racial Feat tidak boleh menggantikan atau menghilangkan tantangan core combat.
6. Race System harus kompatibel dengan Individual Turn-Based Combat dan Reaction-Based QTE.
7. Race System harus mendukung party berisi empat karakter.
8. Seluruh racial effects harus data-driven dan mudah di-balance.

#### 2.2 Non-Goals

Race System tidak mencakup:

- Appearance customization.
- Class progression.
- Skill tree class.
- Equipment system.
- Full elemental resistance system.
- Race-specific class restrictions.
- Race evolution atau race transformation permanen.
- Passive Trait.

**Passive Trait tidak digunakan dalam Race System.**

### 3. Race System Structure

Setiap race memiliki tepat tiga komponen utama:

| Component | Description |
|---|---|
| Racial Ability Bonus | Bonus terhadap Ability Scores karakter. |
| Racial Feat | Signature ability unik yang dimiliki race tersebut. |
| Racial Utility | Keunggulan untuk exploration, event, atau interaksi dunia. |

Tidak ada komponen Passive Trait.

#### 3.1 Racial Ability Bonus

Racial Ability Bonus memberikan bonus terhadap enam Ability Scores:

- STR — Strength
- DEX — Dexterity
- VIT — Vitality
- INT — Intelligence
- WIS — Wisdom
- CHA — Charisma

Bonus race diterapkan setelah proses Point Buy. Racial Ability Bonus tidak boleh memberikan bonus yang terlalu besar hingga menentukan class karakter secara otomatis.

#### 3.2 Racial Feat

Racial Feat adalah kemampuan signature yang hanya dimiliki satu race.

- Setiap race memiliki tepat satu Racial Feat.
- Racial Feat tidak dapat dipelajari race lain.
- Racial Feat berupa active ability (memakai giliran pemilik) atau trigger-based ability (aktif otomatis saat kondisi terpenuhi).
- Racial Feat bukan Skill: tidak memakai MP, tidak menempati slot Active/Passive, dan tetap dapat digunakan saat terkena Silence.
- Racial Feat harus kompatibel dengan combat event system dan memiliki visual serta combat feedback yang jelas.

#### 3.3 Racial Utility

Racial Utility memberikan keuntungan dalam exploration dan interaksi dunia. Aturan lengkap ada di §10.

### 4. Konvensi Combat untuk Racial Feat

Konvensi berikut berlaku untuk semua Racial Feat di §5.

| Istilah | Arti |
|---|---|
| Memakai giliran | Aktivasi menggantikan aksi pemilik pada gilirannya; tidak memberikan extra turn. |
| Sampai giliran berikutnya | Efek berakhir di awal giliran berikutnya milik pemilik feat. |
| Selama N giliran | Efek berakhir di akhir giliran ke-N milik pemilik feat setelah giliran aktivasi. |
| Cooldown N | Feat tidak dapat dipakai pada N giliran pemilik berikutnya setelah aktivasi. |
| Usage limit per battle | Jumlah pemakaian maksimum dalam satu battle; reset di awal battle berikutnya. |
| Damage Bonus / Damage Taken | Modifier multiplicative sesuai Bab 11 §6. |
| Healing racial | Nilai tetap berbasis Max HP target; tidak dipengaruhi Healing Power atau talent healing. |

Aturan umum:

- Feat yang memakai giliran tidak dapat diaktifkan saat pemilik terkena Stun, Sleep, Mind Control, atau dalam kondisi Down.
- Feat trigger-based tetap aktif walaupun pemilik terkena status yang mencegah Action.
- Racial Feat tidak pernah memakai tombol QTE saat timing bar berjalan. Satu-satunya input saat bar adalah penerimaan prompt Second Chance (§5.4).
- Racial Feat tidak memberikan imunitas penuh terhadap damage, status, atau Break.

#### 4.1 Shield dari Racial Feat

- Shield dari Racial Feat ditambahkan sebagai instance terpisah di atas Shield yang sudah ada.
- Jika ada beberapa instance Shield, instance yang paling cepat kedaluwarsa menyerap damage lebih dulu.
- Shield menyerap damage setelah reduksi Defensive QTE dan rounding (Bab 11 §2.3).
- Instance Shield yang sama dari feat yang sama tidak dapat ditumpuk pada satu target; aktivasi baru pada target yang sama ditolak sampai instance lama berakhir.

### 5. Race Definitions

#### 5.1 Human

**Race Identity:** Adaptability, versatility, determination.

##### Racial Ability Bonus — Adaptive Potential

- +1 terhadap dua Ability Scores berbeda yang dipilih pemain.
- Pilihan dilakukan pada Character Creation dan tidak dapat diubah setelah karakter dibuat, kecuali melalui sistem respec.

##### Racial Feat — Determination

| Parameter | Nilai |
|---|---|
| Activation | Trigger-based (`OnCharacterHPThresholdReached`) |
| Trigger | Direct damage menurunkan HP Human dari di atas 30% menjadi 30% Max HP atau lebih rendah, dan Human tetap hidup |
| Effect | Shield 20% Max HP Human + All Damage Bonus +15% |
| Duration | Selama 2 giliran Human (Shield juga berakhir lebih cepat jika habis terserap) |
| Usage limit | 1 per battle |

- DoT tidak memicu Determination.
- Damage yang membuat HP mencapai 0 tidak memicu Determination.

##### Racial Utility — Adaptability

- Sekali per dungeon, Human dapat memakai opsi racial milik race lain dalam satu event, jika race tersebut tidak ada di party.
- Opsi pinjaman memberikan 75% dari reward atau efek opsi aslinya.
- Pada event tanpa opsi racial, Human mendapat opsi tambahan "Improvise" yang memberikan hasil setara opsi umum terbaik tanpa biaya resource.

---

#### 5.2 Elf

**Race Identity:** Grace, precision, magical affinity.

##### Racial Ability Bonus — Elven Grace

- +2 DEX atau +2 INT, dipilih pemain pada Character Creation.

##### Racial Feat — Fey Step

| Parameter | Nilai |
|---|---|
| Activation | Active, memakai giliran Elf |
| Effect | Elf menjadi **Phased**: tidak dapat ditarget oleh aksi musuh single-target. Serangan pertama Elf pada giliran berikutnya mendapat All Damage Bonus +20% |
| Duration | Phased sampai giliran berikutnya Elf |
| Cooldown | 4 |
| Usage limit | Tidak ada batas per battle selain cooldown |

- Aksi multi-target dan AoE tetap mengenai Elf yang Phased.
- Serangan single-target yang sudah ditelegraph ke Elf dialihkan ke karakter aktif berikutnya dalam urutan party (Bab 12 §6.4). Serangan single-target yang belum ditelegraph memilih target lain sesuai AI musuh.
- Fey Step tidak bergantung pada sistem posisi. Teleport hanya bersifat visual.
- Phased tidak dapat dihapus oleh efek musuh.

##### Racial Utility — Keen Senses

- Saat masuk ke sebuah node, hidden path dan hidden node yang berdampingan langsung dengan node tersebut terungkap. Secret yang lebih jauh tidak terungkap.
- Pada event trap atau environmental hazard, Elf mendapat opsi "Detect" yang menghindari trap sepenuhnya.
- Sebelum masuk combat node, komposisi musuh terlihat. Sekali per dungeon, Weakness satu jenis musuh di encounter tersebut ikut terungkap.

---

#### 5.3 Dwarf

**Race Identity:** Endurance, resilience, craftsmanship.

##### Racial Ability Bonus — Dwarven Constitution

- +2 VIT.

##### Racial Feat — Iron Resolve

| Parameter | Nilai |
|---|---|
| Activation | Active, memakai giliran Dwarf |
| Effect | All Damage Taken −30% dan Break Power yang diterima −50% |
| Duration | Sampai giliran berikutnya Dwarf |
| Cooldown | 3 |
| Usage limit | Tidak ada batas per battle selain cooldown |

- Pengurangan damage diterapkan sebagai modifier Damage Taken, sebelum Defensive QTE.
- Pengurangan Break Power diterapkan sebelum faktor (1 − R_QTE) dari Defensive QTE.
- Iron Resolve tidak mencegah Broken sepenuhnya dan tidak memberikan imunitas damage.

##### Racial Utility — Stonecraft

- Event atau node bertag `mineral` memberikan +1 upgrade material tambahan.
- Pada struktur bertag `destructible`, Dwarf dapat membuka jalan pintas tanpa biaya. Race lain dapat membukanya dengan consumable penghancur atau memakai rute normal.
- Pada event bertag `ancient_mechanism`, Dwarf otomatis berhasil pada pemeriksaan mekanisme.

---

#### 5.4 Halfling

**Race Identity:** Luck, agility, resilience through opportunity.

##### Racial Ability Bonus — Halfling Agility

- +2 DEX.

##### Racial Feat — Second Chance

| Parameter | Nilai |
|---|---|
| Activation | Trigger-based (`OnQTEFailure`) dengan konfirmasi pemain |
| Trigger | QTE milik Halfling menghasilkan Poor dari input pemain |
| Effect | Mengulang QTE yang sama satu kali |
| Usage limit | 2 per battle |
| Retry difficulty | Sama dengan QTE pertama (T_lead, window, dan pola tidak berubah) |

Rules:

1. Berlaku untuk Offensive QTE dan Defensive QTE milik Halfling. Pada Shared QTE yang mengenai Halfling, hasil retry hanya berlaku untuk Halfling; target lain tetap memakai hasil percobaan pertama.
2. Satu QTE maksimal di-retry satu kali dari sumber mana pun. Lucky (Talents System) di-roll lebih dulu; Second Chance hanya ditawarkan jika Lucky tidak aktif.
3. Penawaran berupa prompt 1000 ms saat animasi di-pause. Menekan tombol QTE berarti menerima; tidak menekan berarti menolak dan tidak memakai kuota.
4. Opsi menu "Second Chance otomatis" melewati prompt dan langsung memakai Second Chance.
5. Hasil percobaan kedua menjadi hasil final, termasuk jika hasilnya Poor lagi.
6. Pada QTE sekuens, hanya marker yang gagal yang diulang.
7. Auto-Poor (Stun, Sleep, Mind Control, Down) tidak dapat di-retry. Saat Auto-Good aktif, Second Chance tidak pernah ditawarkan.
8. Damage, Break Power, dan combo Martial Artist dihitung dari hasil final.
9. Second Chance tidak memicu dirinya sendiri dari hasil retry dan tidak mengulang damage resolution yang sudah diterapkan.

##### Racial Utility — Small and Nimble

- Jalur bertag `narrow` dapat dilalui Halfling sebagai jalan pintas. Race lain memakai rute normal.
- Pada event bertag `hazard`, damage hazard terhadap seluruh party berkurang 50%.
- Sekali per dungeon, party dapat memakai opsi "Sneak" untuk melewati satu combat node normal tanpa bertarung dan tanpa reward combat. Opsi ini tidak berlaku untuk elite dan boss.

---

#### 5.5 Dragonborn

**Race Identity:** Draconic heritage, elemental power, overwhelming strength.

##### Racial Ability Bonus — Draconic Heritage

Bonus ditentukan oleh lineage (subrace) yang dipilih pada Character Creation.

| Lineage | Elemen | Ability Bonus |
|---|---|---|
| Crimson | Fire | +2 STR |
| Stone | Earth | +2 STR |
| Azure | Lightning | +2 CHA |
| Frost | Ice | +2 CHA |

##### Racial Feat — Draconic Overdrive

| Parameter | Nilai |
|---|---|
| Activation | Active, memakai giliran Dragonborn |
| Effect | All Damage Bonus +35%; Element-Specific Damage Bonus +25% untuk elemen lineage; Break Power +50%; multiplier counter 1.0× → 1.25× (Bab 12 §8.5) |
| Duration | Selama 3 giliran Dragonborn |
| Drawback | Setelah Overdrive berakhir, Dragonborn menerima **Exhausted** |
| Usage limit | 1 per battle |

**Exhausted**

| Parameter | Nilai |
|---|---|
| Effect | All Damage Bonus −20% (0.8×) dan Break Power −30% |
| Duration | Selama 2 giliran Dragonborn |
| Cleanse | Tidak dapat dihapus |

- Exhausted tidak mencegah Dragonborn melakukan aksi.
- Overdrive tidak memberikan extra turn, tidak menghapus cooldown lain, dan tidak meningkatkan stat secara permanen.
- Bonus elemen lineage hanya berlaku untuk damage component dengan elemen tersebut; Overdrive tidak menambahkan damage component baru.

##### Racial Utility — Draconic Presence

- Pada event NPC, Dragonborn mendapat opsi "Intimidate" yang memaksa NPC memberikan informasi atau reward pilihan.
- Sekali per dungeon, Dragonborn mendapat diskon 15% untuk satu transaksi di Dungeon Shop.
- Pada event bertag `dragon` atau `relic`, efek relic atau reward terlihat sebelum diambil, dan tersedia satu opsi khusus Dragonborn.

---

#### 5.6 Liichtian

**Race Identity:** Celestial heritage, radiant power, divine connection.

Liichtian adalah race original dalam dunia game dengan konsep dasar yang terinspirasi oleh Aasimar. Lore, budaya, dan kemampuan ras mengikuti identitas dunia game, bukan sekadar nama lain untuk Aasimar.

##### Racial Ability Bonus — Celestial Wisdom

- +2 WIS atau +2 CHA, dipilih pemain pada Character Creation.

##### Racial Feat — Lightbound

| Parameter | Nilai |
|---|---|
| Activation | Active, memakai giliran Liichtian |
| Target | Seluruh anggota party yang aktif |
| Effect | Heal 10% Max HP masing-masing target; All Damage Taken −10%; Status Effect Resistance +20% (additive, tetap tunduk pada cap 75%) |
| Duration | Selama 2 giliran Liichtian |
| Usage limit | 1 per battle |

Pembeda dengan Ocean's Embrace: Lightbound adalah perlindungan ringan untuk seluruh party yang berfokus pada ketahanan status, sedangkan Ocean's Embrace adalah Shield besar untuk satu target.

##### Racial Utility — Luminescent Insight

- Reward yang membawa curse atau efek negatif tersembunyi ditandai sebelum diambil.
- Pada event bertag `sacred`, tersedia satu opsi blessing tambahan khusus Liichtian.
- Pada event bertag `corruption` atau `supernatural`, sumber pengaruh supernatural terungkap, tetapi event tidak otomatis selesai.

---

#### 5.7 Merfolk

**Race Identity:** Oceanic affinity, water magic, protection.

##### Racial Ability Bonus — Aquatic Affinity

- +2 WIS atau +2 DEX, dipilih pemain pada Character Creation.

##### Racial Feat — Ocean's Embrace

| Parameter | Nilai |
|---|---|
| Activation | Active, memakai giliran Merfolk |
| Target | Satu anggota party, termasuk Merfolk sendiri |
| Effect | Shield 25% Max HP target; Defensive QTE window target +10% selama Shield aktif (dihitung dalam cap +25%) |
| Duration | Sampai Shield habis atau selama 3 giliran Merfolk |
| On end | Saat Shield pecah atau kedaluwarsa, target dipulihkan 10% Max HP satu kali |
| Cooldown | 4 |
| Usage limit | Tidak ada batas per battle selain cooldown |

- Satu target hanya dapat memiliki satu Ocean's Embrace aktif.
- Healing terjadi tepat satu kali per aktivasi.

##### Racial Utility — Ocean's Gift

- Jalur bertag `aquatic` dapat dilalui Merfolk sebagai jalan pintas.
- Pada event bertag `liquid` (mata air, kolam, ramuan, cairan tak dikenal), hasil setiap opsi terlihat sebelum dipilih, dan Merfolk dapat memurnikan cairan beracun menjadi opsi aman.
- Pada event bertag `aquatic`, tersedia satu opsi interaksi dengan makhluk air yang memberikan informasi atau reward tambahan.

### 6. Racial Ability Bonus Rules

#### 6.1 Ability Score Calculation

Ability Scores menggunakan sistem Point Buy yang telah ditetapkan. Racial Ability Bonus diterapkan setelah Point Buy.

Contoh Human:
- Point Buy: STR 12, DEX 14, VIT 10, INT 10, WIS 12, CHA 8.
- Memilih +1 DEX dan +1 CHA.
- Final: STR 12, DEX 15, VIT 10, INT 10, WIS 12, CHA 9.

#### 6.2 Attribute Caps

| Cap | Nilai |
|---|---|
| Maksimum saat Character Creation sebelum racial bonus | 15 |
| Maksimum setelah racial bonus saat Character Creation | 17 |
| Maksimum progression normal | 20 |
| Temporary buff | Boleh melampaui 20 hanya jika efek buff menyatakannya secara eksplisit |

Semua caps disimpan sebagai data configurable.

#### 6.3 Class Independence

- Race tidak memiliki class restriction.
- Racial Ability Bonus tidak boleh membuat class tertentu menjadi mandatory.
- Tidak ada Ability Score yang memengaruhi QTE (Bab 12 §8.2).

### 7. Racial Feat Balance Rules

Setiap Racial Feat memenuhi aturan berikut:

1. Memiliki efek yang jelas dan dapat diukur.
2. Memiliki trigger atau activation condition.
3. Memiliki cooldown, duration, atau usage limit.
4. Tidak menghasilkan infinite loop.
5. Tidak memberikan extra turn.
6. Tidak meniadakan core combat mechanics.
7. Tidak menjadi solusi wajib untuk boss tertentu.
8. Memiliki combat log dan visual feedback.
9. Efek dapat dikonfigurasi melalui data.
10. Tetap relevan sepanjang progression, karena seluruh nilai berbasis persentase Max HP atau modifier, bukan angka flat.

#### 7.1 Ringkasan Racial Feat

| Race | Feat | Tipe | Batas | Peran |
|---|---|---|---|---|
| Human | Determination | Trigger | 1 per battle | Bertahan dari kondisi kritis |
| Elf | Fey Step | Active | Cooldown 4 | Menghindar dan serangan lanjutan |
| Dwarf | Iron Resolve | Active | Cooldown 3 | Tahan damage dan Break |
| Halfling | Second Chance | Trigger + konfirmasi | 2 per battle | Retry QTE |
| Dragonborn | Draconic Overdrive | Active | 1 per battle | Burst dengan drawback |
| Liichtian | Lightbound | Active | 1 per battle | Support ringan seluruh party |
| Merfolk | Ocean's Embrace | Active | Cooldown 4 | Shield besar satu target |

### 8. Combat Integration

Race System terintegrasi dengan Turn Manager, Combat Action System, Damage Calculation, Shield System, Healing System, Status Effect System, Break/Stagger System, Reaction-Based QTE System, Target Selection, dan Combat Event System.

#### 8.1 Combat Events

| Event | Dipakai oleh |
|---|---|
| `OnTurnStart` | Berakhirnya Phased (Fey Step) dan Iron Resolve; hitungan durasi dan cooldown |
| `OnTurnEnd` | Hitungan durasi Determination, Overdrive, Exhausted, Lightbound, Ocean's Embrace |
| `OnDamageReceived` | Pemeriksaan threshold Determination |
| `OnDamageDealt` | Bonus serangan pertama setelah Fey Step |
| `OnShieldGranted` | Determination, Ocean's Embrace |
| `OnShieldBroken` | Healing Ocean's Embrace |
| `OnQTEResolved` | Hasil final setelah retry |
| `OnQTEFailure` | Second Chance |
| `OnCharacterHPThresholdReached` | Determination |
| `OnStatusEffectApplied` | Pemeriksaan Stun, Sleep, dan Mind Control untuk aktivasi feat |
| `OnCharacterBroken` | Combat log dan feedback |

Event system memastikan satu event tidak memicu racial effect berulang tanpa batas: setiap feat memeriksa usage limit dan flag "sudah diproses" per event instance.

#### 8.2 QTE Integration

QTE lifecycle untuk Second Chance:

1. QTE dimulai.
2. Pemain melakukan input.
3. QTE menghasilkan result.
4. Combat system mengidentifikasi result.
5. Jika result Poor dari input pemain, Lucky di-roll lebih dulu (jika karakter memiliki Lucky).
6. Jika Lucky tidak aktif dan karakter memiliki Second Chance dengan kuota tersisa, prompt 1000 ms ditampilkan (atau langsung dipakai jika opsi otomatis aktif).
7. Jika diterima, QTE dimulai ulang dengan parameter yang sama.
8. Hasil QTE kedua menjadi final.
9. Combat effect diterapkan.
10. Usage counter diperbarui.

Second Chance tidak boleh:
- Membuat QTE kedua otomatis berhasil.
- Mengulang QTE tanpa konfirmasi, kecuali opsi otomatis aktif.
- Memicu dirinya sendiri dari hasil retry.
- Mengulang damage resolution yang sudah diterapkan.
- Mengulang QTE milik karakter lain. Pada Shared QTE, hasil retry hanya berlaku untuk Halfling.

#### 8.3 Combat Feedback

- Setiap aktivasi feat menampilkan nama feat di atas karakter dan tercatat di combat log.
- Status bentukan feat (Phased, Exhausted, efek Lightbound) memakai ikon status standar.
- Kuota Second Chance ditampilkan sebagai pip di bawah potret Halfling, terpisah dari pip Lucky.

### 9. Data Architecture

Race System menggunakan data-driven architecture. Field names mengikuti naming convention project.

#### 9.1 Required Data Fields

Setiap race: `raceId`, `displayName`, `description`, `racialAbilityBonus`, `subraces` (jika ada), `racialFeat`, `racialUtility`, `icon`, `loreDescription`.

Setiap racial feat: `featId`, `displayName`, `description`, `activationType`, `triggerEvent` atau activation condition, `effectType`, `cooldown` atau `usageLimitPerBattle`, `duration`, `targetType`, `balanceParameters`.

#### 9.2 Baseline Data

```json
[
  {
    "raceId": "human",
    "racialAbilityBonus": { "choice": "two_different_scores", "amount": 1 },
    "racialFeat": {
      "featId": "determination", "activationType": "trigger",
      "triggerEvent": "OnCharacterHPThresholdReached", "targetType": "self",
      "balanceParameters": { "hpThreshold": 0.30, "shieldMaxHpRatio": 0.20, "allDamageBonus": 0.15, "durationTurns": 2, "usageLimitPerBattle": 1, "triggeredByDoT": false }
    },
    "racialUtility": { "utilityId": "adaptability", "borrowedOptionEfficiency": 0.75, "borrowUsesPerDungeon": 1 }
  },
  {
    "raceId": "elf",
    "racialAbilityBonus": { "choice": ["dexterity", "intelligence"], "amount": 2 },
    "racialFeat": {
      "featId": "fey_step", "activationType": "active", "targetType": "self",
      "balanceParameters": { "status": "phased", "phasedUntil": "next_own_turn", "nextAttackAllDamageBonus": 0.20, "cooldownTurns": 4 }
    },
    "racialUtility": { "utilityId": "keen_senses", "revealRange": "adjacent_nodes", "weaknessRevealsPerDungeon": 1 }
  },
  {
    "raceId": "dwarf",
    "racialAbilityBonus": { "fixed": { "vitality": 2 } },
    "racialFeat": {
      "featId": "iron_resolve", "activationType": "active", "targetType": "self",
      "balanceParameters": { "allDamageTaken": -0.30, "breakPowerTaken": -0.50, "durationUntil": "next_own_turn", "cooldownTurns": 3 }
    },
    "racialUtility": { "utilityId": "stonecraft", "bonusMaterialPerMineralEvent": 1 }
  },
  {
    "raceId": "halfling",
    "racialAbilityBonus": { "fixed": { "dexterity": 2 } },
    "racialFeat": {
      "featId": "second_chance", "activationType": "trigger_confirm",
      "triggerEvent": "OnQTEFailure", "targetType": "own_qte",
      "balanceParameters": { "usageLimitPerBattle": 2, "retryLimitPerQte": 1, "retryDifficulty": "same_as_original", "promptMs": 1000, "luckyResolvesFirst": true, "automaticSuccess": false }
    },
    "racialUtility": { "utilityId": "small_and_nimble", "hazardDamageReduction": 0.50, "sneakUsesPerDungeon": 1 }
  },
  {
    "raceId": "dragonborn",
    "subraces": [
      { "subraceId": "crimson", "element": "fire", "bonus": { "strength": 2 } },
      { "subraceId": "stone", "element": "earth", "bonus": { "strength": 2 } },
      { "subraceId": "azure", "element": "lightning", "bonus": { "charisma": 2 } },
      { "subraceId": "frost", "element": "ice", "bonus": { "charisma": 2 } }
    ],
    "racialFeat": {
      "featId": "draconic_overdrive", "activationType": "active", "targetType": "self",
      "balanceParameters": { "allDamageBonus": 0.35, "lineageElementBonus": 0.25, "breakPowerBonus": 0.50, "counterMultiplier": 1.25, "durationTurns": 3, "usageLimitPerBattle": 1,
        "exhausted": { "allDamageBonus": -0.20, "breakPowerBonus": -0.30, "durationTurns": 2, "cleansable": false } }
    },
    "racialUtility": { "utilityId": "draconic_presence", "shopDiscount": 0.15, "discountUsesPerDungeon": 1 }
  },
  {
    "raceId": "liichtian",
    "racialAbilityBonus": { "choice": ["wisdom", "charisma"], "amount": 2 },
    "racialFeat": {
      "featId": "lightbound", "activationType": "active", "targetType": "all_allies",
      "balanceParameters": { "healMaxHpRatio": 0.10, "allDamageTaken": -0.10, "statusEffectResistance": 0.20, "durationTurns": 2, "usageLimitPerBattle": 1 }
    },
    "racialUtility": { "utilityId": "luminescent_insight", "revealsCursedRewards": true }
  },
  {
    "raceId": "merfolk",
    "racialAbilityBonus": { "choice": ["wisdom", "dexterity"], "amount": 2 },
    "racialFeat": {
      "featId": "oceans_embrace", "activationType": "active", "targetType": "single_ally_or_self",
      "balanceParameters": { "shieldMaxHpRatio": 0.25, "defensiveQteWindowBonus": 0.10, "healOnEndMaxHpRatio": 0.10, "durationTurns": 3, "cooldownTurns": 4, "maxActivePerTarget": 1 }
    },
    "racialUtility": { "utilityId": "oceans_gift", "revealsLiquidOutcomes": true }
  }
]
```

### 10. Exploration Integration

Racial Utility terintegrasi dengan exploration event system melalui event tags, environment tags, race requirements, alternative solutions, reward modifiers, dan information reveal.

#### 10.1 Aturan Umum Utility

- Setiap dungeon memuat minimal satu event atau node yang dapat memakai utility setiap race, sehingga semua utility tetap relevan di 8 dungeon.
- Utility tidak pernah menjadi satu-satunya cara menyelesaikan konten utama. Race lain selalu memiliki alternatif berupa biaya Gold, biaya HP (10% Max HP seluruh party), consumable, atau rute yang lebih panjang.
- Utility race yang sama tidak menumpuk jika party memiliki lebih dari satu karakter dengan race tersebut.
- Efek "sekali per dungeon" reset saat party masuk ke dungeon berikutnya.

#### 10.2 Event Tags

| Tag | Utility yang merespons |
|---|---|
| `hidden`, `trap`, `hazard` | Keen Senses (Elf), Small and Nimble (Halfling) |
| `mineral`, `destructible`, `ancient_mechanism` | Stonecraft (Dwarf) |
| `narrow` | Small and Nimble (Halfling) |
| `npc`, `dragon`, `relic`, `shop` | Draconic Presence (Dragonborn) |
| `sacred`, `corruption`, `supernatural`, `cursed_reward` | Luminescent Insight (Liichtian) |
| `aquatic`, `liquid` | Ocean's Gift (Merfolk) |
| Event tanpa opsi racial | Adaptability (Human) |

Contoh event:

```json
{
  "eventId": "ancient_waterway",
  "environmentTags": ["aquatic", "ancient_mechanism"],
  "racialInteractions": [
    { "raceId": "merfolk", "utilityId": "oceans_gift", "interactionId": "aquatic_route" },
    { "raceId": "dwarf", "utilityId": "stonecraft", "interactionId": "open_sluice_gate" }
  ],
  "alternativeSolution": { "type": "longer_route" }
}
```

### 11. Character Creation Integration

1. Race selection menampilkan seluruh race yang tersedia.
2. Race selection menampilkan lore dan gameplay identity.
3. Racial Ability Bonus ditampilkan secara jelas, termasuk pilihan stat atau lineage.
4. Racial Feat ditampilkan beserta angka baseline-nya.
5. Racial Utility ditampilkan.
6. Final Ability Scores diperbarui ketika race dan pilihan bonus dipilih.
7. Player dapat melihat perubahan Ability Scores sebelum konfirmasi.
8. Race selection kompatibel dengan seluruh class.
9. Character data menyimpan `raceId` dan `subraceId`, bukan display name.

### 12. Persistence

Character save menyimpan:

- Character ID.
- Race ID.
- Subrace ID (Dragonborn).
- Base Ability Scores.
- Racial Ability Bonus selection.
- Final Ability Scores.
- Racial Feat ID.
- Racial Utility ID.

Temporary state tidak disimpan sebagai data permanen dan di-reset di awal setiap battle: kuota Second Chance, durasi Overdrive, Exhausted, Shield Ocean's Embrace, cooldown Fey Step, Iron Resolve, dan Ocean's Embrace, serta pemakaian Determination dan Lightbound.

Penggunaan utility "sekali per dungeon" disimpan di run state dan di-reset saat masuk dungeon berikutnya.

### 13. Error Handling

| Kondisi | Penanganan |
|---|---|
| Race ID tidak ditemukan | Fallback ke Human tanpa racial bonus pilihan, tulis error log |
| Racial Feat data tidak valid | Feat dinonaktifkan untuk karakter tersebut, tulis error log |
| Target ability tidak tersedia atau ally tidak valid | Aktivasi ditolak tanpa memakai giliran |
| QTE sudah diselesaikan | Prompt Second Chance tidak ditampilkan |
| Second Chance sudah digunakan pada QTE ini atau kuota habis | Prompt tidak ditampilkan |
| Karakter tidak lagi aktif dalam battle | Efek feat yang menargetkan karakter tersebut berakhir |
| Target sudah memiliki Ocean's Embrace aktif | Aktivasi ditolak tanpa memakai giliran |
| Combat event dipicu berulang | Flag "sudah diproses" per event instance mencegah efek ganda |
| Race data berubah setelah karakter dibuat | Nilai baru dipakai di battle berikutnya; Ability Score yang tersimpan tidak dihitung ulang kecuali lewat respec |

### 14. Acceptance Criteria

#### Race Data

- [ ] Tujuh race tersedia: Human, Elf, Dwarf, Halfling, Dragonborn, Liichtian, Merfolk.
- [ ] Dragonborn memiliki empat lineage dengan elemen dan bonus sesuai §5.5.
- [ ] Setiap race memiliki tepat satu Racial Feat dan satu Racial Utility.
- [ ] Tidak ada Passive Trait.

#### Character Creation

- [ ] Pilihan bonus Human, Elf, Liichtian, Merfolk, dan lineage Dragonborn berfungsi.
- [ ] Attribute caps 15 / 17 / 20 ditegakkan.
- [ ] Race tidak mengunci class.

#### Combat

- [ ] Determination terpicu tepat sekali saat HP melewati 30% akibat direct damage.
- [ ] Phased mencegah target single-target, tidak mencegah AoE, dan mengalihkan serangan yang sudah ditelegraph.
- [ ] Iron Resolve menerapkan −30% damage taken dan −50% Break Power taken sampai giliran berikutnya.
- [ ] Overdrive menerapkan bonus selama 3 giliran, lalu Exhausted selama 2 giliran yang tidak dapat di-cleanse.
- [ ] Lightbound menyembuhkan dan memberi buff ke seluruh party yang aktif, dengan cap Status Effect Resistance 75%.
- [ ] Ocean's Embrace memberi Shield, bonus window defensif, dan heal satu kali saat berakhir.
- [ ] Feat yang memakai giliran tidak dapat diaktifkan saat Stun, Sleep, Mind Control, atau Down, tetapi dapat diaktifkan saat Silence.
- [ ] Combat log menampilkan setiap penggunaan racial ability.

#### Second Chance

- [ ] Hanya aktif setelah Poor dari input pemain, dan tidak untuk auto-Poor.
- [ ] Lucky di-roll lebih dulu; satu QTE maksimal satu retry.
- [ ] Prompt 1000 ms dan opsi otomatis berfungsi.
- [ ] Retry memakai parameter QTE yang sama dan hasil kedua menjadi final.
- [ ] Kuota 2 per battle ditegakkan.
- [ ] Pada Shared QTE, hasil retry hanya mengubah hasil Halfling.

#### Exploration

- [ ] Setiap dungeon memiliki minimal satu event untuk utility setiap race.
- [ ] Setiap opsi racial memiliki alternatif untuk party tanpa race tersebut.
- [ ] Efek "sekali per dungeon" reset di dungeon berikutnya.

### 15. Balancing Parameters

Seluruh parameter di §5, §6.2, dan §9.2 disimpan sebagai data configurable. Parameter yang paling sensitif untuk dipantau saat playtest:

| Parameter | Baseline | Indikator perlu di-review |
|---|---|---|
| Kuota Second Chance | 2 per battle | Rasio Poor final Halfling lebih rendah >10 poin dari ras lain |
| Overdrive All Damage Bonus | +35% | Dragonborn menyumbang >40% damage party di boss |
| Iron Resolve damage taken | −30% | Dwarf tidak pernah turun di bawah 50% HP di encounter elite |
| Cooldown Fey Step | 4 | Elf Phased lebih dari 40% total giliran battle |
| Lightbound Status Effect Resistance | +20% | Status musuh gagal >80% selama Lightbound aktif |
| Shield Ocean's Embrace | 25% Max HP | Target Ocean's Embrace tidak pernah kehilangan HP dalam 3 giliran |

### 16. Implementation Notes

Implementation mengikuti architecture project yang sudah ada. Sebelum implementasi, audit character data structure, Ability Score calculation, Character Creation flow, Combat Action System, QTE lifecycle, Status Effect System, Exploration Event System, dan save/load system. Jangan membuat sistem baru yang menduplikasi fungsi yang sudah ada.

### 17. Definition of Done

Race System dianggap selesai apabila:

- Seluruh tujuh race dan empat lineage Dragonborn terdaftar.
- Character Creation mendukung race selection dan pilihan bonus.
- Racial Ability Bonus diterapkan dengan benar.
- Seluruh Racial Feat dapat digunakan sesuai §5 dan §8.
- Racial Utility terintegrasi dengan exploration events sesuai §10.
- Second Chance terintegrasi dengan QTE lifecycle dan Lucky.
- Save/load mempertahankan race data.
- Unit tests untuk racial effects dan integration tests untuk combat serta exploration lulus.
- Tidak ada Passive Trait dan tidak ada race-specific class restriction.
- Seluruh racial effects dapat dikonfigurasi tanpa mengubah core system code.

### 18. Change Log

| Version | Notes |
|---|---|
| 1.0 | Draft awal: struktur tiga komponen, konsep feat dan utility, integrasi QTE Second Chance. |
| 1.1 | Semua nilai baseline ditetapkan dan semua status provisional/konsep dihapus. Elf mendapat pilihan +2 INT; Second Chance menjadi 2 per battle dan terintegrasi dengan Lucky serta aturan QTE System; empat lineage Dragonborn; efek Exhausted; konvensi durasi, cooldown, dan Shield; Fey Step tidak lagi bergantung pada posisi; Iron Resolve menjadi active ability; semua Racial Utility diberi mekanik dan aturan relevansi per dungeon. |

## Bab 7 — Background System

> Sumber: Background System.

### 1. Overview

Background represents a character's life and experience before becoming an adventurer. Each character selects one Background, which grants a passive perk that develops as the character levels up.

Backgrounds provide a distinct gameplay identity without determining a character's Race, Class, or Skill Loadout.

#### Design Principles

- Each character has one Background.
- A Background grants one passive perk.
- Perks improve at designated character-level milestones.
- A newly unlocked Rank replaces the previous Rank; effects from different Ranks do not stack.
- Background perks should remain useful across multiple Classes.
- Background perks do not determine Race, Class, or Skill Loadout.
- Values and effects in this document are proposals for evaluation, not confirmed final balance.

### 2. Rank Progression

The game's current documented Character Level Cap is 15. Background progression uses the same five milestone levels as the Skill Tree.

| Perk Rank | Unlock Level |
|---|---:|
| Rank I | 1 |
| Rank II | 4 |
| Rank III | 7 |
| Rank IV | 10 |
| Rank V | 13 |

Rank V is the highest Background Perk Rank within the current level cap.

### 3. Backgrounds and Perks

#### 3.1 Acolyte — Faithful Devotion

**Identity:** Spiritual devotion, protection, and recovery.

Acolyte gains strength through faith and devotion. Its perk supports keeping the character and party alive.

| Rank | Unlock Level | Perk | Effect |
|---|---:|---|---|
| I | 1 | Faithful Heart | Healing dealt increases by 5%. |
| II | 4 | Divine Resilience | Healing dealt increases to 8%. |
| III | 7 | Blessed Recovery | Healing dealt increases to 10%. Healing received also increases by 5%. |
| IV | 10 | Sanctified Spirit | Healing dealt increases to 12%, and healing received increases by 8%. |
| V | 13 | Faith Unbroken | Healing dealt increases to 15%, and healing received increases by 10%. |

**Design note:** Acolyte can benefit any Class with access to healing; it is not exclusive to Cleric.

#### 3.2 Charlatan — Master of Deception

**Identity:** Manipulation, deception, and exploiting enemy weaknesses.

Charlatan turns deception into a combat advantage when enemies are impaired or affected by debuffs.

| Rank | Unlock Level | Perk | Effect |
|---|---:|---|---|
| I | 1 | False Promise | Damage dealt to enemies affected by a debuff increases by 3%. |
| II | 4 | Cunning Exploit | The damage bonus increases to 5%. |
| III | 7 | Deceptive Advantage | The damage bonus increases to 7%. |
| IV | 10 | Master Manipulator | The damage bonus increases to 10%. |
| V | 13 | Perfect Deception | The damage bonus increases to 12%. The character's first attack against an enemy that has not yet acted in the battle deals an additional 5% damage. |

**Design note:** The debuff condition uses the game's defined debuff classification. The Rank V first-attack bonus is a separate effect.

#### 3.3 Guard — Guardian's Discipline

**Identity:** Defense, vigilance, and protection.

Guard has experience protecting people, places, or territory. Its perk provides consistent physical defense.

| Rank | Unlock Level | Perk | Effect |
|---|---:|---|---|
| I | 1 | Watchful Guard | Physical Damage Taken is reduced by 3%. |
| II | 4 | Steady Defense | Physical Damage Taken is reduced by 5%. |
| III | 7 | Defensive Training | Physical Damage Taken is reduced by 7%. |
| IV | 10 | Guardian's Resolve | Physical Damage Taken is reduced by 9%. |
| V | 13 | Unyielding Guardian | Physical Damage Taken is reduced by 10%. While HP is below 30%, Physical Damage Taken is reduced by an additional 5%. |

**Design note:** This perk applies to Physical Damage, not automatically to all damage types.

#### 3.4 Hermit — Solitary Wisdom

**Identity:** Solitude, endurance, and self-control.

Hermit has learned to endure difficult circumstances without relying on others. Its perk improves resistance to status effects.

| Rank | Unlock Level | Perk | Effect |
|---|---:|---|---|
| I | 1 | Quiet Endurance | Status Effect Resistance increases by 5%. |
| II | 4 | Solitary Resolve | Status Effect Resistance increases to 8%. |
| III | 7 | Inner Balance | Status Effect Resistance increases to 10%. |
| IV | 10 | Unshaken Mind | Status Effect Resistance increases to 12%. |
| V | 13 | Tranquil Spirit | Status Effect Resistance increases to 15%. When the character successfully resists a status effect, restore MP equal to 3% of Max MP, at most once per turn. |

**Design note:** This perk follows the game's Status Effect Resistance rules and does not grant automatic immunity.

#### 3.5 Farmer — Hardy Harvest

**Identity:** Hard work, physical endurance, and a simple life.

Farmer is accustomed to demanding work and harsh conditions. Its perk increases durability and post-battle recovery.

| Rank | Unlock Level | Perk | Effect |
|---|---:|---|---|
| I | 1 | Hardy Worker | Max HP increases by 3%. |
| II | 4 | Sturdy Constitution | Max HP increases to 5%. |
| III | 7 | Seasoned Laborer | Max HP increases to 7%. |
| IV | 10 | Resilient Farmer | Max HP increases to 9%. |
| V | 13 | Bountiful Harvest | Max HP increases to 10%. After battle, restore HP equal to 3% of Max HP. |

**Design note:** Post-battle recovery does not revive a character whose HP has reached 0.

#### 3.6 Noble — Noble Authority

**Identity:** Social status, leadership, and confidence.

Noble was raised in an aristocratic environment where influence and responsibility mattered. Its perk improves MP capacity and post-battle recovery.

| Rank | Unlock Level | Perk | Effect |
|---|---:|---|---|
| I | 1 | Noble Bearing | Max MP increases by 3%. |
| II | 4 | Commanding Presence | Max MP increases to 5%. |
| III | 7 | Royal Confidence | Max MP increases to 7%. |
| IV | 10 | Noble Authority | Max MP increases to 9%. |
| V | 13 | Born to Lead | Max MP increases to 10%. After battle, restore MP equal to 5% of Max MP. |

**Design note:** MP capacity can benefit any character whose Active Skills use MP.

#### 3.7 Sage — Learned Mind

**Identity:** Knowledge, research, and deep understanding.

Sage has spent considerable time studying. Its perk improves the efficiency of Active Skill MP usage.

| Rank | Unlock Level | Perk | Effect |
|---|---:|---|---|
| I | 1 | Studied Knowledge | Active Skill MP Cost is reduced by 3%. |
| II | 4 | Efficient Casting | Active Skill MP Cost is reduced by 5%. |
| III | 7 | Deep Understanding | Active Skill MP Cost is reduced by 7%. |
| IV | 10 | Masterful Insight | Active Skill MP Cost is reduced by 9%. |
| V | 13 | Enlightened Mind | Active Skill MP Cost is reduced by 10%. After using an Active Skill, the character has a 10% chance to restore MP equal to 5% of Max MP. |

**Design note:** An Active Skill with an MP cost cannot become free through this perk; its minimum cost remains 1 MP.

#### 3.8 Soldier — Veteran's Instinct

**Identity:** War experience, discipline, and combat ability.

Soldier has experience facing dangerous situations. Its perk improves consistent Physical Damage output.

| Rank | Unlock Level | Perk | Effect |
|---|---:|---|---|
| I | 1 | Combat Training | Physical Damage increases by 3%. |
| II | 4 | Battle Experience | Physical Damage increases to 5%. |
| III | 7 | Veteran's Instinct | Physical Damage increases to 7%. |
| IV | 10 | Battle Hardened | Physical Damage increases to 9%. |
| V | 13 | War Veteran | Physical Damage increases to 10%. After the character receives damage from an enemy attack, its next Physical Damage instance increases by 5%, at most once per turn. |

**Design note:** The Rank V additional bonus applies to the next Physical Damage instance. Multiple enemy hits in the same turn do not stack the effect.

#### 3.9 Wayfarer — Endless Journey

**Identity:** Travel, exploration, and adaptation.

Wayfarer is accustomed to long journeys and unfamiliar environments. Its perk improves Speed and can provide a temporary combat advantage.

| Rank | Unlock Level | Perk | Effect |
|---|---:|---|---|
| I | 1 | Traveler's Instinct | Speed increases by 3%. |
| II | 4 | Roadwise | Speed increases to 5%. |
| III | 7 | Seasoned Traveler | Speed increases to 7%. |
| IV | 10 | Pathfinder | Speed increases to 9%. |
| V | 13 | Endless Journey | Speed increases to 10%. At the start of battle, the character has a 10% chance to gain 10% Speed for 2 turns. |

**Design note:** Speed affects turn order. This perk does not directly grant extra turns unless the turn-order system explicitly supports that behavior.

#### 3.10 Scribe — Keeper of Records

**Identity:** Documentation, analysis, and recorded knowledge.

Scribe is accustomed to observing and recording information. Its perk improves accuracy and rewards repeated attacks against the same target.

| Rank | Unlock Level | Perk | Effect |
|---|---:|---|---|
| I | 1 | Careful Observation | Accuracy increases by 3%. |
| II | 4 | Detailed Records | Accuracy increases to 5%. |
| III | 7 | Enemy Analysis | Accuracy increases to 7%. |
| IV | 10 | Tactical Notes | Accuracy increases to 9%. |
| V | 13 | Complete Record | Accuracy increases to 10%. After attacking the same enemy 3 times, the character's next attack against that enemy gains 5% Critical Chance. |

**Design note:** The Critical Chance bonus applies to the next attack against the same target. After triggering, the target's analysis counter resets.

### 4. Background Identity Summary

| Background | Primary Perk Focus | Potential Class Synergy |
|---|---|---|
| Acolyte | Healing and recovery | Cleric, Druid, support-oriented builds |
| Charlatan | Damage against debuffed targets | Rogue, Warlock, Ranger |
| Guard | Physical defense | Fighter, Cleric, Druid |
| Hermit | Status Effect Resistance | All Classes |
| Farmer | Max HP and recovery | Fighter, Barbarian, durable builds |
| Noble | Max MP and recovery | Wizard, Warlock, support-oriented builds |
| Sage | MP efficiency | Wizard, Cleric, Druid |
| Soldier | Physical offense | Fighter, Barbarian, Martial Artist |
| Wayfarer | Speed and turn order | Rogue, Ranger, support-oriented builds |
| Scribe | Accuracy and target analysis | Ranger, Rogue, Martial Artist |

### 5. Balancing and Implementation Notes

The following items require testing and confirmation before this draft is considered final:

1. **Percentage stacking:** Determine how Background bonuses stack with equipment, Class skills, Race abilities, and other modifiers.
2. **Damage calculation:** Confirm the exact position of damage bonuses and damage reduction in the combat formula.
3. **Status Effect Resistance:** Confirm the formula and whether resistance is additive or multiplicative.
4. **Healing rules:** Confirm whether healing dealt and healing received modifiers stack additively or multiplicatively.
5. **Post-battle recovery:** Confirm when recovery is applied and whether it applies between every battle or only after selected encounters.
6. **Speed and turn order:** Confirm how Speed affects turn order and whether temporary Speed modifiers can alter an already-established turn sequence.
7. **Accuracy and Critical Chance:** Confirm the definitions and calculation rules used by the combat system.
8. **MP cost rounding:** Apply the 1 MP minimum after MP Cost reduction and rounding.

The perk values and secondary effects in this document are provisional. They should be playtested against the game's final combat, attribute, equipment, and progression formulas.

## Bab 8 — Class System

> Sumber: Class System v1.1.

### 1. Overview

Class System defines each character's combat identity, skill access, and build options. A party contains four characters. Combat is turn-based, with individual character turns and reaction-based QTEs used to determine the execution quality of supported actions.

The system supports nine classes. Each class has a distinct core mechanic and a skill tree organized into five Tiers.

#### 1.1 Design Principles

1. Every class should have a distinct tactical identity.
2. No class is mandatory for a successful party composition.
3. Active and passive skills should support more than one viable build where practical.
4. Skill effects must use shared combat systems rather than creating class-specific duplicates.
5. Skill values should be data-driven and balanceable without rewriting core combat code.
6. Descriptions in this document define intended gameplay behavior; unconfirmed numerical values remain TBD.

#### 1.2 Global Class Rules

| System | Rule |
|---|---|
| Party size | 4 characters |
| Character level cap | 15 |
| Skill Tiers | Tier 1 at Level 1; Tier 2 at Level 4; Tier 3 at Level 7; Tier 4 at Level 10; Tier 5 at Level 13 |
| Skill loadout | 3 Active Skill slots + 3 Passive Skill slots per character |
| Skill Rank | Normal skills have Rank 1–3 |
| Skill Points (SP) | Used to unlock and upgrade skills; exact award schedule and costs TBD |
| Tier access | Determined by character level, not by SP |
| Resources | HP and MP only; no class-specific resource pool |
| Loadout changes | At Hub/Camp |
| Weapon compatibility | Strict class weapon compatibility; no Legendary/Mythic exception |
| Skill requirements | A skill may have a specific weapon requirement; requirements are stated per skill when confirmed |
| QTE | QTE quality modifies direct damage and Break Power; it is separate from hit chance and Critical Hit. Only Basic Attack and skills with direct damage use Offensive QTE |
| Numerical tuning | Damage coefficients, MP costs, durations, cooldowns, chances, and caps are TBD unless explicitly noted |

##### 1.2.1 Skill Unlock and Rank

- Character level opens access to a Tier.
- SP is spent to unlock a skill and, where applicable, increase its Rank.
- Rank 1–3 is the standard skill progression.
- Skill Rank effects are skill-specific. Do not assume every skill gains damage, duration, or chance in the same way.
- The exact SP cost per unlock/rank and total SP progression remain TBD.
- Skill loadouts can be changed at the Hub/Camp; combat-time loadout swapping is not part of the baseline design.

##### 1.2.2 Skill Categories

**Active Skill:** An action selected during a character's turn. It may deal damage, heal, apply a status, grant protection, reposition, or alter combat conditions. Active skills generally spend MP; each skill's exact cost is TBD.

**Passive Skill:** A persistent modifier while equipped. Passives may alter stats, skill performance, conditions, or class mechanics. A passive occupies one of the three Passive Skill slots.

**Special Core Skill:** A class mechanic may have a special acquisition or Rank rule. Barbarian's Berserk is the current example: it is an Active Skill with its full designed effect from acquisition and is not upgraded through SP. Its tuning remains TBD.

---

### 2. Combat System Integration

Class skills use the shared combat systems described in the related system documents.

#### 2.1 Damage Types

- **Physical Damage:** Primarily weapon attacks and physical techniques.
- **Elemental Damage:** Fire, Ice, Lightning, Earth, Wind, Water, Holy, and Shadow.
- **Almighty Damage:** A special damage category with its own resolution rules, if a skill explicitly uses it.
- **Nature is a theme / ability identity, not a damage element.** Druid Nature-themed damage should use the damage category and source specified by the skill; the current damage system does not define Nature Damage or Nature Resistance.
- There is no separate Magical Damage category. “Magic” may describe an ability, but its actual damage category must be specified.

#### 2.2 QTE, Hit, and Critical

- QTE grade affects supported direct damage according to the shared QTE system.
- QTE is separate from attack hit chance.
- Ranged Accuracy affects ranged basic attacks and ranged Active Skills; QTE execution does not automatically guarantee a hit.
- Critical Hit applies to Physical Damage under the shared rules. Elemental damage and DoT do not Critical by default.
- Each skill must state whether it invokes a QTE and whether it is single-hit, multi-hit, or multi-component.
- Offensive QTE is used only by Basic Attack and skills with direct damage. Healing, buff, debuff, and control skills without damage do not use QTE.
- QTE grade does not change status chance.
- Multi-hit skills use one sequence QTE: one marker per hit, maximum 6 markers; `qteProfile` may set a smaller marker count. AoE skills use one QTE per hit, applied to every target.
- Poor has no global consequence beyond 0.5× damage and 0.5× Break Power. Additional consequences must come from a core class mechanic or be written in `qteProfile` (maximum one per skill, shown in the tooltip).
- Each class tree may have at most one Perfect-triggered effect in `qteProfile`. Core class mechanics (such as Martial Artist's Perfect combo bonus) are exempt.
- Counter skills trigger only on a Defensive Perfect against a hit that targets the counter user while the counter is active. Maximum one counter per user per enemy action. Counters use no Offensive QTE and deal 1.0× unless `qteProfile` states otherwise.
- Per-target damage coefficients of multi-target skills are at most 60% of an equivalent single-target skill.
- Finisher skills cannot Critical.
- Full QTE rules: Bab 12 (QTE System).

#### 2.3 Status Effects

Skills apply status effects only when their definition explicitly says so. Damage element does not automatically apply a matching status.

Status names used in this document—such as Burn, Freeze, Shock, Curse, Root, Slow, Stun, Shield, and Regeneration—must follow the shared Damage & Status Effect System. Any class-specific duration, chance, or exception is TBD until specified.

#### 2.4 Boss Considerations

Boss resistance and immunity rules come from the shared Status Effect System. Skills that apply hard control, resurrection, large party-wide protection, or delayed burst may require special balance limits. Those limits are marked TBD rather than presumed.

---

### 3. Fighter

#### 3.1 Class Identity — Weapon Techniques

Fighter is a reliable frontline combatant built around weapon mastery, tactical strikes, defensive stances, and counterplay. The Fighter can contribute consistent Physical Damage while protecting the party or exploiting enemy openings.

**Primary role:** Frontline damage / defense
**Core mechanic:** Weapon Techniques
**Build directions:** Offensive weapon specialist; shield defender; counter/tactical fighter
**Weapon requirements:** Weapon-specific requirements may apply to individual skills. Exact compatibility and skill requirements remain TBD unless explicitly stated.

##### Core Gameplay

- Select weapon techniques to match the current enemy situation.
- Use defensive skills to mitigate danger and counter attacks.
- Use armor or guard disruption to create openings for party damage.
- Fighter's passives improve weapon consistency, defense, tactical awareness, or counter performance.

#### 3.2 Skill Tree

##### Tier 1 — Level 1

**Active Skills**
- **Shield Bash** — Strikes a target with a shield, dealing Physical Damage and potentially disrupting its action or posture. The exact disruption effect and shield requirement are TBD.
- **Power Strike** — A committed weapon attack that deals increased Physical Damage compared with a basic attack. Exact coefficient and weapon requirement are TBD.
- **Guard Stance** — Adopts a defensive stance for a limited duration, improving the Fighter's defense or damage mitigation. Duration and modifier are TBD.

**Passive Skills**
- **Weapon Proficiency** — Improves the Fighter's effectiveness with compatible weapons. The affected stat or damage modifier is TBD.
- **Defensive Training** — Improves the Fighter's survivability through a defensive stat or damage-reduction modifier. Exact parameter is TBD.

##### Tier 2 — Level 4

**Active Skills**
- **Cleaving Strike** — Sweeping weapon attack that can hit multiple enemies. Target count, range, and damage distribution are TBD.
- **Shield Wall** — Uses a shield to strengthen defense, potentially protecting the Fighter or nearby/allied targets. Target scope and duration are TBD.
- **Weapon Disarm** — Attempts to disarm an eligible enemy, reducing or disabling its weapon-based threat. Enemy eligibility, chance, and duration are TBD.

**Passive Skills**
- **Combat Discipline** — Improves combat consistency, such as reducing a penalty or strengthening a core combat parameter. Exact effect is TBD.
- **Shield Mastery** — Enhances shield-based skills or shield defense. Exact modifier and qualifying actions are TBD.

##### Tier 3 — Level 7

**Active Skills**
- **Counterattack** — Prepares or triggers a retaliatory strike when a defined enemy attack condition is met. Triggers on a Defensive Perfect while active (shared counter rule, §2.2); damage is TBD.
- **Armor Break** — Deals damage and reduces the target's defense or armor for a duration. The affected defense and duration are TBD.
- **Tactical Strike** — A precise attack that gains an additional benefit when a tactical condition is met, such as a vulnerable or controlled target. Condition and bonus are TBD.

**Passive Skills**
- **Tactical Awareness** — Improves the Fighter's response to enemy actions or tactical conditions. Exact trigger and benefit are TBD.
- **Weapon Adaptation** — Improves flexibility across compatible weapon types. Whether this changes skill performance or grants a general modifier is TBD.

##### Tier 4 — Level 10

**Active Skills**
- **Whirlwind** — A spinning attack that deals Physical Damage to multiple nearby enemies. Target count and damage are TBD.
- **Iron Fortress** — Enters a high-defense stance that substantially improves mitigation or resistance to disruption. Duration and values are TBD.
- **Execution Strike** — Delivers a powerful attack against an enemy meeting a vulnerability condition. The qualifying condition and bonus are TBD.

**Passive Skills**
- **Iron Will** — Improves resistance to control, disruption, or damage under a defined condition. Exact effect is TBD.
- **Counter Specialist** — Improves Counterattack or other counter-based actions. Exact bonus and scope are TBD.

##### Tier 5 — Level 13

**Active Skills**
- **Blade Tempest** — Performs a rapid series of weapon attacks. Hit count, target rules, and per-hit scaling are TBD.
- **Unbreakable** — Activates an exceptional defensive effect that helps the Fighter withstand heavy pressure. Exact protection, duration, and limits are TBD.
- **Final Verdict** — A high-impact finishing strike, potentially gaining power against weakened or vulnerable targets. Condition and scaling are TBD.

**Passive Skills**
- **Master of Arms** — Represents peak weapon mastery and improves a defined set of weapon-based combat parameters. Exact modifiers are TBD.
- **Unyielding Defense** — Strengthens defensive performance, particularly against heavy or repeated attacks. Exact effect is TBD.

---

### 4. Barbarian

#### 4.1 Class Identity — Berserk State

Barbarian is an aggressive Physical Damage class that trades controlled defense for overwhelming pressure and resilience. Its signature state is **Berserk**.

**Primary role:** Frontline physical damage
**Core mechanic:** Berserk State
**Build directions:** Sustained aggression; heavy burst; high-risk damage with survival tools

##### Berserk — Special Core Skill

- Berserk is an **Active Skill**, not an automatic state.
- Activating it uses the Barbarian's turn.
- Berserk grants its full designed effect when acquired; it is treated as Rank 3 for the current concept and is not upgraded using SP.
- Proposed bonus themes include Physical Damage, Critical Chance, and Damage Reduction, but exact effects, values, duration, and restrictions are TBD.
- Berserk does not create a separate resource pool.
- Berserk's final interaction with other buffs and status effects must follow shared stacking rules.

#### 4.2 Skill Tree

##### Tier 1 — Level 1

**Active Skills**
- **Savage Strike** — A brutal basic technique that deals Physical Damage. Coefficient and any additional effect are TBD.
- **Berserk** — Activates the Barbarian's signature Berserk State, granting its defined combat bonuses for a limited duration. Exact parameters are TBD; this skill is not SP-upgraded.
- **Intimidating Roar** — Roars at enemies to apply an intimidation effect or weaken their combat performance. Exact status, target scope, and duration are TBD.

**Passive Skills**
- **Savage Instinct** — Barbarian's starter passive; improves a core offensive or survival parameter that supports aggressive play. Exact effect is TBD.
- **Bloodthirst** — Rewards aggressive combat through a defined benefit, potentially tied to dealing damage or defeating an enemy. Trigger and value are TBD.

##### Tier 2 — Level 4

**Active Skills**
- **Reckless Swing** — A powerful attack with a risk/reward tradeoff. The intended downside and damage bonus are TBD.
- **Crushing Blow** — A heavy strike designed to deal high Physical Damage and/or pressure enemy Break. Exact Break Power and coefficient are TBD.
- **Blood Rush** — A rush of aggression that improves the Barbarian's momentum or attacks a target with a momentum-related benefit. Exact behavior is TBD.

**Passive Skills**
- **Unrelenting Fury** — Improves sustained offensive pressure. Exact modifier and trigger are TBD.
- **Pain Tolerance** — Improves the Barbarian's ability to withstand damage or maintain offense while injured. Exact effect is TBD.

##### Tier 3 — Level 7

**Active Skills**
- **Rampage** — Performs a sequence of aggressive attacks. Hit count, target rules, and scaling are TBD.
- **Brutal Cleave** — A heavy area attack that hits multiple enemies. Target count and damage distribution are TBD.
- **Blood Frenzy** — Activates or exploits a heightened aggressive condition, potentially benefiting from damage dealt or low enemy HP. Trigger and duration are TBD.

**Passive Skills**
- **Berserker's Momentum** — Improves the Barbarian after an offensive action or while Berserk is active. Exact trigger and bonus are TBD.
- **Predator's Instinct** — Improves damage or effectiveness against vulnerable enemies. Vulnerability condition and modifier are TBD.

##### Tier 4 — Level 10

**Active Skills**
- **Devastating Smash** — A crushing attack with high damage and/or Break pressure. Exact target scope and values are TBD.
- **War Cry** — A battle cry that grants a benefit to the Barbarian and/or applies a combat debuff to enemies. Target scope and effect are TBD.
- **Death Rush** — A forceful attack that pressures a target, potentially gaining a bonus against weakened enemies. Exact condition and effect are TBD.

**Passive Skills**
- **Unstoppable Rage** — Improves resistance to interruption or control while attacking or Berserk. Exact condition and effect are TBD.
- **Blood Pact** — A sacrifice- or survival-themed passive. Its actual cost, trigger, and benefit remain TBD; no HP sacrifice is assumed.

##### Tier 5 — Level 13

**Active Skills**
- **Primal Carnage** — A devastating area attack focused on overwhelming physical force. Target count and scaling are TBD.
- **Wrath Incarnate** — A high-tier aggression skill that embodies the Barbarian's rage. Its precise effect and relation to Berserk are TBD.
- **Cataclysm** — A major finishing attack with high impact. Damage, target scope, and usage limits are TBD.

**Passive Skills**
- **Endless Fury** — Extends or strengthens the Barbarian's sustained aggression. Exact mechanic is TBD.
- **Avatar of Destruction** — The Barbarian's peak offensive passive. Its modifiers and conditions are TBD.

---

### 5. Martial Artist

#### 5.1 Class Identity — Combo Flow

Martial Artist fights unarmed and builds a combo through reaction-based QTE performance. The class emphasizes timing, flow, and converting successful actions into a powerful finisher.

**Primary role:** Unarmed physical damage / Break pressure
**Core mechanic:** QTE-based Combo Flow
**Build directions:** Combo builder; QTE specialist; finisher-focused build

##### Combo Flow — Current Rules

- Combo is built through attacks and their QTE outcomes, not by requiring a fixed sequence of specific skills.
- Good increases combo by the skill's base gain; Perfect adds +1 on top; Poor resets combo to 0 (exceptions: Flowing Combo, Unbroken Flow).
- Combo resets if Martial Artist goes one full turn without attacking.
- Combo resets when Martial Artist becomes Broken.
- A finisher consumes the entire current combo.
- The starter passive **Martial Discipline** increases Physical Break Power from unarmed attacks.
- Maximum Combo: 10 (12 with Limitless Potential). Gain above the cap is lost; combo starts at 0 each battle.
- Each combo point grants +2% Physical Damage to non-finisher Martial Artist attacks (+3% with Combo Mastery).
- Combo gain and reset use the final QTE result, including a Lucky retry. Missed hits (failed Hit Chance) still grant gain according to their QTE grade.
- Counters grant combo but do not count as attacking on the Martial Artist's own turn.
- Sequence skills resolve gain per marker in order; a Poor marker resets combo at that point; +1 bonus if every marker is Perfect.
- Finishers consume all combo after the final QTE result, regardless of grade or hit; they never grant gain and cannot Critical.
- With the Auto-Good accessibility setting, Maximum Combo becomes 60% of normal (6, or 7 with Limitless Potential), and Perfect-only effects (including counters) are inactive.

#### 5.2 Skill Tree

##### Tier 1 — Level 1

**Active Skills**
- **Quick Jab** — A fast unarmed strike intended to build combo reliably. Combo gain: 2. Damage is TBD (intended to be low).
- **Rising Kick** — An upward kick that deals Physical Damage and may pressure Break. Combo gain: 1. Exact effects are TBD.
- **Focused Breathing** — An active focus technique that prepares the Martial Artist for improved execution or combat performance. No QTE; grants no combo and does not count as a turn without attacking. Exact buff, duration, and cost are TBD.

**Passive Skills**
- **Martial Discipline** — Starter passive; increases Physical Break Power from unarmed attacks.
- **Combat Rhythm** — Combo gain from Basic Attack and Quick Jab +1.

##### Tier 2 — Level 4

**Active Skills**
- **Palm Strike** — A focused palm attack that deals Physical Damage and may have a defined tactical effect. Combo gain: 1. Exact effect is TBD.
- **Sweep Kick** — A sweeping kick that attacks or disrupts one or more targets. Combo gain: 1 per use (not per target). Target scope and effect are TBD.
- **Flowing Combo** — A technique that continues the Martial Artist's combo flow. Combo gain: 2. `qteProfile`: Poor halves combo (rounded down) instead of resetting it.

**Passive Skills**
- **Combo Mastery** — Combo bonus per point increases from +2% to +3% Physical Damage.
- **Agile Defense** — Improves defensive response while maintaining the Martial Artist's agile fighting style: Defensive QTE window +10% (shared window cap +25%).

##### Tier 3 — Level 7

**Active Skills**
- **Dragon Fist** — A powerful unarmed strike with high Physical Damage and/or Break pressure. Combo gain: 1. Exact values are TBD.
- **Spinning Kick** — A rotating kick that can hit multiple targets or apply a tactical effect. Combo gain: 1 per use. Target scope is TBD.
- **Counterstrike** — A retaliatory unarmed attack. Triggers on a Defensive Perfect (shared counter rule); no Offensive QTE; 1.0× damage. Combo gain: 1.

**Passive Skills**
- **Inner Focus** — Offensive QTE window +10% while combo is 5 or higher (shared window cap +25%).
- **Pressure Point** — Improves the Martial Artist's ability to exploit enemy vulnerabilities. Exact debuff, Break, or damage interaction is TBD.

##### Tier 4 — Level 10

**Active Skills**
- **Hundred Fists** — A rapid series of unarmed attacks: 10 hits in 5 sequence markers. Combo gain 1 per Good/Perfect marker, +1 if all markers are Perfect (maximum 6). Per-hit scaling is TBD.
- **Flying Knee** — A high-impact knee strike. Single Offensive QTE; combo gain 1. Damage and Break Power are TBD.
- **Perfect Counter** — A counter technique triggered by a Defensive Perfect. No Offensive QTE; `qteProfile`: 1.5× damage (this tree's one Perfect-triggered effect). Combo gain: 2.

**Passive Skills**
- **Unbroken Flow** — Once per battle, a Poor halves combo instead of resetting it.
- **Ki Control** — Improves the Martial Artist's control over combat power or resource efficiency. “Ki” is thematic; it does not introduce a separate resource pool.

##### Tier 5 — Level 13

**Active Skills**
- **Dragon's Fury** — Combo finisher, usable at combo 3 or higher. Finisher multiplier F = 1 + 0.15 × combo, applied as a Damage Bonus before QTE. Cannot Critical. Base damage is TBD.
- **Ascendant Strike** — A high-tier strike with a defined benefit tied to execution or combo. Combo gain: 1. Exact behavior is TBD.
- **Final Technique** — A peak Martial Artist finisher. Requires full combo (10, or 12 with Limitless Potential; 6/7 under Auto-Good) and consumes it. F = 1 + 0.2 × combo; once per battle; cannot Critical. Base damage is TBD.

**Passive Skills**
- **Grandmaster** — Represents mastery of unarmed combat and improves defined Martial Artist parameters. Exact effects are TBD.
- **Limitless Potential** — Maximum Combo 12; Perfect combo bonus increases from +1 to +2.

---

### 6. Rogue

#### 6.1 Class Identity — Exploit

Rogue is a precision attacker who exploits enemy conditions, vulnerabilities, and openings. Its core identity is **Exploit**, not a universal Mark mechanic.

**Primary role:** Physical damage / opportunistic burst
**Core mechanic:** Exploit enemy conditions
**Build directions:** Debuff exploiter; execution specialist; evasive assassin

##### Exploit — Design Concept

Proposed exploit conditions include:
- Target is affected by a debuff.
- Target is Broken.
- Target is at low HP.
- Target is preparing or charging an attack.

These are concept triggers, not a finalized global mechanic. Exact trigger definitions, bonuses, and whether several conditions can activate simultaneously remain TBD. Rogue's **Death Mark** is an individual skill and does not replace the Exploit class identity.

#### 6.2 Skill Tree

##### Tier 1 — Level 1

**Active Skills**
- **Quick Slash** — A fast blade attack that deals Physical Damage. Exact coefficient is TBD.
- **Backstab** — A strike that gains a tactical advantage against an eligible target or from a defined position. Dagger requirement is a draft; final weapon requirement is TBD.
- **Smoke Bomb** — Creates a smoke effect that disrupts enemies and/or improves Rogue evasion. Target scope, duration, and status are TBD.

**Passive Skills**
- **Opportunist** — Starter passive; increases Critical Chance against debuffed enemies.
- **Evasive Instinct** — Improves Rogue evasion or defensive response. Exact modifier is TBD.

##### Tier 2 — Level 4

**Active Skills**
- **Shadow Strike** — A shadow-themed attack that deals damage and may benefit from a tactical opening. Damage type and extra effect are TBD.
- **Poison Blade** — An attack that applies Poison on a successful, eligible hit. Poison chance, potency, and duration follow the shared status rules and remain TBD.
- **Feint** — A deceptive maneuver that creates an opening, weakens a target, or improves Rogue's next action. Exact effect is TBD.

**Passive Skills**
- **Dirty Fighting** — Improves the Rogue's effectiveness when using debuffs, conditions, or tactical openings. Exact effect is TBD.
- **Precision** — Improves a defined accuracy, critical, or damage parameter. Exact stat is TBD.

##### Tier 3 — Level 7

**Active Skills**
- **Assassinate** — A high-impact attack against a target meeting a vulnerability condition. Condition and scaling are TBD.
- **Shadowstep** — Repositions the Rogue or grants an evasive shadow movement effect. Movement rules, duration, and target restrictions are TBD.
- **Weakpoint Strike** — Attacks a target's weak point, gaining a defined damage or Break benefit. Exact effect is TBD.

**Passive Skills**
- **Exploit Weakness** — Improves the payoff from Rogue's Exploit conditions. Exact bonus and stacking behavior are TBD.
- **Lethal Focus** — Improves a defined offensive parameter, potentially when targeting a vulnerable enemy. Exact trigger is TBD.

##### Tier 4 — Level 10

**Active Skills**
- **Phantom Dance** — A sequence of evasive or shadow-based attacks. Hit count, defensive effect, and damage are TBD.
- **Death Mark** — Applies a skill-specific mark that makes the target more vulnerable to Rogue's follow-up. Duration and effect are TBD; this is not the class's core mechanic.
- **Vanishing Act** — Temporarily reduces enemy focus or improves Rogue's evasion/positioning. Exact targeting and duration are TBD.

**Passive Skills**
- **Shadow Affinity** — Improves the effectiveness of eligible shadow-themed skills. Damage category and modifier scope are TBD.
- **Executioner** — Improves Rogue's effectiveness against targets near defeat. Threshold and bonus are TBD.

##### Tier 5 — Level 13

**Active Skills**
- **Shadow Requiem** — A high-tier shadow attack or attack sequence. Damage, hit count, and conditions are TBD.
- **Thousand Cuts** — Performs a rapid multi-hit attack using one sequence QTE (one marker per hit, maximum 6 markers). Hit count and per-hit damage are TBD.
- **Final Ambush** — A finishing ambush that exploits a target's vulnerable state. Condition and payoff are TBD.

**Passive Skills**
- **Master Assassin** — Rogue's peak assassin passive, improving a defined set of offensive or exploit parameters. Exact effects are TBD.
- **Death's Embrace** — A high-tier offensive passive associated with finishing or vulnerable targets. Exact trigger and modifier are TBD.

---

### 7. Ranger

#### 7.1 Class Identity — Hunt Mark

Ranger is a ranged specialist who tracks and marks a target, then increases **Ranger's own damage** against that marked target. Hunt Mark does not grant the same damage bonus to the rest of the party.

**Primary role:** Ranged Physical Damage
**Core mechanic:** Hunt Mark
**Build directions:** Mark-focused single target; ranged area damage; accuracy and precision

##### Hunt Mark — Design Concept

- Hunter's Mark applies Hunt Mark to an eligible target.
- Current draft assumes one active marked target at a time.
- Ranger's attacks gain a damage bonus against the marked target.
- The mark's duration, bonus, target switching, and refresh behavior remain TBD.
- Hunt Mark does not automatically increase party-wide damage.

##### Ranged Accuracy

Hunter's Instinct increases Ranged Accuracy for ranged basic attacks and ranged Active Skills. Ranged Accuracy affects hit chance; QTE grade is a separate execution outcome.

#### 7.2 Skill Tree

##### Tier 1 — Level 1

**Active Skills**
- **Quick Shot** — A fast ranged attack with a single Offensive QTE. Damage and accuracy interaction are TBD.
- **Hunter's Mark** — Applies Hunt Mark to a target, enabling Ranger's personal damage bonus against it. Exact bonus and duration are TBD.
- **Evasive Shot** — Fires while taking an evasive action, combining ranged damage with a defensive benefit. Exact effect is TBD.

**Passive Skills**
- **Hunter's Instinct** — Starter passive; increases Ranged Accuracy for ranged basic attacks and ranged Active Skills.
- **Keen Eye** — Improves aiming or target awareness. Exact accuracy, critical, or scouting effect is TBD.

##### Tier 2 — Level 4

**Active Skills**
- **Piercing Arrow** — Fires an arrow that pierces or penetrates eligible targets/defenses. Exact penetration rule and target count are TBD.
- **Volley** — Fires a spread or volley of arrows at multiple enemies. Target count and damage distribution are TBD.
- **Crippling Shot** — A ranged attack that applies a weakening effect, such as reduced movement/Speed or another defined combat parameter. Exact status is TBD.

**Passive Skills**
- **Marksmanship** — Improves ranged attack performance. Exact modifier is TBD.
- **Predator's Focus** — Improves Ranger's performance against a tracked or marked target. Exact trigger and effect are TBD.

##### Tier 3 — Level 7

**Active Skills**
- **Snipe** — A precision ranged attack with high single-target potential and a single Offensive QTE. Accuracy requirement and coefficient are TBD.
- **Explosive Arrow** — Fires an arrow that explodes, dealing damage to a target area. Element/damage type and area rules are TBD.
- **Trap Setup** — Places a trap that triggers under defined enemy conditions. Trap type, trigger, duration, and damage/control are TBD.

**Passive Skills**
- **Improved Tracking** — Improves tracking-related benefits, such as Hunt Mark handling or exploration tracking. Exact scope is TBD.
- **Deadly Aim** — Improves precision damage or another defined ranged offensive parameter. Exact modifier is TBD.

##### Tier 4 — Level 10

**Active Skills**
- **Rain of Arrows** — An area attack that rains arrows over a target zone or enemy group. Target count and damage are TBD.
- **Shadow Shot** — A special ranged attack with a shadow-themed effect. Damage category, status, and utility are TBD.
- **Pinning Shot** — A ranged attack that pins or restricts an eligible target. Status, chance, duration, and boss behavior are TBD.

**Passive Skills**
- **Tactical Hunter** — Improves the Ranger's tactical options or ranged performance under a defined condition. Exact effect is TBD.
- **Eagle Eye** — Improves a defined precision or ranged accuracy parameter. Exact value and scope are TBD.

##### Tier 5 — Level 13

**Active Skills**
- **Deadeye** — A high-tier precision attack against one target with a single Offensive QTE. Damage and accuracy are TBD.
- **Nature's Wrath** — A nature-themed ranged attack. It does not imply a Nature Damage type; the actual damage category and effects are TBD.
- **Perfect Hunt** — A peak Hunt Mark technique that exploits the marked target. Exact mark interaction and damage are TBD.

**Passive Skills**
- **Master Hunter** — Ranger's peak hunting passive, improving defined ranged or tracking parameters. Exact effect is TBD.
- **Apex Predator** — Improves Ranger's effectiveness against a defined prey condition, potentially marked or vulnerable targets. Trigger and bonus are TBD.

---

### 8. Cleric

#### 8.1 Class Identity — Divine Protection

Cleric is a hybrid healer that combines HP restoration, protection, and Holy offense. Cleric is not restricted to healing only; its skills can support the party defensively and contribute damage.

**Primary role:** Hybrid healer / protector / Holy offense
**Core mechanic:** Divine Protection
**Build directions:** Healing specialist; shield/protection support; Holy offense support

##### Healing and Protection Rules

- **Divine Grace** increases Healing Power from Cleric skills.
- Divine Grace does **not** automatically increase Shield Power.
- Shield and damage-reduction effects use their own relevant modifiers.
- Resurrection, Guardian Angel, and Miracle may require use limits or conditions; those limits are TBD.
- Holy skills use the shared Elemental Damage rules for Holy, unless a skill explicitly defines another category.

#### 8.2 Skill Tree

##### Tier 1 — Level 1

**Active Skills**
- **Divine Heal** — Restores HP to one ally. Healing amount, scaling, and MP cost are TBD.
- **Holy Light** — Deals Holy Damage to an eligible enemy with a single Offensive QTE. Damage and target rules are TBD.
- **Blessing** — Grants a beneficial effect to an ally, such as protection or improved combat performance. Exact buff and duration are TBD.

**Passive Skills**
- **Divine Grace** — Starter passive; increases Healing Power from Cleric skills. It does not automatically increase Shield Power.
- **Sacred Vitality** — Improves a defined survivability parameter, such as HP or healing received. Exact effect is TBD.

##### Tier 2 — Level 4

**Active Skills**
- **Prayer of Healing** — Restores HP to multiple party members. Target scope, healing distribution, and cost are TBD.
- **Divine Shield** — Grants Shield or another defined protective effect to an eligible target. Shield amount, duration, and stacking are TBD.
- **Purify** — Removes eligible negative status effects from a target. Cleanse scope and number of instances removed are TBD.

**Passive Skills**
- **Healing Hands** — Improves Cleric healing performance. Exact modifier and scope are TBD.
- **Protective Faith** — Improves protection effects, such as Shield or Damage Reduction, without assuming an increase to Healing Power. Exact effect is TBD.

##### Tier 3 — Level 7

**Active Skills**
- **Sanctuary** — Creates an area or party protection effect that may include healing or damage mitigation. Exact mechanics and duration are TBD.
- **Holy Smite** — Deals Holy Damage to an enemy with a single Offensive QTE. Damage and scaling are TBD.
- **Resurrection** — Revives a fallen ally. Target conditions, restored HP, MP cost, and usage restrictions are TBD.

**Passive Skills**
- **Divine Aegis** — Improves Shield or protection effectiveness. Exact modifier is TBD.
- **Spiritual Focus** — Improves a defined spell-related parameter, such as MP efficiency or accuracy. Exact effect is TBD.

##### Tier 4 — Level 10

**Active Skills**
- **Mass Restoration** — Restores HP to the party or multiple allies. Target scope, amount, and cost are TBD.
- **Guardian Angel** — Grants a powerful protective effect to an ally, potentially preventing or mitigating a critical threat. Exact effect and limits are TBD.
- **Judgment** — A Holy offensive skill that deals damage and may apply a defined secondary effect. Exact effect is TBD.

**Passive Skills**
- **Blessed Recovery** — Improves recovery from healing or another defined restorative effect. Exact scope is TBD.
- **Unwavering Faith** — Improves Cleric's reliability or resistance to disruption. Exact effect is TBD.

##### Tier 5 — Level 13

**Active Skills**
- **Divine Intervention** — A high-tier intervention that protects or restores allies in a critical situation. Trigger/activation, target scope, and limits are TBD.
- **Heaven's Wrath** — Deals Holy Damage to multiple enemies with one Offensive QTE per hit, applied to every target. Area and damage are TBD (per-target coefficient at most 60% of single-target).
- **Miracle** — Cleric's peak healing/protection ability. Exact effect, target scope, cost, and usage limit are TBD.

**Passive Skills**
- **Saint's Resolve** — A peak defensive/support passive. Exact modifiers and conditions are TBD.
- **Eternal Blessing** — A peak blessing-related passive that improves a defined support effect. Exact scope and values are TBD.

---

### 9. Wizard

#### 9.1 Class Identity — Elemental Specialization

Wizard is a flexible spellcaster with skills that use different elements. The player does **not** choose one permanent element for the class; elemental focus emerges from the skills and build selected.

**Primary role:** Elemental / Arcane damage
**Core mechanic:** Flexible elemental skill selection
**Build directions:** Fire damage; Ice control; Lightning attacks; Arcane damage; mixed-element caster

##### Elemental Rules

- Individual skills specify their damage element.
- A Wizard may use Fire, Ice, Lightning, Arcane, or another supported element according to the skill.
- Elemental Damage Bonus and element-specific bonuses follow the shared damage modifier rules.
- “Magic Damage” in early skill notes is descriptive only; the damage category must be Physical, Elemental, or Almighty as defined by the shared system.
- No permanent element specialization is imposed by class selection.

#### 9.2 Skill Tree

##### Tier 1 — Level 1

**Active Skills**
- **Arcane Bolt** — Deals Arcane Elemental Damage to one target. Damage, scaling, and cost are TBD.
- **Fireball** — Deals Fire Elemental Damage to a target or area. Area size and damage distribution are TBD.
- **Frost Nova** — Deals Ice Elemental Damage in an area and applies Slow as defined by the skill. Slow strength and duration are TBD.

**Passive Skills**
- **Arcane Affinity** — Starter passive; reduces MP Cost of Arcane spells.
- **Elemental Knowledge** — Improves Elemental Damage. Exact modifier and scope are TBD.

##### Tier 2 — Level 4

**Active Skills**
- **Lightning Strike** — Deals Lightning Elemental Damage to one target. Damage and any additional effect are TBD.
- **Ice Lance** — Deals Ice Elemental Damage with a penetration-oriented effect. The meaning of penetration—defense, targets, or another rule—is TBD.
- **Arcane Burst** — Deals Arcane Elemental Damage to multiple enemies or an area. Target scope is TBD.

**Passive Skills**
- **Spell Mastery** — Improves spell damage according to a defined scope. Exact modifier is TBD.
- **Mana Efficiency** — Reduces MP Cost of eligible spells. Scope and value are TBD.

##### Tier 3 — Level 7

**Active Skills**
- **Meteor** — A large Fire Elemental Damage area attack. Area, damage, and any delay are TBD.
- **Blizzard** — Deals Ice Elemental Damage over an area and applies Slow or another defined Ice effect. Duration and values are TBD.
- **Chain Lightning** — Lightning Elemental Damage that jumps between eligible targets. Jump count and falloff are TBD.

**Passive Skills**
- **Elemental Focus** — Improves elemental spell performance. Exact modifier and conditions are TBD.
- **Arcane Precision** — Improves a defined spell accuracy or hit parameter. Exact scope is TBD.

##### Tier 4 — Level 10

**Active Skills**
- **Inferno** — Deals Fire Elemental Damage in an area and may apply Burn if the skill definition confirms it. Burn chance, potency, and duration are TBD.
- **Absolute Zero** — Deals Ice Elemental Damage and applies Freeze or another defined control effect. Exact control behavior and boss handling follow shared rules.
- **Arcane Overload** — A powerful Arcane Elemental Damage spell. Damage, cost, and any drawback are TBD.

**Passive Skills**
- **Spell Amplification** — Improves eligible spell damage. Exact scope and modifier are TBD.
- **Magical Resilience** — Improves resistance or mitigation against a defined damage category. The exact category and value are TBD; no separate Magical Damage category is assumed.

##### Tier 5 — Level 13

**Active Skills**
- **Cataclysm** — A major elemental area attack. Element, target scope, and damage are TBD.
- **Apocalypse** — A high-tier spell with substantial damage. Element, area, and additional effects are TBD.
- **Grand Arcanum** — Wizard's peak Arcane spell. Damage, cost, target scope, and special rules are TBD.

**Passive Skills**
- **Archmage** — Wizard's peak spellcaster passive, improving defined spell damage and/or accuracy parameters. Exact modifiers are TBD.
- **Infinite Potential** — Improves a defined MP efficiency or spell progression parameter. Exact effect is TBD.

---

### 10. Warlock

#### 10.1 Class Identity — Curse Mastery

Warlock is a dark spellcaster that specializes in curses, debuffs, and exploiting cursed targets.

**Primary role:** Debuff / damage over time / Dark Elemental Damage
**Core mechanic:** Curse Mastery
**Build directions:** Curse damage over time; debuff support; cursed-target burst

##### Curse Rules

- Each Curse effect is independent.
- Different Curse effects may coexist on the same target.
- There is no automatic Curse stacking, merging, or combination.
- A new instance follows the shared Status Effect rules; do not assume it refreshes or extends another instance.
- Curse's actual effect is defined by the skill that applies it.
- Exact behavior when the target dies or becomes Broken remains TBD where not defined by the shared system.

#### 10.2 Skill Tree

##### Tier 1 — Level 1

**Active Skills**
- **Shadow Bolt** — Deals Dark Elemental Damage to one target. Damage and cost are TBD.
- **Curse of Weakness** — Applies a Curse that reduces the target's Physical Damage output. Modifier and duration are TBD.
- **Corruption** — Applies a Curse-based Damage over Time effect. Damage type, stat scaling, tick timing, and duration are TBD.

**Passive Skills**
- **Pact Affinity** — Starter passive; reduces MP Cost of Pact spells.
- **Lingering Curse** — Extends the duration of eligible Curses. Exact scope and duration modifier are TBD; it does not imply automatic stacking.

##### Tier 2 — Level 4

**Active Skills**
- **Curse of Vulnerability** — Applies a Curse that increases damage received by the target. Modifier scope, duration, and stacking are TBD.
- **Soul Drain** — Deals damage and restores a portion of Warlock's HP. Damage, healing ratio, and damage category are TBD.
- **Shadow Burst** — Deals Dark Elemental Damage to an area. Target count and damage are TBD.

**Passive Skills**
- **Curse Mastery** — Improves the effectiveness of eligible Curses. Exact parameter is TBD.
- **Dark Knowledge** — Improves Dark Elemental Damage. Exact modifier is TBD.

##### Tier 3 — Level 7

**Active Skills**
- **Curse of Agony** — Applies a Curse DoT whose tick damage increases according to a defined progression. Growth rule, cap, and duration are TBD.
- **Soul Harvest** — Deals bonus damage or gains another effect when the target is Cursed. Exact trigger and bonus are TBD.
- **Hex** — Applies a Curse that reduces the target's Elemental Damage output or another explicitly defined spell-related parameter. Exact stat and duration are TBD.

**Passive Skills**
- **Malicious Intent** — Increases Critical Chance against Cursed enemies. Critical applies only to eligible Physical Damage under shared rules; whether this passive also changes another mechanic is TBD.
- **Dark Ritual** — Improves Warlock damage against Cursed targets. Damage scope and modifier are TBD.

##### Tier 4 — Level 10

**Active Skills**
- **Curse of Doom** — Applies a delayed Curse damage effect. Delay, damage, removal behavior, and duration are TBD.
- **Nightfall** — Deals Dark Elemental Damage in an area and applies an Accuracy debuff or other specified weakening effect. Exact debuff is TBD.
- **Soul Collapse** — Deals burst damage with a bonus against Cursed targets. Bonus and Curse conditions are TBD.

**Passive Skills**
- **Forbidden Power** — Improves Dark Elemental Damage or a defined high-risk spell effect. Exact behavior is TBD.
- **Eternal Affliction** — Improves Curse duration or persistence. Exact scope and value are TBD; Curse instances remain independent.

##### Tier 5 — Level 13

**Active Skills**
- **Apocalypse** — Applies a high-tier Curse effect to multiple enemies, weakening them and/or dealing damage. Exact effects and target scope are TBD.
- **Soul Annihilation** — A burst attack against Cursed targets. Damage, conditions, and target scope are TBD.
- **Absolute Damnation** — A major Curse with a large or delayed effect. Exact mechanic, duration, and limits are TBD.

**Passive Skills**
- **Grand Curse Master** — Warlock's peak Curse passive, improving a defined set of Curse parameters. Exact effects are TBD.
- **Abyssal Dominion** — Improves Warlock's damage or control over Cursed enemies. Exact trigger and modifier are TBD.

---

### 11. Druid

#### 11.1 Class Identity — Nature's Control

Druid is a hybrid class combining nature-themed damage, battlefield control, and healing. Its primary identity is tactical control, not being a dedicated healer like Cleric.

**Primary role:** Hybrid damage / control / healing
**Core mechanic:** Nature's Control
**Build directions:** Root/control; Nature-themed damage; hybrid healing and protection

##### Nature Rules

- “Nature” is a skill theme and class identity, not a damage element in the shared damage taxonomy.
- Druid damage skills must specify their actual damage category, scaling, and any affinity interactions.
- Healing, Shield, Root, Slow, and Regeneration use the shared combat/status systems.
- Root/control duration and boss interaction require careful balancing.

#### 11.2 Skill Tree

##### Tier 1 — Level 1

**Active Skills**
- **Thorn Shot** — A nature-themed single-target attack. Its actual damage category, scaling, and additional effects are TBD.
- **Entangling Roots** — Applies Root or another defined movement/control restriction to an eligible target. Duration and boss handling follow shared rules and remain TBD.
- **Nature's Embrace** — Restores HP to one ally through nature-themed healing. Healing amount and scaling are TBD.

**Passive Skills**
- **Nature's Affinity** — Starter passive; increases the defined damage output of Druid's nature-themed magic skills. Exact damage category and modifier must be specified during balance.
- **Natural Recovery** — Improves Healing Power for Druid's Nature-themed healing skills.

##### Tier 2 — Level 4

**Active Skills**
- **Vine Lash** — Deals nature-themed damage and applies Slow or another defined control effect. Actual damage category and status values are TBD.
- **Healing Bloom** — Restores HP to multiple party members. Target scope and healing distribution are TBD.
- **Barkskin** — Grants one ally Damage Reduction or another defined defensive effect. Modifier and duration are TBD.

**Passive Skills**
- **Thorn Mastery** — Improves the damage or effectiveness of eligible thorn-themed skills. Exact scope is TBD.
- **Deep Roots** — Improves Root duration or effectiveness. Exact modifier and boss restrictions are TBD.

##### Tier 3 — Level 7

**Active Skills**
- **Nature's Wrath** — Deals nature-themed damage to multiple enemies. Actual damage category, area, and scaling are TBD.
- **Overgrowth** — Applies Root or a related control effect to multiple eligible enemies. Target count and duration are TBD.
- **Regrowth** — Applies Healing over Time to one ally. Tick timing, scaling, and duration are TBD.

**Passive Skills**
- **Wild Resilience** — Improves resistance or mitigation against defined damage categories or status effects. Exact scope is TBD.
- **Flourishing Life** — Improves Regeneration or Healing over Time effectiveness. Exact modifier is TBD.

##### Tier 4 — Level 10

**Active Skills**
- **Wrath of the Forest** — Deals nature-themed area damage and applies Root or another defined control effect. Target scope and duration are TBD.
- **Ancient Sanctuary** — Provides party healing and/or Damage Reduction. Exact effect, duration, and target scope are TBD.
- **Thorn Prison** — Restricts an eligible target and deals nature-themed damage. Control type, damage category, duration, and boss handling are TBD.

**Passive Skills**
- **Ancient Knowledge** — Improves nature-themed damage and Healing Power. Exact categories and modifiers are TBD.
- **Nature's Protection** — Improves Druid's Shield or protection effects. Exact scope is TBD.

##### Tier 5 — Level 13

**Active Skills**
- **World Tree's Blessing** — Restores HP to multiple allies and applies Regeneration. Target scope, amount, and duration are TBD.
- **Primal Cataclysm** — Deals high nature-themed damage to all or multiple enemies. Actual damage category and scaling are TBD.
- **Nature's Dominion** — Creates a controlled battlefield effect involving Root and nature-themed damage. Area, target limits, duration, and boss behavior are TBD.

**Passive Skills**
- **Master of the Wild** — Druid's peak passive, improving nature-themed damage and control effectiveness. Exact modifiers are TBD.
- **Eternal Bloom** — Improves Healing Power and/or Regeneration duration. Exact scope and values are TBD.

---

### 12. Skill List Summary

Each Tier contains three Active Skills and two Passive Skills. The table below summarizes the skill count per class.

| Class | Active Skills | Passive Skills | Core Mechanic |
|---|---:|---:|---|
| Fighter | 15 | 10 | Weapon Techniques |
| Barbarian | 15* | 10 | Berserk State |
| Martial Artist | 15 | 10 | QTE Combo Flow |
| Rogue | 15 | 10 | Exploit |
| Ranger | 15 | 10 | Hunt Mark |
| Cleric | 15 | 10 | Divine Protection |
| Wizard | 15 | 10 | Elemental Specialization |
| Warlock | 15 | 10 | Curse Mastery |
| Druid | 15 | 10 | Nature's Control |

\* Barbarian's Berserk is included in the Active Skill count, but is a special core skill and is not upgraded with SP under the current design.

---

### 13. Design and Implementation Requirements

#### 13.1 Skill Data

Each skill should be represented by data rather than hard-coded class logic. A skill definition should minimally contain:

- `skillId`
- `displayName`
- `classId`
- `tier`
- `skillType` (`active` or `passive`)
- `description`
- `unlockLevel`
- `spUnlockCost`
- `rankMax` (normally 3)
- `rankUpgradeCosts`
- `mpCost` (Active Skills, if applicable)
- `targetType`
- `damageComponents` (if applicable)
- `scalingStat`
- `skillPower` / scaling coefficient
- `qteProfile` (if applicable): QTE type (single / sequence / none), marker count (maximum 6), combo gain per marker (Martial Artist), Perfect-triggered effect (maximum one per class tree), Poor consequence (maximum one per skill)
- `statusEffects` (if applicable)
- `duration` / `cooldown` / `usageLimit` (if applicable)
- `weaponRequirements` (if applicable)
- `balanceParameters`
- `icon` / visual and audio identifiers

Fields may be adjusted to match the project's actual implementation architecture.

#### 13.2 Runtime Validation

The combat system should validate:
- Character owns and has equipped the skill.
- Skill is in an available Active/Passive slot as applicable.
- Character meets the skill's Tier/level requirements.
- Character has sufficient MP, if required.
- Target is valid and within skill constraints.
- Required weapon is equipped.
- Skill-specific cooldown, usage limit, and status conditions are satisfied.

Invalid actions should not consume a turn or MP unless a skill/system rule explicitly says otherwise.

#### 13.3 Balance Parameters

The following are not globally finalized and must be defined per skill or in shared systems:

- Skill Power and stat scaling.
- MP Cost.
- Rank 1–3 progression.
- Target count, area, and range.
- QTE type and marker count (timing difficulty comes from enemy tier and dungeon, not from the skill).
- Status chance, duration, potency, and resistance interaction.
- Cooldown or usage limit, if used.
- Weapon requirements.
- SP cost and SP acquisition schedule.
- Boss restrictions for control, revive, and high-impact effects.

#### 13.4 Testing Considerations

- Verify each class can function without requiring one specific race.
- Verify active/passive slot limits.
- Verify Tier access at levels 1, 4, 7, 10, and 13.
- Verify SP unlock and Rank progression once costs are finalized.
- Verify skill weapon restrictions against strict class equipment compatibility.
- Verify QTE, hit chance, Critical, Break, status effects, healing, and Shield use shared system rules.
- Verify no passive or skill causes an unintended infinite trigger loop.
- Verify class-specific core mechanics reset or persist at the correct combat lifecycle boundaries.

---

### 14. Open Design Items

The following items remain open and should not be treated as finalized:

1. SP gained per level and from milestones/expedition events.
2. SP costs for unlocking and upgrading skills.
3. Whether every skill uses Rank 1–3 or some skills have special progression.
4. Exact skill damage coefficients, MP Costs, durations, and values.
5. Exact weapon requirements for individual skills.
6. Exact cooldown/usage-limit model, if any, for high-impact skills.
7. Barbarian Berserk's final bonus package and duration.
8. ~~Martial Artist maximum combo, combo gain by skill, Perfect QTE gain, and finisher scaling.~~ Resolved in v1.1 (§5.1, §5.2).
9. Rogue Exploit trigger definitions and simultaneous trigger behavior.
10. Ranger Hunt Mark bonus, duration, target switching, and refresh rules.
11. Cleric Resurrection / Guardian Angel / Miracle limits.
12. Wizard skill-specific element and damage details for any skills still described generically.
13. Warlock Curse interactions with target death, Broken state, and independent instances.
14. Druid Root/control behavior and boss restrictions.
15. Final skill names, descriptions, visual effects, and audio feedback where still draft.

---

### 15. Related Documents

- **Game Design Document** — Core game loop, party, progression, and dungeon structure.
- **Race System** — Race abilities and class-independent race rules.
- **Damage & Status Effect System** — Damage categories, formula, QTE, Critical, affinities, resistance, and status behavior.
- **Bab 12 (QTE System)** — Timing, grades, multi-hit, Shared QTE, Broken, telegraph, counters, Martial Artist combo, UI, and accessibility.
- **Inventory & Equipment System** — Equipment compatibility, stats, affixes, and persistence.
- **Shop System** — Hub facilities, gold, and run economy.

---

#### Revision Note v1.1

QTE sync (4 November 2026): shared QTE rules added to §2.2; counter trigger defined; Martial Artist Combo Flow values, skill combo gains, passives, and finisher formulas defined; ranged and Holy skill QTE profiles defined; `qteProfile` fields specified; open item 8 resolved.

## Bab 9 — Talents System

> Sumber: Talents System v0.2.

### 1. Overview

The Talent System provides characters with passive bonuses that support
different combat roles and build directions. Talents complement a
character's Class Skills and do not replace class identity.

The system is divided into: - **Shared Talents:** Available to
characters of any class. - **Class-Specific Talents:** Designed around
the identity and mechanics of a particular class.

Only effects explicitly defined in the current design discussion are
treated as specified. Values labeled **proposed** or **TBD** are not
final.

### 2. Talent Selection and Progression

Characters receive Talent selections at these level milestones:

    Character Level Talent Selection
  ----------------- ------------------
                  3 Select a Talent
                  6 Select a Talent
                  9 Select a Talent
                 12 Select a Talent
                 15 Select a Talent

#### 2.1 Talent Offer Structure

**Proposal, pending confirmation:** - 1 Shared Talent - 2 Class-Specific
Talents

#### 2.2 Talent Rules

-   Talent selections are intended to be unique for a character;
    already-selected Talents are not offered again.
-   Talent choices contribute to character build specialization.
-   Talent respec is available in the Hub and uses a special currency.
-   Respec currency cost and exact restrictions are **TBD**.

### 3. Shared Talents

#### 3.1 Alert

**Effect:** Reduces the Critical Chance of an enemy attacking the
character who owns Alert.

-   Proposed value: **−10 percentage points Critical Chance** for the
    attacking enemy against this character.
-   Applies to attacks targeting the Talent owner, not the entire party.
-   Final value is subject to balancing.

#### 3.2 Healer

**Effect:** Increases the amount of HP restored by the character.

-   Proposed value: **+15% healing**.
-   Applies to healing from both Skills and Items.
-   Final value is subject to balancing.

#### 3.3 Durable

**Effect:** Improves the character's ability to survive incoming damage.

-   Proposed values: **+10% Max HP** and **5% Damage Reduction**.
-   Damage Reduction is treated as an All Damage Taken −5% modifier and
    stacks multiplicatively; Defensive QTE is applied afterward.
-   Both effects are proposed as one Talent.
-   Final values are subject to balancing.

#### 3.4 Keen Mind

**Effect:** Focuses on MP efficiency.

-   Proposed value: **−10% MP Cost** for Skills.
-   Example: a Skill costing 10 MP costs 9 MP after the reduction.
-   MP-cost rounding and minimum-cost rules should be finalized during
    implementation and balancing.

#### 3.5 Arcane Reserve

**Effect:** Increases the character's Max MP.

-   Exact bonus value: **TBD**.

#### 3.6 Poisoner

**Effect:** Increases the effectiveness of Poison applied by the
character.

-   Exact potency, duration, or other Poison parameters affected:
    **TBD**.
-   Intended to improve Poison applied by its owner.

#### 3.7 Crusher

**Effect:** Increases the character's chance to inflict Stun on enemies.

-   Exact chance bonus: **TBD**.
-   Does not itself guarantee Stun.

#### 3.8 Slasher

**Effect:** Increases the character's chance to inflict Bleed on
enemies.

-   Exact chance bonus: **TBD**.
-   Does not itself guarantee Bleed.

#### 3.9 Piercer

**Effect:** When the character attacks a target, a portion of the
target's Physical Resistance is ignored.

-   Exact resistance reduction or penetration value: **TBD**.
-   Scope across attack types and whether it applies to every hit:
    **TBD**.

#### 3.10 Lucky

**Effect:** Gives the player a chance to retry a failed QTE.

-   Proposed activation chance: **20%** when a QTE fails.
-   The player must perform the retry; Lucky does not automatically
    convert the failed QTE into a success.
-   Maximum **1 activation per QTE**.
-   Maximum **3 activations per battle**.
-   The activation counter resets at the start of the next battle.
-   After reaching the battle limit, Lucky cannot activate again during
    that battle.
-   Lucky triggers only on a Poor caused by player input. Auto-Poor
    results (Stun, Sleep, Mind Control, Down) cannot be retried.
-   Retry flow: the animation pauses at the wind-up frame, a "Lucky!"
    banner shows for 400 ms, and the timing bar repeats with the same
    lead time and timing window. The retry result is final.
-   In a sequence QTE, only the failed marker is retried.
-   Damage, Break Power, and Martial Artist combo use the final result.
-   Shared QTE: if several targets have Lucky, the quota of the first
    character in party order is used.
-   Lucky is not a QTE window modifier and does not count toward the
    +25% window cap.
-   Remaining activations are shown as 3 pips under the portrait.
-   A QTE can be retried at most once from any source. If a character
    has both Lucky and the Halfling Racial Feat Second Chance, Lucky
    rolls first; Second Chance is offered only if Lucky does not
    trigger (Bab 12 §8.5).

#### 3.11 Inspiring Leader

**Effect:** Grants the party a temporary bonus to damage resistance
while the Talent owner remains alive.

-   Proposed bonus: **10% Damage Reduction**.
-   Damage Reduction is treated as an All Damage Taken −10% modifier and
    stacks multiplicatively; Defensive QTE is applied afterward.
-   Target: **the entire party, including the Talent owner**.
-   Duration: **3 turns**.
-   The effect ends immediately if the Talent owner dies, even if the
    duration has not elapsed.
-   Final value and turn-timing behavior are subject to balancing and
    battle-system definitions.

### 4. Additional Shared Talent Concepts

These names were previously proposed for the Shared Talent pool. Their
effects are not finalized in this document:

-   Vitality
-   Battle Rhythm
-   Iron Will
-   Efficient Casting
-   Quick Reflex --- Defensive QTE window +10% (QTE window modifier)
-   Fortune Seeker
-   Keen Eye
-   Steady Hand --- Offensive QTE window +10% (QTE window modifier)
-   Survivor's Instinct

As of v0.2, only Quick Reflex and Steady Hand have QTE effects. The
other names have no QTE interaction; consider renaming Battle Rhythm
when its effect is defined, because the name suggests a QTE effect.

Confirm their effects and values before treating these entries as locked
design.

### 5. Class-Specific Talent Pools

Class-Specific Talents are intended to reinforce each class's core
identity.

  -----------------------------------------------------------------------
  Class                               Core Identity
  ----------------------------------- -----------------------------------
  Fighter                             Weapon Techniques

  Barbarian                           Berserk State

  Martial Artist                      QTE Combo Flow

  Rogue                               Exploit

  Ranger                              Hunt Mark; the mark affects the
                                      Ranger themself

  Cleric                              Divine Protection; hybrid healing,
                                      protection, and Holy offense

  Wizard                              Elemental Specialization; no
                                      permanent element specialization

  Warlock                             Curse Mastery; Curse effects are
                                      independent and can coexist

  Druid                               Nature's Control; hybrid nature
                                      damage, control, and healing
  -----------------------------------------------------------------------

#### 5.1 Proposed Class-Specific Talent Names

These names were proposed in the design discussion. Detailed effects and
balancing values remain subject to review.

**Fighter** - Weapon Adept - Guard Specialist - Tactical Opening -
Weapon Precision - Master of Arms

**Barbarian** - Bloodlust - Unbreakable Rage - Berserk Fury - Raging
Momentum - Last Stand

**Martial Artist** - Flowing Strikes - Perfect Timing - Combo
Discipline - Finisher Mastery - Unbroken Rhythm

**Rogue** - Opportunist - Exploit Weakness - Dirty Fighting - Shadow
Step - Executioner

**Ranger** - Hunter's Instinct - Marked Prey - Deadly Precision -
Longshot - Predator's Patience

**Cleric** - Divine Grace - Sanctuary - Holy Retribution - Guardian's
Resolve - Last Benediction

**Wizard** - Elemental Adept - Focused Casting - Elemental Insight -
Arcane Efficiency - Elemental Mastery

**Warlock** - Lingering Malice - Curse Potency - Dark Knowledge - Soul
Harvester - Doom Weaver

**Druid** - Nature's Embrace - Wild Growth - Root Mastery - Nature's
Wrath - Primal Balance

#### 5.2 QTE-Related Class-Specific Talents (proposed values)

  -----------------------------------------------------------------------
  Talent                              Effect
  ----------------------------------- -----------------------------------
  Flowing Strikes (Martial Artist)    Combo starts at 2 at the start of
                                      each battle.

  Perfect Timing (Martial Artist)     Offensive and Defensive QTE window
                                      +10%.

  Combo Discipline (Martial Artist)   Reset from not attacking happens
                                      after 2 turns instead of 1.

  Finisher Mastery (Martial Artist)   Finisher coefficient per combo
                                      point +0.03.

  Unbroken Rhythm (Martial Artist)    Once per battle, combo is not
                                      reset when becoming Broken.

  Guard Specialist (Fighter)          Defensive QTE window +10%.

  Deadly Precision (Ranger)           Offensive QTE window +10% for
                                      ranged attacks.
  -----------------------------------------------------------------------

All QTE window effects from talents, passives, and equipment stack
additively up to a shared cap of +25%. Talents may affect QTE only through
window modifiers or retry; no talent changes damage reduction values or
grants an automatic grade.

### 6. Open Design Decisions

1.  Confirm whether each Talent milestone offers **1 Shared + 2
    Class-Specific Talents**.
2.  Set final numeric values for Alert, Healer, Durable, Keen Mind,
    Lucky, and Inspiring Leader.
3.  Define Arcane Reserve's Max MP bonus.
4.  Define which Poison properties Poisoner improves.
5.  Define Crusher's Stun chance bonus and Slasher's Bleed chance bonus.
6.  Define Piercer's Physical Resistance penetration value and attack
    scope.
7.  Confirm whether the other proposed Shared Talent names and effects
    belong to the final pool.
8.  Define complete effects and numeric values for all Class-Specific
    Talents.
9.  Define respec currency cost and restrictions.
10. Specify turn timing and refresh behavior for Inspiring Leader.
11. Lucky's retry behavior and the QTE-related talents in §4 and §5.2
    are specified as of v0.2; final numeric balancing remains open.

------------------------------------------------------------------------

### Change Log

  -----------------------------------------------------------------------
  Version                             Notes
  ----------------------------------- -----------------------------------
  0.1                                 Initial Talent System draft
                                      assembled from current design
                                      discussion. Includes Lucky's battle
                                      activation cap and Inspiring
                                      Leader's duration and life
                                      condition.

  0.2                                 QTE sync: Lucky retry flow,
                                      Damage Reduction stacking, QTE
                                      window talents, Martial Artist,
                                      Fighter, and Ranger QTE talent
                                      effects.

  -----------------------------------------------------------------------

# Bagian IV — Combat

## Bab 10 — Combat Structure dan Broken

> Sumber: GDD Visual Direction v0.2 §5.1 dan §5.3. Penomoran §5.x dipertahankan agar rujukan lama tetap berlaku.

### 5. Combat

Ringkasan struktur combat dan definisi kerja Broken. Aturan QTE lengkap ada di Bab 12, dan damage serta status di Bab 11.

#### 5.1 Combat Structure

- Combat berbasis giliran dengan aksi per karakter.

- Pemain memilih aksi/skill dan target; skill kemudian diselesaikan melalui aturan hit, damage, healing, status, dan QTE yang relevan.

- Enemy turn order, initiative formula, action economy, dan defend/item rules masih TBD.

- Aksi musuh berbahaya (Heavy, signature boss, charge, status-only) ditelegraph sejak awal round. Charge dibatalkan jika musuh menjadi Broken; counter pemain hanya terpicu oleh Defensive Perfect. Detail: Bab 12 §6.4–6.5.

#### 5.3 Broken

Broken digunakan oleh beberapa class mechanic (contoh: reset combo Martial Artist). Definisi kerja (asumsi A5 di Bab 12 (QTE System), menunggu konfirmasi): setiap karakter dan musuh memiliki Break gauge (100; boss 200) yang diisi Break Power dari hit lawan dan berkurang 20 di awal giliran pemiliknya. Saat penuh, target menjadi Broken sampai akhir giliran berikutnya miliknya, lalu gauge kembali ke 0. Selama Broken, karakter pemain tidak bisa meraih Perfect pada Defensive QTE, dan musuh yang sedang charge membatalkan serangannya.

## Bab 11 — Damage dan Status Effect

> Sumber: Consolidated Damage & Status Effect GDD v1.1.

### 1. Damage Types dan Elemen

|**Kategori**|**Aturan**|
|---|---|
|Physical Damage|Damage fisik, serangan senjata, dan skill fisik.|
|Elemental Damage|Kategori damage elemental; menggantikan Magical Damage<br>sebagai kategori damage utama.|
|Almighty Damage|Damage absolut yang mengabaikan defense dan<br>affinity/resistance sesuai aturan khusus di bawah.|

#### 1.1 Elemen

Fire, Ice, Lightning, Earth, Wind, Water, Holy, dan Shadow.

Nature bukan elemen damage. Nature adalah tema/identitas kemampuan Druid; tidak ada Nature Damage atau Nature Resistance.

Tidak ada kategori Magical Damage terpisah. Istilah magic dapat menjadi deskripsi skill atau scaling, tetapi damage category yang digunakan adalah Elemental Damage.

### 2. Damage Formula dan QTE

#### 2.1 Base Damage

|**Damage Type**|**Base Formula**|
|---|---|
|Physical|SkillPower + (STR × Scaling)|
|Elemental|SkillPower + (INT × Scaling)|
|Almighty|SkillPower + (INT × Scaling)|

Nilai Scaling belum ditetapkan secara universal dan akan diatur per skill.

#### 2.2 QTE

|**QTE Grade**|**Multiplier**|
|---|---|
|Perfect|1.5×|
|Good|1.0×|
|Poor|0.5×|

- QTE memengaruhi Physical, Elemental, dan Almighty direct damage.

- QTE adalah kualitas eksekusi serangan dan terpisah dari Critical Hit.

- QTE tidak memengaruhi Damage Over Time sama sekali.

- QTE juga dapat diterapkan pada seluruh komponen damage dari skill multi-type.

#### 2.3 Defensive QTE

|**QTE Grade**|**Damage Reduction**|
|---|---|
|Perfect|100% (serangan Heavy: 50%)|
|Good|50%|
|Poor|0%|

- Defensive QTE dipicu oleh setiap aksi musuh yang memiliki minimal satu component direct damage. Aksi musuh yang hanya menerapkan status tidak memicu QTE.

- Setiap hit memiliki QTE sendiri. Hit yang mengenai lebih dari satu target memakai satu Shared QTE dengan hasil yang sama untuk semua target yang bisa bereaksi.

- Defensive QTE diterapkan setelah semua modifier (Damage Bonus, Critical, Defense/Resistance, Damage Taken, affinity) dan sebelum rounding. Shield menyerap sisa damage setelahnya.

D_akhir = floor(D × ∏(1 − DR_i) × (1 − R_QTE))

- Defensive Perfect tidak menghalangi Status Effect.

- Kondisi yang memaksa hasil Poor: Stun, Sleep, Mind Control, dan karakter Down. Broken menutup grade Perfect (hasil terbaik Good). Freeze menyempitkan timing window 15%. Batas bawah window Perfect adalah 66 ms.

- Aksi yang tidak dipilih pemain (Mind Control, Madness, salah aksi akibat Confusion) diselesaikan tanpa Offensive QTE dengan grade Good (1.0×).

#### 2.4 Cakupan Offensive QTE

- Offensive QTE hanya dipakai Basic Attack dan skill yang memiliki direct damage. Heal, buff, debuff, dan control tanpa damage tidak memakai QTE.

- Grade QTE tidak memengaruhi chance Status Effect.

- Multi-hit memakai satu QTE sekuens: satu marker per hit, maksimal 6 marker (qteProfile skill boleh menetapkan jumlah marker sendiri). AoE memakai satu QTE per hit dengan grade yang sama untuk semua target.

#### 2.5 QTE dan Break Power

- Break Power dari serangan pemain dikalikan multiplier grade Offensive QTE (1.5× / 1.0× / 0.5×).

- Break Power yang diterima karakter pemain dikalikan (1 − R_QTE) dari Defensive QTE.

- Definisi kerja Break gauge dan Broken ada di Bab 10 §5.3. Detail lengkap seluruh aturan QTE: Bab 12 (QTE System).

### 3. Critical Hit

|**Parameter**|**Aturan**|
|---|---|
|Damage Type yang bisa Critical|Physical Damage saja.|
|Base Critical Chance|5%.|
|Base Critical Damage|150% (1.5×).|
|Critical Chance Cap|95%.|
|Critical Damage Cap|300% (3×).|
|Roll|Independen per hit.|
|QTE vs Critical|Mekanik terpisah; Critical roll per hit.|

CRIT_final = min(CRIT_base + CRIT_bonus, 95%)

CRITDMG_final = min(CRITDMG_base + CRITDMG_bonus, 300%)

- Critical multiplier diterapkan pada Physical Damage setelah QTE dan sebelum Physical Defense.

- Critical tidak dapat melewati Physical Nullification.

- Elemental, Almighty, dan DoT tidak otomatis melakukan Critical Hit.

- Skill finisher (Dragon's Fury, Final Technique) tidak dapat melakukan Critical Hit.

### 4. Enemy Affinity dan Null Break

|**Affinity**|**Multiplier**|
|---|---|
|Weakness|1.5×|
|Normal|1.0×|
|Resistant|0.5×|
|Nullification|0 damage|

- Enemy affinity berlaku pada damage component yang ditargetkannya.

- Nullification membatalkan komponen terkait; tidak dapat dikurangi/ditingkatkan oleh damage bonuses atau reductions.

- Pada multi-type skill, nullification hanya membatalkan komponen yang terkena; komponen lain tetap dihitung.

- Null Break menonaktifkan Nullification yang ditargetkan dan membuat affinity tersebut berperilaku sebagai Normal selama efek aktif.

- Null Break tidak mengubah Weakness atau Resistant dan tidak mengurangi affinity selain Nullification.

- Hanya satu Null Break aktif per enemy; tidak dapat stack, refresh, atau extend. Enemy berbeda dapat memiliki Null Break masing-masing.

- Durasi berdasarkan skill level: Lv1 1 turn, Lv2 2 turns, Lv3 3 turns.

### 5. Player Elemental Resistance

Player characters tidak memakai enemy-style Weakness/Resistant/Nullification affinity untuk elemen. Mereka menggunakan Elemental Resistance dari equipment, stats, buffs, dan sumber terkait.

R_final = min(R_equipment + R_stats + R_buffs + R_other, 75%)

D_received = D_incoming × (1 − R_final)

- Resistance stacking bersifat additive.

- Cap Elemental Resistance adalah 75% per elemen.

- Setiap elemental damage component menggunakan resistance elemen masing-masing.

- Physical dan Almighty mengabaikan Elemental Resistance.

### 6. Damage Modifier Stacking

Damage Bonus dan Damage Taken modifier menggunakan Multiplicative Stacking. Contoh: +20% = 1.2×; +50% = 1.5×; −20% = 0.8×.

|**Modifier Scope**|**Berlaku untuk**|
|---|---|
|Physical Damage Bonus|Physical Damage|
|Elemental Damage Bonus|Semua Elemental Damage|
|Element-Specific Damage Bonus|Hanya elemen yang cocok: Fire, Ice, Lightning, Earth, Wind,<br>Water, Holy, Shadow|
|All Damage Bonus|Physical, Elemental, dan Almighty|
|Almighty Damage Bonus|Tidak ada modifier khusus; Almighty tetap mendapat All Damage<br>Bonus|
|Physical Damage Taken|Physical; diterapkan setelah Physical Defense|
|Elemental Damage Taken|Semua Elemental Damage; diterapkan sebelum Elemental<br>Resistance|
|Element-Specific Damage Taken|Elemen yangcocok|
|All Damage Taken|Physical, Elemental, dan Almighty; Almighty tidak memiliki<br>modifier Taken khusus|

- Damage Reduction dari talent (misalnya Durable dan Inspiring Leader) diperlakukan sebagai All Damage Taken −x% dan ditumpuk secara multiplicative. Defensive QTE diterapkan sesudahnya.

Nullification adalah pengecualian: jika komponen dinullifikasi, hasilnya 0 dan modifier damage tidak mengubah hasil tersebut.

### 7. Multi-Type dan Multi-Hit

- Skill dapat memiliki beberapa damage component, misalnya Physical + Fire atau dua elemen.

- Setiap component menggunakan formula, affinity, defense/resistance, dan modifier yang sesuai.

- Nullification hanya membatalkan component yang ditargetkan.

- QTE berlaku pada semua component direct damage.

- Critical hanya berlaku pada component Physical.

- Setiap hit adalah damage event tersendiri; Critical roll dilakukan per hit.

- Untuk satu hit dengan beberapa component, pertahankan nilai desimal tiap component, jumlahkan component, lalu lakukan rounding pada total hit.

- Rounding menggunakan Floor, satu kali per hit di akhir perhitungan (dikonfirmasi di Bab 12 §5.2). Hasil 0 diperbolehkan.

- Jika sebuah skill secara eksplisit mengaplikasikan status per hit, setiap hit dapat menghasilkan status instance tersendiri.

- Serangan musuh yang menerapkan status per hit dibatasi maksimal 3 hit; serangan musuh secara umum maksimal 6 hit.

- Koefisien damage per target untuk serangan dan skill multi-target maksimal 60% dari serangan single-target setara.

- Base damage per hit serangan musuh minimal 5, agar rounding Floor tidak menghapus hit kecil.

#### 7.1 Calculation Order — Physical

1. Hitung Base Physical Damage: SkillPower + (STR × Scaling).

2. Terapkan Physical Damage Bonus dan All Damage Bonus (multiplicative).

3. Terapkan QTE multiplier.

4. Roll Critical per hit; jika berhasil, terapkan Critical Damage multiplier.

5. Kurangi Physical Defense secara flat. Damage dapat mencapai 0; tidak ada minimum damage 1.

6. Terapkan Physical Damage Taken modifier.

7. Terapkan Physical Affinity (Weakness/Normal/Resistant). Nullification membatalkan component, kecuali Null Break menonaktifkannya.

8. Floor hasil akhir per hit.

#### 7.2 Calculation Order — Elemental

9. Hitung Base Elemental Damage: SkillPower + (INT × Scaling).

10. Terapkan Elemental Damage Bonus, element-specific bonus, dan All Damage Bonus secara multiplicative.

11. Terapkan QTE multiplier.

12. Terapkan Elemental Damage Taken dan modifier Taken terkait (sebelum Elemental Resistance).

13. Terapkan Enemy Elemental Affinity.

14. Terapkan target Elemental Resistance untuk elemen tersebut.

15. Nullification membatalkan component jika aktif; Null Break dapat meniadakan Nullification yang ditargetkan.

16. Untuk multi-component hit, jumlahkan nilai component lalu rounding total hit.

#### 7.3 Calculation Order — Almighty

17. Hitung Base Almighty Damage: SkillPower + (INT × Scaling).

18. Terapkan All Damage Bonus.

19. Terapkan QTE multiplier.

20. Terapkan All Damage Taken.

21. Almighty mengabaikan Defense, enemy affinity, dan Elemental Resistance; tidak melakukan Critical Hit.

22. Round hasil akhir per hit.

### 8. Status Effect: Prinsip Umum

- Status Effect ditentukan oleh skill; elemen damage tidak otomatis menentukan status.

- Skill dapat memberikan Status Effect tanpa Elemental Damage dan dapat menerapkan lebih dari satu status jika didesain demikian.

- Durasi ditentukan oleh Level Skill; contoh level-to-duration yang pernah dibahas adalah Lv1 1 turn, Lv2 2 turns, Lv3 3 turns, tetapi durasi universal semua skill belum dikonfirmasi.

- Status instances independen: instance baru tidak refresh/extend instance lama.

- Tidak ada batas jumlah instance aktif untuk status yang sama.

- Status Effect berbeda dapat coexist pada target.

- Jika skill menerapkan status per hit, tiap hit melakukan roll independen dan hit sukses dapat menambah instance.

- Status Effect Resistance mengurangi chance aplikasi, bukan duration atau potency setelah aplikasi.

- Roll status independen dari grade QTE, termasuk saat damage component bernilai 0 karena Defense atau Nullification.

### 9. Status Effect Resistance

|**Parameter**|**Aturan**|
|---|---|
|Type|Universal Status Effect Resistance; tidak terpisah per status.|
|Sumber|Base Stats, Equipment, Buffs, Passives.|
|StackingResistance|Additive.|
|Cap|75%.|
|Minimum|0%.|
|Resistance Reduction|Percentage Reduction.|
|Multiple reductions|Multiplicative Stacking.|
|Boss|Resistance tinggi untuk non-Mental status; dapat direduksi.|

R_final = min(R_base + R_equipment + R_stats + R_buffs + R_passives, 75%) P_final = P_base × (1 − R_final) R_after = max(0, R_initial × ∏(1 − D_i))

Contoh: Status Effect Chance 80%, resistance 75% → chance final 20%. Resistance 60% dengan dua debuff 20% → 38.4% resistance; chance skill 40% → 24.64%.

### 10. Damage Over Time

- DoT tick pada awal turn target.

- DoT menggunakan Dynamic Damage: stat dan modifier dihitung ulang pada setiap tick.

- Stat scaling ditentukan oleh skill sumber; tidak ada stat universal DoT.

- DoT menerima Damage Bonus yang relevan dengan Damage Type/element sumber, termasuk Elemental Damage Bonus, element-specific bonus, dan All Damage Bonus jika applicable.

- Damage Taken modifier yang sesuai diterapkan sesuai damage type/element target.

- QTE tidak memengaruhi DoT sama sekali.

- DoT tidak otomatis Critical.

- Status Effect Resistance tidak mengurangi tick damage setelah aplikasi berhasil.

|**DoT**|**Damage Type / Stat**|
|---|---|
|Burn|Elemental Fire Damage; stat scaling ditentukan skill.|
|Poison|Damage Type dan stat scaling ditentukan skill.|
|Bleed|Physical Damage; stat scaling ditentukan skill.|

### 11. Status Effect Non-Mental

|**Status**|**Mekanik yang diputuskan**|
|---|---|
|Burn|DoT Fire; awal turn target; dynamic.|
|Poison|DoT; damage type/stat sesuai skill; awal turn target; dynamic.|
|Bleed|DoT Physical; awal turn target; dynamic.|
|Freeze|Mengurangi Speed target, sehingga turn menjadi lebih lambat;<br>bukan otomatis kehilangan Action.|
|Shock|Target memiliki peluang gagal melakukan Action; tidak otomatis<br>memberikan LightningDamage.|
|Curse|Debuff; efek spesifik ditentukan oleh skill sumber.|
|Stun|Mencegah target melakukan Action selama efek aktif.|

Detail numerik dan perilaku spesifik status-status di atas masih perlu ditentukan pada desain skill.

### 12. Mental Effect System

Mental Effects adalah kategori Status Effect yang memengaruhi pikiran, persepsi, emosi, atau kontrol target. Mental Effect bukan Damage Type/elemen baru dan menggunakan Universal Status Effect Resistance.

|**Mental Effect**|**Mekanik yang disepakati**|
|---|---|
|Mind Control|Target kehilangan kontrol; pihak pengendali menentukan Action<br>target.|
|Confusion|Target masih bertindak,tetapi berpeluangmelakukan Action/target yangsalah.|
|Fear|Model B: Combat Debuff. Target tetap bertindak, tetapi Damage<br>Dealt berkurang.|
|Despair|Model A: Action Disruption. Target memiliki peluang gagal<br>melakukan Action.|
|Charm|Target tidak dapat menyerang sumber Charm, tetapi dapat<br>melakukan Action lain yangvalid.|
|Sleep|Target tidak dapat melakukan Action. Sleep berakhir saat target<br>menerima direct damage atau DoT; damage pemicu tetap<br>diterima.|
|Silence|Target tidak dapat menggunakan Skill; Basic Attack dan Item<br>tetap dapat digunakan.|
||AI memilih Action dan target secara acak. Action dapat ofensif|
|Madness|atau defensif (Basic Attack, Skill, Heal, Buff, Guard, dll. jika<br>valid); target dapat menyerangparty sendiri.|

#### 12.1 Madness Random Action

- Pemilihan Action dilakukan secara acak, bukan AI optimal.

- Pemilihan target juga acak dari target yang valid.

- Action ofensif dan defensif dapat dipilih jika persyaratannya terpenuhi.

- Action invalid tidak masuk ke pilihan.

### 13. Boss Rules

|**Status Category**|**Aturan Boss**|
|---|---|
|Non-Mental Status Effect|Boss dapat terkena; memiliki Universal Status Effect Resistance<br>tinggi.|
|Duration|Tetap mengikuti durasi skill; tidak ada durasi khusus Boss yang<br>lebih pendek.|
|Resistance Reduction|Dapat dikurangi dengan Percentage Reduction dan multiplicative<br>stacking.|
|Mental Effects|Boss immune terhadap seluruh Mental Effects.|
|Immunity bypass|Status Effect Resistance Reduction tidak menembus Mental<br>Effect immunity.|

### 14. Open Questions

- Nilai Skill Scaling dan coefficients tiap skill.

- Exact timing status expiry/cleansing jika terjadi pada timing tick/turn yang sama.

- Nilai dasar dan scaling Burn, Poison, Bleed, serta sumber stat setiap skill.

- Freeze Speed reduction; Shock/Despair fail chance; Fear damage reduction; Confusion mis-action probability.

- Daftar Action dan pemilihan target Mind Control; target scope dan AI details.

- Aturan apakah multiple instance dengan efek chance (Shock/Despair) roll independen ketika aktif—perlu spesifikasi.

- Detail cleanse oleh Cleric/Druid dan item: cakupan status, jumlah instance yang dihapus, dan biaya/targeting.

- Exact status-specific exceptions, boss encounters, dan skill-level duration tables.

### 15. Revision Note v1.1

Sinkronisasi dengan Bab 12 (QTE System) (4 November 2026): grade Miss diganti Poor; Defensive QTE, cakupan Offensive QTE, dan interaksi Break Power ditambahkan (§2.3–2.5); finisher tidak bisa Critical (§3); Damage Reduction talent sebagai All Damage Taken (§6); rounding Floor dikonfirmasi dan batasan desain multi-hit/multi-target ditambahkan (§7); roll status independen dari QTE (§8). Sleep dan Mind Control ditetapkan sebagai kondisi auto-Poor.

## Bab 12 — QTE System

> Sumber: Master Checklist QTE dan Spesifikasi Fase 2 (Claude Doc). Item disusun berdasarkan nomor; template tiap item: tujuan, aturan, interaksi, kurva kesulitan, risiko, pengujian.

### Keputusan Dasar

Keputusan berikut berasal dari Master Checklist QTE yang sudah disetujui dan menjadi dasar seluruh bab ini.

| Aspek | Keputusan |
| --- | --- |
| Peran | QTE reaction-based menentukan kualitas eksekusi aksi dalam combat turn-based |
| Grade | Perfect / Good / Poor ("Miss" diganti Poor agar tidak bentrok dengan miss dari Hit Chance) |
| Offensive QTE | Perfect 1.5×, Good 1.0×, Poor 0.5× direct damage (Physical, Elemental, Almighty) dan Break Power; terpisah dari Hit Chance dan Critical; tidak memengaruhi DoT |
| Cakupan ofensif | Hanya Basic Attack dan skill dengan direct damage; heal, buff, debuff, dan control tanpa damage tidak memakai QTE |
| Defensive QTE | Dipicu setiap aksi musuh dengan direct damage; Perfect 100% reduksi (Heavy 50%), Good 50%, Poor 0%; Perfect tidak menghalangi status |
| Multi-hit | Defensif: satu QTE per hit. Ofensif: satu QTE sekuens per rangkaian |
| AoE | Satu Shared QTE per hit, hasil seragam untuk semua target yang bisa bereaksi |
| Urutan kalkulasi | Defensive QTE diterapkan setelah semua modifier dan sebelum rounding; Shield menyerap sisanya |
| Martial Artist | Combo Flow berbasis QTE: Poor reset, Good gain normal, Perfect gain +1; reset saat satu giliran tanpa menyerang atau Broken; finisher mengonsumsi combo |
| Lucky | 20% retry saat Poor; maksimal 1 per QTE dan 3 per battle |
| Accessibility | Auto-Good tersedia; timing cue tidak hanya bergantung pada animasi |

### Asumsi eksplisit

Empat asumsi di bawah ini belum dikonfirmasi. Kalau ada yang salah, item yang bergantung padanya harus direvisi.

| Kode | Asumsi | Item yang bergantung |
| --- | --- | --- |
| A1 | Target platform PC, dengan dukungan keyboard, mouse, dan controller | 1.1, 1.2 |
| A2 | Engine belum ditentukan; spesifikasi dibuat engine-agnostic dengan timing dalam milidetik | 1.2, 1.3 |
| A3 | Game berjalan pada 60 fps (1 frame ≈ 16.7 ms) | 1.2 |
| A4 | Penilaian QTE memakai timestamp input event, bukan polling per frame | 1.2 |
| A5 | Broken: setiap karakter dan musuh punya Break gauge yang diisi oleh Break Power dari hit lawan. Saat penuh, target menjadi Broken sampai akhir giliran berikutnya miliknya, lalu gauge kembali ke 0 | 3.1, 6.5 |

Urutan pengerjaan mengikuti dependensi: Bagian 1 dan 5 (fondasi), lalu 3 dan 2, kemudian 4, 6, 7, 8, 9, dan 10. Fase 3 dijalankan setelah semua bagian selesai.

Semua angka bertanda *tuning* adalah nilai awal playtest. Angkanya konkret dan bisa langsung diimplementasikan, tetapi diharapkan berubah setelah playtest.

### 1. QTE Fundamentals

#### 1.1 Bentuk indikator final

**Keputusan: timing bar horizontal, bukan shrinking ring.** Bar bisa menampung beberapa marker sekaligus, sehingga QTE sekuens ofensif dan multi-hit defensif muat dalam satu indikator. Ring tidak bisa melakukannya tanpa menumpuk lingkaran.

**1. Tujuan desain.** Pemain harus bisa membaca kapan harus menekan dan seberapa sempit window-nya dalam satu lirikan. Hal ini berlaku untuk semua jenis QTE: tunggal, sekuens, Shared, dan Heavy.

**2. Aturan/mekanik.**

- Ukuran bar 25% lebar layar (480 px pada 1920×1080), diletakkan di tengah, tepat di atas HUD party.
- Cursor bergerak dengan kecepatan konstan dari tepi kiri menuju **hit point** di 75% panjang bar, lalu terus berjalan sampai ujung kanan.
- Kecepatan cursor = 360 px ÷ T\_lead (lihat 1.3). Pada T\_lead 1000 ms, kecepatannya 0.36 px/ms.
- Lebar zona di layar = lebar window × kecepatan cursor. Pada normal: Perfect 90 ms = 32 px dan Good 240 ms = 86 px (angka final per tier di 6.1), keduanya berpusat di hit point.
- Zona Perfect ditandai dengan bentuk (kurung siku di kedua sisi), bukan hanya warna, agar color-safe.
- QTE sekuens dan multi-hit defensif menampilkan beberapa marker pada satu bar. Hanya marker terdepan yang aktif; marker berikutnya aktif setelah marker sebelumnya dinilai.
- Serangan Heavy/Unblockable: bar diberi border tebal, dan zona Perfect diisi pola arsir setengah sebagai tanda bahwa reduksinya maksimal 50%.
- Header bar: QTE ofensif menampilkan nama skill. QTE defensif menampilkan potret karakter target; Shared QTE menampilkan semua potret target.
- Cue audio berupa ritme tiga ketukan: dua tick pada hit point − 2I dan hit point − I, dengan I = T\_lead ÷ 3. Ketukan ketiga adalah saat menekan dan tidak berbunyi. Dengan begitu, pemain bisa menekan tepat waktu hanya dari audio.

**3. Interaksi dengan sistem lain.** Turn-based: bar hanya muncul saat resolusi aksi, tidak pernah saat pemilihan menu. Animasi: frame impact disinkronkan ke hit point, tetapi jam QTE yang menjadi acuan; animasi mengikuti jam QTE, bukan sebaliknya (sesuai prinsip pemisahan logic dan animasi di GDD Visual Direction). Survival: bar defensif adalah alat utama pemain untuk menjaga HP, jadi keterbacaannya menentukan keadilan tekanan resource.

**4. Kurva kesulitan dan sumber tantangan.** Tantangan datang dari dua variabel yang terlihat jelas: kecepatan cursor (T\_lead) dan lebar zona. Pemain selalu bisa melihat seberapa sulit sebuah QTE sebelum menekan, sehingga kegagalan terasa sebagai kesalahan eksekusi, bukan jebakan.

**5. Risiko.**

- Bar di HUD menarik mata dari animasi karakter. Diterima: animasi tetap ada sebagai flavor, tetapi tidak membawa informasi timing.
- Pada layar kecil (misalnya 1280×800), zona Perfect bisa kurang dari 25 px. Mitigasi: lebar bar tetap 25% layar, dengan minimum 400 px.
- Desync audio-video di sebagian perangkat membuat ritme tick menyesatkan. Mitigasi: offset kalibrasi (Bagian 10).

**6. Cara pengujian.**

- Uji pemahaman: 8 tester baru menjalankan 3 QTE tanpa teks tutorial. Target: minimal 7 dari 8 bisa menunjuk zona Perfect dengan benar.
- Uji distribusi: tiap tester menjalankan 200 QTE tier normal. Target untuk pemain rata-rata: Perfect 40–55%, Good 30–45%, Poor ≤ 15% (selaras dengan 6.1).
- Uji audio-only: layar bar disembunyikan selama 20 QTE. Target: rasio Good-atau-lebih-baik minimal 60%.

#### 1.2 Jenis input

**Keputusan: satu tombol aksi untuk semua QTE, dengan penekanan prematur dihitung Poor.** Tantangannya adalah timing, bukan menghafal tombol. Aturan prematur mencegah strategi menekan berulang-ulang (mashing).

**1. Tujuan desain.** Setiap penilaian QTE harus mencerminkan satu keputusan timing yang disengaja, dengan hasil yang konsisten di keyboard, mouse, maupun controller.

**2. Aturan/mekanik.**

- Tombol default (bisa di-rebind): Space di keyboard, klik kiri di mouse (di mana saja pada layar), dan tombol bawah (A/Cross) di controller.
- Penekanan bersifat edge-triggered: tombol harus dilepas lalu ditekan lagi. Input yang dipakai untuk mengonfirmasi menu tidak pernah terbawa ke QTE.
- 150 ms pertama setelah bar muncul adalah grace period: penekanan diabaikan, tidak dihukum.
- Penilaian memakai Δ = |t\_tekan − t\_hit|, dengan t\_tekan sudah dikoreksi offset kalibrasi (default 0 ms, diatur di Bagian 10):
  - Δ ≤ W\_P ÷ 2 → Perfect
  - Δ ≤ W\_G ÷ 2 → Good
  - Selain itu → Poor
- Penekanan setelah grace period tetapi sebelum window Good terbuka langsung dihitung Poor (prematur).
- Tidak menekan sampai window Good tertutup juga dihitung Poor.
- Satu marker hanya menerima satu input. Input selama tampilan hasil diabaikan.
- **Batas bawah window Perfect: 66 ms (4 frame pada 60 fps)**, berlaku di semua kondisi termasuk boss dan Freeze. Batas ini menjamin window tetap valid walaupun engine hanya bisa membaca input per frame.
- Retry dari Lucky memakai tombol dan aturan yang sama.

**3. Interaksi dengan sistem lain.** Turn-based: tombol yang sama dipakai untuk konfirmasi menu, sehingga aturan edge-trigger dan grace period wajib ada. QTE sekuens: tiap marker dinilai terpisah dengan aturan yang sama. Freeze dan tier musuh (Bagian 3 dan 6) boleh menyempitkan window, tetapi tidak pernah di bawah 66 ms.

**4. Kurva kesulitan dan sumber tantangan.** Input tidak menjadi sumber kesulitan. Kesulitan datang sepenuhnya dari lebar window dan kecepatan cursor. Hukuman prematur menambah tantangan secara adil: pemain yang panik dihukum, pemain yang tenang tidak.

**5. Risiko.**

- Controller Bluetooth punya latensi 30–60 ms, yang bisa menghabiskan separuh window Perfect. Mitigasi: kalibrasi offset di Bagian 10 wajib tersedia sebelum playtest pertama.
- Jika engine tidak menyediakan timestamp input (asumsi A4 gagal), resolusi penilaian turun ke 16.7 ms. Batas 66 ms sudah memperhitungkan kondisi ini.
- Klik mouse di mana saja bisa terpicu saat pemain ingin mengklik UI lain. Risiko kecil, karena UI lain dinonaktifkan selama QTE berjalan.

**6. Cara pengujian.**

- Uji latensi: rekam layar dengan kamera 240 fps untuk tiap jenis perangkat, lalu ukur selisih rata-rata antara tekanan fisik dan timestamp. Target: selisih setelah kalibrasi ≤ 10 ms.
- Uji misfire: 300 aksi berturut-turut dari menu ke QTE. Target: 0 input menu yang terbawa ke QTE.
- Uji mashing: tester diminta menekan berulang-ulang secara sengaja. Target: rasio Poor ≥ 90%.

#### 1.3 Durasi satu sesi QTE

**Keputusan: QTE tunggal tier normal berlangsung sekitar 1.6 detik, dan satu bar maksimal memuat 6 marker.** Target waktu QTE per round adalah ≤ 12 detik (median) agar combat tidak terasa seperti rhythm game yang melelahkan.

**1. Tujuan desain.** Durasi harus cukup panjang untuk dibaca dan diantisipasi, tetapi cukup pendek agar 8 dungeon penuh QTE tidak menimbulkan fatigue.

**2. Aturan/mekanik.**

| Fase | Durasi (*tuning*) | Keterangan |
| --- | --- | --- |
| Travel (T\_lead) | 1000 ms di tier normal | Dari bar muncul sampai hit point; 150 ms pertama adalah grace period (1.2). Nilai per tier ditetapkan di Bagian 6, rentang 640–1000 ms tanpa assist (6.1); pengaturan kecepatan (10.4) bisa memperpanjangnya hingga 1500 ms |
| Tail | W\_G ÷ 2 + 150 ms | Sisa window Good ditambah jeda sebelum hasil |
| Tampilan hasil | 350 ms | Berjalan bersamaan dengan damage number |
| Interval antar marker (I\_m) | 350 ms default | Minimum W\_G + 100 ms, agar window tidak pernah tumpang tindih |

- Durasi QTE tunggal = T\_lead + tail + hasil ≈ 1000 + 270 + 350 ≈ 1.6 s.
- Durasi bar n marker = T\_lead + (n − 1) × I\_m + tail + hasil. Untuk 6 marker: ≈ 3.4 s.
- **Maksimal 6 marker per bar.**
  - Ofensif: skill dengan lebih dari 6 hit membagi hit-nya merata ke 6 marker (atau ke jumlah marker yang ditetapkan qteProfile skill, 2.2), dan grade tiap marker berlaku untuk hit-hit di dalamnya.
  - Defensif: karena tiap hit wajib punya QTE sendiri, **serangan musuh dibatasi maksimal 6 hit** (aturan desain musuh).
- Multi-hit defensif ditampilkan sebagai marker pada satu bar, bukan bar terpisah per hit. Tiap hit tetap dinilai sendiri.

**3. Interaksi dengan sistem lain.** Turn-based: perkiraan untuk round normal (4 aksi party dengan 3 skill damage, 3 aksi musuh) adalah 6 bar × 1.6 s ≈ 10 s waktu QTE. Bagian 6 memakai rentang T\_lead di atas untuk membedakan tier. Accessibility (Bagian 10): pengaturan kecepatan QTE harus memanjangkan T\_lead tanpa mengubah lebar window, agar efeknya terhadap balance bisa diukur.

**4. Kurva kesulitan dan sumber tantangan.** T\_lead yang lebih pendek mengurangi waktu antisipasi. Interval antar marker yang tidak teratur (Bagian 6) menguji ritme. Durasi total bukan sumber kesulitan; panjangnya tidak dipakai untuk menghukum pemain.

**5. Risiko.**

- Fatigue pada pertarungan boss panjang (15+ round). Mitigasi: QTE hanya untuk aksi direct damage, dan batas 6 hit.
- Batas 6 hit membatasi desain musuh tipe "serangan beruntun". Diterima; variasinya dipindahkan ke pola interval.
- Pemain bisa merasa I\_m 350 ms terlalu lambat pada sekuens ofensif yang seharusnya "cepat" (misalnya Hundred Fists). Mitigasi: animasi boleh menampilkan lebih banyak pukulan daripada jumlah marker.

**6. Cara pengujian.**

- Telemetri waktu QTE per round di encounter normal. Target: median ≤ 12 s, persentil ke-90 ≤ 18 s.
- Survei fatigue setelah sesi 60 menit (skala 1–5). Target: rata-rata ≤ 2.5.
- Uji batas: musuh 6 hit dan skill 12 hit dijalankan 50 kali masing-masing. Target: tidak ada window yang tumpang tindih dan semua hit terhitung.

### 2. Offensive QTE

#### 2.1 Skill hybrid: QTE dan chance status

**Keputusan: grade QTE ofensif tidak memengaruhi chance status.** QTE hanya mengatur direct damage, simetris dengan aturan defensif (Perfect tidak menghalangi status) dan dengan 5.3.

**1. Tujuan desain.** Satu aturan untuk kedua arah: QTE = damage, resistance dan immunity = status. Pemain tidak perlu menghafal pengecualian.

**2. Aturan/mekanik.**

- Pada skill hybrid (damage + status), grade QTE diterapkan pada semua component direct damage (aturan yang sudah ada).
- Chance status dihitung dari base chance skill dan Status Effect Resistance target, tanpa pengaruh grade.
- Status di-roll walaupun grade-nya Poor, dan walaupun damage akhirnya 0 karena Defense atau Nullification.
- Status di-roll per hit hanya jika skill secara eksplisit menerapkannya per hit (aturan Bab 11 §7).
- Pengecualian hanya boleh ada sebagai efek skill yang tertulis eksplisit di `qteProfile` skill tersebut, misalnya "Perfect: +1 durasi Bleed". Pengecualian seperti ini didokumentasikan per skill di Class System.

**3. Interaksi dengan sistem lain.** Status Effect System: tidak ada perubahan pada rumus chance. Class System: skill control dan debuff tanpa damage tetap tanpa QTE (keputusan Bagian 2). Bagian 7: efek Perfect khusus class (misalnya bonus combo Martial Artist) tetap diizinkan karena bukan chance status.

**4. Kurva kesulitan dan sumber tantangan.** Pemain yang mengandalkan status harus berinvestasi pada chance dan resistance reduction, bukan pada eksekusi. Ini menjaga perbedaan identitas antara build eksekusi dan build status.

**5. Risiko.**

- QTE pada skill hybrid bisa terasa "setengah berguna" bagi pemain build status. Diterima; mitigasinya adalah pengecualian per skill lewat `qteProfile`.
- Pengecualian per skill bisa menumpuk dan membuat aturan umum kabur. Mitigasi: batas maksimal 1 efek Perfect khusus per class tree. Fase 3 memperjelas bahwa mekanik inti class (seperti bonus Perfect combo Martial Artist) tidak dihitung, sehingga efek Martial Artist yang dihitung adalah Perfect Counter.

**6. Cara pengujian.**

- Simulasi 10.000 roll per grade untuk satu skill hybrid. Target: frekuensi status tiap grade sama dalam selisih ±1%.
- Uji pemahaman: setelah sesi, tester ditanya apakah Perfect menaikkan chance status. Target: minimal 80% menjawab benar.

#### 2.2 Grade pada QTE sekuens

**Keputusan: setiap marker dinilai sendiri, dan grade-nya hanya berlaku untuk hit-hit di marker tersebut.** Satu Poor di tengah sekuens tidak membatalkan marker lain.

**1. Tujuan desain.** Skill multi-hit harus memberi hadiah pada konsistensi sepanjang rangkaian, tanpa membuat satu kesalahan menghapus seluruh nilai skill.

**2. Aturan/mekanik.**

- Jumlah marker = jumlah hit, maksimal 6 (1.3), kecuali qteProfile skill menetapkan jumlah marker sendiri (tetap maksimal 6; contoh Hundred Fists 5 marker, 7.2). Jika hit lebih dari 6, hit dibagi merata, dan sisa pembagian dibagikan satu per satu mulai dari marker pertama. Contoh: 8 hit → 6 marker berisi 2, 2, 1, 1, 1, 1.
- Setiap marker memakai aturan penilaian 1.2 dan menghasilkan grade sendiri.
- Multiplier grade marker diterapkan pada setiap hit di dalamnya, termasuk semua component damage-nya.
- Critical tetap di-roll per hit, terpisah dari grade marker.
- Tidak ada bonus maupun penalti untuk "semua Perfect" pada tingkat sistem. Bonus seperti itu hanya boleh ada sebagai efek skill di `qteProfile`.
- Contoh: skill 3 hit, 100 damage per hit, hasil Perfect–Poor–Good → 150 + 50 + 100 = 300 (sebelum Defense dan modifier lain).

**3. Interaksi dengan sistem lain.** Damage calculation: rumus Bab 11 §7 dijalankan per hit dengan multiplier dari marker-nya. Bagian 7: gain combo Martial Artist dari sekuens ditetapkan di sana, tetapi sumber datanya sudah tersedia (grade per marker). Durasi (1.3): sekuens 6 marker berlangsung sekitar 3.4 detik.

**4. Kurva kesulitan dan sumber tantangan.** Tantangan datang dari menjaga ritme selama beberapa marker berturut-turut, terutama jika interval tidak teratur. Damage maksimum skill multi-hit hanya tercapai jika pemain konsisten, bukan hanya beruntung sekali.

**5. Risiko.**

- Skill multi-hit menjadi jauh lebih bervariasi dibanding skill single-hit, sehingga sulit dibalance. Mitigasi: koefisien damage multi-hit dihitung dengan asumsi grade rata-rata Good (1.0×).
- Pembagian hit tidak merata bisa membuat marker pertama terasa lebih penting. Diterima; efeknya kecil (maksimal selisih 1 hit per marker).

**6. Cara pengujian.**

- Unit test pembagian hit untuk 1 sampai 12 hit. Target: jumlah hit selalu sama dengan total di semua marker.
- Telemetri: varians damage per penggunaan skill multi-hit dibanding single-hit. Target: koefisien variasi skill multi-hit tidak lebih dari 1.5 kali skill single-hit.

#### 2.3 Skill AoE ofensif

**Keputusan: satu QTE per hit untuk semua target, dengan grade yang berlaku seragam.** Aturan ini adalah cermin dari Shared QTE defensif (3.2).

**1. Tujuan desain.** Skill AoE tetap menuntut eksekusi tanpa menggandakan jumlah QTE sesuai jumlah musuh, dan aturannya identik di kedua arah.

**2. Aturan/mekanik.**

- Skill yang mengenai lebih dari satu target memakai satu QTE per hit. Grade-nya berlaku untuk semua target hit tersebut.
- AoE multi-hit memakai sekuens (2.2): satu marker per hit, dan grade tiap marker berlaku untuk semua target.
- Hit Chance tetap di-roll per target. Grade Perfect tidak menjamin semua target terkena (aturan "QTE terpisah dari Hit Chance").
- Critical tetap di-roll per target per hit.
- Header bar menampilkan nama skill dan jumlah target.
- Contoh: AoE 1 hit ke 3 musuh, 80 damage per target, Perfect → setiap target yang terkena menerima 120 sebelum modifier.

**3. Interaksi dengan sistem lain.** Damage calculation: affinity, Defense, dan Nullification tetap dihitung per target, sehingga satu grade bisa menghasilkan angka berbeda di tiap target. Class System: skill seperti Heaven's Wrath kini punya QTE profile yang jelas (1 QTE per hit). Durasi (1.3): jumlah target tidak menambah durasi.

**4. Kurva kesulitan dan sumber tantangan.** Satu QTE menentukan damage ke banyak musuh sekaligus, sehingga taruhannya lebih tinggi. Pemain memutuskan kapan memakai AoE berdasarkan seberapa yakin ia bisa mengeksekusinya, bukan hanya berdasarkan jumlah musuh.

**5. Risiko.**

- Satu Perfect pada AoE bisa menghasilkan lonjakan damage yang besar, terutama dikombinasikan dengan Critical per target. Mitigasi: koefisien damage AoE per target maksimal 60% dari skill single-target setara (ditetapkan di Fase 3, sama dengan 3.2).
- Satu Poor pada AoE terasa menghukum. Diterima; ini sepadan dengan risiko serangan AoE musuh.

**6. Cara pengujian.**

- Uji fungsional: AoE ke 1, 2, 3, dan 4 target untuk tiap grade, dengan Hit Chance dan Critical aktif. Target: grade sama di semua target, sementara Hit dan Critical tetap independen.
- Telemetri: pembagian damage total party antara AoE dan single-target. Jika AoE menyumbang lebih dari 50% damage total di encounter normal, koefisien AoE ditinjau ulang.

#### 2.4 Konsekuensi Poor selain pengurangan damage

**Keputusan: secara umum, Poor tidak punya konsekuensi tambahan di luar 0.5× damage dan 0.5× Break Power (3.1).** Konsekuensi lain hanya boleh ada sebagai mekanik class (seperti reset combo Martial Artist) atau tertulis di `qteProfile` skill.

**1. Tujuan desain.** Mencegah death spiral: kesalahan eksekusi sudah dihukum lewat damage, dan hukuman bertumpuk akan membuat pemain yang sedang tertinggal semakin sulit bangkit.

**2. Aturan/mekanik.**

- Poor ofensif: 0.5× damage dan 0.5× Break Power. Tidak ada efek lain secara global: MP tetap terpakai sesuai biaya skill, tidak ada self-debuff, tidak ada kehilangan giliran.
- Konsekuensi tambahan hanya sah jika memenuhi salah satu syarat:
  1. Bagian dari mekanik inti class (contoh: Poor mereset combo Martial Artist).
  2. Tertulis eksplisit di `qteProfile` skill (contoh kandidat: Perfect Counter gagal jika Poor).
- Setiap konsekuensi tambahan wajib terlihat di tooltip skill sebelum pemain memilihnya.
- Batas: maksimal satu konsekuensi tambahan per skill.

**3. Interaksi dengan sistem lain.** Class System: `qteProfile` menjadi tempat resmi untuk mencatat konsekuensi per skill. Bagian 7: aturan combo Martial Artist per grade tetap berlaku. Survival: MP tetap terbuang saat Poor, sehingga kesalahan eksekusi tetap berdampak pada resource.

**4. Kurva kesulitan dan sumber tantangan.** Tantangan datang dari pilihan pemain: skill berisiko tinggi (dengan konsekuensi Poor yang tertulis) memberi hadiah lebih besar, sedangkan skill dasar aman. Kesulitan tidak datang dari hukuman tersembunyi.

**5. Risiko.**

- Poor terasa terlalu ringan bagi game hardcore. Penilaian: MP yang terbuang dan damage yang hilang sudah cukup berat dalam ekonomi resource yang ketat; ini dicek ulang di Fase 3.
- Konsekuensi per skill bisa berkembang menjadi aturan yang tidak konsisten. Mitigasi: batas satu konsekuensi per skill dan wajib tampil di tooltip.

**6. Cara pengujian.**

- Audit data: semua skill dengan konsekuensi Poor dicek terhadap dua syarat sah di atas. Target: 0 pelanggaran.
- Telemetri comeback: rasio menang encounter setelah pemain mendapat 3 Poor beruntun. Jika di bawah 25%, ada indikasi death spiral yang perlu ditinjau.

### 3. Defensive QTE

#### 3.1 Broken dan Defensive QTE

**Keputusan: selama Broken, zona Perfect Defensive QTE ditutup sehingga hasil terbaik adalah Good. Break Power yang diterima juga diskalakan dengan grade Defensive QTE.** Spesifikasi ini memakai asumsi A5 karena definisi global Broken belum ada di GDD; jika definisi resmi berbeda, item ini direvisi.

**1. Tujuan desain.** Broken menjadi jendela kerentanan yang terasa dan bisa dicegah: pemain yang konsisten Perfect tidak akan pernah Broken, sementara pemain yang ceroboh menghadapi satu giliran pertahanan yang lebih lemah.

**2. Aturan/mekanik.**

- Break gauge maksimal 100 per karakter (A5).
- Break Power per hit didefinisikan per serangan musuh (*tuning*): serangan biasa 10–20, serangan Heavy 30–40.
- Break Power yang diterima = Break Power × (1 − R\_QTE). Perfect → 0, Good → 50%, Poor → 100%, Perfect pada Heavy → 50%.
- Gauge berkurang 20 di awal setiap giliran karakter itu sendiri (*tuning*), agar gauge tidak tertahan di dekat penuh.
- Saat gauge mencapai 100, karakter menjadi Broken sampai akhir giliran berikutnya miliknya, lalu gauge kembali ke 0.
- Selama Broken:
  - Zona Perfect tidak ditampilkan dan tidak bisa diraih; penekanan di rentang Perfect dihitung Good. Window Good tidak berubah.
  - Karakter tidak bisa menerima Break Power (tidak bisa Broken berulang).
  - Offensive QTE karakter itu tidak terpengaruh.
- Jika Broken dan Stun aktif bersamaan, Stun yang berlaku (auto-Poor).
- Combo Martial Artist tetap reset saat Broken (aturan yang sudah ada).

**3. Interaksi dengan sistem lain.** Survival: selama Broken, setiap hit minimal masuk 50%, sehingga healing dan Shield harus disiapkan. Class System: reset combo Martial Artist membuat Broken sangat mahal bagi class ini. Interrupt (6.5): musuh juga punya Break gauge, dan Break Power dari serangan pemain diskalakan dengan grade Offensive QTE (Perfect 1.5×, Good 1.0×, Poor 0.5×).

**4. Kurva kesulitan dan sumber tantangan.** Musuh dengan Break Power tinggi menekan pemain untuk menjaga konsistensi eksekusi, bukan hanya bertahan dari damage. Pemain bisa membaca gauge lawan dan dirinya sendiri, lalu memutuskan kapan bertahan, kapan menyembuhkan, dan kapan menyerang.

**5. Risiko.**

- Snowball: Broken → damage lebih besar → party wipe. Mitigasi: durasi pendek, gauge reset ke 0, dan tidak bisa Broken berulang.
- Dua gauge (pemain dan musuh) menambah beban baca HUD. Mitigasi: Bagian 9 menampilkan gauge hanya saat terisi di atas 50.
- Asumsi A5 bisa berbeda dari definisi resmi nanti. Mitigasi: aturan QTE (tutup zona Perfect) tetap berlaku apa pun trigger Broken-nya; yang perlu direvisi hanya angka gauge.

**6. Cara pengujian.**

- Telemetri: frekuensi Broken per encounter. Target: rata-rata 0.5–1.5 kali per encounter elite, dan di bawah 0.5 di encounter normal.
- Uji pemahaman: setelah Broken pertama, tester ditanya mengapa ia Broken. Target: minimal 75% menyebut gauge atau Poor sebagai penyebab.
- Telemetri wipe: jika lebih dari 30% wipe terjadi saat ada karakter Broken, angka Break Power ditinjau ulang.

#### 3.2 Serangan multi-target yang tidak mengenai seluruh party

**Keputusan: setiap hit musuh yang mengenai lebih dari satu target memakai satu Shared QTE, berapa pun jumlah targetnya.** Shared QTE ditentukan oleh jumlah target lebih dari satu, bukan oleh label "AoE".

**1. Tujuan desain.** Pemain hanya menghadapi satu keputusan timing per hit, sehingga durasi tetap terkendali dan aturannya konsisten untuk 2, 3, maupun 4 target.

**2. Aturan/mekanik.**

- Hit dengan 1 target → QTE individual. Hit dengan 2–4 target → satu Shared QTE.
- Header bar menampilkan potret semua target yang terkena (1.1).
- Hasil Shared QTE berlaku seragam untuk semua target yang bisa bereaksi.
- Target yang tidak bisa bereaksi (Stun, lihat 3.3) tetap menerima Poor walaupun hasil Shared-nya Perfect.
- Serangan musuh yang memilih target berbeda per hit (misalnya hit 1 ke A, hit 2 ke B) memakai QTE individual per hit, ditampilkan sebagai marker pada satu bar dengan potret target di atas tiap marker.

**3. Interaksi dengan sistem lain.** Multi-hit (Bagian 4): satu Shared QTE per hit, sehingga serangan 3 hit ke 2 target menghasilkan 3 marker. Damage calculation (5.1): reduksi Shared QTE diterapkan per target dengan Damage Reduction masing-masing. Durasi (1.3): jumlah target tidak menambah durasi.

**4. Kurva kesulitan dan sumber tantangan.** Satu kesalahan pada serangan multi-target berdampak ke beberapa karakter sekaligus. Tekanan ini bersifat adil karena pemain sudah melihat potret target sebelum menekan, dan bisa mengantisipasinya lewat buff, Shield, atau healing sebelum giliran musuh.

**5. Risiko.**

- Satu Poor pada serangan 4 target terasa terlalu menghukum. Diterima sebagai bagian dari identitas hardcore, tetapi damage per target untuk serangan multi-target maksimal 60% dari serangan single-target setara (aturan desain musuh, ditetapkan di Fase 3).
- Pemain bisa tidak menyadari bahwa karakter yang Stun tidak ikut menerima hasil Perfect. Mitigasi: Bagian 9 menampilkan tanda auto-Poor pada potret tersebut.

**6. Cara pengujian.**

- Uji fungsional: serangan 2, 3, dan 4 target, masing-masing dengan dan tanpa karakter Stun. Target: semua hasil per target sesuai aturan.
- Survei playtest: "Apakah kamu tahu siapa saja yang terkena serangan sebelum menekan?" Target: minimal 85% menjawab ya.

#### 3.3 Karakter yang tidak dapat bereaksi selain karena Stun

**Keputusan: hanya kondisi yang menghilangkan Action sepenuhnya yang memaksa Poor.** Status yang hanya memberi peluang gagal atau salah aksi tidak memengaruhi Defensive QTE, karena reaksi bukan Action di giliran sendiri.

**1. Tujuan desain.** Pemain harus tahu persis kapan kontrol diambil darinya. Daftarnya pendek dan jelas, sehingga tidak ada auto-Poor yang terasa mendadak.

**2. Aturan/mekanik.**

| Kondisi | Efek ke Defensive QTE | Alasan |
| --- | --- | --- |
| Stun | Auto-Poor | Sudah diputuskan; Stun mencegah Action |
| Karakter Down (HP 0) | Tidak ada QTE; jika aturan Combat System membuatnya tetap terkena hit, hasilnya Poor | Karakter tidak sadar |
| Freeze | QTE normal, window menyempit (batas 66 ms) | Freeze hanya memperlambat, tidak menghilangkan Action |
| Shock | Tidak berpengaruh | Peluang gagal hanya untuk Action di giliran sendiri |
| Confusion, Fear, Despair (Mental) | Tidak berpengaruh | Mengganggu keputusan, bukan refleks |
| Broken | Zona Perfect ditutup, hasil terbaik Good (3.1) | Kerentanan sementara, bukan hilangnya kontrol (asumsi A5) |

- Aturan umum untuk status baru: status yang tertulis "mencegah Action" otomatis masuk kategori auto-Poor; status lain tidak memengaruhi QTE kecuali deskripsinya menyebutkan efek QTE secara eksplisit. Sinkronisasi dokumen (4 November 2026): berdasarkan Bab 11 §12, Sleep dan Mind Control juga masuk kategori auto-Poor, dan aksi yang tidak dipilih pemain (Mind Control, Madness, salah aksi akibat Confusion) diselesaikan tanpa Offensive QTE dengan grade Good.

**3. Interaksi dengan sistem lain.** Status Effect System: Stun menjadi status paling berbahaya di game, sehingga resistance terhadap Stun dan cleanse-nya bernilai tinggi. Shared QTE (3.2): karakter Stun menerima Poor walaupun hasil Shared-nya lebih baik. Combat System: perilaku hit terhadap karakter Down mengikuti aturan Combat System yang belum ada di project ini.

**4. Kurva kesulitan dan sumber tantangan.** Musuh yang memakai Stun lalu menyerang di giliran berikutnya menjadi pola ancaman yang jelas. Pemain bisa meresponsnya dengan cleanse, membunuh musuh pemberi Stun lebih dulu, atau menyiapkan Shield untuk karakter yang terkena.

**5. Risiko.**

- Kombo Stun lalu serangan Heavy dari boss bisa memberi damage penuh tanpa bisa dicegah. Mitigasi: aturan desain boss, yaitu boss tidak boleh memberi Stun dan serangan Heavy ke target yang sama dalam satu round (dicek di Bagian 6).
- Pemain bisa menganggap Shock juga harus memengaruhi QTE. Diterima; tooltip Shock menjelaskan cakupannya.

**6. Cara pengujian.**

- Matriks uji: setiap kondisi di tabel × serangan single-target dan Shared. Target: semua hasil sesuai tabel.
- Telemetri penyebab party wipe. Jika lebih dari 30% wipe terjadi dalam round yang sama dengan Stun, frekuensi Stun musuh ditinjau ulang.

#### 3.4 Modifier Defensive QTE dari equipment, talent, dan passive

**Keputusan: modifier hanya boleh melebarkan window, dengan total maksimal +25% dari semua sumber.** Modifier tidak pernah mengubah nilai reduksi, batas Heavy, atau langsung memberi grade tertentu. Aturan ini juga menjadi dasar item equipment dan talent di Bagian 8.

**1. Tujuan desain.** Build yang berinvestasi pada QTE (misalnya build "QTE specialist" Martial Artist) harus terasa lebih mudah dieksekusi, tetapi pemain tetap harus menekan tepat waktu sendiri.

**2. Aturan/mekanik.**

- Satu-satunya jenis modifier: **Window +x%**, berlaku pada W\_P dan W\_G sekaligus.
- Setiap modifier wajib menyebut cakupannya: defensif, ofensif, atau keduanya.
- Modifier dari semua sumber ditumpuk secara additive, dengan cap **+25%** (*tuning*).
- Urutan perhitungan window:
  1. Window dasar sesuai tier musuh (Bagian 6).
  2. Penyempitan dari Freeze.
  3. Pelebaran dari modifier pemain.
  4. Clamp W\_P ke rentang 66–125 ms. Pengali assist (10.3) diterapkan sesudah clamp dan boleh melewati 125 ms.
- Contoh: window dasar normal W\_P 90 ms dan modifier total +20% → 108 ms. Pada boss dengan W\_P dasar 70 ms dan Freeze −15% → 59.5 ms, lalu +20% → 71.4 ms.
- Dilarang: mengubah R\_QTE (1.0/0.5/0), mengubah batas Heavy 50%, mengubah grade secara otomatis, atau membatalkan auto-Poor dari Stun.
- Lucky tetap mekanik terpisah (retry), bukan modifier window, dan tidak dihitung dalam cap +25%.

**3. Interaksi dengan sistem lain.** Bagian 8: equipment dan talent yang memodifikasi QTE wajib memakai jenis modifier ini. Bagian 7: passive Martial Artist Agile Defense dan Inner Focus memakai modifier ini (7.5). Accessibility (Bagian 10): pengaturan timing window dari menu tidak dihitung dalam cap +25%, karena merupakan pengaturan pemain, bukan kekuatan karakter.

**4. Kurva kesulitan dan sumber tantangan.** Pemain menukar slot equipment atau talent dengan kemudahan eksekusi, sebagai ganti stat damage atau defense. Cap 25% memastikan boss tetap menuntut eksekusi, karena W\_P boss tidak akan pernah lebih lebar dari W\_P musuh normal tanpa modifier (72 × 1.25 = 90 ms, lihat 6.1).

**5. Risiko.**

- Pemain menumpuk modifier sampai cap lalu mengabaikan Damage Reduction, sehingga build menjadi seragam. Mitigasi: setiap modifier window harus memakai slot yang bersaing dengan stat utama.
- Pelebaran window bisa membuat zona Perfect dan Good pada sekuens saling mendekat. Mitigasi: interval marker minimum dihitung dari W\_G setelah modifier (1.3).

**6. Cara pengujian.**

- Unit test urutan perhitungan: 12 kombinasi tier, Freeze, dan modifier. Target: semua sesuai clamp 66–125 ms.
- Uji perbandingan build: tester yang sama memainkan boss dengan modifier 0% dan +25%. Target: rasio Perfect naik 10–20 poin persentase; kenaikan lebih dari 30 poin berarti cap terlalu tinggi.

### 4. Multi-hit dan AoE

#### 4.1 Timing window setiap hit

**Keputusan: semua marker dalam satu bar memakai window yang sama.** Variasi antar hit hanya datang dari interval antar marker (6.2), bukan dari lebar window.

**1. Tujuan desain.** Dalam satu serangan, pemain hanya perlu membaca satu variabel yang berubah (kapan), bukan dua (kapan dan seberapa sempit). Ini menjaga multi-hit tetap sulit tetapi adil.

**2. Aturan/mekanik.**

- W\_P dan W\_G dihitung sekali saat bar muncul, dengan urutan di 3.4 (tier → Freeze → modifier → clamp), lalu berlaku untuk semua marker di bar itu.
- Jika status berubah di tengah bar (misalnya karakter menjadi Broken karena hit sebelumnya), window marker sisanya tetap sama; efek status baru berlaku mulai bar berikutnya. Pengecualian: Broken menutup zona Perfect segera di marker berikutnya, karena perubahan itu ditampilkan langsung di bar.
- Marker Heavy di dalam bar campuran memakai window yang sama; yang berbeda hanya batas reduksinya (50%).
- Visual: setiap marker menampilkan zona dengan lebar identik.

**3. Interaksi dengan sistem lain.** Bagian 6: tier musuh menentukan window seluruh bar. 3.1: Broken adalah satu-satunya perubahan di tengah bar, karena hanya menyembunyikan zona Perfect dan tidak mengubah lebar window Good. Durasi (1.3): interval minimum tetap dihitung dari W\_G bar tersebut.

**4. Kurva kesulitan dan sumber tantangan.** Tantangan multi-hit adalah menjaga ritme pada interval yang bervariasi. Karena lebar zona konsisten, pemain yang gagal tahu bahwa ia salah membaca ritme, bukan dijebak window yang tiba-tiba menyempit.

**5. Risiko.**

- Multi-hit terasa monoton jika interval selalu teratur. Mitigasi: pola interval di 6.2.
- Pengecualian Broken di tengah bar menambah satu aturan khusus. Diterima, karena perubahannya terlihat jelas di bar.

**6. Cara pengujian.**

- Unit test: bar 6 marker dengan perubahan status di marker ke-3. Target: lebar window marker 4–6 tidak berubah, kecuali zona Perfect tertutup saat Broken.
- Telemetri: rasio Perfect per posisi marker (1–6). Target: selisih antara marker pertama dan terakhir tidak lebih dari 15 poin persentase pada pola Steady.

#### 4.2 Kesulitan pada hit berikutnya

**Keputusan: tidak ada penyempitan window progresif.** Eskalasi di dalam satu serangan hanya boleh datang dari dua hal: interval yang memendek atau tidak teratur, dan satu marker Heavy di akhir rangkaian.

**1. Tujuan desain.** Serangan multi-hit boleh punya klimaks, tetapi klimaks itu harus terlihat sejak bar muncul, bukan muncul diam-diam di hit terakhir.

**2. Aturan/mekanik.**

- Window tetap sama di semua marker (4.1).
- Interval boleh memendek antar marker, dengan batas minimum W\_G + 100 ms (1.3).
- Maksimal **1 marker Heavy per bar**, dan jika ada, posisinya wajib di marker terakhir.
- Semua marker, termasuk tanda Heavy, terlihat sejak bar muncul.
- Pola eskalasi hanya boleh dipakai musuh elite dan boss (6.2).
- Multi-hit ofensif milik pemain tidak memakai eskalasi; interval tetap 350 ms (Steady), kecuali `qteProfile` skill menyatakan lain.

**3. Interaksi dengan sistem lain.** 6.2 dan 6.3: pola "Accelerando" dan serangan signature boss memakai aturan ini. 3.1: marker Heavy di akhir membawa Break Power tertinggi, sehingga gagal di marker terakhir bisa memicu Broken. Durasi (1.3): interval yang memendek justru memperpendek total durasi bar.

**4. Kurva kesulitan dan sumber tantangan.** Musuh normal tidak punya eskalasi. Elite memakai interval yang memendek. Boss menggabungkan interval yang memendek dengan marker Heavy di akhir. Pemain bisa melihat seluruh rangkaian dan menyiapkan diri sejak awal bar.

**5. Risiko.**

- Marker Heavy di akhir mudah ditebak, sehingga kehilangan efek kejutan. Diterima: tujuan desainnya keterbacaan, bukan kejutan.
- Interval minimum pada boss (sekitar 280 ms) bisa terasa terlalu cepat bagi pemain baru. Mitigasi: pengaturan kecepatan QTE di Bagian 10.

**6. Cara pengujian.**

- Telemetri: rasio Poor pada marker Heavy terakhir dibanding marker lain di bar yang sama. Target: maksimal 2 kali lipat; lebih dari itu berarti klimaksnya terlalu menghukum.
- Survei boss: "Apakah kamu tahu serangan ini akan berakhir dengan Heavy?" Target: minimal 85% menjawab ya.

#### 4.3 Multi-hit dan status effect

**Keputusan: roll status per hit tetap independen dari grade QTE, dan serangan musuh yang menerapkan status per hit dibatasi maksimal 3 hit.** Batas ini diperlukan karena status instance tidak punya cap dan tidak saling refresh (Bab 11 §8).

**1. Tujuan desain.** Mencegah satu serangan multi-hit menumpuk banyak instance status sekaligus, yang tidak bisa dicegah lewat QTE karena Perfect tidak menghalangi status.

**2. Aturan/mekanik.**

- Skill yang menerapkan status sekali per serangan: roll dilakukan satu kali setelah hit pertama yang mengenai target.
- Skill yang menerapkan status per hit: roll dilakukan per hit yang mengenai target, dan setiap roll yang berhasil menambah satu instance.
- Hit yang meleset (Hit Chance gagal) tidak me-roll status.
- Grade QTE tidak memengaruhi roll status di kedua arah (2.1, 5.3).
- **Batas desain musuh:** serangan dengan status per hit maksimal 3 hit. Serangan 4–6 hit hanya boleh menerapkan status sekali per serangan.
- Skill pemain tidak diberi batas ini secara sistem, tetapi setiap skill status per hit dengan lebih dari 3 hit wajib ditinjau saat skill itu didefinisikan (per Fase 3, belum ada skill yang melanggar).

**3. Interaksi dengan sistem lain.** Status Effect System: aturan instance independen tetap berlaku tanpa perubahan. DoT: instance Bleed, Burn, dan Poison yang menumpuk mengikuti aturan tick Damage GDD dan tidak terpengaruh QTE. Survival: tumpukan DoT menjadi tekanan resource jangka panjang yang hanya bisa diatasi dengan cleanse atau resistance.

**4. Kurva kesulitan dan sumber tantangan.** Ancaman status dari multi-hit dibatasi pada angka yang bisa ditangani: maksimal 3 instance per serangan. Pemain menanggapinya dengan keputusan (cleanse, resistance, membunuh pemberi status), bukan dengan eksekusi QTE.

**5. Risiko.**

- Pemain merasa QTE-nya sia-sia saat terkena tumpukan status meskipun Perfect. Mitigasi: UI di Bagian 9 memisahkan hasil QTE dari ikon status (lihat 5.3).
- Skill pemain yang menumpuk status per hit bisa terlalu kuat melawan boss. Mitigasi: ditinjau di Fase 3.

**6. Cara pengujian.**

- Simulasi 10.000 serangan 3 hit dengan chance status 40%. Target: rata-rata instance per serangan mendekati 1.2 dan maksimal 3.
- Audit data musuh: semua serangan dengan status per hit dicek terhadap batas 3 hit. Target: 0 pelanggaran.

### 5. QTE dan Damage Calculation

#### 5.1 Defensive QTE vs Damage Reduction lain

**Keputusan: Defensive QTE ditumpuk secara multiplicative dengan semua Damage Reduction lain.** Aturan ini meneruskan aturan Multiplicative Stacking yang sudah ada di Damage & Status Effect GDD, sehingga tidak ada sistem baru.

**1. Tujuan desain.** Talent dan equipment defensif harus tetap bernilai, tetapi tidak boleh bisa ditumpuk sampai 100% tanpa eksekusi Perfect.

**2. Aturan/mekanik.**

- "Damage Reduction" dari talent (Durable 5%, Inspiring Leader 10%) diperlakukan sebagai All Damage Taken modifier −x%.
- "Physical Damage Taken Reduction" dari equipment tetap Physical Damage Taken modifier.
- Semua modifier di atas diterapkan pada step yang sudah ada di Bab 11 §7, dan ditumpuk secara multiplicative.
- Defensive QTE diterapkan sesudahnya sebagai faktor terpisah:

```latex
D_{akhir} = \lfloor D \times \prod_i (1 - DR_i) \times (1 - R_{QTE}) \rfloor
```

- R\_QTE: Perfect 1.0 (Heavy 0.5), Good 0.5, Poor 0.
- Contoh: D = 200, Durable + Inspiring Leader → 200 × 0.95 × 0.90 = 171. Good → 85.5 → 85. Perfect → 0. Perfect pada serangan Heavy → 85.
- Tidak perlu cap Damage Reduction, karena tumpukan multiplicative tidak pernah mencapai 100%.

**3. Interaksi dengan sistem lain.** Survival: Damage Reduction menjadi lapisan pengaman untuk serangan yang hasilnya Good atau Poor, terutama serangan Heavy. Shield (step 4 pada urutan di Bagian 5 checklist) menyerap sisa setelah rumus ini. QTE: Perfect membuat Damage Reduction tidak berpengaruh pada hit itu.

**4. Kurva kesulitan dan sumber tantangan.** Di musuh normal, pemain mahir jarang membutuhkan Damage Reduction. Di boss, serangan Heavy (Perfect hanya 50%) membuat build defensif kembali penting. Pemain harus memutuskan antara build defensif dan build ofensif berdasarkan kemampuan eksekusinya sendiri.

**5. Risiko.**

- Pemain berskill tinggi bisa menganggap Durable dan Inspiring Leader tidak berguna. Mitigasi: frekuensi serangan Heavy di elite dan boss (Bagian 6) menjaga nilai keduanya.
- Pemain bisa salah paham bahwa "5% + 10% = 15%". Mitigasi: tooltip menampilkan total efektif (14.5%).

**6. Cara pengujian.**

- Unit test: 20 kombinasi D, Damage Reduction, dan grade, dibandingkan dengan tabel hasil yang dihitung manual.
- Telemetri pick rate Durable dan Inspiring Leader. Jika pick rate di kelompok pemain dengan rasio Perfect ≥ 50% di bawah 10%, nilai talent ditinjau ulang.

#### 5.2 Metode rounding final

**Keputusan: Floor, satu kali per hit, di akhir perhitungan.** Pilihan ini mengonfirmasi rekomendasi kerja yang sudah tercatat di Damage GDD.

**1. Tujuan desain.** Angka di layar harus sama persis dengan perubahan HP, dan hasilnya harus bisa diprediksi pemain tanpa kalkulator.

**2. Aturan/mekanik.**

- Seluruh perhitungan memakai nilai desimal penuh, tanpa pembulatan di tengah.
- Hit dengan beberapa component: nilai tiap component dijumlahkan dulu, lalu di-floor sekali.
- Ofensif: floor dilakukan setelah semua step Bab 11 §7 selesai.
- Defensif: floor dilakukan setelah reduksi Defensive QTE dan sebelum Shield menyerap (urutan di Bagian 5 checklist).
- Hasil 0 diperbolehkan; tidak ada minimum damage 1 (sesuai Damage GDD).
- Contoh: 85.5 → 85; 0.9 → 0; component 40.6 + 30.7 = 71.3 → 71.

**3. Interaksi dengan sistem lain.** QTE: multiplier 0.5 dan 1.5 sering menghasilkan .5, dan floor selalu membulatkannya ke bawah. Pada ofensif ini sedikit merugikan pemain; pada defensif sedikit menguntungkan pemain. Shield dan HP selalu menerima bilangan bulat. DoT tidak terpengaruh QTE, tetapi tetap memakai floor agar konsisten.

**4. Kurva kesulitan dan sumber tantangan.** Tidak ada. Rounding sengaja dibuat netral dan bukan sumber tantangan.

**5. Risiko.**

- Hit sangat kecil (di bawah 2 setelah modifier) dengan hasil Good menjadi 0. Risiko rendah, karena damage musuh dirancang jauh di atas angka ini. Desainer musuh tidak boleh membuat hit dengan base damage di bawah 5.
- Pemain yang menghitung manual bisa menemukan selisih 1 dibanding perkiraannya. Diterima.

**6. Cara pengujian.**

- Unit test kasus batas: nilai .0, .5, .99, multi-component, dan hasil 0.
- Uji konsistensi UI: 500 hit acak, angka damage di layar dibandingkan dengan perubahan HP aktual. Target: 0 selisih.

#### 5.3 QTE vs Status Immunity

**Keputusan: QTE dan Status Immunity tidak saling memengaruhi.** QTE hanya mengatur direct damage; Immunity hanya mengatur penerapan status. Item ini juga memperjelas satu keputusan yang sudah di-approve (lihat butir terakhir di Aturan).

**1. Tujuan desain.** Pemain harus bisa memisahkan dua pertanyaan dengan jelas: "seberapa besar damage yang masuk" (QTE) dan "apakah status bisa diterapkan" (resistance dan immunity).

**2. Aturan/mekanik.**

- Immunity diperiksa saat status akan diterapkan, sebelum roll chance. Target yang immune tidak pernah terkena status, apa pun grade QTE-nya.
- Grade QTE tidak pernah menembus Immunity, termasuk Mental Effect immunity milik boss.
- Defensive Perfect tidak menghalangi status (aturan yang sudah ada). Satu-satunya perlindungan dari status adalah resistance dan immunity.
- Skill dengan component damage tetap memunculkan QTE walaupun semua target immune terhadap status skill tersebut; QTE tetap mengatur damage-nya.
- QTE tetap muncul walaupun component damage dinullifikasi. Pemain sudah memilih aksinya, dan menghilangkan QTE akan terasa seperti bug.
- **Klarifikasi:** "Semua serangan musuh memicu Defensive QTE" dibaca sebagai semua aksi musuh yang punya minimal satu component direct damage. Aksi musuh yang hanya menerapkan status tidak memicu QTE, karena Perfect pun tidak bisa menghalanginya. QTE seperti itu tidak bermakna.

**3. Interaksi dengan sistem lain.** Status Effect System: resistance mengurangi chance, immunity membatalkannya; keduanya berjalan independen dari QTE. Turn-based: aksi status-only musuh berjalan tanpa QTE, sehingga round lebih singkat (mendukung target durasi di 1.3). Bagian 2: apakah grade QTE ofensif memengaruhi chance status skill hybrid sudah diputuskan di 2.1: tidak memengaruhi.

**4. Kurva kesulitan dan sumber tantangan.** Ancaman status datang dari keputusan pemain: membangun resistance, menyiapkan cleanse, dan memprioritaskan musuh yang menggunakan status. QTE tidak bisa menutupi kelemahan di area ini, sehingga build yang hanya mengandalkan eksekusi tetap rentan.

**5. Risiko.**

- Pemain merasa dicurangi ketika Perfect defend tetap terkena status. Mitigasi: Bagian 9 harus menampilkan ikon status secara terpisah dari hasil QTE, agar penyebabnya jelas.
- Aksi status-only tanpa QTE bisa membuat pemain bingung karena prompt tidak muncul. Mitigasi: telegraph aksi musuh menampilkan ikon "status".

**6. Cara pengujian.**

- Matriks uji: 3 grade QTE × immune/tidak immune × aksi damage/hybrid/status-only. Target: semua 18 kasus sesuai aturan.
- Wawancara playtest setelah pemain terkena status saat Perfect. Target: minimal 80% bisa menjelaskan bahwa QTE tidak menghalangi status.

### 6. Enemy QTE dan Difficulty Scaling

#### 6.1 Angka window dan T\_lead per tier dan dungeon

**Keputusan: lebar window ditetapkan per tier musuh, sedangkan progresi antar dungeon hanya mempercepat cursor (T\_lead).** Dengan begitu, zona Perfect tetap bisa diraih sampai dungeon 8, dan tidak pernah turun di bawah batas 66 ms tanpa status.

**1. Tujuan desain.** Memberi kurva kesulitan yang terbaca: tier menentukan seberapa presisi pemain harus menekan, dungeon menentukan seberapa cepat pemain harus bereaksi.

**2. Aturan/mekanik.**

Nilai dasar (dungeon 1–3, *tuning*):

| Tier | T\_lead (ms) | W\_P (ms) | W\_G total (ms) | Interval minimum (ms) |
| --- | --- | --- | --- | --- |
| Normal | 1000 | 90 | 240 | 340 |
| Elite | 900 | 80 | 210 | 310 |
| Boss | 800 | 72 | 180 | 280 |

- Pengali T\_lead per dungeon: dungeon 1–3 ×1.0, dungeon 4–6 ×0.9, dungeon 7–8 ×0.8. Contoh: boss di dungeon 8 memakai T\_lead 640 ms.
- Lebar window tidak berubah antar dungeon.
- Freeze menyempitkan W\_P dan W\_G sebesar 15% (*tuning*), lalu modifier dan clamp diterapkan sesuai urutan di 3.4.
- Contoh terburuk tanpa modifier: boss + Freeze → 72 × 0.85 = 61.2 ms → di-clamp ke 66 ms.
- Serangan Heavy memakai angka tier yang sama; yang berbeda hanya batas reduksinya.
- Interval minimum = W\_G + 100 ms, dihitung ulang jika W\_G berubah karena Freeze atau modifier.

**3. Interaksi dengan sistem lain.** 1.3: T\_lead semua kombinasi berada di rentang 640–1000 ms, sesuai rentang di 1.3. 3.4: cap modifier +25% membuat W\_P boss maksimal 90 ms, setara dengan musuh normal tanpa modifier. Difficulty System: progresi 8 dungeon sequential tercermin langsung pada pengali T\_lead.

**4. Kurva kesulitan dan sumber tantangan.** Dua sumbu yang terpisah: presisi (tier) dan kecepatan reaksi (dungeon). Pemain yang lolos dungeon awal sudah menguasai presisi musuh normal, lalu ditantang kecepatan di dungeon berikutnya. Boss di dungeon akhir menggabungkan keduanya.

**5. Risiko.**

- Pengali 0.8 di dungeon 7–8 bisa terlalu cepat untuk pemain yang bermain dengan controller Bluetooth. Mitigasi: kalibrasi offset (Bagian 10) dan uji khusus perangkat.
- Window elite dan boss yang berdekatan (80 vs 72 ms) bisa membuat perbedaan tier kurang terasa. Diterima, karena boss dibedakan juga lewat pola (6.2, 6.3).

**6. Cara pengujian.**

- Telemetri rasio grade per tier dan per kelompok dungeon. Target untuk pemain rata-rata: Perfect normal 40–55%, elite 30–45%, boss 20–35%; Poor di semua tier di bawah 25%.
- Uji perangkat: tester dengan controller Bluetooth setelah kalibrasi di dungeon 7–8. Target: rasio Poor tidak lebih dari 5 poin persentase di atas tester keyboard.

#### 6.2 Variasi pola timing antar musuh

**Keputusan: empat pola interval yang tetap per serangan, bukan acak.** Setiap serangan musuh selalu memakai pola yang sama, sehingga pemain bisa mempelajarinya dan kegagalan bisa diperbaiki pada pertemuan berikutnya.

**1. Tujuan desain.** Membedakan musuh lewat ritme serangan, sehingga mengenali musuh menjadi bagian dari keterampilan pemain, tanpa RNG yang tidak adil.

**2. Aturan/mekanik.**

| Pola | Interval antar marker | Tier yang boleh memakai |
| --- | --- | --- |
| Steady | Semua sama, default 350 ms | Semua tier |
| Delayed | Satu interval × 1.6 ("tahan"), sisanya default | Elite, boss |
| Accelerando | Setiap interval berkurang 15% dari sebelumnya, minimal interval minimum tier | Elite, boss |
| Syncopated | Selang-seling panjang dan pendek: 450 ms dan interval minimum tier | Boss |

- Pola ditentukan per serangan di data musuh, bukan di-roll saat battle.
- Semua marker terlihat sejak bar muncul (4.2), jadi pola terbaca sebelum hit pertama.
- Musuh normal hanya memakai Steady. Elite memakai maksimal 2 pola. Boss boleh memakai semuanya.
- Serangan single-hit tidak punya pola; variasinya hanya pada T\_lead tier (6.1).
- Contoh Accelerando untuk elite dengan 4 marker: 350 → 310 (minimum) → 310 → 310 ms.

**3. Interaksi dengan sistem lain.** 4.1: window tetap sama, sehingga pola hanya mengubah kapan. 4.2: Accelerando menjadi satu-satunya bentuk eskalasi interval. 1.3: Delayed memperpanjang bar maksimal sekitar 210 ms, masih dalam batas durasi. Audio cue (1.1): ritme tick mengikuti pola, sehingga pola juga bisa dipelajari lewat suara.

**4. Kurva kesulitan dan sumber tantangan.** Musuh normal mengajarkan ritme dasar. Elite memperkenalkan satu gangguan ritme. Boss menggabungkan beberapa pola. Tantangannya adalah mengenali pola dan menyesuaikan diri, sesuatu yang membaik seiring pengalaman pemain dalam satu run.

**5. Risiko.**

- Pola yang tetap bisa dihafal sampai musuh terasa terlalu mudah. Diterima: menghafal adalah hadiah, dan tekanan tetap datang dari T\_lead dungeon.
- Syncopated dengan interval minimum boss (280 ms) bisa terlalu padat dikombinasikan dengan Freeze. Mitigasi: interval minimum dihitung ulang dari W\_G setelah Freeze (6.1).

**6. Cara pengujian.**

- Telemetri belajar: rasio Perfect pada serangan yang sama di pertemuan ke-1 dibanding ke-3. Target: naik minimal 10 poin persentase, sebagai bukti pola bisa dipelajari.
- Uji per pola: 20 tester × 30 serangan per pola. Target: Syncopated menghasilkan Poor tertinggi, tetapi tetap di bawah 30%.

#### 6.3 Pola QTE khusus boss

**Keputusan: setiap boss punya satu serangan signature dan satu perubahan fase di 50% HP.** Semua serangan berat boss tunduk pada batasan keadilan yang sudah ditetapkan di 3.3 dan 4.2.

**1. Tujuan desain.** Boss menjadi ujian penguasaan semua keterampilan QTE dalam satu pertarungan: presisi, ritme, dan membaca telegraph. Ujian ini harus bisa dipelajari dalam satu run.

**2. Aturan/mekanik.**

- Setiap boss punya 3–5 serangan, termasuk tepat 1 **serangan signature**: 4–6 hit, pola Accelerando atau Syncopated, dengan marker Heavy di akhir.
- **Fase 2 di 50% HP:** boss membuka 1 serangan baru atau 1 pola baru, ditandai animasi transisi dan teks singkat di layar.
- Batasan keadilan:
  - Maksimal 1 serangan berisi marker Heavy per round.
  - Tidak boleh memberi Stun dan serangan Heavy ke target yang sama dalam round yang sama (3.3).
  - Serangan signature wajib ditelegraph satu giliran sebelumnya (6.4).
- Break gauge boss: 200 (dua kali karakter biasa, *tuning*). Saat Broken, boss mengikuti aturan A5 dan serangan charge-nya batal (6.5).
- Boss tetap immune terhadap seluruh Mental Effect (aturan Damage GDD yang sudah ada).

**3. Interaksi dengan sistem lain.** 6.1: angka window dan T\_lead tier boss berlaku untuk semua serangan boss. 6.2: boss satu-satunya tier yang boleh memakai Syncopated. 3.1 dan 6.5: Break gauge 200 membuat interrupt boss memerlukan beberapa hit Perfect yang terencana. Survival: serangan signature adalah titik tekanan HP terbesar dalam pertarungan, sehingga pemain perlu menyimpan heal dan Shield untuknya.

**4. Kurva kesulitan dan sumber tantangan.** Fase 1 mengajarkan serangan dasar boss. Signature menguji eksekusi di bawah tekanan. Fase 2 menguji adaptasi. Pemain yang kalah tahu apa yang harus diperbaiki, tetapi karena kekalahan berarti run berakhir (Difficulty System), setiap pertemuan boss sangat bermakna.

**5. Risiko.**

- Karena run berakhir saat kalah, pemain tidak bisa "latihan" boss sebelum benar-benar menghadapinya. Mitigasi: musuh elite di dungeon yang sama memakai pola yang sama dengan boss-nya sebagai pemanasan.
- Fase 2 bisa terasa seperti lonjakan kesulitan yang tidak adil. Mitigasi: fase 2 hanya menambah satu elemen baru.

**6. Cara pengujian.**

- Telemetri: rasio menang boss pada pertemuan pertama. Target: 50–70% untuk run yang mencapai boss tersebut dengan HP party di atas 50%.
- Telemetri penyebab kalah: jika lebih dari 50% kekalahan terjadi tepat pada serangan signature, damage atau Break Power signature ditinjau ulang.
- Survei: "Apakah kamu tahu serangan boss mana yang mengalahkanmu?" Target: minimal 80% menjawab ya.

#### 6.4 Telegraph serangan dan serangan charge

**Keputusan: aksi berbahaya selalu ditelegraph, tetapi telegraph tidak pernah mengubah timing QTE.** Telegraph memberi waktu untuk mengambil keputusan taktis di giliran pemain; QTE tetap menguji eksekusi.

**1. Tujuan desain.** Serangan terbesar musuh harus bisa diantisipasi lewat keputusan (heal, Shield, buff, interrupt), sesuai prinsip bahwa kesulitan datang dari keputusan dan konsekuensi.

**2. Aturan/mekanik.**

- Aksi yang wajib ditelegraph: serangan berisi marker Heavy, serangan signature boss, pelepasan serangan charge, dan aksi status-only (5.3).
- Serangan biasa tanpa Heavy tidak ditelegraph.
- Telegraph muncul sebagai ikon intent di atas musuh, sejak awal round tempat aksi itu akan dilakukan. Isinya:
  - Jenis aksi: Heavy, signature, charge, atau status.
  - Potret target yang akan terkena.
  - Jumlah hit.
- **Serangan charge:**
  - Giliran 1: musuh memakai gilirannya untuk mengisi charge, tanpa damage. Ikon menampilkan hitungan mundur 1.
  - Giliran 2: musuh melepas serangan Heavy yang sudah ditelegraph.
  - Charge bisa dibatalkan lewat interrupt (6.5).
- Telegraph tidak mengubah T\_lead, window, maupun pola.
- Jika target yang ditelegraph sudah Down saat serangan dilepas, serangan diarahkan ke karakter aktif berikutnya dalam urutan party (deterministik, bukan acak).

**3. Interaksi dengan sistem lain.** Turn-based: telegraph di awal round memberi pemain satu atau lebih giliran untuk merespons sebelum musuh bertindak. 6.3: serangan signature boss ditelegraph satu giliran lebih awal lewat mekanik charge. 6.5: charge adalah satu-satunya target interrupt. Survival: telegraph menentukan kapan heal dan Shield paling bernilai, sehingga resource dipakai secara terencana.

**4. Kurva kesulitan dan sumber tantangan.** Telegraph mengubah serangan terbesar menjadi teka-teki keputusan: bertahan, menyembuhkan, atau mencoba interrupt. Pemain yang mengabaikan telegraph menghadapi serangan Heavy tanpa persiapan, dan konsekuensinya jelas berasal dari keputusannya sendiri.

**5. Risiko.**

- Telegraph yang terlalu informatif membuat pertarungan terasa seperti teka-teki yang sudah terpecahkan. Diterima secara terbatas: hanya aksi berbahaya yang ditelegraph, sementara serangan biasa tetap tanpa info.
- Ikon intent untuk banyak musuh bisa memenuhi layar. Mitigasi: ikon hanya muncul untuk aksi yang wajib ditelegraph (Bagian 9 menentukan tampilannya).

**6. Cara pengujian.**

- Telemetri respons: persentase serangan Heavy yang ditelegraph di mana pemain memakai heal, Shield, buff, atau interrupt pada giliran sebelumnya. Target: 50–75%; di atas 90% berarti responsnya terlalu jelas, di bawah 30% berarti telegraph tidak terbaca.
- Uji pemahaman: tester diminta menjelaskan ikon intent tanpa tutorial. Target: minimal 80% benar untuk ikon Heavy dan charge.

#### 6.5 Interrupt dan counter

**Keputusan: interrupt dilakukan dengan membuat musuh yang sedang charge menjadi Broken. Counter milik pemain hanya terpicu oleh Defensive Perfect.** Keduanya mengubah keberhasilan QTE menjadi keuntungan taktis, bukan sekadar angka damage.

**1. Tujuan desain.** Memberi pemain jawaban aktif terhadap ancaman yang ditelegraph, dan memberi Defensive Perfect nilai lebih dari sekadar 0 damage.

**2. Aturan/mekanik.**

**Interrupt**

- Musuh yang sedang charge (6.4) membatalkan charge-nya jika menjadi Broken sebelum serangannya dilepas.
- Break Power dari serangan pemain = Break Power skill × multiplier grade Offensive QTE (Perfect 1.5×, Good 1.0×, Poor 0.5×).
- Charge yang batal hilang. Pada giliran berikutnya, musuh bertindak normal dan tidak melepas serangan Heavy tersebut.
- Serangan biasa tidak bisa di-interrupt, karena tidak punya fase charge.

**Counter**

- Skill counter (Counterattack, Counterstrike, Perfect Counter) hanya terpicu oleh Defensive Perfect pada hit yang mengenai pengguna counter selama counter aktif.
- Maksimal 1 counter per pengguna per aksi musuh, terpicu pada hit Perfect pertama.
- Counter dijalankan setelah aksi musuh selesai.
- Serangan counter tidak memakai Offensive QTE. Multiplier-nya tetap 1.0×, kecuali `qteProfile` skill menyatakan lain (kandidat: Perfect Counter 1.5×).
- Counter tetap terpicu oleh Perfect pada serangan Heavy, walaupun reduksinya hanya 50%.
- Break Power counter dihitung dengan multiplier 1.0×, sehingga counter bisa ikut membantu interrupt.
- Musuh tidak punya counter berbasis QTE. Bahaya dari musuh hanya datang di giliran musuh sendiri.

**3. Interaksi dengan sistem lain.** 3.1: interrupt memakai Break gauge dari asumsi A5. 6.3: Break gauge boss 200 berarti interrupt boss memerlukan kerja sama beberapa karakter dalam satu round. Class System: Counterattack, Counterstrike, dan Perfect Counter kini punya trigger yang jelas. Bagian 7 menentukan apakah counter Martial Artist dihitung sebagai "menyerang" untuk aturan reset combo.

**4. Kurva kesulitan dan sumber tantangan.** Interrupt adalah keputusan berisiko: pemain harus memilih antara bertahan dari serangan charge atau mengalokasikan giliran untuk menumpuk Break Power, dan Poor bisa membuat usaha itu gagal. Counter memberi hadiah pada eksekusi defensif yang konsisten tanpa membuat pertahanan menjadi strategi pasif.

**5. Risiko.**

- Interrupt bisa terlalu mudah jika Break Power skill pemain terlalu tinggi, sehingga serangan charge tidak pernah dilepas. Mitigasi: target telemetri di bawah.
- Counter bisa terlalu kuat dikombinasikan dengan Shared QTE (satu Perfect memicu counter dari beberapa karakter). Diterima dengan batas 1 counter per pengguna per aksi; ditinjau ulang di Fase 3.

**6. Cara pengujian.**

- Telemetri interrupt: persentase serangan charge yang berhasil dibatalkan. Target: 25–45% secara keseluruhan; di atas 60% berarti Break Power pemain terlalu tinggi.
- Telemetri counter: kontribusi damage counter terhadap total damage party. Target: maksimal 15% untuk party yang memakai skill counter.

### 7. Class dan Skill

#### 7.1 Gain combo per skill Martial Artist

**Keputusan: combo dihitung dalam poin bulat, dengan gain dasar 1–2 per skill dan Perfect menambah +1.** Quick Jab adalah builder utama (gain 2); skill lain memberi gain 1 karena nilai utamanya ada pada damage, Break, atau efek taktis.

**1. Tujuan desain.** Martial Artist harus memilih antara membangun combo dengan aman atau langsung memberi damage besar, dengan eksekusi QTE sebagai penentu kecepatan pembangunan.

**2. Aturan/mekanik.**

| Skill | Tier | Gain dasar (Good) | Catatan |
| --- | --- | --- | --- |
| Basic Attack | — | 1 |  |
| Quick Jab | 1 | 2 | Builder utama, damage rendah |
| Rising Kick | 1 | 1 |  |
| Palm Strike | 2 | 1 |  |
| Sweep Kick | 2 | 1 | Gain dihitung sekali per penggunaan, bukan per target |
| Flowing Combo | 2 | 2 | `qteProfile`: Poor membagi dua combo (dibulatkan ke bawah), bukan reset |
| Dragon Fist | 3 | 1 |  |
| Spinning Kick | 3 | 1 | Sekali per penggunaan |
| Counterstrike | 3 | 1 | Lihat aturan counter di bawah |
| Hundred Fists | 4 | Per marker | Lihat 7.2 |
| Flying Knee | 4 | 1 |  |
| Perfect Counter | 4 | 2 |  |
| Ascendant Strike | 5 | 1 |  |
| Dragon's Fury, Final Technique | 5 | — | Finisher, mengonsumsi combo (7.4) |

- Grade: Perfect = gain dasar + 1; Good = gain dasar; Poor = combo reset ke 0 (kecuali Flowing Combo).
- Gain dihitung setelah hasil final QTE, termasuk hasil retry Lucky (8.1).
- Hit yang meleset (Hit Chance gagal) tetap memberi gain sesuai grade QTE, karena combo mengukur eksekusi.
- **Counter:** counter Martial Artist memberi gain sesuai tabel, tetapi tidak menggantikan serangan di giliran sendiri untuk aturan reset "satu giliran tanpa menyerang".
- **Focused Breathing** (tanpa QTE): tidak memberi gain, tetapi juga tidak dihitung sebagai giliran tanpa menyerang. Ini pengecualian per skill.

**3. Interaksi dengan sistem lain.** 2.4: Flowing Combo memakai satu-satunya konsekuensi Poor yang diizinkan per skill. 6.5: counter menjadi sumber combo di luar giliran Martial Artist. Hit Chance: gain tidak hilang saat meleset, agar Martial Artist tidak dihukum dua kali oleh RNG.

**4. Kurva kesulitan dan sumber tantangan.** Membangun 10 combo membutuhkan sekitar 4 giliran dengan Quick Jab Perfect (3 per giliran) atau 5 giliran dengan Good. Satu Poor menghapus semuanya, sehingga setiap QTE terasa penting. Pemain juga harus memutuskan kapan memakai Flowing Combo sebagai jaring pengaman.

**5. Risiko.**

- Quick Jab bisa menjadi satu-satunya skill yang dipakai. Mitigasi: damage Quick Jab rendah, dan finisher hanya bernilai jika Martial Artist juga menyumbang damage selama membangun combo.
- Gain dari hit yang meleset bisa terasa aneh. Diterima, karena konsisten dengan prinsip "QTE terpisah dari Hit Chance".

**6. Cara pengujian.**

- Telemetri: rata-rata giliran untuk mencapai combo 10. Target: 4–6 giliran untuk pemain rata-rata.
- Telemetri pemakaian skill: jika Quick Jab dipakai lebih dari 60% dari semua aksi Martial Artist, damage atau gain-nya ditinjau ulang.

#### 7.2 Gain combo dari skill multi-hit sekuens

**Keputusan: gain dihitung per marker secara berurutan, dan bonus Perfect +1 diberikan sekali per penggunaan skill, hanya jika semua marker Perfect.** Marker Poor mereset combo pada titik itu, lalu marker berikutnya membangun lagi dari 0.

**1. Tujuan desain.** Skill sekuens harus menjadi builder berisiko tinggi: hadiah besar untuk konsistensi, tetapi tanpa membuat bonus Perfect berlipat per marker.

**2. Aturan/mekanik.**

- Gain per marker didefinisikan di `qteProfile` skill; default 1.
- Marker diproses berurutan:
  1. Good atau Perfect → combo bertambah sebesar gain per marker.
  2. Poor → combo langsung menjadi 0, lalu marker berikutnya tetap diproses.
- Jika semua marker Perfect, combo bertambah +1 setelah marker terakhir.
- Cap Maximum Combo (7.3) diterapkan setiap kali combo bertambah.
- **Hundred Fists:** 10 hit dalam 5 marker (2 hit per marker), gain per marker 1. Gain maksimal 6 (5 marker + bonus semua Perfect).
- Contoh: combo awal 4, Hundred Fists dengan hasil Perfect–Good–Poor–Good–Perfect → 5 → 6 → 0 → 1 → 2. Combo akhir 2.

**3. Interaksi dengan sistem lain.** 2.2: grade per marker sudah tersedia dari sistem sekuens. 1.3: Hundred Fists dengan 5 marker berlangsung sekitar 3 detik. 7.1: aturan per skill tetap berlaku, termasuk pengecualian Flowing Combo jika suatu saat menjadi skill sekuens.

**4. Kurva kesulitan dan sumber tantangan.** Hundred Fists bisa mengisi lebih dari separuh combo dalam satu giliran, tetapi lima QTE berturut-turut berarti lima peluang untuk gagal. Poor di marker akhir jauh lebih mahal daripada di marker awal, sehingga fokus pemain diuji sampai akhir.

**5. Risiko.**

- Pemain bisa merasa dicurangi jika Poor di marker ke-5 menghapus combo yang sudah tinggi. Diterima sebagai risiko skill; tooltip Hundred Fists menyebutkannya.
- Hundred Fists bisa terlalu dominan sebagai builder. Mitigasi: biaya MP lebih tinggi dari Quick Jab, ditetapkan saat balancing skill.

**6. Cara pengujian.**

- Unit test urutan proses untuk semua kombinasi grade pada 5 marker (243 kasus). Target: hasil sesuai aturan berurutan.
- Telemetri: rata-rata gain bersih Hundred Fists. Target: 2.5–4 untuk pemain rata-rata; di atas 4.5 berarti skill ini terlalu aman.

#### 7.3 Maximum Combo

**Keputusan: Maximum Combo adalah 10, dan hanya bisa dinaikkan menjadi 12 oleh passive Limitless Potential.** Angka 10 dicapai dalam 4–6 giliran (7.1), sehingga finisher penuh bisa dipakai sekitar dua kali dalam pertarungan boss yang panjang.

**1. Tujuan desain.** Memberi target yang jelas dan bisa dicapai, sambil mencegah combo menumpuk tanpa batas di pertarungan panjang.

**2. Aturan/mekanik.**

- Combo berada di rentang 0–10 (0–12 dengan Limitless Potential).
- Gain yang melebihi batas hilang. Tidak ada overflow atau penyimpanan.
- Combo dimulai dari 0 di setiap battle dan hilang saat battle berakhir.
- Saat combo penuh, Martial Artist tetap bisa menyerang; combo tetap di 10 selama tidak ada Poor, reset, atau finisher.
- Combo ditampilkan sebagai angka di samping potret Martial Artist, dengan penanda khusus saat mencapai batas.

**3. Interaksi dengan sistem lain.** 7.4: threshold finisher dan bonus per poin memakai rentang 0–10. 3.1: reset combo saat Broken membuat combo penuh rawan hilang jika Break gauge Martial Artist tinggi. Accessibility (Bagian 10): Auto-Good membuat combo tidak pernah reset karena QTE; dampaknya dibahas di sana.

**4. Kurva kesulitan dan sumber tantangan.** Combo penuh bukan akhir keputusan: pemain harus memilih kapan melepas finisher. Menahan combo penuh lebih lama menambah bonus damage per poin (7.4), tetapi juga menambah risiko kehilangan semuanya.

**5. Risiko.**

- Pada pertarungan pendek (encounter normal 3–4 round), combo 10 jarang tercapai sehingga finisher terasa lemah. Diterima: Dragon's Fury tetap bisa dipakai mulai combo 3 (7.4).
- Penanda combo penuh bisa terlewat. Mitigasi: tampilan ditentukan di Bagian 9.

**6. Cara pengujian.**

- Telemetri: persentase battle di mana combo mencapai 10. Target: 30–50% di encounter elite dan boss, di bawah 25% di encounter normal.
- Telemetri gain yang hilang karena batas. Jika lebih dari 20% gain total hilang, batas atau gain ditinjau ulang.

#### 7.4 Threshold dan damage scaling Finisher

**Keputusan: dua finisher dengan profil berbeda. Dragon's Fury fleksibel (mulai combo 3, 1 + 0.15 per poin), Final Technique all-in (wajib combo penuh, 1 + 0.2 per poin, sekali per battle).** Selain finisher, setiap poin combo memberi bonus damage pasif kecil.

**1. Tujuan desain.** Mengubah combo menjadi keputusan timing taktis: lepas sekarang untuk damage pasti, atau tahan untuk damage lebih besar dengan risiko kehilangan semuanya.

**2. Aturan/mekanik.**

- **Bonus pasif combo:** setiap poin memberi +2% Physical Damage pada serangan non-finisher Martial Artist (combo 10 = +20%). Diterapkan sebagai Damage Bonus di step 2 Bab 11 §7.1 (multiplicative).
- **Dragon's Fury:** bisa dipakai pada combo ≥ 3. Multiplier finisher F = 1 + 0.15 × combo.
- **Final Technique:** hanya bisa dipakai saat combo penuh (10, atau 12 dengan Limitless Potential). F = 1 + 0.2 × combo. Maksimal sekali per battle.
- F diterapkan sebagai Damage Bonus di step 2, lalu QTE multiplier di step 3 seperti biasa.

| Combo | Dragon's Fury (F) | Final Technique (F) |
| --- | --- | --- |
| 3 | 1.45× | — |
| 5 | 1.75× | — |
| 10 | 2.50× | 3.00× |
| 12 | 2.80× | 3.40× |

- Contoh: base damage 100, Dragon's Fury pada combo 10 → Perfect 100 × 2.5 × 1.5 = 375; Good 250; Poor 125.
- Finisher selalu mengonsumsi seluruh combo setelah hasil QTE final (termasuk retry Lucky), apa pun grade-nya dan walaupun serangannya meleset.
- Finisher tidak pernah memberi gain combo dan tidak bisa Critical (revisi Fase 3).

**3. Interaksi dengan sistem lain.** Damage calculation: F dan bonus pasif adalah Damage Bonus multiplicative, sehingga tetap tunduk pada Defense dan affinity sesuai urutan yang ada (finisher dikecualikan dari Critical). 7.3: tabel memakai rentang 0–12. 3.1: Broken mereset combo, sehingga menahan combo penuh saat Break gauge tinggi adalah keputusan berisiko.

**4. Kurva kesulitan dan sumber tantangan.** Finisher punya taruhan ganda: combo yang dibangun beberapa giliran dipertaruhkan pada satu QTE, dan Poor membuat hasilnya hanya sepertiga dari Perfect. Ini sengaja menjadikan finisher momen paling menegangkan bagi Martial Artist.

**5. Risiko.**

- Final Technique dengan Perfect dan Critical bisa memberi lonjakan damage sangat besar (100 × 3.4 × 1.5 × 3.0 Critical maksimal = 1530). Mitigasi (Fase 3): finisher tidak bisa Critical, sehingga hasil maksimalnya 100 × 3.4 × 1.5 = 510.
- Pemain bisa merasa Poor pada finisher terlalu menghukum. Diterima sebagai identitas class; Lucky dan modifier window adalah alat mitigasi.

**6. Cara pengujian.**

- Simulasi DPS: Martial Artist yang membangun ke 10 lalu Dragon's Fury dibanding yang selalu memakai skill damage biasa, selama 10 giliran dengan distribusi grade rata-rata. Target: jalur combo unggul 10–25%.
- Telemetri: rata-rata combo saat Dragon's Fury dipakai. Target: 5–8; jika selalu di 10, threshold minimum 3 tidak bermakna dan perlu ditinjau.

#### 7.5 Interaksi QTE dengan passive dan talent

**Keputusan: hanya Martial Artist yang punya passive dan talent yang mengubah aturan combo. Class lain hanya boleh menyentuh QTE lewat dua jalur: modifier window (3.4) dan maksimal satu efek yang dipicu Perfect per class tree (2.1).** QTE tetap menjadi identitas inti Martial Artist, bukan fitur yang tersebar rata.

**1. Tujuan desain.** Memberi build "QTE specialist" Martial Artist pilihan nyata, tanpa membuat class lain bergantung pada eksekusi QTE untuk berfungsi.

**2. Aturan/mekanik.**

Passive Martial Artist (3 slot passive per karakter):

| Passive | Tier | Efek QTE/combo |
| --- | --- | --- |
| Combat Rhythm | 1 | Gain Basic Attack dan Quick Jab +1 |
| Combo Mastery | 2 | Bonus pasif per poin combo +2% → +3% |
| Agile Defense | 2 | Window defensif +10% (modifier 3.4) |
| Inner Focus | 3 | Window ofensif +10% saat combo ≥ 5 |
| Unbroken Flow | 4 | Sekali per battle, Poor membagi dua combo alih-alih reset |
| Limitless Potential | 5 | Maximum Combo 12; bonus Perfect +1 → +2 |
| Martial Discipline, Pressure Point, Ki Control, Grandmaster | — | Tidak ada interaksi QTE |

Talent class Martial Artist (nama dari Bab 9 §5.1):

| Talent | Efek |
| --- | --- |
| Flowing Strikes | Combo dimulai dari 2 di awal setiap battle |
| Perfect Timing | Window ofensif dan defensif +10% |
| Combo Discipline | Reset karena tidak menyerang baru terjadi setelah 2 giliran, bukan 1 |
| Finisher Mastery | Koefisien F per poin +0.03 (Dragon's Fury 0.18, Final Technique 0.23) |
| Unbroken Rhythm | Sekali per battle, combo tidak reset saat Broken |

Class lain (kandidat, mengikuti dua jalur yang diizinkan):

| Class | Talent | Efek |
| --- | --- | --- |
| Fighter | Guard Specialist | Window defensif +10% |
| Ranger | Deadly Precision | Window ofensif +10% untuk serangan ranged |
| Class lain | — | Tidak ada interaksi QTE khusus |

- Semua efek window ditumpuk dengan sumber lain di bawah cap +25% (3.4). Contoh: Perfect Timing + Agile Defense + Inner Focus = 30% → di-clamp ke 25%.
- Efek "sekali per battle" dari passive dan talent yang berbeda tidak saling menggantikan; masing-masing punya hitungan sendiri.

**3. Interaksi dengan sistem lain.** 3.4: cap +25% membuat menumpuk semua efek window tidak efisien, sehingga slot passive lebih baik dipakai untuk efek combo. Talents System: nama talent masih berstatus usulan, jadi efek di atas menjadi usulan resmi untuk dikonfirmasi. 7.3 dan 7.4: Limitless Potential dan Finisher Mastery mengubah tabel finisher.

**4. Kurva kesulitan dan sumber tantangan.** Pemain Martial Artist memilih antara build aman (window lebih lebar, perlindungan dari reset) dan build agresif (gain lebih cepat, finisher lebih kuat). Dengan hanya 3 slot passive, tidak ada build yang bisa mendapatkan semuanya.

**5. Risiko.**

- Kombinasi Unbroken Flow, Unbroken Rhythm, dan Combo Discipline bisa membuat combo hampir mustahil hilang. Mitigasi: dua di antaranya talent dan satu passive, sehingga ketiganya hanya bisa digabung lewat pilihan talent yang sangat spesifik; selain itu, dua di antaranya hanya aktif sekali per battle; ditinjau di Fase 3.
- Class lain bisa merasa QTE "bukan urusan mereka". Diterima: QTE tetap memengaruhi damage semua class, hanya aturannya yang tidak dikustomisasi.

**6. Cara pengujian.**

- Telemetri pick rate passive Martial Artist. Target: tidak ada passive yang dipilih oleh lebih dari 70% atau kurang dari 10% pemain Martial Artist.
- Uji build ekstrem: build "tidak bisa reset" dan build "finisher maksimal" masing-masing dimainkan pada 5 boss. Target: selisih DPS kedua build maksimal 20%.

### 8. Talent, Equipment, Atribut, dan Race

#### 8.1 Retry Lucky pada Offensive QTE

**Keputusan: retry ofensif memakai alur yang sama dengan defensif. Animasi berhenti di frame ancang-ancang, bar diulang dengan window dan T\_lead yang sama, dan hasil retry adalah hasil final.** Semua efek yang bergantung pada grade (damage, combo, Break Power) dihitung dari hasil final itu.

**1. Tujuan desain.** Lucky memberi kesempatan kedua yang tetap harus dieksekusi pemain, dengan alur yang identik di serangan dan pertahanan agar mudah dipahami.

**2. Aturan/mekanik.**

- Pemicu: hasil QTE Poor, lalu roll 20% (aturan Lucky yang sudah ada: maksimal 1 kali per QTE, 3 kali per battle).
- Alur retry:
  1. Animasi serangan berhenti di frame ancang-ancang.
  2. Teks "Lucky!" tampil selama 400 ms.
  3. Bar diulang dari awal dengan T\_lead, W\_P, dan W\_G yang sama.
  4. Hasil retry menggantikan hasil awal, termasuk jika hasilnya Poor lagi.
- Pada QTE sekuens, hanya marker yang gagal yang diulang. Setelah retry, sekuens berlanjut dari marker berikutnya.
- Damage, gain combo, reset combo, konsumsi combo finisher, dan Break Power semuanya dihitung dari hasil final. Contoh: Martial Artist yang mendapat Poor lalu retry Good tidak kehilangan combo.
- **Shared QTE defensif:** jika ada lebih dari satu target dengan Lucky, roll dilakukan sekali memakai kuota Lucky karakter pertama dalam urutan party; hasil retry berlaku untuk semua target.
- Retry menambah sekitar 2 detik (400 ms + durasi satu QTE).

**3. Interaksi dengan sistem lain.** 1.3: retry dikecualikan dari target durasi per round karena jarang terjadi (maksimal 3 kali per battle per karakter). 7.1–7.4: aturan "dihitung dari hasil final" melindungi combo Martial Artist. 3.4: Lucky bukan modifier window dan tidak dihitung dalam cap +25%.

**4. Kurva kesulitan dan sumber tantangan.** Lucky tidak menghilangkan tantangan; ia hanya memberi satu kesempatan tambahan dengan kesulitan yang sama. Pemain yang gagal dua kali tetap menanggung konsekuensinya.

**5. Risiko.**

- Retry di tengah sekuens bisa memecah ritme pemain. Diterima; jeda 400 ms memberi waktu untuk menyesuaikan diri.
- Aturan Shared QTE (kuota karakter pertama) bisa menghabiskan kuota karakter yang tidak diinginkan pemain. Diterima karena urutan party bisa diatur pemain di luar battle.

**6. Cara pengujian.**

- Uji fungsional: retry pada QTE tunggal, marker sekuens ke-3, finisher Martial Artist, dan Shared QTE. Target: semua efek dihitung dari hasil final.
- Telemetri: rasio keberhasilan retry dibanding rasio Good-atau-lebih-baik normal pemain yang sama. Target: selisih maksimal 10 poin persentase, sebagai bukti retry tidak lebih mudah atau lebih sulit.

#### 8.2 Atribut karakter dan QTE

**Keputusan: tidak ada atribut (STR, DEX, VIT, INT, WIS, CHA) yang memengaruhi QTE.** QTE mengukur keterampilan pemain; atribut mengukur kekuatan karakter. Keduanya dipisahkan agar sumber keberhasilan selalu jelas.

**1. Tujuan desain.** Mencegah QTE menjadi stat yang bisa dibeli lewat Point Buy, sekaligus mencegah atribut tertentu (terutama DEX) menjadi pilihan wajib bagi semua build.

**2. Aturan/mekanik.**

- Window, T\_lead, grade, dan multiplier QTE tidak membaca atribut apa pun.
- DEX tetap memengaruhi Accuracy dan Evasion (Character Creation System), yang terpisah dari QTE.
- Satu-satunya cara karakter memengaruhi QTE adalah lewat modifier window dari equipment, talent, dan passive (3.4), dengan cap +25%.
- Pengaturan accessibility (Bagian 10) bukan atribut dan tidak dihitung dalam cap.

**3. Interaksi dengan sistem lain.** Character Creation: Point Buy tetap fokus pada damage, HP, dan stat turunan lainnya. Hit Chance: prinsip "QTE terpisah dari Hit Chance" diperluas menjadi "QTE terpisah dari atribut". 3.4: jalur modifier tetap satu, sehingga balancing tetap terpusat.

**4. Kurva kesulitan dan sumber tantangan.** Kesulitan QTE untuk karakter level 1 dan level 15 sama di tier dan dungeon yang sama. Peningkatan karakter memengaruhi seberapa besar hasil QTE (lewat damage dan HP), bukan seberapa mudah QTE dieksekusi.

**5. Risiko.**

- Pemain mengharapkan DEX membantu timing karena tema "agility". Mitigasi: tooltip DEX menyebutkan cakupannya secara eksplisit.
- Tidak ada progresi QTE dari leveling bisa terasa datar. Diterima, karena progresi sudah datang dari keterampilan pemain dan dari equipment/talent.

**6. Cara pengujian.**

- Uji regresi: QTE dijalankan dengan dua karakter yang atributnya ekstrem berbeda. Target: window dan T\_lead identik.
- Survei: "Apa yang membuat QTE-mu lebih mudah?" Target: tidak ada lebih dari 10% tester yang menyebut atribut.

#### 8.3 Equipment yang memodifikasi QTE

**Keputusan: equipment memodifikasi QTE lewat dua affix baru (Offensive QTE Window dan Defensive QTE Window), plus efek Legendary/Mythic yang dibatasi pada dua jalur yang diizinkan.** Semuanya tunduk pada cap +25% dari 3.4.

**1. Tujuan desain.** Memberi pilihan loot yang bermakna untuk pemain yang ingin eksekusi lebih nyaman, dengan harga berupa slot affix yang seharusnya bisa diisi stat damage atau defense.

**2. Aturan/mekanik.**

Affix baru (kategori "QTE", tambahan untuk tabel kategori affix di Bab 13 §6.1):

| Affix | Valid Equipment Type | Uncommon | Rare | Epic | Legendary | Mythic |
| --- | --- | --- | --- | --- | --- | --- |
| Offensive QTE Window | Main Hand, Second Hand (off-hand weapon atau special item), Accessory | 3–5% | 4–6% | 5–8% | 6–9% | 7–10% |
| Defensive QTE Window | Second Hand (shield), Foot, Accessory | 3–5% | 4–6% | 5–8% | 6–9% | 7–10% |

- Maksimal 1 affix QTE per item.
- Weight affix QTE setengah dari weight affix standar, agar tidak terlalu sering muncul.
- Common tidak punya affix, sehingga tidak bisa memiliki affix QTE.
- Efek Legendary dan Mythic yang menyentuh QTE hanya boleh berupa:
  1. Modifier window hingga 12%.
  2. Satu efek yang dipicu Perfect, misalnya "Perfect pada Offensive QTE memulihkan 2% Max MP".
- Dilarang pada equipment: mengubah R\_QTE, mengubah batas Heavy, memberi grade otomatis, atau memengaruhi Lucky.
- Total modifier window dari semua equipment, talent, dan passive tetap di-clamp ke +25%.

**3. Interaksi dengan sistem lain.** Equipment System: affix QTE bersaing langsung dengan affix damage, defense, dan resistance dalam jumlah affix yang tetap per rarity. 3.4: cap +25% bisa dicapai hanya dengan equipment (misalnya dua Accessory dan satu Foot; affix QTE sengaja hanya valid di sebagian dari 8 slot agar cap tidak terlalu mudah dicapai), sehingga passive dan talent window menjadi kurang efisien jika equipment sudah banyak. Survival: efek Perfect yang memulihkan MP menghubungkan eksekusi dengan ekonomi resource.

**4. Kurva kesulitan dan sumber tantangan.** Pemain memutuskan berapa banyak slot affix yang dikorbankan untuk kenyamanan eksekusi. Karena affix QTE jarang muncul, build window penuh membutuhkan keberuntungan loot dan pengorbanan stat yang nyata.

**5. Risiko.**

- Pemain mencapai cap hanya dari equipment, lalu passive window (Agile Defense, Inner Focus) menjadi tidak berguna. Diterima sebagai pilihan build; tooltip menampilkan total window efektif dan cap.
- Efek Legendary yang dipicu Perfect bisa menjadi terlalu kuat jika memulihkan resource dalam jumlah besar. Mitigasi (Fase 3): efek pemulihan resource dari Perfect dibatasi maksimal 2% Max HP atau Max MP per pemicu, dan maksimal satu pemicu per giliran karakter.

**6. Cara pengujian.**

- Simulasi loot: 10.000 drop per rarity. Target: affix QTE muncul di 8–15% item yang punya affix.
- Telemetri build: persentase pemain yang mencapai cap +25% di dungeon 7–8. Target: 15–35%; di atas 50% berarti affix QTE terlalu mudah didapat.

#### 8.4 Talent lain yang memberi bonus QTE

**Keputusan: dari daftar Shared Talent yang masih berupa usulan, hanya dua yang menyentuh QTE: Quick Reflex (window defensif +10%) dan Steady Hand (window ofensif +10%).** Lucky tetap satu-satunya talent yang memberi retry.

**1. Tujuan desain.** Memberi semua class akses ke bantuan eksekusi lewat talent, tanpa menyaingi talent khusus Martial Artist (Perfect Timing) yang mencakup kedua arah sekaligus.

**2. Aturan/mekanik.**

| Talent | Efek QTE | Catatan |
| --- | --- | --- |
| Quick Reflex | Window defensif +10% | Modifier 3.4 |
| Steady Hand | Window ofensif +10% | Modifier 3.4 |
| Lucky | 20% retry saat Poor | Aturan yang sudah ada + 8.1 |
| Vitality, Battle Rhythm, Iron Will, Efficient Casting, Fortune Seeker, Keen Eye, Survivor's Instinct | Tidak ada | Efek non-QTE ditetapkan di Talents System |
| Alert, Healer, Durable, Keen Mind, Arcane Reserve, Poisoner, Crusher, Slasher, Piercer, Inspiring Leader | Tidak ada | Durable dan Inspiring Leader berinteraksi lewat Damage Reduction (5.1) |

- Quick Reflex dan Steady Hand ditumpuk dengan sumber window lain di bawah cap +25%.
- Talent hanya boleh menyentuh QTE lewat modifier window atau retry; tidak ada talent yang mengubah R\_QTE atau grade.

**3. Interaksi dengan sistem lain.** 7.5: Perfect Timing (Martial Artist) setara dengan Quick Reflex + Steady Hand dalam satu talent, sebagai keunggulan identitas class. 8.3: talent window bersaing dengan affix equipment dalam mencapai cap. Talents System: Battle Rhythm dan Keen Eye tidak diberi efek QTE agar daftar talent QTE tetap pendek dan mudah dipahami.

**4. Kurva kesulitan dan sumber tantangan.** Talent window adalah pilihan untuk pemain yang kesulitan dengan eksekusi, dengan harga berupa talent lain yang tidak diambil (misalnya Durable atau Healer). Pemain yang mahir mendapat nilai lebih dari talent non-QTE.

**5. Risiko.**

- Nama "Battle Rhythm" terdengar seperti talent QTE, sehingga pemain bisa salah paham. Mitigasi: pertimbangkan mengganti nama talent itu saat efeknya ditetapkan.
- Quick Reflex bisa menjadi pilihan wajib di dungeon akhir. Mitigasi: telemetri pick rate di bawah.

**6. Cara pengujian.**

- Telemetri pick rate Quick Reflex dan Steady Hand per kelompok dungeon. Target: masing-masing di bawah 40% dari semua penawaran talent yang memuatnya.
- Uji perbandingan: tester yang sama dengan dan tanpa Quick Reflex di dungeon 7–8. Target: rasio Perfect naik 5–12 poin persentase.

#### 8.5 Racial Feat dan QTE (tambahan)

**Tambahan di luar checklist**, berdasarkan PRD Race System v1.0. Empat Racial Feat menyentuh QTE dengan istilah yang belum ada di spesifikasi ini.

**Keputusan: Racial Feat memakai jalur QTE yang sudah ada (retry 8.1, modifier window 3.4, counter 6.5, Damage Reduction 5.1, Break Power 3.1), tanpa mekanik QTE baru.** Istilah PRD yang belum terdefinisi diterjemahkan ke jalur tersebut, dan penerjemahannya sudah dimasukkan ke Bab 6 (Race System).

**1. Tujuan desain.** Memberi identitas ras yang terasa dalam eksekusi QTE tanpa menambah sistem paralel yang harus dibalance terpisah.

**2. Aturan/mekanik.**

| Racial Feat | Efek QTE di PRD | Penerjemahan ke sistem QTE |
| --- | --- | --- |
| Second Chance (Halfling) | Mengulang QTE yang gagal, 2× per battle (Bab 6 (Race System)) | Alur retry 8.1, untuk Poor dari input pemain, ofensif maupun defensif. Ditawarkan lewat prompt 1000 ms saat animasi di-pause: tekan tombol QTE untuk menerima, diam berarti menolak. Opsi menu "Second Chance otomatis" melewati prompt |
| Draconic Overdrive (Dragonborn) | Bonus "QTE Counter Power" | Selama Overdrive, multiplier counter (6.5) naik dari 1.0× menjadi 1.25×. Window dan grade tidak berubah. Fase Exhausted tidak memengaruhi QTE |
| Ocean's Embrace (Merfolk) | Bonus "QTE Stability" untuk target | Window defensif target +10% selama Shield aktif, dihitung dalam cap +25% |
| Iron Resolve (Dwarf) | Defensive Reaction, mengurangi damage, Stagger Resistance | Diaktifkan sebagai aksi di giliran Dwarf, berlaku sampai awal giliran berikutnya. Pengurangan damage = All Damage Taken −30% (multiplicative, sebelum Defensive QTE). "Stagger Resistance" = Break Power yang diterima dikurangi sebelum faktor (1 − R\_QTE); sebesar 50%; cooldown 3 giliran Dwarf |
| Fey Step (Elf) | Reposition untuk menghindari bahaya | Tidak mengubah QTE. Fey Step memberi status Phased: Elf tidak bisa menjadi target aksi single-target musuh sampai giliran berikutnya. Serangan yang ditelegraph ke Elf dialihkan ke karakter aktif berikutnya (aturan 6.4), bukan dibatalkan |
| Determination (Human), Lightbound (Liichtian) | — | Tidak ada interaksi QTE |

- Satu QTE maksimal di-retry satu kali dari sumber mana pun. Lucky di-roll lebih dulu; Second Chance hanya ditawarkan jika Lucky tidak aktif.
- Racial Feat tidak memakai tombol QTE saat bar berjalan. Semua aktivasi terjadi di giliran pemilik feat, kecuali penerimaan prompt Second Chance.
- Auto-Poor (Stun, Sleep, Mind Control, Down) tidak bisa di-retry, dan saat Auto-Good aktif Second Chance tidak pernah ditawarkan.

**3. Interaksi dengan sistem lain.** 8.1: alur retry dan kuota berlaku sama untuk Second Chance, kecuali pada Shared QTE: hasil retry Second Chance hanya berlaku untuk Halfling, karena Second Chance tidak boleh mengulang QTE milik karakter lain. 3.4: Ocean's Embrace bersaing dengan equipment, talent, dan passive dalam cap +25%. 6.5: Overdrive memperkuat build counter Dragonborn tanpa membuat counter lebih mudah terpicu. Class System: Iron Resolve tetap lebih lemah dari skill defensif Fighter, karena memakai giliran Dwarf dan hanya memotong Break Power, tidak menghapusnya.

**4. Kurva kesulitan dan sumber tantangan.** Ras mengubah seberapa aman eksekusi pemain, bukan angka tantangan musuh. Halfling menjadi ras paling pemaaf untuk QTE, sedangkan Dwarf dan Merfolk memberi lapisan pengaman defensif yang membutuhkan keputusan kapan dipakai.

**5. Risiko.**

- Halfling dengan Lucky dan modifier window bisa terlalu pemaaf. Mitigasi: maksimal satu retry per QTE, sehingga total retry per battle paling banyak 5 (3 Lucky + 2 Second Chance).
- Pemain bisa memakai Fey Step untuk mengalihkan serangan Heavy ke karakter tank. Diterima sebagai keputusan taktis yang sah.
- Prompt Second Chance 1000 ms menambah jeda. Mitigasi: opsi otomatis di menu.

**6. Cara pengujian.**

- Telemetri Halfling dengan Lucky dibanding ras lain dengan Lucky. Target: rasio Poor final lebih rendah 3–8 poin persentase; lebih dari 10 poin berarti terlalu pemaaf.
- Telemetri Overdrive: kontribusi damage counter Dragonborn selama Overdrive. Target: tetap di bawah batas 15% dari 6.5 secara keseluruhan battle.
- Uji prompt Second Chance: persentase prompt yang terlewat tanpa sengaja. Target: di bawah 10%.

### 9. UI, VFX, dan Audio Feedback

#### 9.1 Animasi Perfect, Good, dan Poor

**Keputusan: setiap grade punya "rasa" impact yang berbeda lewat hit-stop, flash, dan pose, dan semuanya muat dalam 350 ms tampilan hasil (1.3).** Grade harus bisa dibedakan dari animasi saja, tanpa membaca teks.

**1. Tujuan desain.** Memberi kepuasan fisik pada Perfect dan rasa "meleset" yang jelas pada Poor, tanpa memperlambat combat.

**2. Aturan/mekanik.**

| Grade | Ofensif (penyerang) | Defensif (karakter pemain) | Hit-stop |
| --- | --- | --- | --- |
| Perfect | Flash putih pada impact, camera shake 4 px, pose follow-through | Pose parry dengan percikan, tanpa reaksi terkena | 80 ms |
| Good | Impact standar | Pose guard, reaksi terkena kecil | 40 ms |
| Poor | Impact VFX diperkecil 50%, frame tersandung 150 ms | Reaksi terkena penuh | 0 ms |

- Hit-stop dihitung di dalam 350 ms tampilan hasil, sehingga tidak menambah durasi.
- Serangan Heavy dengan Perfect memakai pose parry yang sama, ditambah efek dorongan mundur 8 px untuk menunjukkan damage 50% tetap masuk.
- Camera shake dimatikan otomatis jika opsi "Kurangi gerakan layar" aktif (Bagian 10).
- Animasi mengikuti jam QTE (1.1): frame impact dipicu oleh hasil penilaian, bukan sebaliknya.

**3. Interaksi dengan sistem lain.** 1.3: anggaran 350 ms tetap. 9.8: label hasil dan damage number muncul bersamaan dengan frame impact. Visual Style Guide: efek mengikuti fase anticipation, impact, cleanup yang sudah ditetapkan.

**4. Kurva kesulitan dan sumber tantangan.** Tidak menambah tantangan; animasi adalah umpan balik. Kontras yang jelas antar grade membantu pemain belajar lebih cepat dari kesalahannya.

**5. Risiko.**

- Hit-stop 80 ms pada setiap Perfect di sekuens 6 marker bisa terasa tersendat. Mitigasi: dalam sekuens, hit-stop hanya diterapkan pada marker terakhir.
- Camera shake bisa mengganggu pemain yang sensitif. Mitigasi: opsi di Bagian 10.

**6. Cara pengujian.**

- Uji tanpa teks: label grade disembunyikan, tester menebak grade dari animasi selama 30 QTE. Target: akurasi minimal 90%.
- Pengukuran durasi: rekam 100 QTE dan pastikan tidak ada animasi hasil yang melebihi 350 ms.

#### 9.2 SFX setiap hasil QTE

**Keputusan: tiga keluarga SFX yang dibedakan oleh nada dan bentuk suara, dengan varian ofensif dan defensif.** Grade harus bisa dikenali dari suara saja, sejalan dengan cue audio tiga ketukan di 1.1.

**1. Tujuan desain.** Pemain yang matanya sedang melihat karakter, bukan bar, tetap tahu hasil QTE-nya dari suara.

**2. Aturan/mekanik.**

| Grade | Ofensif | Defensif | Karakter suara |
| --- | --- | --- | --- |
| Perfect | Impact + lapisan "ting" bernada tinggi | Clang parry logam | Terang, attack tajam, nada tinggi |
| Good | Impact standar | Thud blok | Netral, nada tengah |
| Poor | Impact teredam | Impact daging/terkena | Tumpul, nada rendah, tanpa lapisan |
| Heavy (Perfect) | — | Clang parry + dentum rendah | Menandakan damage tetap masuk |

- SFX hasil diputar tepat di frame impact (9.1).
- Saat Perfect, musik diturunkan 6 dB selama 300 ms agar SFX terdengar jelas.
- Dalam sekuens, setiap marker memainkan SFX-nya sendiri; lapisan "ting" Perfect dinaikkan nadanya satu semitone per Perfect berturut-turut (maksimal 5), sebagai hadiah ritme.
- Volume SFX QTE punya slider terpisah dari SFX umum (10.6).
- Tick cue (1.1) memakai suara yang berbeda dari ketiga keluarga ini agar tidak tertukar.

**3. Interaksi dengan sistem lain.** 1.1: tick cue dan SFX hasil membentuk ritme lengkap: tick, tick, tekan, hasil. GDD Visual Direction: SFX QTE success, perfect, dan fail sudah tercantum di daftar kebutuhan audio. 10.6: cue audio alternatif dibangun di atas keluarga suara ini.

**4. Kurva kesulitan dan sumber tantangan.** Tidak menambah tantangan. Nada yang naik pada Perfect beruntun memberi target tidak langsung bagi pemain mahir tanpa mengubah aturan.

**5. Risiko.**

- SFX Poor yang terlalu keras atau mengganggu bisa membuat pemain frustrasi di sesi panjang. Mitigasi: Poor sengaja dibuat tumpul dan pendek, bukan suara "error" yang tajam.
- Banyak SFX pada Shared QTE 4 target bisa bertumpuk. Mitigasi: Shared QTE memutar satu SFX hasil, bukan satu per target.

**6. Cara pengujian.**

- Uji audio-only: layar ditutup, tester menyebutkan grade dari suara selama 30 QTE. Target: akurasi minimal 85%.
- Survei kelelahan audio setelah 60 menit. Target: kurang dari 20% tester menyebut SFX QTE mengganggu.

#### 9.3 Visual feedback Defensive QTE

**Keputusan: bar defensif dibedakan dari bar ofensif lewat bentuk dan ikon, bukan hanya warna, dan hasilnya selalu menampilkan persentase reduksi.** Pemain harus langsung tahu apakah ia sedang menyerang atau bertahan, dan berapa damage yang berhasil ditahan.

**1. Tujuan desain.** Menghilangkan kebingungan arah QTE, terutama pada round dengan banyak aksi berurutan.

**2. Aturan/mekanik.**

- Bar defensif: ikon perisai di ujung kiri, bingkai bersudut tajam. Bar ofensif: ikon pedang di ujung kiri, bingkai membulat.
- Header bar defensif menampilkan potret target dan nama serangan musuh.
- Saat bar defensif muncul, potret target di HUD party berkedip satu kali (200 ms).
- Setelah dinilai, label hasil tampil di atas karakter yang bertahan dengan teks reduksi: "PERFECT −100%", "GOOD −50%", "POOR −0%". Untuk Heavy Perfect: "PERFECT −50%".
- Saat target tunggal sedang Broken, zona Perfect pada bar defensif disembunyikan (pada Shared QTE, zona tetap tampil dan karakter Broken ditandai di header, lihat 9.4) dan diganti ikon retak di tempat zona itu berada (3.1).

**3. Interaksi dengan sistem lain.** 1.1: bentuk dasar bar tetap sama; yang berbeda hanya ikon dan bingkai. 9.8: label hasil mengikuti aturan penempatan umum. 3.1 dan 9.6: tampilan Broken dan Heavy dibangun di atas bar ini.

**4. Kurva kesulitan dan sumber tantangan.** Tidak menambah tantangan. Menampilkan persentase reduksi membuat dampak setiap grade terbaca secara numerik, sehingga pemain bisa menilai seberapa penting eksekusinya.

**5. Risiko.**

- Teks reduksi menambah kepadatan informasi di sekitar karakter. Mitigasi: teks reduksi berukuran 70% dari label grade dan hilang bersama label.
- Perbedaan ikon pedang dan perisai kecil di layar kecil. Mitigasi: bingkai yang berbeda bentuk menjadi pembeda kedua.

**6. Cara pengujian.**

- Uji arah: bar ofensif dan defensif ditampilkan acak selama 1 detik, tester menyebutkan arahnya. Target: akurasi minimal 95%.
- Uji pemahaman: tester ditanya berapa damage yang ditahan setelah Good. Target: minimal 90% menjawab 50%.

#### 9.4 Visual feedback Shared QTE

**Keputusan: header bar Shared menampilkan semua potret target, dan target yang tidak bisa bereaksi sudah ditandai sebelum bar berjalan.** Setelah dinilai, setiap target menerima badge hasil masing-masing.

**1. Tujuan desain.** Pemain harus tahu siapa saja yang dipertaruhkan oleh satu tekanan, dan siapa yang tidak terlindungi apa pun hasilnya.

**2. Aturan/mekanik.**

- Header menampilkan 2–4 potret target dalam urutan party, dengan label "×2", "×3", atau "×4".
- Target Stun ditampilkan dengan potret abu-abu dan ikon gembok sejak bar muncul (9.7).
- Target Broken ditampilkan dengan ikon retak; hasil terbaiknya Good (3.1).
- Setelah dinilai, badge hasil muncul di atas setiap target. Badge bisa berbeda antar target (misalnya Perfect untuk yang bebas, Good untuk yang Broken, Poor untuk yang Stun).
- Satu SFX hasil diputar untuk seluruh Shared QTE (9.2), memakai grade dari tekanan pemain.

**3. Interaksi dengan sistem lain.** 3.2: aturan Shared QTE. 3.1 dan 3.3: kondisi Broken dan Stun tampil berbeda per target. 9.8: badge hasil mengikuti aturan penempatan umum di atas tiap karakter.

**4. Kurva kesulitan dan sumber tantangan.** Melihat potret yang dipertaruhkan menambah tekanan psikologis pada serangan multi-target, sesuai identitas hardcore, tanpa menambah tantangan mekanis.

**5. Risiko.**

- Empat potret di header bisa membuat bar terlalu lebar. Mitigasi: potret di header berukuran 32 px; ukuran bar tetap 25% lebar layar.
- Badge berbeda antar target bisa membingungkan. Mitigasi: ikon gembok dan retak yang tampil sebelum bar berjalan menjelaskan alasannya.

**6. Cara pengujian.**

- Uji pemahaman: Shared QTE 3 target dengan satu target Stun. Target: minimal 85% tester bisa menjelaskan mengapa satu target mendapat Poor.
- Uji keterbacaan: 4 potret di header pada layar 1280×800. Target: semua potret bisa dikenali oleh minimal 90% tester.

#### 9.5 Visual feedback multi-hit dan sekuens

**Keputusan: marker diberi nomor, setiap marker yang sudah dinilai berubah menjadi glyph grade, dan ringkasan tampil di ujung bar setelah marker terakhir.** Pemain bisa melihat posisinya dalam rangkaian dan hasil sebelumnya tanpa mengalihkan mata dari bar.

**1. Tujuan desain.** Menjaga orientasi pemain selama rangkaian sampai 6 marker, terutama pada pola yang tidak teratur.

**2. Aturan/mekanik.**

- Setiap marker diberi nomor kecil (1–6) di atasnya.
- Marker aktif ditandai dengan garis tebal; marker berikutnya tampil redup (4.1).
- Setelah dinilai, marker berubah menjadi glyph grade: bintang (Perfect), lingkaran (Good), silang (Poor).
- Setelah marker terakhir, ringkasan tampil di ujung kanan bar selama 350 ms, misalnya "★3 ●2 ✕1".
- Ofensif: damage number per marker ditumpuk ke atas di atas target (9.8).
- Defensif: label reduksi per hit tidak ditampilkan; hanya label grade per marker di bar, ditambah total damage yang diterima setelah marker terakhir.
- Martial Artist: penghitung combo diperbarui setiap marker (7.2, 9.10).

**3. Interaksi dengan sistem lain.** 4.1 dan 4.2: marker Heavy di akhir tetap memakai penanda Heavy (9.6). 6.2: nomor marker membantu membaca pola Delayed dan Syncopated. 8.1: marker yang di-retry Lucky menampilkan ikon semanggi setelah dinilai ulang.

**4. Kurva kesulitan dan sumber tantangan.** Tidak menambah tantangan. Glyph per marker membantu pemain mengenali di bagian mana pola ia sering gagal, yang mempercepat belajar.

**5. Risiko.**

- Total damage defensif tanpa rincian per hit bisa menyembunyikan informasi. Diterima: rincian tersedia di log battle (fitur UI terpisah, di luar cakupan dokumen ini).
- Glyph kecil sulit dibaca. Mitigasi: glyph berukuran minimal 16 px pada 1080p dan memakai bentuk yang berbeda (10.8).

**6. Cara pengujian.**

- Uji orientasi: setelah sekuens 6 marker, tester ditanya marker mana yang gagal. Target: minimal 80% benar.
- Uji keterbacaan glyph di layar 1280×800. Target: minimal 95% glyph dikenali dengan benar.

#### 9.6 Penanda serangan Heavy

**Keputusan: serangan Heavy ditandai lewat tiga lapisan yang konsisten, yaitu ikon telegraph, bar dengan bingkai tebal dan zona Perfect berarsir, serta suara peringatan saat bar muncul.** Ikon Heavy sama di semua tempat, sehingga pemain hanya perlu mempelajarinya sekali.

**1. Tujuan desain.** Pemain tidak boleh pernah terkejut bahwa Perfect hanya menahan 50%. Informasi itu harus tersedia sejak telegraph sampai hasil.

**2. Aturan/mekanik.**

- Ikon Heavy: perisai retak setengah. Ikon ini dipakai di intent musuh (9.10), di header bar, dan pada marker Heavy.
- Bar Heavy: bingkai 2 kali lebih tebal, label "HEAVY" di header, dan zona Perfect diisi pola arsir diagonal setengah (1.1).
- Pada bar campuran (multi-hit dengan marker Heavy di akhir, 4.2), hanya marker terakhir yang berarsir; bingkai tebal tetap untuk seluruh bar.
- Saat bar Heavy muncul, dentum drum rendah diputar satu kali (berbeda dari tick cue).
- Hasil Perfect pada Heavy menampilkan "PERFECT −50%" (9.3) dengan SFX parry ditambah dentum (9.2).

**3. Interaksi dengan sistem lain.** 6.3 dan 6.4: Heavy selalu ditelegraph, sehingga ikon yang sama muncul satu giliran atau lebih sebelum bar. 3.1: Break Power Heavy tinggi, jadi pemain perlu memperhatikan gauge saat ikon ini muncul. 10.8: arsir dan ikon adalah pembeda yang color-safe.

**4. Kurva kesulitan dan sumber tantangan.** Keterbacaan Heavy memastikan serangan terbesar musuh menjadi ujian persiapan (heal, Shield) dan eksekusi, bukan ujian membaca UI.

**5. Risiko.**

- Terlalu banyak penanda bisa terasa berlebihan untuk pemain berpengalaman. Diterima: konsistensi lebih penting daripada minimalisme untuk serangan yang paling berbahaya.
- Ikon perisai retak bisa tertukar dengan ikon retak Broken (9.3). Mitigasi: ikon Broken memakai retakan pada lingkaran, bukan perisai.

**6. Cara pengujian.**

- Uji pemahaman: tester ditanya berapa damage yang ditahan oleh Perfect pada bar berarsir. Target: minimal 85% menjawab 50%.
- Uji pembedaan ikon: ikon Heavy dan Broken ditampilkan acak. Target: akurasi minimal 95%.

#### 9.7 Feedback Stun auto-Poor

**Keputusan: serangan ke target tunggal yang sedang Stun tetap menampilkan bar, tetapi dalam keadaan abu-abu dengan label "AUTO POOR" selama 600 ms.** Pemain tidak menekan apa pun, tetapi tetap melihat mengapa damage penuh masuk.

**1. Tujuan desain.** Auto-Poor tidak boleh terasa seperti bug atau input yang hilang. Penyebabnya harus terlihat sebelum damage muncul.

**2. Aturan/mekanik.**

- Karakter Stun ditandai di HUD party dengan potret abu-abu dan ikon gembok selama Stun aktif.
- Target tunggal Stun: bar abu-abu dengan ikon gembok dan label "AUTO POOR — STUN" tampil 600 ms, tanpa cursor. Input diabaikan.
- Shared QTE dengan sebagian target Stun: bar berjalan normal; target Stun ditandai di header (9.4) dan menerima badge Poor setelah dinilai.
- Multi-hit ke target Stun: satu bar abu-abu untuk seluruh rangkaian, bukan satu per marker.
- Lucky tidak terpicu pada auto-Poor (10.2).

**3. Interaksi dengan sistem lain.** 3.3: aturan Stun. 1.3: durasi bar abu-abu 600 ms lebih pendek dari QTE normal (1.6 s), jadi Stun juga mempersingkat round. 10.2: aturan yang sama berlaku saat Auto-Good aktif.

**4. Kurva kesulitan dan sumber tantangan.** Melihat bar yang "terkunci" mempertegas biaya membiarkan karakter terkena Stun, sehingga pemain lebih menghargai cleanse dan resistance.

**5. Risiko.**

- 600 ms untuk bar yang tidak bisa diinteraksi bisa terasa membuang waktu. Diterima; durasi ini cukup untuk dibaca tetapi tetap lebih singkat dari QTE normal.
- Pemain bisa mencoba menekan dan merasa input-nya rusak. Mitigasi: tidak ada cursor sama sekali di bar abu-abu.

**6. Cara pengujian.**

- Uji pemahaman: setelah auto-Poor pertama, tester ditanya apa yang terjadi. Target: minimal 90% menyebut Stun.
- Laporan bug playtest: jumlah laporan "input tidak terbaca" yang ternyata auto-Poor. Target: 0 setelah iterasi pertama.

#### 9.8 Penempatan damage number dan hasil QTE

**Keputusan: urutan tampil yang tetap di atas karakter yang terkena: label grade (0 ms), lalu damage number (+100 ms), lalu ikon status (+250 ms).** Tiga informasi ini tidak pernah tampil di baris yang sama, sehingga hasil QTE dan penerapan status tidak tertukar.

**1. Tujuan desain.** Memisahkan secara visual tiga pertanyaan berbeda: seberapa baik eksekusinya, berapa damage-nya, dan apakah status diterapkan (5.3).

**2. Aturan/mekanik.**

| Baris (dari bawah ke atas, di atas kepala karakter) | Isi | Muncul | Durasi |
| --- | --- | --- | --- |
| 1 | Label grade (+ reduksi untuk defensif, 9.3) | 0 ms | 700 ms |
| 2 | Damage number | +100 ms | 900 ms |
| 3 | Ikon status yang berhasil diterapkan | +250 ms | 900 ms |

- Waktu dihitung dari saat penilaian QTE.
- Multi-hit: damage number per hit ditumpuk 24 px ke atas, maksimal 6 angka, lalu memudar bersama.
- Critical: angka lebih besar dengan bentuk percikan di belakangnya, terpisah dari label grade (QTE dan Critical adalah mekanik terpisah).
- Damage 0 (Perfect defensif atau Nullification) ditampilkan sebagai "0", bukan disembunyikan.
- Di bar HUD, label grade juga tampil selama 350 ms di ujung bar (1.3), agar mata yang melihat bar tetap mendapat hasilnya.
- Status yang gagal diterapkan karena resistance atau immunity menampilkan teks kecil "RESIST" atau "IMMUNE" di baris 3.

**3. Interaksi dengan sistem lain.** 5.3: pemisahan baris 1 dan 3 menjawab risiko "Perfect tetapi terkena status". 5.2: angka yang tampil sama persis dengan perubahan HP. Visual Style Guide: posisi dan timing damage number yang konsisten sudah menjadi aturan di sana.

**4. Kurva kesulitan dan sumber tantangan.** Tidak menambah tantangan. Kejelasan sebab-akibat membuat kekalahan terasa adil dan bisa dipelajari.

**5. Risiko.**

- Tiga baris di atas karakter yang berdekatan bisa saling menutupi. Mitigasi: jika dua karakter bersebelahan terkena Shared QTE, tumpukan digeser horizontal 16 px.
- Durasi tampil yang panjang bisa menumpuk pada aksi cepat berturut-turut. Mitigasi: angka baru langsung menggantikan yang lama pada target yang sama.

**6. Cara pengujian.**

- Uji pemahaman: setelah serangan hybrid, tester menjelaskan tiga hal (grade, damage, status). Target: minimal 85% menjawab ketiganya dengan benar.
- Uji kepadatan: Shared QTE 4 target dengan status. Target: tidak ada teks yang tertutup lebih dari 30% luasnya.

#### 9.9 Feedback retry Lucky

**Keputusan: banner "Lucky!" dengan ikon semanggi tampil di bar selama 400 ms, dan sisa kuota Lucky selalu terlihat sebagai 3 titik di potret karakter.** Pemain tahu kapan Lucky menyelamatkannya dan berapa kali lagi itu bisa terjadi di battle ini.

**1. Tujuan desain.** Membuat retry terasa sebagai momen keberuntungan yang jelas, sekaligus memberi pemain informasi kuota untuk keputusan berisiko (misalnya finisher Martial Artist).

**2. Aturan/mekanik.**

- Saat Lucky aktif: label Poor di bar diganti banner "Lucky!" dengan ikon semanggi dan chime pendek, selama 400 ms (8.1).
- Bar lalu diulang dengan border berkilau tipis sebagai tanda bahwa ini percobaan kedua.
- Kuota Lucky: 3 titik kecil di bawah potret karakter yang punya talent Lucky. Satu titik meredup setiap kali Lucky terpakai. Kuota reset di battle berikutnya.
- Marker sekuens yang di-retry menampilkan ikon semanggi kecil di samping glyph grade-nya (9.5).
- Jika retry juga Poor, label Poor tampil normal tanpa efek tambahan.

**3. Interaksi dengan sistem lain.** 8.1: alur dan durasi retry. 9.4: pada Shared QTE, titik kuota yang meredup adalah milik karakter yang kuotanya dipakai. 7.4: kuota yang terlihat membantu pemain memutuskan kapan mengambil risiko finisher.

**4. Kurva kesulitan dan sumber tantangan.** Tidak menambah tantangan. Kuota yang terlihat mengubah Lucky dari kejutan acak menjadi sumber daya yang bisa diperhitungkan.

**5. Risiko.**

- Titik kuota menambah elemen di HUD party yang sudah padat. Diterima karena hanya muncul pada karakter dengan Lucky.
- Pemain bisa salah paham bahwa titik kuota menjamin retry. Mitigasi: tooltip titik menyebutkan peluang 20%.

**6. Cara pengujian.**

- Uji pemahaman: tester ditanya arti 3 titik di potret. Target: minimal 80% menjawab benar setelah melihat Lucky aktif sekali.
- Uji pembedaan: tester bisa membedakan bar retry dari bar biasa. Target: minimal 90%.

#### 9.10 Ikon intent, Break gauge, dan penghitung combo (tambahan)

**Tambahan di luar checklist.** Item ini diperlukan karena 3.1, 6.4, 6.5, dan 7.3 merujuk ke Bagian 9 untuk tampilan tiga elemen HUD ini.

**Keputusan: tiga elemen HUD baru, masing-masing hanya muncul saat informasinya relevan.** Prinsipnya: tampilkan saat pemain perlu mengambil keputusan, sembunyikan saat tidak.

**1. Tujuan desain.** Memberi informasi yang dibutuhkan untuk keputusan taktis (bertahan, interrupt, melepas finisher) tanpa membanjiri HUD.

**2. Aturan/mekanik.**

| Elemen | Lokasi | Kapan tampil | Isi |
| --- | --- | --- | --- |
| Ikon intent | Di atas musuh, 24 px | Sejak awal round, hanya untuk aksi wajib telegraph (6.4) | Jenis aksi (Heavy, signature, charge dengan hitungan mundur, status), potret target, jumlah hit "×n" |
| Break gauge pemain | Garis tipis di bawah bar HP potret | Saat gauge di atas 50 | 10 segmen; berkedip saat ≥ 80 |
| Break gauge musuh | Garis tipis di bawah bar HP musuh | Saat musuh sedang charge, atau gauge di atas 50 | 10 segmen (20 untuk boss); berkedip saat ≥ 80% |
| Penghitung combo | Di samping potret Martial Artist | Selama battle | Angka 0–10 (atau 12) dengan cincin bersegmen; "MAX" bercahaya saat penuh; animasi pecah 300 ms saat reset |

- Ikon intent memakai ikon yang sama dengan bar (9.6), sehingga bahasa visualnya konsisten.
- Saat Auto-Good aktif, cincin combo hanya menampilkan 6 segmen (10.1).
- Semua elemen memakai bentuk dan angka sebagai pembeda utama, bukan hanya warna (10.8).

**3. Interaksi dengan sistem lain.** 6.4 dan 6.5: ikon intent dan gauge musuh adalah informasi utama untuk keputusan interrupt. 3.1: gauge pemain memberi peringatan sebelum Broken. 7.3 dan 7.4: penghitung combo menunjukkan kapan finisher layak dipakai.

**4. Kurva kesulitan dan sumber tantangan.** Elemen ini memindahkan kesulitan dari "menebak keadaan" ke "memutuskan berdasarkan keadaan", sesuai prinsip bahwa kesulitan datang dari keputusan.

**5. Risiko.**

- Dengan 4 musuh yang semuanya punya intent dan gauge, layar bisa padat. Mitigasi: aturan "tampil hanya saat relevan" dan ukuran ikon tetap 24 px.
- Pemain baru mungkin tidak memahami gauge. Mitigasi: tooltip saat gauge pertama kali muncul dalam run.

**6. Cara pengujian.**

- Uji kepadatan: battle 4 musuh dengan 2 charge dan 2 Heavy. Target: minimal 80% tester bisa menyebutkan musuh mana yang akan melakukan apa.
- Telemetri keputusan: persentase interrupt yang dicoba saat gauge musuh terlihat ≥ 50%. Target: minimal 60% dari semua upaya interrupt, sebagai bukti gauge dipakai untuk mengambil keputusan.

### 10. Accessibility dan Player Settings

#### 10.1 Auto-Good dan Martial Artist

**Keputusan: saat Auto-Good aktif, Maximum Combo Martial Artist turun menjadi 60% dari nilai normalnya (6, atau 7 dengan Limitless Potential).** Tanpa penyesuaian ini, Auto-Good membuat Martial Artist lebih kuat daripada pemain yang mahir, karena combo-nya tidak pernah reset.

**1. Tujuan desain.** Auto-Good harus memungkinkan pemain menikmati loop combo, tetapi tidak boleh menjadi cara paling efektif untuk memainkan Martial Artist.

**2. Aturan/mekanik.**

- Maximum Combo saat Auto-Good = floor(0.6 × Maximum Combo normal): 6 (normal) atau 7 (Limitless Potential).
- Final Technique bisa dipakai saat combo mencapai maksimum Auto-Good (6 atau 7).
- Perhitungan pembanding (untuk pemain rata-rata dengan Perfect 45%, Good 40%, Poor 15%, memakai Quick Jab):
  - Pemain manual mencapai combo 10 tanpa Poor dalam 4 aksi berturut-turut dengan peluang sekitar 0.85⁴ ≈ 52%.
  - Pemain Auto-Good tanpa batas ini selalu mencapai 10 dalam 5 aksi, dengan Dragon's Fury 2.5× yang pasti.
  - Dengan batas 6, Dragon's Fury Auto-Good maksimal 1.9×, di bawah rata-rata yang bisa dicapai pemain manual.
- Efek yang memerlukan Perfect tidak aktif saat Auto-Good, karena Perfect tidak pernah terjadi: counter (6.5), bonus Perfect combo, efek Legendary yang dipicu Perfect (8.3). Tooltip skill dan item menampilkan "Tidak aktif dalam Auto-Good".
- Penghitung combo menampilkan 6 segmen saat Auto-Good aktif (9.10).

**3. Interaksi dengan sistem lain.** 7.3 dan 7.4: batas dan tabel finisher memakai Maximum Combo Auto-Good. 6.5: skill counter (Counterstrike, Perfect Counter, Counterattack) tidak bisa terpicu. 10.3: Auto-Good adalah pengaturan terpisah dari pelebaran window.

**4. Kurva kesulitan dan sumber tantangan.** Dengan Auto-Good, tantangan Martial Artist berpindah sepenuhnya ke keputusan: kapan melepas finisher dengan batas combo yang lebih rendah. Tantangan eksekusi hilang, dan sebagai gantinya potensi maksimal class juga lebih rendah.

**5. Risiko.**

- Pemain yang membutuhkan Auto-Good kehilangan skill counter sepenuhnya. Ini harga yang nyata; tooltip harus menjelaskannya sebelum pemain memilih skill.
- Batas 60% bisa terlalu rendah atau terlalu tinggi. Mitigasi: telemetri di bawah.

**6. Cara pengujian.**

- Simulasi DPS Martial Artist: Auto-Good (batas 6) dibanding pemain manual rata-rata. Target: Auto-Good berada 5–15% di bawah pemain manual rata-rata.
- Survei pengguna Auto-Good: "Apakah Martial Artist tetap terasa menyenangkan?" Target: minimal 70% menjawab ya.

#### 10.2 Prioritas Stun terhadap Auto-Good

**Keputusan: Stun tetap memaksa Poor walaupun Auto-Good aktif, dan Lucky tidak pernah terpicu pada auto-Poor.** Auto-Good menggantikan eksekusi pemain, bukan aturan status.

**1. Tujuan desain.** Menjaga Stun sebagai ancaman taktis yang sama bagi semua pemain, sehingga keputusan cleanse dan resistance tetap bermakna di semua pengaturan.

**2. Aturan/mekanik.**

- Urutan prioritas hasil QTE defensif, dari yang paling kuat:
  1. Stun → Poor (auto).
  2. Auto-Good → Good.
  3. Broken → hasil terbaik Good.
  4. Input pemain (Perfect, Good, atau Poor).
- Lucky hanya terpicu pada Poor yang berasal dari input pemain. Auto-Poor karena Stun atau karakter Down tidak bisa di-retry, karena karakter memang tidak bisa bereaksi.
- Saat Auto-Good, bar tetap tampil (agar pemain melihat ritme serangan), tetapi tanpa interaksi dan dengan label "AUTO GOOD". Untuk karakter Stun, bar abu-abu "AUTO POOR — STUN" (9.7) yang tampil.

**3. Interaksi dengan sistem lain.** 3.3: daftar kondisi auto-Poor berlaku sama di semua pengaturan. 8.1: aturan Lucky diperjelas. 9.7: tampilan auto-Poor tidak berubah saat Auto-Good aktif.

**4. Kurva kesulitan dan sumber tantangan.** Pengguna Auto-Good tetap menghadapi tantangan yang sama dari Stun, sehingga kesulitan taktis tidak hilang ketika kesulitan eksekusi dihilangkan.

**5. Risiko.**

- Pengguna Auto-Good bisa merasa Stun lebih menghukum bagi mereka, karena perbedaan Good dan Poor terasa tajam. Diterima: perbedaan itu sama besarnya bagi pemain manual.
- Aturan prioritas bisa membingungkan developer. Mitigasi: daftar urutan di atas menjadi satu fungsi resolusi tunggal di kode.

**6. Cara pengujian.**

- Unit test kombinasi: Stun, Auto-Good, Broken, dan Lucky dalam semua kombinasi (16 kasus). Target: semua hasil sesuai urutan prioritas.
- Uji pemahaman pengguna Auto-Good setelah terkena Stun. Target: minimal 85% bisa menjelaskan mengapa hasilnya Poor.

#### 10.3 Pengaturan timing window

**Keputusan: pengali window 100%, 125%, atau 150%, diterapkan setelah semua modifier dan clamp, dan tidak dihitung dalam cap +25%.** Pengaturan assist tidak mengunci konten apa pun, tetapi tercatat di ringkasan run.

**1. Tujuan desain.** Memberi jalan tengah antara bermain manual penuh dan Auto-Good, untuk pemain yang bisa mengikuti ritme tetapi kesulitan dengan presisi milidetik.

**2. Aturan/mekanik.**

- Pilihan: 100% (default), 125%, 150%. Berlaku untuk W\_P dan W\_G, ofensif dan defensif sekaligus.
- Urutan perhitungan: tier → Freeze → modifier karakter → clamp 66–125 ms (3.4) → **pengali assist**.
- Contoh: boss dengan W\_P 72 ms dan pengali 150% → 108 ms. Normal dengan modifier +25% (112.5 ms) dan pengali 150% → 168.75 ms.
- Interval minimum antar marker dihitung ulang dari W\_G setelah pengali (W\_G + 100 ms), sehingga window tidak pernah tumpang tindih.
- Bisa diubah kapan saja di menu pause, berlaku mulai QTE berikutnya.
- Ringkasan run mencatat label "Assist: Window 125%" atau "150%" jika pernah aktif. Tidak ada achievement atau konten yang dikunci.

**3. Interaksi dengan sistem lain.** 3.4: cap +25% hanya untuk kekuatan karakter; pengaturan assist berada di luar cap. 1.3: interval yang lebih panjang memperpanjang bar multi-hit sedikit. 10.1: pengali window tidak memengaruhi Maximum Combo, karena eksekusi tetap dilakukan pemain.

**4. Kurva kesulitan dan sumber tantangan.** Kurva per tier dan dungeon (6.1) tetap berlaku, hanya bergeser lebih lebar. Tantangan taktis tidak berubah sama sekali.

**5. Risiko.**

- Pemain yang sebenarnya mampu bisa memakai 150% untuk mempermudah game. Diterima: ini pilihan pemain, dan label di ringkasan run menjaga transparansi.
- Window 150% pada sekuens 6 marker memperpanjang bar hingga sekitar 4 detik. Diterima sebagai harga aksesibilitas.

**6. Cara pengujian.**

- Uji dengan tester yang punya kesulitan motorik atau reaksi: rasio Poor pada 150%. Target: di bawah 25% di tier normal.
- Unit test urutan perhitungan dengan 12 kombinasi. Target: semua sesuai urutan dan interval tidak pernah tumpang tindih.

#### 10.4 Pengaturan kecepatan QTE

**Keputusan: pengali T\_lead 100%, 125%, atau 150%, yang memperlambat cursor tanpa mengubah lebar window dalam milidetik.** Pengaturan ini membantu pemain yang butuh waktu antisipasi lebih lama, terpisah dari pemain yang butuh toleransi presisi lebih besar (10.3).

**1. Tujuan desain.** Memisahkan dua jenis kesulitan reaksi, yaitu lambat mengantisipasi dan kurang presisi, agar pemain bisa memilih bantuan yang tepat.

**2. Aturan/mekanik.**

- Pilihan: 100% (default), 125%, 150%. Berlaku untuk semua QTE.
- T\_lead akhir = T\_lead tier × pengali dungeon (6.1) × pengali assist.
- Interval antar marker juga dikalikan dengan pengali yang sama, agar ritme pola (6.2) tetap utuh.
- Window W\_P dan W\_G tidak berubah.
- Ritme tick cue (1.1) mengikuti T\_lead yang baru.
- Karena cursor lebih lambat, zona di layar menjadi lebih sempit dalam pixel. Contoh: boss di dungeon 1 dengan 150% → T\_lead 1200 ms, kecepatan 0.3 px/ms, zona Perfect 72 ms ≈ 22 px. Batas minimum tampilan zona Perfect 16 px; jika lebih kecil, zona digambar 16 px tanpa mengubah window sebenarnya.
- Tercatat di ringkasan run sebagai "Assist: Speed 125%/150%".

**3. Interaksi dengan sistem lain.** 1.3: durasi QTE dan target waktu per round ikut memanjang; target telemetri durasi dihitung terpisah untuk pemain dengan assist ini. 10.3: kedua pengaturan bisa dipakai bersamaan. 6.1: progresi antar dungeon tetap terasa karena pengali dungeon tetap berlaku.

**4. Kurva kesulitan dan sumber tantangan.** Tantangan presisi tetap sama; yang berkurang hanya tekanan waktu antisipasi. Ini cocok untuk pemain yang ritmenya bagus tetapi butuh waktu lebih lama membaca bar.

**5. Risiko.**

- Pemain bisa salah memilih pengaturan (Speed padahal masalahnya presisi). Mitigasi: deskripsi menu menjelaskan perbedaannya dengan contoh singkat.
- Zona yang lebih sempit dalam pixel bisa terasa lebih sulit secara visual. Mitigasi: batas tampilan 16 px dan cue audio.

**6. Cara pengujian.**

- Uji pilihan: tester dengan kesulitan reaksi mencoba kedua pengaturan. Target: minimal 70% memilih pengaturan yang menurunkan rasio Poor-nya paling banyak.
- Telemetri durasi: median waktu QTE per round pada 150%. Target: di bawah 18 detik.

#### 10.5 Visual cue alternatif

**Keputusan: lima opsi visual yang bisa diaktifkan terpisah, semuanya hanya mengubah cara timing ditampilkan dan tidak mengubah window.** Karena tidak memengaruhi window, opsi ini tidak tercatat sebagai assist di ringkasan run.

**1. Tujuan desain.** Memberi pemain cara membaca timing yang sesuai dengan cara matanya bekerja, tanpa mengubah tingkat kesulitan.

**2. Aturan/mekanik.**

| Opsi | Efek | Default |
| --- | --- | --- |
| Ukuran bar | 25% atau 35% lebar layar | 25% |
| Kilatan hit point | Seluruh bar berkilat 50 ms tepat di hit point | Mati |
| Denyut tepi layar | Garis tipis di tepi layar berdenyut pada setiap tick dan hit point | Mati |
| Kontras tinggi | Latar bar hitam pekat, zona digambar dengan garis putih tebal | Mati |
| Kurangi gerakan layar | Camera shake mati, flash Perfect diganti outline | Mati |

- Kilatan hit point menandai momen ideal, bukan awal window. Pemain yang menekan saat melihat kilatan akan cenderung terlambat; opsi ini dipadukan dengan kalibrasi offset (10.7).
- Denyut tepi layar mengikuti ritme tick cue (1.1), sehingga pemain yang menatap karakter tetap mendapat ritme visual.
- Semua opsi bisa diubah dari menu pause.

**3. Interaksi dengan sistem lain.** 9.1: opsi "Kurangi gerakan layar" mematikan camera shake. 1.1 dan 10.6: denyut tepi layar adalah padanan visual dari tick cue. 10.8: mode kontras tinggi melengkapi indikator color-safe.

**4. Kurva kesulitan dan sumber tantangan.** Tidak berubah. Opsi ini menghilangkan hambatan persepsi, bukan tantangan eksekusi.

**5. Risiko.**

- Kilatan hit point bisa diam-diam membuat pemain terlambat. Mitigasi: catatan di deskripsi menu dan kalibrasi offset.
- Denyut tepi layar bisa mengganggu pemain yang sensitif cahaya. Mitigasi: default mati, dan intensitasnya rendah (opasitas 30%).

**6. Cara pengujian.**

- Uji per opsi: tester dengan gangguan penglihatan ringan memakai opsi yang mereka pilih. Target: rasio Good-atau-lebih-baik naik minimal 10 poin persentase dibanding default.
- Uji keselamatan: denyut dan kilatan diperiksa terhadap batas frekuensi kilatan untuk fotosensitivitas (tidak lebih dari 3 kilatan per detik).

#### 10.6 Audio cue alternatif

**Keputusan: slider volume khusus QTE, mode metronom empat ketukan, dan getaran controller sebagai cue taktil.** Semua opsi hanya mengubah penyampaian ritme, bukan window.

**1. Tujuan desain.** Memungkinkan pemain yang lemah secara visual, atau yang lebih kuat membaca ritme lewat suara, untuk menjadikan audio sebagai cue utama.

**2. Aturan/mekanik.**

| Opsi | Efek | Default |
| --- | --- | --- |
| Volume cue QTE | Slider 0–100 untuk tick cue dan SFX hasil, terpisah dari SFX umum | 80 |
| Mode metronom | Tick cue menjadi empat ketukan (hit point − 3I sampai − I, dengan I = T\_lead ÷ 4) alih-alih dua | Mati |
| Getaran controller | Getaran pendek 30 ms pada setiap tick | Mati |
| Cue mono | Semua cue QTE diputar di tengah (tanpa panning) | Aktif |

- Pada mode metronom, ketukan terakhir tetap tidak berbunyi, sama seperti 1.1.
- Getaran hanya untuk tick, tidak untuk hit point, agar pemain tidak menunggu getaran lalu terlambat.
- Pada pola Delayed dan Syncopated (6.2), tick dan getaran mengikuti interval yang sebenarnya.

**3. Interaksi dengan sistem lain.** 1.1: mode metronom adalah perluasan dari ritme tiga ketukan. 9.2: slider mencakup keluarga SFX hasil. 10.5: denyut tepi layar adalah padanan visual dari tick cue.

**4. Kurva kesulitan dan sumber tantangan.** Tidak berubah. Mode metronom memberi lebih banyak titik acuan ritme, tetapi window tetap sama.

**5. Risiko.**

- Mode metronom pada T\_lead pendek (boss dungeon 8, 640 ms) menghasilkan ketukan setiap 160 ms, yang bisa terdengar seperti satu suara. Mitigasi: jika I di bawah 200 ms, metronom otomatis kembali ke dua ketukan.
- Getaran controller menambah latensi taktil yang berbeda per perangkat. Diterima karena getaran hanya untuk tick, bukan penanda saat menekan.

**6. Cara pengujian.**

- Uji audio-only dengan mode metronom: layar ditutup selama 30 QTE. Target: rasio Good-atau-lebih-baik minimal 70% (lebih tinggi dari target 60% di 1.1).
- Uji getaran: tester memakai getaran tanpa suara selama 20 QTE. Target: rasio Good-atau-lebih-baik minimal 55%.

#### 10.7 Input accessibility dan kalibrasi

**Keputusan: rebinding penuh, mode "tombol apa saja", filter tremor, toleransi tekan-awal yang terbatas, dan kalibrasi offset per perangkat.** Kalibrasi wajib tersedia sejak playtest pertama karena window Perfect hanya 66–90 ms (1.2).

**1. Tujuan desain.** Memastikan hasil QTE mencerminkan timing pemain, bukan latensi perangkat atau keterbatasan motorik.

**2. Aturan/mekanik.**

- **Rebinding:** tombol QTE bisa diganti di keyboard, mouse, dan controller.
- **Mode tombol apa saja:** semua tombol kecuali pause dan menu dihitung sebagai input QTE. Default mati.
- **Filter tremor:** penekanan kedua dalam 120 ms setelah penekanan pertama diabaikan. Default mati.
- **Toleransi tekan-awal:** penekanan prematur pertama per marker diabaikan, bukan dihitung Poor. Penekanan prematur kedua tetap Poor, agar mashing tetap tidak efektif (1.2). Default mati; tercatat sebagai assist di ringkasan run.
- **Kalibrasi offset:**
  - Rentang −150 sampai +150 ms, langkah 5 ms, disimpan terpisah untuk keyboard/mouse dan controller.
  - Tes kalibrasi: 8 ketukan metronom, pemain menekan di setiap ketukan, lalu median selisihnya disarankan sebagai offset.
  - Offset diterapkan pada timestamp input sebelum penilaian (1.2).
  - Tes kalibrasi ditawarkan saat pertama kali game dijalankan dan bisa diulang dari menu.

**3. Interaksi dengan sistem lain.** 1.2: offset dan filter bekerja di tahap pemrosesan input, sebelum penilaian. 10.5: kilatan hit point paling akurat jika offset sudah dikalibrasi. 10.3: toleransi tekan-awal tercatat sebagai assist karena mengubah aturan penilaian, sedangkan kalibrasi tidak.

**4. Kurva kesulitan dan sumber tantangan.** Kalibrasi tidak mengubah kesulitan; ia menghapus kesulitan palsu dari latensi. Toleransi tekan-awal sedikit menurunkan kesulitan dan karena itu ditandai sebagai assist.

**5. Risiko.**

- Pemain bisa menyalahgunakan offset untuk "menggeser" window. Tidak ada keuntungan: offset hanya memindahkan titik acuan, tidak melebarkan window.
- Mode tombol apa saja bisa memicu QTE secara tidak sengaja dari tombol lain. Diterima, karena default mati dan dipilih secara sadar.

**6. Cara pengujian.**

- Uji kalibrasi: tester dengan controller Bluetooth sebelum dan sesudah kalibrasi. Target: selisih rata-rata timing turun di bawah 10 ms (sejalan dengan 1.2).
- Uji tremor: tester dengan tremor ringan memakai filter tremor. Target: rasio Poor turun minimal 10 poin persentase.
- Uji mashing dengan toleransi tekan-awal aktif. Target: rasio Poor tetap minimal 85%.

#### 10.8 Color-safe indicators

**Keputusan: setiap informasi QTE punya pembeda non-warna (bentuk, pola, teks, atau posisi), dan warna hanya menjadi lapisan kedua.** Aturan ini berlaku tanpa perlu mengaktifkan mode khusus.

**1. Tujuan desain.** Pemain dengan buta warna jenis apa pun bisa membaca semua informasi QTE dengan kecepatan yang sama seperti pemain lain.

**2. Aturan/mekanik.**

| Informasi | Pembeda utama (non-warna) | Rujukan |
| --- | --- | --- |
| Zona Perfect vs Good | Kurung siku di sisi zona Perfect | 1.1 |
| Grade hasil | Teks label + glyph bintang, lingkaran, silang | 9.5, 9.8 |
| Ofensif vs defensif | Ikon pedang/perisai + bentuk bingkai | 9.3 |
| Heavy | Arsir diagonal + bingkai tebal + ikon perisai retak | 9.6 |
| Broken | Ikon lingkaran retak | 3.1, 9.3 |
| Stun / auto-Poor | Potret abu-abu + ikon gembok | 9.7 |
| Gauge dan combo | Segmen bernomor dan angka | 9.10 |

- Kontras zona terhadap latar bar minimal 3:1, dan teks minimal 4.5:1.
- Palet warna diuji dengan simulasi deuteranopia, protanopia, dan tritanopia sebelum aset final.
- Mode kontras tinggi (10.5) tersedia sebagai lapisan tambahan.

**3. Interaksi dengan sistem lain.** Bagian 9: semua spesifikasi visual sebelumnya sudah memakai pembeda non-warna; tabel ini menjadi daftar periksa tunggal untuk tim art. Visual Style Guide: aturan color-safe di sana diperkuat dengan rasio kontras konkret.

**4. Kurva kesulitan dan sumber tantangan.** Tidak berubah. Tujuannya menghapus hambatan persepsi, bukan mengubah tantangan.

**5. Risiko.**

- Terlalu banyak bentuk dan ikon bisa membuat HUD terasa ramai. Diterima: setiap ikon hanya muncul saat informasinya relevan (9.10).
- Arsir Heavy pada zona yang sempit (sekitar 22 px pada 10.4) bisa sulit dibaca. Mitigasi: arsir memakai garis minimal 2 px dengan jarak 4 px.

**6. Cara pengujian.**

- Uji dengan tester buta warna (minimal 2 per jenis yang umum): baca semua informasi di tabel. Target: akurasi setara dengan tester tanpa buta warna, selisih maksimal 5 poin persentase.
- Audit kontras otomatis pada semua elemen bar dan label. Target: semua memenuhi rasio 3:1 dan 4.5:1.

### Fase 3 — Verifikasi konsistensi

**Ditemukan 10 kontradiksi atau celah, dan semuanya sudah diperbaiki langsung di item terkait.** Lima titik risiko lain yang ditandai selama Fase 2 sudah diperiksa dan tidak memerlukan perubahan.

#### Kontradiksi dan perbaikan

| No | Masalah | Item | Perbaikan |
| --- | --- | --- | --- |
| 1 | Final Technique dengan Perfect dan Critical bisa mencapai 15.3× base damage | 7.4 | Finisher tidak bisa Critical; hasil maksimal menjadi 5.1× |
| 2 | Aturan marker ("hit di atas 6 dibagi ke 6 marker") bentrok dengan Hundred Fists 10 hit dalam 5 marker | 1.3, 2.2, 7.2 | qteProfile boleh menetapkan jumlah marker sendiri, tetap maksimal 6 |
| 3 | 2.4 menyatakan Poor hanya berdampak pada damage, padahal 3.1 dan 6.5 juga memotong Break Power | 2.4 | Konsekuensi Poor ditulis lengkap: 0.5× damage dan 0.5× Break Power |
| 4 | Rentang T\_lead 600–1200 ms di 1.3 tidak cocok dengan angka 6.1 (640–1000 ms) dan pengaturan kecepatan (sampai 1500 ms) | 1.3, 6.1 | Rentang diperbarui; 6.1 merujuk ke 1.3 |
| 5 | Clamp W\_P 66–125 ms di 3.4 bertentangan dengan pengali assist 150% di 10.3 | 3.4 | Pengali assist diterapkan sesudah clamp dan boleh melewati 125 ms |
| 6 | Koefisien damage multi-target dijanjikan "lebih rendah" tanpa angka di dua tempat | 2.3, 3.2 | Maksimal 60% dari serangan single-target setara, untuk musuh maupun pemain |
| 7 | 5.3 masih menyebut efek QTE ofensif pada chance status sebagai item terbuka | 5.3 | Dirujuk ke keputusan 2.1 (tidak memengaruhi) |
| 8 | 3.4 menyebut Limitless Potential sebagai modifier window, padahal 7.5 menetapkannya sebagai efek combo | 3.4 | Diganti dengan Agile Defense dan Inner Focus |
| 9 | Batas "1 efek Perfect per class tree" (2.1) ambigu untuk mekanik inti Martial Artist | 2.1 | Mekanik inti class tidak dihitung; efek Martial Artist yang dihitung adalah Perfect Counter |
| 10 | Efek Legendary yang dipicu Perfect tidak punya batas angka | 8.3 | Pemulihan resource maksimal 2% Max HP/MP per pemicu, satu pemicu per giliran |

#### Diperiksa, tidak perlu perubahan

- **Status di-roll walaupun damage 0 karena Nullification (2.1).** Konsisten dengan Bab 11 §8: status ditentukan oleh skill, bukan oleh elemen damage.
- **Counter yang dipicu Shared QTE (6.5).** Batas 1 counter per pengguna per aksi musuh sudah membatasi jumlahnya ke banyaknya pengguna counter di party; target telemetri 15% damage total menjadi pengaman.
- **Tumpukan perlindungan combo Martial Artist (7.5).** Unbroken Flow, Unbroken Rhythm, dan Combo Discipline melindungi dari tiga penyebab reset yang berbeda dan dua di antaranya hanya sekali per battle; tidak ada kombinasi yang membuat combo kebal sepenuhnya terhadap Poor.
- **Poor terlalu ringan untuk game hardcore (2.4).** Poor ofensif memotong damage dan Break Power sambil tetap menghabiskan MP; Poor defensif berarti damage dan Break Power penuh. Hukuman tambahan berisiko menciptakan death spiral dalam run yang berakhir permanen saat kalah.
- **Skill pemain dengan status per hit lebih dari 3 hit (4.3).** Belum ada skill yang terdefinisi melanggar; aturannya diterapkan saat skill baru didefinisikan.

#### Batas cakupan verifikasi

Keseimbangan antara ekonomi resource survival (HP, MP, Gold) dan durasi pertarungan tidak bisa diverifikasi penuh di dokumen ini, karena angka MP cost, healing, dan HP musuh belum ditetapkan di dokumen project mana pun. Dari sisi QTE, dampaknya sudah dibatasi: durasi QTE per round (1.3), MP tetap terpakai saat Poor (2.4), dan pemulihan resource dari Perfect dibatasi (8.3).

#### Status semua item

**Semua 49 item (48 dari checklist ditambah 9.10) sudah lengkap; 12 di antaranya direvisi di Fase 3.** Tidak ada kontradiksi yang tersisa di dalam cakupan dokumen ini. Item tambahan 8.5 (Racial Feat dan QTE) ditulis setelah Fase 3 dan sudah dicek terhadap 3.4, 5.1, 6.4, 6.5, dan 8.1.

| Item | Status | Catatan |
| --- | --- | --- |
| 1.1 Bentuk indikator | Selesai | Timing bar horizontal dengan cue audio tiga ketukan |
| 1.2 Jenis input | Selesai | Satu tombol; prematur = Poor; batas W\_P 66 ms |
| 1.3 Durasi sesi QTE | Direvisi | Rentang T\_lead diselaraskan dengan 6.1 dan 10.4; jumlah marker mengikuti qteProfile |
| 2.1 Skill hybrid dan status | Direvisi | Aturan efek Perfect per class tree diperjelas |
| 2.2 Grade QTE sekuens | Direvisi | qteProfile boleh menetapkan jumlah marker |
| 2.3 AoE ofensif | Direvisi | Koefisien per target maksimal 60% |
| 2.4 Konsekuensi Poor | Direvisi | Konsekuensi Poor mencakup Break Power |
| 3.1 Broken | Selesai | Memakai asumsi A5 (Break gauge) |
| 3.2 Multi-target sebagian party | Direvisi | Koefisien multi-target musuh maksimal 60% |
| 3.3 Karakter tidak bisa bereaksi | Selesai | Hanya Stun dan Down yang memaksa Poor |
| 3.4 Modifier Defensive QTE | Direvisi | Pengali assist di luar clamp; rujukan passive diperbaiki |
| 4.1 Window per hit | Selesai | Window sama untuk semua marker dalam satu bar |
| 4.2 Kesulitan hit berikutnya | Selesai | Eskalasi hanya lewat interval dan satu marker Heavy di akhir |
| 4.3 Multi-hit dan status | Direvisi | Belum ada skill pemain yang melanggar batas 3 hit |
| 5.1 Damage Reduction lain | Selesai | Multiplicative, mengikuti Damage GDD |
| 5.2 Rounding | Selesai | Floor sekali per hit |
| 5.3 Status Immunity | Direvisi | Rujukan ke 2.1 diperbarui |
| 6.1 Window dan T\_lead per tier | Direvisi | Rujukan rentang ke 1.3 |
| 6.2 Pola timing | Selesai | Empat pola tetap, tidak acak |
| 6.3 Pola boss | Selesai | Signature dan fase 2 di 50% HP |
| 6.4 Telegraph dan charge | Selesai | Telegraph tidak mengubah timing |
| 6.5 Interrupt dan counter | Selesai | Interrupt lewat Broken; counter dari Defensive Perfect |
| 7.1 Gain combo per skill | Selesai | Gain 1–2, Perfect +1 |
| 7.2 Gain dari sekuens | Selesai | Gain per marker, bonus +1 jika semua Perfect |
| 7.3 Maximum Combo | Selesai | 10, atau 12 dengan Limitless Potential |
| 7.4 Finisher | Direvisi | Finisher tidak bisa Critical |
| 7.5 Passive dan talent | Selesai | Class lain hanya lewat dua jalur yang diizinkan |
| 8.1 Retry Lucky ofensif | Selesai | Alur identik dengan defensif |
| 8.2 Atribut | Selesai | Tidak ada atribut yang memengaruhi QTE |
| 8.3 Equipment | Direvisi | Batas efek Perfect untuk pemulihan resource |
| 8.4 Talent lain | Selesai | Quick Reflex dan Steady Hand |
| 9.1 Animasi per grade | Selesai | Hit-stop per grade di dalam 350 ms |
| 9.2 SFX | Selesai | Tiga keluarga suara dengan varian ofensif dan defensif |
| 9.3 Feedback defensif | Selesai | Ikon perisai dan persentase reduksi |
| 9.4 Feedback Shared | Selesai | Badge hasil per target |
| 9.5 Feedback multi-hit | Selesai | Glyph per marker dan ringkasan |
| 9.6 Penanda Heavy | Selesai | Tiga lapisan penanda yang konsisten |
| 9.7 Feedback Stun | Selesai | Bar abu-abu "AUTO POOR" |
| 9.8 Damage number | Selesai | Urutan label, damage, lalu status |
| 9.9 Feedback Lucky | Selesai | Banner dan kuota 3 titik |
| 9.10 Intent, gauge, combo | Selesai | Tambahan di luar checklist |
| 10.1 Auto-Good dan Martial Artist | Selesai | Maximum Combo 60% saat Auto-Good |
| 10.2 Prioritas Stun | Selesai | Urutan prioritas hasil QTE |
| 10.3 Timing window | Selesai | Pengali window, tercatat sebagai assist |
| 10.4 Kecepatan QTE | Selesai | Pengali T\_lead, tercatat sebagai assist |
| 10.5 Visual cue | Selesai | Lima opsi visual |
| 10.6 Audio cue | Selesai | Volume, metronom, dan getaran |
| 10.7 Input dan kalibrasi | Selesai | Kalibrasi offset per perangkat |
| 10.8 Color-safe | Selesai | Pembeda non-warna untuk semua informasi |

# Bagian V — Item dan Ekonomi

## Bab 13 — Inventory dan Equipment

> Sumber: Inventory & Equipment System v1.2.

### 1. Overview

Inventory & Equipment System merupakan sistem yang mengatur seluruh item yang dimiliki pemain serta equipment yang digunakan oleh setiap karakter.

Sistem ini menggunakan model Loot RPG dengan enam rarity tiers, random affixes, equipment level, Legendary Effect, dan Mythic Effect.

Equipment dapat diperoleh melalui enemy drops, boss rewards, dungeon chests, dungeon events, serta shop.

Setiap equipment memiliki kombinasi karakteristik yang dapat memengaruhi build karakter.

Sistem ini dirancang untuk mendukung sembilan class:

- Fighter
- Barbarian
- Martial Artist
- Rogue
- Ranger
- Cleric
- Wizard
- Warlock
- Druid

#### 1.1 Design Goals

1. Memberikan variasi build karakter melalui equipment.
2. Menjadikan loot sebagai bagian penting dari dungeon exploration.
3. Memberikan identitas equipment berdasarkan class dan equipment type.
4. Menyediakan progression melalui equipment level dan upgrade.
5. Memungkinkan pemain mengelola, membandingkan, memasang, melepas, serta menjual equipment.
6. Menyediakan sistem equipment yang dapat disimpan dan dimuat melalui Save System.
7. Mempertahankan equipment yang diperoleh selama run sebagai permanent progression.

#### 1.2 Confirmed Design Decisions

| Parameter | Final Decision |
|---|---|
| Equipment Model | Loot RPG |
| Rarity Tiers | 6 |
| Affix Generation | Fixed affix count berdasarkan rarity |
| Equipment Persistence | Semua equipment yang diperoleh selama run tetap persistent |
| Equipment Upgrade | Upgrade meningkatkan Base Stat saja |
| Inventory Capacity | Fixed 100 equipment slots |
| Equipment Stack | Tidak dapat ditumpuk |
| Consumable Stack | Dapat ditumpuk |
| Equipment Upgrade Location | Hub Blacksmith |
| Equipment Acquisition | Loot, reward, shop |

---

### 2. Inventory System

Inventory menyimpan seluruh item yang dimiliki pemain.

Inventory dibagi menjadi empat kategori:

| Category | Contents |
|---|---|
| Equipment | Main Hand, Second Hand, Head, Body, Leg, Foot, Accessory |
| Consumables | Potion, recovery item, buff item, revive item |
| Materials | Upgrade materials dan crafting materials |
| Key Items | Item khusus progression dan dungeon |

#### 2.1 Equipment Inventory

Equipment menggunakan fixed-slot inventory.

- Maximum capacity: 100 equipment slots.
- Setiap equipment menggunakan satu slot.
- Equipment yang sedang dipakai tidak menggunakan inventory slot.
- Equipment yang dilepas kembali masuk ke inventory.
- Equipment yang dijual atau dibuang mengosongkan slot.
- Equipment tidak dapat ditumpuk.
- Equipment yang memiliki Instance ID berbeda dianggap sebagai item berbeda.

Jika inventory penuh, equipment yang baru diperoleh tidak boleh dihapus atau ditimpa secara otomatis.

Sistem harus memberikan notifikasi bahwa inventory penuh.

#### 2.2 Consumable Inventory

Consumables dapat ditumpuk berdasarkan Item Definition yang sama.

Setiap consumable memiliki:

- Item ID
- Stack Count
- Maximum Stack
- Item Effect
- Usage Restriction

Contoh:

`Health Potion × 10`

Stack count bertambah ketika pemain memperoleh consumable identik.

Jika stack sudah mencapai Maximum Stack, item berikutnya menggunakan stack atau slot lain sesuai implementasi inventory consumable.

#### 2.3 Materials Inventory

Materials menggunakan stack-based inventory.

Materials digunakan oleh Blacksmith dan sistem crafting apabila fitur crafting ditambahkan.

Materials tidak memiliki rarity random atau equipment instance.

#### 2.4 Key Items

Key Items disimpan terpisah dari inventory equipment.

Key Items:

- Tidak menggunakan equipment slot.
- Tidak dapat dijual.
- Tidak dapat dibuang secara normal.
- Tidak dapat ditumpuk kecuali didefinisikan secara khusus.

---

### 3. Equipment System

Setiap karakter memiliki delapan equipment slots.

| Slot | Description |
|---|---|
| Main Hand | Weapon utama karakter |
| Second Hand | Off-hand weapon, shield, atau special item |
| Head | Helmet, hat, hood, atau headgear |
| Body | Armor, robe, atau outfit |
| Leg | Greaves, pants, atau leg armor |
| Foot | Boots, shoes, atau footwear |
| Accessory 1 | Accessory pertama |
| Accessory 2 | Accessory kedua |

Aturan slot:

- Main Hand dan Second Hand memiliki daftar kompatibilitas class yang terpisah dan mengikuti Strict Compatibility (Bab 5 §6). Tidak ada pengecualian berdasarkan rarity, level, atau item effect, termasuk Legendary.
- Setiap class memiliki minimal satu pilihan Second Hand yang valid.
- Tidak ada slot lock untuk weapon two-handed. Weapon yang secara visual atau lore dipegang dua tangan (misalnya greatsword atau bow) tetap hanya menempati Main Hand, dan Second Hand tetap dapat diisi sesuai daftar kompatibilitas class.
- Head, Body, Leg, Foot, dan Accessory dapat dipakai lintas class kecuali item tertentu memiliki restriction.
- Item bertipe Accessory dapat dipasang ke Accessory 1 atau Accessory 2.
- Satu Equipment Instance hanya dapat menempati satu slot.

Equipment dapat dipasang jika karakter memenuhi seluruh equipment requirements.

#### 3.1 Equipment Compatibility

Equipment memiliki:

- Equipment Type
- Equipment Slot
- Allowed Classes
- Allowed Character IDs
- Minimum Character Level

Allowed Classes menentukan class yang dapat menggunakan equipment.

Allowed Character IDs digunakan untuk equipment yang hanya dapat digunakan oleh karakter tertentu.

Equipment dapat memiliki class restriction, character restriction, keduanya, atau tidak memiliki restriction tertentu.

#### 3.2 Equip Rules

Ketika pemain memasang equipment:

1. Sistem memvalidasi equipment.
2. Sistem memvalidasi class compatibility.
3. Sistem memvalidasi character restriction.
4. Sistem memvalidasi level requirement.
5. Sistem memeriksa equipment slot. Accessory dipasang ke Accessory 1 atau Accessory 2 sesuai slot yang dipilih pemain.
6. Equipment yang sedang dipakai pada slot tersebut dilepas.
7. Equipment baru dipasang.
8. Character Stats dihitung ulang.
9. Equipment UI diperbarui.

Equipment yang tidak kompatibel tidak dapat dipasang.

Equipment yang dilepas kembali masuk ke inventory.

---

### 4. Equipment Rarity System

Equipment memiliki enam rarity tiers.

| Rarity | Affix Count | Special Effect |
|---|---:|---|
| Common | 0 | None |
| Uncommon | 1 | None |
| Rare | 2 | None |
| Epic | 3 | None |
| Legendary | 3 | 1 Legendary Effect |
| Mythic | 4 | 1 Mythic Effect |

Rarity menentukan jumlah affix, rarity-specific effect, dan rentang nilai stat.

Rarity tidak secara otomatis menentukan equipment mana yang paling sesuai untuk suatu build.

Equipment dengan rarity lebih rendah dapat tetap lebih berguna apabila affix-nya sesuai dengan kebutuhan karakter.

#### 4.1 Rarity Presentation

Setiap rarity memiliki identitas visual pada UI.

| Rarity | UI Color |
|---|---|
| Common | Gray |
| Uncommon | Green |
| Rare | Blue |
| Epic | Purple |
| Legendary | Orange |
| Mythic | Red |

Warna digunakan untuk membedakan rarity pada inventory, equipment screen, loot presentation, dan shop.

---

### 5. Equipment Base Stats

Equipment Definition memiliki Base Stats yang ditentukan oleh equipment designer.

Base Stats tidak dihasilkan secara random.

Contoh:

**Iron Sword**

- STR +10
- Physical Damage Bonus +5%

**Flame Staff**

- INT +12
- Fire Damage Bonus +8%

**Guardian Armor**

- VIT +15
- Physical Damage Taken Reduction +5%

Nilai di atas merupakan contoh.

Base Stats ditentukan berdasarkan:

- Equipment Type
- Equipment Level
- Equipment Rarity
- Equipment Design
- Equipment Progression Tier

#### 5.1 Equipment Level

Equipment memiliki Equipment Level.

Equipment Level menentukan kekuatan dasar equipment dan rentang nilai affix.

Equipment Level berbeda dari Character Level dan Dungeon Level.

Equipment dapat memiliki minimum Character Level requirement.

Contoh:

`Flame Staff — Equipment Level 10 — Required Character Level 10`

Equipment yang tidak memenuhi level requirement tidak dapat dipasang.

---

### 6. Random Affix System

Affix merupakan bonus tambahan yang dihasilkan secara random ketika equipment dibuat.

Affix dapat memberikan bonus stat atau efek tertentu.

#### 6.1 Affix Categories

| Category | Examples |
|---|---|
| Primary Stats | STR, INT, VIT, AGI |
| Secondary Stats | Speed, Critical Chance, Critical Damage |
| Physical | Physical Damage Bonus |
| Elemental | Elemental Damage Bonus |
| Element-Specific | Fire, Ice, Lightning, Earth, Wind, Water, Holy, Shadow |
| Defense | Physical Damage Taken Reduction |
| Elemental Resistance | Fire Resistance, Ice Resistance, dan lainnya |
| Status Resistance | Status Effect Resistance |
| QTE | Offensive QTE Window, Defensive QTE Window |

Affix dapat memiliki flat value atau percentage value.

##### 6.1.1 QTE Affix

| Affix | Valid Equipment Type | Uncommon | Rare | Epic | Legendary | Mythic |
|---|---|---|---|---|---|---|
| Offensive QTE Window | Main Hand, Second Hand (off-hand weapon atau special item), Accessory | 3–5% | 4–6% | 5–8% | 6–9% | 7–10% |
| Defensive QTE Window | Second Hand (shield), Foot, Accessory | 3–5% | 4–6% | 5–8% | 6–9% | 7–10% |

- Maksimal 1 affix QTE per item.
- Affix QTE sengaja dibatasi pada sedikit slot agar delapan slot tidak membuat cap window terlalu mudah dicapai.
- Weight affix QTE setengah dari weight affix standar.
- Common tidak memiliki affix, sehingga tidak bisa memiliki affix QTE.
- Semua modifier window dari equipment, talent, dan passive ditumpuk secara additive dengan cap bersama +25%.

Affix Definition menentukan:

- Affix ID
- Affix Name
- Stat Type
- Modifier Type
- Minimum Value
- Maximum Value
- Valid Equipment Types
- Valid Rarities
- Weight
- Description

#### 6.2 Affix Generation Rules

Affix generation menggunakan fixed affix count berdasarkan rarity.

Contoh:

Rare equipment selalu memiliki dua random affixes.

Epic equipment selalu memiliki tiga random affixes.

Legendary equipment selalu memiliki tiga random affixes.

Mythic equipment selalu memiliki empat random affixes.

Affix count tidak di-random.

#### 6.3 Affix Selection

Affix generation mengikuti aturan berikut:

1. Sistem mengambil affix pool berdasarkan Equipment Type.
2. Sistem memfilter affix berdasarkan rarity.
3. Sistem memfilter affix berdasarkan Equipment Level.
4. Sistem memfilter affix berdasarkan valid stat category.
5. Sistem memilih affix berdasarkan weight.
6. Sistem menentukan affix value.
7. Sistem menyimpan affix pada Equipment Instance.

Affix yang sama tidak boleh muncul dua kali pada satu equipment.

Affix harus memiliki minimum dan maximum value.

Equipment tidak boleh menghasilkan affix yang tidak kompatibel dengan equipment type-nya.

#### 6.4 Affix Value Generation

Affix Value ditentukan berdasarkan:

- Equipment Level
- Rarity
- Affix Definition
- Affix Value Range

Contoh:

`STR Affix — Equipment Level 10`

- Minimum: +5
- Maximum: +10

Nilai tersebut merupakan contoh.

Affix Value dihasilkan ketika equipment dibuat dan tidak berubah ketika equipment dipasang atau dilepas.

---

### 7. Legendary & Mythic Effects

Legendary dan Mythic equipment memiliki special effect.

Special Effect merupakan efek unik yang tidak masuk ke random affix pool biasa.

#### 7.1 Legendary Effect

Legendary Effect dapat memberikan interaksi khusus dengan:

- Damage Type
- Elemental Affinity
- Status Effect
- Critical Hit
- QTE
- Skill
- Character Class

Contoh:

**Infernal Edge — Infernal Resonance**

Physical attacks have a chance to apply Burn.

#### 7.2 Mythic Effect

Mythic Effect merupakan special effect untuk Mythic equipment.

Mythic equipment memiliki empat random affixes dan satu Mythic Effect.

Mythic Effect dapat memiliki mekanik yang lebih kompleks dibandingkan Legendary Effect.

Contoh:

**Frostheart Staff — Absolute Zero**

Elemental damage dealt to Frozen targets gains an additional damage bonus.

Efek tersebut merupakan contoh desain, bukan final gameplay value.

Special Effect tidak boleh melewati aturan dasar Damage & Status Effect System kecuali secara eksplisit dirancang sebagai exception.

#### 7.3 Batasan Special Effect terkait QTE

Legendary dan Mythic Effect yang berinteraksi dengan QTE hanya boleh berupa:

1. Modifier window hingga 12% (tetap tunduk pada cap bersama +25%).
2. Satu efek yang dipicu Perfect. Efek pemulihan resource dibatasi maksimal 2% Max HP atau Max MP per pemicu, dan maksimal satu pemicu per giliran karakter.

Special Effect tidak boleh mengubah nilai reduksi Defensive QTE, batas reduksi serangan Heavy, memberi grade otomatis, atau memengaruhi Lucky.

---

### 8. Loot Generation System

Equipment dapat diperoleh melalui:

- Normal Enemy Drops
- Elite Enemy Drops
- Boss Rewards
- Dungeon Chests
- Dungeon Events
- Dungeon Shop
- Hub Armory

#### 8.1 Loot Generation Flow

Equipment generation mengikuti urutan:

1. Select Equipment Base.
2. Determine Equipment Level.
3. Determine Equipment Rarity.
4. Determine Base Stats.
5. Select Affixes.
6. Generate Affix Values.
7. Assign Legendary or Mythic Effect.
8. Generate Equipment Instance ID.
9. Add Equipment to Inventory.
10. Save Equipment Instance.

#### 8.2 Dungeon Tier

Dungeon Tier menentukan loot progression.

Dungeon 1–2: Tier 1
Dungeon 3–4: Tier 2
Dungeon 5–6: Tier 3
Dungeon 7: Tier 4
Dungeon 8: Tier 5

Dungeon Tier memengaruhi:

- Equipment Level Range
- Rarity Distribution
- Affix Value Range
- Equipment Base Availability
- Legendary and Mythic Drop Chance

Rarity distribution dan loot weights merupakan balancing parameters.

Boss memiliki loot table khusus dan dapat memberikan guaranteed equipment rewards sesuai boss design.

---

### 9. Equipment Upgrade System

Equipment dapat di-upgrade melalui Blacksmith di Hub.

Equipment Upgrade hanya meningkatkan Base Stats.

Upgrade tidak mengubah:

- Equipment Rarity
- Random Affix
- Affix Value
- Legendary Effect
- Mythic Effect
- Equipment Definition

#### 9.1 Upgrade Level

Equipment memiliki Upgrade Level.

Contoh:

`Infernal Edge +0 → Infernal Edge +1 → Infernal Edge +2`

Maximum Upgrade Level ditentukan oleh Equipment Definition atau global equipment rules.

#### 9.2 Upgrade Effect

Upgrade meningkatkan Base Stat berdasarkan upgrade level.

Contoh:

| Upgrade | Base Stat Multiplier |
|---|---:|
| +0 | 100% |
| +1 | 105% |
| +2 | 110% |
| +3 | 115% |
| +4 | 120% |
| +5 | 125% |

Angka merupakan contoh balancing values.

Upgrade menggunakan Hub Gold dan materials.

Upgrade tidak mengubah Affix Values.

---

### 10. Equipment Persistence

Equipment Persistence menggunakan Model C.

**Seluruh equipment yang diperoleh selama dungeon run menjadi persistent inventory.**

Equipment yang diperoleh melalui:

- Enemy Drops
- Boss Rewards
- Dungeon Chests
- Dungeon Events
- Dungeon Shop Purchases

tetap dimiliki pemain meskipun run gagal.

Equipment yang diperoleh selama run tidak dihapus ketika Run Gold hilang.

#### 10.1 Run Failure

Ketika seluruh party mati:

- Run Gold dihapus.
- Dungeon progression di-reset sesuai run failure rules.
- Run state di-reset.
- Equipment yang diperoleh selama run tetap persistent.
- Equipment yang sebelumnya dimiliki pemain tetap persistent.

#### 10.2 Dungeon Clear

Ketika dungeon berhasil diselesaikan:

- Run Gold yang tersisa ditransfer ke Hub Gold.
- Equipment yang diperoleh selama run tetap persistent.
- Equipment yang dibeli selama run tetap persistent.
- Dungeon progression berlanjut sesuai sequential progression rules.

#### 10.3 True New Game

New Game merupakan reset campaign yang disengaja.

True New Game menghapus persistent progression sesuai New Game rules.

Equipment Persistence Model C berlaku terhadap run failure, bukan terhadap reset campaign yang disengaja oleh pemain.

---

### 11. Character Stat Integration

Equipment menggunakan stat modifier system.

Equipment tidak mengubah Base Stats karakter secara permanen.

Final Stats dihitung berdasarkan:

- Character Base Stats
- Equipment Flat Bonuses
- Equipment Percentage Bonuses
- Other Stat Modifiers

Contoh:

| Stat | Base | Equipment | Final |
|---|---:|---:|---:|
| STR | 30 | +20 | 50 |
| INT | 12 | +8 | 20 |
| VIT | 25 | +10 | 35 |
| Speed | 15 | +5 | 20 |

Equipment modifier dihitung ulang ketika:

- Equipment dipasang.
- Equipment dilepas.
- Equipment diganti.
- Equipment di-upgrade.
- Character Base Stats berubah.

Equipment modifier tidak boleh dihitung dua kali.

Damage Bonus dan Damage Taken modifier mengikuti stacking rules dari Damage & Status Effect System.

---

### 12. Inventory UI

Inventory UI memiliki:

- Category Tabs
- Equipment Grid
- Item Details
- Rarity Color
- Item Sorting
- Item Filtering
- Equipment Comparison
- Equip
- Unequip
- Sell
- Use Consumable
- Discard

Equipment detail menampilkan:

- Item Name
- Rarity
- Equipment Level
- Equipment Type
- Base Stats
- Random Affixes
- Special Effect
- Required Character Level
- Allowed Classes
- Sell Value

Equipment yang sedang dipakai memiliki visual indicator.

---

### 13. Equipment UI

Equipment UI menampilkan:

- Character Preview
- Main Hand Slot
- Second Hand Slot
- Head Slot
- Body Slot
- Leg Slot
- Foot Slot
- Accessory 1 Slot
- Accessory 2 Slot
- Character Stats
- Equipment Detail
- Equipment Comparison

Ketika pemain memilih equipment slot, inventory memfilter equipment yang kompatibel.

Equipment comparison menampilkan perubahan stat sebelum equipment dipasang.

---

### 14. Equipment Selling

Equipment dapat dijual melalui Shop.

Sell Value ditentukan berdasarkan:

- Equipment Base Value
- Equipment Level
- Rarity
- Upgrade Level

Contoh formula:

Sell Value = Base Value × Rarity Multiplier × Level Multiplier × Upgrade Multiplier

Equipment yang sedang dipakai tidak dapat dijual tanpa melepasnya terlebih dahulu.

Legendary dan Mythic equipment memerlukan confirmation sebelum dijual.

Key Items tidak dapat dijual.

---

### 15. Unity Implementation Architecture

Sistem menggunakan pemisahan antara Equipment Definition dan Equipment Instance.

#### 15.1 Equipment Definition

Equipment Definition menggunakan ScriptableObject.

Definition menyimpan data dasar equipment.

Fields:

- Equipment ID
- Display Name
- Equipment Type
- Equipment Slot
- Allowed Classes
- Allowed Character IDs
- Base Stats
- Affix Pool
- Rarity Restrictions
- Minimum Character Level
- Maximum Upgrade Level
- Legendary Effect ID
- Mythic Effect ID
- Icon
- Description
- Base Value

Definition tidak menyimpan random stat hasil loot.

#### 15.2 Equipment Instance

Equipment Instance menyimpan data equipment yang dimiliki pemain.

Fields:

- Instance ID
- Equipment Definition ID
- Equipment Level
- Rarity
- Upgrade Level
- Generated Affixes
- Affix Values
- Special Effect ID
- Bound Character ID
- Is Equipped

Equipment Instance menggunakan unique ID.

Equipment yang memiliki Definition ID sama tetap memiliki Instance ID berbeda.

#### 15.3 Inventory Runtime

Inventory Runtime menyimpan:

- Equipment Instances
- Consumable Stacks
- Materials
- Key Items

Inventory Runtime menyediakan:

- Add Item
- Remove Item
- Find Item
- Sort Inventory
- Filter Inventory
- Check Capacity
- Save Inventory
- Load Inventory

#### 15.4 Equipment Manager

Equipment Manager mengatur:

- Equip
- Unequip
- Replace Equipment
- Validate Compatibility
- Calculate Equipment Modifiers
- Update Character Stats

Equipment Manager tidak menyimpan equipment definition secara permanen di scene.

#### 15.5 Equipment Loot Generator

Equipment Loot Generator mengatur:

- Select Equipment Base
- Determine Equipment Level
- Determine Rarity
- Select Affixes
- Generate Affix Values
- Assign Special Effect
- Create Equipment Instance

Equipment generation harus menggunakan deterministic seed ketika diperlukan oleh run state dan save/load.

---

### 16. Save & Load Integration

Save System menyimpan Equipment Instances.

Equipment Definition tidak disimpan sebagai object reference langsung.

Save Data menggunakan Equipment Definition ID.

Contoh:

```text
EquipmentInstanceSaveData
├── InstanceID
├── EquipmentDefinitionID
├── EquipmentLevel
├── Rarity
├── UpgradeLevel
├── GeneratedAffixes
├── AffixValues
├── SpecialEffectID
└── BoundCharacterID
```

Ketika Load:

1. Read Equipment Instance Save Data.
2. Resolve Equipment Definition ID.
3. Reconstruct Equipment Instance.
4. Restore Affixes.
5. Restore Equipment Level.
6. Restore Rarity.
7. Restore Upgrade Level.
8. Restore Equipment Ownership.
9. Restore Equipped State.
10. Recalculate Character Stats.

Equipment Instance harus dapat direkonstruksi tanpa melakukan random generation ulang.

---

### 17. Development Milestones

#### Milestone 1 — Data Foundation

- Item Definition
- Equipment Definition
- Equipment Instance
- Rarity
- Affix Definition
- Affix Generation

#### Milestone 2 — Runtime Systems

- Inventory Runtime
- Equipment Manager
- Equipment Compatibility
- Equipment Stat Modifier
- Equipment Upgrade

#### Milestone 3 — UI

- Inventory Screen
- Equipment Screen
- Item Detail
- Equipment Comparison
- Equip / Unequip

#### Milestone 4 — Loot Integration

- Enemy Drops
- Boss Rewards
- Dungeon Chests
- Dungeon Shop
- Hub Armory

#### Milestone 5 — Persistence

- Equipment Save Data
- Inventory Save Data
- Run Failure Persistence
- Dungeon Clear Persistence
- Save Slot Integration

#### Milestone 6 — Polish

- Equipment Sorting
- Inventory Filtering
- Loot Presentation
- Rarity Visual Effects
- Equipment Comparison Feedback
- Legendary / Mythic Effect Presentation

---

### 18. Acceptance Criteria

Inventory & Equipment System v1.0 dianggap selesai ketika:

1. Equipment dapat dihasilkan melalui loot generation.
2. Equipment memiliki rarity yang benar.
3. Equipment memiliki fixed affix count berdasarkan rarity.
4. Equipment memiliki random affix values.
5. Equipment memiliki unique Instance ID.
6. Equipment dapat dimasukkan ke inventory.
7. Equipment dapat dipasang dan dilepas.
8. Equipment compatibility berfungsi.
9. Equipment dapat mengubah Character Stats.
10. Equipment dapat dibandingkan.
11. Equipment dapat dijual.
12. Equipment dapat di-upgrade.
13. Equipment dapat disimpan dan dimuat.
14. Equipment yang diperoleh selama run tetap persistent ketika run gagal.
15. Equipment yang diperoleh selama run tetap persistent setelah dungeon clear.
16. Equipment tidak menghasilkan stat modifier ganda.
17. Equipment tidak dapat dipasang oleh karakter yang tidak kompatibel.
18. Inventory tidak kehilangan equipment ketika kapasitas penuh.
19. Affix dan Special Effect QTE mengikuti batasan §6.1.1 dan §7.3.

---

Revision v1.1 (4 November 2026): kategori affix QTE (§6.1.1) dan batasan Special Effect terkait QTE (§7.3) ditambahkan sesuai Bab 12 (QTE System).
Revision v1.2 (4 November 2026): equipment slots diubah dari lima menjadi delapan (Main Hand, Second Hand, Head, Body, Leg, Foot, Accessory 1, Accessory 2), selaras dengan Character Creation System dan GDD Draft v0.1. Valid Equipment Type affix QTE disesuaikan. Weapon two-handed hanya menempati Main Hand (keputusan 4 November 2026).

## Bab 14 — Shop dan Run Economy

> Sumber: GDD Shop System v1.0.

### 1. Overview

The game contains two shop contexts: a Dungeon Shop available during a dungeon run and a set of specialized shops/facilities located in the Hub. The Dungeon Shop supports survival and tactical decisions during the current run. Hub facilities support between-run preparation, character growth, and recovery of fallen characters.

Character death is individual rather than immediately run-ending. A run can continue while at least one party character remains alive. The run fails when all party characters are dead.

### 2. Design Goals

- Give Dungeon Shop purchases meaningful risk because Run Gold is lost on a failed run.

- Reward successful dungeon completion by carrying all remaining Run Gold into the Hub without a conversion penalty.

- Separate Hub services by purpose so players can quickly identify where to shop, upgrade, train, or revive.

- Make character death consequential without ending a run after the first fallen character.

- Create a clear recovery path for dead characters through the rare Dungeon revive item or the Hub Temple.

### 3. Shop Types

#### 3.1 Dungeon Shop

The Dungeon Shop is encountered inside a dungeon run. It uses Run Gold and is intended to help the party respond to current-run needs. Its exact appearance, placement frequency, inventory size, stock rules, and refresh rules have not yet been defined.

- May offer consumables.

- May offer equipment.

- May offer run-support items.

- Can offer a rare and expensive revive item.

#### 3.2 Hub Shops and Facilities

The Hub contains five distinct facilities. Hub purchases and services use the persistent Hub Gold balance.

|**Facility Name**|**Primary Function**|**Confirmed Scope**|
|---|---|---|
|Shop|Consumables|Purchase consumable items, including<br>recoveryitems.|
|Armory|Equipment|Purchase equipment for characters.|
|Blacksmith|Equipment upgrades|Upgrade owned equipment.|
|Training Ground|Skills|Manage character skills and upgrade<br>skills.|
|Temple|Revive|Revive characters who are dead.|

### 4. Gold Economy

#### 4.1 Gold Balances

|**Balance**|**Description**|**Persistence**|
|---|---|---|
||Gold acquired and held during the||
|Run Gold|current dungeon run; spent at Dungeon<br>Shop.|Temporary; cleared on run failure.|

Hub Gold

||**GAME DESIGN DOCUMENT  |  SHOP SYSTEM**|
|---|---|
|Gold secured by completing a dungeon;|Persistent between runs, subject to the|
|spent at Hub facilities.|game's overall reset rules.|

#### 4.2 Successful Dungeon Completion

- All remaining Run Gold is transferred to Hub Gold.

- The transfer is 1:1; there is no fee, loss, or conversion deduction.

- The character party may return to the Hub with one or more characters dead; their status remains dead until revived.

#### 4.3 Failed Run

- All Run Gold remaining at the time of failure is lost.

- Previously secured Hub Gold is not affected by the Run Gold loss.

- The run failure and dungeon reset follow the game's established run-failure rules.

#### 4.4 Example

The party earns 500 Run Gold and spends 150 at the Dungeon Shop. If the dungeon is completed, the remaining 350 Gold is transferred in full to Hub Gold. If the entire party dies before completion, the remaining 350 Run Gold is lost.

### 5. Character Death and Run Failure

#### 5.1 Individual Death

When a character dies, that character cannot participate in combat while dead. The rest of the party may continue the dungeon as long as at least one character is alive.

|**Party State**|**Run Result**|
|---|---|
|One character is dead;at least one is alive|Run continues.|
|Multiple characters are dead;at least one is alive|Run continues.|
|Allpartycharacters are dead|Run fails.|

#### 5.2 Run Success Condition

The player must complete the dungeon while keeping at least one party character alive. A character who is dead at the end of a successful run remains dead when the party returns to the Hub.

#### 5.3 Run Failure Condition

If every party character dies during the dungeon run, the run is considered failed. Under the game's established rule, the player returns to the Main Menu and the dungeon progression/run is reset; a new game is required to begin again. Run Gold is lost.

### 6. Revive System

#### 6.1 Dungeon Revive Item

A character can be revived during a run by using a special revive item. The item is defined as rare and expensive, and it is available for purchase only through the Dungeon Shop when stocked.

- The item revives a dead character so that they can return to combat during the current run.

- Its exact item name, price, stock quantity, drop sources, and use restrictions are TBD.

#### 6.2 Hub Revive — Temple

The Temple provides a service to revive dead characters after a successful return to the Hub. This allows players to restore their party without needing to spend a rare revive item during the dungeon.

The Temple's exact cost, requirements, revival presentation, and any limits are TBD.

### 7. Player Flow

1. Prepare the party in the Hub using Shop, Armory, Blacksmith, Training Ground, and Temple as needed.

2. Enter a dungeon; the run begins with its Run Gold balance.

3. Acquire and spend Run Gold at Dungeon Shop(s), including the option to purchase a rare revive item.

4. Continue through the dungeon while at least one party character remains alive.

5. If the dungeon is completed, transfer all remaining Run Gold to Hub Gold and return to the Hub.

6. Use Hub facilities to buy, upgrade, manage skills, and revive dead characters before the next run.

7. If all party characters die, the run fails and remaining Run Gold is lost; apply the game's run reset rules.

### 8. Confirmed Rules and Open Design Items

#### 8.1 Confirmed

- There are two shop contexts: Dungeon Shop and Hub shops.

- Hub facilities are named Shop, Armory, Blacksmith, Training Ground, and Temple.

- Run Gold is temporary; all remaining Run Gold transfers to Hub Gold after successful dungeon completion.

- Run Gold is lost if the run fails.

- A character's death does not end the run if at least one party character remains alive.

- The run fails when all party characters are dead.

- A rare, expensive revive item is purchasable at the Dungeon Shop.

- Dead characters can be revived at the Hub's Temple.

#### 8.2 TBD — Not Yet Defined

- Dungeon Shop appearance frequency, placement, inventory size, stock, and refresh behavior.

- Consumable and equipment inventory lists, prices, and purchase limits.

- Revive item name, price, stock, and any target/use restrictions.

- Temple revive price, requirements, and possible restrictions.

- Blacksmith upgrade materials, costs, levels, and success/failure rules.

- Training Ground skill management and upgrade costs/rules.

- Whether Hub Gold persists across all new-game/reset cases beyond the currently established run-failure handling.

### 9. Terminology

|**Term**|**Meaning**|
|---|---|
|Dungeon Shop|Shopencountered duringa dungeon run.|
|Hub|Town/base area between dungeon runs.|
|Run Gold|TemporaryGold held duringa dungeon run.|
|Hub Gold|Gold secured after successful dungeon completion.|
||**GAME DESIGN DOCUMENT  |  SHOP SYSTEM**|
|Revive|Restore a dead character to a living/active state.|
|Dead|Character state in which the character cannot participate in<br>combat until revived.|
|Run Failure|Allpartycharacters are dead.|

_End of document_

# Bagian VI — Presentasi

## Bab 15 — Art Direction dan Visual Production

> Sumber: GDD Visual Direction v0.2 §13. Penomoran §13.x dipertahankan.

### 13. Art Direction dan Visual Production

Bagian ini menetapkan standar visual untuk mengganti presentasi 8-bit/pixel art lama menjadi Modern Pixel Art dengan nuansa anime JRPG dan dark fantasy. Standar ini berlaku untuk seluruh game: karakter, musuh, environment, efek skill, icon, portrait, serta UI.

#### 13.1 Visual Identity

- Style utama: Modern Pixel Art — pixel art dengan detail, shading, siluet, dan animasi yang lebih kaya daripada 8-bit klasik.

- Nuansa: anime JRPG dengan dark fantasy; dunia terasa berbahaya, misterius, dan memiliki kontras visual yang kuat.

- Prinsip keterbacaan: siluet karakter dan musuh harus terbaca jelas saat combat; efek skill tidak boleh menutupi informasi target atau status penting.

- Konsistensi: seluruh aset menggunakan grid pixel, palette, pencahayaan, dan skala yang konsisten.

- UI ikut diperbarui ke visual modern pixel art; hindari UI yang tampak terpisah dari dunia game.

#### 13.2 Resolution, Camera, dan World Scale

Resolusi referensi adalah 640 × 360 (16:9). Resolusi ini menjadi baseline komposisi dan layout UI, bukan batasan resolusi output monitor. Game menggunakan kamera orthographic.

|**Parameter**|**Standar**|
|---|---|
|**Reference resolution**|640 × 360|
|**Aspect ratio**|16:9|
|**Combat camera**|Side-view|
|**Projection**|Orthographic|
|**Pixels Per Unit (PPU)**|32|
|**Tile baseline**|32 × 32 px = 1 world unit|
|**Character baseline**|64 × 96 px = 2 × 3 world units|
|**Camera position (prototype)**|(0, 0, -10)|
|**Orthographic Size(prototype)**|5.625|

#### 13.3 Sprite Size Guide

|**Asset**|**Canvasguideline**|**Catatan**|
|---|---|---|
|**Character standar**|64 × 96 px|Baseline karakter party; sisakan ruang<br>untuk senjata/efek bila diperlukan.|
|**Character tinggi/besar**|64 × 112 px|Untuk variasi proporsi atau armor besar.|
|**Character kecil**|48 × 80 px|Untuk karakter dengan siluet lebih<br>mungil.|
|**Enemy standar**|64 × 80 px|Skala musuh reguler.|
|**Enemy besar**|96 × 128 px|Elite atau monster berukuran besar.|
|**Boss**|128 × 160 px|Boss standar; ukuran dapat<br>menyesuaikan desain.|
|**Boss besar**|192 × 192 px|Boss dengan footprint visual besar; perlu<br>uji keterbacaan.|

#### 13.4 Combat Composition

- Party player ditempatkan di sisi kiri arena; musuh di sisi kanan.

- Pertarungan menggunakan tampilan side-view. Posisi, jarak, dan arah sprite dibuat konsisten agar target dan urutan aksi mudah dibaca.

- UI combat ditempatkan pada area bawah layar; ruang arena utama dipertahankan agar animasi serangan dan efek skill tidak tertutup UI.

- Kamera tidak perlu mengikuti setiap karakter dalam combat turn-based; framing arena harus stabil.

- Pastikan komposisi tetap terbaca pada resolusi referensi 640 × 360 dan aspect ratio yang didukung.

#### 13.5 Animation Baseline

|**Animation**<br>**Idle**|**Frameguideline**<br>4–6|**Catatan**<br>Loop; gerakan kecil untuk menjaga|
|---|---|---|

|||karakter tetap hidup.|
|---|---|---|
|**Walk**|6–8|Loop; gunakan bila movement di map<br>atau arena diperlukan.|
|**Attack**|6–10|Timing harus mendukung impact dan<br>animasi senjata.|
|**Cast**|6–10|Untuk skill elemental atau magic; VFX<br>dapat menjadi layer terpisah.|
|**Hit**|3–5|Respons singkat, jelas, dan tidak<br>menghambat flow turn.|
|**Defend**|2–4|Pose defensif yang mudah dikenali.|
|**Victory**|6–8|Animasi pendek setelah encounter<br>selesai.|
|**Death**|6–10|Jelas menunjukkan karakter/enemy tidak<br>lagi aktif.|

Jumlah frame di atas adalah guideline produksi awal, bukan batas keras. Frame rate animasi dan durasi final harus diuji dengan pacing turn-based serta QTE.

#### 13.6 Unity Import dan Pixel-Perfect Setup

- Texture Type: Sprite (2D and UI).

- Filter Mode: Point (no filter).

- Compression: None.

- Mip Maps: Off.

- Wrap Mode: Clamp.

- Alpha Is Transparency: On jika aset memakai transparansi.

- Gunakan Pixels Per Unit 32 untuk aset world sesuai standar visual.

- Gunakan 2D Pixel Perfect Camera; Reference Resolution 640 × 360.

- Aktifkan pixel snapping bila sesuai dengan kebutuhan animasi; validasi pergerakan sub-pixel agar tidak

   - menghasilkan jitter.

- Jangan mengubah PPU secara individual tanpa alasan produksi yang terdokumentasi.

#### 13.7 UI Art Direction

- UI memakai bahasa visual Modern Pixel Art yang sama dengan world art: panel, border, icon, dan ornament dibuat konsisten.

- Canvas Scaler menggunakan Scale With Screen Size dengan Reference Resolution 640 × 360 dan Match sekitar 0.5 sebagai baseline.

- Pixel icon harus memakai point filtering; ikon status dan damage type perlu mudah dibedakan dalam ukuran kecil.

- Gunakan 9-slicing untuk panel yang perlu berubah ukuran tanpa merusak border pixel.

- Font harus tetap terbaca pada resolusi referensi; hindari font pixel yang terlalu dekoratif untuk informasi combat.

- Informasi penting—HP, resource, turn order, target, resistance, status, dan hasil QTE—memiliki hierarki visual yang jelas.

- Warna tidak menjadi satu-satunya pembeda status atau damage type; tambahkan ikon, label, atau bentuk.

#### 13.8 VFX dan Combat Feedback

- VFX Physical dan Elemental Damage memiliki bahasa visual yang berbeda.

- Setiap elemen dapat memiliki warna dan motif tersendiri, tetapi efek tidak boleh mengaburkan sprite target atau telegraph penting.

- Damage number, status application, resistance feedback, dan QTE result harus memiliki timing serta posisi konsisten.

- Pisahkan logic combat dari animasi: animation event dapat memicu timing visual, tetapi damage, hit validation, dan status resolution tetap dikendalikan sistem combat.

- Efek hit/cast/impact harus dapat diatur durasinya agar pacing turn-based tetap responsif.

#### 13.9 Asset Pipeline dan Struktur Folder

Struktur folder awal yang direkomendasikan:

Assets/ Art/ Characters/ Enemies/ Bosses/ Environment/ UI/ VFX/ Portraits/ Animations/ Audio/ Prefabs/ Scenes/ Scripts/ ScriptableObjects/

#### 13.10 Prototype Acceptance Criteria

- Satu karakter party (Fighter) memiliki idle, attack, hit, defend, dan death/victory placeholder atau final.

- Satu karakter caster (Wizard) menunjukkan casting dan minimal satu efek elemental.

- Dua tipe enemy memiliki siluet dan animasi hit yang terbaca.

- Satu background arena side-view menggunakan standar 32 PPU dan komposisi 640 × 360.

- Combat UI menampilkan HP/resource, target, dan hasil aksi tanpa menutupi arena secara berlebihan.

- Pixel-perfect rendering diuji pada resolusi referensi dan satu resolusi layar yang lebih tinggi.

- Tidak ada blur akibat filtering; tidak ada jitter mencolok pada animasi atau kamera.

- Feedback physical dan elemental dapat dibedakan tanpa harus membaca teks panjang.

#### 13.11 Open Decisions

- Palette final per region/dungeon dan aturan pencahayannya.

- Apakah character portrait menggunakan pixel art atau ilustrasi non-pixel.

- Apakah map exploration juga side-view atau memakai sudut kamera berbeda.

- Frame rate animasi final dan metode sprite-sheet/animator yang dipilih.

- Skema final warna dan motif untuk masing-masing elemental damage.

- Batas aspect ratio yang didukung selain 16:9 serta kebijakan letterbox/cropping.

**Revision note (4 November 2026):** §5.1–5.3 disinkronkan dengan Bab 12 (QTE System).

## Bab 16 — Visual Style Guide

> Sumber: Game Visual Style Guide v1.0.

### 1. Document Overview

This guide translates the game’s established design identity into actionable art direction for future production. It is intended for artists, animators, UI designers, VFX artists, and Unity developers.

#### Status conventions

|**Label**|**Meaning**|
|---|---|
|CONFIRMED|Explicitly supported by the available design documents.|
|RECOMMENDED|A proposed visual standard to improve consistency; it requires team<br>approval before being treated as binding.|
|OPEN|Not specified, inconsistent across sources, or awaiting a decision.|
|ASSET NOT YET CREATED|The guide describes a target or planned asset; it is not evidence of<br>completed production.|

Primary goal: build a readable, cohesive, modern pixel-art dark fantasy RPG presentation in which combat information remains legible under visual pressure.

### 2. Visual Identity

**RECOMMENDED — art direction**

#### Artistic pillars

|**Pillar**|**Direction**|**Practical test**|
|---|---|---|
|Readable first|Silhouettes, target cues, damage type, and<br>status must remain distinguishable at<br>gameplay scale.|Can a player identify the acting unit and key<br>threat without zooming?|
|Dark fantasy, not visual mud|Use deep values and restrained saturation,<br>but preserve focal contrast and color<br>separation.|Can foreground characters be separated<br>from the environment?|
|Anime JRPG expressiveness|Use expressive poses, iconic class shapes,<br>and stylized impact timing rather than<br>realistic anatomy.|Does the pose communicate the action<br>instantly?|
|Modern pixel craft|Deliberate clusters, clean edges, controlled<br>ramps, and purposeful animation.|Does the asset look intentional at native<br>scale?|
|Gameplay-integrated effects|VFX reinforce the action and mechanics, not<br>obscure the battlefield.|Can targets and UI still be read during the<br>effect?|

#### Avoid

- Uncontrolled pixel sizes or blurry scaling.

- Excessive outlines that flatten all materials into the same visual weight.

- High-saturation effects that compete with UI and target indicators.

- Decorative VFX that imply mechanics not supported by the game rules.

- Treating a proposed design as an approved or existing asset.

### 3. Modern Pixel Art Standards

**CONFIRMED baseline: Modern Pixel Art direction; reference resolution 640 × 360; 16:9; orthographic camera; PPU 32; standard tile 32 × 32; typical character canvas 64 × 96. Treat these as design targets unless current implementation is explicitly verified.**

|**Topic**|**Standard**|
|---|---|
|Pixel density|Use a consistent base pixel scale. Avoid mixing assets with visibly<br>different pixel sizes in one scene.|
|Clusters|Build forms with intentionalpixel clusters;avoid noisysingle-pixel detail unless it serves a focal purpose.|
|Outlines|RECOMMENDED: use selective dark outlines on characters and<br>interactable objects. Interior lines should describe form, not trace<br>every edge.|
|Shading|Use compact value ramps and clear light direction. Prefer grouped<br>shadow shapes over airbrush gradients.|
|Dithering|Use sparingly for texture or transitions; do not use as a substitute for<br>clear value design.|
|Anti-aliasing|RECOMMENDED: avoid conventional smoothing on pixel-art sprites.<br>Preserve hard pixel edges at native scale.|
|Scaling|Use integer scaling where possible. Avoid non-integer transform<br>scaling that creates uneven pixels.|
|Readability|Test sprites at intended gameplay size and against both dark and<br>bright backgrounds.|

#### Recommended sprite review sizes

Review every sprite at 100% native resolution and at the actual in-game display scale. If the game renders at a different internal resolution, approve assets only after checking the final scaled output.

### 4. Color Palette and Color Language

**RECOMMENDED palette system — HEX values are starting swatches, not already implemented art assets.**

|**Palette role**|**HEX**|**Usage**|
|---|---|---|
|Ink / deepest shadow|#171824|Deepest outlines, UI recesses, night values.|
|Dark slate|#292B3D|Panels, secondary shadows, dark<br>environment masses.|
|Muted violet|#454765|Secondary shadow ramp and magical<br>neutral.|
|Mid slate|#777A98|Muted UI text, stone and metal midtones.|
|Pale parchment|#E4E0D2|Readable light text and highlights.|
|Warm highlight|#F4DDA0|Gold, warm focal accents, reward emphasis.|
|Blood accent|#A83D4B|Danger, wounds, selected dark-fantasy<br>accents.|
|Life green|#6FAD79|Recovery, nature motifs, positive feedback.|
|Arcane blue|#6D9BCB|Magic-like interface accents where<br>appropriate.|

#### Element palette proposals

|**Element / type**|**Primary HEX**|**Secondary HEX**|**Shape / motion cue**|
|---|---|---|---|
|Fire|#F06A3B|#FFD06A|Flame tongues; upward,<br>turbulent motion.|
|Ice|#79CDEB|#E1F7FF|Crystalline facets; sharp,<br>outward freeze.|
|Lightning|#F2D84A|#FFF5A3|Angular zigzags; abrupt<br>branching.|
|Earth|#A98258|#D7BD8C|Blocky shards; heavy, grounded<br>movement.|
|Wind|#A8D9C8|#E7FFF4|Curved streaks; sweeping arcs.|
|Water|#4C9DD8|#A8E5F6|Fluid ribbons; flowing and<br>curling.|
|Holy|#F3E7B5|#FFFFFF|Soft circles, rays, clean radial<br>bursts.|
|Shadow|#76539A|#C0A0E8|Tapered forms; inward pull and<br>drifting wisps.|
|Physical|#C7C7CE|#F2F2F5|Impact lines, weapon trails,<br>sparks.|
|Almighty|#D6A7E8|#F5DDFB|Distinct neutral-magenta|

geometry; define final treatment per skill.

Status and rarity colors should use the same palette logic as the relevant UI icons and effects. Color alone must not carry meaning: pair it with shape, icon, animation, or label.

### 5. Lighting and Atmosphere

#### RECOMMENDED

- Use a coherent primary light direction per scene; maintain readable character value separation.

- Keep ambient darkness atmospheric, not so low that silhouettes and combat cues disappear.

- Reserve the brightest values for focal characters, interactable elements, important effects, and UI feedback.

- Use localized light sources—torches, crystals, spells—as controlled accents.

- Avoid bloom or blur that destroys pixel edges; any post-processing should be subtle and tested at target resolution.

- Dungeon-specific lighting recipes remain OPEN until dungeon themes are approved.

### 6. Character Art Direction

**RECOMMENDED framework; class-specific details should follow the approved class and skill documentation. All character art below is planned direction, not completed asset work.**

|**Class**|**Silhouette direction**|**Costume / motif**|**Animation character**|
|---|---|---|---|
|Warrior|Broad, stable, armored outline.|Layered armor, strong<br>shoulders, defensive shapes.|Weighty anticipation and<br>grounded recovery.|
|Knight|Tall, shield-forward, formal<br>silhouette.|Structured plate, heraldic<br>geometry.|Measured guard, deliberate<br>blocks.|
|Ranger|Lean, angled, mobile profile.|Light gear, bow or ranged<br>weapon profile.|Quick draw, clean recoil, precise<br>motion.|
|Martial Artist|Compact, dynamic, open<br>silhouette.|Wraps, flexible clothing, fist/foot<br>emphasis.|Fast rhythm, clear combo poses.|
|Assassin|Narrow, asymmetric, concealed<br>outline.|Hood, layered cloth, paired<br>weapon shapes.|Low anticipation, sharp burst<br>movement.|
|Cleric|Upright, calm, readable support<br>silhouette.|Robes, sacred motifs, staff or<br>holy focus.|Open gestures and soft<br>restorative accents.|
|Wizard|Distinct robe and staff<br>silhouette.|Arcane motifs, layered sleeves,<br>spell focus.|Cast anticipation and element-<br>specific release.|
|Warlock|Angular, restrained, ominous<br>profile.|Curses, sigils, dark ornament.|Controlled gestures; persistent<br>curse motifs.|
|Druid|Organic, layered, nature-themed<br>silhouette.|Leaves, bark, organic shapes;<br>Nature is thematic only.|Flowing, adaptive motions.|

Character production sheet should include front/side reference as needed, silhouette thumbnail, palette swatches, idle, attack, hit, skill, and defeat poses. Weapon assignments must be taken from the current class source of truth, not inferred.

### 7. Enemy and Boss Art Direction

#### RECOMMENDED

- Build enemy silhouettes around role and threat: small/quick, heavy/guarding, ranged/caster, elite, and boss. These are visual categories, not new gameplay rules.

- Make enemy focal features readable at combat scale: weapon, weak point cue if gameplay supports it, and status overlays.

- Boss scale should create presence without hiding the UI, party, or target selection.

- Only show weakness, resistance, nullification, or status immunity cues if the game design supports an in-game indicator.

- Boss mental-effect immunity may be represented with a clear immunity state when the status is attempted; exact UI behavior needs approval.

### 8. Environment and Dungeon Framework

**CONFIRMED: eight sequential dungeons; individual themes, layouts, node counts, encounters, and bosses are not established in the cited GDD baseline. Do not invent definitive dungeon themes.**

|**Environment layer**|**Guideline**|
|---|---|
|Back layer|Large atmospheric shapes, low contrast, controlled detail.|
|Midground|Main architecture and traversal-readable forms.|
|Gameplay plane|Clear floor edge, encounter space, and character footing.|
|Foreground|Use sparingly; avoid covering units, targets, or action cues.|
|Props|Use repeated motifs and value grouping; keep interactive objects<br>visually distinct.|
|Tile sets|Maintain tile grid consistency and transitions; create edge/corner<br>variants where needed.|
|Dungeon identity sheet|For each dungeon, fill: theme, palette, architecture, light source,<br>prop set, hazard language, boss arena motif.|

Dungeon palette progression can be developed after themes are approved. Until then, palette progression is a placeholder framework, not a canonical sequence.

### 9. Combat Visual Design

#### RECOMMENDED

- Keep party and enemy silhouettes visible throughout action resolution.

- Use a consistent target marker with clear selected/hovered/invalid states.

- Place damage numbers near the affected unit without overlapping critical UI.

- Differentiate damage, healing, critical hit, miss, and status application using shape, motion, and concise labels—not color alone.

- CONFIRMED: QTE result grades are Perfect / Good / Poor. Each grade uses a text label plus a glyph (star / circle / cross) and distinct hit-stop and SFX; colour is secondary. Offensive and defensive timing bars differ by icon (sword / shield) and frame shape. Heavy attacks use a thick frame and a half-hatched Perfect zone. Grade label, damage number, and status icon appear on separate rows above the unit (0 ms / +100 ms / +250 ms). Full specification: Bab 12 §9 and §10.8.

- Effects should have a short readable anticipation, impact moment, and cleanup phase; exact timings are tuning values.

### 10. Elemental Visual Effects

**CONFIRMED element list in Damage & Status Effect GDD v1.0: Fire, Ice, Lightning, Earth, Wind, Water, Holy, Shadow. Nature is a Druid theme, not a damage element.**

|**Type**|**Visualgrammar**|**Avoid**|
|---|---|---|
|Fire|Warm rising tongues, sparks, turbulent<br>edges.|Using only orange tint without flame motion.|
|Ice|Faceted shards, crisp freezes, cool<br>highlights.|Soft mist alone that reads as Wind or Water.|
|Lightning|Sharp branching arcs and brief high-contrast<br>flashes.|Long smooth ribbons.|
|Earth|Chunky fragments, weight, ground-up<br>movement.|Tiny floating particles that feel weightless.|
|Wind|Curved streaks, displaced dust, sweeping<br>arcs.|Rigid shard silhouettes.|
|Water|Flowing ribbons, droplets, wave-like impact.|Jagged electric branches.|
|Holy|Radial rays, circles, pale luminous shapes.|Dark smoke or aggressive angularity.|
|Shadow|Tapered wisps, inward movement,<br>restrained purple values.|Generic black cloud with no readable<br>motion.|
|Physical|Weapon trail,impact burst,sparks,slash geometry.|Elemental color coding without an element. i|
|Almighty|Per-skill signature; use a consistent neutral<br>identity after approval.|Assuming a fixed elemental appearance<br>before skill direction is settled.|

### 11. Status Effect Visual Language

**RECOMMENDED visual mapping; status mechanics remain governed by the Damage & Status Effect GDD.**

|**Status**|**Suggested visual cue**|
|---|---|
|Burn|Small ember particles and warm flicker.|
|Poison|Toxic droplet / restrained green pulse.|
|Bleed|Brief red slash or droplets; avoid excessive gore.|
|Freeze|Frost rim, crystal flecks, constrained motion.|
|Shock|Short electrical twitch and branching spark.|
|Curse|Dark sigil or mark; visual form may vary by skill.|
|Stun|Brief impact stars or ring; keep distinct from Shock.|
|Mind Control|Controller-linked motif or clear control marker.|
|Confusion|Offset/rotating symbol; communicate instability.|
|Fear|Desaturated or shrinking emphasis, if approved.|
|Despair|Muted, low-energy visual treatment.|
|Charm|Soft heart/ornament cue; do not imply a new mechanic.|
|Sleep|Closed-eye / drifting particles.|
|Silence|Muted sound/speech glyph.|
|Madness|Unstable, fractured icon or brief distortion.|

Icons must remain legible at the actual UI size. Stacking, duration labels, icon frames, and boss immunity labels are OPEN presentation decisions unless specified in the relevant implementation document.

### 12. UI/UX Visual Direction

#### RECOMMENDED

|**UI area**|**Visual direction**|
|---|---|
|Main menu|Dark, atmospheric backdrop; high-contrast title and clear primary<br>actions.|
|Character / status|Compact panels, clear attributes and class identity; avoid decorative<br>clutter.|
|Skill tree|Tier hierarchy and locked/unlocked states must be unmistakable.|
|Equipment / inventory|Readable item icon, slot, rarity, comparison values, and character<br>ownership.|
|Shop|Clear price, stock, refresh cost, buy/sell affordances; distinguish run<br>Gold from persistent Gold where relevant.|
|Dungeon node map|Readable node state, route, and progression; icon silhouettes should<br>work at small scale.|
|Combat HUD|Turn order, actor, target, HP/MP, QTE, and status states prioritized by<br>gameplay importance.|
|Dialogue / prompts|Legible text, consistent speaker identification, restrained frame<br>ornament.|

Typography: use a pixel-compatible or crisp display face for headings and a highly legible face for small UI text. Final font family, licensing, localization coverage, and minimum size are OPEN.

### 13. Equipment and Item Visual Design

**RESOLVED (Nov 2026): eight equipment slots — Main Hand, Second Hand, Head, Body, Leg, Foot, Accessory 1, Accessory 2. The equipment screen grid can be locked to this layout.**

|**Item category**|**Visual treatment**|
|---|---|
|Weapons|Distinct silhouette and class association; prioritize readable outline over tiny detail.|
|Second Hand|Shields, off-hand weapons, and special items need silhouettes clearly distinct from Main Hand weapons at icon size. CONFIRMED: two-handed weapons occupy only Main Hand, so a Second Hand item can coexist with them. RECOMMENDED: when the Main Hand weapon is drawn two-handed, show the Second Hand item stowed (back or hip) on the combat sprite; approve per item type.|
|Armor (Head, Body, Leg, Foot)|Communicate material through value ramps and highlight shapes; Leg and Foot icons need distinct silhouettes from each other.|
|Accessories|Small iconic shape; avoid overly complex pixel clusters.|
|Consumables|Strong container/item silhouette and clear category language.|
|Rarity|Use approved rarity color plus border, shape, or label; color-only<br>encoding is insufficient.|

Rarity palette colors are source-defined as Gray, Green, Blue, Purple, Orange, and Red. Exact HEX values are RECOMMENDED and require approval.

### 14. Animation Guide

#### RECOMMENDED

|**Animation**|**Key direction**|
|---|---|
|Idle|Small looping motion; keep silhouette stable.|
|Attack|Readable anticipation → contact/impact → recovery.|
|Skill|Cast or wind-up pose, signature effect, clear target impact.|
|Hit reaction|Direction and intensity communicate incoming hit without hiding<br>status.|
|Critical|Emphasize impact with timing/shape, not merely larger numbers.|
|Healing|Upward or restorative motion; distinct from buffvisuals.|
|DoT|Small repeated cue, visually restrained so it does not become noise.|
|Death / defeat|Readable, concise, and consistent with dark fantasy tone.|
|Victory|Character/class personality without excessive screen obstruction.|

Frame counts and timing should be tuned against actual combat pacing. Do not finalize timing values from this guide alone.

### 15. Unity Technical Art Guidelines

#### CONFIRMED design baseline values are targets; verify project settings before production.

|**Setting**|**Recommended baseline / note**|
|---|---|
|Pixels Per Unit|32 (design baseline).|
|Reference resolution|640 × 360, 16:9 (design baseline).|
|Camera|Orthographic side-view combat.|
|Tile size|32 × 32 pixels (design baseline).|
|Filter Mode|Point (no filter) recommended for crisp pixel art.|
|Compression|Avoid lossy compression for pixel-art sprites where it introduces<br>artifacts; confirm platform memory budget.|
|Pixel Perfect Camera|Recommended if using Unity 2D Pixel Perfect workflow; validate with<br>chosen render resolution.|
|Sprite Atlas|Group by scene/use to reduce texture switches while keeping<br>memory manageable.|
|Sorting|Define stable Sorting Layers and Sorting Groups for background,<br>environment, characters, effects, and UI.|
|Animation|Use consistent pivots and sprite dimensions; test animation bounds<br>and sorting.|
|UI Canvas|Keep UI scale consistent with target resolution and accessibility<br>needs.|

The current build’s actual resolution, procedural-vs-authored sprite state, and import settings are not verified by this document. Record an implementation audit before treating settings as current.

### 16. Asset Naming Convention

#### RECOMMENDED

|**Asset**|**Pattern**|**Example**|
|---|---|---|
|Character sprite|CHR_[Class]_[Name]_[State]|CHR_Wizard_Aster_Idle|
|Enemy sprite|ENM_[Family]_[Name]_[State]|ENM_Undead_Skeleton_Idle|
|Environment|ENV_D[##]_[Set]_[Piece]|ENV_D01_Ruins_Wall_A|
|Tile set|TILE_D[##]_[Theme]|TILE_D01_Ruins|
|UI|UI_[Screen]_[Element]|UI_Combat_TargetMarker|
|Icon|ICO_[Category]_[Name]|ICO_Status_Burn|
|VFX|VFX_[Type]_[Name]|VFX_Element_FireImpact|
|Animation clip|ANM_[Asset]_[Action]|ANM_Wizard_Cast|
|Material|MAT_[AssetType]_[Purpose]|MAT_Sprite_Outline|
|Sprite Atlas|ATL_[SceneOrGroup]|ATL_Combat|

Use underscores, stable descriptive tokens, and consistent capitalization. Avoid vague names such as NewSprite, Final2, or Copy.

### 17. Art Production Pipeline

|**Stage**|**Output / exit criteria**|
|---|---|
|1. Brief|Approved purpose, source mechanics, target scale, status label.|
|2. Thumbnail / silhouette|Several readable concepts; select one direction.|
|3. Palette|Approved color ramps and contrast check.|
|4. Sprite production|Clean native-scale sprite or sprite sheet.|
|5. Animation|Required states and timing pass.|
|6. Unity import|Correct PPU, pivot, filtering, atlas, sorting.|
|7. Scene integration|Asset tested in intended gameplay context.|
|8. Visual QA|Readability, consistency, artifact and overlap checks.|
|9. Review|Feedback recorded; revisions tracked.|
|10. Approval / archive|Approved source, export, and naming/version recorded.|

Keep source files editable. Each asset should have an owner, status, and review note. Do not mark an asset complete until it has been checked in the game context.

### 18. Visual Quality Assurance

- [ ] Asset matches approved brief and source mechanics.

- [ ] Silhouette reads at gameplay scale.

- [ ] Pixel density and PPU are consistent.

- [ ] Palette and value contrast are controlled.

- [ ] No unwanted blur, compression artifacts, or uneven scaling.

- [ ] Animation pivots, bounds, and sorting behave correctly.

- [ ] VFX do not obscure target, character, or critical UI information.

- [ ] UI icon and text remain legible at actual display size.

- [ ] Asset naming and folder placement follow the convention.

- [ ] Asset is tested in its real Unity scene and recorded as planned / in progress / approved.

### 19. Visual Prototype Scope

#### RECOMMENDED — prototype assets are to be created; this list does not claim they exist.

Build a small vertical slice to validate the visual language before scaling production.

- One playable character sprite and idle/attack/hit states.

- One enemy sprite with a readable target state.

- One combat environment with background, floor, and a small prop set.

- One Physical attack and one elemental skill effect.

- One status effect icon and in-world cue.

- One QTE UI state with result feedback.

- A minimal combat HUD showing units, HP/MP, target, and status.

- One equipment icon at intended inventory size.

Prototype acceptance: visual readability at target resolution; consistent pixel scale; distinct elemental effect; usable UI hierarchy; no obvious rendering artifacts; and team approval of the direction.

### 20. Open Decisions

|**Topic**|**Current source situation**|**Resolution needed**|
|---|---|---|
|Dungeon themes|Eight sequential dungeons are established;<br>themes/layouts are TBD in GDD baseline.|Approve each dungeon’s theme, palette,<br>architecture, props, and boss arena.|
|Rendering resolution / sprite state|Visual Direction gives 640×360 and sprite<br>dimensions; current implementation may<br>differ.|Audit actual project and choose target<br>render pipeline/resolution.|
|Equipment slots|RESOLVED (Nov 2026): 8 slots, adopted by<br>Equipment & Inventory v1.2.|Design the final 8-slot equipment screen.|
|Element list / naming|Damage & Status v1.0 defines Fire, Ice,<br>Lightning, Earth, Wind, Water, Holy, Shadow;<br>Nature is not damage. Other notes may<br>differ.|Confirm implementation terminology,<br>including any Arcane or Dark alias found in<br>build.|
|QTE grade names|RESOLVED (Nov 2026): Perfect / Good / Poor,<br>with glyph and layout rules in the QTE System<br>Specification.|Approve final glyph art and palette only.|
|Typography|No final font family/size/license identified<br>here.|Choose font, licensing, and localization<br>policy.|
|Rarity HEX values|Rarity colors are named; exact values not<br>confirmed.|Approve exact palette and non-color<br>indicators.|
|Status UI behavior|Icon stacking, duration, frames, immunity<br>labels may be unspecified.|Approve icon size, stacking, duration<br>display, and immunity presentation.|
|Asset implementation|This guide specifies future asset direction.|Create asset inventory and track each<br>asset’s production status.|

## Bab 17 — UI/UX Design

> Sumber: Game UI Design Document (design.md).

### 1. Design Goals

The interface should support a party-based, turn-based roguelike RPG with clear tactical information and minimal friction between decisions.

#### Core goals
- Make combat state, turn order, target selection, and status effects easy to read.
- Keep character progression, equipment, inventory, and party management understandable.
- Clearly communicate the difference between persistent progress and run-only resources.
- Make dungeon choices legible without overwhelming the player with text.
- Keep UI feedback responsive and consistent across mouse, keyboard, and controller where supported.
- Preserve the game’s JRPG/anime-inspired tone while keeping the interface practical.

#### UX principles
1. **Clarity before decoration:** decorative elements must not obscure combat information.
2. **Consistent terminology:** use the same names for stats, damage types, status effects, and currencies everywhere.
3. **Readable hierarchy:** primary action, current actor, target, and immediate result receive the strongest visual emphasis.
4. **Explain consequences:** choices that spend resources, alter equipment, or end a run should state the consequence.
5. **No silent state changes:** show clear feedback when an item is consumed, a status is applied, a reward is retained, or a run resets.

---

### 2. Game UI Context

#### 2.1 Relevant game structure
- Party-based RPG with a maximum party size of **4 characters**.
- **9 classes:** Fighter, Barbarian, Martial Artist, Rogue, Ranger, Cleric, Wizard, Warlock, and Druid.
- **7 races:** Human, Elf, Dwarf, Halfling, Dragonborn, Liichtian, and Merfolk.
- Turn-based combat with a reaction-based QTE mechanic.
- Damage categories: Physical, Elemental, and Almighty.
- Elements: Fire, Ice, Lightning, Earth, Wind, Water, Holy, and Shadow.
- Equipment slots (8): Main Hand, Second Hand, Head, Body, Leg, Foot, Accessory 1, Accessory 2.
- Equipment inventory capacity: 100 slots. Consumables and materials are stackable.
- Dungeon progression consists of **8 sequential dungeons**.
- Run failure returns to the Main Menu and resets dungeon progression; a new game is required to begin again.
- Run Gold is lost on failure. Gold from a cleared dungeon is retained and transferred 1:1 into Hub Gold.
- Equipment acquired during a run persists after failure.

#### 2.2 Currency and persistence communication
The UI must distinguish these resource states visually and textually:
- **Run Gold:** temporary currency held during the current run.
- **Hub Gold:** persistent currency available in the hub.
- **Run progress:** current sequential dungeon progress, reset when the run fails.
- **Persistent equipment:** equipment acquired during a run remains owned even if the run fails.

Whenever the player enters a high-risk choice, the interface should explain the outcome before confirmation.

---

### 3. Navigation and Screen Flow

#### 3.1 High-level flow

```text
Main Menu
 ├─ New Game
 │   └─ Character Creation
 │       └─ Party Setup
 │           └─ Hub
 ├─ Continue / Load Game [Proposed; depends on save implementation]
 ├─ Settings
 ├─ Credits
 └─ Exit

Hub
 ├─ Character / Party
 ├─ Equipment
 ├─ Inventory
 ├─ Shop
 ├─ Blacksmith
 ├─ Training Ground / Skill Tree
 ├─ Temple [if revive/healing services are implemented]
 └─ Dungeon Entrance
     └─ Dungeon Map / Node Selection
         ├─ Exploration Event
         ├─ Shop Node
         ├─ Rest / Recovery Node [if implemented]
         └─ Combat
             ├─ Victory / Reward
             ├─ Dungeon Clear
             └─ Run Failure → Main Menu
```

#### 3.2 Navigation conventions
- A persistent **Back** action should return to the previous safe screen.
- During combat, Back should not cancel an action after it has been committed.
- A confirmation dialog should be used for destructive or irreversible actions.
- The current location and current run state should be visible in screens where that context matters.

---

### 4. Visual Direction

#### 4.1 Overall style
**[Proposed]** A polished JRPG-inspired interface with restrained anime fantasy motifs:
- Dark, low-noise panels for information-heavy areas.
- Warm accent color for primary actions and important rewards.
- Distinct element colors/icons for damage and status communication.
- Character portraits and class/race emblems used as identity cues.
- Subtle motion for transitions, turn changes, rewards, and status application.

Avoid relying on color alone. Icons, labels, and tooltips should also communicate meaning.

#### 4.2 Typography hierarchy
- **Display:** screen titles, dungeon names, major victory/defeat messages.
- **Heading:** panel titles and section labels.
- **Body:** descriptions, skill text, event choices.
- **Small/UI label:** costs, item rarity, status duration, secondary metadata.

Text that affects tactical decisions—damage, healing, duration, costs, and QTE timing—must remain readable at the target gameplay resolution.

#### 4.3 Iconography
Use consistent icons for:
- Physical, Elemental, and Almighty damage.
- Each of the eight elements.
- Status effects and their duration/stacks.
- Character class and race.
- Equipment slot, rarity, currency, and consumable type.
- Node type on the dungeon map.

Each icon should have a tooltip or accessible label.

---

### 5. Screen Specifications

#### 5.1 Main Menu

##### Purpose
Provide entry points to the game and configuration.

##### Components
- Game logo/title.
- **New Game**
- **Continue** [Proposed; show only when a valid save exists]
- **Load Game** [Proposed; show only if multiple/manual save slots exist]
- **Settings**
- **Credits**
- **Exit**
- Background art or subtle animated scene.
- Version/build label in a low-priority location.

##### Behavior
- New Game opens character creation or the agreed starting flow.
- Continue resumes the most recent valid save, if supported.
- Exit prompts for confirmation.
- Menu focus must be visible for keyboard/controller navigation.

---

#### 5.2 Character Creation

##### Purpose
Create and review a character before entering the game.

##### Recommended flow [Proposed]
1. Name
2. Race
3. Ability Scores
4. Class
5. Starting Skills
6. Review and Confirm

##### Layout
- Character preview/portrait.
- Step indicator.
- Main selection panel.
- Description panel for the highlighted race, class, ability, or skill.
- Back, Next, and Confirm actions.

##### Required information
- Race description and racial benefits.
- Class description and role.
- Ability score allocation and resulting derived stats.
- Starting skill list.
- Validation messages for missing or invalid selections.

##### Behavior
- Changing race/class should update the preview and relevant stats.
- The final review screen should summarize the complete character.
- Confirmation should be explicit.

---

#### 5.3 Party Management

##### Purpose
Manage a party of up to four characters.

##### Components
- Party member cards with portrait, name, race, class, level, HP, and resource indicators.
- Party order/formation controls, if formation affects gameplay.
- Add/remove/select member actions, depending on party rules.
- Character detail shortcut.
- Confirm/Back controls.

##### States
- Empty party slot.
- Selected character.
- Character unavailable or incapacitated, if applicable.
- Party full.

The interface must not imply that formation affects combat unless the game rules actually use formation.

---

#### 5.4 Hub

##### Purpose
Provide a safe location for preparation between dungeon attempts.

##### Main layout
- Hub scene/background.
- Facility selection or navigation menu.
- Party summary.
- Hub Gold display.
- Current progression summary and Dungeon Entrance.

##### Facility destinations
- Character / Party
- Shop
- Armory / Equipment
- Blacksmith
- Training Ground / Skill Tree
- Temple [only if the service exists]
- Dungeon Entrance

##### Feedback
- Show a brief interaction label when hovering/focusing a facility.
- Show unavailable services as disabled with a short explanation.
- Clearly distinguish hub services from services encountered during a dungeon.

---

#### 5.5 Character Status

##### Purpose
Present a complete character overview.

##### Information
- Name, portrait, race, class, level, and experience.
- Current/max HP and other character resources.
- Ability scores and derived combat stats.
- Resistances and vulnerabilities.
- Damage bonuses and defensive bonuses.
- Active status effects.
- Equipped items.
- Skill/ability summary.

##### Layout
Use tabs or grouped panels to prevent information overload:
- Overview
- Attributes
- Combat
- Status Effects
- Equipment
- Skills

Every stat should have a short tooltip explaining what it affects.

---

#### 5.6 Equipment

##### Purpose
Equip, compare, and inspect gear.

##### Equipment slots
- Main Hand
- Second Hand
- Head
- Body
- Leg
- Foot
- Accessory 1
- Accessory 2

##### Components
- Character paper-doll or equipment-slot layout.
- Inventory list/grid filtered to compatible equipment.
- Item detail panel.
- Current vs. candidate stat comparison.
- Equip, Unequip, and Back actions.

##### Item details
- Name, rarity, slot, and required conditions (if any).
- Base stats.
- Affixes/modifiers.
- Special effect.
- Description and source, if available.

##### Behavior
- Highlight stat gains and losses in the comparison.
- Do not silently replace an equipped item.
- If an item is incompatible, explain why and disable Equip.
- Display rarity with both color and text/icon.

---

#### 5.7 Inventory

##### Purpose
Manage equipment, consumables, and materials.

##### Categories
- Equipment
- Consumables
- Materials
- Key Items [if implemented]

##### Components
- Category filters.
- Search/sort controls [Proposed].
- Item grid/list.
- Stack count.
- Item detail panel.
- Use, Equip, Discard, or other valid actions.

##### Rules
- Equipment inventory capacity is 100 slots.
- Consumables and materials stack.
- Show a clear full-inventory warning.
- Before discarding or consuming a valuable item, show a confirmation when appropriate.
- If a stack reaches zero, remove it from the inventory view.

---

#### 5.8 Shop

##### Purpose
Buy and sell items where the relevant shop rules permit.

##### Components
- Shop inventory.
- Player inventory/sell list.
- Item detail and price.
- Current applicable currency.
- Buy/Sell action.
- Transaction confirmation.

##### Currency display
- Hub Shop: Hub Gold.
- Dungeon Shop: Run Gold.

##### Feedback
- Show insufficient currency clearly.
- Show inventory capacity issues before purchase.
- Confirm the item and total cost before finalizing, where the transaction is consequential.
- After purchase/sale, update currency and inventory immediately.

---

#### 5.9 Blacksmith

##### Purpose
Upgrade eligible equipment.

##### Components
- Equipment selection.
- Current and upgraded stat preview.
- Required materials/currency.
- Upgrade action.
- Confirmation and result feedback.

##### Known upgrade constraint
- Blacksmith upgrades affect **Base Stats** only.
- Affixes and Legendary/Mythic effects are not upgraded by this system.

The preview must distinguish base-stat changes from affix/special-effect text.

---

#### 5.10 Training Ground / Skill Tree

##### Purpose
Inspect and allocate class skills.

##### Components
- Character selector.
- Class/skill tree visualization.
- Skill nodes and prerequisite links.
- Skill detail panel.
- Current rank, next rank, cost, and requirements.
- Learn/Upgrade action.
- Reset action [only if respec is part of the game rules].

##### Skill detail
- Skill name and category.
- Description.
- Resource cost and cooldown, if applicable.
- Damage/healing/effect values.
- Targeting rules.
- Prerequisites.
- Rank scaling.

##### Behavior
- Locked skills show the exact unmet requirement.
- The interface should prevent invalid allocations.
- Do not show a reset option unless the game supports respec.

---

#### 5.11 Temple / Recovery Service

**Conditional screen:** include only if the temple, revival, or recovery service is part of the final design.

##### Components
- Service description.
- Party member selection.
- Cost and currency.
- Expected result.
- Confirm/Cancel.

Before payment, show which characters or resources will be affected.

---

#### 5.12 Dungeon Map / Node Selection

##### Purpose
Show the player’s route and available next choices.

##### Components
- Node-based map.
- Current node.
- Cleared nodes.
- Available next nodes.
- Locked/unavailable nodes.
- Node type icon and short label.
- Current party status and Run Gold.
- Proceed/Confirm action.

##### Node information
When a node is selected, show:
- Node type.
- Short description.
- Known risk/reward information, if revealed by game rules.
- Whether the choice is one-way.

##### Behavior
- Clearly indicate which nodes are selectable.
- Do not reveal hidden information that the game intends to keep secret.
- Keep the sequential progression across the eight dungeons clear.

---

#### 5.13 Exploration HUD

##### Purpose
Keep essential run information visible while exploring.

##### Components
- Party HP/resource summary.
- Run Gold.
- Current dungeon and node.
- Map access.
- Relevant interaction prompt.
- Status indicators for party members.

Avoid covering interactive objects or narrative content with persistent HUD elements.

---

#### 5.14 Event / Choice Screen

##### Purpose
Present narrative events and player decisions.

##### Components
- Event title.
- Event illustration or scene.
- Event description.
- Choice list.
- Requirement/cost preview where known.
- Outcome feedback.

##### Behavior
- Choices should use concise, action-oriented wording.
- Show requirements before the player commits.
- If an outcome is uncertain by design, do not falsely present it as guaranteed.
- Disable choices that cannot currently be selected and explain why.

---

#### 5.15 Combat HUD

##### Purpose
Present tactical information and actions during turn-based combat.

##### Main layout
- Battlefield view.
- Player party character cards.
- Enemy units and target indicators.
- Turn order/initiative display.
- Current actor highlight.
- Action menu.
- Combat log or recent-action feed.
- Status effect icons and duration/stack information.

##### Character cards
Show:
- Character portrait/name.
- HP and relevant resource.
- Current status effects.
- Turn/selection state.

##### Action menu
**[Proposed; confirm against final combat rules]**
- Attack
- Skills
- Items
- Guard

The menu must only show actions that are valid for the current actor and state.

##### Target selection
- Highlight the selected target.
- Show target name and relevant HP/status information.
- Indicate ally/enemy targeting.
- Provide a clear cancel/back action before the action is committed.
- Display range/valid-target constraints if applicable.

##### Turn order
- Clearly distinguish current turn, upcoming turns, and delayed/altered turns.
- If status effects modify turn order, communicate the change.

---

#### 5.16 Reaction-Based QTE

##### Purpose
Make reaction input readable and fair during combat.

##### Components
- Timing indicator.
- Input prompt.
- Success window.
- Result feedback.
- Damage/result multiplier feedback.

##### Known result values
| Result | Multiplier |
|---|---:|
| Perfect | 1.5× |
| Good | 1.0× |
| Poor | 0.5× |

These values should be displayed only where they apply in the final rules. QTE feedback should clarify that the multiplier affects direct damage, not damage-over-time effects.

##### Defensive result values
| Result | Damage reduction |
|---|---:|
| Perfect | 100% (Heavy: 50%) |
| Good | 50% |
| Poor | 0% |

##### Confirmed presentation rules
- Horizontal timing bar, 25% of screen width (35% option), centered above the party HUD; hit point at 75% of bar length.
- Three-beat audio cue: two ticks, then the silent press beat.
- Offensive bar: sword icon, rounded frame. Defensive bar: shield icon, sharp frame, target portraits in the header.
- Sequence and multi-hit: numbered markers on one bar; judged markers become glyphs (star / circle / cross); summary at the bar end.
- Heavy: thick frame, "HEAVY" label, half-hatched Perfect zone, low drum cue.
- Stun: greyed bar with "AUTO POOR — STUN" for 600 ms.
- Above the unit: grade label (0 ms), damage number (+100 ms), status icon or RESIST/IMMUNE (+250 ms), on separate rows.
- HUD additions: enemy intent icons (only for actions that must be telegraphed), Break gauges (shown above 50%), Martial Artist combo counter, Lucky pips.
- Full specification: Bab 12 §9.

##### Accessibility and usability
- Provide a readable timing cue in addition to animation.
- Avoid relying exclusively on color.
- Keep the prompt visible long enough to understand the required input.
- Provide settings for input/display accessibility if supported.

---

#### 5.17 Damage and Status Feedback

##### Purpose
Communicate what happened, why it happened, and how long it will matter.

##### Damage feedback
- Damage number near the affected unit.
- Damage type icon/label.
- Critical or special-hit marker, if applicable.
- Healing and shield feedback with distinct presentation.
- Avoid stacking too many numbers in the same location.

##### Status effects
- Status icon.
- Name on hover/focus.
- Remaining duration.
- Stack count or independent-instance indicator, where relevant.
- Source/trigger information when useful.

Status effects may have independent instances. The UI must not collapse them into a single misleading duration or stack unless the game’s actual rules combine them.

##### Status application/removal
- Short animation or notification.
- Clear indication of immunity, resistance, expiration, or removal when those outcomes occur.

---

#### 5.18 Combat Victory / Reward

##### Purpose
Summarize combat outcome and reward acquisition.

##### Components
- Victory heading.
- Reward list.
- Gold earned.
- Equipment/material/consumable drops.
- Character progression/experience, if applicable.
- Continue action.

##### Behavior
- Show what is added to the run and what is persistent.
- If inventory capacity affects rewards, explain the result.
- Do not claim that the dungeon is cleared if only a combat encounter was completed.

---

#### 5.19 Dungeon Clear

##### Purpose
Communicate successful completion of a dungeon and the transfer of retained rewards.

##### Components
- Dungeon clear heading.
- Dungeon number/name.
- Reward summary.
- Run Gold transfer summary.
- Next dungeon / return to hub action, according to progression flow.

##### Currency message
Show the exact transfer clearly:
- Run Gold cleared.
- Hub Gold gained at a 1:1 rate.
- New Hub Gold total.

Dungeon progression is sequential across eight dungeons.

---

#### 5.20 Run Failure

##### Purpose
Explain the failure outcome and give the player a clear exit.

##### Components
- Defeat heading.
- Run summary.
- Run Gold lost.
- Progress reset explanation.
- Persistent equipment retention message.
- Return to Main Menu action.

##### Required communication
The UI must state that:
- The failed run ends.
- Dungeon progression resets.
- A new game is required to begin again.
- Run Gold is lost.
- Equipment acquired during the run remains persistent.

This screen should not offer a Continue Run or Resume Dungeon action unless the game rules change.

---

#### 5.21 Settings

##### Categories
- Audio
- Graphics
- Controls
- Gameplay
- Accessibility [if supported]

##### Audio
- Master volume.
- Music volume.
- SFX volume.
- UI volume [if separate].

##### Graphics
- Resolution.
- Display mode.
- Quality settings.
- VSync/frame-rate options [if implemented].

##### Controls
- Keyboard/mouse bindings.
- Controller bindings.
- Input prompt preference [if supported].

##### Gameplay / Accessibility
- Text speed.
- Combat animation speed.
- QTE settings: Auto-Good; timing window ×1.0/×1.25/×1.5; QTE speed ×1.0/×1.25/×1.5; early-press tolerance; tremor filter; any-key mode; calibration offset (−150 to +150 ms, per device); QTE cue volume; metronome cue; controller vibration; bar size; hit-point flash; screen-edge pulse; high contrast. Window, speed, early-press tolerance, and Auto-Good are recorded as Assist on the run summary.
- Screen shake toggle.
- Damage number visibility.
- Color/contrast options, if supported.

##### Behavior
- Apply, Revert, and Reset to Default.
- Warn before leaving with unapplied changes.

---

#### 5.22 Pause Menu

**[Proposed; confirm whether combat/exploration can be paused]**

Possible actions:
- Resume
- Settings
- Controls
- Return to Main Menu [with confirmation]

Do not include Save/Load in the pause menu unless the save system supports it and the game rules permit saving at that moment.

---

### 6. Common UI Components

Build reusable components for:
- Character card.
- Enemy card.
- HP/resource bar.
- Status effect icon with tooltip.
- Item card.
- Rarity label.
- Currency display.
- Skill node.
- Target indicator.
- Confirmation dialog.
- Toast/notification.
- Tooltip.
- Loading indicator.
- Empty state.
- Disabled action with reason.

#### Shared component states
- Default
- Hover
- Focus
- Selected
- Pressed
- Disabled
- Loading
- Error
- Success

Keyboard/controller focus must be visually distinct from hover.

---

### 7. Dialogs, Notifications, and Error States

#### Confirmation dialogs
Use for:
- Starting a New Game when existing progress may be overwritten.
- Leaving a screen with unsaved changes.
- Discarding valuable items.
- Spending a significant resource.
- Irreversible choices.
- Returning to the Main Menu during a run.

#### Notifications
Use concise notifications for:
- Item acquired.
- Item equipped.
- Item upgraded.
- Skill learned.
- Status applied/removed.
- Currency gained/spent.
- Inventory full.
- Insufficient currency.
- Invalid target/action.

Notifications should not cover QTE prompts or critical combat information.

---

### 8. UI State and Data Requirements

Each screen should receive its displayed values from game state rather than maintaining a separate, conflicting source of truth.

#### Minimum data needed
- Active screen and navigation history.
- Party members and their current stats.
- Selected character/target/item/skill.
- Inventory contents, slot usage, and stack counts.
- Equipped gear.
- Hub Gold and Run Gold.
- Current dungeon, node, and progression state.
- Combat turn order, actor, targets, actions, and results.
- Status instances, durations, and stacks.
- Settings values.
- Save availability, if a save system exists.

#### State-change feedback
When game state changes, update the UI and provide feedback appropriate to the change. Examples:
- Currency change → currency display + short notification.
- Equipment change → character stats + equipment slots.
- Status change → status icon + duration + event feedback.
- Run failure → failure screen + explicit reset/retention summary.

---

### 9. Implementation Notes

#### Suggested UI architecture [Proposed]
Separate UI into:
- **Screen layer:** full-screen views and navigation.
- **Reusable component layer:** cards, bars, icons, tooltips, dialogs.
- **Presentation/state layer:** converts game data into display-ready values.
- **Input layer:** mouse, keyboard, and controller navigation.

#### Suggested screen groups
```text
UI
├── MainMenu
├── CharacterCreation
├── Hub
│   ├── PartyManagement
│   ├── CharacterStatus
│   ├── Equipment
│   ├── Inventory
│   ├── Shop
│   ├── Blacksmith
│   └── SkillTree
├── Dungeon
│   ├── DungeonMap
│   ├── ExplorationHUD
│   └── EventChoice
├── Combat
│   ├── CombatHUD
│   ├── TargetSelection
│   ├── QTE
│   ├── CombatFeedback
│   └── CombatResult
├── RunResult
│   ├── DungeonClear
│   └── RunFailure
└── Settings
```

Avoid tightly coupling gameplay logic to visual components. UI should request actions from game systems and render the returned/current state.

---

### 10. Prioritization

#### Phase 1 — Core playable loop
- Main Menu.
- Character Creation.
- Party Management.
- Hub navigation.
- Dungeon Map.
- Exploration HUD.
- Combat HUD.
- Target selection.
- QTE.
- Victory and Run Failure.
- Essential feedback and confirmation dialogs.

#### Phase 2 — Progression and preparation
- Character Status.
- Equipment.
- Inventory.
- Shop.
- Blacksmith.
- Skill Tree.
- Dungeon Clear/reward transfer.

#### Phase 3 — Polish and accessibility
- Settings expansion.
- Controller navigation polish.
- Accessibility options.
- Additional animations and transitions.
- Tooltips, combat log improvements, and advanced filtering.
- Localization and text scaling.

---

### 11. Open Questions / Items to Confirm

These items should be finalized before implementation:
1. Does the game support Continue, manual Load Game, and multiple save slots?
2. Is character creation for one character or the full party?
3. Can the player pause during combat and exploration?
4. Is party formation mechanically relevant?
5. Are Temple/revival and Rest nodes confirmed?
6. What are the final combat actions and item-use rules?
7. Are skill respecs supported?
8. ~~Which accessibility settings are required for the QTE?~~ Resolved: see QTE settings and the Bab 12 §10.
9. Can rewards overflow the inventory, and if so, what happens?
10. Which screens are available during a dungeon run versus only in the hub?

---

### 12. UI Acceptance Checklist

- [ ] Every screen has a clear purpose and primary action.
- [ ] Every interactive element supports focus navigation where required.
- [ ] Current actor, selected target, and turn order are unambiguous in combat.
- [ ] Damage type and status duration are readable.
- [ ] Resource costs and currency type are shown before transactions.
- [ ] Inventory capacity and stack counts are visible.
- [ ] Equipment comparison shows both gains and losses.
- [ ] Run Gold and Hub Gold are visually distinct.
- [ ] Dungeon clear communicates the 1:1 Run Gold transfer.
- [ ] Run failure communicates reset rules and persistent equipment retention.
- [ ] Invalid actions are disabled or explain why they cannot be used.
- [ ] Important information is not communicated by color alone.
- [ ] UI feedback does not obscure critical combat/QTE information.

---

**Revision note (4 November 2026):** §5.16, QTE settings, and the 8 equipment slots synced with Bab 12 (QTE System).

## Bab 18 — Audio Direction

> Sumber: GDD Visual Direction v0.2 §10. Penomoran §10.x dipertahankan.

### 10. Audio Direction

#### 10.1 Main Menu Music

Arah yang dipilih: Main Menu Music Direction A dengan sentuhan JRPG anime. Nuansa konseptual fantasy adventure dan tema melodi yang memorable.

#### 10.2 Battle Music

Beberapa Battle Theme direncanakan. Battle Theme 1, 2, dan 3 telah dibahas sebagai konsep; pemetaan cue, intensitas, adaptive transition, dan final track specification belum final.

#### 10.3 Map Node Music

Musik khusus map node dibutuhkan. Mood, durasi loop, instrumentasi, dan transisi menuju combat/shop/event TBD.

#### 10.4 SFX

SFX dibutuhkan untuk UI, node, skill casting, physical/elemental impact, status, QTE success/perfect/fail, shop, dungeon clear, dan run failure. Final asset list TBD.

---

# Bagian VII — Status Proyek

## Bab 19 — Konflik, Asumsi, dan Isu Terbuka

### 19.1 Konflik versi yang perlu diputuskan

| Area | Konflik | Status di GDD ini |
| --- | --- | --- |
| Race System v1.1 | Ada dua versi v1.1 yang dibuat terpisah. Versi lain memakai lineage Dragonborn Ember (Fire, +2 STR), Frost (Ice, +2 STR), Storm (Lightning, +2 CHA), dan Umbral (Shadow, +2 CHA), serta Exhausted −15% damage | Bab 6 memakai lineage Crimson (Fire), Stone (Earth), Azure (Lightning), Frost (Ice) dan Exhausted −20%. Nilai Racial Feat lainnya sama di kedua versi |

### 19.2 Asumsi yang belum dikonfirmasi

| Kode | Asumsi | Lokasi |
| --- | --- | --- |
| A1 | Target platform PC dengan keyboard, mouse, dan controller | Bab 12, Asumsi eksplisit |
| A2 | Engine belum ditentukan; timing ditulis dalam milidetik | Bab 12, Asumsi eksplisit |
| A3 | Game berjalan pada 60 fps | Bab 12, Asumsi eksplisit |
| A4 | Penilaian QTE memakai timestamp input event | Bab 12, Asumsi eksplisit |
| A5 | Definisi kerja Break gauge dan Broken | Bab 10 §5.3, Bab 12 §3.1 |
| — | Tafsiran "QTE Counter Power" (Dragonborn), "QTE Stability" (Merfolk), dan Iron Resolve sebagai active ability | Bab 12 §8.5, Bab 6 |

### 19.3 Keputusan baru yang menggantikan catatan lama

| Catatan lama | Keputusan sekarang |
| --- | --- |
| Tema dungeon TBD (Bab 15 §13.11, Bab 16 §20) | Identitas delapan layer dan boss-nya sudah ditetapkan di Bab 4 §6. Palet, arsitektur, dan prop per layer tetap perlu dirancang |
| Jumlah equipment slot belum pasti | 8 slot: Main Hand, Second Hand, Head, Body, Leg, Foot, Accessory 1, Accessory 2. Weapon two-handed hanya menempati Main Hand (Bab 13) |
| Nama grade QTE belum pasti | Perfect / Good / Poor (Bab 12) |
| Arah rounding belum dikonfirmasi | Floor, sekali per hit (Bab 11 §7) |
| Nilai combo Martial Artist TBD | Ditetapkan di Bab 8 §5.1 dan Bab 12 §7 |
| Detail race TBD | Ditetapkan di Bab 6 |

### 19.4 Indeks isu terbuka per bab

| Area | Lokasi | Contoh isu |
| --- | --- | --- |
| Struktur combat | Bab 10 §5.1 | Enemy turn order, initiative formula, action economy, aturan defend dan item |
| Damage dan status | Bab 11 §14 | Skill scaling, nilai DoT, angka Freeze/Shock/Despair/Fear/Confusion, detail cleanse |
| Class | Bab 8 §14 | SP per level dan biaya skill, koefisien damage, MP cost, detail beberapa core mechanic |
| Talents | Bab 9 §6 | Struktur milestone, nilai final, respec |
| Character creation | Bab 5 §10 | Point Buy budget, derived-stat formula, starting equipment |
| Difficulty | Bab 3 §8 | Enemy stat scaling, pemulihan HP/MP, Recovery Camp, Character Down, meta-progression |
| Shop | Bab 14 §8.2 | Formula harga, sell value, refresh cost dan weight |
| Lore | Bab 4 §8 | Protagonis, sejarah layer, nasib lima petualang legendaris |
| Art | Bab 15 §13.11, Bab 16 §20 | Resolusi target, tipografi, nilai HEX rarity, status UI |
| UI | Bab 17 §11 | Save/continue, pause, party formation, overflow inventory |

### 19.5 Prioritas desain berikutnya

1. **Fondasi combat math:** enemy turn order, initiative, dan action economy (Bab 10) menjadi dependensi untuk QTE, status, dan class.
2. **Ekonomi resource:** MP cost skill, healing, HP musuh, dan pemulihan antar battle. Tanpa angka ini, keseimbangan QTE terhadap ekonomi survival belum bisa diverifikasi penuh (Bab 12, Fase 3).
3. **Enemy design per layer:** komposisi musuh, pola QTE, dan serangan signature boss untuk delapan layer di Bab 4 §6.

## Lampiran A — Peta Dokumen Sumber

| Dokumen sumber | Versi | Dipakai di | Catatan |
| --- | --- | --- | --- |
| GDD Visual Direction | v0.2 | Bab 1, 2, 10, 15, 18 | Bagian ringkasan yang sudah tercakup di bab detail tidak dimasukkan |
| Game Design Document Draft | v0.1 | — | Tidak dimasukkan; seluruh isinya sudah tercakup di v0.2 |
| Difficulty System | v0.2 | Bab 3 | Lengkap |
| Felna — Game Story & World Lore | Canon reference | Bab 4 | Lengkap |
| Character Creation System — Design Summary | — | Bab 5 | Ringkasan race, class, dan rarity draft diganti oleh Bab 6, 8, dan 13 |
| PRD Race System | v1.1 | Bab 6 | Lengkap |
| Background System | — | Bab 7 | Lengkap |
| Class System | v1.1 | Bab 8 | Lengkap |
| Talents System | v0.2 | Bab 9 | Lengkap |
| Consolidated Damage & Status Effect GDD | v1.1 | Bab 11 | Lengkap |
| Master Checklist QTE dan Spesifikasi Fase 2 | Rev. 4 Nov 2026 | Bab 12 | Keputusan dasar checklist ditambah seluruh spesifikasi, diurutkan berdasarkan nomor item |
| Inventory & Equipment System | v1.2 | Bab 13 | Lengkap |
| GDD Shop System | v1.0 | Bab 14 | Lengkap |
| Game Visual Style Guide | v1.0 | Bab 16 | Lengkap, tanpa document control dan daftar isi sumber |
| Game UI Design Document (design.md) | Draft | Bab 17 | Lengkap |

## Lampiran B — Riwayat Sinkronisasi November 2026

- Spesifikasi QTE lengkap disusun (Fase 2) dan diverifikasi konsistensinya (Fase 3), termasuk item tambahan 8.5 (Racial Feat dan QTE) dan 9.10 (ikon intent, Break gauge, penghitung combo).
- Grade QTE "Miss" diganti "Poor" di semua dokumen.
- Damage & Status Effect GDD v1.1: Defensive QTE, cakupan Offensive QTE, Break Power, finisher tanpa Critical, rounding Floor, batasan multi-hit dan multi-target, Sleep dan Mind Control sebagai auto-Poor.
- Class System v1.1: aturan QTE bersama, trigger counter, nilai Combo Flow Martial Artist, profil QTE skill ranged dan Holy.
- Talents System v0.2: alur retry Lucky, talent window QTE, stacking Damage Reduction, interaksi dengan Second Chance.
- Inventory & Equipment System v1.2: delapan equipment slot, weapon two-handed hanya menempati Main Hand, affix QTE dan batasan Special Effect.
- Difficulty System v0.2: skala kesulitan QTE, batasan desain musuh dan boss, pencatatan Assist.
- PRD Race System v1.1: seluruh nilai baseline ditetapkan tanpa TBD.
- GDD Terpadu v1.0: seluruh dokumen digabungkan menjadi satu dokumen ini.
