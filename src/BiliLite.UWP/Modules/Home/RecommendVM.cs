using BiliLite.Parsing.Recommend;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BiliLite.Helpers;
using System.Collections.ObjectModel;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Windows.Input;

namespace BiliLite.Modules
{
    public class RecommendVM : IModules
    {
        readonly Api.Home.RecommendAPI recommendAPI;
        public RecommendVM()
        {
            recommendAPI = new Api.Home.RecommendAPI();
            Banner = new ObservableCollection<RecommendBannerItemModel>();
            RefreshCommand = new RelayCommand(Refresh);
            LoadMoreCommand = new RelayCommand(LoadMore);
            RetryCommand = new RelayCommand(Retry);
        }
        public ICommand RefreshCommand { get; private set; }
        public ICommand LoadMoreCommand { get; private set; }

        private bool _loading = true;
        public bool Loading
        {
            get { return _loading; }
            set { _loading = value; DoPropertyChanged("Loading"); }
        }

        private ObservableCollection<RecommendBannerItemModel> _banner;

        public ObservableCollection<RecommendBannerItemModel> Banner
        {
            get { return _banner; }
            set { _banner = value; DoPropertyChanged("Banner"); }
        }

        private ObservableCollection<RecommendItemModel> _items;

        public ObservableCollection<RecommendItemModel> Items
        {
            get { return _items; }
            set { _items = value; DoPropertyChanged("Items"); }
        }


        private bool requestPending;
        private bool readyState;
        private bool retryAppend;
        private string continuation;
        public ICommand RetryCommand { get; private set; }
        private string statusText = "";
        public string StatusText { get { return statusText; } private set { statusText = value; DoPropertyChanged("StatusText"); } }
        private bool autoLoad;
        public bool AutoLoad { get { return autoLoad; } private set { autoLoad = value; DoPropertyChanged("AutoLoad"); } }

        public Task GetRecommend(string idx = "0") { return LoadAsync(idx, idx != "0"); }
        private async Task LoadAsync(string idx, bool append)
        {
            if (requestPending) return;
            var hadCommittedFeed = Items != null;
            readyState = false;
            requestPending = true;
            Loading = true;
            AutoLoad = false;
            StatusText = "";
            try
            {
                bool optionalBannerFailure;
                var parsed = RecommendResponseParser.Read(await recommendAPI.Recommend(idx).Request(), out optionalBannerFailure);
                if (!parsed.IsSuccess) throw new InvalidOperationException(parsed.Stage + ": " + parsed.Message);
                if (optionalBannerFailure) LogHelper.Log("推荐横幅解析失败", LogType.ERROR);
                var staged = new ObservableCollection<RecommendItemModel>();
                var banners = new ObservableCollection<RecommendBannerItemModel>();
                foreach (var item in parsed.Value)
                {
                    if (item.card_goto == "banner")
                    {
                        if (!SettingHelper.GetValue("dontloadbanner", false))
                        {
                            try
                            {
                                var optional = new List<RecommendBannerItemModel>();
                                foreach (var entry in item.banner_item ?? new JArray())
                                {
                                    var token = entry[(string)entry["type"] == "static" ? "static_banner" : "ad_banner"];
                                    if (token != null) optional.Add(token.ToObject<RecommendBannerItemModel>());
                                }
                                foreach (var banner in optional) if (banner != null) banners.Add(banner);
                            }
                            catch (Exception ex) { LogHelper.Log("推荐横幅解析失败", LogType.ERROR, ex); }
                        }
                        continue;
                    }
                    if (item.showAD) continue;
                    if (item.three_point_v2 != null && item.three_point_v2.Count > 0 && item.card_goto == "av")
                        item.three_point_v2.Insert(1, new RecommendThreePointV2ItemModel { idx = item.idx, url = $"https://b23.tv/av{item.param}", title = "使用浏览器打开", type = "browser" });
                    staged.Add(item);
                }
                retryAppend = false;
                var next = staged.LastOrDefault(x => !string.IsNullOrWhiteSpace(x.idx))?.idx;
                if (!append)
                {
                    Items = staged;
                    Banner = banners;
                }
                else if (staged.Count > 0 && !string.IsNullOrWhiteSpace(next) && next != continuation)
                    foreach (var item in staged) Items.Add(item);
                else staged.Clear();
                if (!append || staged.Count > 0) continuation = next;
                AutoLoad = Items != null && Items.Count > 0 && !string.IsNullOrWhiteSpace(continuation) && (!append || staged.Count > 0);
                StatusText = Items == null || Items.Count == 0 ? "暂无推荐内容" : (append && staged.Count == 0 ? "暂时没有更多推荐" : "");
                readyState = !append || staged.Count > 0;
            }
            catch (Exception ex)
            {
                retryAppend = append;
                StatusText = append ? "加载更多失败" : (hadCommittedFeed ? "推荐刷新失败" : "推荐加载失败");
                LogHelper.Log("推荐加载失败", LogType.ERROR, ex);
            }
            finally { requestPending = false; Loading = false; }
        }
        public async void LoadMore()
        {
            try
            {
                if (Items == null || Items.Count == 0 || requestPending) return;
                var idx = continuation ?? Items.LastOrDefault(x => !string.IsNullOrWhiteSpace(x.idx))?.idx;
                if (string.IsNullOrWhiteSpace(idx)) return;
                await LoadAsync(idx, true);
            }
            catch (Exception ex) { LogHelper.Log("推荐追加失败", LogType.ERROR, ex); }
        }
        public async void Refresh()
        {
            try { await LoadAsync("0", false); }
            catch (Exception ex) { LogHelper.Log("推荐刷新失败", LogType.ERROR, ex); }
        }
        private async void Retry()
        {
            try
            {
                if (retryAppend) { var idx = continuation ?? Items?.LastOrDefault(x => !string.IsNullOrWhiteSpace(x.idx))?.idx; if (!string.IsNullOrWhiteSpace(idx)) await LoadAsync(idx, true); }
                else await LoadAsync("0", false);
            }
            catch (Exception ex) { LogHelper.Log("推荐重试失败", LogType.ERROR, ex); }
        }

