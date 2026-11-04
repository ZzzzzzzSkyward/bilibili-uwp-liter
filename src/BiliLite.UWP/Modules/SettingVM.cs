using BiliLite.Api;
using BiliLite.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Net.Http;

namespace BiliLite.Modules
{
    public class SettingVM : IModules
    {
        PlayerAPI playerAPI;
        public SettingVM()
        {
            playerAPI = new PlayerAPI();
            CDNServers = new List<CDNServerItem>()
            {
                new CDNServerItem("upos-sz-mirrorhwo1.bilivideo.com","华为云"),
                new CDNServerItem("upos-sz-mirrorcos.bilivideo.com","腾讯云"),
                new CDNServerItem("upos-sz-mirrorali.bilivideo.com","阿里云"),
                new CDNServerItem("upos-sz-mirrorhw.bilivideo.com","华为云"),
                new CDNServerItem("upos-sz-mirrorks3.bilivideo.com","金山云"),
                new CDNServerItem("upos-tf-all-js.bilivideo.com","JS"),
                new CDNServerItem("cn-hk-eq-bcache-01.bilivideo.com","香港"),
                new CDNServerItem("cn-hk-eq-bcache-16.bilivideo.com","香港"),
                new CDNServerItem("upos-hz-mirrorakam.akamaized.net","Akamaized"),
            };
            LoadShieldSetting();
        }

        /// <summary>
        /// 弹幕屏蔽关键词列表
        /// </summary>
        public ObservableCollection<string> ShieldWords { get; set; }
        public ObservableCollection<string> LiveWords { get; set; }
        public ObservableCollection<string> ShieldUsers { get; set; }
        public ObservableCollection<string> ShieldRegulars { get; set; }
        public List<CDNServerItem> CDNServers { get; set; }

        public void LoadShieldSetting()
        {
            LiveWords = SettingHelper.GetValue<ObservableCollection<string>>(SettingHelper.Live.SHIELD_WORD, new ObservableCollection<string>() { });
            ShieldWords = SettingHelper.GetValue<ObservableCollection<string>>(SettingHelper.VideoDanmaku.SHIELD_WORD, new ObservableCollection<string>() { });

            //正则关键词
            ShieldRegulars = SettingHelper.GetValue<ObservableCollection<string>>(SettingHelper.VideoDanmaku.SHIELD_REGULAR, new ObservableCollection<string>() { });
            var validRegulars = ShieldRegulars.Where(x => DanmuFilterRules.TryValidateRegex(x, out _)).ToList();
            if (validRegulars.Count != ShieldRegulars.Count)
            {
                ShieldRegulars = new ObservableCollection<string>(validRegulars);
                SettingHelper.SetValue(SettingHelper.VideoDanmaku.SHIELD_REGULAR, ShieldRegulars);
            }

            //用户
            ShieldUsers = SettingHelper.GetValue<ObservableCollection<string>>(SettingHelper.VideoDanmaku.SHIELD_USER, new ObservableCollection<string>() { });
        }

        /// <summary>
        /// 把本地的弹幕屏蔽规则导出为JSON，格式与bilibili.block.json一致：只有type和filter两个字段
        /// </summary>
        public string ExportDanmuFilterJson()
        {
            var items = new List<DanmuFilterExchangeItem>();
            AppendDanmuFilterItems(items, ShieldWords, 0);
            AppendDanmuFilterItems(items, ShieldRegulars, 1);
            AppendDanmuFilterItems(items, ShieldUsers, 2);
            return JsonConvert.SerializeObject(items, Formatting.Indented);
        }

        private static void AppendDanmuFilterItems(List<DanmuFilterExchangeItem> items, IEnumerable<string> filters, int type)
        {
            if (filters == null)
                return;
            foreach (var filter in filters)
            {
                if (string.IsNullOrWhiteSpace(filter))
                    continue;
                items.Add(new DanmuFilterExchangeItem() { type = type, filter = filter });
            }
        }

        /// <summary>
        /// 导入弹幕屏蔽规则，返回新增的条数。不访问网络，也不走云端同步。
        /// </summary>
        public int ImportDanmuFilterJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                LogHelper.Log("导入弹幕屏蔽词失败：内容为空", LogType.ERROR);
                return 0;
            }

