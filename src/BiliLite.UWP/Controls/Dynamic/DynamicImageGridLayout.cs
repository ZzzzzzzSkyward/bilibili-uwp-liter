using System;

namespace BiliLite.Controls.Dynamic
{
    /// <summary>
    /// 多图网格的纯几何(CARD_PLAN §6)：给张数与可用内宽,算出列数、方形瓦片边长、可见张数与 +N 计数。
    /// 刻意不引用 XAML 类型,视觉宿主(DynamicImageGrid 与其 Panel)只消费结果,探针可以直接执行这一层。
    /// 规则:
    ///   · 画廊宽度封顶 480,随可用内宽收缩;
    ///   · 2 张固定一行两列,4 张固定 2×2;
    ///   · 3 张与 5-9 张在三列瓦片仍 ≥88 时走三列,否则降到两列,不给瓦片设会撑出宽度下限;
    ///   · 超过 9 张只画前九张,第九张带 +N(超出张数)角标,查看器仍拿完整原图列表;
    ///   · 瓦片边长向下取整,保证 columns*tile+(columns-1)*gap ≤ 宽度,任何宽度都不横向溢出。
    /// </summary>
    public sealed class DynamicImageGridLayout
    {
        /// <summary>画廊最大宽度。</summary>
        public const double MaxWidth = 480;
        /// <summary>瓦片间距。</summary>
        public const double Gap = 6;
        /// <summary>三列仍可用所需的最小瓦片边长。</summary>
        public const double MinThreeColumnTile = 88;
        /// <summary>最多画出来的瓦片数,多出来的用 +N 表示。</summary>
        public const int MaxVisibleTiles = 9;

        /// <summary>原始张数(含被 +N 收起的那些)。</summary>
        public int Count { get; private set; }
        /// <summary>列数;没有瓦片时为 0。</summary>
        public int Columns { get; private set; }
        /// <summary>真正画出来的瓦片数。</summary>
        public int VisibleCount { get; private set; }
        /// <summary>+N 的 N:被收起的张数,没有超出时为 0。</summary>
        public int OverflowCount { get; private set; }
        /// <summary>方形瓦片边长(向下取整)。</summary>
        public double TileSize { get; private set; }
        /// <summary>网格实际宽度,不超过 480 也不超过可用内宽。</summary>
        public double Width { get; private set; }
        /// <summary>行数。</summary>
        public int Rows { get; private set; }
        /// <summary>网格实际高度(含行间距)。</summary>
        public double Height { get; private set; }

        public bool HasOverflow { get { return OverflowCount > 0; } }

        public static DynamicImageGridLayout Create(int count, double availableWidth)
        {
            var tiles = count > 0 ? count : 0;
            var width = ResolveWidth(availableWidth);
            var visible = Math.Min(tiles, MaxVisibleTiles);
            var columns = ResolveColumns(visible, width);
            var tile = columns > 0 ? Math.Floor((width - (columns - 1) * Gap) / columns) : 0;
            if (tile <= 0 && columns > 1)
            {
                // 宽度连列间距都不够时退回单列:保证 c*tile+(c-1)*gap ≤ 宽度 对任何宽度都成立,
                // 不会出现"瓦片缩到 0、间距还在往外撑"的横向溢出。
                columns = 1;
                tile = Math.Floor(width);
            }
            if (tile < 0)
            {
                tile = 0;
            }
            var rows = columns > 0 ? (int)Math.Ceiling(visible / (double)columns) : 0;
            return new DynamicImageGridLayout
            {
                Count = tiles,
                Columns = columns,
                VisibleCount = visible,
                OverflowCount = tiles - visible,
                TileSize = tile,
                Width = width,
                Rows = rows,
                Height = rows > 0 ? rows * tile + (rows - 1) * Gap : 0,
            };
        }

        /// <summary>瓦片左边缘:列 × (边长 + 间距)。</summary>
        public double LeftOf(int column)
        {
            return column * (TileSize + Gap);
        }

        /// <summary>瓦片上边缘:行 × (边长 + 间距)。</summary>
        public double TopOf(int row)
        {
            return row * (TileSize + Gap);
        }

        private static double ResolveWidth(double availableWidth)
        {
            if (double.IsNaN(availableWidth) || double.IsInfinity(availableWidth) || availableWidth <= 0)
            {
                return MaxWidth;
            }
            return Math.Min(availableWidth, MaxWidth);
        }

        /// <summary>
        /// 2 张与 4 张的列数是排版语义(一行两张 / 2×2),不参与 88 阈值判断;
        /// 其余三张以上按三列是否还能放下 88 的瓦片决定降不降列。
        /// </summary>
        private static int ResolveColumns(int visibleCount, double width)
        {
            if (visibleCount <= 1)
            {
                return visibleCount;
            }
            if (visibleCount == 2 || visibleCount == 4)
            {
                return 2;
            }
            var threeColumnTile = Math.Floor((width - 2 * Gap) / 3);
            return threeColumnTile >= MinThreeColumnTile ? 3 : 2;
        }
    }
}
