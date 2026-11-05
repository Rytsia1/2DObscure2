using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Obscure
{
    // Shows RunState.CurrentEvent; each choice previews its consequences and is applied once by RunState.
    // Starlit Reliquary "M · Event / Choice" board at 640x360: gold-framed card, illustration | text + numbered choices.
    public class EventScreen : GameScreen
    {
        Transform phase;

        protected override void Build()
        {
            var e = Run?.CurrentEvent ?? Database.Events.Values.First();
            Backdrop.Create(Root, false, 0, Palette.Hex(0x171E38), Palette.Hex(0x0B1022));
            Ui.Rect(Root, 0, 0, Ui.Width, Ui.Height, Palette.Hex(0x05070F, 200), Ui.Back + 1);

            // Breadcrumb chip + run gold
            Ui.Badge(Root, $"FLOOR {(Run != null ? Run.Floor : 1)}  -  EVENT", 10, 9, 120, 15, Palette.Hex(0x070A14, 200), Palette.Ink4, Palette.Mist2);
            Ui.Badge(Root, $"{(Run != null ? Run.Gold : 0)} RUN GOLD", 530, 9, 100, 15, Palette.Hex(0x22120A), Palette.Ember2, Palette.Ember1);

            // Gold-framed event card: illustration column | text + choices
            Ui.Panel(Root, 45, 32, 550, 300, Palette.Ink2, Palette.Gold3);
            Illustration(47, 34, 206, 296);
            Ui.Rect(Root, 253, 34, 1, 296, Palette.Ink4);

            Ui.Label(Root, "EVENT", 268, 46, Palette.Gold2, font: FontKind.DotGothic);
            string title = e.title.ToUpperInvariant();
            int titleScale = PixelFont.Measure(title, FontKind.DotGothic) * 2 <= 310 ? 2 : 1;
            Ui.Label(Root, title, 268, 58, Palette.Gold1, Align.Left, titleScale, FontKind.DotGothic);
            Ui.Label(Root, PixelFont.Wrap(e.text.ToUpperInvariant(), 310), 268, 84, Palette.Mist2);
            var hint = Ui.Label(Root, "", 268, 286, Palette.Mist3);
            int hx = Ui.KeyHint(Root, 268, 310, "CHOOSE", "ARROWS");
            Ui.KeyHint(Root, hx, 310, "CONFIRM", "ENTER");

            var menu = NewMenu();
            menu.onFocus = s => hint.Set(PixelFont.Wrap(s.tooltip ?? "", 310));
            int cy = 280 - e.choices.Length * 28;
            for (int i = 0; i < e.choices.Length; i++)
            {
                int index = i;
                var c = e.choices[i];
                var blocked = Run != null ? Run.EventBlocked(c) : null;
                var b = Ui.Button(phase, $"{i + 1}  {c.label.ToUpperInvariant()}", 278, cy + i * 28, 300, () => Choose(index), 24, interactable: blocked == null);
                b.tooltip = blocked != null ? blocked.ToUpperInvariant() : Consequences(c);
                menu.Add(b);
            }
        }

        // Placeholder shrine scene for the illustration column (stands in for final event art).
        void Illustration(int x, int y, int w, int h)
        {
            Ui.Image(Root, PixelArt.Gradient(w, h, Palette.Hex(0x121A3A), Palette.Hex(0x0A0F22)), x, y, Ui.Panels + 1);
            Ui.Image(Root, PixelArt.Disc(50, Color.white), x + w / 2, y + 120, Ui.Panels + 2).color = new Color(1f, 0.95f, 0.72f, 0.12f);
            Ui.Rect(Root, x + w / 2 - 35, y + 105, 70, 131, Palette.Hex(0x2A3260), Ui.Panels + 3);
            Ui.Rect(Root, x + w / 2 - 21, y + 90, 42, 15, Palette.Hex(0x2A3260), Ui.Panels + 3);
            Ui.Image(Root, PixelArt.Diamond(17, Palette.Hex(0xFFF1B8)), x + w / 2 - 8, y + 118, Ui.Panels + 4);
            Ui.Rect(Root, x, y + h - 60, w, 60, Palette.Hex(0x10152B), Ui.Panels + 4);
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
            if (Run?.RelicManager != null)
            {
                if (c.relic)
                {
                    var clue = Run.RelicManager.GetAdditionalEventClue("relic");
                    if (clue != null) parts.Add(clue.ToUpperInvariant());
                }
            }
            return parts.Count == 0 ? "NOTHING HAPPENS" : string.Join(", ", parts);
        }

        void Choose(int index)
        {
            string outcome = Run.ChooseEvent(index);
            var m = NewMenu();
            Ui.Panel(phase, 266, 150, 316, 126, Palette.Ink3, Palette.Gold3).sortingOrder = Ui.Panels + 5;
            Ui.Header(phase, "OUTCOME", 280, 158, 304, Ui.Text + 1);
            Ui.Label(phase, PixelFont.Wrap(outcome.ToUpperInvariant(), 290), 280, 178, Palette.Mist1, order: Ui.Text + 1);
            m.Add(Ui.Button(phase, "CONTINUE EXPEDITION", 324, 244, 200, () => Game.Go(GameState.Map), 24, style: ButtonStyle.Primary));
        }
    }
}
