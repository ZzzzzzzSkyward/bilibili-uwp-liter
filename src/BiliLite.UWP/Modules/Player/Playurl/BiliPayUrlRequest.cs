﻿using BiliLite.Api;
using BiliLite.Controls;
using BiliLite.Helpers;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Net.Http;
using BiliLite.gRPC.Api;
using Proto.Reply;

namespace BiliLite.Modules.Player.Playurl
{
    enum PlayUrlCodecMode
    {
        // int flv=0, dash=1,dash_hevc=2
        FLV = 0,
        DASH_H264 = 1,
        DASH_H265 = 2,
        DASH_AV1 = 3
    }
    class BiliPlayUrlRequest
    {
        protected bool IsDownload { get; set; } = false;
        protected readonly PlayerAPI playerAPI = new PlayerAPI();
        protected readonly gRPC.Api.PlayURL playUrlApi = new PlayURL();
        /// <summary>
        /// 是否开启替换CDN选项
        /// </summary>
        protected int ReplaceCDNMode { get; }

        /// <summary>
        /// 替换的CDN
        /// </summary>
        protected string CDN { get; }
        /// <summary>
        /// 是否大会员，非大会员最高只能看1080P
        /// </summary>
        protected bool IsVIP { get; }
        /// <summary>
        /// 选择的编码模式
        /// </summary>
        protected PlayUrlCodecMode CodecMode { get; set; }

        /// <summary>
        /// 错误信息
        /// </summary>
        public string Message { get; set; } = "";

        public const string WebUserAgent = "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/46.0.2490.80 Safari/537.36";
        public const string WebReferer = "https://www.bilibili.com";
        public const string AndroidUserAgent = "Bilibili Freedoooooom/MarkII";


        public BiliPlayUrlRequest(bool isDownload)
        {
            IsDownload = isDownload;
            //PriorityAkamaiCDN = SettingHelper.GetValue<bool>(SettingHelper.Roaming.AKAMAI_CDN, false);
            ReplaceCDNMode = SettingHelper.GetValue<int>(SettingHelper.Player.REPLACE_CDN, 3);
            CDN = SettingHelper.GetValue<string>(SettingHelper.Player.CDN_SERVER, "upos-sz-mirrorhwo1.bilivideo.com");
            IsVIP = (SettingHelper.Account.Logined && SettingHelper.Account.Profile.vip != null && SettingHelper.Account.Profile.vip.status != 0);
            CodecMode = (PlayUrlCodecMode)SettingHelper.GetValue<int>(IsDownload ? SettingHelper.Download.DEFAULT_VIDEO_TYPE : SettingHelper.Player.DEFAULT_VIDEO_TYPE, 1);
        }
        protected void AddMessage(string type, string msg)
        {
            Message += $"{type}：{msg}\r\n";
        }
        protected async Task<bool> CheckUrlAvailable(string url, string userAgent, string referer)
        {
            using (HttpClient client = new HttpClient() { Timeout = TimeSpan.FromSeconds(2) })
            {
                try
                {
                    if (userAgent != null && userAgent.Length > 0)
                    {
                        client.DefaultRequestHeaders.Add("User-Agent", userAgent);
                    }
                    if (referer != null && referer.Length > 0)
                    {
                        client.DefaultRequestHeaders.Add("Referer", referer);
                    }
                    client.DefaultRequestHeaders.Add("Range", "bytes=0-9");
                    var response = await client.GetAsync(url);
                    response.EnsureSuccessStatusCode();
                    return true;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                    return false;
                }
            }
        }
        protected async Task<string> ReplaceCDN(string url, string userAgent, string referer)
        {

            Regex regex = new Regex(@"http://|https://?([^/]*)");
            var host = regex.Match(url).Groups[1].Value;
            var replaceUrl = url.Replace(host, CDN);
            if (await CheckUrlAvailable(replaceUrl, userAgent, referer))
            {
                return replaceUrl;
            }
            else
            {
                return url;
            }
        }

