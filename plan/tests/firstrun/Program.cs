using System;
using System.Collections.Generic;
using BiliLite.Services;

namespace FirstRunDisplayModeProbe
{
    /// <summary>
    /// 首启首页布局探针:钉住「比 16:9 更宽且至少 1920 宽才用列表」这条判据。
    /// 旧实现写成 <c>ScreenWidthInRawPixels / ScreenHeightInRawPixels &gt; 16 / 9</c>:像素宽高是 UInt32、
    /// 16/9 也按整数算,于是左侧地板除、右侧是 1,判据实际变成「宽高比 ≥ 2」——1920x1080、2560x1200、
    /// 2048x1080 这些 16:9 到 2:1 之间的屏幕都落进网格分支,高为 0 时还会除零。
    /// 这里改用整数交叉相乘,并把边界逐个钉死。
    /// </summary>
    internal static class Program
    {
        private static int _checks;
        private static readonly List<string> Failures = new List<string>();

        private static int Main()
        {
            // 16:9 正好相等的面板不算「更宽」
            Check("1920x1080 是 16:9,按网格处理", false, FirstRunDisplayMode.PrefersList(1920, 1080));
            Check("2560x1440 是 16:9,按网格处理", false, FirstRunDisplayMode.PrefersList(2560, 1440));
            Check("3840x2160 是 16:9,按网格处理", false, FirstRunDisplayMode.PrefersList(3840, 2160));

            // 更宽的屏幕(21:9、带鱼屏、16:10 的反例)
            Check("2560x1080 (21:9) 用列表", true, FirstRunDisplayMode.PrefersList(2560, 1080));
            Check("3440x1440 (21:9) 用列表", true, FirstRunDisplayMode.PrefersList(3440, 1440));
            Check("3840x1600 用列表", true, FirstRunDisplayMode.PrefersList(3840, 1600));
            Check("1921x1080 比 16:9 宽一点就用列表", true, FirstRunDisplayMode.PrefersList(1921, 1080));

            // 宽度门槛
            Check("1600x900 是 16:9 且太窄", false, FirstRunDisplayMode.PrefersList(1600, 900));
            Check("1919x900 窄于门槛", false, FirstRunDisplayMode.PrefersList(1919, 900));

            // 竖屏与退化输入:旧实现的整数除法在 1080/1920 上得 0,这里必须稳定返回 false
            Check("1080x1920 竖屏用网格", false, FirstRunDisplayMode.PrefersList(1080, 1920));
            Check("高为 0 不抛也不进列表", false, FirstRunDisplayMode.PrefersList(1920, 0));
            Check("负高不进列表", false, FirstRunDisplayMode.PrefersList(1920, -1080));
            Check("1920x2000 竖屏用网格", false, FirstRunDisplayMode.PrefersList(1920, 2000));

            // 大屏:x86 上是 uint,乘法用 long,不会在 4K/8K 上溢出
            Check("7680x2160 (超宽 8K) 用列表", true, FirstRunDisplayMode.PrefersList(7680, 2160));
            Check("7680x4320 (8K 16:9) 用网格", false, FirstRunDisplayMode.PrefersList(7680, 4320));

            if (Failures.Count == 0)
            {
                Console.WriteLine("firstrun PASS —— " + _checks + " 项检查全过");
                return 0;
            }

            Console.WriteLine("firstrun FAIL —— " + Failures.Count + " / " + _checks + " 项没过:");
            foreach (var failure in Failures)
            {
                Console.WriteLine("  - " + failure);
            }
            return 1;
        }

        private static void Check(string what, bool expected, bool actual)
        {
            _checks++;
            if (expected == actual)
            {
                Console.WriteLine("[ok] " + what);
                return;
            }
            Failures.Add(what + "（期望 " + expected + ",实际 " + actual + "）");
            Console.WriteLine("[fail] " + what + "（期望 " + expected + ",实际 " + actual + "）");
        }
    }
}
