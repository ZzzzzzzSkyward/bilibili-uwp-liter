using System;
using System.Collections.Generic;
using BiliLite.Controls.Dynamic;
using BiliLite.Modules;
using BiliLite.Parsing.Dynamic;

namespace G3Probe
{
    /// <summary>
    /// CARD_PLAN §4「Render semantic text as text」的正文流探测。
    ///
    /// 断言只针对真实产品源码（Parsing/Dynamic/RichTextFlow.cs + Controls/Dynamic/RichTextBodyState.cs），
    /// 不重写被测逻辑：句子里出现的每个空白、换行、表情降级都由 RichTextFlow.Build 现算。
    /// 规则住在 XAML/控件里的部分（MaxLines、展开按钮、剪枝时序）在这里用源码检查钉住接线，
    /// 决策本身的纯函数仍由本探测直接断言。
    /// </summary>
    public static class RichTextFlowProbe
    {
        private static int _passed;

        public static int Run()
        {
            NoWhitespaceAcrossSegmentKinds();
            SegmentKindsDoNotBreakTheLine();
            CrlfNormalisesOnce();
            BareCarriageReturnBecomesOneBreak();
            BlankLineSurvives();
            CrossNodeNewlineIsNotDoubled();
            WhitespaceOnlyBodyIsNotContent();
            ProtocolRelativeEmojiIconIsCompleted();
            EmojiSequencesSurviveVerbatim();
            UnusableEmojiIconDegradesToText();
            EmojiVariantIsASemanticSlot();
            EmojiMetricsStayWithinOneAndAHalfEm();
            LineHeightGrowsOnlyWhenTheEmojiNeedsIt();
            LineCapPerSurface();
            ToggleOnlyWhenContentOverflows();
            ExpansionSurvivesSameIdAndResetsOnNewId();
            DetailStartsExpanded();
            IdentityChangeForcesARemount();
            CollapsingKeepsTheFullPlainText();
            EmptyBodyHidesTheRegion();
            ScrollAnchorCompensationKeepsTheAnchor();
            ControlAndRendererWiring();

            if (_failed > 0)
            {
                Console.Error.WriteLine("FAIL " + _failed + " RichTextFlow assertions");
                Environment.Exit(1);
            }
            Console.WriteLine("PASS " + _passed + " RichTextFlow assertions");
            return _passed;
        }

        // ---- 表情：图标可用才用图，否则退回文本 ----

        private static void ProtocolRelativeEmojiIconIsCompleted()
        {
            var segments = RichTextFlow.Build(
                new List<DynamicCardModel2024_node> { Emoji("//i0.example/e.png", "[笑]", 1) }, "");

            Check(segments.Count == 1 && segments[0].Kind == RichTextSegmentKind.Emoji,
                "emoji: a protocol-relative icon stays an image emoji (kind=" +
                (segments.Count == 1 ? segments[0].Kind.ToString() : "none") + ")");
            Check(segments.Count == 1 && segments[0].ImageUrl == "https://i0.example/e.png",
                "emoji: a protocol-relative icon is completed with https before the renderer sees it (url=" +
                (segments.Count == 1 ? segments[0].ImageUrl : "none") + ")");
            Check(RichTextFlow.PlainText(segments) == "[笑]",
                "emoji: an image emoji still copies as its readable text (text=" + RichTextFlow.PlainText(segments) + ")");
        }

        /// <summary>表情序列、变体选择符与零宽连接符都是正文的一部分，不能被拆开或改写。</summary>
        private static void EmojiSequencesSurviveVerbatim()
        {
            const string family = "👨\u200d👩\u200d👧";
            const string withSelector = "❤\ufe0f";

            var segments = RichTextFlow.Build(
                new List<DynamicCardModel2024_node> { Text(family), Text(withSelector) }, "");
            Check(RichTextFlow.PlainText(segments) == family + withSelector,
                "emoji sequence: ZWJ and variation selectors stay in the run verbatim");
            Check(segments.Count == 2,
                "emoji sequence: the two neighbouring runs are not merged or padded (count=" + segments.Count + ")");
        }

