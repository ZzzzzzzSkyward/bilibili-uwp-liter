using BiliLite.Helpers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using BiliLite.Services;
namespace BiliLite.Modules
{
    public class DynamicVM : IModules
    {
        private readonly GrpcService _grpcService;
        private readonly Api.User.DynamicAPI _dynamicAPI;
        private int _page = 1;
        private string _historyOffset;
        private string _updateBaseline;
        //web 聚合动态流自己的翻页游标（data.offset）：与 gRPC 的 historyOffset/updateBaseline、
        //旧 REST 卡的 desc.dynamic_id 分属三套，互不传递
        private string _polymerOffset;
        //本次动态流固定的来源：初始加载/刷新时由选择器与固定顺序共同决定，之后本页及后续页都沿用它，
        //只有同一次请求里前面几路都失败、后面某一路成功时才改到这一路，直到下一次刷新
        private DynamicFeedSource? _pinnedSource;
        //两路各自的失败文本分开存：gRPC 的 grpc-message 只进日志，
        //用户看到的提示只取 REST 那一路的业务/HTTP 信息，且不会被后跑的那一路覆盖
        private string _lastLegacyFailureMessage;
        private string _lastGrpcFailureMessage;
        //REST 动态流自己的翻页游标：上一页最后一张卡的 desc.dynamic_id，
        //与 gRPC 的 historyOffset/updateBaseline 分属两套，互不传递
        private string _restCursor;
        public DynamicVM()
        {
            _grpcService = new GrpcService();
            _dynamicAPI = new Api.User.DynamicAPI();
            dynamicItemDataTemplateSelector = new DynamicItemDataTemplateSelector();
            RefreshCommand = new RelayCommand(Refresh);
            LoadMoreCommand = new RelayCommand(LoadMore);
        }
        public DynamicItemDataTemplateSelector dynamicItemDataTemplateSelector { get; set; }
        private bool _loading = true;
        private bool _canLoadMore;
        private bool _lastPageFailed;
        public bool Loading
        {
            get { return _loading; }
            set { _loading = value; DoPropertyChanged("Loading"); }
        }
        public bool CanLoadMore
        {
            get { return _canLoadMore; }
            set { _canLoadMore = value; DoPropertyChanged("CanLoadMore"); }
        }
        public ICommand RefreshCommand { get; private set; }
        public ICommand LoadMoreCommand { get; private set; }
        private ObservableCollection<DynamicItemModel> _Items;

        public ObservableCollection<DynamicItemModel> Items
        {
            get { return _Items; }
            set { _Items = value; DoPropertyChanged("Items"); }
        }

        public async Task GetDynamicItems(int page = 1)
        {
            try
            {
                Loading = true;
                CanLoadMore = false;
                _lastPageFailed = false;
                _lastLegacyFailureMessage = null;
                _lastGrpcFailureMessage = null;
                if (page == 1)
                {
                    _historyOffset = null;
                    _updateBaseline = null;
                    _polymerOffset = null;
                }
                //来源与上下文都在第一个 await 之前定下来：一次请求只记到这个账号的桶里，
                //中途换账号也不影响已经在途的这次
                var context = DynamicApiSelection.CaptureContext();
                var boundary = page == 1 || !_pinnedSource.HasValue;
                var decision = boundary
                    ? DynamicApiSelection.Choose(context, DynamicApiOperation.HomeVideoFeed, true, "home-feed page=" + page)
                    : null;
                //一屏之内的取数顺序由 HomeFeedChain 决定：gRPC 可用时排第一，否则聚合流第一，旧 REST 永远垫底。
                //翻页只认固定下来的那一路，绝不换源：换源会从第一页重来，成功后就顶替掉已上屏的行
                var chain = boundary
                    ? HomeFeedChain(DynamicApiSelection.ToFeedSource(decision.Source))
                    : new[] { _pinnedSource.Value };
                DynamicApiOutcome firstOutcome = DynamicApiOutcome.Success;
                DynamicApiOutcome lastOutcome = DynamicApiOutcome.Success;
                for (var i = 0; i < chain.Length; i++)
                {
                    var source = chain[i];
                    //换过来的那一路一律从第一页重新取，并整体顶替已发布的页
                    var legPage = i == 0 ? page : 1;
                    var outcome = await RequestFromAsync(source, legPage, i > 0, context, "home-feed page=" + page);
                    if (outcome == DynamicApiOutcome.Success)
                    {
                        //来源在本次动态流的生命周期内固定：后续翻页不再因分数高低而改源
                        _pinnedSource = source;
                        return;
                    }
                    if (i == 0)
                    {
                        firstOutcome = outcome;
                    }
                    lastOutcome = outcome;
                    LogHelper.Log("Home dynamics stage=feed-source-failed page=" + legPage + " source=" + source
                        + " outcome=" + outcome, LogType.DEBUG);
                }
                DynamicApiSelection.LogExhausted(DynamicApiOperation.HomeVideoFeed, firstOutcome, lastOutcome, "home-feed page=" + page);
                if (page > 1)
                {
                    //翻页失败绝不换源：gRPC 兜底会从第一页重来，成功后就顶替掉已上屏的行；
                    //用户正在看的内容也不该被一条错误提示打断，所以这里只记日志
                    CanLoadMore = false;
                    _lastPageFailed = true;
                    LogHelper.Log("Home dynamics stage=feed-failed page=" + page + " source=" + chain[0]
                        + " outcome=" + lastOutcome, LogType.ERROR);
                    return;
                }
                //第一页全部失败：异常文本（只进日志）优先取 REST 那一路，gRPC 的 grpc-message 不参与
                throw new InvalidOperationException(_lastLegacyFailureMessage ?? _lastGrpcFailureMessage ?? "动态请求失败");
            }
            catch (Exception ex)
            {
                CanLoadMore = false;
                _lastPageFailed = true;
                LogHelper.Log("Home dynamics stage=feed-failed page=" + page, LogType.ERROR, ex);
                //只有第一页且 REST 那一路确实失败过才提示：提示内容来自 REST 的业务/HTTP 信息
                if (page == 1 && !string.IsNullOrEmpty(_lastLegacyFailureMessage))
                {
                    Utils.ShowMessageToast(_lastLegacyFailureMessage);
                }
            }
            finally
            {
                Loading = false;
            }
        }

