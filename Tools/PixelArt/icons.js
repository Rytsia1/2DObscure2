// Item, gear and skill icons: 14x14 inner -> selout -> 16x16 (exact 3x = 48px in the 56px item slot / 64px skill node).
// Built from shapes in a local space (any transform: move, scale, rotate, mirror) into a part map, then lit from the
// top left like the unit sprites. Glowing parts (fire, ice, holy...) shade from their core outward instead of as a dome.
// Usage: node icons.js <outDir>
const { P, R, VOID } = require("./engine.js");
const N = 14;
const ramp = (n, hue, sat, light, shift = 25) => P.hueRamp(n, { hue, sat, shift, light });

// Materials: letter -> [ramp, bias, radius, glow]. Icons can override or add letters with def.mat.
const M = {
  steel: ramp(5, 215, 0.16, [0.2, 0.86], 35), iron: ramp(5, 215, 0.12, [0.12, 0.6], 30), gold: R.gold, wood: R.wood,
  leather: R.leather, cloth: R.cloth, bone: ramp(5, 42, 0.22, [0.2, 0.84], 40), red: R.crimson, purple: R.purple,
  fire: ramp(5, 28, 0.95, [0.32, 0.88], 35), ice: ramp(5, 198, 0.55, [0.3, 0.92]), bolt: ramp(4, 52, 0.85, [0.48, 0.92], 20),
  arcane: ramp(5, 282, 0.55, [0.25, 0.8], 30), shadow: ramp(5, 268, 0.36, [0.1, 0.56]), holy: ramp(5, 46, 0.75, [0.42, 0.93]),
  leaf: R.leaf, moss: R.moss, blood: ramp(5, 355, 0.7, [0.16, 0.56], 15), poison: ramp(4, 92, 0.6, [0.25, 0.66], 30),
  mana: ramp(5, 222, 0.62, [0.22, 0.72]), soul: R.cyan, glass: ramp(4, 200, 0.22, [0.45, 0.9], 20), bark: R.brown,
  stone: ramp(4, 230, 0.08, [0.2, 0.6], 25), ember: ramp(4, 14, 0.55, [0.12, 0.42], 20), bronze: ramp(5, 30, 0.45, [0.2, 0.68], 30),
  flesh: R.skin, rose: ramp(4, 335, 0.5, [0.3, 0.72], 25), black: ramp(4, 250, 0.2, [0.06, 0.26], 20), smoke: ramp(4, 230, 0.1, [0.25, 0.62], 20),
};
const L = {
  s: [M.steel, 0.05, 2], S: [M.iron, 0, 2], G: [M.gold, 0.05, 2], w: [M.wood, 0, 2], l: [M.leather, 0, 2], c: [M.cloth, 0, 2],
  b: [M.bone, 0, 2], r: [M.red, 0, 2], u: [M.purple, 0, 2], m: [M.moss, 0, 2], n: [M.leaf, 0.05, 2], V: [M.bark, 0, 2],
  o: [M.stone, 0, 2], z: [M.bronze, 0, 2], f: [M.flesh, 0, 2], x: [M.black, 0, 2], q: [M.glass, 0.1, 1.5], R: [M.blood, 0.05, 2],
  // glowing energy: shaded from the core out
  F: [M.fire, 0.05, 2, 1], I: [M.ice, 0.05, 2, 1], Y: [M.bolt, 0.1, 1.5, 1], A: [M.arcane, 0, 2, 1], D: [M.shadow, 0, 2, 1],
  H: [M.holy, 0.05, 2, 1], N: [M.leaf, 0.1, 2, 1], B: [M.mana, 0.05, 2, 1], C: [M.soul, 0.05, 2, 1], O: [M.poison, 0.05, 2, 1],
  K: [M.blood, 0.1, 2, 1], E: [M.ember, 0.1, 2, 1], X: [M.rose, 0.05, 2, 1], Z: [M.smoke, 0, 2, 1],
  T: [ramp(4, 210, 0.22, [0.55, 0.96], 20), 0.1, 1.5, 1], // pale impact flash / wind
  h: [R.skin, -0.3, 2], e: [M.arcane, 0.05, 1.5], j: [M.mana, 0.05, 1.5], y: [M.ice, 0.05, 2], v: [M.shadow, 0.05, 2],
};
const FIX = { W: P.col([250, 246, 228]), k: VOID, ".": null };
const LIGHT = (() => { const v = [-1, -1.1, 1.3], m = Math.hypot(...v); return v.map((a) => a / m); })();

class Canvas {
  constructor() {
    this.k = Array.from({ length: N }, () => Array(N).fill("."));
    this.z = Array.from({ length: N }, () => Array(N).fill(0));
    this.zc = 0; this.m = [1, 0, 0, 1, 0, 0]; this.clip = null;
  }
  // t: { x, y, s, sx, sy, r (degrees, clockwise), fx (mirror) } applied in that local order: scale/mirror, rotate, move
  at(t, fn) {
    const old = this.m, r = ((t.r || 0) * Math.PI) / 180, cs = Math.cos(r), sn = Math.sin(r);
    const sx = (t.sx ?? t.s ?? 1) * (t.fx ? -1 : 1), sy = t.sy ?? t.s ?? 1;
    const T = [cs * sx, sn * sx, -sn * sy, cs * sy, t.x || 0, t.y || 0], [a, b, c, d, e, f] = old;
    this.m = [a * T[0] + c * T[1], b * T[0] + d * T[1], a * T[2] + c * T[3], b * T[2] + d * T[3], a * T[4] + c * T[5] + e, b * T[4] + d * T[5] + f];
    fn(); this.m = old; return this;
  }
  // only: draw only over these letters; behind: only into empty pixels
  with(o, fn) { const old = this.clip; this.clip = o; fn(); this.clip = old; return this; }
  fill(k, test) {
    const [a, b, c, d, e, f] = this.m, det = a * d - b * c; this.zc++;
    for (let y = 0; y < N; y++) for (let x = 0; x < N; x++) {
      const X = x + 0.5 - e, Y = y + 0.5 - f, lx = (d * X - c * Y) / det, ly = (-b * X + a * Y) / det;
      if (test(lx, ly)) this.put(x, y, k);
    }
    return this;
  }
  put(x, y, k) {
    if (x < 0 || y < 0 || x >= N || y >= N) return;
    const cur = this.k[y][x], o = this.clip;
    if (o && o.only && !o.only.includes(cur)) return;
    if (o && o.behind && cur !== ".") return;
    if (k === ".") { this.k[y][x] = "."; return; }
    this.k[y][x] = k; this.z[y][x] = this.zc;
  }
  ell(cx, cy, rx, ry, k) { return this.fill(k, (x, y) => ((x - cx) / rx) ** 2 + ((y - cy) / ry) ** 2 <= 1); }
  ring(cx, cy, r0, r1, k) { return this.fill(k, (x, y) => { const d = Math.hypot(x - cx, y - cy); return d >= r0 && d <= r1; }); }
  // part of a ring between angles a0..a1 (degrees, clockwise from +x)
  arc(cx, cy, r0, r1, a0, a1, k) {
    return this.fill(k, (x, y) => {
      const d = Math.hypot(x - cx, y - cy); if (d < r0 || d > r1) return false;
      let a = (Math.atan2(y - cy, x - cx) * 180) / Math.PI; while (a < a0) a += 360; return a <= a1;
    });
  }
  rect(x0, y0, x1, y1, k) { return this.fill(k, (x, y) => x >= x0 && x <= x1 && y >= y0 && y <= y1); }
  poly(pts, k) {
    return this.fill(k, (x, y) => {
      let inside = false;
      for (let i = 0, j = pts.length - 1; i < pts.length; j = i++) {
        const [xi, yi] = pts[i], [xj, yj] = pts[j];
        if (yi > y !== yj > y && x < ((xj - xi) * (y - yi)) / (yj - yi) + xi) inside = !inside;
      }
      return inside;
    });
  }
  // stroke along a polyline, radius r0 at the start to r1 at the end
  cap(pts, r0, r1 = r0, k) {
    const seg = pts.slice(1).map((p, i) => Math.hypot(p[0] - pts[i][0], p[1] - pts[i][1])), total = seg.reduce((a, b) => a + b, 0) || 1;
    return this.fill(k, (x, y) => {
      let run = 0;
      for (let i = 0; i < seg.length; i++) {
        const [ax, ay] = pts[i], [bx, by] = pts[i + 1], L2 = seg[i] ** 2 || 1;
        const t = Math.max(0, Math.min(1, ((x - ax) * (bx - ax) + (y - ay) * (by - ay)) / L2));
        const r = r0 + ((r1 - r0) * (run + t * seg[i])) / total;
        if (Math.hypot(x - ax - t * (bx - ax), y - ay - t * (by - ay)) <= r) return true;
        run += seg[i];
      }
      return false;
    });
  }
  // a single pixel at a local point
  px(x, y, k) {
    const [a, b, c, d, e, f] = this.m; this.zc++;
    this.put(Math.floor(a * x + c * y + e), Math.floor(b * x + d * y + f), k); return this;
  }
  // a 1px pixel-perfect line between local points (world-space Bresenham)
  line(x0, y0, x1, y1, k) {
    const [a, b, c, d, e, f] = this.m, w = (x, y) => [Math.floor(a * x + c * y + e), Math.floor(b * x + d * y + f)];
    let [X0, Y0] = w(x0, y0); const [X1, Y1] = w(x1, y1), dx = Math.abs(X1 - X0), dy = -Math.abs(Y1 - Y0);
    const sx = X0 < X1 ? 1 : -1, sy = Y0 < Y1 ? 1 : -1; let err = dx + dy; this.zc++;
    for (;;) { this.put(X0, Y0, k); if (X0 === X1 && Y0 === Y1) break; const e2 = 2 * err; if (e2 >= dy) { err += dy; X0 += sx; } if (e2 <= dx) { err += dx; Y0 += sy; } }
    return this;
  }
}

