#!/usr/bin/env python3
"""Validate Startup Life asset provenance and its bidirectional approval registry.

Standalone stdlib preflight. Never certifies originality, commercial rights, Unity
rendering or actual reviewer identity. Historical M13 art validator remains active.
"""
import argparse
import datetime as dt
import json
import re
import sys
from pathlib import Path

HERE = Path(__file__).resolve().parents[1]
ART = Path("Assets/StartupLife/Art")
PROVENANCE = ART / "_ArtDirection/provenance/manifest.json"
SPEC = ART / "_ArtDirection/art-spec.yaml"
REGISTRY = ART / "Approved/registry.json"
PLANNED = "vn_everyday_pixel_v1"
SCHEMA = "startup-life.asset-provenance.v1"
CATEGORIES = {"character", "environment", "ui", "icon", "audio", "font", "marketing"}
CREATORS = {"first_party", "contractor", "licensed_third_party", "ai_assisted"}
STATES = {"DRAFT", "QUARANTINED", "RIGHTS_PENDING", "READY_FOR_ART_QA",
          "APPROVED", "REJECTED", "REVOKED"}
SCORES = ("style_match", "silhouette", "palette", "anatomy_perspective",
          "vietnam_context", "technical_cleanliness", "mobile_readability",
          "reusability")
BASE = ("asset_id", "type", "style_id", "status", "creator_type",
        "creator_or_licensor_ref", "created_utc", "source_files",
        "output_files", "process_reference", "model_tool_and_terms_ref",
        "input_reference_rights", "third_party_dependencies", "license_scope",
        "license_evidence_ref", "similarity_review_ref",
        "technical_import_review_ref", "visual_beautycell_ref",
        "independent_reviewer", "approval_date_utc", "unity_path", "scorecard")
REFS = ("creator_or_licensor_ref", "process_reference", "license_scope",
        "license_evidence_ref", "similarity_review_ref",
        "technical_import_review_ref", "visual_beautycell_ref")


def check(condition, message):
    if not condition:
        raise ValueError(message)


def nonblank(value):
    return isinstance(value, str) and bool(value.strip()) and (
        value.strip().lower() not in {"tbd", "todo", "pending", "none", "null", "n/a"})


def timestamp(value):
    check(nonblank(value), "missing timestamp")
    try:
        dt.datetime.fromisoformat(value.replace("Z", "+00:00"))
    except ValueError as exc:
        raise ValueError("invalid ISO-8601 date: " + value) from exc


def local_file(root, value, prefix=None):
    check(nonblank(value) and "\\" not in value and "://" not in value,
          "invalid repository-relative path")
    path = Path(value)
    check(not path.is_absolute() and ".." not in path.parts, "unsafe asset path: " + value)
    check(prefix is None or path.parts[:len(prefix.parts)] == prefix.parts,
          "asset outside approved import root: " + value)
    check((root / path).is_file(), "file missing: " + value)


