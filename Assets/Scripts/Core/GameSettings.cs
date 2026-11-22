using System;
using UnityEngine;

// Player preferences, persisted as JSON in PlayerPrefs.
namespace Obscure
{
    [Serializable]
    public class GameSettings
    {
        public const string PrefKey = "obscure.settings";

        public int volume = 8; // 0..10, master (music and sound effects)
        public int musicVolume = 8; // 0..10, music only
        public bool musicMuted;
        public bool fullscreen = true;
        public int qteAssist;  // QteAssist: Standard, Relaxed, Auto (QTEs off)
        public int qteKey;     // index into QteKeys
        public int guardKey;   // index into GuardKeys (QTE Variation Spec Part B)
        public bool singleKeyMode; // accessibility: Guard-key variants also accept the QTE key
        public bool reduceQteDistortions; // accessibility: disables status QTE distortions (Spec Part E)
        public bool reduceMotion; // skips combat screen shake and hit-stop freezes
        public bool prologueCleared; // profile flag: offers Skip Prologue on New Game and silences tutorial prompts (not a gameplay unlock)

        public static readonly UnityEngine.InputSystem.Key[] QteKeys =
        {
            UnityEngine.InputSystem.Key.Space, UnityEngine.InputSystem.Key.Z,
            UnityEngine.InputSystem.Key.J, UnityEngine.InputSystem.Key.Enter,
        };

        public static readonly UnityEngine.InputSystem.Key[] GuardKeys =
        {
            UnityEngine.InputSystem.Key.LeftShift, UnityEngine.InputSystem.Key.RightShift,
            UnityEngine.InputSystem.Key.C, UnityEngine.InputSystem.Key.K,
        };

        public float MusicLevel => musicMuted ? 0f : Mathf.Clamp(musicVolume, 0, 10) / 10f;
        public QteAssist Assist => (QteAssist)Mathf.Clamp(qteAssist, 0, 2);
        public UnityEngine.InputSystem.Key QteKey => QteKeys[Mathf.Clamp(qteKey, 0, QteKeys.Length - 1)];
        public UnityEngine.InputSystem.Key GuardKey => GuardKeys[Mathf.Clamp(guardKey, 0, GuardKeys.Length - 1)];

        public static GameSettings Load()
        {
            try { return JsonUtility.FromJson<GameSettings>(PlayerPrefs.GetString(PrefKey, "{}")) ?? new GameSettings(); }
            catch (ArgumentException) { return new GameSettings(); }
        }

        public void Save()
        {
            PlayerPrefs.SetString(PrefKey, JsonUtility.ToJson(this));
            PlayerPrefs.Save();
            Apply();
        }

        public void Apply()
        {
            AudioListener.volume = volume / 10f;
            MusicManager.I?.SetVolume(MusicLevel);
            Controls.QteKey = QteKey;
            Controls.GuardKey = GuardKey;
            Controls.SingleKeyMode = singleKeyMode;
            if (!Application.isEditor) Screen.fullScreen = fullscreen;
        }
    }
}
