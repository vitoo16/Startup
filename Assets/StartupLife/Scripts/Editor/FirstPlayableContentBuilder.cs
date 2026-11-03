using System;
using System.IO;
using System.Linq;
using StartupLife.Core;
using StartupLife.Content;
using UnityEditor;
using UnityEngine;

namespace StartupLife.Editor
{
    public static class FirstPlayableContentBuilder
    {
        public const string AssetPath = "Assets/StartupLife/Data/FirstPlayableContent.asset";

        [MenuItem("Startup Life/Content/Create or update first playable content")]
        public static void CreateOrUpdate()
        {
            Directory.CreateDirectory("Assets/StartupLife/Data");
            AssetDatabase.Refresh();

            var asset = AssetDatabase.LoadAssetAtPath<StartupLifeContentCatalogAsset>(AssetPath);
            if (!asset)
            {
                asset = ScriptableObject.CreateInstance<StartupLifeContentCatalogAsset>();
                AssetDatabase.CreateAsset(asset, AssetPath);
            }

            Undo.RecordObject(asset, "Update first playable content");
            asset.ReplaceSourceForAuthoring(FirstPlayableContentTemplate.Create());

            // Convert once before saving so invalid authoring data fails at the builder boundary.
            _ = asset.BuildCatalog();

            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(AssetPath, ImportAssetOptions.ForceUpdate);
            VerifyOperatingEligibilityContent();
            Debug.Log("[StartupLifeContent] First playable content validated at " + AssetPath);
        }

        [MenuItem("Startup Life/Content/Refresh career catalog and Vietnamese localization")]
        public static void RefreshCareerCatalogAndLocalization()
        {
            CreateOrUpdate();
            DashboardLocalizationEditorSync.SyncVietnamese();
            Debug.Log("[StartupLifeContent] Career catalog and Vietnamese strings refreshed without running tests.");
        }

