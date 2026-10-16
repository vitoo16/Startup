#!/usr/bin/env python3
"""Native three-stage R5.1 review validation; never Golden/Unity/IP signoff."""
import importlib.util
import json
import sys
import tempfile
from pathlib import Path

S=Path(__file__).resolve().parent
A=S.parent/'Assets/StartupLife/Art/Source/Characters/golden-character-c03'

def module(name):
    sys.path.insert(0,str(S))
    spec=importlib.util.spec_from_file_location(name,S/(name+'.py'))
    result=importlib.util.module_from_spec(spec)
    spec.loader.exec_module(result)
    return result

def validate():
    render=module('Render-GoldenCharacterC03R5_1')
    prior=json.loads((A/'r5-personality-front-review.json').read_text(encoding='utf8'))
    now=json.loads((A/'r5-1-personality-front-review.json').read_text(encoding='utf8'))
    decode=module('Validate-GoldenCharacterC03R1').decode_rgba_png
    painter=module('Render-GoldenCharacterC03R1')
    assert now['status']=='DRAFT_AWAITING_OWNER_REVIEW' and now['view']=='front'
    assert now['parts']==prior['parts'] and now['palette']==prior['palette']
    assert [c for c in now['commands'] if c[1]=='face']==[c for c in prior['commands'] if c[1]=='face']
    assert not any(now[k] for k in ('owner_approved','ip_cleared','unity_integrated'))
    px,layers=painter.paint(now)
    assert len(layers)==21 and set(px[3::4])<=set((0,255))
    assert all(any(layer[3::4]) for layer in layers.values())
    with tempfile.TemporaryDirectory(prefix='c03-r5-1-') as tmp:
        out=Path(tmp);r=render.render(out)
        for name in ('silhouette','face-hair','full-character'):
            ow,oh,native=decode((out/f'C03-R5.1-{name}-1x.png').read_bytes())
            sw,sh,enlarged=decode((out/f'C03-R5.1-{name}-8x.png').read_bytes())
            assert (ow,oh,sw,sh)==(64,80,640,768)
            assert set(native[3::4])<=set((0,255)) and any(native[3::4])
            bg=bytes((244,239,229,255))
            for y in range(80):
                for x in range(64):
                    at=(y*64+x)*4;p=native[at:at+4]
                    expected=p if p[3] else bg
                    for dy in range(8):
                        for dx in range(8):
                            i=(((y+8)*8+dy)*sw+(x+8)*8+dx)*4
                            assert enlarged[i:i+4] == expected,'Non-exact native NN pixels'
        assert r['heads_tall']==2.25 and r['head_fraction_pct']==44.44
        assert r['semantic_names']==21 and r['owner_verdict']=='AWAITING_OWNER_REVIEW'
        assert r['changed_occupied_fraction'] < 0.20, 'Accidental R6-level redesign'
    print('PASS native 64x80, 21 named layers, 0/255 alpha, exact 8x, R5 face/palette unchanged, 2.25 heads')
    print('NOT RUN: owner art approval, rig motion, Unity/device, independent copyright/IP')

if __name__=='__main__':
    try:validate()
    except (AssertionError,KeyError,ValueError,OSError) as exc:
        print('FAIL R5.1:',repr(exc),file=sys.stderr);sys.exit(1)