            List<DanmuFilterExchangeItem> items;
            try
            {
                items = JsonConvert.DeserializeObject<List<DanmuFilterExchangeItem>>(json);
            }
            catch (Exception ex)
            {
                LogHelper.Log("导入弹幕屏蔽词失败：格式错误", LogType.ERROR, ex);
                return 0;
            }

            if (items == null)
            {
                LogHelper.Log("导入弹幕屏蔽词失败：格式错误", LogType.ERROR);
                return 0;
            }

            if (ShieldWords == null)
                ShieldWords = new ObservableCollection<string>();
            if (ShieldRegulars == null)
                ShieldRegulars = new ObservableCollection<string>();
            if (ShieldUsers == null)
                ShieldUsers = new ObservableCollection<string>();

            var added = 0;
            foreach (var item in items)
            {
                if (item == null || string.IsNullOrWhiteSpace(item.filter))
                    continue;
                switch (item.type)
                {
                    case 0:
                        added += DanmuFilterRules.AddUnique(ShieldWords, item.filter) ? 1 : 0;
                        break;
                    case 1:
                        if (!DanmuFilterRules.TryValidateRegex(item.filter, out _))
                            continue;
                        added += DanmuFilterRules.AddUnique(ShieldRegulars, item.filter) ? 1 : 0;
                        break;
                    case 2:
                        added += DanmuFilterRules.AddUnique(ShieldUsers, item.filter) ? 1 : 0;
                        break;
                }
            }

