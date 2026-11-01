using System;
using System.IO;
using BiliLite.Helpers;
using Newtonsoft.Json.Linq;

namespace Cm1Probe
{
    /// <summary>
    /// Cm1: 评论里的图片必须能加载出来，出问题时必须留下痕迹。
    ///
    /// 执行部分直接编译产品源码 Helpers/CommentRichText.cs：地址规范化、表情标记生成、
    /// 链接/av号/表情的拼接顺序都在里面，改坏它就等于改坏这里的断言。
    /// XamlReader 和 Image 只能跑在 UWP 里，所以 ControlHelper、AppHelper、CommentControl、
    /// EmoteVM 按文本读取，这类断言一律以 "source:" 开头。
    /// </summary>
    public static class Program
    {
        private static int _passed;

        private const string RichTextSource = @"src/BiliLite.UWP/Helpers/CommentRichText.cs";
        private const string ControlHelper = @"src/BiliLite.UWP/Helpers/ControlHelper.cs";
        private const string AppHelper = @"src/BiliLite.UWP/Helpers/AppHelper.cs";
        private const string CommentXaml = @"src/BiliLite.UWP/Controls/CommentControl.xaml";
        private const string CommentCode = @"src/BiliLite.UWP/Controls/CommentControl.xaml.cs";
        private const string EmoteVM = @"src/BiliLite.UWP/Modules/EmoteVM.cs";
        private const string TransparentAsset = @"src/BiliLite.UWP/Assets/Thumbnails/transparent.png";

        public static int Main()
        {
            NormalizesEveryCommentImageAddress();
            BuildsEmoteMarkupFromCompleteEntries();
            LeavesIncompleteEntriesAsText();
            EmotesComeLast();
            LinksAndVideoIdsStillWork();
            SourceGuardsKeepTheOldBugsOut();

            Console.WriteLine("PASS " + _passed + " assertions");
            return 0;
        }

        /// <summary>
        /// 验收场景:唯一一条地址规范化规则——空白去掉、// 补协议、http 升级 https、其余不动
        /// </summary>
        private static void NormalizesEveryCommentImageAddress()
        {
            var table = new[]
            {
                new[] { null, "" },
                new[] { "", "" },
                new[] { "   ", "" },
                new[] { "\t", "" },
                new[] { "  https://i0.hdslb.com/a.png  ", "https://i0.hdslb.com/a.png" },
                new[] { "\t//i0.hdslb.com/a.png\n", "https://i0.hdslb.com/a.png" },
                new[] { "//i0.hdslb.com/a.png", "https://i0.hdslb.com/a.png" },
                new[] { "http://i0.hdslb.com/a.png", "https://i0.hdslb.com/a.png" },
                new[] { "  http://i0.hdslb.com/a.png", "https://i0.hdslb.com/a.png" },
                new[] { "HTTP://i0.hdslb.com/a.png", "https://i0.hdslb.com/a.png" },
                new[] { "Http://i0.hdslb.com/a.png", "https://i0.hdslb.com/a.png" },
                new[] { "https://i0.hdslb.com/a.png", "https://i0.hdslb.com/a.png" },
                new[] { "ms-appx:///Assets/Thumbnails/transparent.png", "ms-appx:///Assets/Thumbnails/transparent.png" },
                new[] { "https://i0.hdslb.com/a.png@200w.jpg", "https://i0.hdslb.com/a.png@200w.jpg" },
                new[] { "//", "https://" },
            };

            foreach (var row in table)
            {
                var actual = CommentRichText.NormalizeImageUrl(row[0]);
                Check(actual == row[1], "normalise " + Quote(row[0]) + " -> " + Quote(row[1]));
            }
        }

