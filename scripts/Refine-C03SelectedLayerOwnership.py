#!/usr/bin/env python3
"""Refine C03 selected-image cutout ownership without modifying visible pixels.

The first authoring pass placed 14,025 *unassigned* source-visible pixels in
the torso as fallback, accidentally attaching bits of shoes/face/arms to the
spine. This step reassigns ONLY that recorded fallback to nearest already
authored semantic masks. No new pixel color, anatomy, native redraw or hidden
body parts are invented. This remains NON-SHIPPING REVIEW EVIDENCE.
"""
import argparse
from collections import Counter, deque
import hashlib
import importlib.util
import json
from pathlib import Path

from PIL import Image, ImageDraw

ROOT=Path(__file__).resolve().parents[1]
REVIEW=ROOT/"docs/evidence/production-art/drafts/c03-owner-selected-layer-study"
STRESS=ROOT/"scripts/Preview-C03OwnerCutoutArticulation.py"
REGION_TORSO=((240,339),(376,340),(405,486),(225,486))
W,H=1122,1402
IDENTITY_SHA="1999322a19bf99a5954edc3cf4fd87f6c6b9795006781c0c46ee4ff49fdc30fa"


def load_art_qa():
    spec=importlib.util.spec_from_file_location("c03_selected_pose_review",STRESS)
    module=importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def digest(raw):
    return hashlib.sha256(raw).hexdigest()


def source_torso_mask():
    mask=Image.new("L",(W,H))
    ImageDraw.Draw(mask).polygon(
        [(round(x*W/576),round(y*H/720)) for x,y in REGION_TORSO],
        fill=255)
    return mask.tobytes()


def redistribute(existing,order,canonical,receipt):
    """Only touch proven torso-fallback pixels, preserving RGBA bind composite."""
    if canonical.size!=(W,H) or len(order)!=21 or len(set(order))!=21:
        raise ValueError("Selected C03 authored image/part grid drift")
    if set(existing)!=set(order):
        raise ValueError("C03 source semantic layers differ from declared contract")
    pixels=W*H
    fg=canonical.getchannel("A").tobytes()
    old_alpha={key:existing[key].getchannel("A").tobytes() for key in order}
    polygon=source_torso_mask()
    clean_torso=bytes(
        old_alpha["torso"][i] if polygon[i] else 0 for i in range(pixels))
    masks=dict(old_alpha)
    masks["torso"]=clean_torso

    # Order is back-to-front. The visible owning layer has precedence where
    # multiple old draft regions overlap; this matches the Unity sort order.
    owner=bytearray(pixels)
    for label,name in enumerate(order,1):
        alpha=masks[name]
        for pos,value in enumerate(alpha):
            if value:
                owner[pos]=label

    remaining=bytearray(1 if fg[i] and owner[i]==0 else 0
                        for i in range(pixels))
    baseline_count=sum(remaining)
    if baseline_count!=receipt["unassigned_foreground_fallback_px"] or baseline_count<=0:
        raise ValueError("C03 fallback mismatch; refusing unrelated pixel reassignment")
    # Deterministic nearest-neighbor multi-source flood. Only pixel sites
    # inside the exact owner's visible silhouette can be reassigned.
    todo=deque()
    for i,value in enumerate(remaining):
        if not value:
            continue
        neighbors=[]
        if i%W and owner[i-1]: neighbors.append(owner[i-1])
        if i%W<W-1 and owner[i+1]:neighbors.append(owner[i+1])
        if i>=W and owner[i-W]:neighbors.append(owner[i-W])
        if i+W<pixels and owner[i+W]:neighbors.append(owner[i+W])
        if neighbors:
            count=Counter(neighbors)
            owner[i]=max(count,key=lambda key:(count[key],key))
            remaining[i]=0
            todo.append(i)
    while todo:
        i=todo.popleft()
        for neighbor in ((i-1 if i%W else -1),
                         (i+1 if i%W<W-1 else -1),
                         (i-W if i>=W else -1),
                         (i+W if i+W<pixels else -1)):
            if neighbor>=0 and remaining[neighbor]:
                owner[neighbor]=owner[i]
                remaining[neighbor]=0
                todo.append(neighbor)
    if any(remaining):
        raise ValueError("C03 isolated residual cannot safely attach to an existing body part")

    reassigned={name:0 for name in order}
    result={}
    for label,name in enumerate(order,1):
        alpha=bytearray(masks[name])
        for i,assigned in enumerate(owner):
            if assigned==label and fg[i] and not alpha[i]:
                alpha[i]=fg[i]
                reassigned[name]+=1
        # Every present pixel comes from the same owner-selected reference;
        # do not resynthesize RGB values or resample opaque art.
        source=canonical.copy()
        source.putalpha(Image.frombytes("L",(W,H),bytes(alpha)))
        layer=Image.new("RGBA",(W,H))
        layer.alpha_composite(source)
        if layer.getbbox() is None:
            raise ValueError("Refined semantic layer unexpectedly empty: "+name)
        result[name]=layer
    if sum(reassigned.values())!=baseline_count:
        raise ValueError("C03 reassignment did not conserve visible source pixel count")
    merged=Image.new("RGBA",(W,H))
    for name in order:
        merged.alpha_composite(result[name])
    if merged.tobytes()!=canonical.tobytes():
        raise AssertionError("C03 refined layers changed the owner's visible source composite")
    if result["torso"].getbbox()[3]>round(505*H/720):
        raise AssertionError("C03 torso still extends unexpectedly into legs/shoes")
    return result,merged,baseline_count,reassigned


