using System.Collections;
using NUnit.Framework;
using PlantNursery.Game.Controls;
using PlantNursery.Sim.Time;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace PlantNursery.Game.PlayModeTests
{
    /// <summary>Sleeping at the bed with a virtual keyboard and a virtual gamepad (T-153).</summary>
    public sealed class BedInteractionPlayTests : InputTestFixture
    {
        private Keyboard _keyboard;
        private Gamepad _gamepad;
        private GameObject _hostGo, _player, _bedGo;
        private InputActionAsset _asset;
        private SimHost _host;
        private WorldInteractions _world;

        public override void Setup()
        {
            base.Setup();
            _keyboard = InputSystem.AddDevice<Keyboard>();
            _gamepad = InputSystem.AddDevice<Gamepad>();
            _asset = NurseryControlsBuilder.Build();

            _hostGo = new GameObject("SimHost");
            _host = _hostGo.AddComponent<SimHost>(); // Awake initialises the sim in Play mode

            var bed = default(PlantNursery.Sim.Building.PlacedItem);
            foreach (var p in _host.Build.PlacedItems) if (p.ItemId == "bed") bed = p;
            Assert.IsNotNull(bed, "starter nursery has a bed");
            var pos = new Vector3(bed.Anchor.X * WorldLayout.CellSize, 0f, bed.Anchor.Z * WorldLayout.CellSize - 0.5f);

            _bedGo = new GameObject("Bed");
            _bedGo.AddComponent<BedInteractable>().Configure(_host);
            _player = new GameObject("Player");
            _player.SetActive(false);
            _player.transform.position = pos;
            _world = _player.AddComponent<WorldInteractions>();
            _world.Configure(_host, _player.transform, _asset);
            _player.SetActive(true);
        }

        public override void TearDown()
        {
            Destroy(_player); Destroy(_bedGo); Destroy(_hostGo); Destroy(_asset);
            base.TearDown();
        }

        private static void Destroy(Object o)
        {
            if (o == null) return;
            if (Application.isPlaying) Object.Destroy(o); else Object.DestroyImmediate(o);
        }

        private void AdvanceTo(DayPhase phase)
        {
            while (_host.Clock.Phase != phase) _host.World.Step(1);
        }

        [UnityTest]
        public IEnumerator Keyboard_SleepInEvening_AdvancesToMorning()
        {
            AdvanceTo(DayPhase.Evening);
            var day = _host.Clock.Day;
            yield return null;
            Assert.AreEqual("E: Sleep", _world.Prompt);
            Press(_keyboard.eKey);
            yield return null;
            Release(_keyboard.eKey);
            yield return null;
            Assert.AreEqual(_host.Content.Time.DayStartMinute, _host.Clock.MinuteOfDay);
            Assert.AreEqual(6 * 60, _host.Clock.MinuteOfDay);
            Assert.AreEqual(day + 1, _host.Clock.Day);
        }

        [UnityTest]
        public IEnumerator Gamepad_SleepAtNight_AdvancesToMorning_AndPromptUsesGamepadButton()
        {
            AdvanceTo(DayPhase.Night);
            var day = _host.Clock.Day;
            yield return null;
            Press(_gamepad.buttonSouth);
            yield return null;
            Release(_gamepad.buttonSouth);
            yield return null;
            Assert.AreEqual(6 * 60, _host.Clock.MinuteOfDay);
            Assert.AreEqual(day + 1, _host.Clock.Day);
            Assert.AreEqual(InputDeviceKind.Gamepad, _world.Device);
            Assert.AreEqual("A: Sleep", _world.Prompt);
        }

        [UnityTest]
        public IEnumerator SleepInDaytime_ShowsNotTired_AndTimeDoesNotJump()
        {
            AdvanceTo(DayPhase.Daytime);
            var tick = _host.World.Tick;
            yield return null;
            Press(_keyboard.eKey);
            yield return null;
            Release(_keyboard.eKey);
            yield return null;
            Assert.AreEqual("Not tired yet.", _world.Rejection);
            Assert.Less(_host.World.Tick - tick, 60);
        }

        [UnityTest]
        public IEnumerator FarFromBed_NoPrompt()
        {
            _player.transform.position = new Vector3(0f, 0f, 0f);
            yield return null;
            Assert.IsNull(_world.Nearest);
            Assert.AreEqual(string.Empty, _world.Prompt);
        }
    }
}