        /// <summary>
        /// 验收场景:字段齐全的表情条目拼出图片，尺寸 1 是 20，其余是 36
        /// </summary>
        private static void BuildsEmoteMarkupFromCompleteEntries()
        {
            var markup = CommentRichText.BuildEmoteMarkup(Emote("https://i0.hdslb.com/bfs/emote/a.png", 1));
            Check(markup != null, "a complete entry builds markup");
            Check(markup.IndexOf("<Image Source=\"https://i0.hdslb.com/bfs/emote/a.png\"", StringComparison.Ordinal) >= 0,
                "the image source is the entry url");
            Check(markup.IndexOf("Width=\"20\"", StringComparison.Ordinal) >= 0, "size 1 renders 20px");
            Check(markup.IndexOf("<InlineUIContainer>", StringComparison.Ordinal) >= 0,
                "the image is inline, not a hyperlink");

            var big = CommentRichText.BuildEmoteMarkup(Emote("https://i0.hdslb.com/bfs/emote/b.png", 2));
            Check(big != null && big.IndexOf("Width=\"36\"", StringComparison.Ordinal) >= 0, "size 2 renders 36px");

            var schemeRelative = CommentRichText.BuildEmoteMarkup(Emote("//i0.hdslb.com/bfs/emote/c.png", 1));
            Check(schemeRelative != null
                && schemeRelative.IndexOf("Source=\"https://i0.hdslb.com/bfs/emote/c.png\"", StringComparison.Ordinal) >= 0,
                "a scheme-relative emote url is upgraded to https");

            var plain = CommentRichText.BuildEmoteMarkup(Emote("http://i0.hdslb.com/bfs/emote/d.png", 1));
            Check(plain != null
                && plain.IndexOf("Source=\"https://i0.hdslb.com/bfs/emote/d.png\"", StringComparison.Ordinal) >= 0,
                "an http emote url is upgraded to https");

            var ampersand = CommentRichText.BuildEmoteMarkup(Emote("https://i0.hdslb.com/a.png?x=1&y=2", 1));
            Check(ampersand != null && ampersand.IndexOf("x=1&amp;y=2", StringComparison.Ordinal) >= 0,
                "an ampersand in the source is escaped for the attribute");
        }

        /// <summary>
        /// 验收场景:url 或 meta.size 缺失时保留 [名称]，不抛异常，也不静默
        /// </summary>
        private static void LeavesIncompleteEntriesAsText()
        {
            var incomplete = new[]
            {
                new[] { "missing url", "{\"meta\":{\"size\":1}}" },
                new[] { "blank url", "{\"url\":\"   \",\"meta\":{\"size\":1}}" },
                new[] { "null url", "{\"url\":null,\"meta\":{\"size\":1}}" },
                new[] { "missing meta", "{\"url\":\"https://i0.hdslb.com/a.png\"}" },
                new[] { "null meta", "{\"url\":\"https://i0.hdslb.com/a.png\",\"meta\":null}" },
                new[] { "meta without size", "{\"url\":\"https://i0.hdslb.com/a.png\",\"meta\":{}}" },
                new[] { "null size", "{\"url\":\"https://i0.hdslb.com/a.png\",\"meta\":{\"size\":null}}" },
                new[] { "non numeric size", "{\"url\":\"https://i0.hdslb.com/a.png\",\"meta\":{\"size\":\"abc\"}}" },
                new[] { "meta is not an object", "{\"url\":\"https://i0.hdslb.com/a.png\",\"meta\":7}" },
                new[] { "empty entry", "{}" },
            };

            foreach (var row in incomplete)
            {
                JObject entry = null;
                try
                {
                    entry = JObject.Parse(row[1]);
                }
                catch (Exception ex)
                {
                    Check(false, "the " + row[0] + " fixture must parse: " + ex.Message);
                }

                string markup = null;
                try
                {
                    markup = CommentRichText.BuildEmoteMarkup(entry);
                }
                catch (Exception ex)
                {
                    Check(false, "the " + row[0] + " entry must not throw, got " + ex.GetType().Name);
                }

                Check(markup == null, "the " + row[0] + " entry yields no markup");
            }

            var skipped = new System.Collections.Generic.List<string>();
            var emote = new JObject
            {
                ["[吃瓜]"] = JObject.Parse("{\"url\":\"https://i0.hdslb.com/a.png\"}"),
                ["[妙]"] = Emote("https://i0.hdslb.com/bfs/emote/ok.png", 1),
            };
            var output = CommentRichText.BuildMarkup("[吃瓜]和[妙]", emote, Id, skipped.Add);
            Check(output.IndexOf("[吃瓜]", StringComparison.Ordinal) >= 0,
                "the incomplete emote keeps its bracket text");
            Check(output.IndexOf("<Image", StringComparison.Ordinal) >= 0, "the complete emote is still drawn");
            Check(skipped.Count == 1 && skipped[0] == "[吃瓜]", "the skipped emote is reported, not swallowed");
        }

