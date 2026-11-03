# GAME DESIGN DOCUMENT (GDD)
# SHOP SYSTEM & RUN ECONOMY

**Party-Based Turn-Based Roguelike RPG**  
*Versi Dokumen: v1.1 | Status: Production Design Reference*  
*Bahasa: Bahasa Indonesia (dengan Standar Terminologi Game Design Internasional)*

---

## 1. Document Overview

### 1.1 Nama Sistem
**Shop System, Hub Facilities & Dual-Context Economy**

### 1.2 Tujuan Dokumen
Dokumen ini disusun sebagai spesifikasi desain resmi (*Game Design Document / GDD*) yang mengatur seluruh aspek ekonomi mata uang (Gold), transaksi di dalam dungeon (*Dungeon Shop*), fasilitas interaktif di pangkalan utama (*Hub Facilities*), serta mekanisme penanganan kematian dan kebangkitan karakter (*Death & Revival System*). Dokumen ini menjadi referensi tunggal dan terpadu bagi:
- **Game Designer & Combat/Economy Balancer:** Memahami batasan parameter, sink/faucet ekonomi, dan kurva risiko vs imbalan (*risk vs reward*).
- **Programmer / Software Engineer:** Mengimplementasikan state machine ekonomi, persistensi data antar-dungeon, validasi transaksi, serta transisi status hidup/mati karakter.
- **UI/UX Designer:** Merancang antarmuka belanja, indikator mata uang ganda, pembeda status karakter, serta alur konfirmasi transaksi yang intuitif.
- **QA / Test Engineer:** Menguji setiap skenario normal (*happy path*), skenario kegagalan (*run failure*), hingga kasus batas (*edge cases*).

### 1.3 Scope (Cakupan Sistem)
- **Sistem Ekonomi Ganda (*Dual-Context Economy*):** Aturan perolehan, penyimpanan, pembelanjaan, transfer 1:1 tanpa potongan saat menang, dan penghapusan total saat run gagal untuk *Run Gold* vs *Hub Gold*.
- **Dungeon Shop:** Toko sementara di dalam penjelajahan dungeon, penyedia kebutuhan bertahan hidup darurat, dan satu-satunya sumber pembelian *Revive Item*.
- **Hub Facilities (5 Fasilitas Utama):**
  1. *Shop* (Penyedia Consumables)
  2. *Armory* (Penyedia Equipment)
  3. *Blacksmith* (Fasilitas Upgrade Equipment)
  4. *Training Ground* (Pengelolaan dan Peningkatan Skill)
  5. *Temple* (Fasilitas Kebangkitan Karakter yang Gugur)
- **Status Kematian Karakter & Ekspedisi:** Kondisi individual *Dead*, keberlanjutan ekspedisi selama minimal 1 karakter hidup, kondisi kegagalan total (*Wipe / Run Failure*), dan persistensi status mati saat kembali ke Hub.
- **Batasan Ruang Lingkup (*Out of Scope*):** Angka spesifik harga item, tabel drop numerik, formula stat upgrade, layout grafis final UI, serta meta-progression di luar aturan yang telah disepakati. Seluruh elemen yang belum disepakati ditandai secara tegas sebagai **TBD** (*To Be Determined*) atau **Open Question**.

### 1.4 Terminologi
| Istilah | Definisi Konseptual & Mekanikal |
|---|---|
| **Dungeon Run (Ekspedisi)** | Satu sesi penjelajahan dungeon yang dimulai dari node awal hingga boss dungeon dikalahkan atau seluruh party gugur. |
| **Hub** | Pangkalan/kota peristirahatan utama tempat pemain mengelola party, perlengkapan, skill, dan pemulihan di antara dua ekspedisi. |
| **Run Gold** | Mata uang Gold yang didapatkan dan dibelanjakan selama dungeon run berjalan. Sifatnya sementara (*ephemeral*) dan terikat pada run aktif. |
| **Hub Gold** | Mata uang Gold permanen yang tersimpan di brankas Hub. Diperoleh dari transfer Run Gold setelah dungeon berhasil diselesaikan. |
| **Dungeon Shop** | Titik interaksi belanja yang muncul di dalam dungeon (melalui node/event) yang menggunakan Run Gold sebagai alat bayar. |
| **Revive Item** | Item langka dan mahal yang berfungsi menghidupkan kembali karakter yang mati saat berada di dalam dungeon; eksklusif hanya dapat dibeli di Dungeon Shop. |
| **Temple** | Fasilitas di Hub tempat pemain menghidupkan kembali karakter yang kembali dalam kondisi mati menggunakan Hub Gold. |
| **Dead (Gugur/Mati)** | Status kondisi karakter di mana HP bernilai 0 dan tidak dapat berpartisipasi dalam pertempuran hingga dihidupkan kembali (*revived*). |
| **Party Wipe (Run Failure)** | Kondisi di mana seluruh 4 karakter dalam party berada dalam status *Dead*. Mengakibatkan ekspedisi gagal seketika. |
| **Sequential Progression** | Alur progresi linier berurutan yang terdiri dari 8 dungeon, diselesaikan dari Dungeon 1 hingga Dungeon 8 secara bertahap. |

---

## 2. System Overview

### 2.1 Gambaran Umum Shop System
Shop System dalam game ini dirancang dengan pendekatan **Dual-Context Roguelike Architecture**. Struktur ekonomi dipisahkan secara tegas menjadi dua pilar:
1. **Ekonomi Taktis Jangka Pendek (Dungeon Run):** Berfokus pada kelangsungan hidup (*survival*), adaptasi terhadap tantangan darurat, mitigasi attrition, serta keputusan berisiko tinggi.
2. **Ekonomi Strategis Jangka Panjang (Hub Preparation):** Berfokus pada penguatan vertikal dan horizontal party (*character progression, equipment enhancement, skill upgrades*) guna menghadapi dungeon-dungeon berikutnya yang semakin mematikan.

