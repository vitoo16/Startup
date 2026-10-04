#nullable enable
using System;
using System.IO;
using System.Linq;
using StartupLife.Core;

namespace StartupLife.Infrastructure
{
    public sealed class AtomicFileSaveStore : ISaveCompatibilityStore
    {
        private readonly string primary;
        private readonly ISaveSerializer serializer;
        private readonly Func<string, bool>? fault;
        private readonly object gate = new object();
        public AtomicFileSaveStore(string absolutePath, ISaveSerializer saveSerializer, Func<string, bool>? faultInjection = null)
        {
            if (!Path.IsPathRooted(absolutePath)) throw new ArgumentException("Save path must be injected and absolute.");
            primary = Path.GetFullPath(absolutePath); serializer = saveSerializer; fault = faultInjection;
        }
        public LoadResult Read()
        {
            lock (gate)
            {
                var first = ReadFile(primary);
                if (first.Status == LoadStatus.Valid)
                {
                    var prior = ReadFile(primary + ".backup");
                    // A crash may have promoted ownership in primary before mirroring it in backup.
                    // Only a provably identical generation with historical child IDs qualifies for repair.
                    if (prior.Status == LoadStatus.Valid && prior.SourceSchemaVersion == first.SourceSchemaVersion &&
                        prior.State!.Revision == first.State!.Revision &&
                        !prior.State.Receipts.Select(x => x.CommandId).SequenceEqual(first.State.Receipts.Select(x => x.CommandId)) &&
                        IdentityOnlyChange(prior.State, first.State, first.SourceSchemaVersion))
                        return new LoadResult(LoadStatus.Valid, first.State, BatchReceiptIdentity.BackupRepairReason, first.SourceSchemaVersion);
                    return first;
                }
                if (first.Status == LoadStatus.Unreadable || first.Status == LoadStatus.FutureVersion || first.Status == LoadStatus.UnsupportedContent) return first;
                var backup = ReadFile(primary + ".backup");
                if (backup.Status == LoadStatus.Valid) return new LoadResult(LoadStatus.RecoveredBackup, backup.State, "save.recovered_backup", backup.SourceSchemaVersion);
                if (backup.Status == LoadStatus.Unreadable || backup.Status == LoadStatus.FutureVersion || backup.Status == LoadStatus.UnsupportedContent) return backup;
                return first.Status == LoadStatus.Missing && backup.Status == LoadStatus.Missing ? first : new LoadResult(LoadStatus.Corrupt, reason: "save.unrecoverable");
            }
        }
        private LoadResult ReadFile(string path)
        {
            try { return serializer.DeserializeAndValidate(File.ReadAllBytes(path)); }
            catch (FileNotFoundException) { return new LoadResult(LoadStatus.Missing); }
            catch (DirectoryNotFoundException) { return new LoadResult(LoadStatus.Missing); }
            catch (IOException) { return new LoadResult(LoadStatus.Unreadable, reason: "save.read_failed"); }
            catch (UnauthorizedAccessException) { return new LoadResult(LoadStatus.Unreadable, reason: "save.read_failed"); }
        }
        public WriteStatus Commit(byte[] validatedBytes, long expectedRevision)
            => CommitCore(validatedBytes, expectedRevision, null);
        public WriteStatus CommitReceiptCompatibility(byte[] expectedCheckpoint, byte[] normalizedCheckpoint)
        {
            var expected = serializer.DeserializeAndValidate(expectedCheckpoint);
            var normalized = serializer.DeserializeAndValidate(normalizedCheckpoint);
            if (expected.Status != LoadStatus.Valid || normalized.Status != LoadStatus.Valid ||
                expected.SourceSchemaVersion != normalized.SourceSchemaVersion) return WriteStatus.Failed;
            return CommitCore(normalizedCheckpoint, expected.State!.Revision, expectedCheckpoint);
        }
        private byte[] SerializeForSchema(GameState state, int schemaVersion)
        {
            if (serializer is ISaveCompatibilitySerializer compatibility)
                return compatibility.SerializeForSchema(state, schemaVersion);
            if (schemaVersion != SaveSchema.CurrentVersion) throw new ArgumentException("Serializer cannot preserve historical schema.");
            return serializer.Serialize(state);
        }
        private bool IdentityOnlyChange(GameState expected, GameState candidate, int schemaVersion)
        {
            if (expected.Receipts.Count != candidate.Receipts.Count) return false;
            var ids = candidate.Receipts.Select(x => x.CommandId).ToArray();
            try
            {
                for (var i = 0; i < ids.Length; i++)
                {
                    if (ids[i] != expected.Receipts[i].CommandId && !BatchReceiptIdentity.IsLegacyReplacement(expected.Receipts[i], ids[i])) return false;
                    candidate.Receipts[i].CommandId = expected.Receipts[i].CommandId;
                }
                return SerializeForSchema(expected, schemaVersion).SequenceEqual(SerializeForSchema(candidate, schemaVersion));
            }
            finally { for (var i = 0; i < ids.Length; i++) candidate.Receipts[i].CommandId = ids[i]; }
        }
        private WriteStatus CommitCore(byte[] validatedBytes, long expectedRevision, byte[]? expectedCheckpoint)
        {
            lock (gate)
            {
                var promoted = false;
                // Each writer owns its temp; a loser of the cross-process lock cannot delete another writer's file.
                var temporary = primary + ".tmp-" + Guid.NewGuid().ToString("N");
                try
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(primary)!);
                    using (var fileGate = new FileStream(primary + ".lock", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
                    {
                        var candidate = serializer.DeserializeAndValidate(validatedBytes);
                        if (candidate.Status != LoadStatus.Valid || candidate.State!.Revision !=
                            (expectedCheckpoint == null ? checked(expectedRevision + 1) : expectedRevision)) return WriteStatus.Failed;
                        var previous = Read();
                        if (previous.Status != LoadStatus.Valid && previous.Status != LoadStatus.RecoveredBackup && !(previous.Status == LoadStatus.Missing && expectedRevision == 0)) return WriteStatus.Failed;
                        if (previous.State != null && previous.State.Revision != expectedRevision) return WriteStatus.Failed;
                        if (expectedCheckpoint != null)
                        {
                            var expected = serializer.DeserializeAndValidate(expectedCheckpoint);
                            if (previous.State == null || expected.Status != LoadStatus.Valid ||
                                previous.SourceSchemaVersion != expected.SourceSchemaVersion ||
                                candidate.SourceSchemaVersion != expected.SourceSchemaVersion ||
                                !SerializeForSchema(previous.State, expected.SourceSchemaVersion).SequenceEqual(
                                    SerializeForSchema(expected.State!, expected.SourceSchemaVersion)) ||
                                !IdentityOnlyChange(expected.State!, candidate.State!, expected.SourceSchemaVersion)) return WriteStatus.Failed;
                        }
                        using (var file = new FileStream(temporary, FileMode.Create, FileAccess.Write, FileShare.None))
                        { file.Write(validatedBytes, 0, validatedBytes.Length); file.Flush(true); }
                        if (ReadFile(temporary).Status != LoadStatus.Valid) return WriteStatus.Failed;
                        if (fault?.Invoke("before-replace") == true) throw new IOException("Injected before replacement.");
                        if (File.Exists(primary))
                        {
                            if (ReadFile(primary).Status == LoadStatus.Valid) File.Replace(temporary, primary, primary + ".backup");
                            else
                            {
                                // Retain corrupted bytes without replacing the last validated backup with them.
                                File.Move(primary, primary + ".corrupt-" + Guid.NewGuid().ToString("N"));
                                File.Move(temporary, primary);
                            }
                        }
                        else File.Move(temporary, primary);
                        promoted = true;
                        if (fault?.Invoke("after-replace") == true) throw new IOException("Injected after replacement.");
                        if (expectedCheckpoint != null)
                        {
                            // The prior primary is the same logical generation. Keep its recovered ownership in backup too.
                            using (var file = new FileStream(temporary, FileMode.Create, FileAccess.Write, FileShare.None))
                            { file.Write(validatedBytes, 0, validatedBytes.Length); file.Flush(true); }
                            if (ReadFile(temporary).Status != LoadStatus.Valid) return WriteStatus.Ambiguous;
                            if (fault?.Invoke("before-compatibility-backup") == true) throw new IOException("Injected before compatibility backup.");
                            if (File.Exists(primary + ".backup")) File.Replace(temporary, primary + ".backup", null);
                            else File.Move(temporary, primary + ".backup");
                        }
                        var written = ReadFile(primary);
                        return written.Status == LoadStatus.Valid && written.State!.Revision == candidate.State.Revision ? WriteStatus.Committed : WriteStatus.Ambiguous;
                    }
                }
                catch (Exception e) when (e is IOException || e is UnauthorizedAccessException || e is PlatformNotSupportedException ||
                    e is OverflowException || e is ArgumentException)
                { return promoted ? WriteStatus.Ambiguous : WriteStatus.Failed; }
                finally
                {
                    try { if (File.Exists(temporary)) File.Delete(temporary); }
                    catch (IOException) { } catch (UnauthorizedAccessException) { }
                }
            }
        }
    }
}