        /// <summary>
        /// 验收场景:表情最后拼。先拼就会被链接正则改写，图源地址变成按钮的参数
        /// </summary>
        private static void EmotesComeLast()
        {
            var emote = new JObject { ["[吃瓜]"] = Emote("https://i0.hdslb.com/bfs/emote/av12345678.png", 1) };
            var markup = CommentRichText.BuildMarkup("[吃瓜]", emote, Id);

            Check(markup.IndexOf("Source=\"https://i0.hdslb.com/bfs/emote/av12345678.png\"", StringComparison.Ordinal) >= 0,
                "the emote image keeps its own url");
            Check(markup.IndexOf("<HyperlinkButton", StringComparison.Ordinal) < 0,
                "no link swallows the generated image markup");
            Check(markup.IndexOf("[吃瓜]", StringComparison.Ordinal) < 0, "the bracket text is replaced");
            Check(Count(markup, "<Image") == 1, "exactly one image is emitted");

            var withLink = CommentRichText.BuildMarkup("[吃瓜] 看 https://b23.tv/abcdefg", emote, u => u);
            Check(Count(withLink, "<Image") == 1, "the user's link does not cost the emote its image");
            Check(Count(withLink, "<HyperlinkButton") == 1, "the user's link is still a link");
            Check(withLink.IndexOf("Source=\"https://i0.hdslb.com/bfs/emote/av12345678.png\"", StringComparison.Ordinal) >= 0,
                "the emote url survives a comment that also has a link");
        }

        /// <summary>
        /// 验收场景:链接和 av/BV/cv 的处理没有因为搬家少掉一半
        /// </summary>
        private static void LinksAndVideoIdsStillWork()
        {
            var link = CommentRichText.BuildMarkup("看 https://b23.tv/abcdefg", null, Utils_ProcessURL);
            Check(link.IndexOf("CommandParameter=\"https://b23.tv/abcdefg\"", StringComparison.Ordinal) >= 0,
                "a link keeps its full url in the command parameter");
            Check(link.IndexOf("🔗b23.tv/abcdefg", StringComparison.Ordinal) >= 0, "a link keeps its shortened display text");

            var av = CommentRichText.BuildMarkup("av123", null, Id);
            Check(av.IndexOf("CommandParameter=\"bilibili://video/av123\"", StringComparison.Ordinal) >= 0,
                "an av number links to the video");
            Check(av.IndexOf("<TextBlock>av123</TextBlock>", StringComparison.Ordinal) >= 0, "the av number is still visible");

            var bv = CommentRichText.BuildMarkup("BV1xx411c7mD", null, Id);
            Check(bv.IndexOf("CommandParameter=\"bilibili://video/BV1xx411c7mD\"", StringComparison.Ordinal) >= 0,
                "a BV number links to the video");

            var cv = CommentRichText.BuildMarkup("cv12345", null, Id);
            Check(cv.IndexOf("CommandParameter=\"bilibili://article/12345\"", StringComparison.Ordinal) >= 0,
                "a cv number links to the article");

            Check(CommentRichText.BuildMarkup(null, null, Id) == null, "null text stays null");
        }

        /// <summary>
        /// 验收场景:产品侧接线——只有一个规范化入口、表情在正则之后、兜底留痕、
        /// 透明图指向存在的资源、配图走压缩、表情包为空是显式状态
        /// </summary>
        private static void SourceGuardsKeepTheOldBugsOut()
        {
            var control = ReadSource(ControlHelper);
            Check(control.IndexOf("CommentRichText.BuildMarkup(", StringComparison.Ordinal) >= 0,
                "source: StringToRichText delegates the markup order");
            Check(control.IndexOf("HandelEmoji", StringComparison.Ordinal) < 0,
                "source: no second emote pass is left behind");
            Check(control.IndexOf("[\"url\"]", StringComparison.Ordinal) < 0,
                "source: the emote entry is never dereferenced by hand");
            Check(control.IndexOf("catch (Exception ex)", StringComparison.Ordinal) >= 0,
                "source: the fallback keeps the exception");
            Check(control.IndexOf("LogType.ERROR, ex", StringComparison.Ordinal) >= 0,
                "source: the fallback logs the exception");
            Check(control.IndexOf("return PlainText(txt)", StringComparison.Ordinal) >= 0,
                "source: plain text is still the last resort");
            Check(control.IndexOf("LogType.INFO));", StringComparison.Ordinal) >= 0,
                "source: a skipped emote entry is logged");

            var helpers = ReadSource(AppHelper);
            Check(helpers.IndexOf("TRANSPARENT_IMAGE = \"ms-appx:///Assets/Thumbnails/transparent.png\"", StringComparison.Ordinal) >= 0,
                "source: transparent image points at the asset directory");
            Check(helpers.IndexOf("MiniIcon/transparent.png", StringComparison.Ordinal) < 0,
                "source: the missing MiniIcon path is gone");
            Check(File.Exists(Path.Combine(RepoRoot(), TransparentAsset)), "the transparent asset exists in the repo");
            Check(NoSourceMentions("MiniIcon/transparent.png"), "no source file still references the missing asset");

            var xaml = ReadSource(CommentXaml);
            var pictures = Line(xaml, "ImgSrc");
            Check(pictures.IndexOf("imageConvert", StringComparison.Ordinal) >= 0,
                "source: comment pictures go through the compression converter");
            Check(pictures.IndexOf("ConverterParameter='200w'", StringComparison.Ordinal) >= 0,
                "source: comment pictures ask for a 200w resize");
            Check(pictures.IndexOf("ImageFailed=\"OnImageFail\"", StringComparison.Ordinal) >= 0,
                "source: the picture still has a failure handler");

            var code = Block(ReadSource(CommentCode), "private void OnImageFail", 900);
            Check(code.IndexOf("DefaultImage.png", StringComparison.Ordinal) >= 0,
                "source: a failed picture still falls back to the placeholder");
            Check(code.IndexOf("LogHelper.Log(", StringComparison.Ordinal) >= 0,
                "source: a failed picture is logged");
            Check(code.IndexOf("UriSource", StringComparison.Ordinal) >= 0,
                "source: the log carries the failing uri");

            var emoteVm = ReadSource(EmoteVM);
            Check(emoteVm.IndexOf("data.data[\"packages\"].ToString()", StringComparison.Ordinal) < 0,
                "source: packages is parsed through a null check");
            Check(emoteVm.IndexOf("Packages = new List<EmotePackageModel>()", StringComparison.Ordinal) >= 0,
                "source: an empty package list is an explicit state");
            Check(emoteVm.IndexOf("LogHelper.Log(", StringComparison.Ordinal) >= 0,
                "source: the empty emote panel is logged");
            Check(emoteVm.IndexOf("CommentRichText.NormalizeImageUrl(emote.url)", StringComparison.Ordinal) >= 0,
                "source: picker emote urls use the same normaliser");
        }

