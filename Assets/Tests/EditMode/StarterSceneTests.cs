using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;

namespace PlantNursery.Game.Tests
{
    /// <summary>Checks the committed Starter scene is fully generated and wired (decision 0008).</summary>
    public class StarterSceneTests
    {
        private UnityEngine.SceneManagement.Scene _scene;

        [SetUp]
        public void Open() => _scene = EditorSceneManager.OpenScene("Assets/Scenes/Starter.unity", OpenSceneMode.Single);

        private static SerializedProperty Prop(Object o, string name)
        {
            var p = new SerializedObject(o).FindProperty(name);
            Assert.IsNotNull(p, o.GetType().Name + "." + name);
            return p;
        }

        [Test]
        public void PlayerIsConfigured()
        {
            var player = GameObject.Find("Player");
            Assert.IsNotNull(player);
            Assert.IsNotNull(player.GetComponent<CharacterController>());
            var fpc = player.GetComponent<FirstPersonController>();
            Assert.IsNotNull(fpc);
            Assert.IsNotNull(Prop(fpc, "actions").objectReferenceValue, "actions");
            Assert.IsNotNull(Prop(fpc, "cameraPivot").objectReferenceValue, "camera");
            Assert.IsNotNull(player.GetComponent<InteractionController>());
        }

        [Test]
        public void WorldIsWired()
        {
            Assert.IsNotNull(Object.FindFirstObjectByType<SimHost>());
            var view = Object.FindFirstObjectByType<WorldView>();
            Assert.IsNotNull(view);
            Assert.Greater(Prop(view, "prefabs").arraySize, 0);
        }

        [Test]
        public void HudAndBuildModeExist()
        {
            var hud = Object.FindFirstObjectByType<InteractionHud>();
            Assert.IsNotNull(hud);
            Assert.IsNotNull(hud.GetComponent<UIDocument>());
            var build = Object.FindFirstObjectByType<BuildModeController>();
            Assert.IsNotNull(build);
            Assert.IsNotNull(Prop(build, "validMaterial").objectReferenceValue);
            Assert.IsNotNull(Prop(build, "invalidMaterial").objectReferenceValue);
        }

        [Test]
        public void SunHasDayNightLighting()
        {
            DayNightLighting found = null;
            foreach (var l in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
                if (l.type == LightType.Directional) found = l.GetComponent<DayNightLighting>();
            Assert.IsNotNull(found, "directional light with DayNightLighting");
        }
    }
}
