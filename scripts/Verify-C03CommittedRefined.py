#!/usr/bin/env python3
"""Validate PR #59's *committed* C03 refined PNG bytes against selected artwork.

Read-only. This checks actual Git LFS PNGs and provenance, not inferred
originality, joint animation readiness, hidden anatomy or art approval.
"""
import hashlib
import json
from pathlib import Path

from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
SELECTED = ROOT / "Assets/StartupLife/Art/Source/Characters/golden-character-c03/references/C03-hand-behind-bag-reference.png"
RECORD = ROOT / "docs/evidence/production-art/C03-LATEST-MINOR-POLISH-REFERENCE.json"
ORIGINAL = ROOT / "docs/evidence/production-art/drafts/c03-owner-selected-layer-study"
REFINED = ROOT / "docs/evidence/production-art/drafts/c03-owner-selected-layer-refined"
SELECTED_SHA = "1999322a19bf99a5954edc3cf4fd87f6c6b9795006781c0c46ee4ff49fdc30fa"
GRID = (1122, 1402)


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def require(ok, why):
    if not ok:
        raise ValueError("C03 refined source integrity FAILED: " + why)


def run(root=ROOT):
    root = Path(root)
    source = root / SELECTED.relative_to(ROOT)
    original_dir = root / ORIGINAL.relative_to(ROOT)
    refined_dir = root / REFINED.relative_to(ROOT)
    receipt = json.loads((refined_dir / "C03-refined-layer-ownership-receipt.json").read_text(encoding="utf-8"))
    original_receipt = json.loads((original_dir / "C03-selected-cutout-receipt.json").read_text(encoding="utf-8"))
    selected_record = json.loads((root / RECORD.relative_to(ROOT)).read_text(encoding="utf-8"))["latest_reference"]
    guide = json.loads((original_dir / "C03-selected-16-joint-guide-DRAFT.json").read_text(encoding="utf-8"))
    require(selected_record["sha256"] == SELECTED_SHA and
            selected_record["expected_repo_draft_path"] == SELECTED.relative_to(ROOT).as_posix(),
            "selected reference record changed")
    require(source.is_file() and digest(source) == SELECTED_SHA,
            "owner-selected image is missing or a Git LFS pointer")
    with Image.open(source) as im:
        require(im.format == "PNG" and im.mode == "RGBA" and im.size == GRID,
                "owner-selected image format or dimensions differ")
    require(receipt["schema"] == "startup-life.c03.source-visible-layer-ownership-refinement.v1" and
            receipt["status"] == "DRAFT_SOURCE_CORRECTION_NOT_NATIVE_OR_ANIMATABLE" and
            receipt["canonical_selected_sha256"] == SELECTED_SHA and
            receipt["preserved_semantic_layers"] == 21 and
            receipt["preserved_joint_topology"] == 16 and
            receipt["original_fallback_px"] == original_receipt["unassigned_foreground_fallback_px"] == 14025 and
            receipt["composite_identical_to_original_selected_source_cutout"] is True and
            receipt["pixel_colors_changed"] is False and
            receipt["native_128x160_art"] is False and
            receipt["painted_hidden_limb_pixels"] is False and
            receipt["motion_or_unity_verified"] is False and
            receipt["owner_approved"] is False and
            receipt["approved_for_gameplay"] is False,
            "refined receipt identity, status or no-approval declaration differs")
    require(receipt["selected_source_grid"] == list(GRID), "refined canvas changed")
    require(guide["joint_count"] == 16 and guide["rig_approval"] is False and
            guide["reference_sha256"] == SELECTED_SHA, "original guide changed")
    order = original_receipt["semantic_order"]
    require(len(order) == len(set(order)) == 21 and
            order.index("accessory_front") > order.index("hand_r"),
            "21 semantic slots or bag-hand ordering changed")
    declared = receipt["per_file_sha256"]
    expected = {"layers/" + part + ".png" for part in order}
    expected.add("layers/masks/bag_body.png")
    require(set(declared) == expected and len(declared) == 22,
            "checksum manifest does not name exactly 21 layers plus one bag-body mask")
    composite = Image.new("RGBA", GRID)
    for part in order:
        rel = "layers/" + part + ".png"
        file = refined_dir / rel
        require(file.is_file() and digest(file) == declared[rel],
                "missing/unhydrated or modified layer " + rel)
        with Image.open(file) as im:
            require(im.format == "PNG" and im.mode == "RGBA" and im.size == GRID,
                    "not full-resolution 1122x1402 RGBA: " + rel)
            composite.alpha_composite(im)
    mask = refined_dir / "layers/masks/bag_body.png"
    require(mask.is_file() and digest(mask) == declared["layers/masks/bag_body.png"],
            "independent bag mask missing or changed")
    require(declared["layers/masks/bag_body.png"] ==
            original_receipt["file_sha256"]["layers/masks/bag_body.png"],
            "bag mask changed from source study")
    with Image.open(mask) as im:
        require(im.format == "PNG" and im.mode == "RGBA" and im.size == GRID,
                "bag-body mask format or dimensions changed")
    with Image.open(original_dir / "C03-selected-cutout-composite-DRAFT.png") as original:
        require(composite.tobytes() == original.convert("RGBA").tobytes(),
                "refined compositor altered owner-selected visible pixels")
    with Image.open(refined_dir / "C03-refined-layer-ownership-composite.png") as staged:
        require(composite.tobytes() == staged.convert("RGBA").tobytes(),
                "committed refined composite does not match the committed layer files")
    before, after = receipt["torso_bbox_before"], receipt["torso_bbox_after"]
    require(len(before) == len(after) == 4 and after[3] < before[3] and
            after[3] < 1000 and before[3] > 1200,
            "torso ownership bounds do not reflect the source correction")
    require(sum(receipt["fallback_px_assigned_by_semantic_part"].values()) == 14025,
            "original 14,025 fallback pixel accounting changed")
    return {
        "status": "PASS_REFINE_SOURCE_INTEGRITY_NOT_ART_APPROVAL",
        "selected_source_sha256": SELECTED_SHA,
        "verified_pngs": len(expected),
        "semantic_sprites": len(order),
        "bag_mask": "SOURCE_IDENTICAL",
        "corrected_fallback_px": 14025,
        "composite_exact_rgba": True,
        "approved_for_gameplay": False,
        "unity_editor_verified": False,
    }


if __name__ == "__main__":
    print(json.dumps(run(), indent=2))
