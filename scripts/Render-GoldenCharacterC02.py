#!/usr/bin/env python3
"""Draw original C02 pixel chibi explorations from first-party authored geometry.

Nine unapproved candidates in 3 distinct silhouette families and four
NON-MIRRORED views. Each of the 21 rig layers is separately rasterizable.
Deterministic stdlib-only PNG; NEVER load a third-party image/photo/sprite.
This is a source/rig art draft, NOT a Unity import, approved character or
on-device motion/BeautyCell evidence.
"""
import argparse
import json
import math
import sys
from pathlib import Path
sys.path.insert(0, str(Path(__file__).resolve().parent))
from PixelPng import png_rgba

ROOT = Path(__file__).resolve().parents[1]
SOURCE = Path("Assets/StartupLife/Art/Source/Characters/golden-character-c02/explorations.json")
REGISTRY = Path("Assets/StartupLife/Art/Approved/registry.json")
TRANSPARENT = b"\x00\x00\x00\x00"


def rgba(code):
    if len(code) != 7 or not code.startswith("#"):
        raise ValueError("Invalid first-party RGB swatch")
    return bytes.fromhex(code[1:]) + b"\xff"


def shapes(source, variant, view):
    """Independent bottom-left pixel rectangles per semantic rig part."""
    concept = next(c for c in source["concepts"] if c["id"] == variant["concept"])
    c, depth = view["cx"], view["depth"]
    head_c = c + view["head_dx"]
    hw = view["head_w"]
    layers = {part["id"]: [] for part in source["rig"]}
    def paint(name, box, tone):
        x,y,w,h = box
        if not (type(x) is int and type(y) is int and
                type(w) is int and type(h) is int and
                w > 0 and h > 0 and 0 <= x < 128 and 0 <= y < 160 and
                x + w <= 128 and y + h <= 160):
            raise ValueError("Outside source pixel canvas: " + name + " " + repr(box))
        layers[name].append((x,y,w,h,tone))

    # Independent leg pivots: thigh -> lower leg -> shoe, not a rigid slide.
    for side in ("l","r"):
        offset = (-13 if side == "l" else 13)
        if depth == 2: offset = (-6 if side == "l" else 5)
        knee_x = c + offset
        paint("upper_leg_"+side,(knee_x-7,32,14,27),"pants")
        paint("upper_leg_"+side,(knee_x-7,33,3,19),"pants_shadow")
        paint("lower_leg_"+side,(knee_x-7,12,14,25),"pants")
        paint("lower_leg_"+side,(knee_x+4,12,3,20),"pants_shadow")
        paint("foot_"+side,(knee_x-10,5,20,11),"shoe")
        paint("foot_"+side,(knee_x-10,5,20,3),"ink")
    paint("pelvis",(c-20,52,40,15),"pants")
    paint("pelvis",(c-20,52,40,3),"pants_shadow")

    width=concept["shoulder_width"]
    paint("torso",(c-width//2,68,width,30),"skin_base")
    paint("torso",(c-6,95,12,7),"skin_base")
    paint("outfit_torso",(c-width//2-2,68,width+4,27),"shirt_base")
    paint("outfit_torso",(c-width//2-2,68,5,23),"shirt_shadow")
    paint("outfit_torso",(c+width//2-3,68,5,24),"shirt_shadow")
    paint("outfit_torso",(c-3,77,6,17),"shirt_light")
    if concept["outerwear"]=="soft_cardigan":
        paint("outfit_torso",(c-5,73,3,22),"shirt_shadow")
        paint("outfit_torso",(c+5,73,3,22),"shirt_shadow")
        paint("outfit_torso",(c-1,74,2,4),"accent")
    elif concept["outerwear"]=="rolled_sleeve":
        paint("outfit_torso",(c-width//2,81,7,5),"shirt_light")
        paint("outfit_torso",(c+width//2-7,81,7,5),"shirt_light")
    else:
        paint("outfit_torso",(c-1,70,2,26),"ink")
        paint("outfit_torso",(c+2,83,3,4),"accent")
    if depth==3:
        paint("outfit_torso",(c-2,80,4,13),"shirt_shadow")

    for side in ("l","r"):
        sign = -1 if side=="l" else 1
        shoulder=c+sign*(width//2+3)
        if depth == 2 and side=="r": shoulder-=3
        paint("upper_arm_"+side,(shoulder-6,66,12,24),"shirt_base")
        paint("upper_arm_"+side,(shoulder-6,66,3,19),"shirt_shadow")
        paint("forearm_"+side,(shoulder-5,52,11,20),"skin_base")
        paint("forearm_"+side,(shoulder-5,61,4,10),"skin_light")
        paint("hand_"+side,(shoulder-6,46,12,12),"skin_base")
        paint("hand_"+side,(shoulder-2,51,5,5),"skin_light")

    hx=head_c-hw//2
    paint("head",(hx,92,hw,57),"skin_base")
    paint("head",(hx+4,91,hw-8,9),"skin_base")
    paint("head",(hx+5,111,hw-10,30),"skin_light")
    paint("head",(hx+2,101,5,29),"skin_deep")
    # Distinct side profile nose is authored, not a flipped frontal sprite.
    if depth==2:
        paint("head",(hx-4,116,7,9),"skin_base")
        paint("head",(hx-3,113,4,4),"skin_light")
    elif depth==1:
        paint("head",(hx-3,118,6,7),"skin_base")

    paint("hair_back",(hx-3,140,hw+6,17),"hair_base")
    paint("hair_back",(hx-3,138,7,16),"hair_shadow")
    paint("hair_back",(hx+hw-3,137,8,16),"hair_shadow")
    # Original differentiating silhouette passes across all four projections.
    if concept["hair"]=="angled_fringe":
        paint("hair_back",(hx-5,138,7,19),"hair_base")
        paint("hair_front",(hx+1,142,hw-8,10),"hair_base")
        paint("hair_front",(hx+4,139,20 if depth!=2 else 13,8),"hair_shadow")
        paint("hair_front",(hx+21,143,13,9),"hair_light")
    elif concept["hair"]=="looped_curl":
        for i in range(5):
            bx=hx-3+i*(hw//5)
            paint("hair_back",(bx,149+(i%2)*2,12,6),"hair_base")
            paint("hair_front",(bx+1,140+(i%2)*3,10,10),"hair_base")
        paint("hair_front",(hx+11,147,9,5),"hair_light")
    else:
        paint("hair_back",(hx+11,151,hw-17,6),"hair_base")
        paint("hair_back",(hx+18,149,14,8),"hair_light")
        paint("hair_front",(hx-2,144,hw+4,8),"hair_base")
        paint("hair_front",(hx+6,142,14,7),"hair_shadow")

    if depth==0:
        for offset in (-11,10):
            paint("face",(head_c+offset-2,120,4,5),"ink")
        paint("face",(head_c-4,107,8,2),"skin_deep")
        paint("face",(head_c-19,112,5,2),"cheek")
        paint("face",(head_c+14,112,5,2),"cheek")
    elif depth==1:
        paint("face",(head_c-10,121,4,5),"ink")
        paint("face",(head_c+8,122,3,4),"ink")
        paint("face",(head_c-6,108,7,2),"skin_deep")
        paint("face",(head_c-17,112,6,2),"cheek")
    elif depth==2:
        paint("face",(head_c-12,121,4,5),"ink")
        paint("face",(head_c-16,111,6,2),"skin_deep")
        paint("face",(head_c-23,114,5,2),"cheek")
    else:
        # Back is separately authored: no eyes, smile or front cheek symbols.
        paint("hair_back",(hx+8,134,hw-18,7),"hair_light")

    # Commuter accessories contain no third-party trademarks/text.
    if concept["carry"]=="diagonal_satchel":
        if depth!=3:
            for j in range(8):
                x=c+11-j*3 if depth!=2 else c+5-j*2
                paint("accessory_front",(x,85-j*3,3,6),"accent")
            paint("accessory_front",(c+10,56,15,13),"accent")
            paint("accessory_front",(c+10,56,15,3),"ink")
        else:
            paint("accessory_back",(c-20,65,10,22),"accent")
            paint("accessory_back",(c-17,87,5,12),"ink")
    elif concept["carry"]=="canvas_tote":
        bag_x=c-32 if depth!=2 else c-29
        paint("accessory_front" if depth!=3 else "accessory_back",
              (bag_x,44,18,24),"accent")
        paint("accessory_front" if depth!=3 else "accessory_back",
              (bag_x+4,61,10,13),"ink")
        paint("accessory_front" if depth!=3 else "accessory_back",
              (bag_x+7,63,4,11),"shirt_base")
    else:
        paint("accessory_back",(c+width//2-1,62,14,26),"accent")
        paint("accessory_back",(c+width//2+7,62,4,22),"ink")
        if depth==3:
            paint("accessory_front",(c-14,67,28,23),"accent")
            paint("accessory_front",(c-12,73,24,2),"ink")

    return layers


def render_part(rects, swatches, width=128, height=160):
    pixels = bytearray(TRANSPARENT * (width * height))
    for x,y,w,h,tone in rects:
        if tone not in swatches:
            raise ValueError("Unknown authored swatch: " + tone)
        rgba_pixel = rgba(swatches[tone])
        for yy in range(y, y+h):
            line = (height - yy - 1) * width * 4
            for xx in range(x,x+w):
                start=line + xx*4
                pixels[start:start+4]=rgba_pixel
    return pixels


def compose(parts, order, swatches):
    pixels=bytearray(TRANSPARENT*(128*160))
    for name in order:
        layer=render_part(parts[name],swatches)
        for offset in range(0,len(layer),4):
            if layer[offset+3]:
                pixels[offset:offset+4]=layer[offset:offset+4]
    return pixels


def coverage(pixels):
    points=[(i % 128,159 - i//128) for i in range(128*160) if pixels[i*4+3]]
    if not points: raise ValueError("Empty C02 exploration")
    return {"pixels":len(points),
            "bbox":[min(x for x,y in points),min(y for x,y in points),
                    max(x for x,y in points),max(y for x,y in points)]}


def png_canvas(pixels,w=128,h=160):
    return png_rgba(w,h,[pixels[y*w*4:(y+1)*w*4] for y in range(h)])


def build(root=ROOT, output=None, export_layers=None):
    root=Path(root)
    source=json.loads((root/SOURCE).read_text(encoding="utf-8"))
    if source["status"]!="DRAFT_NOT_APPROVED":
        raise ValueError("Draft cannot self-promote to approval")
    all_colors=source["palette"]
    ordering=[x["id"] for x in source["rig"]]
    # One sheet: candidate rows, individually authored view columns.
    scale=2
    gap=12
    tile_w,tile_h=128*scale,160*scale
    sheet_w=gap+4*(tile_w+gap)
    sheet_h=gap+9*(tile_h+gap)
    background=rgba("#F6EBD4")
    canvas=bytearray(background*(sheet_w*sheet_h))
    report=[]
    for index,candidate in enumerate(source["variants"]):
        swatches=dict(all_colors)
        swatches.update(candidate["tones"])
        for col,view in enumerate(source["views"]):
            parts=shapes(source,candidate,view)
            image=compose(parts,ordering,swatches)
            stat=coverage(image)
            report.append({"candidate":candidate["id"],"view":view["id"],**stat})
            px=gap+col*(tile_w+gap)
            py=gap+index*(tile_h+gap)
            for y in range(160):
                for x in range(128):
                    src=(y*128+x)*4
                    if image[src+3]==0: continue
                    for ky in range(scale):
                        for kx in range(scale):
                            dst=((py+y*scale+ky)*sheet_w +px+x*scale+kx)*4
                            canvas[dst:dst+4]=image[src:src+4]
            if export_layers:
                folder=Path(export_layers)/candidate["id"]/view["id"]
                folder.mkdir(parents=True,exist_ok=True)
                for name in ordering:
                    p=render_part(parts[name],swatches)
                    (folder/(name+".png")).write_bytes(png_canvas(p))
    if output:
        output=Path(output)
        output.parent.mkdir(parents=True,exist_ok=True)
        output.write_bytes(png_rgba(sheet_w,sheet_h,
            (canvas[y*sheet_w*4:(y+1)*sheet_w*4] for y in range(sheet_h))))
    return {"schema":"startup-life.character-source-preview.v1",
            "status":"DRAFT_NOT_APPROVED","candidates":9,"view_count":4,
            "panel_size":[sheet_w,sheet_h],"samples":report}


def main():
    parser=argparse.ArgumentParser()
    parser.add_argument("--output",required=True)
    parser.add_argument("--report")
    parser.add_argument("--export-layers")
    args=parser.parse_args()
    report=build(output=args.output,export_layers=args.export_layers)
    if args.report:
        path=Path(args.report)
        path.parent.mkdir(parents=True,exist_ok=True)
        path.write_text(json.dumps(report,indent=2)+"\n",encoding="utf-8")
    print("PASS C02 authored 9 x 4 view source contact sheet; UNAPPROVED. "+
          "No Unity import, device BeautyCell or independent originality clearance.")


if __name__=="__main__":
    main()
