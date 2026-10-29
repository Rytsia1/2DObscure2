using System;
using UnityEngine;

// Player preferences, persisted as JSON in PlayerPrefs.
namespace Obscure
{
    [Serializable]
    public class GameSettings
    {
        const string PrefKey = "obscure.settings";

        public int volume = 8; // 0..10
        public bool fullscreen = true;
        public int qteAssist;  // QteAssist: Standard, Relaxed, Auto (QTEs off)
        public int qteKey;     // index into QteKeys

        public static readonly UnityEngine.InputSystem.Key[] QteKeys =
        {
            UnityEngine.InputSystem.Key.Space, UnityEngine.InputSystem.Key.Z,
            UnityEngine.InputSystem.Key.J, UnityEngine.InputSystem.Key.Enter,
        };

        public QteAssist Assist => (QteAssist)Mathf.Clamp(qteAssist, 0, 2);
        public UnityEngine.InputSystem.Key QteKey => QteKeys[Mathf.Clamp(qteKey, 0, QteKeys.Length - 1)];

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
            Controls.QteKey = QteKey;
            if (!Application.isEditor) Screen.fullScreen = fullscreen;
        }
    }
}
