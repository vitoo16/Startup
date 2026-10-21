#!/usr/bin/env python3
"""Read-only readiness diagnosis for the owner's exact selected C03 illustration.

This does NOT promote or import an asset, create PNGs, alter Unity scene/saves
or assume that a validated concept image is an articulated gameplay sprite.
"""
import argparse
import hashlib
import json
from pathlib import Path
import struct

ROOT = Path(__file__).resolve().parents[1]
REFERENCE = Path("docs/evidence/production-art/C03-LATEST-MINOR-POLISH-REFERENCE.json")
REGISTRY = Path("Assets/StartupLife/Art/Approved/registry.json")
BRIDGE = Path("Assets/StartupLife/Scripts/Presentation/CharacterCreatorVisualPreviewBridge.cs")
CREATOR = Path("Assets/StartupLife/Scripts/Presentation/CharacterCreationViewController.cs")
PREFAB = Path("Assets/StartupLife/Art/Source/Characters/golden-character-c03/Generated-V3-Quarantine/C03_IDLE_V3_QUARANTINED.prefab")
PNG_HEADER = b"\x89PNG\r\n\x1a\n"


def png_dimensions(path: Path):
    with path.open("rb") as handle:
        header = handle.read(24)
    if len(header) != 24 or not header.startswith(PNG_HEADER) or header[12:16] != b"IHDR":
        raise ValueError("Not a PNG with a valid IHDR prefix")
    return list(struct.unpack(">II", header[16:24]))


def diagnose(root: Path = ROOT, candidate: Path = None):
    root = Path(root)
    record = json.loads((root / REFERENCE).read_text(encoding="utf-8"))
    latest = record["latest_reference"]
    expected = root / latest["expected_repo_draft_path"]
    selected = Path(candidate) if candidate is not None else expected
    registry = json.loads((root / REGISTRY).read_text(encoding="utf-8"))
    approved = registry.get("approved_entries", [])
    result = {
        "character": "C03",
        "selected_visual_reference": latest["filename"],
        "expected_sha256": latest["sha256"],
        "expected_dimensions": [latest["width"], latest["height"]],
        "repository_source_path": latest["expected_repo_draft_path"],
        "tested_source": str(selected),
        "source_file_present": selected.is_file(),
        "source_sha256_matches": False,
        "source_dimensions_match": False,
        "approved_asset_count": len(approved),
        "quarantined_draft_prefab_present": (root / PREFAB).is_file(),
        "approved_preview_bridge_source_present": (root / BRIDGE).is_file(),
        "creator_source_present": (root / CREATOR).is_file(),
        "gameplay_character_ready": False,
        "unity_runtime_verified": False,
        "blocking_reasons": []
    }
    if not selected.is_file():
        result["blocking_reasons"].append("Selected original PNG missing; reference documentation is not an image.")
    else:
        actual_sha = hashlib.sha256(selected.read_bytes()).hexdigest()
        result["actual_sha256"] = actual_sha
        result["source_sha256_matches"] = actual_sha == latest["sha256"]
        try:
            actual_dimensions = png_dimensions(selected)
        except ValueError as error:
            actual_dimensions = None
            result["blocking_reasons"].append(str(error))
        result["actual_dimensions"] = actual_dimensions
        result["source_dimensions_match"] = actual_dimensions == result["expected_dimensions"]
        if not result["source_sha256_matches"]:
            result["blocking_reasons"].append("PNG SHA-256 differs from the owner-approved visual reference.")
        if actual_dimensions is not None and not result["source_dimensions_match"]:
            result["blocking_reasons"].append("PNG dimensions differ from the selected visual reference.")
    if not approved:
        result["blocking_reasons"].append("Art/Approved/registry.json has no shipping character assets.")
    if not result["quarantined_draft_prefab_present"]:
        result["blocking_reasons"].append("No locally imported C03 draft proof prefab; offline staging is not Unity import.")
    result["blocking_reasons"].append(
        "A reference PNG is not a gameplay-ready transparent, authored 21-part rig, "
        "nor does it configure the Creator preview bridge.")
    result["reference_verified"] = (
        result["source_file_present"] and result["source_sha256_matches"]
        and result["source_dimensions_match"]
    )
    result["status"] = ("REFERENCE_VERIFIED_NOT_GAMEPLAY_READY" if result["reference_verified"]
                        else "MISSING_OR_INCORRECT_REFERENCE")
    return result


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--source", type=Path, help="Check an exact owner-selected PNG outside the repository")
    parser.add_argument("--root", type=Path, default=ROOT, help=argparse.SUPPRESS)
    args = parser.parse_args()
    report = diagnose(args.root, args.source)
    print(json.dumps(report, ensure_ascii=False, indent=2))
    return 0 if report["reference_verified"] else 2


if __name__ == "__main__":
    raise SystemExit(main())