```
       [ PEROLEHAN GOLD DI DUNGEON ]
                     │
                     ▼
           ┌───────────────────┐
           │     RUN GOLD      │◄────────────────────────┐
           └─────────┬─────────┘                         │
                     │                                   │
      ┌──────────────┴──────────────┐                    │
      ▼                             ▼                    │
[ DUNGEON SHOP ]              [ RUN BERAKHIR ]           │
 - Consumables                 │                         │
 - Equipment (TBD)             ├───────────────┐         │
 - Revive Item (Rare)          ▼               ▼         │
                       [ BERHASIL ]       [ GAGAL (WIPE) ]
                       Transfer 1:1       Semua Run Gold
                       Tanpa Potongan     HANGUS TOTAL
                               │
                               ▼
                      ┌─────────────────┐
                      │    HUB GOLD     │ (Permanen)
                      └────────┬────────┘
                               │
       ┌───────────────┬───────┴───────┬───────────────┐
       ▼               ▼               ▼               ▼
    [ SHOP ]       [ ARMORY ]    [ BLACKSMITH ]    [ TEMPLE ]
  Consumables      Equipment        Upgrade         Revive
                                       │
                                       ▼
                              [ TRAINING GROUND ]
                               Skill Management
```

### 2.2 Peran Shop System dalam Gameplay
- **Tension & Opportunity Cost:** Setiap Run Gold yang dibelanjakan di Dungeon Shop meningkatkan peluang bertahan hidup party di dungeon saat ini, tetapi secara langsung mengurangi jumlah Hub Gold yang dapat dibawa pulang untuk upgrade permanen di Hub.
- **High-Stakes Decision Making:** Pemain harus menimbang secara cermat apakah akan membeli *Revive Item* yang sangat mahal saat seorang karakter gugur, atau memaksakan melanjutkan dungeon dengan 3 karakter yang tersisa demi mengamankan Gold dalam jumlah besar untuk dibawa ke Hub.
- **Clear Hub Specialization:** Pemain memiliki lokus fasilitas yang terfokus. Setiap fasilitas di Hub memiliki satu peran unik sehingga tidak terjadi tumpang-tindih antarmuka atau kebingungan fungsi.

### 2.3 Hubungan dengan Sistem Lain
- **Hubungan dengan Dungeon Progression (8 Dungeon):** Kampanye berjalan melalui 8 dungeon berurutan. Menyelesaikan Dungeon $N$ memicu transfer Run Gold ke Hub Gold, memungkinkan persiapan di Hub, sebelum party berangkat memasuki Dungeon $N+1$.
- **Hubungan dengan Combat & Party System (4 Karakter):** Party terdiri dari 4 karakter. Kematian parsial (1–3 karakter gugur) mempersempit fleksibilitas taktis pertempuran giliran (*turn-based actions*), menuntut pemanfaatan item pemulihan atau revive.
- **Hubungan dengan Equipment & Inventory System:** Armory dan Blacksmith menjadi muara peredaran senjata dan zirah. Kompatibilitas senjata tetap mengikuti aturan mutlak per-class dari *Equipment System*.
- **Hubungan dengan Class & Skill System:** Fasilitas Training Ground menjadi tempat alokasi dan upgrade skill karakter tanpa melanggar batasan tier dan aturan class yang ditetapkan dalam `Class System.md`.

---

## 3. Economy System (Dual-Currency Model)

Sistem moneter dalam game ini menggunakan satu unit denominasi dasar (**Gold**), tetapi dibedakan secara kaku berdasarkan **Konteks Operasional**:

### 3.1 Run Gold
- **Karakteristik:** Bersifat sementara (*ephemeral*), volatil, dan hanya eksis di dalam memori aktif dungeon run yang sedang berjalan.
- **Tujuan:** Mendanai kebutuhan bertahan hidup instan party selama menjelajahi dungeon.
- **Fasilitas Pembelanjaan:** Hanya dapat dibelanjakan di **Dungeon Shop**.
- **Aturan Hilang (*Loss Condition*):** Jika run berakhir dengan kegagalan (*Party Wipe* / semua karakter mati), **100% Run Gold yang tersisa hangus seketika**. Tidak ada kompensasi, tidak ada persentase sisa yang disimpan.

### 3.2 Hub Gold
- **Karakteristik:** Bersifat permanen (*persistent*), aman, dan tersimpan di dalam save file profil pemain di Hub.
- **Tujuan:** Mendanai progresi jangka panjang party (pembelian perlengkapan, penempaan upgrade, pelatihan skill, dan ritual pembangkitan karakter di kuil).
- **Fasilitas Pembelanjaan:** Dibelanjakan di seluruh fasilitas Hub: *Shop*, *Armory*, *Blacksmith*, *Training Ground*, dan *Temple*.
- **Kekebalan terhadap Kegagalan Run:** Kegagalan pada dungeon run berikutnya **TIDAK PERNAH** mengurangi atau menghapus Hub Gold yang sudah berhasil diamankan sebelumnya.

### 3.3 Gold Acquisition (Perolehan Gold)
- **Sumber Run Gold:**
  - Hadiah kemenangan pertempuran (*Combat Encounter Victory Rewards*).
  - Pembukaan peti harta karun dungeon (*Treasure Chest Nodes/Events*).
  - Hadiah interaksi event acak (*Dungeon Choice Events*).
  - *Catatan:* Formula matematis perolehan dan drop rate numerik berstatus **[TBD]**.
