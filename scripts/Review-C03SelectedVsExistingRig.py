#!/usr/bin/env python3
"""Make an honest side-by-side review of the selected C03 and existing V3 layers.

READ ONLY source audit. No sprite replacement, auto-cutout, rig rewrite,
production registration, or automatic visual/IP approval. Needs hydrated
Git LFS PNGs and Pillow on the review runner.
"""
import argparse
import hashlib
import importlib.util
import json
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[1]
REFERENCE_RECORD = ROOT / "docs/evidence/production-art/C03-LATEST-MINOR-POLISH-REFERENCE.json"
SOURCE = ROOT / "docs/evidence/production-art/source/c03-authoring/C03-native-front-source.json"
HISTORICAL_RENDERER = ROOT / "scripts/Render-C03OwnerNativeFront.py"
LEGACY_LAYERS = ROOT / "Assets/StartupLife/Art/Source/Characters/golden-character-c03/Generated-V3-Quarantine/layers"
LEGACY_PREFAB = ROOT / "Assets/StartupLife/Art/Source/Characters/golden-character-c03/Generated-V3-Quarantine/C03_IDLE_V3_QUARANTINED.prefab"

SHEET_WIDTH = 1380
PANEL_WIDTH = 650
PANEL_HEIGHT = 850
COLLAGE_BG = (247, 243, 234, 255)
BLACK = (43, 43, 47, 255)


def sha256(data):
    return hashlib.sha256(data).hexdigest()


def load_historical_renderer():
    spec = importlib.util.spec_from_file_location("c03_native_historical", HISTORICAL_RENDERER)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def load_canonical_image(root=ROOT):
    record = json.loads((root / REFERENCE_RECORD.relative_to(ROOT)).read_text(encoding="utf-8"))
    ref = record["latest_reference"]
    if record["character_id"] != "C03" or ref["sha256"] != (
        "1999322a19bf99a5954edc3cf4fd87f6c6b9795006781c0c46ee4ff49fdc30fa"
    ):
        raise ValueError("Selected C03 reference identity has changed; stop review")
    path = root / ref["expected_repo_draft_path"]
    binary = path.read_bytes()
    if sha256(binary) != ref["sha256"]:
        raise ValueError("Selected C03 PNG is absent, unhydrated LFS or has wrong SHA-256")
    with Image.open(path) as raw:
        if raw.format != "PNG" or raw.mode != "RGBA" or raw.size != (1122, 1402):
            raise ValueError("C03 selected reference PNG mode or dimensions changed")
        return raw.copy(), ref


def load_legacy_layer_images(source_module, root=ROOT):
    descriptor = source_module.expand_source(root / SOURCE.relative_to(ROOT))
    if descriptor["visual_master_sha256"] != (
        "d2e7e9a79672e23408c0a48626ef3968af84f5564f44e14eac41e4add38cb25e"
    ):
        raise ValueError("Historical source art identity has changed unexpectedly")
    raw_layers, mask = source_module.paint(descriptor)
    loaded = {}
    parity = {}
    for name in source_module.PARTS:
        path = root / LEGACY_LAYERS.relative_to(ROOT) / (name + ".png")
        with Image.open(path) as sprite:
            if sprite.format != "PNG" or sprite.mode != "RGBA" or sprite.size != (128, 160):
                raise ValueError("Invalid imported historical C03 sprite: " + name)
            loaded[name] = sprite.copy()
        pixels = loaded[name].tobytes()
        if any(a not in (0, 255) for a in pixels[3::4]):
            raise ValueError("Non-binary alpha in legacy C03 sprite: " + name)
        parity[name] = pixels == raw_layers[name]
    return loaded, parity, descriptor, mask


def composite(layers, order):
    output = Image.new("RGBA", (128, 160))
    for name in order:
        output.alpha_composite(layers[name])
    return output


