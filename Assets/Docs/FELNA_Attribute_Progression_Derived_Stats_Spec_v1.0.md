# FELNA — ATTRIBUTE-DRIVEN DERIVED STATS INTEGRATION & BALANCING SPECIFICATION

**Document ID:** `FELNA-SYS-ATTR-002` (Supersedes `FELNA-SYS-ATTR-001 v1.0`)  
**Version:** `2.0 — FINAL`  
**Date:** `2026-11-23`  
**Status:** `FINAL` (Seluruh elemen `CONFIRMED`, `APPROVED`, `PROPOSED`, dan `TBD` telah dikunci sebagai keputusan final)  
**Dokumen Terkait:** `GDD_Terbaru.md` (Bab 3, 5, 6, 7, 8, 9, 10, 11, 12, 13, 21), `Felna_QTE_Variation_Spec_v1.md`, `Felna_Items_and_Equipment_Catalogue_v2_Cursed_Equipment.xlsx`

> **Catatan Status Provenance:** Seluruh aturan, pemetaan Primary/Secondary Attribute, dan koefisien dalam dokumen ini berstatus **`FINAL`**. Tag di dalam tanda kurung mencatat asal-usul setiap keputusan untuk kebutuhan audit lintas dokumen:
> - `FINAL (Confirmed — GDD)` — sudah ditetapkan sebelumnya di GDD FELNA.
> - `FINAL (Locked — Approved)` — keputusan desain baru yang telah disetujui dan dikunci.
> - `FINAL (Locked — Proposal)` — usulan pemetaan/mekanisme yang telah disetujui menjadi final.
> - `FINAL (Locked — Resolved TBD)` — koefisien atau aturan yang sebelumnya TBD dan kini dikunci sebagai baseline final.

---

## 1. Executive Summary

### 1.1 Tujuan Desain & Filosofi Utama
**Attribute-Driven Derived Stats Framework (`FELNA-SYS-ATTR-002`)** mengintegrasikan keenam Primary Attributes—**Strength (`STR`)**, **Dexterity (`DEX`)**, **Vitality (`VIT`)**, **Intelligence (`INT`)**, **Wisdom (`WIS`)**, dan **Charisma (`CHA`)**—secara bermakna ke dalam seluruh sistem *Derived Stats* dan performa combat karakter.

Prinsip utama yang dikunci (`FINAL (Locked — Approved)`):
1. **Attributes dan Derived Stats Tetap Terhubung:** Statistik combat tidak dipisahkan menjadi sistem progresi mandiri yang terputus dari atribut. Setiap Derived Stat memiliki **Primary Attribute (`w_1`)** yang memberikan kontribusi dominan dan, pada stat komposit, **Secondary Attribute (`w_2 < w_1`)** yang memberikan kontribusi pendukung yang lebih kecil.
2. **Progresi Atribut & Hybrid Modifier Tetap Konsisten:** Point Buy **27 poin** di Level 1 (`8–15` pra-racial, maks `17` pasca-racial), **+2 AP** setiap *Skill Tier Unlock milestone* (`Level 4, 7, 10, 13, 16, 19, 22, 25, 28` = 18 AP s.d. Level 30; pada fase build aktif **Level Cap 15**, milestone aktif adalah `Level 4, 7, 10, 13` = 8 AP), *Free Allocation* + *banking*, Normal Progression Cap **20** (temporary buff > 20 hanya jika eksplisit), dan **Hybrid Modifier** $\text{Mod} = \lfloor (\text{Score} - 10) / 2 \rfloor$.
3. **Critical Chance Khusus Physical Damage (`FINAL (Confirmed — GDD Bab 11 §3 & Locked)`):** Hanya **Physical Damage** langsung yang memiliki *Critical Chance* (`Primary: DEX, Secondary: STR`). *Elemental Damage*, *Almighty Damage*, dan *DoT* **tidak** memiliki Critical Chance.
4. **Identitas Combat Penuh untuk Charisma (`CHA`):** `CHA` memiliki tiga pilar kontribusi mekanis di dalam combat (**Commanding Presence**, **Disruptive Presence**, **Inspiring Presence**) yang memimpin `Buff Effectiveness`, `Debuff Effectiveness`, dan `Support Skill Power`, serta mendukung `Shield Power` sebagai Secondary Attribute tanpa menggantikan peran `WIS` (Healing Power) maupun `INT` (Elemental Damage & Max MP).

---

## 2. Existing System Audit & Contradiction Resolutions

### 2.1 Audit Sumber Proyek FELNA

| Sumber Dokumen | Sistem yang Diaudit | Aturan Terkunci yang Dipertahankan (`FINAL (Confirmed — GDD)`) |
|---|---|---|
| `GDD_Terbaru.md` **Bab 3 (§2.1–2.4)** | Difficulty System | *Relentless Tactical Survival*; *Controlled RNG* (tanpa bad-luck protection; Control Effects tidak self-stack dan memberi 1-turn Control Immunity). |
| `GDD_Terbaru.md` **Bab 5 & Bab 6** | Character Creation & Race | 6 Ability Scores, Point Buy 27 poin (`8–15`), Racial Bonus pasca-Point Buy (maks `17`), Normal Cap `20`, +2 AP di Skill Tier Unlock milestones. |
| `GDD_Terbaru.md` **Bab 8 (§1–§13)** | Class System (9 Classes) | Hanya 2 resource pool (**HP** dan **MP**); 3 kategori damage (**Physical**, **Elemental**, **Almighty**); Cleric §8.1: *Divine Grace* hanya menaikkan Healing Power dan tidak otomatis menaikkan Shield Power. |
| `GDD_Terbaru.md` **Bab 9 (§1–§5)** | Talents System | Pilihan manual di Lv 3, 6, 9, 12, 15; *Durable* (`+10% Max HP`, `5% DR`), *Inspiring Leader* (`10% DR`), *Healer* (`+15% Healing`), *Alert* (`-10 pp Enemy Crit`), *Quick Reflex* / *Steady Hand* (`+10% QTE Window`). |
| `GDD_Terbaru.md` **Bab 10 (§5.3, §7–§8)** | Turn Order, Broken & Scaling | Dynamic Initiative Queue berbasis `EffectiveSPD`; tie-breaker deterministik; **Speed tidak pernah memberi extra turn**; Enemy Break Gauge (`100` Normal/Elite, `200` Boss); Player Poise Gauge (`60`). |
| `GDD_Terbaru.md` **Bab 11 (§1–§13)** | Damage & Status Effect | Base Damage `SkillPower + (Stat × Scaling)`; Physical Crit (`Base 5%`, `150%`, cap `95% / 300%`, finisher & Elemental tidak bisa Crit); Status Resistance & Mental Resistance additive, dua pool terpisah (`[-85%, +85%]`, revisi 25 Nov 2026); Boss 100% Mental Immunity; `floor()` sekali per hit. |
| `GDD_Terbaru.md` **Bab 12 (§3.4–3.5, §6.5, §8.2)** | QTE, Block & Counter | Shared QTE Window cap `+25%` (`W_P ∈ [66, 125] ms`); Defensive QTE memakai `Block Power` (`cap 90%`, Perfect non-Heavy `1 - min(0.75, block)`, Good / Heavy Perfect `1 - 0.5 × block`, Full Parry `0 dmg` hanya saat Guard + Perfect non-Heavy); Counter pada Defensive Perfect dengan power `M × (1 + CounterPower / 100)`. |

### 2.2 Resolusi Konflik Desain (`FINAL (Locked — Approved)`)

