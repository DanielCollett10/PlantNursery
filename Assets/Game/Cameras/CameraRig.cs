using UnityEngine;
using UnityEngine.InputSystem;

namespace PlantNursery.Game
{
    /// <summary>
    /// One camera rig per local player (decision 0014 allows Cinemachine; this version uses a plain smooth-pan
    /// rig because the package is not installed yet). The rig follows the target; the camera child blends between
    /// mode poses over the configured pan time, never cutting. Holds no game rules: idle time and pan time come
    /// from content (building.json "camera").
    /// </summary>
    public sealed class CameraRig : MonoBehaviour
    {
        [SerializeField] private SimHost host;
        [SerializeField] private Transform target;
        [SerializeField] private Transform cameraTransform;
        [SerializeField] private InputActionAsset actions;
        [SerializeField] private float autoZoomOutSeconds = 4f;
        [SerializeField] private float panSeconds = 0.4f;
        [SerializeField] private Vector3 followOffset = new Vector3(0f, 8f, -5f);
        [SerializeField] private Vector3 zoomedOutOffset = new Vector3(0f, 22f, -13.75f);
        [SerializeField] private float pitch = 58f;

        private CameraModeMachine _machine;
        private InputAction _move, _zoom;
        private Vector3 _fromOffset, _toOffset, _currentOffset;
        private float _panT = 1f;
        private bool _configLoaded;

        /// <summary>Current mode.</summary>
        public CameraMode Mode => _machine != null ? _machine.Mode : CameraMode.Follow;
        /// <summary>The camera's current offset from the target (metres).</summary>
        public Vector3 CurrentOffset => _currentOffset;
        /// <summary>The offset the pan is heading for.</summary>
        public Vector3 TargetOffset => _toOffset;
        /// <summary>Pan duration in seconds.</summary>
        public float PanSeconds => panSeconds;
        /// <summary>Idle seconds before zooming out.</summary>
        public float AutoZoomOutSeconds => autoZoomOutSeconds;

        /// <summary>Wire references (used by the scene generator and tests).</summary>
        public void Configure(SimHost simHost, Transform followTarget, Transform cam, InputActionAsset asset)
        {
            host = simHost; target = followTarget; cameraTransform = cam; actions = asset;
        }

        /// <summary>Set timings directly (tests, or when no SimHost is wired).</summary>
        public void SetTimings(float autoZoomSeconds, float pan)
        {
            autoZoomOutSeconds = autoZoomSeconds; panSeconds = pan; _machine = null;
        }

        /// <summary>Stub for later: bench view slot. Not implemented.</summary>
        public void EnterBenchView(Transform bench) { }

        /// <summary>Stub for later: fixed-area (rush zone) slot. Not implemented.</summary>
        public void EnterFixedArea(Transform area) { }

        private void OnEnable()
        {
            if (actions != null)
            {
                var map = actions.FindActionMap("Walk", false);
                if (map != null) { _move = map.FindAction("Move", false); _zoom = map.FindAction("Zoom", false); map.Enable(); }
            }
            _currentOffset = _toOffset = followOffset;
            _panT = 1f;
            _machine = null;
            Apply();
        }

        private void OnDisable() { _move = null; _zoom = null; }

        private void LateUpdate()
        {
            if (target == null || cameraTransform == null) return;
            if (!_configLoaded && host != null && host.IsReady)
            {
                autoZoomOutSeconds = host.Content.Building.AutoZoomOutSeconds;
                panSeconds = host.Content.Building.CameraPanSeconds;
                _configLoaded = true; _machine = null;
            }
            if (_machine == null) _machine = new CameraModeMachine(autoZoomOutSeconds);
            var zoomPressed = _zoom != null && _zoom.WasPressedThisFrame();
            if (_machine.Update(Time.deltaTime, AnyInput(), zoomPressed))
            {
                _fromOffset = _currentOffset;
                _toOffset = _machine.Mode == CameraMode.ZoomedOut ? zoomedOutOffset : followOffset;
                _panT = 0f;
            }
            if (_panT < 1f)
            {
                _panT = panSeconds <= 0.0001f ? 1f : Mathf.Min(1f, _panT + Time.deltaTime / panSeconds);
                _currentOffset = Vector3.Lerp(_fromOffset, _toOffset, Mathf.SmoothStep(0f, 1f, _panT));
            }
            Apply();
        }

        private void Apply()
        {
            if (target == null || cameraTransform == null) return;
            transform.position = target.position;
            cameraTransform.localPosition = _currentOffset;
            cameraTransform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }

        private bool AnyInput()
        {
            if (_move != null && _move.ReadValue<Vector2>().sqrMagnitude > 0.01f) return true;
            var kb = Keyboard.current;
            if (kb != null && kb.anyKey.isPressed && !(_zoom != null && _zoom.IsPressed())) return true;
            var mouse = Mouse.current;
            if (mouse != null && (mouse.leftButton.isPressed || mouse.rightButton.isPressed || mouse.middleButton.isPressed)) return true;
            var pad = Gamepad.current;
            if (pad != null)
            {
                if (pad.leftStick.ReadValue().sqrMagnitude > 0.04f) return true;
                if (pad.buttonSouth.isPressed || pad.buttonNorth.isPressed || pad.buttonEast.isPressed || pad.buttonWest.isPressed
                    || pad.leftShoulder.isPressed || pad.rightShoulder.isPressed
                    || pad.leftTrigger.isPressed || pad.rightTrigger.isPressed
                    || pad.dpad.up.isPressed || pad.dpad.down.isPressed || pad.dpad.left.isPressed || pad.dpad.right.isPressed
                    || pad.startButton.isPressed) return true;
            }
            return false;
        }
    }
}
