using System.Collections.Generic;
using PlantNursery.Sim.Building;
using PlantNursery.Sim.Content;
using PlantNursery.Sim.Plants;
using PlantNursery.Sim.Storage;
using UnityEngine;

namespace PlantNursery.Game
{
    /// <summary>
    /// Mirrors the sim's nursery into prefab instances (decision 0014). Every frame it reconciles against the full sim state:
    /// placed floor items (benches and the rest), things sitting on benches (planting-slot containers, working-strip items such as
    /// the watering can and carry trays), pots inside carry trays, and plants. A loaded game rebuilds without past events, and
    /// a lost event can never leave a stale view. Prefabs come from the <see cref="ViewCatalog"/>; unknown ids get the fallback.
    /// Bench slot positions are bench-local (so the bench rotation carries them) and sit at the prefab's <c>Anchor_Slot</c> height;
    /// held items and crate stock are not drawn here.
    /// </summary>
    public sealed class WorldView : MonoBehaviour
    {
        /// <summary>Name of the empty transform on a prefab whose height is where things sit on it.</summary>
        public const string SlotAnchorName = "Anchor_Slot";

        private const float Unit = 0.1f;

        [SerializeField] private SimHost host;
        [SerializeField] private ViewCatalog catalog;
        [SerializeField] private Vector3 plotOrigin = Vector3.zero;

        private sealed class ThingView
        {
            public HeldThing Thing;
            public GameObject Go;
        }

        private readonly Dictionary<int, GameObject> _floor = new Dictionary<int, GameObject>();
        private readonly Dictionary<string, ThingView> _things = new Dictionary<string, ThingView>();
        private readonly Dictionary<int, GameObject> _pots = new Dictionary<int, GameObject>();
        private readonly Dictionary<int, PlantView> _plants = new Dictionary<int, PlantView>();
        private readonly Dictionary<int, GameObject> _containerViews = new Dictionary<int, GameObject>();
        private readonly HashSet<int> _seenInts = new HashSet<int>();
        private readonly HashSet<string> _seenKeys = new HashSet<string>();

        /// <summary>Assign references (used by the scene generator and tests).</summary>
        public void Configure(SimHost simHost, ViewCatalog viewCatalog)
        {
            host = simHost; catalog = viewCatalog;
        }

        /// <summary>Number of live floor item views (benches, barrel, crate, bed...).</summary>
        public int FloorViewCount => _floor.Count;
        /// <summary>Number of live views of things in bench slots (slot containers, strip items, carry trays).</summary>
        public int SlotViewCount => _things.Count;
        /// <summary>Number of live views of pots carried in carry trays.</summary>
        public int PotViewCount => _pots.Count;
        /// <summary>Number of live plant views.</summary>
        public int PlantViewCount => _plants.Count;
        /// <summary>The view of a placed floor item, or null.</summary>
        public GameObject FloorView(int placedId) => _floor.TryGetValue(placedId, out var g) ? g : null;
        /// <summary>The view of whatever sits at a bench slot, or null.</summary>
        public GameObject SlotView(SlotAddress a) => _things.TryGetValue(a.Key, out var t) ? t.Go : null;
        /// <summary>The view of a plant, or null.</summary>
        public PlantView PlantViewOf(int plantId) => _plants.TryGetValue(plantId, out var v) ? v : null;

        private void LateUpdate() { Sync(); }

        /// <summary>Destroy every view and rebuild from the full sim state (e.g. after load).</summary>
        public void RebuildAll()
        {
            DestroyAll(_floor);
            foreach (var kv in _things) if (kv.Value.Go != null) Kill(kv.Value.Go);
            _things.Clear();
            DestroyAll(_pots);
            foreach (var kv in _plants) if (kv.Value != null) Kill(kv.Value.gameObject);
            _plants.Clear();
            Sync();
        }

        /// <summary>Reconcile views with the sim: add missing, remove stale, refresh plants.</summary>
        public void Sync()
        {
            if (host == null || !host.IsReady || host.Nursery == null) return;
            var nursery = host.Nursery;
            var content = host.Content;

            SyncFloor(nursery);
            _containerViews.Clear();
            SyncSlotThings(nursery, content);
            SyncPots(nursery, content);
            SyncPlants(nursery, content);
        }

