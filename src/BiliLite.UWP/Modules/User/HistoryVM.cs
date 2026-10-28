using BiliLite.Helpers;
using BiliLite.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;

namespace BiliLite.Modules.User
{
    public class HistoryVM : IModules
    {
        Api.AccountApi accountApi;
        private HistoryCursorModel cursor;
        public HistoryVM()
        {
            accountApi = new Api.AccountApi();
            RefreshCommand = new RelayCommand(Refresh);
            LoadMoreCommand = new RelayCommand(LoadMore);
        }
        public int Page { get; set; } = 1;
        private bool _loading = false;
       
        public bool Loading
        {
            get { return _loading; }
            set { _loading = value; DoPropertyChanged("Loading"); }
        }
        public ICommand LoadMoreCommand { get; private set; }
        public ICommand RefreshCommand { get; private set; }
        private bool _Nothing = false;
        public bool Nothing
        {
            get { return _Nothing; }
            set { _Nothing = value; DoPropertyChanged("Nothing"); }
        }

        private bool _ShowLoadMore = false;
        public bool ShowLoadMore
        {
            get { return _ShowLoadMore; }
            set { _ShowLoadMore = value; DoPropertyChanged("ShowLoadMore"); }
        }
        private ObservableCollection<HistoryItemModel> _videos;
        public ObservableCollection<HistoryItemModel> Videos
        {
            get { return _videos; }
            set { _videos = value; DoPropertyChanged("Videos"); }
        }

        public async Task LoadHistory()
        {
            try
            {
                ShowLoadMore = false;
                Loading = true;
                Nothing = false;
                var results = await accountApi.HistoryWbi(cursor?.Max ?? 0, cursor?.ViewAt ?? 0).Request();
                if (results.status)
                {
                    var data = await results.GetJson<ApiDataModel<HistoryPageResponse>>();
                    if (data.success)
                    {
                        var records = data.data?.list ?? new List<HistoryRecord>();
                        LogHelper.Log("History stage=cursor-loaded page=" + Page + " count=" + records.Count +
                            " nextMax=" + (data.data?.cursor?.Max ?? 0) + " nextViewAt=" + (data.data?.cursor?.ViewAt ?? 0), LogType.INFO);
                        if (Page == 1)
                        {
                            if (records.Count == 0)
                            {
                                Nothing = true;
                                return;
                            }
                            Videos = new ObservableCollection<HistoryItemModel>();
                        }

                        foreach (var record in records)
                        {
                            var history = record.history;
                            var cover = record.cover;
                            if (string.IsNullOrEmpty(cover) && record.covers != null && record.covers.Count > 0)
                            {
                                cover = record.covers[0]?.ToString();
                            }
                            Videos.Add(new HistoryItemModel
                            {
                                aid = !string.IsNullOrEmpty(history?.bvid) ? history.bvid : history?.oid.ToString(),
                                cover = cover,
                                pic = cover,
                                owner = new HistoryItemOwnerModel
                                {
                                    mid = record.author_mid,
                                    name = record.author_name,
                                    face = record.author_face
                                },
                                name = record.title,
                                title = record.title,
                                view_at = record.view_at,
                                kid = record.kid,
                                business = history?.business,
                                historyOid = history?.oid ?? 0,
                                historyEpid = history?.epid ?? 0,
                                historyCid = history?.cid ?? 0
                            });
                        }

                        if (records.Count != 0)
                        {
                            cursor = data.data?.cursor;
                            ShowLoadMore = cursor != null;
                            Page++;
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
                var handel = HandelError<AnimeHomeModel>(ex);
                Utils.ShowMessageToast(handel.message);
            }
            finally
            {
                Loading = false;
            }
        }
        public async void Refresh()
        {
            if (Loading)
            {
                return;
            }
            Page = 1;
            cursor = null;
            Videos = null;
            await LoadHistory();
        }
        public async void LoadMore()
        {
            if (Loading)
            {
                return;
            }
            if (Videos == null || Videos.Count == 0)
            {
                return;
            }
            await LoadHistory();
        }
        public async void Del(HistoryItemModel item)
        {
            try
            {

                var results = await accountApi.DelHistory(item.business+"_" +item.kid).Request();
                if (results.status)
                {
                    var data = await results.GetJson<ApiDataModel<JObject>>();
                    if (data.success)
                    {
                        Videos.Remove(item);
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
                var handel = HandelError<AnimeHomeModel>(ex);
                Utils.ShowMessageToast(handel.message);
            }
        }
    }
    public class HistoryItemModel
    {
        public string aid { get; set; }
        public string cover { get; set; }
        public HistoryItemOwnerModel owner { get; set; }
        public string name { get; set; }
        public string pic { get; set; }
        public string title { get; set; }
        public long view_at { get; set; }
        public string tname { get; set; }
        public string kid { get; set; }
        public string business { get; set; }
        public long historyOid { get; set; }
        public long historyEpid { get; set; }
        public long historyCid { get; set; }
    }

    public class HistoryItemOwnerModel
    {
        public long mid { get; set; }
        public string name { get; set; }
        public string face { get; set; }
    }

    public class HistoryPageResponse
    {
        [JsonProperty("cursor")]
        public HistoryCursorModel cursor { get; set; }

        [JsonProperty("list")]
        public List<HistoryRecord> list { get; set; }
    }

    public class HistoryCursorModel
    {
        [JsonProperty("max")]
        public long Max { get; set; }

        [JsonProperty("view_at")]
        public long ViewAt { get; set; }
    }

    public class HistoryRecord
    {
        [JsonProperty("title")]
        public string title { get; set; }

        [JsonProperty("cover")]
        public string cover { get; set; }

        [JsonProperty("covers")]
        public JArray covers { get; set; }

        [JsonProperty("author_name")]
        public string author_name { get; set; }

        [JsonProperty("author_face")]
        public string author_face { get; set; }

        [JsonProperty("author_mid")]
        public long author_mid { get; set; }

        [JsonProperty("view_at")]
        public long view_at { get; set; }

        [JsonProperty("kid")]
        public string kid { get; set; }

        [JsonProperty("history")]
        public HistoryRecordDetail history { get; set; }
    }

    public class HistoryRecordDetail
    {
        [JsonProperty("oid")]
        public long oid { get; set; }

        [JsonProperty("bvid")]
        public string bvid { get; set; }

        [JsonProperty("epid")]
        public long epid { get; set; }

        [JsonProperty("cid")]
        public long cid { get; set; }

        [JsonProperty("business")]
        public string business { get; set; }
    }
}
