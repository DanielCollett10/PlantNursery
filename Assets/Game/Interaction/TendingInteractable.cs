using System.Collections.Generic;
using PlantNursery.Sim.Building;
using PlantNursery.Sim.Content;
using PlantNursery.Sim.Core;
using PlantNursery.Sim.Plants;
using PlantNursery.Sim.Storage;
using PlantNursery.Sim.Targeting;
using UnityEngine;

namespace PlantNursery.Game
{
    /// <summary>
    /// Walking-around tending as one <see cref="IInteractable"/> (controls.md, Targeting). Unity only gathers what is in reach and
    /// measures distances; the Sim picks the water and scrape targets (<see cref="SmartTarget"/>) and decides every outcome.
    /// With empty hands: scrape moss or take a bench strip item, whichever is nearer. Holding the can: refill at the rain barrel,
    /// else water the driest container, else put it down on a free strip slot, else return it to the crate. Holding anything else:
    /// the nearer of a free strip slot (PutInSlot) and the crate (ReturnToCrate).
    /// </summary>
    public sealed class TendingInteractable : MonoBehaviour, IInteractable
    {
        private const float Unit = WorldLayout.UnitSize;
        private const string CrateItemId = "storage_crate";

        private struct Spot
        {
            public SlotAddress Address;
            public Vector3 Centre;
        }

        private struct Plan
        {
            public string PromptKey;
            public float Distance;
            public ICommand Command;
        }

        [SerializeField] private SimHost host;
        [SerializeField] private Transform player;
        [SerializeField] private Vector3 plotOrigin = Vector3.zero;

        private WaterTargetStrategy _water;
        private ScrapeTargetStrategy _scrape;

        /// <summary>Assign references (scene generator and tests).</summary>
        public void Configure(SimHost simHost, Transform playerTransform)
        {
            host = simHost; player = playerTransform;
        }

        private void OnEnable() { WorldInteractions.Register(this); }
        private void OnDisable() { WorldInteractions.Unregister(this); }

        /// <inheritdoc />
        public bool IsAvailable => isActiveAndEnabled && host != null && host.IsReady && host.Nursery != null && player != null;

        /// <inheritdoc />
        public string PromptKey => Decide(player.position).PromptKey ?? "prompt.tend";

        /// <inheritdoc />
        public float DistanceTo(Vector3 point)
        {
            var plan = Decide(point);
            return plan.Command == null ? float.MaxValue : plan.Distance;
        }

        /// <inheritdoc />
        public CommandResult Interact(SimHost simHost)
        {
            var h = simHost != null ? simHost : host;
            var plan = Decide(player.position);
            if (plan.Command == null) return CommandResult.Rejected(RejectReason.InvalidTarget, "NothingToDo");
            return h.Submit(plan.Command);
        }

        private Plan Decide(Vector3 pos)
        {
            var n = host.Nursery;
            var me = host.Session.LocalPlayer;
            var held = n.Hands.GetHeld(me);

            if (held == null)
            {
                var scrape = PickContainer(pos, _scrape ?? (_scrape = new ScrapeTargetStrategy(host.Content, n.PlantById)), null);
                var pick = NearestSpot(pos, true, a => n.Contents.Get(a) != null, a => host.Validate(new PickUpFromSlot(me, a)).IsAccepted);
                var scrapePlan = scrape == null ? None : new Plan
                {
                    PromptKey = "prompt.scrape", Distance = (float)scrape.Distance, Command = new ScrapeMoss(me, scrape.EntityId)
                };
                var pickPlan = pick == null ? None : new Plan
                {
                    PromptKey = "prompt.pickup", Distance = Dist(pos, pick.Value.Centre), Command = new PickUpFromSlot(me, pick.Value.Address)
                };
                return Nearer(pickPlan, scrapePlan);
            }

            var crate = CratePlan(pos, me);
            var put = PutPlan(pos, me, held);
            if (held.Kind != HeldKind.Can) return Nearer(put, crate);

            var barrel = BarrelPlan(pos, me, held);
            if (barrel.Command != null) return barrel;
            var target = PickContainer(pos, _water ?? (_water = new WaterTargetStrategy()), c => c.WaterMeter < 100f);
            if (target != null)
                return new Plan { PromptKey = "prompt.water", Distance = (float)target.Distance, Command = new WaterContainer(me, target.EntityId) };
            return Nearer(put, crate);
        }

        private static readonly Plan None = new Plan { Distance = float.MaxValue };

        private static Plan Nearer(Plan a, Plan b)
        {
            if (a.Command == null) return b;
            if (b.Command == null) return a;
            return b.Distance < a.Distance ? b : a;
        }

        private Plan CratePlan(Vector3 pos, PlayerId me)
        {
            var d = FloorDistance(pos, CrateItemId, out _);
            return d > WorldInteractions.Reach ? None
                : new Plan { PromptKey = "prompt.crate", Distance = d, Command = new ReturnToCrate(me) };
        }

        private Plan BarrelPlan(Vector3 pos, PlayerId me, HeldThing can)
        {
            var d = FloorDistance(pos, RefillCan.RainBarrelItemId, out var barrel);
            if (barrel == null || d > WorldInteractions.Reach) return None;
            var capacity = host.Content.ItemDefs.TryGetValue(can.ItemId, out var def) ? def.WaterCapacity : 0;
            if (can.Charges >= capacity) return None;
            return new Plan { PromptKey = "prompt.refill", Distance = d, Command = new RefillCan(me, barrel.Id) };
        }

