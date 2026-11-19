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
    // Drives the real game through every state with simulated keyboard/mouse input.
    public class GameFlowTests : InputTestFixture
    {
        const string PrefKey = "obscure.settings";
        Keyboard kb;
        Mouse mouse;
        string savedPrefs;
        ISaveStorage realStorage;

        public override void Setup()
        {
            var uiModule = Object.FindFirstObjectByType<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            if (uiModule != null) uiModule.enabled = false;
            base.Setup();
            realStorage = SaveManager.Storage;
            SaveManager.Storage = new MemorySaveStorage();
            kb = InputSystem.AddDevice<Keyboard>();
            mouse = InputSystem.AddDevice<Mouse>();
            if (uiModule != null) uiModule.enabled = true;
            StartClockAfterPlayModeEntry();
            savedPrefs = PlayerPrefs.GetString(PrefKey, null);
            Time.timeScale = 4f; // combat pacing uses scaled time; menus/fades use unscaled
        }

        public override void TearDown()
        {
            Time.timeScale = 1f;
            if (realStorage != null) SaveManager.Storage = realStorage;
            if (savedPrefs == null) PlayerPrefs.DeleteKey(PrefKey); else PlayerPrefs.SetString(PrefKey, savedPrefs);
            var uiModule = Object.FindFirstObjectByType<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            if (uiModule != null) uiModule.enabled = false;
            base.TearDown();
            if (uiModule != null) uiModule.enabled = true;
        }

        // In the editor the Input System drops every state event stamped between leaving edit mode and entering
        // play mode (InputSystem.s_SystemObject.exitEditModeTime..enterPlayModeTime, several seconds apart in batch mode).
        // The test runtime's clock restarts at zero each test, so a long test walks into that window and loses every
        // key until it walks out again. Start the clock past the window. The fields are internal, hence reflection.
        void StartClockAfterPlayModeEntry()
        {
            var sys = typeof(InputSystem).GetField("s_SystemObject", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)?.GetValue(null);
            var enter = sys?.GetType().GetField("enterPlayModeTime")?.GetValue(sys);
            Assert.IsNotNull(enter, "Input System internals changed: InputSystem.s_SystemObject.enterPlayModeTime not found");
            currentTime = System.Math.Max(currentTime, (double)enter + 1);
        }

        // queueEventOnly: let the player loop process input like real hardware does.
        IEnumerator Tap(Key key)
        {
            Press(kb[key], queueEventOnly: true);
            yield return null;
            Release(kb[key], queueEventOnly: true);
            yield return null;
        }

        static IEnumerator Settle()
        {
            for (float t = 0; (Game.I == null || Controls.Locked) && t < 5f; t += Time.unscaledDeltaTime) yield return null;
            Assert.IsNotNull(Game.I, "game did not boot");
            Assert.IsFalse(Controls.Locked, "transition stuck");
            yield return null;
        }

        static string Focus
        {
            get
            {
                var u = UitkMenu.ActiveMenu?.Current;
                if (u != null) return u.text;
                var c = Object.FindFirstObjectByType<Menu>()?.Current;
                return c != null ? (c.label != null ? c.label.Text : c.name) : "";
            }
        }

        static List<string> ActiveUiTexts()
        {
            var list = new List<string>();
            foreach (var doc in Object.FindObjectsByType<UIDocument>(FindObjectsSortMode.None))
            {
                if (doc.rootVisualElement == null) continue;
                foreach (var l in doc.rootVisualElement.Query<UiLabel>().ToList())
                    if (l.resolvedStyle.display != DisplayStyle.None && !string.IsNullOrEmpty(l.text))
                        list.Add(l.text);
            }
            foreach (var l in Object.FindObjectsByType<Label>(FindObjectsSortMode.None))
                if (l.gameObject.activeInHierarchy && !string.IsNullOrEmpty(l.Text))
                    list.Add(l.Text);
            return list;
        }

        static GameState State => Game.I.State;

        // Tests share one booted game; each starts from the title screen regardless of order.
        // Flow tests run with QTEs on Auto (the "QTEs disabled" path); QTE tests opt into Standard.
        IEnumerator Begin(QteAssist assist = QteAssist.Auto)
        {
            yield return Settle();
            // Park the mouse off every button (hover focuses), then rebuild the title menu fresh.
            Set(mouse.position, Vector2.zero, queueEventOnly: true);
            yield return null;
            Game.I.EndRun();
            yield return Settle();
            Assert.AreEqual(GameState.MainMenu, State);
            Game.I.Settings.qteAssist = (int)assist;
            Game.I.Settings.qteKey = 0;
            Controls.QteKey = Key.Space;
        }
        static RunState Run => Game.I.Run;

        // NEW GAME now opens Character Creation; flow tests skip it and start a run with the default roster.
        IEnumerator StartRunOnMap()
        {
            Game.I.StartRun();
            yield return WaitFor(() => State == GameState.Map, "the map");
            yield return Settle();
        }

        IEnumerator TapAndSettle(Key key)
        {
            yield return Tap(key);
            yield return Settle();
        }

        static Battle CurrentBattle => Object.FindFirstObjectByType<CombatView>().Battle;

        static IEnumerator WaitFor(System.Func<bool> condition, string what, float timeout = 15f)
        {
            for (float t = 0; !condition() && t < timeout; t += Time.unscaledDeltaTime) yield return null;
            Assert.IsTrue(condition(), "timed out waiting for " + what);
        }

        // Enter on ATTACK opens targeting, Enter again strikes; Enter also dismisses the result panel.
        IEnumerator MashEnterUntilLeaving(GameState state)
        {
            for (float t = 0; t < 30f && State == state; t += Time.unscaledDeltaTime) yield return Tap(Key.Enter);
            yield return Settle();
            Assert.AreNotEqual(state, State, "battle never ended");
        }

        IEnumerator WinBattle()
        {
            for (float t = 0; t < 30f && State == GameState.Combat; t += Time.unscaledDeltaTime)
            {
                var cb = Object.FindFirstObjectByType<CombatView>()?.Battle;
                if (cb != null && cb.Outcome == Outcome.None)
                    foreach (var e in cb.Enemies)
                        if (e.Alive && e.unit.hp > 1) e.unit.hp = 1;
                yield return Tap(Key.Enter);
            }
            yield return Settle();
            Assert.AreNotEqual(GameState.Combat, State, "battle never ended");
        }

        [UnityTest]
        public IEnumerator KeyboardDrivesFullRunToVictory()
        {
            yield return Begin();
            Assert.AreEqual("NEW GAME", Focus);
            yield return Tap(Key.DownArrow); // LOAD GAME
            yield return Tap(Key.DownArrow); // SETTINGS
            Assert.AreEqual("SETTINGS", Focus);
            yield return TapAndSettle(Key.Enter);
            Assert.AreEqual(GameState.Settings, State);
            int volume = Game.I.Settings.volume;
            yield return Tap(Key.LeftArrow);
            Assert.AreEqual(Mathf.Max(0, volume - 1), Game.I.Settings.volume);
            yield return TapAndSettle(Key.Escape);
            if (State == GameState.Settings) yield return TapAndSettle(Key.Enter); // confirm Unsaved Changes Apply modal
            Assert.AreEqual(GameState.MainMenu, State);

            yield return StartRunOnMap();
            var run = Run;
            Assert.AreEqual(MapGenerator.StartLanes, run.Available.Count());

            // Walk all 3 stages of Dungeon 1 by keyboard, always taking the first open node, until Zoroa falls.
            string rewardFocus = "";
            var bossEncountersSeen = new List<string>();
            for (int step = 0; step < 220 && State != GameState.Victory && State != GameState.DungeonClear; step++)
            {
                switch (State)
                {
                    case GameState.Map: yield return TapAndSettle(Key.Enter); break;
                    case GameState.Combat:
                        if (run.Current?.type == NodeType.Boss && !string.IsNullOrEmpty(run.Current.encounter))
                            bossEncountersSeen.Add(run.Current.encounter);
                        yield return WinBattle();
                        break;
                    case GameState.Reward: rewardFocus = Focus; yield return TapAndSettle(Key.Enter); break; // first prize
                    case GameState.Rest:                                                  // recover, continue
                        yield return TapAndSettle(Key.Enter);
                        yield return TapAndSettle(Key.Enter);
                        break;
                    case GameState.Event:                                                 // last choice is always open
                        yield return Tap(Key.UpArrow);
                        yield return TapAndSettle(Key.Enter);
                        yield return TapAndSettle(Key.Enter);
                        break;
                    case GameState.Shop:                                                  // skip shop via Back
                        yield return TapAndSettle(Key.Escape);
                        break;
                    default: Assert.Fail("unexpected state " + State); break;
                }
            }
            Assert.AreEqual(GameState.DungeonClear, State);
            Assert.AreEqual(RunStatus.Victory, run.Status);
            Assert.AreEqual(3, run.Stage);
            Assert.AreEqual(run.TotalStages * MapGenerator.Floors, run.Stats.nodesCompleted);
            CollectionAssert.AreEqual(new[] { "d1_e6", "d1_e7", "d1_boss" }, bossEncountersSeen);
            Assert.IsTrue(run.Stats.bossDefeated);
            Assert.IsTrue(
                run.Relics.Contains(Database.BossRelic) || run.Relics.Count == RelicManager.MaxRelicCapacity,
                $"expected BossRelic or full {RelicManager.MaxRelicCapacity} relics across 3 stages; had [{string.Join(", ", run.Relics)}], boss reward focus was {rewardFocus}");

            yield return TapAndSettle(Key.Enter); // Return to Hub from Dungeon Clear
            Assert.AreEqual(GameState.Hub, State);
        }

        [UnityTest]
        public IEnumerator DefeatEndsRunAndReturnsToTitle()
        {
            yield return Begin();
            yield return StartRunOnMap();
            yield return TapAndSettle(Key.Enter); // node a
            Assert.AreEqual(GameState.Combat, State);
            var battle = CurrentBattle;
            foreach (var p in battle.Party) p.unit.hp = 1;
            foreach (var e in battle.Enemies) e.unit.hp = 9999;
            yield return MashEnterUntilLeaving(GameState.Combat);
            Assert.AreEqual(Outcome.Defeat, battle.Outcome);
            Assert.AreEqual(GameState.GameOver, State);
            Assert.AreEqual(RunStatus.Defeat, Run.Status);
            CollectionAssert.IsEmpty(Run.Available, "no node selection after defeat");
            yield return TapAndSettle(Key.Enter); // Return to Main Menu
            Assert.AreEqual(GameState.MainMenu, State);
        }

        [UnityTest]
        public IEnumerator CombatUiUpdatesAfterEveryAction()
        {
            yield return Begin();
            yield return StartRunOnMap();
            yield return TapAndSettle(Key.Enter); // node a
            Assert.AreEqual(GameState.Combat, State);
            var b = CurrentBattle;

            for (int action = 0; action < 4; action++)
            {
                yield return WaitFor(() => PartyMenuOpen(b), "a party turn");
                int logCount = b.Log.Count;
                yield return Tap(Key.Enter); // ATTACK
                yield return Tap(Key.Enter); // first target
                yield return WaitFor(() => b.Log.Count > logCount, "the attack to resolve");
                yield return null;

                var texts = ActiveUiTexts();
                foreach (var p in b.Party) CollectionAssert.Contains(texts, $"{p.unit.hp}/{p.Stats.maxHp}", p.name + " HP row");
                CollectionAssert.Contains(texts, PixelFont.Wrap(b.Log.Last().ToUpperInvariant(), 158).Split('\n').Last(), "log shows latest line");
                CollectionAssert.Contains(texts, $"ROUND {b.Round}");
            }
        }

        // ---- QTEs with real input ----

        static QtePanel Panel => Object.FindFirstObjectByType<QtePanel>();

        static bool PartyMenuOpen(Battle b)
        {
            var m = UitkMenu.ActiveMenu;
            return b.Active != null && !b.Active.IsEnemy && m != null && m.items.Count > 0 && Panel == null;
        }

        IEnumerator StartFirstBattle(QteAssist assist)
        {
            yield return Begin(assist);
            yield return StartRunOnMap();
            yield return TapAndSettle(Key.Enter); // node a
            Assert.AreEqual(GameState.Combat, State);
        }

        IEnumerator GuardUntil(Battle b, System.Func<bool> stop)
        {
            for (int i = 0; i < 8; i++)
            {
                yield return WaitFor(() => stop() || PartyMenuOpen(b), "a turn");
                if (stop()) yield break;
                yield return Tap(Key.UpArrow); // wraps from ATTACK to GUARD
                yield return Tap(Key.Enter);
            }
            Assert.IsTrue(stop(), "condition never reached");
        }

        // Press the QTE key just before the ideal moment; the press lands on the next frame.
        IEnumerator PressAt(Qte q, System.Func<float> ideal)
        {
            yield return WaitFor(() => q.T >= ideal() - 0.02f || q.Done, "the QTE window");
            Press(kb[Key.Space], queueEventOnly: true);
            yield return null;
            Release(kb[Key.Space], queueEventOnly: true);
            yield return null;
        }

        [UnityTest]
        public IEnumerator AttackTimingQteWithRealInput()
        {
            yield return StartFirstBattle(QteAssist.Standard);
            var b = CurrentBattle;
            yield return WaitFor(() => PartyMenuOpen(b), "a party turn");
            var actor = b.Active;
            var target = b.Enemies.First(e => e.Alive);
            int hp = target.unit.hp, logCount = b.Log.Count;
            yield return Tap(Key.Enter); // ATTACK
            yield return Tap(Key.Enter); // first target
            yield return WaitFor(() => Panel != null, "the attack QTE");
            var q = (TimingQte)Panel.Qte;

            yield return new WaitForSecondsRealtime(0.2f);
            Assert.AreEqual(logCount, b.Log.Count, "nothing resolves while the QTE runs");
            Assert.AreEqual(actor, b.Active, "combat does not advance during the QTE");

            yield return PressAt(q, () => q.target);
            yield return WaitFor(() => q.Done, "the QTE result");
            Assert.AreNotEqual(QteResult.Miss, q.Result, "pressed inside the zone");
            yield return WaitFor(() => b.Log.Count > logCount, "the attack to resolve");
            Assert.Less(target.unit.hp, hp);
        }

        [UnityTest]
        public IEnumerator DefenseQteWithRealInput()
        {
            yield return StartFirstBattle(QteAssist.Standard);
            var b = CurrentBattle;
            yield return GuardUntil(b, () => Panel != null && Panel.Qte is DefenseQte); // no Guard needed for the reaction itself
            var q = (DefenseQte)Panel.Qte;
            int logCount = b.Log.Count;
            yield return PressAt(q, () => q.impacts[0]);
            yield return WaitFor(() => q.Done, "the reaction result");
            Assert.AreNotEqual(QteResult.Miss, q.HitResult(0), "pressed as the hit landed");
            yield return WaitFor(() => b.Log.Count > logCount, "the attack to resolve");
            Assert.IsTrue(b.Log.Skip(logCount).Any(l => l.Contains("parries") || l.Contains("(blocked)")), string.Join(" | ", b.Log.Skip(logCount)));
        }

        [UnityTest]
        public IEnumerator TypingQteWithRealInput()
        {
            yield return StartFirstBattle(QteAssist.Standard);
            var b = CurrentBattle;
            var mage = b.Party.First(p => p.name == "Mage");
            yield return GuardUntil(b, () => PartyMenuOpen(b) && b.Active == mage);
            int mp = mage.unit.mp;
            yield return Tap(Key.DownArrow); // SKILL
            yield return Tap(Key.Enter);     // skill list, IGNIS focused
            yield return Tap(Key.Enter);     // IGNIS -> targeting
            yield return Tap(Key.Enter);     // first enemy
            yield return WaitFor(() => Panel != null, "the typing QTE");
            var q = (TypingQte)Panel.Qte;
            Assert.AreEqual("IGNIS", q.sequence);
            Assert.AreEqual(mp, mage.unit.mp, "MP is spent only when the spell resolves");

            InputSystem.QueueTextEvent(kb, 'q');
            yield return null;
            yield return null;
            Assert.AreEqual(0, q.Index, "wrong letter rejected");
            Assert.AreEqual(1, q.Mistakes);
            foreach (var c in "ignis")
            {
                InputSystem.QueueTextEvent(kb, c);
                yield return null;
            }
            yield return WaitFor(() => q.Done, "the cast");
            Assert.AreEqual(QteResult.Good, q.Result, "one mistake caps the cast at Good");
            yield return WaitFor(() => mage.unit.mp < mp, "the spell to resolve");
        }

        [UnityTest]
        public IEnumerator BattleEndingCancelsActiveQte()
        {
            yield return StartFirstBattle(QteAssist.Standard);
            var b = CurrentBattle;
            yield return WaitFor(() => PartyMenuOpen(b), "a party turn");
            yield return Tap(Key.Enter);
            yield return Tap(Key.Enter);
            yield return WaitFor(() => Panel != null, "the attack QTE");
            var q = Panel.Qte;
            int logCount = b.Log.Count;
            foreach (var e in b.Enemies) e.unit.hp = 0; // the battle ends underneath the QTE
            yield return WaitFor(() => q.Done, "cancellation");
            Assert.IsTrue(q.Cancelled);
            yield return WaitFor(() => ActiveUiTexts().Contains("VICTORY"), "the victory panel");
            Assert.AreEqual(logCount, b.Log.Count, "the cancelled attack never resolved");
            yield return MashEnterUntilLeaving(GameState.Combat);
            Assert.AreEqual(GameState.Reward, State);
        }

        [UnityTest]
        public IEnumerator MouseClickActivatesButton()
        {
            yield return Begin();
            var settingsItem = UitkMenu.ActiveMenu.items.First(it => it.text == "SETTINGS");
            var wb = settingsItem.element.worldBound;
            var panelSize = settingsItem.element.panel.visualTree.layout;
            float sx = Screen.width / Mathf.Max(1f, panelSize.width);
            float sy = Screen.height / Mathf.Max(1f, panelSize.height);
            Vector2 screen = new Vector2(wb.center.x * sx, Screen.height - wb.center.y * sy);

            Set(mouse.position, screen, queueEventOnly: true);
            yield return null;
            yield return null;
            var es = UnityEngine.EventSystems.EventSystem.current;
            var uiMod = Object.FindFirstObjectByType<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            var picked = settingsItem.element.panel.Pick(wb.center);
            Debug.Log($"[DIAG-M2] Focus={Focus} wb={wb} screen={Screen.width}x{Screen.height} target={screen} es={(es != null)} mod={(uiMod != null && uiMod.enabled)} point={uiMod?.point?.action?.ReadValue<Vector2>()} picked={picked?.name}/{picked?.GetType().Name} isOver={es?.IsPointerOverGameObject()}");
            Assert.AreEqual("SETTINGS", Focus, "hover should focus");
            Press(mouse.leftButton, queueEventOnly: true);
            yield return null;
            yield return null;
            Release(mouse.leftButton, queueEventOnly: true);
            yield return null;
            yield return null;
            yield return Settle();
            Assert.AreEqual(GameState.Settings, State);
            yield return TapAndSettle(Key.Escape);
            Assert.AreEqual(GameState.MainMenu, State);
        }

        [UnityTest]
        public IEnumerator PartyStatusHeroSwitchAndTalentModalDoNotLeak()
        {
            yield return Begin();
            yield return StartRunOnMap();

            // Open Party -> Q/E switches selected member -> Enter opens CharacterStatus -> Q switches hero
            yield return Tap(Key.RightArrow); // focus Party button? Let's click Party item cleanly
            var partyBtn = UitkMenu.ActiveMenu.items.First(it => it.text == "Party");
            UitkMenu.ActiveMenu.Focus(partyBtn, false, true);
            yield return TapAndSettle(Key.Enter);
            Assert.AreEqual(GameState.Party, State);

            int beforeIdx = PartyScreen.SelectedIndex;
            yield return Tap(Key.E);
            Assert.AreNotEqual(beforeIdx, PartyScreen.SelectedIndex, "E should cycle party member");

            yield return TapAndSettle(Key.Enter);
            Assert.AreEqual(GameState.CharacterStatus, State);
            var status = Game.I.CurrentScreen as CharacterStatusScreen;
            Assert.IsNotNull(status);
            var firstUnit = status.CurrentUnit;
            yield return Tap(Key.E);
            Assert.AreNotEqual(firstUnit, status.CurrentUnit, "E should switch hero on CharacterStatusScreen");

            // Navigate to Talents and open/close Scroll of Emptiness dialog twice without leaking UitkMenu
            Game.I.Go(GameState.Talents);
            yield return Settle();
            Assert.AreEqual(GameState.Talents, State);
            var screenGo = Game.I.CurrentScreen.UiRoot != null ? Object.FindFirstObjectByType<UitkMenu>()?.gameObject : null;
            Assert.IsNotNull(screenGo);

            var scrollItem = UitkMenu.ActiveMenu.items.First(it => it.text == "Scroll of Emptiness");
            UitkMenu.ActiveMenu.Focus(scrollItem, false, true);
            yield return Tap(Key.Enter);
            Assert.AreEqual(2, screenGo.GetComponents<UitkMenu>().Length, "Modal opens a second UitkMenu");
            yield return Tap(Key.Escape);
            yield return null;
            Assert.AreEqual(1, screenGo.GetComponents<UitkMenu>().Length, "Closing modal must destroy dialog UitkMenu");
        }
    }
}