        /// <summary>
        /// 首页视频动态的取数顺序：聚合流永远排在旧 REST 之前（旧接口已经没有游标，翻不了页），
        /// 选择器只决定 gRPC 与聚合流谁先试——gRPC 可用时更快，被拒时（或还在冷却里）直接走聚合流。
        /// </summary>
        private static DynamicFeedSource[] HomeFeedChain(DynamicFeedSource preferred)
        {
            return preferred == DynamicFeedSource.Grpc
                ? new[] { DynamicFeedSource.Grpc, DynamicFeedSource.Polymer, DynamicFeedSource.Legacy }
                : new[] { DynamicFeedSource.Polymer, DynamicFeedSource.Legacy, DynamicFeedSource.Grpc };
        }

        private Task<DynamicApiOutcome> RequestFromAsync(DynamicFeedSource source, int page, bool replaceRows, DynamicApiContext context, string stage)
        {
            switch (source)
            {
                case DynamicFeedSource.Polymer:
                    return RequestPolymerSourceAsync(page, replaceRows);
                case DynamicFeedSource.Grpc:
                    return RequestGrpcSourceAsync(page, replaceRows, context, stage);
                default:
                    return RequestLegacySourceAsync(page == 1 ? null : _restCursor, replaceRows, page, context, stage);
            }
        }

        /// <summary>
        /// web 聚合动态流（x/polymer/web-dynamic/v1/feed/all?type=video）：Cookie 鉴权，
        /// 游标是响应里的 data.offset，只在这一路里传递。
        /// </summary>
        private async Task<DynamicApiOutcome> RequestPolymerSourceAsync(int page, bool replaceRows)
        {
            var loadMore = page > 1 && !string.IsNullOrEmpty(_polymerOffset);
            var requestPage = loadMore ? page : 1;
            var cursor = loadMore ? _polymerOffset : null;
            LogHelper.Log("Home dynamics stage=feed-polymer-request page=" + requestPage + " offset=" + (cursor ?? ""), LogType.DEBUG);
            HttpResults response;
            try
            {
                response = await _dynamicAPI.ArticleV2(requestPage, cursor, "video").Request();
            }
            catch (Exception ex)
            {
                var outcome = DynamicApiSelection.ClassifyRequestFailure(ex);
                if (outcome == DynamicApiOutcome.IgnoredFailure)
                    throw;
                return outcome;
            }
            var data = response != null && response.status ? response.GetJObject() : null;
            var businessCode = data?["code"] == null ? (int?)null : data["code"].ToInt32();
            var envelope = DynamicApiSelection.ClassifyLegacy(response, businessCode);
            if (envelope != DynamicApiOutcome.Success || data["data"] == null)
            {
                //信封合法但没有数据，或者业务码本身不是 0：都算这次没拿到有效页。
                //游标属于"上一次成功"的状态，失败只让翻页停下，不能把它抹掉
                return envelope == DynamicApiOutcome.Success ? DynamicApiOutcome.MalformedResponse : envelope;
            }

            var feedData = data["data"];
            JArray items = null;
            List<DynamicItemModel> mappedItems;
            bool hasMore;
            string nextOffset;
            try
            {
                //信封合法不代表字段都合规：少一层对象或多一个字符串，解码/映射里任何一步都可能抛。
                //这一路只算"没拿到有效页"，交给换源链决定下一步，异常绝不掀掉整个请求
                items = feedData["items"] as JArray;
                mappedItems = DynamicGrpcMapper.MapToHomeVideoItems(items);
                hasMore = feedData["has_more"]?.Value<bool>() ?? false;
                nextOffset = feedData["offset"]?.ToString();
            }
            catch (Exception ex)
            {
                LogHelper.Log("Home dynamics stage=feed-polymer-failed page=" + requestPage
                    + " message=" + ex.Message, LogType.ERROR, ex);
                return DynamicApiOutcome.MalformedResponse;
            }
            LogHelper.Log("Home dynamics stage=items-mapped page=" + requestPage + " sourceCount=" + (items?.Count ?? 0)
                + " mappedCount=" + mappedItems.Count + " hasMore=" + hasMore, LogType.DEBUG);
            if (requestPage == 1 || replaceRows || Items == null)
            {
                Items = new ObservableCollection<DynamicItemModel>(mappedItems);
            }
            else
            {
                foreach (var item in mappedItems)
                {
                    Items.Add(item);
                }
            }
            _polymerOffset = nextOffset;
            //换到聚合流之后另外两套游标作废：三种分页不互传
            _historyOffset = null;
            _updateBaseline = null;
            _restCursor = null;
            _page = requestPage;
            //只有服务端说还有下一页、游标真的前进了、而且本页确实取到了行，才继续翻
            var cursorAdvanced = !string.IsNullOrEmpty(nextOffset) && !string.Equals(nextOffset, cursor, StringComparison.Ordinal);
            CanLoadMore = hasMore && cursorAdvanced && mappedItems.Count > 0;
            LogHelper.Log("Home dynamics stage=items-published source=polymer page=" + _page + " count=" + Items.Count
                + " hasMore=" + hasMore + " offset=" + (nextOffset ?? "") + " canLoadMore=" + CanLoadMore, LogType.DEBUG);
            return DynamicApiOutcome.Success;
        }

