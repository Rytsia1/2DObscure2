# PRD — Race System
## Party-Based Roguelike RPG

**Version:** 1.0  
**Status:** Draft — Ready for Implementation Planning  
**System:** Character Creation, Combat, Exploration  
**Genre:** Party-Based Turn-Based Roguelike RPG

---

# 1. Overview

Race System memberikan identitas mekanis dan naratif kepada setiap karakter yang dibuat pemain.

Setiap race memiliki keunggulan tersendiri yang memengaruhi:
- Character attributes.
- Combat strategy.
- Racial signature abilities.
- Exploration dan interaksi dengan dunia.
- Sinergi dengan party composition.

Race tidak boleh menjadi penentu mutlak class atau role karakter. Semua race harus tetap dapat digunakan untuk berbagai class tanpa menciptakan kombinasi yang wajib dipilih untuk mencapai efektivitas optimal.

Sistem dirancang agar seluruh racial effects dapat dikonfigurasi melalui data, sehingga balancing dapat dilakukan tanpa mengubah core combat code.

# 2. Design Goals

## 2.1 Primary Goals

1. Setiap race memiliki identitas gameplay yang jelas.
2. Setiap race memiliki keunggulan yang relevan dalam combat maupun exploration.
3. Racial Feat harus unik dan tidak dimiliki race lain.
4. Tidak ada race yang wajib digunakan untuk class tertentu.
5. Racial Feat tidak boleh menggantikan atau menghilangkan tantangan core combat.
6. Race System harus kompatibel dengan Individual Turn-Based Combat dan Reaction-Based QTE.
7. Race System harus mendukung party berisi empat karakter.
8. Seluruh racial effects harus data-driven dan mudah di-balance.

## 2.2 Non-Goals

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

# 3. Race System Structure

Setiap race memiliki tepat tiga komponen utama:

| Component | Description |
|---|---|
| Racial Ability Bonus | Bonus terhadap Ability Scores karakter. |
| Racial Feat | Signature ability unik yang dimiliki race tersebut. |
| Racial Utility | Keunggulan untuk exploration, event, atau interaksi dunia. |

Tidak ada komponen Passive Trait.

## 3.1 Racial Ability Bonus

Racial Ability Bonus memberikan bonus terhadap enam Ability Scores:

- STR — Strength
- DEX — Dexterity
- VIT — Vitality
- INT — Intelligence
- WIS — Wisdom
- CHA — Charisma

Bonus race diterapkan setelah proses Point Buy.

Racial Ability Bonus tidak boleh memberikan bonus yang terlalu besar hingga menentukan class karakter secara otomatis.

## 3.2 Racial Feat

Racial Feat adalah kemampuan signature yang hanya dimiliki satu race.

Aturan:

- Setiap race memiliki satu Racial Feat utama.
- Racial Feat tidak dapat dipelajari race lain.
- Racial Feat dapat berupa active ability, reaction, atau trigger-based ability.
- Racial Feat dapat memiliki cooldown, duration, usage limit, atau trigger condition.
- Racial Feat harus kompatibel dengan combat event system.
- Racial Feat harus memiliki visual dan combat feedback yang jelas.

## 3.3 Racial Utility

Racial Utility memberikan keuntungan dalam exploration dan interaksi dunia.

Utility dapat memengaruhi:

- Exploration events.
- Environmental hazards.
- Hidden paths.
- Dialogue options.
- Resource discovery.
- Roguelike event outcomes.

Utility tidak boleh membuat konten utama hanya dapat diselesaikan oleh satu race.

Jika suatu utility membuka jalur khusus, harus tersedia alternatif untuk pemain yang tidak memiliki race tersebut.

# 4. Race Definitions

## 4.1 Human

**Race Identity:** Adaptability, versatility, determination.

### Racial Ability Bonus

Human mendapatkan bonus terhadap dua Ability Scores yang dipilih pemain.

Provisional:
- +1 terhadap dua Ability Scores pilihan.
- Pilihan dilakukan pada Character Creation.
- Bonus tidak dapat diubah setelah karakter dibuat, kecuali melalui sistem respec.

### Racial Feat — Determination

Human memanfaatkan tekadnya untuk bertahan dalam situasi kritis.

