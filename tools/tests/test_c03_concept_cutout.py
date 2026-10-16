"""Draft concept isolation regression; no visual or native-grid approval."""
import importlib.util
from pathlib import Path
import unittest
try:
    from PIL import Image
except ImportError:
    Image = None

ROOT = Path(__file__).resolve().parents[2]
SCRIPT = ROOT / 'scripts/Prepare-C03OwnerConceptCutout.py'

@unittest.skipIf(Image is None,'Pillow optional on art workstations')
class C03ConceptCutoutTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        spec=importlib.util.spec_from_file_location('c03_cutout',SCRIPT)
        cls.module=importlib.util.module_from_spec(spec)
        spec.loader.exec_module(cls.module)

    def test_keep_original_rgb_and_hard_alpha(self):
        original=Image.new('RGB',(12,12),(253,251,244))
        for y in range(1,7):
            for x in range(4,8):original.putpixel((x,y),(33,48,53))
        isolated=self.module.isolate(original,seed=(5,2))
        self.assertEqual(isolated.getpixel((5,2)),(33,48,53,255))
        self.assertEqual(isolated.getpixel((0,0)),(253,251,244,0))
        self.assertEqual(isolated.getchannel('A').getbbox(),(4,1,8,7))
        self.assertEqual(original.mode,'RGB')

    def test_rejects_incorrect_seed_and_mode(self):
        source=Image.new('RGB',(12,12),(253,251,244))
        with self.assertRaisesRegex(ValueError,'hair seed'):
            self.module.isolate(source,seed=(6,2))
        with self.assertRaisesRegex(ValueError,'Expected RGB'):
            self.module.isolate(source.convert('RGBA'))

    def test_disconnected_foreground_noise_excluded(self):
        image=Image.new('RGB',(12,12),(253,251,244))
        for y in range(2,8):
            for x in range(4,9):image.putpixel((x,y),(25,30,35))
        image.putpixel((0,11),(25,30,35))
        isolated=self.module.isolate(image,seed=(5,3))
        self.assertEqual(isolated.getpixel((0,11))[3],0)
        self.assertEqual(isolated.getpixel((6,5))[3],255)

if __name__=='__main__':
    unittest.main()
