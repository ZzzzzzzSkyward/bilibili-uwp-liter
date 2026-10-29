using BiliLite.Controls.Dynamic;
using BiliLite.Helpers;
using BiliLite.Models.Dynamic;
using BiliLite.Pages;
using BiliLite.Services;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
        // 本列表一旦回退到 REST 就固定下来，直到 Refresh/Clear，避免每页都先失败一次。
        bool useRestFallback = false;

        public async Task GetDynamicItems()
        {
            try
            {
                CanLoadMore = false;
                Loading = true;
                if (!useRestFallback && await TryLoadGrpcPage())
                {
                    return;
                }
                await LoadRestPage();
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
        /// 返回 false 表示本页没有可用的 gRPC 数据，调用方应改走 REST。
        /// </summary>
        private async Task<bool> TryLoadGrpcPage()
        {
            try
            {
                var rsp = await grpcService.GetDynRepostList(ID, grpcOffset);
                var displays = DynamicRepostMapper.ToDisplayList(
                    DynamicGrpcMapper.MapToUserCards(rsp?.List),
                    UserCommand);
                if (displays.Count > 0)
                {
                    Append(displays);
                    grpcOffset = rsp?.Offset ?? "";
                    CanLoadMore = rsp?.HasMore ?? false;
                    return true;
                }
                LogHelper.Log("Dynamic repost VM stage=repost-grpc-unusable dynamicId=" + ID + " offset=" + grpcOffset, LogType.INFO);
            }
            catch (Exception ex)
            {
                LogHelper.Log("Dynamic repost VM stage=repost-grpc-failed dynamicId=" + ID + " offset=" + grpcOffset + " message=" + ex.Message, LogType.INFO);
            }

            useRestFallback = true;
            return false;
        }

        private async Task LoadRestPage()
        {
            var api = dynamicAPI.DynamicRepost(ID, restOffset);

            var results = await api.Request();
            if (results.status)
            {
                var data = results.GetJObject();
                if (data["code"].ToInt32() == 0)
                {
                    var items = JsonConvert.DeserializeObject<List<DynamicCardModel>>(data["data"]?["items"]?.ToString() ?? "[]");

                    var displays = new List<DynamicItemDisplayModel>();
                    foreach (var item in items)
                    {
                        displays.Add(ConvertToDisplayRepost(item));
                    }
                    Append(displays);
                    // 上一版把 has_more 当成 offset 回传，第二页会重复第一页；这里推进真正的 offset，
                    // 并要求 has_more 与 offset 同时有效，否则分页会在第一页打转。
                    var hasMore = (data["data"]?["has_more"]?.ToInt32() ?? 0) == 1;
                    restOffset = data["data"]?["offset"]?.ToString() ?? "";
                    CanLoadMore = hasMore && !string.IsNullOrEmpty(restOffset);
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
            useRestFallback = false;
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
            useRestFallback = false;
            Items = null;

        }
    }
}
