#!/usr/bin/env python3
"""Draft-only C03 front rig layering checker; no Unity or IP/Golden approval.

Uses the existing strict RGBA PNG decoder from Validate-GoldenCharacterC03R1.
Works only with truly authored native layers, never the opaque concept PNG.
"""
import argparse
import importlib.util
import json
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
DEFAULT_CONTRACT = ROOT / 'Assets/StartupLife/Art/Source/Characters/golden-character-c03/C03-owner-front-rig-contract.json'
DECODER = ROOT / 'scripts/Validate-GoldenCharacterC03R1.py'
CANONICAL_PARTS = frozenset(('hair_back','accessory_back','foot_l','foot_r',
    'lower_leg_l','lower_leg_r','upper_leg_l','upper_leg_r','pelvis','torso',
    'upper_arm_l','upper_arm_r','forearm_l','forearm_r','hand_l','hand_r',
    'outfit_torso','head','face','hair_front','accessory_front'))


def rgba_decoder():
    spec = importlib.util.spec_from_file_location('native_rgba_decoder', DECODER)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module.decode_rgba_png


def coverage(buffer):
    return bytes(1 if buffer[i] else 0 for i in range(3, len(buffer), 4))


def inspect_masked_occlusion(hand, bag, bag_body, canvas=(64, 80), *, minimum_overlap=2, minimum_exposed_below=1):
    """Inspect real masks and composite pixel order, not just layer names.

    bag_body is an independent authored opacity mask, a subset of the
    accessory_front pixels, excluding the diagonal shoulder strap.
    """
    width, height = canvas
    pixel_count = width * height
    if any(len(buf) != pixel_count*4 for buf in (hand, bag, bag_body)):
        raise ValueError('The native RGBA layers must share the contract grid')
    if any(alpha not in (0, 255) for buf in (hand, bag, bag_body)
           for alpha in buf[3::4]):
        raise ValueError('Non-binary alpha in native pixel layer')
    h, a, m = (coverage(buf) for buf in (hand, bag, bag_body))
    if any(m[i] and not a[i] for i in range(pixel_count)):
        raise ValueError('Bag-body mask must be a subset of accessory_front')
    bag_top_at_x = {}
    bag_bottom_at_x = {}
    for x in range(width):
        ys = [y for y in range(height) if m[y*width+x]]
        if ys:
            bag_top_at_x[x] = min(ys)
            bag_bottom_at_x[x] = max(ys)
    overlap = sum(bool(h[i] and m[i]) for i in range(pixel_count))
    below = sum(bool(h[y*width+x] and not m[y*width+x] and
                     x in bag_bottom_at_x and y > bag_bottom_at_x[x])
                for y in range(height) for x in range(width))
    # A partially visible hand is permitted BELOW the bag, never floating
    # above it or showing through a transparent gap inside the bag body.
    misplaced = sum(bool(h[y*width+x] and not m[y*width+x] and
                         x in bag_top_at_x and y <= bag_bottom_at_x[x])
                    for y in range(height) for x in range(width))
    if overlap < minimum_overlap:
        raise ValueError(f'Bag does not sufficiently occlude the hand ({overlap} overlap pixels)')
    if below < minimum_exposed_below:
        raise ValueError(f'Hand is not visibly below the bottom of bag ({below} pixels)')
    if misplaced:
        raise ValueError(f'Hand pixels are wrongly exposed above/inside the bag body ({misplaced})')
    # The new composite must draw the complete accessory_front after the hand.
    # This also explicitly preserves covered hand pixels for later joint motion.
    composite = bytearray(hand)
    for i in range(0, len(composite), 4):
        if bag[i+3]:
            composite[i:i+4] = bag[i:i+4]
    for i in range(pixel_count):
        if m[i] and h[i] and composite[4*i:4*i+4] != bag[4*i:4*i+4]:
            raise ValueError('Hand incorrectly covers the front of the bag')
    return {'overlap_covered_px': overlap, 'hand_visible_below_bag_px': below,
            'bag_front_occludes_hand': True}


def validate_contract(contract):
    if contract.get('schema') != 'startup-life.c03.owner-front-rig-handoff.v1':
        raise ValueError('Unknown C03 rig contract version')
    if contract.get('view') != 'front' or contract.get('canvas_px') != [64,80]:
        raise ValueError('Owner-only front native-grid contract changed')
    names = contract['rig_parts_back_to_front']
    if len(names) != 21 or len(set(names)) != 21 or set(names) != CANONICAL_PARTS:
        raise ValueError('Need precisely the 21 existing semantic rig parts')
    layering=contract['occlusion']
    affected=layering['hand_part_id']
    covering=layering['covering_part_id']
    if (affected not in ('hand_l','hand_r') or covering!='accessory_front' or
        names.index(affected) >= names.index(covering)):
        raise ValueError('The bag-front sorting must be AFTER the affected hand')
    if affected in layering.get('covered_part_ids_to_hide_entirely', []):
        raise ValueError('Never hide the whole hand rig part to fake bag occlusion')
    if contract['owner_visual_decision']!='APPROVED_DIRECTION' or contract['production_registry_approval']:
        raise ValueError('Owner image choice is not Golden registry approval')
    return True


def verify(contract_path, layers_dir):
    contract=json.loads(Path(contract_path).read_text(encoding='utf8'))
    validate_contract(contract)
    decode=rgba_decoder()
    layers={}
    for name in contract['rig_parts_back_to_front']:
        path=Path(layers_dir) / (name+'.png')
        w,h,pixels=decode(path.read_bytes())
        if (w,h)!=(64,80):raise ValueError('Wrong pixel layer dimensions: '+name)
        layers[name]=pixels
    mask_path=Path(layers_dir)/contract['occlusion']['bag_body_mask_filename']
    w,h,mask=decode(mask_path.read_bytes())
    if (w,h)!=(64,80):raise ValueError('Wrong bag mask dimensions')
    report=inspect_masked_occlusion(layers[contract['occlusion']['hand_part_id']],
            layers['accessory_front'],mask,minimum_overlap=contract['occlusion']['minimum_covered_native_px'],
            minimum_exposed_below=contract['occlusion']['minimum_visible_below_native_px'])
    return report


if __name__=='__main__':
    cli=argparse.ArgumentParser()
    cli.add_argument('--contract',type=Path,default=DEFAULT_CONTRACT)
    cli.add_argument('--layers-dir',type=Path,required=True,help='21 native authored RGBA PNGs and bag body mask')
    args=cli.parse_args()
    try:
        print(json.dumps(verify(args.contract,args.layers_dir),indent=2))
        print('PASS draft layered occlusion only; NOT a visual/IP/Unity/device Golden approval')
    except (ValueError,FileNotFoundError,KeyError) as err:
        print('FAIL C03 rig handoff:',err,file=sys.stderr)
        sys.exit(1)
