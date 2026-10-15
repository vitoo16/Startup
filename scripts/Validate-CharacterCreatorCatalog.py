#!/usr/bin/env python3
"""Validate text-only Character Creator choices. Static checks never approve art."""
import json
import sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
PATH=ROOT/"Assets/StartupLife/Data/CharacterCreatorDraftCatalog.json"
def verify(path=PATH):
    d=json.loads(Path(path).read_text(encoding="utf-8"))
    assert d["schema"]=="startup-life.character-creator.draft.v1"
    assert d["status"]=="DRAFT_COSMETIC_OPTIONS_NO_APPROVED_SPRITES"
    slots={"Appearance","Face","Hair","HairColor","SkinTone","Accessory","Outfit","Trait"}
    options=d["options"]
    assert {o["slot"] for o in options}==slots
    assert {x["slot"] for x in d["defaults"]}==slots
    ids={(o["slot"],o["id"]) for o in options}
    assert len(ids)==len(options)
    assert len({x["slot"] for x in d["defaults"]})==len(d["defaults"])
    assert all((x["slot"],x["id"]) in ids for x in d["defaults"])
    assert {x["id"] for x in options if x["slot"]=="Appearance"}=={"base.male","base.female"}
    assert all(o["labelKey"]=="character.option."+o["id"] for o in options)
    assert len({o["labelKey"] for o in options})==len(options)
    assert all(isinstance(o["label"],str) and o["label"].strip() for o in options)
    assert all(set(o)=={"slot","id","labelKey","label"} for o in options)
    return len(options)
if __name__=="__main__":
    try: print("PASS Character Creator",verify(),"text options, no sprites")
    except (OSError,AssertionError,KeyError,ValueError) as e:
        print("FAIL Character Creator catalog:",e,file=sys.stderr)
        sys.exit(1)
