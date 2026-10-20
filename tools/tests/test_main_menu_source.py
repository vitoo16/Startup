"""Source invariants for player home menu; not a Unity runtime/visual PASS."""
from pathlib import Path
import unittest

ROOT = Path(__file__).resolve().parents[2]
SCRIPTS = ROOT / "Assets/StartupLife/Scripts"
MENU = SCRIPTS / "Presentation/FirstPlayableMainMenu.cs"
BOOT = SCRIPTS / "Presentation/StartupLifeBootstrapper.cs"
ARCHIVE = SCRIPTS / "Infrastructure/LocalCheckpointArchiver.cs"
CATALOG = SCRIPTS / "Presentation/DashboardLocalizationCatalog.cs"


class HomeMenuContracts(unittest.TestCase):
    def test_menu_has_explicit_new_run_confirmation_and_continue(self):
        menu = MENU.read_text(encoding="utf-8")
        for label in ("StartupMainMenuOpenButton", "StartupMainMenuContinueButton",
                      "StartupMainMenuNewGameButton", "StartupMainMenuSettingsButton",
                      "StartupMainMenuConfirmButton", "StartupMainMenuCancelButton"):
            self.assertIn(label, menu)
        self.assertIn('confirmationPage.SetActive(true)', menu)
        self.assertIn('bootstrap.TryArchiveAndStartNewGame(out var reason)', menu)
        self.assertIn('continueButton.interactable', menu)
        self.assertNotIn("File.Delete(", menu)
        self.assertNotIn("PlayerPrefs.DeleteAll(", menu)
        self.assertNotIn("GameSession.NewState(", menu)

    def test_runtime_and_editor_use_one_locked_archiver(self):
        bootstrap = BOOT.read_text(encoding="utf-8")
        archiver = ARCHIVE.read_text(encoding="utf-8")
        editor = (SCRIPTS / "Editor/StartupLifeLocalSaveDiagnostics.cs").read_text(encoding="utf-8")
        self.assertIn("LocalCheckpointArchiver.Archive(SavePath)", bootstrap)
        self.assertIn("LocalCheckpointArchiver.Archive(savePath)", editor)
        self.assertIn('FileShare.None', archiver)
        self.assertIn('primary + ".lock"', archiver)
        self.assertIn('primary + ".backup"', archiver)
        self.assertIn("for (var i = moved.Count - 1; i >= 0; i--)", archiver)
        self.assertNotIn("File.Delete(", archiver)

    def test_all_player_menu_text_is_keyed_to_dashboard_namespace(self):
        source = MENU.read_text(encoding="utf-8")
        catalog = CATALOG.read_text(encoding="utf-8")
        keys = [
            "open", "title", "continue", "new", "settings", "back", "confirm",
            "archive_new", "cancel", "archive_failed", "motion.enabled",
            "motion.disabled", "motion.toggle"
        ]
        for key in keys:
            full = "dashboard.menu." + key
            self.assertIn('"' + full + '"', source)
            self.assertIn('"' + full + '"', catalog)

    def test_no_automatic_destructive_startup(self):
        bootstrap = BOOT.read_text(encoding="utf-8")
        menu = MENU.read_text(encoding="utf-8")
        self.assertIn("FirstPlayableMainMenu.Install(this", bootstrap)
        self.assertIn('screen.SetActive(false)', menu)
        self.assertIn('lifecycle.SetPaused(true)', bootstrap)
        self.assertIn('if (archive.Length == 0)', bootstrap)
        self.assertIn('lifecycle.SetPaused(false)', bootstrap)


if __name__ == "__main__":
    unittest.main()
