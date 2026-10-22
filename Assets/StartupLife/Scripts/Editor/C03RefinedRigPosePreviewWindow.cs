using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace StartupLife.Editor
{
    // Opt-in isolated Editor preview only: no scene edits, prefab edits,
    // motion clips or Animator. Rotations expose seam defects in a cutout
    // that has NOT had hidden anatomy painted yet.
    internal sealed class C03RefinedRigPosePreviewWindow : EditorWindow
    {
        private const string QaPrefabPath =
            "Assets/StartupLife/Art/Source/Characters/golden-character-c03/" +
            "Generated-Owner-Refined-Cutout-QA/C03_OWNER_SELECTED_REFINED_CUTOUT_QA.prefab";
        private PreviewRenderUtility preview;
        private GameObject instance;
        private Transform[] editableJoints = Array.Empty<Transform>();
        private readonly Dictionary<Transform, float> angles =
            new Dictionary<Transform, float>();
        private Vector2 scroll;

        [MenuItem("Startup Life/Art/C03/Preview refined joint rotation (QA only)")]
        private static void Open()
        {
            var window = GetWindow<C03RefinedRigPosePreviewWindow>("C03 Pivot & Seam QA");
            window.minSize = new Vector2(500f, 650f);
            window.Show();
        }

        private void OnEnable()
        {
            // Never instantiate the prefab in the user's active scene.
            // PreviewRenderUtility owns a private preview scene.
            preview = new PreviewRenderUtility();
            preview.camera.orthographic = true;
            preview.camera.orthographicSize = 3.4f;
            preview.camera.transform.position = new Vector3(0f, 0f, -12f);
            preview.camera.transform.rotation = Quaternion.identity;
            preview.camera.clearFlags = CameraClearFlags.SolidColor;
            preview.camera.backgroundColor = new Color(.96f, .94f, .90f, 1f);
            preview.camera.nearClipPlane = .01f;
            preview.camera.farClipPlane = 100f;
            LoadPreview();
        }

        private void LoadPreview()
        {
            if (preview == null || instance != null) return;
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(QaPrefabPath);
            if (prefab == null) return;

            instance = Instantiate(prefab);
            instance.name = "C03_Refined_Preview_Unsaved";
            instance.hideFlags = HideFlags.HideAndDontSave;
            preview.AddSingleGO(instance);

            // A joint has no SpriteRenderer. Sprite parts do. The root is
            // skipped so the proof cannot be faked by translating the body.
            editableJoints = instance.GetComponentsInChildren<Transform>(true)
                .Where(t => t != instance.transform &&
                            t.GetComponent<SpriteRenderer>() == null)
                .OrderBy(t => t.name, StringComparer.Ordinal)
                .ToArray();
            if (editableJoints.Length != 15 ||
                instance.GetComponentsInChildren<SpriteRenderer>(true).Length != 21)
                throw new InvalidOperationException(
                    "C03 QA prefab no longer has 15 movable joints + root and 21 sprites.");
            angles.Clear();
            foreach (var joint in editableJoints)
                angles.Add(joint, 0f);
        }

        private void OnDisable()
        {
            if (preview != null)
            {
                preview.Cleanup();
                preview = null;
            }
            instance = null;
            editableJoints = Array.Empty<Transform>();
            angles.Clear();
        }

        private void OnGUI()
        {
            EditorGUILayout.HelpBox(
                "C03 owner-selected refined cutout — diagnostic rotation ONLY. " +
                "Hidden anatomy and clean joints are not approved. " +
                "Preview is isolated from game scenes and cannot save Animator clips.",
                MessageType.Warning);

            if (instance == null)
            {
                EditorGUILayout.HelpBox(
                    "Build the separate QA prefab first: Startup Life / Art / C03 / " +
                    "Build selected-owner REFINED cutout QA rig (NOT motion ready).",
                    MessageType.Info);
                if (GUILayout.Button("Reload QA prefab")) LoadPreview();
                return;
            }

            var ratio = 1.0f;
            var viewWidth = Mathf.Min(position.width - 20f, 450f);
            var viewRect = GUILayoutUtility.GetRect(viewWidth, viewWidth / ratio);
            if (Event.current.type == EventType.Repaint)
            {
                preview.BeginPreview(viewRect, GUIStyle.none);
                preview.camera.Render();
                var frame = preview.EndPreview();
                GUI.DrawTexture(viewRect, frame, ScaleMode.ScaleToFit);
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Neutral / reset all joints"))
            {
                foreach (var joint in editableJoints)
                {
                    joint.localRotation = Quaternion.identity;
                    angles[joint] = 0f;
                }
                Repaint();
            }
            if (GUILayout.Button("Locate prefab (do not edit)"))
            {
                var asset = AssetDatabase.LoadAssetAtPath<GameObject>(QaPrefabPath);
                Selection.activeObject = asset;
                EditorGUIUtility.PingObject(asset);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.LabelField(
                "Joint rotation, -20° to +20° (source has unpainted overlaps)",
                EditorStyles.boldLabel);
            scroll = EditorGUILayout.BeginScrollView(scroll, GUILayout.ExpandHeight(true));
            foreach (var joint in editableJoints)
            {
                var oldAngle = angles[joint];
                var nextAngle = EditorGUILayout.Slider(joint.name, oldAngle, -20f, 20f);
                if (Mathf.Approximately(nextAngle, oldAngle)) continue;
                angles[joint] = nextAngle;
                joint.localRotation = Quaternion.Euler(0f, 0f, nextAngle);
                Repaint();
            }
            EditorGUILayout.EndScrollView();
        }
    }
}
