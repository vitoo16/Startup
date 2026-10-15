#!/usr/bin/env python3
"""Export one quarantined C03-R4 front; reuses the existing native painter."""
import argparse
import hashlib
import importlib.util
import json
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE_DIR = ROOT / 'Assets/StartupLife/Art/Source/Characters/golden-character-c03'
R1_RENDERER = ROOT / 'scripts/Render-GoldenCharacterC03R1.py'
R2_RENDERER = ROOT / 'scripts/Render-GoldenCharacterC03R2.py'


def load_renderer(path, name):
    sys.path.insert(0, str(path.parent))
    spec = importlib.util.spec_from_file_location(name, path)
    obj = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(obj)
    return obj


def alpha_bbox(pixels, w=64, h=80):
    occupied = [(x,y) for y in range(h) for x in range(w)
                if pixels[(y*w+x)*4+3] != 0]
    if not occupied: raise ValueError('Empty sprite')
    return (min(x for x,y in occupied), min(y for x,y in occupied),
            max(x for x,y in occupied)+1, max(y for x,y in occupied)+1)


def export(directory, source=SOURCE_DIR / 'r4-front-draft.json'):
    r1=load_renderer(R1_RENDERER, 'c03_r1_painter_r4')
    r2=load_renderer(R2_RENDERER, 'c03_r2_helpers_r4')
    r4=json.loads(Path(source).read_text(encoding='utf8'))
    if (r4['schema'] != 'startup-life.c03-r4-front-pixel-draft.v1'
        or r4['status'] != 'DRAFT_AWAITING_OWNER_REVIEW'
        or r4['view'] != 'front' or r4['native_grid'] != [64,80]
        or len(set(r4['parts'])) != 21
        or any(r4[k] for k in ('owner_approved','ip_cleared','unity_integrated'))):
        raise ValueError('Invalid R4 quarantine or native grid')
    previous=[json.loads((SOURCE_DIR / ('r'+version+'-front-draft.json')).read_text(encoding='utf8'))
              for version in ('2','3')]
    if any(v['parts'] != r4['parts'] for v in previous):
        raise ValueError('Changed rig semantic contract')
    pixels,layers=r1.paint(r4)
    if not all(any(a[3::4]) for a in layers.values()):
        raise ValueError('Missing painted semantic layer')
    if set(pixels[3::4]) - {0,255}:
        raise ValueError('Nonbinary alpha found')
    bbox=alpha_bbox(pixels)
    head_top=bbox[1]
    chin=int(r4['review_landmarks']['chin_bottom_y_inclusive'])
    height=bbox[3]-bbox[1]
    head_px=chin-head_top+1
    if not 2.2 <= height/head_px <= 2.4 or not 0.42 <= head_px/height <= 0.45:
        raise ValueError('Actual occupied-pixel chibi ratio out of range')
    shadow=bytearray(len(pixels))
    for i in range(0,len(pixels),4):
        if pixels[i+3]:shadow[i:i+4]=bytes((25,25,29,255))
    comparison_w,comparison_h=240,96
    comparison=bytearray(r2.BACKGROUND * (comparison_w*comparison_h))
    for i,spec in enumerate([*previous,r4]):
        image=pixels if i==2 else r1.paint(spec)[0]
        r2.composite_sprite(comparison,comparison_w,image,8+80*i,8)
    images={
      'C03-R4-native-1x.png':r1.png(pixels,64,80),
      'C03-R4-review-8x.png':r1.preview(pixels,64,80,8),
      'C03-R4-silhouette.png':r1.preview(shadow,64,80,8),
      'C03-R4-comparison-R2-R3-R4.png':r2.exact_integer_scale(comparison,comparison_w,comparison_h,8)
    }
    folder=Path(directory);folder.mkdir(parents=True,exist_ok=True)
    for name,png in images.items():(folder/name).write_bytes(png)
    layer_dir=folder/'layers';layer_dir.mkdir(exist_ok=True)
    for name,layer in layers.items():(layer_dir/(name+'.png')).write_bytes(r1.png(layer,64,80))
    results={
      'status':'DRAFT_AWAITING_OWNER_REVIEW','style_id':r4['style_id'],
      'source':str(Path(source).name),'single_view':'front','native_grid':[64,80],
      'opaque_bbox_exclusive':list(bbox),'occupied_height_px':height,
      'head_landmarks_inclusive':[head_top,chin],'head_height_px':head_px,
      'actual_heads_tall':round(height/head_px,4),
      'head_percent_of_actual_height':round(100*head_px/height,2),
      'semantic_layer_count':len(layers),'authored_pixel_commands':len(r4['commands']),
      'provenance':'R3 base edited with first-party native pixel commands; no concept filter',
      'art_approval':'AWAITING_OWNER_REVIEW','independent_IP':'NOT_RUN',
      'unity_animation_device':'NOT_RUN','files_sha256':{
        name:hashlib.sha256(png).hexdigest() for name,png in images.items()
      }
    }
    (folder/'C03-R4-technical-evidence.json').write_text(json.dumps(results,indent=2,ensure_ascii=False)+'\n',encoding='utf8')
    return results


if __name__=='__main__':
    p=argparse.ArgumentParser()
    p.add_argument('--output-dir',type=Path,required=True)
    p.add_argument('--source',type=Path,default=SOURCE_DIR/'r4-front-draft.json')
    args=p.parse_args()
    print(json.dumps(export(args.output_dir,args.source),indent=2))
