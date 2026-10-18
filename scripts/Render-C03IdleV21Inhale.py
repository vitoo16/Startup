#!/usr/bin/env python3
"""Owner C03 Idle V2.1: authored ONE inhale contour. No loop/Unity approval."""
import argparse
import hashlib
import importlib.util
import json
import sys
from pathlib import Path

ROOT=Path(__file__).resolve().parents[1]
ART=ROOT/'docs/evidence/production-art/source/c03-authoring'
W,H=128,160
NATIVE=ART/'C03-native-front-source.json'
PARENT=ART/'C03-idle-v2-keyposes-DRAFT.json'
REVISION=ART/'C03-idle-v2.1-inhale-contour-DRAFT.json'
DIRECTION=ART/'C03-idle-v2-art-direction.json'
REGIONS={
    'upper_arm_l':{'x':[39,51],'y':[77,84]},
    'upper_arm_r':{'x':[85,95],'y':[79,84]},
    'outfit_torso':{'x':[49,86],'y':[77,95]}
}
FROZEN=('head','face','hair_front','hair_back','foot_l','foot_r',
        'lower_leg_l','lower_leg_r','upper_leg_l','upper_leg_r','pelvis',
        'hand_l','hand_r','forearm_l','forearm_r','accessory_front',
        'accessory_back','torso')


def load_tool(name):
    scripts=ROOT/'scripts'
    sys.path.insert(0,str(scripts))
    module=importlib.util.spec_from_file_location(name,scripts/(name+'.py'))
    result=importlib.util.module_from_spec(module)
    module.loader.exec_module(result)
    return result


def next_to(layer,x,y,opaque):
    for yy in range(max(0,y-1),min(H,y+2)):
        for xx in range(max(0,x-1),min(W,x+2)):
            if (xx,yy)!=(x,y) and (layer[(yy*W+xx)*4+3]>0)==opaque:
                return True
    return False


