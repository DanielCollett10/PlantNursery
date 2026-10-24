using PlantNursery.Sim.Core;
using NUnit.Framework;

namespace PlantNursery.Sim.Tests
{
    public class SimWorldTests
    {
        [Test]
        public void StepAdvancesTickAndTime()
        {
            var world = new SimWorld(secondsPerTick: 0.5);
            world.Step(4);
            Assert.That(world.Tick, Is.EqualTo(4));
            Assert.That(world.SimulatedSeconds, Is.EqualTo(2.0).Within(1e-9));
        }

        [Test]
        public void NewWorldIsNotPaused()
        {
            Assert.That(new SimWorld().IsPaused, Is.False);
        }

        [Test]
        public void StepWhilePausedDoesNotAdvance()
        {
            var world = new SimWorld();
            world.Step(2);
            world.Pause();
            world.Step(5);
            Assert.That(world.IsPaused, Is.True);
            Assert.That(world.Tick, Is.EqualTo(2));
            Assert.That(world.SimulatedSeconds, Is.EqualTo(2 * world.SecondsPerTick).Within(1e-9));
        }

        [Test]
        public void ResumeAllowsSteppingAgain()
        {
            var world = new SimWorld();
            world.Pause();
            world.Step(3);
            world.Resume();
            world.Step(3);
            Assert.That(world.IsPaused, Is.False);
            Assert.That(world.Tick, Is.EqualTo(3));
        }

        [Test]
        public void NegativeStepIsRejected()
        {
            var world = new SimWorld();
            Assert.Throws<System.ArgumentOutOfRangeException>(() => world.Step(-1));
        }

        private sealed class Recorder : ISimSystem
        {
            private readonly System.Collections.Generic.List<string> _log;
            private readonly string _name;
            public Recorder(System.Collections.Generic.List<string> log, string name) { _log = log; _name = name; }
            public void Update(SimWorld world) { _log.Add(_name); }
        }

        [Test]
        public void SystemsUpdateInPhaseOrderRegardlessOfRegistrationOrder()
        {
            var log = new System.Collections.Generic.List<string>();
            var world = new SimWorld();
            world.AddSystem(new Recorder(log, "econ"), SystemPhase.Economy);
            world.AddSystem(new Recorder(log, "orders"), SystemPhase.Orders);
            world.AddSystem(new Recorder(log, "growth"), SystemPhase.Growth);
            world.AddSystem(new Recorder(log, "mob"), SystemPhase.Mobility);
            world.AddSystem(new Recorder(log, "time"), SystemPhase.Time);
            world.AddSystem(new Recorder(log, "water"), SystemPhase.Watering);
            world.AddSystem(new Recorder(log, "beh"), SystemPhase.Behaviour);
            world.AddSystem(new Recorder(log, "growth2"), SystemPhase.Growth);
            world.Step();
            Assert.That(log, Is.EqualTo(new[] { "time", "growth", "growth2", "water", "beh", "mob", "orders", "econ" }));
            Assert.That(world.SystemCount(SystemPhase.Weather), Is.EqualTo(0));
        }

        [Test]
        public void NextEntityIdIncreasesAndIsNeverReused()
        {
            var world = new SimWorld();
            var a = world.NextEntityId();
            var b = world.NextEntityId();
            var c = world.NextEntityId();
            Assert.That(b, Is.GreaterThan(a));
            Assert.That(c, Is.GreaterThan(b));
        }

        private sealed class RollSystem : ISimSystem
        {
            public readonly System.Collections.Generic.List<int> Rolls = new System.Collections.Generic.List<int>();
            public void Update(SimWorld world) { Rolls.Add(world.Random.Next(1000)); world.NextEntityId(); }
        }

        private static (System.Collections.Generic.List<int>, int) Run(int seed)
        {
            var world = new SimWorld(seed: seed);
            var sys = new RollSystem();
            world.AddSystem(sys, SystemPhase.Growth);
            world.Step(20);
            return (sys.Rolls, world.NextEntityId());
        }

        [Test]
        public void SameSeedGivesIdenticalState_DifferentSeedDiffers()
        {
            var (a, ida) = Run(42);
            var (b, idb) = Run(42);
            var (c, _) = Run(43);
            Assert.That(a, Is.EqualTo(b));
            Assert.That(ida, Is.EqualTo(idb));
            Assert.That(c, Is.Not.EqualTo(a));
            Assert.That(new SimWorld(seed: 7).Seed, Is.EqualTo(7));
        }
    }
}
