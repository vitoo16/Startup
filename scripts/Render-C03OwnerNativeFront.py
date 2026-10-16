#!/usr/bin/env python3
"""Reconstruct C03's manually authored 128x160 native row-runs (DRAFT only).

Not an HD resampler or approval pipeline. Reuses first-party PixelPng.py.
Layer names, transparency and draw order match the existing shared rig.
"""
import argparse
import base64
import hashlib
import json
import lzma
import struct
import sys
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parent))
from PixelPng import png_rgba

ROOT=Path(__file__).resolve().parents[1]
SOURCE=ROOT/'Assets/StartupLife/Art/Source/Characters/golden-character-c03/C03-native-front-source.json'
PARTS=('hair_back','accessory_back','foot_l','foot_r','lower_leg_l','lower_leg_r',
 'upper_leg_l','upper_leg_r','pelvis','torso','upper_arm_l','upper_arm_r',
 'forearm_l','forearm_r','hand_l','hand_r','outfit_torso','head','face',
 'hair_front','accessory_front')
WIDTH,HEIGHT=128,160
EXPECTED_VISUAL_SHA='d2e7e9a79672e23408c0a48626ef3968af84f5564f44e14eac41e4add38cb25e'



def expand_source(source):
    """Decode a size-bounded, SHA-pinned lossless native pixel-row source.

    Row packet: magic C03N, big-endian uint16 counts, [part,y,x,length,palette]
    bytes for each colored pixel run, then [y,x,length] bag mask runs.
    Only 128x160 native pixels; no HD texture sampling or rescaling.
    """
    descriptor=json.loads(Path(source).read_text(encoding='utf8'))
    if 'compressed_runs_file' not in descriptor:
        return descriptor
    base=Path(source).resolve().parent
    filename=descriptor['compressed_runs_file']
    if (not isinstance(filename,str) or Path(filename).name!=filename
        or filename!='C03-native-front-pixel-runs.lzma.b64'):
        raise ValueError('Noncanonical source stream filename')
    packed=base/filename
    encoded=base64.b64decode(packed.read_text(encoding='ascii').strip(),validate=True)
    decompressor=lzma.LZMADecompressor()
    raw=decompressor.decompress(encoded,max_length=20000)
    if not decompressor.eof or decompressor.unused_data or len(raw)>20000:
        raise ValueError('Oversized or truncated C03 native source packet')
    if hashlib.sha256(raw).hexdigest()!=descriptor.get('packet_sha256'):
        raise ValueError('Native pixel packet SHA256 does not match source')
    if raw[:4]!=b'C03N' or len(raw)<8:
        raise ValueError('Unknown native pixel source packet')
    count,mask_count=struct.unpack('>HH',raw[4:8])
    if (count!=descriptor.get('pixel_runs_count') or
        mask_count!=descriptor.get('bag_body_mask_runs_count') or
        len(raw)!=8+5*count+3*mask_count):
        raise ValueError('Corrupt native source row counts')
    palette=descriptor['palette_order']
    if (len(palette)!=len(descriptor['palette']) or
        set(palette)!=set(descriptor['palette']) or len(set(palette))!=len(palette)):
        raise ValueError('Native pixel palette ordering corrupted')
    layers={name:[] for name in PARTS}
    offset=8
    for index in range(count):
        part,y,x,length,color=raw[offset:offset+5]
        offset+=5
        if part>=len(PARTS) or color>=len(palette):
            raise ValueError('Invalid authored part/palette index')
        layers[PARTS[part]].append([y,x,length,palette[color]])
    mask=[]
    for index in range(mask_count):
        y,x,length=raw[offset:offset+3]
        offset+=3
        mask.append([y,x,length])
    descriptor['layers']=layers
    descriptor['bag_body_mask']=mask
    return descriptor

