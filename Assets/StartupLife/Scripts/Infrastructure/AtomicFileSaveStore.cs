#nullable enable
using System;
using System.IO;
using StartupLife.Core;

namespace StartupLife.Infrastructure
{
    public sealed class AtomicFileSaveStore : ISaveStore
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
                if (first.Status == LoadStatus.Valid || first.Status == LoadStatus.Unreadable || first.Status == LoadStatus.FutureVersion || first.Status == LoadStatus.UnsupportedContent) return first;
                var backup = ReadFile(primary + ".backup");
                if (backup.Status == LoadStatus.Valid) return new LoadResult(LoadStatus.RecoveredBackup, backup.State, "save.recovered_backup");
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
                        if (candidate.Status != LoadStatus.Valid || candidate.State!.Revision != checked(expectedRevision + 1)) return WriteStatus.Failed;
                        var previous = Read();
                        if (previous.Status != LoadStatus.Valid && previous.Status != LoadStatus.RecoveredBackup && !(previous.Status == LoadStatus.Missing && expectedRevision == 0)) return WriteStatus.Failed;
                        if (previous.State != null && previous.State.Revision != expectedRevision) return WriteStatus.Failed;
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
                        var written = ReadFile(primary);
                        return written.Status == LoadStatus.Valid && written.State!.Revision == candidate.State.Revision ? WriteStatus.Committed : WriteStatus.Ambiguous;
                    }
                }
                catch (Exception e) when (e is IOException || e is UnauthorizedAccessException || e is PlatformNotSupportedException || e is OverflowException)
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
