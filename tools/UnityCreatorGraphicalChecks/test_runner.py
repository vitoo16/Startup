import importlib.util, shutil, tempfile, unittest
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
spec=importlib.util.spec_from_file_location("visual_runner",ROOT/"scripts/Validate-UnityCreatorGraphicalRunner.py")
gate=importlib.util.module_from_spec(spec)
spec.loader.exec_module(gate)
class NativeMatrixContractTests(unittest.TestCase):
    def setUp(self):
        self.tmp=tempfile.TemporaryDirectory()
        self.addCleanup(self.tmp.cleanup)
        self.root=Path(self.tmp.name)
        for source in (gate.CLI,gate.WORKFLOW,gate.VERIFY):
            dest=self.root/source
            dest.parent.mkdir(parents=True,exist_ok=True)
            shutil.copyfile(ROOT/source,dest)
    def test_valid_graphics_pipeline(self): self.assertEqual(gate.check(self.root),36)
    def test_headless_playmode_not_allowed(self):
        p=self.root/gate.CLI
        p.write_text(p.read_text().replace(
            "$testMode -eq 'PlayMode' -and $EnableGraphics.IsPresent",
            "$testMode -eq 'EditMode' -and $EnableGraphics.IsPresent"),encoding="utf8")
        with self.assertRaises(AssertionError):gate.check(self.root)
    def test_image_check_cannot_be_disabled(self):
        p=self.root/gate.WORKFLOW
        p.write_text(p.read_text().replace("Verify-CreatorResponsiveEvidence.ps1",
            "Write-Output 'skipped'"),encoding="utf8")
        with self.assertRaises(AssertionError):gate.check(self.root)
    def test_expected_png_count_cannot_be_reduced(self):
        p=self.root/gate.VERIFY
        p.write_text(p.read_text().replace("if ($inspected.Count -ne 36)",
             "if ($inspected.Count -ne 12)"),encoding="utf8")
        with self.assertRaises(AssertionError):gate.check(self.root)
if __name__=="__main__": unittest.main()
