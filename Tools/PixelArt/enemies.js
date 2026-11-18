// Dungeon 1 enemy part maps (v2, dark fantasy pass). Drawn facing right, mirrored (flip) to face left in battle.
// Normal/elite inner 22x24 -> 24x26 (exact 4x in the 96x104 box); boss inner 28x30 -> 30x32 (4x in 120x128).
// Menace comes from silhouette and face, not detail: dirty dark ramps, pinprick eyes, skulls and open jaws,
// tatters, jagged weapons, dried blood. Run: node enemies.js <outDir> [id,id]   |   node enemies.js --export
const { P, R, VOID, frames, DELAY, norm } = require("./engine.js");

const NW = 22, NH = 24, BW = 28, BH = 30;
const over = (rows, k, pts, onlyEmpty = false) => {
  const m = rows.map((r) => [...r]);
  for (const [x, y] of pts) if (m[y] && m[y][x] !== undefined && (!onlyEmpty || m[y][x] === ".")) m[y][x] = k;
  return m.map((r) => r.join(""));
};
const vline = (x, y0, y1) => Array.from({ length: y1 - y0 + 1 }, (_, i) => [x, y0 + i]);
// pixel-perfect 1px line (Bresenham steps corner to corner)
const line = (x0, y0, x1, y1) => {
  const pts = [], dx = Math.abs(x1 - x0), dy = -Math.abs(y1 - y0), sx = x0 < x1 ? 1 : -1, sy = y0 < y1 ? 1 : -1;
  let err = dx + dy;
  for (;;) {
    pts.push([x0, y0]);
    if (x0 === x1 && y0 === y1) return pts;
    const e2 = 2 * err;
    if (e2 >= dy) { err += dy; x0 += sx; }
    if (e2 <= dx) { err += dx; y0 += sy; }
  }
};

const E = {
  bone: P.hueRamp(5, { hue: 42, sat: 0.2, shift: 40, light: [0.14, 0.74] }),   // dirty, yellowed bone
  iron: P.hueRamp(5, { hue: 215, sat: 0.12, shift: 30, light: [0.1, 0.58] }),   // dark tarnished steel
  rust: P.hueRamp(4, { hue: 18, sat: 0.42, shift: 20, light: [0.12, 0.42] }),
  rag: P.hueRamp(4, { hue: 30, sat: 0.15, shift: 25, light: [0.08, 0.32] }),
  grave: P.hueRamp(4, { hue: 200, sat: 0.1, shift: 25, light: [0.07, 0.32] }),
  fire: P.hueRamp(4, { hue: 22, sat: 0.9, shift: 30, light: [0.3, 0.82] }),
  coal: P.hueRamp(4, { hue: 15, sat: 0.18, shift: 20, light: [0.08, 0.36] }),
  hound: P.hueRamp(5, { hue: 228, sat: 0.2, shift: 25, light: [0.08, 0.42] }),
  royal: P.hueRamp(4, { hue: 228, sat: 0.32, shift: 25, light: [0.1, 0.36] }),
  flesh: P.hueRamp(5, { hue: 330, sat: 0.12, shift: 35, light: [0.13, 0.58] }), // sickly grey-pink
  rot: P.hueRamp(4, { hue: 85, sat: 0.16, shift: 30, light: [0.14, 0.48] }),
  quilt: P.hueRamp(4, { hue: 35, sat: 0.2, shift: 25, light: [0.12, 0.42] }),
  wraith: P.hueRamp(5, { hue: 262, sat: 0.22, shift: 25, light: [0.05, 0.3] }),
  abyss: P.hueRamp(5, { hue: 250, sat: 0.28, shift: 25, light: [0.04, 0.28] }),
  horn: P.hueRamp(4, { hue: 30, sat: 0.12, shift: 30, light: [0.1, 0.45] }),
};
const SOUL = R.cyan;
const BLOOD = P.hueRamp(4, { hue: 355, sat: 0.75, shift: 10, light: [0.16, 0.5] }); // dried -> fresh
const TOOTH = E.bone[4], EYE = SOUL[3], GORE = BLOOD[1], WOOD = R.wood;

