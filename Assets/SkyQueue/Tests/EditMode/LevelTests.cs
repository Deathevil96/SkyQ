using System.Collections.Generic;
using NUnit.Framework;

namespace SkyQueue.Tests
{
    public class LevelTests
    {
        [SetUp] public void ResetRules() { Rules.Configure(new RulesConfig()); }

        static IEnumerable<int> LevelIds()
        {
            for (int i = 1; i <= LevelLibrary.Count; i++) yield return i;
        }

        [Test]
        public void LevelTable_LoadsFromData_WithTenLevels()
        {
            Assert.GreaterOrEqual(LevelLibrary.Count, 10);
        }

        [TestCaseSource(nameof(LevelIds))]
        public void Level_PassengerTotalsMatchPlaneCapacities_PerColour(int id)
        {
            var lvl = LevelLibrary.Get(id);
            var capacity = new int[Palette.Count];
            var passengers = new int[Palette.Count];
            foreach (var p in lvl.Planes) capacity[p.Color] += p.Capacity;
            foreach (var b in lvl.PassengerBands) passengers[b[0]] += b[1];
            CollectionAssert.AreEqual(capacity, passengers);
        }

        [TestCaseSource(nameof(LevelIds))]
        public void Level_BandsAreWholeRows(int id)
        {
            foreach (var b in LevelLibrary.Get(id).PassengerBands)
                Assert.AreEqual(0, b[1] % Rules.RowSize, "a band must be a whole number of rows");
        }

        [TestCaseSource(nameof(LevelIds))]
        public void Level_BoardIsSolvable_AndPlanesDoNotOverlap(int id)
        {
            var lvl = LevelLibrary.Get(id);
            Assert.IsTrue(GameModel.BoardSolvable(lvl));
            var seen = new HashSet<Vector2IntKey>();
            foreach (var p in lvl.Planes)
                foreach (var c in p.Cells())
                    Assert.IsTrue(seen.Add(new Vector2IntKey(c.x, c.y)), "cell used twice");
        }

        [TestCaseSource(nameof(LevelIds))]
        public void Level_StoredSolution_ClearsTheBoardInOrder(int id)
        {
            var lvl = LevelLibrary.Get(id);
            Assert.IsTrue(GameModel.IsValidOrder(lvl, lvl.Solution), "Solution must be a legal removal order");
        }

        [Test]
        public void Validator_RejectsAnIllegalSolutionOrder()
        {
            var lvl = LevelLibrary.Get(5);
            var reversed = (int[])lvl.Solution.Clone();
            System.Array.Reverse(reversed);
            Assert.IsFalse(GameModel.IsValidOrder(lvl, reversed));
            lvl.Solution = reversed;
            Assert.IsFalse(LevelValidator.IsValid(lvl));
        }

        [TestCaseSource(nameof(LevelIds))]
        public void Level_PassesTheFastValidator(int id)
        {
            var errors = LevelValidator.Validate(LevelLibrary.Get(id));
            Assert.IsEmpty(errors, string.Join("; ", errors));
        }

        [Test]
        public void Validator_RejectsBrokenLevels()
        {
            var good = LevelLibrary.Get(1);
            Assert.IsTrue(LevelValidator.IsValid(good));

            var wrongTotals = LevelLibrary.Get(1);
            wrongTotals.PassengerBands[0][1] += Rules.RowSize;
            Assert.IsFalse(LevelValidator.IsValid(wrongTotals), "passenger totals must match plane capacity");

            var offBoard = LevelLibrary.Get(1);
            offBoard.Planes[0].Col = offBoard.Cols + 3;
            Assert.IsFalse(LevelValidator.IsValid(offBoard), "a plane outside the board is invalid");

            Assert.IsFalse(LevelValidator.IsValid(null));
        }

        [Test]
        public void Generation_IsDeterministic_ForTheSameSeed()
        {
            var a = LevelLibrary.Get(5);
            var b = LevelLibrary.Get(5);
            Assert.AreEqual(a.Planes.Count, b.Planes.Count);
            for (int i = 0; i < a.Planes.Count; i++)
            {
                Assert.AreEqual(a.Planes[i].Col, b.Planes[i].Col);
                Assert.AreEqual(a.Planes[i].Row, b.Planes[i].Row);
                Assert.AreEqual(a.Planes[i].Color, b.Planes[i].Color);
                Assert.AreEqual(a.Planes[i].Dir, b.Planes[i].Dir);
            }
        }

        [Test]
        public void Difficulty_FollowsTheEveryFiveEveryTenCadence()
        {
            Assert.AreEqual(Difficulty.Normal, LevelLibrary.DifficultyFor(1));
            Assert.AreEqual(Difficulty.Hard, LevelLibrary.DifficultyFor(5));
            Assert.AreEqual(Difficulty.Normal, LevelLibrary.DifficultyFor(6));
            Assert.AreEqual(Difficulty.SuperHard, LevelLibrary.DifficultyFor(10));
            Assert.AreEqual(Difficulty.Hard, LevelLibrary.DifficultyFor(15));
            Assert.AreEqual(Difficulty.SuperHard, LevelLibrary.DifficultyFor(20));
        }

        // tiny hashable key so the test needs no extra usings
        readonly struct Vector2IntKey
        {
            readonly int x, y;
            public Vector2IntKey(int x, int y) { this.x = x; this.y = y; }
        }
    }
}
