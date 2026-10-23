using System.Collections.Generic;
using PlantNursery.Sim.Building;
using PlantNursery.Sim.Content;
using PlantNursery.Sim.Core;
using PlantNursery.Sim.Hands;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace PlantNursery.Game
{
    /// <summary>
    /// Build mode and the put-down ghost. BuildToggle opens a UI Toolkit panel listing the build inventory;
    /// a ghost follows the aimed floor cell (R rotates, Place sends <see cref="PlaceItem"/>, PickUpDrop on a placed
    /// item sends <see cref="PickUpItem"/>). While holding a surface item, aiming at a bench shows a ghost snapped to the
    /// nearest mini-grid unit and auto-nudged by the sim's FindNearestFreeSpot; PickUpDrop sends <see cref="PutDown"/>.
    /// Ghost colour comes from <see cref="SimHost.Validate"/>; the only local rule is the player-capsule check.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class BuildModeController : MonoBehaviour
    {
        private const float BuildReach = 6f;
        private const float PlayerRadius = 0.3f;
        private const float PlayerHeight = 1.8f;
        /// <summary>Reason shown when the player is standing where a floor item would go.</summary>
        public const string PlayerInTheWay = "PlayerInTheWay";

        [SerializeField] private SimHost host;
        [SerializeField] private Camera viewCamera;
        [SerializeField] private InputActionAsset actions;
        [SerializeField] private List<PlaceableAuthoring> prefabs = new List<PlaceableAuthoring>();
        [SerializeField] private Material validMaterial;
        [SerializeField] private Material invalidMaterial;
        [SerializeField] private Vector3 plotOrigin = Vector3.zero;

        private InputAction _toggle, _rotate, _place, _pickUpDrop, _cancel;
        private Label _status, _hint;
        private VisualElement _panel, _list;
        private string _listSignature = string.Empty;
        private readonly List<ItemDef> _owned = new List<ItemDef>();
        private string _selectedId;
        private Rotation _rotation = Rotation.Deg0;
        private GameObject _ghost;
        private string _ghostId;
        private Renderer[] _ghostRenderers = new Renderer[0];
        private bool _ghostValid;
        private string _reason = string.Empty;
        private FirstPersonController _player;
        private WorldView _world;
        private Material _fallbackValid, _fallbackInvalid;

        // Latest aim result, consumed by the actions.
        private bool _hasAim;
        private Cell _floorAnchor;
        private Unit _surfaceAnchor;
        private bool _aimIsSurface;

        /// <summary>True while build mode is open.</summary>
        public bool IsBuildMode { get; private set; }
        /// <summary>Selected build item id, or null.</summary>
        public string SelectedItemId => _selectedId;
        /// <summary>True when the ghost is shown and the sim accepts the aimed action.</summary>
        public bool GhostValid => _ghost != null && _ghost.activeSelf && _ghostValid;
        /// <summary>Rejection reason name for the red ghost, or empty.</summary>
        public string Reason => _reason;
        /// <summary>Current ghost rotation.</summary>
        public Rotation GhostRotation => _rotation;

        /// <summary>Assign references (used by the scene generator).</summary>
        public void Configure(SimHost simHost, Camera cam, InputActionAsset asset, List<PlaceableAuthoring> placeablePrefabs,
            Material valid, Material invalid)
        {
            host = simHost; viewCamera = cam; actions = asset; prefabs = placeablePrefabs;
            validMaterial = valid; invalidMaterial = invalid;
        }

        private void OnEnable()
        {
            if (actions != null)
            {
                var map = actions.FindActionMap("Player", true);
                _toggle = map.FindAction("BuildToggle", true);
                _rotate = map.FindAction("Rotate", true);
                _place = map.FindAction("Place", true);
                _pickUpDrop = map.FindAction("PickUpDrop", true);
                _cancel = map.FindAction("Cancel", true);
                foreach (var a in new[] { _toggle, _rotate, _place, _pickUpDrop, _cancel }) a.Enable();
            }
            BuildPanel();
        }

        private void OnDisable()
        {
            if (_ghost != null) Destroy(_ghost);
            _ghost = null; _ghostId = null;
            IsBuildMode = false;
        }

        private void BuildPanel()
        {
            var doc = GetComponent<UIDocument>();
            if (doc.panelSettings == null)
            {
                Debug.LogError("[PlantNursery] BuildModeController: UIDocument has no PanelSettings. Assign Assets/Game/UI/GamePanelSettings.asset (run Plant Nursery/Rebuild Greybox World).", this);
                return;
            }
            var root = doc.rootVisualElement;
            root.Clear();
            root.style.flexGrow = 1;
            root.pickingMode = PickingMode.Ignore;
            var tree = Resources.Load<VisualTreeAsset>("UI/BuildPanel");
            var sheet = Resources.Load<StyleSheet>("UI/BuildPanel");
            if (tree == null) { Debug.LogError("Resources/UI/BuildPanel.uxml missing"); return; }
            if (sheet != null) root.styleSheets.Add(sheet);
            tree.CloneTree(root);
            _panel = root.Q("build-panel");
            _list = root.Q("build-list");
            _status = root.Q<Label>("build-status");
            _hint = root.Q<Label>("build-hint");
            root.Q<Label>("build-title").text = UiStrings.Get("build.title");
            _hint.text = UiStrings.Get("build.hint");
            _panel.RemoveFromClassList("build-panel--open");
        }

        private void Update()
        {
            if (host == null || !host.IsReady || viewCamera == null || _toggle == null) return;
            if (_player == null) _player = FindAnyObjectByType<FirstPersonController>();
            if (_player != null && !_player.enabled) { HideGhost(); return; } // a UI screen owns the input

            if (_toggle.WasPressedThisFrame()) SetBuildMode(!IsBuildMode);
            else if (IsBuildMode && _cancel.WasPressedThisFrame()) SetBuildMode(false);

            if (IsBuildMode) { RefreshOwned(); HandleSelectionKeys(); }
            if (_rotate.WasPressedThisFrame()) _rotation = BuildAim.Next(_rotation);

            _hasAim = false;
            var held = host.Hands.GetHeld(SimHost.LocalPlayer);
            if (held != null && IsSurfaceItem(held.ItemId)) UpdateSurfaceGhost(held.ItemId);
            else if (IsBuildMode && _selectedId != null) UpdateFloorGhost(_selectedId);
            else { HideGhost(); _reason = string.Empty; }

            HandleActions(held != null);
            if (IsBuildMode) RefreshPanel();
        }

        private void SetBuildMode(bool on)
        {
            IsBuildMode = on;
            _panel?.EnableInClassList("build-panel--open", on);
            _listSignature = string.Empty;
            if (!on) { HideGhost(); _reason = string.Empty; }
        }

        private bool IsSurfaceItem(string id)
            => host.Content.ItemDefs.TryGetValue(id, out var def) && def.Layer == ItemLayer.Surface;

        private void RefreshOwned()
        {
            _owned.Clear();
            foreach (var def in host.Content.ItemDefs.Values)
                if (def.Layer == ItemLayer.Floor && host.Build.Inventory.Count(def.Id) > 0) _owned.Add(def);
            _owned.Sort((a, b) => string.CompareOrdinal(a.Name, b.Name));
            var found = false;
            foreach (var d in _owned) if (d.Id == _selectedId) found = true;
            if (!found) _selectedId = _owned.Count > 0 ? _owned[0].Id : null;
        }

        private void HandleSelectionKeys()
        {
            if (_owned.Count == 0) return;
            var kb = Keyboard.current;
            if (kb != null)
            {
                for (var i = 0; i < _owned.Count && i < 9; i++)
                    if (kb[Key.Digit1 + i].wasPressedThisFrame) Select(_owned[i].Id);
            }
            var mouse = Mouse.current;
            if (mouse != null)
            {
                var scroll = mouse.scroll.ReadValue().y;
                if (Mathf.Abs(scroll) > 0.1f)
                {
                    var idx = _owned.FindIndex(d => d.Id == _selectedId);
                    idx = (idx + (scroll > 0 ? -1 : 1) + _owned.Count) % _owned.Count;
                    Select(_owned[idx].Id);
                }
            }
        }

        private void Select(string id)
        {
            if (id == _selectedId) return;
            _selectedId = id; _rotation = Rotation.Deg0;
        }

        // ---- floor ghost --------------------------------------------------------------------------------------

        private void UpdateFloorGhost(string itemId)
        {
            var ray = new Ray(viewCamera.transform.position, viewCamera.transform.forward);
            var plane = new Plane(Vector3.up, plotOrigin);
            if (!plane.Raycast(ray, out var dist) || dist > BuildReach) { HideGhost(); _reason = string.Empty; return; }
            var def = host.Content.ItemDefs[itemId];
            var size = Footprints.Rotate(def.FloorFootprint, _rotation);
            var anchor = BuildAim.FloorAnchor(ray.GetPoint(dist), plotOrigin, size.W, size.D);

            var result = host.Validate(new PlaceItem(SimHost.LocalPlayer, itemId, ItemLayer.Floor, anchor, _rotation));
            var reason = result.IsAccepted ? string.Empty : ReasonOf(result);
            ShowGhost(itemId);
            var pos = WorldLayout.PivotFor(plotOrigin + new Vector3(anchor.X * WorldLayout.CellSize, 0f, anchor.Z * WorldLayout.CellSize),
                _rotation, size.W * WorldLayout.CellSize, size.D * WorldLayout.CellSize);
            _ghost.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, WorldLayout.Yaw(_rotation), 0f));

            if (reason.Length == 0 && PlayerStandsIn(anchor, size)) reason = PlayerInTheWay;
            SetGhostState(reason.Length == 0, reason);
            _hasAim = true; _aimIsSurface = false; _floorAnchor = anchor;
        }

        private bool PlayerStandsIn(Cell anchor, Size2 size)
        {
            if (_player == null) return false;
            var rect = BuildAim.FloorRect(anchor, size.W, size.D, plotOrigin);
            var height = _ghostRenderers.Length > 0 ? 0f : 1f;
            foreach (var r in _ghostRenderers) height = Mathf.Max(height, r.bounds.max.y);
            return BuildAim.CapsuleOverlaps(rect, height, _player.transform.position, PlayerRadius, PlayerHeight);
        }

        // ---- surface (put-down) ghost -------------------------------------------------------------------------

        private void UpdateSurfaceGhost(string itemId)
        {
            var ray = new Ray(viewCamera.transform.position, viewCamera.transform.forward);
            if (!Physics.Raycast(ray, out var hit, host.Content.Building.InteractReach, ~0, QueryTriggerInteraction.Ignore)
                || hit.point.y < 0.2f)
            {
                HideGhost(); _reason = string.Empty; return;
            }
            var def = host.Content.ItemDefs[itemId];
            var size = Footprints.Rotate(def.SurfaceFootprint, _rotation);
            var desired = BuildAim.SurfaceAnchor(hit.point, plotOrigin, size.W, size.D);
            var spot = host.Surface.FindNearestFreeSpot(itemId, _rotation, desired, host.Content.Building.NudgeRadius);
            var anchor = spot ?? desired;
            var result = host.Validate(new PutDown(SimHost.LocalPlayer, anchor, _rotation));

            ShowGhost(itemId);
            var top = SupportTop(anchor, hit.point.y);
            var pos = WorldLayout.PivotFor(plotOrigin + new Vector3(anchor.U * WorldLayout.UnitSize, top, anchor.V * WorldLayout.UnitSize),
                _rotation, size.W * WorldLayout.UnitSize, size.D * WorldLayout.UnitSize);
            _ghost.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, WorldLayout.Yaw(_rotation), 0f));
            SetGhostState(result.IsAccepted, result.IsAccepted ? string.Empty : ReasonOf(result));
            _hasAim = true; _aimIsSurface = true; _surfaceAnchor = anchor;
        }

        private float SupportTop(Unit anchor, float fallback)
        {
            var support = host.Build.GetAt(SurfaceGrid.CellOf(anchor));
            if (support == null) return fallback;
            if (_world == null) _world = FindAnyObjectByType<WorldView>();
            var view = _world != null ? _world.FloorView(support.Id) : null;
            if (view == null) return fallback;
            var top = view.transform.position.y;
            foreach (var r in view.GetComponentsInChildren<Renderer>()) top = Mathf.Max(top, r.bounds.max.y);
            return top;
        }

        // ---- actions ------------------------------------------------------------------------------------------

        private void HandleActions(bool holding)
        {
            if (holding)
            {
                if (_hasAim && _aimIsSurface && _pickUpDrop.WasPressedThisFrame())
                    Send(new PutDown(SimHost.LocalPlayer, _surfaceAnchor, _rotation));
                return;
            }
            if (!IsBuildMode) return;
            if (_hasAim && !_aimIsSurface && _place.WasPressedThisFrame())
            {
                if (_reason == PlayerInTheWay) Fail(PlayerInTheWay);
                else Send(new PlaceItem(SimHost.LocalPlayer, _selectedId, ItemLayer.Floor, _floorAnchor, _rotation));
            }
            if (_pickUpDrop.WasPressedThisFrame()) PickUpAimedItem();
        }

        private void PickUpAimedItem()
        {
            var ray = new Ray(viewCamera.transform.position, viewCamera.transform.forward);
            if (!Physics.Raycast(ray, out var hit, host.Content.Building.InteractReach, ~0, QueryTriggerInteraction.Ignore)) return;
            var probe = hit.point + ray.direction * 0.02f;
            var cell = new Cell(Mathf.FloorToInt((probe.x - plotOrigin.x) / WorldLayout.CellSize),
                Mathf.FloorToInt((probe.z - plotOrigin.z) / WorldLayout.CellSize));
            var placed = host.Build.GetAt(cell);
            if (placed == null) return;
            Send(new PickUpItem(SimHost.LocalPlayer, placed.Id));
        }

        private void Send(ICommand command)
        {
            var result = host.Submit(command);
            if (!result.IsAccepted) Fail(ReasonOf(result));
        }

        private void Fail(string reason)
        {
            _reason = reason;
            if (_status != null) _status.text = ReasonText(reason);
        }

        private static string ReasonOf(CommandResult r) => string.IsNullOrEmpty(r.Message) ? r.Reason.ToString() : r.Message;

        private static string ReasonText(string reason)
            => reason.Length == 0 ? string.Empty : UiStrings.Has("build.reject." + reason) ? UiStrings.Get("build.reject." + reason) : reason;

        // ---- ghost object -------------------------------------------------------------------------------------

        private void ShowGhost(string itemId)
        {
            if (_ghost == null || _ghostId != itemId)
            {
                if (_ghost != null) Destroy(_ghost);
                _ghost = BuildGhostObject(itemId);
                _ghostId = itemId;
                _ghostRenderers = _ghost.GetComponentsInChildren<Renderer>();
            }
            if (!_ghost.activeSelf) _ghost.SetActive(true);
        }

        private void HideGhost()
        {
            if (_ghost != null && _ghost.activeSelf) _ghost.SetActive(false);
        }

        private GameObject BuildGhostObject(string itemId)
        {
            GameObject go = null;
            foreach (var p in prefabs)
                if (p != null && p.ItemId == itemId) { go = Instantiate(p.gameObject); break; }
            if (go == null)
            {
                go = new GameObject();
                var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cube.transform.SetParent(go.transform, false);
                cube.transform.localScale = Vector3.one * 0.2f;
                cube.transform.localPosition = Vector3.one * 0.1f;
            }
            go.name = "Ghost_" + itemId;
            foreach (var c in go.GetComponentsInChildren<Collider>()) Destroy(c);
            foreach (var t in go.GetComponentsInChildren<InteractableTarget>()) Destroy(t);
            foreach (var t in go.GetComponentsInChildren<PlaceableAuthoring>()) Destroy(t);
            foreach (var r in go.GetComponentsInChildren<Renderer>()) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return go;
        }

        private void SetGhostState(bool valid, string reason)
        {
            _ghostValid = valid;
            _reason = reason;
            var mat = valid ? ValidMaterial() : InvalidMaterial();
            foreach (var r in _ghostRenderers)
            {
                var mats = r.sharedMaterials;
                for (var i = 0; i < mats.Length; i++) mats[i] = mat;
                r.sharedMaterials = mats;
            }
            if (_status != null) _status.text = ReasonText(reason);
        }

        private Material ValidMaterial()
            => validMaterial != null ? validMaterial : _fallbackValid ?? (_fallbackValid = MakeFallback(new Color(0.25f, 1f, 0.25f, 0.5f)));

        private Material InvalidMaterial()
            => invalidMaterial != null ? invalidMaterial : _fallbackInvalid ?? (_fallbackInvalid = MakeFallback(new Color(1f, 0.25f, 0.25f, 0.5f)));

        private static Material MakeFallback(Color colour)
        {
            var shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default");
            var m = new Material(shader) { color = colour };
            return m;
        }

        // ---- panel --------------------------------------------------------------------------------------------

        private void RefreshPanel()
        {
            if (_list == null) return;
            var sig = _selectedId + "|";
            foreach (var d in _owned) sig += d.Id + ":" + host.Build.Inventory.Count(d.Id) + ";";
            if (sig == _listSignature) return;
            _listSignature = sig;
            _list.Clear();
            if (_owned.Count == 0)
            {
                var empty = new Label(UiStrings.Get("build.empty"));
                empty.AddToClassList("build-row");
                _list.Add(empty);
                return;
            }
            for (var i = 0; i < _owned.Count; i++)
            {
                var d = _owned[i];
                var row = new Label(UiStrings.Get("build.row", i + 1, d.Name, host.Build.Inventory.Count(d.Id)));
                row.AddToClassList("build-row");
                if (d.Id == _selectedId) row.AddToClassList("build-row--selected");
                _list.Add(row);
            }
        }
    }
}
