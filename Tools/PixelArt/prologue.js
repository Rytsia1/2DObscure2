// Tutorial Prologue enemies (Data/PrologueRoster.cs), dark fantasy like Dungeon 1, themed on the rift / The World's Between.
// Built from shapes (ellipses, polygons, tapered strokes, 1px lines) into part maps, then lit by the shared engine.
// Normals: inner 22x24 -> 24x26 (4x in the 96x104 box). Aetherion: inner 46x58 -> 48x60 (4x in the 192x240 boss box).
// Drawn facing right; enemies.js mirrors them to face the party.  Preview: node prologue.js <outDir>
const { P, R, VOID } = require("./engine.js");

const ramp = (n, hue, sat, light, shift = 25) => P.hueRamp(n, { hue, sat, shift, light });
const C = {
  flesh: ramp(5, 262, 0.22, [0.05, 0.34]),          // void flesh, almost black violet
  bone: ramp(5, 42, 0.2, [0.14, 0.74], 40),           // dirty claws and teeth
  crystal: ramp(5, 228, 0.34, [0.08, 0.66], 30),      // dark shattered crystal
  veil: ramp(5, 248, 0.15, [0.06, 0.34]),             // tattered shroud
  skin: ramp(4, 250, 0.08, [0.2, 0.55], 20),          // corpse-grey skin
  stone: ramp(5, 238, 0.14, [0.05, 0.42], 25),        // ancient black stone, cold
  gold: ramp(4, 40, 0.42, [0.16, 0.56], 25),          // tarnished gold trim
  rift: ramp(4, 270, 0.78, [0.3, 0.8], 15),           // violet rift light
  blood: ramp(4, 355, 0.75, [0.16, 0.5], 10),
};
const SOUL = R.cyan, BOLT = ramp(3, 52, 0.9, [0.55, 0.92], 15);

// shape builder: draws letters into a w x h part map
function grid(w, h) {
  const g = Array.from({ length: h }, () => Array(w).fill("."));
  const set = (x, y, k, only) => {
    x = Math.round(x); y = Math.round(y);
    if (x < 0 || y < 0 || x >= w || y >= h) return;
    if (only && !only.includes(g[y][x])) return;
    g[y][x] = k;
  };
  const fill = (test, k, only) => { for (let y = 0; y < h; y++) for (let x = 0; x < w; x++) if (test(x, y)) set(x, y, k, only); };
  return {
    set,
    ell: (cx, cy, rx, ry, k, only) => fill((x, y) => ((x - cx) / rx) ** 2 + ((y - cy) / ry) ** 2 <= 1, k, only),
    poly: (pts, k, only) => fill((x, y) => {
      let inside = false;
      for (let i = 0, j = pts.length - 1; i < pts.length; j = i++) {
        const [xi, yi] = pts[i], [xj, yj] = pts[j];
        if (yi > y !== yj > y && x < ((xj - xi) * (y - yi)) / (yj - yi) + xi) inside = !inside;
      }
      return inside;
    }, k, only),
    rect: (x0, y0, x1, y1, k, only) => fill((x, y) => x >= x0 && x <= x1 && y >= y0 && y <= y1, k, only),
    // tapered stroke along a polyline: radius r0 at the start to r1 at the end
    stroke: (pts, r0, r1, k, only) => {
      const seg = pts.slice(1).map((p, i) => Math.hypot(p[0] - pts[i][0], p[1] - pts[i][1])), total = seg.reduce((a, b) => a + b, 0);
      let run = 0;
      pts.slice(1).forEach((p, i) => {
        const [ax, ay] = pts[i];
        for (let s = 0; s <= seg[i]; s += 0.25) {
          const t = (run + s) / total, r = r0 + (r1 - r0) * t, px = ax + ((p[0] - ax) * s) / seg[i], py = ay + ((p[1] - ay) * s) / seg[i], Rr = Math.ceil(r);
          for (let y = -Rr; y <= Rr; y++) for (let x = -Rr; x <= Rr; x++) if (x * x + y * y <= r * r + 0.3) set(px + x, py + y, k, only);
        }
        run += seg[i];
      });
    },
    // pixel-perfect 1px line (corner to corner steps)
    line: (x0, y0, x1, y1, k, only) => {
      const dx = Math.abs(x1 - x0), dy = -Math.abs(y1 - y0), sx = x0 < x1 ? 1 : -1, sy = y0 < y1 ? 1 : -1; let err = dx + dy;
      for (;;) { set(x0, y0, k, only); if (x0 === x1 && y0 === y1) return; const e2 = 2 * err; if (e2 >= dy) { err += dy; x0 += sx; } if (e2 <= dx) { err += dx; y0 += sy; } }
    },
    rows: () => g.map((r) => r.join("")),
  };
}

