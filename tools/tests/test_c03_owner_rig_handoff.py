"""Native overlap negative/positive tests without Unity; fixture pixels are synthetic."""
import importlib.util
import json
from pathlib import Path
import unittest

ROOT = Path(__file__).resolve().parents[1]
SCRIPT=ROOT/'scripts/Validate-C03OwnerRigHandoff.py'
CONTRACT=ROOT/'docs/evidence/production-art/source/c03-authoring/C03-owner-front-rig-contract.json'
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
