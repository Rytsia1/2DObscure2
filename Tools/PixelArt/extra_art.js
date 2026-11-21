// Generates reward category icons (16x16) and dungeon event illustrations (106x145)
// into Assets/Resources/Icons/Rewards/ and Assets/Resources/Events/.
// Usage: node Tools/PixelArt/extra_art.js
const fs = require("fs");
const path = require("path");
const crypto = require("crypto");
const { P, R, VOID } = require("./engine.js");

const ASSETS = path.resolve(__dirname, "../../Assets");
const META_TEMPLATE = fs.readFileSync(path.join(ASSETS, "Resources/Fx/hit.png.meta"), "utf8");

function ensureMeta(file) {
  if (!fs.existsSync(file + ".meta")) {
    fs.writeFileSync(
      file + ".meta",
      META_TEMPLATE.replace(/^guid: \w+/m, "guid: " + crypto.randomBytes(16).toString("hex"))
    );
  }
}

function saveSprite(grid, palette, file) {
  const h = grid.length;
  const w = grid[0].length;
  const inner = new P.Buf(w, h);
  for (let y = 0; y < h; y++) {
    for (let x = 0; x < w; x++) {
      const ch = grid[y][x];
      if (ch !== "." && palette[ch]) inner.pset(x, y, palette[ch]);
    }
  }
  const out = P.selout(inner, VOID, VOID);
  P.save(out, file);
  ensureMeta(file);
}

// 16x16 reward category icons (14x14 + 1px selout = 16x16)
function exportRewards() {
  const dir = path.join(ASSETS, "Resources/Icons/Rewards");
  fs.mkdirSync(dir, { recursive: true });

  const C = {
    G0: R.gold[0], G1: R.gold[1], G2: R.gold[2], G3: R.gold[3], G4: R.gold[4],
    S0: P.col([56, 62, 76]), S1: P.col([98, 108, 128]), S2: P.col([150, 162, 184]), S3: P.col([215, 224, 240]),
    C0: R.crimson[1], C1: R.crimson[2], C2: R.crimson[3], C3: R.crimson[4],
    P0: R.purple[1], P1: R.purple[2], P2: R.purple[3], P3: P.col([230, 196, 255]),
    Y0: R.cyan[1], Y1: R.cyan[2], Y2: R.cyan[3], Y3: P.col([225, 252, 255]),
    W0: R.wood[0], W1: R.wood[1], W2: R.wood[2], W3: R.wood[3],
    E0: R.leaf[0], E1: R.leaf[1], E2: R.leaf[2], E3: R.leaf[3],
  };

  const P16 = {
    a: C.G0, b: C.G1, c: C.G2, d: C.G3, e: C.G4,
    r: C.C0, R: C.C2, z: C.C3,
    s: C.S0, S: C.S1, t: C.S2, T: C.S3,
    p: C.P0, P: C.P2, q: C.P3,
    y: C.Y0, Y: C.Y2, Z: C.Y3,
    w: C.W0, W: C.W2, u: C.W3,
    g: C.E0, G: C.E2, h: C.E3,
  };

  const icons = {
    relic: [
      ".....cccc.....",
      "....c....c....",
      "....b....b....",
      ".....abcd.....",
      "....cddcbb....",
      "..ccdrRrcbcc..",
      ".cdbrRzzRrbca.",
      ".cbbrRRRRbbca.",
      "..aabbbbbbaa..",
      "...cdedcbb....",
      "....bcdeba....",
      ".....bcba.....",
      "......cd......",
      "......ba......",
    ],
    upgrade: [
      "......d.......",
      ".....cTd......",
      "....cTTTd.....",
      "...cTTtTTd....",
      "..cTTt.tTTd...",
      "....sTtTs.....",
      "....sTtTs..y..",
      "....sTtTs.yZy.",
      "....sTtTs..y..",
      "..y.sTtTs.....",
      ".yZy.sss......",
      "..y..bbb......",
      "....abcda.....",
      "..............",
    ],
    modifier: [
      "..............",
      "....pPqPqp....",
      "...pPqqqqPp...",
      "..pPqYYYYqp...",
      ".pPqYYZZYYqp..",
      ".pPqYZZZZYqp..",
      ".pPqYYZZYYqp..",
      "..pPqYYYYqp...",
      "...pPqqqqPp...",
      "....pPqqPp....",
      "...baappaa....",
      "..bcdeddcb....",
      "..aaaaaaab....",
      "..............",
    ],
    gold: [
      "..............",
      "......cccc....",
      ".....cdedcb...",
      "....cdeeedcb..",
      "...ccdcbcbda..",
      "...cdedcbdda..",
      "..cdeeedcaaa..",
      ".ccdedcbaedc..",
      "cdeddcbddedcb.",
      "cdeedccdeedcb.",
      "cdcbabacdcbba.",
      "aaaaaa.aaaaaa.",
      "..............",
      "..............",
    ],
    item: [
      ".....uuuu.....",
      ".....wWWw.....",
      "......ss......",
      ".....sTTs.....",
      "....sTTTTs....",
      "...sTyyyyTs...",
      "..sTyYYYYYTs..",
      "..sTyYZZYYTs..",
      "..sTyYYYYYTs..",
      "..sTyyyyyyTs..",
      "...sSSSSSSs...",
      "....ssssss....",
      "..............",
      "..............",
    ],
    equipment: [
      "..........tTT.",
      ".........tTTt.",
      "........tTTt..",
      ".......tTTt...",
      "......tTTt....",
      ".....tTTt.....",
      "...stTTs......",
      "..cbTs........",
      "..bWc.........",
      ".bWub.........",
      "aWua..........",
      "cba...........",
      "..............",
      "..............",
    ],
  };

  for (const [name, grid] of Object.entries(icons)) {
    saveSprite(grid, P16, path.join(dir, name + ".png"));
  }
}