function render(c, def = {}) {
  const map = c.k, at = (x, y) => (map[y] && map[y][x]) || ".", lt = { ...L, ...(def.mat || {}) };
  const b = new P.Buf(N, N), base = new Array(N * N).fill(-1), part = (k) => lt[k] && !(k in FIX);
  for (const k of new Set(map.flat())) {
    if (!part(k)) continue;
    const [rmp, bias, rad, glow] = lt[k], grp = (def.group && def.group[k]) || k, inside = (x, y) => grp.includes(at(x, y));
    const d = new Float32Array(N * N);
    for (let y = 0; y < N; y++) for (let x = 0; x < N; x++) {
      let r = 0;
      if (inside(x, y)) { r = 1; while (r < 6 && [[r,0],[-r,0],[0,r],[0,-r],[r,r],[-r,r],[r,-r],[-r,-r]].every(([a, q]) => inside(x + a, y + q))) r++; }
      d[y * N + x] = Math.min(r, rad) / rad;
    }
    const h = (x, y) => (x < 0 || y < 0 || x >= N || y >= N ? 0 : d[y * N + x]);
    for (let y = 0; y < N; y++) for (let x = 0; x < N; x++) {
      if (at(x, y) !== k) continue;
      let v;
      if (glow) v = 0.1 + d[y * N + x] * 0.8 + bias + (h(x - 1, y - 1) > h(x + 1, y + 1) ? 0.08 : 0);
      else {
        const n = [-(h(x + 1, y) - h(x - 1, y)), -(h(x, y + 1) - h(x, y - 1)), 0.9], m = Math.hypot(...n);
        v = 0.15 + ((n[0] * LIGHT[0] + n[1] * LIGHT[1] + n[2] * LIGHT[2]) / m) * 0.85 + bias - (y / N) * 0.1;
      }
      const i = P.band(Math.max(0, Math.min(0.999, v)), rmp.length, x, y, 0.15);
      b.pset(x, y, rmp[i]); base[y * N + x] = i;
    }
  }
  // seams: a solid part under a later part (above or left of it) falls in its shadow, one step darker
  for (let y = 0; y < N; y++) for (let x = 0; x < N; x++) {
    const k = at(x, y); if (!part(k) || lt[k][3]) continue;
    const over = (X, Y) => { const q = at(X, Y); return part(q) && q !== k && !lt[q][3] && c.z[Y][X] > c.z[y][x]; };
    if ((y > 0 && over(x, y - 1)) || (x > 0 && over(x - 1, y))) { const i = base[y * N + x]; if (i > 0) b.pset(x, y, lt[k][0][i - 1]); }
  }
  for (let y = 0; y < N; y++) for (let x = 0; x < N; x++) { const k = at(x, y); if (FIX[k]) b.pset(x, y, FIX[k]); }
  // outline: the lit side takes a dark step of the icon's main material, the rest is void
  const count = {}; map.flat().forEach((k) => part(k) && (count[k] = (count[k] || 0) + 1));
  const main = def.edge || Object.keys(count).sort((a, z) => count[z] - count[a])[0] || "s", mr = lt[main][0];
  return P.selout(b, mr[Math.min(1, mr.length - 3)], VOID);
}

