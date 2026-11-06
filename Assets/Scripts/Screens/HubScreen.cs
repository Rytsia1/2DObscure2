using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Obscure
{
    // Hub — Starlit Reliquary "D · Hub — Geistwald City" board at 640x360:
    // night town scene, top bar (safe-zone label, city name, Auto Saved tag, Save / Load / Main Menu, Hub Gold),
    // facility menu (each facility opens its own screen), run progress panel, party strip,
    // and a tooltip describing the focused facility.
    public class HubScreen : GameScreen
    {
        Menu menu;
        SpriteRenderer tipBox;
        Label tipTitle, tipText;
        Label feedbackLabel;

        protected override void Build()
        {
            // Auto-save on Hub arrival if not already auto-saved
            if (Run != null && PersistenceCommitHandler.ActiveCommittedSnapshot == null)
            {
                PersistenceCommitHandler.SetActiveCommittedSnapshot(PersistenceCommitHandler.CreateCommittedSnapshot(Run));
                SaveManager.AutoSave(out _);
            }

            BuildScene();
            BuildTopBar();
            BuildFacilityMenu();
            BuildProgress();
            BuildPartyStrip();
        }

        // ------------------------------------------------------------- scene

        void BuildScene()
        {
            Backdrop.Create(Root, false, 14, Palette.Hex(0x070B1C), Palette.Hex(0x101A3A), Palette.Hex(0x28284F), Palette.Hex(0x3A2F55));
            var rng = new System.Random(11);
            for (int i = 0; i < 40; i++)
                Ui.Rect(Root, rng.Next(8, Ui.Width - 8), rng.Next(6, 150), 1, 1, i % 5 == 0 ? Palette.Gold1 : i % 2 == 0 ? Palette.Mist1 : Palette.Mist2, Ui.Back + 1);

            Ui.Image(Root, PixelArt.Ridge(Ui.Width, 187, Palette.Hex(0x1A2146), 3), 0, 173, Ui.Back + 2);

            // Town silhouettes (placeholder for hub art), windows and the dungeon gate
            Gable(210, 155, 75, 85, Palette.Hex(0x141B3C));
            Gable(295, 180, 60, 60, Palette.Hex(0x171F44));
            Ui.Rect(Root, 365, 160, 85, 80, Palette.Hex(0x121939), Ui.Back + 3);
            Ui.Rect(Root, 390, 125, 34, 35, Palette.Hex(0x121939), Ui.Back + 3);
            Ui.Rect(Root, 472, 98, 36, 142, Palette.Hex(0x0F1534), Ui.Back + 3);
            Ui.Rect(Root, 466, 106, 48, 134, Palette.Hex(0x0F1534), Ui.Back + 3);
            Ui.Image(Root, PixelArt.Box(20, 32, Palette.Hex(0x2A1A10), Palette.Ember2), 480, 208, Ui.Back + 4);
            Ui.Rect(Root, 235, 206, 7, 9, Palette.Gold2, Ui.Back + 4);
            Ui.Rect(Root, 320, 211, 7, 9, Palette.Gold2, Ui.Back + 4);
            Ui.Rect(Root, 390, 196, 7, 9, Palette.Ember2, Ui.Back + 4);
            Ui.Rect(Root, 0, 239, Ui.Width, 121, Palette.Hex(0x0B1024), Ui.Back + 5);

            // Vignette
            Color vign = Palette.Hex(0x05070F, 110);
            Ui.Rect(Root, 0, Ui.Height - 12, Ui.Width, 12, vign, Ui.Back + 6);
            Ui.Rect(Root, 0, 0, 12, Ui.Height, vign, Ui.Back + 6);
            Ui.Rect(Root, Ui.Width - 12, 0, 12, Ui.Height, vign, Ui.Back + 6);
        }

        // Pitched-roof house silhouette.
        void Gable(int x, int y, int w, int h, Color c)
        {
            int roof = h * 2 / 5;
            for (int i = 0; i < roof; i += 2)
            {
                int inset = (roof - i) * w / (2 * roof);
                Ui.Rect(Root, x + inset, y + i, w - 2 * inset, 2, c, Ui.Back + 3);
            }
            Ui.Rect(Root, x, y + roof, w, h - roof, c, Ui.Back + 3);
        }

        // ------------------------------------------------------------ top bar

        void BuildTopBar()
        {
            Ui.Image(Root, PixelArt.Gradient(Ui.Width, 48, Palette.Hex(0x070A14, 218), Palette.Hex(0x070A14, 0)), 0, 0, Ui.Panels);
            Ui.Label(Root, "HUB · SAFE ZONE", 14, 9, Palette.Mist3, font: FontKind.Interface);
            Ui.Label(Root, "Geistwald City", 14, 19, Palette.Gold2);

            int gold = Run != null ? Run.Gold : 0;
            int gw = Mathf.Max(84, PixelFont.Measure($"{gold:#,##0} HUB GOLD", FontKind.DotGothic) + 16);
            int x = 626 - gw;
            Ui.CurrencyTag(Root, gold, true, x, 11);

            x -= 6 + 66;
            Ui.Button(Root, "Main Menu", x, 11, 66, OnTitleClicked, 18, showCursor: false, style: ButtonStyle.Flat);
            x -= 4 + 40;
            Ui.Button(Root, "Load", x, 11, 40, () => Game.Go(GameState.Load), 18, showCursor: false);
            x -= 4 + 40;
            Ui.Button(Root, "Save", x, 11, 40, () => Game.Go(GameState.Save), 18, showCursor: false);

            var auto = SaveManager.GetSlotMetadata(SaveSlotId.AutoSave);
            string when = auto != null && auto.isOccupied && auto.lastSavedTicks > 0 ? new DateTime(auto.lastSavedTicks).ToString("HH:mm") : "HUB";
            string tag = $"AUTO SAVED · {when}";
            Ui.Tag(Root, tag, x - 8 - PixelFont.Measure(tag) - 8, 14, TagKind.Keep, 13);
        }

        // ------------------------------------------------------- facility menu

        void BuildFacilityMenu()
        {
            var nav = new GameObject("FacilityMenu").transform;
            nav.SetParent(Root, false);
            const int px = 14, py = 48, pw = 165, ph = 224;
            Ui.Panel(nav, px, py, pw, ph);

            menu = Root.gameObject.AddComponent<Menu>();
            menu.onFocus = ShowTip;
            var party = Run?.Party ?? new List<Unit>();
            int sp = party.Sum(u => u.skillLoadout?.SkillPoints ?? 0);
            int dead = party.Count(u => !u.Alive);
            int relics = Run?.Relics.Count ?? 0;

            int y = py + 5;
            Item(nav, "Character / Party", y, () => Game.Go(GameState.Party), "VIEW THE PARTY, STATUS AND LINEUP.");
            Item(nav, "Equipment", y += 19, () => Game.Go(GameState.Equipment), "EQUIP GEAR ON EACH MEMBER AND COMPARE STATS.");
            Item(nav, "Inventory", y += 19, () => Game.Go(GameState.Inventory), "EQUIPMENT BAG, CONSUMABLES, MATERIALS AND KEY ITEMS.");
            Item(nav, "Shop", y += 19, () => Game.Go(GameState.HubShop), "CONSUMABLES FOR HUB GOLD. UNLIMITED STOCK.", note: "CONSUMABLES");
            Item(nav, "Armory", y += 19, () => Game.Go(GameState.Armory), "WEAPONS AND ARMOR FOR HUB GOLD.", note: "EQUIPMENT");
            Item(nav, "Blacksmith", y += 19, () => Game.Go(GameState.Blacksmith), "UPGRADE GEAR. UPGRADES RAISE BASE STATS ONLY.");
            Item(nav, "Training Ground", y += 19, () => Game.Go(GameState.Skills), "SPEND SKILL POINTS AND CHOOSE TALENTS.", buff: sp > 0 ? $"{sp} SP" : null);
            Item(nav, "Temple", y += 19, () => Game.Go(GameState.Temple), "REVIVE FALLEN MEMBERS AND RECOVER HP / MP.", danger: dead > 0 ? $"{dead} DEAD" : null);
            Item(nav, "Magic Shop", y += 19, () => Game.Go(GameState.MagicShop), "BUY AND SELL PERMANENT RELICS.", note: $"RELICS {relics}/5");

            y += 22;
            Ui.Rect(nav, px + 6, y, pw - 12, 1, Palette.Ink5, Ui.Panels + 2);
            int act = PersistenceCommitHandler.ActiveCommittedSnapshot?.nextDungeonAct ?? Run?.Act ?? 1;
            var gate = Ui.Button(nav, "Dungeon Entrance", px + 14, y + 5, pw - 20, OnEmbarkClicked, 22, style: ButtonStyle.Flat);
            gate.label.Color = Palette.Gold1;
            var baseVisual = gate.visual;
            gate.visual = on => { baseVisual(on); gate.label.Color = on ? Palette.Gold1 : Palette.Gold2; };
            gate.visual(false);
            Ui.Badge(gate.transform, "Z", pw - 36, 5, 12, 12, Palette.Mist2, Palette.Hex(0x8790AD), Palette.Ink1, order: Ui.Widget + 1, font: FontKind.Interface);
            gate.tooltip = act > 8 ? "ALL EIGHT LAYERS ARE CLEAR." : $"BEGIN DUNGEON {act} OF 8. A NEW RUN STARTS WITH 0 RUN GOLD.";
            gate.name = "Dungeon Entrance";
            menu.Add(gate);

            feedbackLabel = Ui.Label(Root, "", 190, 262, Palette.Err, font: FontKind.Interface);

            // Tooltip (.tip) beside the town — describes the focused facility.
            tipBox = Ui.Image(Root, null, 451, 105, Ui.Overlay - 2);
            tipTitle = Ui.Label(Root, "", 457, 110, Palette.Gold1, order: Ui.Overlay - 1);
            tipText = Ui.Label(Root, "", 457, 124, Palette.Mist2, order: Ui.Overlay - 1, font: FontKind.Interface);
            menu.Focus(menu.items.Count - 1, false, true); // Dungeon Entrance is the default focus
        }

        void Item(Transform nav, string text, int y, Action go, string tip, string note = null, string buff = null, string danger = null)
        {
            const int x = 31, w = 143;
            var b = Ui.Button(nav, text, x, y, w, go, 18, style: ButtonStyle.Flat);
            b.tooltip = tip;
            b.name = text;
            if (note != null) Ui.Label(b.transform, note, w - 4, 6, Palette.Mist2, Align.Right, order: Ui.Widget + 1, font: FontKind.Interface);
            if (buff != null)
            {
                int bw = PixelFont.Measure(buff) + 12;
                Ui.Image(b.transform, PixelArt.Badge(bw, 12, Palette.Ink0, Palette.Hex(0x3B9A67)), w - 4 - bw, 3, Ui.Widget + 1);
                Ui.Image(b.transform, PixelArt.Diamond(5, Palette.Hex(0x9AF0BF)), w - bw, 6, Ui.Widget + 2);
                Ui.Label(b.transform, buff, w - 4 - bw + 9, 5, Palette.Hex(0x9AF0BF), order: Ui.Widget + 2, font: FontKind.Interface);
            }
            if (danger != null)
            {
                int dw = PixelFont.Measure(danger) + 8;
                Ui.Image(b.transform, PixelArt.Badge(dw, 12, Palette.Hex(0x3A1418), Palette.Err), w - 4 - dw, 3, Ui.Widget + 1);
                Ui.Label(b.transform, danger, w - dw, 5, Palette.Hex(0xFFD2CC), order: Ui.Widget + 2, font: FontKind.Interface);
            }
            menu.Add(b);
        }

        void ShowTip(Selectable s)
        {
            if (tipBox == null || s == null) return;
            string body = PixelFont.Wrap(s.tooltip ?? "", 106);
            int h = 22 + (body.Count(c => c == '\n') + 1) * PixelFont.LineHeight;
            tipBox.sprite = PixelArt.Box(118, h, Palette.Ink2, Palette.Gold3);
            tipTitle.Set(s.name);
            tipText.Set(body);
        }

        // ------------------------------------------------------------ progress

        void BuildProgress()
        {
            const int x = 14, y = 290, w = 165, h = 56;
            Ui.Panel(Root, x, y, w, h);
            int act = Run != null ? Run.Act : 1;
            int cleared = Math.Clamp(act - 1, 0, 8);
            Ui.Label(Root, "RUN PROGRESS", x + 7, y + 7, Palette.Mist3, font: FontKind.Interface);
            Ui.Label(Root, $"{cleared} / 8 cleared", x + w - 7, y + 5, Palette.Gold2, Align.Right);
            int bw = (w - 14 - 7 * 2) / 8;
            for (int i = 1; i <= 8; i++)
            {
                int bx = x + 7 + (i - 1) * (bw + 2), by = y + 20;
                bool done = i < act, now = i == act;
                var sprite = done ? PixelArt.Badge(bw, 11, Palette.Ink4, Palette.Mist3)
                    : now ? PixelArt.Badge(bw, 11, Palette.Hex(0x2B2210), Palette.Gold2)
                    : PixelArt.DashedBadge(bw, 11, Color.clear, Palette.Ink5);
                Ui.Image(Root, sprite, bx, by, Ui.Widget);
                Ui.Label(Root, i.ToString(), bx + bw / 2, by + 2, done ? Palette.Mist2 : now ? Palette.Gold1 : Palette.Ink5, Align.Center, order: Ui.Widget + 1, font: FontKind.Interface);
            }
            Ui.Label(Root, "PROGRESS RESETS IF THE PARTY FALLS.", x + 7, y + 36, Palette.Mist2, font: FontKind.Interface);
            Ui.Label(Root, "EQUIPMENT IS ALWAYS KEPT.", x + 7, y + 45, Palette.Mist2, font: FontKind.Interface);
        }

        // ---------------------------------------------------------- party strip

        void BuildPartyStrip()
        {
            var party = Run?.Party ?? Database.Party.Select(id => new Unit(Database.Characters[id])).ToList();
            const int cw = 98, ch = 32, gap = 5, y = 314;
            int x0 = 626 - 4 * cw - 3 * gap;
            for (int i = 0; i < 4 && i < party.Count; i++)
            {
                var u = party[i];
                int x = x0 + i * (cw + gap);
                Ui.Panel(Root, x, y, cw, ch);
                Color c = Palette.ForClass(u.classDef?.id);
                Ui.Image(Root, PixelArt.Box(20, 20, Color.Lerp(c, Palette.Ink1, 0.7f), Palette.Ink5), x + 5, y + 6, Ui.Widget);
                Ui.Rect(Root, x + 12, y + 10, 6, 5, Color.Lerp(c, Palette.Hex(0xF3DCC0), 0.55f), Ui.Widget + 1);
                Ui.Rect(Root, x + 8, y + 19, 14, 4, c, Ui.Widget + 1);
                Ui.Label(Root, PixelFont.Fit(u.name, 46, FontKind.DotGothic), x + 29, y + 4, u.Alive ? Palette.Mist1 : Palette.Mist3, order: Ui.Widget);
                Ui.Label(Root, $"LV {u.level}", x + cw - 5, y + 6, Palette.Mist3, Align.Right, order: Ui.Widget, font: FontKind.Interface);
                float hp = u.Alive ? (float)u.hp / Mathf.Max(1, u.stats.maxHp) : 0f;
                Ui.Image(Root, PixelArt.GaugeBar(cw - 34, 4, hp, hp < 0.25f ? Palette.HpLow : Palette.Hp), x + 29, y + 17, Ui.Widget);
                Ui.Image(Root, PixelArt.GaugeBar(cw - 34, 4, (float)u.mp / Mathf.Max(1, u.stats.maxMp), Palette.Mp), x + 29, y + 23, Ui.Widget);
            }
        }

        // ------------------------------------------------------------- actions

        void OnEmbarkClicked()
        {
            if (Run == null) return;
            if (Run.Party.All(u => !u.Alive))
            {
                Sfx.Play(Sound.Deny);
                feedbackLabel?.Set("ALL HEROES ARE DOWN — REVIVE AT THE TEMPLE FIRST.");
                return;
            }

            SaveManager.AutoSave(out _);

            int targetAct = PersistenceCommitHandler.ActiveCommittedSnapshot != null
                ? PersistenceCommitHandler.ActiveCommittedSnapshot.nextDungeonAct
                : Run.Act;

            if (targetAct > 8)
            {
                Game.Go(GameState.Victory);
                return;
            }

            if (Run.Map == null || Run.Map.act < targetAct)
            {
                var nextMap = MapGenerator.Generate(Run.Seed + targetAct, targetAct);
                Run.AdvanceToNextDungeon(nextMap);
            }
            Game.Go(GameState.Map);
        }

        void OnTitleClicked()
        {
            new ConfirmationModal(Root, "RETURN TO TITLE",
                "Return to the Main Menu?\nEnsure your progress is saved before exiting the Hub.",
                "RETURN TO TITLE",
                () => Game.Go(GameState.MainMenu));
        }
    }
}
