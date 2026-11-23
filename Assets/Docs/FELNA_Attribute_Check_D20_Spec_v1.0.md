# FELNA — ATTRIBUTE CHECK: D20 SYSTEM INTEGRATION SPECIFICATION

**Document ID:** `FELNA-SYS-EVT-D20-001`  
**Version:** `1.0 — FINAL`  
**Date:** `2026-11-23`  
**Status:** `FINAL` (Seluruh aturan inti, resolusi konflik, dan integrasi kode C# telah disetujui; rentang angka DC ditandai secara eksplisit sebagai rekomendasi balancing)  
**Dokumen Terkait:** `GDD_Terbaru.md` (Bab 3, 5, 6, 7, 13, 14, 15, 16, 19, 21), `FELNA_Attribute_Progression_Derived_Stats_Spec_v1.0.md` (`FELNA-SYS-ATTR-002 v2.0`), `Felna_Dungeon1_Event_Pool_Final.xlsx`

> **Catatan Status Provenance:**
> - `FINAL (Confirmed — Approved Rule)` — Aturan inti yang telah ditetapkan di GDD FELNA atau disetujui secara eksplisit oleh desainer proyek.
> - `RECOMMENDED (Balancing Baseline)` — Rekomendasi rentang nilai Difficulty Class (DC) dan contoh tuning event berdasarkan analisis distribusi Attribute Modifier party; bukan angka kaku yang mengikat seluruh event masa depan tanpa evaluasi playtest.

---

## 1. System Overview

### 1.1 Tujuan Sistem (`FINAL (Confirmed — Approved Rule)`)
**Attribute Check** merupakan mekanik dalam **Exploration Event** (`NodeType.Event`) yang memungkinkan pemain menggunakan **Character Attributes** (`STR`, `DEX`, `VIT`, `INT`, `WIS`, `CHA`) untuk menghadapi tantangan tertentu melalui lemparan dadu 20-sisi (**D20**).

Sistem ini dirancang untuk:
1. Memberikan pilihan penyelesaian event yang lebih interaktif di samping solusi berbasis sumber daya (*Alternative Solutions*) dan kemampuan ras (*Racial Utility*).
2. Menggunakan **Attribute Modifier tertinggi dari anggota party yang masih hidup/aktif** (`Party Highest Attribute Modifier`) untuk atribut yang diminta oleh event.
3. Menghadirkan unsur keberuntungan yang terkendali (*Controlled RNG*) melalui lemparan **D20**.
4. Memperlihatkan tingkat kesulitan (**Difficulty Class / DC**), atribut yang diuji, dan modifier tertinggi party **sebelum** pemain menentukan pilihan (*Knowledge Is Power*).
5. Memberikan empat tingkat konsekuensi berbeda berdasarkan hasil pemeriksaan (**Critical Success**, **Success**, **Failure**, **Critical Failure**).

### 1.2 Penggantian Formula Pasif Lama (`FINAL (Confirmed — Approved Rule)`)
Formula pasif lama `Social Check Value = 10 + CHA Mod + Social Bonuses` dihapus dan digantikan sepenuhnya oleh sistem **D20 Attribute Check (`D20 Roll + Party Highest Attribute Modifier`)**. Pemeriksaan event tidak lagi terbatas pada `CHA` secara pasif, melainkan dapat menguji salah satu dari keenam Ability Scores (`STR`, `DEX`, `VIT`, `INT`, `WIS`, atau `CHA`) sesuai konteks tantangan.

---

## 2. Core Mechanics

### 2.1 Alur Eksekusi Attribute Check (`FINAL (Confirmed — Approved Rule)`)
1. **Penyajian Pilihan:** Saat pemain memasuki sebuah Exploration Event node, setiap opsi bertipe Attribute Check menampilkan **Nama Attribute**, **Difficulty Class (DC)**, **Party Highest Attribute Modifier** (dari anggota party yang hidup), dan informasi bahwa hasil ditentukan melalui **D20 Roll**.
2. **Keputusan Pemain:** Pemain bebas memilih opsi Attribute Check, opsi *Racial Utility* (jika syarat ras terpenuhi), atau opsi *Alternative Solution*.
3. **Keterbukaan Akses:** Pilihan Attribute Check **selalu dapat dipilih**, berapa pun nilai modifier party (termasuk `+0` atau negatif seperti `-1`). Tidak ada Attribute Check yang dikunci hanya karena modifier party rendah.
4. **Lemparan D20 Tunggal:** Ketika pemain memilih Attribute Check, sistem melakukan tepat **satu kali lemparan D20** (`1–20`) dan menambahkan **Party Highest Attribute Modifier**.
5. **Evaluasi 4 Kategori & Resolusi:** Sistem membandingkan hasil terhadap aturan **Natural 20**, **Natural 1**, dan **DC**, menerapkan reward atau konsekuensi yang sesuai, menampilkan rincian penuh hasil lemparan di layar *Outcome*, dan menandai node event sebagai selesai (`Cleared`).

---

## 3. Party Highest Attribute Modifier

### 3.1 Aturan Pemilihan Modifier (`FINAL (Confirmed — Approved Rule)`)
Ketika pemain memilih Attribute Check untuk atribut $A \in \{\text{STR}, \text{DEX}, \text{VIT}, \text{INT}, \text{WIS}, \text{CHA}\}$, sistem secara otomatis mengambil **Attribute Modifier tertinggi dari seluruh anggota party yang masih hidup/aktif (`HP > 0`)**:

$$\text{PartyHighestMod}(A) = \max_{u \in \text{Party}, \, u.\text{Alive}} \left( \text{Mod}_u(A) \right)$$

Di mana formula Attribute Modifier setiap karakter tetap mengikuti aturan baku FELNA (`GDD Bab 5 §3` & `FELNA-SYS-ATTR-002 §6.1`):

$$\text{Mod}_u(A) = \left\lfloor \frac{\text{Score}_u(A) - 10}{2} \right\rfloor$$

### 3.2 Ketentuan Mutlak (`FINAL (Confirmed — Approved Rule)`)
1. **Hanya Modifier Tertinggi:** Hanya satu modifier tertinggi yang digunakan.
2. **Tidak Dijumlahkan:** Modifier dari beberapa karakter tidak pernah dijumlahkan.
3. **Tidak Dirata-ratakan:** Modifier dari anggota party tidak pernah dirata-ratakan.
4. **Otomatis Tanpa Pemilihan Karakter:** Pemain tidak perlu menentukan karakter mana yang melakukan check; sistem memilih modifier tertinggi secara otomatis.
5. **Hanya Anggota Party yang Aktif/Hidup (`HP > 0`):**
   - Karakter yang mati (`HP == 0` / `!Alive`) saat battle **tidak** bangkit kembali secara otomatis ke `1 HP` setelah battle selesai (baik pada battle Normal, Elite, Stage Miniboss, maupun Final Boss).
   - Karakter yang mati tetap berada dalam kondisi mati (`HP == 0`) selama eksplorasi dungeon sampai dihidupkan kembali menggunakan *Revive Item* (*Phoenix Plume*) atau melalui layanan *Revive* di Hub Temple (`GDD Bab 16 §5–§6`).
   - Karakter yang sedang mati (`HP == 0`) **tidak dapat** menyumbangkan Attribute Modifier-nya pada perhitungan `Party Highest Attribute Modifier`.

### 3.3 Contoh Penentuan Party Highest Attribute Modifier (`FINAL (Confirmed — Approved Rule)`)

**Kasus A — Seluruh Anggota Party Hidup:**

| Character | Status | STR Score | STR Modifier | Perhitungan |
|---|---|---:|---:|---|
| Character A | Alive (`HP > 0`) | 12 | `+1` | Diabaikan |
| **Character B** | **Alive (`HP > 0`)** | **16** | **`+3`** | **Digunakan (`Party Highest STR Modifier = +3`)** |
| Character C | Alive (`HP > 0`) | 10 | `+0` | Diabaikan |
| Character D | Alive (`HP > 0`) | 14 | `+2` | Diabaikan |

**Kasus B — Pemilik Modifier Tertinggi Sedang Mati (`HP == 0`):**
Jika Character B (`STR Mod +3`) mati pada battle sebelumnya dan belum di-revive, maka sistem mengambil modifier tertinggi dari anggota party yang masih hidup, yaitu **Character D (`STR Mod +2`)**.

---

## 4. D20 Roll Calculation

### 4.1 Rumus Perhitungan (`FINAL (Confirmed — Approved Rule)`)
Ketika Attribute Check dipilih, sistem melakukan satu kali lemparan D20 (`1` s.d. `20`) dan menghitung:

$$\text{Check Result} = \text{D20 Roll} + \text{Party Highest Attribute Modifier}$$

Hasil akhirnya dibandingkan dengan **Difficulty Class (DC)** milik pilihan event tersebut (dengan pengecualian mutlak pada Natural 20 dan Natural 1 sesuai §6).

### 4.2 Determinisme Seed & Controlled RNG (`FINAL (Confirmed — Approved Rule)`)
- Setiap angka pada D20 (`1` hingga `20`) memiliki peluang kemunculan seragam sebesar **5%** (`1/20`).
- Sesuai aturan *Controlled RNG — No Bad-Luck Protection* (`GDD Bab 3 §2.4`), lemparan D20 bersifat independen tanpa *pity timer*.
- Untuk mencegah eksploitasi *save-scumming* (keluar/reload untuk mengulang dadu), lemparan D20 diikat secara deterministik pada seed node event (`NodeRng(NodeDef, salt + choiceIndex)`).

---

## 5. Difficulty Class (DC)

### 5.1 Ketentuan Difficulty Class (`FINAL (Confirmed — Approved Rule)`)
1. **Selalu Terlihat Sebelum Dipilih:** DC wajib ditampilkan secara terbuka kepada pemain pada kartu pilihan sebelum Attribute Check dipilih.
2. **Ditentukan per Event:** Setiap pilihan Attribute Check memiliki DC spesifik yang ditetapkan oleh data event.
3. **Dasar Pertimbangan Taktis:** Pemain dapat membandingkan DC terhadap `Party Highest Attribute Modifier` dan menimbang peluang keberhasilan sebelum memutuskan apakah akan mengambil Attribute Check, memakai *Racial Utility*, atau membayar *Alternative Solution*.
4. **Target Keberhasilan:** Untuk lemparan Natural `2–19`, DC adalah target angka yang harus **dicapai atau dilewati** (`Check Result >= DC`).

---

## 6. Check Result Categories

### 6.1 Empat Kategori Hasil (`FINAL (Confirmed — Approved Rule)`)
Sistem memiliki tepat empat kategori hasil dengan aturan evaluasi berikut:

| Hasil | Ketentuan (`FINAL (Confirmed — Approved Rule)`) |
|---|---|
| **Critical Success** | **Natural 20** (`D20 Roll == 20`) |
| **Success** | `D20 Roll` bernilai `2–19` dan `Check Result >= DC` |
| **Failure** | `D20 Roll` bernilai `2–19` dan `Check Result < DC` |
| **Critical Failure** | **Natural 1** (`D20 Roll == 1`) |

### 6.2 Aturan Khusus Natural 20 dan Natural 1 (`FINAL (Confirmed — Approved Rule)`)
- **Natural 20 selalu menghasilkan Critical Success**, tanpa memperhatikan DC atau Modifier (bahkan jika `20 + Modifier < DC`).
- **Natural 1 selalu menghasilkan Critical Failure**, tanpa memperhatikan DC atau Modifier (bahkan jika `1 + Modifier >= DC`).
- Untuk **Natural 2–19**, hasil ditentukan murni dengan membandingkan `Check Result` terhadap `DC`.

### 6.3 Contoh Perhitungan Hasil (`FINAL (Confirmed — Approved Rule)`)
**Parameter:** `DC = 15`, `Party Highest STR Modifier = +3`

| D20 Roll | Modifier | Check Result | Perbandingan vs DC (15) | Hasil Akhir |
|---:|---:|---:|---|---|
| `1` | `+3` | `4` | Override Natural 1 | **Critical Failure** |
| `5` | `+3` | `8` | `8 < 15` | **Failure** |
| `12` | `+3` | `15` | `15 >= 15` | **Success** |
| `19` | `+3` | `22` | `22 >= 15` | **Success** |
| `20` | `+3` | `23` | Override Natural 20 | **Critical Success** |

---

## 7. Consequences and Rewards

### 7.1 Tingkat Konsekuensi per Kategori Hasil (`FINAL (Confirmed — Approved Rule)`)

1. **Critical Success**
   - Menghasilkan keberhasilan terbaik.
   - Memberikan reward utama ditambah bonus reward ekstra, keuntungan tambahan, atau efek positif khusus (seperti tambahan *Spoils*, tambahan *Upgrade Material*, *Consumable* ekstra, atau *Dungeon Modifier* buff positif).
2. **Success**
   - Tantangan berhasil diselesaikan.
   - Pemain memperoleh hasil normal sesuai tujuan pilihan event tersebut.
3. **Failure**
   - Tantangan tidak berhasil diselesaikan.
   - Pemain menerima konsekuensi normal yang ditentukan oleh event (seperti kehilangan sebagian HP, terkena *Hazard Damage*, atau hanya memperoleh sebagian hasil).
4. **Critical Failure**
   - Tantangan mengalami kegagalan kritis.
   - Pemain menerima konsekuensi yang lebih berat daripada Failure biasa (seperti kehilangan HP / *Hazard Damage* yang lebih tinggi ditambah *Dungeon Modifier* debuff seperti *Fractured Focus*, *Withering Curse*, *Heavy Steps*, atau *Leaking Mana*, serta kehilangan akses reward).

### 7.2 Kontekstualitas & Aturan Non-Lethal Exploration Damage (`FINAL (Confirmed — Approved Rule)`)
- Konsekuensi tidak diseragamkan secara kaku untuk semua event; setiap event menentukan konsekuensi dan reward yang sesuai dengan konteks narasi, kategori event, dan situasinya.
- Sesuai aturan Exploration Event FELNA, kehilangan HP di dalam event (`hurtPercent` maupun `hazardDamagePercent`) chỉ berdampak pada anggota party yang masih hidup (`u.Alive`) dan tidak pernah menurunkan HP mereka di bawah `1 HP` (`Math.Max(1, u.hp - dmg)`). Karakter yang sudah mati (`HP == 0`) tetap memiliki `0 HP`.

---

## 8. Exploration Event Integration

### 8.1 Integrasi dengan Struktur Data Event FELNA (`FINAL (Confirmed — Approved Rule)`)
Di dalam arsitektur Exploration Event FELNA (`Defs.cs`, `RunState.cs`, `Dungeon1EventPool.cs`):
- Sebuah `EventDef` memuat array pilihan `EventChoice[]`.
- Sebuah `EventChoice` bertipe Attribute Check memiliki flag `isAttributeCheck = true`, `checkAbility` (`Ability.STR / DEX / VIT / INT / WIS / CHA`), `difficultyClass` (`int`), serta empat definisi hasil (`criticalSuccessOutcome`, `successOutcome`, `failureOutcome`, `criticalFailureOutcome`).
- Opsi Attribute Check ditambahkan sebagai **pilihan tambahan** di samping opsi yang sudah ada pada event, sehingga memperkaya variasi keputusan tanpa merusak struktur event yang telah berjalan.

### 8.2 Peran Tematik Keenam Attribute dalam Exploration Event (`FINAL (Confirmed — Approved Rule)`)
Integrasi ini menjaga fungsi utama masing-masing Attribute di dalam combat (`FELNA-SYS-ATTR-002`) sambil memperluas kegunaannya di luar combat:
- **STR (Strength):** Mendobrak pintu batu/palang besi, mengangkat beban reruntuhan, menahan tekanan mekanis.
- **DEX (Dexterity):** Membuka kunci bersegel (*lockpicking*), melepas pegas perangkap jarum/panah, manuver presisi tinggi.
- **VIT (Vitality):** Menahan badai abu panas, racun, gas, cairan meragukan, atau pengurasan fisik.
- **INT (Intelligence):** Memecahkan urutan segel astronomi/mekanisme kuno, menganalisis rune dan arsip sejarah.
- **WIS (Wisdom):** Menyelaraskan batin dengan gema doa/altar spiritual, menangkal ilusi dan bisikan supernatural.
- **CHA (Charisma):** Memerintah atau bernegosiasi dengan NPC penjaga melalui wibawa ekspedisi kerajaan, menghadapi entitas berakal.

---

## 9. Racial Utility and Alternative Solutions Interaction

### 9.1 Koeksistensi Tiga Jenis Pilihan (`FINAL (Confirmed — Approved Rule)`)
Attribute Check **tidak menggantikan** sistem *Racial Utility* (`GDD Bab 6 §5 & §10`) maupun *Alternative Solutions* (`GDD Bab 6 §10.1`). Dalam sebuah Exploration Event, pemain dapat menemukan ketiga jenis pilihan secara berdampingan:
1. **Attribute Check:** Menggunakan `D20 Roll + Party Highest Attribute Modifier` vs `DC`.
2. **Racial Utility:** Menggunakan kemampuan khusus suatu race sesuai Race System FELNA (deterministik, membutuhkan race terkait di dalam party atau dipinjam via *Human Adaptability*).
3. **Alternative Solution:** Menggunakan solusi lain yang disediakan event, seperti membayar *Spoils*, kehilangan HP pasti, memakai *Consumable* / *Material*, atau mengambil jalur alternatif yang aman.

### 9.2 Ketentuan Interaksi (`FINAL (Confirmed — Approved Rule)`)
- **Selalu Dapat Dipilih:** Attribute Check tetap dapat dipilih meskipun `Party Highest Attribute Modifier` rendah atau negatif.
- **Tidak Pernah Dikunci oleh Modifier:** Tidak ada Attribute Check yang dikunci hanya karena modifier party tidak memenuhi angka tertentu.
- **Independensi Pilihan:** Racial Utility dan Alternative Solutions tetap dapat dipilih secara independen; tidak ada kewajiban bagi pemain untuk menggunakan Attribute Check.
- **Pemisahan dari Aksi Racial Utility:** Penggunaan Attribute Check tidak otomatis mengaktifkan atau menggantikan pilihan aksi Racial Utility.
- **Human Adaptability (`GDD Bab 6 §5.1`):**
  - Pinjaman opsi racial (`75% effect`, `1x per dungeon`) hanya berlaku pada pilihan **Racial Utility** (`requiredRace`), bukan pada Attribute Check.
  - Opsi *Improvise* milik Human tetap muncul pada event yang tidak memiliki pilihan *Racial Utility* (`!ev.HasAnyRacialChoice()`), terlepas dari ada atau tidaknya pilihan Attribute Check.
- **Mitigasi Pasif Berbasis Tag Tetap Berlaku (`FINAL — Confirmed OD-04`):**
  - Apabila sebuah pilihan Attribute Check gagal (`Failure` atau `Critical Failure`) dan menghasilkan *Hazard Damage* (`hazardDamagePercent`), maka efek mitigasi pasif berbasis tag seperti **Halfling `Small and Nimble` (`-50% party hazard damage`)** dan **Relic `R014 Pallbearer's Gloves` (meniadakan *minor chest trap* pertama di dungeon jika `isTrapChoice = true`)** **tetap berlaku** mengurangi atau meniadakan hazard damage tersebut.

---

## 10. UI and Player Feedback

### 10.1 Tampilan Sebelum Keputusan (Kartu Pilihan `EventScreen`) (`FINAL (Confirmed — Approved Rule)`)
Sebelum pemain memilih opsi Attribute Check, kartu pilihan pada `EventScreen` wajib menampilkan:
1. **Nama Attribute yang digunakan** dan **Difficulty Class (DC)** pada badge kanan kartu:
   - Format Badge: `[<ABILITY> CHECK · DC <X>]` (contoh: `STR CHECK · DC 13`).
2. **Informasi D20 Roll, DC, dan Party Highest Attribute Modifier** pada baris subjudul kartu:
   - Format Subjudul: `D20 ROLL · DC <X> · PARTY HIGHEST <ABILITY> MOD: <+Y>` (contoh: `D20 ROLL · DC 13 · PARTY HIGHEST STR MOD: +3`).
3. **Penyembunyian Detail Output Sebelum Dipilih:** Sesuai prinsip UI Exploration Event FELNA (`GDD Bab 19 §5.14`), rincian angka hadiah atau penalti dari masing-masing kategori hasil tetap disembunyikan sebelum pilihan dieksekusi.

### 10.2 Tampilan Setelah Roll Dilakukan (Panel Outcome `EventScreen`) (`FINAL (Confirmed — Approved Rule)`)
Setelah pemain memilih Attribute Check, panel *Outcome* menampilkan kotak hasil pemeriksaan (`AttributeCheckResultBox`) beserta rincian berikut:
1. **Nama Attribute & Target DC:** `<ABILITY> ATTRIBUTE CHECK · TARGET DC <X>`
2. **Hasil D20 Roll, Modifier, dan Total Check Result:**
   - `D20 ROLL: <Roll> [(NATURAL 20) / (NATURAL 1)]  +  PARTY HIGHEST <ABILITY> MOD: <+Mod>  =  TOTAL: <CheckResult>`
3. **Kategori Hasil Akhir:**
   - `RESULT: CRITICAL SUCCESS` / `RESULT: SUCCESS` / `RESULT: FAILURE` / `RESULT: CRITICAL FAILURE`
4. **Narasi & Konsekuensi / Reward:** Teks narasi hasil serta rincian perolehan *Spoils*, *Equipment*, *Consumable*, *Material*, *Relic*, *Dungeon Modifier*, atau kehilangan HP.

---

## 11. Balance Considerations

### 11.1 Analisis Rentang Attribute Modifier Party
Berdasarkan aturan Point Buy 27 poin (`8–15` pra-racial, maks `17` pasca-racial di Level 1) dan progresi `+2 AP` setiap milestone Skill Tier Unlock (`Level 4, 7, 10, 13` = 8 AP pada Level Cap 15, dengan Normal Progression Cap `20`):
- **Skor 8–9 (`Modifier -1`):** *Dump stat* jika tidak ada satu pun anggota party yang berinvestasi pada atribut tersebut.
- **Skor 10–11 (`Modifier +0`):** Atribut netral tanpa investasi.
- **Skor 12–15 (`Modifier +1` s.d. `+2`):** Atribut sekunder party pada awal hingga pertengahan run.
- **Skor 16–17 (`Modifier +3`):** Atribut utama (*Primary Attribute*) karakter spesialis pada Level 1 (Dungeon 1).
- **Skor 18–19 (`Modifier +4`):** Atribut utama karakter spesialis pada Level 4–7 (pertengahan Dungeon 1).
- **Skor 20 (`Modifier +5`):** Atribut utama karakter spesialis pada Normal Progression Cap (Level 7–15, akhir Dungeon 1 hingga Dungeon 2–8).
- **Dampak Kematian Karakter terhadap Modifier Party:** Karena karakter yang mati (`HP == 0`) tidak lagi bangkit otomatis ke `1 HP` setelah battle dan tidak dihitung dalam `Party Highest Attribute Modifier`, kehilangan anggota party spesialis (mis. Wizard dengan `INT 18 [+4]` tumbang) akan menurunkan `Party Highest INT Modifier` ke anggota hidup berikutnya (mis. `+1` atau `+0`). Hal ini menciptakan tekanan survival yang kohesif antara combat dan eksplorasi.

### 11.2 Tabel Probabilitas Keberhasilan (`Success + Critical Success`)
Dengan aturan Natural 20 = selalu **Critical Success (`5%`)** dan Natural 1 = selalu **Critical Failure (`5%`)**:

| Target DC | Mod `-1` | Mod `+0` | Mod `+1` | Mod `+2` | Mod `+3` | Mod `+4` | Mod `+5` |
|:---:|---:|---:|---:|---:|---:|---:|---:|
| **DC 8** | 65% | 70% | 75% | 80% | 85% | 90% | 95% |
| **DC 10** | 55% | 60% | 65% | 70% | 75% | 80% | 85% |
| **DC 11** | 50% | 55% | 60% | 65% | 70% | 75% | 80% |
| **DC 12** | 45% | 50% | 55% | 60% | 65% | 70% | 75% |
| **DC 13** | 40% | 45% | 50% | 55% | 60% | 65% | 70% |
| **DC 14** | 35% | 40% | 45% | 50% | 55% | 60% | 65% |
| **DC 15** | 30% | 35% | 40% | 45% | 50% | 55% | 60% |
| **DC 16** | 25% | 30% | 35% | 40% | 45% | 50% | 55% |
| **DC 18** | 15% | 20% | 25% | 30% | 35% | 40% | 45% |

### 11.3 Rekomendasi Rentang DC Berdasarkan Tingkat Kesulitan Event (`RECOMMENDED — Bukan Aturan Final`)

> **Catatan:** Rentang DC di bawah ini adalah **rekomendasi baseline** untuk memandu perancangan event dan dapat disesuaikan melalui playtesting.

| Kategori Kesulitan Event | Rekomendasi Rentang DC (`RECOMMENDED`) | Peluang Lulus (`Mod +0`) | Peluang Lulus (`Mod +3` Spesialis Lv 1) | Peluang Lulus (`Mod +5` Cap) | Pedoman Penggunaan |
|---|:---:|---:|---:|---:|---|
| **Easy (Mudah)** | **DC 8 – 10** | 60% – 70% | 75% – 85% | 85% – 95% | Rintangan ringan atau event pembuka dengan risiko gagal kecil. |
| **Moderate (Menengah)** | **DC 11 – 13** | 45% – 55% | 60% – 70% | 70% – 80% | Standar tantangan Exploration Event di Dungeon 1 (`Uncommon`). Memberikan keunggulan nyata bagi party yang memiliki spesialis atribut terkait (`65%–70%` di Lv 1) dibandingkan lemparan koin `50%`. |
| **Hard (Sulit)** | **DC 14 – 16** | 30% – 40% | 45% – 55% | 55% – 65% | Event `Rare` atau rintangan berhadiah tinggi (*Equipment*, *Relic*, *Rare Modifier*). |
| **Very Hard (Sangat Sulit)** | **DC 17 – 19** | 15% – 25% | 30% – 40% | 40% – 50% | Tantangan ekstrem di layer dalam atau objek terkutuk tingkat tinggi. |

---

## 12. Edge Cases and Exploit Prevention

1. **Pencegahan Percobaan Berulang pada Event yang Sama (`FINAL`):**
   - Setiap Exploration Event node bersifat *single-resolution*. Begitu pemain memilih Attribute Check, lemparan D20 langsung dievaluasi dan node ditandai `Cleared`. Pemain tidak dapat mengulang Attribute Check yang gagal pada node yang sama.
2. **Pencegahan *Save-Scumming* melalui Seeded Node RNG (`FINAL`):**
   - Nilai D20 Roll untuk setiap opsi Attribute Check dibangkitkan secara deterministik dari `NodeRng(Current, 40 + choiceIndex)` yang bergantung pada `DungeonSeed`, `Stage`, `NodeId`, dan `ChoiceIndex`. Memuat ulang (*reload*) save pada node yang sama tidak akan mengubah hasil lemparan D20.
3. **Evaluasi *Hyper-Specialization* (Pemanfaatan Modifier Tertinggi Berlebihan) (`FINAL`):**
   - Party berisi 4 karakter sedangkan sistem memiliki 6 Ability Scores (`STR`, `DEX`, `VIT`, `INT`, `WIS`, `CHA`). Bahkan jika keempat karakter mengambil spesialisasi atribut utama yang berbeda, minimal 2 dari 6 atribut di dalam party akan tetap berada pada tingkat sekunder/rendah.
   - Selain itu, karena karakter yang mati (`HP == 0`) saat battle tidak bangkit otomatis ke `1 HP` dan tidak dihitung dalam `Party Highest Attribute Modifier`, party yang mengorbankan atribut pertahanan (`VIT`/`STR`/`WIS`) demi menumpuk satu atribut akan mudah kehilangan karakter di combat dan kehilangan akses modifier tinggi tersebut di Exploration Event.
   - Aturan **Natural 1 = Critical Failure (`5%`)** memastikan bahwa modifier setinggi apa pun (`+5`) tidak pernah memberikan keberhasilan 100% bebas risiko seperti *Racial Utility*.
4. **Penanganan Kapasitas Inventory / Relic Penuh (`FINAL`):**
   - Jika sebuah opsi Attribute Check berpotensi memberikan *Relic* (`MayGrantRelic`) ketika slot Relic party sudah penuh (`5/5`) atau memberikan *Equipment* (`MayGrantEquipment`) ketika slot Equipment penuh (`100/100`), kartu pilihan dinonaktifkan sebelum dipilih dengan pesan jelas (`"Relic capacity full (5/5)"` / `"Equipment inventory full"`), sehingga pemain tidak pernah kehilangan hadiah utama setelah menang roll D20.

---

## 13. Example Event Implementations

Keenam contoh implementasi berikut mencakup seluruh 6 Ability Scores (`STR`, `DEX`, `VIT`, `INT`, `WIS`, `CHA`) dengan 4 kategori hasil (**Critical Success**, **Success**, **Failure**, **Critical Failure**) dan telah diintegrasikan sebagai pilihan tambahan di samping *Racial Utility* dan *Alternative Solutions* pada `Dungeon1EventPool.cs`:

### 13.1 STR Check — `D1_EVT_008` (*The Sealed Armory*)
- **Pilihan Attribute Check:** *Heave the jammed stone doors open by raw force* (`STR Check · DC 13`)
- **Critical Success (Natural 20):** Pintu batu terangkat bersih hingga palang besinya terlepas utuh (`+1 Random Equipment`, `+2x Iron Scrap [MAT001]`, `+25 Spoils`).
- **Success (`Check Result >= 13`):** Palang pintu berhasil dipatahkan tanpa cedera (`+1 Random Equipment`, `+1x Iron Scrap [MAT001]`).
- **Failure (`Check Result < 13`):** Pintu batu terpeleset kembali menghantam bahu party saat meraih satu senjata dari celah (`Party kehilangan 10% Max HP`, `+1 Random Equipment`).
- **Critical Failure (Natural 1):** Ambang batu atas runtuh menghantam party dan menutup rapat pintu gudang (`Party kehilangan 18% Max HP`, terkena debuff `MOD-020 Heavy Steps [Party Speed -5%, 1 Battle]`).
- **Opsi Lain yang Tetap Tersedia:** *Breach with a Smoke Bomb* (Alternative Solution — Consumable), *Strain the geared door open by force* (Alternative Solution — HP Cost), *Trigger the ancient mechanism & breach shortcut* (Racial Utility — Dwarf *Stonecraft*), *Pass the sealed armory* (Safe Exit).

### 13.2 DEX Check — `D1_EVT_006` (*The Royal Treasury*)
- **Pilihan Attribute Check:** *Pick the warded lockplate and unhook the needle spring* (`DEX Check · DC 13`, `isTrapChoice = true`)
- **Critical Success (Natural 20):** Pegas jarum berhasil dilepas utuh beserta mekanisme penguncinya (`+65 Spoils`, `+2x Iron Scrap [MAT001]`, `+1 Draught`).
- **Success (`Check Result >= 13`):** Silinder kunci terbuka sebelum pegas pelontar jarum menyambar (`+65 Spoils`, `+1x Iron Scrap [MAT001]`).
- **Failure (`Check Result < 13`):** Kunci terbuka namun jarum pelontar menyerempet tangan (`Party menerima 14% Hazard Damage` [dikurangi 50% oleh Halfling *Small and Nimble* atau ditiadakan oleh Relic `R014 Pallbearer's Gloves`], tetap memperoleh `+50 Spoils`).
- **Critical Failure (Natural 1):** Alat pengungkit patah di dalam kunci, memicu rentetan penuh jarum dan membuat kunci macet total (`Party menerima 22% Hazard Damage`, terkena debuff `MOD-018 Fractured Focus [Offensive QTE Window -10%, 1 Battle]`).
- **Opsi Lain yang Tetap Tersedia:** *Take the unlocked chest* (Alternative Solution), *Use coin weights to safely release the lock* (Alternative Solution — 20 Spoils), *Force the locked chest open* (Alternative Risk), *Detect and disarm the chest trap* (Racial Utility — Elf *Keen Senses*), *Leave the treasury* (Safe Exit).

### 13.3 VIT Check — `D1_EVT_011` (*Ashfall Courtyard*)
- **Pilihan Attribute Check:** *Endure the searing cinders and march through the center* (`VIT Check · DC 12`)
- **Critical Success (Natural 20):** Ketahanan fisik party menembus badai abu tanpa goyah dan membangkitkan ketangguhan formasi (`+1x Soul Ash [MAT002]`, `+35 Spoils`, memperoleh buff `MOD-006 Inspiring Formation [Party DR +5%, 2 Battles]`).
- **Success (`Check Result >= 12`):** Party menahan panasnya bara dan mengambil perbekalan di tengah halaman tanpa cedera (`+1x Soul Ash [MAT002]`, `+25 Spoils`).
- **Failure (`Check Result < 12`):** Abu panas menyengat pernapasan saat memungut *Soul Ash* (`Party menerima 12% Hazard Damage`, `+1x Soul Ash [MAT002]`, `+20 Spoils`).
- **Critical Failure (Natural 1):** Badai bara menguras stamina party hingga terbatuk hebat dan melambat (`Party menerima 20% Hazard Damage`, terkena debuff `MOD-020 Heavy Steps [Party Speed -5%, 1 Battle]`, hanya memperoleh `+10 Spoils`).
- **Opsi Lain yang Tetap Tersedia:** *Rush straight through the ashfall* (Alternative Solution — HP/Curse cost), *Soak cloths with a Draught* (Alternative Solution — Consumable), *Detect calm pockets through the ash* (Racial Utility — Elf *Keen Senses*), *Skirt the outer courtyard wall* (Safe Exit).

### 13.4 INT Check — `D1_EVT_015` (*The Three Royal Seals*)
- **Pilihan Attribute Check:** *Decipher the astronomical inscriptions to align the seals* (`INT Check · DC 13`)
- **Critical Success (Natural 20):** Memecahkan urutan segel utama sekaligus membuka kompartemen arkanik rahasia di balik ambang gerbang (`+60 Spoils`, `+1x Soul Ash [MAT002]`, memperoleh buff `MOD-004 Steady Focus [Offensive QTE Window +10%, 2 Battles]`).
- **Success (`Check Result >= 13`):** Ketiga cincin kerajaan berputar tepat tanpa memicu beban penolak (`+45 Spoils`, memperoleh buff `MOD-004 Steady Focus`).
- **Failure (`Check Result < 13`):** Salah perhitungan putaran memicu baut batu sebelum pintu akhirnya terbuka (`Party menerima 10% Hazard Damage`, `+35 Spoils`).
- **Critical Failure (Natural 1):** Segel terkunci rapat dan melepaskan gelombang penolak arkanik yang mengganggu kestabilan mana (`Party menerima 16% Hazard Damage`, terkena debuff `MOD-021 Leaking Mana [Active Skill MP Cost +1, 1 Battle]`).
- **Opsi Lain yang Tetap Tersedia:** *Work out the seal sequence* (Alternative Trial), *Force the stone seals with wedges* (Alternative Solution — 25 Spoils), *Align the ancient mechanism effortlessly* (Racial Utility — Dwarf *Stonecraft*), *Take the side passage* (Safe Exit).

### 13.5 WIS Check — `D1_EVT_001` (*Chapel of Unclaimed Names*)
- **Pilihan Attribute Check:** *Attune your spirit to the nameless prayers* (`WIS Check · DC 12`)
- **Critical Success (Natural 20):** Ketenangan batin memisahkan doa suci dari residu keputusasaan, memulihkan tubuh dan pikiran party (`Party pulih 25% Max HP`, `Party pulih 15% Max MP`, memperoleh buff `MOD-007 Vital Spark [Healing Received +10%, 2 Battles]`).
- **Success (`Check Result >= 12`):** Menarik arus spiritual yang menenangkan dari altar (`Party pulih 15% Max HP`, memperoleh buff `MOD-002 Arcane Current [Elemental Damage +5%, 2 Battles]`).
- **Failure (`Check Result < 12`):** Gema kesedihan altar menolak fokusmu dan menyengat semangat party (`Party kehilangan 10% Max HP`).
- **Critical Failure (Natural 1):** Gelombang keputusasaan roh tanpa nama meremukkan konsentrasi party (`Party kehilangan 15% Max HP`, terkena debuff `MOD-018 Fractured Focus [Offensive QTE Window -10%, 1 Battle]`).
- **Opsi Lain yang Tetap Tersedia:** *Pray for a blessing* (Alternative Solution — 20 Spoils), *Investigate the altar* (Alternative Risk), *Channel sacred radiance* (Racial Utility — Liichtian *Luminescent Insight*), *Leave the chapel* (Safe Exit).

### 13.6 CHA Check — `D1_EVT_004` (*The Last Sentry*)
- **Pilihan Attribute Check:** *Command the sentry's respect by royal mandate* (`CHA Check · DC 12`)
- **Critical Success (Natural 20):** Wibawa kepemimpinanmu membangkitkan sumpah kuno sang penjaga; ia memberi hormat, menyerahkan perbekalan terbaiknya, dan menandai jalur patroli aman (`+35 Spoils`, `+1 Health Potion [CON002]`, `+1x Iron Scrap [MAT001]`, memperoleh buff `MOD-006 Inspiring Formation [Party DR +5%, 2 Battles]`).
- **Success (`Check Result >= 12`):** Penjaga mengakui otoritas ekspedisimu, menurunkan tombaknya, dan membagikan ransum (`+25 Spoils`, `+1 Emergency Ration [CON011]`).
- **Failure (`Check Result < 12`):** Penjaga tidak terkesan dan memukul mundur barisan depan dengan gagang tombaknya (`Party kehilangan 10% Max HP`).
- **Critical Failure (Natural 1):** Penjaga menganggap klaimmu sebagai penghinaan terhadap mahkota dan melancarkan tebasan yang melemahkan party sebelum kalian mundur (`Party kehilangan 18% Max HP`, terkena debuff `MOD-019 Withering Curse [Healing Received -15%, 2 Battles]`).
- **Opsi Lain yang Tetap Tersedia:** *Parley and offer passage coin* (Alternative Solution — 20 Spoils), *Endure its challenge to prove intent* (Alternative Solution — 10% HP Cost), *Intimidate the sentry* (Racial Utility — Dragonborn *Draconic Presence*), *Withdraw before conflict* (Safe Exit).

---

## 14. Open Design Decisions & Resolved Decisions Log

### 14.1 Keputusan Desain yang Telah Disetujui (`FINAL — Locked 23 November 2026`)

| ID | Topik Keputusan | Keputusan Final (`FINAL`) |
|---|---|---|
| **RD-01** | **Penggantian Formula Pasif Social Check** | Formula pasif `10 + CHA Mod + Social Bonuses` digantikan sepenuhnya oleh sistem **D20 Attribute Check (`D20 Roll + Party Highest Attribute Modifier`)** untuk seluruh 6 Ability Scores (`STR`, `DEX`, `VIT`, `INT`, `WIS`, `CHA`). |
| **RD-02** | **Integrasi pada Event Dungeon 1 yang Sudah Ada** | Pilihan D20 Attribute Check ditambahkan sebagai **opsi tambahan** di samping opsi *Racial Utility* dan *Alternative Solutions* yang sudah ada pada event Dungeon 1. |
| **RD-03** | **Perhitungan Anggota Party Aktif/Hidup & Persistensi Kematian Pasca-Battle** | `Party Highest Attribute Modifier` hanya menghitung anggota party yang **aktif/hidup (`HP > 0`)**. Karakter yang mati (`HP == 0`) saat battle **tidak** dikembalikan ke `1 HP` setelah battle selesai, melainkan tetap mati sampai di-revive dengan *Revive Item* atau di Hub Temple. |
| **RD-04** | **Interaksi dengan Mitigasi Pasif Berbasis Tag** | Efek pasif berbasis tag event seperti **Halfling `Small and Nimble` (`-50% hazard damage`)** dan **Relic `R014 Pallbearer's Gloves`** tetap berlaku mengurangi *hazard/trap damage* yang diterima dari hasil `Failure` maupun `Critical Failure` pada Attribute Check. |
| **RD-05** | **Implementasi Kode & UI** | Sistem diimplementasikan penuh ke dalam `Defs.cs`, `StatsSystem.cs`, `RunState.cs`, `Dungeon1EventPool.cs`, dan `EventScreen.cs`. |

### 14.2 Item Tuning Terbuka untuk Tahap Pengembangan Berikutnya (`OPEN / TUNING`)
1. **Kurva Scaling DC untuk Dungeon 2–8 (`RECOMMENDED / TUNING`):** Menentukan apakah rentang DC standar (`Moderate DC 11–13`, `Hard DC 14–16` di Dungeon 1) dinaikkan sebesar `+1` hingga `+3` pada Dungeon 3–8 seiring party mencapai Normal Progression Cap `20` (`Mod +5`).
2. **Distribusi Frekuensi Attribute Check per Dungeon (`OPEN`):** Menetapkan target proporsi event per dungeon untuk masing-masing dari 6 Ability Scores agar keenam atribut memiliki frekuensi kemunculan yang seimbang di seluruh 8 layer *The World's Between*.
