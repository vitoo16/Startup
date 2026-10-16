#!/usr/bin/env python3
"""Strict source-of-truth check for the C03 owner-requested hand/bag PNG.

Checks exact image bytes after someone has committed the reference to Git.
Does not process, downsample, approve or register the image.
"""
import hashlib
import struct
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
PNG = ROOT / 'Assets/StartupLife/Art/Source/Characters/golden-character-c03/references/C03-hand-behind-bag-reference.png'
EXPECTED_SHA256 = 'd2e7e9a79672e23408c0a48626ef3968af84f5564f44e14eac41e4add38cb25e'
WIDTH, HEIGHT = 1171, 1343


def verify(path=PNG):
    path = Path(path)
    if not path.is_file():
        raise FileNotFoundError(
            'C03 image binary is not yet committed to the repository: ' + str(path))
    source=path.read_bytes()
    if hashlib.sha256(source).hexdigest() != EXPECTED_SHA256:
        raise ValueError('Image differs from exact owner-reviewed hand-behind-bag reference')
    if source[:8] != b'\x89PNG\r\n\x1a\n' or source[12:16] != b'IHDR':
        raise ValueError('Not a PNG with IHDR')
    width,height,depth,mode,*_=struct.unpack('>IIBBBBB',source[16:29])
    if (width,height,depth,mode) != (WIDTH,HEIGHT,8,2):
        raise ValueError('Unexpected format; expected 1171x1343, RGB 8-bit PNG')
    print('PASS exact C03 hand-behind-bag reference binary SHA-256 and PNG dimensions')
    print('NOT APPROVED: opaque illustration, no native pixel source, rig, Unity/IP/device QA')
    return True


if __name__ == '__main__':
    try:verify()
    except (FileNotFoundError, ValueError) as e:
        print('FAIL C03 image reference: ' + str(e),file=sys.stderr)
        raise SystemExit(1)
