# GAME DESIGN DOCUMENT
# INVENTORY & EQUIPMENT SYSTEM

**Version:** 1.0  
**Status:** Finalized Design — Ready for Unity Implementation  
**System Category:** Character Progression / Itemization / Loot  
**Related Systems:** Combat, Character Stats, Class System, Damage & Status Effect, Dungeon Progression, Shop System, Blacksmith, Save System

---

# 1. Overview

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

## 1.1 Design Goals

1. Memberikan variasi build karakter melalui equipment.
2. Menjadikan loot sebagai bagian penting dari dungeon exploration.
3. Memberikan identitas equipment berdasarkan class dan equipment type.
4. Menyediakan progression melalui equipment level dan upgrade.
5. Memungkinkan pemain mengelola, membandingkan, memasang, melepas, serta menjual equipment.
6. Menyediakan sistem equipment yang dapat disimpan dan dimuat melalui Save System.
7. Mempertahankan equipment yang diperoleh selama run sebagai permanent progression.

## 1.2 Confirmed Design Decisions

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

# 2. Inventory System

Inventory menyimpan seluruh item yang dimiliki pemain.

Inventory dibagi menjadi empat kategori:

| Category | Contents |
|---|---|
| Equipment | Weapon, Head, Body, Accessory |
| Consumables | Potion, recovery item, buff item, revive item |
| Materials | Upgrade materials dan crafting materials |
| Key Items | Item khusus progression dan dungeon |

## 2.1 Equipment Inventory

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

## 2.2 Consumable Inventory

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

## 2.3 Materials Inventory

Materials menggunakan stack-based inventory.

Materials digunakan oleh Blacksmith dan sistem crafting apabila fitur crafting ditambahkan.

Materials tidak memiliki rarity random atau equipment instance.

## 2.4 Key Items

Key Items disimpan terpisah dari inventory equipment.

Key Items:

- Tidak menggunakan equipment slot.
- Tidak dapat dijual.
- Tidak dapat dibuang secara normal.
- Tidak dapat ditumpuk kecuali didefinisikan secara khusus.

---

# 3. Equipment System

Setiap karakter memiliki lima equipment slots.

| Slot | Description |
|---|---|
| Weapon | Weapon utama karakter |
| Head | Helmet, hat, hood, atau headgear |
| Body | Armor, robe, atau outfit |
| Accessory 1 | Accessory pertama |
| Accessory 2 | Accessory kedua |

Equipment dapat dipasang jika karakter memenuhi seluruh equipment requirements.

## 3.1 Equipment Compatibility

Equipment memiliki:

- Equipment Type
- Equipment Slot
- Allowed Classes
- Allowed Character IDs
- Minimum Character Level

Allowed Classes menentukan class yang dapat menggunakan equipment.

Allowed Character IDs digunakan untuk equipment yang hanya dapat digunakan oleh karakter tertentu.

Equipment dapat memiliki class restriction, character restriction, keduanya, atau tidak memiliki restriction tertentu.

## 3.2 Equip Rules

Ketika pemain memasang equipment:

1. Sistem memvalidasi equipment.
2. Sistem memvalidasi class compatibility.
3. Sistem memvalidasi character restriction.
4. Sistem memvalidasi level requirement.
5. Sistem memeriksa equipment slot.
6. Equipment yang sedang dipakai pada slot tersebut dilepas.
7. Equipment baru dipasang.
8. Character Stats dihitung ulang.
9. Equipment UI diperbarui.

Equipment yang tidak kompatibel tidak dapat dipasang.

Equipment yang dilepas kembali masuk ke inventory.

---

# 4. Equipment Rarity System

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

## 4.1 Rarity Presentation

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

# 5. Equipment Base Stats

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

## 5.1 Equipment Level

Equipment memiliki Equipment Level.

Equipment Level menentukan kekuatan dasar equipment dan rentang nilai affix.

Equipment Level berbeda dari Character Level dan Dungeon Level.

Equipment dapat memiliki minimum Character Level requirement.

Contoh:

`Flame Staff — Equipment Level 10 — Required Character Level 10`

Equipment yang tidak memenuhi level requirement tidak dapat dipasang.

---

# 6. Random Affix System

Affix merupakan bonus tambahan yang dihasilkan secara random ketika equipment dibuat.

Affix dapat memberikan bonus stat atau efek tertentu.

## 6.1 Affix Categories

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

Affix dapat memiliki flat value atau percentage value.

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

## 6.2 Affix Generation Rules

Affix generation menggunakan fixed affix count berdasarkan rarity.

Contoh:

Rare equipment selalu memiliki dua random affixes.

Epic equipment selalu memiliki tiga random affixes.

Legendary equipment selalu memiliki tiga random affixes.

Mythic equipment selalu memiliki empat random affixes.

Affix count tidak di-random.

## 6.3 Affix Selection

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

## 6.4 Affix Value Generation

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

# 7. Legendary & Mythic Effects

Legendary dan Mythic equipment memiliki special effect.

