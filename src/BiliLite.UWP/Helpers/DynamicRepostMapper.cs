using System.Collections.Generic;
using System.Windows.Input;
using BiliLite.Controls.Dynamic;
using BiliLite.Modules;

namespace BiliLite.Helpers
{
    /// <summary>
    /// 转发列表把 gRPC 的 Dynamic V2 卡片转成转发面板的显示模型。
    /// 面板只绑定作者、时间、动态 ID 与纯文本正文，因此这里仅复制这几个字段。
    /// DynamicVM.ConvertToDisplay2024 是实例方法：它需要 14 个命令、稍后再看 VM、
    /// RichTextBlock 富文本与 OneRowInfo 解析；仅为转发面板实例化整个 DynamicVM
    /// 会带上动态首页的全部状态，所以在该文件里重复这十来行字段是有意的取舍。
    /// </summary>
    public static class DynamicRepostMapper
    {
        public static List<DynamicItemDisplayModel> ToDisplayList(
            IEnumerable<DynamicCardModel2024> cards,
            ICommand userCommand)
        {
            var result = new List<DynamicItemDisplayModel>();
            if (cards == null)
            {
                return result;
            }

            foreach (var card in cards)
            {
                var display = ToDisplay(card, userCommand);
                if (display != null)
                {
                    result.Add(display);
                }
            }

            return result;
        }

        public static DynamicItemDisplayModel ToDisplay(DynamicCardModel2024 card, ICommand userCommand)
        {
            if (card == null)
            {
                return null;
            }

            var author = card.modules?.module_author;
            var display = new DynamicItemDisplayModel
            {
                Type = DynamicDisplayType.Repost,
                DynamicID = card.id_str,
                Mid = author?.mid ?? card.mid,
                UserName = FirstNonEmpty(author?.name, card.owner),
                Photo = FirstNonEmpty(author?.face, card.face),
                IsYearVip = author?.vip != null && author.vip.status == 1 && author.vip.type == 2,
                Time = string.IsNullOrEmpty(author?.pub_time)
                    ? (card.pubdate <= 0 ? "" : Utils.HandelTimestamp(card.pubdate.ToString()))
                    : author.pub_time,
                ContentStr = GetContentText(card),
                IsRepost = true,
                UserCommand = userCommand,
            };

            display.Datetime = string.IsNullOrEmpty(author?.pub_time)
                ? (card.pubdate <= 0 ? "" : Utils.TimestampToDatetime(card.pubdate).ToString())
                : author.pub_time;

            switch (author?.official_verify?.type ?? 3)
            {
                case 0:
                    display.Verify = AppHelper.VERIFY_PERSONAL_IMAGE;
                    break;
                case 1:
                    display.Verify = AppHelper.VERIFY_OGANIZATION_IMAGE;
                    break;
                default:
                    display.Verify = AppHelper.TRANSPARENT_IMAGE;
                    break;
            }

            return display;
        }

        /// <summary>
        /// 转发面板展示的正文：Dynamic V2 的 desc.text 已经是纯文本。
        /// </summary>
        public static string GetContentText(DynamicCardModel2024 card)
        {
            return card?.modules?.module_dynamic?.desc?.text ?? "";
        }

        private static string FirstNonEmpty(params string[] values)
        {
            if (values == null)
            {
                return "";
            }

            foreach (var value in values)
            {
                if (!string.IsNullOrEmpty(value))
                {
                    return value;
                }
            }

            return "";
        }
    }
}
