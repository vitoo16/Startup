#!/usr/bin/env python3
"""Quarantined 3-stage C03 R5.1 front review; reuses existing native painter.

Does not generate other views, approve art, or infer rig/Unity readiness.
"""
import argparse
import hashlib
import importlib.util
import json
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SCRIPTS = ROOT / 'scripts'
SOURCE_DIR = ROOT / 'Assets/StartupLife/Art/Source/Characters/golden-character-c03'
SOURCE = SOURCE_DIR / 'r5-1-personality-front-review.json'
PREVIOUS = SOURCE_DIR / 'r5-personality-front-review.json'


def module(name):
    sys.path.insert(0, str(SCRIPTS))
    spec = importlib.util.spec_from_file_location(name, SCRIPTS / (name + '.py'))
    instance = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(instance)
    return instance


def bbox(pixels):
    occupied = [(x, y) for y in range(80) for x in range(64) if pixels[(y*64+x)*4+3]]
    if not occupied:
        raise ValueError('Empty native sprite')
    return (min(x for x,y in occupied), min(y for x,y in occupied),
            max(x for x,y in occupied)+1, max(y for x,y in occupied)+1)


def render(output, source=SOURCE, previous=PREVIOUS):
    painter = module('Render-GoldenCharacterC03R1')
    sheets = module('Render-GoldenCharacterC03R2')
    stages = module('Render-GoldenCharacterC03R5')
    spec = json.loads(Path(source).read_text(encoding='utf-8'))
    prior = json.loads(Path(previous).read_text(encoding='utf-8'))
    if (spec['schema'] != 'startup-life.c03-r5-1-personality-front-review.v1'
        or spec['status'] != 'DRAFT_AWAITING_OWNER_REVIEW'
        or spec['native_grid'] != [64, 80] or spec['view'] != 'front'
        or spec['parts'] != prior['parts'] or len(spec['parts']) != 21
        or any(spec[f] for f in ('owner_approved','ip_cleared','unity_integrated'))):
        raise ValueError('R5.1 owner/stage/source gate failed')
    for unchanged in ('palette', 'native_grid'):
        if spec[unchanged] != prior[unchanged]:
            raise ValueError('R5.1 changed frozen contract: '+unchanged)
    for region in ('face',):
        if [c for c in spec['commands'] if c[1] == region] != [c for c in prior['commands'] if c[1] == region]:
            raise ValueError('R5.1 altered frozen facial personality')
    whole, layers = painter.paint(spec)
    old, old_layers = painter.paint(prior)
    face = stages.stage_composite(layers, ['hair_back', 'head', 'face', 'hair_front'])
    silhouette = bytes(channel for i in range(0,len(whole),4)
                       for channel in ((30,30,34,255) if whole[i+3] else (0,0,0,0)))
    if len(layers) != 21 or set(whole[3::4]) != {0,255}:
        raise ValueError('Layers/alpha contract failed')
    w,h=240,96
    stage_board=bytearray(sheets.BACKGROUND * (w*h))
    for i, image in enumerate((silhouette,face,whole)):
        sheets.composite_sprite(stage_board,w,image,8+80*i,8)
    comp_board=bytearray(sheets.BACKGROUND * (w*h))
    for i, image in enumerate((old,whole)):
        sheets.composite_sprite(comp_board,w,image,8+80*i,8)
    files={}
    for title,px in (('silhouette',silhouette),('face-hair',face),('full-character',whole)):
        files[f'C03-R5.1-{title}-1x.png'] = painter.png(px,64,80)
        files[f'C03-R5.1-{title}-8x.png'] = painter.preview(px,64,80,8)
    files['C03-R5.1-three-stages-1x.png'] = painter.png(stage_board,w,h)
    files['C03-R5.1-three-stages-8x.png'] = sheets.exact_integer_scale(stage_board,w,h,8)
    files['C03-R5-vs-R5.1-8x.png'] = sheets.exact_integer_scale(comp_board,w,h,8)
    path=Path(output);path.mkdir(parents=True, exist_ok=True)
    for name,png in files.items(): (path/name).write_bytes(png)
    a=bbox(whole);hair_crown=a[1]; chin=38; ht=a[3]-a[1]; hh=chin-hair_crown+1
    if not (2.2 <= ht/hh <= 2.4 and 0.42 <= hh/ht <= 0.45):
        raise ValueError('Real native-pixel proportions drift')
    occupied_changed = sum(whole[i:i+4] != old[i:i+4] for i in range(0,len(old),4)
                           if whole[i+3] or old[i+3])
    occupied_union = sum(bool(whole[i+3] or old[i+3]) for i in range(0,len(old),4))
    report = {'status':'DRAFT_AWAITING_OWNER_REVIEW','view':'front','native_grid':[64,80],
              'review_stages':['silhouette','face-hair','full-character'],
              'opaque_bbox_exclusive':list(a),'height_px':ht,'head_height_px':hh,
              'heads_tall':round(ht/hh,4),'head_fraction_pct':round(100*hh/ht,2),
              'authored_commands':len(spec['commands']),'semantic_names':len(layers),
              'changed_occupied_pixels':occupied_changed, 'occupied_pixel_union':occupied_union,
              'changed_occupied_fraction':round(occupied_changed/occupied_union,4),
              'palette_frozen':True,'face_commands_frozen':True,
              'rig_animation':'NOT_RUN','independent_IP':'NOT_RUN',
              'owner_verdict':'AWAITING_OWNER_REVIEW',
              'files_sha256':{name:hashlib.sha256(png).hexdigest() for name,png in files.items()}}
    (path/'C03-R5.1-technical-evidence.json').write_text(json.dumps(report,indent=2,ensure_ascii=False)+'\n',encoding='utf-8')
    return report

if __name__ == '__main__':
    p=argparse.ArgumentParser()
    p.add_argument('--output-dir',required=True,type=Path)
    p.add_argument('--source',type=Path,default=SOURCE)
    p.add_argument('--previous',type=Path,default=PREVIOUS)
    args=p.parse_args()
    print(json.dumps(render(args.output_dir,args.source,args.previous),indent=2))
