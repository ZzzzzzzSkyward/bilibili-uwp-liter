using BiliLite.Converters;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace BiliLite.Controls.Dynamic
{
    /// <summary>
    /// 动态多图网格(CARD_PLAN §6)。张数规则全部来自 DynamicImageGridLayout,这里只做三件事:
    /// 把 ImagesInfo 摊成瓦片(顺序不变、超过九张收起并用 +N 标在第九张)、按当前宽度把边长写进瓦片、
    /// 给瓦片绑上既有的查看/保存命令。单图与无图仍走 Photo1x1 / Text 模板,不进这个控件。
    /// </summary>
    public sealed partial class DynamicImageGrid : UserControl
    {
        /// <summary>瓦片边长最大 237;400 的缩略图参数已覆盖 ~1.7 倍缩放,不在这里解原图。</summary>
        private const string ThumbnailParameter = "400w_400h_1c";

        private static readonly ImageCompressionConvert ThumbnailConverter = new ImageCompressionConvert();

        private List<DynamicImageGridTile> _tiles = new List<DynamicImageGridTile>();

        public DynamicImageGrid()
        {
            this.InitializeComponent();
            this.SizeChanged += OnGridSizeChanged;
        }

        private void OnGridSizeChanged(object sender, SizeChangedEventArgs e)
        {
            ApplyTileSize(e.NewSize.Width);
        }

        /// <summary>
        /// 图片列表:ImagesInfo。空项与无地址的项直接跳过,但不去改它们自己的 Index/AllImages,
        /// 所以点开时查看器拿到的仍是完整原图列表和这张图在原始顺序里的位置。
        /// </summary>
        public object ItemsSource
        {
            get { return (object)GetValue(ItemsSourceProperty); }
            set { SetValue(ItemsSourceProperty, value); }
        }

        public static readonly DependencyProperty ItemsSourceProperty =
            DependencyProperty.Register("ItemsSource", typeof(object), typeof(DynamicImageGrid), new PropertyMetadata(null, OnItemsSourceChanged));

        private static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((DynamicImageGrid)d).Rebuild();
        }

        private void Rebuild()
        {
            var tiles = new List<DynamicImageGridTile>();
            var items = ItemsSource as IEnumerable;
            if (items != null)
            {
                foreach (var entry in items)
                {
                    var image = entry as DyanmicItemDisplayImageInfo;
                    if (image == null || string.IsNullOrEmpty(image.ImageUrl))
                    {
                        continue;
                    }
                    tiles.Add(new DynamicImageGridTile
                    {
                        Image = image,
                        ImageUrl = image.ImageUrl,
                        ThumbUrl = ThumbnailConverter.Convert(image.ImageUrl, typeof(string), ThumbnailParameter, null) as string,
                        AccessibilityName = "查看第 " + (tiles.Count + 1) + " 张图片",
                    });
                }
            }

            var layout = DynamicImageGridLayout.Create(tiles.Count, ResolveWidth());
            var lastVisible = Math.Min(tiles.Count, DynamicImageGridLayout.MaxVisibleTiles) - 1;
            if (lastVisible >= 0 && layout.HasOverflow)
            {
                tiles[lastVisible].OverflowText = "+" + layout.OverflowCount;
                tiles[lastVisible].OverflowVisibility = Visibility.Visible;
                tiles[lastVisible].AccessibilityName = "还有 " + layout.OverflowCount + " 张图片,查看第 " + (lastVisible + 1) + " 张";
            }

            _tiles = tiles;
            Tiles.ItemsSource = _tiles;
            ApplyTileSize(layout);
        }

        private void ApplyTileSize(double width)
        {
            ApplyTileSize(DynamicImageGridLayout.Create(_tiles.Count, width));
        }

        /// <summary>
        /// 边长写进瓦片模型而不是模板:ContentPresenter 默认左上对齐,根元素自己带尺寸才能保证
        /// 方格先占位、行列位置与 DynamicImageGridPanel 的摆放一致。
        /// </summary>
        private void ApplyTileSize(DynamicImageGridLayout layout)
        {
            for (var i = 0; i < _tiles.Count; i++)
            {
                _tiles[i].TileSize = layout.TileSize;
            }
        }

        private double ResolveWidth()
        {
            var width = ActualWidth;
            return double.IsNaN(width) || width <= 0 ? DynamicImageGridLayout.MaxWidth : width;
        }
    }

    /// <summary>
    /// 一张瓦片的绑定载体:缩略图地址、原图地址(保存用)、命令参数(查看器要的整套图片信息)
    /// 与第九张的 +N 角标。TileSize 需要在宽度变化时更新,所以带一次属性通知。
    /// </summary>
    public sealed class DynamicImageGridTile : INotifyPropertyChanged
    {
        private double _tileSize;

        public DyanmicItemDisplayImageInfo Image { get; set; }
        public string ImageUrl { get; set; }
        public string ThumbUrl { get; set; }
        public string AccessibilityName { get; set; }
        public string OverflowText { get; set; } = "";
        public Visibility OverflowVisibility { get; set; } = Visibility.Collapsed;

        public double TileSize
        {
            get { return _tileSize; }
            set
            {
                if (_tileSize == value)
                {
                    return;
                }
                _tileSize = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TileSize)));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
