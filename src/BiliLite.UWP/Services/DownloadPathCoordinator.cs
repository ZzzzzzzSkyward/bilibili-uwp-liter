using System;
using System.Collections.Generic;
using System.Linq;

namespace BiliLite.Services
{
    public enum DownloadPathFailure { Busy, Changed, InvalidPath, Reconciliation }
    public sealed class DownloadPathException : InvalidOperationException
    {
        public DownloadPathFailure Failure { get; }
        public DownloadPathException(DownloadPathFailure failure, Exception inner = null) : base("下载目录当前不可操作，请刷新后重试", inner) { Failure = failure; }
    }

    // Only short registry operations hold this monitor. Logical leases span storage awaits.
    public static class DownloadPathCoordinator
    {
        private static readonly object Sync = new object();
        private static readonly List<Lease> Active = new List<Lease>();
        private static readonly Dictionary<string, long> Creations = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
        private static long revision;
        private static long evidenceRevision;
        private static readonly Dictionary<string, long> Evidence = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, long> Partials = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, Generation> Generations = new Dictionary<string, Generation>(StringComparer.OrdinalIgnoreCase);

        // One current record per episode. No disk schema or transfer-lifetime lease is involved.
        public sealed class Generation
        {
            internal Generation(string path, long version, int required) { Path = path; Revision = version; Required = required; }
            internal readonly string Path;
            internal readonly long Revision;
            internal readonly int Required;
            internal bool PreparationSettled, Prepared, Failed;
            internal int Succeeded;
            internal bool Complete { get { return Required > 0 && PreparationSettled && Prepared && !Failed && Succeeded == Required; } }
        }
        public sealed class ReadStamp
        {
            internal ReadStamp(long creation, long evidence) { Creation = creation; Evidence = evidence; }
            internal readonly long Creation, Evidence;
        }
        private static void Touch(string path) { Evidence[path] = ++evidenceRevision; }
        private static long EvidenceFor(string path) { long value; return Evidence.TryGetValue(path, out value) ? value : 0; }
        public static Generation BeginGeneration(Lease lease, int required)
        {
            lock (Sync)
            {
                if (!Active.Contains(lease) || Revision(lease.Path) != lease.Revision) throw new DownloadPathException(DownloadPathFailure.Changed);
                var generation = new Generation(lease.Path, lease.Revision, required);
                Generations[lease.Path] = generation;
                Touch(lease.Path);
                return generation;
            }
        }
        private static bool Current(Generation generation)
        {
            Generation current;
            return Generations.TryGetValue(generation.Path, out current) && ReferenceEquals(current, generation) && Revision(generation.Path) == generation.Revision;
        }
        public static void PreparationSettled(Generation generation, bool success)
        {
            lock (Sync)
            {
                if (!Current(generation)) return;
                generation.PreparationSettled = true; generation.Prepared = success;
                Touch(generation.Path);
            }
        }
        public static void TransferSettled(Generation generation, bool success)
        {
            lock (Sync)
            {
                if (!Current(generation)) return;
                if (success) generation.Succeeded++; else generation.Failed = true;
                Touch(generation.Path);
            }
        }
        public static void MarkPartial(string path)
        {
            path = Normalize(path);
            lock (Sync) { Partials[path] = Revision(path); Touch(path); }
        }
        private static bool Blocked(string path)
        {
            Generation generation;
            if (Generations.TryGetValue(path, out generation) && generation.Revision == Revision(path))
            {
                if (!generation.Complete) return true;
                long partialRevision;
                if (Partials.TryGetValue(path, out partialRevision) && generation.Revision > partialRevision)
                { Partials.Remove(path); Touch(path); }
            }
            return Partials.ContainsKey(path);
        }
        public static ReadStamp CaptureRead(string path)
        {
            path = Normalize(path);
            lock (Sync) { Blocked(path); return new ReadStamp(Revision(path), EvidenceFor(path)); }
        }
        public static bool CanPublish(string path, ReadStamp stamp)
        {
            path = Normalize(path);
            lock (Sync) return !Blocked(path) && stamp.Creation == Revision(path) && stamp.Evidence == EvidenceFor(path);
        }
        public static long CompletedRevision(string path)
        {
            path = Normalize(path);
            lock (Sync)
            {
                Generation generation;
                return !Blocked(path) && Generations.TryGetValue(path, out generation) && generation.Complete && generation.Revision == Revision(path) ? generation.Revision : 0;
            }
        }
        public static void CleanupSucceeded(Lease lease)
        {
            lock (Sync)
            {
                if (!Active.Contains(lease) || Revision(lease.Path) != lease.Revision) throw new DownloadPathException(DownloadPathFailure.Changed);
                foreach (var path in Partials.Keys.Concat(Generations.Keys).Distinct(StringComparer.OrdinalIgnoreCase).Where(x => Contains(lease.Path, x)).ToList())
                { Partials.Remove(path); Generations.Remove(path); Touch(path); }
            }
        }

