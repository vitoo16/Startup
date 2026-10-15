"""C03 draft anti-slop, native pixels, first-party provenance, and no auto approval."""
import importlib.util,json,shutil,tempfile,unittest
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
spec=importlib.util.spec_from_file_location('c03_validator',ROOT/'scripts/Validate-GoldenCharacterC03.py')
gate=importlib.util.module_from_spec(spec);spec.loader.exec_module(gate)
class C03NativeDraftChecks(unittest.TestCase):
    def setUp(self):
        self.tmp=tempfile.TemporaryDirectory();self.addCleanup(self.tmp.cleanup)
        self.root=Path(self.tmp.name)
        for p in (gate.MANIFEST,gate.REGISTRY,gate.PROVENANCE,gate.RENDER,Path('scripts/PixelPng.py')):
            target=self.root/p;target.parent.mkdir(parents=True,exist_ok=True)
            shutil.copyfile(ROOT/p,target)
    def change(self,path,modify):
        target=self.root/path
        doc=json.loads(target.read_text(encoding='utf8'));modify(doc)
        target.write_text(json.dumps(doc),encoding='utf8')
    def test_three_original_silhouettes(self):self.assertEqual(gate.verify(self.root),12)
    def test_no_fake_art_signoff(self):
        self.change(gate.MANIFEST,lambda d:d['visual_gates'].update(owner_approval=True))
        with self.assertRaises(AssertionError):gate.verify(self.root)
    def test_reject_unregistered_views(self):
        self.change(gate.MANIFEST,lambda d:d['views'].remove('back'))
        with self.assertRaises(AssertionError):gate.verify(self.root)
    def test_provenance_must_remain_draft(self):
        def mut(d):next(x for x in d['assets'] if x['asset_id']=='sl-character-golden-c03').update(status='APPROVED')
        self.change(gate.PROVENANCE,mut)
        with self.assertRaises(AssertionError):gate.verify(self.root)
    def test_legacy_registry_stays_unchanged(self):
        self.change(gate.REGISTRY,lambda d:d['approved_entries'].append({'asset_id':'C03-FAKE'}))
        with self.assertRaises(AssertionError):gate.verify(self.root)
    def test_reject_duplicate_silhouette_definition(self):
        self.change(gate.MANIFEST,lambda d:d['silhouettes'][1].update(id=d['silhouettes'][0]['id']))
        with self.assertRaises(AssertionError):gate.verify(self.root)
if __name__=='__main__':unittest.main()
