#!/usr/bin/env python3
"""Engine-free validation for Startup Life Golden art direction and asset gates.

The two *.yaml source files intentionally use the JSON subset of YAML 1.2,
enabling strict standard-library validation without runner dependencies.
No output files are written; Unity import and visual quality are separate.
"""
import json
import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
ART = ROOT / "Assets/StartupLife/Art/_ArtDirection"
STYLE = "vietnam_cozy_chibi_cutout_v1"
CATEGORIES = {"golden-character", "golden-home", "golden-office",
              "golden-business", "golden-life-ui"}

def require(condition, message):
    if not condition:
        raise AssertionError(message)

def read_json(path):
    return json.loads(path.read_text(encoding="utf-8"))

def theme_color(text, name):
    pat = r"(?m)^\s*" + re.escape(name) + (
        r": \{r: ([0-9.]+), g: ([0-9.]+), b: ([0-9.]+), a: ([0-9.]+)\}")
    found = re.search(pat, text)
    require(found is not None, "Theme role missing: " + name)
    channels = [int(round(float(s) * 255)) for s in found.groups()[:3]]
    return "#" + "".join(f"{v:02X}" for v in channels)

def verify():
    spec = read_json(ART / "art-spec.yaml")
    composition = read_json(ART / "composition.yaml")
    briefs = read_json(ART / "golden-targets.json")
    registry = read_json(ROOT / "Assets/StartupLife/Art/Approved/registry.json")
    require(spec["style_id"] == composition["style_id"] ==
            briefs["style_id"] == registry["style_id"] == STYLE, "style drift")
    require(briefs["art_spec"] == "Assets/StartupLife/Art/_ArtDirection/art-spec.yaml",
            "wrong canonical art source")
    theme = (ROOT / spec["palette"]["master_source"]).read_text(encoding="utf-8")
    for color in ("background", "panel", "ink", "accent"):
        require(spec["palette"]["roles"][color] == theme_color(theme, color),
                "MobileTheme/art-spec palette mismatch: " + color)
    require(spec["craft"]["pixels_per_unit"] == composition["camera"]["pixels_per_unit"] == 100,
            "pixel density mismatch")
    require(spec["camera"]["ortho_size"] == composition["camera"]["ortho_size"] == 5,
            "camera mismatch")
    require(spec["craft"]["light_direction"] ==
            composition["shadow_and_contact"]["key_light_direction"], "light drift")
    require(spec["camera"]["reference_resolution"] ==
            composition["camera"]["reference_resolution"] == [1080,1920], "portrait source mismatch")
    require(spec["characters"]["rig_must_deform"] and
            spec["characters"]["rigid_sprite_translation_does_not_count_as_walk"],
            "rigged animation requirement missing")
    require(spec["characters"]["chibi_total_heads"] > 2 and
            spec["characters"]["chibi_total_heads"] < 3.1, "chibi silhouette drift")
    require(spec["mobile_budget"]["ui_touch_target_reference_px"] >= 96,
            "undersized touch contract")
    scenes = composition["scenes"]
    targets = briefs["targets"]
    require({s["id"] for s in scenes} == CATEGORIES, "missing composition Golden scenes")
    require({t["id"] for t in targets} == CATEGORIES, "missing asset Golden target")
    for s in scenes:
        anchors = s["locality"]["local_anchors"]
        require(2 <= len(anchors) <= 4, "locality anchor count " + s["id"])
        require(s["locality"]["text_strategy"], "missing sign text policy")
        require(s["camera_profile"] == "ortho_portrait", "camera contract drift")
    required = briefs["approval_policy"]
    require(required["golden_threshold"] >= 15 and required["total_max"] == 16 and
            required["no_zero"] and required["requires_human_or_separate_art_director_review"],
            "approval weakened")
    require(len(required["criteria"]) == 8, "expected 8 scorecard dimensions")
    for target in targets:
        require(target["asset_state"] == "brief" and target["approved"] is False,
                "unverified Golden art falsely approved: " + target["id"])
        require(target["source_path"] is None and target["unity_path"] is None,
                "unimported Golden asset has a runtime reference")
        require(target["min_candidates"] >= 8, "best-of-N gate weakened")
        require(2 <= len(target["local_anchors"]) <= 4, "brief locality count")
        require(target["scene"] == target["id"], "brief/scene mismatch")
    require(not registry["approved_entries"], "registry contains unverified art")
    require("text_baked_into_background" in spec["rejection"], "Vietnamese sign policy missing")
    print("PASS M13 Golden art contracts: 5 targets, palette/scale/camera, "
          "locality, and empty asset registry (no Unity visual acceptance claimed).")

if __name__ == "__main__":
    try:
        verify()
    except (OSError, KeyError, ValueError, AssertionError) as exc:
        print("FAIL M13 Golden art contract: " + str(exc), file=sys.stderr)
        sys.exit(1)
