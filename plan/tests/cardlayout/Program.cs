using System;
using BiliLite.Controls.Dynamic;

namespace CardLayoutProbe
{
    /// <summary>
    /// 多图网格几何探针(CARD_PLAN §6)。被测对象是 DynamicImageGridLayout.Create：
    /// 张数 2/3/4/5/9/12 × 宽度 320/400/480/800、三列的 88px 阈值、九张上限与 +N 计数、瓦片不横向溢出。
    /// 同一批断言可以用 --baseline 跑一遍旧模板的固定瓦片几何(100/120px、MaxWidth 400/300、
    /// 四张以外一律三列、不收起超出的图)，用来证明这些断言对改动前实现是失败的。
    /// </summary>
    internal static class Program
    {
        private static int _checks;
        private static int _failures;

        private static int Main(string[] args)
        {
            var baseline = args.Length > 0 && (args[0] == "--baseline" || args[0] == "--legacy");
            Console.WriteLine(baseline
                ? "mode=baseline (旧模板: 固定 100/120 瓦片, MaxWidth 400/300, 不收起超出张数)"
                : "mode=current (DynamicImageGridLayout)");

            RunCases(baseline);
            RunThreshold(baseline);
            RunCap(baseline);
            RunNoOverflow(baseline);

            Console.WriteLine("CHECKS=" + _checks);
            Console.WriteLine("TOTAL_FAILURES=" + _failures);
            return _failures == 0 ? 0 : 1;
        }

        /// <summary>每个用例固定写期望的列数与瓦片边长，不是拿被测实现自己算出来的值回比。</summary>
        private static void RunCases(bool baseline)
        {
            Measure measure = baseline ? (Measure)Baseline : Current;

            // 2 张:一行两列,宽度封顶 480
            ExpectCase(measure(2, 320), 2, 2, 157, "2@320");
            ExpectCase(measure(2, 400), 2, 2, 197, "2@400");
            ExpectCase(measure(2, 480), 2, 2, 237, "2@480");
            ExpectCase(measure(2, 800), 2, 2, 237, "2@800");

            // 3 张:三列(320 时每格 102 ≥ 88)
            ExpectCase(measure(3, 320), 3, 3, 102, "3@320");
            ExpectCase(measure(3, 400), 3, 3, 129, "3@400");
            ExpectCase(measure(3, 480), 3, 3, 156, "3@480");
            ExpectCase(measure(3, 800), 3, 3, 156, "3@800");

            // 4 张:固定 2×2
            ExpectCase(measure(4, 320), 2, 4, 157, "4@320");
            ExpectCase(measure(4, 400), 2, 4, 197, "4@400");
            ExpectCase(measure(4, 480), 2, 4, 237, "4@480");
            ExpectCase(measure(4, 800), 2, 4, 237, "4@800");

            // 5 与 9 张:三列,行数分别是 2 / 3
            ExpectCase(measure(5, 320), 3, 5, 102, "5@320");
            ExpectCase(measure(5, 400), 3, 5, 129, "5@400");
            ExpectCase(measure(5, 480), 3, 5, 156, "5@480");
            ExpectCase(measure(5, 800), 3, 5, 156, "5@800");
            ExpectCase(measure(9, 320), 3, 9, 102, "9@320");
            ExpectCase(measure(9, 400), 3, 9, 129, "9@400");
            ExpectCase(measure(9, 480), 3, 9, 156, "9@480");
            ExpectCase(measure(9, 800), 3, 9, 156, "9@800");

            // 12 张:同 9 张的格子,多出来的 3 张只进 +N
            ExpectCase(measure(12, 320), 3, 9, 102, "12@320");
            ExpectCase(measure(12, 400), 3, 9, 129, "12@400");
            ExpectCase(measure(12, 480), 3, 9, 156, "12@480");
            ExpectCase(measure(12, 800), 3, 9, 156, "12@800");

            var five = measure(5, 320);
            Check("5@320 rows=2", five.Rows == 2, "rows=" + five.Rows);
            var nine = measure(9, 320);
            Check("9@320 rows=3", nine.Rows == 3, "rows=" + nine.Rows);
            Check("9@320 height=318", nine.Height == 318, "height=" + nine.Height);

            var one = measure(1, 320);
            Check("1@320 single column", one.Columns == 1 && one.Visible == 1 && one.Overflow == 0,
                "columns=" + one.Columns + " visible=" + one.Visible + " overflow=" + one.Overflow);

            var none = measure(0, 320);
            Check("0@320 empty", none.Columns == 0 && none.Visible == 0 && none.Height == 0,
                "columns=" + none.Columns + " visible=" + none.Visible + " height=" + none.Height);

            // 窄到放不下两个像样的瓦片也不强设下限:整数瓦片仍然算得出来,也不会横向溢出
            var narrow = measure(2, 100);
            Check("2@100 no forced minimum", narrow.Columns == 2 && narrow.Tile == 47,
                "columns=" + narrow.Columns + " tile=" + narrow.Tile);
        }

