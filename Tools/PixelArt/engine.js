// Shared hero sprite generator (codixel core). Part map -> lit ramps -> seams -> selout -> 4-frame idle.
// Usage: node heroes.js <outDir>
const P = require(process.env.CODIXEL || "C:/Users/Dios/.claude/skills/codixel/core/pixel.js");
const W = 20, H = 25; // inner; selout makes 22x27 = exact 4x in the 88x108 combat box
const VOID = P.col([7, 10, 20]);
const R = {
  steel: P.hueRamp(6, { hue: 218, sat: 0.2, shift: 35, light: [0.17, 0.86] }),
  gold: P.hueRamp(5, { hue: 40, sat: 0.6, shift: 30, light: [0.24, 0.74] }),
  red: P.hueRamp(4, { hue: 354, sat: 0.55, shift: 25, light: [0.2, 0.5] }),
  crimson: P.hueRamp(5, { hue: 356, sat: 0.55, shift: 30, light: [0.18, 0.58] }),
  leather: P.hueRamp(4, { hue: 24, sat: 0.38, shift: 25, light: [0.14, 0.42] }),
  wood: P.hueRamp(4, { hue: 28, sat: 0.4, shift: 25, light: [0.16, 0.46] }),
  skin: P.hueRamp(5, { hue: 22, sat: 0.5, shift: 25, light: [0.3, 0.78] }),
  hair: P.hueRamp(4, { hue: 25, sat: 0.3, shift: 30, light: [0.08, 0.32] }),
  beard: P.hueRamp(4, { hue: 22, sat: 0.6, shift: 25, light: [0.22, 0.55] }),
  cloth: P.hueRamp(5, { hue: 45, sat: 0.18, shift: 40, light: [0.36, 0.93] }),
  purple: P.hueRamp(5, { hue: 268, sat: 0.38, shift: 30, light: [0.18, 0.7] }),
  night: P.hueRamp(5, { hue: 245, sat: 0.32, shift: 25, light: [0.08, 0.36] }),
  arcane: P.hueRamp(4, { hue: 285, sat: 0.5, shift: 25, light: [0.32, 0.72] }),
  moss: P.hueRamp(5, { hue: 135, sat: 0.32, shift: 40, light: [0.15, 0.62] }),
  leaf: P.hueRamp(4, { hue: 105, sat: 0.5, shift: 35, light: [0.22, 0.62] }),
  cyan: P.hueRamp(4, { hue: 172, sat: 0.6, shift: 20, light: [0.32, 0.82] }),
  orange: P.hueRamp(4, { hue: 30, sat: 0.8, shift: 20, light: [0.38, 0.8] }),
  bone: P.hueRamp(4, { hue: 45, sat: 0.25, shift: 25, light: [0.45, 0.9] }),
  fur: P.hueRamp(4, { hue: 30, sat: 0.28, shift: 30, light: [0.2, 0.55] }),
  brown: P.hueRamp(5, { hue: 26, sat: 0.32, shift: 30, light: [0.12, 0.5] }),
};
const LIGHT = (() => { const v = [-1, -1.1, 1.3], m = Math.hypot(...v); return v.map((a) => a / m); })();

function norm(rows, w = W, h = H) {
  if (rows.length > h) throw new Error("too many rows " + rows.length);
  rows.forEach((r, i) => { if (r.length > w) throw new Error(`row ${i} too long (${r.length}): ${r}`); });
  return [...Array(h - rows.length).fill(""), ...rows].map((r) => r.padEnd(w, "."));
}

