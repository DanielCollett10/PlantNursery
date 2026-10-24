using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using NUnit.Framework;
using PlantNursery.Sim.Building;
using PlantNursery.Sim.Content;
using PlantNursery.Sim.Core;
using PlantNursery.Sim.Economy;
using PlantNursery.Sim.Hands;
using PlantNursery.Sim.Plants;
using PlantNursery.Sim.Storage;
using PlantNursery.Sim.Time;
using RefillCan = PlantNursery.Sim.Storage.RefillCan;

namespace PlantNursery.Sim.Tests
{
    /// <summary>
    /// Headless M2 scenario (T-145): two players over a <see cref="LoopbackCommandSink"/> run the calm-bench loop for five days
    /// on the real content and starter kit. Moss is watered and scraped daily; a bought Blue Chimebell tray is sown in one click and
    /// potted on to a Specimen; a second, never-watered tray wilts, dies and is composted; every night ends with Sleep.
    /// </summary>
    public class TwoPlayersFiveDaysTests
    {
        private static readonly PlayerId P1 = new PlayerId(1);
        private static readonly PlayerId P2 = new PlayerId(2);
        private const int Days = 5;

        /// <summary>First tick a plant was seen dry / wilted / dead (recorded by a probe system).</summary>
        private sealed class Probe : ISimSystem
        {
            public readonly List<Plant> Watched = new List<Plant>();
            public readonly Dictionary<int, long> Dry = new Dictionary<int, long>();
            /// <summary>The plant's DryTicks counter when first seen (the probe joins a tick or two after sowing).</summary>
            public readonly Dictionary<int, int> DryTicksThen = new Dictionary<int, int>();
            public readonly Dictionary<int, long> Wilted = new Dictionary<int, long>();
            public readonly Dictionary<int, long> Dead = new Dictionary<int, long>();

            public void Update(SimWorld w)
            {
                foreach (var p in Watched)
                {
                    if (p.IsDry && !Dry.ContainsKey(p.Id)) { Dry[p.Id] = w.Tick; DryTicksThen[p.Id] = p.DryTicks; }
                    if (p.IsWilted && !Wilted.ContainsKey(p.Id)) Wilted[p.Id] = w.Tick;
                    if (p.IsDead && !Dead.ContainsKey(p.Id)) Dead[p.Id] = w.Tick;
                }
            }
        }

        private sealed class Run
        {
            public ContentDb Db;
            public SimWorld World;
            public GameClock Clock;
            public Nursery N;
            public Probe Probe = new Probe();
            public readonly List<ISimEvent> Events = new List<ISimEvent>();
            public readonly Dictionary<int, LoopbackCommandSink> Sinks = new Dictionary<int, LoopbackCommandSink>();

            public int StarterBench, BenchB, BenchC, Barrel;
            public SlotAddress CanAddr, TrayAddr, SmallAddr, MediumAddr, LargeAddr, DryAddr, ClashAddr;
            public Container BlueTray, DryTray;
            public Plant Blue;
            public List<int> MossTrays = new List<int>();
            public int Stage; // 0 tray, 1 small, 2 medium, 3 large (done)
            public bool Composted;
            public float DayDrop, NightDrop;
            public int ClashRejections;

            public Plant Lookup(int id) => N.Plants.TryGetValue(id, out var p) ? p : null;

            public IEnumerable<Container> Containers()
            {
                foreach (var t in N.Contents.Things) if (t.Kind == HeldKind.Container) yield return t.Container;
                foreach (var t in N.Hands.AllHeld) if (t.Kind == HeldKind.Container) yield return t.Container;
                foreach (var c in N.Crate.Containers) yield return c;
            }

            /// <summary>Submit the commands through each player's sink, step one tick, keep events; fail on unexpected rejections.</summary>
            public List<CommandRejectedEvent> Do(bool allowReject, params ICommand[] cmds)
            {
                foreach (var c in cmds) Sinks[c.PlayerId.Value].Submit(c);
                World.Step(1);
                var fresh = World.DrainEvents();
                Events.AddRange(fresh);
                var rejected = fresh.OfType<CommandRejectedEvent>().ToList();
                if (!allowReject)
                    Assert.IsEmpty(rejected, rejected.Count > 0 ? rejected[0].Command.GetType().Name + ": " + rejected[0].Result.Message : "");
                return rejected;
            }

            public void Tick() { World.Step(1); Events.AddRange(World.DrainEvents()); }

            public int Held(PlayerId p) => N.Hands.GetHeld(p).Container.Id;
        }

