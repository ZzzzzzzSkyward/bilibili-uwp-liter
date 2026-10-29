using BiliLite.Controls.Dynamic;
using BiliLite.Helpers;
using BiliLite.Models;
using BiliLite.Models.Dynamic;
using BiliLite.Pages;
using BiliLite.Pages.User;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.Foundation;
using Windows.UI.Popups;
using Windows.UI.Xaml.Controls;
using static BiliLite.Api.User.DynamicAPI;
using BiliLite.Dialogs;
using System.Reflection;
using Windows.Storage.Pickers;
using Windows.Storage.Provider;
using Windows.Storage;
using System.IO;
using System.Security.Policy;
using System.Net.Http;
using Grpc.Core;
using System.Net;
using System.Globalization;
using System.Text.RegularExpressions;
using BiliLite.Modules;
using BiliLite.Services;

namespace BiliLite.Modules.User
{
    public enum DynamicType
    {
        /// <summary>
        /// 用户关注动态
        /// </summary>
        UserDynamic,
        /// <summary>
        /// 话题动态
        /// </summary>
        Topic,
        /// <summary>
        /// 个人空间动态
        /// </summary>
        Space
    }
    public class DynamicVM : IModules
    {
        readonly WatchLaterVM watchLaterVM;
        readonly Api.User.DynamicAPI dynamicAPI;
        readonly GrpcService grpcService;
        private int feedPage = 1;
        private string historyOffset;
        private string updateBaseline;
        private string articleOffset;
        //REST 动态流的翻页游标（data.offset），与 gRPC 的 historyOffset/updateBaseline 分属两套，互不传递
        private string restOffset;
        //gRPC 动态接口被拒后粘滞到实例的兜底开关：置位后同一分类的后续页都走 REST，直到 Refresh 重新试一次 gRPC
        private bool restFeedMode;
        private bool lastPageFailed;
        //每次刷新/切分类都递增，只有持有一致代号的请求才能发布结果
        private int requestGeneration;
        //刷新在请求进行中被请求时置位，由旧请求结束时启动最新一次刷新
        private bool refreshQueued;
        //真正的在途请求标记（Loading 只是界面状态）
        private bool feedRequestInFlight;
        //连续空页上限：服务端页被过滤成 0 张卡片时游标仍在前进，只靠 has_more/游标判断会无限翻页，
        //所以连续空页达到上限就停止，作为兜底（只在达到上限的那一页记一条日志）
        private const int EmptyPageCap = 5;
        private int consecutiveEmptyPages;
        public DynamicVM()
        {
            dynamicAPI = new Api.User.DynamicAPI();
            grpcService = new GrpcService();
            watchLaterVM = new WatchLaterVM();
            dynamicItemDataTemplateSelector = new DynamicItemDataTemplateSelector();
            RefreshCommand = new RelayCommand(Refresh);
            LoadMoreCommand = new RelayCommand(LoadMore);

            UserCommand = new RelayCommand<object>(OpenUser);
            LaunchUrlCommand = new RelayCommand<object>(LaunchUrl);
            WebCommand = new RelayCommand<object>(OpenWeb);
            TagCommand = new RelayCommand<object>(OpenTag);
            LotteryCommand = new RelayCommand<object>(OpenLottery);
            VoteCommand = new RelayCommand<object>(OpenVote);
            ImageCommand = new RelayCommand<object>(OpenImage);
            SaveImageCommand = new RelayCommand<object>(SaveImage);
            CommentCommand = new RelayCommand<DynamicItemDisplayModel>(OpenComment);
            DeleteCommand = new RelayCommand<DynamicItemDisplayModel>(Delete);
            LikeCommand = new RelayCommand<DynamicItemDisplayModel>(DoLike);
            RepostCommand = new RelayCommand<DynamicItemDisplayModel>(OpenSendDynamicDialog);
            DetailCommand = new RelayCommand<DynamicItemDisplayModel>(OpenDetail);
        }

        public async void SaveImage(object obj)
        {
            await SaveImageAsync(obj);
        }

        public event EventHandler<DynamicItemDisplayModel> OpenCommentEvent;
        public ICommand VoteCommand { get; set; }
        public ICommand UserCommand { get; set; }
        public ICommand LotteryCommand { get; set; }
        public ICommand LaunchUrlCommand { get; set; }
        public ICommand WebCommand { get; set; }
        public ICommand TagCommand { get; set; }
        public ICommand ImageCommand { get; set; }
        public ICommand SaveImageCommand { get; set; }
        public ICommand CommentCommand { get; set; }
        public ICommand RepostCommand { get; set; }
        public ICommand LikeCommand { get; set; }
        public ICommand DeleteCommand { get; set; }
        public ICommand DetailCommand { get; set; }
        public DynamicItemDataTemplateSelector dynamicItemDataTemplateSelector { get; set; }
        private bool _loading = true;
        public bool Loading
        {
            get { return _loading; }
            set { _loading = value; DoPropertyChanged("Loading"); }
        }
        private bool _loadMore = false;
        public bool CanLoadMore
        {
            get { return _loadMore; }
            set { _loadMore = value; DoPropertyChanged("CanLoadMore"); }
        }
        private bool _showError = false;
        public bool ShowError
        {
            get { return _showError; }
            set { _showError = value; DoPropertyChanged("ShowError"); }
        }
        private string _errorMsg = "";
        public string ErrorMsg
        {
            get { return _errorMsg; }
            set { _errorMsg = value; DoPropertyChanged("ErrorMsg"); }
        }
        public ICommand RefreshCommand { get; private set; }
        public ICommand LoadMoreCommand { get; private set; }
        public DynamicType DynamicType { get; set; } = DynamicType.UserDynamic;
        public string Uid { get; set; }
        public UserDynamicType UserDynamicType { get; set; } = UserDynamicType.All;



        private ObservableCollection<DynamicItemDisplayModel> _Items;

        public ObservableCollection<DynamicItemDisplayModel> Items
        {
            get { return _Items; }
            set { _Items = value; DoPropertyChanged("Items"); }
        }

