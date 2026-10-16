#!/usr/bin/env python3
"""16-frame C03 FRONT idle-joint proof on existing native RGBA layers.

Authoring/QA only: no new sprite art, no game prefab, and NO Unity validation.
All transforms and compositing reuse Preview-C03FrontRig + native renderer.
"""
import argparse
import hashlib
import importlib.util
import json
import math
import sys
from pathlib import Path

HERE = Path(__file__).resolve().parent
ROOT = HERE.parent
ART = ROOT / 'Assets/StartupLife/Art/Source/Characters/golden-character-c03'
NATIVE = ART / 'C03-native-front-source.json'
JOINTS = ART / 'C03-front-joint-plan-DRAFT.json'
CONTRACT = ART / 'C03-owner-front-rig-contract.json'


def load_tool(name):
    sys.path.insert(0, str(HERE))
    spec = importlib.util.spec_from_file_location(name, HERE / (name + '.py'))
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def interpolate_angles(left, right, index, count):
    if count < 4 or count % 2:
        raise ValueError('The debug loop requires an even sampling count >= 4')
    if index < 0 or index >= count:
        raise ValueError('Loop sample is out of bounds')
    wave = math.cos(2 * math.pi * index / count)
    return {name: (left.get(name, 0) + right.get(name, 0)) / 2 +
            (left.get(name, 0) - right.get(name, 0)) / 2 * wave
            for name in sorted(set(left) | set(right))}


def local_contact(first, second, pivot, width, height, radius, max_gap):
    """Check alpha support from *both* native bones around their shared pivot.

    Chebyshev distance <= 1 admits touching corners, preventing torn joints.
    Checks two separate source layers; a torso-colored overlay cannot fake
    a correctly articulated upper-arm/forearm contact.
    """
    px, py = pivot
    x0 = max(0, math.floor(px - radius))
    x1 = min(width, math.ceil(px + radius) + 1)
    y0 = max(0, math.floor(py - radius))
    y1 = min(height, math.ceil(py + radius) + 1)
    a = {(x, y) for y in range(y0, y1) for x in range(x0, x1)
         if first[(y * width + x) * 4 + 3]}
    b = {(x, y) for y in range(y0, y1) for x in range(x0, x1)
         if second[(y * width + x) * 4 + 3]}
    if not a or not b:
        return False
    for x, y in a:
        for dx in range(-max_gap, max_gap + 1):
            for dy in range(-max_gap, max_gap + 1):
                if (x + dx, y + dy) in b:
                    return True
    return False


def inspect_connections(qa, joints, assigned, posed, world, width=128, height=160):
    radius = qa['local_contact_radius_px']
    gap = qa['max_local_contact_gap_px']
    if type(radius) is not int or type(gap) is not int or radius < 1 or radius > 20 or gap != 1:
        raise ValueError('Invalid owner-rig contact budget; only 1 native pixel allowed')
    links = qa['connections']
    if len(links) != 8 or len({tuple(pair) for pair in links}) != 8:
        raise ValueError('Expected eight unique arm/leg joint connections')
    for item in links:
        if len(item) != 3:
            raise ValueError('Invalid joint connection')
        parent_part, child_part, joint_name = item
        if (parent_part not in posed or child_part not in posed or
            joint_name not in joints or joint_name == 'root' or
            assigned[child_part] != joint_name or
            joints[joint_name]['parent'] != assigned[parent_part]):
            raise ValueError('Incorrect actual joint-to-layer ancestry')
        if not local_contact(posed[parent_part], posed[child_part],
                             world[joint_name][:2], width, height, radius, gap):
            raise ValueError('Native seam or missing joint pixels at '+joint_name)
    return len(links)


