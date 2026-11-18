using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using UiLabel = UnityEngine.UIElements.Label;

namespace Obscure.Tests
{
    // Live PlayMode UI button & hit-test suite: verifies every rendered button's hitbox,
    // viewport bounds, orphan-button absence, and real mouse clicks across all screens and modals.
    public class UiButtonPlayTests : InputTestFixture
    {
        const string PrefKey = "obscure.settings";
        Keyboard kb;
        Mouse mouse;
        string savedPrefs;
        ISaveStorage realStorage;

        public override void Setup()
        {
            var uiModule = UnityEngine.Object.FindFirstObjectByType<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            if (uiModule != null) uiModule.enabled = false;
            base.Setup();
            realStorage = SaveManager.Storage;
            SaveManager.Storage = new MemorySaveStorage();
            kb = InputSystem.AddDevice<Keyboard>();
            mouse = InputSystem.AddDevice<Mouse>();
            if (uiModule != null) uiModule.enabled = true;
            StartClockAfterPlayModeEntry();
            savedPrefs = PlayerPrefs.GetString(PrefKey, null);
            Time.timeScale = 4f;
        }

        public override void TearDown()
        {
            Time.timeScale = 1f;
            if (realStorage != null) SaveManager.Storage = realStorage;
            if (savedPrefs == null) PlayerPrefs.DeleteKey(PrefKey); else PlayerPrefs.SetString(PrefKey, savedPrefs);
            var uiModule = UnityEngine.Object.FindFirstObjectByType<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            if (uiModule != null) uiModule.enabled = false;
            base.TearDown();
            if (uiModule != null) uiModule.enabled = true;
        }

        void StartClockAfterPlayModeEntry()
        {
            var sys = typeof(InputSystem).GetField("s_SystemObject", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)?.GetValue(null);
            var enter = sys?.GetType().GetField("enterPlayModeTime")?.GetValue(sys);
            Assert.IsNotNull(enter);
            currentTime = Math.Max(currentTime, (double)enter + 1);
        }

        static GameState State => Game.I.State;
        static RunState Run => Game.I.Run;

        static IEnumerator Settle()
        {
            for (float t = 0; (Game.I == null || Controls.Locked) && t < 5f; t += Time.unscaledDeltaTime) yield return null;
            Assert.IsNotNull(Game.I, "game did not boot");
            Assert.IsFalse(Controls.Locked, "transition locked");
            yield return null;
            yield return null;
        }

        IEnumerator Begin(QteAssist assist = QteAssist.Auto)
        {
            yield return Settle();
            Set(mouse.position, Vector2.zero, queueEventOnly: true);
            yield return null;
            Game.I.EndRun();
            yield return Settle();
            Assert.AreEqual(GameState.MainMenu, State);
            Game.I.Settings.qteAssist = (int)assist;
            Game.I.Settings.qteKey = 0;
            Controls.QteKey = Key.Space;
        }

        IEnumerator Tap(Key key)
        {
            Press(kb[key], queueEventOnly: true);
            yield return null;
            Release(kb[key], queueEventOnly: true);
            yield return null;
        }

        static Vector2 PanelToScreen(VisualElement el, Vector2 panelPoint)
        {
            var panelSize = el.panel.visualTree.layout;
            float sx = Screen.width / Mathf.Max(1f, panelSize.width);
            float sy = Screen.height / Mathf.Max(1f, panelSize.height);
            return new Vector2(panelPoint.x * sx, Screen.height - panelPoint.y * sy);
        }

        IEnumerator MoveMouseTo(VisualElement el)
        {
            Assert.IsNotNull(el, "target element is null");
            Assert.IsNotNull(el.panel, $"element '{el.name}' not attached to a panel");
            yield return null;
            var wb = el.worldBound;
            Assert.Greater(wb.width, 0f, $"element '{el.name}' has 0 width");
            Assert.Greater(wb.height, 0f, $"element '{el.name}' has 0 height");
            Set(mouse.position, PanelToScreen(el, wb.center), queueEventOnly: true);
            yield return null;
            yield return null;
        }

        IEnumerator ClickElement(VisualElement el)
        {
            yield return MoveMouseTo(el);
            Press(mouse.leftButton, queueEventOnly: true);
            yield return null;
            yield return null;
            Release(mouse.leftButton, queueEventOnly: true);
            yield return null;
            yield return null;
            yield return Settle();
        }

