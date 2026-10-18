"""Regression: C03 authoring data are losslessly archived, not Unity-imported assets.

Static Unity foundation must keep checking every *actual* tracked Unity asset
for a .meta. Do not work around it by changing validator or inventing Unity GUIDs.
"""
import hashlib
import json
from pathlib import Path
import unittest

ROOT = Path(__file__).resolve().parents[2]
ARCHIVE = ROOT/'docs/evidence/production-art/source/c03-authoring'
UNITY = ROOT/'Assets/StartupLife/Art/Source/Characters/golden-character-c03'
ORIGINAL_GIT_BLOBS = {
    'C03-IdlePose-A.json':'fd6497582825c64c8054b2ab4b2fec6f49c41519',
    'C03-IdlePose-B.json':'4be97348febaaddcc36e618f32368acb09fe0408',
    'C03-front-joint-plan-DRAFT.json':'d955bce7c892b0038a424466a15b063e2e7eee7a',
    'C03-idle-v2-art-direction.json':'a022b2cab53d5aed65eb2bc1e1548b6b5e21eb65',
    'C03-idle-v2-keyposes-DRAFT.json':'77db7bec3bcdc99aeefd4d1cc923c20c9ae07a16',
    'C03-idle-v2.1-inhale-contour-DRAFT.json':'f76fb73d71e3f0a9309f0a59ca719ab3c6ee4024',
    'C03-native-front-pixel-runs.lzma.b64':'49faa526c8c6fcebd47c527ec3961af6249b612e',
    'C03-native-front-source.json':'be6339b7a83ad5f2214feff826cbe3a762e35f0c',
    'C03-owner-front-rig-contract.json':'360805fd46738208d037e135ca5206845143f627',
    'r5-1-personality-front-review.json':'5e8623de82e580aafc68e4ff7bc4ce5257013289',
    'r5-personality-front-review.json':'9dd46b419d7d74d504d861ff20f3c23055a1f824',
}


class C03AuthoringSourceMigrationTests(unittest.TestCase):
    def test_all_moved_byte_identical_to_pre_migration_git_blobs(self):
        for name,original in ORIGINAL_GIT_BLOBS.items():
            with self.subTest(file=name):
                path=ARCHIVE/name
                self.assertTrue(path.is_file())
                body=path.read_bytes()
                object_bytes=b'blob '+str(len(body)).encode('ascii')+b'\0'+body
                self.assertEqual(hashlib.sha1(object_bytes).hexdigest(),original)
                self.assertFalse((UNITY/name).exists(),'Non-Unity file needs no import into Assets')

    def test_native_packet_and_companion_manifest_remain_together(self):
        desc=json.loads((ARCHIVE/'C03-native-front-source.json').read_text(encoding='utf8'))
        packet=ARCHIVE/desc['compressed_runs_file']
        self.assertTrue(packet.is_file())
        self.assertEqual(packet.name,'C03-native-front-pixel-runs.lzma.b64')
        self.assertEqual(desc['packet_sha256'],
                         '1b04022f5f828ba95374cc3e0e875df9597ea89454d3ebdd5730aa46a3254c1f')

    def test_still_imported_historical_drafts_have_real_metadata_pairs(self):
        # R1-R4/native draft are already Unity-tracked TextAssets; don't
        # relocate them and don't generate fake GUID or importer metadata.
        for name in ('native-draft.json','r1-front-draft.json','r2-front-draft.json',
                     'r3-front-draft.json','r4-front-draft.json'):
            with self.subTest(file=name):
                self.assertTrue((UNITY/name).exists())
                self.assertTrue((UNITY/(name+'.meta')).exists())

    def test_active_v3_offline_source_stays_outside_unity_assets(self):
        v3=ROOT/'docs/evidence/production-art/source/C03-Idle-V3-stable-blink-DRAFT.json'
        data=json.loads(v3.read_text(encoding='utf8'))
        self.assertEqual(data['owner_motion_review_verdict'],'APPROVE_MOTION_DIRECTION')
        self.assertEqual(data['gate']['unity'],'NOT_RUN')
        self.assertFalse(data['gate']['approved_registry'])


if __name__=='__main__':
    unittest.main()
