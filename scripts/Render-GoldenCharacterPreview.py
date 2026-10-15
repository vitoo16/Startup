#!/usr/bin/env python3
"""Render deterministic C01 front-view QA contact sheet (stdlib only).

This is NOT a Unity render, rig playback, Golden approval or art provenance.
The second panel is a black-ink silhouette check, at integer magnification.
"""
import argparse
import json
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
from PixelPng import png_rgba

ROOT = Path(__file__).resolve().parents[1]
SOURCE = Path("Assets/StartupLife/Art/Source/Characters/golden-character-c01/layers.json")
SPEC = Path("Assets/StartupLife/Art/_ArtDirection/art-spec.yaml")
SCALE = 3
MARGIN = 24


def color(value):
    if not isinstance(value, str) or len(value) != 7 or value[0] != "#":
        raise ValueError("Invalid pixel color")
    return bytes([int(value[i:i + 2], 16) for i in (1, 3, 5)] + [255])


def painted_pixels(draft):
    width, height = draft["canvas"]
    if width != 128 or height != 160:
        raise ValueError("Prototype must remain 128x160 until style re-approval")
    colors = {p["id"]: color(p["hex"]) for p in draft["palette"]}
    if len(colors) != len(draft["palette"]):
        raise ValueError("Duplicate C01 swatches")
    pixels = [None] * (width * height)
    ordered = sorted(enumerate(draft["parts"]),
                     key=lambda value: (value[1]["sorting_order"], value[0]))
    for _, part in ordered:
        for rect in part["rects"]:
            x, y, w, h = rect["box"]
            if not (0 <= x < width and 0 <= y < height and
                    w > 0 and h > 0 and x + w <= width and y + h <= height):
                raise ValueError("Out-of-bounds paint: " + part["id"])
            shade = colors[rect["color"]]
            for row in range(y, y + h):
                start = row * width + x
                pixels[start:start + w] = [shade] * w
    return pixels


def coverage(draft, pixels):
    width, height = draft["canvas"]
    coords = [(i % width, i // width) for i, px in enumerate(pixels) if px is not None]
    if not coords:
        raise ValueError("C01 is visually empty")
    xs, ys = zip(*coords)
    return {"opaque": len(coords),
            "fraction": round(len(coords) / (width * height), 4),
            "bbox": [min(xs), min(ys), max(xs), max(ys)]}


def render(root=ROOT):
    root = Path(root)
    draft = json.loads((root / SOURCE).read_text(encoding="utf-8"))
    spec = json.loads((root / SPEC).read_text(encoding="utf-8"))
    if (draft["status"] != "DRAFT_NOT_APPROVED" or
            draft["style_id"] != spec["style_id"] or
            draft["views_completed"] != ["front"]):
        raise ValueError("Only unapproved C01 front-source preview is allowed")
    pixels = painted_pixels(draft)
    stats = coverage(draft, pixels)
    if not 3000 <= stats["opaque"] <= 14000:
        raise ValueError("C01 silhouette coverage needs independent visual review")
    width, height = draft["canvas"]
    panel_w, panel_h = width * SCALE, height * SCALE
    out_w = panel_w * 2 + MARGIN * 3
    out_h = panel_h + MARGIN * 2
    background = color(spec["palette"]["roles"]["background"])
    ink = color(spec["palette"]["roles"]["ink"])
    canvas = bytearray(background * (out_w * out_h))
    for y in range(height):
        for x in range(width):
            px = pixels[y * width + x]
            if px is None:
                continue
            row_top = MARGIN + (height - y - 1) * SCALE
            for dx in range(SCALE):
                for dy in range(SCALE):
                    row = row_top + dy
                    col = MARGIN + x * SCALE + dx
                    idx = (row * out_w + col) * 4
                    canvas[idx:idx + 4] = px
                    idx2 = (row * out_w + (col + panel_w + MARGIN)) * 4
                    canvas[idx2:idx2 + 4] = ink
    rows = (bytes(canvas[y * out_w * 4:(y + 1) * out_w * 4])
            for y in range(out_h))
    return png_rgba(out_w, out_h, rows), stats


if __name__ == "__main__":
    ap = argparse.ArgumentParser()
    ap.add_argument("--output", type=Path, required=True)
    args = ap.parse_args()
    picture, metrics = render()
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_bytes(picture)
    print(f"C01 DRAFT preview only: {metrics}, {len(picture)} bytes at {args.output}. "
          "No Unity/IP/Golden art approval implied.")
