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
                return u != null ? u.text : "";
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
            yield return Tap(Key.DownArrow); // skips disabled LOAD GAME -> SETTINGS (or LOAD GAME if save exists)
            if (Focus == "LOAD GAME") yield return Tap(Key.DownArrow);
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
                    case GameState.StageClear: yield return TapAndSettle(Key.Enter); break; // primary: Continue to next stage
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
            yield return TapAndSettle(Key.Enter); // pick node a (focus moves to Travel)
            yield return TapAndSettle(Key.Enter); // Travel
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
            yield return TapAndSettle(Key.Enter); // pick node a (focus moves to Travel)
            yield return TapAndSettle(Key.Enter); // Travel
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
                CollectionAssert.Contains(texts, PixelFont.Wrap(b.Log.Last().ToUpperInvariant(), 296).Split('\n').Last(), "log shows latest line");
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
            yield return TapAndSettle(Key.Enter); // pick node a (focus moves to Travel)
            yield return TapAndSettle(Key.Enter); // Travel
            Assert.AreEqual(GameState.Combat, State);
        }

        IEnumerator GuardUntil(Battle b, System.Func<bool> stop)
        {
            for (int i = 0; i < 8; i++)
            {
                yield return WaitFor(() => stop() || PartyMenuOpen(b), "a turn");
                if (stop()) yield break;
                yield return Tap(Key.UpArrow); // wraps from ATTACK to ANALYSIS
                yield return Tap(Key.UpArrow); // moves from ANALYSIS to GUARD
                yield return Tap(Key.Enter);
            }
            Assert.IsTrue(stop(), "condition never reached");
        }

        // Press the QTE key just before the ideal moment; the press lands on the next frame.
        IEnumerator PressAt(Qte q, System.Func<float> ideal, Key key = Key.Space)
        {
            yield return WaitFor(() => q.T >= ideal() - 0.02f || q.Done, "the QTE window");
            Press(kb[key], queueEventOnly: true);
            yield return null;
            Release(kb[key], queueEventOnly: true);
            yield return null;
        }

        // Single physical enemy hits carry a dodge arrow instead of the QTE key.
        static Key KeyFor(DodgeDir d) => d switch
        {
            DodgeDir.Up => Key.UpArrow,
            DodgeDir.Down => Key.DownArrow,
            DodgeDir.Left => Key.LeftArrow,
            DodgeDir.Right => Key.RightArrow,
            _ => Key.Space,
        };

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
            yield return PressAt(q, () => q.impacts[0], KeyFor(q.cues[0]));
            yield return WaitFor(() => q.Done, "the reaction result");
            Assert.AreNotEqual(QteResult.Miss, q.HitResult(0), "pressed as the hit landed");
            yield return WaitFor(() => b.Log.Count > logCount, "the attack to resolve");
            // Without Guard or a shield a Perfect only deflects (Poise rework); a full parry needs one of them.
            Assert.IsTrue(b.Log.Skip(logCount).Any(l => l.Contains("parries") || l.Contains("(blocked)") || l.Contains("(deflected)")), string.Join(" | ", b.Log.Skip(logCount)));
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

        // Regression: Camp → Develop Skills (→ Talents) → Back used to land on the map with the camp node
        // still unresolved, where no path could be taken.
        [UnityTest]
        public IEnumerator CampSkillTreeBack_ReturnsToCamp_AndRunCanContinue()
        {
            yield return Begin();
            yield return StartRunOnMap();

            // No public way to jump nodes: point the run at the first camp, as if the player walked there.
            var camp = Run.Map.nodes.First(n => n.type == NodeType.Rest);
            typeof(RunState).GetProperty(nameof(RunState.Current)).SetValue(Run, camp);
            Game.I.Go(GameState.Rest);
            yield return Settle();
            Assert.AreEqual(GameState.Rest, State);

            Game.I.Go(GameState.Skills);
            yield return Settle();
            yield return TapAndSettle(Key.Escape);
            Assert.AreEqual(GameState.Rest, State, "Back from the skill tree must return to the camp");

            Game.I.Go(GameState.Skills);
            yield return Settle();
            Game.I.Go(GameState.Talents);
            yield return Settle();
            yield return TapAndSettle(Key.Escape);
            Assert.AreEqual(GameState.Skills, State, "Back from talents returns to the skill tree it was opened from");
            yield return TapAndSettle(Key.Escape);
            Assert.AreEqual(GameState.Rest, State, "...and Back again returns to the camp");

            yield return Tap(Key.Enter);            // RECOVER (first camp action) resolves the node
            yield return TapAndSettle(Key.Enter);   // CONTINUE EXPEDITION
            Assert.AreEqual(GameState.Map, State);
            Assert.IsTrue(Run.Available.Any(), "After the camp the map must offer a path forward");
        }

        // Battle rewards: every offered modifier shows its effect, and one is always taken (even on "Skip prize").
        [UnityTest]
        public IEnumerator BattleReward_ModifierIsMandatory_AndShowsItsEffect()
        {
            yield return Begin();
            yield return StartRunOnMap();
            // Stage-boss fights offer modifiers (Dungeon 1 stage 1 boss d1_e6 is a stage boss, not the final one).
            // No public way to jump nodes, so point the run at it as if the player walked there.
            Assert.IsFalse(Run.IsFinalStage, "stage 1 of Dungeon 1 must not be the final stage");
            var stageBoss = Run.Map.nodes.First(n => n.type == NodeType.Boss);
            typeof(RunState).GetProperty(nameof(RunState.Current)).SetValue(Run, stageBoss);
            Game.I.Go(GameState.Combat);
            yield return Settle();
            Assert.AreEqual(GameState.Combat, State);
            yield return WinBattle();
            Assert.AreEqual(GameState.Reward, State);

            var mods = Run.PendingReward.modifierChoices;
            Assert.Greater(mods.Count, 0, "a stage-boss battle offers modifiers");
            var texts = ActiveUiTexts();
            foreach (var m in mods)
            {
                var (_, detail) = RewardGenerator.Describe(new RewardOption { kind = RewardKind.Modifier, id = m.id, element = m.element }, Run.ModifierManager);
                CollectionAssert.Contains(texts, detail, $"effect of {m.id} must be shown on the reward screen");
            }

            // Pick a lasting (not one-shot) modifier so its instance can be counted afterwards.
            var lasting = mods.First(m => !Database.DungeonModifiers[m.id].IsOneShot);
            var (label, _) = RewardGenerator.Describe(new RewardOption { kind = RewardKind.Modifier, id = lasting.id, element = lasting.element }, Run.ModifierManager);
            UitkMenu.ActiveMenu.Focus(UitkMenu.ActiveMenu.items.First(it => it.text == label), false, true);
            yield return Tap(Key.Enter);
            yield return null;

            UitkMenu.ActiveMenu.Focus(UitkMenu.ActiveMenu.items.First(it => it.text == "Skip prize"), false, true);
            yield return TapAndSettle(Key.Enter);
            Assert.AreNotEqual(GameState.Reward, State);
            Assert.IsTrue(Run.ModifierManager.ActiveModifiers.Any(i => i.modifierId == lasting.id),
                "skipping the prize must still grant the chosen modifier");
        }

        [UnityTest]
        public IEnumerator NormalBattleReward_EquipmentWithModifiers_LayoutUnclippedAndClaimsBoth()
        {
            yield return Begin();
            yield return StartRunOnMap();
            yield return TapAndSettle(Key.Enter); // select floor-0 Normal battle
            yield return TapAndSettle(Key.Enter); // Travel
            Assert.AreEqual(GameState.Combat, State);
            yield return WinBattle();
            Assert.AreEqual(GameState.Reward, State);

            var offer = Run.PendingReward;
            Assert.AreEqual(3, offer.modifierChoices.Count, "normal battle must offer 3 modifiers");
            int equipIdx = offer.choices.FindIndex(c => c.kind == RewardKind.Equipment);
            Assert.GreaterOrEqual(equipIdx, 0, "normal battle must offer an equipment card");
            string equipId = offer.choices[equipIdx].id;
            string equipName = Database.Equipment[equipId].name;

            // Select the Equipment reward card so both hero selector and modifier row render in the confirm panel.
            UitkMenu.ActiveMenu.Focus(UitkMenu.ActiveMenu.items.First(it => it.text == equipName), false, true);
            yield return Tap(Key.Enter);
            yield return null;
            yield return null;

            // Verify every interactive control (cards, heroes, modifiers, Back, Store in bag, Confirm) is unclipped & clickable.
            var menu = UitkMenu.ActiveMenu;
            Assert.IsNotNull(menu);
            Assert.IsTrue(menu.items.Any(it => it.text == "Back"), "Back button must exist");
            Assert.IsTrue(menu.items.Any(it => it.text == "Store in bag"), "Store in bag button must exist");
            var confirmItem = menu.items.FirstOrDefault(it => it.text.StartsWith("Confirm"));
            Assert.IsNotNull(confirmItem, "Primary Confirm button must exist");

            foreach (var it in menu.items)
            {
                var el = it.element;
                Assert.IsNotNull(el?.panel, $"Item '{it.text}' not attached to panel");
                var wb = el.worldBound;
                var panelRect = el.panel.visualTree.layout;
                Assert.Greater(wb.width, 0f, $"Item '{it.text}' width <= 0");
                Assert.Greater(wb.height, 0f, $"Item '{it.text}' height <= 0");
                Assert.GreaterOrEqual(wb.yMin, -2f, $"Item '{it.text}' clipped top ({wb})");
                Assert.LessOrEqual(wb.yMax, panelRect.height + 2f, $"Item '{it.text}' clipped bottom ({wb} vs {panelRect})");
                Assert.LessOrEqual(wb.xMax, panelRect.width + 2f, $"Item '{it.text}' clipped right ({wb} vs {panelRect})");
                var picked = el.panel.Pick(wb.center);
                Assert.IsTrue(picked != null && (picked == el || el.Contains(picked)),
                    $"Item '{it.text}' at {wb} blocked or clipped (picked={picked?.name})");
            }

            // Pick a lasting modifier, then confirm & store/equip.
            var lasting = offer.modifierChoices.First(m => !Database.DungeonModifiers[m.id].IsOneShot);
            var (modLabel, _) = RewardGenerator.Describe(
                new RewardOption { kind = RewardKind.Modifier, id = lasting.id, element = lasting.element }, Run.ModifierManager);
            UitkMenu.ActiveMenu.Focus(UitkMenu.ActiveMenu.items.First(it => it.text == modLabel), false, true);
            yield return Tap(Key.Enter);
            yield return null;

            int equipCountBefore = Run.Inventory.equipment.Count;
            UitkMenu.ActiveMenu.Focus(UitkMenu.ActiveMenu.items.First(it => it.text == "Store in bag"), false, true);
            yield return TapAndSettle(Key.Enter);
            Assert.AreEqual(GameState.Map, State);
            Assert.AreEqual(equipCountBefore + 1, Run.Inventory.equipment.Count, "equipment must be granted");
            Assert.IsTrue(Run.ModifierManager.ActiveModifiers.Any(i => i.modifierId == lasting.id),
                "chosen modifier must be granted alongside equipment");
        }

        [UnityTest]
        public IEnumerator PrologueBattleReward_ShowsThreeModifierCardsAndApplies()
        {
            yield return Begin();
            bool prevCleared = Game.I.Settings.prologueCleared;
            Game.I.Settings.prologueCleared = true; // skip blocking tutorial coach prompts
            try
            {
                yield return StartRunOnMap();
                Run.BeginPrologue();
                Game.I.Go(GameState.Map);
                yield return Settle();
                Assert.IsTrue(Run.IsPrologue);

                yield return TapAndSettle(Key.Enter); // select p0
                yield return TapAndSettle(Key.Enter); // Travel
                Assert.AreEqual(GameState.Combat, State);
                yield return WinBattle();
                Assert.AreEqual(GameState.Reward, State);

                var offer = Run.PendingReward;
                Assert.AreEqual(0, offer.choices.Count, "Prologue node 0 has no standard prize cards");
                Assert.AreEqual(3, offer.modifierChoices.Count, "Prologue node 0 must offer 3 modifiers");

                var menu = UitkMenu.ActiveMenu;
                Assert.IsNotNull(menu);
                Assert.IsTrue(menu.items.Any(it => it.text == "Continue to map"), "Continue to map button must exist");
                foreach (var it in menu.items)
                {
                    var el = it.element;
                    Assert.IsNotNull(el?.panel, $"Item '{it.text}' not attached to panel");
                    var wb = el.worldBound;
                    var panelRect = el.panel.visualTree.layout;
                    Assert.Greater(wb.width, 0f, $"Item '{it.text}' width <= 0");
                    Assert.Greater(wb.height, 0f, $"Item '{it.text}' height <= 0");
                    Assert.LessOrEqual(wb.yMax, panelRect.height + 2f, $"Item '{it.text}' clipped bottom ({wb})");
                    var picked = el.panel.Pick(wb.center);
                    Assert.IsTrue(picked != null && (picked == el || el.Contains(picked)),
                        $"Item '{it.text}' at {wb} blocked or clipped (picked={picked?.name})");
                }

                var lasting = offer.modifierChoices.First(m => !Database.DungeonModifiers[m.id].IsOneShot);
                var (modLabel, modDetail) = RewardGenerator.Describe(
                    new RewardOption { kind = RewardKind.Modifier, id = lasting.id, element = lasting.element }, Run.ModifierManager);
                CollectionAssert.Contains(ActiveUiTexts(), modDetail, "modifier effect text must be visible on Prologue reward card");

                UitkMenu.ActiveMenu.Focus(UitkMenu.ActiveMenu.items.First(it => it.text == modLabel), false, true);
                yield return Tap(Key.Enter);
                yield return null;

                UitkMenu.ActiveMenu.Focus(UitkMenu.ActiveMenu.items.First(it => it.text == "Continue to map"), false, true);
                yield return TapAndSettle(Key.Enter);
                Assert.AreEqual(GameState.Map, State);
                Assert.IsTrue(Run.ModifierManager.ActiveModifiers.Any(i => i.modifierId == lasting.id),
                    "chosen Prologue modifier must be granted");
            }
            finally
            {
                Game.I.Settings.prologueCleared = prevCleared;
            }
        }

        [UnityTest]
        public IEnumerator StageBossDefeat_OptionalReturnToHub_ClearsModifiersAndResumesAtNextStage()
        {
            yield return Begin();
            yield return StartRunOnMap();
            Assert.AreEqual(1, Run.Act);
            Assert.AreEqual(1, Run.Stage);

            int retriesBefore = Run.RetriesLeft;
            int hubGoldBefore = Run.HubGold;
            Run.Gold = 75;
            Run.ModifierManager.AddModifier("MOD-001", Run);
            Run.BattleBuffs.Add(new StatusApply { id = StatusId.AttackUp, turns = 2, magnitude = 0.25f });

            // Jump to Stage 1 Mini-Boss (d1_e6), defeat it, and claim reward -> lands on StageClearScreen
            var stageBoss = Run.Map.nodes.First(n => n.type == NodeType.Boss);
            typeof(RunState).GetProperty(nameof(RunState.Current)).SetValue(Run, stageBoss);
            Game.I.Go(GameState.Combat);
            yield return Settle();
            yield return WinBattle();
            Assert.AreEqual(GameState.Reward, State);

            int expectedSpoilsAfterReward = Run.Gold + Run.PendingReward.gold;
            var lasting = Run.PendingReward.modifierChoices.First(m => !Database.DungeonModifiers[m.id].IsOneShot);
            var (modLabel, _) = RewardGenerator.Describe(
                new RewardOption { kind = RewardKind.Modifier, id = lasting.id, element = lasting.element }, Run.ModifierManager);
            UitkMenu.ActiveMenu.Focus(UitkMenu.ActiveMenu.items.First(it => it.text == modLabel), false, true);
            yield return Tap(Key.Enter);
            yield return null;

            UitkMenu.ActiveMenu.Focus(UitkMenu.ActiveMenu.items.First(it => it.text == "Skip prize"), false, true);
            yield return TapAndSettle(Key.Enter);

            Assert.AreEqual(GameState.StageClear, State, "Defeating a stage mini-boss must open StageClearScreen");
            Assert.AreEqual(2, Run.Stage);
            Assert.Greater(Run.ModifierManager.Count, 0, "Modifiers stay active on StageClearScreen if player chooses to continue");
            Assert.AreEqual("Continue to Stage 2", Focus, "Continue to Stage 2 should be the default focused option");

            // Choose "Return to Hub" -> confirm modal -> arrive at Hub
            var returnItem = UitkMenu.ActiveMenu.items.First(it => it.text == "Return to Hub");
            UitkMenu.ActiveMenu.Focus(returnItem, false, true);
            yield return Tap(Key.Enter);
            yield return null;

            var confirmItem = UitkMenu.ActiveMenu.items.First(it => it.text == "RETURN TO HUB");
            UitkMenu.ActiveMenu.Focus(confirmItem, false, true);
            yield return TapAndSettle(Key.Enter);

            Assert.AreEqual(GameState.Hub, State);
            Assert.AreEqual(0, Run.ModifierManager.Count, "Returning to Hub must clear all Dungeon Modifiers");
            Assert.AreEqual(0, Run.BattleBuffs.Count, "Returning to Hub must clear all Event Battle Buffs");
            Assert.AreEqual(0, Run.Gold, "Spoils transferred to Hub Gold");
            Assert.GreaterOrEqual(Run.HubGold, hubGoldBefore + expectedSpoilsAfterReward, "Spoils converted 1:1 into Hub Gold");
            Assert.AreEqual(retriesBefore, Run.RetriesLeft, "Optional mini-boss Hub return spends 0 retries");
            Assert.AreEqual(2, PersistenceCommitHandler.ActiveCommittedSnapshot.NormalizedNextStage());
            Assert.AreEqual(2, SaveManager.GetSlotMetadata(SaveSlotId.AutoSave).GetNormalizedNextStage(), "Hub arrival must auto-save Stage 2 checkpoint");

            // Re-enter Dungeon from Hub (Dungeon Entrance is focused by default) -> starts at Stage 2 with 0 modifiers & 0 spoils
            Assert.AreEqual("Dungeon Entrance", Focus);
            yield return TapAndSettle(Key.Enter);

            Assert.AreEqual(GameState.Map, State);
            Assert.AreEqual(1, Run.Act);
            Assert.AreEqual(2, Run.Stage, "Re-entering dungeon must resume at Stage 2 checkpoint");
            Assert.AreEqual(0, Run.ModifierManager.Count, "Re-entering dungeon must start with 0 Dungeon Modifiers");
            Assert.AreEqual(0, Run.BattleBuffs.Count, "Re-entering dungeon must start with 0 Event Battle Buffs");
            Assert.AreEqual(0, Run.Gold, "Re-entering dungeon must start with 0 Spoils");
        }
    }
}