| ID | Konflik yang Ditemukan | Definisi Dokumen / Proposal | Keputusan Resolusi Final (`FINAL`) |
|---|---|---|---|
| **C-01** | **Isolasi Atribut (`v1.0`) vs. Integrasi Atribut (`v2.0`)** | `v1.0` memisahkan Defense, Block, Counter, Break, Speed, Crit, Penetration, Shield, DR, dan Status Resistance dari atribut, sedangkan spesifikasi baru mewajibkan seluruh Derived Stats tetap terhubung dengan 6 Ability Scores. | **Diintegrasikan Penuh (`FINAL`):** Seluruh 17 Derived Combat Stats kembali terhubung ke Primary (`w_1`) dan Secondary (`w_2 < w_1`) Attribute Modifiers di atas Class Base, Equipment, dan Passive. |
| **C-02** | **Elemental Critical Chance vs. Aturan Critical Physical-Only** | Tabel proposal awal mencantumkan baris *Elemental Critical Chance*, sedangkan `GDD Bab 8 §2.2 & Bab 11 §3` menegaskan hanya Physical Damage yang dapat Critical. | **Dihapus / Physical-Only (`FINAL`):** Critical Chance **hanya berlaku untuk Physical Damage** (`Primary: DEX, Secondary: STR`). Elemental Damage tidak memiliki Critical Chance. |
| **C-03** | **Status Resistance Governing Attributes** | Proposal awal menawarkan `WIS` dengan opsi `VIT / CHA`. | **Dikunci (`FINAL`):** **Status Resistance** menggunakan **`Primary: VIT (+2.0%/mod)`** dan **`Secondary: WIS (+1.0%/mod)`**, ditumpuk additive dalam rentang `[-85%, +85%]` (`GDD Bab 11 §9`, revisi 25 Nov 2026). **Mental Resistance** (pool terpisah untuk Mental Effects) menggunakan **`Primary: WIS (+2.0%/mod)`** dan **`Secondary: INT (+1.0%/mod)`**. |
| **C-04** | **QTE Stability vs. QTE Timing Window** | `GDD Bab 12 §3.4 & §8.2` melarang perubahan `R_QTE` dan membatasi pelebaran window dasar pada `DEX` (`+3%/mod`, cap `+25%`). `QTE Stability` harus memiliki fungsi berbeda agar tidak menduplikasi `QTE Window`. | **Dikunci (`FINAL`):** **QTE Stability** menggunakan **`Primary: DEX (+2.0%/mod)`** dan **`Secondary: WIS (+1.0%/mod)`** (cap `50%`). Fungsinya: (1) mengurangi penalti penyempitan window akibat `Freeze` (`-15%`) dan mengurangi intensitas distorsi visual status (`ShakePx` / memperlebar `ClearZone` `150→220 ms`) sebesar `1%` relatif per poin, serta (2) memperlebar batas toleransi luar `PartialSuccess` pada evaluasi 4-kategori tanpa mengubah `W_P`, `W_G`, atau `R_QTE`. |
| **C-05** | **Independensi Shield Power Cleric vs. Atribut** | `GDD Bab 8 §8.1` menetapkan *Divine Grace* meningkatkan Healing Power dan tidak otomatis meningkatkan Shield Power; Shield memakai modifier-nya sendiri. | **Dikunci (`FINAL`):** `Shield Power` adalah Derived Stat tersendiri (**`Primary: WIS (+3.5%/mod)`, `Secondary: CHA (+1.5%/mod)`**) yang terpisah dari `Healing Power` (**`Primary: WIS (+4.0%/mod)` saja**). Passive healing (*Divine Grace*, *Healer*) hanya memodifikasi `Healing Power`; passive shield (*Protective Faith*, *Divine Aegis*) hanya memodifikasi `Shield Power`. |
| **C-06** | **Magic Defense & Magic Penetration vs. Elemental Resistance** | `BattleAction.Rules.Magic` dan `GDD Bab 10 §7.3` menggunakan flat `MDEF` (`Magic Defense`), sedangkan `DamageCalculator` Bab 11 §5 memiliki `% Elemental Resistance` per elemen (cap `75%`). | **Dikunci (`FINAL`):** Flat **`Magic Defense`** (`Primary: INT, Secondary: WIS`) dan flat **`Magic Penetration`** (`Primary: INT, Secondary: WIS`) dipertahankan pada `DerivedStats` untuk mitigasi `MATK vs MDEF`, berdampingan dengan `% Elemental Resistance` per elemen (`0–75%`) dari equipment/affinity/buff. |

---

## 3. Six Attribute Identities

### 3.1 Strength (`STR`) — Physical Force, Armor Posture & Break Impact
- **Identitas Utama:** Kekuatan otot, daya rusak senjata fisik, ketahanan menopang armor berat, serta daya dobrak terhadap pertahanan dan postur lawan.
- **Direct Skill Scaling (`FINAL (Confirmed — GDD Bab 11 §2.1)`):** Raw `STR` meningkatkan `Base Physical Damage = SkillPower + (STR × Scaling)` pada skill fisik yang menyatakan scaling `STR`.
- **Primary Derived Stats (`w_1`):**
  - **Physical Defense (`DEF`)** (`+1.5` per `STR Mod`)
  - **Armor Penetration (`APN`)** (`+1.2` per `STR Mod`)
  - **Block Power (`BLK`)** (`+2.0%` per `STR Mod`)
  - **Universal Break Power (`BRK`)** (`+1.5` per `STR Mod`)
- **Secondary Derived Stats (`w_2`):**
  - **Damage Reduction (`DR`)** (`+0.5%` per `STR Mod`, mendukung `VIT`)
  - **Stagger Resistance (`STRS`)** (`+1.0%` per `STR Mod`, mendukung `VIT`)
  - **Physical Critical Chance (`CRT`)** (`+0.5%` per `STR Mod`, mendukung `DEX`)
  - **Counter Power (`CNT`)** (`+1.5%` per `STR Mod`, mendukung `DEX`)

### 3.2 Dexterity (`DEX`) — Precision, Tempo, Evasion & Reaction Execution
- **Identitas Utama:** Presisi serangan jarak jauh, kelincahan menghindar, kecepatan inisiatif, ketajaman titik lemah (Critical & Counter), serta eksekusi reaksi QTE.
- **Core Derived Stats (`FINAL (Confirmed — GDD Bab 5 §7 & Bab 12 §8.2)`):**
  - **Ranged Accuracy Bonus** (`+2.0%` per `DEX Mod`, khusus `AttackCategory.Ranged`)
  - **Evasion (`EVA`)** (`+2.0%` per `DEX Mod`, cap `75%`)
  - **Timing QTE Window Bonus (`QTE_TIME`)** (`+3.0%` per `DEX Mod`, shared cap `+25%`, floor `-15%`)
- **Primary Derived Stats (`w_1`):**
  - **Initiative (`INI` / `SPD`)** (`+1.0` per `DEX Mod`)
  - **Action Speed (`ACT_SPD`)** (`+1.5` per `DEX Mod`)
  - **Physical Critical Chance (`CRT`)** (`+1.5%` per `DEX Mod`, khusus Physical Damage)
  - **Counter Power (`CNT`)** (`+2.5%` per `DEX Mod`)
  - **QTE Stability (`QTE_STAB`)** (`+2.0%` per `DEX Mod`)
- **Secondary Derived Stats (`w_2`):**
  - **Armor Penetration (`APN`)** (`+0.6` per `DEX Mod`, mendukung `STR`)
  - **Universal Break Power (`BRK`)** (`+0.75` per `DEX Mod`, mendukung `STR`)

### 3.3 Vitality (`VIT`) — Endurance, Bodily Resilience & Poise
- **Identitas Utama:** Daya tahan tubuh, kapasitas nyawa, ketahanan terhadap efek status/racun/penyakit, reduksi damage menyeluruh, dan keteguhan postur (Poise).
- **Primary Resource Pool (`FINAL (Locked — Approved)`):**
  - **Max HP** (`+4.0% Base HP` per `VIT Mod`, non-compounding)
- **Primary Derived Stats (`w_1`):**
  - **Damage Reduction (`DR`)** (`+1.0%` per `VIT Mod`, shared cap `25%`)
  - **Status Resistance (`STAT_RES`)** (`+2.0%` per `VIT Mod`, additive, rentang `[-85%, +85%]`)
  - **Stagger Resistance (`STG_RES`)** (`+2.0%` per `VIT Mod`, cap `75%`)
- **Secondary Derived Stats (`w_2`):**
  - **Physical Defense (`DEF`)** (`+0.75` per `VIT Mod`, mendukung `STR`)
  - **Block Power (`BLK`)** (`+1.0%` per `VIT Mod`, mendukung `STR`)

### 3.4 Intelligence (`INT`) — Elemental Mastery, Arcane Analysis & Mana Capacity
- **Identitas Utama:** Penguasaan sihir elemental, kapasitas mana, analisis pertahanan magis lawan, dan kecepatan kalkulasi taktis.
- **Direct Skill Scaling & Resource Pool (`FINAL (Confirmed — GDD Bab 11 §2.1 & Approved)`):**
  - **Base Elemental & Almighty Damage** (`SkillPower + (INT × Scaling)`, menggunakan Raw `INT`)
  - **Max MP** (`+4.0% Base MP` per `INT Mod`, non-compounding)
