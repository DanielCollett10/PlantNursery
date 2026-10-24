using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using PlantNursery.Sim.Storage;
using UnityEngine;

namespace PlantNursery.Game.Tests
{
    /// <summary>WorldView creates and destroys views to match sim entities, using the ViewCatalog (T-151).</summary>
    public class WorldViewReconcileTests
    {
        private GameObject _hostGo, _viewGo, _fallback, _known;
        private SimHost _host;
        private WorldView _view;
        private ViewCatalog _catalog;

        [SetUp]
        public void SetUp()
        {
            _hostGo = new GameObject("SimHost");
            _host = _hostGo.AddComponent<SimHost>();
            _host.Initialize();

            _fallback = new GameObject("FallbackPrefab");
            _known = new GameObject("KnownPrefab");
            new GameObject("FallbackMarker").transform.SetParent(_fallback.transform, false);
            new GameObject("KnownMarker").transform.SetParent(_known.transform, false);
            _catalog = ScriptableObject.CreateInstance<ViewCatalog>();
            _catalog.Configure(new[] { new ViewCatalog.Entry { id = "bench_3m", prefab = _known } }, _fallback);

            _viewGo = new GameObject("WorldView");
            _view = _viewGo.AddComponent<WorldView>();
            _view.Configure(_host, _catalog);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_viewGo);
            Object.DestroyImmediate(_hostGo);
            Object.DestroyImmediate(_fallback);
            Object.DestroyImmediate(_known);
            Object.DestroyImmediate(_catalog);
        }

        private int ExpectedSlotThings() => _host.Nursery.Contents.Things.Count();
        private int ExpectedPots() => _host.Nursery.Contents.Things.Sum(t => t.Pots.Count);

        private int ExpectedPlants()
        {
            // Plants in containers that sit in a slot or in a carried tray on a bench.
            var n = 0;
            foreach (var t in _host.Nursery.Contents.Things)
            {
                if (t.Container != null) n += t.Container.PlantIds.Count;
                foreach (var p in t.Pots) n += p.PlantIds.Count;
            }
            return n;
        }

        [Test]
        public void CatalogReturnsEntryOrFallback()
        {
            Assert.AreSame(_known, _catalog.PrefabFor("bench_3m"));
            Assert.AreSame(_fallback, _catalog.PrefabFor("no_such_item"));
            Assert.AreSame(_fallback, _catalog.PrefabFor(null));
            Assert.IsTrue(_catalog.Has("bench_3m"));
            Assert.IsFalse(_catalog.Has("no_such_item"));
        }

        [Test]
        public void StarterKitGetsOneViewPerSimEntity()
        {
            _view.Sync();
            Assert.AreEqual(_host.Build.PlacedItems.Count(), _view.FloorViewCount);
            Assert.AreEqual(ExpectedSlotThings(), _view.SlotViewCount);
            Assert.AreEqual(ExpectedPots(), _view.PotViewCount);
            Assert.AreEqual(ExpectedPlants(), _view.PlantViewCount);
            Assert.Greater(_view.PlantViewCount, 0, "starter moss");
            Assert.Greater(_view.SlotViewCount, 0);
        }

        [Test]
        public void RemovingAndReturningASlotThingDestroysAndRecreatesExactlyItsViews()
        {
            _view.Sync();
            var floor = _view.FloorViewCount;
            var things = _view.SlotViewCount;
            var plants = _view.PlantViewCount;

            // Take the first planting-slot container (a tray with plants) into the player's hand.
            var bench = _host.Build.PlacedItems.First(p => _host.Content.ItemDefs[p.ItemId].Bench != null);
            var address = SlotAddress.Planting(bench.Id, "plug_tray", 0, 0);
            var tray = _host.Nursery.Contents.Get(address);
            Assert.IsNotNull(tray, "starter tray at slot 0");
            var trayPlants = tray.Container.PlantIds.Count;
            Assert.Greater(trayPlants, 0);
            Assert.IsNotNull(_view.SlotView(address));

            var player = _host.Session.LocalPlayer;
            _host.Submit(new PickUpFromSlot(player, address));
            _host.World.Step(1);
            _view.Sync();

            Assert.IsNull(_host.Nursery.Contents.Get(address), "sim removed it");
            Assert.AreEqual(floor, _view.FloorViewCount);
            Assert.AreEqual(things - 1, _view.SlotViewCount);
            Assert.AreEqual(plants - trayPlants, _view.PlantViewCount);
            Assert.IsNull(_view.SlotView(address));

            _host.Submit(new PutInSlot(player, address));
            _host.World.Step(1);
            _view.Sync();

            Assert.AreEqual(floor, _view.FloorViewCount);
            Assert.AreEqual(things, _view.SlotViewCount);
            Assert.AreEqual(plants, _view.PlantViewCount);
            Assert.IsNotNull(_view.SlotView(address));
        }

        [Test]
        public void KnownIdsUseCatalogPrefabAndUnknownIdsUseFallback()
        {
            _view.Sync();
            var bench = _host.Build.PlacedItems.First(p => p.ItemId == "bench_3m");
            var barrel = _host.Build.PlacedItems.First(p => p.ItemId == "rain_barrel");
            Assert.IsNotNull(_view.FloorView(bench.Id).transform.Find("KnownMarker"), "bench uses the catalog prefab");
            Assert.IsNull(_view.FloorView(barrel.Id).transform.Find("KnownMarker"), "barrel uses the fallback");
            Assert.IsNotNull(_view.FloorView(barrel.Id).transform.Find("FallbackMarker"));
        }

        [Test]
        public void SyncTwiceDoesNotDuplicateViews()
        {
            _view.Sync();
            var counts = new List<int> { _view.FloorViewCount, _view.SlotViewCount, _view.PotViewCount, _view.PlantViewCount };
            _view.Sync();
            Assert.AreEqual(counts, new List<int> { _view.FloorViewCount, _view.SlotViewCount, _view.PotViewCount, _view.PlantViewCount });
            Assert.AreEqual(counts[0], _viewGo.transform.childCount, "only floor views are direct children");
        }
    }
}
