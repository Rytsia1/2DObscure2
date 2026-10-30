using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Obscure
{
    // Shows RunState.CurrentEvent; each choice previews its consequences and is applied once by RunState.
    // Conforming to 640x360 Reference Resolution, Octopath Traveler II Aesthetics, Serif Display Headers.
    public class EventScreen : GameScreen
    {
        Transform phase;

        protected override void Build()
        {
            var e = Run?.CurrentEvent ?? Database.Events.Values.First();
            Backdrop.Create(Root, false, 8, Palette.DeepCharcoal, Palette.MidnightNavy, Palette.Dusk);

            // Grand Event Container Panel (480x312)
            Ui.Panel(Root, 80, 24, 480, 312, Palette.DeepCharcoal, Palette.DarkBorder);
            Ui.Label(Root, e.title.ToUpperInvariant(), 320, 36, Palette.ArcaneViolet, Align.Center, 2, FontKind.Serif);
            Ui.Divider(Root, 110, 58, 420, Palette.DarkBorder, Palette.AntiqueGoldDim);

            Ui.Label(Root, PixelFont.Wrap(e.text.ToUpperInvariant(), 420), 110, 68, Palette.WarmIvory);
            Ui.Label(Root, $"GOLD: {(Run != null ? Run.Gold : 0)}G", 530, 36, Palette.AntiqueGoldBright, Align.Right);
            var hint = Ui.Label(Root, "", 320, 312, Palette.MutedGray, Align.Center);

            var menu = NewMenu();
            menu.onFocus = s => hint.Set(s.tooltip ?? "");
            for (int i = 0; i < e.choices.Length; i++)
            {
                int index = i;
                var c = e.choices[i];
                var blocked = Run != null ? Run.EventBlocked(c) : null;
                var b = Ui.Button(phase, c.label.ToUpperInvariant(), 110, 136 + i * 36, 420, () => Choose(index), 24, interactable: blocked == null);
                b.tooltip = blocked != null ? blocked.ToUpperInvariant() : Consequences(c);
                menu.Add(b);
            }
        }

        Menu NewMenu()
        {
            if (phase != null) Object.Destroy(phase.gameObject);
            phase = new GameObject("Phase").transform;
            phase.SetParent(Root, false);
            return phase.gameObject.AddComponent<Menu>();
        }

        string Consequences(EventChoice c)
        {
            var parts = new List<string>();
            if (c.goldCost > 0) parts.Add($"-{c.goldCost} GOLD");
            if (c.gold > 0) parts.Add($"+{c.gold} GOLD");
            if (c.healPercent > 0) parts.Add($"HEAL {c.healPercent}% HP");
            if (c.hurtPercent > 0) parts.Add($"LOSE {c.hurtPercent}% MAX HP");
            if (c.item != null) parts.Add("+" + Database.Items[c.item].name.ToUpperInvariant());
            if (c.relic && Run?.EventRelic != null) parts.Add("+" + Database.Relics[Run.EventRelic].name.ToUpperInvariant());
            if (c.battleBuff != null) parts.Add("NEXT BATTLE: " + StatusInfo.Name(c.battleBuff.id).ToUpperInvariant());
            return parts.Count == 0 ? "NOTHING HAPPENS" : string.Join(", ", parts);
        }

        void Choose(int index)
        {
            string outcome = Run.ChooseEvent(index);
            var m = NewMenu();
            Ui.Panel(phase, 110, 136, 420, 126, Palette.MidnightNavy, Palette.AntiqueGoldDim).sortingOrder = Ui.Panels + 1;
            Ui.Label(phase, PixelFont.Wrap(outcome.ToUpperInvariant(), 390), 125, 150, Palette.WarmIvory, order: Ui.Text + 1);
            m.Add(Ui.Button(phase, "CONTINUE EXPEDITION", 220, 214, 200, () => Game.Go(GameState.Map), 24));
        }
    }
}
