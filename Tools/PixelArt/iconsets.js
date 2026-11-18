// Icon compositions keyed by game id (Database.cs / CatalogueDatabase.cs). Each entry is a draw function on a Canvas.
// g(): a glyph centred on (7,7) by default; I(): a glyph that already fills the 14x14 icon space. t = {x, y, s, r, fx}.
const { G } = require("./icons.js");
const g = (name, o = {}, t = {}) => (c) => c.at({ x: 7, y: 7, ...t }, () => G[name](c, o));
const I = (name, o = {}, t = {}) => (c) => c.at(t, () => G[name](c, o));
const all = (...f) => (c) => f.forEach((x) => x(c));
const up = (k = "N") => g("up", { k }, { x: 11.2, y: 3, s: 0.7 });
const down = (k = "K") => g("down", { k }, { x: 11.2, y: 11, s: 0.7 });
const spark = (k = "T", x = 11, y = 3, s = 0.7) => g("star", { n: 4, k, r1: 4, r0: 1.1 }, { x, y, s });
const plus = (k = "N") => g("plus", { k }, { x: 11.3, y: 3, s: 0.75 });
const goldStar = () => spark("H", 11.3, 2.8, 0.75);
const diag = (name, o = {}, t = {}) => g(name, o, { r: 45, ...t });
const comet = (k, core = "W") => (c) => c.at({ x: 7, y: 7 }, () => { c.cap([[6, -6], [-1, 1]], 0.4, 2.6, k); c.ell(-2, 2, 2.9, 2.9, k); c.ell(-2.6, 1.4, 1.2, 1.2, core); });

// ---------------- items ----------------
const ITEMS = {
  draught: I("flask", { liquid: "K", shape: "tall" }),
  ether: I("flask", { liquid: "B", shape: "tall" }),
  elixir: all(I("flask", { liquid: "D", cork: "b" }), g("skull", { eye: "D" }, { x: 11.4, y: 2.8, s: 0.42 })),
  phoenix_down: g("feather", { k: "F", quill: "E" }, { r: 40, s: 0.95 }),
  CON001: I("flask", { liquid: "K", shape: "vial", fill: 7.5 }),
  CON002: I("flask", { liquid: "K", shape: "tall" }),
  CON003: all(I("flask", { liquid: "K", cork: "G" }), spark("H", 11.5, 2.5, 0.6)),
  CON004: I("flask", { liquid: "B", shape: "vial", fill: 7.5 }),
  CON005: I("flask", { liquid: "B", shape: "tall" }),
  CON006: all(I("flask", { liquid: "B", cork: "G" }), spark("H", 11.5, 2.5, 0.6)),
  CON007: I("bandage", { cross: "r" }),
  CON008: all(I("flask", { liquid: "O", shape: "vial" }), g("drop", { k: "O" }, { x: 11.5, y: 4, s: 0.45 })),
  CON009: I("pouch", { k: "c", tie: "z", spill: (c) => G.pile(c, { k: "q" }) }),
  CON010: all(I("flask", { liquid: "I" }), g("cross", { k: "H" }, { x: 7, y: 9.2, s: 0.4 })),
  CON011: I("bread", {}),
  CON012: all(I("flask", { liquid: "m", shape: "tall", cork: "V" }), g("leaf", { k: "n" }, { x: 11.5, y: 3.5, s: 0.4, r: 30 })),
  CON013: all(I("flask", { liquid: "Y", shape: "tall" }), g("eye", { iris: "N" }, { x: 7, y: 9.8, s: 0.4 })),
  CON014: I("jug", { k: "z", drop: "F" }),
  CON015: all(I("flask", { liquid: "C", shape: "vial" }), g("wing", { k: "c" }, { x: 9.5, y: 5, s: 0.5 })),
  CON016: I("urn", { k: "o", glow: "F" }),
  CON017: g("feather", { k: "H", tip: "F", quill: "G" }, { r: 40, s: 0.95 }),
  CON018: I("bomb", {}),
  CON019: I("lantern", { glow: "C" }),
  CON020: I("incense", { k: "z", smoke: "X" }),
  MAT001: I("scrap", {}),
  MAT002: I("pile", { k: "o", dots: [[5, 9], [8, 7], [9.5, 10.5], [6.5, 11.5]], dot: "C" }),
  MAT003: g("chain", { k: "S" }, { s: 1.15 }),
  MAT004: I("medal", { k: "z", ribbon: "r", inner: "G" }),
  MAT005: g("crystal", { k: "A" }, { r: 20 }),
  MAT006: I("ingot", { k: "G" }),
  MAT007: all(g("orb", { k: "O", r: 4.6 }), (c) => c.at({ x: 7, y: 7 }, () => c.with({ only: "O" }, () => { c.line(-3, 2, 1, -1, "x"); c.line(1, -1, 3, 2, "x"); c.line(1, -1, 0, -4, "x"); }))),
  MAT008: I("scale", { k: "r", ridge: "E" }),
  MAT009: all(g("crystal", { k: "D", facet: "A" }, { r: -25 }), spark("A", 11, 3, 0.55)),
  MAT010: all(g("root", { k: "c" }, { y: 8 }), g("moon", { k: "I" }, { x: 10.5, y: 3, s: 0.45 })),
  MAT011: g("flower", { petal: "B", core: "W" }, { s: 1.05 }),
  MAT012: I("marrow", {}),
  KEY001: all(I("scroll", {}), g("coin", { k: "r", inner: "K", r: 2.2 }, { x: 10, y: 10.5 })),
  KEY002: all(g("coin", { k: "S", inner: "C", r: 6 }), g("flame", { k: "C" }, { s: 0.5, y: 7.5 })),
  KEY003: g("key", { k: "S" }, { s: 1.1 }),
  KEY004: all(g("coin", { k: "G", inner: "z", r: 6 }), g("star", { n: 5, k: "r", core: false }, { s: 0.55 })),
  KEY005: I("book", { cover: "e", spine: "u", sigil: (c) => G.star(c, { n: 4, k: "A" }) }),
  KEY006: all(g("halo", { k: "G" }, { s: 1.3 }), (c) => c.with({ only: "G" }, () => c.rect(8, 0, 9.5, 14, "."))),
  KEY007: g("heart", { k: "O" }, { s: 1.1 }),
  KEY008: I("scale", { k: "E", ridge: "F", rim: "F" }),
  KEY009: I("book", { cover: "l", spine: "V", sigil: (c) => { for (let i = 0; i < 5; i++) { const a = (i / 5) * Math.PI * 2 - Math.PI / 2; c.ell(Math.cos(a) * 4, Math.sin(a) * 4, 1.2, 1.2, "G"); } } }),
  KEY010: all(I("castle", { k: "o" }, { x: 0, y: 0 }), (c) => c.with({ only: "o" }, () => c.rect(0, 0, 14, 14, "o")), (c) => c.poly([[3, 13], [4, 2], [10, 2], [11, 13]], "o"), g("star", { n: 4, k: "A", r1: 4.5 }, { y: 7.5, s: 0.8 })),
};

