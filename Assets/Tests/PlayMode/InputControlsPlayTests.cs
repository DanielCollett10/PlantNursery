using System.Collections;
using NUnit.Framework;
using PlantNursery.Game.Controls;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace PlantNursery.Game.PlayModeTests
{
    public class InputControlsPlayTests : InputTestFixture
    {
        InputActionAsset _asset;

        public override void Setup()
        {
            base.Setup();
            _asset = NurseryControlsBuilder.Build();
        }

        public override void TearDown()
        {
            if (Application.isPlaying) Object.Destroy(_asset);
            else Object.DestroyImmediate(_asset);
            base.TearDown();
        }

        [Test]
        public void EveryActionIsBoundOnBothDevices()
        {
            Assert.AreEqual(5, _asset.actionMaps.Count);
            foreach (var map in _asset.actionMaps)
            foreach (var action in map.actions)
            {
                bool km = false, gp = false;
                foreach (var b in action.bindings)
                {
                    if (b.groups == NurseryControlsBuilder.KeyboardMouse) km = true;
                    if (b.groups == NurseryControlsBuilder.Gamepad) gp = true;
                }
                Assert.IsTrue(km, map.name + "/" + action.name + " lacks keyboard+mouse binding");
                Assert.IsTrue(gp, map.name + "/" + action.name + " lacks gamepad binding");
            }
        }

        [UnityTest]
        public IEnumerator SwitchingModeEnablesOnlyThatMap()
        {
            var sw = new InputModeSwitcher(_asset);
            foreach (InputMode mode in System.Enum.GetValues(typeof(InputMode)))
            {
                sw.SetMode(mode);
                yield return null;
                foreach (var map in _asset.actionMaps)
                    Assert.AreEqual(map.name == mode.ToString(), map.enabled, map.name + " in mode " + mode);
            }
        }

        [UnityTest]
        public IEnumerator TrackerDetectsDeviceChange()
        {
            var keyboard = InputSystem.AddDevice<Keyboard>();
            var gamepad = InputSystem.AddDevice<Gamepad>();
            var sw = new InputModeSwitcher(_asset);
            using (var tracker = new InputDeviceTracker(_asset))
            {
                Assert.AreEqual(InputDeviceKind.KeyboardMouse, tracker.Current);
                Press(gamepad.buttonSouth);
                yield return null;
                Assert.AreEqual(InputDeviceKind.Gamepad, tracker.Current);
                Release(gamepad.buttonSouth);
                Press(keyboard.eKey);
                yield return null;
                Assert.AreEqual(InputDeviceKind.KeyboardMouse, tracker.Current);
            }
        }
    }
}
