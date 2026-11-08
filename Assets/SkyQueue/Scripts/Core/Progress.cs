using System;
using System.Collections.Generic;
using UnityEngine;

namespace SkyQueue
{
    /// <summary>All coin rewards (GDD 7.2). Data-driven; serialized on GameConfig.</summary>
    [Serializable]
    public sealed class EconomyConfig
    {
        [Tooltip("Coins for the FIRST win of a level (replays pay nothing).")]
        public int winCoins = 20;
        [Tooltip("Every N-th level completed pays the milestone bonus.")]
        public int milestoneEvery = 10;
        public int milestoneBonus = 50;
        [Tooltip("Milestone bonus applies up to and including this level (0 = no limit).")]
        public int milestoneLastLevel = 100;
        [Tooltip("Optional extra for Super Hard wins; off for initial balance.")]
        public bool superHardBonusEnabled;
        public int superHardBonus = 10;
        public int startingCoins;
        public BoosterConfig boosters = new BoosterConfig();
    }

    /// <summary>Persistent player progress. Plain data so it serializes with JsonUtility; versioned for migrations.</summary>
    [Serializable]
    public sealed class SaveData
    {
        public const int CurrentVersion = 1;

        public int version = CurrentVersion;
        public int coins;
        public int currentLevel = 1;                       // level to open on launch
        public int highestCompleted;                       // furthest level ever won
        public List<int> completed = new List<int>();      // levels won at least once (first-win claims)
        public List<int> milestonesClaimed = new List<int>();
        public int[] boosterInventory = new int[BoosterConfig.KindCount];   // free uses owned, indexed by BoosterKind
        public int[] boosterIntroduced = new int[BoosterConfig.KindCount];  // 1 once the unlock popup was shown
        public Settings settings = new Settings();

        [Serializable]
        public sealed class Settings
        {
            public bool sound = true, music = true, haptics = true, reduceMotion;
        }
    }

    /// <summary>What a win paid out (for the result screen).</summary>
    public struct WinReward
    {
        public int Base, Milestone, SuperHard;
        public bool FirstWin;
        public int Total { get { return Base + Milestone + SuperHard; } }
    }

    /// <summary>
    /// Progress and economy rules on top of <see cref="SaveData"/>: first-win-only level rewards, one-time milestone
    /// bonuses, a coin balance that can never go negative, and the level to resume. Pure logic, no I/O.
    /// </summary>
    public sealed class Progress
    {
        readonly EconomyConfig economy;
        readonly HashSet<int> completed = new HashSet<int>(), milestones = new HashSet<int>();

        public SaveData Data { get; private set; }
        public int Coins { get { return Data.coins; } }
        public int CurrentLevel { get { return Data.currentLevel; } }

        public event Action<int> CoinsChanged;

        public Progress(SaveData data, EconomyConfig economy)
        {
            this.economy = economy ?? new EconomyConfig();
            Data = data ?? New(this.economy);
            completed.UnionWith(Data.completed);
            milestones.UnionWith(Data.milestonesClaimed);
        }

        public static SaveData New(EconomyConfig economy)
        {
            var d = new SaveData { coins = Mathf.Max(0, economy != null ? economy.startingCoins : 0) };
            if (economy != null && economy.boosters != null)
                for (int i = 0; i < BoosterConfig.KindCount; i++) d.boosterInventory[i] = economy.boosters.StartOf((BoosterKind)i);
            return d;
        }

        public event Action BoostersChanged;

        public int BoosterCount(BoosterKind k) { return Data.boosterInventory[(int)k]; }

        /// <summary>Uses one owned booster. Call only once the booster has actually executed.</summary>
        public bool ConsumeBooster(BoosterKind k)
        {
            if (Data.boosterInventory[(int)k] <= 0) return false;
            Data.boosterInventory[(int)k]--;
            if (BoostersChanged != null) BoostersChanged();
            return true;
        }

        /// <summary>
        /// First time a booster is available: marks it introduced and grants its gift. Returns false if it was already
        /// introduced or is not unlocked yet at <paramref name="level"/>.
        /// </summary>
        public bool TryIntroduceBooster(BoosterKind k, int level)
        {
            if (!economy.boosters.IsUnlocked(k, level) || Data.boosterIntroduced[(int)k] != 0) return false;
            Data.boosterIntroduced[(int)k] = 1;
            int gift = economy.boosters.GiftOf(k);
            if (gift > 0) GrantBooster(k, gift);
            return true;
        }

