using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace StartupLife.Editor
{
    // C01 is a hand-authored editable *candidate*, never an approved runtime asset.
    // Generated assets stay in Art/Source and are NOT added to any build scene.
    public static class GoldenCharacterDraftBuilder
    {
        private const string Source = "Assets/StartupLife/Art/Source/Characters/golden-character-c01/layers.json";
        private const string Generated =
            "Assets/StartupLife/Art/Source/Characters/golden-character-c01/Generated";
        private const string CharacterRoot = "SL_Golden_C01_Quarantined";

        [Serializable] private sealed class Swatch
        {
            public string id = "";
            public string hex = "";
        }

        [Serializable] private sealed class RectLayer
        {
            public string color = "";
            public int[] box = Array.Empty<int>();
        }

        [Serializable] private sealed class Part
        {
            public string id = "";
            public string parent = "";
            public int[] joint_px = Array.Empty<int>();
            public int sorting_order;
            public RectLayer[] rects = Array.Empty<RectLayer>();
        }

        [Serializable] private sealed class JointTrack
        {
            public string part = "";
            public float[] angles = Array.Empty<float>();
        }

        [Serializable] private sealed class Motion
        {
            public string id = "";
            public float duration_seconds;
            public JointTrack[] joints = Array.Empty<JointTrack>();
        }

        [Serializable] private sealed class Draft
        {
            public string schema = "";
            public string style_id = "";
            public string status = "";
            public int ppu;
            public int[] canvas = Array.Empty<int>();
            public int[] root_pivot = Array.Empty<int>();
            public Swatch[] palette = Array.Empty<Swatch>();
            public Part[] parts = Array.Empty<Part>();
            public Motion[] motions = Array.Empty<Motion>();
        }

        [MenuItem("Startup Life/Art/Build quarantined Golden character C01")]
        public static void Build()
        {
            // Fail closed. Do not overwrite artist-modified generated layers or
            // re-create Unity .meta files without the artist's review.
            if (AssetDatabase.IsValidFolder(Generated) || Directory.Exists(Generated))
                throw new InvalidOperationException(
                    "C01 generated output already exists. Review the art before any replacement.");

            var draft = JsonUtility.FromJson<Draft>(File.ReadAllText(Source));
            if (draft.schema != "startup-life.pixel-character-draft.v1" ||
                draft.style_id != "vn_everyday_pixel_v1" ||
                draft.status != "DRAFT_NOT_APPROVED" ||
                draft.ppu != PixelWorldCameraSetup.AssetsPpu ||
                draft.canvas.Length != 2 ||
                draft.canvas[0] != 128 || draft.canvas[1] != 160 ||
                draft.root_pivot.Length != 2)
                throw new InvalidOperationException("C01 draft disagrees with pixel art contract.");

            var palette = new Dictionary<string, Color32>(StringComparer.Ordinal);
            foreach (var swatch in draft.palette)
            {
                if (!ColorUtility.TryParseHtmlString(swatch.hex, out var color) ||
                    !palette.TryAdd(swatch.id, color))
                    throw new InvalidOperationException("Invalid or duplicate C01 color: " + swatch.id);
            }
            var nodes = draft.parts.ToDictionary(p => p.id, StringComparer.Ordinal);
            if (nodes.Count < 19 || !nodes.ContainsKey("upper_leg_l") ||
                !nodes.ContainsKey("lower_leg_l") || !nodes.ContainsKey("upper_leg_r") ||
                !nodes.ContainsKey("lower_leg_r"))
                throw new InvalidOperationException("Incomplete articulated candidate.");

            Directory.CreateDirectory(Generated);
            AssetDatabase.Refresh();
            // Each part is painted in an untrimmed logical coordinate system,
            // then tightly cropped with an exact pixel joint pivot on import.
            foreach (var part in draft.parts)
                PaintAndImport(draft, part, palette);

            GameObject root = null;
            try
            {
                root = new GameObject(CharacterRoot);
                var transforms = new Dictionary<string, Transform>(StringComparer.Ordinal)
                {
                    { "root", root.transform }
                };
                var remaining = new List<Part>(draft.parts);
                for (var pass = 0; remaining.Count > 0 && pass < draft.parts.Length; pass++)
                {
                    var progress = false;
                    for (var i = remaining.Count - 1; i >= 0; i--)
                    {
                        var part = remaining[i];
                        if (!transforms.TryGetValue(part.parent, out var parent))
                            continue;
                        var node = new GameObject(part.id);
                        node.transform.SetParent(parent, false);
                        var parentPivot = part.parent == "root"
                            ? draft.root_pivot : nodes[part.parent].joint_px;
                        node.transform.localPosition = new Vector3(
                            (part.joint_px[0] - parentPivot[0]) / (float)draft.ppu,
                            (part.joint_px[1] - parentPivot[1]) / (float)draft.ppu, 0);
                        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(
                            Generated + "/" + part.id + ".png");
                        if (!sprite)
                            throw new InvalidOperationException("Missing imported sprite: " + part.id);
                        var renderer = node.AddComponent<SpriteRenderer>();
                        renderer.sprite = sprite;
                        renderer.sortingOrder = part.sorting_order;
                        transforms.Add(part.id, node.transform);
                        remaining.RemoveAt(i);
                        progress = true;
                    }
                    if (!progress)
                        throw new InvalidOperationException("Unresolvable C01 rig parenting.");
                }

                var controller = AnimatorController.CreateAnimatorControllerAtPath(
                    Generated + "/c01-proof.controller");
                controller.AddParameter("Motion", AnimatorControllerParameterType.Int);
                var machine = controller.layers[0].stateMachine;
                if (draft.motions.Length != 4 ||
                    draft.motions[0].id != "idle" || draft.motions[1].id != "walk" ||
                    draft.motions[2].id != "type" || draft.motions[3].id != "talk")
                    throw new InvalidOperationException("Expected four authored C01 motion states.");
                for (var mode = 0; mode < draft.motions.Length; mode++)
                {
                    var motion = draft.motions[mode];
                    var clip = CreateClip(motion, root.transform, transforms);
                    AssetDatabase.CreateAsset(clip, Generated + "/c01-" + motion.id + ".anim");
                    var state = machine.AddState(motion.id);
                    state.motion = clip;
                    if (mode == 0) machine.defaultState = state;
                    var transition = machine.AddAnyStateTransition(state);
                    transition.hasExitTime = false;
                    transition.duration = 0f;
                    transition.canTransitionToSelf = false;
                    transition.AddCondition(AnimatorConditionMode.Equals, mode, "Motion");
                }
                var animator = root.AddComponent<Animator>();
                animator.runtimeAnimatorController = controller;
                PrefabUtility.SaveAsPrefabAsset(root, Generated + "/C01_QUARANTINED.prefab");
                AssetDatabase.SaveAssets();
                EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<GameObject>(
                    Generated + "/C01_QUARANTINED.prefab"));
                Debug.Log("C01 prototype generated in Art/Source only. Motion values 0=idle, "
                    + "1=walk, 2=type, 3=talk. NOT approved; no production scene wired.");
            }
            finally
            {
                if (root != null) UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static void PaintAndImport(Draft draft, Part part,
            IReadOnlyDictionary<string, Color32> palette)
        {
            if (part.rects == null || part.rects.Length == 0 || part.joint_px.Length != 2)
                throw new InvalidOperationException("Missing C01 layer geometry: " + part.id);
            var left = part.rects.Min(r => r.box[0]);
            var bottom = part.rects.Min(r => r.box[1]);
            var right = part.rects.Max(r => r.box[0] + r.box[2]);
            var top = part.rects.Max(r => r.box[1] + r.box[3]);
            var width = right - left;
            var height = top - bottom;
            if (left < 0 || bottom < 0 || right > draft.canvas[0] ||
                top > draft.canvas[1] || width <= 0 || height <= 0 ||
                part.joint_px[0] < left || part.joint_px[0] >= right ||
                part.joint_px[1] < bottom || part.joint_px[1] >= top)
                throw new InvalidOperationException("C01 layer outside pixel bounds: " + part.id);
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            try
            {
                texture.filterMode = FilterMode.Point;
                var clear = new Color32[width * height];
                texture.SetPixels32(clear);
                foreach (var rectangle in part.rects)
                {
                    if (!palette.TryGetValue(rectangle.color, out var paint))
                        throw new InvalidOperationException("Unknown paint role: " + rectangle.color);
                    var box = rectangle.box;
                    for (var y = box[1]; y < box[1] + box[3]; y++)
                        for (var x = box[0]; x < box[0] + box[2]; x++)
                            texture.SetPixel(x - left, y - bottom, paint);
                }
                texture.Apply(false, false);
                var path = Generated + "/" + part.id + ".png";
                File.WriteAllBytes(path, texture.EncodeToPNG());
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null)
                    throw new InvalidOperationException("C01 must import as a TextureImporter.");
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = draft.ppu;
                importer.spriteAlignment = (int)SpriteAlignment.Custom;
                importer.spritePivot = new Vector2(
                    (part.joint_px[0] - left) / (float)width,
                    (part.joint_px[1] - bottom) / (float)height);
                importer.filterMode = FilterMode.Point;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }
            finally { UnityEngine.Object.DestroyImmediate(texture); }
        }

        private static AnimationClip CreateClip(Motion motion, Transform root,
            IReadOnlyDictionary<string, Transform> bones)
        {
            if (motion.joints == null || motion.joints.Length == 0 ||
                motion.duration_seconds <= 0f || float.IsNaN(motion.duration_seconds) ||
                float.IsInfinity(motion.duration_seconds))
                throw new InvalidOperationException("Invalid animation duration or joint roster.");
            var clip = new AnimationClip { name = "C01_" + motion.id, frameRate = 12f };
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var track in motion.joints)
            {
                if (!bones.TryGetValue(track.part, out var bone) ||
                    !seen.Add(track.part) || track.angles == null ||
                    track.angles.Length != 5 || track.angles[0] != track.angles[4])
                    throw new InvalidOperationException(
                        "Invalid or duplicate C01 motion curve: " + track.part);
                var keys = new Keyframe[track.angles.Length];
                for (var i = 0; i < keys.Length; i++)
                {
                    if (float.IsNaN(track.angles[i]) || float.IsInfinity(track.angles[i]) ||
                        Mathf.Abs(track.angles[i]) > 45f)
                        throw new InvalidOperationException("Unbounded C01 motion angle.");
                    keys[i] = new Keyframe(
                        motion.duration_seconds * i / (keys.Length - 1), track.angles[i]);
                }
                var relativePath = AnimationUtility.CalculateTransformPath(bone, root);
                AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(
                    relativePath, typeof(Transform), "localEulerAnglesRaw.z"),
                    new AnimationCurve(keys));
            }
            clip.wrapMode = WrapMode.Loop;
            return clip;
        }
    }
}
