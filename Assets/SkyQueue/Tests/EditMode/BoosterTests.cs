using NUnit.Framework;

namespace SkyQueue.Tests
{
    /// <summary>Booster rules on the model. Pure logic, no simulation runs.</summary>
    public class BoosterTests
    {
        [SetUp] public void ResetRules() { Rules.Configure(new RulesConfig()); }

        // plane 1 is blocked by plane 0 sitting right in front of it
        static GameModel Jammed()
        {
            var planes = new[]
            {
                TestSupport.Plane(0, 0, PlaneSize.Small, 0, 0, Dir.Up),
                TestSupport.Plane(1, 1, PlaneSize.Small, 0, 2, Dir.Up),
                TestSupport.Plane(2, 2, PlaneSize.Small, 2, 2, Dir.Up),
            };
            return TestSupport.Model(TestSupport.Level(4, 4, planes, new[] { 0, 16 }, new[] { 1, 16 }, new[] { 2, 16 }));
        }

        [Test]
        public void Vip_FliesABlockedPlane_ToTheVipStand_Once()
        {
            var m = Jammed();
            int slot;
            Assert.AreEqual(LaunchResult.Blocked, m.TryLaunch(1, out slot));
            Assert.IsTrue(m.CanUseVip);
            Assert.AreEqual(LaunchResult.Success, m.TryLaunchVip(1));
            Assert.AreEqual(0, m.GetPlane(1).Slot, "VIP stand is slot 0");
            Assert.IsFalse(m.CanUseVip, "the VIP stand is now taken");
            Assert.AreEqual(LaunchResult.NoSlot, m.TryLaunchVip(2));
        }

        [Test]
        public void NormalLaunches_NeverUseTheVipStand()
        {
            var m = Jammed();
            int slot;
            Assert.AreEqual(LaunchResult.Success, m.TryLaunch(0, out slot));
            Assert.AreNotEqual(0, slot);
        }

        [Test]
        public void ExtraSlot_OpensEachPurchasableStandOnce()
        {
            var m = Jammed();
            int s = m.NextLockedPurchasableSlot();
            Assert.GreaterOrEqual(s, 0);
            Assert.IsTrue(m.TryUnlockSlot(s));
            Assert.IsFalse(m.TryUnlockSlot(s), "already open");
            Assert.IsFalse(m.TryUnlockSlot(1), "plain stands are not for sale");
        }

        [Test]
        public void Jumble_KeepsEverySizesColourCounts()
        {
            var lvl = LevelLibrary.Get(25);
            var m = TestSupport.Model(lvl);
            var before = new int[Palette.Count, 3];
            foreach (var p in m.Planes) before[p.Color, (int)p.Size]++;
            Assert.IsTrue(m.Jumble(new System.Random(3)));
            var after = new int[Palette.Count, 3];
            foreach (var p in m.Planes) after[p.Color, (int)p.Size]++;
            CollectionAssert.AreEqual(before, after, "colours only move between planes of the same size");
            Assert.IsTrue(LevelValidator.IsValid(lvl), "passenger totals still match plane capacity");
        }

        [Test]
        public void Arrange_PutsWantedColourFirst_AndKeepsTotals()
        {
            var m = Jammed();
            int slot;
            m.TryLaunch(2, out slot);           // colour-2 plane goes to a stand
            m.NotifyArrived(m.GetPlane(2));
            int[] totals = { m.Ring.CountOfColor(0), m.Ring.CountOfColor(1), m.Ring.CountOfColor(2) };
            Assert.IsTrue(m.CanArrange);
            m.Arrange();
            Assert.AreEqual(totals[0], m.Ring.CountOfColor(0));
            Assert.AreEqual(totals[2], m.Ring.CountOfColor(2));
            var front = m.Ring.Feed[0].Count > 0 ? m.Ring.Feed[0][0] : m.Ring.Ring[0];
            Assert.AreEqual(2, front.Color, "the stand's colour now comes first");
        }

        [Test]
        public void BoosterInventory_IsSavedAndNeverNegative()
        {
            var p = new Progress(null, new EconomyConfig());
            Assert.AreEqual(0, p.BoosterCount(BoosterKind.Vip), "nothing before the unlock");
            Assert.IsFalse(p.TryIntroduceBooster(BoosterKind.Vip, 1), "VIP is still locked at level 1");
            Assert.IsTrue(p.TryIntroduceBooster(BoosterKind.Vip, 4), "unlocks at level 4 with a gift");
            Assert.IsFalse(p.TryIntroduceBooster(BoosterKind.Vip, 5), "introduced only once");
            Assert.AreEqual(1, p.BoosterCount(BoosterKind.Vip));
            Assert.IsTrue(p.ConsumeBooster(BoosterKind.Vip));
            Assert.IsFalse(p.ConsumeBooster(BoosterKind.Vip));
            var loaded = SaveCodec.Decode(SaveCodec.Encode(p.Data));
            Assert.AreEqual(0, loaded.boosterInventory[(int)BoosterKind.Vip]);
            Assert.AreEqual(BoosterConfig.KindCount, SaveCodec.Decode("{\"version\":1}").boosterInventory.Length, "old saves get an inventory");
        }
    }
}
