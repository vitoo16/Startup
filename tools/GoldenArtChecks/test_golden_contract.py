"""Deterministic negative fixtures for the authoritative pixel technical contract."""
import importlib.util
import json
import shutil
import tempfile
import unittest
from pathlib import Path

SOURCE = Path(__file__).resolve().parents[2]
SPEC = importlib.util.spec_from_file_location(
    "pixel_golden", SOURCE / "scripts/Validate-GoldenArt.py")
gate = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(gate)
ART = "Assets/StartupLife/Art/_ArtDirection/"
REG = "Assets/StartupLife/Art/Approved/registry.json"
THEME = "Assets/StartupLife/Data/MobileTheme.asset"
CS = "Assets/StartupLife/Scripts/Presentation/MobileTheme.cs"
INPUTS = [ART + "art-spec.yaml", ART + "composition.yaml",
          ART + "golden-targets.json", REG, THEME, CS]


class PixelContractCases(unittest.TestCase):
    def setUp(self):
        self.tmp = tempfile.TemporaryDirectory()
        self.addCleanup(self.tmp.cleanup)
        self.root = Path(self.tmp.name)
        for name in INPUTS:
            dest = self.root / name
            dest.parent.mkdir(parents=True, exist_ok=True)
            shutil.copyfile(SOURCE / name, dest)

    def change(self, path, fn):
        dest = self.root / path
        obj = json.loads(dest.read_text(encoding="utf-8"))
        fn(obj)
        dest.write_text(json.dumps(obj), encoding="utf-8")

    def rejected(self, detail):
        with self.assertRaisesRegex(AssertionError, detail):
            gate.verify(self.root)

    def test_empty_honest_pixel_contract(self):
        self.assertEqual(gate.verify(self.root), 5)

    def test_legacy_style_rejected(self):
        self.change(REG, lambda j: j.update(style_id="vietnam_cozy_chibi_cutout_v1"))
        self.rejected("style drift")

    def test_wrong_ppu_rejected(self):
        self.change(ART + "composition.yaml", lambda j: j["camera"].update(
            pixels_per_unit=100))
        self.rejected("pixel family")

    def test_built_in_pixel_camera_rejected(self):
        self.change(ART + "composition.yaml", lambda j: j["camera"].update(
            pixel_perfect_component="UnityEngine.U2D.PixelPerfectCamera"))
        self.rejected("non URP")

    def test_bilinear_rejected(self):
        self.change(ART + "art-spec.yaml", lambda j: j["craft"].update(
            filter_mode="Bilinear"))
        self.rejected("sampling")

    def test_mipmap_rejected(self):
        self.change(ART + "art-spec.yaml", lambda j: j["craft"].update(
            mip_maps=True))
        self.rejected("sampling")

    def test_palette_drift_rejected(self):
        self.change(ART + "art-spec.yaml", lambda j: j["palette"]["roles"].update(
            background="#FFFFFF"))
        self.rejected("palette drift")

    def test_builder_palette_drift_rejected(self):
        path = self.root / CS
        path.write_text(path.read_text(encoding="utf-8").replace(
            "new Color32(246, 235, 212, 255)",
            "new Color32(245, 235, 212, 255)"), encoding="utf-8")
        self.rejected("palette drift")

    def test_unapproved_asset_is_never_shipped(self):
        self.change(REG, lambda j: j["approved_entries"].append(
            {"asset_id": "fake", "style_id": "vn_everyday_pixel_v1"}))
        self.rejected("unverified art approved")

    def test_fake_golden_approval_rejected(self):
        self.change(ART + "golden-targets.json", lambda j: j["targets"][0].update(
            approved=True))
        self.rejected("unverified Golden")

    def test_low_golden_threshold_rejected(self):
        self.change(ART + "golden-targets.json", lambda j: j["approval_policy"].update(
            golden_threshold=13))
        self.rejected("Golden threshold")

    def test_native_tmp_never_low_res(self):
        self.change(ART + "composition.yaml", lambda j: j["ui_policy"].update(
            native_resolution_tmp=False))
        self.rejected("low res")

    def test_reject_slide_as_walk(self):
        self.change(ART + "art-spec.yaml", lambda j: j["characters"].update(
            rigid_sprite_translation_does_not_count_as_walk=False))
        self.rejected("fake joint")


if __name__ == "__main__":
    unittest.main()
