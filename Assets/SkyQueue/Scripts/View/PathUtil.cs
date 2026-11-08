using System.Collections.Generic;
using UnityEngine;

namespace SkyQueue
{
    public static class PathUtil
    {
        /// <summary>Catmull-Rom spline through pts, returned as a dense polyline.</summary>
        public static List<Vector3> CatmullRom(IList<Vector3> pts, bool closed, int per)
        {
            var res = new List<Vector3>();
            int n = pts.Count;
            int segs = closed ? n : n - 1;
            for (int i = 0; i < segs; i++)
            {
                Vector3 p0 = Get(pts, i - 1, closed), p1 = Get(pts, i, closed), p2 = Get(pts, i + 1, closed), p3 = Get(pts, i + 2, closed);
                for (int k = 0; k < per; k++)
                {
                    float t = k / (float)per, t2 = t * t, t3 = t2 * t;
                    res.Add(0.5f * ((2f * p1) + (-p0 + p2) * t + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 + (-p0 + 3f * p1 - 3f * p2 + p3) * t3));
                }
            }
            if (!closed) res.Add(pts[n - 1]);
            return res;
        }

        /// <summary>
        /// Straight segments joined by tangent circular arcs. Unlike Catmull-Rom it never swings outside a corner, so a route
        /// that turns at a boundary line stays on its side of that line. The radius shrinks where segments are short.
        /// </summary>
        public static List<Vector3> RoundedCorners(IList<Vector3> pts, float radius, int arcSamples)
        {
            var res = new List<Vector3>();
            // drop repeated points first: a zero-length leg has no direction and would make the corner maths degenerate
            var clean = new List<Vector3>(pts.Count);
            for (int i = 0; i < pts.Count; i++)
                if (clean.Count == 0 || (pts[i] - clean[clean.Count - 1]).sqrMagnitude > 1e-6f) clean.Add(pts[i]);
            pts = clean;
            int n = pts.Count;
            if (n == 0) return res;
            res.Add(pts[0]);
            for (int i = 1; i < n - 1; i++)
            {
                Vector3 a = pts[i - 1], c = pts[i], b = pts[i + 1];
                Vector3 u1 = a - c, u2 = b - c;
                float l1 = u1.magnitude, l2 = u2.magnitude;
                u1 /= l1; u2 /= l2;
                float angle = Vector3.Angle(u1, u2) * Mathf.Deg2Rad;
                if (angle > Mathf.PI - 1e-3f || angle < 1e-3f) { res.Add(c); continue; }   // straight through / reversal: no arc
                float half = angle * 0.5f;
                float t = Mathf.Min(radius / Mathf.Tan(half), l1 * 0.5f, l2 * 0.5f);
                float r = t * Mathf.Tan(half);
                Vector3 p0 = c + u1 * t, p1 = c + u2 * t;
                Vector3 centre = c + (u1 + u2).normalized * (r / Mathf.Sin(half));
                Vector3 from = p0 - centre, to = p1 - centre;
                for (int k = 0; k <= arcSamples; k++) res.Add(centre + Vector3.Slerp(from, to, k / (float)arcSamples));
            }
            res.Add(pts[n - 1]);
            return res;
        }

        static Vector3 Get(IList<Vector3> pts, int i, bool closed)
        {
            int n = pts.Count;
            if (closed) return pts[((i % n) + n) % n];
            return pts[Mathf.Clamp(i, 0, n - 1)];
        }
    }

    /// <summary>Arc-length parameterised polyline (optionally closed).</summary>
    public sealed class Polyline
    {
        readonly Vector3[] p;
        readonly float[] cum;
        public readonly float Length;
        public readonly bool Closed;

        public Polyline(List<Vector3> pts, bool closed)
        {
            Closed = closed;
            var list = new List<Vector3>(pts);
            if (closed) list.Add(pts[0]);
            p = list.ToArray();
            cum = new float[p.Length];
            for (int i = 1; i < p.Length; i++) cum[i] = cum[i - 1] + Vector3.Distance(p[i - 1], p[i]);
            Length = cum[p.Length - 1];
        }

        public int Count { get { return p.Length; } }
        public Vector3 Raw(int i) { return p[i]; }

        int Find(float s)
        {
            int lo = 0, hi = cum.Length - 1;
            while (hi - lo > 1)
            {
                int mid = (lo + hi) >> 1;
                if (cum[mid] <= s) lo = mid; else hi = mid;
            }
            return lo;
        }

        float Wrap(float s)
        {
            if (Closed) return PassengerRing.Mod(s, Length);
            return Mathf.Clamp(s, 0f, Length);
        }

        public Vector3 Point(float s)
        {
            s = Wrap(s);
            int i = Find(s);
            float seg = cum[i + 1] - cum[i];
            float t = seg > 1e-5f ? (s - cum[i]) / seg : 0f;
            return Vector3.Lerp(p[i], p[i + 1], t);
        }

        public Vector3 Tangent(float s)
        {
            s = Wrap(s);
            int i = Find(s);
            Vector3 d = p[i + 1] - p[i];
            d.y = 0;
            return d.sqrMagnitude > 1e-8f ? d.normalized : Vector3.forward;
        }

        /// <summary>Arc position of the sample closest to a world point.</summary>
        public float Closest(Vector3 w)
        {
            float best = float.MaxValue, bs = 0;
            for (int i = 0; i < p.Length; i++)
            {
                float d = (new Vector3(p[i].x, 0, p[i].z) - new Vector3(w.x, 0, w.z)).sqrMagnitude;
                if (d < best) { best = d; bs = cum[i]; }
            }
            return bs;
        }
    }
}