        private void SyncFloor(Nursery nursery)
        {
            _seenInts.Clear();
            foreach (var p in nursery.Build.PlacedItems)
            {
                _seenInts.Add(p.Id);
                if (!_floor.ContainsKey(p.Id)) _floor[p.Id] = SpawnFloor(p);
            }
            Prune(_floor, _seenInts);
        }

        private void SyncSlotThings(Nursery nursery, ContentDb content)
        {
            _seenKeys.Clear();
            foreach (var bench in nursery.Build.PlacedItems)
            {
                if (!content.ItemDefs.TryGetValue(bench.ItemId, out var def) || def.Bench == null) continue;
                if (!_floor.TryGetValue(bench.Id, out var benchView) || benchView == null) continue;
                var top = AnchorHeight(benchView);

                foreach (var size in content.ContainerDefs.Values)
                {
                    var layout = SlotLayout.For(def.Bench, size);
                    for (var row = 0; row < layout.Rows; row++)
                        for (var col = 0; col < layout.Cols; col++)
                        {
                            var address = SlotAddress.Planting(bench.Id, size.Id, col, row);
                            var local = new Vector3(layout.StartU(col) * Unit, top, layout.StartV(row) * Unit);
                            Reconcile(nursery, address, benchView, local);
                        }
                }

                if (def.Bench.StripDepthUnits <= 0 || def.Bench.StripAccepts == null) continue;
                var stripStartV = def.FloorFootprint.D * 5 - def.Bench.StripDepthUnits;
                foreach (var accept in def.Bench.StripAccepts)
                {
                    var cols = nursery.Strip.Cols(def.Bench, accept.ItemId);
                    var rows = nursery.Strip.Rows(def.Bench, accept.ItemId);
                    for (var row = 0; row < rows; row++)
                        for (var col = 0; col < cols; col++)
                        {
                            var address = SlotAddress.Strip(bench.Id, accept.ItemId, col, row);
                            var local = new Vector3(col * accept.StripUnits.W * Unit, top,
                                (stripStartV + row * accept.StripUnits.D) * Unit);
                            Reconcile(nursery, address, benchView, local);
                        }
                }
            }

            var stale = new List<string>();
            foreach (var kv in _things) if (!_seenKeys.Contains(kv.Key)) stale.Add(kv.Key);
            foreach (var key in stale)
            {
                if (_things[key].Go != null) Kill(_things[key].Go);
                _things.Remove(key);
            }
        }

        private void Reconcile(Nursery nursery, SlotAddress address, GameObject benchView, Vector3 local)
        {
            var thing = nursery.Contents.Get(address);
            if (thing == null) return;
            var key = address.Key;
            _seenKeys.Add(key);
            if (_things.TryGetValue(key, out var existing))
            {
                if (existing.Thing == thing && existing.Go != null)
                {
                    if (thing.Container != null) _containerViews[thing.Container.Id] = existing.Go;
                    return;
                }
                if (existing.Go != null) Kill(existing.Go);
            }
            var go = Spawn(thing.ItemId, thing.ItemId + "_" + key);
            go.transform.SetParent(benchView.transform, false);
            go.transform.localPosition = local;
            go.transform.localRotation = Quaternion.identity;
            _things[key] = new ThingView { Thing = thing, Go = go };
            if (thing.Container != null) _containerViews[thing.Container.Id] = go;
        }

        private void SyncPots(Nursery nursery, ContentDb content)
        {
            _seenInts.Clear();
            foreach (var kv in _things)
            {
                var thing = kv.Value.Thing;
                var tray = kv.Value.Go;
                if (thing.Kind != HeldKind.CarryTray || tray == null) continue;
                var top = AnchorHeight(tray);
                for (var i = 0; i < thing.Pots.Count; i++)
                {
                    var pot = thing.Pots[i];
                    _seenInts.Add(pot.Id);
                    if (!_pots.TryGetValue(pot.Id, out var go) || go == null)
                    {
                        go = Spawn(pot.Def.ItemId, pot.Def.ItemId + "_" + pot.Id);
                        _pots[pot.Id] = go;
                    }
                    if (go.transform.parent != tray.transform) go.transform.SetParent(tray.transform, false);
                    go.transform.localPosition = new Vector3((0.5f + i) * 2f * Unit, top, 0.2f);
                    go.transform.localRotation = Quaternion.identity;
                    _containerViews[pot.Id] = go;
                }
            }
            Prune(_pots, _seenInts);
        }

