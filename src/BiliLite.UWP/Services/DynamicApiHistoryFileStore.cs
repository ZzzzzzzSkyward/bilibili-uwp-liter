using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using BiliLite.Helpers;
using Newtonsoft.Json;
using Windows.Storage;

namespace BiliLite.Services
{
    /// <summary>
    /// 把选择器的历史落到应用本地目录:版本化快照、最多 32 个桶、写入合并。
    /// 只存时间戳、结果、耗时、偏好和冷却状态;不存凭据、条目 ID 或游标。
    /// 文件缺失、版本不符或读不出来都当作没有历史;写盘失败只是丢掉持久化,选择照常在内存里工作。
    /// </summary>
    internal sealed class DynamicApiHistoryFileStore : IDynamicApiHistoryStore
    {
        /// <summary>快照格式版本,不一致就整份丢弃</summary>
        public const int SnapshotVersion = 1;

        /// <summary>最多保留的操作/上下文桶数,超出按最近使用淘汰</summary>
        public const int MaxBuckets = 32;

        public const string FileName = "dynamic-api-history.v1.json";

        private static readonly TimeSpan WriteCoalesceDelay = TimeSpan.FromSeconds(2);

        private readonly object _gate = new object();
        private readonly object _writeGate = new object();
        private readonly Dictionary<string, BucketSnapshot> _buckets =
            new Dictionary<string, BucketSnapshot>(StringComparer.Ordinal);

        private bool _loaded;
        private bool _dirty;
        private bool _writeFailureLogged;
        private long _order;
        private string _path;
        private Timer _flushTimer;

        public DynamicApiBucketState Read(string bucketKey)
        {
            if (string.IsNullOrEmpty(bucketKey))
                return null;
            lock (_gate)
            {
                EnsureLoaded();
                BucketSnapshot snapshot;
                if (!_buckets.TryGetValue(bucketKey, out snapshot))
                    return null;
                return ToBucket(snapshot);
            }
        }

        public void Write(DynamicApiBucketState bucket)
        {
            if (bucket == null || string.IsNullOrEmpty(bucket.Key))
                return;
            lock (_gate)
            {
                EnsureLoaded();
                var snapshot = FromBucket(bucket);
                snapshot.Order = ++_order;
                _buckets[bucket.Key] = snapshot;
                EvictOldest();
                _dirty = true;
                ScheduleFlush();
            }
        }

        private void EnsureLoaded()
        {
            if (_loaded)
                return;
            _loaded = true;

            string path;
            string json;
            try
            {
                path = ResolvePath();
                if (!File.Exists(path))
                    return;
                json = File.ReadAllText(path);
            }
            catch (Exception ex)
            {
                // 读不到就从头开始,不影响内存里的选择
                LogDiscard("unreadable", ex);
                return;
            }

            HistoryFile file;
            try
            {
                file = JsonConvert.DeserializeObject<HistoryFile>(json);
            }
            catch (Exception ex)
            {
                LogDiscard("unreadable", ex);
                return;
            }

            // 版本号是唯一的结构契约:不匹配就整份当作没写过
            if (file == null || file.Version != SnapshotVersion || file.Buckets == null)
            {
                LogDiscard("incompatible", null);
                return;
            }

            foreach (var snapshot in file.Buckets)
            {
                if (!IsUsable(snapshot))
                    continue;
                _buckets[snapshot.Key] = snapshot;
                if (snapshot.Order > _order)
                    _order = snapshot.Order;
            }
            if (_buckets.Count > MaxBuckets)
            {
                EvictOldest();
                _dirty = true;
                ScheduleFlush();
            }
        }

        private static bool IsUsable(BucketSnapshot snapshot)
        {
            if (snapshot == null || string.IsNullOrEmpty(snapshot.Key))
                return false;
            return Enum.IsDefined(typeof(DynamicApiOperation), snapshot.Operation);
        }

        /// <summary>只保留最近使用的 MaxBuckets 个桶</summary>
        private void EvictOldest()
        {
            if (_buckets.Count <= MaxBuckets)
                return;
            var victims = _buckets.Values
                .OrderBy(b => b.Order)
                .Take(_buckets.Count - MaxBuckets)
                .Select(b => b.Key)
                .ToList();
            foreach (var key in victims)
                _buckets.Remove(key);
        }

