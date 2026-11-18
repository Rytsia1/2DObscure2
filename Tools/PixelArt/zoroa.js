// Zoroa, Harvester of Souls, boss scale: inner 46x58 -> 48x60 (exact 4x in a 192x240 combat box), ~2.2x hero height.
// Built from shapes (ellipses, polygons, tapered strokes) into a part map, then lit by the shared engine.
// Towering hood with curling ram horns, a cluster of pinprick eyes over hanging fangs,
// the cloak open on a ribcage with trapped souls, one long arm dangling the soul lantern from bony claws, the other
// hanging with long claws, and the cloak torn into tendrils. P2: eyes, throat, souls and lantern turn crimson, horns burn.
const W = 46, H = 58;

function build(p2) {
  const g = Array.from({ length: H }, () => Array(W).fill("."));
  const set = (x, y, k, only) => {
    x = Math.round(x); y = Math.round(y);
    if (x < 0 || y < 0 || x >= W || y >= H) return;
    if (only && !only.includes(g[y][x])) return;
    g[y][x] = k;
  };
  const ellipse = (cx, cy, rx, ry, k, only) => {
    for (let y = 0; y < H; y++) for (let x = 0; x < W; x++) if (((x - cx) / rx) ** 2 + ((y - cy) / ry) ** 2 <= 1) set(x, y, k, only);
  };
  const poly = (pts, k, only) => {
    for (let y = 0; y < H; y++) for (let x = 0; x < W; x++) {
      let inside = false;
      for (let i = 0, j = pts.length - 1; i < pts.length; j = i++) {
        const [xi, yi] = pts[i], [xj, yj] = pts[j];
        if (yi > y !== yj > y && x < ((xj - xi) * (y - yi)) / (yj - yi) + xi) inside = !inside;
      }
      if (inside) set(x, y, k, only);
    }
  };
  // tapered stroke along a polyline: radius goes from r0 at the start to r1 at the end
  const stroke = (pts, r0, r1, k, only) => {
    const seg = pts.slice(1).map((p, i) => Math.hypot(p[0] - pts[i][0], p[1] - pts[i][1])), total = seg.reduce((a, b) => a + b, 0);
    let run = 0;
    pts.slice(1).forEach((p, i) => {
      const [ax, ay] = pts[i];
      for (let s = 0; s <= seg[i]; s += 0.25) {
        const t = (run + s) / total, r = r0 + (r1 - r0) * t, px = ax + ((p[0] - ax) * s) / seg[i], py = ay + ((p[1] - ay) * s) / seg[i];
        const R = Math.ceil(r);
        for (let y = -R; y <= R; y++) for (let x = -R; x <= R; x++) if (x * x + y * y <= r * r + 0.3) set(px + x, py + y, k, only);
      }
      run += seg[i];
    });
  };
  const line = (x0, y0, x1, y1, k, only) => {
    const n = Math.max(Math.abs(x1 - x0), Math.abs(y1 - y0));
    for (let i = 0; i <= n; i++) set(x0 + ((x1 - x0) * i) / n, y0 + ((y1 - y0) * i) / n, k, only);
  };

  // back arm, hanging, with long claws (drawn first: the cloak covers its shoulder)
  stroke([[12, 24], [9, 31], [7, 37]], 2.5, 1.8, "A");
  ellipse(7, 38, 1.3, 1.3, "h");
  stroke([[6, 38], [4, 42], [3, 45]], 0.7, 0.4, "h");
  stroke([[7, 39], [7, 43], [6, 46]], 0.7, 0.4, "h");
  stroke([[8, 38], [10, 42], [10, 45]], 0.7, 0.4, "h");

  // cloak and its tendrils, with three folds falling from the shoulders
  poly([[11, 22], [33, 22], [37, 30], [41, 42], [42, 48], [2, 48], [3, 40], [7, 30]], "C");
  for (const t of [[[5, 46], [4, 51], [3, 56]], [[11, 46], [10, 52], [11, 57]], [[17, 46], [17, 50], [15, 54]], [[23, 46], [24, 51], [22, 57]],
    [[29, 46], [30, 50], [31, 55]], [[35, 46], [36, 51], [38, 57]], [[40, 46], [41, 49], [42, 53]]]) stroke(t, 2.2, 0.5, "C");
  line(13, 25, 9, 46, "q", "C"); line(19, 39, 18, 50, "q", "C"); line(32, 27, 37, 46, "q", "C");

  // open chest: dark cavity, spine and ribs, souls caught between the ribs
  poly([[19, 24], [29, 24], [31, 38], [18, 38]], "v"); // own letter: the face void "k" lags with the head, the chest must not
  // ribs leave the spine and slope down and out, like a real rib cage (chevrons, not a grid); shorter toward the bottom
  line(24, 24, 24, 38, "b");
  for (const [r, w] of [[25, 5], [28, 6], [31, 5], [34, 4]]) { line(24, r, 24 - w, r + 2, "b"); line(24, r, 24 + w, r + 2, "b"); }
  for (const [x, y] of [[21, 27], [27, 28], [22, 31], [28, 33], [20, 34], [26, 36]]) set(x, y, "g", "v");
  for (const [x, y] of [[27, 29], [21, 31]]) set(x, y, "G", "v");

  // hood with its tip falling back, then the dark face opening
  ellipse(23, 15, 10, 10, "H");
  stroke([[23, 8], [21, 3], [17, 1]], 3, 0.6, "H");
  ellipse(26, 17, 5.5, 6.5, "k");

  // thick ram horns curling out of the hood, dark growth rings across them
  stroke([[16, 9], [12, 4], [8, 3], [5, 6], [6, 9]], 2.6, 0.6, "N");
  stroke([[29, 8], [33, 3], [37, 2], [40, 5], [39, 8]], 2.6, 0.6, "N");
  for (const [x, y] of [[13, 5], [9, 3], [32, 4], [36, 2]]) { set(x, y, "n", "N"); set(x, y + 1, "n", "N"); }

  // eyes: two slits and a scatter of pinpricks; below them long fangs hang over a dark red throat
  for (const [x, y] of [[23, 14], [24, 14], [28, 14], [29, 14], [22, 12], [26, 11], [30, 12], [21, 16], [31, 16]]) set(x, y, "e");
  for (let y = 19; y <= 21; y++) for (let x = 23; x <= 29; x++) set(x, y, "K", "k");
  for (const [x, y0, y1] of [[23, 18, 20], [26, 18, 21], [29, 18, 20], [24, 21, 22], [28, 21, 22]]) for (let y = y0; y <= y1; y++) set(x, y, "t");

  // front arm reaching out, bony claws holding the chain of the soul lantern
  stroke([[32, 24], [37, 30], [41, 31]], 2.8, 2, "A");
  for (let y = 34; y <= 40; y++) set(42, y, "c");
  ellipse(42, 31, 1.4, 1.4, "h");
  stroke([[42, 32], [44, 35], [44, 38]], 0.7, 0.4, "h");
  stroke([[43, 31], [45, 33], [45, 36]], 0.7, 0.4, "h");
  stroke([[41, 32], [40, 36], [39, 38]], 0.7, 0.4, "h");
  // lantern: iron cap and base, a cage of four bars, the soul burning brightest in the middle
  for (let x = 41; x <= 42; x++) set(x, 40, "L");
  for (let x = 40; x <= 43; x++) { set(x, 41, "L"); set(x, 49, "L"); }
  for (let y = 42; y <= 48; y++) for (let x = 39; x <= 44; x++) set(x, y, y === 42 || y === 48 || x === 39 || x === 44 ? "L" : "l");
  for (let y = 43; y <= 47; y++) { set(40, y, "L"); set(43, y, "L"); }
  for (const [x, y] of [[41, 44], [42, 44], [41, 45], [42, 45], [41, 46]]) set(x, y, "G");

  if (p2) for (const [x, y, k] of [[8, 0, "f"], [7, 1, "f"], [9, 1, "f"], [8, 1, "F"], [37, 0, "f"], [36, 1, "f"], [38, 1, "f"], [37, 1, "F"]])
    if (g[y][x] === ".") g[y][x] = k;
  return g.map((r) => r.join(""));
}

module.exports = (P, R, E, SOUL, BLOOD, VOID, p2) => ({
  w: W, h: H, map: build(p2),
  parts: { N: [E.horn, 0.1, 2], n: [E.horn, -0.3, 2], H: [E.abyss, 0.2, 5], C: [E.abyss, 0.1, 6], q: [E.abyss, -0.25, 6],
    A: [E.abyss, 0.05, 2.5], h: [E.bone, -0.05, 1], b: [E.bone, -0.05, 1], s: [E.bone, 0.05, 1.5], L: [E.iron, -0.1, 1] },
  fixed: { k: VOID, v: VOID, e: p2 ? BLOOD[3] : SOUL[3], t: E.bone[2], K: p2 ? BLOOD[2] : BLOOD[0], c: E.iron[2],
    g: p2 ? BLOOD[1] : SOUL[1], G: p2 ? BLOOD[3] : SOUL[3], l: p2 ? BLOOD[1] : SOUL[1], f: SOUL[1], F: SOUL[3] },
  group: { C: "Cq", q: "Cq", N: "Nn", n: "Nn" }, noSeam: "Cq Nn",
  head: "NnHketKfF",
});