def verify_frame_bounds(frame, width=128, height=160):
    """Detect clipping at the sprite canvas perimeter, not merely changes in bbox."""
    expected = width * height * 4
    if len(frame) != expected:
        raise ValueError('Wrong output native frame length')
    pixels = [(x, y) for y in range(height) for x in range(width)
              if frame[(y * width + x) * 4 + 3]]
    if not pixels:
        raise ValueError('Empty articulated frame')
    bbox = [min(x for x, y in pixels), min(y for x, y in pixels),
            max(x for x, y in pixels) + 1, max(y for x, y in pixels) + 1]
    if bbox[0] <= 0 or bbox[1] <= 0 or bbox[2] >= width or bbox[3] >= height:
        raise ValueError('Animated frame touches native canvas boundary')
    return bbox


def pixel_difference(first, second):
    if len(first) != len(second) or len(first) % 4:
        raise ValueError('Cannot compare differently sized RGBA frames')
    return sum(first[i:i+4] != second[i:i+4] for i in range(0, len(first), 4))


def nearest_scale(rgba, width, height, scale):
    if scale != 4:
        raise ValueError('Debug review scale fixed to exact 4x NN')
    expanded = bytearray(width * scale * height * scale * 4)
    for y in range(height):
        for dy in range(scale):
            out_row = (y * scale + dy) * width * scale * 4
            source_row = y * width * 4
            for x in range(width):
                color = rgba[source_row + x*4:source_row + x*4+4]
                start = out_row + x * scale * 4
                expanded[start:start+scale*4] = color*scale
    return bytes(expanded)