// 106x145 procedural pixel-art event illustrations (scales 4x neatly to 424x580)
function makeEventBuf() {
  return new P.Buf(106, 145);
}

function clamp(v, lo, hi) { return v < lo ? lo : v > hi ? hi : v; }

function unpack(c) {
  if (Array.isArray(c)) return c;
  return [c & 0xff, (c >>> 8) & 0xff, (c >>> 16) & 0xff];
}

function mixCol(c0, c1, t) {
  const a = unpack(c0), b = unpack(c1), u = clamp(t, 0, 1);
  return P.rgb(a[0] + (b[0] - a[0]) * u, a[1] + (b[1] - a[1]) * u, a[2] + (b[2] - a[2]) * u);
}

function fillBackdrop(b, topCol, botCol) {
  for (let y = 0; y < b.h; y++) {
    const col = mixCol(topCol, botCol, y / (b.h - 1));
    for (let x = 0; x < b.w; x++) b.pset(x, y, col);
  }
}

function drawBrickArch(b, wallCol, mortarCol, archRadius = 38) {
  const cx = Math.floor(b.w / 2), archY = 58;
  const wRgb = unpack(wallCol);
  for (let y = 0; y < b.h; y++) {
    for (let x = 0; x < b.w; x++) {
      const inOpening = x > cx - archRadius && x < cx + archRadius &&
        (y >= archY || (x - cx) * (x - cx) + (y - archY) * (y - archY) <= archRadius * archRadius);
      if (!inOpening) {
        const brickRow = Math.floor(y / 8);
        const brickCol = Math.floor((x + (brickRow % 2) * 7) / 14);
        const edge = (y % 8 === 0) || ((x + (brickRow % 2) * 7) % 14 === 0);
        const n = ((brickRow * 37 + brickCol * 17) % 7) - 3;
        const c = edge ? mortarCol : P.rgb(wRgb[0] + n * 2, wRgb[1] + n * 2, wRgb[2] + n * 3);
        b.pset(x, y, c);
      }
    }
  }
  // Stone arch trim
  for (let y = 10; y <= archY; y++) {
    for (let x = cx - archRadius - 4; x <= cx + archRadius + 4; x++) {
      const d = Math.hypot(x - cx, y - archY);
      if (d >= archRadius - 1 && d <= archRadius + 4) {
        b.pset(x, y, mixCol(wallCol, [180, 178, 195], 0.35));
      }
    }
  }
}