        private static Run Build()
        {
            var r = new Run { Db = ContentLoader.Load(ContentLoaderTests.RealFiles(false)) };
            r.World = new SimWorld(0.05, 42);
            r.Clock = GameClock.Install(r.World, r.Db.Time);
            r.N = NurseryBuilder.Build(r.World, r.Db);
            var n = r.N;
            r.Sinks[P1.Value] = new LoopbackCommandSink(r.World);
            r.Sinks[P2.Value] = new LoopbackCommandSink(r.World);

            var factory = new EntityFactory(r.World, r.Db);
            CrateSupplier.Register(r.World, r.Db, n.Wallet, n.Build, n.Crate, factory);
            CrateCommands.Register(r.World, n.Hands);
            n.Contents.Register(r.World);
            n.Contents.RegisterPotOn(r.World, n.Crate, n.Plants);
            WaterCommands.Register(r.World, n, r.Db);
            MossCommands.Register(r.World, n, r.Db, r.Lookup);
            CompostCommands.Register(r.World, n);
            new SowSystem(r.Db, n.Hands, n.Contents, new SpeciesPlantFactory(r.World, r.Db), n.PlantMap).Register(r.World);

            SpeciesGrowthSystem.Install(r.World, r.Db, r.Containers, r.Lookup, r.Clock);
            WaterSystem.Install(r.World, r.Containers, r.Lookup, r.Clock);
            MossRegrowSystem.Install(r.World, r.Containers, r.Lookup);
            r.World.AddSystem(r.Probe, SystemPhase.Behaviour);
            return r;
        }

        private static int PlaceBench(Run r, Cell cell)
        {
            r.N.Build.Inventory.Add("bench_3m");
            var b = r.N.Build.Place("bench_3m", ItemLayer.Floor, cell, Rotation.Deg0, out var why);
            Assert.IsNotNull(b, why.ToString());
            return b.Id;
        }

        private static void Setup(Run r)
        {
            var n = r.N;
            r.StarterBench = n.Build.PlacedItems.First(p => p.ItemId == "bench_3m").Id;
            r.Barrel = n.Build.PlacedItems.First(p => p.ItemId == RefillCan.RainBarrelItemId).Id;
            r.CanAddr = SlotAddress.Strip(r.StarterBench, "watering_can", 0, 0);
            r.MossTrays = r.Containers().Where(c => c.PlantIds.Count > 0).Select(c => c.Id).ToList();
            Assert.AreEqual(6, r.MossTrays.Count, "starter kit: six moss trays");
            r.BenchB = PlaceBench(r, new Cell(10, 10));
            r.BenchC = PlaceBench(r, new Cell(10, 13));
            r.TrayAddr = SlotAddress.Planting(r.BenchB, "plug_tray", 0, 0);
            r.SmallAddr = SlotAddress.Planting(r.BenchB, "pot_small", 4, 0);
            r.MediumAddr = SlotAddress.Planting(r.BenchB, "pot_medium", 4, 0);
            r.LargeAddr = SlotAddress.Planting(r.BenchB, "pot_large", 3, 0);
            r.DryAddr = SlotAddress.Planting(r.BenchC, "plug_tray", 0, 0);
            r.ClashAddr = SlotAddress.Planting(r.BenchC, "pot_small", 5, 0);

            // P1 shops: two packets, two trays, pots for the chain and the clash.
            var money = n.Wallet.Balance;
            r.Do(false,
                new BuyItem(P1, "seeds_chimebell_blue", 2), new BuyItem(P1, "plug_tray", 2),
                new BuyItem(P1, "pot_small", 3), new BuyItem(P1, "pot_medium", 1), new BuyItem(P1, "pot_large", 1));
            Assert.AreEqual(money - (2 * 12 + 2 * 5 + 3 * 2 + 4 + 8), n.Wallet.Balance);
            Assert.AreEqual(2, n.Crate.StockCount("seeds_chimebell_blue"));

            // Tray 1 (P1) and tray 2 (P2) onto the benches.
            r.Do(false, new TakeFromCrate(P1, "plug_tray"), new TakeFromCrate(P2, "plug_tray"));
            r.BlueTray = n.Hands.GetHeld(P1).Container;
            r.DryTray = n.Hands.GetHeld(P2).Container;
            r.Do(false, new PutInSlot(P1, r.TrayAddr), new PutInSlot(P2, r.DryAddr));

            // One click fills the whole plug tray: P1 sows the blue packet, P2 the other (left unwatered).
            r.Do(false, new TakeFromCrate(P1, "seeds_chimebell_blue"), new TakeFromCrate(P2, "seeds_chimebell_blue"));
            r.Do(false, new SowSpecies(P1, r.BlueTray.Id), new SowSpecies(P2, r.DryTray.Id));
            Assert.AreEqual(6, r.BlueTray.PlantIds.Count, "one click fills the tray");
            Assert.AreEqual(6, r.DryTray.PlantIds.Count);
            Assert.IsNull(n.Hands.GetHeld(P1), "empty packet is used up");
            Assert.AreEqual(12, r.Events.OfType<PlantSown>().Count());
            r.Blue = n.Plants[r.BlueTray.PlantIds[0]];
            foreach (var id in r.DryTray.PlantIds) r.Probe.Watched.Add(n.Plants[id]);

            // Pots into their slots; two players race for one slot on bench C (second is Taken).
            foreach (var (size, addr) in new[] { ("pot_small", r.SmallAddr), ("pot_medium", r.MediumAddr), ("pot_large", r.LargeAddr) })
            {
                r.Do(false, new TakeFromCrate(P1, size));
                r.Do(false, new PutInSlot(P1, addr));
            }
            r.Do(false, new TakeFromCrate(P1, "pot_small"), new TakeFromCrate(P2, "pot_small"));
            var rejected = r.Do(true, new PutInSlot(P1, r.ClashAddr), new PutInSlot(P2, r.ClashAddr));
            Assert.AreEqual(1, rejected.Count, "same-slot clash");
            Assert.AreEqual(RejectReason.Taken, rejected[0].Result.Reason);
            Assert.AreEqual(P2.Value, rejected[0].Command.PlayerId.Value);
            r.ClashRejections = rejected.Count;
            Assert.IsNull(n.Hands.GetHeld(P1));
            Assert.IsNotNull(n.Hands.GetHeld(P2), "loser still holds the pot");
            r.Do(false, new ReturnToCrate(P2));

            WaterRound(r, null);
        }