const ENEMIES = {
  // hunched skeleton: cracked helm, gaping jaw, one cold eye, broken buckler, notched bloody blade, grave rags
  hollow_squire: {
    map: [
      "......................", "......hhhhh...........", ".....hhhkhhhh.........", ".....hSSSSSSSS........",
      ".....hSkkSSkkS........", "......SkkSSkeS........", "......SSSSkSSS........", "......SkSkSkSS........",
      ".......kkkkkkk........", "...oo..SkSkSS.........", "..ooooBBBBBBBbb.......", ".oOOOoBvvvvBB.bb......",
      ".oOROoBBBBBBB..bb.....", ".oOO.oBvvvvBB...cc....", "..oo..BBBBBB....cc....", "......rjjjjr..........",
      ".....rrPPPPPr.........", ".....r.LL..LLr........", ".....r.LL..LL.........", "......LL....LL........",
      "......LL....LL........", "......LL....LL........", ".....FFF....FFF.......", ".....FFFF...FFFF......",
    ],
    parts: { h: [E.iron, -0.05, 2.5], S: [E.bone, 0.1, 2.5], B: [E.bone, 0, 2], b: [E.bone, -0.05, 1], c: [E.bone, 0.05, 1],
      j: [E.bone, -0.15, 1], P: [E.bone, -0.05, 1.5], L: [E.bone, -0.1, 1], F: [E.bone, -0.2, 1.5], r: [E.rag, 0, 1],
      o: [E.rust, -0.1, 1], O: [E.rust, 0.1, 2], R: [E.rust, 0.35, 1] },
    // v: gaps between the ribs; own letter because "k" (sockets, maw) lags with the head and must not drag them along
    fixed: { k: VOID, v: VOID, e: EYE, X: E.iron[2], Q: GORE }, group: { O: "OR" }, noSeam: "OR",
    ground: "LF", head: "hSke",
    after: (m) => over(over(m, "X", [[18, 15], [19, 15], [19, 16], [20, 16], [20, 17], [21, 18]]), "Q", [[20, 17], [21, 18], [19, 16]]),
  },

  // gaunt hooded archer, skull lost in the hood's shadow, bow drawn with an arrow aimed at the party
  gravebound_archer: {
    map: [
      "......................", "...HHHH...............", "..HHHHHHH.............", ".HHHHHHHHH............",
      ".HHHHHkkkkH...........", ".HHHHkkSSSk...........", ".HHHHkkSkeS...........", ".HHHHkkSSSS...........",
      ".HHHHHkSkSk...........", ".HHHHHHkkk............", ".HHHHBBBBBBBbbbbb.....", ".HHHBvvvvBBB..........",
      ".HHHBBBBBBBB..........", ".HHHBvvvvBB...........", ".HH.H.BBBBB...........", ".HH.H..jjjj...........",
      ".H..H..PPPP...........", ".H....PP..PP..........", "......LL..LL..........", "......LL..LL..........",
      "......LL...LL.........", "......LL...LL.........", ".....FFF...FFF........", ".....FFFF..FFFF.......",
    ],
    parts: { H: [E.grave, 0, 2.5], S: [E.bone, 0.05, 1.5], B: [E.bone, -0.05, 2], b: [E.bone, -0.05, 1], j: [E.bone, -0.15, 1],
      P: [E.bone, -0.05, 1.5], L: [E.bone, -0.1, 1], F: [E.bone, -0.2, 1.5], w: [WOOD, -0.05, 1], c: [E.bone, 0.1, 1] },
    fixed: { k: VOID, v: VOID, e: EYE, s: E.bone[1], a: WOOD[2], X: E.iron[3] },
    ground: "LF", head: "HSke",
    after: (m) => {
      const bow = [[15, 1], [16, 2], [16, 3], [17, 4], [17, 5], ...vline(18, 6, 14), [17, 15], [17, 16], [16, 17], [16, 18], [15, 19]];
      m = over(m, "s", [...line(15, 1, 10, 9), ...line(10, 9, 15, 19)], true); // string pulled back to the chest
      m = over(m, "w", bow);
      m = over(m, "a", line(10, 9, 20, 9));
      m = over(m, "X", [[21, 9]]);
      return over(m, "c", [[17, 10], [18, 10], [10, 10]]);
    },
  },

  // a charred skull screaming inside its own fire: burning eye sockets, open jaw, flames tearing off the crown
  cinder_wisp: {
    map: [
      ".......r........r.....", "......rr...r...rr.....", "......ro..rr...ro..r..", "..r..roo.roo..roo.rr..",
      "..rr.rooorooo.rooorro.", "..ro.rooyoooyoooyoorr.", "...rrooyyyoyyyyyoooor.", "...roooyyyyyyyyyyyoor.",
      "..rrooyycccccccyyyoor.", "..rooyccccccccccyyor..", "..rooccccccccccccyor..", "..roccWWcccccWWcccor..",
      "..roccWycccccyWcccor..", "..rroccccckkcccccoor..", "...rocccccccccccoor...", "...rrocctktktktcor....",
      "....rrokkkkkkkkor.....", ".....roctktktkcor.....", "......rrcccccorr......", ".......rrrrrrrr.......",
      "........r...r.........", "......................", "......................", "......................",
    ],
    parts: { c: [E.coal, 0.1, 3] },
    fixed: { r: E.fire[0], o: E.fire[1], y: E.fire[2], W: E.fire[3], k: VOID, t: TOOTH },
    lit: E.fire[0], headTo: 8,
  },

  // emaciated hellhound: spine spikes, rib cage showing, head low, jaws gaping with fangs, a broken soul chain
  soulbound_hound: {
    map: [
      "......................", "......................", "......................", "......................",
      "......................", "......................", "........s.s.s.........", ".......sBsBsBs........",
      "..t...BBBBBBBBBB......", "..tt.BBBBBBBBBBBBE.E..", "...tBBBqBqBqBBBBHEEE..", "....BBBqBqBqBBBHHHHH..",
      "....BBBqBqBqBBcHHHeHH.", "....BBBBBBBBBBcHHHHHMM", ".....BBBBBBBBBcHHwkwkw", ".....LLLB...NNcHkkkkk.",
      ".....LLL....NN.Hwkwkw.", "....LLL.nn..NN.cMMMM..", "....LL..nn..NN.c.mm...", "...LL...nn..NN...mm...",
      "...LL...nn..NN...mm...", "..LL....nn..NN...mm...", ".PPP...ppp.PPP..ppp...", ".PPP...ppp.PPP..ppp...",
    ],
    parts: { E: [E.hound, 0.1, 1], H: [E.hound, 0.15, 2.5], M: [E.hound, 0.05, 1.5], t: [E.hound, 0, 1], B: [E.hound, 0.1, 3],
      L: [E.hound, -0.05, 1.5], N: [E.hound, 0.05, 1], n: [E.hound, -0.3, 1], m: [E.hound, -0.25, 1],
      P: [E.hound, -0.05, 1], p: [E.hound, -0.3, 1], s: [E.bone, 0, 1] },
    fixed: { e: EYE, k: VOID, w: TOOTH, q: E.hound[0], c: SOUL[0] },
    ground: "LNnmPp", head: "EHMekw",
  },

  // corpse in tarnished plate: dented helm, a cold glow and old blood running from the cracked visor, torn tabard
  possessed_royal_guard: {
    map: [
      ".....GGGGG............", "....GGGGGGG...........", "....GGGkGGG...........", "....GGVVeVV...........",
      "....GGGGQGG...........", "....GGGGQGG...........", ".....GGGQG............", "...PPPTTQTPPP.........",
      "..PPPPTTTQTPPP........", "..AAATTyTQTTAAA.......", "..AAATyyyTTTAA........", "..AAATTyTTTT..........",
      "..gg.TTTTTTT..........", "..gg.WWWOWWW..........", ".....TTQTTTT..........", ".....T.TT.TT..........",
      ".....LLL.MMM..........", ".....LLL.MMM..........", ".....LLL.MMM..........", ".....LLL.MMM..........",
      ".....LLL.MMM..........", "....BBBB.CCCC.........", "....BBBB.CCCCC........", "....BBBBB.CCCCC.......",
    ],
    parts: { G: [E.iron, 0.05, 3], P: [E.iron, 0, 2], T: [E.royal, 0.05, 3], A: [E.iron, -0.1, 1.5], g: [E.iron, -0.1, 1.5],
      W: [R.leather, -0.1, 1], O: [R.gold, 0, 1], L: [E.iron, -0.18, 1.5], M: [E.iron, -0.05, 1.5], B: [E.iron, -0.3, 1.5],
      C: [E.iron, -0.2, 1.5], h: [E.iron, 0, 1], w: [WOOD, -0.1, 1], X: [E.iron, 0.1, 1.5] },
    fixed: { V: VOID, k: VOID, e: EYE, y: R.gold[1], Z: E.iron[4], Q: BLOOD[0] }, group: { T: "TyQ", G: "GQk" }, noSeam: "WO XZ",
    ground: "LMBC", head: "GVekQ",
    // halberd with a notched axe blade, honed edge, back hook
    after: (m) => {
      m = over(m, "w", vline(15, 2, 23), true);
      m = over(m, "Z", [[15, 0], [15, 1], [19, 3], [19, 5]]);
      m = over(m, "X", [[16, 2], [17, 2], [16, 3], [17, 3], [18, 3], [16, 4], [17, 4], [16, 5], [17, 5], [18, 5], [17, 6], [14, 3], [13, 2]]);
      return over(m, "h", [[14, 10], [15, 10], [14, 11], [15, 11]]);
    },
  },

  // rearing lamprey-leech: split jaws ringed with teeth, a dark red throat, three cold eyes along the head
  crypt_leech: {
    map: [
      "......................", "......................", "............SSSS......", "...........SSSSSSmm...",
      "..........SSeSeSmtttt.", "..........SSSSSSmKKKKk", ".........SSSSSSmKKKKKk", ".........SSSSSSmKKKKk.",
      ".........SSSSSSmtttt..", ".........SSSSSSSmmm...", ".........SSSSSSSS.....", "........SSSSSSSS......",
      "........SSSSSSS.......", ".......SSSSSSS........", "......SSSSSSS.........", ".....SSSSSSSS.........",
      "....SSSSSSSSSS........", "...SSSSSSSSSSSS.......", "..SSSSSSSSSSSSSS......", "..SSSSSSS..SSSSSS.....",
      ".SSSSSSS....SSSSSS....", ".SSSSSSSSSSSSSSSSSS...", ".SSSSSSSSSSSSSSSSSSS..", "..SSSSSSSSSSSSSSSSSS..",
    ],
    parts: { S: [E.flesh, 0.05, 3], g: [E.flesh, -0.3, 3], U: [E.flesh, 0.3, 1], m: [BLOOD, 0.1, 1] },
    fixed: { k: VOID, t: TOOTH, K: BLOOD[0], e: EYE }, group: { S: "SgUe", g: "SgUe", U: "SgUe" }, noSeam: "Sg SU gU",
    groundFrom: 18, headTo: 11,
    after: (m) => m.map((r, y) => [...r].map((c, x) => {
      if (c !== "S") return c;
      if (y >= 9 && y <= 16 && r[x + 1] !== "S") return "U";
      if (y % 3 === 0 && y >= 9 && r[x - 1] === "S" && r[x + 1] === "S") return "g";
      return c;
    }).join("")),
  },

  // rotting conscript: half the face gone to the jawbone, one dead socket, ribs through a torn coat, bloody spear
  forgotten_militia: {
    map: [
      "......................", "......hhhhh...........", ".....hhhhhhh..........", "...hhhhhhhkhhh........",
      ".....ZZZZZZZ..........", ".....ZZkZZeZ..........", ".....ZZZZSSS..........", ".....ZZkSkSk..........",
      "......ZZSSS...........", "....GGGGGGGG..........", "...GGGGvGGGGAA........", "..GGGGbvbGgGGA........",
      "..AAGGvbvGGGG.........", "..AAGGGGGgGGG.........", "..ZZ.WWWWWWW..........", "..Z..GGGQGGG..........",
      ".....GG.GGGG..........", ".....LLL.MMM..........", ".....LLL.MMM..........", ".....LLL.MMM..........",
      ".....LLL.MMM..........", ".....LLL.MMM..........", "....BBBB.CCCC.........", "....BBBB.CCCCC........",
    ],
    parts: { h: [E.iron, -0.12, 2.5], Z: [E.rot, 0.05, 2], S: [E.bone, 0.05, 1], G: [E.quilt, 0.05, 3], A: [E.quilt, -0.05, 1.5],
      W: [R.leather, -0.1, 1], L: [E.rag, 0, 1.5], M: [E.rag, 0.1, 1.5], B: [R.leather, -0.15, 1.5], C: [R.leather, -0.05, 1.5],
      w: [WOOD, -0.1, 1], z: [E.rot, 0, 1], b: [E.bone, 0.05, 1] },
    // v / b: the coat's torn holes and the ribs behind them; own letters so they stay with the body, not the head
    fixed: { k: VOID, v: VOID, e: EYE, g: E.quilt[0], X: E.iron[3], x: E.iron[4], Q: BLOOD[0] }, group: { G: "Gg" },
    ground: "LMBC", head: "hZkeS",
    after: (m) => {
      m = over(m, "w", vline(16, 3, 23), true);
      m = over(m, "X", [[16, 0], [15, 1], [16, 1], [17, 1], [16, 2]]);
      m = over(m, "x", [[16, 0]]);
      m = over(m, "Q", [[16, 2], [16, 3]]);
      return over(m, "z", [[14, 10], [15, 10], [16, 10], [15, 11], [16, 11]]);
    },
  },

  // tall wraith: a skull grinning inside the hood, robe torn into long tendrils, a great notched scythe
  soul_reaper: {
    map: [
      "......................", ".......HHH............", "......HHHHH...........", ".....HHHHHHH..........",
      "....HHHkkkkkH.........", "....HHkkSSSkk.........", "...HHHkSSSSSk.........", "...HHHkSkSeSk.........",
      "...HHHkSSkSSk.........", "..HHHHkSkSkSk.........", "..HCCCCCCCCCaaa.......", ".HCCCCCCCCCCC.a.......",
      ".CCCCCCCCCCCC.........", ".CCCCCCCCCCCCaaa......", "CCCCCCCCCCCCC.........", "CCCCCCCCCCCCC.........",
      ".CCCCCCCCCCCC.........", ".CCCC.CCCCCCC.........", "..CCC..CCC.CC.........", "..CC...CC...C.........",
      "...C....C...C.........", "...C....C.............", "........C.............", "......................",
    ],
    parts: { H: [E.wraith, 0.15, 2.5], C: [E.wraith, 0.05, 3], a: [E.wraith, 0, 1], S: [E.bone, 0, 1.5], w: [WOOD, -0.15, 1],
      h: [E.bone, 0.05, 1], s: [E.iron, 0, 1.5] },
    fixed: { k: VOID, e: EYE, Z: E.iron[4], Q: BLOOD[0] },
    head: "HkSe",
    after: (m) => {
      m = over(m, "w", vline(16, 1, 22), true);
      m = over(m, "s", [[15, 0], [16, 0], [17, 0], [18, 0], [19, 0], [20, 1], [21, 2], [21, 3], [21, 4], [21, 5]]);
      m = over(m, "Z", [[17, 1], [18, 1], [19, 1], [20, 2], [20, 4], [21, 6]]);
      m = over(m, "Q", [[20, 3]]);
      return over(m, "h", [[15, 11], [16, 11], [15, 14], [16, 14]]);
    },
  },

  // crowned skull on a bone knight: jagged crown, spiked pauldrons, rib cage under the tabard,
  // skull-carved tower shield with blood on it, spiked bone mace
  royal_ossuary_knight: {
    map: [
      "....y..y..y...........", "....yy.yy.yy..........", "....yyyyyyyy..........", "....SSSSSSSS..........",
      "....SkkSSkkS..........", "....SkkSSkeS..........", "....SSSSkSSS..........", "....SkSkSkSS..........",
      ".....SSSSSS...........", "..sPPPTTTTPPs.........", "..PPPPTqTqTPP.........", "..AAATqTqTTT..........",
      "..AAATTTTTTT..........", "..AAAWWWOWWW..........", "..hh.TTTTTTT..........", "...w.TT.T.TT..........",
      "...w.LLL.MMM..........", ".mmmm.LL.MMM..........", "mmmmmmLL.MMM..........", ".mmmm.LL.MMM..........",
      "..m..LLL.MMM..........", "....BBBB.CCCC.........", "....BBBB.CCCCC........", "....BBBBB.CCCCC.......",
    ],
    parts: { S: [E.bone, 0.1, 2.5], P: [E.bone, 0, 2], T: [E.wraith, 0.15, 3], A: [E.bone, -0.1, 1.5], h: [E.bone, 0, 1],
      W: [R.leather, -0.1, 1], O: [R.gold, 0, 1], L: [E.bone, -0.15, 1.5], M: [E.bone, -0.02, 1.5], B: [E.bone, -0.3, 1.5],
      C: [E.bone, -0.2, 1.5], w: [WOOD, -0.1, 1], m: [E.bone, 0, 1.5], s: [E.bone, 0.1, 1], D: [E.bone, 0.05, 4], r: [E.rust, 0, 1] },
    fixed: { k: VOID, e: EYE, y: R.gold[1], q: E.wraith[0], K: E.bone[0], Q: BLOOD[0] }, group: { D: "DKQ" }, noSeam: "DK DQ Dr KQ WO",
    ground: "LMBC", head: "ySke",
    // tower shield: bone face in a rusted iron frame with two spikes on top, carved skull, old blood
    after: (m) => {
      const mm = m.map((r) => [...r]);
      for (let y = 9; y <= 22; y++) for (let x = 12; x <= 19; x++) {
        if ((y === 9 || y === 22) && (x === 12 || x === 19)) continue;
        mm[y][x] = x === 12 || x === 19 || y === 9 || y === 22 ? "r" : "D";
      }
      for (const [x, y] of [[13, 12], [14, 12], [13, 13], [14, 13], [17, 12], [18, 12], [17, 13], [18, 13], [15, 15], [16, 15],
        [14, 17], [15, 17], [16, 17], [17, 17], [15, 18], [17, 18]]) mm[y][x] = "K";
      for (const [x, y] of [[16, 19], [17, 19], [17, 20], [16, 13]]) mm[y][x] = "Q";
      mm[8][13] = "r"; mm[8][18] = "r";
      mm[17][0] = "."; mm[19][0] = "."; mm[16][2] = "m"; mm[20][3] = "."; // spikes on the mace head
      return mm.map((r) => r.join(""));
    },
  },
};

