using System.Collections.Generic;
using BiliLite.Models.Common;
using Newtonsoft.Json;

namespace BiliLite.Models.Api.Video
{
    /// <summary>
    /// x/web-interface/view（以及 app/x/v2/view、第三方代理）的 data。
    /// 三源共用一个形状：改造前它们共用一份 VideoDetailModel，这里保持同样的取舍。
    ///
    /// 1. aid / cid / mid 线上是数字，DTO 里一律是 string：页面、播放队列、合集都按 string 传 id，
    ///    不在这一层转数字（Newtonsoft 会把数字读进 string）。
    /// 2. ugc_season 直接复用 Models/VideoCollections.cs 的 VideoUgcSeason：它本来就是线上形状，
    ///    页面也绑定它，再写一份 DTO 只是多一层没有含义的复制。
    /// 3. req_user 只做形状保真：它是本地 UI 状态，MapWebVideoView 不搬它的值。
    /// </summary>
    public class WebVideoViewDto
    {
        [JsonProperty("bvid")]
        public string Bvid { get; set; }

        [JsonProperty("aid")]
        public string Aid { get; set; }

        /// <summary>分P数量</summary>
        [JsonProperty("videos")]
        public int Videos { get; set; }

        /// <summary>分区ID</summary>
        [JsonProperty("tid")]
        public int Tid { get; set; }

        /// <summary>分区名</summary>
        [JsonProperty("tname")]
        public string Tname { get; set; }

        /// <summary>封面</summary>
        [JsonProperty("pic")]
        public string Pic { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        /// <summary>上传时间</summary>
        [JsonProperty("pubdate")]
        public long Pubdate { get; set; }

        /// <summary>创建时间</summary>
        [JsonProperty("ctime")]
        public long Ctime { get; set; }

        /// <summary>简介</summary>
        [JsonProperty("desc")]
        public string Desc { get; set; }

        [JsonProperty("attribute")]
        public long Attribute { get; set; }

        [JsonProperty("state")]
        public int State { get; set; }

        /// <summary>时长</summary>
        [JsonProperty("duration")]
        public int Duration { get; set; }

        [JsonProperty("rights")]
        public WebVideoRightsDto Rights { get; set; }

        [JsonProperty("dynamic")]
        public string Dynamic { get; set; }

        /// <summary>UP主</summary>
        [JsonProperty("owner")]
        public WebVideoOwnerDto Owner { get; set; }

        /// <summary>UP主信息扩展：web 端点从来不返回，app 端点才有</summary>
        [JsonProperty("owner_ext")]
        public WebVideoOwnerExtDto OwnerExt { get; set; }

        [JsonProperty("stat")]
        public WebVideoStatDto Stat { get; set; }

        /// <summary>线上的用户状态；本机状态由动作/状态读驱动，mapper 不搬</summary>
        [JsonProperty("req_user")]
        public WebVideoReqUserDto ReqUser { get; set; }

        /// <summary>分P；缺失/null = 没有分P（mapper 给空表）</summary>
        [JsonProperty("pages")]
        public List<WebVideoPageDto> Pages { get; set; }

        /// <summary>合作成员；缺失/null = 没有，页面据此隐藏</summary>
        [JsonProperty("staff")]
        public List<WebVideoStaffDto> Staff { get; set; }

        /// <summary>互动视频</summary>
        [JsonProperty("interaction")]
        public WebVideoInteractionDto Interaction { get; set; }

        /// <summary>续播位置</summary>
        [JsonProperty("history")]
        public WebVideoHistoryDto History { get; set; }

        [JsonProperty("argue_msg")]
        public string ArgueMsg { get; set; }

        [JsonProperty("share_subtitle")]
        public string ShareSubtitle { get; set; }

        [JsonProperty("redirect_url")]
        public string RedirectUrl { get; set; }

        /// <summary>合集；视频不属于合集时缺失</summary>
        [JsonProperty("ugc_season")]
        public VideoUgcSeason UgcSeason { get; set; }
    }

    public class WebVideoRightsDto
    {
        [JsonProperty("bp")]
        public int Bp { get; set; }

        /// <summary>能不能充电</summary>
        [JsonProperty("elec")]
        public int Elec { get; set; }

        /// <summary>能不能下载</summary>
        [JsonProperty("download")]
        public int Download { get; set; }

        /// <summary>是不是电影</summary>
        [JsonProperty("movie")]
        public int Movie { get; set; }

        /// <summary>是不是付费</summary>
        [JsonProperty("pay")]
        public int Pay { get; set; }
    }