        IEnumerator ClickMenuItem(string exactText, bool preferLast = false)
        {
            var m = UitkMenu.ActiveMenu;
            Assert.IsNotNull(m, $"No ActiveMenu when looking for '{exactText}' in {State}");
            var matches = m.items.Where(it => string.Equals(it.text, exactText, StringComparison.OrdinalIgnoreCase)).ToList();
            var item = preferLast ? matches.LastOrDefault() : matches.FirstOrDefault();
            Assert.IsNotNull(item, $"Button '{exactText}' not found in ActiveMenu on {State}. Available: [{string.Join(", ", m.items.Select(i => i.text))}]");
            ScrollParentIntoView(item.element);
            yield return null;
            AssertHitTest(item, State.ToString());
            yield return ClickElement(item.element);
        }

        static void ScrollParentIntoView(VisualElement el)
        {
            for (var p = el?.parent; p != null; p = p.parent)
            {
                if (p is ScrollView sv)
                {
                    sv.ScrollTo(el);
                    break;
                }
            }
        }

        static bool IsEffectivelyVisible(VisualElement el)
        {
            for (var cur = el; cur != null; cur = cur.parent)
            {
                if (cur.resolvedStyle.display == DisplayStyle.None || cur.resolvedStyle.visibility == Visibility.Hidden)
                    return false;
            }
            return true;
        }

        static bool IsInsideScrollView(VisualElement el)
        {
            for (var p = el?.parent; p != null; p = p.parent)
                if (p is ScrollView) return true;
            return false;
        }

        static void AssertHitTest(UitkMenu.Item item, string context)
        {
            var el = item.element;
            Assert.IsNotNull(el, $"[{context}] Item '{item.text}' has null element");
            if (!IsEffectivelyVisible(el)) return;
            Assert.IsNotNull(el.panel, $"[{context}] Item '{item.text}' has null panel");

            var wb = el.worldBound;
            Assert.Greater(wb.width, 0f, $"[{context}] Item '{item.text}' width <= 0");
            Assert.Greater(wb.height, 0f, $"[{context}] Item '{item.text}' height <= 0");

            if (!IsInsideScrollView(el))
            {
                var panelRect = el.panel.visualTree.layout;
                Assert.GreaterOrEqual(wb.xMin, -2f, $"[{context}] '{item.text}' clipped left ({wb})");
                Assert.GreaterOrEqual(wb.yMin, -2f, $"[{context}] '{item.text}' clipped top ({wb})");
                Assert.LessOrEqual(wb.xMax, panelRect.width + 2f, $"[{context}] '{item.text}' clipped right ({wb} vs {panelRect})");
                Assert.LessOrEqual(wb.yMax, panelRect.height + 2f, $"[{context}] '{item.text}' clipped bottom ({wb} vs {panelRect})");
            }
            else
            {
                ScrollParentIntoView(el);
                wb = el.worldBound;
                var panelRect = el.panel.visualTree.layout;
                if (wb.yMax < 0f || wb.yMin > panelRect.height) return;
            }

            var picked = el.panel.Pick(wb.center);
            Assert.IsNotNull(picked, $"[{context}] panel.Pick({wb.center}) returned null for '{item.text}'");
            bool owned = picked == el || el.Contains(picked);
            Assert.IsTrue(owned, $"[{context}] Button '{item.text}' at {wb} is blocked by '{picked.name}' ({picked.GetType().Name}, classes: {string.Join(".", picked.GetClasses())})");
        }

        static void AssertActiveMenuAudit(string context, bool checkOrphanButtons = true)
        {
            var m = UitkMenu.ActiveMenu;
            Assert.IsNotNull(m, $"[{context}] UitkMenu.ActiveMenu is null");
            Assert.Greater(m.items.Count, 0, $"[{context}] ActiveMenu has 0 items");
            foreach (var it in m.items)
                AssertHitTest(it, context);

            if (!checkOrphanButtons) return;
            var root = m.items[0].element?.panel?.visualTree;
            if (root == null) return;
            foreach (var btn in root.Query<Button>().ToList())
            {
                if (!IsEffectivelyVisible(btn) || !btn.enabledInHierarchy) continue;
                bool wired = m.items.Any(it => it.element == btn || (it.element != null && it.element.Contains(btn)));
                Assert.IsTrue(wired, $"[{context}] Enabled visible orphan UXML <Button> '{btn.text}' (name={btn.name}, wb={btn.worldBound}) is NOT wired to ActiveMenu!");
            }
        }

