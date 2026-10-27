#!/usr/bin/env python3
"""Source-faithful hidden-seam underpaint CANDIDATE for selected C03.

Adds RGB from existing C03 sprite pixels ONLY underneath later, fully opaque
occluding semantic layers. Static selected composite must remain byte-exact.
This is not a native pixel repaint, new character, correct hidden anatomy,
or authorization to animate/ship. It does not modify tracked input files.
"""
import argparse
from collections import deque
import hashlib
import importlib.util
import json
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[1]
DRAFT = ROOT / "docs/evidence/production-art/drafts/c03-owner-selected-layer-refined"
STUDY = ROOT / "docs/evidence/production-art/drafts/c03-owner-selected-layer-study"
VERIFY = ROOT / "scripts/Verify-C03CommittedRefined.py"
POSES = ROOT / "scripts/Preview-C03OwnerCutoutArticulation.py"
SHA = "1999322a19bf99a5954edc3cf4fd87f6c6b9795006781c0c46ee4ff49fdc30fa"
SIZE = (1122, 1402)

# All source pixels copied from the exact selected C03, never another design.
# Front occluder MUST sort later than the underpainted semantic part.
PAIRS = (
    ("foot_l", "lower_leg_l", 17),
    ("foot_r", "lower_leg_r", 17),
    ("lower_leg_l", "upper_leg_l", 22),
    ("lower_leg_r", "upper_leg_r", 22),
    ("upper_leg_l", "pelvis", 22),
    ("upper_leg_r", "pelvis", 22),
    ("upper_arm_l", "forearm_l", 20),
    ("upper_arm_r", "forearm_r", 20),
    ("forearm_l", "hand_l", 15),
    ("forearm_r", "hand_r", 15),
    ("hand_r", "accessory_front", 26),
    ("head", "hair_front", 12),
)


