#!/usr/bin/env python3
"""Compare provisional-vs-measured C03 joint poses from the EXACT 21 refined PNGs.

This is a visual review, NOT a quality score, native redraw, production
animation or Unity Editor run. Reuses the existing real-joint pose renderer.
Neither selected C03 art nor the original 16-joint topology is rewritten.
"""
import argparse
import importlib.util
import json
from pathlib import Path

from PIL import Image, ImageChops, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[1]
POSE_SCRIPT = ROOT / "scripts/Preview-C03OwnerCutoutArticulation.py"
OVERLAP_GATE = ROOT / "scripts/Verify-C03OverlapPivotGuide.py"
GUIDE_PATH = ROOT / "docs/evidence/production-art/drafts/c03-owner-selected-layer-refined/C03-selected-overlap-pivot-guide-QA.json"
SHA = "1999322a19bf99a5954edc3cf4fd87f6c6b9795006781c0c46ee4ff49fdc30fa"
PANELS = (420, 480)
POSE_NAMES = ("bind_exact", "left_arm_bend", "right_arm_bend", "walk_leg_A", "walk_leg_B")


def load_module(path, name):
    spec = importlib.util.spec_from_file_location(name, path)
    obj = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(obj)
    return obj


def changed_pixels(before, after):
    parts = ImageChops.difference(before, after).split()
    diff = parts[0]
    for channel in parts[1:]:
        diff = ImageChops.lighter(diff, channel)
    return sum(value != 0 for value in diff.tobytes())


def tile(frame, size=PANELS):
    w, h = size
    panel = Image.new("RGBA", (w, h), (247, 242, 233, 255))
    view = frame.copy()
    view.thumbnail((w - 22, h - 28), Image.Resampling.NEAREST)
    panel.alpha_composite(view, ((w - view.width)//2, (h - view.height)//2))
    return panel.convert("RGB")


def run(output_dir, root=ROOT):
    output_dir = Path(output_dir)
    if output_dir.exists():
        raise FileExistsError("Do not replace previous C03 pivot visual proof")
    root = Path(root)
    stress = load_module(root / POSE_SCRIPT.relative_to(ROOT), "c03_pose_review_reuse")
    gate = load_module(root / OVERLAP_GATE.relative_to(ROOT), "c03_pivot_source_gate")
    verified = gate.run(root)
    if verified["status"] != "PASS_TEN_SOURCE_OVERLAP_PIVOTS_NOT_MOTION_APPROVAL":
        raise ValueError("Real selected C03 overlap-pivot source QA has not passed")
    order, sprites, original_joints, owners, source = stress.checked_inputs(root)
    guide = json.loads((root / GUIDE_PATH.relative_to(ROOT)).read_text(encoding="utf-8"))
    corrected_joints = {joint["id"]: joint for joint in guide["joints"]}
    if (len(corrected_joints) != 16 or
            set(corrected_joints) != set(original_joints) or
            source["reference_sha256"] != SHA):
        raise ValueError("C03 owner or rig joint identities have changed")
    for part, bone in owners.items():
        if part not in corrected_joints[bone]["parts"]:
            raise ValueError("C03 corrected rig remapped an old semantic sprite")

    w, h = PANELS
    canvas = Image.new("RGB", (w*2 + 40, (h + 39)*len(POSE_NAMES) + 16),
                       (231, 228, 222))
    draw = ImageDraw.Draw(canvas)
    font = ImageFont.load_default(size=17)
    comparison = {}
    for row, name in enumerate(POSE_NAMES):
        angles = stress.POSES[name]
        previous = stress.render(order, sprites, original_joints, owners, angles)
        revised = stress.render(order, sprites, corrected_joints, owners, angles)
        diff = changed_pixels(previous, revised)
        if name == "bind_exact" and diff != 0:
            raise AssertionError("C03 bind pose changed by pivot-only adjustment")
        comparison[name] = {
            "rotation_degrees": angles,
            "rgba_pixels_differ_old_vs_calibrated": diff,
            "aesthetic_improvement_verified": False
        }
        for col, (label, frame) in enumerate(
            (("ORIGINAL PIVOTS", previous), ("TEN OVERLAP PIVOTS", revised))
        ):
            x, y = 12 + col * (w + 14), 8 + row * (h + 39)
            canvas.paste(tile(frame), (x, y + 26))
            draw.text((x + 3, y + 5), name + "  /  " + label,
                      fill=(56, 54, 52), font=font)

    output_dir.mkdir(parents=True)
    canvas.save(output_dir / "C03-original-vs-calibrated-pivot-poses-REVIEW.png")
    report = {
        "schema": "startup-life.c03.source-overlap-pivot-visual-review.v1",
        "status": "HUMAN_SEAM_REVIEW_REQUIRED_NOT_ART_ANIMATION_APPROVAL",
        "reference_sha256": SHA,
        "source_refined_png_count": 21,
        "joint_count": 16,
        "calibrated_overlap_joint_count": 10,
        "bind_pose_pixel_identical": True,
        "pivot_overlap_source_gate": "PASS",
        "pose_diagnostics": comparison,
        "actual_Unity_Editor_or_device_run": False,
        "native_pixel_art_authoring_complete": False,
        "approved_for_gameplay": False,
        "art_remaining": [
            "Hand-painted hidden arm/wrist, neck and leg anatomy",
            "Clean alpha boundaries at bag, sleeves and shoes",
            "Native 128x160 manually authored pixel layers",
            "Unity Editor motion QA, mobile beauty cell and owner/IP review"
        ]
    }
    (output_dir / "C03-pivot-visual-review.json").write_text(
        json.dumps(report, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    return report


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--output-dir", type=Path, required=True)
    args = parser.parse_args()
    r = run(args.output_dir)
    print(json.dumps({
        "status": r["status"],
        "rig_joint_count": r["joint_count"],
        "calibrated_joint_count": r["calibrated_overlap_joint_count"],
        "all_poses_have_before_after_images": True
    }, indent=2))
