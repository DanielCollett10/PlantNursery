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

            Directory.CreateDirectory("Assets/Scenes");            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

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
            var input = AssetDatabase.LoadAssetAtPath<UnityEngine.InputSystem.InputActionAsset>("Assets/Input/PlayerControls.inputactions");
            player.AddComponent<FirstPersonController>().Configure(input, camGo.transform);
            var interaction = player.AddComponent<InteractionController>();
            interaction.Configure(simHost.GetComponent<SimHost>(), cam, input);
            var prefabs = new System.Collections.Generic.List<PlaceableAuthoring>();
            foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Game/Greybox/Prefabs" }))
            {
                var p = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
                var a = p != null ? p.GetComponent<PlaceableAuthoring>() : null;
                if (a != null) prefabs.Add(a);
            }
            var worldGo = new GameObject("WorldView");
            worldGo.AddComponent<WorldView>().Configure(simHost.GetComponent<SimHost>(), prefabs);
            var heldGo =new GameObject("HeldItem");
            heldGo.transform.SetParent(camGo.transform, false);
            heldGo.AddComponent<HeldItemView>().Configure(interaction, prefabs);
            var hud = new GameObject("HUD");
            hud.AddComponent<UnityEngine.UIElements.UIDocument>();
            hud.AddComponent<InteractionHud>().Configure(interaction);

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
