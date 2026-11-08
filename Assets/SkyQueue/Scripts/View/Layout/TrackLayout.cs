using System.Collections.Generic;
using UnityEngine;

namespace SkyQueue
{
    /// <summary>
    /// The passenger-loop centre-line paths in world space, built from a <see cref="TrackDefinition"/>: two feed lanes from the
    /// top merge into a smooth elliptical ring that circulates around the small island; the exit gap sits at the bottom.
    /// </summary>
    public sealed class TrackLayout
    {
        public readonly TrackDefinition Definition;
        public readonly Polyline Ring;
        public readonly Polyline[] Feeds;
        public readonly float[] MergeS;          // ring arc position where each feed joins
        public readonly float BoardS;            // ring arc position of the exit gap
        public readonly float FeedHeadStart;     // feed arc position at the top edge of the screen
        public readonly Vector3 ExitStubStart, ExitStubEnd;   // exit neck from the ring bottom to the runway
        public readonly float ExitStubHalfWidth, RingHalfWidth, FeedHalfWidth;   // road half widths, world units
        public readonly float LaneWidth;   // passenger spacing across a row, so a full row fills the ring road

        public TrackLayout(ScreenMap map, TrackDefinition def)
        {
            Definition = def;

            // ring: a rounded triangle in reference px (straight sides, circular-arc corners), mapped onto the ground
            var ringPx = RoundedPolygon(def.ringCornersPx, def.ringCornerRadiusPx, Mathf.Max(4, def.ringArcSamples));
            var ring = new List<Vector3>(ringPx.Count);
            for (int i = 0; i < ringPx.Count; i++) ring.Add(map.Ground(ringPx[i]));
            Ring = new Polyline(ring, true);

            int samples = Mathf.Max(4, def.splineSamplesPerSegment);
            var rightPx = new Vector2[def.feedLeft.Length];
            for (int i = 0; i < rightPx.Length; i++) rightPx[i] = new Vector2(GameConfig.RefW - def.feedLeft[i].x, def.feedLeft[i].y);   // mirror
            List<Vector3> left = PathUtil.CatmullRom(ToWorld(map, def.feedLeft), false, samples);
            List<Vector3> right = PathUtil.CatmullRom(ToWorld(map, rightPx), false, samples);

            // feeds end exactly on the ring so passengers merge without a visual jump
            MergeS = new[] { SnapEndToRing(left), SnapEndToRing(right) };
            Feeds = new[] { new Polyline(left, false), new Polyline(right, false) };
            BoardS = Ring.Closest(map.Ground(def.exitGap));
            FeedHeadStart = Feeds[0].Closest(map.Ground(def.feedScreenTop));
            ExitStubStart = Ring.Point(BoardS);
            ExitStubEnd = map.Ground(def.exitStubEndPx);
            ExitStubHalfWidth = map.WorldDistance(def.exitStubEndPx, def.exitStubEndPx + new Vector2(def.exitStubHalfWidthPx, 0f));
            Vector2 mid = def.ringCornersPx.Length > 0 ? def.ringCornersPx[0] : def.exitGap;
            RingHalfWidth = map.WorldDistance(mid, mid + new Vector2(def.ringHalfWidthPx, 0f));
            FeedHalfWidth = map.WorldDistance(def.feedScreenTop, def.feedScreenTop + new Vector2(def.feedHalfWidthPx, 0f));
            LaneWidth = 2f * Mathf.Min(RingHalfWidth, FeedHalfWidth) * def.rowFillFraction / Rules.RowSize;
        }

        /// <summary>
        /// Closed outline through the corners with each corner replaced by a tangent circular arc (no corners anywhere).
        /// Straight sides are subdivided so the path is evenly sampled.
        /// </summary>
        static List<Vector2> RoundedPolygon(Vector2[] corners, float[] radii, int arcSamples)
        {
            int n = corners.Length;
            var tIn = new Vector2[n]; var tOut = new Vector2[n]; var centre = new Vector2[n]; var radius = new float[n];
            for (int i = 0; i < n; i++)
            {
                Vector2 c = corners[i], u1 = (corners[(i - 1 + n) % n] - c).normalized, u2 = (corners[(i + 1) % n] - c).normalized;
                float half = Vector2.Angle(u1, u2) * 0.5f * Mathf.Deg2Rad;
                float r = radii != null && radii.Length > 0 ? radii[Mathf.Min(i, radii.Length - 1)] : 0f;
                float maxT = Mathf.Min((corners[(i - 1 + n) % n] - c).magnitude, (corners[(i + 1) % n] - c).magnitude) * 0.5f;
                float t = Mathf.Min(r / Mathf.Tan(half), maxT);
                r = t * Mathf.Tan(half);
                tIn[i] = c + u1 * t; tOut[i] = c + u2 * t; radius[i] = r;
                centre[i] = c + (u1 + u2).normalized * (r / Mathf.Sin(half));
            }
            var pts = new List<Vector2>();
            for (int i = 0; i < n; i++)
            {
                // arc around corner i from its incoming to its outgoing tangent point (the short way)
                float a0 = Mathf.Atan2(tIn[i].y - centre[i].y, tIn[i].x - centre[i].x);
                float a1 = Mathf.Atan2(tOut[i].y - centre[i].y, tOut[i].x - centre[i].x);
                float da = Mathf.DeltaAngle(a0 * Mathf.Rad2Deg, a1 * Mathf.Rad2Deg) * Mathf.Deg2Rad;
                for (int k = 0; k <= arcSamples; k++)
                {
                    float a = a0 + da * k / arcSamples;
                    pts.Add(centre[i] + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius[i]);
                }
                // straight side to the next corner's arc
                Vector2 from = tOut[i], to = tIn[(i + 1) % n];
                int steps = Mathf.Max(1, Mathf.CeilToInt((to - from).magnitude / 12f));
                for (int k = 1; k < steps; k++) pts.Add(Vector2.Lerp(from, to, (float)k / steps));
            }
            return pts;
        }

        float SnapEndToRing(List<Vector3> feed)
        {
            float s = Ring.Closest(feed[feed.Count - 1]);
            feed[feed.Count - 1] = Ring.Point(s);
            return s;
        }

        static List<Vector3> ToWorld(ScreenMap map, Vector2[] px)
        {
            var res = new List<Vector3>(px.Length);
            for (int i = 0; i < px.Length; i++) res.Add(map.Ground(px[i]));
            return res;
        }
    }
}
