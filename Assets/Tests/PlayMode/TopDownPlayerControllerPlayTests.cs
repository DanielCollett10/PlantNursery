using System.Collections;
using NUnit.Framework;
using PlantNursery.Game.Controls;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace PlantNursery.Game.PlayModeTests
{
    /// <summary>Drives the top-down controller with a virtual keyboard and a virtual gamepad.</summary>
    public sealed class TopDownPlayerControllerPlayTests : InputTestFixture
    {
        private Keyboard _keyboard;
        private Gamepad _gamepad;
        private GameObject _floor, _player, _cam;
        private InputActionAsset _asset;

        public override void Setup()
        {
            base.Setup();
            _keyboard = InputSystem.AddDevice<Keyboard>();
            _gamepad = InputSystem.AddDevice<Gamepad>();

            _floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _floor.transform.position = new Vector3(0f, -0.5f, 0f);
            _floor.transform.localScale = new Vector3(100f, 1f, 100f);

            // Camera looks along +z and down, so screen-up is world +z and screen-right is world +x.
            _cam = new GameObject("Cam");
            _cam.transform.position = new Vector3(0f, 8f, -5f);
            _cam.transform.rotation = Quaternion.Euler(58f, 0f, 0f);

            _asset = NurseryControlsBuilder.Build();
            _player = new GameObject("Player");
            _player.SetActive(false);
            _player.transform.position = new Vector3(0f, 0.05f, 0f);
            _player.AddComponent<CharacterController>();
            _player.AddComponent<TopDownPlayerController>().Configure(_asset, _cam.transform);
            _player.SetActive(true);
        }

        public override void TearDown()
        {
            Destroy(_player); Destroy(_floor); Destroy(_cam); Destroy(_asset);
            base.TearDown();
        }

        private static void Destroy(Object o)
        {
            if (o == null) return;
            if (Application.isPlaying) Object.Destroy(o); else Object.DestroyImmediate(o);
        }

        [UnityTest]
        public IEnumerator HoldingW_MovesUpScreenAtThreeMetresPerSecond()
        {
            yield return null;
            var start = _player.transform.position;
            Press(_keyboard.wKey);
            yield return new WaitForSeconds(1f);
            Release(_keyboard.wKey);
            yield return null;
            var d = _player.transform.position - start;
            Assert.Greater(d.z, 2.0f);
            Assert.Less(d.z, 3.6f);
            Assert.Less(Mathf.Abs(d.x), 0.1f);
        }

        [UnityTest]
        public IEnumerator HoldingD_MovesScreenRight()
        {
            yield return null;
            var start = _player.transform.position;
            Press(_keyboard.dKey);
            yield return new WaitForSeconds(0.5f);
            Release(_keyboard.dKey);
            yield return null;
            Assert.Greater(_player.transform.position.x - start.x, 0.8f);
        }

        [UnityTest]
        public IEnumerator GamepadStickRight_MovesScreenRight()
        {
            yield return null;
            var start = _player.transform.position;
            Set(_gamepad.leftStick, new Vector2(1f, 0f));
            yield return new WaitForSeconds(0.5f);
            Set(_gamepad.leftStick, Vector2.zero);
            yield return null;
            Assert.Greater(_player.transform.position.x - start.x, 0.8f);
        }

        [UnityTest]
        public IEnumerator GamepadStickUp_MovesUpScreen()
        {
            yield return null;
            var start = _player.transform.position;
            Set(_gamepad.leftStick, new Vector2(0f, 1f));
            yield return new WaitForSeconds(0.5f);
            Set(_gamepad.leftStick, Vector2.zero);
            yield return null;
            Assert.Greater(_player.transform.position.z - start.z, 0.8f);
        }

        [Test]
        public void ToWorld_ClampsDiagonalToUnitLength()
        {
            var v = TopDownPlayerController.ToWorld(new Vector2(1f, 1f), _cam.transform);
            Assert.AreEqual(1f, v.magnitude, 0.01f);
        }
    }
}
