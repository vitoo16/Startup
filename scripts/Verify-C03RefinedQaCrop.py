#!/usr/bin/env python3
"""Verify lossless alpha-bounds cropping of the *committed* refined C03 sources.

The Unity QA importer crops transparent source-space margins and applies a
center offset, preserving the owner-selected static silhouette. This verifies
the same crop geometry against hydrated Git LFS PNGs, and reports the avoided
uncompressed texture memory. Does not claim Unity Editor or animation PASS.
"""
import importlib.util
import json
from pathlib import Path

from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
VERIFIER = ROOT / "scripts/Verify-C03CommittedRefined.py"
REFINED = ROOT / "docs/evidence/production-art/drafts/c03-owner-selected-layer-refined"
STUDY = ROOT / "docs/evidence/production-art/drafts/c03-owner-selected-layer-study"
WIDTH, HEIGHT = 1122, 1402
PADDING = 2
PPU = 280.5


def load_source_verifier():
    spec = importlib.util.spec_from_file_location("c03_committed_source", VERIFIER)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def crop_alpha(source):
    if source.mode != "RGBA" or source.size != (WIDTH, HEIGHT):
        raise ValueError("C03 source cutout must be full-canvas RGBA")
    bounds = source.getchannel("A").getbbox()
    if bounds is None:
        raise ValueError("C03 transparent semantic layer is not valid")
    left = max(0, bounds[0] - PADDING)
    top = max(0, bounds[1] - PADDING)
    right = min(WIDTH, bounds[2] + PADDING)
    bottom = min(HEIGHT, bounds[3] + PADDING)
    crop = source.crop((left, top, right, bottom))

    # Unity texture has bottom-left pixel origin, and the QA importer uses
    # SpriteAlignment.Center on a cropped texture. Undo the cropped center
    # displacement in the sprite Transform (not by changing source pixels).
    center_x = (left + right) * 0.5
    center_y = (top + bottom) * 0.5
    unity_offset = ((center_x - WIDTH * 0.5) / PPU,
                    (HEIGHT * 0.5 - center_y) / PPU)
    return crop, (left, top), unity_offset


def run(root=ROOT):
    root = Path(root)
    verify = load_source_verifier()
    result = verify.run(root)
    if result["status"] != "PASS_REFINE_SOURCE_INTEGRITY_NOT_ART_APPROVAL":
        raise ValueError("Committed 21 PNG verification has not passed")
    refined = root / REFINED.relative_to(ROOT)
    study = root / STUDY.relative_to(ROOT)
    receipt = json.loads((study / "C03-selected-cutout-receipt.json").read_text())
    names = receipt["semantic_order"]
    if len(names) != 21:
        raise ValueError("Wrong C03 semantics / layer count")

    reconstructed = Image.new("RGBA", (WIDTH, HEIGHT))
    uncompressed_before = 0
    uncompressed_after = 0
    offsets = {}
    for name in names + ["masks/bag_body"]:
        path = refined / "layers" / (name + ".png")
        with Image.open(path) as image:
            source = image.convert("RGBA")
        cropped, top_left, world_offset = crop_alpha(source)
        if cropped.width >= WIDTH or cropped.height >= HEIGHT:
            raise ValueError("Untrimmed full-canvas sprite: " + name)
        uncompressed_before += WIDTH * HEIGHT * 4
        uncompressed_after += cropped.width * cropped.height * 4
        if name != "masks/bag_body":
            reconstructed.alpha_composite(cropped, dest=top_left)
        offsets[name] = {
            "source_canvas_bbox_top_left": [
                top_left[0], top_left[1],
                top_left[0] + cropped.width, top_left[1] + cropped.height
            ],
            "trim_center_offset_unity_units": [
                round(world_offset[0], 8), round(world_offset[1], 8)
            ]
        }

    with Image.open(refined / "C03-refined-layer-ownership-composite.png") as ground_truth:
        if reconstructed.tobytes() != ground_truth.convert("RGBA").tobytes():
            raise AssertionError("C03 crop/offset fails pixel-perfect static composite reconstruction")

    saving = 1 - uncompressed_after/uncompressed_before
    if saving <= .9:
        raise AssertionError("C03 trimmed QA cutouts did not deliver meaningful texture savings")

    return {
        "status": "PASS_SOURCE_TRIM_GEOMETRY_ONLY_NOT_UNITY_EDITOR",
        "selected_reference_sha256": result["selected_source_sha256"],
        "semantic_layers": len(names),
        "bag_mask_count": 1,
        "transparent_padding_pixels": PADDING,
        "source_width": WIDTH,
        "source_height": HEIGHT,
        "qa_pixels_per_unit": PPU,
        "rgba_uncompressed_before_bytes": uncompressed_before,
        "rgba_uncompressed_after_bytes": uncompressed_after,
        "uncompressed_reduction_percent": round(saving * 100, 3),
        "static_pixel_composite_unchanged": True,
        "cropped_sprite_centers": offsets,
        "native_128x160_rig": False,
        "real_Unity_import_or_animation_verified": False,
        "approved_for_gameplay": False
    }


if __name__ == "__main__":
    print(json.dumps(run(), ensure_ascii=False, indent=2))