// ---------------- Riftling: a spider-legged void crawler torn by a glowing rift, a mantis scythe raised over its maw ----------------
function riftling() {
  const g = grid(22, 24);
  g.stroke([[4, 15], [1.5, 13], [1, 9], [2.5, 6.5]], 1, 0.35, "T");                     // whip tail curling up behind
  // long spider-thin legs, knees high
  g.stroke([[5, 16], [2, 18.5], [2, 23]], 0.9, 0.6, "L"); g.stroke([[8, 17], [6.5, 20], [6, 23]], 0.9, 0.6, "L");
  g.stroke([[11, 17], [13.5, 20], [13, 23]], 0.9, 0.6, "L");
  g.ell(9, 14.8, 5.6, 3.1, "B");                                                         // lean hunched body
  for (const [x, t] of [[5, 9.5], [8, 8.5], [11, 9.5]]) { g.stroke([[x, 12.5], [x - 1, t]], 0.9, 0.3, "B"); g.set(x - 1, t, "s"); } // spines, bone tips
  g.ell(16.5, 14.6, 3.4, 2.7, "H");                                                      // head thrust forward
  g.poly([[16, 15.2], [21.4, 14], [21, 17.6], [16.6, 16.8]], "k");                        // gaping maw
  for (const [x, y] of [[18, 15], [20, 14], [19, 17], [21, 17]]) g.set(x, y, "t");          // fangs
  for (const [x, y] of [[16, 13], [18, 13], [17, 12]]) g.set(x, y, "e");                    // pinprick eyes
  // mantis arm raised over the head, the scythe claw hooked down toward its prey
  g.stroke([[12, 12.5], [15, 7.5]], 1.2, 1, "A");
  g.stroke([[15, 7.5], [19, 7.2], [21, 9.5], [20.5, 12.5]], 0.9, 0.35, "c");
  // the rift tearing through it: a jagged crack glowing from a white-hot core
  for (const [a, b] of [[[4, 14], [6, 16]], [[6, 16], [8, 13]], [[8, 13], [10, 16]], [[10, 16], [12, 14]]]) g.line(...a, ...b, "r", "B");
  for (const [x, y] of [[6, 16], [8, 13], [10, 16]]) g.set(x, y, "R", "r");
  return {
    map: g.rows(),
    parts: { B: [C.flesh, 0.15, 2.5], H: [C.flesh, 0.2, 2], A: [C.flesh, 0.05, 1.5], L: [C.flesh, -0.1, 1], T: [C.flesh, -0.05, 1], c: [C.bone, 0.05, 1] },
    fixed: { k: VOID, t: C.bone[3], e: SOUL[3], r: C.rift[2], R: C.rift[3], s: C.bone[2] },
    ground: "L", head: "Hkte", lit: C.flesh[1],
  };
}

