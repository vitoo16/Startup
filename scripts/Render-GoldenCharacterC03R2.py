#!/usr/bin/env python3
"""Produce owner-only C03-R2 front review using the existing R1 pixel painter.

No new sprite renderer, Unity import, asset-registry mutation or automatic art approval.
"""
import argparse
import hashlib
import importlib.util
import json
import sys
from pathlib import Path
from PixelPng import png_rgba

ROOT = Path(__file__).resolve().parents[1]
R1_RENDER = ROOT / 'scripts/Render-GoldenCharacterC03R1.py'
R1_SOURCE = ROOT / 'Assets/StartupLife/Art/Source/Characters/golden-character-c03/r1-front-draft.json'
R2_SOURCE = ROOT / 'Assets/StartupLife/Art/Source/Characters/golden-character-c03/r2-front-draft.json'
C03_REVIEW_SCHEMA = 'startup-life.c03-r2-front-pixel-draft.v1'
WIDTH, HEIGHT = 64, 80
BACKGROUND = bytes((244,239,229,255))


def renderer():
    sys.path.insert(0, str(R1_RENDER.parent))
    spec=importlib.util.spec_from_file_location('pixel_source_r1', R1_RENDER)
    module=importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def output_png(pixels, width, height):
    return png_rgba(width,height,(pixels[y*width*4:(y+1)*width*4] for y in range(height)))


def composite_sprite(sheet,sheet_width,pixels,x_offset,y_offset):
    for y in range(HEIGHT):
        for x in range(WIDTH):
            i=(y*WIDTH+x)*4
            if pixels[i+3]:
                dest=((y+y_offset)*sheet_width+x+x_offset)*4
                sheet[dest:dest+4]=pixels[i:i+4]


def exact_integer_scale(pixels,width,height,scale=8):
    result_width,result_height=width*scale,height*scale
    result=bytearray(result_width*result_height*4)
    for y in range(height):
        for x in range(width):
            src=(y*width+x)*4
            color=pixels[src:src+4]
            for dy in range(scale):
                start=(((y*scale)+dy)*result_width+x*scale)*4
                result[start:start+scale*4]=color*scale
    return output_png(result,result_width,result_height)


def build(output_dir, r2_source=R2_SOURCE, r1_source=R1_SOURCE):
    r2=json.loads(Path(r2_source).read_text(encoding='utf-8'))
    r1=json.loads(Path(r1_source).read_text(encoding='utf-8'))
    if (r2['schema']!=C03_REVIEW_SCHEMA or
        r2['status']!='DRAFT_AWAITING_OWNER_REVIEW' or
        r2['native_grid'] != [WIDTH,HEIGHT] or
        any(r2[x] for x in ('owner_approved','ip_cleared','unity_integrated'))):
        raise ValueError('R2 must remain one native, unapproved draft')
    if r1['parts']!=r2['parts'] or len(r2['parts'])!=21 or len(set(r2['parts']))!=21:
        raise ValueError('Shared semantic rig changed')
    paint=renderer()
    r2_pixels,parts=paint.paint(r2)
    r1_pixels,_=paint.paint(r1)
    if len(r2_pixels)!=WIDTH*HEIGHT*4 or not all(
        any(p[i+3] for i in range(0,len(p),4)) for p in parts.values()):
        raise ValueError('Missing native pixel layers')
    black=bytearray(len(r2_pixels))
    for i in range(0,len(black),4):
        if r2_pixels[i+3]:black[i:i+4]=bytes((29,29,32,255))
    w,h=160,96
    comparison=bytearray(BACKGROUND * (w*h))
    composite_sprite(comparison,w,r1_pixels,8,8)
    composite_sprite(comparison,w,r2_pixels,88,8)
    # Source sprite sizes match: LEFT R1 / RIGHT R2.
    files={
      'C03-R2-native-1x.png':output_png(r2_pixels,WIDTH,HEIGHT),
      'C03-R2-review-8x.png':paint.preview(r2_pixels,WIDTH,HEIGHT,8),
      'C03-R2-silhouette.png':paint.preview(black,WIDTH,HEIGHT,8),
      'C03-R2-comparison.png':exact_integer_scale(comparison,w,h,8),
    }
    folder=Path(output_dir)
    folder.mkdir(parents=True,exist_ok=True)
    for name,content in files.items():(folder/name).write_bytes(content)
    layerdir=folder/'layers'
    layerdir.mkdir(exist_ok=True)
    for name,pixels in parts.items():(layerdir/(name+'.png')).write_bytes(output_png(pixels,WIDTH,HEIGHT))
    report={'status':r2['status'],'view':'front', 'native':[WIDTH,HEIGHT],
        'source_commands':len(r2['commands']),'semantic_parts':len(parts),
        'production_approved':False,'unity_device_evidence':'NOT_RUN',
        'owner_decision':'AWAITING_OWNER_REVIEW',
        'file_sha256':{name:hashlib.sha256(data).hexdigest() for name,data in files.items()}}
    (folder/'C03-R2-evidence.json').write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8')
    return report


if __name__=='__main__':
    p=argparse.ArgumentParser()
    p.add_argument('--output-dir',type=Path,required=True)
    p.add_argument('--source',type=Path,default=R2_SOURCE)
    args=p.parse_args()
    print(json.dumps(build(args.output_dir,args.source),indent=2))
