using NUnit.Framework;
using StartupLife.Content;
using UnityEngine;

namespace StartupLife.Tests.EditMode
{
    public sealed class ContentCatalogTests
    {
        [Test]
        public void FirstPlayableTemplateBuildsThroughContentAssembly()
        {
            var catalog = FirstPlayableContentTemplate.BuildCatalog();
            Assert.That(catalog.Version, Is.EqualTo("first-playable.v1"));
            Assert.That(catalog.Skills.Count, Is.EqualTo(6));
            Assert.That(catalog.Careers.ContainsKey("developer"), Is.True);
            Assert.That(catalog.Courses.ContainsKey("communication-basics"), Is.True);
        }

        [Test]
        public void ScriptableObjectWrapperBuildsDetachedCatalog()
        {
            var asset = ScriptableObject.CreateInstance<StartupLifeContentCatalogAsset>();
            try
            {
                var source = FirstPlayableContentTemplate.Create();
                asset.ReplaceSourceForAuthoring(source);
                var catalog = asset.BuildCatalog();

                source.Starts[0].Cash = 1;
                source.Skills[0].Thresholds[0] = 999999;

                Assert.That(catalog.Starts["fresh"].Cash, Is.EqualTo(3000000L));
                Assert.That(catalog.Skills["communication"].Thresholds[0], Is.EqualTo(100L));
            }
            finally
            {
                Object.DestroyImmediate(asset);
            }
        }

        [Test]
        public void InvalidAuthoringDataFailsBeforeRuntimeCatalogPublication()
        {
            var source = FirstPlayableContentTemplate.Create();
            source.Careers[0].Scenes[0].SkillId = "missing-skill";
            Assert.Throws<System.ArgumentException>(() => source.Build());
        }
    }
}
