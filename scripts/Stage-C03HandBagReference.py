#!/usr/bin/env python3
"""Stage exact owner-approved C03 concept PNG to draft/source, never Approved.

Accepts PNG or handoff ZIP. No lossy rewrite/resize. Does not git commit.
"""
import argparse
import hashlib
import os
from pathlib import Path
import tempfile
import zipfile

ROOT=Path(__file__).resolve().parents[1]
RELATIVE=Path('Assets/StartupLife/Art/Source/Characters/golden-character-c03/references/C03-hand-behind-bag-reference.png')
EXPECTED='d2e7e9a79672e23408c0a48626ef3968af84f5564f44e14eac41e4add38cb25e'


def stage(source, repo_root=ROOT):
    source=Path(source)
    repo_root=Path(repo_root)
    if source.suffix.lower()=='.zip':
        with zipfile.ZipFile(source) as archive:
            # Read only the approved destination; no arbitrary ZIP extraction.
            png=archive.read(RELATIVE.as_posix())
    else:
        png=source.read_bytes()
    if hashlib.sha256(png).hexdigest()!=EXPECTED:
        raise ValueError('C03 input is not the owner-approved hand-under-bag PNG (SHA mismatch)')
    if not (png[:8]==b'\x89PNG\r\n\x1a\n' and png[12:16]==b'IHDR'):
        raise ValueError('Wrong format: source must be an exact PNG')
    target=repo_root/RELATIVE
    target.parent.mkdir(parents=True, exist_ok=True)
    if target.exists():
        if target.read_bytes()==png:
            return target, False
        raise FileExistsError('Different C03 binary already exists; refusing to overwrite')
    with tempfile.NamedTemporaryFile(dir=target.parent,prefix='.c03-',delete=False) as temp:
        tmp=Path(temp.name)
        try:
            temp.write(png)
            temp.flush(); os.fsync(temp.fileno())
        except Exception:
            tmp.unlink(missing_ok=True)
            raise
    try:
        os.replace(tmp,target)
    finally:
        tmp.unlink(missing_ok=True)
    return target, True


if __name__=='__main__':
    cli=argparse.ArgumentParser()
    cli.add_argument('source',type=Path,help='Exact approved PNG or C03 handoff ZIP')
    cli.add_argument('--repo-root',type=Path,default=ROOT)
    args=cli.parse_args()
    destination,changed=stage(args.source,args.repo_root)
    print(('STAGED' if changed else 'ALREADY_STAGED'),destination)
    print('NEXT: git add the PNG; commit; verify repo hash. This did not approve a runtime sprite.')
