using System;
using System.Collections.Generic;
using System.Text;
using BiliLite.Controls.Dynamic;

namespace G3Probe
{
    /// <summary>
    /// 正文挂载规则（RichTextBodyState）的探测。
    ///
    /// 这里断言的是"复用后的最终挂载"，而不是单张卡片的独立用例：
    /// 1. 规则矩阵：富文本→空、空→富文本、空→空、同一段纯文本、null 富文本但有纯文本。
    /// 2. 两张卡片复用成一个序列：A 有富文本正文，B 没有正文——结束态必须是 B 的空/纯文本，
    ///    不能留下 A 的正文。
    /// 3. 对照组 PreFixBody 是修复前 RichTextControl 的逐行转写（rich 标志 + 早退 + 不清理），
    ///    同一序列跑一遍必须留下 A 的正文，证明这组断言确实能抓到旧行为（RED）。
    ///
    /// 控制层本身是 UserControl，不能在探测里实例化；所以 MountedBody 用字符串描述
    /// Parent.Children 的内容，驱动方式与 RichTextControl.RefreshBody 一一对应。
    /// </summary>
    public static class RichTextBodyProbe
    {
        private static int _passed;

        /// <param name="args">带 --prefix-red 时把复用序列跑在修复前的转写上，用来确认这组断言会红。</param>
        public static int Run(string[] args)
        {
            if (args != null && Array.IndexOf(args, "--prefix-red") >= 0)
            {
                return RunRedAgainstPreFix();
            }

            MatrixRichToEmpty();
            MatrixEmptyToRich();
            MatrixEmptyToEmpty();
            MatrixSamePlainText();
            MatrixNullRichWithPlainText();
            MatrixDifferentRichInstance();
            TwoCardReuseKeepsNoPreviousBody();
            TwoCardReuseBodylessCardWithText();
            ThreeCardReuseBackToRich();
            PreFixKeepsPreviousBody();
            PreFixSwallowsTheNewCardsText();

            Console.WriteLine("PASS " + _passed + " RichTextBodyState assertions");
            return _passed;
        }

        // ---- 规则矩阵 ----

        private static void MatrixRichToEmpty()
        {
            var state = new RichTextBodyState();
            var rich = new FakeRich("A");

            var first = state.Apply(rich, "");
            Check(first.Kind == RichTextBodyKind.RichText, "rich→empty: 有富文本时挂富文本");
            Check(first.Changed, "rich→empty: 首次挂载是变化");

            var cleared = state.Apply(null, "");
            Check(cleared.Kind == RichTextBodyKind.Empty, "rich→empty: 富文本被清空后降级为空");
            Check(cleared.Changed, "rich→empty: 清空必须报告变化，控制层才会移除上一条正文");
            Check(state.MountedKind == RichTextBodyKind.Empty, "rich→empty: 状态跟着挂载一起复位");
            Check(state.MountedRich == null, "rich→empty: 不再持有已清空的富文本实例");
        }

        private static void MatrixEmptyToRich()
        {
            var state = new RichTextBodyState();
            var empty = state.Apply(null, "");
            Check(empty.Kind == RichTextBodyKind.Empty && !empty.Changed, "empty→rich: 起始是空且无需重挂");

            var rich = new FakeRich("B");
            var mounted = state.Apply(rich, "旧文本");
            Check(mounted.Kind == RichTextBodyKind.RichText, "empty→rich: 有富文本时富文本优先");
            Check(mounted.Changed, "empty→rich: 空→富文本是变化");
            Check(mounted.Text == "", "empty→rich: 富文本挂载不带纯文本回退");
        }

        private static void MatrixEmptyToEmpty()
        {
            var state = new RichTextBodyState();
            var first = state.Apply(null, "");
            var second = state.Apply(null, "");
            Check(!first.Changed, "empty→empty: 空内容不触发挂载");
            Check(!second.Changed, "empty→empty: 重复的空内容仍然不触发挂载");
            Check(state.MountedKind == RichTextBodyKind.Empty, "empty→empty: 空状态保持为空");
        }

        private static void MatrixSamePlainText()
        {
            var state = new RichTextBodyState();
            var first = state.Apply(null, "同一段正文");
            var second = state.Apply(null, "同一段正文");
            Check(first.Changed, "same text: 首次纯文本回退是变化");
            Check(!second.Changed, "same text: 同样的文本不重建 TextBlock");
            Check(second.Kind == RichTextBodyKind.PlainText && second.Text == "同一段正文",
                "same text: 结果仍是同一段纯文本");

            var changed = state.Apply(null, "另一段正文");
            Check(changed.Changed && changed.Text == "另一段正文", "same text: 文本变了就要重挂");
        }

        private static void MatrixNullRichWithPlainText()
        {
            var state = new RichTextBodyState();
            var plan = state.Apply(null, "纯文本回退");
            Check(plan.Kind == RichTextBodyKind.PlainText, "null rich + 纯文本: 回退到纯文本");
            Check(plan.Text == "纯文本回退", "null rich + 纯文本: 回退文本原样带出");
        }

        private static void MatrixDifferentRichInstance()
        {
            var state = new RichTextBodyState();
            var a = new FakeRich("A");
            var b = new FakeRich("B");
            Check(state.Apply(a, "").Changed, "两个富文本实例: 挂 A 是变化");
            Check(state.Apply(b, "").Changed, "两个富文本实例: 换成 B 仍是变化（不是按文本比较）");
            Check(ReferenceEquals(state.MountedRich, b), "两个富文本实例: 状态记录当前挂的是 B");
        }

        // ---- 两张卡片复用：一个序列 ----