- **Primary Derived Stats (`w_1`):**
  - **Magic Defense (`MDF`)** (`+1.5` per `INT Mod`)
  - **Magic Penetration (`MPN`)** (`+1.2` per `INT Mod`)
- **Secondary Derived Stats (`w_2`):**
  - **Initiative (`INI` / `SPD`)** (`+0.5` per `INT Mod`, mendukung `DEX`)
  - **Action Speed (`ACT_SPD`)** (`+0.75` per `INT Mod`, mendukung `DEX`)
  - **Debuff Effectiveness (`DEBUFF_EFF`)** (`+1.5%` per `INT Mod`, mendukung `CHA`)

### 3.5 Wisdom (`WIS`) — Spiritual Insight, Restoration, Ward & Composure
- **Identitas Utama:** Pemulihan HP (Healing), kekuatan perisai pelindung (Shield), ketenangan batin menghadapi status/distorsi QTE, dan wawasan pertahanan spiritual.
- **Primary Restorative Stat (`FINAL (Locked — Approved)`):**
  - **Healing Power (`HEL`)** (`+4.0% Base Healing` per `WIS Mod`, non-compounding)
- **Primary Derived Stats (`w_1`):**
  - **Shield Power (`SHD_PWR`)** (`+3.5% Base Shield` per `WIS Mod`, terpisah dari Healing Power)
- **Secondary Derived Stats (`w_2`):**
  - **Status Resistance (`STAT_RES`)** (`+1.0%` per `WIS Mod`, mendukung `VIT`)
  - **Mental Resistance (`MENT_RES`)** (`+2.0%` per `WIS Mod`, Primary)
  - **Magic Defense (`MDF`)** (`+0.75` per `WIS Mod`, mendukung `INT`)
  - **Magic Penetration (`MPN`)** (`+0.6` per `WIS Mod`, mendukung `INT`)
  - **QTE Stability (`QTE_STAB`)** (`+1.0%` per `WIS Mod`, mendukung `DEX`)
  - **Buff Effectiveness (`BUFF_EFF`)** (`+1.5%` per `WIS Mod`, mendukung `CHA`)
  - **Support Skill Power (`SUP_PWR`)** (`+1.5%` per `WIS Mod`, mendukung `CHA`)

### 3.6 Charisma (`CHA`) — Commanding, Disruptive & Inspiring Presence
- **Identitas Utama:** Kepemimpinan medan tempur, penguatan buff rekan, pelemahan mental/debuff musuh, daya dukung skill komando, dan inspirasi perisai pelindung, serta interaksi sosial/komando dalam Exploration Event melalui sistem D20 Attribute Check (`FELNA-SYS-EVT-D20-001`).
- **Primary Derived Stats (`w_1`):**
  - **Buff Effectiveness (`BUFF_EFF`)** (`+4.0%` per `CHA Mod`)
  - **Debuff Effectiveness (`DEBUFF_EFF`)** (`+3.5%` per `CHA Mod`)
  - **Support Skill Power (`SUP_PWR`)** (`+3.0%` per `CHA Mod`)
- **Exploration Event Check (`FINAL — FELNA-SYS-EVT-D20-001`):**
  - **D20 Attribute Check (`STR / DEX / VIT / INT / WIS / CHA`):** Menggantikan formula pasif lama (`10 + CHA Mod + Social Bonuses`) dengan sistem seragam `Check Result = D20 Roll + Party Highest Attribute Modifier` (hanya dari anggota party yang hidup/aktif `HP > 0`) vs Event `Difficulty Class (DC)`.
- **Secondary Derived Stats (`w_2`):**
  - **Shield Power (`SHD_PWR`)** (`+1.5% Base Shield` per `CHA Mod`, mendukung `WIS`)

---

## 4. Attribute-to-Derived-Stat Mapping Table

Tabel pemetaan final di bawah ini mengunci tepat satu **Primary Attribute (`w_1`)** dan maksimal satu **Secondary Attribute (`w_2 < w_1`)** untuk setiap Derived Stat (`FINAL (Locked — Approved)`):

