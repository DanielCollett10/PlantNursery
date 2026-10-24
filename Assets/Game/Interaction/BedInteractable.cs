using PlantNursery.Sim.Building;
using PlantNursery.Sim.Core;
using PlantNursery.Sim.Time;
using UnityEngine;

namespace PlantNursery.Game
{
    /// <summary>
    /// The bed as an <see cref="IInteractable"/>. Finds the placed "bed" in the Sim's build state for its footprint and sends
    /// <see cref="SleepCommand"/> through the host's command sink. Whether sleeping is allowed (Evening/Night) is the Sim's call;
    /// a refusal comes back as NotTired for the HUD to show.
    /// </summary>
    public sealed class BedInteractable : MonoBehaviour, IInteractable
    {
        private const string BedItemId = "bed";

        [SerializeField] private SimHost host;
        [SerializeField] private Vector3 plotOrigin = Vector3.zero;

        /// <summary>Assign the host (scene generator and tests).</summary>
        public void Configure(SimHost simHost) { host = simHost; }

        private void OnEnable() { WorldInteractions.Register(this); }
        private void OnDisable() { WorldInteractions.Unregister(this); }

        /// <inheritdoc />
        public bool IsAvailable => isActiveAndEnabled && host != null && host.IsReady && IsSleepTime && FindBed() != null;

        // Only a display hint so the prompt matches the Sim's NotTired rule; the Sim still decides.
        private bool IsSleepTime => host.Clock.Phase == DayPhase.Evening || host.Clock.Phase == DayPhase.Night;

        /// <inheritdoc />
        public string PromptKey => "prompt.sleep";

        /// <inheritdoc />
        public float DistanceTo(Vector3 point)
        {
            var bed = FindBed();
            if (bed == null) return float.MaxValue;
            var min = plotOrigin + new Vector3(bed.Anchor.X * WorldLayout.CellSize, 0f, bed.Anchor.Z * WorldLayout.CellSize);
            var max = min + new Vector3(bed.Width * WorldLayout.CellSize, 0f, bed.Depth * WorldLayout.CellSize);
            var dx = Mathf.Max(min.x - point.x, 0f, point.x - max.x);
            var dz = Mathf.Max(min.z - point.z, 0f, point.z - max.z);
            return Mathf.Sqrt(dx * dx + dz * dz);
        }

        /// <inheritdoc />
        public CommandResult Interact(SimHost simHost)
        {
            var h = simHost != null ? simHost : host;
            return h.Submit(new SleepCommand(h.Session.LocalPlayer));
        }

        private PlacedItem FindBed()
        {
            foreach (var p in host.Build.PlacedItems) if (p.ItemId == BedItemId) return p;
            return null;
        }
    }
}
