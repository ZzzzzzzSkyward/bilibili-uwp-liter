using BiliLite.Api;
using BiliLite.Helpers;
using BiliLite.Parsing.Season;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
namespace BiliLite.Modules
{
    public class SeasonDetailVM : IModules
    {
        readonly Api.SeasonApi seasonApi;
        public SeasonDetailVM()
        {
            seasonApi = new Api.SeasonApi();
            FollowCommand = new RelayCommand(DoFollow);
        }
        private SeasonDetailModel _Detail;

        public SeasonDetailModel Detail
        {
            get { return _Detail; }
            set { _Detail = value; DoPropertyChanged("Detail"); }
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
        private bool _ShowError = false;
        public bool ShowError
        {
            get { return _ShowError; }
            set { _ShowError = value; DoPropertyChanged("ShowError"); }
        }
        private string _errorMsg = "";

        public string ErrorMsg
        {
            get { return _errorMsg; }
            set { _errorMsg = value; DoPropertyChanged("ErrorMsg"); }
        }

        public ICommand FollowCommand { get; private set; }

        private List<SeasonDetailEpisodeModel> _episodes;
        public List<SeasonDetailEpisodeModel> Episodes
        {
            get { return _episodes; }
            set { _episodes = value; DoPropertyChanged("Episodes"); }
        }
        private bool _showEpisodes = false;
        public bool ShowEpisodes
        {
            get { return _showEpisodes; }
            set { _showEpisodes = value; DoPropertyChanged("ShowEpisodes"); }
        }

        private List<SeasonDetailEpisodeModel> _previews;
        public List<SeasonDetailEpisodeModel> Previews
        {
            get { return _previews; }
            set { _previews = value; DoPropertyChanged("Previews"); }
        }
        private bool _showPreview = false;
        public bool ShowPreview
        {
            get { return _showPreview; }
            set { _showPreview = value; DoPropertyChanged("ShowPreview"); }
        }
        private bool _nothingPlay = false;
        public bool NothingPlay
        {
            get { return _nothingPlay; }
            set { _nothingPlay = value; DoPropertyChanged("NothingPlay"); }
        }

        public async Task LoadSeasonDetail(string ep_id)
        {
            try
            {
                Loaded = false;
                Loading = true;
                ShowError = false;

                //整页信息（标题/封面/评分/地区/追番状态）只在 pgc/view/web/season 上，
                //ep/list 只有片单和分区，拿它当整页信息会让 Detail 全空
                var seasonDetail = await seasonApi.Detail(ep_id, SeasonIdType.EpId).Request();
                var seasonData = await seasonDetail.GetResult<SeasonDetailModel>();
                var detail = seasonData != null && seasonData.success ? seasonData.result : null;
                if (detail == null)
                {
                    //整页信息拿不到不挡播放：片单照旧发布，只是元数据面板空着
                    LogHelper.Log("番剧详情解析失败 ep=" + ep_id + " endpoint=pgc/view/web/season", LogType.ERROR);
                }

                var parsed = WebSeasonEpListParser.ParseWebSeasonEpList(
                    await seasonApi.EpList(ep_id, SeasonIdType.EpId).Request());
                if (!parsed.IsSuccess)
                {
                    //解析层只给结论，用户看什么由这里定：错误面板，不弹 toast
                    ShowError = true;
                    ErrorMsg = parsed.Message;
                    return;
                }

                //通过解析才发布；空但合法的季照样发布，由页面显示空状态
                var season = WebSeasonEpListMapper.MapWebSeasonEpList(parsed.Value);
                Detail = detail ?? season.Detail;
                //season 只给 season_title 时页面标题也得有字
                if (string.IsNullOrEmpty(Detail.title))
                {
                    Detail.title = Detail.season_title;
                }
                //下载面板与历史行为都吃 ep/list 的合并列表（含分区预告、已按 ep 号去重），
                //season 自带的 episodes 只有正片，不能拿它顶替
                if (season.Detail.episodes != null && season.Detail.episodes.Count > 0)
                {
                    Detail.episodes = season.Detail.episodes;
                }
                Episodes = season.Episodes;
                ShowEpisodes = Episodes.Count > 0;
                Previews = season.Previews;
                ShowPreview = Previews.Count > 0;
                NothingPlay = !ShowEpisodes && !ShowPreview;
                Loaded = true;
            }
            catch (Exception ex)
            {
                var handel = HandelError<AnimeHomeModel>(ex);
                //异常只落到错误面板：解析路径不弹 toast
                ShowError = true;
                ErrorMsg = handel.message;
            }
            finally
            {
                Loading = false;
            }
        }

        public async void DoFollow()
        {
            if (!SettingHelper.Account.Logined && !await Utils.ShowLoginDialog())
            {
                Utils.ShowMessageToast("请先登录后再操作");
                return;
            }
        }

    }
    public class SeasonDetailModel
    {
        public SeasonDetailUserStatusModel user_status { get; set; }
        public string origin_name { get; set; }
        public int season_id { get; set; }
        public string season_title { get; set; }
        public string title { get; set; }
        public string cover { get; set; }
        public string evaluate { get; set; }
        public string alias { get; set; }
        public string badge { get; set; }
        public int status { get; set; }
        public string subtitle { get; set; }
        public bool show_badge { get { return !string.IsNullOrEmpty(badge); } }
        public string link { get; set; }
        public string short_link { get; set; }
        public string square_cover { get; set; }
        public int media_id { get; set; }
        public int mode { get; set; }
        public SeasonDetailUpInfoModel up_info { get; set; }
        public List<SeasonDetailAreaItemModel> areas { get; set; }
        public string area
        {
            get
            {
                var r = "";
                if (areas != null)
                {
                    foreach (var item in areas)
                    {
                        r += item.name + " ";
                    }
                    return r;
                }
                else
                {
                    return "";
                }
            }
        }
        public List<SeasonDetailEpisodeModel> episodes { get; set; }
        public SeasonDetailRatingModel rating { get; set; }
        public bool show_rating
        {
            get
            {
                return rating != null;
            }
        }
        public SeasonDetailPublishModel publish { get; set; }
        public List<SeasonDetailSeasonItemModel> seasons { get; set; }
        public bool show_seasons
        {
            get
            {
                return seasons != null && seasons.Count > 1;
            }
        }
        public SeasonDetailSeasonItemModel current_season
        {
            get
            {
                if (seasons != null)
                {
                    return seasons.FirstOrDefault(x => x.season_id == season_id);
                }
                else
                {
                    return null;
                }
            }
        }
        //public List<SeasonDetailStyleItemModel> styles { get; set; }
        public SeasonDetailStatModel stat { get; set; }
        public int total { get; set; }
        public int type { get; set; }
        public List<SeasonDetailSectionItemModel> section { get; set; }
        public SeasonDetailPaymentModel payment { get; set; }
        public bool show_payment
        {
            get
            {
                return payment != null && payment.dialog != null;
            }
        }
    }
    public class SeasonDetailUpInfoModel
    {
        public long mid{ get; set; }
        public string uname { get; set; }
    }
    public class SeasonDetailPaymentModel
    {
        public string price { get; set; }
        public string tv_price { get; set; }
        public SeasonDetailPaymentDialogModel dialog { get; set; }
    }
    public class SeasonDetailPaymentDialogModel
    {
        public string title { get; set; }
        public string desc { get; set; }

    }
    public class SeasonDetailSectionItemModel
    {
        public string title { get; set; }
        public long id { get; set; }
        public int type { get; set; }
        public List<SeasonDetailEpisodeModel> episodes { get; set; }
    }
    public class SeasonDetailEpisodeModel
    {
        public string aid { get; set; }
        public string bvid { get; set; }
        public string cid { get; set; }
        public int badge_type { get; set; }
        public string badge { get; set; }
        public bool show_badge
        {
            get
            {
                return !string.IsNullOrEmpty(badge);
            }
        }
        public string cover { get; set; }

