using System;
using BiliLite.Services;

namespace OVProbe
{
    /// <summary>
    /// OV: the player overlay's observable rule — a 0-1 volume renders as an integer
    /// percentage. It lives in PlayerToastOverlay, which has no XAML dependency.
    /// </summary>
    public static class Program
    {
        private static int _passed;

        public static int Main()
        {
            VolumeRendersAsIntegerPercentage();
            VolumeClampsAtBothEnds();
            VolumeRoundsHalfAwayFromZero();
            VolumeTextHasNoFraction();
            SliderTooltipUsesPercent();

            Console.WriteLine("PASS " + _passed + " assertions");
            return 0;
        }

        private static void VolumeRendersAsIntegerPercentage()
        {
            AssertEqual("音量:0%", PlayerToastOverlay.FormatVolume(0), "volume 0");
            AssertEqual("音量:50%", PlayerToastOverlay.FormatVolume(0.5), "volume 0.5");
            AssertEqual("音量:70%", PlayerToastOverlay.FormatVolume(0.7), "volume 0.7");
            AssertEqual("音量:100%", PlayerToastOverlay.FormatVolume(1), "volume 1");

            // 0.1 步进的键盘音量会累积二进制误差，仍必须是整数百分比
            var drifted = 1.0 - 0.1;
            AssertEqual("音量:90%", PlayerToastOverlay.FormatVolume(drifted), "volume with binary drift");
        }

        private static void VolumeClampsAtBothEnds()
        {
            AssertEqual("音量:100%", PlayerToastOverlay.FormatVolume(1.5), "volume above range");
            AssertEqual("音量:0%", PlayerToastOverlay.FormatVolume(-0.2), "volume below range");
            AssertEqual("音量:0%", PlayerToastOverlay.FormatVolume(double.NaN), "volume NaN");
        }

        private static void VolumeRoundsHalfAwayFromZero()
        {
            // 0.125 / 0.375 是精确的二进制小数，乘 100 后正好落在 .5 上
            AssertEqual("音量:13%", PlayerToastOverlay.FormatVolume(0.125), "half rounds up");
            AssertEqual("音量:38%", PlayerToastOverlay.FormatVolume(0.375), "half rounds up again");
            AssertEqual("音量:62%", PlayerToastOverlay.FormatVolume(0.6249), "just below half rounds down");
        }

        private static void VolumeTextHasNoFraction()
        {
            var text = PlayerToastOverlay.FormatVolume(0.753);
            Assert(text.EndsWith("%", StringComparison.Ordinal), "volume text ends with %: " + text);
            Assert(text.StartsWith("音量:", StringComparison.Ordinal), "volume text keeps the 音量 prefix: " + text);
            Assert(text.IndexOf('.') < 0, "volume text has no decimal separator: " + text);
            AssertEqual("音量:75%", text, "volume 0.753");
        }



        private static void SliderTooltipUsesPercent()
        {
            // 音量滑块的气泡走同一个 0-1 → 整数百分比规则，但不带“音量:”前缀
            AssertEqual("0%", PlayerToastOverlay.FormatPercent(0), "slider 0");
            AssertEqual("50%", PlayerToastOverlay.FormatPercent(0.5), "slider 0.5");
            AssertEqual("100%", PlayerToastOverlay.FormatPercent(1), "slider 1");
            AssertEqual("100%", PlayerToastOverlay.FormatPercent(1.5), "slider above range");
            AssertEqual("13%", PlayerToastOverlay.FormatPercent(0.125), "slider half rounds up");
        }

        private static void Assert(bool condition, string what)
        {
            if (!condition)
            {
                Console.Error.WriteLine("FAIL " + what);
                Environment.Exit(1);
            }
            _passed++;
        }

        private static void AssertEqual<T>(T expected, T actual, string what)
        {
            if (!Equals(expected, actual))
            {
                Console.Error.WriteLine("FAIL " + what + ": expected <" + expected + "> got <" + actual + ">");
                Environment.Exit(1);
            }
            _passed++;
        }
    }
}
