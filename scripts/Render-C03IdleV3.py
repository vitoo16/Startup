#!/usr/bin/env python3
"""C03 Idle V3: frozen body, authored native eyelid poses, true-timed review.

Offline review exporter, not a Unity importer or a production scheduler.
Reuses the existing C03 pixel source and V2 closed-eye compiler.
"""
import argparse
import hashlib
import importlib.util
import json
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SCRIPTS = ROOT / 'scripts'
ART = ROOT / 'Assets/StartupLife/Art/Source/Characters/golden-character-c03'
SOURCE = ART / 'C03-native-front-source.json'
OLD_POSES = ART / 'C03-idle-v2-keyposes-DRAFT.json'
OLD_RULES = ART / 'C03-idle-v2-art-direction.json'
V3 = ART / 'C03-idle-v3-stable-blink-DRAFT.json'
W, H = 128, 160
REST_RGBA_SHA = 'efda42e83eb039d31e6ff3cd25dbf68db8d4f65cdc34beab842ee90be96ebe1d'
CLOSED_RGBA_SHA = '1faf1b8c78e078dd3aed386e22a7932eea5b19d3836bebcb260ae62a3ebd853b'


def load(name):
    sys.path.insert(0, str(SCRIPTS))
    spec = importlib.util.spec_from_file_location(name, SCRIPTS / (name + '.py'))
    result = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(result)
    return result