        protected async Task<BiliPlayUrlQualitesInfo> ParseJson(int quality, JObject obj, string userAgent, string referer, bool isProxy)
        {
            try
            {
                BiliPlayUrlQualitesInfo info = new BiliPlayUrlQualitesInfo();
                info.Qualites = new List<BiliPlayUrlInfo>();

                var timeLength = obj["timelength"].ToInt32();

                foreach (var item in obj["support_formats"])
                {
                    info.Qualites.Add(new BiliPlayUrlInfo()
                    {
                        UserAgent = userAgent,
                        Referer = referer,
                        QualityID = item["quality"].ToInt32(),
                        QualityName = item["new_description"].ToString(),
                        Timelength = timeLength,
                        HasPlayUrl = false,
                    });
                }
                var qualites = info.Qualites.Select(x => x.QualityID).ToList();
                if (obj.ContainsKey("dash") && obj["dash"]["video"] != null)
                {
                    List<DashItemModel> videos = JsonConvert.DeserializeObject<List<DashItemModel>>(obj["dash"]["video"].ToString());
                    List<DashItemModel> audios = JsonConvert.DeserializeObject<List<DashItemModel>>(obj["dash"]["audio"].ToString());
                    var h264Videos = videos.Where(x => x.codecid == (int)BiliPlayUrlVideoCodec.AVC);
                    var h265Videos = videos.Where(x => x.codecid == (int)BiliPlayUrlVideoCodec.HEVC);
                    var av01Videos = videos.Where(x => x.codecid == (int)BiliPlayUrlVideoCodec.AV1);

                    var duration = obj["dash"]["duration"].ToInt32();
                    var minBufferTime = obj["dash"]["minBufferTime"].ToString();

                    var qn = quality;
                    if (qn > qualites.Max())
                    {
                        qn = qualites.Max();
                    }
                    if (!qualites.Contains(qn))
                    {
                        qn = qualites.Max();
                    }
                    for (int i = 0; i < info.Qualites.Count; i++)
                    {
                        var item = info.Qualites[i];
                        item.PlayUrlType = BiliPlayUrlType.DASH;
                        var video = h264Videos.FirstOrDefault(x => x.id == item.QualityID);
                        var h265_video = h265Videos.FirstOrDefault(x => x.id == item.QualityID);
                        var av1_video = av01Videos.FirstOrDefault(x => x.id == item.QualityID);
                        //h265处理
                        if (CodecMode == PlayUrlCodecMode.DASH_H265 && h265_video != null)
                        {
                            video = h265_video;
                        }
                        //av1处理
                        if (CodecMode == PlayUrlCodecMode.DASH_AV1)
                        {
                            //部分清晰度可能没有av1编码，切换至hevc
                            if (av1_video != null)
                            {

                                video = av1_video;
                            }
                            else if (h265_video != null)
                            {
                                video = h265_video;
                            }
                        }
                        //没有视频，跳过此清晰度
                        if (video == null)
                        {
                            //info.Qualites.Remove(item);
                            continue;
                        }
                        DashItemModel audio = null;
                        //部分视频没有音频文件
                        if (audios != null && audios.Count > 0)
                        {
                            if (qn > 64)
                            {
                                audio = audios.LastOrDefault();
                            }
                            else
                            {
                                audio = audios.FirstOrDefault();
                            }
                        }
                        //替换链接
                        video.baseUrl = await HandleUrl(video.baseUrl, video.backupUrl, userAgent, referer, isProxy);
                        if (audio != null)
                        {
                            audio.baseUrl = await HandleUrl(audio.baseUrl, audio.backupUrl, userAgent, referer, isProxy);
                        }

                        item.Codec = (BiliPlayUrlVideoCodec)video.codecid;
                        item.HasPlayUrl = true;

                        item.DashInfo = new BiliDashPlayUrlInfo()
                        {
                            Audio = audio?.ToBiliDashItem(),
                            Video = video.ToBiliDashItem(),
                        };
                    }
                    //移除没有链接的视频
                    info.Qualites = info.Qualites.Where(x => x.HasPlayUrl).ToList();
                    if (!IsVIP)
                    {
                        //非大会员，去除大会员专享清晰度
                        info.Qualites = info.Qualites.Where(x => x.QualityID != 74 && x.QualityID <= 80).ToList();
                    }

                    var current = info.Qualites.FirstOrDefault(x => x.QualityID == qn);
                    if (current == null)
                    {
                        current = info.Qualites.OrderByDescending(x => x.QualityID).FirstOrDefault(x => x.HasPlayUrl);
                    }
                    info.CurrentQuality = current;
                    return info;
                }
                else if (obj.ContainsKey("durl"))
                {
                    var durl = JsonConvert.DeserializeObject<List<FlvDurlModel>>(obj["durl"].ToString());
                    var index = qualites.IndexOf(quality);
                    if (index == -1)
                    {
                        index = 0;
                    }
                    //替换链接
                    foreach (var item in durl)
                    {
                        item.url = await HandleUrl(item.url, item.backup_url, userAgent, referer, isProxy);
                    }
                    info.Qualites[index].Codec = BiliPlayUrlVideoCodec.AVC;
                    info.Qualites[index].PlayUrlType = durl.Count == 1 ? BiliPlayUrlType.SingleFLV : BiliPlayUrlType.MultiFLV;
                    info.Qualites[index].FlvInfo = durl.Select(x => x.ToBiliFlvItem()).ToList();
                    info.Qualites[index].HasPlayUrl = true;


                    info.CurrentQuality = info.Qualites[index];
                    return info;
                }
                else
                {
                    return BiliPlayUrlQualitesInfo.Failure("无法读取播放链接");
                }
            }
            catch (Exception ex)
            {
                return BiliPlayUrlQualitesInfo.Failure(ex.Message);
            }

        }
        protected async Task<BiliPlayUrlQualitesInfo> ParseGrpc(int quality, PlayViewReply obj, string userAgent, string referer)
        {
            try
            {
                BiliPlayUrlQualitesInfo info = new BiliPlayUrlQualitesInfo();
                info.Qualites = new List<BiliPlayUrlInfo>();

                var timeLength = obj.VideoInfo.Timelength;

                foreach (var item in obj.VideoInfo.StreamList)
                {
                    info.Qualites.Add(new BiliPlayUrlInfo()
                    {
                        UserAgent = userAgent,
                        Referer = referer,
                        QualityID = item.StreamInfo.Quality,
                        QualityName = item.StreamInfo.NewDescription,
                        Timelength = timeLength,
                        HasPlayUrl = false,
                    });
                }
                var qualites = info.Qualites.Select(x => x.QualityID).ToList();

                if (obj.VideoInfo.DashAudio != null && obj.VideoInfo.DashAudio.Count > 0)
                {
                    List<DashItemModel> videos = new List<DashItemModel>();
                    List<DashItemModel> audios = new List<DashItemModel>();

                    foreach (var item in obj.VideoInfo.StreamList)
                    {
                        if (item.DashVideo == null)
                        {
                            continue;
                        }
                        var codecs = "avc1.640032";
                        var initialization = "0-995";
                        var indexRange = "996-4639";
                        if (item.DashVideo.Codecid == 12)
                        {
                            codecs = "hev1.1.6.L150.90";
                            initialization = "0-105";
                            indexRange = "1060-4703";
                        }
                        if (item.DashVideo.Codecid == 13)
                        {
                            codecs = "av01.0.08M.08.0.110.01.01.01.0";
                            initialization = "0-939";
                            indexRange = "1328-4983";
                        }
                        videos.Add(new DashItemModel()
                        {
                            backupUrl = item.DashVideo.BackupUrl.ToList(),
                            baseUrl = item.DashVideo.BaseUrl,
                            bandwidth = item.DashVideo.Bandwidth,
                            codecid = item.DashVideo.Codecid,
                            mimeType = "video/mp4",
                            id = item.StreamInfo.Quality,
                            startWithSap = 1,
                            sar = "",
                            codecs = codecs,
                            frameRate = "",
                            SegmentBase = new SegmentBase()
                            {
                                indexRange = indexRange,
                                initialization = initialization
                            }
                        });
                    }
                    foreach (var item in obj.VideoInfo.DashAudio)
                    {

                        audios.Add(new DashItemModel()
                        {
                            backupUrl = item.BackupUrl.ToList(),
                            baseUrl = item.BaseUrl,
                            bandwidth = item.Bandwidth,
                            codecid = item.Codecid,
                            mimeType = "audio/mp4",
                            id = item.Id,
                            codecs = "mp4a.40.2",
                            startWithSap = 0,
                            SegmentBase = new SegmentBase()
                            {
                                indexRange = "0-907",
                                initialization = "908-4575"
                            }
                        });
                    }


                    var h264Videos = videos.Where(x => x.codecid == (int)BiliPlayUrlVideoCodec.AVC);
                    var h265Videos = videos.Where(x => x.codecid == (int)BiliPlayUrlVideoCodec.HEVC);
                    var av01Videos = videos.Where(x => x.codecid == (int)BiliPlayUrlVideoCodec.AV1);

                    var duration = (timeLength / 1000).ToInt32();


                    var qn = quality;
                    if (qn > qualites.Max())
                    {
                        qn = qualites.Max();
                    }
                    if (!qualites.Contains(qn))
                    {
                        qn = qualites.Max();
                    }
                    for (int i = 0; i < info.Qualites.Count; i++)
                    {
                        var item = info.Qualites[i];
                        item.PlayUrlType = BiliPlayUrlType.DASH;
                        var video = h264Videos.FirstOrDefault(x => x.id == item.QualityID);
                        var h265_video = h265Videos.FirstOrDefault(x => x.id == item.QualityID);
                        var av1_video = av01Videos.FirstOrDefault(x => x.id == item.QualityID);
                        //h265处理
                        if (CodecMode == PlayUrlCodecMode.DASH_H265 && h265_video != null)
                        {
                            video = h265_video;
                        }
                        //av1处理
                        if (CodecMode == PlayUrlCodecMode.DASH_AV1)
                        {
                            //部分清晰度可能没有av1编码，切换至hevc
                            if (av1_video != null)
                            {

                                video = av1_video;
                            }
                            else if (h265_video != null)
                            {
                                video = h265_video;
                            }
                        }
                        //没有视频，跳过此清晰度
                        if (video == null)
                        {

                            //info.Qualites.Remove(item);
                            continue;
                        }
                        DashItemModel audio = null;
                        //部分视频没有音频文件
                        if (audios != null && audios.Count > 0)
                        {
                            if (qn > 64)
                            {
                                audio = audios.LastOrDefault();
                            }
                            else
                            {
                                audio = audios.FirstOrDefault();
                            }
                        }
                        //替换链接
                        video.baseUrl = await HandleUrl(video.baseUrl, video.backupUrl, userAgent, referer, false);
                        if (audio != null)
                        {
                            audio.baseUrl = await HandleUrl(audio.baseUrl, audio.backupUrl, userAgent, referer, false);
                        }

                        item.Codec = (BiliPlayUrlVideoCodec)video.codecid;
                        item.HasPlayUrl = true;
                        item.DashInfo = new BiliDashPlayUrlInfo()
                        {
                            Audio = audio?.ToBiliDashItem(),
                            Video = video.ToBiliDashItem(),
                        };
                    }
                    //移除没有链接的视频
                    info.Qualites = info.Qualites.Where(x => x.HasPlayUrl).ToList();
                    if (!IsVIP)
                    {
                        //非大会员，去除大会员专享清晰度
                        info.Qualites = info.Qualites.Where(x => x.QualityID != 74 && x.QualityID <= 80).ToList();
                    }
                    var current = info.Qualites.FirstOrDefault(x => x.QualityID == qn);
                    if (current == null)
                    {
                        current = info.Qualites.OrderByDescending(x => x.QualityID).FirstOrDefault(x => x.HasPlayUrl);
                    }
                    info.CurrentQuality = current;
                    return info;
                }
                else if (obj.VideoInfo.StreamList.FirstOrDefault(x => x.SegmentVideo != null) != null)
                {
                    var video = obj.VideoInfo.StreamList.FirstOrDefault(x => x.SegmentVideo != null);

                    List<FlvDurlModel> durl = new List<FlvDurlModel>();
                    foreach (var item in video.SegmentVideo.Segment)
                    {
                        durl.Add(new FlvDurlModel()
                        {
                            backup_url = item.BackupUrl.ToList(),
                            length = item.Length,
                            order = item.Order,
                            size = item.Size,
                            url = item.Url
                        });

                    }
                    //替换链接
                    foreach (var item in durl)
                    {
                        item.url = await HandleUrl(item.url, item.backup_url, userAgent, referer, false);
                    }
                    var index = qualites.IndexOf(quality);
                    if (index == -1)
                    {
                        index = 0;
                    }

                    info.Qualites[index].Codec = BiliPlayUrlVideoCodec.AVC;
                    info.Qualites[index].PlayUrlType = durl.Count == 1 ? BiliPlayUrlType.SingleFLV : BiliPlayUrlType.MultiFLV;
                    info.Qualites[index].FlvInfo = durl.Select(x => x.ToBiliFlvItem()).ToList();
                    info.Qualites[index].HasPlayUrl = true;


                    info.CurrentQuality = info.Qualites[index];
                    return info;
                }
                else
                {
                    return BiliPlayUrlQualitesInfo.Failure("无法读取播放链接");
                }
            }
            catch (Exception ex)
            {
                return BiliPlayUrlQualitesInfo.Failure(ex.Message);
            }

        }
        public virtual Task<BiliPlayUrlQualitesInfo> GetPlayUrlInfo(PlayInfo playInfo, int qualityID)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 处理链接
        /// 获取Akamai\替换CDN\替换PCDN
        /// </summary>
        /// <returns></returns>
        private async Task<string> HandleUrl(string url, List<string> backupUrl, string userAgent, string referer, bool isProxy)
        {
            var isPCDN = false;
            var optimizationUrl = url;
            var uri = new Uri(url);
            if (!uri.Host.Contains("bilivideo.com") && !uri.Host.Contains("akamaized.net"))
            {
                isPCDN = true;
                //设置优选链接（非PCDN）
                foreach (var item in backupUrl)
                {
                    var _host = new Uri(item).Host;
                    if (_host.Contains("bilivideo.com") || _host.Contains("akamaized.net"))
                    {
                        optimizationUrl = item;
                        break;
                    }
                }
            }
            //未开启CDN替换
            if (ReplaceCDNMode == 0)
            {
                return optimizationUrl;
            }
            //全部替换
            if (ReplaceCDNMode == 1)
            {
                return await ReplaceCDN(optimizationUrl, userAgent, referer);
            }
            //只替换PCDN
            if (ReplaceCDNMode == 2 && isPCDN)
            {
                return await ReplaceCDN(optimizationUrl, userAgent, referer);
            }
            //只替换代理
            if (ReplaceCDNMode == 3 && isProxy)
            {
                return await ReplaceCDN(optimizationUrl, userAgent, referer);
            }
            return optimizationUrl;
        }
    }
    class BiliVideoPlayUrlRequest : BiliPlayUrlRequest
    {
        public BiliVideoPlayUrlRequest(bool isDownload) : base(isDownload)
        {
        }

