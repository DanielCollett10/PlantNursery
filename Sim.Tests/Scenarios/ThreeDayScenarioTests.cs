using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using PlantNursery.Sim.Building;
using PlantNursery.Sim.Content;
using PlantNursery.Sim.Core;
using PlantNursery.Sim.Economy;
using PlantNursery.Sim.Hands;
using PlantNursery.Sim.Plants;
using PlantNursery.Sim.Time;

namespace PlantNursery.Sim.Tests
{
    /// <summary>
    /// End-to-end headless scenarios for the M1 loop, using the real content files with the demo time profile.
    /// Everything goes through commands except putting trays and the can on the bench (surface placement of
    /// inventory items has no command yet).
    /// </summary>
    public class ThreeDayScenarioTests
    {
        private static readonly PlayerId P = new PlayerId(1);

        /// <summary>One fully wired nursery over the real content.</summary>
        private sealed class Nursery
        {
            public ContentDb Db;
            public SimWorld World;
            public GameClock Clock;
            public BuildState Build;
            public SurfaceGrid Surface;
            public Wallet Wallet;
            public HandsState Hands;
            public readonly List<ISimEvent> Events = new List<ISimEvent>();

            public static Nursery Create()
            {
                var n = new Nursery { Db = LoadRealContentWithDemo() };
                n.World = new SimWorld();
                n.Clock = GameClock.Install(n.World, n.Db.Time);
                n.Build = new BuildState(n.Db);
                n.Surface = new SurfaceGrid(n.Db, n.Build);
                n.Build.PlaceStarterLayout();
                n.Wallet = new Wallet(n.Db.Economy.StartingMoney);
                n.Hands = new HandsState(n.Db, n.Surface);
                SupplierModule.Register(n.World, n.Db, n.Wallet, n.Build, n.Surface);
                BuildingModule.Register(n.World, n.Build);
                HandsModule.Register(n.World, n.Hands);
                GrowthSystem.Install(n.World, n.Db, n.Hands, n.Clock);
                SoftLockGuard.Install(n.World, n.Db, n.Wallet, n.Build, n.Surface, n.Hands, n.Clock);
                return n;
            }

            /// <summary>Submit commands, run one tick, keep the events and fail on any rejection.</summary>
            public void Do(params ICommand[] commands)
            {
                foreach (var c in commands) World.Submit(c);
                World.Step(1);
                Collect();
                var rejected = Events.OfType<CommandRejectedEvent>().ToList();
                Assert.IsEmpty(rejected, "a command was rejected");
            }

            public void Step(int ticks)
            {
                World.Step(ticks);
                Collect();
            }

            public void Collect() => Events.AddRange(World.DrainEvents());

            public void StepToEvening()
            {
                while (Clock.Phase != DayPhase.Evening) Step(1);
            }

            public void Sleep()
            {
                Do(new SleepCommand(P));
                Assert.AreEqual(0, Clock.MinuteOfDay - Db.Time.DayStartMinute, "sleep ends at day start");
            }

            public SurfaceItem Find(string itemId) => Surface.Items.First(i => i.ItemId == itemId);

            public List<SurfaceItem> FindAll(string itemId) => Surface.Items.Where(i => i.ItemId == itemId).OrderBy(i => i.Id).ToList();
        }

        private static ContentDb LoadRealContentWithDemo()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            string found = null;
            while (dir != null && found == null)
            {
                var c = Path.Combine(dir.FullName, "Assets", "StreamingAssets", "Content");
                if (Directory.Exists(c)) found = c;
                dir = dir.Parent;
            }
            Assert.IsNotNull(found, "content folder not found");
            var files = new Dictionary<string, string>();
            foreach (var p in Directory.GetFiles(found, "*.json"))
                files[Path.GetFileName(p)] = File.ReadAllText(p); // includes time.demo.json
            return ContentLoader.Load(files);
        }

        // Surface unit anchors on a bench at cell (5,1): units u 25..29, v 5..24.
        private static readonly Unit TrayAAnchor = new Unit(25, 5);
        private static readonly Unit CanAnchor = new Unit(25, 9);
        private static readonly Unit PacketAnchor = new Unit(25, 13);
        private static readonly Unit Packet2Anchor = new Unit(27, 13);
        private static readonly Unit TrayBAnchor = new Unit(25, 16);

        private static void Sow6(Nursery n, int packetId, int trayId)
        {
            var cmds = new List<ICommand> { new PickUp(P, packetId) };
            for (var i = 0; i < 6; i++) cmds.Add(new Sow(P, trayId, i));
            n.Do(cmds.ToArray());
        }

