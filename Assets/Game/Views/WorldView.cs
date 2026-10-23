using System.Collections.Generic;
using PlantNursery.Sim.Building;
using PlantNursery.Sim.Content;
using PlantNursery.Sim.Hands;
using PlantNursery.Sim.Plants;
using UnityEngine;

namespace PlantNursery.Game
{
    /// <summary>
    /// Mirrors the sim's placed items, surface items and plants into prefab instances. It reconciles against
    /// the full sim state every frame, so a loaded game rebuilds without needing past events, and items that
    /// appear without a player command (deliveries, the neighbour's gift) show up too.
    /// Floor items sit at cell x 0.5 m; surface items at unit x 0.1 m on top of the supporting item.
    /// </summary>
    public sealed class WorldView : MonoBehaviour
    {
        [SerializeField] private SimHost host;
        [SerializeField] private List<PlaceableAuthoring> prefabs = new List<PlaceableAuthoring>();
        [SerializeField] private Vector3 plotOrigin = Vector3.zero;

        private readonly Dictionary<int, GameObject> _floor = new Dictionary<int, GameObject>();
        private readonly Dictionary<int, GameObject> _surface = new Dictionary<int, GameObject>();
        private readonly Dictionary<int, PlantView> _plants = new Dictionary<int, PlantView>();
        private readonly Dictionary<string, PlaceableAuthoring> _prefabById = new Dictionary<string, PlaceableAuthoring>();
        private readonly HashSet<int> _seen = new HashSet<int>();

        /// <summary>Assign references (used by the scene generator).</summary>
        public void Configure(SimHost simHost, List<PlaceableAuthoring> placeablePrefabs)
        {
            host = simHost; prefabs = placeablePrefabs; _prefabById.Clear();
        }

        /// <summary>Number of live floor item views.</summary>
        public int FloorViewCount => _floor.Count;
        /// <summary>Number of live surface item views.</summary>
        public int SurfaceViewCount => _surface.Count;
        /// <summary>Number of live plant views.</summary>
        public int PlantViewCount => _plants.Count;
        /// <summary>The view of a placed floor item, or null.</summary>
        public GameObject FloorView(int placedId) => _floor.TryGetValue(placedId, out var g) ? g : null;
        /// <summary>The view of a surface item, or null.</summary>
        public GameObject SurfaceView(int surfaceId) => _surface.TryGetValue(surfaceId, out var g) ? g : null;
        /// <summary>The view of a plant, or null.</summary>
        public PlantView PlantViewOf(int plantId) => _plants.TryGetValue(plantId, out var v) ? v : null;

        private void LateUpdate() { Sync(); }

        /// <summary>Destroy every view and rebuild from the full sim state (e.g. after load).</summary>
        public void RebuildAll()
        {
            DestroyAll(_floor); DestroyAll(_surface);
            foreach (var kv in _plants) if (kv.Value != null) Kill(kv.Value.gameObject);
            _plants.Clear();
            Sync();
        }

        /// <summary>Reconcile views with the sim: add missing, remove stale, refresh plants.</summary>
        public void Sync()
        {
            if (host == null || !host.IsReady) return;
            if (_prefabById.Count == 0) foreach (var p in prefabs) if (p != null) _prefabById[p.ItemId] = p;

            _seen.Clear();
            foreach (var p in host.Build.PlacedItems)
            {
                _seen.Add(p.Id);
                if (!_floor.ContainsKey(p.Id)) _floor[p.Id] = SpawnFloor(p);
            }
            Prune(_floor, _seen);

            _seen.Clear();
            foreach (var s in host.Surface.Items)
            {
                _seen.Add(s.Id);
                if (_surface.ContainsKey(s.Id)) continue;
                var g = SpawnSurface(s);
                if (g != null) _surface[s.Id] = g;
            }
            Prune(_surface, _seen);

            _seen.Clear();
            foreach (var s in host.Surface.Items)
            {
                if (!_surface.TryGetValue(s.Id, out var parent)) continue;
                foreach (var plant in host.Hands.GetContents(s).Plants)
                {
                    _seen.Add(plant.Id);
                    if (_plants.TryGetValue(plant.Id, out var existing) && existing != null)
                    {
                        if (existing.transform.parent != parent.transform) existing.transform.SetParent(parent.transform, false);
                        existing.Refresh();
                    }
                    else _plants[plant.Id] = SpawnPlant(plant, s, parent);
                }
            }
            var stale = new List<int>();
            foreach (var kv in _plants) if (!_seen.Contains(kv.Key)) stale.Add(kv.Key);
            foreach (var id in stale)
            {
                if (_plants[id] != null) Kill(_plants[id].gameObject);
                _plants.Remove(id);
            }
        }

        private GameObject Spawn(string itemId, string objectName)
        {
            GameObject go;
            if (_prefabById.TryGetValue(itemId, out var prefab)) go = Instantiate(prefab.gameObject);
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

        private GameObject SpawnSurface(SurfaceItem s)
        {
            var support = host.Build.GetAt(SurfaceGrid.CellOf(s.Anchor));
            if (support == null || !_floor.TryGetValue(support.Id, out var supportView)) return null;
            var go = Spawn(s.ItemId, s.ItemId + "_" + s.Id);
            go.transform.SetPositionAndRotation(
                WorldLayout.SurfacePosition(s, plotOrigin, TopOf(supportView)),
                Quaternion.Euler(0f, WorldLayout.Yaw(s.Rotation), 0f));
            var target = go.GetComponent<InteractableTarget>();
            if (target == null) target = go.AddComponent<InteractableTarget>();
            target.Configure(TargetKind.SurfaceItem, s.Id);
            return go;
        }

        private PlantView SpawnPlant(Plant plant, SurfaceItem s, GameObject parent)
        {
            var def = host.Content.ItemDefs[s.ItemId];
            var count = 1;
            foreach (var c in host.Content.ContainerDefs.Values) if (c.ItemId == s.ItemId) { count = c.CellCount; break; }
            var go = new GameObject("Plant_" + plant.Id);
            go.transform.SetParent(parent.transform, false);
            go.transform.localPosition = WorldLayout.CellCentre(plant.CellIndex, count, def.SurfaceFootprint.W, def.SurfaceFootprint.D)
                + new Vector3(0f, ContainerTop(parent), 0f);
            var box = go.AddComponent<BoxCollider>();
            box.size = new Vector3(0.05f, 0.15f, 0.05f);
            box.center = new Vector3(0f, 0.075f, 0f);
            go.AddComponent<InteractableTarget>().Configure(TargetKind.SurfaceItem, s.Id, plant.CellIndex);
            var view = go.AddComponent<PlantView>();
            view.Bind(plant);
            return view;
        }

        private static float ContainerTop(GameObject container)
        {
            var top = 0f;
            foreach (var r in container.GetComponentsInChildren<Renderer>())
            {
                if (r.GetComponentInParent<PlantView>() != null) continue; // ignore plants already on it
                top = Mathf.Max(top, r.bounds.max.y - container.transform.position.y);
            }
            return top;
        }

        private static float TopOf(GameObject view)
        {
            var top = view.transform.position.y;
            foreach (var r in view.GetComponentsInChildren<Renderer>()) top = Mathf.Max(top, r.bounds.max.y);
            return top;
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