        public override async Task<BiliPlayUrlQualitesInfo> GetPlayUrlInfo(PlayInfo playInfo, int qualityID)
        {
            //尝试WEB API读取播放地址
            var webResult = await GetPlayUrlUseWebApi(playInfo, qualityID);
            if (webResult.Success)
            {
                return webResult;
            }
            AddMessage("[/x/player/playurl]", webResult.Message);
            //尝试GRPC API读取地址
            var grpcResult = await GetPlayUrlUseGrpc(playInfo, qualityID);
            if (grpcResult.Success)
            {
                return grpcResult;
            }
            AddMessage("[/v1.PlayURL/PlayView]", grpcResult.Message);
            return BiliPlayUrlQualitesInfo.Failure(Message);
        }

        private async Task<BiliPlayUrlQualitesInfo> GetPlayUrlUseWebApi(PlayInfo playInfo, int qualityID)
        {
            try
            {
                var webApiResult = await (playerAPI.VideoPlayUrl(aid: playInfo.avid, cid: playInfo.cid, qn: qualityID, dash: CodecMode != PlayUrlCodecMode.FLV, false, playInfo.area)).Request();
                if (!webApiResult.status)
                {
                    return BiliPlayUrlQualitesInfo.Failure(webApiResult.message);
                }
                var data = await webApiResult.GetData<JObject>();
                if (data.code != 0)
                {
                    return BiliPlayUrlQualitesInfo.Failure(data.message);
                }
                var jsonResult = await ParseJson(qualityID, data.data, WebUserAgent, WebReferer, false);
                return jsonResult;
            }
            catch (Exception ex)
            {
                return BiliPlayUrlQualitesInfo.Failure(ex.ToString());
            }

        }
        private async Task<BiliPlayUrlQualitesInfo> GetPlayUrlUseGrpc(PlayInfo playInfo, int qualityID)
        {
            try
            {
                Proto.Request.CodeType codec = CodecMode == PlayUrlCodecMode.DASH_H265 ? Proto.Request.CodeType.Code265 : Proto.Request.CodeType.Code264;

                var playViewReply = await playUrlApi.VideoPlayView(Convert.ToInt64(playInfo.avid), Convert.ToInt64(playInfo.cid), qualityID, 16, codec, SettingHelper.Account.AccessKey);

                var grpcResult = await ParseGrpc(qualityID, playViewReply, AndroidUserAgent, "");
                return grpcResult;
            }
            catch (Exception ex)
            {
                return BiliPlayUrlQualitesInfo.Failure(ex.ToString());
            }

        }
    }
    class BiliSeasonPlayUrlRequest : BiliPlayUrlRequest
    {
        public BiliSeasonPlayUrlRequest(bool isDownload) : base(isDownload)
        {
        }

