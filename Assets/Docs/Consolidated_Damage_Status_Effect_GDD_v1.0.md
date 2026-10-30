**GAME DESIGN DOCUMENT  |  DAMAGE & STATUS EFFECT SYSTEM** 

**GAME DESIGN DOCUMENT** 

# **DAMAGE & STATUS EFFECT SYSTEM** 

_Damage Types • Damage Calculation • Resistance • DoT • Mental Effects_ 

Consolidated Draft v1.0  |  30 Oktober 2026 

Dokumen rangkuman keputusan dari diskusi desain. Aturan yang belum disepakati ditandai TBD. 

Project Tugas GenAI  •  Consolidated Discussion GDD 

**GAME DESIGN DOCUMENT  |  DAMAGE & STATUS EFFECT SYSTEM** 

## **Dokumen dan Status** 

|**Item**|**Keterangan**|
|---|---|
|Dokumen|Damage & Status Effect System — Consolidated Draft v1.0|
|Basis|Keputusan yang telah disetujui dalam diskusi proyek.|
|Cakupan|Damage taxonomy, formula, QTE, Critical, affinities, resistance,<br>modifier, DoT, status application, Mental Effects, Boss rules.|
|Status|Working draft. Angka dan perilaku yang belum dipilih secara<br>eksplisit tidak diperlakukan sebagai final.|



## **Daftar Isi** 

- 1. Damage Types dan Elemen 

- 2. Damage Formula dan QTE 

- 3. Critical Hit 

- 4. Enemy Affinity dan Null Break 

- 5. Player Elemental Resistance 

- 6. Damage Modifier Stacking 

- 7. Multi-Type dan Multi-Hit 

- 8. Status Effect: Prinsip Umum 

- 9. Status Effect Resistance 

- 10. Damage Over Time 

- 11. Status Effect Non-Mental 

- 12. Mental Effect System 

- 13. Boss Rules 

- 14. Open Questions 

## **1. Damage Types dan Elemen** 

|**Kategori**|**Aturan**|
|---|---|
|Physical Damage|Damage fisik, serangan senjata, dan skill fisik.|
|Elemental Damage|Kategori damage elemental; menggantikan Magical Damage<br>sebagai kategori damage utama.|
|Almighty Damage|Damage absolut yang mengabaikan defense dan<br>affinity/resistance sesuai aturan khusus di bawah.|



### **1.1 Elemen** 

Fire, Ice, Lightning, Earth, Wind, Water, Holy, dan Shadow. 

Nature bukan elemen damage. Nature adalah tema/identitas kemampuan Druid; tidak ada Nature Damage atau Nature Resistance. 

Tidak ada kategori Magical Damage terpisah. Istilah magic dapat menjadi deskripsi skill atau scaling, tetapi damage category yang digunakan adalah Elemental Damage. 

## **2. Damage Formula dan QTE** 

### **2.1 Base Damage** 

|**Damage Type**|**Base Formula**|
|---|---|
|Physical|SkillPower + (STR × Scaling)|



Project Tugas GenAI  •  Consolidated Discussion GDD 

**GAME DESIGN DOCUMENT  |  DAMAGE & STATUS EFFECT SYSTEM** 

|Elemental|SkillPower + (INT × Scaling)|
|---|---|
|Almighty|SkillPower + (INT × Scaling)|



Nilai Scaling belum ditetapkan secara universal dan akan diatur per skill. 

### **2.2 QTE** 

|**QTE Grade**|**Multiplier**|
|---|---|
|Perfect|1.5×|
|Good|1.0×|
|Miss|0.5×|



- QTE memengaruhi Physical, Elemental, dan Almighty direct damage. 

- QTE adalah kualitas eksekusi serangan dan terpisah dari Critical Hit. 

- QTE tidak memengaruhi Damage Over Time sama sekali. 

- QTE juga dapat diterapkan pada seluruh komponen damage dari skill multi-type. 

## **3. Critical Hit** 

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

## **4. Enemy Affinity dan Null Break** 

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

Project Tugas GenAI  •  Consolidated Discussion GDD 

**GAME DESIGN DOCUMENT  |  DAMAGE & STATUS EFFECT SYSTEM** 

- Durasi berdasarkan skill level: Lv1 1 turn, Lv2 2 turns, Lv3 3 turns. 

## **5. Player Elemental Resistance** 

Player characters tidak memakai enemy-style Weakness/Resistant/Nullification affinity untuk elemen. Mereka menggunakan Elemental Resistance dari equipment, stats, buffs, dan sumber terkait. 

R_final = min(R_equipment + R_stats + R_buffs + R_other, 75%) 

D_received = D_incoming × (1 − R_final) 

