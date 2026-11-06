using System;
using System.Collections.Generic;
using UnityEngine;

// Sprite-based UI Engine conforming to the "Starlit Reliquary" visual system.
// Reference resolution: 640x360 (16:9), top-left pixel coordinates (0..640, 0..360), y down.
namespace Obscure
{
    public enum Align { Left, Center, Right }
    // Card = selectable list/grid card (.pnl.flat → .pnl.gold on focus); label sits top-left.
    public enum ButtonStyle { Slab, Primary, Flat, Danger, Card }
    public enum TagKind { Cls, Keep, Temp, Cond, Gold }

    public static class Ui
    {
        public const int Width = 640, Height = 360;

        // Sorting layers by convention.
        public const int Back = 0, World = 10, Panels = 20, Text = 30, Widget = 40, Overlay = 1000;

        public static SpriteRenderer Image(Transform parent, Sprite sprite, int x, int y, int order, Color? tint = null)
        {
            var go = new GameObject(sprite ? sprite.name : "image");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(x, -y, 0);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = order;
            sr.color = tint ?? Color.white;
            return sr;
        }

        public static Label Label(Transform parent, string text, int x, int y, Color color, Align align = Align.Left, int scale = 1, int order = Text, FontKind font = FontKind.DotGothic)
        {
            var sr = Image(parent, null, 0, 0, order, color);
            var label = sr.gameObject.AddComponent<Label>();
            label.Init(sr, x, y, align, scale, font);
            label.Set(text);
            return label;
        }

        public static Label Label(Transform parent, string text, int x, int y, Color color, Align align, int scale, FontKind font, int order = Text)
        {
            return Label(parent, text, x, y, color, align, scale, order, font);
        }

        // Primary panel: Ink 2 fill, Ink 5 border, gold rivets on the corners
        public static SpriteRenderer Panel(Transform parent, int x, int y, int w, int h, Color? fill = null, Color? border = null) =>
            Image(parent, PixelArt.Box(w, h, fill ?? Palette.DeepCharcoal, border ?? Palette.DarkBorder), x, y, Panels);

        // Secondary card / nested container: raised Ink 3 with Ink 5 border
        public static SpriteRenderer Card(Transform parent, int x, int y, int w, int h, Color? fill = null, Color? border = null) =>
            Image(parent, PixelArt.Box(w, h, fill ?? Palette.MidnightNavy, border ?? Palette.DarkBorder), x, y, Panels + 1);

        // Enemy card (.ec): notched corners
        public static SpriteRenderer EnemyCard(Transform parent, int x, int y, int w, int h, Color? fill = null, Color? border = null) =>
            Image(parent, PixelArt.NotchedBox(w, h, fill ?? Palette.MidnightNavy, border ?? Palette.DarkBorder), x, y, Panels + 1);

        // Double border card (.rar.r-leg / .rar.r-myt)
        public static SpriteRenderer DoubleBorderCard(Transform parent, int x, int y, int w, int h, Color? fill = null, Color? border = null) =>
            Image(parent, PixelArt.DoubleBorderBox(w, h, fill ?? Palette.DeepCharcoal, border ?? Palette.Gold2), x, y, Panels + 1);

