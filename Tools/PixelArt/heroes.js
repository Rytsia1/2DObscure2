// Hero part maps for the shared generator. Run: node heroes.js <outDir> [id,id]
const { P, R, VOID, frames, DELAY, norm } = require("./engine.js");

// props drawn over the body map
const over = (rows, k, pts, onlyEmpty = false) => {
  const m = norm(rows).map((r) => [...r]);
  for (const [x, y] of pts) if (m[y] && m[y][x] !== undefined && (!onlyEmpty || m[y][x] === ".")) m[y][x] = k;
  return m.map((r) => r.join(""));
};
const vline = (x, y0, y1) => Array.from({ length: y1 - y0 + 1 }, (_, i) => [x, y0 + i]);

// flat heraldic shield: lit diagonal half, shaded half, darker rim lit on its top-left
const shield = (face, rim) => {
  const origin = (c) => {
    let sx = 99, sy = 99;
    for (let y = 0; y < c.H; y++) for (let x = 0; x < c.W; x++) if ((face + rim).includes(c.at(x, y))) { sx = Math.min(sx, x); sy = Math.min(sy, y); }
    return [sx, sy];
  };
  return {
    [face]: (x, y, c) => { const [sx, sy] = origin(c), d = x - sx + (y - sy); return d <= 2 ? R.gold[4] : d <= 7 ? R.gold[3] : R.gold[2]; },
    [rim]: (x, y, c) => { const [sx, sy] = origin(c), d = x - sx + (y - sy); return (c.at(x, y + 1) === face || c.at(x + 1, y) === face) && d <= 6 ? R.gold[2] : R.gold[1]; },
  };
};
const EYE = { I: VOID };