function drawVignetteAndFloor(b, floorCol, floorLineCol, floorY = 104) {
  for (let y = floorY; y < b.h; y++) {
    const row = Math.floor((y - floorY) / 7);
    const seam = (y - floorY) % 7 === 0;
    for (let x = 0; x < b.w; x++) {
      const vSeam = ((x + row * 11) % 22) === 0;
      const depth = (y - floorY) / (b.h - floorY);
      const base = mixCol(floorCol, [10, 9, 14], depth * 0.65);
      b.pset(x, y, seam || vSeam ? floorLineCol : base);
    }
  }
  // Soft border frame
  const rim = P.col([52, 46, 36]);
  for (let x = 0; x < b.w; x++) { b.pset(x, 0, rim); b.pset(x, b.h - 1, rim); }
  for (let y = 0; y < b.h; y++) { b.pset(0, y, rim); b.pset(b.w - 1, y, rim); }
}

function addGlow(b, cx, cy, r, col, strength = 0.65) {
  const x0 = Math.max(1, Math.floor(cx - r)), x1 = Math.min(b.w - 2, Math.ceil(cx + r));
  const y0 = Math.max(1, Math.floor(cy - r)), y1 = Math.min(b.h - 2, Math.ceil(cy + r));
  for (let y = y0; y <= y1; y++) {
    for (let x = x0; x <= x1; x++) {
      const d = Math.hypot(x - cx, y - cy);
      if (d < r) {
        const f = Math.pow(1 - d / r, 1.8) * strength;
        const cur = b.px[y * b.w + x];
        b.pset(x, y, mixCol(cur, col, f));
      }
    }
  }
}

function rect(b, x, y, w, h, col) {
  for (let j = y; j < y + h; j++) for (let i = x; i < x + w; i++) b.pset(i, j, col);
}

function drawCandle(b, x, y, h, flameCol) {
  rect(b, x, y, 3, h, P.col([215, 205, 185]));
  rect(b, x, y, 1, h, P.col([245, 238, 222]));
  b.pset(x + 1, y - 1, P.col([80, 60, 45]));
  b.pset(x + 1, y - 2, flameCol);
  b.pset(x + 1, y - 3, P.col([255, 245, 180]));
}

