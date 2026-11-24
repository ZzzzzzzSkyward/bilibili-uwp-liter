using BiliLite.Services;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Networking.BackgroundTransfer;
using Windows.Storage;

namespace BiliLite.Helpers
{
    public enum DownloadType
    {
        /// <summary>
        /// 视频
        /// </summary>
        Video = 0,
        /// <summary>
        /// 番剧、电影、电视剧等
        /// </summary>
        Season = 1,
        /// <summary>
        /// 音乐，暂不支持
        /// </summary>
        Music = 2,
        /// <summary>
        /// 课程，暂不支持
        /// </summary>
        Cheese = 3
    }
    
    public static class DownloadHelper
    {
        public static BackgroundTransferGroup group = BackgroundTransferGroup.CreateGroup("BiliDownlad");//下载组，方便管理
        public async static Task AddDownload(DownloadInfo downloadInfo)
        {
            // Keep validated names, URLs and metadata bound to the captured admission across awaits.
            downloadInfo = JsonConvert.DeserializeObject<DownloadInfo>(JsonConvert.SerializeObject(downloadInfo));
            if (downloadInfo == null || downloadInfo.Urls == null || downloadInfo.Urls.Count == 0)
                throw new InvalidOperationException("没有可下载的视频链接，请重新读取后重试");
            foreach (var url in downloadInfo.Urls)
            {
                if (url == null || string.IsNullOrWhiteSpace(url.Url) || string.IsNullOrWhiteSpace(url.FileName))
                    throw new InvalidOperationException("视频下载链接或文件名无效，请重新读取后重试");
                // Use the same URI construction as the transfer, before any target mutation.
                try { new Uri(url.Url); }
                catch (Exception ex) { throw new InvalidOperationException("视频下载链接无效，请重新读取后重试", ex); }
            }
            var root = await GetDownloadFolder();
            var parentName = downloadInfo.Type == DownloadType.Season ? "ss" + downloadInfo.SeasonID : downloadInfo.AVID;
            var episodeName = downloadInfo.Type == DownloadType.Season ? downloadInfo.EpisodeID : downloadInfo.CID;
            var path = DownloadPathCoordinator.Child(DownloadPathCoordinator.Child(root.Path, parentName), episodeName);
            // Validate every filename before acquiring ownership or mutating storage.
            foreach (var url in downloadInfo.Urls) DownloadPathCoordinator.Child(path, url.FileName);
            if (downloadInfo.Subtitles != null)
                foreach (var subtitle in downloadInfo.Subtitles) DownloadPathCoordinator.Child(path, subtitle.Name + ".json");
            using (var lease = DownloadPathCoordinator.Acquire(path, creation: true))
            {
                var generation = DownloadPathCoordinator.BeginGeneration(lease, downloadInfo.Urls.Count);
                bool prepared = false;
                try
                {
                var folder = await root.CreateFolderAsync(parentName, CreationCollisionOption.OpenIfExists);
                var episodeFolder = await folder.CreateFolderAsync(episodeName, CreationCollisionOption.OpenIfExists);
                await DownloadCover(downloadInfo.CoverUrl, folder);
                await DownloadDanmaku(downloadInfo.DanmakuUrl, episodeFolder);
                if (downloadInfo.Subtitles != null)
                    foreach (var item in downloadInfo.Subtitles) await DownloadSubtitle(item, episodeFolder);
                var preparations = downloadInfo.Urls.Select(item => DownloadVideo(downloadInfo, item, episodeFolder, generation)).ToList();
                // WhenAll settles every sibling before a required error leaves the lease.
                Exception failure = null;
                try { await Task.WhenAll(preparations); } catch (Exception ex) { failure = ex; }
                try { await SaveInfo(downloadInfo, folder, episodeFolder); } catch (Exception ex) { failure = failure ?? ex; }
                if (failure != null) throw new InvalidOperationException("部分下载准备失败，已创建的任务已保留，请刷新任务列表后重试", failure);
                prepared = true;
                }
                finally { DownloadPathCoordinator.PreparationSettled(generation, prepared); }
            }
        }

