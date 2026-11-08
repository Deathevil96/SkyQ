using System;
using System.Collections.Generic;
using UnityEngine;

namespace SkyQueue
{
    public enum LaunchResult { Success, Blocked, NoSlot, Invalid }

    public struct RingSettings
    {
        public float Spacing, Length, BoardS, Speed, FeedLen, FeedHeadStart, HoldGap;
        public bool StartFull;
        public float[] MergeS;
        /// <summary>View-independent defaults used by headless tests/bots (ring ~8 units, merges at 1/8 and ~2/5 around).</summary>
        public static RingSettings Default()
        {
            return new RingSettings { Spacing = 0.30f, Length = 8f, BoardS = 6.2f, Speed = 2.0f, MergeS = new[] { 1.0f, 3.2f }, FeedLen = 8f, FeedHeadStart = 2f, HoldGap = 0.6f };
        }
    }

    /// <summary>Authoritative game state. Runs without any view; visuals subscribe to events and report animation completion back.</summary>
    public sealed class GameModel
    {
        public readonly LevelData Level;
        public readonly TrafficBoard Board;
        public readonly List<PlaneData> Planes;
        public readonly SlotData[] Slots;
        public readonly PassengerRing Ring;
        readonly Dictionary<int, PlaneData> planeById;
        readonly Func<int, float, bool> boardCallback;   // cached: avoids a delegate allocation every tick
        public bool Won { get; private set; }
        public bool Failed { get; private set; }
        public int Moves { get; private set; }

        public event Action<PlaneData, int> OnPlaneLaunched;                  // plane, slot
        public event Action<PlaneData> OnPlaneArrived;
        public event Action<PlaneData, int, float> OnRowBoarded;   // plane, colour, ring arc position (a whole row boards at once)
        public event Action<PlaneData> OnPlaneFull;
        public event Action<PlaneData> OnPlaneDeparting;
        public event Action<PlaneData> OnPlaneDeparted;
        public event Action OnLevelWon;
        public event Action OnLevelFailed;
        public event Action<PlaneData> OnPlaneRecolored;   // Jumble changed a plane's colour

        public GameModel(LevelData level, RingSettings ring)
        {
            Level = level;
            Planes = level.Planes;
            Board = new TrafficBoard(level.Cols, level.Rows);
            planeById = new Dictionary<int, PlaneData>(Planes.Count);
            foreach (var p in Planes) { Board.Place(p); planeById[p.Id] = p; }
            boardCallback = TryBoard;

            // Slot layout mirrors the reference: [VIP][4 plain][3 purchasable "+"]. VIP and purchasable are locked in Phase 1.
            var slots = new List<SlotData>();
            if (Rules.VipSlot) slots.Add(new SlotData { Kind = SlotKind.Vip, Unlocked = false });
            for (int i = 0; i < Rules.DefaultSlots; i++) slots.Add(new SlotData { Kind = SlotKind.Normal, Unlocked = true });
            for (int i = 0; i < Rules.PurchasableSlots; i++) slots.Add(new SlotData { Kind = SlotKind.Purchasable, Unlocked = false });
            Slots = slots.ToArray();

            Ring = new PassengerRing(level.PassengerBands, Rules.RowSize, ring.Spacing, ring.Length, ring.BoardS, ring.MergeS, ring.FeedLen, ring.FeedHeadStart, ring.Speed * level.LoopSpeedScale, ring.HoldGap, ring.StartFull);
        }

        public PlaneData GetPlane(int id)
        {
            PlaneData p;
            return planeById.TryGetValue(id, out p) ? p : null;
        }

        int FirstFreeSlot()
        {
            for (int i = 0; i < Slots.Length; i++) if (Slots[i].Free) return i;
            return -1;
        }

