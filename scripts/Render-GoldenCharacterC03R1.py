#!/usr/bin/env python3
"""Render hand-composed C03 R1 native pixel commands (draft, never production approved).

The JSON artwork lists explicit polygon/cluster strokes and semantic rig layers.
This renderer never references the rejected C03 silhouettes or approved assets.
"""
import argparse
import hashlib
import json
from pathlib import Path
from PixelPng import png_rgba

SOURCE = Path(__file__).resolve().parents[1] / 'Assets/StartupLife/Art/Source/Characters/golden-character-c03/r1-front-draft.json'


def paint(spec):
    width, height = spec['native_grid']
    if [width, height] != [64, 80] or len(spec['parts']) != 21:
        raise ValueError('Unexpected draft pixel/rig contract')
    names = spec['parts']
    if len(set(names)) != len(names) or spec['status'] != 'DRAFT_AWAITING_OWNER_REVIEW':
        raise ValueError('Invalid draft status or rig layers')
    palette = {name:bytes.fromhex(value[1:])+b'\xff' for name,value in spec['palette'].items()}
    if any(len(color) != 4 for color in palette.values()):
        raise ValueError('Invalid RGBA palette')
    layers = {name:bytearray(width*height*4) for name in names}

    def set_pixel(layer, color, x, y):
        if layer not in layers or color not in palette or not(0 <= x < width and 0 <= y < height):
            raise ValueError('Invalid rig layer, palette or grid coordinate')
        index = (y*width+x)*4
        layers[layer][index:index+4] = palette[color]

    for command in spec['commands']:
        kind, layer, color, *coords = command
        if kind == 'R':
            for x,y,length in coords:
                if length <= 0 or x+length > width: raise ValueError('Invalid native cluster')
                for dx in range(length):set_pixel(layer,color,x+dx,y)
        elif kind == 'P':
            if len(coords) < 3 or len({tuple(p) for p in coords}) < 3:
                raise ValueError('Invalid authored polygon')
            if any(not(0<=x<width and 0<=y<height) for x,y in coords):
                raise ValueError('Native polygon exceeds canvas')
            for y in range(min(p[1] for p in coords), max(p[1] for p in coords)+1):
                sy = y + 0.5
                hits = sorted(a[0]+(sy-a[1])*(b[0]-a[0])/(b[1]-a[1])
                              for a,b in zip(coords,coords[1:]+coords[:1])
                              if a[1]<=sy<b[1] or b[1]<=sy<a[1])
                for left,right in zip(hits[::2],hits[1::2]):
                    for x in range(int(left)-1,int(right)+2):
                        if left<=x+0.5<right:set_pixel(layer,color,x,y)
        else:raise ValueError('Unsupported native paint primitive')

    out = bytearray(width*height*4)
    for layer in names:
        for i in range(0,len(out),4):
            if layers[layer][i+3]:out[i:i+4] = layers[layer][i:i+4]
    return bytes(out), layers


def png(pixels, width, height):
    return png_rgba(width,height,(pixels[y*width*4:(y+1)*width*4]
                                 for y in range(height)))


def preview(pixels, width, height, scale):
    if scale not in (1,4,8):raise ValueError('Native and exact NN scales only')
    pad = 8
    w,h=(width+2*pad)*scale,(height+2*pad)*scale
    out = bytearray(bytes((244,239,229,255)) * (w*h))
    for y in range(height):
        for x in range(width):
            i=(y*width+x)*4
            if pixels[i+3]:
                for dy in range(scale):
                    for dx in range(scale):
                        j=(((y+pad)*scale+dy)*w+(x+pad)*scale+dx)*4
                        out[j:j+4]=pixels[i:i+4]
    return png(out,w,h)


def export(source, output):
    spec=json.loads(Path(source).read_text(encoding='utf8'))
    if spec['schema']!='startup-life.c03-r1-front-pixel-draft.v1':
        raise ValueError('Unknown sprite draft schema')
    pixels,layers=paint(spec)
    width,height=spec['native_grid']
    output=Path(output);output.mkdir(parents=True,exist_ok=True)
    files={'front-native-64x80.png':png(pixels,width,height)}
    files.update({f'front-review-{scale}x.png':preview(pixels,width,height,scale) for scale in (1,4,8)})
    for name,data in files.items(): (output/name).write_bytes(data)
    report={'status':spec['status'],'view':spec['view'], 'native_grid':spec['native_grid'],
            'commands':len(spec['commands']), 'rig_layers':len(layers),
            'populated_layers':sum(any(p[i+3] for i in range(0,len(p),4)) for p in layers.values()),
            'sha256':{name:hashlib.sha256(data).hexdigest() for name,data in files.items()},
            'approval':{'owner':False,'independent_ip':False,'unity':False,'device':False}}
    (output/'evidence.json').write_text(json.dumps(report,indent=2)+'\n',encoding='utf8')
    return report


if __name__=='__main__':
    parser=argparse.ArgumentParser()
    parser.add_argument('--source',type=Path,default=SOURCE)
    parser.add_argument('--output-dir',type=Path,required=True)
    args=parser.parse_args()
    print(json.dumps(export(args.source,args.output_dir),indent=2))
