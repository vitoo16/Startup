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



namespace StartupLife.Editor
{
    // Quarantined Editor-only V3 proof. Reuses the existing Editor assembly/.meta.
    // Never wires a draft sprite to the production character resolver or a gameplay scene.
    public static class C03IdleV3QuarantineBuilder
    {
        private const string Input = "artifacts/C03-Idle-V3-Unity-Quarantine/input";
        private const string Output =
            "Assets/StartupLife/Art/Source/Characters/golden-character-c03/Generated-V3-Quarantine";
        private const string Packet =
            "1b04022f5f828ba95374cc3e0e875df9597ea89454d3ebdd5730aa46a3254c1f";

        [Serializable] private class InputFile { public string path = ""; public string sha256 = ""; }
        [Serializable] private class Staging
        {
            public string schema = "", status = "", source_packet_sha256 = "";
            public string owner_motion_direction = "";
            public bool rest_pixel_identical, blink_only_face_layer;
            public bool owner_golden_asset_approved, production_registry_approved;
            public int[] native_grid = Array.Empty<int>();
            public int pixels_per_unit;
            public InputFile[] files = Array.Empty<InputFile>();
        }
        [Serializable] private class Bone
        {
            public string id = "", parent = "";
            public float[] local_xy_unity_units = Array.Empty<float>();
        }
        [Serializable] private class Part
        {
            public string part_id = "", bone = "", filename = "";
            public int sort_order;
            public float[] local_xy_unity_units = Array.Empty<float>();
        }
        [Serializable] private class Rig
        {
            public string schema = "", status = "";
            public int[] native_grid = Array.Empty<int>();
            public int pixels_per_unit;
            public bool owner_final_golden_approval;
            public Bone[] joints = Array.Empty<Bone>();
            public Part[] sprite_renderers = Array.Empty<Part>();
        }

