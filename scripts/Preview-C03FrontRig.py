#!/usr/bin/env python3
"""Front-only C03 rig articulation proof: native joint rotations, no runtime asset.

Reads the existing 21 native painted layers; cannot generate/alter character
art or edit Unity .meta. Renders 3 rigid-cutout nearest-neighbor pose samples
to show potential joint gaps before Unity pivot authoring.
"""
import argparse
import hashlib
import importlib.util
import json
import math
import sys
from pathlib import Path

HERE = Path(__file__).resolve().parent
ROOT = HERE.parent
BASE = ROOT/'docs/evidence/production-art/source/c03-authoring'
NATIVE = BASE/'C03-native-front-source.json'
JOINTS = BASE/'C03-front-joint-plan-DRAFT.json'
CONTRACT = BASE/'C03-owner-front-rig-contract.json'
W,H = 128,160


def script_module(filename):
    sys.path.insert(0,str(HERE))
    spec = importlib.util.spec_from_file_location(filename, HERE/(filename+'.py'))
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def read_plan(plan, contract, source):
    if plan.get('schema') != 'startup-life.c03.front-joint-plan.v1' or plan.get('status') != 'DRAFT_PREVIEW_ONLY_PENDING_UNITY_PIVOT_AND_MOTION_QA':
        raise ValueError('Unknown or falsely approved pivot plan')
    if plan.get('canvas_px') != [W,H] or plan.get('pixels_per_unit') != 32 or plan.get('view') != 'front':
        raise ValueError('Native rig grid drift')
    if plan.get('native_source_packet_sha256') != source.get('packet_sha256'):
        raise ValueError('Native sprite/provisional pivot source hash mismatch')
    if (not source.get('owner_native_direction_approved') or
        source.get('owner_native_reconstruction_approved') is not False or
        source.get('ip_cleared') is not False or
        source.get('unity_animation_verified') is not False or
        contract.get('production_registry_approval') is not False):
        raise ValueError('Art direction/production release states are conflated')
    order = contract['rig_parts_back_to_front']
    if (len(order) != 21 or set(order) != set(source['part_order']) or
        len(set(order)) != 21 or
        order.index('hand_r') >= order.index('accessory_front') or
        contract['occlusion']['covered_part_ids_to_hide_entirely']):
        raise ValueError('Bag-over-hand/sort order mismatch')
    joints = plan['joints']
    lookup = {}
    assigned = {}
    for item in joints:
        name = item['id']; parent = item['parent']; pivot = item['pivot']
        if name in lookup or (parent is not None and parent not in lookup):
            raise ValueError('Duplicate joints or cyclic/out-of-order joint ancestry')
        if (len(pivot)!=2 or any(type(n) is not int for n in pivot) or
            not (0<=pivot[0]<W and 0<=pivot[1]<H)):
            raise ValueError('Non-pixel-aligned or invalid joint pivot')
        if parent is None and name != 'root':
            raise ValueError('Unexpected extra independent joint root')
        if parent is not None and name == 'root':
            raise ValueError('Root cannot have a parent')
        lookup[name] = item
        for part in item['parts']:
            if part in assigned: raise ValueError('A semantic layer belongs to multiple joints')
            assigned[part] = name
    if len(joints)!=16 or set(assigned)!=set(order) or set(lookup)-{'root'}==set():
        raise ValueError('Expected 16 joints and exactly 21 attached native layers')
    poses=plan['poses']
    if len(poses)!=3 or poses[0].get('id')!='bind_front' or poses[0].get('degrees')!={}:
        raise ValueError('Invalid three-pose proof suite')
    if len({p['id'] for p in poses})!=len(poses):
        raise ValueError('Duplicate pose')
    for pose in poses:
        for k,deg in pose['degrees'].items():
            if k not in lookup or k=='root' or type(deg) not in (int,float) or not math.isfinite(deg) or abs(deg)>8:
                raise ValueError('Pose exceeds limited debug-only joint rotation budget')
    return lookup,assigned


def world_joints(joints, degrees):
    world={}
    for name,joint in joints.items():
        px,py=joint['pivot']; parent=joint['parent']
        if parent is None:
            world[name]=(float(px),float(py),math.radians(degrees.get(name,0)))
            continue
        parent_pivot=joints[parent]['pivot']
        gx,gy,pa=world[parent]
        dx,dy=px-parent_pivot[0],py-parent_pivot[1]
        world[name]=(gx+math.cos(pa)*dx-math.sin(pa)*dy,
                     gy+math.sin(pa)*dx+math.cos(pa)*dy,
                     pa+math.radians(degrees.get(name,0)))
    return world