        // Ink slab button; focus lifts it into a gold frame with gold text and the pixel hand-pointer.
        // Primary = the one gold call-to-action per view; Flat = menu-list item (.mi) with a gold wash on focus; Danger = destructive alert.
        public static Selectable Button(Transform parent, string text, int x, int y, int w, Action onClick, int h = 18, bool interactable = true, bool showCursor = true, FontKind font = FontKind.DotGothic, ButtonStyle style = ButtonStyle.Slab)
        {
            UiPointerDispatcher.Ensure();
            var go = new GameObject("Button:" + text);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(x, -y, 0);
            var box = Image(go.transform, null, 0, 0, Widget);
            int gHeight = font == FontKind.DotGothic ? PixelFont.DotGothicGlyphHeight : font == FontKind.Serif ? PixelFont.SerifGlyphHeight : PixelFont.GlyphHeight;
            bool flat = style == ButtonStyle.Flat, card = style == ButtonStyle.Card;
            var label = Label(go.transform, text, flat || card ? 6 : w / 2, card ? 4 : (h - gHeight) / 2, Palette.WarmIvory, flat || card ? Align.Left : Align.Center, order: Widget + 1, font: font);
            var cursor = showCursor ? Image(go.transform, PixelArt.Pointer, -10, (h - 7) / 2, Widget + 1) : null;
            var sel = go.AddComponent<Selectable>();
            sel.size = new Vector2Int(w, h);
            sel.onClick = onClick;
            sel.label = label;
            sel.interactable = interactable;
            if (!Selectable.All.Contains(sel)) Selectable.All.Add(sel);
            sel.visual = on =>
            {
                box.sprite = style == ButtonStyle.Flat ? (on ? PixelArt.MenuHighlight(w, h) : null)
                    : style == ButtonStyle.Primary ? PixelArt.PrimaryBox(w, h, on, sel.interactable)
                    : style == ButtonStyle.Danger ? PixelArt.DangerBox(w, h, on, sel.interactable)
                    : card ? PixelArt.Box(w, h, Palette.Ink2, on || sel.selected ? Palette.Gold3 : Palette.Ink4)
                    : PixelArt.ButtonBox(w, h, on, sel.interactable);
                label.Color = !sel.interactable ? Palette.MutedGray
                    : card ? (on || sel.selected ? Palette.Gold1 : Palette.Mist1)
                    : style == ButtonStyle.Primary ? Palette.Ink1
                    : style == ButtonStyle.Danger ? (on ? Palette.WarmIvory : Palette.HpLow)
                    : on ? Palette.AntiqueGoldBright : Palette.WarmIvory;
                if (cursor != null) cursor.gameObject.SetActive(on);
            };
            sel.visual(false);
            return sel;
        }

        // Modern gauge with outer bezel and two-tone gradient fill
        public static SpriteRenderer Gauge(Transform parent, int x, int y, int w, int h, float ratio, Color fill, Color highlight, Color? bg = null, int order = Widget)
        {
            return Image(parent, PixelArt.GaugeBar(w, h, ratio, fill, highlight, bg ?? Palette.OverlayCharcoal), x, y, order);
        }

        // Decorative horizontal divider with jewel center
        public static SpriteRenderer Divider(Transform parent, int x, int y, int w, Color? lineCol = null, Color? centerCol = null, int order = Panels + 1)
        {
            return Image(parent, PixelArt.Divider(w, lineCol ?? Palette.DarkBorder, centerCol ?? Palette.AntiqueGold), x, y, order);
        }

        // Solid rectangle (rules, scrims, fills).
        public static SpriteRenderer Rect(Transform parent, int x, int y, int w, int h, Color color, int order = Panels + 1)
        {
            var sr = Image(parent, PixelArt.Pixel, x, y, order, color);
            sr.transform.localScale = new Vector3(w, h, 1);
            return sr;
        }

        // Panel header (.ph): gold diamond + gold title over an ink rule spanning the panel width.
        public static Label Header(Transform parent, string text, int x, int y, int w, int order = Text, FontKind font = FontKind.DotGothic)
        {
            Image(parent, PixelArt.Diamond(5, Palette.Gold2), x, y + 1, order);
            Rect(parent, x - 6, y + 13, w, 1, Palette.Ink4, order);
            return Label(parent, text, x + 9, y, Palette.Gold2, order: order, font: font);
        }

        // Status tag (.st): ▲ buff, ▼ debuff, ● neutral
        public static GameObject StatusBadge(Transform parent, string code, string timeOrStack, bool isBuff, int x, int y, int order = Widget)
        {
            var go = new GameObject("Status:" + code);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(x, -y, 0);
            Color fill = isBuff ? Palette.Hex(0x0E2818) : Palette.Hex(0x280E12);
            Color border = isBuff ? Palette.Hp : Palette.HpLow;
            Color textCol = isBuff ? Palette.Hp : Palette.HpLow;
            string prefix = isBuff ? "▲ " : "▼ ";
            string text = prefix + code + (string.IsNullOrEmpty(timeOrStack) ? "" : " " + timeOrStack);
            int w = Mathf.Max(28, PixelFont.Measure(text, FontKind.DotGothic) + 6);
            Image(go.transform, PixelArt.Badge(w, 14, fill, border), 0, 0, order);
            Label(go.transform, text, w / 2, 2, textCol, Align.Center, order: order + 1, font: FontKind.DotGothic);
            return go;
        }

