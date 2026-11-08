using System.Collections.Generic;

namespace SkyQueue
{
    /// <summary>
    /// Fast, simulation-free level validation: structure, bounds, overlap, passenger/plane totals and board solvability.
    /// Cheap enough to run on every generated or hand-authored level (tools, tests, editor import) without ever stalling.
    /// </summary>
    public static class LevelValidator
    {
        public static List<string> Validate(LevelData lvl)
        {
            var errors = new List<string>();
            if (lvl == null) { errors.Add("level is null"); return errors; }
            if (lvl.Cols <= 0 || lvl.Rows <= 0) errors.Add("board has no size");
            if (lvl.Planes == null || lvl.Planes.Count == 0) { errors.Add("no planes"); return errors; }

            var cells = new HashSet<long>();
            var capacity = new int[Palette.Count];
            foreach (var p in lvl.Planes)
            {
                if (p.Color < 0 || p.Color >= Palette.Count) { errors.Add("plane " + p.Id + " has colour " + p.Color + " outside the palette"); continue; }
                capacity[p.Color] += p.Capacity;
                foreach (var c in p.Cells())
                {
                    if (c.x < 0 || c.y < 0 || c.x >= lvl.Cols || c.y >= lvl.Rows) errors.Add("plane " + p.Id + " leaves the board");
                    else if (!cells.Add((long)c.x * 100000L + c.y)) errors.Add("plane " + p.Id + " overlaps another plane at " + c.x + "," + c.y);
                }
            }

            var passengers = new int[Palette.Count];
            if (lvl.PassengerBands == null || lvl.PassengerBands.Count == 0) errors.Add("no passenger bands");
            else
                foreach (var b in lvl.PassengerBands)
                {
                    if (b == null || b.Length < 2 || b[0] < 0 || b[0] >= Palette.Count) { errors.Add("malformed passenger band"); continue; }
                    if (b[1] <= 0 || b[1] % Rules.RowSize != 0) errors.Add("band of colour " + b[0] + " is not a whole number of rows");
                    passengers[b[0]] += b[1];
                }
            for (int c = 0; c < Palette.Count; c++)
                if (capacity[c] != passengers[c]) errors.Add("colour " + c + ": planes carry " + capacity[c] + " but " + passengers[c] + " passengers wait");

            if (errors.Count == 0 && !GameModel.BoardSolvable(lvl)) errors.Add("the board cannot be fully cleared (a plane is permanently blocked)");
            else if (errors.Count == 0 && lvl.Solution != null && lvl.Solution.Length > 0 && !GameModel.IsValidOrder(lvl, lvl.Solution))
                errors.Add("the stored solution order is not a legal removal order");
            return errors;
        }

        public static bool IsValid(LevelData lvl) { return Validate(lvl).Count == 0; }
    }
}
