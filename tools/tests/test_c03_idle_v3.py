"""C03 Idle V3: actual native sprite, frozen eye blink and preview regression tests."""
import importlib.util
import json
import tempfile
from pathlib import Path
import unittest

ROOT=Path(__file__).resolve().parents[2]
SCRIPTS=ROOT/'scripts'
ART=ROOT/'Assets/StartupLife/Art/Source/Characters/golden-character-c03'
SPEC=importlib.util.spec_from_file_location('c03v3',SCRIPTS/'Render-C03IdleV3.py')
v3module=importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(v3module)


def loaded():
    painter=v3module.load('Render-C03OwnerNativeFront')
    prior=v3module.load('Render-C03IdleV2Keyposes')
    checker=v3module.load('Validate-C03OwnerRigHandoff')
    source=painter.expand_source(ART/'C03-native-front-source.json')
    body,mask=painter.paint(source)
    older=json.loads((ART/'C03-idle-v2-keyposes-DRAFT.json').read_text())
    rule=json.loads((ART/'C03-idle-v2-art-direction.json').read_text())
    closed=prior.compile_poses(source,older,rule,body)['blink']
    direction=json.loads((ART/'C03-idle-v3-stable-blink-DRAFT.json').read_text())
    return painter,checker,source,body,mask,closed,direction


class C03IdleV3Tests(unittest.TestCase):
    def test_rest_frozen_and_closed_exact_inherited_v2(self):
        painter,checker,source,base,mask,closed,data=loaded()
        poses=v3module.assemble(source,data,base,closed)
        stats=v3module.quality(painter,checker,source,data,poses,mask)
        self.assertEqual(stats['open']['changed_visible_pixels'],0)
        self.assertEqual(stats['half']['changed_visible_pixels'],19)
        self.assertEqual(stats['closed']['changed_visible_pixels'],35)
        self.assertEqual(v3module.pixel_diffs(painter.composite(base),painter.composite(poses['half']))[0][1],47)
        self.assertEqual({name:(stat['overlap_covered_px'],stat['hand_visible_below_bag_px'])
                          for name,stat in stats.items()},
                         {name:(191,24) for name in ('open','half','closed')})
        for pose in poses.values():
            for frozen in data['frozen_parts']:
                self.assertEqual(base[frozen],pose[frozen])

    def test_outside_eye_roi_and_frozen_layers_fail_closed(self):
        painter,checker,source,base,mask,closed,data=loaded()
        data['half_close_edits'].append(['erase_iris',95,55,1])
        with self.assertRaisesRegex(ValueError,'outside frozen eye ROI'):
            v3module.assemble(source,data,base,closed)
        painter,checker,source,base,mask,closed,data=loaded()
        broken=dict(closed)
        broken['hair_front']=bytes(len(closed['hair_front']))
        with self.assertRaisesRegex(ValueError,'frozen layer'):
            v3module.assemble(source,data,base,broken)

    def test_no_inhale_in_active_source_or_playback(self):
        painter,checker,source,base,mask,closed,data=loaded()
        self.assertTrue(data['constraints']['no_inhale'])
        self.assertEqual(data['owner_verdict'],'REJECT_INHALE_MOTION')
        poses=v3module.playback_timing(data)
        self.assertEqual(sum(duration for _,duration in poses),12360)
        self.assertEqual(sum(name=='closed' for name,_ in poses),2)
        self.assertEqual(poses[0][0],poses[-1][0])
        self.assertTrue(all(name in ('open','half','closed') for name,_ in poses))
        data['timing_profile']['total_blink_ms']=400
        with self.assertRaisesRegex(ValueError,'timing'):
            v3module.playback_timing(data)

    def test_only_iris_ink_can_be_replaced(self):
        painter,checker,source,base,mask,closed,data=loaded()
        data['half_close_edits'][0]=['erase_iris',49,60,1]
        with self.assertRaisesRegex(ValueError,'existing ink iris'):
            v3module.assemble(source,data,base,closed)

    def test_export_real_timed_gif_and_review_assets(self):
        from PIL import Image
        with tempfile.TemporaryDirectory(prefix='c03-v3-') as folder:
            qa=v3module.export(Path(folder))
            self.assertEqual(qa['status'],'IDLE_V3_BLINK_PLAYBACK_AWAITING_OWNER_REVIEW')
            self.assertEqual(qa['playback_total_ms'],12360)
            self.assertEqual(qa['blink_count_in_sample'],2)
            self.assertEqual(qa['unity_editor'],'NOT_RUN')
            for name in ('C03-Idle-V3-open-1x.png','C03-Idle-V3-half-1x.png',
                         'C03-Idle-V3-closed-1x.png','C03-Idle-V3-Blink-Sheet-4x.png',
                         'C03-Idle-V3-Eyes-10x.png','C03-Idle-V3-Timed-Playback.gif',
                         'C03-Idle-V3-Gameplay-Scale-MOCK.png','C03-Idle-V3-QA.json'):
                self.assertTrue((Path(folder)/name).exists(),name)
            with Image.open(Path(folder)/'C03-Idle-V3-Timed-Playback.gif') as gif:
                self.assertEqual(gif.n_frames,9)
                self.assertEqual(sum(gif.seek(i) or gif.info['duration'] for i in range(9)),12360)


if __name__=='__main__':
    unittest.main()
