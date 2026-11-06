using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Obscure
{
    public enum GameState { MainMenu, Settings, CharacterCreation, CharacterStatus, OpeningSequence, Map, Combat, Reward, Rest, Event, GameOver, Victory, Inventory, Equipment, Skills, Shop, Hub, Talents, Save, Load, Party, HubShop, Armory, Blacksmith, Temple, MagicShop, DungeonClear }

    // A full-screen state. Builds its own objects under Root; destroyed on exit.
    public abstract class GameScreen
    {
        protected Transform Root { get; private set; }
        protected static Game Game => Game.I;
        protected static RunState Run => Game.I != null ? Game.I.Run : null;

        // Back target for service screens (Status, Equipment, Inventory, Skills, Talents): the hub-side screen
        // they were opened from, else the map during a run, else the title.
        protected static GameState ReturnTarget()
        {
            var prev = Game.I != null ? Game.I.PreviousState : GameState.MainMenu;
            if (prev == GameState.Hub || prev == GameState.Party || prev == GameState.CharacterStatus || prev == GameState.Map) return prev;
            return Game.I != null ? Game.I.LastBase : GameState.MainMenu;
        }

        public void Open(Transform parent)
        {
            Root = new GameObject(GetType().Name).transform;
            Root.SetParent(parent, false);
            Build();
        }

        protected abstract void Build();
        public void Close()
        {
            if (Root == null) return;
            if (Application.isPlaying) Object.Destroy(Root.gameObject);
            else Object.DestroyImmediate(Root.gameObject);
        }
    }

    // Boots the game, owns the run and drives screen transitions.
    public class Game : MonoBehaviour
    {
        public static Game I { get; private set; }
        public static void SetInstance(Game instance) => I = instance;
        public RunState Run { get; private set; }
        public CharacterRoster Roster { get; private set; } = new CharacterRoster();
        public NewGamePartyBuilder NewGameBuilder { get; private set; } = new NewGamePartyBuilder();
        public GameSettings Settings { get; private set; }
        public GameState State { get; private set; }
        public GameState PreviousState { get; private set; }
        public GameState LastBase { get; private set; } = GameState.MainMenu; // last Hub or Map visited
        public GameScreen CurrentScreen => screen;
        public bool HasSavedGame => SaveManager.HasAnySave(out _) || GameSave.HasSavedGame();

        public void ApplyLoadedCampaign(CharacterRoster roster, RunState run)
        {
            Roster = roster;
            Run = run;
        }

        void Update()
        {
            SaveManager.RecordPlaytime(Time.unscaledDeltaTime);
        }

        GameScreen screen;
        SpriteRenderer fader;
        bool transitioning;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (I == null) new GameObject("Game").AddComponent<Game>();
        }

        void Awake()
        {
            I = this;
            // Screen origin at the camera's top-left so children use pixel coordinates.
            transform.position = new Vector3(-Ui.Width / 2, Ui.Height / 2, 0);
            SetupCamera();
            UiPointerDispatcher.Ensure(gameObject);
            Settings = GameSettings.Load();
            Sfx.Init(gameObject);
            MusicManager.Create(gameObject);
            Settings.Apply();
            fader = Ui.Image(transform, PixelArt.Pixel, 0, 0, Ui.Overlay, Color.clear);
            fader.transform.localScale = new Vector3(Ui.Width, Ui.Height, 1);
            Go(GameState.MainMenu);
        }

        void OnDestroy()
        {
            if (I == this) I = null;
        }

        static void SetupCamera()
        {
            var cam = Camera.main;
            if (cam == null) cam = new GameObject("Main Camera", typeof(Camera)) { tag = "MainCamera" }.GetComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = Ui.Height * 0.5f;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 100f;
            cam.backgroundColor = Palette.Void;
            cam.transform.position = new Vector3(0, 0, -10);
            var ppc = cam.GetComponent<PixelPerfectCamera>();
            if (ppc == null) ppc = cam.gameObject.AddComponent<PixelPerfectCamera>();
            ppc.assetsPPU = 1;
            ppc.refResolutionX = Ui.Width;
            ppc.refResolutionY = Ui.Height;
            ppc.gridSnapping = PixelPerfectCamera.GridSnapping.PixelSnapping;
            ppc.cropFrame = PixelPerfectCamera.CropFrame.Windowbox;
        }

        // Main Menu -> New Game (Section 2):
        // Initializes a pending New Game character creation flow without overwriting any existing save,
        // and opens the Character Creation screen to create the 4 initial party members.
        public void StartNewGame()
        {
            if (NewGameBuilder == null)
                NewGameBuilder = new NewGamePartyBuilder();
            else
                NewGameBuilder.StartNewGame();
            Go(GameState.CharacterCreation);
        }

        // Main Menu -> Continue (Section 2 & Save Spec):
        // Loads the most recent valid save and enters the Sanctuary Hub directly.
        public bool ContinueGame()
        {
            if (SaveManager.TryContinue(out _))
                return true;

            if (!GameSave.TryLoadContinue(out var restoredRoster, out var restoredRun))
                return false;
            Roster = restoredRoster;
            Run = restoredRun;
            Go(GameState.Hub);
            return true;
        }

        // Transitions from confirmed party creation directly to the Sanctuary Hub with 600 starter gold.
        public void EnterHubFromCreation(int? seed = null)
        {
            int s = seed ?? System.Environment.TickCount;
            if (Roster != null && NewGameBuilder != null && NewGameBuilder.PendingParty.Count == CharacterRoster.PartySize)
            {
                Roster.CommitConfirmedParty(NewGameBuilder.PendingParty);
            }

            var party = NewGameBuilder != null && NewGameBuilder.PendingParty.Count == CharacterRoster.PartySize
                ? NewGameBuilder.PendingParty
                : (Roster != null && Roster.GetActiveParty().Count == CharacterRoster.PartySize
                    ? Roster.GetActiveParty()
                    : Database.Party.Select(id => Database.Characters[id]).ToList());

            Run = new RunState(MapGenerator.Generate(s, 1), party, s);
            Run.Gold = PersistenceCommitHandler.StarterGold;

            var snapshot = PersistenceCommitHandler.CreateNewGameCommittedSnapshot(party, s, starterGold: PersistenceCommitHandler.StarterGold);
            PersistenceCommitHandler.SetActiveCommittedSnapshot(snapshot);
            Go(GameState.Hub);
            SaveManager.AutoSave(out _);
        }

        // Transitions from confirmed party creation to the Opening Sequence before the first expedition (Section 2 & 12).
        public void BeginOpeningSequence()
        {
            Go(GameState.OpeningSequence);
        }

        // Starts the first expedition after the Opening Sequence using the confirmed New Game party.
        public void BeginFirstExpedition(int? seed = null)
        {
            int s = seed ?? System.Environment.TickCount;
            if (NewGameBuilder != null && NewGameBuilder.IsPartyConfirmed)
            {
                Run = NewGameBuilder.BeginFirstExpedition(Roster, s);
                PersistenceCommitHandler.SetActiveCommittedSnapshot(PersistenceCommitHandler.CreateNewGameCommittedSnapshot(Roster.GetActiveParty(), s));
                Go(GameState.Map);
                return;
            }
            StartRun(s);
        }

        // A fresh expedition run using the existing confirmed party in Roster (does NOT reopen Character Creation).
        public void StartRun(int? seed = null)
        {
            int s = seed ?? System.Environment.TickCount;
            var party = Roster != null ? Roster.GetActiveParty() : Database.Party.Select(id => Database.Characters[id]).ToList();
            Run = new RunState(MapGenerator.Generate(s), party, s);
            PersistenceCommitHandler.SetActiveCommittedSnapshot(PersistenceCommitHandler.CreateNewGameCommittedSnapshot(party, s));
            Go(GameState.Map);
        }

        // Which screen resolves a node.
        public static GameState StateFor(NodeDef n) => n.type switch
        {
            NodeType.Treasure => GameState.Reward,
            NodeType.Rest => GameState.Rest,
            NodeType.Event => GameState.Event,
            NodeType.Shop => GameState.Shop,
            _ => GameState.Combat,
        };

        public void EndRun()
        {
            Run = null;
            Go(GameState.MainMenu);
        }

        // Every state change goes through here: lock input, fade out, swap screen, fade in.
        public void Go(GameState next)
        {
            if (!Application.isPlaying)
            {
                screen?.Close();
                PreviousState = State;
                State = next;
                if (next == GameState.Hub || next == GameState.Map) LastBase = next;
                screen = Create(next);
                if (transform != null)
                    screen.Open(transform);
                return;
            }
            if (transitioning) return;
            transitioning = true;
            Controls.Locked = true;
            MusicManager.I?.PlayFor(next, Run); // music fades alongside the screen fade
            StartCoroutine(Transition(next));
        }

        IEnumerator Transition(GameState next)
        {
            yield return Fade(0, 1);
            screen?.Close();
            PreviousState = State;
            State = next;
            if (next == GameState.Hub || next == GameState.Map) LastBase = next;
            screen = Create(next);
            screen.Open(transform);
            yield return Fade(1, 0);
            Controls.Locked = false;
            transitioning = false;
        }

        IEnumerator Fade(float from, float to)
        {
            const float duration = 0.18f;
            for (float t = 0; t < duration; t += Time.unscaledDeltaTime)
            {
                // Stepped alpha keeps the fade feeling retro rather than smooth.
                fader.color = new Color(Palette.Void.r, Palette.Void.g, Palette.Void.b, Mathf.Round(Mathf.Lerp(from, to, t / duration) * 4) / 4);
                yield return null;
            }
            fader.color = new Color(Palette.Void.r, Palette.Void.g, Palette.Void.b, to);
        }

        static GameScreen Create(GameState s) => s switch
        {
            GameState.MainMenu => new MainMenuScreen(),
            GameState.Settings => new SettingsScreen(),
            GameState.CharacterCreation => new CharacterCreationScreen(),
            GameState.CharacterStatus => new CharacterStatusScreen(),
            GameState.OpeningSequence => new OpeningSequenceScreen(),
            GameState.Map => new MapScreen(),
            GameState.Combat => new CombatScreen(),
            GameState.Reward => new RewardScreen(),
            GameState.Rest => new RestScreen(),
            GameState.Event => new EventScreen(),
            GameState.GameOver => new EndScreen(false),
            GameState.Inventory => new InventoryScreen(),
            GameState.Equipment => new EquipmentScreen(),
            GameState.Skills => new SkillTreeScreen(),
            GameState.Shop => new ShopScreen(),
            GameState.Hub => new HubScreen(),
            GameState.Talents => new TalentScreen(),
            GameState.Save => new SaveScreen(),
            GameState.Party => new PartyScreen(),
            GameState.HubShop => new HubShopScreen(),
            GameState.Armory => new ArmoryScreen(),
            GameState.Blacksmith => new BlacksmithScreen(),
            GameState.Temple => new TempleScreen(),
            GameState.MagicShop => new MagicShopScreen(),
            GameState.DungeonClear => new DungeonClearScreen(),
            GameState.Load => new LoadScreen(Game.I != null && Game.I.State == GameState.Hub ? GameState.Hub : GameState.MainMenu),
            _ => new EndScreen(true),
        };

        public void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