| Kategori | Derived Stat | Stat ID | Primary Attribute (`w_1`) | Secondary Attribute (`w_2`) | Formula Ringkas | Clamp / Cap | Status |
|---|---|---|---|---|---|---|---|
| **Direct / Resource** | **Base Physical Damage** | `PHYS_PWR` | **Raw `STR`** | *None* | `SkillPower + (STR × Scaling)` | Min `0` | `FINAL (Confirmed — GDD)` |
| **Direct / Resource** | **Base Elemental / Almighty Damage** | `MAG_PWR` | **Raw `INT`** | *None* | `SkillPower + (INT × Scaling)` | Min `0` | `FINAL (Confirmed — GDD)` |
| **Direct / Resource** | **Max HP** | `MAX_HP` | **`VIT` Mod** (`+4.0%`) | *None* | `floor(BaseHP × (1 + 0.04×VITMod + Pct) + Flat)` | Min `1` | `FINAL (Locked — Approved)` |
| **Direct / Resource** | **Max MP** | `MAX_MP` | **`INT` Mod** (`+4.0%`) | *None* | `floor(BaseMP × (1 + 0.04×INTMod + Pct) + Flat)` | Min `0` | `FINAL (Locked — Approved)` |
| **Direct / Resource** | **Healing Power** | `HEAL_PWR` | **`WIS` Mod** (`+4.0%`) | *None* | `floor(BaseHeal × (1 + 0.04×WISMod + Pct))` | Min `0` | `FINAL (Locked — Approved)` |
| **Direct / Resource** | **Ranged Accuracy Bonus** | `RNG_ACC` | **`DEX` Mod** (`+2.0%`) | *None* | `2% × DEXMod` (khusus Ranged) | Hit `[5%, 95%]` | `FINAL (Locked — Proposal)` |
| **Defensive** | **Evasion** | `EVASION` | **`DEX` Mod** (`+2.0%`) | *None* | `BaseEva + 2%×DEXMod + Bonuses` | `[0%, 75%]` | `FINAL (Locked — Proposal)` |
| **Defensive** | **Shield Power** | `MAX_SHIELD` | **`WIS` Mod** (`+3.5%`) | **`CHA` Mod** (`+1.5%`) | `floor(BaseShield × (1 + 0.035×WISMod + 0.015×CHAMod + Pct) + Flat)` | Bonus `[-25%, +50%]` | `FINAL (Locked — Resolved TBD)` |
| **Defensive** | **Damage Reduction** | `DMG_RED` | **`VIT` Mod** (`+1.0%`) | **`STR` Mod** (`+0.5%`) | `BaseDR + 1.0%×VITMod + 0.5%×STRMod + Bonuses` | **`[0%, 25%]`** | `FINAL (Locked — Resolved TBD)` |
| **Defensive** | **Status Resistance** | `STAT_RES` | **`VIT` Mod** (`+2.0%`) | **`WIS` Mod** (`+1.0%`) | `BaseRes + 2.0%×VITMod + 1.0%×WISMod + Bonuses` | **`[-85%, +85%]`** | `FINAL (Revised 2026-11-25)` |
| **Defensive** | **Mental Resistance** | `MENT_RES` | **`WIS` Mod** (`+2.0%`) | **`INT` Mod** (`+1.0%`) | `BaseMentalRes (0%) + 2.0%×WISMod + 1.0%×INTMod + Bonuses` | **`[-85%, +85%]`** | `Posture B (Added 2026-11-25)` |
| **Defensive** | **Stagger Resistance** | `STG_RES` | **`VIT` Mod** (`+2.0%`) | **`STR` Mod** (`+1.0%`) | `BaseStg + floor(2.0×VITMod + 1.0×STRMod) + Bonuses` | **`[0%, 75%]`** | `FINAL (Locked — Resolved TBD)` |
| **Defensive** | **Physical Defense** | `PHYS_DEF` | **`STR` Mod** (`+1.5`) | **`VIT` Mod** (`+0.75`) | `BaseDef + floor(1.5×STRMod + 0.75×VITMod) + Gear` | Min `0` | `FINAL (Locked — Resolved TBD)` |
| **Defensive** | **Magic Defense** | `MAG_DEF` | **`INT` Mod** (`+1.5`) | **`WIS` Mod** (`+0.75`) | `BaseMDef + floor(1.5×INTMod + 0.75×WISMod) + Gear` | Min `0` | `FINAL (Locked — Resolved TBD)` |
| **Defensive** | **Block Power** | `BLK_PWR` | **`STR` Mod** (`+2.0`) | **`VIT` Mod** (`+1.0`) | `BaseBlk + floor(2.0×STRMod + 1.0×VITMod) + Shield + Passives` | **`[0, 90]`** | `FINAL (Locked — Resolved TBD)` |
| **Offensive** | **Physical Critical Chance** | `CRIT_CHC` | **`DEX` Mod** (`+1.5%`) | **`STR` Mod** (`+0.5%`) | `BaseCrit + floor(1.5×DEXMod + 0.5×STRMod) + Bonuses` | **`[0%, 95%]`** | `FINAL (Locked — Approved)` |
| **Offensive** | **Armor Penetration** | `ARM_PEN` | **`STR` Mod** (`+1.2`) | **`DEX` Mod** (`+0.6`) | `BaseAPen + floor(1.2×STRMod + 0.6×DEXMod) + Bonuses` | Min `0` | `FINAL (Locked — Resolved TBD)` |
| **Offensive** | **Magic Penetration** | `MAG_PEN` | **`INT` Mod** (`+1.2`) | **`WIS` Mod** (`+0.6`) | `BaseMPen + floor(1.2×INTMod + 0.6×WISMod) + Bonuses` | Min `0` | `FINAL (Locked — Resolved TBD)` |
| **Offensive** | **Counter Power** | `CNT_PWR` | **`DEX` Mod** (`+2.5`) | **`STR` Mod** (`+1.5`) | `BaseCnt + floor(2.5×DEXMod + 1.5×STRMod) + Bonuses` | `[0, 100]` | `FINAL (Locked — Resolved TBD)` |
| **Offensive** | **Universal Break Power** | `P_BREAK` / `M_BREAK` | **`STR` Mod** (`+1.5`) | **`DEX` Mod** (`+0.75`) | `BaseBrk + WeaponBrk + floor(1.5×STRMod + 0.75×DEXMod) + Bonuses` | Min `1` | `FINAL (Locked — Resolved TBD)` |
| **Execution** | **Initiative (`SPD`)** | `INIT` | **`DEX` Mod** (`+1.0`) | **`INT` Mod** (`+0.5`) | `BaseSpd + floor(1.0×DEXMod + 0.5×INTMod) + Bonuses` | Min `1` | `FINAL (Locked — Resolved TBD)` |
| **Execution** | **Action Speed** | `ACT_SPD` | **`DEX` Mod** (`+1.5`) | **`INT` Mod** (`+0.75`) | `BaseActSpd + floor(1.5×DEXMod + 0.75×INTMod) + Bonuses` | Min `1` | `FINAL (Locked — Resolved TBD)` |
| **Execution** | **Timing QTE Window** | `QTE_TIME` | **`DEX` Mod** (`+3.0%`) | *None* | `clamp(3%×DEXMod + WindowBonuses, -15%, +25%)` | **`[-15%, +25%]`** | `FINAL (Confirmed — GDD)` |
| **Execution** | **QTE Stability** | `QTE_STAB` | **`DEX` Mod** (`+2.0%`) | **`WIS` Mod** (`+1.0%`) | `BaseStab + floor(2.0×DEXMod + 1.0×WISMod) + Bonuses` | **`[0%, 50%]`** | `FINAL (Locked — Approved)` |
| **Support** | **Buff Effectiveness** | `BUFF_EFF` | **`CHA` Mod** (`+4.0%`) | **`WIS` Mod** (`+1.5%`) | `clamp(4.0%×CHAMod + 1.5%×WISMod + Bonuses, -20%, +40%)` | **`[-20%, +40%]`** | `FINAL (Locked — Resolved TBD)` |
| **Support** | **Debuff Effectiveness** | `DEBUFF_EFF` | **`CHA` Mod** (`+3.5%`) | **`INT` Mod** (`+1.5%`) | `clamp(3.5%×CHAMod + 1.5%×INTMod + Bonuses, -20%, +40%)` | **`[-20%, +40%]`** | `FINAL (Locked — Resolved TBD)` |
| **Support** | **Support Skill Power** | `SUP_PWR` | **`CHA` Mod** (`+3.0%`) | **`WIS` Mod** (`+1.5%`) | `clamp(BaseSup + 3.0%×CHAMod + 1.5%×WISMod + Bonuses, -20%, +50%)` | **`[-20%, +50%]`** | `FINAL (Locked — Resolved TBD)` |

---

## 5. Detailed Derived Stat Definitions

### 5.1 Defensive Statistics

1. **Shield Power (`MAX_SHIELD` / `shieldPowerBonus`)**
   - **Fungsi:** Meningkatkan besaran barrier Shield yang diberikan oleh skill (*Divine Shield*, *Sanctuary*, *Iron Fortress*), passive, atau efek perisai yang mendukung scaling Shield (`FinalShield = floor(BaseShield × (1 + ShieldPowerBonus))`). Shield menyerap damage integer setelah Defensive QTE (`GDD Bab 11 §2.3`).
   - **Primary Attribute:** `WIS` (`+3.5%` per modifier).
   - **Secondary Attribute:** `CHA` (`+1.5%` per modifier).
   - **Interaksi Sistem:** Terpisah dari `Healing Power` (`GDD Bab 8 §8.1`). *Divine Grace* dan *Healer* tidak menaikkan Shield Power; *Protective Faith* dan *Divine Aegis* menaikkan Shield Power.
   - **Representasi & Batas:** Persentase multiplier (`[-25%, +50%]`) dan nilai flat preview pada lembar karakter (minimum `0`).

2. **Damage Reduction (`DMG_RED` / `damageReductionBonus`)**
   - **Fungsi:** Mengurangi seluruh damage yang diterima (`All Damage Taken −x%`) sebelum reduksi Defensive QTE (`D_akhir = floor(D × ∏(1 − DR_i) × (1 − R_QTE))` sesuai `GDD Bab 12 §5.1`).
   - **Primary Attribute:** `VIT` (`+1.0%` per modifier).
   - **Secondary Attribute:** `STR` (`+0.5%` per modifier).
   - **Interaksi Sistem:** Ditumpuk di dalam pool Damage Reduction karakter bersama modifier/passive DR dan dibatasi oleh shared cap **`25%`** (`DungeonModifierTests.DamageReductionCap_25Percent`). Stance aktif pemakan giliran (*Guard* `0.5×`, *Iron Resolve* `-30%`) tetap mengalikan secara terpisah.
   - **Representasi & Batas:** Persentase (`[0%, 25%]`).

3. **Status Resistance (`STAT_RES`)**
   - **Fungsi:** Mengurangi peluang masuknya Status Effect **non-Mental** musuh (`P_final = P_base × (1 − R_effective)` sesuai `GDD Bab 11 §9`). Mental Effects memakai Mental Resistance (lihat 3b).
   - **Primary Attribute:** `VIT` (`+2.0%` per modifier).
   - **Secondary Attribute:** `WIS` (`+1.0%` per modifier).
   - **Interaksi Sistem:** Mengisi komponen `R_stats` pada `StatusResistanceProfile`, ditumpuk additive dengan `R_base + R_equipment + R_buffs + R_passives` lalu di-clamp ke **`[-85%, +85%]`**, lalu nilai positif dikalikan secara multiplicative oleh debuff `ResistanceDown` (nilai negatif tidak diubah).
   - **Representasi & Batas:** Persentase (`[-85%, +85%]`). Nilai negatif menaikkan peluang status masuk.

3b. **Mental Resistance (`MENT_RES`)**
   - **Fungsi:** Mengurangi peluang masuknya Mental Effects (Mind Control, Confusion, Fear, Despair, Charm, Sleep, Silence, Madness). Tidak menumpuk dengan Status Resistance.
   - **Primary Attribute:** `WIS` (`+2.0%` per modifier).
   - **Secondary Attribute:** `INT` (`+1.0%` per modifier).
   - **Interaksi Sistem:** Mengisi `mentalStatsResistance` pada `StatusResistanceProfile` (Equipment & Passives masuk lewat derived stat), ditambah buff Mental. Rentang `[-85%, +85%]`; `ResistanceDown` ikut mereduksi nilai positif. Boss tetap 100% immune terhadap Mental Effects; Mental Resistance dan reduksi tidak menembus immunity (`GDD Bab 11 §13`).
   - **Representasi & Batas:** Persentase (`[-85%, +85%]`), base `0%` untuk semua class (Posture B).

