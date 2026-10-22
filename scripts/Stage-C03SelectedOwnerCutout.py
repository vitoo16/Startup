#!/usr/bin/env python3
"""Non-destructive 21-slot C03 owner-image *cutout study*, not a native sprite rig.

Uses only the exact 2026-10-21 Git LFS visual selected by the owner. Source
pixels and dimensions are preserved: NO downsampling, repainting or made-up
character design. The opaque background is removed and the visible character
is separated into manually outlined semantic regions. Hidden anatomy/joint
motion cannot be recovered from a flattened illustration. These outputs are
ONLY for artist correction and human review; not FirstPlayable or Art/Approved.
"""
import argparse
import hashlib
import json
from pathlib import Path

from PIL import Image, ImageChops, ImageDraw, ImageFilter, ImageOps

ROOT = Path(__file__).resolve().parents[1]
REF = ROOT / "Assets/StartupLife/Art/Source/Characters/golden-character-c03/references/C03-hand-behind-bag-reference.png"
RECORD = ROOT / "docs/evidence/production-art/C03-LATEST-MINOR-POLISH-REFERENCE.json"
CONTRACT = ROOT / "docs/evidence/production-art/C03-ACTIVE-OWNER-FRONT-RIG-CONTRACT.json"
SHA = "1999322a19bf99a5954edc3cf4fd87f6c6b9795006781c0c46ee4ff49fdc30fa"
W, H = 1122, 1402
STATUS = "REVIEW_ONLY_OPAQUE_IMAGE_EXTRACTION_NOT_NATIVE_RIG"
MASK_NAME = "masks/bag_body.png"

# Curated anatomical regions in an image-relative 576x720 authoring grid.
# These are editable CUT-OUT GUIDES, not traced/approved pivots or original
# hidden anatomy. No old V3 pixels and no image-gen replacement are used.
REGIONS = {
    "hair_back": [[171,131],[179,114],[213,98],[248,90],[291,86],[338,101],[384,124],[412,153],[407,237],[366,258],[339,225],[232,236],[187,249],[165,207]],
    "accessory_back": [[245,347],[271,345],[294,383],[282,410],[257,379]],
    "foot_l": [[194,609],[296,609],[299,656],[180,658],[179,634]],
    "foot_r": [[315,612],[419,608],[446,650],[446,662],[317,663]],
    "lower_leg_l": [[212,538],[297,537],[295,630],[208,632]],
    "lower_leg_r": [[304,536],[404,535],[409,629],[313,632]],
    "upper_leg_l": [[216,490],[301,486],[301,567],[217,571]],
    "upper_leg_r": [[300,486],[410,490],[412,571],[303,570]],
    "pelvis": [[213,480],[401,476],[420,535],[307,532],[302,544],[290,536],[211,531]],
    "torso": [[240,339],[376,340],[405,486],[225,486]],
    "upper_arm_l": [[220,358],[270,361],[268,447],[197,458],[185,422]],
    "upper_arm_r": [[353,357],[397,360],[424,417],[419,464],[370,454]],
    "forearm_l": [[184,410],[242,432],[238,502],[168,512]],
    "forearm_r": [[386,426],[433,421],[441,503],[385,505]],
    "hand_l": [[163,495],[229,493],[229,555],[167,553]],
    "hand_r": [[377,490],[449,491],[449,557],[381,557]],
    "outfit_torso": [[208,383],[250,350],[285,344],[268,464],[274,493],[204,495],[186,440]],
    "outfit_torso_right": [[347,345],[388,350],[417,392],[438,478],[384,500],[364,481]],
    "head": [[213,172],[371,164],[409,188],[415,242],[433,263],[418,304],[385,342],[360,356],[298,357],[258,343],[214,311],[178,277],[188,217]],
    "face": [[231,218],[367,217],[384,265],[376,330],[317,344],[254,327],[223,287]],
    "hair_front": [[172,157],[186,126],[228,101],[277,88],[323,93],[365,102],[398,137],[405,182],[386,234],[356,218],[346,176],[319,178],[291,197],[251,211],[203,240],[171,213]],
    "accessory_front": [[347,441],[359,439],[414,484],[399,498]],
    "bag_body": [[358,466],[445,460],[450,540],[360,545]],
}
# Strap is traced as an open, 3-7px-wide line, separate from bag-body mask.
STRAP = [(253,347),(267,361),(292,395),(314,424),(343,460),(365,483)]