        private int? _id;
        public int id
        {
            get
            {
                if (_id == null && ep_id != null)
                {
                    return ep_id.Value;
                }
                return _id.Value;
            }
            set { _id = value; }
        }
        private int? _status;
        public int status
        {
            get
            {
                if (_status == null && episode_status != null)
                {
                    return episode_status.Value;
                }
                return _status.Value;
            }
            set { _status = value; }
        }

        private string _title;

        public string title
        {
            get
            {
                if (_title == null && index != null)
                {
                    return index;
                }
                return _title;
            }
            set { _title = value; }
        }


        private string _long_title;

        public string long_title
        {
            get
            {
                if (_long_title == null && index_title != null)
                {
                    return index_title;
                }
                return _long_title;
            }
            set { _long_title = value; }
        }


        public int? ep_id { get; set; }
        public int? episode_status { get; set; }
        public string index { get; set; }
        public string index_title { get; set; }
        public int section_type { get; set; }
        public bool IsPreview { get { return section_type != 0; } }
    }
    public class SeasonDetailUserStatusModel : IModules
    {

        private int _follow;

        public int follow
        {
            get { return _follow; }
            set { _follow = value; DoPropertyChanged("follow"); }
        }

        public int follow_bubble { get; set; }
        public int follow_status { get; set; }
        public int pay { get; set; }
        public int vip { get; set; }
        public int sponsor { get; set; }
        public int vip_frozen { get; set; }
        public SeasonDetailUserStatusProgressModel progress { get; set; }
    }
    public class SeasonDetailUserStatusProgressModel
    {
        public string last_ep_index { get; set; }
        public int last_ep_id { get; set; }
        public int last_time { get; set; }
    }
    public class SeasonDetailStatModel
    {
        public int coins { get; set; }
        public int danmakus { get; set; }
        public int favorites { get; set; }
        public string followers { get; set; }
        public string play { get; set; }
        public int reply { get; set; }
        public int share { get; set; }
        public int views { get; set; }
    }
    public class SeasonDetailStyleItemModel
    {
        public string id { get; set; }
        public string name { get; set; }
        public string url { get; set; }
    }
    public class SeasonDetailPublishModel
    {
        public int is_finish { get; set; }
        public int is_started { get; set; }
        public int weekday { get; set; }
        public string pub_time { get; set; }
        public string pub_time_show { get; set; }
        public string release_date_show { get; set; }
        public string time_length_show { get; set; }
    }
    public class SeasonDetailSeasonItemModel
    {
        public int season_id { get; set; }
        public string cover { get; set; }
        public string season_title { get; set; }
        public string title { get; set; }
    }
    public class SeasonDetailRatingModel
    {
        public int count { get; set; }
        public double score { get; set; }
        public double score_5
        {
            get
            {
                return score / 2;
            }
        }
    }
    public class SeasonDetailAreaItemModel
    {
        public string id { get; set; }
        public string name { get; set; }
    }

}