// def: { map, parts: {k:[ramp,bias,radius]}, fixed: {k:color}, flat: {k:(x,y,ctx)=>color}, group: {k:"letters domed together"},
//        noSeam: "pairs", ground: "letters that stay put", head: "letters that lag" }
function render(def, map) {
  const W = map[0].length, H = map.length; // per-sprite size (heroes 20x25, enemies larger)
  const b = new P.Buf(W, H), at = (x, y) => (map[y] && map[y][x]) || ".";
  for (const [k, [ramp, bias, rad]] of Object.entries(def.parts)) {
    const grp = (def.group && def.group[k]) || k, inside = (x, y) => grp.includes(at(x, y));
    const d = new Float32Array(W * H);
    for (let y = 0; y < H; y++) for (let x = 0; x < W; x++) {
      let r = 0;
      if (inside(x, y)) { r = 1; while (r < 6 && [[r,0],[-r,0],[0,r],[0,-r],[r,r],[-r,r],[r,-r],[-r,-r]].every(([a, c]) => inside(x + a, y + c))) r++; }
      d[y * W + x] = Math.min(r, rad) / rad;
    }
    const h = (x, y) => (x < 0 || y < 0 || x >= W || y >= H ? 0 : d[y * W + x]);
    for (let y = 0; y < H; y++) for (let x = 0; x < W; x++) {
      if (at(x, y) !== k) continue;
      const n = [-(h(x + 1, y) - h(x - 1, y)), -(h(x, y + 1) - h(x, y - 1)), 0.9], m = Math.hypot(...n);
      const lit = (n[0] * LIGHT[0] + n[1] * LIGHT[1] + n[2] * LIGHT[2]) / m;
      const v = Math.max(0, Math.min(0.999, 0.15 + lit * 0.85 + bias - (y / H) * 0.12));
      b.pset(x, y, ramp[P.band(v, ramp.length, x, y, 0.15)]);
    }
  }
  // seams: a part under another part (above or left of it) sits in its shadow -> one ramp step darker
  const base = b.px.slice(), solid = (c) => c !== "." && !(def.fixed && def.fixed[c]) && !(def.flat && def.flat[c]);
  const noSeam = (a, c) => a === c || (def.noSeam || "").split(" ").some((p) => p === a + c || p === c + a);
  for (let y = 0; y < H; y++) for (let x = 0; x < W; x++) {
    const k = at(x, y); if (!def.parts[k]) continue;
    const up = at(x, y - 1), lf = at(x - 1, y);
    if (!((solid(up) && !noSeam(up, k)) || (solid(lf) && !noSeam(lf, k)))) continue;
    const ramp = def.parts[k][0], i = ramp.indexOf(base[y * W + x]);
    if (i > 0) b.px[y * W + x] = ramp[i - 1];
  }
  for (let y = 0; y < H; y++) for (let x = 0; x < W; x++) {
    const k = at(x, y);
    if (def.flat && def.flat[k]) b.pset(x, y, def.flat[k](x, y, { at, W, H }));
    if (def.fixed && def.fixed[k] !== undefined) b.pset(x, y, def.fixed[k]);
  }
  return b;
}

const IDLE = [[0, 0], [1, 1], [1, 2], [0, 1]]; // [body dy, head dy]; head lags a frame
const DELAY = [320, 180, 320, 180];

// One posed frame. Every pixel belongs to one group, each moved by its own offset:
//   g ground (legs/feet, or the robe from groundFrom down), b body, a front arm + held weapon (def.arm), h head (lags).
// squash: n rows are cut out of the legs just below the hips and everything above sinks n rows (crouch / kneel).
// fx(buf, info): effects drawn after the outline (smears, glows); info.arm is the weapon's box in output pixels.
function pose(def, p = {}) {
  const W = def.w || 20, H = def.h || 25, ground = def.ground || "", head = def.head || "";
  let map = norm(def.map, W, H);
  const isHead = (c, y) => head.includes(c) || (def.headTo !== undefined && y < def.headTo);
  // robes: everything from groundFrom down stays put (trim letters repeat above and below the belt)
  const isGround = (c, y) => (def.groundFrom !== undefined ? y >= def.groundFrom : ground.includes(c));
  const isArm = (c, x) => def.arm && def.arm.letters.includes(c) && x >= def.arm.minX;
  const group = (c, x, y) => (isGround(c, y) ? "g" : isArm(c, x) ? "a" : isHead(c, y) ? "h" : "b");
  let hip = def.groundFrom;
  if (hip === undefined) hip = map.findIndex((r, y) => [...r].some((c, x) => c !== "." && group(c, x, y) === "g"));
  if (def.flip) map = map.map((r) => [...r].reverse().join("")); // drawn facing right, mirrored to face left; light stays top-left
  const fx = (x) => (def.flip ? W - 1 - x : x);
  const n = p.squash || 0, off = { g: p.g || [0, 0], b: p.b || [0, 0], a: p.a || p.b || [0, 0], h: p.h || [0, 0] };
  const m = Array.from({ length: H }, () => Array(W).fill(".")), armBox = [99, 99, -1, -1];
  // ground, body, arm, head: the same order idle always used (the arm used to be part of the body)
  for (const want of "gbah") map.forEach((r, y) => [...r].forEach((c, x) => {
    if (c === "." || group(c, fx(x), y) !== want) return;
    if (want === "g" && y >= hip && y < hip + n) return;
    const [dx, dy] = off[want], sink = want === "g" ? 0 : n, X = x + dx, Y = y + dy + sink;
    if (X < 0 || Y < 0 || X >= W || Y >= H) return;
    m[Y][X] = c;
    if (want === "a") { armBox[0] = Math.min(armBox[0], X); armBox[1] = Math.min(armBox[1], Y); armBox[2] = Math.max(armBox[2], X); armBox[3] = Math.max(armBox[3], Y); }
  }));
  const out = P.selout(render(def, m.map((r) => r.join(""))), def.lit ?? R.steel[1], VOID);
  if (p.fx) p.fx(out, { arm: armBox.map((v) => v + 1), W: W + 2, H: H + 2 });
  return out;
}

function frames(def) {
  return IDLE.map(([dyB, dyH]) => pose(def, { b: [0, dyB], h: [0, dyH] }));
}
module.exports = { P, R, VOID, W, H, frames, pose, DELAY, norm };
