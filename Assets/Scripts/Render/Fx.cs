using System.Collections.Generic;
using UnityEngine;

// One-shot pixel VFX played from sprite sheets in Resources/Fx (exported from pixelfx with --sheet).
// Each sheet has a sibling .json; only meta.pixelfx is read (fps, frame size, columns, pivot).
namespace Obscure
{
    public static class Fx
    {
        [System.Serializable] class Sheet { public SheetMeta meta; }
        [System.Serializable] class SheetMeta { public PixelfxMeta pixelfx; }
        [System.Serializable] class PixelfxMeta { public int fps, frames, columns; public int[] frameSize, pivot; }

        public class Clip
        {
            public Sprite[] frames;
            public float fps;
            public bool grounded; // pivot sits near the bottom of the frame: anchor at the target's feet, not its center
        }

        static readonly Dictionary<string, Clip> cache = new Dictionary<string, Clip>();

        public static Clip Load(string name)
        {
            if (cache.TryGetValue(name, out var clip) && clip != null && clip.frames != null && clip.frames.Length > 0 && clip.frames[0] != null)
                return clip;
            var tex = Resources.Load<Texture2D>("Fx/" + name);
            var json = Resources.Load<TextAsset>("Fx/" + name);
            if (tex == null || json == null) { cache[name] = null; return null; }
            tex.filterMode = FilterMode.Point;

            var m = JsonUtility.FromJson<Sheet>(json.text).meta.pixelfx;
            int w = m.frameSize[0], h = m.frameSize[1];
            var pivot = new Vector2(m.pivot[0] / (float)w, 1f - m.pivot[1] / (float)h);
            var frames = new Sprite[m.frames];
            for (int i = 0; i < m.frames; i++)
            {
                int col = i % m.columns, row = i / m.columns;
                // Sheet rows run top-down; texture rects run bottom-up.
                frames[i] = Sprite.Create(tex, new Rect(col * w, tex.height - (row + 1) * h, w, h), pivot, 1f, 0, SpriteMeshType.FullRect);
            }
            return cache[name] = new Clip { frames = frames, fps = m.fps, grounded = m.pivot[1] > h * 0.6f };
        }

        // x, y in UI pixels (top-left origin, y down), given as the target's center and feet; flip mirrors right-facing effects.
        public static GameObject Play(Transform parent, string name, int x, int centerY, int feetY, bool flip = false, int order = Ui.Widget + 8)
        {
            var clip = Load(name);
            if (clip == null || clip.frames.Length == 0) return null;
            var sr = Ui.Image(parent, clip.frames[0], x, clip.grounded ? feetY : centerY, order);
            sr.gameObject.name = "Fx:" + name;
            if (flip) sr.transform.localScale = new Vector3(-1, 1, 1);
            sr.gameObject.AddComponent<FxPlayer>().clip = clip;
            return sr.gameObject;
        }
    }

    public class FxPlayer : MonoBehaviour
    {
        public Fx.Clip clip;
        SpriteRenderer sr;
        float age;

        void Awake() => sr = GetComponent<SpriteRenderer>();

        void Update()
        {
            age += Time.deltaTime;
            int i = (int)(age * clip.fps);
            if (i >= clip.frames.Length) { Destroy(gameObject); return; }
            sr.sprite = clip.frames[i];
        }
    }
}