// ---------- glyphs (local space; most are drawn upright and centred on 0,0) ----------
const G = {
  // blade weapons, upright: tip at y=-9, pommel at y=+9
  sword(c, o = {}) {
    const bl = o.blade || "s", w = o.w ?? 1.15, len = o.len ?? 9;
    c.poly([[0, -len - 0.2], [w, -len + 1.6], [w, 3.6], [-w, 3.6], [-w, -len + 1.6]], bl);
    if (o.fuller !== false) c.line(0, -len + 2.5, 0, 2.2, o.fullerK || "S");
    c.cap([[-o.gw || -3.6, 4.2], [o.gw || 3.6, 4.2]], 0.85, 0.85, o.guard || "G");
    c.rect(-0.75, 4.6, 0.75, 7.6, o.grip || "l");
    c.ell(0, 8.4, 1.1, 1.1, o.pommel || o.guard || "G");
  },
  dagger(c, o = {}) {
    c.poly([[0, -6.5], [1.1, -4.6], [1.1, 1.8], [-1.1, 1.8], [-1.1, -4.6]], o.blade || "s");
    c.cap([[-2.6, 2.4], [2.6, 2.4]], 0.8, 0.8, o.guard || "G");
    c.rect(-0.7, 2.8, 0.7, 5.6, o.grip || "l");
    c.ell(0, 6.3, 0.95, 0.95, o.pommel || o.guard || "G");
  },
  axe(c, o = {}) {
    c.cap([[0, -8], [0, 9]], 0.8, 0.8, o.haft || "w");
    const hd = o.head || "s";
    c.poly([[0.3, -7.5], [5.5, -9], [6.5, -4.5], [5.5, -0.5], [0.3, -2.5]], hd);
    if (o.double) c.poly([[-0.3, -7.5], [-5.5, -9], [-6.5, -4.5], [-5.5, -0.5], [-0.3, -2.5]], hd);
    c.cap([[6, -8.3], [6.6, -4.5], [5.8, -1]], 0.5, 0.5, o.edge || "W");
  },
  hammer(c, o = {}) {
    c.cap([[0, -7], [0, 9]], 0.8, 0.8, o.haft || "w");
    c.rect(-4.5, -9, 4.5, -3.5, o.head || "S");
    c.rect(-4.5, -9, -3.4, -3.5, o.band || "s"); c.rect(3.4, -9, 4.5, -3.5, o.band || "s");
  },
  mace(c, o = {}) { // flanged head on a short haft
    c.cap([[0, -2], [0, 8.5]], 0.8, 0.8, o.haft || "w");
    c.ell(0, 8.6, 1, 1, o.pommel || "G");
    for (const r of [0, 60, 120]) c.at({ y: -4.5, r }, () => c.poly([[0, -4.2], [0.9, 0], [0, 4.2], [-0.9, 0]], o.head || "s"));
    c.ell(0, -4.5, 2.2, 2.2, o.head || "s");
    c.rect(-1.3, -1.6, 1.3, -0.6, o.band || "G");
  },
  spear(c, o = {}) {
    c.cap([[0, -5], [0, 9.5]], 0.75, 0.75, o.haft || "w");
    c.poly([[0, -10], [1.9, -6], [0.8, -3.6], [-0.8, -3.6], [-1.9, -6]], o.head || "s");
    c.rect(-1.2, -3.8, 1.2, -2.6, o.band || "G");
  },
  scythe(c, o = {}) { // icon space: haft from bottom left to top right, blade sweeping back over the top
    c.cap([[2.5, 13], [10.5, 1.5]], 0.8, 0.8, o.haft || "w");
    c.cap([[10.2, 1.8], [7, 0.9], [3.5, 1.7], [0.8, 4.8]], 1.7, 0.3, o.head || "s");
    c.line(9.2, 2.7, 6, 2.6, o.edge || "W"); c.line(6, 2.6, 2.6, 4.4, o.edge || "W");
  },
  staff(c, o = {}) {
    c.cap([[0, -5.5], [0, 9.5]], 0.8, 0.8, o.haft || "w");
    if (o.crook) c.cap([[0, -5], [0, -8.5], [-2.6, -9.6], [-3.6, -7.5]], 0.85, 0.7, o.haft || "w");
    if (o.gem) c.ell(0, -7.5, 1.9, 1.9, o.gem);
    if (o.prongs) { c.cap([[0, -4.5], [-2.2, -7], [-1.6, -10]], 0.6, 0.4, o.prongs); c.cap([[0, -4.5], [2.2, -7], [1.6, -10]], 0.6, 0.4, o.prongs); }
  },
  wand(c, o = {}) {
    c.cap([[0, -6], [0, 7.5]], 0.75, 0.55, o.haft || "w");
    c.rect(-1, 3.5, 1, 4.5, o.band || "G");
    c.ell(0, -7, 1.6, 1.6, o.gem || "A");
  },
  bow(c, o = {}) {
    c.cap([[1, -9], [-1.5, -6], [-2.8, 0], [-1.5, 6], [1, 9]], 0.9, 0.9, o.limb || "w");
    c.line(1.2, -8.5, 1.2, 8.5, o.string || "c");
    if (o.grip !== false) c.rect(-3.6, -1.2, -2, 1.2, o.gripK || "l");
  },
  arrow(c, o = {}) {
    c.line(0, -7, 0, 7, o.shaft || "w");
    c.poly([[0, -9.5], [1.8, -6.2], [-1.8, -6.2]], o.head || "s");
    c.poly([[0, 5], [1.8, 8.5], [0, 7.5], [-1.8, 8.5]], o.fletch || "c");
  },
  crossbow(c, o = {}) {
    c.rect(-0.9, -4, 0.9, 8.5, o.stock || "w");
    c.cap([[-6.5, -1.5], [-4, -4.5], [0, -5.3], [4, -4.5], [6.5, -1.5]], 0.9, 0.9, o.limb || "S");
    c.line(-6.3, -1, 6.3, -1, o.string || "c");
    c.line(0, -9, 0, -1, o.bolt || "s");
  },
  shield(c, o = {}) { // kite / heater, centred on 7,7 in icon space
    const k = o.face || "S";
    if (o.round) c.ell(7, 7, 6.3, 6.3, o.rim || "s"), c.ell(7, 7, 5, 5, k);
    else if (o.tower) c.rect(1.5, 0.5, 12.5, 13.5, o.rim || "s"), c.rect(2.6, 1.6, 11.4, 12.4, k);
    else { c.poly([[1, 1], [13, 1], [13, 6], [7, 13.6], [1, 6]], o.rim || "s"); c.poly([[2.2, 2.2], [11.8, 2.2], [11.8, 5.7], [7, 12], [2.2, 5.7]], k); }
    if (o.boss) c.ell(7, 6.4, 1.6, 1.6, o.boss);
    if (o.cross) { c.rect(6.3, 2.5, 7.7, 11, o.cross); c.rect(3, 4.8, 11, 6.2, o.cross); }
  },
  // potions and vials, in icon space
  flask(c, o = {}) {
    const g = o.glass || "q", liq = o.liquid || "R";
    if (o.shape === "vial") {
      c.rect(4.6, 3, 9.4, 13.4, g); c.with({ only: g }, () => c.rect(5.6, o.fill ?? 6.5, 8.4, 12.4, liq));
      c.rect(4, 0.6, 10, 3, o.cork || "w");
    } else if (o.shape === "tall") {
      c.ell(7, 9.4, 4.2, 4.4, g); c.rect(5.6, 2.5, 8.4, 6.5, g);
      c.with({ only: g }, () => c.ell(7, 9.8, 3.2, 3.4, liq)); c.with({ only: liq }, () => c.rect(0, 0, 14, o.fill ?? 7.5, g));
      c.rect(5, 0.6, 9, 2.8, o.cork || "w");
    } else {
      c.ell(7, 8.8, 5.6, 4.9, g); c.rect(5.4, 1.8, 8.6, 5, g);
      c.with({ only: g }, () => c.ell(7, 9.2, 4.6, 3.9, liq)); c.with({ only: liq }, () => c.rect(0, 0, 14, o.fill ?? 6.8, g));
      c.rect(4.8, 0.4, 9.2, 2.4, o.cork || "w");
    }
    c.px(o.shape === "vial" ? 5.2 : 3.6, o.shape === "vial" ? 5 : 7.4, "W");
  },
  flame(c, o = {}) { // centred, ~10 tall
    const k = o.k || "F";
    c.poly([[0, -6], [2.4, -2.5], [3.8, 0.5], [3.4, 3.2], [0, 5], [-3.4, 3.2], [-3.8, 0.5], [-2, -1.5], [-1.2, -4]], k);
    c.cap([[2.6, -2], [3.6, -4.8]], 0.9, 0.3, k);
    if (o.core !== false) c.ell(0, 2.2, 1.6, 2.2, o.coreK || "W");
  },
  shard(c, o = {}) { c.poly([[0, -5.5], [1.6, -1], [0.9, 4.5], [-0.9, 4.5], [-1.6, -1]], o.k || "I"); },
  snow(c, o = {}) {
    const k = o.k || "I";
    for (const r of [0, 60, 120]) c.at({ r }, () => { c.cap([[0, -5.5], [0, 5.5]], 0.6, 0.6, k); c.cap([[0, -3.6], [1.4, -5]], 0.45, 0.45, k); c.cap([[0, -3.6], [-1.4, -5]], 0.45, 0.45, k); c.cap([[0, 3.6], [1.4, 5]], 0.45, 0.45, k); c.cap([[0, 3.6], [-1.4, 5]], 0.45, 0.45, k); });
    c.ell(0, 0, 1.2, 1.2, o.core || "W");
  },
  bolt(c, o = {}) { c.poly([[1.5, -6.5], [-2.5, 0.5], [0, 0.5], [-1.5, 6.5], [2.8, -1], [0.3, -1], [2.2, -6.5]], o.k || "Y"); },
  orb(c, o = {}) { c.ell(0, 0, o.r || 3.6, o.r || 3.6, o.k || "A"); c.ell(-(o.r || 3.6) * 0.35, -(o.r || 3.6) * 0.35, 0.9, 0.9, "W"); },
  star(c, o = {}) { // n-point burst
    const n = o.n || 4, r0 = o.r0 ?? 1.3, r1 = o.r1 ?? 5.5, pts = [];
    for (let i = 0; i < n * 2; i++) { const a = (i * Math.PI) / n - Math.PI / 2 + (o.rot || 0), r = i % 2 ? r0 : r1; pts.push([Math.cos(a) * r, Math.sin(a) * r]); }
    c.poly(pts, o.k || "H");
    if (o.core !== false) c.ell(0, 0, o.coreR ?? 1, o.coreR ?? 1, o.coreK || "W");
  },
  skull(c, o = {}) {
    const k = o.k || "b";
    c.ell(0, -1, 4.2, 3.9, k); c.rect(-2.4, 1.5, 2.4, 4.2, k);
    c.ell(-1.6, -0.4, 1.15, 1.25, o.eye || "k"); c.ell(1.6, -0.4, 1.15, 1.25, o.eye || "k");
    c.px(0, 1.8, "k"); c.line(-1.5, 3.6, 1.5, 3.6, "k");
  },
  eye(c, o = {}) {
    c.poly([[-6, 0], [-3, -3], [3, -3], [6, 0], [3, 3], [-3, 3]], o.white || "c");
    c.ell(0, 0, 2.4, 2.4, o.iris || "B"); c.ell(0, 0, 1, 1.6, "k"); c.px(-1, -1.2, "W");
  },
  leaf(c, o = {}) {
    const k = o.k || "n";
    c.poly([[0, -6], [2.4, -3.5], [3, 0], [2, 3], [0, 5], [-2, 3], [-3, 0], [-2.4, -3.5]], k);
    c.line(0, -4.5, 0, 6.5, o.vein || "m");
  },
  heart(c, o = {}) { const k = o.k || "K"; c.ell(-2.6, -1.8, 2.8, 2.8, k); c.ell(2.6, -1.8, 2.8, 2.8, k); c.poly([[-5.3, -0.9], [5.3, -0.9], [0, 5.4]], k); c.with({ only: k }, () => c.rect(-0.5, -5, 0.5, -3.4, ".")); },
  cross(c, o = {}) { const k = o.k || "H"; c.rect(-1.1, -5.5, 1.1, 5.5, k); c.rect(-4, -2.8, 4, -0.8, k); },
  drop(c, o = {}) { const k = o.k || "R"; c.ell(0, 1.2, 2.8, 2.8, k); c.poly([[0, -4.5], [2.5, 0], [-2.5, 0]], k); },
  fist(c, o = {}) { // side view punching right: wrapped wrist, curled fingers stacked, thumb over them
    const k = o.k || "f";
    c.rect(-6.5, -2.2, -2.5, 2.6, o.wrap || "c");
    c.line(-5, -2.2, -5, 2.6, o.wrapLine || "l");
    c.ell(0.6, 0.2, 3.9, 3.6, k); c.rect(-2.6, -3, 2, 3.6, k);
    for (const y of [-1.5, 0.6, 2.6]) c.ell(3.6, y, 1.1, 1.05, k);
    for (const y of [-0.5, 1.6]) c.line(1.4, y, 4.6, y, o.crease || "h");
    c.cap([[-1.6, -2.9], [2.4, -2.4]], 1.05, 0.95, o.thumb || k);
    c.line(-1.2, -1.5, 2.2, -1.2, o.crease || "h");
  },
  boot(c, o = {}) {
    const k = o.k || "l";
    c.poly([[-3, -6], [1.5, -6], [1.5, 2], [5.5, 3], [5.5, 5.5], [-3, 5.5]], k);
    if (o.cuff) c.rect(-3.4, -6.5, 2, -4.5, o.cuff);
    if (o.sole) c.rect(-3, 4.6, 5.5, 5.8, o.sole);
  },
  helm(c, o = {}) {
    const k = o.k || "s";
    c.ell(7, 6.5, 5.2, 5.6, k); c.rect(1.8, 6.5, 12.2, 12.5, k);
    if (o.visor !== false) { c.rect(3, 6.5, 11, 7.6, "k"); c.rect(6.4, 7.6, 7.6, 11, "k"); }
    if (o.crest) c.cap([[7, 1], [7, 4]], 1, 0.8, o.crest);
    if (o.horns) { c.cap([[2.5, 5], [0.6, 2], [1.2, 0.3]], 1.1, 0.4, o.horns); c.cap([[11.5, 5], [13.4, 2], [12.8, 0.3]], 1.1, 0.4, o.horns); }
  },
  hood(c, o = {}) {
    const k = o.k || "u";
    c.poly([[7, 0.5], [11.5, 3.5], [13, 9], [12, 13.5], [2, 13.5], [1, 9], [2.5, 3.5]], k);
    c.ell(7, 8.4, 3.2, 3.6, o.face || "k");
    if (o.eyes) { c.px(5.6, 8, o.eyes); c.px(8, 8, o.eyes); }
  },
  crown(c, o = {}) {
    const k = o.k || "G";
    c.poly([[1, 12], [1, 4], [4, 7.5], [7, 2], [10, 7.5], [13, 4], [13, 12]], k);
    c.rect(1, 10, 13, 12.5, o.band || k);
    if (o.gem) c.ell(7, 8.6, 1.3, 1.3, o.gem);
  },
  ring(c, o = {}) { c.ring(7, 8.5, 2.6, 4.4, o.k || "G"); if (o.gem) c.ell(7, 3.4, 2.2, 1.9, o.gem); },
  pendant(c, o = {}) {
    c.cap([[1.5, 0.5], [3.6, 5], [7, 6.5], [10.4, 5], [12.5, 0.5]], 0.5, 0.5, o.chain || "G");
    c.ell(7, 9.6, o.r || 3.3, o.r || 3.3, o.frame || "G"); c.ell(7, 9.6, (o.r || 3.3) - 1.2, (o.r || 3.3) - 1.2, o.gem || "X");
  },
  book(c, o = {}) {
    c.rect(2, 1.5, 12, 12.5, o.cover || "u"); c.rect(3, 12, 12.6, 13.2, o.pages || "c"); c.rect(2, 1.5, 3.2, 12.5, o.spine || "l");
    if (o.sigil) c.at({ x: 7.6, y: 7, s: 0.55 }, () => o.sigil(c));
  },
  scroll(c, o = {}) {
    c.rect(3, 2, 11, 12, o.paper || "c");
    c.cap([[2, 2], [12, 2]], 1.2, 1.2, o.rod || "w"); c.cap([[2, 12], [12, 12]], 1.2, 1.2, o.rod || "w");
    for (const y of [4.5, 6.5, 8.5]) c.line(4.5, y, 9.5, y, o.ink || "l");
  },
  key(c, o = {}) {
    const k = o.k || "G";
    c.ring(-3, -3, 1.4, 3, k); c.cap([[-1, -1], [5, 5]], 0.75, 0.75, k);
    c.cap([[3, 3], [5, 1]], 0.6, 0.6, k); c.cap([[4.6, 4.6], [6.4, 2.8]], 0.6, 0.6, k);
  },
  feather(c, o = {}) { // upright, use r:40 for the usual slant
    const k = o.k || "c";
    c.poly([[0, -8], [1.9, -5.5], [2.7, -1.5], [2.3, 2.5], [0, 4.5], [-2.3, 2.5], [-2.7, -1.5], [-1.9, -5.5]], k);
    c.with({ only: k }, () => { c.line(2.8, -2.8, 1.2, -1.6, "."); c.line(-2.8, 0.6, -1.4, 1.6, "."); });
    if (o.tip) c.with({ only: k }, () => c.rect(-3, -9, 3, -4.5, o.tip));
    c.line(0, -6.5, 0, 7.5, o.quill || "b");
  },
  chain(c, o = {}) { const k = o.k || "S"; for (const [x, y] of [[-3.5, 3.5], [0, 0], [3.5, -3.5]]) c.ring(x, y, 1, 2.2, k); },
  coin(c, o = {}) { c.ell(0, 0, o.r || 4.5, o.r || 4.5, o.k || "G"); c.ring(0, 0, (o.r || 4.5) - 1.8, (o.r || 4.5) - 1.1, o.inner || "z"); },
  gem(c, o = {}) {
    const k = o.k || "A";
    c.poly([[0, -5], [3.5, -1.5], [0, 5], [-3.5, -1.5]], k);
    c.line(-3.2, -1.5, 3.2, -1.5, o.facet || "W");
  },
  moon(c, o = {}) { c.ell(0, 0, 5, 5, o.k || "D"); c.ell(2.2, -1.6, 4.2, 4.2, "."); },
  sun(c, o = {}) { G.star(c, { n: 8, r0: 3.4, r1: 6.4, k: o.ray || "H", core: false }); c.ell(0, 0, 3.6, 3.6, o.k || "H"); },
  wing(c, o = {}) { // spread to the right from a root at 0,0, scalloped feather tips along the bottom
    const k = o.k || "c";
    c.poly([[0, 1], [1.2, -2.6], [3.6, -5.2], [6.6, -6.4], [6.6, -3.6], [6, -1.2], [6.4, 0.6], [5, 1.4], [5, 2.8], [3.4, 2.8], [2.8, 4.2], [1, 3.4]], k);
    c.line(1.6, -0.6, 5.6, -2.6, o.vein || "q"); c.line(1.4, 1.6, 4.4, 1.2, o.vein || "q");
  },
  hand(c, o = {}) { // open palm, fingers up and spread, dark gaps between them
    const k = o.k || "f", gap = o.gap || "h";
    c.ell(0.2, 2.6, 3.4, 3.1, k); c.rect(-2.4, 4, 2.6, 6.8, k);
    for (const [x, t, lean] of [[-2.7, -3.4, -0.6], [-0.9, -5.4, -0.2], [0.9, -5.6, 0.2], [2.7, -4.2, 0.6]]) c.cap([[x * 0.8, 1], [x + lean, t]], 0.85, 0.75, k);
    for (const [x0, x1] of [[-1.8, -2.2], [0, 0], [1.8, 2.2]]) c.line(x0, 0.6, x1, -2.4, gap);
    c.cap([[-2.6, 3.6], [-5.6, 0.4]], 0.95, 0.8, k);
  },
  swirl(c, o = {}) { // spiral stroke
    const pts = []; for (let i = 0; i <= 26; i++) { const a = i * 0.42, r = 0.4 + i * 0.21; pts.push([Math.cos(a) * r, Math.sin(a) * r]); }
    c.cap(pts, 0.5, 1.1, o.k || "A");
  },
  burst(c, o = {}) { // explosion: jagged ring of rays
    G.star(c, { n: o.n || 7, r0: o.r0 ?? 2.6, r1: o.r1 ?? 6.4, k: o.k || "F", core: false, rot: o.rot || 0.2 });
    c.ell(0, 0, o.coreR ?? 2.2, o.coreR ?? 2.2, o.coreK || "W");
  },
  slash(c, o = {}) { // curved swipe arc
    const pts = []; for (let i = 0; i <= 16; i++) { const a = -2.3 + (i / 16) * 2.2; pts.push([Math.cos(a) * (o.r || 6), Math.sin(a) * (o.r || 6) + 2]); }
    c.cap(pts, 0.2, o.w || 1.3, o.k || "W");
  },
  claw(c, o = {}) { for (const dx of [-2.6, 0, 2.6]) c.cap([[dx - 1.8, -5], [dx + 0.3, 0], [dx + 1.6, 5]], 0.3, 0.9, o.k || "K"); },
  cloud(c, o = {}) { const k = o.k || "Z"; c.ell(-2.5, 1, 3, 2.6, k); c.ell(1, -1, 3.4, 3.2, k); c.ell(3.6, 1.6, 2.6, 2.2, k); c.rect(-5, 1, 6, 3.6, k); },
  wave(c, o = {}) { const pts = []; for (let i = 0; i <= 20; i++) pts.push([-6 + i * 0.6, Math.sin(i * 0.7) * 1.4]); c.cap(pts, 0.7, 0.7, o.k || "B"); },
  target(c, o = {}) { c.ring(0, 0, 3.6, 5, o.k || "r"); c.ring(0, 0, 1.2, 2.3, o.k || "r"); c.line(0, -6.5, 0, -4.4, o.k || "r"); c.line(0, 4.4, 0, 6.5, o.k || "r"); c.line(-6.5, 0, -4.4, 0, o.k || "r"); c.line(4.4, 0, 6.5, 0, o.k || "r"); },
  tree(c, o = {}) {
    c.cap([[0, 6], [0, -1]], 1.4, 0.8, o.bark || "V");
    c.cap([[0, 1], [-3, -1.5]], 0.6, 0.4, o.bark || "V"); c.cap([[0, 0], [3, -2.5]], 0.6, 0.4, o.bark || "V");
    c.ell(0, -3.5, 5.6, 3.6, o.k || "n"); c.ell(-3, -1.5, 2.6, 2, o.k || "n"); c.ell(3, -1.5, 2.6, 2, o.k || "n");
  },
  vine(c, o = {}) {
    const pts = []; for (let i = 0; i <= 14; i++) pts.push([-6 + i * 0.85, Math.sin(i * 0.8) * 2]);
    c.cap(pts, 0.65, 0.65, o.k || "m");
    if (o.thorns !== false) for (const [x, y, d] of [[-3.5, 1.6, 1], [0, -1.9, -1], [3.5, 1.6, 1]]) c.cap([[x, y], [x + 0.6, y + d * 1.6]], 0.5, 0.15, o.thorn || "b");
  },
  root(c, o = {}) {
    for (const p of [[[0, -6], [0, -1], [-3, 2], [-5, 6]], [[0, -1], [2.5, 2.5], [4.5, 6]], [[0, 1], [0.5, 6]]]) c.cap(p, 1, 0.3, o.k || "V");
  },
  flower(c, o = {}) {
    for (let i = 0; i < 5; i++) { const a = (i * 2 * Math.PI) / 5 - Math.PI / 2; c.ell(Math.cos(a) * 2.6, Math.sin(a) * 2.6, 1.9, 1.9, o.petal || "X"); }
    c.ell(0, 0, 1.4, 1.4, o.core || "H");
  },
  halo(c, o = {}) { c.at({ sy: 0.4 }, () => c.ring(0, 0, 3.6, 5.2, o.k || "H")); },
  chainLink(c, o = {}) { c.at({ sx: 1, sy: 1.6 }, () => c.ring(0, 0, 1, 2, o.k || "S")); },
  mark(c, o = {}) { // speed / motion streaks to the left of the subject
    for (const [y, l] of [[-2.5, 3], [0, 4.5], [2.5, 3]]) c.line(-l, y, 0, y, o.k || "W");
  },
};

