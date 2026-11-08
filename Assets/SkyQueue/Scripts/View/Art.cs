using System.Collections.Generic;
using UnityEngine;

namespace SkyQueue
{
    /// <summary>Procedural placeholder art: materials and meshes. Replace with Meshy models later (see ART_ASSET_PLAN.md).</summary>
    public static class Art
    {
        static readonly Dictionary<int, Material> cache = new Dictionary<int, Material>();
        static Mesh passengerMesh, arrowMesh;
        static Material litTemplate, unlitTemplate;
        static PrimitiveMeshes shapes;

        /// <summary>Supplies the material templates (shader + variants live in project assets, never found by name at runtime).</summary>
        public static void Configure(Material lit, Material unlit, PrimitiveMeshes primitiveMeshes)
        {
            litTemplate = lit; unlitTemplate = unlit; shapes = primitiveMeshes;
            cache.Clear();
            passengerMesh = null;
        }

        public static Material Lit(Color c, float smooth = 0.65f, bool instancing = false)
        {
            int key = c.GetHashCode() ^ (int)(smooth * 1000) ^ (instancing ? 0x5555 : 0);
            Material m;
            if (cache.TryGetValue(key, out m) && m != null) return m;
            m = new Material(litTemplate);
            m.SetColor("_BaseColor", c);
            m.SetFloat("_Smoothness", smooth);
            m.SetFloat("_Metallic", 0f);
            m.enableInstancing = instancing;
            cache[key] = m;
            return m;
        }

        /// <summary>Lit material with a tiling texture (falls back to the plain colour material when <paramref name="tex"/> is null).</summary>
        public static Material LitTextured(Texture tex, Color tint, float smooth = 0.15f, bool instancing = false)
        {
            if (tex == null) return Lit(tint, smooth, instancing);
            int key = tex.GetInstanceID() ^ tint.GetHashCode() ^ (int)(smooth * 1000) ^ 0x7a7a ^ (instancing ? 0x5555 : 0);
            Material m;
            if (cache.TryGetValue(key, out m) && m != null) return m;
            m = new Material(litTemplate);
            m.SetTexture("_BaseMap", tex);
            m.SetColor("_BaseColor", tint);
            m.SetFloat("_Smoothness", smooth);
            m.SetFloat("_Metallic", 0f);
            m.enableInstancing = instancing;
            cache[key] = m;
            return m;
        }

        /// <summary>World-space planar UVs (u = x / tile, v = z / tile): every ground piece shares one continuous texture, so
        /// ribbons, polygons and junction fills meet without seams.</summary>
        public static Mesh PlanarUV(Mesh m, float tileWorld)
        {
            var v = m.vertices;
            var uv = new Vector2[v.Length];
            float k = 1f / Mathf.Max(0.01f, tileWorld);
            for (int i = 0; i < v.Length; i++) uv[i] = new Vector2(v[i].x * k, v[i].z * k);
            m.uv = uv;
            return m;
        }

        /// <summary>
        /// Splits a mesh into the triangles lying fully above <paramref name="cutY"/> and the rest (e.g. a helicopter's rotor,
        /// a pad's raised rim). Returns the centre of the upper part. Both meshes share the source vertex arrays.
        /// </summary>
        public static void SplitAbove(Mesh src, float cutY, out Mesh lower, out Mesh upper, out Vector3 upperCenter)
        {
            var v = src.vertices; var n = src.normals; var uv = src.uv; var tri = src.triangles;
            var lo = new List<int>(tri.Length); var hi = new List<int>(tri.Length / 4);
            for (int i = 0; i < tri.Length; i += 3)
            {
                bool above = v[tri[i]].y > cutY && v[tri[i + 1]].y > cutY && v[tri[i + 2]].y > cutY;
                var dst = above ? hi : lo;
                dst.Add(tri[i]); dst.Add(tri[i + 1]); dst.Add(tri[i + 2]);
            }
            lower = CopyMesh(src.name + "_lower", v, n, uv, lo);
            upper = CopyMesh(src.name + "_upper", v, n, uv, hi);
            if (hi.Count > 0)
            {
                var b = new Bounds(v[hi[0]], Vector3.zero);
                foreach (int i in hi) b.Encapsulate(v[i]);
                upperCenter = b.center;
            }
            else upperCenter = src.bounds.center;
        }