        private void ScheduleFlush()
        {
            if (_flushTimer == null)
                _flushTimer = new Timer(OnFlushTimer, null, WriteCoalesceDelay, Timeout.InfiniteTimeSpan);
            else
                _flushTimer.Change(WriteCoalesceDelay, Timeout.InfiniteTimeSpan);
        }

        private void OnFlushTimer(object state)
        {
            Flush();
        }

        /// <summary>把当前的快照写成一份文件;写盘串行,失败只记一条并把脏标记留下一次重试</summary>
        private void Flush()
        {
            HistoryFile payload;
            lock (_gate)
            {
                if (!_dirty)
                    return;
                _dirty = false;
                payload = new HistoryFile
                {
                    Version = SnapshotVersion,
                    Buckets = _buckets.Values.OrderBy(b => b.Order).ToList(),
                };
            }

            lock (_writeGate)
            {
                try
                {
                    WriteFile(payload);
                    _writeFailureLogged = false;
                }
                catch (Exception ex)
                {
                    lock (_gate)
                    {
                        _dirty = true;
                    }
                    if (!_writeFailureLogged)
                    {
                        _writeFailureLogged = true;
                        LogHelper.Log("Dynamic api history stage=persist-write-failed; selection continues in memory"
                            + " error=" + ex.GetType().Name, LogType.ERROR, ex);
                    }
                }
            }
        }

        private void WriteFile(HistoryFile payload)
        {
            var path = ResolvePath();
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            var json = JsonConvert.SerializeObject(payload, Formatting.None);
            var temp = path + ".tmp";
            File.WriteAllText(temp, json);
            try
            {
                if (File.Exists(path))
                    File.Replace(temp, path, null);
                else
                    File.Move(temp, path);
            }
            catch (Exception)
            {
                // 目标被占用时退化成覆盖写:半截文件会被读侧当作无历史
                File.Copy(temp, path, true);
                File.Delete(temp);
            }
        }

        private void LogDiscard(string reason, Exception ex)
        {
            LogHelper.Log("Dynamic api history stage=discard reason=" + reason + "; starting fresh", LogType.INFO, ex);
        }

        private string ResolvePath()
        {
            if (_path == null)
                _path = Path.Combine(ApplicationData.Current.LocalFolder.Path, FileName);
            return _path;
        }

        // ------------------------------------------------------------ 快照模型

        private sealed class HistoryFile
        {
            public int Version { get; set; }
            public List<BucketSnapshot> Buckets { get; set; }
        }

        private sealed class BucketSnapshot
        {
            public string Key { get; set; }
            public DynamicApiOperation Operation { get; set; }
            public long AccountId { get; set; }
            public string LoginAppKey { get; set; }
            public string LegacyAndroidAppKey { get; set; }
            public DynamicApiSource? PreferredSource { get; set; }
            public DateTimeOffset? LastAttemptAt { get; set; }
            public DateTimeOffset? LastUsedAt { get; set; }
            /// <summary>最近一次写入的序号,持久化后仍按它做 LRU</summary>
            public long Order { get; set; }
            public SourceSnapshot Grpc { get; set; }
            public SourceSnapshot Legacy { get; set; }
        }

        private sealed class SourceSnapshot
        {
            public DateTimeOffset? LastAttemptAt { get; set; }
            public DateTimeOffset? CooldownUntil { get; set; }
            public int RejectionCount { get; set; }
            public int TransientFailureCount { get; set; }
            public DateTimeOffset? LastTransientFailureAt { get; set; }
            public string LastOutcome { get; set; }
            public DateTimeOffset? LastOutcomeAt { get; set; }
            public int LastBusinessCode { get; set; }
            public List<SampleSnapshot> Samples { get; set; }
        }

        private sealed class SampleSnapshot
        {
            public DateTimeOffset At { get; set; }
            public bool Success { get; set; }
            public double ElapsedMs { get; set; }
        }

