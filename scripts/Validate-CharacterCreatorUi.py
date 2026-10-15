#!/usr/bin/env python3
"""Preflight data-driven Character Creator uGUI migration; NOT device/Unity proof."""
import json
import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
VIEW = Path("Assets/StartupLife/Scripts/Presentation/CharacterCreationViewController.cs")
BUILDER = Path("Assets/StartupLife/Scripts/Editor/FirstPlayableShellBuilder.cs")
DATA = Path("Assets/StartupLife/Data/CharacterCreatorDraftCatalog.json")

def verify(root=ROOT):
    root = Path(root)
    view = (root / VIEW).read_text(encoding="utf-8")
    build = (root / BUILDER).read_text(encoding="utf-8")
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
    assert "UnityEventTools.AddPersistentListener(create.onClick, character.Submit)" in build
    assert 'var selectedAppearance = draft != null ? draft.Draft.AppearanceId : appearanceId;' in view
    assert "flow.CreateCharacter" in view and "GameSession" not in view
    assert "GameCommand" not in view and "File.Write" not in view
    for option in data["options"]:
        assert option["labelKey"] == "character.option."+option["id"]
    assert 'Add(table, option.labelKey, option.label);' in build
    return len(data["options"])

if __name__=="__main__":
    try: print("PASS UI static contract:", verify(), "localized selectors; Unity/device QA pending")
    except (OSError, AssertionError, ValueError, KeyError) as e:
        print("FAIL Character Creator uGUI contract:",e,file=sys.stderr);sys.exit(1)
