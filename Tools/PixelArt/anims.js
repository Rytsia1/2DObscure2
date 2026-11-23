// Hero combat animations: attack (melee swing or bow shot), cast (staff/hand raised with a glow), hit (flinch), KO (slump).
// Poses move whole part groups (legs, body, front arm + weapon, head) by a pixel or two, because turning parts of a
// 22x27 sprite only breaks its lines; smears, arrows and glows are drawn on top. The lunge toward the enemy is the
// combat screen sliding the whole sprite, not part of these frames.
// Run: node anims.js <outDir> [id,id]
const { P, R, pose, frames, DELAY } = require("./engine.js");
const { HEROES } = require("./heroes.js");

const put = (b, x, y, c, onlyEmpty = true) => {
  if (x < 0 || y < 0 || x >= b.w || y >= b.h) return;
  if (onlyEmpty && b.px[y * b.w + x] >>> 24) return;
  b.px[y * b.w + x] = P.col(c);
};

// arrow released along the bow's middle: a pale streak with a steel head
const arrow = (gone) => (b, { arm: [, y1, x2, y2] }) => {
  const y = Math.round((y1 + y2) / 2);
  for (let x = x2 + 1; x < b.w; x++) put(b, x, y, gone ? R.wood[2] : x === b.w - 1 ? R.steel[5] : R.bone[3]);
  if (gone) for (let x = x2 + 4; x < b.w; x++) b.px[y * b.w + x] = 0;
};
// spell glow on the staff head / hand: small spark, full burst ring, scattered sparkles
// (centred on the head itself, not above it, so it never leaves the frame)
const glow = (cols, size) => (b, { arm: [x1, y1, x2] }) => {
  const cx = Math.round((x1 + x2) / 2), cy = y1 + 1, [hi, mid] = cols;
  if (size === "sparkle") { for (const [dx, dy] of [[-3, -1], [3, 0], [-2, 2], [2, -3]]) put(b, cx + dx, cy + dy, mid); return; }
  if (size === "big") for (let a = 0; a < 360; a += 30) put(b, Math.round(cx + 3 * Math.cos((a * Math.PI) / 180)), Math.round(cy + 3 * Math.sin((a * Math.PI) / 180)), mid);
  for (const [dx, dy] of [[0, -1], [-1, 0], [1, 0], [0, 1]]) put(b, cx + dx, cy + dy, mid);
  put(b, cx, cy, hi, false);
};

// front arm + held weapon per hero (pixels left of minX stay with the body); style picks the action
const RIG = {
  knight: { arm: { letters: "SYR", minX: 11 }, style: "melee" },
  fighter: { arm: { letters: "XZqh", minX: 13 }, style: "melee" },
  barbarian: { arm: { letters: "wGEh", minX: 14 }, style: "melee" },
  martial_artist: { arm: { letters: "Ah", minX: 14 }, style: "melee" },
  rogue: { arm: { letters: "Xxh", minX: 14 }, style: "melee" },
  ranger: { arm: { letters: "wsh", minX: 13 }, style: "shoot" },
  cleric: { arm: { letters: "wYhA", minX: 13 }, style: "cast", glow: [R.gold[4], R.gold[2]] },
  wizard: { arm: { letters: "woOhA", minX: 12 }, style: "cast", glow: [R.orange[3], R.orange[1]] },
  warlock: { arm: { letters: "fFghA", minX: 13 }, style: "cast", glow: [R.cyan[3], R.cyan[1]] },
  // pale lime, not leaf green: a green glow vanishes against the druid's own leaves
  druid: { arm: { letters: "wlhA", minX: 13 }, style: "cast", glow: [R.bone[3], P.hsl(78, 0.65, 0.72)] },
};

