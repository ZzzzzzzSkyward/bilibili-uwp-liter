using BiliLite.Services;
using BiliLite.Helpers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.Networking.BackgroundTransfer;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Media.Imaging;

namespace BiliLite.Modules
{
    public sealed class DownloadCleanupTarget
    {
        internal DownloadCleanupTarget(string path, long revision, HashSet<string> guids, Dictionary<string, CancellationTokenSource> sources, bool cancelRunning, string title)
        { Path = path; Revision = revision; Guids = guids; Sources = sources; CancelRunning = cancelRunning; Title = title; }
        public string Path { get; }
        internal readonly long Revision;
        internal readonly HashSet<string> Guids;
        internal readonly Dictionary<string, CancellationTokenSource> Sources;
        internal readonly bool CancelRunning;
        internal bool WasCancelled, FailedCancellation;
        public string Title { get; }
        public string FailureText { get; internal set; }
    }
    public class DownloadVM : IModules
    {
        private static DownloadVM _downloadVM;
        public static DownloadVM Instance
        {
            get
            {
                if (_downloadVM == null)
                {
                    _downloadVM = new DownloadVM();
                }
                return _downloadVM;
            }
        }
        private ObservableCollection<DownloadedItem> _downloaded;
        public ObservableCollection<DownloadedItem> Downloadeds
        {
            get { return _downloaded; }
            set { _downloaded = value; DoPropertyChanged("Downloadeds"); }
        }

        private ObservableCollection<DownloadingItem> _downloading;
        public ObservableCollection<DownloadingItem> Downloadings
        {
            get { return _downloading; }
            set { _downloading = value; DoPropertyChanged("Downloadings"); }
        }

        public DownloadVM()
        {
            Downloadeds = new ObservableCollection<DownloadedItem>();
            Downloadings = new ObservableCollection<DownloadingItem>();

            RefreshDownloadedCommand = new RelayCommand(RefreshDownloaded);
            RetryCleanupCommand = new RelayCommand(RetryCleanup);
            PauseItemCommand = new RelayCommand<DownloadingSubItem>(PauseItem);
            ResumeItemCommand = new RelayCommand<DownloadingSubItem>(ResumeItem);
            DeleteItemCommand = new RelayCommand<DownloadingItem>(DeleteItem);
            PauseCommand = new RelayCommand(PauseAll);
            StartCommand = new RelayCommand(StartAll);
            DeleteCommand = new RelayCommand(DeleteAll);
        }
        public ICommand PauseCommand { get; private set; }
        public ICommand StartCommand { get; private set; }
        public ICommand DeleteCommand { get; private set; }
        public ICommand DeleteItemCommand { get; private set; }
        public ICommand PauseItemCommand { get; private set; }
        public ICommand ResumeItemCommand { get; private set; }

        public ICommand RefreshDownloadedCommand { get; private set; }
        private bool _loadingDownloaded = true;
        public bool LoadingDownloaded
        {
            get { return _loadingDownloaded; }
            set { _loadingDownloaded = value; DoPropertyChanged("LoadingDownloaded"); }
        }

        private double _diskTotal;
        public double DiskTotal
        {
            get { return _diskTotal; }
            set { _diskTotal = value; DoPropertyChanged("DiskTotal"); }
        }
        private double _diskUse;
        public double DiskUse
        {
            get { return _diskUse; }
            set { _diskUse = value; DoPropertyChanged("DiskUse"); }
        }
        private double _diskFree;
        public double DiskFree
        {
            get { return _diskFree; }
            set { _diskFree = value; DoPropertyChanged("DiskFree"); }
        }

        private bool legacyPartial;
        private bool scanPending;
        private bool discoveryPending;
        private string downloadedStatus = "";
        public string DownloadedStatus { get { return downloadedStatus; } private set { downloadedStatus = value; DoPropertyChanged("DownloadedStatus"); } }
        public bool DiskAvailable { get; private set; }
        public ObservableCollection<DownloadCleanupTarget> CleanupFailures { get; } = new ObservableCollection<DownloadCleanupTarget>();
        public ICommand RetryCleanupCommand { get; private set; }

