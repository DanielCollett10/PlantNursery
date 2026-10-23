using System.Collections.Generic;
using UnityEngine;

namespace PlantNursery.Game
{
    /// <summary>
    /// Shows the sim's held item in front of the camera. Uses the greybox prefab with a matching
    /// <see cref="PlaceableAuthoring.ItemId"/> when assigned, otherwise a small cube.
    /// </summary>
    public sealed class HeldItemView : MonoBehaviour
    {
        [SerializeField] private InteractionController interaction;
        [SerializeField] private List<PlaceableAuthoring> prefabs = new List<PlaceableAuthoring>();
        [SerializeField] private Vector3 offset = new Vector3(0.3f, -0.3f, 0.6f);

        private string _shownId = string.Empty;
        private GameObject _instance;

        /// <summary>Assign references (used by the scene generator).</summary>
        public void Configure(InteractionController controller, List<PlaceableAuthoring> placeablePrefabs)
        {
            interaction = controller; prefabs = placeablePrefabs;
        }

        private void LateUpdate()
        {
            if (interaction == null) return;
            var id = interaction.HeldItemId;
            if (id == _shownId) return;
            _shownId = id;
            if (_instance != null) Destroy(_instance);
            _instance = string.IsNullOrEmpty(id) ? null : Build(id);
        }

        private GameObject Build(string id)
        {
            GameObject go = null;
            foreach (var p in prefabs)
                if (p != null && p.ItemId == id) { go = Instantiate(p.gameObject); break; }
            if (go == null)
            {
                go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.transform.localScale = Vector3.one * 0.15f;
            }
            foreach (var c in go.GetComponentsInChildren<Collider>()) Destroy(c);
            go.name = "Held_" + id;
            go.transform.SetParent(transform, false);
            go.transform.localPosition = offset;
            return go;
        }
    }
}
