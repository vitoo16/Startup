#!/usr/bin/env python3
"""Reproduce two native front-only C03 idle candidate previews using the existing R1 painter."""
import argparse
import hashlib
import importlib.util
import json
import sys
from pathlib import Path

ROOT=Path(__file__).resolve().parents[1]
SCRIPTS=ROOT/'scripts'
SRC=ROOT/'Assets/StartupLife/Art/Source/Characters/golden-character-c03'

def module(name):
    sys.path.insert(0,str(SCRIPTS))
    spec=importlib.util.spec_from_file_location(name,SCRIPTS/(name+'.py'))
    mod=importlib.util.module_from_spec(spec)
    spec.loader.exec_module(mod)
    return mod

def painted_parts(spec,painter):
    img,layers=painter.paint(spec)
    if len(layers)!=21 or not all(any(p[3::4]) for p in layers.values()):
        raise ValueError('Expected 21 nonempty semantic layers')
    if set(img[3::4]) - {0,255}:
        raise ValueError('Partial alpha introduced')
    return img,layers

def shadow(native):
    return bytes(channel for i in range(0,len(native),4) for channel in
                 ((24,27,32,255) if native[i+3] else (0,0,0,0)))

def cutout_on_canvas(raw,width,x0,y0,cw,ch):
    data=bytearray(cw*ch*4)
    for y in range(ch):
        src=((y+y0)*width+x0)*4
        data[y*cw*4:(y+1)*cw*4]=raw[src:src+cw*4]
    return bytes(data)

def output(folder,asset_dir=SRC):
    painter=module('Render-GoldenCharacterC03R1')
    sheet=module('Render-GoldenCharacterC03R2')
    p=Path(asset_dir)
    old=json.loads((p/'r5-1-personality-front-review.json').read_text(encoding='utf8'))
    specimens={}
    for key in ('A','B'):
        source=json.loads((p/f'C03-IdlePose-{key}.json').read_text(encoding='utf8'))
        if source.get('schema')!='startup-life.c03-idle-pose-native-candidate.v1' or source.get('candidate')!=key:
            raise ValueError('Invalid A/B source signature')
        if source.get('base_sha256') != hashlib.sha256((p/'r5-1-personality-front-review.json').read_bytes()).hexdigest():
            raise ValueError('Base R5.1 source mismatch')
        if source['parts'][-1]!='accessory_front' or source['parts'].index('hand_r')>source['parts'].index('accessory_front'):
            raise ValueError('Incorrect hand-on-top sorting')
        if source['palette']!=old['palette'] or len(source['parts']) != 21 or set(source['parts']) != set(old['parts']):
            raise ValueError('Frozen palette or semantic layers changed')
        frozen={'hair_back','hair_front','head','face','torso','pelvis','upper_leg_l','upper_leg_r','lower_leg_l','lower_leg_r','foot_l','foot_r','outfit_torso'}
        for part in frozen:
            cmds=lambda record:[c for c in record['commands'] if c[1]==part]
            if cmds(source)!=cmds(old):
                raise ValueError('Frozen native geometry changed: '+part)
        specimens[key]=painted_parts(source,painter)
    previous=painter.paint(old)[0]
    w,h=64,80
    native={}
    for n,(img,layers) in specimens.items():
        native[f'C03-IdlePose-{n}-1x.png']=painter.png(img,w,h)
        native[f'C03-IdlePose-{n}-8x.png']=painter.preview(img,w,h,8)
    silhouette=bytearray(sheet.BACKGROUND * (160*96))
    for index,n in enumerate(('A','B')):
        sheet.composite_sprite(silhouette,160,shadow(specimens[n][0]),8+index*80,8)
    native['C03-IdlePose-Silhouette-AB.png']=sheet.exact_integer_scale(silhouette,160,96,8)
    native['C03-IdlePose-Silhouette-AB-1x.png']=painter.png(silhouette,160,96)
    compare=bytearray(sheet.BACKGROUND * (240*96))
    for index,im in enumerate((previous,specimens['A'][0],specimens['B'][0])):
        sheet.composite_sprite(compare,240,im,8+80*index,8)
    native['C03-IdlePose-Comparison.png']=sheet.exact_integer_scale(compare,240,96,8)
    crops=bytearray(sheet.BACKGROUND * (76*41))
    for index,n in enumerate(('A','B')):
        cropped=cutout_on_canvas(specimens[n][0],w,34,45,30,30)
        for y in range(30):
            for x in range(30):
                a=(y*30+x)*4
                if cropped[a+3]:
                    b=((y+5)*76+(x+5+index*38))*4
                    crops[b:b+4]=cropped[a:a+4]
    native['C03-IdlePose-HandCrop-AB.png']=sheet.exact_integer_scale(crops,76,41,12)
    folder=Path(folder);folder.mkdir(parents=True,exist_ok=True)
    for name,binary in native.items():
        (folder/name).write_bytes(binary)
    metrics={}
    for n,(img,parts) in specimens.items():
        pts=[(x,y) for y in range(h) for x in range(w) if img[(y*w+x)*4+3]]
        bbox=[min(x for x,y in pts),min(y for x,y in pts),max(x for x,y in pts)+1,max(y for x,y in pts)+1]
        head=38-bbox[1]+1;body=bbox[3]-bbox[1]
        if not(2.2<=body/head<=2.4):
            raise ValueError('Head ratio violated')
        palm={i for i in range(0,len(img),4) if parts['hand_r'][i+3]}
        bags={i for part in ('accessory_front','accessory_back') for i in range(0,len(img),4) if parts[part][i+3]}
        metrics[n]={
          'actual_alpha_bbox_exclusive':bbox,'occupied_height_px':body,
          'head_height_px':head,'heads_tall':round(body/head,4),'head_pct':round(100*head/body,2),
          'all_semantic_layers_populated':True,'hand_pixels':len(palm),
          'hand_bag_layer_overlap_pixels':len(palm & bags),
          'hand_layer_before_bag':True,
        }
        layer_dir=folder/f'layers-{n}';layer_dir.mkdir(exist_ok=True)
        for name,pixels in parts.items():
            (layer_dir/(name+'.png')).write_bytes(painter.png(pixels,w,h))
    report={
      'status':'A_AND_B_AWAITING_OWNER_REVIEW',
      'base_version':'C03-R5.1','chosen':None,'palette_frozen':True,
      'head_hair_face_body_legs_shoes_frozen':True,
      'view':'front','canvas_px':[64,80],'layer_names':21,
      'rig_animation':'NOT_RUN','independent_ip_review':'NOT_RUN','unity_device':'NOT_RUN',
      'candidates':metrics,
      'file_sha256':{k:hashlib.sha256(v).hexdigest() for k,v in native.items()},
    }
    (folder/'C03-IdlePose-technical-evidence.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n',encoding='utf8')
    return report

if __name__=='__main__':
    parser=argparse.ArgumentParser()
    parser.add_argument('--output-dir',type=Path,required=True)
    parser.add_argument('--asset-dir',type=Path,default=SRC)
    args=parser.parse_args()
    print(json.dumps(output(args.output_dir,args.asset_dir),indent=2))
