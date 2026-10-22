#!/usr/bin/env python3
"""Generate honest joint-rotation stress evidence for the selected C03 cutout.

Read-only: the 21 PNGs and 16 rough joints must be source-faithful Git LFS
drafts. This does NOT repair hidden limb anatomy, draw native 128x160 pixels,
make shipping clips or certify visual/IP/mobile QA. It intentionally shows
where old flattened pixels tear when articulated before cleanup.
"""
import argparse
import hashlib
import json
import math
from pathlib import Path

from PIL import Image, ImageChops, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[1]
DRAFT = ROOT / "docs/evidence/production-art/drafts/c03-owner-selected-layer-study"
SOURCE = ROOT / "Assets/StartupLife/Art/Source/Characters/golden-character-c03/references/C03-hand-behind-bag-reference.png"
EXPECTED = "1999322a19bf99a5954edc3cf4fd87f6c6b9795006781c0c46ee4ff49fdc30fa"
W, H = 1122, 1402
VIEW = (336, 420)
POSES = {
    "bind_exact": {},
    "left_arm_bend": {"shoulder_l": -7, "elbow_l": 13, "wrist_l": -8, "neck": -3},
    "right_arm_bend": {"shoulder_r": 7, "elbow_r": -13, "wrist_r": 9, "neck": 3},
    "walk_leg_A": {"hip_l": 8, "knee_l": -9, "ankle_l": 3,
                   "hip_r": -7, "knee_r": 9, "ankle_r": -3},
    "walk_leg_B": {"hip_l": -7, "knee_l": 9, "ankle_l": -3,
                   "hip_r": 8, "knee_r": -9, "ankle_r": 3},
}


def digest(data):
    return hashlib.sha256(data).hexdigest()


def checked_inputs(root=ROOT):
    root = Path(root)
    folder = root / DRAFT.relative_to(ROOT)
    ref = root / SOURCE.relative_to(ROOT)
    if not ref.is_file() or digest(ref.read_bytes()) != EXPECTED:
        raise ValueError("C03 owner-selected Git LFS source is absent or wrong")
    receipt = json.loads((folder / "C03-selected-cutout-receipt.json").read_text(encoding="utf-8"))
    guide = json.loads((folder / "C03-selected-16-joint-guide-DRAFT.json").read_text(encoding="utf-8"))
    if (receipt.get("status") != "REVIEW_ONLY_OPAQUE_IMAGE_EXTRACTION_NOT_NATIVE_RIG" or
            receipt.get("reference_sha256") != EXPECTED or
            receipt.get("layer_count") != 21 or
            receipt.get("native_pixel_128x160") is not False or
            receipt.get("approved_for_gameplay") is not False or
            guide.get("status") != "PIVOT_ESTIMATES_ONLY_NOT_NATIVE_NOT_ANIMATABLE" or
            guide.get("joint_count") != 16 or guide.get("rig_approval") is not False or
            len(guide.get("joints", [])) != 16):
        raise ValueError("C03 draft rig source or status drifted")
    order = receipt["semantic_order"]
    joint_list = guide["joints"]
    if len(order) != 21 or len(set(order)) != 21:
        raise ValueError("Wrong C03 semantic slot count")
    joints = {j["id"]: j for j in joint_list}
    if len(joints) != 16 or set(order) != {
            part for j in joint_list for part in j.get("parts", [])}:
        raise ValueError("C03 guide changed the 16 joint / 21 part mapping")
    if joints["root"]["parent"] is not None:
        raise ValueError("C03 root must remain parentless")
    layers = {}
    declared = receipt["file_sha256"]
    if len(declared) != 22 or set(declared) != {
            "layers/" + name + ".png" for name in order} | {"layers/masks/bag_body.png"}:
        raise ValueError("C03 receipt does not cover exactly 22 source art files")
    for name in order:
        rel = "layers/" + name + ".png"
        path = folder / rel
        if digest(path.read_bytes()) != declared[rel]:
            raise ValueError("C03 imported draft layer SHA differs: " + name)
        with Image.open(path) as src:
            if src.format != "PNG" or src.mode != "RGBA" or src.size != (W, H):
                raise ValueError("C03 draft layer has wrong native metadata: " + name)
            layers[name] = src.copy()
    mask = folder / "layers/masks/bag_body.png"
    if digest(mask.read_bytes()) != declared["layers/masks/bag_body.png"]:
        raise ValueError("C03 bag body mask differs")
    part_owner = {}
    for bone in joint_list:
        pivot = bone["pivot_native_estimate_xy"]
        if len(pivot) != 2 or any(type(z) is not int for z in pivot) or not (
                0 <= pivot[0] < 128 and 0 <= pivot[1] < 160):
            raise ValueError("Malformed C03 guide joint pivot")
        for part in bone["parts"]:
            if part in part_owner:
                raise ValueError("Duplicate part owner: " + part)
            part_owner[part] = bone["id"]
    if len(part_owner) != 21 or part_owner["hand_r"] != "wrist_r" or (
            part_owner["accessory_front"] != "spine"):
        raise ValueError("C03 bags/hands must use preserved semantic joints")
    return order, layers, joints, part_owner, receipt


# Affine coefficients map x,y to a*x+b*y+c, d*x+e*y+f.
IDENTITY = (1., 0., 0., 0., 1., 0.)


def combine(a, b):
    return (a[0]*b[0]+a[1]*b[3], a[0]*b[1]+a[1]*b[4],
            a[0]*b[2]+a[1]*b[5]+a[2],
            a[3]*b[0]+a[4]*b[3], a[3]*b[1]+a[4]*b[4],
            a[3]*b[2]+a[4]*b[5]+a[5])


