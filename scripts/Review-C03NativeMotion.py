#!/usr/bin/env python3
"""Preflight native pixel motion readability for C03; NOT a Golden art approval.

Measures continuous head/hair pixel-crawl and stationary planted feet from
actual RGBA frames. Unlike the rejected Idle v1 checker, intentional held
frames are welcome. Requires human review even if all numeric checks pass.
"""
import argparse
import importlib.util
import json
import sys
from pathlib import Path

ROOT=Path(__file__).resolve().parents[1]
ART=ROOT/'Assets/StartupLife/Art/Source/Characters/golden-character-c03'
SPEC=ART/'C03-idle-v2-art-direction.json'
DECODER=ROOT/'scripts/Validate-GoldenCharacterC03R1.py'


def decoder():
    spec=importlib.util.spec_from_file_location('c03_png_reader',DECODER)
    module=importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module.decode_rgba_png


def count_region_change(a,b,width,start,end):
    return sum(a[p:p+4]!=b[p:p+4]
               for y in range(start,end) for x in range(width)
               for p in [(y*width+x)*4])


def inspect(frames,contract):
    """Exact pixel statistics; no image filtering, interpolation or heuristics from filenames."""
    if (contract.get('schema')!='startup-life.c03.idle-v2-motion-direction.v1'
        or contract.get('owner_approved_idle_animation') is not False
        or contract.get('no_automatic_runtime_promotion') is not True):
        raise ValueError('Invalid C03 motion approval separation')
    w,h=contract['canvas_px']
    rule=contract['quality_checks']
    if (len(frames)<4 or len(frames)>48 or
        any(len(frame)!=w*h*4 for frame in frames)):
        raise ValueError('C03 motion requires 4..48 native RGBA frames')
    if any(pixel not in (0,255) for frame in frames for pixel in frame[3::4]):
        raise ValueError('Nonbinary alpha in native keyframe')
    for kind in ('face_hair_roi_y','torso_roi_y','planted_shoe_roi_y'):
        lo,hi=rule[kind]
        if not(0<=lo<hi<=h):raise ValueError('Invalid native motion audit region')
    head_start,head_end=rule['face_hair_roi_y']
    torso_start,torso_end=rule['torso_roi_y']
    shoe_start,shoe_end=rule['planted_shoe_roi_y']
    base=frames[0]
    heads=[count_region_change(base,f,w,head_start,head_end) for f in frames]
    torsos=[count_region_change(base,f,w,torso_start,torso_end) for f in frames]
    shoes=[count_region_change(base,f,w,shoe_start,shoe_end) for f in frames]
    all_changes=[sum(base[p:p+4]!=f[p:p+4]
                     for p in range(0,len(base),4)) for f in frames]
    # Row-normalized dominance makes a large, constantly rotating head fail,
    # while a 2-3 pixel eyelid edit can occur during an otherwise calm hold.
    head_rate=max(heads)/(head_end-head_start)
    torso_rate=max(torsos)/(torso_end-torso_start)
    ratio=round(head_rate/torso_rate,4) if torso_rate else (
        float('inf') if head_rate else 0.0)
    issues=[]
    if max(heads)>rule['max_head_changed_pixels_vs_rest']:
        issues.append('EXCESS_HEAD_PIXEL_CRAWL')
    if (max(heads)>rule['apply_ratio_only_if_head_changed_pixels_exceeds']
        and ratio>rule['max_head_to_torso_changed_pixels_per_row_ratio']):
        issues.append('HEAD_MOVES_OUT_OF_PROPORTION_TO_TORSO')
    if rule['feet_must_be_pixel_identical_in_idle'] and max(shoes)>0:
        issues.append('IDLE_PLANTED_FEET_NOT_STABLE')
    if max(all_changes)<8:
        issues.append('NO_AUTHORED_IDLE_MOTION')
    if rule['native_bbox_alpha_must_not_touch_canvas']:
        for frame in frames:
            if (any(frame[(x)*4+3] for x in range(w)) or
                any(frame[((h-1)*w+x)*4+3] for x in range(w)) or
                any(frame[(y*w)*4+3] for y in range(h)) or
                any(frame[(y*w+w-1)*4+3] for y in range(h))):
                issues.append('SPRITE_TOUCHES_CANVAS_EDGE')
                break
    return {
        'status':'REJECTED_MOTION_PREFLIGHT' if issues else 'STRUCTURAL_PREFLIGHT_ONLY_OWNER_PENDING',
        'passed_artistic_review':False,
        'unity_validation':'NOT_RUN',
        'frames':len(frames),
        'max_changed_head_pixels':max(heads),
        'max_changed_torso_pixels':max(torsos),
        'max_changed_shoe_pixels':max(shoes),
        'max_changed_frame_pixels':max(all_changes),
        'row_normalized_head_to_torso_ratio':None if ratio==float('inf') else ratio,
        'held_adjacent_pairs':sum(frames[i]==frames[i+1] for i in range(len(frames)-1)),
        'occlusion_layer_proof':'PENDING_AUTHORED_PER_KEYPOSE_LAYER_MASKS',
        'issues':issues,
        'note':'Passing this structural preflight does not approve animation; keypose aesthetics and real Unity motion are human gates.'
    }


def check_files(directory,contract_path=SPEC):
    contract=json.loads(Path(contract_path).read_text(encoding='utf8'))
    files=sorted(Path(directory).glob('*.png'))
    if not files:
        raise ValueError('No native motion frames in directory')
    read=decoder()
    frames=[]
    for path in files:
        w,h,pixels=read(path.read_bytes())
        if [w,h]!=contract['canvas_px']:
            raise ValueError('Wrong native frame canvas: '+path.name)
        frames.append(pixels)
    return inspect(frames,contract)


if __name__=='__main__':
    cli=argparse.ArgumentParser()
    cli.add_argument('--frames-dir',type=Path,required=True)
    cli.add_argument('--contract',type=Path,default=SPEC)
    cli.add_argument('--report',type=Path)
    args=cli.parse_args()
    try:
        result=check_files(args.frames_dir,args.contract)
        if args.report:
            args.report.parent.mkdir(parents=True,exist_ok=True)
            args.report.write_text(json.dumps(result,indent=2,ensure_ascii=False)+'\n',encoding='utf8')
        print(json.dumps(result,indent=2,ensure_ascii=False))
        sys.exit(1 if result['issues'] else 0)
    except (ValueError,KeyError,FileNotFoundError) as exc:
        print('FAIL C03 motion preflight: '+str(exc),file=sys.stderr)
        sys.exit(1)