        /// <summary>
        /// 只有从第一页重来、或者整批换掉行（换源）时才作废 gRPC 游标。
        /// 追加（page > 1 且不替换行）失败必须留住上一次成功页的 historyOffset/updateBaseline，
        /// 否则 footer 重试会把 null 塞进 protobuf 的字符串字段，那一页再也接不上。
        /// </summary>
        private void ClearGrpcCursorOnRestart(int page, bool replaceRows)
        {
            if (page > 1 && !replaceRows)
            {
                return;
            }
            _historyOffset = null;
            _updateBaseline = null;
        }

        /// <summary>
        /// gRPC 视频动态流：游标是 historyOffset/updateBaseline。
        /// 这套游标只在本路里传递，REST 侧绝不会收到它们。
        /// </summary>
        private async Task<DynamicApiOutcome> RequestGrpcSourceAsync(int page, bool replaceRows, DynamicApiContext context, string stage)
        {
            ClearGrpcCursorOnRestart(page, replaceRows);
            var attempt = DynamicApiSelection.CreateAttempt(context, DynamicApiOperation.HomeVideoFeed, DynamicApiSource.Grpc);
            var stopwatch = Stopwatch.StartNew();
            Bilibili.App.Dynamic.V2.DynVideoReply reply;
            try
            {
                LogHelper.Log("Home dynamics stage=" + stage + " rpc=Dynamic/DynVideo page=" + page
                    + " hasHistoryOffset=" + !string.IsNullOrEmpty(_historyOffset)
                    + " hasUpdateBaseline=" + !string.IsNullOrEmpty(_updateBaseline), LogType.DEBUG);
                reply = await _grpcService.GetDynVideo(page, _historyOffset, _updateBaseline);
            }
            catch (Exception ex)
            {
                var outcome = DynamicApiSelection.ClassifyRequestFailure(ex);
                DynamicApiSelection.Record(attempt, outcome, stopwatch.Elapsed, stage);
                if (outcome == DynamicApiOutcome.IgnoredFailure)
                    throw;
                ClearGrpcCursorOnRestart(page, replaceRows);
                _lastGrpcFailureMessage = ex.Message;
                return outcome;
            }
            if (reply == null || reply.DynamicList == null)
            {
                DynamicApiSelection.Record(attempt, DynamicApiOutcome.MalformedResponse, stopwatch.Elapsed, stage);
                ClearGrpcCursorOnRestart(page, replaceRows);
                _lastGrpcFailureMessage = "动态请求失败：响应无法解析";
                return DynamicApiOutcome.MalformedResponse;
            }
            DynamicApiSelection.Record(attempt, DynamicApiOutcome.Success, stopwatch.Elapsed, stage);

            var dynamicList = reply.DynamicList;
            var mappedItems = DynamicGrpcMapper.MapToHomeItems(dynamicList.List);
            LogHelper.Log("Home dynamics stage=items-mapped page=" + page + " sourceCount=" + dynamicList.List.Count
                + " mappedCount=" + mappedItems.Count + " hasMore=" + dynamicList.HasMore, LogType.DEBUG);
            if (page == 1 || replaceRows || Items == null)
            {
                Items = new ObservableCollection<DynamicItemModel>(mappedItems);
            }
            else
            {
                foreach (var item in mappedItems)
                {
                    Items.Add(item);
                }
            }
            _historyOffset = dynamicList.HistoryOffset;
            _updateBaseline = dynamicList.UpdateBaseline;
            //换回 gRPC 之后 REST 游标作废：两套分页不互传
            _restCursor = null;
            _page = page;
            CanLoadMore = dynamicList.HasMore;
            LogHelper.Log("Home dynamics stage=items-published source=grpc page=" + _page + " count=" + Items.Count, LogType.DEBUG);
            return DynamicApiOutcome.Success;
        }

