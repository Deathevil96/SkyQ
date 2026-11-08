using UnityEngine;

namespace SkyQueue
{
    /// <summary>Authoritative grid occupancy. Never depends on rendered transforms or physics.</summary>
    public sealed class TrafficBoard
    {
        public readonly int Cols, Rows;
        readonly int[] occ;

        public TrafficBoard(int cols, int rows)
        {
            Cols = cols; Rows = rows;
            occ = new int[cols * rows];
            for (int i = 0; i < occ.Length; i++) occ[i] = -1;
        }

        public bool InBounds(int c, int r) { return c >= 0 && r >= 0 && c < Cols && r < Rows; }
        public int At(int c, int r) { return InBounds(c, r) ? occ[r * Cols + c] : -1; }

        public bool CanPlace(PlaneData p)
        {
            foreach (var c in p.Cells())
                if (!InBounds(c.x, c.y) || occ[c.y * Cols + c.x] >= 0) return false;
            return true;
        }

        public void Place(PlaneData p)
        {
            foreach (var c in p.Cells()) occ[c.y * Cols + c.x] = p.Id;
        }

        public void Remove(PlaneData p)
        {
            foreach (var c in p.Cells())
                if (InBounds(c.x, c.y) && occ[c.y * Cols + c.x] == p.Id) occ[c.y * Cols + c.x] = -1;
        }

        /// <summary>True when every cell between the plane's nose and the board edge (in its arrow direction) is empty.</summary>
        public bool CanExit(PlaneData p)
        {
            Vector2Int d = DirUtil.Delta(p.Dir);
            Vector2Int c = p.LeadCell() + d;
            while (InBounds(c.x, c.y))
            {
                int id = occ[c.y * Cols + c.x];
                if (id >= 0 && id != p.Id) return false;
                c += d;
            }
            return true;
        }

        /// <summary>Number of cells from the nose to the board edge (exclusive of the plane).</summary>
        public int CellsToEdge(PlaneData p)
        {
            Vector2Int d = DirUtil.Delta(p.Dir);
            Vector2Int c = p.LeadCell() + d;
            int n = 0;
            while (InBounds(c.x, c.y)) { n++; c += d; }
            return n;
        }
    }
}
