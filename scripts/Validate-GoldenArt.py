#!/usr/bin/env python3
"""Strict, engine-free pixel Golden contract validator; never substitutes Unity/IP approval."""
import json
import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
ART = Path("Assets/StartupLife/Art/_ArtDirection")
REG = Path("Assets/StartupLife/Art/Approved/registry.json")
STYLE = "vn_everyday_pixel_v1"
CATEGORIES = {"golden-character", "golden-home", "golden-office",
              "golden-business", "golden-life-ui"}
CRITERIA = {"style_match", "silhouette", "palette", "anatomy_perspective",
            "vietnam_context", "technical_cleanliness", "mobile_readability",
            "reusability"}


def require(value, reason):
    if not value:
        raise AssertionError(reason)


def load(path):
    return json.loads(path.read_text(encoding="utf-8"))


def theme_color(data, role):
    pattern = (r"(?m)^\s*" + re.escape(role) +
               r": \{r: ([0-9.]+), g: ([0-9.]+), b: ([0-9.]+), a: ([0-9.]+)\}$")
    m = re.search(pattern, data)
    require(m is not None, "theme role missing: " + role)
    require(float(m[4]) == 1.0, "theme alpha: " + role)
    return tuple(round(float(v) * 255) for v in m.groups()[:3])


def csharp_color(data, role):
    m = re.search(r"public Color " + re.escape(role) +
                  r" = new Color32\((\d+),\s*(\d+),\s*(\d+),\s*(\d+)\);", data)
    require(m is not None and m[4] == "255", "builder fallback missing: " + role)
    return tuple(int(x) for x in m.groups()[:3])