const HEROES = {
  knight: {
    map: [
      "......HHHHH.........", ".....HHHHHHH........", "....HHHHHHHHH.......", "....HHHHHHHHH.......",
      "....HHHHHVVgV.......", "....HHHHHHHHH.......", "....HHHHHHHHH.......", ".....HHHHHHH........",
      "...PPPTTTTTPPP......", "..PPPPTTTTTPPPYYYYY.", "..PPPPTTTTTYSSSSSSY.", "..AAATTTTTTYSSRSSSY.",
      "..AAATTTTTTYSRRRSSY.", "..AAATTTTTTYSSRSSSY.", "..GGATTTTTTYSSRSSSY.", "..GG.WWWOWWYSSRSSSY.",
      ".....KKKKKKYSSSSSSY.", ".....KKKKKK.YSSSSY..", ".....LLL.MMM.YSSY...", ".....LLL.MMM........",
      ".....LLL.MMM........", ".....LLL.MMM........", "....BBBB..CCC.......", "....BBBBB.CCCC......", "....BBBBB.CCCC......",
    ],
    parts: { H: [R.steel, 0.05, 3], P: [R.steel, 0, 2], T: [R.steel, -0.05, 3], A: [R.steel, -0.15, 2], G: [R.steel, -0.1, 1.5],
      K: [R.steel, -0.12, 2], L: [R.steel, -0.18, 1.5], M: [R.steel, -0.05, 1.5], B: [R.steel, -0.3, 1.5], C: [R.steel, -0.2, 1.5],
      W: [R.leather, 0, 1], O: [R.gold, 0.25, 1] },
    flat: shield("S", "Y"), fixed: { V: VOID, g: R.steel[1], R: R.red[2] }, noSeam: "WO",
    ground: "LMBC", head: "HVg",
  },

  fighter: {
    map: [
      ".....rr.............", "...rrrrHH...........", "..rrrHHHHHH.........", "..r.HHHHHHHH........",
      "....HHHHHHHH........", "....HHHFFFFH........", "....HHHFFIFF........", ".....HHFFFF.........",
      "...PPPTTTTTPP.......", "..PPPPTTTTTPPP......", ".YYYYPTTTTTPPAA.....", "YYSSSYTTTTTTAAA.....",
      "YSSRSSYTTTTTTA......", "YSRRRSYTTTTT.A......", "YSSRSSYTTTTT........", "YYSSSYWWWOWW........",
      ".YYYY.KKKKKK........", "......KKKKKK........", ".....LLL.NNN........", ".....LLL.NNN........",
      ".....LLL.NNN........", ".....LLL.NNN........", "....BBBB..DDD.......", "....BBBBB.DDDD......", "....BBBBB.DDDD......",
    ],
    parts: { r: [R.crimson, 0.1, 1.5], H: [R.steel, 0.05, 3], F: [R.skin, 0.05, 2], P: [R.steel, 0, 2], T: [R.crimson, 0, 3],
      A: [R.steel, -0.1, 1.5], h: [R.leather, 0.1, 1], W: [R.leather, 0, 1], O: [R.gold, 0.25, 1], K: [R.crimson, -0.2, 2],
      L: [R.steel, -0.18, 1.5], N: [R.steel, -0.05, 1.5], B: [R.leather, -0.1, 1.5], D: [R.leather, 0, 1.5], q: [R.gold, 0.1, 1] },
    flat: shield("S", "Y"), fixed: { ...EYE, R: R.red[2], X: R.steel[5], Z: R.steel[3] }, noSeam: "WO",
    ground: "LNBD", head: "rHFI",
    // upright sword: two-tone blade, gold guard and pommel, gloved fist on the grip
    after: (m) => {
      m = over(m, "X", vline(15, 1, 11));
      m = over(m, "Z", vline(16, 2, 11));
      m = over(m, "q", [[14, 12], [15, 12], [16, 12], [17, 12], [15, 14]]);
      return over(m, "h", [[14, 13], [15, 13]]);
    },
  },

  barbarian: {
    map: [
      "..O..........O......", "..OO........OO......", "...OOHHHHHHOO.......", "....HHHHHHHH........",
      "....HHHHHHHH........", "....bbFFFFFF........", "....bbFFFIFF........", "....bbbbbFbb........",
      "...UUbbbbbbbTT......", "..UUUUTTTTTTTAA.....", "..UUUTTTTTTTTTAA....", "..AAATTTTTTTT.......",
      "..AA.TTTTTTT........", "..AA.TTTTTTT........", "..AA.RRRRoRR........", "..hh.UUUUUUU........",
      ".....UUUUUUUU.......", ".....UUUUUUUUU......", ".....LLL.MMM........", ".....LLL.MMM........",
      ".....LLL.MMM........", ".....BBB.DDD........", "....BBBB.DDDD.......", "....BBBBB.DDDD......", "....BBBBB.DDDD......",
    ],
    parts: { O: [R.bone, 0.05, 1], H: [R.steel, 0, 2.5], b: [R.beard, 0.05, 2], F: [R.skin, 0.05, 2], T: [R.skin, 0, 3],
      A: [R.skin, -0.08, 1.5], h: [R.skin, 0, 1], U: [R.fur, 0, 2], R: [R.red, 0.1, 1], w: [R.wood, 0, 1], G: [R.steel, 0.05, 2],
      L: [R.skin, -0.12, 1.5], M: [R.skin, -0.02, 1.5], B: [R.fur, -0.15, 1.5], D: [R.fur, -0.05, 1.5] },
    fixed: { ...EYE, o: R.gold[3], E: R.steel[5] }, noSeam: "Ro GE",
    ground: "LMBD", head: "OHbFI",
    // axe: straight haft, crescent bit narrowing into a neck at the haft, bright honed edge, back spike
    after: (m) => {
      m = over(m, "w", vline(16, 1, 18));
      m = over(m, "G", [[15, 3], [17, 2], [17, 3], [17, 4], [18, 1], [18, 2], [18, 3], [18, 4], [18, 5]]);
      m = over(m, "E", [[19, 0], [19, 1], [19, 2], [19, 3], [19, 4], [19, 5], [19, 6]]);
      return over(m, "h", [[15, 11], [16, 11], [15, 12], [16, 12]]);
    },
  },

  martial_artist: {
    map: [
      "....................", "....................", ".....NNNN...........", "....NNNNNN..........",
      "..rrrrrrrrr.........", ".rr.NNFFFFF.........", "r...NFFFFIF.........", "....NFFFFFF.........",
      ".....FFFFF..........", "....GGGGGGGG........", "...GGGGgGGGGAA......", "..GGGGGGgGGGGAAAhh..",
      "..AA.GGGGgGG....hh..", "..AA..GGGGGG........", "..hh..KKKKKK........", "......LLLMMMM.......",
      ".....LLLL.MMMM......", "....LLLL...MMMM.....", "...LLLL.....MMM.....", "...LLL.......MMM....",
      "..LLL........MMM....", "..LLL.........MMM...", "..ff..........ff....", ".fff..........fff...", ".ffff.........ffff..",
    ],
    parts: { N: [R.hair, 0.1, 2], r: [R.crimson, 0.1, 1], F: [R.skin, 0.05, 2], G: [R.cloth, 0, 3], A: [R.cloth, -0.05, 1.5],
      h: [R.skin, 0.05, 1], K: [R.night, 0.05, 1], L: [R.cloth, -0.2, 1.5], M: [R.cloth, -0.05, 1.5], f: [R.skin, -0.1, 1] },
    fixed: { ...EYE, g: R.cloth[1] }, group: { G: "Gg" },
    ground: "LMf", head: "NrFI",
  },

  rogue: {
    map: [
      "....................", "......HHHH..........", ".....HHHHHH.........", "....HHHHHHHH........",
      "...HHHHHHHHH........", "...HHHHFFIFF........", "...HHHmmmmmm........", "....HHHmmmm.........",
      "...CCCCCCCCCC.......", "..CCCCTTTTTCCA......", "..CCCTTTTTTCAAA.....", "..CCCTTTTTTT..Ah....",
      "..CCCTTTTTTT........", "..CCCrrrrrrr........", "..CCCTTTTTTT........", "..CCCCLLL.MMM.......",
      "...CCCLLL.MMM.......", "....CCLLL.MMM.......", "......LLL.MMM.......", "......LLL.MMM.......",
      "......LLL.MMM.......", ".....BBBB.DDDD......", ".....BBBB.DDDD......", "....BBBBB.DDDDD.....", "....BBBBB.DDDDD.....",
    ],
    parts: { H: [R.brown, -0.05, 2.5], F: [R.skin, 0.05, 1.5], m: [R.night, 0.05, 1.5], C: [R.brown, -0.15, 2.5], T: [R.leather, 0.1, 2],
      A: [R.brown, 0, 1.5], h: [R.skin, 0.05, 1], r: [R.crimson, 0.05, 1], L: [R.night, -0.05, 1.5], M: [R.night, 0.08, 1.5],
      B: [R.leather, -0.1, 1.5], D: [R.leather, 0, 1.5] },
    fixed: { ...EYE, X: R.steel[4], x: R.steel[2] },
    ground: "LMBD", head: "HFIm",
    // dagger angled forward and up out of the fist, dark pommel below it
    after: (m) => over(over(m, "X", [[16, 10], [17, 9], [18, 8]]), "x", [[15, 12]]),
  },

  ranger: {
    map: [
      "....................", "......eeee..........", ".....eeeeee.........", "....eeeeeeee........",
      "....eeeFFFFe........", "....eeFFFIFF........", "....eeFFFFF.........", "...eeeeFFF..........",
      "..eeeeeTTTTee.......", "..eeeeTTTTTTAA......", "..eeeTTTTTTTTAA.....", "..eeeTTTTTTT.AAAA...",
      "..eeeTTTTTTT....hh..", "..eeeWWWWOWW........", "..eeeTTTTTTT........", "...eeTTTTTTT........",
      "....eLLL.MMM........", ".....LLL.MMM........", ".....LLL.MMM........", ".....LLL.MMM........",
      ".....LLL.MMM........", ".....BBB.DDD........", "....BBBB.DDDD.......", "....BBBBB.DDDD......", "....BBBBB.DDDD......",
    ],
    parts: { e: [R.moss, -0.05, 2.5], F: [R.skin, 0.05, 2], T: [R.brown, 0.1, 2.5], A: [R.brown, 0, 1.5], h: [R.leather, 0.1, 1],
      W: [R.leather, 0, 1], O: [R.gold, 0.25, 1], L: [R.brown, -0.15, 1.5], M: [R.brown, -0.02, 1.5], B: [R.leather, -0.1, 1.5],
      D: [R.leather, 0, 1.5], w: [R.wood, 0.1, 1] },
    fixed: { ...EYE, s: R.bone[1] }, noSeam: "WO",
    ground: "LMBD", head: "eFI",
    // longbow: pixel-perfect limbs bowed forward, straight string behind them, fist over the grip
    after: (m) => {
      const bow = [[14, 1], [15, 2], [15, 3], [16, 4], [16, 5], ...vline(17, 6, 17), [16, 18], [16, 19], [15, 20], [15, 21], [14, 22]];
      m = over(m, "s", vline(14, 2, 21), true);
      return over(m, "w", bow.filter(([, y]) => y !== 12));
    },
  },

  cleric: {
    map: [
      "....................", "....................", "......cccc..........", ".....cccccc.........",
      "....cccccccc........", "....ccccFFFF........", "....cccFFIFF........", "....cccFFFFc........",
      "...ccccTTTTcc.......", "..cTTTTTTTTTTAA.....", "..TTTTTTyTTTTTAAhh..", "..TTTTTyyyTTT...hh..",
      "..ATTTTTyTTTT.......", "..ATTTTTyTTTT.......", "..hTWWWWWWWWT.......", "...QQQQQyQQQQQ......",
      "...QQQQQyQQQQQ......", "...QQQQQyQQQQQ......", "..QQQQQQyQQQQQQ.....", "..QQQQQQyQQQQQQ.....",
      "..QQQQQQyQQQQQQ.....", ".QQQQQQQyQQQQQQQ....", ".QQQQQQQyQQQQQQQ....", ".QQQQQQQyQQQQQQQ....", ".yyyyyyyyyyyyyyy....",
    ],
    parts: { c: [R.cloth, 0, 2.5], F: [R.skin, 0.05, 2], T: [R.cloth, 0.05, 3], A: [R.cloth, -0.05, 1.5], h: [R.skin, 0, 1],
      Q: [R.cloth, -0.05, 3], W: [R.gold, -0.1, 1], w: [R.wood, 0.05, 1] },
    fixed: { ...EYE, y: R.gold[3], Y: R.gold[4] }, group: { Q: "Qy", T: "Ty" },
    groundFrom: 15, head: "cFI",
    // staff with a gold cross head
    after: (m) => over(over(m, "w", vline(17, 3, 24), true), "Y", [[17, 0], [16, 1], [17, 1], [18, 1], [17, 2]]),
  },

  wizard: {
    map: [
      "....pp..............", ".....ppp............", "......pppp..........", ".....ppppp..........",
      ".....pppppp.........", "....pyyyyyyp........", "..pppppppppppp......", ".....NFFFIFF........",
      ".....NFFFFFF........", "......FFFF..........", "....TTTTTTTTAA......", "...TTTTyTTTTTAAhh...",
      "..TTTTTyTTTTT..hh...", "..ATTTTyTTTTT.......", "..ATTTTyTTTTT.......", "..hTWWWWWWWWT.......",
      "...QQQQyQQQQQ.......", "...QQQQyQQQQQ.......", "..QQQQQyQQQQQQ......", "..QQQQQyQQQQQQ......",
      "..QQQQQyQQQQQQ......", ".QQQQQQyQQQQQQQ.....", ".QQQQQQyQQQQQQQ.....", ".QQQQQQyQQQQQQQ.....", ".yyyyyyyyyyyyyy.....",
    ],
    parts: { p: [R.purple, 0.05, 2], N: [R.hair, 0.05, 1], F: [R.skin, -0.05, 2], T: [R.purple, 0.05, 3], A: [R.purple, -0.05, 1.5],
      h: [R.skin, 0, 1], Q: [R.purple, -0.05, 3], W: [R.gold, -0.1, 1], w: [R.wood, 0.05, 1] },
    fixed: { ...EYE, y: R.gold[3], o: R.orange[2], O: R.orange[3] }, group: { Q: "Qy", T: "Ty" },
    groundFrom: 16, head: "pyNFI",
    // staff topped with an ember orb, lit on its top-left pixels
    after: (m) => {
      m = over(m, "w", vline(16, 4, 24), true);
      m = over(m, "o", [[16, 0], [15, 1], [16, 1], [17, 1], [15, 2], [16, 2], [17, 2], [16, 3]]);
      return over(m, "O", [[15, 1], [16, 0]]);
    },
  },

  warlock: {
    map: [
      "....................", ".......HH...........", "......HHHH..........", ".....HHHHHH.........",
      "....HHHHHHHH........", "....HHHkkkkk........", "....HHkkckck........", "....HHkkkkkk........",
      ".....HHkkkk.........", "...HHTTTTTTHH.......", "..HTTTTpTTTTTAA.....", "..TTTTTpTTTTTTAAh...",
      "..ATTTpPpTTTT.......", "..ATTTTpTTTTT.......", "..hTTTTpTTTTT.......", "...QQQQpQQQQQ.......",
      "...QQQQpQQQQQ.......", "...QQQQpQQQQQQ......", "..QQQQQpQQQQQQ......", "..QQQQQpQQQQQQQ.....",
      "..QQQQQpQQQQQQQ.....", ".QQQQQQpQQQQQQQ.....", ".QQQQQQpQQQQQQQQ....", ".QQQQQQpQQQQQQQQ....", ".pppppppppppppppp...",
    ],
    parts: { H: [R.night, 0.1, 2.5], T: [R.night, 0.1, 3], A: [R.night, 0.05, 1.5], h: [R.skin, -0.1, 1], Q: [R.night, 0.05, 3] },
    fixed: { k: VOID, c: R.cyan[3], p: R.arcane[1], P: R.arcane[3], f: R.cyan[1], F: R.cyan[2], g: R.cyan[3] },
    group: { Q: "Qp", T: "TpP" },
    groundFrom: 15, head: "Hkc",
    // abyssal flame floating over the open hand: dark rim, mid body, bright core near the hand
    after: (m) => {
      m = over(m, "f", [[16, 10], [17, 10], [15, 9], [16, 9], [17, 9], [18, 9], [16, 8], [17, 8], [16, 7], [17, 6]]);
      m = over(m, "F", [[16, 9], [17, 9], [17, 8], [16, 10]]);
      return over(m, "g", [[17, 9]]);
    },
  },

  druid: {
    map: [
      "..n.n.....n.n.......", "...nn......nn.......", "....nvvvvvvn........", "....vNNNNNNv........",
      "....NNNNNNNN........", "....NNNFFFFF........", "....NNFFFIFF........", "....NNFFFFF.........",
      ".....NNFFF..........", "...eeeTTTTTee.......", "..eeTTTTTTTTTAA.....", "..eTTTTTvTTTTTAA....",
      "..ATTTTTvTTTT.......", "..ATTTTvvTTTT.......", "..hTWWWWWWWWT.......", "...QQQQQvQQQQ.......",
      "...QQQQQvvQQQ.......", "...QQQQQvQQQQQ......", "..QQQQvvvQQQQQ......", "..QQQQQQvQQQQQQ.....",
      "..QQQQQQvvQQQQQ.....", ".QQQQQQQvQQQQQQ.....", ".QQQQQvvvQQQQQQQ....", ".QQQQQQQvQQQQQQQ....", ".WWWWWWWWWWWWWWW....",
    ],
    parts: { n: [R.wood, 0.1, 1], N: [R.hair, 0.15, 2], F: [R.skin, 0.05, 2], e: [R.leaf, 0, 1.5], T: [R.moss, 0.05, 3],
      A: [R.moss, -0.05, 1.5], h: [R.skin, 0, 1], Q: [R.moss, -0.05, 3], W: [R.leather, 0, 1], w: [R.wood, 0, 1], l: [R.leaf, 0.1, 1.5] },
    fixed: { ...EYE, v: R.leaf[2] }, group: { Q: "Qv", T: "Tv" },
    groundFrom: 15, head: "nvNFI",
    // gnarled staff forking at the top, leaves on the fork, fist on the haft
    after: (m) => {
      m = over(m, "w", [...vline(17, 4, 23), [16, 3], [18, 3], [16, 2], [19, 2]], true);
      m = over(m, "l", [[15, 1], [16, 1], [15, 0], [18, 1], [19, 1], [19, 0], [17, 2]]);
      return over(m, "h", [[16, 11], [17, 11], [16, 12]]);
    },
  },
};

