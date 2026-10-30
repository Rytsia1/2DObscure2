# GAME DESIGN DOCUMENT 

#### **Party-Based Turn-Based Roguelike RPG** 

_Draft v0.1 | 30 Oktober 2026_ 

Dokumen desain awal berdasarkan keputusan yang telah dibahas. Bagian yang belum ditetapkan ditandai TBD; angka skill dan balancing masih berupa konsep. 

|**Dokumen**|**Cakupan**|
|---|---|
|**GDD Draft v0.1**|Core loop, party, progression, combat, class, damage,|
||equipment, shop, audio, UI, dan open questions<br>i  i|
|**Status**|Working draft; bukan spesifikasi implementasi final|
|**Prinsip**|Mencatat keputusan yang sudah ada dan tidak menganggap<br>i|
||usulanyangbelum disetujui sebagai aturan final|



GDD Draft v0.1 — Project Tugas GenAI 

## **Daftar Isi** 

- 1. High Concept dan Pilar Desain 

- 2. Core Loop dan Struktur Dungeon 

- 3. Party, Character, Attributes, Race 

- 4. Class dan Skill System 

- 5. Combat dan QTE 

- 6. Damage Type, Resistance, Calculation 

- 7. Equipment 

- 8. Shop dan Run Economy 

- 9. Run Failure / Victory 

- 10. Audio Direction 

- 11. UI/UX dan Accessibility 

- 12. Scope dan Open Questions 

GDD Draft v0.1 — Project Tugas GenAI 

## **1. High Concept** 

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



### **1.1 Pilar Desain** 

- Combat taktis dengan pilihan aksi dan target yang bermakna. 

- Identitas class kuat dengan build yang dibentuk lewat skill, Rank, dan equipment. 

- Keputusan roguelike dari node, shop acak, dan sumber daya run-based. 

- Risiko run nyata: kekalahan mengakhiri ekspedisi. 

- Feedback combat yang jelas untuk damage type, status, resistance, dan QTE. 

GDD Draft v0.1 — Project Tugas GenAI 

## **2. Core Loop dan Struktur Dungeon** 

|**Tahap**|**Aktivitas**|
|---|---|
|**New Game**|Mulai ekspedisi baru.|
|**Persiapan**|Party/loadout/equipment; lokasi dan detail preparation masih<br>TBD.|
|**Dungeon Map**|Pilih node untuk maju.<br>i|
|**Node**|Combat, event, shop, reward atau tipe node lain; daftar final<br>TBD.|
|**Progression**|EXP/SP, Gold, item; reward table TBD.|
|**Dungeon Clear**|Pertahankan progres run dan lanjut ke dungeon berikutnya.|
|**Final Clear**|Menyelesaikan dungeon ke-8 dan run.|
|**Run Failure**|Run dihapus,kembali ke Main Menu;New Game diperlukan.|



### **2.1 Sequential Progression** 

Delapan dungeon diselesaikan dalam urutan yang telah ditentukan. Tidak ada pemilihan urutan dungeon. Tema, layout, node count, encounter, boss, dan reward per dungeon masih TBD. 

### **2.2 Gold dan persistensi antar dungeon** 

- Gold yang tersisa setelah menyelesaikan dungeon tetap tersedia selama run berlanjut. 

- Gold dihapus ketika run berakhir. 

- Detail persistensi level, SP, equipment, consumables, dan status party antar dungeon perlu dikonfirmasi; asumsi kerja: party berlanjut dalam run sukses yang sama. 

GDD Draft v0.1 — Project Tugas GenAI 

## **3. Party, Character, Attributes, Race** 

### **3.1 Resources dan Level** 

|**Sistem**|**Aturan / draft**|
|---|---|
|**HP**|Kelangsungan karakter; status HP 0 dan recovery rules perlu<br>i|
||finalisasi.|
|**MP**|Dibayar untuk Active Skills.|
|**Resource class**|Tidak ada resource pool khusus class; hanya HP dan MP.|
|**Level**|Maksimum 15.|
|**Point Buy**|Stat allocation menggunakan Point Buy;nilai dan batas TBD.|



### **3.2 Attributes** 

