"""Deterministic provenance preflight regressions without Unity or external packages."""
import copy
import importlib.util
import json
import tempfile
import unittest
from pathlib import Path

SCRIPT = Path(__file__).resolve().parents[2] / "scripts/Validate-AssetProvenance.py"
SPEC = importlib.util.spec_from_file_location("asset_provenance", SCRIPT)
module = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(module)


class ProvenanceTests(unittest.TestCase):
    def setUp(self):
        self.tmp = tempfile.TemporaryDirectory()
        self.addCleanup(self.tmp.cleanup)
        self.root = Path(self.tmp.name)
        self.active_style = "vietnam_cozy_chibi_cutout_v1"
        self.manifest = {"schema": module.SCHEMA,
                         "proposed_style_id": module.PLANNED, "assets": []}
        self.registry = {"style_id": self.active_style, "approved_entries": []}

    def write(self):
        for location, data in (
            (module.PROVENANCE, self.manifest),
            (module.SPEC, {"style_id": self.active_style}),
            (module.REGISTRY, self.registry),
        ):
            path = self.root / location
            path.parent.mkdir(parents=True, exist_ok=True)
            path.write_text(json.dumps(data), encoding="utf-8")

    def draft(self):
        return {
            "asset_id": "sl-character-main-001", "type": "character",
            "style_id": module.PLANNED, "status": "DRAFT",
            "creator_type": "ai_assisted", "creator_or_licensor_ref": "tool:pixel-draft",
            "created_utc": "2026-10-14T00:00:00Z",
            "source_files": [], "output_files": [],
            "process_reference": "brief:v1",
            "model_tool_and_terms_ref": None, "input_reference_rights": [],
            "third_party_dependencies": [], "license_scope": None,
            "license_evidence_ref": None, "similarity_review_ref": None,
            "technical_import_review_ref": None, "visual_beautycell_ref": None,
            "independent_reviewer": None, "approval_date_utc": None,
            "unity_path": None, "scorecard": None
        }

    def accepted(self):
        self.active_style = module.PLANNED
        self.registry["style_id"] = module.PLANNED
        asset = self.draft()
        src = "Assets/StartupLife/Art/Source/Characters/original.aseprite"
        output = "Assets/StartupLife/Art/Approved/Character/actor.png"
        for path in (src, output):
            target = self.root / path
            target.parent.mkdir(parents=True, exist_ok=True)
            target.write_bytes(b"fixture")
        asset.update({
            "status": "APPROVED", "source_files": [src], "output_files": [output],
            "unity_path": output, "license_scope": "commercial:android-ios-store",
            "license_evidence_ref": "rights:document-123",
            "similarity_review_ref": "review:visual-123",
            "technical_import_review_ref": "unity:import-123",
            "visual_beautycell_ref": "android:beautycell-123",
            "model_tool_and_terms_ref": "terms:2026-10",
            "independent_reviewer": "human:external-art-director",
            "approval_date_utc": "2026-10-14T01:00:00Z",
            "scorecard": {k: 2 for k in module.SCORES},
        })
        self.registry["approved_entries"] = [
            {"asset_id": asset["asset_id"], "style_id": module.PLANNED,
             "unity_path": output}]
        return asset

    def assert_rejected(self, snippet):
        self.write()
        with self.assertRaisesRegex(ValueError, snippet):
            module.validate(self.root)

    def test_empty_manifest_preserves_historical_m13(self):
        self.write()
        self.assertEqual(module.validate(self.root), (0, 0, self.active_style))

    def test_draft_without_fake_evidence_is_valid(self):
        self.manifest["assets"] = [self.draft()]
        self.write()
        self.assertEqual(module.validate(self.root)[0], 1)

    def test_approved_complete_join(self):
        self.manifest["assets"] = [self.accepted()]
        self.write()
        self.assertEqual(module.validate(self.root)[1], 1)

    def test_reject_pre_migration_pixel_approval(self):
        self.manifest["assets"] = [self.accepted()]
        self.active_style = "vietnam_cozy_chibi_cutout_v1"
        self.registry["style_id"] = self.active_style
        self.assert_rejected("before W03")

    def test_reject_duplicate_asset_id(self):
        asset = self.draft()
        self.manifest["assets"] = [asset, copy.deepcopy(asset)]
        self.assert_rejected("duplicate asset_id")

    def test_reject_undocumented_registry_asset(self):
        self.registry["approved_entries"] = [
            {"asset_id": "unknown", "style_id": self.active_style, "unity_path": "bad"}]
        self.assert_rejected("no approved provenance")

    def test_reject_quarantined_approval_join(self):
        asset = self.accepted()
        asset["status"] = "QUARANTINED"
        self.manifest["assets"] = [asset]
        self.assert_rejected("no approved provenance")

    def test_reject_self_review(self):
        asset = self.accepted()
        asset["independent_reviewer"] = asset["creator_or_licensor_ref"]
        self.manifest["assets"] = [asset]
        self.assert_rejected("independent human reviewer")

    def test_reject_missing_license(self):
        asset = self.accepted()
        asset["license_evidence_ref"] = None
        self.manifest["assets"] = [asset]
        self.assert_rejected("license_evidence_ref")

    def test_reject_low_score(self):
        asset = self.accepted()
        asset["scorecard"]["silhouette"] = 0
        self.manifest["assets"] = [asset]
        self.assert_rejected("Golden score")

    def test_reject_path_traversal(self):
        asset = self.accepted()
        asset["source_files"] = ["../somewhere.png"]
        self.manifest["assets"] = [asset]
        self.assert_rejected("unsafe asset path")

    def test_reject_approval_missing_registry(self):
        asset = self.accepted()
        self.registry["approved_entries"] = []
        self.manifest["assets"] = [asset]
        self.assert_rejected("missing from registry")

    def test_reject_unknown_state(self):
        asset = self.draft()
        asset["status"] = "AUTO_APPROVED"
        self.manifest["assets"] = [asset]
        self.assert_rejected("unknown asset status")


if __name__ == "__main__":
    unittest.main()
