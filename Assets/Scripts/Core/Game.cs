using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Obscure
{
    public enum GameState { MainMenu, Settings, CharacterCreation, CharacterStatus, Map, Combat, Reward, Rest, Event, GameOver, Victory }

    // A full-screen state. Builds its own objects under Root; destroyed on exit.
    public abstract class GameScreen
    {
        protected Transform Root { get; private set; }
        protected static Game Game => Game.I;
        protected static RunState Run => Game.I.Run;

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
        public RunState Run { get; private set; }
        public CharacterRoster Roster { get; private set; } = new CharacterRoster();
        public GameSettings Settings { get; private set; }
        public GameState State { get; private set; }
        public GameScreen CurrentScreen => screen;

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
            Settings = GameSettings.Load();
            Settings.Apply();
            Sfx.Init(gameObject);
            fader = Ui.Image(transform, PixelArt.Pixel, 0, 0, Ui.Overlay, Palette.Void);
            fader.transform.localScale = new Vector3(Ui.Width, Ui.Height, 1);
            Go(GameState.MainMenu);
        }

        static void SetupCamera()
        {
            var cam = Camera.main;
            if (cam == null) cam = new GameObject("Main Camera", typeof(Camera)) { tag = "MainCamera" }.GetComponent<Camera>();
            cam.orthographic = true;
            cam.backgroundColor = Palette.Void;
            cam.transform.position = new Vector3(0, 0, -10);
            var ppc = cam.GetComponent<PixelPerfectCamera>();
            if (ppc == null) ppc = cam.gameObject.AddComponent<PixelPerfectCamera>();
            ppc.assetsPPU = 1;
            ppc.refResolutionX = Ui.Width;
            ppc.refResolutionY = Ui.Height;
            ppc.gridSnapping = PixelPerfectCamera.GridSnapping.UpscaleRenderTexture;
            ppc.cropFrame = PixelPerfectCamera.CropFrame.Windowbox;
        }

        // A fresh run: new seed (unless given), new map, active party from Roster. The previous run is dropped here.
        public void StartRun(int? seed = null)
        {
            int s = seed ?? System.Environment.TickCount;
            var party = Roster != null ? Roster.GetActiveParty() : Database.Party.Select(id => Database.Characters[id]).ToList();
            Run = new RunState(MapGenerator.Generate(s), party, s);
            Go(GameState.Map);
        }

        // Which screen resolves a node.
        public static GameState StateFor(NodeDef n) => n.type switch
        {
            NodeType.Treasure => GameState.Reward,
            NodeType.Rest => GameState.Rest,
            NodeType.Event => GameState.Event,
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
            if (transitioning) return;
            transitioning = true;
            Controls.Locked = true;
            StartCoroutine(Transition(next));
        }

        IEnumerator Transition(GameState next)
        {
            yield return Fade(0, 1);
            screen?.Close();
            State = next;
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
            GameState.Map => new MapScreen(),
            GameState.Combat => new CombatScreen(),
            GameState.Reward => new RewardScreen(),
            GameState.Rest => new RestScreen(),
            GameState.Event => new EventScreen(),
            GameState.GameOver => new EndScreen(false),
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
