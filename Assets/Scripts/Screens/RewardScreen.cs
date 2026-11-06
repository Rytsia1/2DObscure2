using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Obscure
{
    // Presents RunState.PendingReward (combat or treasure). Gold is automatic; the player picks one prize or skips.
    // Starlit Reliquary "Q · Combat Victory" board at 640x360: ornament + big outlined title over a dimmed dungeon,
    // Rewards panel (Run Gold strip, prize rows) | Party panel, one gold "Continue" action.
    public class RewardScreen : GameScreen
    {
        protected override void Build()
        {
            var r = Run?.PendingReward ?? new RewardOffer { title = "VICTORY", text = "SPOILS OF WAR", choices = new List<RewardOption>() };
            Backdrop.Create(Root, false, 0, Palette.Hex(0x171E38), Palette.Hex(0x0B1022));
            ResultArt.Bricks(Root);
            Ui.Rect(Root, 0, 0, Ui.Width, Ui.Height, Palette.Hex(0x05070F, 209), Ui.Back + 2);

            // Title block
            ResultArt.Ornament(Root, 320, 17, 260, Palette.Gold3);
            ResultArt.Title(Root, r.title.ToUpperInvariant(), 320, 30, Palette.Gold1);
            Ui.Label(Root, PixelFont.Fit(r.text.ToUpperInvariant(), 560), 320, 74, Palette.Mist2, Align.Center, font: FontKind.Interface);

            // Rewards panel
            const int lx = 40, py = 100, pw = 270, ph = 196;
            Ui.Panel(Root, lx, py, pw, ph);
            Ui.Header(Root, "Rewards", lx + 8, py + 6, pw - 4);
            Ui.Image(Root, PixelArt.DashedBadge(pw - 16, 24, Palette.Hex(0x1D1009), Palette.Ember2), lx + 8, py + 24, Ui.Panels + 2);
            Ui.Image(Root, PixelArt.Coin(9, false), lx + 14, py + 31, Ui.Widget);
            Ui.Label(Root, $"+{r.gold}", lx + 27, py + 30, Palette.Ember1, order: Ui.Widget);
            Ui.Label(Root, "RUN GOLD", lx + 31 + PixelFont.Measure($"+{r.gold}", FontKind.DotGothic), py + 32, Palette.Ember2, order: Ui.Widget, font: FontKind.Interface);
            if (Run != null) Ui.Label(Root, $"RUN GOLD {Run.Gold - r.gold} › {Run.Gold}", lx + pw - 14, py + 32, Palette.Ember1, Align.Right, order: Ui.Widget, font: FontKind.Interface);
            Ui.Label(Root, r.choices.Count > 0 ? "CHOOSE ONE PRIZE" : "NO PRIZE THIS TIME", lx + 8, py + 56, Palette.Mist3, font: FontKind.Interface);

            var hint = Ui.Label(Root, "", lx + 8, py + ph - 14, Palette.Mist3, font: FontKind.Interface);
            var menu = Root.gameObject.AddComponent<Menu>();
            menu.onFocus = s => hint.Set(PixelFont.Fit(s.tooltip ?? "", pw - 16));

            var party = Run?.Party ?? Database.Party.Select(id => new Unit(Database.Characters[id])).ToList();
            for (int i = 0; i < r.choices.Count; i++)
            {
                int choice = i;
                var o = r.choices[i];
                var (label, detail) = RewardGenerator.Describe(o);
                if (o.kind == RewardKind.Upgrade)
                {
                    var u = party.FirstOrDefault(p => p.id == o.id || p.classDef?.id == o.id) ?? party[0];
                    detail = u.stats.Preview(Database.Upgrades[o.id].bonus);
                }
                int ry = py + 68 + i * 30;
                var b = Ui.Button(Root, "", lx + 8, ry, pw - 16, () => Claim(choice), 28, style: ButtonStyle.Card);
                b.tooltip = detail.ToUpperInvariant();
                PrizeIcon(b.transform, o);
                Ui.Label(b.transform, PixelFont.Fit(label, pw - 110, FontKind.DotGothic), 32, 3, Palette.Mist1, order: Ui.Widget + 1);
                Ui.Label(b.transform, PixelFont.Fit(detail.ToUpperInvariant(), pw - 110), 32, 17, Palette.Mist3, order: Ui.Widget + 1, font: FontKind.Interface);
                bool keep = o.kind == RewardKind.Relic;
                string tag = keep ? "KEPT EVEN IF THE RUN FAILS" : "ADDED TO BAG";
                Ui.Tag(b.transform, PixelFont.Fit(tag, 70), pw - 16 - PixelFont.Measure(PixelFont.Fit(tag, 70)) - 14, 8, keep ? TagKind.Keep : TagKind.Cls, 11, Ui.Widget + 1);
                menu.Add(b);
            }

            // Party panel (vitals after the fight)
            const int rx = 330;
            Ui.Panel(Root, rx, py, pw, ph);
            Ui.Header(Root, "Party", rx + 8, py + 6, pw - 4);
            for (int i = 0; i < party.Count && i < 4; i++)
            {
                var u = party[i];
                int y = py + 26 + i * 42;
                Color c = Palette.ForClass(u.classDef?.id);
                Ui.Image(Root, PixelArt.Box(22, 22, Color.Lerp(c, Palette.Ink1, 0.7f), Palette.Ink5), rx + 10, y, Ui.Widget);
                Ui.Rect(Root, rx + 18, y + 4, 6, 6, Color.Lerp(c, Palette.Hex(0xF3DCC0), 0.55f), Ui.Widget + 1);
                Ui.Rect(Root, rx + 14, y + 14, 14, 5, c, Ui.Widget + 1);
                Ui.Label(Root, PixelFont.Fit(u.name, 120, FontKind.DotGothic), rx + 40, y, u.Alive ? Palette.Mist1 : Palette.Mist3);
                Ui.Label(Root, $"LV {u.level}", rx + 46 + PixelFont.Measure(PixelFont.Fit(u.name, 120, FontKind.DotGothic), FontKind.DotGothic), y + 2, Palette.Mist3, font: FontKind.Interface);
                float hp = (float)u.hp / Mathf.Max(1, u.stats.maxHp);
                Ui.Image(Root, PixelArt.GaugeBar(pw - 56, 6, Mathf.Clamp01(hp), hp < 0.25f ? Palette.HpLow : Palette.Hp), rx + 40, y + 14, Ui.Widget);
                Ui.Image(Root, PixelArt.GaugeBar(pw - 56, 4, (float)u.mp / Mathf.Max(1, u.stats.maxMp), Palette.Mp), rx + 40, y + 22, Ui.Widget);
            }

            var go = Ui.Button(Root, r.choices.Count > 0 ? "Skip prize" : "Continue to map", 320 - 65, 314, 130, () => Claim(-1), 22,
                style: r.choices.Count > 0 ? ButtonStyle.Slab : ButtonStyle.Primary);
            go.tooltip = "TAKE ONLY THE GOLD AND CONTINUE";
            menu.Add(go);
            int hx = Ui.KeyHint(Root, lx, 342, "CHOOSE", "UP", "DN");
            Ui.KeyHint(Root, hx, 342, "CLAIM", "Z");
        }

        static void PrizeIcon(Transform t, RewardOption o)
        {
            if (o.kind == RewardKind.Relic && Database.Relics.TryGetValue(o.id ?? "", out var relic)) { Ui.Relic(t, 3, 3, 22, relic, Ui.Widget + 1); return; }
            Ui.Image(t, PixelArt.Box(22, 22, Palette.Ink0, Palette.Ink4), 3, 3, Ui.Widget + 1);
            var icon = PixelArt.ItemIcon(o.kind == RewardKind.Item ? "potion" : "scroll", o.kind == RewardKind.Item ? Palette.Hex(0xFF6A7A) : Palette.Gold2);
            Ui.Image(t, icon, 3 + (22 - (int)icon.rect.width) / 2, 3 + (22 - (int)icon.rect.height) / 2, Ui.Widget + 2);
        }

        static void Claim(int choice)
        {
            Run.ClaimReward(choice);
            if (choice >= 0) Sfx.Play(Sound.Success);
            if (Run.Won)
            {
                Run.TransferRunGoldToHub();
                PersistenceCommitHandler.CommitDungeonClear(Run);
                SaveManager.AutoSave(out _);
                Game.Go(Run.Act >= 8 ? GameState.Victory : GameState.DungeonClear);
            }
            else
            {
                Game.Go(GameState.Map);
            }
        }
    }

    // Starlit Reliquary "R · Dungeon Clear" board: night ridge, "Dungeon n of 8" + DUNGEON CLEAR + name + 8 diamonds,
    // gold transfer panel (Run Gold → Hub Gold 1:1 → new total), summary tiles, Return to Hub + commitment tags.
    public class DungeonClearScreen : GameScreen
    {
        protected override void Build()
        {
            Backdrop.Create(Root, true, 18);
            Ui.Image(Root, PixelArt.Ridge(Ui.Width, 140, Palette.Hex(0x1A2146), 3), 0, 220, Ui.Back + 5);
            Ui.Image(Root, PixelArt.Ridge(Ui.Width, 90, Palette.Hex(0x111733), 11), 0, 270, Ui.Back + 6);
            Ui.Rect(Root, 0, 0, Ui.Width, Ui.Height, Palette.Hex(0x05070F, 140), Ui.Back + 7);

            int act = Run != null ? Run.Act : 1;
            var stats = Run != null ? Run.Stats : new RunStats();
            int earned = stats.goldEarned, total = Run != null ? Run.Gold : 0, before = Mathf.Max(0, total - earned);

            Ui.Label(Root, $"DUNGEON {act} OF 8", 320, 17, Palette.Gold2, Align.Center, font: FontKind.Interface);
            ResultArt.Title(Root, "DUNGEON CLEAR", 320, 28, Palette.Gold1);
            Ui.Label(Root, Run?.Map?.name ?? "Dungeon", 320, 64, Palette.Mist2, Align.Center);
            for (int i = 1; i <= 8; i++)
            {
                var pip = i <= act ? PixelArt.Diamond(11, Palette.Gold2) : i == act + 1 ? PixelArt.Diamond(11, Palette.Gold1, false) : PixelArt.Diamond(11, Palette.Ink5, false);
                Ui.Image(Root, pip, 320 - 8 * 7 + (i - 1) * 14, 80, Ui.Text);
            }

            // Gold transfer panel
            const int px = 95, py = 112, pw = 450, ph = 96;
            Ui.Panel(Root, px, py, pw, ph, null, Palette.Gold3);
            Ui.Header(Root, "Run Gold transferred to Hub Gold", px + 8, py + 6, pw - 4);
            Ui.Label(Root, "RATE 1 : 1", px + pw - 8, py + 8, Palette.Mist1, Align.Right, font: FontKind.Interface);
            int y = py + 30;
            Ui.Label(Root, "RUN GOLD CLEARED", px + 14, y, Palette.Mist3, font: FontKind.Interface);
            Ui.CurrencyTag(Root, earned, false, px + 14, y + 12);
            Ui.Label(Root, "RUN GOLD NOW 0", px + 14, y + 36, Palette.Mist2, font: FontKind.Interface);
            Ui.Image(Root, PixelArt.FromRows(new[] { "....YY.", "YYYYYYY", "YYYYYYYY", "YYYYYYY", "....YY." }), px + 126, y + 16, Ui.Widget);
            Ui.Label(Root, "1:1", px + 129, y + 24, Palette.Gold2, font: FontKind.Interface);
            Ui.Label(Root, "HUB GOLD GAINED", px + 152, y, Palette.Mist3, font: FontKind.Interface);
            Ui.CurrencyTag(Root, earned, true, px + 152, y + 12);
            Ui.Label(Root, "COMMITTED · KEPT IF A LATER RUN FAILS", px + 152, y + 36, Palette.Mist2, font: FontKind.Interface);
            Ui.Label(Root, "=", px + 312, y + 14, Palette.Mist3);
            Ui.Label(Root, "NEW HUB GOLD TOTAL", px + 330, y, Palette.Mist3, font: FontKind.Interface);
            Ui.Label(Root, total.ToString("#,##0"), px + 330, y + 11, Palette.Gold1, Align.Left, 2);
            Ui.Label(Root, $"WAS {before:#,##0}", px + 330, y + 36, Palette.Mist2, font: FontKind.Interface);

            // Summary tiles
            var tiles = new (string k, string v, string n)[]
            {
                ("ENCOUNTERS", stats.battlesWon.ToString(), "CLEARED"),
                ("ELITES", stats.elitesWon.ToString(), "VANQUISHED"),
                ("CHAMBERS", stats.nodesCompleted.ToString(), "VISITED"),
                ("RELICS", (Run?.Relics.Count ?? 0).ToString(), "OWNED · KEPT"),
            };
            int tw = (pw - 18) / 4;
            for (int i = 0; i < tiles.Length; i++)
            {
                int tx = px + i * (tw + 6);
                Ui.Panel(Root, tx, 234, tw, 46);
                Ui.Label(Root, tiles[i].k, tx + 7, 240, Palette.Mist3, font: FontKind.Interface);
                Ui.Label(Root, tiles[i].v, tx + 7, 250, Palette.Mist1, Align.Left, 2);
                Ui.Label(Root, tiles[i].n, tx + 7, 270, Palette.Mist2, font: FontKind.Interface);
            }

            var menu = Root.gameObject.AddComponent<Menu>();
            menu.Add(Ui.Button(Root, "Return to Hub", px, 312, 120, () => Game.Go(GameState.Hub), 24, style: ButtonStyle.Primary));
            Ui.Label(Root, $"NEXT: DUNGEON {Mathf.Min(8, act + 1)} OF 8 UNLOCKED", px + 128, 321, Palette.Mist2, font: FontKind.Interface);
            int x = px + pw;
            foreach (var (t, k) in new[] { ($"{Run?.Relics.Count ?? 0} RELICS STAY", TagKind.Keep), ("MODIFIERS END HERE", TagKind.Temp), ("PROGRESS COMMITTED", TagKind.Keep) })
            {
                int w = PixelFont.Measure(t) + 8;
                x -= w;
                Ui.Tag(Root, t, x, 340, k);
                x -= 4;
            }
        }
    }

    // Shared art for the run-result boards (Q, R, S).
    public static class ResultArt
    {
        public static void Bricks(Transform root)
        {
            var col = Palette.Hex(0x0B1022);
            for (int y = 15; y < Ui.Height; y += 16) Ui.Rect(root, 0, y, Ui.Width, 1, col, Ui.Back + 1);
            for (int y = 0; y < Ui.Height; y += 16)
                for (int x = (y / 16) % 2 * 16; x < Ui.Width; x += 32) Ui.Rect(root, x, y, 1, 15, col, Ui.Back + 1);
        }

        // Ornament rule (.orn): two lines with a diamond in the middle.
        public static void Ornament(Transform root, int cx, int y, int w, Color c)
        {
            Ui.Rect(root, cx - w / 2, y + 3, w / 2 - 8, 1, c, Ui.Text);
            Ui.Rect(root, cx + 8, y + 3, w / 2 - 8, 1, c, Ui.Text);
            Ui.Image(root, PixelArt.Diamond(7, c), cx - 3, y, Ui.Text);
        }

        // Big display title (.d-xl.outline): 3x DotGothic with a 1px ink outline.
        public static void Title(Transform root, string text, int cx, int y, Color c)
        {
            foreach (var (dx, dy) in new[] { (-3, 0), (3, 0), (0, -3), (0, 3) })
                Ui.Label(root, text, cx + dx, y + dy, Palette.Ink0, Align.Center, 3, FontKind.DotGothic, Ui.Text);
            Ui.Label(root, text, cx, y, c, Align.Center, 3, FontKind.DotGothic, Ui.Text + 1);
        }
    }
}