        public void RefreshDownloaded()
        {
            if (LoadingDownloaded)
            {
                return;
            }

            LoadDownloaded();
        }
        /// <summary>
        /// 读取下载的视频
        /// </summary>
        /// <returns></returns>
        public async void LoadDownloaded()
        {
            try { await LoadDownloadedAsync(); }
            catch (Exception ex) { LogHelper.Log("下载目录读取失败", LogType.ERROR, ex); }
        }
        public async Task LoadDownloadedAsync()
        {
            if (scanPending) return;
            scanPending = true;
            LoadingDownloaded = true;
            var staged = new ObservableCollection<DownloadedItem>();
            bool partial = false;
            bool withheld = false;
            var reads = new Dictionary<DownloadedSubItem, DownloadPathCoordinator.ReadStamp>();
            var candidates = new Dictionary<DownloadedItem, Dictionary<string, DownloadPathCoordinator.ReadStamp>>();
            try
            {
            var folder = await GetDownloadFolder();
            try { await LoadDiskSize(folder); DiskAvailable = true; }
            catch (Exception ex) { DiskAvailable = false; DiskFree = DiskTotal = DiskUse = 0; partial = true; LogHelper.Log("磁盘信息读取失败", LogType.ERROR, ex); }
            DoPropertyChanged("DiskAvailable");
            // var list = new List<DownloadedItem>();
            foreach (var item in await folder.GetFoldersAsync())
            {
                try
                {
                    //检查是否存在info.json
                    var infoFile = await item.TryGetItemAsync("info.json") as StorageFile;
                    if (infoFile == null)
                    {
                        continue;
                    }
                    var info = JsonConvert.DeserializeObject<DownloadSaveInfo>(await FileIO.ReadTextAsync(infoFile));
                    //旧版无Cover字段，跳过
                    if (string.IsNullOrEmpty(info.Cover))
                    {
                        continue;
                    }
                    List<DownloadedSubItem> lsEpisodes = new List<DownloadedSubItem>();
                    DownloadedItem downloadedItem = new DownloadedItem()
                    {
                        CoverPath = Path.Combine(item.Path, "cover.jpg"),
                        Episodes = new ObservableCollection<DownloadedSubItem>(),
                        ID = info.ID,
                        Title = info.Title,
                        UpdateTime = infoFile.DateCreated.LocalDateTime,
                        IsSeason = info.Type == DownloadType.Season,
                        Path = item.Path
                    };
                    candidates[downloadedItem] = new Dictionary<string, DownloadPathCoordinator.ReadStamp>();
                    var coverFile = await item.TryGetItemAsync("cover.jpg") as StorageFile;
                    if (coverFile != null)
                    {
                        BitmapImage bitmapImage = new BitmapImage();
                        var buffer = await FileIO.ReadBufferAsync(coverFile);
                        using (IRandomAccessStream stream = new InMemoryRandomAccessStream())
                        {
                            await stream.WriteAsync(buffer);
                            stream.Seek(0);
                            bitmapImage.SetSource(stream);
                            downloadedItem.Cover = bitmapImage;

                        }

                    }

                    foreach (var episodeItem in await item.GetFoldersAsync())
                    {
                        try
                        {
                        var readStamp = DownloadPathCoordinator.CaptureRead(episodeItem.Path);
                        candidates[downloadedItem][episodeItem.Path] = readStamp;
                        //检查是否存在info.json
                        var episodeInfoFile = await episodeItem.TryGetItemAsync("info.json") as StorageFile;
                        if (episodeInfoFile == null)
                        {
                            continue;
                        }
                        var files = (await episodeItem.GetFilesAsync()).Where(x => x.FileType == ".blv" || x.FileType == ".mp4" || x.FileType == ".m4s");
                        if (files.Count() == 0)
                        {
                            continue;
                        }
                        bool flag = false;
                        foreach (var subfile in files)
                        {
                            var size = (await subfile.GetBasicPropertiesAsync()).Size;
                            if (size == 0)
                            {
                                flag = true;
                                break;
                            }
                        }
                        files = null;
                        if (flag)
                        {
                            continue;
                        }

                        var episodeInfo = JsonConvert.DeserializeObject<DownloadSaveEpisodeInfo>(await FileIO.ReadTextAsync(episodeInfoFile));
                        var downloadedSubItem = new DownloadedSubItem()
                        {
                            AVID = episodeInfo.AVID,
                            CID = episodeInfo.CID,
                            Index = episodeInfo.Index,
                            EpisodeID = episodeInfo.EpisodeID,
                            IsDash = episodeInfo.IsDash,
                            Paths = new List<string>(),
                            Title = episodeInfo.EpisodeTitle,
                            SubtitlePath = new List<DownloadSubtitleInfo>(),
                            Path = episodeItem.Path
                        };
                        //设置视频
                        foreach (var path in episodeInfo.VideoPath)
                        {
                            downloadedSubItem.Paths.Add(Path.Combine(episodeItem.Path, path));
                        }
                        if (!string.IsNullOrEmpty(episodeInfo.DanmakuPath))
                        {
                            downloadedSubItem.DanmakuPath = Path.Combine(episodeItem.Path, episodeInfo.DanmakuPath);
                        }
                        if (episodeInfo.SubtitlePath != null)
                        {
                            foreach (var subtitle in episodeInfo.SubtitlePath)
                            {
                                downloadedSubItem.SubtitlePath.Add(new DownloadSubtitleInfo()
                                {
                                    Name = subtitle.Name,
                                    Url = Path.Combine(episodeItem.Path, subtitle.Url)
                                });
                            }
                        }

                        reads?.Add(downloadedSubItem, readStamp);
                        lsEpisodes.Add(downloadedSubItem);
                        }
                        catch (Exception ex) { partial = true; LogHelper.Log("下载分集读取失败", LogType.ERROR, ex); }
                    }
                    //排序
                    foreach (var episode in lsEpisodes.OrderBy(x => x.Index))
                    {
                        downloadedItem.Episodes.Add(episode);
                    }

                    staged.Add(downloadedItem);
                }
                catch (Exception ex)
                {
                    partial = true; LogHelper.Log("下载目录子项读取失败", LogType.ERROR, ex);
                    continue;
                }


            }


                if (SettingHelper.GetValue(SettingHelper.Download.LOAD_OLD_DOWNLOAD, false))
                {
                    try { legacyPartial = false; await LoadDownloadedOld(staged, reads, candidates); partial |= legacyPartial; }
                    catch (Exception ex) { partial = true; LogHelper.Log("旧下载目录读取失败", LogType.ERROR, ex); }
                }
                // No await separates this classification check from staged publication.
                foreach (var title in staged.ToList())
                {
                    bool removed = false;
                    Dictionary<string, DownloadPathCoordinator.ReadStamp> titleCandidates;
                    if (candidates.TryGetValue(title, out titleCandidates))
                        foreach (var candidate in titleCandidates)
                            if (!DownloadPathCoordinator.CanPublish(candidate.Key, candidate.Value)) removed = withheld = true;
                    foreach (var episode in title.Episodes.ToList())
                    {
                        DownloadPathCoordinator.ReadStamp stamp;
                        if (reads.TryGetValue(episode, out stamp) && !DownloadPathCoordinator.CanPublish(episode.Path, stamp))
                        { title.Episodes.Remove(episode); removed = withheld = true; }
                    }
                    if (removed && title.Episodes.Count == 0) staged.Remove(title);
                }
                foreach (var failure in CleanupFailures.ToList())
                    if (DownloadPathCoordinator.CompletedRevision(failure.Path) > failure.Revision) CleanupFailures.Remove(failure);
                Downloadeds = staged;
                DownloadedStatus = withheld ? "部分下载尚未完成或待清理，请完成下载或清理后刷新" : partial ? "部分下载内容或磁盘信息读取失败，请刷新重试" : (staged.Count == 0 ? "暂无离线视频" : "");
            }
            catch (Exception ex)
            {
                DiskAvailable = false; DiskFree = DiskTotal = DiskUse = 0; DoPropertyChanged("DiskAvailable");
                DownloadedStatus = "下载目录读取失败，请刷新重试";
                LogHelper.Log("下载目录读取失败", LogType.ERROR, ex);
            }
            finally { scanPending = false; LoadingDownloaded = false; }
        }
        /// <summary>
        /// 读取旧版下载的视频
        /// </summary>
        /// <returns></returns>
        public async Task LoadDownloadedOld(ObservableCollection<DownloadedItem> target = null, IDictionary<DownloadedSubItem, DownloadPathCoordinator.ReadStamp> reads = null, IDictionary<DownloadedItem, Dictionary<string, DownloadPathCoordinator.ReadStamp>> candidates = null)
        {

            var folder = await GetDownloadOldFolder();

            //var list = new List<DownloadedItem>();
            foreach (var item in await folder.GetFoldersAsync())
            {
                try
                {
                    //检查是否存在info.json
                    var infoFile = await item.TryGetItemAsync("info.json") as StorageFile;
                    if (infoFile == null)
                    {
                        continue;
                    }
                  
                    var info = JObject.Parse(await FileIO.ReadTextAsync(infoFile));
                    //新版下载无thumb字段
                    if (!info.ContainsKey("thumb"))
                    {
                        continue;
                    }
                    List<DownloadedSubItem> lsEpisodes = new List<DownloadedSubItem>();
                    DownloadedItem downloadedItem = new DownloadedItem()
                    {
                        CoverPath = Path.Combine(item.Path, "thumb.jpg"),
                        Episodes = new ObservableCollection<DownloadedSubItem>(),
                        ID = info["id"].ToString(),
                        Title = info["title"].ToString(),
                        UpdateTime = infoFile.DateCreated.LocalDateTime,
                        IsSeason = info["type"].ToString() != "video",
                        Path = item.Path
                    };
                    if (candidates != null) candidates[downloadedItem] = new Dictionary<string, DownloadPathCoordinator.ReadStamp>();
                    var coverFile = await item.TryGetItemAsync("thumb.jpg") as StorageFile;
                    if (coverFile != null)
                    {
                        BitmapImage bitmapImage = new BitmapImage();
                        bitmapImage.SetSource(await coverFile.GetScaledImageAsThumbnailAsync(Windows.Storage.FileProperties.ThumbnailMode.VideosView));
                        downloadedItem.Cover = bitmapImage;
                    }

                    foreach (var episodeItem in await item.GetFoldersAsync())
                    {
                        try
                        {
                        var readStamp = DownloadPathCoordinator.CaptureRead(episodeItem.Path);
                        if (candidates != null) candidates[downloadedItem][episodeItem.Path] = readStamp;
                        //检查是否存在info.json
                        var episodeInfoFile = await episodeItem.TryGetItemAsync("info.json") as StorageFile;
                        if (episodeInfoFile == null)
                        {
                            continue;
                        }
                        var files = (await episodeItem.GetFilesAsync()).Where(x => x.FileType == ".blv" || x.FileType == ".flv" || x.FileType == ".mp4" || x.FileType == ".m4s");
                        if (files.Count() == 0)
                        {
                            continue;
                        }
                        bool flag = false;
                        foreach (var subfile in files)
                        {
                            var size = (await subfile.GetBasicPropertiesAsync()).Size;
                            if (size == 0)
                            {
                                flag = true;
                                break;
                            }
                        }

                        if (flag)
                        {
                            continue;
                        }

                        var episodeInfo = JObject.Parse(await FileIO.ReadTextAsync(episodeInfoFile));
                        var downloadedSubItem = new DownloadedSubItem()
                        {
                            AVID = "",
                            CID = episodeInfo["cid"].ToString(),
                            Index = episodeInfo["index"].ToInt32(),
                            EpisodeID = episodeInfo["epid"]?.ToString() ?? "",
                            IsDash = await episodeItem.TryGetItemAsync("video.m4s") != null,
                            Paths = new List<string>(),
                            Title = episodeInfo["title"].ToString(),
                            SubtitlePath = new List<DownloadSubtitleInfo>(),
                            Path = episodeItem.Path
                        };
                        //设置视频
                        foreach (var file in files)
                        {
                            downloadedSubItem.Paths.Add(file.Path);
                        }

                        files = null;
                        downloadedSubItem.DanmakuPath = Path.Combine(episodeItem.Path, downloadedSubItem.CID + ".xml");


                        reads?.Add(downloadedSubItem, readStamp);
                        lsEpisodes.Add(downloadedSubItem);
                        }
                        catch (Exception ex) { legacyPartial = true; LogHelper.Log("旧下载分集读取失败", LogType.ERROR, ex); }
                    }
                    //排序
                    foreach (var episode in lsEpisodes.OrderBy(x => x.Index))
                    {
                        downloadedItem.Episodes.Add(episode);
                    }

                    (target ?? Downloadeds).Add(downloadedItem);
                }
                catch (Exception ex)
                {
                    LogHelper.Log("旧下载子项读取失败", LogType.ERROR, ex);
                    legacyPartial = true;
                    continue;
                }


            }

            // list = list.OrderByDescending(x => x.UpdateTime).ToList();

            //return list;

        }
        /// <summary>
        /// 读取磁盘可用空间
        /// </summary>
        /// <param name="folder"></param>
        /// <returns></returns>
        public async Task LoadDiskSize(StorageFolder folder)
        {
            var properties = await folder.Properties.RetrievePropertiesAsync(new string[] { "System.FreeSpace", "System.Capacity" });
            DiskFree = (ulong)properties["System.FreeSpace"] / 1024d / 1024d / 1024d;
            DiskTotal = (ulong)properties["System.Capacity"] / 1024d / 1024d / 1024d;
            DiskUse = DiskTotal - DiskFree;
        }
        /// <summary>
        /// 下载目录
        /// </summary>
        /// <returns></returns>
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
        /// <summary>
        /// 旧版下载目录
        /// </summary>
        /// <returns></returns>
        private async static Task<StorageFolder> GetDownloadOldFolder()
        {
            var path = SettingHelper.GetValue(SettingHelper.Download.OLD_DOWNLOAD_PATH, SettingHelper.Download.DEFAULT_OLD_PATH);
            if (path == SettingHelper.Download.DEFAULT_OLD_PATH)
            {
                var folder = KnownFolders.VideosLibrary;
                return await folder.CreateFolderAsync("BiliBiliDownload", CreationCollisionOption.OpenIfExists);
            }
            else
            {
                return await StorageFolder.GetFolderFromPathAsync(path);
            }
        }