        private static void UnusableEmojiIconDegradesToText()
        {
            foreach (var icon in new[] { "", "not a url", "javascript:alert(1)", "data:image/png;base64,AAAA" })
            {
                var segments = RichTextFlow.Build(
                    new List<DynamicCardModel2024_node> { Text("前"), Emoji(icon, "[笑]", 1), Text("后") }, "");
                Check(RichTextFlow.PlainText(segments) == "前[笑]后",
                    "emoji: unusable icon '" + icon + "' keeps the readable text (text=" +
                    RichTextFlow.PlainText(segments) + ")");
                Check(segments.FindAll(x => x.Kind == RichTextSegmentKind.Emoji).Count == 0,
                    "emoji: unusable icon '" + icon + "' never produces an image segment");
            }
        }

        // ---- 句子流：类型切换不注入空白 ----

        private static void NoWhitespaceAcrossSegmentKinds()
        {
            var nodes = new List<DynamicCardModel2024_node>
            {
                Text("hello "),
                Mention("@user", "777"),
                Text(" world"),
            };

            var segments = RichTextFlow.Build(nodes, "");
            Check(segments.Count == 3, "sentence: three nodes make exactly three segments (count=" + segments.Count + ")");
            Check(segments[0].Text == "hello " && segments[1].Text == "@user" && segments[2].Text == " world",
                "sentence: every segment keeps the source text verbatim");
            Check(RichTextFlow.PlainText(segments) == "hello @user world",
                "sentence: text→mention→text reads as one sentence (text=" + RichTextFlow.PlainText(segments) + ")");
            Check(segments.FindAll(x => x.Kind == RichTextSegmentKind.LineBreak).Count == 0,
                "sentence: switching node kind does not inject a break");
        }

        private static void SegmentKindsDoNotBreakTheLine()
        {
            var nodes = new List<DynamicCardModel2024_node>
            {
                Text("笑"),
                Emoji("//i0.example/e.png", "[笑]", 1),
                Text("了"),
                Link("拍摄地点", "//example.com/place"),
                Text("，真好看"),
            };

            var segments = RichTextFlow.Build(nodes, "");
            Check(RichTextFlow.PlainText(segments) == "笑[笑]了拍摄地点，真好看",
                "sentence: emoji and link sit inside the sentence (text=" + RichTextFlow.PlainText(segments) + ")");
            Check(segments.Count == 5, "sentence: no whitespace-only segment is created between kinds (count=" + segments.Count + ")");
            Check(segments.FindAll(x => x.Kind == RichTextSegmentKind.LineBreak).Count == 0,
                "sentence: neither emoji nor link starts a new paragraph");
        }

        // ---- 换行：CRLF 只归一一次，真实 LF 才断行 ----

        private static void CrlfNormalisesOnce()
        {
            var segments = RichTextFlow.Build(null, "第一段\r\n第二段");
            Check(segments.Count == 3
                  && segments[0].Kind == RichTextSegmentKind.Text
                  && segments[1].Kind == RichTextSegmentKind.LineBreak
                  && segments[2].Kind == RichTextSegmentKind.Text,
                "crlf: one CRLF makes exactly one LineBreak between the two runs (count=" + segments.Count + ")");
            Check(RichTextFlow.PlainText(segments) == "第一段\n第二段",
                "crlf: copy text restores one LF (text=" + RichTextFlow.PlainText(segments).Replace("\n", "\\n") + ")");
        }

        private static void BareCarriageReturnBecomesOneBreak()
        {
            var segments = RichTextFlow.Build(null, "a\rb");
            Check(segments.FindAll(x => x.Kind == RichTextSegmentKind.LineBreak).Count == 1,
                "crlf: a bare CR is one break, not a literal character");
            Check(RichTextFlow.PlainText(segments) == "a\nb",
                "crlf: the bare CR never reaches the visible run (text=" + RichTextFlow.PlainText(segments).Replace("\n", "\\n") + ")");
        }

