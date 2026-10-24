using System;
using System.Collections.Generic;
using PlantNursery.Sim.Content;
using PlantNursery.Sim.Core;
using PlantNursery.Sim.Plants;
using PlantNursery.Sim.Storage;
using PlantNursery.Sim.Time;

namespace PlantNursery.Sim.Economy
{
    /// <summary>
    /// Soft-lock guard on the storage-crate path (docs/design/economy.md, Soft-lock guard, rethemed).
    /// Runs once at each day start. When the purse is below the cheapest packet price (lowest Buy price among items
    /// with <c>packetSeeds</c>), no living plant is held in the crate, a bench or any hand, and no unsown packet is in
    /// the crate or any hand, one <c>spores_hearthmoss_amber</c> is put into the storage crate, plus a plug tray if
    /// none exists anywhere. Raises <see cref="NeighbourGift"/>. Runs beside the old <see cref="SoftLockGuard"/>
    /// (decision 0011); do not register both.
    /// </summary>
    public sealed class CrateSoftLockGuard : ISimSystem
    {
        /// <summary>Item id of the free packet (Hearthmoss spores, one tray).</summary>
        public const string GiftItemId = "spores_hearthmoss_amber";
        /// <summary>Item id of the plug tray added when the nursery owns none.</summary>
        public const string PlugTrayItemId = "plug_tray";

        private readonly ContentDb _content;
        private readonly Nursery _nursery;
        private readonly GameClock _clock;
        private readonly EntityFactory _factory;

        private CrateSoftLockGuard(ContentDb content, Nursery nursery, GameClock clock, EntityFactory factory)
        {
            _content = content;
            _nursery = nursery;
            _clock = clock;
            _factory = factory;
        }

        /// <summary>Creates the guard over a nursery built by <see cref="NurseryBuilder"/> and adds it in the Economy phase. Install after the clock.</summary>
        public static CrateSoftLockGuard Install(SimWorld world, ContentDb content, Nursery nursery, GameClock clock)
        {
            if (world == null) throw new ArgumentNullException(nameof(world));
            if (content == null) throw new ArgumentNullException(nameof(content));
            if (nursery == null) throw new ArgumentNullException(nameof(nursery));
            if (clock == null) throw new ArgumentNullException(nameof(clock));
            var guard = new CrateSoftLockGuard(content, nursery, clock, new EntityFactory(world, content));
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

        /// <summary>True when every condition holds: purse below the cheapest packet price, no living plant, no unsown packet in the crate or hands.</summary>
        public bool IsTriggered()
        {
            if (!_content.ItemDefs.ContainsKey(GiftItemId)) return false;
            if (!TryCheapestPacketPrice(out var price)) return false;
            if (_nursery.Wallet.Balance >= price) return false;
            if (LivingPlantExists()) return false;
            if (UnsownPacketHeld()) return false;
            return true;
        }

        private void Deliver(SimWorld world)
        {
            var gift = _content.ItemDefs[GiftItemId];
            _nursery.Crate.AddStock(GiftItemId, 1);

            var trayAdded = false;
            if (!OwnsPlugTray())
            {
                var tray = _factory.CreateContainerForItem(PlugTrayItemId);
                if (tray != null)
                {
                    _nursery.Crate.AddContainer(tray);
                    trayAdded = true;
                }
            }
            world.Raise(new NeighbourGift(GiftItemId, gift.VarietyId, trayAdded));
        }

        /// <summary>Lowest Buy price among items with packetSeeds and a price. False if there are none.</summary>
        private bool TryCheapestPacketPrice(out int price)
        {
            price = 0;
            var found = false;
            foreach (var def in _content.ItemDefs.Values)
            {
                if (def.PacketSeeds <= 0 || def.Price <= 0) continue;
                if (!found || def.Price < price) price = def.Price;
                found = true;
            }
            return found;
        }

        /// <summary>
        /// Every container held anywhere on this path: in the crate (loose or inside a carry tray), in a bench slot or on the
        /// strip (also inside carry trays), or in a hand.
        /// </summary>
        private IEnumerable<Container> AllContainers()
        {
            foreach (var c in _nursery.Crate.Containers) yield return c;
            foreach (var t in _nursery.Crate.Things)
                foreach (var c in ContainersIn(t)) yield return c;
            foreach (var t in _nursery.Contents.Things)
                foreach (var c in ContainersIn(t)) yield return c;
            foreach (var t in _nursery.Hands.AllHeld)
                foreach (var c in ContainersIn(t)) yield return c;
        }

        private static IEnumerable<Container> ContainersIn(HeldThing t)
        {
            if (t.Container != null) yield return t.Container;
            foreach (var pot in t.Pots) yield return pot;
        }

        /// <summary>
        /// True if a container holds a plant that is not dead. A plant id this nursery does not know counts as living,
        /// so an unexpected plant never lets a gift stack on top of it.
        /// </summary>
        private bool LivingPlantExists()
        {
            foreach (var c in AllContainers())
                foreach (var id in c.PlantIds)
                    if (!_nursery.Plants.TryGetValue(id, out var plant) || !plant.IsDead) return true;
            return false;
        }

        /// <summary>True if the crate or any hand holds a packet (counted or loose) that still has seeds.</summary>
        private bool UnsownPacketHeld()
        {
            foreach (var def in _content.ItemDefs.Values)
                if (def.PacketSeeds > 0 && _nursery.Crate.StockCount(def.Id) > 0) return true;
            foreach (var t in _nursery.Crate.Things)
                if (IsUnsownPacket(t)) return true;
            foreach (var t in _nursery.Hands.AllHeld)
                if (IsUnsownPacket(t)) return true;
            return false;
        }

        private static bool IsUnsownPacket(HeldThing t) => t.Kind == HeldKind.SeedPacket && t.SeedsLeft > 0;

        /// <summary>True if a plug tray is in the build inventory, placed on the floor, or in the crate, a bench slot or a hand.</summary>
        private bool OwnsPlugTray()
        {
            if (_nursery.Build.Inventory.Count(PlugTrayItemId) > 0) return true;
            foreach (var p in _nursery.Build.PlacedItems)
                if (p.ItemId == PlugTrayItemId) return true;
            if (_nursery.Crate.HasItem(PlugTrayItemId)) return true;
            foreach (var t in _nursery.Contents.Things)
                if (t.ItemId == PlugTrayItemId) return true;
            foreach (var t in _nursery.Hands.AllHeld)
                if (t.ItemId == PlugTrayItemId) return true;
            return false;
        }
    }
}