def inspect_legacy_prefab(root=ROOT):
    text = (root / LEGACY_PREFAB.relative_to(ROOT)).read_text(encoding="utf-8")
    # Structural checks only. These do not establish that a pose looks good.
    import re
    types = [int(x) for x in re.findall(r"^--- !u!(\d+) &", text, re.M)]
    orders = sorted(int(x) for x in re.findall(r"^  m_SortingOrder: (-?\d+)$", text, re.M))
    result = {
        "game_objects": types.count(1),
        "transforms": types.count(4),
        "sprite_renderers": types.count(212),
        "animators": types.count(95),
        "sorting_orders_unique_0_to_20": orders == list(range(21)),
    }
    if (result["game_objects"], result["transforms"],
            result["sprite_renderers"], result["animators"]) != (37, 37, 21, 1):
        raise ValueError("Existing C03 prefab structure changed; audit before reuse")
    if not result["sorting_orders_unique_0_to_20"]:
        raise ValueError("Existing C03 sprite sorting has gaps/duplicates")
    return result


def compose_panel(source, title, label, *, native=False):
    panel = Image.new("RGBA", (PANEL_WIDTH, PANEL_HEIGHT), COLLAGE_BG)
    d = ImageDraw.Draw(panel)
    font = ImageFont.load_default(size=21)
    small = ImageFont.load_default(size=16)
    d.text((22, 16), title, fill=BLACK, font=font)
    d.text((22, 48), label, fill=(93, 87, 82, 255), font=small)
    available_w, available_h = PANEL_WIDTH - 52, PANEL_HEIGHT - 130
    scale = min(available_w / source.width, available_h / source.height)
    if native:
        scale = max(1, min(int(scale), 6))
        method = Image.Resampling.NEAREST
    else:
        method = Image.Resampling.LANCZOS  # display sizing only; never edits art
    draw_w = int(source.width * scale)
    draw_h = int(source.height * scale)
    scaled = source.resize((draw_w, draw_h), method)
    x = (PANEL_WIDTH - draw_w) // 2
    y = 81 + (available_h - draw_h) // 2
    if native:
        panel.alpha_composite(scaled, (x, y))
    else:
        panel.paste(scaled, (x, y))
    return panel


def make_review_sheet(canonical, merged):
    out = Image.new("RGB", (SHEET_WIDTH, PANEL_HEIGHT + 95), (233, 230, 223))
    source = compose_panel(
        canonical, "OWNER SELECTED C03 / 2026-10-21", "Exact Git LFS PNG | 1122 x 1402"
    )
    legacy = compose_panel(
        merged, "EXISTING V3 RIG / 2026-10-16", "Composited from the 21 tracked Unity sprite PNGs",
        native=True
    )
    out.paste(source.convert("RGB"), (20, 10))
    out.paste(legacy.convert("RGB"), (710, 10))
    d = ImageDraw.Draw(out)
    font = ImageFont.load_default(size=17)
    d.text(
        (25, PANEL_HEIGHT + 26),
        "REVIEW ONLY: NOT IMAGE-PARITY PROOF. Different source revisions, proportions and grids.",
        fill=(114, 51, 43), font=font
    )
    d.text(
        (25, PANEL_HEIGHT + 54),
        "Preserve existing 16-joint / 21-slot rig. Mark only justified art/pivot differences.",
        fill=BLACK, font=font
    )
    return out