4. **Stagger Resistance (`STG_RES`)**
   - **Fungsi:** Mengurangi Break Power (Poise damage) yang diterima hero terhadap `PlayerPoiseGauge = 60` sebelum faktor `(1 − R_QTE)` Defensive QTE (`GDD Bab 10 §5.3`).
   - **Primary Attribute:** `VIT` (`+2.0%` per modifier).
   - **Secondary Attribute:** `STR` (`+1.0%` per modifier).
   - **Interaksi Sistem:** Melindungi hero dari status `Broken` (`1.25×` damage taken dan penutupan zona Perfect Defensive QTE).
   - **Representasi & Batas:** Persentase (`[0%, 75%]`).

5. **Physical Defense (`PHYS_DEF`)**
   - **Fungsi:** Pengurang flat terhadap Physical Damage setelah Critical Hit dan sebelum `Physical Damage Taken` (`afterDef = max(0, afterCrit − max(0, TargetPhysDef − AttackerArmorPen))` sesuai `GDD Bab 11 §7.1`).
   - **Primary Attribute:** `STR` (`+1.5` per modifier).
   - **Secondary Attribute:** `VIT` (`+0.75` per modifier).
   - **Interaksi Sistem:** Dapat menurunkan Physical Damage hingga `0` (tidak ada minimum damage 1). Diabaikan oleh Elemental dan Almighty Damage.
   - **Representasi & Batas:** Integer flat (minimum `0`).

6. **Magic Defense (`MAG_DEF`)**
   - **Fungsi:** Pertahanan flat terhadap serangan magis/elemental pada resolusi `MATK vs MDEF` (`GDD Bab 10 §7.3`), dikurangi oleh `Magic Penetration` penyerang.
   - **Primary Attribute:** `INT` (`+1.5` per modifier).
   - **Secondary Attribute:** `WIS` (`+0.75` per modifier).
   - **Interaksi Sistem:** Bekerja berdampingan dengan `% Elemental Resistance` per elemen (`GDD Bab 11 §5`, cap `75%`).
   - **Representasi & Batas:** Integer flat (minimum `0`).

7. **Block Power (`BLK_PWR`)**
   - **Fungsi:** Menentukan rasio mitigasi (`block = BlockPower / 100`) saat Defensive QTE berhasil (`GDD Bab 12 §3.5`):
     - Perfect (non-Heavy): `1 − min(0.75, block)`
     - Good atau Heavy Perfect: `1 − 0.5 × block`
     - Full Parry (`0` damage): hanya saat `Guard` + non-Heavy `Perfect`.
   - **Primary Attribute:** `STR` (`+2.0` per modifier).
   - **Secondary Attribute:** `VIT` (`+1.0` per modifier).
   - **Interaksi Sistem:** Ditambahkan ke Class Base (`50`), Shield equipment (`Iron Shield +8`), Background (`Oathsworn +4`), dan Passive (`Shield Mastery +15`, `Divine Aegis +8`).
   - **Representasi & Batas:** Poin persentase integer (`[0, 90]`, dengan Perfect non-Guard dibatasi `75%`).

---

### 5.2 Offensive Statistics