        private static List<int> WaterTargets(Run r)
        {
            var ids = new List<int>();
            foreach (var t in r.N.Contents.Things)
                if (t.Kind == HeldKind.Container && t.Container.PlantIds.Count > 0 && t.Container.Id != r.DryTray.Id)
                    ids.Add(t.Container.Id);
            return ids.OrderBy(i => i).ToList();
        }

        /// <summary>P1 takes the can off the strip, refills it at the barrel, waters every tended container (plus an extra pot) and puts it back.</summary>
        private static void WaterRound(Run r, int? extra)
        {
            var cmds = new List<ICommand> { new PickUpFromSlot(P1, r.CanAddr), new RefillCan(P1, r.Barrel) };
            var ids = WaterTargets(r);
            if (extra.HasValue) ids.Add(extra.Value);
            cmds.AddRange(ids.Select(id => (ICommand)new WaterContainer(P1, id)));
            cmds.Add(new PutInSlot(P1, r.CanAddr));
            r.Do(false, cmds.ToArray());
        }

        private static void ScrapeAll(Run r)
        {
            var cmds = new List<ICommand>();
            for (var i = 0; i < r.MossTrays.Count; i++)
            {
                var who = i % 2 == 0 ? P1 : P2;
                cmds.Add(new ScrapeMoss(who, r.MossTrays[i]));
                cmds.Add(new ReturnToCrate(who));
            }
            r.Do(false, cmds.ToArray());
        }

        private static void TryPotOn(Run r)
        {
            if (r.Stage >= 3) return;
            var p = r.Blue;
            SlotAddress from, to;
            switch (r.Stage)
            {
                case 0: if (p.StageProgress < 1f) return; from = r.TrayAddr; to = r.SmallAddr; break;
                case 1: if (p.StageProgress < 1f) return; from = r.SmallAddr; to = r.MediumAddr; break;
                default: if (p.StageIndex < 3) return; from = r.MediumAddr; to = r.LargeAddr; break;
            }
            var target = r.N.Contents.Get(to).Container.Id;
            // P1 waters the target pot while P2 lifts the source; then P2 pots on.
            r.Do(false, new PickUpFromSlot(P1, r.CanAddr), new RefillCan(P1, r.Barrel), new WaterContainer(P1, target),
                new PutInSlot(P1, r.CanAddr), new PickUpFromSlot(P2, from));
            r.Do(false, new PotOn(P2, to, r.Stage == 0 ? p.CellIndex : -1));
            if (r.N.Hands.GetHeld(P2) != null) r.Do(false, new PutInSlot(P2, from)); // tray with plants left goes back
            r.Stage++;
            Assert.AreEqual(r.N.Contents.Get(to).Container.Id, r.N.FindContainerOfPlant(p.Id).Id, "plant sits in the new pot");
        }