|**Attribute**|**Draft fungsi**<br>i|
|---|---|
|**STR**|Kekuatan fisik / Physical Damage.|
|**DEX**|Akurasi, evasion, dan/atau ranged performance; formula TBD.|
|**VIT**|HP dan ketahanan.|
|**INT**|Magic/Elemental Damage.<br>i|
|**WIS**|Healing, resistance, atau magic support; final TBD.<br>i|
|**CHA**|Efek tertentu/support/status;final TBD.|



### **3.3 Race System** 

Race memberikan Ability Bonus, Feat, dan Utility. Tidak ada passive trait terpisah. 

|**Race**|**Catatan**|
|---|---|
|**Human**|Bonus/Feat/Utility detail TBD.|
|**Elf**|Detail TBD.|
|**Dwarf**|Detail TBD.|
|**Halfling**|Detail TBD.|
|**l**<br>**Dragonborn**|Detail TBD.|
|**Liichtian**|Original celestial heritage; detail TBD.|
|**Merfolk**|Detail TBD.|



GDD Draft v0.1 — Project Tugas GenAI 

## **4. Class dan Skill System** 

Sembilan class dirancang dengan core mechanic berbeda. Loadout: 3 Active Skill dan 3 Passive Skill. Skill tree memiliki lima Tier yang terbuka pada Level 1, 4, 7, 10, dan 13. Skill normal memiliki Rank 1–3 dan menggunakan SP untuk unlock/upgrade. 

|**Class**|**Core mechanic**|**Identity**|
|---|---|---|
|**Fighter**|Weapon Techniques|Teknik senjata, offense, defense, taktik.<br>i|
|**Barbarian**|Berserk State|Agresi fisik dan daya tahan. Berserk<br>adalah Active Skill khusus, efek penuh<br>saat didapat dan tidak di-upgrade.|
|**Martial Artist**|Combo Flow|Unarmed combat, combo berbasis QTE<br>dan finisher.|
|**Rogue**|Exploit|Memanfaatkan kondisi target; bukan<br>core Mark.|
|**Ranger**|Hunt Mark|Ranged hunter; Mark meningkatkan<br>damage Ranger sendiri saja.|
|**Cleric**|Divine Protection|Hybrid healer: Healing, Protection, Holy<br>Offense.|
|**Wizard**|Elemental Specialization|Spell beragam elemen; tidak memilih<br>elemen permanen.|
|**Warlock**|Curse Mastery|Curse independen; beberapa Curse<br>berbeda dapat aktif bersamaan, tanpa<br>kombinasi otomatis.|
|**Druid**|Nature’s Control|Hybrid Nature Damage, control, dan<br>Healing.|



### **4.1 Skill tree references** 

Daftar skill setiap Tier disusun dalam dokumen pendamping “Class Skill Compendium Draft”. Nama dan efek skill adalah rancangan; coefficients, MP Cost, durasi, Rank scaling, dan balancing belum final. 

### **4.2 SP Progression** 

Model SP hybrid: SP dari level-up dan milestone/expedition events. Variable SP per level telah dipilih sebagai arah, tetapi tabel SP definitif dan milestone reward belum ditetapkan. 

### **4.3 Loadout** 

Loadout diubah di Hub/Camp. Detail akses Hub/Camp selama run dan batas perubahan loadout masih TBD. 

GDD Draft v0.1 — Project Tugas GenAI 

## **5. Combat dan QTE** 

### **5.1 Combat Structure** 

- Combat berbasis giliran dengan aksi per karakter. 

- Pemain memilih aksi/skill dan target; skill kemudian diselesaikan melalui aturan hit, damage, healing, status, dan QTE yang relevan. 

- Enemy turn order, initiative formula, action economy, dan defend/item rules masih TBD. 

- Beberapa serangan musuh dapat memiliki telegraph/charge; interrupt/counter rules perlu dirinci. 

### **5.2 QTE** 

|**Aspek**|**Keputusan / draft**|
|---|---|
|**Peran**<br>**f**|Reaction-based input memperkaya turn-based action.|
|**Difficulty**|Musuh lebih sulit dapat memiliki QTE lebih sulit.|
|**Performance**|QTE memengaruhi hasil serangan; tidak menjamin hit.|
|**Grades**|Success/Perfect digunakan dalam konsep Martial Artist; grade<br>set final TBD.|
|**Failure**|Penalti berbeda per action masih TBD.|
|**Accessibility**|Timing adjustment, cue alternatif, dan opsi accessibility perlu<br>dirancang.|



