using System.Collections.Generic;
using PlantNursery.Game;
using UnityEditor;
using UnityEngine;

namespace PlantNursery.EditorTools
{
    /// <summary>
    /// Generates GDD-coloured URP Lit materials and placeholder prefabs (docs/design/visuals.md, decision 0006).
    /// Idempotent: existing assets are updated in place so GUIDs and references survive.
    /// </summary>
    public static class GreyboxGenerator
    {
        private const string Root = "Assets/Game/Greybox";
        private const string MaterialDir = Root + "/Materials";
        private const string PrefabDir = Root + "/Prefabs";

        private const string Brown = "#8B5A2B", Green = "#4CAF50", Blue = "#2F7FD8", Yellow = "#F2C230",
            Purple = "#9C5BD1", Orange = "#E8833A", Grass = "#7A9A5A", Grey = "#BDBDBD",
            GhostValid = "#40FF4080", GhostInvalid = "#FF404080";

        private sealed class Spec
        {
            public string Name, Id, Material;
            public PlaceableLayer Layer;
            public int W, D;          // footprint (cells or units)
            public Vector3 Size;      // metres
        }

        private const float Cell = 0.5f, Unit = 0.1f;

        private static readonly Spec[] Specs =
        {
            // Floor items: size = cells * 0.5 m. Bench top at 0.8 m, 1 x 4 cells.
            new Spec { Name = "Bench", Id = "bench", Material = "Furniture", Layer = PlaceableLayer.Floor, W = 1, D = 4, Size = new Vector3(Cell, 0.8f, 4 * Cell) },
            new Spec { Name = "Tap", Id = "tap", Material = "Water", Layer = PlaceableLayer.Floor, W = 1, D = 1, Size = new Vector3(0.3f, 0.9f, 0.3f) },
            new Spec { Name = "Computer", Id = "computer", Material = "Building", Layer = PlaceableLayer.Floor, W = 2, D = 1, Size = new Vector3(2 * Cell, 0.8f, Cell) },
            new Spec { Name = "Bed", Id = "bed", Material = "Building", Layer = PlaceableLayer.Floor, W = 2, D = 4, Size = new Vector3(2 * Cell, 0.5f, 4 * Cell) },
            new Spec { Name = "DeliveryShelf", Id = "delivery_shelf", Material = "Building", Layer = PlaceableLayer.Floor, W = 1, D = 3, Size = new Vector3(Cell, 1.2f, 3 * Cell) },
            // Surface items: size = units * 0.1 m (building.md surface table).
            new Spec { Name = "PlugTray", Id = "plug_tray", Material = "Plant", Layer = PlaceableLayer.Surface, W = 2, D = 3, Size = new Vector3(2 * Unit, 0.05f, 3 * Unit) },
            new Spec { Name = "SmallPot", Id = "small_pot", Material = "Plant", Layer = PlaceableLayer.Surface, W = 1, D = 1, Size = new Vector3(Unit * 0.9f, 0.09f, Unit * 0.9f) },
            new Spec { Name = "MediumPot", Id = "medium_pot", Material = "Plant", Layer = PlaceableLayer.Surface, W = 2, D = 2, Size = new Vector3(0.17f, 0.15f, 0.17f) },
            new Spec { Name = "WateringCan", Id = "watering_can", Material = "Water", Layer = PlaceableLayer.Surface, W = 2, D = 3, Size = new Vector3(2 * Unit, 0.2f, 3 * Unit) },
            new Spec { Name = "SeedPacket", Id = "seed_packet", Material = "Plant", Layer = PlaceableLayer.Surface, W = 1, D = 2, Size = new Vector3(Unit, 0.01f, 2 * Unit) },
        };

        /// <summary>Menu entry: creates or updates all greybox materials and prefabs.</summary>
        [MenuItem("PlantNursery/Generate Greybox")]
        public static void Generate()
        {
            EnsureFolder("Assets/Game", "Greybox");
            EnsureFolder(Root, "Materials");
            EnsureFolder(Root, "Prefabs");

            var mats = new Dictionary<string, Material>
            {
                ["Furniture"] = Mat("Furniture", Brown, false),
                ["Plant"] = Mat("Plant", Green, false),
                ["Water"] = Mat("Water", Blue, false),
                ["Vehicle"] = Mat("Vehicle", Yellow, false),
                ["Customer"] = Mat("Customer", Purple, false),
                ["Vendor"] = Mat("Vendor", Orange, false),
                ["Ground"] = Mat("Ground", Grass, false),
                ["Building"] = Mat("Building", Grey, false),
                ["GhostValid"] = Mat("GhostValid", GhostValid, true),
                ["GhostInvalid"] = Mat("GhostInvalid", GhostInvalid, true),
            };

            foreach (var s in Specs) BuildPrefab(s, mats[s.Material]);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Greybox generated: " + Specs.Length + " prefabs, " + mats.Count + " materials.");
        }

        private static void BuildPrefab(Spec s, Material mat)
        {
            var root = new GameObject(s.Name);
            root.AddComponent<PlaceableAuthoring>().Configure(s.Id, s.Layer, s.W, s.D);

            var mesh = GameObject.CreatePrimitive(PrimitiveType.Cube);
            mesh.name = "Mesh";
            mesh.transform.SetParent(root.transform, false);
            mesh.transform.localScale = s.Size;
            // Pivot at the south-west corner on the ground: centre the mesh over the footprint.
            mesh.transform.localPosition = new Vector3(s.Size.x / 2f, s.Size.y / 2f, s.Size.z / 2f);
            mesh.GetComponent<MeshRenderer>().sharedMaterial = mat;

            PrefabUtility.SaveAsPrefabAsset(root, PrefabDir + "/" + s.Name + ".prefab");
            Object.DestroyImmediate(root);
        }

        private static Material Mat(string name, string hex, bool ghost)
        {
            string path = MaterialDir + "/" + name + ".mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(mat, path);
            }
            ColorUtility.TryParseHtmlString(hex, out Color c);
            mat.SetColor("_BaseColor", c);
            mat.color = c;
            mat.SetFloat("_Smoothness", 0.1f);
            if (ghost)
            {
                mat.SetFloat("_Surface", 1f); // transparent
                mat.SetFloat("_Blend", 0f);   // alpha
                mat.SetOverrideTag("RenderType", "Transparent");
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 0);
                mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            }
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static void EnsureFolder(string parent, string child)
        {
            if (!AssetDatabase.IsValidFolder(parent + "/" + child)) AssetDatabase.CreateFolder(parent, child);
        }
    }
}
