#!/usr/bin/env python3
"""Static preflight for pixel UI theme consistency and color contrast.

No visual/device or readability approval. Native Unity UI checks remain required.
"""
import importlib.util
import json
import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
ASSET = Path("Assets/StartupLife/Data/MobileTheme.asset")
ART = Path("Assets/StartupLife/Art/_ArtDirection/art-spec.yaml")
VIEW = Path("Assets/StartupLife/Scripts/Presentation/LifeScreenViewController.cs")
SCENE_BUILDER = Path("Assets/StartupLife/Scripts/Editor/FirstPlayableShellBuilder.cs")
UI_FILES = {
    "dashboard": Path("Assets/StartupLife/Scripts/Presentation/LifeDashboardOverlay.cs"),
    "business": Path("Assets/StartupLife/Scripts/Presentation/BusinessManagementOverlay.cs"),
    "event": Path("Assets/StartupLife/Scripts/Presentation/EventChoiceOverlay.cs"),
}

GOLDEN_SPEC = importlib.util.spec_from_file_location(
    "golden_art", ROOT / "scripts/Validate-GoldenArt.py")
golden = importlib.util.module_from_spec(GOLDEN_SPEC)
GOLDEN_SPEC.loader.exec_module(golden)


def check(condition, message):
    if not condition:
        raise AssertionError(message)


def luminance(channels):
    linear = [v / 255 / 12.92 if v / 255 <= 0.04045
              else ((v / 255 + 0.055) / 1.055) ** 2.4 for v in channels]
    return sum(a * b for a, b in zip(linear, (0.2126, 0.7152, 0.0722)))


def contrast(a, b):
    l1, l2 = luminance(a), luminance(b)
    return (max(l1, l2) + 0.05) / (min(l1, l2) + 0.05)


def verify(root=ROOT):
    root = Path(root)
    spec = json.loads((root / ART).read_text(encoding="utf-8"))
    theme = (root / ASSET).read_text(encoding="utf-8")
    palette = {}
    for role in ("background", "panel", "ink", "accent"):
        channels = golden.theme_color(theme, role)
        expected = tuple(int(spec["palette"]["roles"][role][i:i + 2], 16)
                         for i in (1, 3, 5))
        check(channels == expected, "art and UI theme palette drift")
        palette[role] = channels
    check(contrast(palette["ink"], palette["panel"]) >= 7.0,
          "ink/panel is not legible at strong contrast")
    check(contrast(palette["ink"], palette["background"]) >= 7.0,
          "ink/background contrast too low")
    check(contrast((255, 255, 255), palette["accent"]) >= 4.5,
          "white/accent button contrast below AA")
    check(contrast((255, 255, 255), palette["ink"]) >= 4.5,
          "white/selected card contrast below AA")
    view = (root / VIEW).read_text(encoding="utf-8")
    builder = (root / SCENE_BUILDER).read_text(encoding="utf-8")
    check("[SerializeField] private MobileTheme uiTheme;" in view and
          "public MobileTheme UiTheme" in view and
          "ScriptableObject.CreateInstance<MobileTheme>()" in view,
          "life view has no owned theme or historical-scene fallback")
    check('Assign(life, "uiTheme", theme);' in builder and
          builder.count('Assign(life, "uiTheme", theme);') >= 2,
          "scene build/repair does not serialize active theme")
    for name, path in UI_FILES.items():
        source = (root / path).read_text(encoding="utf-8")
        check("theme = " in source and ".UiTheme" in source,
              "overlay does not receive authoritative theme: " + name)
        check("theme.ink" in source and "theme.accent" in source,
              "overlay missing readable foreground/action palette: " + name)
        check("new Color(0.08f, 0.12f, 0.18f" not in source and
              "new Color(0.16f, 0.45f, 0.50f" not in source,
              "legacy HD palette leaked into overlay: " + name)
        check("Art/Approved" not in source and "Art/Source" not in source,
              "UI may not directly load unreviewed sprites: " + name)
    dashboard = (root / UI_FILES["dashboard"]).read_text(encoding="utf-8")
    check("new Color32(244, 239, 226" not in dashboard,
          "old dashboard color family remains")
    business = (root / UI_FILES["business"]).read_text(encoding="utf-8")
    event = (root / UI_FILES["event"]).read_text(encoding="utf-8")
    check("text.color = ink ?? theme.ink;" in business and
          'Color.white);' in business,
          "business text/button contrast semantics drift")
    check("text.color = ink ?? theme.ink;" in event and
          'Color.white);' in event,
          "event text/button contrast semantics drift")
    check("theme.background" in business and "theme.background" in event,
          "scroll surfaces must share pixel theme")
    return {name: round(contrast(a, b), 2) for name, a, b in (
        ("ink_panel", palette["ink"], palette["panel"]),
        ("ink_background", palette["ink"], palette["background"]),
        ("white_accent", (255, 255, 255), palette["accent"]),
        ("white_ink", (255, 255, 255), palette["ink"]))}


if __name__ == "__main__":
    try:
        print("PASS pixel UI semantic-theme preflight: " + str(verify())
              + "; Unity/device typography and focus evidence NOT RUN.")
    except (OSError, KeyError, TypeError, ValueError, AssertionError) as err:
        print("FAIL pixel UI preflight: " + str(err), file=sys.stderr)
        sys.exit(1)
