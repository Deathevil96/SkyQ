using UnityEngine;

namespace SkyQueue
{
    /// <summary>Maps logical grid cells to world space for the rotated traffic board. Pure placement math; no puzzle logic.</summary>
    public readonly struct BoardLayout
    {
        public readonly Vector3 Center, AxisRight, AxisDown;
        public readonly float Cell;
        readonly float pickHeight;   // roof height of a plane, used for tap picking
        public readonly int Cols, Rows;

        public BoardLayout(ScreenMap map, GameConfig cfg, int cols, int rows)
        {
            Cols = cols; Rows = rows;
            pickHeight = cfg.boardPickHeight;
            float cellPx = Mathf.Clamp(cfg.boardFitPx / Mathf.Max(cols, rows), cfg.minCellPx, cfg.maxCellPx);
            Center = map.Ground(cfg.boardCenterPx);
            Cell = map.WorldDistance(cfg.boardCenterPx, cfg.boardCenterPx + new Vector2(cellPx, 0f));
            float a = cfg.boardAngleDeg * Mathf.Deg2Rad;
            AxisRight = new Vector3(Mathf.Cos(a), 0f, -Mathf.Sin(a));
            AxisDown = new Vector3(-Mathf.Sin(a), 0f, -Mathf.Cos(a));
        }

        public Vector3 CellCenter(float col, float row)
        {
            return Center + AxisRight * ((col + 0.5f - Cols * 0.5f) * Cell) + AxisDown * ((row + 0.5f - Rows * 0.5f) * Cell);
        }

        public Vector3 PlaneCenter(PlaneData p) { return CellCenter(p.Col + (p.W - 1) * 0.5f, p.Row + (p.H - 1) * 0.5f); }

        public Vector3 DirWorld(Dir d)
        {
            switch (d)
            {
                case Dir.Right: return AxisRight;
                case Dir.Left: return -AxisRight;
                case Dir.Down: return AxisDown;
                default: return -AxisDown;
            }
        }

        public Quaternion PlaneRotation(Dir d) { return Quaternion.LookRotation(DirWorld(d), Vector3.up); }

        /// <summary>Converts a camera ray into the grid cell under it (no physics involved).</summary>
        public bool TryPickCell(Ray ray, out int col, out int row)
        {
            col = row = -1;
            if (Mathf.Abs(ray.direction.y) < 1e-5f) return false;
            float t = (pickHeight - ray.origin.y) / ray.direction.y;
            Vector3 d = ray.origin + ray.direction * t - Center;
            col = Mathf.FloorToInt(Vector3.Dot(d, AxisRight) / Cell + Cols * 0.5f);
            row = Mathf.FloorToInt(Vector3.Dot(d, AxisDown) / Cell + Rows * 0.5f);
            return true;
        }
    }
}
