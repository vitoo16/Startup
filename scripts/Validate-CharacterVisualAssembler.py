#!/usr/bin/env python3
"""Check Unity visual assembler source contract without claiming Unity runtime success."""
import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
ASSEMBLER = ROOT / "Assets/StartupLife/Scripts/Presentation/CharacterVisualAssembler.cs"
UNITY_TESTS = ROOT / "Assets/StartupLife/Tests/EditMode/CharacterVisualAssemblerTests.cs"
APPROVED = ROOT / "Assets/StartupLife/Art/Approved/registry.json"


def check(contents, approved_text):
    must = [
        "public interface IApprovedCharacterSpriteResolver",
        "public sealed class CharacterVisualAssembler : MonoBehaviour",
        "public void Configure(",
        "public bool Apply(CharacterVisualPlan plan)",
        "public void Clear()",
        "part.Renderer.transform.IsChildOf(transform)",
        "Array.Clear(stagedSprites, 0, stagedSprites.Length)",
        "TryResolveApprovedSprite(layer.SpriteAssetId, out var sprite)",
        "texture.filterMode != FilterMode.Point",
        "texture.mipmapCount != 1",
        "var changed = false;",
        "renderer.sprite != sprite",
        "renderer.enabled != visible",
        "renderer.sortingOrder != stagedOrders[i]"
    ]
    for required in must:
        if required not in contents:
            raise AssertionError("Visual assembler invariant missing: " + required)
    prohibited = [
        "new GameObject(", "Instantiate(", "Destroy(", "DestroyImmediate(",
        "AssetDatabase.", "Resources.Load", "Addressables.Load",
        "File.Read", "File.Write", "GameCommand", "GameSession", "SendMessage("
    ]
    for fragment in prohibited:
        if fragment in contents:
            raise AssertionError("Visual assembler forbidden production action: " + fragment)
    if contents.find("TryResolveApprovedSprite(layer.SpriteAssetId") > contents.find(
        "renderer.sprite = sprite;"):
        raise AssertionError("Sprite assignment before verified preflight")
    if '"approved_entries": []' not in approved_text:
        raise AssertionError("The approved art registry unexpectedly changed")
    return len(must)


def verify(root=ROOT):
    import json
    from pathlib import Path
    root = Path(root)
    contents = (root / ASSEMBLER.relative_to(ROOT)).read_text(encoding="utf-8")
    approved_text = (root / APPROVED.relative_to(ROOT)).read_text(encoding="utf-8")
    tests = (root / UNITY_TESTS.relative_to(ROOT)).read_text(encoding="utf-8")
    count = check(contents, approved_text)
    for case in (
        "WorkOutfitSwapsOnlyOneRendererWithoutRecreatingRig",
        "MissingSpriteFailsBeforeAnyLayerChanges",
        "ConfigureRejectsExternalOrDuplicateRendererAndDoesNotTouchRig",
        "ClearDisablesOnlySpriteSlotsButPreservesJointTransforms"
    ):
        if case not in tests:
            raise AssertionError("Missing Unity EditMode test contract: " + case)
    if json.loads(approved_text)["approved_entries"]:
        raise AssertionError("No asset may be auto-approved in this slice")
    return count


if __name__ == "__main__":
    try:
        count = verify()
        print(f"PASS Unity assembler static contract: {count} invariants; "
              "EditMode/PlayMode/device not executed.")
    except (OSError, AssertionError, ValueError) as error:
        print("FAIL Unity assembler static contract: " + str(error), file=sys.stderr)
        sys.exit(1)
