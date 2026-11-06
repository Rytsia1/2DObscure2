using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Obscure
{
    // Shared frame for the Hub service boards (H1 Shop, H2 Armory, I Blacksmith, V1 Temple, V2 Magic Shop):
    // Back + h1 title header, Hub Gold on the right, pages rebuilt on every change, Back/Esc returns to the Hub.
    public abstract class ServiceScreen : GameScreen
    {
        protected const int Left = 12, RightEdge = 628, BodyY = 33, BodyBottom = 351;
        protected static readonly Color Ok = Palette.Hex(0x9AF0BF), No = Palette.Hex(0xFF9A8C), Up = Palette.Hex(0x7FF0AD);

        protected Menu menu;
        protected Transform page;
        protected string feedback;
        protected bool feedbackBad;
        int focusIndex;
        bool rebuilding;
        readonly Dictionary<Selectable, Action> selectOnFocus = new Dictionary<Selectable, Action>();

        protected static int Gold => Run != null ? Run.Gold : 0;
        protected static InventoryRuntime Bag => Run?.Inventory ?? new InventoryRuntime();
        protected static List<Unit> Party => Run?.Party ?? Database.Party.Select(id => new Unit(Database.Characters[id])).ToList();

        protected override void Build()
        {
            BuildBackground();
            menu = Root.gameObject.AddComponent<Menu>();
            menu.onBack = () => Game?.Go(GameState.Hub);
            menu.onFocus = s =>
            {
                if (rebuilding || s == null) return;
                focusIndex = menu.items.IndexOf(s);
                if (!s.selected && selectOnFocus.TryGetValue(s, out var select)) select();
            };
            Init();
            Rebuild();
        }

        protected virtual void BuildBackground() => Backdrop.Create(Root, false, 0, Palette.Ink1, Palette.Ink1);
        protected virtual void Init() { }
        protected abstract void BuildPage();

        protected void Rebuild()
        {
            if (Root == null || rebuilding) return;
            rebuilding = true;
            try
            {
                if (page != null)
                {
                    if (Application.isPlaying) UnityEngine.Object.Destroy(page.gameObject);
                    else UnityEngine.Object.DestroyImmediate(page.gameObject);
                }
                page = new GameObject("Page").transform;
                page.SetParent(Root, false);
                menu.items.Clear();
                selectOnFocus.Clear();
                BuildPage();
                if (menu.items.Count > 0) menu.Focus(Mathf.Clamp(focusIndex, 0, menu.items.Count - 1), false, true);
                AfterRebuild();
            }
            finally { rebuilding = false; }
        }

        // Hook for screens with an open dialog to take keyboard focus back after the page is rebuilt.
        protected virtual void AfterRebuild() { }

        protected void Say(string text, bool bad)
        {
            feedback = text;
            feedbackBad = bad;
            Sfx.Play(bad ? Sound.Deny : Sound.Success);
            Rebuild();
        }

        // Header: Back (btn.sm) · title (h1 gold). Returns the x where the next header item can go.
        protected int Header(string title)
        {
            menu.Add(Ui.Button(page, "Back", Left, 9, 40, () => Game?.Go(GameState.Hub), 18, showCursor: false));
            Ui.Label(page, title, Left + 50, 13, Palette.Gold2);
            return Left + 50 + PixelFont.Measure(title, FontKind.DotGothic) + 10;
        }

        // Hub Gold (.cur.hub) on the right; returns its left x.
        protected int GoldTag()
        {
            int w = Mathf.Max(84, PixelFont.Measure($"{Gold:#,##0} HUB GOLD", FontKind.DotGothic) + 16);
            Ui.CurrencyTag(page, Gold, true, RightEdge - w, 9);
            return RightEdge - w;
        }

        protected void BagCount(int rightX) =>
            Ui.Label(page, $"BAG {Bag.EquipmentCount} / {InventoryRuntime.MaxEquipmentCapacity}", rightX - 8, 15, Palette.Mist2, Align.Right, font: FontKind.Interface);

        // Selectable list row: transparent until focused / chosen (gold frame on #2b2210 like the boards' focused row).
        protected Selectable Row(int x, int y, int w, int h, bool chosen, Action select, bool locked = false)
        {
            var row = Ui.Button(page, "", x, y, w, select, h);
            var box = row.GetComponentInChildren<SpriteRenderer>();
            row.selected = chosen;
            row.visual = on =>
            {
                box.sprite = on || row.selected ? PixelArt.Badge(w, h, Palette.Hex(0x2B2210), on ? Palette.Gold1 : Palette.Gold2)
                    : locked ? PixelArt.DashedBadge(w, h, Palette.Ink1, Palette.Ink5)
                    : null;
            };
            row.visual(false);
            if (select != null) selectOnFocus[row] = select;
            menu.Add(row);
            return row;
        }

        // Tab (.tab) strip item; disabled tabs are dashed and not selectable.
        protected Selectable Tab(string text, int x, int y, bool on, Action go, bool enabled = true)
        {
            int w = PixelFont.Measure(text, FontKind.DotGothic) + 18;
            var t = Ui.Button(page, text, x, y, w, go, 20, interactable: enabled, showCursor: false);
            var box = t.GetComponentInChildren<SpriteRenderer>();
            t.visual = focus =>
            {
                box.sprite = enabled ? TabBox(w, 20, on, focus) : PixelArt.DashedBadge(w, 20, Palette.Ink2, Palette.Ink5);
                t.label.Color = !enabled ? Palette.Mist3 : on || focus ? Palette.Gold1 : Palette.Mist2;
            };
            t.visual(false);
            if (enabled) menu.Add(t);
            return t;
        }

        protected static Sprite TabBox(int w, int h, bool on, bool focus)
        {
            var fill = on || focus ? Palette.Ink3 : Palette.Ink2;
            var border = on ? Palette.Gold3 : focus ? Palette.Mist3 : Palette.Ink4;
            var px = new Color32[w * h];
            for (int y = 0; y < h; y++)
            {
                int py = h - 1 - y;
                for (int x = 0; x < w; x++)
                {
                    bool edge = x == 0 || x == w - 1 || py == 0;
                    px[y * w + x] = edge ? border : on && py <= 2 ? Palette.Gold2 : fill;
                }
            }
            return PixelArt.SpriteFrom(px, w, h, PixelArt.TopLeft, "tab");
        }

        // Toast (.toast.ok/.bad/.gold): icon square + one line of text.
        protected void Toast(int x, int y, int w, string text, bool bad, string icon = "!")
        {
            string t = PixelFont.Wrap(text.ToUpperInvariant(), w - 26);
            int h = Mathf.Max(18, 8 + (t.Count(c => c == '\n') + 1) * PixelFont.LineHeight);
            Color border = bad ? Palette.Hex(0xB5483E) : Palette.Gold3, ic = bad ? Palette.Err : Palette.Gold2;
            Ui.Image(page, PixelArt.Box(w, h, Palette.Ink2, border), x, y, Ui.Panels + 3);
            Ui.Image(page, PixelArt.Badge(14, 14, ic, ic), x + 2, y + 2, Ui.Widget);
            Ui.Label(page, icon, x + 9, y + 6, Palette.Ink0, Align.Center, order: Ui.Widget + 1, font: FontKind.Interface);
            Ui.Label(page, t, x + 21, y + 5, Palette.Mist1, order: Ui.Widget, font: FontKind.Interface);
        }

        protected void Feedback(int x, int y, int w)
        {
            if (string.IsNullOrEmpty(feedback)) return;
            Toast(x, y, w, feedback, feedbackBad, feedbackBad ? "!" : "✓");
        }

        // Stat row (.sr): label left, value right, ink rule under.
        protected int StatRow(int x, int y, int w, string label, string value, Color? color = null)
        {
            Ui.Label(page, label, x, y + 2, Palette.Mist2, font: FontKind.Interface);
            Ui.Label(page, value, x + w, y + 2, color ?? Palette.Mist1, Align.Right, font: FontKind.Interface);
            Ui.Rect(page, x, y + 12, w, 1, Palette.Ink3, Ui.Panels + 2);
            return y + 15;
        }

        // Rarity chip (.rar): letter block + name; Legendary/Mythic use a double frame.
        protected int RarityChip(int x, int y, EquipmentRarity r)
        {
            Color c = EquipmentRarities.GetColor(r);
            string name = r.ToString().ToUpperInvariant();
            int w = PixelFont.Measure(name) + 16;
            Ui.Image(page, r >= EquipmentRarity.Legendary ? PixelArt.DoubleBorderBox(w, 11, Palette.Ink0, c) : PixelArt.Badge(w, 11, Palette.Ink0, c), x, y, Ui.Widget);
            Ui.Image(page, PixelArt.Badge(7, 7, c, c), x + 2, y + 2, Ui.Widget + 1);
            Ui.Label(page, Palette.RarityBadge(r), x + 3, y + 2, Palette.Ink0, order: Ui.Widget + 2, font: FontKind.Interface);
            Ui.Label(page, name, x + 12, y + 2, c, order: Ui.Widget + 1, font: FontKind.Interface);
            return w;
        }

        // Small item slot for consumables / materials (.slot with a .pi icon).
        protected void ConsumableSlot(int x, int y, int size, ItemDef def, bool material = false)
        {
            Color rc = def != null ? EquipmentRarities.GetColor(def.rarity) : Palette.RarityCommon;
            Ui.Image(page, PixelArt.Box(size, size, Palette.Ink0, Color.Lerp(rc, Palette.Ink4, 0.45f)), x, y, Ui.Widget);
            var icon = material ? PixelArt.ItemIcon("ore", Palette.ElementIce) : PixelArt.ItemIcon(ConsumableKind(def), ConsumableTint(def));
            Ui.Image(page, icon, x + (size - (int)icon.rect.width) / 2, y + (size - (int)icon.rect.height) / 2, Ui.Widget + 1);
        }

        protected static string ConsumableKind(ItemDef d) => d?.category switch
        {
            "Food" => "herb",
            "Tactical Item" or "Exploration Item" => "scroll",
            _ => "potion",
        };

        protected static Color ConsumableTint(ItemDef d) =>
            d == null ? Palette.Mist2
            : d.isRevive ? Palette.Gold1
            : d.mana > 0 || d.manaPercent > 0 ? Palette.Mp
            : d.heal > 0 || d.healPercent > 0 ? Palette.Hex(0xFF6A7A)
            : d.category == "Buff Item" ? Palette.Xp
            : Palette.ElementWind;

        // Party portrait chip (.pt) used by the "Shopping for" switchers.
        protected static void Portrait(Transform t, Unit u, int x, int y, int size, bool on, bool dim = false)
        {
            Color c = Palette.ForClass(u.classDef?.id);
            if (dim) c = Color.Lerp(c, Palette.Ink3, 0.7f);
            Ui.Image(t, PixelArt.Box(size, size, Color.Lerp(c, Palette.Ink1, 0.7f), on ? Palette.Gold2 : Palette.Ink5), x, y, Ui.Widget + 1);
            Ui.Rect(t, x + size / 2 - 3, y + 4, 6, 6, Color.Lerp(c, Palette.Hex(0xF3DCC0), dim ? 0.2f : 0.55f), Ui.Widget + 2);
            Ui.Rect(t, x + 3, y + size - 6, size - 6, 4, c, Ui.Widget + 2);
        }

        protected static string Money(int v) => v.ToString("#,##0");
    }

    // ===================================================================== H1 · Shop (consumables)
    public class HubShopScreen : ServiceScreen
    {
        List<ConsumableShopEntry> catalog;
        bool sellTab;
        int selected;
        bool dialogOpen;
        int qty = 1;
        Menu dialogMenu;

        protected override void AfterRebuild()
        {
            if (dialogMenu != null) dialogMenu.Focus(dialogMenu.items.Count - 1, false, true);
        }

        protected override void Init() => catalog = HubEconomy.GenerateHubShopCatalog();

        List<(string id, string name, string sub, ItemDef def, int price, int owned)> Rows()
        {
            if (!sellTab)
                return catalog.Select(c => (c.itemId, c.name, Database.Items.TryGetValue(c.itemId, out var d) ? FirstLine(d) : c.description, d, c.cost, Bag.GetConsumableCount(c.itemId))).ToList();
            return Bag.consumables.Select(s => (s.itemId, s.Definition?.name ?? s.itemId, s.Definition != null ? FirstLine(s.Definition) : "", s.Definition, SellValue(s.Definition), s.count)).ToList();
        }

        static string FirstLine(ItemDef d) => string.IsNullOrEmpty(d.itemEffect) ? (d.description ?? "") : d.itemEffect;
        static int SellValue(ItemDef d) => Math.Max(1, (d?.shopValue ?? 20) / 2);

        protected override void BuildPage()
        {
            int x = Header("Shop");
            x += Ui.Tag(page, "HUB SHOP · CONSUMABLES · PAYS IN HUB GOLD", x, 13, TagKind.Keep, 13) + 6;
            const string armory = "Equipment is at the Armory ›";
            menu.Add(Ui.Button(page, armory, x, 9, PixelFont.Measure(armory, FontKind.DotGothic) + 14, () => Game?.Go(GameState.Armory), 18, showCursor: false, style: ButtonStyle.Flat));
            BagCount(GoldTag());

            var rows = Rows();
            selected = Mathf.Clamp(selected, 0, Mathf.Max(0, rows.Count - 1));
            dialogMenu = null;

            // Buy / Sell tabs over the list panel
            int tx = Left;
            tx += Tab("Buy", tx, BodyY, !sellTab, () => { sellTab = false; selected = 0; feedback = null; Rebuild(); }).size.x + 2;
            Tab("Sell", tx, BodyY, sellTab, () => { sellTab = true; selected = 0; feedback = null; Rebuild(); });
            int lw = 419, py = BodyY + 19;
            Ui.Panel(page, Left, py, lw, BodyBottom - py);
            Ui.Label(page, "ITEM", Left + 56, py + 7, Palette.Mist3, font: FontKind.Interface);
            Ui.Label(page, "TYPE", Left + 262, py + 7, Palette.Mist3, font: FontKind.Interface);
            Ui.Label(page, "PRICE", Left + lw - 12, py + 7, Palette.Mist3, Align.Right, font: FontKind.Interface);
            int y = py + 18;
            for (int i = 0; i < rows.Count && y < BodyBottom - 30; i++)
            {
                int idx = i;
                var r = rows[i];
                bool afford = sellTab || Gold >= r.price;
                var row = Row(Left + 18, y, lw - 26, 26, i == selected, () => { if (selected != idx) { selected = idx; Rebuild(); } });
                var t = row.transform;
                Color rc = r.def != null ? EquipmentRarities.GetColor(r.def.rarity) : Palette.Mist1;
                Ui.Image(t, PixelArt.Box(20, 20, Palette.Ink0, Color.Lerp(rc, Palette.Ink4, 0.45f)), 4, 3, Ui.Widget + 1);
                var icon = PixelArt.ItemIcon(ConsumableKind(r.def), ConsumableTint(r.def));
                Ui.Image(t, icon, 4 + (20 - (int)icon.rect.width) / 2, 3 + (20 - (int)icon.rect.height) / 2, Ui.Widget + 2);
                Ui.Label(t, PixelFont.Fit(r.name, 170, FontKind.DotGothic), 30, 2, rc, order: Ui.Widget + 2);
                Ui.Label(t, PixelFont.Fit(r.sub.ToUpperInvariant(), 180), 30, 16, Palette.Mist3, order: Ui.Widget + 2, font: FontKind.Interface);
                Ui.Label(t, sellTab ? $"OWN {r.owned}" : (r.def?.category ?? "CONSUMABLE").ToUpperInvariant(), 226, 10, Palette.Mist2, order: Ui.Widget + 2, font: FontKind.Interface);
                Ui.Label(t, Money(r.price), lw - 34, afford ? 7 : 3, afford ? Palette.Gold1 : Palette.Err, Align.Right, order: Ui.Widget + 2);
                if (!afford) Ui.Label(t, $"NEED {Money(r.price - Gold)} MORE", lw - 34, 16, Palette.Err, Align.Right, order: Ui.Widget + 2, font: FontKind.Interface);
                y += 28;
            }
            if (rows.Count == 0)
                Ui.Label(page, sellTab ? "NOTHING TO SELL." : "SOLD OUT.", Left + lw / 2, py + 60, Palette.Mist3, Align.Center, font: FontKind.Interface);

            BuildDetail(rows);
            if (dialogOpen && !sellTab && rows.Count > 0) BuildDialog(rows[selected]);
        }

        void BuildDetail(List<(string id, string name, string sub, ItemDef def, int price, int owned)> rows)
        {
            int dx = 438, dw = RightEdge - dx;
            Ui.Panel(page, dx, BodyY, dw, BodyBottom - BodyY);
            if (rows.Count == 0) { Feedback(dx + 8, BodyBottom - 64, dw - 16); return; }
            var r = rows[selected];
            Ui.Header(page, PixelFont.Fit(r.name, dw - 24, FontKind.DotGothic), dx + 8, BodyY + 6, dw - 4);
            int x = dx + 8, w = dw - 16, y = BodyY + 24;
            int cx = x;
            if (r.def != null) cx += RarityChip(cx, y, r.def.rarity) + 4;
            Ui.Tag(page, sellTab ? "OWNED" : "CONSUMABLE", cx, y);
            y += 17;
            string desc = PixelFont.Wrap((r.def?.description ?? r.sub).ToUpperInvariant(), w);
            Ui.Label(page, desc, x, y, Palette.Mist1, font: FontKind.Interface);
            y += (desc.Count(c => c == '\n') + 1) * PixelFont.LineHeight + 6;
            y = StatRow(x, y, w, "YOU OWN", r.owned.ToString());
            y = StatRow(x, y, w, sellTab ? "SELLS FOR" : "PRICE EACH", $"{Money(r.price)} HUB GOLD", Palette.Gold2);
            if (r.def?.target != null) y = StatRow(x, y, w, "TARGET", r.def.target.ToUpperInvariant());
            y += 6;
            if (!sellTab && Gold < r.price) { Toast(x, y, w, $"{r.name}: NOT ENOUGH HUB GOLD — NEED {Money(r.price - Gold)} MORE.", true); y += 30; }
            Feedback(x, BodyBottom - 64, w);

            Ui.Rect(page, dx + 2, BodyBottom - 34, dw - 4, 1, Palette.Ink4);
            var buy = Ui.Button(page, sellTab ? $"Sell for {Money(r.price)}" : "Buy...", x, BodyBottom - 27, w, sellTab ? (Action)(() => Sell(r.id, r.price)) : () => { qty = 1; dialogOpen = true; Rebuild(); }, 20, interactable: sellTab || Gold >= r.price, showCursor: false, style: ButtonStyle.Primary);
            menu.Add(buy);
        }

        // Purchase confirmation (.dlg) with quantity stepper over a scrim.
        void BuildDialog((string id, string name, string sub, ItemDef def, int price, int owned) r)
        {
            int maxQty = Math.Max(1, Math.Min(99, Gold / Math.Max(1, r.price)));
            qty = Mathf.Clamp(qty, 1, maxQty);
            int total = qty * r.price;
            const int w = 260, h = 136;
            int x = (Ui.Width - w) / 2, y = 112;
            Ui.Rect(page, 0, 0, Ui.Width, Ui.Height, Palette.Hex(0x05070F, 184), Ui.Overlay - 4);
            Ui.Image(page, PixelArt.Box(w, h, Palette.Ink2, Palette.Gold3), x, y, Ui.Overlay - 3);
            Ui.Label(page, $"Buy {r.name}?", x + 10, y + 9, Palette.Gold1, order: Ui.Overlay);
            Ui.Image(page, PixelArt.Box(28, 28, Palette.Ink0, Palette.Ink4), x + 10, y + 26, Ui.Overlay - 2);
            var icon = PixelArt.ItemIcon(ConsumableKind(r.def), ConsumableTint(r.def));
            Ui.Image(page, icon, x + 10 + (28 - (int)icon.rect.width) / 2, y + 26 + (28 - (int)icon.rect.height) / 2, Ui.Overlay - 1);
            Ui.Label(page, PixelFont.Fit(r.name, 110, FontKind.DotGothic), x + 44, y + 27, Palette.Mist1, order: Ui.Overlay);
            Ui.Label(page, $"YOU OWN {r.owned} · STACKS", x + 44, y + 42, Palette.Mist2, order: Ui.Overlay, font: FontKind.Interface);

            var dmenuGo = new GameObject("Dialog");
            dmenuGo.transform.SetParent(page, false);
            var dmenu = dmenuGo.AddComponent<Menu>();
            dmenu.onBack = () => { dialogOpen = false; Rebuild(); };
            Ui.Label(page, "QTY", x + 168, y + 33, Palette.Mist3, order: Ui.Overlay, font: FontKind.Interface);
            var minus = Ui.Button(page, "-", x + 188, y + 27, 16, () => { qty--; Rebuild(); }, 18, interactable: qty > 1, showCursor: false);
            Ui.Label(page, qty.ToString(), x + 218, y + 31, Palette.Mist1, Align.Center, order: Ui.Overlay);
            var plus = Ui.Button(page, "+", x + 232, y + 27, 16, () => { qty++; Rebuild(); }, 18, interactable: qty < maxQty, showCursor: false);
            foreach (var b in new[] { minus, plus }) SetOverlay(b);

            Ui.Image(page, PixelArt.Badge(w - 20, 30, Palette.Ink0, Palette.Ink4), x + 10, y + 60, Ui.Overlay - 2);
            Ui.Label(page, "TOTAL", x + 16, y + 66, Palette.Mist2, order: Ui.Overlay, font: FontKind.Interface);
            Ui.Label(page, $"{Money(total)} HUB GOLD", x + w - 16, y + 66, Palette.Gold1, Align.Right, order: Ui.Overlay, font: FontKind.Interface);
            Ui.Label(page, "HUB GOLD AFTER", x + 16, y + 78, Palette.Mist2, order: Ui.Overlay, font: FontKind.Interface);
            Ui.Label(page, $"{Money(Gold)} › {Money(Gold - total)}", x + w - 16, y + 78, Palette.Mist1, Align.Right, order: Ui.Overlay, font: FontKind.Interface);

            Ui.Rect(page, x + 2, y + h - 32, w - 4, 30, Palette.Ink1, Ui.Overlay - 2);
            Ui.Rect(page, x + 2, y + h - 32, w - 4, 1, Palette.Ink4, Ui.Overlay - 2);
            var cancel = Ui.Button(page, "Cancel", x + w - 176, y + h - 26, 60, () => { dialogOpen = false; Rebuild(); }, 20, showCursor: false);
            var confirm = Ui.Button(page, $"Buy for {Money(total)}", x + w - 110, y + h - 26, 100, () => Buy(r.id, qty, r.name), 20, showCursor: false, style: ButtonStyle.Primary);
            confirm.onAdjust = d => { qty = Mathf.Clamp(qty + d, 1, maxQty); Rebuild(); };
            foreach (var b in new[] { cancel, confirm }) SetOverlay(b);
            dmenu.Add(minus); dmenu.Add(plus); dmenu.Add(cancel); dmenu.Add(confirm);
            dialogMenu = dmenu;
        }

        static void SetOverlay(Selectable b)
        {
            foreach (var sr in b.GetComponentsInChildren<SpriteRenderer>(true)) sr.sortingOrder += Ui.Overlay;
        }

        void Buy(string id, int count, string name)
        {
            dialogOpen = false;
            if (HubEconomy.TryBuyHubConsumable(Run, id, count, out string err)) Say($"BOUGHT {count} X {name}.", false);
            else Say(err, true);
        }

        void Sell(string id, int value)
        {
            if (Run == null || Bag.GetConsumableCount(id) <= 0) { Say("NOTHING TO SELL.", true); return; }
            Bag.RemoveConsumable(id, 1);
            Run.Items.Remove(id);
            Run.Gold += value;
            Say($"SOLD 1 FOR {value} HUB GOLD.", false);
        }
    }

    // ===================================================================== H2 · Armory (equipment)
    public class ArmoryScreen : ServiceScreen
    {
        static ShopInventory stock;
        static int stockAct = -1;
        int heroIndex, selected;
        int filter; // 0 = All, then slot groups below
        static readonly string[] FilterNames = { "All", "Main Hand", "Second Hand", "Head", "Body", "Leg", "Foot", "Accessory" };

        protected override void Init()
        {
            int act = Run?.Act ?? 1;
            if (stock == null || stockAct != act) { stock = EquipmentShopManager.GenerateShop(act, itemCount: 8); stockAct = act; }
            heroIndex = Mathf.Clamp(PartyScreen.SelectedIndex, 0, Mathf.Max(0, Party.Count - 1));
        }

        static bool InFilter(EquipmentDef d, int f) => f switch
        {
            0 => true,
            1 => d.slot == EquipmentSlot.MainHand,
            2 => d.slot == EquipmentSlot.SecondHand,
            3 => d.slot == EquipmentSlot.HeadArmor,
            4 => d.slot == EquipmentSlot.BodyArmor,
            5 => d.slot == EquipmentSlot.LegArmor,
            6 => d.slot == EquipmentSlot.FootArmor,
            _ => d.slot == EquipmentSlot.Accessory1 || d.slot == EquipmentSlot.Accessory2,
        };

        protected override void BuildPage()
        {
            var party = Party;
            var hero = party[Mathf.Clamp(heroIndex, 0, party.Count - 1)];
            int x = Header("Armory");
            x += Ui.Tag(page, "EQUIPMENT · PAYS IN HUB GOLD", x, 13, TagKind.Keep, 13) + 12;
            Ui.Label(page, "SHOPPING FOR", x, 15, Palette.Mist2, font: FontKind.Interface);
            x += PixelFont.Measure("SHOPPING FOR") + 6;
            Ui.Badge(page, "Q", x, 13, 12, 12, Palette.Mist2, Palette.Hex(0x8790AD), Palette.Ink1, font: FontKind.Interface);
            x += 15;
            for (int i = 0; i < party.Count; i++)
            {
                int idx = i;
                var p = Ui.Button(page, "", x, 9, 17, () => { heroIndex = idx; Rebuild(); }, 17, showCursor: false, style: ButtonStyle.Card);
                p.selected = i == heroIndex;
                p.visual(false);
                p.onAdjust = d => { heroIndex = (heroIndex + d + party.Count) % party.Count; Rebuild(); };
                Portrait(p.transform, party[i], 0, 0, 17, i == heroIndex);
                menu.Add(p);
                x += 20;
            }
            Ui.Badge(page, "E", x + 1, 13, 12, 12, Palette.Mist2, Palette.Hex(0x8790AD), Palette.Ink1, font: FontKind.Interface);
            BagCount(GoldTag());

            // Left panel: slot filter chips, then the stock list
            int lw = 419;
            Ui.Panel(page, Left, BodyY, lw, BodyBottom - BodyY);
            int fx = Left + 6;
            for (int i = 0; i < FilterNames.Length; i++)
            {
                int f = i;
                bool on = filter == i;
                int w = PixelFont.Measure(FilterNames[i].ToUpperInvariant()) + 10;
                var chip = Ui.Button(page, "", fx, BodyY + 6, w, () => { filter = f; selected = 0; Rebuild(); }, 14, showCursor: false);
                var box = chip.GetComponentInChildren<SpriteRenderer>();
                var lbl = Ui.Label(chip.transform, FilterNames[i].ToUpperInvariant(), 5, 4, on ? Palette.Gold1 : Palette.Mist2, order: Ui.Widget + 1, font: FontKind.Interface);
                chip.visual = focus => { box.sprite = PixelArt.Badge(w, 14, on || focus ? Palette.Ink3 : Palette.Ink2, on ? Palette.Gold3 : focus ? Palette.Mist3 : Palette.Ink4); };
                chip.visual(false);
                menu.Add(chip);
                fx += w + 2;
            }
            Ui.Rect(page, Left + 2, BodyY + 25, lw - 4, 1, Palette.Ink4, Ui.Panels + 2);

            var items = stock.items.Where(s => s.item?.Definition != null && InFilter(s.item.Definition, filter)).ToList();
            selected = Mathf.Clamp(selected, 0, Mathf.Max(0, items.Count - 1));
            int y = BodyY + 31;
            for (int i = 0; i < items.Count && y < BodyBottom - 28; i++)
            {
                int idx = i;
                var s = items[i];
                var def = s.item.Definition;
                string why = s.isSold ? "SOLD" : EquipmentValidator.Validate(def, def.slot, hero.classDef, hero.race, hero.level, hero.FinalScores, hero.id);
                bool usable = why == null;
                var row = Row(Left + 18, y, lw - 26, 28, i == selected, () => { if (selected != idx) { selected = idx; Rebuild(); } }, locked: !usable && !s.isSold);
                var t = row.transform;
                Color rc = EquipmentRarities.GetColor(s.item.rarity);
                Ui.ItemSlot(t, 4, 3, 22, def, def.slot, false, Ui.Widget + 1);
                Ui.Label(t, PixelFont.Fit(def.name, 150, FontKind.DotGothic), 32, 3, usable ? rc : Color.Lerp(rc, Palette.Mist3, 0.5f), order: Ui.Widget + 2);
                Ui.Label(t, $"{s.item.rarity} · EQUIP LV {s.item.level}".ToUpperInvariant(), 32, 17, Palette.Mist3, order: Ui.Widget + 2, font: FontKind.Interface);
                string use = s.isSold ? "SOLD OUT" : usable ? $"✓ USABLE BY {hero.name}".ToUpperInvariant() : PixelFont.Fit(why.ToUpperInvariant(), 120);
                Ui.Label(t, PixelFont.Fit(use, 124), 216, 10, s.isSold ? Palette.Mist3 : usable ? Ok : No, order: Ui.Widget + 2, font: FontKind.Interface);
                Ui.Label(t, s.isSold ? "-" : Money(s.cost), lw - 34, 8, usable && Gold >= s.cost ? Palette.Gold1 : Palette.Mist3, Align.Right, order: Ui.Widget + 2);
                y += 30;
            }
            if (items.Count == 0) Ui.Label(page, "NOTHING IN STOCK FOR THIS SLOT.", Left + lw / 2, BodyY + 80, Palette.Mist3, Align.Center, font: FontKind.Interface);

            BuildDetail(items.Count > 0 ? items[selected] : null, hero);
        }

        void BuildDetail(ShopItem s, Unit hero)
        {
            int dx = 438, dw = RightEdge - dx, x = dx + 8, w = dw - 16;
            Ui.Panel(page, dx, BodyY, dw, BodyBottom - BodyY, null, Palette.Gold3);
            if (s == null) { Feedback(x, BodyBottom - 64, w); return; }
            var item = s.item;
            var def = item.Definition;
            Ui.ItemSlot(page, x, BodyY + 7, 26, def, def.slot);
            Ui.Label(page, PixelFont.Fit(def.name, w - 34, FontKind.DotGothic), x + 32, BodyY + 7, EquipmentRarities.GetColor(item.rarity));
            int cx = x + 32;
            cx += RarityChip(cx, BodyY + 21, item.rarity) + 3;
            Ui.Tag(page, PixelFont.Fit(EquipmentSlots.DisplayName(def.slot).ToUpperInvariant(), x + w - cx - 8), cx, BodyY + 21);
            Ui.Rect(page, dx + 2, BodyY + 38, dw - 4, 1, Palette.Ink4, Ui.Panels + 2);

            int y = BodyY + 44;
            Ui.Label(page, "BASE STATS · FIXED", x, y, Palette.Gold2, font: FontKind.Interface);
            y += 11;
            if (item.EffectiveWeaponPower > 0) y = StatRow(x, y, w, "WEAPON POWER", $"+{item.EffectiveWeaponPower}");
            if (item.EffectivePhysicalDefense > 0) y = StatRow(x, y, w, "DEFENSE", $"+{item.EffectivePhysicalDefense}");
            if (item.EffectiveMagicDefense > 0) y = StatRow(x, y, w, "MAGIC DEFENSE", $"+{item.EffectiveMagicDefense}");
            if (item.EffectiveBreakPowerModifier > 0) y = StatRow(x, y, w, "BREAK POWER", $"+{item.EffectiveBreakPowerModifier}");
            y += 4;
            int affixCount = item.affixes?.Count ?? 0;
            Ui.Label(page, "RANDOM AFFIXES", x, y, Palette.Gold2, font: FontKind.Interface);
            Ui.Label(page, $"{affixCount} / 3", x + w, y, Palette.Mist3, Align.Right, font: FontKind.Interface);
            y += 11;
            foreach (var a in (item.affixes ?? new List<GeneratedAffix>()).Take(3))
                y = StatRow(x, y, w, PixelFont.Fit(a.GetDescription().ToUpperInvariant(), w), "");
            y += 4;
            string why = EquipmentValidator.Validate(def, def.slot, hero.classDef, hero.race, hero.level, hero.FinalScores, hero.id);
            Ui.Label(page, PixelFont.Wrap(why == null ? $"✓ {hero.name} CAN USE THIS ({hero.classDef?.name})".ToUpperInvariant() : why.ToUpperInvariant(), w), x, y, why == null ? Ok : No, font: FontKind.Interface);
            y += 22;
            var stat = def.slot == EquipmentSlot.MainHand || def.slot == EquipmentSlot.SecondHand
                ? new[] { DerivedStatId.PhysicalPower, DerivedStatId.RangedPower, DerivedStatId.MagicPower }.OrderByDescending(st => hero.Derived.Get(st)).First()
                : DerivedStatId.PhysicalDefense;
            int delta = why == null ? EquipmentManager.CalculateStatDelta(hero, def.slot, item, stat) : 0;
            hero.equipped.TryGetValue(def.slot, out var cur);
            if (why == null)
                Ui.Label(page, PixelFont.Fit($"VS {(cur != null ? cur.name : "EMPTY SLOT")}: {Database.DerivedStatDefs[stat].shortName} {(delta > 0 ? "+" : "")}{delta}".ToUpperInvariant(), w), x, y, delta > 0 ? Up : delta < 0 ? No : Palette.Mist3, font: FontKind.Interface);

            Feedback(x, BodyBottom - 76, w);
            Ui.Rect(page, dx + 2, BodyY + BodyBottom - BodyY - 46, dw - 4, 1, Palette.Ink4, Ui.Panels + 2);
            Ui.Label(page, $"HUB GOLD {Money(Gold)} › {Money(Gold - s.cost)} · GOES TO THE BAG", x, BodyBottom - 40, Palette.Mist2, font: FontKind.Interface);
            menu.Add(Ui.Button(page, s.isSold ? "Sold out" : $"Buy · {Money(s.cost)}", x, BodyBottom - 27, w, () => Buy(s), 20, interactable: !s.isSold && Gold >= s.cost, showCursor: false, style: ButtonStyle.Primary));
        }

        void Buy(ShopItem s)
        {
            if (HubEconomy.TryBuyHubEquipment(Run, stock, s.shopItemId, out var bought, out string err))
                Say($"BOUGHT {bought.Definition?.name}. IT'S IN THE BAG.", false);
            else Say(err, true);
        }
    }

    // ===================================================================== I · Blacksmith
    public class BlacksmithScreen : ServiceScreen
    {
        int filter, selected;
        bool confirming;
        static readonly string[] Filters = { "All", "Weapon", "Armor", "Acc." };

        static bool InFilter(EquipmentDef d, int f) => f switch
        {
            1 => d.slot == EquipmentSlot.MainHand || d.slot == EquipmentSlot.SecondHand,
            2 => d.slot == EquipmentSlot.HeadArmor || d.slot == EquipmentSlot.BodyArmor || d.slot == EquipmentSlot.LegArmor || d.slot == EquipmentSlot.FootArmor,
            3 => d.slot == EquipmentSlot.Accessory1 || d.slot == EquipmentSlot.Accessory2,
            _ => true,
        };

        string Owner(EquipmentInstance e)
        {
            var u = Party.FirstOrDefault(p => p.id == e.boundCharacterId);
            string where = e.Definition != null ? EquipmentSlots.DisplayName(e.Definition.slot) : "";
            return (e.isEquipped && u != null ? $"{u.name} · {where}" : $"BAG · {where}").ToUpperInvariant();
        }

        static string Name(EquipmentInstance e) => e.upgradeLevel > 0 ? $"{e.Definition?.name} +{e.upgradeLevel}" : e.Definition?.name ?? "Equipment";

        protected override void BuildPage()
        {
            int x = Header("Blacksmith");
            Ui.Label(page, "UPGRADES RAISE BASE STATS ONLY.", x, 15, Palette.Mist2, font: FontKind.Interface);
            GoldTag();

            var gear = Bag.equipment.Where(e => e.Definition != null && InFilter(e.Definition, filter))
                .OrderByDescending(e => e.isEquipped).ThenByDescending(e => e.rarity).ThenBy(e => e.Definition.name).ToList();
            selected = Mathf.Clamp(selected, 0, Mathf.Max(0, gear.Count - 1));

            // Left: choose equipment
            int lw = 165;
            Ui.Panel(page, Left, BodyY, lw, BodyBottom - BodyY);
            Ui.Header(page, "Choose equipment", Left + 8, BodyY + 6, lw - 4);
            int sx = Left + 6;
            for (int i = 0; i < Filters.Length; i++)
            {
                int f = i;
                int w = PixelFont.Measure(Filters[i].ToUpperInvariant()) + 12;
                bool on = filter == i;
                var seg = Ui.Button(page, "", sx, BodyY + 24, w, () => { filter = f; selected = 0; confirming = false; Rebuild(); }, 14, showCursor: false);
                var box = seg.GetComponentInChildren<SpriteRenderer>();
                Ui.Label(seg.transform, Filters[i].ToUpperInvariant(), 6, 4, on ? Palette.Hex(0x1D1404) : Palette.Mist2, order: Ui.Widget + 1, font: FontKind.Interface);
                seg.visual = focus => box.sprite = PixelArt.Badge(w, 14, on ? Palette.Gold2 : focus ? Palette.Ink3 : Palette.Ink1, Palette.Ink5);
                seg.visual(false);
                menu.Add(seg);
                sx += w - 1;
            }
            int y = BodyY + 44;
            for (int i = 0; i < gear.Count && y < BodyBottom - 26; i++)
            {
                int idx = i;
                var e = gear[i];
                var row = Row(Left + 16, y, lw - 22, 25, i == selected, () => { if (selected != idx) { selected = idx; confirming = false; Rebuild(); } });
                var t = row.transform;
                Ui.ItemSlot(t, 3, 2, 20, e.Definition, e.Definition.slot, false, Ui.Widget + 1);
                Ui.Label(t, PixelFont.Fit(Name(e), lw - 72, FontKind.DotGothic), 27, 1, EquipmentRarities.GetColor(e.rarity), order: Ui.Widget + 2);
                Ui.Label(t, PixelFont.Fit(Owner(e), lw - 72), 27, 15, Palette.Mist3, order: Ui.Widget + 2, font: FontKind.Interface);
                Ui.Label(t, $"+{e.upgradeLevel}", lw - 28, 9, Palette.Mist2, Align.Right, order: Ui.Widget + 2, font: FontKind.Interface);
                y += 27;
            }
            if (gear.Count == 0) Ui.Label(page, PixelFont.Wrap("NO EQUIPMENT TO UPGRADE.", lw - 20), Left + 10, BodyY + 60, Palette.Mist3, font: FontKind.Interface);

            var sel = gear.Count > 0 ? gear[selected] : null;
            BuildAnvil(sel);
            BuildCost(sel);
        }

        // Middle (gold): anvil preview — big slot, +n › +n+1, base stats before/after, what upgrades never change.
        void BuildAnvil(EquipmentInstance e)
        {
            int ax = Left + 165 + 7, aw = 628 - 170 - 7 - ax;
            Ui.Panel(page, ax, BodyY, aw, BodyBottom - BodyY, null, Palette.Gold3);
            if (e == null) return;
            var def = e.Definition;
            Ui.Image(page, PixelArt.Disc(34, Color.white), ax + 4, BodyY + 4, Ui.Panels + 1).color = new Color(0.94f, 0.54f, 0.24f, 0.10f);
            Ui.ItemSlot(page, ax + 10, BodyY + 9, 48, def, def.slot);
            Ui.Label(page, PixelFont.Fit(def.name, aw - 80, FontKind.DotGothic), ax + 66, BodyY + 12, EquipmentRarities.GetColor(e.rarity));
            bool canUp = e.CanUpgrade(out string maxReason);
            Ui.Label(page, $"+{e.upgradeLevel}", ax + 66, BodyY + 30, Palette.Mist3, scale: 1);
            Ui.Label(page, "›", ax + 66 + 22, BodyY + 30, Palette.Gold2);
            Ui.Label(page, canUp ? $"+{e.upgradeLevel + 1}" : "MAX", ax + 66 + 34, BodyY + 30, Palette.Gold1);
            RarityChip(ax + 66 + 64, BodyY + 31, e.rarity);
            Ui.Rect(page, ax + 2, BodyY + 64, aw - 4, 1, Palette.Ink4, Ui.Panels + 2);

            int x = ax + 10, w = aw - 20, y = BodyY + 71;
            Ui.Label(page, "BASE STATS · UPGRADED", x, y, Palette.Gold2, font: FontKind.Interface);
            y += 12;
            float now = e.UpgradeMultiplier, next = 1f + (e.upgradeLevel + 1) * 0.05f;
            void Line(string n, int baseVal)
            {
                if (baseVal <= 0) return;
                int a = Mathf.RoundToInt(baseVal * now), b = canUp ? Mathf.RoundToInt(baseVal * next) : a;
                Ui.Label(page, n, x, y + 3, Palette.Mist2, font: FontKind.Interface);
                Ui.Label(page, a.ToString(), x + w - 70, y + 3, Palette.Mist3, Align.Right, font: FontKind.Interface);
                Ui.Label(page, "›", x + w - 60, y + 3, Palette.Mist3, font: FontKind.Interface);
                Ui.Label(page, b.ToString(), x + w - 30, y + 2, Palette.Mist1, Align.Right);
                if (b > a) Ui.Label(page, $"+{b - a}", x + w, y + 3, Up, Align.Right, font: FontKind.Interface);
                Ui.Rect(page, x, y + 15, w, 1, Palette.Ink3, Ui.Panels + 2);
                y += 18;
            }
            Line("WEAPON POWER", def.weaponPower);
            Line("DEFENSE", def.physicalDefense);
            Line("MAGIC DEFENSE", def.magicDefense);
            Line("BREAK POWER", def.breakPowerModifier);

            y += 8;
            var notes = (e.affixes ?? new List<GeneratedAffix>()).Select(a => "AFFIX · " + a.GetDescription()).ToList();
            if (e.SpecialEffect != null) notes.Add($"{e.rarity} · {e.SpecialEffect.name} — {e.SpecialEffect.description}");
            if (notes.Count == 0) notes.Add("NO AFFIXES OR SPECIAL EFFECTS.");
            string body = PixelFont.Wrap(string.Join("\n", notes).ToUpperInvariant(), w - 12);
            int h = 22 + (body.Count(c => c == '\n') + 1) * PixelFont.LineHeight;
            h = Mathf.Min(h, BodyBottom - 8 - y);
            Ui.Image(page, PixelArt.DashedBadge(w, h, Color.clear, Palette.Ink5), x, y, Ui.Panels + 2);
            Ui.Label(page, "NOT CHANGED BY UPGRADES", x + 6, y + 6, Palette.Mist3, font: FontKind.Interface);
            Ui.Tag(page, "UNCHANGED", x + w - PixelFont.Measure("UNCHANGED") - 14, y + 4);
            Ui.Label(page, body, x + 6, y + 19, Palette.Mist2, font: FontKind.Interface);
            if (!canUp) Ui.Label(page, maxReason.ToUpperInvariant(), x, BodyBottom - 14, Palette.Mist3, font: FontKind.Interface);
        }

        // Right: cost and the confirm card.
        void BuildCost(EquipmentInstance e)
        {
            int cx = 628 - 170, cw = 170, x = cx + 8, w = cw - 16;
            Ui.Panel(page, cx, BodyY, cw, BodyBottom - BodyY);
            Ui.Header(page, "Upgrade cost", cx + 8, BodyY + 6, cw - 4);
            if (e == null) { Feedback(x, BodyY + 30, w); return; }
            int cost = e.GetUpgradeCostGold();
            bool canUp = e.CanUpgrade(out _), afford = Gold >= cost;
            int y = BodyY + 26;
            Ui.Rect(page, x, y, w, 1, Palette.Ink4, Ui.Panels + 2);
            y += 6;
            Ui.Label(page, "HUB GOLD", x, y + 3, Palette.Mist1, font: FontKind.Interface);
            Ui.Label(page, canUp ? $"{Money(cost)} {(afford ? "✓" : "X")}" : "-", x + w, y + 2, afford ? Palette.Gold1 : Palette.Err, Align.Right);
            y += 22;
            Feedback(x, y, w);

            int by = BodyBottom - 84;
            Ui.Image(page, PixelArt.Box(w, 76, Palette.Ink2, Palette.Gold3), x, by, Ui.Panels + 2);
            Ui.Label(page, canUp ? $"Upgrade to +{e.upgradeLevel + 1}?" : "Fully upgraded", x + 6, by + 6, Palette.Gold1);
            string text = canUp ? $"SPENDS {Money(cost)} HUB GOLD. HUB GOLD AFTER: {Money(Gold - cost)}" : "THIS ITEM IS AT ITS MAXIMUM UPGRADE LEVEL.";
            Ui.Label(page, PixelFont.Wrap(text, w - 12), x + 6, by + 22, Palette.Mist2, font: FontKind.Interface);
            int bw = (w - 16) / 2;
            menu.Add(Ui.Button(page, "Cancel", x + 6, by + 52, bw, () => { selected = 0; feedback = null; Rebuild(); }, 18, showCursor: false));
            menu.Add(Ui.Button(page, "Upgrade", x + 10 + bw, by + 52, bw, () => Upgrade(e), 18, interactable: canUp && afford, showCursor: false, style: ButtonStyle.Primary));
        }

        void Upgrade(EquipmentInstance e)
        {
            if (HubEconomy.TryBlacksmithUpgrade(Run, e, out string err)) Say($"UPGRADED {e.Definition?.name} TO +{e.upgradeLevel}.", false);
            else Say(err, true);
        }
    }

    // ===================================================================== V1 · Temple (Revive & Recovery)
    public class TempleScreen : ServiceScreen
    {
        readonly HashSet<Unit> revive = new HashSet<Unit>();
        readonly HashSet<Unit> recover = new HashSet<Unit>();

        protected override void BuildBackground()
        {
            Backdrop.Create(Root, true, 0);
            Ui.Rect(Root, 0, 0, Ui.Width, Ui.Height, Palette.Hex(0x05070F, 168), Ui.Back + 20);
        }

        int Total => revive.Count * HubEconomy.TempleReviveCost + recover.Count * HubEconomy.TempleRecoveryCost;

        protected override void BuildPage()
        {
            int x = Header("Temple");
            Ui.Label(page, "THE PARTY ISN'T HEALED AUTOMATICALLY ON RETURN — CHOOSE WHO TO TREAT", x, 15, Palette.Mist2, font: FontKind.Interface);
            GoldTag();

            var party = Party;
            int lw = 419;
            Ui.Panel(page, Left, BodyY, lw, BodyBottom - BodyY);
            Ui.Header(page, "Party", Left + 8, BodyY + 6, lw - 4);
            Ui.Label(page, "REVIVE FIRST, THEN RECOVERY", Left + lw - 8, BodyY + 8, Palette.Mist2, Align.Right, font: FontKind.Interface);

            int y = BodyY + 25;
            foreach (var u in party)
            {
                var unit = u;
                bool queued = revive.Contains(u) || recover.Contains(u);
                bool full = u.Alive && u.hp >= u.stats.maxHp && u.mp >= u.stats.maxMp;
                Ui.Image(page, queued ? PixelArt.Box(lw - 16, 58, Palette.Ink2, Palette.Gold3) : PixelArt.Box(lw - 16, 58, Palette.Ink2, Palette.Ink5), Left + 8, y, Ui.Panels + 2);
                Portrait(page, u, Left + 14, y + 16, 26, false, !u.Alive);
                Ui.Label(page, (u.classDef?.name ?? "").Substring(0, Math.Min(3, (u.classDef?.name ?? "").Length)).ToUpperInvariant(), Left + 15, y + 36, Palette.ForClass(u.classDef?.id), order: Ui.Widget + 3, font: FontKind.Interface);

                int nx = Left + 48;
                Ui.Label(page, PixelFont.Fit(u.name, 90, FontKind.DotGothic), nx, y + 8, Palette.Mist1);
                int tagX = nx + PixelFont.Measure(PixelFont.Fit(u.name, 90, FontKind.DotGothic), FontKind.DotGothic) + 6;
                if (!u.Alive)
                {
                    int w = PixelFont.Measure("DEAD") + 8;
                    Ui.Image(page, PixelArt.Badge(w, 11, Palette.Hex(0x3A1418), Palette.Err), tagX, y + 9, Ui.Widget);
                    Ui.Label(page, "DEAD", tagX + 4, y + 11, Palette.Hex(0xFFD2CC), order: Ui.Widget + 1, font: FontKind.Interface);
                }
                else Ui.Tag(page, full ? "FULL" : "HURT", tagX, y + 9);
                float hp = (float)u.hp / Mathf.Max(1, u.stats.maxHp), mp = (float)u.mp / Mathf.Max(1, u.stats.maxMp);
                Bar(nx, y + 25, "HP", hp, hp < 0.25f ? Palette.HpLow : Palette.Hp, u.hp.ToString());
                Bar(nx, y + 37, "MP", mp, Palette.Mp, u.mp.ToString());

                int bx = Left + lw - 8 - 2 * 92;
                Toggle(bx, y + 19, "Revive", u.Alive ? "—" : Money(HubEconomy.TempleReviveCost), revive.Contains(u), !u.Alive, () =>
                {
                    if (!revive.Remove(unit)) revive.Add(unit); else recover.Remove(unit);
                    Rebuild();
                });
                bool canRecover = (u.Alive && !full) || revive.Contains(u);
                Toggle(bx + 92, y + 19, "Recovery", Money(HubEconomy.TempleRecoveryCost), recover.Contains(u), canRecover, () =>
                {
                    if (!recover.Remove(unit)) recover.Add(unit);
                    Rebuild();
                });
                y += 62;
            }
            Ui.Label(page, PixelFont.Wrap("RECOVERY: FULL HP AND MP, AND CLEARS DEBUFFS AND NEGATIVE STATUSES. LIVING CHARACTERS ONLY.", lw - 20), Left + 10, y + 2, Palette.Mist2, font: FontKind.Interface);

            BuildBill(party);
        }

        void Bar(int x, int y, string label, float ratio, Color fill, string value)
        {
            Ui.Label(page, label, x, y, Palette.Mist3, font: FontKind.Interface);
            Ui.Image(page, PixelArt.GaugeBar(110, 5, Mathf.Clamp01(ratio), fill), x + 14, y + 1, Ui.Widget);
            Ui.Label(page, value, x + 150, y, Palette.Mist1, Align.Right, font: FontKind.Interface);
        }

        // Service toggle (.btn.sm, .is-sel when queued): label left, price right.
        void Toggle(int x, int y, string label, string price, bool on, bool enabled, Action toggle)
        {
            const int w = 88, h = 20;
            var b = Ui.Button(page, "", x, y, w, toggle, h, interactable: enabled, showCursor: false);
            var box = b.GetComponentInChildren<SpriteRenderer>();
            var l = Ui.Label(b.transform, label, 6, 5, Palette.Mist1, order: Ui.Widget + 1);
            var p = Ui.Label(b.transform, price, w - 6, 6, Palette.Mist1, Align.Right, order: Ui.Widget + 1, font: FontKind.Interface);
            b.visual = focus =>
            {
                box.sprite = !enabled ? PixelArt.ButtonBox(w, h, false, false) : on ? PixelArt.Badge(w, h, Palette.Ink4, Palette.Gold2) : PixelArt.ButtonBox(w, h, focus, true);
                Color c = !enabled ? Palette.Mist3 : on || focus ? Palette.Gold1 : Palette.Mist1;
                l.Color = c;
                p.Color = c;
            };
            b.visual(false);
            if (enabled) menu.Add(b);
        }

        void BuildBill(List<Unit> party)
        {
            int dx = 438, dw = RightEdge - dx, x = dx + 8, w = dw - 16;
            Ui.Panel(page, dx, BodyY, dw, BodyBottom - BodyY, null, Palette.Gold3);
            Ui.Header(page, "This visit", dx + 8, BodyY + 6, dw - 4);
            int y = BodyY + 26;
            foreach (var u in party.Where(revive.Contains)) y = StatRow(x, y, w, $"REVIVE · {u.name}".ToUpperInvariant(), Money(HubEconomy.TempleReviveCost), Palette.Gold2);
            foreach (var u in party.Where(recover.Contains)) y = StatRow(x, y, w, $"RECOVERY · {u.name}".ToUpperInvariant(), Money(HubEconomy.TempleRecoveryCost), Palette.Gold2);
            if (revive.Count + recover.Count == 0) { Ui.Label(page, "NOTHING SELECTED YET.", x, y + 2, Palette.Mist3, font: FontKind.Interface); y += 15; }
            y += 4;
            Ui.Label(page, "Total", x, y + 2, Palette.Mist1);
            Ui.Label(page, Money(Total), x + w, y + 2, Palette.Gold2, Align.Right);
            Ui.Rect(page, x, y + 15, w, 1, Palette.Gold3, Ui.Panels + 2);
            y += 22;
            Ui.Label(page, $"HUB GOLD {Money(Gold)} › {Money(Gold - Total)}", x, y, Total > Gold ? Palette.Err : Palette.Mist2, font: FontKind.Interface);
            y += 16;
            Ui.Label(page, "REVIVE PRICE", x, y, Palette.Mist3, font: FontKind.Interface);
            Ui.Label(page, $"{Money(HubEconomy.TempleReviveCost)} EACH · RETURNS AT 50% HP", x + w, y, Palette.Mist2, Align.Right, font: FontKind.Interface);
            y += 16;
            Feedback(x, y, w);

            Ui.Rect(page, dx + 2, BodyBottom - 34, dw - 4, 1, Palette.Ink4, Ui.Panels + 2);
            int cw = (w - 6) / 3;
            menu.Add(Ui.Button(page, "Clear", x, BodyBottom - 27, cw, () => { revive.Clear(); recover.Clear(); feedback = null; Rebuild(); }, 20, interactable: Total > 0, showCursor: false));
            menu.Add(Ui.Button(page, $"Pay {Money(Total)}", x + cw + 6, BodyBottom - 27, w - cw - 6, Pay, 20, interactable: Total > 0 && Total <= Gold, showCursor: false, style: ButtonStyle.Primary));
        }

        void Pay()
        {
            var errors = new List<string>();
            foreach (var u in revive.ToList()) if (!HubEconomy.TryTempleRevive(Run, u, out var e)) errors.Add(e);
            foreach (var u in recover.ToList()) if (!HubEconomy.TryTempleRecovery(Run, u, out var e)) errors.Add(e);
            revive.Clear();
            recover.Clear();
            if (Run != null) SaveManager.AutoSave(out _);
            Say(errors.Count == 0 ? "THE TEMPLE'S WORK IS DONE." : errors[0], errors.Count > 0);
        }
    }

    // ===================================================================== V2 · Magic Shop (relics)
    public class MagicShopScreen : ServiceScreen
    {
        List<RelicDef> stock;
        int selected;

        protected override void Init()
        {
            int seed = (Run?.Seed ?? 1) * 31 + (Run?.Act ?? 1);
            var rng = new System.Random(seed);
            stock = Database.Layer1Relics.Values.Where(r => r.buyPrice > 0).OrderBy(_ => rng.Next()).Take(6).ToList();
        }

        static List<string> Owned => Run?.Relics ?? new List<string>();

        protected override void BuildPage()
        {
            int x = Header("Magic Shop");
            x += Ui.Tag(page, "RELICS · PAYS IN HUB GOLD", x, 13, TagKind.Keep, 13) + 4;
            Ui.Tag(page, "PRICES ARE PLACEHOLDERS · TBD", x, 13, TagKind.Cond, 13);
            GoldTag();

            selected = Mathf.Clamp(selected, 0, Mathf.Max(0, stock.Count - 1));
            int lw = 419;
            int tx = Left;
            tx += Tab("Relics", tx, BodyY, true, () => { }).size.x + 2;
            Tab("Rare & Epic items", tx, BodyY, false, null, enabled: false);
            int py = BodyY + 19, ph = 192;
            Ui.Panel(page, Left, py, lw, ph);
            Ui.Label(page, "RELIC", Left + 56, py + 6, Palette.Mist3, font: FontKind.Interface);
            Ui.Label(page, "CATEGORY", Left + 262, py + 6, Palette.Mist3, font: FontKind.Interface);
            Ui.Label(page, "PRICE", Left + lw - 12, py + 6, Palette.Mist3, Align.Right, font: FontKind.Interface);
            int y = py + 17;
            for (int i = 0; i < stock.Count; i++)
            {
                int idx = i;
                var r = stock[i];
                bool owned = Owned.Contains(r.id), afford = Gold >= r.buyPrice;
                var row = Row(Left + 18, y, lw - 26, 27, i == selected, () => { if (selected != idx) { selected = idx; Rebuild(); } });
                var t = row.transform;
                Ui.Relic(t, 4, 4, 19, r, Ui.Widget + 1);
                Color rc = Ui.RelicColor(r);
                Ui.Label(t, PixelFont.Fit(r.name, 170, FontKind.DotGothic), 30, 2, owned ? Color.Lerp(rc, Palette.Ink2, 0.5f) : rc, order: Ui.Widget + 2);
                Ui.Label(t, PixelFont.Fit((r.gameplayEffect ?? r.description ?? "").ToUpperInvariant(), 190), 30, 16, Palette.Mist3, order: Ui.Widget + 2, font: FontKind.Interface);
                Ui.Label(t, $"{r.proposedRarity} · {r.category}".ToUpperInvariant(), 226, 10, Palette.Mist2, order: Ui.Widget + 2, font: FontKind.Interface);
                string price = owned ? "OWNED" : Money(r.buyPrice);
                Ui.Label(t, price, lw - 34, owned || !afford ? 3 : 8, owned ? Palette.Mist3 : afford ? Palette.Gold1 : Palette.Err, Align.Right, order: Ui.Widget + 2);
                if (owned) Ui.Label(t, "NO DUPLICATES", lw - 34, 17, Palette.Mist3, Align.Right, order: Ui.Widget + 2, font: FontKind.Interface);
                else if (!afford) Ui.Label(t, $"NEED {Money(r.buyPrice - Gold)} MORE", lw - 34, 17, Palette.Err, Align.Right, order: Ui.Widget + 2, font: FontKind.Interface);
                y += 28;
            }

            if (stock.Count > 0) BuildSelected(stock[selected], py + ph + 8);
            BuildOwned();
        }

        void BuildSelected(RelicDef r, int y)
        {
            int lw = 419, h = BodyBottom - y;
            Ui.Panel(page, Left, y, lw, h, null, Palette.Gold3);
            Ui.Relic(page, Left + 10, y + 10, 30, r);
            Color rc = Ui.RelicColor(r);
            int nx = Left + 48;
            Ui.Label(page, PixelFont.Fit(r.name, 140, FontKind.DotGothic), nx, y + 9, rc);
            int cx = nx + PixelFont.Measure(PixelFont.Fit(r.name, 140, FontKind.DotGothic), FontKind.DotGothic) + 6;
            cx += Ui.Tag(page, r.proposedRarity.ToString(), cx, y + 10, TagKind.Cls) + 3;
            Ui.Tag(page, r.category.ToString(), cx, y + 10);
            Ui.Label(page, PixelFont.Wrap((r.gameplayEffect ?? r.description ?? "").ToUpperInvariant(), 190), nx, y + 26, Palette.Mist1, font: FontKind.Interface);
            Ui.Label(page, "PASSIVE · WHOLE PARTY · PERMANENT ONCE BOUGHT", nx, y + h - 16, Palette.Mist2, font: FontKind.Interface);

            int rx = Left + lw - 128, rw = 118;
            int count = Owned.Count;
            bool owned = Owned.Contains(r.id), full = count >= RelicManager.MaxRelicCapacity;
            int ry = StatRow(rx, y + 8, rw, "RELIC SLOTS", owned ? $"{count} / 5" : $"{count} / 5 › {Math.Min(5, count + 1)} / 5");
            ry = StatRow(rx, ry, rw, "HUB GOLD", $"{Money(Gold)} › {Money(Gold - r.buyPrice)}", Palette.Gold2);
            menu.Add(Ui.Button(page, owned ? "Owned" : $"Buy · {Money(r.buyPrice)}", rx, ry + 4, rw, () => Buy(r), 20, interactable: !owned && !full && Gold >= r.buyPrice, showCursor: false, style: ButtonStyle.Primary));
        }

        void BuildOwned()
        {
            int dx = 438, dw = RightEdge - dx, x = dx + 8, w = dw - 16;
            Ui.Panel(page, dx, BodyY, dw, BodyBottom - BodyY);
            Ui.Header(page, "Your relics", dx + 8, BodyY + 6, dw - 4);
            Ui.Label(page, $"{Owned.Count} / 5", dx + dw - 8, BodyY + 7, Palette.Gold1, Align.Right);
            int y = BodyY + 25;
            for (int i = 0; i < RelicManager.MaxRelicCapacity; i++)
            {
                if (i < Owned.Count && Database.Relics.TryGetValue(Owned[i], out var r))
                {
                    Ui.Image(page, PixelArt.Badge(w, 28, Palette.Ink1, Palette.Ink4), x, y, Ui.Panels + 2);
                    Ui.Relic(page, x + 4, y + 4, 20, r);
                    Ui.Label(page, PixelFont.Fit(r.name, w - 92, FontKind.DotGothic), x + 30, y + 3, Ui.RelicColor(r));
                    Ui.Label(page, $"{r.proposedRarity} · {r.category}".ToUpperInvariant(), x + 30, y + 17, Palette.Mist3, font: FontKind.Interface);
                    bool canSell = r.sellPrice > 0 && r.bonus.IsZero && r.abilityBonus.IsZero;
                    var relic = r;
                    menu.Add(Ui.Button(page, canSell ? $"Sell {Money(r.sellPrice)}" : "Sell", x + w - 58, y + 5, 54, () => Sell(relic), 18, interactable: canSell, showCursor: false));
                }
                else
                {
                    Ui.Image(page, PixelArt.DashedBadge(w, 28, Color.clear, Palette.Ink5), x, y, Ui.Panels + 2);
                    Ui.Relic(page, x + 4, y + 4, 20, null);
                    Ui.Label(page, "Empty slot", x + 30, y + 3, Palette.Mist3);
                    Ui.Label(page, "BUY A RELIC OR FIND ONE IN A DUNGEON", x + 30, y + 17, Palette.Mist3, font: FontKind.Interface);
                }
                y += 31;
            }
            Ui.Label(page, PixelFont.Wrap("RELICS CAN ONLY BE SOLD HERE, AT ANY TIME. SELLING FREES ONE SLOT. NO DUPLICATES: A RELIC YOU OWN IS GREYED OUT IN STOCK.", w), x, y + 2, Palette.Mist2, font: FontKind.Interface);
            y += 44;

            var sel = stock.Count > 0 ? stock[selected] : null;
            if (Owned.Count >= RelicManager.MaxRelicCapacity) { Toast(x, y, w, "RELIC SLOTS ARE FULL. SELL A RELIC BEFORE BUYING.", false, "5"); y += 30; }
            if (sel != null && !Owned.Contains(sel.id) && Gold < sel.buyPrice) { Toast(x, y, w, $"{sel.name}: NEED {Money(sel.buyPrice - Gold)} MORE HUB GOLD.", true); y += 30; }
            Feedback(x, y, w);
        }

        void Buy(RelicDef r)
        {
            if (Run == null) return;
            if (Owned.Contains(r.id)) { Say("YOU ALREADY OWN THIS RELIC.", true); return; }
            if (Run.RelicManager.IsFull) { Say("RELIC SLOTS ARE FULL.", true); return; }
            if (Run.Gold < r.buyPrice) { Say($"NEED {Money(r.buyPrice - Run.Gold)} MORE HUB GOLD.", true); return; }
            if (!Run.TryAddRelic(r.id, out string err)) { Say(err, true); return; }
            Run.Gold -= r.buyPrice;
            Say($"BOUGHT {r.name}. IT NOW EMPOWERS THE WHOLE PARTY.", false);
        }

        void Sell(RelicDef r)
        {
            if (Run == null) return;
            Run.RelicManager.RemoveRelic(r.id);
            Run.Relics.Remove(r.id);
            Run.Gold += r.sellPrice;
            Say($"SOLD {r.name} FOR {Money(r.sellPrice)} HUB GOLD.", false);
        }
    }
}
