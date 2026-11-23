# FELNA — ATTRIBUTE PROGRESSION & DERIVED STATS SYSTEM SPECIFICATION

**Document ID:** `FELNA-SYS-ATTR-001`  
**Version:** `1.0 — FINAL`  
**Date:** `2026-11-23`  
**Status:** `FINAL` (Seluruh elemen `CONFIRMED`, `APPROVED`, `PROPOSED`, dan `TBD` telah difinalisasi)  
**Dokumen Terkait:** `GDD_Terbaru.md` (Bab 5, 6, 7, 8, 9, 10, 11, 12, 13, 21), `Felna_QTE_Variation_Spec_v1.md`, `Felna_Items_and_Equipment_Catalogue_v2_Cursed_Equipment.xlsx`

> **Catatan Status Provenance:** Seluruh aturan dan parameter dalam dokumen ini berstatus **`FINAL`**. Tag di dalam tanda kurung menunjukkan asal keputusan untuk kebutuhan audit lintas dokumen:
> - `FINAL (Confirmed — GDD)` — sudah ditetapkan sebelumnya di GDD FELNA.
> - `FINAL (Locked — Approved)` — keputusan desain baru yang telah disetujui dan dikunci.
> - `FINAL (Locked — Proposal)` — usulan balancing yang kini dikunci sebagai baseline final.
> - `FINAL (Locked — Resolved TBD)` — item yang sebelumnya TBD dan kini diputuskan final.
>
> **Catatan Level Cap Aktif:** Target desain jangka panjang adalah **Character Level Cap 30** (9 milestone AP = 18 AP). Untuk fase build saat ini, **Character Level Cap aktif dipertahankan di Level 15** (4 milestone aktif: Level 4, 7, 10, 13 = 8 AP). Jadwal 9 milestone hingga Level 28 sudah dikonfigurasi penuh sehingga aktif otomatis ketika Level Cap dinaikkan ke 30.
>
> **Catatan Workbook Advancement:** Workbook *Advancement* (`.xlsx`) belum tersedia di repositori `Assets/Docs/`; verifikasi silang pada revisi ini menggunakan `GDD_Terbaru.md` v1.6, seluruh 7 spreadsheet di `Assets/Docs/`, dan implementasi Unity C#.

---

## 1. Overview & Design Goals

### 1.1 Ringkasan Sistem

**Attribute Progression & Derived Stats System** mengatur bagaimana enam Primary Ability Scores (`STR`, `DEX`, `VIT`, `INT`, `WIS`, `CHA`) diperoleh pada Character Creation, berkembang melalui Attribute Points (AP) di tiap Skill Tier Unlock milestone, dikonversi melalui **Hybrid Ability Score System** (*Raw Score* dan *Attribute Modifier*), serta dipetakan ke *Derived Stats* secara terpisah dan deterministik.

### 1.2 Design Goals

| # | Design Goal | Rasional Desain | Status |
|---|---|---|---|
| 1 | **Kebebasan Build (Free Allocation)** | Point Buy di Level 1 + Free Allocation AP di tiap milestone memberi pemain kendali penuh membangun karakter spesialis maupun hibrida tanpa kewajiban menyebar AP. | `FINAL (Locked — Approved)` |
| 2 | **Pemisahan Eksekusi vs Kekuatan Karakter** | QTE mengukur eksekusi pemain; Ability Scores mengukur kekuatan karakter. Hanya `DEX` yang memengaruhi lebar timing window QTE dalam shared cap `+25%`. | `FINAL (Confirmed — GDD)` |
| 3 | **Murni 6 Ability Scores Tanpa Coupling Liar** | `Block Power` dan `Counter Power` dipisahkan penuh dari `STR` dan `DEX`; tidak ada atribut yang secara diam-diam memberi bonus Speed, Crit, Break Power universal, DR, Shield, atau Status Resistance. | `FINAL (Locked — Approved)` |
| 4 | **Anti-Compounding & Anti-Double Scaling** | Semua persentase Derived Stat mengacu pada *Base Value*; Raw Score dan Modifier tidak pernah dipakai bersamaan pada efek yang sama kecuali dinyatakan eksplisit oleh skill. | `FINAL (Locked — Approved)` |
| 5 | **Pemisahan Efek Support** | Healing Power (`WIS`), Buff Effectiveness (`CHA`), Shield Power (terpisah), Damage Reduction (terpisah), dan Status Effect Resistance (shared combat system) dijaga independen. | `FINAL (Confirmed — GDD)` |
| 6 | **Data-Driven & Konsisten `floor()`** | Semua konstanta disimpan sebagai data konfigurasi dan memakai pembulatan `floor()` yang konsisten dengan Damage, Turn Order, dan Enemy Scaling GDD. | `FINAL (Confirmed — GDD)` |

---

## 2. Attribute Progression Rules

### 2.1 Character Creation Baseline (Level 1)

| Parameter | Aturan Final | Status |
|---|---|---|
| Metode Alokasi Awal | **Point Buy** | `FINAL (Confirmed — GDD Bab 5 §3)` |
| Point Buy Budget | **27 Points** | `FINAL (Locked — Resolved TBD)` |
| Starting Base Score | **8** pada seluruh 6 Ability Scores | `FINAL (Locked — Resolved TBD)` |
| Rentang Point Buy (Pra-Racial) | **8 – 15** | `FINAL (Confirmed — GDD Bab 6 §6.2)` |
| Tabel Biaya Kumulatif Point Buy | `8→0, 9→1, 10→2, 11→3, 12→4, 13→5, 14→7, 15→9` | `FINAL (Locked — Resolved TBD)` |
| Racial Ability Bonus | Diterapkan **setelah** Point Buy; maks **17** pada Level 1 | `FINAL (Confirmed — GDD Bab 6 §6.1–6.2)` |
| AP di Level 1 | **0 AP** (pemain sudah mengalokasikan skor melalui Point Buy) | `FINAL (Locked — Approved)` |