        private static JObject Emote(string url, int size)
        {
            return new JObject { ["url"] = url, ["meta"] = new JObject { ["size"] = size } };
        }

        private static string Id(string url)
        {
            return url;
        }

        private static string Utils_ProcessURL(string url)
        {
            var u = url.StartsWith("https://", StringComparison.Ordinal) ? url.Substring(8)
                : url.StartsWith("http://", StringComparison.Ordinal) ? url.Substring(7) : url;
            return u.Length > 40 ? u.Substring(0, 37) + "..." : u;
        }

        private static int Count(string text, string token)
        {
            var count = 0;
            var index = 0;
            while ((index = text.IndexOf(token, index, StringComparison.Ordinal)) >= 0)
            {
                count++;
                index += token.Length;
            }
            return count;
        }

        private static bool NoSourceMentions(string token)
        {
            var root = Path.Combine(RepoRoot(), "src", "BiliLite.UWP");
            foreach (var file in Directory.GetFiles(root, "*.*", SearchOption.AllDirectories))
            {
                var name = file.Replace('\\', '/');
                if (name.IndexOf("/obj/", StringComparison.Ordinal) >= 0) continue;
                if (name.IndexOf("/bin/", StringComparison.Ordinal) >= 0) continue;
                if (!name.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)
                    && !name.EndsWith(".xaml", StringComparison.OrdinalIgnoreCase)) continue;
                if (File.ReadAllText(file).IndexOf(token, StringComparison.Ordinal) >= 0) return false;
            }
            return true;
        }

        private static string Line(string source, string marker)
        {
            var start = source.IndexOf(marker, StringComparison.Ordinal);
            if (start < 0) return "";
            var begin = source.LastIndexOf('\n', start) + 1;
            var end = source.IndexOf('\n', start);
            return end < 0 ? source.Substring(begin) : source.Substring(begin, end - begin);
        }

        private static string Block(string source, string marker, int length)
        {
            var start = source.IndexOf(marker, StringComparison.Ordinal);
            if (start < 0) return "";
            return source.Substring(start, Math.Min(length, source.Length - start));
        }

        private static string ReadSource(string relativePath)
        {
            var text = File.ReadAllText(Path.Combine(RepoRoot(), relativePath.Replace('\\', Path.DirectorySeparatorChar)));
            return text.Replace("\r\n", "\n");
        }

        private static string RepoRoot()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null)
            {
                if (File.Exists(Path.Combine(dir.FullName, "src", "BiliLite.UWP", "Helpers", "CommentRichText.cs")))
                {
                    return dir.FullName;
                }
                dir = dir.Parent;
            }
            throw new InvalidOperationException("repository root not found from " + AppContext.BaseDirectory);
        }

        private static string Quote(string value)
        {
            return value == null ? "<null>" : "\"" + value + "\"";
        }

        private static void Check(bool condition, string message)
        {
            if (!condition)
            {
                Console.Error.WriteLine("FAIL " + message);
                Environment.Exit(1);
            }
            _passed++;
            Console.WriteLine("ok " + message);
        }
    }
}
