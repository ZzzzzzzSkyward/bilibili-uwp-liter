using System;
using Windows.System;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Automation;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using BiliLite.Models.Common;

namespace BiliLite.Controls
{
    /// <summary>
    /// 播放目录的唯一一套呈现:页头、分段页签和行模板都在这里。
    /// 它只显示模型、把点击/回车翻译成事件,自己不取页、不换集、不动播放。
    /// </summary>
    public sealed partial class PlayDirectoryControl : UserControl
    {
        private PlayDirectoryModel m_model;
        private PlayDirectorySectionKind m_activeKind;
        private bool m_scopeSet;

        public PlayDirectoryControl()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// 激活一行;分段和行的身份都在事件里,由宿主决定播什么
        /// </summary>
        public event EventHandler<PlayDirectoryRowActivatedEventArgs> RowActivated;

        /// <summary>
        /// 换了浏览的分段。只是浏览,不改播放,也不改连播来源。
        /// </summary>
        public event EventHandler<PlayDirectorySourceEventArgs> ScopeChanged;

        /// <summary>
        /// 用户要按某个外层来源连播;宿主负责确认该来源包含当前视频
        /// </summary>
        public event EventHandler<PlayDirectorySourceEventArgs> ContinuationSourceRequested;

        public event EventHandler CloseRequested;

        public PlayDirectorySectionKind ActiveKind
        {
            get { return m_activeKind; }
        }

        /// <summary>
        /// 换一份目录。分段顺序由模型决定,页签只为有内容的分段生成。
        /// 重画时留着用户正在浏览的分段:换集、自动连播都不该把人从别的页签拽走;
        /// 只有新开目录或那一段没了才回到连播来源。
        /// </summary>
        public void SetModel(PlayDirectoryModel model)
        {
            var resetScope = model != null && model.IsFreshOpen || !m_scopeSet;
            var browsed = resetScope ? (PlayDirectorySectionKind?)null : m_activeKind;
            m_model = model;
            HeaderText.Text = model == null ? "" : model.HeaderText ?? "";
            SourceTitleText.Text = model == null ? "" : model.SourceTitle ?? "";
            SetPosition(model);

            BuildTabs(model);
            SelectScope(PlayDirectoryBuilder.ResolveBrowsedSection(model, browsed), false);
            UpdateSourceActions(model);
        }

        /// <summary>
        /// 把键盘焦点放到行列表:方向键浏览、回车激活都从这里开始
        /// </summary>
        public void FocusRows()
        {
            RowList.Focus(FocusState.Programmatic);
        }

        /// <summary>
        /// 把正在播放的一行滚进视野。先找用户正在浏览的分段,这一段没有当前行才退到连播来源;
        /// 排在前面的分段(分P)不会因为也有当前行就把浏览位置抢走。
        /// </summary>
        public void LocateCurrent()
        {
            var kind = PlayDirectoryBuilder.LocateSection(m_model, m_activeKind);
            var row = PlayDirectoryBuilder.CurrentRowOf(m_model, kind);
            if (row == null) return;
            if (kind != m_activeKind) SelectScope(kind, true);
            RowList.SelectedItem = row;
            RowList.ScrollIntoView(row, ScrollIntoViewAlignment.Leading);
        }

        /// <summary>
        /// 位置和数量各占一个块:其中一个为空时另一个自己站着,不留分隔符
        /// </summary>
        private void SetPosition(PlayDirectoryModel model)
        {
            var position = model == null ? "" : model.CurrentPositionText ?? "";
            PositionText.Text = position;
            PositionCountText.Text = model == null ? "" : model.CountText ?? "";
            PositionCountText.Margin = new Thickness(string.IsNullOrEmpty(position) ? 0 : 8, 0, 0, 0);
        }

        private void BuildTabs(PlayDirectoryModel model)
        {
            TabHost.Children.Clear();
            if (model == null || model.Sections == null) return;
            foreach (var section in model.Sections)
            {
                if (section == null) continue;
                var tab = new Button()
                {
                    Tag = section.Kind,
                    Style = (Style)Resources["DirectoryTabStyle"],
                };
                tab.Content = section.Kind == model.ActiveKind
                    ? (object)ContinuationTabContent(section.Title ?? "", tab)
                    : section.Title ?? "";
                tab.Click += Tab_Click;
                TabHost.Children.Add(tab);
            }
        }

        /// <summary>
        /// 连播来源那一段的页签:前面一个圆点,状态不再用字写一遍
        /// </summary>
        private UIElement ContinuationTabContent(string title, Button tab)
        {
            var host = new StackPanel() { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            var dot = new TextBlock()
            {
                Text = "●",
                FontSize = 10,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 6, 0),
                Style = (Style)Resources["DirectoryDotStyle"],
            };
            AutomationProperties.SetName(dot, "连播中");
            host.Children.Add(dot);
            var label = new TextBlock() { Text = title, VerticalAlignment = VerticalAlignment.Center };
            // 页签的选中样式改的是按钮的 Foreground,标题跟着它走
            label.SetBinding(TextBlock.ForegroundProperty, new Binding() { Source = tab, Path = new PropertyPath("Foreground") });
            host.Children.Add(label);
            return host;
        }