// ---------- more glyphs: centred ones (c) are drawn around 0,0, the rest fill the 14x14 icon space ----------
Object.assign(G, {
  shieldC(c, o) { c.at({ x: -7, y: -7 }, () => G.shield(c, o)); },
  up(c, o = {}) { c.poly([[0, -3.2], [3.2, 0.4], [1.2, 0.4], [1.2, 3], [-1.2, 3], [-1.2, 0.4], [-3.2, 0.4]], o.k || "N"); }, // c
  down(c, o = {}) { c.at({ r: 180 }, () => G.up(c, o)); }, // c
  crack(c, o = {}) { const k = o.k || "k"; c.line(-1, -6, 0.5, -2.5, k); c.line(0.5, -2.5, -1, 0.5, k); c.line(-1, 0.5, 1, 3.5, k); c.line(1, 3.5, 0, 6, k); c.line(0.5, -2.5, 3, -3.5, k); c.line(-1, 0.5, -3.5, 1.5, k); }, // c
  sound(c, o = {}) { for (const r of [2, 4, 6]) c.arc(0, 0, r, r + 0.9, -50, 50, o.k || "T"); }, // c, waves to the right
  greatsword(c, o = {}) { G.sword(c, { w: 1.6, len: 9.6, gw: 4.2, ...o }); },
  rapier(c, o = {}) {
    c.poly([[0, -9.8], [0.6, -8.5], [0.6, 3.6], [-0.6, 3.6], [-0.6, -8.5]], o.blade || "s");
    c.arc(0, 5, 2, 3, 0, 180, o.guard || "G"); c.cap([[-3, 3.8], [3, 3.8]], 0.6, 0.6, o.guard || "G");
    c.rect(-0.7, 4, 0.7, 7.4, o.grip || "l"); c.ell(0, 8.3, 1, 1, o.guard || "G");
  },
  glaive(c, o = {}) {
    c.cap([[0, -4], [0, 9.5]], 0.75, 0.75, o.haft || "w");
    c.poly([[0.8, -10], [2.6, -7], [2.2, -3], [-0.8, -3.5], [-0.8, -8]], o.head || "s");
    c.rect(-1.2, -3.8, 1.2, -2.6, o.band || "G");
  },
  dual(c, o = {}) { c.at({ r: -32, x: -1.5 }, () => G.dagger(c, o)); c.at({ r: 32, x: 1.5 }, () => G.dagger(c, o)); }, // c
  knuckle(c, o = {}) { // brass knuckles, c
    const k = o.k || "z"; for (const x of [-3.6, -1.2, 1.2, 3.6]) c.ring(x, -1.5, 0.6, 1.6, k);
    c.cap([[-4.5, 1.2], [-2, 3.6], [2, 3.6], [4.5, 1.2]], 1, 1, k);
  },
  scepter(c, o = {}) {
    c.cap([[0, -4], [0, 9]], 0.75, 0.75, o.haft || "G");
    c.at({ x: -7, y: -12.5, s: 0.5 }, () => G.crown(c, { k: o.head || "G", gem: o.gem || "K" }));
    c.ell(0, -4.6, 1.3, 1.3, o.gem || "K");
  },
  holy(c, o = {}) { // holy symbol: ring with a sun cross, c
    c.ring(0, 0, 3.6, 5, o.k || "s"); c.rect(-0.8, -5.5, 0.8, 5.5, o.k || "s"); c.rect(-5.5, -0.8, 5.5, 0.8, o.k || "s"); c.ell(0, 0, 1.6, 1.6, o.gem || "H");
  },
  orbHeld(c, o = {}) { // c: orb gripped by three claws on a short stem
    c.at({ y: -1.2 }, () => G.orb(c, { k: o.k || "A", r: 4.3 }));
    for (const fx of [false, true]) c.at({ fx }, () => c.cap([[1.2, 6], [4, 3.5], [4.6, 0], [3.6, -2.6]], 0.75, 0.35, o.claw || "G"));
    c.cap([[0, 6.4], [0, 2.4]], 0.8, 0.5, o.claw || "G");
  },
  totem(c, o = {}) { // carved post, c
    c.rect(-2.6, -6, 2.6, 6.5, o.k || "w"); c.rect(-3.6, -6.5, 3.6, -4.8, o.k || "w");
    c.px(-1.2, -2.5, o.eye || "N"); c.px(1, -2.5, o.eye || "N"); c.line(-1.2, 0.5, 1.2, 0.5, "k"); c.line(-2.6, 3, 2.6, 3, "V");
    c.cap([[2.6, -5], [5, -6.5]], 0.6, 0.3, o.leaf || "n"); c.cap([[-2.6, -5], [-5, -6.5]], 0.6, 0.3, o.leaf || "n");
  },
  quiver(c, o = {}) { // c
    c.at({ r: 20 }, () => { c.rect(-2.4, -2, 2.4, 6.5, o.k || "l"); c.rect(-2.6, -2.5, 2.6, -1.2, o.band || "z");
      for (const x of [-1.4, 0, 1.4]) { c.line(x, -6, x, -2.6, "w"); c.poly([[x, -7.4], [x + 1, -5.6], [x - 1, -5.6]], o.fletch || "r"); } });
  },
  bell(c, o = {}) { // c
    c.poly([[-1.6, -4.5], [1.6, -4.5], [2.6, -1], [4.5, 3.5], [-4.5, 3.5], [-2.6, -1]], o.k || "G");
    c.ring(0, -5.5, 0.6, 1.4, o.k || "G"); c.ell(0, 4.6, 1.2, 1.2, o.clap || "z");
  },
  beads(c, o = {}) { // prayer beads, c
    for (let i = 0; i < 10; i++) { const a = (i / 10) * Math.PI * 2; c.ell(Math.cos(a) * 4.2, Math.sin(a) * 3.6 - 1.2, 0.95, 0.95, o.k || "w"); }
    c.cap([[0, 2.4], [0, 6.2]], 0.5, 0.5, o.cord || "r"); c.at({ x: 0, y: 5.2, s: 0.35 }, () => G.cross(c, { k: o.charm || "G" }));
  },
  horn(c, o = {}) { // war horn, c
    c.cap([[-5.5, 3], [-2, 0.5], [2, -0.5], [5, -3.5]], 0.6, 2.3, o.k || "b");
    c.line(-1.4, -0.6, -0.6, 1.8, o.band || "z"); c.line(2, -1.8, 2.6, 0.8, o.band || "z");
  },
  crystal(c, o = {}) { // c
    c.poly([[0, -6], [2.6, -3.5], [2.6, 3.5], [0, 6], [-2.6, 3.5], [-2.6, -3.5]], o.k || "B");
    c.line(0, -5, 0, 5, o.facet || "W");
  },
  seed(c, o = {}) { c.ell(0, 1.2, 3.4, 4.4, o.k || "E"); c.cap([[0, -3], [0.5, -5.5], [2.5, -6.5]], 0.6, 0.5, o.sprout || "n"); }, // c
  medal(c, o = {}) { // ribbon and disc, icon space
    c.poly([[3.5, 0.5], [6.5, 0.5], [7.5, 6], [5, 6]], o.ribbon || "r"); c.poly([[10.5, 0.5], [7.5, 0.5], [6.5, 6], [9, 6]], o.ribbon2 || o.ribbon || "r");
    c.ell(7, 9.4, 4, 4, o.k || "G"); c.at({ x: 7, y: 9.4, s: 0.45 }, () => G.star(c, { n: 5, k: o.inner || "z", core: false, r0: 2.6, r1: 6 }));
  },
  locket(c, o = {}) { G.pendant(c, { gem: o.gem || "z", frame: o.k || "z", chain: o.chain || "z", r: 3.4 }); c.line(7, 7, 7, 12.2, "k"); },
  compass(c, o = {}) { // icon space
    c.ell(7, 7.5, 6, 6, o.k || "z"); c.ell(7, 7.5, 4.6, 4.6, o.face || "c"); c.ring(7, 1.2, 0.5, 1.3, o.k || "z");
    c.at({ x: 7, y: 7.5, r: 30 }, () => { c.poly([[0, -4], [1.2, 0], [-1.2, 0]], o.needle || "C"); c.poly([[0, 4], [1.2, 0], [-1.2, 0]], "S"); });
  },
  cap_(c, o = {}) { // soft cap, icon space
    c.ell(7, 7.5, 5.6, 5, o.k || "l"); c.rect(1, 7.5, 13, 9.5, o.k || "l"); c.rect(0.5, 9, 13.5, 10.6, o.brim || "l");
    if (o.feather) c.at({ x: 11, y: 3.5, r: 30, s: 0.55 }, () => G.feather(c, { k: o.feather }));
  },
  circlet(c, o = {}) { // icon space
    c.at({ x: 7, y: 8, sy: 0.5 }, () => c.ring(0, 0, 4.6, 6, o.k || "G"));
    c.at({ x: 7, y: 4.6 }, () => G.gem(c, { k: o.gem || "B" }));
  },
  headband(c, o = {}) { // icon space
    c.at({ x: 7, y: 7, sy: 0.5 }, () => c.ring(0, 0, 4.6, 6.2, o.k || "r"));
    c.cap([[1.8, 7.4], [0.6, 10.5], [1.5, 13]], 0.6, 0.5, o.k || "r"); c.cap([[2.6, 7.6], [2.8, 11], [4, 13]], 0.6, 0.5, o.k || "r");
    c.ell(7, 10, 1.2, 1, o.gem || "G");
  },
  mask(c, o = {}) { // icon space
    c.poly([[2, 2], [12, 2], [12.5, 8], [10, 12.5], [7, 13.5], [4, 12.5], [1.5, 8]], o.k || "S");
    c.rect(3.2, 5.5, 6, 6.8, "k"); c.rect(8, 5.5, 10.8, 6.8, "k"); c.line(7, 3, 7, 11.5, o.ridge || "s");
    if (o.bars) for (const x of [4.5, 7, 9.5]) c.line(x, 9, x, 12.5, o.bars);
  },
  laurel(c, o = {}) { // icon space: two branches rising from the bottom, leaves along each arc pointing up the branch
    for (const fx of [false, true]) c.at({ x: 7, y: 7.2, fx }, () => {
      c.arc(0, 0, 4.6, 5.3, 105, 250, o.stem || o.k || "G");
      for (const [deg, off] of [[125, 1], [155, -1], [185, 1], [215, -1], [245, 0]]) {
        const a = (deg * Math.PI) / 180, r = 5 + off * 0.9;
        c.at({ x: Math.cos(a) * r, y: Math.sin(a) * r, r: deg + 180 + off * 25, s: 0.36 }, () => G.leaf(c, { k: o.k || "G", vein: o.k || "G" }));
      }
    });
    if (o.gem) c.ell(7, 12.2, 1.1, 1.1, o.gem);
  },
  veil(c, o = {}) { G.hood(c, { k: o.k || "v", face: "k", eyes: o.eyes }); c.with({ only: "k" }, () => { for (const x of [5, 7, 9]) c.line(x, 6, x, 12, o.lace || "x"); }); },
  antlers(c, o = {}) { // icon space
    c.at({ x: 7, y: 9.5, sy: 0.45 }, () => c.ring(0, 0, 3.8, 5.4, o.band || "V"));
    for (const fx of [false, true]) c.at({ x: 7, fx }, () => { c.cap([[3, 8], [4.5, 4], [6, 1]], 0.7, 0.4, o.k || "b"); c.cap([[4.4, 4.5], [2.6, 2]], 0.5, 0.3, o.k || "b"); c.cap([[5.3, 2.6], [6.8, 3.4]], 0.45, 0.3, o.k || "b"); });
    if (o.leaf) c.at({ x: 7, y: 9.5, s: 0.4 }, () => G.leaf(c, { k: o.leaf }));
  },
  torso(c, o = {}) { // body armour, icon space; kind: plate | chain | robe | coat | harness | vest | tunic
    const k = o.k || "s", kind = o.kind || "plate";
    if (kind === "robe") c.poly([[4, 1], [10, 1], [12.5, 4], [12.5, 7], [11, 7.5], [12.5, 13.5], [1.5, 13.5], [3, 7.5], [1.5, 7], [1.5, 4]], k);
    else c.poly([[4, 1], [10, 1], [13, 3.5], [13, 7.5], [11, 7.5], [11, 13], [3, 13], [3, 7.5], [1, 7.5], [1, 3.5]], k);
    c.poly([[5, 1], [9, 1], [7, 3.6]], "k");
    if (kind === "plate") { c.line(7, 4.5, 7, 12, o.trim || "S"); c.rect(3, 9.5, 11, 10.5, o.trim || "S"); }
    if (kind === "chain") c.with({ only: k }, () => { for (let y = 4; y < 13; y += 2) for (let x = 2 + (y % 4 ? 1 : 0); x < 13; x += 2) c.px(x + 0.5, y + 0.5, o.trim || "S"); });
    if (kind === "robe") { c.rect(6.4, 3.6, 7.6, 13.5, o.trim || "G"); c.rect(3, 7.2, 11, 8.2, o.belt || "l"); }
    if (kind === "coat") { c.line(7, 3.6, 7, 13, "k"); c.rect(3, 8.5, 11, 9.6, o.belt || "l"); c.px(8.2, 10.8, o.trim || "G"); }
    if (kind === "harness") { c.with({ only: k }, () => { c.cap([[3.5, 2], [10.5, 12]], 0.7, 0.7, o.trim || "l"); c.cap([[10.5, 2], [3.5, 12]], 0.7, 0.7, o.trim || "l"); }); c.ell(7, 7, 1.3, 1.3, o.boss || "z"); }
    if (kind === "vest" || kind === "tunic") { c.line(7, 3.6, 7, 9, o.trim || "l"); c.rect(3, 9.5, 11, 10.6, o.belt || "l"); }
    if (o.gem) c.ell(7, 6, 1.2, 1.2, o.gem);
  },
  legs(c, o = {}) { // icon space; kind: pants | greaves | sash | shackles
    const k = o.k || "l", kind = o.kind || "pants";
    if (kind === "sash") { c.rect(2, 2, 12, 5, o.k || "r"); c.poly([[6, 4.5], [9.5, 4.5], [11, 13], [7.5, 12]], o.k || "r"); c.ell(7.5, 3.5, 1.4, 1.4, o.gem || "G"); return; }
    c.rect(2.5, 1, 11.5, 3.5, o.belt || k); c.poly([[2.5, 3], [6.8, 3], [6.3, 13], [2.5, 13]], k); c.poly([[7.2, 3], [11.5, 3], [11.5, 13], [7.7, 13]], k);
    if (kind === "greaves") { c.rect(2.3, 6, 6.5, 7, o.trim || "S"); c.rect(7.5, 6, 11.7, 7, o.trim || "S"); c.ell(4.4, 6.5, 1.2, 1, o.knee || "s"); c.ell(9.6, 6.5, 1.2, 1, o.knee || "s"); }
    if (kind === "shackles") { c.rect(2, 10, 6.8, 11.5, "S"); c.rect(7.2, 10, 12, 11.5, "S"); c.line(6.8, 11, 7.2, 11, "S"); c.with({ only: "." }, () => c.cap([[4.5, 12], [7, 13.5], [9.5, 12]], 0.5, 0.5, "S")); }
    c.ell(7, 2.2, 1, 0.8, o.buckle || "G");
  },
  bootI(c, o = {}) { // footwear in icon space; kind: boot | plate | sandal | tabi | wing
    const k = o.k || "l", kind = o.kind || "boot";
    if (kind === "sandal") { // bare foot laced into a sandal
      c.poly([[3, 0.8], [7, 0.8], [7, 7.5], [12.5, 9], [12.8, 11.8], [2.5, 11.8], [2.5, 5]], "f");
      c.with({ only: "f" }, () => { for (const y of [2, 5, 8.6]) c.rect(0, y, 14, y + 1, k); c.line(7, 8.2, 9.5, 11.8, k); });
      c.rect(2.2, 11.6, 13, 13.2, o.sole || "w");
    }
    else {
      c.poly([[2.5, 0.8], [7.8, 0.8], [7.8, 8], [12.5, 9.3], [12.8, 12.8], [2.5, 12.8]], k);
      if (kind === "plate") { c.line(2.5, 4, 7.8, 4, o.trim || "S"); c.line(7.8, 9, 12.5, 10.3, o.trim || "S"); }
      if (kind === "tabi") c.line(10.4, 9.2, 10.4, 12.8, "k");
      c.rect(2, 0.5, 8.2, 2.4, o.cuff || k); c.rect(2.5, 12, 12.8, 13.2, o.sole || "S");
    }
    if (kind === "wing" || o.wing) c.at({ x: 2.5, y: 4.5, r: -30, s: 0.5 }, () => G.wing(c, { k: o.wing || "c", fx: true }));
  },
  belt(c, o = {}) { c.rect(0.5, 5, 13.5, 9, o.k || "l"); c.rect(5, 4, 9, 10, o.buckle || "G"); c.rect(6.2, 5.6, 7.8, 8.4, o.k || "l"); }, // icon space
  anklet(c, o = {}) { c.at({ x: 7, y: 6, sy: 0.5 }, () => c.ring(0, 0, 3.6, 4.8, o.k || "G")); for (const x of [3.5, 7, 10.5]) c.ell(x, 9 + (x === 7 ? 1 : 0), 1.1, 1.3, o.charm || "B"); },
  fang(c, o = {}) { c.cap([[1, 1], [7, 4], [13, 1]], 0.5, 0.5, o.cord || "l"); c.poly([[5.2, 4], [8.8, 4], [8, 9], [7, 13], [6, 9]], o.k || "b"); c.line(7, 5, 7, 10, o.ridge || "x"); },
  bomb(c, o = {}) { c.ell(6.5, 8.5, 5, 5, o.k || "x"); c.rect(5, 2.5, 8, 4.5, o.cap || "S"); c.cap([[6.5, 3], [8.5, 1.2], [10.5, 1.5]], 0.45, 0.45, "c"); c.at({ x: 11, y: 2, s: 0.45 }, () => G.star(c, { k: o.spark || "F", r1: 4, r0: 1.2 })); c.px(4, 6, "W"); },
  lantern(c, o = {}) {
    c.ring(7, 1.6, 0.7, 1.5, o.k || "S"); c.poly([[3.5, 3], [10.5, 3], [11.5, 5], [2.5, 5]], o.k || "S");
    c.rect(3.5, 5, 10.5, 11.5, o.glass || "q"); c.with({ only: o.glass || "q" }, () => c.at({ x: 7, y: 8.5, s: 0.55 }, () => G.flame(c, { k: o.glow || "C" })));
    c.line(3.5, 5, 3.5, 11.5, o.k || "S"); c.line(10.5, 5, 10.5, 11.5, o.k || "S"); c.rect(2.5, 11.5, 11.5, 13, o.k || "S");
  },
  bandage(c, o = {}) { c.ell(6, 7, 4.8, 4.8, o.k || "c"); c.ring(6, 7, 1.2, 2.2, "l"); c.poly([[8, 10.5], [13, 11], [12, 13.5], [7.5, 11.8]], o.k || "c"); if (o.cross) c.at({ x: 10.5, y: 3, s: 0.45 }, () => G.cross(c, { k: o.cross })); },
  pouch(c, o = {}) { c.ell(7, 9, 5.4, 4.6, o.k || "l"); c.poly([[4, 2.5], [10, 2.5], [8.4, 5.5], [5.6, 5.5]], o.k || "l"); c.rect(4.6, 4.8, 9.4, 6, o.tie || "r"); if (o.spill) c.at({ x: 7, y: 9.5, s: 0.5 }, () => o.spill(c)); },
  bread(c, o = {}) { c.ell(7, 8, 6, 4, o.k || "z"); for (const x of [4.5, 7, 9.5]) c.line(x - 1, 6, x + 1, 9, o.score || "c"); c.rect(1.5, 11, 12.5, 12.5, o.cloth || "c"); },
  jug(c, o = {}) { c.ell(7, 9, 4.6, 4.4, o.k || "z"); c.rect(5.6, 2, 8.4, 5.5, o.k || "z"); c.arc(9.5, 6, 1.4, 2.4, -90, 90, o.k || "z"); c.rect(5, 0.5, 9, 2.2, o.cork || "w"); if (o.drop) c.at({ x: 3.5, y: 4, s: 0.45 }, () => G.drop(c, { k: o.drop })); },
  urn(c, o = {}) { c.poly([[4, 3], [10, 3], [12, 7], [10, 12], [4, 12], [2, 7]], o.k || "o"); c.rect(3.5, 1.5, 10.5, 3, o.lid || o.k || "o"); c.rect(4.5, 12, 9.5, 13.5, o.k || "o"); if (o.glow) c.at({ x: 7, y: 1, s: 0.45 }, () => G.flame(c, { k: o.glow })); },
  incense(c, o = {}) { c.ell(7, 11, 5, 2.4, o.k || "z"); c.rect(3, 9, 11, 11, o.k || "z"); c.cap([[7, 8.5], [5.5, 6], [8, 4], [6, 1]], 0.9, 0.4, o.smoke || "Z"); c.cap([[9, 8.5], [10.5, 5.5], [9.5, 3]], 0.7, 0.3, o.smoke || "Z"); },
  ingot(c, o = {}) { c.poly([[3, 4], [11, 4], [13, 11], [1, 11]], o.k || "G"); c.poly([[3, 4], [11, 4], [10, 6], [4, 6]], o.top || o.k || "G"); c.px(4.5, 5, "W"); },
  scrap(c, o = {}) { c.poly([[1.5, 9], [6, 5], [8, 7.5], [4, 12]], o.k || "S"); c.poly([[7, 2], [12, 3.5], [11, 6.5], [6.5, 5]], o.k || "S"); c.cap([[8.5, 9], [12.5, 12]], 0.9, 0.9, o.k2 || "z"); c.ring(10.5, 10.5, 0.4, 1.3, "o"); },
  pile(c, o = {}) { c.poly([[1, 12.5], [4, 7], [7, 4.5], [10, 7], [13, 12.5]], o.k || "o"); for (const [x, y] of o.dots || []) c.px(x, y, o.dot || "C"); }, // icon space heap
  scale(c, o = {}) { c.poly([[7, 1], [12, 5], [11, 10.5], [7, 13], [3, 10.5], [2, 5]], o.k || "r"); c.line(7, 2.5, 7, 11.5, o.ridge || "E"); c.arc(7, 13, 5, 6, 200, 340, o.rim || "E"); },
  marrow(c, o = {}) { c.at({ x: 7, y: 7, r: 45 }, () => { c.rect(-1.4, -4.5, 1.4, 4.5, o.k || "b"); for (const y of [-5, 5]) { c.ell(-1.4, y, 1.7, 1.7, o.k || "b"); c.ell(1.4, y, 1.7, 1.7, o.k || "b"); } c.rect(-0.6, -3.5, 0.6, 3.5, o.marrow || "K"); }); },
  castle(c, o = {}) { // icon space
    const k = o.k || "o"; c.rect(2, 4, 12, 13, k); for (const x of [2, 5.3, 8.6, 11]) c.rect(x, 1.5, x + 1.4, 4, k);
    c.ell(7, 9.5, 2, 2, "k"); c.rect(5, 9.5, 9, 13, "k"); c.line(2, 6.5, 12, 6.5, o.line || "S");
  },
  trap(c, o = {}) { c.arc(7, 9, 3.5, 5, 180, 360, o.k || "S"); for (const x of [3.5, 5.5, 7, 8.5, 10.5]) c.poly([[x - 0.7, 9], [x + 0.7, 9], [x, 7]], o.k || "S"); c.rect(1, 9, 13, 10.4, o.k || "S"); c.ring(7, 11.5, 0.6, 1.5, o.k || "S"); },
  coffin(c, o = {}) { c.poly([[4.5, 0.5], [9.5, 0.5], [12, 4], [10, 13.5], [4, 13.5], [2, 4]], o.k || "w"); c.at({ x: 7, y: 5.6, s: 0.55 }, () => G.cross(c, { k: o.cross || "S" })); c.rect(2.5, 9.5, 11.5, 10.5, o.band || "S"); },
  angel(c, o = {}) { // c: halo over a glowing spirit between spread wings
    c.at({ x: -0.6, y: 0.5, fx: true }, () => G.wing(c, { k: o.k || "c" })); c.at({ x: 0.6, y: 0.5 }, () => G.wing(c, { k: o.k || "c" }));
    c.poly([[0, -2], [1.6, 5.5], [-1.6, 5.5]], o.body || "H"); c.ell(0, -2.6, 1.3, 1.3, o.body || "H");
    c.at({ y: -5.4, s: 0.5 }, () => G.halo(c, { k: o.halo || "H" }));
  },
  meteor(c, o = {}) { // c: rock falling from the top right
    c.cap([[5.5, -5.5], [0, 0]], 0.6, 3, o.tail || "F"); c.ell(-1.5, 1.5, 3.2, 3.2, o.k || "E"); c.ell(-2.2, 1, 1.3, 1.3, o.core || "F");
  },
  whirl(c, o = {}) { // c: two curved arrows chasing each other
    for (const r of [0, 180]) c.at({ r }, () => { c.arc(0, 0, 4, 5.3, 200, 340, o.k || "T"); c.poly([[5.2, -0.8], [6.6, -3.2], [3.3, -2.4]].map(([x, y]) => [x, y]), o.k || "T"); });
  },
  wall(c, o = {}) { // icon space bricks
    const k = o.k || "o"; c.rect(1, 3, 13, 12, k);
    for (const y of [5.2, 8.2]) c.line(1, y, 13, y, o.mortar || "S");
    for (const [x, y0, y1] of [[5, 3, 5], [9, 3, 5], [3, 5.5, 8], [7, 5.5, 8], [11, 5.5, 8], [5, 8.5, 12], [9, 8.5, 12]]) c.line(x, y0, x, y1, o.mortar || "S");
  },
  foot(c, o = {}) { // c: a wrapped foot kicking to the right (heel left, toes right)
    const k = o.k || "f";
    c.rect(-5.5, -6.5, -1.5, -1, o.wrap || "c"); c.line(-5.5, -4, -1.5, -4, o.wrapLine || "l");
    c.poly([[-5.5, -1.2], [-1.5, -1.2], [2, 0.2], [5.5, 0.6], [6.4, 2], [5.6, 3.6], [-5.5, 3.6]], k);
    c.rect(-5.5, 2.8, 5.8, 3.8, o.sole || "h");
  },
  knee(c, o = {}) { // c: bent leg, knee driving up and right
    c.cap([[-6, 3.5], [0.5, -1.5]], 1.9, 1.7, o.k || "r"); c.cap([[0.5, -1.5], [-1.5, 5.5]], 1.4, 1.2, o.shin || "f");
    c.ell(1, -1.6, 2.1, 2.1, o.pad || "c"); c.rect(-3, 5, 0.5, 6.5, o.shin || "f");
  },
  dragon(c, o = {}) { // c: dragon head in profile facing right, jaws open
    const k = o.k || "F";
    c.cap([[-3, -2.8], [-6.4, -6]], 1, 0.3, o.horn || "b"); c.cap([[-1, -3.4], [-3, -6.8]], 0.9, 0.3, o.horn || "b");
    c.poly([[-6.5, 1], [-4.5, -2.6], [-0.5, -4], [3.5, -3.2], [6.8, -1.6], [6.8, -0.2], [1.5, 0.2], [-1, 1.5]], k);
    c.poly([[1, 1.2], [4.6, 1.9], [4.4, 3.2], [0, 3.6], [-3.5, 3.4], [-6.5, 5.5], [-6.5, 1], [-1, 1.5]], o.jaw || k);
    c.px(0, -2.2, o.eye || "Y"); c.px(5.6, -1.2, "k"); c.px(3, 0.4, "W"); c.px(4.6, 0.6, "W"); c.px(2.6, 1.6, "W");
  },
  smoke(c, o = {}) { G.cloud(c, { k: o.k || "Z" }); c.at({ y: -4, x: -1, s: 0.6 }, () => G.cloud(c, { k: o.k || "Z" })); }, // c
  xmark(c, o = {}) { c.cap([[-4.5, -4.5], [4.5, 4.5]], 1, 1, o.k || "K"); c.cap([[4.5, -4.5], [-4.5, 4.5]], 1, 1, o.k || "K"); }, // c
  ripple(c, o = {}) { c.ring(0, 0, 5, 6.2, o.k || "I"); c.ring(0, 0, 2.2, 3.2, o.k || "I"); }, // c
  hourglass(c, o = {}) { c.rect(-4, -6, 4, -5, o.frame || "w"); c.rect(-4, 5, 4, 6, o.frame || "w"); c.poly([[-3, -5], [3, -5], [0.6, 0], [3, 5], [-3, 5], [-0.6, 0]], o.k || "q"); c.with({ only: o.k || "q" }, () => { c.rect(-3, 2.5, 3, 5, o.sand || "z"); c.rect(-1, -2, 1, -0.5, o.sand || "z"); }); }, // c
  rain(c, o = {}) { for (const [x, y] of [[-4, -3], [0, -5], [4, -2], [-2, 2], [2.5, 3]]) c.at({ x, y, r: 200, s: 0.45 }, () => G.arrow(c, o)); }, // c
  plus(c, o = {}) { c.rect(-0.9, -3, 0.9, 3, o.k || "N"); c.rect(-3, -0.9, 3, 0.9, o.k || "N"); }, // c
  sprout(c, o = {}) { c.cap([[0, 5], [0, -1]], 0.7, 0.6, o.stem || "m"); c.at({ x: -2, y: -2, r: -50, s: 0.55 }, () => G.leaf(c, { k: o.k || "n" })); c.at({ x: 2, y: -3, r: 50, s: 0.55 }, () => G.leaf(c, { k: o.k || "n" })); }, // c
  blackSun(c, o = {}) { G.star(c, { n: 8, r0: 3.6, r1: 6.6, k: o.ray || "D", core: false, rot: 0.2 }); c.ell(0, 0, 4, 4, o.rim || "H"); c.ell(0, 0, 3, 3, "k"); }, // c
  hat(c, o = {}) { // pointed wizard hat, icon space
    c.cap([[7, 9], [8, 4], [10.5, 1]], 3, 0.4, o.k || "u"); c.ell(7, 10.3, 6.4, 2, o.k || "u"); c.rect(3.5, 7.8, 10.5, 9.2, o.band || "G");
    if (o.star) c.at({ x: 6, y: 5, s: 0.35 }, () => G.star(c, { n: 4, k: o.star }));
  },
  pearl(c, o = {}) { c.ell(0, 0, 4.4, 4.4, o.k || "c"); c.with({ only: o.k || "c" }, () => { c.cap([[1, 4.5], [1.5, 1.5], [3.5, -0.5]], 0.6, 0.4, o.vein || "D"); c.cap([[-1, 4.5], [-2, 2]], 0.5, 0.3, o.vein || "D"); }); c.px(-1.8, -2, "W"); }, // c
});