Special Effect merupakan efek unik yang tidak masuk ke random affix pool biasa.

## 7.1 Legendary Effect

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

## 7.2 Mythic Effect

Mythic Effect merupakan special effect untuk Mythic equipment.

Mythic equipment memiliki empat random affixes dan satu Mythic Effect.

Mythic Effect dapat memiliki mekanik yang lebih kompleks dibandingkan Legendary Effect.

Contoh:

**Frostheart Staff — Absolute Zero**

Elemental damage dealt to Frozen targets gains an additional damage bonus.

Efek tersebut merupakan contoh desain, bukan final gameplay value.

Special Effect tidak boleh melewati aturan dasar Damage & Status Effect System kecuali secara eksplisit dirancang sebagai exception.

---

# 8. Loot Generation System

Equipment dapat diperoleh melalui:

- Normal Enemy Drops
- Elite Enemy Drops
- Boss Rewards
- Dungeon Chests
- Dungeon Events
- Dungeon Shop
- Hub Armory

## 8.1 Loot Generation Flow

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

## 8.2 Dungeon Tier

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

# 9. Equipment Upgrade System

Equipment dapat di-upgrade melalui Blacksmith di Hub.

Equipment Upgrade hanya meningkatkan Base Stats.

Upgrade tidak mengubah:

- Equipment Rarity
- Random Affix
- Affix Value
- Legendary Effect
- Mythic Effect
- Equipment Definition

## 9.1 Upgrade Level

Equipment memiliki Upgrade Level.

Contoh:

`Infernal Edge +0 → Infernal Edge +1 → Infernal Edge +2`

Maximum Upgrade Level ditentukan oleh Equipment Definition atau global equipment rules.

## 9.2 Upgrade Effect

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

# 10. Equipment Persistence

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

## 10.1 Run Failure

Ketika seluruh party mati:

- Run Gold dihapus.
- Dungeon progression di-reset sesuai run failure rules.
- Run state di-reset.
- Equipment yang diperoleh selama run tetap persistent.
- Equipment yang sebelumnya dimiliki pemain tetap persistent.

## 10.2 Dungeon Clear

Ketika dungeon berhasil diselesaikan:

- Run Gold yang tersisa ditransfer ke Hub Gold.
- Equipment yang diperoleh selama run tetap persistent.
- Equipment yang dibeli selama run tetap persistent.
- Dungeon progression berlanjut sesuai sequential progression rules.

## 10.3 True New Game

New Game merupakan reset campaign yang disengaja.

True New Game menghapus persistent progression sesuai New Game rules.

Equipment Persistence Model C berlaku terhadap run failure, bukan terhadap reset campaign yang disengaja oleh pemain.

---

# 11. Character Stat Integration

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

# 12. Inventory UI

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

# 13. Equipment UI

Equipment UI menampilkan:

- Character Preview
- Weapon Slot
- Head Slot
- Body Slot
- Accessory 1 Slot
- Accessory 2 Slot
- Character Stats
- Equipment Detail
- Equipment Comparison

Ketika pemain memilih equipment slot, inventory memfilter equipment yang kompatibel.

Equipment comparison menampilkan perubahan stat sebelum equipment dipasang.

---

# 14. Equipment Selling

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

# 15. Unity Implementation Architecture

Sistem menggunakan pemisahan antara Equipment Definition dan Equipment Instance.

## 15.1 Equipment Definition

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

## 15.2 Equipment Instance

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

## 15.3 Inventory Runtime

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

## 15.4 Equipment Manager

Equipment Manager mengatur:

- Equip
- Unequip
- Replace Equipment
- Validate Compatibility
- Calculate Equipment Modifiers
- Update Character Stats

Equipment Manager tidak menyimpan equipment definition secara permanen di scene.

## 15.5 Equipment Loot Generator

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

# 16. Save & Load Integration

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

# 17. Development Milestones

## Milestone 1 — Data Foundation

- Item Definition
- Equipment Definition
- Equipment Instance
- Rarity
- Affix Definition
- Affix Generation

## Milestone 2 — Runtime Systems

- Inventory Runtime
- Equipment Manager
- Equipment Compatibility
- Equipment Stat Modifier
- Equipment Upgrade

## Milestone 3 — UI

- Inventory Screen
- Equipment Screen
- Item Detail
- Equipment Comparison
- Equip / Unequip

## Milestone 4 — Loot Integration

- Enemy Drops
- Boss Rewards
- Dungeon Chests
- Dungeon Shop
- Hub Armory

## Milestone 5 — Persistence

- Equipment Save Data
- Inventory Save Data
- Run Failure Persistence
- Dungeon Clear Persistence
- Save Slot Integration

## Milestone 6 — Polish

- Equipment Sorting
- Inventory Filtering
- Loot Presentation
- Rarity Visual Effects
- Equipment Comparison Feedback
- Legendary / Mythic Effect Presentation

---

# 18. Acceptance Criteria

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

---

# END OF DOCUMENT

**Inventory & Equipment System GDD v1.0**

Design finalized based on the selected Equipment Model C parameters.