        private static void TwoCardReuseKeepsNoPreviousBody()
        {
            var body = new MountedBody();
            var cardA = new FakeRich("A 的正文");

            body.Refresh(cardA, "");
            Check(body.Child == "rich:A 的正文", "复用序列 1/2: 卡片 A 挂上自己的富文本正文");

            body.Refresh(null, "");
            Check(body.Child == null, "复用序列 2/2: 卡片 B 没有正文，控件不留 A 的正文");
        }

        private static void TwoCardReuseBodylessCardWithText()
        {
            var body = new MountedBody();
            body.Refresh(new FakeRich("A 的正文"), "");
            Check(body.Child == "rich:A 的正文", "复用(带文本) 1/2: 卡片 A 挂上富文本正文");

            body.Refresh(null, "B 的纯文本");
            Check(body.Child == "plain:B 的纯文本", "复用(带文本) 2/2: 卡片 B 显示自己的纯文本回退，而不是 A 的正文");
        }

        private static void ThreeCardReuseBackToRich()
        {
            var body = new MountedBody();
            body.Refresh(new FakeRich("A"), "");
            body.Refresh(null, "B");
            body.Refresh(new FakeRich("C"), "C 的 desc");
            Check(body.Child == "rich:C", "复用三张卡: A→B(纯文本)→C 的结束态是 C 的富文本");
        }

        /// <summary>
        /// RED 运行：复用序列原样跑在修复前的转写上。断言与 TwoCardReuseKeepsNoPreviousBody 一致，
        /// 因此这里必须 FAIL 并退出码 1，才说明前面的绿来自修复而不是断言太松。
        /// </summary>
        private static int RunRedAgainstPreFix()
        {
            var body = new PreFixBody();
            body.Refresh(new FakeRich("A 的正文"), "");
            Check(body.Child == "rich:A 的正文", "RED(pre-fix) 1/2: 卡片 A 挂上自己的富文本正文");

            body.Refresh(null, "");
            Check(body.Child == null, "RED(pre-fix) 2/2: 卡片 B 没有正文，控件不留 A 的正文");
            Console.WriteLine("RED run did not fail: the pre-fix rule passed the reuse assertions");
            return _passed;
        }

        // ---- 修复前逻辑的转写（RED 对照） ----

        private static void PreFixKeepsPreviousBody()
        {
            var body = new PreFixBody();
            body.OnText("");            // 卡片 A: Text="" (富文本卡片没有 desc)
            body.OnContent(new FakeRich("A 的正文"));

            body.OnText("");            // 卡片 B: Text="" 没有变化 → 没有回调
            body.OnContent(null);       // 卡片 B: Content 变成 null

            Check(body.Children.Count == 1 && body.Children[0] == "rich:A 的正文",
                "RED 对照组: 修复前 rich 标志早退 + 不清 Children，卡片 B 仍显示 A 的正文");
        }

        private static void PreFixSwallowsTheNewCardsText()
        {
            var body = new PreFixBody();
            body.OnText("");
            body.OnContent(new FakeRich("A 的正文"));

            body.OnText("B 的纯文本");  // rich 仍为 true → UpdateText 直接 return
            body.OnContent(null);       // rich=false，但仍不清 Children，也不补 B 的文本

            Check(body.Children.Count == 1 && body.Children[0] == "rich:A 的正文",
                "RED 对照组: 修复前 B 的纯文本被 rich 早退吞掉，结束时还是 A 的正文");
        }

        // ---- 夹具 ----

        /// <summary>富文本内容的替身：探测只需要"是不是同一个实例"和一个可读标签。</summary>
        private sealed class FakeRich
        {
            public FakeRich(string text)
            {
                Text = text;
            }

            public string Text { get; private set; }
        }

        /// <summary>修复后的挂载：RefreshBody 的等价驱动，Child 就是 Parent.Children 的内容。</summary>
        private sealed class MountedBody
        {
            private readonly RichTextBodyState _state = new RichTextBodyState();

            public string Child { get; private set; }

            public void Refresh(object richContent, string text)
            {
                var plan = _state.Apply(richContent, text);
                if (!plan.Changed)
                {
                    return;
                }

                Child = null;
                if (plan.Kind == RichTextBodyKind.RichText)
                {
                    Child = "rich:" + ((FakeRich)richContent).Text;
                }
                else if (plan.Kind == RichTextBodyKind.PlainText)
                {
                    Child = "plain:" + plan.Text;
                }
            }
        }

        /// <summary>
        /// 修复前 RichTextControl 的转写：UpdateContent 先置 rich=false，只有内容非空才 Clear+Add；
        /// UpdateText 在 rich 为 true 时直接返回，并在清空后按文本重新挂。缺的那一步正是"内容清空时复位"。
        /// </summary>
        private sealed class PreFixBody
        {
            private bool _rich;

            public List<string> Children { get; } = new List<string>();

            public string Child
            {
                get { return Children.Count == 0 ? null : Children[0]; }
            }

            public void OnContent(FakeRich content)
            {
                _rich = false;
                if (content != null)
                {
                    _rich = true;
                    Children.Clear();
                    Children.Add("rich:" + content.Text);
                }
            }

            public void OnText(string text)
            {
                if (_rich)
                {
                    return;
                }

                Children.Clear();
                if (string.IsNullOrEmpty(text))
                {
                    return;
                }

                Children.Add("plain:" + text);
            }

            /// <summary>按模板里的属性顺序驱动：先 Text 再 Content（DynamicTemplate.xaml 的 Photo3x3）。</summary>
            public void Refresh(object richContent, string text)
            {
                OnText(text);
                OnContent(richContent as FakeRich);
            }
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