const ANIMS = {
  // wind back, coil, strike, follow through, recover, stance (the impact effect plays on the target)
  melee: { delay: [110, 130, 70, 120, 120, 90], poses: () => [
    { b: [-1, 0], h: [-1, 0], a: [-2, -1] },
    { b: [-1, 0], h: [-1, 1], a: [-2, -2], squash: 1 },
    { b: [1, 0], h: [1, 0], a: [2, 0] },
    { b: [1, 0], h: [1, 1], a: [2, 1] },
    { b: [0, 0], a: [1, 0] },
    {},
  ] },
  // draw back, hold, release (arrow streak), recoil (streak leaving), stance
  shoot: { delay: [120, 160, 70, 120, 110], poses: () => [
    { b: [-1, 0], h: [-1, 0], a: [-1, 0] },
    { b: [-1, 0], h: [-1, 0], a: [-1, 0], squash: 1 },
    { fx: arrow(false) },
    { b: [-1, 0], h: [-1, 0], a: [-1, 0], fx: arrow(true) },
    {},
  ] },
  // raise, charge (spark), release (burst ring), sparkles fade, stance; `up` is capped by the headroom above the staff
  cast: { delay: [120, 160, 200, 140, 110], poses: (rig, up) => [
    { a: [0, -Math.min(1, up)] },
    { a: [0, -up], fx: glow(rig.glow, "small") },
    { a: [0, -up], fx: glow(rig.glow, "big") },
    { a: [0, -Math.min(1, up)], fx: glow(rig.glow, "sparkle") },
    {},
  ] },
  // knocked back, head snaps, stance
  hit: { delay: [90, 110, 90], poses: () => [
    { b: [-1, 0], h: [-2, 0], a: [-1, 0] },
    { b: [-1, 0], h: [-1, 0], a: [-1, 0] },
    {},
  ] },
  // knocked back, knees give, kneel, slump with the head bowed (held)
  ko: { delay: [100, 140, 160, 600], poses: () => [
    { b: [-1, 0], h: [-2, 0], a: [-1, 0] },
    { squash: 2, h: [-1, 1], a: [0, 1] },
    { squash: 4, h: [0, 3], a: [0, 3] },
    { squash: 5, h: [1, 5], a: [1, 6] },
  ] },
};

// all of a hero's combat clips: { action, hit, ko } -> { frames, delay }
function clips(id) {
  const def = { ...HEROES[id], arm: RIG[id].arm }, rig = RIG[id], act = ANIMS[rig.style];
  let top = 0; pose(def, { fx: (b, i) => { top = i.arm[1]; } });
  const up = Math.max(0, Math.min(2, top - 1)); // row 0 is the outline: the weapon may rise until it touches it
  const make = (a) => ({ frames: a.poses(rig, up).map((p) => pose(def, p)), delay: a.delay });
  return { action: make(act), hit: make(ANIMS.hit), ko: make(ANIMS.ko), style: rig.style };
}
// ---------------------------------------------------------------------------------------------------------------------
// Enemy attacks. Enemies are drawn facing right and mirrored, so pose offsets are in screen pixels: forward is -x.
// Arm groups are given in drawing (pre-mirror) coordinates.
const { ENEMIES } = require("./enemies.js");
const F = -1; // forward, for enemies facing left