- Resistance stacking bersifat additive. 

- Cap Elemental Resistance adalah 75% per elemen. 

- Setiap elemental damage component menggunakan resistance elemen masing-masing. 

- Physical dan Almighty mengabaikan Elemental Resistance. 

## **6. Damage Modifier Stacking** 

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



Nullification adalah pengecualian: jika komponen dinullifikasi, hasilnya 0 dan modifier damage tidak mengubah hasil tersebut. 

## **7. Multi-Type dan Multi-Hit** 

- Skill dapat memiliki beberapa damage component, misalnya Physical + Fire atau dua elemen. 

- Setiap component menggunakan formula, affinity, defense/resistance, dan modifier yang sesuai. 

- Nullification hanya membatalkan component yang ditargetkan. 

- QTE berlaku pada semua component direct damage. 

- Critical hanya berlaku pada component Physical. 

- Setiap hit adalah damage event tersendiri; Critical roll dilakukan per hit. 

- Untuk satu hit dengan beberapa component, pertahankan nilai desimal tiap component, jumlahkan component, lalu lakukan rounding pada total hit. 

- Rounding direction belum dikonfirmasi secara eksplisit; rekomendasi kerja sebelumnya adalah Floor. 

- Jika sebuah skill secara eksplisit mengaplikasikan status per hit, setiap hit dapat menghasilkan status instance tersendiri. 

Project Tugas GenAI  •  Consolidated Discussion GDD 

**GAME DESIGN DOCUMENT  |  DAMAGE & STATUS EFFECT SYSTEM** 

### **7.1 Calculation Order — Physical** 

1. Hitung Base Physical Damage: SkillPower + (STR × Scaling). 

2. Terapkan Physical Damage Bonus dan All Damage Bonus (multiplicative). 

3. Terapkan QTE multiplier. 

4. Roll Critical per hit; jika berhasil, terapkan Critical Damage multiplier. 

5. Kurangi Physical Defense secara flat. Damage dapat mencapai 0; tidak ada minimum damage 1. 

6. Terapkan Physical Damage Taken modifier. 

7. Terapkan Physical Affinity (Weakness/Normal/Resistant). Nullification membatalkan component, kecuali Null Break menonaktifkannya. 

8. Round hasil akhir per hit (metode rounding final perlu konfirmasi). 

### **7.2 Calculation Order — Elemental** 

9. Hitung Base Elemental Damage: SkillPower + (INT × Scaling). 

10. Terapkan Elemental Damage Bonus, element-specific bonus, dan All Damage Bonus secara multiplicative. 

11. Terapkan QTE multiplier. 

12. Terapkan Elemental Damage Taken dan modifier Taken terkait (sebelum Elemental Resistance). 

13. Terapkan Enemy Elemental Affinity. 

14. Terapkan target Elemental Resistance untuk elemen tersebut. 

15. Nullification membatalkan component jika aktif; Null Break dapat meniadakan Nullification yang ditargetkan. 

16. Untuk multi-component hit, jumlahkan nilai component lalu rounding total hit. 

### **7.3 Calculation Order — Almighty** 

17. Hitung Base Almighty Damage: SkillPower + (INT × Scaling). 

18. Terapkan All Damage Bonus. 

19. Terapkan QTE multiplier. 

20. Terapkan All Damage Taken. 

21. Almighty mengabaikan Defense, enemy affinity, dan Elemental Resistance; tidak melakukan Critical Hit. 

22. Round hasil akhir per hit. 

## **8. Status Effect: Prinsip Umum** 

- Status Effect ditentukan oleh skill; elemen damage tidak otomatis menentukan status. 

- Skill dapat memberikan Status Effect tanpa Elemental Damage dan dapat menerapkan lebih dari satu status jika didesain demikian. 

- Durasi ditentukan oleh Level Skill; contoh level-to-duration yang pernah dibahas adalah Lv1 1 turn, Lv2 2 turns, Lv3 3 turns, tetapi durasi universal semua skill belum dikonfirmasi. 

- Status instances independen: instance baru tidak refresh/extend instance lama. 

- Tidak ada batas jumlah instance aktif untuk status yang sama. 

- Status Effect berbeda dapat coexist pada target. 

- Jika skill menerapkan status per hit, tiap hit melakukan roll independen dan hit sukses dapat menambah instance. 

- Status Effect Resistance mengurangi chance aplikasi, bukan duration atau potency setelah aplikasi. 

## **9. Status Effect Resistance** 

|**Parameter**|**Aturan**|
|---|---|
|Type|Universal Status Effect Resistance; tidak terpisah per status.|
|Sumber|Base Stats, Equipment, Buffs, Passives.|
|StackingResistance|Additive.|
|Cap|75%.|
|Minimum|0%.|
|Resistance Reduction|Percentage Reduction.|



