"""Native overlap negative/positive tests without Unity; fixture pixels are synthetic."""
import hashlib
import importlib.util
import json
import tempfile
from pathlib import Path
import unittest

ROOT = Path(__file__).resolve().parents[2]
SCRIPT=ROOT/'scripts/Validate-C03OwnerRigHandoff.py'
CONTRACT=ROOT/'docs/evidence/production-art/C03-ACTIVE-OWNER-FRONT-RIG-CONTRACT.json'
modspec=importlib.util.spec_from_file_location('occlusion',SCRIPT)
mod=importlib.util.module_from_spec(modspec)
modspec.loader.exec_module(mod)


def layers():
    width=64; height=80
    hand=bytearray(width*height*4); bag=bytearray(width*height*4); mask=bytearray(width*height*4)
    def draw(data,x,y,color):data[(y*width+x)*4:(y*width+x)*4+4]=bytes((*color,255))
    for y in range(35,43):
        for x in range(40,44):draw(hand,x,y,(234,175,139))
    for y in range(34,40):
        for x in range(39,45):
            draw(bag,x,y,(146,110,78));draw(mask,x,y,(255,255,255))
    return hand,bag,mask

class C03OwnerRigTests(unittest.TestCase):
    def test_preserves_all_parts_with_strict_sorting_and_not_release_approved(self):
        contract=json.loads(CONTRACT.read_text())
        self.assertTrue(mod.validate_contract(contract))
        self.assertFalse(contract['production_registry_approval'])
        self.assertEqual(contract['occlusion']['covered_part_ids_to_hide_entirely'], [])

    def test_native_rig_contract_must_match_current_owner_reference(self):
        contract = json.loads(CONTRACT.read_text(encoding='utf-8'))
        reference = json.loads(mod.REFERENCE_RECORD.read_text(encoding='utf-8'))['latest_reference']
        self.assertEqual(reference['sha256'], contract['selected_concept']['image_sha256'])
        self.assertEqual(reference['expected_repo_draft_path'],
                         contract['selected_concept']['repo_target'])
        self.assertTrue(mod.validate_contract(contract))

        # Historical Oct 16 SHA, uncommitted metadata and moved source paths
        # must not validate a handoff for the Oct 21 canonical reference.
        stale = json.loads(CONTRACT.read_text(encoding='utf-8'))
        stale['selected_concept']['image_sha256'] = (
            'd2e7e9a79672e23408c0a48626ef3968af84f5564f44e14eac41e4add38cb25e')
        with self.assertRaisesRegex(ValueError, 'latest owner-selected reference'):
            mod.validate_contract(stale)

        missing = json.loads(CONTRACT.read_text(encoding='utf-8'))
        missing['selected_concept']['image_committed'] = False
        with self.assertRaisesRegex(ValueError, 'latest owner-selected reference'):
            mod.validate_contract(missing)

        wrong_path = json.loads(CONTRACT.read_text(encoding='utf-8'))
        wrong_path['selected_concept']['repo_target'] = 'Assets/other.png'
        with self.assertRaisesRegex(ValueError, 'latest owner-selected reference'):
            mod.validate_contract(wrong_path)

    def test_active_contract_rejects_unreconstructed_legacy_native_art(self):
        contract = json.loads(CONTRACT.read_text(encoding='utf-8'))
        self.assertEqual('NOT_AUTHORED',
                         contract['selected_concept']['native_rig_from_selected_image'])
        with self.assertRaisesRegex(ValueError, 'not authored yet'):
            mod.verify_native_packet(contract, ROOT / 'not-a-valid-layer-folder')

    def test_current_native_layer_packet_hashes_and_source_are_required(self):
        contract = json.loads(CONTRACT.read_text(encoding='utf-8'))
        contract['selected_concept']['native_rig_from_selected_image'] = (
            'ARTIST_AUTHORED_LAYERS_SUBMITTED')
        current = json.loads(mod.REFERENCE_RECORD.read_text(encoding='utf-8'))['latest_reference']
        with tempfile.TemporaryDirectory(prefix='c03-native-gate-') as tmp:
            folder = Path(tmp)
            # Integrity-only fixture: real PNG decoding/occlusion is checked by
            # the separate production verify() path, not these placeholder bytes.
            names = {part + '.png' for part in contract['rig_parts_back_to_front']}
            names.add(contract['occlusion']['bag_body_mask_filename'])
            hashes = {}
            for name in sorted(names):
                path = folder / name
                path.parent.mkdir(parents=True, exist_ok=True)
                body = b'fixture-' + name.encode('ascii')
                path.write_bytes(body)
                hashes[name] = hashlib.sha256(body).hexdigest()
            canonical = ''.join(
                filename + chr(0) + hashes[filename] + chr(10)
                for filename in sorted(hashes))
            packet_digest = hashlib.sha256(canonical.encode('utf-8')).hexdigest()
            manifest = {
                'schema': mod.NATIVE_SCHEMA,
                'status': mod.NATIVE_STATUS,
                'selected_reference_sha256': current['sha256'],
                'selected_reference_path': current['expected_repo_draft_path'],
                'native_canvas_px': contract['canvas_px'],
                'pixels_per_unit': contract['pixel_per_unit'],
                'file_sha256': hashes,
                'source_packet_sha256': packet_digest,
            }
            manifest_path = folder / mod.NATIVE_MANIFEST
            manifest_path.write_text(json.dumps(manifest), encoding='utf-8')
            self.assertTrue(mod.verify_native_packet(contract, folder))

            (folder / 'hand_r.png').write_bytes(b'tampered')
            with self.assertRaisesRegex(ValueError, 'SHA-256 mismatch'):
                mod.verify_native_packet(contract, folder)
            (folder / 'hand_r.png').write_bytes(b'fixture-hand_r.png')

            manifest['selected_reference_sha256'] = (
                'd2e7e9a79672e23408c0a48626ef3968af84f5564f44e14eac41e4add38cb25e')
            manifest_path.write_text(json.dumps(manifest), encoding='utf-8')
            with self.assertRaisesRegex(ValueError, 'missing, stale or incomplete'):
                mod.verify_native_packet(contract, folder)

            manifest['selected_reference_sha256'] = current['sha256']
            manifest['source_packet_sha256'] = contract['native_front_source_packet_sha256']
            manifest_path.write_text(json.dumps(manifest), encoding='utf-8')
            with self.assertRaisesRegex(ValueError, 'historical native packet'):
                mod.verify_native_packet(contract, folder)

            manifest['source_packet_sha256'] = packet_digest
            manifest_path.write_text(json.dumps(manifest), encoding='utf-8')
            self.assertTrue(mod.verify_native_packet(contract, folder))
            manifest['source_packet_sha256'] = '0' * 64
            manifest_path.write_text(json.dumps(manifest), encoding='utf-8')
            with self.assertRaisesRegex(ValueError, 'aggregate SHA-256 mismatch'):
                mod.verify_native_packet(contract, folder)
            manifest['source_packet_sha256'] = packet_digest
            del manifest['file_sha256']['face.png']
            manifest_path.write_text(json.dumps(manifest), encoding='utf-8')
            with self.assertRaisesRegex(ValueError, 'missing, stale or incomplete'):
                mod.verify_native_packet(contract, folder)

    def test_all_twenty_one_parts_need_real_binary_alpha_pixels(self):
        contract = json.loads(CONTRACT.read_text(encoding='utf-8'))
        width, height = contract['canvas_px']
        count = width * height * 4
        parts = {}
        for part in contract['rig_parts_back_to_front']:
            pixels = bytearray(count)
            pixels[3] = 255  # synthetic one-pixel authored fixture
            parts[part] = pixels
        self.assertTrue(mod.inspect_required_native_layers(contract, parts))
        parts['torso'][:] = bytes(count)
        with self.assertRaisesRegex(ValueError, 'is empty: torso'):
            mod.inspect_required_native_layers(contract, parts)
        parts['torso'][3] = 255
        parts['head'][3] = 128
        with self.assertRaisesRegex(ValueError, 'non-binary native alpha: head'):
            mod.inspect_required_native_layers(contract, parts)
        parts['head'][3] = 255
        del parts['foot_r']
        with self.assertRaisesRegex(ValueError, 'missing an authored semantic part'):
            mod.inspect_required_native_layers(contract, parts)

    def test_partial_hand_covered_and_skin_below(self):
        result=mod.inspect_masked_occlusion(*layers())
        self.assertEqual(result['overlap_covered_px'],20)
        self.assertEqual(result['hand_visible_below_bag_px'],12)

    def test_rejects_hand_poking_above_bag_front(self):
        hand,bag,mask=layers()
        index=(33*64+40)*4
        hand[index:index+4]=bytes((234,175,139,255))
        with self.assertRaisesRegex(ValueError,'wrongly exposed above/inside'):
            mod.inspect_masked_occlusion(hand,bag,mask)

    def test_rejects_missing_hand_bag_intersection(self):
        hand,bag,mask=layers()
        hand[:]=bytes(len(hand))
        with self.assertRaisesRegex(ValueError,'does not sufficiently occlude'):
            mod.inspect_masked_occlusion(hand,bag,mask)

    def test_rejects_fully_hidden_hand(self):
        hand,bag,mask=layers()
        for y in range(40,43):
            for x in range(40,44):hand[(y*64+x)*4+3]=0
        with self.assertRaisesRegex(ValueError,'not visibly below'):
            mod.inspect_masked_occlusion(hand,bag,mask)

    def test_rejects_wrong_sort_order_or_hide_whole_hand(self):
        d=json.loads(CONTRACT.read_text())
        stack=d['rig_parts_back_to_front'];stack.remove('hand_r');stack.append('hand_r')
        with self.assertRaisesRegex(ValueError,'AFTER'):
            mod.validate_contract(d)
        d=json.loads(CONTRACT.read_text());d['occlusion']['covered_part_ids_to_hide_entirely']=['hand_r']
        with self.assertRaisesRegex(ValueError,'Never hide'):
            mod.validate_contract(d)

    def test_rejects_unknown_semantic_rig_part(self):
        contract=json.loads(CONTRACT.read_text())
        stack=contract['rig_parts_back_to_front']
        stack[0]='unapproved_proxy_layer'
        with self.assertRaisesRegex(ValueError,'precisely the 21'):
            mod.validate_contract(contract)

    def test_rejects_bag_mask_unbacked_by_accessory_pixels(self):
        hand,bag,mask=layers()
        bag[(34*64+40)*4+3]=0
        with self.assertRaisesRegex(ValueError,'subset'):
            mod.inspect_masked_occlusion(hand,bag,mask)

    def test_rejects_nonbinary_alpha(self):
        hand,bag,mask=layers()
        bag[(34*64+40)*4+3]=100
        with self.assertRaisesRegex(ValueError,'Non-binary alpha'):
            mod.inspect_masked_occlusion(hand,bag,mask)

if __name__=='__main__':unittest.main()