module.exports = { Canvas, render, G, L, M, N };

// Every icon the game needs, in catalogue order, read straight from the C# data so ids and names stay in sync.
function catalogue() {
  const fs = require("fs"), path = require("path"), D = path.join(__dirname, "../../Assets/Scripts/Data/");
  const A = fs.readFileSync(D + "Database.cs", "utf8"), B = fs.readFileSync(D + "CatalogueDatabase.cs", "utf8");
  const blocks = (src, cls) => {
    const out = [], re = new RegExp("new " + cls + "\\s*\\{", "g"); let m;
    while ((m = re.exec(src))) { let i = src.indexOf("{", m.index), d = 0, j = i; for (; j < src.length; j++) { if (src[j] === "{") d++; else if (src[j] === "}" && !--d) break; } out.push(src.slice(i, j + 1)); }
    return out;
  };
  const f = (b, k) => (b.match(new RegExp("\\b" + k + "\\s*=\\s*\"([^\"]*)\"")) || [])[1] || "";
  const e = (b, k) => (b.match(new RegExp("\\b" + k + "\\s*=\\s*\\w+\\.(\\w+)")) || [])[1] || "";
  const S = require("./iconsets.js"), row = (id, name, fn, group) => ({ id, name, fn, group });
  const items = [...blocks(A, "ItemDef"), ...blocks(B, "ConsumableDef"), ...blocks(B, "MaterialDef"), ...blocks(B, "KeyItemDef")].map((b) => row(f(b, "id"), f(b, "name"), S.ITEMS[f(b, "id")], "items"));
  const gear = [...blocks(A, "EquipmentDef"), ...blocks(B, "EquipmentDef")].map((b) => row(f(b, "id"), f(b, "name"), S.gearIcon(f(b, "id"), f(b, "name"), e(b, "slot"), e(b, "weaponType")), "gear"));
  const skills = blocks(A, "SkillDef").map((b) => row(f(b, "id"), f(b, "name"), S.SKILLS[f(b, "id")], f(b, "classId") || "shared"));
  const passives = blocks(A, "PassiveDef").map((b) => row(f(b, "id"), f(b, "name"), S.PASSIVES[f(b, "id")], f(b, "classId")));
  const enumSrc = fs.readFileSync(path.join(__dirname, "../../Assets/Scripts/Combat/Status.cs"), "utf8").match(/enum StatusId\s*\{([^}]*)\}/)[1];
  const status = enumSrc.replace(/\/\/.*$/gm, "").split(",").map((x) => x.trim()).filter(Boolean).map((id) => row(id, id.replace(/([a-z])([A-Z])/g, "$1 $2"), S.STATUS[id], "status"));
  const racial = [...blocks(A, "RacialFeatDef").map((b) => row(f(b, "id"), f(b, "name"), S.RACIAL[f(b, "id")] || S.RACIAL._sigil, "racial")), row("_sigil", "Racial", S.RACIAL._sigil, "racial")];
  const elSrc = fs.readFileSync(path.join(__dirname, "../../Assets/Scripts/Combat/Elements.cs"), "utf8").match(/enum Element\s*\{([^}]*)\}/)[1];
  const elements = elSrc.replace(/\/\/.*$/gm, "").split(",").map((x) => x.trim()).filter((x) => x && !x.includes("= Dark")).map((x) => x.split("=")[0].trim()).map((id) => row(id, id, S.ELEMENTS[id], "elements"));
  // Talents: shared ones are TalentDef blocks with a literal id; class ones come from the classTalents table (id rule as in C#).
  const T = fs.readFileSync(D + "TalentSystem.cs", "utf8");
  const talents = blocks(T, "TalentDef").filter((b) => f(b, "id")).map((b) => row(f(b, "id"), f(b, "name"), S.TALENTS[f(b, "id")], "talents"));
  for (const [, cid, names] of T.matchAll(/\("(\w+)",\s*new\[\]\s*\{([^}]*)\}\)/g))
    for (const [, n] of names.matchAll(/"([^"]+)"/g)) { const id = cid + "_" + n.toLowerCase().replace(/ /g, "_").replace(/'/g, ""); talents.push(row(id, n, S.TALENTS[id], "talents")); }
  return { items, gear, skills, passives, status, racial, elements, talents };
}
module.exports.catalogue = catalogue;