- **Sumber Hub Gold:**
  - Satu-satunya sumber legal penambahan Hub Gold pada baseline sistem adalah **keberhasilan menyelesaikan dungeon run** (*Dungeon Completion Transfer*).
  - *Catatan:* Fitur penjualan equipment di Hub atau quest sampingan di luar dungeon berstatus **[TBD]**.

### 3.4 Aturan Transfer Gold (Dungeon Clear)
1. **Aturan Rasio 1:1 Mutlak [CONFIRMED]:**  
   Saat boss dungeon dikalahkan dan kondisi kemenangan dungeon terpenuhi, seluruh sisa Run Gold yang ada di inventori party ditransfer secara langsung menjadi Hub Gold dengan rasio **1:1**.
2. **Tanpa Potongan Apapun [CONFIRMED]:**  
   Dilarang keras memberlakukan:
   - Pajak kota / bea cukai (*Taxes/Tariffs*).
   - Biaya administrasi transfer (*Transfer Fees*).
   - Penalti kematian parsial (misal: memotong Gold karena ada anggota party yang mati saat clear).
   - Rumus diskon atau konversi pecahan.
   *Contoh:* Pemain mengumpulkan 1.200 Run Gold dan membelanjakan 400 Run Gold di Dungeon Shop. Saat menyelesaikan dungeon, sisa **800 Run Gold** ditambahkan utuh ke saldo Hub Gold.

### 3.5 Perbandingan Atribut Mata Uang
| Parameter | Run Gold | Hub Gold |
|---|---|---|
| **Lokasi Keberadaan** | Dungeon Run (Aktif) | Hub / Base Camp (Save File) |
| **Fasilitas Belanja** | Dungeon Shop | Shop, Armory, Blacksmith, Training Ground, Temple |
| **Dampak Run Gagal** | **Hangus 100% (Hilang Total)** | **Aman 100% (Tidak Berkurang)** |
| **Dampak Run Berhasil** | Dikonversi 100% ke Hub Gold (1:1) | Saldo bertambah sebesar sisa Run Gold |
| **Persistensi Antar-Sesi** | Reset setiap dungeon baru dimulai | Tersimpan permanen dalam Save Data |

---

## 4. Dungeon Shop

### 4.1 Tujuan & Peran Desain
Dungeon Shop berfungsi sebagai oase taktis di tengah ancaman *attrition* (kehabisan HP, MP, dan gugurnya anggota party) selama penjelajahan. Dungeon Shop memberikan pemain ruang bernapas untuk mengevaluasi kondisi party, menukar kelebihan Run Gold dengan stabilitas tempur, atau mengambil risiko menahan Gold demi tabungan di Hub.

### 4.2 Ekonomi & Transaksi
- **Mata Uang:** Seluruh transaksi di Dungeon Shop menggunakan **Run Gold**.
- **Komitmen Pembelian:** Seluruh pembelian bersifat instan dan langsung memotong Run Gold.
- **Penyadaran Pemain (*Player Perception*):** UI Dungeon Shop harus secara eksplisit mengomunikasikan bahwa Gold yang digunakan saat ini adalah Run Gold yang akan dibawa pulang jika selamat, memicu dilema taktis: *"Beli sekarang agar selamat, atau hemat demi modal Hub?"*

### 4.3 Kategori Barang yang Tersedia
Dungeon Shop dapat menyediakan barang-barang yang relevan untuk kebutuhan run langsung:
1. **Consumables:** Potion pemulihan HP, pemulihan MP, dan penawar status debuff darurat.
2. **Equipment (Jika Tersedia):** Senjata atau armor yang dapat langsung dipakai untuk menggantikan perlengkapan yang tertinggal.
3. **Revive Item (Spesial & Eksklusif):** Item pembangkit karakter gugur.

### 4.4 Mekanisme Khusus: Revive Item
- **Eksklusivitas [CONFIRMED]:** Revive Item **HANYA DAPAT DIBELI MELALUI DUNGEON SHOP**. Item ini tidak dijual di Hub Shop.
- **Kelangkaan & Valuasi [CONFIRMED]:** Revive Item diklasifikasikan sebagai barang yang **Sangat Langka (*Rare*)** dan **Sangat Mahal (*Expensive*)**. Harganya harus mencerminkan nilai strategis yang sangat tinggi, yaitu mengembalikan 1 anggota party yang mati ke dalam barisan tempur dungeon saat itu juga.
- **Fungsi Mekanikal [CONFIRMED]:** Mengembalikan karakter berstatus *Dead* menjadi hidup dan siap bertarung kembali pada pertempuran berikutnya dalam run yang sama.
- **Batasan Penggunaan [CONFIRMED]:** Revive Item **DAPAT DIKONSUMSI SAAT PERTEMPURAN BERJALAN (*In-Combat Turn*)** maupun di luar pertarungan (*Map Screen / Inventory*). Saat digunakan in-combat, aksi ini mengonsumsi giliran aksi (*turn action*) karakter yang menggunakannya untuk membangkitkan sekutu yang gugur.
- **Detail TBD:**
  - *Nama spesifik item* (TBD).
  - *Harga numerik pasti* (TBD).
  - *Batas kemunculan stok dalam satu shop* (TBD).
  - *Persentase HP/MP awal saat karakter bangkit di dungeon* (TBD).

