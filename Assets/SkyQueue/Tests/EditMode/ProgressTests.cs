using NUnit.Framework;

namespace SkyQueue.Tests
{
    /// <summary>Economy (GDD 7.2) and save-data rules. Pure logic: runs in milliseconds.</summary>
    public class ProgressTests
    {
        static Progress Fresh() { return new Progress(null, new EconomyConfig()); }

        [Test]
        public void FirstWin_Pays20_ReplayPaysNothing()
        {
            var p = Fresh();
            Assert.AreEqual(20, p.RecordWin(3, Difficulty.Normal).Total);
            Assert.AreEqual(0, p.RecordWin(3, Difficulty.Normal).Total, "replaying a completed level must not pay again");
            Assert.AreEqual(20, p.Coins);
        }

        [Test]
        public void EveryTenthLevel_PaysTheMilestoneOnce_UpToLevel100()
        {
            var p = Fresh();
            Assert.AreEqual(70, p.RecordWin(10, Difficulty.SuperHard).Total, "20 base + 50 milestone");
            Assert.AreEqual(0, p.RecordWin(10, Difficulty.SuperHard).Total, "milestone pays once");
            Assert.AreEqual(70, p.RecordWin(100, Difficulty.SuperHard).Total);
            Assert.AreEqual(20, p.RecordWin(110, Difficulty.SuperHard).Total, "no milestone past level 100 by default");
        }

        [Test]
        public void SuperHardBonus_IsOffByDefault_AndConfigurable()
        {
            Assert.AreEqual(0, Fresh().RecordWin(20, Difficulty.SuperHard).SuperHard);
            var on = new Progress(null, new EconomyConfig { superHardBonusEnabled = true, superHardBonus = 10 });
            Assert.AreEqual(10, on.RecordWin(20, Difficulty.SuperHard).SuperHard);
        }

        [Test]
        public void Spending_NeverGoesNegative()
        {
            var p = Fresh();
            p.RecordWin(1, Difficulty.Normal);
            Assert.IsFalse(p.TrySpend(21));
            Assert.IsTrue(p.TrySpend(20));
            Assert.AreEqual(0, p.Coins);
        }

        [Test]
        public void SaveRoundTrip_KeepsCoinsLevelsAndClaims()
        {
            var p = Fresh();
            p.RecordWin(10, Difficulty.SuperHard);
            p.SetCurrentLevel(11);
            var loaded = new Progress(SaveCodec.Decode(SaveCodec.Encode(p.Data)), new EconomyConfig());
            Assert.AreEqual(70, loaded.Coins);
            Assert.AreEqual(11, loaded.CurrentLevel);
            Assert.AreEqual(0, loaded.RecordWin(10, Difficulty.SuperHard).Total, "claims survive a reload");
        }

        [Test]
        public void CorruptOrHostileSaves_AreRejectedOrRepaired()
        {
            Assert.IsNull(SaveCodec.Decode("not json"));
            Assert.IsNull(SaveCodec.Decode(""));
            Assert.IsNull(SaveCodec.Decode("{\"version\":999,\"coins\":5}"), "a save from a newer version is not trusted");
            var repaired = SaveCodec.Decode("{\"version\":1,\"coins\":-50,\"currentLevel\":0,\"completed\":[3,3,-1]}");
            Assert.AreEqual(0, repaired.coins);
            Assert.AreEqual(1, repaired.currentLevel);
            CollectionAssert.AreEqual(new[] { 3 }, repaired.completed);
        }

        [Test]
        public void LevelRange_Reaches1000_AndProceduralLevelsAreDeterministic()
        {
            Assert.AreEqual(1000, LevelLibrary.MaxLevel);
            var a = LevelLibrary.Get(437); var b = LevelLibrary.Get(437);
            Assert.AreEqual(a.Planes.Count, b.Planes.Count);
            for (int i = 0; i < a.Planes.Count; i++) Assert.AreEqual(a.Planes[i].Col, b.Planes[i].Col);
            Assert.IsTrue(LevelValidator.IsValid(a));
        }
    }
}
