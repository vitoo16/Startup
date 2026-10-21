"""Structural source contract: real C03 Editor import is gated and quarantined.

This is NOT evidence of a Unity Editor compile, executed AnimationClip,
runtime renderer or approved character art.
"""
from pathlib import Path
import unittest

ROOT = Path(__file__).resolve().parents[2]
EDITOR = ROOT / "Assets/StartupLife/Scripts/Editor/GoldenCharacterDraftBuilder.cs"
STAGER = ROOT / "scripts/Stage-C03SelectedNativeRig.py"


class C03SelectedNativeEditorSourceTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        content = EDITOR.read_text(encoding="utf-8")
        cls.content = content.split("public static class C03SelectedNativeRigBuilder", 1)[1]

    def test_builder_only_uses_new_selected_native_stage(self):
        source = self.content
        self.assertIn("Build selected native articulated rig (quarantine)", source)
        self.assertIn("artifacts/C03-selected-native-rig/input", source)
        self.assertIn("C03_SELECTED_NATIVE_QA.prefab", source)
        self.assertIn("C03-ACTIVE-OWNER-FRONT-RIG-CONTRACT.json", source)
        self.assertIn("C03-LATEST-MINOR-POLISH-REFERENCE.json", source)
        self.assertNotIn("Generated-V3-Quarantine", source)
        self.assertNotIn("FirstPlayable.unity", source)
        self.assertNotIn("Art/Approved/registry.json", source)

    def test_editor_rechecks_source_binary_and_21_layer_checksums_before_copy(self):
        source = self.content
        preflight = source.split("private static HandoffRecord LoadCheckedHandoff()", 1)[1]
        preflight = preflight.split("[MenuItem(", 1)[0]
        self.assertIn("Sha256(File.ReadAllBytes(source))", preflight)
        self.assertIn("Sha256(File.ReadAllBytes(Input + \"/\" + file.path))", preflight)
        self.assertIn("ARTIST_AUTHORED_LAYERS_SUBMITTED", preflight)
        self.assertIn("doc.files.Length != 21", preflight)
        self.assertIn("doc.joints.Length != 16", preflight)
        self.assertIn("doc.approved_for_gameplay", preflight)
        self.assertLess(source.index("LoadCheckedHandoff()\n        {"),
                        source.index("Directory.CreateDirectory(Output + \"/layers\")"))
        self.assertLess(source.index("var doc = LoadCheckedHandoff();"),
                        source.index("Directory.CreateDirectory(Output + \"/layers\")"))

    def test_editor_builds_real_joint_hierarchy_and_keyframed_clips(self):
        source = self.content
        self.assertIn("node.transform.SetParent(parent, false)", source)
        self.assertIn("node.transform.SetParent(joints[part.bone], false)", source)
        self.assertIn("NativeSpriteImportSettings.ConfigurePivot(", source)
        self.assertIn("FilterMode.Point", source)
        self.assertIn("TextureImporterCompression.Uncompressed", source)
        self.assertIn("importer.SaveAndReimport()", source)
        self.assertIn("AnimatorController.CreateAnimatorControllerAtPath", source)
        self.assertIn("AnimationUtility.SetEditorCurve(clip", source)
        self.assertIn('typeof(Transform), "localEulerAnglesRaw.z"', source)
        self.assertIn("PrefabUtility.SaveAsPrefabAsset(root", source)
        self.assertNotIn('"m_LocalPosition"', source)

    def test_zero_auto_approval_and_no_overwrite_of_artist_assets(self):
        source = self.content
        self.assertIn("Directory.Exists(Output) || AssetDatabase.IsValidFolder(Output)", source)
        self.assertIn("throw new InvalidOperationException(", source)
        self.assertIn("QUARANTINED: Unity PlayMode, art/IP and device QA NOT APPROVED", source)
        self.assertIn("AssetDatabase.LoadAssetAtPath<GameObject>", source)
        self.assertNotIn("ApprovedCharacter", source)
        self.assertNotIn("sceneLoaded", source)


if __name__ == "__main__":
    unittest.main()
