using System.IO;
using PlantNursery.Game;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace PlantNursery.EditorTools
{
    /// <summary>
    /// Generates Assets/Scenes/Starter.unity: ground plot sized from content, SimHost, player spawn,
    /// directional light. Re-running rebuilds the scene. Menu: Plant Nursery/Generate Starter Scene.
    /// </summary>
    public static class StarterSceneGenerator
    {
        private const string ScenePath = "Assets/Scenes/Starter.unity";
        private const float CellSize = 0.5f;

        [MenuItem("Plant Nursery/Generate Starter Scene")]
        public static void Generate()
        {
            // Read plot size through the real loader so the scene matches content.
            var host = new GameObject("tmp").AddComponent<SimHost>();
            int w, d;
            try
            {
                host.Initialize();
                w = host.Content.Building.PlotWidth;
                d = host.Content.Building.PlotDepth;
            }
            finally { Object.DestroyImmediate(host.gameObject); }

            Directory.CreateDirectory("Assets/Scenes");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var simHost = new GameObject("SimHost");
            simHost.AddComponent<SimHost>();

            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            var size = new Vector3(w * CellSize, 0f, d * CellSize);
            // Plot origin (cell 0,0) at world origin, extending +x/+z. Plane is 10x10 units by default.
            ground.transform.position = new Vector3(size.x / 2f, 0f, size.z / 2f);
            ground.transform.localScale = new Vector3(size.x / 10f, 1f, size.z / 10f);
            var mat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Game/Greybox/Materials/Ground.mat");
            if (mat != null) ground.GetComponent<MeshRenderer>().sharedMaterial = mat;

            var spawn = new GameObject("PlayerSpawn");
            spawn.transform.position = new Vector3(size.x / 2f, 0f, 1f);
            spawn.transform.rotation = Quaternion.identity;

            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            camGo.AddComponent<Camera>();
            camGo.AddComponent<AudioListener>();
            camGo.transform.SetParent(spawn.transform, false);
            camGo.transform.localPosition = new Vector3(0f, 1.6f, 0f);

            var lightGo = new GameObject("Directional Light");
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.shadows = LightShadows.Soft;
            lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.Refresh();
            Debug.Log("Generated " + ScenePath + " (plot " + w + "x" + d + " cells)");
        }
    }
}
