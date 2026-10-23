using System;
using System.Collections.Generic;
using UnityEngine;

namespace PlantNursery.Game
{
    /// <summary>
    /// Data-friendly string table loaded from Resources/UI/strings.json (key/value entries).
    /// All HUD and shop text goes through here so it can be edited or translated without code changes.
    /// </summary>
    public static class UiStrings
    {
        [Serializable] private sealed class Entry { public string key; public string value; }
        [Serializable] private sealed class Table { public List<Entry> entries; }

        private static Dictionary<string, string> _map;

        /// <summary>Parse a table from JSON (used by tests and by <see cref="Get"/>).</summary>
        public static Dictionary<string, string> Parse(string json)
        {
            var map = new Dictionary<string, string>();
            var table = JsonUtility.FromJson<Table>(json);
            if (table?.entries != null)
                foreach (var e in table.entries) if (!string.IsNullOrEmpty(e.key)) map[e.key] = e.value;
            return map;
        }

        /// <summary>Text for <paramref name="key"/> formatted with <paramref name="args"/>; the key itself if missing.</summary>
        public static string Get(string key, params object[] args)
        {
            if (_map == null)
            {
                var asset = Resources.Load<TextAsset>("UI/strings");
                _map = asset != null ? Parse(asset.text) : new Dictionary<string, string>();
            }
            if (!_map.TryGetValue(key, out var text)) return key;
            return args == null || args.Length == 0 ? text : string.Format(text, args);
        }

        /// <summary>True if the table has <paramref name="key"/>.</summary>
        public static bool Has(string key)
        {
            Get(key);
            return _map.ContainsKey(key);
        }
    }
}