def compile_inhale(spec,manifest,source):
    limits=spec.get('limits',{})
    if (spec.get('schema')!='startup-life.c03.idle-v2.1-contour-study.v1' or
        spec.get('status')!='INHALE_V21_DRAFT_AWAITING_OWNER_REVIEW' or
        spec.get('owner_verdict')!='REVISE_INHALE_KEEP_REST_BLINK' or
        spec.get('owner_approved_inhale') is not False or
        spec.get('owner_approved_idle_loop') is not False or
        spec.get('canvas_px')!=[W,H] or spec.get('view')!='front' or
        spec.get('frozen_native_packet_sha256')!=manifest.get('packet_sha256') or
        spec.get('parent_keyposes')!='C03-idle-v2-keyposes-DRAFT.json' or
        spec.get('replaces_only')!='inhale'):
        raise ValueError('C03 V2.1 native source/approval contract drift')
    # An editable contract cannot silently relax these hard spatial limits.
    if (limits.get('allowed_regions')!=REGIONS or
        limits.get('freeze_parts')!=list(FROZEN) or
        limits.get('maximum_new_alpha_pixels_per_arm')!=6 or
        limits.get('maximum_global_added_alpha_pixels')!=12 or
        limits.get('maximum_global_removed_alpha_pixels')!=6 or
        limits.get('minimum_visible_alpha_changed_pixels')!=4 or
        limits.get('minimum_visible_changed_pixels')!=10 or
        limits.get('maximum_visible_changed_pixels')!=48 or
        limits.get('max_contour_shift_chebyshev_px')!=1):
        raise ValueError('C03 V2.1 shoulder-specific validator budgets changed')
    palette={key:bytes.fromhex(value[1:])+b'\xff'
             for key,value in manifest['palette'].items()}
    layers={key:bytearray(value) for key,value in source.items()}
    touched=set()
    added={'upper_arm_l':0,'upper_arm_r':0}
    removed=0
    for row in spec['patches']:
        if len(row)!=6:raise ValueError('Invalid V2.1 authored pixel patch')
        part,operation,y,x,length,color=row
        if (part not in REGIONS or any(type(n)!=int for n in (x,y,length))
            or length<1 or x<0 or y<0 or x+length>W or y>=H):
            raise ValueError('Patch targets non-authorized C03 native part')
        bounds=REGIONS[part]
        if (not bounds['x'][0]<=x or x+length>bounds['x'][1] or
            not bounds['y'][0]<=y<bounds['y'][1]):
            raise ValueError('Patch outside local shoulder/chest authoring ROI')
        if operation not in ('add_opaque','erase_opaque','repaint'):
            raise ValueError('Invalid native contour edit operation')
        if (operation in ('add_opaque','erase_opaque') and
            part not in added):
            raise ValueError('Alpha edits allowed only at two shoulder contours')
        if (operation=='add_opaque' and color!='ink') or (
            operation=='erase_opaque' and color!='') or (
            operation!='erase_opaque' and color not in palette):
            raise ValueError('Invalid silhouette ink or C03 palette role')
        for xx in range(x,x+length):
            if (part,xx,y) in touched:
                raise ValueError('Duplicate pixel mutation in same native keypose')
            touched.add((part,xx,y))
            offset=(y*W+xx)*4
            existing=source[part][offset+3]
            if operation=='add_opaque':
                if existing or not next_to(source[part],xx,y,True):
                    raise ValueError('Added contour pixel must touch original within 1px')
                layers[part][offset:offset+4]=palette[color]
                added[part]+=1
            elif operation=='erase_opaque':
                if existing!=255 or not next_to(source[part],xx,y,False):
                    raise ValueError('Can erase only original external outline pixel')
                layers[part][offset:offset+4]=bytes(4)
                removed+=1
            else:
                if existing!=255:
                    raise ValueError('Can recolor only an already opaque pixel')
                layers[part][offset:offset+4]=palette[color]
    if (any(v<1 or v>6 for v in added.values()) or
        sum(added.values())>12 or removed>6):
        raise ValueError('Contour alpha-change budget exceeded')
    if any(layers[name]!=source[name] for name in FROZEN):
        raise ValueError('Unrelated head, hand, bag or lower-body layer modified')
    return {name:bytes(layer) for name,layer in layers.items()}, {
        'added_alpha_by_arm':added,'removed_alpha':removed
    }


def validate(painter,checker,base,inhale,mask):
    before=painter.composite(base)
    after=painter.composite(inhale)
    changed=[]
    contour=[]
    for y in range(H):
        for x in range(W):
            index=(y*W+x)*4
            if before[index:index+4]!=after[index:index+4]:
                changed.append((x,y))
                if before[index+3]!=after[index+3]:
                    contour.append((x,y))
    if not 10<=len(changed)<=48 or not 4<=len(contour)<=18:
        raise ValueError('Inhale is either shading-only or outside 1px contour budget')
    if any(y<77 or y>=95 for x,y in changed):
        raise ValueError('Head/hips/feet or unrelated area moved')
    if not any(x<64 for x,y in contour) or not any(x>=64 for x,y in contour):
        raise ValueError('Both shoulders must show actual alpha contour movement')
    for x,y in contour:
        offset=(y*W+x)*4
        if after[offset+3] and not next_to(before,x,y,True):
            raise ValueError('Visible contour jumped beyond original 1px envelope')
        if not after[offset+3] and not next_to(before,x,y,False):
            raise ValueError('Removed contour is not close to original edge')
    if any(inhale[name]!=base[name] for name in FROZEN):
        raise ValueError('Locked art changes forbidden')
    bag=checker.inspect_masked_occlusion(
        inhale['hand_r'],inhale['accessory_front'],mask,
        canvas=(W,H),minimum_overlap=16,minimum_exposed_below=8)
    return {'changed_visible_pixels':len(changed),
            'changed_alpha_pixels':len(contour),
            'changed_alpha_xy':contour,
            'changed_bbox_xyxy':[min(x for x,y in changed),min(y for x,y in changed),
                                 max(x for x,y in changed)+1,max(y for x,y in changed)+1],
            **bag}


