#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SkyQueue
{
    /// <summary>Editor-only calibration aid: draws the reference screenshot over the gameplay viewport (toggle with O). Never shipped.</summary>
    public sealed class ReferenceOverlay : MonoBehaviour
    {
        const string ReferencePath = "Assets/SkyQueue/Reference/ref_level15.png";
        GameConfig cfg;
        ScreenMap map;
        Texture2D tex;

        public void Init(GameConfig config, ScreenMap screenMap) { cfg = config; map = screenMap; }

        void Update()
        {
            var kb = Keyboard.current;
            if (kb != null && kb.oKey.wasPressedThisFrame) cfg.showReferenceOverlay = !cfg.showReferenceOverlay;
        }

        void OnGUI()
        {
            if (cfg == null || !cfg.showReferenceOverlay) return;
            if (tex == null) tex = AssetDatabase.LoadAssetAtPath<Texture2D>(ReferencePath);
            if (tex == null) return;
            Rect pr = map.FramePx;
            var prev = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, cfg.overlayAlpha);
            GUI.DrawTexture(new Rect(pr.x, Screen.height - pr.yMax, pr.width, pr.height), tex, ScaleMode.StretchToFill);
            GUI.color = prev;
        }
    }
}
#endif