        private static void Water(Nursery n, int trayId)
        {
            var plants = n.Hands.GetContents(n.Surface.Items.First(i => i.Id == trayId)).Plants.Select(p => p.Id).ToList();
            var cmds = new List<ICommand> { new PickUp(P, n.Find("watering_can").Id), new RefillCan(P) };
            cmds.AddRange(plants.Select(id => (ICommand)new WaterPlant(P, id)));
            cmds.Add(new PutDown(P, CanAnchor, Rotation.Deg0));
            n.Do(cmds.ToArray());
        }

        /// <summary>
        /// Day 1 setup then three days of watering tray A morning and evening and never tray B, sleeping each night.
        /// Calls <paramref name="afterDay"/> with the day just finished (1..3) once the player has slept.
        /// </summary>
        private static Nursery RunThreeDays(Action<Nursery, int> afterDay = null)
        {
            var n = Nursery.Create();
            n.Do(new BuyItem(P, "seed_packet", 2));
            n.Do(new PlaceItem(P, "bench_standard", ItemLayer.Floor, new Cell(5, 1), Rotation.Deg0));

            var trayA = n.Surface.Place("plug_tray", TrayAAnchor, Rotation.Deg0, out var rej);
            Assert.AreEqual(BuildRejection.None, rej);
            var trayB = n.Surface.Place("plug_tray", TrayBAnchor, Rotation.Deg0, out rej);
            Assert.AreEqual(BuildRejection.None, rej);
            n.Surface.Place("watering_can", CanAnchor, Rotation.Deg0, out rej);
            Assert.AreEqual(BuildRejection.None, rej);

            var packets = n.FindAll("seed_packet");
            Assert.AreEqual(2, packets.Count);
            Sow6(n, packets[0].Id, trayA.Id);
            n.Do(new PutDown(P, PacketAnchor, Rotation.Deg0));
            Sow6(n, packets[1].Id, trayB.Id);
            n.Do(new PutDown(P, Packet2Anchor, Rotation.Deg0));

            for (var day = 1; day <= 3; day++)
            {
                Water(n, trayA.Id); // tray B is deliberately never watered
                n.StepToEvening();
                Water(n, trayA.Id);
                n.Sleep();
                afterDay?.Invoke(n, day);
            }
            return n;
        }

        private static List<Plant> PlantsOf(Nursery n, string trayItemId, int index)
        {
            var tray = n.FindAll(trayItemId)[index];
            return n.Hands.GetContents(tray).Plants.OrderBy(p => p.CellIndex).ToList();
        }

        [Test]
        public void ThreeDays_WateredTrayGrowsToRootedAndUnwateredTrayWilts()
        {
            var sw = Stopwatch.StartNew();
            var stagesByDay = new Dictionary<int, PlantStage[]>();
            var n = RunThreeDays((nursery, day) =>
            {
                var a = PlantsOf(nursery, "plug_tray", 0);
                var b = PlantsOf(nursery, "plug_tray", 1);
                Assert.AreEqual(6, a.Count);
                Assert.AreEqual(6, b.Count);
                stagesByDay[day] = a.Select(p => p.Stage).ToArray();

                // The unwatered tray never gets past Seedling and loses health once it dries out.
                foreach (var p in b)
                {
                    Assert.LessOrEqual(p.Stage, PlantStage.Seedling, "unwatered plant grew too far on day " + day);
                    Assert.AreEqual(0f, p.Moisture, 0.001f);
                    Assert.Less(p.Health, 100f, "unwatered plant should wilt on day " + day);
                }
            });

            Assert.AreEqual(4, n.Clock.Day);
            Assert.AreEqual(3, n.Db.Time.SeasonLengthDays, "demo profile is loaded");
            Assert.AreEqual(Season.Summer, n.Clock.Season, "three spring days then summer begins");

            // Watered tray: Seedling after day 1, Rooted after day 2, and it stops there in the plug tray.
            Assert.IsTrue(stagesByDay[1].All(s => s >= PlantStage.Seedling));
            Assert.IsTrue(stagesByDay[1].All(s => s < PlantStage.Rooted));
            Assert.IsTrue(stagesByDay[2].All(s => s == PlantStage.Rooted));
            var variety = n.Db.VarietyDefs.Values.First();
            foreach (var p in PlantsOf(n, "plug_tray", 0))
            {
                Assert.AreEqual(PlantStage.Rooted, p.Stage);
                Assert.LessOrEqual(p.Growth, variety.StageThresholds[(int)PlantStage.Rooted] + 0.001f, "growth is capped at Rooted");
                Assert.IsFalse(p.IsDead);
                Assert.Greater(p.RootBoundMinutes, 0);
            }
            Assert.IsTrue(n.Events.OfType<PlantStageChanged>().Any(e => e.To == PlantStage.Rooted));
            Assert.IsFalse(n.Events.OfType<PlantStageChanged>().Any(e => e.To > PlantStage.Rooted), "nothing outgrows the plug tray");

            // Unwatered tray ends the three days dead.
            Assert.IsTrue(PlantsOf(n, "plug_tray", 1).All(p => p.IsDead));
            Assert.AreEqual(6, n.Events.OfType<PlantDied>().Count());
            Assert.AreEqual(200 - 2 * 10, n.Wallet.Balance);
            Assert.Less(sw.Elapsed.TotalSeconds, 2.0, "scenario must stay fast");
        }

