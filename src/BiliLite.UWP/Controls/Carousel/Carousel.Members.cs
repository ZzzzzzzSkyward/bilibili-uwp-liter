﻿using System;
using System.Windows.Input;
using Windows.Foundation.Metadata;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;

namespace BiliLite.Controls
{
    internal partial class Carousel
    {
        private DispatcherTimer _slideTimer = null;

        #region ItemsSource

        public object ItemsSource
        {
            get { return (object)GetValue(ItemsSourceProperty); }
            set { SetValue(ItemsSourceProperty, value); }
        }

        public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty.Register("ItemsSource", typeof(object), typeof(Carousel), new PropertyMetadata(null));

        #endregion ItemsSource

        #region SelectedIndex

        public int SelectedIndex
        {
            get { return (int)GetValue(SelectedIndexProperty); }
            set
            {
                // NOTE: Avoid external exceptions when this property is binded
                try
                {
                    SetValue(SelectedIndexProperty, value);
                }
                catch { }
            }
        }

        private static void SelectedIndexChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = d as Carousel;
            control.SetIndex((int)e.NewValue);
        }

        private void SetIndex(int index)
        {
            int itemCount = _panel.Items.Count;
            index = index.Mod(itemCount);
            if (index != this.Index.Mod(itemCount))
            {
                this.Index = index;
                this.Position = -index * this.ItemWidth;
            }
        }

        public static readonly DependencyProperty SelectedIndexProperty = DependencyProperty.Register("SelectedIndex", typeof(int), typeof(Carousel), new PropertyMetadata(-1, SelectedIndexChanged));

        #endregion SelectedIndex

        #region Index

        public int Index
        {
            get { return (int)GetValue(IndexProperty); }
            set { SetValue(IndexProperty, value); }
        }

        private static void IndexChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = d as Carousel;
            control.SetIndexInternal((int)e.NewValue);
        }

        private void SetIndexInternal(int index)
        {
            int itemCount = _panel.Items.Count;
            this.SelectedIndex = index.Mod(itemCount);
        }

        public static readonly DependencyProperty IndexProperty = DependencyProperty.Register("Index", typeof(int), typeof(Carousel), new PropertyMetadata(0, IndexChanged));

        #endregion Index

        #region ContentTemplate

        public DataTemplate ContentTemplate
        {
            get { return (DataTemplate)GetValue(ContentTemplateProperty); }
            set { SetValue(ContentTemplateProperty, value); }
        }

        public static readonly DependencyProperty ContentTemplateProperty = DependencyProperty.Register("ContentTemplate", typeof(DataTemplate), typeof(Carousel), new PropertyMetadata(null));

        #endregion ContentTemplate

        #region MaxItems

        public int MaxItems
        {
            get { return (int)GetValue(MaxItemsProperty); }
            set { SetValue(MaxItemsProperty, value); }
        }

        private static void MaxItemsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = d as Carousel;
            control.InvalidateMeasure();
        }

        public static readonly DependencyProperty MaxItemsProperty = DependencyProperty.Register("MaxItems", typeof(int), typeof(Carousel), new PropertyMetadata(3, MaxItemsChanged));

        #endregion MaxItems

        #region AspectRatio

        public double AspectRatio
        {
            get { return (double)GetValue(AspectRatioProperty); }
            set { SetValue(AspectRatioProperty, value); }
        }

        private static void AspectRatioChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = d as Carousel;
            control.InvalidateMeasure();
        }

        public static readonly DependencyProperty AspectRatioProperty = DependencyProperty.Register("AspectRatio", typeof(double), typeof(Carousel), new PropertyMetadata(1.6, AspectRatioChanged));

        #endregion AspectRatio

        #region GradientOpacity

        public double GradientOpacity
        {
            get { return (double)GetValue(GradientOpacityProperty); }
            set { SetValue(GradientOpacityProperty, value); }
        }

        public static readonly DependencyProperty GradientOpacityProperty = DependencyProperty.Register("GradientOpacity", typeof(double), typeof(Carousel), new PropertyMetadata(0.0));

        #endregion GradientOpacity

        #region ArrowsVisibility

        public Visibility ArrowsVisibility
        {
            get { return (Visibility)GetValue(ArrowsVisibilityProperty); }
            set { SetValue(ArrowsVisibilityProperty, value); }
        }

        public static readonly DependencyProperty ArrowsVisibilityProperty = DependencyProperty.Register("ArrowsVisibility", typeof(Visibility), typeof(Carousel), new PropertyMetadata(Visibility.Visible));

        #endregion ArrowsVisibility

        #region ItemClickCommand

        public ICommand ItemClickCommand
        {
            get { return (ICommand)GetValue(ItemClickCommandProperty); }
            set { SetValue(ItemClickCommandProperty, value); }
        }

        public static readonly DependencyProperty ItemClickCommandProperty = DependencyProperty.Register("ItemClickCommand", typeof(ICommand), typeof(Carousel), new PropertyMetadata(null));

        #endregion ItemClickCommand

        #region SlideInterval

        public double SlideInterval
        {
            get { return (double)GetValue(SlideIntervalProperty); }
            set { SetValue(SlideIntervalProperty, value); }
        }

        private static void SlideIntervalChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = d as Carousel;
            control.SetSlideInterval((double)e.NewValue);
        }

        public static readonly DependencyProperty SlideIntervalProperty = DependencyProperty.Register("SlideInterval", typeof(double), typeof(Carousel), new PropertyMetadata(0.0, SlideIntervalChanged));

        #endregion SlideInterval

        private void SetSlideInterval(double milliseconds)
        {
            if (milliseconds > 150.0)
            {
                if (_slideTimer == null)
                {
                    _slideTimer = new DispatcherTimer();
                    _slideTimer.Tick += OnSlideTimerTick;
                }
                _slideTimer.Interval = TimeSpan.FromMilliseconds(milliseconds);
                _slideTimer.Start();
            }
            else
            {
                if (_slideTimer != null)
                {
                    _slideTimer.Stop();
                }
            }
        }

        private void OnSlideTimerTick(object sender, object e)
        {
            if (!_isBusy)
            {
                this.AnimateNext();
            }
        }

        public double ItemWidth
        {
            get { return _panel.ItemWidth; }
        }

        // Obsolete

        #region AlignmentX

        [Deprecated("AligmentX property will be removed in future versions.", DeprecationType.Deprecate, 65536)]
        public AlignmentX AlignmentX
        {
            get { return (AlignmentX)GetValue(AlignmentXProperty); }
            set { SetValue(AlignmentXProperty, value); }
        }

        public static readonly DependencyProperty AlignmentXProperty = DependencyProperty.Register("AlignmentX", typeof(AlignmentX), typeof(Carousel), new PropertyMetadata(AlignmentX.Left));

        #endregion AlignmentX
    }
}