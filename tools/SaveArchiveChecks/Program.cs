using System;
using System.IO;
using StartupLife.Infrastructure;

internal static class Program
{
    private static int Main()
    {
        var count = 0;
        Check("archive preserves primary, backup and unrelated files", PrimaryAndBackup); count++;
        Check("backup-only recovery checkpoint remains recoverable", BackupOnly); count++;
        Check("missing checkpoint is not synthesized", Missing); count++;
        Check("concurrent writer lock refuses archive without data loss", Locked); count++;
        Check("archive IDs do not collide and old run is recoverable", Sequential); count++;
        Console.WriteLine(count + "/" + count + " save archive checks passed.");
        return 0;
    }

    private static void Check(string name, Action test)
    {
        var root = Path.Combine(Path.GetTempPath(), "startup-run-archive-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            // The harness itself controls all input files under its temp root.
            rootPath = root;
            test();
            Console.WriteLine("PASS " + name);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
            rootPath = string.Empty;
        }
    }

    private static string rootPath = string.Empty;
    private static string Save => Path.Combine(rootPath, "startup-life.json");

    private static void PrimaryAndBackup()
    {
        File.WriteAllText(Save, "primary\u0000contents");
        File.WriteAllText(Save + ".backup", "backup\u0000contents");
        File.WriteAllText(Path.Combine(rootPath, "unrelated.txt"), "leave alone");
        var archive = LocalCheckpointArchiver.Archive(Save);
        Require(!File.Exists(Save) && !File.Exists(Save + ".backup"), "old checkpoints still active");
        Require(File.ReadAllText(Path.Combine(archive, "startup-life.json")) == "primary\u0000contents",
            "primary byte content modified");
        Require(File.ReadAllText(Path.Combine(archive, "startup-life.json.backup")) == "backup\u0000contents",
            "backup byte content modified");
        Require(File.ReadAllText(Path.Combine(rootPath, "unrelated.txt")) == "leave alone", "unrelated file changed");
    }

    private static void BackupOnly()
    {
        File.WriteAllText(Save + ".backup", "backup-valid");
        var archived = LocalCheckpointArchiver.Archive(Save);
        Require(File.ReadAllText(Path.Combine(archived, "startup-life.json.backup")) == "backup-valid",
            "backup is lost");
        Require(!File.Exists(Save + ".backup"), "backup was left in active save location");
    }

    private static void Missing()
    {
        var result = LocalCheckpointArchiver.Archive(Save);
        Require(result.Length == 0, "missing save should not yield an archive");
        Require(!File.Exists(Save), "missing save became a new game checkpoint");
    }

    private static void Locked()
    {
        File.WriteAllText(Save, "keep-data");
        using (var gate = new FileStream(Save + ".lock", FileMode.OpenOrCreate,
            FileAccess.ReadWrite, FileShare.None))
        {
            var threw = false;
            try { LocalCheckpointArchiver.Archive(Save); }
            catch (IOException) { threw = true; }
            Require(threw, "an active writer lock must fail closed");
            Require(File.ReadAllText(Save) == "keep-data", "locked save was touched");
        }
    }

    private static void Sequential()
    {
        File.WriteAllText(Save, "run-one");
        var first = LocalCheckpointArchiver.Archive(Save);
        File.WriteAllText(Save, "run-two");
        var second = LocalCheckpointArchiver.Archive(Save);
        Require(first != second, "archive identity collided");
        Require(File.ReadAllText(Path.Combine(first, "startup-life.json")) == "run-one", "first run was replaced");
        Require(File.ReadAllText(Path.Combine(second, "startup-life.json")) == "run-two", "second run was replaced");
        File.Copy(Path.Combine(first, "startup-life.json"), Save);
        Require(File.ReadAllText(Save) == "run-one", "old run could not be restored manually");
    }

    private static void Require(bool condition, string failure)
    {
        if (!condition) throw new InvalidOperationException(failure);
    }
}
