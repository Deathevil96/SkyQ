using System.Collections.Generic;
using UnityEngine;

namespace SkyQueue
{
    /// <summary>Builds the static, one-time scenery: floors, loop track, runway, boarding stands and decor. Placeholder art.</summary>
    public static class EnvironmentBuilder
    {
        public static Transform Build(ScreenMap map, SlotLayout slots, TrackLayout track, GameConfig cfg, EnvironmentTheme th)
        {
            var root = new GameObject("Environment (" + th.name + ")").transform;
            BuildGround(root, map, th);
            BuildTrack(root, track, th);
            BuildSlots(root, map, slots, cfg, th);
            BuildTree(root, map.Ground(th.treeLeftPx), th.treeLeftScale, th);
            BuildTree(root, map.Ground(th.treeRightPx), th.treeRightScale, th);
            if (th.ambientFrames != null && th.ambientFrames.Length > 0)
                AmbientFlipbook.Create(root, "Ambient", th.ambientFrames, map.Ground(th.ambientPx) + Vector3.up * th.ambientWorldSize * 0.5f,
                                       th.ambientWorldSize, th.ambientFps, th.ambientRestSeconds, map.Cam);
            return root;
        }

        static void Poly(Transform root, ScreenMap map, string name, Color c, float y, params Vector2[] px)
        {
            Art.MeshObject(name, Art.PolygonMeshEar(ToWorld(map, px, y)), Art.Lit(c, 0.15f), root);
        }

        /// <summary>Ground polygon with a world-tiled texture (plain colour when the texture is not set).</summary>
        static void TexPoly(Transform root, ScreenMap map, string name, Texture2D tex, Color tint, Color fallback, float tile, float y, params Vector2[] px)
        {
            var mesh = Art.PolygonMeshEar(ToWorld(map, px, y));
            if (tex != null) Art.PlanarUV(mesh, tile);
            Art.MeshObject(name, mesh, tex != null ? Art.LitTextured(tex, tint) : Art.Lit(fallback, 0.15f), root);
        }

        /// <summary>Road surface (ribbon or polygon) with the world-tiled asphalt texture.</summary>
        static void Road(Transform root, string name, Mesh mesh, EnvironmentTheme th, Material plain)
        {
            if (th.asphaltTexture != null) Art.PlanarUV(mesh, th.asphaltTileWorld);
            Art.MeshObject(name, mesh, th.asphaltTexture != null ? Art.LitTextured(th.asphaltTexture, th.roadTint) : plain, root);
        }

        static List<Vector3> ToWorld(ScreenMap map, Vector2[] px, float y, float scale = 1f, Vector2 around = default(Vector2))
        {
            var res = new List<Vector3>(px.Length);
            for (int i = 0; i < px.Length; i++)
            {
                Vector2 p = scale != 1f ? around + (px[i] - around) * scale : px[i];
                res.Add(map.Ground(p, y));
            }
            return res;
        }

        static void BuildGround(Transform root, ScreenMap map, EnvironmentTheme th)
        {
            float rx = th.reachX, ry = th.reachY, w = GameConfig.RefW, h = GameConfig.RefH;
            // floor base, garden above the runway edge, asphalt runway (outline follows the reference)
            Poly(root, map, "Floor", th.floor, -0.10f, new Vector2(-rx, -ry), new Vector2(w + rx, -ry), new Vector2(w + rx, h + ry), new Vector2(-rx, h + ry));
            Texture2D g = th.gardenTexture, a = th.runwayTexture != null ? th.runwayTexture : th.asphaltTexture;
            float gt = th.gardenTileWorld, at = th.asphaltTileWorld;
            TexPoly(root, map, "Garden", g, th.gardenTint, th.garden, gt, -0.08f, new Vector2(-rx, -ry), new Vector2(w + rx, -ry), new Vector2(w + rx, th.gardenBottomY), new Vector2(-rx, th.gardenBottomY));
            TexPoly(root, map, "GardenWedgeL", g, th.gardenTint, th.garden, gt, -0.08f, new Vector2(-rx, th.gardenBottomY), new Vector2(th.runwayNotchLeftX, th.gardenBottomY), new Vector2(0, th.runwayTopY), new Vector2(-rx, th.runwayTopY));
            TexPoly(root, map, "GardenWedgeR", g, th.gardenTint, th.garden, gt, -0.08f, new Vector2(th.runwayNotchRightX, th.gardenBottomY), new Vector2(w + rx, th.gardenBottomY), new Vector2(w + rx, th.runwayTopY), new Vector2(w, th.runwayTopY));
            TexPoly(root, map, "Runway", a, th.runwayTint, th.runway, at, -0.06f, new Vector2(-rx, th.runwayTopY), new Vector2(0, th.runwayTopY), new Vector2(th.runwayNotchLeftX, th.gardenBottomY),
                new Vector2(th.runwayNotchRightX, th.gardenBottomY), new Vector2(w, th.runwayTopY), new Vector2(w + rx, th.runwayTopY), new Vector2(w + rx, th.runwayBottomY), new Vector2(-rx, th.runwayBottomY));
        }