const ERIG = {
  hollow_squire: { arm: { letters: "bcXQ", minX: 13 }, style: "melee" },
  gravebound_archer: { arm: { letters: "wsacXb", minX: 10 }, style: "shoot" },
  cinder_wisp: { style: "pounce" },
  soulbound_hound: { style: "pounce" },
  possessed_royal_guard: { arm: { letters: "wXZhA", minX: 12 }, style: "melee" },
  crypt_leech: { style: "pounce" },
  forgotten_militia: { arm: { letters: "wXxQzA", minX: 12 }, style: "melee" },
  soul_reaper: { arm: { letters: "wsZQha", minX: 12 }, style: "melee" },
  royal_ossuary_knight: { arm: { letters: "rDKQ", minX: 12 }, style: "melee" }, // shield bash
  // roster v1.9 Elites
  reliquary_confessor: { arm: { letters: "woGh", minX: 13 }, style: "melee" },  // reliquary staff swing
  graveglass_arcanist: { arm: { letters: "AQq", minX: 13 }, style: "melee" },   // glass shard thrust
  crownless_duelist: { arm: { letters: "AhgsZ", minX: 12 }, style: "melee" },   // rapier lunge
  tombstone_colossus: { arm: { letters: "AaFm", minX: 15 }, style: "melee" },   // monument fist
  mourning_bell_wraith: { arm: { letters: "ah", minX: 14 }, style: "melee" },   // echo claw
  // common supports
  ossuary_acolyte: { arm: { letters: "AhcCgs", minX: 13 }, style: "melee" },    // censer swing
  dirge_chanter: { style: "pounce" },                                          // mournful wail: the veiled head lunges
  hollow_banneret: { arm: { letters: "bhwfyX", minX: 12 }, style: "melee" },    // banner pole jab
  zoroa_p1: { arm: { letters: "AhcLlG", minX: 30 }, style: "reap", glow: [R.cyan[3], R.cyan[1]] },
  zoroa_p2: { arm: { letters: "AhcLlG", minX: 30 }, style: "reap", glow: [P.hsl(352, 0.85, 0.68), P.hsl(352, 0.8, 0.45)] },
};

// arrow loosed toward the party (leftward), from the bow's middle to the frame edge
const arrowLeft = (gone) => (b, { arm: [x1, y1, , y2] }) => {
  const y = Math.round((y1 + y2) / 2);
  for (let x = x1 - 1; x >= 0; x--) put(b, x, y, gone ? R.wood[2] : x === 0 ? R.steel[5] : R.bone[3]);
  if (gone) for (let x = x1 - 4; x >= 0; x--) b.px[y * b.w + x] = 0;
};
// soul lantern flaring: a spark, then a burst ring centred on the lantern (bottom of the arm group), then embers
// The lantern is the outermost, lowest part of the arm group: mirrored, that is the group's left edge.
const lantern = (cols, size) => (b, { arm: [x1, , , y2] }) => {
  const cx = x1 + 4, cy = y2 - 4, [hi, mid] = cols;
  if (size === "sparkle") { for (const [dx, dy] of [[-5, -2], [4, -4], [-3, 4], [5, 2]]) put(b, cx + dx, cy + dy, mid); return; }
  // small enough to stay in frame (the lantern hangs near the edge); the core burns bright on top of the lantern
  const r = size === "big" ? 5 : 3;
  for (let a = 0; a < 360; a += size === "big" ? 15 : 30) put(b, Math.round(cx + r * Math.cos((a * Math.PI) / 180)), Math.round(cy + r * Math.sin((a * Math.PI) / 180)), mid);
  for (const [dx, dy] of [[0, 0], [0, -1], [-1, 0], [1, 0], [0, 1]]) put(b, cx + dx, cy + dy, size === "big" ? hi : mid, false);
};

const EANIMS = {
  // wind back, coil, strike, follow through, recover, stance (mirrored hero melee)
  melee: { delay: [110, 130, 70, 120, 120, 90], poses: () => [
    { b: [-F, 0], h: [-F, 0], a: [-2 * F, -1] },
    { b: [-F, 0], h: [-F, 1], a: [-2 * F, -2], squash: 1 },
    { b: [F, 0], h: [F, 0], a: [2 * F, 0] },
    { b: [F, 0], h: [F, 1], a: [2 * F, 1] },
    { a: [F, 0] },
    {},
  ] },
  shoot: { delay: [120, 160, 70, 120, 110], poses: () => [
    { b: [-F, 0], h: [-F, 0], a: [-F, 0] },
    { b: [-F, 0], h: [-F, 0], a: [-F, 0], squash: 1 },
    { fx: arrowLeft(false) },
    { b: [-F, 0], h: [-F, 0], a: [-F, 0], fx: arrowLeft(true) },
    {},
  ] },
  // rear back and gather, then the head / jaws / flames lunge forward and snap back
  pounce: { delay: [130, 150, 70, 110, 120, 90], poses: () => [
    { b: [-F, 0], h: [-F, 1] },
    { b: [-F, 0], h: [-2 * F, 1], squash: 1 },
    { b: [F, 0], h: [3 * F, 1] },
    { b: [F, 0], h: [2 * F, 1] },
    { h: [F, 0] },
    {},
  ] },
  // the lantern rises and flares as the hood leans in, embers drift off, stance
  reap: { delay: [140, 180, 220, 160, 120], poses: (rig) => [
    { a: [0, -1] },
    { a: [0, -2], fx: lantern(rig.glow, "small") },
    { a: [F, -2], h: [2 * F, 1], fx: lantern(rig.glow, "big") }, // the hood lunges at the party as the lantern flares
    { a: [0, -1], fx: lantern(rig.glow, "sparkle") },
    {},
  ] },
};

