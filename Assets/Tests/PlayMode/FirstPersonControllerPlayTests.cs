using System.Collections;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace PlantNursery.Game.PlayModeTests
{
    /// <summary>Proves WASD moves and the mouse turns the player through the real input asset.</summary>
    public sealed class FirstPersonControllerPlayTests : InputTestFixture
    {
        private const string ControlsPath = "Assets/Input/PlayerControls.inputactions";

        private Keyboard _keyboard;
        private Mouse _mouse;
        private GameObject _floor, _player;
        private FirstPersonController _controller;

        public override void Setup()
        {
            base.Setup();
            _keyboard = InputSystem.AddDevice<Keyboard>();
            _mouse = InputSystem.AddDevice<Mouse>();

            _floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _floor.transform.position = new Vector3(0f, -0.5f, 0f);
            _floor.transform.localScale = new Vector3(50f, 1f, 50f);

            var asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(ControlsPath);
            Assert.IsNotNull(asset, "Missing " + ControlsPath);

            _player = new GameObject("Player");
            _player.SetActive(false);
            _player.AddComponent<CharacterController>();
            var pivot = new GameObject("CameraPivot").transform;
            pivot.SetParent(_player.transform, false);
            _controller = _player.AddComponent<FirstPersonController>();
            _controller.Configure(asset, pivot);
            _player.SetActive(true);
        }

        public override void TearDown()
        {
            Object.Destroy(_player);
            Object.Destroy(_floor);
            base.TearDown();
        }

        [UnityTest]
        public IEnumerator HoldingW_MovesPlayerForward()
        {
            yield return null;
            var start = _player.transform.position;
            Press(_keyboard.wKey);
            yield return new WaitForSeconds(0.5f);
            Release(_keyboard.wKey);
            yield return null;
            var forward = Vector3.Dot(_player.transform.position - start, _player.transform.forward);
            Assert.Greater(forward, 0.5f);
        }

        [UnityTest]
        public IEnumerator MouseDeltaX100_TurnsPlayerAboutTenDegrees()
        {
            yield return null;
            var startYaw = _player.transform.eulerAngles.y;
            Set(_mouse.delta, new Vector2(100f, 0f));
            yield return null;
            yield return null;
            var yaw = Mathf.DeltaAngle(startYaw, _player.transform.eulerAngles.y);
            Assert.AreEqual(10f, yaw, 1f);
        }
    }
}
