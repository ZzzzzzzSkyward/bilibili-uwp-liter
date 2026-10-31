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
using System.Diagnostics;
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
        //web 聚合动态流的翻页游标（data.offset）：第三套分页，同样不与上面两套互传
        private string polymerOffset;
        //本次全部/视频动态流固定的来源：刷新/切分类时由选择器与固定顺序共同决定，翻页沿用；
        //只有同一次请求里前面几路都失败、后面某一路成功时才改到这一路
        private DynamicFeedSource? pinnedFeedSource;
        //切到 REST 后仍没内容时用来解释空列表的那句拒绝文案
        private string feedFallbackReason;
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
        //个人空间动态（Space）的 gRPC 分页状态：页序号与 history_offset 自成一套，
        //既不与全部/视频动态流的 historyOffset/updateBaseline 共享，也不与 SpaceHistory 的 REST 游标（idx）互传
        private int spacePage = 1;
        private string spaceOffset;
        //本次个人空间动态固定的来源：刷新/换用户时由选择器重新决定，翻页沿用
        private DynamicApiSource? pinnedSpaceSource;
        //spacePage/spaceOffset/pinnedSpaceSource 属于哪个用户；Uid 变了这组状态必须作废
        private string spaceUid;
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
            polymerOffset = null;
            consecutiveEmptyPages = 0;
            //固定的来源不跨刷新继承：刷新就是新的一屏边界，交回选择器重新决定（一屏之内最多三路）
            pinnedFeedSource = null;
            pinnedSpaceSource = null;
            feedFallbackReason = null;
            LogHelper.Log("User dynamic VM stage=refresh-source generation=" + requestGeneration + " itemType=" + UserDynamicType, LogType.DEBUG);
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
        //Space 的 gRPC 分页状态回到初始：页序号、游标、REST 兜底开关一起复位，并记住这组状态属于哪个用户
        private void ResetSpaceFeed()
        {
            spacePage = 1;
            spaceOffset = null;
            pinnedSpaceSource = null;
            spaceUid = Uid;
        }
        public async Task GetDynamicItems(string idx = "", bool loadMore = false)
        {
            //代号在第一个 await 之前取得，之后只认这一份分类/代号
            var generation = ++requestGeneration;
            var requestDynamicType = DynamicType;
            var requestItemType = UserDynamicType;
            refreshQueued = false;
            //换了用户：上一用户的页序号/游标/兜底开关全部作废，本次按第一页重新开始（不能续在别人的列表后面）
            var spaceStateReset = false;
            if (requestDynamicType == DynamicType.Space && !string.Equals(spaceUid, Uid, StringComparison.Ordinal))
            {
                spaceStateReset = true;
                ResetSpaceFeed();
                LogHelper.Log("User dynamic VM stage=space-uid-changed generation=" + generation + " uid=" + (Uid ?? ""), LogType.INFO);
            }
            var stage = "request-start";
            try
            {
                CanLoadMore = false;
                feedRequestInFlight = true;
                Loading = true;
                if (!loadMore)
                {
                    ResetSpaceFeed();
                    lastPageFailed = false;
                    ShowError = false;
                    ErrorMsg = "";
                    feedPage = 1;
                    historyOffset = null;
                    updateBaseline = null;
                    articleOffset = null;
                    restOffset = null;
                    polymerOffset = null;
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

                    //全部/视频的动态流由选择器在每一屏边界决定来源；专栏/番剧的取数来源保持原样
                    var usesGrpcFeed = requestItemType == UserDynamicType.All || requestItemType == UserDynamicType.Video;
                    if (usesGrpcFeed)
                    {
                        await ServeUserFeedAsync(generation, requestDynamicType, requestItemType, loadMore);
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
                        grpcCards = DynamicGrpcMapper.MapToUserFeedCards(articleItems);
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

                //个人空间动态：每一屏边界由选择器决定来源，翻页沿用；首选失败只再试另一路一次，
                //换源从第一页重新开始并由成功的那一页顶替已发布的行，失败则保留现有行
                if (requestDynamicType == DynamicType.Space)
                {
                    await ServeSpaceFeedAsync(generation, requestDynamicType, requestItemType, idx, loadMore, spaceStateReset);
                    return;
                }

                var api = dynamicAPI.DyanmicNew(requestItemType);
                switch (requestDynamicType)
                {
                    //UserDynamic 与 Space 的所有分类都已在上面按来源取数并返回，这里只剩话题动态
                    case DynamicType.Topic:
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

        private static DynamicApiOperation FeedOperation(UserDynamicType itemType)
        {
            return itemType == UserDynamicType.Video ? DynamicApiOperation.UserVideoFeed : DynamicApiOperation.UserAllFeed;
        }

        //全部/视频动态流：一屏之内固定三路顺序（web 聚合流 → 选择器首选 → 另一路），
        //翻页只认固定下来的那一路，绝不换源（换源会从第一页重来并顶替掉已上屏的行）
        private async Task ServeUserFeedAsync(int generation, DynamicType dynamicType, UserDynamicType itemType, bool loadMore)
        {
            var operation = FeedOperation(itemType);
            //上下文在第一个 await 之前捕获：中途换账号也只记到这次请求的桶里
            var context = DynamicApiSelection.CaptureContext();
            var boundary = !loadMore || !pinnedFeedSource.HasValue;
            var decision = boundary
                ? DynamicApiSelection.Choose(context, operation, true, "user-feed itemType=" + itemType)
                : null;
            var chain = boundary
                ? UserFeedChain(DynamicApiSelection.ToFeedSource(decision.Source))
                : new[] { pinnedFeedSource.Value };
            DynamicApiOutcome firstOutcome = DynamicApiOutcome.Success;
            DynamicApiOutcome lastOutcome = DynamicApiOutcome.Success;
            string legacyFailureMessage = null;
            for (var i = 0; i < chain.Length; i++)
            {
                var source = chain[i];
                //换源不传递游标：换过来的那一路一律从第一页重新取，成功的那一页整体顶替已发布的行
                var legLoadMore = i == 0 && loadMore;
                var result = await TryUserFeedSourceAsync(source, generation, dynamicType, itemType, legLoadMore, context);
                if (result.Retired)
                {
                    return;
                }
                if (result.Outcome == DynamicApiOutcome.Success)
                {
                    feedFallbackReason = null;
                    pinnedFeedSource = source;
                    return;
                }
                if (i == 0)
                {
                    firstOutcome = result.Outcome;
                }
                lastOutcome = result.Outcome;
                if (source == DynamicFeedSource.Legacy)
                {
                    //用户看到的提示只取旧 REST 那一路的失败原因
                    legacyFailureMessage = result.FailureMessage;
                }
                feedFallbackReason = DescribeFeedFailure("动态请求失败", result.FailureMessage);
                LogHelper.Log("User dynamic VM stage=feed-source-failed itemType=" + itemType + " page=" + (feedPage + (legLoadMore ? 1 : 0))
                    + " source=" + source + " outcome=" + result.Outcome, LogType.DEBUG);
            }
            if (!boundary)
            {
                //翻页失败绝不换源：换源会从第一页重来，成功后就顶替掉已上屏的行；
                //用户正在看的内容也不该被一条错误提示打断，所以这里只记日志
                lastPageFailed = true;
                CanLoadMore = false;
                LogHelper.Log("User dynamic VM stage=feed-failed page=" + (feedPage + 1) + " source=" + chain[0]
                    + " outcome=" + lastOutcome, LogType.ERROR);
                return;
            }
            DynamicApiSelection.LogExhausted(operation, firstOutcome, lastOutcome, "user-feed itemType=" + itemType);
            if (!IsCurrentRequest(generation, dynamicType, itemType))
            {
                return;
            }
            LogHelper.Log("User dynamic VM stage=feed-failed itemType=" + itemType + " first=" + firstOutcome
                + " second=" + lastOutcome + " message=" + (legacyFailureMessage ?? ""), LogType.ERROR);
            //第一页全部失败才提示，提示内容只取 REST 那一路的失败原因
            PublishFeedError(DescribeFeedFailure("动态请求失败", legacyFailureMessage));
        }

        /// <summary>
        /// 全部/视频动态的取数顺序：web 聚合流 → 选择器首选 → 另一路。
        /// 聚合流排在前面：它是这套账号下唯一还会返回游标的动态源（gRPC 被 -663 拒，旧接口没有游标）。
        /// </summary>
        private static DynamicFeedSource[] UserFeedChain(DynamicFeedSource preferred)
        {
            return preferred == DynamicFeedSource.Grpc
                ? new[] { DynamicFeedSource.Polymer, DynamicFeedSource.Grpc, DynamicFeedSource.Legacy }
                : new[] { DynamicFeedSource.Polymer, DynamicFeedSource.Legacy, DynamicFeedSource.Grpc };
        }

        private Task<DynamicApiSourceResult> TryUserFeedSourceAsync(DynamicFeedSource source, int generation, DynamicType dynamicType,
            UserDynamicType itemType, bool loadMore, DynamicApiContext context)
        {
            switch (source)
            {
                case DynamicFeedSource.Polymer:
                    return TryUserFeedPolymerAsync(generation, dynamicType, itemType, loadMore);
                case DynamicFeedSource.Grpc:
                    return TryUserFeedGrpcAsync(generation, dynamicType, itemType, loadMore, context);
                default:
                    return TryUserFeedRestAsync(generation, dynamicType, itemType, loadMore, context);
            }
        }

        /// <summary>
        /// 全部/视频动态流的 web 聚合流（x/polymer/web-dynamic/v1/feed/all?type=all|video）。
        /// Cookie 鉴权，分页游标是响应里的 data.offset，只在这一路里传递。
        /// </summary>
        private async Task<DynamicApiSourceResult> TryUserFeedPolymerAsync(int generation, DynamicType dynamicType, UserDynamicType itemType,
            bool loadMore)
        {
            var feedType = itemType == UserDynamicType.Video ? "video" : "all";
            var polymerLoadMore = loadMore && !string.IsNullOrEmpty(polymerOffset);
            var page = polymerLoadMore ? feedPage + 1 : 1;
            var cursor = polymerLoadMore ? polymerOffset : null;
            LogHelper.Log("User dynamic VM stage=feed-polymer-request page=" + page + " offset=" + (cursor ?? "")
                + " generation=" + generation, LogType.DEBUG);
            HttpResults response;
            try
            {
                response = await dynamicAPI.ArticleV2(page, cursor, feedType).Request();
            }
            catch (Exception ex)
            {
                var failed = DynamicApiSelection.ClassifyRequestFailure(ex);
                if (failed == DynamicApiOutcome.IgnoredFailure)
                {
                    throw;
                }
                return DynamicApiSourceResult.Failed(failed, ex.Message);
            }
            if (!IsCurrentRequest(generation, dynamicType, itemType))
            {
                return DynamicApiSourceResult.Superseded();
            }
            var data = response != null && response.status ? response.GetJObject() : null;
            var businessCode = data?["code"] == null ? (int?)null : data["code"].ToInt32();
            var envelope = DynamicApiSelection.ClassifyLegacy(response, businessCode);
            if (envelope != DynamicApiOutcome.Success || data["data"] == null)
            {
                //信封合法但没有数据，或者业务码本身不是 0：都算这次没拿到有效页
                var failed = envelope == DynamicApiOutcome.Success ? DynamicApiOutcome.MalformedResponse : envelope;
                return DynamicApiSourceResult.Failed(failed, data?["message"]?.ToString() ?? response?.message);
            }

            var feedData = data["data"];
            bool hasMore;
            string nextOffset;
            ObservableCollection<DynamicItemDisplayModel> mapped;
            try
            {
                //信封合法不代表字段都合规：少一层对象或多一个字符串，解码/映射里任何一步都可能抛。
                //这一路只算"没拿到有效页"，交给换源链决定下一步，异常绝不掀掉整个请求
                var items = feedData["items"] as JArray;
                var cards = DynamicGrpcMapper.MapToUserFeedCards(items);
                hasMore = feedData["has_more"]?.Value<bool>() ?? false;
                nextOffset = feedData["offset"]?.ToString();
                mapped = new ObservableCollection<DynamicItemDisplayModel>();
                foreach (var card in cards)
                {
                    mapped.Add(ConvertToDisplay2024(card));
                }
            }
            catch (Exception ex)
            {
                LogHelper.Log("User dynamic VM stage=feed-polymer-failed page=" + page + " generation=" + generation
                    + " message=" + ex.Message, LogType.ERROR, ex);
                return DynamicApiSourceResult.Failed(DynamicApiOutcome.MalformedResponse, ex.Message);
            }
            if (!IsCurrentRequest(generation, dynamicType, itemType))
            {
                return DynamicApiSourceResult.Superseded();
            }
            if (polymerLoadMore && Items != null)
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
            polymerOffset = nextOffset;
            //换到聚合流之后另外两套游标作废：三种分页不互传
            historyOffset = null;
            updateBaseline = null;
            restOffset = null;
            //停止条件统一在 CanContinuePaging 里：服务端没有更多、游标没前进、连续空页到上限都停
            var cursorAdvanced = !string.IsNullOrEmpty(nextOffset) && !string.Equals(nextOffset, cursor, StringComparison.Ordinal);
            CanLoadMore = CanContinuePaging(hasMore, cursorAdvanced, mapped.Count, itemType, generation);
            lastPageFailed = false;
            ShowError = false;
            ErrorMsg = "";
            LogHelper.Log("User dynamic VM stage=feed-items-published source=polymer page=" + feedPage + " count=" + mapped.Count
                + " hasMore=" + hasMore + " offset=" + (nextOffset ?? "") + " generation=" + generation, LogType.INFO);
            if (!CanLoadMore && (Items == null || Items.Count == 0))
            {
                //分页已经停下，而且一张都没有：把换源时那条拒绝说明摆出来，不让页面无声空白
                PublishFeedError(feedFallbackReason ?? "动态请求失败");
            }
            return DynamicApiSourceResult.Published();
        }

        /// <summary>
        /// 全部/视频动态流的 gRPC 一路。loadMore 为 false 表示这是换源重启：按第一页取并整体顶替已发布的页。
        /// </summary>
        private async Task<DynamicApiSourceResult> TryUserFeedGrpcAsync(int generation, DynamicType dynamicType, UserDynamicType itemType,
            bool loadMore, DynamicApiContext context)
        {
            string failureMessage = null;
            var operation = FeedOperation(itemType);
            var attempt = DynamicApiSelection.CreateAttempt(context, operation, DynamicApiSource.Grpc);
            var stopwatch = Stopwatch.StartNew();
            var page = loadMore ? feedPage + 1 : 1;
            //本页请求所用的游标：发布时与返回值比较，判断游标是否真的前进
            var requestCursor = loadMore ? historyOffset : null;
            List<DynamicCardModel2024> grpcCards = null;
            var hasMore = false;
            string nextOffset = null;
            string nextBaseline = null;
            const string stage = "feed-grpc-request";
            try
            {
                if (itemType == UserDynamicType.All)
                {
                    var reply = await grpcService.GetDynAll(page, loadMore ? historyOffset : null);
                    if (!IsCurrentRequest(generation, dynamicType, itemType))
                    {
                        DynamicApiSelection.Record(attempt, DynamicApiOutcome.IgnoredFailure, stopwatch.Elapsed, stage + " superseded");
                        return DynamicApiSourceResult.Superseded();
                    }
                    if (reply == null || reply.DynamicList == null)
                    {
                        DynamicApiSelection.Record(attempt, DynamicApiOutcome.MalformedResponse, stopwatch.Elapsed, stage);
                        failureMessage = "DynAll returned no dynamic list";
                        return DynamicApiSourceResult.Failed(DynamicApiOutcome.MalformedResponse, failureMessage);
                    }
                    grpcCards = DynamicGrpcMapper.MapToUserCards(reply.DynamicList.List);
                    hasMore = reply.DynamicList.HasMore;
                    nextOffset = reply.DynamicList.HistoryOffset;
                }
                else
                {
                    var reply = await grpcService.GetDynVideo(page, loadMore ? historyOffset : null, loadMore ? updateBaseline : null);
                    if (!IsCurrentRequest(generation, dynamicType, itemType))
                    {
                        DynamicApiSelection.Record(attempt, DynamicApiOutcome.IgnoredFailure, stopwatch.Elapsed, stage + " superseded");
                        return DynamicApiSourceResult.Superseded();
                    }
                    if (reply == null || reply.DynamicList == null)
                    {
                        DynamicApiSelection.Record(attempt, DynamicApiOutcome.MalformedResponse, stopwatch.Elapsed, stage);
                        failureMessage = "DynVideo returned no dynamic list";
                        return DynamicApiSourceResult.Failed(DynamicApiOutcome.MalformedResponse, failureMessage);
                    }
                    grpcCards = DynamicGrpcMapper.MapToUserCards(reply.DynamicList.List);
                    hasMore = reply.DynamicList.HasMore;
                    nextOffset = reply.DynamicList.HistoryOffset;
                    nextBaseline = reply.DynamicList.UpdateBaseline;
                }
            }
            catch (Exception ex)
            {
                var failed = DynamicApiSelection.ClassifyRequestFailure(ex);
                DynamicApiSelection.Record(attempt, failed, stopwatch.Elapsed, stage);
                if (failed == DynamicApiOutcome.IgnoredFailure)
                {
                    //原因与端点可用性无关（取消）：交回原有错误呈现
                    throw;
                }
                //gRPC 游标属于另一套分页，换源前清空，绝不会把 historyOffset/update_baseline 传给 REST
                historyOffset = null;
                updateBaseline = null;
                failureMessage = ex.Message;
                return DynamicApiSourceResult.Failed(failed, failureMessage);
            }
            DynamicApiSelection.Record(attempt, DynamicApiOutcome.Success, stopwatch.Elapsed, stage);

            var mapped = new ObservableCollection<DynamicItemDisplayModel>();
            foreach (var card in grpcCards)
            {
                mapped.Add(ConvertToDisplay2024(card));
            }
            if (!IsCurrentRequest(generation, dynamicType, itemType))
            {
                return DynamicApiSourceResult.Superseded();
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
            //gRPC 动态流只认 history_offset：缺失或重复都说明下一页拿不到（空 offset 会让下一页回到不带 History 的首页）
            var cursorAdvanced = !string.IsNullOrEmpty(nextOffset) && !string.Equals(nextOffset, requestCursor, StringComparison.Ordinal);
            CanLoadMore = CanContinuePaging(hasMore, cursorAdvanced, mapped.Count, itemType, generation);
            lastPageFailed = false;
            ShowError = false;
            ErrorMsg = "";
            var firstId = grpcCards.FirstOrDefault()?.id_str ?? "";
            LogHelper.Log("User dynamic VM stage=feed-items-published source=grpc page=" + feedPage + " count=" + mapped.Count
                + " hasMore=" + hasMore + " firstDynamicId=" + firstId + " generation=" + generation, LogType.INFO);
            return DynamicApiSourceResult.Published();
        }

        /// <summary>
        /// 全部/视频动态流的 REST 一路。分页游标是 data.offset，只在这一路内部流转。
        /// </summary>
        private async Task<DynamicApiSourceResult> TryUserFeedRestAsync(int generation, DynamicType dynamicType, UserDynamicType itemType,
            bool loadMore, DynamicApiContext context)
        {
            string failureMessage = null;
            var operation = FeedOperation(itemType);
            var attempt = DynamicApiSelection.CreateAttempt(context, operation, DynamicApiSource.Legacy);
            var stopwatch = Stopwatch.StartNew();
            var restLoadMore = loadMore && !string.IsNullOrEmpty(restOffset);
            var restPage = restLoadMore ? feedPage + 1 : 1;
            var restCursor = restLoadMore ? restOffset : null;
            const string stage = "feed-rest-request";
            HttpResults restResponse;
            try
            {
                LogHelper.Log("User dynamic VM stage=" + stage + " page=" + restPage + " offset=" + (restCursor ?? "")
                    + " generation=" + generation, LogType.DEBUG);
                var restApi = dynamicAPI.DyanmicNew(itemType, restCursor);
                restResponse = await restApi.Request();
            }
            catch (Exception ex)
            {
                var failed = DynamicApiSelection.ClassifyRequestFailure(ex);
                DynamicApiSelection.Record(attempt, failed, stopwatch.Elapsed, stage);
                if (failed == DynamicApiOutcome.IgnoredFailure)
                {
                    throw;
                }
                failureMessage = ex.Message;
                return DynamicApiSourceResult.Failed(failed, failureMessage);
            }
            if (!IsCurrentRequest(generation, dynamicType, itemType))
            {
                DynamicApiSelection.Record(attempt, DynamicApiOutcome.IgnoredFailure, stopwatch.Elapsed, stage + " superseded");
                return DynamicApiSourceResult.Superseded();
            }
            if (!restResponse.status)
            {
                //HTTP 状态在 code 里：4xx/5xx 与传输失败分开归类
                var failed = DynamicApiSelection.ClassifyLegacy(restResponse, null);
                DynamicApiSelection.Record(attempt, failed, stopwatch.Elapsed, stage);
                failureMessage = restResponse.message;
                return DynamicApiSourceResult.Failed(failed, failureMessage);
            }
            var restData = restResponse.GetJObject();
            var businessCode = restData == null || restData["code"] == null ? (int?)null : restData["code"].ToInt32();
            var envelope = DynamicApiSelection.ClassifyLegacy(restResponse, businessCode);
            if (envelope != DynamicApiOutcome.Success || restData["data"] == null)
            {
                var failed = envelope == DynamicApiOutcome.Success ? DynamicApiOutcome.MalformedResponse : envelope;
                DynamicApiSelection.Record(attempt, failed, stopwatch.Elapsed, stage, businessCode ?? 0);
                failureMessage = restData?["message"]?.ToString() ?? restResponse.message;
                return DynamicApiSourceResult.Failed(failed, failureMessage);
            }
            DynamicApiSelection.Record(attempt, DynamicApiOutcome.Success, stopwatch.Elapsed, stage);

            var restCards = JsonConvert.DeserializeObject<List<DynamicCardModel>>(restData["data"]["cards"]?.ToString() ?? "[]", jsonSerializerSettings)
                ?? new List<DynamicCardModel>();
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
            if (!IsCurrentRequest(generation, dynamicType, itemType))
            {
                return DynamicApiSourceResult.Superseded();
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
            CanLoadMore = CanContinuePaging(restHasMore, restCursorAdvanced, restMapped.Count, itemType, generation);
            lastPageFailed = false;
            ShowError = false;
            ErrorMsg = "";
            LogHelper.Log("User dynamic VM stage=feed-items-published source=rest page=" + feedPage + " count=" + restMapped.Count
                + " hasMore=" + restHasMore + " offset=" + (nextRestOffset ?? "") + " generation=" + generation, LogType.INFO);
            if (!CanLoadMore && (Items == null || Items.Count == 0))
            {
                //分页已经停下，而且一张都没有：把换源时那条拒绝说明摆出来，不让页面无声空白
                PublishFeedError(feedFallbackReason ?? "动态请求失败");
            }
            return DynamicApiSourceResult.Published();
        }

        //个人空间动态：来源在每一屏边界由选择器决定，翻页沿用；换源从第一页重新开始，失败保留现有行
        private async Task ServeSpaceFeedAsync(int generation, DynamicType dynamicType, UserDynamicType itemType, string idx, bool loadMore, bool spaceStateReset)
        {
            var context = DynamicApiSelection.CaptureContext();
            var spaceLoadMore = loadMore && !spaceStateReset;
            var boundary = !spaceLoadMore || !pinnedSpaceSource.HasValue;
            var decision = boundary
                ? DynamicApiSelection.Choose(context, DynamicApiOperation.SpaceFeed, true, "space-feed uid=" + (Uid ?? ""))
                : null;
            var source = boundary ? decision.Source : pinnedSpaceSource.Value;
            var result = source == DynamicApiSource.Grpc
                ? await TrySpaceGrpcAsync(generation, dynamicType, itemType, spaceLoadMore, context)
                : await TrySpaceRestAsync(generation, dynamicType, itemType, spaceLoadMore, spaceLoadMore ? idx : "", context);
            if (result.Retired)
            {
                return;
            }
            var outcome = result.Outcome;
            var failureMessage = result.FailureMessage;
            if (outcome == DynamicApiOutcome.Success)
            {
                pinnedSpaceSource = source;
                return;
            }
            var alternative = DynamicApiSelection.Opposite(source);
            DynamicApiSelection.LogFallback(DynamicApiOperation.SpaceFeed, source, alternative, outcome, "space-feed");
            //换源不传递游标：gRPC 从第一页、SpaceHistory 从空 idx 重新开始，成功的一页顶替已发布的行
            var fallback = alternative == DynamicApiSource.Grpc
                ? await TrySpaceGrpcAsync(generation, dynamicType, itemType, false, context)
                : await TrySpaceRestAsync(generation, dynamicType, itemType, false, "", context);
            if (fallback.Retired)
            {
                return;
            }
            var fallbackOutcome = fallback.Outcome;
            failureMessage = fallback.FailureMessage;
            if (fallbackOutcome == DynamicApiOutcome.Success)
            {
                pinnedSpaceSource = alternative;
                return;
            }
            DynamicApiSelection.LogExhausted(DynamicApiOperation.SpaceFeed, outcome, fallbackOutcome, "space-feed");
            if (!IsCurrentRequest(generation, dynamicType, itemType))
            {
                return;
            }
            LogHelper.Log("User dynamic VM stage=space-feed-failed first=" + outcome + " second=" + fallbackOutcome
                + " message=" + (failureMessage ?? ""), LogType.ERROR);
            PublishFeedError(DescribeFeedFailure("动态请求失败", failureMessage));
        }

        private async Task<DynamicApiSourceResult> TrySpaceGrpcAsync(int generation, DynamicType dynamicType, UserDynamicType itemType,
            bool spaceLoadMore, DynamicApiContext context)
        {
            string failureMessage = null;
            var attempt = DynamicApiSelection.CreateAttempt(context, DynamicApiOperation.SpaceFeed, DynamicApiSource.Grpc);
            var stopwatch = Stopwatch.StartNew();
            if (!spaceLoadMore)
            {
                //换源/换用户：DynSpace 的页序号与 history_offset 从第一页重新开始
                spacePage = 1;
                spaceOffset = null;
            }
            const string stage = "space-grpc-request";
            List<DynamicCardModel2024> spaceCards;
            bool hasMore;
            string nextOffset;
            try
            {
                var reply = await grpcService.GetDynSpace(long.Parse(Uid), page: spacePage, offset: spaceLoadMore ? spaceOffset : null);
                if (!IsCurrentRequest(generation, dynamicType, itemType))
                {
                    DynamicApiSelection.Record(attempt, DynamicApiOutcome.IgnoredFailure, stopwatch.Elapsed, stage + " superseded");
                    return DynamicApiSourceResult.Superseded();
                }
                if (reply == null)
                {
                    DynamicApiSelection.Record(attempt, DynamicApiOutcome.MalformedResponse, stopwatch.Elapsed, stage);
                    failureMessage = "DynSpace returned no reply";
                    return DynamicApiSourceResult.Failed(DynamicApiOutcome.MalformedResponse, failureMessage);
                }
                spaceCards = DynamicGrpcMapper.MapToUserCards(reply.List);
                hasMore = reply.HasMore;
                nextOffset = reply.HistoryOffset;
            }
            catch (Exception ex)
            {
                var failed = DynamicApiSelection.ClassifyRequestFailure(ex);
                DynamicApiSelection.Record(attempt, failed, stopwatch.Elapsed, stage);
                if (failed == DynamicApiOutcome.IgnoredFailure)
                {
                    throw;
                }
                //SpaceHistory 的 idx 与 DynSpace 的 history_offset 分属两套，换源前清空
                spacePage = 1;
                spaceOffset = null;
                failureMessage = ex.Message;
                return DynamicApiSourceResult.Failed(failed, failureMessage);
            }
            DynamicApiSelection.Record(attempt, DynamicApiOutcome.Success, stopwatch.Elapsed, stage);

            var spaceMapped = new ObservableCollection<DynamicItemDisplayModel>();
            foreach (var card in spaceCards)
            {
                spaceMapped.Add(ConvertToDisplay2024(card));
            }
            if (!IsCurrentRequest(generation, dynamicType, itemType))
            {
                return DynamicApiSourceResult.Superseded();
            }
            if (spaceLoadMore && spacePage > 1 && Items != null)
            {
                foreach (var item in spaceMapped)
                {
                    Items.Add(item);
                }
            }
            else
            {
                Items = spaceMapped;
            }
            spaceOffset = nextOffset;
            spacePage++;
            //停止条件只认服务端的 has_more：本页即使被过滤成 0 张卡片也不代表没有下一页
            CanLoadMore = hasMore;
            lastPageFailed = false;
            ShowError = false;
            ErrorMsg = "";
            LogHelper.Log("User dynamic VM stage=space-items-published source=grpc page=" + (spacePage - 1) + " count="
                + spaceMapped.Count + " hasMore=" + hasMore + " generation=" + generation, LogType.INFO);
            return DynamicApiSourceResult.Published();
        }

        private async Task<DynamicApiSourceResult> TrySpaceRestAsync(int generation, DynamicType dynamicType, UserDynamicType itemType,
            bool spaceLoadMore, string cursor, DynamicApiContext context)
        {
            string failureMessage = null;
            var attempt = DynamicApiSelection.CreateAttempt(context, DynamicApiOperation.SpaceFeed, DynamicApiSource.Legacy);
            var stopwatch = Stopwatch.StartNew();
            const string stage = "space-rest-request";
            HttpResults results;
            try
            {
                results = await dynamicAPI.SpaceHistory(Uid, cursor).Request();
            }
            catch (Exception ex)
            {
                var failed = DynamicApiSelection.ClassifyRequestFailure(ex);
                DynamicApiSelection.Record(attempt, failed, stopwatch.Elapsed, stage);
                if (failed == DynamicApiOutcome.IgnoredFailure)
                {
                    throw;
                }
                failureMessage = ex.Message;
                return DynamicApiSourceResult.Failed(failed, failureMessage);
            }
            if (!IsCurrentRequest(generation, dynamicType, itemType))
            {
                DynamicApiSelection.Record(attempt, DynamicApiOutcome.IgnoredFailure, stopwatch.Elapsed, stage + " superseded");
                return DynamicApiSourceResult.Superseded();
            }
            if (!results.status)
            {
                var failed = DynamicApiSelection.ClassifyLegacy(results, null);
                DynamicApiSelection.Record(attempt, failed, stopwatch.Elapsed, stage);
                failureMessage = results.message;
                return DynamicApiSourceResult.Failed(failed, failureMessage);
            }
            var data = results.GetJObject();
            var businessCode = data == null || data["code"] == null ? (int?)null : data["code"].ToInt32();
            var envelope = DynamicApiSelection.ClassifyLegacy(results, businessCode);
            if (envelope != DynamicApiOutcome.Success || data["data"] == null)
            {
                var failed = envelope == DynamicApiOutcome.Success ? DynamicApiOutcome.MalformedResponse : envelope;
                DynamicApiSelection.Record(attempt, failed, stopwatch.Elapsed, stage, businessCode ?? 0);
                failureMessage = data?["message"]?.ToString() ?? results.message;
                return DynamicApiSourceResult.Failed(failed, failureMessage);
            }
            DynamicApiSelection.Record(attempt, DynamicApiOutcome.Success, stopwatch.Elapsed, stage);

            var items = JsonConvert.DeserializeObject<List<DynamicCardModel>>(data["data"]?["cards"]?.ToString() ?? "[]", jsonSerializerSettings)
                ?? new List<DynamicCardModel>();
            var mapped = new ObservableCollection<DynamicItemDisplayModel>();
            foreach (var item in items)
            {
                mapped.Add(ConvertToDisplay(item));
            }
            if (!IsCurrentRequest(generation, dynamicType, itemType))
            {
                return DynamicApiSourceResult.Superseded();
            }
            if (spaceLoadMore && Items != null)
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
            //SpaceHistory 的下一页游标是上一页最后一张卡的动态 ID，本页没有卡片就没有下一页
            CanLoadMore = mapped.Count > 0;
            lastPageFailed = false;
            ShowError = false;
            ErrorMsg = "";
            LogHelper.Log("User dynamic VM stage=space-items-published source=rest count=" + mapped.Count
                + " cursor=" + (cursor ?? "") + " generation=" + generation, LogType.INFO);
            return DynamicApiSourceResult.Published();
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
                //详情是独立的一屏：来源与上下文都在第一个 await 之前定下来
                var context = DynamicApiSelection.CaptureContext();
                var decision = DynamicApiSelection.Choose(context, DynamicApiOperation.DynamicDetail, true, "detail id=" + id);
                var result = decision.Source == DynamicApiSource.Grpc
                    ? await TryDetailGrpcAsync(id, context)
                    : await TryDetailRestAsync(id, context);
                var outcome = result.Outcome;
                var failureMessage = result.FailureMessage;
                if (outcome == DynamicApiOutcome.Success)
                {
                    return;
                }
                //一个逻辑请求最多两路：首选失败（含映射不出可用卡片）只再试另一路一次
                var alternative = DynamicApiSelection.Opposite(decision.Source);
                DynamicApiSelection.LogFallback(DynamicApiOperation.DynamicDetail, decision.Source, alternative, outcome, "detail id=" + id);
                var fallback = alternative == DynamicApiSource.Grpc
                    ? await TryDetailGrpcAsync(id, context)
                    : await TryDetailRestAsync(id, context);
                var fallbackOutcome = fallback.Outcome;
                failureMessage = fallback.FailureMessage;
                if (fallbackOutcome != DynamicApiOutcome.Success)
                {
                    DynamicApiSelection.LogExhausted(DynamicApiOperation.DynamicDetail, outcome, fallbackOutcome, "detail id=" + id);
                    Utils.ShowMessageToast(failureMessage ?? "动态加载失败");
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

        /// <summary>
        /// gRPC 详情。映射不出唯一一张可用卡片属于内容/本地转换问题，不降低端点可用性，
        /// 但本次仍然交给 REST 详情报一次错。
        /// </summary>
        private async Task<DynamicApiSourceResult> TryDetailGrpcAsync(string id, DynamicApiContext context)
        {
            string failureMessage = null;
            var attempt = DynamicApiSelection.CreateAttempt(context, DynamicApiOperation.DynamicDetail, DynamicApiSource.Grpc);
            var stopwatch = Stopwatch.StartNew();
            const string stage = "detail-grpc-request";
            ObservableCollection<DynamicItemDisplayModel> mapped = null;
            string failureDetail = null;
            DynamicApiOutcome outcome;
            try
            {
                var detail = await grpcService.GetDynDetail(id);
                var detailCards = DynamicGrpcMapper.MapToUserCards(new[] { detail?.Item });
                if (detailCards.Count == 1)
                {
                    var display = ConvertToDisplay2024(detailCards[0]);
                    //未支持的类型和转换失败都会退化成 Other 占位卡片，这类结果要留给 REST 详情
                    if (display.Type != DynamicDisplayType.Other)
                    {
                        mapped = new ObservableCollection<DynamicItemDisplayModel> { display };
                    }
                    else
                    {
                        failureDetail = "unsupported type=" + detailCards[0].type;
                    }
                }
                else
                {
                    failureDetail = "mapping produced " + detailCards.Count + " cards";
                }
                outcome = mapped != null ? DynamicApiOutcome.Success : DynamicApiOutcome.IgnoredFailure;
            }
            catch (Exception ex)
            {
                //映射/转换失败与请求失败都落到 REST 详情，与原先的兜底一致：本地转换问题不降低端点可用性
                outcome = DynamicApiSelection.ClassifyRequestFailure(ex);
                failureDetail = ex.Message;
            }
            DynamicApiSelection.Record(attempt, outcome, stopwatch.Elapsed, stage);
            if (outcome != DynamicApiOutcome.Success)
            {
                LogHelper.Log("User dynamic VM stage=detail-grpc-unusable id=" + id + " outcome=" + outcome
                    + " detail=" + (failureDetail ?? ""), LogType.INFO);
                failureMessage = failureDetail;
                return DynamicApiSourceResult.Failed(outcome, failureMessage);
            }
            Items = mapped;
            return DynamicApiSourceResult.Published();
        }

        private async Task<DynamicApiSourceResult> TryDetailRestAsync(string id, DynamicApiContext context)
        {
            string failureMessage = null;
            var attempt = DynamicApiSelection.CreateAttempt(context, DynamicApiOperation.DynamicDetail, DynamicApiSource.Legacy);
            var stopwatch = Stopwatch.StartNew();
            const string stage = "detail-rest-request";
            HttpResults results;
            try
            {
                results = await dynamicAPI.DynamicDetail(id).Request();
            }
            catch (Exception ex)
            {
                var failed = DynamicApiSelection.ClassifyRequestFailure(ex);
                DynamicApiSelection.Record(attempt, failed, stopwatch.Elapsed, stage);
                failureMessage = ex.Message;
                return DynamicApiSourceResult.Failed(failed, failureMessage);
            }
            if (!results.status)
            {
                var failed = DynamicApiSelection.ClassifyLegacy(results, null);
                DynamicApiSelection.Record(attempt, failed, stopwatch.Elapsed, stage);
                failureMessage = results.message;
                return DynamicApiSourceResult.Failed(failed, failureMessage);
            }
            var data = results.GetJObject();
            var businessCode = data == null || data["code"] == null ? (int?)null : data["code"].ToInt32();
            var envelope = DynamicApiSelection.ClassifyLegacy(results, businessCode);
            if (envelope != DynamicApiOutcome.Success || data["data"] == null)
            {
                var failed = envelope == DynamicApiOutcome.Success ? DynamicApiOutcome.MalformedResponse : envelope;
                DynamicApiSelection.Record(attempt, failed, stopwatch.Elapsed, stage, businessCode ?? 0);
                failureMessage = data?["message"]?.ToString() ?? results.message;
                return DynamicApiSourceResult.Failed(failed, failureMessage);
            }
            DynamicApiSelection.Record(attempt, DynamicApiOutcome.Success, stopwatch.Elapsed, stage);
            var item = JsonConvert.DeserializeObject<DynamicCardModel>(data["data"]?.ToString() ?? "{}", jsonSerializerSettings);
            Items = new ObservableCollection<DynamicItemDisplayModel> { ConvertToDisplay(item) };
            LogHelper.Log("User dynamic VM stage=detail-items-published source=rest id=" + id, LogType.INFO);
            return DynamicApiSourceResult.Published();
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