// ---------------- gear: picked from the weapon type / slot, coloured from words in the name ----------------
const has = (name, re) => re.test(name.toLowerCase());
function metal(n) {
  if (has(n, /gold|gilded|aureate|sun|dawn|regalia|halo|seraph|choir|champion|laurel|signet|mercy/)) return "G";
  if (has(n, /bone|wyrm|dragon|fang|skull|antler|horn/)) return "b";
  if (has(n, /obsidian|umbral|shadow|eclipse|black|damned|abyss|widow|hollow|grave|soul|darkweave|corrupt|forsaken|mourner|coffin|oathbreaker|endworld|calamity|whisper|last/)) return "x";
  if (has(n, /iron|chain|prison|warden|shackle|knuckle|tower/)) return "S";
  if (has(n, /bronze|copper|colosseum|arena|pitfighter/)) return "z";
  return "s";
}
function glow(n, fallback) {
  const rules = [[/ember|cinder|ash|flame|fire|calamity|war god|maw/, "F"], [/storm|thunder/, "Y"], [/tide|drown|frost|glass|mana|pearl/, "B"],
    [/soul|reaper|spirit|lantern|compass/, "C"], [/holy|sanct|dawn|sun|halo|seraph|choir|pilgrim|mercy|saint|divine|golden|aureate|acolyte|prayer|consecrat/, "H"],
    [/verdant|root|thorn|oak|bark|antler|amber|nature|moon|hawk/, "N"], [/shadow|umbral|eclipse|damned|black|abyss|corrupt|whisper|pact|eldritch|ritual|grimoire|widow|hollow|last|forgotten|unblinking/, "D"],
    [/arcane|rune|runic|archmage|star|apprentice|scholar|focus|chronicle|wand/, "A"], [/blood|wound|crimson|heart|vitality|ruby/, "K"], [/blight|poison|filter/, "O"]];
  for (const [re, k] of rules) if (has(n, re)) return k;
  return fallback;
}
const W45 = (name, o) => diag(name, o);
function gearIcon(id, name, slot, wt) {
  const m = metal(name), gl = glow(name, null), n = name.toLowerCase();
  const bladeOf = gl === "D" ? "x" : m === "G" ? "s" : m === "x" ? "x" : m === "b" ? "b" : m === "S" ? "S" : "s";
  const guard = m === "G" || gl === "H" ? "G" : m === "x" ? "u" : m === "b" ? "b" : "z";
  const acc = (fn) => (gl ? all(fn, g("star", { n: 4, k: gl, r1: 3.6, r0: 1, coreR: 0.6 }, { x: 11.5, y: 2.6, s: 0.6 })) : fn);
  // off-hand trinkets carry no weapon type
  if (has(n, /warhorn/)) return g("horn", {}, { s: 1.15 });
  if (has(n, /ki beads/)) return g("beads", {});
  if (has(n, /quiver/)) return g("quiver", {});
  if (has(n, /chime/)) return g("bell", {});
  if (has(n, /tome|grimoire/) && wt !== "Tome") return I("book", { cover: has(n, /shadow/) ? "x" : "u", spine: "l", sigil: (c) => G.star(c, { n: 5, k: has(n, /shadow/) ? "D" : "A", core: false }) });
  if (has(n, /totem/)) return g("totem", {});
  if (has(n, /parrying dagger/)) return diag("dagger", { guard: "G" }, { s: 1.2 });
  switch (wt) {
    case "Sword": return acc(W45("sword", { blade: bladeOf, guard, fullerK: gl || "S" }));
    case "Greatsword": return acc(W45("greatsword", { blade: bladeOf, guard, fullerK: gl || "S" }));
    case "Rapier": return W45("rapier", { guard });
    case "Dagger": return acc(diag("dagger", { blade: bladeOf, guard }, { s: 1.2 }));
    case "RitualBlade": return all(diag("dagger", { blade: "x", guard: "u", grip: "r" }, { s: 1.2 }), g("drop", { k: "K" }, { x: 11, y: 3, s: 0.45 }));
    case "DualDaggers": return acc(g("dual", { blade: bladeOf, guard }));
    case "Axe": return g("axe", { head: m === "s" ? "s" : m }, { r: 30, s: 0.85, x: 7.5, y: 7.5 });
    case "Greataxe": return acc(g("axe", { head: m === "s" ? "s" : m, double: true }, { r: 20, s: 0.8, x: 7, y: 7.5 }));
    case "Warhammer": return acc(g("hammer", { head: m === "G" ? "G" : "S", band: m === "G" ? "H" : "s" }, { r: 35, s: 0.95 }));
    case "Mace": return acc(g("mace", { head: m === "s" ? "s" : m, band: "G" }, { r: 35 }));
    case "Spear": return W45("spear", {});
    case "Polearm": return W45("glaive", { head: bladeOf });
    case "Scythe": return all(I("scythe", { head: "S", edge: "C", haft: "x" }), spark("C", 11.5, 11, 0.5));
    case "Gauntlet": return g("fist", { k: "s", wrap: "S", crease: "S" });
    case "Fist": case "FistWeapon":
      if (has(n, /knuckle$/) && !has(n, /wraps/)) return g("knuckle", { k: m === "S" ? "S" : "z" });
      return all(g("fist", { wrap: has(n, /ash/) ? "o" : "c" }), gl ? spark(gl, 11.5, 2.6, 0.6) : () => {});
    case "Bow": return acc(W45("bow", { limb: m === "x" ? "x" : m === "b" ? "b" : "w", gripK: gl === "Y" ? "Y" : "l" }));
    case "Crossbow": return g("crossbow", { stock: "w", limb: has(n, /emerose/) ? "r" : "S" }, { y: 7.5 });
    case "Staff":
      return W45("staff", { gem: gl || "A", prongs: has(n, /runed|star|scepter|archmage/) ? "G" : null, crook: has(n, /oak|verdant|thorn|root/), haft: has(n, /root|thorn|verdant/) ? "V" : "w" });
    case "Wand": return diag("wand", { gem: gl || "A", haft: has(n, /tide/) ? "q" : "w" }, { s: 1.05 });
    case "Rod": return W45("staff", { gem: "D", prongs: "u", haft: "x" });
    case "Scepter": return diag("scepter", { gem: "K" }, { s: 1 });
    case "Orb": return g("orbHeld", { k: "A" });
    case "HolySymbol": return g("holy", { k: m === "s" ? "s" : m });
    case "Tome": return I("book", { cover: gl === "D" || m === "x" ? "x" : "u", spine: "l", sigil: (c) => G.star(c, { n: 5, k: gl === "D" ? "D" : "A", core: false }) });
    case "Shield": return shieldIcon(n, m, gl);
    case "OffHandWeapon":
      if (has(n, /handbow/)) return g("crossbow", {}, { s: 0.8 });
      if (has(n, /claw/)) return g("claw", { k: "b" });
      return acc(diag("dagger", { blade: bladeOf, guard: "G" }, { s: 1.2 }));
    case "SpecialFocus":
      if (has(n, /reliquary/)) return all(I("lantern", { glow: "H", k: "G" }));
      if (has(n, /eclipse/)) return g("blackSun", {}, { s: 0.95 });
      return g("crystal", { k: "A" });
  }
  switch (slot) {
    case "SecondHand": return shieldIcon(n, m, gl);
    case "HeadArmor": return headIcon(n, m, gl);
    case "BodyArmor": return bodyIcon(n, m, gl);
    case "LegArmor": return legIcon(n, m, gl);
    case "FootArmor": return footIcon(n, m, gl);
    default: return accIcon(n, m, gl);
  }
}
function shieldIcon(n, m, gl) {
  if (has(n, /coffin/)) return I("coffin", { k: "x", cross: "S" });
  if (has(n, /buckler/)) return I("shield", { round: true, rim: "S", face: "w", boss: "s" });
  if (has(n, /tower/)) return I("shield", { tower: true, rim: "S", face: "s", boss: "S" });
  if (has(n, /obsidian/)) return I("shield", { rim: "x", face: "x", boss: "D" });
  if (m === "G" || gl === "H") return I("shield", { rim: "G", face: "H", cross: "G" });
  return I("shield", { rim: "s", face: m === "S" ? "S" : "r", boss: "G" });
}
function headIcon(n, m, gl) {
  if (has(n, /antler/)) return I("antlers", { leaf: "N" });
  if (has(n, /hood|cowl/)) return I("hood", { k: has(n, /scout|leather/) ? "l" : has(n, /blight/) ? "m" : has(n, /scholar/) ? "u" : "x", eyes: gl || null });
  if (has(n, /crown/)) return I("crown", { k: has(n, /cinder/) ? "E" : has(n, /drowned/) ? "S" : "G", gem: gl || "K" });
  if (has(n, /circlet|halo|diadem/)) return I("circlet", { k: "G", gem: gl || "B" });
  if (has(n, /headband/)) return I("headband", {});
  if (has(n, /cap/)) return I("cap_", { feather: "r" });
  if (has(n, /mask/)) return I("mask", { k: "S", bars: "S" });
  if (has(n, /laurel/)) return I("laurel", { k: "G", gem: "K" });
  if (has(n, /veil/)) return I("veil", { eyes: "C" });
  if (has(n, /antler/)) return I("antlers", { leaf: "N" });
  return I("helm", { k: m === "x" ? "S" : m === "b" ? "b" : "s", horns: has(n, /horn|dwarven/) ? "b" : null, crest: has(n, /guard|iron helm/) ? "r" : null });
}
function bodyIcon(n, m, gl) {
  const kind = has(n, /hauberk|chain|brigandine/) ? "chain" : has(n, /harness/) ? "harness" : has(n, /tunic/) ? "tunic" : has(n, /jerkin|coat/) ? "coat"
    : has(n, /vest$|barkskin vest/) ? "vest" : has(n, /vestment|robe|mantle|chasuble|weave/) ? "robe" : "plate";
  const k = kind === "robe" ? (gl === "H" || has(n, /linen|chasuble/) ? "c" : has(n, /ashen/) ? "o" : gl === "D" || has(n, /dark/) ? "x" : "u")
    : kind === "coat" || kind === "tunic" ? (has(n, /cloth/) ? "c" : "l") : kind === "harness" ? (has(n, /dragon/) ? "r" : "l")
    : kind === "vest" ? "V" : kind === "chain" ? "S" : m === "x" ? "x" : m === "G" ? "G" : "s";
  return I("torso", { kind, k, trim: kind === "robe" ? (gl === "D" ? "D" : "G") : kind === "plate" ? (m === "G" ? "z" : "S") : "l", gem: gl && kind !== "chain" ? gl : null });
}
function legIcon(n, m, gl) {
  if (has(n, /girdle/)) return I("belt", { k: "l", buckle: "G" });
  if (has(n, /sash/)) return I("legs", { kind: "sash", k: "r", gem: "G" });
  if (has(n, /shackle/)) return I("legs", { kind: "shackles", k: "x" });
  const greaves = has(n, /greave|legguard/);
  return I("legs", { kind: greaves ? "greaves" : "pants", k: greaves ? (m === "G" ? "G" : m === "b" ? "b" : m === "x" ? "x" : "S") : has(n, /fur/) ? "z" : has(n, /cloth|trouser/) ? "c" : "l", trim: gl || "S", knee: m === "G" ? "H" : "s" });
}
function footIcon(n, m, gl) {
  if (has(n, /sandal/)) return I("bootI", { kind: "sandal", k: "l", wing: has(n, /wing/) ? "c" : null });
  if (has(n, /tabi/)) return I("bootI", { kind: "tabi", k: "c", sole: "Y" });
  const plate = has(n, /sabaton|iron|greave|calamity|tread/);
  return I("bootI", { kind: plate ? "plate" : "boot", k: plate ? (m === "x" ? "x" : "S") : has(n, /soft/) ? "z" : "l", cuff: gl || (plate ? "s" : "c"), wing: has(n, /wing|strider/) ? "c" : null });
}
function accIcon(n, m, gl) {
  const gem = gl || "B";
  if (has(n, /locket/)) return I("locket", {});
  if (has(n, /ring|band|signet/)) return I("ring", { k: has(n, /copper/) ? "z" : has(n, /thief/) ? "S" : "G", gem: has(n, /keen/) ? "s" : gem });
  if (has(n, /medal/)) return I("medal", { k: "z", ribbon: "r", inner: "G" });
  if (has(n, /fang|talisman/)) return I("fang", {});
  if (has(n, /anklet/)) return I("anklet", { charm: "C" });
  if (has(n, /feather/)) return g("feather", { k: "z", tip: "l" }, { r: 40 });
  if (has(n, /rosary/)) return g("beads", {});
  if (has(n, /crystal/)) return g("crystal", { k: "B" });
  if (has(n, /seed/)) return g("seed", {});
  if (has(n, /seal/)) return all(g("coin", { k: "x", inner: "D", r: 5.6 }), g("star", { n: 5, k: "D", core: false }, { s: 0.45 }));
  if (has(n, /compass/)) return I("compass", {});
  if (has(n, /chronicle/)) return I("book", { cover: "l", sigil: (c) => G.star(c, { n: 5, k: "G", core: false }) });
  if (has(n, /covenant/)) return all(I("scroll", { paper: "c", ink: "R" }), g("drop", { k: "K" }, { x: 7, y: 7.5, s: 0.5 }));
  if (has(n, /black sun/)) return g("blackSun", {});
  if (has(n, /pearl/)) return g("pearl", {});
  if (has(n, /heart/)) return g("heart", { k: has(n, /hollow/) ? "D" : "K" }, { s: 1.05 });
  if (has(n, /eye/)) return g("eye", { iris: has(n, /wyrm/) ? "F" : "K" });
  if (has(n, /girdle/)) return I("belt", { k: "l", buckle: "G" });
  return I("pendant", { gem });
}

