using System;
using System.Collections.Generic;
using UnityEngine;

namespace SkyQueue
{
    public enum Dir { Up, Down, Left, Right }
    public enum PlaneSize { Small = 0, Medium = 1, Large = 2 }
    public enum PlaneState { OnBoard, MovingOut, AtBoardingSlot, Full, Departing, Departed }
    public enum Difficulty { Normal, Hard, SuperHard }
    public enum SlotKind { Normal, Vip, Purchasable }

    /// <summary>Serializable source of the gameplay rule values (edited on GameConfig, applied once at start-up).</summary>
    [Serializable]
    public sealed class RulesConfig
    {
        [Tooltip("Seats per plane size: Small, Medium, Large.")]
        public int[] capacities = { 16, 24, 40 };
        [Tooltip("Footprint length in grid cells per plane size (width is always 1).")]
        public int[] lengths = { 2, 3, 4 };
        [Tooltip("Passengers per loop row. Every capacity must be a multiple of this.")]
        public int rowSize = 4;
        [Tooltip("Plain boarding stands that are open from the start.")]
        public int defaultSlots = 4;
        [Tooltip("Locked '+' stands that can be unlocked later.")]
        public int purchasableSlots = 3;
        [Tooltip("Whether the gold VIP stand exists (locked until the VIP booster ships).")]
        public bool vipSlot = true;
    }

    /// <summary>Gameplay rule values. Defaults match the design doc; <see cref="Configure"/> overrides them from data.</summary>
    public static class Rules
    {
        public static int[] Capacity = { 16, 24, 40 };
        public static int[] Length = { 2, 3, 4 };
        public static int RowSize = 4;
        public static int DefaultSlots = 4;
        public static int PurchasableSlots = 3;
        public static bool VipSlot = true;

        /// <summary>Total stands in the row: optional VIP + plain + purchasable (order matches the stand row left to right).</summary>
        public static int TotalSlots { get { return (VipSlot ? 1 : 0) + DefaultSlots + PurchasableSlots; } }
        public static int FirstPlainSlot { get { return VipSlot ? 1 : 0; } }

        /// <summary>Applies and validates a rule set. Returns false (leaving the previous values) when invalid.</summary>
        public static bool Configure(RulesConfig c)
        {
            if (c == null || c.capacities == null || c.lengths == null || c.capacities.Length != 3 || c.lengths.Length != 3 || c.rowSize < 1)
            {
                Debug.LogError("[SkyQueue] Invalid RulesConfig: need 3 capacities, 3 lengths and rowSize >= 1.");
                return false;
            }
            foreach (int cap in c.capacities)
                if (cap <= 0 || cap % c.rowSize != 0)
                {
                    Debug.LogError("[SkyQueue] Invalid RulesConfig: capacity " + cap + " is not a positive multiple of rowSize " + c.rowSize + ".");
                    return false;
                }
            Capacity = (int[])c.capacities.Clone();
            Length = (int[])c.lengths.Clone();
            RowSize = c.rowSize;
            DefaultSlots = Mathf.Max(1, c.defaultSlots);
            PurchasableSlots = Mathf.Max(0, c.purchasableSlots);
            VipSlot = c.vipSlot;
            return true;
        }
    }

    public static class DirUtil
    {
        /// <summary>Grid delta. Row grows downward on screen, so Up = -row.</summary>
        public static Vector2Int Delta(Dir d)
        {
            switch (d)
            {
                case Dir.Up: return new Vector2Int(0, -1);
                case Dir.Down: return new Vector2Int(0, 1);
                case Dir.Left: return new Vector2Int(-1, 0);
                default: return new Vector2Int(1, 0);
            }
        }
        public static bool IsHorizontal(Dir d) { return d == Dir.Left || d == Dir.Right; }
    }

    public sealed class PlaneData
    {
        public int Id;
        public int Color;
        public PlaneSize Size;
        public int Col, Row;          // top-left cell
        public Dir Dir;
        public PlaneState State = PlaneState.OnBoard;
        public int Boarded;
        public int Slot = -1;

        public int Length { get { return Rules.Length[(int)Size]; } }
        public int Capacity { get { return Rules.Capacity[(int)Size]; } }
        public int Remaining { get { return Capacity - Boarded; } }
        public bool Horizontal { get { return DirUtil.IsHorizontal(Dir); } }
        public int W { get { return Horizontal ? Length : 1; } }
        public int H { get { return Horizontal ? 1 : Length; } }

        public IEnumerable<Vector2Int> Cells()
        {
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                    yield return new Vector2Int(Col + x, Row + y);
        }

        /// <summary>The cell at the nose (leading edge) of the plane.</summary>
        public Vector2Int LeadCell()
        {
            switch (Dir)
            {
                case Dir.Right: return new Vector2Int(Col + Length - 1, Row);
                case Dir.Left: return new Vector2Int(Col, Row);
                case Dir.Down: return new Vector2Int(Col, Row + Length - 1);
                default: return new Vector2Int(Col, Row);
            }
        }

        static bool Allowed(PlaneState a, PlaneState b)
        {
            switch (a)
            {
                case PlaneState.OnBoard: return b == PlaneState.MovingOut;
                case PlaneState.MovingOut: return b == PlaneState.AtBoardingSlot;
                case PlaneState.AtBoardingSlot: return b == PlaneState.Full;
                case PlaneState.Full: return b == PlaneState.Departing;
                case PlaneState.Departing: return b == PlaneState.Departed;
                default: return false;
            }
        }

        /// <summary>State machine: rejects invalid transitions.</summary>
        public bool TrySetState(PlaneState next)
        {
            if (!Allowed(State, next))
            {
                Debug.LogWarning("[SkyQueue] Rejected plane " + Id + " transition " + State + " -> " + next);
                return false;
            }
            State = next;
            return true;
        }
    }

    public sealed class SlotData
    {
        public SlotKind Kind;
        public bool Unlocked;
        public int PlaneId = -1;
        public bool Free { get { return Unlocked && PlaneId < 0; } }
    }

    public sealed class LevelData
    {
        public int Id;
        public Difficulty Difficulty;
        public int Cols, Rows;
        public List<PlaneData> Planes = new List<PlaneData>();
        public List<int[]> PassengerBands = new List<int[]>(); // [colour, size] in entry order; bands alternate between the two feeds
        public int PassengerCount { get { int n = 0; foreach (var b in PassengerBands) n += b[1]; return n; } }
        public int[] Solution = new int[0];                    // plane ids in a valid removal order
        public int ColorCount;
        public float LoopSpeedScale = 1f;
    }

    /// <summary>Passenger/plane colour palette (index = colour id). Defaults are overridden from GameConfig at start-up.</summary>
    public static class Palette
    {
        public static Color[] Colors =
        {
            new Color(1.00f, 0.55f, 0.10f), // 0 orange
            new Color(1.00f, 0.84f, 0.12f), // 1 yellow
            new Color(0.20f, 0.55f, 1.00f), // 2 blue
            new Color(0.30f, 0.85f, 0.20f), // 3 green
            new Color(0.95f, 0.20f, 0.20f), // 4 red
            new Color(1.00f, 0.45f, 0.75f), // 5 pink
            new Color(0.60f, 0.25f, 0.90f), // 6 purple
            new Color(0.45f, 0.28f, 0.15f), // 7 brown
        };

        public static int Count { get { return Colors.Length; } }

        public static void Configure(Color[] colors)
        {
            if (colors == null || colors.Length == 0) return;
            Colors = (Color[])colors.Clone();
        }
    }
}