def compare_pose(a,old,new,joints,assignments):
    names=("left_arm_bend","walk_leg_A")
    view=Image.new("RGB",(2*400+40,4*430+60),(232,230,225))
    d=ImageDraw.Draw(view)
    for row,name in enumerate(names):
        angles=a.POSES[name]
        for col,parts in enumerate((old,new)):
            frame=a.render(list(parts),parts,joints,assignments,angles)
            fitted=frame.copy()
            fitted.thumbnail((360,375),Image.Resampling.NEAREST)
            square=Image.new("RGBA",(380,380),(246,240,229,255))
            square.alpha_composite(fitted,((380-fitted.width)//2,(380-fitted.height)//2))
            x=20+col*400
            y=20+row*430
            view.paste(square.convert("RGB"),(x,y+24))
            d.text((x,y+3),name+" / "+("before" if col==0 else "reassigned"),fill=(48,47,54))
    return view.crop((0,0,840,930))


def run(output_dir):
    output_dir=Path(output_dir)
    if output_dir.exists():
        raise FileExistsError("Do not overwrite previous C03 artist correction draft")
    stress=load_art_qa()
    order,old,joints,ownership,receipt=stress.checked_inputs()
    if receipt["reference_sha256"]!=IDENTITY_SHA:
        raise ValueError("Wrong C03 selected image")
    with Image.open(REVIEW/"C03-selected-cutout-composite-DRAFT.png") as image:
        canonical=image.convert("RGBA")
    refined,merged,count,redistributed=redistribute(old,order,canonical,receipt)
    (output_dir/"layers"/"masks").mkdir(parents=True)
    hashes={}
    for name in order:
        file=output_dir/"layers"/(name+".png")
        refined[name].save(file)
        hashes["layers/"+name+".png"]=digest(file.read_bytes())
    mask=REVIEW/"layers/masks/bag_body.png"
    target=output_dir/"layers/masks/bag_body.png"
    target.write_bytes(mask.read_bytes())
    hashes["layers/masks/bag_body.png"]=digest(target.read_bytes())
    if hashes["layers/masks/bag_body.png"]!=receipt["file_sha256"]["layers/masks/bag_body.png"]:
        raise AssertionError("C03 bag mask must stay unchanged")
    merged.save(output_dir/"C03-refined-layer-ownership-composite.png")
    compare_pose(stress,old,refined,joints,ownership).save(
        output_dir/"C03-refined-ownership-before-after-stress.png")
    # New authored hidden anatomy cannot be inferred here: retain
    # rig quarantine and require explicit artist decisions for seams.
    result={
        "schema":"startup-life.c03.source-visible-layer-ownership-refinement.v1",
        "status":"DRAFT_SOURCE_CORRECTION_NOT_NATIVE_OR_ANIMATABLE",
        "canonical_selected_sha256":IDENTITY_SHA,
        "original_review_source":"docs/evidence/production-art/drafts/c03-owner-selected-layer-study",
        "selected_source_grid":[W,H],"preserved_semantic_layers":21,
        "preserved_joint_topology":16,"original_fallback_px":count,
        "fallback_px_assigned_by_semantic_part":redistributed,
        "torso_bbox_before":list(old["torso"].getbbox()),
        "torso_bbox_after":list(refined["torso"].getbbox()),
        "composite_identical_to_original_selected_source_cutout":True,
        "pixel_colors_changed":False,"native_128x160_art":False,
        "painted_hidden_limb_pixels":False,
        "motion_or_unity_verified":False,"owner_approved":False,
        "approved_for_gameplay":False,"per_file_sha256":hashes
    }
    (output_dir/"C03-refined-layer-ownership-receipt.json").write_text(
        json.dumps(result,ensure_ascii=False,indent=2)+"\n",encoding="utf-8")
    return result


if __name__=="__main__":
    cli=argparse.ArgumentParser(description=__doc__)
    cli.add_argument("--output-dir",required=True,type=Path)
    args=cli.parse_args()
    r=run(args.output_dir)
    print(json.dumps({"status":r["status"],"fallback_px":r["original_fallback_px"],
                      "torso_before":r["torso_bbox_before"],
                      "torso_after":r["torso_bbox_after"],
                      "selected_composite_preserved":r["composite_identical_to_original_selected_source_cutout"]},
                     indent=2))
