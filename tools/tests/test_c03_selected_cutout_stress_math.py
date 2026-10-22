"""Deterministic C03 16-joint pose stress tester checks (Pillow workflow only)."""
import importlib.util
import math
from pathlib import Path
import unittest

from PIL import Image

ROOT=Path(__file__).resolve().parents[2]
FILE=ROOT/"scripts/Preview-C03OwnerCutoutArticulation.py"
spec=importlib.util.spec_from_file_location("c03_pose_stress",FILE)
mod=importlib.util.module_from_spec(spec)
spec.loader.exec_module(mod)


def transform(m,xy):
    x,y=xy
    return (m[0]*x+m[1]*y+m[2], m[3]*x+m[4]*y+m[5])


class SelectedC03StressMathTests(unittest.TestCase):
    def test_identity_and_inverse_are_exact_to_pixel(self):
        self.assertEqual(mod.combine(mod.IDENTITY,mod.IDENTITY),mod.IDENTITY)
        for ang in (-13,0,19):
            m=mod.about((17.0,39.0),ang)
            inv=mod.inverse(m)
            for pt in ((17,39),(0,0),(84,33)):
                new=transform(inv,transform(m,pt))
                self.assertAlmostEqual(new[0],pt[0],places=8)
                self.assertAlmostEqual(new[1],pt[1],places=8)

    def test_rotation_preserves_joint_center_without_sliding(self):
        m=mod.about((41.,115.),19)
        self.assertAlmostEqual(transform(m,(41.,115.))[0],41.)
        self.assertAlmostEqual(transform(m,(41.,115.))[1],115.)
        self.assertNotEqual(transform(m,(49.,115.)),(49.,115.))

    def test_parent_rotation_propagates_to_child_bone(self):
        joints={
            "root":{"id":"root","parent":None,"pivot_native_estimate_xy":[64,80]},
            "spine":{"id":"spine","parent":"root","pivot_native_estimate_xy":[64,80]},
            "elbow_l":{"id":"elbow_l","parent":"spine","pivot_native_estimate_xy":[30,60]},
        }
        m=mod.joint_matrices(joints,{"spine":8,"elbow_l":-12})
        self.assertEqual(m["root"],mod.IDENTITY)
        self.assertNotEqual(m["spine"],mod.IDENTITY)
        self.assertNotEqual(m["elbow_l"],mod.IDENTITY)
        self.assertNotEqual(m["spine"],m["elbow_l"])

    def test_rejects_invalid_ancestor_and_root_translation(self):
        graph={"root":{"parent":None,"pivot_native_estimate_xy":[64,80]},
               "elbow_l":{"parent":"missing","pivot_native_estimate_xy":[25,55]}}
        with self.assertRaisesRegex(ValueError,"Cycle or missing parent"):
            mod.joint_matrices(graph,{"elbow_l":7})
        graph.pop("elbow_l")
        with self.assertRaisesRegex(ValueError,"root"):
            mod.joint_matrices(graph,{"root":1})

    def test_bone_pose_rotates_png_not_whole_body_root(self):
        pixels=Image.new("RGBA",(mod.W,mod.H))
        for x in range(300,321):
            pixels.putpixel((x,400),(120,50,80,255))
        joints={
            "root":{"parent":None,"pivot_native_estimate_xy":[64,80]},
            "wrist_l":{"parent":"root","pivot_native_estimate_xy":[35,45]},
        }
        before=mod.render(["hand_l"],{"hand_l":pixels},joints,
                          {"hand_l":"wrist_l"}, {})
        after=mod.render(["hand_l"],{"hand_l":pixels},joints,
                         {"hand_l":"wrist_l"}, {"wrist_l":10})
        self.assertNotEqual(before.tobytes(),after.tobytes())
        self.assertEqual(before.getbbox(),(300,400,321,401))
        self.assertIsNotNone(after.getbbox())


if __name__=="__main__":
    unittest.main()
