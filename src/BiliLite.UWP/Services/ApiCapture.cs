using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BiliLite.Helpers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Windows.Storage;

namespace BiliLite.Services
{
    /// <summary>
    /// 响应证据采集：每次响应一行 NDJSON（端点、状态、耗时、字节数、形状、漂移），
    /// 形状基线按端点存在本地，字段类型一变就在那一行里指出来——不必再靠猜。
    /// 默认关闭；采集失败一律不影响请求本身。
    /// </summary>
    public static class ApiCapture
    {
        /// <summary>单个正文样本的上限；样本默认不采</summary>
        public const int MaxSampleBytes = 4096;

        /// <summary>一天最多写多少字节，超了当天就不再写</summary>
        private const int MaxDailyBytes = 512 * 1024;

        private static readonly SemaphoreSlim Gate = new SemaphoreSlim(1, 1);

        public static bool IsEnabled
        {
            get { return SettingHelper.GetValue(SettingHelper.Diagnostics.API_CAPTURE, false); }
        }

        public static bool SamplesEnabled
        {
            get { return SettingHelper.GetValue(SettingHelper.Diagnostics.API_CAPTURE_SAMPLES, false); }
        }

        /// <summary>失败返回、无正文的返回也记，状态码本身就是证据。</summary>
        public static void Record(HttpResults results, long elapsedMs)
        {
            if (results == null || !IsEnabled)
            {
                return;
            }
            try
            {
                WriteAsync(results, elapsedMs).ContinueWith(task => { }, TaskContinuationOptions.OnlyOnFaulted);
            }
            catch (Exception)
            {
                // 采集失败绝不能影响请求
            }
        }

        /// <summary>
        /// 组装一行（不写盘、不读盘）。形状、漂移与样本都在这里决定，
        /// 所以探针可以对同一份正文直接断言这一行。
        /// </summary>
        public static string BuildLine(HttpResults results, long elapsedMs,
            IDictionary<string, string> baseline, out IDictionary<string, string> shape)
        {
            shape = null;
            if (results == null)
            {
                return null;
            }
            var body = results.results;
            var line = new JObject
            {
                ["ts"] = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fff", CultureInfo.InvariantCulture),
                ["ep"] = results.Endpoint ?? "",
                ["method"] = results.Method ?? "",
                ["request"] = results.RequestId ?? "",
                ["status"] = results.status,
                ["code"] = results.code,
                ["ms"] = elapsedMs,
                ["bytes"] = string.IsNullOrEmpty(body) ? 0 : Encoding.UTF8.GetByteCount(body),
            };

            JToken token = null;
            if (!string.IsNullOrEmpty(body))
            {
                try
                {
                    token = JToken.Parse(body);
                }
                catch (Exception)
                {
                    line["shapeError"] = "body is not json";
                }
            }

            if (token != null)
            {
                shape = ApiShape.Collect(token);
                var shapeJson = new JObject();
                foreach (var pair in shape)
                {
                    shapeJson[pair.Key] = pair.Value;
                }
                line["shape"] = shapeJson;
                if (baseline != null)
                {
                    var drift = ApiShape.Diff(baseline, shape);
                    if (drift.Count > 0)
                    {
                        line["drift"] = new JArray(drift);
                    }
                }
                if (SamplesEnabled)
                {
                    var sample = HttpLogSanitizer.RedactBody(body, MaxSampleBytes);
                    if (!string.IsNullOrEmpty(sample))
                    {
                        line["sample"] = sample;
                    }
                }
            }

            return line.ToString(Formatting.None);
        }

        /// <summary>端点 → 基线文件名用的键：只留字母数字，避免路径里出现非法字符。</summary>
        public static string BaselineKey(HttpResults results)
        {
            var endpoint = results == null ? null : results.Endpoint;
            if (string.IsNullOrEmpty(endpoint))
            {
                return "unknown";
            }
            var builder = new StringBuilder();
            foreach (var c in endpoint)
            {
                builder.Append(char.IsLetterOrDigit(c) || c == '-' || c == '_' ? c : '_');
            }
            return builder.ToString();
        }

        private static async Task WriteAsync(HttpResults results, long elapsedMs)
        {
            await Gate.WaitAsync();
            try
            {
                var folder = await GetFolderAsync();
                var key = BaselineKey(results);
                var baseline = await ReadBaselineAsync(folder, key);
                IDictionary<string, string> shape;
                var line = BuildLine(results, elapsedMs, baseline, out shape);
                if (line == null)
                {
                    return;
                }
                var file = await folder.CreateFileAsync(DailyFileName(), CreationCollisionOption.OpenIfExists);
                var properties = await file.GetBasicPropertiesAsync();
                if (properties.Size >= MaxDailyBytes)
                {
                    return;
                }
                await FileIO.AppendTextAsync(file, line + "\n");
                // 第一次见到，或者形状变了：把这次的形状记成新基线，漂移只报一次
                if (shape != null && (baseline == null || baseline.Count == 0 || JObject.Parse(line)["drift"] != null))
                {
                    await WriteBaselineAsync(folder, key, shape);
                }
            }
            finally
            {
                Gate.Release();
            }
        }

        private static async Task<IDictionary<string, string>> ReadBaselineAsync(StorageFolder folder, string key)
        {
            try
            {
                var file = await folder.TryGetItemAsync(BaselineFileName(key)) as StorageFile;
                if (file == null)
                {
                    return null;
                }
                var text = await FileIO.ReadTextAsync(file);
                return JsonConvert.DeserializeObject<Dictionary<string, string>>(text);
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static async Task WriteBaselineAsync(StorageFolder folder, string key, IDictionary<string, string> shape)
        {
            try
            {
                var file = await folder.CreateFileAsync(BaselineFileName(key), CreationCollisionOption.ReplaceExisting);
                await FileIO.WriteTextAsync(file, JsonConvert.SerializeObject(shape));
            }
            catch (Exception)
            {
                // 基线写不进去只影响下一次的漂移判断
            }
        }

        private static string DailyFileName()
        {
            return DateTime.Now.ToString("yyyyMMdd", CultureInfo.InvariantCulture) + ".ndjson";
        }

        private static string BaselineFileName(string key)
        {
            return "baseline-" + (string.IsNullOrEmpty(key) ? "unknown" : key) + ".json";
        }

        private static async Task<StorageFolder> GetFolderAsync()
        {
            var log = await ApplicationData.Current.LocalFolder.CreateFolderAsync("log", CreationCollisionOption.OpenIfExists);
            return await log.CreateFolderAsync("api", CreationCollisionOption.OpenIfExists);
        }
    }
}
