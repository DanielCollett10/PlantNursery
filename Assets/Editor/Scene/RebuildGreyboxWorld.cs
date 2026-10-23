using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace PlantNursery.EditorTools
{
    /// <summary>
    /// One command that regenerates every generated asset in the right order (decision 0008).
    /// </summary>
    public static class RebuildGreyboxWorld
    {
        /// <summary>Menu entry: URP (if missing), greybox materials and prefabs, Starter scene with lighting, save.</summary>
        [MenuItem("Plant Nursery/Rebuild Greybox World")]
        public static void Rebuild()
        {
            if (GraphicsSettings.defaultRenderPipeline == null) UrpProjectSetup.Apply();
            GreyboxGenerator.Generate();
            StarterSceneGenerator.Generate();
            AssetDatabase.SaveAssets();
            Debug.Log("[PlantNursery] Rebuild Greybox World complete.");
        }
    }
}
