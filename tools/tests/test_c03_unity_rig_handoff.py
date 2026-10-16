"""Engine-free C03 Unity transform manifest QA; no Unity Editor invoked."""
import importlib.util
import json
from pathlib import Path
import tempfile
import unittest

ROOT=Path(__file__).resolve().parents[2]
SOURCE=ROOT/'Assets/StartupLife/Art/Source/Characters/golden-character-c03'
SPEC=importlib.util.spec_from_file_location('unity_manifest',ROOT/'scripts/Export-C03UnityRigHandoff.py')
module=importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(module)


def inputs():
    return (
        json.loads((SOURCE/'C03-front-joint-plan-DRAFT.json').read_text()),
        json.loads((SOURCE/'C03-owner-front-rig-contract.json').read_text()),
        json.loads((SOURCE/'C03-native-front-source.json').read_text())
    )


class C03UnityHandoffTests(unittest.TestCase):
    def test_full_hierarchy_and_bag_sorting_order(self):
        plan,contract,source=inputs()
        doc=module.generate(plan,contract,source)
        self.assertEqual(doc['native_grid'],[128,160])
        self.assertEqual(len(doc['joints']),16)
        self.assertEqual(len(doc['sprite_renderers']),21)
        self.assertFalse(doc['owner_final_golden_approval'])
        self.assertEqual(doc['unity_import_and_animation'],'NOT_RUN')
        self.assertEqual(doc['owner_idle_animation_verdict'],'REJECTED')
        self.assertFalse(doc['idle_motion_clip_ready'])
        self.assertEqual(doc['rejected_legacy_idle'],'DO_NOT_BIND_OR_INTEGRATE')
        records={item['part_id']:item for item in doc['sprite_renderers']}
        self.assertGreater(records['accessory_front']['sort_order'],
                           records['hand_r']['sort_order'])
        self.assertEqual(records['hand_r']['bone'],'wrist_r')
        self.assertEqual(records['accessory_front']['bone'],'spine')
        self.assertEqual(doc['sprite_import_requirements']['filter_mode'],'Point')
        self.assertFalse(doc['sprite_import_requirements']['mipmaps'])

    def test_root_to_joint_and_sprite_center_pivots_are_exact(self):
        plan,contract,source=inputs()
        doc=module.generate(plan,contract,source)
        bones={x['id']:x for x in doc['joints']}
        self.assertEqual(bones['root']['local_xy_unity_units'],[0.0,0.0])
        self.assertEqual(bones['shoulder_l']['local_xy_unity_units'],[-.65625,.875])
        self.assertEqual(bones['wrist_r']['local_xy_unity_units'],[-.03125,-.3125])
        face=next(p for p in doc['sprite_renderers'] if p['part_id']=='face')
        self.assertEqual(face['local_xy_unity_units'],[-.09375,-.09375])

    def test_packet_mismatch_and_unapproved_runtime_cannot_export(self):
        plan,contract,source=inputs()
        source['packet_sha256']='bad'
        with self.assertRaisesRegex(ValueError,'source hash mismatch'):
            module.generate(plan,contract,source)
        plan,contract,source=inputs()
        contract['production_registry_approval']=True
        with self.assertRaisesRegex(ValueError,'conflated'):
            module.generate(plan,contract,source)

    def test_can_export_machine_readable_draft_json_without_unity(self):
        with tempfile.TemporaryDirectory(prefix='c03-unity-') as folder:
            target=Path(folder)/'C03-unity-rig-handoff.json'
            report=module.export(target)
            result=json.loads(target.read_text())
            self.assertEqual(report['joints'],16)
            self.assertEqual(report['semantic_renderers'],21)
            self.assertEqual(result['status'],'DRAFT_EDITOR_HANDOFF_NOT_PRODUCTION_APPROVED')
            self.assertTrue(result['sprite_import_requirements']['full_canvas_untrimmed_required'])


if __name__=='__main__':
    unittest.main()
