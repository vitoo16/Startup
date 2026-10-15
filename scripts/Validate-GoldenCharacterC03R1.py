#!/usr/bin/env python3
"""Engine-free structural QA only. Passing is NEVER art approval."""
import argparse
import importlib.util
import json
import struct
import sys
import zlib
from pathlib import Path


def decode_rgba_png(binary):
    if not binary.startswith(b'\x89PNG\r\n\x1a\n'):
        raise ValueError('Not PNG')
    at=8;compressed=[]; width=height=None
    while at<len(binary):
        length=struct.unpack_from('>I',binary,at)[0]; at+=4
        kind=binary[at:at+4]; at+=4
        payload=binary[at:at+length]; at+=length+4
        if kind==b'IHDR':
            width,height,depth,color,*_=struct.unpack('>IIBBBBB',payload)
            if depth!=8 or color!=6:raise ValueError('Expected 8-bit RGBA PNG')
        elif kind==b'IDAT':compressed.append(payload)
        elif kind==b'IEND':break
    data=zlib.decompress(b''.join(compressed));stride=width*4+1
    if len(data)!=stride*height:raise ValueError('Invalid pixel length')
    if any(data[y*stride]!=0 for y in range(height)):raise ValueError('Unexpected PNG filter')
    return width,height,b''.join(data[y*stride+1:(y+1)*stride] for y in range(height))


def verify(source, renderer):
    spec=importlib.util.spec_from_file_location('c03_r1',renderer)
    sys.path.insert(0,str(renderer.parent))
    module=importlib.util.module_from_spec(spec);spec.loader.exec_module(module)
    draft=json.loads(Path(source).read_text(encoding='utf8'))
    if not(draft['status']=='DRAFT_AWAITING_OWNER_REVIEW'
           and draft['owner_approved'] is False
           and draft['ip_cleared'] is False
           and draft['unity_integrated'] is False):
        raise AssertionError('Draft cannot claim approval')
    pixels,layers=module.paint(draft)
    assert len(layers)==21 and all(any(layer[i+3] for i in range(0,len(layer),4))
                                   for layer in layers.values())
    assert len(pixels)==64*80*4
    w,h,native=decode_rgba_png(module.png(pixels,64,80))
    assert (w,h)==(64,80) and native==pixels
    w,h,sheet=decode_rgba_png(module.preview(pixels,64,80,4))
    assert (w,h)==(320,384)
    backdrop=b'\xf4\xef\xe5\xff'
    for y in range(80):
        for x in range(64):
            source_offset=(y*64+x)*4
            color=pixels[source_offset:source_offset+4]
            expected=color if color[3] else backdrop
            for dy in range(4):
                for dx in range(4):
                    offset=(((y+8)*4+dy)*w+(x+8)*4+dx)*4
                    assert sheet[offset:offset+4]==expected, 'Non-NN preview pixel'
    print('PASS C03-R1 native pixels + 21 parts + integer NN preview; ART/UNITY/IP NOT APPROVED')


if __name__=='__main__':
    p=argparse.ArgumentParser()
    p.add_argument('--source',type=Path,required=True)
    p.add_argument('--renderer',type=Path,required=True)
    a=p.parse_args();verify(a.source,a.renderer)
