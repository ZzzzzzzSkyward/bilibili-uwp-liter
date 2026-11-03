using System;
using BiliLite.Helpers;
using Newtonsoft.Json.Linq;

namespace BiliLite.Parsing.Dynamic
{
    /// <summary>
    /// web 聚合动态流的一页。图片字段原样留在 items 里交给映射层，
    /// 这里只负责信封、形状和这一路自己的游标。
    /// </summary>
    public sealed class WebDynamicFeedPage
    {
        public JArray Items { get; set; }

        /// <summary>下一页要回传的 offset；空串/null 表示服务端没给游标</summary>
        public string NextCursor { get; set; }

        public bool HasMore { get; set; }
    }

    /// <summary>
    /// x/polymer/web-dynamic/v1/feed/{all,space} 的解析：解码一次、查信封、查字段形状。
    /// 不判断要不要换源，也不发布任何东西。
    ///
    /// 字段/行为约定（媒体：web 聚合流、gRPC DynamicItem、legacy cards 的同一套卡片字段；夹具见 plan/tests/g3）
    /// 1. 信封与游标各归各的来源：web 的 items/has_more/offset、gRPC 的 history offset、legacy 的
    ///    cards/offset 互不挪用；本文件只查形状，不选源、不翻页。
    /// 2. 字段表（✓=映射层已搬进卡片模型；—=来源没有该字段；✗=仍未搬，行尾给出原因）：
    ///
    ///    卡片种类          web                                   gRPC                                   legacy
    ///    正文节点          ✓ desc 与 major.opus.summary 的节点都搬；   ✓ ModuleDesc；本轮补 OPUS 摘要段落     ✓ desc.text + emoji_details
    ///                       节点类型显式归一，emoji 保留 icon/size
    ///    图片数组          ✓ opus.pics 认 url 与 src；draw.items 过滤空地址  ✓ DynDraw.Items；本轮补 OPUS Covers  ✓ card.item.pictures
    ///    视频 / 合集       ✓ archive / ugc_season                ✓ DynArchive / DynUgcSeason             ✓ card → DynamicVideoCardModel
    ///    番剧              ✓ pgc                                 ✓ DynPgc                                ✓ apiSeasonInfo
    ///    专栏              ✓ article                             ✓ DynArticle                            ✓ card.item
    ///    音频              ✓ music（借 article 槽）               ✓ DynMusic（同上）                       ✓ card 音频字段
    ///    直播              ✓ live；live_rcmd 解嵌套 content      ✓ DynCommonLive / DynLiveRcmd           ✓ live_play_info
    ///    转发原卡          ✓ orig 递归同一映射                   ✓ DynForward.Item 递归                   ✓ origin + origin_extend_json
    ///    附件卡            ✓ 本轮搬 additional（预约/投票/通用/商品/稿件） ✓ 本轮搬 ModuleAdditional        ✗ 只读了 add_on_card_info 的 reserve 子集
    ///    计数 / 禁评禁转   ✓ count + forbidden + liked          ✓ 本轮补 no_comment / no_forward         ✓ desc.comment/like/repost
    ///    OPUS 标题         ✗ draw 槽只有 items，标题要等卡数据契约    ✗ Paragraph.Title 无槽位                — 旧接口没有 OPUS
    ///    绝对发布时间      ✓ module_author.pub_ts                ✗ 只有 ptime_label_text                  ✓ desc.timestamp
    /// 3. 正文句子流不在这里拼 XAML 字符串：节点顺序由 RichTextFlow 归一成片段（Text/LineBreak/
    ///    Mention/Topic/Link/Emoji），只有来源里的换行产生 LineBreak，也不按字符数截断；
    ///    链接的标签进正文、目标地址只做跳转目标。
    /// </summary>
    public static class WebDynamicFeedParser
    {
        public static ApiParseResult<WebDynamicFeedPage> Parse(HttpResults response)
        {
            //传输、JSON、业务码、data 对象这一层两种来源一样，交给 ApiEnvelope 统一判断
            var envelope = ApiEnvelope.Read(response);
            if (!envelope.IsSuccess)
            {
                return ApiParseResult<WebDynamicFeedPage>.FromFailure(envelope);
            }
            var data = envelope.Value;
            //items 缺失或为 null 是一个合法的空页；给了别的类型才是形状问题
            var itemsToken = data["items"];
            JArray items;
            if (itemsToken == null || itemsToken.Type == JTokenType.Null)
            {
                items = new JArray();
            }
            else if (itemsToken.Type == JTokenType.Array)
            {
                items = (JArray)itemsToken;
            }
            else
            {
                return ApiParseResult<WebDynamicFeedPage>.Fail(ApiParseFailure.Payload, "data.items",
                    "items 不是数组（实际是 " + itemsToken.Type + "）");
            }

            var hasMoreToken = data["has_more"];
            var hasMore = false;
            if (hasMoreToken != null && hasMoreToken.Type != JTokenType.Null)
            {
                if (hasMoreToken.Type != JTokenType.Boolean)
                {
                    return ApiParseResult<WebDynamicFeedPage>.Fail(ApiParseFailure.Payload, "data.has_more",
                        "has_more 不是布尔值（实际是 " + hasMoreToken.Type + "）");
                }
                hasMore = hasMoreToken.Value<bool>();
            }

            var cursorToken = data["offset"];
            return ApiParseResult<WebDynamicFeedPage>.Success(new WebDynamicFeedPage
            {
                Items = items,
                HasMore = hasMore,
                NextCursor = cursorToken == null || cursorToken.Type == JTokenType.Null
                    ? null
                    : cursorToken.ToString(),
            });
        }
    }
}
