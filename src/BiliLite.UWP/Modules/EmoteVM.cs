using BiliLite.Api;
using BiliLite.Helpers;
using BiliLite.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BiliLite.Modules
{
    public class EmoteVM:IModules
    {
        readonly EmoteApi emoteApi;
        public EmoteVM()
        {
            emoteApi = new EmoteApi();
        }

        private List<EmotePackageModel> _packages;

        public List<EmotePackageModel> Packages
        {
            get { return _packages; }
            set { _packages = value; DoPropertyChanged("Packages"); }
        }

        private bool _loading = true;
        public bool Loading
        {
            get { return _loading; }
            set { _loading = value; DoPropertyChanged("Loading"); }
        }

        public async Task GetEmote(EmoteBusiness business)
        {
            try
            {
                Loading = true;
                var api = emoteApi.UserEmote(business);

                var results = await api.Request();
                if (results.status)
                {
                    var data = await results.GetJson<ApiDataModel<JObject>>();
                    if (data.success)
                    {
                        //packages 为 null 时原来直接把 null 赋给 Packages，选择器空白且没有任何痕迹
                        var packages = data.data?["packages"] as JArray;
                        if (packages == null || packages.Count == 0)
                        {
                            Packages = new List<EmotePackageModel>();
                            LogHelper.Log("表情包接口没有返回 packages，选择器按无表情处理", LogType.INFO);
                        }
                        else
                        {
                            Packages = JsonConvert.DeserializeObject<List<EmotePackageModel>>(packages.ToString());
                            foreach (var item in Packages)
                            {
                                if (item.emote == null) continue;
                                foreach (var emote in item.emote)
                                {
                                    emote.url = CommentRichText.NormalizeImageUrl(emote.url);
                                }
                            }
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
    }


    public class EmotePackageModel
    {
        public long id { get; set; }
        public string text { get; set; }
        public string url { get; set; }
        public int type { get; set; }
      
        public int attr { get; set; }
        public List<EmotePackageItemModel> emote { get; set; }
    }
    public class EmotePackageItemModel
    {
        public long id { get; set; }
        public string text { get; set; }
        public string url { get; set; }
        public int package_id { get; set; }
        public int type { get; set; }
        public bool showImage { get { return type != 4; } }
    }
}
