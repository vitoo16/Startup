#!/usr/bin/env python3
"""Structural-only C03-R4 tests: no owner/IP/Unity approval implied."""
import importlib.util
import json
import sys
import tempfile
from pathlib import Path

ROOT=Path(__file__).resolve().parents[1]
SCRIPTS=ROOT/'scripts'
SRC=ROOT/'Assets/StartupLife/Art/Source/Characters/golden-character-c03'


def module(name):
    sys.path.insert(0,str(SCRIPTS))
    spec=importlib.util.spec_from_file_location(name,SCRIPTS/(name+'.py'))
    obj=importlib.util.module_from_spec(spec);spec.loader.exec_module(obj)
    return obj


def check():
    r4=module('Render-GoldenCharacterC03R4')
    base=module('Render-GoldenCharacterC03R1')
    decoder=module('Validate-GoldenCharacterC03R1')
    draft=json.loads((SRC/'r4-front-draft.json').read_text(encoding='utf8'))
    prior=json.loads((SRC/'r3-front-draft.json').read_text(encoding='utf8'))
    assert all(draft[f] is False for f in ('owner_approved','ip_cleared','unity_integrated'))
    assert draft['view']=='front' and draft['parts']==prior['parts']
    assert draft['status']=='DRAFT_AWAITING_OWNER_REVIEW'
    for part in ('hair_front','hair_back','face'):
        commands=lambda data:[c for c in data['commands'] if c[1]==part]
        assert commands(prior)==commands(draft),part+' unintentionally changed'
    pixels,layers=base.paint(draft)
    assert len(layers)==21 and all(any(v[3::4]) for v in layers.values())
    assert set(pixels[3::4]) <= {0,255}
    with tempfile.TemporaryDirectory(prefix='c03-r4-') as folder:
        out=Path(folder)
        results=r4.export(out)
        w,h,native=decoder.decode_rgba_png((out/'C03-R4-native-1x.png').read_bytes())
        assert (w,h)==(64,80) and native==pixels
        nw,nh,scaled=decoder.decode_rgba_png((out/'C03-R4-review-8x.png').read_bytes())
        assert (nw,nh)==(640,768)
        background=bytes((244,239,229,255))
        for y in range(80):
            for x in range(64):
                offset=(y*64+x)*4;p=native[offset:offset+4]
                expected=p if p[3] else background
                for dy in range(8):
                    for dx in range(8):
                        at=(((y+8)*8+dy)*nw+(x+8)*8+dx)*4
                        assert scaled[at:at+4]==expected,'8x pixel mismatch'
        assert results['semantic_layer_count']==21
        assert 2.2<=results['actual_heads_tall']<=2.4
        assert 42<=results['head_percent_of_actual_height']<=45
        assert results['art_approval']=='AWAITING_OWNER_REVIEW'
        assert results['independent_IP']=='NOT_RUN'
    print('PASS R4 64x80 RGBA / binary alpha / 21 layer pixels / head proportions / 8x pixel exact NN')
    print('NOT TESTED: Unity animation, device BeautyCell, independent copyright/IP, owner Golden approval')


if __name__=='__main__':
    try:check()
    except (AssertionError,KeyError,ValueError,OSError) as exc:
        print('FAIL R4:',exc,file=sys.stderr)
        raise SystemExit(1)
