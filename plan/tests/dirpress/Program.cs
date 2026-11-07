using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace DirPressProbe
{
    /// <summary>
    /// BR-44 探针:直接读真实的 Controls/PlayerControl.xaml 和 PlayerControl.xaml.cs,
    /// 把每一条 Storyboard.SetTarget / SetTargetProperty 配成对,校验属性名确实长在目标对象的类型上。
    /// 线上那次崩溃就是 Opacity 被挂到 CompositeTransform 上 —— Transform 没有这个属性,Begin() 当场抛。
    /// 顺带钉住"目录"按钮按下反馈的形状:一次闪光、两个目标元素、时长只认 ButtonPressMilliseconds、故事板只建一次。
    /// </summary>
    internal static class Program
    {
        private static int _checks;
        private static readonly List<string> Failures = new List<string>();

        private static readonly HashSet<string> TransformTypes = new HashSet<string>(StringComparer.Ordinal)
        {
            "CompositeTransform", "TranslateTransform", "ScaleTransform", "RotateTransform",
            "SkewTransform", "MatrixTransform", "TransformGroup",
            "TranslateTransform3D", "ScaleTransform3D", "RotateTransform3D",
        };

        // Transform 上能动的属性就这么几样;表里没有就说明属性放错了对象
        private static readonly HashSet<string> TransformProperties = new HashSet<string>(StringComparer.Ordinal)
        {
            "ScaleX", "ScaleY", "TranslateX", "TranslateY", "Rotation", "SkewX", "SkewY",
            "CenterX", "CenterY", "M11", "M12", "M21", "M22", "OffsetX", "OffsetY",
        };

        // 非 Transform 元素(UIElement/FrameworkElement)上能动的属性。将来真要动别的属性,
        // 探针会红一次,把表补上就行 —— 红过一次总比静悄悄退回崩溃好
        private static readonly HashSet<string> ElementProperties = new HashSet<string>(StringComparer.Ordinal)
        {
            "Opacity", "Width", "Height", "MinWidth", "MinHeight", "MaxWidth", "MaxHeight",
        };

        private const string ButtonName = "BottomBtnDirectory";
        private const string TransformName = "BottomBtnDirectoryPress";
        private const string HostName = "BottomBtnDirectoryPressHost";
        private const string PressMethod = "private void PlayDirectoryButtonPress(";

        private static int Main(string[] args)
        {
            string xamlPath = null;
            string csPath = null;
            for (var i = 0; i < args.Length; i++)
            {
                if (args[i] == "--xaml" && i + 1 < args.Length) xamlPath = args[++i];
                else if (args[i] == "--cs" && i + 1 < args.Length) csPath = args[++i];
                else
                {
                    Console.Error.WriteLine("未知参数:" + args[i] + "(可用:--xaml <path> --cs <path>)");
                    return 2;
                }
            }

            var root = FindRepoRoot();
            var xamlFile = xamlPath ?? Path.Combine(root, "src", "BiliLite.UWP", "Controls", "PlayerControl.xaml");
            var csFile = csPath ?? Path.Combine(root, "src", "BiliLite.UWP", "Controls", "PlayerControl.xaml.cs");
            Console.WriteLine("xaml = " + xamlFile);
            Console.WriteLine("cs   = " + csFile);
            if (!File.Exists(xamlFile) || !File.Exists(csFile))
            {
                Console.Error.WriteLine("源文件不存在");
                return 2;
            }

            var named = LoadNamedElements(xamlFile);
            CheckNamedElements(named);
            var code = File.ReadAllText(csFile);
            var map = ScanBraces(code);
            var pairs = BuildPairs(code, map);
            CheckTargetPairs(code, named, pairs);
            CheckPressMethod(code, map, named, pairs);
            CheckDrawerStoryboard(code, map, named);

            Console.WriteLine();
            if (Failures.Count == 0)
            {
                Console.WriteLine("dirpress PASS —— " + _checks + " 项检查全过");
                return 0;
            }
            Console.WriteLine("dirpress FAIL —— " + Failures.Count + " / " + _checks + " 项没过:");
            foreach (var failure in Failures) Console.WriteLine("  - " + failure);
            return 1;
        }

        // ---------- XAML ----------

        private static List<XElement> LoadNamedElements(string xamlPath)
        {
            XNamespace xns = "http://schemas.microsoft.com/winfx/2006/xaml";
            var doc = XDocument.Load(xamlPath, LoadOptions.SetLineInfo);
            var named = new List<XElement>();
            foreach (var element in doc.Descendants())
            {
                var name = element.Attribute(xns + "Name");
                if (name != null) named.Add(element);
            }
            return named;
        }

        private static string NameOf(XElement element)
        {
            XNamespace xns = "http://schemas.microsoft.com/winfx/2006/xaml";
            var name = element.Attribute(xns + "Name");
            return name == null ? null : name.Value;
        }

        private static XElement FindNamed(List<XElement> named, string name)
        {
            var hits = named.Where(e => NameOf(e) == name).ToList();
            if (hits.Count > 1) Fail("xaml 里 " + name + " 出现了 " + hits.Count + " 次");
            return hits.FirstOrDefault();
        }

        private static void CheckNamedElements(List<XElement> named)
        {
            var button = FindNamed(named, ButtonName);
            var transform = FindNamed(named, TransformName);
            var host = FindNamed(named, HostName);

            Check(button != null, "xaml: 按钮 " + ButtonName + " 存在");
            Check(transform != null, "xaml: 变换 " + TransformName + " 存在");
            Check(host != null, "xaml: 按下反馈的宿主元素 " + HostName + " 存在(Opacity 要挂在这个真 UIElement 上)");
            if (button == null || transform == null || host == null) return;

            var transformType = transform.Name.LocalName;
            var hostType = host.Name.LocalName;
            Check(transformType == "CompositeTransform",
                "xaml: " + TransformName + " 是 CompositeTransform(实际 " + transformType + ")");
            Check(!TransformTypes.Contains(hostType),
                "xaml: " + HostName + " 不是 Transform 而是可挂 Opacity 的元素(实际 " + hostType + ")");
            Check(host.Descendants().Any(e => ReferenceEquals(e, transform)),
                "xaml: " + TransformName + " 就在 " + HostName + " 的 RenderTransform 下");
            Check(button.Descendants().Any(e => ReferenceEquals(e, host)),
                "xaml: " + HostName + " 在按钮 " + ButtonName + " 里面");
        }

        private static string TypeOfNamed(List<XElement> named, string name)
        {
            var element = FindNamed(named, name);
            return element == null ? null : element.Name.LocalName;
        }

        // ---------- C# ----------

        /// <summary>
        /// 每个字符归属的最内层花括号块,顺带记下配对关系。
        /// 字符串/注释里的花括号要跳过,不然块编号会被注释里的文本带跑
        /// </summary>
        private static CodeMap ScanBraces(string text)
        {
            var ids = new int[text.Length];
            for (var k = 0; k < ids.Length; k++) ids[k] = -1;
            var open = new Stack<int>();
            var match = new Dictionary<int, int>();
            var current = -1;
            var i = 0;
            while (i < text.Length)
            {
                var c = text[i];
                if (c == '/' && i + 1 < text.Length && text[i + 1] == '/')
                {
                    while (i < text.Length && text[i] != '\n') { ids[i] = current; i++; }
                    continue;
                }
                if (c == '/' && i + 1 < text.Length && text[i + 1] == '*')
                {
                    ids[i] = current; ids[i + 1] = current; i += 2;
                    while (i < text.Length)
                    {
                        if (text[i] == '*' && i + 1 < text.Length && text[i + 1] == '/')
                        {
                            ids[i] = current; ids[i + 1] = current; i += 2;
                            break;
                        }
                        ids[i] = current; i++;
                    }
                    continue;
                }
                if (c == '@' && i + 1 < text.Length && text[i + 1] == '"')
                {
                    ids[i] = current; ids[i + 1] = current; i += 2;
                    while (i < text.Length)
                    {
                        if (text[i] == '"')
                        {
                            if (i + 1 < text.Length && text[i + 1] == '"')
                            {
                                ids[i] = current; ids[i + 1] = current; i += 2;
                                continue;
                            }
                            ids[i] = current; i++;
                            break;
                        }
                        ids[i] = current; i++;
                    }
                    continue;
                }
                if (c == '"' || c == '\'')
                {
                    var quote = c;
                    ids[i] = current; i++;
                    while (i < text.Length)
                    {
                        if (text[i] == '\\')
                        {
                            ids[i] = current; i++;
                            if (i < text.Length) { ids[i] = current; i++; }
                            continue;
                        }
                        if (text[i] == quote) { ids[i] = current; i++; break; }
                        ids[i] = current; i++;
                    }
                    continue;
                }
                if (c == '{')
                {
                    ids[i] = current;
                    open.Push(i);
                    current = i;
                    i++;
                    continue;
                }
                if (c == '}')
                {
                    if (open.Count > 0)
                    {
                        var opened = open.Pop();
                        match[opened] = i;
                        current = open.Count > 0 ? open.Peek() : -1;
                    }
                    ids[i] = current;
                    i++;
                    continue;
                }
                ids[i] = current;
                i++;
            }
            return new CodeMap { BlockIds = ids, BraceMatch = match };
        }

        private sealed class CodeMap
        {
            public int[] BlockIds;
            public Dictionary<int, int> BraceMatch;
        }

        private sealed class Pair
        {
            public string Anim;
            public string Target;
            public string Property;
            public int TargetIndex = -1;
            public int PropertyIndex = -1;
        }

        private static readonly Regex SetTargetRegex =
            new Regex(
                @"Storyboard\s*\.\s*SetTarget\s*\(\s*(?<anim>[A-Za-z_]\w*)\s*,\s*(?<target>[A-Za-z_][\w\.]*)\s*\)");

        private static readonly Regex SetTargetPropertyRegex =
            new Regex(
                @"Storyboard\s*\.\s*SetTargetProperty\s*\(\s*(?<anim>[A-Za-z_]\w*)\s*,\s*""(?<prop>[^""]*)""\s*\)");

        private static readonly Regex SetTargetPropertyAnyRegex =
            new Regex(@"Storyboard\s*\.\s*SetTargetProperty\s*\(");

        /// <summary>
        /// 按"最内层块 + 动画变量名"把 SetTarget 和 SetTargetProperty 配成对。
        /// 配对只认同一个花括号块:同一文件里两个方法各有一个 fade 也不会串味
        /// </summary>
        private static List<Pair> BuildPairs(string code, CodeMap map)
        {
            var pairs = new Dictionary<string, Pair>(StringComparer.Ordinal);
            foreach (Match m in SetTargetRegex.Matches(code))
            {
                var key = map.BlockIds[m.Index] + "|" + m.Groups["anim"].Value;
                if (pairs.ContainsKey(key)) Fail("同一个块里对 " + m.Groups["anim"].Value + " 设了两次 SetTarget");
                else pairs[key] = new Pair
                {
                    Anim = m.Groups["anim"].Value,
                    Target = m.Groups["target"].Value,
                    TargetIndex = m.Index,
                };
            }
            foreach (Match m in SetTargetPropertyRegex.Matches(code))
            {
                var key = map.BlockIds[m.Index] + "|" + m.Groups["anim"].Value;
                if (!pairs.ContainsKey(key)) { Fail("动画 " + m.Groups["anim"].Value + " 设了属性却没设目标"); continue; }
                pairs[key].Property = m.Groups["prop"].Value;
                pairs[key].PropertyIndex = m.Index;
            }
            var skipped = SetTargetPropertyAnyRegex.Matches(code).Count - SetTargetPropertyRegex.Matches(code).Count;
            Check(skipped == 0,
                "SetTargetProperty 的属性名全是字面量(有 " + skipped + " 处不是,探针没法静态校验)");
            return pairs.Values.OrderBy(p => p.TargetIndex).ToList();
        }

        private static void CheckTargetPairs(string code, List<XElement> named, List<Pair> pairs)
        {
            Check(pairs.Count > 0, "源码里能找到 Storyboard.SetTarget 的地方(" + pairs.Count + " 处)");
            foreach (var pair in pairs)
            {
                if (pair.Property == null)
                {
                    Fail("动画 " + pair.Anim + " 没设 SetTargetProperty(" + LineOf(code, pair.TargetIndex) + ")");
                    continue;
                }
                var targetName = pair.Target.StartsWith("this.", StringComparison.Ordinal)
                    ? pair.Target.Substring(5)
                    : pair.Target;
                var targetType = TypeOfNamed(named, targetName);
                var where = LineOf(code, pair.PropertyIndex) + " 附近";
                if (targetType == null)
                {
                    Fail(where + ": SetTarget 的目标 " + targetName + " 不是 xaml 里的 x:Name,探针判不出它的类型");
                    continue;
                }
                var isTransform = TransformTypes.Contains(targetType);
                var allowed = isTransform ? TransformProperties : ElementProperties;
                if (pair.Property == "Opacity" && isTransform)
                {
                    Fail(where + ": Opacity 挂在变换 " + targetName + "(" + targetType + ") 上 —— Transform 没有 Opacity,"
                        + "Storyboard.Begin() 会抛,点一次就崩一次");
                    continue;
                }
                if (!allowed.Contains(pair.Property))
                {
                    Fail(where + ": " + pair.Property + " 不是 " + targetName + "(" + targetType + ") 上的可动属性");
                    continue;
                }
                Check(true, where + ": " + pair.Target + "(" + targetType + ")." + pair.Property + " 存在");
            }
        }

        private static void CheckPressMethod(string code, CodeMap map, List<XElement> named, List<Pair> pairs)
        {
            var decl = code.IndexOf(PressMethod, StringComparison.Ordinal);
            Check(decl >= 0, "源码里有 PlayDirectoryButtonPress");
            if (decl < 0) return;
            var open = code.IndexOf('{', decl);
            int close;
            if (open < 0 || !map.BraceMatch.TryGetValue(open, out close))
            {
                Fail("找不到 PlayDirectoryButtonPress 的方法体");
                return;
            }
            var body = code.Substring(open, close - open + 1);

            Check(body.Contains("BottomBtnDirectoryPress == null") && body.Contains(HostName + " == null"),
                "PlayDirectoryButtonPress 对两个目标都做了 null 判断");
            Check(body.Contains("_directoryPressStoryboard == null"),
                "PlayDirectoryButtonPress 只在第一次建故事板");
            Check(CountOf(body, "new Storyboard") == 1,
                "PlayDirectoryButtonPress 只 new 一个 Storyboard(实际 " + CountOf(body, "new Storyboard") + " 个)");
            Check(body.Contains(".Begin()"), "PlayDirectoryButtonPress 会 Begin 故事板");
            Check(body.Contains(".Stop()"), "PlayDirectoryButtonPress 重放前会先停掉自己那一份");

            var fromMs = new Regex(@"TimeSpan\s*\.\s*FromMilliseconds\s*\(\s*([^)]*)\)");
            var millisMatches = fromMs.Matches(body);
            Check(millisMatches.Count > 0, "按下反馈的时长来自 TimeSpan.FromMilliseconds");
            foreach (Match m in millisMatches)
            {
                Check(m.Groups[1].Value.Contains("DirectoryDrawerMotion.ButtonPressMilliseconds"),
                    "时长只用 DirectoryDrawerMotion.ButtonPressMilliseconds,没有写死数字(现为 "
                    + m.Groups[1].Value.Trim() + ")");
            }

            var pressPairs = pairs
                .Where(p => p.TargetIndex > open && p.TargetIndex < close && p.PropertyIndex > open && p.PropertyIndex < close)
                .ToList();
            var transformPairs = pressPairs.Where(p => p.Target == TransformName).ToList();
            Check(transformPairs.Any(p => p.Property == "ScaleX"), "按下反馈缩放 " + TransformName + ".ScaleX");
            Check(transformPairs.Any(p => p.Property == "ScaleY"), "按下反馈缩放 " + TransformName + ".ScaleY");
            var fadePairs = pressPairs.Where(p => p.Property == "Opacity").ToList();
            Check(fadePairs.Count == 1, "按下反馈里 Opacity 只有一处(实际 " + fadePairs.Count + " 处)");
            Check(fadePairs.Count == 1 && fadePairs[0].Target == HostName,
                "按下反馈的 Opacity 挂在 " + HostName + " 上"
                + (fadePairs.Count == 1 ? "(实际 " + fadePairs[0].Target + ")" : ""));
            Check(CountOf(body, "0.88") == 2 && CountOf(body, "0.55") == 1,
                "闪光幅度还是 0.88 / 0.55(一次点击一次闪回)");
            Check(TypeOfNamed(named, HostName) != null && !TransformTypes.Contains(TypeOfNamed(named, HostName)),
                "按下反馈的 Opacity 目标不是 Transform");
        }

        private static void CheckDrawerStoryboard(string code, CodeMap map, List<XElement> named)
        {
            // 抽屉本体那条故事板也在同一个文件里,顺手一起验,免得以后再犯同样的错
            var decl = code.IndexOf("private void StartDirectoryStoryboard(", StringComparison.Ordinal);
            if (decl < 0) return;
            var open = code.IndexOf('{', decl);
            int close;
            if (open < 0 || !map.BraceMatch.TryGetValue(open, out close)) return;
            var body = code.Substring(open, close - open + 1);
            Check(body.Contains("DirectoryDrawerMotion."), "抽屉开关的时长也来自 DirectoryDrawerMotion");
        }

        // ---------- helpers ----------

        private static int CountOf(string haystack, string needle)
        {
            var count = 0;
            var index = 0;
            while ((index = haystack.IndexOf(needle, index, StringComparison.Ordinal)) >= 0)
            {
                count++;
                index += needle.Length;
            }
            return count;
        }

        private static string LineOf(string text, int index)
        {
            var line = 1;
            for (var i = 0; i < index && i < text.Length; i++)
            {
                if (text[i] == '\n') line++;
            }
            return "第 " + line + " 行";
        }

        private static void Check(bool ok, string message)
        {
            _checks++;
            if (ok) Console.WriteLine("[ok] " + message);
            else Fail(message);
        }

        private static void Fail(string message)
        {
            Failures.Add(message);
            Console.WriteLine("[FAIL] " + message);
        }

        private static string FindRepoRoot()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null)
            {
                if (File.Exists(Path.Combine(dir.FullName, "src", "BiliLite.UWP", "Controls", "PlayerControl.xaml")))
                    return dir.FullName;
                dir = dir.Parent;
            }
            throw new InvalidOperationException("找不到仓库根目录(向上找 src/BiliLite.UWP/Controls/PlayerControl.xaml)");
        }
    }
}