### 2.2 Attribute Points (AP) Milestone Schedule

Pemain memperoleh **+2 Attribute Points (AP)** pada setiap **Skill Tier Unlock milestone** (`FINAL (Locked — Approved)`):

| Milestone # | Character Level | AP Diperoleh | Kumulatif AP | Fase Level Cap |
|---:|---:|---:|---:|---|
| — | Level 1 | 0 AP (Point Buy) | 0 AP | Aktif (Cap 15) |
| 1 | Level 4 | +2 AP | 2 AP | Aktif (Cap 15) |
| 2 | Level 7 | +2 AP | 4 AP | Aktif (Cap 15) |
| 3 | Level 10 | +2 AP | 6 AP | Aktif (Cap 15) |
| 4 | Level 13 | +2 AP | 8 AP | Aktif (Cap 15) |
| 5 | Level 16 | +2 AP | 10 AP | Siap untuk Cap 30 |
| 6 | Level 19 | +2 AP | 12 AP | Siap untuk Cap 30 |
| 7 | Level 22 | +2 AP | 14 AP | Siap untuk Cap 30 |
| 8 | Level 25 | +2 AP | 16 AP | Siap untuk Cap 30 |
| 9 | Level 28 | +2 AP | 18 AP | Siap untuk Cap 30 |
| **Total s.d. Lv 15 (Aktif)** | **4 Milestones** | **+2 AP / milestone** | **8 AP** | **Build Aktif** |
| **Total s.d. Lv 30 (Target)** | **9 Milestones** | **+2 AP / milestone** | **18 AP** | **Target Ekspansi Cap 30** |

### 2.3 Allocation Rules (Free Allocation)

| Aturan | Spesifikasi Final | Status |
|---|---|---|
| Biaya Kenaikan | **1 AP meningkatkan satu Ability Score sebesar +1**. | `FINAL (Locked — Approved)` |
| Alokasi Ganda | Pemain **boleh** mengalokasikan kedua AP pada milestone yang sama ke Ability Score yang sama. | `FINAL (Locked — Approved)` |
| Penyimpanan AP (Banking) | Pemain **boleh** menyimpan AP untuk digunakan di kemudian hari. | `FINAL (Locked — Approved)` |
| Bebas Distribusi | Tidak ada kewajiban menyebarkan AP ke beberapa Ability Scores. | `FINAL (Locked — Approved)` |
| Proteksi Cap | AP yang tidak dapat digunakan karena Attribute Cap (`20`) **tidak boleh hangus** dan tetap berada pada pool `unspentAttributePoints`. | `FINAL (Locked — Approved)` |
| Respec | Respec mengembalikan seluruh AP yang telah dialokasikan kembali ke `unspentAttributePoints` dan menghitung ulang seluruh Derived Stats dari sumber dasar secara bersih. | `FINAL (Locked — Resolved TBD)` |

---

## 3. Attribute Modifier System

### 3.1 Formula Modifier

$$\text{Modifier} = \left\lfloor \frac{\text{Ability Score} - 10}{2} \right\rfloor$$

Pembulatan wajib memakai mathematical **`floor()`** menuju $-\infty$ (`FINAL (Confirmed — GDD & Approved)`).

### 3.2 Tabel Referensi Ability Score → Modifier

| Ability Score | Modifier | Keterangan |
|---:|---:|---|
| 6–7 *(debuff)* | `-2` | Penalti ekstrem akibat debuff/curse |
| 8–9 | `-1` | Minimum Point Buy (`8`) |
| 10–11 | `0` | Baseline netral |
| 12–13 | `+1` | Di atas rata-rata |
| 14–15 | `+2` | Maksimum Point Buy pra-racial (`15`) |
| 16–17 | `+3` | Maksimum Character Creation pasca-racial (`17`) |
| 18–19 | `+4` | Master |
| 20 | `+5` | Normal Progression Maximum (`20`) |
| 22–23 *(temp buff)* | `+6` | Hanya jika temporary buff eksplisit mengizinkan `> 20` |

### 3.3 Hybrid System Rules

| # | Aturan | Status |
|---|---|---|
| 1 | **Raw Ability Score** digunakan untuk **skill damage scaling** (`STR` untuk Physical, `INT` untuk Elemental/Almighty) apabila skill secara eksplisit menyatakan atribut yang digunakan. | `FINAL (Confirmed — GDD Bab 11 §2.1 & Approved)` |
| 2 | **Attribute Modifier** digunakan untuk menghitung seluruh **Derived Stats** berbasis atribut. | `FINAL (Locked — Approved)` |
| 3 | **Modifier negatif tetap berlaku penuh** (mengurangi Max HP, Max MP, Ranged Accuracy, Evasion, QTE Window, Healing Power, Buff Effectiveness, dan Social Checks sesuai formula). | `FINAL (Locked — Approved)` |
| 4 | **Anti-Double Scaling:** Dilarang menggunakan Raw Score dan Attribute Modifier secara bersamaan pada efek yang sama, kecuali skill secara eksplisit menyatakannya. | `FINAL (Locked — Approved)` |

---

## 4. Six Ability Scores