        [UnityTest]
        public IEnumerator MainMenuAndSettingsButtons_HitTestAndWorkViaMouse()
        {
            yield return Begin();
            AssertActiveMenuAudit("MainMenu");

            // Click SETTINGS
            yield return ClickMenuItem("SETTINGS");
            Assert.AreEqual(GameState.Settings, State);
            AssertActiveMenuAudit("Settings-Audio");

            // Click slider track directly and drag without rebuilding mid-click
            int beforeVol = Game.I.Settings.volume;
            var volRow = UitkMenu.ActiveMenu.items.First(i => i.text == "Master volume").element;
            var track = volRow.Query<VisualElement>().ToList().FirstOrDefault(v => Mathf.Abs(v.resolvedStyle.width - 280f) < 2f);
            Assert.IsNotNull(track, "Slider track not found on Audio Settings");
            var twb = track.worldBound;
            Vector2 nearLeft = PanelToScreen(track, new Vector2(twb.xMin + 28f, twb.center.y));
            Set(mouse.position, nearLeft, queueEventOnly: true);
            yield return null;
            Press(mouse.leftButton, queueEventOnly: true);
            yield return null;
            Vector2 nearRight = PanelToScreen(track, new Vector2(twb.xMax - 28f, twb.center.y));
            Set(mouse.position, nearRight, queueEventOnly: true);
            yield return null;
            Release(mouse.leftButton, queueEventOnly: true);
            yield return null;
            Assert.AreNotEqual(beforeVol, Game.I.Settings.volume, "Dragging Master volume slider track must update setting value");

            // Toggle Mute music via click
            bool muted = Game.I.Settings.musicMuted;
            yield return ClickMenuItem("Mute music");
            Assert.AreEqual(!muted, Game.I.Settings.musicMuted);

            // Click Graphics, Controls, QTE tabs
            yield return ClickMenuItem("Graphics");
            AssertActiveMenuAudit("Settings-Graphics");
            yield return ClickMenuItem("Display mode");

            yield return ClickMenuItem("Controls");
            AssertActiveMenuAudit("Settings-Controls");

            yield return ClickMenuItem("QTE");
            AssertActiveMenuAudit("Settings-QTE");

            // Revert dirty changes and verify clean Back
            yield return ClickMenuItem("Revert");
            yield return ClickMenuItem("Back");
            Assert.AreEqual(GameState.MainMenu, State);
        }

