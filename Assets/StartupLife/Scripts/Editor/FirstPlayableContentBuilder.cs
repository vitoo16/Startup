using System.IO;
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
            Debug.Log("[StartupLifeContent] First playable content validated at " + AssetPath);
        }
    }
}
