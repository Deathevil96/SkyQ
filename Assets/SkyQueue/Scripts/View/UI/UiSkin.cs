using System;
using UnityEngine;

namespace SkyQueue
{
    /// <summary>One popup's art placement (reference px, 921 x 2000, y down).</summary>
    [Serializable]
    public sealed class PanelFrame
    {
        public Vector2 center, size;
        public PanelFrame() { }   // serializer
        public PanelFrame(Vector2 center, Vector2 size) { this.center = center; this.size = size; }
        public float Top { get { return center.y - size.y * 0.5f; } }
    }

    /// <summary>
    /// UI kit art (Art/Sprites/UI, cut by Tools/art/cut_ui_kit.py). Every slot is optional: an empty slot falls back to
    /// the procedural rounded rectangle and flat colours, so the HUD always works without art.
    /// </summary>
    [Serializable]
    public sealed class UiSkin
    {
        [Header("Buttons (9-sliced pills)")]
        public Sprite buttonBlue;
        public Sprite buttonGreen, buttonGrey;
        public Sprite roundRestart, roundPause;

        [Header("Coins")]
        public Sprite coinBar;
        public Vector2 coinBarSize = new Vector2(250f, 84f);
        [Tooltip("Coin count centre, reference px right of the bar centre (clears the coin disc on the left).")]
        public float coinTextOffsetX = 28f;
        public string coinBarFormat = "{0}";
        public Color coinBarTextColor = new Color(0.45f, 0.25f, 0.08f);

        [Header("Popups")]
        public Sprite panel;
        public Sprite ribbonWin, ribbonLose, ribbonPause, ribbonInfo;
        public Vector2 ribbonSize = new Vector2(700f, 240f);
        [Tooltip("Ribbon centre relative to the panel top (reference px, + = down). The popup title sits on it.")]
        public float ribbonOffsetY = 10f;
        public Sprite starsWin, starsLose;
        public Vector2 starsSize = new Vector2(420f, 176f);
        [Tooltip("Stars centre relative to the panel top (reference px, - = above).")]
        public float starsOffsetY = -175f;
        public int ribbonFontSize = 64;
        public Color panelTextColor = new Color(0.42f, 0.24f, 0.1f);
        public Color panelAccentColor = new Color(0.85f, 0.45f, 0.05f);
        public PanelFrame resultPanel = new PanelFrame(new Vector2(460f, 975f), new Vector2(780f, 390f));
        public PanelFrame pausePanel = new PanelFrame(new Vector2(460f, 1235f), new Vector2(780f, 930f));
        public PanelFrame buyPanel = new PanelFrame(new Vector2(460f, 1040f), new Vector2(880f, 680f));
        public PanelFrame introPanel = new PanelFrame(new Vector2(460f, 950f), new Vector2(880f, 500f));

        [Header("Difficulty banner")]
        public Sprite bannerHard, bannerSuperHard;
        public Vector2 bannerArtSize = new Vector2(820f, 190f);

        [Header("Win confetti (Art/Sprites/VFX/confetti_*)")]
        public Sprite[] confetti = new Sprite[0];
        public Vector2 confettiSize = new Vector2(600f, 600f);
        [Tooltip("Confetti centre relative to the panel top (reference px).")]
        public float confettiOffsetY = -170f;
        public float confettiSeconds = 0.9f;

        public bool HasPanels { get { return panel != null; } }
    }
}