        public LaunchResult TryLaunch(int planeId, out int slot)
        {
            slot = -1;
            if (Won || Failed) return LaunchResult.Invalid;
            var p = GetPlane(planeId);
            if (p == null || p.State != PlaneState.OnBoard) return LaunchResult.Invalid;
            if (!Board.CanExit(p)) return LaunchResult.Blocked;
            int s = FirstFreeSlot();
            if (s < 0) return LaunchResult.NoSlot;

            p.TrySetState(PlaneState.MovingOut);
            Board.Remove(p);
            Slots[s].PlaneId = p.Id;
            p.Slot = s;
            Moves++;
            slot = s;
            if (OnPlaneLaunched != null) OnPlaneLaunched(p, s);
            return LaunchResult.Success;
        }

        // boosters ---------------------------------------------------------------------------------------------------
        int VipSlotIndex { get { return Rules.VipSlot ? 0 : -1; } }

        bool Playing { get { return !Won && !Failed; } }

        /// <summary>The VIP stand is free, so the VIP booster can be used now.</summary>
        public bool CanUseVip { get { return Playing && VipSlotIndex >= 0 && Slots[VipSlotIndex].PlaneId < 0; } }

        /// <summary>VIP booster: any plane still on the board (even a blocked one) flies straight to the VIP stand.</summary>
        public LaunchResult TryLaunchVip(int planeId)
        {
            if (!CanUseVip) return LaunchResult.NoSlot;
            var p = GetPlane(planeId);
            if (p == null || p.State != PlaneState.OnBoard) return LaunchResult.Invalid;
            int s = VipSlotIndex;
            p.TrySetState(PlaneState.MovingOut);
            Board.Remove(p);
            Slots[s].PlaneId = p.Id;
            p.Slot = s;
            Moves++;
            if (OnPlaneLaunched != null) OnPlaneLaunched(p, s);
            return LaunchResult.Success;
        }

        /// <summary>First purchasable stand that is still closed, or -1.</summary>
        public int NextLockedPurchasableSlot()
        {
            for (int i = 0; i < Slots.Length; i++) if (Slots[i].Kind == SlotKind.Purchasable && !Slots[i].Unlocked) return i;
            return -1;
        }

        /// <summary>Extra Slot booster: opens a purchasable "+" stand for the rest of this level.</summary>
        public bool TryUnlockSlot(int index)
        {
            if (!Playing || index < 0 || index >= Slots.Length) return false;
            if (Slots[index].Kind != SlotKind.Purchasable || Slots[index].Unlocked) return false;
            Slots[index].Unlocked = true;
            return true;
        }

        /// <summary>Arrange can help only while a parked plane still has seats and passengers are waiting.</summary>
        public bool CanArrange
        {
            get
            {
                if (!Playing || Ring.Remaining == 0) return false;
                foreach (var s in Slots)
                {
                    var p = s.PlaneId >= 0 ? GetPlane(s.PlaneId) : null;
                    if (p != null && (p.State == PlaneState.AtBoardingSlot || p.State == PlaneState.MovingOut) && p.Remaining > 0) return true;
                }
                return false;
            }
        }

        /// <summary>Arrange booster: rows matching the parked planes (stand order) move to the front of the queue.</summary>
        public bool Arrange()
        {
            if (!CanArrange) return false;
            var demand = new List<int[]>();
            foreach (var s in Slots)
            {
                var p = s.PlaneId >= 0 ? GetPlane(s.PlaneId) : null;
                if (p != null && (p.State == PlaneState.AtBoardingSlot || p.State == PlaneState.MovingOut) && p.Remaining > 0)
                    demand.Add(new[] { p.Color, p.Remaining / Rules.RowSize });
            }
            return Ring.Arrange(demand);
        }

        /// <summary>Jumble can change something when two planes of one size with different colours are still on the board.</summary>
        public bool CanJumble
        {
            get
            {
                if (!Playing) return false;
                var first = new int[Rules.Length.Length];
                for (int i = 0; i < first.Length; i++) first[i] = -1;
                foreach (var p in Planes)
                {
                    if (p.State != PlaneState.OnBoard) continue;
                    int k = (int)p.Size;
                    if (first[k] < 0) first[k] = p.Color;
                    else if (first[k] != p.Color) return true;
                }
                return false;
            }
        }

