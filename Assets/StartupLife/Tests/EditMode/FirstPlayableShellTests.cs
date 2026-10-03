using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using StartupLife.Content;
using UnityEditor;

namespace StartupLife.Tests.EditMode
{
    public sealed class FirstPlayableShellTests
    {
        [Test]
        public void GeneratedSceneReopensWithSerializedContentAndControllerBindings()
        {
            var gate = AppDomain.CurrentDomain.GetAssemblies().Single(assembly => assembly.GetName().Name == "StartupLife.Editor")
                .GetType("StartupLife.Editor.FirstPlayableRuntimeGate", true);
            Assert.DoesNotThrow(() => gate.GetMethod("VerifyScene", BindingFlags.Public | BindingFlags.Static).Invoke(null, null));
        }

        [Test]
        public void GeneratedContentAssetImportsAndBuildsTheFirstPlayableCatalog()
        {
            var asset = AssetDatabase.LoadAssetAtPath<StartupLifeContentCatalogAsset>("Assets/StartupLife/Data/FirstPlayableContent.asset");
            Assert.That(asset, Is.Not.Null, "Run first-playable builders before the runtime gate.");
            var catalog = asset.BuildCatalog();
            Assert.That(catalog.Careers["developer"].EndMinute, Is.EqualTo(1020));
            Assert.That(catalog.Courses.ContainsKey("communication-basics"), Is.True);
        }
    }
}