if (require.main === module && process.argv.includes("--test")) {
  const out = process.argv[2] || "ArtPreview";
  const T = {
    sword: (c) => c.at({ x: 7, y: 7, r: 45 }, () => G.sword(c)),
    dagger: (c) => c.at({ x: 7, y: 7, r: 45 }, () => G.dagger(c)),
    axe: (c) => c.at({ x: 7.5, y: 7.5, r: 30 }, () => G.axe(c)),
    hammer: (c) => c.at({ x: 7, y: 7, r: 35 }, () => G.hammer(c)),
    mace: (c) => c.at({ x: 7, y: 7.5, r: 35 }, () => G.mace(c)),
    spear: (c) => c.at({ x: 7, y: 7, r: 45 }, () => G.spear(c)),
    scythe: (c) => c.at({ x: 7.5, y: 7, r: 25 }, () => G.scythe(c)),
    staff: (c) => c.at({ x: 7, y: 7, r: 35 }, () => G.staff(c, { gem: "B", prongs: "G" })),
    bow: (c) => c.at({ x: 7, y: 7, r: 45 }, () => G.bow(c)),
    crossbow: (c) => c.at({ x: 7, y: 7.5, r: 0 }, () => G.crossbow(c)),
    shield: (c) => G.shield(c, { boss: "G" }),
    potion: (c) => G.flask(c),
    mana: (c) => G.flask(c, { liquid: "B", shape: "tall" }),
    vial: (c) => G.flask(c, { liquid: "O", shape: "vial" }),
    flame: (c) => c.at({ x: 7, y: 7.5 }, () => G.flame(c)),
    snow: (c) => c.at({ x: 7, y: 7 }, () => G.snow(c)),
    bolt: (c) => c.at({ x: 7, y: 7 }, () => G.bolt(c)),
    skull: (c) => c.at({ x: 7, y: 7.5 }, () => G.skull(c)),
    helm: (c) => G.helm(c, { crest: "r" }),
    hood: (c) => G.hood(c, { eyes: "C" }),
    crown: (c) => G.crown(c, { gem: "K" }),
    ring: (c) => G.ring(c, { gem: "B" }),
    boot: (c) => c.at({ x: 6.5, y: 7.5 }, () => G.boot(c, { cuff: "c", sole: "S" })),
    leaf: (c) => c.at({ x: 7, y: 7, r: 30 }, () => G.leaf(c)),
    heart: (c) => c.at({ x: 7, y: 7 }, () => G.heart(c)),
    book: (c) => G.book(c, { sigil: (c) => G.star(c, { n: 5, k: "G", core: false }) }),
    fist: (c) => c.at({ x: 7.5, y: 7.5 }, () => G.fist(c)),
    feather: (c) => c.at({ x: 7, y: 7 }, () => G.feather(c)),
    sun: (c) => c.at({ x: 7, y: 7 }, () => G.sun(c)),
    eye: (c) => c.at({ x: 7, y: 7 }, () => G.eye(c)),
  };
  const items = Object.entries(T).map(([label, fn]) => { const c = new Canvas(); fn(c); return { buf: render(c), label }; });
  P.save(P.sheet(items.map((i) => ({ ...i, scale: 4 })), { cols: 10 }), out + "/icons_test.png");
  P.save(P.sheet(items.map((i) => ({ buf: i.buf, label: "", scale: 1 })), { cols: 10, gap: 4 }), out + "/icons_test_1x.png");
}

