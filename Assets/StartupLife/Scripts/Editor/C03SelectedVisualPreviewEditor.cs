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

        [MenuItem("Startup Life/Art/C03/Open selected visual reference in Game View")]
        public static void OpenReferenceStage()
        {
            var record = JsonUtility.FromJson<ReferenceRecord>(File.ReadAllText(Record));
            var reference = record != null ? record.latest_reference : null;
            if (reference == null ||
                string.IsNullOrEmpty(reference.expected_repo_draft_path) ||
                !reference.expected_repo_draft_path.StartsWith(
                    "Assets/StartupLife/Art/Source/Characters/golden-character-c03/references/",
                    StringComparison.Ordinal) ||
                !reference.expected_repo_draft_path.EndsWith(".png", StringComparison.Ordinal))
                throw new InvalidOperationException("C03 selected reference record is invalid.");

            var path = reference.expected_repo_draft_path;
            if (!File.Exists(path))
                throw new FileNotFoundException(
                    "Exact owner-selected C03 PNG has not been staged. This QA stage cannot use the old draft instead.",
                    path);

            byte[] bytes = File.ReadAllBytes(path);
            string actual;
            using (var sha = SHA256.Create())
                actual = BitConverter.ToString(sha.ComputeHash(bytes))
                    .Replace("-", "").ToLowerInvariant();
            if (!string.Equals(actual, reference.sha256, StringComparison.Ordinal))
                throw new InvalidOperationException(
                    "C03 reference SHA-256 does not match the owner's selected artwork. No preview opened.");

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (!texture || texture.width != reference.width || texture.height != reference.height)
                throw new InvalidOperationException(
                    "C03 original PNG was not imported at its verified full resolution.");

            // Ask before closing an unsaved active scene. Never modify or save
            // FirstPlayable and never add the unapproved visual to a build scene.
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
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
            var sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height),
                new Vector2(0.5f, 0.5f), 350f);
            sprite.name = "C03_SELECTED_REFERENCE_TEMPORARY";
            renderer.sprite = sprite;
            renderer.sortingOrder = 0;

            Debug.Log("[StartupLife][C03] Verified original owner-selected reference " + actual +
                ". This scene is UNSAVED; the illustration has an opaque background and no character rig. " +
                "No production sprite/animation approved or bound to FirstPlayable.");
            EditorUtility.DisplayDialog("C03 selected visual · reference only",
                "Open the Game tab to inspect the exact owner-selected image. " +
                "This is an UNSAVED standalone QA scene, NOT Character Creator or gameplay. " +
                "No transparent cutout, rig, blink or production approval is implied.", "OK");
        }
    }
}
