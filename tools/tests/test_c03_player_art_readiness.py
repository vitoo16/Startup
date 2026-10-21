"""Identity-only C03 art readiness checks; never auto-approve or import drafts."""
import hashlib
import importlib.util
import json
from pathlib import Path
import struct
import tempfile
import unittest

REPO = Path(__file__).resolve().parents[2]
SCRIPT = REPO / "scripts/Check-C03PlayerArtReadiness.py"
spec = importlib.util.spec_from_file_location("c03_readiness", SCRIPT)
module = importlib.util.module_from_spec(spec)
spec.loader.exec_module(module)


def fake_png():
    # Small PNG identity fixture. We test SHA-256 and PNG header; actual pixel
    # decoding/import/BeautyCell remains exclusively Unity's responsibility.
    header = b"\x89PNG\r\n\x1a\n"
    return header + struct.pack(">I", 13) + b"IHDR" + struct.pack(">II", 1122, 1402) + b"\x08\x02\x00\x00\x00"


class C03ReadinessTests(unittest.TestCase):
    def setUp(self):
        self.tmp = tempfile.TemporaryDirectory(prefix="c03-asset-gate-")
        self.addCleanup(self.tmp.cleanup)
        self.root = Path(self.tmp.name)
        record = {
            "latest_reference": {
                "filename": "image(20261021-124610).png",
                "sha256": hashlib.sha256(fake_png()).hexdigest(),
                "width": 1122, "height": 1402,
                "expected_repo_draft_path": "Assets/StartupLife/Art/Source/Characters/golden-character-c03/references/C03-hand-behind-bag-reference.png",
            }
        }
        reference = self.root / module.REFERENCE
        reference.parent.mkdir(parents=True, exist_ok=True)
        reference.write_text(json.dumps(record), encoding="utf-8")
        registry = self.root / module.REGISTRY
        registry.parent.mkdir(parents=True, exist_ok=True)
        registry.write_text(json.dumps({"approved_entries": []}), encoding="utf-8")
        self.target = self.root / record["latest_reference"]["expected_repo_draft_path"]
        self.target.parent.mkdir(parents=True, exist_ok=True)

    def test_missing_selected_original_is_explicitly_blocked(self):
        result = module.diagnose(self.root)
        self.assertFalse(result["reference_verified"])
        self.assertFalse(result["gameplay_character_ready"])
        self.assertEqual("MISSING_OR_INCORRECT_REFERENCE", result["status"])
        self.assertIn("no shipping character assets", " ".join(result["blocking_reasons"]))

    def test_exact_source_hash_only_verifies_reference_not_runtime(self):
        self.target.write_bytes(fake_png())
        result = module.diagnose(self.root)
        self.assertTrue(result["source_sha256_matches"])
        self.assertTrue(result["source_dimensions_match"])
        self.assertTrue(result["reference_verified"])
        self.assertEqual("REFERENCE_VERIFIED_NOT_GAMEPLAY_READY", result["status"])
        self.assertFalse(result["gameplay_character_ready"])
        self.assertEqual(0, result["approved_asset_count"])

    def test_changed_png_same_resolution_must_fail(self):
        self.target.write_bytes(fake_png() + b"modified-visual-content")
        result = module.diagnose(self.root)
        self.assertFalse(result["source_sha256_matches"])
        self.assertTrue(result["source_dimensions_match"])
        self.assertFalse(result["reference_verified"])
        self.assertIn("SHA-256 differs", " ".join(result["blocking_reasons"]))

    def test_wrong_png_dimensions_must_fail(self):
        image = bytearray(fake_png())
        image[16:24] = struct.pack(">II", 1171, 1343)
        self.target.write_bytes(image)
        result = module.diagnose(self.root)
        self.assertFalse(result["source_dimensions_match"])
        self.assertFalse(result["reference_verified"])

    def test_isolated_editor_preview_uses_exact_hash_and_never_touches_gameplay(self):
        source = (REPO / "Assets/StartupLife/Scripts/Editor/C03SelectedVisualPreviewEditor.cs").read_text(encoding="utf-8")
        self.assertIn("Startup Life/Art/C03/Open selected visual reference in Game View", source)
        self.assertIn("SHA256.Create()", source)
        self.assertIn("reference.sha256", source)
        self.assertIn("reference.expected_repo_draft_path", source)
        self.assertIn("TextureImporterType.Sprite", source)
        self.assertIn("importer.SaveAndReimport()", source)
        self.assertIn("AssetDatabase.LoadAssetAtPath<Sprite>(path)", source)
        self.assertIn("SaveCurrentModifiedScenesIfUserWantsTo()", source)
        self.assertIn("NewSceneSetup.EmptyScene", source)
        self.assertIn("C03_OPAQUE_REFERENCE_NOT_GAMEPLAY", source)
        self.assertNotIn("FirstPlayable.unity", source)
        self.assertNotIn("PrefabUtility.SaveAsPrefabAsset", source)
        self.assertNotIn("Art/Approved/registry.json", source)
        self.assertNotIn("File.Write", source)

    def test_actual_repository_has_no_shipping_registration(self):
        result = module.diagnose(REPO)
        self.assertEqual(0, result["approved_asset_count"],
            "An approval is a separate review gate; update this check only with art evidence.")
        self.assertFalse(result["gameplay_character_ready"])
        if result["source_file_present"]:
            self.assertTrue(result["reference_verified"],
                "The current owner-selected reference in Git must match its exact hash.")
        else:
            self.assertFalse(result["reference_verified"])


if __name__ == "__main__":
    unittest.main()
