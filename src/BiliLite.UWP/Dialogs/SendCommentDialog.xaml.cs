using BiliLite.Api;
using BiliLite.Helpers;
using BiliLite.Modules;
using BiliLite.Parsing;
using BiliLite.Parsing.Comment;
using System;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using static BiliLite.Api.CommentApi;

namespace BiliLite.Dialogs
{
    public sealed partial class SendCommentDialog : ContentDialog
    {
        readonly CommentApi commentApi;
        readonly EmoteVM emoteVM;
        readonly string oid;
        readonly CommentType commentType;
        private Task _sendTask = Task.CompletedTask;
        private bool _isSending;
        private bool _retired;
        private long _sendGeneration;

        public SendCommentDialog(string oid, CommentType commentType)
        {
            this.InitializeComponent();
            commentApi = new CommentApi();
            emoteVM = new EmoteVM();
            this.oid = oid;
            this.commentType = commentType;
            Closing += ContentDialog_Closing;
            Closed += ContentDialog_Closed;
        }

        private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            // Cancel platform closing synchronously; the owned task alone may explicitly close on success.
            args.Cancel = true;
            if (_retired || _isSending) return;
            var text = txt_Comment.Text;
            if (string.IsNullOrWhiteSpace(text))
            {
                SendStatus.Text = "请输入评论内容";
                return;
            }

            _isSending = true;
            var generation = ++_sendGeneration;
            SetSending(true);
            SendStatus.Text = "正在发送";
            _sendTask = SendCommentAsync(text, generation);
        }

        private async Task SendCommentAsync(string text, long generation)
        {
            try
            {
                var response = await commentApi.AddComment(oid, commentType, text).Request();
                if (!OwnsSend(generation)) return;
                var parsed = WebCommentActionParser.Parse(response);
                if (!parsed.IsSuccess)
                {
                    var message = parsed.Failure == ApiParseFailure.Rejected
                        ? parsed.Message : "发送结果未确认，请先刷新评论再重试。";
                    RestoreReady(generation, message);
                    return;
                }

                // Retire before notice/Hide so reentrant button/Closing callbacks cannot send or publish twice.
                Retire();
                Utils.ShowMessageToast("发表评论成功");
                Hide();
            }
            catch (Exception)
            {
                // Transport completion is uncertain; never log raw draft, response or exception payload.
                LogHelper.Log("Comment send task failed", LogType.ERROR);
                RestoreReady(generation, "发送结果未确认，请先刷新评论再重试。");
            }
        }

        private bool OwnsSend(long generation)
        {
            return !_retired && _isSending && generation == _sendGeneration;
        }

        private void RestoreReady(long generation, string message)
        {
            if (!OwnsSend(generation)) return;
            _isSending = false;
            SetSending(false);
            SendStatus.Text = message;
            txt_Comment.Focus(FocusState.Programmatic);
        }

        private void SetSending(bool sending)
        {
            IsPrimaryButtonEnabled = !sending;
            txt_Comment.IsReadOnly = sending;
            btnOpenFace.IsEnabled = !sending;
            SendProgress.Visibility = sending ? Visibility.Visible : Visibility.Collapsed;
        }

        private void Retire()
        {
            if (_retired) return;
            _retired = true;
            ++_sendGeneration;
        }

        private void ContentDialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            args.Cancel = true;
            if (_retired) return;
            Retire();
            Hide();
        }

        private void ContentDialog_Closing(ContentDialog sender, ContentDialogClosingEventArgs args)
        {
            Retire();
        }

        private void ContentDialog_Closed(ContentDialog sender, ContentDialogClosedEventArgs args)
        {
            // Defensive fallback; the exception-contained task retains ownership of its completion after dismissal.
            Retire();
        }

        private async void btnOpenFace_Click(object sender, RoutedEventArgs e)
        {
            if (_retired || _isSending) return;
            try
            {
                FaceFlyout.ShowAt(sender as Button);
                if (emoteVM.Packages == null || emoteVM.Packages.Count == 0)
                {
                    await emoteVM.GetEmote(EmoteBusiness.reply);
                }
            }
            catch (Exception)
            {
                LogHelper.Log("Comment emote load failed", LogType.ERROR);
            }
        }

        private void GridView_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (_retired || _isSending) return;
            var item = e.ClickedItem as EmotePackageItemModel;
            if (item != null) txt_Comment.Text += item.text;
        }
    }
}
