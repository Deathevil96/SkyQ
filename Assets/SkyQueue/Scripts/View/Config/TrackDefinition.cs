using UnityEngine;

namespace SkyQueue
{
    /// <summary>
    /// Data asset describing the passenger-loop track. Coordinates are reference-screenshot pixels (921 x 2000, y down),
    /// so the loop scales with every screen. The ring is a smooth rounded triangle (apex up, flat bottom with the exit gap); the right feed is
    /// the left feed mirrored about the vertical centre. The road surface is drawn as smooth strips along these centre
    /// lines, so the islands are simply the garden showing through: no hand-traced polygons, no corners.
    /// </summary>
    [CreateAssetMenu(menuName = "SkyQueue/Track Definition", fileName = "TrackDefinition")]
    public sealed class TrackDefinition : ScriptableObject
    {
        [Header("Ring (smooth rounded triangle, travelled clockwise on screen)")]
        [Tooltip("Corners of the ring centre line, clockwise on screen: apex (top), bottom-right, bottom-left. Reference px.")]
        public Vector2[] ringCornersPx = { new Vector2(460f, 300f), new Vector2(745f, 560f), new Vector2(175f, 560f) };
        [Tooltip("Rounding radius per corner (reference px); larger = rounder.")]
        public float[] ringCornerRadiusPx = { 130f, 115f, 115f };
        [Range(4, 64)] public int ringArcSamples = 24;

        [Header("Feeds")]
        [Tooltip("Left feed from above the screen towards the ring (the end is snapped onto the ring). The right feed mirrors it.")]
        public Vector2[] feedLeft =
        {
            new Vector2(250,-250), new Vector2(235,0), new Vector2(215,120), new Vector2(176,215), new Vector2(146,300), new Vector2(165,375),
            new Vector2(215,415), new Vector2(262,430)
        };
        [Range(4, 32)] public int splineSamplesPerSegment = 14;

        [Header("Road surface")]
        [Tooltip("Half width of the ring road (reference px); the reference ring band is about 100 px wide.")]
        public float ringHalfWidthPx = 70f;
        [Tooltip("Half width of the feed roads (reference px); the reference feeds are about 120-135 px wide.")]
        public float feedHalfWidthPx = 70f;
        [Tooltip("Share of the road width a row of passengers spans (the rest is margin to the rims).")]
        [Range(0.4f, 1f)] public float rowFillFraction = 0.82f;
        [Tooltip("Width of the white rim outside the road (world units).")]
        public float rimWidth = 0.07f;
        [Tooltip("Feed/ring junctions are filled with road wherever the two road edges are closer than this (world units), so no garden sliver shows.")]
        public float junctionFillDistance = 0.9f;
        [Tooltip("The exit gap opens from the ring bottom down to this point (the runway).")]
        public Vector2 exitStubEndPx = new Vector2(460f, 650f);
        [Tooltip("Half width of the exit neck (reference px); the reference neck spans about x 375..545.")]
        public float exitStubHalfWidthPx = 85f;

        [Header("Markers")]
        public Vector2 exitGap = new Vector2(460f, 560f);
        [Tooltip("Where the feed crosses the top edge of the frame; feed queues start there.")]
        public Vector2 feedScreenTop = new Vector2(235f, 0f);
    }
}