Konsep:
- Memicu efek ketika Human mencapai kondisi HP kritis.
- Memberikan temporary combat advantage.
- Efek dapat berupa temporary Shield, peningkatan defense, atau peningkatan damage.
- Memiliki batas penggunaan per battle.

Detail trigger, angka, dan durasi harus configurable.

### Racial Utility — Adaptability

Human memiliki fleksibilitas dalam menghadapi exploration events.

Contoh:
- Mendapatkan opsi tambahan dalam event tertentu.
- Mendapatkan bonus pada situasi yang tidak memiliki racial advantage khusus.
- Dapat mengakses beberapa solusi alternatif dalam event.

Utility Human harus berfokus pada fleksibilitas, bukan menjadi bonus universal yang selalu lebih kuat daripada race lain.

---

## 4.2 Elf

**Race Identity:** Grace, precision, magical affinity.

### Racial Ability Bonus

- +2 DEX.

### Racial Feat — Fey Step

Elf menggunakan kekuatan Fey untuk berpindah posisi secara instan.

Konsep:
- Melakukan reposition dalam combat.
- Dapat digunakan untuk menghindari situasi berbahaya.
- Dapat memberikan bonus Evasion atau keuntungan taktis setelah teleport.
- Tidak memberikan extra turn.

Jika battlefield menggunakan fixed positions, Fey Step harus mematuhi validasi posisi dan aturan movement.

### Racial Utility — Keen Senses

Elf memiliki kemampuan persepsi yang tajam.

Contoh:
- Mendeteksi hidden paths.
- Menemukan petunjuk exploration.
- Mendeteksi jebakan atau environmental hazards.
- Mengungkap informasi tambahan tentang encounter.

Utility ini tidak boleh otomatis mengungkap seluruh secret dalam suatu area.

---

## 4.3 Dwarf

**Race Identity:** Endurance, resilience, craftsmanship.

### Racial Ability Bonus

- +2 VIT.

### Racial Feat — Iron Resolve

Dwarf menahan serangan dengan ketahanan luar biasa.

Konsep:
- Diaktifkan sebagai defensive reaction.
- Mengurangi damage yang diterima.
- Dapat memberikan bonus Stagger Resistance.
- Dapat membantu mempertahankan posisi saat menerima serangan berat.

Iron Resolve tidak boleh membuat Dwarf sepenuhnya kebal terhadap damage atau Break.

### Racial Utility — Stonecraft

Dwarf memiliki pengetahuan tentang batu, mineral, dan struktur bawah tanah.

Contoh:
- Menemukan mineral atau resource tertentu.
- Mengidentifikasi struktur yang dapat dihancurkan.
- Mengakses jalur alternatif di dungeon.
- Mendapatkan informasi tambahan tentang konstruksi atau mekanisme kuno.

---

## 4.4 Halfling

**Race Identity:** Luck, agility, resilience through opportunity.

### Racial Ability Bonus

- +2 DEX.

### Racial Feat — Second Chance

Halfling mendapatkan kesempatan kedua ketika keberuntungan meninggalkannya.

Second Chance terintegrasi langsung dengan Reaction-Based QTE System.

#### Trigger

Halfling mendapatkan hasil `Failure` pada QTE.

#### Effect

Pemain dapat memilih untuk mengulang QTE yang sama satu kali.

#### Rules

1. Second Chance hanya dapat digunakan setelah hasil QTE adalah `Failure`.
2. Second Chance tidak otomatis mengubah Failure menjadi Success.
3. Pemain harus melakukan QTE kedua secara manual.
4. Hasil percobaan kedua menjadi hasil final.
5. Jika percobaan kedua juga Failure, efek kegagalan tetap berlaku.
6. Second Chance tidak dapat digunakan untuk mengulang hasil Success atau Perfect.
7. Second Chance tidak dapat digunakan berulang kali pada QTE yang sama.
8. Second Chance memiliki batas penggunaan per battle.
9. Second Chance tidak boleh menyebabkan QTE menjadi infinite loop.
10. QTE retry harus mempertahankan konteks serangan atau event yang sama.

#### QTE Difficulty

Untuk versi awal:
- Percobaan kedua menggunakan difficulty yang sama dengan QTE pertama.
- Difficulty tidak diturunkan secara otomatis.
- QTE tetap menggunakan input dan timing window normal.