### 4.5 Detail Dungeon Shop
| Aspek | Status | Ketetapan / Keterangan Desain |
|---|---|---|
| **Penempatan & Frekuensi** | **CONFIRMED** | Kemunculan node Dungeon Shop bersifat **semi-acak berdasarkan generator peta dungeon (*semi-random procedural map generation*)**, bukan node statis yang selalu tetap. |
| **Struktur Stok Barang** | **TBD** | Berapa jumlah slot barang yang ditampilkan (misal: 4, 6, atau 8 slot barang). |
| **Sistem Restock / Refresh** | **TBD** | Apakah ada fitur membayar Run Gold untuk mengacak ulang (*re-roll*) barang dagangan, dan berapa biayanya. |
| **Penjualan Barang Pemain** | **TBD** | Apakah pemain dapat menjual equipment lama ke Dungeon Shop untuk menambah Run Gold di tengah jalan. |

---

## 5. Hub Shop and Facilities

Hub memiliki **lima fasilitas spesifik** dengan batas fungsi yang terisolasi secara tegas. Seluruh fasilitas di Hub menggunakan **Hub Gold**.

```
┌────────────────────────────────────────────────────────────────────────┐
│                              HUB FACILITIES                            │
├─────────────┬─────────────┬──────────────┬──────────────────┬──────────┤
│    SHOP     │   ARMORY    │  BLACKSMITH  │ TRAINING GROUND  │  TEMPLE  │
│ Consumables │  Equipment  │   Upgrade    │ Skill Management │  Revive  │
└─────────────┴─────────────┴──────────────┴──────────────────┴──────────┘
```

### 5.1 Shop (Consumables Outpost)
- **Fungsi Utama:** Menyediakan barang-barang habis pakai (*consumables*) untuk bekal persiapan sebelum party memulai ekspedisi berikutnya.
- **Peran Ekonomi Hub:** Berfungsi sebagai *gold sink* reguler bervolume rendah-menengah. Memastikan pemain tidak memasuki dungeon baru dengan tangan hampa.
- **Batasan Konten:**
  - Menjual item pemulihan HP, pemulihan MP, dan penawar status abnormal.
  - **TIDAK MENJUAL** Revive Item (eksklusif Dungeon Shop).
- **Aturan Stok [CONFIRMED]:** Barang-barang consumables di Hub Shop memiliki **stok tidak terbatas (*infinite / unlimited stock*)** selama pemain memiliki Hub Gold yang mencukupi untuk membayarnya.
- **Status Parameter [TBD]:** Daftar katalog item consumables dan harga satuan Hub Gold per item berstatus TBD.

### 5.2 Armory (Equipment Merchant)
- **Fungsi Utama:** Menyediakan senjata (*Weapons*), zirah pelindung (*Armors*), dan aksesoris (*Accessories*) siap pakai yang dapat dibeli oleh pemain.
- **Peran Progresi Equipment:** Memberikan opsi akuisisi perlengkapan horizontal dan pemenuhan kebutuhan dasar party jika perlengkapan drop dari dungeon belum memadai untuk kelas tertentu.
- **Kepatuhan Sistem:** Seluruh perlengkapan yang dijual wajib mematuhi aturan ketat kompatibilitas senjata per-class yang diatur dalam `Equipment System.md` (misal: Greatsword hanya untuk Fighter/Barbarian, Dagger untuk Rogue, dsb).
- **Status Parameter [TBD]:** Roster rotasi barang, tier equipment yang terbuka berdasarkan progres 8 dungeon, harga beli, serta sistem rarity berstatus TBD.

### 5.3 Blacksmith (Equipment Forge & Refinement)
- **Fungsi Utama:** Menyediakan layanan peningkatan mutu (*upgrade/enhancement*) terhadap equipment yang telah dimiliki oleh pemain.
- **Peran Progresi Equipment:** Menjadi jalur progresi vertikal utama. Memungkinkan senjata atau armor favorit pemain tetap relevan terhadap peningkatan kurva kesulitan monster di dungeon-dungeon tingkat lanjut.
- **Mekanika Layanan:** Mengonsumsi Hub Gold (dan potensi material tempa jika disepakati) untuk meningkatkan atribut dasar equipment (seperti Physical/Magical ATK, Defense, atau substat).
- **Status Parameter [TBD]:** Batas maksimum level upgrade ($+1, +2, \dots$), biaya Hub Gold per level, kebutuhan material sampingan, serta formula kenaikan stat berstatus TBD.

### 5.4 Training Ground (Skill Academy & Management)
- **Fungsi Utama:** Tempat pengelolaan dan peningkatan kapabilitas kemampuan tempur karakter (*Class Skills*).
- **Peran Progresi Karakter:**
  - Mengelola loadout skill aktif (3 slot) dan pasif (3 slot) karakter di luar dungeon.
  - Memfasilitasi pembelanjaan sumber daya untuk membuka atau meningkatkan rank skill (*Skill Upgrades*).
- **Kepatuhan Sistem:** Wajib patuh secara absolut pada arsitektur 5-Tier Skill dan aturan Class dari `Class System.md` dan `GDD_Skill_System_v1.0.md`. Fasilitas ini tidak boleh merombak atau menambah slot skill sembarangan.
- **Status Parameter [TBD]:** Persyaratan biaya Hub Gold vs Skill Points (SP), aturan refund/respec, dan batasan level unlock berstatus TBD.

### 5.5 Temple (Sanctuary of Resurrection)
- **Fungsi Utama:** Menghidupkan kembali (*revive*) karakter party yang gugur dan berhasil dibawa pulang ke Hub dalam kondisi mati.
- **Peran Sistem Pemulihan:** Merupakan fasilitas penyelamat party antar-run. Jika pemain berhasil menyelesaikan dungeon namun ada 1–3 karakter yang mati di dalam pertempuran terakhir, Temple menjadi satu-satunya tempat di Hub untuk memulihkan karakter tersebut agar dapat bertarung kembali di run berikutnya.
- **Status Parameter [TBD]:** Biaya Hub Gold per karakter yang di-revive, apakah biaya scaling berdasarkan level karakter, serta ada/tidaknya penalti kelelahan berstatus TBD.

