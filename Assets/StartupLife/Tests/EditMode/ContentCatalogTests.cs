using NUnit.Framework;
using StartupLife.Core;
using UnityEditor;
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
            Assert.That(catalog.Careers.Count, Is.EqualTo(6));
            Assert.That(catalog.Careers.ContainsKey("developer"), Is.True);
            Assert.That(catalog.Careers.ContainsKey("marketing"), Is.True);
            Assert.That(catalog.Careers.ContainsKey("sales"), Is.True);
            Assert.That(catalog.Careers.ContainsKey("office-admin"), Is.True);
            Assert.That(catalog.Careers.ContainsKey("chef"), Is.True);
            Assert.That(catalog.Careers.ContainsKey("barista"), Is.True);
            Assert.That(catalog.Courses.ContainsKey("communication-basics"), Is.True);
            Assert.That(catalog.Businesses.Count, Is.EqualTo(4));
            Assert.That(catalog.Businesses[FirstPlayableContentTemplate.FreelanceId].OperatingRequirements.RequiredOwnerMinutes, Is.EqualTo(120));
        }

        [Test]
        public void SerializedFirstPlayableAssetContainsScheduledBusinesses()
        {
            var asset = AssetDatabase.LoadAssetAtPath<StartupLifeContentCatalogAsset>("Assets/StartupLife/Data/FirstPlayableContent.asset");
            Assert.That(asset, Is.Not.Null);
            var catalog = asset.BuildCatalog();
            Assert.That(catalog.Businesses.Count, Is.EqualTo(4));
            Assert.That(catalog.Careers.Count, Is.EqualTo(6),
                "The Unity authoring gate must regenerate the serialized catalog from Batch A source.");
            Assert.That(catalog.Careers["marketing"].Scenes.Count, Is.EqualTo(6));
            Assert.That(catalog.Careers["sales"].WorkDays.Count, Is.EqualTo(6));
            Assert.That(catalog.Careers["office-admin"].Ranks.Count, Is.EqualTo(3));
            Assert.That(catalog.Careers["chef"].Scenes.Count, Is.EqualTo(6));
            Assert.That(catalog.Careers["barista"].Scenes.Count, Is.EqualTo(6));
            Assert.That(catalog.Careers["chef"].WorkDays.Count, Is.EqualTo(6));
            Assert.That(catalog.Careers["barista"].WorkDays.Count, Is.EqualTo(6));
            var freelance = catalog.Businesses[FirstPlayableContentTemplate.FreelanceId];
            var kiosk = catalog.Businesses[FirstPlayableContentTemplate.CoffeeKioskId];
            Assert.That(freelance.OperationMode, Is.EqualTo(BusinessOperationMode.SideHustleCompatible));
            Assert.That(freelance.OperatingRequirements.RequiredOwnerMinutes, Is.EqualTo(120));
            Assert.That(freelance.OperatingRequirements.OperatingWindows.Count, Is.EqualTo(7));
            Assert.That(kiosk.OperationMode, Is.EqualTo(BusinessOperationMode.FullTimeRequired));
            Assert.That(kiosk.OperatingRequirements.RequiredOwnerMinutes, Is.EqualTo(480));
            Assert.That(catalog.Businesses.ContainsKey("online-store"), Is.True);
            Assert.That(catalog.Businesses.ContainsKey("home-food-preorder"), Is.True);
            Assert.That(catalog.Businesses["online-store"].Type, Is.EqualTo(BusinessType.OnlineStore));
            Assert.That(catalog.Businesses["home-food-preorder"].Type, Is.EqualTo(BusinessType.HomeFoodPreorder));
            Assert.That(catalog.Businesses["online-store"].OperatingRequirements.RequiredOwnerMinutes, Is.EqualTo(120));
            Assert.That(catalog.Businesses["home-food-preorder"].OperatingRequirements.RequiredOwnerMinutes, Is.EqualTo(120));
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