        static Mesh CopyMesh(string name, Vector3[] v, Vector3[] n, Vector2[] uv, List<int> tris)
        {
            var m = new Mesh { name = name };
            if (v.Length > 65000) m.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            m.vertices = v;
            if (n != null && n.Length == v.Length) m.normals = n;
            if (uv != null && uv.Length == v.Length) m.uv = uv;
            m.SetTriangles(tris, 0);
            m.RecalculateBounds();
            return m;
        }

        public static Material Unlit(Color c)
        {
            int key = c.GetHashCode() ^ 0x1234567;
            Material m;
            if (cache.TryGetValue(key, out m) && m != null) return m;
            m = new Material(unlitTemplate);
            m.SetColor("_BaseColor", c);
            cache[key] = m;
            return m;
        }

        static Mesh Prim(PrimitiveType t) { return shapes.Get(t); }

        public static Mesh PassengerMesh(PassengerArtSettings s)
        {
            if (passengerMesh != null) return passengerMesh;
            var capsule = Prim(PrimitiveType.Capsule);
            var sphere = Prim(PrimitiveType.Sphere);
            var body = new CombineInstance { mesh = capsule, transform = Matrix4x4.TRS(s.bodyOffset, Quaternion.identity, s.bodyScale) };
            var head = new CombineInstance { mesh = sphere, transform = Matrix4x4.TRS(s.headOffset, Quaternion.identity, s.headScale) };
            passengerMesh = new Mesh { name = "PassengerPlaceholder" };
            passengerMesh.CombineMeshes(new[] { body, head }, true, true);
            return passengerMesh;
        }

        /// <summary>Flat arrow pointing +Z, 1 unit long, 0.7 wide.</summary>
        public static Mesh ArrowMesh()
        {
            if (arrowMesh != null) return arrowMesh;
            var v = new List<Vector3>
            {
                new Vector3(0f, 0, 0.5f), new Vector3(-0.38f, 0, 0.05f), new Vector3(0.38f, 0, 0.05f),   // head
                new Vector3(-0.14f, 0, 0.05f), new Vector3(-0.14f, 0, -0.5f), new Vector3(0.14f, 0, -0.5f), new Vector3(0.14f, 0, 0.05f) // shaft
            };
            var tri = new List<int> { 0, 2, 1, 3, 6, 5, 3, 5, 4 };
            arrowMesh = new Mesh { name = "Arrow" };
            arrowMesh.SetVertices(v);
            arrowMesh.SetTriangles(tri, 0);
            arrowMesh.RecalculateNormals();
            return arrowMesh;
        }

        /// <summary>Ear-clipping triangulation for simple (possibly concave) polygons given in XZ. Works for either winding; faces up.</summary>
        public static Mesh PolygonMeshEar(IList<Vector3> pts)
        {
            int n = pts.Count;
            float area = 0;
            for (int i = 0; i < n; i++) { var a = pts[i]; var b = pts[(i + 1) % n]; area += a.x * b.z - b.x * a.z; }
            bool cw = area < 0;
            var idx = new List<int>();
            for (int i = 0; i < n; i++) idx.Add(i);
            var tris = new List<int>();
            int guard = n * n + 10;
            while (idx.Count > 3 && guard-- > 0)
            {
                bool clipped = false;
                for (int k = 0; k < idx.Count; k++)
                {
                    int ia = idx[(k + idx.Count - 1) % idx.Count], ib = idx[k], ic = idx[(k + 1) % idx.Count];
                    Vector3 a = pts[ia], b = pts[ib], c = pts[ic];
                    float cross = (b.x - a.x) * (c.z - b.z) - (b.z - a.z) * (c.x - b.x);
                    if ((cw ? -cross : cross) <= 1e-6f) continue;           // reflex vertex
                    bool inside = false;
                    for (int m = 0; m < idx.Count; m++)
                    {
                        int im = idx[m];
                        if (im == ia || im == ib || im == ic) continue;
                        if (PointInTri(pts[im], a, b, c)) { inside = true; break; }
                    }
                    if (inside) continue;
                    if (cw) { tris.Add(ia); tris.Add(ib); tris.Add(ic); } else { tris.Add(ia); tris.Add(ic); tris.Add(ib); }
                    idx.RemoveAt(k);
                    clipped = true;
                    break;
                }
                if (!clipped) break;
            }
            if (idx.Count == 3)
            {
                if (cw) { tris.Add(idx[0]); tris.Add(idx[1]); tris.Add(idx[2]); } else { tris.Add(idx[0]); tris.Add(idx[2]); tris.Add(idx[1]); }
            }
            var m2 = new Mesh();
            m2.SetVertices(new List<Vector3>(pts));
            m2.SetTriangles(tris, 0);
            m2.RecalculateNormals();
            return m2;
        }