### 5.6 Matriks Perbandingan Fasilitas Hub
| Fasilitas | Objek Interaksi | Sumber Daya yang Dikonsumsi | Dampak Terhadap Party |
|---|---|---|---|
| **Shop** | Inventory Item | Hub Gold | Stok persediaan consumables untuk run berikutnya bertambah |
| **Armory** | Inventory Equipment | Hub Gold | Menambah variasi perlengkapan baru yang kompatibel |
| **Blacksmith** | Senjata & Armor Milik | Hub Gold (+ Material TBD) | Meningkatkan angka stat pada equipment yang sudah ada |
| **Training Ground** | Skill Tree & Loadout | Hub Gold / SP (TBD) | Membuka variasi build dan meningkatkan rank skill |
| **Temple** | Karakter Mati (*Dead*) | Hub Gold | Mengembalikan status karakter menjadi hidup (*Alive*) |

---

## 6. Character Death and Revival

Sistem penanganan kematian karakter mengusung konsep **Individual Permadeath Immunity with Collective Run Failure**:

### 6.1 Kematian Individual Karakter [CONFIRMED]
- Karakter dinyatakan mati (*Dead*) ketika HP-nya mencapai angka 0 dalam pertarungan.
- Karakter yang berstatus *Dead* tidak dapat menerima giliran aksi, tidak dapat menjadi target buff normal, dan tidak dapat bertarung.
- **Aturan Ekspedisi Berjalan:** Kematian satu atau beberapa karakter **TIDAK MENGAKHIBATKAN RUN BERAKHIR**, selama setidaknya **1 (satu) karakter party masih hidup**.
- Sisa anggota party yang masih hidup berhak melanjutkan penjelajahan node dungeon berikutnya dan memasuki combat dengan formasi yang berkurang.

### 6.2 Kondisi Kegagalan Run (Party Wipe) [CONFIRMED]
- Ekspedisi dinyatakan **GAGAL TOTAL (*Run Failure*)** hanya jika dan hanya jika **seluruh karakter dalam party (ke-4 karakter) berstatus *Dead***.
- Ketika kondisi ini terjadi:
  1. Dungeon run berhenti seketika.
  2. Seluruh Run Gold yang sedang dipegang **hangus 100%**.
  3. Pemain diarahkan kembali ke Main Menu (sesuai baseline GDD: run direset dan membutuhkan *New Game*).
  4. Seluruh Hub Gold yang sebelumnya telah tersimpan di Hub **tetap aman**.

### 6.3 Kondisi Karakter Saat Dungeon Berhasil Diselesaikan [CONFIRMED]
- Jika pemain berhasil mengalahkan boss dungeon dan menyelesaikan ekspedisi sementara terdapat 1, 2, atau 3 karakter yang mati:
  - Ekspedisi tetap dinilai sebagai **SUKSES**.
  - Seluruh sisa Run Gold ditransfer utuh ke Hub Gold (1:1).
  - **Karakter yang mati TETAP DALAM KONDISI MATI (*Dead*) saat tiba di Hub.** Tidak ada pemulihan otomatis gratis pasca-dungeon untuk karakter yang tewas.
  - Karakter tersebut tidak dapat dimasukkan ke dalam ekspedisi berikutnya sebelum dihidupkan kembali melalui **Temple**.

### 6.4 Jalur Kebangkitan Karakter (Revival Pathways)
| Parameter | Revive di Dungeon | Revive di Hub |
|---|---|---|
| **Lokasi Akses** | Di tengah dungeon run aktif | Di pangkalan Hub |
| **Metode Pelaksanaan** | Menggunakan *Revive Item* dari inventori (bisa saat turn combat maupun di luar combat) | Menggunakan layanan fasilitas *Temple* |
| **Sumber Akuisisi** | Dibeli eksklusif di *Dungeon Shop* | Tersedia permanen sebagai fasilitas kota |
| **Mata Uang Digunakan** | Run Gold | Hub Gold |
| **Urgensi Taktis** | Kritis: Menyelamatkan run dari ancaman *wipe* secara instan | Strategis: Menyiapkan komposisi 4 karakter untuk run berikutnya |
| **Biaya & Kelangkaan** | Sangat Langka (*Rare*) & Sangat Mahal | Terjadwal, biaya berbasis Hub Gold (TBD) |

---

## 7. Player Flow & Game Loops

Berikut adalah alur lengkap pemain (*User Journey*) yang mengintegrasikan transaksi ekonomi, keputusan belanja, dan status karakter:

