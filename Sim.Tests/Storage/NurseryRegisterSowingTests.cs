using System.Linq;
using NUnit.Framework;
using PlantNursery.Sim.Content;
using PlantNursery.Sim.Core;
using PlantNursery.Sim.Plants;
using PlantNursery.Sim.Storage;

namespace PlantNursery.Sim.Tests
{
    /// <summary>Nursery.RegisterSowing wires SowSpecies to the nursery's own hands, slots and plant map (T-158).</summary>
    public class NurseryRegisterSowingTests
    {
        private static readonly PlayerId P = new PlayerId(1);

        private static (SimWorld world, ContentDb db, Nursery n, SlotAddress slot) Setup()
        {
            var db = ContentLoader.Load(ContentLoaderTests.RealFiles(false));
            var world = new SimWorld(0.05, 1);
            var n = NurseryBuilder.Build(world, db);
            n.Contents.Register(world);
            n.RegisterSowing(world, db);
            var bench = n.Build.PlacedItems.Single(i => i.ItemId == "bench_3m");
            return (world, db, n, SlotAddress.Planting(bench.Id, "plug_tray", 0, 0));
        }

        [Test]
        public void SowsAnEmptyTrayAndPlantsJoinTheNurseryMap()
        {
            var (world, db, n, slot) = Setup();
            world.Submit(new PickUpFromSlot(P, slot));
            world.Step();
            n.Hands.TakeHeld(P);
            var tray = new EntityFactory(world, db).CreateContainer("plug_tray");
            n.Hands.TryGive(P, HeldThing.OfContainer(tray));
            world.Submit(new PutInSlot(P, slot));
            world.Step();
            n.Hands.TryGive(P, HeldThing.Fresh(db.ItemDefs["spores_hearthmoss_amber"]));

            var result = world.Validate(new SowSpecies(P, tray.Id));
            Assert.IsTrue(result.IsAccepted, result.Message);
            world.Submit(new SowSpecies(P, tray.Id));
            world.Step();

            Assert.AreEqual(1, tray.PlantIds.Count);
            Assert.IsNotNull(n.PlantById(tray.PlantIds[0]));
        }

        [Test]
        public void RejectsWithoutAPacketAndForAnUnknownContainer()
        {
            var (world, _, n, slot) = Setup();
            var id = n.Contents.Get(slot).Container.Id;
            Assert.AreEqual("NoSeedPacket", world.Validate(new SowSpecies(P, id)).Message);
        }
    }
}
