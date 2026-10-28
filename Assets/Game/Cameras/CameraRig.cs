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
        private bool _benchActive;
        private float _benchBlend;
        private Vector3 _benchPos;
        private Quaternion _benchRot = Quaternion.Euler(90f, 0f, 0f);

        /// <summary>Current mode.</summary>
        public CameraMode Mode => _benchActive ? CameraMode.BenchView : (_machine != null ? _machine.Mode : CameraMode.Follow);
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

        /// <summary>
        /// Pan to look straight down on a bench (or back-to-back benches). The camera ends at <paramref name="centre"/> raised to
        /// <paramref name="height"/>, turned by <paramref name="yawDegrees"/> so screen-up is the view bench's back. A smooth pan
        /// over <see cref="PanSeconds"/>, never a cut. Movement and the follow target are untouched.
        /// </summary>
        public void EnterBenchView(Vector3 centre, float yawDegrees, float height)
        {
            _benchPos = new Vector3(centre.x, height, centre.z);
            _benchRot = Quaternion.Euler(90f, yawDegrees, 0f);
            _benchActive = true;
        }

        /// <summary>Pan back to the follow pose.</summary>
        public void ExitBenchView()
        {
            if (!_benchActive) return;
            _benchActive = false;
            _machine = null; // fresh idle timer and Follow mode
            _currentOffset = _toOffset = followOffset;
        }

        /// <summary>True from entering bench view until exit is called.</summary>
        public bool IsBenchView => _benchActive;

        /// <summary>0 = follow pose, 1 = bench pose (the pan progress).</summary>
        public float BenchBlend => _benchBlend;

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
            // Bench view owns the camera: movement stays live but must not zoom out or return to follow.
            if (!_benchActive && _machine.Update(Time.deltaTime, AnyInput(), zoomPressed))
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
            var goal = _benchActive ? 1f : 0f;
            _benchBlend = panSeconds <= 0.0001f ? goal : Mathf.MoveTowards(_benchBlend, goal, Time.deltaTime / panSeconds);
            Apply();
        }

        private void Apply()
        {
            if (target == null || cameraTransform == null) return;
            transform.position = target.position;
            var followRot = Quaternion.Euler(pitch, 0f, 0f);
            if (_benchBlend <= 0f)
            {
                cameraTransform.localPosition = _currentOffset;
                cameraTransform.localRotation = followRot;
                return;
            }
            var s = Mathf.SmoothStep(0f, 1f, _benchBlend);
            var followPos = transform.position + _currentOffset;
            cameraTransform.SetPositionAndRotation(Vector3.Lerp(followPos, _benchPos, s), Quaternion.Slerp(followRot, _benchRot, s));
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