```
[ START / HUB ]
       │
       ▼
[ HUB PREPARATION ]
 ├─ Pergi ke SHOP ──────────────► Beli Consumables (Hub Gold, Stok Tak Terbatas)
 ├─ Pergi ke ARMORY ────────────► Beli Equipment Baru (Hub Gold)
 ├─ Pergi ke BLACKSMITH ────────► Upgrade Equipment (Hub Gold)
 ├─ Pergi ke TRAINING GROUND ───► Atur & Upgrade Skill (Hub Gold / SP)
 └─ Pergi ke TEMPLE ────────────► Revive Karakter Mati (Hub Gold)
       │
       ▼
[ MEMASUKI DUNGEON ] ───────────► Inisialisasi: Run Gold = 0 (atau initial run balance)
       │
       ▼
[ PENJELAJAHAN NODE DUNGEON ]
 ├─ Menang Combat / Chest ──────► Mendapatkan RUN GOLD
 ├─ Karakter Terluka / Mati ────► Evaluasi Kondisi Party
 │
 ├─ Menemukan DUNGEON SHOP (Semi-Acak):
 │   ├─ Tinjau Barang & Run Gold
 │   ├─ Beli Consumables / Potions
 │   ├─ Beli REVIVE ITEM (Jika ada anggota mati & Run Gold cukup)
 │   └─ Selesai Belanja / Lewati Shop
 │
 └─ Pertempuran Berlanjut:
     │
     ├─► KASUS A: SELURUH PARTY MATI SEBELUM BOSS KALAH (4 Dead)
     │    │
     │    ▼
     │   [ RUN FAILURE ]
     │    ├─ Seluruh sisa Run Gold HANGUS
     │    ├─ Kembali ke Main Menu / Ekspedisi Reset
     │    └─ Hub Gold sebelumnya TETAP AMAN
     │
     └─► KASUS B: BOSS KALAH (Minimal 1 Hidup ATAU Mutual Kill)
          │
          ▼
         [ DUNGEON VICTORY ]
          ├─ Seluruh sisa Run Gold ditransfer ke Hub Gold (1:1 tanpa potongan)
          ├─ Karakter hidup kembali dengan selamat
          ├─ Karakter mati TETAP MATI saat tiba di Hub (Revive di Temple)
          └─ Kembali ke [ HUB PREPARATION ] untuk ekspedisi berikutnya!
```

---

## 8. System Rules and Edge Cases

Untuk menghindari kesalahan penafsiran pada tahap pemrograman dan QA, aturan-aturan logika berikut didefinisikan secara tegas:

### 8.1 Transaksi & Inventori
- **Rule 8.1.1 (Dana Kurang):** Pemain tidak dapat melakukan pembelian barang atau layanan jika jumlah Gold (Run Gold di dungeon, atau Hub Gold di Hub) bernilai lebih kecil dari harga barang. Transaksi ditolak dengan pesan error yang jelas.
- **Rule 8.1.2 (Inventori Penuh [TBD]):** Jika sistem inventori menerapkan pembatasan slot, sistem wajib memvalidasi ketersediaan slot sebelum memotong Gold. Jika inventori penuh, transaksi dibatalkan tanpa pengurangan Gold.
- **Rule 8.1.3 (Sifat Pembelian Final):** Seluruh transaksi pembelian consumables di Dungeon Shop bersifat final dan tidak dapat di-*undo* (*no refund*).

### 8.2 Logika Kematian & Ekspedisi
- **Rule 8.2.1 (Party Status Check Post-Battle):** Di setiap akhir giliran pertempuran (*combat resolution*), sistem memeriksa jumlah karakter yang memiliki status `Alive` (`HP > 0`).
  - Jika `AliveCount >= 1`: Pertempuran/ekspedisi berlanjut normal.
  - Jika `AliveCount == 0`: Picu event `OnRunFailed()`, KECUALI jika Boss dungeon tewas pada giliran yang sama (lihat Rule 8.3.3).
- **Rule 8.2.2 (Sisa Run Gold saat Wipe):** Saat `OnRunFailed()` dipanggil, variabel `ActiveRunGold` di-set menjadi 0 sebelum transisi layar kegagalan dijalankan.
- **Rule 8.2.3 (Transfer Sisa Run Gold saat Clear):** Saat `OnDungeonCompleted()` dipanggil:
  $$\text{HubGold}_{\text{baru}} = \text{HubGold}_{\text{lama}} + \text{ActiveRunGold}$$
  $$\text{ActiveRunGold} = 0$$
  Tidak ada faktor pengali ($\times$) atau pembagi ($\div$) yang diterapkan.

### 8.3 Edge Cases (Kasus Batas Khusus)
1. **Edge Case 1: Menang Dungeon dengan 1 Karakter Hidup dan 3 Karakter Mati.**
   - *Status:* **Tervalidasi & Sah**.
   - *Penanganan:* Dungeon dinyatakan selesai secara sukses. Seluruh sisa Run Gold masuk utuh ke Hub Gold. Ketiga karakter yang gugur tetap berada dalam kondisi `Dead` di profil party Hub. Pemain wajib ke Temple untuk menghidupkan mereka sebelum dapat digunakan kembali.
2. **Edge Case 2: Membeli Revive Item tetapi Tidak Ada Karakter yang Mati.**
   - *Status:* **Diizinkan (Sebagai Persediaan)**.
   - *Penanganan:* Revive Item masuk ke dalam inventori party selama run, siap digunakan kapanpun seorang anggota party tewas di pertempuran berikutnya (baik saat giliran tempur maupun di luar tempur).
3. **Edge Case 3: Seluruh Party Gugur Bersamaan pada Giliran Terakhir Mengalahkan Boss (Mutual Kill).**
   - *Status:* **Menang (Dungeon Clear) [CONFIRMED]**.
   - *Penanganan:* Pertempuran dinilai sebagai **Menang (Clear)**. Kematian boss diutamakan sebagai pencapaian tujuan ekspedisi. Seluruh sisa Run Gold ditransfer secara utuh ke Hub Gold (1:1 tanpa potongan). Seluruh 4 karakter kembali ke Hub dalam kondisi `Dead`. Di Hub, pemain wajib menghidupkan minimal 1 karakter di Temple sebelum dapat memulai ekspedisi baru.
4. **Edge Case 4: Pemain Membawa 0 Run Gold ke Dungeon Shop.**
   - *Status:* **Tervalidasi**.
   - *Penanganan:* UI toko tetap terbuka normal, seluruh tombol pembelian berstatus dinonaktifkan (*disabled/greyed out*), pemain dapat keluar dari toko tanpa dikenakan sanksi apapun.

---

## 9. UI/UX Considerations & Guidelines

Meskipun layout visual artistik bersifat TBD, panduan interaksi fungsional berikut harus dipatuhi:

### 9.1 Pembeda Visual Run Gold vs Hub Gold
Untuk mengeliminasi ambiguitas pemain mengenai jenis uang yang sedang mereka gunakan:
- **Label dan Ikon yang Berbeda:**
  - **Run Gold:** Ditampilkan dengan label `RUN GOLD` atau `EXPEDITION GOLD`, didampingi ikon kantong koin dengan penanda penjelajahan. Terdapat tooltip: *"Gold sementara yang diperoleh selama ekspedisi. Akan hilang jika seluruh party gugur."*
  - **Hub Gold:** Ditampilkan dengan label `HUB GOLD` atau `TREASURY`, didampingi ikon koin emas berkilau / brankas. Terdapat tooltip: *"Gold permanen kota. Aman dan dapat digunakan untuk seluruh fasilitas Hub."*
- **Penempatan Layar:**
  - Di dalam dungeon dan Dungeon Shop, nilai yang ditampilkan secara dominan adalah **Run Gold**.
  - Di dalam Hub, nilai yang ditampilkan di bar status utama adalah **Hub Gold**.

### 9.2 Indikator Status Karakter di Toko dan Layanan
- **Indikator Hidup / Gugur:**
  - Karakter yang hidup menampilkan bilah HP dan MP normal.
  - Karakter yang gugur menampilkan badge visual bertuliskan **DEAD / FALLEN**, foto potret karakter berwarna monokrom/abu-abu (*greyscale*), dan HP bernilai `0 / MaxHP`.
- **Targeting Item / Layanan:**
  - Pada Dungeon Shop saat menggunakan *Revive Item*, hanya karakter dengan status `Dead` yang dapat dipilih sebagai target.
  - Pada fasilitas *Temple* di Hub, karakter yang masih hidup berstatus *unavailable* untuk dipilih, dan hanya karakter `Dead` yang memiliki tombol `Revive`.

### 9.3 Feedback Transaksi
- **Transaksi Berhasil:**
  - Animasi singkat pemotongan saldo Gold.
  - SFX denting koin emas yang memuaskan.
  - Notifikasi teks mengambang (*toast message*): *"Item berhasil dibeli!"* atau *"Karakter berhasil dihidupkan!"*.
- **Transaksi Ditolak (Gagal):**
  - Efek goyang visual (*shake animation*) pada label Gold.
  - SFX nada rendah peringatan (*error buzzer*).
  - Teks deskripsi alasan penolakan (misal: *"Run Gold tidak mencukupi!"* atau *"Slot inventori penuh!"*).

---

## 10. Balancing Parameters (Matrix Parameter)

Seluruh angka di bawah ini sengaja ditulis sebagai **TBD** sesuai instruksi sistem agar tim balancing dapat mengisi data berdasarkan pengujian playtest:

| Parameter Kunci | Lokasi Sistem | Deskripsi Parameter | Nilai Awal |
|---|---|---|:---:|
| `RUN_GOLD_BASE_DROP_MIN` | Combat/Node | Jumlah minimum Run Gold dari kemenangan pertarungan normal | **TBD** |
| `RUN_GOLD_BASE_DROP_MAX` | Combat/Node | Jumlah maksimum Run Gold dari kemenangan pertarungan normal | **TBD** |
| `DUNGEON_SHOP_REVIVE_COST` | Dungeon Shop | Harga beli Revive Item di dalam dungeon (Run Gold) | **TBD** |
| `DUNGEON_SHOP_REVIVE_WEIGHT`| Dungeon Shop | Peluang kemunculan Revive Item di stok Dungeon Shop | **TBD** |
| `REVIVE_ITEM_HP_RESTORE_PCT`| Consumable | Persentase HP yang dipulihkan saat karakter di-revive di dungeon | **TBD** |
| `REVIVE_ITEM_MP_RESTORE_PCT`| Consumable | Persentase MP yang dipulihkan saat karakter di-revive di dungeon | **TBD** |
| `TEMPLE_REVIVE_BASE_COST` | Temple (Hub) | Biaya dasar Hub Gold untuk menghidupkan 1 karakter di Hub | **TBD** |
| `TEMPLE_REVIVE_LEVEL_SCALE` | Temple (Hub) | Faktor pengali biaya revive berdasarkan level karakter (jika ada) | **TBD** |
| `BLACKSMITH_UPGRADE_COST_L1`| Blacksmith | Biaya Hub Gold untuk meningkatkan equipment ke tingkat dasar | **TBD** |
| `SHOP_POTION_HP_PRICE` | Shop (Hub) | Harga Hub Gold untuk Potion HP standar di Hub Shop | **TBD** |
| `SHOP_POTION_MP_PRICE` | Shop (Hub) | Harga Hub Gold untuk Potion MP standar di Hub Shop | **TBD** |
| `ARMORY_TIER1_WEAPON_PRICE` | Armory (Hub) | Harga rata-rata senjata tingkat dasar di Armory Hub | **TBD** |

---

## 11. Open Questions & Resolved Decisions

### 11.1 Keputusan yang Telah Disepakati (Resolved Decisions)
Berdasarkan konfirmasi terkini:
1. **Resolusi "Mutual Kill" pada Boss:**  
   *Keputusan:* **Menang (Dungeon Clear)**. Jika karakter terakhir party gugur bersamaan dengan tewasnya Boss (misal akibat DoT atau serangan balik), pertempuran dinyatakan **Menang**. Seluruh sisa Run Gold ditransfer 1:1 ke Hub Gold. Ke-4 karakter kembali ke Hub dalam kondisi *Dead*, dan pemain harus me-revive setidaknya 1 karakter di Temple sebelum memulai ekspedisi baru.