### 4.1 STR — Strength
- **Primary Function:** Physical Damage scaling.
- **Formula (`FINAL (Confirmed — GDD Bab 11 §2.1 & Approved)`):**
  $$\text{Base Physical Damage} = \text{SkillPower} + (\text{STR}_{\text{raw}} \times \text{Scaling})$$
- **Aturan Final:**
  - Menggunakan **Raw STR**.
  - Hanya skill yang menyatakan STR scaling yang menggunakan formula ini.
  - STR tidak otomatis meningkatkan semua Physical Damage.
  - STR **tidak** memberikan universal Break Power bonus.
  - STR **tidak** lagi memengaruhi `Block Power` maupun `Counter Power` (dipisah penuh agar STR murni Physical Damage scaling).
  - Hindari double scaling.

### 4.2 DEX — Dexterity
- **Primary Functions:** Ranged Accuracy, Evasion, Timing-based QTE Window.
- **Formulas (`FINAL (Locked — Approved & Proposal)`):**
  - $\text{Ranged Accuracy Bonus} = 2\% \times \text{DEX Modifier}$
  - $\text{Evasion Bonus} = 2\% \times \text{DEX Modifier}$
  - $\text{QTE Window Bonus} = 3\% \times \text{DEX Modifier}$
- **Aturan Final:**
  - QTE Window mengikuti shared additive cap **`+25%`** dan batas bawah total **`-15%`** dari atribut, dengan batas bawah absolut **`W_P >= 66 ms`** (GDD Bab 12 §3.4 & §8.2). Berlaku hanya untuk QTE berbasis timing (tidak untuk Typing QTE: incantation & Ward).
  - DEX **tidak** mengubah QTE grade, damage multiplier, Break multiplier, `T_lead`, atau hasil eksekusi pemain.
  - DEX **tidak** otomatis meningkatkan Speed (`SPD` / Initiative), Critical Chance, Ranged Power, atau `Counter Power`, dan tidak memberikan extra turns.
  - Ranged Accuracy hanya berlaku pada Ranged Basic Attacks dan Ranged Active Skills (tidak otomatis pada Melee atau Elemental/Magic).
  - Accuracy dan Evasion tetap mengikuti sistem combat masing-masing (`Hit Chance ∈ [5%, 95%]`, `Evasion ∈ [0%, 75%]`).

### 4.3 VIT — Vitality
- **Primary Function:** Max HP.
- **Formula (`FINAL (Locked — Approved)`):**
  $$\text{Max HP} = \left\lfloor \text{Base HP} \times (1 + 0.04 \times \text{VIT Modifier}) \right\rfloor$$
- **Aturan Final:**
  - Setiap `+1` VIT Modifier memberikan **`+4%` Max HP** dari `Base HP`.
  - Bonus dihitung dari **Base HP** untuk menghindari compounding.
  - VIT **tidak** otomatis memberikan Damage Reduction, Physical Defense, Shield, Stagger Resistance, QTE Stability, atau Status Effect Resistance.

### 4.4 INT — Intelligence
- **Primary Functions:** Elemental Damage scaling & Max MP.
- **Formulas (`FINAL (Confirmed — GDD Bab 11 §2.1 & Approved)`):**
  - $\text{Base Elemental Damage} = \text{SkillPower} + (\text{INT}_{\text{raw}} \times \text{Scaling})$
  - $\text{Max MP} = \left\lfloor \text{Base MP} \times (1 + 0.04 \times \text{INT Modifier}) \right\rfloor$
- **Aturan Final:**
  - Elemental Damage (dan Almighty Damage jika dinyatakan skill) menggunakan **Raw INT**.
  - Max MP menggunakan **INT Modifier** (`+4%` dari `Base MP` per modifier).
  - INT tidak otomatis meningkatkan seluruh efek magis, Break Power, atau penetrasi.
  - Gunakan 3 damage category yang sudah ada (**Physical**, **Elemental**, **Almighty**). Jangan membuat *Magical Damage* sebagai damage category baru.

### 4.5 WIS — Wisdom
- **Primary Function:** Healing Power.
- **Formula (`FINAL (Locked — Approved)`):**
  $$\text{Healing Output} = \left\lfloor \text{Base Healing} \times (1 + 0.04 \times \text{WIS Modifier}) \right\rfloor$$
- **Aturan Final:**
  - Setiap `+1` WIS Modifier memberikan **`+4%` Healing Power**.
  - Berlaku untuk skill yang menggunakan Healing Power, termasuk Healing over Time (*Regeneration* / *Regrowth*) jika skill tersebut mendukung scaling (`usesHealingPower = true`).
  - WIS **tidak** otomatis meningkatkan Shield Power, Damage Reduction, Max MP, Magic Defense, atau Status Effect Resistance.
  - Healing berbasis persentase Max HP tetap mengikuti aturan skill terkait dan **tidak** menerima WIS scaling kecuali skill secara eksplisit menyatakan bahwa Healing Power diterapkan.

### 4.6 CHA — Charisma
- **Primary Functions:** Buff Effectiveness, Social Interaction, Class-specific support mechanics.
- **Formulas (`FINAL (Locked — Approved)`):**
  - $\text{Buff Effectiveness Bonus} = 4\% \times \text{CHA Modifier}$
  - $\text{Final Buff Magnitude} = \text{Base Buff Magnitude} \times (1 + \text{Buff Effectiveness Bonus})$
