using System.Collections.Generic;

namespace SkyQueue.Tests
{
    /// <summary>Builders and headless "players" shared by the tests. No views, no animation: the model is driven through its public commands.</summary>
    internal static class TestSupport
    {
        public const float Dt = 1f / 30f;

        public static PlaneData Plane(int id, int color, PlaneSize size, int col, int row, Dir dir)
        {
            return new PlaneData { Id = id, Color = color, Size = size, Col = col, Row = row, Dir = dir };
        }

        /// <summary>Hand-built level. Bands are [colour, passengerCount] with the count a multiple of Rules.RowSize.</summary>
        public static LevelData Level(int cols, int rows, PlaneData[] planes, params int[][] bands)
        {
            var lvl = new LevelData { Id = 1, Cols = cols, Rows = rows, ColorCount = 8 };
            lvl.Planes.AddRange(planes);
            lvl.PassengerBands.AddRange(bands);
            return lvl;
        }

        public static GameModel Model(LevelData level) { return new GameModel(level, RingSettings.Default()); }

        /// <summary>Plays animations instantly: arrivals, departures.</summary>
        public static void SettleAnimations(GameModel m)
        {
            foreach (var p in m.Planes)
            {
                if (p.State == PlaneState.MovingOut) m.NotifyArrived(p);
                else if (p.State == PlaneState.Full) { m.BeginDeparture(p); m.NotifyDeparted(p); }
            }
        }
    }
}
