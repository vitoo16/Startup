#!/usr/bin/env python3
"""Check C01 source topology and quarantine. Not visual/IP/Unity acceptance."""
import json
import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = Path("Assets/StartupLife/Art/Source/Characters/golden-character-c01/layers.json")
SPEC = Path("Assets/StartupLife/Art/_ArtDirection/art-spec.yaml")
PROVENANCE = Path("Assets/StartupLife/Art/_ArtDirection/provenance/manifest.json")
REGISTRY = Path("Assets/StartupLife/Art/Approved/registry.json")
BUILDER = Path("Assets/StartupLife/Scripts/Editor/GoldenCharacterDraftBuilder.cs")


def require(condition, message):
    if not condition:
        raise AssertionError(message)


def validate(root=ROOT):
    root = Path(root)
    data = json.loads((root / SOURCE).read_text(encoding="utf-8"))
    spec = json.loads((root / SPEC).read_text(encoding="utf-8"))
    provenance = json.loads((root / PROVENANCE).read_text(encoding="utf-8"))
    registry = json.loads((root / REGISTRY).read_text(encoding="utf-8"))
    require(data["schema"] == "startup-life.pixel-character-draft.v1", "draft schema drift")
    require(data["asset_id"] == "sl-character-golden-c01" and
            data["status"] == "DRAFT_NOT_APPROVED", "draft identity/approval drift")
    require(data["style_id"] == spec["style_id"] == registry["style_id"] ==
            "vn_everyday_pixel_v1", "draft/style drift")
    require(spec["craft"]["prototype_actor_source"] == SOURCE.as_posix() and
            data["ppu"] == spec["craft"]["pixels_per_unit"] and
            data["canvas"] == spec["craft"]["native_character_size_px"] ==
            [128, 160] and data["root_pivot"] == [64, 0],
            "draft size/pixel authority drift")
    require(data["views_completed"] == ["front"] and
            sorted(data["views_pending"]) == ["side", "three_quarter_left"],
            "prototype cannot claim finished turnaround")
    require(not registry["approved_entries"] and
            not spec["conditioning"]["approved_golden_ids"], "draft was auto-approved")
    candidates = [a for a in provenance["assets"] if a.get("asset_id") == data["asset_id"]]
    require(len(candidates) == 1 and candidates[0]["status"] == "DRAFT" and
            candidates[0]["unity_path"] is None and
            SOURCE.as_posix() in candidates[0]["source_files"] and
            candidates[0]["approval_date_utc"] is None,
            "missing/unapproved candidate provenance")
    palette = data["palette"]
    swatches = {s["id"]: s["hex"] for s in palette}
    require(len(swatches) == len(palette) and len(swatches) >= 8 and
            all(re.fullmatch(r"#[A-Fa-f0-9]{6}", v) for v in swatches.values()),
            "invalid or duplicate source swatch")
    parts = data["parts"]
    names = {p["id"] for p in parts}
    required = set(spec["characters"]["mandatory_layers"])
    require(len(parts) == len(names) and required <= names, "missing or duplicate rig layers")
    require(len(parts) >= 19, "not enough rig parts")
    seen_sort = set()
    W, H = data["canvas"]
    for piece in parts:
        name = piece["id"]
        parent = piece["parent"]
        require(parent == "root" or parent in names and parent != name,
                "invalid rig parent: " + name)
        x, y = piece["joint_px"]
        require(0 <= x < W and 0 <= y < H and
                isinstance(piece["sorting_order"], int), "invalid joint or sorting")
        boxes = piece["rects"]
        require(1 <= len(boxes) <= 12, "empty/oversized layer: " + name)
        ext = []
        for shape in boxes:
            X, Y, w, h = shape["box"]
            require(shape["color"] in swatches and
                    all(type(z) is int for z in (X, Y, w, h)) and
                    w > 0 and h > 0 and X >= 0 and Y >= 0 and
                    X + w <= W and Y + h <= H,
                    "invalid painted rectangle: " + name)
            ext.append((X, Y, X + w, Y + h))
        require(min(z[0] for z in ext) <= x < max(z[2] for z in ext) and
                min(z[1] for z in ext) <= y < max(z[3] for z in ext),
                "pivot outside layer painted bbox: " + name)
    def visit(node, stack, done):
        if node in done:
            return
        require(node not in stack, "cycle in rig hierarchy")
        parent = next(p["parent"] for p in parts if p["id"] == node)
        if parent != "root":
            visit(parent, stack | {node}, done)
        done.add(node)
    done = set()
    for name in names:
        visit(name, set(), done)
    require(data["motion_contract"] == ["idle", "walk", "type", "talk"],
            "missing functional animation contract")
    motions = data["motions"]
    require([m["id"] for m in motions] == data["motion_contract"],
            "authored animation clips do not match motion contract")
    for clip in motions:
        require(0.25 <= clip["duration_seconds"] <= 2.0 and
                1 <= len(clip["joints"]) <= len(parts),
                "invalid motion duration or layer count")
        targets = {track["part"] for track in clip["joints"]}
        require(len(targets) == len(clip["joints"]) and targets <= names,
                "motion targets missing or duplicate")
        for track in clip["joints"]:
            angles = track["angles"]
            require(len(angles) == 5 and angles[0] == angles[-1] and
                    all(type(a) in (float, int) and abs(a) <= 45 for a in angles),
                    "invalid motion keyframe")
    walk = {t["part"]: t["angles"] for t in motions[1]["joints"]}
    require({"upper_leg_l", "upper_leg_r", "lower_leg_l", "lower_leg_r"}
            <= set(walk) and
            all(any(a != 0 for a in walk[name]) for name in (
                "upper_leg_l", "upper_leg_r", "lower_leg_l", "lower_leg_r")) and
            walk["upper_leg_l"][1] * walk["upper_leg_r"][1] < 0,
            "walk requires independently animated knees and opposing legs")
    typing = {t["part"]: t["angles"] for t in motions[2]["joints"]}
    require({"forearm_l", "forearm_r"} <= set(typing) and
            all(any(a != 0 for a in typing[name]) for name in (
                "forearm_l", "forearm_r")), "type requires independently animated forearms")
    code = (root / BUILDER).read_text(encoding="utf-8")
    for required_piece in (
        "AssetDatabase.ImportAsset", "TextureImporter", "FilterMode.Point",
        "TextureImporterCompression.Uncompressed", "mipmapEnabled = false",
        "AnimationUtility.SetEditorCurve", "localEulerAnglesRaw.z",
        "upper_leg_l", "lower_leg_l", "upper_leg_r", "lower_leg_r",
        "forearm_l", "forearm_r", "PrefabUtility.SaveAsPrefabAsset",
        "CreateAnimatorControllerAtPath", "Assets/StartupLife/Art/Source/Characters"
    ):
        require(required_piece in code, "draft builder implementation missing: " + required_piece)
    require("Assets/StartupLife/Scenes/" not in code and
            "Art/Approved" not in code and "BuildSettings" not in code,
            "prototype builder may not write production scene or approval registry")
    return len(parts), len(swatches)


if __name__ == "__main__":
    try:
        layers, swatches = validate()
        print(f"PASS quarantined original C01 source: {layers} layers, {swatches} swatches; "
              "four motion authoring paths. Unity rig/animation and originality NOT verified.")
    except (OSError, KeyError, TypeError, ValueError, AssertionError) as error:
        print("FAIL Golden character draft source: " + str(error), file=sys.stderr)
        sys.exit(1)
