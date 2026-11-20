using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using UiLabel = UnityEngine.UIElements.Label;

namespace Obscure
{
    // Shared UITK base for Hub service screens (06 Shop, 07 Armory, 08 Blacksmith, 09 Temple, 10 Magic Shop).
    public abstract class ServiceScreen : GameScreen
    {
        protected static readonly Color Ok = Palette.Hex(0x9AF0BF), No = Palette.Hex(0xFF9A8C), Up = Palette.Hex(0x7FF0AD);

        protected UitkMenu menu;
        protected string feedback;
        protected bool feedbackBad;
        protected int focusIndex;
        protected bool rebuilding;

        protected static int Gold => Run != null ? Run.HubGold : 0;
        protected static InventoryRuntime Bag => Run?.Inventory ?? new InventoryRuntime();
        protected static List<Unit> Party => Run?.Party ?? Database.Party.Select(id => new Unit(Database.Characters[id])).ToList();

        protected abstract string FigmaScreenKey { get; }

        protected override void Build()
        {
            MountFigma(FigmaScreenKey);
            Init();
            Rebuild();
        }

        protected virtual void Init() { }
        protected abstract void BuildPage();
        protected virtual void AfterRebuild() { }

        protected void ResetMenu()
        {
            FigmaUi.DestroyMenu(ref menu);
            menu = Root.gameObject.AddComponent<UitkMenu>();
            menu.AttachRoot(UiRoot);
            menu.onBack = () => Game?.Go(GameState.Hub);
            menu.onFocus = item =>
            {
                if (rebuilding || item == null) return;
                focusIndex = menu.items.IndexOf(item);
            };
        }

        protected void Rebuild()
        {
            if (Root == null || rebuilding) return;
            rebuilding = true;
            try
            {
                // Re-clone fresh layout on state changes or rebuild dynamic hosts
                UiRoot = FigmaUi.Mount(Root.gameObject, FigmaScreenKey);
                FigmaUi.HideInstancesWithText(UiRoot, FigmaUi.TitleRuleTags);
                ResetMenu();
                BuildPage();
                if (menu.items.Count > 0)
                    menu.Focus(Mathf.Clamp(focusIndex, 0, menu.items.Count - 1), false, true);
                AfterRebuild();
            }
            finally { rebuilding = false; }
        }

        protected virtual Sound SuccessSound => Sound.Success;

        protected void Say(string text, bool bad)
        {
            feedback = text;
            feedbackBad = bad;
            Sfx.Play(bad ? Sound.Deny : SuccessSound);
            Rebuild();
        }

        protected void BindBackButton()
        {
            var back = FigmaUi.FindButton(UiRoot, "Back");
            if (back != null)
                FigmaUi.ApplyDefaultFocusVisual(menu.Add(back, "Back", () => Game?.Go(GameState.Hub)), back);
        }

        protected void BindHubGold()
        {
            var hubTag = UiRoot.Q<VisualElement>(className: "currency-tag-hub");
            if (hubTag != null)
            {
                FigmaUi.SetInstanceText(hubTag, "text-0", $"{Money(Gold)} GOLD");
                FigmaUi.AnchorRight(hubTag, 24f);
            }
        }

        protected static void HideElementsByClass(VisualElement root, params string[] classes) => FigmaUi.HideByClass(root, classes);

        protected VisualElement CreateListRow(string title, string sub, string mid, string right, string subRight, Color? titleCol, Color? rightCol, bool chosen, bool locked, Action onSelect, Texture2D icon = null)
        {
            var row = new VisualElement();
            row.style.height = 56;
            row.style.marginBottom = 6;
            row.style.paddingLeft = 12;
            row.style.paddingRight = 12;
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = UnityEngine.UIElements.Align.Center;
            row.style.borderTopWidth = 2;
            row.style.borderBottomWidth = 2;
            row.style.borderLeftWidth = 2;
            row.style.borderRightWidth = 2;

            var ptr = new UiLabel(chosen ? "▸" : "") { style = { width = 18, fontSize = 18, color = Palette.Gold2 } };
            row.Add(ptr);
            var iconEl = IconArt.Inline(icon, 3);
            if (iconEl != null) { if (locked) iconEl.style.opacity = 0.5f; row.Add(iconEl); }

            // The item name stays on one line: once measured it becomes the column's min width, so a long
            // middle note wraps instead of squeezing the name. The sub line (often a description) still wraps.
            var mainCol = new VisualElement { style = { flexGrow = 1, flexDirection = FlexDirection.Column, justifyContent = Justify.Center, overflow = Overflow.Hidden } };
            var titleLbl = new UiLabel(title) { style = { fontSize = 18, color = titleCol ?? Palette.Mist1, whiteSpace = WhiteSpace.NoWrap } };
            mainCol.Add(titleLbl);
            FigmaUi.OnFirstLayout(titleLbl, () => mainCol.style.minWidth = Mathf.Min(360f,
                titleLbl.MeasureTextSize(titleLbl.text, 0, VisualElement.MeasureMode.Undefined, 0, VisualElement.MeasureMode.Undefined).x));
            if (!string.IsNullOrEmpty(sub))
                mainCol.Add(new UiLabel(sub) { style = { fontSize = 12, color = Palette.Mist3 } });
            row.Add(mainCol);

            if (!string.IsNullOrEmpty(mid))
            {
                row.Add(new UiLabel(mid)
                {
                    style = { fontSize = 13, color = locked ? No : Palette.Mist2, marginLeft = 10, marginRight = 12, minWidth = 110, unityTextAlign = TextAnchor.MiddleRight }
                });
            }

            if (!string.IsNullOrEmpty(right) || !string.IsNullOrEmpty(subRight))
            {
                var rCol = new VisualElement { style = { flexDirection = FlexDirection.Column, alignItems = UnityEngine.UIElements.Align.FlexEnd, minWidth = 86 } };
                if (!string.IsNullOrEmpty(right))
                    rCol.Add(new UiLabel(right) { style = { fontSize = 16, color = rightCol ?? Palette.Gold1 } });
                if (!string.IsNullOrEmpty(subRight))
                    rCol.Add(new UiLabel(subRight) { style = { fontSize = 11, color = Palette.Err } });
                row.Add(rCol);
            }

            var item = menu.Add(row, title, onSelect, interactable: true);
            item.selected = chosen;
            item.visual = on =>
            {
                row.style.backgroundColor = on || chosen ? Palette.Hex(0x2B2210) : locked ? Palette.Ink1 : Palette.Ink2;
                var border = on ? Palette.Gold1 : chosen ? Palette.Gold2 : Palette.Ink5;
                row.style.borderTopColor = border;
                row.style.borderBottomColor = border;
                row.style.borderLeftColor = border;
                row.style.borderRightColor = border;
                ptr.text = on || chosen ? "▸" : "";
            };
            item.visual(false);
            return row;
        }

        protected static VisualElement StatLine(string label, string value, Color? valueColor = null)
        {
            var r = new VisualElement
            {
                style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween, alignItems = UnityEngine.UIElements.Align.Center, height = 28, borderBottomWidth = 2, borderBottomColor = Palette.Ink3 }
            };
            r.Add(new UiLabel(label) { style = { fontSize = 13, color = Palette.Mist2 } });
            r.Add(new UiLabel(value) { style = { fontSize = 13, color = valueColor ?? Palette.Mist1 } });
            return r;
        }

        protected static VisualElement CreateToast(string message, bool bad, string icon = null)
        {
            var toast = FigmaUi.InstantiateTemplate(bad ? "Toast-Error" : "Toast-Info") ?? new VisualElement();
            toast.style.width = Length.Percent(100);
            toast.style.marginTop = 8;
            FigmaUi.SetInstanceText(toast, "text-0", icon ?? (bad ? "!" : "✓"));
            FigmaUi.SetInstanceText(toast, "text-1", (message ?? "").ToUpperInvariant());
            return toast;
        }

