"""C03 V2.1 Inhale contour specific regression — not animation approval."""
import importlib.util
import json
from pathlib import Path
import tempfile
import unittest

ROOT=Path(__file__).resolve().parents[2]
ART=ROOT/'docs/evidence/production-art/source/c03-authoring'
SCRIPTS=ROOT/'scripts'


def load(name):
    spec=importlib.util.spec_from_file_location(name,SCRIPTS/(name+'.py'))
    obj=importlib.util.module_from_spec(spec)
    spec.loader.exec_module(obj)
    return obj


revision=load('Render-C03IdleV21Inhale')
painter=load('Render-C03OwnerNativeFront')
checker=load('Validate-C03OwnerRigHandoff')
older=load('Render-C03IdleV2Keyposes')


def fixtures():
    manifest=painter.expand_source(ART/'C03-native-front-source.json')
    source,mask=painter.paint(manifest)
    v21=json.loads((ART/'C03-idle-v2.1-inhale-contour-DRAFT.json').read_text(encoding='utf8'))
    parent=json.loads((ART/'C03-idle-v2-keyposes-DRAFT.json').read_text(encoding='utf8'))
    direction=json.loads((ART/'C03-idle-v2-art-direction.json').read_text(encoding='utf8'))
    return manifest,source,mask,v21,parent,direction


class C03IdleV21InhaleTests(unittest.TestCase):
    def test_actual_native_rest_blink_unchanged_and_local_alpha_contour(self):
        manifest,source,mask,spec,parent,direction=fixtures()
        before=older.compile_poses(manifest,parent,direction,source)
        inhale,counts=revision.compile_inhale(spec,manifest,source)
        self.assertEqual(before['rest'],source)
        self.assertEqual(counts['added_alpha_by_arm'],{'upper_arm_l':4,'upper_arm_r':5})
        self.assertEqual(counts['removed_alpha'],0)
        metrics=revision.validate(painter,checker,source,inhale,mask)
        self.assertEqual(metrics['changed_visible_pixels'],21)
        self.assertEqual(metrics['changed_alpha_pixels'],7)
        self.assertEqual(metrics['overlap_covered_px'],191)
        self.assertEqual(metrics['hand_visible_below_bag_px'],24)
        for name in spec['limits']['freeze_parts']:
            self.assertEqual(inhale[name],source[name],name)
        self.assertNotEqual(painter.composite(inhale),painter.composite(source))
        self.assertFalse(spec['owner_approved_inhale'])

    def test_disallow_changing_regions_even_if_keypose_has_valid_colors(self):
        manifest,source,mask,spec,parent,direction=fixtures()
        spec['patches'].append(['upper_arm_l','add_opaque',97,40,1,'ink'])
        with self.assertRaisesRegex(ValueError,'ROI'):
            revision.compile_inhale(spec,manifest,source)
        manifest,source,mask,spec,parent,direction=fixtures()
        spec['patches'].append(['hair_front','repaint',28,70,1,'hair'])
        with self.assertRaisesRegex(ValueError,'non-authorized'):
            revision.compile_inhale(spec,manifest,source)
        manifest,source,mask,spec,parent,direction=fixtures()
        spec['patches'].append(list(spec['patches'][0]))
        with self.assertRaisesRegex(ValueError,'Duplicate'):
            revision.compile_inhale(spec,manifest,source)

    def test_disallow_interior_pixel_add_and_color_changing_new_contour(self):
        manifest,source,mask,spec,parent,direction=fixtures()
        spec['patches'][0]=['upper_arm_l','add_opaque',83,40,1,'ink']
        with self.assertRaisesRegex(ValueError,'contour'):
            revision.compile_inhale(spec,manifest,source)
        manifest,source,mask,spec,parent,direction=fixtures()
        spec['patches'][0][5]='green_lit'
        with self.assertRaisesRegex(ValueError,'ink'):
            revision.compile_inhale(spec,manifest,source)

    def test_fail_closed_when_validator_budgets_or_approval_changed(self):
        manifest,source,mask,spec,parent,direction=fixtures()
        spec['limits']['allowed_regions']['upper_arm_l']['y']=[0,160]
        with self.assertRaisesRegex(ValueError,'validator budgets'):
            revision.compile_inhale(spec,manifest,source)
        manifest,source,mask,spec,parent,direction=fixtures()
        spec['owner_approved_idle_loop']=True
        with self.assertRaisesRegex(ValueError,'contract drift'):
            revision.compile_inhale(spec,manifest,source)

    def test_produces_exact_two_view_sheets_and_no_timed_loop(self):
        with tempfile.TemporaryDirectory(prefix='c03-v21-') as location:
            result=revision.render(Path(location))
            self.assertEqual(result['status'],'INHALE_V21_DRAFT_AWAITING_OWNER_REVIEW')
            self.assertEqual(result['full_idle_loop'],'NOT_CREATED')
            self.assertTrue(result['rest_v2_unchanged'])
            self.assertTrue(result['blink_v2_unchanged'])
            self.assertEqual(result['unity'],'NOT_RUN')
            for filename in ('C03-idle-v2.1-rest-inhale-side-by-side-1x.png',
                             'C03-idle-v2.1-rest-inhale-side-by-side-4x.png',
                             'C03-idle-v2.1-shoulder-chest-10x.png'):
                self.assertEqual((Path(location)/filename).read_bytes()[:8],b'\x89PNG\r\n\x1a\n')


if __name__=='__main__':
    unittest.main()