        private readonly IDictionary<string, CancellationTokenSource> cts = new Dictionary<string, CancellationTokenSource>();
        private readonly Dictionary<string, CancellationTokenSource> failedCancellations = new Dictionary<string, CancellationTokenSource>();
        private readonly HashSet<string> observerFinished = new HashSet<string>();
        private readonly HashSet<string> independentlyAbsent = new HashSet<string>();
        private void RetireSource(string guid, CancellationTokenSource source)
        {
            CancellationTokenSource owned;
            if (cts.TryGetValue(guid, out owned) && ReferenceEquals(source, owned))
            { cts.Remove(guid); source.Dispose(); }
            downloadOperations?.RemoveAll(x => x.Guid.ToString() == guid);
            observerFinished.Remove(guid); independentlyAbsent.Remove(guid);
        }
        private void ReconcileAbsent(HashSet<string> current)
        {
            foreach (var entry in cts.ToList())
            {
                if (current.Contains(entry.Key)) continue;
                independentlyAbsent.Add(entry.Key);
                CancellationTokenSource failed;
                if (failedCancellations.TryGetValue(entry.Key, out failed) && ReferenceEquals(failed, entry.Value)) failedCancellations.Remove(entry.Key);
                if (observerFinished.Contains(entry.Key)) RetireSource(entry.Key, entry.Value);
            }
        }
        List<DownloadOperation> downloadOperations;
        /// <summary>
        /// 读取下载中
        /// </summary>
        public async void LoadDownloading()
        {
            if (discoveryPending) return;
            discoveryPending = true;
            try
            {
            if (downloadOperations == null)
            {
                downloadOperations = new List<DownloadOperation>();
            }
            var pendingObservers = new List<Tuple<DownloadOperation, CancellationTokenSource>>();
            ObservableCollection<DownloadingSubItem> subItems = new ObservableCollection<DownloadingSubItem>();
            var staged = new ObservableCollection<DownloadingItem>();
            var ls = await BackgroundDownloader.GetCurrentDownloadsForTransferGroupAsync(DownloadHelper.group);
            var metadata = new Dictionary<string, DownloadGUIDInfo>();
            bool metadataPartial = false;
            foreach (var op in ls)
            {
                var key = op.Guid.ToString();
                var value = SettingHelper.GetValue<DownloadGUIDInfo>(key, null);
                try
                {
                    if (value == null || string.IsNullOrWhiteSpace(value.CID) || string.IsNullOrWhiteSpace(value.Path) || value.GUID != key)
                        throw new DownloadPathException(DownloadPathFailure.Reconciliation);
                    DownloadPathCoordinator.Normalize(value.Path);
                    metadata.Add(key, value);
                }
                catch (Exception ex) { metadataPartial = true; LogHelper.Log("下载任务元数据读取失败", LogType.ERROR, ex); }
            }
            if (!metadataPartial) ReconcileAbsent(new HashSet<string>(ls.Select(x => x.Guid.ToString())));
            foreach (var item in ls)
            {
                CancellationTokenSource cancellationTokenSource = null;

                DownloadGUIDInfo data;
                if (!metadata.TryGetValue(item.Guid.ToString(), out data)) continue;
                if (cts.ContainsKey(item.Guid.ToString()))
                {
                    cancellationTokenSource = cts[item.Guid.ToString()];
                }
                else
                {
                    cancellationTokenSource = new CancellationTokenSource();
                    cts.Add(item.Guid.ToString(), cancellationTokenSource);
                }

                if (!downloadOperations.Any(x => x.Guid == item.Guid))
                {
                    downloadOperations.Add(item);
                    pendingObservers.Add(Tuple.Create(item, cancellationTokenSource));
                }

                subItems.Add(new DownloadingSubItem()
                {
                    ProgressBytes = item.Progress.BytesReceived,
                    TotalBytes = item.Progress.TotalBytesToReceive,
                    Progress = GetProgress(item.Progress.BytesReceived, item.Progress.TotalBytesToReceive),
                    Status = item.Progress.Status,
                    Title = data.Title,
                    FileName = data.FileName,
                    EpisodeTitle = data.EpisodeTitle,
                    Path = data.Path,
                    CID = data.CID,
                    GUID = data.GUID,
                    PauseItemCommand = PauseItemCommand,
                    ResumeItemCommand = ResumeItemCommand
                });
            }
            foreach (var item in subItems.GroupBy(x => new { x.CID, x.Path }))
            {
                ObservableCollection<DownloadingSubItem> items = new ObservableCollection<DownloadingSubItem>();
                foreach (var item2 in item)
                {
                    items.Add(item2);
                }
                staged.Add(new DownloadingItem()
                {
                    EpisodeID = item.Key.CID,
                    Items = items,
                    Title = item.FirstOrDefault().Title,
                    EpisodeTitle = item.FirstOrDefault().EpisodeTitle,
                    Path = item.FirstOrDefault().Path,
                    DeleteItemCommand = DeleteItemCommand,
                });
            }
            if (metadataPartial)
            {
                // Preserve established rows/sources while still exposing successfully published siblings.
                foreach (var row in staged)
                {
                    var existing = Downloadings.FirstOrDefault(x => x.EpisodeID == row.EpisodeID && x.Path == row.Path);
                    if (existing == null) Downloadings.Add(row);
                    else foreach (var child in row.Items) if (!existing.Items.Any(x => x.GUID == child.GUID)) existing.Items.Add(child);
                }
                Utils.ShowMessageToast("部分下载任务读取失败，请刷新重试");
            }
            else Downloadings = staged;
            foreach (var observer in pendingObservers) _ = Handel(observer.Item1, observer.Item2);
            }
            catch (Exception ex) { LogHelper.Log("下载任务读取失败", LogType.ERROR, ex); Utils.ShowMessageToast("下载任务读取失败，请重试"); }
            finally { discoveryPending = false; }

        }
        private async Task Handel(DownloadOperation downloadOperation, CancellationTokenSource cancellationTokenSource)
        {
            bool success = true;
            try
            {

                Progress<DownloadOperation> progressCallback = new Progress<DownloadOperation>(DownloadProgress);
                if (cancellationTokenSource != null)
                {
                    await downloadOperation.AttachAsync().AsTask(cancellationTokenSource.Token, progressCallback);
                }
                else
                {
                    await downloadOperation.AttachAsync().AsTask(progressCallback);
                }

                //var ls = list_Downing.ItemsSource as ObservableCollection<DisplayModel>;
                RefreshDownloaded();

            }
            catch (TaskCanceledException)
            {
                success = false;
            }
            catch (Exception ex)
            {
                success = false;
                if (ex.Message.Contains("0x80072EF1") || ex.Message.Contains("0x80070002") || ex.Message.Contains("0x80004004"))
                {
                    return;
                }
                var guid = downloadOperation.Guid.ToString();
                var item = Downloadings.FirstOrDefault(x => x.Items.FirstOrDefault(y => y.GUID == guid) != null);
                try { await Utils.ShowDialog("下载出现问题", $"失败视频:{item?.Title ?? ""} {item?.EpisodeTitle ?? ""}\r\n" + ex.Message); }
                catch (Exception reportError) { LogHelper.Log("下载错误提示失败", LogType.ERROR, reportError); }
            }
            finally
            {
                var guid = downloadOperation.Guid.ToString();
                RemoveItem(guid, success);
                observerFinished.Add(guid);
                // A locally cancelled observer alone is not proof that a failed native cancellation settled.
                if (!cancellationTokenSource.IsCancellationRequested || independentlyAbsent.Contains(guid)) RetireSource(guid, cancellationTokenSource);

            }
        }
        private void DownloadProgress(DownloadOperation op)
        {
            try
            {
                if (Downloadings == null || Downloadings.Count == 0)
                {
                    return;
                }
                var guid = op.Guid.ToString();

                var item = Downloadings.FirstOrDefault(x => x.Items.Count(y => y.GUID == guid) != 0);
                if (item != null)
                {
                    var subItem = item.Items.FirstOrDefault(x => x.GUID == guid);
                    if (subItem != null)
                    {
                        subItem.ProgressBytes = op.Progress.BytesReceived;
                        subItem.Status = op.Progress.Status;
                        subItem.TotalBytes = op.Progress.TotalBytesToReceive;
                        subItem.Progress = GetProgress(subItem.ProgressBytes, subItem.TotalBytes);
                    }
                }

            }
            catch (Exception)
            {
                return;
            }

        }