- **Aturan Final:**
  - CHA meningkatkan magnitude buff positif yang secara eksplisit mendukung CHA scaling (`supportsChaScaling = true`).
  - CHA **tidak** otomatis meningkatkan seluruh buff.
  - CHA **tidak** meningkatkan Healing Power, Shield Power, atau Damage Reduction secara otomatis.
  - CHA **tidak** otomatis meningkatkan buff duration, status chance, cooldown, atau target count.
  - Safety ceiling untuk temporary buff penembus cap CHA 20: `Buff Effectiveness Bonus` di-clamp pada **`[-20%, +40%]`** (`FINAL (Locked — Resolved TBD)`).
  - Social checks menggunakan formula event terpisah (`FINAL (Locked — Resolved TBD)`):
    $$\text{Social Check Value} = 10 + \text{CHA Modifier} + \text{Social Bonuses} \quad (\text{vs Event DC } 10 / 12 / 14 / 16)$$

---

## 5. Derived Stats Formulas & Rounding Rules

### 5.1 Master Derived Stats Table

| Derived Stat | Governing Attribute | Formula Final | Clamp / Batas | Rounding | Status |
|---|---|---|---|---|---|
| **Base Physical Damage** | Raw `STR` | `SkillPower + (STR × Scaling)` | Min `0` (akhir pipeline) | `floor()` sekali per hit | `FINAL (Confirmed — GDD Bab 11)` |
| **Base Elemental Damage** | Raw `INT` | `SkillPower + (INT × Scaling)` | Min `0` (akhir pipeline) | `floor()` sekali per hit | `FINAL (Confirmed — GDD Bab 11)` |
| **Base Almighty Damage** | Raw `INT` (jika dinyatakan) | `SkillPower + (INT × Scaling)` | Min `0` (akhir pipeline) | `floor()` sekali per hit | `FINAL (Confirmed — GDD Bab 11)` |
| **Max HP** | `VIT Mod` | `floor(BaseHP × (1 + 0.04 × VITMod + OtherBaseHpPct) + FlatHp)` | Min `1` | `floor()` sekali di akhir | `FINAL (Locked — Approved)` |
| **Max MP** | `INT Mod` | `floor(BaseMP × (1 + 0.04 × INTMod + OtherBaseMpPct) + FlatMp)` | Min `0` | `floor()` sekali di akhir | `FINAL (Locked — Approved)` |
| **Ranged Accuracy Bonus** | `DEX Mod` | `2% × DEXMod` | Khusus Ranged | Float penuh | `FINAL (Locked — Proposal)` |
| **Ranged Hit Chance** | `DEX Mod` + Acc − Eva | `clamp(85% + AttackerRangedAcc - TargetEvasion, 5%, 95%)` | `[5%, 95%]` | Float penuh | `FINAL (Locked — Resolved TBD)` |
| **Evasion** | `DEX Mod` | `clamp(BaseEvasion + (2% × DEXMod) + OtherEvaPct, 0%, 75%)` | `[0%, 75%]` | Float penuh | `FINAL (Locked — Proposal)` |
| **Timing QTE Window Bonus** | `DEX Mod` | `clamp(max(-15%, 3% × DEXMod) + OtherWindowPct, -15%, +25%)` | `[-15%, +25%]`, `W_P ∈ [66, 125] ms` | Float ms penuh | `FINAL (Confirmed — GDD Bab 12)` |
| **Healing Output** | `WIS Mod` | `floor(BaseHealing × (1 + 0.04 × WISMod + OtherHealPct) × (1 + HealRecvPct))` | Min `0` | `floor()` sekali per cast/tick | `FINAL (Locked — Approved)` |
| **Buff Effectiveness Bonus** | `CHA Mod` | `clamp(4% × CHAMod, -20%, +40%)` | `[-20%, +40%]` | Float penuh | `FINAL (Locked — Approved)` |
| **Final Buff Magnitude** | `CHA Mod` (opt-in) | `BaseBuffMagnitude × (1 + BuffEffectivenessBonus)` | Sesuai cap buff | Float (1 desimal UI) | `FINAL (Locked — Approved)` |
| **Social Check Value** | `CHA Mod` | `10 + CHAMod + SocialBonuses` vs Event DC | — | Integer | `FINAL (Locked — Resolved TBD)` |

### 5.2 Aturan Rounding & Non-Compounding

1. **Attribute Modifier:** selalu menggunakan `floor((Ability Score - 10) / 2)`.
2. **Base Value Reference (Non-Compounding):** bonus persentase dari Attribute Modifier (`VIT`, `INT`, `WIS`) dan sumber persentase Base lainnya (seperti Farmer Max HP%, Durable Max HP%, Acolyte Healing%) dijumlahkan secara **additive terhadap Base Value**, bukan dikalikan dari hasil yang sudah menerima modifier atribut.
3. **Unified `floor()` pada Hasil Akhir Integer:** seluruh nilai akhir integer (**Max HP**, **Max MP**, **Damage per hit**, **Healing per cast/tick**, **Effective SPD**) memakai **`floor()` satu kali di akhir kalkulasi** (dengan proteksi epsilon `1e-4` terhadap error representasi floating-point biner), konsisten dengan GDD Bab 10 §7.2, Bab 11 §7, dan Bab 12 §5.2.
4. **Presisi Persentase:** `Final Buff Magnitude` (misal `+11.2%`), `Ranged Accuracy Bonus`, `Evasion`, dan `QTE Window Bonus` dipertahankan dalam presisi floating-point penuh (ditampilkan hingga 1 desimal bila pecahan) dan tidak di-floor ke integer sebelum masuk ke resolusi damage/HP/MP/healing.
5. **Aturan Terkunci Tidak Diubah:** QTE grade/multiplier, Status Resistance (additive cap `75%`), Shield Power (independen), dan Damage Reduction (`All Damage Taken` multiplicative) tetap mengikuti spesifikasi GDD masing-masing.
6. **Data-Driven:** seluruh konstanta disimpan pada konfigurasi `AttributeSystemConfig` tanpa hard-code di logika combat.