def validate(root=HERE):
    root = Path(root)
    manifest = json.loads((root / PROVENANCE).read_text(encoding="utf-8"))
    spec = json.loads((root / SPEC).read_text(encoding="utf-8"))
    registry = json.loads((root / REGISTRY).read_text(encoding="utf-8"))
    check(isinstance(manifest, dict) and manifest.get("schema") == SCHEMA,
          "provenance schema mismatch")
    check(manifest.get("proposed_style_id") == PLANNED, "proposed pixel style drift")
    assets = manifest.get("assets")
    approved = registry.get("approved_entries")
    check(isinstance(assets, list), "assets must be an array")
    check(isinstance(approved, list), "approved_entries must be an array")
    check(isinstance(spec.get("style_id"), str), "active art style missing")
    check(isinstance(registry.get("style_id"), str), "registry style missing")
    check(spec["style_id"] == registry["style_id"], "active spec/registry style drift")
    by_id = {}
    for asset in assets:
        check(isinstance(asset, dict), "asset record must be object")
        check(all(key in asset for key in BASE), "asset record missing required field")
        aid = asset["asset_id"]
        check(nonblank(aid) and re.fullmatch(r"[a-z0-9]+(?:-[a-z0-9]+)*", aid),
              "invalid asset_id")
        check(aid not in by_id, "duplicate asset_id: " + aid)
        by_id[aid] = asset
        check(asset["type"] in CATEGORIES, "unknown asset type: " + aid)
        check(asset["status"] in STATES, "unknown asset status: " + aid)
        check(asset["creator_type"] in CREATORS, "unknown creator type: " + aid)
        check(asset["style_id"] == PLANNED, "non-pixel provenance candidate: " + aid)
        check(nonblank(asset["creator_or_licensor_ref"]), "missing creator: " + aid)
        check(nonblank(asset["process_reference"]), "missing process reference: " + aid)
        timestamp(asset["created_utc"])
        for field in ("source_files", "output_files", "input_reference_rights",
                      "third_party_dependencies"):
            check(isinstance(asset[field], list), field + " must be array: " + aid)
        if asset["status"] != "APPROVED":
            continue
        check(spec["style_id"] == PLANNED, "cannot approve pixel asset before W03 migration")
        for field in REFS:
            check(nonblank(asset[field]), "approval missing " + field + ": " + aid)
        check(nonblank(asset["unity_path"]), "approval missing unity_path: " + aid)
        check(isinstance(asset["source_files"], list) and bool(asset["source_files"]),
              "approval missing editable source: " + aid)
        check(isinstance(asset["output_files"], list) and bool(asset["output_files"]),
              "approval missing imported output: " + aid)
        for path in asset["source_files"]:
            local_file(root, path, ART / "Source")
        for path in asset["output_files"]:
            local_file(root, path, ART / "Approved")
        local_file(root, asset["unity_path"], ART / "Approved")
        check(asset["unity_path"] in asset["output_files"],
              "approved unity_path absent from output_files: " + aid)
        if asset["creator_type"] == "ai_assisted":
            check(nonblank(asset["model_tool_and_terms_ref"]),
                  "AI tool/terms evidence missing: " + aid)
        reviewer = asset["independent_reviewer"]
        check(nonblank(reviewer) and reviewer.startswith("human:") and
              reviewer != asset["creator_or_licensor_ref"],
              "independent human reviewer reference required: " + aid)
        timestamp(asset["approval_date_utc"])
        score = asset["scorecard"]
        check(isinstance(score, dict) and set(score) == set(SCORES),
              "8 score dimensions required: " + aid)
        check(all(type(score[k]) is int and 1 <= score[k] <= 2 for k in SCORES)
              and sum(score.values()) >= 15,
              "Golden score must be >=15/16, without zeros: " + aid)
    registry_ids = set()
    for entry in approved:
        check(isinstance(entry, dict), "approved registry entry must be object")
        aid = entry.get("asset_id")
        check(nonblank(aid) and aid not in registry_ids, "duplicate/invalid approved registry ID")
        registry_ids.add(aid)
        asset = by_id.get(aid)
        check(asset is not None and asset["status"] == "APPROVED",
              "registry has no approved provenance: " + aid)
        check(entry.get("style_id") == asset["style_id"] == registry["style_id"],
              "registry entry style mismatch: " + aid)
        check(entry.get("unity_path") == asset["unity_path"],
              "registry Unity path mismatch: " + aid)
    check(registry_ids == {aid for aid, a in by_id.items() if a["status"] == "APPROVED"},
          "approved provenance missing from registry")
    return len(assets), len(registry_ids), spec["style_id"]


if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("--root", type=Path, default=HERE,
                        help="Repository root; used by deterministic fixture tests")
    args = parser.parse_args()
    try:
        candidates, approved_count, active = validate(args.root)
        print(f"PASS asset provenance preflight: {candidates} candidates, "
              f"{approved_count} approvals, active style {active}. "
              "No legal or visual clearance implied.")
    except (OSError, KeyError, TypeError, ValueError, json.JSONDecodeError) as error:
        print("FAIL asset provenance preflight: " + str(error), file=sys.stderr)
        sys.exit(1)