        private double GetProgress(ulong progress, ulong total)
        {
            if (total == 0) return 0;
            if (total >= progress)
            {
                return ((double)progress / total) * 100;
            }
            else
            {
                return 0;
            }
        }
        private void RemoveItem(string guid, bool success = true)
        {
            try
            {
                if (Downloadings == null || Downloadings.Count == 0)
                {
                    return;
                }
                var item = Downloadings.FirstOrDefault(x => x.Items.Count(y => y.GUID == guid) != 0);
                if (item != null)
                {
                    if (item.Items.Count > 1)
                    {
                        var subItem = item.Items.FirstOrDefault(x => x.GUID == guid);
                        if (subItem != null)
                        {
                            item.Items.Remove(subItem);
                        }
                    }
                    else
                    {
                        if (success && SettingHelper.GetValue<bool>(SettingHelper.Download.SEND_TOAST, false))
                        {
                            Utils.ShowMessageToast("《" + item.Title + " " + item.EpisodeTitle + "》下载完成");
                        }
                        Downloadings.Remove(item);

                    }
                }

            }
            catch (Exception ex)
            {
            }

        }
        /// <summary>
        /// 暂停下载
        /// </summary>
        /// <param name="item"></param>
        private void PauseItem(DownloadingSubItem item) { ControlOperation(item, true, true); }
        private void ResumeItem(DownloadingSubItem item) { ControlOperation(item, false, true); }
        private bool ControlOperation(DownloadingSubItem item, bool pause, bool report)
        {
            var op = downloadOperations?.FirstOrDefault(x => x.Guid.ToString() == item?.GUID);
            if (op == null) return true;
            try { if (pause) op.Pause(); else op.Resume(); return true; }
            catch (Exception ex)
            {
                LogHelper.Log(pause ? "暂停下载失败" : "恢复下载失败", LogType.ERROR, ex);
                if (report) Utils.ShowMessageToast(pause ? "暂停失败，请刷新任务后重试" : "恢复失败，请刷新任务后重试");
                return false;
            }
        }
        private bool cleanupPending;
        private async Task<HashSet<string>> ReconcileAsync(string path)
        {
            try
            {
            var result = new HashSet<string>();
            var operations = await BackgroundDownloader.GetCurrentDownloadsForTransferGroupAsync(DownloadHelper.group);
            foreach (var op in operations)
            {
                var guid = op.Guid.ToString();
                var data = SettingHelper.GetValue<DownloadGUIDInfo>(guid, null);
                if (data == null || string.IsNullOrWhiteSpace(data.Path) || data.GUID != guid || string.IsNullOrWhiteSpace(data.CID))
                    throw new DownloadPathException(DownloadPathFailure.Reconciliation);
                if (DownloadPathCoordinator.Contains(path, data.Path)) result.Add(guid);
            }
            ReconcileAbsent(new HashSet<string>(operations.Select(x => x.Guid.ToString())));
            return result;
            }
            catch (Exception ex) { throw new DownloadPathException(DownloadPathFailure.Reconciliation, ex); }
        }
        public async Task<DownloadCleanupTarget> CaptureCleanupAsync(string path, string title, bool cancelRunning)
        {
            using (var lease = DownloadPathCoordinator.Acquire(path))
            {
                var guids = await ReconcileAsync(path);
                if (!cancelRunning && guids.Count != 0) throw new DownloadPathException(DownloadPathFailure.Busy);
                var sources = new Dictionary<string, CancellationTokenSource>();
                if (cancelRunning)
                    foreach (var guid in guids)
                    {
                        CancellationTokenSource source;
                        if (!cts.TryGetValue(guid, out source)) throw new DownloadPathException(DownloadPathFailure.Reconciliation);
                        sources.Add(guid, source);
                    }
                return new DownloadCleanupTarget(path, lease.Revision, guids, sources, cancelRunning, title);
            }
        }
        public async Task DeleteCleanupAsync(DownloadCleanupTarget target)
        {
            using (var lease = DownloadPathCoordinator.Acquire(target.Path, expectedRevision: target.Revision))
            {
                var current = await ReconcileAsync(target.Path);
                if (!current.IsSubsetOf(target.Guids) || (!target.CancelRunning && current.Count != 0)) throw new DownloadPathException(DownloadPathFailure.Changed);
                // Resolve every source before cancelling any operation.
                foreach (var guid in current)
                {
                    CancellationTokenSource source;
                    if (!cts.TryGetValue(guid, out source) || !ReferenceEquals(source, target.Sources[guid])) throw new DownloadPathException(DownloadPathFailure.Changed);
                    CancellationTokenSource failed;
                    if (failedCancellations.TryGetValue(guid, out failed) && ReferenceEquals(failed, source))
                    { target.FailedCancellation = true; throw new DownloadPathException(DownloadPathFailure.Reconciliation); }
                }
                foreach (var guid in current)
                {
                    var operation = downloadOperations.First(x => x.Guid.ToString() == guid);
                    var metadata = SettingHelper.GetValue<DownloadGUIDInfo>(guid, null);
                    if (operation.Progress.TotalBytesToReceive == 0 || operation.Progress.BytesReceived < operation.Progress.TotalBytesToReceive)
                        DownloadPathCoordinator.MarkPartial(metadata.Path);
                    try { target.Sources[guid].Cancel(); target.WasCancelled = true; }
                    catch
                    {
                        failedCancellations[guid] = target.Sources[guid]; target.FailedCancellation = true;
                        throw;
                    }
                }
                try
                {
                    var folder = await StorageFolder.GetFolderFromPathAsync(target.Path);
                    await folder.DeleteAsync(StorageDeleteOption.PermanentDelete);
                }
                catch (FileNotFoundException) { }
                catch (DirectoryNotFoundException) { }
                DownloadPathCoordinator.CleanupSucceeded(lease);
                foreach (var failure in CleanupFailures.Where(x => DownloadPathCoordinator.Contains(target.Path, x.Path) && x.Revision <= target.Revision).ToList()) CleanupFailures.Remove(failure);
            }
        }
        private async void DeleteItem(DownloadingItem data)
        {
            try { await DeleteItemAsync(data); }
            catch (Exception ex) { LogHelper.Log("清理任务失败", LogType.ERROR, ex); Utils.ShowMessageToast("清理失败，请重试"); }
        }
        public async Task DeleteItemAsync(DownloadingItem data)
        {
            if (cleanupPending || data == null) return;
            cleanupPending = true;
            try
            {
                var target = await CaptureCleanupAsync(data.Path, data.Title + " " + data.EpisodeTitle, true);
                if (!await Utils.ShowDialog("取消并删除任务", $"确定取消《{target.Title}》并永久删除已下载的部分文件吗？")) return;
                await DownloadCleanupTargetsAsync(new[] { target });
            }
            catch (Exception ex) { LogHelper.Log("删除下载任务失败", LogType.ERROR, ex); Utils.ShowMessageToast("任务未删除，请刷新后重试"); }
            finally { cleanupPending = false; }
        }
        private async Task DownloadCleanupTargetsAsync(IEnumerable<DownloadCleanupTarget> targets)
        {
            int success = 0, failure = 0, superseded = 0;
            foreach (var target in targets)
            {
                try { await DeleteCleanupAsync(target); CleanupFailures.Remove(target); success++; }
                catch (Exception ex)
                {
                    if (DownloadPathCoordinator.CompletedRevision(target.Path) > target.Revision)
                    {
                        CleanupFailures.Remove(target); superseded++;
                        LogHelper.Log("新下载已完成，旧清理记录失效", LogType.ERROR, ex);
                        continue;
                    }
                    failure++;
                    target.FailureText = target.FailedCancellation
                        ? (target.WasCancelled ? "部分任务已取消，仍有任务取消失败，部分文件未删除" : "任务取消失败，文件未删除")
                        : (target.WasCancelled ? "任务已取消，部分文件未删除" : "清理失败，请刷新后重试");
                    if (CleanupFailures.Contains(target)) CleanupFailures.Remove(target);
                    CleanupFailures.Add(target);
                    LogHelper.Log("下载部分文件清理失败: " + target.Title, LogType.ERROR, ex);
                }
            }
            Utils.ShowMessageToast($"已清理{success}项，失败{failure}项" + (superseded > 0 ? $"，{superseded}项下载已更新，旧清理记录已失效，未删除新下载" : "") + (failure > 0 ? "，任务可能已取消，部分文件未删除，请重试清理" : ""));
        }
        private async void RetryCleanup()
        {
            if (cleanupPending) return;
            cleanupPending = true;
            try
            {
                var targets = CleanupFailures.ToList();
                if (targets.Count == 0) return;
                if (!await Utils.ShowDialog("重试清理", $"确定永久删除{targets.Count}项任务遗留的部分文件吗？")) return;
                await DownloadCleanupTargetsAsync(targets);
            }
            catch (Exception ex) { LogHelper.Log("重试清理失败", LogType.ERROR, ex); }
            finally { cleanupPending = false; }
        }