        /// <summary>
        /// Jumble booster: shuffles colours among the planes still on the board, only between planes of the same size, so
        /// every colour keeps exactly the seats its passengers need and the board stays solvable (geometry is unchanged).
        /// </summary>
        public bool Jumble(System.Random rng)
        {
            if (!CanJumble) return false;
            bool changed = false;
            for (int size = 0; size < Rules.Length.Length; size++)
            {
                var group = new List<PlaneData>();
                foreach (var p in Planes) if (p.State == PlaneState.OnBoard && (int)p.Size == size) group.Add(p);
                var colors = new List<int>(group.Count);
                foreach (var p in group) colors.Add(p.Color);
                for (int i = colors.Count - 1; i > 0; i--) { int j = rng.Next(i + 1); int t = colors[i]; colors[i] = colors[j]; colors[j] = t; }
                for (int i = 0; i < group.Count; i++)
                {
                    if (group[i].Color == colors[i]) continue;
                    group[i].Color = colors[i];
                    changed = true;
                    if (OnPlaneRecolored != null) OnPlaneRecolored(group[i]);
                }
            }
            return changed;
        }

        /// <summary>View reports the plane reached its slot. Boarding is only possible after this.</summary>
        public void NotifyArrived(PlaneData p)
        {
            if (p.State != PlaneState.MovingOut) return;
            p.TrySetState(PlaneState.AtBoardingSlot);
            if (OnPlaneArrived != null) OnPlaneArrived(p);
        }

        /// <summary>View starts the departure animation: slot is freed for the next plane.</summary>
        public void BeginDeparture(PlaneData p)
        {
            if (p.State != PlaneState.Full) return;
            p.TrySetState(PlaneState.Departing);
            if (p.Slot >= 0 && Slots[p.Slot].PlaneId == p.Id) Slots[p.Slot].PlaneId = -1;
            if (OnPlaneDeparting != null) OnPlaneDeparting(p);
            CheckFail();
        }

        public void NotifyDeparted(PlaneData p)
        {
            if (p.State != PlaneState.Departing) return;
            p.TrySetState(PlaneState.Departed);
            if (OnPlaneDeparted != null) OnPlaneDeparted(p);
            CheckEnd();
        }

        float failTimer;
        public void Tick(float dt)
        {
            if (Won || Failed) return;
            using (Markers.ModelTick.Auto())
            {
                using (Markers.RingTick.Auto()) Ring.Tick(dt, boardCallback);
            CheckEnd(false);   // the loss check is throttled below: it scans every plane's exit ray
                failTimer += dt;
                if (failTimer > 0.25f) { failTimer = 0; using (Markers.FailCheck.Auto()) CheckFail(); }
            }
        }

        /// <summary>A whole row of RowSize passengers boards the first waiting plane of its colour that has room for it.</summary>
        bool TryBoard(int color, float ringS)
        {
            PlaneData target = null;
            for (int i = 0; i < Slots.Length; i++)
            {
                if (Slots[i].PlaneId < 0) continue;
                PlaneData p = GetPlane(Slots[i].PlaneId);
                if (p == null || p.State != PlaneState.AtBoardingSlot) continue;
                if (p.Color != color || p.Remaining < Rules.RowSize) continue;
                target = p; break;
            }
            if (target == null) return false;
            target.Boarded += Rules.RowSize;
            if (OnRowBoarded != null) OnRowBoarded(target, color, ringS);
            if (target.Boarded == target.Capacity)
            {
                target.TrySetState(PlaneState.Full);
                if (OnPlaneFull != null) OnPlaneFull(target);
            }
            return true;
        }

        public bool AnyLegalLaunch()
        {
            if (FirstFreeSlot() < 0) return false;
            foreach (var p in Planes)
                if (p.State == PlaneState.OnBoard && Board.CanExit(p)) return true;
            return false;
        }

