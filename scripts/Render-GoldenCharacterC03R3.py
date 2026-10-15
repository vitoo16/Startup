#!/usr/bin/env python3
"""Owner-review export for ONE native C03-R3 front; reuses the R1 and R2 helpers.

No new painting engine, animation, or automatic approval. Native source is the
hand-authored JSON; this tool only renders and provides quarantined evidence.
"""
import argparse
import hashlib
import importlib.util
import json
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SCRIPT = Path(__file__).resolve().parent
NATIVE = ROOT / 'Assets/StartupLife/Art/Source/Characters/golden-character-c03'
R3_SOURCE = NATIVE / 'r3-front-draft.json'
R2_SOURCE = NATIVE / 'r2-front-draft.json'


def load(name):
    sys.path.insert(0, str(SCRIPT))
    spec = importlib.util.spec_from_file_location(name, SCRIPT/(name+'.py'))
    mod = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(mod)
    return mod


def bounding_box(pixels, w=64, h=80):
    coords = [(x,y) for y in range(h) for x in range(w) if pixels[(y*w+x)*4+3]]
    if not coords: raise ValueError('Empty native sprite')
    return [min(x for x,y in coords),min(y for x,y in coords),
            max(x for x,y in coords)+1,max(y for x,y in coords)+1]


def produce(destination, r3_path=R3_SOURCE, r2_path=R2_SOURCE):
    r1 = load('Render-GoldenCharacterC03R1')
    r2_helpers = load('Render-GoldenCharacterC03R2')
    r3 = json.loads(Path(r3_path).read_text(encoding='utf8'))
    earlier = json.loads(Path(r2_path).read_text(encoding='utf8'))
    if (r3['schema']!='startup-life.c03-r3-front-pixel-draft.v1'
        or r3['view']!='front' or r3['native_grid'] != [64,80]
        or r3['status'] != 'DRAFT_AWAITING_OWNER_REVIEW'
        or r3['parts'] != earlier['parts'] or len(set(r3['parts'])) != 21
        or any(r3[k] for k in ('owner_approved','ip_cleared','unity_integrated'))):
        raise ValueError('R3 front source and quarantined rig contract mismatch')
    pixels,layers=r1.paint(r3)
    previous,_=r1.paint(earlier)
    if not all(any(part[i+3] for i in range(0,len(part),4)) for part in layers.values()):
        raise ValueError('Empty semantic part')
    black=bytearray(len(pixels))
    for i in range(0,len(pixels),4):
        if pixels[i+3]: black[i:i+4] = bytes((30,30,33,255))
    w,h=160,96
    side_by_side=bytearray(r2_helpers.BACKGROUND * (w*h))
    r2_helpers.composite_sprite(side_by_side,w,previous,8,8)
    r2_helpers.composite_sprite(side_by_side,w,pixels,88,8)
    images={
        'C03-R3-native-1x.png':r1.png(pixels,64,80),
        'C03-R3-review-8x.png':r1.preview(pixels,64,80,8),
        'C03-R3-silhouette.png':r1.preview(black,64,80,8),
        'C03-R3-comparison-R2-R3.png':r2_helpers.exact_integer_scale(side_by_side,w,h,8)
    }
    root=Path(destination)
    root.mkdir(parents=True,exist_ok=True)
    for name,data in images.items():(root/name).write_bytes(data)
    layers_path=root/'layers'
    layers_path.mkdir(exist_ok=True)
    for name,raw in layers.items(): (layers_path/(name+'.png')).write_bytes(r1.png(raw,64,80))
    visible=bounding_box(pixels)
    chin=int(r3['review_landmarks']['chin_bottom_y_inclusive'])
    hair_start=min(visible[1], min(y for y in range(80) if any(
      layers[name][(y*64+x)*4+3] for name in ('hair_back','hair_front') for x in range(64))))
    head_px=chin-hair_start+1
    total_px=visible[3]-visible[1]
    if not (2.2<=total_px/head_px<=2.4):
        raise AssertionError('Actual pixel head-to-body ratio falls out of contract')
    if not all(c in (0,255) for c in pixels[3::4]):
        raise AssertionError('Nonbinary alpha introduced')
    report={
      'status':r3['status'],'view':'front','native_grid':[64,80],
      'actual_opaque_bbox_xyxy_exclusive':visible,'actual_height_px':total_px,
      'hair_top_y':hair_start,'chin_landmark_y_inclusive':chin,
      'head_height_px':head_px,'heads_tall':round(total_px/head_px,4),
      'head_height_share':round(head_px/total_px,4),
      'authored_commands':len(r3['commands']),'nonempty_semantic_layers':len(layers),
      'native_edge_alpha':'BINARY_ONLY','owner_gate':'AWAITING_OWNER_REVIEW',
      'unity_animation':'NOT_RUN','independent_ip':'NOT_RUN',
      'visual_art_approval':'NOT_GRANTED',
      'file_sha256':{name:hashlib.sha256(data).hexdigest() for name,data in images.items()}
    }
    (root/'C03-R3-technical-evidence.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
    return report

if __name__=='__main__':
    parser=argparse.ArgumentParser()
    parser.add_argument('--source',type=Path,default=R3_SOURCE)
    parser.add_argument('--previous',type=Path,default=R2_SOURCE)
    parser.add_argument('--output-dir',type=Path,required=True)
    args=parser.parse_args()
    print(json.dumps(produce(args.output_dir,args.source,args.previous),indent=2))