            SettingHelper.SetValue(SettingHelper.VideoDanmaku.SHIELD_WORD, ShieldWords);
            SettingHelper.SetValue(SettingHelper.VideoDanmaku.SHIELD_REGULAR, ShieldRegulars);
            SettingHelper.SetValue(SettingHelper.VideoDanmaku.SHIELD_USER, ShieldUsers);
            return added;
        }

        public async Task SyncDanmuFilter()
        {
            try
            {
                var result = await playerAPI.GetDanmuFilterWords().Request();
                if (!result.status)
                {
                    Utils.ShowMessageToast(result.message);
                    return;
                }
                var obj = result.GetJObject();
                if (obj["code"].ToInt32() != 0)
                {
                    Utils.ShowMessageToast(obj["message"].ToString());
                    return;
                }

                var items = JsonConvert.DeserializeObject<List<DanmuFilterItem>>(obj["data"]["rule"].ToString());
                if (items == null)
                    return;

                EnsureShieldLists();

                // 三类规则各自先形成独立快照，再改动可观察集合：服务器没有的本地规则不会被清空吃掉。
                var mergedWords = DanmuFilterRules.Merge(ShieldWords,
                    items.Where(x => x.type == 0).Select(x => x.filter));
                var mergedRegulars = DanmuFilterRules.Merge(ShieldRegulars,
                    items.Where(x => x.type == 1).Select(x => x.filter),
                    x => DanmuFilterRules.TryValidateRegex(x, out _));
                var mergedUsers = DanmuFilterRules.Merge(ShieldUsers,
                    items.Where(x => x.type == 2).Select(x => x.filter));

                ResetShieldList(ShieldWords, mergedWords);
                ResetShieldList(ShieldRegulars, mergedRegulars);
                ResetShieldList(ShieldUsers, mergedUsers);

                SettingHelper.SetValue(SettingHelper.VideoDanmaku.SHIELD_WORD, ShieldWords);
                SettingHelper.SetValue(SettingHelper.VideoDanmaku.SHIELD_REGULAR, ShieldRegulars);
                SettingHelper.SetValue(SettingHelper.VideoDanmaku.SHIELD_USER, ShieldUsers);
            }
            catch (Exception ex)
            {
                LogHelper.Log("读取弹幕屏蔽词失败", LogType.ERROR, ex);
            }
        }

        private void EnsureShieldLists()
        {
            if (ShieldWords == null)
                ShieldWords = new ObservableCollection<string>();
            if (ShieldRegulars == null)
                ShieldRegulars = new ObservableCollection<string>();
            if (ShieldUsers == null)
                ShieldUsers = new ObservableCollection<string>();
        }

        private static void ResetShieldList(ObservableCollection<string> target, List<string> snapshot)
        {
            target.Clear();
            foreach (var item in snapshot)
            {
                target.Add(item);
            }
        }

        /// <summary>
        /// 把规则加入本地列表并持久化，返回是否新增。空值、重复值和无效正则返回 false，不访问网络。
        /// </summary>
        public Task<bool> AddDanmuFilterItem(string word, int type)
        {
            EnsureShieldLists();
            ObservableCollection<string> target;
            string key;
            switch (type)
            {
                case 0:
                    target = ShieldWords;
                    key = SettingHelper.VideoDanmaku.SHIELD_WORD;
                    break;
                case 1:
                    target = ShieldRegulars;
                    key = SettingHelper.VideoDanmaku.SHIELD_REGULAR;
                    break;
                case 2:
                    target = ShieldUsers;
                    key = SettingHelper.VideoDanmaku.SHIELD_USER;
                    break;
                default:
                    return Task.FromResult(false);
            }

            if (type == 1 && !DanmuFilterRules.TryValidateRegex(word, out _))
                return Task.FromResult(false);
            if (!DanmuFilterRules.AddUnique(target, word))
                return Task.FromResult(false);

            SettingHelper.SetValue(key, target);
            return Task.FromResult(true);
        }
        /// <summary>
        /// CDN延迟测试
        /// </summary>
        public async void CDNServerDelayTest()
        {
            foreach (var item in CDNServers)
            {
                var time = await GetDelay(item.Server);
                item.Delay = time;
            }
        }
        private HttpClient _httpClient = new HttpClient()
        {
            Timeout = TimeSpan.FromSeconds(2)
        };
        private async Task<long> GetDelay(string server)
        {
            //随便整个链接
            var testUrl = $"https://{server}/upgcxcode/76/62/729206276/729206276_nb2-1-30112.m4s";

            try
            {
                Stopwatch sw = Stopwatch.StartNew();
                var res = await _httpClient.GetAsync(new Uri(testUrl));
                sw.Stop();
                return sw.ElapsedMilliseconds;
            }
            catch (Exception)
            {
                return -1;
            }

        }

        //傻逼微软，UWP连个Ping都不支持
        //private async Task<long> GetPing(string address)
        //{
        //    try
        //    {
        //        Ping ping = new Ping();
        //        PingOptions options = new PingOptions();
        //        options.DontFragment = true;
        //        byte[] buffer = new byte[32];
        //        for (int i = 0; i < 32; i++)
        //        {
        //            buffer[i] = 0;
        //        }
        //        int timeout = 3000;
        //        PingReply reply = await ping.SendPingAsync(address, timeout, buffer, options);
        //        if (reply.Status == IPStatus.Success)
        //        {
        //            return reply.RoundtripTime;
        //        }
        //        else
        //        {
        //            return -1;
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Debug.WriteLine(ex);
        //        return -1;
        //    }


        //}

    }
    /// <summary>
    /// 弹幕屏蔽规则的交换格式，只保留type和filter，兼容bilibili.block.json
    /// </summary>
    class DanmuFilterExchangeItem
    {
        public int type { get; set; }
        public string filter { get; set; }
    }
    public class DanmuFilterItem
    {
        public long id { get; set; }
        public long mid { get; set; }
        public int type { get; set; }
        public string filter { get; set; }
        public string comment { get; set; }
        public long ctime { get; set; }
        public long mtime { get; set; }

    }
    public class CDNServerItem : IModules
    {
        public CDNServerItem(string server, string remark)
        {
            this.Server = server;
            this.Remark = remark;
        }
        public string Server { get; set; }
        public string Remark { get; set; }

        public bool ShowDelay
        {
            get
            {
                return Delay > 0;
            }
        }
        public bool ShowTimeOut
        {
            get
            {
                return Delay < 0;
            }
        }
        private long _Delay = 0;
        public long Delay
        {
            get { return _Delay; }
            set
            {
                _Delay = value;
                DoPropertyChanged("Delay");
                DoPropertyChanged("ShowDelay");
                DoPropertyChanged("ShowTimeOut");
            }
        }

    }
}
