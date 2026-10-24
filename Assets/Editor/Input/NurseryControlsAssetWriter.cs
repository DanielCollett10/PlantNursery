using System.IO;
using PlantNursery.Game.Controls;
using UnityEditor;

namespace PlantNursery.EditorTools
{
    /// <summary>Writes Assets/Input/NurseryControls.inputactions from <see cref="NurseryControlsBuilder"/>.</summary>
    public static class NurseryControlsAssetWriter
    {
        const string Path = "Assets/Input/NurseryControls.inputactions";

        /// <summary>Menu: regenerate the action asset (overwrites rebinds made in the asset editor).</summary>
        [MenuItem("PlantNursery/Input/Generate NurseryControls")]
        public static void Generate()
        {
            var asset = NurseryControlsBuilder.Build();
            File.WriteAllText(Path, asset.ToJson());
            UnityEngine.Object.DestroyImmediate(asset);
            AssetDatabase.ImportAsset(Path);
        }
    }
}