        public static string Normalize(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) throw new DownloadPathException(DownloadPathFailure.InvalidPath);
            path = path.Replace('/', '\\').TrimEnd('\\');
            if (!(path.StartsWith("\\", StringComparison.Ordinal) || (path.Length > 2 && char.IsLetter(path[0]) && path[1] == ':' && path[2] == '\\')))
                throw new DownloadPathException(DownloadPathFailure.InvalidPath);
            var parts = path.Split('\\');
            if (parts.Any(x => x == "." || x == "..")) throw new DownloadPathException(DownloadPathFailure.InvalidPath);
            var prefix = path.StartsWith("\\\\", StringComparison.Ordinal) ? "\\\\" : (path.StartsWith("\\", StringComparison.Ordinal) ? "\\" : "");
            return prefix + string.Join("\\", parts.Where(x => x.Length > 0)).ToUpperInvariant();
        }
        public static string Child(string parent, string name)
        {
            if (string.IsNullOrWhiteSpace(name) || name.Any(char.IsControl) || name != name.Trim() || name.IndexOfAny(new[] { '/', '\\', ':', '<', '>', '"', '|', '?', '*' }) >= 0 || name == "." || name == ".." || name.EndsWith(".", StringComparison.Ordinal))
                throw new DownloadPathException(DownloadPathFailure.InvalidPath);
            var stem = name.Split('.')[0].ToUpperInvariant();
            if (stem == "CON" || stem == "PRN" || stem == "AUX" || stem == "NUL" ||
                (stem.Length == 4 && (stem.StartsWith("COM", StringComparison.Ordinal) || stem.StartsWith("LPT", StringComparison.Ordinal)) && stem[3] >= '1' && stem[3] <= '9'))
                throw new DownloadPathException(DownloadPathFailure.InvalidPath);
            return Normalize(parent) + "\\" + name.ToUpperInvariant();
        }
        public static bool Contains(string parent, string child)
        {
            parent = Normalize(parent); child = Normalize(child);
            return child == parent || child.StartsWith(parent + "\\", StringComparison.OrdinalIgnoreCase);
        }
        public static Lease Acquire(string path, bool creation = false, long? expectedRevision = null)
        {
            path = Normalize(path);
            lock (Sync)
            {
                if (Active.Any(x => Contains(x.Path, path) || Contains(path, x.Path))) throw new DownloadPathException(DownloadPathFailure.Busy);
                var current = Revision(path);
                if (expectedRevision.HasValue && current != expectedRevision.Value) throw new DownloadPathException(DownloadPathFailure.Changed);
                if (creation) { Creations[path] = ++revision; current = Revision(path); Touch(path); }
                var lease = new Lease(path, current); Active.Add(lease); return lease;
            }
        }
        private static long Revision(string path)
        {
            return Creations.Where(x => Contains(path, x.Key)).Select(x => x.Value).DefaultIfEmpty(0).Max();
        }
        public sealed class Lease : IDisposable
        {
            internal Lease(string path, long version) { Path = path; Revision = version; }
            public string Path { get; }
            public long Revision { get; }
            public void Dispose() { lock (Sync) Active.Remove(this); }
        }
    }
}