        public async void Refresh()
        {
            //先作废正在进行的请求：即使它还没结束，也不能再发布旧分类的数据
            requestGeneration++;
            refreshQueued = true;
            Items = null;
            CanLoadMore = false;
            lastPageFailed = false;
            ShowError = false;
            ErrorMsg = "";
            feedPage = 1;
            historyOffset = null;
            updateBaseline = null;
            articleOffset = null;
            restOffset = null;
            consecutiveEmptyPages = 0;
            //兜底开关不跨刷新继承：刷新时回到 gRPC 再试一次（每次请求最多一次尝试，失败即在同一次请求内改走 REST，不会循环）
            if (restFeedMode)
            {
                restFeedMode = false;
                LogHelper.Log("User dynamic VM stage=refresh-source grpc-retry restFeedMode reset generation=" + requestGeneration + " itemType=" + UserDynamicType, LogType.INFO);
            }
            else
            {
                LogHelper.Log("User dynamic VM stage=refresh-source grpc generation=" + requestGeneration + " itemType=" + UserDynamicType, LogType.DEBUG);
            }
            if (feedRequestInFlight)
            {
                //请求进行中，由它的 finally 丢弃结果后启动本次刷新（最新分类胜出）
                LogHelper.Log("User dynamic VM stage=refresh-queued generation=" + requestGeneration + " itemType=" + UserDynamicType, LogType.INFO);
                return;
            }
            refreshQueued = false;
            await GetDynamicItems();
        }
        public async void LoadMore()
        {
            if (Loading)
            {
                return;
            }
            if (!CanLoadMore || lastPageFailed)
            {
                return;
            }
            var last = Items?.LastOrDefault();
            await GetDynamicItems(last?.DynamicID ?? "", true);
        }
        public void OpenUser(object id)
        {
            MessageCenter.NavigateToPage(this, new NavigationInfo()
            {
                icon = Symbol.Contact,
                page = typeof(UserInfoPage),
                title = "用户中心",
                parameters = id
            });
        }
        public async void LaunchUrl(object url)
        {
            var result = await MessageCenter.HandleUrl(url.ToString());
            if (!result)
            {
                Utils.ShowMessageToast("无法打开Url");
            }
        }
        public void OpenWeb(object url)
        {
            MessageCenter.NavigateToPage(null, new NavigationInfo()
            {
                icon = Symbol.World,
                page = typeof(WebPage),
                title = "加载中...",
                parameters = url
            });
        }