// Zoroa, Harvester of Souls: a cluster of eyes over a fanged maw in the hood's dark, ram horns, a clawed hand
// dangling the soul lantern, the cloak torn into long tendrils. P2: the lantern bursts, eyes and maw burn crimson,
// soul flames on the horn tips.
const ZOROA = [
  "............................", "....N...............N.......", "...NN...............NN......", "...NNN.............NNN......",
  "....NNN...........NNN.......", "......NNNHHHHHHHNNN.........", "......HHHHHHHHHHHHH.........", ".....HHHHHHHHHHHHHHH........",
  ".....HHHHkkkkkkkHHHH........", "....HHHHkkkkkkkkkHHHH.......", "....HHHkkkkkkkkkkkHHH.......", "....HHHkkkkkkkkkkkHHH.......",
  "....HHHkkkkkkkkkkkHHH.......", "....HHHHkkkkkkkkkHHHH.......", "...CCHHHHkkkkkkkHHHHCC......", "..CCCCCHHHHHHHHHHHCCCCAA....",
  ".CCCCCCCCCCCCCCCCCCCCAAAA...", ".CCCCCCCCCCCCCCCCCCCC..AAhh.", "CCCCCCCCCCCCCCCCCCCCC...hhhh", "CCCCCCCCCCCCCCCCCCCCC...hc.h",
  "CCCCCCCCCCCCCCCCCCCCC....c..", ".CCCCCCCCCCCCCCCCCCC....LLL.", ".CCCCCCCCCCCCCCCCCCC...LgggL", "..CCCCCCCCCCCCCCCCC....LgGgL",
  "..CCCCCCCCCCCCCCCCC....LgggL", "...CCCC.CCCCCC.CCC......LLL.", "...CCC..CCCCC...CC..........", "....CC...CCC....C...........",
  "....C.....CC....C...........", "..........C.................",
];
const zoroa = (p2) => ({
  w: BW, h: BH, map: ZOROA,
  parts: { N: [E.horn, 0.1, 1.5], H: [E.abyss, 0.2, 3], C: [E.abyss, 0.1, 4], q: [E.abyss, -0.25, 4], A: [E.abyss, 0.05, 1.5],
    h: [E.bone, -0.05, 1], L: [E.iron, -0.15, 1] },
  fixed: { k: VOID, e: p2 ? BLOOD[3] : EYE, t: E.bone[2], K: p2 ? BLOOD[2] : BLOOD[0], c: E.iron[2],
    g: p2 ? BLOOD[1] : SOUL[1], G: p2 ? BLOOD[3] : SOUL[3], f: SOUL[1], F: SOUL[3] },
  group: { C: "Cq", q: "Cq" }, noSeam: "Cq",
  head: "NHketKfF",
  after: (m) => {
    m = over(m, "q", [...vline(6, 17, 25), ...vline(11, 16, 27), ...vline(16, 17, 25)].filter(([x, y]) => m[y][x] === "C"));
    // a scatter of pinprick eyes; below, long canines hang down and short fangs rise around a dark red throat
    m = over(m, "e", [[10, 9], [14, 9], [12, 8], [8, 10], [16, 10]]);
    m = over(m, "K", [[11, 12], [12, 12], [13, 12], [12, 13]]);
    m = over(m, "t", [[10, 11], [12, 11], [14, 11], [10, 12], [14, 12], [11, 13], [13, 13], [11, 14], [13, 14]]);
    if (p2) m = over(over(m, "f", [[4, 0], [3, 0], [5, 0], [20, 0], [19, 0], [21, 0]], true), "F", [[4, 0], [20, 0]]);
    return m;
  },
});
// boss scale (48x60): see zoroa.js. The small `zoroa` above is kept only as the old design reference.
const bigZoroa = require("./zoroa.js");
ENEMIES.zoroa_p1 = bigZoroa(P, R, E, SOUL, BLOOD, VOID, false);
ENEMIES.zoroa_p2 = bigZoroa(P, R, E, SOUL, BLOOD, VOID, true);

