using UnityEngine;

namespace SkyQueue
{
    /// <summary>
    /// World-space positions for the boarding-stand row ([VIP] [plain...] [purchasable...], per <see cref="Rules"/>), as in the
    /// reference: rounded pads tilted a little to the left (top leans left), long enough that parked planes keep their board size.
    /// </summary>
    public sealed class SlotLayout
    {
        public readonly int Count;
        public readonly Vector3[] Positions;
        public readonly Vector3[] Forward;       // per stand: unit ground direction from the pad's bottom to its top (parked nose)
        public readonly Quaternion[] Rotations;  // per stand: parked-plane rotation (nose along Forward)
        public readonly float SlotLength;        // world length of one stand (inner pad, along its tilted axis)
        public readonly float SlotWidth;         // world width of one stand
        public readonly float LabelOffset;       // distance below the stand centre for the remaining-seats badge
        public readonly float ZSouth;            // z of the lane just below the stands (approach corridor)
        public readonly float RailLeft, RailRight;   // side rails just inside the actual screen edges (any screen shape)
        public readonly float FloorZ;                // invisible line above the booster bar that leaving planes never cross
        readonly GameConfig cfg;
        readonly Vector2 upPx, rightPx;              // pad axes in reference px (y down)

        public SlotLayout(ScreenMap map, GameConfig config)
        {
            cfg = config;
            Count = Rules.TotalSlots;
            float t = cfg.slotTiltDeg * Mathf.Deg2Rad;
            upPx = new Vector2(-Mathf.Sin(t), -Mathf.Cos(t));       // positive tilt: the top of the pad leans left
            rightPx = new Vector2(Mathf.Cos(t), -Mathf.Sin(t));

            Positions = new Vector3[Count];
            Forward = new Vector3[Count];
            Rotations = new Quaternion[Count];
            for (int i = 0; i < Count; i++)
            {
                Vector2 c = PxCenter(i);
                Positions[i] = map.Ground(c);
                Vector3 f = map.Ground(c + upPx * cfg.slotHalfHeightPx) - map.Ground(c - upPx * cfg.slotHalfHeightPx);
                f.y = 0f;
                Forward[i] = f.normalized;
                Rotations[i] = Quaternion.LookRotation(Forward[i], Vector3.up);
            }

            Vector2 mid = PxCenter(Rules.FirstPlainSlot);
            SlotLength = map.WorldDistance(mid - upPx * cfg.slotHalfHeightPx, mid + upPx * cfg.slotHalfHeightPx);
            SlotWidth = map.WorldDistance(mid - rightPx * cfg.slotHalfWidthPx, mid + rightPx * cfg.slotHalfWidthPx);
            LabelOffset = SlotLength * 0.5f + cfg.slotLabelGap;
            ZSouth = map.Ground(GameConfig.RefW * 0.5f, mid.y + cfg.slotApproachBelowPx).z;
            RailLeft = map.Ground(map.SafeEdgeRefX(true) + cfg.railInsetPx, mid.y).x;
            RailRight = map.Ground(map.SafeEdgeRefX(false) - cfg.railInsetPx, mid.y).x;
            float boosterTop = cfg.hud.boosterY - cfg.hud.boosterSize.y * 0.5f;
            FloorZ = map.Ground(GameConfig.RefW * 0.5f, boosterTop - cfg.exitFloorMarginPx).z;
        }

        /// <summary>Corner of stand <paramref name="i"/>'s pad in reference px, grown by <paramref name="grow"/> px (border).
        /// <paramref name="sx"/>/<paramref name="sy"/> pick the corner (-1 or +1; sy = +1 is the top).</summary>
        public Vector2 CornerPx(int i, float sx, float sy, float grow)
        {
            return PxCenter(i) + rightPx * (sx * (cfg.slotHalfWidthPx + grow)) + upPx * (sy * (cfg.slotHalfHeightPx + grow));
        }

        /// <summary>Where a plane on the approach lane turns up into stand <paramref name="i"/> (on the pad's tilted axis).</summary>
        public Vector3 ApproachPoint(int i)
        {
            Vector3 f = Forward[i], p = Positions[i];
            float back = Mathf.Abs(f.z) > 1e-4f ? (p.z - ZSouth) / f.z : 0f;
            return new Vector3(p.x - f.x * back, 0f, ZSouth);
        }

        /// <summary>Stand under a ground point (tilted pad test), or -1.</summary>
        public int StandAt(Vector3 ground)
        {
            for (int i = 0; i < Count; i++)
            {
                Vector3 d = ground - Positions[i];
                Vector3 f = Forward[i], r = new Vector3(f.z, 0f, -f.x);
                if (Mathf.Abs(d.x * r.x + d.z * r.z) <= SlotWidth * 0.5f && Mathf.Abs(d.x * f.x + d.z * f.z) <= SlotLength * 0.5f) return i;
            }
            return -1;
        }

        public SlotKind KindOf(int i)
        {
            if (Rules.VipSlot && i == 0) return SlotKind.Vip;
            return i < Rules.FirstPlainSlot + Rules.DefaultSlots ? SlotKind.Normal : SlotKind.Purchasable;
        }

        public Vector2 PxCenter(int i) { return new Vector2(cfg.slotFirstPx.x + (i - Rules.FirstPlainSlot) * cfg.slotStepPx, cfg.slotFirstPx.y); }

        /// <summary>Parked-plane scale. Planes keep their board size (1) whenever the longest plane fits the pad; only a board
        /// whose cells are too big for the pad shrinks them, uniformly, just enough to fit.</summary>
        public float PlaneScale(float boardCell)
        {
            float longest = Rules.Length[(int)PlaneSize.Large] * boardCell * cfg.planeArt.lengthFill;
            return Mathf.Min(1f, SlotLength * cfg.slotPlaneFill / longest);
        }
    }
}