        private static void BlankLineSurvives()
        {
            var segments = RichTextFlow.Build(null, "第一段\r\n\r\n第二段");
            Check(segments.FindAll(x => x.Kind == RichTextSegmentKind.LineBreak).Count == 2,
                "crlf: an intentional blank line stays a blank line (two breaks)");
            Check(RichTextFlow.PlainText(segments) == "第一段\n\n第二段",
                "crlf: the blank line is preserved for copy too (text=" +
                RichTextFlow.PlainText(segments).Replace("\n", "\\n") + ")");
        }

        private static void CrossNodeNewlineIsNotDoubled()
        {
            // 换行落在节点边界上：一个节点以文本结尾、下一个节点以 LF 开头，仍然只有一个换行。
            var nodes = new List<DynamicCardModel2024_node>
            {
                Text("a\n"),
                Text("b"),
            };
            var segments = RichTextFlow.Build(nodes, "");
            Check(segments.FindAll(x => x.Kind == RichTextSegmentKind.LineBreak).Count == 1,
                "crlf: a source LF at a node boundary produces one break (count=" +
                segments.FindAll(x => x.Kind == RichTextSegmentKind.LineBreak).Count + ")");
            Check(RichTextFlow.PlainText(segments) == "a\nb",
                "crlf: the sentence is not doubled at the node boundary (text=" +
                RichTextFlow.PlainText(segments).Replace("\n", "\\n") + ")");
        }

        // ---- 空正文：不塞占位 ----

        private static void WhitespaceOnlyBodyIsNotContent()
        {
            var nodes = new List<DynamicCardModel2024_node>();
            Check(RichTextFlow.Build(nodes, "").Count == 0,
                "empty body: no nodes and no text yields no segment");
            Check(RichTextFlow.Build(nodes, "   ").Count == 0,
                "empty body: a whitespace-only body is still empty, not a blank line");
            Check(RichTextFlow.Build(nodes, "\r\n").Count == 0,
                "empty body: a lone line break is not a body");
        }

        // ---- 表情档位与度量 ----

        private static void EmojiVariantIsASemanticSlot()
        {
            Check(BuildSingleEmojiVariant(1) == RichTextEmojiVariant.Normal, "emoji size: size=1 is the normal slot");
            Check(BuildSingleEmojiVariant(2) == RichTextEmojiVariant.Large, "emoji size: size=2 is the large slot");
            Check(BuildSingleEmojiVariant(99) == RichTextEmojiVariant.Large,
                "emoji size: a huge wire size is still the large slot, not a multiplier");
            Check(BuildSingleEmojiVariant(-3) == RichTextEmojiVariant.Normal,
                "emoji size: an out-of-range size falls back to the normal slot");
        }

        private static RichTextEmojiVariant BuildSingleEmojiVariant(int wireSize)
        {
            var segments = RichTextFlow.Build(
                new List<DynamicCardModel2024_node> { Emoji("https://i0.example/e.png", "[笑]", wireSize) }, "");
            if (segments.Count != 1 || segments[0].Kind != RichTextSegmentKind.Emoji)
            {
                Check(false, "emoji size: a usable icon must produce one emoji segment (size=" + wireSize + ")");
                return RichTextEmojiVariant.Normal;
            }
            return segments[0].EmojiVariant;
        }

