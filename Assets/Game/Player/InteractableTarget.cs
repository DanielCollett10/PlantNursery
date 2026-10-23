using PlantNursery.Sim.Hands;
using UnityEngine;

namespace PlantNursery.Game
{
    /// <summary>
    /// Put on a collider (or its parent) so the raycast can describe it in sim terms.
    /// Views set the sim ids when they spawn; this holds no rules.
    /// </summary>
    public sealed class InteractableTarget : MonoBehaviour
    {
        [SerializeField] private TargetKind kind = TargetKind.SurfaceItem;
        [SerializeField] private int surfaceItemId;
        [SerializeField] private int cellIndex = -1;

        /// <summary>Kind of target.</summary>
        public TargetKind Kind => kind;
        /// <summary>Sim surface item id.</summary>
        public int SurfaceItemId => surfaceItemId;
        /// <summary>Container cell, or -1 for the whole item.</summary>
        public int CellIndex => cellIndex;

        /// <summary>Set the sim ids (called by views).</summary>
        public void Configure(TargetKind targetKind, int itemId = 0, int cell = -1)
        {
            kind = targetKind; surfaceItemId = itemId; cellIndex = cell;
        }

        /// <summary>This target as a sim <see cref="InteractTarget"/>.</summary>
        public InteractTarget ToSim() => new InteractTarget(kind, surfaceItemId, cellIndex);
    }
}