        private void UpdateSourceActions(PlayDirectoryModel model)
        {
            var bothSources = model != null
                && SectionOf(PlayDirectorySectionKind.UgcSeason) != null
                && SectionOf(PlayDirectorySectionKind.Playlist) != null;
            PlayCollectionButton.Visibility = bothSources && model.ActiveKind != PlayDirectorySectionKind.UgcSeason
                ? Visibility.Visible
                : Visibility.Collapsed;
            PlayPlaylistButton.Visibility = bothSources && model.ActiveKind != PlayDirectorySectionKind.Playlist
                ? Visibility.Visible
                : Visibility.Collapsed;
            SourceActionHost.Visibility = bothSources ? Visibility.Visible : Visibility.Collapsed;
        }

        private void SelectScope(PlayDirectorySectionKind kind, bool notify)
        {
            var section = SectionOf(kind);
            if (section == null) return;
            m_activeKind = section.Kind;
            m_scopeSet = true;
            RowList.ItemsSource = section.Rows;
            UpdateTabStyles();
            if (notify) ScopeChanged?.Invoke(this, new PlayDirectorySourceEventArgs(section.Kind));
        }

        private void UpdateTabStyles()
        {
            foreach (var child in TabHost.Children)
            {
                var tab = child as Button;
                if (tab == null) continue;
                var isActive = tab.Tag is PlayDirectorySectionKind && (PlayDirectorySectionKind)tab.Tag == m_activeKind;
                tab.Style = (Style)Resources[isActive ? "DirectoryTabActiveStyle" : "DirectoryTabStyle"];
            }
        }

        private PlayDirectorySection SectionOf(PlayDirectorySectionKind kind)
        {
            if (m_model == null || m_model.Sections == null) return null;
            foreach (var section in m_model.Sections)
            {
                if (section != null && section.Kind == kind) return section;
            }
            return null;
        }

        private void Activate(PlayDirectoryRow row)
        {
            if (row == null) return;
            var handler = RowActivated;
            if (handler != null) handler(this, new PlayDirectoryRowActivatedEventArgs(m_activeKind, row));
        }

        private void Tab_Click(object sender, RoutedEventArgs e)
        {
            var tab = sender as Button;
            if (tab == null || !(tab.Tag is PlayDirectorySectionKind)) return;
            SelectScope((PlayDirectorySectionKind)tab.Tag, true);
        }

        private void RowList_ItemClick(object sender, ItemClickEventArgs e)
        {
            Activate(e.ClickedItem as PlayDirectoryRow);
        }

        private void RowList_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key != VirtualKey.Enter && e.Key != VirtualKey.Space) return;
            var item = e.OriginalSource as ListViewItem;
            var row = item == null ? RowList.SelectedItem as PlayDirectoryRow : item.Content as PlayDirectoryRow;
            if (row == null) return;
            // 回车/空格是激活;方向键只移动焦点,由列表自己处理
            e.Handled = true;
            Activate(row);
        }

        private void LocateButton_Click(object sender, RoutedEventArgs e)
        {
            LocateCurrent();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            var handler = CloseRequested;
            if (handler != null) handler(this, EventArgs.Empty);
        }

        private void PlayCollectionButton_Click(object sender, RoutedEventArgs e)
        {
            var handler = ContinuationSourceRequested;
            if (handler != null) handler(this, new PlayDirectorySourceEventArgs(PlayDirectorySectionKind.UgcSeason));
        }

        private void PlayPlaylistButton_Click(object sender, RoutedEventArgs e)
        {
            var handler = ContinuationSourceRequested;
            if (handler != null) handler(this, new PlayDirectorySourceEventArgs(PlayDirectorySectionKind.Playlist));
        }

        // 抽屉里的点击/拖动不再交给播放器的手势识别:滑动列表不该改进度或切全屏
        private void Root_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            e.Handled = true;
        }

        private void Root_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            e.Handled = true;
        }

        private void Root_Tapped(object sender, TappedRoutedEventArgs e)
        {
            e.Handled = true;
        }

        private void Root_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            e.Handled = true;
        }
    }

    public sealed class PlayDirectoryRowActivatedEventArgs : EventArgs
    {
        public PlayDirectoryRowActivatedEventArgs(PlayDirectorySectionKind kind, PlayDirectoryRow row)
        {
            Kind = kind;
            Row = row;
        }

        public PlayDirectorySectionKind Kind { get; private set; }

        public PlayDirectoryRow Row { get; private set; }
    }

    public sealed class PlayDirectorySourceEventArgs : EventArgs
    {
        public PlayDirectorySourceEventArgs(PlayDirectorySectionKind kind)
        {
            Kind = kind;
        }

        public PlayDirectorySectionKind Kind { get; private set; }
    }
}