        private static void TryCompost(Run r)
        {
            if (r.Composted) return;
            var dead = r.DryTray.PlantIds.Where(id => r.Lookup(id) != null && r.Lookup(id).IsDead).ToList();
            if (dead.Count < r.DryTray.PlantIds.Count) return;
            r.Do(false, dead.Select((id, i) => (ICommand)new CompostSpecies(i % 2 == 0 ? P1 : P2, id)).ToArray());
            Assert.AreEqual(0, r.DryTray.PlantIds.Count);
            r.Do(false, new PickUpFromSlot(P2, r.DryAddr), new ReturnToCrate(P2));
            Assert.AreSame(r.DryTray, r.N.Crate.FindContainer(r.DryTray.Id), "empty tray back in the crate");
            r.Composted = true;
        }

        /// <summary>Plays the whole scenario and returns the finished run.</summary>
        private static Run Play()
        {
            var r = Build();
            Setup(r);
            var rainRate = 0f;
            var fired = new HashSet<string> { "1:366" }; // day one's morning round is part of Setup
            bool Due(int day, int minute)
            {
                if (r.Clock.MinuteOfDay < minute || r.Clock.Day != day || fired.Contains(day + ":" + minute)) return false;
                fired.Add(day + ":" + minute);
                return true;
            }
            for (var day = 1; day <= Days; day++)
            {
                // Each day is stepped a tick at a time so every reaction happens on its own minute.
                while (r.Clock.Day == day)
                {
                    if (day >= 2 && Due(day, 365)) ScrapeAll(r);
                    if (Due(day, 366) || Due(day, 720) || Due(day, 1080)) WaterRound(r, null);
                    TryPotOn(r);
                    TryCompost(r);
                    if (day == 1 && Due(day, 1260))
                    {
                        r.DayDrop = MeterAfter(r, 60, MossMeter);
                        r.Tick();
                        rainRate = r.Clock.NeedRateMultiplier;
                        r.NightDrop = MeterAfter(r, 60, MossMeter);
                    }
                    if (r.Clock.MinuteOfDay >= 1200 && (day > 1 || r.Clock.MinuteOfDay >= 1385) && r.Clock.CanSleep)
                    {
                        r.Do(false, new SleepCommand(P1));
                        Assert.AreEqual(r.Db.Time.DayStartMinute, r.Clock.MinuteOfDay);
                        break;
                    }
                    r.Tick();
                }
            }
            Assert.AreEqual(0.25f, rainRate, 1e-6f, "night rate seen at 22:00");
            return r;
        }

        private static float MossMeter(Run r) => r.Containers().First(c => c.Id == r.MossTrays[0]).WaterMeter;

        private static float MeterAfter(Run r, int ticks, System.Func<Run, float> read)
        {
            var before = read(r);
            r.World.Step(ticks);
            r.Events.AddRange(r.World.DrainEvents());
            return before - read(r);
        }

        private static Run _cached;
        private static Run Cached => _cached ?? (_cached = Play());

        [Test]
        public void MossIsWateredAndScrapedDailyWithBundlesInTheCrate()
        {
            var r = Cached;
            var scraped = r.Events.OfType<MossScraped>().ToList();
            Assert.AreEqual(6 * (Days - 1), scraped.Count, "six trays scraped on each of days 2..5");
            Assert.IsTrue(scraped.Any(e => e.PlayerId.Equals(P1)) && scraped.Any(e => e.PlayerId.Equals(P2)), "both players scraped");
            var bundles = r.N.Crate.Things.Where(t => t.ItemId == "bundle_hearthmoss").ToList();
            Assert.AreEqual(6 * (Days - 1), bundles.Count, "all bundles are in the crate");
            var mosses = r.N.Plants.Values.Where(p => p.SpeciesId == "hearthmoss").ToList();
            Assert.AreEqual(6, mosses.Count);
            Assert.IsTrue(mosses.All(p => !p.IsDead && !p.IsWilted && p.TicksNotDry == p.TicksAlive), "never dry");
        }

        [Test]
        public void BlueChimebellIsPottedOnToASpecimenAndLooksBlueStriped()
        {
            var r = Cached;
            Assert.AreEqual(3, r.Stage, "potted on small, medium and large");
            Assert.AreEqual(3, r.Events.OfType<PottedOn>().Count());
            Assert.AreEqual(3, r.Blue.StageIndex, "Specimen");
            Assert.AreEqual("pot_large", r.N.FindContainerOfPlant(r.Blue.Id).Def.Id);
            Assert.AreEqual("blue", r.Blue.Genome.MendelianPhenotype("bloomColour"));
            Assert.AreEqual("striped", r.Blue.Genome.MendelianPhenotype("bellPattern"));
            Assert.IsFalse(r.Blue.IsDead);
            Assert.AreEqual(5, r.BlueTray.PlantIds.Count, "the other five plugs stay in the tray");
        }