        public override async Task<BiliPlayUrlQualitesInfo> GetPlayUrlInfo(PlayInfo playInfo, int qualityID)
        {
            //尝试WEB API读取播放地址
            // 按此顺序，访问代理
            string[] proxyAreas = new string[] { "", "cn", "hk", "tw" };
            if (playInfo.area == "hk")
            {
                proxyAreas = new string[] { "", "hk", "tw", "cn" };
            }
            else if (playInfo.area == "tw")
            {
                proxyAreas = new string[] { "", "tw", "hk", "cn" };
            }
            foreach (var item in proxyAreas)
            {
                var webResult = await GetPlayUrlUseWebApi(playInfo, qualityID, area: item);
                if (webResult.Success)
                {
                    return webResult;
                }
                var areaName = "无代理";
                switch (item)
                {
                    case "hk":
                        areaName = "香港代理";
                        break;
                    case "cn":
                        areaName = "大陆代理";
                        break;
                    case "tw":
                        areaName = "台湾代理";
                        break;
                    default:
                        break;
                }
                AddMessage($"WebAPI-{areaName}", webResult.Message);
            }


            //尝试GRPC API读取地址
            var grpcResult = await GetPlayUrlUseGrpc(playInfo, qualityID);
            if (grpcResult.Success)
            {
                return grpcResult;
            }
            AddMessage("[/v1.PlayURL/PlayView]", grpcResult.Message);
            return BiliPlayUrlQualitesInfo.Failure(Message);

        }

