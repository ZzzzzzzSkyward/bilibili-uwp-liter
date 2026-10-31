using BiliLite.Controls.Dynamic;
using BiliLite.Helpers;
using BiliLite.Models.Dynamic;
using BiliLite.Pages;
using BiliLite.Parsing.Dynamic;
using BiliLite.Services;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.UI.Xaml.Controls;

namespace BiliLite.Modules.User
{
    public class DynamicRepostVM:IModules
    {
        readonly Api.User.DynamicAPI dynamicAPI;
        readonly GrpcService grpcService;
        public DynamicRepostVM()
        {
            dynamicAPI = new Api.User.DynamicAPI();
            grpcService = new GrpcService();
            RefreshCommand = new RelayCommand(Refresh);
            LoadMoreCommand = new RelayCommand(LoadMore);
            UserCommand = new RelayCommand<object>(OpenUser);
        }
        public string ID { get; set; }
        public ICommand RefreshCommand { get; private set; }
        public ICommand LoadMoreCommand { get; private set; }
        public ICommand UserCommand { get; set; }
        private bool _loading = false;
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
        private ObservableCollection<DynamicItemDisplayModel> _Items;
        public ObservableCollection<DynamicItemDisplayModel> Items
        {
            get { return _Items; }
            set { _Items = value; DoPropertyChanged("Items"); }
        }
        // gRPC 与 REST 的分页游标互不相干：一个回包里的 offset 不能拿去请求另一个接口。
        string grpcOffset = "";
        string restOffset = "";
        // 本次转发列表固定的来源：刷新/清空时由选择器重新决定，翻页沿用；
        // 只有同一次请求里首选失败、备用来源成功时才改到这一路
        DynamicApiSource? pinnedSource;
        // 每次刷新/清空都递增，只有持有一致代号的请求才能发布结果
        int requestGeneration;

