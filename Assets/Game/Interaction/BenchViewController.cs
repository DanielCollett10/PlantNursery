using System.Collections.Generic;
using PlantNursery.Sim.Building;
using PlantNursery.Sim.Core;
using PlantNursery.Sim.Storage;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PlantNursery.Game
{
    /// <summary>
    /// Bench view (docs/design/controls.md, Bench view and Cameras). Interacting with a bench pans the <see cref="CameraRig"/>
    /// straight down onto it and any bench backed against it; walking more than <see cref="ExitDistance"/> from the bench edge
    /// leaves; there is no menu and movement stays live. Two pointers choose a slot with the same rules: the mouse (ray to
    /// bench-local point to <see cref="SlotPointer"/>) and the gamepad (<see cref="SlotStepper"/>). The pointed slot is lit in the
    /// player's colour and, while holding something, a ghost snaps into it, green or red by the Sim's PutInSlot validation.
    /// Holds no rules: slot choice, stepping and validity are all Sim calls.
    /// </summary>
    public sealed class BenchViewController : MonoBehaviour
    {
        /// <summary>Default distance from the bench edge beyond which bench view ends (controls.md: benchViewExitDistance).</summary>
        public const float DefaultExitDistance = 1.0f;

        private static readonly Color GhostValid = new Color(0.2f, 0.85f, 0.3f, 0.55f);
        private static readonly Color GhostInvalid = new Color(0.9f, 0.2f, 0.2f, 0.55f);

        [SerializeField] private SimHost host;
        [SerializeField] private Transform player;
        [SerializeField] private CameraRig rig;
        [SerializeField] private Camera viewCamera;
        [SerializeField] private InputActionAsset actions;
        [SerializeField] private WorldView worldView;
        [SerializeField] private float exitDistance = DefaultExitDistance;
        [SerializeField] private float fallbackBenchTopY = 0.9f;

        private readonly Dictionary<int, BenchTarget> _targets = new Dictionary<int, BenchTarget>();
        private readonly List<int> _scratchIds = new List<int>();
        private InputActionMap _map;
        private InputAction _step, _stepPrev, _stepNext;
        private Vector2 _lastStep, _lastMouse;
        private bool _usingMouse = true;
        private string _lastHeldKind;
        private GameObject _highlight, _ghost;
        private Material _highlightMat, _ghostMat;
        private float _planeY;

        /// <summary>Benches in the current view (view bench first); empty when not in bench view.</summary>
        public IReadOnlyList<int> ViewBenchIds { get; private set; } = new int[0];
        /// <summary>True while the bench view is active.</summary>
        public bool InBenchView => ViewBenchIds.Count > 0;
        /// <summary>The slot currently pointed at (mouse or gamepad), or null.</summary>
        public SlotAddress? PointedSlot { get; private set; }
        /// <summary>True when the mouse is the active pointer, false when the gamepad is.</summary>
        public bool UsingMouse => _usingMouse;
        /// <summary>Whether a ghost is shown and the Sim would accept putting the held thing there.</summary>
        public bool GhostValidNow { get; private set; }
        /// <summary>True while the ghost is visible.</summary>
        public bool GhostVisible => _ghost != null && _ghost.activeSelf;
        /// <summary>The Sim's rejection message for the ghost (empty when valid or no ghost).</summary>
        public string GhostReason { get; private set; } = string.Empty;
        /// <summary>Debug readout: raw pointer position and what it found.</summary>
        public string DebugText { get; private set; } = string.Empty;
        /// <summary>Exit distance in metres.</summary>
        public float ExitDistance { get => exitDistance; set => exitDistance = value; }

        /// <summary>Assign references (scene generator and tests).</summary>
        public void Configure(SimHost simHost, Transform playerTransform, CameraRig cameraRig, Camera cam, InputActionAsset asset, WorldView view)
        {
            host = simHost; player = playerTransform; rig = cameraRig; viewCamera = cam; actions = asset; worldView = view;
        }

        /// <summary>The highlight colour for a player (placeholder palette until players choose one).</summary>
        public static Color PlayerColour(PlayerId id)
        {
            var palette = new[]
            {
                new Color(1.0f, 0.78f, 0.15f), new Color(0.25f, 0.65f, 1.0f),
                new Color(1.0f, 0.4f, 0.7f), new Color(0.6f, 0.4f, 1.0f)
            };
            return palette[((id.Value % palette.Length) + palette.Length) % palette.Length];
        }

        /// <summary>The highlight object's colour (for tests and debugging).</summary>
        public Color HighlightColour => _highlightMat != null ? _highlightMat.color : Color.clear;

        /// <summary>World centre of the highlight (for tests).</summary>
        public Vector3 HighlightPosition => _highlight != null ? _highlight.transform.position : Vector3.zero;

        /// <summary>World centre of the ghost (for tests).</summary>
        public Vector3 GhostPosition => _ghost != null ? _ghost.transform.position : Vector3.zero;

        /// <summary>Enter bench view on a bench (and its back-to-back partners). Returns false if it is not a bench.</summary>
        public bool Enter(int benchId)
        {
            if (host == null || !host.IsReady) return false;
            var group = BenchSpace.ViewGroup(host.Content, host.Build, benchId);
            if (group.Count == 0) return false;
            var wasIn = InBenchView;
            ViewBenchIds = group;
            PointedSlot = null;
            _lastHeldKind = HeldKindId();
            _lastMouse = Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
            _planeY = ComputePlaneY();
            if (rig != null) PanCamera(group);
            if (_map != null) _map.Enable();
            if (!wasIn) { _lastStep = Vector2.zero; _usingMouse = Gamepad.current == null || Mouse.current != null; }
            return true;
        }

        /// <summary>Leave bench view; the camera pans back to follow.</summary>
        public void Exit()
        {
            if (!InBenchView) return;
            ViewBenchIds = new int[0];
            PointedSlot = null;
            if (rig != null) rig.ExitBenchView();
            Hide();
        }

        private void OnEnable()
        {
            if (actions != null)
            {
                _map = actions.FindActionMap("BenchView", false);
                if (_map != null)
                {
                    _step = _map.FindAction("Step", false);
                    _stepPrev = _map.FindAction("StepPrev", false);
                    _stepNext = _map.FindAction("StepNext", false);
                }
            }
        }

        private void OnDisable()
        {
            foreach (var t in _targets.Values) WorldInteractions.Unregister(t);
            _targets.Clear();
            if (InBenchView) Exit();
            _map = null; _step = _stepPrev = _stepNext = null;
            Release(_highlight); Release(_ghost); _highlight = _ghost = null;
        }

        private void Update()
        {
            if (host == null || !host.IsReady) return;
            SyncTargets();
            if (!InBenchView) return;
            if (!StillValid()) { Exit(); return; }
            ReadPointers();
            ShowPointed();
        }

        private bool StillValid()
        {
            foreach (var id in ViewBenchIds) if (host.Build.Get(id) == null) return false;
            if (player == null) return true;
            var d = BenchSpace.DistanceToBenches(host.Content, host.Build, ViewBenchIds, player.position.x, player.position.z);
            return d <= exitDistance;
        }

        private void SyncTargets()
        {
            _scratchIds.Clear();
            foreach (var p in host.Build.PlacedItems)
            {
                if (!host.Content.ItemDefs.TryGetValue(p.ItemId, out var def) || def.Bench == null) continue;
                _scratchIds.Add(p.Id);
                if (_targets.ContainsKey(p.Id)) continue;
                var t = new BenchTarget(this, p.Id);
                _targets[p.Id] = t;
                WorldInteractions.Register(t);
            }
            List<int> stale = null;
            foreach (var kv in _targets) if (!_scratchIds.Contains(kv.Key)) (stale ?? (stale = new List<int>())).Add(kv.Key);
            if (stale == null) return;
            foreach (var id in stale) { WorldInteractions.Unregister(_targets[id]); _targets.Remove(id); }
        }

        // ---------- pointing ----------

        private string HeldKindId()
        {
            var held = host.Nursery.Hands.GetHeld(host.Session.LocalPlayer);
            if (held == null) return null;
            return held.Kind == PlantNursery.Sim.Storage.HeldKind.Container && held.Container != null ? held.Container.Def.Id : held.ItemId;
        }

        private void ReadPointers()
        {
            var held = HeldKindId();
            if (held != _lastHeldKind) { _lastHeldKind = held; if (!_usingMouse) PointedSlot = null; }

            var mouse = Mouse.current;
            var mousePos = Vector2.zero;
            if (mouse != null)
            {
                mousePos = mouse.position.ReadValue();
                // An exact (0,0) means "no pointer yet", never a real position.
                var moved = mousePos != Vector2.zero && (mousePos - _lastMouse).sqrMagnitude > 0.25f;
                if (moved || (mouse.leftButton.wasPressedThisFrame && mousePos != Vector2.zero)) _usingMouse = true;
                if (mousePos != Vector2.zero) _lastMouse = mousePos;
            }

            var stepDir = ReadStep();
            if (stepDir.HasValue) _usingMouse = false;

            if (_usingMouse)
            {
                PointedSlot = mouse != null && mousePos != Vector2.zero ? SlotUnderMouse(mousePos, held) : null;
                DebugText = "mouse " + mousePos + " -> " + (PointedSlot.HasValue ? PointedSlot.Value.Key : "no slot");
                return;
            }

            var stepper = new SlotStepper(host.Content, host.Build, held, host.Build.Get(ViewBenchIds[0]).Rotation);
            if (!PointedSlot.HasValue) PointedSlot = StartSlot(stepper);
            else if (stepDir.HasValue) PointedSlot = stepper.Step(PointedSlot.Value, stepDir.Value) ?? PointedSlot;
            DebugText = "gamepad -> " + (PointedSlot.HasValue ? PointedSlot.Value.Key : "no slot");
        }

        private SlotAddress? StartSlot(SlotStepper stepper)
        {
            var p = player != null ? player.position : Vector3.zero;
            return stepper.Start(ViewBenchIds, p.x, p.z);
        }

        private StepDirection? ReadStep()
        {
            StepDirection? dir = null;
            if (_step != null)
            {
                var v = _step.ReadValue<Vector2>();
                if (_lastStep.sqrMagnitude < 0.25f && v.sqrMagnitude >= 0.25f)
                    dir = Mathf.Abs(v.x) > Mathf.Abs(v.y)
                        ? (v.x > 0f ? StepDirection.Right : StepDirection.Left)
                        : (v.y > 0f ? StepDirection.Up : StepDirection.Down);
                _lastStep = v;
            }
            if (_stepNext != null && _stepNext.WasPressedThisFrame()) dir = StepDirection.Right;
            if (_stepPrev != null && _stepPrev.WasPressedThisFrame()) dir = StepDirection.Left;
            return dir;
        }

        private Camera Cam => viewCamera != null ? viewCamera : Camera.main;

        private SlotAddress? SlotUnderMouse(Vector2 screen, string held)
        {
            var cam = Cam;
            if (cam == null) return null;
            var ray = cam.ScreenPointToRay(new Vector3(screen.x, screen.y, 0f));
            if (Mathf.Abs(ray.direction.y) < 1e-5f) return null;
            var t = (_planeY - ray.origin.y) / ray.direction.y;
            if (t < 0f) return null;
            var hit = ray.origin + ray.direction * t;
            foreach (var id in ViewBenchIds)
            {
                var bench = host.Build.Get(id);
                if (bench == null || !BenchSpace.TryWorldToLocal(host.Content, bench, hit.x, hit.z, out var u, out var v)) continue;
                var slot = SlotPointer.SlotAt(host.Content, bench, u, v, held, host.Nursery.Slots, host.Nursery.Strip);
                if (slot.HasValue) return slot;
            }
            return null;
        }

        // ---------- camera ----------

        private void PanCamera(IReadOnlyList<int> group)
        {
            float minX = float.MaxValue, minZ = float.MaxValue, maxX = float.MinValue, maxZ = float.MinValue;
            foreach (var id in group)
            {
                var p = host.Build.Get(id);
                minX = Mathf.Min(minX, p.Anchor.X * WorldLayout.CellSize); minZ = Mathf.Min(minZ, p.Anchor.Z * WorldLayout.CellSize);
                maxX = Mathf.Max(maxX, (p.Anchor.X + p.Width) * WorldLayout.CellSize);
                maxZ = Mathf.Max(maxZ, (p.Anchor.Z + p.Depth) * WorldLayout.CellSize);
            }
            var centre = new Vector3((minX + maxX) / 2f, 0f, (minZ + maxZ) / 2f);
            var yaw = (float)(int)host.Build.Get(group[0]).Rotation;
            var up = new Vector3(Mathf.Sin(yaw * Mathf.Deg2Rad), 0f, Mathf.Cos(yaw * Mathf.Deg2Rad));
            var right = new Vector3(up.z, 0f, -up.x);
            var halfX = (maxX - minX) / 2f; var halfZ = (maxZ - minZ) / 2f;
            var halfUp = Mathf.Abs(up.x) * halfX + Mathf.Abs(up.z) * halfZ;
            var halfRight = Mathf.Abs(right.x) * halfX + Mathf.Abs(right.z) * halfZ;
            var cam = Cam;
            var fov = cam != null ? cam.fieldOfView : 60f;
            var aspect = cam != null && cam.aspect > 0.1f ? cam.aspect : 16f / 9f;
            var tan = Mathf.Tan(fov * 0.5f * Mathf.Deg2Rad);
            var margin = 1.35f;
            var height = Mathf.Max(3f, _planeY + Mathf.Max((halfUp + 0.6f) * margin / tan, (halfRight + 0.6f) * margin / (tan * aspect)));
            rig.EnterBenchView(centre, yaw, height);
        }

        private float ComputePlaneY()
        {
            if (worldView == null) return fallbackBenchTopY;
            var view = worldView.FloorView(ViewBenchIds[0]);
            if (view == null) return fallbackBenchTopY;
            var y = 0f;
            foreach (var r in view.GetComponentsInChildren<Renderer>())
            {
                if (r.GetComponentInParent<PlantView>() != null) continue;
                y = Mathf.Max(y, r.bounds.max.y);
            }
            return y > 0.05f ? y : fallbackBenchTopY;
        }

        // ---------- highlight and ghost ----------

        private void ShowPointed()
        {
            if (!PointedSlot.HasValue) { Hide(); return; }
            var a = PointedSlot.Value;
            var bench = host.Build.Get(a.Slot.BenchId);
            var rect = BenchSpace.SlotRectOf(host.Content, host.Build, a);
            if (bench == null || !rect.HasValue || !BenchSpace.TryLocalToWorld(host.Content, bench, rect.Value.CentreU, rect.Value.CentreV, out var wx, out var wz))
            { Hide(); return; }
            var w = (float)(rect.Value.UMax - rect.Value.UMin);
            var d = (float)(rect.Value.VMax - rect.Value.VMin);
            var rot = Quaternion.Euler(0f, (int)bench.Rotation, 0f);

            EnsureObjects();
            _highlightMat.color = WithAlpha(PlayerColour(host.Session.LocalPlayer), 0.5f);
            _highlight.SetActive(true);
            _highlight.transform.SetPositionAndRotation(new Vector3((float)wx, _planeY + 0.012f, (float)wz), rot);
            _highlight.transform.localScale = new Vector3(w, 0.02f, d);

            var held = host.Nursery.Hands.GetHeld(host.Session.LocalPlayer);
            if (held == null) { GhostReason = string.Empty; GhostValidNow = false; _ghost.SetActive(false); return; }
            var result = host.Validate(new PutInSlot(host.Session.LocalPlayer, a));
            GhostValidNow = result.IsAccepted;
            GhostReason = result.IsAccepted ? string.Empty : (string.IsNullOrEmpty(result.Message) ? result.Reason.ToString() : result.Message);
            _ghostMat.color = GhostValidNow ? GhostValid : GhostInvalid;
            _ghost.SetActive(true);
            const float h = 0.12f;
            _ghost.transform.SetPositionAndRotation(new Vector3((float)wx, _planeY + 0.03f + h / 2f, (float)wz), rot);
            _ghost.transform.localScale = new Vector3(w * 0.9f, h, d * 0.9f);
        }

        private static Color WithAlpha(Color c, float a) { c.a = a; return c; }

        private void Hide()
        {
            GhostValidNow = false; GhostReason = string.Empty;
            if (_highlight != null) _highlight.SetActive(false);
            if (_ghost != null) _ghost.SetActive(false);
        }

        private void EnsureObjects()
        {
            if (_highlight != null) return;
            _highlightMat = new Material(Shader.Find("Sprites/Default"));
            _ghostMat = new Material(Shader.Find("Sprites/Default"));
            _highlight = MakeBox("BenchSlotHighlight", _highlightMat);
            _ghost = MakeBox("BenchSlotGhost", _ghostMat);
        }

        private GameObject MakeBox(string name, Material mat)
        {
            var go = new GameObject(name);
            go.AddComponent<MeshFilter>().sharedMesh = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
            go.AddComponent<MeshRenderer>().sharedMaterial = mat;
            go.SetActive(false);
            return go;
        }

        private static void Release(GameObject go)
        {
            if (go == null) return;
            if (Application.isPlaying) Destroy(go); else DestroyImmediate(go);
        }

        private void OnGUI()
        {
            if (!InBenchView) return;
            var cam = Cam;
            var text = DebugText;
            if (!string.IsNullOrEmpty(GhostReason)) text += "\n" + GhostReason;
            GUI.Label(new Rect(10, 10, 600, 60), "Bench view: " + text);
            if (cam == null || !PointedSlot.HasValue || string.IsNullOrEmpty(GhostReason)) return;
            var sp = cam.WorldToScreenPoint(GhostPosition);
            if (sp.z > 0f) GUI.Label(new Rect(sp.x - 80, Screen.height - sp.y - 40, 200, 24), GhostReason);
        }

        /// <summary>One placed bench as an interactable: interacting enters the bench view for its group.</summary>
        private sealed class BenchTarget : IInteractable
        {
            // Within this many metres of the bench edge the bench wins over walking-around tending.
            private const float Priority = 0.5f;
            private readonly BenchViewController _owner;
            private readonly int _benchId;

            public BenchTarget(BenchViewController owner, int benchId) { _owner = owner; _benchId = benchId; }

            public bool IsAvailable
            {
                get
                {
                    if (_owner == null || !_owner.isActiveAndEnabled || _owner.host == null || !_owner.host.IsReady) return false;
                    if (_owner.host.Build.Get(_benchId) == null) return false;
                    // While in the view, interact belongs to the slot actions, not to re-entering.
                    foreach (var id in _owner.ViewBenchIds) if (id == _benchId) return false;
                    return true;
                }
            }

            public string PromptKey => "prompt.bench";

            public float DistanceTo(Vector3 point)
            {
                var d = BenchSpace.DistanceToBenches(_owner.host.Content, _owner.host.Build, new[] { _benchId }, point.x, point.z);
                return Mathf.Max(0f, (float)d - Priority);
            }

            public CommandResult Interact(SimHost simHost)
                => _owner.Enter(_benchId) ? CommandResult.Accepted() : CommandResult.Rejected(RejectReason.InvalidTarget, "NotABench");
        }
    }
}
