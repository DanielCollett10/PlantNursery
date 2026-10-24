using System.Collections.Generic;
using PlantNursery.Game.Controls;
using PlantNursery.Sim.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PlantNursery.Game
{
    /// <summary>
    /// Registry of <see cref="IInteractable"/>s plus the player-side probe: the nearest available one within
    /// <see cref="Reach"/> metres of the player shows a prompt (button name for the last-used device) and the Walk map's
    /// Interact action uses it. Other features register their own interactables; this file does not change for them.
    /// </summary>
    public sealed class WorldInteractions : MonoBehaviour
    {
        /// <summary>Interaction reach in metres (controls.md).</summary>
        public const float Reach = 1.5f;
        private const float RejectionSeconds = 2f;

        private static readonly List<IInteractable> Registry = new List<IInteractable>();

        [SerializeField] private SimHost host;
        [SerializeField] private Transform player;
        [SerializeField] private InputActionAsset actions;

        private InputAction _interact;
        private InputDeviceTracker _tracker;
        private string _rejection = string.Empty;
        private float _rejectUntil;

        /// <summary>Add an interactable to the registry (idempotent).</summary>
        public static void Register(IInteractable interactable)
        {
            if (interactable != null && !Registry.Contains(interactable)) Registry.Add(interactable);
        }

        /// <summary>Remove an interactable from the registry.</summary>
        public static void Unregister(IInteractable interactable) { Registry.Remove(interactable); }

        /// <summary>The nearest available interactable within reach of <paramref name="point"/>, or null.</summary>
        public static IInteractable FindNearest(Vector3 point, float reach = Reach)
        {
            IInteractable best = null;
            var bestDist = reach;
            foreach (var i in Registry)
            {
                if (i == null || !i.IsAvailable) continue;
                var d = i.DistanceTo(point);
                if (d <= bestDist) { best = i; bestDist = d; }
            }
            return best;
        }

        /// <summary>The interactable currently offered, or null.</summary>
        public IInteractable Nearest { get; private set; }

        /// <summary>Last-used device kind (picks the button names in prompts).</summary>
        public InputDeviceKind Device => _tracker != null ? _tracker.Current : InputDeviceKind.KeyboardMouse;

        /// <summary>Prompt for <see cref="Nearest"/>, for example "E: Sleep"; empty when nothing is in reach.</summary>
        public string Prompt => Nearest == null ? string.Empty
            : UiStrings.Get("prompt.format", UiStrings.Get("button.interact." + Device), UiStrings.Get(Nearest.PromptKey));

        /// <summary>Latest rejection text while fresh, else empty.</summary>
        public string Rejection => Time.time < _rejectUntil ? _rejection : string.Empty;

        /// <summary>Assign references (scene generator and tests).</summary>
        public void Configure(SimHost simHost, Transform playerTransform, InputActionAsset asset)
        {
            host = simHost; player = playerTransform; actions = asset;
        }

        private void OnEnable()
        {
            if (actions == null) { Debug.LogError("WorldInteractions needs 'actions' assigned.", this); return; }
            var map = actions.FindActionMap("Walk", true);
            _interact = map.FindAction("Interact", true);
            map.Enable();
            _tracker = new InputDeviceTracker(actions);
        }

        private void OnDisable()
        {
            _tracker?.Dispose();
            _tracker = null;
            _interact = null;
            Nearest = null;
        }

        private void Update()
        {
            if (host == null || !host.IsReady || player == null) { Nearest = null; return; }
            Nearest = FindNearest(player.position);
            if (Nearest != null && _interact != null && _interact.WasPressedThisFrame()) Use(Nearest);
        }

        private void Use(IInteractable target)
        {
            var result = target.Interact(host);
            if (result.IsAccepted) return;
            var key = string.IsNullOrEmpty(result.Message) ? result.Reason.ToString() : result.Message;
            // A specific message ("CanEmpty") wins over the broad reason ("NotAllowed").
            if (!string.IsNullOrEmpty(result.Message) && UiStrings.Has("reject." + result.Message)) _rejection = UiStrings.Get("reject." + result.Message);
            else _rejection = UiStrings.Has("reject." + result.Reason) ? UiStrings.Get("reject." + result.Reason) : key;
            _rejectUntil = Time.time + RejectionSeconds;
        }
    }
}