def read_module(path, name):
    spec = importlib.util.spec_from_file_location(name, path)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def sha256(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def underpaint_pair(source, occluder, radius):
    """8-connected fixed-radius color flood into covered transparent texels.

    Only original alpha=0 and later occluder alpha=255 can be changed.
    Deterministic traversal and RGB borrowed solely from the source sprite.
    RGBA input and dimensions stay unchanged. Never treats air as anatomy.
    """
    if source.size != occluder.size or source.mode != "RGBA" or occluder.mode != "RGBA":
        raise ValueError("C03 seam candidate expects equal RGBA source/occluder canvases")
    if type(radius) is not int or radius < 1 or radius > 32:
        raise ValueError("C03 underpaint radius outside limited QA range")
    w, h = source.size
    b = source.getchannel("A").getbbox()
    c = occluder.getchannel("A").getbbox()
    if b is None or c is None:
        raise ValueError("C03 semantic source/occluder must have visible alpha")

    # The scan must include ORIGINAL source pixels to seed the flood.
    # Clipping the ROI to the occluder would discard edge seeds when two
    # anatomy parts merely touch at a seam (e.g. cuff at wrist).
    left = max(0, b[0] - radius)
    top = max(0, b[1] - radius)
    right = min(w, b[2] + radius)
    bottom = min(h, b[3] + radius)
    if right <= c[0] or left >= c[2] or bottom <= c[1] or top >= c[3]:
        return source.copy(), 0

    raw = bytearray(source.tobytes())
    cover = occluder.tobytes()
    stride = right - left
    height = bottom - top
    visited = bytearray(stride * height)
    frontier = deque()
    # Seed all *original* source pixels within the overlapping ROI; only
    # source-border pixels next to the occluder can propagate.
    for y in range(top, bottom):
        for x in range(left, right):
            i = (y - top) * stride + x - left
            p = (y * w + x) * 4
            if raw[p + 3] > 0:
                visited[i] = 1
                frontier.append((x, y, 0))
    if not frontier:
        return source.copy(), 0

    additions = 0
    directions = ((-1, 0), (1, 0), (0, -1), (0, 1),
                  (-1, -1), (-1, 1), (1, -1), (1, 1))
    while frontier:
        x, y, step = frontier.popleft()
        if step >= radius:
            continue
        p = (y * w + x) * 4
        for dx, dy in directions:
            nx, ny = x + dx, y + dy
            if nx < left or nx >= right or ny < top or ny >= bottom:
                continue
            i = (ny - top) * stride + nx - left
            if visited[i]:
                continue
            q = (ny * w + nx) * 4
            if raw[q + 3] != 0 or cover[q + 3] != 255:
                continue
            visited[i] = 1
            raw[q:q + 4] = bytes((raw[p], raw[p + 1], raw[p + 2], 255))
            additions += 1
            frontier.append((nx, ny, step + 1))

    result = Image.frombytes("RGBA", (w, h), bytes(raw))
    return result, additions


def review_sheet(before, after, names, joints, owner, stress):
    pose_names = ("bind_exact", "left_arm_bend", "right_arm_bend", "walk_leg_A")
    tile_w, tile_h = 420, 450
    sheet = Image.new("RGB", (2 * tile_w + 36, len(pose_names) * (tile_h + 30) + 20),
                      (235, 231, 224))
    draw = ImageDraw.Draw(sheet)
    font = ImageFont.load_default(size=15)
    for row, pose in enumerate(pose_names):
        angles = stress.POSES[pose]
        for col, layer_set in enumerate((before, after)):
            frame = stress.render(names, layer_set, joints, owner, angles)
            frame.thumbnail((tile_w - 18, tile_h - 32), Image.Resampling.NEAREST)
            panel = Image.new("RGBA", (tile_w, tile_h), (248, 242, 230, 255))
            panel.alpha_composite(frame, ((tile_w - frame.width)//2,
                                          (tile_h - frame.height)//2))
            x, y = 14 + col * (tile_w + 8), 10 + row * (tile_h + 30)
            sheet.paste(panel.convert("RGB"), (x, y + 19))
            draw.text((x, y), pose + (" / SOURCE REFINED" if col == 0 else
                                    " / HIDDEN UNDERPAINT STUDY"),
                      fill=(54, 50, 49), font=font)
    return sheet


def stage(output):
    output = Path(output)
    if output.exists():
        raise FileExistsError("Refusing overwrite of C03 art correction evidence")
    verifier = read_module(VERIFY, "c03_committed_hidden_gate")
    source_gate = verifier.run()
    if source_gate["status"] != "PASS_REFINE_SOURCE_INTEGRITY_NOT_ART_APPROVAL":
        raise ValueError("C03 refined source Git LFS SHA gate not passed")
    receipt = json.loads((DRAFT / "C03-refined-layer-ownership-receipt.json").read_text())
    old_receipt = json.loads((STUDY / "C03-selected-cutout-receipt.json").read_text())
    guide = json.loads((STUDY / "C03-selected-16-joint-guide-DRAFT.json").read_text())
    names = old_receipt["semantic_order"]
    if (len(names) != 21 or len(set(names)) != 21 or
            receipt["canonical_selected_sha256"] != SHA):
        raise ValueError("C03 selected owner and semantic topology drift")
    for behind, front, radius in PAIRS:
        if names.index(behind) >= names.index(front):
            raise ValueError("C03 hidden underpaint must remain under later sprite")
    before = {}
    for name in names:
        with Image.open(DRAFT / "layers" / (name + ".png")) as im:
            before[name] = im.convert("RGBA")
            if before[name].size != SIZE:
                raise ValueError("C03 refined sprite wrong source canvas")

    after = {name: im.copy() for name, im in before.items()}
    changes = {}
    for behind, front, radius in PAIRS:
        after[behind], added = underpaint_pair(after[behind], after[front], radius)
        changes[behind + "_under_" + front] = {
            "hidden_pixels_extended": added, "max_8_connected_steps": radius
        }
    if sum(value["hidden_pixels_extended"] for value in changes.values()) == 0:
        raise ValueError("No C03 hidden seam pixels authored")

    def comp(layers):
        image = Image.new("RGBA", SIZE)
        for name in names:
            image.alpha_composite(layers[name])
        return image
    original = comp(before)
    candidate = comp(after)
    if original.tobytes() != candidate.tobytes():
        raise AssertionError("Hidden seam study changed approved C03 standing appearance")

    owner = {}
    joints = {j["id"]: j for j in guide["joints"]}
    for joint in guide["joints"]:
        for part in joint["parts"]:
            if part in owner:
                raise ValueError("C03 rig guide duplicate owner")
            owner[part] = joint["id"]
    if len(joints) != 16 or set(owner) != set(names):
        raise ValueError("C03 16/21 joint guide changed")
    stress = read_module(POSES, "c03_underpaint_stress")
    output.mkdir(parents=True)
    (output / "layers" / "masks").mkdir(parents=True)
    hashes = {}
    for name in names:
        path = output / "layers" / (name + ".png")
        after[name].save(path)
        hashes["layers/" + name + ".png"] = sha256(path)
    mask = DRAFT / "layers/masks/bag_body.png"
    target = output / "layers/masks/bag_body.png"
    target.write_bytes(mask.read_bytes())
    hashes["layers/masks/bag_body.png"] = sha256(target)
    if hashes["layers/masks/bag_body.png"] != receipt["per_file_sha256"]["layers/masks/bag_body.png"]:
        raise ValueError("Bag-body reference mask not source identical")
    candidate.save(output / "C03-hidden-underpaint-bind-composite.png")
    review_sheet(before, after, names, joints, owner, stress).save(
        output / "C03-underpaint-pose-before-after-REVIEW.png")
    evidence = {
        "schema": "startup-life.c03.hidden-underpaint-candidate.v1",
        "status": "ARTIST_REVIEW_REQUIRED_NOT_ANIMATABLE_OR_APPROVED",
        "owner_selected_sha256": SHA,
        "refined_sha_receipt": "docs/evidence/production-art/drafts/c03-owner-selected-layer-refined/C03-refined-layer-ownership-receipt.json",
        "semantic_layer_count": 21,
        "joint_topology_count": 16,
        "mask_count": 1,
        "new_hidden_pixel_count": sum(z["hidden_pixels_extended"] for z in changes.values()),
        "underpaint_pairs": changes,
        "static_composite_exact_rgba": True,
        "same_source_palette_for_hidden_extension": True,
        "new_face_or_character": False,
        "real_hidden_anatomy_painted_by_artist": False,
        "native_128x160_art": False,
        "rig_and_motion_verified_in_Unity": False,
        "approved_for_gameplay": False,
        "file_sha256": hashes,
        "caution": "Hidden colors are edge copies, not anatomy. Pose previews may introduce visible artifacts; independent artist review required."
    }
    (output / "C03-hidden-underpaint-candidate.json").write_text(
        json.dumps(evidence, indent=2, ensure_ascii=False)+"\n", encoding="utf-8")
    print(json.dumps({"status": evidence["status"],
        "hidden_pixels": evidence["new_hidden_pixel_count"],
        "static_composite_exact": evidence["static_composite_exact_rgba"],
        "output": str(output)}, indent=2))
    return evidence


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--output-dir", required=True, type=Path)
    args = parser.parse_args()
    stage(args.output_dir)
