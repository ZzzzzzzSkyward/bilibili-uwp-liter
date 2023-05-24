﻿using System;
using System.Windows.Input;

using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

namespace BiliLite.Controls
{
    public class IntEventArgs : EventArgs
    {
        public int Value { get; private set; }

        public IntEventArgs(int value)
        {
            this.Value = value;
        }
    }

    internal partial class CarouselPanel
    {
        public event EventHandler<IntEventArgs> SelectedIndexChanged;

        #region ItemTemplate

        public DataTemplate ItemTemplate
        {
            get { return (DataTemplate)GetValue(ItemTemplateProperty); }
            set { SetValue(ItemTemplateProperty, value); }
        }

        private static void ItemTemplateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = d as CarouselPanel;
            control.InvalidateMeasure();
        }

        public static readonly DependencyProperty ItemTemplateProperty = DependencyProperty.Register("ItemTemplate", typeof(DataTemplate), typeof(CarouselPanel), new PropertyMetadata(null, ItemTemplateChanged));

        #endregion ItemTemplate

        #region ItemWidth

        public double ItemWidth
        {
            get { return (double)GetValue(ItemWidthProperty); }
            set { SetValue(ItemWidthProperty, value); }
        }

        private static void ItemWidthChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = d as CarouselPanel;
            control.InvalidateMeasure();
        }

        public static readonly DependencyProperty ItemWidthProperty = DependencyProperty.Register("ItemWidth", typeof(double), typeof(CarouselPanel), new PropertyMetadata(400.0, ItemWidthChanged));

        #endregion ItemWidth

        #region ItemHeight

        public double ItemHeight
        {
            get { return (double)GetValue(ItemHeightProperty); }
            set { SetValue(ItemHeightProperty, value); }
        }

        private static void ItemHeightChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = d as CarouselPanel;
            control.InvalidateMeasure();
        }

        public static readonly DependencyProperty ItemHeightProperty = DependencyProperty.Register("ItemHeight", typeof(double), typeof(CarouselPanel), new PropertyMetadata(300.0, ItemHeightChanged));

        #endregion ItemHeight

        #region ItemClickCommand

        public ICommand ItemClickCommand
        {
            get { return (ICommand)GetValue(ItemClickCommandProperty); }
            set { SetValue(ItemClickCommandProperty, value); }
        }

        public static readonly DependencyProperty ItemClickCommandProperty = DependencyProperty.Register("ItemClickCommand", typeof(ICommand), typeof(CarouselPanel), new PropertyMetadata(null));

        #endregion ItemClickCommand

        private void OnPaneTapped(object sender, TappedRoutedEventArgs e)
        {
            var contentControl = sender as ContentControl;
            if (contentControl != null)
            {
                if (SelectedIndexChanged != null)
                {
                    if (contentControl.Tag != null)
                    {
                        SelectedIndexChanged(this, new IntEventArgs((int)contentControl.Tag));
                    }
                }

                if (ItemClickCommand != null)
                {
                    if (ItemClickCommand.CanExecute(contentControl.Content))
                    {
                        ItemClickCommand.Execute(contentControl.Content);
                    }
                }
            }
        }
    }
}