---

## 6. Support Effect Separation

| System | Governing Attribute | Aturan Pemisahan | Status |
|---|---|---|---|
| **Physical Damage** | Raw `STR` | Hanya skill dengan STR scaling eksplisit. | `FINAL (Confirmed — GDD)` |
| **Elemental Damage** | Raw `INT` | 8 elemen resmi; tanpa *Magical Damage*. | `FINAL (Confirmed — GDD)` |
| **Max HP** | `VIT` Modifier | `+4%` Base HP per modifier. | `FINAL (Locked — Approved)` |
| **Max MP** | `INT` Modifier | `+4%` Base MP per modifier. | `FINAL (Locked — Approved)` |
| **Ranged Accuracy** | `DEX` Modifier | `+2%` per modifier; khusus Ranged. | `FINAL (Locked — Proposal)` |
| **Evasion** | `DEX` Modifier | `+2%` per modifier; cap `75%`. | `FINAL (Locked — Proposal)` |
| **QTE Window** | `DEX` Modifier | `+3%` per modifier; timing QTE only; shared cap `+25%` / floor `-15%`. | `FINAL (Confirmed — GDD)` |
| **Healing Power** | `WIS` Modifier | `+4%` Base Healing per modifier. | `FINAL (Locked — Approved)` |
| **Buff Effectiveness** | `CHA` Modifier | `+4%` Buff Magnitude per modifier; opt-in buff positif. | `FINAL (Locked — Approved)` |
| **Shield Power** | **Separate modifier** | Terpisah total dari `WIS`, `CHA`, dan `VIT` (sesuai desain Cleric Bab 8 §8.1). | `FINAL (Confirmed — GDD)` |
| **Damage Reduction** | **Separate modifier** | Multiplicative `All Damage Taken −x%`; terpisah dari `VIT`, `WIS`, dan `CHA`. | `FINAL (Confirmed — GDD)` |
| **Status Effect Resistance** | **Shared combat system** | Universal Status Resistance additive, cap `75%`; terpisah dari `VIT` dan `WIS`. | `FINAL (Confirmed — GDD)` |
| **Block Power & Counter Power** | **Separate (Class/Gear/Passive)** | Dipisah penuh dari `STR` dan `DEX`. | `FINAL (Locked — Approved)` |

---

## 7. Attribute Cap Rules

| Cap | Nilai Final | Aturan | Status |
|---|---:|---|---|
| **Character Creation (Sebelum Racial Bonus)** | **15** | Batas atas Point Buy per atribut. | `FINAL (Confirmed — GDD Bab 6 §6.2)` |
| **Character Creation (Setelah Racial Bonus)** | **17** | Batas atas skor Level 1 pasca-racial. | `FINAL (Confirmed — GDD Bab 6 §6.2)` |
| **Normal Progression Maximum** | **20** | Batas permanen dari `Point Buy + Racial + AP Allocation`. | `FINAL (Confirmed — GDD Bab 6 §6.2)` |
| **Standard Equipment Bonus** | **Tunduk pada Cap 20** | Bonus atribut equipment standar ikut di-clamp di `20`; hanya item dengan flag eksplisit `canExceedAttributeCap = true` yang dapat melampaui `20`. | `FINAL (Locked — Resolved TBD)` |
| **Temporary Buff** | **Boleh > 20 jika eksplisit (ceiling 30)** | Temporary buff hanya melampaui `20` jika efeknya secara eksplisit mengizinkan (`canExceedAttributeCap = true`). | `FINAL (Confirmed — GDD Bab 6 §6.2 & Resolved TBD)` |
| **Minimum Score (Debuffed)** | **1** | Debuff tidak menurunkan Ability Score di bawah `1`. | `FINAL (Locked — Resolved TBD)` |
| **AP Overflow Rule** | **Tidak Hangus** | AP yang tidak dapat dialokasikan karena Cap `20` tetap tersimpan di `unspentAttributePoints`. | `FINAL (Locked — Approved)` |

---

## 8. Calculation Examples

> **Disclaimer:** Seluruh nilai `SkillPower`, `Scaling`, `Base HP`, `Base MP`, `Base Healing`, `Base Buff`, dan `Base Evasion` di bawah ini adalah **`[EXAMPLE — Angka ilustrasi semata, bukan angka resmi GDD]`**.

### 8.1 Contoh Wajib Enam Ability Scores

| Ability | Skor | Mod | Input `[EXAMPLE]` | Perhitungan | Hasil Akhir |
|---|---:|---:|---|---|---|
| **STR** | `14` | `+2` *(tidak dipakai)* | `SkillPower = 40`, `Scaling = 1.5` | `40 + (14 × 1.5) = 40 + 21` (Raw STR) | **Base Physical Damage = `61`** |
| **DEX** | `16` | `+3` | `Base Hit = 85%`, `Base Eva = 5%`, `Normal W_P = 90 ms` | Ranged Acc: `2% × 3 = +6%`<br>Evasion: `2% × 3 = +6%` (Total `11%`)<br>QTE Window: `3% × 3 = +9%` (`90 × 1.09 = 98.1 ms`) | **Ranged Acc `+6%`**, **Evasion `+6%`**, **QTE Window `+9%`** |
| **VIT** | `16` | `+3` | `Base HP = 1,000` | `floor(1,000 × (1 + 0.04 × 3)) = floor(1,000 × 1.12)` | **Max HP = `1,120`** |
| **INT** | `16` | `+3` | `SkillPower = 50`, `Scaling = 1.5`, `Base MP = 200` | Elemental: `50 + (16 × 1.5) = 50 + 24 = 74` (Raw INT)<br>Max MP: `floor(200 × (1 + 0.04 × 3)) = floor(200 × 1.12)` | **Base Elemental Damage = `74`**<br>**Max MP = `224`** |
| **WIS** | `16` | `+3` | `Base Healing = 200` | `floor(200 × (1 + 0.04 × 3)) = floor(200 × 1.12)` | **Healing Output = `224`** |
| **CHA** | `16` | `+3` | `Base Attack Buff = +10%` | `Buff Eff = 4% × 3 = 12%`<br>`10% × (1 + 0.12) = 10% × 1.12` | **Final Attack Buff = `+11.2%`** |

