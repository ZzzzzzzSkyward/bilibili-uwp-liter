using System.Text;
using BiliLite.Modules;

namespace BiliLite.Parsing.Dynamic
{
    /// <summary>
    /// 预约卡的说明行：来源把时间（desc1）、预约人数（desc2）、抽奖文案（desc3）分成三个槽位，
    /// 拼成一句"12-01 20:00 1.2万人预约"读不出哪段是哪段，也和按钮上的「预约」用词不一致。
    /// 这里按槽位给标签：时间 开播 / N人已预约 / 抽奖文案，标签之间用一个空格分隔，不加括号；
    /// 一段都取不到时退回来源给的整句（reserve.desc），不替来源编造文案。
    /// 纯函数：不读卡片其它字段，也不做 URL/命令判断。
    /// </summary>
    public static class ReserveCardCopy
    {
        /// <summary>时间标签的结尾词与状态词：时段补「开播」，状态词（直播中/预约中）原样保留。</summary>
        private const string StartWord = "开播";
        private const string LiveWord = "直播";
        private const string LiveStateWord = "直播中";
        private const string ReserveStateWord = "预约中";
        private const string ReserveWord = "预约";
        private const string ReservedWord = "已预约";

        public static string Format(DynamicCardModel2024_addin_reserve reserve)
        {
            if (reserve == null)
            {
                return "";
            }

            var copy = new StringBuilder();
            AppendTag(copy, TimeTag(reserve.desc1?.text));
            AppendTag(copy, CountTag(reserve.reserve_total, reserve.desc2?.text));
            // 抽奖文案是来源写好的整句，不拆不改，只加框
            AppendTag(copy, Trimmed(reserve.desc3?.text));

            return copy.Length == 0 ? reserve.desc ?? "" : copy.ToString();
        }

        /// <summary>
        /// 时间槽位：来源有时写「11-13 20:00」、有时写「11-13 20:00 直播」，也见过已经写成「开播」的。
        /// 一时段一律落到「… 开播」；「直播中」「预约中」是状态词而不是时间，原样保留。
        /// </summary>
        private static string TimeTag(string text)
        {
            var value = Trimmed(text);
            if (value.Length == 0)
            {
                return "";
            }
            // desc1 不一定是一串时间：style=1 的直播中/预约中本身就是一个状态词，补「开播」会读成病句
            if (value.EndsWith(StartWord) || value.EndsWith(LiveStateWord) || value.EndsWith(ReserveStateWord))
            {
                return value;
            }
            if (value.EndsWith(LiveWord))
            {
                value = value.Substring(0, value.Length - LiveWord.Length).TrimEnd();
            }
            return value + " " + StartWord;
        }

        /// <summary>
        /// 人数槽位：结构化的人数（reserve_total）比文案可信，有就用它；没有才读 desc2，
        /// 并把来源的「预约」补成「已预约」——「175人预约」读成待办，「175人已预约」才是现状。
        /// </summary>
        private static string CountTag(int reserveTotal, string text)
        {
            if (reserveTotal > 0)
            {
                return reserveTotal + "人" + ReservedWord;
            }

            var value = Trimmed(text);
            if (value.Length == 0)
            {
                return "";
            }
            if (value.EndsWith(ReservedWord))
            {
                return value;
            }
            if (value.EndsWith(ReserveWord))
            {
                return value.Substring(0, value.Length - ReserveWord.Length) + ReservedWord;
            }
            return value + "人" + ReservedWord;
        }

        private static void AppendTag(StringBuilder copy, string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return;
            }
            if (copy.Length > 0)
            {
                copy.Append(' ');
            }
            copy.Append(text);
        }

        private static string Trimmed(string text)
        {
            return (text ?? "").Trim();
        }
    }
}