// node icons.js --export : Assets/Resources/Icons/<Items|Gear|Skills|Passives|Status>/<id>.png (read by IconArt.cs).
// Import settings are copied from the FX sheets (point filter, no mips, uncompressed); an existing .meta keeps its guid.
if (require.main === module && process.argv.includes("--export")) {
  const fs = require("fs"), path = require("path"), crypto = require("crypto");
  const assets = path.resolve(__dirname, "../../Assets"), meta = fs.readFileSync(path.join(assets, "Resources/Fx/hit.png.meta"), "utf8");
  const folders = { items: "Items", gear: "Gear", skills: "Skills", passives: "Passives", status: "Status", racial: "Racial", elements: "Elements", talents: "Talents" };
  for (const [group, rows] of Object.entries(catalogue())) {
    const dir = path.join(assets, "Resources/Icons", folders[group]), done = new Set();
    fs.mkdirSync(dir, { recursive: true });
    for (const r of rows) {
      if (done.has(r.id)) continue; done.add(r.id);
      const c = new Canvas(); r.fn(c);
      const file = path.join(dir, r.id + ".png"); P.save(render(c), file);
      if (!fs.existsSync(file + ".meta")) fs.writeFileSync(file + ".meta", meta.replace(/^guid: \w+/m, "guid: " + crypto.randomBytes(16).toString("hex")));
    }
    console.log("exported", folders[group], done.size);
  }
  process.exit(0);
}

