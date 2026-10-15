#!/usr/bin/env python3
"""Strict original C02 nine-candidate/four-view topology and quarantine gate."""
import importlib.util
import json
import math
import sys
from pathlib import Path

ROOT=Path(__file__).resolve().parents[1]
SOURCE=Path("Assets/StartupLife/Art/Source/Characters/golden-character-c02/explorations.json")
REGISTRY=Path("Assets/StartupLife/Art/Approved/registry.json")
ART_SPEC=Path("Assets/StartupLife/Art/_ArtDirection/art-spec.yaml")
RENDER=Path("scripts/Render-GoldenCharacterC02.py")
VIEWS=("front","three_quarter_left","side","back")


def validate(root=ROOT):
    root=Path(root)
    data=json.loads((root/SOURCE).read_text(encoding="utf8"))
    registry=json.loads((root/REGISTRY).read_text(encoding="utf8"))
    spec=json.loads((root/ART_SPEC).read_text(encoding="utf8"))
    assert data["schema"]=="startup-life.pixel-character-explorations.v1"
    assert data["style_id"]==spec["style_id"]==registry["style_id"]=="vn_everyday_pixel_v1"
    assert data["status"]=="DRAFT_NOT_APPROVED"
    assert data["owner_visual_review"]["status"]=="REVISE_REQUIRED", (
        "Owner rejected C02's Westernized/mature-looking art direction")
    assert data["owner_visual_review"]["release_blocked"] is True
    assert data["art_direction"]["ppu"] == 32
    art=data["art_direction"]
    assert art["native_canvas_px"]==[128,160]
    assert art["adult_coded"] and art["gender_independent"] and art["career_independent"]
    low,high=art["figure_vertical_span_px"]
    head_lo,head_hi=art["head_vertical_span_px"]
    actual=(high-low)/(head_hi-head_lo)
    assert 2.2<=actual<=2.4 and abs(actual-art["actual_nominal_heads"])<.005
    assert [v["id"] for v in data["views"]]==list(VIEWS)
    concepts={c["id"]:c for c in data["concepts"]}
    assert len(concepts)==len(data["concepts"])==3
    assert len({(c["hair"],c["outerwear"],c["carry"]) for c in concepts.values()})==3
    assert len(data["variants"])==9 and len({v["id"] for v in data["variants"]})==9
    assert all(sum(variant["concept"]==id for variant in data["variants"])==3
               for id in concepts)
    assert all(variant["iteration"] in (1,2,3) for variant in data["variants"])
    rig={part["id"]:part for part in data["rig"]}
    assert len(rig)==len(data["rig"])>=20
    required={"head","face","hair_back","hair_front","torso","pelvis",
              "upper_arm_l","upper_arm_r","forearm_l","forearm_r","hand_l","hand_r",
              "upper_leg_l","upper_leg_r","lower_leg_l","lower_leg_r",
              "foot_l","foot_r","outfit_torso"}
    assert required<=set(rig)
    for part in rig.values():
        assert part["parent"]=="root" or part["parent"] in rig
        x,y=part["pivot_px"]
        assert 0<=x<128 and 0<=y<160
        visited=set()
        current=part["id"]
        while current!="root":
            assert current not in visited,"Cycle in C02 rig"
            visited.add(current)
            current=rig[current]["parent"]
    for code in data["palette"].values():
        assert len(code)==7 and code[0]=="#"
        bytes.fromhex(code[1:])
    for var in data["variants"]:
        assert set(var["tones"])<=set(data["palette"])
        for code in var["tones"].values():
            assert len(code)==7 and code[0]=="#"
            bytes.fromhex(code[1:])
    assert registry["approved_entries"]==[]
    assert data["approval"]["approved_registry"]==REGISTRY.as_posix()
    assert all(data["approval"][key] is False for key in (
        "approved","independent_art_review","ip_review",
        "unity_import","physical_device_beautycell","animator_verified")), (
        "No automated approval or QA pass may be claimed")
    provenance=json.loads((root/"Assets/StartupLife/Art/_ArtDirection/provenance/manifest.json")
                          .read_text(encoding="utf8"))
    records=[a for a in provenance["assets"] if a["asset_id"]=="sl-character-golden-c02"]
    assert len(records)==1 and records[0]["status"]=="DRAFT"
    assert SOURCE.as_posix() in records[0]["source_files"]
    assert records[0]["unity_path"] is None and records[0]["approval_date_utc"] is None
    # Ensure artwork is a valid layered RGBA raster, not just a manifest.
    spec_obj=importlib.util.spec_from_file_location("golden_c02",root/RENDER)
    module=importlib.util.module_from_spec(spec_obj)
    spec_obj.loader.exec_module(module)
    report=module.build(root)
    assert len(report["samples"])==36
    assert all(1200<=s["pixels"]<=11000 for s in report["samples"])
    assert all(s["bbox"][1]<=5 and s["bbox"][3]>=156 for s in report["samples"])
    assert all(s["bbox"][0]>=8 and s["bbox"][2]<=118 for s in report["samples"])
    # Side/back need separately authored geometry; do not allow silent mirrors.
    for row in range(9):
        four=report["samples"][row*4:row*4+4]
        assert len({tuple(v["bbox"]) for v in four})>=2
    return report


if __name__=="__main__":
    try:
        result=validate()
        print("PASS C02 3 independent design directions, 9 quarantined candidates, "+
              str(len(result["samples"]))+" independently constructed views; NOT approved")
    except (ValueError,KeyError,AssertionError,OSError,TypeError) as error:
        print("FAIL C02 draft: "+str(error),file=sys.stderr)
        sys.exit(1)
