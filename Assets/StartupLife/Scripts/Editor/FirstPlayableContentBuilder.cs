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

            if (catalog.Careers.Count != 4)
                throw new InvalidOperationException("Expected Developer plus Career Batch A (Marketing, Sales, Office/Admin).");
            var weekdays = new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday,
                DayOfWeek.Thursday, DayOfWeek.Friday };
            VerifyCareer(catalog.Careers["developer"], 540, 1020, 4, weekdays,
                new[] { 40, 20, 15, 10, 10, 5 }, new long[] { 10000000, 15000000 });
            VerifyCareer(catalog.Careers["marketing"], 540, 1020, 4, weekdays,
                new[] { 25, 20, 20, 15, 10, 10 }, new long[] { 9000000, 13500000, 18000000 });
            VerifyCareer(catalog.Careers["sales"], 540, 1080, 4,
                weekdays.Concat(new[] { DayOfWeek.Saturday }).ToArray(),
                new[] { 20, 20, 15, 20, 15, 10 }, new long[] { 8000000, 12000000, 16000000 });
            VerifyCareer(catalog.Careers["office-admin"], 510, 1050, 4, weekdays,
                new[] { 25, 20, 20, 15, 10, 10 }, new long[] { 7000000, 10500000, 14000000 });
            Debug.Log("[M8-T02-Authoring] Loaded asset: Freelance 240/120; Kiosk 480/480; scheduled content verified.");
            Debug.Log("[StartupLifeContent] Four careers and four business schedules validated.");
        }
        private static void VerifyCareer(CareerDefinition career, int start, int end,
            int scenesPerShift, DayOfWeek[] workDays, int[] sceneWeights, long[] salaries)
        {
            if (career.StartMinute != start || career.EndMinute != end ||
                career.ScenesPerShift != scenesPerShift || career.QuotaSlots != 20 ||
                !career.WorkDays.SequenceEqual(workDays) ||
                !career.Scenes.Select(x => x.Weight).SequenceEqual(sceneWeights) ||
                !career.Ranks.Select(x => x.MonthlySalary).SequenceEqual(salaries))
                throw new InvalidOperationException("Career content differs from the authored Batch A contract: " + career.Id);
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
