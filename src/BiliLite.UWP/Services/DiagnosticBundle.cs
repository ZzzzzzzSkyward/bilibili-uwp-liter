using System;
using System.Collections.Generic;
using System.Globalization;
using Newtonsoft.Json.Linq;

namespace BiliLite.Services
{
    /// <summary>
    /// 诊断包的清单：哪台版本、哪一次会话、哪一刻导出、包里有哪些文件、采集开关当时是什么状态。
    /// 只有拼 JSON，所以探针能直接断言；打包（zip）留给页面，因为那要 UWP 的存储流。
    /// </summary>
    public static class DiagnosticBundle
    {
        public const string ManifestName = "manifest.json";

        public static string BuildManifest(string appVersion, string sessionId, DateTimeOffset exportedAt,
            bool apiCapture, bool apiCaptureSamples, IEnumerable<string> fileNames)
        {
            var files = new JArray();
            if (fileNames != null)
            {
                foreach (var name in fileNames)
                {
                    if (string.IsNullOrEmpty(name))
                    {
                        continue;
                    }
                    files.Add(name);
                }
            }
            var manifest = new JObject
            {
                ["tool"] = "BiliLite diagnostic bundle",
                ["appVersion"] = appVersion ?? "",
                ["sessionId"] = sessionId ?? "",
                ["exportedAt"] = exportedAt.ToString("o", CultureInfo.InvariantCulture),
                ["apiCapture"] = apiCapture,
                ["apiCaptureSamples"] = apiCaptureSamples,
                ["files"] = files,
            };
            return manifest.ToString(Newtonsoft.Json.Formatting.Indented);
        }

        /// <summary>包内路径：日志放 logs/，响应证据放 api/，其余平铺。</summary>
        public static string EntryPath(string folderName, string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                return "";
            }
            return string.IsNullOrEmpty(folderName) ? fileName : folderName + "/" + fileName;
        }
    }
}
