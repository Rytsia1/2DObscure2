using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

// Pixel VFX played from sprite sheets in Resources/Fx (exported from pixelfx with --sheet).
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

        // Plays a sheet as a UI Toolkit element with the effect's pivot at (x, y) in the parent's px, scaled `scale` times.
        // flip mirrors right-facing effects about the pivot. loops: plays it that many times, 0 = until the element is removed.
        // centerX: put the frame's horizontal middle at x instead of the pivot (attack effects are drawn from the attacker's
        // feet, so this lands their arc on a target). startAt skips that many seconds of a slow wind-up.
        // It runs on game time, so a hit-stop (Time.timeScale = 0) holds it on its frame; unscaled keeps it playing through one.
        // The element animates and removes itself; it never takes clicks.
        public static VisualElement Spawn(VisualElement parent, string name, float x, float y, float scale = 2f, bool flip = false, int loops = 1,
            bool centerX = false, float startAt = 0f, bool unscaled = false)
        {
            var clip = Load(name);
            if (parent == null || clip == null || clip.frames.Length == 0) return null;
            var f0 = clip.frames[0];
            float px = centerX ? f0.rect.width * scale / 2f : f0.pivot.x * scale, py = (f0.rect.height - f0.pivot.y) * scale; // pivot from the top-left
            var el = new VisualElement { name = "Fx:" + name, pickingMode = PickingMode.Ignore };
            el.style.position = Position.Absolute;
            el.style.left = x - px;
            el.style.top = y - py;
            el.style.width = f0.rect.width * scale;
            el.style.height = f0.rect.height * scale;
            el.style.backgroundImage = new StyleBackground(f0);
            if (flip)
            {
                el.style.transformOrigin = new TransformOrigin(px, py);
                el.style.scale = new Scale(new Vector2(-1, 1));
            }
            parent.Add(el);

            float age = startAt, last = unscaled ? Time.unscaledTime : Time.time;
            long total = loops > 0 ? (long)clip.frames.Length * loops : long.MaxValue;
            el.schedule.Execute(() =>
            {
                float now = unscaled ? Time.unscaledTime : Time.time;
                age += now - last;
                last = now;
                long i = (long)(age * clip.fps);
                if (i >= total) { el.RemoveFromHierarchy(); return; }
                el.style.backgroundImage = new StyleBackground(clip.frames[i % clip.frames.Length]);
            }).Every(16).Until(() => el.parent == null);
            return el;
        }
    }
}