// Preview sheets (3x, labelled) and a 1x check sheet per group.
if (require.main === module && !process.argv.includes("--test")) {
  const out = process.argv[2] || "ArtPreview", cat = catalogue(), missing = [];
  const label = (s) => s.toUpperCase().replace(/[^A-Z0-9 \-.:/+()#]/g, "").slice(0, 11);
  const only = (process.argv.find((a) => a.startsWith("--only=")) || "").slice(7).split(",").filter(Boolean);
  if (only.length) { // close-up check of a few icons at 4x and 1x
    const rows = Object.values(cat).flat().filter((r) => only.includes(r.id) && r.fn);
    const bufs = [...new Map(rows.map((r) => [r.id, r])).values()].map((r) => { const c = new Canvas(); r.fn(c); return { buf: render(c), label: label(r.name) }; });
    P.save(P.sheet([...bufs.map((b) => ({ ...b, scale: 4 })), ...bufs.map((b) => ({ buf: b.buf, label: "", scale: 1 }))], { cols: bufs.length }), `${out}/icons_check.png`);
    process.exit(0);
  }
  const groupOnly = (process.argv.find((a) => a.startsWith("--group=")) || "").slice(8);
  for (const [group, rows] of Object.entries(cat)) {
    if (groupOnly && group !== groupOnly) continue;
    const seen = new Set(), list = rows.filter((r) => !seen.has(r.id) && seen.add(r.id));
    const bufs = list.map((r) => { if (!r.fn) { missing.push(group + ":" + r.id); return null; } const c = new Canvas(); r.fn(c); return { buf: render(c), label: label(r.name) }; }).filter(Boolean);
    P.save(P.sheet(bufs.map((b) => ({ ...b, scale: 3 })), { cols: 12 }), `${out}/icons_${group}.png`);
    P.save(P.sheet(bufs.map((b) => ({ buf: b.buf, label: "", scale: 1 })), { cols: 24, gap: 3 }), `${out}/icons_${group}_1x.png`);
    console.log(group, bufs.length);
  }
  if (missing.length) console.log("missing:", missing.join(" "));
}