def warp_native(layer, bind_pivot, world_pivot):
    """Inverse-sample one native RGBA sprite: exact nearest integer pixels."""
    wx,wy,angle=world_pivot; bx,by=bind_pivot
    c,s=math.cos(angle),math.sin(angle)
    output=bytearray(W*H*4)
    for y in range(H):
        dy=y-wy
        for x in range(W):
            dx=x-wx
            sx=round(bx+c*dx+s*dy)
            sy=round(by-s*dx+c*dy)
            if 0<=sx<W and 0<=sy<H:
                src=(sy*W+sx)*4
                if layer[src+3]:
                    dest=(y*W+x)*4
                    output[dest:dest+4]=layer[src:src+4]
    return bytes(output)


def posed_layers(layers, mask, joints, assigned, degrees):
    world=world_joints(joints,degrees)
    transformed={}
    for part,raw in layers.items():
        node=assigned[part]
        transformed[part]=warp_native(raw,joints[node]['pivot'],world[node])
    bag_bone=assigned['accessory_front']
    transformed_mask=warp_native(mask,joints[bag_bone]['pivot'],world[bag_bone])
    return transformed,transformed_mask


def render_preview(output_dir, native=NATIVE, joint_plan=JOINTS, contract_path=CONTRACT):
    composer=script_module('Render-C03OwnerNativeFront')
    checker=script_module('Validate-C03OwnerRigHandoff')
    source=composer.expand_source(native)
    plan=json.loads(Path(joint_plan).read_text(encoding='utf8'))
    contract=json.loads(Path(contract_path).read_text(encoding='utf8'))
    joints,assigned=read_plan(plan,contract,source)
    parts,mask=composer.paint(source)
    base=composer.composite(parts)
    output_dir=Path(output_dir)
    output_dir.mkdir(parents=True,exist_ok=True)
    frames=[]
    reports=[]
    for pose in plan['poses']:
        warped,warped_mask=posed_layers(parts,mask,joints,assigned,pose['degrees'])
        merged=composer.composite(warped)
        if pose['id']=='bind_front' and merged!=base:
            raise ValueError('Bind pose must be pixel-exact original native art')
        if pose['id']!='bind_front' and merged==base:
            raise ValueError('Joint movement failed to move actual native parts')
        overlap=checker.inspect_masked_occlusion(
            warped['hand_r'],warped['accessory_front'],warped_mask,
            canvas=(W,H),
            minimum_overlap=contract['occlusion']['minimum_covered_native_px'],
            minimum_exposed_below=contract['occlusion']['minimum_visible_below_native_px'])
        png=composer.png(merged,W,H)
        (output_dir/(pose['id']+'-native.png')).write_bytes(png)
        frames.append(merged)
        reports.append({'pose':pose['id'],
            'pixel_changed_vs_bind':sum(merged[i:i+4]!=base[i:i+4] for i in range(0,len(base),4)),
            **overlap,'png_sha256':hashlib.sha256(png).hexdigest()})
    margin=8; stride=W+2*margin
    width,height=stride*len(frames),H+2*margin
    background=bytes((246,242,232,255))
    board=bytearray(background*(width*height))
    for column,pixels in enumerate(frames):
        for y in range(H):
            for x in range(W):
                src=(y*W+x)*4
                if pixels[src+3]:
                    dest=((y+margin)*width+(column*stride+x+margin))*4
                    board[dest:dest+4]=pixels[src:src+4]
    sheet=composer.png(board,width,height)
    sheet_path=output_dir/'C03-front-16-joint-pose-proof-1x.png'
    sheet_path.write_bytes(sheet)
    summary={'stage':'ARTICULATION_ENGINE_FREE_PREVIEW_NOT_UNITY_VERIFIED',
        'authoritative_native_source_sha256':source['packet_sha256'],
        'joint_count':len(joints),'semantic_layers':len(parts),
        'owner_native_direction':'APPROVED',
        'frames':reports,
        'no_new_variants_or_turnarounds':True,
        'animation_runtime_test':'NOT_RUN',
        'final_golden_approval':False}
    (output_dir/'C03-front-rig-pose-evidence.json').write_text(json.dumps(summary,indent=2)+'\n',encoding='utf8')
    return summary


if __name__=='__main__':
    p=argparse.ArgumentParser()
    p.add_argument('--output-dir',type=Path,required=True)
    args=p.parse_args()
    print(json.dumps(render_preview(args.output_dir),indent=2))