### **5.3 Broken** 

Broken digunakan oleh beberapa class mechanic (contoh: reset combo Martial Artist). Trigger global, durasi, recovery, dan efek terhadap target masih TBD. 

GDD Draft v0.1 — Project Tugas GenAI 

## **6. Damage Type, Resistance, dan Calculation** 

### **6.1 Damage Types** 

|**Jenis**|**Deskripsi**<br>ii|
|---|---|
|**Physical Damage**|Damage dari serangan fisik, senjata, dan skill fisik.|
|**Elemental Damage**|Kategori utama damage spell; menggantikan Magical Damage<br>sebagai kategori damage.|
|**Elemen contoh**|Fire, Ice, Lightning, Arcane, Dark, Holy, Nature. Taxonomy<br>final,termasuk Arcane classification, perlu konfirmasi.|



Model Elemental Damage B dipilih: Physical Damage dan Elemental Damage merupakan kategori terpisah. Istilah Magic dapat tetap dipakai sebagai tag spell, akurasi, atau scaling atribut jika disepakati, tetapi bukan kategori damage utama. 

### **6.2 Resistance** 

Physical Resistance dan Elemental Resistance mengurangi damage yang sesuai. Cap, negative resistance/vulnerability, stacking, dan tampilan UI masih TBD. 

### **6.3 Formula kerja** 

#### **Final Damage = Base Skill Damage × Attribute/Skill Scaling × Damage Bonuses × (1 − Target Resistance) × QTE Modifier** 

Formula di atas hanya kerangka. Order of operations, armor, critical hit, additive vs multiplicative modifiers, minimum/maximum damage, dan resistance clamps perlu spesifikasi. 

### **6.4 Accuracy** 

- Ranged Accuracy memengaruhi hit chance basic attack dan active skill ranged. 

- Draft hit model: Base Hit Chance + Accuracy − Evasion, dengan clamp min/max; angka TBD. 

- QTE performance memengaruhi hasil serangan secara terpisah dari hit chance. 

- Hunter’s Instinct meningkatkan Ranged Accuracy Ranger. 

GDD Draft v0.1 — Project Tugas GenAI 

## **7. Equipment** 

|**Slot**|**Keterangan**|
|---|---|
|**Main Hand**|Primary weapon.<br>f|
|**Second Hand**|Offhand; setiap class memiliki setidaknya satu pilihan<br>kompatibel.|
|**Head Armor**|Armor slot.|
|**Body Armor**|Armor slot.|
|**Leg Armor**|Armor slot.|
|**Foot Armor**|Armor slot.|
|**Accessory 1**|Accessory.|
|**Accessory 2**|Accessory.|



### **7.1 Compatibility rules** 

- Weapon compatibility ketat berdasarkan class. 

- Main Hand dan Second Hand memiliki daftar kompatibilitas terpisah. 

- Tidak ada pengecualian kompatibilitas senjata, termasuk Legendary. 

- Armor dan accessories dapat dipakai lintas class kecuali dinyatakan terbatas. 

- Weapon Requirement Skills merupakan konsep: skill tertentu dapat membutuhkan tipe senjata; mapping final TBD. 

### **7.2 Itemization** 

Stat budget, rarity, affixes, upgrade/enhancement, equipment acquisition, dan loot tables belum ditetapkan. 

GDD Draft v0.1 — Project Tugas GenAI 

## **8. Shop dan Run Economy** 

Shop System v1.0 memiliki rules berikut: 

- Gold hanya berlaku selama dungeon run. 

- Inventory shop dihasilkan secara acak. 

- Equipment dapat dibeli untuk karakter tertentu. 

- Consumables memiliki stock terbatas. 

- Inventory Refresh menghabiskan Gold. 

- Pemain dapat menjual equipment. 

- Gold tersisa hilang ketika run berakhir. 

- Shop dapat ditinggalkan tanpa pembelian. 

### **8.1 Detail TBD** 

- Harga item, sell value, refresh cost dan scaling. 

- Batas refresh atau aturan stock item non-consumable. 

- Distribusi item dan rarity di inventory acak. 

- Gold inflow/outflow per dungeon dan balancing ekonomi. 

GDD Draft v0.1 — Project Tugas GenAI 

