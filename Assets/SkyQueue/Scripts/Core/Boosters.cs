using System;
using UnityEngine;

namespace SkyQueue
{
    /// <summary>
    /// Boosters, matching the reference's booster bar plus its purchasable stands:
    /// VIP (fly any plane straight to the VIP stand), Arrange (bring rows that match the parked planes to the front of the
    /// queue), Jumble (shuffle plane colours among planes of the same size) and Extra Slot (open a "+" stand for the level).
    /// </summary>
    public enum BoosterKind { Vip = 0, Arrange = 1, Jumble = 2, ExtraSlot = 3 }

    /// <summary>Booster prices and starting inventory (GDD 7.3). All data-driven.</summary>
    [Serializable]
    public sealed class BoosterConfig
    {
        public const int KindCount = 4;

        [Tooltip("Coin price per use, indexed by BoosterKind: VIP, Arrange, Jumble, Extra Slot.")]
        public int[] cost = { 1200, 900, 900, 600 };
        [Tooltip("Free uses a new player starts with, indexed by BoosterKind.")]
        public int[] startingInventory = { 0, 0, 0, 0 };
        [Tooltip("Level at which each booster unlocks (intro popup + gift the first time), indexed by BoosterKind.")]
        public int[] unlockLevel = { 4, 6, 8, 2 };
        [Tooltip("Free uses granted when a booster is first introduced, indexed by BoosterKind.")]
        public int[] introGift = { 1, 1, 1, 0 };

        public int CostOf(BoosterKind k) { int i = (int)k; return cost != null && i < cost.Length ? Mathf.Max(0, cost[i]) : 0; }
        public int UnlockLevelOf(BoosterKind k) { int i = (int)k; return unlockLevel != null && i < unlockLevel.Length ? Mathf.Max(1, unlockLevel[i]) : 1; }
        public int GiftOf(BoosterKind k) { int i = (int)k; return introGift != null && i < introGift.Length ? Mathf.Max(0, introGift[i]) : 0; }
        public bool IsUnlocked(BoosterKind k, int level) { return level >= UnlockLevelOf(k); }

        public int StartOf(BoosterKind k) { int i = (int)k; return startingInventory != null && i < startingInventory.Length ? Mathf.Max(0, startingInventory[i]) : 0; }
    }
}
