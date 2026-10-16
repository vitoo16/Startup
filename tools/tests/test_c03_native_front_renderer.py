"""Native C03 21-layer renderer schema regression. No image/Golden approvals."""
import importlib.util
import json
from pathlib import Path
import tempfile
import unittest

ROOT=Path(__file__).resolve().parents[2]
SCRIPT=ROOT/'scripts/Render-C03OwnerNativeFront.py'
spec=importlib.util.spec_from_file_location('c03_native_renderer',SCRIPT)
module=importlib.util.module_from_spec(spec)
spec.loader.exec_module(module)


def fixture():
    # Synthetic authoring rows; not an alternative design or fake production art.
    all_parts={name:[[8+i,40,1,'ink']] for i,name in enumerate(module.PARTS)}
    all_parts['hand_r'].append([126,90,3,'skin'])
    all_parts['hand_r'].append([127,90,3,'skin'])
    all_parts['accessory_front']=[[126,90,3,'brown']]
    return {
      'schema':'startup-life.c03.native-layer-runs.v1',
      'status':'NATIVE_FRONT_RECONSTRUCTION_DRAFT_NOT_GOLDEN_APPROVED',
      'view':'front','canvas_px':[128,160],'pixels_per_unit':32,
      'part_order':list(module.PARTS),
      'visual_master_sha256':module.EXPECTED_VISUAL_SHA,
      'owner_native_reconstruction_approved':False,
      'ip_cleared':False,'unity_animation_verified':False,
      'palette':{'ink':'#34343b','skin':'#e7ac88','brown':'#ad7850'},
      'layers':all_parts,
      'bag_body_mask':[[126,90,3]]
    }


class NativeFrontRows(unittest.TestCase):
    def test_all_canonical_parts_and_mask(self):
        parts,bag=module.paint(fixture())
        self.assertEqual(len(parts),21)
        self.assertEqual(len(bag),128*160*4)
        pixel=(126*128+90)*4
        self.assertEqual(module.composite(parts)[pixel:pixel+4],bytes((173,120,80,255)))

    def test_no_in_place_color_or_approval_inference(self):
        with tempfile.TemporaryDirectory() as path:
            source=Path(path)/'source.json'
            source.write_text(json.dumps(fixture()))
            report=module.export(source,Path(path)/'out')
            self.assertEqual(report['owner_native_review'],'PENDING')
            self.assertEqual(report['nonempty_layers'],21)
            self.assertEqual((Path(path)/'out/C03-native-front-128x160-DRAFT.png').read_bytes()[:8],b'\x89PNG\r\n\x1a\n')

    def test_repo_native_rows_and_hand_bag_overlap(self):
        # Decodes the ACTUAL committed pixel source, not a synthetic fixture.
        data=module.expand_source(module.SOURCE)
        self.assertEqual(len(data['layers']),21)
        parts,mask=module.paint(data)
        actual=module.composite(parts)
        self.assertEqual(len(actual),128*160*4)
        self.assertEqual(set(actual[3::4]),{0,255})
        dec_spec=importlib.util.spec_from_file_location(
            'c03_occlusion',ROOT/'scripts/Validate-C03OwnerRigHandoff.py')
        checker=importlib.util.module_from_spec(dec_spec)
        dec_spec.loader.exec_module(checker)
        stats=checker.inspect_masked_occlusion(
            parts['hand_r'],parts['accessory_front'],mask,
            canvas=(128,160),minimum_overlap=16,minimum_exposed_below=8)
        self.assertGreaterEqual(stats['overlap_covered_px'],16)
        self.assertGreaterEqual(stats['hand_visible_below_bag_px'],8)
        self.assertTrue(stats['bag_front_occludes_hand'])

    def test_reject_out_of_bounds_runs(self):
        f=fixture();f['layers']['hand_l'].append([160,2,1,'skin'])
        with self.assertRaisesRegex(ValueError,'Invalid native pixel row'):
            module.paint(f)

    def test_reject_duplicate_opaque_pixels(self):
        f=fixture();f['layers']['hand_l'].append(list(f['layers']['hand_l'][0]))
        with self.assertRaisesRegex(ValueError,'Duplicate opaque pixels'):
            module.paint(f)

    def test_reject_unapproved_sources_claiming_golden(self):
        f=fixture();f['owner_native_reconstruction_approved']=True
        with self.assertRaisesRegex(ValueError,'Unreviewed C03 native source'):
            module.paint(f)


if __name__=='__main__':unittest.main()