def verify(root=ROOT):
    root = Path(root)
    spec = load(root / ART / "art-spec.yaml")
    comp = load(root / ART / "composition.yaml")
    briefs = load(root / ART / "golden-targets.json")
    reg = load(root / REG)
    require(spec["style_id"] == comp["style_id"] == briefs["style_id"] ==
            reg["style_id"] == STYLE, "style drift or hybrid HD/pixel")
    require((spec["schema"], comp["schema"], briefs["schema"], reg["schema"]) ==
            ("startup-life.art-direction.v2", "startup-life.composition.v2",
             "startup-life.golden-targets.v2", "startup-life.approved-assets.v2"),
            "style schema drift")
    require(briefs["art_spec"] == (ART / "art-spec.yaml").as_posix() and
            briefs["composition"] == (ART / "composition.yaml").as_posix() and
            briefs["approved_registry"] == REG.as_posix(), "art authority drift")
    require(spec["status"] == "pixel_technical_contract_not_asset_or_scene_approval" and
            reg["approved_entries"] == [] and
            spec["conditioning"]["approved_golden_ids"] == [] and
            spec["conditioning"]["style_anchor_images"] == [],
            "unverified art approved or registered")
    c = spec["craft"]
    cam = spec["camera"]
    cc = comp["camera"]
    require(c["pixels_per_unit"] == cc["pixels_per_unit"] == 32 and
            c["tile_size_px"] == [32, 32] and
            c["native_character_size_px"] == [128, 160], "pixel family drift")
    require(cam["reference_resolution"] == cc["reference_resolution"] == [360, 640] and
            cam["target_aspect"] == cc["target_aspect"] == "9:16" and
            cam["ortho_size"] == cc["ortho_size"] ==
            cam["reference_resolution"][1] / (2 * c["pixels_per_unit"]),
            "pixel camera framing drift")
    require(cam["render_pipeline"] == "URP_2D" and
            cam["pixel_perfect_component"] == cc["pixel_perfect_component"] ==
            "UnityEngine.Rendering.Universal.PixelPerfectCamera",
            "non URP pixel camera")
    require(cam["ui_rendering"] == "screen_space_overlay_native_resolution_TMP" and
            cc["native_ui_overlay"] is True and
            comp["ui_policy"]["native_resolution_tmp"] is True and
            comp["ui_policy"]["safe_area_required"] is True and
            comp["ui_policy"]["screen_space_overlay"] is True,
            "low res UI/TMP")
    require(c["filter_mode"] == "Point" and c["mip_maps"] is False and
            c["texture_compression"].startswith("Uncompressed") and
            c["atlas_padding_px"] >= 4 and c["atlas_tight_packing"] is False and
            cam["msaa"] is False and cam["dynamic_resolution"] is False,
            "pixel import sampling drift")
    actor = spec["characters"]
    require(actor["rig_requires_independent_joint_motion"] is True and
            actor["rigid_sprite_translation_does_not_count_as_walk"] is True and
            {"walk", "idle", "talk", "type", "study", "carry", "kiosk_serve", "transition"}
            <= set(actor["allowed_motions"]), "fake joint animation")
    low, high = comp["framing"]["character_vertical_screen_share_pct"]
    require(low <= 100 * c["native_character_size_px"][1] /
            cam["reference_resolution"][1] <= high, "character not readable at pixel scale")
    require(spec["mobile_budget"]["ui_touch_target_reference_px"] >= 96 and
            comp["ui_policy"]["touch_target_reference_px"] >= 96, "touch gate weakened")
    require(c["light_direction"] == comp["shadow_and_contact"]["key_light_direction"],
            "lighting drift")
    theme = (root / spec["palette"]["master_source"]).read_text(encoding="utf-8")
    defaults = (root / "Assets/StartupLife/Scripts/Presentation/MobileTheme.cs").read_text(
        encoding="utf-8")
    for role in ("background", "panel", "ink", "accent"):
        value = spec["palette"]["roles"][role]
        require(re.fullmatch(r"#[0-9A-Fa-f]{6}", value) is not None,
                "malformed semantic color")
        reference = tuple(int(value[i:i + 2], 16) for i in (1, 3, 5))
        require(reference == theme_color(theme, role) == csharp_color(defaults, role),
                "palette drift: " + role)
    for role, alias in (("panel", "ui_panel"), ("ink", "ui_ink"),
                        ("accent", "ui_accent")):
        require(comp["color_zones"][alias] == spec["palette"]["roles"][role],
                "composition palette drift")
    scenes, targets = comp["scenes"], briefs["targets"]
    require(len(scenes) == len(targets) == len(CATEGORIES) and
            {x["id"] for x in scenes} == {x["id"] for x in targets} ==
            CATEGORIES, "Golden targets missing or duplicate")
    for scene in scenes:
        loc = scene["locality"]
        require(scene["camera_profile"] == "urp_pixel_orthographic_portrait" and
                bool(scene["pixel_grammar"]) and
                2 <= len(loc["local_anchors"]) <= 4 and
                bool(loc["text_strategy"]) and bool(loc["prohibited_stereotypes"]),
                "scene/locality drift: " + scene["id"])
    policy = briefs["approval_policy"]
    require(policy["golden_threshold"] >= 15 and policy["total_max"] == 16 and
            policy["no_zero"] is True and
            policy["requires_human_or_separate_art_director_review"] is True and
            len(policy["criteria"]) == 8 and set(policy["criteria"]) == CRITERIA,
            "Golden threshold weakened")
    for target in targets:
        require(target["asset_state"] == "brief" and target["approved"] is False and
                target["source_path"] is None and target["unity_path"] is None and
                target["scene"] == target["id"] and target["min_candidates"] >= 8 and
                2 <= len(target["local_anchors"]) <= 4,
                "unverified Golden asset: " + target["id"])
    require("hd_chibi_or_pixel_mix" in spec["rejection"] and
            "text_baked_into_background" in spec["rejection"],
            "old/new art or fake text rejection disabled")
    return len(targets)


if __name__ == "__main__":
    try:
        count = verify()
        print(f"PASS pixel Golden technical contract: {count} empty briefs; "
              "no Unity screenshot, rig or IP clearance implied.")
    except (OSError, KeyError, TypeError, ValueError, AssertionError) as err:
        print("FAIL pixel Golden contract: " + str(err), file=sys.stderr)
        sys.exit(1)
