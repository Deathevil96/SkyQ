using System;
using UnityEngine;
using UnityEngine.UI;

namespace SkyQueue
{
    /// <summary>Small helpers for building uGUI elements in reference-pixel coordinates (top-left anchored).</summary>
    public static class UiFactory
    {
        /// <summary>Raised for every button press (UI click sound).</summary>
        public static event Action AnyClick;

        static float refToCanvas = 1f;   // canvas units per reference-screenshot pixel; set once from the HUD layout
        /// <summary>Must be called before building any UI: ties reference pixels to the HUD canvas size.</summary>
        public static void Configure(Vector2 canvasSize) { refToCanvas = canvasSize.x / GameConfig.RefW; }

        static Font font;
        static Sprite rounded;

        public static Font Font { get { return font != null ? font : (font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")); } }

        /// <summary>Procedural 9-sliced rounded rectangle so the UI needs no imported art.</summary>
        public static Sprite Rounded
        {
            get
            {
                if (rounded != null) return rounded;
                const int n = 64; const float r = 20f;
                var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp, name = "UiRounded" };
                var px = new Color[n * n];
                for (int y = 0; y < n; y++)
                    for (int x = 0; x < n; x++)
                    {
                        float dx = Mathf.Max(r - x - 0.5f, x + 0.5f - (n - r), 0f);
                        float dy = Mathf.Max(r - y - 0.5f, y + 0.5f - (n - r), 0f);
                        px[y * n + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(r - Mathf.Sqrt(dx * dx + dy * dy) + 0.5f));
                    }
                tex.SetPixels(px);
                tex.Apply(false, true);
                return rounded = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(r, r, r, r));
            }
        }

        public static RectTransform Rect(string name, Transform parent, Vector2 centerPx, Vector2 sizePx)
        {
            var rt = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(centerPx.x * refToCanvas, -centerPx.y * refToCanvas);
            rt.sizeDelta = sizePx * refToCanvas;
            return rt;
        }

        public static Text Text(string content, Transform parent, Vector2 centerPx, Vector2 sizePx, int fontSize, Color color)
        {
            var rt = Rect("Text", parent, centerPx, sizePx);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = Font; t.text = content; t.fontSize = fontSize; t.color = color;
            t.alignment = TextAnchor.MiddleCenter; t.fontStyle = FontStyle.Bold;
            t.horizontalOverflow = HorizontalWrapMode.Overflow; t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            var o = rt.gameObject.AddComponent<Outline>();
            o.effectColor = new Color(0.1f, 0.15f, 0.35f, 0.9f); o.effectDistance = new Vector2(3f, -3f);
            return t;
        }

        public static Button Button(string label, Transform parent, Vector2 centerPx, Vector2 sizePx, Color color, int fontSize, Action onClick)
        {
            var rt = Rect("Btn_" + label, parent, centerPx, sizePx);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = Rounded; img.type = Image.Type.Sliced; img.color = color;
            var b = rt.gameObject.AddComponent<Button>();
            b.targetGraphic = img;
            b.onClick.AddListener(() => { if (AnyClick != null) AnyClick(); if (onClick != null) onClick(); });
            var t = Text(label, rt, Vector2.zero, sizePx, fontSize, Color.white);
            t.rectTransform.anchorMin = t.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            t.rectTransform.anchoredPosition = Vector2.zero;
            return b;
        }

        public static GameObject Overlay(string name, Transform parent, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero;
            go.GetComponent<Image>().color = color;
            return go;
        }
    }
}
