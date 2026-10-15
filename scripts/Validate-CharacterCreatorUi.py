#!/usr/bin/env python3
"""Preflight data-driven Character Creator uGUI migration; NOT device/Unity proof."""
import json
import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
VIEW = Path("Assets/StartupLife/Scripts/Presentation/CharacterCreationViewController.cs")
BUILDER = Path("Assets/StartupLife/Scripts/Editor/FirstPlayableShellBuilder.cs")
GATE = Path("Assets/StartupLife/Scripts/Editor/FirstPlayableRuntimeGate.cs")
DATA = Path("Assets/StartupLife/Data/CharacterCreatorDraftCatalog.json")

def verify(root=ROOT):
    root = Path(root)
    view = (root / VIEW).read_text(encoding="utf-8")
    build = (root / BUILDER).read_text(encoding="utf-8")
    gate = (root / GATE).read_text(encoding="utf-8")
    data = json.loads((root / DATA).read_text(encoding="utf-8"))
    for token in ("ParseDraft(draftCatalog)", "CharacterCreatorDraftSession",
                  "creatorScroll", "identityPage", "lookPage", "wardrobePage",
                  "traitsPage", "draft.Next()", "draft.Previous()"):
        assert token in view, "Missing UI draft contract: " + token
    for name in ("Face", "Hair", "HairColor", "SkinTone", "Outfit", "Accessory", "Trait"):
        assert "Previous"+name+"()" in view and "Next"+name+"()" in view
        assert 'CreatorSelector("'+name+'"' in build, "Missing layout selector "+name
    assert "UpgradeCharacterCreator(characterPanel, character, font, theme);" in build
    assert "UpgradeCharacterCreator(character.gameObject, character, font, theme);" in build
    assert "MobileScrollViewFactory.Create(panel.transform" in build
    assert "var identity = CreatorSection" in build and "look.SetActive(false)" in build
    assert "scroll.Scroll.transform.SetAsFirstSibling()" in build
    assert "CreatorUnsavedNote" in build and "character.cosmetic.unsaved" in build
    assert 'row.name.StartsWith("Creator", StringComparison.Ordinal)' in build
    assert 'Repair assets and Character Creator' in build
    assert "var life = UnityEngine.Object.FindAnyObjectByType<LifeScreenViewController>(" in build
    assert "FindObjectsInactive.Include)" in build.split(
        "var life = UnityEngine.Object.FindAnyObjectByType<LifeScreenViewController>(", 1
    )[1].split(";", 1)[0], "Inactive LifeScreen migration regression"
    assert "FirstPlayableShellBuilder.RepairGeneratedAssetReferences();" in gate
    assert "CharacterCreatorDraftCatalog.json" in gate
    assert "CreatorNextPageButton" in gate and "CreatorTraitNextButton" in gate
    assert "creatorScroll.transform.parent == creatorPanel" in gate
    assert "!submit.transform.IsChildOf(creatorScroll.content)" in gate
    assert "UnityEventTools.AddPersistentListener(create.onClick, character.Submit)" in build
    assert 'var selectedAppearance = draft != null ? draft.Draft.AppearanceId : appearanceId;' in view
    assert "flow.CreateCharacter" in view and "GameSession" not in view
    assert "GameCommand" not in view and "File.Write" not in view
    assert view.count("creatorScroll.verticalNormalizedPosition = 1f;") == 1
    assert "ResetCreatorScroll();" in view
    assert "lastOptionLabelKeys[index] == key" in view
    assert "panel.AddComponent<CharacterCreatorTouchSizer>()" in build
    assert 'Assign(sizer, "uiTheme", theme);' in build
    assert "sizer.RefreshAuthoredBindings();" in build
    assert 'percentage.text = "100%"' in build
    assert "creatorPanel.GetComponent<CharacterCreatorTouchSizer>()" in gate
    assert "UpgradeCreatorTextScaling(panel, font, theme);" in build
    assert build.count("UpgradeCreatorTextScaling(panel, font, theme);") == 2
    assert "CreatorTextSmallerButton" in build and "CreatorTextLargerButton" in build
    assert "UnityEventTools.AddPersistentListener(larger.onClick, scaler.IncreaseTextScale);" in build
    assert "CharacterCreatorTextScaler" in gate and "CreatorTextSizeRow" in gate
    assert "panel.AddComponent<CharacterCreatorKeyboardAvoidance>()" in build
    assert "CharacterCreatorKeyboardAvoidance" in gate and 'CheckReferences(keyboard, "nameInput", "rootCanvas");' in gate
    assert 'Add(table, "character.textsize", "Cỡ chữ");' in build
    assert "page.activeSelf != visible" in view
    assert "ageValue.text != ageLabel" in view
    for option in data["options"]:
        assert option["labelKey"] == "character.option."+option["id"]
    assert 'Add(table, option.labelKey, option.label);' in build
    return len(data["options"])

if __name__=="__main__":
    try: print("PASS UI static contract:", verify(), "localized selectors; Unity/device QA pending")
    except (OSError, AssertionError, ValueError, KeyError) as e:
        print("FAIL Character Creator uGUI contract:",e,file=sys.stderr);sys.exit(1)
