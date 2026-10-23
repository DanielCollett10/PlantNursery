using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace PlantNursery.EditorTools
{
    /// <summary>
    /// Creates or updates the one shared PanelSettings asset used by every UI Toolkit document in the game.
    /// Assets/Game/UI/GamePanelSettings.asset uses ScaleWithScreenSize at 1920x1080 and points at
    /// Assets/Game/UI/UnityDefaultRuntimeTheme.tss. That file must be plain USS text that imports Unity's
    /// built-in theme (<see cref="ThemeUss"/>); a serialized ThemeStyleSheet YAML dump never draws anything.
    /// Called by <see cref="StarterSceneGenerator"/>, so Plant Nursery/Rebuild Greybox World keeps it in place.
    /// </summary>
    public static class GamePanelSettingsBuilder
    {
        /// <summary>Committed PanelSettings asset shared by the HUD and build panel.</summary>
        public const string PanelSettingsPath = "Assets/Game/UI/GamePanelSettings.asset";

        /// <summary>Committed theme stylesheet (USS text) that imports Unity's default runtime theme.</summary>
        public const string ThemePath = "Assets/Game/UI/UnityDefaultRuntimeTheme.tss";

        /// <summary>Exact USS text written to <see cref="ThemePath"/>. Never YAML.</summary>
        public const string ThemeUss = "@import url(\"unity-theme://default\");\n";

        private const string UiFolder = "Assets/Game/UI";
        private static readonly Vector2Int ReferenceResolution = new Vector2Int(1920, 1080);

        /// <summary>Writes the USS theme file if it is missing or wrong, then returns the configured PanelSettings.</summary>
        public static PanelSettings Ensure()
        {
            EnsureFolder(UiFolder);

            WriteThemeIfNeeded();
            var theme = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(ThemePath);
            if (theme == null)
            {
                Debug.LogError("[PlantNursery] " + ThemePath + " did not import as a theme stylesheet.");
                return null;
            }

            var panel = AssetDatabase.LoadAssetAtPath<PanelSettings>(PanelSettingsPath);
            if (panel == null)
            {
                panel = ScriptableObject.CreateInstance<PanelSettings>();
                AssetDatabase.CreateAsset(panel, PanelSettingsPath);
            }
            panel.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            panel.referenceResolution = ReferenceResolution;
            panel.themeStyleSheet = theme;
            EditorUtility.SetDirty(panel);
            AssetDatabase.SaveAssets();
            return panel;
        }

        /// <summary>Rewrites the theme file as plain USS text when it is missing or not exactly <see cref="ThemeUss"/>.</summary>
        private static void WriteThemeIfNeeded()
        {
            var fullPath = System.IO.Path.GetFullPath(ThemePath);
            if (System.IO.File.Exists(fullPath) && System.IO.File.ReadAllText(fullPath) == ThemeUss) return;

            System.IO.File.WriteAllText(fullPath, ThemeUss, new System.Text.UTF8Encoding(false));
            AssetDatabase.ImportAsset(ThemePath, ImportAssetOptions.ForceUpdate);
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
            var name = System.IO.Path.GetFileName(path);
            if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }
    }
}