        // Authoring verification only; this is not the final Unity test/acceptance gate.
        public static void VerifyOperatingEligibilityContent()
        {
            var asset = AssetDatabase.LoadAssetAtPath<StartupLifeContentCatalogAsset>(AssetPath);
            if (!asset) throw new InvalidOperationException("First playable content asset is missing.");
            var catalog = asset.BuildCatalog();
            if (catalog.Businesses.Count != 4)
                throw new InvalidOperationException("Expected four authored functional businesses.");
            Verify(catalog.Businesses[FirstPlayableContentTemplate.FreelanceId], BusinessType.FreelanceService,
                BusinessOperationMode.SideHustleCompatible, 1080, 1320, 120);
            Verify(catalog.Businesses[FirstPlayableContentTemplate.CoffeeKioskId], BusinessType.CoffeeKiosk,
                BusinessOperationMode.FullTimeRequired, 540, 1020, 480);
            Verify(catalog.Businesses[FirstPlayableContentTemplate.OnlineStoreId], BusinessType.OnlineStore,
                BusinessOperationMode.SideHustleCompatible, 1080, 1320, 120);
            Verify(catalog.Businesses[FirstPlayableContentTemplate.HomeFoodPreorderId], BusinessType.HomeFoodPreorder,
                BusinessOperationMode.SideHustleCompatible, 1080, 1320, 120);
            VerifyAffinityTags(catalog.Businesses[FirstPlayableContentTemplate.FreelanceId].BusinessAffinityTags,
                new[] { "b2b-conversion", "customer-acquisition", "digital-product", "professional-services", "supplier-operations" },
                "freelance-service");
            VerifyAffinityTags(catalog.Businesses[FirstPlayableContentTemplate.CoffeeKioskId].BusinessAffinityTags,
                new[] { "customer-experience", "food", "supplier-operations" }, "coffee-kiosk");
            VerifyAffinityTags(catalog.Businesses[FirstPlayableContentTemplate.OnlineStoreId].BusinessAffinityTags,
                new[] { "b2b-conversion", "customer-acquisition", "digital-product" }, "online-store");
            VerifyAffinityTags(catalog.Businesses[FirstPlayableContentTemplate.HomeFoodPreorderId].BusinessAffinityTags,
                new[] { "customer-acquisition", "food", "supplier-operations" }, "home-food-preorder");

            if (catalog.Careers.Count != 6)
                throw new InvalidOperationException("Expected six initial professions from the GDD.");
            var weekdays = new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday,
                DayOfWeek.Thursday, DayOfWeek.Friday };
            VerifyCareer(catalog.Careers["developer"], 540, 1020, 4, weekdays,
                new[] { 40, 20, 15, 10, 10, 5 }, new long[] { 10000000, 15000000 },
                new long[] { 0, 1000 }, new[] { 0, 30 },
                new[] { "problem-solving", "problem-solving" }, new[] { 0, 2 });
            VerifyCareer(catalog.Careers["marketing"], 540, 1020, 4, weekdays,
                new[] { 25, 20, 20, 15, 10, 10 }, new long[] { 9000000, 13500000, 18000000 },
                new long[] { 0, 1000, 3000 }, new[] { 0, 30, 90 },
                new[] { "networking", "communication", "networking" }, new[] { 0, 1, 2 });
            VerifyCareer(catalog.Careers["sales"], 540, 1080, 4,
                weekdays.Concat(new[] { DayOfWeek.Saturday }).ToArray(),
                new[] { 20, 20, 15, 20, 15, 10 }, new long[] { 8000000, 12000000, 16000000 },
                new long[] { 0, 1000, 3000 }, new[] { 0, 30, 90 },
                new[] { "negotiation", "communication", "negotiation" }, new[] { 0, 1, 2 });
            VerifyCareer(catalog.Careers["office-admin"], 510, 1050, 4, weekdays,
                new[] { 25, 20, 20, 15, 10, 10 }, new long[] { 7000000, 10500000, 14000000 },
                new long[] { 0, 1000, 3000 }, new[] { 0, 30, 90 },
                new[] { "time-management", "problem-solving", "leadership" }, new[] { 0, 1, 2 });
            var kitchenDays = new[] { DayOfWeek.Tuesday, DayOfWeek.Wednesday,
                DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday };
            VerifyCareer(catalog.Careers["chef"], 600, 1080, 4, kitchenDays,
                new[] { 25, 30, 10, 10, 10, 15 }, new long[] { 8500000, 12500000, 17500000 },
                new long[] { 0, 1000, 3000 }, new[] { 0, 30, 90 },
                new[] { "time-management", "problem-solving", "leadership" }, new[] { 0, 1, 2 });
            VerifyCareer(catalog.Careers["barista"], 480, 960, 4, kitchenDays,
                new[] { 20, 30, 20, 10, 10, 10 }, new long[] { 6500000, 9500000, 13000000 },
                new long[] { 0, 1000, 3000 }, new[] { 0, 30, 90 },
                new[] { "communication", "time-management", "networking" }, new[] { 0, 1, 2 });
            VerifyAffinityTags(catalog.Careers["developer"].BusinessAffinityTags,
                new[] { "digital-product", "professional-services" }, "developer");
            VerifyAffinityTags(catalog.Careers["marketing"].BusinessAffinityTags,
                new[] { "customer-acquisition", "professional-services" }, "marketing");
            VerifyAffinityTags(catalog.Careers["sales"].BusinessAffinityTags,
                new[] { "b2b-conversion", "professional-services" }, "sales");
            VerifyAffinityTags(catalog.Careers["office-admin"].BusinessAffinityTags,
                new[] { "professional-services", "supplier-operations" }, "office-admin");
            VerifyAffinityTags(catalog.Careers["chef"].BusinessAffinityTags,
                new[] { "food", "supplier-operations" }, "chef");
            VerifyAffinityTags(catalog.Careers["barista"].BusinessAffinityTags,
                new[] { "customer-experience", "food" }, "barista");
            if (catalog.Courses.Count != 6)
                throw new InvalidOperationException("Expected six authored skill courses.");
            VerifyCourse(catalog.Courses["communication-basics"], "communication", 1, 0, 360, 200000);
            VerifyCourse(catalog.Courses["problem-course"], "problem-solving", 2, 0, 360, 200000);
            VerifyCourse(catalog.Courses["negotiation-basics"], "negotiation", 1, 0, 420, 220000);
            VerifyCourse(catalog.Courses["time-management-basics"], "time-management", 1, 0, 360, 180000);
            VerifyCourse(catalog.Courses["networking-basics"], "networking", 1, 0, 480, 250000);
            VerifyCourse(catalog.Courses["leadership-basics"], "leadership", 1, 0, 540, 300000);
            var trained = catalog.Courses.Values.Select(x => x.SkillId)
                .ToHashSet(StringComparer.Ordinal);
            if (!catalog.Skills.Keys.All(trained.Contains))
                throw new InvalidOperationException("Every transferable skill needs a study route.");

