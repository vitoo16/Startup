#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using StartupLife.Presentation;
using TMPro;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization.Tables;

namespace StartupLife.Editor
{
    // Unity Editor writes both table and SharedTableData; never hand-edit YAML IDs.
    public static class DashboardLocalizationEditorSync
    {
        private const string LocalePath = "Assets/StartupLife/Localization";
        private const string FontPath = "Assets/StartupLife/UI/Fonts/NotoSansVietnamese.asset";

        [MenuItem("Tools/Startup Life/Localization/Sync Dashboard Vietnamese")]
        public static void SyncVietnamese()
        {
            var locale = LocalizationEditorSettings.GetLocales()
                .Single(x => x.Identifier.Code == "vi");
            var collection = LocalizationEditorSettings.GetStringTableCollections()
                .FirstOrDefault(x => x.TableCollectionName == LocalizedKeyLabel.Table) ??
                LocalizationEditorSettings.CreateStringTableCollection(LocalizedKeyLabel.Table, LocalePath);
            var table = (StringTable)collection.GetTable(locale.Identifier);
            if (table == null) throw new InvalidOperationException("Missing Vietnamese FirstPlayableUI table.");
            Apply(table);
            EditorUtility.SetDirty(table);
            EditorUtility.SetDirty(collection.SharedData);
            EditorUtility.SetDirty(collection);
            AssetDatabase.SaveAssets();
            Debug.Log("Startup Life: synced " + DashboardLocalizationCatalog.Entries.Count +
                " Vietnamese dashboard entries.");
        }

        public static void Apply(StringTable table)
        {
            if (table == null) throw new ArgumentNullException(nameof(table));
            foreach (var translation in DashboardLocalizationCatalog.Entries)
            {
                var entry = table.GetEntry(translation.Key);
                if (entry == null) table.AddEntry(translation.Key, translation.Value);
                else if (entry.Value != translation.Value) entry.Value = translation.Value;
            }
        }

        [MenuItem("Tools/Startup Life/Localization/Validate Dashboard Vietnamese")]
        public static void Validate()
        {
            var locale = LocalizationEditorSettings.GetLocales()
                .Single(x => x.Identifier.Code == "vi");
            var collection = LocalizationEditorSettings.GetStringTableCollections()
                .Single(x => x.TableCollectionName == LocalizedKeyLabel.Table);
            var table = (StringTable)collection.GetTable(locale.Identifier);
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            if (font == null) throw new InvalidOperationException("NotoSansVietnamese TMP font missing.");
            var invalid = DashboardLocalizationCatalog.Entries
                .Where(x => table.GetEntry(x.Key)?.Value != x.Value).Select(x => x.Key).ToArray();
            if (invalid.Length > 0)
                throw new InvalidOperationException("Missing/stale Vietnamese keys: " +
                    string.Join(", ", invalid));
            var glyphs = new HashSet<char>();
            foreach (var pair in DashboardLocalizationCatalog.Entries)
                foreach (var ch in pair.Value)
                    if (!char.IsWhiteSpace(ch) && !font.HasCharacter(ch))
                        glyphs.Add(ch);
            if (glyphs.Count != 0)
                throw new InvalidOperationException("Missing Vietnamese glyphs: " +
                    new string(glyphs.OrderBy(x => x).ToArray()));
            Debug.Log("Startup Life: dashboard key and TMP glyph validation PASS.");
        }
    }
}
