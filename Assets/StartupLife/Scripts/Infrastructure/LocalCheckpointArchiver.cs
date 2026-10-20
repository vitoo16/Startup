#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace StartupLife.Infrastructure
{
    /// <summary>
    /// Archives the paired primary/backup checkpoints under the same volume.
    /// Does not deserialize, reset, replace or write any gameplay state.
    /// Call only after the active session has suspended ongoing operations.
    /// </summary>
    public static class LocalCheckpointArchiver
    {
        private const string ArchiveFolder = "RunArchives";

        public static string Archive(string saveFilePath)
        {
            if (string.IsNullOrWhiteSpace(saveFilePath) ||
                !Path.IsPathRooted(saveFilePath))
                throw new ArgumentException("A rooted checkpoint path is required.", nameof(saveFilePath));

            var primary = Path.GetFullPath(saveFilePath);
            var parent = Path.GetDirectoryName(primary)
                ?? throw new ArgumentException("Checkpoint has no directory.", nameof(saveFilePath));
            Directory.CreateDirectory(parent);

            // Use the exact same cross-process lock as AtomicFileSaveStore.
            // Do not archive .lock itself; other writers must keep honoring it.
            using (var gate = new FileStream(primary + ".lock",
                FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
            {
                var originals = new[] { primary, primary + ".backup" };
                var existing = new List<string>(2);
                foreach (var source in originals)
                    if (File.Exists(source)) existing.Add(source);

                if (existing.Count == 0) return string.Empty;

                var archive = Path.Combine(parent, ArchiveFolder,
                    DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff", CultureInfo.InvariantCulture) +
                    "-" + Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(archive);
                var moved = new List<string>(existing.Count);
                try
                {
                    foreach (var source in existing)
                    {
                        File.Move(source, Path.Combine(archive, Path.GetFileName(source)));
                        moved.Add(source);
                    }
                }
                catch (Exception error)
                {
                    // Reverse in a predictable order. If rollback fails, report
                    // exact paths instead of falsely claiming the save is safe.
                    var restorationErrors = new List<string>();
                    for (var i = moved.Count - 1; i >= 0; i--)
                    {
                        var original = moved[i];
                        try
                        {
                            File.Move(Path.Combine(archive, Path.GetFileName(original)), original);
                        }
                        catch (Exception restoreError)
                        {
                            restorationErrors.Add(original + ": " + restoreError.Message);
                        }
                    }
                    throw new IOException("Checkpoint archive failed. Source: " + parent +
                        ". Archive: " + archive + ". Restore errors: " +
                        string.Join("; ", restorationErrors), error);
                }
                return archive;
            }
        }
    }
}
