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

            // The HUD only writes its text once the Sim is running, so wait for SimHost.IsReady first.
            var host = Object.FindFirstObjectByType<SimHost>();
            var deadline = Time.realtimeSinceStartup + 15f;
            while ((host == null || !host.IsReady) && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
                if (host == null) host = Object.FindFirstObjectByType<SimHost>();
            }
            Assert.IsNotNull(host, "SimHost in Starter.unity");
            Assert.IsTrue(host.IsReady, "SimHost.IsReady within 15s");
            // Let InteractionHud.Update run a few times and UI Toolkit lay the labels out.
            for (var i = 0; i < 5; i++) yield return null;

            var hud = Object.FindFirstObjectByType<InteractionHud>();
            Assert.IsNotNull(hud, "InteractionHud in Starter.unity");
            var root = hud.GetComponent<UIDocument>().rootVisualElement;
            Assert.IsNotNull(root.panel, "HUD root has no panel");
            var screen = root.panel.visualTree.worldBound;
            Assert.Greater(screen.width, 0f, "panel width");
            Assert.Greater(screen.height, 0f, "panel height");

            foreach (var name in new[] { "day-label", "season-label", "clock-label", "wallet-label" })
            {
                var label = root.Q<Label>(name);
                Assert.IsNotNull(label, name + " exists");
                Assert.IsFalse(string.IsNullOrEmpty(label.text), name + " text is empty");
                Assert.AreNotEqual(DisplayStyle.None, label.resolvedStyle.display, name + " display");
                var wb = label.worldBound;
                Assert.Greater(wb.width, 0f, name + " width");
                Assert.Greater(wb.height, 0f, name + " height");
                Assert.GreaterOrEqual(wb.xMin, screen.xMin - 0.5f, name + " left edge on screen");
                Assert.GreaterOrEqual(wb.yMin, screen.yMin - 0.5f, name + " top edge on screen");
                Assert.LessOrEqual(wb.xMax, screen.xMax + 0.5f, name + " right edge on screen");
                Assert.LessOrEqual(wb.yMax, screen.yMax + 0.5f, name + " bottom edge on screen");
            }
        }
    }
}
