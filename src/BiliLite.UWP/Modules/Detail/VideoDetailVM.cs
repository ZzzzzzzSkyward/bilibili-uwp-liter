using BiliLite.Api;
using BiliLite.Helpers;
using BiliLite.Models;
using BiliLite.Models.Common;
using BiliLite.Parsing;
using BiliLite.Parsing.Video;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace BiliLite.Modules
{
    public class VideoDetailVM:IModules
    {
        readonly Api.User.FavoriteApi favoriteAPI;
        readonly VideoAPI videoAPI;
        readonly PlayerAPI  PlayerAPI;
        readonly Api.User.FollowAPI followAPI;
        public VideoDetailVM()
        {
            videoAPI = new VideoAPI();
            favoriteAPI = new Api.User.FavoriteApi();
            PlayerAPI = new PlayerAPI();
            followAPI = new Api.User.FollowAPI();
            RefreshCommand = new RelayCommand(Refresh);
            LikeCommand = new RelayCommand(DoLike);
            DislikeCommand = new RelayCommand(DoDislike);
            LaunchUrlCommand=new RelayCommand<object>(LaunchUrl);
            CoinCommand = new RelayCommand<string>(DoCoin);
            //FavoriteCommand = new RelayCommand<string>(DoFavorite);
            AttentionCommand = new RelayCommand(DoAttentionUP);
            SetStaffHeightCommand = new RelayCommand<string>(SetStaffHeight);
        }
        public ICommand RefreshCommand { get; private set; }
        public ICommand LikeCommand{ get; private set; }
        public ICommand DislikeCommand { get; private set; }
        public ICommand CoinCommand { get; private set; }
        //public ICommand FavoriteCommand { get; private set; }
        public ICommand AttentionCommand { get; private set; }
        public ICommand LaunchUrlCommand { get; private set; }

        private async void LaunchUrl(object paramenter)
        {

            await MessageCenter.HandleUrl(paramenter.ToString());
            return;


        }


        private bool _loading = true;
        public bool Loading
        {
            get { return _loading; }
            set { _loading = value; DoPropertyChanged("Loading"); }
        }
        private bool _loaded = false;
        public bool Loaded
        {
            get { return _loaded; }
            set { _loaded = value; DoPropertyChanged("Loaded"); }
        }
        private bool _ShowError=false;
        public bool ShowError
        {
            get { return _ShowError; }
            set { _ShowError = value; DoPropertyChanged("ShowError"); }
        }
        private string _errorMsg="";

        public string ErrorMsg
        {
            get { return _errorMsg; }
            set { _errorMsg = value; DoPropertyChanged("ErrorMsg"); }
        }


        private VideoDetailModel _videoInfo;
        public VideoDetailModel VideoInfo
        {
            get { return _videoInfo; }
            set { _videoInfo = value; DoPropertyChanged("VideoInfo"); }
        }

        private double _staffHeight = 88.0;
        public double StaffHeight
        {
            get { return _staffHeight; }
            set { _staffHeight = value; DoPropertyChanged("StaffHeight"); }
        }

        private bool _showMoreStaff;

        public bool showMoreStaff
        {
            get { return _showMoreStaff; }
            set { _showMoreStaff = value; DoPropertyChanged("showMoreStaff"); }
        }
        public ICommand SetStaffHeightCommand { get; private set; }
        public void SetStaffHeight(string height)
        {
            var h = Convert.ToDouble(height);
            showMoreStaff = h > StaffHeight;
            StaffHeight = h;

        }


        private ObservableCollection<FavoriteItemModel> _myFavorite;
        public ObservableCollection<FavoriteItemModel> MyFavorite
        {
            get { return _myFavorite; }
            set { _myFavorite = value; DoPropertyChanged("MyFavorite"); }
        }
        public async Task LoadFavorite(string avid)
        {
            try
            {
                if (!SettingHelper.Account.Logined)
                {
                    return;
                }
                var parsed = FavoriteCreatedListParser.ParseFavoriteCreatedList(
                    await favoriteAPI.MyCreatedFavorite(avid).Request());
                if (!parsed.IsSuccess)
                {
                    //收藏夹列表读不到只影响收藏弹窗:一句提示,详情页照常
                    Utils.ShowMessageToast(VideoFailureText.For(parsed));
                    return;
                }
                MyFavorite = FavoriteCreatedListMapper.Map(parsed.Value);
            }
            catch (Exception ex)
            {
                var handel = HandelError<AnimeHomeModel>(ex);
                Utils.ShowMessageToast(handel.message);
            }
        }

        public async Task LoadVideoDetail(string id,bool isbvid=false)
        {
            try
            {
                Loaded = false;
                Loading = true;
                ShowError = false;

                //详情三源:web 详情 → 签名 app 详情 → 第三方代理。换不换源只看解析结论,
                //"这一源解不出来"才是不可用,"没拿到 relates"不是
                var webResult = await videoAPI.DetailWebInterface(id, isbvid).Request();
                var parsed = WebVideoViewParser.ParseWebVideoView(webResult);
                if (!parsed.IsSuccess)
                {
                    LogParseFailure("view.web", parsed);
                    parsed = WebVideoViewParser.ParseWebVideoView(await videoAPI.Detail(id, isbvid).Request());
                }
                if (!parsed.IsSuccess)
                {
                    LogParseFailure("view.app", parsed);
                    //第三方代理(biliproxy.iill.moe)不归本项目维护,只在前两源都解不出来时才会被请求;
                    //它的结论与 app 端点走同一个解析和同一个错误面板,不额外信任、不额外扩用
                    parsed = WebVideoViewParser.ParseWebVideoView(await videoAPI.DetailProxy(id, isbvid).Request());
                    if (!parsed.IsSuccess)
                    {
                        LogParseFailure("view.proxy", parsed);
                    }
                }

                if (!parsed.IsSuccess)
                {
                    ShowError = true;
                    ErrorMsg = VideoFailureText.For(parsed);
                    return;
                }

                var detail = WebVideoViewMapper.MapWebVideoView(parsed.Value);

                //相关视频是另一条读:它失败只丢推荐区,详情照常发布,整页不再被拖成 null
                var parsedRelates = WebVideoRelatesParser.ParseWebVideoRelates(
                    await videoAPI.RelatesWebInterface(id, isbvid).Request());
                if (parsedRelates.IsSuccess)
                {
                    detail.relates = WebVideoViewMapper.MapWebVideoRelates(parsedRelates.Value);
                }
                else
                {
                    LogParseFailure("relates", parsedRelates);
                    detail.relates = new List<VideoDetailRelatesModel>();
                }

                VideoInfo = detail;
                Loaded = true;
                await LoadOwnerFans();
                await GetAttentionUp();
                await GetUserVideoStates();
                await LoadVideoTags(detail.aid);
                await LoadFavorite(detail.aid);
            }
            catch (Exception ex)
            {
                var handel = HandelError<AnimeHomeModel>(ex);
                Utils.ShowMessageToast(handel.message);
                ShowError = true;
                ErrorMsg = handel.message;
            }
            finally
            {
                Loading = false;
            }
        }

        /// <summary>
        /// 解析失败的诊断日志:类别、字段定位、原文分开写,方便判断"换了源还是同一处错"
        /// </summary>
        private static void LogParseFailure<T>(string source, ApiParseResult<T> result)
        {
            System.Diagnostics.Debug.WriteLine("[video." + source + "] " +
                result.Failure + " " + result.Stage + ": " + result.Message);
        }

        /// <summary>
        /// 详情接口(Web/app)可能不含粉丝数, 单独查询; 失败时保持 fans=0 且不打断页面。
        /// 详情已经带了粉丝数就不再查:web 端点从来不返回 owner_ext,app 端点带了就不必多发一条
        /// </summary>
        private async Task LoadOwnerFans()
        {
            var ownerExt = VideoInfo?.owner_ext;
            var mid = VideoInfo?.owner?.mid;
            if (ownerExt == null || ownerExt.HasFans || string.IsNullOrEmpty(mid))
            {
                return;
            }

            int fans;
            try
            {
                var parsed = WebRelationParser.ParseWebRelationStat(await videoAPI.RelationStat(mid).Request());
                if (!parsed.IsSuccess)
                {
                    LogParseFailure("relation.stat", parsed);
                    return;
                }
                fans = parsed.Value;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
                return;
            }

            if (fans <= 0)
            {
                return;
            }

            ownerExt.fans = fans;
            VideoInfo.DoPropertyChanged("owner_ext");
        }

        private async Task LoadVideoTags(string aid)
        {
            try
            {
                var parsed = WebVideoTagsParser.ParseWebVideoTags(await videoAPI.Tags(aid).Request());
                if (!parsed.IsSuccess)
                {
                    //标签只是附加信息:读不到就保持原标签,只留诊断
                    LogParseFailure("tags", parsed);
                    return;
                }
                if (VideoInfo != null)
                {
                    VideoInfo.tag = WebVideoViewMapper.MapWebVideoTags(parsed.Value);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }
        }

        private async Task GetAttentionUp()
        {
            VideoInfo.req_user = VideoInfo.req_user ?? new VideoDetailReqUserModel();
            if (VideoInfo.req_user.attention != 1 && VideoInfo.req_user.attention != -999)
            {
                VideoInfo.req_user.attention = -999;
            }
            var previousAttention = VideoInfo.req_user.attention;
            if (!SettingHelper.Account.Logined)
            {
                return;
            }

            try
            {
                var parsed = WebRelationParser.ParseWebRelationAttribute(
                    await followAPI.GetAttention(VideoInfo.owner.mid).Request());
                if (!parsed.IsSuccess)
                {
                    LogParseFailure("relation.attribute", parsed);
                    return;
                }
                //2=已关注, 6=已互关;其余(0=未关注, 128=已拉黑)都按未关注
                VideoInfo.req_user.attention = parsed.Value == 2 || parsed.Value == 6 ? 1 : -999;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
                VideoInfo.req_user.attention = previousAttention;
            }
        }

        private async Task GetUserVideoStates()
        {
            try
            {
                if (!SettingHelper.Account.Logined || VideoInfo == null)
                {
                    return;
                }

                VideoInfo.req_user = VideoInfo.req_user ?? new VideoDetailReqUserModel();
                var likeTask = videoAPI.HasLike(VideoInfo.aid, VideoInfo.bvid).Request();
                var coinTask = videoAPI.HasCoin(VideoInfo.aid, VideoInfo.bvid).Request();
                var favoriteTask = videoAPI.HasFav(VideoInfo.aid, VideoInfo.bvid).Request();
                await Task.WhenAll(likeTask, coinTask, favoriteTask);

                var parsedLike = WebVideoStateParser.ParseWebVideoStateLike(likeTask.Result);
                if (parsedLike.IsSuccess)
                {
                    VideoInfo.req_user.like = parsedLike.Value == 1 ? 1 : 0;
                }
                else
                {
                    LogParseFailure("state.like", parsedLike);
                }

                var parsedCoin = WebVideoStateParser.ParseWebVideoStateCoin(coinTask.Result);
                if (parsedCoin.IsSuccess)
                {
                    VideoInfo.req_user.coin = parsedCoin.Value > 0 ? 1 : 0;
                }
                else
                {
                    LogParseFailure("state.coin", parsedCoin);
                }

                var parsedFavoured = WebVideoStateParser.ParseWebVideoStateFavoured(favoriteTask.Result);
                if (parsedFavoured.IsSuccess)
                {
                    VideoInfo.req_user.favorite = parsedFavoured.Value ? 1 : 0;
                }
                else
                {
                    LogParseFailure("state.favoured", parsedFavoured);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }
        }
        public void Refresh()
        {

        }
        public async void DoLike()
        {
            if (!SettingHelper.Account.Logined&&!await Utils.ShowLoginDialog())
            {
                Utils.ShowMessageToast("请先登录后再操作");
                return;
            }
            try
            {
                var parsed = WebVideoActionParser.ParseWebVideoAction(
                    await videoAPI.Like(VideoInfo.aid, VideoInfo.req_user.dislike, VideoInfo.req_user.like).Request());
                if (!parsed.IsSuccess)
                {
                    Utils.ShowMessageToast(VideoFailureText.For(parsed));
                    return;
                }
                if (VideoInfo.req_user.like == 1)
                {
                    VideoInfo.req_user.like = 0;
                    VideoInfo.stat.like -= 1;
                }
                else
                {
                    VideoInfo.req_user.like = 1;
                    VideoInfo.req_user.dislike = 0;
                    VideoInfo.stat.like += 1;
                }
                //服务端说了话就说它的话,没说就说"操作成功"
                Utils.ShowMessageToast(string.IsNullOrEmpty(parsed.Value) ? "操作成功" : parsed.Value);
            }
            catch (Exception ex)
            {
               var handel = HandelError<object>(ex);
                Utils.ShowMessageToast(handel.message);
            }


        }
        public async void DoDislike()
        {
            if (!SettingHelper.Account.Logined && !await Utils.ShowLoginDialog())
            {
                Utils.ShowMessageToast("请先登录后再操作");
                return;
            }
            try
            {
                var parsed = WebVideoActionParser.ParseWebVideoAction(
                    await videoAPI.Dislike(VideoInfo.aid, VideoInfo.req_user.dislike, VideoInfo.req_user.like).Request());
                if (!parsed.IsSuccess)
                {
                    Utils.ShowMessageToast(VideoFailureText.For(parsed));
                    return;
                }
                if (VideoInfo.req_user.dislike == 1)
                {
                    VideoInfo.req_user.dislike = 0;
                }
                else
                {
                    VideoInfo.req_user.dislike = 1;
                    if (VideoInfo.req_user.like == 1)
                    {
                        VideoInfo.req_user.like = 0;
                        VideoInfo.stat.like -= 1;
                    }
                }
                Utils.ShowMessageToast(string.IsNullOrEmpty(parsed.Value) ? "操作成功" : parsed.Value);
            }
            catch (Exception ex)
            {
                var handel = HandelError<object>(ex);
                Utils.ShowMessageToast(handel.message);
            }
            


        }
        public async void DoTriple()
        {
            if (!SettingHelper.Account.Logined && !await Utils.ShowLoginDialog())
            {
                Utils.ShowMessageToast("请先登录后再操作");
                return;
            }
            try
            {
                var parsed = WebVideoActionParser.ParseWebVideoAction(
                    await videoAPI.Triple(VideoInfo.aid).Request());
                if (!parsed.IsSuccess)
                {
                    Utils.ShowMessageToast(VideoFailureText.For(parsed));
                    return;
                }
                VideoInfo.req_user.like = 1;
                VideoInfo.stat.like += 1;
                VideoInfo.req_user.coin = 1;
                VideoInfo.stat.coin += 1;
                VideoInfo.req_user.favorite = 1;
                VideoInfo.stat.favorite += 1;
                if (VideoInfo.req_user.dislike == 1)
                {
                    VideoInfo.req_user.dislike = 0;
                }

                Utils.ShowMessageToast("三连完成");
            }
            catch (Exception ex)
            {
                var handel = HandelError<object>(ex);
                Utils.ShowMessageToast(handel.message);
            }



        }

        public async void DoCoin(string num)
        {
            if (!SettingHelper.Account.Logined && !await Utils.ShowLoginDialog())
            {
                Utils.ShowMessageToast("请先登录后再操作");
                return;
            }
            var coinNum = Convert.ToInt32(num);
            try
            {
                var parsed = WebVideoActionParser.ParseWebVideoAction(
                    await videoAPI.Coin(VideoInfo.aid, coinNum).Request());
                if (!parsed.IsSuccess)
                {
                    Utils.ShowMessageToast(VideoFailureText.For(parsed));
                    return;
                }
                if (VideoInfo.req_user.coin == 1)
                {
                    VideoInfo.req_user.coin = 0;
                    VideoInfo.stat.coin -= coinNum;
                }
                else
                {
                    VideoInfo.req_user.coin = 1;
                    VideoInfo.stat.coin += coinNum;
                }
                Utils.ShowMessageToast(string.IsNullOrEmpty(parsed.Value) ? "操作成功" : parsed.Value);
            }
            catch (Exception ex)
            {
                var handel = HandelError<object>(ex);
                Utils.ShowMessageToast(handel.message);
            }
          


        }
        public async void DoFavorite(List<string> fav_ids,string avid)
        {
            if (!SettingHelper.Account.Logined && !await Utils.ShowLoginDialog())
            {
                Utils.ShowMessageToast("请先登录后再操作");
                return;
            }
            try
            {
                var parsed = WebVideoActionParser.ParseWebVideoAction(
                    await favoriteAPI.AddFavorite(fav_ids, avid).Request());
                if (!parsed.IsSuccess)
                {
                    Utils.ShowMessageToast(VideoFailureText.For(parsed));
                    return;
                }
                if (fav_ids.Count != 0)
                {
                    VideoInfo.req_user.favorite = 1;
                    VideoInfo.stat.favorite += 1;
                }
                else
                {
                    VideoInfo.req_user.favorite = 0;
                    VideoInfo.stat.favorite -= 1;
                }
                if (string.IsNullOrEmpty(parsed.Value))
                {
                    Utils.ShowMessageToast("操作成功");
                    await LoadFavorite(avid);
                }
                else
                {
                    Utils.ShowMessageToast(parsed.Value);
                }
            }
            catch (Exception ex)
            {
                var handel = HandelError<object>(ex);
                Utils.ShowMessageToast(handel.message);
            }
           
           

        }
      

        public async void DoAttentionUP()
        {
            var result = await AttentionUP(VideoInfo.owner.mid, VideoInfo.req_user.attention==1?2:1);
            if (result)
            {
                if (VideoInfo.req_user.attention == 1)
                {
                    VideoInfo.req_user.attention = -999;
                    if (VideoInfo.owner_ext != null && VideoInfo.owner_ext.fans > 0) VideoInfo.owner_ext.fans -= 1;
                }
                else
                {
                    VideoInfo.req_user.attention = 1;
                    if (VideoInfo.owner_ext != null && VideoInfo.owner_ext.fans > 0) VideoInfo.owner_ext.fans += 1;
                }
            }
        }
        public async Task<bool> AttentionUP(string mid,int mode)
        {
            if (!SettingHelper.Account.Logined && !await Utils.ShowLoginDialog())
            {
                Utils.ShowMessageToast("请先登录后再操作");
                return false;
            }
          
            try
            {
                var parsed = WebVideoActionParser.ParseWebVideoAction(
                    await videoAPI.Attention(mid, mode.ToString()).Request());
                if (!parsed.IsSuccess)
                {
                    Utils.ShowMessageToast(VideoFailureText.For(parsed));
                    return false;
                }
                Utils.ShowMessageToast("操作成功");
                return true;
            }
            catch (Exception ex)
            {
                var handel = HandelError<object>(ex);
                Utils.ShowMessageToast(handel.message);
                return false;
            }



        }
    
        public async Task<string> GetPlayUrl()
        {
            try
            {
                var results = await PlayerAPI.VideoPlayUrl(VideoInfo.aid,VideoInfo.pages[0].cid,80,false).Request();
                if (results.status)
                {
                    var data = await results.GetJson<ApiDataModel<JObject>>();
                    if (data.success)
                    {
                        return data.data["durl"][0]["url"].ToString();
                    }
                    else
                    {
                        Utils.ShowMessageToast(data.message);
                        return "";
                    }
                }
                else
                {
                    Utils.ShowMessageToast(results.message);
                    return "";
                }
            }
            catch (Exception ex)
            {

                var handel = HandelError<string>(ex);
                Utils.ShowMessageToast(handel.message);
                return "";
            }
        }
    
    }

    public class VideoDetailModel:IModules
    {
        public string bvid { get; set; }
        public string aid { get; set; }
        /// <summary>
        /// 视频数量
        /// </summary>
        public int videos { get; set; }
        /// <summary>
        /// 分区ID
        /// </summary>
        public int tid { get; set; }
        /// <summary>
        /// 分区名
        /// </summary>
        public string tname { get; set; }
        /// <summary>
        /// 封面
        /// </summary>
        public string pic { get; set; }
        public string title { get; set; }
        /// <summary>
        /// 上传时间
        /// </summary>
        public long pubdate { get; set; }
        /// <summary>
        /// 创建时间
        /// </summary>
        public long ctime { get; set; }
        /// <summary>
        /// 简介
        /// </summary>
        public string desc { get; set; }

        public long attribute { get; set; }
        public int state { get; set; }
        /// <summary>
        /// 时长
        /// </summary>
        public int duration { get; set; }
        public VideoDetailRightsModel rights { get; set; }
        public string dynamic { get; set; }
        /// <summary>
        /// UP主
        /// </summary>
        public VideoDetailOwnerModel owner { get; set; }
        /// <summary>
        /// UP主信息扩展
        /// </summary>
        public VideoDetailOwnerExtModel owner_ext { get; set; }
        /// <summary>
        /// 数据
        /// </summary>
        public VideoDetailStatModel stat { get; set; }
        /// <summary>
        /// 用户数据
        /// </summary>
        public VideoDetailReqUserModel req_user { get; set; }
        /// <summary>
        /// Tag
        /// </summary>
        public List<VideoDetailTagModel> tag { get; set; }
       

        private List<VideoDetailRelatesModel> _relates;
        /// <summary>
        /// 推荐。null 是合法值：线上可能直接给 relates: null，related 读失败时页面也保留空表。
        /// 这里选"空安全 setter"而不是"mapper 绕过 setter"，因为直接反序列化这份模型的路径
        /// （plan/tests/c1 的夹具、任何按 wire 形状读它的调用点）同样会走 setter，绕过只修得了一条路。
        /// </summary>
        public List<VideoDetailRelatesModel> relates
        {
            get { return _relates; }
            set { _relates = value == null ? null : value.Where(x => x != null && !string.IsNullOrEmpty(x.aid)).ToList(); }
        }



        public string share_subtitle { get; set; }
        public string short_link { get; set; }
        public string redirect_url { get; set; }

        public List<VideoDetailPagesModel> pages { get; set; }

        public bool showPages
        {
            get
            {
                return pages != null && pages.Count > 1;
            }
        }
        /// <summary>
        /// 互动视频
        /// </summary>
        public VideoDetailInteractionModel interaction { get; set; }
        public List<VideoDetailStaffModel> staff { get; set; }

        public bool showStaff
        {
            get
            {
                return staff != null;
            }
        }

        public string argue_msg { get; set; }
        public bool showArgueMsg
        {
            get
            {
                return !string.IsNullOrEmpty(argue_msg);
            }
        }
        public VideoDetailHistoryModel history { get; set; }

        /// <summary>
        /// 合集，视频不属于合集时为空
        /// </summary>
        [JsonProperty("ugc_season")]
        public VideoUgcSeason UgcSeason { get; set; }

        public bool showUgcSeason
        {
            get
            {
                return UgcSeason != null && UgcSeason.Sections != null && UgcSeason.Sections.Count > 0;
            }
        }
    } 
    public class VideoDetailRightsModel
    {
        public int bp { get; set; }
        /// <summary>
        /// 能不能充电
        /// </summary>
        public int elec { get; set; }
        /// <summary>
        /// 能不能下载
        /// </summary>
        public int download { get; set; }
        /// <summary>
        /// 是不是电影
        /// </summary>
        public int movie { get; set; }
        /// <summary>
        /// 是不是付费
        /// </summary>
        public int pay { get; set; }
    }
    public class VideoDetailOwnerModel
    {
        public string mid { get; set; }
        public string name { get; set; }
        public string face { get; set; }
    }
    public class VideoDetailOwnerExtModel
    {
        /// <summary>
        /// 粉丝数
        /// </summary>
        public int fans { get; set; }

        /// <summary>
        /// 粉丝数是否可用(接口未返回/读取失败时为 false, 用于隐藏粉丝数标签)
        /// </summary>
        public bool HasFans => fans > 0;

        /// <summary>
        /// 大会员信息
        /// </summary>
       public VideoDetailOwnerExtVipModel vip { get; set; }
        /// <summary>
        /// 认证信息
        /// </summary>
        public VideoDetailOwnerExtOfficialVerifyModel official_verify { get; set; }
    }
    public class VideoDetailOwnerExtVipModel
    {
        public int vipType { get; set; }
        public int vipStatus { get; set; }

    }
    public class VideoDetailOwnerExtOfficialVerifyModel
    {
        /// <summary>
        /// 0个人认证,1企业认证
        /// </summary>
        public int type { get; set; }
        public string desc { get; set; }

    }
    public class VideoDetailStatModel : IModules
    {
        public string aid { get; set; }
        /// <summary>
        /// 播放
        /// </summary>
        public int view { get; set; }
        /// <summary>
        /// 弹幕
        /// </summary>
        public int danmaku { get; set; }
        /// <summary>
        /// 评论
        /// </summary>
        public int reply { get; set; }

        private int _favorite;
        /// <summary>
        /// 收藏
        /// </summary>
        public int favorite
        {
            get { return _favorite; }
            set { _favorite = value;DoPropertyChanged("favorite"); }
        }

        private int _coin;
        /// <summary>
        /// 投币
        /// </summary>
        public int coin
        {
            get { return _coin; }
            set { _coin = value; DoPropertyChanged("coin"); }
        }
        private int _share;
        /// <summary>
        /// 分享
        /// </summary>
        public int share
        {
            get { return _share; }
            set { _share = value; DoPropertyChanged("share"); }
        }

        private int _like;
        /// <summary>
        /// 点赞
        /// </summary>
        public int like
        {
            get { return _like; }
            set { _like = value; DoPropertyChanged("like"); }
        }
        /// <summary>
        /// 不喜欢，固定0
        /// </summary>
        public int dislike { get; set; }
    }
    public class VideoDetailReqUserModel:IModules
    {
        private int _attention;
        /// <summary>
        /// 是否关注
        /// </summary>
        public int attention
        {
            get { return _attention; }
            set { _attention = value;DoPropertyChanged("attention"); }
        }

        private int _guest_attention;
        /// <summary>
        /// 是否特别关注
        /// </summary>
        public int guest_attention
        {
            get { return _guest_attention; }
            set { _guest_attention = value; DoPropertyChanged("guest_attention"); }
        }

        private int _favorite;
        /// <summary>
        /// 是否收藏
        /// </summary>
        public int favorite
        {
            get { return _favorite; }
            set { _favorite = value; DoPropertyChanged("favorite"); }
        }

        private int _like;
        /// <summary>
        /// 是否点赞
        /// </summary>
        public int like
        {
            get { return _like; }
            set { _like = value; DoPropertyChanged("like"); }
        }

        private int _coin;
        /// <summary>
        /// 是否投币
        /// </summary>
        public int coin
        {
            get { return _coin; }
            set { _coin = value; DoPropertyChanged("coin"); }
        }

        private int _dislike;
        /// <summary>
        /// 是否不喜欢
        /// </summary>
        public int dislike
        {
            get { return _dislike; }
            set { _dislike = value; DoPropertyChanged("dislike"); }
        }
    }

    public class VideoDetailTagModel
    {
        public int tag_id { get; set; }
        public string tag_name { get; set; }
    }

    public class VideoDetailRelatesModel
    {
        public string aid { get; set; }
        public string pic { get; set; }
        public string title { get; set; }
        public VideoDetailOwnerModel owner { get; set; }
        public VideoDetailStatModel stat { get; set; }
    }

    public class VideoDetailPagesModel
    {
        public string cid { get; set; }
        public string page { get; set; }
        public string part { get; set; }
        public int duration { get; set; }
        public string dmlink { get; set; }
        public string download_title { get; set; }
        public string download_subtitle { get; set; }
    }
    public class VideoDetailInteractionModel
    {
        public int graph_version { get; set; }
        public VideoDetailInteractionHistoryNodeModel history_node { get; set; }

    }
    public class VideoDetailInteractionHistoryNodeModel
    {
        public int node_id { get; set; }
        public string title { get; set; }
        public long cid { get; set; }
    }
    public class VideoDetailStaffModel : IModules
    {
        public string mid { get; set; }
        public string title { get; set; }
        public string face { get; set; }
        public string name { get; set; }
   
        public VideoDetailOwnerExtVipModel vip { get; set; }
        public VideoDetailOwnerExtOfficialVerifyModel official_verify { get; set; }
        private int _attention;

        public int attention
        {
            get { return _attention; }
            set { _attention = value; DoPropertyChanged("attention"); }
        }

    }

    public class VideoDetailHistoryModel
    {
        public string cid { get; set; }
        public int progress { get; set; }

    }
}