        // Currency tag (.cur): solid gold frame for Hub Gold, dashed ember frame for Run Gold
        public static GameObject CurrencyTag(Transform parent, int amount, bool isHubGold, int x, int y, int order = Widget)
        {
            var go = new GameObject("Currency:" + (isHubGold ? "Hub" : "Run"));
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(x, -y, 0);
            string lbl = isHubGold ? $"{amount:#,##0} HUB GOLD" : $"{amount:#,##0} RUN GOLD";
            Color bg = isHubGold ? Palette.Hex(0x221A08) : Palette.Hex(0x241008);
            Color border = isHubGold ? Palette.Gold2 : Palette.Ember2;
            Color textCol = isHubGold ? Palette.Gold1 : Palette.Ember1;
            int w = Mathf.Max(84, PixelFont.Measure(lbl, FontKind.DotGothic) + 16);
            Image(go.transform, PixelArt.Badge(w, 18, bg, border), 0, 0, order);
            Image(go.transform, PixelArt.Coin(7, isHubGold), 6, 5, order + 1);
            Label(go.transform, lbl, 16, 3, textCol, Align.Left, order: order + 1, font: FontKind.DotGothic);
            return go;
        }

        // Key prompt (.hint): keycaps then the action. Returns the x where the next hint can start.
        public static int KeyHint(Transform parent, int x, int y, string action, params string[] keys)
        {
            foreach (var k in keys)
            {
                int w = Mathf.Max(11, PixelFont.Measure(k, FontKind.DotGothic) + 6);
                Badge(parent, k, x, y, w, 11, Palette.Mist2, Palette.Hex(0x8790AD), Palette.Ink1, font: FontKind.DotGothic);
                x += w + 2;
            }
            Label(parent, action, x + 2, y + 2, Palette.Mist2, font: FontKind.DotGothic);
            return x + 2 + PixelFont.Measure(action, FontKind.DotGothic) + 14;
        }

        // Tag chip (.tag): cls = ink, keep = gold solid, temp = ember dashed, cond = mist dashed, gold = class/selected accent.
        // Returns the chip width so callers can lay tags out in a row.
        public static int Tag(Transform parent, string text, int x, int y, TagKind kind = TagKind.Cls, int h = 11, int order = Widget)
        {
            text = (text ?? "").ToUpperInvariant();
            int w = PixelFont.Measure(text, FontKind.Interface) + 8;
            var (fill, border, ink) = kind switch
            {
                TagKind.Keep => (Palette.Hex(0x1D1608), Palette.Gold3, Palette.Gold1),
                TagKind.Temp => (Palette.Hex(0x22120A), Palette.Ember2, Palette.Ember1),
                TagKind.Cond => (Color.clear, Palette.Mist3, Palette.Mist2),
                TagKind.Gold => (Palette.Ink3, Palette.Gold3, Palette.Gold1),
                _ => (Palette.Ink3, Palette.Ink5, Palette.Mist1),
            };
            bool dashed = kind == TagKind.Temp || kind == TagKind.Cond;
            Image(parent, dashed ? PixelArt.DashedBadge(w, h, fill, border) : PixelArt.Badge(w, h, fill, border), x, y, order);
            Label(parent, text, x + 4, y + (h - PixelFont.GlyphHeight) / 2, ink, order: order + 1, font: FontKind.Interface);
            return w;
        }

        // Inventory / equipment slot (.slot): ink well, rarity-tinted border, item icon, rarity letter top-left.
        // Empty slots show the slot's silhouette dimmed so the player can still tell what goes there.
        public static void ItemSlot(Transform parent, int x, int y, int size, EquipmentDef item, EquipmentSlot slot, bool selected = false, int order = Widget, string count = null)
        {
            Color rc = item != null ? Palette.ForRarity(item.rarity) : Palette.Ink4;
            Color border = selected ? Palette.Gold2 : item != null ? Color.Lerp(rc, Palette.Ink4, 0.45f) : Palette.Ink4;
            Image(parent, PixelArt.Box(size, size, Palette.Ink0, border), x, y, order);
            if (selected) Image(parent, PixelArt.Badge(size + 4, size + 4, Color.clear, Palette.Gold3), x - 2, y - 2, order);
            var icon = PixelArt.FromRows(Art.GearIcon(slot, item));
            int iw = (int)icon.rect.width, ih = (int)icon.rect.height;
            Image(parent, icon, x + (size - iw) / 2, y + (size - ih) / 2, order + 1, item != null ? rc : Palette.Ink4);
            if (item != null)
            {
                Image(parent, PixelArt.Badge(7, 8, rc, rc), x, y, order + 2);
                Label(parent, Palette.RarityBadge(item.rarity), x + 1, y + 1, Palette.Ink0, order: order + 3, font: FontKind.Interface);
            }
            if (!string.IsNullOrEmpty(count))
                Label(parent, count, x + size - 2, y + size - 9, Palette.Mist1, Align.Right, order: order + 3, font: FontKind.Interface);
        }

