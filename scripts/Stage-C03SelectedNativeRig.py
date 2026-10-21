#!/usr/bin/env python3
"""Stage a genuinely authored C03 native rig for isolated Unity Editor import.

No sprites are generated here. The selected 2026-10-21 reference, 21 native
layers, bag mask and separately authored joint/motion plan must already exist.
Never edits approved art, shipping scenes, player state, or Unity .meta files.
"""
import argparse
import hashlib
import importlib.util
import json
from pathlib import Path
import shutil

ROOT = Path(__file__).resolve().parents[1]
CONTRACT = ROOT / "docs/evidence/production-art/C03-ACTIVE-OWNER-FRONT-RIG-CONTRACT.json"
OLD_PLAN = ROOT / "docs/evidence/production-art/source/c03-authoring/C03-front-joint-plan-DRAFT.json"
CHECKER = ROOT / "scripts/Validate-C03OwnerRigHandoff.py"
INPUT_SCHEMA = "startup-life.c03.selected-native-joint-motion.v1"
OUTPUT_SCHEMA = "startup-life.c03.selected-native-unity-handoff.v1"
PARENTS = {
    "root": None, "hips": "root", "spine": "hips", "neck": "spine",
    "shoulder_l": "spine", "elbow_l": "shoulder_l", "wrist_l": "elbow_l",
    "shoulder_r": "spine", "elbow_r": "shoulder_r", "wrist_r": "elbow_r",
    "hip_l": "hips", "knee_l": "hip_l", "ankle_l": "knee_l",
    "hip_r": "hips", "knee_r": "hip_r", "ankle_r": "knee_r",
}


def load_checker():
    spec = importlib.util.spec_from_file_location("c03_selected_rig_checker", CHECKER)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def number(value):
    return type(value) in (int, float) and abs(value) < 100000


def validate_authored_plan(plan, contract, historical_plan, packet_hash):
    """Validate artist-controlled joints and motion, no guessed/recycled pivots."""
    if (plan.get("schema") != INPUT_SCHEMA or
            plan.get("status") != "ARTIST_AUTHORED_PENDING_UNITY_REVIEW" or
            plan.get("character_id") != "C03" or plan.get("view") != "front" or
            plan.get("selected_reference_sha256") != contract["selected_concept"]["image_sha256"] or
            plan.get("source_packet_sha256") != packet_hash or
            plan.get("native_grid") != contract["canvas_px"] or
            plan.get("pixels_per_unit") != contract["pixel_per_unit"]):
        raise ValueError("C03 authored rig identity/grid/packet differs from selected reference")
    joints = plan.get("joints")
    if not isinstance(joints, list) or len(joints) != len(PARENTS):
        raise ValueError("C03 authored rig needs exactly 16 joint entries")
    by_id = {j.get("id"): j for j in joints if isinstance(j, dict)}
    if set(by_id) != set(PARENTS) or len(by_id) != len(joints):
        raise ValueError("C03 authored joint IDs are missing/duplicated")
    w, h = contract["canvas_px"]
    for name, parent in PARENTS.items():
        joint = by_id[name]
        p = joint.get("pivot_px")
        if (joint.get("parent") != parent or not isinstance(p, list) or
                len(p) != 2 or any(type(c) is not int for c in p) or
                not (0 <= p[0] < w and 0 <= p[1] < h)):
            raise ValueError("C03 invalid artist-authored parent/pivot: " + name)
    original = {j["id"]: j["pivot"] for j in historical_plan["joints"]}
    if all(by_id[name]["pivot_px"] == original[name] for name in PARENTS):
        raise ValueError("C03 historical 2026-10-16 joint pivots cannot be relabelled as current-image rig")

    assignments = {}
    for name, joint in by_id.items():
        parts = joint.get("parts")
        if not isinstance(parts, list) or len(parts) != len(set(parts)):
            raise ValueError("C03 invalid part assignments at joint " + name)
        for part in parts:
            if part in assignments:
                raise ValueError("C03 semantic part assigned twice: " + part)
            assignments[part] = name
    ordered = contract["rig_parts_back_to_front"]
    if set(assignments) != set(ordered) or len(assignments) != 21:
        raise ValueError("C03 all 21 semantic parts need exactly one authored joint")
    if (assignments["hand_r"] != "wrist_r" or assignments["hand_l"] != "wrist_l" or
            assignments["accessory_front"] != "spine"):
        raise ValueError("C03 hand/bag part ownership changed")
    if ordered.index("accessory_front") <= ordered.index("hand_r"):
        raise ValueError("C03 bag must cover near-side hand")

    motions = plan.get("motions")
    if not isinstance(motions, list) or not motions:
        raise ValueError("C03 needs artist-authored joint-keyframed motion")
    ids = set()
    exported_clips = []
    for clip in motions:
        name = clip.get("id")
        duration = clip.get("duration_seconds")
        tracks = clip.get("tracks")
        if (name not in ("idle", "walk", "work", "study") or name in ids or
                not number(duration) or not 0.2 <= duration <= 10 or
                not isinstance(tracks, list) or not tracks):
            raise ValueError("Invalid C03 motion ID, duration or tracks")
        ids.add(name)
        moved = set()
        out_tracks = []
        for tr in tracks:
            bone = tr.get("joint_id")
            keys = tr.get("keys")
            if (bone not in PARENTS or bone == "root" or
                    bone in {item["joint_id"] for item in out_tracks} or
                    not isinstance(keys, list) or len(keys) < 3):
                raise ValueError("C03 motion cannot slide root, duplicate or omit joint keys")
            last = -1.0
            frames = []
            for key in keys:
                if (not isinstance(key, list) or len(key) != 2 or
                        not number(key[0]) or not number(key[1]) or
                        not 0 <= key[0] <= duration or key[0] <= last or
                        abs(key[1]) > 45):
                    raise ValueError("Invalid C03 authored joint keyframe")
                last = key[0]
                frames.append({"time_seconds": float(key[0]), "angle_degrees": float(key[1])})
            if (abs(frames[0]["time_seconds"]) > 0.00001 or
                    abs(frames[-1]["time_seconds"] - duration) > 0.00001 or
                    abs(frames[0]["angle_degrees"]) > 0.00001 or
                    abs(frames[-1]["angle_degrees"]) > 0.00001):
                raise ValueError("C03 loop must return to bind-pose angles without a jump")
            if any(abs(k["angle_degrees"]) > 0.001 for k in frames[1:-1]):
                moved.add(bone)
            out_tracks.append({"joint_id": bone, "keys": frames})
        if len(moved) < 2:
            raise ValueError("C03 motion needs multiple genuinely moving limb/head joints")
        if name == "walk" and len(moved.intersection({"hip_l", "knee_l", "hip_r", "knee_r"})) < 2:
            raise ValueError("C03 walk needs articulated legs, not a sliding root")
        exported_clips.append({"id": name, "duration_seconds": float(duration), "tracks": out_tracks})
    if "idle" not in ids:
        raise ValueError("C03 current-image rig requires authored idle motion")
    return by_id, assignments, exported_clips