## **9. Run Failure / Victory** 

### **9.1 Failure** 

- Run gagal/kalah mengakhiri ekspedisi. 

- Pemain kembali ke Main Menu. 

- Dungeon/run state reset; harus New Game untuk mencoba lagi. 

- Sisa Gold run dihapus. 

- Checkpoint/resume belum disepakati. 

### **9.2 Victory** 

Dungeon clear melanjutkan progres ke dungeon berikutnya. Dungeon ke-8 menyelesaikan ekspedisi. Ending, victory screen, run summary, dan meta-progression belum ditentukan. 

### **9.3 Meta progression** 

Belum diputuskan apakah ada unlock permanen atau progres antar-run. Jangan mengasumsikan persistent upgrades sebelum disepakati. 

GDD Draft v0.1 — Project Tugas GenAI 

## **10. Audio Direction** 

### **10.1 Main Menu Music** 

Arah yang dipilih: Main Menu Music Direction A dengan sentuhan JRPG anime. Nuansa konseptual fantasy adventure dan tema melodi yang memorable. 

### **10.2 Battle Music** 

Beberapa Battle Theme direncanakan. Battle Theme 1, 2, dan 3 telah dibahas sebagai konsep; pemetaan cue, intensitas, adaptive transition, dan final track specification belum final. 

### **10.3 Map Node Music** 

Musik khusus map node dibutuhkan. Mood, durasi loop, instrumentasi, dan transisi menuju combat/shop/event TBD. 

### **10.4 SFX** 

SFX dibutuhkan untuk UI, node, skill casting, physical/elemental impact, status, QTE success/perfect/fail, shop, dungeon clear, dan run failure. Final asset list TBD. 

GDD Draft v0.1 — Project Tugas GenAI 

## **11. UI/UX dan Accessibility** 

- Main Menu: New Game dan opsi dasar; flow detail TBD. 

- Party UI: 4 karakter, HP/MP, status, level, skill loadout, equipment. 

- Combat UI: turn order, target, skill cost, damage type, status duration, enemy telegraph, QTE feedback. 

- Map UI: node choices dan progres dungeon sequence. 

- Shop UI: random inventory, harga, target character, consumable stock, refresh cost, sell, leave. 

- Status icons/tooltips perlu memperlihatkan efek dan durasi. 

- Accessibility: QTE timing options, visual/audio cues, text scaling, dan color-safe indicators perlu spesifikasi. 

GDD Draft v0.1 — Project Tugas GenAI 

## **12. Content Scope dan Open Questions** 

### **12.1 Known scope** 

- 9 playable classes. 

- 7 races: Human, Elf, Dwarf, Halfling, Dragonborn, Liichtian, Merfolk. 

- 8 sequential dungeons. 

- 5 skill tiers per class; draft tiap Tier berisi 3 Active dan 2 Passive. 

- 8 equipment slots. 

- Node map dan run-based shop. 

### **12.2 Major open questions** 

|**Area**|**Perlu ditentukan**<br>ii|
|---|---|
|**Combat math**|Formula final, armor, crit, resistance caps, modifier order.|
|**Status glossary**|Broken, Root, Slow, Burn, Poison, Curse, Shield, Regeneration,<br>Stun, Freeze.|
|**Level/SP**|EXP curve, SP per level, milestone rewards, SP cost per Rank.|
|**Race**|Ability Bonus, Feat, Utility, Point Buy values.<br>f       i|
|**Class skills**|Effect, Rank scaling, MP cost, duration, QTE profile, weapon<br>requirement.<br>f|
|**Equipment**|Compatibility matrix, rarity, stats, affixes, progression.|
|**Dungeon content**|Themes, node layouts, encounters, bosses, rewards.|
|**Shop**|Price formula, sell value, refresh cost/weights.|
|**Run persistence**|What precisely carries between cleared dungeons.|
|**Meta**|Persistent unlocks or none across failed runs.<br>l|
|**Audio/UI**|Final music cue sheet,SFX list,UI flow,accessibilityspecs.|



### **Recommended next design pass** 

Prioritaskan combat math dan status glossary. Keduanya menjadi dependensi untuk skill seluruh class, enemy design, itemization, dan encounter balancing. 

GDD Draft v0.1 — Project Tugas GenAI 