// ---------------- Shard Wisp: a screaming skull caught in a broken crown of dark crystal, crackling with lightning ----------------
function shardWisp() {
  const g = grid(22, 24);
  // uneven shards of different lengths and angles, splintered off the core
  const shards = [[10, 0.5, 2.4], [15.5, 2, 2], [20.5, 7, 1.8], [19, 14, 2], [3.5, 2.5, 2.1], [0.8, 10, 1.6], [5, 16.5, 1.9], [13.5, 18, 1.4]];
  for (const [x, y, r] of shards) g.stroke([[11, 9.5], [x, y]], r, 0.3, "S");
  g.ell(11, 9.8, 5, 5.2, "S");
  for (const [x, y] of shards) g.line(Math.round((11 + x) / 2), Math.round((9.5 + y) / 2), Math.round(x), Math.round(y), "f", "S"); // bright facet edge
  // skull: dark sockets with cold eyes, the jaw hanging open in a scream
  g.ell(11, 9.2, 4, 3.7, "K"); g.rect(9, 11, 13, 14, "K");
  g.ell(9.3, 8.8, 1.2, 1.4, "k"); g.ell(12.7, 8.8, 1.2, 1.4, "k");
  g.set(9, 9, "e"); g.set(13, 9, "e"); g.set(11, 11, "k");
  g.rect(10, 12.4, 12, 15.2, "k");
  for (const [x, y] of [[10, 12], [12, 12], [10, 15], [12, 15]]) g.set(x, y, "t");
  // lightning crackling across the crystal
  for (const [a, b] of [[[3, 6], [5, 8]], [[5, 8], [4, 10]], [[17, 4], [16, 6]], [[16, 6], [18, 8]]]) g.line(...a, ...b, "y");
  // splinters falling away below
  for (const [x, y] of [[9, 20], [12, 21], [10, 23]]) g.set(x, y, "S");
  return {
    map: g.rows(),
    parts: { S: [C.crystal, 0.15, 2.5], K: [C.bone, 0, 2] },
    fixed: { k: VOID, e: SOUL[3], t: C.bone[3], f: C.crystal[4], y: BOLT[1] },
    head: "Kkete", lit: C.crystal[1],
  };
}

// ---------------- Veil Stalker: a gaunt thing under a torn shroud, faceless but for a split maw, long-fingered hand ----------------
function veilStalker() {
  const g = grid(22, 24);
  // shroud: tall and hunched forward, hanging in tatters to the ground
  g.poly([[7, 3], [12, 1.5], [15, 4], [16, 11], [15, 19], [13, 23.5], [3, 23.5], [2, 18], [4, 9]], "V");
  for (const [x, len] of [[3, 2], [5, 3], [7, 1], [9, 3], [11, 2], [13, 1]]) g.stroke([[x + 0.5, 22], [x + 0.5, 22 + len]], 0.7, 0.4, "V");
  for (const x of [5, 8, 11]) g.line(x, 9, x - 1, 21, "q", "V");                               // deep folds
  // hood pulled far forward over the head, a black void inside
  g.ell(14, 5.5, 4, 4.4, "V"); g.ell(15.6, 6.4, 2.5, 3.1, "k");
  // a vertical split maw in the void with fangs along it, one cold eye above
  g.line(16, 6, 16, 9, "m", "k"); for (const [x, y] of [[15, 7], [17, 8], [15, 9]]) g.set(x, y, "t", "k");
  g.set(16, 4, "e");
  // a long thin arm reaching out and down, three long bony fingers
  g.stroke([[13, 11], [16.5, 13.5], [18.5, 16.5]], 0.95, 0.7, "a");
  for (const [x, y] of [[21.5, 16.5], [21, 19.5], [19, 21.5]]) g.stroke([[18.5, 16.5], [x, y]], 0.5, 0.3, "h");
  // space tears around it: rows of the body slip sideways, edged with rift light
  const m = g.rows().map((r) => [...r]);
  for (const [y, dx] of [[12, -1], [13, -1], [17, 1]]) {
    const row = m[y].slice();
    for (let x = 0; x < 22; x++) m[y][x] = row[x - dx] ?? ".";
    for (let x = 0; x < 22; x++) if (m[y][x] !== "." && (x === 0 || m[y][x - 1] === ".")) { m[y][x] = "r"; break; }
  }
  return {
    map: m.map((r) => r.join("")),
    parts: { V: [C.veil, 0.15, 3], q: [C.veil, -0.2, 3], a: [C.skin, -0.05, 1], h: [C.skin, 0.05, 1] },
    fixed: { k: VOID, m: C.blood[0], t: C.bone[3], e: SOUL[3], r: C.rift[2] },
    group: { V: "Vq", q: "Vq" }, noSeam: "Vq", groundFrom: 20, headTo: 11, lit: C.veil[2],
  };
}