        [Test]
        public void SameCommandScriptTwiceGivesIdenticalState()
        {
            var a = Fingerprint(RunThreeDays());
            var b = Fingerprint(RunThreeDays());
            Assert.AreEqual(a, b);
            Assert.IsNotEmpty(a);
        }

        [Test]
        public void GoBroke_DeadPlantsComposted_NextMorningNeighbourGift()
        {
            var sw = Stopwatch.StartNew();
            var n = Nursery.Create();
            n.Do(new BuyItem(P, "seed_packet", 1)); // 10
            n.Do(new PlaceItem(P, "bench_standard", ItemLayer.Floor, new Cell(5, 1), Rotation.Deg0));
            var tray = n.Surface.Place("plug_tray", TrayAAnchor, Rotation.Deg0, out _);
            Sow6(n, n.Find("seed_packet").Id, tray.Id);
            n.Do(new PutDown(P, PacketAnchor, Rotation.Deg0)); // empty packet, no longer blocks the gift

            // Spend the other 190: four benches (160) and two trays (30).
            n.Do(new BuyItem(P, "bench_standard", 4), new BuyItem(P, "plug_tray", 2));
            Assert.AreEqual(0, n.Wallet.Balance);

            // Never water. Run until every plant is dead (bounded to five days).
            var plants = PlantsOf(n, "plug_tray", 0);
            var limit = 5 * GameClock.MinutesPerDay;
            while (plants.Any(p => !p.IsDead) && n.World.Tick < limit) n.Step(60);
            Assert.IsTrue(plants.All(p => p.IsDead), "plants should starve to death");
            Assert.AreEqual(0, n.Events.OfType<NeighbourGift>().Count(), "no gift while plants were alive");
            Assert.AreEqual(0, n.Events.OfType<CommandRejectedEvent>().Count());

            n.Do(plants.Select(p => (ICommand)new Compost(P, p.Id)).ToArray());
            Assert.AreEqual(0, PlantsOf(n, "plug_tray", 0).Count);
            Assert.AreEqual(6, n.Events.OfType<PlantComposted>().Count());

            n.StepToEvening();
            n.Sleep();

            var gift = n.Events.OfType<NeighbourGift>().Single();
            Assert.AreEqual("seed_packet", gift.ItemId);
            var packet = n.Surface.Items.Single(i => i.Id == gift.SurfaceItemId);
            Assert.AreEqual(6, n.Hands.GetContents(packet).Seeds);
            Assert.AreSame(DeliveryShelf.Find(n.Build), n.Build.GetAt(SurfaceGrid.CellOf(packet.Anchor)), "gift lands on the delivery shelf");
            Assert.AreEqual(0, n.Wallet.Balance);
            Assert.Less(sw.Elapsed.TotalSeconds, 2.0, "scenario must stay fast");
        }

        private static string Fingerprint(Nursery n)
        {
            var ci = CultureInfo.InvariantCulture;
            var sb = new StringBuilder();
            sb.Append("tick=").Append(n.World.Tick).Append(";money=").Append(n.Wallet.Balance).Append('\n');
            foreach (var item in n.Surface.Items.OrderBy(i => i.Id))
            {
                var c = n.Hands.GetContents(item);
                sb.Append("S").Append(item.Id).Append(' ').Append(item.ItemId).Append(item.Anchor)
                  .Append(" seeds=").Append(c.Seeds).Append(" water=").Append(c.Water).Append('\n');
                foreach (var p in c.Plants.OrderBy(p => p.Id))
                    sb.Append("  P").Append(p.Id).Append(' ').Append(p.VarietyId).Append(" cell=").Append(p.CellIndex)
                      .Append(" stage=").Append(p.Stage).Append(" g=").Append(p.Growth.ToString("R", ci))
                      .Append(" m=").Append(p.Moisture.ToString("R", ci)).Append(" h=").Append(p.Health.ToString("R", ci))
                      .Append(" rb=").Append(p.RootBoundMinutes).Append(" gc=").Append(p.GoodCareMinutes)
                      .Append(" dead=").Append(p.IsDead).Append('\n');
            }
            foreach (var pl in n.Build.PlacedItems.OrderBy(i => i.Id))
                sb.Append("B").Append(pl.Id).Append(' ').Append(pl.ItemId).Append(pl.Anchor.X).Append(',').Append(pl.Anchor.Z).Append('\n');
            foreach (var e in n.Events) sb.Append(e.GetType().Name).Append('\n');
            return sb.ToString();
        }
    }
}