        /// <summary>
        /// Road = smooth strips along the passenger centre lines (ring, both feeds, exit neck) over a slightly wider white rim
        /// strip, so every edge is round. The islands are the garden showing through, outlined by the rim.
        /// </summary>
        static void BuildTrack(Transform root, TrackLayout track, EnvironmentTheme th)
        {
            TrackDefinition d = track.Definition;
            float ringHalf = track.RingHalfWidth, feedHalf = track.FeedHalfWidth;
            var rim = Art.Lit(th.rim, 0.2f);
            var road = Art.Lit(th.road, 0.15f);
            var neck = new List<Vector3>(2) { track.ExitStubStart, track.ExitStubEnd };

            Art.MeshObject("RingRim", Art.RibbonMesh(Points(track.Ring), ringHalf + d.rimWidth, true, RimY), rim, root);
            Art.MeshObject("FeedRimL", Art.RibbonMesh(Points(track.Feeds[0]), feedHalf + d.rimWidth, false, RimY), rim, root);
            Art.MeshObject("FeedRimR", Art.RibbonMesh(Points(track.Feeds[1]), feedHalf + d.rimWidth, false, RimY), rim, root);
            Art.MeshObject("ExitRim", Art.RibbonMesh(neck, track.ExitStubHalfWidth + d.rimWidth, false, RimY), rim, root);

            Road(root, "Ring", Art.RibbonMesh(Points(track.Ring), ringHalf, true, RoadY), th, road);
            Road(root, "FeedL", Art.RibbonMesh(Points(track.Feeds[0]), feedHalf, false, RoadY), th, road);
            Road(root, "FeedR", Art.RibbonMesh(Points(track.Feeds[1]), feedHalf, false, RoadY), th, road);
            Road(root, "ExitNeck", Art.RibbonMesh(neck, track.ExitStubHalfWidth, false, RoadY), th, road);

            float reach = ringHalf + feedHalf + d.junctionFillDistance;
            for (int f = 0; f < 2; f++)
            {
                var fill = JunctionFill(track.Feeds[f], track.Ring, reach);
                if (fill != null) Road(root, f == 0 ? "JunctionL" : "JunctionR", Art.PolygonMeshEar(fill), th, road);
            }
        }

        /// <summary>
        /// Road between a feed's last stretch and the ring it merges into, so the junction is one continuous surface with no
        /// garden sliver. Covers the stretch where the two centre lines are closer than <paramref name="reach"/>.
        /// </summary>
        static List<Vector3> JunctionFill(Polyline feed, Polyline ring, float reach)
        {
            int last = feed.Count - 1, start = last;
            for (int i = last - 1; i >= 0; i--)
            {
                Vector3 p = feed.Raw(i);
                if ((ring.Point(ring.Closest(p)) - p).magnitude > reach) break;
                start = i;
            }
            if (last - start < 2) return null;

            var poly = new List<Vector3>();
            for (int i = start; i <= last; i++) poly.Add(At(feed.Raw(i), RoadY));
            // back along the ring (the short way) from the merge point to the ring point beside the start of the stretch
            float sm = ring.Closest(feed.Raw(last)), sq = ring.Closest(feed.Raw(start));
            float fwd = PassengerRing.Mod(sq - sm, ring.Length), back = ring.Length - fwd;
            float span = fwd <= back ? fwd : -back;
            const int steps = 24;
            for (int k = 1; k <= steps; k++) poly.Add(At(ring.Point(PassengerRing.Mod(sm + span * k / steps, ring.Length)), RoadY));
            return poly;
        }

        static Vector3 At(Vector3 p, float y) { return new Vector3(p.x, y, p.z); }

        const float RimY = -0.07f, RoadY = -0.055f;   // garden -0.08 < rim < runway -0.06 < road

        static List<Vector3> Points(Polyline line)
        {
            var pts = new List<Vector3>(line.Count);
            for (int i = 0; i < line.Count; i++) pts.Add(line.Raw(i));
            return pts;
        }

