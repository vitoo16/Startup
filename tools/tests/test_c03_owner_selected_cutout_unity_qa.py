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
        self.assertIn("Generated-Owner-Refined-Cutout-QA", s)
        self.assertIn("docs/evidence/production-art/drafts/c03-owner-selected-layer-refined", s)
        self.assertIn("docs/evidence/production-art/drafts/c03-owner-selected-layer-study", s)
        self.assertIn("C03_OWNER_SELECTED_REFINED_CUTOUT_QA.prefab", s)
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
        self.assertIn("PIVOT_ESTIMATES_ONLY_NOT_NATIVE_NOT_ANIMATABLE", s)
        self.assertIn("REFINED cutout QA rig (NOT motion ready)", s)
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
