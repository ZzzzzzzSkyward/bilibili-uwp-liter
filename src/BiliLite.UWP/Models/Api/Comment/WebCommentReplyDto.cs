using System.Collections.Generic;
using BiliLite.Models.Common;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BiliLite.Models.Api.Comment
{
    /// <summary>
    /// 一条评论（x/v2/reply、x/v2/reply/wbi/main、x/v2/reply/reply 三个端点的行都是这个形状）。
    /// 宽窄照线上：rpid/oid/root/parent/ctime 是数字，member.mid 是字符串，content.emote 是自由的键值对象。
    /// 应用模型（CommentModel）在 Controls/CommentControl.xaml.cs，字段归一化在 WebCommentPageMapper。
    /// </summary>
    public class WebCommentReplyDto
    {
        [JsonProperty("rpid")]
        public long Rpid { get; set; }

        /// <summary>rpid 的字符串形式；动作接口要的是数字，字符串只做携带</summary>
        [JsonProperty("rpid_str")]
        public string RpidStr { get; set; }

        [JsonProperty("oid")]
        public long Oid { get; set; }

        [JsonProperty("type")]
        public int Type { get; set; }

        [JsonProperty("mid")]
        public long Mid { get; set; }

        [JsonProperty("root")]
        public long Root { get; set; }

        [JsonProperty("parent")]
        public long Parent { get; set; }

        [JsonProperty("count")]
        public int Count { get; set; }

        /// <summary>回复条数，界面显示的是它</summary>
        [JsonProperty("rcount")]
        public int Rcount { get; set; }

        [JsonProperty("like")]
        public int Like { get; set; }

        /// <summary>当前账号点过没有：0 未点赞，1 已点赞</summary>
        [JsonProperty("action")]
        public int Action { get; set; }

        [JsonProperty("floor")]
        public int Floor { get; set; }

        [JsonProperty("state")]
        public int State { get; set; }

        /// <summary>秒；页面上的 time 由模型的 getter 换算</summary>
        [JsonProperty("ctime")]
        public long Ctime { get; set; }

        [JsonProperty("member")]
        public WebCommentMemberDto Member { get; set; }

        [JsonProperty("content")]
        public WebCommentContentDto Content { get; set; }

        [JsonProperty("up_action")]
        public WebCommentUpActionDto UpAction { get; set; }

        [JsonProperty("reply_control")]
        public WebCommentReplyControlDto ReplyControl { get; set; }
    }

    public class WebCommentMemberDto
    {
        /// <summary>线上是字符串；应用模型也按字符串携带</summary>
        [JsonProperty("mid")]
        public string Mid { get; set; }

        [JsonProperty("uname")]
        public string Uname { get; set; }

        [JsonProperty("sex")]
        public string Sex { get; set; }

        [JsonProperty("sign")]
        public string Sign { get; set; }

        [JsonProperty("avatar")]
        public string Avatar { get; set; }

        [JsonProperty("level_info")]
        public WebCommentLevelInfoDto LevelInfo { get; set; }

        /// <summary>粉丝牌；没有就是 null，界面按 null 判断显不显示</summary>
        [JsonProperty("fans_detail")]
        public WebCommentFansDetailDto FansDetail { get; set; }

        [JsonProperty("user_sailing")]
        public WebCommentUserSailingDto UserSailing { get; set; }

        /// <summary>头像框；这里的 id 字段线上叫 pid</summary>
        [JsonProperty("pendant")]
        public WebCommentPendantDto Pendant { get; set; }

        [JsonProperty("official_verify")]
        public WebCommentOfficialVerifyDto OfficialVerify { get; set; }

        [JsonProperty("vip")]
        public WebCommentVipDto Vip { get; set; }
    }

    public class WebCommentLevelInfoDto
    {
        [JsonProperty("current_level")]
        public int CurrentLevel { get; set; }
    }

    public class WebCommentFansDetailDto
    {
        [JsonProperty("uid")]
        public long Uid { get; set; }

        [JsonProperty("medal_id")]
        public int MedalId { get; set; }

        [JsonProperty("medal_name")]
        public string MedalName { get; set; }

        [JsonProperty("level")]
        public int Level { get; set; }

        [JsonProperty("medal_color")]
        public long MedalColor { get; set; }

        [JsonProperty("medal_color_end")]
        public long MedalColorEnd { get; set; }

        [JsonProperty("medal_color_border")]
        public long MedalColorBorder { get; set; }

        [JsonProperty("medal_color_name")]
        public long MedalColorName { get; set; }

        [JsonProperty("medal_color_level")]
        public long MedalColorLevel { get; set; }
    }

    public class WebCommentPendantDto
    {
        [JsonProperty("pid")]
        public long Pid { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("image")]
        public string Image { get; set; }
    }

    public class WebCommentUserSailingDto
    {
        [JsonProperty("pendant")]
        public WebCommentSailingPendantDto Pendant { get; set; }

        [JsonProperty("cardbg")]
        public WebCommentCardbgDto Cardbg { get; set; }
    }

    /// <summary>user_sailing.pendant：id 字段线上就叫 id（member.pendant 那个叫 pid）</summary>
    public class WebCommentSailingPendantDto
    {
        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("image")]
        public string Image { get; set; }
    }

    public class WebCommentCardbgDto
    {
        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("image")]
        public string Image { get; set; }

        [JsonProperty("jump_url")]
        public string JumpUrl { get; set; }

        [JsonProperty("fan")]
        public WebCommentCardbgFanDto Fan { get; set; }
    }

    public class WebCommentCardbgFanDto
    {
        [JsonProperty("is_fan")]
        public int IsFan { get; set; }

        [JsonProperty("number")]
        public int Number { get; set; }

        [JsonProperty("color")]
        public string Color { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("num_desc")]
        public string NumDesc { get; set; }
    }

    public class WebCommentOfficialVerifyDto
    {
        [JsonProperty("type")]
        public int Type { get; set; }

        [JsonProperty("desc")]
        public string Desc { get; set; }
    }

    public class WebCommentVipDto
    {
        /// <summary>0 不是大会员，2 是大会员（归档能到 2）</summary>
        [JsonProperty("vipType")]
        public int VipType { get; set; }

        [JsonProperty("vipStatus")]
        public int VipStatus { get; set; }
    }

    public class WebCommentContentDto
    {
        [JsonProperty("message")]
        public string Message { get; set; }

        /// <summary>来源端：2 Android / 3 iOS / 4 WindowsPhone / 6 Windows，模型的 plat_str 从这里派生</summary>
        [JsonProperty("plat")]
        public int Plat { get; set; }

        [JsonProperty("device")]
        public string Device { get; set; }

        /// <summary>笔记图片；线上形状就是 Models/Common.cs 的 NotePicture，直接复用那个线上类型</summary>
        [JsonProperty("pictures")]
        public List<NotePicture> Pictures { get; set; }

        /// <summary>表情键值表，键是明文、值带 url；原样交给 ControlHelper.StringToRichText</summary>
        [JsonProperty("emote")]
        public JObject Emote { get; set; }
    }

    public class WebCommentUpActionDto
    {
        [JsonProperty("like")]
        public bool Like { get; set; }

        [JsonProperty("reply")]
        public bool Reply { get; set; }
    }

    public class WebCommentReplyControlDto
    {
        [JsonProperty("time_desc")]
        public string TimeDesc { get; set; }

        /// <summary>线上是"IP属地：上海"这一串</summary>
        [JsonProperty("location")]
        public string Location { get; set; }
    }
}
