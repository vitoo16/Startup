"""C03 continuous idle proof: real joints, native seams and bag overlap (not Unity)."""
import importlib.util
import json
from pathlib import Path
import tempfile
import unittest

ROOT=Path(__file__).resolve().parents[2]
SCRIPTS=ROOT/'scripts'
SOURCE=ROOT/'Assets/StartupLife/Art/Source/Characters/golden-character-c03'
SPEC=importlib.util.spec_from_file_location('idle_loop',SCRIPTS/'Preview-C03IdleLoop.py')
loop=importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(loop)


class C03IdleLoopTests(unittest.TestCase):
    def test_cosine_interpolation_has_exact_endpoints_and_no_fake_root_shift(self):
        left={'neck':-2,'elbow_l':4,'hip_r':1}
        right={'neck':2,'elbow_l':-3,'knee_l':2}
        a=loop.interpolate_angles(left,right,0,16)
        b=loop.interpolate_angles(left,right,8,16)
        self.assertEqual(a['neck'],-2)
        self.assertEqual(b['neck'],2)
        self.assertEqual(a['hip_r'],1)
        self.assertEqual(b['hip_r'],0)
        self.assertEqual(a['knee_l'],0)
        self.assertEqual(b['knee_l'],2)
        self.assertNotIn('root',a)
        with self.assertRaisesRegex(ValueError,'even sampling'):
            loop.interpolate_angles(left,right,0,7)

    def test_direct_joint_contact_and_gap_rejection(self):
        w,h=24,24
        left=bytearray(w*h*4)
        right=bytearray(w*h*4)
        left[(10*w+10)*4+3]=255
        right[(11*w+11)*4+3]=255
        self.assertTrue(loop.local_contact(left,right,(10,10),w,h,5,1))
        right=bytearray(w*h*4)
        right[(13*w+13)*4+3]=255
        self.assertFalse(loop.local_contact(left,right,(10,10),w,h,5,1))

    def test_out_of_canvas_or_empty_frame_rejected(self):
        blank=bytearray(128*160*4)
        with self.assertRaisesRegex(ValueError,'Empty'):
            loop.verify_frame_bounds(blank)
        blank[(0*128+40)*4+3]=255
        with self.assertRaisesRegex(ValueError,'boundary'):
            loop.verify_frame_bounds(blank)

    def test_identical_native_frames_are_rejected_by_pixel_diff(self):
        red=bytes((255,0,0,255))*4
        blue=bytes((0,0,255,255))*4
        self.assertEqual(loop.pixel_difference(red,red),0)
        self.assertEqual(loop.pixel_difference(red,blue),4)
        with self.assertRaisesRegex(ValueError,'differently sized'):
            loop.pixel_difference(red,red[:-4])

    def test_contact_plan_must_follow_actual_parent_child_joint_assignment(self):
        rig=loop.load_tool('Preview-C03FrontRig')
        plan=json.loads((SOURCE/'C03-front-joint-plan-DRAFT.json').read_text())
        manifest=json.loads((SOURCE/'C03-native-front-source.json').read_text())
        contract=json.loads((SOURCE/'C03-owner-front-rig-contract.json').read_text())
        joints,assigned=rig.read_plan(plan,contract,manifest)
        image=bytes(128*160*4)
        parts={name:image for name in manifest['part_order']}
        w=rig.world_joints(joints,{})
        mutated=dict(plan['idle_loop_proof'])
        mutated['connections']=[list(a) for a in mutated['connections']]
        mutated['connections'][0][2]='wrist_l'
        with self.assertRaisesRegex(ValueError,'ancestry'):
            loop.inspect_connections(mutated,joints,assigned,parts,w)

    def test_real_native_source_all_sixteen_frames_have_connected_joints(self):
        with tempfile.TemporaryDirectory(prefix='c03-idle-test-') as folder:
            report=loop.render(Path(folder))
            self.assertEqual(report['stage'],'C03_ENGINE_FREE_IDLE_MOTION_PROOF_NOT_UNITY')
            self.assertEqual(report['sample_count'],16)
            self.assertEqual(report['joint_count'],16)
            self.assertEqual(report['semantic_layer_count'],21)
            self.assertEqual(report['contacts_checked_per_frame'],8)
            self.assertGreater(report['min_adjacent_changed_pixels'],0)
            self.assertTrue(report['all_hand_bag_occlusion_checks_passed'])
            self.assertEqual(len(report['frames']),16)
            self.assertTrue(all(f['seam_pairs_checked']==8 for f in report['frames']))
            self.assertTrue(all(f['bag_front_occludes_hand'] for f in report['frames']))
            self.assertTrue((Path(folder)/'C03-idle-loop-sheet-1x-DRAFT.png').is_file())
            self.assertTrue((Path(folder)/'C03-idle-loop-sheet-4x-DRAFT.png').is_file())
            self.assertTrue((Path(folder)/'C03-idle-loop-technical-evidence.json').is_file())
            self.assertFalse(report['owner_final_golden_approval'])


if __name__=='__main__':
    unittest.main()