        // Relic token (.relic): octagon in the relic's rarity color with a 2-letter code; empty = dashed ink.
        public static void Relic(Transform parent, int x, int y, int size, RelicDef relic, int order = Widget)
        {
            Color c = relic != null ? RelicColor(relic) : Palette.Ink5;
            Image(parent, PixelArt.Octagon(size, Palette.Ink0, c, relic == null), x, y, order);
            if (relic != null) Label(parent, RelicCode(relic), x + size / 2, y + (size - PixelFont.GlyphHeight) / 2, c, Align.Center, order: order + 1, font: FontKind.Interface);
        }

        public static Color RelicColor(RelicDef r) => r.proposedRarity switch
        {
            RelicProposedRarity.Uncommon => Palette.RarityUncommon,
            RelicProposedRarity.Rare => Palette.RarityRare,
            RelicProposedRarity.Epic => Palette.RarityEpic,
            _ => Palette.RarityCommon,
        };

        public static string RelicCode(RelicDef r)
        {
            var words = (r?.name ?? "?").Split(new[] { ' ', '-' }, StringSplitOptions.RemoveEmptyEntries);
            if (words.Length == 0) return "?";
            return (words.Length >= 2 ? $"{words[0][0]}{words[1][0]}" : words[0].Substring(0, Math.Min(2, words[0].Length))).ToUpperInvariant();
        }

        // Square chip
        public static SpriteRenderer Badge(Transform parent, string text, int x, int y, int w, int h, Color fill, Color border, Color textColor, Align align = Align.Center, int order = Widget, FontKind font = FontKind.DotGothic)
        {
            var box = Image(parent, PixelArt.Badge(w, h, fill, border), x, y, order);
            int gHeight = font == FontKind.DotGothic ? PixelFont.DotGothicGlyphHeight : font == FontKind.Serif ? PixelFont.SerifGlyphHeight : PixelFont.GlyphHeight;
            Label(parent, text, align == Align.Center ? x + w / 2 : align == Align.Right ? x + w - 3 : x + 3, y + (h - gHeight) / 2, textColor, align, order: order + 1, font: font);
            return box;
        }
    }

    public class Label : MonoBehaviour
    {
        SpriteRenderer sr;
        int x, y, scale;
        Align align;
        FontKind font;
        public string Text { get; private set; }

        public void Init(SpriteRenderer renderer, int px, int py, Align a, int s, FontKind f = FontKind.DotGothic)
        {
            sr = renderer; x = px; y = py; align = a; scale = s; font = f;
            transform.localScale = new Vector3(s, s, 1);
        }

        public Color Color { set => sr.color = value; }

        public void Set(string text, FontKind? overrideFont = null)
        {
            Text = text ?? "";
            if (overrideFont.HasValue) font = overrideFont.Value;
            sr.sprite = PixelFont.Render(Text, font);
            int w = (int)sr.sprite.rect.width * scale;
            int ox = align == Align.Center ? w / 2 : align == Align.Right ? w : 0;
            transform.localPosition = new Vector3(x - ox, -y, 0);
        }
    }

    [ExecuteAlways]
    public class Selectable : MonoBehaviour
    {
        public static readonly List<Selectable> All = new List<Selectable>();

        public Vector2Int size;
        public Action onClick;
        public Action<int> onAdjust;
        public Action<bool> visual;
        public Label label;
        public bool interactable = true;
        public bool selected; // persistent "chosen" state for Card style (gold frame without focus)
        public string tooltip;
        public Menu menu;

        void Awake()
        {
            All.RemoveAll(s => s == null);
            if (!All.Contains(this)) All.Add(this);
        }

