using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace PlantNursery.EditorTools
{
    /// <summary>
    /// Creates or updates the one shared PanelSettings asset used by every UI Toolkit document in the game.
    /// Assets/Game/UI/GamePanelSettings.asset uses ScaleWithScreenSize at 1920x1080 and points at
    /// Assets/Game/UI/UnityDefaultRuntimeTheme.tss, a committed copy of Unity's default runtime theme.
    /// Called by <see cref="StarterSceneGenerator"/>, so Plant Nursery/Rebuild Greybox World keeps it in place.
    /// </summary>
    public static class GamePanelSettingsBuilder
    {
        /// <summary>Committed PanelSettings asset shared by the HUD and build panel.</summary>
        public const string PanelSettingsPath = "Assets/Game/UI/GamePanelSettings.asset";

        /// <summary>Committed copy of Unity's default runtime theme.</summary>
        public const string ThemePath = "Assets/Game/UI/UnityDefaultRuntimeTheme.tss";

        private const string UiFolder = "Assets/Game/UI";
        private static readonly Vector2Int ReferenceResolution = new Vector2Int(1920, 1080);

        /// <summary>Creates the theme and PanelSettings assets if missing and returns the configured PanelSettings.</summary>
        public static PanelSettings Ensure()
        {
            EnsureFolder(UiFolder);

            var theme = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(ThemePath);
            if (theme == null)
            {
                // A fresh PanelSettings references Unity's default runtime theme; copy it into the project.
                var defaults = ScriptableObject.CreateInstance<PanelSettings>();
                var source = defaults.themeStyleSheet;
                Object.DestroyImmediate(defaults);
                if (source == null)
                {
                    Debug.LogError("[PlantNursery] Could not read Unity's default runtime theme.");
                    return null;
                }
                theme = Object.Instantiate(source);
                AssetDatabase.CreateAsset(theme, ThemePath);
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
