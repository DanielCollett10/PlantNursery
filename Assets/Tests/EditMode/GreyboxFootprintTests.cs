using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using PlantNursery.Game;
using UnityEditor;
using UnityEngine;

namespace PlantNursery.Game.Tests
{
    /// <summary>
    /// Checks every generated greybox placeable matches its item footprint in items.json:
    /// floor prefabs are cells * 0.5 m, surface prefabs are units * 0.1 m (decision 0006).
    /// This catches prefabs left stale after content or generator changes (T-061).
    /// </summary>
    public class GreyboxFootprintTests
    {
        private const string PrefabDir = "Assets/Game/Greybox/Prefabs";
        private const string ItemsPath = "Assets/StreamingAssets/Content/items.json";
        private const float FloorCell = 0.5f, SurfaceUnit = 0.1f, Tolerance = 0.001f;

        [Test]
        public void EveryGreyboxPrefabMatchesItemFootprint()
        {
            var items = LoadItemFootprints();
            var guids = AssetDatabase.FindAssets("t:Prefab", new[] { PrefabDir });
            Assert.Greater(guids.Length, 0, "no greybox prefabs under " + PrefabDir);

            var problems = new StringBuilder();
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                var auth = prefab != null ? prefab.GetComponent<PlaceableAuthoring>() : null;
                if (auth == null) continue;

                if (!items.TryGetValue(auth.ItemId, out var fp))
                {
                    problems.AppendLine(path + ": itemId '" + auth.ItemId + "' is not in items.json");
                    continue;
                }

                float cell = auth.Layer == PlaceableLayer.Floor ? FloorCell : SurfaceUnit;
                float expectX = fp.w * cell, expectZ = fp.d * cell;
                var mesh = prefab.transform.Find("Mesh");
                if (mesh == null)
                {
                    problems.AppendLine(path + ": no child 'Mesh'");
                    continue;
                }

                var s = mesh.localScale;
                if (Mathf.Abs(s.x - expectX) > Tolerance || Mathf.Abs(s.z - expectZ) > Tolerance)
                    problems.AppendLine(string.Format("{0} ({1}): size {2:F3} x {3:F3} m, expected {4:F3} x {5:F3} m from footprint {6}x{7}",
                        path, auth.ItemId, s.x, s.z, expectX, expectZ, fp.w, fp.d));
            }

            Assert.IsEmpty(problems.ToString(),
                "Greybox prefabs out of date. Run Plant Nursery/Rebuild Greybox World:\n" + problems);
        }

        [Test]
        public void BenchPrefabIsTwoByFourCells()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabDir + "/Bench.prefab");
            Assert.IsNotNull(prefab, "Bench.prefab");
            var auth = prefab.GetComponent<PlaceableAuthoring>();
            Assert.AreEqual("bench_standard", auth.ItemId);
            var s = prefab.transform.Find("Mesh").localScale;
            Assert.AreEqual(1f, s.x, Tolerance, "bench width (2 cells)");
            Assert.AreEqual(2f, s.z, Tolerance, "bench depth (4 cells)");
        }

        private struct Footprint { public int w, d; }

        private static Dictionary<string, Footprint> LoadItemFootprints()
        {
            var json = JObject.Parse(File.ReadAllText(Path.GetFullPath(ItemsPath)));
            var result = new Dictionary<string, Footprint>();
            foreach (var item in (JArray)json["items"])
            {
                var layer = (string)item["layer"];
                var key = layer == "Surface" ? "surfaceFootprint" : "floorFootprint";
                var fp = item[key];
                if (fp == null) continue;
                result[(string)item["id"]] = new Footprint { w = (int)fp["w"], d = (int)fp["d"] };
            }
            return result;
        }
    }
}