8. **Physical Critical Chance (`CRIT_CHC`)**
   - **Fungsi:** Peluang (`%`) per hit bagi serangan **Physical Damage langsung** untuk menghasilkan Critical Hit (`Base Critical Damage = 150%`, cap `300%`) setelah QTE dan sebelum `Physical Defense` (`GDD Bab 11 §3`).
   - **Primary Attribute:** `DEX` (`+1.5%` per modifier).
   - **Secondary Attribute:** `STR` (`+0.5%` per modifier).
   - **Interaksi Sistem:** **Hanya berlaku untuk Physical Damage.** Elemental Damage, Almighty Damage, DoT, dan Finisher (*Dragon's Fury*, *Final Technique*) **tidak memiliki Critical Chance** (`FINAL (Confirmed — GDD Bab 11 §3 & Locked)`).
   - **Representasi & Batas:** Persentase (`Base 5%`, clamp `[0%, 95%]`).

9. **Armor Penetration (`ARM_PEN`)**
   - **Fungsi:** Mengabaikan sejumlah poin flat `Physical Defense` target saat menghitung Physical Damage (`EffectivePhysDef = max(0, TargetPhysDef − AttackerArmorPen)`).
   - **Primary Attribute:** `STR` (`+1.2` per modifier).
   - **Secondary Attribute:** `DEX` (`+0.6` per modifier).
   - **Interaksi Sistem:** Tidak dapat menurunkan `Physical Defense` target di bawah `0` dan tidak menembus Physical Nullification.
   - **Representasi & Batas:** Integer flat (minimum `0`).

10. **Magic Penetration (`MAG_PEN`)**
    - **Fungsi:** Mengabaikan sejumlah poin flat `Magic Defense (MDEF)` target saat menghitung serangan magis/elemental (`EffectiveMagDef = max(0, TargetMagDef − AttackerMagicPen)`).
    - **Primary Attribute:** `INT` (`+1.2` per modifier).
    - **Secondary Attribute:** `WIS` (`+0.6` per modifier).
    - **Interaksi Sistem:** Tidak menembus Elemental Nullification (yang tetap membutuhkan *Null Break* sesuai `GDD Bab 11 §4`).
    - **Representasi & Batas:** Integer flat (minimum `0`).

11. **Counter Power (`CNT_PWR`)**
    - **Fungsi:** Meningkatkan damage serangan balasan (*Counterattack*, *Counterstrike*, *Perfect Counter*) yang terpicu oleh **Defensive Perfect** (`GDD Bab 12 §6.5`):
      $$\text{Counter Damage} = \text{PhysicalHit}_{\text{Good}} \times M \times \left(1 + \frac{\text{Counter Power}}{100}\right)$$
    - **Primary Attribute:** `DEX` (`+2.5` per modifier).
    - **Secondary Attribute:** `STR` (`+1.5` per modifier).
    - **Interaksi Sistem:** Mengalikan serangan dasar Physical setara Good bersama multiplier skill `M` (`1.0×` / `1.5×`) dan *Draconic Overdrive* (`1.25×`).
    - **Representasi & Batas:** Poin persentase integer (`Base 10`, clamp `[0, 100]`).

12. **Universal Break Power (`P_BREAK` / `M_BREAK`)**
    - **Fungsi:** Besaran kerusakan Break Gauge dasar yang diberikan serangan/skill ber-damage langsung kepada musuh sebelum dikalikan multiplier grade Offensive QTE (`Perfect 1.5× / Good 1.0× / Poor 0.5×` sesuai `GDD Bab 11 §2.5 & Bab 12 §2.5`).
    - **Primary Attribute:** `STR` (`+1.5` per modifier).
    - **Secondary Attribute:** `DEX` (`+0.75` per modifier).
    - **Interaksi Sistem:** Menjumlahkan Class Base Break Power, 100% Main-Hand + 100% Off-Hand Weapon Break Power, Passive (*Martial Discipline*), dan kontribusi `STR + DEX`. Terpisah dari Break Power Received, Stagger Resistance, dan QTE Break Multiplier.
    - **Representasi & Batas:** Integer flat (minimum `1`).

---

### 5.3 Combat Execution Statistics

13. **Initiative (`INIT` / `SPD`)**
    - **Fungsi:** Menentukan `ScaledSPD` unit dalam **Dynamic Initiative Queue** setiap awal round (`EffectiveSPD = max(1, floor(ScaledSPD × clamp(1 + Bonus − Penalty, 0.50, 1.50)))` sesuai `GDD Bab 10 §8.2`).
    - **Primary Attribute:** `DEX` (`+1.0` per modifier).
    - **Secondary Attribute:** `INT` (`+0.5` per modifier).
    - **Interaksi Sistem:** Mengatur urutan giliran secara deterministik tanpa RNG. **Tidak pernah memberikan extra turn atau aksi tambahan** (`GDD Bab 10 §8.1`).
    - **Representasi & Batas:** Integer flat (minimum `1`).

14. **Action Speed (`ACT_SPD`)**
    - **Fungsi:** Rating tempo eksekusi untuk skill yang membaca perbandingan kecepatan (*Quick Slash*, *Quick Shot*, *Crippling Shot*) dan pemeriksaan ketangkasan tanpa mengubah antrean giliran `EffectiveSPD` dan tanpa memberi extra turn.
    - **Primary Attribute:** `DEX` (`+1.5` per modifier).
    - **Secondary Attribute:** `INT` (`+0.75` per modifier).
    - **Representasi & Batas:** Integer flat (minimum `1`).

15. **QTE Stability (`QTE_STAB`)**
    - **Fungsi:** Ketahanan fokus eksekusi QTE saat karakter berada di bawah tekanan status, **tanpa menduplikasi `Timing QTE Window` (`+3% × DEX Mod`) dan tanpa mengubah nilai `R_QTE`** (`GDD Bab 12 §3.4`):
      1. **Freeze & Distortion Mitigation:** Setiap `1%` QTE Stability mengurangi penalti penyempitan window akibat `Freeze` (`-15%`) dan intensitas guncangan distorsi QTE (`ShakePx`) sebesar `1%` secara relatif (hingga reduksi penalti `50%` pada QTE Stability `50`), serta memperluas `ClearZone` bebas distorsi (`150 ms → hingga 220 ms`).
      2. **Partial-Success Tolerance Buffer:** Memperlebar batas toleransi luar `PartialSuccess` pada evaluasi 4-kategori (`QteConfig.JudgeCategory`: `PartialWindowMultiplier = 1.35 × (1 + 0.002 × QteStability)`), tanpa mengubah `W_P` (Perfect) maupun `W_G` (Good).
    - **Primary Attribute:** `DEX` (`+2.0%` per modifier).
    - **Secondary Attribute:** `WIS` (`+1.0%` per modifier).
    - **Representasi & Batas:** Poin persentase integer (`[0%, 50%]`).

---

### 5.4 Support Statistics

16. **Buff Effectiveness (`BUFF_EFF`)**
    - **Fungsi:** Memperkuat magnitude buff positif yang diberikan caster pada rekan atau diri sendiri (`AttackUp`, `DefenseUp`, `MagicDefenseUp`, dan buff positif bertanda `supportsChaScaling`):
      $$\text{Final Buff Magnitude} = \text{Base Buff Magnitude} \times (1 + \text{Buff Effectiveness Bonus})$$
    - **Primary Attribute:** `CHA` (`+4.0%` per modifier).
    - **Secondary Attribute:** `WIS` (`+1.5%` per modifier).
    - **Interaksi Sistem:** Berlaku termasuk pada modifier negatif; tidak mengubah durasi giliran, cooldown, jumlah target, atau self-steroid khusus (*Berserk*, *Draconic Overdrive*).
    - **Representasi & Batas:** Persentase float (`clamp [-20%, +40%]`).

17. **Debuff Effectiveness (`DEBUFF_EFF`)**
    - **Fungsi:** Memperkuat magnitude debuff pelemah non-kontrol yang diberikan caster kepada musuh (`AttackDown`, `DefenseDown`, `MagicDefenseDown`, `Slow`, `Fear`, `ResistanceDown`):
      $$\text{Final Debuff Magnitude} = \text{Base Debuff Magnitude} \times (1 + \text{Debuff Effectiveness Bonus})$$
    - **Primary Attribute:** `CHA` (`+3.5%` per modifier).
    - **Secondary Attribute:** `INT` (`+1.5%` per modifier).
    - **Interaksi Sistem:** Bersinergi dengan background *Charlatan* dan skill Curse Warlock / Roar Barbarian / Disarm Fighter. Tidak mengubah durasi hard Control Effect (`Stun, Sleep, Mind Control, Madness, Silence` — `GDD Bab 11 §8.1`) dan tidak menembus Mental Immunity Boss (`GDD Bab 11 §13`).
    - **Representasi & Batas:** Persentase float (`clamp [-20%, +40%]`).

18. **Support Skill Power (`SUP_PWR`)**
    - **Fungsi:** Mengukur daya dukung taktis non-healing pada skill komando, proteksi rekan, dan pemulihan moril/Poise (seperti *Guard Stance*, *Interpose*, *Shield Wall*, *Blessing*, *Barkskin*).
    - **Primary Attribute:** `CHA` (`+3.0%` per modifier).
    - **Secondary Attribute:** `WIS` (`+1.5%` per modifier).
    - **Interaksi Sistem:** Memberikan progresi terukur untuk build Commander/Support tanpa menggelembungkan `Healing Power` (`WIS`) atau base damage (`STR`/`INT`).
    - **Representasi & Batas:** Persentase / poin indeks (`clamp [-20%, +50%]`).

---

## 6. Formula and Scaling Framework

### 6.1 Model Matematika Terpadu (`FINAL (Locked — Approved)`)

1. **Hybrid Attribute Modifier:**
   $$\text{Mod}(A) = \left\lfloor \frac{\text{Score}(A) - 10}{2} \right\rfloor$$
2. **Aturan Dominasi Primary atas Secondary:**
   Untuk setiap Derived Stat dengan dua atribut ($A_1$ Primary, $A_2$ Secondary), berlaku **$w_1 > w_2 > 0$** (rasio standar $w_1 : w_2 = 2 : 1$ hingga $2.67 : 1$), sehingga investasi pada Primary Attribute selalu memberikan hasil lebih tinggi daripada Secondary Attribute.
3. **Model Flat Additive dengan Single Final `floor()`:**
   $$\text{Stat}_{\text{flat}} = \text{clamp}\left(\left\lfloor \text{Base}_{\text{class}} + \left(\text{Mod}(A_1) \times w_1 + \text{Mod}(A_2) \times w_2\right) + \text{Flat}_{\text{gear}} + \text{Flat}_{\text{passive}} + \text{Flat}_{\text{temp}} \right\rfloor, \text{Min}, \text{Max}\right)$$
   *(Kontribusi Primary dan Secondary dijumlahkan dalam desimal penuh terlebih dahulu sebelum `floor()` diterapkan satu kali di akhir.)*
4. **Model Base-Value Reference (Anti-Compounding):**
   Untuk `Max HP`, `Max MP`, `Healing Power`, dan `Shield Power`:
   $$\text{Output} = \left\lfloor \text{BaseValue} \times \max\left(0, 1 + \text{Mod}(A_1) \times w_1 + \text{Mod}(A_2) \times w_2 + \sum \text{PctAdd}_{\text{base}}\right) + \text{FlatBonus} \right\rfloor$$
   Seluruh bonus persentase (atribut, background Farmer/Acolyte, talent Durable/Healer) mengacu pada `BaseValue` secara additive—tidak pernah saling mengalikan hasil yang sudah dimodifikasi atribut.
5. **Modifier Negatif Berlaku Penuh:**
   Skor `8–9` ($\text{Mod} = -1$) atau skor ter-debuff di bawah `10` menurunkan Derived Stat terkait sesuai koefisien $w_1$ dan $w_2$ hingga batas minimum stat tersebut.

---

## 7. Charisma Integration Design

`CHA` dirancang di sekitar tiga pilar combat yang saling melengkapi (`FINAL (Locked — Approved)`):

| Pilar Charisma | Derived Stat Utama | Atribut Pendukung | Aplikasi Mekanis di Combat | Batasan Ketat (Guardrails) |
|---|---|---|---|---|
| **A. Commanding Presence** | **Buff Effectiveness** (`CHA +4.0%/mod`) & **Support Skill Power** (`CHA +3.0%/mod`) | `WIS` (`+1.5%/mod`) | Memperkuat magnitude buff positif party/target (`AttackUp`, `DefenseUp`, `MagicDefenseUp`) dan efektivitas skill komando (*Blessing*, *Rally*, *Interpose*, *Fortify*). | Tidak berlaku pada self-steroid (*Berserk*, *Draconic Overdrive*); tidak menambah durasi giliran atau jumlah target. |
| **B. Disruptive Presence** | **Debuff Effectiveness** (`CHA +3.5%/mod`) | `INT` (`+1.5%/mod`) | Memperkuat magnitude debuff pelemah musuh (`AttackDown`, `DefenseDown`, `MagicDefenseDown`, `Slow`, `Fear`, `ResistanceDown`) pada skill Warlock, Barbarian, Fighter, Rogue, dan Druid. | Tidak mengubah durasi Control Effect (`Stun, Sleep, Mind Control, Madness, Silence`) dan tidak menembus Mental Immunity Boss. |
| **C. Inspiring Presence** | **Shield Power** (`CHA +1.5%/mod` sebagai Secondary) | `WIS` (`+3.5%/mod` sebagai Primary) | Memperkuat kapasitas barrier Shield (*Divine Shield*, *Sanctuary*, *Iron Fortress*) serta bersinergi dengan talent *Inspiring Leader*. | Tidak meningkatkan `Healing Power` (tetap murni `WIS`) dan tidak memberi `Max HP` atau `Physical Defense` gratis. |

---

## 8. Calculation Examples & Character Progression Simulation

> **Disclaimer:** Nilai `SkillPower`, `Scaling`, `Base HP`, `Base MP`, `Base Healing`, `Base Shield`, dan `Base Buff/Debuff` pada bagian ini adalah **`[EXAMPLE — Angka ilustrasi semata, bukan angka resmi GDD]`**.

### 8.1 Contoh Perhitungan Lengkap Enam Ability Scores & Derived Stats

| Kasus Uji | Skor Atribut | Modifiers | Input `[EXAMPLE]` | Perhitungan Formula Final | Hasil Akhir |
|---|---|---|---|---|---|
| **1. STR (Physical & Tank/Break)** | `STR 16, VIT 14, DEX 12` | `STR +3, VIT +2, DEX +1` | `SkillPower = 40, Scaling = 1.5`<br>`BaseDef = 10, BaseBlk = 50, BaseBrk = 16, BaseAPen = 4` | • **Base Phys Dmg:** `40 + (16 × 1.5) = 64` *(Raw STR)*<br>• **Phys Def:** `10 + floor(3×1.5 + 2×0.75) = 10 + floor(6.0) = 16`<br>• **Block Power:** `50 + floor(3×2.0 + 2×1.0) = 58%`<br>• **Universal Break:** `16 + floor(3×1.5 + 1×0.75) = 16 + 5 = 21`<br>• **Armor Pen:** `4 + floor(3×1.2 + 1×0.6) = 4 + 4 = 8` | **Dmg `64`**, **DEF `16`**, **Block `58%`**, **Break `21`**, **APen `8`** |
| **2. DEX (Precision, Tempo & QTE)** | `DEX 16, STR 14, INT 12, WIS 12` | `DEX +3, STR +2, INT +1, WIS +1` | `BaseEva = 5%, BaseCrit = 5%, BaseCnt = 10, BaseSpd = 10, BaseStab = 0` | • **Ranged Acc:** `+6%` (`2% × 3`)<br>• **Evasion:** `5% + 6% = 11%`<br>• **QTE Window:** `+9%` (`3% × 3`)<br>• **QTE Stability:** `0 + floor(3×2.0 + 1×1.0) = 7%`<br>• **Phys Crit Chance:** `5% + floor(3×1.5 + 2×0.5) = 5% + 5% = 10%`<br>• **Counter Power:** `10 + floor(3×2.5 + 2×1.5) = 10 + 10 = 20%`<br>• **Initiative:** `10 + floor(3×1.0 + 1×0.5) = 13` | **RngAcc `+6%`**, **Eva `11%`**, **Window `+9%`**, **Stab `7%`**, **Crit `10%`**, **Counter `20%`**, **SPD `13`** |
| **3. VIT (HP, DR, Status & Stagger Res)** | `VIT 16, STR 14, WIS 14` | `VIT +3, STR +2, WIS +2` | `BaseHP = 1,000, BaseDR = 0%, BaseStatRes = 5%, BaseStgRes = 15%` | • **Max HP:** `floor(1,000 × (1 + 0.04×3)) = 1,120`<br>• **Damage Reduction:** `0% + (3×1.0% + 2×0.5%) = 4.0%`<br>• **Status Resistance:** `5% + (3×2.0% + 2×1.0%) = 13.0%`<br>• **Stagger Resistance:** `15% + floor(3×2.0 + 2×1.0) = 23%` | **Max HP `1,120`**, **DR `4.0%`**, **Status Res `13%`**, **Stagger Res `23%`** |
| **4. INT (Elemental, MP, MDEF & MPen)** | `INT 16, WIS 14` | `INT +3, WIS +2` | `SkillPower = 50, Scaling = 1.5, BaseMP = 200, BaseMDef = 8, BaseMPen = 4` | • **Base Elem Dmg:** `50 + (16 × 1.5) = 74` *(Raw INT)*<br>• **Max MP:** `floor(200 × (1 + 0.04×3)) = 224`<br>• **Magic Defense:** `8 + floor(3×1.5 + 2×0.75) = 8 + 6 = 14`<br>• **MagicPen:** `4 + floor(3×1.2 + 2×0.6) = 4 + 4 = 8` | **Elem Dmg `74`**, **Max MP `224`**, **MDEF `14`**, **MPen `8`** |
| **5. WIS (Healing & Shield Power)** | `WIS 16, CHA 14` | `WIS +3, CHA +2` | `BaseHealing = 200, BaseShield = 150` | • **Healing Output:** `floor(200 × (1 + 0.04×3)) = 224`<br>• **Shield Power Bonus:** `(3×3.5%) + (2×1.5%) = +13.5%`<br>• **Shield Output:** `floor(150 × 1.135) = 170` | **Heal `224`**, **Shield Bonus `+13.5%`** (**Shield `170`**) |
| **6. CHA (Buff, Debuff & Support Power)** | `CHA 16, WIS 14, INT 12` | `CHA +3, WIS +2, INT +1` | `BaseAttackBuff = +10%, BaseAttackDebuff = -20%, BaseSup = 10` | • **Buff Eff Bonus:** `(3×4.0%) + (2×1.5%) = +15.0%` → `+10% × 1.15 = +11.5%`<br>• **Debuff Eff Bonus:** `(3×3.5%) + (1×1.5%) = +12.0%` → `-20% × 1.12 = -22.4%`<br>• **Support Skill Power:** `10 + floor(3×3.0 + 2×1.5) = 22%` | **Final Buff `+11.5%`**, **Final Debuff `-22.4%`**, **Support Power `22%`** |

---

### 8.2 Simulasi Progresi Karakter Lengkap (Liichtian Cleric Level 1 → 15 → 30)

- **Karakter:** **Liichtian Cleric** (`+2 WIS` racial bonus)
- **Level 1 (27-pt Point Buy + Racial):** `STR 10 (0), DEX 12 (+1), VIT 12 (+1), INT 14 (+2), WIS 17 (+3), CHA 9 (-1)`
- **Level 15 (8 AP — Cap Aktif Build Saat Ini):** `STR 10 (0), DEX 12 (+1), VIT 14 (+2), INT 16 (+3), WIS 20 (+5), CHA 10 (0)`
- **Level 30 (18 AP — Target Ekspansi Cap 30):** `STR 12 (+1), DEX 14 (+2), VIT 14 (+2), INT 18 (+4), WIS 20 (+5), CHA 14 (+2)`

Input ilustrasi `[EXAMPLE]`: Base HP (`300 / 720 / 1,300`), Base MP (`80 / 160 / 260`), Base DEF (`8`), Base MDEF (`10`), Base Block (`55`), Base Crit (`5%`), Base Evasion (`5%`), Base Counter (`10`), Base Break (`16`), Base Status Res (`10%`), Base Stagger Res (`18%`), Base SPD (`10`), Base Support (`10`), Mace Strike (`30 + STR × 1.2`), Holy Smite (`50 + INT × 1.5`), Divine Heal (`Base 200`), Divine Shield (`Base 150`), Blessing (`Base +10%`), Judgment Debuff (`Base -15%`).

| Derived Stat | Governing Attributes (`Primary / Secondary`) | Level 1 | Level 15 (8 AP — Cap Aktif) | Level 30 (18 AP — Target) |
|---|---|---:|---:|---:|
| **Max HP** | `VIT` (`+4%/mod`) | `312` | `777` | `1,404` |
| **Max MP** | `INT` (`+4%/mod`) | `86` | `179` | `301` |
| **Base Physical Damage** | Raw `STR` | `42` | `42` | `44` |
| **Base Elemental Damage** | Raw `INT` | `71` | `74` | `77` |
| **Healing Output** | `WIS` (`+4%/mod`) | `224` | `240` | `240` |
| **Shield Power Bonus → Output** | `WIS (+3.5%)` / `CHA (+1.5%)` | `+9.0%` → **`163`** | `+17.5%` → **`176`** | `+20.5%` → **`180`** |
| **Damage Reduction** | `VIT (+1.0%)` / `STR (+0.5%)` | **`1.0%`** | **`2.0%`** | **`2.5%`** |
| **Status Resistance** | `VIT (+2.0%)` / `WIS (+1.0%)` | **`15.0%`** | **`19.0%`** | **`19.0%`** |
| **Stagger Resistance** | `VIT (+2.0%)` / `STR (+1.0%)` | **`20%`** | **`22%`** | **`23%`** |
| **Physical Defense** | `STR (+1.5)` / `VIT (+0.75)` | **`8`** | **`9`** | **`11`** |
| **Magic Defense** | `INT (+1.5)` / `WIS (+0.75)` | **`15`** | **`18`** | **`19`** |
| **Block Power** | `STR (+2.0)` / `VIT (+1.0)` | **`56%`** | **`57%`** | **`59%`** |
| **Physical Critical Chance** | `DEX (+1.5%)` / `STR (+0.5%)` | **`6%`** | **`6%`** | **`8%`** |
| **Armor Penetration** | `STR (+1.2)` / `DEX (+0.6)` | **`0`** | **`0`** | **`2`** |
| **Magic Penetration** | `INT (+1.2)` / `WIS (+0.6)` | **`4`** | **`6`** | **`7`** |
| **Counter Power** | `DEX (+2.5)` / `STR (+1.5)` | **`12%`** | **`12%`** | **`16%`** |
| **Universal Break Power** | `STR (+1.5)` / `DEX (+0.75)` | **`16`** | **`16`** | **`19`** |
| **Initiative (`SPD`)** | `DEX (+1.0)` / `INT (+0.5)` | **`12`** | **`12`** | **`14`** |
| **Ranged Accuracy Bonus** | `DEX (+2.0%)` | **`+2%`** | **`+2%`** | **`+4%`** |
| **Evasion** | `DEX (+2.0%)` | **`7%`** | **`7%`** | **`9%`** |
| **Timing QTE Window Bonus** | `DEX (+3.0%)` | **`+3%`** | **`+3%`** | **`+6%`** |
| **QTE Stability** | `DEX (+2.0%)` / `WIS (+1.0%)` | **`5%`** | **`7%`** | **`9%`** |
| **Buff Effectiveness → Final Buff** | `CHA (+4.0%)` / `WIS (+1.5%)` | `+0.5%` → **`+10.05%`** | `+7.5%` → **`+10.75%`** | `+15.5%` → **`+11.55%`** |
| **Debuff Effectiveness → Final Debuff** | `CHA (+3.5%)` / `INT (+1.5%)` | `-0.5%` → **`-14.93%`** | `+4.5%` → **`-15.68%`** | `+13.0%` → **`-16.95%`** |
| **Support Skill Power** | `CHA (+3.0%)` / `WIS (+1.5%)` | **`11%`** | **`17%`** | **`23%`** |

---

## 9. Class and Build Implications

| Kombinasi Build | Identitas Taktis | Keunggulan Derived Stats Utama | Contoh Class |
|---|---|---|---|
| **STR / VIT** | *Physical Tank & Heavy Frontliner* | `Max HP`, `Physical Defense`, `Block Power`, `Stagger Resistance`, `Damage Reduction`, `Physical Damage`, `Universal Break Power`. | Fighter, Barbarian |
| **DEX / STR** | *Counter Fighter & Physical Duelist* | `Counter Power`, `Physical Critical Chance`, `Armor Penetration`, `Universal Break Power`, `Evasion`, `Timing QTE Window`. | Fighter, Martial Artist, Rogue |
| **DEX / INT** | *Agile Ranged & Precision Combatant* | `Initiative`, `Action Speed`, `Ranged Accuracy`, `Max MP`, `Magic Defense`, `Timing QTE Window`. | Ranger, Rogue |
| **INT / WIS** | *Elemental Mage & Arcane Specialist* | `Elemental Damage`, `Max MP`, `Magic Penetration`, `Magic Defense`, `QTE Stability`, `Status Resistance`. | Wizard, Druid, Cleric |
| **WIS / VIT** | *Defensive Support & Healer* | `Healing Power`, `Max HP`, `Status Resistance`, `Shield Power`, `Stagger Resistance`, `QTE Stability`. | Cleric, Druid |
| **CHA / WIS** | *Support Leader & Protective Commander* | `Buff Effectiveness`, `Support Skill Power`, `Shield Power`, `Healing Power`, `QTE Stability`. | Cleric, Druid |
| **CHA / INT** | *Debuffer & Tactical Controller* | `Debuff Effectiveness`, `Elemental Damage`, `Max MP`, `Magic Defense`, `Magic Penetration`. | Warlock, Wizard, Druid |
| **STR / CHA** | *Commanding Physical Frontliner* | `Physical Damage`, `Block Power`, `Physical Defense`, `Buff Effectiveness`, `Debuff Effectiveness`, `Support Skill Power`. | Fighter, Barbarian |

---

## 10. Balance Risks and Safeguards (`FINAL`)

1. **Pencegahan Inflasi DEX:** Meskipun `DEX` memengaruhi akurasi ranged, evasion, QTE window, initiative, crit, counter, dan QTE stability, koefisien `Crit` dibatasi pada `+1.5%/mod`, `Initiative` pada `+1.0/mod` (tanpa extra turn), `Ranged Accuracy` hanya untuk serangan Ranged, dan `QTE Window` tunduk pada shared cap `+25%`.
2. **Pencegahan Tank Invulnerability (`STR + VIT`):** `Damage Reduction` dari atribut (`+1.0% VIT + 0.5% STR`, maks `+7.5%` pada 20/20) masuk ke dalam shared cap **`25%`**, sedangkan `Block Power` Perfect non-Guard dibatasi pada **`75%`** (`50%` pada Heavy). Serangan Elemental dan Almighty mengabaikan `Physical Defense`.
3. **Pencegahan Dominasi CHA atas WIS/INT:** `CHA` tidak memberikan `Healing Power` (murni `WIS`) dan tidak meningkatkan `Elemental Damage` atau `Max MP` (murni `INT`). `Buff Effectiveness` dan `Debuff Effectiveness` di-clamp pada **`[-20%, +40%]`**.
4. **Kalibrasi Baseline Class vs. Dungeon 1 Boss (*Zoroa*):** Base stat pada `ClassDef` dikalibrasi sehingga total stat hero Level 1–15 (Class Base + kontribusi Primary & Secondary Attribute) mempertahankan kurva kesulitan *Relentless Tactical Survival* pada `BalanceSimulation`.

---

## 11. Implementation Architecture (Unity C#)

- **`Assets/Scripts/Data/StatsSystem.cs`:**
  - `AttributeSystemConfig`: menyimpan seluruh koefisien Primary (`w_1`) dan Secondary (`w_2`) serta batas clamp.
  - `DerivedStats`: menyimpan `shieldPowerBonus`, `damageReductionBonus`, `buffEffectivenessBonus`, `debuffEffectivenessBonus`, `supportSkillPowerBonus`, `rangedAccuracyBonus`, serta 21 nilai integer `DerivedStatId`.
  - `StatCalculator.Calculate` & `StatCalculator.Explain`: menghitung kontribusi Primary + Secondary Attribute dengan `FloorInt` tunggal di akhir dan non-compounding Base Value Reference.
- **`Assets/Scripts/Combat/Battle.cs`, `BattleAction.cs`, `Combatant.cs`, `QteConfig.cs`:**
  - `Battle.SupportScaled`: menggunakan `Buff Effectiveness` (`CHA` Primary + `WIS` Secondary).
  - `Battle.DebuffScaled`: menggunakan `Debuff Effectiveness` (`CHA` Primary + `INT` Secondary) pada debuff non-kontrol (`AttackDown`, `DefenseDown`, `MagicDefenseDown`, `Slow`, `Fear`, `ResistanceDown`).
  - `Shield` grants: dikalikan `(1 + ShieldPowerBonus)` (`WIS` Primary + `CHA` Secondary).
  - `Damage Reduction`: menerapkan `Derived.damageReductionBonus` (`VIT` Primary + `STR` Secondary) di dalam shared cap `25%`.
  - `QteConfig`: menghubungkan `QTE Stability` (`DEX` Primary + `WIS` Secondary) ke mitigasi penalti window `Freeze` dan batas `PartialWindowMultiplier`.