def make_layer_sheet(layers, order):
    cell_w, cell_h = 175, 236
    out = Image.new("RGB", (cell_w * 7, cell_h * 3), (239, 235, 227))
    d = ImageDraw.Draw(out)
    font = ImageFont.load_default(size=14)
    for i, part in enumerate(order):
        x, y = (i % 7) * cell_w, (i // 7) * cell_h
        sprite = layers[part].resize((128, 160), Image.Resampling.NEAREST)
        bg = Image.new("RGBA", (128, 160), COLLAGE_BG)
        bg.alpha_composite(sprite)
        out.paste(bg.convert("RGB"), (x + 23, y + 15))
        d.text((x + 8, y + 187), part, fill=(43, 43, 47), font=font)
        d.text((x + 8, y + 207), "LEGACY 2026-10-16", fill=(123, 104, 90), font=font)
    return out


def run(output_dir, root=ROOT):
    output_dir = Path(output_dir)
    current, reference = load_canonical_image(root)
    renderer = load_historical_renderer()
    sprites, parity, descriptor, mask = load_legacy_layer_images(renderer, root)
    prefab = inspect_legacy_prefab(root)
    merged = composite(sprites, renderer.PARTS)
    old_reference = Image.frombytes("RGBA", (128, 160), renderer.composite(
        renderer.paint(renderer.expand_source(root / SOURCE.relative_to(ROOT)))[0]))
    composite_equal = merged.tobytes() == old_reference.tobytes()
    occlusion = {}
    # The historical bag-body mask is independent authored source data; check
    # preservation, but never claim it applies to the newer selected concept.
    from importlib.util import spec_from_file_location, module_from_spec
    checker_path = root / "scripts/Validate-C03OwnerRigHandoff.py"
    checker_spec = spec_from_file_location("c03_bag_checker_review", checker_path)
    checker = module_from_spec(checker_spec)
    checker_spec.loader.exec_module(checker)
    occlusion = checker.inspect_masked_occlusion(
        sprites["hand_r"].tobytes(), sprites["accessory_front"].tobytes(),
        mask, canvas=(128, 160), minimum_overlap=16, minimum_exposed_below=8
    )
    output_dir.mkdir(parents=True, exist_ok=True)
    make_review_sheet(current, merged).save(output_dir / "C03-selected-vs-existing-V3-side-by-side.png")
    make_layer_sheet(sprites, renderer.PARTS).save(output_dir / "C03-existing-V3-21-layers.png")
    merged.save(output_dir / "C03-existing-V3-composite-128x160.png")
    report = {
        "schema": "startup-life.c03.existing-rig-visual-review.v1",
        "status": "AWAITING_OWNER_SIDE_BY_SIDE_REVIEW_NOT_APPROVED",
        "selected_image_sha256": reference["sha256"],
        "selected_image_dimensions": [1122, 1402],
        "selected_image_is_opaque_reference_not_sprite": True,
        "legacy_source_image_sha256": descriptor["visual_master_sha256"],
        "legacy_source_native_packet_sha256": descriptor["packet_sha256"],
        "legacy_layer_count": len(sprites),
        "legacy_layers_equal_frozen_old_pixel_packet": parity,
        "legacy_composite_matches_old_authored_pixels": composite_equal,
        "legacy_occlusion": occlusion,
        "unity_prefab_structure": prefab,
        "visual_parity_selected_vs_legacy": "NOT_DETERMINED_NEEDS_HUMAN_IMAGE_COMPARISON",
        "joint_pivots_match_selected_image": "NOT_DETERMINED",
        "no_art_asset_or_prefab_modified": True,
        "no_unity_editor_or_device_execution": True,
        "required_decision": "MARK_JUSTIFIED_DIFFERENCES_OR_KEEP_LEGACY_APPEARANCE_PENDING_REVIEW",
    }
    (output_dir / "C03-review.json").write_text(
        json.dumps(report, indent=2, ensure_ascii=False) + "\n", encoding="utf-8"
    )
    print(json.dumps({
        "status": report["status"],
        "legacy_layer_count": len(sprites),
        "old_source_layer_identity": all(parity.values()),
        "old_source_composite_identity": composite_equal,
        "selected_source_sha256_verified": reference["sha256"],
        "owner_image_comparison": report["visual_parity_selected_vs_legacy"],
        "output_dir": str(output_dir),
    }, indent=2))
    return report


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--output-dir", type=Path, required=True)
    args = parser.parse_args()
    run(args.output_dir)
