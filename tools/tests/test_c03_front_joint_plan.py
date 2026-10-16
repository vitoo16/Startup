"""Engine-free tests for C03 front 16-joint plan (not Unity animation tests)."""
import importlib.util
import json
from pathlib import Path
import tempfile
import unittest

ROOT=Path(__file__).resolve().parents[2]
SCRIPT=ROOT/'scripts/Preview-C03FrontRig.py'
PLAN=ROOT/'Assets/StartupLife/Art/Source/Characters/golden-character-c03/C03-front-joint-plan-DRAFT.json'
CONTRACT=ROOT/'Assets/StartupLife/Art/Source/Characters/golden-character-c03/C03-owner-front-rig-contract.json'
SOURCE=ROOT/'Assets/StartupLife/Art/Source/Characters/golden-character-c03/C03-native-front-source.json'

spec=importlib.util.spec_from_file_location('c03_joint_preview',SCRIPT)
mod=importlib.util.module_from_spec(spec)
spec.loader.exec_module(mod)


def originals():
    return (json.loads(PLAN.read_text(encoding='utf8')),
            json.loads(CONTRACT.read_text(encoding='utf8')),
            json.loads(SOURCE.read_text(encoding='utf8')))


class C03NativeJointTests(unittest.TestCase):
    def test_all_parts_bound_once_and_sorts_bag_ahead_of_hand(self):
        plan, contract, source=originals()
        joints,parts=mod.read_plan(plan,contract,source)
        self.assertEqual(len(joints),16)
        self.assertEqual(len(parts),21)
        self.assertEqual(parts['hand_r'],'wrist_r')
        self.assertEqual(parts['accessory_front'],'spine')

    def test_parent_child_pivot_has_correct_bind_world_location(self):
        plan,contract,source=originals()
        joints,_=mod.read_plan(plan,contract,source)
        world=mod.world_joints(joints,{})
        for name,joint in joints.items():
            self.assertAlmostEqual(world[name][0],joint['pivot'][0])
            self.assertAlmostEqual(world[name][1],joint['pivot'][1])

    def test_forearm_inherits_elbow_and_shoulder_rotation(self):
        plan,contract,source=originals()
        joints,_=mod.read_plan(plan,contract,source)
        from math import radians
        world=mod.world_joints(joints,{'shoulder_l':5,'elbow_l':-2})
        self.assertAlmostEqual(world['wrist_l'][2],radians(3))
        self.assertNotEqual(world['wrist_l'][0],joints['wrist_l']['pivot'][0])

    def test_reject_duplicate_parts_or_unknown_parent(self):
        plan,contract,source=originals()
        plan['joints'][3]['parts'].append('pelvis')
        with self.assertRaisesRegex(ValueError,'multiple joints'):
            mod.read_plan(plan,contract,source)
        plan,contract,source=originals()
        plan['joints'][3]['parent']='missing_bone'
        with self.assertRaisesRegex(ValueError,'ancestry'):
            mod.read_plan(plan,contract,source)

    def test_rejects_fractional_pivots_and_unapproved_release(self):
        plan,contract,source=originals()
        plan['joints'][2]['pivot'][0]=67.5
        with self.assertRaisesRegex(ValueError,'Non-pixel-aligned'):
            mod.read_plan(plan,contract,source)
        plan,contract,source=originals()
        contract['production_registry_approval']=True
        with self.assertRaisesRegex(ValueError,'conflated'):
            mod.read_plan(plan,contract,source)

    def test_rejects_out_of_bounds_pose_or_hidden_bag_hand(self):
        plan,contract,source=originals()
        plan['poses'][1]['degrees']['neck']=45
        with self.assertRaisesRegex(ValueError,'budget'):
            mod.read_plan(plan,contract,source)
        plan,contract,source=originals()
        contract['occlusion']['covered_part_ids_to_hide_entirely']=['hand_r']
        with self.assertRaisesRegex(ValueError,'sort order'):
            mod.read_plan(plan,contract,source)

    def test_all_three_poses_use_real_native_pixels_and_preserve_occlusion(self):
        with tempfile.TemporaryDirectory() as folder:
            report=mod.render_preview(Path(folder))
            self.assertEqual(report['semantic_layers'],21)
            self.assertEqual(report['joint_count'],16)
            self.assertTrue((Path(folder)/'C03-front-16-joint-pose-proof-1x.png').is_file())
            self.assertEqual([p['pose'] for p in report['frames']],
                             ['bind_front','idle_left_test','idle_right_test'])
            self.assertEqual(report['frames'][0]['pixel_changed_vs_bind'],0)
            self.assertTrue(all(p['bag_front_occludes_hand'] for p in report['frames']))
            self.assertTrue(all(p['hand_visible_below_bag_px']>=8 for p in report['frames']))
            self.assertGreater(report['frames'][1]['pixel_changed_vs_bind'],0)


if __name__=='__main__':
    unittest.main()