Project Tugas GenAI  •  Consolidated Discussion GDD 

**GAME DESIGN DOCUMENT  |  DAMAGE & STATUS EFFECT SYSTEM** 

|Multiple reductions|Multiplicative Stacking.|
|---|---|
|Boss|Resistance tinggi untuk non-Mental status; dapat direduksi.|



R_final = min(R_base + R_equipment + R_stats + R_buffs + R_passives, 75%) P_final = P_base × (1 − R_final) R_after = max(0, R_initial × ∏(1 − D_i)) 

Contoh: Status Effect Chance 80%, resistance 75% → chance final 20%. Resistance 60% dengan dua debuff 20% → 38.4% resistance; chance skill 40% → 24.64%. 

## **10. Damage Over Time** 

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



## **11. Status Effect Non-Mental** 

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

## **12. Mental Effect System** 

Mental Effects adalah kategori Status Effect yang memengaruhi pikiran, persepsi, emosi, atau kontrol target. Mental Effect bukan Damage Type/elemen baru dan menggunakan Universal Status Effect Resistance. 

|**Mental Effect**|**Mekanik yang disepakati**|
|---|---|
|Mind Control|Target kehilangan kontrol; pihak pengendali menentukan Action<br>target.|
|Confusion|Target masih bertindak,tetapi berpeluangmelakukan|



Project Tugas GenAI  •  Consolidated Discussion GDD 

**GAME DESIGN DOCUMENT  |  DAMAGE & STATUS EFFECT SYSTEM** 

||Action/target yangsalah.|
|---|---|
|Fear|Model B: Combat Debuff. Target tetap bertindak, tetapi Damage<br>Dealt berkurang.|
|Despair|Model A: Action Disruption. Target memiliki peluang gagal<br>melakukan Action.|
|Charm|Target tidak dapat menyerang sumber Charm, tetapi dapat<br>melakukan Action lain yangvalid.|
|Sleep|Target tidak dapat melakukan Action. Sleep berakhir saat target<br>menerima direct damage atau DoT; damage pemicu tetap<br>diterima.|
|Silence|Target tidak dapat menggunakan Skill; Basic Attack dan Item<br>tetap dapat digunakan.|
||AI memilih Action dan target secara acak. Action dapat ofensif|
|Madness|atau defensif (Basic Attack, Skill, Heal, Buff, Guard, dll. jika<br>valid); target dapat menyerangparty sendiri.|



### **12.1 Madness Random Action** 

- Pemilihan Action dilakukan secara acak, bukan AI optimal. 

- Pemilihan target juga acak dari target yang valid. 

- Action ofensif dan defensif dapat dipilih jika persyaratannya terpenuhi. 

- Action invalid tidak masuk ke pilihan. 

## **13. Boss Rules** 

|**Status Category**|**Aturan Boss**<br>f|
|---|---|
|Non-Mental Status Effect|Boss dapat terkena; memiliki Universal Status Effect Resistance<br>tinggi.|
|Duration|Tetap mengikuti durasi skill; tidak ada durasi khusus Boss yang<br>lebih pendek.|
|Resistance Reduction<br>f|Dapat dikurangi dengan Percentage Reduction dan multiplicative<br>stacking.<br>f|
|Mental Effects|Boss immune terhadap seluruh Mental Effects.<br>f|
|Immunity bypass|Status Effect Resistance Reduction tidak menembus Mental<br>Effect immunity.|



## **14. Open Questions** 

- Konfirmasi arah rounding: Floor atau nearest integer; per-hit total rounding telah dipilih sebagai pendekatan, tetapi arah belum explicit-confirmed. 

- Nilai Skill Scaling dan coefficients tiap skill. 

- Exact timing status expiry/cleansing jika terjadi pada timing tick/turn yang sama. 

- Nilai dasar dan scaling Burn, Poison, Bleed, serta sumber stat setiap skill. 

- Freeze Speed reduction; Shock/Despair fail chance; Fear damage reduction; Confusion mis-action probability. 

- Daftar Action dan pemilihan target Mind Control; target scope dan AI details. 

- Aturan apakah multiple instance dengan efek chance (Shock/Despair) roll independen ketika aktif—perlu spesifikasi. 

- Detail cleanse oleh Cleric/Druid dan item: cakupan status, jumlah instance yang dihapus, dan biaya/targeting. 

- Exact status-specific exceptions, boss encounters, dan skill-level duration tables. 

   - _End of Consolidated Damage & Status Effect System GDD —_ 

Project Tugas GenAI  •  Consolidated Discussion GDD 

