using System.Collections;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace PlantNursery.Game.PlayModeTests
{
    /// <summary>Proves the HUD actually draws in Play mode on Starter.unity (T-049): its labels exist and lay out.</summary>
    public sealed class StarterHudPlayTests
    {
        private const string ScenePath = "Assets/Scenes/Starter.unity";

        [UnityTest]
        public IEnumerator HudRootHasChildrenAndResolvedLayout()
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(ScenePath, new LoadSceneParameters(LoadSceneMode.Single));
            // UI Toolkit resolves layout on a panel update, so give it a few frames.
            for (var i = 0; i < 5; i++) yield return null;

            var hud = Object.FindFirstObjectByType<InteractionHud>();
            Assert.IsNotNull(hud, "InteractionHud in Starter.unity");
            var root = hud.GetComponent<UIDocument>().rootVisualElement;
            Assert.Greater(root.childCount, 0, "HUD rootVisualElement has no children");
            Assert.Greater(root.layout.width, 0f, "HUD root layout width");
            Assert.Greater(root.layout.height, 0f, "HUD root layout height");

            var day = root.Q<Label>("day-label");
            Assert.IsNotNull(day, "day-label");
            Assert.Greater(day.resolvedStyle.width, 0f, "day-label resolved width");
            Assert.Greater(day.resolvedStyle.height, 0f, "day-label resolved height");
        }
    }
}