        private async Task<BiliPlayUrlQualitesInfo> GetPlayUrlUseWebApi(PlayInfo playInfo, int qualityID, string area = "")
        {
            try
            {
                var webApiResult = await (playerAPI.SeasonPlayUrl(aid: playInfo.avid, cid: playInfo.cid, ep_id: playInfo.ep_id, qn: qualityID, season_type: playInfo.season_type, dash: CodecMode != PlayUrlCodecMode.FLV, area != "", area)).Request();
                if (!webApiResult.status)
                {
                    return BiliPlayUrlQualitesInfo.Failure(webApiResult.message);
                }
                var data = await webApiResult.GetResult<JObject>();
                if (data.code != 0)
                {
                    return BiliPlayUrlQualitesInfo.Failure(data.message);
                }
                var jsonResult = await ParseJson(qualityID, data.result, WebUserAgent, WebReferer, area != "");
                return jsonResult;
            }
            catch (Exception ex)
            {

                return BiliPlayUrlQualitesInfo.Failure(ex.Message);
            }

        }
        private async Task<BiliPlayUrlQualitesInfo> GetPlayUrlUseGrpc(PlayInfo playInfo, int qualityID)
        {
            try
            {

                Proto.Request.CodeType codec = CodecMode == PlayUrlCodecMode.DASH_H265 ? Proto.Request.CodeType.Code265 : Proto.Request.CodeType.Code264;

                var playViewReply = await playUrlApi.BangumiPlayView(Convert.ToInt64(playInfo.ep_id), Convert.ToInt64(playInfo.cid), qualityID, 0, codec, SettingHelper.Account.AccessKey);

                var grpcResult = await ParseGrpc(qualityID, playViewReply, AndroidUserAgent, "");
                return grpcResult;
            }
            catch (Exception ex)
            {

                return BiliPlayUrlQualitesInfo.Failure(ex.Message);
            }
        }
    }