        protected VisualElement CreateFeedbackToast()
            => string.IsNullOrEmpty(feedback) ? null : CreateToast(feedback, feedbackBad, feedbackBad ? "!" : "✓");

        protected static VisualElement RarityTag(EquipmentRarity r)
        {
            Color c = EquipmentRarities.GetColor(r);
            var tag = FigmaUi.InstantiateTemplate("Tag-Cls") ?? new VisualElement();
            FigmaUi.SetInstanceText(tag, "text-0", $"{Palette.RarityBadge(r)} · {r.ToString().ToUpperInvariant()}");
            var lbl = tag.Q<UiLabel>("text-0");
            if (lbl != null) lbl.style.color = c;
            tag.style.marginRight = 6;
            return tag;
        }

        protected static VisualElement CreateTabRow(float left, float top, (string label, bool active, bool enabled, Action onClick)[] tabs, UitkMenu m)
        {
            var row = new VisualElement { style = { position = Position.Absolute, left = left, top = top, height = 38, flexDirection = FlexDirection.Row } };
            foreach (var (lbl, active, enabled, onClick) in tabs)
            {
                var btn = FigmaUi.CreateButton(lbl, "button-slab", null);
                btn.style.height = 38;
                btn.style.paddingLeft = 16;
                btn.style.paddingRight = 16;
                btn.style.marginRight = 4;
                btn.style.fontSize = 18;
                btn.style.borderTopWidth = active ? 3 : 2;
                btn.style.borderBottomWidth = active ? 0 : 2;
                row.Add(btn);
                if (enabled)
                {
                    var item = m.Add(btn, lbl, onClick, selected: active);
                    item.visual = on =>
                    {
                        btn.style.backgroundColor = on || active ? Palette.Ink3 : Palette.Ink2;
                        btn.style.borderTopColor = on ? Palette.Gold1 : active ? Palette.Gold2 : Palette.Ink4;
                        var side = on ? Palette.Gold1 : active ? Palette.Gold3 : Palette.Ink4;
                        btn.style.borderLeftColor = side;
                        btn.style.borderRightColor = side;
                        btn.style.borderBottomColor = active ? Palette.Ink3 : side;
                        btn.style.color = on || active ? Palette.Gold1 : Palette.Mist2;
                    };
                    item.visual(false);
                }
                else
                {
                    btn.SetEnabled(false);
                    btn.style.opacity = 0.55f;
                }
            }
            return row;
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
        int dialogFocusIndex = -1;
        UitkMenu dialogMenu;

        protected override string FigmaScreenKey => "06---Shop---Consumables";
        protected override Sound SuccessSound => Sound.Coin;
        protected override void Init() => catalog = HubEconomy.GenerateHubShopCatalog(Run);

        // Buy limit ignoring gold: shop stock and the carry cap (Draught/Ether 10).
        int Limit(string id) => sellTab ? int.MaxValue : HubEconomy.HubBuyLimit(Run, id);

        string LimitReason(string id) =>
            HubEconomy.HubStockLeft(Run, id) <= 0 ? "SOLD OUT · RESTOCKS AFTER THE NEXT DUNGEON"
            : $"CARRY LIMIT {Database.Items[id].maxStack} REACHED";

        protected override void AfterRebuild()
        {
            if (dialogMenu != null && dialogMenu.items.Count > 0)
            {
                int target = dialogFocusIndex >= 0 ? Mathf.Clamp(dialogFocusIndex, 0, dialogMenu.items.Count - 1) : dialogMenu.items.Count - 1;
                dialogMenu.Focus(target, false, true);
            }
        }

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
            BindBackButton();
            BindHubGold();
            FigmaUi.DestroyMenu(ref dialogMenu);

            var armoryBtn = FigmaUi.FindButton(UiRoot, "Armory ›");
            if (armoryBtn != null)
                FigmaUi.ApplyDefaultFocusVisual(menu.Add(armoryBtn, "Armory", () => Game?.Go(GameState.Armory)), armoryBtn);

            var bagLbl = UiRoot.Q<UiLabel>(className: "bag-0-100-vars");
            // Consumables don't use the 100 equipment slots (they cap per item), so count what this shop sells.
            if (bagLbl != null) bagLbl.text = $"BAG {Bag.consumables.Sum(s => s.count)} ITEMS";

            // Hide baked static rows, headers & right detail content
            var screen = UiRoot.Q<VisualElement>(className: "_06-shop-consumables") ?? UiRoot;
            var tv = UiRoot.Q<TabView>();
            if (tv != null) tv.style.display = DisplayStyle.None;
            HideElementsByClass(UiRoot, "list-row__i", "list-row-2__i", "list-row-2__i-2",
                "consumable-vars", "need-20-more-vars", "consumable-vars-2", "need-25-more-vars", "consumable-vars-3", "need-50-more-vars");
            var detailPanel = UiRoot.Q<VisualElement>(className: "panel-default__i-13");
            if (detailPanel != null)
            {
                int idx = screen.IndexOf(detailPanel);
                for (int i = idx + 1; i < screen.childCount; i++)
                {
                    var ch = screen[i];
                    if (ch.name == "DynamicHost") continue;
                    ch.style.display = DisplayStyle.None;
                }
            }

            screen.Add(CreateTabRow(24, 68, new (string, bool, bool, Action)[]
            {
                ("Buy", !sellTab, true, () => { sellTab = false; selected = 0; feedback = null; Rebuild(); }),
                ("Sell", sellTab, true, () => { sellTab = true; selected = 0; feedback = null; Rebuild(); }),
            }, menu));

            var rows = Rows();
            selected = Mathf.Clamp(selected, 0, Mathf.Max(0, rows.Count - 1));

            var listHost = FigmaUi.CreateVerticalScrollView();
            listHost.style.position = Position.Absolute;
            listHost.style.left = 44;
            listHost.style.top = 148;
            listHost.style.width = 798;
            listHost.style.height = 530;
            screen.Add(listHost);

            for (int i = 0; i < rows.Count; i++)
            {
                int idx = i;
                var r = rows[i];
                bool canBuy = sellTab || Limit(r.id) > 0;
                bool afford = sellTab || (Gold >= r.price && canBuy);
                Color rc = r.def != null ? EquipmentRarities.GetColor(r.def.rarity) : Palette.Mist1;
                // Type column; stock and owned counts live in the detail panel.
                string mid = (string.IsNullOrEmpty(r.def?.category) ? "CONSUMABLE" : r.def.category).ToUpperInvariant();
                string subRight = !canBuy ? LimitReason(r.id) : Gold < r.price ? $"NEED {Money(r.price - Gold)} MORE" : null;
                var row = CreateListRow(r.name, r.sub.ToUpperInvariant(), mid, Money(r.price), subRight, rc, afford ? Palette.Gold1 : Palette.Err, i == selected, false, () =>
                {
                    if (selected != idx) { selected = idx; Rebuild(); }
                    else if (!sellTab) { qty = 1; dialogFocusIndex = -1; dialogOpen = true; Rebuild(); }
                    else { Sell(r.id, r.price); }
                }, IconArt.Item(r.id));
                listHost.Add(row);
            }

            if (rows.Count == 0)
                listHost.Add(new UiLabel(sellTab ? "NOTHING TO SELL." : "SOLD OUT.") { style = { fontSize = 15, color = Palette.Mist3, alignSelf = UnityEngine.UIElements.Align.Center, marginTop = 80 } });

            BuildDetail(screen, rows);
            if (dialogOpen && !sellTab && rows.Count > 0) BuildDialog(rows[selected]);
        }