Difficulty retry dapat menjadi balancing parameter di masa depan, tetapi bukan bagian dari baseline implementation.

#### Usage Limit

Provisional:
- 1 penggunaan per battle.

Usage limit harus configurable.

### Racial Utility — Small and Nimble

Halfling memiliki tubuh kecil dan kemampuan bergerak di ruang sempit.

Contoh:
- Mengakses jalur kecil.
- Menghindari environmental hazards tertentu.
- Menemukan hidden paths.
- Mendapatkan opsi tambahan dalam exploration events.

Utility tidak boleh memberikan kemampuan untuk melewati seluruh dungeon obstacles secara otomatis.

---

## 4.5 Dragonborn

**Race Identity:** Draconic heritage, elemental power, overwhelming strength.

### Racial Ability Bonus

Bonus Ability Score bergantung pada subrace.

Provisional:
- +2 STR atau CHA sesuai pilihan subrace.
- Subrace harus memiliki identitas elemental atau draconic yang jelas.

### Racial Feat — Draconic Overdrive

Dragonborn membangkitkan kekuatan darah naga untuk sementara.

#### Activation

- Active ability.
- Menggunakan turn Dragonborn.
- Memiliki cooldown atau usage limit.
- Tidak memberikan extra turn.

#### Effects

Selama Draconic Overdrive aktif:

- Physical Power atau Magic Power meningkat.
- Break Power meningkat.
- QTE Counter Power mendapatkan bonus.
- Efek elemental dapat mengikuti subrace Dragonborn.

Setelah efek berakhir, Dragonborn menerima status `Exhausted`.

#### Exhausted

Exhausted merupakan status sementara yang mengurangi sebagian kemampuan combat Dragonborn.

Contoh efek:
- Mengurangi Physical Power.
- Mengurangi Magic Power.
- Mengurangi Break Power.

Status Exhausted harus memiliki durasi dan modifier yang configurable.

#### Design Constraints

- Overdrive tidak boleh meningkatkan semua combat stats secara permanen.
- Overdrive tidak boleh memberikan extra turn secara default.
- Overdrive tidak boleh menghapus cooldown ability lain.
- Exhausted tidak boleh membuat Dragonborn kehilangan kemampuan untuk melakukan aksi.
- Overdrive harus memiliki visual feedback yang jelas.

### Racial Utility — Draconic Presence

Dragonborn memiliki aura dan warisan naga yang memengaruhi interaksi dunia.

Contoh:
- Dialogue options khusus.
- Interaksi dengan dragon-related events.
- Kesempatan untuk mengintimidasi NPC tertentu.
- Informasi tambahan tentang draconic relics atau ancient dragon sites.

---

## 4.6 Liichtian

**Race Identity:** Celestial heritage, radiant power, divine connection.

Liichtian adalah race original dalam dunia game dengan konsep dasar yang terinspirasi oleh Aasimar.

Liichtian bukan sekadar nama lain untuk Aasimar. Lore, budaya, dan kemampuan ras harus mengikuti identitas dunia game.

### Racial Ability Bonus

Provisional:
- +2 WIS atau CHA.

Pilihan bonus dapat disesuaikan berdasarkan subrace atau karakterisasi Liichtian.

### Racial Feat — Lightbound

Liichtian memanifestasikan kekuatan celestial untuk membantu party.

Konsep:
- Menghasilkan efek radiant atau divine.
- Dapat memberikan perlindungan atau support kepada ally.
- Dapat meningkatkan kemampuan bertahan party.
- Memiliki durasi atau usage limit.

Lightbound harus menjadi signature support ability yang berbeda dari Ocean's Embrace.

### Racial Utility — Luminescent Insight

Liichtian memiliki koneksi dengan energi celestial.

Contoh:
- Mengungkap hidden clues yang berkaitan dengan radiant atau divine energy.
- Mendeteksi corruption atau supernatural influence.
- Mendapatkan informasi tambahan dari sacred sites.
- Membuka interaksi khusus pada celestial-related events.

Utility tidak boleh otomatis mengungkap seluruh lore atau menyelesaikan supernatural events.

---