        /// <summary>
        /// Loss conditions (as described by the design owner): the loop ring is full, no passenger on it can board any
        /// parked plane, and the player has no launch that could change that (all stands taken by wrong colours, or the
        /// remaining planes are jammed). A blocked tap / full stands on their own never fail the level.
        /// </summary>
        void CheckFail()
        {
            if (Won || Failed) return;
            bool anyOnBoard = false, anySlotPlane = false, pending = false;
            bool slotCanFill = false;
            foreach (var p in Planes)
            {
                switch (p.State)
                {
                    case PlaneState.OnBoard: anyOnBoard = true; break;
                    case PlaneState.MovingOut:
                    case PlaneState.AtBoardingSlot:
                        anySlotPlane = true;
                        if (p.Remaining > 0 && Ring.RingRowsOfColor(p.Color) > 0) slotCanFill = true;
                        break;
                    case PlaneState.Full:
                    case PlaneState.Departing: pending = true; break;
                }
            }
            if (!anyOnBoard && !anySlotPlane) return;      // only departures left: win check handles it
            if (pending) return;                           // a stand is about to free up; re-evaluate afterwards
            bool legal = AnyLegalLaunch();
            bool jammed = !anySlotPlane && !legal;         // nothing can ever move again
            bool launchHelps = false;
            if (legal)
                foreach (var p in Planes)
                    if (p.State == PlaneState.OnBoard && Board.CanExit(p) && Ring.RingRowsOfColor(p.Color) > 0) { launchHelps = true; break; }
            bool stuck = !slotCanFill && !launchHelps && !Ring.HasRoom;
            if (jammed || stuck)
            {
                DevLog.Info("model", "level failed: " + (jammed ? "board jammed (no plane can ever move)" : "ring full and nothing can board") + ", ring rows " + Ring.RingRows + ", has room " + Ring.HasRoom);
                Failed = true;
                if (OnLevelFailed != null) OnLevelFailed();
            }
        }

        void CheckEnd(bool evaluateFail = true)
        {
            if (Won || Failed) return;
            bool allDeparted = true;
            foreach (var p in Planes) if (p.State != PlaneState.Departed) { allDeparted = false; break; }
            if (allDeparted && Ring.Remaining == 0)
            {
                Won = true;
                if (OnLevelWon != null) OnLevelWon();
                return;
            }
            if (evaluateFail) CheckFail();
        }

        /// <summary>Greedy solver on the board only (slots always eventually free because totals match). Used for validation.</summary>
        public static bool BoardSolvable(LevelData level) { return SolveOrder(level) != null; }

        /// <summary>A valid board removal order (plane ids), or null when the board jams. Greedy is complete here:
        /// removing a plane never blocks another, so any plane that can exit now can still exit later.</summary>
        public static int[] SolveOrder(LevelData level)
        {
            var board = new TrafficBoard(level.Cols, level.Rows);
            var left = new List<PlaneData>(level.Planes);
            foreach (var p in left) board.Place(p);
            var order = new List<int>(left.Count);
            bool progress = true;
            while (left.Count > 0 && progress)
            {
                progress = false;
                for (int i = left.Count - 1; i >= 0; i--)
                {
                    if (board.CanExit(left[i])) { board.Remove(left[i]); order.Add(left[i].Id); left.RemoveAt(i); progress = true; }
                }
            }
            return left.Count == 0 ? order.ToArray() : null;
        }

        /// <summary>True when <paramref name="order"/> removes every plane of the level legally, in sequence.</summary>
        public static bool IsValidOrder(LevelData level, int[] order)
        {
            if (order == null || order.Length != level.Planes.Count) return false;
            var board = new TrafficBoard(level.Cols, level.Rows);
            var byId = new Dictionary<int, PlaneData>();
            foreach (var p in level.Planes) { board.Place(p); byId[p.Id] = p; }
            foreach (int id in order)
            {
                PlaneData p;
                if (!byId.TryGetValue(id, out p) || !board.CanExit(p)) return false;
                board.Remove(p);
                byId.Remove(id);
            }
            return true;
        }
    }
}