        void OnEnable()
        {
            All.RemoveAll(s => s == null);
            if (!All.Contains(this)) All.Add(this);
        }

        void OnDisable()
        {
            All.Remove(this);
            All.RemoveAll(s => s == null);
        }

        void OnDestroy()
        {
            All.Remove(this);
            All.RemoveAll(s => s == null);
        }

        public bool Contains(Vector2 world)
        {
            if (float.IsNaN(world.x) || float.IsNaN(world.y)) return false;
            var p = (Vector2)transform.position;
            return world.x >= p.x && world.x <= p.x + size.x && world.y <= p.y && world.y >= p.y - size.y;
        }
    }

    public class UiPointerDispatcher : MonoBehaviour
    {
        public static UiPointerDispatcher Instance { get; private set; }
        Selectable currentHovered;
        Vector2 lastMousePos = new Vector2(float.NaN, float.NaN);

        public static void Ensure(GameObject fallbackHost = null)
        {
            if (Instance != null) return;
            var host = fallbackHost ?? (Game.I != null ? Game.I.gameObject : null);
            if (host == null)
            {
                var go = new GameObject("UiPointerDispatcher");
                Instance = go.AddComponent<UiPointerDispatcher>();
                if (Application.isPlaying) DontDestroyOnLoad(go);
            }
            else
            {
                Instance = host.GetComponent<UiPointerDispatcher>() ?? host.AddComponent<UiPointerDispatcher>();
            }
        }

        void Awake()
        {
            if (Instance == null) Instance = this;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void Update()
        {
            if (Controls.Locked) return;

            Vector2 mouse = Controls.MouseWorld;
            bool mouseMoved = mouse != lastMousePos;
            lastMousePos = mouse;

            Selectable hit = null;
            if (!float.IsNaN(mouse.x) && !float.IsNaN(mouse.y))
            {
                for (int i = Selectable.All.Count - 1; i >= 0; i--)
                {
                    var s = Selectable.All[i];
                    if (s != null && s.isActiveAndEnabled && s.Contains(mouse))
                    {
                        hit = s;
                        break;
                    }
                }
            }

            if (mouseMoved && hit != currentHovered)
            {
                if (currentHovered != null && currentHovered.visual != null)
                {
                    if (currentHovered.menu == null || currentHovered.menu.Current != currentHovered)
                        currentHovered.visual(false);
                }

                currentHovered = hit;

                if (currentHovered != null)
                {
                    if (currentHovered.menu != null)
                    {
                        currentHovered.menu.Focus(currentHovered, false);
                    }
                    else
                    {
                        currentHovered.visual?.Invoke(true);
                    }
                }
            }

            if (Controls.Click)
            {
                if (hit != null)
                {
                    if (hit.interactable)
                    {
                        Sfx.Play(Sound.Confirm);
                        if (hit.menu != null) hit.menu.Focus(hit, false);
                        hit.onClick?.Invoke();
                    }
                    else
                    {
                        Sfx.Play(Sound.Deny);
                    }
                }
            }
        }
    }

    public class Menu : MonoBehaviour
    {
        public static Menu ActiveMenu;
        public readonly List<Selectable> items = new List<Selectable>();
        public Action onBack;
        public Action<Selectable> onFocus;
        int index, bornFrame;

        void Awake()
        {
            bornFrame = Time.frameCount;
            ActiveMenu = this;
        }

        void OnEnable()
        {
            ActiveMenu = this;
        }

        void OnDestroy()
        {
            if (ActiveMenu == this) ActiveMenu = null;
        }

        public Selectable Current => index >= 0 && index < items.Count ? items[index] : null;

        public Selectable Add(Selectable s)
        {
            if (s == null) return null;
            s.menu = this;
            items.Add(s);
            if (items.Count - 1 == index) Focus(index, false, true);
            return s;
        }

        public void Focus(int i, bool sound = true, bool force = false)
        {
            if (i < 0 || i >= items.Count) return;
            if (i == index && !force) return;
            Current?.visual?.Invoke(false);
            index = i;
            Current?.visual?.Invoke(true);
            ActiveMenu = this;
            if (sound) Sfx.Play(Sound.Move);
            onFocus?.Invoke(Current);
        }

        public void Focus(Selectable s, bool sound = true)
        {
            int idx = items.IndexOf(s);
            if (idx >= 0) Focus(idx, sound);
        }

