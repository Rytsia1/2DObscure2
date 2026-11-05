using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Obscure
{
    // Presents RunState.PendingReward (combat or treasure). Gold is automatic; the player picks one prize or skips.
    // Starlit Reliquary "Q · Combat Victory" board at 640x360: gold title, Rewards panel | Party panel.
    public class RewardScreen : GameScreen
    {
        protected override void Build()
        {
            var r = Run?.PendingReward ?? new RewardOffer { title = "VICTORY", text = "SPOILS OF WAR", choices = new List<RewardOption>() };
            Backdrop.Create(Root, false, 12, Palette.Hex(0x171E38), Palette.Hex(0x0B1022));
            Ui.Rect(Root, 0, 0, Ui.Width, Ui.Height, Palette.Hex(0x05070F, 210), Ui.Back + 1);

            // Title block per FELNA — UI System (Page 34)
            Ui.Divider(Root, 190, 14, 260, Palette.Gold3, Palette.Gold2);
            Ui.Label(Root, r.title.ToUpperInvariant(), 320, 24, Palette.Gold1, Align.Center, 2, FontKind.DotGothic);
            Ui.Label(Root, PixelFont.Fit(r.text.ToUpperInvariant(), 560, FontKind.DotGothic), 320, 50, Palette.Mist2, Align.Center, font: FontKind.DotGothic);

            // Rewards panel
            Ui.Panel(Root, 40, 68, 330, 250);
            Ui.Header(Root, "REWARDS", 54, 76, 318, font: FontKind.DotGothic);
            Ui.Badge(Root, $"+{r.gold} GOLD", 52, 96, 120, 18, Palette.Hex(0x1D1009), Palette.Ember2, Palette.Ember1, font: FontKind.DotGothic);
            if (Run != null) Ui.Label(Root, $"NOW {Run.Gold}", 360, 102, Palette.Ember1, Align.Right, font: FontKind.DotGothic);
            Ui.Label(Root, r.choices.Count > 0 ? "CHOOSE ONE PRIZE" : "NO PRIZE THIS TIME", 54, 124, Palette.Mist3, font: FontKind.DotGothic);

            var hint = Ui.Label(Root, "", 54, 290, Palette.Mist3, font: FontKind.DotGothic);
            var menu = Root.gameObject.AddComponent<Menu>();
            menu.onFocus = s => hint.Set(PixelFont.Wrap(s.tooltip ?? "", 300, FontKind.DotGothic));

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
                var b = Ui.Button(Root, label.ToUpperInvariant(), 64, 138 + i * 28, 296, () => Claim(choice), 24, font: FontKind.DotGothic);
                b.tooltip = detail.ToUpperInvariant();
                menu.Add(b);
            }

            var skip = Ui.Button(Root, r.choices.Count > 0 ? "SKIP PRIZE" : "CONTINUE", 64, 138 + r.choices.Count * 28, 296, () => Claim(-1), 24,
                style: r.choices.Count > 0 ? ButtonStyle.Slab : ButtonStyle.Primary, font: FontKind.DotGothic);
            skip.tooltip = "TAKE ONLY THE GOLD AND ADVANCE FORWARD";
            menu.Add(skip);

            // Party panel
            Ui.Panel(Root, 380, 68, 220, 250);
            Ui.Header(Root, "PARTY", 394, 76, 208, font: FontKind.DotGothic);
            for (int i = 0; i < party.Count && i < 4; i++)
            {
                var u = party[i];
                int py = 98 + i * 54;
                Ui.Image(Root, PixelArt.Box(30, 34, Palette.Ink1, Palette.Ink5), 392, py, Ui.Panels + 2);
                if (u.sprite != null) Ui.Image(Root, PixelArt.FromRows(u.sprite), 396, py + 31 - u.sprite.Length, Ui.Widget);
                Ui.Label(Root, PixelFont.Fit(u.name.ToUpperInvariant(), 110), 430, py + 2, u.Alive ? Palette.Mist1 : Palette.Mist3);
                Ui.Label(Root, $"LV {u.level}", 588, py + 2, Palette.Gold2, Align.Right);
                Ui.Image(Root, PixelArt.GaugeBar(158, 6, Palette.Hp, (float)u.hp / Mathf.Max(1, u.stats.maxHp)), 430, py + 14, Ui.Widget);
                Ui.Image(Root, PixelArt.GaugeBar(158, 6, Palette.Mp, (float)u.mp / Mathf.Max(1, u.stats.maxMp)), 430, py + 23, Ui.Widget);
            }

            int hx = Ui.KeyHint(Root, 40, 332, "CHOOSE", "ARROWS");
            Ui.KeyHint(Root, hx, 332, "CLAIM", "ENTER");
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
                if (Run.Act >= 8) Game.Go(GameState.Victory);
                else Game.Go(GameState.Hub);
            }
            else
            {
                Game.Go(GameState.Map);
            }
        }
    }
}
