#!/usr/bin/env python3
"""Engine-free R3 integrity checks; intentionally not a Golden/style/IP approval."""
import importlib.util
import json
import sys
import tempfile
from pathlib import Path

ROOT=Path(__file__).resolve().parents[1]
SCRIPTS=ROOT/'scripts'
SRC=ROOT/'Assets/StartupLife/Art/Source/Characters/golden-character-c03/r3-front-draft.json'
PREV=ROOT/'Assets/StartupLife/Art/Source/Characters/golden-character-c03/r2-front-draft.json'


def load(filename):
    sys.path.insert(0,str(SCRIPTS))
    spec=importlib.util.spec_from_file_location(filename, SCRIPTS/(filename+'.py'))
    mod=importlib.util.module_from_spec(spec);spec.loader.exec_module(mod)
    return mod


def verify():
    render=load('Render-GoldenCharacterC03R3')
    painter=load('Render-GoldenCharacterC03R1')
    pngcheck=load('Validate-GoldenCharacterC03R1')
    src=json.loads(SRC.read_text(encoding='utf-8'))
    prev=json.loads(PREV.read_text(encoding='utf-8'))
    assert src['status']=='DRAFT_AWAITING_OWNER_REVIEW'
    assert src['view']=='front' and src['parts']==prev['parts']
    assert all(src[x] is False for x in ('owner_approved','ip_cleared','unity_integrated'))
    pixels,parts=painter.paint(src)
    assert len(parts)==21 and all(any(p[3::4]) for p in parts.values())
    assert set(pixels[3::4])<=set((0,255))
    with tempfile.TemporaryDirectory(prefix='c03-r3-check-') as output:
        report=render.produce(Path(output),SRC,PREV)
        native_w,native_h,native=pngcheck.decode_rgba_png((Path(output)/'C03-R3-native-1x.png').read_bytes())
        assert (native_w,native_h)==(64,80) and native==pixels
        enlarged_w,enlarged_h,large=pngcheck.decode_rgba_png((Path(output)/'C03-R3-review-8x.png').read_bytes())
        assert (enlarged_w,enlarged_h)==(640,768)
        bg=bytes((244,239,229,255))
        for y in range(80):
            for x in range(64):
                pos=(y*64+x)*4; color=native[pos:pos+4]
                expected=color if color[3] else bg
                for dy in range(8):
                    for dx in range(8):
                        big=((((y+8)*8)+dy)*enlarged_w+((x+8)*8)+dx)*4
                        assert large[big:big+4]==expected, '8x is not pixel-exact NN'
        assert report['heads_tall']>=2.2 and report['heads_tall']<=2.4
        assert report['head_height_share']>=0.42 and report['head_height_share']<=0.45
        assert report['nonempty_semantic_layers']==21
        assert report['owner_gate']=='AWAITING_OWNER_REVIEW'
        print('PASS R3 true 64x80 RGBA, binary alpha, 21 rig parts, 8x NN and real bbox ratio.')
        print('NOT TESTED: Unity / articulated animation / device BeautyCell / independent IP.')
        print('AWAITING OWNER VISUAL DECISION: technical PASS is not art approval.')
    return True

if __name__=='__main__':
    try:verify()
    except (ValueError,KeyError,AssertionError,OSError) as ex:
        print('FAIL R3 technical verification:',str(ex),file=sys.stderr)
        raise SystemExit(1)
