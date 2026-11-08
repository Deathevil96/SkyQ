using System;
using UnityEngine;

namespace SkyQueue
{
    [Serializable]
    public sealed class DebugMenuSettings
    {
        public bool enabled = true;
        public int coinsPerGrant = 1000;
        public int boostersPerGrant = 5;
        [Tooltip("DBG toggle button, screen fraction from the bottom-left corner.")]
        public Vector2 togglePosition = new Vector2(0.01f, 0.01f);
        [Tooltip("UI scale relative to screen height (1 = readable on a phone).")]
        public float uiScale = 1f;
    }

    /// <summary>
    /// Cheat panel for testing: coins, boosters, level jumps, instant win/lose, save reset. Only exists in the editor and
    /// development builds (the component is never added otherwise). IMGUI so it works with mouse and touch without
    /// touching the game HUD. While open, board taps are ignored (<see cref="Open"/>).
    /// </summary>
    public sealed class DebugMenu : MonoBehaviour
    {
        public static bool Open { get; private set; }

        public interface IHost
        {
            int CurrentLevel { get; }
            void DebugGrantCoins(int amount);
            void DebugGrantBoosters(int each);
            void DebugUnlockAllBoosters();
            void DebugLoadLevel(int level);
            void DebugFinishLevel(bool won);
            void DebugResetSave();
        }

        IHost host;
        DebugMenuSettings s;
        GUIStyle button, label, box;
        bool confirmReset;
        string levelField = "";

        public static void Attach(GameObject go, IHost host, DebugMenuSettings settings)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (settings == null || !settings.enabled) return;
            var m = go.AddComponent<DebugMenu>();
            m.host = host; m.s = settings;
            DevLog.Info("debug", "debug menu available (DBG button bottom-left, F1 in the editor)");
#endif
        }

        void Update()
        {
#if UNITY_EDITOR
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb != null && kb.f1Key.wasPressedThisFrame) Open = !Open;
#endif
        }

        void OnDisable() { Open = false; }

        void Styles(float unit)
        {
            int font = Mathf.RoundToInt(unit * 0.42f);
            button = new GUIStyle(GUI.skin.button) { fontSize = font, fontStyle = FontStyle.Bold };
            label = new GUIStyle(GUI.skin.label) { fontSize = font, alignment = TextAnchor.MiddleCenter, normal = { textColor = Color.white } };
            box = new GUIStyle(GUI.skin.box);
        }

        void OnGUI()
        {
            if (host == null) return;
            float unit = Screen.height * 0.042f * Mathf.Max(0.3f, s.uiScale);   // row height
            Styles(unit);

            // small corner button below the booster bar, so a tap on it never lands on a booster or the board
            var toggle = new Rect(Screen.width * s.togglePosition.x, Screen.height * (1f - s.togglePosition.y) - unit * 0.8f, unit * 1.9f, unit * 0.8f);
            if (GUI.Button(toggle, Open ? "X" : "DBG", button)) { Open = !Open; confirmReset = false; }
            if (!Open) return;

            float w = Mathf.Min(Screen.width * 0.92f, unit * 11f), pad = unit * 0.25f;
            const int rows = 8;   // title + 7 button rows
            var panel = new Rect((Screen.width - w) * 0.5f, Screen.height * 0.18f, w, rows * (unit + pad) + pad);
            GUI.Box(panel, "", box);
            GUI.Box(panel, "", box);   // darker
            float x = panel.x + pad, y = panel.y + pad, cw = w - pad * 2, half = (cw - pad) * 0.5f;

            GUI.Label(new Rect(x, y, cw, unit), "DEBUG  (level " + host.CurrentLevel + ")", label); y += unit + pad;
            if (GUI.Button(new Rect(x, y, cw, unit), "+" + s.coinsPerGrant + " coins", button)) host.DebugGrantCoins(s.coinsPerGrant);
            y += unit + pad;
            if (GUI.Button(new Rect(x, y, half, unit), "+" + s.boostersPerGrant + " each booster", button)) host.DebugGrantBoosters(s.boostersPerGrant);
            if (GUI.Button(new Rect(x + half + pad, y, half, unit), "Unlock boosters", button)) host.DebugUnlockAllBoosters();
            y += unit + pad;
            if (GUI.Button(new Rect(x, y, half, unit), "Win level", button)) { host.DebugFinishLevel(true); Open = false; }
            if (GUI.Button(new Rect(x + half + pad, y, half, unit), "Lose level", button)) { host.DebugFinishLevel(false); Open = false; }
            y += unit + pad;
            float third = (cw - pad * 2) / 3f;
            if (GUI.Button(new Rect(x, y, third, unit), "< Level", button)) host.DebugLoadLevel(host.CurrentLevel - 1);
            if (GUI.Button(new Rect(x + third + pad, y, third, unit), "Level >", button)) host.DebugLoadLevel(host.CurrentLevel + 1);
            if (GUI.Button(new Rect(x + (third + pad) * 2, y, third, unit), "+10 (world)", button)) host.DebugLoadLevel(host.CurrentLevel + 10);
            y += unit + pad;
            levelField = GUI.TextField(new Rect(x, y, half, unit), levelField, new GUIStyle(GUI.skin.textField) { fontSize = label.fontSize, alignment = TextAnchor.MiddleCenter });
            int target;
            if (GUI.Button(new Rect(x + half + pad, y, half, unit), "Go to level", button) && int.TryParse(levelField, out target)) host.DebugLoadLevel(target);
            y += unit + pad;
            if (GUI.Button(new Rect(x, y, cw, unit), confirmReset ? "Tap again to RESET save" : "Reset save", button))
            {
                if (confirmReset) { host.DebugResetSave(); confirmReset = false; Open = false; }
                else confirmReset = true;
            }
            y += unit + pad;
            if (GUI.Button(new Rect(x, y, cw, unit), "Close", button)) { Open = false; confirmReset = false; }
        }
    }
}
