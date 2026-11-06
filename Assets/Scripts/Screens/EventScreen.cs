using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Obscure
{
    // Shows RunState.CurrentEvent; each choice previews its consequences and is applied once by RunState.
    // Starlit Reliquary "M · Event / Choice" board at 640x360: breadcrumb chip + Run Gold over a dimmed dungeon,
    // gold-framed card (illustration | EVENT label, title, text, numbered choice cards with cost / requirement tags).
    public class EventScreen : GameScreen
    {
        const int CardX = 45, CardY = 37, CardW = 550, CardH = 295, IllW = 210;
        Transform phase;

        protected override void Build()
        {
            var e = Run?.CurrentEvent ?? Database.Events.Values.First();
            Backdrop.Create(Root, false, 0, Palette.Hex(0x171E38), Palette.Hex(0x0B1022));
            Ui.Rect(Root, 0, 0, Ui.Width, Ui.Height, Palette.Hex(0x05070F, 204), Ui.Back + 1);

            // Breadcrumb chip: D<n> · dungeon · Node · Event
            string dungeon = Run?.Map?.name ?? "Dungeon";
            string crumb = $"D{Run?.Act ?? 1}";
            int cw = PixelFont.Measure(crumb, FontKind.DotGothic) + PixelFont.Measure(dungeon, FontKind.DotGothic) + PixelFont.Measure($" · NODE {Run?.Floor ?? 1} · EVENT") + 26;
            Ui.Image(Root, PixelArt.Badge(cw, 18, Palette.Hex(0x070A14, 200), Palette.Ink4), 10, 9, Ui.Panels);
            Ui.Label(Root, crumb, 17, 13, Palette.Gold2, order: Ui.Text);
            int bx = 17 + PixelFont.Measure(crumb, FontKind.DotGothic) + 5;
            Ui.Label(Root, dungeon, bx, 13, Palette.Mist1, order: Ui.Text);
            Ui.Label(Root, $" · NODE {Run?.Floor ?? 1} · EVENT", bx + PixelFont.Measure(dungeon, FontKind.DotGothic) + 2, 15, Palette.Mist2, order: Ui.Text, font: FontKind.Interface);
            int gold = Run != null ? Run.Gold : 0;
            int gw = Mathf.Max(84, PixelFont.Measure($"{gold:#,##0} RUN GOLD", FontKind.DotGothic) + 16);
            Ui.CurrencyTag(Root, gold, false, 630 - gw, 9);

            // Gold-framed event card
            Ui.Panel(Root, CardX, CardY, CardW, CardH, Palette.Ink2, Palette.Gold3);
            Illustration(CardX + 2, CardY + 2, IllW, CardH - 4);
            Ui.Rect(Root, CardX + 2 + IllW, CardY + 2, 1, CardH - 4, Palette.Ink4);

            int tx = CardX + IllW + 17, tw = CardW - IllW - 32;
            Ui.Label(Root, "EVENT", tx, CardY + 14, Palette.Gold2, font: FontKind.Interface);
            Ui.Label(Root, e.title, tx, CardY + 26, Palette.Gold1);
            string body = PixelFont.Wrap(e.text.ToUpperInvariant(), tw);
            Ui.Label(Root, body, tx, CardY + 44, Palette.Mist2, font: FontKind.Interface);
            int bodyBottom = CardY + 44 + (body.Count(c => c == '\n') + 1) * PixelFont.LineHeight;

            int hx = Ui.KeyHint(Root, tx, CardY + CardH - 18, "CHOOSE", "UP", "DN");
            Ui.KeyHint(Root, hx, CardY + CardH - 18, "CONFIRM", "Z");

            var menu = NewMenu();
            int rows = e.choices.Length;
            int y0 = Mathf.Max(bodyBottom + 10, CardY + CardH - 24 - rows * 30);
            for (int i = 0; i < rows; i++)
            {
                int index = i;
                var c = e.choices[i];
                var blocked = Run != null ? Run.EventBlocked(c) : null;
                ChoiceRow(menu, tx + 13, y0 + i * 30, tw - 13, i + 1, c, blocked, () => Choose(index));
            }
        }

        // Choice card (.pnl.flat; focused = gold on #2b2210; blocked = dashed ink): number · label + outcome · tag.
        void ChoiceRow(Menu menu, int x, int y, int w, int number, EventChoice c, string blocked, System.Action choose)
        {
            const int h = 26;
            var b = Ui.Button(phase, "", x, y, w, choose, h, interactable: blocked == null);
            var box = b.GetComponentInChildren<SpriteRenderer>();
            var num = Ui.Label(b.transform, number.ToString(), 7, 8, Palette.Mist2, order: Ui.Widget + 1);
            b.visual = on =>
            {
                box.sprite = blocked != null ? PixelArt.DashedBadge(w, h, Palette.Ink1, Palette.Ink5)
                    : on ? PixelArt.Box(w, h, Palette.Hex(0x2B2210), Palette.Gold2)
                    : PixelArt.Box(w, h, Palette.Ink2, Palette.Ink5);
                num.Color = blocked != null ? Palette.Mist3 : on ? Palette.Gold2 : Palette.Mist2;
            };
            b.visual(false);
            Color ink = blocked != null ? Palette.Mist3 : Palette.Mist1;
            Ui.Label(b.transform, PixelFont.Fit(c.label, w - 120, FontKind.DotGothic), 22, 3, ink, order: Ui.Widget + 1);
            string sub = blocked != null ? blocked.ToUpperInvariant() : Consequences(c);
            Ui.Label(b.transform, PixelFont.Fit(sub, w - 120), 22, 16, blocked != null ? Palette.Mist3 : Palette.Mist2, order: Ui.Widget + 1, font: FontKind.Interface);

            // Right tag: cost (temp, ember dashed) or the requirement that blocks it (cond, dashed)
            string tag = blocked != null ? PixelFont.Fit(blocked.ToUpperInvariant(), 92) : c.goldCost > 0 ? $"-{c.goldCost} RUN GOLD" : null;
            if (tag != null)
            {
                int tw = PixelFont.Measure(tag) + 8;
                Ui.Tag(b.transform, tag, w - tw - 6, 7, blocked != null ? TagKind.Cond : TagKind.Temp, 11, Ui.Widget + 1);
            }
            b.tooltip = sub;
            menu.Add(b);
        }

        // Placeholder shrine scene for the illustration column (stands in for final event art).
        void Illustration(int x, int y, int w, int h)
        {
            Ui.Image(Root, PixelArt.Gradient(w, h, Palette.Hex(0x121A3A), Palette.Hex(0x0A0F22)), x, y, Ui.Panels + 1);
            Ui.Image(Root, PixelArt.Disc(55, Color.white), x + w / 2 - 55, y + 60, Ui.Panels + 2).color = new Color(1f, 0.95f, 0.72f, 0.12f);
            Ui.Rect(Root, x + w / 2 - 35, y + 105, 70, h - 165, Palette.Hex(0x2A3260), Ui.Panels + 3);
            Ui.Rect(Root, x + w / 2 - 21, y + 90, 42, 15, Palette.Hex(0x2A3260), Ui.Panels + 3);
            Ui.Image(Root, PixelArt.Diamond(17, Palette.ElementHoly), x + w / 2 - 8, y + 122, Ui.Panels + 4);
            for (int i = 0; i < 6; i++) Ui.Rect(Root, x + w / 2 - 1, y + 145 + i * 6, 3, 3, Palette.Shield, Ui.Panels + 4);
            Ui.Rect(Root, x, y + h - 60, w, 60, Palette.Hex(0x10152B), Ui.Panels + 4);
            Ui.Label(Root, "EVENT ILLUSTRATION PLACEHOLDER", x + 6, y + h - 12, Palette.Mist3, order: Ui.Panels + 5, font: FontKind.Interface);
        }

        Menu NewMenu()
        {
            if (phase != null)
            {
                if (Application.isPlaying) Object.Destroy(phase.gameObject);
                else Object.DestroyImmediate(phase.gameObject);
            }
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
            if (c.relic && Run?.RelicManager != null)
            {
                var clue = Run.RelicManager.GetAdditionalEventClue("relic");
                if (clue != null) parts.Add(clue.ToUpperInvariant());
            }
            return parts.Count == 0 ? "NOTHING HAPPENS" : string.Join(", ", parts);
        }

        void Choose(int index)
        {
            string outcome = Run.ChooseEvent(index);
            var m = NewMenu();
            int x = CardX + IllW + 17, w = CardW - IllW - 32;
            Ui.Image(phase, PixelArt.Box(w, 120, Palette.Ink3, Palette.Gold3), x, 150, Ui.Panels + 5);
            Ui.Header(phase, "Outcome", x + 10, 158, w - 4, Ui.Text + 1);
            Ui.Label(phase, PixelFont.Wrap(outcome.ToUpperInvariant(), w - 20), x + 10, 178, Palette.Mist1, order: Ui.Text + 1, font: FontKind.Interface);
            m.Add(Ui.Button(phase, "Continue", x + w - 110, 238, 100, () => Game.Go(GameState.Map), 22, style: ButtonStyle.Primary));
        }
    }
}