// ---------------- Aetherion, the Threshold Guardian (boss, 46x58 inner) ----------------
// A towering gatekeeper of cold black stone and tarnished gold: a tall faceless crowned helm split by one vertical rift-eye,
// a broken halo of shards, the chest cracked open on the rift itself (the threshold), a great key-blade planted point-down,
// the other hand hanging with long claws, a torn mantle. P2 "opens the rift": the chest rift widens, more eyes open, blood-red.
function aetherion(p2) {
  const W = 46, H = 58, g = grid(W, H);
  // halo of jagged shards hanging in a broken ring above and behind the head, rift light at the tips
  for (let i = 0; i < 7; i++) {
    const a = ((200 + i * 23.3) * Math.PI) / 180, x0 = 23 + Math.cos(a) * 9, y0 = 11 + Math.sin(a) * 8, x1 = 23 + Math.cos(a) * 14.5, y1 = 11 + Math.sin(a) * 11;
    g.stroke([[x0, y0], [x1, y1]], 1.4, 0.3, "Z"); g.set(x1, y1, "R");
  }
  // back (left) arm, hanging with long claws
  g.stroke([[12, 23], [8, 31], [6, 38]], 3, 2.3, "A");
  g.ell(6, 39, 2.4, 2, "A");
  for (const [x, y] of [[2.5, 45], [5.5, 46], [8.5, 45]]) g.stroke([[6, 40], [x, y]], 0.8, 0.35, "c");
  // legs and the torn mantle between them
  g.stroke([[18, 40], [16, 49], [15, 56]], 3.3, 2.9, "L"); g.stroke([[28, 40], [30, 49], [31, 56]], 3.3, 2.9, "L");
  g.rect(11, 56, 19, 57.6, "F"); g.rect(27, 56, 35, 57.6, "F");
  g.poly([[14, 36], [32, 36], [35, 50], [32, 49], [29, 55], [26, 49], [23, 56], [20, 49], [17, 55], [14, 49], [11, 50]], "M");
  // torso: broad plated chest tapering hard to the waist, spiked pauldrons
  g.poly([[11, 20], [35, 20], [32, 30], [28, 40], [18, 40], [14, 30]], "B");
  g.stroke([[10, 20], [7, 23]], 3.2, 2.4, "B"); g.stroke([[36, 20], [39, 23]], 3.2, 2.4, "B");
  g.stroke([[8, 19], [5, 13]], 1.5, 0.3, "B"); g.stroke([[38, 19], [41, 13]], 1.5, 0.3, "B");
  g.line(12, 21, 34, 21, "g", "B"); g.line(18, 39, 28, 39, "g", "B");                          // collar and belt trim
  for (const [x0, x1] of [[14, 20], [26, 32]]) g.line(x0, 26, x1, 26, "q", "B");                 // plate seams
  // the threshold: a jagged rift cracked open through the chest, light bleeding into the stone around it
  const rw = p2 ? 4 : 2.6;
  const rift = [[23, 21.5], [23 + rw, 25], [23 + rw * 0.5, 29], [23 + rw * 1.1, 33], [23, 38.5], [23 - rw * 1.1, 34], [23 - rw * 0.6, 30], [23 - rw, 26]];
  g.poly(rift.map(([x, y]) => [23 + (x - 23) * 1.7, y]), "p", "B");
  g.poly(rift, "v");
  g.line(23, 23, 23, 37, "R", "v");
  if (p2) for (const [x, y] of [[21, 28], [25, 32], [22, 34]]) g.set(x, y, "e", "v");
  // front (right) arm and the great key-blade planted point-down
  g.stroke([[35, 22], [39, 30], [40, 36]], 3, 2.4, "A");
  g.stroke([[42, 14], [42, 54]], 1.6, 1.1, "K");
  g.line(42, 17, 42, 52, "k", "K");
  g.stroke([[39, 36], [45, 36]], 1.1, 1.1, "G");
  g.stroke([[42, 30], [42, 35]], 1, 1, "w");
  g.ell(42, 29, 1.8, 1.8, "G"); g.set(42, 29, "R");
  g.poly([[40.4, 47], [42, 47], [42, 49], [43.6, 49], [43.6, 51], [42, 51]], "G");
  g.ell(40, 37, 2.4, 2.2, "A");
  // head: a tall faceless helm, crowned, split by one vertical rift-eye
  g.poly([[23, 4], [28, 9], [29, 18], [26, 21], [20, 21], [17, 18], [18, 9]], "H");
  for (const [x, t] of [[19, 5], [21, 2.5], [23, 1], [25, 2.5], [27, 5]]) g.stroke([[x, 8], [x, t]], 0.9, 0.3, "G");
  g.line(18, 8, 28, 8, "G", "H");
  g.poly([[23, 10], [24.5, 13.5], [23, 18], [21.5, 13.5]], "k");
  g.line(23, 11, 23, 17, "E", "k");
  if (p2) for (const [x, y] of [[20, 12], [26, 12], [20, 16], [26, 16]]) g.set(x, y, "e");
  for (const [x, y] of p2 ? [[4, 20], [44, 6], [2, 31], [37, 45], [8, 52]] : [[4, 20], [44, 6], [37, 45]]) g.set(x, y, "R");
  return {
    w: W, h: H, map: g.rows(),
    parts: { B: [C.stone, 0.15, 4], q: [C.stone, -0.3, 4], H: [C.stone, 0.2, 3], A: [C.stone, 0.1, 2.5], L: [C.stone, 0, 2.5], F: [C.stone, -0.1, 1.5],
      M: [C.veil, 0.05, 3], Z: [C.stone, 0.2, 1.2], c: [C.bone, -0.05, 1], K: [R.steel, 0, 1.5], G: [C.gold, 0.1, 1.5], w: [R.leather, 0, 1] },
    fixed: { k: VOID, v: VOID, g: C.gold[2], p: C.rift[0], R: C.rift[3], E: p2 ? C.blood[3] : C.rift[3], e: p2 ? C.blood[2] : SOUL[3] },
    group: { B: "Bqp", q: "Bqp" }, noSeam: "Bq Bp qp", ground: "LFM", head: "HZGkE", lit: C.stone[2],
  };
}

