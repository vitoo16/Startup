"""Negative engine-free mutations for the quarantined Unity avatar assembler."""
import importlib.util
import json
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
SPEC = importlib.util.spec_from_file_location(
    "avatar_static", ROOT / "scripts/Validate-CharacterVisualAssembler.py")
validator = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(validator)


class VisualAssemblerSourceChecks(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.source = validator.ASSEMBLER.read_text(encoding="utf-8")
        cls.registry = validator.APPROVED.read_text(encoding="utf-8")

    def test_present_in_project_with_unapproved_registry(self):
        self.assertGreaterEqual(validator.verify(), 10)

    def test_forbid_rig_reconstruction(self):
        with self.assertRaisesRegex(AssertionError, "forbidden production action"):
            validator.check(self.source + "\nnew GameObject(\"bad\");", self.registry)

    def test_forbid_source_asset_loading(self):
        with self.assertRaisesRegex(AssertionError, "forbidden production action"):
            validator.check(self.source + "\nResources.Load<Sprite>(\"raw\");", self.registry)

    def test_required_preflight(self):
        with self.assertRaisesRegex(AssertionError, "invariant missing"):
            validator.check(self.source.replace(
                "TryResolveApprovedSprite(layer.SpriteAssetId, out var sprite)",
                "TryResolveSprite(layer.SpriteAssetId, out var sprite)"), self.registry)

    def test_reject_fake_registry(self):
        changed = json.loads(self.registry)
        changed["approved_entries"] = [{"id": "forged"}]
        with self.assertRaisesRegex(AssertionError, "unexpectedly changed"):
            validator.check(self.source, json.dumps(changed))


if __name__ == "__main__":
    unittest.main()
