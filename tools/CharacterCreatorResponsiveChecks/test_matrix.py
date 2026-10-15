import importlib.util
import shutil
import tempfile
import unittest
from pathlib import Path

ROOT=Path(__file__).resolve().parents[2]
spec=importlib.util.spec_from_file_location(
    "responsive_matrix", ROOT/"scripts/Validate-CharacterCreatorResponsiveMatrix.py")
gate=importlib.util.module_from_spec(spec)
spec.loader.exec_module(gate)

class ResponsiveMatrixChecks(unittest.TestCase):
    def setUp(self):
        self.tmp=tempfile.TemporaryDirectory()
        self.addCleanup(self.tmp.cleanup)
        self.root=Path(self.tmp.name)
        for p in (gate.MATRIX,gate.GATE):
            dst=self.root/p
            dst.parent.mkdir(parents=True,exist_ok=True)
            shutil.copyfile(ROOT/p,dst)

    def test_authored_matrix(self):
        self.assertGreater(gate.verify(self.root),10)

    def test_dropping_small_viewport_fails(self):
        p=self.root/gate.MATRIX
        p.write_text(p.read_text().replace("new Vector2Int(360, 640)",
            "new Vector2Int(800, 1200)"),encoding="utf-8")
        with self.assertRaises(AssertionError):gate.verify(self.root)

    def test_disabling_scaler_fails(self):
        p=self.root/gate.MATRIX
        p.write_text(p.read_text()+
            "\n// WARNING: scaler.enabled = false",encoding="utf-8")
        with self.assertRaises(AssertionError):gate.verify(self.root)

    def test_skipping_reopen_gate_fails(self):
        p=self.root/gate.GATE
        p.write_text(p.read_text().replace(
            "FirstPlayableShellBuilder.RepairGeneratedAssetReferences();\n            VerifyScene();",
            "FirstPlayableShellBuilder.RepairGeneratedAssetReferences();"),
            encoding="utf-8")
        with self.assertRaises(AssertionError):gate.verify(self.root)

if __name__=="__main__":
    unittest.main()