## 4.7 Merfolk

**Race Identity:** Oceanic affinity, water magic, protection.

### Racial Ability Bonus

Provisional:
- +2 WIS atau DEX.

### Racial Feat — Ocean's Embrace

Merfolk membungkus satu anggota party dengan arus air magis yang melindunginya.

#### Activation

- Active ability.
- Target: satu anggota party.
- Target dapat berupa Merfolk sendiri atau ally lain.
- Menggunakan turn Merfolk.
- Memiliki cooldown atau usage limit.

#### Effects

Ocean's Embrace memberikan perlindungan kepada target.

Baseline concept:
1. Memberikan Shield.
2. Mengurangi damage yang diterima selama Shield aktif.
3. Ketika Shield pecah atau durasi berakhir, sebagian energi air dapat memulihkan HP target.
4. Target dapat memperoleh bonus QTE Stability selama perlindungan aktif.

#### Design Constraints

- Shield tidak boleh bersifat permanent.
- Shield harus memiliki duration atau damage absorption limit.
- Healing tidak boleh terjadi berulang tanpa batas.
- Shield harus mengikuti aturan damage resolution yang berlaku.
- Ocean's Embrace tidak boleh membuat target kebal terhadap seluruh efek combat.

### Racial Utility — Ocean's Gift

Merfolk memiliki hubungan alami dengan lingkungan perairan.

Contoh:
- Mendapatkan keuntungan dalam underwater exploration.
- Mengakses jalur perairan tertentu.
- Mendapatkan informasi tambahan dari aquatic environments.
- Berinteraksi dengan aquatic creatures atau oceanic events.

Utility tetap harus memiliki kegunaan di dungeon yang tidak memiliki area perairan.

# 5. Racial Ability Bonus Rules

## 5.1 Ability Score Calculation

Ability Scores menggunakan sistem Point Buy yang telah ditetapkan.

Racial Ability Bonus diterapkan setelah Point Buy.

Contoh:

Human:
- STR 12
- DEX 14
- VIT 10
- INT 10
- WIS 12
- CHA 8

Human memilih +1 DEX dan +1 CHA.

Final:
- STR 12
- DEX 15
- VIT 10
- INT 10
- WIS 12
- CHA 9

## 5.2 Attribute Caps

Provisional rules:

- Character Creation maximum before racial bonus: 15.
- Post-racial creation cap: 17.
- Normal progression cap: 20.
- Temporary buffs dapat melampaui normal cap hanya jika diizinkan oleh sistem buff.

Semua caps harus configurable.

## 5.3 Class Independence

Race tidak boleh memiliki class restriction.

Racial Ability Bonus tidak boleh membuat class tertentu menjadi mandatory.

Balancing harus mempertimbangkan seluruh class dan combat roles.

# 6. Racial Feat Balance Rules

Setiap Racial Feat harus memenuhi aturan berikut:

1. Memiliki efek yang jelas dan dapat diukur.
2. Memiliki trigger atau activation condition.
3. Memiliki cooldown, duration, usage limit, atau resource cost.
4. Tidak boleh menghasilkan infinite loop.
5. Tidak boleh memberikan extra turn secara default.
6. Tidak boleh meniadakan core combat mechanics.
7. Tidak boleh menjadi solusi wajib untuk boss tertentu.
8. Harus memiliki combat log dan visual feedback.
9. Efek harus dapat dikonfigurasi melalui data.
10. Racial Feat harus tetap relevan sepanjang progression.

# 7. Combat Integration

Race System harus terintegrasi dengan:

- Turn Manager.
- Combat Action System.
- Damage Calculation.
- Shield System.
- Healing System.
- Status Effect System.
- Break/Stagger System.
- Reaction-Based QTE System.
- Target Selection.
- Combat Event System.

## 7.1 Combat Events

Racial Feat dapat menggunakan event berikut:

- `OnTurnStart`
- `OnTurnEnd`
- `OnDamageReceived`
- `OnDamageDealt`
- `OnShieldGranted`
- `OnShieldBroken`
- `OnQTEResolved`
- `OnQTEFailure`
- `OnCharacterHPThresholdReached`
- `OnStatusEffectApplied`
- `OnCharacterBroken`

