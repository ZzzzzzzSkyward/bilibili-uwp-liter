using System;
using System.Globalization;
using Windows.System;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Automation;
using Windows.UI.Xaml.Automation.Peers;
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
        private PlayDirectoryModel m_source;
        private PlayDirectoryModel m_model;
        private PlayDirectorySectionKind m_activeKind;
        private bool m_scopeSet;
        private string m_query = "";

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

        /// <summary>
        /// 用户点了"加载更多"/"重试"。取页由宿主执行,抽屉自己不发请求。
        /// </summary>
        public event EventHandler LoadMoreRequested;

        public event EventHandler CloseRequested;

        public PlayDirectorySectionKind ActiveKind
        {
            get { return m_activeKind; }
        }

        /// <summary>
        /// 换一份目录。分段顺序由模型决定,页签只为有内容的分段生成。
        /// 重画时留着用户正在浏览的分段:换集、自动连播都不该把人从别的页签拽走;
        /// 只有新开目录或那一段没了才回到连播来源。
        /// 重画照旧带着正在用的搜索词:新到的行也从同一道筛子里过。
        /// </summary>
        public void SetModel(PlayDirectoryModel model)
        {
            var resetScope = model != null && model.IsFreshOpen || !m_scopeSet;
            m_source = model;
            // 新开目录:上一段搜索不跟过来,输入框也回空
            if (resetScope)
            {
                m_query = "";
                QueryBox.Text = "";
            }
            m_model = DisplayModel();
            HeaderText.Text = model == null ? "" : model.HeaderText ?? "";
            SourceTitleText.Text = model == null ? "" : model.SourceTitle ?? "";
            SetPosition(model);
            Render(resetScope ? (PlayDirectorySectionKind?)null : m_activeKind);
        }

        /// <summary>
        /// 在已经加载的行里搜索。只筛抽屉手里这份目录:不发请求、不换连播来源、不起播,
        /// 浏览的分段只要还在就留着。空词恢复未过滤的那份。
        /// </summary>
        public void SetQuery(string query)
        {
            var text = query ?? "";
            if (QueryBox.Text != text) QueryBox.Text = text;
            var needle = text.Trim();
            if (needle == m_query) return;
            m_query = needle;
            m_model = DisplayModel();
            // 过滤只换行,不通知宿主:没有任何一段因此换来源
            Render(m_scopeSet ? m_activeKind : (PlayDirectorySectionKind?)null);
        }

        /// <summary>
        /// 搜索框里有词。Esc 先清它,空了才轮到关抽屉
        /// </summary>
        public bool HasQuery
        {
            get { return m_query.Length > 0; }
        }

        /// <summary>
        /// 要显示的那份:有词就筛一遍手里这份,没词就是它本身
        /// </summary>
        private PlayDirectoryModel DisplayModel()
        {
            return m_query.Length == 0 ? m_source : PlayDirectoryBuilder.Filter(m_source, m_query);
        }

        private void Render(PlayDirectorySectionKind? browsed)
        {
            BuildTabs(m_model);
            SelectScope(PlayDirectoryBuilder.ResolveBrowsedSection(m_model, browsed), false);
            UpdateSourceActions(m_model);
            UpdatePaging(m_model);
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
            // 过滤时只在用户正在看的那一段里找:当前行被筛掉了就什么都不做,不跳到别的段
            var kind = m_model != null && m_model.IsFiltered
                ? m_activeKind
                : PlayDirectoryBuilder.LocateSection(m_model, m_activeKind);
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
            var filtering = model.IsFiltered;
            foreach (var section in model.Sections)
            {
                if (section == null) continue;
                var count = section.Rows == null ? 0 : section.Rows.Count;
                var title = section.Title ?? "";
                // 过滤时页签带上命中数:一个数字,不加单位也不解释
                if (filtering) title += " " + count.ToString(CultureInfo.InvariantCulture);
                var tab = new Button()
                {
                    Tag = section.Kind,
                    Style = (Style)Resources["DirectoryTabStyle"],
                    // 没命中的段留着让用户看见它还在,只是点不开
                    IsEnabled = !filtering || count > 0,
                };
                var isContinuation = section.Kind == model.ActiveKind;
                tab.Content = isContinuation
                    ? (object)ContinuationTabContent(title, tab)
                    : title;
                // 读屏读到页签名和它是不是连播来源;页签上的圆点只是把同一件事画出来,不再念一遍
                AutomationProperties.SetName(tab, isContinuation ? title + " 连播中" : title);
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
            // 状态由页签的自动化名称说出来,圆点只是画出来的
            AutomationProperties.SetAccessibilityView(dot, AccessibilityView.Raw);
            host.Children.Add(dot);
            var label = new TextBlock() { Text = title, VerticalAlignment = VerticalAlignment.Center };
            // 页签的选中样式改的是按钮的 Foreground,标题跟着它走
            label.SetBinding(TextBlock.ForegroundProperty, new Binding() { Source = tab, Path = new PropertyPath("Foreground") });
            host.Children.Add(label);
            return host;
        }

        /// <summary>
        /// 翻页 footer:只属于远程播放列表那一段,按钮长在它的行下面。
        /// 已到头和固定内存队列都不留空块;加载中只有转圈,同一件事不再用字说一遍。
        /// </summary>
        private void UpdatePaging(PlayDirectoryModel model)
        {
            var state = model == null ? PlayDirectoryPagingState.None : model.Paging;
            // 该不该摆:这页真的能翻,而且用户正看着会产生新行的那一段
            var owned = state != PlayDirectoryPagingState.None
                && m_activeKind == PlayDirectorySectionKind.Playlist
                && SectionOf(PlayDirectorySectionKind.Playlist) != null;
            var loading = state == PlayDirectoryPagingState.Loading;
            var actionable = state == PlayDirectoryPagingState.More || state == PlayDirectoryPagingState.Failed;

            PagingHost.Visibility = owned && (actionable || loading) ? Visibility.Visible : Visibility.Collapsed;
            PagingRing.Visibility = owned && loading ? Visibility.Visible : Visibility.Collapsed;
            PagingRing.IsActive = owned && loading;
            LoadMoreButton.Visibility = owned && actionable ? Visibility.Visible : Visibility.Collapsed;

            var text = state == PlayDirectoryPagingState.Failed ? "重试" : "加载更多";
            LoadMoreText.Text = text;
            // 按钮上的字就是它做的事,读屏读同一句
            AutomationProperties.SetName(LoadMoreButton, text);
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
            UpdatePaging(m_model);
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

        /// <summary>
        /// 输入框里打字归输入框;上下键把焦点交给结果列表并移动选中行,回车激活那一行
        /// </summary>
        private void QueryBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Enter)
            {
                e.Handled = true;
                var selected = RowList.SelectedItem as PlayDirectoryRow;
                if (selected == null)
                {
                    FocusRows();
                    return;
                }
                Activate(selected);
                return;
            }
            if (e.Key != VirtualKey.Up && e.Key != VirtualKey.Down) return;
            e.Handled = true;
            FocusRows();
            MoveSelection(e.Key == VirtualKey.Down ? 1 : -1);
        }

        private void QueryBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            SetQuery(QueryBox.Text);
        }

        /// <summary>
        /// 在结果里挪一行;还没有选中项时从这一头的第一行开始
        /// </summary>
        private void MoveSelection(int delta)
        {
            var count = RowList.Items.Count;
            if (count == 0) return;
            var index = RowList.SelectedIndex;
            index = index < 0
                ? (delta > 0 ? 0 : count - 1)
                : Math.Min(Math.Max(0, index + delta), count - 1);
            RowList.SelectedIndex = index;
            var row = RowList.SelectedItem as PlayDirectoryRow;
            if (row != null) RowList.ScrollIntoView(row);
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

        private void LoadMoreButton_Click(object sender, RoutedEventArgs e)
        {
            var handler = LoadMoreRequested;
            if (handler != null) handler(this, EventArgs.Empty);
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

    /// <summary>
    /// 布尔标记转可见性:为真才显示。等待的那一行用它挂转圈,没有标记的行不占位。
    /// </summary>
    public sealed class PlayDirectoryVisibleConvert : Windows.UI.Xaml.Data.IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return value is bool && (bool)value ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotSupportedException();
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
