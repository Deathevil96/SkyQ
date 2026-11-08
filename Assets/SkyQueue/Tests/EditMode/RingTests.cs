using System.Collections.Generic;
using NUnit.Framework;

namespace SkyQueue.Tests
{
    public class RingTests
    {
        [SetUp] public void ResetRules() { Rules.Configure(new RulesConfig()); }

        static PassengerRing NewRing(params int[][] bands)
        {
            var s = RingSettings.Default();
            return new PassengerRing(bands, Rules.RowSize, s.Spacing, s.Length, s.BoardS, s.MergeS, s.FeedLen, s.FeedHeadStart, s.Speed);
        }

        static void Run(PassengerRing r, float seconds, System.Func<int, float, bool> board = null)
        {
            if (board == null) board = (c, s) => false;
            for (float t = 0; t < seconds; t += TestSupport.Dt) r.Tick(TestSupport.Dt, board);
        }

        [Test]
        public void EveryRowStartsInAFeed_AndBandsAlternateBetweenTheTwoFeeds()
        {
            var r = NewRing(new[] { 0, 8 }, new[] { 1, 8 }, new[] { 2, 8 });
            Assert.AreEqual(4, r.Feed[0].Count, "bands 0 and 2 (2 rows each) queue in feed 0");
            Assert.AreEqual(2, r.Feed[1].Count, "band 1 (2 rows) queues in feed 1");
        }

        [Test]
        public void Rows_AreSolidColour_AndTotalsAreRowMultiples()
        {
            var r = NewRing(new[] { 3, 12 }, new[] { 4, 8 });
            Assert.AreEqual(20, r.Remaining);
            Assert.AreEqual(12, r.CountOfColor(3));
            Assert.AreEqual(8, r.CountOfColor(4));
            foreach (var feed in r.Feed) foreach (var row in feed) Assert.IsTrue(row.Color == 3 || row.Color == 4);
        }

        [Test]
        public void FeedRows_MergeIntoTheRing_AndKeepCirculating()
        {
            var r = NewRing(new[] { 0, 16 });
            Run(r, 8f);
            Assert.AreEqual(4, r.RingRows, "all four rows have merged");
            Assert.AreEqual(16, r.Remaining, "circulating rows are still waiting to board");
            Run(r, 30f);
            Assert.AreEqual(4, r.RingRows, "rows that nobody boards keep circling forever");
        }

        [Test]
        public void ABoardingRow_LeavesTheRingAsAWholeRow()
        {
            var r = NewRing(new[] { 2, 16 });
            int boardedRows = 0;
            Run(r, 20f, (c, s) => { if (boardedRows == 0) { boardedRows++; return true; } return false; });
            Assert.AreEqual(1, boardedRows);
            Assert.AreEqual(16 - Rules.RowSize, r.Remaining);
            Assert.AreEqual(3, r.RingRows);
        }

        [Test]
        public void RowsOnTheRing_KeepTheirSpacing_AndNeverOverlap()
        {
            var r = NewRing(new[] { 0, 40 }, new[] { 1, 40 });
            for (float t = 0; t < 40f; t += TestSupport.Dt)
            {
                r.Tick(TestSupport.Dt, (c, s) => false);
                var pos = new List<float>();
                foreach (var row in r.Ring) pos.Add(row.S);
                pos.Sort();
                for (int i = 1; i < pos.Count; i++)
                    Assert.GreaterOrEqual(pos[i] - pos[i - 1], r.Spacing * 0.97f, "rows on the ring must stay a full spacing apart");
            }
        }

        [Test]
        public void ARingThatIsFull_StallsTheFeeds()
        {
            var r = NewRing(new[] { 0, 4 * 80 });
            Run(r, 120f);
            Assert.IsFalse(r.HasRoom);
            int before = r.RingRows;
            Run(r, 20f);
            Assert.AreEqual(before, r.RingRows, "no row may merge into a full ring");
            Assert.Greater(r.Feed[0].Count, 0, "the rest of the crowd waits in the feed");
        }

        [Test]
        public void HasRoom_RequiresAGapWideEnoughToMerge_NotJustAFreeCount()
        {
            var r = NewRing(new[] { 0, 4 * 80 });
            Run(r, 120f);
            // Free capacity can exist as slack split into gaps that are each too small to accept a row.
            Assert.IsFalse(r.HasRoom);
        }
    }
}
