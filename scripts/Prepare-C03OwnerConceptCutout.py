#!/usr/bin/env python3
"""DRAFT C03 opaque-concept isolation, NOT a native-pixel sprite generator.

Keep the exact owner-approved reference unchanged. This separately creates an
illustration-scale alpha cutout for silhouette/edge review. Pixel-art authoring,
21-part layer reconstruction, rigging and IP approval are independent tasks.

Dependency: Pillow (art workstation only). No network or runtime Unity dependency.
"""
import argparse
import hashlib
import json
from pathlib import Path

try:
    from PIL import Image, ImageChops, ImageDraw
except ImportError as exc:
    raise SystemExit('Pillow is needed for optional offline C03 concept isolation') from exc

ROOT = Path(__file__).resolve().parents[1]
REFERENCE = ROOT / 'Assets/StartupLife/Art/Source/Characters/golden-character-c03/references/C03-hand-behind-bag-reference.png'
SOURCE_SHA256 = '1999322a19bf99a5954edc3cf4fd87f6c6b9795006781c0c46ee4ff49fdc30fa'
CANVAS = (1122, 1402)
BEIGE = (253, 251, 244)
DISTANCE_THRESHOLD = 28
EXPECTED_BBOX = (307, 180, 855, 1256)
EXPECTED_OPAQUE_PIXELS = 405998


def isolate(rgb, background=BEIGE, threshold=DISTANCE_THRESHOLD, seed=None):
    """Keep the foreground component connected to the actor's top-center hair.

    Outputs exact source RGB and HARD 0/255 mask. Any detached detail, dark noise
    or nearly-white material may be lost; production cannot use this as a sprite.
    """
    if rgb.mode != 'RGB':
        raise ValueError('Expected RGB; original background must not be discarded')
    width, height = rgb.size
    differences = [ImageChops.difference(channel, Image.new('L', rgb.size, color))
                   for channel, color in zip(rgb.split(), background)]
    spread = ImageChops.lighter(ImageChops.lighter(differences[0], differences[1]), differences[2])
    candidate = spread.point(lambda val: 255 if val > threshold else 0)
    if seed is None:
        seed = (width // 2, height // 6)
    if candidate.getpixel(seed) != 255:
        raise ValueError('Expected a dark character pixel at the verified hair seed')
    # ImageDraw.floodfill requires a mutable image: point() output is mutable.
    ImageDraw.floodfill(candidate, seed, 128)
    mask = candidate.point(lambda val: 255 if val == 128 else 0)
    isolated = rgb.copy()
    isolated.putalpha(mask)
    return isolated


def produce(source, output_dir):
    source = Path(source)
    content = source.read_bytes()
    if hashlib.sha256(content).hexdigest() != SOURCE_SHA256:
        raise ValueError('Not the exact C03 owner-approved hand-behind-bag PNG')
    with Image.open(source) as original:
        if original.format != 'PNG' or original.mode != 'RGBA' or original.size != CANVAS:
            raise ValueError('Unrecognized C03 owner reference image format')
        if original.getchannel('A').getextrema() != (255, 255):
            raise ValueError('C03 selected source has unexpected transparency')
        cutout = isolate(original.convert('RGB'))
    if (cutout.getchannel('A').getbbox() != EXPECTED_BBOX or
        cutout.getchannel('A').histogram()[255] != EXPECTED_OPAQUE_PIXELS):
        raise ValueError('The generated draft mask changed unexpectedly')
    output_dir = Path(output_dir)
    output_dir.mkdir(parents=True, exist_ok=True)
    rgba = output_dir / 'C03-owner-concept-alpha-DRAFT.png'
    cutout.save(rgba)
    # Diagnostic composite with a contrasting single-color background.
    proof = Image.new('RGB', CANVAS, (188, 205, 225))
    proof.paste(cutout, (0, 0), cutout.getchannel('A'))
    preview = output_dir / 'C03-owner-concept-background-check-DRAFT.png'
    proof.save(preview)
    report = {
        'status':'DRAFT_REFERENCE_ONLY_NO_PRODUCTION_APPROVAL',
        'source_sha256':SOURCE_SHA256,
        'source_still_opaque_and_unchanged':True,
        'alpha_mask':'heuristic-hard-edge, not native pixel art or reviewed cutout',
        'foreground_alpha_bbox':list(EXPECTED_BBOX),
        'cutout_sha256':hashlib.sha256(rgba.read_bytes()).hexdigest(),
        'unity_animation':'NOT_RUN',
        'independent_ip':'NOT_RUN'
    }
    (output_dir / 'C03-concept-isolation-report.json').write_text(
        json.dumps(report,indent=2)+'\n',encoding='utf8')
    return report


if __name__ == '__main__':
    cli = argparse.ArgumentParser()
    cli.add_argument('--source',type=Path,default=REFERENCE)
    cli.add_argument('--output-dir',type=Path,required=True)
    args=cli.parse_args()
    print(json.dumps(produce(args.source,args.output_dir),indent=2))