const PROLOGUE = { riftling: riftling(), shard_wisp: shardWisp(), veil_stalker: veilStalker(), aetherion: aetherion(false), aetherion_p2: aetherion(true) };
// strike effects drawn after the outline on the frames where the blow lands (frames are mirrored: the party is to the left)
const put = (b, x, y, c) => { if (x >= 0 && y >= 0 && x < b.w && y < b.h) b.px[y * b.w + x] = c; };
// a crescent of rift light swept in front of the blade; on the follow-through it breaks up and fades
const riftSlash = (cols, fade) => (b, { arm: [x1, y1, , y2] }) => {
  // the blade stands at the frame's edge, so the arc sweeps over it (its leftmost point just past the blade)
  const r = Math.round((y2 - y1) * 0.55), cx = x1 + r - 1, cy = Math.round((y1 + y2) / 2);
  for (let a = 115; a <= 245; a += 2) {
    const t = (a - 115) / 130, w = Math.sin(t * Math.PI) * 2.2, rad = (a * Math.PI) / 180;
    for (let d = 0; d <= w; d += 0.5) {
      if (fade && (a / 2) % 3 === 0) continue;
      put(b, Math.round(cx + (r - d) * Math.cos(rad)), Math.round(cy + (r - d) * Math.sin(rad)), d < 0.8 && !fade ? cols[1] : cols[0]);
    }
  }
};
// a forked bolt from the skull to the party, one frame bright, the next a fading afterimage
const shardBolt = (fade) => (b) => {
  let x = 9, y = 13, step = 0; // from the screaming mouth toward the party
  while (x > 0) {
    const nx = x - 3, ny = y + (step++ % 2 ? 2 : -2);
    for (let i = 0; i <= 3; i++) { const yy = Math.round(y + ((ny - y) * i) / 3); put(b, x - i, yy, fade ? BOLT[0] : BOLT[2]); if (!fade) put(b, x - i, yy + 1, BOLT[1]); }
    x = nx; y = ny;
  }
  if (!fade) for (const [dx, dy] of [[1, -1], [1, 1]]) put(b, 1 + dx, y + dy, BOLT[1]);
};
// attack styles for anims.js (arm = the group that lunges ahead of the body; fx = {frame: effect})
const PROLOGUE_RIG = {
  riftling: { arm: { letters: "Ac", minX: 12 }, style: "melee" },
  shard_wisp: { style: "pounce", fx: { 2: shardBolt(false), 3: shardBolt(true) } },
  veil_stalker: { arm: { letters: "ah", minX: 13 }, style: "melee" },
  aetherion: { arm: { letters: "AKkGwR", minX: 34 }, style: "melee", fx: { 2: riftSlash([C.rift[1], C.rift[3]], false), 3: riftSlash([C.rift[0], C.rift[2]], true) } },
  aetherion_p2: { arm: { letters: "AKkGwR", minX: 34 }, style: "melee", fx: { 2: riftSlash([C.blood[1], C.blood[3]], false), 3: riftSlash([C.blood[0], C.blood[2]], true) } },
};
module.exports = { PROLOGUE, PROLOGUE_RIG };

