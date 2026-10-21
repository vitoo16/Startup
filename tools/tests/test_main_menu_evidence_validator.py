"""Engine-free contract tests of the PowerShell screenshot *file* verifier.

Synthetic PNG fixtures ONLY test the verifier's fail-closed behavior; they do
not constitute Unity render, screenshot provenance, visual or device evidence.
"""

import json
from pathlib import Path
import shutil
import struct
import subprocess
import tempfile
import unittest
import zlib


ROOT = Path(__file__).resolve().parents[2]
VERIFIER = ROOT / "scripts/Verify-MainMenuResponsiveEvidence.ps1"
ORIGINAL = ROOT / "scripts/Verify-CreatorResponsiveEvidence.ps1"
SIZES = ((360, 640), (390, 844), (430, 932))
PAGES = ("StartupMainMenuHome", "StartupMainMenuSettings",
         "StartupMainMenuNewGameConfirmation")


def png(width, height):
    def chunk(label, data):
        return (struct.pack(">I", len(data)) + label + data +
                struct.pack(">I", zlib.crc32(label + data) & 0xFFFFFFFF))
    scanline = b"\x00" + b"\x00\x00\x00\xff" * width
    raw = scanline * height
    return (b"\x89PNG\r\n\x1a\n" +
            chunk(b"IHDR", struct.pack(">IIBBBBB", width, height, 8, 6, 0, 0, 0)) +
            chunk(b"IDAT", zlib.compress(raw)) +
            chunk(b"IEND", b""))


class MainMenuEvidenceVerifierTests(unittest.TestCase):
    def test_script_reuses_creator_validator_and_mode(self):
        shared = ORIGINAL.read_text(encoding="utf-8")
        wrapper = VERIFIER.read_text(encoding="utf-8")
        self.assertIn("-Mode MainMenu", wrapper)
        self.assertIn("Verify-CreatorResponsiveEvidence.ps1", wrapper)
        self.assertIn("ValidateSet('Creator', 'MainMenu')", shared)
        self.assertIn("Get-FileHash", shared)
        self.assertIn("$expectedCount", shared)
        self.assertIn("Unexpected count of responsive captures", shared)

    @unittest.skipUnless(shutil.which("pwsh"), "PowerShell 7 is not available")
    def test_nine_expected_files_pass_and_modified_inputs_fail(self):
        with tempfile.TemporaryDirectory() as folder:
            directory = Path(folder)
            for width, height in SIZES:
                for page in PAGES:
                    (directory / f"main-menu-{width}x{height}-{page}.png").write_bytes(
                        png(width, height))
            report = directory / "verified.json"

            def check():
                return subprocess.run(
                    ["pwsh", "-NoProfile", "-File", str(VERIFIER),
                     "-Directory", str(directory), "-ReportPath", str(report)],
                    text=True, capture_output=True, check=False)

            passed = check()
            self.assertEqual(0, passed.returncode, passed.stderr)
            content = json.loads(report.read_text(encoding="utf-8-sig"))
            self.assertEqual(9, content["count"])
            self.assertTrue(content["success"])
            self.assertEqual(9, len(set(x["sha256"] + x["file"] for x in content["screenshots"])))
            self.assertIn("human", content["note"].lower())

            key = directory / f"main-menu-360x640-{PAGES[0]}.png"
            original = key.read_bytes()
            key.unlink()
            missing = check()
            self.assertNotEqual(0, missing.returncode, "Missing page must fail verification")
            key.write_bytes(png(320, 640))
            wrong_size = check()
            self.assertNotEqual(0, wrong_size.returncode, "Wrong PNG dimensions must fail")
            key.write_bytes(original)
            unexpected = directory / "main-menu-360x640-UnknownPage.png"
            unexpected.write_bytes(original)
            extra = check()
            self.assertNotEqual(0, extra.returncode, "Extra pages must fail verification")

    @unittest.skipUnless(shutil.which("pwsh"), "PowerShell 7 is not available")
    def test_missing_directory_is_not_success(self):
        with tempfile.TemporaryDirectory() as folder:
            target = Path(folder) / "missing-directory"
            failed = subprocess.run(
                ["pwsh", "-NoProfile", "-File", str(VERIFIER),
                 "-Directory", str(target)],
                text=True, capture_output=True, check=False)
            self.assertNotEqual(0, failed.returncode)


if __name__ == "__main__":
    unittest.main()
