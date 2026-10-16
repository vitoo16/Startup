#!/usr/bin/env python3
"""Strict, engine-free native front A/B pose evidence; no owner art signoff."""
import importlib.util
import json
import sys
import tempfile
from pathlib import Path

ROOT=Path(__file__).resolve().parents[1]
S=ROOT/'scripts';P=ROOT/'Assets/StartupLife/Art/Source/Characters/golden-character-c03'

def load(name):
    sys.path.insert(0,str(S))
    spec=importlib.util.spec_from_file_location(name,S/(name+'.py'))
    obj=importlib.util.module_from_spec(spec)
    spec.loader.exec_module(obj)
    return obj

def test():
    renderer=load('Render-C03IdlePoseAB')
    source=load('Render-GoldenCharacterC03R1')
    decoder=load('Validate-GoldenCharacterC03R1').decode_rgba_png
    original=json.loads((P/'r5-1-personality-front-review.json').read_text(encoding='utf8'))
    with tempfile.TemporaryDirectory(prefix='c03-idle-') as temp:
        report=renderer.output(temp)
        root=Path(temp)
        assert report['status']=='A_AND_B_AWAITING_OWNER_REVIEW'
        assert report['chosen'] is None
        original_img,original_parts=source.paint(original)
        for key in ('A','B'):
            draft=json.loads((P/f'C03-IdlePose-{key}.json').read_text(encoding='utf8'))
            assert draft['view']=='front' and draft['palette']==original['palette']
            assert draft['parts'].index('hand_r')<draft['parts'].index('accessory_front')
            assert all(not draft[k] for k in ('owner_approved','ip_cleared','unity_integrated'))
            rgba,parts=source.paint(draft)
            for part in ('head','face','hair_front','hair_back'):
                assert bytes(parts[part])==bytes(original_parts[part]),part
            assert len(parts)==21 and all(any(v[3::4]) for v in parts.values())
            def pix(name):
                return {i for i in range(0,len(rgba),4) if parts[name][i+3]}
            assert pix('upper_arm_r')&pix('forearm_r'), 'Disconnected elbow'
            assert pix('forearm_r')&pix('hand_r'), 'Disconnected wrist'
            assert min(p//4//64 for p in pix('hand_r'))>=60, 'Palm over chest'
            assert len(pix('hand_r')&(pix('accessory_front')|pix('accessory_back')))<=2, 'Hand merges with bag'
            w,h,native=decoder((root/f'C03-IdlePose-{key}-1x.png').read_bytes())
            ew,eh,big=decoder((root/f'C03-IdlePose-{key}-8x.png').read_bytes())
            assert (w,h,ew,eh)==(64,80,640,768)
            assert native==rgba and set(native[3::4])<={0,255}
            neutral=bytes((244,239,229,255))
            for y in range(80):
                for x in range(64):
                    i=(y*64+x)*4;px=native[i:i+4]
                    correct=px if px[3] else neutral
                    for dy in range(8):
                        for dx in range(8):
                            j=(((y+8)*8+dy)*ew+(x+8)*8+dx)*4
                            assert big[j:j+4]==correct,'Not pixel exact NN 8x'
            assert report['candidates'][key]['heads_tall']==2.25
        print('PASS both native 64x80 sprites, frozen head/hair/face, connected arm/wrist, hand below chest, minimal bag overlap')
        print('PASS 21 semantic layers, 0/255 alpha, 2.25 heads, exact 8x nearest neighbor across every native pixel')
        print('NOT RUN owner art verdict, independent IP, Unity sprite import, articulated rig, on-device BeautyCell')

if __name__=='__main__':
    try:test()
    except (AssertionError,KeyError,ValueError,OSError) as exc:
        print('FAIL C03 IdlePose:',repr(exc),file=sys.stderr)
        raise SystemExit(1)