        /// <summary>
        /// REST 兜底：第一页取 dynamic_new，后续页取 dynamic_history 并带上上一页最后一张卡的 desc.dynamic_id。
        /// </summary>
        private async Task<DynamicApiOutcome> RequestLegacySourceAsync(string cursor, bool replaceRows, int page, DynamicApiContext context, string stage)
        {
            var attempt = DynamicApiSelection.CreateAttempt(context, DynamicApiOperation.HomeVideoFeed, DynamicApiSource.Legacy);
            var stopwatch = Stopwatch.StartNew();
            HttpResults restResponse;
            try
            {
                LogHelper.Log("Home dynamics stage=" + stage + " rpc=" + (string.IsNullOrEmpty(cursor) ? "dynamic_new" : "dynamic_history")
                    + " page=" + page + " cursor=" + (cursor ?? ""), LogType.DEBUG);
                var restApi = string.IsNullOrEmpty(cursor)
                    ? _dynamicAPI.DyanmicNew(Api.User.DynamicAPI.UserDynamicType.Video)
                    : _dynamicAPI.HistoryDynamic(cursor, Api.User.DynamicAPI.UserDynamicType.Video);
                restResponse = await restApi.Request();
            }
            catch (Exception ex)
            {
                var outcome = DynamicApiSelection.ClassifyRequestFailure(ex);
                DynamicApiSelection.Record(attempt, outcome, stopwatch.Elapsed, stage);
                if (outcome == DynamicApiOutcome.IgnoredFailure)
                    throw;
                _lastLegacyFailureMessage = ex.Message;
                return outcome;
            }
            if (!restResponse.status)
            {
                //HTTP 状态在 code 里：4xx/5xx 与传输失败分开归类
                var outcome = DynamicApiSelection.ClassifyLegacy(restResponse, null);
                DynamicApiSelection.Record(attempt, outcome, stopwatch.Elapsed, stage);
                _lastLegacyFailureMessage = string.IsNullOrEmpty(restResponse.message) ? "动态请求失败" : restResponse.message;
                return outcome;
            }
            var restData = restResponse.GetJObject();
            var businessCode = restData == null || restData["code"] == null ? (int?)null : restData["code"].ToInt32();
            var envelope = DynamicApiSelection.ClassifyLegacy(restResponse, businessCode);
            if (envelope != DynamicApiOutcome.Success || restData["data"] == null)
            {
                //信封合法但没有数据，或者业务码本身不是 0：都算这次没拿到有效页
                var outcome = envelope == DynamicApiOutcome.Success ? DynamicApiOutcome.MalformedResponse : envelope;
                DynamicApiSelection.Record(attempt, outcome, stopwatch.Elapsed, stage, businessCode ?? 0);
                _lastLegacyFailureMessage = restData?["message"]?.ToString() ?? restResponse.message ?? "动态请求失败";
                return outcome;
            }
            DynamicApiSelection.Record(attempt, DynamicApiOutcome.Success, stopwatch.Elapsed, stage);

            var restCards = JsonConvert.DeserializeObject<ObservableCollection<DynamicItemModel>>(restData["data"]["cards"]?.ToString() ?? "[]")
                ?? new ObservableCollection<DynamicItemModel>();
            if (replaceRows && Items != null)
            {
                Items.Clear();
            }
            if (Items == null)
            {
                Items = restCards;
            }
            else
            {
                foreach (var item in restCards)
                {
                    Items.Add(item);
                }
            }
            var lastRestCard = restCards.Count > 0 ? restCards[restCards.Count - 1] : null;
            _restCursor = lastRestCard?.desc?.dynamic_id;
            //换源时 gRPC 游标已清空，这里再清一次，保证 REST 页面不会带着 historyOffset
            _historyOffset = null;
            _updateBaseline = null;
            _page = page;
            //只有真的取到卡片、且最后一张卡带着可用的 desc.dynamic_id 时才有下一页可翻
            CanLoadMore = restCards.Count > 0 && !string.IsNullOrEmpty(_restCursor);
            LogHelper.Log("Home dynamics stage=items-published source=rest page=" + _page + " count=" + restCards.Count
                + " cursor=" + (_restCursor ?? "") + " canLoadMore=" + CanLoadMore, LogType.DEBUG);
            return DynamicApiOutcome.Success;
        }

