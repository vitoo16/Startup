#!/usr/bin/env python3
"""Non-invasive C03 Unity rig authoring manifest (NOT an imported prefab).

This file describes transforms/renderer ordering for a human-controlled Unity
Editor import. It never edits .meta, scenes, GameObjects or production registry.
All 21 layer PNGs must first come from Render-C03OwnerNativeFront.py.
"""
import argparse
import hashlib
import importlib.util
import json
import sys
from pathlib import Path

ROOT=Path(__file__).resolve().parents[1]
BASE=ROOT/'Assets/StartupLife/Art/Source/Characters/golden-character-c03'
PLAN=BASE/'C03-front-joint-plan-DRAFT.json'
CONTRACT=BASE/'C03-owner-front-rig-contract.json'
SOURCE=BASE/'C03-native-front-source.json'
TOOLS=ROOT/'scripts'


def load_preview():
    sys.path.insert(0,str(TOOLS))
    spec=importlib.util.spec_from_file_location('c03_rig_preview',TOOLS/'Preview-C03FrontRig.py')
    instance=importlib.util.module_from_spec(spec)
    spec.loader.exec_module(instance)
    return instance


def vec_pixel_to_unity(dx, dy, pixels_per_unit):
    return [round(dx / pixels_per_unit, 8), round(-dy / pixels_per_unit, 8)]


def generate(plan, contract, source):
    preview=load_preview()
    joints,assigned=preview.read_plan(plan,contract,source)
    if plan.get('owner_idle_animation_verdict') != 'REJECTED' or plan.get('legacy_idle_loop_allowed_for_production') is not False:
        raise ValueError('Legacy C03 Idle rejection record is required before Unity handoff')
    width,height=plan['canvas_px']
    ppu=plan['pixels_per_unit']
    root=joints['root']['pivot']
    if width!=128 or height!=160 or ppu!=32:
        raise ValueError('Unreviewed native production grid or PPU drift')
    rig=[]
    for name,item in joints.items():
        parent=item['parent']
        if parent is None:
            local=[0.0,0.0]
        else:
            parent_pivot=joints[parent]['pivot']
            local=vec_pixel_to_unity(item['pivot'][0]-parent_pivot[0],
                                     item['pivot'][1]-parent_pivot[1],ppu)
        rig.append({'id':name, 'parent':parent,'native_pivot_xy':item['pivot'],
                    'local_xy_unity_units':local, 'parts':list(item['parts'])})
    sprites=[]
    for sorting,name in enumerate(contract['rig_parts_back_to_front']):
        owner=assigned[name]
        pivot=joints[owner]['pivot']
        local=vec_pixel_to_unity(width//2-pivot[0],height//2-pivot[1],ppu)
        sprites.append({'part_id':name, 'bone':owner,'sort_order':sorting,
                        'filename':'layers/'+name+'.png',
                        'sprite_pivot_normalized':[0.5,0.5],
                        'local_xy_unity_units':local})
    if len(sprites)!=21 or len(rig)!=16:
        raise ValueError('Missing semantic slots or joints')
    ordered={p['part_id']:p['sort_order'] for p in sprites}
    if ordered['accessory_front'] <= ordered['hand_r']:
        raise ValueError('C03 bag is not in front of covered hand')
    # Rest-pose roundtrip must reconstruct the pixel-center world coordinates,
    # otherwise even a correct native sprite would be visually displaced.
    for part in sprites:
        owner_pivot=joints[part['bone']]['pivot']
        actual=[owner_pivot[0]+part['local_xy_unity_units'][0]*ppu,
                owner_pivot[1]-part['local_xy_unity_units'][1]*ppu]
        if any(abs(a-b)>0.00001 for a,b in zip(actual,(width/2,height/2))):
            raise ValueError('Full-canvas pixel sprite would not align in bind pose')
    return {
        'schema':'startup-life.c03.unity-rig-authoring-manifest.v1',
        'status':'DRAFT_EDITOR_HANDOFF_NOT_PRODUCTION_APPROVED',
        'character_id':'C03', 'view':'front',
        'owner_native_direction_approved':True,
        'native_art_packet_sha256':source['packet_sha256'],
        'pixels_per_unit':ppu,'native_grid':[width,height],
        'unity_coordinate_rule':'x right, y up; top-left native pixels converted relative to root',
        'rig_root_native_pixel':root,
        'full_canvas_sprite_center_xy':[width//2,height//2],
        'sprite_import_requirements':{
            'sprite_type':'Single','filter_mode':'Point',
            'mipmaps':False,'compression':'Uncompressed',
            'alpha_is_transparency':True,'pivot':'Center',
            'full_canvas_untrimmed_required':True
        },
        'joints':rig,'sprite_renderers':sprites,
        'bag_body_mask':'layers/masks/bag_body.png',
        'rules':[
            'Create authored hierarchy in a LIVE Unity Editor after import capability checks',
            'No .meta direct edits; sprite importer owns pivots/rects',
            'Do not bind draft layer sprites into production game scenes',
            'Verify actual screen sorting/accessory_front in front of hand_r',
            'Do not delete covered hand pixels or change identity of approved image',
            'Test actual rotation and phone BeautyCell before Golden registry approval'
        ],
        'rejected_legacy_idle':'DO_NOT_BIND_OR_INTEGRATE',
        'idle_v2_keypose_contract':'C03-idle-v2-art-direction.json',
        'idle_motion_clip_ready':False,
        'owner_idle_animation_verdict':'REJECTED',
        'unity_import_and_animation':'NOT_RUN',
        'owner_final_golden_approval':False
    }


def export(output_path, joint_path=PLAN, contract_path=CONTRACT, source_path=SOURCE):
    plan=json.loads(Path(joint_path).read_text(encoding='utf-8'))
    contract=json.loads(Path(contract_path).read_text(encoding='utf-8'))
    source=json.loads(Path(source_path).read_text(encoding='utf-8'))
    doc=generate(plan,contract,source)
    target=Path(output_path); target.parent.mkdir(parents=True,exist_ok=True)
    body=json.dumps(doc,indent=2,ensure_ascii=False)+'\n'
    target.write_text(body,encoding='utf-8')
    return {'status':doc['status'],'joints':len(doc['joints']),
            'semantic_renderers':len(doc['sprite_renderers']),
            'sha256':hashlib.sha256(body.encode('utf-8')).hexdigest()}


if __name__=='__main__':
    cli=argparse.ArgumentParser()
    cli.add_argument('--output',type=Path,required=True)
    args=cli.parse_args()
    print(json.dumps(export(args.output),indent=2))