    public class DashItemModel
    {
        public long id { get; set; }
        public int bandwidth { get; set; }
        public string baseUrl { get; set; }
        public List<string> backupUrl { get; set; }
        public string mimeType { get; set; }
        public string codecs { get; set; }
        public int codecid { get; set; }
        public int width { get; set; }
        public int height { get; set; }
        public string frameRate { get; set; }
        public int startWithSap { get; set; }
        public string sar { get; set; }
        /// <summary>
        /// 计算平均帧数
        /// </summary>
        public string fps
        {
            get
            {
                try
                {
                    if (!string.IsNullOrEmpty(frameRate) && frameRate.Contains("/"))
                    {
                        var values = frameRate.Split('/');
                        if (values.Length == 1)
                        {
                            return frameRate;
                        }
                        double r = Convert.ToDouble(values[0]);
                        double d = Convert.ToDouble(values[1]);
                        return (r / d).ToString("0.0");
                    }
                    else
                    {
                        return frameRate;
                    }
                }
                catch (Exception)
                {
                    return frameRate;
                }

            }
        }

        public SegmentBase SegmentBase { get; set; }


        public BiliDashItem ToBiliDashItem()
        {
            return new BiliDashItem()
            {
                BandWidth = bandwidth,
                CodecID = codecid,
                Codecs = codecs,
                FrameRate = frameRate,
                Height = height,
                Width = width,
                ID = id,
                IsVideo = width != 0,
                MimeType = mimeType,
                Sar = sar,
                StartWithSap = startWithSap,
                SegmentBaseIndexRange = SegmentBase.indexRange,
                SegmentBaseInitialization = SegmentBase.initialization,
                Url = baseUrl,
            };
        }
    }

    public class SegmentBase
    {
        public string initialization { get; set; }
        public string indexRange { get; set; }

    }

    public class FlvDurlModel
    {
        public List<string> backup_url { get; set; }
        public string url { get; set; }
        public int order { get; set; }
        public long size { get; set; }
        public long length { get; set; }
        public BiliFlvPlayUrlInfo ToBiliFlvItem()
        {
            return new BiliFlvPlayUrlInfo()
            {
                Url = url,
                Length = length,
                Size = size,
                Order = order,
            };
        }
    }
}
