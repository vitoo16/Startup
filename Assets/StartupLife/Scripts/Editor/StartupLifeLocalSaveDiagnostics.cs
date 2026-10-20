using System;
using System.IO;
using StartupLife.Infrastructure;
using StartupLife.Presentation;
using UnityEditor;
using UnityEngine;

namespace StartupLife.Editor
{
    /// <summary>
    /// Editor-only diagnostics for the actual local save used by Play Mode.
    /// Keep player save management separate from gameplay and save serialization.
    /// </summary>
    public static class StartupLifeLocalSaveDiagnostics
    {
        private const string SaveFileName = "startup-life.json";

        [MenuItem("Startup Life/Debug/Show actual save location")]
        public static void ShowActualSaveLocation()
        {
            var directory = Application.persistentDataPath;
            var savePath = Path.Combine(directory, SaveFileName);
            var count = (File.Exists(savePath) ? 1 : 0) +
                (File.Exists(savePath + ".backup") ? 1 : 0);
            var bootstrap = UnityEngine.Object.FindAnyObjectByType<StartupLifeBootstrapper>();
            var snapshot = bootstrap ? bootstrap.Snapshot : null;
            var runtimeState = !EditorApplication.isPlaying
                ? "Editor is not in Play Mode."
                : snapshot == null
                    ? "Play Mode has no ready game snapshot."
                    : string.IsNullOrEmpty(snapshot.Name)
                        ? "Play Mode character name is empty."
                        : "Play Mode already has a character (revision " + snapshot.Revision + ").";

            Debug.Log("[StartupLifeSave] Application.persistentDataPath: " + directory +
                "\n[StartupLifeSave] Expected save: " + savePath +
                "\n[StartupLifeSave] Matching local save files: " + count +
                "\n[StartupLifeSave] " + runtimeState);
            EditorUtility.DisplayDialog("Startup Life - actual save location",
                "Save path:\n" + savePath + "\n\nFiles found: " + count +
                "\n\n" + runtimeState + "\n\nThe full path is also in the Console.", "OK");
            if (Directory.Exists(directory)) EditorUtility.RevealInFinder(directory);
        }

        [MenuItem("Startup Life/Debug/Archive test save for new character")]
        public static void ArchiveTestSaveForNewCharacter()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorUtility.DisplayDialog("Stop Play Mode first",
                    "The current character is still in memory while Play Mode runs. " +
                    "Stop Play Mode before archiving the save, then press Play again.", "OK");
                return;
            }

            var directory = Application.persistentDataPath;
            var savePath = Path.Combine(directory, SaveFileName);
            var fileCount = (File.Exists(savePath) ? 1 : 0) +
                (File.Exists(savePath + ".backup") ? 1 : 0);
            if (fileCount == 0)
            {
                Debug.Log("[StartupLifeSave] No matching save files in " + directory +
                    ". If Play Mode still shows an existing character, check another player instance or stale Editor state.");
                EditorUtility.DisplayDialog("No test save found",
                    "No startup-life.json or its .backup file was found at:\n" + directory +
                    "\n\nNothing has been changed. Start Play Mode again and inspect the Console if the character still exists.", "OK");
                return;
            }

            if (!EditorUtility.DisplayDialog("Archive local Startup Life test save?",
                "Move " + fileCount + " checkpoint file(s) into a timestamped " +
                "RunArchives folder in the SAME local save directory?\n\n" + directory +
                "\n\nNo file is deleted. This affects Editor-local test progress, not the repo.",
                "Archive and test new character", "Cancel")) return;

            string archive;
            try
            {
                archive = LocalCheckpointArchiver.Archive(savePath);
                if (archive.Length == 0)
                {
                    EditorUtility.DisplayDialog("No checkpoint found",
                        "The checkpoint changed after confirmation. No new character was started.", "OK");
                    return;
                }
            }
            catch (Exception error)
            {
                Debug.LogError("[StartupLifeSave] Archive refused; original checkpoint should remain intact. " +
                    "Check source and RunArchives before retrying: " + error);
                EditorUtility.DisplayDialog("Archive failed",
                    "The checkpoint was not archived. Review the Console and backup locations before retrying.", "OK");
                return;
            }

            Debug.Log("[StartupLifeSave] Archived " + fileCount + " checkpoint file(s) at " + archive +
                ". Stop/Start Play Mode to see the new Character Creator.");
            EditorUtility.DisplayDialog("Test save archived",
                "Backup saved at:\n" + archive +
                "\n\nNow open FirstPlayable.unity and press Play. " +
                "Restore files from this folder later if you want the original character back.", "OK");
        }
    }
}
