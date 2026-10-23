using System.IO;
using PlantNursery.Game;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace PlantNursery.EditorTools
{
    /// <summary>
    /// Generates Assets/Scenes/Starter.unity: ground plot sized from content, SimHost, player, world view, HUD,
    /// build mode and a day-night lit sun. Generates the greybox first if its prefabs are missing.
    /// Re-running rebuilds the scene. Menu: Plant Nursery/Generate Starter Scene.
    /// </summary>
    public static class StarterSceneGenerator
    {
        private const string ScenePath = "Assets/Scenes/Starter.unity";
        private const string PrefabDir = "Assets/Game/Greybox/Prefabs";
        private const string GhostValidPath = "Assets/Game/Greybox/Materials/GhostValid.mat";
        private const string GhostInvalidPath = "Assets/Game/Greybox/Materials/GhostInvalid.mat";
        private const string InputPath = "Assets/Input/PlayerControls.inputactions";
        private const float CellSize = 0.5f;

        [MenuItem("Plant Nursery/Generate Starter Scene")]
        public static void Generate()
        {
            if (!HasPrefabs()) GreyboxGenerator.Generate();
            if (!AssetsPresent()) return;

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

            // Player: CharacterController body, camera at eye height, interaction, HUD and held item view.
            var player = new GameObject("Player");
            player.transform.SetPositionAndRotation(spawn.transform.position, spawn.transform.rotation);
            player.AddComponent<CharacterController>();
            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = camGo.AddComponent<Camera>();
            camGo.AddComponent<AudioListener>();
            camGo.transform.SetParent(player.transform, false);
            camGo.transform.localPosition = new Vector3(0f, FirstPersonController.EyeHeight, 0f);
            var input = AssetDatabase.LoadAssetAtPath<UnityEngine.InputSystem.InputActionAsset>(InputPath);
            player.AddComponent<FirstPersonController>().Configure(input, camGo.transform);
            var interaction = player.AddComponent<InteractionController>();
            interaction.Configure(simHost.GetComponent<SimHost>(), cam, input);
            var prefabs = new System.Collections.Generic.List<PlaceableAuthoring>();
            foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { PrefabDir }))
            {
                var p = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
                var a = p != null ? p.GetComponent<PlaceableAuthoring>() : null;
                if (a != null) prefabs.Add(a);
            }
            var worldGo = new GameObject("WorldView");
            worldGo.AddComponent<WorldView>().Configure(simHost.GetComponent<SimHost>(), prefabs);
            var heldGo = new GameObject("HeldItem");
            heldGo.transform.SetParent(camGo.transform, false);
            heldGo.AddComponent<HeldItemView>().Configure(interaction, prefabs);
            var hud = new GameObject("HUD");
            hud.AddComponent<UnityEngine.UIElements.UIDocument>();
            hud.AddComponent<InteractionHud>().Configure(interaction);

            var buildGo = new GameObject("BuildMode");
            buildGo.AddComponent<UnityEngine.UIElements.UIDocument>();
            buildGo.AddComponent<BuildModeController>().Configure(simHost.GetComponent<SimHost>(), cam, input, prefabs,
                AssetDatabase.LoadAssetAtPath<Material>(GhostValidPath),
                AssetDatabase.LoadAssetAtPath<Material>(GhostInvalidPath));

            var lightGo = new GameObject("Directional Light");
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.shadows = LightShadows.Soft;
            lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            // Add and wire DayNightLighting on the sun so regenerating never drops lighting.
            DayNightLightingSetup.Apply();

            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.Refresh();
            Debug.Log("Generated " + ScenePath + " (plot " + w + "x" + d + " cells)");
        }

        private static bool HasPrefabs()
        {
            return AssetDatabase.IsValidFolder(PrefabDir)
                && AssetDatabase.FindAssets("t:Prefab", new[] { PrefabDir }).Length > 0;
        }

        private static bool AssetsPresent()
        {
            string missing = null;
            if (!HasPrefabs()) missing = PrefabDir + " (no prefabs)";
            else if (AssetDatabase.LoadAssetAtPath<Material>(GhostValidPath) == null) missing = GhostValidPath;
            else if (AssetDatabase.LoadAssetAtPath<Material>(GhostInvalidPath) == null) missing = GhostInvalidPath;
            else if (AssetDatabase.LoadAssetAtPath<UnityEngine.InputSystem.InputActionAsset>(InputPath) == null) missing = InputPath;
            if (missing == null) return true;
            Debug.LogError("[PlantNursery] Cannot generate Starter scene, missing asset: " + missing);
            return false;
        }
    }
}