def _scale(point):
    return (round(point[0] * W / 576), round(point[1] * H / 720))


def _poly(points):
    out = Image.new("L", (W, H))
    ImageDraw.Draw(out).polygon([_scale(p) for p in points], fill=255)
    return out


def read_selected():
    record = json.loads(RECORD.read_text(encoding="utf-8"))["latest_reference"]
    contract = json.loads(CONTRACT.read_text(encoding="utf-8"))
    if (record["sha256"] != SHA or contract["selected_concept"]["image_sha256"] != SHA
            or record["expected_repo_draft_path"] != REF.relative_to(ROOT).as_posix()):
        raise ValueError("C03 selected source record/contract drift")
    data = REF.read_bytes()
    if hashlib.sha256(data).hexdigest() != SHA:
        raise ValueError("Exact owner C03 Git LFS image absent or different")
    with Image.open(REF) as raw:
        if raw.format != "PNG" or raw.mode != "RGBA" or raw.size != (W, H):
            raise ValueError("Expected exact 1122x1402 RGBA selected owner PNG")
        return raw.copy(), contract


def visible_silhouette(source):
    """Separate cream matte by closed-contour tracing, without recoloring art."""
    # Background is light beige. Threshold identifies the dark continuous
    # contour; fill enclosed shirt/skin regions rather than deleting cream shirt.
    bg = source.getpixel((0, 0))[:3]
    rgb = source.convert("RGB")
    channels = ImageChops.difference(rgb, Image.new("RGB", rgb.size, bg)).split()
    different = ImageChops.lighter(ImageChops.lighter(channels[0], channels[1]), channels[2])
    body = different.point(lambda v: 255 if v >= 38 else 0)
    body = body.filter(ImageFilter.MaxFilter(7)).filter(ImageFilter.MinFilter(7))
    # Clip to drawn person's known broad bounding envelope to discard
    # vignette/background noise; never fill the entire opaque source rectangle.
    crop = Image.new("L", (W, H))
    ImageDraw.Draw(crop).rectangle((round(150*W/576),round(67*H/720),
                                    round(471*W/576),round(679*H/720)), fill=255)
    body = ImageChops.multiply(body, crop)
    # Fill interior shirt and skin holes. Border floodfill labels OUTSIDE only.
    filled = body.copy()
    ImageDraw.floodfill(filled, (0, 0), 128, thresh=0)
    return filled.point(lambda v: 0 if v == 128 else 255)


def extract(source, contract):
    order = contract["rig_parts_back_to_front"]
    if len(order) != 21 or "accessory_front" not in order:
        raise ValueError("Existing C03 21-layer identity changed")
    foreground = visible_silhouette(source)
    polygons = {key: _poly(points) for key, points in REGIONS.items()}
    # The handbag front covers the near hand. Keep its source pixels separate.
    bag = polygons["bag_body"]
    strap = Image.new("L", (W, H))
    d = ImageDraw.Draw(strap)
    d.line([_scale(p) for p in STRAP], fill=255, width=max(8, round(W/100)))
    polygons["accessory_front"] = ImageChops.lighter(
        polygons["accessory_front"], ImageChops.lighter(bag, strap))
    polygons["outfit_torso"] = ImageChops.lighter(
        polygons["outfit_torso"], polygons["outfit_torso_right"])

    # Guarantee source-visible pixels are represented without changing colors.
    # Unassigned tail of the source is kept on torso backing in this cutout
    # study, explicitly reported for future human anatomy authoring.
    coverage = Image.new("L", (W, H))
    for name in order:
        coverage = ImageChops.lighter(coverage, polygons[name])
    uncovered = ImageChops.subtract(foreground, coverage)
    uncovered_count = sum(bool(a) for a in uncovered.getdata())
    masks = {name: ImageChops.multiply(polygons[name], foreground) for name in order}
    masks["torso"] = ImageChops.lighter(masks["torso"], uncovered)

    layers = {}
    for name in order:
        sprite = source.copy()
        sprite.putalpha(masks[name])
        layers[name] = sprite
    merged = Image.new("RGBA", source.size)
    for name in order:
        merged.alpha_composite(layers[name])
    exact_foreground = source.copy()
    exact_foreground.putalpha(foreground)
    # Invisible RGB values are deliberately discarded by alpha-compositing
    # BOTH sides over the same transparent canvas. Visible RGBA must match
    # exactly, not merely approximate the author's image colors.
    expected_composite = Image.new("RGBA", source.size)
    expected_composite.alpha_composite(exact_foreground)
    if merged.tobytes() != expected_composite.tobytes():
        raise AssertionError("Cutout compositor cannot reconstruct selected source-visible RGB pixels")
    for name, image in layers.items():
        if image.getbbox() is None:
            raise ValueError("Missing visible pixels for proposed C03 layer: " + name)
    body_mask = ImageChops.multiply(bag, foreground)
    return layers, merged, body_mask, uncovered_count, foreground