        private void StartAll() { ControlAll(false); }
        private void PauseAll() { ControlAll(true); }
        private void ControlAll(bool pause)
        {
            int failed = 0;
            foreach (var item in Downloadings.ToList())
                foreach (var child in item.Items.ToList()) if (!ControlOperation(child, pause, false)) failed++;
            if (failed > 0) Utils.ShowMessageToast($"{failed}项任务" + (pause ? "暂停失败" : "恢复失败") + "，请刷新后重试");
        }
        private async void DeleteAll()
        {
            try { await DeleteAllAsync(); }
            catch (Exception ex) { LogHelper.Log("批量清理任务失败", LogType.ERROR, ex); Utils.ShowMessageToast("清理失败，请重试"); }
        }
        public async Task DeleteAllAsync()
        {
            if (cleanupPending || Downloadings.Count == 0) return;
            cleanupPending = true;
            try
            {
                var targets = new List<DownloadCleanupTarget>();
                int blocked = 0;
                foreach (var item in Downloadings.ToList())
                {
                    try { targets.Add(await CaptureCleanupAsync(item.Path, item.Title + " " + item.EpisodeTitle, true)); }
                    catch (Exception ex) { blocked++; LogHelper.Log("下载任务无法确认清理范围", LogType.ERROR, ex); }
                }
                if (targets.Count == 0) { Utils.ShowMessageToast("任务未删除，请刷新后重试"); return; }
                if (!await Utils.ShowDialog("取消并删除任务", $"确定取消{targets.Count}项任务并永久删除部分文件吗？" + (blocked > 0 ? $"另有{blocked}项不可清理。" : ""))) return;
                await DownloadCleanupTargetsAsync(targets);
            }
            catch (Exception ex) { LogHelper.Log("批量清理失败", LogType.ERROR, ex); Utils.ShowMessageToast("清理失败，请重试"); }
            finally { cleanupPending = false; }
        }
        public async void UpdateSetting()
        {
            var downList = await BackgroundDownloader.GetCurrentDownloadsForTransferGroupAsync(DownloadHelper.group);
            var parallelDownload = SettingHelper.GetValue<bool>(SettingHelper.Download.PARALLEL_DOWNLOAD, true);
            var allowCostNetwork = SettingHelper.GetValue<bool>(SettingHelper.Download.ALLOW_COST_NETWORK, false);
            //设置下载模式
            foreach (var item in downList)
            {
                if (parallelDownload)
                {
                    item.TransferGroup.TransferBehavior = BackgroundTransferBehavior.Parallel;
                }
                else
                {
                    item.TransferGroup.TransferBehavior = BackgroundTransferBehavior.Serialized;

                }
                item.CostPolicy = allowCostNetwork ? BackgroundTransferCostPolicy.Always : BackgroundTransferCostPolicy.UnrestrictedOnly;
            }

        }
    }
    /// <summary>
    /// 正在下载列表
    /// </summary>
    public class DownloadingItem
    {
        public ICommand DeleteItemCommand { get; set; }
        public string EpisodeID { get; set; }
        public string Title { get; set; }
        public string EpisodeTitle { get; set; }
        public string Path { get; set; }

