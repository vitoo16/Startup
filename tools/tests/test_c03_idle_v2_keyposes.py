"""C03 Idle V2 first three hand-authored native keyposes; no runtime approval."""
import importlib.util
import json
from pathlib import Path
import tempfile
import unittest

ROOT=Path(__file__).resolve().parents[2]
ART=ROOT/'docs/evidence/production-art/source/c03-authoring'
SCRIPTS=ROOT/'scripts'

def load(name):
    import sys
    sys.path.insert(0,str(SCRIPTS))
    module=importlib.util.spec_from_file_location(name,SCRIPTS/(name+'.py'))
    instance=importlib.util.module_from_spec(module)
    module.loader.exec_module(instance)
    return instance

preview=load('Render-C03IdleV2Keyposes')
native=load('Render-C03OwnerNativeFront')
checker=load('Validate-C03OwnerRigHandoff')


def inputs():
    manifest=native.expand_source(ART/'C03-native-front-source.json')
    keyposes=json.loads((ART/'C03-idle-v2-keyposes-DRAFT.json').read_text(encoding='utf8'))
    contract=json.loads((ART/'C03-idle-v2-art-direction.json').read_text(encoding='utf8'))
    layers,bag_mask=native.paint(manifest)
    return manifest,keyposes,contract,layers,bag_mask


class C03IdleV2Keyposes(unittest.TestCase):
    def test_actual_source_three_poses_not_rejected_cosine(self):
        manifest,data,rules,base,bag=inputs()
        poses=preview.compile_poses(manifest,data,rules,base)
        result=preview.review_invariants(native,poses,base,bag,checker)
        self.assertEqual(list(poses),['rest','inhale','blink'])
        self.assertEqual(result['rest']['changed_visible_pixels'],0)
        self.assertEqual(result['inhale']['changed_visible_pixels'],27)
        self.assertEqual(result['blink']['changed_visible_pixels'],35)
        for name in poses:
            self.assertEqual(result[name]['overlap_covered_px'],191)
            self.assertEqual(result[name]['hand_visible_below_bag_px'],24)
            self.assertTrue(result[name]['bag_front_occludes_hand'])
        self.assertFalse(data['owner_approved_idle_animation'])

    def test_immutable_hair_shoes_and_bag_all_poses(self):
        manifest,data,rules,base,bag=inputs()
        poses=preview.compile_poses(manifest,data,rules,base)
        for name,parts in poses.items():
            for locked in ('hair_front','hair_back','head','foot_l','foot_r',
                           'hand_l','hand_r','accessory_front','accessory_back'):
                self.assertEqual(parts[locked],base[locked],f'{name} changed {locked}')
            self.assertEqual(bytes(native.composite(parts)[3::4]),
                             bytes(native.composite(base)[3::4]))

    def test_disallows_patches_outside_specified_native_regions(self):
        manifest,data,rules,base,bag=inputs()
        data['keyposes'][1]['patches']=[['foot_l','paint_opaque',145,50,2,'shoe_dark']]
        with self.assertRaisesRegex(ValueError,'Pose-specific'):
            preview.compile_poses(manifest,data,rules,base)
        manifest,data,rules,base,bag=inputs()
        data['keyposes'][2]['patches']=[['head','paint_opaque',48,49,2,'skin']]
        with self.assertRaisesRegex(ValueError,'Pose-specific'):
            preview.compile_poses(manifest,data,rules,base)

    def test_approval_and_source_hash_fail_closed(self):
        manifest,data,rules,base,bag=inputs()
        data['owner_approved_idle_animation']=True
        with self.assertRaisesRegex(ValueError,'contract drift'):
            preview.compile_poses(manifest,data,rules,base)
        manifest,data,rules,base,bag=inputs()
        manifest['packet_sha256']='wrong'
        with self.assertRaisesRegex(ValueError,'contract drift'):
            preview.compile_poses(manifest,data,rules,base)

    def test_native_renderer_writes_preview_not_animation(self):
        with tempfile.TemporaryDirectory(prefix='c03-v2-keyposes-') as folder:
            report=preview.render(Path(folder))
            self.assertEqual(report['status'],'KEYPOSES_DRAFT_AWAITING_OWNER_VISUAL_REVIEW')
            self.assertEqual(report['full_idle_loop'],'NOT_CREATED')
            self.assertEqual(report['unity'],'NOT_RUN')
            self.assertEqual(report['owner_motion_keypose_review'],'PENDING')
            self.assertTrue((Path(folder)/'C03-idle-v2-three-keyposes-4x-DRAFT.png').exists())
            self.assertTrue((Path(folder)/'C03-idle-v2-keypose-evidence.json').exists())


if __name__=='__main__':
    unittest.main()
