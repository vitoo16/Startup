"""Regression fixtures keep the prototype structurally valid, not approved."""
import copy
import importlib.util
import json
import shutil
import tempfile
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
mod_spec = importlib.util.spec_from_file_location(
    "c01_check", ROOT / "scripts/Validate-GoldenCharacter.py")
gate = importlib.util.module_from_spec(mod_spec)
mod_spec.loader.exec_module(gate)
FILES = (gate.SOURCE, gate.SPEC, gate.PROVENANCE, gate.REGISTRY, gate.BUILDER)


class GoldenC01Tests(unittest.TestCase):
    def setUp(self):
        self.tmp = tempfile.TemporaryDirectory()
        self.addCleanup(self.tmp.cleanup)
        self.root = Path(self.tmp.name)
        for f in FILES:
            path = self.root / f
            path.parent.mkdir(parents=True, exist_ok=True)
            shutil.copyfile(ROOT / f, path)

    def change(self, path, mutate):
        dst = self.root / path
        data = json.loads(dst.read_text(encoding="utf-8"))
        mutate(data)
        dst.write_text(json.dumps(data), encoding="utf-8")

    def rejected(self, phrase):
        with self.assertRaisesRegex(AssertionError, phrase):
            gate.validate(self.root)

    def test_untouched_draft_is_structurally_valid(self):
        self.assertEqual(gate.validate(self.root)[0], 20)

    def test_no_autoapproval(self):
        self.change(gate.REGISTRY, lambda d: d["approved_entries"].append({"id": "fake"}))
        self.rejected("auto-approved")

    def test_reject_cyclic_skeleton(self):
        self.change(gate.SOURCE, lambda d: d["parts"][0].update(parent="pelvis"))
        self.rejected("cycle")

    def test_reject_missing_knee(self):
        self.change(gate.SOURCE, lambda d: d["parts"].__setitem__(3, {
            **d["parts"][3], "id": "other_lower_leg"}))
        self.rejected("missing or duplicate")

    def test_reject_clipped_pixels(self):
        self.change(gate.SOURCE, lambda d: d["parts"][0]["rects"][0].update(
            box=[124, 155, 8, 8]))
        self.rejected("invalid painted rectangle")

    def test_reject_pivot_outside_crop(self):
        self.change(gate.SOURCE, lambda d: d["parts"][0].update(joint_px=[0, 0]))
        self.rejected("pivot outside")

    def test_reject_unresolved_swatch(self):
        self.change(gate.SOURCE, lambda d: d["parts"][0]["rects"][0].update(
            color="stolen"))
        self.rejected("invalid painted rectangle")

    def test_reject_claimed_turnaround(self):
        self.change(gate.SOURCE, lambda d: d.update(
            views_completed=["front", "side"]))
        self.rejected("turnaround")

    def test_reject_moved_approval(self):
        self.change(gate.PROVENANCE, lambda d: d["assets"][0].update(
            status="APPROVED"))
        self.rejected("candidate provenance")

    def test_reject_unreviewed_motion_claim(self):
        self.change(gate.SOURCE, lambda d: d.update(
            motion_contract=["idle", "walk"]))
        self.rejected("animation contract")


if __name__ == "__main__":
    unittest.main()