        public BitmapImage Cover { get; set; }

        /// <summary>
        /// 正在下载的分段内容
        /// </summary>
        public ObservableCollection<DownloadingSubItem> Items { get; set; }
    }

    public class DownloadingSubItem : IModules
    {
        public string GUID { get; set; }
        /// <summary>
        /// 标题
        /// </summary>

        private BackgroundTransferStatus _status;
        /// <summary>
        /// 下载状态
        /// </summary>
        public BackgroundTransferStatus Status
        {
            get { return _status; }
            set { _status = value; DoPropertyChanged("Status"); DoPropertyChanged("StatusString"); }
        }

        private ulong _TotalBytes;
        /// <summary>
        /// 文件总大小
        /// </summary>
        public ulong TotalBytes
        {
            get { return _TotalBytes; }
            set
            {
                _TotalBytes = value;
                DoPropertyChanged("TotalBytes");
                DoPropertyChanged("ShowPause");
                DoPropertyChanged("ShowStart");
            }
        }
        private ulong _ProgressBytes;
        /// <summary>
        /// 已下载大小
        /// </summary>
        public ulong ProgressBytes
        {
            get { return _ProgressBytes; }
            set { _ProgressBytes = value; DoPropertyChanged("ProgressBytes"); }
        }

        private double _Progress = 0;

