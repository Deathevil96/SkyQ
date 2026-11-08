using System;
using UnityEngine;

namespace SkyQueue
{
    /// <summary>
    /// All HUD placement, sizes, fonts and colours (reference-screenshot pixels, 921 x 2000, y down). Serialized on
    /// <see cref="GameConfig"/> so the UI can be tuned in the inspector without touching code.
    /// </summary>
    [Serializable]
    public sealed class HudLayout
    {
        [Header("Canvas the layout is authored against")]
        public Vector2 canvasSize = new Vector2(1320f, 2868f);

        [Header("Top bar")]
        public Vector2 levelLabel = new Vector2(460f, 182f);
        public Vector2 levelLabelSize = new Vector2(400f, 90f);
        public int levelFontSize = 70;
        public Vector2 restartButton = new Vector2(65f, 190f);
        public Vector2 pauseButton = new Vector2(855f, 190f);
        public Vector2 cornerButtonSize = new Vector2(86f, 86f);
        public int cornerFontSize = 52;
        public Vector2 coinsLabel = new Vector2(460f, 252f);
        public Vector2 coinsLabelSize = new Vector2(420f, 64f);
        public int coinsFontSize = 44;
        public string coinsFormat = "Coins {0}";
        public float coinCountSeconds = 0.6f;     // counter rolls up/down to the new balance
        public float panelFadeSeconds = 0.2f;     // popups fade in
        public Color coinsColor = new Color(1f, 0.86f, 0.3f);

        [Header("Booster bar (button i = BoosterKind i)")]
        public string[] boosterLabels = { "VIP", "Arrange", "Jumble" };
        public float[] boosterXs = { 200f, 460f, 722f };
        public float boosterY = 1840f;
        public Vector2 boosterSize = new Vector2(200f, 125f);
        public int boosterFontSize = 44;
        [Tooltip("Booster icons (Art/Sprites/Boosters), same order as the labels. The icon sits on top of the button like the reference.")]
        public Sprite[] boosterIcons = new Sprite[0];
        public Vector2 boosterIconSize = new Vector2(150f, 150f);
        [Tooltip("Icon centre, reference px from the button's top-left corner (negative y = above the button).")]
        public Vector2 boosterIconCenter = new Vector2(100f, 0f);
        [Tooltip("Label offset (reference px, + = down) when an icon is shown, so the text sits under the icon.")]
        public float boosterLabelDropPx = 34f;
        public int boosterIconFontSize = 34;
        public Color boosterIconLockedTint = new Color(0.55f, 0.55f, 0.6f, 0.85f);
        public string boosterOwnedFormat = "{0}\nx{1}";
        public string boosterBuyFormat = "{0}\n+";
        [Tooltip("One-line formats used when icons are shown.")]
        public string boosterIconOwnedFormat = "{0} x{1}", boosterIconBuyFormat = "{0} +", boosterIconLockedFormat = "{0} Lv {1}";
        public Color boosterColor = new Color(0.25f, 0.55f, 0.95f);
        public Color boosterArmedColor = new Color(1f, 0.72f, 0.15f);
        public string boosterLockedFormat = "{0}\nLv {1}";
        public Color boosterLockedColor = new Color(0.45f, 0.5f, 0.62f, 0.8f);
        public string boosterLockedToast = "{0} unlocks at level {1}";

        [Header("Booster intro popup")]
        public string introTitleFormat = "New booster: {0}";
        public string introGiftFormat = "Free gift: x{0}";
        public Vector2 introOkButton = new Vector2(460f, 1080f);

        [Header("Settings (pause panel)")]
        public Vector2 settingSound = new Vector2(460f, 1340f);
        public Vector2 settingHaptics = new Vector2(460f, 1460f);
        public Vector2 settingMotion = new Vector2(460f, 1580f);
        public Vector2 settingButtonSize = new Vector2(460f, 100f);
        public int settingFontSize = 44;
        public string soundFormat = "Sound: {0}";
        public string hapticsFormat = "Vibration: {0}";
        public string motionFormat = "Motion: {0}";
        public string onText = "On", offText = "Off", fullMotionText = "Full", reducedMotionText = "Reduced";
        public Color settingColor = new Color(0.35f, 0.4f, 0.55f);

        [Header("Buy panel")]
        public Vector2 buyTitle = new Vector2(460f, 760f);
        public Vector2 buyDescription = new Vector2(460f, 860f);
        public Vector2 buyDescriptionSize = new Vector2(860f, 80f);
        public int buyDescriptionFontSize = 44;
        public Vector2 buyCost = new Vector2(460f, 950f);
        public string buyCostFormat = "{0} coins";
        public string buyCostShortFormat = "{0} coins (not enough)";
        public Vector2 buyButton = new Vector2(460f, 1080f);
        public Vector2 cancelButton = new Vector2(460f, 1240f);

        [Header("Messages")]
        public Vector2 toast = new Vector2(460f, 840f);
        public Vector2 toastSize = new Vector2(800f, 90f);
        public int toastFontSize = 50;
        public float toastSeconds = 1f;
        public Vector2 banner = new Vector2(460f, 1000f);
        public Vector2 bannerSize = new Vector2(900f, 160f);
        public int bannerFontSize = 110;
        public float bannerSeconds = 1.6f;
        public float fadeSeconds = 0.35f;
        public Color bannerColor = new Color(1f, 0.85f, 0.2f);

        [Header("Panels")]
        public Vector2 resultTitle = new Vector2(460f, 820f);
        public Vector2 pauseTitle = new Vector2(460f, 780f);
        public Vector2 resultTitleSize = new Vector2(900f, 200f);
        public int resultTitleFontSize = 90;
        public Vector2 resultReward = new Vector2(460f, 900f);
        public Vector2 resultRewardSize = new Vector2(900f, 80f);
        public int resultRewardFontSize = 54;
        public string rewardFormat = "+{0} coins";
        public string milestoneFormat = "+{0} milestone bonus";
        public string replayText = "Already completed - no coins";
        public Vector2 primaryButton = new Vector2(460f, 1000f);
        public Vector2 secondaryButton = new Vector2(460f, 1170f);
        public Vector2 panelButtonSize = new Vector2(380f, 130f);
        public int panelFontSize = 58;

        [Header("Colours")]
        public Color accent = new Color(0.25f, 0.55f, 0.95f);
        public Color positive = new Color(0.25f, 0.75f, 0.3f);
        public Color inactive = new Color(0.25f, 0.55f, 0.95f, 0.45f);
        public Color dim = new Color(0.05f, 0.08f, 0.2f, 0.72f);
    }
}
