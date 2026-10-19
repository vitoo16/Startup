"""C03 V3 unity staging: native RGBA/closed-eye hash and 21-part isolation checks."""
import importlib.util
import json
from pathlib import Path
import tempfile
import unittest
from shutil import copyfile

ROOT=Path(__file__).resolve().parents[2]
TOOLS=ROOT/'scripts'
SPEC=importlib.util.spec_from_file_location('c03_unity_stage',TOOLS/'Stage-C03IdleV3UnityProof.py')
try:
    from PIL import Image
except ImportError:
    Image=None
if Image:
    stage=importlib.util.module_from_spec(SPEC)
    SPEC.loader.exec_module(stage)


@unittest.skipIf(Image is None,'Pillow required on the art workstation for offline staging')
class C03UnityStagingTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.tmp=tempfile.TemporaryDirectory(prefix='c03-v3-unity-proof-')
        cls.root=Path(cls.tmp.name)
        # Generate actual native sprites from committed source, not fake fixture images.
        def use(name):
            spec=importlib.util.spec_from_file_location(name,TOOLS/(name+'.py'))
            module=importlib.util.module_from_spec(spec)
            spec.loader.exec_module(module)
            return module
        native=use('Render-C03OwnerNativeFront')
        review=use('Render-C03IdleV3')
        rig=use('Export-C03UnityRigHandoff')
        native.export(native.SOURCE,cls.root/'native')
        review.export(cls.root/'v3')
        rig.export(cls.root/'rig.json')
        cls.parts=cls.root/'native/layers'
        cls.frames=cls.root/'v3'
        cls.rig=cls.root/'rig.json'

    @classmethod
    def tearDownClass(cls):
        cls.tmp.cleanup()

    def test_authoritative_21_parts_and_face_only_blink_restore_composite_exactly(self):
        result=stage.prepare(self.parts,self.frames,self.rig,self.root/'proof1')
        self.assertEqual(len(result['files']),24)
        self.assertTrue(result['rest_pixel_identical'])
        self.assertTrue(result['blink_only_face_layer'])
        self.assertFalse(result['owner_golden_asset_approved'])
        self.assertEqual(result['source_packet_sha256'],stage.PACKET)
        self.assertTrue((self.root/'proof1/layers/accessory_front.png').is_file())
        self.assertTrue((self.root/'proof1/face_closed.png').is_file())
        self.assertEqual(result['frame_changed_visible_pixels'],{'open':0,'half':19,'closed':35})

    def test_no_overwrite_of_previous_artist_output(self):
        with self.assertRaises(FileExistsError):
            stage.prepare(self.parts,self.frames,self.rig,self.root/'proof1')

    def test_reject_modified_eye_outside_native_roi(self):
        from PIL import Image as Pillow
        corrupted=self.root/'bad-v3'
        corrupted.mkdir(exist_ok=True)
        for pose in stage.POSES:
            copyfile(self.frames/('C03-Idle-V3-'+pose+'-1x.png'),
                     corrupted/('C03-Idle-V3-'+pose+'-1x.png'))
        path=corrupted/'C03-Idle-V3-half-1x.png'
        with Pillow.open(path) as file:
            img=file.convert('RGBA')
        img.putpixel((12,12),(255,0,0,255))
        img.save(path)
        with self.assertRaisesRegex(ValueError,'outside blink eye ROI'):
            stage.prepare(self.parts,corrupted,self.rig,self.root/'bad-proof')

    def test_reject_tampered_joint_pivot_and_full_canvas_sprite_offset(self):
        manifest=json.loads(self.rig.read_text(encoding='utf8'))
        joint=next(x for x in manifest['joints'] if x['id']=='neck')
        joint['local_xy_unity_units'][0]+=0.03125
        shifted=self.root/'tampered-joint.json'
        shifted.write_text(json.dumps(manifest),encoding='utf8')
        with self.assertRaisesRegex(ValueError,'Joint offset'):
            stage.prepare(self.parts,self.frames,shifted,self.root/'invalid-joint')
        manifest=json.loads(self.rig.read_text(encoding='utf8'))
        face=next(x for x in manifest['sprite_renderers'] if x['part_id']=='face')
        face['local_xy_unity_units'][0]+=0.03125
        shifted=self.root/'tampered-face.json'
        shifted.write_text(json.dumps(manifest),encoding='utf8')
        with self.assertRaisesRegex(ValueError,'Sprite center'):
            stage.prepare(self.parts,self.frames,shifted,self.root/'invalid-face')

    def test_staging_manifest_carries_exact_source_rig_sha256(self):
        import hashlib
        report=json.loads((self.root/'proof1/C03-quarantine-input.json').read_text(encoding='utf8'))
        self.assertEqual(report['rig_manifest_sha256'],
                         hashlib.sha256(self.rig.read_bytes()).hexdigest())
        self.assertEqual(len(report['files']),24)

    def test_wrong_bag_sorting_not_promoted(self):
        contract=json.loads(self.rig.read_text(encoding='utf8'))
        parts=contract['sprite_renderers']
        front=next(x for x in parts if x['part_id']=='accessory_front')
        front['sort_order']=-10
        path=self.root/'invalid-rig.json'
        path.write_text(json.dumps(contract),encoding='utf8')
        with self.assertRaisesRegex(ValueError,'sorting'):
            stage.prepare(self.parts,self.frames,path,self.root/'wrong-bag')


if __name__=='__main__':
    unittest.main()