def stage(layers_dir, plan_path, output_dir, root=ROOT):
    layers_dir, plan_path, output_dir, root = map(Path, (layers_dir, plan_path, output_dir, root))
    if output_dir.exists():
        raise FileExistsError("Refuse to overwrite an existing C03 Unity stage: " + str(output_dir))
    checker = load_checker()
    contract_path = root / CONTRACT.relative_to(ROOT)
    contract = json.loads(contract_path.read_text(encoding="utf-8"))
    if contract["selected_concept"].get("native_rig_from_selected_image") != "ARTIST_AUTHORED_LAYERS_SUBMITTED":
        raise ValueError("C03 selected native artwork has not been authored; do not stage historical V3")
    # Independent packet validation also requires an exact Git LFS-selected image.
    checker.verify(contract_path, layers_dir)
    native_manifest = json.loads((layers_dir / checker.NATIVE_MANIFEST).read_text(encoding="utf-8"))
    historical = json.loads((root / OLD_PLAN.relative_to(ROOT)).read_text(encoding="utf-8"))
    plan = json.loads(plan_path.read_text(encoding="utf-8"))
    by_id, assigned, clips = validate_authored_plan(
        plan, contract, historical, native_manifest["source_packet_sha256"])
    ppu = contract["pixel_per_unit"]
    w, h = contract["canvas_px"]
    joints = []
    for name, parent in PARENTS.items():
        x, y = by_id[name]["pivot_px"]
        if parent is None:
            local = [0.0, 0.0]
        else:
            px, py = by_id[parent]["pivot_px"]
            local = [round((x-px)/ppu, 8), round((py-y)/ppu, 8)]
        joints.append({"id": name, "parent": parent or "", "local_xy_unity_units": local})
    sprites = []
    for sort, part in enumerate(contract["rig_parts_back_to_front"]):
        joint = assigned[part]
        x, y = by_id[joint]["pivot_px"]
        sprites.append({"part_id": part, "bone": joint, "sort_order": sort,
                        "path": "layers/" + part + ".png",
                        "local_xy_unity_units": [round((w/2-x)/ppu, 8),
                                                  round((y-h/2)/ppu, 8)]})
    files = []
    for part in contract["rig_parts_back_to_front"]:
        source = layers_dir / (part + ".png")
        files.append({"path": "layers/" + part + ".png",
                      "sha256": hashlib.sha256(source.read_bytes()).hexdigest()})
    manifest = {
        "schema": OUTPUT_SCHEMA, "status": "QUARANTINED_EDITOR_IMPORT_ONLY",
        "selected_reference_sha256": contract["selected_concept"]["image_sha256"],
        "native_source_packet_sha256": native_manifest["source_packet_sha256"],
        "native_grid": contract["canvas_px"], "pixels_per_unit": ppu,
        "files": files, "joints": joints, "sprite_renderers": sprites,
        "motions": clips, "approved_for_gameplay": False
    }
    # All validation happens BEFORE touching the destination.
    output_dir.mkdir(parents=True)
    (output_dir / "layers").mkdir()
    for file in files:
        shutil.copyfile(layers_dir / Path(file["path"]).name, output_dir / file["path"])
    (output_dir / "C03-selected-native-unity-handoff.json").write_text(
        json.dumps(manifest, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    return manifest


if __name__ == "__main__":
    cli = argparse.ArgumentParser(description=__doc__)
    cli.add_argument("--layers-dir", type=Path, required=True)
    cli.add_argument("--rig-plan", type=Path, required=True)
    cli.add_argument("--output", type=Path, default=ROOT / "artifacts/C03-selected-native-rig/input")
    args = cli.parse_args()
    try:
        result = stage(args.layers_dir, args.rig_plan, args.output)
        print(json.dumps({"status": result["status"], "joints": len(result["joints"]),
                          "sprites": len(result["sprite_renderers"]),
                          "motions": [c["id"] for c in result["motions"]]}, indent=2))
    except (ValueError, FileNotFoundError, FileExistsError, KeyError, TypeError) as error:
        cli.exit(1, "BLOCKED C03 selected native Unity stage: " + str(error) + "\n")