def preview_png(painter,images,crop=None,scale=1):
    if crop is None: crop=(0,0,W,H)
    x0,y0,x1,y1=crop
    cropped_w,cropped_h=x1-x0,y1-y0
    gap=8
    w,h=2*(cropped_w+2*gap),cropped_h+2*gap
    pixels=bytearray(bytes((246,242,232,255))*(w*h))
    for column,rgba in enumerate(images):
        for y in range(y0,y1):
            for x in range(x0,x1):
                source=(y*W+x)*4
                if rgba[source+3]:
                    dest=((y-y0+gap)*w+column*(cropped_w+2*gap)+x-x0+gap)*4
                    pixels[dest:dest+4]=rgba[source:source+4]
    if scale==1:
        return painter.png(pixels,w,h)
    large=bytearray(w*scale*h*scale*4)
    for y in range(h):
        for dy in range(scale):
            for x in range(w):
                color=pixels[(y*w+x)*4:(y*w+x)*4+4]
                start=((y*scale+dy)*w*scale+x*scale)*4
                large[start:start+4*scale]=color*scale
    return painter.png(large,w*scale,h*scale)


def render(destination,source=NATIVE,parent=PARENT,revision=REVISION,direction=DIRECTION):
    artist=load_tool('Render-C03IdleV2Keyposes')
    painter=load_tool('Render-C03OwnerNativeFront')
    checker=load_tool('Validate-C03OwnerRigHandoff')
    manifest=painter.expand_source(source)
    baseline,mask=painter.paint(manifest)
    old=json.loads(Path(parent).read_text(encoding='utf8'))
    rules=json.loads(Path(direction).read_text(encoding='utf8'))
    spec=json.loads(Path(revision).read_text(encoding='utf8'))
    inherited=artist.compile_poses(manifest,old,rules,baseline)
    if inherited['rest']!=baseline:
        raise ValueError('Owner-approved Rest changed')
    inhale,alpha_stats=compile_inhale(spec,manifest,baseline)
    evidence=validate(painter,checker,baseline,inhale,mask)
    frames={'rest':painter.composite(inherited['rest']),
            'inhale_v21':painter.composite(inhale),
            'blink_unchanged':painter.composite(inherited['blink'])}
    folder=Path(destination)
    folder.mkdir(parents=True,exist_ok=True)
    hashes={}
    for name,rgba in frames.items():
        filename=f'C03-idle-v2.1-{name}-1x-DRAFT.png'
        encoded=painter.png(rgba,W,H)
        (folder/filename).write_bytes(encoded)
        hashes[filename]=hashlib.sha256(encoded).hexdigest()
    for filename,crop,scale in [
        ('C03-idle-v2.1-rest-inhale-side-by-side-1x.png',None,1),
        ('C03-idle-v2.1-rest-inhale-side-by-side-4x.png',None,4),
        ('C03-idle-v2.1-shoulder-chest-10x.png',(25,69,112,107),10)]:
        encoded=preview_png(painter,[frames['rest'],frames['inhale_v21']],crop,scale)
        (folder/filename).write_bytes(encoded)
        hashes[filename]=hashlib.sha256(encoded).hexdigest()
    result={'status':'INHALE_V21_DRAFT_AWAITING_OWNER_REVIEW',
        'rest_v2_unchanged':True,'blink_v2_unchanged':True,
        'native_packet_sha256':manifest['packet_sha256'],
        'inhale':evidence,'layer_mutations':alpha_stats,'outputs_sha256':hashes,
        'full_idle_loop':'NOT_CREATED','owner_animation_approval':'PENDING',
        'unity':'NOT_RUN','ip':'NOT_RUN','approved_registry':'UNCHANGED'}
    (folder/'C03-idle-v2.1-review-evidence.json').write_text(
        json.dumps(result,indent=2)+'\n',encoding='utf8')
    return result


if __name__=='__main__':
    cli=argparse.ArgumentParser()
    cli.add_argument('--output-dir',type=Path,required=True)
    args=cli.parse_args()
    print(json.dumps(render(args.output_dir),indent=2))