### 8.2 Contoh Kasus Batas (Modifier Negatif, Anti-Compounding, & Cap)

| Kasus | Input `[EXAMPLE]` | Perhitungan | Hasil |
|---|---|---|---|
| **Modifier Negatif (VIT 8)** | `VIT = 8 (Mod -1)`, `Base HP = 1,000` | `floor(1,000 × 0.96)` | **Max HP = `960`** |
| **Modifier Negatif (DEX 8)** | `DEX = 8 (Mod -1)`, `Normal W_P = 90 ms` | Ranged Acc `-2%`, Evasion `-2%`, QTE Window `-3%` (`87.3 ms`) | **Penalti aktif penuh** |
| **Modifier Negatif (CHA 8)** | `CHA = 8 (Mod -1)`, `Base Attack Buff = +10%` | `Buff Eff = -4%` → `10% × 0.96` | **Final Buff = `+9.6%`** |
| **Non-Compounding Max HP** | `VIT 16 (+12%)` + Farmer Rank V (`+10%`), `Base HP = 1,000` | `floor(1,000 × (1 + 0.12 + 0.10))` *(bukan `1,120 × 1.10`)* | **Max HP = `1,220`** |
| **Shared QTE Cap `+25%`** | `DEX 20 (+15%)` + Steady Hand (`+10%`) + Ring (`+6%`) | `clamp(15% + 10% + 6%, -15%, +25%)` | **Effective QTE Window = `+25%`** |
| **Shield Independen** | `WIS 20 (+5)`, `CHA 18 (+4)`, `Divine Shield = 150` | Shield Power tidak membaca `WIS` atau `CHA` | **Shield = `150`** |

---

## 9. Character Progression Simulation

### 9.1 Karakter Simulasi `[EXAMPLE]`
- **Race:** **Liichtian** (`Celestial Wisdom`: `+2 WIS` — GDD Bab 6 §5.6)
- **Class:** **Cleric** (Hybrid Healer / Protector / Holy Offense)
- **Level 1 Point Buy (27/27 pts):** `STR 10` (2), `DEX 12` (4), `VIT 12` (4), `INT 14` (7), `WIS 15` (9), `CHA 9` (1) → Pasca-racial: **`STR 10, DEX 12, VIT 12, INT 14, WIS 17, CHA 9`**.

### 9.2 Progresi 18 AP (Level 1 → Level 15 Aktif → Level 30 Target)

| Level | AP | Alokasi | STR (Mod) | DEX (Mod) | VIT (Mod) | INT (Mod) | WIS (Mod) | CHA (Mod) | Catatan |
|---:|---:|---|---|---|---|---|---|---|---|
| **1** | 0 | Creation + `+2 WIS` | 10 (`0`) | 12 (`+1`) | 12 (`+1`) | 14 (`+2`) | 17 (`+3`) | 9 (`-1`) | Cap 17 pasca-racial; CHA `-1` |
| **4** | +2 | `+2 WIS` | 10 (`0`) | 12 (`+1`) | 12 (`+1`) | 14 (`+2`) | 19 (`+4`) | 9 (`-1`) | Kedua AP ke 1 atribut |
| **7** | +2 | `+1 WIS`, `+1 CHA` | 10 (`0`) | 12 (`+1`) | 12 (`+1`) | 14 (`+2`) | **20** (`+5`) | 10 (`0`) | WIS menyentuh Cap `20` |
| **10** | +2 | `+2 INT` | 10 (`0`) | 12 (`+1`) | 12 (`+1`) | 16 (`+3`) | 20 (`+5`) | 10 (`0`) | — |
| **13** | +2 | `+2 VIT` | 10 (`0`) | 12 (`+1`) | 14 (`+2`) | 16 (`+3`) | 20 (`+5`) | 10 (`0`) | **Akhir AP Cap 15 (8 AP)** |
| **16** | +2 | `+2 DEX` | 10 (`0`) | 14 (`+2`) | 14 (`+2`) | 16 (`+3`) | 20 (`+5`) | 10 (`0`) | Mulai fase Cap 30 |
| **19** | +2 | `+2 INT` | 10 (`0`) | 14 (`+2`) | 14 (`+2`) | 18 (`+4`) | 20 (`+5`) | 10 (`0`) | — |
| **22** | +2 | `+2 CHA` | 10 (`0`) | 14 (`+2`) | 14 (`+2`) | 18 (`+4`) | 20 (`+5`) | 12 (`+1`) | — |
| **25** | +2 | `+2 CHA` | 10 (`0`) | 14 (`+2`) | 14 (`+2`) | 18 (`+4`) | 20 (`+5`) | 14 (`+2`) | — |
| **28** | +2 | `+2 STR` | 12 (`+1`) | 14 (`+2`) | 14 (`+2`) | 18 (`+4`) | 20 (`+5`) | 14 (`+2`) | **Total 18 AP (Cap 30)** |