def frames_sheet(frames, width=128, height=160):
    if len(frames) != 16:
        raise ValueError('Expected exactly 16 independently rendered frames')
    columns = 4
    gap = 8
    step_x, step_y = width + 2*gap, height + 2*gap
    sheet_width, sheet_height = columns*step_x, 4*step_y
    cream = bytes((246,242,232,255))
    canvas = bytearray(cream*(sheet_width*sheet_height))
    for number, frame in enumerate(frames):
        x0=(number%columns)*step_x+gap
        y0=(number//columns)*step_y+gap
        for y in range(height):
            for x in range(width):
                pos=(y*width+x)*4
                if frame[pos+3]:
                    dest=((y0+y)*sheet_width+(x0+x))*4
                    canvas[dest:dest+4]=frame[pos:pos+4]
    return bytes(canvas), sheet_width, sheet_height


def render(output_dir, native=NATIVE, joint_plan=JOINTS, contract_path=CONTRACT, export_gif=False):
    motion=load_tool('Preview-C03FrontRig')
    painter=load_tool('Render-C03OwnerNativeFront')
    overlay=load_tool('Validate-C03OwnerRigHandoff')
    source=painter.expand_source(native)
    plan=json.loads(Path(joint_plan).read_text(encoding='utf-8'))
    contract=json.loads(Path(contract_path).read_text(encoding='utf-8'))
    joints, assigned=motion.read_plan(plan, contract, source)
    qa=plan['idle_loop_proof']
    count=qa['sample_count']
    if count != 16 or qa['status'] != 'ENGINE_FREE_DEBUG_ONLY_NOT_UNITY_ANIMATION':
        raise ValueError('Unexpected rig debug loop contract')
    layers,bag_mask=painter.paint(source)
    left,right=(plan['poses'][1]['degrees'],plan['poses'][2]['degrees'])
    frames=[]
    metrics=[]
    root=joints['root']['pivot']
    out=Path(output_dir)
    out.mkdir(parents=True,exist_ok=True)
    for i in range(count):
        angles=interpolate_angles(left,right,i,count)
        if any(abs(value)>8 for value in angles.values()):
            raise ValueError('Idle debug angles exceed joint-plan envelope')
        world=motion.world_joints(joints,angles)
        if tuple(world['root'][:2]) != tuple(root):
            raise ValueError('Root sliding, not true joint movement')
        parts,mask=motion.posed_layers(layers,bag_mask,joints,assigned,angles)
        pairs=inspect_connections(qa,joints,assigned,parts,world)
        bag=overlay.inspect_masked_occlusion(
            parts[contract['occlusion']['hand_part_id']],
            parts[contract['occlusion']['covering_part_id']],mask,
            canvas=(128,160),
            minimum_overlap=contract['occlusion']['minimum_covered_native_px'],
            minimum_exposed_below=contract['occlusion']['minimum_visible_below_native_px'])
        frame=painter.composite(parts)
        bbox=verify_frame_bounds(frame)
        file=out/f'C03-idle-{i:02d}-native-DRAFT.png'
        png=painter.png(frame,128,160)
        file.write_bytes(png)
        frames.append(frame)
        metrics.append({'frame':i,'bbox':bbox,'seam_pairs_checked':pairs,
                        'degrees':angles, **bag,
                        'png_sha256':hashlib.sha256(png).hexdigest()})
    steps=[pixel_difference(frames[i],frames[(i+1)%count]) for i in range(count)]
    if min(steps) <= 0 or pixel_difference(frames[0],frames[count//2]) <= 0:
        raise ValueError('Idle loop has static/duplicated adjacent frames')
    sheet,w,h=frames_sheet(frames)
    sheet_1=painter.png(sheet,w,h)
    sheet_4=painter.png(nearest_scale(sheet,w,h,4),w*4,h*4)
    (out/'C03-idle-loop-sheet-1x-DRAFT.png').write_bytes(sheet_1)
    (out/'C03-idle-loop-sheet-4x-DRAFT.png').write_bytes(sheet_4)
    if export_gif:
        try:
            from PIL import Image
        except ImportError as exc:
            raise ValueError('Optional animated GIF needs Pillow on an art workstation') from exc
        images=[]
        background=Image.new('RGBA',(128,160),(246,242,232,255))
        for frame in frames:
            picture=background.copy()
            picture.alpha_composite(Image.frombytes('RGBA',(128,160),frame))
            images.append(picture.convert('RGB').resize((512,640),Image.Resampling.NEAREST))
        images[0].save(out/'C03-idle-16-frame-debug.gif',save_all=True,
                       append_images=images[1:],duration=100,loop=0,optimize=False)
    evidence={'stage':'C03_ENGINE_FREE_IDLE_MOTION_PROOF_NOT_UNITY',
        'native_canvas':[128,160],'source_packet_sha256':source['packet_sha256'],
        'sample_count':count,'joint_count':len(joints),
        'semantic_layer_count':len(layers),'contacts_checked_per_frame':8,
        'all_hand_bag_occlusion_checks_passed':True,
        'min_adjacent_changed_pixels':min(steps),
        'max_adjacent_changed_pixels':max(steps),
        'max_canvas_bbox_xyxy_exclusive':[
            min(m['bbox'][0] for m in metrics), min(m['bbox'][1] for m in metrics),
            max(m['bbox'][2] for m in metrics), max(m['bbox'][3] for m in metrics)],
        'frames':metrics,'adjacent_changed_pixels':steps,
        'unity_editor':'NOT_RUN','mobile_BeautyCell':'NOT_RUN',
        'independent_IP':'NOT_RUN','owner_final_golden_approval':False,
        'authoritative_reviews':{'sheet_1x_sha256':hashlib.sha256(sheet_1).hexdigest(),
                                'sheet_4x_sha256':hashlib.sha256(sheet_4).hexdigest()}}
    (out/'C03-idle-loop-technical-evidence.json').write_text(
        json.dumps(evidence,indent=2,ensure_ascii=False)+'\n',encoding='utf-8')
    return evidence


if __name__ == '__main__':
    p=argparse.ArgumentParser()
    p.add_argument('--output-dir',type=Path,required=True)
    p.add_argument('--gif',action='store_true',help='Optional Pillow-dependent GIF for inspection only')
    args=p.parse_args()
    result=render(args.output_dir,export_gif=args.gif)
    print(json.dumps({k:v for k,v in result.items() if k not in ('frames','adjacent_changed_pixels')},
                     indent=2,ensure_ascii=False))
