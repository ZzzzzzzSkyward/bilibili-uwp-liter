using System;
using Windows.Foundation;
using Windows.UI.Xaml.Controls;

namespace BiliLite.Controls.Dynamic
{
    /// <summary>
    /// 网格瓦片的定位:只按 DynamicImageGridLayout 算出的方阵摆放。
    /// 瓦片尺寸由 DataTemplate 按同一份布局写到根元素上,这里只负责行列坐标与间距,
    /// 不做二次收缩,免得布局函数与实测宽度各算一套。
    /// </summary>
    public sealed class DynamicImageGridPanel : Panel
    {
        protected override Size MeasureOverride(Size availableSize)
        {
            var layout = DynamicImageGridLayout.Create(Children.Count, availableSize.Width);
            var cell = new Size(layout.TileSize, layout.TileSize);
            // 只为预算了空间的瓦片测量:超过 MaxVisibleTiles 的子项没有行列预算,量了也摆不下
            var count = Math.Min(Children.Count, layout.VisibleCount);
            for (var i = 0; i < count; i++)
            {
                Children[i].Measure(cell);
            }
            return new Size(layout.Width, layout.Height);
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            var layout = DynamicImageGridLayout.Create(Children.Count, finalSize.Width);
            var count = Math.Min(Children.Count, layout.VisibleCount);
            for (var i = 0; i < count; i++)
            {
                var row = i / layout.Columns;
                var column = i % layout.Columns;
                Children[i].Arrange(new Rect(
                    layout.LeftOf(column),
                    layout.TopOf(row),
                    layout.TileSize,
                    layout.TileSize));
            }
            return new Size(layout.Width, layout.Height);
        }
    }
}