        void Update()
        {
            if (Controls.Locked || items.Count == 0 || Time.frameCount == bornFrame) return;

            // Only the currently active menu processes keyboard / gamepad navigation
            if (ActiveMenu != null && ActiveMenu != this) return;

            var adjust = Current?.onAdjust;
            if (adjust != null && (Controls.Left || Controls.Right)) { adjust(Controls.Left ? -1 : 1); Sfx.Play(Sound.Move); }
            else if (Controls.Up || Controls.Left) Focus((index - 1 + items.Count) % items.Count);
            else if (Controls.Down || Controls.Right) Focus((index + 1) % items.Count);

            if (Controls.Confirm) Activate();
            else if (Controls.Back && onBack != null) { Sfx.Play(Sound.Back); onBack(); }
        }

        public void Activate()
        {
            if (Current == null || Current.onClick == null) return;
            if (!Current.interactable) { Sfx.Play(Sound.Deny); return; }
            Sfx.Play(Sound.Confirm);
            Current.onClick();
        }
    }

    public class Backdrop : MonoBehaviour
    {
        readonly List<(Transform t, float speed, float phase, float x)> embers = new List<(Transform, float, float, float)>();
        readonly List<(Transform t, float phase, float rate)> stars = new List<(Transform, float, float)>();

        public static Backdrop Create(Transform parent, bool moon, int embersCount, params Color[] sky)
        {
            var go = new GameObject("Backdrop");
            go.transform.SetParent(parent, false);
            var b = go.AddComponent<Backdrop>();
            // Default: Starlit Reliquary night sky
            Color[] finalSky = sky != null && sky.Length > 0 ? sky : new[] { Palette.Hex(0x070B1C), Palette.Hex(0x101A3A), Palette.Hex(0x28284F), Palette.Hex(0x3A2F55) };
            Ui.Image(go.transform, PixelArt.Gradient(Ui.Width, Ui.Height, finalSky), 0, 0, Ui.Back);
            if (moon)
            {
                // Cinematic moon positioned elegantly in the upper right
                Ui.Image(go.transform, PixelArt.Disc(22, Palette.Shade), 510, 24, Ui.Back + 1);
                Ui.Image(go.transform, PixelArt.Disc(19, Palette.WarmIvory), 513, 27, Ui.Back + 2);
                Ui.Image(go.transform, PixelArt.Disc(16, Palette.Shade), 522, 26, Ui.Back + 3);

                var rngStars = new System.Random(42);
                for (int i = 0; i < 45; i++)
                {
                    int sx = rngStars.Next(16, Ui.Width - 16);
                    int sy = rngStars.Next(10, 150);
                    if (Mathf.Abs(sx - 515) < 32 && Mathf.Abs(sy - 30) < 32) continue;
                    var star = Ui.Image(go.transform, PixelArt.Pixel, sx, sy, Ui.Back + 1, Palette.MutedGray);
                    b.stars.Add((star.transform, (float)rngStars.NextDouble() * Mathf.PI * 2, 1.5f + (float)rngStars.NextDouble() * 2f));
                }
            }
            var rng = new System.Random(7);
            for (int i = 0; i < embersCount; i++)
            {
                var sr = Ui.Image(go.transform, PixelArt.Pixel, 0, 0, Ui.Back + 4, i % 3 == 0 ? Palette.AntiqueGold : Palette.Ember);
                b.embers.Add((sr.transform, 6f + rng.Next(10), rng.Next(1000) / 100f, rng.Next(Ui.Width)));
            }
            return b;
        }

        void Update()
        {
            float time = Time.unscaledTime;
            foreach (var (t, speed, phase, x) in embers)
            {
                float y = Ui.Height - ((time * speed + phase * 40f) % (Ui.Height + 10));
                t.localPosition = new Vector3(Mathf.Round(x + Mathf.Sin(time + phase) * 4f), -Mathf.Round(y), 0);
            }
            foreach (var (t, phase, rate) in stars)
            {
                var sr = t.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    float brightness = (Mathf.Sin(time * rate + phase) + 1f) * 0.5f;
                    sr.color = brightness > 0.65f ? Palette.WarmIvory : brightness > 0.3f ? Palette.MutedGray : Palette.DarkBorder;
                }
            }
        }
    }
}
