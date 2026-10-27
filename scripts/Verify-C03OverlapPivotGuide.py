#!/usr/bin/env python3
"""Verify 10 source-overlap C03 QA pivots against actual hydrated refined PNGs.

This validates measured geometry only. An alpha-intersection centroid is a
candidate pivot, NOT a human anatomy/motion sign-off or shippable rig.
"""
import importlib.util
import json
from pathlib import Path

from PIL import Image, ImageChops

ROOT = Path(__file__).resolve().parents[1]
REFINED = ROOT / "docs/evidence/production-art/drafts/c03-owner-selected-layer-refined"
STUDY = ROOT / "docs/evidence/production-art/drafts/c03-owner-selected-layer-study"
SOURCE_GATE = ROOT / "scripts/Verify-C03CommittedRefined.py"
SHA = "1999322a19bf99a5954edc3cf4fd87f6c6b9795006781c0c46ee4ff49fdc30fa"
WIDTH, HEIGHT = 1122, 1402


def alpha_centroid(a, b):
    """Count opaque overlapping alpha pixels and return the true image-space centroid."""
    if a.mode != "RGBA" or b.mode != "RGBA" or a.size != b.size or a.size != (WIDTH, HEIGHT):
        raise ValueError("Expected selected C03 1122x1402 RGBA PNGs")
    overlap = ImageChops.multiply(a.getchannel("A"), b.getchannel("A"))
    bbox = overlap.getbbox()
    if bbox is None:
        raise ValueError("No native-part overlap; cannot author a source-derived pivot")
    sub = overlap.crop(bbox)
    w, h = sub.size
    vals = sub.tobytes()
    qty = 0
    sum_x, sum_y = 0, 0
    for y in range(h):
        row = vals[y * w:(y + 1) * w]
        for x, value in enumerate(row):
            if value <= 0:
                continue
            qty += 1
            sum_x += bbox[0] + x
            sum_y += bbox[1] + y
    if qty <= 0:
        raise ValueError("C03 supposed overlap contained no pixels")
    return qty, (sum_x / qty, sum_y / qty)


def run(root=ROOT):
    root = Path(root)
    spec = importlib.util.spec_from_file_location("c03_overlap_source_gate",
                                                   root / SOURCE_GATE.relative_to(ROOT))
    verifier = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(verifier)
    verified = verifier.run(root)
    if verified["status"] != "PASS_REFINE_SOURCE_INTEGRITY_NOT_ART_APPROVAL":
        raise ValueError("Refined source image identity was not verified")
    folder = root / REFINED.relative_to(ROOT)
    guide = json.loads((folder / "C03-selected-overlap-pivot-guide-QA.json").read_text(encoding="utf-8"))
    original = json.loads((root / STUDY.relative_to(ROOT) /
                           "C03-selected-16-joint-guide-DRAFT.json").read_text(encoding="utf-8"))
    if (guide["schema"] != "startup-life.c03.owner-source-overlap-pivot-qa.v1" or
            guide["status"] != "OVERLAP_CENTER_ESTIMATE_QA_ONLY_NOT_NATIVE_OR_ANIMATABLE" or
            guide["reference_sha256"] != SHA or guide["rig_approval"] is not False or
            guide["calibrated_joint_count"] != 10 or guide["joint_count"] != 16 or
            len(guide["joints"]) != 16 or guide["motion_clips"] != [] or
            guide["source_canvas_px"] != [WIDTH, HEIGHT] or
            guide["candidate_native_grid_for_future_redraw"] != [128, 160]):
        raise ValueError("C03 QA pivot guide changes owner identity or misstates approval")
    old_joints = {entry["id"]: entry for entry in original["joints"]}
    new_joints = {entry["id"]: entry for entry in guide["joints"]}
    if set(old_joints) != set(new_joints) or len(new_joints) != 16:
        raise ValueError("C03 joint identity count/topology changed")
    changed = guide["pivot_change_provenance"]
    if len(changed) != 10:
        raise ValueError("Exactly ten calibrated C03 hand/leg joints expected")
    report = {}
    for name, joint in new_joints.items():
        legacy = old_joints[name]
        if joint["parent"] != legacy["parent"] or joint["parts"] != legacy["parts"]:
            raise ValueError("C03 topology/semantic sprite ownership must stay unchanged")
        pivot = joint["pivot_native_estimate_xy"]
        if len(pivot) != 2 or any(type(n) is not int or n < 0 or n >= lim
                                 for n, lim in zip(pivot, (128, 160))):
            raise ValueError("C03 QA pivot is outside candidate reference grid")
        if name not in changed:
            if pivot != legacy["pivot_native_estimate_xy"]:
                raise ValueError("C03 unrelated joint was moved without measured overlap")
            continue
        record = changed[name]
        part_a, part_b = record["alpha_overlap_parts"]
        if (record["previous_native_xy"] != legacy["pivot_native_estimate_xy"] or
                record["candidate_native_xy"] != pivot):
            raise ValueError("C03 candidate pivot history is inconsistent")
        with Image.open(folder / "layers" / (part_a + ".png")) as i1, Image.open(
            folder / "layers" / (part_b + ".png")) as i2:
            qty, (x, y) = alpha_centroid(i1.convert("RGBA"), i2.convert("RGBA"))
        native = [round(x * 128 / WIDTH), round(y * 160 / HEIGHT)]
        # Require the exact rounded centroid, not +/- 1. A single native
        # pixel misalignment is large enough to show a visible joint seam.
        if qty != record["source_overlap_pixels"] or native != pivot:
            raise ValueError("C03 pivot/count differs from measured overlap: " + name)
        report[name] = {
            "original_native_xy": legacy["pivot_native_estimate_xy"],
            "candidate_native_xy": pivot,
            "measured_overlap_native_xy": native,
            "shared_opaque_source_pixels": qty
        }
    return {
        "status": "PASS_TEN_SOURCE_OVERLAP_PIVOTS_NOT_MOTION_APPROVAL",
        "selected_reference_sha256": SHA,
        "rig_joint_count": 16,
        "sprite_slots_unchanged": 21,
        "corrected_joint_count": len(report),
        "joint_deltas": report,
        "Unity_Editor_executed": False,
        "native_128x160_authoring_complete": False,
        "approved_for_gameplay": False
    }


if __name__ == "__main__":
    print(json.dumps(run(), indent=2))
