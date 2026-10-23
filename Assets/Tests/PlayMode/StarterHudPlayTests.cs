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

        [UnityTest]
        public IEnumerator HudLabelsHaveTextAndAreOnScreen()
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(ScenePath, new LoadSceneParameters(LoadSceneMode.Single));
            var t = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup - t < 1.5f) yield return null;

            var hud = Object.FindFirstObjectByType<InteractionHud>();
            Assert.IsNotNull(hud, "InteractionHud in Starter.unity");
            var root = hud.GetComponent<UIDocument>().rootVisualElement;
            var screen = root.panel.visualTree.worldBound;
            foreach (var name in new[] { "day-label", "season-label", "clock-label", "wallet-label" })
            {
                var label = root.Q<Label>(name);
                Assert.IsNotNull(label, name);
                Assert.IsFalse(string.IsNullOrEmpty(label.text), name + " text is empty");
                Assert.AreNotEqual(DisplayStyle.None, label.resolvedStyle.display, name + " display");
                Assert.Greater(label.worldBound.width, 0f, name + " width");
            }
            foreach (var name in new[] { "clock-label", "wallet-label" })
            {
                var wb = root.Q<Label>(name).worldBound;
                Assert.GreaterOrEqual(wb.xMin, screen.xMin, name + " left");
                Assert.GreaterOrEqual(wb.yMin, screen.yMin, name + " top");
                Assert.LessOrEqual(wb.xMax, screen.xMax + 0.5f, name + " right");
                Assert.LessOrEqual(wb.yMax, screen.yMax + 0.5f, name + " bottom");
            }
        }
    }
}
