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
            if (catalog.Businesses.Count != 2) throw new InvalidOperationException("Expected two functional businesses.");
            Verify(catalog.Businesses[FirstPlayableContentTemplate.FreelanceId], BusinessType.FreelanceService,
                BusinessOperationMode.SideHustleCompatible, 1080, 1320, 120);
            Verify(catalog.Businesses[FirstPlayableContentTemplate.CoffeeKioskId], BusinessType.CoffeeKiosk,
                BusinessOperationMode.FullTimeRequired, 540, 1020, 480);
            Debug.Log("[M8-T02-Authoring] Loaded asset: Freelance 240/120; Kiosk 480/480; scheduled content verified.");
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
