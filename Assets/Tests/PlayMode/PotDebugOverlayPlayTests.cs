using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace PlantNursery.Game.PlayModeTests
{
    /// <summary>The F1 pot overlay finds a container under the mouse over every bench pot in Starter.unity (T-184).</summary>
    public sealed class PotDebugOverlayPlayTests
    {
        private const string ScenePath = "Assets/Scenes/Starter.unity";

        [UnityTest]
        public IEnumerator MouseOverEachContainerViewShowsLabel()
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(ScenePath, new LoadSceneParameters(LoadSceneMode.Single));
            var host = Object.FindFirstObjectByType<SimHost>();
            var deadline = Time.realtimeSinceStartup + 15f;
            while ((host == null || !host.IsReady) && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
                if (host == null) host = Object.FindFirstObjectByType<SimHost>();
            }
            Assert.IsTrue(host != null && host.IsReady, "SimHost ready");
            for (var i = 0; i < 5; i++) yield return null; // let WorldView.LateUpdate build the views

            var overlay = Object.FindFirstObjectByType<PotDebugOverlay>();
            var world = Object.FindFirstObjectByType<WorldView>();
            Assert.IsNotNull(overlay, "PotDebugOverlay in Starter.unity");
            Assert.IsNotNull(world, "WorldView in Starter.unity");
            var cam = Camera.main;
            Assert.IsNotNull(cam, "main camera");

            overlay.Visible = true;
            Assert.AreEqual("Pot debug ON (F1)", overlay.IndicatorLabel);

            var views = new List<KeyValuePair<int, GameObject>>(world.ContainerViews);
            Assert.GreaterOrEqual(views.Count, 6, "the six bench slot containers have views");
            foreach (var kv in views)
            {
                var rend = kv.Value.GetComponentInChildren<Renderer>();
                Assert.IsNotNull(rend, "renderer for container " + kv.Key);
                var sp = cam.WorldToScreenPoint(rend.bounds.center);
                overlay.Refresh(new Vector2(sp.x, sp.y));
                Assert.IsNotNull(overlay.CurrentText, "label for container " + kv.Key + " at " + sp);
            }

            overlay.Visible = false;
            Assert.IsNull(overlay.IndicatorLabel);
        }
    }
}