        private static void EmojiMetricsStayWithinOneAndAHalfEm()
        {
            const double body = 16;
            Check(RichTextFlow.EmojiSide(body, RichTextEmojiVariant.Normal) == 19,
                "emoji metrics: normal emoji is 1.2 body em rounded (side=" +
                RichTextFlow.EmojiSide(body, RichTextEmojiVariant.Normal) + ")");
            Check(RichTextFlow.EmojiSide(body, RichTextEmojiVariant.Large) == 24,
                "emoji metrics: large emoji is 1.5 body em (side=" +
                RichTextFlow.EmojiSide(body, RichTextEmojiVariant.Large) + ")");
            Check(RichTextFlow.EmojiSide(body, RichTextEmojiVariant.Large) <= body * RichTextFlow.LargeEmojiEm,
                "emoji metrics: the large slot never exceeds 1.5 body em");
            Check(RichTextFlow.EmojiSide(32, RichTextEmojiVariant.Large) == 48,
                "emoji metrics: the size follows the body font, not a fixed pixel box");
            Check(RichTextFlow.EmojiBaselineOffset(body) > 0
                  && RichTextFlow.EmojiBaselineOffset(body) < body / 2,
                "emoji metrics: the baseline drop is a descender, not a full glyph (drop=" +
                RichTextFlow.EmojiBaselineOffset(body) + ")");
        }

        private static void LineHeightGrowsOnlyWhenTheEmojiNeedsIt()
        {
            const double body = 16;
            const double line = 24;

            var plain = RichTextFlow.Build(new List<DynamicCardModel2024_node> { Text("没有表情") }, "");
            Check(RichTextFlow.RequiredLineHeight(plain, body, line) == line,
                "line height: text without emoji keeps the body line height");

            var small = RichTextFlow.Build(
                new List<DynamicCardModel2024_node> { Emoji("https://i0.example/s.png", "[笑]", 1) }, "");
            Check(RichTextFlow.RequiredLineHeight(small, body, line) == line,
                "line height: a normal emoji still fits one line, spacing does not pre-grow (height=" +
                RichTextFlow.RequiredLineHeight(small, body, line) + ")");

            var large = RichTextFlow.Build(
                new List<DynamicCardModel2024_node> { Emoji("https://i0.example/l.png", "[大笑]", 2) }, "");
            var grown = RichTextFlow.RequiredLineHeight(large, body, line);
            Check(grown == 27 && grown > line,
                "line height: a large emoji grows the line instead of being clipped (height=" + grown + ")");
            Check(RichTextFlow.RequiredLineHeight(large, body, 40) == 40,
                "line height: an already tall line is not shrunk by the emoji");
        }

        // ---- 展开策略：六行 / 四行 / 详情展开 ----

        private static void LineCapPerSurface()
        {
            Check(RichTextBodyPolicy.LineCap(RichTextBodyRole.Feed) == 6,
                "line cap: a feed body starts at six rendered lines");
            Check(RichTextBodyPolicy.LineCap(RichTextBodyRole.RepostOrigin) == 4,
                "line cap: a repost-origin body starts at four rendered lines");
            Check(RichTextBodyPolicy.LineCap(RichTextBodyRole.Detail) == RichTextBodyPolicy.UnlimitedLines,
                "line cap: the detail body is not capped");
            Check(!RichTextBodyPolicy.StartsExpanded(RichTextBodyRole.Feed)
                  && !RichTextBodyPolicy.StartsExpanded(RichTextBodyRole.RepostOrigin)
                  && RichTextBodyPolicy.StartsExpanded(RichTextBodyRole.Detail),
                "line cap: only the detail surface starts expanded");

            Check(RichTextBodyPolicy.ResolveRole(RichTextBodyRole.Feed, 1) == RichTextBodyRole.Feed,
                "line cap: a top-level card body is a feed body");
            Check(RichTextBodyPolicy.ResolveRole(RichTextBodyRole.Feed, 2) == RichTextBodyRole.RepostOrigin,
                "line cap: a card nested inside another card is a repost-origin body");
            Check(RichTextBodyPolicy.ResolveRole(RichTextBodyRole.Detail, 2) == RichTextBodyRole.Detail,
                "line cap: the declared detail role wins over nesting");
        }