        public void OpenImage(object data)
        {
            DyanmicItemDisplayImageInfo info = data as DyanmicItemDisplayImageInfo;
            MessageCenter.OpenImageViewer(info.AllImages, info.Index);
        }
        public async Task SaveImageAsync(object data)
        {
            FileSavePicker save = new FileSavePicker();
            save.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
            save.FileTypeChoices.Add("图片", new List<string>() { Path.GetExtension(data as string) });
            save.SuggestedFileName = "bili_img_" + DateTime.Now.ToString("yyyyMMddHHmmss");

            StorageFile file = await save.PickSaveFileAsync();
            if (file != null)
            {
                var httpClient = new HttpClient();
                var response = await httpClient.GetAsync(data as string);
                if (response != null && response.StatusCode == HttpStatusCode.OK)
                {
                    using (var stream = await file.OpenStreamForWriteAsync())
                    {
                        await response.Content.CopyToAsync(stream);
                    }
                    FileUpdateStatus status = await CachedFileManager.CompleteUpdatesAsync(file);
                    if (status == FileUpdateStatus.Complete)
                        Utils.ShowMessageToast("保存成功");
                    else
                        Utils.ShowMessageToast("保存失败");
                    return;
                }
                Utils.ShowMessageToast("下载失败");
            }
        }
        public void OpenTag(object name)
        {
            //TODO 打开话题
            MessageCenter.NavigateToPage(this, new NavigationInfo()
            {
                icon = Symbol.World,
                page = typeof(WebPage),
                title = name.ToString(),
                parameters = "https://t.bilibili.com/topic/name/" + Uri.EscapeDataString(name.ToString())
            });
        }
        public async void OpenLottery(object id)
        {
            ContentDialog contentDialog = new ContentDialog()
            {
                IsPrimaryButtonEnabled = true,
                Title = "抽奖",
                PrimaryButtonText = "关闭"
            };
            contentDialog.PrimaryButtonClick += new TypedEventHandler<ContentDialog, ContentDialogButtonClickEventArgs>((sender, e) =>
            {
                contentDialog.Hide();
            });
            contentDialog.Content = new WebView()
            {
                Width = 500,
                Height = 500,
                Source = new Uri($"https://t.bilibili.com/lottery/h5/index/#/result?business_id={id.ToString()}&business_type=1&isWeb=1"),
            };
            await contentDialog.ShowAsync();
        }
        public void OpenVote(object id)
        {
            MessageCenter.NavigateToPage(this, new NavigationInfo()
            {
                icon = Symbol.Document,
                page = typeof(WebPage),
                title = "投票",
                parameters = $"https://t.bilibili.com/vote/h5/index/#/result?vote_id={id.ToString()}"
            });
            //ContentDialog contentDialog = new ContentDialog()
            //{
            //    IsPrimaryButtonEnabled = true,
            //    Title = "投票",
            //    PrimaryButtonText = "关闭"
            //};
            //contentDialog.PrimaryButtonClick += new TypedEventHandler<ContentDialog, ContentDialogButtonClickEventArgs>((sender, e) =>
            //{
            //    contentDialog.Hide();
            //});
            //contentDialog.Content = new WebView()
            //{
            //    Width = 500,
            //    Height = 500,
            //    Source = new Uri($"https://t.bilibili.com/vote/h5/index/#/result?vote_id={ id.ToString()}"),
            //};
            //await contentDialog.ShowAsync();
        }
        public void OpenComment(DynamicItemDisplayModel data)
        {
            OpenCommentEvent?.Invoke(this, data);
        }
        public void OpenDetail(DynamicItemDisplayModel data)
        {
            MessageCenter.NavigateToPage(this, new NavigationInfo()
            {
                icon = Symbol.Document,
                page = typeof(DynamicDetailPage),
                title = "动态详情",
                parameters = data.DynamicID
            });
        }
        public async void Delete(DynamicItemDisplayModel item)
        {
            if (!SettingHelper.Account.Logined && !await Utils.ShowLoginDialog())
            {
                Utils.ShowMessageToast("请先登录后再操作");
                return;
            }
            MessageDialog messageDialog = new MessageDialog("确定要删除动态吗?", "删除动态");
            messageDialog.Commands.Add(new UICommand("确定", cmd => { }, commandId: 0));
            messageDialog.Commands.Add(new UICommand("取消", cmd => { }, commandId: 1));
            var result = await messageDialog.ShowAsync();
            if (result.Id.ToInt32() == 1) return;
            try
            {
                var results = await dynamicAPI.Delete(item.DynamicID).Request();
                if (results.status)
                {
                    var data = await results.GetJson<ApiDataModel<object>>();
                    if (data.success)
                    {
                        Items.Remove(item);
                    }
                    else
                    {
                        Utils.ShowMessageToast(data.message);
                    }
                }
                else
                {
                    Utils.ShowMessageToast(results.message);
                }
            }
            catch (Exception ex)
            {
                var handel = HandelError<object>(ex);
                Utils.ShowMessageToast(handel.message);
            }

        }
        public async void DoLike(DynamicItemDisplayModel item)
        {
            if (!SettingHelper.Account.Logined && !await Utils.ShowLoginDialog())
            {
                Utils.ShowMessageToast("请先登录后再操作");
                return;
            }

            try
            {
                var results = await dynamicAPI.Like(item.DynamicID, item.Liked ? 2 : 1).Request();
                if (results.status)
                {
                    var data = await results.GetJson<ApiDataModel<object>>();
                    if (data.success)
                    {
                        if (item.Liked)
                        {
                            item.Liked = false;
                            item.LikeCount -= 1;
                        }
                        else
                        {
                            item.Liked = true;
                            item.LikeCount += 1;
                        }
                    }
                    else
                    {
                        Utils.ShowMessageToast(data.message);
                    }
                }
                else
                {
                    Utils.ShowMessageToast(results.message);
                }
            }
            catch (Exception ex)
            {
                var handel = HandelError<object>(ex);
                Utils.ShowMessageToast(handel.message);
            }




        }
        public async void OpenSendDynamicDialog(DynamicItemDisplayModel data)
        {
            if (!SettingHelper.Account.Logined && !await Utils.ShowLoginDialog())
            {
                Utils.ShowMessageToast("请先登录后再操作");
                return;
            }

            SendDynamicDialog sendDynamicDialog = new SendDynamicDialog();
            if (data != null)
            {
                sendDynamicDialog = new SendDynamicDialog(data);
            }
            await sendDynamicDialog.ShowAsync();
        }
        // 设置json序列化忽略null
        // 这样就不会报null的错误了
        public JsonSerializerSettings jsonSerializerSettings = new JsonSerializerSettings()
        {
            NullValueHandling = NullValueHandling.Ignore
        };
        public async Task GetDynamicItems(string idx = "", bool loadMore = false)
        {
            //代号在第一个 await 之前取得，之后只认这一份分类/代号
            var generation = ++requestGeneration;
            var requestDynamicType = DynamicType;
            var requestItemType = UserDynamicType;
            refreshQueued = false;
            var stage = "request-start";
            try
            {
                CanLoadMore = false;
                feedRequestInFlight = true;
                Loading = true;
                if (!loadMore)
                {
                    lastPageFailed = false;
                    ShowError = false;
                    ErrorMsg = "";
                    feedPage = 1;
                    historyOffset = null;
                    updateBaseline = null;
                    articleOffset = null;
                    restOffset = null;
                    consecutiveEmptyPages = 0;
                }
                LogHelper.Log("User dynamic VM stage=request-start generation=" + generation + " dynamicType=" + requestDynamicType + " itemType=" + requestItemType + " history=" + (idx != ""), LogType.DEBUG);

                //只有 gRPC 动态流（全部/视频/番剧）需要 app access key；专栏走 ArticleV2 的 Cookie 请求，
                //未登录也要照常尝试（与源项目一致），不能因为没登录就不发请求
                var feedRequiresAppLogin = requestItemType == UserDynamicType.All
                    || requestItemType == UserDynamicType.Video
                    || requestItemType == UserDynamicType.Season;
                if (requestDynamicType == DynamicType.UserDynamic && feedRequiresAppLogin && !SettingHelper.Account.Logined)
                {
                    stage = "login-prompt";
                    LogHelper.Log("User dynamic VM stage=login-prompt generation=" + generation + " itemType=" + requestItemType, LogType.INFO);
                    var logined = await Utils.ShowLoginDialog();
                    if (!IsCurrentRequest(generation, requestDynamicType, requestItemType))
                    {
                        LogHelper.Log("User dynamic VM stage=login-prompt discarded generation=" + generation, LogType.DEBUG);
                        return;
                    }
                    if (!logined)
                    {
                        //未登录：给出登录提示状态，而不是走传输失败分支，也不发送请求
                        lastPageFailed = true;
                        ShowError = true;
                        ErrorMsg = "请先登录后查看动态";
                        Utils.ShowMessageToast(ErrorMsg);
                        LogHelper.Log("User dynamic VM stage=login-required generation=" + generation, LogType.INFO);
                        return;
                    }
                    stage = "feed-request";
                }

                if (requestDynamicType == DynamicType.UserDynamic)
                {
                    stage = "feed-request";
                    var page = loadMore ? feedPage + 1 : 1;
                    //本页请求所用的游标：专栏用 articleOffset，其余动态流用 historyOffset；发布时与返回值比较，判断游标是否真的前进
                    var requestCursor = loadMore
                        ? (requestItemType == UserDynamicType.Article ? articleOffset : historyOffset)
                        : null;
                    List<DynamicCardModel2024> grpcCards = null;
                    bool hasMore = false;
                    string nextOffset = null;
                    string nextBaseline = null;

                    //全部/视频的动态流由 gRPC 提供；专栏/番剧的取数来源保持原样，不受兜底开关影响
                    var usesGrpcFeed = requestItemType == UserDynamicType.All || requestItemType == UserDynamicType.Video;
                    var serveRestFeed = usesGrpcFeed && restFeedMode;
                    //gRPC 被拒的原因（含 bili-status-code=-663 的说明），REST 兜底页也空时作为可见错误，不让页面无声空白
                    string restFallbackMessage = null;
                    if (usesGrpcFeed && !restFeedMode)
                    {
                        try
                        {
                            if (requestItemType == UserDynamicType.All)
                            {
                                var reply = await grpcService.GetDynAll(page, loadMore ? historyOffset : null);
                                if (!IsCurrentRequest(generation, requestDynamicType, requestItemType))
                                {
                                    LogHelper.Log("User dynamic VM stage=feed-request discarded generation=" + generation, LogType.DEBUG);
                                    return;
                                }
                                if (reply?.DynamicList == null)
                                {
                                    throw new InvalidOperationException("DynAll returned no dynamic list");
                                }
                                grpcCards = DynamicGrpcMapper.MapToUserCards(reply.DynamicList.List);
                                hasMore = reply.DynamicList.HasMore;
                                nextOffset = reply.DynamicList.HistoryOffset;
                            }
                            else
                            {
                                var reply = await grpcService.GetDynVideo(page, loadMore ? historyOffset : null, loadMore ? updateBaseline : null);
                                if (!IsCurrentRequest(generation, requestDynamicType, requestItemType))
                                {
                                    LogHelper.Log("User dynamic VM stage=feed-request discarded generation=" + generation, LogType.DEBUG);
                                    return;
                                }
                                if (reply == null)
                                {
                                    throw new InvalidOperationException("DynVideo returned no reply");
                                }
                                if (reply.DynamicList == null)
                                {
                                    throw new InvalidOperationException("DynVideo returned no dynamic list");
                                }
                                grpcCards = DynamicGrpcMapper.MapToUserCards(reply.DynamicList.List);
                                hasMore = reply.DynamicList.HasMore;
                                nextOffset = reply.DynamicList.HistoryOffset;
                                nextBaseline = reply.DynamicList.UpdateBaseline;
                            }
                        }
                        catch (Exception ex)
                        {
                            if (!IsCurrentRequest(generation, requestDynamicType, requestItemType))
                            {
                                LogHelper.Log("User dynamic VM stage=" + stage + " discarded generation=" + generation, LogType.DEBUG, ex);
                                return;
                            }
                            //接口拒绝（200 + 空体 + bili-status-code=-663 等）：错误文本进日志，同时把它当作切到 REST 动态流的触发条件
                            LogHelper.Log("User dynamic VM stage=rest-fallback reason=" + ex.Message + " itemType=" + requestItemType + " loadMore=" + loadMore + " generation=" + generation, LogType.INFO);
                            restFeedMode = true;
                            restFallbackMessage = DescribeFeedFailure("动态请求失败", ex.Message);
                            //gRPC 游标属于另一套分页，换源前清空，绝不会把 historyOffset/update_baseline 传给 REST
                            historyOffset = null;
                            updateBaseline = null;
                            //gRPC 页序号无法映射成 REST 游标：从 REST 第一页重新取游标，本页按首页发布
                            restOffset = null;
                            serveRestFeed = true;
                        }
                    }

                    if (serveRestFeed)
                    {
                        var restLoadMore = loadMore && !string.IsNullOrEmpty(restOffset);
                        var restPage = restLoadMore ? feedPage + 1 : 1;
                        var restCursor = restLoadMore ? restOffset : null;
                        stage = "rest-feed-request";
                        LogHelper.Log("User dynamic VM stage=rest-feed-request page=" + restPage + " offset=" + (restCursor ?? "") + " generation=" + generation, LogType.DEBUG);
                        var restApi = dynamicAPI.DyanmicNew(requestItemType, restCursor);
                        var restResponse = await restApi.Request();
                        if (!IsCurrentRequest(generation, requestDynamicType, requestItemType))
                        {
                            LogHelper.Log("User dynamic VM stage=rest-feed-request discarded generation=" + generation, LogType.DEBUG);
                            return;
                        }
                        if (!restResponse.status)
                        {
                            throw new InvalidOperationException(restResponse.message);
                        }
                        var restData = restResponse.GetJObject();
                        if (!IsCurrentRequest(generation, requestDynamicType, requestItemType))
                        {
                            LogHelper.Log("User dynamic VM stage=rest-feed-request discarded generation=" + generation, LogType.DEBUG);
                            return;
                        }
                        if (restData == null || restData["code"] == null || restData["code"].ToInt32() != 0 || restData["data"] == null)
                        {
                            throw new InvalidOperationException(restData?["message"]?.ToString() ?? restResponse.message);
                        }
                        stage = "rest-feed-card-convert";
                        var restCards = JsonConvert.DeserializeObject<List<DynamicCardModel>>(restData["data"]["cards"]?.ToString() ?? "[]", jsonSerializerSettings) ?? new List<DynamicCardModel>();
                        var restHasMore = ReadHasMoreFlag(restData["data"]);
                        var nextRestOffset = restData["data"]["offset"]?.ToString();
                        var restMapped = new ObservableCollection<DynamicItemDisplayModel>();
                        foreach (var card in restCards)
                        {
                            if (card?.desc == null)
                            {
                                continue;
                            }
                            restMapped.Add(ConvertToDisplay(card));
                        }
                        stage = "rest-feed-items-publish";
                        if (!IsCurrentRequest(generation, requestDynamicType, requestItemType))
                        {
                            LogHelper.Log("User dynamic VM stage=rest-feed-items-publish discarded generation=" + generation, LogType.DEBUG);
                            return;
                        }
                        if (restLoadMore && Items != null)
                        {
                            foreach (var item in restMapped)
                            {
                                Items.Add(item);
                            }
                        }
                        else
                        {
                            Items = restMapped;
                        }
                        feedPage = restPage;
                        restOffset = nextRestOffset;
                        //停止条件统一在 CanContinuePaging 里：服务端没有更多、游标没前进、连续空页到上限都停
                        var restCursorAdvanced = !string.IsNullOrEmpty(nextRestOffset) && !string.Equals(nextRestOffset, restCursor, StringComparison.Ordinal);
                        CanLoadMore = CanContinuePaging(restHasMore, restCursorAdvanced, restMapped.Count, requestItemType, generation);
                        lastPageFailed = false;
                        ShowError = false;
                        ErrorMsg = "";
                        LogHelper.Log("User dynamic VM stage=rest-feed-items-published page=" + feedPage + " count=" + restMapped.Count + " hasMore=" + restHasMore + " offset=" + (nextRestOffset ?? "") + " generation=" + generation, LogType.INFO);
                        if (!CanLoadMore && (Items == null || Items.Count == 0))
                        {
                            //分页已经停下，而且累积下来一张都没有：保留 gRPC 的拒绝说明，而不是显示一个无解释的空列表。
                            //游标还在前进时第一页为空只说明这一页没内容，继续翻页。
                            PublishFeedError(restFallbackMessage ?? "动态请求失败");
                        }
                        return;
                    }

                    if (requestItemType == UserDynamicType.Article)
                    {
                        var articleRequest = dynamicAPI.ArticleV2(page, loadMore ? articleOffset : null);
                        var response = await articleRequest.Request();
                        if (!IsCurrentRequest(generation, requestDynamicType, requestItemType))
                        {
                            LogHelper.Log("User dynamic VM stage=feed-request discarded generation=" + generation, LogType.DEBUG);
                            return;
                        }
                        if (!response.status)
                        {
                            throw new InvalidOperationException(response.message);
                        }
                        var data = await response.GetData<JObject>();
                        if (!IsCurrentRequest(generation, requestDynamicType, requestItemType))
                        {
                            LogHelper.Log("User dynamic VM stage=feed-request discarded generation=" + generation, LogType.DEBUG);
                            return;
                        }
                        if (data == null || !data.success || data.data == null)
                        {
                            throw new InvalidOperationException(data?.message ?? "Article feed returned no data");
                        }

                        var articleItems = data.data["items"] as JArray;
                        grpcCards = DynamicGrpcMapper.MapToUserArticleCards(articleItems);
                        hasMore = data.data["has_more"]?.Value<bool>() ?? false;
                        nextOffset = data.data["offset"]?.ToString();
                    }
                    else if (requestItemType == UserDynamicType.Season)
                    {
                        var reply = await grpcService.GetDynVideo(page, loadMore ? historyOffset : null, loadMore ? updateBaseline : null);
                        if (!IsCurrentRequest(generation, requestDynamicType, requestItemType))
                        {
                            LogHelper.Log("User dynamic VM stage=feed-request discarded generation=" + generation, LogType.DEBUG);
                            return;
                        }
                        if (reply == null)
                        {
                            throw new InvalidOperationException("DynVideo returned no reply");
                        }
                        grpcCards = DynamicGrpcMapper.MapToUserSeasonCards(reply.VideoFollowList?.List);
                        hasMore = false;
                    }

                    stage = "feed-card-convert";
                    var mapped = new ObservableCollection<DynamicItemDisplayModel>();
                    foreach (var card in grpcCards)
                    {
                        mapped.Add(ConvertToDisplay2024(card));
                    }

                    stage = "feed-items-publish";
                    if (!IsCurrentRequest(generation, requestDynamicType, requestItemType))
                    {
                        LogHelper.Log("User dynamic VM stage=feed-items-publish discarded generation=" + generation, LogType.DEBUG);
                        return;
                    }
                    if (loadMore && Items != null)
                    {
                        foreach (var item in mapped)
                        {
                            Items.Add(item);
                        }
                    }
                    else
                    {
                        Items = mapped;
                    }
                    feedPage = page;
                    historyOffset = nextOffset;
                    updateBaseline = nextBaseline;
                    if (requestItemType == UserDynamicType.Article)
                    {
                        articleOffset = nextOffset;
                    }
                    //与 REST 兜底同一套停止条件：hasMore 为 false、游标重复、连续空页到上限都会停。
                    //gRPC 动态流只认 history_offset，缺失或重复都说明下一页拿不到（空 offset 会让下一次请求回到不带
                    //History 的首页）；专栏还有独立的 page 参数在前进，只有返回的 offset 与本次相同才算重复。
                    var cursorAdvanced = requestItemType == UserDynamicType.Article
                        ? string.IsNullOrEmpty(nextOffset) || !string.Equals(nextOffset, requestCursor, StringComparison.Ordinal)
                        : !string.IsNullOrEmpty(nextOffset) && !string.Equals(nextOffset, requestCursor, StringComparison.Ordinal);
                    CanLoadMore = CanContinuePaging(hasMore, cursorAdvanced, mapped.Count, requestItemType, generation);
                    lastPageFailed = false;
                    ShowError = false;
                    ErrorMsg = "";
                    var itemTypes = string.Join(",", grpcCards.Select(item => item?.type ?? "null").Distinct());
                    var firstId = grpcCards.FirstOrDefault()?.id_str ?? "";
                    LogHelper.Log("User dynamic VM stage=feed-items-published page=" + feedPage + " count=" + grpcCards.Count + " hasMore=" + hasMore + " types=" + itemTypes + " firstDynamicId=" + firstId + " generation=" + generation, LogType.INFO);
                    return;
                }

                var api = dynamicAPI.DyanmicNew(requestItemType);
                switch (requestDynamicType)
                {
                    //UserDynamic 的所有分类都已在上面按来源（gRPC / REST 动态流 / 专栏 / 番剧）取数并返回
                    case DynamicType.Topic:
                        break;
                    case DynamicType.Space:
                        api = dynamicAPI.SpaceHistory(Uid, idx);
                        break;
                    default:
                        break;
                }
                var results = await api.Request();
                if (!IsCurrentRequest(generation, requestDynamicType, requestItemType))
                {
                    LogHelper.Log("User dynamic VM stage=response-received discarded generation=" + generation, LogType.DEBUG);
                    return;
                }
                stage = "response-received";
                LogHelper.Log("User dynamic VM stage=response-received status=" + results.status + " generation=" + generation, LogType.DEBUG);
                if (results.status)
                {
                    var data = results.GetJObject();
                    var responseCode = data == null ? -1 : data["code"].ToInt32();
                    LogHelper.Log("User dynamic VM stage=response-parsed code=" + responseCode, LogType.DEBUG);
                    if (data != null && responseCode == 0)
                    {
                        stage = "cards-deserialize";
                        var items = JsonConvert.DeserializeObject<List<DynamicCardModel>>(data["data"]?["cards"]?.ToString() ?? "[]", jsonSerializerSettings);
                        var itemTypes = string.Join(",", items.Select(item => item.desc == null ? "null" : item.desc.type.ToString()).Distinct());
                        LogHelper.Log("User dynamic VM stage=cards-deserialized count=" + items.Count + " types=" + itemTypes, LogType.DEBUG);
                        if (!IsCurrentRequest(generation, requestDynamicType, requestItemType))
                        {
                            LogHelper.Log("User dynamic VM stage=items-publish discarded generation=" + generation, LogType.DEBUG);
                            return;
                        }
                        if (items.Count > 0)
                        {
                            CanLoadMore = true;
                        }
                        ObservableCollection<DynamicItemDisplayModel> _ls = new ObservableCollection<DynamicItemDisplayModel>();
                        stage = "card-convert";
                        foreach (var item in items)
                        {
                            try
                            {
                                _ls.Add(ConvertToDisplay(item));
                            }
                            catch (Exception ex)
                            {
                                LogHelper.Log("User dynamic VM stage=card-convert-failed type=" + (item?.desc == null ? "null" : item.desc.type.ToString()) + " dynamicId=" + (item?.desc?.dynamic_id ?? ""), LogType.ERROR, ex);
                                throw;
                            }
                        }
                        stage = "items-publish";
                        if (Items == null)
                        {

                            Items = _ls;
                        }
                        else
                        {
                            foreach (var item in _ls)
                            {
                                Items.Add(item);
                            }
                        }
                        LogHelper.Log("User dynamic VM stage=items-published count=" + Items.Count + " generation=" + generation, LogType.DEBUG);
                        lastPageFailed = false;
                        ShowError = false;
                        ErrorMsg = "";
                    }
                    else
                    {
                        PublishFeedError(data["message"].ToString());
                    }
                }
                else
                {
                    PublishFeedError(results.message);

                }
            }
            catch (Exception ex)
            {
                if (!IsCurrentRequest(generation, requestDynamicType, requestItemType))
                {
                    LogHelper.Log("User dynamic VM stage=" + stage + " discarded generation=" + generation, LogType.DEBUG, ex);
                    return;
                }
                CanLoadMore = false;
                LogHelper.Log("User dynamic VM stage=" + stage + " failed", LogType.ERROR, ex);
                var handel = HandelError<AnimeHomeModel>(ex);
                PublishFeedError(DescribeFeedFailure(handel.message, ex.Message));
            }
            finally
            {
                if (generation == requestGeneration)
                {
                    feedRequestInFlight = false;
                    Loading = false;
                }
                else if (refreshQueued)
                {
                    //旧请求作废且期间有新刷新（切换分类）：现在才启动最新分类的请求
                    refreshQueued = false;
                    LogHelper.Log("User dynamic VM stage=refresh-dequeued supersededGeneration=" + generation + " itemType=" + UserDynamicType, LogType.INFO);
                    await GetDynamicItems();
                }
            }
        }
        private bool IsCurrentRequest(int generation, DynamicType dynamicType, UserDynamicType itemType)
        {
            return generation == requestGeneration && dynamicType == this.DynamicType && itemType == this.UserDynamicType;
        }
        //失败状态：记录以便重试，错因仅在列表为空时作为整页错误展示
        private void PublishFeedError(string message)
        {
            lastPageFailed = true;
            //本页失败即停止翻页：不再依赖短视口检查去重试失败的页
            CanLoadMore = false;
            if (Items == null || Items.Count == 0)
            {
                ShowError = true;
                ErrorMsg = message;
            }
            Utils.ShowMessageToast(message);
        }
        //翻页停止条件：服务端说没有更多、本页游标没前进（重复游标会把同一页无限重复），
        //或连续空页达到上限（服务端页全被过滤成 0 张卡片时的兜底）。
        private bool CanContinuePaging(bool serverHasMore, bool cursorAdvanced, int pageItemCount, UserDynamicType itemType, int generation)
        {
            if (pageItemCount == 0)
            {
                consecutiveEmptyPages++;
            }
            else
            {
                consecutiveEmptyPages = 0;
            }
            if (consecutiveEmptyPages >= EmptyPageCap)
            {
                //只在达到上限的那一页记一条：CanLoadMore 随后为 false，不会再有下一页请求
                LogHelper.Log("User dynamic VM stage=empty-page-cap itemType=" + itemType + " emptyPages=" + consecutiveEmptyPages + " generation=" + generation, LogType.INFO);
                return false;
            }
            if (!serverHasMore)
            {
                return false;
            }
            if (!cursorAdvanced)
            {
                LogHelper.Log("User dynamic VM stage=cursor-repeated itemType=" + itemType + " generation=" + generation, LogType.INFO);
                return false;
            }
            return true;
        }
        //REST 动态流的 has_more 可能是 true/false，也可能是 0/1
        private static bool ReadHasMoreFlag(JToken data)
        {
            var token = data?["has_more"];
            if (token == null || token.Type == JTokenType.Null)
                return false;
            return token.Type == JTokenType.Boolean ? token.Value<bool>() : token.ToInt32() != 0;
        }
        private static readonly Regex BiliStatusCodePattern = new Regex(@"bili-status-code=(-?\d+)", RegexOptions.Compiled);
        private static readonly Regex GrpcMessageCodePattern = new Regex(@"grpc-message=(-?\d+)", RegexOptions.Compiled);
        //传输层把服务端码写在异常文本里，取出来给用户一个能对上的说法
        private static string DescribeFeedFailure(string fallback, string transportMessage)
        {
            int code;
            if (!TryReadServerCode(transportMessage, out code))
                return fallback;
            if (code == -352 || code == -412 || code == -663)
                return "服务端拒绝了动态请求（" + code + "），请稍后重试";
            return "动态请求失败（" + code + "）";
        }
        private static bool TryReadServerCode(string transportMessage, out int code)
        {
            code = 0;
            if (string.IsNullOrEmpty(transportMessage))
                return false;
            var match = BiliStatusCodePattern.Match(transportMessage);
            if (!match.Success)
                match = GrpcMessageCodePattern.Match(transportMessage);
            return match.Success
                && int.TryParse(match.Groups[1].Value, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out code);
        }
        public async Task GetDynamicDetail(string id)
        {
            try
            {
                CanLoadMore = false;
                Loading = true;
                var api = dynamicAPI.DynamicDetail(id);

                var results = await api.Request();
                if (results.status)
                {
                    var data = results.GetJObject();
                    if (data["code"].ToInt32() == 0)
                    {
                        var item = JsonConvert.DeserializeObject<DynamicCardModel>(data["data"]?.ToString() ?? "{}", jsonSerializerSettings);
                        ObservableCollection<DynamicItemDisplayModel> _ls = new ObservableCollection<DynamicItemDisplayModel>();
                        _ls.Add(ConvertToDisplay(item));
                        Items = _ls;
                    }
                    else
                    {
                        Utils.ShowMessageToast(data["message"].ToString());
                    }
                }
                else
                {
                    Utils.ShowMessageToast(results.message);

                }
            }
            catch (Exception ex)
            {
                var handel = HandelError<AnimeHomeModel>(ex);
                Utils.ShowMessageToast(handel.message);
            }
            finally
            {
                Loading = false;

            }
        }
        private DynamicItemDisplayModel ConvertToDisplay(DynamicCardModel item)
        {
            try
            {

                var card = JObject.Parse(item.card);
                var extend_json = JObject.Parse(item.extend_json);
                var display=item.display;
                var data = new DynamicItemDisplayModel()
                {
                    CommentCount = item.desc.comment,
                    Datetime = Utils.TimestampToDatetime(item.desc.timestamp).ToString(),
                    DynamicID = item.desc.dynamic_id,
                    LikeCount = item.desc.like,
                    Mid = item.desc.uid,
                    ShareCount = item.desc.repost,
                    Time = Utils.HandelTimestamp(item.desc.timestamp.ToString()),
                    IntType = item.desc.type,
                    ReplyID = item.desc.rid_str,
                    ReplyType = item.desc.r_type,
                    Type = DynamicParse.ParseType(item.desc.type),
                    UserCommand = UserCommand,
                    LaunchUrlCommand = LaunchUrlCommand,
                    WebCommand = WebCommand,
                    TagCommand = TagCommand,
                    LotteryCommand = LotteryCommand,
                    VoteCommand = VoteCommand,
                    IsSelf = item.desc.uid == SettingHelper.Account.UserID,
                    ImageCommand = ImageCommand,
                    SaveImageCommand = SaveImageCommand,
                    CommentCommand = CommentCommand,
                    LikeCommand = LikeCommand,
                    DeleteCommand = DeleteCommand,
                    RepostCommand = RepostCommand,
                    DetailCommand = DetailCommand,
                    WatchLaterCommand = watchLaterVM.AddCommand,
                    Liked = item.desc.is_liked == 1
                };
                if (data.Type == DynamicDisplayType.Other)
                {
                    return new DynamicItemDisplayModel()
                    {
                        Type = DynamicDisplayType.Other,
                        IntType = 999,
                        DynamicID = item.desc.dynamic_id,
                        ContentStr = ""
                    };
                }
                data.OneRowInfo = DynamicParse.ParseOneRowInfo(data.Type, card);
                if (data.Type == DynamicDisplayType.ShortVideo)
                {
                    data.ShortVideoInfo = DynamicParse.ParseShortVideoInfo(card);
                }
                if (data.Type == DynamicDisplayType.Photo)
                {
                    List<DyanmicItemDisplayImageInfo> imgs = new List<DyanmicItemDisplayImageInfo>();
                    List<string> allImageUrl = new List<string>();
                    int i = 0;
                    foreach (var img in card["item"]["pictures"])
                    {
                        allImageUrl.Add(img["img_src"].ToString());
                        imgs.Add(new DyanmicItemDisplayImageInfo()
                        {
                            ImageUrl = img["img_src"].ToString(),
                            Height = img["img_height"].ToInt32(),
                            Width = img["img_width"].ToInt32(),
                            Index = i,
                            ImageCommand = ImageCommand,
                            SaveImageCommand = SaveImageCommand
                        });
                        i++;
                    }

                    //偷懒方法，点击图片时可以获取全部图片信息，好孩子不要学
                    imgs.ForEach((x) => x.AllImages = allImageUrl);

                    data.ImagesInfo = imgs;
                }

                if (data.Type == DynamicDisplayType.Repost)
                {
                    if (card.ContainsKey("origin_user"))
                    {
                        var originUser = JsonConvert.DeserializeObject<DynamicCardDescUserProfileModel>(card["origin_user"].ToString());
                        var originDisplay = ConvertToDisplay(new DynamicCardModel()
                        {
                            extend_json = card["origin_extend_json"].ToString(),
                            card = card["origin"].ToString(),
                            display = item.display?.origin,
                            desc = new DynamicCardDescModel()
                            {
                                user_profile = originUser,
                                uid = originUser.info.uid,
                                dynamic_id = item.desc.orig_dy_id,
                                type = item.desc.orig_type
                            }
                        });
                        originDisplay.IsRepost = true;
                        data.OriginInfo = new List<DynamicItemDisplayModel>() { originDisplay };
                    }
                    else
                    {

                        data.OriginInfo = new List<DynamicItemDisplayModel>() {
                        new DynamicItemDisplayModel()
                        {
                            IsRepost=true,
                            IntType=1024,
                            Type= DynamicDisplayType.Miss
                        }
                    };
                    }

                }
                if (item.desc.comment == 0 && card.ContainsKey("stat"))
                {
                    data.CommentCount = card["stat"]["reply"].ToInt32();
                }
                //Season数据会出现desc.comment为0的情况
                if (item.desc.comment == 0 && card.ContainsKey("reply_count"))
                {
                    data.CommentCount = card["reply_count"].ToInt32();
                }
                //专栏数据会出现desc.comment为0的情况
                if (item.desc.comment == 0 && card.ContainsKey("stats"))
                {
                    data.CommentCount = card["stats"]["reply"].ToInt32();
                }
                var content = "";
                //内容
                if (card.ContainsKey("item") && card["item"]["content"] != null)
                {
                    content = card["item"]["content"]?.ToString();
                    extend_json["at_control"] = card["item"]["ctrl"];
                }
                else if (card.ContainsKey("item") && card["item"]["description"] != null)
                {
                    content = card["item"]["description"]?.ToString();
                    extend_json["at_control"] = card["item"]["at_control"];
                }
                else if (card.ContainsKey("dynamic"))
                {
                    content = card["dynamic"]?.ToString();
                }
                else if (card.ContainsKey("vest") && card["vest"]["content"] != null)
                {
                    content = card["vest"]["content"]?.ToString();
                }


                //直播预约
                if (display?.add_on_card_info != null)
                {
                    var arr = new JArray();
                    foreach (var _info in display.add_on_card_info)
                    {
                        var info = _info.reserve_attach_card;
                        if (info == null) { continue; }
                        var tp = info.type;
                        if (tp != null && tp.ToString() == "reserve")
                        {
                            //这个是一个预约
                            var time = info.desc_first?.text;
                            var people = info.desc_second;
                            var title = info.title;
                            arr.Add(new JObject(
                                new JProperty("time", time),
                                new JProperty("people", people),
                                new JProperty("title", title)
                            ));
                        }
                    }
                    extend_json["直播"] = arr;
                }


                if (!string.IsNullOrEmpty(content))
                {
                    data.ContentStr = content;
                    data.Content = DynamicParse.StringToRichText(item.desc.dynamic_id, content, item.display?.emoji_info?.emoji_details, extend_json);
                }
                else
                {
                    data.ShowContent = false;
                }

                if (item.desc.user_profile == null && card["owner"] != null)
                {
                    data.UserName = card["owner"]["name"]?.ToString();
                    data.Photo = card["owner"]["face"]?.ToString();
                    data.Verify = AppHelper.TRANSPARENT_IMAGE;
                    data.Mid = long.Parse(card["owner"]["mid"]?.ToString());
                }
                if (item.desc.user_profile != null)
                {
                    data.UserName = item.desc.user_profile.info.uname;
                    data.Photo = item.desc.user_profile.info.face;
                    if (item.desc.user_profile.vip != null)
                    {
                        data.IsYearVip = item.desc.user_profile.vip.vipStatus == 1 && item.desc.user_profile.vip.vipType == 2;
                    }
                    switch (item.desc.user_profile.card?.official_verify?.type ?? 3)
                    {
                        case 0:
                            data.Verify = AppHelper.VERIFY_PERSONAL_IMAGE;
                            break;
                        case 1:
                            data.Verify = AppHelper.VERIFY_OGANIZATION_IMAGE;
                            break;
                        default:
                            data.Verify = AppHelper.TRANSPARENT_IMAGE;
                            break;
                    }
                    if (!string.IsNullOrEmpty(item.desc.user_profile.pendant?.image))
                    {
                        data.Pendant = item.desc.user_profile.pendant.image;
                    }
                    //装扮
                    data.DecorateName = item.desc.user_profile.decorate_card?.name;
                    data.DecorateText = item.desc.user_profile.decorate_card?.fan?.num_desc;
                    data.DecorateColor = item.desc.user_profile.decorate_card?.fan?.color;
                    data.DecorateImage = item.desc.user_profile.decorate_card?.big_card_url;
                }



                if (card.ContainsKey("apiSeasonInfo"))
                {
                    data.UserName = card["apiSeasonInfo"]["title"].ToString();
                    data.Photo = card["apiSeasonInfo"]["cover"].ToString();
                    data.TagName = card["apiSeasonInfo"]["type_name"].ToString();
                    data.ShowTag = true;
                    data.Time = data.Time + "更新了";
                }
                if (card.ContainsKey("season"))
                {
                    data.UserName = card["season"]["title"].ToString();
                    data.Photo = card["season"]["cover"].ToString();
                    data.TagName = card["season"]["type_name"].ToString();
                    data.ShowTag = true;
                    data.Time = data.Time + "更新了";
                }
                return data;
            }
            catch (Exception)
            {
                return new DynamicItemDisplayModel()
                {
                    Type = DynamicDisplayType.Other,
                    IntType = 999,
                    DynamicID = item.desc.dynamic_id,
                    ContentStr = "动态加载失败"
                };
            }

        }
        private DynamicItemDisplayModel ConvertToDisplay2024(DynamicCardModel2024 item)
        {
            try
            {
                var t = DynamicParse.ParseType2024(item.type);
                if (t == DynamicDisplayType.Other)
                {
                    return new DynamicItemDisplayModel()
                    {
                        Type = DynamicDisplayType.Other,
                        IntType = 999,
                        DynamicID = item.id_str,
                        ContentStr = ""
                    };
                }
                var data = new DynamicItemDisplayModel()
                {
                    CommentCount = item.comment,
                    Datetime = Utils.TimestampToDatetime(item.pubdate).ToString(),
                    DynamicID = item.id_str,
                    LikeCount = item.like,
                    Mid = item.mid,
                    ShareCount = item.forward,
                    Time = string.IsNullOrEmpty(item.modules?.module_author?.pub_time)
                        ? (item.pubdate <= 0 ? "" : Utils.HandelTimestamp(item.pubdate.ToString()))
                        : item.modules.module_author.pub_time,
                    IntType = t.ToInt32(),// item.type,
                    ReplyID = item.basic.rid_str,
                    ReplyType = item.basic.comment_type,
                    Type = t,
                    UserCommand = UserCommand,
                    LaunchUrlCommand = LaunchUrlCommand,
                    WebCommand = WebCommand,
                    TagCommand = TagCommand,
                    LotteryCommand = LotteryCommand,
                    VoteCommand = VoteCommand,
                    IsSelf = item.mid == SettingHelper.Account.UserID,
                    ImageCommand = ImageCommand,
                    SaveImageCommand = SaveImageCommand,
                    CommentCommand = CommentCommand,
                    LikeCommand = LikeCommand,
                    DeleteCommand = DeleteCommand,
                    RepostCommand = RepostCommand,
                    DetailCommand = DetailCommand,
                    WatchLaterCommand = watchLaterVM.AddCommand,
                    Liked = item.liked
                };
                //prefix
                if(data.Type == DynamicDisplayType.Repost)
                {
                    var orig = ConvertToDisplay2024(item.orig);
                    orig.IsRepost = true;
                    data.OriginInfo =new List<DynamicItemDisplayModel> { orig };
                }
                data.OneRowInfo = DynamicParse.ParseOneRowInfo2024(data, item);
                //post fix
                if (data.ImagesInfo != null)
                {
                    foreach (var i in data.ImagesInfo)
                    {
                        i.SaveImageCommand = SaveImageCommand;
                        i.ImageCommand = ImageCommand;
                    }
                }
                
                data.Datetime = string.IsNullOrEmpty(item.modules?.module_author?.pub_time)
                    ? (item.pubdate <= 0 ? "" : Utils.TimestampToDatetime(item.pubdate).ToString())
                    : item.modules.module_author.pub_time;
                data.ShowContent = false;
                if ( item.owner!= null)
                {
                    data.UserName = item.owner;
                    data.Photo =item.face?.ToString();
                    data.Verify = AppHelper.TRANSPARENT_IMAGE;
                    data.Mid = item.mid;
                }
                if (item.modules.module_author!= null)
                {
                    var d = item.modules.module_author;
                    data.UserName = d.name;
                    data.Photo = d.face;
                    if (d.vip != null)
                    {
                        data.IsYearVip = d.vip.status == 1 && d.vip.type == 2;
                    }
                    switch (d.official_verify?.type ?? 3)
                    {
                        case 0:
                            data.Verify = AppHelper.VERIFY_PERSONAL_IMAGE;
                            break;
                        case 1:
                            data.Verify = AppHelper.VERIFY_OGANIZATION_IMAGE;
                            break;
                        default:
                            data.Verify = AppHelper.TRANSPARENT_IMAGE;
                            break;
                    }
                    if (!string.IsNullOrEmpty(d.pendant?.image))
                    {
                        data.Pendant = d.pendant.image;
                    }
                    //装扮
                    data.DecorateName = d.decorate?.name;
                    data.DecorateText = d.decorate?.fan?.number.ToString();
                    data.DecorateColor = d.decorate?.fan?.color;
                    data.DecorateImage = d.decorate?.card_url ;
                }


                /*
                if (card.ContainsKey("apiSeasonInfo"))
                {
                    data.UserName = card["apiSeasonInfo"]["title"].ToString();
                    data.Photo = card["apiSeasonInfo"]["cover"].ToString();
                    data.TagName = card["apiSeasonInfo"]["type_name"].ToString();
                    data.ShowTag = true;
                    data.Time = data.Time + "更新了";
                }
                if (card.ContainsKey("season"))
                {
                    data.UserName = card["season"]["title"].ToString();
                    data.Photo = card["season"]["cover"].ToString();
                    data.TagName = card["season"]["type_name"].ToString();
                    data.ShowTag = true;
                    data.Time = data.Time + "更新了";
                }
                */
                return data;
            }
            catch (Exception)
            {
                return new DynamicItemDisplayModel()
                {
                    Type = DynamicDisplayType.Other,
                    IntType = 999,
                    DynamicID = item.id_str,
                    ContentStr = "动态加载失败"
                };
            }

        }

    }


}
