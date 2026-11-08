using System;
using UnityEngine;

namespace SkyQueue
{
    /// <summary>Colours, lighting and decor placement for the scene (placeholder art direction). Serialized on <see cref="GameConfig"/>.</summary>
    [Serializable]
    public sealed class EnvironmentTheme
    {
        [Tooltip("World name (logs / analytics), e.g. Blossom Garden, Desert Rescue.")]
        public string name = "Blossom Garden";

        [Header("Ground")]
        public Color floor = new Color(0.80f, 0.81f, 0.92f);
        public Color garden = new Color(0.94f, 0.72f, 0.72f);
        public Color runway = new Color(0.36f, 0.37f, 0.42f);
        public Color road = new Color(0.40f, 0.41f, 0.47f);
        public Color rim = new Color(0.97f, 0.97f, 1f);
        [Header("Ground textures (Art/Textures; empty = plain colours above)")]
        public Texture2D gardenTexture;
        public Texture2D asphaltTexture;
        [Tooltip("Stand strip / runway texture; empty = use the asphalt texture.")]
        public Texture2D runwayTexture;
        [Tooltip("World size of one texture tile.")]
        public float gardenTileWorld = 6f, asphaltTileWorld = 3f;
        [Tooltip("Tints multiplied onto the textures (white = as painted). Road and runway share the asphalt texture.")]
        public Color gardenTint = Color.white, roadTint = Color.white, runwayTint = new Color(0.9f, 0.9f, 0.92f);
        [Tooltip("How far (reference px) the scenery extends past the gameplay frame so any screen shape stays filled.")]
        public float reachX = 1600f, reachY = 1400f;

        [Header("Runway outline (reference px)")]
        public float gardenBottomY = 618f;
        public float runwayTopY = 650f;
        public float runwayBottomY = 915f;
        public float runwayNotchLeftX = 370f, runwayNotchRightX = 550f;

        [Header("Stands")]
        public Color slotFill = new Color(0.30f, 0.31f, 0.37f);
        public Color slotNormal = new Color(0.55f, 0.57f, 0.65f);
        public Color slotVip = new Color(1f, 0.72f, 0.15f);
        public Color slotPurchasable = new Color(0.45f, 0.78f, 0.52f);
        public float slotVipTextSize = 0.11f, slotPlusTextSize = 0.2f;

        [Header("Decor")]
        public Color blossom = new Color(1f, 0.78f, 0.86f);
        public Color trunk = new Color(0.45f, 0.3f, 0.25f);
        public Vector2 treeLeftPx = new Vector2(35f, 500f);
        public Vector2 treeRightPx = new Vector2(890f, 480f);
        public float treeLeftScale = 0.55f, treeRightScale = 0.6f;

        [Header("Ambient flipbook (e.g. desert signal smoke; empty = none)")]
        public Sprite[] ambientFrames = new Sprite[0];
        [Tooltip("Ground point in reference px (bottom of the effect).")]
        public Vector2 ambientPx = new Vector2(860f, 300f);
        public float ambientWorldSize = 2.2f, ambientFps = 10f, ambientRestSeconds = 1.2f;

        [Header("Lighting")]
        public Vector3 lightEuler = new Vector3(58f, -28f, 0f);
        public float lightIntensity = 1.05f;
        [Range(0f, 1f)] public float shadowStrength = 0.45f;
        public Color ambient = new Color(0.78f, 0.78f, 0.88f);
        public Color cameraBackground = new Color(0.8f, 0.8f, 0.92f);

        /// <summary>Starting values for the desert-rescue world: sand, dirt road, dusty airstrip, warm light. Plain colours
        /// until the desert art pack arrives (then assign its textures in the inspector).</summary>
        public static EnvironmentTheme Desert()
        {
            return new EnvironmentTheme
            {
                name = "Desert Rescue",
                floor = new Color(0.90f, 0.89f, 0.86f),         // pale concrete apron: yellow/orange planes stay readable
                garden = new Color(0.91f, 0.76f, 0.52f),        // sand
                runway = new Color(0.52f, 0.46f, 0.40f),        // dusty airstrip
                road = new Color(0.62f, 0.50f, 0.38f),          // packed dirt
                rim = new Color(0.99f, 0.93f, 0.80f),
                gardenTint = Color.white, roadTint = Color.white, runwayTint = Color.white,
                slotFill = new Color(0.42f, 0.36f, 0.31f),
                slotNormal = new Color(0.78f, 0.70f, 0.58f),
                blossom = new Color(0.38f, 0.62f, 0.33f),       // cactus green (placeholder decor)
                trunk = new Color(0.55f, 0.42f, 0.30f),
                lightEuler = new Vector3(62f, -20f, 0f),
                lightIntensity = 1.2f,
                shadowStrength = 0.55f,
                ambient = new Color(0.86f, 0.80f, 0.72f),
                cameraBackground = new Color(0.92f, 0.90f, 0.86f)
            };
        }
    }

    /// <summary>
    /// 3D stand pads (Art/3D/*parking_pad*). The meshes are coloured by the world's theme, not their baked textures: the
    /// raised top (rim, plus sign) gets the stand's rim colour and the inset surface the fill colour.
    /// Empty = flat polygon pads.
    /// </summary>
    [Serializable]
    public sealed class StandArtSettings
    {
        public Mesh plain, vip, locked;
        [Tooltip("Yaw per model so its long side runs along the stand (the locked pad is modelled sideways).")]
        public float plainYaw = 0f, vipYaw = 0f, lockedYaw = 90f;
        [Tooltip("Pad thickness in world units (top sits just above the ground so parked planes rest on it).")]
        public float thicknessWorld = 0.05f;
        [Tooltip("Rim cut between the pad's inner floor (detected per model: largest flat upward area) and its top: 0 = at the floor, 1 = at the top. Everything above is rim / plus sign.")]
        [Range(0.05f, 0.9f)] public float rimCut = 0.3f;
        public float rimSmoothness = 0.35f, fillSmoothness = 0.15f;
        public bool IsSet { get { return plain != null; } }
    }

    /// <summary>Worlds rotate by level: levels 1..N use world 0, the next N use world 1, and so on, wrapping around.</summary>
    [Serializable]
    public sealed class WorldRotation
    {
        [Tooltip("Levels per world before switching to the next one.")]
        public int levelsPerWorld = 10;
        [Tooltip("Worlds after the base theme (GameConfig > theme is world 0).")]
        public EnvironmentTheme[] extraWorlds = { EnvironmentTheme.Desert() };
    }
}
