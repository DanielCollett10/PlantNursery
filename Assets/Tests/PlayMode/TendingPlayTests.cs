using System.Collections;
using NUnit.Framework;
using PlantNursery.Sim.Building;
using PlantNursery.Sim.Storage;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace PlantNursery.Game.PlayModeTests
{
    /// <summary>Tending from the walk view with a virtual keyboard and gamepad: water, scrape, strip pick-up and put-down, crate, barrel (T-154).</summary>
    public sealed class TendingPlayTests : InputTestFixture
    {
        private Keyboard _keyboard;
        private Gamepad _gamepad;
        private GameObject _hostGo, _player, _tendGo;
        private InputActionAsset _asset;
        private SimHost _host;
        private PlacedItem _bench;
        private WorldInteractions _world;

        public override void Setup()
        {
            base.Setup();
            _keyboard = InputSystem.AddDevice<Keyboard>();
            _gamepad = InputSystem.AddDevice<Gamepad>();
            _asset = PlantNursery.Game.Controls.NurseryControlsBuilder.Build();

            _hostGo = new GameObject("SimHost");
            _host = _hostGo.AddComponent<SimHost>();
            foreach (var p in _host.Build.PlacedItems) if (p.ItemId == "bench_3m") _bench = p;
            Assert.IsNotNull(_bench, "starter nursery has a bench");

            _player = new GameObject("Player");
            _player.SetActive(false);
            _player.transform.position = BenchCentre();
            _world = _player.AddComponent<WorldInteractions>();
            _world.Configure(_host, _player.transform, _asset);
            _tendGo = new GameObject("Tending");
            _tendGo.AddComponent<TendingInteractable>().Configure(_host, _player.transform);
            _player.SetActive(true);
        }

        public override void TearDown()
        {
            Destroy(_player); Destroy(_tendGo); Destroy(_hostGo); Destroy(_asset);
            base.TearDown();
        }

        private static void Destroy(Object o)
        {
            if (o == null) return;
            if (Application.isPlaying) Object.Destroy(o); else Object.DestroyImmediate(o);
        }

        private Vector3 BenchCentre() => new Vector3(_bench.Anchor.X * 0.5f + _bench.Width * 0.25f, 0f, _bench.Anchor.Z * 0.5f + _bench.Depth * 0.25f);

        private Vector3 StripSpot() => new Vector3(_bench.Anchor.X * 0.5f + 0.25f, 0f, _bench.Anchor.Z * 0.5f + _bench.Depth * 0.5f - 0.1f);

        private Vector3 Near(string itemId)
        {
            foreach (var p in _host.Build.PlacedItems)
                if (p.ItemId == itemId) return new Vector3(p.Anchor.X * 0.5f + p.Width * 0.25f, 0f, p.Anchor.Z * 0.5f + p.Depth * 0.25f);
            Assert.Fail("no " + itemId);
            return Vector3.zero;
        }

        private static SlotAddress CanSlot(PlacedItem bench) => SlotAddress.Strip(bench.Id, "watering_can", 0, 0);

        private HeldThing Held => _host.Nursery.Hands.GetHeld(_host.Session.LocalPlayer);

        private void TakeCan()
        {
            var r = _host.Submit(new PickUpFromSlot(_host.Session.LocalPlayer, CanSlot(_bench)));
            Assert.IsTrue(r.IsAccepted, r.Message);
        }

        private void MakeAllMossHarvestable(bool harvestable)
        {
            foreach (var plant in _host.Nursery.Plants.Values)
            {
                var sp = _host.Content.SpeciesDefs[plant.SpeciesId];
                plant.StageIndex = sp.Stages.Count - 1;
                plant.RegrowTicksLeft = harvestable ? 0 : 100000;
            }
        }

        private IEnumerator Interact(bool gamepad)
        {
            var button = gamepad ? (UnityEngine.InputSystem.Controls.ButtonControl)_gamepad.buttonSouth : _keyboard.eKey;
            yield return null;
            Press(button);
            yield return null;
            Release(button);
            yield return null;
        }

        private int WateredCount() { var n = 0; foreach (var c in _host.Nursery.AllContainers()) if (c.WaterMeter >= 100f) n++; return n; }

        private IEnumerator WaterDryTray(bool gamepad)
        {
            MakeAllMossHarvestable(false);
            TakeCan();
            foreach (var c in _host.Nursery.AllContainers()) c.WaterMeter = 30f;
            var charges = Held.Charges;
            yield return Interact(gamepad);
            Assert.AreEqual(1, WateredCount(), "exactly one tray was watered");
            Assert.Less(Held.Charges, charges, "the can spent water");
        }

        private IEnumerator ScrapeHarvestable(bool gamepad)
        {
            MakeAllMossHarvestable(true);
            TakeCan();
            Assert.IsTrue(_host.Submit(new ReturnToCrate(_host.Session.LocalPlayer)).IsAccepted);
            yield return Interact(gamepad);
            Assert.IsNotNull(Held, "scraped moss is in hand");
            Assert.AreEqual(HeldKind.Bundle, Held.Kind);
            var regrowing = 0;
            foreach (var p in _host.Nursery.Plants.Values) if (p.IsRegrowing) regrowing++;
            Assert.Greater(regrowing, 0, "the scraped moss regrows");

            // Holding the bundle at the crate returns it.
            _player.transform.position = Near("storage_crate");
            yield return Interact(gamepad);
            Assert.IsNull(Held, "the crate took the bundle");
        }

        [UnityTest] public IEnumerator Keyboard_WaterDryTray() { yield return WaterDryTray(false); }

        [UnityTest] public IEnumerator Gamepad_WaterDryTray() { yield return WaterDryTray(true); }

        [UnityTest] public IEnumerator Keyboard_ScrapeHarvestableTray() { yield return ScrapeHarvestable(false); }

        [UnityTest] public IEnumerator Gamepad_ScrapeHarvestableTray() { yield return ScrapeHarvestable(true); }

        [UnityTest]
        public IEnumerator Keyboard_PickUpAndPutDownStripItem()
        {
            MakeAllMossHarvestable(false);
            foreach (var c in _host.Nursery.AllContainers()) c.WaterMeter = 100f;
            _player.transform.position = StripSpot();
            Assert.IsNull(_host.Nursery.Hands.GetHeld(_host.Session.LocalPlayer));
            yield return Interact(false);
            Assert.IsNotNull(Held, "picked up the can");
            Assert.AreEqual(HeldKind.Can, Held.Kind);
            Assert.IsNull(_host.Nursery.Contents.Get(CanSlot(_bench)));
            yield return Interact(false);
            Assert.IsNull(Held, "put the can back on the strip");
            Assert.IsNotNull(_host.Nursery.Contents.Get(CanSlot(_bench)));
        }

        [UnityTest]
        public IEnumerator Keyboard_EmptyCanRefillsAtBarrel()
        {
            MakeAllMossHarvestable(false);
            TakeCan();
            Held.Charges = 0;
            _player.transform.position = Near("rain_barrel");
            yield return Interact(false);
            Assert.Greater(Held.Charges, 0, "refilled at the barrel");
        }

        [UnityTest]
        public IEnumerator FarFromEverything_NothingOffered()
        {
            _player.transform.position = new Vector3(-30f, 0f, -30f);
            yield return null;
            Assert.IsNull(_world.Nearest);
        }
    }
}