        private static void ToggleOnlyWhenContentOverflows()
        {
            Check(!RichTextBodyPolicy.Overflows(3, 6), "overflow: a short body offers no toggle");
            Check(!RichTextBodyPolicy.Overflows(6, 6),
                "overflow: a body that exactly fills the cap offers no toggle (only real overflow does)");
            Check(RichTextBodyPolicy.Overflows(7, 6), "overflow: one line past the cap offers the toggle");
            Check(!RichTextBodyPolicy.Overflows(4, 4), "overflow: repost-origin cap of four is not overflow at four");
            Check(RichTextBodyPolicy.Overflows(5, 4), "overflow: repost-origin overflow at five lines");
            Check(!RichTextBodyPolicy.Overflows(40, RichTextBodyPolicy.UnlimitedLines),
                "overflow: an uncapped (detail) body never shows the toggle");

            const double line = 24;
            Check(RichTextBodyPolicy.RenderedLines(6 * line, line) == 6,
                "overflow: six line boxes measure as six rendered lines");
            Check(RichTextBodyPolicy.RenderedLines(8 * line, line) == 8,
                "overflow: eight line boxes measure as eight rendered lines");
            Check(RichTextBodyPolicy.RenderedLines(6 * line + 2, line) == 6,
                "overflow: sub-line measurement noise does not fake a seventh line");
            Check(RichTextBodyPolicy.RenderedLines(6 * line + line * 0.6, line) == 7,
                "overflow: a real seventh line is counted as one");
            Check(RichTextBodyPolicy.RenderedLines(100, 0) == 0,
                "overflow: a zero line height cannot claim lines");
            Check(RichTextBodyPolicy.RenderedLines(-5, line) == 0,
                "overflow: a negative measurement is not content");
        }

        // ---- 展开状态：按动态 id 记住，换 id 复位 ----

        private static void ExpansionSurvivesSameIdAndResetsOnNewId()
        {
            var state = new RichTextBodyState();
            Check(state.BeginIdentity("9001", RichTextBodyRole.Feed),
                "expansion: the first identity is a change");
            Check(!state.IsExpanded, "expansion: a feed body starts collapsed");

            state.SetExpanded(true);
            Check(state.IsExpanded, "expansion: 展开 flips the state for the current id");

            // 同一张卡片重渲染（新内容、同一 id）：展开态必须还在
            state.Apply(new object(), "");
            Check(!state.BeginIdentity("9001", RichTextBodyRole.Feed),
                "expansion: re-rendering the same id is not an identity change");
            Check(state.IsExpanded, "expansion: re-rendering the same id keeps the expanded state");

            Check(state.BeginIdentity("9002", RichTextBodyRole.Feed),
                "expansion: a recycled card with a new id is an identity change");
            Check(!state.IsExpanded, "expansion: a new id starts collapsed, the old expansion does not leak");

            state.SetExpanded(true);
            state.BeginIdentity("9001", RichTextBodyRole.Feed);
            Check(!state.IsExpanded, "expansion: switching back to an old id does not resurrect its expansion");
        }

        private static void DetailStartsExpanded()
        {
            var state = new RichTextBodyState();
            state.BeginIdentity("9003", RichTextBodyRole.Detail);
            Check(state.IsExpanded, "expansion: the detail surface starts expanded");

            state.Apply(new object(), "");
            state.BeginIdentity("9003", RichTextBodyRole.Detail);
            Check(state.IsExpanded, "expansion: a detail re-render stays expanded");
        }

        // ---- 换身份：先清旧正文，再挂新正文 ----

        private static void IdentityChangeForcesARemount()
        {
            Check(RichTextBodyPolicy.NeedsClear(identityChanged: true, contentChanged: false),
                "recycle: a new identity clears even when the content object looks unchanged");
            Check(RichTextBodyPolicy.NeedsClear(identityChanged: false, contentChanged: true),
                "recycle: changed content clears too");
            Check(!RichTextBodyPolicy.NeedsClear(identityChanged: false, contentChanged: false),
                "recycle: an unchanged body is left mounted");

            var state = new RichTextBodyState();
            var rich = new object();
            Check(state.Apply(rich, "").Changed, "recycle: card A mounts its rich body");
            state.BeginIdentity("9002", RichTextBodyRole.Feed);
            Check(state.Apply(rich, "").Changed,
                "recycle: after an identity change the same instance still remounts (old inlines were cleared)");
            Check(!state.Apply(rich, "").Changed,
                "recycle: without an identity change the same instance is not remounted twice");
        }

