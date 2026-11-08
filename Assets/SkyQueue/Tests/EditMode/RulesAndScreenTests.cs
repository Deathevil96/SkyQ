using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace SkyQueue.Tests
{
    public class RulesAndScreenTests
    {
        [SetUp] public void ResetRules() { Rules.Configure(new RulesConfig()); }
        [TearDown] public void RestoreRules() { Rules.Configure(new RulesConfig()); }

        // ---- rules ----
        [Test]
        public void DefaultRules_MatchTheDesignDoc()
        {
            CollectionAssert.AreEqual(new[] { 16, 24, 40 }, Rules.Capacity);
            Assert.AreEqual(4, Rules.DefaultSlots);
            Assert.AreEqual(3, Rules.PurchasableSlots);
            Assert.IsTrue(Rules.VipSlot);
            Assert.AreEqual(8, Rules.TotalSlots, "VIP + 4 plain + 3 purchasable");
        }

        [Test]
        public void EveryCapacity_IsAWholeNumberOfRows()
        {
            foreach (int cap in Rules.Capacity) Assert.AreEqual(0, cap % Rules.RowSize);
        }

        [Test]
        public void InvalidRules_AreRejected_AndPreviousValuesKept()
        {
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("Invalid RulesConfig"));
            var bad = new RulesConfig { capacities = new[] { 16, 24, 41 }, rowSize = 4 };   // 41 is not a multiple of 4
            Assert.IsFalse(Rules.Configure(bad));
            CollectionAssert.AreEqual(new[] { 16, 24, 40 }, Rules.Capacity);
        }

        [Test]
        public void ConfiguredRules_ChangeSlotCountsAndCapacities()
        {
            Assert.IsTrue(Rules.Configure(new RulesConfig { capacities = new[] { 8, 16, 32 }, rowSize = 4, defaultSlots = 5, purchasableSlots = 2, vipSlot = false }));
            Assert.AreEqual(7, Rules.TotalSlots);
            Assert.AreEqual(0, Rules.FirstPlainSlot);
            var plane = TestSupport.Plane(0, 0, PlaneSize.Medium, 0, 0, Dir.Left);
            Assert.AreEqual(16, plane.Capacity);
        }

        // ---- screen fitting ----
        static Rect Fit(float w, float h, Rect safe) { return ScreenMap.FitFrame(w, h, safe, GameConfig.RefAspect); }

        [TestCase(1080f, 1920f, 0f, 0f, 0f, 0f)]
        [TestCase(1170f, 2532f, 0f, 102f, 0f, 136f)]     // notch + home indicator
        [TestCase(1080f, 2520f, 0f, 0f, 0f, 80f)]        // tall phone with a camera cut-out
        [TestCase(1536f, 2048f, 0f, 0f, 0f, 0f)]         // tablet portrait
        [TestCase(2048f, 1536f, 0f, 0f, 0f, 0f)]         // tablet landscape
        [TestCase(2532f, 1170f, 132f, 63f, 132f, 0f)]    // phone landscape with side insets
        [TestCase(1800f, 1800f, 0f, 0f, 0f, 0f)]         // foldable square
        [TestCase(321f, 531f, 0f, 0f, 0f, 0f)]           // small editor window
        public void Frame_AlwaysFits_KeepsAspect_AndIsCentredInsideTheSafeArea(float w, float h, float left, float bottom, float right, float top)
        {
            var safe = new Rect(left, bottom, w - left - right, h - bottom - top);
            Rect f = Fit(w, h, safe);
            Assert.AreEqual(GameConfig.RefAspect, f.width / f.height, 1e-3f, "aspect");
            Assert.GreaterOrEqual(f.xMin, safe.xMin - 0.5f); Assert.LessOrEqual(f.xMax, safe.xMax + 0.5f);
            Assert.GreaterOrEqual(f.yMin, safe.yMin - 0.5f); Assert.LessOrEqual(f.yMax, safe.yMax + 0.5f);
            Assert.AreEqual(w * 0.5f, f.center.x, 0.5f, "centred horizontally");
            Assert.AreEqual(h * 0.5f, f.center.y, 0.5f, "centred vertically");
        }

        [Test]
        public void Fov_IsUnchanged_WhenTheFrameFillsTheScreenHeight_AndWidensOnTallerScreens()
        {
            Assert.AreEqual(30f, ScreenMap.FovForFrame(30f, 1920f, 1920f), 1e-3f);
            Assert.Greater(ScreenMap.FovForFrame(30f, 2532f, 2260f), 30f, "extra scenery above/below the frame needs a wider view");
        }
    }
}
