using System;
using System.Collections.Generic;
using PlantNursery.Sim.Building;
using PlantNursery.Sim.Content;
using PlantNursery.Sim.Core;
using PlantNursery.Sim.Hands;
using PlantNursery.Sim.Plants;
using PlantNursery.Sim.Time;

namespace PlantNursery.Sim.Economy
{
    /// <summary>
    /// Soft-lock guard: "a neighbour's gift" (docs/design/economy.md#soft-lock-guard-a-neighbours-gift-mvp).
    /// Runs once at each day start. If the wallet is below the cheapest seed packet, no living plant exists
    /// and no seed packet with seeds exists anywhere, one free packet is delivered (shelf first, then any
    /// surface), and a plug tray is added if the nursery owns none. If there is no room, it waits for the next day.
    /// </summary>
    public sealed class SoftLockGuard : ISimSystem
    {
        /// <summary>Item id of the plug tray.</summary>
        public const string PlugTrayItemId = "plug_tray";

        private readonly ContentDb _content;
        private readonly Wallet _wallet;
        private readonly BuildState _build;
        private readonly SurfaceGrid _surface;
        private readonly HandsState _hands;
        private readonly GameClock _clock;

        private SoftLockGuard(ContentDb content, Wallet wallet, BuildState build, SurfaceGrid surface,
            HandsState hands, GameClock clock)
        {
            _content = content ?? throw new ArgumentNullException(nameof(content));
            _wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));
            _build = build ?? throw new ArgumentNullException(nameof(build));
            _surface = surface ?? throw new ArgumentNullException(nameof(surface));
            _hands = hands ?? throw new ArgumentNullException(nameof(hands));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        }

        /// <summary>Creates the guard and adds it as a system. Install it after the clock.</summary>
        public static SoftLockGuard Install(SimWorld world, ContentDb content, Wallet wallet, BuildState build,
            SurfaceGrid surface, HandsState hands, GameClock clock)
        {
            if (world == null) throw new ArgumentNullException(nameof(world));
            var guard = new SoftLockGuard(content, wallet, build, surface, hands, clock);
            world.AddSystem(guard, SystemPhase.Economy);
            return guard;
        }

        /// <inheritdoc />
        public void Update(SimWorld world)
        {
            var tick = world.Tick;
            if (tick <= 0) return;
            // Same test the clock uses to raise DayStartedEvent: only the tick a new day begins is checked.
            if (_clock.DayAt(tick) == _clock.DayAt(tick - 1)) return;
            if (!_content.Economy.SoftLockGuard.Enabled) return;
            if (!IsTriggered()) return;
            Deliver(world);
        }

        /// <summary>
        /// True when all three trigger conditions hold: wallet below the cheapest seed packet price, no living plant,
        /// and no seed packet with at least one seed on a surface, in hand or in the build inventory.
        /// </summary>
        public bool IsTriggered()
        {
            if (!_content.ItemDefs.TryGetValue(SupplierModule.SeedPacketId, out var packet)) return false;
            if (_wallet.Balance >= packet.Price) return false;
            if (LivingPlantExists()) return false;
            if (SeedsExist()) return false;
            return true;
        }

        private void Deliver(SimWorld world)
        {
            var packet = _content.ItemDefs[SupplierModule.SeedPacketId];
            var spot = DeliveryShelf.FindSpotOnShelf(_build, _surface, packet)
                ?? DeliveryShelf.FindSpotAnywhere(_build, _surface, packet);
            if (!spot.HasValue) return; // No room anywhere: the gift waits for the next morning.

            var varietyId = GiftVarietyId();
            var item = _surface.Place(packet.Id, spot.Value, Rotation.Deg0, out _, packet.SeedCount);
            if (item == null) return;
            _hands.GetContents(item).VarietyId = varietyId;

            var trayAdded = false;
            if (!OwnsPlugTray())
            {
                _build.Inventory.Add(PlugTrayItemId);
                trayAdded = true;
            }
            world.Raise(new NeighbourGift(packet.Id, varietyId, item.Id, trayAdded));
        }

        /// <summary>The configured gift variety, or the first variety in content when none is configured.</summary>
        /// <remarks>Varieties carry no price yet, so "cheapest" cannot be computed; the first is used instead.</remarks>
        private string GiftVarietyId()
        {
            var configured = _content.Economy.SoftLockGuard.GiftVarietyId;
            if (configured != null) return configured;
            foreach (var k in _content.VarietyDefs.Keys) return k;
            throw new InvalidOperationException("No varieties in content.");
        }

        private bool LivingPlantExists()
        {
            var plants = new List<KeyValuePair<Plant, ContainerDef>>();
            _hands.CollectPlants(plants);
            foreach (var kv in plants)
                if (!kv.Key.IsDead) return true;
            return false;
        }

        private bool SeedsExist()
        {
            foreach (var item in _surface.Items)
                if (item.ItemId == SupplierModule.SeedPacketId && _hands.GetContents(item).Seeds > 0) return true;
            foreach (var held in _hands.HeldItems)
                if (held.ItemId == SupplierModule.SeedPacketId && held.Contents.Seeds > 0) return true;
            return _build.Inventory.Count(SupplierModule.SeedPacketId) > 0;
        }

        private bool OwnsPlugTray()
        {
            if (_build.Inventory.Count(PlugTrayItemId) > 0) return true;
            foreach (var p in _build.PlacedItems)
                if (p.ItemId == PlugTrayItemId) return true;
            foreach (var item in _surface.Items)
                if (item.ItemId == PlugTrayItemId) return true;
            foreach (var held in _hands.HeldItems)
                if (held.ItemId == PlugTrayItemId) return true;
            return false;
        }
    }
}
