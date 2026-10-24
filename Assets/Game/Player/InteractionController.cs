using System;
using PlantNursery.Sim.Core;
using PlantNursery.Sim.Hands;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PlantNursery.Game
{
    /// <summary>
    /// Raycasts from the camera within reach (from content), asks the sim which action the target and held
    /// item offer (<see cref="Interaction.Offer"/>), shows the prompt and submits the command on Interact.
    /// Holds no rules: the sim validates and its rejection message is shown briefly.
    /// </summary>
    public sealed class InteractionController : MonoBehaviour
    {
        private const float RejectionSeconds = 2f;

        [SerializeField] private SimHost host;
        [SerializeField] private Camera viewCamera;
        [SerializeField] private InputActionAsset actions;

        private InputAction _interact;
        private InteractionOffer _offer;
        private float _rejectUntil;
        private string _rejection = string.Empty;

        /// <summary>Raised when the shop should open (computer). UI is added by another task.</summary>
        public event Action ShopRequested;

        /// <summary>Current prompt text, or empty when nothing is offered.</summary>
        public string Prompt => _offer?.Prompt ?? string.Empty;
        /// <summary>Latest rejection message while it is still fresh, else empty.</summary>
        public string Rejection => Time.time < _rejectUntil ? _rejection : string.Empty;
        /// <summary>The held item id, or empty.</summary>
        public string HeldItemId => host != null && host.IsReady ? host.Hands.GetHeld(host.Session.LocalPlayer)?.ItemId ?? string.Empty : string.Empty;

        /// <summary>Wire references (used by the scene generator).</summary>
        public void Configure(SimHost simHost, Camera cam, InputActionAsset asset)
        {
            host = simHost; viewCamera = cam; actions = asset;
        }

        private void OnEnable()
        {
            if (actions == null) return;
            _interact = actions.FindActionMap("Player", true).FindAction("Interact", true);
            _interact.Enable();
        }

        private void Update()
        {
            if (host == null || !host.IsReady || viewCamera == null) return;
            _offer = FindOffer();
            if (_interact != null && _interact.WasPressedThisFrame()) Use();
        }

        private InteractionOffer FindOffer()
        {
            var ray = new Ray(viewCamera.transform.position, viewCamera.transform.forward);
            if (!Physics.Raycast(ray, out var hit, host.Content.Building.InteractReach, ~0, QueryTriggerInteraction.Ignore))
                return null;
            return Interaction.Offer(host.Session.LocalPlayer,Resolve(hit.collider), host.Hands, host.Surface);
        }

        private static InteractTarget Resolve(Collider c)
        {
            var target = c.GetComponentInParent<InteractableTarget>();
            if (target != null) return target.ToSim();
            var placeable = c.GetComponentInParent<PlaceableAuthoring>();
            if (placeable != null) return new InteractTarget(Interaction.KindForPlacedItem(placeable.ItemId));
            return InteractTarget.None;
        }

        private void Use()
        {
            if (_offer == null) return;
            if (_offer.OpensShop) { ShopRequested?.Invoke(); return; }
            var result = host.Submit(_offer.Command);
            if (!result.IsAccepted)
            {
                _rejection = string.IsNullOrEmpty(result.Message) ? result.Reason.ToString() : result.Message;
                _rejectUntil = Time.time + RejectionSeconds;
            }
        }
    }
}
