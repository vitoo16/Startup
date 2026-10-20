using System;
using System.IO;
using System.Linq;
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
        private const string ArchiveDirectoryName = "EditorTestSaveArchives";

        [MenuItem("Startup Life/Debug/Show actual save location")]
        public static void ShowActualSaveLocation()
        {
            var directory = Application.persistentDataPath;
            var savePath = Path.Combine(directory, SaveFileName);
            var count = Directory.Exists(directory)
                ? Directory.GetFiles(directory, SaveFileName + "*", SearchOption.TopDirectoryOnly).Length
                : 0;
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
            var files = Directory.Exists(directory)
                ? Directory.GetFiles(directory, SaveFileName + "*", SearchOption.TopDirectoryOnly)
                    .OrderBy(path => path, StringComparer.Ordinal).ToArray()
                : Array.Empty<string>();
            if (files.Length == 0)
            {
                Debug.Log("[StartupLifeSave] No matching save files in " + directory +
                    ". If Play Mode still shows an existing character, check another player instance or stale Editor state.");
                EditorUtility.DisplayDialog("No test save found",
                    "No startup-life.json* files were found at:\n" + directory +
                    "\n\nNothing has been changed. Start Play Mode again and inspect the Console if the character still exists.", "OK");
                return;
            }

            if (!EditorUtility.DisplayDialog("Archive local Startup Life test save?",
                "Move " + files.Length + " startup-life.json* file(s) to a timestamped " +
                "subfolder in the SAME local save directory?\n\n" + directory +
                "\n\nNo file is deleted. This affects Editor-local test progress, not the repo.",
                "Archive and test new character", "Cancel")) return;

            var archive = Path.Combine(directory, ArchiveDirectoryName,
                DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff", System.Globalization.CultureInfo.InvariantCulture)
                + "-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(archive);
            var moved = new System.Collections.Generic.List<string>();
            try
            {
                foreach (var source in files)
                {
                    var destination = Path.Combine(archive, Path.GetFileName(source));
                    File.Move(source, destination);
                    moved.Add(source);
                }
            }
            catch (Exception error)
            {
                for (var i = moved.Count - 1; i >= 0; i--)
                {
                    var source = moved[i];
                    var destination = Path.Combine(archive, Path.GetFileName(source));
                    try { File.Move(destination, source); }
                    catch (Exception rollbackError)
                    {
                        Debug.LogError("[StartupLifeSave] Could not restore " + source + ": " + rollbackError.Message);
                    }
                }
                Debug.LogError("[StartupLifeSave] Archive interrupted. Inspect both folders before retrying: " +
                    directory + " and " + archive + "\n" + error);
                EditorUtility.DisplayDialog("Archive failed",
                    "Could not move all files. Check Console for the locations. " +
                    "Do not run Play Mode before verifying your saved files.", "OK");
                return;
            }

            Debug.Log("[StartupLifeSave] Archived " + moved.Count + " file(s) at " + archive +
                ". Stop/Start Play Mode to see the new Character Creator.");
            EditorUtility.DisplayDialog("Test save archived",
                "Backup saved at:\n" + archive +
                "\n\nNow open FirstPlayable.unity and press Play. " +
                "Restore files from this folder later if you want the original character back.", "OK");
        }
    }
}
