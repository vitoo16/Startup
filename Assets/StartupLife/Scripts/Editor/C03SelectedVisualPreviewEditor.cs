using System;
using System.IO;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace StartupLife.Editor
{
    /// <summary>
    /// Opens an UNSAVED, isolated visual-reference scene; never touches
    /// FirstPlayable, Art/Approved, player saves, prefab or animation assets.
    /// This displays the original opaque reference, not a gameplay character.
    /// </summary>
    public static class C03SelectedVisualPreviewEditor
    {
        private const string Record =
            "docs/evidence/production-art/C03-LATEST-MINOR-POLISH-REFERENCE.json";

        [Serializable]
        private sealed class LatestReference
        {
            public string expected_repo_draft_path = "";
            public string sha256 = "";
            public int width;
            public int height;
        }

        [Serializable]
        private sealed class ReferenceRecord
        {
            public LatestReference latest_reference;
        }

        // This stable source path/GUID belongs to the selected reference, not the
        // production sprite rig. Retain the GUID when Unity serializes the importer.
        private const string ReferencePath =
            "Assets/StartupLife/Art/Source/Characters/golden-character-c03/references/C03-hand-behind-bag-reference.png";
        private const string ReferenceGuid = "0eb68713039bb3f40d6ad5c2a6bb0309";
        private const float ReferencePreviewPpu = 350f;

        // Read-only verification does not configure importers or create scenes.
        // It deliberately fails on the checked-in identity-only .meta until the
        // actual Unity Editor has serialized the appropriate TextureImporter.
        [MenuItem("Startup Life/Art/C03/Verify imported selected reference (read only)")]
        public static void VerifyImportedReference()
        {
            var reference = ReadVerifiedReference();
            VerifyImportedReferenceSprite(reference);
            Debug.Log("[StartupLife][C03] Selected source SHA, GUID, TextureImporter, " +
                "sprite dimensions and QA preview settings verified in this Unity Editor. " +
                "No runtime sprite, rig, IP or device approval is implied.");
        }

        private static LatestReference ReadVerifiedReference()
        {
            if (!File.Exists(Record))
                throw new FileNotFoundException("C03 owner reference record is missing.", Record);

            var record = JsonUtility.FromJson<ReferenceRecord>(File.ReadAllText(Record));
            var reference = record != null ? record.latest_reference : null;
            if (reference == null ||
                !string.Equals(reference.expected_repo_draft_path, ReferencePath, StringComparison.Ordinal) ||
                reference.width <= 0 || reference.height <= 0 ||
                string.IsNullOrEmpty(reference.sha256))
                throw new InvalidOperationException("C03 selected reference record is invalid.");

            if (!File.Exists(ReferencePath))
                throw new FileNotFoundException(
                    "Exact owner-selected C03 PNG is missing or Git LFS is not hydrated. " +
                    "Never substitute an earlier draft.", ReferencePath);

            // ImportAsset can auto-create a new Unity GUID if the sidecar is
            // absent. Fail before calling it; never silently replace identity.
            var metaPath = ReferencePath + ".meta";
            if (!File.Exists(metaPath))
                throw new FileNotFoundException(
                    "C03 reference .meta is missing. Restore the tracked GUID before import.", metaPath);
            var metaLines = File.ReadAllLines(metaPath);
            if (!Array.Exists(metaLines, line => string.Equals(
                    line.Trim(), "guid: " + ReferenceGuid, StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException(
                    "C03 tracked .meta GUID differs from the selected reference. Refusing import.");

            byte[] bytes = File.ReadAllBytes(ReferencePath);
            string actual;
            using (var sha = SHA256.Create())
                actual = BitConverter.ToString(sha.ComputeHash(bytes))
                    .Replace("-", "").ToLowerInvariant();
            if (!string.Equals(actual, reference.sha256, StringComparison.Ordinal))
                throw new InvalidOperationException(
                    "C03 reference SHA-256 does not match the owner-selected record.");

            return reference;
        }

        private static Sprite VerifyImportedReferenceSprite(LatestReference reference)
        {
            var importer = AssetImporter.GetAtPath(ReferencePath) as TextureImporter;
            if (importer == null)
                throw new InvalidOperationException("C03 PNG has no Unity TextureImporter.");

            var guid = AssetDatabase.AssetPathToGUID(ReferencePath);
            if (!string.Equals(guid, ReferenceGuid, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException(
                    "C03 reference Unity GUID changed. Restore the tracked .meta; do not approve this import.");

            if (importer.textureType != TextureImporterType.Sprite ||
                importer.spriteImportMode != SpriteImportMode.Single ||
                !Mathf.Approximately(importer.spritePixelsPerUnit, ReferencePreviewPpu) ||
                importer.filterMode != FilterMode.Point ||
                importer.mipmapEnabled ||
                importer.textureCompression != TextureImporterCompression.Uncompressed)
                throw new InvalidOperationException(
                    "C03 QA reference importer is not serialized with required preview settings.");

            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(ReferencePath);
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ReferencePath);
            if (!texture || !sprite ||
                texture.width != reference.width || texture.height != reference.height)
                throw new InvalidOperationException(
                    "C03 selected reference is not an imported full-resolution Sprite.");
            return sprite;
        }

        [MenuItem("Startup Life/Art/C03/Open selected visual reference in Game View")]
        public static void OpenReferenceStage()
        {
            var reference = ReadVerifiedReference();

            // Honor cancellation BEFORE touching the importer or the active scene.
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            AssetDatabase.ImportAsset(ReferencePath, ImportAssetOptions.ForceSynchronousImport);
            var importer = AssetImporter.GetAtPath(ReferencePath) as TextureImporter;
            if (importer == null)
                throw new InvalidOperationException("C03 PNG has no Unity TextureImporter.");
            if (!string.Equals(AssetDatabase.AssetPathToGUID(ReferencePath),
                    ReferenceGuid, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException(
                    "C03 reference GUID changed; refusing to reserialize the importer.");

            // Unity writes its own complete TextureImporter .meta. Do not hand
            // manufacture or re-GUID a texture asset. This PPU is for the
            // full-resolution opaque QA reference ONLY, not native pixel art.
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = ReferencePreviewPpu;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
            var sprite = VerifyImportedReferenceSprite(reference);

            // Never modify or save FirstPlayable. Keep this QA scene isolated.
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var camera = new GameObject("C03_REFERENCE_ONLY_Camera", typeof(Camera))
                .GetComponent<Camera>();
            camera.tag = "MainCamera";
            camera.orthographic = true;
            camera.orthographicSize = 3.8f;
            camera.transform.position = new Vector3(0f, 0f, -10f);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.94f, 0.92f, 0.87f);

            var holder = new GameObject("C03_OPAQUE_REFERENCE_NOT_GAMEPLAY", typeof(SpriteRenderer));
            var renderer = holder.GetComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = 0;

            Debug.Log("[StartupLife][C03] Verified original owner-selected reference " + reference.sha256 +
                ". This scene is UNSAVED; the illustration has an opaque background and no character rig. " +
                "No production sprite/animation approved or bound to FirstPlayable.");
            EditorUtility.DisplayDialog("C03 selected visual · reference only",
                "Open the Game tab to inspect the exact owner-selected image. " +
                "This is an UNSAVED standalone QA scene, NOT Character Creator or gameplay. " +
                "No transparent cutout, rig, blink or production approval is implied.", "OK");
        }
    }
}