        /// <summary>
        /// Flat, upward-facing strip of constant width following a path on the ground (y is taken from <paramref name="y"/>).
        /// Used for smooth road surfaces and rims: dense path samples give perfectly round curves with no polygon corners.
        /// </summary>
        public static Mesh RibbonMesh(IList<Vector3> pts, float halfWidth, bool closed, float y)
        {
            int n = pts.Count;
            var verts = new List<Vector3>(n * 2 + 2);
            var tris = new List<int>(n * 6 + 6);
            int count = closed ? n + 1 : n;
            for (int i = 0; i < count; i++)
            {
                int c = i % n;
                Vector3 prev = closed ? pts[(c - 1 + n) % n] : pts[Mathf.Max(c - 1, 0)];
                Vector3 next = closed ? pts[(c + 1) % n] : pts[Mathf.Min(c + 1, n - 1)];
                Vector3 t = next - prev; t.y = 0f;
                Vector3 left = new Vector3(-t.z, 0f, t.x).normalized * halfWidth;
                Vector3 p = new Vector3(pts[c].x, y, pts[c].z);
                verts.Add(p + left);   // 2i   left edge
                verts.Add(p - left);   // 2i+1 right edge
                if (i == 0) continue;
                int l0 = 2 * (i - 1), r0 = l0 + 1, l1 = 2 * i, r1 = l1 + 1;
                tris.Add(l0); tris.Add(l1); tris.Add(r0);   // clockwise seen from above -> faces up
                tris.Add(r0); tris.Add(l1); tris.Add(r1);
            }
            var m = new Mesh();
            if (verts.Count > 65000) m.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            m.SetVertices(verts);
            m.SetTriangles(tris, 0);
            m.RecalculateNormals();
            return m;
        }

        static bool PointInTri(Vector3 p, Vector3 a, Vector3 b, Vector3 c)
        {
            float d1 = Sign(p, a, b), d2 = Sign(p, b, c), d3 = Sign(p, c, a);
            bool neg = d1 < 0 || d2 < 0 || d3 < 0, pos = d1 > 0 || d2 > 0 || d3 > 0;
            return !(neg && pos);
        }
        static float Sign(Vector3 p, Vector3 a, Vector3 b) { return (p.x - b.x) * (a.z - b.z) - (a.x - b.x) * (p.z - b.z); }

        public static GameObject MeshObject(string name, Mesh mesh, Material mat, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return go;
        }

        /// <summary>A renderer-only shape (no collider: gameplay never uses physics) built from the configured primitive meshes.</summary>
        public static GameObject Primitive(PrimitiveType t, string name, Transform parent, Vector3 pos, Quaternion rot, Vector3 scale, Material mat)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos; go.transform.localRotation = rot; go.transform.localScale = scale;
            go.AddComponent<MeshFilter>().sharedMesh = Prim(t);
            go.AddComponent<MeshRenderer>().sharedMaterial = mat;
            return go;
        }

        static Font font;
        public static TextMesh MakeText(string txt, Transform parent, Color c, float charSize, int fontSize = 64)
        {
            if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var go = new GameObject("Text_" + txt);
            go.transform.SetParent(parent, false);
            var tm = go.AddComponent<TextMesh>();
            tm.font = font; tm.text = txt; tm.fontSize = fontSize; tm.characterSize = charSize;
            tm.anchor = TextAnchor.MiddleCenter; tm.alignment = TextAlignment.Center; tm.color = c;
            go.GetComponent<MeshRenderer>().sharedMaterial = font.material;
            return tm;
        }
    }
}
