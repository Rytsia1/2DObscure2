using System.Collections.Generic;
using UnityEngine;

namespace Obscure
{
    // Shows RunState.CurrentEvent; each choice previews its consequences and is applied once by RunState.
    public class EventScreen : GameScreen
    {
        Transform phase;

        protected override void Build()
        {
            var e = Run.CurrentEvent;
            Backdrop.Create(Root, false, 6, Palette.Void, Palette.Night, Palette.Dusk);
            Ui.Panel(Root, 24, 20, 272, 144);
            Ui.Label(Root, e.title.ToUpper(), 160, 28, Palette.Lilac, Align.Center, 2);
            Ui.Label(Root, PixelFont.Wrap(e.text.ToUpper(), 248), 36, 48, Palette.Parchment);
            Ui.Label(Root, $"GOLD {Run.Gold}", 288, 152, Palette.Gold, Align.Right);
            var hint = Ui.Label(Root, "", 36, 152, Palette.Ash);

            var menu = NewMenu();
            menu.onFocus = s => hint.Set(s.tooltip ?? "");
            for (int i = 0; i < e.choices.Length; i++)
            {
                int index = i;
                var c = e.choices[i];
                var blocked = Run.EventBlocked(c);
                var b = Ui.Button(phase, c.label.ToUpper(), 40, 80 + i * 16, 240, () => Choose(index), interactable: blocked == null);
                b.tooltip = blocked != null ? blocked.ToUpper() : Consequences(c);
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
            if (c.item != null) parts.Add("+" + Database.Items[c.item].name.ToUpper());
            if (c.relic && Run.EventRelic != null) parts.Add("+" + Database.Relics[Run.EventRelic].name.ToUpper());
            if (c.battleBuff != null) parts.Add("NEXT BATTLE: " + StatusInfo.Name(c.battleBuff.id).ToUpper());
            return parts.Count == 0 ? "NOTHING HAPPENS" : string.Join(", ", parts);
        }

        void Choose(int index)
        {
            string outcome = Run.ChooseEvent(index);
            var m = NewMenu();
            Ui.Panel(phase, 32, 76, 256, 64, Palette.Night, Palette.Shade).sortingOrder = Ui.Panels + 1;
            Ui.Label(phase, PixelFont.Wrap(outcome.ToUpper(), 236), 40, 84, Palette.Bone, order: Ui.Text + 1);
            m.Add(Ui.Button(phase, "CONTINUE", 110, 120, 100, () => Game.Go(GameState.Map)));
        }
    }
}
