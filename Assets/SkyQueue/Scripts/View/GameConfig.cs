using UnityEngine;

namespace SkyQueue
{
    /// <summary>
    /// Central, editable tuning for the whole game: rules, palette, camera calibration, board placement, loop feel, stands,
    /// motion, theme, HUD and debug. Screen-layout values are in reference-screenshot pixels (921 x 2000, the downscaled
    /// Bus Fever Party Level 15 capture). Nothing gameplay-relevant should be hard-coded elsewhere.
    /// </summary>
    public sealed class GameConfig : MonoBehaviour
    {
        public const float RefW = 921f, RefH = 2000f;
        public const float RefAspect = 1320f / 2868f;   // aspect of the original capture the layout was traced from

        [Header("Material templates (assets, so shaders/variants ship in builds)")]
        public Material litTemplate;
        public Material unlitTemplate;
        public PrimitiveMeshes primitives = new PrimitiveMeshes();

        [Header("Rules (validated and applied at start)")]
        public RulesConfig rules = new RulesConfig();

        [Header("Palette (index = colour id)")]
        public Color[] palette = (Color[])Palette.Colors.Clone();

        [Header("Camera (calibrate against the reference overlay: press O in Play mode)")]
        public float camHeight = 30f;
        public float camPitch = 74f;
        [Tooltip("Vertical FOV that frames the whole reference composition; the real FOV is derived per screen.")]
        public float camFov = 30f;
        public float camZ = -8.6f;

        [Header("Board (reference px)")]
        public Vector2 boardCenterPx = new Vector2(462f, 1290f);
        public float boardAngleDeg = 20f;
        public float boardFitPx = 610f;      // square the board must fit in
        public float minCellPx = 30f, maxCellPx = 96f;
        public float boardPickHeight = 0.3f; // roof height used for tap picking

        [Header("Passenger loop")]
        public bool ringStartsFull = false;    // level opens with the ring packed and feeds queued (reference look)
        public float feedHoldGap = 0.8f;        // waiting feed rows stand this far upstream of the merge point
        public float passengerSpacing = 0.30f;   // distance between rows along the path
        public float loopSpeed = 1.9f;
        public float passengerScale = 0.42f;
        public float walkerTime = 0.32f;
        public float walkerHop = 0.35f;

        [Header("Track")]
        public TrackDefinition track;

        [Header("Stands (reference px)")]
        public Vector2 slotFirstPx = new Vector2(217.5f, 755f); // first plain stand (row centred on the screen)
        public float slotStepPx = 98f;                          // pad pitch: leaves a gap so swollen planes don't touch
        [Tooltip("Pad half size (reference px). The length is chosen so the large plane parks at its board size on 13x13 boards and denser.")]
        public float slotHalfWidthPx = 37f, slotHalfHeightPx = 90f, slotBorderPx = 5f;
        [Tooltip("Pad tilt in degrees; positive leans the top of each pad to the left, as in the reference.")]
        public float slotTiltDeg = 12f;
        [Tooltip("Fraction of the pad length the longest parked plane may use before planes are scaled down.")]
        public float slotPlaneFill = 1f;
        public float slotApproachBelowPx = 112f;                // approach lane below the stand row
        public StandArtSettings standArt = new StandArtSettings();
        [Tooltip("Side rails for leaving planes sit this far (reference px) inside the actual screen edges, on every screen shape.")]
        public float railInsetPx = 28f;
        [Tooltip("Invisible floor line: leaving planes stay this far (reference px) above the top of the booster bar.")]
        public float exitFloorMarginPx = 30f;
        public float slotLabelGap = 0.08f;

        [Header("Plane motion")]
        public float planeSpeed = 11f;
        public float planeTurnDegPerSec = 540f;
        public float departSpeedFactor = 0.9f;
        public float departTurnDegPerSec = 420f;
        public float departScale = 1.5f;
        public float departLift = 5f;
                public float exitMarginCells = 0.5f;
        [Tooltip("Half the plane body size (cells) kept clear of the rails and the floor line.")]
        public float planeBodyHalfCells = 0.6f;
        [Tooltip("Turn radius (cells) where a leaving plane changes direction.")]
        public float planeTurnRadiusCells = 1.2f;
        public bool reduceMotion;

        [Header("Feel (animation timing)")]
        public FeelConfig feel = new FeelConfig();
        public HelicopterSettings helicopter = new HelicopterSettings();
        public ExhaustSettings exhaust = new ExhaustSettings();
        public VfxSettings vfx = new VfxSettings();
        public SoundSettings sound = new SoundSettings();
        public EmojiSettings emoji = new EmojiSettings();

        [Header("Placeholder art proportions")]
        public PlaneArtSettings planeArt = new PlaneArtSettings();
        public PassengerArtSettings passengerArt = new PassengerArtSettings();

        [Header("Theme (world 0) and world rotation")]
        public EnvironmentTheme theme = new EnvironmentTheme();
        public WorldRotation worlds = new WorldRotation();

        public int WorldCount { get { return 1 + (worlds != null && worlds.extraWorlds != null ? worlds.extraWorlds.Length : 0); } }

        /// <summary>World index for a level: blocks of <see cref="WorldRotation.levelsPerWorld"/> levels, wrapping round.</summary>
        public int WorldIndexFor(int level)
        {
            int per = worlds != null ? Mathf.Max(1, worlds.levelsPerWorld) : 1;
            return ((Mathf.Max(1, level) - 1) / per) % WorldCount;
        }

        public EnvironmentTheme ThemeFor(int level)
        {
            int i = WorldIndexFor(level);
            return i == 0 ? theme : worlds.extraWorlds[i - 1];
        }

        [Header("HUD")]
        public HudLayout hud = new HudLayout();

        [Header("Economy & save")]
        public EconomyConfig economy = new EconomyConfig();
        public string saveFileName = "skyqueue_save.json";

        [Header("Debug")]
        public bool showReferenceOverlay;
        [Tooltip("Development builds/editor: also log high-frequency steps (each tap, each boarding row).")]
        public bool devLogVerbose;
        [Tooltip("Development builds/editor: log whether each launch route passes over a booster button.")]
        public bool logRouteChecks = true;
        [Tooltip("Development builds/editor only: cheat menu (DBG button bottom-left, F1 in the editor).")]
        public DebugMenuSettings debugMenu = new DebugMenuSettings();
        [Range(0f, 1f)] public float overlayAlpha = 0.5f;
    }
}