def preview(merged, layers, order):
    matte = Image.new("RGBA", merged.size, (245, 240, 229, 255))
    matte.alpha_composite(merged)
    matte.thumbnail((410, 560), Image.Resampling.LANCZOS)
    width, height = 6*190, 4*220
    sheet = Image.new("RGB", (width, height), (244, 241, 235))
    draw = ImageDraw.Draw(sheet)
    for i, name in enumerate(order):
        spr = layers[name].copy()
        bounds = spr.getbbox()
        if bounds:
            spr = spr.crop(bounds)
        spr.thumbnail((160, 162), Image.Resampling.NEAREST)
        x, y = (i%6)*190, (i//6)*220
        tile = Image.new("RGBA", (190, 190), (244, 241, 235, 255))
        tile.alpha_composite(spr, ((190-spr.width)//2, (185-spr.height)//2))
        sheet.paste(tile.convert("RGB"), (x, y))
        draw.text((x+7,y+193), name, fill=(50,50,56))
    return matte, sheet


def stage(output_dir):
    output_dir = Path(output_dir)
    if output_dir.exists():
        raise FileExistsError("Never overwrite a prior owner-image cutout draft: " + str(output_dir))
    source, contract = read_selected()
    layers, merged, bag_mask, uncovered, foreground = extract(source, contract)
    (output_dir/"layers"/"masks").mkdir(parents=True)
    files = {}
    for name in contract["rig_parts_back_to_front"]:
        target = output_dir/"layers"/(name+".png")
        layers[name].save(target)
        files[target.relative_to(output_dir).as_posix()] = hashlib.sha256(target.read_bytes()).hexdigest()
    mask_target = output_dir/"layers"/"masks"/"bag_body.png"
    Image.merge("RGBA", (bag_mask,bag_mask,bag_mask,bag_mask)).save(mask_target)
    files[mask_target.relative_to(output_dir).as_posix()] = hashlib.sha256(mask_target.read_bytes()).hexdigest()
    merged.save(output_dir/"C03-selected-cutout-composite-DRAFT.png")
    main, sheet = preview(merged, layers, contract["rig_parts_back_to_front"])
    main.save(output_dir/"C03-selected-cutout-preview-DRAFT.png")
    sheet.save(output_dir/"C03-selected-21-layer-contact-sheet-DRAFT.png")
    pixelcount = sum(bool(v) for v in foreground.getdata())
    receipt = {
        "schema": "startup-life.c03.selected-image-cutout-study.v1",
        "status": STATUS,
        "reference_sha256": SHA,
        "reference_canvas": [W,H],
        "layer_count": len(layers),
        "joint_topology_slots_preserved": 16,
        "semantic_order": contract["rig_parts_back_to_front"],
        "file_sha256": files,
        "composite_matches_visible_source_pixels": True,
        "unassigned_foreground_fallback_px": uncovered,
        "foreground_opaque_pixels": pixelcount,
        "native_pixel_128x160": False,
        "human_corrected_layer_seams": False,
        "occluded_joint_anatomy_authored": False,
        "unity_usable_articulated_rig": False,
        "approved_for_gameplay": False,
        "no_legacy_v3_sprite_replacement": True,
        "next_step": "Artist refine boundaries/hidden joints and rebuild native 128x160 from exact selected reference; preserve 16-joint/21-slot hierarchy."
    }
    (output_dir/"C03-selected-cutout-receipt.json").write_text(
        json.dumps(receipt, ensure_ascii=False, indent=2)+"\n", encoding="utf-8")
    return receipt


if __name__=="__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--output-dir", required=True, type=Path)
    args = parser.parse_args()
    report = stage(args.output_dir)
    print(json.dumps({key:report[key] for key in ("status","reference_sha256",
        "layer_count","composite_matches_visible_source_pixels",
        "unassigned_foreground_fallback_px","foreground_opaque_pixels")},indent=2))