        // ---- 折叠不删正文：纯文本仍完整 ----

        private static void CollapsingKeepsTheFullPlainText()
        {
            var nodes = new List<DynamicCardModel2024_node>();
            for (var i = 0; i < 10; i++)
            {
                if (i > 0)
                {
                    nodes.Add(Text("\n"));
                }
                nodes.Add(Text("第" + i + "段"));
            }

            var segments = RichTextFlow.Build(nodes, "");
            var plain = RichTextFlow.PlainText(segments);
            Check(plain.EndsWith("第9段", StringComparison.Ordinal) && plain.StartsWith("第0段", StringComparison.Ordinal),
                "collapsed body: every paragraph stays in the copy text (text=" + plain.Replace("\n", "\\n") + ")");
            Check(plain.Split('\n').Length == 10,
                "collapsed body: all ten paragraphs are still available for copy");
            Check(segments.FindAll(x => x.Kind == RichTextSegmentKind.LineBreak).Count == 9,
                "collapsed body: the source breaks are all kept, the line cap is only visual");
            Check(RichTextBodyPolicy.Overflows(10, RichTextBodyPolicy.LineCap(RichTextBodyRole.Feed)),
                "collapsed body: ten paragraphs overflow the six-line feed cap");
        }

        // ---- 空正文：区域收起，不塞“无内容” ----

        private static void EmptyBodyHidesTheRegion()
        {
            var state = new RichTextBodyState();
            var empty = state.Apply(null, "");
            Check(empty.Kind == RichTextBodyKind.Empty, "empty body: nothing to mount");
            Check(empty.Text == "", "empty body: no placeholder text is produced (text=" + empty.Text + ")");
            Check(RichTextBodyPolicy.HidesRegion(empty.Kind),
                "empty body: the body region is hidden, not filled with 无内容");

            var rich = new object();
            Check(!RichTextBodyPolicy.HidesRegion(state.Apply(rich, "").Kind),
                "empty body: a card with a real body keeps the region visible");

            var recycled = state.Apply(null, "");
            Check(recycled.Kind == RichTextBodyKind.Empty && RichTextBodyPolicy.HidesRegion(recycled.Kind),
                "empty body: a recycled picture/video card hides the region again");
        }

        // ---- 高度变化时保住滚动锚点 ----

        private static void ScrollAnchorCompensationKeepsTheAnchor()
        {
            var delta = RichTextBodyPolicy.ScrollCompensation(120, 300);
            Check(delta == 180, "scroll anchor: the compensation is the anchor's own displacement (delta=" + delta + ")");
            Check(300 - delta == 120,
                "scroll anchor: scrolling by the delta puts the anchor back where it was");
            Check(RichTextBodyPolicy.CompensatedOffset(500, delta) == 680,
                "scroll anchor: the offset moves by the same delta");
            Check(RichTextBodyPolicy.CompensatedOffset(100, -180) == 0,
                "scroll anchor: a shrink near the top clamps at zero instead of overscrolling");
            Check(RichTextBodyPolicy.ScrollCompensation(80, 80) == 0,
                "scroll anchor: an unchanged layout compensates nothing");
        }

        // ---- 接线：规则住在 XAML/控件里，用源码检查钉住 ----