        [UnityTest]
        public IEnumerator CharacterCreationButtons_NoHoverSpendAndModalIsolated()
        {
            yield return Begin();
            yield return ClickMenuItem("NEW GAME");
            Assert.AreEqual(GameState.CharacterCreation, State);
            AssertActiveMenuAudit("CharCreation-Race");

            var screen = Game.I.CurrentScreen as CharacterCreationScreen;
            Assert.IsNotNull(screen);

            // No dedicated locked Confirm button or lock note in footer; select choice then click Next steps strictly 1 page at a time
            Assert.IsNull(FigmaUi.FindButton(screen.UiRoot, "Confirm"), "Dedicated 'Confirm' button must be removed.");
            Assert.IsNull(FigmaUi.FindLabel(screen.UiRoot, "🔒 CONFIRM UNLOCKS ON REVIEW"), "Lock label must be removed.");

            // Pick a Race card, then click Next -> must go ONLY to Class (not jump to Review/Party)
            yield return ClickMenuItem("Elf");
            Assert.AreEqual(CharacterCreationScreen.Page.Race, screen.CurrentPage, "Selecting a race must stay on Race until Next is clicked.");
            Assert.AreEqual("elf", screen.Session.Race.id);

            yield return ClickMenuItem("Next: Class");
            Assert.AreEqual(CharacterCreationScreen.Page.Class, screen.CurrentPage);
            AssertActiveMenuAudit("CharCreation-Class");

            // Pick a Class card, then click Next -> must go ONLY to Abilities
            yield return ClickMenuItem("Wizard");
            Assert.AreEqual(CharacterCreationScreen.Page.Class, screen.CurrentPage);
            Assert.AreEqual("wizard", screen.Session.Class.id);

            yield return ClickMenuItem("Next: Abilities");
            Assert.AreEqual(CharacterCreationScreen.Page.Abilities, screen.CurrentPage);
            AssertActiveMenuAudit("CharCreation-Abilities");

            // Bug 1a regression: hovering STR/DEX must NOT spend point buy points
            int remBefore = screen.Session.PointBuy.RemainingPoints;
            var strCard = UitkMenu.ActiveMenu.items.First(i => i.text == "STR").element;
            var dexCard = UitkMenu.ActiveMenu.items.First(i => i.text == "DEX").element;
            yield return MoveMouseTo(strCard);
            yield return MoveMouseTo(dexCard);
            Assert.AreEqual(remBefore, screen.Session.PointBuy.RemainingPoints, "Hovering ability cards must NOT auto-spend ability points!");

            // Clicking STR spends points as expected, Reset refunds
            yield return ClickElement(strCard);
            Assert.Less(screen.Session.PointBuy.RemainingPoints, remBefore);
            yield return ClickMenuItem("Reset");
            Assert.AreEqual(remBefore, screen.Session.PointBuy.RemainingPoints);

            // Open Background modal and verify CLOSE isolates ActiveMenu and closes cleanly
            yield return ClickMenuItem("Next: Background");
            Assert.AreEqual(CharacterCreationScreen.Page.Background, screen.CurrentPage);
            AssertActiveMenuAudit("CharCreation-Background");

            screen.OpenBackgroundDetailsModal();
            yield return null;
            Assert.IsTrue(screen.IsBackgroundModalOpen);
            AssertActiveMenuAudit("CharCreation-BackgroundModal", checkOrphanButtons: false);
            yield return ClickMenuItem("CLOSE");
            Assert.IsFalse(screen.IsBackgroundModalOpen);

            // Step chip click returns to Race (completed chips show '+ RACE')
            var raceChip = FigmaUi.FindInstanceWithText(screen.UiRoot, "text-0", "+ RACE");
            Assert.IsNotNull(raceChip);
            yield return ClickElement(raceChip);
            Assert.AreEqual(CharacterCreationScreen.Page.Race, screen.CurrentPage);

            // Return to Main Menu via Back
            yield return ClickMenuItem("Back");
            Assert.AreEqual(GameState.MainMenu, State);
        }

