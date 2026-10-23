using System;
using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace PlantNursery.Game.Tests
{
    /// <summary>
    /// Guards the asmdef platform lists. An assembly whose only platform is Editor is classed by Unity as
    /// EditMode, which runs PlayMode tests without a player loop. The PlayMode assembly must not be Editor-only.
    /// </summary>
    public sealed class TestAssemblyPlatformTests
    {
        [Serializable]
        private sealed class AsmdefFile
        {
            public string name;
            public string[] includePlatforms;
        }

        [TestCase("Tests/PlayMode/PlantNursery.Game.PlayModeTests.asmdef")]
        [TestCase("Game/PlantNursery.Game.asmdef")]
        public void AssemblyIsNotEditorOnly(string assetsRelativePath)
        {
            var path = Path.Combine(Application.dataPath, assetsRelativePath);
            Assert.IsTrue(File.Exists(path), "Missing asmdef: " + path);

            var asmdef = JsonUtility.FromJson<AsmdefFile>(File.ReadAllText(path));
            var platforms = asmdef.includePlatforms ?? new string[0];
            bool editorOnly = platforms.Length == 1 && platforms[0] == "Editor";

            Assert.IsFalse(editorOnly,
                asmdef.name + " has Editor as its only includePlatforms entry, so Unity classes it as EditMode.");
        }
    }
}
