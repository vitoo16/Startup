"""Mutation checks: no silent disappearance of creator pages/bindings."""
import importlib.util
import shutil
import tempfile
import unittest
from pathlib import Path

ROOT=Path(__file__).resolve().parents[2]
SPEC=importlib.util.spec_from_file_location("creator_ui",
    ROOT/"scripts/Validate-CharacterCreatorUi.py")
gate=importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(gate)

class CreatorUiCases(unittest.TestCase):
    def setUp(self):
        self.temp=tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.root=Path(self.temp.name)
        for name in (gate.VIEW,gate.BUILDER,gate.DATA):
            dest=self.root/name;dest.parent.mkdir(parents=True,exist_ok=True)
            shutil.copyfile(ROOT/name,dest)
    def test_valid_wiring(self):
        self.assertEqual(gate.verify(self.root),19)
    def test_missing_repair_wiring_rejected(self):
        p=self.root/gate.BUILDER
        p.write_text(p.read_text().replace(
            "UpgradeCharacterCreator(character.gameObject, character, font, theme);",
            "// removed"),encoding="utf-8")
        with self.assertRaises(AssertionError):gate.verify(self.root)
    def test_missing_control_rejected(self):
        p=self.root/gate.VIEW
        p.write_text(p.read_text().replace(
            "public void NextHairColor()", "public void RenamedHairColor()"),
            encoding="utf-8")
        with self.assertRaises(AssertionError):gate.verify(self.root)

if __name__=="__main__":unittest.main()