    public class WebVideoOwnerDto
    {
        [JsonProperty("mid")]
        public string Mid { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("face")]
        public string Face { get; set; }
    }

    public class WebVideoOwnerExtDto
    {
        /// <summary>粉丝数</summary>
        [JsonProperty("fans")]
        public int Fans { get; set; }

        /// <summary>大会员信息</summary>
        [JsonProperty("vip")]
        public WebVideoOwnerExtVipDto Vip { get; set; }

        /// <summary>认证信息</summary>
        [JsonProperty("official_verify")]
        public WebVideoOwnerExtOfficialVerifyDto OfficialVerify { get; set; }
    }

    public class WebVideoOwnerExtVipDto
    {
        [JsonProperty("vipType")]
        public int VipType { get; set; }

        [JsonProperty("vipStatus")]
        public int VipStatus { get; set; }
    }

    public class WebVideoOwnerExtOfficialVerifyDto
    {
        /// <summary>0个人认证,1企业认证</summary>
        [JsonProperty("type")]
        public int Type { get; set; }

        [JsonProperty("desc")]
        public string Desc { get; set; }
    }

    public class WebVideoStatDto
    {
        [JsonProperty("aid")]
        public string Aid { get; set; }

        /// <summary>播放</summary>
        [JsonProperty("view")]
        public int View { get; set; }

        /// <summary>弹幕</summary>
        [JsonProperty("danmaku")]
        public int Danmaku { get; set; }

        /// <summary>评论</summary>
        [JsonProperty("reply")]
        public int Reply { get; set; }

        /// <summary>收藏</summary>
        [JsonProperty("favorite")]
        public int Favorite { get; set; }

        /// <summary>投币</summary>
        [JsonProperty("coin")]
        public int Coin { get; set; }

        /// <summary>分享</summary>
        [JsonProperty("share")]
        public int Share { get; set; }

        /// <summary>点赞</summary>
        [JsonProperty("like")]
        public int Like { get; set; }

        /// <summary>不喜欢，固定0</summary>
        [JsonProperty("dislike")]
        public int Dislike { get; set; }
    }

    public class WebVideoReqUserDto
    {
        /// <summary>是否关注</summary>
        [JsonProperty("attention")]
        public int Attention { get; set; }

        /// <summary>是否特别关注</summary>
        [JsonProperty("guest_attention")]
        public int GuestAttention { get; set; }

        /// <summary>是否收藏</summary>
        [JsonProperty("favorite")]
        public int Favorite { get; set; }

        /// <summary>是否点赞</summary>
        [JsonProperty("like")]
        public int Like { get; set; }

        /// <summary>是否投币</summary>
        [JsonProperty("coin")]
        public int Coin { get; set; }

        /// <summary>是否不喜欢</summary>
        [JsonProperty("dislike")]
        public int Dislike { get; set; }
    }

    public class WebVideoPageDto
    {
        [JsonProperty("cid")]
        public string Cid { get; set; }

        [JsonProperty("page")]
        public string Page { get; set; }

        [JsonProperty("part")]
        public string Part { get; set; }

        [JsonProperty("duration")]
        public int Duration { get; set; }

        [JsonProperty("dmlink")]
        public string Dmlink { get; set; }

        [JsonProperty("download_title")]
        public string DownloadTitle { get; set; }

        [JsonProperty("download_subtitle")]
        public string DownloadSubtitle { get; set; }
    }

    public class WebVideoStaffDto
    {
        [JsonProperty("mid")]
        public string Mid { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("face")]
        public string Face { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("vip")]
        public WebVideoOwnerExtVipDto Vip { get; set; }

        [JsonProperty("official_verify")]
        public WebVideoOwnerExtOfficialVerifyDto OfficialVerify { get; set; }

        [JsonProperty("attention")]
        public int Attention { get; set; }
    }

    public class WebVideoInteractionDto
    {
        [JsonProperty("graph_version")]
        public int GraphVersion { get; set; }

        [JsonProperty("history_node")]
        public WebVideoInteractionHistoryNodeDto HistoryNode { get; set; }
    }

    public class WebVideoInteractionHistoryNodeDto
    {
        [JsonProperty("node_id")]
        public int NodeId { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("cid")]
        public long Cid { get; set; }
    }

    public class WebVideoHistoryDto
    {
        [JsonProperty("cid")]
        public string Cid { get; set; }

        [JsonProperty("progress")]
        public int Progress { get; set; }
    }
}