        public void GrantBooster(BoosterKind k, int amount)
        {
            Data.boosterInventory[(int)k] = Mathf.Max(0, Data.boosterInventory[(int)k] + amount);
            if (BoostersChanged != null) BoostersChanged();
        }

        public bool IsCompleted(int level) { return completed.Contains(level); }

        /// <summary>Records a win and pays what is due. Replays pay nothing; milestones pay once.</summary>
        public WinReward RecordWin(int level, Difficulty difficulty)
        {
            var r = new WinReward();
            if (completed.Add(level))
            {
                r.FirstWin = true;
                r.Base = economy.winCoins;
                if (economy.superHardBonusEnabled && difficulty == Difficulty.SuperHard) r.SuperHard = economy.superHardBonus;
                Data.completed.Add(level);
            }
            bool milestoneLevel = economy.milestoneEvery > 0 && level % economy.milestoneEvery == 0
                                  && (economy.milestoneLastLevel <= 0 || level <= economy.milestoneLastLevel);
            if (milestoneLevel && milestones.Add(level))
            {
                r.Milestone = economy.milestoneBonus;
                Data.milestonesClaimed.Add(level);
            }
            Data.highestCompleted = Mathf.Max(Data.highestCompleted, level);
            AddCoins(r.Total);
            return r;
        }

        public void SetCurrentLevel(int level) { Data.currentLevel = Mathf.Max(1, level); }

        /// <summary>Spends coins only if the balance covers it (boosters are never charged on failure).</summary>
        public bool TrySpend(int amount)
        {
            if (amount < 0 || Data.coins < amount) return false;
            AddCoins(-amount);
            return true;
        }

        /// <summary>Adds coins from outside the win flow (rewards, debug menu). Negative values are ignored.</summary>
        public void GrantCoins(int amount) { if (amount > 0) AddCoins(amount); }

        /// <summary>Debug: marks every booster introduced (as if its unlock level was reached) without granting gifts.</summary>
        public void IntroduceAllBoosters()
        {
            for (int i = 0; i < Data.boosterIntroduced.Length; i++) Data.boosterIntroduced[i] = 1;
            if (BoostersChanged != null) BoostersChanged();
        }

        void AddCoins(int delta)
        {
            if (delta == 0) return;
            Data.coins = Mathf.Max(0, Data.coins + delta);
            if (CoinsChanged != null) CoinsChanged(Data.coins);
        }
    }

    /// <summary>JSON encode/decode with validation and schema migration. A corrupt or foreign file yields null.</summary>
    public static class SaveCodec
    {
        public static string Encode(SaveData d) { return JsonUtility.ToJson(d); }

        public static SaveData Decode(string json)
        {
            if (string.IsNullOrEmpty(json)) return null;
            SaveData d;
            try { d = JsonUtility.FromJson<SaveData>(json); }
            catch (Exception) { return null; }
            return Validate(d);
        }

        /// <summary>Repairs or rejects loaded data: never negative coins, valid levels, no duplicates.</summary>
        public static SaveData Validate(SaveData d)
        {
            if (d == null || d.version <= 0 || d.version > SaveData.CurrentVersion) return null;
            // (future) migrate older versions here, step by step, then set d.version = CurrentVersion
            d.coins = Mathf.Max(0, d.coins);
            d.currentLevel = Mathf.Max(1, d.currentLevel);
            d.highestCompleted = Mathf.Max(0, d.highestCompleted);
            d.completed = Clean(d.completed);
            d.milestonesClaimed = Clean(d.milestonesClaimed);
            if (d.settings == null) d.settings = new SaveData.Settings();
            var inv = new int[BoosterConfig.KindCount];
            if (d.boosterInventory != null)
                for (int i = 0; i < inv.Length && i < d.boosterInventory.Length; i++) inv[i] = Mathf.Max(0, d.boosterInventory[i]);
            d.boosterInventory = inv;
            var intro = new int[BoosterConfig.KindCount];
            if (d.boosterIntroduced != null)
                for (int i = 0; i < intro.Length && i < d.boosterIntroduced.Length; i++) intro[i] = d.boosterIntroduced[i] != 0 ? 1 : 0;
            d.boosterIntroduced = intro;
            return d;
        }

        static List<int> Clean(List<int> list)
        {
            var res = new List<int>();
            if (list == null) return res;
            var seen = new HashSet<int>();
            foreach (int v in list) if (v > 0 && seen.Add(v)) res.Add(v);
            return res;
        }
    }
}
