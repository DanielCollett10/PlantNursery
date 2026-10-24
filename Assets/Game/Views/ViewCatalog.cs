using System;
using System.Collections.Generic;
using UnityEngine;

namespace PlantNursery.Game
{
    /// <summary>
    /// Maps sim definition ids (item ids) to view prefabs (decision 0014, "Swappable visuals"). Switching an item to final art is
    /// one entry; unknown ids get the greybox fallback prefab. Holds no rules.
    /// </summary>
    [CreateAssetMenu(menuName = "Plant Nursery/View Catalog", fileName = "ViewCatalog")]
    public sealed class ViewCatalog : ScriptableObject
    {
        /// <summary>One definition id and the prefab that shows it.</summary>
        [Serializable]
        public struct Entry
        {
            /// <summary>Sim definition id (item id).</summary>
            public string id;
            /// <summary>View prefab.</summary>
            public GameObject prefab;
        }

        [SerializeField] private List<Entry> entries = new List<Entry>();
        [SerializeField] private GameObject fallback;

        private Dictionary<string, GameObject> _lookup;

        /// <summary>The prefab used for ids with no entry.</summary>
        public GameObject Fallback => fallback;
        /// <summary>Number of explicit entries.</summary>
        public int Count => entries.Count;

        /// <summary>Set the contents (used by the Editor generator and tests).</summary>
        public void Configure(IEnumerable<Entry> newEntries, GameObject fallbackPrefab)
        {
            entries = new List<Entry>(newEntries);
            fallback = fallbackPrefab;
            _lookup = null;
        }

        /// <summary>True if the id has its own prefab (not the fallback).</summary>
        public bool Has(string id) => id != null && Lookup().ContainsKey(id);

        /// <summary>The prefab for a definition id, or the fallback prefab if the id is unknown. Null only if no fallback is set.</summary>
        public GameObject PrefabFor(string id)
            => id != null && Lookup().TryGetValue(id, out var p) ? p : fallback;

        private Dictionary<string, GameObject> Lookup()
        {
            if (_lookup != null) return _lookup;
            _lookup = new Dictionary<string, GameObject>();
            foreach (var e in entries)
                if (!string.IsNullOrEmpty(e.id) && e.prefab != null) _lookup[e.id] = e.prefab;
            return _lookup;
        }

        private void OnValidate() { _lookup = null; }
    }
}
