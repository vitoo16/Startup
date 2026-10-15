#!/usr/bin/env python3
"""Strict draft-only C03 pixel raster and RGBA layers validator."""
import importlib.util
import json
import sys
from pathlib import Path

ROOT=Path(__file__).resolve().parents[1]
MANIFEST=Path('Assets/StartupLife/Art/Source/Characters/golden-character-c03/native-draft.json')
RENDER=Path('scripts/Render-GoldenCharacterC03.py')
REGISTRY=Path('Assets/StartupLife/Art/Approved/registry.json')
PROVENANCE=Path('Assets/StartupLife/Art/_ArtDirection/provenance/manifest.json')

def verify(root=ROOT):
    root=Path(root)
    m=json.loads((root/MANIFEST).read_text(encoding='utf8'))
    registry=json.loads((root/REGISTRY).read_text(encoding='utf8'))
    prov=json.loads((root/PROVENANCE).read_text(encoding='utf8'))
    assert m['schema']=='startup-life.pixel-character-c03-native-draft.v1'
    assert m['status']=='DRAFT_NOT_APPROVED'
    assert m['style_id']==registry['style_id']=='vn_everyday_pixel_v1'
    assert m['native_canvas_px']==[64,80] and m['baseline_art_ppu']==32
    assert len({x['id'] for x in m['silhouettes']})==3
    assert m['candidate_variants']==9
    assert m['views']==['front','three_quarter_left','side','back']
    assert len(m['rig_parts'])==len(set(m['rig_parts']))==21
    assert m['expressions']==['neutral','soft_smile','focused','delighted']
    assert not any(m['visual_gates'].values()),'No auto art approval'
    assert registry['approved_entries']==[]
    matches=[a for a in prov['assets'] if a['asset_id']=='sl-character-golden-c03']
    assert len(matches)==1 and matches[0]['status']=='DRAFT'
    assert MANIFEST.as_posix() in matches[0]['source_files']
    assert matches[0]['unity_path'] is None and matches[0]['approval_date_utc'] is None
    spec=importlib.util.spec_from_file_location('c03_native',root/RENDER)
    module=importlib.util.module_from_spec(spec)
    sys.path.insert(0,str((root/'scripts').resolve()))
    spec.loader.exec_module(module)
    assert list(module.ORDER)==m['rig_parts']
    assert list(module.STYLES)==[x['id'] for x in m['silhouettes']]
    assert list(module.VIEWS)==m['views']
    shape_fingerprints=set()
    for style in module.STYLES:
        for view in module.VIEWS:
            image=module.draw(style,view)
            assert all(len(x)==64*80*4 for x in image.parts.values())
            pixels=image.merged()
            assert any(pixels[i+3] for i in range(0,len(pixels),4))
            if view=='front':
                alpha=bytes(pixels[i+3] for i in range(0,len(pixels),4))
                shape_fingerprints.add(alpha)
            if view=='back':
                # Facial pixels may not be authored on the rear projection.
                assert not any(image.parts['face'][i+3] for i in range(0,64*80*4,4))
    assert len(shape_fingerprints)==3, 'Recolors do not count as silhouettes'
    for style in module.STYLES:
        for tone in range(3):
            assert module.draw(style,'front',tone).merged()
    for e in m['expressions']:
        assert module.draw(module.STYLES[0],'front',0,e).merged()
    try:
        module.Sprite(module.PALETTES[0]).rect('face',63,2,3,4,'eye')
        raise AssertionError('Out-of-bounds pixel rect accepted')
    except ValueError:
        pass
    return 12

if __name__=='__main__':
    try:print('PASS C03 native pixel layers/quarantine:',verify(),'views + 9 front variants')
    except (AssertionError,KeyError,ValueError,OSError,TypeError) as err:
        print('FAIL C03:',err,file=sys.stderr);sys.exit(1)
