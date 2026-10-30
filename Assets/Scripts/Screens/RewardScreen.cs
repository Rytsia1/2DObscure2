using System.Collections.Generic;
using System.Linq;

namespace Obscure
{
    // Presents RunState.PendingReward (combat or treasure). Gold is automatic; the player picks one prize or skips.
    // Conforming to 640x360 Reference Resolution, Octopath Traveler II Aesthetics, Serif Display Headers.
    public class RewardScreen : GameScreen
    {
        protected override void Build()
        {
            var r = Run?.PendingReward ?? new RewardOffer { title = "VICTORY", text = "SPOILS OF WAR", choices = new List<RewardOption>() };
            Backdrop.Create(Root, false, 12, Palette.DeepCharcoal, Palette.MidnightNavy, Palette.SpecCrimson);

            // Grand Reward Container Panel (480x312)
            Ui.Panel(Root, 80, 24, 480, 312, Palette.DeepCharcoal, Palette.AntiqueGold);
            Ui.Label(Root, r.title.ToUpperInvariant(), 320, 34, Palette.AntiqueGoldBright, Align.Center, 2, FontKind.Serif);
            Ui.Divider(Root, 110, 56, 420, Palette.DarkBorder, Palette.AntiqueGoldBright);

            Ui.Label(Root, PixelFont.Wrap(r.text.ToUpperInvariant(), 420), 110, 66, Palette.WarmIvory);
            Ui.Badge(Root, $"+{r.gold} GOLD CLAIMED", 240, 92, 160, 20, Palette.MidnightNavy, Palette.AntiqueGoldDim, Palette.AntiqueGoldBright, Align.Center);

            var hint = Ui.Label(Root, "", 320, 312, Palette.MutedGray, Align.Center);
            var menu = Root.gameObject.AddComponent<Menu>();
            menu.onFocus = s => hint.Set(s.tooltip ?? "");

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
                var b = Ui.Button(Root, label.ToUpperInvariant(), 110, 124 + i * 36, 420, () => Claim(choice), 24);
                b.tooltip = detail.ToUpperInvariant();
                menu.Add(b);
            }

            var skip = Ui.Button(Root, r.choices.Count > 0 ? "SKIP PRIZE" : "CONTINUE", 110, 124 + r.choices.Count * 36, 420, () => Claim(-1), 24);
            skip.tooltip = "TAKE ONLY THE GOLD AND ADVANCE FORWARD";
            menu.Add(skip);
        }

        static void Claim(int choice)
        {
            Run.ClaimReward(choice);
            if (choice >= 0) Sfx.Play(Sound.Success);
            Game.Go(Run.Won ? GameState.Victory : GameState.Map);
        }
    }
}