for (const def of Object.values(HEROES)) { def.map = norm(def.map); if (def.after) def.map = def.after(def.map); }
module.exports = { HEROES };

// node heroes.js --export : one PNG per idle frame into Assets/Resources/Heroes/<id>_<i>.png (read by HeroArt.cs).
// Import settings are copied from the FX sheets (point filter, no mips, uncompressed); an existing .meta keeps its guid.
if (require.main === module && process.argv[2] === "--export") {
  const fs = require("fs"), path = require("path"), crypto = require("crypto");
  const assets = path.resolve(__dirname, "../../Assets"), dir = path.join(assets, "Resources/Heroes");
  fs.mkdirSync(dir, { recursive: true });
  const meta = fs.readFileSync(path.join(assets, "Resources/Fx/hit.png.meta"), "utf8");
  for (const [id, def] of Object.entries(HEROES)) {
    frames(def).forEach((f, i) => {
      const file = path.join(dir, `${id}_${i}.png`);
      P.save(f, file);
      if (!fs.existsSync(file + ".meta")) fs.writeFileSync(file + ".meta", meta.replace(/^guid: \w+/m, "guid: " + crypto.randomBytes(16).toString("hex")));
    });
    console.log("exported", id, "4 frames 22x27");
  }
  process.exit(0);
}

