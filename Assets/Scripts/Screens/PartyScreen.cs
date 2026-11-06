using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Obscure
{
    // Party Management — Starlit Reliquary "C · Party Management" board at 640x360:
    // title + party-size tag + Hub Gold, four member cards (figure, level, KO state, HP/MP), party overview, footer.
    public class PartyScreen : GameScreen
    {
        // Member picked here is the one Character Status opens on.
        public static int SelectedIndex { get; private set; }

        const int Left = 14, RightEdge = 626, CardsY = 38, CardH = 166, FooterY = 327;

        List<Unit> party;
        static GameState BackTo => Game.I == null || Game.I.LastBase == GameState.MainMenu ? GameState.Hub : Game.I.LastBase;
        Menu menu;
        Transform page;

        protected override void Build()
        {
            party = Run?.Party
                ?? Game?.Roster?.GetActiveParty().Select(d => new Unit(d)).ToList()
                ?? Database.Party.Select(id => new Unit(Database.Characters[id])).ToList();
            SelectedIndex = Mathf.Clamp(SelectedIndex, 0, Mathf.Max(0, party.Count - 1));

            Backdrop.Create(Root, false, 0, Palette.Ink1, Palette.Ink1);
            menu = Root.gameObject.AddComponent<Menu>();
            menu.onBack = () => Game?.Go(BackTo);
            Rebuild();
        }

        void Select(int i)
        {
            if (i == SelectedIndex) return;
            SelectedIndex = i;
            Rebuild();
        }

        void Rebuild()
        {
            if (page != null)
            {
                if (Application.isPlaying) Object.Destroy(page.gameObject);
                else Object.DestroyImmediate(page.gameObject);
            }
            page = new GameObject("Party").transform;
            page.SetParent(Root, false);
            menu.items.Clear();
            menu.onFocus = null;

            // Header: title, party size tag, Hub Gold
            Ui.Label(page, "Party", Left, 14, Palette.Gold2);
            string size = party.Count >= CharacterRoster.PartySize ? $"{party.Count} / 4 · PARTY FULL" : $"{party.Count} / 4";
            Ui.Tag(page, size, Left + PixelFont.Measure("Party", FontKind.DotGothic) + 10, 13, TagKind.Keep, 13);
            int gold = Run != null ? Run.Gold : 0;
            int gw = Mathf.Max(84, PixelFont.Measure($"{gold:#,##0} HUB GOLD", FontKind.DotGothic) + 16);
            Ui.CurrencyTag(page, gold, true, RightEdge - gw, 10);

            // Member cards
            int cw = (RightEdge - Left - 3 * 8) / 4;
            string[] numerals = { "I", "II", "III", "IV" };
            for (int i = 0; i < 4; i++)
            {
                int x = Left + i * (cw + 8);
                if (i >= party.Count)
                {
                    Ui.Image(page, PixelArt.DashedBadge(cw, CardH, Palette.Ink0, Palette.Ink5), x, CardsY, Ui.Panels);
                    Ui.Label(page, "+ Create member", x + cw / 2, CardsY + CardH / 2 - 5, Palette.Mist3, Align.Center);
                    continue;
                }
                int idx = i;
                var u = party[i];
                var card = Ui.Button(page, "", x, CardsY, cw, () => { SelectedIndex = idx; Game?.Go(GameState.CharacterStatus); }, CardH, style: ButtonStyle.Card);
                card.selected = i == SelectedIndex;
                card.visual(false);
                menu.Add(card);
                MemberCard(card.transform, u, cw, numerals[i], i == SelectedIndex);
            }

            // Party overview
            int oy = CardsY + CardH + 8, oh = 319 - oy;
            Ui.Panel(page, Left, oy, RightEdge - Left, oh);
            Ui.Header(page, "Party Overview", Left + 8, oy + 6, RightEdge - Left - 4);
            int colW = (RightEdge - Left - 20) / 3, cx = Left + 10, cy = oy + 26;

            Ui.Label(page, "ROLES COVERED", cx, cy, Palette.Mist3, font: FontKind.Interface);
            int tx = cx, ty = cy + 11;
            foreach (var role in party.Select(u => u.classDef?.role).Where(r => !string.IsNullOrEmpty(r)).Distinct())
            {
                string t = PixelFont.Fit(role.ToUpperInvariant(), colW - 12);
                int w = PixelFont.Measure(t) + 8;
                if (tx + w > cx + colW - 6) { tx = cx; ty += 14; }
                tx += Ui.Tag(page, t, tx, ty) + 3;
            }

            cx += colW + 10;
            Ui.Label(page, "DAMAGE TYPES AVAILABLE", cx, cy, Palette.Mist3, font: FontKind.Interface);
            var elements = party.SelectMany(u => u.skills ?? new string[0])
                .Where(id => Database.Skills.ContainsKey(id)).Select(id => Database.Skills[id].element)
                .Prepend(Element.Physical).Distinct().ToList();
            int ex = cx;
            foreach (var el in elements)
            {
                ElementChip(page, el, ex, cy + 11);
                ex += 15;
                if (ex > cx + colW - 14) break;
            }

            cx += colW + 10;
            Ui.Label(page, "NEEDS ATTENTION", cx, cy, Palette.Mist3, font: FontKind.Interface);
            var ko = party.Where(u => !u.Alive).Select(u => u.name).ToList();
            string note = ko.Count == 0 ? "ALL MEMBERS READY." : $"{string.Join(", ", ko)} {(ko.Count == 1 ? "IS" : "ARE")} KO — REVIVE AT THE TEMPLE BEFORE ENTERING A DUNGEON.";
            Ui.Label(page, PixelFont.Wrap(note.ToUpperInvariant(), colW - 8), cx, cy + 12, ko.Count == 0 ? Palette.Ok : Palette.Err, font: FontKind.Interface);

            // Footer
            menu.Add(Ui.Button(page, "Back", Left, FooterY, 52, () => Game?.Go(BackTo), 22, showCursor: false));
            int hx = Ui.KeyHint(page, Left + 62, FooterY + 5, "SELECT MEMBER", "LT", "RT");
            Ui.KeyHint(page, hx, FooterY + 5, "STATUS", "S");
            menu.Add(Ui.Button(page, "Confirm Party", RightEdge - 104, FooterY, 104, () => Game?.Go(BackTo), 22, showCursor: false, style: ButtonStyle.Primary));
            menu.Add(Ui.Button(page, "View Status", RightEdge - 104 - 8 - 88, FooterY, 88, () => Game?.Go(GameState.CharacterStatus), 22, showCursor: false));

            if (party.Count > 0) menu.Focus(SelectedIndex, false, true);
            menu.onFocus = s => { int i = menu.items.IndexOf(s); if (i >= 0 && i < party.Count) Select(i); };
        }

        // One member card (.pnl, gold when selected): figure well, slot numeral, level, KO tag, name, race/class tags, HP/MP bars.
        static void MemberCard(Transform t, Unit u, int w, string numeral, bool selected)
        {
            const int wellH = 105;
            Ui.Image(t, PixelArt.Gradient(w - 4, wellH, Palette.Hex(0x16204A), Palette.Ink1), 2, 2, Ui.Widget + 1);
            Ui.Rect(t, 2, wellH + 2, w - 4, 1, Palette.Ink4, Ui.Widget + 1);
            if (selected) Ui.Rect(t, 2, 2, w - 4, 2, Palette.Gold2, Ui.Widget + 3);
            if (u.sprite != null && u.sprite.Length > 0)
            {
                int scale = Mathf.Max(1, (wellH - 20) / u.sprite.Length);
                var fig = Ui.Image(t, PixelArt.FromRows(u.sprite), 0, 0, Ui.Widget + 2, u.Alive ? Color.white : new Color(1, 1, 1, 0.35f));
                fig.transform.localScale = new Vector3(scale, scale, 1);
                fig.transform.localPosition = new Vector3((w - u.sprite[0].Length * scale) / 2, -(wellH - 7 - u.sprite.Length * scale), 0);
            }
            Ui.Label(t, numeral, 6, 5, Palette.Mist3, order: Ui.Widget + 3);
            Ui.Label(t, $"LV {u.level}", w - 6, 6, Palette.Gold1, Align.Right, order: Ui.Widget + 3, font: FontKind.Interface);
            if (!u.Alive)
            {
                const string ko = "KO · INCAPACITATED";
                int kw = PixelFont.Measure(ko) + 8;
                Ui.Image(t, PixelArt.Badge(kw, 12, Palette.Hex(0x3A1418), Palette.Err), (w - kw) / 2, 46, Ui.Widget + 3);
                Ui.Label(t, ko, (w - kw) / 2 + 4, 49, Palette.Hex(0xFFD2CC), order: Ui.Widget + 4, font: FontKind.Interface);
            }

            int y = wellH + 8;
            Ui.Label(t, PixelFont.Fit(u.name ?? "Hero", w - 14, FontKind.DotGothic), 7, y, u.Alive ? Palette.Mist1 : Palette.Mist3, order: Ui.Widget + 2);
            y += 14;
            int tx = 7;
            tx += Ui.Tag(t, u.race?.name ?? "Human", tx, y, TagKind.Cls, 11, Ui.Widget + 2) + 3;
            Ui.Tag(t, PixelFont.Fit((u.classDef?.name ?? u.role ?? "").ToUpperInvariant(), w - tx - 10), tx, y, TagKind.Cls, 11, Ui.Widget + 2);
            y += 15;
            float hp = (float)u.hp / Mathf.Max(1, u.stats.maxHp);
            VitalRow(t, "HP", y, w, hp, hp < 0.25f ? Palette.HpLow : Palette.Hp, u.hp.ToString());
            VitalRow(t, "MP", y + 10, w, (float)u.mp / Mathf.Max(1, u.stats.maxMp), Palette.Mp, u.mp.ToString());
        }

        // Bar row (.br): label · segmented bar · value.
        static void VitalRow(Transform t, string label, int y, int w, float ratio, Color fill, string value)
        {
            Ui.Label(t, label, 7, y, Palette.Mist3, order: Ui.Widget + 2, font: FontKind.Interface);
            Ui.Image(t, PixelArt.GaugeBar(w - 54, 6, Mathf.Clamp01(ratio), fill), 20, y, Ui.Widget + 2);
            Ui.Label(t, value, w - 7, y, Palette.Mist1, Align.Right, order: Ui.Widget + 2, font: FontKind.Interface);
        }

        // Element chip (.el): hue square with the 2-letter code.
        public static void ElementChip(Transform t, Element el, int x, int y, int order = Ui.Widget)
        {
            Ui.Image(t, PixelArt.Badge(13, 13, Palette.ForElement(el), Palette.Ink0), x, y, order);
            Ui.Label(t, Palette.ElementCode(el), x + 7, y + 3, Palette.Ink1, Align.Center, order: order + 1, font: FontKind.Interface);
        }
    }
}
