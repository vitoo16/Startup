"""Preview output is deterministic and cannot silently become approval evidence."""
import importlib.util
import json
import struct
import tempfile
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
LOC = ROOT / "scripts/Render-GoldenCharacterPreview.py"
SPEC = importlib.util.spec_from_file_location("c01_preview", LOC)
preview = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(preview)


class CharacterPreviewChecks(unittest.TestCase):
    def test_deterministic_valid_png(self):
        one, stats = preview.render(ROOT)
        two, _ = preview.render(ROOT)
        self.assertEqual(one, two)
        self.assertTrue(one.startswith(b"\x89PNG\r\n\x1a\n"))
        width, height = struct.unpack("!II", one[16:24])
        self.assertEqual((width, height), (2 * 128 * 3 + 3 * 24, 160 * 3 + 2 * 24))
        self.assertEqual(stats["bbox"], [35, 5, 96, 156])
        self.assertGreater(stats["opaque"], 3000)

    def test_fails_on_out_of_bounds_art(self):
        data = json.loads((ROOT / preview.SOURCE).read_text(encoding="utf-8"))
        data["parts"][0]["rects"][0]["box"] = [127, 0, 2, 2]
        with self.assertRaisesRegex(ValueError, "Out-of-bounds"):
            preview.painted_pixels(data)

    def test_fails_on_empty_character(self):
        data = json.loads((ROOT / preview.SOURCE).read_text(encoding="utf-8"))
        data["parts"] = []
        with self.assertRaisesRegex(ValueError, "visually empty"):
            preview.coverage(data, preview.painted_pixels(data))

    def test_rejects_bad_palette(self):
        data = json.loads((ROOT / preview.SOURCE).read_text(encoding="utf-8"))
        data["palette"][0]["hex"] = "invalid"
        with self.assertRaisesRegex(ValueError, "Invalid pixel color"):
            preview.painted_pixels(data)


if __name__ == "__main__":
    unittest.main()
