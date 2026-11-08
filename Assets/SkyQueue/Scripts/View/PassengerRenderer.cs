using System;
using System.Collections.Generic;
using UnityEngine;

namespace SkyQueue
{
    /// <summary>
    /// GPU-instanced drawing of ring + feed passengers and boarding walkers. Reads authoritative ring state only.
    /// One instanced batch per (character archetype, colour); each row mixes archetypes, stable per row.
    /// </summary>
    public sealed class PassengerRenderer : MonoBehaviour
    {
        struct Walker { public Vector3 a, b; public float t, dur; public int color, arch; public PlaneView target; }

        int colors, archs;
        Mesh[] meshes;
        float[] lifts;                 // per archetype: raise so the feet stand on the ground (model pivot is mid-body)
        float modelScale = 1f;
        Material[] mats;               // [arch * colors + colour]
        List<Matrix4x4>[] lists;       // same indexing
        readonly List<Walker> walkers = new List<Walker>();
        readonly Matrix4x4[] buf = new Matrix4x4[1023];
        readonly System.Random walkerRng = new System.Random();
        PassengerRing ring; Polyline ringPath; Polyline[] feedPaths; GameConfig cfg; Camera cam;

        float laneWidth;   // side-by-side passenger spacing, derived from the road width

        public void Bind(PassengerRing r, Polyline ringP, Polyline[] feeds, GameConfig c, Camera camera, float laneSpacing)
        {
            laneWidth = laneSpacing;
            ring = r; ringPath = ringP; feedPaths = feeds; cfg = c; cam = camera; walkers.Clear();
            BuildBatches(c.passengerArt);
        }

        void BuildBatches(PassengerArtSettings a)
        {
            colors = Palette.Count;
            bool art = a.models != null && a.models.Length > 0 && a.models[0] != null;
            archs = art ? a.models.Length : 1;
            meshes = new Mesh[archs]; lifts = new float[archs];
            modelScale = art ? a.modelScale : 1f;
            mats = new Material[archs * colors];
            lists = new List<Matrix4x4>[archs * colors];
            for (int k = 0; k < archs; k++)
            {
                meshes[k] = art ? a.models[k] : Art.PassengerMesh(a);
                lifts[k] = art ? -meshes[k].bounds.min.y : 0f;
                for (int c = 0; c < colors; c++)
                {
                    int i = k * colors + c;
                    Texture2D tex = art && a.colorTextures != null && i < a.colorTextures.Length ? a.colorTextures[i] : null;
                    mats[i] = tex != null ? Art.LitTextured(tex, Color.white, a.smoothness, true) : Art.Lit(Palette.Colors[c], a.smoothness, true);
                    lists[i] = new List<Matrix4x4>(256);
                }
            }
        }

        /// <summary>Archetype for one passenger: stable for the row's lifetime, varied across lanes and rows.</summary>
        int ArchOf(PassengerRing.Row row, int lane)
        {
            if (archs <= 1) return 0;
            uint h = (uint)System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(row) * 2654435761u + (uint)lane * 40503u;
            return (int)((h >> 16) % (uint)archs);
        }

        Matrix4x4 Pose(Vector3 pos, Quaternion rot, float scale, int arch)
        {
            float s = scale * modelScale;
            pos.y += lifts[arch] * s;
            return Matrix4x4.TRS(pos, rot, Vector3.one * s);
        }

        Vector3 Place(Polyline path, float s, int lane)
        {
            Vector3 t = path.Tangent(s);
            Vector3 n = new Vector3(-t.z, 0, t.x);
            return path.Point(s) + n * ((lane - (Rules.RowSize - 1) * 0.5f) * laneWidth);
        }

        public Vector3 LoopPoint(int lane, float s) { return Place(ringPath, s, lane); }