        void BuildDetail(VisualElement screen, List<(string id, string name, string sub, ItemDef def, int price, int owned)> rows)
        {
            var host = new VisualElement { name = "DynamicHost", style = { position = Position.Absolute, left = 894, top = 84, width = 346, height = 600, flexDirection = FlexDirection.Column } };
            screen.Add(host);

            if (rows.Count == 0)
            {
                var fb = CreateFeedbackToast();
                if (fb != null) host.Add(fb);
                return;
            }

            var r = rows[selected];
            host.Add(FigmaUi.CreateSectionHeader(r.name, 346f));

            var tagRow = new VisualElement { style = { flexDirection = FlexDirection.Row, marginTop = 10, marginBottom = 10 } };
            if (r.def != null) tagRow.Add(RarityTag(r.def.rarity));
            var t2 = FigmaUi.InstantiateTemplate("Tag-Cls");
            if (t2 != null) { FigmaUi.SetInstanceText(t2, "text-0", sellTab ? "OWNED" : "CONSUMABLE"); tagRow.Add(t2); }
            host.Add(tagRow);

            var desc = new UiLabel((r.def?.description ?? r.sub).ToUpperInvariant()) { style = { fontSize = 14, color = Palette.Mist1, whiteSpace = WhiteSpace.Normal, marginBottom = 14 } };
            host.Add(desc);
            host.Add(StatLine("YOU OWN", r.def != null && r.def.maxStack < 99 ? $"{r.owned} / {r.def.maxStack}" : r.owned.ToString()));
            host.Add(StatLine(sellTab ? "SELLS FOR" : "PRICE EACH", $"{Money(r.price)} GOLD", Palette.Gold2));
            if (!sellTab && HubEconomy.HubStockLeft(Run, r.id) != int.MaxValue)
                host.Add(StatLine("IN STOCK", $"{HubEconomy.HubStockLeft(Run, r.id)} · RESTOCKS AFTER EACH DUNGEON"));
            if (r.def?.target != null) host.Add(StatLine("TARGET", r.def.target.ToUpperInvariant()));

            if (!sellTab && Limit(r.id) <= 0)
                host.Add(CreateToast($"{r.name}: {LimitReason(r.id)}.", true));
            else if (!sellTab && Gold < r.price)
                host.Add(CreateToast($"{r.name}: NOT ENOUGH GOLD — NEED {Money(r.price - Gold)} MORE.", true));

            var fbToast = CreateFeedbackToast();
            if (fbToast != null) host.Add(fbToast);

            var spacer = new VisualElement { style = { flexGrow = 1 } };
            host.Add(spacer);

            bool canAct = sellTab || (Gold >= r.price && Limit(r.id) > 0);
            var actBtn = FigmaUi.CreateButton(sellTab ? $"Sell for {Money(r.price)}" : "Buy...", canAct ? "button-primary" : "button-slab", 346f);
            actBtn.SetEnabled(canAct);
            host.Add(actBtn);
            FigmaUi.ApplyDefaultFocusVisual(menu.Add(actBtn, sellTab ? "Sell" : "Buy", sellTab ? (Action)(() => Sell(r.id, r.price)) : () => { qty = 1; dialogFocusIndex = -1; dialogOpen = true; Rebuild(); }, interactable: canAct));
        }