def about(p, degrees):
    radians = math.radians(degrees)
    c, s = math.cos(radians), math.sin(radians)
    x, y = p
    return (c, -s, x-c*x+s*y, s, c, y-s*x-c*y)


def inverse(m):
    a,b,c,d,e,f = m
    det = a*e-b*d
    if abs(det) < .000001:
        raise ValueError("Singular C03 joint transform")
    return (e/det,-b/det,(b*f-e*c)/det,
            -d/det,a/det,(d*c-a*f)/det)


def joint_matrices(joints, angles):
    positions = {}
    unresolved = set(joints)
    for _ in range(len(joints)):
        for name in list(unresolved):
            joint = joints[name]
            parent = joint["parent"]
            if parent is not None and parent not in positions:
                continue
            if parent is None and name != "root":
                raise ValueError("Extra C03 root joint")
            pivot = joint["pivot_native_estimate_xy"]
            xy = (pivot[0] * W/128, pivot[1] * H/160)
            a = float(angles.get(name,0))
            if abs(a) > 20 or name == "root" and a != 0:
                raise ValueError("C03 stress-test cannot move root or exceed 20 degrees")
            positions[name] = combine(positions[parent] if parent else IDENTITY,
                                      about(xy, a))
            unresolved.remove(name)
    if unresolved:
        raise ValueError("Cycle or missing parent in C03 guide")
    return positions


def render(order, layers, joints, owner, angles):
    matrices = joint_matrices(joints, angles)
    canvas = Image.new("RGBA", (W, H))
    for part in order:
        matrix = matrices[owner[part]]
        if all(abs(matrix[i]-IDENTITY[i]) < 1e-9 for i in range(6)):
            frame = layers[part]
        else:
            frame = layers[part].transform(
                (W,H), Image.Transform.AFFINE, inverse(matrix),
                resample=Image.Resampling.NEAREST, fillcolor=(0,0,0,0))
        canvas.alpha_composite(frame)
    return canvas


def changed_count(a, b):
    diff = ImageChops.difference(a,b).convert("RGBA")
    return sum(v > 0 for v in diff.convert("RGB").getchannel("R").getdata()) + sum(
        v > 0 for v in diff.getchannel("A").getdata())


def make_sheet(images):
    tw,th = VIEW
    out = Image.new("RGB", (5*(tw+14)+20, th+100), "#e7e4de")
    d = ImageDraw.Draw(out)
    font = ImageFont.load_default(size=17)
    small = ImageFont.load_default(size=14)
    for idx, (name, src) in enumerate(images.items()):
        cell = Image.new("RGBA", VIEW, "#f7f2e9")
        size = src.copy()
        size.thumbnail((tw-30,th-35),Image.Resampling.NEAREST)
        cell.alpha_composite(size,((tw-size.width)//2,(th-size.height)//2))
        x = 15+idx*(tw+14)
        out.paste(cell.convert("RGB"), (x,36))
        d.text((x,10),name,fill="#35353a",font=font)
    d.text((20,th+49),
           "DIAGNOSTIC JOINT ROTATION — seams/hidden anatomy NOT APPROVED; no production motion",
           fill="#963a30",font=small)
    d.text((20,th+71),
           "Exact selected-image pixels at bind pose; generated poses show where cleanup is required",
           fill="#35353a",font=small)
    return out


def run(out,root=ROOT):
    out = Path(out)
    if out.exists():
        raise FileExistsError("Never overwrite prior C03 pose QA outputs: "+str(out))
    order,layers,joints,owner,receipt=checked_inputs(root)
    frames = {name:render(order,layers,joints,owner,angles)
              for name,angles in POSES.items()}
    baseline = frames["bind_exact"]
    with Image.open(Path(root)/DRAFT.relative_to(ROOT)/"C03-selected-cutout-composite-DRAFT.png") as source:
        if baseline.tobytes() != source.convert("RGBA").tobytes():
            raise ValueError("Bind pose no longer preserves the selected-image pixel composite")
    out.mkdir(parents=True)
    for name,im in frames.items():
        im.save(out/("C03-"+name+"-NOT-APPROVED.png"))
    make_sheet(frames).save(out/"C03-joint-stress-side-by-side-NOT-APPROVED.png")
    indicators = {}
    for name,im in frames.items():
        if name == "bind_exact":
            continue
        indicators[name] = {
            "diagnostic_angles_degrees":POSES[name],
            "pixel_rgba_change_proxy":changed_count(baseline,im)
        }
    summary = {
        "schema":"startup-life.c03.selected-cutout-stress-review.v1",
        "status":"DEFECT_DISCOVERY_ONLY_NOT_ANIMATION_OR_APPROVAL",
        "selected_image_sha256":EXPECTED,
        "preserved_joints":16,"preserved_layers":21,
        "bind_pose_matches_selected_source_cutout":True,
        "test_rotations_not_production_animation":True,
        "native_sprite_authoring":False,
        "visual_evaluation":"HUMAN_REVIEW_REQUIRED",
        "art_cleanup_needed":["hair/face anatomical partition","left and right cuffs/hands",
                              "shirt/jacket boundary","bag strap/bag obscured right wrist",
                              "leg/foot overlap","paint hidden anatomy under joints"],
        "pose_changes":indicators,
        "not_authorized_for_ArtApproved_or_FirstPlayable":True
    }
    (out/"C03-stress-review.json").write_text(json.dumps(summary,indent=2)+"\n",encoding="utf-8")
    return summary


if __name__ == "__main__":
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--output-dir",type=Path,required=True)
    args=parser.parse_args()
    report=run(args.output_dir)
    print(json.dumps({"status":report["status"],
        "selected_image_sha256":report["selected_image_sha256"],
        "poses":list(report["pose_changes"])},indent=2))