        public async void Refresh()
        {
            if (Loading)
            {
                return;
            }
            Items = null;
            CanLoadMore = false;
            _lastPageFailed = false;
            _lastLegacyFailureMessage = null;
            _lastGrpcFailureMessage = null;
            _page = 1;
            _historyOffset = null;
            _updateBaseline = null;
            _polymerOffset = null;
            //刷新回到选择器重新决定来源：固定的来源与粘滞兜底都在这里作废，三套游标随新的动态流一起作废
            _pinnedSource = null;
            _restCursor = null;
            await GetDynamicItems(1);
        }
        public async void LoadMore()
        {
            if (Loading)
            {
                return;
            }
            if (Items == null || Items.Count == 0 || (!CanLoadMore && !_lastPageFailed))
            {
                return;
            }
            await GetDynamicItems(_page + 1);
        }




    }
    public class DynamicItemModel
    {
        /// <summary>
        /// json字符串
        /// </summary>
        public string extend_json { get; set; }
        /// <summary>
        /// json字符串,根据desc里的type，获得数据
        /// </summary>
        private string _card;
        private DynamicVideoCardModel _video;
        public DynamicVideoCardModel Video
        {
            get { return _video; }
            set { _video = value; }
        }
        public DynamicSeasonCardModel Season { get; set; }
        public string card
        {
            get { return _card; }
            set
            {
                _card = value;
                _video = null;
                Season = null;
            }
        }
        public DynamicDescModel desc { get; set; }
        public DynamicVideoCardModel video
        {
            get
            {
                if (desc != null && (desc.type == 8 || desc.type == 4310))
                {
                    if (_video == null)
                    {
                        try
                        {
                            _video = Newtonsoft.Json.JsonConvert.DeserializeObject<DynamicVideoCardModel>(card);
                            LogHelper.Log("Home dynamics stage=video-card-converted type=" + desc.type + " dynamicId=" + desc.dynamic_id + " hasOwner=" + (_video?.owner != null) + " hasStats=" + (_video?.stat != null), LogType.DEBUG);
                        }
                        catch (Exception ex)
                        {
                            LogHelper.Log("Home dynamics stage=video-card-conversion-failed type=" + desc.type + " dynamicId=" + desc.dynamic_id, LogType.ERROR, ex);
                            throw;
                        }
                    }
                    return _video;
                }
                return null;
            }
            set { _video = value; }
        }
        public DynamicSeasonCardModel season
        {
            get
            {
                if (desc != null && desc.type == 512)
                {
                    return Season ?? Newtonsoft.Json.JsonConvert.DeserializeObject<DynamicSeasonCardModel>(card);
                }
                return null;
            }
            set { Season = value; }
        }
    }
    public class DynamicItemDataTemplateSelector : DataTemplateSelector
    {
        public ResourceDictionary resource;
        protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
        {
            var card = item as DynamicItemModel;
            LogHelper.Log("Home dynamics stage=template-select type=" + (card?.desc == null ? "null" : card.desc.type.ToString()) + " dynamicId=" + (card?.desc?.dynamic_id ?? ""), LogType.DEBUG);
            if (card.desc.type == 8 || card.desc.type == 4310)
            {
                return resource["DynamicVideo"] as DataTemplate;
            }
            else
            {
                return resource["DynamicSeason"] as DataTemplate;
            }
        }
    }
    public class DynamicDescModel
    {
        public string uid { get; set; }
        public string time_text { get; set; }
        /// <summary>
        /// 8=视频，512=番剧，4310=用户投稿合集
        /// </summary>
        public int type { get; set; }
        public string rid { get; set; }
        public int view { get; set; }
        public int like { get; set; }
        public int comment { get; set; }
        public int is_liked { get; set; }
        public string dynamic_id_str { get; set; }
        public string dynamic_id { get; set; }
        public int status { get; set; }
        public long timestamp { get; set; }
    }
    public class DynamicVideoCardModel
    {
        public string aid { get; set; }
        public int attribute { get; set; }
        public string cid { get; set; }
        public long ctime { get; set; }
        public string desc { get; set; }
        public int duration { get; set; }
        public string dynamic { get; set; }
        public string jump_url { get; set; }
        public DynamicVideoCardOwnerModel owner { get; set; }
        public string pic { get; set; }
        public long pubdate { get; set; }
        public string title { get; set; }
        public DynamicVideoCardStatModel stat { get; set; }
        public string viewtext { get { return stat == null ? "0" : stat.view.ToString(); } }
        public string danmakutext { get { return stat == null ? "0" : stat.danmaku.ToString(); } }
    }
    public class DynamicVideoCardStatModel
    {
        public int coin { get; set; }
        public int danmaku { get; set; }
        public int favorite { get; set; }
        public int like { get; set; }
        public int reply { get; set; }
        public int share { get; set; }
        public int view { get; set; }
    }
    public class DynamicVideoCardOwnerModel
    {
        public string face { get; set; }
        public string mid { get; set; }
        public string name { get; set; }
    }
    public class DynamicSeasonCardModel
    {
        public string aid { get; set; }
        public string cover { get; set; }
        public string index_title { get; set; }
        public string index { get; set; }
        public string new_desc { get; set; }
        public string url { get; set; }
        public int play_count { get; set; }
        public int reply_count { get; set; }
        public int bullet_count { get; set; }
        public int episode_id { get; set; }
        public DynamicSeasonCardApiSeasonInfoModel season { get; set; }
    }
    public class DynamicSeasonCardApiSeasonInfoModel
    {
        public string type_name { get; set; }
        public string cover { get; set; }
        public string title { get; set; }
        public int is_finish { get; set; }
        public int season_id { get; set; }
    }
    public class DynamicCardModel2024
    {
        public DynamicCardModel2024_basic basic { get; set; }
        public DynamicCardModel2024_modules modules { get; set; }
        public DynamicCardModel2024 orig { get; set; }
        public string type { get; set; }
        public string id_str { get; set; }
        public bool visible { get; set; }
        public string title
        {
            get
            {
                return
                  modules?.module_dynamic?.major?.archive?.title
                  ??
                  modules?.module_dynamic?.major?.live?.title
                  ??
                  modules?.module_dynamic?.major?.article?.title
                  ??
                  modules?.module_dynamic?.major?.ugc_season?.title
                  ??
                  modules?.module_dynamic?.major?.medialist?.title
                  ??
                  modules?.module_dynamic?.major?.pgc?.title
                  ??
                  modules?.module_dynamic?.additional?.title
                  ??
                  "";
            }
        }
        public string jumpurl
        {
            get
            {
                var url =
                    modules?.module_dynamic?.major?.archive?.jump_url ??
                    modules?.module_dynamic?.major?.article?.jump_url ??
                    modules?.module_dynamic?.major?.pgc?.jump_url ??
                    modules?.module_dynamic?.major?.ugc_season?.jump_url ??
                    modules?.module_dynamic?.major?.medialist?.jump_url ??
                    modules?.module_dynamic?.major?.live?.jump_url ??
                     "";
                if (url.Length == 0) return url;
                if (url[0] == '/') return "https:" + url;
                return url;
            }
        }
        public string durationtext
        {
            get
            {
                return
                    modules.module_dynamic.major?.archive?.duration_text ??
                    modules.module_dynamic.major?.ugc_season?.duration_text ?? "";
            }
        }

        public long pubdate
        {
            get { return modules?.module_author?.pub_ts ?? 0; }
        }
        public string face
        {
            get { return modules?.module_author?.face ?? ""; }
        }

        public string owner
        {
            get { return modules?.module_author?.name ?? ""; }
        }

        public long mid
        {
            get
            {
                return modules?.module_author?.mid ?? 0;
            }
        }