def pixel_diffs(a, b):
    return [(i % W, i // W) for i in range(W * H)
            if a[i*4:i*4+4] != b[i*4:i*4+4]]


def assemble(manifest, v3, native_layers, closed_layers):
    """Only the face pixel layer is replaceable. Source validation fails closed."""
    if (v3.get('schema') != 'startup-life.c03.idle-v3-stable-blink.v1' or
        v3.get('status') != 'IDLE_V3_BLINK_PLAYBACK_AWAITING_OWNER_REVIEW' or
        v3.get('owner_verdict') != 'REJECT_INHALE_MOTION' or
        v3.get('native_canvas') != [W, H] or
        v3.get('native_pixel_packet_sha256') != manifest['packet_sha256'] or
        v3.get('rest_source') != 'C03-native-front-source.json' or
        v3.get('closed_source') != 'C03-idle-v2-keyposes-DRAFT.json' or
        v3.get('closed_source_pose') != 'blink' or
        v3.get('eye_roi_xyxy') != [48, 47, 83, 52] or
        v3.get('active_pose_order') != ['open', 'half', 'closed', 'half', 'open'] or
        v3.get('rejected_sources_archive_only') !=
          ['C03-idle-v2.1-inhale-contour-DRAFT.json', 'scripts/Preview-C03IdleLoop.py'] or
        not v3.get('constraints', {}).get('no_inhale') or
        v3.get('gate', {}).get('approved_registry') is not False):
        raise ValueError('C03 V3 source, rejected Inhale or review gate drift')
    if set(native_layers) != set(manifest['part_order']) or set(closed_layers) != set(native_layers):
        raise ValueError('Incomplete C03 native layer rig')
    if v3.get('frozen_parts') != [p for p in manifest['part_order'] if p != 'face']:
        raise ValueError('Frozen 20-layer authoring contract was weakened')
    if any(native_layers[p] != closed_layers[p] for p in v3['frozen_parts']):
        raise ValueError('Inherited V2 blink modifies a frozen layer')
    face = bytearray(native_layers['face'])
    mutated = set()
    x0, y0, x1, y1 = v3['eye_roi_xyxy']
    for row in v3['half_close_edits']:
        if (len(row) != 4 or row[0] != 'erase_iris' or
            any(type(n) is not int for n in row[1:])):
            raise ValueError('Only explicitly authored eye iris pixel erasure is allowed')
        _, y, x, length = row
        if y < y0 or y >= y1 or x < x0 or x + length > x1 or length < 1:
            raise ValueError('Half blink edit outside frozen eye ROI')
        for px in range(x, x + length):
            if (px, y) in mutated:
                raise ValueError('Duplicate half-blink pixel mutation')
            mutated.add((px, y))
            position = (y * W + px) * 4
            if face[position:position+4] != bytes.fromhex('34343bff'):
                raise ValueError('Half-blink can only cover existing ink iris pixels')
            if native_layers['head'][position+3] != 255:
                raise ValueError('Original head skin must already exist behind the eye')
            face[position:position+4] = bytes(4)
    if not 12 <= len(mutated) <= v3['half_max_changed_pixels']:
        raise ValueError('Half-closed eye edit budget violated')
    half = dict(native_layers)
    half['face'] = bytes(face)
    return {'open': native_layers, 'half': half, 'closed': closed_layers}


def quality(painter, checker, manifest, v3, frames, mask):
    rest = painter.composite(frames['open'])
    closed = painter.composite(frames['closed'])
    if hashlib.sha256(rest).hexdigest() != REST_RGBA_SHA:
        raise ValueError('Rest native identity mismatches frozen source')
    if hashlib.sha256(closed).hexdigest() != CLOSED_RGBA_SHA:
        raise ValueError('Closed eyes were not inherited from frozen V2')
    eye_box = v3['eye_roi_xyxy']
    statistics = {}
    for name, parts in frames.items():
        if any(parts[p] != frames['open'][p] for p in v3['frozen_parts']):
            raise ValueError('Body, head, hands, strap or feet modified during blink')
        rgba = painter.composite(parts)
        changes = pixel_diffs(rest, rgba)
        if any(not (eye_box[0] <= x < eye_box[2] and eye_box[1] <= y < eye_box[3])
               for x, y in changes):
            raise ValueError('A pixel outside the eye ROI changed')
        if name == 'half' and not 12 <= len(changes) <= v3['half_max_changed_pixels']:
            raise ValueError('Half-eye differences outside budget')
        if name == 'closed' and not 25 <= len(changes) <= v3['closed_max_changed_pixels']:
            raise ValueError('Closed-eye differences outside budget')
        if any(value not in (0, 255) for value in rgba[3::4]):
            raise ValueError('Antialiasing or nonbinary alpha in native frame')
        if rgba[3::4] != rest[3::4]:
            raise ValueError('Blink altered character alpha silhouette')
        if any(rgba[(y*W+x)*4+3] for x in range(W) for y in (0, H-1)) or any(
               rgba[(y*W+x)*4+3] for y in range(H) for x in (0, W-1)):
            raise ValueError('Native character touches the canvas boundary')
        bag = checker.inspect_masked_occlusion(
            parts['hand_r'], parts['accessory_front'], mask, canvas=(W, H),
            minimum_overlap=16, minimum_exposed_below=8)
        statistics[name] = {'changed_visible_pixels':len(changes),
                            'only_eye_roi_changed':True, **bag}
    if statistics['open']['changed_visible_pixels'] != 0:
        raise ValueError('The initial Rest frame was edited')
    return statistics


def playback_timing(v3):
    t = v3['timing_profile']
    closing, closed, opening = t['closing_ms'], t['closed_hold_ms'], t['reopening_ms']
    if not (40 <= closing <= 70 and 40 <= closed <= 80 and 50 <= opening <= 90 and
            closing + closed + opening == t['total_blink_ms'] and
            t['playback_sample_rest_gaps_ms'] == [3000, 5000, 4000] and
            t['playback_only_gaps_not_production_schedule'] is True and
            t['blink_independent_of_other_actions'] is True and
            t['deterministic_gameplay_state_untouched'] is True and
            t['future_scheduler_rest_gap_range_ms'] == [2800, 6500]):
        raise ValueError('Unexpected blink timing / gameplay contract')
    stages = [('open', 3000), ('half', closing), ('closed', closed),
              ('half', opening), ('open', 5000), ('half', closing),
              ('closed', closed), ('half', opening), ('open', 4000)]
    if stages[0][0] != stages[-1][0] or any(s[0] not in ('open','half','closed')
                                              for s in stages):
        raise ValueError('Idle V3 playback is not a stable rest-bookended blink')
    return stages


def export(folder, source=SOURCE, old_poses=OLD_POSES, old_rules=OLD_RULES, v3path=V3):
    painter = load('Render-C03OwnerNativeFront')
    previous = load('Render-C03IdleV2Keyposes')
    checker = load('Validate-C03OwnerRigHandoff')
    manifest = painter.expand_source(source)
    layers, mask = painter.paint(manifest)
    old = json.loads(Path(old_poses).read_text(encoding='utf8'))
    rules = json.loads(Path(old_rules).read_text(encoding='utf8'))
    v3 = json.loads(Path(v3path).read_text(encoding='utf8'))
    inherited = previous.compile_poses(manifest, old, rules, layers)
    if inherited['rest'] != layers:
        raise ValueError('V2 Rest unexpectedly changed')
    frames = assemble(manifest, v3, layers, inherited['blink'])
    statistics = quality(painter, checker, manifest, v3, frames, mask)
    output = Path(folder)
    output.mkdir(parents=True, exist_ok=True)
    images = {}
    for name,parts in frames.items():
        raw = painter.composite(parts)
        images[name] = raw
        (output / f'C03-Idle-V3-{name}-1x.png').write_bytes(painter.png(raw, W, H))
    from PIL import Image, ImageDraw
    cream = (246, 242, 232, 255)
    sheet = Image.new('RGBA', (4*144,176), cream)
    for i,name in enumerate(('open','half','closed','open')):
        pic = Image.frombytes('RGBA',(W,H),images[name])
        sheet.alpha_composite(pic,(i*144+8,8))
    sheet.convert('RGB').resize((4*144*4,176*4),Image.Resampling.NEAREST).save(
        output/'C03-Idle-V3-Blink-Sheet-4x.png')
    eye = Image.new('RGBA',(3*64,48),cream)
    for i,name in enumerate(('open','half','closed')):
        pic=Image.frombytes('RGBA',(W,H),images[name]).crop((36,38,92,62))
        eye.alpha_composite(pic,(i*64+4,12))
    eye.convert('RGB').resize((1920,480),Image.Resampling.NEAREST).save(
        output/'C03-Idle-V3-Eyes-10x.png')
    # Mock 360x640 portrait composition. Not a Unity BeautyCell.
    phone = Image.new('RGBA',(360,640),(235,227,207,255))
    canvas = Image.frombytes('RGBA',(W,H),images['open']).resize((256,320),Image.Resampling.NEAREST)
    phone.alpha_composite(canvas,(52,205))
    ImageDraw.Draw(phone).line((0,511,360,511),fill=(188,178,161,255),width=2)
    phone.convert('RGB').save(output/'C03-Idle-V3-Gameplay-Scale-MOCK.png')
    stages = playback_timing(v3)
    gif_frames = []
    for name,_ in stages:
        image = Image.new('RGBA',(W,H),cream)
        image.alpha_composite(Image.frombytes('RGBA',(W,H),images[name]))
        gif_frames.append(image.convert('RGB').resize((512,640),Image.Resampling.NEAREST))
    durations = [ms for _,ms in stages]
    gif_frames[0].save(output/'C03-Idle-V3-Timed-Playback.gif',save_all=True,
                       append_images=gif_frames[1:],duration=durations,
                       disposal=2,loop=0,optimize=False)
    with Image.open(output/'C03-Idle-V3-Timed-Playback.gif') as animated:
        actual=[animated.seek(i) or animated.info['duration'] for i in range(animated.n_frames)]
    if actual != durations:
        raise ValueError('Exported GIF playback durations do not match contract')
    evidence={'status':'IDLE_V3_BLINK_PLAYBACK_AWAITING_OWNER_REVIEW',
              'owner_approval':False,'owner_verdict_for_inhale':'REJECT_INHALE_MOTION',
              'native_grid':[W,H], 'frames':statistics,
              'playback_timing_ms':durations,'playback_total_ms':sum(actual),
              'blink_count_in_sample':2,'rest_open_equals_close':True,
              'rest_sha256':hashlib.sha256(images['open']).hexdigest(),
              'closed_sha256':hashlib.sha256(images['closed']).hexdigest(),
              'legacy_inhale_used':False,'unity_editor':'NOT_RUN',
              'device_beautycell':'NOT_RUN','independent_ip':'NOT_RUN',
              'approved_registry':'UNCHANGED','no_gameplay_save_changes':True,
              'output_sha256':{p.name:hashlib.sha256(p.read_bytes()).hexdigest()
                                for p in sorted(output.iterdir()) if p.is_file()}}
    (output/'C03-Idle-V3-QA.json').write_text(json.dumps(evidence,indent=2)+'\n',encoding='utf8')
    return evidence


if __name__=='__main__':
    ap=argparse.ArgumentParser()
    ap.add_argument('--output-dir',type=Path,required=True)
    args=ap.parse_args()
    result=export(args.output_dir)
    print(json.dumps({'status':result['status'],'timed_playback_ms':result['playback_total_ms'],
                      'frame_pixel_changes':{name:stats['changed_visible_pixels']
                                             for name,stats in result['frames'].items()}},indent=2))
