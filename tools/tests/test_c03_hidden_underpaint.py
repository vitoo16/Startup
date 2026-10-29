"""C03 non-shipping joint underpaint: exact occluded-only pixel contract."""
import importlib.util
from pathlib import Path
import unittest
from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
SCRIPT = ROOT / "scripts/Stage-C03HiddenSeamUnderpaint.py"
spec = importlib.util.spec_from_file_location("c03_underpaint_study", SCRIPT)
module = importlib.util.module_from_spec(spec)
spec.loader.exec_module(module)


class HiddenSeamUnderpaintTests(unittest.TestCase):
    @staticmethod
    def fixture():
        a = Image.new("RGBA", (20, 20))
        b = Image.new("RGBA", (20, 20))
        for x in range(4, 9):
            for y in range(5, 12):
                a.putpixel((x, y), (23, 120, 81, 255))
        for x in range(9, 17):
            for y in range(7, 13):
                b.putpixel((x, y), (144, 68, 42, 255))
        return a, b

    def test_adds_only_pixels_behind_full_opacity_and_preserves_bind_composite(self):
        a, b = self.fixture()
        after, count = module.underpaint_pair(a, b, 4)
        self.assertGreater(count, 0)
        self.assertEqual(a.getpixel((9, 8))[3], 0)
        self.assertEqual(after.getpixel((9, 8)), (23, 120, 81, 255))
        self.assertEqual(after.getpixel((18, 8))[3], 0)
        self.assertEqual(after.getpixel((9, 13))[3], 0)
        # Front only 255 alpha is allowed to cover newly authored pixels.
        before_composite = Image.alpha_composite(a, b)
        after_composite = Image.alpha_composite(after, b)
        self.assertEqual(before_composite.tobytes(), after_composite.tobytes())
        self.assertEqual(a.getpixel((9, 8))[3], 0)  # never mutates input

    def test_reproducible_and_bounded(self):
        a, b = self.fixture()
        a1, n1 = module.underpaint_pair(a, b, 2)
        a2, n2 = module.underpaint_pair(a, b, 2)
        self.assertEqual(a1.tobytes(), a2.tobytes())
        self.assertEqual(n1, n2)
        self.assertGreater(n1, 0)
        a3, n3 = module.underpaint_pair(a, b, 5)
        self.assertGreater(n3, n2)
        with self.assertRaises(ValueError):
            module.underpaint_pair(a, b, 50)

    def test_does_not_underpaint_partially_transparent_front(self):
        a, b = self.fixture()
        b.putpixel((9, 8), (144, 68, 42, 140))
        after, _ = module.underpaint_pair(a, b, 4)
        self.assertEqual(after.getpixel((9, 8))[3], 0)

    def test_pixel_delta_reports_exact_changed_bbox_and_bind_identity(self):
        before = Image.new("RGBA", (12, 10), (248, 242, 230, 255))
        after = before.copy()
        mask, count, bbox = module.pixel_delta(before, after)
        self.assertEqual(0, count)
        self.assertIsNone(bbox)
        self.assertEqual(0, sum(mask.tobytes()))

        after.putpixel((4, 3), (255, 0, 210, 255))
        after.putpixel((6, 5), (255, 0, 210, 255))
        mask, count, bbox = module.pixel_delta(before, after)
        self.assertEqual(2, count)
        self.assertEqual((4, 3, 7, 6), bbox)
        self.assertEqual(255, mask.getpixel((4, 3)))
        self.assertEqual(0, mask.getpixel((5, 4)))

    def test_candidate_uses_calibrated_pivots_and_covers_both_walk_poses(self):
        source = SCRIPT.read_text(encoding="utf-8")
        self.assertIn('PIVOT_VERIFY = ROOT / "scripts/Verify-C03OverlapPivotGuide.py"', source)
        self.assertIn('C03-selected-overlap-pivot-guide-QA.json', source)
        self.assertIn('PASS_TEN_SOURCE_OVERLAP_PIVOTS_NOT_MOTION_APPROVAL', source)
        self.assertIn('pose_names = ("bind_exact", "left_arm_bend", "right_arm_bend",', source)
        self.assertIn('"walk_leg_A", "walk_leg_B")', source)
        self.assertIn('"C03-underpaint-changed-pixel-hotspots-REVIEW.png"', source)
        self.assertIn('"pivot_guide_sha256": sha256(refined_guide_path)', source)
        self.assertIn('"rig_and_motion_verified_in_Unity": False', source)
        self.assertIn('"approved_for_gameplay": False', source)
        self.assertIn('"underpaint_motion_eligible": False', source)
        self.assertIn('"FAIL_AUTO_UNDERPAINT_EXPOSES_PIXELS"', source)
        self.assertIn('"newly_visible_alpha_total": newly_visible_total', source)

    def test_preserves_21_slot_identity_and_occlusion_order(self):
        names = [
            "hair_back", "accessory_back", "foot_l", "foot_r",
            "lower_leg_l", "lower_leg_r", "upper_leg_l", "upper_leg_r",
            "pelvis", "torso", "upper_arm_l", "upper_arm_r",
            "forearm_l", "forearm_r", "hand_l", "hand_r", "outfit_torso",
            "head", "face", "hair_front", "accessory_front"]
        self.assertEqual(len(names), 21)
        self.assertTrue(all(names.index(a) < names.index(b)
                            for a, b, _ in module.PAIRS))
        self.assertTrue(any(a == "hand_r" and b == "accessory_front"
                            for a, b, _ in module.PAIRS))


if __name__ == "__main__":
    unittest.main()