        public string pic
        {
            get
            {
                return
                  modules?.module_dynamic?.major?.archive?.cover ??
                  modules?.module_dynamic?.major?.live?.cover ??
                  modules?.module_dynamic?.major?.article?.covers?.FirstOrDefault() ??
                  modules?.module_dynamic?.major?.pgc?.cover ??
                  modules?.module_dynamic?.major?.ugc_season?.cover ??
                  modules?.module_dynamic?.major?.medialist?.cover ??
                  "";
            }
        }
        public string aid
        {
            get
            {
                return
                  modules?.module_dynamic?.major?.archive?.aid ??
                  modules?.module_dynamic?.major?.pgc?.aid ??
                  modules?.module_dynamic?.major?.pgc?.season_id_string ??
                  modules?.module_dynamic?.major?.live?.id ??
                  basic.rid_str ??
                  "0";
            }
        }
        public string danmakutext
        {
            get { return modules?.module_dynamic?.major?.archive?.stat?.danmaku ?? "0"; }
        }
        public int danmaku
        {
            get
            {
                var text = danmakutext;
                text = text.Replace("万", "0000").Replace("千", "000").Replace("百", "00").Replace("十", "0");
                int result;
                if (Int32.TryParse(text, out result))
                {
                    return result;
                }
                return 0;
            }
        }
        public int view
        {
            get
            {
                var text = viewtext;
                text = text.Replace("万", "0000").Replace("千", "000").Replace("百", "00").Replace("十", "0");
                int result;
                if (Int32.TryParse(text, out result))
                {
                    return result;
                }
                return 0;
            }
        }
        public string viewtext
        {
            get
            {
                return modules?.module_dynamic?.major?.archive?.stat?.play ??
                  modules.module_dynamic.major?.article?.label ??
                  modules.module_dynamic.major?.live?.desc_second ??
                  modules.module_dynamic.major?.pgc?.stat?.play ??
                  modules.module_dynamic.major?.ugc_season?.stat?.play ??
                  modules.module_dynamic.additional?.reserve?.desc2?.text ??
                  "0";
            }
        }
        public string reservetime
        {
            get
            {
                return
                  modules.module_dynamic.additional?.reserve?.desc1?.text ?? "";
            }
        }
        public int reservecount
        {
            get
            {
                return
                  modules.module_dynamic.additional?.reserve?.reserve_total ?? 0;
            }
        }
        public int like
        {
            get
            {
                return modules?.module_stat?.like?.count ?? 0;
            }
        }
        public int comment
        {
            get
            {
                return modules?.module_stat?.comment?.count ?? 0;
            }
        }
        public int forward
        {
            get
            {
                return modules?.module_stat?.forward?.count ?? 0;
            }
        }
        public bool liked
        {
            get
            {
                return modules?.module_stat?.like?.status ?? false;
            }
        }
        public string desc
        {
            get
            {
                return
                    modules?.module_dynamic?.desc?.text ??
                    modules?.module_dynamic?.major?.article?.desc ??
                    "";
            }
        }
        public string richtext
        {
            get
            {
                return modules?.module_dynamic?.desc?.richtext;
            }
        }
    }
    public class DynamicCardModel2024_basic
    {
        public string comment_id_str { get; set; }
        public int comment_type { get; set; }
        public string rid_str { get; set; }
        //public object like_icon
    }
    public class DynamicCardModel2024_modules
    {
        public DynamicCardModel2024_author module_author;
        public DynamicCardModel2024_stat module_stat;
        public DynamicCardModel2024_dynamic module_dynamic;
    }
    public class DynamicCardModel2024_author
    {
        public DynamicCardModel2024_avatar avatar { get; set; }
        public DynamicCardModel2024_decorate decorate { get; set; }
        public DynamicCardModel2024_vip vip { get; set; }
        public DynamicCardModel2024_off official_verify { get; set; }
        public DynamicCardModel2024_pendant pendant { get; set; }
        public long mid { get; set; }
        public string name { get; set; }
        public string pub_time { get; set; }
        public string type { get; set; }
        public string pub_action { get; set; }
        public string pub_location_text { get; set; }
        public string label { get; set; }
        public string face { get; set; }
        public string jump_url { get; set; }
        public long pub_ts { get; set; }
        public bool following { get; set; }
    }
    public class DynamicCardModel2024_avatar
    {
        public string mid { get; set; }
    }
    public class DynamicCardModel2024_off
    {
        public string desc { get; set; }
        public int type { get; set; }
    }
    public class DynamicCardModel2024_vip
    {
        public string nickname_color { get; set; }
        public int status { get; set; }
        public int type { get; set; }
        public int theme_type { get; set; }
    }
    public class DynamicCardModel2024_pendant
    {
        public string image { get; set; }
        public string image_enhance { get; set; }
        public int expire { get; set; }
        public long pid { get; set; }
        public long n_pid { get; set; }
        public string name { get; set; }
    }
    public class DynamicCardModel2024_decorate
    {
        public string name { get; set; }
        public string card_url { get; set; }
        public string jump_url { get; set; }
        public DynamicCardModel2024_fan fan { get; set; }
        public int type { get; set; }
    }
    public class DynamicCardModel2024_face
    {
        public string mid { get; set; }
    }
    public class DynamicCardModel2024_fan
    {
        public bool is_fan { get; set; }
        public string color { get; set; }
        public int number { get; set; }
    }
    public class DynamicCardModel2024_stat
    {
        public DynamicCardModel2024_comment comment { get; set; }
        public DynamicCardModel2024_forward forward { get; set; }
        public DynamicCardModel2024_like like { get; set; }
    }
    public class DynamicCardModel2024_forward
    {
        public int count { get; set; }
        public bool forbidden { get; set; }
    }
    public class DynamicCardModel2024_comment : DynamicCardModel2024_forward
    {
    }
    public class DynamicCardModel2024_like : DynamicCardModel2024_forward
    {
        public bool status { get; set; }
    }
    public class DynamicCardModel2024_dynamic
    {
        //public DynamicCardModel2024_topic topic { get; set; }
        public DynamicCardModel2024_add additional { get; set; }
        public DynamicCardModel2024_desc desc { get; set; }
        public DynamicCardModel2024_major major { get; set; }
    }
    public class DynamicCardModel2024_desc
    {
        public static int maxlength = 1000;
        public List<DynamicCardModel2024_node> rich_text_nodes { get; set; }
        public string text { get; set; }
        public static int BEGIN = 0;
        public static int END = 1;
        public string richtext
        {
            get
            {
                var str = "";
                var state = END;
                foreach (var s in rich_text_nodes)
                {
                    var t = s.richtext;
                    if (!string.IsNullOrEmpty(t))
                    {
                        if (s.type == "RICH_TEXT_NODE_TYPE_TEXT")
                        {
                            if (state == BEGIN)
                            {
                                str += "</Paragraph>";
                            }
                            else
                            {
                            }
                            str += "<Paragraph>";
                            state = BEGIN;
                        }
                        else
                        {
                            if (state == BEGIN) { }
                            else
                            {
                                str += "<Paragraph>";
                            }
                            state = BEGIN;
                        }
                        str += $"{s.richtext}";
                    }
                    if (str.Length > maxlength) break;
                }
                if (state == BEGIN)
                {
                    str += "</Paragraph>";
                }
                return str;
            }
        }
    }
    public class DynamicCardModel2024_node
    {
        public string orig_text { get; set; }
        public string text { get; set; }
        public string type { get; set; }
        public string jump_url { get; set; }
        public string icon_name { get; set; }
        public string rid { get; set; }
        //goods
        public DynamicCardModel2024_emojiinfo emoji { get; set; }
        //解析出富文本
        public string richtext
        {
            get
            {
                return Parser.ParseRichText(this);
            }
        }
    }
    public class DynamicCardModel2024_emojiinfo
    {
        public string icon_url { get; set; }
        public string text { get; set; }
        public int size { get; set; }//12,不知道
        public int type { get; set; }//123,不知道
    }
    public class DynamicCardModel2024_add
    {
        public DynamicCardModel2024_addin common { get; set; }
        public DynamicCardModel2024_addin_reserve reserve { get; set; }
        public DynamicCardModel2024_addin goods { get; set; }
        public DynamicCardModel2024_addin vote { get; set; }
        public DynamicCardModel2024_addin ugc { get; set; }
        public string type { get; set; }
        public string title
        {
            get
            {
                return
                    common?.title ??
                    reserve?.title ??
                    goods?.title ??
                    vote?.title ??
                    ugc?.title;
            }
        }
    }
    public class DynamicCardModel2024_addin
    {
        public DynamicCardModel2024_button button { get; set; }
        public string cover { get; set; }
        public string desc1 { get; set; }//预约时间
        public string desc2 { get; set; }//预约量
        public string head_text { get; set; }
        public string id_str { get; set; }
        public string jump_url { get; set; }
        public int style { get; set; }
        public string sub_type { get; set; }//game,decoration,ovg
        public string title { get; set; }
        public long rid { get; set; }
        public int state { get; set; }
        public int stype { get; set; }
    }
    public class DynamicCardModel2024_addin_reserve : DynamicCardModel2024_addin
    {
        public DynamicCardModel2024_button2 button { get; set; }
        public long up_mid { get; set; }
        public DynamicCardModel2024_reservetext desc1 { get; set; }//预约时间
        public DynamicCardModel2024_reservetext desc2 { get; set; }//预约量
        public DynamicCardModel2024_reservetext desc3 { get; set; }//预约抽奖信息
        public int reserve_total { get; set; }
    }
    public class DynamicCardModel2024_reservetext
    {
        public int style { get; set; }//0预约中,1直播中
        public string text { get; set; }
    }
    public class DynamicCardModel2024_button
    {
        public int status { get; set; }//1未预约uncheck，2已预约check
        public string type { get; set; }//game,decoration,ovg
        public string jump_url { get; set; }
        public DynamicCardModel2024_buttontext jump_style { get; set; }
        public DynamicCardModel2024_buttontext check { get; set; }
        public DynamicCardModel2024_buttontext uncheck { get; set; }
        public string text
        {
            get
            {
                return
                    (status == 2 ? check : uncheck)?.text ?? "";
            }
        }
        public string icon
        {
            get
            {
                return
                    (status == 2 ? check : uncheck)?.icon_url ?? "";
            }
        }
    }
    public class DynamicCardModel2024_button2 : DynamicCardModel2024_button
    {
        public int type { get; set; }//game,decoration,ovg
        public string icon_url { get; set; }
        public int disable { get; set; }
        public string toast { get; set; }
    }
    public class DynamicCardModel2024_buttontext
    {
        public string icon_url { get; set; }
        public string text { get; set; }//game=进入,decoration=去看看,check=追剧,uncheck=已追剧
    }
    public class DynamicCardModel2024_major
    {
        public DynamicCardModel2024_archive archive { get; set; }
        public DynamicCardModel2024_draw draw { get; set; }
        public DynamicCardModel2024_live live { get; set; }
        public DynamicCardModel2024_article article { get; set; }
        public DynamicCardModel2024_season pgc { get; set; }
        public DynamicCardModel2024_list ugc_season { get; set; }
        public DynamicCardModel2024_list medialist { get; set; }
        public DynamicCardModel2024_live_rcmd live_rcmd { get; set; }
        public string type { get; set; }
    }
    public class DynamicCardModel2024_live_rcmd
    {
        public string content { get; set; }
    }
    public class DynamicCardModel2024_article
    {
        public List<string> covers { get; set; }
        public string desc { get; set; }
        public int id { get; set; }
        public string jump_url { get; set; }
        public string label { get; set; }
        public string title { get; set; }
    }
    public class DynamicCardModel2024_season
    {
        //public object badge{ get; set; }
        public string aid { get; set; }
        public string cover { get; set; }
        public int epid { get; set; }
        public int season_id { get; set; }
        public string season_id_string
        {
            get
            {
                return season_id!=0 ? season_id.ToString() : null;
            }
        }
        public DynamicCardModel2024_astat stat { get; set; }
        /*1：番剧
2：电影
3：纪录片
4：国创
5：电视剧
6：漫画
7：综艺*/
        public int sub_type { get; set; }
        public string jump_url { get; set; }
        public int type { get; set; }
        public string title { get; set; }
    }
    public class DynamicCardModel2024_list
    {
        // 播单 ID；合集卡片没有该字段（0）。
        public long id { get; set; }
        public long aid { get; set; }
        //public object badge{ get; set; }
        public string cover { get; set; }
        public string desc { get; set; }
        public int disable_preview { get; set; }
        public string duration_text { get; set; }
        public string jump_url { get; set; }
        public DynamicCardModel2024_astat stat { get; set; }
        public string title { get; set; }
    }
    public class DynamicCardModel2024_live
    {
        //public obj badge {get; set;}
        public string cover { get; set; }
        public string desc_first { get; set; }
        public string desc_second { get; set; }
        public string id { get; set; }
        public string jump_url { get; set; }
        public int live_state { get; set; }
        public string title { get; set; }
        public int style { get; set; }

    }
    public class DynamicCardModel2024_archive
    {
        public string aid { get; set; }
        public DynamicCardModel2024_badge badge { get; set; }
        public string bvid { get; set; }
        public string cover { get; set; }
        public string desc { get; set; }
        public int disable_preview { get; set; }
        public string duration_text { get; set; }
        public string jump_url { get; set; }
        public string title { get; set; }
        public int type { get; set; }
        public DynamicCardModel2024_astat stat;
    }
    public class DynamicCardModel2024_badge
    {
        public string bg_color { get; set; }
        public string color { get; set; }
        public string text { get; set; }
        //public string icon_url { get; set; }
    }
    public class DynamicCardModel2024_draw
    {
        public long id { get; set; }
        public List<DynamicCardModel2024_pic> items { get; set; }
    }
    public class DynamicCardModel2024_pic
    {
        public int height { get; set; }
        public int width { get; set; }
        public float size { get; set; }
        public string src { get; set; }
        //public List<string> tags {get; set;}
    }
    public class DynamicCardModel2024_astat
    {
        public string danmaku { get; set; }
        public string play { get; set; }
    }
    //原来位于DynamicParse.cs
    public static class Parser
    {
        public static string escapexaml(string text)
        {

            text = text.Replace("&", "&amp;");
            text = text.Replace("<", "&lt;");
            text = text.Replace(">", "&gt;");
            text = text.Replace("\"", "&quot;");
            text = text.Replace("'", "&apos;");
            text = text.Replace("\r\n", "\n");

            return text;
        }
        public static List<string> HTTP(string text)
        {
            if (!string.IsNullOrEmpty(text))
            {
                text = escapexaml(text);
            }
            return text.Split('\n').ToList();
        }
        public static string At(DynamicCardModel2024_node node, string text)
        {
            if (node.type != "RICH_TEXT_NODE_TYPE_AT") return text;
            var mid = node.rid;
            if (string.IsNullOrEmpty(mid)) return text;
            var url = $"https://space.bilibili.com/{mid}";
            url = escapexaml(url);
            return $"<InlineUIContainer><HyperlinkButton NavigateUri=\"{url}\">{text}<ToolTipService.ToolTip>" +
                $"{url}" +
        "</ToolTipService.ToolTip ></HyperlinkButton></InlineUIContainer>";
        }
        public static string Topic(DynamicCardModel2024_node node, string text)
        {
            if (node.type != "RICH_TEXT_NODE_TYPE_TOPIC") return text;
            var url = node.jump_url;
            url = escapexaml(url);
            //replace the whole text with a hyperlink to url
            return $"<InlineUIContainer><HyperlinkButton NavigateUri=\"{url}\">{text}</HyperlinkButton></InlineUIContainer>";
        }
        public static string Web(DynamicCardModel2024_node node, string text)
        {
            if (node.type != "RICH_TEXT_NODE_TYPE_WEB") return text;
            var url = node.jump_url;
            url = escapexaml(url);
            //replace the whole text with a hyperlink to url
            return $"<InlineUIContainer><HyperlinkButton NavigateUri=\"{url}\">{url}</HyperlinkButton></InlineUIContainer>";
        }
        public static string Emoji(DynamicCardModel2024_node node, string text)
        {
            var emojidata = node.emoji;
            if (null == emojidata) return text;
            var type = emojidata.type;
            var size = emojidata.size;
            var url = emojidata.icon_url;
            if (string.IsNullOrEmpty(url)) return text;
            url = escapexaml(url);
            //replace [emojidata.text] with <InlineUIContainer><Border Margin=""0 -4 4 -4""><Image Source=""{emojidata.icon_url}"" MaxWidth=""{emojidata.size*1em}"" MaxHeight=""{emojidata.size*1em}""/></Border></InlineUIContainer>
            return
                $"<InlineUIContainer><Border Margin=\"0 -4 4 -4\"><Image Source=\"{url}\" MaxWidth=\"{emojidata.size * 18}\" MaxHeight=\"{emojidata.size * 18}\"/></Border></InlineUIContainer>";
        }

        public static string ParseRichText(DynamicCardModel2024_node node)
        {
            var str = "";
            if (node.text != null)
            {
                str = node.text;
            }
            var strs = HTTP(str);
            for (var i = 0; i < strs.Count; i++)
            {
                strs[i] = Emoji(node, strs[i]);
                strs[i] = Topic(node, strs[i]);
                strs[i] = At(node, strs[i]);
                strs[i] = Web(node, strs[i]);
            }
            str = "";
            foreach (var s in strs)
            {
                str += $"{s}\n";
            }
            return str;
        }
        public static string ParseText(string text)
        {
            var str = "";
            if (text != null)
            {
                str = text;
            }
            var strs = HTTP(str);
            for (var i = 0; i < strs.Count; i++)
            {
            }
            str = "";
            foreach (var s in strs)
            {
                str += $"<Paragraph>{s}</Paragraph>\n";
            }
            return str;
        }
    }
}