        private static void ControlAndRendererWiring()
        {
            var root = RepoRoot();
            var controlXaml = Read(root, "src/BiliLite.UWP/Controls/Dynamic/RichTextControl.xaml");
            var controlCode = Read(root, "src/BiliLite.UWP/Controls/Dynamic/RichTextControl.xaml.cs");
            var renderer = Read(root, "src/BiliLite.UWP/Controls/Dynamic/DynamicParse.cs");

            Check(controlXaml.Contains("x:Name=\"ExpandToggle\""),
                "wiring: RichTextControl owns the 展开/收起 toggle");
            Check(controlXaml.Contains("x:Name=\"BodyHost\""),
                "wiring: the body is mounted into its own host, so clearing it cannot drop the toggle");
            Check(controlCode.Contains("RichTextBodyPolicy.LineCap"),
                "wiring: the control takes the line cap from the policy, not from a literal");
            Check(controlCode.Contains("MaxLines"),
                "wiring: the mounted block is clamped by MaxLines");
            Check(controlCode.Contains("RichTextBodyPolicy.HidesRegion"),
                "wiring: the control hides the region through the policy");
            Check(controlCode.Contains("card.BodyRole"),
                "wiring: the shared templates take the body role from the card shell the page styles");
            Check(controlCode.Contains("BodyHost.Children.Clear()"),
                "wiring: a new identity clears the old body out of the host before mounting the new one");
            Check(controlCode.Contains("r.Parent != null && !ReferenceEquals(r.Parent, BodyHost)"),
                "wiring: one RichTextBlock is never attached to two parents");
            Check(controlCode.Contains("ChangeView"),
                "wiring: the height change compensates the scroll offset");
            Check(controlCode.Contains("RichTextFlow.PlainText") == false
                  && renderer.Contains("RichTextFlow.PlainText"),
                "wiring: the copy text comes from RichTextFlow.PlainText at build time, not a second implementation");
            Check(renderer.Contains("RichTextFlow.EmojiSide")
                  && renderer.Contains("RichTextFlow.EmojiBaselineOffset")
                  && renderer.Contains("RichTextFlow.RequiredLineHeight"),
                "wiring: the renderer takes emoji size, baseline and line height from RichTextFlow");
            Check(!renderer.Contains("const double BodyFontSize = 14"),
                "wiring: the renderer no longer measures emoji against a private 14px body font");
        }

        private static string RepoRoot()
        {
            var dir = new System.IO.DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null)
            {
                if (System.IO.File.Exists(System.IO.Path.Combine(dir.FullName, "src", "BiliLite.UWP", "Services", "GrpcService.cs")))
                {
                    return dir.FullName;
                }
                dir = dir.Parent;
            }
            throw new InvalidOperationException("repository root not found from " + AppContext.BaseDirectory);
        }

        private static string Read(string root, string relative)
        {
            return System.IO.File.ReadAllText(System.IO.Path.Combine(root, relative));
        }

        // ---- 夹具 ----

        private static DynamicCardModel2024_node Text(string text)
        {
            return new DynamicCardModel2024_node { type = "RICH_TEXT_NODE_TYPE_TEXT", text = text };
        }

        private static DynamicCardModel2024_node Mention(string text, string mid)
        {
            return new DynamicCardModel2024_node { type = "RICH_TEXT_NODE_TYPE_AT", text = text, rid = mid };
        }

        private static DynamicCardModel2024_node Link(string text, string url)
        {
            return new DynamicCardModel2024_node { type = "RICH_TEXT_NODE_TYPE_WEB", text = text, jump_url = url };
        }

        private static DynamicCardModel2024_node Emoji(string icon, string text, int size)
        {
            return new DynamicCardModel2024_node
            {
                type = "RICH_TEXT_NODE_TYPE_EMOJI",
                text = text,
                orig_text = text,
                emoji = new DynamicCardModel2024_emojiinfo { icon_url = icon, text = text, size = size },
            };
        }

        private static int _failed;

        private static void Check(bool condition, string message)
        {
            if (!condition)
            {
                _failed++;
                Console.Error.WriteLine("FAIL " + message);
                return;
            }
            _passed++;
            Console.WriteLine("ok " + message);
        }
    }
}