function exportEvents() {
  const dir = path.join(ASSETS, "Resources/Events");
  fs.mkdirSync(dir, { recursive: true });

  const saveEvt = (name, fn) => {
    const b = makeEventBuf();
    fn(b);
    const file = path.join(dir, name + ".png");
    P.save(b, file);
    ensureMeta(file);
  };

  // 1. Shrine (Whispering Shrine / Altars)
  saveEvt("shrine", (b) => {
    fillBackdrop(b, P.col([14, 15, 24]), P.col([22, 20, 32]));
    drawBrickArch(b, P.col([44, 44, 58]), P.col([24, 24, 34]), 36);
    drawVignetteAndFloor(b, P.col([34, 32, 42]), P.col([20, 18, 26]), 106);
    // Stone altar pedestal
    rect(b, 33, 86, 40, 22, P.col([68, 70, 84]));
    rect(b, 29, 82, 48, 5, P.col([98, 100, 118]));
    rect(b, 38, 60, 30, 22, P.col([82, 84, 102]));
    rect(b, 42, 44, 22, 16, P.col([94, 96, 116]));
    // Carved crack & golden rune sigil
    for (let i = 0; i < 16; i++) b.pset(52 + (i % 3 === 0 ? 1 : 0), 46 + i, P.col([230, 188, 92]));
    addGlow(b, 53, 56, 28, P.col([240, 180, 75]), 0.45);
    drawCandle(b, 32, 74, 8, P.col([255, 165, 55]));
    drawCandle(b, 71, 74, 8, P.col([255, 165, 55]));
    addGlow(b, 33, 71, 14, P.col([255, 155, 55]), 0.5);
    addGlow(b, 72, 71, 14, P.col([255, 155, 55]), 0.5);
  });

  // 2. Cistern (D1_EVT_017 - Forgotten Cistern: flooded vaulted basin, crystal spring water, dripping stalactites)
  saveEvt("cistern", (b) => {
    fillBackdrop(b, P.col([10, 18, 28]), P.col([14, 26, 38]));
    drawBrickArch(b, P.col([38, 52, 64]), P.col([20, 28, 38]), 42);
    // Cistern stone basin + flooded turquoise water
    rect(b, 12, 92, 82, 10, P.col([72, 86, 98]));
    rect(b, 14, 95, 78, 44, P.col([20, 96, 116]));
    // Rippling water highlights & reflections
    for (let y = 96; y < 138; y += 3) {
      for (let x = 16; x < 90; x++) {
        const wave = ((x * 7 + y * 13) % 19 === 0);
        if (wave) b.pset(x, y, P.col([110, 220, 228]));
        else if ((x + y) % 11 === 0) b.pset(x, y, P.col([44, 154, 174]));
      }
    }
    // Dripping spout pouring water stream into the cistern
    rect(b, 48, 34, 10, 14, P.col([92, 106, 120]));
    rect(b, 50, 42, 6, 4, P.col([28, 38, 48]));
    for (let y = 46; y < 104; y++) {
      b.pset(52, y, P.col([130, 232, 240]));
      if (y % 2 === 0) b.pset(53, y, P.col([195, 250, 255]));
    }
    // Splash rings on water surface
    for (let x = 44; x <= 62; x++) {
      if (Math.abs(x - 53) > 2 && (x % 2 === 0)) b.pset(x, 104, P.col([205, 252, 255]));
    }
    addGlow(b, 53, 104, 34, P.col([72, 215, 225]), 0.55);
    // Mossy stone ledge and rim
    rect(b, 8, 92, 8, 50, P.col([54, 66, 76]));
    rect(b, 90, 92, 8, 50, P.col([54, 66, 76]));
    rect(b, 12, 138, 82, 5, P.col([58, 72, 84]));
    for (let x = 16; x < 88; x += 6) b.pset(x, 92, P.col([68, 128, 84]));
  });

  // 3. Survival (Fungal grove / shelter & embers)
  saveEvt("survival", (b) => {
    fillBackdrop(b, P.col([12, 18, 16]), P.col([18, 28, 22]));
    drawBrickArch(b, P.col([44, 52, 46]), P.col([22, 28, 24]), 40);
    drawVignetteAndFloor(b, P.col([34, 42, 34]), P.col([20, 26, 20]), 102);
    // Glowing mushrooms + mossy stones
    const caps = [[36, 92, 10], [66, 86, 13], [52, 98, 7]];
    for (const [mx, my, mr] of caps) {
      rect(b, mx - 2, my, 4, 14, P.col([185, 195, 172]));
      for (let dy = -mr / 2; dy <= 0; dy++) {
        const w = Math.floor(mr * Math.sqrt(1 - (dy / (mr / 2 + 1)) ** 2));
        for (let dx = -w; dx <= w; dx++) b.pset(mx + dx, my + Math.round(dy), P.col([70, 185, 135]));
      }
      addGlow(b, mx, my - 2, 18, P.col([85, 225, 155]), 0.45);
    }
  });

  // 4. NPC Encounter (cloaked lantern wanderer in archway)
  saveEvt("npc", (b) => {
    fillBackdrop(b, P.col([14, 13, 22]), P.col([22, 18, 28]));
    drawBrickArch(b, P.col([52, 48, 60]), P.col([26, 24, 32]), 34);
    drawVignetteAndFloor(b, P.col([38, 34, 42]), P.col([22, 20, 26]), 106);
    // Cloaked silhouette
    rect(b, 44, 62, 18, 44, P.col([62, 54, 72]));
    rect(b, 47, 50, 12, 14, P.col([78, 68, 90]));
    rect(b, 49, 55, 8, 6, P.col([24, 20, 30]));
    b.pset(51, 57, P.col([225, 195, 130]));
    b.pset(55, 57, P.col([225, 195, 130]));
    // Warm swinging lantern held right
    rect(b, 64, 76, 5, 7, P.col([250, 195, 75]));
    addGlow(b, 66, 79, 28, P.col([255, 180, 75]), 0.65);
  });

  // 5. Treasure (Ironbound chest overflowing with gold & gems)
  saveEvt("treasure", (b) => {
    fillBackdrop(b, P.col([18, 14, 16]), P.col([26, 20, 20]));
    drawBrickArch(b, P.col([58, 48, 42]), P.col([28, 22, 20]), 38);
    drawVignetteAndFloor(b, P.col([44, 36, 32]), P.col([24, 18, 16]), 104);
    // Open chest base & lid
    rect(b, 30, 82, 46, 24, P.col([108, 68, 38]));
    rect(b, 30, 82, 46, 3, P.col([222, 174, 58]));
    rect(b, 30, 103, 46, 3, P.col([145, 110, 42]));
    rect(b, 32, 62, 42, 16, P.col([90, 56, 30]));
    rect(b, 32, 62, 42, 3, P.col([222, 174, 58]));
    // Gold & ruby hoard inside
    rect(b, 34, 77, 38, 6, P.col([246, 205, 72]));
    b.pset(44, 78, P.col([230, 58, 72]));
    b.pset(58, 79, P.col([75, 220, 235]));
    addGlow(b, 53, 80, 30, P.col([250, 200, 70]), 0.55);
  });

  // 6. Trap & Hazard (Runic pressure plate & iron spikes)
  saveEvt("trap", (b) => {
    fillBackdrop(b, P.col([20, 12, 14]), P.col([28, 16, 18]));
    drawBrickArch(b, P.col([52, 42, 46]), P.col([26, 18, 22]), 36);
    drawVignetteAndFloor(b, P.col([42, 34, 36]), P.col([22, 16, 18]), 100);
    // Descending iron blades / spikes
    for (const sx of [34, 46, 58, 70]) {
      rect(b, sx - 2, 36, 5, 28, P.col([130, 138, 156]));
      rect(b, sx - 1, 64, 3, 8, P.col([185, 195, 212]));
      b.pset(sx, 72, P.col([235, 240, 250]));
    }
    // Red warning glyph on floor plate
    rect(b, 32, 106, 42, 14, P.col([68, 56, 60]));
    rect(b, 38, 110, 30, 6, P.col([220, 65, 65]));
    addGlow(b, 53, 113, 26, P.col([235, 65, 65]), 0.55);
  });

  // 7. Supernatural (Violet astral obelisk & wisp)
  saveEvt("supernatural", (b) => {
    fillBackdrop(b, P.col([16, 12, 28]), P.col([24, 16, 40]));
    drawBrickArch(b, P.col([46, 42, 68]), P.col([22, 20, 38]), 38);
    drawVignetteAndFloor(b, P.col([36, 32, 52]), P.col([20, 18, 30]), 104);
    // Levitating crystal obelisk shards
    rect(b, 47, 46, 12, 42, P.col([105, 68, 172]));
    rect(b, 50, 50, 6, 34, P.col([175, 128, 245]));
    rect(b, 52, 56, 2, 20, P.col([238, 220, 255]));
    addGlow(b, 53, 66, 34, P.col([178, 112, 250]), 0.65);
  });

  // 8. Puzzle & Exploration (Rotating celestial dial monolith)
  saveEvt("puzzle", (b) => {
    fillBackdrop(b, P.col([12, 16, 24]), P.col([20, 24, 34]));
    drawBrickArch(b, P.col([48, 54, 68]), P.col([24, 28, 38]), 38);
    drawVignetteAndFloor(b, P.col([36, 42, 52]), P.col([20, 24, 30]), 106);
    const cx = 53, cy = 68;
    for (let y = 38; y <= 98; y++) {
      for (let x = 23; x <= 83; x++) {
        const d = Math.hypot(x - cx, y - cy);
        if (d <= 26 && d >= 22) b.pset(x, y, P.col([215, 178, 86]));
        else if (d <= 16 && d >= 13) b.pset(x, y, P.col([128, 184, 212]));
        else if (d < 13) b.pset(x, y, P.col([44, 52, 68]));
      }
    }
    rect(b, 51, 56, 4, 24, P.col([235, 208, 112]));
    addGlow(b, cx, cy, 28, P.col([130, 190, 235]), 0.45);
  });

  // 9. Cursed & Risk (Ominous blood-sigil pedestal)
  saveEvt("cursed", (b) => {
    fillBackdrop(b, P.col([20, 8, 12]), P.col([28, 10, 16]));
    drawBrickArch(b, P.col([50, 32, 38]), P.col([24, 14, 18]), 36);
    drawVignetteAndFloor(b, P.col([38, 22, 26]), P.col([20, 10, 14]), 104);
    rect(b, 41, 76, 24, 28, P.col([58, 38, 44]));
    rect(b, 37, 72, 32, 5, P.col([88, 52, 60]));
    // Crimson horned chalice & chains
    rect(b, 48, 58, 10, 14, P.col([165, 32, 48]));
    rect(b, 46, 54, 14, 5, P.col([225, 62, 78]));
    addGlow(b, 53, 60, 32, P.col([220, 42, 62]), 0.65);
  });
}

exportRewards();
exportEvents();
console.log("Generated Reward icons and Event scenes.");
