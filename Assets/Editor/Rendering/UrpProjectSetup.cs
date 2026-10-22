#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace PlantNursery.EditorTools
{
    /// <summary>
    /// Creates the URP pipeline asset and renderer data under Assets/Settings (if missing)
    /// and assigns the pipeline in Graphics settings and every Quality level.
    /// Runs once automatically when no pipeline is assigned; also available from the menu.
    /// </summary>
    [InitializeOnLoad]
    internal static class UrpProjectSetup
    {
        const string Folder = "Assets/Settings";
        const string RendererPath = Folder + "/UniversalRP-Renderer.asset";
        const string PipelinePath = Folder + "/UniversalRP-Asset.asset";

        static UrpProjectSetup()
        {
            EditorApplication.delayCall += () =>
            {
                if (GraphicsSettings.defaultRenderPipeline == null)
                    Apply();
            };
        }

        [MenuItem("PlantNursery/Setup URP")]
        static void Apply()
        {
            if (!AssetDatabase.IsValidFolder(Folder))
                AssetDatabase.CreateFolder("Assets", "Settings");

            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelinePath);
            if (pipeline == null)
            {
                var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererPath);
                if (renderer == null)
                {
                    renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
                    AssetDatabase.CreateAsset(renderer, RendererPath);
                }
                pipeline = UniversalRenderPipelineAsset.Create(renderer);
                AssetDatabase.CreateAsset(pipeline, PipelinePath);
            }

            GraphicsSettings.defaultRenderPipeline = pipeline;

            var current = QualitySettings.GetQualityLevel();
            for (var i = 0; i < QualitySettings.names.Length; i++)
            {
                QualitySettings.SetQualityLevel(i, false);
                QualitySettings.renderPipeline = pipeline;
            }
            QualitySettings.SetQualityLevel(current, false);

            AssetDatabase.SaveAssets();
            Debug.Log("[PlantNursery] URP pipeline asset assigned in Graphics and Quality settings.");
        }
    }
}
#endif
