"""Falsification cases for the pixel UI color/authority preflight."""
import importlib.util
import json
import shutil
import tempfile
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
SPEC = importlib.util.spec_from_file_location(
    "pixel_ui", ROOT / "scripts/Validate-PixelUi.py")
gate = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(gate)
FILES = [gate.ASSET, gate.ART, gate.VIEW, gate.SCENE_BUILDER,
         *gate.UI_FILES.values(), Path("scripts/Validate-GoldenArt.py")]


class PixelUiRegression(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        for path in FILES:
            dest = self.root / path
            dest.parent.mkdir(parents=True, exist_ok=True)
            shutil.copyfile(ROOT / path, dest)

    def rewrite(self, path, old, new):
        dest = self.root / path
        contents = dest.read_text(encoding="utf-8")
        self.assertIn(old, contents)
        dest.write_text(contents.replace(old, new), encoding="utf-8")

    def rejects(self, substring):
        with self.assertRaisesRegex(AssertionError, substring):
            gate.verify(self.root)

    def test_theme_contrast_and_three_overlays(self):
        outcome = gate.verify(self.root)
        self.assertGreaterEqual(outcome["white_accent"], 4.5)

    def test_reject_legacy_dark_modal(self):
        self.rewrite(gate.UI_FILES["event"], "theme.panel",
                     "new Color(0.08f, 0.12f, 0.18f, 0.98f)")
        self.rejects("legacy HD palette")

    def test_reject_unbound_scene_builder(self):
        self.rewrite(gate.SCENE_BUILDER, 'Assign(life, "uiTheme", theme);',
                     "// theme removed")
        self.rejects("scene build/repair")

    def test_reject_unbound_business_overlay(self):
        self.rewrite(gate.UI_FILES["business"], "view.UiTheme",
                     "view.gameObject")
        self.rejects("authoritative theme")

    def test_reject_dashboard_old_colors(self):
        self.rewrite(gate.UI_FILES["dashboard"], "theme.background",
                     "new Color32(244, 239, 226, 255)")
        self.rejects("old dashboard color")

    def test_reject_low_contrast_palette(self):
        src = self.root / gate.ART
        data = json.loads(src.read_text(encoding="utf-8"))
        data["palette"]["roles"]["accent"] = "#F5F5F5"
        src.write_text(json.dumps(data), encoding="utf-8")
        self.rejects("art and UI theme")

    def test_reject_legacy_fallback(self):
        self.rewrite(gate.VIEW, "public MobileTheme UiTheme",
                     "public MobileTheme OldUiTheme")
        self.rejects("life view")

    def test_reject_missing_button_label_contrast(self):
        self.rewrite(gate.UI_FILES["event"], "text.color = ink ?? theme.ink;",
                     "text.color = Color.white;")
        self.rejects("event text/button contrast")

if __name__ == "__main__":
    unittest.main()