        public async Task GetDynamicItems()
        {
            var generation = ++requestGeneration;
            try
            {
                CanLoadMore = false;
                Loading = true;
                // 上下文与来源都在第一个 await 之前定下来
                var context = DynamicApiSelection.CaptureContext();
                var boundary = !pinnedSource.HasValue;
                var decision = boundary
                    ? DynamicApiSelection.Choose(context, DynamicApiOperation.Reposts, true, "reposts")
                    : null;
                var source = boundary ? decision.Source : pinnedSource.Value;
                var page = source == DynamicApiSource.Grpc
                    ? await TryLoadGrpcPage(grpcOffset, false, context, generation)
                    : await LoadRestPage(restOffset, false, context, generation);
                if (page.Served)
                {
                    pinnedSource = source;
                    return;
                }
                var outcome = page.Outcome;
                var failureMessage = page.FailureMessage;
                var firstOutcome = outcome;
                // 一个逻辑请求最多两路：首选失败只再试另一路一次
                var alternative = DynamicApiSelection.Opposite(source);
                DynamicApiSelection.LogFallback(DynamicApiOperation.Reposts, source, alternative, outcome, "reposts");
                // 换源不传游标：另一路从第一页重新取，第一条成功的页整体顶替已发布的行
                var fallback = alternative == DynamicApiSource.Grpc
                    ? await TryLoadGrpcPage("", true, context, generation)
                    : await LoadRestPage("", true, context, generation);
                if (fallback.Served)
                {
                    pinnedSource = alternative;
                    return;
                }
                outcome = fallback.Outcome;
                failureMessage = fallback.FailureMessage;
                if (generation != requestGeneration)
                {
                    return;
                }
                DynamicApiSelection.LogExhausted(DynamicApiOperation.Reposts, firstOutcome, outcome, "reposts");
                Utils.ShowMessageToast(failureMessage ?? "转发列表加载失败");
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
        /// gRPC 转发列表。served 为 false 表示本页没有可用的条目（接口拒绝或这一页是空的），
        /// 由调用方在本次请求内改试 REST；空页本身仍然算一次有效响应，不降低端点可用性。
        /// </summary>
        private async Task<DynamicApiSourceResult> TryLoadGrpcPage(string offset, bool replaceRows, DynamicApiContext context, int generation)
        {
            DynamicApiOutcome outcome;
            var attempt = DynamicApiSelection.CreateAttempt(context, DynamicApiOperation.Reposts, DynamicApiSource.Grpc);
            var stopwatch = Stopwatch.StartNew();
            const string stage = "reposts-grpc-request";
            Bilibili.App.Dynamic.V2.RepostListRsp rsp;
            try
            {
                rsp = await grpcService.GetDynRepostList(ID, offset);
            }
            catch (Exception ex)
            {
                outcome = DynamicApiSelection.ClassifyRequestFailure(ex);
                DynamicApiSelection.Record(attempt, outcome, stopwatch.Elapsed, stage);
                if (outcome == DynamicApiOutcome.IgnoredFailure)
                {
                    throw;
                }
                return DynamicApiSourceResult.Failed(outcome, ex.Message);
            }
            if (generation != requestGeneration)
            {
                DynamicApiSelection.Record(attempt, DynamicApiOutcome.IgnoredFailure, stopwatch.Elapsed, stage + " superseded");
                return DynamicApiSourceResult.Superseded();
            }
            var displays = DynamicRepostMapper.ToDisplayList(
                DynamicGrpcMapper.MapToUserCards(rsp?.List),
                UserCommand);
            outcome = DynamicApiOutcome.Success;
            DynamicApiSelection.Record(attempt, outcome, stopwatch.Elapsed, stage);
            if (displays.Count == 0)
            {
                LogHelper.Log("Dynamic repost VM stage=repost-grpc-unusable dynamicId=" + ID + " offset=" + offset, LogType.INFO);
                return DynamicApiSourceResult.Unpublished();
            }
            if (replaceRows && Items != null)
            {
                Items.Clear();
            }
            Append(displays);
            grpcOffset = rsp?.Offset ?? "";
            // 换到 gRPC 之后 REST 游标作废：两套分页不互传
            restOffset = "";
            CanLoadMore = rsp?.HasMore ?? false;
            return DynamicApiSourceResult.Published();
        }

        private async Task<DynamicApiSourceResult> LoadRestPage(string offset, bool replaceRows, DynamicApiContext context, int generation)
        {
            string failureMessage = null;
            DynamicApiOutcome outcome;
            var attempt = DynamicApiSelection.CreateAttempt(context, DynamicApiOperation.Reposts, DynamicApiSource.Legacy);
            var stopwatch = Stopwatch.StartNew();
            const string stage = "reposts-rest-request";
            HttpResults results;
            try
            {
                results = await dynamicAPI.DynamicRepost(ID, offset).Request();
            }
            catch (Exception ex)
            {
                outcome = DynamicApiSelection.ClassifyRequestFailure(ex);
                DynamicApiSelection.Record(attempt, outcome, stopwatch.Elapsed, stage);
                if (outcome == DynamicApiOutcome.IgnoredFailure)
                {
                    throw;
                }
                return DynamicApiSourceResult.Failed(outcome, ex.Message);
            }
            if (generation != requestGeneration)
            {
                DynamicApiSelection.Record(attempt, DynamicApiOutcome.IgnoredFailure, stopwatch.Elapsed, stage + " superseded");
                return DynamicApiSourceResult.Superseded();
            }
            var parsed = LegacyDynamicFeedParser.ParseReposts(results);
            if (!parsed.IsSuccess)
            {
                outcome = DynamicApiSelection.ToOutcome(parsed);
                DynamicApiSelection.Record(attempt, outcome, stopwatch.Elapsed, stage, parsed.BusinessCode);
                failureMessage = parsed.Message;
                return DynamicApiSourceResult.Failed(outcome, failureMessage);
            }
            outcome = DynamicApiOutcome.Success;
            var repostPage = parsed.Value;
            List<DynamicCardModel> items;
            if (!LegacyDynamicFeedParser.TryDeserialize(repostPage.Items, null, out items))
            {
                //一条尝试只记一条结论：先记成功再补失败会被忽略
                DynamicApiSelection.Record(attempt, DynamicApiOutcome.MalformedResponse, stopwatch.Elapsed, stage + " cards");
                return DynamicApiSourceResult.Failed(DynamicApiOutcome.MalformedResponse, "转发列表解析失败");
            }
            DynamicApiSelection.Record(attempt, DynamicApiOutcome.Success, stopwatch.Elapsed, stage, parsed.BusinessCode);
            var displays = new List<DynamicItemDisplayModel>();
            foreach (var item in items)
            {
                displays.Add(ConvertToDisplayRepost(item));
            }
            // 换源时已展示的条目让位给第一条成功的 REST 页，之后各页照旧追加，同一批转发不会出现两次
            if (replaceRows && Items != null)
            {
                Items.Clear();
            }
            Append(displays);
            // 上一版把 has_more 当成 offset 回传，第二页会重复第一页；这里推进真正的 offset，
            // 并要求 has_more 与 offset 同时有效，否则分页会在第一页打转
            var hasMore = repostPage.HasMore;
            restOffset = repostPage.NextCursor ?? "";
            // 换到 REST 之后 gRPC 游标作废
            grpcOffset = "";
            CanLoadMore = hasMore && !string.IsNullOrEmpty(restOffset);
            return DynamicApiSourceResult.Published();
        }

        private void Append(IEnumerable<DynamicItemDisplayModel> displays)
        {
            if (Items == null)
            {
                Items = new ObservableCollection<DynamicItemDisplayModel>(displays);
                return;
            }
            foreach (var item in displays)
            {
                Items.Add(item);
            }
        }

        public async void Refresh()
        {
            if (Loading)
            {
                return;
            }
            grpcOffset = "";
            restOffset = "";
            // 刷新就是新的一屏边界：交回选择器重新决定来源，代号递增让在途请求作废
            pinnedSource = null;
            requestGeneration++;
            Items = null;
            await GetDynamicItems();
        }
        public async void LoadMore()
        {
            if (Loading)
            {
                return;
            }
            if (Items == null || Items.Count == 0)
            {
                return;
            }
            var last = Items.LastOrDefault();
            await GetDynamicItems();
        }
        private DynamicItemDisplayModel ConvertToDisplayRepost(DynamicCardModel item)
        {
            var card = JObject.Parse(item.card);
            var data = new DynamicItemDisplayModel()
            {
                Datetime = Utils.TimestampToDatetime(item.desc.timestamp).ToString(),
                DynamicID = item.desc.dynamic_id,
                Mid = item.desc.uid,
                Time = Utils.HandelTimestamp(item.desc.timestamp.ToString()),
                UserCommand=UserCommand
            };
            var content = "";
            //内容
            if (card.ContainsKey("item") && card["item"]["content"] != null)
            {
                content = card["item"]["content"]?.ToString();
            }
            data.ContentStr = content;

            if (item.desc.user_profile != null)
            {
                data.UserName = item.desc.user_profile.info.uname;
                data.Photo = item.desc.user_profile.info.face;
                if (item.desc.user_profile.vip != null)
                {
                    data.IsYearVip = item.desc.user_profile.vip.vipStatus==1&&item.desc.user_profile.vip.vipType == 2;
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
            }
            return data;
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


        public void Clear()
        {
            grpcOffset = "";
            restOffset = "";
            pinnedSource = null;
            requestGeneration++;
            Items = null;

        }
    }
}
