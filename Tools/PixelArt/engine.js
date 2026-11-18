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
function frames(def) {
  const W = def.w || 20, H = def.h || 25, ground = def.ground || "", head = def.head || "";
  let map = norm(def.map, W, H);
  if (def.flip) map = map.map((r) => [...r].reverse().join("")); // drawn facing right, mirrored to face left; light stays top-left
  const isHead = (c, y) => head.includes(c) || (def.headTo !== undefined && y < def.headTo);
  return IDLE.map(([dyB, dyH]) => {
    const m = Array.from({ length: H }, () => Array(W).fill("."));
    // robes: everything from groundFrom down stays put (trim letters repeat above and below the belt)
    const isGround = (c, y) => (def.groundFrom !== undefined ? y >= def.groundFrom : ground.includes(c));
    const put = (dy, ok) => map.forEach((r, y) => [...r].forEach((c, x) => { if (c !== "." && ok(c, y) && y + dy < H) m[y + dy][x] = c; }));
    put(0, (c, y) => isGround(c, y));
    put(dyB, (c, y) => !isGround(c, y) && !isHead(c, y));
    put(dyH, (c, y) => !isGround(c, y) && isHead(c, y));
    return P.selout(render(def, m.map((r) => r.join(""))), def.lit ?? R.steel[1], VOID);
  });
}
module.exports = { P, R, VOID, W, H, frames, DELAY, norm };
