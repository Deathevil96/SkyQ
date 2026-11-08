using System;
using UnityEngine;

namespace SkyQueue
{
    /// <summary>
    /// Booster interaction rules (GDD 7.3): an owned booster is used directly; with none owned, the player is offered a
    /// purchase and must confirm it (never auto-buy). Coins and inventory are only taken once the booster has actually
    /// executed. VIP is two-step: pick the booster, then tap any plane. Holds no game rules itself; it calls the model.
    /// </summary>
    public sealed class BoosterFlow
    {
        readonly Progress progress;
        readonly BoosterConfig config;
        readonly HudView hud;
        readonly System.Random rng = new System.Random();
        GameModel model;

        // a booster waiting for the player's confirmation (purchase) or target (VIP / stand)
        BoosterKind pendingKind;
        bool pendingPurchase, awaitingVipTarget, awaitingBuyAnswer, vipPaidWithCoins;
        int pendingSlot = -1;

        public bool AwaitingVipTarget { get { return awaitingVipTarget; } }

        /// <summary>Raised when a purchased "+" stand opens, so the view can restyle it.</summary>
        public event Action<int> SlotUnlocked;
        /// <summary>A booster actually executed: kind, paid with coins (false = used an owned one).</summary>
        public event Action<BoosterKind, bool> Used;

        public BoosterFlow(Progress progress, BoosterConfig config, HudView hud)
        {
            this.progress = progress; this.config = config; this.hud = hud;
            hud.BoosterPressed += OnBoosterPressed;
            hud.BuyConfirmed += OnBuyConfirmed;
            hud.BuyCancelled += Cancel;
            progress.BoostersChanged += RefreshCounts;
            RefreshCounts();
        }

        public void Bind(GameModel m) { model = m; Cancel(); RefreshCounts(); }

        int Level { get { return model != null ? model.Level.Id : 1; } }

        /// <summary>Debug menu: treat every booster as unlocked regardless of level (session only, never saved).</summary>
        public bool DebugUnlockAll;

        bool Unlocked(BoosterKind k) { return DebugUnlockAll || config.IsUnlocked(k, Level); }

        public void Refresh() { RefreshCounts(); }

        public void Cancel()
        {
            awaitingVipTarget = awaitingBuyAnswer = vipPaidWithCoins = false;
            pendingSlot = -1;
            hud.HideBuy();
            hud.SetBoosterHighlight(-1);
        }

        void RefreshCounts()
        {
            for (int i = 0; i < hud.BoosterButtonCount; i++)
            {
                var k = (BoosterKind)i;
                if (!Unlocked(k)) { hud.SetBoosterLocked(i, config.UnlockLevelOf(k)); continue; }
                hud.SetBoosterCount(i, progress.BoosterCount(k), config.CostOf(k));
            }
            hud.SetBoosterHighlight(awaitingVipTarget ? (int)BoosterKind.Vip : -1);
        }

        // entry points ------------------------------------------------------------------------------------------------
        void OnBoosterPressed(int index)
        {
            if (model == null) return;
            var kind = (BoosterKind)index;
            if (awaitingVipTarget && kind == BoosterKind.Vip) { Cancel(); return; }   // tap again to cancel
            Cancel();
            if (!Unlocked(kind)) { hud.Toast(hud.BoosterLockedMessage(index, config.UnlockLevelOf(kind))); return; }
            string why = Unavailable(kind);
            if (why != null) { hud.Toast(why); return; }
            Request(kind, -1);
        }

        /// <summary>The player tapped a closed "+" stand.</summary>
        public void OnLockedStandTapped(int slot)
        {
            if (model == null) return;
            Cancel();
            if (!Unlocked(BoosterKind.ExtraSlot)) { hud.Toast(string.Format("Extra stands unlock at level {0}", config.UnlockLevelOf(BoosterKind.ExtraSlot))); return; }
            Request(BoosterKind.ExtraSlot, slot);
        }