// tutorial enemies bring their own rigs; rig.fx = { frame: effect } adds a strike effect (bolt, rift slash) to that frame
Object.assign(ERIG, require("./prologue.js").PROLOGUE_RIG);

function enemyAttack(id) {
  const rig = ERIG[id], def = { ...ENEMIES[id], arm: rig.arm }, a = EANIMS[rig.style];
  const poses = a.poses(rig).map((p, i) => (rig.fx && rig.fx[i] ? { ...p, fx: rig.fx[i] } : p));
  return { frames: poses.map((p) => pose(def, p)), delay: a.delay, style: rig.style };
}

module.exports = { RIG, ANIMS, clips, ERIG, EANIMS, enemyAttack };

// node anims.js --export-enemies : Assets/Resources/Enemies/<id>_atk_<i>.png plus Enemies/clips.json for UnitArt.cs
if (require.main === module && process.argv[2] === "--export-enemies") {
  const fs = require("fs"), path = require("path"), crypto = require("crypto");
  const assets = path.resolve(__dirname, "../../Assets"), dir = path.join(assets, "Resources/Enemies");
  const writeMeta = (file, src) => {
    if (!fs.existsSync(file + ".meta"))
      fs.writeFileSync(file + ".meta", fs.readFileSync(path.join(assets, src), "utf8").replace(/^guid: \w+/m, "guid: " + crypto.randomBytes(16).toString("hex")));
  };
  const list = [];
  for (const id of Object.keys(ERIG)) {
    const atk = enemyAttack(id);
    atk.frames.forEach((f, i) => { const file = path.join(dir, `${id}_atk_${i}.png`); P.save(f, file); writeMeta(file, "Resources/Fx/hit.png.meta"); });
    list.push({ hero: id, clip: "atk", style: atk.style, strike: 2, delays: atk.delay.map((d) => d / 1000) });
    console.log("exported", id, atk.style, atk.frames.length, `${atk.frames[0].w}x${atk.frames[0].h}`);
  }
  const json = path.join(dir, "clips.json");
  fs.writeFileSync(json, JSON.stringify({ clips: list }, null, 1));
  writeMeta(json, "Resources/Fx/hit.json.meta");
  process.exit(0);
}

// node anims.js --enemies <outDir> : preview sheet + one GIF per enemy (stance, attack, stance)
if (require.main === module && process.argv[2] === "--enemies") {
  const out = process.argv[3], fs = require("fs"), items = [];
  for (const id of Object.keys(ERIG)) {
    const atk = enemyAttack(id), idle = frames(ENEMIES[id]), big = atk.frames[0].w > 30;
    items.push({ buf: idle[0], label: id.slice(0, 8).toUpperCase(), scale: big ? 2 : 3 });
    atk.frames.forEach((f, i) => items.push({ buf: f, label: atk.style.slice(0, 2).toUpperCase() + i, scale: big ? 2 : 3 }));
    while (items.length % 7) items.push({ buf: new P.Buf(1, 1), label: "", scale: 3 });
    const seq = [...idle, ...atk.frames, ...idle], del = [...DELAY, ...atk.delay, ...DELAY];
    fs.writeFileSync(`${out}/combat_enemy_${id}.gif`, P.gif(seq, { delay: del, scale: big ? 3 : 5 }));
  }
  P.save(P.sheet(items, { cols: 7 }), out + "/combat_enemy_anims.png");
  process.exit(0);
}

