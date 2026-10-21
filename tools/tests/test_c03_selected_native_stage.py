"""Engine-free tests for the new C03 artist-authored Unity handoff (not art approval)."""
import copy
import importlib.util
import json
from pathlib import Path
import unittest

ROOT = Path(__file__).resolve().parents[2]
SOURCE = ROOT / "scripts/Stage-C03SelectedNativeRig.py"
CONTRACT = ROOT / "docs/evidence/production-art/C03-ACTIVE-OWNER-FRONT-RIG-CONTRACT.json"
HISTORICAL = ROOT / "docs/evidence/production-art/source/c03-authoring/C03-front-joint-plan-DRAFT.json"
spec = importlib.util.spec_from_file_location("c03_native_unity_stage", SOURCE)
stage = importlib.util.module_from_spec(spec)
spec.loader.exec_module(stage)


def fixture():
    contract = json.loads(CONTRACT.read_text(encoding="utf-8"))
    historical = json.loads(HISTORICAL.read_text(encoding="utf-8"))
    source_hash = "ee" * 32  # synthetic new packet, never shipped
    joints = [
        {"id": x["id"], "parent": x["parent"],
         "pivot_px": list(x["pivot"]), "parts": list(x["parts"])}
        for x in historical["joints"]
    ]
    # Simulates artist-repositioned joint on the new concept, not accepted art.
    joints[3]["pivot_px"][1] += 1
    plan = {
        "schema": stage.INPUT_SCHEMA,
        "status": "ARTIST_AUTHORED_PENDING_UNITY_REVIEW",
        "character_id": "C03",
        "view": "front",
        "selected_reference_sha256": contract["selected_concept"]["image_sha256"],
        "source_packet_sha256": source_hash,
        "native_grid": contract["canvas_px"],
        "pixels_per_unit": contract["pixel_per_unit"],
        "joints": joints,
        "motions": [{
            "id": "idle", "duration_seconds": 1.2,
            "tracks": [
                {"joint_id": "neck", "keys": [[0.0, 0.0], [0.3, 1.5], [1.2, 0.0]]},
                {"joint_id": "elbow_l", "keys": [[0.0, 0.0], [0.5, -2.0], [1.2, 0.0]]}
            ]
        }]
    }
    return plan, contract, historical, source_hash


class C03SelectedNativeStageTests(unittest.TestCase):
    def test_new_art_motion_plan_exports_exact_skeleton_without_root_sliding(self):
        plan, contract, historical, packet = fixture()
        bones, assigned, motions = stage.validate_authored_plan(
            plan, contract, historical, packet)
        self.assertEqual(len(bones), 16)
        self.assertEqual(len(assigned), 21)
        self.assertEqual(assigned["hand_r"], "wrist_r")
        self.assertEqual(assigned["accessory_front"], "spine")
        self.assertEqual(len(motions), 1)
        self.assertEqual({t["joint_id"] for t in motions[0]["tracks"]},
                         {"neck", "elbow_l"})
        self.assertEqual(motions[0]["tracks"][0]["keys"][1]["angle_degrees"], 1.5)

    def test_rejects_historical_pivots_pretending_to_be_new_reference(self):
        plan, contract, historical, packet = fixture()
        plan["joints"][3]["pivot_px"][1] -= 1
        with self.assertRaisesRegex(ValueError, "historical"):
            stage.validate_authored_plan(plan, contract, historical, packet)

    def test_rejects_stale_owner_image_and_native_packet(self):
        plan, contract, historical, packet = fixture()
        plan["selected_reference_sha256"] = "d2e7e9a79672e23408c0a48626ef3968af84f5564f44e14eac41e4add38cb25e"
        with self.assertRaisesRegex(ValueError, "identity/grid/packet"):
            stage.validate_authored_plan(plan, contract, historical, packet)
        plan, contract, historical, packet = fixture()
        plan["source_packet_sha256"] = "11" * 32
        with self.assertRaisesRegex(ValueError, "identity/grid/packet"):
            stage.validate_authored_plan(plan, contract, historical, packet)

    def test_rejects_invalid_joint_parent_or_out_of_canvas_pivot(self):
        plan, contract, historical, packet = fixture()
        plan["joints"][2]["parent"] = "neck"
        with self.assertRaisesRegex(ValueError, "parent/pivot"):
            stage.validate_authored_plan(plan, contract, historical, packet)
        plan, contract, historical, packet = fixture()
        plan["joints"][2]["pivot_px"] = [128, 99]
        with self.assertRaisesRegex(ValueError, "parent/pivot"):
            stage.validate_authored_plan(plan, contract, historical, packet)

    def test_rejects_duplicate_or_unassigned_semantic_parts(self):
        plan, contract, historical, packet = fixture()
        plan["joints"][3]["parts"].append("hand_r")
        with self.assertRaisesRegex(ValueError, "assigned twice"):
            stage.validate_authored_plan(plan, contract, historical, packet)
        plan, contract, historical, packet = fixture()
        plan["joints"][3]["parts"].remove("face")
        with self.assertRaisesRegex(ValueError, "all 21 semantic"):
            stage.validate_authored_plan(plan, contract, historical, packet)

    def test_rejects_root_translate_or_one_jiggling_joint(self):
        plan, contract, historical, packet = fixture()
        plan["motions"][0]["tracks"][0]["joint_id"] = "root"
        with self.assertRaisesRegex(ValueError, "root"):
            stage.validate_authored_plan(plan, contract, historical, packet)
        plan, contract, historical, packet = fixture()
        plan["motions"][0]["tracks"].pop()
        with self.assertRaisesRegex(ValueError, "multiple genuinely moving"):
            stage.validate_authored_plan(plan, contract, historical, packet)

    def test_rejects_loop_seam_and_nonmonotonic_authored_timing(self):
        plan, contract, historical, packet = fixture()
        plan["motions"][0]["tracks"][0]["keys"][-1][1] = 2
        with self.assertRaisesRegex(ValueError, "bind-pose"):
            stage.validate_authored_plan(plan, contract, historical, packet)
        plan, contract, historical, packet = fixture()
        plan["motions"][0]["tracks"][0]["keys"][1][0] = 0.0
        with self.assertRaisesRegex(ValueError, "joint keyframe"):
            stage.validate_authored_plan(plan, contract, historical, packet)

    def test_walk_requires_articulated_lower_body(self):
        plan, contract, historical, packet = fixture()
        clip = copy.deepcopy(plan["motions"][0])
        clip["id"] = "walk"
        plan["motions"].append(clip)
        with self.assertRaisesRegex(ValueError, "articulated legs"):
            stage.validate_authored_plan(plan, contract, historical, packet)
        clip["tracks"][0]["joint_id"] = "hip_l"
        clip["tracks"][1]["joint_id"] = "hip_r"
        bones, assigned, motions = stage.validate_authored_plan(
            plan, contract, historical, packet)
        self.assertEqual([m["id"] for m in motions], ["idle", "walk"])


if __name__ == "__main__":
    unittest.main()
