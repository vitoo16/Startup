#!/usr/bin/env python3
"""No-Golden-pass C03-R5 design-stage validation, never Unity or IP signoff."""
from pathlib import Path
import importlib.util, json, sys, tempfile
ROOT=Path(__file__).resolve().parents[1]
S=ROOT/'scripts';A=ROOT/'docs/evidence/production-art/source/c03-authoring'

def load(name):
    sys.path.insert(0,str(S))
    spec=importlib.util.spec_from_file_location(name,S/(name+'.py'))
    mod=importlib.util.module_from_spec(spec);spec.loader.exec_module(mod);return mod

def test():
    generate=load('Render-GoldenCharacterC03R5');decode=load('Validate-GoldenCharacterC03R1')
    spec=json.loads((A/'r5-personality-front-review.json').read_text(encoding='utf8'))
    assert spec['status']=='DRAFT_AWAITING_OWNER_REVIEW'
    assert spec['design_gates']['21_layer_animation']=='NOT_STARTED'
    assert spec['view']=='front' and all(not spec[k] for k in ('owner_approved','ip_cleared','unity_integrated'))
    with tempfile.TemporaryDirectory(prefix='c03-r5-') as temp:
        result=generate.export(temp)
        folder=Path(temp)
        for stage in ('silhouette','face-hair','full-character'):
            w,h,native=decode.decode_rgba_png((folder/('C03-R5-'+stage+'-1x.png')).read_bytes())
            sw,sh,scaled=decode.decode_rgba_png((folder/('C03-R5-'+stage+'-8x.png')).read_bytes())
            assert (w,h,sw,sh)==(64,80,640,768)
            assert set(native[3::4]) <= {0,255}
            assert any(native[3::4]), 'Empty stage'
            bg=b'\xf4\xef\xe5\xff'
            for y in range(80):
                for x in range(64):
                    k=(y*64+x)*4; rgba=native[k:k+4]
                    expected=rgba if rgba[3] else bg
                    for dy in range(8):
                        for dx in range(8):
                            at=((((y+8)*8)+dy)*sw+((x+8)*8)+dx)*4
                            assert scaled[at:at+4]==expected,'Not native NN 8x'
        assert 2.2 <= result['heads_tall'] <= 2.4
        assert 42 <= result['head_percent'] <= 45
        assert result['semantic_names_reserved']==21 and result['layer_animation_ready'] is False
        assert result['art_approval']=='NOT_GRANTED' and result['gate']=='AWAITING_OWNER_REVIEW'
    print('PASS R5: 3 original native stages; RGBA alpha binary; 8x exact NN; measured height ratio')
    print('NOT PASSED: owner Golden / Unity animation / device / IP; 21 rig-ready layers not yet reviewed')

if __name__=='__main__':
    try:test()
    except (KeyError,AssertionError,OSError,ValueError) as e:
        print('FAIL C03-R5:',repr(e),file=sys.stderr);raise SystemExit(1)