Event system harus memastikan bahwa satu event tidak memicu racial effect secara berulang tanpa batas.

## 7.2 QTE Integration

Second Chance membutuhkan integrasi khusus.

QTE lifecycle:

1. QTE dimulai.
2. Pemain melakukan input.
3. QTE menghasilkan result.
4. Combat system mengidentifikasi result.
5. Jika result Failure dan karakter memiliki Second Chance, sistem memeriksa usage limit.
6. Pemain mendapatkan pilihan menggunakan Second Chance.
7. Jika digunakan, QTE dimulai ulang.
8. Hasil QTE kedua menjadi final.
9. Combat effect diterapkan.
10. Usage counter diperbarui.

Second Chance tidak boleh:
- Membuat QTE kedua otomatis berhasil.
- Mengulang QTE secara otomatis.
- Memicu dirinya sendiri dari hasil retry.
- Mengulang damage resolution yang sudah diterapkan.
- Mengulang QTE dari karakter lain.

# 8. Data Architecture

Race System harus menggunakan data-driven architecture.

Contoh schema:

```json
{
  "raceId": "halfling",
  "displayName": "Halfling",
  "racialAbilityBonus": {
    "dexterity": 2
  },
  "racialFeat": {
    "featId": "second_chance",
    "activationType": "reaction",
    "triggerEvent": "OnQTEFailure",
    "effectType": "retry_qte",
    "retryLimit": 1,
    "usageLimitPerBattle": 1,
    "retryDifficulty": "same_as_original",
    "automaticSuccess": false
  },
  "racialUtility": {
    "utilityId": "small_and_nimble",
    "category": "exploration"
  }
}
```

Schema di atas merupakan contoh konseptual. Field names harus disesuaikan dengan architecture dan naming conventions project yang sudah ada.

## 8.1 Required Data Fields

Setiap race minimal harus memiliki:

- `raceId`
- `displayName`
- `description`
- `racialAbilityBonus`
- `racialFeat`
- `racialUtility`
- `icon`
- `loreDescription`

Setiap racial feat minimal harus memiliki:

- `featId`
- `displayName`
- `description`
- `activationType`
- `triggerEvent` atau activation condition
- `effectType`
- `cooldown` atau `usageLimit`
- `duration`, jika relevan
- `targetType`
- `balanceParameters`

# 9. Character Creation Integration

Race harus dapat dipilih melalui Character Creation.

Requirements:

1. Race selection menampilkan seluruh race yang tersedia.
2. Race selection menampilkan lore dan gameplay identity.
3. Racial Ability Bonus ditampilkan secara jelas.
4. Racial Feat ditampilkan beserta deskripsi dan mekaniknya.
5. Racial Utility ditampilkan.
6. Final Ability Scores diperbarui ketika race dipilih.
7. Player dapat melihat perubahan Ability Scores sebelum konfirmasi.
8. Race selection harus kompatibel dengan class selection.
9. Race tidak boleh mengunci class.
10. Character data harus menyimpan raceId, bukan hanya display name.

# 10. Exploration Integration

Racial Utility harus terintegrasi dengan exploration event system.

Setiap utility dapat menggunakan:

- Event tags.
- Environment tags.
- Race requirements.
- Alternative solutions.
- Reward modifiers.
- Information reveal.

Contoh:

```json
{
  "eventId": "ancient_waterway",
  "environmentTags": ["aquatic", "underground"],
  "racialInteractions": [
    {
      "raceId": "merfolk",
      "utilityId": "oceans_gift",
      "interactionId": "aquatic_route"
    }
  ]
}
```

Event system harus menyediakan alternatif untuk race lain jika utility membuka jalur atau reward penting.

# 11. Persistence

Race data harus tersimpan pada character save data.

Character save minimal menyimpan:

- Character ID.
- Race ID.
- Subrace ID, jika digunakan.
- Base Ability Scores.
- Racial Ability Bonus selection.
- Final Ability Scores.
- Racial Feat ID.
- Racial Utility ID.

Temporary combat state tidak boleh disimpan sebagai permanent character data.

Contoh temporary state:

- Second Chance usage counter.
- Draconic Overdrive duration.
- Exhausted status.
- Ocean's Embrace Shield.
- Fey Step cooldown.