        /// <summary>One passenger walks from its lane at the exit gap to the plane; it waits <paramref name="delay"/> seconds first.</summary>
        public void SpawnWalker(int lane, float s, Vector3 target, int color, PlaneView view, float delay, float duration)
        {
            walkers.Add(new Walker { a = LoopPoint(lane, s), b = target, t = -delay, dur = Mathf.Max(0.01f, duration), color = color, arch = walkerRng.Next(Mathf.Max(1, archs)), target = view });
        }

        bool Visible(Vector3 p)
        {
            Vector3 v = cam.WorldToViewportPoint(p);
            float m = cfg.feel.cullMargin;
            return v.z > 0 && v.x > -m && v.x < 1f + m && v.y > -m && v.y < 1f + m;
        }

        void AddRow(Polyline path, PassengerRing.Row row, float time, int salt)
        {
            if (!Visible(path.Point(row.S))) return;
            FeelConfig f = cfg.feel;
            Quaternion rot = Quaternion.LookRotation(path.Tangent(row.S), Vector3.up);
            for (int lane = 0; lane < Rules.RowSize; lane++)
            {
                Vector3 pos = Place(path, row.S, lane);
                pos.y = cfg.reduceMotion ? 0f : Mathf.Abs(Mathf.Sin(time * f.bobSpeed + salt * f.bobRowPhase + lane * f.bobLanePhase)) * f.bobHeight;
                int arch = ArchOf(row, lane);
                lists[arch * colors + row.Color].Add(Pose(pos, rot, cfg.passengerScale, arch));
            }
        }

        /// <summary>Instances drawn last frame (for the performance probe).</summary>
        public static int LastInstanceCount, LastWalkerCount;

        void LateUpdate()
        {
            if (ring == null) return;
            using (Markers.PassengerDraw.Auto()) DrawAll();
        }

        void DrawAll()
        {
            for (int i = 0; i < lists.Length; i++) lists[i].Clear();
            float time = Time.time;

            for (int j = 0; j < ring.Ring.Count; j++) AddRow(ringPath, ring.Ring[j], time, j);
            for (int f = 0; f < 2; f++)
            {
                var feed = ring.Feed[f];
                for (int j = 0; j < feed.Count; j++)
                {
                    if (feed[j].S < 0f) continue;   // still queued above the screen
                    AddRow(feedPaths[f], feed[j], time, j);
                }
            }

            for (int i = walkers.Count - 1; i >= 0; i--)
            {
                var w = walkers[i];
                w.t += Time.deltaTime;
                float k = Mathf.Clamp01(w.t / w.dur);   // negative t: still standing at the gap, waiting for its turn
                Vector3 pos = Vector3.Lerp(w.a, w.b, k);
                pos.y = Mathf.Sin(k * Mathf.PI) * cfg.walkerHop;
                Vector3 dir = (w.b - w.a); dir.y = 0;
                Quaternion rot = dir.sqrMagnitude > 1e-4f ? Quaternion.LookRotation(dir.normalized) : Quaternion.identity;
                lists[w.arch * colors + w.color].Add(Pose(pos, rot, cfg.passengerScale * (1f - cfg.feel.walkerShrink * k), w.arch));
                if (k >= 1f)
                {
                    if (w.target != null) w.target.WalkerArrived();
                    walkers.RemoveAt(i);
                }
                else walkers[i] = w;
            }

            int total = 0;
            for (int i = 0; i < lists.Length; i++) { total += lists[i].Count; Draw(i); }
            LastInstanceCount = total;
            LastWalkerCount = walkers.Count;
        }

        void Draw(int i)
        {
            var l = lists[i];
            int n = l.Count, start = 0;
            if (n == 0) return;
            var mesh = meshes[i / colors];
            var rp = new RenderParams(mats[i]) { shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off, receiveShadows = false, worldBounds = new Bounds(Vector3.zero, Vector3.one * 2000f) };
            while (start < n)
            {
                int cnt = Mathf.Min(1023, n - start);
                l.CopyTo(start, buf, 0, cnt);
                Graphics.RenderMeshInstanced(rp, mesh, 0, buf, cnt, 0);
                start += cnt;
            }
        }
    }
}
