using System.Collections.Generic;
using UnityEngine;

namespace SkyQueue
{
    /// <summary>Builds the smooth travel paths for plane launch (board -> stand) and departure (stand -> takeoff).</summary>
    public static class PlaneRoutes
    {
        /// <summary>Plane drives out along its arrow, loops round the on-screen side rail if needed, then enters its stand from below.</summary>
        public static Polyline Launch(PlaneData p, Vector3 start, BoardLayout board, int cellsToEdge, SlotLayout slots, int slot, float exitMarginCells, float bodyHalfCells, float turnRadiusCells)
        {
            Vector3 forward = board.DirWorld(p.Dir);
            float travel = (cellsToEdge + p.Length * 0.5f + (p.Dir == Dir.Up ? 0.2f : p.Length * 0.5f + exitMarginCells)) * board.Cell;
            Vector3 exit = start + forward * travel;
            // never cross the invisible line above the booster bar: a downward run stops there and turns along it
            float body = bodyHalfCells * board.Cell;   // keep the whole plane, not just its centre, inside the lines
            float floor = slots.FloorZ + body;
            if (forward.z < -1e-4f && exit.z < floor)
                exit = start + forward * Mathf.Max(0f, (floor - start.z) / forward.z);
            Vector3 stand = slots.Positions[slot];

            var pts = new List<Vector3>(7) { start, exit };
            if (p.Dir != Dir.Up)
            {
                bool left = p.Dir == Dir.Left || (p.Dir == Dir.Down && exit.x < board.Center.x);
                float rail = left ? slots.RailLeft + body : slots.RailRight - body;
                // never run past the rail: the whole route stays on screen
                if (Mathf.Abs(forward.x) > 1e-4f && (left ? exit.x < rail : exit.x > rail))
                    exit = start + forward * Mathf.Max(0f, (rail - start.x) / forward.x);
                pts.Add(new Vector3(rail, 0f, exit.z));
                pts.Add(new Vector3(rail, 0f, slots.ZSouth));
            }
            pts.Add(slots.ApproachPoint(slot));   // turn up into the tilted pad along its axis
            pts.Add(stand);
            return new Polyline(PathUtil.RoundedCorners(pts, turnRadiusCells * board.Cell, 10), false);
        }

        /// <summary>Taxi out to the right of the stand row and lift off past the screen edge.</summary>
        public static Polyline Depart(Vector3 stand, ScreenMap map, SlotLayout slots, FeelConfig feel)
        {
            float rowY = slots.PxCenter(2).y;
            var pts = new List<Vector3>(4)
            {
                stand,
                stand + new Vector3(feel.departTaxi.x, 0f, feel.departTaxi.y),
                map.Ground(feel.departMidPx.x, rowY + feel.departMidPx.y),
                map.Ground(feel.departEndPx.x, rowY + feel.departEndPx.y)
            };
            return new Polyline(PathUtil.CatmullRom(pts, false, 12), false);
        }
    }
}