// node anims.js --export : Assets/Resources/Heroes/<id>_<act|hit|ko>_<i>.png plus clips.json (frame delays, strike frame,
// style) for UnitArt.cs. Import settings are copied from the FX sheets; an existing .meta keeps its guid.
if (require.main === module && process.argv[2] === "--export") {
  const fs = require("fs"), path = require("path"), crypto = require("crypto");
  const assets = path.resolve(__dirname, "../../Assets"), dir = path.join(assets, "Resources/Heroes");
  const meta = (src) => fs.readFileSync(path.join(assets, src), "utf8").replace(/^guid: \w+/m, "guid: " + crypto.randomBytes(16).toString("hex"));
  const writeMeta = (file, src) => { if (!fs.existsSync(file + ".meta")) fs.writeFileSync(file + ".meta", meta(src)); };
  const list = [];
  for (const id of Object.keys(RIG)) {
    const c = clips(id);
    for (const [name, clip] of [["act", c.action], ["hit", c.hit], ["ko", c.ko]]) {
      clip.frames.forEach((f, i) => { const file = path.join(dir, `${id}_${name}_${i}.png`); P.save(f, file); writeMeta(file, "Resources/Fx/hit.png.meta"); });
      // strike: the frame the blow lands on (hits apply then); -1 for clips with no blow
      list.push({ hero: id, clip: name, style: name === "act" ? c.style : name, strike: name === "act" ? 2 : -1, delays: clip.delay.map((d) => d / 1000) });
    }
    console.log("exported", id, c.style, c.action.frames.length + "/" + c.hit.frames.length + "/" + c.ko.frames.length);
  }
  const json = path.join(dir, "clips.json");
  fs.writeFileSync(json, JSON.stringify({ clips: list }, null, 1));
  writeMeta(json, "Resources/Fx/hit.json.meta");
  process.exit(0);
}

if (require.main === module) {
  const out = process.argv[2], fs = require("fs"), only = process.argv[3];
  const ids = Object.keys(RIG).filter((id) => !only || only.split(",").includes(id));
  const items = [];
  for (const id of ids) {
    const c = clips(id), idle = frames(HEROES[id]);
    const row = [...c.action.frames.map((f, i) => ({ buf: f, label: (i ? "" : id.slice(0, 5).toUpperCase() + " ") + c.style.slice(0, 2).toUpperCase() + i, scale: 3 })),
      ...c.hit.frames.map((f, i) => ({ buf: f, label: "HIT" + i, scale: 3 })), ...c.ko.frames.map((f, i) => ({ buf: f, label: "KO" + i, scale: 3 }))];
    while (row.length < 13) row.push({ buf: new P.Buf(1, 1), label: "", scale: 3 });
    items.push(...row);
    // one GIF per hero: stance, the action, stance, a hit, stance, then the KO held
    const seq = [], del = [];
    const add = (fs2, ds) => fs2.forEach((f, i) => { seq.push(f); del.push(ds[i]); });
    add(idle, DELAY); add(c.action.frames, c.action.delay); add(idle, DELAY); add(c.hit.frames, c.hit.delay); add(idle, DELAY);
    add(c.ko.frames, c.ko.delay.map((d, i, a) => (i === a.length - 1 ? 1400 : d)));
    fs.writeFileSync(`${out}/combat_${id}.gif`, P.gif(seq, { delay: del, scale: 5 }));
  }
  P.save(P.sheet(items, { cols: 13 }), out + "/combat_anims.png");
}