        void BuildDialog((string id, string name, string sub, ItemDef def, int price, int owned) r)
        {
            int maxQty = Math.Max(1, Math.Min(Math.Min(99, Gold / Math.Max(1, r.price)), Limit(r.id)));
            qty = Mathf.Clamp(qty, 1, maxQty);
            int total = qty * r.price;

            var backdrop = new VisualElement { name = "ShopQtyModal" };
            backdrop.AddToClassList("uitk-modal-backdrop");
            var card = new VisualElement();
            card.AddToClassList("uitk-modal-panel");
            card.style.borderTopColor = Palette.Gold3;
            card.style.borderBottomColor = Palette.Gold3;
            card.style.borderLeftColor = Palette.Gold3;
            card.style.borderRightColor = Palette.Gold3;

            card.Add(new UiLabel($"Buy {r.name}?") { style = { fontSize = 24, color = Palette.Gold1, marginBottom = 8 } });
            card.Add(new UiLabel($"YOU OWN {r.owned} · PRICE {Money(r.price)} GOLD EACH") { style = { fontSize = 13, color = Palette.Mist2, marginBottom = 16 } });

            var qtyRow = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = UnityEngine.UIElements.Align.Center, marginBottom = 16 } };
            qtyRow.Add(new UiLabel("QTY") { style = { fontSize = 14, color = Palette.Mist3, width = 60 } });
            var minusBtn = FigmaUi.CreateButton("-", "button-slab", 48f);
            var qtyLbl = new UiLabel(qty.ToString()) { style = { fontSize = 22, color = Palette.Mist1, width = 56, unityTextAlign = TextAnchor.MiddleCenter } };
            var plusBtn = FigmaUi.CreateButton("+", "button-slab", 48f);
            qtyRow.Add(minusBtn);
            qtyRow.Add(qtyLbl);
            qtyRow.Add(plusBtn);
            card.Add(qtyRow);

            var summary = new VisualElement { style = { backgroundColor = Palette.Ink0, paddingLeft = 12, paddingRight = 12, paddingTop = 8, paddingBottom = 8, marginBottom = 16, borderTopWidth = 1, borderBottomWidth = 1, borderLeftWidth = 1, borderRightWidth = 1, borderTopColor = Palette.Ink4, borderBottomColor = Palette.Ink4, borderLeftColor = Palette.Ink4, borderRightColor = Palette.Ink4 } };
            summary.Add(StatLine("TOTAL", $"{Money(total)} GOLD", Palette.Gold1));
            summary.Add(StatLine("GOLD AFTER", $"{Money(Gold)} › {Money(Gold - total)}"));
            card.Add(summary);

            var btnRow = new VisualElement { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.FlexEnd } };
            var cancelBtn = FigmaUi.CreateButton("Cancel", "button-slab", 140f);
            cancelBtn.style.marginRight = 12;
            var confirmBtn = FigmaUi.CreateButton($"Buy for {Money(total)}", "button-primary", 200f);
            btnRow.Add(cancelBtn);
            btnRow.Add(confirmBtn);
            card.Add(btnRow);
            backdrop.Add(card);
            UiRoot.Add(backdrop);

            dialogMenu = Root.gameObject.AddComponent<UitkMenu>();
            dialogMenu.AttachRoot(backdrop);
            dialogMenu.onFocus = item => { if (!rebuilding && item != null) dialogFocusIndex = dialogMenu.items.IndexOf(item); };
            dialogMenu.onBack = () => { dialogOpen = false; dialogFocusIndex = -1; Rebuild(); };

            FigmaUi.ApplyDefaultFocusVisual(dialogMenu.Add(minusBtn, "-", () => { qty--; Rebuild(); }, interactable: qty > 1));
            FigmaUi.ApplyDefaultFocusVisual(dialogMenu.Add(plusBtn, "+", () => { qty++; Rebuild(); }, interactable: qty < maxQty));
            FigmaUi.ApplyDefaultFocusVisual(dialogMenu.Add(cancelBtn, "Cancel", () => { dialogOpen = false; dialogFocusIndex = -1; Rebuild(); }));
            var cItem = dialogMenu.Add(confirmBtn, "Buy", () => Buy(r.id, qty, r.name), onAdjust: d => { qty = Mathf.Clamp(qty + d, 1, maxQty); Rebuild(); });
            FigmaUi.ApplyDefaultFocusVisual(cItem);
        }

        void Buy(string id, int count, string name)
        {
            dialogOpen = false;
            dialogFocusIndex = -1;
            bool ok = HubEconomy.TryBuyHubConsumable(Run, id, count, out string err);
            catalog = HubEconomy.GenerateHubShopCatalog(Run); // refresh stock shown in the list
            if (ok) Say($"BOUGHT {count} X {name}.", false);
            else Say(err, true);
        }

        void Sell(string id, int value)
        {
            if (Run == null || Bag.GetConsumableCount(id) <= 0) { Say("NOTHING TO SELL.", true); return; }
            Bag.RemoveConsumable(id, 1);
            Run.Items.Remove(id);
            Run.HubGold += value;
            Say($"SOLD 1 FOR {value} GOLD.", false);
        }
    }

    // ===================================================================== H2 · Armory (equipment)
    public class ArmoryScreen : ServiceScreen
    {
        protected override Sound SuccessSound => Sound.Coin;
        static ShopInventory stock;
        static int stockAct = -1;
        static int stockSeed = int.MinValue;
        int heroIndex, selected;
        int filter;
        static readonly string[] FilterNames = { "All", "Main Hand", "Second Hand", "Head", "Body", "Leg", "Foot", "Accessory" };

        protected override string FigmaScreenKey => "07---Armory---Equipment";

        protected override void Init()
        {
            int act = Run?.Act ?? 1;
            int seed = Run?.Seed ?? 0;
            if (stock == null || stockAct != act || stockSeed != seed)
            {
                stock = EquipmentShopManager.GenerateShop(act, itemCount: 8);
                stockAct = act;
                stockSeed = seed;
            }
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
            BindBackButton();
            BindHubGold();
            var party = Party;
            menu.onPrevPage = () => { if (party.Count > 0) { heroIndex = (heroIndex - 1 + party.Count) % party.Count; Rebuild(); } };
            menu.onNextPage = () => { if (party.Count > 0) { heroIndex = (heroIndex + 1) % party.Count; Rebuild(); } };
            var hero = party[Mathf.Clamp(heroIndex, 0, party.Count - 1)];

            var screen = UiRoot.Q<VisualElement>(className: "_07-armory-equipment") ?? UiRoot;
            var tv = UiRoot.Q<TabView>();
            if (tv != null) tv.style.display = DisplayStyle.None;

            // Hero portrait switcher
            string[] frameClasses = { "frame-vars", "frame-vars-2", "frame-vars-3", "frame-vars-4" };
            for (int i = 0; i < frameClasses.Length; i++)
            {
                var fr = UiRoot.Q<VisualElement>(className: frameClasses[i]);
                if (fr == null) continue;
                if (i >= party.Count) { fr.style.display = DisplayStyle.None; continue; }
                int idx = i;
                var u = party[i];
                if (fr.childCount > 0 && u.sprite != null && u.sprite.Length > 0)
                    FigmaUi.SetBackgroundContain(fr[0], FigmaUi.GetSpriteTexture(u.sprite, "armory_" + (u.classDef?.id ?? u.name)));
                bool onHero = i == heroIndex;
                var it = menu.Add(fr, u.name, () => { heroIndex = idx; Rebuild(); }, selected: onHero, onAdjust: d => { heroIndex = (heroIndex + d + party.Count) % party.Count; Rebuild(); });
                it.visual = focus =>
                {
                    var col = focus ? Palette.Gold1 : onHero ? Palette.Gold2 : Palette.Ink5;
                    fr.style.borderTopColor = col; fr.style.borderBottomColor = col; fr.style.borderLeftColor = col; fr.style.borderRightColor = col;
                };
                it.visual(false);
            }

            // Hide baked static stock list and right panel detail nodes
            HideElementsByClass(UiRoot, "list-row-2__i-3", "list-row-2__i-4", "list-row__i-2", "list-row-2__i-5", "list-row-2__i-6", "list-row-2__i-7", "tag-cond");
            var leftPanel = UiRoot.Q<VisualElement>(className: "panel-default__i-14");
            if (leftPanel != null)
            {
                int lIdx = screen.IndexOf(leftPanel);
                var rightPanel = UiRoot.Q<VisualElement>(className: "panel-gold__i-4");
                int rIdx = rightPanel != null ? screen.IndexOf(rightPanel) : screen.childCount;
                for (int i = lIdx + 1; i < screen.childCount; i++)
                    if (i != rIdx) screen[i].style.display = DisplayStyle.None;
            }

            // Filter chip strip
            var chipRow = new VisualElement { style = { position = Position.Absolute, left = 44, top = 120, flexDirection = FlexDirection.Row, flexWrap = Wrap.Wrap, width = 800 } };
            for (int i = 0; i < FilterNames.Length; i++)
            {
                int f = i;
                bool active = filter == i;
                var tag = FigmaUi.InstantiateTemplate(active ? "Tag-Gold" : "Tag-Cls");
                if (tag == null) continue;
                FigmaUi.SetInstanceText(tag, "text-0", FilterNames[i].ToUpperInvariant());
                tag.style.marginRight = 6;
                chipRow.Add(tag);
                var it = menu.Add(tag, FilterNames[i], () => { filter = f; selected = 0; Rebuild(); }, selected: active);
                it.visual = on => tag.style.opacity = on || active ? 1f : 0.72f;
            }
            screen.Add(chipRow);

            var items = stock.items.Where(s => s.item?.Definition != null && InFilter(s.item.Definition, filter)).ToList();
            selected = Mathf.Clamp(selected, 0, Mathf.Max(0, items.Count - 1));

            var listHost = FigmaUi.CreateVerticalScrollView();
            listHost.style.position = Position.Absolute;
            listHost.style.left = 44;
            listHost.style.top = 162;
            listHost.style.width = 802;
            listHost.style.height = 520;
            screen.Add(listHost);
            for (int i = 0; i < items.Count; i++)
            {
                int idx = i;
                var s = items[i];
                var def = s.item.Definition;
                string why = s.isSold ? "SOLD" : EquipmentValidator.Validate(def, def.slot, hero.classDef, hero.race, hero.level, hero.FinalScores, hero.id);
                bool usable = why == null;
                Color rc = EquipmentRarities.GetColor(s.item.rarity);
                string mid = s.isSold ? "SOLD OUT" : usable ? $"✓ {hero.name.ToUpperInvariant()}" : why.ToUpperInvariant();
                listHost.Add(CreateListRow(def.name, $"{s.item.rarity} · LV {s.item.level}".ToUpperInvariant(), mid, s.isSold ? "-" : Money(s.cost), null, usable ? rc : Color.Lerp(rc, Palette.Mist3, 0.5f), usable && Gold >= s.cost ? Palette.Gold1 : Palette.Mist3, i == selected, !usable && !s.isSold, () =>
                {
                    if (selected != idx) { selected = idx; Rebuild(); }
                }, IconArt.Gear(def)));
            }
            if (items.Count == 0)
                listHost.Add(new UiLabel("NOTHING IN STOCK FOR THIS SLOT.") { style = { fontSize = 15, color = Palette.Mist3, alignSelf = UnityEngine.UIElements.Align.Center, marginTop = 80 } });

            BuildDetail(screen, items.Count > 0 ? items[selected] : null, hero);
        }

        void BuildDetail(VisualElement screen, ShopItem s, Unit hero)
        {
            var host = new VisualElement { style = { position = Position.Absolute, left = 894, top = 84, width = 346, height = 600, flexDirection = FlexDirection.Column } };
            screen.Add(host);
            if (s == null) { var fb = CreateFeedbackToast(); if (fb != null) host.Add(fb); return; }

            var item = s.item;
            var def = item.Definition;
            host.Add(FigmaUi.CreateSectionHeader(def.name, 346f));

            var tagRow = new VisualElement { style = { flexDirection = FlexDirection.Row, marginTop = 10, marginBottom = 10 } };
            tagRow.Add(RarityTag(item.rarity));
            var slotTag = FigmaUi.InstantiateTemplate("Tag-Cls");
            if (slotTag != null) { FigmaUi.SetInstanceText(slotTag, "text-0", EquipmentSlots.DisplayName(def.slot).ToUpperInvariant()); tagRow.Add(slotTag); }
            host.Add(tagRow);

            host.Add(new UiLabel("BASE STATS · FIXED") { style = { fontSize = 12, color = Palette.Gold2, marginBottom = 4 } });
            if (item.EffectiveWeaponPower > 0) host.Add(StatLine("WEAPON POWER", $"+{item.EffectiveWeaponPower}"));
            if (item.EffectivePhysicalDefense > 0) host.Add(StatLine("DEFENSE", $"+{item.EffectivePhysicalDefense}"));
            if (item.EffectiveMagicDefense > 0) host.Add(StatLine("MAGIC DEFENSE", $"+{item.EffectiveMagicDefense}"));
            if (item.EffectiveBreakPowerModifier > 0) host.Add(StatLine("BREAK POWER", $"+{item.EffectiveBreakPowerModifier}"));

            int affixCount = item.affixes?.Count ?? 0;
            host.Add(new UiLabel($"RANDOM AFFIXES ({affixCount} / 3)") { style = { fontSize = 12, color = Palette.Gold2, marginTop = 10, marginBottom = 4 } });
            foreach (var a in (item.affixes ?? new List<GeneratedAffix>()).Take(3))
                host.Add(StatLine(a.GetDescription().ToUpperInvariant(), ""));

            string why = EquipmentValidator.Validate(def, def.slot, hero.classDef, hero.race, hero.level, hero.FinalScores, hero.id);
            host.Add(new UiLabel(why == null ? $"✓ {hero.name} CAN USE THIS".ToUpperInvariant() : why.ToUpperInvariant())
            {
                style = { fontSize = 13, color = why == null ? Ok : No, whiteSpace = WhiteSpace.Normal, marginTop = 10 }
            });

            if (why == null)
            {
                var stat = def.slot == EquipmentSlot.MainHand || def.slot == EquipmentSlot.SecondHand
                    ? new[] { DerivedStatId.PhysicalPower, DerivedStatId.RangedPower, DerivedStatId.MagicPower }.OrderByDescending(st => hero.Derived.Get(st)).First()
                    : DerivedStatId.PhysicalDefense;
                int delta = EquipmentManager.CalculateStatDelta(hero, def.slot, item, stat);
                hero.equipped.TryGetValue(def.slot, out var cur);
                host.Add(new UiLabel($"VS {(cur != null ? cur.name : "EMPTY")}: {Database.DerivedStatDefs[stat].shortName} {(delta > 0 ? "+" : "")}{delta}".ToUpperInvariant())
                {
                    style = { fontSize = 13, color = delta > 0 ? Up : delta < 0 ? No : Palette.Mist3, marginTop = 4 }
                });
            }

            var fbToast = CreateFeedbackToast();
            if (fbToast != null) host.Add(fbToast);

            host.Add(new VisualElement { style = { flexGrow = 1 } });
            host.Add(new UiLabel($"GOLD {Money(Gold)} › {Money(Gold - s.cost)}") { style = { fontSize = 12, color = Palette.Mist2, marginBottom = 8 } });
            bool canBuy = !s.isSold && Gold >= s.cost;
            var buyBtn = FigmaUi.CreateButton(s.isSold ? "Sold out" : $"Buy · {Money(s.cost)}", canBuy ? "button-primary" : "button-slab", 346f);
            buyBtn.SetEnabled(canBuy);
            host.Add(buyBtn);
            FigmaUi.ApplyDefaultFocusVisual(menu.Add(buyBtn, "Buy", () => Buy(s), interactable: canBuy));
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
        protected override Sound SuccessSound => Sound.BlacksmithForge;
        int filter, selected;
        static readonly string[] Filters = { "All", "Weapon", "Armor", "Acc." };

        protected override string FigmaScreenKey => "08---Blacksmith---Upgrade";

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
            BindBackButton();
            BindHubGold();
            HideElementsByClass(UiRoot, "upgrades-raise-base-stats-only-vars");
            var screen = UiRoot.Q<VisualElement>(className: "_08-blacksmith-upgrade") ?? UiRoot;

            var panel1 = UiRoot.Q<VisualElement>(className: "panel-default__i-15");
            if (panel1 != null)
            {
                for (int i = screen.IndexOf(panel1) + 1; i < screen.childCount; i++)
                    screen[i].style.display = DisplayStyle.None;
            }
            var p1 = UiRoot.Q<VisualElement>(className: "panel-default__i-15"); if (p1 != null) p1.style.display = DisplayStyle.Flex;
            var p2 = UiRoot.Q<VisualElement>(className: "panel-gold__i-5"); if (p2 != null) p2.style.display = DisplayStyle.Flex;
            var p3 = UiRoot.Q<VisualElement>(className: "panel-default__i-16"); if (p3 != null) p3.style.display = DisplayStyle.Flex;

            var gear = Bag.equipment.Where(e => e.Definition != null && InFilter(e.Definition, filter))
                .OrderByDescending(e => e.isEquipped).ThenByDescending(e => e.rarity).ThenBy(e => e.Definition.name).ToList();
            selected = Mathf.Clamp(selected, 0, Math.Max(0, gear.Count - 1));

            // Left panel
            var leftHost = new VisualElement { style = { position = Position.Absolute, left = 36, top = 84, width = 306, height = 600, flexDirection = FlexDirection.Column } };
            screen.Add(leftHost);
            leftHost.Add(FigmaUi.CreateSectionHeader("Choose equipment", 298f));

            var fltRow = new VisualElement { style = { flexDirection = FlexDirection.Row, marginTop = 10, marginBottom = 10 } };
            for (int i = 0; i < Filters.Length; i++)
            {
                int f = i;
                bool active = filter == i;
                var tag = FigmaUi.InstantiateTemplate(active ? "Tag-Gold" : "Tag-Cls");
                if (tag != null)
                {
                    FigmaUi.SetInstanceText(tag, "text-0", Filters[i].ToUpperInvariant());
                    tag.style.marginRight = 6;
                    fltRow.Add(tag);
                    var it = menu.Add(tag, Filters[i], () => { filter = f; selected = 0; Rebuild(); }, selected: active);
                    it.visual = on => tag.style.opacity = on || active ? 1f : 0.72f;
                }
            }
            leftHost.Add(fltRow);

            var listScroll = FigmaUi.CreateVerticalScrollView();
            listScroll.style.height = 490;
            leftHost.Add(listScroll);

            for (int i = 0; i < gear.Count; i++)
            {
                int idx = i;
                var e = gear[i];
                listScroll.Add(CreateListRow(Name(e), Owner(e), null, $"+{e.upgradeLevel}", null, EquipmentRarities.GetColor(e.rarity), Palette.Mist2, i == selected, false, () =>
                {
                    if (selected != idx) { selected = idx; Rebuild(); }
                }, IconArt.Gear(e.Definition)));
            }
            if (gear.Count == 0) listScroll.Add(new UiLabel("NO EQUIPMENT TO UPGRADE.") { style = { fontSize = 14, color = Palette.Mist3, marginTop = 24 } });

            var sel = gear.Count > 0 ? gear[selected] : null;
            BuildAnvil(screen, sel);
            BuildCost(screen, sel);
        }

        void BuildAnvil(VisualElement screen, EquipmentInstance e)
        {
            var host = new VisualElement { style = { position = Position.Absolute, left = 396, top = 96, width = 468, height = 580, flexDirection = FlexDirection.Column } };
            screen.Add(host);
            if (e == null) return;

            var def = e.Definition;
            bool canUp = e.CanUpgrade(out string maxReason);
            var top = new VisualElement { style = { alignItems = UnityEngine.UIElements.Align.Center, marginBottom = 18 } };
            var slot = FigmaUi.InstantiateTemplate("Item-Slot-Selected");
            if (slot != null)
            {
                FigmaUi.SetInstanceText(slot, "text-0", Palette.RarityBadge(e.rarity));
                slot.style.width = 92; slot.style.height = 92; slot.style.marginBottom = 12;
                IconArt.Overlay(slot, IconArt.Gear(def), 5);
                top.Add(slot);
            }
            top.Add(new UiLabel(def.name) { style = { fontSize = 24, color = EquipmentRarities.GetColor(e.rarity), marginBottom = 8 } });

            var lvlRow = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = UnityEngine.UIElements.Align.Center } };
            var curT = FigmaUi.InstantiateTemplate("Tag-Cls"); if (curT != null) { FigmaUi.SetInstanceText(curT, "text-0", $"+{e.upgradeLevel}"); lvlRow.Add(curT); }
            lvlRow.Add(new UiLabel("  ›  ") { style = { fontSize = 20, color = Palette.Gold2 } });
            var nxtT = FigmaUi.InstantiateTemplate("Tag-Gold"); if (nxtT != null) { FigmaUi.SetInstanceText(nxtT, "text-0", canUp ? $"+{e.upgradeLevel + 1}" : "MAX"); lvlRow.Add(nxtT); }
            lvlRow.Add(RarityTag(e.rarity));
            top.Add(lvlRow);
            host.Add(top);

            host.Add(new UiLabel("BASE STATS · UPGRADED") { style = { fontSize = 13, color = Palette.Gold2, marginBottom = 6 } });
            float now = e.UpgradeMultiplier, next = 1f + (e.upgradeLevel + 1) * 0.05f;
            void AddStat(string n, int baseVal)
            {
                if (baseVal <= 0) return;
                int a = Mathf.RoundToInt(baseVal * now), b = canUp ? Mathf.RoundToInt(baseVal * next) : a;
                string val = b > a ? $"{a} › {b} (+{b - a})" : a.ToString();
                host.Add(StatLine(n, val, b > a ? Up : Palette.Mist1));
            }
            AddStat("WEAPON POWER", def.weaponPower);
            AddStat("DEFENSE", def.physicalDefense);
            AddStat("MAGIC DEFENSE", def.magicDefense);
            AddStat("BREAK POWER", def.breakPowerModifier);

            var notes = (e.affixes ?? new List<GeneratedAffix>()).Select(a => "AFFIX · " + a.GetDescription()).ToList();
            if (e.SpecialEffect != null) notes.Add($"{e.rarity} · {e.SpecialEffect.name} — {e.SpecialEffect.description}");
            if (notes.Count == 0) notes.Add("NO AFFIXES OR SPECIAL EFFECTS.");
            var box = new VisualElement { style = { marginTop = 18, paddingLeft = 10, paddingRight = 10, paddingTop = 8, paddingBottom = 8, borderTopWidth = 1, borderBottomWidth = 1, borderLeftWidth = 1, borderRightWidth = 1, borderTopColor = Palette.Ink5, borderBottomColor = Palette.Ink5, borderLeftColor = Palette.Ink5, borderRightColor = Palette.Ink5 } };
            box.Add(new UiLabel("NOT CHANGED BY UPGRADES") { style = { fontSize = 12, color = Palette.Mist3, marginBottom = 4 } });
            box.Add(new UiLabel(string.Join("\n", notes).ToUpperInvariant()) { style = { fontSize = 12, color = Palette.Mist2, whiteSpace = WhiteSpace.Normal } });
            host.Add(box);
            if (!canUp) host.Add(new UiLabel(maxReason.ToUpperInvariant()) { style = { fontSize = 12, color = Palette.Mist3, marginTop = 8 } });
        }

        void BuildCost(VisualElement screen, EquipmentInstance e)
        {
            var host = new VisualElement { style = { position = Position.Absolute, left = 922, top = 84, width = 318, height = 600, flexDirection = FlexDirection.Column } };
            screen.Add(host);
            host.Add(FigmaUi.CreateSectionHeader("Upgrade cost", 318f));
            if (e == null) { var fb = CreateFeedbackToast(); if (fb != null) host.Add(fb); return; }

            int cost = e.GetUpgradeCostGold();
            bool canUp = e.CanUpgrade(out _), afford = Gold >= cost;
            var list = new VisualElement { style = { marginTop = 14 } };
            list.Add(StatLine("GOLD", canUp ? $"{Money(cost)} {(afford ? "✓" : "X")}" : "-", afford ? Palette.Gold1 : Palette.Err));
            host.Add(list);
            host.Add(CreateToast(canUp ? $"SPENDS {Money(cost)} GOLD. GOLD AFTER: {Money(Gold - cost)}" : "AT MAXIMUM UPGRADE LEVEL.", canUp && !afford, "!"));

            var fbToast = CreateFeedbackToast();
            if (fbToast != null) host.Add(fbToast);

            host.Add(new VisualElement { style = { flexGrow = 1 } });
            var btnRow = new VisualElement { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween } };
            var cancelBtn = FigmaUi.CreateButton("Cancel", "button-slab", 148f);
            var upBtn = FigmaUi.CreateButton("Upgrade", canUp && afford ? "button-primary" : "button-slab", 156f);
            upBtn.SetEnabled(canUp && afford);
            btnRow.Add(cancelBtn);
            btnRow.Add(upBtn);
            host.Add(btnRow);
            FigmaUi.ApplyDefaultFocusVisual(menu.Add(cancelBtn, "Cancel", () => Game?.Go(GameState.Hub)));
            FigmaUi.ApplyDefaultFocusVisual(menu.Add(upBtn, "Upgrade", () => Upgrade(e), interactable: canUp && afford));
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
        protected override Sound SuccessSound => Sound.TempleBless;
        readonly HashSet<Unit> revive = new HashSet<Unit>();
        readonly HashSet<Unit> recover = new HashSet<Unit>();

        protected override string FigmaScreenKey => "09---Temple---Revive---Recovery";
        int Total => revive.Count * HubEconomy.TempleReviveCost + recover.Count * HubEconomy.TempleRecoveryCost;

        protected override void BuildPage()
        {
            BindBackButton();
            BindHubGold();
            var party = Party;
            var screen = UiRoot.Q<VisualElement>(className: "_09-temple-revive-recovery") ?? UiRoot;

            // Hide baked hero rows inside panel-default__i-17 and bill children inside panel-gold
            var pLeft = UiRoot.Q<VisualElement>(className: "panel-default__i-17");
            var pRight = UiRoot.Q<VisualElement>(className: "panel-gold");
            int lIdx = pLeft != null ? screen.IndexOf(pLeft) : -1;
            int rIdx = pRight != null ? screen.IndexOf(pRight) : -1;
            if (lIdx >= 0)
            {
                for (int i = lIdx + 1; i < screen.childCount; i++)
                    if (i != rIdx) screen[i].style.display = DisplayStyle.None;
            }

            var leftHost = new VisualElement { style = { position = Position.Absolute, left = 40, top = 84, width = 588, height = 600, flexDirection = FlexDirection.Column } };
            screen.Add(leftHost);
            leftHost.Add(FigmaUi.CreateSectionHeader("Party condition", 588f));

            var heroList = new VisualElement { style = { marginTop = 12, flexDirection = FlexDirection.Column } };
            foreach (var u in party)
            {
                var unit = u;
                bool queued = revive.Contains(u) || recover.Contains(u);
                bool full = u.Alive && u.hp >= u.stats.maxHp && u.mp >= u.stats.maxMp;

                var card = new VisualElement
                {
                    style =
                    {
                        height = 104, marginBottom = 10, paddingLeft = 10, paddingRight = 10,
                        flexDirection = FlexDirection.Row, alignItems = UnityEngine.UIElements.Align.Center,
                        backgroundColor = queued ? Palette.Hex(0x2B2210) : Palette.Ink2,
                        borderTopWidth = 2, borderBottomWidth = 2, borderLeftWidth = 2, borderRightWidth = 2,
                        borderTopColor = queued ? Palette.Gold3 : Palette.Ink5, borderBottomColor = queued ? Palette.Gold3 : Palette.Ink5,
                        borderLeftColor = queued ? Palette.Gold3 : Palette.Ink5, borderRightColor = queued ? Palette.Gold3 : Palette.Ink5,
                    }
                };

                var fig = new VisualElement { style = { width = 64, height = 80, marginRight = 12, opacity = u.Alive ? 1f : 0.35f } };
                if (u.sprite != null && u.sprite.Length > 0)
                    FigmaUi.SetBackgroundContain(fig, FigmaUi.GetSpriteTexture(u.sprite, "tpl_" + (u.classDef?.id ?? u.name)));
                card.Add(fig);

                var midCol = new VisualElement { style = { flexGrow = 1, flexDirection = FlexDirection.Column } };
                var titleLine = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = UnityEngine.UIElements.Align.Center, marginBottom = 6 } };
                titleLine.Add(new UiLabel(u.name) { style = { fontSize = 20, color = Palette.Mist1, marginRight = 8 } });
                titleLine.Add(new UiLabel(u.Alive ? (full ? "FULL" : "HURT") : "DEAD") { style = { fontSize = 12, color = !u.Alive ? Palette.Err : full ? Ok : Palette.Gold1 } });
                midCol.Add(titleLine);

                float hp = u.Alive ? (float)u.hp / Mathf.Max(1, u.stats.maxHp) : 0f, mp = (float)u.mp / Mathf.Max(1, u.stats.maxMp);
                midCol.Add(new UiLabel($"HP {u.hp}/{u.stats.maxHp}  ·  MP {u.mp}/{u.stats.maxMp}") { style = { fontSize = 12, color = Palette.Mist2, marginBottom = 4 } });

                VisualElement MiniBar(float ratio, Color col)
                {
                    var track = new VisualElement { style = { width = 220, height = 5, backgroundColor = Palette.Ink0, borderTopWidth = 1, borderBottomWidth = 1, borderLeftWidth = 1, borderRightWidth = 1, borderTopColor = Palette.Ink4, borderBottomColor = Palette.Ink4, borderLeftColor = Palette.Ink4, borderRightColor = Palette.Ink4, marginBottom = 3 } };
                    track.Add(new VisualElement { style = { width = Length.Percent(Mathf.Clamp01(ratio) * 100f), height = Length.Percent(100), backgroundColor = col } });
                    return track;
                }
                midCol.Add(MiniBar(hp, hp < 0.25f ? Palette.HpLow : Palette.Hp));
                midCol.Add(MiniBar(mp, Palette.Mp));
                card.Add(midCol);

                var btnCol = new VisualElement { style = { flexDirection = FlexDirection.Row } };
                bool canRev = !u.Alive;
                var revBtn = FigmaUi.CreateButton(u.Alive ? "Revive —" : $"Revive {Money(HubEconomy.TempleReviveCost)}", revive.Contains(u) ? "button-primary" : "button-slab", 118f);
                revBtn.SetEnabled(canRev);
                revBtn.style.marginRight = 6;
                btnCol.Add(revBtn);
                FigmaUi.ApplyDefaultFocusVisual(menu.Add(revBtn, $"Revive {u.name}", () =>
                {
                    if (!revive.Remove(unit)) revive.Add(unit); else recover.Remove(unit);
                    Rebuild();
                }, interactable: canRev));

                bool canRec = (u.Alive && !full) || revive.Contains(u);
                var recBtn = FigmaUi.CreateButton($"Heal {Money(HubEconomy.TempleRecoveryCost)}", recover.Contains(u) ? "button-primary" : "button-slab", 112f);
                recBtn.SetEnabled(canRec);
                btnCol.Add(recBtn);
                FigmaUi.ApplyDefaultFocusVisual(menu.Add(recBtn, $"Recovery {u.name}", () =>
                {
                    if (!recover.Remove(unit)) recover.Add(unit);
                    Rebuild();
                }, interactable: canRec));

                card.Add(btnCol);
                heroList.Add(card);
            }
            leftHost.Add(heroList);

            // Right bill panel
            var rightHost = new VisualElement { style = { position = Position.Absolute, left = 682, top = 84, width = 556, height = 600, flexDirection = FlexDirection.Column } };
            screen.Add(rightHost);
            rightHost.Add(FigmaUi.CreateSectionHeader("This visit", 556f));

            var billLines = new VisualElement { style = { marginTop = 14, flexDirection = FlexDirection.Column } };
            foreach (var u in party.Where(revive.Contains)) billLines.Add(StatLine($"REVIVE · {u.name}".ToUpperInvariant(), Money(HubEconomy.TempleReviveCost), Palette.Gold2));
            foreach (var u in party.Where(recover.Contains)) billLines.Add(StatLine($"RECOVERY · {u.name}".ToUpperInvariant(), Money(HubEconomy.TempleRecoveryCost), Palette.Gold2));
            if (revive.Count + recover.Count == 0)
                billLines.Add(new UiLabel("NOTHING SELECTED YET.") { style = { fontSize = 14, color = Palette.Mist3, marginBottom = 8 } });
            billLines.Add(StatLine("Total", Money(Total), Palette.Gold1));
            billLines.Add(new UiLabel($"GOLD {Money(Gold)} › {Money(Gold - Total)}") { style = { fontSize = 13, color = Total > Gold ? Palette.Err : Palette.Mist2, marginTop = 10 } });
            rightHost.Add(billLines);

            var fbToast = CreateFeedbackToast();
            if (fbToast != null) rightHost.Add(fbToast);

            rightHost.Add(new VisualElement { style = { flexGrow = 1 } });
            var footRow = new VisualElement { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween } };
            var clearBtn = FigmaUi.CreateButton("Clear", "button-slab", 160f);
            clearBtn.SetEnabled(Total > 0);
            var payBtn = FigmaUi.CreateButton($"Pay {Money(Total)}", Total > 0 && Total <= Gold ? "button-primary" : "button-slab", 380f);
            payBtn.SetEnabled(Total > 0 && Total <= Gold);
            footRow.Add(clearBtn);
            footRow.Add(payBtn);
            rightHost.Add(footRow);
            FigmaUi.ApplyDefaultFocusVisual(menu.Add(clearBtn, "Clear", () => { revive.Clear(); recover.Clear(); feedback = null; Rebuild(); }, interactable: Total > 0));
            FigmaUi.ApplyDefaultFocusVisual(menu.Add(payBtn, "Pay", Pay, interactable: Total > 0 && Total <= Gold));
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
        protected override Sound SuccessSound => Sound.Coin;
        List<RelicDef> stock;
        int selected;

        protected override string FigmaScreenKey => "10---Magic-Shop---Relics";

        protected override void Init()
        {
            int seed = (Run?.Seed ?? 1) * 31 + (Run?.Act ?? 1);
            var rng = new System.Random(seed);
            stock = Database.Layer1Relics.Values.Where(r => r.buyPrice > 0).OrderBy(_ => rng.Next()).Take(6).ToList();
        }

        static List<string> Owned => Run?.Relics ?? new List<string>();

        protected override void BuildPage()
        {
            BindBackButton();
            BindHubGold();
            selected = Mathf.Clamp(selected, 0, Math.Max(0, stock.Count - 1));
            var screen = UiRoot.Q<VisualElement>(className: "_10-magic-shop-relics") ?? UiRoot;

            var tv = UiRoot.Q<TabView>(); if (tv != null) tv.style.display = DisplayStyle.None;
            var panelTop = UiRoot.Q<VisualElement>(className: "panel-default__i-18");
            if (panelTop != null)
            {
                for (int i = screen.IndexOf(panelTop) + 1; i < screen.childCount; i++)
                {
                    var ch = screen[i];
                    if (ch.ClassListContains("panel-gold__i-7") || ch.ClassListContains("panel-default__i-19")) continue;
                    ch.style.display = DisplayStyle.None;
                }
            }

            screen.Add(CreateTabRow(24, 68, new (string, bool, bool, Action)[]
            {
                ("Relics", true, true, () => { }),
                ("Rare & Epic items", false, false, null),
            }, menu));

            // Stock list
            var listHost = new VisualElement { style = { position = Position.Absolute, left = 40, top = 124, width = 806, height = 350, flexDirection = FlexDirection.Column } };
            screen.Add(listHost);
            for (int i = 0; i < stock.Count; i++)
            {
                int idx = i;
                var r = stock[i];
                bool owned = Owned.Contains(r.id), afford = Gold >= r.buyPrice;
                string price = owned ? "OWNED" : Money(r.buyPrice);
                string subRight = !owned && !afford ? $"NEED {Money(r.buyPrice - Gold)} MORE" : null;
                listHost.Add(CreateListRow(r.name, (r.gameplayEffect ?? r.description ?? "").ToUpperInvariant(), $"{r.proposedRarity} · {r.category}".ToUpperInvariant(), price, subRight, Ui.RelicColor(r), owned ? Palette.Mist3 : afford ? Palette.Gold1 : Palette.Err, i == selected, false, () =>
                {
                    if (selected != idx) { selected = idx; Rebuild(); }
                }));
            }

            // Bottom selected relic box
            if (stock.Count > 0) BuildSelected(screen, stock[selected]);
            BuildOwned(screen);
        }

        void BuildSelected(VisualElement screen, RelicDef r)
        {
            var host = new VisualElement { style = { position = Position.Absolute, left = 44, top = 522, width = 798, height = 166, flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween } };
            screen.Add(host);

            var left = new VisualElement { style = { width = 500, flexDirection = FlexDirection.Column } };
            left.Add(new UiLabel(r.name) { style = { fontSize = 24, color = Ui.RelicColor(r), marginBottom = 8 } });
            left.Add(new UiLabel($"{r.proposedRarity} · {r.category}".ToUpperInvariant()) { style = { fontSize = 12, color = Palette.Gold2, marginBottom = 8 } });
            left.Add(new UiLabel((r.gameplayEffect ?? r.description ?? "").ToUpperInvariant()) { style = { fontSize = 13, color = Palette.Mist1, whiteSpace = WhiteSpace.Normal, flexGrow = 1 } });
            host.Add(left);

            var right = new VisualElement { style = { width = 260, flexDirection = FlexDirection.Column, justifyContent = Justify.SpaceBetween } };
            int count = Owned.Count;
            bool owned = Owned.Contains(r.id), full = count >= RelicManager.MaxRelicCapacity;
            var stats = new VisualElement();
            stats.Add(StatLine("RELIC SLOTS", owned ? $"{count} / 5" : $"{count} / 5 › {Math.Min(5, count + 1)} / 5"));
            stats.Add(StatLine("GOLD", $"{Money(Gold)} › {Money(Gold - r.buyPrice)}", Palette.Gold2));
            right.Add(stats);

            bool canBuy = !owned && !full && Gold >= r.buyPrice;
            var buyBtn = FigmaUi.CreateButton(owned ? "Owned" : $"Buy · {Money(r.buyPrice)}", canBuy ? "button-primary" : "button-slab", 258f);
            buyBtn.SetEnabled(canBuy);
            right.Add(buyBtn);
            FigmaUi.ApplyDefaultFocusVisual(menu.Add(buyBtn, "Buy Relic", () => Buy(r), interactable: canBuy));
            host.Add(right);
        }

        void BuildOwned(VisualElement screen)
        {
            var host = new VisualElement { style = { position = Position.Absolute, left = 894, top = 84, width = 346, height = 600, flexDirection = FlexDirection.Column } };
            screen.Add(host);
            host.Add(FigmaUi.CreateSectionHeader($"Your relics  {Owned.Count} / 5", 346f));

            var slotsCol = new VisualElement { style = { marginTop = 12, flexDirection = FlexDirection.Column } };
            for (int i = 0; i < RelicManager.MaxRelicCapacity; i++)
            {
                var box = new VisualElement { style = { height = 56, marginBottom = 8, paddingLeft = 12, paddingRight = 10, flexDirection = FlexDirection.Row, alignItems = UnityEngine.UIElements.Align.Center, borderTopWidth = 2, borderBottomWidth = 2, borderLeftWidth = 2, borderRightWidth = 2, borderTopColor = Palette.Ink5, borderBottomColor = Palette.Ink5, borderLeftColor = Palette.Ink5, borderRightColor = Palette.Ink5, backgroundColor = Palette.Ink1 } };
                if (i < Owned.Count && Database.Relics.TryGetValue(Owned[i], out var r))
                {
                    var txt = new VisualElement { style = { flexGrow = 1, flexDirection = FlexDirection.Column } };
                    txt.Add(new UiLabel(r.name) { style = { fontSize = 16, color = Ui.RelicColor(r) } });
                    txt.Add(new UiLabel($"{r.proposedRarity} · {r.category}".ToUpperInvariant()) { style = { fontSize = 11, color = Palette.Mist3 } });
                    box.Add(txt);
                    bool canSell = r.sellPrice > 0 && r.bonus.IsZero && r.abilityBonus.IsZero;
                    var relic = r;
                    var sBtn = FigmaUi.CreateButton(canSell ? $"Sell {Money(r.sellPrice)}" : "Sell", "button-slab", 92f);
                    sBtn.SetEnabled(canSell);
                    box.Add(sBtn);
                    FigmaUi.ApplyDefaultFocusVisual(menu.Add(sBtn, $"Sell {r.name}", () => Sell(relic), interactable: canSell));
                }
                else
                {
                    var txt = new VisualElement { style = { flexDirection = FlexDirection.Column } };
                    txt.Add(new UiLabel("Empty slot") { style = { fontSize = 16, color = Palette.Mist3 } });
                    txt.Add(new UiLabel("BUY OR FIND ONE IN A DUNGEON.") { style = { fontSize = 11, color = Palette.Mist3 } });
                    box.Add(txt);
                }
                slotsCol.Add(box);
            }
            host.Add(slotsCol);
            var fbToast = CreateFeedbackToast();
            if (fbToast != null) host.Add(fbToast);
        }

        void Buy(RelicDef r)
        {
            if (Run == null) return;
            if (Owned.Contains(r.id)) { Say("YOU ALREADY OWN THIS RELIC.", true); return; }
            if (Run.RelicManager.IsFull) { Say("RELIC SLOTS ARE FULL.", true); return; }
            if (Run.HubGold < r.buyPrice) { Say($"NEED {Money(r.buyPrice - Run.HubGold)} MORE GOLD.", true); return; }
            if (!Run.TryAddRelic(r.id, out string err)) { Say(err, true); return; }
            Run.HubGold -= r.buyPrice;
            Say($"BOUGHT {r.name}. IT NOW EMPOWERS THE WHOLE PARTY.", false);
        }

        void Sell(RelicDef r)
        {
            if (Run == null) return;
            Run.RelicManager.RemoveRelic(r.id);
            Run.Relics.Remove(r.id);
            Run.HubGold += r.sellPrice;
            Say($"SOLD {r.name} FOR {Money(r.sellPrice)} GOLD.", false);
        }
    }
}
