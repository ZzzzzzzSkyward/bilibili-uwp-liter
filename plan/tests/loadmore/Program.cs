using System;
using System.Collections.Generic;
using BiliLite.Controls;

namespace LoadMoreProbe
{
    /// <summary>
    /// 自动翻页探针:2026-11-15 22:03:59–22:04:42 那 966 次 show/popular/index 的回归护栏。
    ///
    /// 病根:MyAdaptiveGridView 在 Loading 每次 true→false 时判断「没填满一屏就再翻一页」,
    /// 而翻页失败也会把 Loading 置回 false,于是失败页 → 再翻 → 再失败,循环没有出口。
    /// 修法:AutoLoadMoreGate —— 只有内容真的变长过才允许再自动翻一次;失败/空页不增长,闸门保持卸膛。
    ///
    /// 本探针编译 App 工程里真实的 AutoLoadMoreGate.cs(链接编译,不是复制),
    /// 按控件里的接线方式回放日志:首屏成功 1 次 + 成功翻页 49 次 + 失败 900 次。
    /// </summary>
    internal static class Program
    {
        private static int _checks;
        private static readonly List<string> Failures = new List<string>();

        private const int InitialItems = 12;
        private const int ItemsPerPage = 12;

        private static int Main()
        {
            CheckFirstFillFiresOnce();
            CheckGrowthRearms();
            CheckBlockedCheckDoesNotConsume();
            CheckDetachedGridNeverFires();
            CheckStormReplay();
            CheckEmptyPageStops();

            Console.WriteLine();
            if (Failures.Count == 0)
            {
                Console.WriteLine($"PASS {_checks} checks");
                return 0;
            }
            Console.WriteLine($"FAIL {Failures.Count}/{_checks}");
            foreach (var failure in Failures)
            {
                Console.WriteLine("  - " + failure);
            }
            return 1;
        }

        /// <summary>首屏内容到位后,只能自动补一页;没有新内容就不能再补。</summary>
        private static void CheckFirstFillFiresOnce()
        {
            var grid = new GridSim();
            grid.AddItems(InitialItems);

            Check(grid.OnLoadingFalse(), "首屏回来后应该自动补一页");
            Check(!grid.OnLoadingFalse(), "没有新内容时不允许再次自动翻页");
            Check(grid.Fires == 1, $"自动翻页次数应为 1,实际 {grid.Fires}");
        }

        /// <summary>翻页成功、内容变长之后,闸门要重新打开。</summary>
        private static void CheckGrowthRearms()
        {
            var grid = new GridSim();
            grid.AddItems(InitialItems);
            grid.OnLoadingFalse();
            grid.AddItems(ItemsPerPage);

            Check(grid.OnLoadingFalse(), "内容变长后应该允许再自动翻一页");
            Check(grid.Fires == 2, $"自动翻页次数应为 2,实际 {grid.Fires}");
        }

        /// <summary>控件不在树上/高度为 0/还有可滚动内容时被拦下,不能把闸门消耗掉。</summary>
        private static void CheckBlockedCheckDoesNotConsume()
        {
            var grid = new GridSim();
            grid.AddItems(InitialItems);
            grid.CanAutoFill = false;

            Check(!grid.OnLoadingFalse(), "页面不在可视树里时不允许自动翻页");
            Check(!grid.OnLoadingFalse(), "页面不在可视树里时不允许自动翻页(第二次)");
            grid.CanAutoFill = true;
            Check(grid.OnLoadingFalse(), "回到可视树后仍应允许一次自动翻页");
            Check(grid.Fires == 1, $"自动翻页次数应为 1,实际 {grid.Fires}");
        }

        /// <summary>切走的缓存页(ScrollableHeight 停在 0)连续 1000 次 Loading 变化都不许发请求。</summary>
        private static void CheckDetachedGridNeverFires()
        {
            var grid = new GridSim();
            grid.AddItems(InitialItems);
            grid.CanAutoFill = false;
            for (var i = 0; i < 1000; i++)
            {
                grid.OnLoadingFalse();
            }
            Check(grid.Fires == 0, $"缓存页不该自动翻页,实际 {grid.Fires} 次");
        }

        /// <summary>
        /// 回放日志:首屏 1 次成功 → 连续 49 页成功 → 之后 900 页全部失败。
        /// 修复前每条失败都再触发一次(= 950 次请求);修复后失败页必须停在原地。
        /// </summary>
        private static void CheckStormReplay()
        {
            var guarded = Replay(true);
            var unguarded = Replay(false);

            Check(guarded == 1 + 49 + 1,
                $"有闸门时请求数应为 51(首屏 + 50 页),实际 {guarded}");
            Check(unguarded > 900,
                $"没有闸门时应当复现日志里的请求风暴,实际只有 {unguarded} 次");
            Check(guarded * 10 < unguarded,
                $"闸门必须把请求量压到十分之一以下:{guarded} vs {unguarded}");
        }

        /// <summary>翻页返回空页(列表到底)也不能循环。</summary>
        private static void CheckEmptyPageStops()
        {
            var grid = new GridSim();
            grid.AddItems(InitialItems);
            grid.OnLoadingFalse();

            grid.AddItems(0);
            for (var i = 0; i < 50; i++)
            {
                grid.OnLoadingFalse();
            }
            Check(grid.Fires == 1, $"空页之后不该再自动翻页,实际 {grid.Fires} 次");
        }

        /// <summary>按 HotPage 的实际接线回放:guarded=false 时用修复前的旧判断(不看内容是否增长)。</summary>
        private static int Replay(bool guarded)
        {
            var grid = new GridSim();
            grid.AddItems(InitialItems);

            var requests = 1;
            var successLeft = 49;
            var failingLeft = 900;

            var wantMore = guarded ? grid.OnLoadingFalse() : ForceOnce(grid);
            while (wantMore)
            {
                requests++;
                if (successLeft > 0)
                {
                    successLeft--;
                    grid.AddItems(ItemsPerPage);
                }
                else
                {
                    failingLeft--;
                }
                if (failingLeft <= 0)
                {
                    break;
                }
                wantMore = guarded ? grid.OnLoadingFalse() : ForceOnce(grid);
            }
            return requests;
        }

        /// <summary>修复前的行为:Loading 一变 false 就翻页,与有没有新内容无关。</summary>
        private static bool ForceOnce(GridSim grid)
        {
            grid.ForceLoadingFalse();
            return true;
        }

        /// <summary>把控件里的接线搬成最小状态机:Loading→false 时问闸门,内容变化时上膛。</summary>
        private sealed class GridSim
        {
            private readonly AutoLoadMoreGate gate = new AutoLoadMoreGate();

            public int Items;
            public int Fires;
            public bool CanAutoFill = true;

            public void AddItems(int count)
            {
                Items += count;
                if (count > 0)
                {
                    gate.OnContentChanged();
                }
            }

            public bool OnLoadingFalse()
            {
                if (!gate.TryTake(CanAutoFill))
                {
                    return false;
                }
                Fires++;
                return true;
            }

            /// <summary>修复前的行为:不做任何判断直接翻页。</summary>
            public void ForceLoadingFalse()
            {
                Fires++;
            }
        }

        private static void Check(bool condition, string description)
        {
            _checks++;
            if (!condition)
            {
                Failures.Add(description);
            }
            Console.WriteLine((condition ? "ok   " : "FAIL ") + description);
        }
    }
}