// ---------------- skills ----------------
const sword = (o = {}, t = {}) => diag("sword", o, t);
const SKILLS = {
  // fighter
  shield_bash: all(I("shield", { boss: "G" }, { x: -0.5, y: 1.5, s: 0.8 }), spark("T", 10.8, 3.4, 0.85)),
  power_strike: all(sword({}, { s: 0.9, x: 6.5, y: 7.5 }), spark("T", 10.8, 3.2, 0.9)),
  guard_stance: all(sword({}, { r: -45, s: 0.85 }), I("shield", { boss: "G" }, { x: 2.8, y: 3.4, s: 0.6 })),
  fortify: all(I("shield", { cross: "G" }, { x: 0, y: 1, s: 0.85 }), up()),
  interpose: all(g("heart", { k: "K" }, { x: 10, y: 10, s: 0.55 }), I("shield", { boss: "G" }, { x: 0.5, y: 0.5, s: 0.72 })),
  cleaving_strike: all(g("slash", { k: "T", r: 6.2, w: 1.5 }, { y: 5.5 }), sword({}, { r: -60, s: 0.6, x: 4.5, y: 9.5 })),
  shield_wall: all(I("shield", {}, { x: -1, y: 0.5, s: 0.6 }), I("shield", {}, { x: 6.6, y: 0.5, s: 0.6 }), I("shield", { boss: "G" }, { x: 2.3, y: 2.6, s: 0.68 })),
  weapon_disarm: all(g("hand", {}, { x: 4.5, y: 9.5, s: 0.55 }), sword({}, { r: 120, s: 0.6, x: 9, y: 5 }), g("mark", { k: "T" }, { x: 6.5, y: 3.5, r: -30, s: 0.6 })),
  counterattack: all(g("whirl", { k: "T" }, { s: 0.95 }), sword({}, { s: 0.65 })),
  armor_break: all(I("torso", { kind: "plate", k: "s" }, { x: 1.5, y: 1.5, s: 0.8 }), g("crack", { k: "k" }, { x: 7, y: 8, s: 0.85 }), spark("T", 11.5, 2.5, 0.7)),
  tactical_strike: all(g("target", { k: "r" }), sword({}, { s: 0.6 })),
  whirlwind: all(g("whirl", { k: "T" }, { s: 1.15 }), sword({}, { s: 0.5 })),
  iron_fortress: I("castle", { k: "S", line: "s" }),
  execution_strike: all(g("skull", {}, { x: 7, y: 9.6, s: 0.62 }), sword({}, { r: 180, s: 0.72, x: 7, y: 4.4 })),
  blade_tempest: all(g("swirl", { k: "T" }, { s: 1.15 }), ...[0, 120, 240].map((r) => (c) => c.at({ x: 7, y: 7, r: r + 15 }, () => c.at({ y: -3.8, s: 0.55 }, () => G.sword(c, {}))))),
  unbreakable: all(I("shield", { rim: "G", face: "H", boss: "W" }), spark("W", 2.5, 2.5, 0.5)),
  final_verdict: all(g("sun", {}, { s: 0.7, y: 4.5 }), sword({ guard: "G" }, { r: 180, s: 0.78, y: 7.8 })),
  // shared / legacy
  reckless_cleave: all(g("axe", {}, { r: 30, s: 0.8 }), g("drop", { k: "K" }, { x: 3, y: 10.5, s: 0.5 })),
  war_cry: all(g("horn", {}, { x: 5.5, y: 8, s: 0.75 }), g("sound", { k: "T" }, { x: 9, y: 6, s: 0.6 })),
  quick_strike: all(g("mark", { k: "T" }, { x: 5.5, y: 8 }), diag("dagger", {}, { x: 9, y: 6, s: 0.95 })),
  expose: all(g("eye", { iris: "K" }, { s: 0.9 }), down("K")),
  evasion: all(g("mark", { k: "T" }, { x: 6, y: 7 }), g("wing", { k: "c" }, { x: 8.5, y: 7.5, s: 0.9 })),
  aimed_shot: all(g("target", { k: "r" }), diag("arrow", {}, { s: 0.7 })),
  piercing_volley: all(diag("arrow", { head: "A" }, { s: 0.62, x: 4.5, y: 8 }), diag("arrow", { head: "A" }, { s: 0.62, x: 7, y: 7 }), diag("arrow", { head: "A" }, { s: 0.62, x: 9.5, y: 6 })),
  arcane_missile: comet("A"),
  eldritch_pact: all(g("hand", { k: "f" }, { y: 8.5, s: 0.9 }), g("orb", { k: "D", r: 2.2 }, { x: 7, y: 2.8 })),
  soul_hex: all(g("swirl", { k: "D" }, { s: 1.1 }), g("skull", { eye: "C" }, { s: 0.6 })),
  thorn_whip: g("vine", { k: "m", thorn: "b" }, { r: -30 }),
  // barbarian
  savage_strike: all(g("claw", { k: "K" }, { x: 4.5, s: 0.6 }), g("axe", {}, { r: 30, s: 0.8, x: 8, y: 7.5 })),
  berserk: all(g("flame", { k: "K", core: false }, { s: 1.2, y: 7.5 }), g("fist", {}, { s: 0.6, y: 9 })),
  intimidating_roar: all(g("skull", { eye: "K" }, { x: 5.5, y: 7.5, s: 0.85 }), g("sound", { k: "K" }, { x: 9.5, y: 7, s: 0.65 })),
  crushing_blow: all(g("hammer", {}, { r: -30, s: 0.85, x: 8, y: 6 }), g("crack", {}, { x: 4, y: 10, s: 0.5 })),
  reckless_swing: all(g("slash", { k: "K", r: 6, w: 1.3 }, { y: 6 }), g("axe", {}, { r: -20, s: 0.7, x: 6, y: 8 })),
  blood_rush: all(g("mark", { k: "K" }, { x: 5, y: 8 }), g("drop", { k: "K" }, { x: 9, y: 7, s: 1 })),
  rampage: g("claw", { k: "K" }, { s: 1.1 }),
  blood_frenzy: all(g("flame", { k: "F", core: false }, { s: 1.2, y: 7.5 }), g("heart", { k: "K" }, { s: 0.65, y: 8.5 })),
  brutal_cleave: all(g("slash", { k: "K", r: 6.2, w: 1.4 }, { y: 6 }), g("axe", { double: true }, { r: 25, s: 0.75, x: 7, y: 7.5 })),
  devastating_smash: all(g("burst", { k: "E", coreK: "F", r1: 6.4 }, { y: 9.5, s: 0.75 }), g("hammer", {}, { r: -15, s: 0.75, x: 7, y: 5.5 })),
  death_rush: all(g("mark", { k: "K" }, { x: 4.5, y: 8 }), g("skull", { eye: "K" }, { x: 8.5, y: 7.5, s: 0.8 })),
  barbarian_war_cry: all(g("horn", { k: "b", band: "r" }, { x: 5.5, y: 8, s: 0.75 }), g("sound", { k: "K" }, { x: 9, y: 6, s: 0.6 })),
  barbarian_cataclysm: all(g("burst", { k: "F", coreK: "W", n: 8 }), g("axe", { double: true }, { s: 0.55 })),
  wrath_incarnate: all(g("flame", { k: "K", core: false }, { s: 1.3, y: 7 }), (c) => { c.px(5.5, 8, "W"); c.px(8.5, 8, "W"); }),
  primal_carnage: all(g("claw", { k: "K" }, { s: 1.05 }), g("skull", { eye: "K" }, { x: 10.5, y: 10.5, s: 0.42 })),
  // martial artist
  quick_jab: all(g("mark", { k: "T" }, { x: 4.5, y: 7.5 }), g("fist", {}, { x: 9, y: 7.5, s: 0.8 })),
  focused_breathing: all(g("swirl", { k: "C" }, { s: 1.1 }), g("orb", { k: "C", r: 1.4 })),
  rising_kick: all(g("foot", {}, { r: -40, s: 0.95, x: 6.5, y: 8 }), g("up", { k: "T" }, { x: 3, y: 3.5, s: 0.6 })),
  flowing_combo: all(g("fist", { k: "T", wrap: "T", crease: "T", thumb: "T", wrapLine: "T" }, { x: 5, y: 5.5, s: 0.62 }), g("fist", {}, { x: 9, y: 8.6, s: 0.7 }), g("wave", { k: "T" }, { y: 12.3, s: 0.8 })),
  palm_strike: all(g("hand", {}, { s: 0.95 }), g("crack", { k: "T" }, { x: 11.5, y: 7, s: 0.55 })),
  sweep_kick: all(g("slash", { k: "T", r: 5.5 }, { y: 7, r: 180 }), g("foot", {}, { y: 8.5, s: 0.85 })),
  spinning_kick: all(g("whirl", { k: "T" }, { s: 1.05 }), g("foot", {}, { s: 0.65 })),
  counterstrike: all(g("whirl", { k: "C" }, { s: 0.95 }), g("fist", {}, { s: 0.6 })),
  dragon_fist: all(g("dragon", { k: "F" }, { x: 7, y: 5.5, s: 0.85 }), g("fist", {}, { x: 5, y: 9.5, s: 0.6 })),
  hundred_fists: all(g("mark", { k: "T" }, { x: 3, y: 7 }), g("fist", { k: "T", wrap: "T", crease: "T", thumb: "T", wrapLine: "T" }, { x: 6, y: 3.8, s: 0.5 }), g("fist", { k: "T", wrap: "T", crease: "T", thumb: "T", wrapLine: "T" }, { x: 6, y: 10.4, s: 0.5 }), g("fist", { k: "T", wrap: "T", crease: "T", thumb: "T", wrapLine: "T" }, { x: 7.5, y: 7, s: 0.55 }), g("fist", {}, { x: 9.5, y: 7, s: 0.62 })),
  perfect_counter: all(g("hand", {}, { s: 0.85, y: 8 }), spark("H", 7, 2.5, 0.8)),
  flying_knee: all(g("mark", { k: "T" }, { x: 3.5, y: 9.5, s: 0.8 }), g("knee", {}, { x: 7.8, y: 7, s: 1.1 })),
  ascendant_strike: all(g("star", { n: 8, k: "H", r1: 6.5, r0: 2.5, core: false }, { y: 8 }), g("fist", {}, { y: 7, s: 0.7 })),
  final_technique: all(g("burst", { k: "Y", coreK: "W" }), g("fist", {}, { s: 0.55 })),
  dragons_fury: all(g("flame", { k: "F", core: false }, { s: 1.2, y: 8 }), g("dragon", { k: "K", horn: "E" }, { s: 0.85, y: 7 })),
  // rogue
  quick_slash: all(g("slash", { k: "T" }, { y: 5.5 }), diag("dagger", {}, { x: 6, y: 8.5, s: 0.85 })),
  smoke_bomb: all(g("smoke", { k: "Z" }, { y: 8.5 }), I("bomb", {}, { x: 0, y: 0, s: 0.55 })),
  backstab: all(I("hood", { k: "x" }, { x: 0, y: 1, s: 0.85 }), diag("dagger", {}, { r: 200, x: 10, y: 6, s: 0.9 })),
  feint: all(diag("dagger", { blade: "D", guard: "D", grip: "D", pommel: "D" }, { x: 5, y: 8, s: 0.95 }), diag("dagger", {}, { x: 9, y: 6, s: 0.95 })),
  shadow_strike: all(g("slash", { k: "D" }, { y: 5.5 }), diag("dagger", { blade: "x" }, { x: 6, y: 8.5, s: 0.85 })),
  poison_blade: all(diag("dagger", { blade: "O" }, { s: 1.2 }), g("drop", { k: "O" }, { x: 3.5, y: 4, s: 0.5 })),
  assassinate: all(g("skull", {}, { s: 0.85, x: 6, y: 8 }), diag("dagger", {}, { r: 200, x: 9.5, y: 5.5, s: 0.85 })),
  shadowstep: all(g("mark", { k: "D" }, { x: 6, y: 7.5 }), I("hood", { k: "x", eyes: "D" }, { x: 5, y: 1.5, s: 0.75 })),
  weakpoint_strike: all(g("target", { k: "K" }), diag("dagger", {}, { s: 0.75 })),
  death_mark: all(g("target", { k: "K" }), g("skull", {}, { s: 0.55 })),
  vanishing_act: all(I("hood", { k: "x" }, { x: 2, y: 0.5, s: 0.7 }), g("smoke", { k: "Z" }, { y: 9.5, s: 0.9 })),
  phantom_dance: all(...[0, 120, 240].map((r) => (c) => c.at({ x: 7, y: 7, r }, () => c.at({ y: -3.6, r: 45, s: 0.55 }, () => G.dagger(c, { blade: "D" })))), g("swirl", { k: "D" }, { s: 0.45 })),
  thousand_cuts: all(...[-4, -1.5, 1, 3.5].map((d) => (c) => c.at({ x: 7 + d, y: 7 - d * 0.2, r: 45 }, () => c.cap([[0, -5], [0, 5]], 0.2, 0.8, "T"))), (c) => c.at({ x: 7, y: 7, r: -45 }, () => c.cap([[0, -6], [0, 6]], 0.2, 0.9, "T"))),
  shadow_requiem: all(g("moon", { k: "D" }, { x: 6.5, y: 6.5, s: 1.05 }), diag("dagger", { blade: "x" }, { x: 9.5, y: 8.5, s: 0.7 })),
  final_ambush: all(I("hood", { k: "x", eyes: "K" }, { x: 2, y: 0, s: 0.7 }), g("dual", {}, { y: 9.5, s: 0.6 })),
  // ranger
  quick_shot: all(g("mark", { k: "T" }, { x: 5, y: 9 }), diag("arrow", {}, { x: 8.5, y: 6, s: 0.85 })),
  hunter_mark: all(g("target", { k: "r" }), g("eye", { iris: "N" }, { s: 0.45 })),
  evasive_shot: all(diag("bow", {}, { x: 5, y: 9, s: 0.7 }), diag("arrow", {}, { x: 9, y: 5, s: 0.7 }), g("mark", { k: "T" }, { x: 4.5, y: 4, s: 0.6 })),
  piercing_arrow: all((c) => c.ring(7, 7, 3.6, 4.6, "T"), diag("arrow", {}, { s: 0.85 })),
  crippling_shot: all(diag("arrow", {}, { s: 0.8, x: 6, y: 7 }), down("K")),
  volley: all(diag("arrow", {}, { s: 0.62, x: 4.5, y: 8 }), diag("arrow", {}, { s: 0.62, x: 7, y: 7 }), diag("arrow", {}, { s: 0.62, x: 9.5, y: 6 })),
  snipe: all(g("target", { k: "r" }, { s: 0.75, x: 9, y: 5 }), diag("arrow", {}, { s: 0.55, x: 4.5, y: 9.5 })),
  trap_setup: I("trap", {}),
  explosive_arrow: all(diag("arrow", {}, { s: 0.7, x: 5.5, y: 8.5 }), g("burst", { k: "F", r1: 4.6, r0: 2, coreR: 1.4 }, { x: 10, y: 4 })),
  shadow_shot: all((c) => c.at({ x: 7, y: 7, r: 45 }, () => c.cap([[0, 1], [0, 8]], 0.4, 1.8, "D")), diag("arrow", { head: "x", fletch: "u" }, { s: 0.85 })),
  pinning_shot: all((c) => c.rect(1.5, 11.6, 12.5, 12.8, "o"), g("crack", { k: "k" }, { x: 7, y: 12, s: 0.35 }), diag("arrow", {}, { r: 180, s: 0.72, x: 7, y: 5.5 })),
  rain_of_arrows: g("rain", {}),
  deadeye: all(g("eye", { iris: "K" }), (c) => c.at({ x: 7, y: 7 }, () => { c.line(0, -6, 0, -3.6, "r"); c.line(0, 3.6, 0, 6, "r"); })),
  perfect_hunt: all(diag("bow", { limb: "G" }, { s: 0.9 }), spark("H", 11, 3, 0.75)),
  natures_wrath_shot: all(g("vine", { k: "N", thorns: false }, { r: -45, s: 0.8, x: 5.5, y: 8.5 }), diag("arrow", { head: "n", fletch: "n" }, { s: 0.75, x: 8, y: 6 })),
  // cleric
  heal: all(g("cross", { k: "H" }, { s: 1.2 }), spark("W", 11.2, 2.8, 0.5)),
  blessing: all(g("hand", {}, { y: 8.5, s: 0.85 }), spark("H", 7, 2.4, 0.75), spark("W", 11.5, 4.5, 0.4), spark("W", 2.5, 4.5, 0.4)),
  holy_light: g("star", { n: 8, k: "H", r1: 6.6, r0: 2.2, coreR: 1.8 }),
  mend: all(I("book", { cover: "c", spine: "G", sigil: (c) => G.cross(c, { k: "H" }) })),
  divine_shield: I("shield", { rim: "G", face: "H", cross: "W" }),
  purify: all(g("drop", { k: "I" }, { s: 1.15, y: 7.5 }), spark("W", 11, 3, 0.5), spark("W", 3, 10.5, 0.4)),
  prayer_of_healing_all: all(g("ripple", { k: "H" }), g("cross", { k: "H" }, { s: 0.45 })),
  sanctuary: all(g("halo", { k: "H" }, { y: 3, s: 0.8 }), I("castle", { k: "c", line: "G" }, { x: 2, y: 4, s: 0.72 })),
  holy_smite: all(g("star", { n: 4, k: "H", r1: 4.5 }, { x: 10.5, y: 3.5, s: 0.8 }), g("mace", { head: "G" }, { r: 35, s: 0.8, x: 6.5, y: 8 })),
  resurrection: all(g("angel", { k: "c" }, { y: 8, s: 0.8 }), g("up", { k: "H" }, { y: 9, s: 0.6 })),
  guardian_angel: g("angel", { k: "c" }, { y: 8 }),
  judgment: all(g("halo", { k: "H" }, { y: 2.6, s: 0.7 }), sword({ guard: "G", blade: "s" }, { r: 180, s: 0.72, y: 8 })),
  miracle: all(g("star", { n: 8, k: "H", r1: 6.6, r0: 1.6, coreR: 1.4 }, { rot: 0.39 }), g("star", { n: 4, k: "W", r1: 4, r0: 1, core: false })),
  divine_intervention: all(g("star", { n: 6, k: "H", r1: 6, r0: 2, core: false }, { y: 9 }), g("hand", { k: "c" }, { r: 180, y: 4.8, s: 0.8 })),
  heavens_wrath: all(g("cloud", { k: "c" }, { y: 3.5, s: 0.8 }), g("bolt", { k: "H" }, { y: 8.5, s: 0.9 })),
  // wizard
  firebolt: comet("F"),
  frost_nova: all(g("ripple", { k: "I" }), g("snow", {}, { s: 0.42 })),
  arcane_bolt: comet("A"),
  lightning_strike: all(g("cloud", { k: "Z" }, { y: 3.2, s: 0.8 }), g("bolt", { k: "Y" }, { y: 8.6, s: 0.95 })),
  frost_shard: all(g("shard", {}, { r: 45, s: 1.2 }), g("mark", { k: "I" }, { x: 4.5, y: 9.5, r: 45, s: 0.5 })),
  arcane_burst: g("burst", { k: "A", coreK: "W" }),
  meteor: g("meteor", { k: "E", tail: "F" }),
  blizzard: all(g("cloud", { k: "c" }, { y: 4, s: 0.8 }), g("snow", {}, { x: 4.5, y: 10.5, s: 0.35 }), g("snow", {}, { x: 9.5, y: 10, s: 0.35 }), g("snow", {}, { x: 7, y: 12, s: 0.25 })),
  chain_lightning: all(g("bolt", {}, { x: 4, y: 5, s: 0.55, r: -20 }), g("bolt", {}, { x: 8, y: 8, s: 0.55, r: 20 }), g("bolt", {}, { x: 11, y: 4, s: 0.45 })),
  inferno: all(g("flame", { k: "F", core: false }, { x: 3.5, y: 9, s: 0.6 }), g("flame", { k: "F", core: false }, { x: 10.5, y: 9, s: 0.6 }), g("flame", {}, { y: 7, s: 1.05 })),
  absolute_zero: all((c) => c.ring(7, 7, 6, 6.8, "I"), g("snow", {}, { s: 1.05 })),
  arcane_overload: all(g("orb", { k: "A", r: 4.6 }), g("crack", { k: "W" }, { s: 0.7 })),
  cataclysm_spell: all(g("flame", { k: "F" }, { x: 4, y: 4.5, s: 0.5 }), g("snow", {}, { x: 10, y: 4.5, s: 0.38 }), g("bolt", {}, { x: 7, y: 10, s: 0.55 })),
  apocalypse_spell: all(g("shard", {}, { x: 3.5, y: 6, s: 0.65, r: 20 }), g("shard", {}, { x: 7, y: 7, s: 0.95, r: 20 }), g("shard", {}, { x: 10.5, y: 5.5, s: 0.65, r: 20 })),
  grand_arcanum: all((c) => c.ring(7, 7, 5.4, 6.4, "A"), g("star", { n: 6, k: "A", r1: 4.6, r0: 1.8, coreR: 1 })),
  // warlock
  shadow_bolt: comet("D", "A"),
  curse_of_weakness: all(g("fist", { k: "v", wrap: "x", crease: "x" }, { s: 0.85, x: 6, y: 7.5 }), down("D")),
  corruption: all(g("swirl", { k: "D" }, { s: 1.05 }), g("drop", { k: "O" }, { s: 0.5 })),
  curse_of_vulnerability: all(I("shield", { rim: "x", face: "v" }, { x: 0.5, y: 0.5, s: 0.85 }), g("crack", { k: "D" }, { x: 6.5, y: 6.5, s: 0.7 })),
  soul_drain: all(g("hand", { k: "v" }, { x: 5, y: 8.5, s: 0.8 }), g("flame", { k: "C" }, { x: 10.5, y: 4, s: 0.5 })),
  shadow_burst: g("burst", { k: "D", coreK: "A" }),
  curse_of_agony: all(g("skull", { eye: "K" }, { y: 6.5 }), g("drop", { k: "K" }, { x: 5, y: 11.5, s: 0.4 }), g("drop", { k: "K" }, { x: 9, y: 12, s: 0.35 })),
  soul_harvest: all(I("scythe", { head: "S", haft: "x", edge: "C" }), g("flame", { k: "C" }, { x: 10.5, y: 10, s: 0.5 })),
  hex: all((c) => c.ring(7, 7, 5.4, 6.4, "D"), g("star", { n: 5, k: "D", r1: 5, r0: 2, core: false })),
  curse_of_doom: all(g("skull", { k: "v", eye: "K" }, { y: 7.5 }), g("hourglass", { k: "q", sand: "K" }, { x: 11.5, y: 3.2, s: 0.4 })),
  soul_collapse: all(g("swirl", { k: "C" }, { s: 1.1 }), g("crack", { k: "W" }, { s: 0.5 })),
  nightfall: all(g("moon", { k: "D" }, { x: 6, s: 1.05 }), spark("W", 11.2, 4, 0.55), spark("W", 10.5, 11, 0.4)),
  absolute_damnation: all(g("flame", { k: "D", core: false }, { s: 1.3, y: 7 }), g("skull", { eye: "K" }, { s: 0.6, y: 8 })),
  soul_annihilation: all(g("burst", { k: "C", coreK: "W" }), g("skull", { eye: "C" }, { s: 0.42 })),
  apocalypse_curse: g("blackSun", {}),
  // druid
  natures_embrace: all(g("heart", { k: "N" }, { s: 1.05, y: 7.5 }), g("leaf", { k: "n" }, { x: 10.8, y: 3.4, r: 40, s: 0.5 })),
  thorn_shot: all(g("mark", { k: "n" }, { x: 5, y: 8.5 }), g("shard", { k: "V" }, { r: 60, x: 9, y: 7, s: 0.9 })),
  entangling_roots: all(g("root", {}, { y: 8 }), g("vine", { k: "m", thorns: false }, { y: 9.5, s: 0.9 })),
  healing_bloom: all(g("flower", { petal: "X", core: "H" }), plus("N")),
  vine_lash: g("vine", { k: "m", thorn: "b" }, { r: -40, s: 1.1 }),
  barkskin: all(I("shield", { rim: "V", face: "V", boss: "n" })),
  regrowth: all(g("whirl", { k: "N" }, { s: 0.95 }), g("sprout", {}, { s: 0.7 })),
  natures_wrath: all(...[[3.5, 4, -30], [10, 4.5, 40], [5, 10, 120], [10, 10.5, 200], [7, 7, 0]].map(([x, y, r]) => g("leaf", { k: "N" }, { x, y, r, s: 0.45 }))),
  overgrowth: all(g("vine", { k: "m" }, { y: 4.5, s: 0.9 }), g("vine", { k: "m" }, { y: 9.5, s: 0.9, fx: true }), g("leaf", { k: "n" }, { x: 7, y: 7, s: 0.45 })),
  ancient_sanctuary: g("tree", { k: "n" }, { y: 7.5, s: 1.05 }),
  thorn_prison: all(...[3, 5.5, 8.5, 11].map((x) => (c) => c.cap([[x, 1], [x + (x % 2 ? 0.6 : -0.6), 13]], 0.6, 0.6, "V")), (c) => { c.line(1, 2, 13, 2, "m"); c.line(1, 12, 13, 12, "m"); }),
  wrath_of_the_forest: all(g("tree", { k: "m" }, { y: 7.5 }), (c) => { c.px(5.5, 4.5, "K"); c.px(8.5, 4.5, "K"); }),
  world_tree_blessing: all(g("tree", { k: "N" }, { y: 7.5 }), spark("W", 11.5, 2.5, 0.5), spark("W", 2.5, 3.5, 0.4)),
  primal_cataclysm: all(g("burst", { k: "N", coreK: "W", n: 8 }), g("leaf", { k: "n" }, { s: 0.4 })),
  natures_dominion: I("laurel", { k: "n", gem: "N" }),
};