        private Plan PutPlan(Vector3 pos, PlayerId me, HeldThing held)
        {
            var spot = NearestSpot(pos, true, a => a.Slot.Size == held.ItemId && IsFree(a), a => host.Validate(new PutInSlot(me, a)).IsAccepted);
            return spot == null ? None
                : new Plan { PromptKey = "prompt.putdown", Distance = Dist(pos, spot.Value.Centre), Command = new PutInSlot(me, spot.Value.Address) };
        }

        private bool IsFree(SlotAddress a) => host.Nursery.Contents.Get(a) == null;

        /// <summary>Gather containers in bench planting slots within reach and let the Sim choose one.</summary>
        private TargetCandidate PickContainer(Vector3 pos, ISmartTargetStrategy strategy, System.Func<Container, bool> include)
        {
            var candidates = new List<TargetCandidate>();
            foreach (var spot in Spots(false))
            {
                var thing = host.Nursery.Contents.Get(spot.Address);
                if (thing == null || thing.Container == null) continue;
                if (include != null && !include(thing.Container)) continue;
                var d = Dist(pos, spot.Centre);
                if (d <= WorldInteractions.Reach) candidates.Add(TargetCandidate.ForContainer(thing.Container, d));
            }
            return SmartTarget.Pick(strategy, host.Session.LocalPlayer, candidates, host.World);
        }

        private Spot? NearestSpot(Vector3 pos, bool strip, System.Func<SlotAddress, bool> wanted, System.Func<SlotAddress, bool> accepted)
        {
            Spot? best = null;
            var bestD = WorldInteractions.Reach;
            foreach (var s in Spots(strip))
            {
                var d = Dist(pos, s.Centre);
                if (d > bestD || !wanted(s.Address) || !accepted(s.Address)) continue;
                best = s; bestD = d;
            }
            return best;
        }

        /// <summary>World-space centres of every strip slot (or every planting slot) on every bench.</summary>
        private IEnumerable<Spot> Spots(bool strip)
        {
            var content = host.Content;
            var nursery = host.Nursery;
            foreach (var bench in nursery.Build.PlacedItems)
            {
                if (!content.ItemDefs.TryGetValue(bench.ItemId, out var def) || def.Bench == null) continue;
                var origin = WorldLayout.FloorPosition(bench, plotOrigin);
                var rot = Quaternion.Euler(0f, WorldLayout.Yaw(bench.Rotation), 0f);

                if (strip)
                {
                    if (def.Bench.StripDepthUnits <= 0 || def.Bench.StripAccepts == null) continue;
                    var stripStartV = def.FloorFootprint.D * 5 - def.Bench.StripDepthUnits;
                    foreach (var accept in def.Bench.StripAccepts)
                    {
                        var cols = nursery.Strip.Cols(def.Bench, accept.ItemId);
                        var rows = nursery.Strip.Rows(def.Bench, accept.ItemId);
                        for (var row = 0; row < rows; row++)
                            for (var col = 0; col < cols; col++)
                            {
                                var local = new Vector3((col + 0.5f) * accept.StripUnits.W * Unit, 0f,
                                    (stripStartV + (row + 0.5f) * accept.StripUnits.D) * Unit);
                                yield return new Spot { Address = SlotAddress.Strip(bench.Id, accept.ItemId, col, row), Centre = origin + rot * local };
                            }
                    }
                    continue;
                }

                foreach (var size in content.ContainerDefs.Values)
                {
                    var layout = SlotLayout.For(def.Bench, size);
                    for (var row = 0; row < layout.Rows; row++)
                        for (var col = 0; col < layout.Cols; col++)
                        {
                            var local = new Vector3((layout.StartU(col) + layout.AlongUnits * 0.5f) * Unit, 0f,
                                (layout.StartV(row) + layout.AcrossUnits * 0.5f) * Unit);
                            yield return new Spot { Address = SlotAddress.Planting(bench.Id, size.Id, col, row), Centre = origin + rot * local };
                        }
                }
            }
        }

        /// <summary>Horizontal distance to the nearest placed floor item with this id (its footprint rectangle).</summary>
        private float FloorDistance(Vector3 point, string itemId, out PlacedItem nearest)
        {
            nearest = null;
            var best = float.MaxValue;
            foreach (var p in host.Nursery.Build.PlacedItems)
            {
                if (p.ItemId != itemId) continue;
                var min = plotOrigin + new Vector3(p.Anchor.X * WorldLayout.CellSize, 0f, p.Anchor.Z * WorldLayout.CellSize);
                var max = min + new Vector3(p.Width * WorldLayout.CellSize, 0f, p.Depth * WorldLayout.CellSize);
                var dx = Mathf.Max(min.x - point.x, 0f, point.x - max.x);
                var dz = Mathf.Max(min.z - point.z, 0f, point.z - max.z);
                var d = Mathf.Sqrt(dx * dx + dz * dz);
                if (d < best) { best = d; nearest = p; }
            }
            return best;
        }

        private static float Dist(Vector3 a, Vector3 b)
        {
            var dx = a.x - b.x;
            var dz = a.z - b.z;
            return Mathf.Sqrt(dx * dx + dz * dz);
        }
    }
}
