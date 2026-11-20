import { Particles, Lights, P, PF, Rng, fit, hex, sparkle, band, ease, clamp, WHITE } from "../../core/index.js";

export const meta = {
  name: "Status mark",
  desc: "A status lands without touching the sprite: a white pop, floor ring, motes of its colour rising or sinking round the body, and bevelled arrows climbing (buff) or sinking (debuff).",
  tags: ["status", "buff", "debuff", "poison", "burn", "bleed", "curse", "marker", "one-shot"],
  size: [72, 80],
  origin: [36, 68],
  duration: 1.6,
  loop: false,
  fps: 30,
  bg: "ground",
  // arrows: 1 up (buff), -1 down (debuff), 0 none. motes: "rise" | "ember" | "drip".
  options: { palette: ["#2a1606", "#6a3a08", "#b8780e", "#e8b020", "#ffd84a", "#fff2a8", "#ffffff"], arrows: 1, motes: "rise", seed: 3 },
};

const CHEST = -20;
// arrow head (triangle) and stem, pointing up, centred on (0, 0): 9 px wide, 11 px tall
function inArrow(x, y) {
  if (y >= -6.5 && y < -0.5) return Math.abs(x) <= (y + 6.5) * 0.95 + 0.5;
  return y >= -0.5 && y <= 4.5 && Math.abs(x) <= 1.6;
}

export function create(o) {
  const C = fit(o.palette, 7).map(hex), R = new Rng(o.seed);
  const dir = Math.sign(o.arrows ?? 0), kind = o.motes ?? "rise";
  const ps = new Particles({ rng: R, ground: () => 0 }), lights = new Lights();
  lights.add(0, CHEST, 26, C[4], 0.8, 0.35);
  lights.add(0, -2, 22, C[3], 0.45, 1.1);
  // motes, staggered over the first 0.8 s round the body (about 22 x 38 px)
  for (let i = 0; i < 30; i++) {
    const x = R.range(-12, 12), delay = R.range(0, 0.8) * (i / 30 + 0.3);
    if (kind === "drip") ps.spawn(P.SPARK, x * 0.7, R.range(-34, -14), R.range(-6, 6), R.range(10, 40), R.range(0.5, 0.9), C[R.int(3, 5)], 1, delay);
    else if (kind === "ember") ps.spawn(P.EMBER, x, R.range(-12, -1), R.range(-8, 8), R.range(-40, -70), R.range(0.5, 0.9), C[R.int(3, 6)], 1, delay);
    else if (dir < 0) ps.spawn(P.MOTE, x, R.range(-40, -26), R.range(-3, 3), R.range(18, 34), R.range(0.5, 0.85), C[R.int(3, 6)], 1, delay);
    else ps.spawn(P.MOTE, x, R.range(-10, 0), R.range(-3, 3), R.range(-30, -55), R.range(0.45, 0.8), C[R.int(3, 6)], 1, delay);
  }
  if (kind === "ember") for (let i = 0; i < 12; i++) ps.spawn(P.FIRE, R.range(-10, 10), R.range(-14, -2), R.range(-6, 6), R.range(-25, -45), R.range(0.35, 0.6), 0, R.range(1, 2), R.range(0, 0.5), PF.SMOKES);
  for (let i = 0; i < 5; i++) ps.spawn(P.STAR, R.range(-14, 14), R.range(-38, -8), 0, dir >= 0 ? -6 : 6, R.range(0.4, 0.7), C[6], 1, R.range(0.2, 0.8));
  // three arrows: centre one big and first, the side ones smaller and later
  const arrows = dir === 0 ? [] : [
    { x: 0, y: CHEST - 4, s: 1, t0: 0.06 },
    { x: -12, y: CHEST + 6, s: 0.75, t0: 0.18 },
    { x: 12, y: CHEST + 4, s: 0.75, t0: 0.28 },
  ];
  let t = 0;

  function ring(p, near) {
    const u = clamp(t / 0.6, 0, 1);
    if (u >= 1) return;
    band(p, 0, 0, 4 + ease.outQuart(u) * 20, 0.3, 2 - u * 1.5, (1 - u) * 0.9, [C[2], C[3], C[5], C[6]], (g) => (Math.sin(g) > 0) === near, u < 0.15);
  }

  function arrow(p, a) {
    const age = t - a.t0, life = 1.05;
    if (age < 0 || age > life) return;
    const u = age / life, pop = age < 0.06; // a white frame as it appears
    const y = a.y - dir * (ease.outQuart(Math.min(1, u * 1.6)) * 8 + u * 6);
    // pops in a touch big, then shrinks away at the end rather than dithering into speckle
    const alpha = 1, s = a.s * (age < 0.1 ? 0.7 + 4 * age : u < 0.7 ? 1.1 - Math.min(0.1, (age - 0.1) * 0.5) : 1 - ease.inQuad((u - 0.7) / 0.3));
    if (s < 0.3) return;
    const inside = (x, yy) => inArrow(x / s, (dir > 0 ? yy : -yy) / s);
    p.shade(a.x - 7, y - 8, a.x + 7, y + 8, (x, yy, out) => {
      const lx = x - a.x, ly = yy - y;
      if (inside(lx, ly)) {
        // bevel: lit up-left, shadowed down-right
        const lit = !inside(lx - 1, ly - 1), dark = !inside(lx + 1, ly + 1);
        out.c = pop ? WHITE : lit ? C[6] : dark ? C[2] : C[4];
        out.a = alpha;
        out.l = pop || lit ? C[5] : 0;
        out.k = 0.25 * alpha;
        return;
      }
      if (inside(lx - 1, ly) || inside(lx + 1, ly) || inside(lx, ly - 1) || inside(lx, ly + 1)) { out.c = C[0]; out.a = alpha * 0.9; }
    });
  }

  return {
    update(dt) { t += dt; ps.update(dt); lights.update(dt); },
    drawBack(p) { ring(p, false); ps.draw(p, "back"); },
    draw(p) {
      ring(p, true);
      if (t < 0.12) sparkle(p, 0, CHEST, 8 * (1 - t / 0.12), WHITE, C[5]);
      ps.draw(p, "front");
      for (const a of arrows) arrow(p, a);
      lights.draw(p);
    },
  };
}
