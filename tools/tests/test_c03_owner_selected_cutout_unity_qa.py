"""Engine-free contract tests; NOT a Unity compilation or visual rig approval."""
from pathlib import Path
import unittest

ROOT = Path(__file__).resolve().parents[2]
EDITOR = ROOT / "Assets/StartupLife/Scripts/Editor/GoldenCharacterDraftBuilder.cs"
STAGER = ROOT / "scripts/Stage-C03SelectedOwnerCutout.py"
WORKFLOW = ROOT / ".github/workflows/c03-selected-layer-draft.yml"
SOURCE = ROOT / "docs/evidence/production-art/drafts/c03-owner-selected-layer-refined"
GUIDE_SOURCE = ROOT / "docs/evidence/production-art/drafts/c03-owner-selected-layer-study"


class C03OwnerSelectedCutoutUnityQA(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        editor = EDITOR.read_text(encoding="utf-8")
        cls.body = editor.split("public static class C03OwnerSelectedCutoutRigQA", 1)[1]
        cls.authoring = STAGER.read_text(encoding="utf-8")

    def test_preserves_old_rig_and_imports_new_lfs_art(self):
        s = self.body
        self.assertIn("Generated-Owner-Refined-Calibration-QA", s)
        self.assertIn("docs/evidence/production-art/drafts/c03-owner-selected-layer-refined", s)
        self.assertIn("docs/evidence/production-art/drafts/c03-owner-selected-layer-study", s)
        self.assertIn("C03_OWNER_SELECTED_REFINED_OVERLAP_PIVOT_QA.prefab", s)
        self.assertIn("C03-hand-behind-bag-reference.png", s)
        self.assertIn("1999322a19bf99a5954edc3cf4fd87f6c6b9795006781c0c46ee4ff49fdc30fa", s)
        self.assertNotIn("Generated-V3-Quarantine", s)
        self.assertNotIn("FirstPlayable.unity", s)
        self.assertNotIn("Art/Approved/registry.json", s)

    def test_import_refuses_wrong_sha_or_sneaky_approval(self):
        s = self.body
        self.assertIn("receipt.approved_for_gameplay", s)
        self.assertIn("receipt.native_pixel_128x160", s)
        self.assertIn("receipt.composite_matches_visible_source_pixels", s)
        self.assertIn("refined.composite_identical_to_original_selected_source_cutout", s)
        self.assertIn("refined.pixel_colors_changed", s)
        self.assertIn("refined.approved_for_gameplay", s)
        self.assertIn("refined.owner_approved", s)
        self.assertIn("refined.original_fallback_px != receipt.unassigned_foreground_fallback_px", s)
        self.assertIn("C03-refined-layer-ownership-receipt.json", s)
        self.assertIn("DRAFT_SOURCE_CORRECTION_NOT_NATIVE_OR_ANIMATABLE", s)
        self.assertIn("guide.rig_approval", s)
        self.assertIn("C03-selected-overlap-pivot-guide-QA.json", s)
        self.assertIn("var guide = JsonUtility.FromJson<Guide>", s)
        self.assertIn("startup-life.c03.owner-source-overlap-pivot-qa.v1", s)
        self.assertIn("var raw = File.ReadAllText(Input + \"/C03-refined-layer-ownership-receipt.json\")", s)
        self.assertIn("File.ReadAllText(GuideInput + \"/C03-selected-cutout-receipt.json\")", s)
        self.assertIn("var source = Input + \"/\" + rel;", s)
        self.assertIn("fileSha.TryGetValue", s)
        self.assertIn("Digest(source) != sha", s)
        self.assertIn("Digest(SourceImage) != SelectedSha", s)
        self.assertLess(s.index("var inputs = ValidateDraft();"),
                        s.index("Directory.CreateDirectory(Output + \"/layers\")"))
        self.assertIn("Directory.Exists(Output) || AssetDatabase.IsValidFolder(Output)", s)
        self.assertIn("EditorApplication.isPlayingOrWillChangePlaymode", s)

    def test_hierarchy_has_16_joints_and_21_sorted_sprites(self):
        s = self.body
        self.assertIn("guide.joints.Length != 16", s)
        self.assertIn("receipt.semantic_order.Length != 21", s)
        self.assertIn("sprite.sortingOrder = i;", s)
        self.assertIn("node.transform.SetParent(joints[boneName],false)", s)
        self.assertIn("node.transform.SetParent(parent, false)", s)
        self.assertIn("GetComponentsInChildren<Transform>(true).Length != 37", s)
        self.assertIn("GetComponentsInChildren<SpriteRenderer>(true).Length != 21", s)
        self.assertIn("PrefabUtility.SaveAsPrefabAsset(root,dest)", s)

    def test_cutout_is_not_false_native_or_animated(self):
        s = self.body
        self.assertIn("OVERLAP_CENTER_ESTIMATE_QA_ONLY_NOT_NATIVE_OR_ANIMATABLE", s)
        self.assertIn("refined overlap-pivot QA rig (NOT motion ready)", s)
        self.assertIn("importer.spritePixelsPerUnit = Ppu;", s)
        self.assertIn("FilterMode.Point", s)
        self.assertIn("TextureImporterCompression.Uncompressed", s)
        self.assertIn("GetComponent<Animator>() != null", s)
        self.assertNotIn("AnimatorController.CreateAnimatorControllerAtPath", s)
        self.assertNotIn("AnimationUtility.SetEditorCurve", s)

    def test_committed_refined_source_manifest_matches_original_semantic_slots(self):
        import json
        refined = json.loads((SOURCE / "C03-refined-layer-ownership-receipt.json").read_text(encoding="utf-8"))
        original = json.loads((GUIDE_SOURCE / "C03-selected-cutout-receipt.json").read_text(encoding="utf-8"))
        self.assertEqual(refined["canonical_selected_sha256"], original["reference_sha256"])
        self.assertEqual(21, refined["preserved_semantic_layers"])
        self.assertEqual(16, refined["preserved_joint_topology"])
        self.assertEqual(original["unassigned_foreground_fallback_px"], refined["original_fallback_px"])
        self.assertEqual(22, len(refined["per_file_sha256"]))
        self.assertEqual(
            {"layers/" + part + ".png" for part in original["semantic_order"]} | {"layers/masks/bag_body.png"},
            set(refined["per_file_sha256"])
        )
        self.assertTrue(refined["composite_identical_to_original_selected_source_cutout"])
        self.assertFalse(refined["approved_for_gameplay"])
        self.assertFalse(refined["native_128x160_art"])

    def test_ten_overlap_calibrated_qa_pivots_keep_topology_and_source_provenance(self):
        import json
        original = json.loads(
            (GUIDE_SOURCE / "C03-selected-16-joint-guide-DRAFT.json").read_text(encoding="utf-8"))
        calibrated = json.loads(
            (SOURCE / "C03-selected-overlap-pivot-guide-QA.json").read_text(encoding="utf-8"))
        self.assertEqual("startup-life.c03.owner-source-overlap-pivot-qa.v1",
                         calibrated["schema"])
        self.assertEqual("OVERLAP_CENTER_ESTIMATE_QA_ONLY_NOT_NATIVE_OR_ANIMATABLE",
                         calibrated["status"])
        self.assertEqual(original["reference_sha256"], calibrated["reference_sha256"])
        self.assertEqual(16, len(calibrated["joints"]))
        self.assertEqual(10, len(calibrated["pivot_change_provenance"]))
        self.assertFalse(calibrated["rig_approval"])
        self.assertEqual([], calibrated["motion_clips"])
        old_joints = {x["id"]: x for x in original["joints"]}
        corrected = {x["id"]: x for x in calibrated["joints"]}
        self.assertEqual(set(old_joints), set(corrected))
        for name, part in corrected.items():
            baseline = old_joints[name]
            self.assertEqual(baseline["parent"], part["parent"])
            self.assertEqual(baseline["parts"], part["parts"])
            if name in calibrated["pivot_change_provenance"]:
                provenance = calibrated["pivot_change_provenance"][name]
                self.assertEqual(provenance["candidate_native_xy"],
                                 part["pivot_native_estimate_xy"])
                self.assertEqual(provenance["previous_native_xy"],
                                 baseline["pivot_native_estimate_xy"])
                self.assertGreater(provenance["source_overlap_pixels"], 0)
            else:
                self.assertEqual(baseline["pivot_native_estimate_xy"],
                                 part["pivot_native_estimate_xy"])
        self.assertEqual([91, 110], corrected["wrist_r"]["pivot_native_estimate_xy"])
        self.assertEqual([79, 113], corrected["hip_r"]["pivot_native_estimate_xy"])
        self.assertIn("scripts/Verify-C03OverlapPivotGuide.py",
                      (ROOT / ".github/workflows/c03-committed-refined-qa.yml")
                      .read_text(encoding="utf-8"))

    def test_refined_qa_import_crops_transparent_texture_area_without_shifting_pose(self):
        importer = self.body
        cropper_path = ROOT / "Assets/StartupLife/Scripts/Editor/C03RefinedSpriteCropper.cs"
        cropper = cropper_path.read_text(encoding="utf-8")
        meta = (ROOT / "Assets/StartupLife/Scripts/Editor/C03RefinedSpriteCropper.cs.meta").read_text(encoding="utf-8")

        self.assertIn("C03RefinedSpriteCropper.CropToSprite(", importer)
        self.assertIn("var trimOffsets = new Dictionary<string, Vector2>", importer)
        self.assertIn("var trimOffset = trimOffsets[part];", importer)
        self.assertIn("(64-center[0])/32f + trimOffset.x", importer)
        self.assertIn("(center[1]-80)/32f + trimOffset.y", importer)
        self.assertNotIn("File.Copy(Input + \"/layers/\" + part + \".png\"", importer)

        self.assertIn("SourceWidth = 1122", cropper)
        self.assertIn("SourceHeight = 1402", cropper)
        self.assertIn("TransparentPaddingPixels = 2", cropper)
        self.assertIn("pixels[y * SourceWidth + x].a == 0", cropper)
        self.assertIn("Array.Copy(pixels, (bottom + y) * SourceWidth + left", cropper)
        self.assertIn("ImageConversion.EncodeToPNG(trimmed)", cropper)
        self.assertIn("File.WriteAllBytes(assetDestination, encoded)", cropper)
        self.assertIn("(left + width * .5f - SourceWidth * .5f) / pixelsPerUnit", cropper)
        self.assertIn("(bottom + height * .5f - SourceHeight * .5f) / pixelsPerUnit", cropper)
        self.assertIn("DestroyImmediate(trimmed)", cropper)
        self.assertIn("DestroyImmediate(source)", cropper)
        self.assertIn("guid: dd9bd043bd96471a8daaf6232693c08a", meta)

    def test_pivot_preview_is_isolated_from_scene_and_contains_no_production_animation(self):
        path = ROOT / "Assets/StartupLife/Scripts/Editor/C03RefinedRigPosePreviewWindow.cs"
        src = path.read_text(encoding="utf-8")
        meta = path.with_suffix(".cs.meta").read_text(encoding="utf-8")
        self.assertIn("PreviewRenderUtility()", src)
        self.assertIn("preview.AddSingleGO(instance)", src)
        self.assertIn("preview.Cleanup()", src)
        self.assertIn("instance.hideFlags = HideFlags.HideAndDontSave", src)
        self.assertIn("Generated-Owner-Refined-Calibration-QA", src)
        self.assertIn("editableJoints.Length != 15", src)
        self.assertIn("GetComponentsInChildren<SpriteRenderer>(true).Length != 21", src)
        self.assertIn("joint.localRotation = Quaternion.Euler(0f, 0f, nextAngle)", src)
        self.assertIn("joint.localRotation = Quaternion.identity", src)
        self.assertIn("preview.camera.Render()", src)
        self.assertIn("guid: ffc71a74bea64e169cb9b5d96f41975d", meta)
        self.assertNotIn("PrefabUtility.SaveAsPrefabAsset", src)
        self.assertNotIn("EditorSceneManager.SaveScene", src)
        self.assertNotIn("AnimationClip", src)
        self.assertNotIn("AnimatorController", src)
        self.assertNotIn("FirstPlayable", src)
        self.assertNotIn("Art/Approved", src)

    def test_real_selected_image_cutout_source_provenance_and_review_branch(self):
        s = self.authoring
        self.assertIn("SHA = \"1999322a19bf99a5954edc3cf4fd87f6c6b9795006781c0c46ee4ff49fdc30fa\"", s)
        self.assertIn("exact_foreground", s)
        self.assertIn("expected_composite", s)
        self.assertIn("joint_count", s)
        flow = WORKFLOW.read_text(encoding="utf-8")
        self.assertIn("permissions:\n  contents: read", flow)
        self.assertNotIn("git lfs push origin HEAD", flow)
        self.assertNotIn("git push origin", flow)
        self.assertIn("assert receipt['layer_count']==21", flow)


if __name__ == "__main__":
    unittest.main()
