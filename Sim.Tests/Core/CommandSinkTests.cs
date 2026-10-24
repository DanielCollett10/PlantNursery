using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using PlantNursery.Sim.Building;
using PlantNursery.Sim.Content;
using PlantNursery.Sim.Core;

namespace PlantNursery.Sim.Tests
{
    public class CommandSinkTests
    {
        private static readonly PlayerId P1 = new PlayerId(1);
        private static readonly PlayerId P2 = new PlayerId(2);

        private static ContentDb LoadContent()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            string found = null;
            while (dir != null && found == null)
            {
                var candidate = Path.Combine(dir.FullName, "Assets", "StreamingAssets", "Content");
                if (Directory.Exists(candidate)) found = candidate;
                dir = dir.Parent;
            }
            var files = new Dictionary<string, string>();
            foreach (var path in Directory.GetFiles(found, "*.json"))
            {
                var name = Path.GetFileName(path);
                if (name == ContentLoader.TimeDemoFile) continue;
                files[name] = File.ReadAllText(path);
            }
            return ContentLoader.Load(files);
        }

        [Test]
        public void LoopbackSinkForwardsToWorldSubmit()
        {
            var world = new SimWorld();
            BuildingModule.Register(world, new BuildState(LoadContent()));
            ICommandSink sink = new LoopbackCommandSink(world);

            sink.Submit(new PlaceItem(P1, "bench_standard", ItemLayer.Floor, new Cell(4, 4), Rotation.Deg0));

            Assert.AreEqual(1, world.PendingCommandCount);
        }

        [Test]
        public void LoopbackSinkRejectsNullWorldAndNullCommand()
        {
            Assert.Throws<System.ArgumentNullException>(() => new LoopbackCommandSink(null));
            var sink = new LoopbackCommandSink(new SimWorld());
            Assert.Throws<System.ArgumentNullException>(() => sink.Submit(null));
        }

        [Test]
        public void CommandsFromTwoPlayersKeepIdsAndApplyInSubmissionOrder()
        {
            var world = new SimWorld();
            var state = new BuildState(LoadContent());
            BuildingModule.Register(world, state);
            ICommandSink sink = new LoopbackCommandSink(world);

            // Same cell: the first submitted places, the second is rejected as Occupied.
            sink.Submit(new PlaceItem(P1, "bench_standard", ItemLayer.Floor, new Cell(4, 4), Rotation.Deg0));
            sink.Submit(new PlaceItem(P2, "bench_standard", ItemLayer.Floor, new Cell(4, 4), Rotation.Deg0));
            world.Step();

            var events = world.DrainEvents();
            var placed = events.OfType<ItemPlaced>().Single();
            Assert.AreEqual(P1, placed.PlayerId);

            var rejected = events.OfType<CommandRejectedEvent>().Single();
            Assert.AreEqual(P2, rejected.Command.PlayerId);
            Assert.AreEqual(RejectReason.Blocked, rejected.Result.Reason);
            Assert.AreEqual("Occupied", rejected.Result.Message);
            Assert.AreEqual(1, state.PlacedItems.Count());
        }

        [Test]
        public void SubmissionOrderIsPreservedAcrossPlayersWhenReversed()
        {
            var world = new SimWorld();
            var state = new BuildState(LoadContent());
            BuildingModule.Register(world, state);
            ICommandSink sink = new LoopbackCommandSink(world);

            sink.Submit(new PlaceItem(P2, "bench_standard", ItemLayer.Floor, new Cell(4, 4), Rotation.Deg0));
            sink.Submit(new PlaceItem(P1, "bench_standard", ItemLayer.Floor, new Cell(4, 4), Rotation.Deg0));
            world.Step();

            Assert.AreEqual(P2, world.DrainEvents().OfType<ItemPlaced>().Single().PlayerId);
        }
    }
}
