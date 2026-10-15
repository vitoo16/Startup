#!/usr/bin/env python3
"""Static, mutation-testable gate for real Unity responsive matrix authoring.

This is intentionally NOT a screenshot or Unity PlayMode verdict.
"""
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
MATRIX = Path("Assets/StartupLife/Tests/PlayMode/CharacterCreatorResponsiveMatrixTests.cs")
GATE = Path("Assets/StartupLife/Scripts/Editor/FirstPlayableRuntimeGate.cs")

def verify(root=ROOT):
    root = Path(root)
    content = (root / MATRIX).read_text(encoding="utf-8")
    gate = (root / GATE).read_text(encoding="utf-8")
    required = (
        "new Vector2Int(360, 640)",
        "new Vector2Int(390, 844)",
        "new Vector2Int(430, 932)",
        "Screen.SetResolution(dimensions.x, dimensions.y, false)",
        "Is.EqualTo(dimensions.x)",
        "Is.EqualTo(dimensions.y)",
        "scaler && scaler.enabled",
        "ScaleMode.ScaleWithScreenSize",
        "textScaler.SetStep(scaleStep)",
        "scaleStep <= 2",
        "creator.NextCreatorPage()",
        "AssertCreatorActions(",
        "scrollBounds.min.y",
        "text.isTextOverflowing",
        "STARTUP_LIFE_CREATOR_RESPONSIVE_EVIDENCE",
        "ScreenCapture.CaptureScreenshotAsTexture()",
        "Screen.SetResolution(originalWidth, originalHeight, originalFullscreen)",
        'Directory.GetFiles(saveDirectory, "startup-life.json*")',
        "File.WriteAllBytes(pair.Key, pair.Value)",
        "DestroyImmediate(bootstrap.gameObject)",
    )
    for token in required:
        if token not in content:
            raise AssertionError("Missing responsive matrix contract: " + token)
    forbidden = (
        "scaler.enabled = false", "canvas.scaleFactor = 1",
        "Assert.Ignore(", "Assert.Inconclusive(", "Assert.Warn("
    )
    for token in forbidden:
        if token in content:
            raise AssertionError("Matrix may not fake a responsive PASS: " + token)
    if "FirstPlayableShellBuilder.RepairGeneratedAssetReferences();\n            VerifyScene();" not in gate:
        raise AssertionError("Editor must verify reopened migrated scene after repair")
    return len(required)

if __name__ == "__main__":
    count = verify()
    print(f"PASS authored responsive matrix source: {count} assertions; "
          "actual Unity/device test NOT RUN")