        /// <summary>三列阈值:每格 ≥88 才用三列。276 是三列刚好 88 的最小宽度,275 必须降成两列。</summary>
        private static void RunThreshold(bool baseline)
        {
            Measure measure = baseline ? (Measure)Baseline : Current;

            var at276 = measure(3, 276);
            Check("3@276 three columns", at276.Columns == 3, "columns=" + at276.Columns);
            Check("3@276 tile=88", at276.Tile == 88, "tile=" + at276.Tile);

            var at275 = measure(3, 275);
            Check("3@275 falls back to two columns", at275.Columns == 2, "columns=" + at275.Columns);

            var fiveAt275 = measure(5, 275);
            Check("5@275 falls back to two columns", fiveAt275.Columns == 2, "columns=" + fiveAt275.Columns);

            var nineAt276 = measure(9, 276);
            Check("9@276 three columns", nineAt276.Columns == 3, "columns=" + nineAt276.Columns);

            var nineAt275 = measure(9, 275);
            Check("9@275 two columns", nineAt275.Columns == 2, "columns=" + nineAt275.Columns);

            // 4 张是排版语义(2×2),不参与 88 阈值
            var fourAt275 = measure(4, 275);
            Check("4@275 stays 2x2", fourAt275.Columns == 2, "columns=" + fourAt275.Columns);
        }

        /// <summary>九张上限与 +N 计数。</summary>
        private static void RunCap(bool baseline)
        {
            Measure measure = baseline ? (Measure)Baseline : Current;

            var nine = measure(9, 480);
            Check("9 visible=9 overflow=0", nine.Visible == 9 && nine.Overflow == 0,
                "visible=" + nine.Visible + " overflow=" + nine.Overflow);

            var ten = measure(10, 480);
            Check("10 visible=9 overflow=1", ten.Visible == 9 && ten.Overflow == 1,
                "visible=" + ten.Visible + " overflow=" + ten.Overflow);

            var twelve = measure(12, 320);
            Check("12 visible=9 overflow=3", twelve.Visible == 9 && twelve.Overflow == 3,
                "visible=" + twelve.Visible + " overflow=" + twelve.Overflow);
            Check("12 columns follow nine", twelve.Columns == 3, "columns=" + twelve.Columns);
        }

        /// <summary>任何张数/宽度下:columns*tile+(columns-1)*gap ≤ 网格宽度。</summary>
        private static void RunNoOverflow(bool baseline)
        {
            Measure measure = baseline ? (Measure)Baseline : Current;

            var widths = new[] { 5d, 8d, 12d, 20d, 100d, 200d, 240d, 275d, 276d, 320d, 400d, 480d, 640d, 800d };
            for (var count = 0; count <= 15; count++)
            {
                foreach (var width in widths)
                {
                    var g = measure(count, width);
                    var used = g.Columns * g.Tile + Math.Max(0, g.Columns - 1) * DynamicImageGridLayout.Gap;
                    Check("no overflow " + count + "@" + width, used <= g.Width + 0.0001,
                        "used=" + used + " width=" + g.Width);
                    Check("width capped " + count + "@" + width, g.Width <= DynamicImageGridLayout.MaxWidth,
                        "width=" + g.Width);
                    Check("tile positive " + count + "@" + width, count == 0 || g.Tile > 0, "tile=" + g.Tile);
                }
            }
        }

        private static void ExpectCase(Geometry g, int columns, int visible, int tile, string label)
        {
            Check(label + " columns=" + columns, g.Columns == columns, "columns=" + g.Columns);
            Check(label + " visible=" + visible, g.Visible == visible, "visible=" + g.Visible);
            Check(label + " tile=" + tile, g.Tile == tile, "tile=" + g.Tile);
        }

        private static void Check(string name, bool ok, string detail)
        {
            _checks++;
            if (ok)
            {
                return;
            }
            _failures++;
            Console.WriteLine("FAIL " + name + " :: " + detail);
        }

        private static Geometry Current(int count, double width)
        {
            var layout = DynamicImageGridLayout.Create(count, width);
            return new Geometry
            {
                Columns = layout.Columns,
                Visible = layout.VisibleCount,
                Overflow = layout.OverflowCount,
                Tile = (int)layout.TileSize,
                Width = (int)layout.Width,
                Rows = layout.Rows,
                Height = (int)layout.Height,
            };
        }

        /// <summary>
        /// 改动前的几何(从 DynamicTemplate.xaml 的 Photo3x3/Photo2x2 抄下来):
        /// 四张 120px 瓦片装在 MaxWidth 300 的 GridView,其余 100px 瓦片装在 MaxWidth 400 的 GridView,
        /// 列数由 GridView 自己换行决定(这里按 3 列),不收起超过九张的图。
        /// </summary>
        private static Geometry Baseline(int count, double width)
        {
            var isFour = count == 4;
            var tile = isFour ? 120 : 100;
            var gridWidth = isFour ? 300 : 400;
            var columns = count <= 1 ? Math.Max(count, 0) : isFour ? 2 : 3;
            var rows = columns == 0 ? 0 : (int)Math.Ceiling(count / (double)columns);
            return new Geometry
            {
                Columns = columns,
                Visible = count,
                Overflow = 0,
                Tile = tile,
                Width = gridWidth,
                Rows = rows,
                Height = rows == 0 ? 0 : rows * tile,
            };
        }

        private delegate Geometry Measure(int count, double width);

        private struct Geometry
        {
            public int Columns;
            public int Visible;
            public int Overflow;
            public int Tile;
            public int Width;
            public int Rows;
            public int Height;
        }
    }
}
