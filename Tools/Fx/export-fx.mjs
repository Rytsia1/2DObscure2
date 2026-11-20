// Renders the game's new FX sheets with pixelfx and copies them into Assets/Resources/Fx/<name>.png/.json.
// usage (from the project root): node Tools/Fx/export-fx.mjs <pixelfx checkout> Assets/Resources/Fx
// pixelfx: git clone https://github.com/patch-gg/pixelfx. status-mark.js and impact-frame-wide.js (this folder) are the
// game's own effects; they are copied into the checkout first.
import { execFileSync } from "node:child_process";
import { copyFileSync, readdirSync, mkdirSync } from "node:fs";
import { join, resolve, dirname } from "node:path";
import { pathToFileURL, fileURLToPath } from "node:url";

const [pfx, dest] = process.argv.slice(2).map((p) => resolve(p));
const here = dirname(fileURLToPath(import.meta.url));
for (const [file, category] of [["status-mark.js", "status"], ["impact-frame-wide.js", "impacts"]])
  copyFileSync(join(here, file), join(pfx, "src/fx", category, file));
const { PAL } = await import(pathToFileURL(join(pfx, "src/core/index.js")).href);

const MARK = "status/status-mark";
const FX = {
  st_poison: [MARK, { arrows: 0, motes: "rise", palette: PAL.poison }],
  st_burn: [MARK, { arrows: 0, motes: "ember", palette: PAL.fire }],
  st_bleed: [MARK, { arrows: 0, motes: "drip", palette: PAL.blood }],
  st_curse: [MARK, { arrows: 0, motes: "rise", palette: PAL.shadow }],
  st_slow: [MARK, { arrows: -1, palette: PAL.ice }],
  buff: [MARK, { arrows: 1, palette: PAL.gold }],
  debuff: [MARK, { arrows: -1, palette: PAL.blood }],
  st_freeze: [MARK, { arrows: 0, motes: "rise", palette: PAL.ice }],
  st_shock: [MARK, { arrows: 0, motes: "rise", palette: PAL.electric }],
  st_stun: ["status/stunned", {}],
  st_sleep: ["status/sleep", {}],
  st_charm: ["status/charmed", {}],
  st_confuse: ["status/confused", {}],
  guard: ["impacts/shield-block", {}],
  cast: ["arcane/spell-charge", {}],
  charge: ["arcane/spell-charge", { palette: PAL.blood }],
  revive: ["holy/holy-pillar", {}],
  phase: ["shadow/dark-explosion", {}],
  campfire: ["fire/campfire", {}],
  // per-move hits (CombatScreen.MoveFx picks one from the skill / attack / attacker clip style)
  slash: ["melee/sword-slash", {}],
  cleave: ["melee/heavy-cleave", {}],
  spin: ["melee/spin-slash", {}],
  flurry: ["melee/dagger-flurry", {}],
  claw: ["melee/claw-swipe", {}],
  reap: ["melee/scythe-reap", {}],
  whip: ["melee/whip-crack", {}],
  kick: ["melee/uppercut", {}],
  pierce: ["melee/thrust", {}],
  thorns: ["nature/thorns", {}],
  spike: ["nature/earth-spike", {}],
  vine: ["nature/vine-whip", {}],
  meteor: ["fire/meteor", {}],
  frost: ["ice/ice-spikes", {}],
  bolt: ["lightning/lightning-strike", {}],
  // full-screen manga flash on a critical hit (CombatScreen.Resolve), drawn at 4x centred on the struck unit
  impact: ["impacts/impact-frame-wide", {}],
};

for (const [name, [id, opts]] of Object.entries(FX)) {
  const out = join(pfx, "records", "_game", name);
  mkdirSync(out, { recursive: true });
  execFileSync("node", ["tools/render.mjs", id, "--sheet", "--no-actor", "--out", out, "--opts", JSON.stringify(opts)], { cwd: pfx, stdio: "inherit" });
  const dir = join(out, id), base = id.split("/")[1];
  for (const ext of [".png", ".json"]) copyFileSync(join(dir, base + ext), join(dest, name + ext));
}
console.log("copied", Object.keys(FX).length, "sheets to", dest, readdirSync(dest).length, "files there");
