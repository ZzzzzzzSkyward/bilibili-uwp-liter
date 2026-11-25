using BiliLite.Helpers;
using BiliLite.Models;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace BiliLite.Modules
{
    public class MyFollowVideoVM : IModules
    {
        readonly Api.User.FavoriteApi  favoriteAPI;
        public MyFollowVideoVM()
        {
            favoriteAPI = new Api.User.FavoriteApi();
            RefreshCommand = new RelayCommand(Refresh);
            LoadMoreCommand = new RelayCommand(LoadMore);
        }
        private bool _loading = false;
        public bool Loading
        {
            get { return _loading; }
            set { _loading = value; DoPropertyChanged("Loading"); }
        }
        public ICommand RefreshCommand { get; private set; }
        public ICommand LoadMoreCommand { get; private set; }
        private ObservableCollection<FavoriteItemModel> _myFavorite;
        public ObservableCollection<FavoriteItemModel> MyFavorite
        {
            get { return _myFavorite; }
            set
            {
                if (ReferenceEquals(_myFavorite, value)) return;
                _myFavorite = value;
                DoPropertyChanged("MyFavorite");
            }
        }

        private ObservableCollection<FavoriteItemModel> _collectFavorite;
        public ObservableCollection<FavoriteItemModel> CollectFavorite
        {
            get { return _collectFavorite; }
            set { _collectFavorite = value; DoPropertyChanged("CollectFavorite"); }
        }
        private bool _hasMore = false;
        public bool HasMore
        {
            get { return _hasMore; }
            set { _hasMore = value; DoPropertyChanged("HasMore"); }
        }
        private int Page = 1;
        public async Task LoadFavorite()
        {
            try
            {
                Loading = true;
                HasMore = false;
                Page = 1;
                var results = await favoriteAPI.MyFavorite().Request();
                if (results.status)
                {
                    var data = await results.GetJson<ApiDataModel<JObject>>();
                    if (data.success)
                    {
                        // 先完成当前分支的响应读取，避免后续字段失败时发布半页收藏夹。
                        ObservableCollection<FavoriteItemModel> created = null;
                        FavoriteItemModel defaultFolder = null;
                        var hasMore = false;
                        var loadCreated = data.data["space_infos"][0]["mediaListResponse"] != null;
                        if (loadCreated)
                        {
                            created = await data.data["space_infos"][0]["mediaListResponse"]["list"].ToString().DeserializeJson<ObservableCollection<FavoriteItemModel>>();
                            if (created == null)
                            {
                                created = new ObservableCollection<FavoriteItemModel>();
                            }
                            defaultFolder = await data.data["default_folder"]["folder_detail"].ToString().DeserializeJson<FavoriteItemModel>();
                            hasMore = (bool)data.data["space_infos"][0]["mediaListResponse"]["has_more"];
                        }
                        ObservableCollection<FavoriteItemModel> collected = null;
                        var loadCollected = data.data["space_infos"][1]["mediaListResponse"] != null;
                        if (loadCollected)
                        {
                            collected = await data.data["space_infos"][1]["mediaListResponse"]["list"].ToString().DeserializeJson<ObservableCollection<FavoriteItemModel>>();
                        }
                        if (loadCreated)
                        {
                            MyFavorite = created;
                            MyFavorite.Insert(0, defaultFolder);
                            HasMore = hasMore;
                            Page ++;
                        }
                        if (loadCollected)
                        {
                            CollectFavorite = collected;
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
        public async void LoadMore()
        {
           await LoadCreateList();
        }
        
        public async Task LoadCreateList()
        {
            try
            {
                HasMore = false;
                var results = await favoriteAPI.MyCreatedFavoriteList(Page).Request();
                if (results.status)
                {
                    var data = await results.GetJson<ApiDataModel<JObject>>();
                    if (data.success)
                    {
                        var ls  = await data.data["list"].ToString().DeserializeJson<List<FavoriteItemModel>>();
                        var hasMore = (bool)data.data["has_more"];
                        foreach (var item in ls)
                        {
                            MyFavorite.Add(item);
                        }
                        HasMore = hasMore;
                        Page++;
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


        public async Task<bool> DelFavorite(string id)
        {
            try
            {
                var results = await favoriteAPI.DelFavorite(id).Request();
                if (results.status)
                {
                    var data = await results.GetJson<ApiDataModel<object>>();
                    if (data.success)
                    {
                        return true;
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
            return false;
        }
        public async void Refresh()
        {
            if (Loading)
            {
                return;
            }
            MyFavorite = null;
            CollectFavorite = null;
            await LoadFavorite();
        }
        public async Task SortMyFavorite()
        {
            var favIds = MyFavorite.Select(x => x.id).ToList();
            if (favIds.Count < 2) { return; }
            var result = await favoriteAPI.Sort(favIds).Request();
            if (!result.status)
            {
                Utils.ShowMessageToast("排序失败" + result.message);
                return;
            }

            var data = await result.GetData<object>();
            if (data.success)
            {
                Utils.ShowMessageToast("排序成功");
                return;
            }
            Utils.ShowMessageToast("排序失败" + data.message);
        }
    }

    public class FavoriteItemModel : INotifyPropertyChanged
    {
        public string cover { get; set; }
        public int attr { get; set; }
        public bool privacy
        {
            get
            {
                //attr单数为私密，双数为公开
                return attr %2!=0;
            }
        }
        public string intro { get; set; }
        public string fid { get; set; }
        public string id { get; set; }
        public int like_state { get; set; }
       
        public string mid { get; set; }
        public string title { get; set; }
        public int type { get; set; }


        private int _media_count;
        public int media_count
        {
            get { return _media_count; }
            set { _media_count = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("media_count")); }
        }
        public int fav_state { get; set; }
        public bool is_fav
        {
            get
            {
                return fav_state == 1;
            }
            set
            {
                if (value) {
                    fav_state = 1;
                }
                else
                {
                    fav_state = 0;
                }
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("is_fav"));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("fav_state"));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }

}
