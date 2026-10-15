#!/usr/bin/env python3
"""Character Creator uses only an explicitly approved, user-supplied rig.

Static source and mock .NET tests are NOT the art approval gate or Unity visual QA.
"""
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
BRIDGE=Path("Assets/StartupLife/Scripts/Presentation/CharacterCreatorVisualPreviewBridge.cs")
VIEW=Path("Assets/StartupLife/Scripts/Presentation/CharacterCreationViewController.cs")
REGISTRY=Path("Assets/StartupLife/Art/Approved/registry.json")

def verify(root=ROOT):
    import json
    root=Path(root)
    source=(root/BRIDGE).read_text(encoding="utf8")
    creator=(root/VIEW).read_text(encoding="utf8")
    approved=json.loads((root/REGISTRY).read_text(encoding="utf8"))
    assert approved["approved_entries"]==[], "Asset registry changed without owner approval"
    for guard in ("public bool TryShow(CharacterLookDraft draft)",
                  "assembler!.Apply(plan)",
                  "if (!bodyPresent)",
                  "HideRenderedSprites();",
                  "bakedSkinToneId",
                  "bakedHairColorId",
                  "CharacterVisualSlot.Appearance",
                  "public bool SetView(CharacterRigView view)",
                  "public bool SetSceneContext(CharacterSceneContext context, string? careerId)",
                  "public void Hide()",
                  "IsShowing = false;"):
        assert guard in source, "Missing fail-closed preview contract: "+guard
    for prohibited in ("new GameObject(", "Instantiate(", "Resources.Load",
                       "Addressables.Load", "AssetDatabase.", "File.Read",
                       "File.Write", "GameCommand", "GameSession", "SaveStore"):
        assert prohibited not in source, "Unsafe preview production action: "+prohibited
    assert "AttachApprovedPreview" in creator
    assert "approvedPreview?.TryShow(draft.Draft)" in creator
    assert "approvedPreview?.Hide()" in creator
    return 13

if __name__=="__main__":
    print("PASS character preview bridge approval/quarantine source contract:",verify(),
          "(Unity graphics/asset clearance still required)")