for (const def of Object.values(ENEMIES)) {
  def.w = def.w || NW; def.h = def.h || NH; def.flip = true; def.lit = def.lit ?? E.iron[1];
  def.map = norm(def.map, def.w, def.h);
  if (def.after) def.map = def.after(def.map);
}
module.exports = { ENEMIES };

// node enemies.js --export : one PNG per idle frame into Assets/Resources/Enemies/<id>_<i>.png (read by UnitArt.cs).
// Import settings are copied from the FX sheets (point filter, no mips, uncompressed); an existing .meta keeps its guid.
if (require.main === module && process.argv[2] === "--export") {
  const fs = require("fs"), path = require("path"), crypto = require("crypto");
  const assets = path.resolve(__dirname, "../../Assets"), dir = path.join(assets, "Resources/Enemies");
  fs.mkdirSync(dir, { recursive: true });
  const meta = fs.readFileSync(path.join(assets, "Resources/Fx/hit.png.meta"), "utf8");
  for (const [id, def] of Object.entries(ENEMIES)) {
    frames(def).forEach((f, i) => {
      const file = path.join(dir, `${id}_${i}.png`);
      P.save(f, file);
      if (!fs.existsSync(file + ".meta")) fs.writeFileSync(file + ".meta", meta.replace(/^guid: \w+/m, "guid: " + crypto.randomBytes(16).toString("hex")));
    });
    console.log("exported", id, `4 frames ${def.w + 2}x${def.h + 2}`);
  }
  process.exit(0);
}