            Debug.Log("[M8-T02-Authoring] Loaded asset: Freelance 240/120; Kiosk 480/480; scheduled content verified.");
            Debug.Log("[StartupLifeContent] Six careers, six skill courses, four business schedules and affinity tags validated.");
        }
        private static void VerifyAffinityTags(IEnumerable<string> actual, string[] expected, string owner)
        {
            if (!actual.SequenceEqual(expected))
                throw new InvalidOperationException("Business affinity tags differ from the authored contract: " + owner);
        }

        private static void VerifyCourse(CourseDefinition course, string skillId, int target,
            int prerequisite, int minutes, long price)
        {
            if (course.SkillId != skillId || course.TargetLevel != target ||
                course.PrerequisiteLevel != prerequisite || course.BaseMinutes != minutes ||
                course.Price != price)
                throw new InvalidOperationException("Study course differs from the authored contract: " + course.Id);
        }

        private static void VerifyCareer(CareerDefinition career, int start, int end,
            int scenesPerShift, DayOfWeek[] workDays, int[] sceneWeights, long[] salaries,
            long[] requiredXp, int[] requiredServiceDays, string[] grantSkills, int[] grantLevels)
        {
            if (career.StartMinute != start || career.EndMinute != end ||
                career.ScenesPerShift != scenesPerShift || career.QuotaSlots != 20 ||
                !career.WorkDays.SequenceEqual(workDays) ||
                !career.Scenes.Select(x => x.Weight).SequenceEqual(sceneWeights) ||
                !career.Ranks.Select(x => x.MonthlySalary).SequenceEqual(salaries) ||
                !career.Ranks.Select(x => x.RequiredXp).SequenceEqual(requiredXp) ||
                !career.Ranks.Select(x => x.RequiredServiceDays).SequenceEqual(requiredServiceDays) ||
                !career.Ranks.Select(x => x.GrantSkillId).SequenceEqual(grantSkills) ||
                !career.Ranks.Select(x => x.GrantLevel).SequenceEqual(grantLevels))
                throw new InvalidOperationException("Career content differs from the authored contract: " + career.Id);
        }

        private static void Verify(BusinessDefinition business, BusinessType type, BusinessOperationMode mode,
            int start, int end, int required)
        {
            var requirements = business.OperatingRequirements;
            if (business.Type != type || business.OperationMode != mode || requirements == null ||
                requirements.RequiredOwnerMinutes != required || requirements.OperatingWindows.Count != 7 ||
                !requirements.OperatingWindows.Select(x => (int)x.DayOfWeek).SequenceEqual(Enumerable.Range(0, 7)) ||
                requirements.OperatingWindows.Any(x => x.StartMinute != start || x.EndMinute != end))
                throw new InvalidOperationException("Functional business requirements differ from the approved contract.");
        }
    }
}
