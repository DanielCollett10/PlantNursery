using System.Collections;
using NUnit.Framework;
using PlantNursery.Game.Controls;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace PlantNursery.Game.PlayModeTests
{
    /// <summary>Camera rig: idle zoom-out, zoom button, input returns, smooth pan (no cuts).</summary>
    public sealed class CameraRigPlayTests : InputTestFixture
    {
        private Keyboard _keyboard;
        private GameObject _target, _rigGo, _cam;
        private InputActionAsset _asset;
        private CameraRig _rig;

        public override void Setup()
        {
            base.Setup();
            _keyboard = InputSystem.AddDevice<Keyboard>();
            _asset = NurseryControlsBuilder.Build();
            _target = new GameObject("Target");
            _rigGo = new GameObject("Rig");
            _rigGo.SetActive(false);
            _cam = new GameObject("Cam");
            _cam.transform.SetParent(_rigGo.transform, false);
            _rig = _rigGo.AddComponent<CameraRig>();
            _rig.Configure(null, _target.transform, _cam.transform, _asset);
            _rig.SetTimings(0.5f, 0.4f);
            _rigGo.SetActive(true);
        }

        public override void TearDown()
        {
            Destroy(_rigGo); Destroy(_target); Destroy(_asset);
            base.TearDown();
        }

        private static void Destroy(Object o)
        {
            if (o == null) return;
            if (Application.isPlaying) Object.Destroy(o); else Object.DestroyImmediate(o);
        }

        [UnityTest]
        public IEnumerator IdleZoomsOut_ThenInputReturns_WithoutCuts()
        {
            yield return null;
            Assert.AreEqual(CameraMode.Follow, _rig.Mode);
            var followY = _cam.transform.localPosition.y;
            yield return new WaitForSeconds(0.7f);
            Assert.AreEqual(CameraMode.ZoomedOut, _rig.Mode);
            // Mid-pan the camera is strictly between the two poses (not cut).
            var seen = false;
            var t = 0f;
            while (t < 1.0f)
            {
                var y = _cam.transform.localPosition.y;
                if (y > followY + 0.1f && y < _rig.TargetOffset.y - 0.1f) seen = true;
                t += Time.deltaTime; yield return null;
            }
            Assert.IsTrue(seen || Mathf.Approximately(_cam.transform.localPosition.y, _rig.TargetOffset.y));
            Assert.AreEqual(_rig.TargetOffset.y, _cam.transform.localPosition.y, 0.05f);

            Press(_keyboard.dKey);
            yield return null; yield return null;
            Assert.AreEqual(CameraMode.Follow, _rig.Mode);
            Release(_keyboard.dKey);
        }

        [UnityTest]
        public IEnumerator ZoomKeyZoomsOutImmediately()
        {
            yield return null;
            Press(_keyboard.zKey);
            yield return null; yield return null;
            Assert.AreEqual(CameraMode.ZoomedOut, _rig.Mode);
            Release(_keyboard.zKey);
        }

        [UnityTest]
        public IEnumerator CameraFollowsTarget()
        {
            yield return null;
            _target.transform.position = new Vector3(5f, 0f, 7f);
            yield return null;
            Assert.AreEqual(5f, _rigGo.transform.position.x, 0.01f);
            Assert.AreEqual(7f, _rigGo.transform.position.z, 0.01f);
        }
    }
}