        public async Task Dislike(string idx, RecommendThreePointV2ItemModel threePointV2Item, RecommendThreePointV2ItemReasonsModel itemReasons)
        {
            try
            {
                var collection = Items;
                var recommendItem = collection?.FirstOrDefault(x => x.idx == idx);
                if (recommendItem == null) return;
                if (!SettingHelper.Account.Logined && !await Utils.ShowLoginDialog())
                {
                    Utils.ShowMessageToast("请先登录");
                    return;
                }
                if (!ReferenceEquals(Items, collection) || !collection.Contains(recommendItem)) return;
                var api = recommendAPI.Dislike(_goto: recommendItem.card_goto, id: recommendItem.param, mid: recommendItem.args.up_id, reason_id: itemReasons?.id ?? 0, rid: recommendItem.args.rid, tag_id: recommendItem.args.tid);
                if (threePointV2Item.type == "feedback")
                {
                    api = recommendAPI.Feedback(_goto: recommendItem.card_goto, id: recommendItem.param, mid: recommendItem.args.up_id, feedback_id: itemReasons?.id ?? 0, rid: recommendItem.args.rid, tag_id: recommendItem.args.tid);
                }
                var result = await api.Request();
                if (result.status)
                {
                    var obj = result.GetJObject();
                    if (RecommendResponseParser.ActionSucceeded(result))
                    {
                        if (ReferenceEquals(Items, collection) && collection.Contains(recommendItem))
                        {
                            collection.Remove(recommendItem);
                            if (!requestPending && readyState)
                            {
                                continuation = collection.LastOrDefault(x => !string.IsNullOrWhiteSpace(x.idx))?.idx;
                                AutoLoad = collection.Count > 0 && !string.IsNullOrWhiteSpace(continuation);
                                StatusText = collection.Count == 0 ? "暂无推荐内容" : "";
                            }
                        }
                    }
                    else
                    {
                        Utils.ShowMessageToast(obj["message"].ToString());
                    }
                }
                else
                {
                    Utils.ShowMessageToast(result.message);
                }
            }
            catch (Exception ex)
            {
                var handel = HandelError<AnimeHomeModel>(ex);
                Utils.ShowMessageToast(handel.message);
            }
        }


    }
    public class RecommendItemModel
    {
        //public ObservableCollection<RecommendBannerItemModel> banner_item { get; set; }
        public JArray banner_item { get; set; }
        private string _title = "";
        public string title
        {
            get
            {
                if (string.IsNullOrEmpty(_title) && string.IsNullOrEmpty(uri))
                {
                    if (ad_info == null)
                    {
                        return "你追的番剧更新啦~";
                    }
                    else
                    {
                        return ad_info.creative_content?.title ?? "";
                    }
                }
                return _title;
            }
            set { _title = value; }
        }

        public string _cover { get; set; }

        public string cover
        {
            get
            {
                if (string.IsNullOrEmpty(_cover) && ad_info != null)
                {
                    return ad_info.creative_content.image_url;
                }
                else
                {
                    return _cover;
                }
            }
            set
            {
                _cover = value;
            }
        }

        public string uri { get; set; }
        public string param { get; set; }
        public string card_goto { get; set; }

