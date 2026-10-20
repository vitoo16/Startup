"""Engine-free source contract for opt-in Unity Editor save diagnostics.

Does NOT run Unity, exercise filesystem moves, or validate user data.
"""
from pathlib import Path
import unittest

ROOT = Path(__file__).resolve().parents[2]
EDITOR = ROOT / "Assets/StartupLife/Scripts/Editor/StartupLifeLocalSaveDiagnostics.cs"
BOOTSTRAP = ROOT / "Assets/StartupLife/Scripts/Presentation/StartupLifeBootstrapper.cs"


class EditorSaveDiagnosticsContractTests(unittest.TestCase):
    def test_editor_matches_runtime_save_location_and_name(self):
        editor = EDITOR.read_text(encoding="utf-8")
        bootstrap = BOOTSTRAP.read_text(encoding="utf-8")
        self.assertIn('private const string SaveFileName = "startup-life.json";', editor)
        self.assertIn('saveFileName = "startup-life.json"', bootstrap)
        self.assertIn('Application.persistentDataPath', editor)
        self.assertIn('UnityEngine.Application.persistentDataPath', bootstrap)
        self.assertIn('"Startup Life/Debug/Show actual save location"', editor)
        self.assertIn('"Startup Life/Debug/Archive test save for new character"', editor)

    def test_archiving_is_opt_in_and_fails_closed_in_play_mode(self):
        editor = EDITOR.read_text(encoding="utf-8")
        self.assertIn('EditorApplication.isPlayingOrWillChangePlaymode', editor)
        self.assertIn('if (!EditorUtility.DisplayDialog("Archive local Startup Life test save?"', editor)
        self.assertIn('LocalCheckpointArchiver.Archive(savePath)', editor)
        self.assertIn('RunArchives', editor)
        self.assertNotIn('File.Move(', editor)
        archive_source = (ROOT / "Assets/StartupLife/Scripts/Infrastructure/LocalCheckpointArchiver.cs").read_text(encoding="utf-8")
        self.assertIn('File.Move(source, Path.Combine(archive, Path.GetFileName(source)))', archive_source)
        self.assertIn('File.Move(Path.Combine(archive, Path.GetFileName(original)), original)', archive_source)
        self.assertNotIn('File.Delete(', editor)
        self.assertNotIn('Directory.Delete(', editor)
        self.assertNotIn('PlayerPrefs.DeleteAll(', editor)
        self.assertNotIn('GameSession.NewState(', editor)

    def test_diagnostics_distinguish_runtime_snapshot_from_disk(self):
        editor = EDITOR.read_text(encoding="utf-8")
        self.assertIn('FindAnyObjectByType<StartupLifeBootstrapper>', editor)
        self.assertIn('snapshot.Name', editor)
        self.assertIn('Matching local save files:', editor)
        self.assertIn('No startup-life.json or its .backup file was found', editor)


    def test_bootstrap_logs_actual_load_status_without_exposing_names(self):
        bootstrap = BOOTSTRAP.read_text(encoding="utf-8")
        self.assertIn('private static void LogEditorSaveRead(string path, LoadResult read)', bootstrap)
        self.assertEqual(2, bootstrap.count('LogEditorSaveRead(path, '))
        self.assertIn('status=" + read.Status', bootstrap)
        self.assertIn('backupExists=" + File.Exists(path + ".backup")', bootstrap)
        self.assertIn('characterPresent=" + (read.State != null', bootstrap)
        self.assertIn('#if UNITY_EDITOR', bootstrap)
        self.assertNotIn('Debug.Log(read.State.Name)', bootstrap)


if __name__ == '__main__':
    unittest.main()
