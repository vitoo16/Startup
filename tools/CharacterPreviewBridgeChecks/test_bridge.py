import importlib.util,shutil,tempfile,unittest
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
spec=importlib.util.spec_from_file_location("bridge",ROOT/"scripts/Validate-CharacterCreatorPreviewBridge.py")
gate=importlib.util.module_from_spec(spec)
spec.loader.exec_module(gate)
class BridgeQuarantineChecks(unittest.TestCase):
    def setUp(self):
        self.temp=tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.root=Path(self.temp.name)
        for p in (gate.BRIDGE,gate.VIEW,gate.REGISTRY):
            dest=self.root/p
            dest.parent.mkdir(parents=True,exist_ok=True)
            shutil.copyfile(ROOT/p,dest)
    def test_approved_only_bridge_contract(self): self.assertEqual(gate.verify(self.root),13)
    def test_asset_loading_in_bridge_rejected(self):
        p=self.root/gate.BRIDGE
        p.write_text(p.read_text()+"\nResources.Load(\"unapproved\");",encoding="utf8")
        with self.assertRaises(AssertionError):gate.verify(self.root)
    def test_stale_color_preview_cannot_pass(self):
        p=self.root/gate.BRIDGE
        p.write_text(p.read_text().replace("bakedHairColorId","bakedWrongColor"),
            encoding="utf8")
        with self.assertRaises(AssertionError):gate.verify(self.root)
    def test_unapproved_registry_mutation_rejected(self):
        p=self.root/gate.REGISTRY
        p.write_text(p.read_text().replace('"approved_entries": []',
            '"approved_entries": [{"id":"unreviewed"}]'),encoding="utf8")
        with self.assertRaises(AssertionError):gate.verify(self.root)
if __name__=="__main__":unittest.main()