// ---------------- passives ----------------
const PASSIVES = {
  weapon_proficiency: all(sword({}, { s: 0.85, x: 6.5, y: 7.5 }), up()),
  savage_instinct: all(g("claw", { k: "K" }, { s: 0.95 }), up("K")),
  martial_discipline: all(g("fist", {}, { s: 0.85, x: 6.5, y: 7.5 }), up()),
  opportunist: all(g("eye", { iris: "K" }, { s: 0.8, y: 5 }), diag("dagger", {}, { x: 7, y: 9.5, s: 0.6, r: 90 })),
  hunters_instinct: all(g("eye", { iris: "N" }, { s: 0.85, y: 7 }), up()),
  divine_grace: all(g("heart", { k: "H" }, { y: 8.6, s: 0.8 }), g("halo", { k: "H" }, { y: 2.6, s: 0.7 })),
  arcane_affinity: all(g("gem", { k: "A" }, { s: 1.1 }), down("B")),
  pact_affinity: all(g("hand", { k: "v" }, { y: 8.5, s: 0.85 }), g("star", { n: 5, k: "D", core: false }, { y: 3, s: 0.45 })),
  natures_attunement: all(g("leaf", { k: "N" }, { r: 30, s: 1 }), up()),
  defensive_training: all(I("shield", { boss: "G" }, { x: 0, y: 1, s: 0.85 }), up()),
  combat_discipline: all(g("hourglass", {}, { s: 0.95 }), sword({}, { s: 0.45, x: 11, y: 10.5 })),
  shield_mastery: all(I("shield", { rim: "G", face: "S", boss: "G" }), goldStar()),
  tactical_awareness: all(g("eye", {}, { s: 0.85, y: 6 }), g("target", { k: "r" }, { x: 10.5, y: 10.5, s: 0.4 })),
  weapon_adaptation: all(sword({}, { s: 0.8 }), g("axe", {}, { r: -45, s: 0.65, fx: true })),
  iron_will: all(I("helm", { k: "S" }, { x: 0.5, y: 1, s: 0.85 }), up()),
  counter_specialist: all(g("whirl", { k: "T" }, { s: 0.95 }), sword({}, { s: 0.6 }), goldStar()),
  master_of_arms: all(sword({ guard: "G" }, { s: 0.8 }), g("axe", { head: "G" }, { r: -45, s: 0.65, fx: true }), goldStar()),
  unyielding_defense: all(I("castle", { k: "S" }, { x: 0, y: 1.5, s: 0.75 }), I("shield", { boss: "G" }, { x: 6, y: 5.5, s: 0.55 })),
  battle_focus: all(g("target", { k: "r" }), g("plus", { k: "T" }, { s: 0.6 })),
  bloodthirst: all(g("drop", { k: "K" }, { s: 1.2, y: 7.5 }), plus("K")),
  unrelenting_fury: all(g("flame", { k: "K", core: false }, { s: 1.15 }), up("F")),
  pain_tolerance: all(g("heart", { k: "K" }, { s: 0.95 }), I("bandage", {}, { x: 7, y: 7, s: 0.45 })),
  predators_instinct: g("eye", { iris: "F", white: "y" }),
  berserkers_momentum: all(g("mark", { k: "K" }, { x: 4.5, y: 8 }), g("axe", {}, { r: 30, s: 0.7, x: 8.5, y: 7 })),
  unstoppable_rage: all(g("flame", { k: "K", core: false }, { s: 1.2 }), g("skull", { eye: "K" }, { s: 0.5, y: 8 })),
  blood_pact: all((c) => c.ring(7, 7, 5.2, 6.4, "K"), g("drop", { k: "K" }, { s: 0.9, y: 7.5 })),
  avatar_of_destruction: all(g("burst", { k: "K", coreK: "F" }), g("axe", { double: true }, { s: 0.5 })),
  endless_fury: all(g("flame", { k: "K", core: false }, { s: 1.1, y: 8 }), g("flame", { k: "F" }, { s: 0.6, y: 8.5 }), up("F")),
  combat_rhythm: all(g("hourglass", {}, { s: 0.9 }), g("fist", {}, { s: 0.42, x: 11, y: 10.5 })),
  combo_mastery: all(g("fist", {}, { x: 4.6, y: 6, s: 0.6 }), g("fist", {}, { x: 9.5, y: 8.5, s: 0.6 }), goldStar()),
  agile_defense: all(g("wing", { k: "c" }, { x: 8, y: 7.5 }), I("shield", { boss: "G" }, { x: 0.5, y: 4, s: 0.5 })),
  inner_focus: g("eye", { iris: "C" }),
  pressure_point: all(g("hand", {}, { s: 0.9 }), g("target", { k: "K" }, { x: 7, y: 8, s: 0.3 })),
  unbroken_flow: all(g("wave", { k: "T" }, { y: 5 }), g("wave", { k: "C" }, { y: 9 })),
  ki_control: all(g("hand", {}, { x: 3.5, y: 8, s: 0.6, r: 40 }), g("hand", {}, { x: 10.5, y: 8, s: 0.6, r: -40, fx: true }), g("orb", { k: "C", r: 2.4 }, { y: 6 })),
  grandmaster: all(g("fist", { wrap: "G" }, { s: 0.95 }), goldStar()),
  limitless_potential: all(g("star", { n: 8, k: "C", r1: 6, r0: 2.4, coreR: 1.2 }), up("H")),
  evasive_instinct: all(g("mark", { k: "T" }, { x: 5.5, y: 7 }), g("wing", { k: "v" }, { x: 8.5, y: 7.5, s: 0.9 })),
  precision: all(g("target", { k: "r" }), diag("dagger", {}, { s: 0.6 })),
  dirty_fighting: all(diag("dagger", { blade: "O" }, { s: 1.05 }), g("drop", { k: "O" }, { x: 3, y: 4, s: 0.45 }), up("K")),
  lethal_focus: all(g("eye", { iris: "K" }, { s: 0.75, y: 4.5 }), diag("dagger", {}, { r: 180, y: 9.5, s: 0.6 })),
  exploit_weakness: all(g("crack", { k: "K" }, { s: 0.95 }), g("target", { k: "r" }, { s: 0.55, x: 10.5, y: 10.5 })),
  shadow_affinity: all(g("moon", { k: "D" }), up("D")),
  executioner: all(g("skull", {}, { y: 8.5, s: 0.8 }), g("axe", {}, { r: 0, s: 0.45, x: 10.5, y: 4 })),
  master_assassin: all(I("hood", { k: "x", eyes: "K" }, { x: 0.5, y: 1, s: 0.85 }), goldStar()),
  deaths_embrace: all(g("skull", { k: "v", eye: "D" }), g("heart", { k: "K" }, { x: 11, y: 11, s: 0.4 })),
  keen_eye: g("eye", { iris: "N" }),
  marksmanship: all(g("target", { k: "r" }), diag("bow", {}, { s: 0.55 })),
  predators_focus: all(g("target", { k: "K" }), g("claw", { k: "K" }, { s: 0.4 })),
  deadly_aim: all(diag("arrow", {}, { s: 0.85, x: 6, y: 8 }), g("skull", {}, { x: 10.5, y: 3.5, s: 0.4 })),
  improved_tracking: I("compass", { needle: "N" }),
  eagle_eye: all(g("eye", { iris: "G" }, { s: 0.85, y: 8 }), g("feather", { k: "z" }, { x: 11, y: 4, s: 0.45, r: 40 })),
  tactical_hunter: all(I("shield", { boss: "G" }, { x: 0, y: 2, s: 0.65 }), diag("bow", {}, { x: 9.5, y: 6.5, s: 0.6 })),
  master_hunter: all(diag("bow", { limb: "G" }, { s: 0.9 }), goldStar()),
  apex_predator: all(g("claw", { k: "N" }, { s: 1.05 }), I("crown", { k: "G" }, { x: 7.5, y: 0, s: 0.45 })),
  sacred_vitality: all(g("heart", { k: "H" }, { s: 1.05 }), plus("N")),
  healing_hands: all(g("hand", {}, { s: 0.95, y: 8 }), plus("H")),
  protective_faith: all(I("shield", { face: "c", rim: "G", cross: "G" }), plus("H")),
  divine_aegis: I("shield", { round: true, rim: "G", face: "H", boss: "W" }),
  spiritual_focus: all(I("book", { cover: "c", spine: "G", sigil: (c) => G.star(c, { n: 4, k: "H" }) })),
  blessed_recovery: all(g("drop", { k: "H" }, { s: 1.1, y: 7.5 }), up("H")),
  unwavering_faith: g("holy", { k: "G", gem: "H" }),
  saints_resolve: all(I("shield", { rim: "G", face: "c", cross: "G" }, { x: 1.3, y: 3.2, s: 0.8 }), g("halo", { k: "H" }, { y: 2, s: 0.65 })),
  eternal_blessing: all(g("angel", { k: "H", halo: "G" }, { y: 8 }), goldStar()),
  elemental_knowledge: I("book", { cover: "e", spine: "u", sigil: (c) => { G.flame(c, { k: "F", core: false }); } }),
  spell_mastery: all(diag("wand", { gem: "A" }, { s: 0.95 }), goldStar()),
  mana_efficiency: all(g("crystal", { k: "B" }, { s: 0.95 }), down("B")),
  elemental_focus: all(g("orb", { k: "A", r: 2.4 }), g("flame", { k: "F" }, { x: 7, y: 2.2, s: 0.38 }), g("snow", {}, { x: 2.6, y: 10.6, s: 0.28 }), g("bolt", {}, { x: 11.4, y: 10.6, s: 0.38 })),
  arcane_precision: all(g("target", { k: "e" }), g("star", { n: 4, k: "A", r1: 3 }, { s: 0.6 })),
  spell_amplification: all(g("burst", { k: "A", coreK: "W" }, { s: 0.9 }), up("A")),
  magical_resilience: I("shield", { rim: "e", face: "A", boss: "W" }),
  archmage: I("hat", { star: "H" }),
  infinite_potential: all(g("star", { n: 8, k: "A", r1: 6.4, r0: 2.4, coreR: 1.4 })),
  lingering_curse: all(g("swirl", { k: "D" }, { s: 1.1 }), g("hourglass", { k: "q", sand: "D" }, { x: 11.5, y: 3, s: 0.38 })),
  curse_mastery: all(g("skull", { k: "v", eye: "D" }), goldStar()),
  dark_knowledge: I("book", { cover: "x", spine: "u", sigil: (c) => G.eye(c, { iris: "D", white: "v" }) }),
  malicious_intent: g("eye", { iris: "K", white: "v" }),
  dark_ritual: all((c) => c.ring(7, 7, 5.2, 6.4, "D"), g("flame", { k: "D" }, { s: 0.7 })),
  eternal_affliction: g("hourglass", { k: "q", sand: "D", frame: "x" }, { s: 1.05 }),
  forbidden_power: all(I("book", { cover: "x", spine: "u" }), g("chain", { k: "S" }, { s: 0.85 })),
  grand_curse_master: all(g("skull", { k: "v", eye: "D" }, { y: 8.5, s: 0.85 }), I("crown", { k: "D" }, { x: 3.5, y: -0.5, s: 0.5 })),
  abyssal_dominion: all(g("moon", { k: "D" }, { s: 1.05 }), g("eye", { iris: "K", white: "v" }, { s: 0.45, x: 6, y: 7 })),
  natural_recovery: all(g("sprout", {}), plus("N")),
  thorn_mastery: all(g("vine", { k: "m", thorn: "b" }, { r: -30 }), goldStar()),
  deep_roots: g("root", {}, { s: 1.1 }),
  flourishing_life: g("flower", { petal: "X", core: "N" }, { s: 1.05 }),
  wild_resilience: all(I("shield", { rim: "V", face: "m", boss: "n" })),
  ancient_knowledge: I("book", { cover: "m", spine: "V", sigil: (c) => G.leaf(c, { k: "N" }) }),
  natures_protection: all(I("shield", { rim: "V", face: "V" }, { x: 0, y: 1, s: 0.85 }), g("leaf", { k: "N" }, { x: 7, y: 6.8, s: 0.45 })),
  master_of_the_wild: I("antlers", { leaf: "N" }),
  eternal_bloom: all(g("flower", { petal: "H", core: "W" }), goldStar()),
};

module.exports = { ITEMS, SKILLS, PASSIVES, gearIcon };