Temporary state harus di-reset sesuai lifecycle battle.

# 12. Error Handling

Race System harus menangani kondisi berikut:

- Race ID tidak ditemukan.
- Racial Feat data tidak valid.
- Target ability tidak tersedia.
- Target ally tidak valid.
- QTE sudah diselesaikan.
- Second Chance sudah digunakan.
- Character tidak lagi aktif dalam battle.
- Shield target sudah mencapai limit.
- Combat event dipicu berulang.
- Race data mengalami perubahan setelah character dibuat.

Jika race data tidak valid, sistem harus menggunakan fallback yang aman dan memberikan error log.

# 13. Acceptance Criteria

## Race Data

- [ ] Tujuh race tersedia: Human, Elf, Dwarf, Halfling, Dragonborn, Liichtian, Merfolk.
- [ ] Setiap race memiliki Racial Ability Bonus.
- [ ] Setiap race memiliki tepat satu Racial Feat.
- [ ] Setiap race memiliki Racial Utility.
- [ ] Tidak ada Passive Trait.
- [ ] Racial Feat tidak dapat digunakan oleh race lain.

## Character Creation

- [ ] Pemain dapat memilih race.
- [ ] Ability Scores diperbarui berdasarkan racial bonus.
- [ ] Race tidak mengunci class.
- [ ] Race selection dapat disimpan dan dimuat kembali.
- [ ] Racial Feat dan Utility ditampilkan pada character details.

## Combat

- [ ] Racial Feat dapat digunakan sesuai activation rules.
- [ ] Racial Feat mengikuti turn system.
- [ ] Cooldown dan usage limits berfungsi.
- [ ] Racial Feat tidak menyebabkan infinite loops.
- [ ] Racial Feat tidak memberikan extra turn secara default.
- [ ] Combat log menampilkan penggunaan racial ability.

## Second Chance

- [ ] Second Chance hanya aktif setelah QTE Failure.
- [ ] Pemain dapat memilih untuk mengulang QTE.
- [ ] QTE retry menggunakan difficulty yang sama.
- [ ] Retry tidak otomatis berhasil.
- [ ] Hasil kedua menjadi final.
- [ ] Second Chance tidak dapat digunakan dua kali pada QTE yang sama.
- [ ] Second Chance mengikuti usage limit per battle.

## Exploration

- [ ] Racial Utility dapat dipicu pada event yang sesuai.
- [ ] Race-specific interactions memiliki feedback yang jelas.
- [ ] Konten utama tidak terkunci hanya karena pemain tidak memiliki race tertentu.

# 14. Balancing Parameters

Seluruh parameter berikut harus configurable:

- Racial Ability Bonus.
- Racial Feat cooldown.
- Racial Feat usage limit.
- Racial Feat duration.
- Racial Feat damage modifier.
- Racial Feat healing modifier.
- Racial Feat shield amount.
- Racial Feat status effect duration.
- Racial Feat trigger threshold.
- Racial Utility reward modifier.
- Racial Utility event conditions.
- Attribute caps.

# 15. Implementation Notes

Implementation harus mengikuti architecture project yang sudah ada.

Sebelum implementasi:

1. Audit character data structure.
2. Audit Ability Score calculation.
3. Audit Character Creation flow.
4. Audit Combat Action System.
5. Audit QTE lifecycle.
6. Audit Status Effect System.
7. Audit Exploration Event System.
8. Audit save/load system.

Jangan membuat sistem baru yang menduplikasi fungsi yang sudah ada.

Race System harus menggunakan existing systems jika memungkinkan.

# 16. Definition of Done

Race System dianggap selesai apabila:

- Seluruh tujuh race terdaftar.
- Character Creation mendukung race selection.
- Racial Ability Bonus diterapkan dengan benar.
- Seluruh Racial Feat dapat digunakan.
- Racial Utility terintegrasi dengan exploration events.
- Second Chance terintegrasi dengan QTE lifecycle.
- Save/load mempertahankan race data.
- Unit tests untuk racial effects lulus.
- Integration tests untuk combat dan exploration lulus.
- Tidak ada Passive Trait.
- Tidak ada race-specific class restriction.
- Seluruh racial effects dapat dikonfigurasi tanpa mengubah core system code.