        [Test]
        public void UnwateredTrayWiltsAndDiesOnScheduleAndIsComposted()
        {
            var r = Cached;
            var cfg = new WaterConfig();
            Assert.AreEqual(6, r.Probe.Watched.Count);
            foreach (var p in r.Probe.Watched)
            {
                Assert.AreEqual(cfg.WiltAfterHours * 60 - r.Probe.DryTicksThen[p.Id], r.Probe.Wilted[p.Id] - r.Probe.Dry[p.Id], "wilts after 12 dry hours");
                Assert.LessOrEqual(r.Probe.DryTicksThen[p.Id], 3, "dry from the moment it was sown");
                Assert.AreEqual(cfg.DieAfterWiltedDays * GameClock.MinutesPerDay, r.Probe.Dead[p.Id] - r.Probe.Wilted[p.Id], "dies after 3 wilted days");
                Assert.IsFalse(r.N.Plants.ContainsKey(p.Id), "composted");
            }
            Assert.AreEqual(6, r.Events.OfType<PlantWilted>().Count());
            Assert.AreEqual(6, r.Events.OfType<PlantDied>().Count());
            Assert.AreEqual(6, r.Events.OfType<SpeciesPlantComposted>().Count());
            Assert.IsTrue(r.Composted);
        }

        [Test]
        public void DaysEndWithSleepAndNightsDryAtAQuarterRate()
        {
            var r = Cached;
            Assert.AreEqual(0.25f, r.Db.Time.NightRateMultiplier, 1e-6f);
            Assert.AreEqual(8f, r.DayDrop, 0.01f, "tray drains 8 an hour by day");
            Assert.AreEqual(2f, r.NightDrop, 0.01f, "and a quarter of that at night");
            Assert.AreEqual(Days + 1, r.Clock.Day);
            Assert.AreEqual(r.Db.Time.DayStartMinute, r.Clock.MinuteOfDay);
            Assert.AreEqual(Days * GameClock.MinutesPerDay, r.World.Tick);
        }

        [Test]
        public void SameSlotClashIsRejectedTaken()
        {
            var r = Cached;
            Assert.AreEqual(1, r.ClashRejections);
            var taken = r.Events.OfType<CommandRejectedEvent>().Where(e => e.Result.Reason == RejectReason.Taken).ToList();
            Assert.AreEqual(1, taken.Count);
            Assert.AreEqual(2, taken[0].Command.PlayerId.Value);
        }

        [Test]
        public void WholeRunIsDeterministic()
        {
            var a = Fingerprint(Play());
            var b = Fingerprint(Play());
            Assert.IsNotEmpty(a);
            Assert.AreEqual(a, b);
            Assert.AreEqual(a, Fingerprint(Cached));
        }

        private static string Fingerprint(Run r)
        {
            var ci = CultureInfo.InvariantCulture;
            var sb = new StringBuilder();
            sb.Append("tick=").Append(r.World.Tick).Append(" money=").Append(r.N.Wallet.Balance)
              .Append(" bundles=").Append(r.N.Crate.Things.Count).Append('\n');
            foreach (var p in r.N.Plants.Values.OrderBy(p => p.Id))
                sb.Append('P').Append(p.Id).Append(' ').Append(p.VarietyId).Append(" stage=").Append(p.StageIndex)
                  .Append(" prog=").Append(p.StageProgress.ToString("R", ci)).Append(" hp=").Append(p.Health.ToString("R", ci))
                  .Append(" alive=").Append(p.TicksAlive).Append(" notdry=").Append(p.TicksNotDry)
                  .Append(" regrow=").Append(p.RegrowTicksLeft).Append(" dead=").Append(p.IsDead).Append('\n');
            foreach (var c in r.Containers().OrderBy(c => c.Id))
                sb.Append('C').Append(c.Id).Append(' ').Append(c.Def.Id).Append(" water=").Append(c.WaterMeter.ToString("R", ci))
                  .Append(" plants=").Append(string.Join(",", c.PlantIds)).Append('\n');
            foreach (var e in r.Events) sb.Append(e.GetType().Name).Append('\n');
            return sb.ToString();
        }
    }
}