        [UnityTest]
        public IEnumerator HubServicesAndInventoryButtons_HitTestAndNoSilentNoOps()
        {
            yield return Begin();
            Game.I.StartRun();
            yield return Settle();
            Game.I.Go(GameState.Hub);
            yield return Settle();
            Assert.AreEqual(GameState.Hub, State);
            AssertActiveMenuAudit("Hub");

            // Give plenty of gold & materials for shop/blacksmith/inventory tests
            Run.Gold = 5000;
            Run.Inventory.AddMaterial("iron_ingot", 3);

            // 1. HubShop: Buy modal + / - keeps focus on +/- instead of jumping to Buy
            yield return ClickMenuItem("Shop");
            Assert.AreEqual(GameState.HubShop, State);
            AssertActiveMenuAudit("HubShop");
            yield return ClickMenuItem("Buy", preferLast: true); // opens quantity dialog (avoid 'Buy' tab)
            AssertActiveMenuAudit("HubShop-QtyModal", checkOrphanButtons: false);
            yield return ClickMenuItem("+");
            Assert.AreEqual("+", UitkMenu.ActiveMenu.Current?.text, "Clicking '+' must preserve focus on '+'");
            yield return Tap(Key.Enter); // pressing Enter must increment qty again (to 3), NOT buy!
            Assert.AreEqual("+", UitkMenu.ActiveMenu.Current?.text, "Pressing Enter on '+' must stay on '+'");
            yield return ClickMenuItem("Cancel");
            yield return ClickMenuItem("Back");
            Assert.AreEqual(GameState.Hub, State);

            // 2. Armory
            yield return ClickMenuItem("Armory");
            Assert.AreEqual(GameState.Armory, State);
            AssertActiveMenuAudit("Armory");
            yield return ClickMenuItem("Back");

            // 3. Blacksmith
            yield return ClickMenuItem("Blacksmith");
            Assert.AreEqual(GameState.Blacksmith, State);
            AssertActiveMenuAudit("Blacksmith");
            yield return ClickMenuItem("Back");

            // 4. Temple
            yield return ClickMenuItem("Temple");
            Assert.AreEqual(GameState.Temple, State);
            AssertActiveMenuAudit("Temple");
            yield return ClickMenuItem("Back");

            // 5. Magic Shop
            yield return ClickMenuItem("Magic Shop");
            Assert.AreEqual(GameState.MagicShop, State);
            AssertActiveMenuAudit("MagicShop");
            yield return ClickMenuItem("Back");

            // 6. Inventory: Materials Discard works & Key Items Discard disabled
            yield return ClickMenuItem("Inventory");
            Assert.AreEqual(GameState.Inventory, State);
            AssertActiveMenuAudit("Inventory-Equipment");
            yield return ClickMenuItem("Materials");
            AssertActiveMenuAudit("Inventory-Materials");
            Assert.IsFalse(UitkMenu.ActiveMenu.items.First(i => i.text == "Sort").interactable, "Sort must be disabled on non-Equipment tabs");
            Assert.IsFalse(UitkMenu.ActiveMenu.items.First(i => i.text == "Filter").interactable, "Filter must be disabled on non-Equipment tabs");
            int matsBefore = Run.Inventory.GetMaterialCount("iron_ingot");
            yield return ClickMenuItem("Discard");
            Assert.AreEqual(matsBefore - 1, Run.Inventory.GetMaterialCount("iron_ingot"), "Discard on Materials tab must discard 1 unit");
            yield return ClickMenuItem("Key Items");
            AssertActiveMenuAudit("Inventory-KeyItems");
            var disOnKeys = UitkMenu.ActiveMenu.items.First(i => i.text == "Discard");
            Assert.IsFalse(disOnKeys.interactable, "Discard must be disabled on Key Items");
            yield return ClickMenuItem("Back");

            // 7. Equipment, Skills, Talents, Save, Load
            yield return ClickMenuItem("Equipment");
            Assert.AreEqual(GameState.Equipment, State);
            AssertActiveMenuAudit("Equipment");
            yield return ClickMenuItem("Back");

            yield return ClickMenuItem("Training Ground");
            Assert.AreEqual(GameState.Skills, State);
            AssertActiveMenuAudit("Skills");
            yield return ClickMenuItem("Talents");
            Assert.AreEqual(GameState.Talents, State);
            AssertActiveMenuAudit("Talents");
            yield return ClickMenuItem("Scroll of Emptiness");
            AssertActiveMenuAudit("Talents-RespecModal", checkOrphanButtons: false);
            yield return ClickMenuItem("Cancel");
            yield return ClickMenuItem("Back");

            yield return ClickMenuItem("Save");
            Assert.AreEqual(GameState.Save, State);
            AssertActiveMenuAudit("Save");
            yield return ClickMenuItem("Back");

            yield return ClickMenuItem("Load");
            Assert.AreEqual(GameState.Load, State);
            AssertActiveMenuAudit("Load");
            yield return ClickMenuItem("Back");
        }