if (require.main === module) {
  const out = process.argv[2], fs = require("fs"), path = require("path"), only = process.argv[3];
  const inGame = path.resolve(__dirname, "../../Assets/Resources/Enemies");
  const all = {};
  for (const [id, def] of Object.entries(ENEMIES)) {
    if (only && !only.split(",").includes(id)) continue;
    const f = frames(def); all[id] = f;
    const e = P.edgeCheck(f[0]), cc = P.colorCount(f[0]);
    console.log(id.padEnd(22), f[0].w + "x" + f[0].h, "nubs", e.nubs.length, "strips", e.strips.length, "holes", e.holes.length, "colors", cc.length, "top", (cc[0].share * 100).toFixed(0) + "%");
    fs.writeFileSync(`${out}/enemy_${id}_idle.gif`, P.gif(f, { delay: DELAY, scale: 6 }));
  }
  const ids = Object.keys(all), short = (id) => id.replace(/_/g, " ").toUpperCase().slice(0, 12);
  // before (what is in the game now) next to after, then the 1x row
  const items = [];
  for (const id of ids) {
    const prev = path.join(inGame, `${id}_0.png`);
    if (fs.existsSync(prev)) items.push({ buf: P.load(prev), label: "OLD", scale: 4 });
    items.push({ buf: all[id][0], label: short(id), scale: 4 });
  }
  P.save(P.sheet(items, { cols: 6 }), out + "/enemies_lineup.png");
  P.save(P.sheet(ids.map((id) => ({ buf: all[id][0], label: "1X", scale: 1 })), { cols: 11 }), out + "/enemies_1x.png");
  P.save(P.sheet(ids.flatMap((id) => all[id].map((f, i) => ({ buf: f, label: id.slice(0, 4).toUpperCase() + i, scale: 3 }))), { cols: 8 }), out + "/enemies_frames.png");
}