        private void SyncPlants(Nursery nursery, ContentDb content)
        {
            _seenInts.Clear();
            foreach (var kv in _containerViews)
            {
                var container = nursery.FindContainer(kv.Key);
                if (container == null || kv.Value == null) continue;
                var parent = kv.Value;
                var top = AnchorHeight(parent);
                foreach (var plantId in container.PlantIds)
                {
                    var plant = nursery.PlantById(plantId);
                    if (plant == null) continue;
                    _seenInts.Add(plant.Id);
                    if (_plants.TryGetValue(plant.Id, out var existing) && existing != null)
                    {
                        if (existing.transform.parent != parent.transform) existing.transform.SetParent(parent.transform, false);
                        existing.transform.localPosition = PlantPosition(plant, container.Def, top);
                        existing.Refresh();
                    }
                    else
                    {
                        var go = new GameObject("Plant_" + plant.Id);
                        go.transform.SetParent(parent.transform, false);
                        go.transform.localPosition = PlantPosition(plant, container.Def, top);
                        var view = go.AddComponent<PlantView>();
                        view.Bind(plant);
                        _plants[plant.Id] = view;
                    }
                }
            }
            var stale = new List<int>();
            foreach (var kv in _plants) if (!_seenInts.Contains(kv.Key)) stale.Add(kv.Key);
            foreach (var id in stale)
            {
                if (_plants[id] != null) Kill(_plants[id].gameObject);
                _plants.Remove(id);
            }
        }

        private static Vector3 PlantPosition(Plant plant, ContainerDef def, float top)
            => WorldLayout.CellCentre(plant.CellIndex, def.CellCount, def.AlongUnits, def.AcrossUnits) + new Vector3(0f, top, 0f);

        private GameObject Spawn(string definitionId, string objectName)
        {
            var prefab = catalog != null ? catalog.PrefabFor(definitionId) : null;
            GameObject go;
            if (prefab != null) go = Instantiate(prefab);
            else
            {
                go = new GameObject();
                var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cube.transform.SetParent(go.transform, false);
                cube.transform.localScale = Vector3.one * 0.2f;
                cube.transform.localPosition = Vector3.one * 0.1f;
            }
            go.name = objectName;
            go.transform.SetParent(transform, false);
            return go;
        }

        private GameObject SpawnFloor(PlacedItem p)
        {
            var go = Spawn(p.ItemId, p.ItemId + "_" + p.Id);
            go.transform.SetPositionAndRotation(WorldLayout.FloorPosition(p, plotOrigin), Quaternion.Euler(0f, WorldLayout.Yaw(p.Rotation), 0f));
            return go;
        }

        /// <summary>Height (in the view's local space) of its Anchor_Slot; if the prefab has none, the top of its renderers.</summary>
        private static float AnchorHeight(GameObject view)
        {
            var anchor = FindAnchor(view.transform);
            if (anchor != null) return view.transform.InverseTransformPoint(anchor.position).y;
            var top = 0f;
            foreach (var r in view.GetComponentsInChildren<Renderer>())
            {
                if (r.GetComponentInParent<PlantView>() != null) continue;
                top = Mathf.Max(top, view.transform.InverseTransformPoint(r.bounds.max).y);
            }
            return top;
        }

        private static Transform FindAnchor(Transform root)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>())
                if (t.name == SlotAnchorName) return t;
            return null;
        }

        private static void Prune(Dictionary<int, GameObject> views, HashSet<int> keep)
        {
            var stale = new List<int>();
            foreach (var kv in views) if (!keep.Contains(kv.Key)) stale.Add(kv.Key);
            foreach (var id in stale)
            {
                if (views[id] != null) Kill(views[id]);
                views.Remove(id);
            }
        }

        private static void DestroyAll(Dictionary<int, GameObject> views)
        {
            foreach (var kv in views) if (kv.Value != null) Kill(kv.Value);
            views.Clear();
        }

        private static void Kill(GameObject go)
        {
            if (Application.isPlaying) Destroy(go); else DestroyImmediate(go);
        }
    }
}
