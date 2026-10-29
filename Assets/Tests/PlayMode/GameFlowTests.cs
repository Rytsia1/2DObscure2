using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace Obscure.Tests
{
    // Drives the real game through every state with simulated keyboard/mouse input.
    public class GameFlowTests : InputTestFixture
    {
        const string PrefKey = "obscure.settings";
        Keyboard kb;
        Mouse mouse;
        string savedPrefs;

        public override void Setup()
        {
            base.Setup();
            kb = InputSystem.AddDevice<Keyboard>();
            mouse = InputSystem.AddDevice<Mouse>();
            savedPrefs = PlayerPrefs.GetString(PrefKey, null);
            Time.timeScale = 4f; // combat pacing uses scaled time; menus/fades use unscaled
        }

        public override void TearDown()
        {
            Time.timeScale = 1f;
            if (savedPrefs == null) PlayerPrefs.DeleteKey(PrefKey); else PlayerPrefs.SetString(PrefKey, savedPrefs);
            base.TearDown();
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
                var c = Object.FindFirstObjectByType<Menu>().Current;
                return c.label != null ? c.label.Text : c.name;
            }
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
            foreach (var e in CurrentBattle.Enemies) e.unit.hp = 1;
            yield return MashEnterUntilLeaving(GameState.Combat);
        }

        [UnityTest]
        public IEnumerator KeyboardDrivesFullRunToVictory()
        {
            yield return Begin();
            Assert.AreEqual("START RUN", Focus);

            yield return Tap(Key.DownArrow);
            Assert.AreEqual("SETTINGS", Focus);
            yield return TapAndSettle(Key.Enter);
            Assert.AreEqual(GameState.Settings, State);

            int volume = Game.I.Settings.volume;
            yield return Tap(Key.LeftArrow);
            Assert.AreEqual(Mathf.Max(0, volume - 1), Game.I.Settings.volume);
            yield return TapAndSettle(Key.Escape);
            Assert.AreEqual(GameState.MainMenu, State);

            yield return TapAndSettle(Key.Enter);
            Assert.AreEqual(GameState.Map, State);
            Assert.AreEqual("Node:a", Focus);
            yield return Tap(Key.RightArrow);
            Assert.AreEqual("Node:b", Focus);
            yield return Tap(Key.LeftArrow);

            yield return TapAndSettle(Key.Enter);
            Assert.AreEqual(GameState.Combat, State);
            Assert.AreEqual("a", Run.Current.id);
            yield return WinBattle();
            Assert.AreEqual(GameState.Reward, State);
            yield return TapAndSettle(Key.Enter);
            Assert.AreEqual(GameState.Map, State);
            CollectionAssert.Contains(Run.Cleared, "a");
            Assert.AreEqual(16, Run.Gold);

            // a -> c -> g -> j -> m -> n -> boss, always taking the first focused node or stepping right.
            foreach (var id in new[] { "c", "g", "j", "m", "n", "boss" })
            {
                for (int i = 0; i < 3 && Focus != "Node:" + id; i++) yield return Tap(Key.RightArrow);
                Assert.AreEqual("Node:" + id, Focus);
                yield return TapAndSettle(Key.Enter);
                if (State == GameState.Combat) yield return WinBattle();
                Assert.AreEqual(GameState.Reward, State);
                yield return TapAndSettle(Key.Enter);
            }

            Assert.AreEqual(GameState.Victory, State);
            yield return TapAndSettle(Key.Enter);
            Assert.AreEqual(GameState.MainMenu, State);
            Assert.IsNull(Run);
        }

        [UnityTest]
        public IEnumerator DefeatEndsRunAndReturnsToTitle()
        {
            yield return Begin();
            yield return TapAndSettle(Key.Enter);
            yield return TapAndSettle(Key.Enter);
            Assert.AreEqual(GameState.Combat, State);
            var battle = CurrentBattle;
            foreach (var p in battle.Party) p.unit.hp = 1;
            foreach (var e in battle.Enemies) e.unit.hp = 9999;
            yield return MashEnterUntilLeaving(GameState.Combat);
            Assert.AreEqual(Outcome.Defeat, battle.Outcome);
            Assert.AreEqual(GameState.GameOver, State);
            yield return TapAndSettle(Key.Enter);
            Assert.AreEqual(GameState.MainMenu, State);
        }

        [UnityTest]
        public IEnumerator CombatUiUpdatesAfterEveryAction()
        {
            yield return Begin();
            yield return TapAndSettle(Key.Enter);
            yield return TapAndSettle(Key.Enter);
            Assert.AreEqual(GameState.Combat, State);
            var b = CurrentBattle;

            for (int action = 0; action < 4; action++)
            {
                yield return WaitFor(() =>
                {
                    var m = Object.FindFirstObjectByType<Menu>();
                    return b.Active != null && !b.Active.IsEnemy && m != null && m.items.Count > 0;
                }, "a party turn");
                int logCount = b.Log.Count;
                yield return Tap(Key.Enter); // ATTACK
                yield return Tap(Key.Enter); // first target
                yield return WaitFor(() => b.Log.Count > logCount, "the attack to resolve");
                yield return null;

                var texts = Object.FindObjectsByType<Label>(FindObjectsSortMode.None).Select(l => l.Text).ToList();
                foreach (var p in b.Party) CollectionAssert.Contains(texts, $"{p.unit.hp}/{p.Stats.maxHp}", p.name + " HP row");
                CollectionAssert.Contains(texts, PixelFont.Wrap(b.Log.Last().ToUpperInvariant(), 158).Split('\n').Last(), "log shows latest line");
                CollectionAssert.Contains(texts, $"ROUND {b.Round}");
            }
        }

        // ---- QTEs with real input ----

        static QtePanel Panel => Object.FindFirstObjectByType<QtePanel>();

        static bool PartyMenuOpen(Battle b)
        {
            var m = Object.FindFirstObjectByType<Menu>();
            return b.Active != null && !b.Active.IsEnemy && m != null && m.items.Count > 0 && Panel == null;
        }

        IEnumerator StartFirstBattle(QteAssist assist)
        {
            yield return Begin(assist);
            yield return TapAndSettle(Key.Enter); // START RUN
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
            yield return WaitFor(() => Object.FindObjectsByType<Label>(FindObjectsSortMode.None).Any(l => l.Text == "VICTORY"), "the victory panel");
            Assert.AreEqual(logCount, b.Log.Count, "the cancelled attack never resolved");
            yield return MashEnterUntilLeaving(GameState.Combat);
            Assert.AreEqual(GameState.Reward, State);
        }

        [UnityTest]
        public IEnumerator MouseClickActivatesButton()
        {
            yield return Begin();
            var settings = GameObject.Find("Button:SETTINGS").GetComponent<Selectable>();
            var center = settings.transform.position + new Vector3(settings.size.x / 2f, -settings.size.y / 2f);
            Vector2 screen = Camera.main.WorldToScreenPoint(center);

            Set(mouse.position, screen, queueEventOnly: true);
            yield return null;
            Assert.AreEqual("SETTINGS", Focus, "hover should focus");
            Press(mouse.leftButton, queueEventOnly: true);
            yield return null;
            Release(mouse.leftButton, queueEventOnly: true);
            yield return Settle();
            Assert.AreEqual(GameState.Settings, State);
            yield return TapAndSettle(Key.Escape);
            Assert.AreEqual(GameState.MainMenu, State);
        }
    }
}
