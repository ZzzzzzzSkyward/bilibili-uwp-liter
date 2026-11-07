using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace FocusProbe
{
    /// <summary>
    /// BR-46 探针:换视频之后按空格不能变成切页签。
    ///
    /// 病根:PivotHeaderItem 自己吃掉空格键去切页签,播放器那条 CoreWindow.KeyDown 只收没人处理的键,
    /// 于是连播/下一个/目录/F5 换过视频之后,焦点留在页签头上,空格就再也没到过播放器。
    /// 修法:PlayerControl 只留一个公开的焦点交还入口,页面在 InitializeVideo 的成功路径上带着门禁调它
    /// (焦点确实在 Pivot / PivotHeaderItem 上才抢),定时器那条路不许碰页签头。
    ///
    /// 探针只读源码,不编译 App 工程。想验红就把 HEAD 的源码拷出来传 --player / --page:
    ///   sh plan/tests/focus/run.sh --player /tmp/red/PlayerControl.xaml.cs --page /tmp/red/VideoDetailPage.xaml.cs
    /// </summary>
    internal static class Program
    {
        private static int _checks;
        private static readonly List<string> Failures = new List<string>();

        private const string FocusEntry = "FocusPlayer";
        private const string GateHelper = "IsTabStripFocused";
        private const string FocusSink = "BtnFoucs.Focus(FocusState.Programmatic)";

        // Timer_focus_Tick 只该认这四种控件:多一样都可能变成"定时器从页签上抢焦点"
        private static readonly string[] TimerFocusTypes =
        {
            "Button", "AppBarButton", "HyperlinkButton", "MenuFlyoutItem",
        };

        private static int Main(string[] args)
        {
            string playerPath = null;
            string pagePath = null;
            for (var i = 0; i < args.Length; i++)
            {
                if (args[i] == "--player" && i + 1 < args.Length) playerPath = args[++i];
                else if (args[i] == "--page" && i + 1 < args.Length) pagePath = args[++i];
                else
                {
                    Console.Error.WriteLine("未知参数:" + args[i] + "(可用:--player <path> --page <path>)");
                    return 2;
                }
            }

            var root = FindRepoRoot();
            var playerFile = playerPath ?? Path.Combine(root, "src", "BiliLite.UWP", "Controls", "PlayerControl.xaml.cs");
            var pageFile = pagePath ?? Path.Combine(root, "src", "BiliLite.UWP", "Pages", "VideoDetailPage.xaml.cs");
            Console.WriteLine("player = " + playerFile);
            Console.WriteLine("page   = " + pageFile);
            if (!File.Exists(playerFile) || !File.Exists(pageFile))
            {
                Console.Error.WriteLine("源文件不存在");
                return 2;
            }

            var player = File.ReadAllText(playerFile);
            var page = File.ReadAllText(pageFile);

            CheckPlayer(player);
            CheckPage(page);

            Console.WriteLine();
            if (Failures.Count == 0)
            {
                Console.WriteLine("focus PASS —— " + _checks + " 项检查全过");
                return 0;
            }
            Console.WriteLine("focus FAIL —— " + Failures.Count + " / " + _checks + " 项没过:");
            foreach (var failure in Failures) Console.WriteLine("  - " + failure);
            return 1;
        }

        // ---------- PlayerControl ----------

        private static void CheckPlayer(string code)
        {
            var map = ScanBraces(code);

            // 唯一公开的焦点交还入口:除了它,不许有第二个公开方法去动 BtnFoucs
            var sinks = Occurrences(code, FocusSink);
            Check(sinks.Count > 0, "PlayerControl 里还有 " + FocusSink + " 这个接键落点");
            var publicSinks = new List<string>();
            foreach (var index in sinks)
            {
                var decl = EnclosingDeclaration(code, map, index);
                if (decl != null && decl.Access == "public") publicSinks.Add(decl.Name);
            }
            // "只交还焦点"的入口:公开、方法体除了这一句什么都不做。别的公开方法顺手给焦点不算入口。
            var pureEntries = new List<string>();
            foreach (var index in sinks)
            {
                var decl = EnclosingDeclaration(code, map, index);
                if (decl == null || decl.Access != "public") continue;
                var body = code.Substring(decl.BodyStart, decl.BodyEnd - decl.BodyStart + 1);
                if (Normalize(body) == Normalize(FocusSink + ";")) pureEntries.Add(decl.Name);
            }
            var distinctPure = pureEntries.Distinct(StringComparer.Ordinal).ToList();
            Check(distinctPure.Count == 1,
                "PlayerControl 只有一个公开的焦点交还入口(实际:"
                + (distinctPure.Count == 0 ? "无" : string.Join(",", distinctPure)) + ")");
            Check(distinctPure.Count == 1 && distinctPure[0] == FocusEntry,
                "那个入口就叫 " + FocusEntry + "(实际:"
                + (distinctPure.Count == 0 ? "无" : string.Join(",", distinctPure)) + ")");
            Check(distinctPure.Count == 1 && distinctPure[0] == FocusEntry
                  && FocusSink.Length > 0,
                "那个入口交还的落点就是 " + FocusSink);

            var entryDecl = code.IndexOf("public void " + FocusEntry + "(", StringComparison.Ordinal);
            Check(entryDecl >= 0, "PlayerControl 声明了 public void " + FocusEntry + "()");
            if (entryDecl >= 0)
            {
                var body = MethodBody(code, map, entryDecl);
                Check(body != null, FocusEntry + " 的方法体能取到");
                if (body != null)
                {
                    Check(CountOf(body, FocusSink) == 1,
                        FocusEntry + " 只调一次 " + FocusSink + "(实际 " + CountOf(body, FocusSink) + " 次)");
                    Check(!body.Contains("Timer_focus") && !body.Contains("timer_focus"),
                        FocusEntry + " 不去碰定时器");
                }
            }

            // 定时器那条路不许把页签头加进类型表:否则焦点会被定时器从页签上抢走,键盘切换页签就废了
            var tickDecl = code.IndexOf("private void Timer_focus_Tick(", StringComparison.Ordinal);
            Check(tickDecl >= 0, "PlayerControl 里还有 Timer_focus_Tick");
            if (tickDecl >= 0)
            {
                var tickBody = MethodBody(code, map, tickDecl);
                Check(tickBody != null, "Timer_focus_Tick 的方法体能取到");
                if (tickBody != null)
                {
                    var types = IsTypesOf(tickBody);
                    Check(!types.Contains("PivotHeaderItem"),
                        "Timer_focus_Tick 没有把 PivotHeaderItem 加进类型表(加了就是定时器抢页签的焦点)");
                    Check(!types.Contains("Pivot"),
                        "Timer_focus_Tick 没有把 Pivot 加进类型表");
                    var extra = types.Where(t => !TimerFocusTypes.Contains(t)).ToList();
                    Check(extra.Count == 0,
                        "Timer_focus_Tick 的类型表还是那四种" + (extra.Count == 0 ? "" : "(多出:" + string.Join(",", extra) + ")"));
                    Check(CountOf(tickBody, FocusSink) == 1,
                        "Timer_focus_Tick 仍然把焦点还给播放器(修法没把它弄哑)");
                }
            }
        }

        // ---------- VideoDetailPage ----------

        private static void CheckPage(string code)
        {
            var map = ScanBraces(code);

            var initDecl = code.IndexOf("private async Task<bool> InitializeVideo(", StringComparison.Ordinal);
            Check(initDecl >= 0, "页面上还有 InitializeVideo(string id, PlayTargetStart start)");
            if (initDecl < 0) return;
            var initBody = MethodBody(code, map, initDecl);
            Check(initBody != null, "InitializeVideo 的方法体能取到");
            if (initBody == null) return;
            var bodyStart = code.IndexOf('{', initDecl);

            var calls = Occurrences(initBody, "player." + FocusEntry + "(");
            Check(calls.Count == 1,
                "InitializeVideo 调一次 player." + FocusEntry + "()(实际 " + calls.Count + " 次)");
            if (calls.Count == 1)
            {
                var callAt = bodyStart + calls[0];
                var publishAt = code.IndexOf("player.InitializePlayInfo(", bodyStart, StringComparison.Ordinal);
                Check(publishAt >= 0 && callAt > publishAt,
                    "交还焦点排在播放信息发布之后(" + Where(code, callAt, publishAt) + ")");

                var lastReturn = initBody.LastIndexOf("return played;", StringComparison.Ordinal);
                Check(lastReturn >= 0 && callAt < bodyStart + lastReturn,
                    "交还焦点排在方法最后那句 return played 之前");

                var ticketCheck = code.IndexOf("transition.IsCurrent(ticket)", bodyStart, StringComparison.Ordinal);
                Check(ticketCheck >= 0 && callAt > ticketCheck,
                    "交还焦点排在 _isClosed/_id/票据 那几道检查之后(晚到的结果不会抢焦点)");

                // 门禁:必须走页面那个唯一的判定,不能无条件抢焦点
                var guardAt = code.LastIndexOf("if (", callAt, StringComparison.Ordinal);
                var guardText = guardAt < 0 ? "" : code.Substring(guardAt, callAt - guardAt);
                Check(guardAt >= 0 && callAt - guardAt < 200 && guardText.Contains(GateHelper + "()"),
                    "这一处调用在 if (" + GateHelper + "()) 里面,不是无条件抢焦点");
                Check(code.Contains("if (played && " + GateHelper + "())"),
                    "交还焦点带着门禁:只在真正起播过(played)且焦点在页签上时才做");
            }

            // 全页面只有 InitializeVideo 这一处交还焦点:别的地方抢焦点会干扰目录和输入框
            var pageCalls = Occurrences(code, "player." + FocusEntry + "(");
            Check(pageCalls.Count == 1,
                "整个页面只有一处 player." + FocusEntry + "()(实际 " + pageCalls.Count + " 处)");

            // 门禁助手:只认 Pivot / PivotHeaderItem,别的一概不抢
            var helperDecl = code.IndexOf("private bool " + GateHelper + "(", StringComparison.Ordinal);
            Check(helperDecl >= 0, "页面上有一个私有判定 " + GateHelper + "()");
            Check(Occurrences(code, GateHelper + "()").Count == 2,
                "判定只在声明和 InitializeVideo 里出现(实际 " + Occurrences(code, GateHelper + "()").Count + " 处)");
            if (helperDecl >= 0)
            {
                var helperBody = MethodBody(code, map, helperDecl);
                Check(helperBody != null, GateHelper + " 的方法体能取到");
                if (helperBody != null)
                {
                    Check(helperBody.Contains("is PivotHeaderItem"),
                        GateHelper + " 认 PivotHeaderItem(空格被它吃掉的那个页签头)");
                    Check(helperBody.Contains("is Pivot"),
                        GateHelper + " 认 Pivot 本身");
                    Check(helperBody.Contains("FocusManager.GetFocusedElement()"),
                        GateHelper + " 读的是真实焦点");
                    var types = IsTypesOf(helperBody);
                    var extra = types.Where(t => t != "PivotHeaderItem" && t != "Pivot").ToList();
                    Check(extra.Count == 0,
                        GateHelper + " 只认 Pivot/PivotHeaderItem,不多抢别的控件"
                        + (extra.Count == 0 ? "" : "(多出:" + string.Join(",", extra) + ")"));
                }
            }

            // 页面那条 CoreWindow.KeyDown 照旧只管 F5:空格不许在这里被特判
            var keyDecl = code.IndexOf("private async void CoreWindow_KeyDown(", StringComparison.Ordinal);
            Check(keyDecl >= 0, "页面上还有 CoreWindow_KeyDown");
            if (keyDecl >= 0)
            {
                var keyBody = MethodBody(code, map, keyDecl);
                Check(keyBody != null, "CoreWindow_KeyDown 的方法体能取到");
                if (keyBody != null)
                {
                    Check(!keyBody.Contains("VirtualKey.Space"),
                        "CoreWindow_KeyDown 仍然没有特判 Space(空格归播放器,不在这里拦)");
                }
            }
        }

        private static string Where(string code, int callAt, int publishAt)
        {
            return "调用在第 " + LineOf(code, callAt) + " 行,发布在第 " + LineOf(code, publishAt) + " 行";
        }

        // ---------- C# 结构 ----------

        private sealed class CodeMap
        {
            public int[] BlockIds;
            public Dictionary<int, int> BraceMatch;
        }

        /// <summary>
        /// 每个字符归属的最内层花括号块 + 配对关系。字符串/注释里的花括号要跳过,
        /// 不然注释里的花括号会把块编号带跑
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

        private sealed class Declaration
        {
            public string Access;
            public string Name;
            public int Index;
            public int BodyStart;
            public int BodyEnd;
        }

        private static readonly Regex DeclarationRegex = new Regex(
            @"\b(public|private|protected|internal)\s+(?:(?:static|async|override|virtual|sealed|partial|new|extern|unsafe)\s+)*"
            + @"(?:[A-Za-z_][\w<>\[\],\.\?]*\s+)?(?<name>[A-Za-z_]\w*)\s*\(");

        private static Declaration EnclosingDeclaration(string code, CodeMap map, int index)
        {
            Declaration best = null;
            foreach (Match m in DeclarationRegex.Matches(code))
            {
                if (m.Index >= index) break;
                var open = code.IndexOf('{', m.Index);
                if (open < 0 || open > index) continue;
                int close;
                if (!map.BraceMatch.TryGetValue(open, out close) || close < index) continue;
                if (best == null || open > best.BodyStart)
                {
                    best = new Declaration
                    {
                        Access = m.Groups[1].Value,
                        Name = m.Groups["name"].Value,
                        Index = m.Index,
                        BodyStart = open,
                        BodyEnd = close,
                    };
                }
            }
            return best;
        }

        private static string MethodBody(string code, CodeMap map, int declIndex)
        {
            var open = code.IndexOf('{', declIndex);
            int close;
            if (open < 0 || !map.BraceMatch.TryGetValue(open, out close)) return null;
            return code.Substring(open, close - open + 1);
        }

        private static List<string> IsTypesOf(string body)
        {
            var types = new List<string>();
            foreach (Match m in Regex.Matches(body, @"\bis\s+([A-Za-z_][\w\.]*)"))
            {
                var type = m.Groups[1].Value;
                var dot = type.LastIndexOf('.');
                types.Add(dot >= 0 ? type.Substring(dot + 1) : type);
            }
            return types;
        }

        // ---------- helpers ----------

        private static List<int> Occurrences(string haystack, string needle)
        {
            var hits = new List<int>();
            var index = 0;
            while ((index = haystack.IndexOf(needle, index, StringComparison.Ordinal)) >= 0)
            {
                hits.Add(index);
                index += needle.Length;
            }
            return hits;
        }

        private static int CountOf(string haystack, string needle)
        {
            return Occurrences(haystack, needle).Count;
        }

        /// <summary>去掉注释和空白,把"方法体里只剩一句话"判成同一串</summary>
        private static string Normalize(string body)
        {
            var text = Regex.Replace(body, @"/\*.*?\*/", "", RegexOptions.Singleline);
            text = Regex.Replace(text, @"//[^\n]*", "");
            text = Regex.Replace(text, @"\s+", "");
            return text.Replace("{", "").Replace("}", "").Replace(";", "");
        }

        private static string LineOf(string text, int index)
        {
            var line = 1;
            for (var i = 0; i < index && i < text.Length; i++)
            {
                if (text[i] == '\n') line++;
            }
            return line.ToString();
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
            foreach (var start in new[] { AppContext.BaseDirectory, Directory.GetCurrentDirectory() })
            {
                var dir = new DirectoryInfo(start);
                while (dir != null)
                {
                    if (File.Exists(Path.Combine(dir.FullName, "src", "BiliLite.UWP", "Pages", "VideoDetailPage.xaml.cs")))
                        return dir.FullName;
                    dir = dir.Parent;
                }
            }
            throw new InvalidOperationException("找不到仓库根目录(向上找 src/BiliLite.UWP/Pages/VideoDetailPage.xaml.cs)");
        }
    }
}
