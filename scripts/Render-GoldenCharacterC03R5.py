#!/usr/bin/env python3
"""Three-stage native-pixel C03-R5 personality review (never Golden acceptance).

Uses the original R1 polygon/pixel-row painter and R2 canvas compositors.
No rig or full runtime sprite import is performed.
"""
from pathlib import Path
import argparse,importlib.util,hashlib,json,sys
ROOT=Path(__file__).resolve().parents[1]
SRC=ROOT/'Assets/StartupLife/Art/Source/Characters/golden-character-c03'
SCRIPTS=ROOT/'scripts'

def load(name):
    sys.path.insert(0,str(SCRIPTS))
    spec=importlib.util.spec_from_file_location(name,SCRIPTS/(name+'.py'))
    obj=importlib.util.module_from_spec(spec);spec.loader.exec_module(obj);return obj

def stage_composite(layers,names):
    rgba=bytearray(64*80*4)
    for name in names:
        pixels=layers[name]
        for pos in range(0,len(pixels),4):
            if pixels[pos+3]: rgba[pos:pos+4]=pixels[pos:pos+4]
    return bytes(rgba)

def export(folder,source=SRC/'r5-personality-front-review.json'):
    r1=load('Render-GoldenCharacterC03R1');r2=load('Render-GoldenCharacterC03R2')
    data=json.loads(Path(source).read_text(encoding='utf8'))
    if (data['schema']!='startup-life.c03-r5-personality-front-review.v1'
        or data['status']!='DRAFT_AWAITING_OWNER_REVIEW'
        or data['view']!='front' or data['native_grid'] != [64,80]
        or set(data['parts']) != set(json.loads((SRC/'r4-front-draft.json').read_text(encoding='utf8'))['parts'])
        or len(data['parts']) != 21 or data['parts'][-1] != 'hand_r'
        or any(data[k] for k in ('owner_approved','ip_cleared','unity_integrated'))):
        raise ValueError('Invalid R5 one-front quarantine/staging/stack order')
    full,layers=r1.paint(data)
    face=stage_composite(layers,['head','face','hair_front'])
    shadow=bytes(v for px in (full[i:i+4] for i in range(0,len(full),4))
                 for v in ((35,35,39,255) if px[3] else (0,0,0,0)))
    w,h=240,96
    sheet=bytearray(r2.BACKGROUND*(w*h))
    for index,pixels in enumerate((shadow,face,full)):
        r2.composite_sprite(sheet,w,pixels,8+80*index,8)
    p2=json.loads((SRC/'r2-front-draft.json').read_text(encoding='utf8'))
    p4=json.loads((SRC/'r4-front-draft.json').read_text(encoding='utf8'))
    history=bytearray(r2.BACKGROUND*(w*h))
    for index,pixels in enumerate((r1.paint(p2)[0],r1.paint(p4)[0],full)):
        r2.composite_sprite(history,w,pixels,8+80*index,8)
    result={}
    for name,pixels in [('silhouette',shadow),('face-hair',face),('full-character',full)]:
        result['C03-R5-'+name+'-1x.png']=r1.png(pixels,64,80)
        result['C03-R5-'+name+'-8x.png']=r1.preview(pixels,64,80,8)
    result['C03-R5-stages-1x.png']=r1.png(sheet,w,h)
    result['C03-R5-stages-8x.png']=r2.exact_integer_scale(sheet,w,h,8)
    result['C03-R5-comparison-R2-R4-R5-8x.png']=r2.exact_integer_scale(history,w,h,8)
    folder=Path(folder);folder.mkdir(parents=True,exist_ok=True)
    for name,png in result.items():(folder/name).write_bytes(png)
    coords=[(x,y) for y in range(80) for x in range(64) if full[(y*64+x)*4+3]]
    x0=min(x for x,y in coords);y0=min(y for x,y in coords);x1=max(x for x,y in coords)+1;y1=max(y for x,y in coords)+1
    head=38-y0+1;body=y1-y0
    evidence={'status':data['status'],'view':'front','native_grid':[64,80],
      'review_stage':'SILHOUETTE_FACE_HAIR_FULL','gate':'AWAITING_OWNER_REVIEW',
      'visible_bbox_xyxy_exclusive':[x0,y0,x1,y1], 'actual_height_px':body,
      'head_landmarks_y_inclusive':[y0,38],'head_height_px':head,
      'heads_tall':round(body/head,4),'head_percent':round(100*head/body,2),
      'authored_commands':len(data['commands']),
      'semantic_names_reserved':len(data['parts']),
      'layer_animation_ready':False,'final_21_layer_rig_gate':'NOT_STARTED',
      'art_approval':'NOT_GRANTED','independent_IP':'NOT_RUN','unity_device':'NOT_RUN',
      'output_sha256':{name:hashlib.sha256(png).hexdigest() for name,png in result.items()}}
    (folder/'C03-R5-technical-evidence.json').write_text(json.dumps(evidence,ensure_ascii=False,indent=2)+'\n',encoding='utf8')
    return evidence

if __name__=='__main__':
    cli=argparse.ArgumentParser();cli.add_argument('--output-dir',required=True);cli.add_argument('--source',type=Path,default=SRC/'r5-personality-front-review.json');args=cli.parse_args()
    print(json.dumps(export(args.output_dir,args.source),indent=2))
