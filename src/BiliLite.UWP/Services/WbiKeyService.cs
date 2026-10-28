using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using BiliLite.Helpers;

namespace BiliLite.Services
{
    public sealed class WbiKey
    {
        public WbiKey(string imgKey, string subKey)
        {
            ImgKey = imgKey;
            SubKey = subKey;
        }

        public string ImgKey { get; private set; }
        public string SubKey { get; private set; }
    }

    /// <summary>Fetches, validates, caches, and signs requests with the target's WBI keys.</summary>
    public sealed class WbiKeyService
    {
        private static readonly int[] MixinKeyOrder =
        {
            46, 47, 18, 2, 53, 8, 23, 32, 15, 50, 10, 31, 58, 3, 45, 35, 27, 43, 5, 49,
            33, 9, 42, 19, 29, 28, 14, 39, 12, 38, 41, 13, 37, 48, 7, 16, 24, 55, 40,
            61, 26, 17, 0, 1, 60, 51, 30, 4, 22, 25, 54, 21, 56, 59, 6, 63, 57, 62, 11,
            36, 20, 34, 44, 52
        };

        private readonly Func<Task<WbiKey>> fetch;
        private readonly Func<DateTimeOffset> clock;
        private readonly TimeSpan timeToLive;
        private readonly Action<WbiKey> onKeysChanged;
        private readonly object gate = new object();
        private WbiKey cached;
        private DateTimeOffset cachedAt;
        private Task<WbiKey> inFlight;

        public WbiKeyService(Func<Task<WbiKey>> fetch, TimeSpan? timeToLive = null,
            Func<DateTimeOffset> clock = null, Action<WbiKey> onKeysChanged = null)
        {
            this.fetch = fetch ?? throw new ArgumentNullException(nameof(fetch));
            var ttl = timeToLive ?? TimeSpan.FromHours(1);
            if (ttl <= TimeSpan.Zero || ttl > TimeSpan.FromDays(7) || double.IsNaN(ttl.TotalSeconds) || double.IsInfinity(ttl.TotalSeconds))
                throw new ArgumentOutOfRangeException(nameof(timeToLive), "WBI cache lifetime must be finite, positive, and at most seven days.");
            this.timeToLive = ttl;
            this.clock = clock ?? (() => DateTimeOffset.UtcNow);
            this.onKeysChanged = onKeysChanged;
        }

        public WbiKey CachedKeys
        {
            get { lock (gate) return cached; }
        }

        public bool HasCachedKeys
        {
            get { lock (gate) return cached != null; }
        }

        public bool HasValidCachedKeys
        {
            get { lock (gate) return IsFreshLocked(clock()); }
        }

        public void Seed(WbiKey keys)
        {
            Validate(keys);
            lock (gate)
            {
                cached = keys;
                cachedAt = clock();
            }
            onKeysChanged?.Invoke(keys);
        }

        public async Task<bool> LoadWbiKeyAsync()
        {
            var wasCached = HasValidCachedKeys;
            await GetKeysAsync();
            return !wasCached;
        }

        public async Task<WbiKey> GetKeysAsync()
        {
            Task<WbiKey> pending;
            TaskCompletionSource<WbiKey> producer = null;
            lock (gate)
            {
                var now = clock();
                if (IsFreshLocked(now)) return cached;
                if (inFlight != null) pending = inFlight;
                else
                {
                    producer = new TaskCompletionSource<WbiKey>(TaskCreationOptions.RunContinuationsAsynchronously);
                    pending = inFlight = producer.Task;
                }
            }

            if (producer != null) _ = FetchAndPublishAsync(producer);
            return await pending;
        }

        private bool IsFreshLocked(DateTimeOffset now)
        {
            if (cached == null || now < cachedAt || now - cachedAt >= timeToLive) return false;
            return true;
        }

        private async Task FetchAndPublishAsync(TaskCompletionSource<WbiKey> producer)
        {
            try
            {
                var result = await fetch();
                Validate(result);
                lock (gate)
                {
                    cached = result;
                    cachedAt = clock();
                    if (ReferenceEquals(inFlight, producer.Task)) inFlight = null;
                }
                onKeysChanged?.Invoke(result);
                producer.TrySetResult(result);
            }
            catch (Exception exception)
            {
                lock (gate)
                {
                    if (ReferenceEquals(inFlight, producer.Task)) inFlight = null;
                }
                producer.TrySetException(exception);
            }
        }

        private static void Validate(WbiKey keys)
        {
            if (keys == null || !IsWbiKey(keys.ImgKey) || !IsWbiKey(keys.SubKey))
                throw new FormatException("WBI key response did not contain valid image and sub keys.");
        }

        private static bool IsWbiKey(string key) => key != null && key.Length == 32 && key.All(Uri.IsHexDigit);

        public static void ValidateForApi(WbiKey keys) => Validate(keys);

        public async Task<string> GetWbiSignAsync(string query, DateTimeOffset? timestamp = null)
        {
            var keys = await GetKeysAsync();
            return Sign(query, keys, timestamp ?? clock());
        }

        public static string Sign(string query, WbiKey keys, DateTimeOffset timestamp)
        {
            Validate(keys);
            var parsed = HttpUtility.ParseQueryString(query ?? "");
            var parameters = parsed.AllKeys
                .Where(key => key != null)
                .ToDictionary(key => key, key => parsed[key] ?? "", StringComparer.Ordinal);
            parameters["wts"] = Math.Round((timestamp - new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero)).TotalSeconds)
                .ToString(CultureInfo.InvariantCulture);

            var ordered = parameters.OrderBy(pair => pair.Key, StringComparer.Ordinal).ToArray();
            var clean = ordered.Select(pair => new KeyValuePair<string, string>(pair.Key,
                new string(pair.Value.Where(character => "!'()*".IndexOf(character) < 0).ToArray()))).ToArray();
            var unsignedQuery = string.Join("&", clean.Select(pair => pair.Key + "=" + Uri.EscapeDataString(pair.Value)));
            var originKey = keys.ImgKey + keys.SubKey;
            var mixin = new string(MixinKeyOrder.Select(index => originKey[index]).Take(32).ToArray());
            var signature = Utils.ToMD5(unsignedQuery + mixin).ToLowerInvariant();
            return unsignedQuery + "&w_rid=" + signature;
        }
    }
}
