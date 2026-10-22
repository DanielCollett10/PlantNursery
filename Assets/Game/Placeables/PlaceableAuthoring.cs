using UnityEngine;

namespace PlantNursery.Game
{
    /// <summary>Which placement grid a placeable uses.</summary>
    public enum PlaceableLayer
    {
        /// <summary>0.5 m floor cells.</summary>
        Floor,
        /// <summary>0.1 m surface mini-grid units.</summary>
        Surface
    }

    /// <summary>
    /// Root component of every placeable prefab: the Sim item id and footprint.
    /// The prefab pivot is the footprint's south-west corner (rotation 0); the placeholder mesh is a child.
    /// Holds no game rules; the Sim owns the real footprint data.
    /// </summary>
    public sealed class PlaceableAuthoring : MonoBehaviour
    {
        [SerializeField] private string itemId;
        [SerializeField] private PlaceableLayer layer;
        [SerializeField] private int footprintWidth = 1;
        [SerializeField] private int footprintDepth = 1;

        /// <summary>Sim item id.</summary>
        public string ItemId => itemId;
        /// <summary>Grid layer used by the footprint.</summary>
        public PlaceableLayer Layer => layer;
        /// <summary>Footprint width (x) in cells (Floor) or units (Surface).</summary>
        public int FootprintWidth => footprintWidth;
        /// <summary>Footprint depth (z) in cells (Floor) or units (Surface).</summary>
        public int FootprintDepth => footprintDepth;

        /// <summary>Sets the authoring data (used by the Editor generator).</summary>
        public void Configure(string id, PlaceableLayer placeableLayer, int width, int depth)
        {
            itemId = id;
            layer = placeableLayer;
            footprintWidth = width;
            footprintDepth = depth;
        }
    }
}