if (require.main === module) {
  const { norm, frames, pose, DELAY } = require("./engine.js"), { EANIMS } = require("./anims.js");
  const out = process.argv[2] || "ArtPreview", fs = require("fs"), sheet = [], ones = [];
  for (const [id, d] of Object.entries(PROLOGUE)) {
    const def = { ...d, w: d.w || 22, h: d.h || 24, flip: true };
    def.map = norm(def.map, def.w, def.h);
    const idle = frames(def), rig = PROLOGUE_RIG[id], a = EANIMS[rig.style];
    const atk = a.poses(rig).map((p, i) => pose({ ...def, arm: rig.arm }, rig.fx && rig.fx[i] ? { ...p, fx: rig.fx[i] } : p));
    const e = P.edgeCheck(idle[0]), cc = P.colorCount(idle[0]);
    console.log(id.padEnd(14), idle[0].w + "x" + idle[0].h, "nubs", e.nubs.length, "holes", e.holes.length, "colors", cc.length, "top", (cc[0].share * 100).toFixed(0) + "%");
    const sc = def.w > 30 ? 3 : 5, tag = id.replace("_", " ").toUpperCase().slice(0, 10);
    idle.forEach((f, i) => sheet.push({ buf: f, label: `${tag} I${i}`, scale: sc }));
    atk.forEach((f, i) => sheet.push({ buf: f, label: `ATK${i}`, scale: sc }));
    ones.push({ buf: idle[0], label: "", scale: 1 });
    fs.writeFileSync(`${out}/prologue_${id}.gif`, P.gif([...idle, ...idle, ...atk, ...idle], { delay: [...DELAY, ...DELAY, ...a.delay, ...DELAY], scale: 5 }));
  }
  P.save(P.sheet(sheet, { cols: 10 }), out + "/prologue_frames.png");
  P.save(P.sheet(ones, { cols: 5, gap: 6 }), out + "/prologue_1x.png");
}
