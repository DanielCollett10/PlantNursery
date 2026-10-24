using System;
using PlantNursery.Sim.Building;
using PlantNursery.Sim.Content;
using PlantNursery.Sim.Core;
using PlantNursery.Sim.Economy;
using PlantNursery.Sim.Plants;

namespace PlantNursery.Sim.Storage
{
    /// <summary>Builds day one from <c>economy.starterKit</c> (docs/design/economy.md, Starting kit).</summary>
    public static class NurseryBuilder
    {
        /// <summary>
        /// Creates a new nursery: places the kit's floor items, sows its plug trays (plants from the plant factory,
        /// ids from <see cref="SimWorld.NextEntityId"/>), puts strip items on the benches, stocks the crate and fills the purse
        /// from <c>startingMoney</c>. Deterministic: only the world's id counter is used, in file order.
        /// </summary>
        /// <exception cref="InvalidOperationException">If content disagrees with itself (validated content never does).</exception>
        public static Nursery Build(SimWorld world, ContentDb content)
        {
            if (world == null) throw new ArgumentNullException(nameof(world));
            if (content == null) throw new ArgumentNullException(nameof(content));

            var build = new BuildState(content);
            var slots = new SlotOccupancy(content, build);
            var strip = new StripOccupancy(content, build);
            var crate = new StorageCrate();
            var hands = new HandsV2(content, crate);
            var contents = new SlotContents(slots, strip, hands, content);
            var nursery = new Nursery(build, slots, strip, contents, crate, hands, new Wallet(content.Economy.StartingMoney));
            var entities = new EntityFactory(world, content);
            var plants = new SpeciesPlantFactory(world, content);

            foreach (var p in content.StarterKit.Placements)
            {
                // Kit items are free: lend one to the inventory so the normal placement rules apply.
                build.Inventory.Add(p.ItemId);
                var placed = build.Place(p.ItemId, ItemLayer.Floor, new Cell(p.X, p.Z), (Rotation)p.RotationDegrees, out var why);
                if (placed == null) throw new InvalidOperationException("Starter kit cannot place " + p.ItemId + ": " + why);

                foreach (var s in p.Planting) SowSlot(content, entities, plants, nursery, placed, s);
                foreach (var s in p.Strip) PutOnStrip(content, nursery, placed, s);
                foreach (var h in p.Holds)
                {
                    if (p.ItemId != "storage_crate")
                        throw new InvalidOperationException("Only the storage crate can hold stock, not " + p.ItemId);
                    crate.AddStock(h.ItemId, h.Count);
                }
            }
            return nursery;
        }

        private static void SowSlot(ContentDb content, EntityFactory entities, SpeciesPlantFactory plants,
            Nursery n, PlacedItem bench, StarterSowing s)
        {
            var container = entities.CreateContainerForItem(s.ContainerItemId);
            var layout = SlotLayout.For(content.ItemDefs[bench.ItemId].Bench, container.Def);
            var col = s.Slot % layout.Cols;
            var row = s.Slot / layout.Cols;

            var plant = plants.Create(SpeciesOf(content, s.VarietyId), s.VarietyId, container.Def.Id, 0);
            plant.Moisture = s.Moisture;
            container.WaterMeter = s.Moisture;
            container.AddPlant(plant.Id);
            n.PlantMap[plant.Id] = plant;

            var r = n.Slots.PlaceInSlot(bench.Id, container.Def.Id, col, row);
            if (r != SlotRejection.None) throw new InvalidOperationException("Starter kit planting slot " + s.Slot + ": " + r);
            n.Contents.Stock(SlotAddress.Planting(bench.Id, container.Def.Id, col, row), HeldThing.OfContainer(container));
        }

        private static void PutOnStrip(ContentDb content, Nursery n, PlacedItem bench, StarterStripItem s)
        {
            var cols = n.Strip.Cols(content.ItemDefs[bench.ItemId].Bench, s.ItemId);
            var col = s.Slot % cols;
            var row = s.Slot / cols;
            var r = n.Strip.Place(bench.Id, s.ItemId, col, row, StripContents.NoPlants);
            if (r != StripRejection.None) throw new InvalidOperationException("Starter kit strip slot " + s.Slot + ": " + r);
            n.Contents.Stock(SlotAddress.Strip(bench.Id, s.ItemId, col, row), HeldThing.Fresh(content.ItemDefs[s.ItemId]));
        }

        private static string SpeciesOf(ContentDb content, string varietyId)
        {
            foreach (var sp in content.SpeciesDefs.Values)
                if (sp.Varieties.ContainsKey(varietyId)) return sp.Id;
            throw new InvalidOperationException("No species for variety " + varietyId);
        }
    }
}
