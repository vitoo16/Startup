#!/usr/bin/env python3
"""C03 Idle V2 first three native keyposes — draft, not a timed Unity animation.

Reuses authored 128x160 RGBA semantic rig pixels from Render-C03OwnerNativeFront.
Applies only explicitly authored row-pixel edits. No cosine, layer rotation,
whole-character transform, HD downsample, auto-animation or prefab creation.
"""
import argparse
import hashlib
import importlib.util
import json
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SCRIPTS = ROOT / 'scripts'
ART = ROOT / 'Assets/StartupLife/Art/Source/Characters/golden-character-c03'
SOURCE = ART / 'C03-native-front-source.json'
KEYPOSES = ART / 'C03-idle-v2-keyposes-DRAFT.json'
RULES = ART / 'C03-idle-v2-art-direction.json'
W, H = 128, 160


def module(name):
    sys.path.insert(0,str(SCRIPTS))
    spec=importlib.util.spec_from_file_location(name,SCRIPTS/(name+'.py'))
    loaded=importlib.util.module_from_spec(spec)
    spec.loader.exec_module(loaded)
    return loaded


def compile_poses(manifest, specification, rules, base_layers):
    if (specification.get('schema')!='startup-life.c03.idle-v2-native-keyposes.v1'
        or specification.get('status')!='KEYPOSES_DRAFT_AWAITING_OWNER_VISUAL_REVIEW'
        or specification.get('view')!='front'
        or specification.get('canvas_px')!=[W,H]
        or specification.get('owner_approved_idle_animation') is not False
        or specification.get('base_native_packet_sha256')!=manifest.get('packet_sha256')
        or specification.get('base_native_packet_sha256')!=rules.get('frozen_visual_packet_sha256')
        or rules.get('no_automatic_runtime_promotion') is not True):
        raise ValueError('C03 V2 keypose authoring/approval contract drift')
    if [k.get('id') for k in specification['keyposes']]!=['rest','inhale','blink']:
        raise ValueError('Review exactly three native keyposes before timing')
    parts=manifest['part_order']
    if len(parts)!=21 or set(parts)!=set(base_layers):
        raise ValueError('Missing C03 native semantic layer')
    palette={name:bytes.fromhex(hexcode[1:])+b'\xff'
             for name,hexcode in manifest['palette'].items()}
    compiled={}
    for pose in specification['keyposes']:
        name=pose['id']
        layers={part:bytearray(raw) for part,raw in base_layers.items()}
        for patch in pose['patches']:
            if len(patch)!=6:
                raise ValueError('Malformed pixel patch')
            part,operation,y,x,length,color=patch
            if (part not in layers or any(type(i) is not int for i in (x,y,length))
                or not (0<=x<W and 0<=y<H and 0<length<=W-x)):
                raise ValueError('Pixel patch outside native canvas')
            if (name=='inhale' and (part not in ('upper_arm_l','upper_arm_r','outfit_torso')
                                    or not 76<=y<115)
                or name=='blink' and (part!='face' or not 45<=y<=51)
                or name=='rest'):
                raise ValueError('Pose-specific layer or region not allowed')
            for xx in range(x,x+length):
                at=(y*W+xx)*4
                if operation=='paint_opaque':
                    if layers[part][at+3]!=255 or color not in palette:
                        raise ValueError('Only repaint original opaque native pixels')
                    layers[part][at:at+4]=palette[color]
                elif operation=='erase_opaque':
                    if part!='face' or color!='':
                        raise ValueError('Only face eyelids can erase eye pixels')
                    layers[part][at:at+4]=bytes(4)
                elif operation=='paint_on_skin':
                    if (part!='face' or color not in ('brow','ear_shadow')
                        or base_layers['head'][at+3]!=255):
                        raise ValueError('Eyelid must sit on existing face skin')
                    layers[part][at:at+4]=palette[color]
                else:
                    raise ValueError('Unknown pixel patch operation')
        compiled[name]={part:bytes(raw) for part,raw in layers.items()}
    return compiled


