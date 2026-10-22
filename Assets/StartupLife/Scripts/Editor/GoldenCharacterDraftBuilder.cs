using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace StartupLife.Editor
{
    // Unity 6.x exposes spriteAlignment on TextureImporterSettings, not TextureImporter.
    // Shared by the C01 and C03 draft importers to keep pivot authoring consistent.
    internal static class NativeSpriteImportSettings
    {
        internal static void ConfigurePivot(TextureImporter importer, SpriteAlignment alignment, Vector2 pivot)
        {
            if (importer == null) throw new ArgumentNullException(nameof(importer));
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)alignment;
            settings.spritePivot = pivot;
            importer.SetTextureSettings(settings);
        }

        internal static bool HasAlignment(TextureImporter importer, SpriteAlignment alignment)
        {
            if (importer == null) return false;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            return settings.spriteAlignment == (int)alignment;
        }
    }

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
                NativeSpriteImportSettings.ConfigurePivot(importer, SpriteAlignment.Custom,
                    new Vector2((part.joint_px[0] - left) / (float)width,
                        (part.joint_px[1] - bottom) / (float)height));
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
            public string rig_manifest_sha256 = "";
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
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                VerifySerializedProof(rig);
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
            if (!string.Equals(staging.rig_manifest_sha256, SHA256(File.ReadAllBytes(rigPath)),
                    StringComparison.Ordinal))
                throw new InvalidOperationException("C03 rig manifest hash mismatch.");
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
            foreach (var file in staging.files)
            {
                var path = Path.Combine(Input, file.path);
                if (!File.Exists(path)) throw new FileNotFoundException("C03 input missing.", path);
                var actual = SHA256(File.ReadAllBytes(path));
                if (!string.Equals(actual, file.sha256, StringComparison.Ordinal))
                    throw new InvalidOperationException("C03 input hash mismatch: " + file.path);
            }
            return (staging, rig);
        }

        private static string SHA256(byte[] data)
        {
            using (var hash = System.Security.Cryptography.SHA256.Create())
                return BitConverter.ToString(hash.ComputeHash(data)).Replace("-", "").ToLowerInvariant();
        }

        [MenuItem("Startup Life/Art/C03/Verify quarantined Idle V3 blink preview")]
        public static void VerifyExisting()
        {
            var checkedInputs = ValidateInputs();
            VerifySerializedProof(checkedInputs.Item2);
            Debug.Log("C03 V3 serialized quarantine proof verified in Unity Editor. "
                + "Actual PlayMode, device and Golden art gates are still NOT RUN.");
        }

        private static void VerifySerializedProof(Rig rig)
        {
            var prefabPath = Output + "/C03_IDLE_V3_QUARANTINED.prefab";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(
                Output + "/C03-Idle-V3-Blink-PROOF.anim");
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(
                Output + "/C03-Idle-V3-QUARANTINED.controller");
            if (!prefab || !clip || !controller ||
                (prefab.name != "C03_Idle_V3_QUARANTINED_NOT_FOR_GAMEPLAY" &&
                 prefab.name != "C03_IDLE_V3_QUARANTINED"))
                throw new InvalidOperationException("Missing persisted quarantined C03 prefab/clip.");
            var animator = prefab.GetComponent<Animator>();
            if (!animator || animator.runtimeAnimatorController != controller ||
                controller.parameters.Length != 1 ||
                controller.parameters[0].name != "Blink" ||
                controller.parameters[0].type != AnimatorControllerParameterType.Trigger)
                throw new InvalidOperationException("C03 proof Animator lost its manual-only Blink.");
            var renderers = prefab.GetComponentsInChildren<SpriteRenderer>(true);
            if (renderers.Length != 21 || rig.sprite_renderers.Length != 21)
                throw new InvalidOperationException("C03 persisted rig has incorrect renderer count.");
            var byName = renderers.ToDictionary(r => r.gameObject.name, StringComparer.Ordinal);
            if (byName.Count != 21 || !byName.ContainsKey("face"))
                throw new InvalidOperationException("C03 persisted rig lost semantic part identity.");
            var transforms = prefab.GetComponentsInChildren<Transform>(true)
                .ToDictionary(t => t.gameObject.name, StringComparer.Ordinal);
            if (transforms.Count != 37) // 1 root + 15 child bones + 21 renderers
                throw new InvalidOperationException("C03 persisted rig lost bone/part hierarchy.");
            foreach (var bone in rig.joints)
            {
                var current = bone.id == "root" ? prefab.transform : transforms[bone.id];
                var parent = string.IsNullOrEmpty(bone.parent) ? null :
                    bone.parent == "root" ? prefab.transform : transforms[bone.parent];
                if (current.parent != parent ||
                    Vector2.Distance((Vector2)current.localPosition, (Vector2)XY(bone.local_xy_unity_units)) > 0.00001f ||
                    current.localRotation != Quaternion.identity)
                    throw new InvalidOperationException("C03 joint transform drift: " + bone.id);
            }
            foreach (var part in rig.sprite_renderers)
            {
                var renderer = byName[part.part_id];
                var expected = AssetDatabase.LoadAssetAtPath<Sprite>(Output + "/" + part.filename);
                var expectedParent = part.bone == "root" ? prefab.transform : transforms[part.bone];
                if (!expected || renderer.sprite != expected ||
                    renderer.sortingOrder != part.sort_order ||
                    renderer.transform.parent != expectedParent ||
                    Vector2.Distance((Vector2)renderer.transform.localPosition,
                        (Vector2)XY(part.local_xy_unity_units)) > 0.00001f ||
                    renderer.transform.localRotation != Quaternion.identity)
                    throw new InvalidOperationException("C03 imported native pose drift: " + part.part_id);
            }
            if (byName["accessory_front"].sortingOrder <= byName["hand_r"].sortingOrder)
                throw new InvalidOperationException("C03 bag/hand sorting is reversed.");
            var bindings = AnimationUtility.GetObjectReferenceCurveBindings(clip);
            if (bindings.Length != 1 || bindings[0].type != typeof(SpriteRenderer) ||
                bindings[0].propertyName != "m_Sprite" ||
                bindings[0].path != AnimationUtility.CalculateTransformPath(
                    byName["face"].transform, prefab.transform) ||
                AnimationUtility.GetCurveBindings(clip).Length != 0)
                throw new InvalidOperationException("C03 Blink must only swap the face sprite.");
            var keys = AnimationUtility.GetObjectReferenceCurve(clip, bindings[0]);
            var names = new[] { "face_open.png", "face_half.png", "face_closed.png",
                "face_half.png", "face_open.png" };
            var times = new[] { 0f, .02f, .05f, .11f, .18f };
            if (keys == null || keys.Length != 5 || Mathf.Abs(keys[4].time - .18f) > .0001f)
                throw new InvalidOperationException("C03 Blink is not exactly 180ms.");
            for (var i = 0; i < keys.Length; i++)
                if (Mathf.Abs(keys[i].time - times[i]) > .0001f ||
                    keys[i].value != Load(names[i]))
                    throw new InvalidOperationException("C03 Blink eye-frame order/timing mismatch.");
            var states = controller.layers[0].stateMachine.states;
            if (states.Length != 2 ||
                states.Count(st => st.state.name == "Rest_Frozen") != 1 ||
                states.Count(st => st.state.name == "Blink_Once_180ms") != 1)
                throw new InvalidOperationException("C03 proof should have exactly Rest and Blink states.");
            foreach (var part in rig.sprite_renderers)
                VerifyImportedTexture(Output + "/" + part.filename);
            foreach (var name in new[] { "face_open.png", "face_half.png", "face_closed.png" })
                VerifyImportedTexture(Output + "/" + name);
        }

        private static void VerifyImportedTexture(string path)
        {
            var tex = AssetImporter.GetAtPath(path) as TextureImporter;
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (tex == null || !sprite || tex.textureType != TextureImporterType.Sprite ||
                tex.spriteImportMode != SpriteImportMode.Single ||
                tex.spritePixelsPerUnit != 32 ||
                !NativeSpriteImportSettings.HasAlignment(tex, SpriteAlignment.Center) ||
                tex.filterMode != FilterMode.Point || tex.mipmapEnabled ||
                tex.textureCompression != TextureImporterCompression.Uncompressed ||
                Mathf.Abs(sprite.rect.width - 128f) > .0001f ||
                Mathf.Abs(sprite.rect.height - 160f) > .0001f ||
                Vector2.Distance(sprite.pivot, new Vector2(64f, 80f)) > .0001f)
                throw new InvalidOperationException("C03 sprite import settings drift: " + path);
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
            NativeSpriteImportSettings.ConfigurePivot(importer, SpriteAlignment.Center,
                new Vector2(.5f, .5f));
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

    // Separate 2026-10-21 owner-image pipeline, never derived from the
    // archived 2026-10-16 V3 quarantine rig. No FirstPlayable/Approved writes.
    public static class C03SelectedNativeRigBuilder
    {
        private const string Input = "artifacts/C03-selected-native-rig/input";
        private const string Output =
            "Assets/StartupLife/Art/Source/Characters/golden-character-c03/Generated-Selected-Quarantine";
        private const string Handoff = Input + "/C03-selected-native-unity-handoff.json";
        private const string Contract =
            "docs/evidence/production-art/C03-ACTIVE-OWNER-FRONT-RIG-CONTRACT.json";
        private const string Reference =
            "docs/evidence/production-art/C03-LATEST-MINOR-POLISH-REFERENCE.json";

        [Serializable] private sealed class ImageRef
        {
            public string sha256 = "", expected_repo_draft_path = "";
        }
        [Serializable] private sealed class ReferenceRecord
        {
            public ImageRef latest_reference;
        }
        [Serializable] private sealed class SourceConcept
        {
            public string image_sha256 = "", native_rig_from_selected_image = "";
        }
        [Serializable] private sealed class ActiveContract
        {
            public int pixel_per_unit;
            public int[] canvas_px = Array.Empty<int>();
            public string[] rig_parts_back_to_front = Array.Empty<string>();
            public SourceConcept selected_concept;
        }
        [Serializable] private sealed class InputFile
        {
            public string path = "", sha256 = "";
        }
        [Serializable] private sealed class Joint
        {
            public string id = "", parent = "";
            public float[] local_xy_unity_units = Array.Empty<float>();
        }
        [Serializable] private sealed class Part
        {
            public string part_id = "", bone = "", path = "";
            public int sort_order;
            public float[] local_xy_unity_units = Array.Empty<float>();
        }
        [Serializable] private sealed class MotionKey
        {
            public float time_seconds, angle_degrees;
        }
        [Serializable] private sealed class MotionTrack
        {
            public string joint_id = "";
            public MotionKey[] keys = Array.Empty<MotionKey>();
        }
        [Serializable] private sealed class Motion
        {
            public string id = "";
            public float duration_seconds;
            public MotionTrack[] tracks = Array.Empty<MotionTrack>();
        }
        [Serializable] private sealed class HandoffRecord
        {
            public string schema = "", status = "", selected_reference_sha256 = "";
            public string native_source_packet_sha256 = "";
            public bool approved_for_gameplay;
            public int[] native_grid = Array.Empty<int>();
            public int pixels_per_unit;
            public InputFile[] files = Array.Empty<InputFile>();
            public Joint[] joints = Array.Empty<Joint>();
            public Part[] sprite_renderers = Array.Empty<Part>();
            public Motion[] motions = Array.Empty<Motion>();
        }

        // All file checks happen before ANY Unity assets are copied/imported.
        private static HandoffRecord LoadCheckedHandoff()
        {
            if (!File.Exists(Handoff) || !File.Exists(Contract) || !File.Exists(Reference))
                throw new FileNotFoundException(
                    "Run Stage-C03SelectedNativeRig.py with actual owner-matched artist layers first.");
            var doc = JsonUtility.FromJson<HandoffRecord>(File.ReadAllText(Handoff));
            var contract = JsonUtility.FromJson<ActiveContract>(File.ReadAllText(Contract));
            var record = JsonUtility.FromJson<ReferenceRecord>(File.ReadAllText(Reference));
            if (doc == null || contract == null || record == null ||
                contract.selected_concept == null || record.latest_reference == null ||
                doc.schema != "startup-life.c03.selected-native-unity-handoff.v1" ||
                doc.status != "QUARANTINED_EDITOR_IMPORT_ONLY" || doc.approved_for_gameplay ||
                contract.selected_concept.native_rig_from_selected_image !=
                    "ARTIST_AUTHORED_LAYERS_SUBMITTED" ||
                !string.Equals(doc.selected_reference_sha256,
                    contract.selected_concept.image_sha256, StringComparison.Ordinal) ||
                !string.Equals(doc.selected_reference_sha256,
                    record.latest_reference.sha256, StringComparison.Ordinal) ||
                doc.selected_reference_sha256.Length != 64 ||
                doc.native_source_packet_sha256.Length != 64 ||
                doc.native_grid.Length != 2 || contract.canvas_px.Length != 2 ||
                doc.native_grid[0] != contract.canvas_px[0] ||
                doc.native_grid[1] != contract.canvas_px[1] ||
                doc.native_grid[0] != 128 || doc.native_grid[1] != 160 ||
                doc.pixels_per_unit != contract.pixel_per_unit ||
                doc.pixels_per_unit != PixelWorldCameraSetup.AssetsPpu ||
                doc.joints.Length != 16 || doc.files.Length != 21 ||
                doc.sprite_renderers.Length != 21 || doc.motions.Length == 0 ||
                contract.rig_parts_back_to_front.Length != 21)
                throw new InvalidOperationException("C03 owner-selected native Unity input is not authorized.");

            // Source image must be the exact Git LFS binary, not a pointer.
            var source = record.latest_reference.expected_repo_draft_path;
            if (!source.StartsWith(
                    "Assets/StartupLife/Art/Source/Characters/golden-character-c03/references/",
                    StringComparison.Ordinal) || !File.Exists(source) ||
                !string.Equals(Sha256(File.ReadAllBytes(source)),
                    doc.selected_reference_sha256, StringComparison.Ordinal))
                throw new InvalidOperationException("C03 owner reference source is absent or wrong.");

            var expected = new HashSet<string>(contract.rig_parts_back_to_front, StringComparer.Ordinal);
            var files = new HashSet<string>(StringComparer.Ordinal);
            foreach (var file in doc.files)
            {
                if (file == null || !file.path.StartsWith("layers/", StringComparison.Ordinal) ||
                    !file.path.EndsWith(".png", StringComparison.Ordinal) ||
                    file.path.IndexOf('/', "layers/".Length) >= 0 ||
                    !expected.Contains(Path.GetFileNameWithoutExtension(file.path)) ||
                    !files.Add(file.path) || file.sha256.Length != 64 ||
                    !string.Equals(Sha256(File.ReadAllBytes(Input + "/" + file.path)),
                        file.sha256, StringComparison.Ordinal))
                    throw new InvalidOperationException("C03 native layer absent or checksum changed.");
            }
            if (files.Count != expected.Count) throw new InvalidOperationException("C03 missing native layers.");

            var joints = new Dictionary<string, Joint>(StringComparer.Ordinal);
            foreach (var joint in doc.joints)
            {
                if (joint == null || string.IsNullOrWhiteSpace(joint.id) ||
                    joint.local_xy_unity_units.Length != 2 || !joints.TryAdd(joint.id, joint))
                    throw new InvalidOperationException("C03 duplicate or malformed authored joint.");
            }
            if (!joints.TryGetValue("root", out var root) ||
                !string.IsNullOrEmpty(root.parent) ||
                root.local_xy_unity_units[0] != 0f || root.local_xy_unity_units[1] != 0f)
                throw new InvalidOperationException("C03 root joint invalid.");

            var pending = new HashSet<string>(joints.Keys, StringComparer.Ordinal);
            pending.Remove("root");
            var visited = new HashSet<string>(StringComparer.Ordinal) { "root" };
            for (var pass = 0; pass < doc.joints.Length; pass++)
            {
                foreach (var name in pending.ToArray())
                    if (visited.Contains(joints[name].parent))
                    {
                        pending.Remove(name);
                        visited.Add(name);
                    }
            }
            if (pending.Count > 0)
                throw new InvalidOperationException("C03 rig contains cyclic or orphaned joints.");

            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var part in doc.sprite_renderers)
            {
                if (part == null || !expected.Contains(part.part_id) || !seen.Add(part.part_id) ||
                    !joints.ContainsKey(part.bone) ||
                    part.local_xy_unity_units.Length != 2 ||
                    part.sort_order < 0 || part.sort_order >= expected.Count ||
                    part.part_id != contract.rig_parts_back_to_front[part.sort_order] ||
                    part.path != "layers/" + part.part_id + ".png")
                    throw new InvalidOperationException("C03 invalid source-authored semantic renderer.");
            }
            var byPart = doc.sprite_renderers.ToDictionary(p => p.part_id, StringComparer.Ordinal);
            if (byPart["accessory_front"].sort_order <= byPart["hand_r"].sort_order ||
                byPart["hand_r"].bone != "wrist_r" ||
                byPart["accessory_front"].bone != "spine")
                throw new InvalidOperationException("C03 bag/hand sorting or articulation is invalid.");

            var clips = new HashSet<string>(StringComparer.Ordinal);
            foreach (var motion in doc.motions)
            {
                if (motion == null || !clips.Add(motion.id) || motion.id.Length == 0 ||
                    motion.duration_seconds < .2f || motion.duration_seconds > 10f ||
                    motion.tracks.Length == 0)
                    throw new InvalidOperationException("C03 invalid authored motion clip.");
                var tracks = new HashSet<string>(StringComparer.Ordinal);
                var moved = 0;
                foreach (var track in motion.tracks)
                {
                    if (track == null || track.joint_id == "root" ||
                        !joints.ContainsKey(track.joint_id) ||
                        !tracks.Add(track.joint_id) || track.keys.Length < 3)
                        throw new InvalidOperationException("C03 motion must animate real authored joints.");
                    var last = -1f;
                    var changing = false;
                    foreach (var frame in track.keys)
                    {
                        if (frame == null || float.IsNaN(frame.time_seconds) ||
                            float.IsNaN(frame.angle_degrees) ||
                            frame.time_seconds <= last ||
                            frame.time_seconds > motion.duration_seconds + .00001f ||
                            Mathf.Abs(frame.angle_degrees) > 45f)
                            throw new InvalidOperationException("C03 invalid joint motion keyframe.");
                        if (Mathf.Abs(frame.angle_degrees) > .001f) changing = true;
                        last = frame.time_seconds;
                    }
                    if (Mathf.Abs(track.keys[0].time_seconds) > .00001f ||
                        Mathf.Abs(last - motion.duration_seconds) > .00001f ||
                        Mathf.Abs(track.keys[0].angle_degrees) > .00001f ||
                        Mathf.Abs(track.keys[track.keys.Length - 1].angle_degrees) > .00001f)
                        throw new InvalidOperationException("C03 motion has an open loop seam.");
                    if (changing) moved++;
                }
                if (moved < 2) throw new InvalidOperationException(
                    "C03 motion cannot fake a living character with one rotating joint.");
                if (motion.id == "walk" &&
                    tracks.Count(t => t == "hip_l" || t == "hip_r" ||
                                      t == "knee_l" || t == "knee_r") < 2)
                    throw new InvalidOperationException("C03 walk needs moving leg joints.");
            }
            if (!clips.Contains("idle"))
                throw new InvalidOperationException("C03 requires artist-keyframed idle.");
            return doc;
        }

        private static string Sha256(byte[] bytes)
        {
            using (var hash = System.Security.Cryptography.SHA256.Create())
                return BitConverter.ToString(hash.ComputeHash(bytes))
                    .Replace("-", "").ToLowerInvariant();
        }

        private static Vector3 Position(float[] xy)
        {
            return new Vector3(xy[0], xy[1], 0f);
        }

        [MenuItem("Startup Life/Art/C03/Build selected native articulated rig (quarantine)")]
        public static void Build()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Stop Play Mode before authoring C03 Unity assets.");
            if (Directory.Exists(Output) || AssetDatabase.IsValidFolder(Output))
                throw new InvalidOperationException(
                    "Selected C03 quarantine rig already exists; do not overwrite artist revisions.");

            var doc = LoadCheckedHandoff();
            Directory.CreateDirectory(Output + "/layers");
            foreach (var file in doc.files)
                File.Copy(Input + "/" + file.path, Output + "/" + file.path);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            foreach (var file in doc.files)
            {
                var assetPath = Output + "/" + file.path;
                var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                if (importer == null) throw new InvalidOperationException("Missing C03 TextureImporter.");
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = doc.pixels_per_unit;
                importer.filterMode = FilterMode.Point;
                importer.mipmapEnabled = false;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.alphaIsTransparency = true;
                NativeSpriteImportSettings.ConfigurePivot(importer, SpriteAlignment.Center,
                    new Vector2(.5f, .5f));
                importer.SaveAndReimport();
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
                if (!texture || !sprite || texture.width != doc.native_grid[0] ||
                    texture.height != doc.native_grid[1])
                    throw new InvalidOperationException("C03 imported sprite has incorrect native dimensions.");
            }

            GameObject root = null;
            try
            {
                root = new GameObject("C03_SELECTED_NATIVE_QUARANTINE_NOT_GAMEPLAY");
                var joints = new Dictionary<string, Transform>(StringComparer.Ordinal)
                {
                    { "root", root.transform }
                };
                var pending = new List<Joint>(doc.joints.Where(j => j.id != "root"));
                for (var pass = 0; pending.Count > 0 && pass < doc.joints.Length; pass++)
                {
                    var progressed = false;
                    for (var i = pending.Count - 1; i >= 0; i--)
                    {
                        var joint = pending[i];
                        if (!joints.TryGetValue(joint.parent, out var parent)) continue;
                        var node = new GameObject(joint.id);
                        node.transform.SetParent(parent, false);
                        node.transform.localPosition = Position(joint.local_xy_unity_units);
                        joints.Add(joint.id, node.transform);
                        pending.RemoveAt(i);
                        progressed = true;
                    }
                    if (!progressed)
                        throw new InvalidOperationException("C03 authored joint hierarchy is incomplete.");
                }
                foreach (var part in doc.sprite_renderers)
                {
                    var node = new GameObject(part.part_id);
                    node.transform.SetParent(joints[part.bone], false);
                    node.transform.localPosition = Position(part.local_xy_unity_units);
                    var renderer = node.AddComponent<SpriteRenderer>();
                    renderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Output + "/" + part.path);
                    renderer.sortingOrder = part.sort_order;
                    if (!renderer.sprite) throw new InvalidOperationException("Missing C03 part " + part.part_id);
                }

                var controller = AnimatorController.CreateAnimatorControllerAtPath(
                    Output + "/C03_Selected_Native_QA.controller");
                var machine = controller.layers[0].stateMachine;
                foreach (var motion in doc.motions)
                {
                    var clip = new AnimationClip
                    {
                        name = "C03_Selected_" + motion.id + "_ARTIST_QA",
                        frameRate = 60f,
                        wrapMode = WrapMode.Loop
                    };
                    foreach (var track in motion.tracks)
                    {
                        var keys = track.keys.Select(frame =>
                            new Keyframe(frame.time_seconds, frame.angle_degrees)).ToArray();
                        var curve = new AnimationCurve(keys);
                        var path = AnimationUtility.CalculateTransformPath(
                            joints[track.joint_id], root.transform);
                        AnimationUtility.SetEditorCurve(clip,
                            EditorCurveBinding.FloatCurve(
                                path, typeof(Transform), "localEulerAnglesRaw.z"), curve);
                    }
                    AssetDatabase.CreateAsset(clip,
                        Output + "/C03_Selected_" + motion.id + "_QA.anim");
                    var state = machine.AddState("Review_" + motion.id);
                    state.motion = clip;
                    if (motion.id == "idle") machine.defaultState = state;
                }
                root.AddComponent<Animator>().runtimeAnimatorController = controller;
                PrefabUtility.SaveAsPrefabAsset(root, Output + "/C03_SELECTED_NATIVE_QA.prefab");
                AssetDatabase.SaveAssets();

                var saved = AssetDatabase.LoadAssetAtPath<GameObject>(
                    Output + "/C03_SELECTED_NATIVE_QA.prefab");
                if (!saved || saved.GetComponentsInChildren<SpriteRenderer>(true).Length != 21 ||
                    saved.GetComponentsInChildren<Transform>(true).Length != 37)
                    throw new InvalidOperationException(
                        "C03 serialized rig lost its 16 joints or 21 visible layer slots.");
                Debug.Log("[StartupLife][C03] Authored selected-image rig staged with " +
                    doc.joints.Length + " joints, " + doc.sprite_renderers.Length +
                    " renderers and " + doc.motions.Length + " keyframed motions. " +
                    "QUARANTINED: Unity PlayMode, art/IP and device QA NOT APPROVED.");
            }
            finally
            {
                if (root != null) UnityEngine.Object.DestroyImmediate(root);
            }
        }
    }


    // Builds a read-only concept QA rig from the exact owner-selected source
    // cutout study. The 21 source PNGs are 1122x1402, NOT native 128x160 art.
    // Visual seams/hidden anatomy and animation need manual artist cleanup.
    public static class C03OwnerSelectedCutoutRigQA
    {
        private const string Input =
            "docs/evidence/production-art/drafts/c03-owner-selected-layer-refined";
        private const string GuideInput =
            "docs/evidence/production-art/drafts/c03-owner-selected-layer-study";
        private const string Output =
            "Assets/StartupLife/Art/Source/Characters/golden-character-c03/Generated-Owner-Refined-Cutout-QA";
        private const string SourceImage =
            "Assets/StartupLife/Art/Source/Characters/golden-character-c03/references/C03-hand-behind-bag-reference.png";
        private const string SelectedSha =
            "1999322a19bf99a5954edc3cf4fd87f6c6b9795006781c0c46ee4ff49fdc30fa";
        private const float Ppu = 280.5f; // 1122 source px / 4 native world units

        [Serializable] private sealed class Receipt
        {
            public string schema = "", status = "", reference_sha256 = "";
            public string[] semantic_order = Array.Empty<string>();
            public int layer_count, joint_topology_slots_preserved, unassigned_foreground_fallback_px;
            public bool composite_matches_visible_source_pixels;
            public bool native_pixel_128x160, approved_for_gameplay;
        }

        // Separately SHA-checked receipt created by the Codex merged PR #59.
        // The old receipt still supplies the frozen 21-slot ordering, not PNGs.
        [Serializable] private sealed class RefinedReceipt
        {
            public string schema = "", status = "", canonical_selected_sha256 = "";
            public int preserved_semantic_layers, preserved_joint_topology, original_fallback_px;
            public int[] selected_source_grid = Array.Empty<int>();
            public int[] torso_bbox_before = Array.Empty<int>();
            public int[] torso_bbox_after = Array.Empty<int>();
            public bool composite_identical_to_original_selected_source_cutout;
            public bool pixel_colors_changed, native_128x160_art;
            public bool painted_hidden_limb_pixels, motion_or_unity_verified;
            public bool owner_approved, approved_for_gameplay;
        }

        [Serializable] private sealed class Joint
        {
            public string id = "", parent = "";
            public int[] pivot_native_estimate_xy = Array.Empty<int>();
            public string[] parts = Array.Empty<string>();
        }

        [Serializable] private sealed class Guide
        {
            public string schema = "", status = "", reference_sha256 = "";
            public int joint_count;
            public bool rig_approval;
            public Joint[] joints = Array.Empty<Joint>();
        }

        private static string Digest(string path)
        {
            using (var hash = System.Security.Cryptography.SHA256.Create())
                return BitConverter.ToString(hash.ComputeHash(File.ReadAllBytes(path)))
                    .Replace("-", "").ToLowerInvariant();
        }

        private static (Receipt, Guide) ValidateDraft()
        {
            if (!File.Exists(Input + "/C03-refined-layer-ownership-receipt.json") ||
                !File.Exists(GuideInput + "/C03-selected-cutout-receipt.json") ||
                !File.Exists(GuideInput + "/C03-selected-16-joint-guide-DRAFT.json"))
                throw new InvalidOperationException(
                    "C03 refined cutout and joint guide missing; hydrate Git LFS on main.");

            // Every SHA below belongs to the NEW refined layer files on main.
            // Never fetch old study layers just because their part names match.
            var raw = File.ReadAllText(Input + "/C03-refined-layer-ownership-receipt.json");
            var refined = JsonUtility.FromJson<RefinedReceipt>(raw);
            var receipt = JsonUtility.FromJson<Receipt>(
                File.ReadAllText(GuideInput + "/C03-selected-cutout-receipt.json"));
            var guide = JsonUtility.FromJson<Guide>(
                File.ReadAllText(GuideInput + "/C03-selected-16-joint-guide-DRAFT.json"));
            if (receipt == null || refined == null || guide == null ||
                receipt.schema != "startup-life.c03.selected-image-cutout-study.v1" ||
                receipt.status != "REVIEW_ONLY_OPAQUE_IMAGE_EXTRACTION_NOT_NATIVE_RIG" ||
                receipt.reference_sha256 != SelectedSha ||
                receipt.layer_count != 21 || receipt.joint_topology_slots_preserved != 16 ||
                receipt.semantic_order.Length != 21 ||
                !receipt.composite_matches_visible_source_pixels ||
                receipt.native_pixel_128x160 || receipt.approved_for_gameplay ||
                guide.schema != "startup-life.c03.visual-joint-guide.v1" ||
                guide.status != "PIVOT_ESTIMATES_ONLY_NOT_NATIVE_NOT_ANIMATABLE" ||
                guide.reference_sha256 != SelectedSha ||
                guide.rig_approval || guide.joint_count != 16 || guide.joints.Length != 16 ||
                refined.schema != "startup-life.c03.source-visible-layer-ownership-refinement.v1" ||
                refined.status != "DRAFT_SOURCE_CORRECTION_NOT_NATIVE_OR_ANIMATABLE" ||
                refined.canonical_selected_sha256 != SelectedSha ||
                refined.preserved_semantic_layers != receipt.layer_count ||
                refined.preserved_joint_topology != receipt.joint_topology_slots_preserved ||
                refined.original_fallback_px != receipt.unassigned_foreground_fallback_px ||
                refined.selected_source_grid.Length != 2 ||
                refined.selected_source_grid[0] != 1122 ||
                refined.selected_source_grid[1] != 1402 ||
                refined.torso_bbox_before.Length != 4 || refined.torso_bbox_after.Length != 4 ||
                refined.torso_bbox_after[3] >= refined.torso_bbox_before[3] ||
                !refined.composite_identical_to_original_selected_source_cutout ||
                refined.pixel_colors_changed || refined.native_128x160_art ||
                refined.painted_hidden_limb_pixels || refined.motion_or_unity_verified ||
                refined.owner_approved || refined.approved_for_gameplay)
                throw new InvalidOperationException(
                    "C03 refined cutout is not a verified nonshipping owner-image draft.");

            if (!File.Exists(SourceImage) || Digest(SourceImage) != SelectedSha)
                throw new InvalidOperationException(
                    "Canonical 2026-10-21 source is missing/incorrect (Git LFS).");

            var expected = new HashSet<string>(StringComparer.Ordinal);
            foreach (var part in receipt.semantic_order)
            {
                if (string.IsNullOrEmpty(part) ||
                    !System.Text.RegularExpressions.Regex.IsMatch(part, "^[a-z0-9_]+$") ||
                    !expected.Add(part))
                    throw new InvalidOperationException("Malformed or duplicate C03 part ID.");
            }
            // Enforce exact old 21 semantic slots without copying its old art.
            var required = new [] {
                "hair_back", "accessory_back", "foot_l", "foot_r",
                "lower_leg_l", "lower_leg_r", "upper_leg_l", "upper_leg_r",
                "pelvis", "torso", "upper_arm_l", "upper_arm_r",
                "forearm_l", "forearm_r", "hand_l", "hand_r", "outfit_torso",
                "head", "face", "hair_front", "accessory_front"
            };
            if (!receipt.semantic_order.SequenceEqual(required))
                throw new InvalidOperationException("C03 legacy sorting slots changed.");

            // JsonUtility does not deserialize dictionaries. Parse only the
            // exact 22 canonical filename+SHA pairs with an anchored pattern.
            var entries = System.Text.RegularExpressions.Regex.Matches(raw,
                "\"(?<file>layers/(?:[a-z0-9_]+|masks/bag_body)\\.png)\"\\s*:\\s*\"(?<sha>[0-9a-f]{64})\"");
            if (entries.Count != 22)
                throw new InvalidOperationException("Missing C03 refined source layer checksum entries.");
            var fileSha = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (System.Text.RegularExpressions.Match match in entries)
                if (!fileSha.TryAdd(match.Groups["file"].Value, match.Groups["sha"].Value))
                    throw new InvalidOperationException("Duplicate C03 cutout file digest.");
            foreach (var part in receipt.semantic_order.Concat(new[] {"masks/bag_body"}))
            {
                var rel = "layers/" + part + ".png";
                var source = Input + "/" + rel;
                if (!fileSha.TryGetValue(rel, out var sha) ||
                    !File.Exists(source) || Digest(source) != sha)
                    throw new InvalidOperationException("C03 source layer LFS SHA mismatch: " + rel);
            }

            var bones = new Dictionary<string, Joint>(StringComparer.Ordinal);
            var attached = new HashSet<string>(StringComparer.Ordinal);
            foreach (var joint in guide.joints)
            {
                if (joint == null || string.IsNullOrEmpty(joint.id) ||
                    !System.Text.RegularExpressions.Regex.IsMatch(joint.id, "^[a-z_]+$") ||
                    joint.pivot_native_estimate_xy.Length != 2 ||
                    joint.pivot_native_estimate_xy[0] < 0 ||
                    joint.pivot_native_estimate_xy[0] >= 128 ||
                    joint.pivot_native_estimate_xy[1] < 0 ||
                    joint.pivot_native_estimate_xy[1] >= 160 ||
                    !bones.TryAdd(joint.id, joint))
                    throw new InvalidOperationException("Malformed C03 joint guide.");
                foreach (var part in joint.parts)
                    if (!expected.Contains(part) || !attached.Add(part))
                        throw new InvalidOperationException("Duplicate or unknown C03 bound part.");
            }
            if (attached.Count != 21 || !bones.ContainsKey("root") ||
                !string.IsNullOrEmpty(bones["root"].parent))
                throw new InvalidOperationException("C03 complete 16-joint rig binding absent.");

            // Validate ancestry before copying any file: no cycles/orphans.
            var reached = new HashSet<string>(StringComparer.Ordinal) {"root"};
            for (var pass = 0; pass < 16; pass++)
                foreach (var pair in bones)
                    if (reached.Contains(pair.Value.parent))
                        reached.Add(pair.Key);
            if (reached.Count != 16)
                throw new InvalidOperationException("Orphaned/cyclic C03 rig guide.");

            return (receipt, guide);
        }

        [MenuItem("Startup Life/Art/C03/Build selected-owner REFINED cutout QA rig (NOT motion ready)")]
        public static void Build()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Stop Play Mode before importing a C03 QA rig.");
            if (Directory.Exists(Output) || AssetDatabase.IsValidFolder(Output))
                throw new InvalidOperationException(
                    "C03 QA rig exists already. Do not overwrite an artist's Unity work.");

            var inputs = ValidateDraft();
            var receipt = inputs.Item1;
            var guide = inputs.Item2;
            Directory.CreateDirectory(Output + "/layers");
            Directory.CreateDirectory(Output + "/layers/masks");
            foreach (var part in receipt.semantic_order.Concat(new[] {"masks/bag_body"}))
                File.Copy(Input + "/layers/" + part + ".png",
                    Output + "/layers/" + part + ".png");
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            foreach (var part in receipt.semantic_order.Concat(new[] {"masks/bag_body"}))
            {
                var path = Output + "/layers/" + part + ".png";
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null)
                    throw new InvalidOperationException("Missing C03 QA sprite importer.");
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = Ppu;
                importer.filterMode = FilterMode.Point;
                importer.mipmapEnabled = false;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.alphaIsTransparency = true;
                NativeSpriteImportSettings.ConfigurePivot(importer,
                    SpriteAlignment.Center, new Vector2(.5f,.5f));
                importer.SaveAndReimport();
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                if (texture == null || texture.width != 1122 || texture.height != 1402)
                    throw new InvalidOperationException("C03 QA cutout PNG lost source resolution.");
            }

            GameObject root = null;
            try
            {
                root = new GameObject("C03_OWNER_SELECTED_REFINED_CUTOUT_QA_NOT_ANIMATABLE");
                var joints = new Dictionary<string, Transform>(StringComparer.Ordinal)
                {
                    {"root", root.transform}
                };
                var coords = guide.joints.ToDictionary(j => j.id, StringComparer.Ordinal);
                var pending = new List<Joint>(guide.joints.Where(j => j.id != "root"));
                for (var pass = 0; pending.Count > 0 && pass < 16; pass++)
                {
                    var advanced = false;
                    for (var i = pending.Count - 1; i >= 0; i--)
                    {
                        var joint = pending[i];
                        if (!joints.TryGetValue(joint.parent, out var parent)) continue;
                        var parentPivot = coords[joint.parent].pivot_native_estimate_xy;
                        var pivot = joint.pivot_native_estimate_xy;
                        var node = new GameObject(joint.id);
                        node.transform.SetParent(parent, false);
                        node.transform.localPosition = new Vector3(
                            (pivot[0]-parentPivot[0])/32f,
                            (parentPivot[1]-pivot[1])/32f,0f);
                        joints.Add(joint.id,node.transform);
                        pending.RemoveAt(i);
                        advanced = true;
                    }
                    if (!advanced) throw new InvalidOperationException("C03 joint ancestry drift.");
                }
                if (joints.Count != 16)
                    throw new InvalidOperationException("C03 QA rig missing bones.");

                var ownerByPart = guide.joints
                    .SelectMany(j => j.parts.Select(p => new {Part=p, Bone=j.id}))
                    .ToDictionary(x => x.Part,x => x.Bone,StringComparer.Ordinal);
                for (var i = 0; i < receipt.semantic_order.Length; i++)
                {
                    var part = receipt.semantic_order[i];
                    var boneName = ownerByPart[part];
                    var center = coords[boneName].pivot_native_estimate_xy;
                    var node = new GameObject(part);
                    node.transform.SetParent(joints[boneName],false);
                    node.transform.localPosition = new Vector3(
                        (64-center[0])/32f, (center[1]-80)/32f,0f);
                    var sprite = node.AddComponent<SpriteRenderer>();
                    sprite.sortingOrder = i;
                    sprite.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(
                        Output + "/layers/" + part + ".png");
                    if (sprite.sprite == null)
                        throw new InvalidOperationException("C03 QA sprite import missing: " + part);
                }
                // Deliberately no Animator/clip: opaque-image cutouts lack
                // hidden limbs, motion will reveal unpainted seams.
                var dest = Output + "/C03_OWNER_SELECTED_REFINED_CUTOUT_QA.prefab";
                PrefabUtility.SaveAsPrefabAsset(root,dest);
                AssetDatabase.SaveAssets();
                var saved = AssetDatabase.LoadAssetAtPath<GameObject>(dest);
                if (saved == null ||
                    saved.GetComponentsInChildren<Transform>(true).Length != 37 ||
                    saved.GetComponentsInChildren<SpriteRenderer>(true).Length != 21 ||
                    saved.GetComponent<Animator>() != null)
                    throw new InvalidOperationException("C03 persisted QA joint hierarchy differs.");
                Debug.Log("[StartupLife][C03] REFINED owner-image 21-layer / 16-joint QA cutout " +
                    "imported without animation or runtime binding. Correct layer seams, " +
                    "hidden anatomy, and native redraw before approving animation.");
            }
            finally
            {
                if (root != null) UnityEngine.Object.DestroyImmediate(root);
            }
        }
    }
}