def paint(data):
    if (data.get('schema')!='startup-life.c03.native-layer-runs.v1'
        or data.get('status')!='NATIVE_FRONT_RECONSTRUCTION_DRAFT_NOT_GOLDEN_APPROVED'
        or data.get('view')!='front'
        or data.get('canvas_px')!=[WIDTH,HEIGHT]
        or data.get('pixels_per_unit')!=32
        or tuple(data.get('part_order',()))!=PARTS
        or data.get('visual_master_sha256')!=EXPECTED_VISUAL_SHA
        or data.get('owner_native_reconstruction_approved') is not False
        or data.get('ip_cleared') is not False
        or data.get('unity_animation_verified') is not False):
        raise ValueError('Unreviewed C03 native source contract drift')
    if set(data['layers'])!=set(PARTS):
        raise ValueError('Missing or invalid C03 native parts')
    palette={}
    for key,hexcolor in data['palette'].items():
        if not isinstance(hexcolor,str) or len(hexcolor)!=7 or not hexcolor.startswith('#'):
            raise ValueError('Bad native palette entry')
        palette[key]=bytes.fromhex(hexcolor[1:])+b'\xff'
    result={}
    for part in PARTS:
        rows=data['layers'][part]
        if not rows: raise ValueError('Empty rig part '+part)
        pixels=bytearray(WIDTH*HEIGHT*4)
        for row in rows:
            if len(row)!=4: raise ValueError('Bad row-run structure')
            y,x,length,key=row
            if (any(type(z) is not int for z in (y,x,length))
                or not (0<=y<HEIGHT and 0<=x<WIDTH and 1<=length<=WIDTH-x)
                or key not in palette):
                raise ValueError('Invalid native pixel row')
            pos=(y*WIDTH+x)*4
            if any(pixels[i+3] for i in range(pos,pos+length*4,4)):
                raise ValueError('Duplicate opaque pixels within '+part)
            pixels[pos:pos+length*4]=palette[key]*length
        result[part]=bytes(pixels)
    mask=bytearray(WIDTH*HEIGHT*4)
    for row in data['bag_body_mask']:
        if len(row)!=3:raise ValueError('Malformed bag-body row')
        y,x,length=row
        if (any(type(z) is not int for z in row)
            or not(0<=y<HEIGHT and 0<=x<WIDTH and 1<=length<=WIDTH-x)):
            raise ValueError('Invalid native bag-body mask')
        pos=(y*WIDTH+x)*4
        if any(mask[i+3] for i in range(pos,pos+length*4,4)):
            raise ValueError('Duplicate bag-body mask pixels')
        mask[pos:pos+length*4]=b'\xff\xff\xff\xff'*length
    return result,bytes(mask)


def composite(parts):
    merged=bytearray(WIDTH*HEIGHT*4)
    for name in PARTS:
        layer=parts[name]
        for i in range(0,len(merged),4):
            if layer[i+3]:merged[i:i+4]=layer[i:i+4]
    return bytes(merged)


def png(pixels,w=WIDTH,h=HEIGHT):
    return png_rgba(w,h,(pixels[y*w*4:(y+1)*w*4] for y in range(h)))


def enlarge(pixels,scale=8):
    # Integer nearest-neighbor only, no interpolation / AA.
    margin=8
    w,h=(WIDTH+margin*2)*scale,(HEIGHT+margin*2)*scale
    image=bytearray(bytes((246,242,232,255))*(w*h))
    for y in range(HEIGHT):
        for x in range(WIDTH):
            pos=(y*WIDTH+x)*4
            if pixels[pos+3]:
                for offset in range(scale):
                    start=(((y+margin)*scale+offset)*w+(x+margin)*scale)*4
                    image[start:start+4*scale]=pixels[pos:pos+4]*scale
    return png(image,w,h)


def export(source,out):
    spec=expand_source(source)
    layers,mask=paint(spec)
    image=composite(layers)
    matte=bytearray(len(image))
    for i in range(0,len(image),4):
        if image[i+3]:matte[i:i+4]=bytes((32,33,38,255))
    out=Path(out)
    out.mkdir(parents=True,exist_ok=True)
    images={
      'C03-native-front-128x160-DRAFT.png':png(image),
      'C03-native-front-8x-DRAFT.png':enlarge(image),
      'C03-native-silhouette-8x-DRAFT.png':enlarge(matte),
    }
    for name,content in images.items():(out/name).write_bytes(content)
    folder=out/'layers'
    folder.mkdir(exist_ok=True)
    for name,data in layers.items():(folder/(name+'.png')).write_bytes(png(data))
    mask_dir=folder/'masks'
    mask_dir.mkdir(exist_ok=True)
    (mask_dir/'bag_body.png').write_bytes(png(mask))
    occupied=[(x,y) for y in range(HEIGHT) for x in range(WIDTH)
              if image[(y*WIDTH+x)*4+3]]
    bbox=[min(x for x,y in occupied),min(y for x,y in occupied),
          max(x for x,y in occupied)+1,max(y for x,y in occupied)+1]
    evidence={'status':spec['status'],'view':'front','native_grid':[WIDTH,HEIGHT],
              'opaque_bbox_exclusive':bbox,'nonempty_layers':len(layers),
              'owner_native_review':'PENDING','ip':'NOT_RUN','unity':'NOT_RUN',
              'image_sha256':{k:hashlib.sha256(v).hexdigest() for k,v in images.items()}}
    (out/'C03-native-front-evidence.json').write_text(json.dumps(evidence,indent=2)+'\n')
    return evidence


if __name__=='__main__':
    cli=argparse.ArgumentParser()
    cli.add_argument('--source',type=Path,default=SOURCE)
    cli.add_argument('--output-dir',type=Path,required=True)
    cli.add_argument('--expanded-source-output',type=Path,
                     help='Optional, editable/inspectable complete native row JSON')
    args=cli.parse_args()
    if args.expanded_source_output:
        expanded=expand_source(args.source)
        expanded.pop('compressed_runs_file',None)
        expanded.pop('codec',None)
        args.expanded_source_output.parent.mkdir(parents=True,exist_ok=True)
        args.expanded_source_output.write_text(json.dumps(expanded,indent=2,ensure_ascii=False)+'\n',encoding='utf8')
    print(json.dumps(export(args.source,args.output_dir),indent=2))
