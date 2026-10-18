#!/usr/bin/env python3
"""Stage authored native C03 V3 Blink sprites for a quarantined Unity Editor preview.

Offline only. No imported Unity assets, no .meta edits and no registry changes.
Depends on Pillow on the ART workstation, never Unity runtime.
"""
import argparse
import hashlib
import json
from pathlib import Path
from shutil import copyfile
from PIL import Image

PARTS=('hair_back','accessory_back','foot_l','foot_r','lower_leg_l','lower_leg_r',
 'upper_leg_l','upper_leg_r','pelvis','torso','upper_arm_l','upper_arm_r',
 'forearm_l','forearm_r','hand_l','hand_r','outfit_torso','head','face',
 'hair_front','accessory_front')
POSES=('open','half','closed')
EYE=(48,47,83,52)
PACKET='1b04022f5f828ba95374cc3e0e875df9597ea89454d3ebdd5730aa46a3254c1f'


def native(path):
    with Image.open(path) as image:
        if image.format!='PNG' or image.mode!='RGBA' or image.size!=(128,160):
            raise ValueError('Expected original 128x160 native RGBA PNG: '+str(path))
        frame=image.copy()
    if any(a not in (0,255) for a in frame.getchannel('A').tobytes()):
        raise ValueError('Antialiased pixel alpha in '+str(path))
    return frame


def prepare(native_layers, v3_frames, rig_json, dest):
    native_layers,v3_frames,rig_json,dest=map(Path,(native_layers,v3_frames,rig_json,dest))
    if dest.exists():
        raise FileExistsError('Refuse overwriting a previous C03 quarantined import: '+str(dest))
    rig=json.loads(rig_json.read_text(encoding='utf8'))
    if (rig.get('schema')!='startup-life.c03.unity-rig-authoring-manifest.v1'
        or rig.get('status')!='DRAFT_EDITOR_HANDOFF_NOT_PRODUCTION_APPROVED'
        or rig.get('native_grid')!=[128,160] or rig.get('pixels_per_unit')!=32
        or rig.get('owner_final_golden_approval') is not False
        or len(rig.get('joints',[]))!=16 or len(rig.get('sprite_renderers',[]))!=21):
        raise ValueError('C03 Editor rig is not an isolated 16-joint / 21-layer preview')
    slots={part['part_id']:part for part in rig['sprite_renderers']}
    if (set(slots)!=set(PARTS) or len(set(s['sort_order'] for s in slots.values()))!=21
        or slots['accessory_front']['sort_order']<=slots['hand_r']['sort_order']):
        raise ValueError('Bad semantic rig sorting or parts')
    layers={part:native(native_layers/(part+'.png')) for part in PARTS}
    poses={name:native(v3_frames/('C03-Idle-V3-'+name+'-1x.png')) for name in POSES}
    base=Image.new('RGBA',(128,160))
    for part in sorted(PARTS,key=lambda p:slots[p]['sort_order']):
        base.alpha_composite(layers[part])
    if base.tobytes()!=poses['open'].tobytes():
        raise ValueError('Authored layered Rest and owner V3 Open pixels differ')
    faces={}
    for name in POSES:
        face=layers['face'].copy()
        for y in range(160):
            for x in range(128):
                target=poses[name].getpixel((x,y))
                if target==poses['open'].getpixel((x,y)):
                    continue
                if not(EYE[0]<=x<EYE[2] and EYE[1]<=y<EYE[3]):
                    raise ValueError('Unauthorized changes outside blink eye ROI')
                skin=layers['head'].getpixel((x,y))
                if skin[3]!=255:
                    raise ValueError('Closed eyelid must have authored head skin underneath')
                face.putpixel((x,y),(0,0,0,0) if target==skin else target)
        composed=Image.new('RGBA',(128,160))
        for part in sorted(PARTS,key=lambda p:slots[p]['sort_order']):
            composed.alpha_composite(face if part=='face' else layers[part])
        if composed.tobytes()!=poses[name].tobytes():
            raise ValueError('Face-only sprite cannot reproduce '+name+' native pose')
        faces[name]=face
    if faces['open'].tobytes()!=layers['face'].tobytes():
        raise ValueError('Open must preserve original face layer')
    dest.mkdir(parents=True)
    (dest/'layers').mkdir()
    for part in PARTS: copyfile(native_layers/(part+'.png'),dest/'layers'/(part+'.png'))
    for name in POSES: faces[name].save(dest/('face_'+name+'.png'))
    (dest/'C03-unity-rig-authoring-manifest.json').write_bytes(rig_json.read_bytes())
    files=[{'path':p.relative_to(dest).as_posix(),
            'sha256':hashlib.sha256(p.read_bytes()).hexdigest()}
           for p in sorted(dest.rglob('*.png'))]
    result={
     'schema':'startup-life.c03.idle-v3.unity-quarantine-input.v1',
     'status':'OFFLINE_STAGING_EDITOR_IMPORT_PENDING',
     'source_packet_sha256':PACKET,'native_grid':[128,160],
     'pixels_per_unit':32,'rest_pixel_identical':True,
     'blink_only_face_layer':True,
     'frame_changed_visible_pixels':{'open':0,'half':19,'closed':35},
     'owner_motion_direction':'APPROVE_MOTION_DIRECTION',
     'owner_golden_asset_approved':False,
     'production_registry_approved':False,
     'unity_editor':'NOT_RUN','files':files}
    (dest/'C03-quarantine-input.json').write_text(
        json.dumps(result,indent=2)+'\n',encoding='utf8')
    return result


if __name__=='__main__':
    p=argparse.ArgumentParser()
    p.add_argument('--native-layers',type=Path,required=True)
    p.add_argument('--v3-frames',type=Path,required=True)
    p.add_argument('--rig-manifest',type=Path,required=True)
    p.add_argument('--output',type=Path,required=True)
    a=p.parse_args()
    print(json.dumps(prepare(a.native_layers,a.v3_frames,a.rig_manifest,a.output),indent=2))
