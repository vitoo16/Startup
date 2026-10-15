"""C02 pixel source mutation QA; never claims physical BeautyCell or IP approval."""
import copy
import importlib.util
import json
import shutil
import tempfile
import unittest
from pathlib import Path

ROOT=Path(__file__).resolve().parents[2]
spec=importlib.util.spec_from_file_location("c02_gate",ROOT/"scripts/Validate-GoldenCharacterC02.py")
gate=importlib.util.module_from_spec(spec)
spec.loader.exec_module(gate)
FILES=(gate.SOURCE,gate.REGISTRY,gate.ART_SPEC,gate.RENDER,
       Path("scripts/PixelPng.py"),
       Path("Assets/StartupLife/Art/_ArtDirection/provenance/manifest.json"))


class GoldenC02Checks(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.valid=gate.validate(ROOT)

    def setUp(self):
        self.tmp=tempfile.TemporaryDirectory()
        self.addCleanup(self.tmp.cleanup)
        self.root=Path(self.tmp.name)
        for p in FILES:
            dest=self.root/p
            dest.parent.mkdir(parents=True,exist_ok=True)
            shutil.copyfile(ROOT/p,dest)

    def mutate(self,path,update):
        p=self.root/path
        data=json.loads(p.read_text(encoding="utf8"))
        update(data)
        p.write_text(json.dumps(data),encoding="utf8")

    def test_original_nine_candidate_turnarounds(self):
        self.assertEqual(len(self.valid["samples"]),36)
        self.assertEqual(self.valid["candidates"],9)
        self.assertGreaterEqual(self.valid["panel_size"][0],1000)

    def test_head_ratio_cannot_revert_to_old_style(self):
        self.mutate(gate.SOURCE,lambda d:d["art_direction"].update(
            head_vertical_span_px=[100,157]))
        with self.assertRaises(AssertionError):gate.validate(self.root)

    def test_cannot_claim_originality_approval(self):
        self.mutate(gate.SOURCE,lambda d:d["approval"].update(ip_review=True))
        with self.assertRaises(AssertionError):gate.validate(self.root)

    def test_owner_visual_rejection_cannot_silently_become_approved(self):
        self.mutate(gate.SOURCE, lambda d: d["owner_visual_review"].update(
            status="APPROVED", release_blocked=False))
        with self.assertRaises(AssertionError): gate.validate(self.root)

    def test_no_autoapproval(self):
        self.mutate(gate.REGISTRY,lambda d:d["approved_entries"].append(
            {"asset_id":"not-reviewed"}))
        with self.assertRaises(AssertionError):gate.validate(self.root)

    def test_lost_back_turnaround_rejected(self):
        self.mutate(gate.SOURCE,lambda d:d["views"].pop())
        with self.assertRaises(AssertionError):gate.validate(self.root)

    def test_cycle_in_joint_tree_rejected(self):
        self.mutate(gate.SOURCE,lambda d:d["rig"][0].update(parent="head"))
        with self.assertRaises(AssertionError):gate.validate(self.root)

    def test_hidden_duplicate_candidate_rejected(self):
        self.mutate(gate.SOURCE,lambda d:d["variants"][1].update(
            id=d["variants"][0]["id"]))
        with self.assertRaises(AssertionError):gate.validate(self.root)

    def test_render_rejects_unbounded_pixel_rect(self):
        render_spec=importlib.util.spec_from_file_location("c02_render",ROOT/gate.RENDER)
        renderer=importlib.util.module_from_spec(render_spec)
        render_spec.loader.exec_module(renderer)
        data=json.loads((ROOT/gate.SOURCE).read_text(encoding="utf8"))
        layers=renderer.shapes(data,data["variants"][0],data["views"][0])
        layers["head"].append((127,130,3,10,"skin_base"))
        # Runtime renderer must validate clip boundaries instead of silently
        # clipping offscreen malformed source clusters.
        with self.assertRaises(ValueError):
            renderer.render_part(layers["head"],data["palette"])


if __name__=="__main__":
    unittest.main()
