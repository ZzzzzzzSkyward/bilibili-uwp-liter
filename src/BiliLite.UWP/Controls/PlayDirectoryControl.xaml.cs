using System;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
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
        /// </summary>
        public void SetModel(PlayDirectoryModel model)
        {
            m_model = model;
            HeaderText.Text = model == null ? "" : model.HeaderText ?? "";
            SourceTitleText.Text = model == null ? "" : model.SourceTitle ?? "";
            PositionText.Text = ComposePosition(model);

            BuildTabs(model);
            var active = model == null ? PlayDirectorySectionKind.Parts : model.ActiveKind;
            SelectScope(SectionOf(active) == null ? FirstKind(model) : active, false);
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
        /// 把正在播放的一行滚进视野;当前行不在浏览的分段里时先切回它所在的分段
        /// </summary>
        public void LocateCurrent()
        {
            var row = CurrentRow();
            if (row == null) return;
            var kind = KindOf(row);
            if (kind != m_activeKind) SelectScope(kind, true);
            RowList.SelectedItem = row;
            RowList.ScrollIntoView(row, ScrollIntoViewAlignment.Leading);
        }

        private static string ComposePosition(PlayDirectoryModel model)
        {
            if (model == null) return "";
            if (string.IsNullOrEmpty(model.CurrentPositionText)) return model.CountText ?? "";
            if (string.IsNullOrEmpty(model.CountText)) return model.CurrentPositionText;
            return model.CurrentPositionText + " · " + model.CountText;
        }

        private void BuildTabs(PlayDirectoryModel model)
        {
            TabHost.Children.Clear();
            if (model == null || model.Sections == null) return;
            foreach (var section in model.Sections)
            {
                if (section == null) continue;
                var text = section.Kind == model.ActiveKind ? section.Title + " · 连播中" : section.Title;
                var tab = new Button()
                {
                    Content = text,
                    Tag = section.Kind,
                    Style = (Style)Resources["DirectoryTabStyle"],
                };
                tab.Click += Tab_Click;
                TabHost.Children.Add(tab);
            }
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

        private PlayDirectorySectionKind FirstKind(PlayDirectoryModel model)
        {
            if (model != null && model.Sections != null)
            {
                foreach (var section in model.Sections)
                {
                    if (section != null) return section.Kind;
                }
            }
            return PlayDirectorySectionKind.Parts;
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

        private PlayDirectoryRow CurrentRow()
        {
            if (m_model == null || string.IsNullOrEmpty(m_model.CurrentKey)) return null;
            foreach (var section in m_model.Sections)
            {
                if (section == null || section.Rows == null) continue;
                foreach (var row in section.Rows)
                {
                    if (row != null && row.IsCurrent) return row;
                }
            }
            return null;
        }

        private PlayDirectorySectionKind KindOf(PlayDirectoryRow row)
        {
            if (m_model == null || m_model.Sections == null) return m_activeKind;
            foreach (var section in m_model.Sections)
            {
                if (section == null || section.Rows == null) continue;
                foreach (var candidate in section.Rows)
                {
                    if (ReferenceEquals(candidate, row)) return section.Kind;
                }
            }
            return m_activeKind;
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