### 9.3 Derived Stats Simulasi (`[EXAMPLE]` Base Values)

Input ilustrasi `[EXAMPLE]`: Base HP (`300 / 720 / 1,300`), Base MP (`80 / 160 / 260`), Base Evasion (`5%`), Normal `W_P` (`90 ms`), Mace Strike (`30 + STR × 1.2`), Holy Smite (`50 + INT × 1.5`), Divine Heal (`Base 200`), Blessing (`Base +10%`), Divine Shield (`Base 150`), Lightbound DR (`-10%`).

| Derived Stat | Level 1 | Level 15 (8 AP — Cap Aktif) | Level 30 (18 AP — Target) | Atribut |
|---|---:|---:|---:|---|
| **Max HP** | `312` | `777` | `1,404` | `VIT Mod` |
| **Max MP** | `86` | `179` | `301` | `INT Mod` |
| **Base Physical Damage** | `42` | `42` | `44` | Raw `STR` |
| **Base Elemental Damage** | `71` | `74` | `77` | Raw `INT` |
| **Ranged Accuracy Bonus** | `+2%` | `+2%` | `+4%` | `DEX Mod` |
| **Total Evasion** | `7%` | `7%` | `9%` | `DEX Mod` |
| **Timing QTE Window Bonus** | `+3%` (`92.7 ms`) | `+3%` (`92.7 ms`) | `+6%` (`95.4 ms`) | `DEX Mod` |
| **Healing Output** | `224` | `240` | `240` | `WIS Mod` |
| **Buff Effectiveness Bonus** | `-4%` | `0%` | `+8%` | `CHA Mod` |
| **Final Blessing Attack Buff** | `+9.6%` | `+10.0%` | `+10.8%` | `CHA Mod` |
| **Social Check Value** | `9` | `10` | `12` | `CHA Mod` |
| **Shield Output** *(Separate)* | `150` | `150` | `150` | Independen |
| **Damage Reduction** *(Separate)* | `-10%` | `-10%` | `-10%` | Independen |

---

## 10. Balance Considerations

1. **Efisiensi DEX (3 Manfaat):** DEX maksimal (`20`) memberi `+10%` Ranged Accuracy, `+10%` Evasion, dan `+15%` Timing QTE Window. Mitigasi yang dijaga: (a) DEX tidak menambah Speed, Crit, Ranged Power, atau Counter Power; (b) Ranged Accuracy hanya berguna untuk Ranged; (c) bonus QTE berbagi cap `+25%` dengan equipment/talent/passive dan tidak berlaku pada Typing QTE (*Ward*) atau *Struggle* mash.
2. **Resource Pool VIT & INT:** Pada skor `20`, VIT memberi `+20%` Max HP dan INT memberi `+20%` Max MP. Non-compounding Base Reference mencegah inflasi eksponensial bersama Background (Farmer/Noble) dan Talent (Durable). MP Cost skill Tier tinggi tetap harus menuntut efisiensi MP sehingga INT 20 memberi ruang taktis tanpa menghapus tekanan resource.
3. **CHA pada Buff Persentase Tinggi:** Pada CHA 20 (`+20%` Buff Effectiveness), buff `+10%` → `+12.0%` dan `+30%` → `+36.0%`. Mitigasi: gunakan `supportsChaScaling = true` secara selektif hanya pada buff party/target terukur (`+10%–25%` Base), dan `false` pada self-steroid ekstrem (*Berserk*, *Draconic Overdrive*). Safety clamp `[-20%, +40%]` melindungi kasus temporary buff penembus cap 20.
4. **Kompetitivitas STR vs INT:** INT memberi Elemental Damage + Max MP, sedangkan STR hanya Physical Damage — namun Physical dapat Critical (`1.5×–3.0×`), sedangkan Elemental tidak dapat Critical secara default tetapi dapat mengeksploitasi Elemental Weakness (`1.5×`). Pastikan `Scaling` pada skill Tier tinggi tumbuh sebanding dengan `SkillPower` (menjaga porsi `Stat × Scaling` sekitar `35%–55%` dari Base Damage) agar AP pada STR/INT tetap berharga di late-game.
5. **Identitas WIS vs CHA:** WIS = *Reactive/Sustained Recovery* (HP heal & HoT); CHA = *Proactive Force Multiplication & Exploration* (Buff Magnitude opt-in + Social Checks). Pastikan sebagian besar skill heal Cleric/Druid memakai `Base Healing` (skala WIS) alih-alih mayoritas `% Max HP`.
6. **Independensi Shield Power:** Shield Power tetap di luar `WIS`, `CHA`, dan `VIT` agar build Cleric terbagi nyata antara Healing Specialist vs Shield/Protection Support.

---

## 11. Implementation Notes

### 11.1 Data Configuration (`AttributeSystemConfig`)
Seluruh konstanta disimpan dalam `AttributeSystemConfig` di `Assets/Scripts/Data/StatsSystem.cs`:
- Point Buy: `budget = 27`, `baseScore = 8`, `minScore = 8`, `maxBeforeRacial = 15`.
- Caps: `maxAfterRacialAtCreation = 17`, `normalProgressionCap = 20`, `absoluteHardCap = 30`, `absoluteMinScore = 1`.
- Milestones: `apMilestoneLevels = { 4, 7, 10, 13, 16, 19, 22, 25, 28 }`, `apPerMilestone = 2` (dengan `RunConfig.MaxLevel = 15` aktif saat ini).
- Koefisien Derived Stats: `VIT Max HP = 0.04f`, `INT Max MP = 0.04f`, `DEX Ranged Acc = 0.02f`, `DEX Evasion = 0.02f`, `DEX QTE Window = 0.03f`, `WIS Healing = 0.04f`, `CHA Buff Effectiveness = 0.04f`.
- Batas: `QTE Window ∈ [-15%, +25%]`, `Hit Chance ∈ [5%, 95%]` (`Base = 85%`), `Evasion ∈ [0%, 75%]`, `Status Resistance ∈ [0%, 75%]`, `CHA Buff Eff ∈ [-20%, +40%]`.