2. **Frekuensi & Penempatan Node Dungeon Shop:**  
   *Keputusan:* **Semi-Acak Berdasarkan Procedural Map Generator**. Node toko tidak selalu berada pada posisi statis tetap, melainkan dihasilkan secara semi-acak oleh generator peta dungeon dengan parameter bobot kemunculan tertentu.
3. **Mekanika Penggunaan Revive Item (Combat vs Non-Combat):**  
   *Keputusan:* **Dapat digunakan di dalam pertempuran (*In-Combat Turn*) maupun di luar pertempuran**. Penggunaan in-combat mengonsumsi 1 giliran aksi karakter yang memakainya untuk membangkitkan sekutu yang gugur.
4. **Batasan Pembelian / Kuota Stok di Hub Shop:**  
   *Keputusan:* **Stok Tidak Terbatas (*Unlimited Supply*)**. Seluruh barang consumables di Hub Shop dapat dibeli tanpa batasan stok selama pemain memiliki Hub Gold yang mencukupi.

### 11.2 Open Questions (Menunggu Keputusan Lanjutan)
Berikut adalah pertanyaan desain yang masih memerlukan kesepakatan formal lebih lanjut oleh tim pengembang:

#### Open Question 1: Persistensi Hub Gold saat New Game Pasca-Wipe
- **Pertanyaan:** Dokumen menetapkan bahwa Hub Gold aman dari kegagalan run biasa. Namun, jika game menerapkan *full permadeath roguelike* di mana New Game menghapus seluruh data karakter, apakah Hub Gold direset total atau tetap bertahan sebagai mata uang meta-progression akun?
- **Dampak Desain & Balancing:** Menentukan apakah game ini condong ke arah *hardcore roguelike* murni (Hub Gold ikut hilang saat New Game) atau *rogue-lite* dengan warisan brankas kota (*legacy treasury* yang dipertahankan antar-file/run).

#### Open Question 2: Batas Rentang Kemunculan Node Dungeon Shop per Dungeon
- **Pertanyaan:** Karena penempatan Dungeon Shop telah disepakati semi-acak, berapa batas minimum dan maksimum kemunculan node Dungeon Shop per dungeon (misal: dijamin muncul minimal 1 kali per dungeon, atau dibatasi maksimal 2 kali)?
- **Dampak Desain & Balancing:** Menghindari situasi ekstrem di mana pemain sama sekali tidak menemukan toko dalam satu dungeon, atau sebaliknya menemukan toko terlalu sering yang merusak ekonomi Run Gold.

---

## 12. Summary of Confirmed Rules (Aturan Baku Proyek)

Sebagai panduan cepat bagi seluruh tim, berikut adalah ringkasan aturan yang **telah disepakati dan tidak boleh diubah**:

1. **Dua Jenis Gold Berbeda:** *Run Gold* (hanya di dungeon, sementara) dan *Hub Gold* (di Hub, permanen).
2. **Transfer 1:1 Tanpa Potongan:** Saat dungeon berhasil diselesaikan, seluruh sisa Run Gold masuk ke saldo Hub Gold dengan rasio **1:1**. Tidak ada pajak, potongan, atau biaya konversi apapun.
3. **Hangus Total saat Gagal:** Jika ekspedisi gagal (*wipe*), **seluruh sisa Run Gold hilang**. Hub Gold yang didapatkan dari ekspedisi sebelumnya **tidak terpengaruh**.
4. **Kematian Individual Tidak Mengakhiri Run:** Ekspedisi tetap berlanjut selama minimal **1 karakter party masih hidup**.
5. **Kegagalan Run Total:** Ekspedisi hanya gagal jika **keempat karakter party mati** sebelum dungeon clear terpenuhi. Pemain kembali ke Main Menu dan membutuhkan New Game.
6. **Resolusi Mutual Kill pada Boss:** Jika karakter terakhir pemain gugur bersamaan dengan tewasnya Boss, pertempuran dinilai **Menang (Clear)**. Sisa Run Gold tetap ditransfer 1:1 ke Hub Gold, dan party kembali ke Hub dalam kondisi *Dead*.
7. **Persistensi Kondisi Gugur:** Karakter yang mati di dungeon dan berhasil dibawa keluar (karena dungeon selesai) **tetap dalam kondisi mati saat tiba di Hub**.
8. **Eksklusivitas Revive Item:** Revive Item **hanya dapat dibeli di Dungeon Shop**. Item ini berstatus **Langka (*Rare*)** dan **Mahal (*Expensive*)**.
9. **Fleksibilitas Penggunaan Revive Item:** Revive Item **dapat dikonsumsi saat giliran tempur (*in-combat turn*)** maupun di luar pertarungan.
10. **Frekuensi Dungeon Shop:** Kemunculan node Dungeon Shop bersifat **semi-acak berdasarkan generator peta (*semi-random procedural map generation*)**.
11. **Kebangkitan di Hub:** Karakter yang mati dapat dihidupkan kembali di Hub melalui fasilitas **Temple**.
12. **Spesialisasi 5 Fasilitas Hub:**
    - **Shop:** Menjual Consumables dengan **stok tidak terbatas** selama Hub Gold mencukupi.
    - **Armory:** Menjual Equipment.
    - **Blacksmith:** Layanan Upgrade Equipment.
    - **Training Ground:** Pengelolaan dan Peningkatan Skill (mematuhi `Class System.md`).
    - **Temple:** Menghidupkan kembali karakter mati.
13. **Integritas Sistem:** Dilarang mengarang formula biaya, daftar harga numerik, atau aturan spesifik tanpa persetujuan formal; seluruhnya wajib ditandai sebagai **TBD**.

---
*Dokumen ini sah sebagai acuan desain sistem ekonomi dan shop per tanggal pembaruan.*
