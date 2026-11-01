using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace ToastAlignProbe
{
    /// <summary>
    /// BR-43 探针:直接读真实的 Controls/PlayerToast.xaml 和 PlayerToast.xaml.cs,钉住浮层里
    /// "图标与文字的墨迹中心对齐"这件事的实现形状。两轮真机都栽在同一类假设上:
    ///   第一轮:两种字体行框落点不同,内容整体偏移;
    ///   第二轮:把两块的行框钉成同一高度(BlockLineHeight + LineHeight),盒子等高但墨迹不等高,文字更偏下。
    /// 所以这里不允许任何 LineStackingStrategy/LineHeight,两块必须 TextLineBounds="Tight",
    /// 垂直方向只留一个命名的光学常量,并且胶囊外框靠 ToastRoot.MinHeight 维持旧尺寸。
    /// </summary>
    internal static class Program
    {
        private static int _checks;
        private static readonly List<string> Failures = new List<string>();

        private const string IconName = "IconGlyph";
        private const string TextName = "TxtToast";
        private const string RootName = "ToastRoot";
        private const string OffsetName = "IconOffset";
        private const string OpticalConstant = "BaseIconOpticalOffset";
        private const string ContentConstant = "BaseContentHeight";

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
            var xamlFile = xamlPath ?? Path.Combine(root, "src", "BiliLite.UWP", "Controls", "PlayerToast.xaml");
            var csFile = csPath ?? Path.Combine(root, "src", "BiliLite.UWP", "Controls", "PlayerToast.xaml.cs");
            Console.WriteLine("xaml = " + xamlFile);
            Console.WriteLine("cs   = " + csFile);
            if (!File.Exists(xamlFile) || !File.Exists(csFile))
            {
                Console.Error.WriteLine("源文件不存在");
                return 2;
            }

            var doc = XDocument.Load(xamlFile, LoadOptions.SetLineInfo);
            var code = File.ReadAllText(csFile);

            CheckXamlLineBoxes(doc);
            CheckXamlLayout(doc);
            CheckCodeHasNoLineHeight(code);
            CheckOpticalConstant(code);
            CheckIconVerticalMove(code);
            CheckScaleConsistency(code);
            CheckPillMinimum(code);
            CheckCollapseBehaviour(code);

            Console.WriteLine();
            if (Failures.Count == 0)
            {
                Console.WriteLine("toastalign PASS —— " + _checks + " 项检查全过");
                return 0;
            }
            Console.WriteLine("toastalign FAIL —— " + Failures.Count + " / " + _checks + " 项没过:");
            foreach (var failure in Failures) Console.WriteLine("  - " + failure);
            return 1;
        }

        // ---------- XAML ----------

        private static void CheckXamlLineBoxes(XDocument doc)
        {
            // 契约:行框规则整体交给引擎,两块都收紧到墨迹。任何一处钉死行框都会让墨迹偏离盒心
            var stacking = doc.Descendants()
                .Where(e => e.Attribute("LineStackingStrategy") != null)
                .ToList();
            Check(stacking.Count == 0,
                "XAML 里没有任何 LineStackingStrategy(实际 " + stacking.Count + " 处"
                + (stacking.Count == 0 ? "" : ": " + LineList(stacking)) + ")");

            var lineHeight = doc.Descendants()
                .Where(e => e.Attribute("LineHeight") != null)
                .ToList();
            Check(lineHeight.Count == 0,
                "XAML 里没有任何 LineHeight(实际 " + lineHeight.Count + " 处"
                + (lineHeight.Count == 0 ? "" : ": " + LineList(lineHeight)) + ")");

            var icon = FindNamed(doc, IconName);
            var text = FindNamed(doc, TextName);
            Check(icon != null, "xaml 里有 " + IconName);
            Check(text != null, "xaml 里有 " + TextName);
            Check(Attribute(icon, "TextLineBounds") == "Tight", IconName + " 是 TextLineBounds=\"Tight\"(实际 \"" + Attribute(icon, "TextLineBounds") + "\")");
            Check(Attribute(text, "TextLineBounds") == "Tight", TextName + " 是 TextLineBounds=\"Tight\"(实际 \"" + Attribute(text, "TextLineBounds") + "\")");
            // 同样的墨迹对齐还要求两块字号和垂直对齐方式一致,否则盒子中心本身就不在一条线上
            Check(Attribute(icon, "FontSize") == Attribute(text, "FontSize"),
                "两块字号一致(" + IconName + "=" + Attribute(icon, "FontSize") + ", " + TextName + "=" + Attribute(text, "FontSize") + ")");
            Check(Attribute(icon, "VerticalAlignment") == "Center" && Attribute(text, "VerticalAlignment") == "Center",
                "两块都 VerticalAlignment=\"Center\"");
        }

        private static void CheckXamlLayout(XDocument doc)
        {
            var root = FindNamed(doc, RootName);
            Check(root != null, "xaml 里有 " + RootName);
            // 胶囊的圆角/内边距写在 ApplyScale 里,标记只留一个初值;这里确认初值上下对称
            var padding = Attribute(root, "Padding");
            var parts = SplitThickness(padding);
            Check(parts != null && parts[0] == parts[2] && parts[1] == parts[3],
                RootName + ".Padding 初值上下对称(实际 \"" + padding + "\")");

            // 垂直方向唯一允许的位移载体就是载着 IconOffset 的那个 TranslateTransform
            var transforms = doc.Descendants().Where(e => e.Name.LocalName == "TranslateTransform").ToList();
            Check(transforms.Count == 1 && NameOf(transforms[0]) == OffsetName,
                "XAML 里只有一个 TranslateTransform,且名为 " + OffsetName + "(实际 " + transforms.Count + " 个)");
            var origins = doc.Descendants().Where(e => e.Attribute("RenderTransformOrigin") != null).ToList();
            Check(origins.Count == 0, "XAML 里没有 RenderTransformOrigin 参与对齐(实际 " + origins.Count + " 处)");
        }

        // ---------- 代码 ----------

        private static void CheckCodeHasNoLineHeight(string code)
        {
            // 上一轮的修复就是把 LineHeight 钉死:两块盒子等高、墨迹不等高,文字被行框内部的留白推下去
            var hits = Regex.Matches(code, @"\bLineHeight\b")
                .Cast<Match>()
                .Select(m => LineOf(code, m.Index))
                .ToList();
            Check(hits.Count == 0,
                "代码里没有任何 LineHeight(实际 " + hits.Count + " 处"
                + (hits.Count == 0 ? "" : ": " + string.Join(", ", hits)) + ")");
            Check(!Regex.IsMatch(code, @"LineStackingStrategy"),
                "代码里没有 LineStackingStrategy");
        }

        private static void CheckOpticalConstant(string code)
        {
            var constants = Regex.Matches(code, @"private\s+const\s+double\s+(\w+)\s*=\s*([^;]+);")
                .Cast<Match>()
                .Select(m => new { Name = m.Groups[1].Value, Value = Normalize(m.Groups[2].Value) })
                .ToList();
            var optical = constants
                .Where(c => c.Name.IndexOf("Offset", StringComparison.Ordinal) >= 0
                            || c.Name.IndexOf("Optical", StringComparison.Ordinal) >= 0)
                .ToList();
            Check(optical.Count == 1,
                "只有一个光学/位移常量(实际 " + optical.Count + " 个"
                + (optical.Count == 0 ? "" : ": " + string.Join(", ", optical.Select(c => c.Name))) + ")");
            Check(optical.Count == 1 && optical[0].Name == OpticalConstant,
                "那个常量叫 " + OpticalConstant);
            // 视觉残差归零:紧墨迹对齐之后,除非真机上还看得出 FA 的不对称,不该再预先补偿
            Check(optical.Count == 1 && optical[0].Value == "0",
                OpticalConstant + " 为 0(实际 \"" + (optical.Count == 1 ? optical[0].Value : "?") + "\")");
        }

        private static void CheckIconVerticalMove(string code)
        {
            var writes = Regex.Matches(code, @"IconOffset\.Y\s*=\s*([^;]+);")
                .Cast<Match>()
                .Select(m => Normalize(m.Groups[1].Value))
                .ToList();
            Check(writes.Count == 1, "IconOffset.Y 只被赋值一次(实际 " + writes.Count + " 次)");
            Check(writes.Count == 1 && writes[0] == "hasText?BaseIconOpticalOffset*_scale:0",
                "IconOffset.Y 的唯一来源是 " + OpticalConstant + "×scale,且无文本时为 0(实际 \""
                + (writes.Count == 1 ? writes[0] : "?") + "\")");

            // 不许再有第二条垂直通路:TranslateY / 图标上下 Margin / 上下不对称的内边距
            Check(!Regex.IsMatch(code, @"\bTranslateY\b"), "代码里没有 TranslateY");
            var margins = Regex.Matches(code, @"IconGlyph\.Margin\s*=\s*new\s+Thickness\(([^;]+)\);")
                .Cast<Match>()
                .Select(m => Normalize(m.Groups[1].Value))
                .ToList();
            Check(margins.Count == 1, "IconGlyph.Margin 只被赋值一次(实际 " + margins.Count + " 次)");
            Check(margins.Count == 1 && margins[0] == "0,0,hasText?BaseIconSpacing*_scale:0,0",
                "IconGlyph.Margin 的上下边距都是 0,只留右侧间距且随文本有无开关(实际 \""
                + (margins.Count == 1 ? margins[0] : "?") + "\")");
        }

        private static void CheckScaleConsistency(string code)
        {
            // 内边距必须上下相等,并且和内边距里的字号用同一个 scale —— 三套缩放系数是上一轮偏移的来源之一
            var paddings = Regex.Matches(code, @"ToastRoot\.Padding\s*=\s*new\s+Thickness\(([^;]+)\);")
                .Cast<Match>()
                .Select(m => Normalize(m.Groups[1].Value))
                .ToList();
            Check(paddings.Count == 1, "ToastRoot.Padding 只被赋值一次(实际 " + paddings.Count + " 次)");
            var args = paddings.Count == 1 ? paddings[0].Split(',') : new string[0];
            Check(args.Length == 4, "ToastRoot.Padding 是四边写法(实际 " + args.Length + " 个参数)");
            Check(args.Length == 4 && args[0] == args[2],
                "ToastRoot.Padding 左右相等(" + (args.Length == 4 ? args[0] + " vs " + args[2] : "?") + ")");
            Check(args.Length == 4 && args[1] == args[3],
                "ToastRoot.Padding 上下相等 —— 不靠不对称内边距顶位置(" + (args.Length == 4 ? args[1] + " vs " + args[3] : "?") + ")");
            Check(args.Length == 4 && args[0].EndsWith("*scale", StringComparison.Ordinal) && args[0] != args[1],
                "ToastRoot.Padding 随 scale 缩放(横向 " + (args.Length == 4 ? args[0] : "?") + ")");

            var fontSizes = new[] { TextName, IconName }
                .Select(name => Regex.Matches(code, Regex.Escape(name) + @"\.FontSize\s*=\s*([^;]+);")
                    .Cast<Match>()
                    .Select(m => Normalize(m.Groups[1].Value))
                    .ToList())
                .ToList();
            Check(fontSizes.All(l => l.Count == 1 && l[0] == "BaseFontSize*scale"),
                "两块字号都由 BaseFontSize×scale 算出(" + TextName + "=" + string.Join("/", fontSizes[0]) + ", "
                + IconName + "=" + string.Join("/", fontSizes[1]) + ")");
        }

        private static void CheckPillMinimum(string code)
        {
            // 墨迹收紧后块高会缩,胶囊要靠最小高度保持旧的外框尺寸:内容 22 + 上下内边距,再乘同一个 scale
            var declarations = Regex.Matches(code, @"private\s+const\s+double\s+" + ContentConstant + @"\s*=\s*([^;]+);")
                .Cast<Match>()
                .Select(m => Normalize(m.Groups[1].Value))
                .ToList();
            Check(declarations.Count == 1, ContentConstant + " 声明一次(实际 " + declarations.Count + " 次)");
            Check(declarations.Count == 1 && declarations[0] == "BaseFontSize*1.1",
                ContentConstant + " = BaseFontSize×1.1,也就是旧的内容高度 22(实际 \""
                + (declarations.Count == 1 ? declarations[0] : "?") + "\")");
            var uses = Regex.Matches(code, @"\b" + ContentConstant + @"\b").Count;
            Check(uses == 2, ContentConstant + " 只用于最小高度这一处(声明 + 1 次使用,实际 " + uses + " 次)");

            var mins = Regex.Matches(code, @"ToastRoot\.MinHeight\s*=\s*([^;]+);")
                .Cast<Match>()
                .Select(m => Normalize(m.Groups[1].Value))
                .ToList();
            Check(mins.Count == 1, "ToastRoot.MinHeight 被设置(实际 " + mins.Count + " 次)");
            Check(mins.Count == 1 && mins[0] == "(BaseContentHeight+BasePaddingY*2)*scale",
                "ToastRoot.MinHeight = (内容高度 + 上下内边距)×scale(实际 \""
                + (mins.Count == 1 ? mins[0] : "?") + "\")");
        }

        private static void CheckCollapseBehaviour(string code)
        {
            // 对外行为不变:没有文本时折叠文本块、收掉图标右侧间距;图标为空时也折叠
            Check(Regex.IsMatch(code, @"TxtToast\.Visibility\s*=\s*string\.IsNullOrEmpty\(text\)\s*\?\s*Visibility\.Collapsed\s*:\s*Visibility\.Visible;"),
                "没有文本时 TxtToast 折叠(不留空位)");
            Check(Regex.IsMatch(code, @"var\s+hasText\s*=\s*TxtToast\.Visibility\s*==\s*Visibility\.Visible;"),
                "hasText 仍然按 TxtToast 的可见性判断");
            Check(Regex.IsMatch(code, @"IconGlyph\.Visibility\s*=\s*Visibility\.Collapsed;"),
                "没有图标时 IconGlyph 折叠");
        }

        // ---------- helpers ----------

        private static XElement FindNamed(XDocument doc, string name)
        {
            XNamespace xns = "http://schemas.microsoft.com/winfx/2006/xaml";
            return doc.Descendants().FirstOrDefault(e => NameOf(e) == name);
        }

        private static string NameOf(XElement element)
        {
            XNamespace xns = "http://schemas.microsoft.com/winfx/2006/xaml";
            var name = element == null ? null : element.Attribute(xns + "Name");
            return name == null ? null : name.Value;
        }

        private static string Attribute(XElement element, string name)
        {
            var attribute = element == null ? null : element.Attribute(name);
            return attribute == null ? "" : attribute.Value.Trim();
        }

        private static string LineList(List<XElement> elements)
        {
            XNamespace xns = "http://schemas.microsoft.com/winfx/2006/xaml";
            return string.Join(", ", elements.Select(e =>
            {
                var info = (System.Xml.IXmlLineInfo)e;
                var name = e.Attribute(xns + "Name");
                return (name == null ? e.Name.LocalName : name.Value) + "@第 " + info.LineNumber + " 行";
            }));
        }

        private static string[] SplitThickness(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            var parts = value.Split(',').Select(p => p.Trim()).ToArray();
            return parts.Length == 2 ? new[] { parts[0], parts[1], parts[0], parts[1] }
                : parts.Length == 4 ? parts
                : null;
        }

        private static string Normalize(string expression)
        {
            return Regex.Replace(expression ?? "", @"\s+", "");
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
            Console.WriteLine("[fail] " + message);
        }

        private static string FindRepoRoot()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null)
            {
                if (File.Exists(Path.Combine(dir.FullName, "src", "BiliLite.UWP", "Controls", "PlayerToast.xaml")))
                    return dir.FullName;
                dir = dir.Parent;
            }
            throw new InvalidOperationException("找不到仓库根目录(向上找 src/BiliLite.UWP/Controls/PlayerToast.xaml)");
        }
    }
}