        static void BuildSlots(Transform root, ScreenMap map, SlotLayout slots, GameConfig cfg, EnvironmentTheme th)
        {
            float b = cfg.slotBorderPx;
            StandArtSettings art = cfg.standArt;
            bool models = art != null && art.IsSet;
            for (int i = 0; i < slots.Count; i++)
            {
                SlotKind kind = slots.KindOf(i);
                Color border = kind == SlotKind.Vip ? th.slotVip : (kind == SlotKind.Purchasable ? th.slotPurchasable : th.slotNormal);
                if (models)
                {
                    // 3D pad, coloured by the world; a "+" stand also gets a hidden plain pad shown once it is bought
                    Mesh mesh = kind == SlotKind.Vip && art.vip != null ? art.vip : kind == SlotKind.Purchasable && art.locked != null ? art.locked : art.plain;
                    float yaw = mesh == art.vip ? art.vipYaw : mesh == art.locked ? art.lockedYaw : art.plainYaw;
                    var pad = BuildPad(root, slots, cfg, art, i, mesh, yaw, border, th.slotFill);
                    if (kind == SlotKind.Purchasable)
                    {
                        pad.name = "SlotLocked" + i;
                        var open = BuildPad(root, slots, cfg, art, i, art.plain, art.plainYaw, th.slotNormal, th.slotFill);
                        open.name = "SlotOpen" + i;
                        open.gameObject.SetActive(false);
                    }
                    if (kind != SlotKind.Vip) continue;   // the locked model carries its own plus sign; VIP still needs its word
                }
                else
                {
                    // tilted pads (top leans left): corners top-left, top-right, bottom-right, bottom-left
                    Poly(root, map, "SlotBorder" + i, border, -0.04f,
                        slots.CornerPx(i, -1, 1, b), slots.CornerPx(i, 1, 1, b), slots.CornerPx(i, 1, -1, b), slots.CornerPx(i, -1, -1, b));
                    Poly(root, map, "SlotFill" + i, th.slotFill, -0.03f,
                        slots.CornerPx(i, -1, 1, 0f), slots.CornerPx(i, 1, 1, 0f), slots.CornerPx(i, 1, -1, 0f), slots.CornerPx(i, -1, -1, 0f));
                }
                if (kind != SlotKind.Normal)
                {
                    bool vip = kind == SlotKind.Vip;
                    var label = Art.MakeText(vip ? "VIP" : "+", root, border, vip ? th.slotVipTextSize : th.slotPlusTextSize);
                    label.gameObject.name = "SlotLabel" + i;
                    float yaw = slots.Rotations[i].eulerAngles.y + (vip ? 90f : 0f);   // follow the pad tilt
                    label.transform.SetPositionAndRotation(slots.Positions[i] + new Vector3(0f, 0.02f, 0f), Quaternion.Euler(90f, yaw, 0f));
                }
            }
        }

        /// <summary>
        /// One 3D pad sized to stand <paramref name="i"/> (inner pad + border), turned with the stand's tilt, its top just above
        /// the ground. The model's raised top becomes the rim (rim colour), everything below it the inset fill.
        /// </summary>
        static Transform BuildPad(Transform root, SlotLayout slots, GameConfig cfg, StandArtSettings art, int i, Mesh mesh, float yaw, Color rim, Color fill)
        {
            var pad = new GameObject("SlotPad" + i).transform;
            pad.SetParent(root, false);
            pad.SetPositionAndRotation(slots.Positions[i], slots.Rotations[i]);

            Bounds mb = mesh.bounds;
            bool sideways = Mathf.Abs(Mathf.DeltaAngle(yaw, 90f)) < 45f || Mathf.Abs(Mathf.DeltaAngle(yaw, -90f)) < 45f;
            float width = slots.SlotWidth * (cfg.slotHalfWidthPx + cfg.slotBorderPx) / cfg.slotHalfWidthPx;
            float length = slots.SlotLength * (cfg.slotHalfHeightPx + cfg.slotBorderPx) / cfg.slotHalfHeightPx;
            float sx = (sideways ? length : width) / Mathf.Max(1e-4f, mb.size.x);
            float sz = (sideways ? width : length) / Mathf.Max(1e-4f, mb.size.z);
            float sy = art.thicknessWorld / Mathf.Max(1e-4f, mb.size.y);

            var model = new GameObject("Model").transform;
            model.SetParent(pad, false);
            model.localRotation = Quaternion.Euler(0f, yaw, 0f);
            model.localScale = new Vector3(sx, sy, sz);
            Vector3 c = model.localRotation * Vector3.Scale(mb.center, new Vector3(sx, 0f, sz));
            model.localPosition = new Vector3(-c.x, 0.005f - mb.max.y * sy, -c.z);   // top just above the ground

            Mesh lower, upper;
            SplitPad(mesh, art.rimCut, out lower, out upper);
            Art.MeshObject("SlotFill" + i, lower, Art.Lit(fill, art.fillSmoothness), model);
            Art.MeshObject("SlotBorder" + i, upper, Art.Lit(rim, art.rimSmoothness), model);
            return pad;
        }