        /// <summary>Called for a board tap while VIP is armed. Returns true when the tap was consumed by the booster.</summary>
        public bool OnPlaneTapped(int planeId)
        {
            if (!awaitingVipTarget || model == null) return false;
            var p = model.GetPlane(planeId);
            if (p == null || p.State != PlaneState.OnBoard) return true;
            int cost = config.CostOf(BoosterKind.Vip);
            if (vipPaidWithCoins && progress.Coins < cost) { hud.Toast("Not enough coins"); Cancel(); return true; }
            if (model.TryLaunchVip(planeId) == LaunchResult.Success)   // pay only once the plane is actually on its way
            {
                if (vipPaidWithCoins) progress.TrySpend(cost);
                else progress.ConsumeBooster(BoosterKind.Vip);
                if (Used != null) Used(BoosterKind.Vip, vipPaidWithCoins);
            }
            Cancel();
            return true;
        }

        string Unavailable(BoosterKind kind)
        {
            switch (kind)
            {
                case BoosterKind.Vip: return model.CanUseVip ? null : "VIP stand is busy";
                case BoosterKind.Arrange: return model.CanArrange ? null : "No plane is waiting for passengers";
                case BoosterKind.Jumble: return model.CanJumble ? null : "Nothing to jumble";
                default: return null;
            }
        }

        void Request(BoosterKind kind, int slot)
        {
            DevLog.Info("booster", "request " + kind + (slot >= 0 ? " (stand " + slot + ")" : "") + ", owned " + (kind == BoosterKind.ExtraSlot ? 0 : progress.BoosterCount(kind)) + ", coins " + progress.Coins);
            pendingKind = kind; pendingSlot = slot;
            bool owned = kind != BoosterKind.ExtraSlot && progress.BoosterCount(kind) > 0;
            if (owned) { pendingPurchase = false; Execute(); return; }
            int cost = config.CostOf(kind);
            pendingPurchase = true;
            awaitingBuyAnswer = true;
            hud.ShowBuy(Title(kind), Describe(kind), cost, progress.Coins >= cost);
        }

        void OnBuyConfirmed()
        {
            if (!awaitingBuyAnswer || model == null) return;
            awaitingBuyAnswer = false;
            hud.HideBuy();
            if (progress.Coins < config.CostOf(pendingKind)) { hud.Toast("Not enough coins"); return; }
            Execute();
        }

        /// <summary>Runs the booster; pays (coins or inventory) only if it actually did something.</summary>
        void Execute()
        {
            switch (pendingKind)
            {
                case BoosterKind.Vip:
                    awaitingVipTarget = true;
                    vipPaidWithCoins = pendingPurchase;
                    hud.SetBoosterHighlight((int)BoosterKind.Vip);
                    hud.Toast("Tap any plane to fly it to the VIP stand");
                    return;   // pays when the plane is chosen
                case BoosterKind.Arrange:
                    if (model.Arrange()) Pay(); else hud.Toast("Queue is already arranged");
                    return;
                case BoosterKind.Jumble:
                    if (model.Jumble(rng)) Pay(); else hud.Toast("Nothing to jumble");
                    return;
                case BoosterKind.ExtraSlot:
                    if (model.TryUnlockSlot(pendingSlot)) { Pay(); if (SlotUnlocked != null) SlotUnlocked(pendingSlot); }
                    return;
            }
        }

        void Pay()
        {
            DevLog.Info("booster", "executed " + pendingKind + (pendingPurchase ? " (paid " + config.CostOf(pendingKind) + " coins)" : " (owned use)"));
            if (pendingPurchase) progress.TrySpend(config.CostOf(pendingKind));
            else progress.ConsumeBooster(pendingKind);
            if (Used != null) Used(pendingKind, pendingPurchase);
        }

        public static string NameOf(BoosterKind k) { return Title(k); }
        public static string DescriptionOf(BoosterKind k) { return Describe(k); }

        static string Title(BoosterKind k)
        {
            switch (k)
            {
                case BoosterKind.Vip: return "VIP";
                case BoosterKind.Arrange: return "Arrange";
                case BoosterKind.Jumble: return "Jumble";
                default: return "Extra Stand";
            }
        }

        static string Describe(BoosterKind k)
        {
            switch (k)
            {
                case BoosterKind.Vip: return "Fly any plane straight to the VIP stand";
                case BoosterKind.Arrange: return "Bring matching passengers to the front";
                case BoosterKind.Jumble: return "Shuffle the colours of the planes";
                default: return "Open one more stand for this level";
            }
        }
    }
}