        public string idx { get; set; }

        private List<RecommendThreePointV2ItemModel> _three_point_v2;

        public List<RecommendThreePointV2ItemModel> three_point_v2
        {
            get
            {
                if (_three_point_v2 != null)
                {
                    foreach (var item in _three_point_v2)
                    {
                        item.idx = idx;
                    }
                }

                return _three_point_v2;
            }
            set { _three_point_v2 = value; }
        }


        public RecommendItemArgsModel args { get; set; }

        public RecommendRcmdReasonStyleModel rcmd_reason_style { get; set; }
        public RecommendDescButtonModel desc_button { get; set; }
        public RecommendADInfoModel ad_info { get; set; }
        public string cover_left_text_1 { get; set; }
        public string cover_left_text_2 { get; set; }
        public int cover_left_icon_1 { get; set; }

        public int cover_left_icon_2 { get; set; }
        public string left_text
        {
            get
            {
                return $"{iconToText(cover_left_icon_1)}{cover_left_text_1 ?? ""} {iconToText(cover_left_icon_2)}{cover_left_text_2 ?? ""}";
            }
        }
        public string cover_right_text { get; set; }

        public string badge { get; set; }
        public bool showBadge
        {
            get
            {
                return !string.IsNullOrEmpty(badge);
            }
        }

        public bool showCoverText
        {
            get
            {
                return !string.IsNullOrEmpty(cover_left_text_1) || !string.IsNullOrEmpty(cover_left_text_2) || !string.IsNullOrEmpty(cover_right_text);
            }
        }
        public bool showRcmd
        {
            get
            {
                return rcmd_reason_style != null;
            }
        }
        public bool showAD
        {
            get
            {
                return ad_info != null && ad_info.creative_content != null;
            }
        }
        public string bottomText
        {
            get
            {
                if (desc_button != null)
                {
                    return desc_button.text;
                }
                if (card_goto=="live")
                {
                    return args.up_name;
                }
                return args.up_name;
            }
        }
        public string timeText
        {
            get
            {
                return "";
                if (desc_button != null)
                {
                    return desc_button.text;
                }
                if (card_goto=="live")
                {
                    return args.up_name;
                }
            }
        }

        public string iconToText(int icon)
        {
            switch (icon)
            {
                case 1:
                case 6:
                    return "观看:";
                case 2:
                    return "人气:";
                case 3:
                    return "弹幕:";
                case 4:
                    return "追番:";
                case 7:
                    return "评论:";
                default:
                    return "";
            }
        }
    }
    public class RecommendBannerItemModel
    {
        public long id { get; set; }
        public string title { get; set; }
        public string image { get; set; }


        public string hash { get; set; }

        public string uri { get; set; }

        public string request_id { get; set; }
        /// <summary>
        /// Server_type
        /// </summary>
        public int server_type { get; set; }
        /// <summary>
        /// Resource_id
        /// </summary>
        public int resource_id { get; set; }
        /// <summary>
        /// Index
        /// </summary>
        public int index { get; set; }
        /// <summary>
        /// Cm_mark
        /// </summary>
        public int cm_mark { get; set; }
    }
    public class RecommendThreePointV2ItemModel
    {

        public string idx { get; set; }
        public string title { get; set; }
        public string type { get; set; }
        public string subtitle { get; set; }
        public string url { get; set; }
        public List<RecommendThreePointV2ItemReasonsModel> reasons { get; set; }

    }
    public class RecommendThreePointV2ItemReasonsModel
    {
        public long id { get; set; }
        public string name { get; set; }

    }
    public class RecommendItemArgsModel
    {
        public string up_id { get; set; }
        public string up_name { get; set; }
        public long rid { get; set; }
        public long tid { get; set; }
        public string tname { get; set; }
        public string rname { get; set; }
        public long aid { get; set; }

    }
    public class RecommendRcmdReasonStyleModel
    {
        public string text { get; set; }
        public string text_color { get; set; }

        public string bg_color { get; set; }

        public string border_color { get; set; }
        public string text_color_night { get; set; }
        public string bg_color_night { get; set; }
        public string border_color_night { get; set; }
        public int bg_style { get; set; }

    }
    public class RecommendDescButtonModel
    {
        public string text { get; set; }
        public string uri { get; set; }
    }
    public class RecommendADInfoModel
    {
        public string creative_id { get; set; }
        public RecommendADInfoCreativeModel creative_content { get; set; }
    }
    public class RecommendADInfoCreativeModel
    {
        public string description { get; set; }
        public string title { get; set; }
        public string image_url { get; set; }
        public string url { get; set; }
        public string click_url { get; set; }
    }
}