        private static BucketSnapshot FromBucket(DynamicApiBucketState bucket)
        {
            return new BucketSnapshot
            {
                Key = bucket.Key,
                Operation = bucket.Operation,
                AccountId = bucket.AccountId,
                LoginAppKey = bucket.LoginAppKey ?? string.Empty,
                LegacyAndroidAppKey = bucket.LegacyAndroidAppKey ?? string.Empty,
                PreferredSource = bucket.PreferredSource,
                LastAttemptAt = bucket.LastAttemptAt,
                LastUsedAt = bucket.LastUsedAt,
                Grpc = FromState(bucket.Grpc),
                Legacy = FromState(bucket.Legacy),
            };
        }

        private static SourceSnapshot FromState(DynamicApiSourceState state)
        {
            if (state == null)
                return new SourceSnapshot { Samples = new List<SampleSnapshot>() };
            var samples = new List<SampleSnapshot>();
            if (state.Samples != null)
            {
                var skip = state.Samples.Count - DynamicApiSelector.MaxSamplesPerSource;
                if (skip < 0)
                    skip = 0;
                for (var i = skip; i < state.Samples.Count; i++)
                {
                    var sample = state.Samples[i];
                    samples.Add(new SampleSnapshot
                    {
                        At = sample.At,
                        Success = sample.Success,
                        ElapsedMs = sample.ElapsedMs,
                    });
                }
            }
            return new SourceSnapshot
            {
                LastAttemptAt = state.LastAttemptAt,
                CooldownUntil = state.CooldownUntil,
                RejectionCount = state.RejectionCount,
                TransientFailureCount = state.TransientFailureCount,
                LastTransientFailureAt = state.LastTransientFailureAt,
                LastOutcome = state.LastOutcome.HasValue ? state.LastOutcome.Value.ToString() : null,
                LastOutcomeAt = state.LastOutcomeAt,
                LastBusinessCode = state.LastBusinessCode,
                Samples = samples,
            };
        }

        private static DynamicApiBucketState ToBucket(BucketSnapshot snapshot)
        {
            var bucket = new DynamicApiBucketState
            {
                Key = snapshot.Key,
                Operation = snapshot.Operation,
                AccountId = snapshot.AccountId,
                LoginAppKey = snapshot.LoginAppKey ?? string.Empty,
                LegacyAndroidAppKey = snapshot.LegacyAndroidAppKey ?? string.Empty,
                PreferredSource = snapshot.PreferredSource,
                LastAttemptAt = snapshot.LastAttemptAt,
                LastUsedAt = snapshot.LastUsedAt,
            };
            bucket.Grpc = ToState(snapshot.Grpc, DynamicApiSource.Grpc);
            bucket.Legacy = ToState(snapshot.Legacy, DynamicApiSource.Legacy);
            return bucket;
        }

        private static DynamicApiSourceState ToState(SourceSnapshot snapshot, DynamicApiSource source)
        {
            var state = new DynamicApiSourceState { Source = source };
            if (snapshot == null)
                return state;

            state.LastAttemptAt = snapshot.LastAttemptAt;
            state.CooldownUntil = snapshot.CooldownUntil;
            state.RejectionCount = snapshot.RejectionCount < 0 ? 0 : snapshot.RejectionCount;
            state.TransientFailureCount = snapshot.TransientFailureCount < 0 ? 0 : snapshot.TransientFailureCount;
            state.LastTransientFailureAt = snapshot.LastTransientFailureAt;
            state.LastOutcomeAt = snapshot.LastOutcomeAt;
            state.LastBusinessCode = snapshot.LastBusinessCode;

            DynamicApiOutcome outcome;
            if (!string.IsNullOrEmpty(snapshot.LastOutcome)
                && Enum.TryParse<DynamicApiOutcome>(snapshot.LastOutcome, true, out outcome)
                && Enum.IsDefined(typeof(DynamicApiOutcome), outcome))
            {
                state.LastOutcome = outcome;
            }

            if (snapshot.Samples != null)
            {
                foreach (var sample in snapshot.Samples)
                {
                    if (sample == null)
                        continue;
                    state.Samples.Add(new DynamicApiSample
                    {
                        At = sample.At,
                        Success = sample.Success,
                        ElapsedMs = sample.ElapsedMs < 0d ? 0d : sample.ElapsedMs,
                    });
                }
                var excess = state.Samples.Count - DynamicApiSelector.MaxSamplesPerSource;
                if (excess > 0)
                    state.Samples.RemoveRange(0, excess);
            }
            return state;
        }
    }
}