        [MenuItem("Startup Life/Art/C03/Build quarantined Idle V3 blink preview")]
        public static void Build()
        {
            if (Directory.Exists(Output) || AssetDatabase.IsValidFolder(Output))
                throw new InvalidOperationException(
                    "C03 preview already exists. Never overwrite artist-imported Unity assets.");
            var checkedInputs = ValidateInputs();
            var inputs = checkedInputs.Item1;
            var rig = checkedInputs.Item2;
            Directory.CreateDirectory(Output + "/layers");
            foreach (var file in inputs.files)
                File.Copy(Path.Combine(Input, file.path), Output + "/" + file.path);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            foreach (var file in inputs.files)
                ImportSprite(Output + "/" + file.path);

            GameObject root = null;
            try
            {
                root = new GameObject("C03_Idle_V3_QUARANTINED_NOT_FOR_GAMEPLAY");
                var bones = new Dictionary<string, Transform>(StringComparer.Ordinal)
                {
                    { "root", root.transform }
                };
                var pending = new List<Bone>(rig.joints.Where(j => j.id != "root"));
                for (var pass = 0; pending.Count > 0 && pass < rig.joints.Length; pass++)
                {
                    var advanced = false;
                    for (var i = pending.Count - 1; i >= 0; i--)
                    {
                        var joint = pending[i];
                        if (!bones.TryGetValue(joint.parent, out var parent)) continue;
                        var go = new GameObject(joint.id);
                        go.transform.SetParent(parent, false);
                        go.transform.localPosition = XY(joint.local_xy_unity_units);
                        bones.Add(joint.id, go.transform);
                        pending.RemoveAt(i);
                        advanced = true;
                    }
                    if (!advanced) throw new InvalidOperationException("C03 joint ancestry is invalid.");
                }
                if (bones.Count != 16)
                    throw new InvalidOperationException("C03 rig must contain 16 authored joints.");
                SpriteRenderer face = null;
                foreach (var part in rig.sprite_renderers)
                {
                    var go = new GameObject(part.part_id);
                    go.transform.SetParent(bones[part.bone], false);
                    go.transform.localPosition = XY(part.local_xy_unity_units);
                    var renderer = go.AddComponent<SpriteRenderer>();
                    renderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Output + "/" + part.filename);
                    renderer.sortingOrder = part.sort_order;
                    if (!renderer.sprite)
                        throw new InvalidOperationException("Missing imported C03 part " + part.part_id);
                    if (part.part_id == "face") face = renderer;
                }
                if (!face) throw new InvalidOperationException("C03 face slot missing.");
                var open = Load("face_open.png");
                var half = Load("face_half.png");
                var closed = Load("face_closed.png");
                // Only the FACE renderer has object reference keyframes. The body
                // remains on 20 stable native SpriteRenderers in their authored rig.
                var blink = new AnimationClip
                {
                    name = "C03_V3_EyesOnly_Proof", frameRate = 100f, wrapMode = WrapMode.Once
                };
                var relative = AnimationUtility.CalculateTransformPath(face.transform, root.transform);
                AnimationUtility.SetObjectReferenceCurve(blink,
                    EditorCurveBinding.PPtrCurve(relative, typeof(SpriteRenderer), "m_Sprite"),
                    new[]
                    {
                        new ObjectReferenceKeyframe { time = 0f, value = open },
                        new ObjectReferenceKeyframe { time = .02f, value = half },
                        new ObjectReferenceKeyframe { time = .05f, value = closed },
                        new ObjectReferenceKeyframe { time = .11f, value = half },
                        new ObjectReferenceKeyframe { time = .18f, value = open }
                    });
                AssetDatabase.CreateAsset(blink, Output + "/C03-Idle-V3-Blink-PROOF.anim");
                var restClip = new AnimationClip { name = "C03_Static_Rest_Proof", frameRate = 100f };
                AssetDatabase.CreateAsset(restClip, Output + "/C03-Idle-V3-Rest-PROOF.anim");
                var controller = AnimatorController.CreateAnimatorControllerAtPath(
                    Output + "/C03-Idle-V3-QUARANTINED.controller");
                controller.AddParameter("Blink", AnimatorControllerParameterType.Trigger);
                var machine = controller.layers[0].stateMachine;
                var rest = machine.AddState("Rest_Frozen");
                rest.motion = restClip;
                machine.defaultState = rest;
                var action = machine.AddState("Blink_Once_180ms");
                action.motion = blink;
                var toBlink = rest.AddTransition(action);
                toBlink.hasExitTime = false;
                toBlink.duration = 0f;
                toBlink.AddCondition(AnimatorConditionMode.If, 0f, "Blink");
                var toRest = action.AddTransition(rest);
                toRest.hasExitTime = true;
                toRest.exitTime = 1f;
                toRest.duration = 0f;
                root.AddComponent<Animator>().runtimeAnimatorController = controller;
                PrefabUtility.SaveAsPrefabAsset(root, Output + "/C03_IDLE_V3_QUARANTINED.prefab");
                AssetDatabase.SaveAssets();
                Debug.Log("C03 V3 quarantine imported in Editor. Trigger Blink manually in "
                    + "Play Mode and inspect pixel edges/sorting. Not production approved.");
            }
            finally
            {
                if (root != null) UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static (Staging, Rig) ValidateInputs()
        {
            var stagingPath = Path.Combine(Input, "C03-quarantine-input.json");
            var rigPath = Path.Combine(Input, "C03-unity-rig-authoring-manifest.json");
            if (!File.Exists(stagingPath) || !File.Exists(rigPath))
                throw new InvalidOperationException("Export native staging input before Unity import.");
            var staging = JsonUtility.FromJson<Staging>(File.ReadAllText(stagingPath));
            var rig = JsonUtility.FromJson<Rig>(File.ReadAllText(rigPath));
            if (staging == null || rig == null ||
                staging.schema != "startup-life.c03.idle-v3.unity-quarantine-input.v1" ||
                staging.status != "OFFLINE_STAGING_EDITOR_IMPORT_PENDING" ||
                staging.source_packet_sha256 != Packet ||
                staging.owner_motion_direction != "APPROVE_MOTION_DIRECTION" ||
                !staging.rest_pixel_identical || !staging.blink_only_face_layer ||
                staging.owner_golden_asset_approved || staging.production_registry_approved ||
                staging.native_grid.Length != 2 || staging.native_grid[0] != 128 ||
                staging.native_grid[1] != 160 || staging.pixels_per_unit != 32 ||
                rig.schema != "startup-life.c03.unity-rig-authoring-manifest.v1" ||
                rig.status != "DRAFT_EDITOR_HANDOFF_NOT_PRODUCTION_APPROVED" ||
                rig.owner_final_golden_approval || rig.native_grid.Length != 2 ||
                rig.native_grid[0] != 128 || rig.native_grid[1] != 160 ||
                rig.pixels_per_unit != 32 || rig.joints.Length != 16 ||
                rig.sprite_renderers.Length != 21 || staging.files.Length != 24)
                throw new InvalidOperationException("Unapproved or malformed C03 staged import.");
            if (rig.joints.Count(j => j.id == "root" && string.IsNullOrEmpty(j.parent)) != 1 ||
                rig.joints.Select(j => j.id).Distinct(StringComparer.Ordinal).Count() != 16 ||
                rig.joints.Any(j => j.local_xy_unity_units.Length != 2) ||
                rig.sprite_renderers.Select(x => x.part_id).Distinct(StringComparer.Ordinal).Count() != 21 ||
                rig.sprite_renderers.Select(x => x.sort_order).Distinct().Count() != 21 ||
                rig.sprite_renderers.Any(p => p.local_xy_unity_units.Length != 2 ||
                    p.filename != "layers/" + p.part_id + ".png" ||
                    !rig.joints.Any(j => j.id == p.bone)))
                throw new InvalidOperationException("C03 layer/rig topology changed.");
            var ordered = rig.sprite_renderers.ToDictionary(p => p.part_id, p => p.sort_order,
                StringComparer.Ordinal);
            if (ordered["accessory_front"] <= ordered["hand_r"])
                throw new InvalidOperationException("Bag must sort after its covered hand.");
            var files = new HashSet<string>(rig.sprite_renderers.Select(p => p.filename),
                StringComparer.Ordinal)
                { "face_open.png", "face_half.png", "face_closed.png" };
            if (files.Count != 24 ||
                staging.files.Select(f => f.path).Distinct(StringComparer.Ordinal).Count() != 24 ||
                staging.files.Any(f => !files.Contains(f.path)))
                throw new InvalidOperationException("Unexpected native C03 input files.");
            using (var sha = System.Security.Cryptography.SHA256.Create())
                foreach (var file in staging.files)
                {
                    var path = Path.Combine(Input, file.path);
                    if (!File.Exists(path)) throw new FileNotFoundException("C03 input missing.", path);
                    var actual = BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path)))
                        .Replace("-", "").ToLowerInvariant();
                    if (!string.Equals(actual, file.sha256, StringComparison.Ordinal))
                        throw new InvalidOperationException("C03 input hash mismatch: " + file.path);
                }
            return (staging, rig);
        }

        private static Vector3 XY(float[] value) =>
            new Vector3(value[0], value[1], 0f);

        private static Sprite Load(string filename)
        {
            var asset = AssetDatabase.LoadAssetAtPath<Sprite>(Output + "/" + filename);
            if (!asset) throw new InvalidOperationException("Missing C03 face pose " + filename);
            return asset;
        }

        private static void ImportSprite(string path)
        {
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) throw new InvalidOperationException("Not a PNG importer: " + path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spriteAlignment = (int)SpriteAlignment.Center;
            importer.spritePixelsPerUnit = 32;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport(); // Unity Editor creates and owns .meta
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (!texture || texture.width != 128 || texture.height != 160)
                throw new InvalidOperationException("C03 import grid is not 128x160: " + path);
        }
    }
}
