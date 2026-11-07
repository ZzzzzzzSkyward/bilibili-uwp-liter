using BiliLite.Modules;

namespace BiliLite.Parsing.Dynamic
{
    /// <summary>
    /// 不认识的卡片类型（服务端新增了类型，或映射器还没跟上）也要留人能看懂的东西：
    /// 作者、时间、正文和动态 ID。Other 卡片靠这些字段显示"是谁发的、说了什么"，
    /// 并在有 ID 时提供打开原动态的入口。
    /// </summary>
    public sealed class UnsupportedCardFields
    {
        public string DynamicId { get; set; } = "";
        public long Mid { get; set; }
        public string UserName { get; set; } = "";
        public string Photo { get; set; } = "";
        public string PendantImage { get; set; } = "";
        /// <summary>原始 official_verify.type：0 个人、1 机构，其它（含缺失）按无认证处理。</summary>
        public int OfficialVerifyType { get; set; } = 3;
        public string Time { get; set; } = "";
        public string Text { get; set; } = "";

        /// <summary>有动态 ID 才能打开原动态；作者信息可能缺席，正文可能为空。</summary>
        public bool HasDynamicId
        {
            get { return !string.IsNullOrEmpty(DynamicId); }
        }
    }

    public static class UnsupportedCard
    {
        /// <summary>
        /// 从卡片数据里取外层可显示的部分。不抛异常：任何一层缺失都退化成空字段，
        /// 让卡片退化成"只有正文"，而不是整张卡消失。
        /// </summary>
        public static UnsupportedCardFields From(DynamicCardModel2024 item)
        {
            if (item == null)
            {
                return new UnsupportedCardFields();
            }
            var author = item.modules?.module_author;
            var text = item.modules?.module_dynamic?.desc?.text;
            return new UnsupportedCardFields
            {
                DynamicId = item.id_str ?? "",
                Mid = author?.mid ?? 0,
                UserName = author?.name ?? "",
                Photo = author?.face ?? "",
                PendantImage = author?.pendant?.image ?? "",
                OfficialVerifyType = author?.official_verify?.type ?? 3,
                Time = author?.pub_time ?? "",
                Text = text ?? ""
            };
        }
    }
}
