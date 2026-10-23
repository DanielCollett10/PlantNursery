using PlantNursery.Game;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace PlantNursery.EditorTools
{
    /// <summary>
    /// Menu tool that makes the open scene day-night lit: finds or creates the directional "Sun" light,
    /// adds <see cref="DayNightLighting"/> to it and points it at the scene's <see cref="SimHost"/>.
    /// Safe to run more than once.
    /// </summary>
    public static class DayNightLightingSetup
    {
        /// <summary>Menu entry: Plant Nursery/Setup Day-Night Lighting.</summary>
        [MenuItem("Plant Nursery/Setup Day-Night Lighting")]
        public static void Apply()
        {
            var sunLight = FindDirectionalLight();
            if (sunLight == null)
            {
                var go = new GameObject("Sun");
                sunLight = go.AddComponent<Light>();
                sunLight.type = LightType.Directional;
                sunLight.shadows = LightShadows.Soft;
                go.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
                Undo.RegisterCreatedObjectUndo(go, "Create Sun");
            }

            var lighting = sunLight.GetComponent<DayNightLighting>();
            if (lighting == null) lighting = Undo.AddComponent<DayNightLighting>(sunLight.gameObject);

            var host = Object.FindObjectOfType<SimHost>();
            var serialized = new SerializedObject(lighting);
            serialized.FindProperty("host").objectReferenceValue = host;
            serialized.FindProperty("sun").objectReferenceValue = sunLight;
            serialized.ApplyModifiedProperties();

            EditorSceneManager.MarkSceneDirty(sunLight.gameObject.scene);
            Debug.Log(host == null
                ? "[PlantNursery] Day-night lighting added to Sun, but no SimHost is in the scene yet."
                : "[PlantNursery] Day-night lighting wired to SimHost. Save the scene.");
        }

        private static Light FindDirectionalLight()
        {
            foreach (var light in Object.FindObjectsOfType<Light>())
                if (light.type == LightType.Directional) return light;
            return null;
        }
    }
}