        public double Progress
        {
            get { return _Progress; }
            set { _Progress = value; DoPropertyChanged("Progress"); }
        }

        public string CID { get; set; }
        public string Title { get; set; }
        public string FileName { get; set; }
        public string EpisodeTitle { get; set; }
        public string Path { get; set; }
        public bool ShowPause { get { return Status == BackgroundTransferStatus.Running; } }
        public bool ShowStart { get { return Status != BackgroundTransferStatus.Running; } }
        public ICommand PauseItemCommand { get; set; }
        public ICommand ResumeItemCommand { get; set; }
    }
    /// <summary>
    /// 已下载的列表
    /// </summary>
    public class DownloadedItem
    {
        public bool IsSeason { get; set; }
        public string ID { get; set; }
        public string CoverPath { get; set; }
        public BitmapImage Cover { get; set; }

        public string Title { get; set; }
        public DateTime UpdateTime { get; set; }
        public ObservableCollection<DownloadedSubItem> Episodes { get; set; }
        public string Path { get; set; }
    }
    public class DownloadedSubItem
    {
        public string AVID { get; set; }
        public string CID { get; set; }
        public string EpisodeID { get; set; }
        public string Title { get; set; }
        public bool IsDash { get; set; }
        public int QualityID { get; set; }
        public string QualityName { get; set; }
        public List<string> Paths { get; set; }
        public string DanmakuPath { get; set; }
        public int Index { get; set; }
        public string Path { get; set; }
        public List<DownloadSubtitleInfo> SubtitlePath { get; set; }
    }
}