        [UnityTest]
        public IEnumerator PartyStatusAndDungeonCombatButtons_WorkViaMouseClick()
        {
            yield return Begin();
            Game.I.StartRun();
            yield return Settle();
            Assert.AreEqual(GameState.Map, State);
            AssertActiveMenuAudit("Map");

            // Open Party -> View Status -> tabs & StatRow click -> Back
            yield return ClickMenuItem("Party");
            Assert.AreEqual(GameState.Party, State);
            AssertActiveMenuAudit("Party");

            yield return ClickMenuItem("View Status");
            Assert.AreEqual(GameState.CharacterStatus, State);
            AssertActiveMenuAudit("Status-Overview");
            yield return ClickMenuItem("Attributes");
            AssertActiveMenuAudit("Status-Attributes");
            yield return ClickMenuItem("Combat");
            AssertActiveMenuAudit("Status-CombatStats");
            yield return ClickMenuItem("Status Effects");
            AssertActiveMenuAudit("Status-StatusEffects");
            yield return ClickMenuItem("Back");
            Assert.AreEqual(GameState.Party, State);
            yield return ClickMenuItem("Confirm Party");
            Assert.AreEqual(GameState.Map, State);

            // Map Pause modal -> QTE Assists -> Settings QTE tab -> Back
            yield return Tap(Key.Escape);
            yield return Settle();
            AssertActiveMenuAudit("Map-PauseModal", checkOrphanButtons: false);
            yield return ClickMenuItem("QTE Assists");
            Assert.AreEqual(GameState.Settings, State);
            AssertActiveMenuAudit("Settings-FromPauseQTE");
            yield return ClickMenuItem("Back");
            Assert.AreEqual(GameState.Map, State);

            // Enter Combat node by clicking it on the Map
            var combatItem = UitkMenu.ActiveMenu.items.First(i => i.text == "Combat");
            yield return ClickElement(combatItem.element);
            Assert.AreEqual(GameState.Combat, State);

            // Wait for party turn
            for (float t = 0; t < 10f && (UitkMenu.ActiveMenu == null || UitkMenu.ActiveMenu.items.Count == 0); t += Time.unscaledDeltaTime)
                yield return null;
            AssertActiveMenuAudit("Combat-Actions");

            // Click SKILL -> BACK -> ATTACK -> click target button
            yield return ClickMenuItem("SKILL");
            AssertActiveMenuAudit("Combat-Skills");
            yield return ClickMenuItem("BACK");
            AssertActiveMenuAudit("Combat-Actions");

            var cb = UnityEngine.Object.FindFirstObjectByType<CombatView>().Battle;
            int logBefore = cb.Log.Count;
            yield return ClickMenuItem("ATTACK");
            AssertActiveMenuAudit("Combat-Targets");
            var firstEnemy = UitkMenu.ActiveMenu.items[0];
            yield return ClickElement(firstEnemy.element);

            for (float t = 0; t < 10f && cb.Log.Count == logBefore; t += Time.unscaledDeltaTime)
                yield return null;
            Assert.Greater(cb.Log.Count, logBefore, "Clicking ATTACK + Enemy target must execute attack");
        }

        [UnityTest]
        public IEnumerator MainMenu_ResolvesDisplayGothicAndUiFontAssets()
        {
            yield return Begin();
            var doc = UnityEngine.Object.FindObjectsByType<UIDocument>(FindObjectsSortMode.None)
                .FirstOrDefault(d => d.rootVisualElement != null && d.rootVisualElement.Q<UiLabel>(className: "felna-vars") != null);
            Assert.IsNotNull(doc, "MainMenu UIDocument must be mounted.");

            var title = doc.rootVisualElement.Q<UiLabel>(className: "felna-vars");
            var subtitle = doc.rootVisualElement.Q<UiLabel>(className: "the-world-s-between-eight-layers-down-vars");
            var newGameBtn = doc.rootVisualElement.Q<Button>(className: "button-flat-2");

            Assert.IsNotNull(title, "MainMenu title label must exist");
            Assert.IsNotNull(subtitle, "MainMenu subtitle label must exist");
            Assert.IsNotNull(newGameBtn, "MainMenu NEW GAME button must exist");

            var titleFont = title.resolvedStyle.unityFontDefinition.fontAsset;
            var gothicFont = newGameBtn.resolvedStyle.unityFontDefinition.fontAsset;
            var uiFont = subtitle.resolvedStyle.unityFontDefinition.fontAsset;

            Assert.IsNotNull(titleFont, "MainMenu title fontAsset resolved null (MissingAssetReference)");
            Assert.IsNotNull(gothicFont, "MainMenu button fontAsset resolved null (MissingAssetReference)");
            Assert.IsNotNull(uiFont, "MainMenu subtitle fontAsset resolved null");

            Assert.AreEqual("FelnaDisplayFont", titleFont.name);
            Assert.AreEqual("FelnaGothicFont", gothicFont.name);
            Assert.AreEqual("FelnaUIFont", uiFont.name);
            Assert.Greater(titleFont.characterTable.Count, 0, "FelnaDisplayFont must populate dynamic glyphs on MainMenu");
            Assert.Greater(gothicFont.characterTable.Count, 0, "FelnaGothicFont must populate dynamic glyphs on MainMenu");
        }
    }
}