def review_invariants(painter,compiled,base,bag_mask,checker):
    original=painter.composite(base)
    result={}
    for name,layers in compiled.items():
        image=painter.composite(layers)
        affected=[(x,y) for y in range(H) for x in range(W)
                  if image[(y*W+x)*4:(y*W+x)*4+4]!=original[(y*W+x)*4:(y*W+x)*4+4]]
        if name=='rest' and (affected or any(layers[p]!=base[p] for p in base)):
            raise ValueError('Rest must be byte-exact native bind pose')
        if name=='inhale' and (not 8<=len(affected)<=96 or
                              any(not 76<=y<115 for x,y in affected)):
            raise ValueError('Inhale is not restricted to a small visible torso change')
        if name=='blink' and (not 8<=len(affected)<=80 or
                             any(not 45<=y<=51 for x,y in affected)):
            raise ValueError('Blink must modify only the little eye clusters')
        frozen=('head','hair_front','hair_back','foot_l','foot_r',
                'upper_leg_l','upper_leg_r','lower_leg_l','lower_leg_r',
                'hand_l','hand_r','accessory_back','accessory_front')
        if any(layers[p]!=base[p] for p in frozen):
            raise ValueError('Locked silhouette, shoes, hand or bag changed')
        if any(image[i+3] not in (0,255) for i in range(0,len(image),4)):
            raise ValueError('Nonbinary alpha introduced by pixel edits')
        if bytes(image[i] for i in range(3,len(image),4)) != bytes(original[i] for i in range(3,len(original),4)):
            raise ValueError('Keypose changed original silhouette alpha')
        overlap=checker.inspect_masked_occlusion(
            layers['hand_r'],layers['accessory_front'],bag_mask,
            canvas=(W,H),minimum_overlap=16,minimum_exposed_below=8)
        result[name]={'changed_visible_pixels':len(affected),
                      'bbox_xyxy_exclusive':None if not affected else [
                          min(x for x,y in affected),min(y for x,y in affected),
                          max(x for x,y in affected)+1,max(y for x,y in affected)+1],
                      **overlap}
    return result


def nearest_4x(rgba,w,h):
    pixels=bytearray(w*4*h*4*4)
    for y in range(h):
        for yy in range(4):
            for x in range(w):
                color=rgba[(y*w+x)*4:(y*w+x)*4+4]
                offset=((y*4+yy)*w*4+x*4)*4
                pixels[offset:offset+16]=color*4
    return pixels


def render(output,source=SOURCE,keyposes=KEYPOSES,rules=RULES):
    painter=module('Render-C03OwnerNativeFront')
    checker=module('Validate-C03OwnerRigHandoff')
    manifest=painter.expand_source(source)
    spec=json.loads(Path(keyposes).read_text(encoding='utf8'))
    art_rules=json.loads(Path(rules).read_text(encoding='utf8'))
    original,bag_mask=painter.paint(manifest)
    poses=compile_poses(manifest,spec,art_rules,original)
    stats=review_invariants(painter,poses,original,bag_mask,checker)
    folder=Path(output)
    folder.mkdir(parents=True,exist_ok=True)
    board_w,board_h=432,176
    sheet=bytearray(bytes((246,242,232,255))*(board_w*board_h))
    hashes={}
    for number,(name,layers) in enumerate(poses.items()):
        rgba=painter.composite(layers)
        png=painter.png(rgba,W,H)
        filename=f'C03-idle-v2-{name}-native-1x-DRAFT.png'
        (folder/filename).write_bytes(png)
        hashes[filename]=hashlib.sha256(png).hexdigest()
        for y in range(H):
            for x in range(W):
                offset=(y*W+x)*4
                if rgba[offset+3]:
                    dst=((y+8)*board_w+(number*144+x+8))*4
                    sheet[dst:dst+4]=rgba[offset:offset+4]
    board=painter.png(nearest_4x(sheet,board_w,board_h),board_w*4,board_h*4)
    filename='C03-idle-v2-three-keyposes-4x-DRAFT.png'
    (folder/filename).write_bytes(board)
    hashes[filename]=hashlib.sha256(board).hexdigest()
    report={'status':spec['status'],'native_source_sha256':manifest['packet_sha256'],
            'native_canvas':[W,H],'keyposes':stats,'outputs_sha256':hashes,
            'owner_motion_keypose_review':'PENDING','full_idle_loop':'NOT_CREATED',
            'unity':'NOT_RUN','independent_ip':'NOT_RUN','approved_registry':'UNCHANGED'}
    (folder/'C03-idle-v2-keypose-evidence.json').write_text(
        json.dumps(report,indent=2,ensure_ascii=False)+'\n',encoding='utf8')
    return report


if __name__=='__main__':
    args=argparse.ArgumentParser()
    args.add_argument('--output-dir',type=Path,required=True)
    cli=args.parse_args()
    print(json.dumps(render(cli.output_dir),indent=2))