### 11.2 Pemetaan Modul Unity C#
- **`StatsSystem.cs`:** `AttributeSystemConfig`, `AbilityScoreSheet` (cap 20 normal + `capBreakingTemporaryModifiers` hingga 30), `AbilityScoreImprovementConfig` (`4, 7, 10, 13, 16, 19, 22, 25, 28`), `AccuracySystem` (DEX accuracy khusus `AttackCategory.Ranged`), `StatCalculator` (formula final + `Math.Floor` + pelepasan seluruh coupling terlarang termasuk Block Power & Counter Power).
- **`Database.cs`:** `AsiConfig`, `AbilityDefs`, `Formulas`, `DerivedStatDefs`, `QteStatInteractions`.
- **`QteConfig.cs` & `Battle.cs`:** `SupportPotencyMultiplier` dan `SupportScaled` memakai `1 + clamp(0.04 × CHAMod, -0.20, +0.40)` dengan modifier negatif aktif penuh dan flag opt-in `supportsChaScaling`.
- **`BattleAction.cs` & `Combatant.cs`:** `Rules.Healing` memakai `Math.Floor` dengan `WIS` scaling berbasis `% Base Healing`; `DamageComponent` memakai Raw `STR` / Raw `INT`.

---

## 12. Resolved Decisions & Cross-Document Conflict Registry

### 12.1 Keputusan Final (Seluruh Ex-TBD & Ex-Proposal Terkunci)

| ID | Elemen | Keputusan Final |
|---|---|---|
| **D1** | Point Buy Budget & Cost Table | `27 points`, base `8`, rentang `8–15`, biaya `8:0, 9:1, 10:2, 11:3, 12:4, 13:5, 14:7, 15:9`. |
| **D2** | Level Cap Aktif vs Jadwal AP | **Level Cap aktif = 15** (`RunConfig.MaxLevel = 15`, 4 milestone aktif: Lv 4, 7, 10, 13 = 8 AP); seluruh 9 milestone hingga Lv 28 (18 AP) sudah tertanam untuk ekspansi Cap 30. |
| **D3** | Block Power & Counter Power | **Dipisah penuh** dari `STR` dan `DEX`; keduanya murni berasal dari Class Base + Equipment/Shield + Passive/Skill/Overdrive. |
| **D4** | DEX Derived Stat Coefficients | Ranged Accuracy `2% × DEX Mod`, Evasion `2% × DEX Mod`, Timing QTE Window `3% × DEX Mod`. |
| **D5** | Accuracy / Hit Chance Baseline | `clamp(85% + AttackerAcc - TargetEva, 5%, 95%)`; Evasion cap `75%`; DEX Accuracy khusus Ranged. |
| **D6** | Rounding Policy | `floor()` untuk Modifier, Max HP, Max MP, Damage (sekali per hit), Healing (sekali per cast/tick). Float penuh untuk persentase buff/chance/QTE ms. |
| **D7** | Equipment Bonus vs Cap 20 | Equipment standar tunduk pada cap `20`; hanya item/buff dengan flag eksplisit `canExceedAttributeCap = true` yang dapat melampaui `20` (ceiling `30`). |
| **D8** | CHA Buff Ceiling & Social Check | `Buff Effectiveness Bonus ∈ [-20%, +40%]`; Social Check terpisah = `10 + CHA Mod + Social Bonuses` vs Event DC (`10/12/14/16`). |

### 12.2 Status Resolusi Konflik Lintas Dokumen

| ID | Area Konflik | Resolusi Final |
|---|---|---|
| **C1** | Level Cap 15 vs 30 | Level Cap aktif tetap `15` untuk fase saat ini; jadwal AP 9 milestone (`4, 7, 10, 13, 16, 19, 22, 25, 28`) sudah dikunci dan siap untuk ekspansi Cap 30. |
| **C2** | Workbook Advancement | Belum tersedia di repositori; akan disinkronkan ulang jika workbook Advancement diserahkan kemudian. |
| **C3** | Coupling Lama di `StatsSystem.cs` | Diselesaikan melalui refaktor `StatsSystem.cs`, `Database.cs`, `QteConfig.cs`, `Battle.cs`, dan `BattleAction.cs`. |
| **C4** | Block Power (`STR`) & Counter Power (`STR+DEX`) di Bab 12 | Diputuskan dipisah penuh; Bab 12 §3.5, §6.5, §8.2 dan kode Unity telah disinkronkan. |
| **C5** | Penamaan `AGI` vs `DEX` & contoh cap Bab 13 | Disinkronkan di GDD Bab 13 (`AGI` → `DEX` dan catatan kepatuhan Cap `20`); spreadsheet `Items Catalogue.xlsx` perlu diselaraskan secara terpisah saat pembaruan Excel berikutnya. |
| **C6** | Terminologi *Arcane*/*Dark* di Bab 8 & *MagicPower* di kode | Kategori damage tetap 3 resmi (`Physical`, `Elemental`, `Almighty`); `DerivedStatDefs[MagicPower]` diberi label Elemental/Spell Power tanpa membuat kategori *Magical Damage* baru. |