if (require.main === module) {
  const out = process.argv[2], fs = require("fs"), only = process.argv[3];
  const all = {};
  for (const [id, def] of Object.entries(HEROES)) {
    if (only && !only.split(",").includes(id)) continue;
    const f = frames(def); all[id] = f;
    const e = P.edgeCheck(f[0]), cc = P.colorCount(f[0]);
    console.log(id.padEnd(15), "nubs", e.nubs.length, "strips", e.strips.length, "holes", JSON.stringify(e.holes), "colors", cc.length, "top", (cc[0].share * 100).toFixed(0) + "%");
    fs.writeFileSync(`${out}/${id}_idle.gif`, P.gif(f, { delay: DELAY, scale: 6 }));
  }
  const ids = Object.keys(all);
  P.save(P.sheet([
    ...ids.map((id) => ({ buf: all[id][0], label: id.replace("_", " ").toUpperCase(), scale: 5 })),
    ...ids.map((id) => ({ buf: all[id][0], label: "1X", scale: 1 })),
  ], { cols: Math.min(5, ids.length) }), out + "/heroes_lineup.png");
  P.save(P.sheet(ids.flatMap((id) => all[id].map((f, i) => ({ buf: f, label: id.slice(0, 4).toUpperCase() + i, scale: 3 }))), { cols: 8 }), out + "/heroes_frames.png");
}