        // rim / fill split per pad model, computed once and shared by every stand and every world rebuild
        static readonly Dictionary<int, Mesh[]> padSplits = new Dictionary<int, Mesh[]>();

        static void SplitPad(Mesh mesh, float rimCut, out Mesh lower, out Mesh upper)
        {
            int key = mesh.GetInstanceID() ^ Mathf.RoundToInt(rimCut * 10000f);
            Mesh[] pair;
            if (!padSplits.TryGetValue(key, out pair) || pair[0] == null)
            {
                Bounds mb = mesh.bounds; Vector3 unused;
                float floor = InnerFloorY(mesh);
                pair = new Mesh[2];
                Art.SplitAbove(mesh, floor + (mb.max.y - floor) * rimCut, out pair[0], out pair[1], out unused);
                padSplits[key] = pair;
                DevLog.Info("world", "pad '" + mesh.name + "' floor at " + ((floor - mb.min.y) / mb.size.y * 100f).ToString("0") + "% of its height; rim tris " + pair[1].triangles.Length / 3 + ", fill tris " + pair[0].triangles.Length / 3);
            }
            lower = pair[0]; upper = pair[1];
        }

        /// <summary>Height of the pad's inset surface: the level holding the most upward-facing area (above the base).</summary>
        static float InnerFloorY(Mesh mesh)
        {
            const int bins = 50;
            var v = mesh.vertices; var tri = mesh.triangles;
            Bounds mb = mesh.bounds;
            var area = new float[bins]; var sumY = new float[bins];
            for (int i = 0; i < tri.Length; i += 3)
            {
                Vector3 a = v[tri[i]], b = v[tri[i + 1]], c = v[tri[i + 2]];
                Vector3 n = Vector3.Cross(b - a, c - a);
                float ar = n.magnitude * 0.5f;
                if (ar <= 0f || n.y / (2f * ar) < 0.7f) continue;   // upward-facing only
                float y = (a.y + b.y + c.y) / 3f;
                float h = (y - mb.min.y) / Mathf.Max(1e-6f, mb.size.y);
                if (h < 0.2f) continue;                                 // ignore the underside / base plate
                int k = Mathf.Clamp((int)(h * bins), 0, bins - 1);
                area[k] += ar; sumY[k] += y * ar;
            }
            int best = 0;
            for (int k = 1; k < bins; k++) if (area[k] > area[best]) best = k;
            return area[best] > 0f ? sumY[best] / area[best] : mb.center.y;
        }

        /// <summary>Restyles a purchasable stand: open (bought for this level) looks like a plain stand, closed shows the "+".</summary>
        public static void SetStandOpen(Transform env, int i, bool open, EnvironmentTheme th)
        {
            if (env == null) return;
            var locked = env.Find("SlotLocked" + i);
            var openPad = env.Find("SlotOpen" + i);
            if (locked != null && openPad != null)   // 3D pads: swap the "+" pad for the plain one
            {
                locked.gameObject.SetActive(!open);
                openPad.gameObject.SetActive(open);
                return;
            }
            var border = env.Find("SlotBorder" + i);
            if (border != null) border.GetComponent<MeshRenderer>().sharedMaterial = Art.Lit(open ? th.slotNormal : th.slotPurchasable, 0.15f);
            var label = env.Find("SlotLabel" + i);
            if (label != null) label.gameObject.SetActive(!open);
        }

        static void BuildTree(Transform parent, Vector3 pos, float s, EnvironmentTheme th)
        {
            var root = new GameObject("Tree").transform;
            root.SetParent(parent, false);
            root.position = pos;
            var blossom = Art.Lit(th.blossom, 0.3f);
            Art.Primitive(PrimitiveType.Cylinder, "Trunk", root, new Vector3(0, 0.6f * s, 0), Quaternion.identity, new Vector3(0.25f * s, 0.6f * s, 0.25f * s), Art.Lit(th.trunk));
            Art.Primitive(PrimitiveType.Sphere, "Crown1", root, new Vector3(0, 1.7f * s, 0), Quaternion.identity, Vector3.one * 1.7f * s, blossom);
            Art.Primitive(PrimitiveType.Sphere, "Crown2", root, new Vector3(0.7f * s, 1.3f * s, 0.2f), Quaternion.identity, Vector3.one * 1.2f * s, blossom);
            Art.Primitive(PrimitiveType.Sphere, "Crown3", root, new Vector3(-0.7f * s, 1.4f * s, -0.1f), Quaternion.identity, Vector3.one * 1.3f * s, blossom);
        }
    }
}
