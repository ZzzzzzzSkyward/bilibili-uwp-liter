using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BiliLite.Helpers;
using BiliLite.Models.Common;
using BiliLite.Models.Requests.Api;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

//https://go.microsoft.com/fwlink/?LinkId=234236 上介绍了“用户控件”项模板

namespace BiliLite.Controls
{
    /// <summary>
    /// The danmaku list button and its flyout. It only reads what the player already loaded:
    /// comments near the playhead or the whole loaded pool, copy on every row, and a like that
    /// reaches the server only for online rows that carry a server id.
    /// </summary>
    public sealed partial class DanmakuListControl : UserControl
    {
        private readonly HashSet<string> likedRowIds = new HashSet<string>(StringComparer.Ordinal);
        private string likedItemCid;
        private DanmakuListMode mode = DanmakuListMode.Recent;

        public DanmakuListControl()
        {
            this.InitializeComponent();
            TxtTitle.Text = TitleForMode(mode);
            // 判定结果出来之前不占位置:没有弹幕的集不该留一个点开是空的入口
            ApplyShowButton(false);
        }

        /// <summary>The player control whose loaded pool and play item this list reads.</summary>
        public PlayerControl Owner { get; set; }

        /// <summary>
        /// 本集有没有弹幕可以列。由播放器判定后设置,控件自己不去猜;
        /// 默认藏,第一次判定之前保持藏。
        /// </summary>
        public bool ShowButton
        {
            get { return (bool)GetValue(ShowButtonProperty); }
            set { SetValue(ShowButtonProperty, value); }
        }

        public static readonly DependencyProperty ShowButtonProperty =
            DependencyProperty.Register("ShowButton", typeof(bool), typeof(DanmakuListControl),
                new PropertyMetadata(false, OnShowButtonChanged));

        private static void OnShowButtonChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            ((DanmakuListControl)sender).ApplyShowButton(e.NewValue is bool show && show);
        }

        private void ApplyShowButton(bool show)
        {
            if (BtnOpenDanmakuList == null) return;
            BtnOpenDanmakuList.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
        }

        private static string TitleForMode(DanmakuListMode listMode)
        {
            return listMode == DanmakuListMode.All ? "弹幕池中全部弹幕" : "前后10秒内弹幕";
        }

        private void DanmakuListFlyout_Opening(object sender, object e)
        {
            mode = DanmakuListMode.Recent;
            RefreshRows();
        }

        private void BtnReplaceShowMode_Click(object sender, RoutedEventArgs e)
        {
            mode = mode == DanmakuListMode.Recent ? DanmakuListMode.All : DanmakuListMode.Recent;
            RefreshRows();
        }

        private void RefreshRows()
        {
            var owner = Owner;
            var cid = owner?.CurrentPlayItem?.cid;
            if (!string.Equals(cid, likedItemCid, StringComparison.Ordinal))
            {
                likedRowIds.Clear();
                likedItemCid = cid;
            }
            TxtTitle.Text = TitleForMode(mode);
            var rows = owner?.GetDanmakuListSnapshot(mode) ?? new List<DanmakuListRow>();
            DanmakuListSnapshot.ApplySessionLikes(rows, likedRowIds);
            DanmakuList.ItemsSource = rows;
            TxtEmpty.Visibility = rows.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        private static DanmakuListRow RowOf(object sender)
        {
            return (sender as FrameworkElement)?.Tag as DanmakuListRow;
        }

        private void BtnCopyRow_Click(object sender, RoutedEventArgs e)
        {
            var row = RowOf(sender);
            if (row == null || string.IsNullOrEmpty(row.Text)) return;
            if (Utils.SetClipboard(row.Text))
            {
                Utils.ShowMessageToast("已复制弹幕内容");
            }
            else
            {
                Utils.ShowMessageToast("复制弹幕失败");
            }
        }

        private async void BtnLikeRow_Click(object sender, RoutedEventArgs e)
        {
            await SendLike(RowOf(sender), 1);
        }

        private async void BtnCancelLikeRow_Click(object sender, RoutedEventArgs e)
        {
            await SendLike(RowOf(sender), 2);
        }

        /// <summary>Sends op=1 to like or op=2 to cancel a like sent from this list.</summary>
        private async Task SendLike(DanmakuListRow row, int op)
        {
            var owner = Owner;
            if (row == null || owner == null || !row.CanLike || !row.HasServerId) return;
            var cid = owner.CurrentPlayItem?.cid;
            if (string.IsNullOrEmpty(cid)) return;
            var results = await new DanmakuApi().Like(row.RowId, cid, op).Request();
            if (!results.status)
            {
                Utils.ShowMessageToast("点赞弹幕失败：" + results.message);
                return;
            }
            var body = results.GetJObject();
            if (body == null || body["code"].ToInt32() != 0)
            {
                Utils.ShowMessageToast("点赞弹幕失败：" + (body?["message"]?.ToString() ?? string.Empty));
                return;
            }
            if (op == 1)
            {
                likedRowIds.Add(row.RowId);
            }
            else
            {
                likedRowIds.Remove(row.RowId);
            }
            Utils.ShowMessageToast(op == 1 ? "已点赞弹幕" : "已取消点赞");
            RefreshRows();
        }
    }
}
