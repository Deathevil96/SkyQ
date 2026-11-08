using System;
using UnityEngine;

namespace SkyQueue
{
    /// <summary>Animation timing and juice values for planes, passengers and result flow. Serialized on <see cref="GameConfig"/>.</summary>
    [Serializable]
    public sealed class FeelConfig
    {
        [Header("Plane")]
        public float bumpSeconds = 0.12f;           // scale pulse when a passenger boards
        public float bumpAmount = 0.06f;
        [Tooltip("Parked planes swell as passengers board: extra size when full (0.35 = 35% bigger). Keep the full size inside the stand gap.")]
        public float boardGrowMax = 0.35f;
        public float shakeSeconds = 0.28f;          // blocked-tap shake
        public float shakeAmplitude = 0.1f;
        public float shakeFrequency = 60f;
        public float settleRotateSeconds = 0.15f;   // turn to face the stand after arriving
        [Range(0f, 1f)] public float arrivalScaleStart = 0.7f;   // fraction of the route where the plane begins shrinking into the stand

        [Header("Stand label")]
        public float labelHeight = 0.05f;
        public float labelTextSize = 0.075f;
        public Vector2 badgeSize = new Vector2(0.62f, 0.34f);

        [Header("Boarding walkers")]
        public float walkerJitterX = 0.12f, walkerJitterZ = 0.2f;
        [Range(0f, 1f)] public float walkerShrink = 0.35f;
        [Tooltip("Seconds between passengers of one row setting off, so they board one by one instead of as a block.")]
        public float walkerStagger = 0.11f;
        [Tooltip("Random +/- fraction applied to each walker's walk time so they move as individuals.")]
        [Range(0f, 0.5f)] public float walkerTimeJitter = 0.2f;

        [Header("Passengers")]
        public float bobSpeed = 7f, bobHeight = 0.04f, bobLanePhase = 0.4f, bobRowPhase = 0.7f;
        [Tooltip("Viewport margin outside which rows are not drawn.")]
        public float cullMargin = 0.1f;

        [Header("Departure route (reference px, relative to the stand row)")]
        public Vector2 departTaxi = new Vector2(0.7f, 0.5f);          // world offset from the stand
        public Vector2 departMidPx = new Vector2(1000f, -30f);        // x px, y px offset from the stand row
        public Vector2 departEndPx = new Vector2(1160f, -90f);

        [Header("Result flow")]
        public float winResultDelay = 0.9f;
        public float failResultDelay = 0.6f;
    }

    /// <summary>Plane look. When <see cref="models"/> are set the art models are used; otherwise the primitive placeholder
    /// (proportions below, in board cells) is built.</summary>
    [Serializable]
    public sealed class PlaneArtSettings
    {
        [Header("Art models (index = PlaneSize: Small 16, Medium 24, Large 40)")]
        public Mesh[] models = new Mesh[0];
        [Tooltip("Grey 'paint' textures (Art/Textures/planeNN_paint): tinted with the gameplay colour.")]
        public Texture2D[] paintTextures = new Texture2D[0];
        [Tooltip("Yaw that turns the model's nose to +Z (the art faces +X).")]
        public float modelYaw = -90f;
        [Tooltip("Wingspan cap in board cells, so wings never reach into a neighbouring plane's cells.")]
        public float maxWingCells = 1.25f;
        [Tooltip("Height relative to the length-based scale (1 = model proportions).")]
        public float modelHeightScale = 1f;
        public float paintSmoothness = 0.6f;
        public float arrowLift = 0.02f;                // arrow decal height above the model's top

        [Header("Shared")]
        public float lengthFill = 0.94f;       // fuselage length / footprint length
        public float bodyWidth = 0.56f;
        public float wingSpan = 0.96f, wingThickness = 0.09f, wingChordFraction = 0.3f, wingMaxChord = 1.1f, wingOffset = 0.04f;
        public float tailSpan = 0.54f, tailThickness = 0.07f, tailChordFraction = 0.12f, tailOffset = -0.4f;
        public float finThickness = 0.07f, finHeight = 0.34f, finChordFraction = 0.14f, finOffset = -0.42f;
        public float cockpitForward = 0.26f;
        public Color cockpitColor = new Color(0.85f, 0.95f, 1f);
        public float bodySmoothness = 0.75f, accentSmoothness = 0.6f, cockpitSmoothness = 0.9f;
        [Range(0f, 1f)] public float accentDarken = 0.78f;
        public Vector2 arrowSize = new Vector2(0.62f, 0.8f);   // width, length in cells
        public float arrowForwardOffset = -0.05f;
        public Color arrowColor = Color.white;
    }

    /// <summary>Passenger look. With <see cref="models"/> set, the character archetypes are drawn (mixed per row);
    /// otherwise the placeholder (a capsule body + sphere head, proportions below in model units).</summary>
    [Serializable]
    public sealed class PassengerArtSettings
    {
        [Header("Character archetypes (Art/3D/Characters)")]
        public Mesh[] models = new Mesh[0];
        [Tooltip("Baked outfit textures, archetype-major: index = archetype * paletteCount + colour (Art/Textures/Passengers).")]
        public Texture2D[] colorTextures = new Texture2D[0];
        [Tooltip("Model scale relative to GameConfig.passengerScale (matches the placeholder's height).")]
        public float modelScale = 1.3f;

        [Header("Placeholder")]
        public Vector3 bodyOffset = new Vector3(0f, 0.38f, 0f);
        public Vector3 bodyScale = new Vector3(0.66f, 0.38f, 0.58f);
        public Vector3 headOffset = new Vector3(0f, 0.92f, 0.02f);
        public Vector3 headScale = new Vector3(0.7f, 0.66f, 0.66f);
        public float smoothness = 0.55f;
    }
}
