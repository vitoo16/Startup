"""Engine-free guard for Unity 6.3 Editor API and Creator PlayMode compatibility.

This is a source contract check, NOT Unity C# compilation. It catches the
exact two API regressions that blocked a real Unity 6000.3.25f1 checkout.
"""
from pathlib import Path
import re
import unittest

ROOT = Path(__file__).resolve().parents[2]
SCALER = ROOT / "Assets/StartupLife/Scripts/Presentation/CharacterCreatorTextScaler.cs"
POLICY = ROOT / "Assets/StartupLife/Scripts/Presentation/CharacterCreatorTextScalePolicy.cs"
PLAYMODE = ROOT / "Assets/StartupLife/Tests/PlayMode/CharacterCreatorResponsiveMatrixTests.cs"
EDITOR = ROOT / "Assets/StartupLife/Scripts/Editor/GoldenCharacterDraftBuilder.cs"


class Unity63LocalCompileContractTests(unittest.TestCase):
    def test_responsive_playmode_expects_public_scale_percent(self):
        scaler = SCALER.read_text(encoding="utf-8")
        policy = POLICY.read_text(encoding="utf-8")
        tests = PLAYMODE.read_text(encoding="utf-8")
        self.assertIn("textScaler.ScalePercent", tests)
        self.assertRegex(scaler, r"public\s+int\s+ScalePercent\s*=>\s*\(int\)\s*\(CharacterCreatorTextScalePolicy\.Multiplier\(step\)\s*\*\s*100f\)")
        self.assertIn("ScalePercent.ToString(", scaler)
        self.assertIn("ClampStep(requested)", scaler)
        for token in ("case 0: return 1f;", "case 1: return 1.5f;", "case 2: return 2f;"):
            self.assertIn(token, policy)

    def test_unity6_sprite_alignment_uses_texture_importer_settings(self):
        editor = EDITOR.read_text(encoding="utf-8")
        self.assertNotRegex(editor, r"\b(?:tex|importer)\.spriteAlignment\b")
        self.assertIn("class NativeSpriteImportSettings", editor)
        self.assertIn("new TextureImporterSettings()", editor)
        self.assertIn("importer.ReadTextureSettings(settings)", editor)
        self.assertIn("settings.spriteAlignment = (int)alignment;", editor)
        self.assertIn("settings.spritePivot = pivot;", editor)
        self.assertIn("importer.SetTextureSettings(settings)", editor)
        self.assertIn("NativeSpriteImportSettings.ConfigurePivot(importer, SpriteAlignment.Custom,", editor)
        self.assertIn("NativeSpriteImportSettings.ConfigurePivot(importer, SpriteAlignment.Center,", editor)
        self.assertIn("NativeSpriteImportSettings.HasAlignment(tex, SpriteAlignment.Center)", editor)

    def test_editor_importers_do_not_access_removed_texture_importer_alignment_api(self):
        # Check all Editor source, not only the original C01/C03 builder.
        # The valid Unity 6.3 API is TextureImporterSettings.spriteAlignment.
        editor_root = ROOT / "Assets/StartupLife/Scripts/Editor"
        offenders = []
        for source in sorted(editor_root.rglob("*.cs")):
            code = source.read_text(encoding="utf-8")
            # Identify TextureImporter-typed locals/parameters before checking their
            # member accesses. Avoid flagging TextureImporterSettings variables.
            importer_names = set(re.findall(
                r"\\bTextureImporter\\s+([A-Za-z_][A-Za-z_0-9]*)\\b", code
            ))
            for name in importer_names:
                if re.search(r"\\b" + re.escape(name) + r"\\s*\\.\\s*spriteAlignment\\b", code):
                    offenders.append(str(source.relative_to(ROOT)) + ": " + name)
        self.assertEqual([], offenders,
                         "Unity 6.3 requires TextureImporterSettings.spriteAlignment")

    def test_approved_asset_gate_unchanged_and_unity_proof_quarantined(self):
        editor = EDITOR.read_text(encoding="utf-8")
        self.assertIn("C03_Idle_V3_QUARANTINED_NOT_FOR_GAMEPLAY", editor)
        self.assertIn("Generated-V3-Quarantine", editor)
        self.assertIn("VerifySerializedProof(rig)", editor)
        self.assertNotIn("Assets/StartupLife/Art/Approved/registry.json", editor)


if __name__ == "__main__":
    unittest.main()
