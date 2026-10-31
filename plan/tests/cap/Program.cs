using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace CapProbe
{
    /// <summary>
    /// CAP: the response-evidence rules that need no UWP runtime. ApiShape and the redaction
    /// helper are compiled from product source; the writer itself (Windows.Storage) is only
    /// checked as a wiring contract, because it cannot run outside the UWP target.
    /// </summary>
    public static class Program
    {
        private static int _passed;

        public static int Main()
        {
            ShapeDescribesPathsNotValues();
            ShapeCapsDepthAndKeys();
            ShapeDescribesArraysByElementType();
            DriftNamesTheChangedField();
            RenderStaysOneCompactLine();
            RedactionDropsCredentialFields();
            RedactionTruncatesByBytes();
            HttpHelperRecordsThroughApiCapture();
            BundleManifestNamesEveryFile();
            ExportReplacesTheDestinationWhole();
            ImportNormalizesBothInputs();

            Console.WriteLine("PASS " + _passed + " assertions");
            return 0;
        }

        private static void ShapeDescribesPathsNotValues()
        {
            var body = JToken.Parse(
                "{\"code\":0,\"message\":\"0\",\"data\":{\"items\":[{\"id_str\":\"1\",\"modules\":{\"module_author\":{\"name\":\"up\"}}}],\"has_more\":true}}");
            var shape = BiliLite.Services.ApiShape.Collect(body);

            AssertEqual("object", shape["$"], "root type");
            AssertEqual("number", shape["$.code"], "code is a number, not its value");
            AssertEqual("string", shape["$.data.items[].id_str"], "array elements are described under []");
            AssertEqual("object", shape["$.data.items[].modules.module_author"], "nested objects keep their path");
            AssertEqual("boolean", shape["$.data.has_more"], "booleans are named");
            Assert(!shape.Values.Any(value => value.Contains("up")), "shape never contains field values");
        }

        private static void ShapeCapsDepthAndKeys()
        {
            var deep = new JObject();
            var cursor = deep;
            for (var i = 0; i < 20; i++)
            {
                var next = new JObject();
                cursor["level" + i] = next;
                cursor = next;
            }
            var shape = BiliLite.Services.ApiShape.Collect(deep);
            Assert(shape.Keys.All(key => key.Split('.').Length <= BiliLite.Services.ApiShape.MaxDepth + 1),
                "depth is capped at MaxDepth");

            var wide = new JObject();
            for (var i = 0; i < 200; i++)
            {
                wide["k" + i] = i;
            }
            var wideShape = BiliLite.Services.ApiShape.Collect(wide);
            Assert(wideShape.Count <= BiliLite.Services.ApiShape.MaxKeys + 1,
                "a wide object is capped at MaxKeys (keys=" + wideShape.Count + ")");
        }

        private static void ShapeDescribesArraysByElementType()
        {
            var mixed = JToken.Parse("{\"items\":[{\"a\":1},\"text\",null]}");
            var shape = BiliLite.Services.ApiShape.Collect(mixed);
            AssertEqual("number", shape["$.items[].a"], "an object element is expanded");
            Assert(shape.ContainsKey("$.items[]"), "the array element path is present");
        }

        private static void DriftNamesTheChangedField()
        {
            var before = BiliLite.Services.ApiShape.Collect(JToken.Parse(
                "{\"data\":{\"items\":[{\"emoji\":{\"icon_url\":\"u\"}}],\"offset\":\"1\"}}"));
            var after = BiliLite.Services.ApiShape.Collect(JToken.Parse(
                "{\"data\":{\"items\":[{\"emoji\":\"x\"}],\"has_more\":true}}"));

            var drift = BiliLite.Services.ApiShape.Diff(before, after);
            Assert(drift.Contains("$.data.items[].emoji:object→string"),
                "a field that changes type is named with both types (drift=" + string.Join("|", drift) + ")");
            Assert(drift.Contains("-$.data.offset:string"), "a removed field is reported");
            Assert(drift.Contains("+$.data.has_more:boolean"), "an added field is reported");
            Assert(BiliLite.Services.ApiShape.Diff(before, before).Count == 0,
                "the same shape has no drift");
        }

        private static void RenderStaysOneCompactLine()
        {
            var shape = BiliLite.Services.ApiShape.Collect(JToken.Parse("{\"a\":1,\"b\":\"x\"}"));
            var rendered = BiliLite.Services.ApiShape.Render(shape);
            Assert(rendered == "$:object,$.a:number,$.b:string",
                "render is a stable one-line summary (got " + rendered + ")");
            Assert(!rendered.Contains("\n"), "the rendered shape is a single line");

            var many = new SortedDictionary<string, string>(StringComparer.Ordinal);
            for (var i = 0; i < 100; i++)
            {
                many["k" + i] = "string";
            }
            var capped = BiliLite.Services.ApiShape.Render(many, 10);
            Assert(capped.Contains("+90"), "render reports how many entries it dropped");
        }

        private static void RedactionDropsCredentialFields()
        {
            var body = "{\"data\":{\"SESSDATA\":\"secret-value\",\"bili_jct\":\"csrf\",\"w_rid\":\"sig\",\"normal\":\"kept\"}}";
            var redacted = BiliLite.Services.HttpLogSanitizer.RedactBody(body, 4096);
            Assert(!redacted.Contains("secret-value"), "SESSDATA value is dropped");
            Assert(!redacted.Contains("csrf"), "bili_jct value is dropped");
            Assert(!redacted.Contains("sig"), "w_rid value is dropped");
            Assert(redacted.Contains("kept"), "ordinary fields survive redaction");
        }

        private static void RedactionTruncatesByBytes()
        {
            var big = new string('a', 5000);
            var redacted = BiliLite.Services.HttpLogSanitizer.RedactBody(big, 100);
            Assert(redacted.Length < 5000 && redacted.EndsWith("…"), "a large body is truncated with a marker");
            Assert(System.Text.Encoding.UTF8.GetByteCount(redacted) <= 100 + 3,
                "the truncation respects the byte budget");
            Assert(BiliLite.Services.HttpLogSanitizer.RedactBody("", 100) == "", "an empty body stays empty");
        }

        /// <summary>
        /// 写盘用 Windows.Storage，探针里跑不了；这条只钉住"下一次请求仍然会经过采集"，即接线没有被删掉。
        /// </summary>
        private static void HttpHelperRecordsThroughApiCapture()
        {
            var root = RepoRoot();
            var helper = File.ReadAllText(Path.Combine(root, "src", "BiliLite.UWP", "Helpers", "HttpHelper.cs"));
            var records = System.Text.RegularExpressions.Regex.Matches(helper, "ApiCapture\\.Record\\(").Count;
            Assert(records >= 4, "GET and POST, success and status failure all record (calls=" + records + ")");
            Assert(helper.Contains("using BiliLite.Services;"), "HttpHelper reaches the capture service");

            var settings = File.ReadAllText(Path.Combine(root, "src", "BiliLite.UWP", "Pages", "SettingPage.xaml"));
            Assert(settings.Contains("swApiCapture") && settings.Contains("swApiCaptureSamples"),
                "the settings page exposes both toggles");

            var capture = File.ReadAllText(Path.Combine(root, "src", "BiliLite.UWP", "Services", "ApiCapture.cs"));
            Assert(capture.Contains("SettingHelper.Diagnostics.API_CAPTURE"), "capture is opt-in through its setting");
            Assert(capture.Contains("MaxDailyBytes"), "a daily byte budget bounds the file growth");
        }

        private static void BundleManifestNamesEveryFile()
        {
            var manifest = BiliLite.Services.DiagnosticBundle.BuildManifest(
                "1.0.9.15", "abc123", new DateTimeOffset(2026, 10, 31, 10, 0, 0, TimeSpan.Zero),
                true, false, new[] { "logs/20261031.log", "api/20261031.ndjson", "manifest.json" });
            var json = JObject.Parse(manifest);

            AssertEqual("1.0.9.15", json["appVersion"].ToString(), "manifest carries the app version");
            AssertEqual("abc123", json["sessionId"].ToString(), "manifest carries the session id");
            // JObject 会把 ISO 时间读成 DateTime，所以直接断原文，避免把格式问题当成内容问题
            Assert(manifest.Contains("\"exportedAt\": \"2026-10-31T10:00:00"), "manifest carries the export time as ISO text");
            Assert(json["apiCapture"].Value<bool>() && !json["apiCaptureSamples"].Value<bool>(),
                "manifest records which capture switches were on");
            AssertEqual(3, json["files"].Count(), "manifest lists every file in the bundle");
            Assert(json["files"].ToObject<string[]>().Contains("api/20261031.ndjson"),
                "the capture file is named with its folder inside the archive");

            AssertEqual("logs/a.log", BiliLite.Services.DiagnosticBundle.EntryPath("logs", "a.log"), "foldered entry path");
            AssertEqual("a.log", BiliLite.Services.DiagnosticBundle.EntryPath(null, "a.log"), "flat entry path");
            AssertEqual("", BiliLite.Services.DiagnosticBundle.EntryPath("logs", ""), "an empty name has no entry");
        }

        /// <summary>
        /// 选中一个更大的旧 zip 时，必须先把目标截断，否则新包后面留着旧的归档尾部（评审 2026-10-31）。
        /// 写包本身要在 UWP 里跑，所以这里钉住顺序：先截断/归零，再建 ZipArchive。
        /// </summary>
        private static void ExportReplacesTheDestinationWhole()
        {
            var page = File.ReadAllText(Path.Combine(RepoRoot(), "src", "BiliLite.UWP", "Pages", "SettingPage.xaml.cs"));
            var truncate = page.IndexOf("stream.Size = 0;", StringComparison.Ordinal);
            var create = page.IndexOf("ZipArchiveMode.Create", StringComparison.Ordinal);
            Assert(truncate > 0, "the bundle export truncates the destination stream");
            Assert(create > 0 && truncate < create, "truncation happens before the archive is created");
            Assert(page.Contains("stream.Seek(0);"), "the stream is rewound before writing");
        }

        /// <summary>
        /// 导出包解压后是一个目录，导入时必须和 zip 一样先选 api/ 子目录（评审 2026-10-31）。
        /// </summary>
        private static void ImportNormalizesBothInputs()
        {
            var tool = File.ReadAllText(Path.Combine(RepoRoot(), "tools", "import-capture.ps1"));
            var helper = tool.IndexOf("function Select-CaptureDir", StringComparison.Ordinal);
            Assert(helper > 0, "the importer has one folder-selection step");
            Assert(tool.Contains("Select-CaptureDir $resolved.Path") && tool.Contains("Select-CaptureDir $workDir"),
                "both the unpacked folder and the extracted zip go through the same step");
        }
        private static string RepoRoot()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null && !Directory.Exists(Path.Combine(dir.FullName, "plan", "tests")))
            {
                dir = dir.Parent;
            }
            return dir == null ? "" : dir.FullName;
        }

        private static void Assert(bool condition, string what)
        {
            if (!condition)
            {
                Console.WriteLine("FAIL " + what);
                Environment.Exit(1);
            }
            _passed++;
        }

        private static void AssertEqual<T>(T expected, T actual, string what)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
            {
                Console.WriteLine("FAIL " + what + " (expected " + expected + ", got " + actual + ")");
                Environment.Exit(1);
            }
            _passed++;
        }
    }
}