        private async static Task<StorageFolder> GetDownloadFolder()
        {
            var path = SettingHelper.GetValue(SettingHelper.Download.DOWNLOAD_PATH, SettingHelper.Download.DEFAULT_PATH);
            if (path == SettingHelper.Download.DEFAULT_PATH)
            {
                var folder = KnownFolders.VideosLibrary;
                return await folder.CreateFolderAsync("哔哩哔哩下载", CreationCollisionOption.OpenIfExists);
            }
            else
            {
                return await StorageFolder.GetFolderFromPathAsync(path);
            }
        }
        private async static Task DownloadCover(string url, StorageFolder folder)
        {
            try
            {
                var buffer = await HttpHelper.GetBuffer(url + "@200w.jpg");
                StorageFile file = await folder.CreateFileAsync("cover.jpg", CreationCollisionOption.OpenIfExists);
                await FileIO.WriteBufferAsync(file, buffer);
                //Path.Combine(path,"cover.jpg")

            }
            catch (Exception ex)
            {
                LogHelper.Log("封面下载失败:" + url, LogType.ERROR, ex);
            }
        }
        private async static Task DownloadDanmaku(string url, StorageFolder episodeFolder)
        {
            try
            {
                var buffer = await HttpHelper.GetBuffer(url);
                StorageFile file = await episodeFolder.CreateFileAsync("danmaku.xml", CreationCollisionOption.OpenIfExists);
                await FileIO.WriteBufferAsync(file, buffer);
                //Path.Combine(path,"cover.jpg")

            }
            catch (Exception ex)
            {
                LogHelper.Log("弹幕下载失败:" + url, LogType.ERROR, ex);
            }
        }
        private async static Task SaveInfo(DownloadInfo info, StorageFolder folder, StorageFolder episodeFolder)
        {
            try
            {
                DownloadSaveInfo downloadSaveInfo = new DownloadSaveInfo() { 
                    Cover=info.CoverUrl,
                    SeasonType=info.SeasonType,
                    Title=info.Title,
                    Type=info.Type,
                    ID= info.Type== DownloadType.Season? info.SeasonID.ToString():info.AVID
                };
                DownloadSaveEpisodeInfo downloadSaveEpisodeInfo = new DownloadSaveEpisodeInfo() {
                    CID = info.CID,
                    DanmakuPath = "danmaku.xml",
                    EpisodeID = info.EpisodeID,
                    EpisodeTitle = info.EpisodeTitle,
                    AVID = info.AVID,
                    SubtitlePath = new List<DownloadSubtitleInfo>(),
                    Index = info.Index,
                    VideoPath = new List<string>(),
                    QualityID=info.QualityID,
                    QualityName=info.QualityName
                };
                if (info.Subtitles != null)
                {
                    foreach (var item in info.Subtitles)
                    {
                        downloadSaveEpisodeInfo.SubtitlePath.Add(new DownloadSubtitleInfo() { 
                            Name=item.Name,
                            Url=item.Name+".json"
                        });
                    }
                }
                foreach (var item in info.Urls)
                {
                    downloadSaveEpisodeInfo.VideoPath.Add(item.FileName);
                }
                downloadSaveEpisodeInfo.IsDash= downloadSaveEpisodeInfo.VideoPath.FirstOrDefault(x => x.Contains(".m4s"))!=null;
                StorageFile file = await folder.CreateFileAsync("info.json", CreationCollisionOption.ReplaceExisting);
                await FileIO.WriteTextAsync(file, JsonConvert.SerializeObject(downloadSaveInfo));
                StorageFile episodeFile = await episodeFolder.CreateFileAsync("info.json", CreationCollisionOption.ReplaceExisting);
                await FileIO.WriteTextAsync(episodeFile, JsonConvert.SerializeObject(downloadSaveEpisodeInfo));

            }
            catch (Exception ex)
            {
                LogHelper.Log("文件保存失败:" + episodeFolder.Path, LogType.ERROR,ex);
                throw;
            }
        }
        private async static Task DownloadSubtitle(DownloadSubtitleInfo subtitleInfo, StorageFolder episodeFolder)
        {
            try
            {
                var url = subtitleInfo.Url;
                if (!url.Contains("http:") || !url.Contains("https:"))
                {
                    url = "https:" + url;
                }
                var buffer = await HttpHelper.GetBuffer(url);
                StorageFile file = await episodeFolder.CreateFileAsync(subtitleInfo.Name + ".json", CreationCollisionOption.OpenIfExists);
                await FileIO.WriteBufferAsync(file, buffer);
                //Path.Combine(path,"cover.jpg")

            }
            catch (Exception ex)
            {
                LogHelper.Log($"字幕下载失败:{subtitleInfo.Name}={subtitleInfo.Url}", LogType.ERROR, ex);
            }
        }
        private async static Task DownloadVideo(DownloadInfo downloadInfo, DownloadUrlInfo url, StorageFolder episodeFolder, DownloadPathCoordinator.Generation generation)
        {
            BackgroundDownloader downloader = new BackgroundDownloader();
            if (url.HttpHeader!=null)
            {
                foreach (var item in url.HttpHeader)
                {
                    downloader.SetRequestHeader(item.Key, item.Value);
                }
            }
            var parallelDownload = SettingHelper.GetValue<bool>(SettingHelper.Download.PARALLEL_DOWNLOAD, true);
            var allowCostNetwork = SettingHelper.GetValue<bool>(SettingHelper.Download.ALLOW_COST_NETWORK, false);
            //设置下载模式
            if (parallelDownload)
            {
                group.TransferBehavior = BackgroundTransferBehavior.Parallel;
            }
            else
            {
                group.TransferBehavior = BackgroundTransferBehavior.Serialized;
            }
            downloader.TransferGroup = group;


            //创建视频文件
           
            StorageFile file = await episodeFolder.CreateFileAsync(url.FileName, CreationCollisionOption.OpenIfExists);
            DownloadOperation downloadOp = downloader.CreateDownload(new Uri(url.Url), file);
            //设置下载策略
            if (allowCostNetwork)
            {
                downloadOp.CostPolicy = BackgroundTransferCostPolicy.Always;
            }
            else
            {
                downloadOp.CostPolicy = BackgroundTransferCostPolicy.UnrestrictedOnly;
            }
            var guid = downloadOp.Guid.ToString();
            SettingHelper.SetValue(guid, new DownloadGUIDInfo() { 
                CID= downloadInfo.CID,
                Path= episodeFolder.Path,
                EpisodeTitle= downloadInfo.EpisodeTitle,
                FileName=url.FileName,
                Title= downloadInfo.Title,
                GUID=guid,
                Type= downloadInfo.Type,
                ID = downloadInfo.Type== DownloadType.Video? downloadInfo.AVID: downloadInfo.SeasonID.ToString()
            });
           
            // Start dispatch belongs to preparation; transfer completion owns no path lease.
            var transfer = downloadOp.StartAsync().AsTask();
            _ = ObserveTransferAsync(transfer, generation);
        }
        private static async Task ObserveTransferAsync(Task transfer, DownloadPathCoordinator.Generation generation)
        {
            bool success = false;
            try { await transfer; success = true; }
            catch (Exception ex) { LogHelper.Log("后台下载结束或失败", LogType.ERROR, ex); }
            finally { DownloadPathCoordinator.TransferSettled(generation, success); }

        }
    }
    public class DownloadInfo
    {
        public DownloadType Type { get; set; }
        public string AVID { get; set; }
        public long SeasonID { get; set; }
        public int SeasonType { get; set; }
        public string EpisodeID { get; set; }
        public string CID { get; set; }
        public string Title { get; set; }
        public string EpisodeTitle { get; set; }
        public int Index { get; set; }
        /// <summary>
        /// 下载链接
        /// </summary>
        public List<DownloadUrlInfo> Urls { get; set; }
        /// <summary>
        /// 封面
        /// </summary>
        public string CoverUrl { get; set; }
        /// <summary>
        /// 弹幕链接
        /// </summary>
        public string DanmakuUrl { get; set; }
        /// <summary>
        /// 字幕
        /// </summary>
        public List<DownloadSubtitleInfo> Subtitles { get; set; }
        public int QualityID { get; set; }
        public string QualityName { get; set; }

    }
    public class DownloadSubtitleInfo
    {
        public string Name { get; set; }
        public string Url { get; set; }
    }
    public class DownloadUrlInfo
    {
        /// <summary>
        /// 链接
        /// </summary>
        public string Url { get; set; }
        /// <summary>
        /// 保存文件名称
        /// </summary>
        public string FileName { get; set; }
        /// <summary>
        /// Http请求头
        /// </summary>
        public IDictionary<string, string> HttpHeader { get; set; }
    }


    public class DownloadSaveInfo
    {
        
        public DownloadType Type { get; set; }
        public string ID { get; set; }
        public int SeasonType { get; set; }
        public string Title { get; set; }
        public string Cover { get; set; }
     
    }
    public class DownloadSaveEpisodeInfo
    {
        public int Index { get; set; }
        public string AVID { get; set; }
        public string EpisodeID { get; set; }
        public string CID { get; set; }
        public string EpisodeTitle { get; set; }
        public List<string> VideoPath { get; set; }
        public string DanmakuPath { get; set; }
        public int QualityID { get; set; }
        public string QualityName { get; set; }
        public List<DownloadSubtitleInfo> SubtitlePath { get; set; }
        public bool IsDash { get; set; }
    }


    public class DownloadGUIDInfo
    {
        public string GUID { get; set; }
        public string CID { get; set; }
        public string ID { get; set; }
        public string FileName { get; set; }
        public string Title { get; set; }
        public string EpisodeTitle { get; set; }
        public string Path { get; set; }
        public DownloadType Type { get; set; }
    }
}
