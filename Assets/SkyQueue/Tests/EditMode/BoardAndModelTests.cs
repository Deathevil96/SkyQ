using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace SkyQueue.Tests
{
    public class BoardAndModelTests
    {
        [SetUp] public void ResetRules() { Rules.Configure(new RulesConfig()); }

        // ---- grid / movement ----
        [Test]
        public void Plane_CanExit_WhenFullPathIsEmpty()
        {
            var board = new TrafficBoard(6, 6);
            var p = TestSupport.Plane(0, 0, PlaneSize.Small, 1, 2, Dir.Right);
            board.Place(p);
            Assert.IsTrue(board.CanExit(p));
        }

        [Test]
        public void Plane_CannotExit_WhenAnyCellInPathIsOccupied()
        {
            var board = new TrafficBoard(6, 6);
            var p = TestSupport.Plane(0, 0, PlaneSize.Small, 0, 2, Dir.Right);
            var blocker = TestSupport.Plane(1, 1, PlaneSize.Small, 4, 2, Dir.Up);   // vertical, sits in p's row
            board.Place(p); board.Place(blocker);
            Assert.IsFalse(board.CanExit(p));
        }

        [Test]
        public void Plane_OnlyChecksItsOwnArrowDirection()
        {
            var board = new TrafficBoard(6, 6);
            var p = TestSupport.Plane(0, 0, PlaneSize.Small, 2, 2, Dir.Right);
            var behind = TestSupport.Plane(1, 1, PlaneSize.Small, 0, 2, Dir.Left);   // in the opposite direction
            board.Place(p); board.Place(behind);
            Assert.IsTrue(board.CanExit(p), "blockers behind the plane must not matter");
        }

        [Test]
        public void CellsToEdge_CountsCellsBetweenNoseAndBoardEdge()
        {
            var board = new TrafficBoard(8, 4);
            var p = TestSupport.Plane(0, 0, PlaneSize.Medium, 1, 1, Dir.Right);   // occupies cols 1..3
            board.Place(p);
            Assert.AreEqual(4, board.CellsToEdge(p));
        }

        // ---- stands ----
        [Test]
        public void LaunchingAPlane_UsesTheFirstFreeStand_AndRejectsWhenFull()
        {
            var planes = new PlaneData[Rules.DefaultSlots + 1];
            for (int i = 0; i < planes.Length; i++) planes[i] = TestSupport.Plane(i, 0, PlaneSize.Small, 0, i, Dir.Left);   // each exits left, all clear
            var m = TestSupport.Model(TestSupport.Level(4, planes.Length, planes, new[] { 0, 16 * planes.Length }));

            int slot;
            for (int i = 0; i < Rules.DefaultSlots; i++)
            {
                Assert.AreEqual(LaunchResult.Success, m.TryLaunch(planes[i].Id, out slot));
                Assert.IsTrue(m.Slots[slot].Kind == SlotKind.Normal && m.Slots[slot].Unlocked);
            }
            Assert.AreEqual(LaunchResult.NoSlot, m.TryLaunch(planes[Rules.DefaultSlots].Id, out slot), "no stand left");
            Assert.AreEqual(PlaneState.OnBoard, planes[Rules.DefaultSlots].State, "a rejected launch must not change the plane");
        }

        [Test]
        public void VipAndPurchasableStands_AreLockedByDefault()
        {
            var m = TestSupport.Model(TestSupport.Level(4, 1, new[] { TestSupport.Plane(0, 0, PlaneSize.Small, 0, 0, Dir.Left) }, new[] { 0, 16 }));
            int open = 0;
            foreach (var s in m.Slots) if (s.Unlocked) open++;
            Assert.AreEqual(Rules.DefaultSlots, open);
            Assert.AreEqual(Rules.TotalSlots, m.Slots.Length);
        }

        [Test]
        public void Launch_ReturnsBlocked_AndDoesNotAlterTheBoard()
        {
            var a = TestSupport.Plane(0, 0, PlaneSize.Small, 0, 0, Dir.Right);
            var b = TestSupport.Plane(1, 1, PlaneSize.Small, 3, 0, Dir.Down);
            var m = TestSupport.Model(TestSupport.Level(5, 3, new[] { a, b }, new[] { 0, 16 }, new[] { 1, 16 }));
            int slot;
            Assert.AreEqual(LaunchResult.Blocked, m.TryLaunch(a.Id, out slot));
            Assert.AreEqual(PlaneState.OnBoard, a.State);
            Assert.AreEqual(a.Id, m.Board.At(0, 0));
        }

        // ---- boarding / departure ----
        static GameModel OnePlaneModel(out PlaneData plane, int color = 0, PlaneSize size = PlaneSize.Small, params int[][] bands)
        {
            plane = TestSupport.Plane(0, color, size, 0, 0, Dir.Left);
            return TestSupport.Model(TestSupport.Level(4, 1, new[] { plane }, bands));
        }

        [Test]
        public void Passengers_CannotBoard_BeforeThePlaneHasArrived()
        {
            PlaneData p;
            var m = OnePlaneModel(out p, 0, PlaneSize.Small, new[] { 0, 16 });
            int slot; m.TryLaunch(p.Id, out slot);
            for (int i = 0; i < 600; i++) m.Tick(TestSupport.Dt);   // plenty of time for rows to pass the gap
            Assert.AreEqual(0, p.Boarded, "plane is still MovingOut");
        }

        [Test]
        public void MatchingRows_Board_AndPlaneDepartsExactlyAtCapacity()
        {
            PlaneData p;
            var m = OnePlaneModel(out p, 2, PlaneSize.Medium, new[] { 2, 24 });
            int slot; m.TryLaunch(p.Id, out slot); m.NotifyArrived(p);
            int rowsBoarded = 0;
            m.OnRowBoarded += (pl, c, s) => rowsBoarded++;
            for (int i = 0; i < 3000 && p.State != PlaneState.Full; i++) m.Tick(TestSupport.Dt);
            Assert.AreEqual(PlaneState.Full, p.State);
            Assert.AreEqual(24, p.Boarded, "exactly capacity, no overflow");
            Assert.AreEqual(24 / Rules.RowSize, rowsBoarded, "boarding happens one whole row at a time");
        }

        [Test]
        public void RowsOfAnotherColour_NeverBoard()
        {
            PlaneData p;
            var m = OnePlaneModel(out p, 0, PlaneSize.Small, new[] { 5, 16 });
            int slot; m.TryLaunch(p.Id, out slot); m.NotifyArrived(p);
            for (int i = 0; i < 1500; i++) m.Tick(TestSupport.Dt);
            Assert.AreEqual(0, p.Boarded);
            Assert.AreEqual(16, m.Ring.CountOfColor(5), "passengers must not disappear or change colour");
        }

        [Test]
        public void Departure_FreesTheStand_AndWinNeedsEveryPassengerAndPlane()
        {
            PlaneData p;
            var m = OnePlaneModel(out p, 1, PlaneSize.Small, new[] { 1, 16 });
            int slot; m.TryLaunch(p.Id, out slot); m.NotifyArrived(p);
            for (int i = 0; i < 3000 && p.State != PlaneState.Full; i++) m.Tick(TestSupport.Dt);
            Assert.IsFalse(m.Won, "a full plane that has not departed yet is not a win");
            m.BeginDeparture(p);
            Assert.IsTrue(m.Slots[slot].Free, "departure frees the stand");
            Assert.IsFalse(m.Won);
            m.NotifyDeparted(p);
            Assert.IsTrue(m.Won);
        }

        [Test]
        public void PlaneStateMachine_RejectsInvalidTransitions()
        {
            var p = TestSupport.Plane(0, 0, PlaneSize.Small, 0, 0, Dir.Left);
            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("Rejected plane"));
            Assert.IsFalse(p.TrySetState(PlaneState.Full), "OnBoard -> Full is not allowed");
            Assert.AreEqual(PlaneState.OnBoard, p.State);
            Assert.IsTrue(p.TrySetState(PlaneState.MovingOut));
        }

        // ---- fail conditions ----
        [Test]
        public void Level_Fails_WhenTheRingIsFullOfRowsNoParkedPlaneCanUse()
        {
            PlaneData p;
            // A colour-0 plane is parked but only colour-1 passengers exist, far more than the ring holds.
            var m = OnePlaneModel(out p, 0, PlaneSize.Small, new[] { 1, 4 * 60 });
            int slot; m.TryLaunch(p.Id, out slot); m.NotifyArrived(p);
            for (int i = 0; i < 6000 && !m.Failed; i++) m.Tick(TestSupport.Dt);
            Assert.IsTrue(m.Failed);
            Assert.IsFalse(m.Ring.HasRoom);
        }

        [Test]
        public void ABlockedTap_IsNotAFailure_WhileAnotherPlaneCanStillMove()
        {
            var a = TestSupport.Plane(0, 0, PlaneSize.Small, 0, 0, Dir.Right);   // blocked by b
            var b = TestSupport.Plane(1, 0, PlaneSize.Small, 3, 0, Dir.Right);   // clear: can still move
            var m = TestSupport.Model(TestSupport.Level(6, 1, new[] { a, b }, new[] { 0, 32 }));
            int slot;
            Assert.AreEqual(LaunchResult.Blocked, m.TryLaunch(a.Id, out slot));
            m.Tick(TestSupport.Dt);
            Assert.IsFalse(m.Failed);
        }

        [Test]
        public void Level_Fails_WhenTheBoardIsJammedAndNothingCanMove()
        {
            var a = TestSupport.Plane(0, 0, PlaneSize.Small, 0, 0, Dir.Right);   // a and b face each other
            var b = TestSupport.Plane(1, 0, PlaneSize.Small, 3, 0, Dir.Left);
            var m = TestSupport.Model(TestSupport.Level(6, 1, new[] { a, b }, new[] { 0, 32 }));
            for (int i = 0; i < 20 && !m.Failed; i++) m.Tick(0.3f);
            Assert.IsTrue(m.Failed);
        }
    }
}
