using System.Collections.ObjectModel;
using BiliLite.Helpers;
using BiliLite.Modules;
using BiliLite.Pages;
using BiliLite.Services;
using Windows.Storage;
using Windows.Networking.BackgroundTransfer;

class Capture : SynchronizationContext
{
    public int Active; public List<Exception> Errors = new(); private Queue<(SendOrPostCallback, object)> queue = new();
    public override void OperationStarted() => Active++;
    public override void OperationCompleted() => Active--;
    public override void Post(SendOrPostCallback callback, object state) { lock (queue) queue.Enqueue((callback, state)); }
    public void Run(Action action)
    {
        SetSynchronizationContext(this); action(); var deadline = DateTime.UtcNow.AddSeconds(5);
        while (Active > 0 || queue.Count > 0)
        {
            (SendOrPostCallback, object) entry = default; bool found = false;
            lock (queue) if (queue.Count > 0) { entry = queue.Dequeue(); found = true; }
            if (found) { try { entry.Item1(entry.Item2); } catch (Exception ex) { Errors.Add(ex); } }
            else Thread.Sleep(1);
            if (DateTime.UtcNow > deadline) throw new Exception("adapter timeout");
        }
        SetSynchronizationContext(null);
    }
}
class Program
{
    const string Target = "/videos/哔哩哔哩下载/av1/cid1";
    static int checks, failures;
    static void Check(bool value, string name) { checks++; if (!value) failures++; Console.WriteLine((value ? "PASS " : "FAIL ") + name); }
    static HttpResults Json(string body) => new() { status = true, results = body };
    static HttpResults Feed(string items) => Json("{\"code\":0,\"data\":{\"items\":" + items + "}}");
    static TaskCompletionSource<T> Pending<T>() => new(TaskCreationOptions.RunContinuationsAsynchronously);
    static async Task Until(Func<bool> predicate) { var deadline = DateTime.UtcNow.AddSeconds(4); while (!predicate()) { if (DateTime.UtcNow > deadline) throw new Exception("readiness timeout"); await Task.Delay(1); } }
    static async Task<bool> Fails(Func<Task> action) { try { await action(); return false; } catch { return true; } }
    static RecommendVM Seed() => new() { Items = new ObservableCollection<RecommendItemModel> { new() { idx = "10", card_goto = "av", param = "123", title = "known-good", args = new() { up_id = "1", rid = 2, tid = 3 } } }, Loading = false };
    static DownloadInfo Info(string cid = "cid1", string avid = "av1") => new() { Type = DownloadType.Video, AVID = avid, CID = cid, EpisodeTitle = cid, Title = avid, CoverUrl = "https://example.invalid/cover", DanmakuUrl = "https://example.invalid/danmaku", Urls = new() { new() { FileName = "video.m4s", Url = "https://example.invalid/video" } } };
    static (DownloadVM, DownloadingItem, DownloadOperation) SeedTransfer(string cid = "cid1", string path = Target)
    {
        var op = new DownloadOperation { Path = path + "/old.m4s" };
        BackgroundDownloader.Operations.Add(op);
        SettingHelper.SetValue(op.Guid.ToString(), new DownloadGUIDInfo { GUID = op.Guid.ToString(), CID = cid, Path = path, Title = "title-" + cid, EpisodeTitle = cid });
        var vm = new DownloadVM(); vm.LoadDownloading(); return (vm, vm.Downloadings.Single(), op);
    }
    static async Task Reset()
    {
        StorageFile.OnBasic = null; StorageFolder.OnItem = null; Utils.OnToast = null;
        foreach (var op in BackgroundDownloader.Operations.ToList()) op.Completion.TrySetResult();
        await Task.Delay(15);
        BackgroundDownloader.Operations.Clear(); BackgroundDownloader.FailEnumeration = false;
        StorageFolder.OnLookup = null; StorageFolder.OnDelete = null;
        var cleanup = new DownloadVM();
        foreach (var scope in new[] { "/videos", "/episodes", "/other", "/elsewhere", "/denied" })
            await cleanup.DeleteCleanupAsync(await cleanup.CaptureCleanupAsync(scope, "fixture teardown", false));
        StorageFolder.Reset(); BackgroundDownloader.Operations = new(); BackgroundDownloader.FailEnumeration = BackgroundDownloader.FailCreate = DownloadOperation.FailStart = false;
        SettingHelper.Values = new(); SettingHelper.OnSet = null; FileIO.OnWrite = null; FileIO.OnReadText = null; PropertySet.Fail = false;
        Utils.Confirm = (t, c) => Task.FromResult(true); Utils.Dialogs.Clear(); Utils.Toasts.Clear(); LogHelper.Entries.Clear(); FakeTransport.Requests.Clear();
        FakeTransport.Next = idx => Task.FromResult(new HttpResults { status = false, message = "offline" });
    }
    static async Task Recommendations()
    {
        await Reset(); var initial = new RecommendVM(); await initial.GetRecommend(); var capture = new Capture(); capture.Run(initial.LoadMore);
        Check(capture.Errors.Count == 0 && !initial.Loading && initial.StatusText == "推荐加载失败" && !initial.AutoLoad, "initial failure and real LoadMore are safe with persistent retry state");
        var empty = new RecommendVM(); FakeTransport.Next = _ => Task.FromResult(Feed("[]")); await empty.GetRecommend(); capture.Run(empty.LoadMore);
        Check(capture.Errors.Count == 0 && empty.Items.Count == 0 && empty.StatusText == "暂无推荐内容" && !empty.AutoLoad, "successful empty and real LoadMore are safe");
        FakeTransport.Next = _ => Task.FromResult(new HttpResults { status = false }); var append = Seed(); var rows = append.Items; var banner = append.Banner;
        FakeTransport.Requests.Clear(); capture.Run(append.LoadMore); capture.Run(append.LoadMore); capture.Run(append.LoadMore);
        Check(FakeTransport.Requests.SequenceEqual(new[] { "10", "10", "10" }), "failed append accepts each later explicit retry at idx10");
        Check(ReferenceEquals(rows, append.Items) && append.Items.Count == 1 && append.Items[0].idx == "10" && !append.AutoLoad && append.StatusText == "加载更多失败", "failed append preserves collection rows and cursor");
        capture.Run(append.Refresh); Check(ReferenceEquals(rows, append.Items) && ReferenceEquals(banner, append.Banner) && append.StatusText == "推荐刷新失败", "failed refresh retains exact known-good collection and banner");
        capture.Run(() => append.RetryCommand.Execute(null)); Check(FakeTransport.Requests.Last() == "0", "refresh retry remembers refresh mode with retained rows");
        FakeTransport.Next = _ => Task.FromResult(Feed("[{\"idx\":\"11\",\"card_goto\":\"av\",\"args\":{}}]")); capture.Run(append.LoadMore);
        Check(append.Items.Count == 2 && ReferenceEquals(rows, append.Items) && append.AutoLoad, "successful append publishes once and restores auto append");
        FakeTransport.Next = _ => Task.FromResult(Feed("[]")); capture.Run(append.LoadMore); capture.Run(append.LoadMore);
        Check(append.Items.Count == 2 && !append.AutoLoad && append.StatusText == "暂时没有更多推荐" && FakeTransport.Requests.TakeLast(2).All(x => x == "11"), "empty append suppresses automatic loops but manual same-cursor retry works");
        foreach (var body in new[] { "{\"code\":\"oops\",\"data\":{\"items\":[]}}", "{\"code\":0.0,\"data\":{\"items\":[]}}", "{\"code\":2147483648,\"data\":{\"items\":[]}}", "{\"data\":{\"items\":[]}}", "{\"code\":0,\"data\":{}}", "{\"code\":0,\"data\":{\"items\":null}}", "{\"code\":0,\"data\":{\"items\":{}}}", "{\"code\":0,\"data\":{\"items\":[null]}}", "{\"code\":0,\"data\":{\"items\":[1]}}", "{\"code\":0,\"data\":{\"items\":[{\"args\":1}]}}", "{\"code\":-1,\"message\":\"rejected\"}", "garbage" })
        {
            var vm = Seed(); var before = vm.Items; FakeTransport.Next = _ => Task.FromResult(Json(body)); capture.Run(vm.Refresh);
            Check(ReferenceEquals(before, vm.Items) && vm.StatusText == "推荐刷新失败" && !vm.Loading, "required feed failure retains rows: " + body);
        }
        var single = new RecommendVM(); var pending = Pending<HttpResults>(); FakeTransport.Requests.Clear(); FakeTransport.Next = _ => pending.Task;
        var load = single.GetRecommend(); single.Refresh(); single.RetryCommand.Execute(null); single.LoadMore();
        Check(FakeTransport.Requests.Count == 1 && single.Loading && !single.AutoLoad, "initial navigation and all commands share one request owner");
        pending.SetResult(Feed("[]")); await load;
        var filtered = new RecommendVM(); FakeTransport.Next = _ => Task.FromResult(Feed("[{\"idx\":\"99\",\"ad_info\":{\"creative_content\":{}}}]")); await filtered.GetRecommend();
        Check(filtered.Items.Count == 0 && !filtered.AutoLoad && filtered.StatusText == "暂无推荐内容", "all-filtered initial batch is successful empty without ad cursor");
        var optional = new RecommendVM(); FakeTransport.Next = _ => Task.FromResult(Feed("[{\"card_goto\":\"banner\",\"banner_item\":[{\"type\":\"static\",\"static_banner\":{\"title\":\"good\"}},{\"type\":\"static\",\"static_banner\":7}]},{\"idx\":\"5\",\"args\":{},\"left_bottom_rcmd_reason_style\":{\"text\":\"reason\"}}]")); await optional.GetRecommend();
        Check(optional.Items.Count == 1 && optional.Items[0].rcmd_reason_style.text == "reason" && optional.Banner.Count == 0, "optional banner failure is atomic and card compatibility is retained");
        FakeTransport.Next = _ => Task.FromResult(Feed("[{\"card_goto\":\"banner\",\"banner_item\":{}},{\"idx\":\"6\",\"args\":{}}]")); await optional.GetRecommend();
        Check(optional.Items.Count == 1 && optional.Items[0].idx == "6" && optional.Banner.Count == 0, "wrong-shaped optional banner cannot discard usable recommendation cards");
        FakeTransport.Next = _ => Task.FromResult(Json("prefix {\"code\":0,\"data\":{\"items\":[]}} suffix")); await optional.GetRecommend();
        Check(optional.Items.Count == 0 && optional.StatusText == "暂无推荐内容", "actual HttpResults embedded-object salvage policy is preserved");
        var feedback = Seed(); FakeTransport.Next = _ => Task.FromResult(Json("{\"code\":0}")); await feedback.Dislike("10", new() { type = "feedback" }, new() { id = 91 });
        Check(FakeTransport.Last.parameter.Contains("feedback_id=91") && !FakeTransport.Last.parameter.Contains("reason_id=") && feedback.Items.Count == 0, "feedback uses returned real builder and accepts code0 without data");
        foreach (var body in new[] { "{\"code\":\"oops\"}", "{\"code\":-1}" }) { feedback = Seed(); FakeTransport.Next = _ => Task.FromResult(Json(body)); await feedback.Dislike("10", new() { type = "dislike" }, null); Check(feedback.Items.Count == 1, "failed action preserves row: " + body); }
        DownloadVM.Instance.Downloadings.Clear(); DownloadVM.Instance.Downloadings.Add(new() { EpisodeID = "23456" });
        Check(SeasonCaller.CurrentState(new() { id = 12345, cid = "23456" }) == 2, "actual season caller recognizes CID while completed predicate retains episode id");
    }
    static async Task DownloadReadsAndTasks()
    {
        await Reset(); var vm = new DownloadVM(); vm.Downloadeds.Add(new() { Title = "existing" }); var before = vm.Downloadeds;
        StorageFolder.OnLookup = _ => Task.FromException<StorageFolder>(new UnauthorizedAccessException()); SettingHelper.Values[SettingHelper.Download.DOWNLOAD_PATH] = "/denied";
        var capture = new Capture(); capture.Run(vm.LoadDownloaded);
        Check(capture.Errors.Count == 0 && !vm.LoadingDownloaded && ReferenceEquals(before, vm.Downloadeds) && vm.DownloadedStatus.Contains("失败"), "root scan failure is caught and resets busy preserving exact rows");
        StorageFolder.OnLookup = null; capture.Run(vm.RefreshDownloaded);
        Check(!vm.LoadingDownloaded && vm.Downloadeds.Count == 0 && vm.DownloadedStatus == "暂无离线视频", "root failure permits real refresh retry to successful empty");
        PropertySet.Fail = true; await vm.LoadDownloadedAsync();
        Check(!vm.DiskAvailable && vm.DiskFree == 0 && vm.DownloadedStatus.Contains("部分"), "optional disk failure retains successful scan and marks figures unavailable");
        PropertySet.Fail = false; SettingHelper.Values[SettingHelper.Download.LOAD_OLD_DOWNLOAD] = true;
        StorageFolder.OnFolders = path => path.EndsWith("BiliBiliDownload") ? Task.FromException<List<StorageFolder>>(new Exception("legacy")) : Task.FromResult(new List<StorageFolder>());
        await vm.LoadDownloadedAsync(); Check(vm.DownloadedStatus.Contains("部分") && vm.Downloadeds.Count == 0, "legacy root failure is partial and does not discard current scan");
        SettingHelper.Values.Remove(SettingHelper.Download.LOAD_OLD_DOWNLOAD); StorageFolder.OnFolders = null;
        var root = await StorageFolder.GetFolderFromPathAsync("/denied"); var title = await root.CreateFolderAsync("title", CreationCollisionOption.OpenIfExists);
        title.Files["info.json"] = new() { Path = title.Path + "/info.json", Text = "{\"Cover\":\"cover\",\"Title\":\"good\",\"ID\":\"1\"}" };
        var good = await title.CreateFolderAsync("good", CreationCollisionOption.OpenIfExists); good.Files["info.json"] = new() { Path = good.Path + "/info.json", Text = "{\"CID\":\"1\",\"Index\":2,\"VideoPath\":[\"video.mp4\"]}" }; good.Files["video.mp4"] = new() { Path = good.Path + "/video.mp4" };
        var bad = await title.CreateFolderAsync("bad", CreationCollisionOption.OpenIfExists); bad.Files["info.json"] = new() { Path = bad.Path + "/info.json", Text = "invalid" }; bad.Files["video.mp4"] = new() { Path = bad.Path + "/video.mp4" };
        await vm.LoadDownloadedAsync(); Check(vm.Downloadeds.Count == 1 && vm.Downloadeds[0].Episodes.Count == 1 && vm.DownloadedStatus.Contains("部分"), "malformed episode is skipped without losing valid sibling and partial warning");
        var folders = Pending<List<StorageFolder>>(); StorageFolder.OnFolders = _ => folders.Task; var scanning = vm.LoadDownloadedAsync(); var retained = vm.Downloadeds; var second = vm.LoadDownloadedAsync();
        Check(second.IsCompleted && ReferenceEquals(retained, vm.Downloadeds) && vm.LoadingDownloaded, "duplicate scan does not clear or republish while pending"); folders.SetResult(new()); await scanning;
        await Reset(); var (tasks, row, op) = SeedTransfer(); tasks.LoadDownloading();
        Check(op.AttachCount == 1 && op.AttachedToken.CanBeCanceled, "repeated transfer scan reuses actual per-GUID observer token");
        Utils.Confirm = (t, c) => Task.FromResult(false); capture.Run(() => tasks.DeleteItemCommand.Execute(row));
        Check(!op.CancellationSignaled && StorageFolder.Deleted.Count == 0, "declining task deletion leaves token and folder intact");
        Utils.Confirm = (t, c) => Task.FromResult(true); capture.Run(() => tasks.DeleteItemCommand.Execute(row));
        Check(Utils.Dialogs.Last().Contains("永久") && Utils.Dialogs.Last().Contains("title-cid1"), "task confirmation names target and permanent partial-file deletion");
        Check(op.AttachedToken.IsCancellationRequested && StorageFolder.Deleted.SequenceEqual(new[] { Target }), "confirmed cancellation reaches actual attached token and only episode folder");
        await Reset(); (tasks, row, op) = SeedTransfer("A", "/episodes/A"); var b = new DownloadOperation { Path = "/episodes/B/b.m4s" }; BackgroundDownloader.Operations.Add(b); SettingHelper.SetValue(b.Guid.ToString(), new DownloadGUIDInfo { GUID = b.Guid.ToString(), CID = "B", Path = "/episodes/B", Title = "B" }); tasks.LoadDownloading();
        StorageFolder.OnDelete = path => path.EndsWith("A") ? Task.FromException(new UnauthorizedAccessException("locked")) : Task.CompletedTask;
        capture.Run(() => tasks.DeleteCommand.Execute(null)); Check(StorageFolder.Deleted.SequenceEqual(new[] { "/episodes/B" }), "batch continues after first folder failure");
        Check(Utils.Toasts.Count == 1 && Utils.Toasts[0].Contains("失败1") && LogHelper.Entries.Count > 0, "mixed batch reports one truthful summary and logs failure");
        Check(tasks.CleanupFailures.Count == 1, "cancellation completion before failed delete retains independent cleanup target");
        StorageFolder.OnDelete = null; capture.Run(() => tasks.RetryCleanupCommand.Execute(null));
        Check(tasks.CleanupFailures.Count == 0 && StorageFolder.Deleted.Contains("/episodes/A"), "cleanup retry uses retained immutable target after active row retired");
        await Reset(); (tasks, row, op) = SeedTransfer(); op.ThrowPause = true; tasks.PauseItemCommand.Execute(row.Items[0]); Check(Utils.Toasts.Count == 1 && LogHelper.Entries.Count > 0, "failed pause is logged and visibly reported");
        op.ThrowResume = true; tasks.ResumeItemCommand.Execute(row.Items[0]); Check(Utils.Toasts.Count == 2, "failed resume is visibly reported");
        var prior = tasks.Downloadings; BackgroundDownloader.FailEnumeration = true; tasks.LoadDownloading(); Check(ReferenceEquals(prior, tasks.Downloadings) && op.AttachCount == 1, "failed scan retains active rows and original token ownership");
        BackgroundDownloader.FailEnumeration = false; BackgroundDownloader.Operations.Add(new()); tasks.LoadDownloading(); Check(ReferenceEquals(prior, tasks.Downloadings), "unknown operation metadata fails discovery closed retaining prior rows");
    }
    static async Task CreatorAndCleanup()
    {
        await Reset(); var (vm, row, old) = SeedTransfer(); var file = Pending<StorageFile>(); StorageFolder.OnCreateFile = path => path == Target + "/video.m4s" ? file.Task : Task.FromResult(new StorageFile { Path = path });
        var creation = DownloadHelper.AddDownload(Info()); await Until(() => StorageFolder.Events.Contains("create-file:" + Target + "/video.m4s"));
        Check(!creation.IsCompleted && BackgroundDownloader.Operations.Count == 1, "actual AddDownload awaits child preparation before GUID publication");
        var capture = new Capture(); capture.Run(() => vm.DeleteItemCommand.Execute(row));
        Check(!old.CancellationSignaled && StorageFolder.Deleted.Count == 0, "same-path pre-GUID creator excludes cleanup before cancellation");
        var other = DownloadHelper.AddDownload(Info("other")); await other; Check(other.IsCompleted, "different episode preparation progresses independently");
        file.SetResult(new() { Path = Target + "/video.m4s" }); await creation; var started = BackgroundDownloader.Operations.Last(x => x.Path.StartsWith(Target + "/"));
        Check(started.Started.Task.IsCompleted && !started.Completion.Task.IsCompleted, "StartAsync dispatch precedes preparation release and media lifetime remains detached");
        using (DownloadPathCoordinator.Acquire(Target)) Check(true, "preparation lease releases while background transfer remains pending");
        foreach (var phase in new[] { "lookup", "delete" })
        {
            await Reset(); (vm, row, old) = SeedTransfer(); var held = Pending<bool>();
            if (phase == "lookup") StorageFolder.OnLookup = async path => { if (path == Target) await held.Task; return new(path); };
            else StorageFolder.OnDelete = async path => { if (path == Target) await held.Task; };
            vm.DeleteItemCommand.Execute(row); await Until(() => StorageFolder.Events.Contains((phase == "lookup" ? "lookup:" : "delete-enter:") + Target));
            var events = StorageFolder.Events.Count; Check(await Fails(() => DownloadHelper.AddDownload(Info())), "actual creator is refused during awaited cleanup " + phase);
            Check(StorageFolder.Events.Skip(events).All(x => !x.StartsWith("create-dir:" + Target) && !x.StartsWith("create-file:" + Target)), "busy creator performs no target mutation during " + phase);
            held.SetResult(true); await Until(() => StorageFolder.Deleted.Contains(Target)); using (DownloadPathCoordinator.Acquire(Target)) Check(true, "cleanup " + phase + " releases ownership");
        }
        foreach (var retire in new[] { false, true })
        {
            await Reset(); (vm, row, old) = SeedTransfer(); var confirm = Pending<bool>(); Utils.Confirm = (t, c) => confirm.Task; vm.DeleteItemCommand.Execute(row);
            await DownloadHelper.AddDownload(Info()); var newer = BackgroundDownloader.Operations.Last(); vm.LoadDownloading();
            if (retire) { newer.Completion.TrySetResult(); BackgroundDownloader.Operations.Remove(newer); await Task.Delay(10); }
            confirm.SetResult(true); await Until(() => Utils.Toasts.Count > 0);
            Check(!newer.CancellationSignaled && !old.CancellationSignaled && StorageFolder.Deleted.Count == 0, "intervening creator invalidates confirmation even after creator retired=" + retire);
        }
        await Reset(); (vm, row, old) = SeedTransfer(); var snapshot = await vm.CaptureCleanupAsync(Target, "old", true); StorageFolder.OnDelete = _ => Task.FromException(new Exception("locked"));
        Check(await Fails(() => vm.DeleteCleanupAsync(snapshot)), "cleanup storage failure is propagated after cancellation");
        using (DownloadPathCoordinator.Acquire(Target)) Check(true, "failed cleanup releases exclusive ownership");
        StorageFolder.OnDelete = null; await DownloadHelper.AddDownload(Info()); Check(await Fails(() => vm.DeleteCleanupAsync(snapshot)) && StorageFolder.Deleted.Count == 0, "retry preserves old revision and cannot delete new generation");
        await Reset(); (vm, row, old) = SeedTransfer(); snapshot = await vm.CaptureCleanupAsync(Target, "old", true); BackgroundDownloader.FailEnumeration = true;
        Check(await Fails(() => vm.DeleteCleanupAsync(snapshot)) && !old.CancellationSignaled && StorageFolder.Deleted.Count == 0, "failed final reconciliation returns before any cancellation or deletion"); BackgroundDownloader.FailEnumeration = false;
        BackgroundDownloader.Operations.Add(new()); Check(await Fails(() => vm.DeleteCleanupAsync(snapshot)) && !old.CancellationSignaled, "unknown GUID metadata fails cleanup closed");
        await Reset(); (vm, row, old) = SeedTransfer(); snapshot = await vm.CaptureCleanupAsync(Target, "old", true); var sameCid = new DownloadOperation { Path = "/other/cid1/video.m4s" }; BackgroundDownloader.Operations.Add(sameCid); SettingHelper.SetValue(sameCid.Guid.ToString(), new DownloadGUIDInfo { CID = "cid1", GUID = sameCid.Guid.ToString(), Path = "/other/cid1" }); vm.LoadDownloading(); await vm.DeleteCleanupAsync(snapshot);
        Check(!sameCid.CancellationSignaled && old.CancellationSignaled, "same-CID GUID at another path never inherits confirmed source cancellation");
        foreach (var mode in new[] { "file", "operation", "metadata", "manifest", "start" })
        {
            await Reset(); var info = Info(); info.Urls.Add(new() { FileName = "second.m4s", Url = "https://example.invalid/second" });
            StorageFolder.OnCreateFile = path => path.EndsWith("video.m4s") && mode == "file" ? Task.FromException<StorageFile>(new Exception("required file")) : Task.FromResult(new StorageFile { Path = path });
            BackgroundDownloader.FailCreate = mode == "operation"; DownloadOperation.FailStart = mode == "start";
            SettingHelper.OnSet = (key, value) => { if (mode == "metadata") throw new Exception("metadata"); };
            FileIO.OnWrite = f => mode == "manifest" && f.Path.EndsWith("info.json") ? Task.FromException(new Exception("manifest")) : Task.CompletedTask;
            Check(await Fails(() => DownloadHelper.AddDownload(info)), "required preparation failure reaches awaited caller: " + mode);
            using (DownloadPathCoordinator.Acquire(Target)) Check(true, "preparation failure releases path: " + mode);
            if (mode == "file") Check(BackgroundDownloader.Operations.Count == 1 && BackgroundDownloader.Operations[0].Started.Task.IsCompleted, "successful sibling is published and started despite other required file failure");
        }
        await Reset(); FileIO.OnWrite = f => !f.Path.EndsWith("info.json") ? Task.FromException(new Exception("optional")) : Task.CompletedTask; await DownloadHelper.AddDownload(Info());
        Check(BackgroundDownloader.Operations.Count == 1 && LogHelper.Entries.Count >= 2, "optional cover and danmaku failures retain prepared video task");
        foreach (var name in new[] { "../escape", "/rooted", "bad\\child", "..", "bad:", "trailing.", "bad?" })
        {
            await Reset(); var info = Info(name); Check(await Fails(() => DownloadHelper.AddDownload(info)) && !StorageFolder.Events.Any(x => x.StartsWith("create-file:") || x.StartsWith("create-dir:/videos/哔哩哔哩下载/av1")), "invalid child rejected before target mutation: " + name);
        }
        Check(DownloadPathCoordinator.Contains(@"C:\Videos\Title", @"c:/videos/title/episode") && !DownloadPathCoordinator.Contains(@"C:\Videos\Title", @"C:\Videos\Title2"), "path normalization handles Windows case separators and ancestor boundaries");
    }
    static async Task CompletedPageDeletion()
    {
        foreach (var titleScope in new[] { false, true })
        {
            await Reset(); var vm = new DownloadVM(); var episode = new DownloadedSubItem { Path = Target, Title = "episode" }; var parent = new DownloadedItem { Path = "/videos/哔哩哔哩下载/av1", Title = "title", Episodes = new() { episode } }; vm.Downloadeds.Add(parent); var page = new DownloadPage { downloadVM = vm }; var capture = new Capture(); Action delete = () => { if (titleScope) page.DeleteTitle(parent); else page.DeleteEpisode(parent, episode); };
            Utils.Confirm = (_, _) => Task.FromResult(false); capture.Run(delete); Check(StorageFolder.Deleted.Count == 0 && parent.Episodes.Count == 1 && vm.Downloadeds.Count == 1, "actual completed handler decline preserves files and rows title=" + titleScope);
            Utils.Confirm = (_, _) => Task.FromResult(true); StorageFolder.OnDelete = _ => Task.FromException(new Exception("locked")); capture.Run(delete); Check(parent.Episodes.Count == 1 && vm.Downloadeds.Count == 1 && Utils.Toasts.Count > 0, "actual completed handler storage failure retains rows title=" + titleScope);
            StorageFolder.OnDelete = null; capture.Run(delete); Check((titleScope ? vm.Downloadeds.Count == 0 : parent.Episodes.Count == 0) && StorageFolder.Deleted.Count == 1, "actual completed handler success removes intended row title=" + titleScope);
            await Reset(); vm = new DownloadVM(); parent.Episodes = new() { episode }; vm.Downloadeds.Add(parent); page.downloadVM = vm;
            var confirm = Pending<bool>(); Utils.Confirm = (_, _) => confirm.Task; delete(); await DownloadHelper.AddDownload(Info(titleScope ? "new-child" : "cid1")); var newOp = BackgroundDownloader.Operations.Single(); newOp.Completion.TrySetResult(); BackgroundDownloader.Operations.Clear(); confirm.SetResult(true); await Until(() => Utils.Toasts.Count > 0);
            Check(StorageFolder.Deleted.Count == 0 && parent.Episodes.Count == 1 && vm.Downloadeds.Count == 1, "actual completed handler rejects intervening child revision title=" + titleScope);
            await Reset(); vm = new DownloadVM(); page.downloadVM = vm; parent.Episodes = new() { episode }; vm.Downloadeds.Add(parent);
            using (DownloadPathCoordinator.Acquire(titleScope ? parent.Path + "/unlisted-child" : episode.Path, creation: true)) { capture.Run(delete); Check(StorageFolder.Deleted.Count == 0 && Utils.Dialogs.Count == 0, "completed subtree snapshot refuses pending creator before confirmation title=" + titleScope); }
            await Reset(); vm = new DownloadVM(); page.downloadVM = vm; parent.Episodes = new() { episode }; vm.Downloadeds.Add(parent); var running = new DownloadOperation(); BackgroundDownloader.Operations.Add(running); SettingHelper.SetValue(running.Guid.ToString(), new DownloadGUIDInfo { GUID = running.Guid.ToString(), CID = "cid1", Path = Target }); capture.Run(delete);
            Check(!running.CancellationSignaled && StorageFolder.Deleted.Count == 0, "completed handler never cancels overlapping running task title=" + titleScope);
        }
    }
    static async Task AcceptanceEdges()
    {
        new RecommendPage().Top(); Check(true, "actual recommendation top button is safe on empty host");
        await Reset(); var capture = new Capture();
        var banner = new RecommendVM(); SettingHelper.Values["dontloadbanner"] = true;
        FakeTransport.Next = _ => Task.FromResult(Feed("[{\"card_goto\":\"banner\",\"banner_item\":[{\"type\":\"static\",\"static_banner\":{\"title\":\"hidden\"}}]},{\"idx\":\"7\",\"card_goto\":\"av\",\"three_point_v2\":[{\"type\":\"dislike\"}],\"args\":{}},{\"idx\":null,\"args\":{}}]")); await banner.GetRecommend();
        Check(banner.Banner.Count == 0 && banner.Items[0].three_point_v2.Any(x => x.type == "browser") && banner.AutoLoad, "disabled banner and browser menu preserved with last usable visible cursor");
        FakeTransport.Next = _ => Task.FromResult(Feed("[]")); capture.Run(banner.LoadMore); Check(FakeTransport.Requests.Last() == "7", "continuation comes from last committed usable visible row");
        FakeTransport.Next = _ => Task.FromResult(Json("{\"code\":9999999999999999999999999999999999,\"data\":{\"items\":[]}}")); var before = banner.Items; capture.Run(banner.Refresh); Check(ReferenceEquals(before, banner.Items), "BigInteger business code rejects safely");
        var action = Seed(); FakeTransport.Next = _ => Task.FromResult(Json("{\"code\":0}")); await action.Dislike("10", new() { type = "dislike" }, new() { id = 72 }); Check(FakeTransport.Last.parameter.Contains("reason_id=72") && action.Items.Count == 0, "ordinary dislike keeps reason_id request and acknowledgement contract");
        await Reset(); var (vm, row, op) = SeedTransfer(); vm.LoadDownloading(); vm.LoadDownloading(); Check(op.AttachCount == 1, "three scans still own one observer per GUID");
        var sibling = new DownloadOperation { Path = Target + "/audio.m4s" }; var elsewhere = new DownloadOperation { Path = "/elsewhere/other/video.m4s" };
        foreach (var pair in new[] { (sibling, "cid1", Target), (elsewhere, "other", "/elsewhere/other") }) { BackgroundDownloader.Operations.Add(pair.Item1); SettingHelper.SetValue(pair.Item1.Guid.ToString(), new DownloadGUIDInfo { GUID = pair.Item1.Guid.ToString(), CID = pair.Item2, Path = pair.Item3 }); }
        vm.LoadDownloading(); row = vm.Downloadings.Single(x => x.Path == Target); capture.Run(() => vm.DeleteItemCommand.Execute(row));
        Check(op.CancellationSignaled && sibling.CancellationSignaled && !elsewhere.CancellationSignaled, "confirmed multi-segment/DASH sibling set cancels together excluding other CID");
        await Reset(); (vm, row, op) = SeedTransfer(); op.DeferCancellation = true; capture.Run(() => vm.DeleteItemCommand.Execute(row)); await DownloadHelper.AddDownload(Info()); var fresh = BackgroundDownloader.Operations.Last(); vm.LoadDownloading();
        Check(fresh.AttachedToken.CanBeCanceled && !fresh.AttachedToken.IsCancellationRequested && fresh.AttachedToken != op.AttachedToken, "new generation gets fresh actual per-GUID source after cancelled generation");
        op.Completion.TrySetCanceled(); BackgroundDownloader.Operations.Remove(op); await Until(() => vm.Downloadings.SelectMany(x => x.Items).All(x => x.GUID != op.Guid.ToString()));
        Check(vm.Downloadings.SelectMany(x => x.Items).Any(x => x.GUID == fresh.Guid.ToString()) && !fresh.AttachedToken.IsCancellationRequested, "late old observer completion cannot remove or cancel new generation row");
        var toastCount = Utils.Toasts.Count; vm.PauseItemCommand.Execute(new DownloadingSubItem { GUID = "missing" }); vm.ResumeItemCommand.Execute(new DownloadingSubItem { GUID = "missing" }); Check(Utils.Toasts.Count == toastCount, "stale missing pause/resume is harmless");
        fresh.ThrowPause = true; var other = new DownloadOperation(); BackgroundDownloader.Operations.Add(other); SettingHelper.SetValue(other.Guid.ToString(), new DownloadGUIDInfo { GUID = other.Guid.ToString(), CID = "other", Path = "/other" }); vm.LoadDownloading(); Utils.Toasts.Clear(); vm.PauseCommand.Execute(null);
        Check(Utils.Toasts.Count == 1 && other.Progress.Status == BackgroundTransferStatus.PausedByApplication, "bulk pause continues good operations and summarizes failure once");
        fresh.ThrowResume = true; Utils.Toasts.Clear(); vm.StartCommand.Execute(null); Check(Utils.Toasts.Count == 1 && other.Progress.Status == BackgroundTransferStatus.Running, "bulk resume continues good operations and summarizes failure once");
        await Reset(); (vm, row, op) = SeedTransfer(); var confirm = Pending<bool>(); Utils.Confirm = (_, _) => confirm.Task; var deleting = vm.DeleteItemAsync(row); var duplicate = vm.DeleteAllAsync(); var duplicateOne = vm.DeleteItemAsync(row);
        Check(Utils.Dialogs.Count == 1 && duplicate.IsCompleted && duplicateOne.IsCompleted && StorageFolder.Deleted.Count == 0, "duplicate single/bulk cleanup schedules one pending confirmation");
        confirm.SetResult(false); await deleting; Check(!op.CancellationSignaled && StorageFolder.Deleted.Count == 0, "declined duplicate-protected cleanup has zero mutation");
        Utils.Confirm = (_, _) => Task.FromException<bool>(new Exception("dialog")); await vm.DeleteItemAsync(row); Utils.Confirm = (_, _) => Task.FromResult(false); var count = Utils.Dialogs.Count; await vm.DeleteItemAsync(row); Check(Utils.Dialogs.Count == count + 1 && !op.CancellationSignaled, "dialog exception releases command gate");
        Utils.Confirm = (_, _) => Task.FromResult(true); op.ThrowCancel = true; await vm.DeleteItemAsync(row);
        Check(StorageFolder.Deleted.Count == 0 && vm.CleanupFailures.Count == 1, "cancellation failure blocks storage deletion and retains cleanup failure"); op.ThrowCancel = false;
        await Reset(); (vm, row, op) = SeedTransfer(); StorageFolder.OnLookup = _ => Task.FromException<StorageFolder>(new FileNotFoundException()); await vm.DeleteItemAsync(row); Check(vm.CleanupFailures.Count == 0 && Utils.Toasts.Last().Contains("已清理1"), "already absent folder is successful cleanup after ownership validation");
        foreach (var phase in new[] { "lookup", "delete" })
        {
            await Reset(); (vm, row, op) = SeedTransfer(); StorageFolder.OnDelete = _ => Task.FromException(new UnauthorizedAccessException()); await vm.DeleteItemAsync(row); var failed = vm.CleanupFailures.Single(); var held = Pending<bool>(); StorageFolder.OnDelete = null; StorageFolder.Events.Clear();
            if (phase == "lookup") StorageFolder.OnLookup = async path => { await held.Task; return new(path); };
            else StorageFolder.OnDelete = async path => await held.Task;
            vm.RetryCleanupCommand.Execute(null); await Until(() => StorageFolder.Events.Contains((phase == "lookup" ? "lookup:" : "delete-enter:") + Target));
            Check(await Fails(() => DownloadHelper.AddDownload(Info())), "retained cleanup retry excludes actual creator during " + phase); held.SetResult(true); await Until(() => vm.CleanupFailures.Count == 0);
        }
        await Reset(); (vm, row, op) = SeedTransfer(); var snap = await vm.CaptureCleanupAsync(Target, "old", true); var unsnapshotted = new DownloadOperation(); BackgroundDownloader.Operations.Add(unsnapshotted); SettingHelper.SetValue(unsnapshotted.Guid.ToString(), new DownloadGUIDInfo { GUID = unsnapshotted.Guid.ToString(), CID = "cid1", Path = Target }); vm.LoadDownloading();
        Check(await Fails(() => vm.DeleteCleanupAsync(snap)) && !op.CancellationSignaled && !unsnapshotted.CancellationSignaled, "unsnapshotted same-path GUID fails final ownership check before cancellation");
        await Reset(); (vm, row, op) = SeedTransfer(); confirm = Pending<bool>(); Utils.Confirm = (_, _) => confirm.Task; deleting = vm.DeleteItemAsync(row); FileIO.OnWrite = f => f.Path.EndsWith("info.json") ? Task.FromException(new Exception("manifest")) : Task.CompletedTask;
        Check(await Fails(() => DownloadHelper.AddDownload(Info())), "intervening creator may fail required manifest before confirmation resumes"); foreach (var created in BackgroundDownloader.Operations.Where(x => x != op).ToList()) { created.Completion.TrySetResult(); BackgroundDownloader.Operations.Remove(created); } confirm.SetResult(true); await deleting;
        Check(!op.CancellationSignaled && StorageFolder.Deleted.Count == 0, "failed and retired intervening creator still invalidates old revision");
        await Reset(); var info = Info(); info.Urls.Add(new() { FileName = "second.m4s", Url = "https://example.invalid/second" }); var secondFile = Pending<StorageFile>(); StorageFolder.OnCreateFile = path => path.EndsWith("video.m4s") ? Task.FromException<StorageFile>(new Exception("first file")) : path.EndsWith("second.m4s") ? secondFile.Task : Task.FromResult(new StorageFile { Path = path }); var prepare = DownloadHelper.AddDownload(info);
        Check(!prepare.IsCompleted && await Fails(() => Task.Run(() => { using (DownloadPathCoordinator.Acquire(Target)) { } })), "failed child cannot release preparation while sibling remains pending");
        secondFile.SetResult(new() { Path = Target + "/second.m4s" }); Check(await Fails(() => prepare) && BackgroundDownloader.Operations.Single().Started.Task.IsCompleted, "all siblings settle and successful sibling remains discoverable before failure reaches caller");
        await Reset(); (vm, row, op) = SeedTransfer(); var previous = vm.Downloadeds; StorageFolder.OnFolders = _ => Task.FromException<List<StorageFolder>>(new UnauthorizedAccessException()); await vm.LoadDownloadedAsync(); Check(ReferenceEquals(previous, vm.Downloadeds) && !vm.LoadingDownloaded, "required root enumeration failure preserves exact prior collection and resets busy");
        await Reset(); var legacyRoot = await KnownFolders.VideosLibrary.CreateFolderAsync("BiliBiliDownload", CreationCollisionOption.OpenIfExists); var legacyTitle = await legacyRoot.CreateFolderAsync("title", CreationCollisionOption.OpenIfExists);
        legacyTitle.Files["info.json"] = new() { Path = legacyTitle.Path + "/info.json", Text = "{\"thumb\":\"thumb\",\"id\":\"1\",\"title\":\"legacy\",\"type\":\"video\"}" };
        foreach (var valid in new[] { true, false })
        {
            var child = await legacyTitle.CreateFolderAsync(valid ? "good" : "bad", CreationCollisionOption.OpenIfExists);
            child.Files["info.json"] = new() { Path = child.Path + "/info.json", Text = valid ? "{\"cid\":\"1\",\"index\":1,\"title\":\"old\"}" : "malformed" };
            child.Files["video.flv"] = new() { Path = child.Path + "/video.flv" };
        }
        SettingHelper.Values[SettingHelper.Download.LOAD_OLD_DOWNLOAD] = true; var legacyVM = new DownloadVM(); await legacyVM.LoadDownloadedAsync();
        Check(legacyVM.Downloadeds.Count == 1 && legacyVM.Downloadeds[0].Episodes.Count == 1 && legacyVM.DownloadedStatus.Contains("部分"), "legacy valid media and extension remain readable when malformed sibling is skipped with warning");
        // Source wiring assertions complement executed bodies; they do not certify XAML compilation/rendering.
        var root = new DirectoryInfo(AppContext.BaseDirectory); while (root != null && !Directory.Exists(Path.Combine(root.FullName, "src/BiliLite.UWP"))) root = root.Parent;
        var recXaml = File.ReadAllText(Path.Combine(root.FullName, "src/BiliLite.UWP/Pages/Home/RecommendPage.xaml")); var downXaml = File.ReadAllText(Path.Combine(root.FullName, "src/BiliLite.UWP/Pages/DownloadPage.xaml"));
        System.Xml.Linq.XDocument.Parse(recXaml); System.Xml.Linq.XDocument.Parse(downXaml);
        Check(recXaml.Contains("recommendVM.AutoLoad") && recXaml.Contains("recommendVM.StatusText") && recXaml.Contains("recommendVM.RetryCommand"), "recommendation progress/error/empty/retry and automatic eligibility wiring is present");
        Check(downXaml.Contains("downloadVM.CleanupFailures") && downXaml.Contains("FailureText") && downXaml.Contains("btnCleanupOpen_Click") && downXaml.Contains("RetryCleanupCommand") && downXaml.Contains("DownloadedStatus"), "download read state and named retained cleanup/open-folder/retry wiring is present");
        var season = File.ReadAllText(Path.Combine(root.FullName, "src/BiliLite.UWP/Pages/SeasonDetailPage.xaml.cs")); Check(season.Contains("y.EpisodeID == item.id.ToString()"), "completed season episode-id predicate remains unchanged");
    }
    static async Task FinalAcceptance()
    {
        await Reset(); var info = Info(); info.Urls.Add(new() { FileName = "audio.m4s", Url = "https://example.invalid/audio" }); bool first = true;
        SettingHelper.OnSet = (_, _) => { if (first) { first = false; throw new Exception("first metadata save"); } };
        Check(await Fails(() => DownloadHelper.AddDownload(info)), "partial required metadata failure reaches caller while sibling preparation settles");
        var vm = new DownloadVM(); vm.LoadDownloading();
        Check(vm.Downloadings.SelectMany(x => x.Items).Count() == 1 && BackgroundDownloader.Operations.Last().Started.Task.IsCompleted, "successfully published sibling remains discoverable alongside failed GUID metadata");
        await Reset(); var (tasks, row, op) = SeedTransfer(); StorageFolder.OnDelete = _ => Task.FromException(new Exception("locked")); await tasks.DeleteItemAsync(row); var target = tasks.CleanupFailures.Single(); StorageFolder.OnDelete = null; StorageFolder.Events.Clear();
        Utils.Confirm = (_, _) => Task.FromResult(false); var capture = new Capture(); capture.Run(() => tasks.RetryCleanupCommand.Execute(null));
        Check(tasks.CleanupFailures.Contains(target) && StorageFolder.Events.Count == 0 && Utils.Dialogs.Last().Contains("永久"), "cleanup retry reconfirms effect and decline preserves exact failure without storage mutation");
        Utils.Confirm = (_, _) => Task.FromResult(true); capture.Run(() => tasks.RetryCleanupCommand.Execute(null)); Check(tasks.CleanupFailures.Count == 0, "declined retry releases command gate for later confirmed recovery");
        await Reset(); info = Info(); info.Subtitles = new() { new() { Name = "Chinese", Url = "//example.invalid/subtitle" } }; FileIO.OnWrite = f => f.Path.EndsWith("info.json") ? Task.CompletedTask : Task.FromException(new Exception("auxiliary")); await DownloadHelper.AddDownload(info);
        Check(LogHelper.Entries.Count >= 3 && BackgroundDownloader.Operations.Single().Started.Task.IsCompleted, "optional subtitle/cover/danmaku failures remain nonfatal");
        foreach (var name in new[] { "CON", "aux.json", "COM1", "bad\u0001name" }) { await Reset(); info = Info(name); Check(await Fails(() => DownloadHelper.AddDownload(info)), "Windows-invalid child name is refused: " + name.Replace("\u0001", "[control]")); }
        await Reset(); (tasks, row, op) = SeedTransfer(); Utils.Confirm = (_, _) => Task.FromException<bool>(new Exception("report dialog")); op.Completion.TrySetException(new Exception("native transfer error")); BackgroundDownloader.Operations.Remove(op);
        await Until(() => tasks.Downloadings.Count == 0); Check(LogHelper.Entries.Contains("下载错误提示失败"), "detached actual GUID observer contains reporting-dialog failure and still retires its own row");
        foreach (var phase in new[] { "lookup", "delete" })
        {
            await Reset(); var parent = new DownloadedItem { Path = "/videos/哔哩哔哩下载/av1", Title = "title", Episodes = new() }; vm = new DownloadVM(); vm.Downloadeds.Add(parent); var page = new DownloadPage { downloadVM = vm }; var held = Pending<bool>();
            if (phase == "lookup") StorageFolder.OnLookup = async path => { await held.Task; return new(path); }; else StorageFolder.OnDelete = async _ => await held.Task;
            page.DeleteTitle(parent); await Until(() => StorageFolder.Events.Contains((phase == "lookup" ? "lookup:" : "delete-enter:") + parent.Path));
            Check(await Fails(() => DownloadHelper.AddDownload(Info("unlisted-child"))), "actual completed title handler excludes unseen descendant creator through awaited " + phase);
            held.SetResult(true); await Until(() => vm.Downloadeds.Count == 0);
        }
    }
    static async Task<StorageFolder> MediaFixture(string cid = "cid1", string avid = "av1")
    {
        var root = await KnownFolders.VideosLibrary.CreateFolderAsync("哔哩哔哩下载", CreationCollisionOption.OpenIfExists);
        var title = await root.CreateFolderAsync(avid, CreationCollisionOption.OpenIfExists);
        title.Files["info.json"] = new() { Path = title.Path + "/info.json", Text = Newtonsoft.Json.JsonConvert.SerializeObject(new DownloadSaveInfo { ID = avid, Title = avid, Cover = "cover" }) };
        var episode = await title.CreateFolderAsync(cid, CreationCollisionOption.OpenIfExists);
        episode.Files["info.json"] = new() { Path = episode.Path + "/info.json", Text = Newtonsoft.Json.JsonConvert.SerializeObject(new DownloadSaveEpisodeInfo { CID = cid, EpisodeID = cid, EpisodeTitle = cid, VideoPath = new() { "video.m4s" } }) };
        episode.Files["video.m4s"] = new() { Path = episode.Path + "/video.m4s", Size = 1 };
        return episode;
    }
    static void RemoveMedia(string path)
    {
        foreach (var folder in StorageFolder.Folders.Values) folder.Children.RemoveAll(x => x.Path == path);
        foreach (var key in StorageFolder.Folders.Keys.Where(x => x == path || x.StartsWith(path + "/", StringComparison.Ordinal)).ToList()) StorageFolder.Folders.Remove(key);
    }
    static int Completed(DownloadVM vm) => vm.Downloadeds.Sum(x => x.Episodes.Count);
    static async Task<(DownloadVM vm, DownloadOperation op)> PartialFixture()
    {
        var (vm, row, op) = SeedTransfer(); await MediaFixture(); StorageFolder.OnDelete = _ => Task.FromException(new UnauthorizedAccessException("locked remnant"));
        await vm.DeleteItemAsync(row); await Until(() => vm.Downloadings.Count == 0); StorageFolder.OnDelete = null;
        return (vm, op);
    }
    static async Task ReviewFixAcceptance()
    {
        var capture = new Capture();
        foreach (var command in new[] { "retained", "fresh", "bulk" })
        {
            await Reset(); var (vm, row, op) = SeedTransfer(); op.ThrowCancel = true; await vm.DeleteItemAsync(row); var source = op.AttachedToken; StorageFolder.Events.Clear(); op.ThrowCancel = false; vm.LoadDownloading();
            if (command == "retained") capture.Run(() => vm.RetryCleanupCommand.Execute(null)); else if (command == "fresh") await vm.DeleteItemAsync(row); else await vm.DeleteAllAsync();
            Check(!StorageFolder.Events.Any(x => x.StartsWith("lookup:") || x.StartsWith("delete-enter:")) && !op.Completion.Task.IsCompleted && vm.CleanupFailures.Count > 0, "RF1 " + command + " fails closed while cancellation-failed GUID remains current even after primitive fault clears");
            Check(op.AttachCount == 1 && op.AttachedToken == source, "RF1 " + command + " keeps original attached source across rescan");
            op.Completion.TrySetResult(); BackgroundDownloader.Operations.Remove(op); await Until(() => vm.Downloadings.Count == 0); capture.Run(() => vm.RetryCleanupCommand.Execute(null));
            Check(vm.CleanupFailures.Count == 0 && StorageFolder.Deleted.Contains(Target), "RF1 " + command + " cleanup succeeds after independent operation settlement/absence");
        }
        await Reset(); var (locallyRetired, localRow, localOp) = SeedTransfer(); localOp.ThrowCancel = true; await locallyRetired.DeleteItemAsync(localRow); localOp.Completion.TrySetCanceled(); await Until(() => locallyRetired.Downloadings.Count == 0); locallyRetired.LoadDownloading(); StorageFolder.Events.Clear(); localOp.ThrowCancel = false; capture.Run(() => locallyRetired.RetryCleanupCommand.Execute(null));
        Check(localOp.AttachCount == 1 && !StorageFolder.Events.Any(x => x.StartsWith("lookup:")), "RF1 local observer cancellation with GUID still current does not clear failed source knowledge or reattach");
        BackgroundDownloader.Operations.Remove(localOp); BackgroundDownloader.FailEnumeration = true; capture.Run(() => locallyRetired.RetryCleanupCommand.Execute(null)); Check(locallyRetired.CleanupFailures.Count > 0 && StorageFolder.Deleted.Count == 0, "RF1 failed enumeration is not failed-operation absence proof"); BackgroundDownloader.FailEnumeration = false; capture.Run(() => locallyRetired.RetryCleanupCommand.Execute(null)); Check(locallyRetired.CleanupFailures.Count == 0, "RF1 later proven absence clears only the owned failed GUID");
        await Reset(); var (batch, firstRow, firstOp) = SeedTransfer(); var poisoned = new DownloadOperation { Path = Target + "/audio.m4s", ThrowCancel = true }; BackgroundDownloader.Operations.Add(poisoned); SettingHelper.SetValue(poisoned.Guid.ToString(), new DownloadGUIDInfo { GUID = poisoned.Guid.ToString(), CID = "cid1", Path = Target }); var other = new DownloadOperation { Path = "/episodes/other/video.m4s" }; BackgroundDownloader.Operations.Add(other); SettingHelper.SetValue(other.Guid.ToString(), new DownloadGUIDInfo { GUID = other.Guid.ToString(), CID = "other", Path = "/episodes/other" }); batch.LoadDownloading(); await batch.DeleteAllAsync();
        Check(firstOp.CancellationSignaled && poisoned.CancellationSignaled && !StorageFolder.Deleted.Contains(Target) && StorageFolder.Deleted.Contains("/episodes/other"), "RF1 partial-sibling fault prevents target deletion while independent episode proceeds");
        Check(batch.CleanupFailures.Count == 1 && batch.CleanupFailures.Single().FailureText.Contains("部分任务已取消"), "RF1 ordered sibling cancellation reports partial result truthfully");
        poisoned.Completion.TrySetResult(); BackgroundDownloader.Operations.Remove(poisoned); await Until(() => batch.Downloadings.Count == 0);
        await Reset(); var (partial, cancelled) = await PartialFixture(); await partial.LoadDownloadedAsync();
        Check(Completed(partial) == 0 && partial.Downloadeds.Count == 0 && partial.DownloadedStatus.Contains("清理") && partial.CleanupFailures.Count == 1, "FA02 known nonzero cancelled remnant is withheld with recovery/partial status and no empty completed title");
        await partial.LoadDownloadedAsync(); partial.LoadDownloading(); await partial.LoadDownloadedAsync(); Check(Completed(partial) == 0, "FA02 marker survives observer retirement and repeated active/completed scans");
        await MediaFixture("cid10"); await MediaFixture("cid1", "av2"); await partial.LoadDownloadedAsync();
        Check(Completed(partial) == 2 && partial.Downloadeds.SelectMany(x => x.Episodes).All(x => x.Path != Target), "FA02 exact-path marker preserves prefix sibling and same-CID different-title media");
        Utils.Confirm = (_, _) => Task.FromResult(false); capture.Run(() => partial.RetryCleanupCommand.Execute(null)); await partial.LoadDownloadedAsync(); Check(Completed(partial) == 2 && partial.CleanupFailures.Count == 1, "FA02 declined retry leaves remnant withheld and recovery unchanged");
        Utils.Confirm = (_, _) => Task.FromResult(true); RemoveMedia(Target); StorageFolder.OnLookup = p => p == Target ? Task.FromException<StorageFolder>(new FileNotFoundException()) : Task.FromResult(StorageFolder.Folders[p]); capture.Run(() => partial.RetryCleanupCommand.Execute(null)); StorageFolder.OnLookup = null; await partial.LoadDownloadedAsync();
        Check(partial.CleanupFailures.Count == 0 && Completed(partial) == 2, "FA02 validated absent cleanup resolves exact marker while preserving sibling controls");
        await Reset(); (partial, cancelled) = await PartialFixture(); await MediaFixture("valid"); await partial.LoadDownloadedAsync(); var title = partial.Downloadeds.Single(); Check(title.Episodes.Count == 1, "FA02 completed title retains valid descendant while partial descendant stays withheld");
        var page = new DownloadPage { downloadVM = partial }; StorageFolder.OnDelete = path => { RemoveMedia(path); return Task.CompletedTask; }; capture.Run(() => page.DeleteTitle(title)); StorageFolder.OnDelete = null; await partial.LoadDownloadedAsync(); Check(partial.CleanupFailures.Count == 0 && Completed(partial) == 0, "FA02 successful actual completed-title subtree cleanup resolves covered marker and old recovery");
        // Hold a genuine file read while the actual cancellation and failed deletion establish newer partial state.
        await Reset(); var (reading, readRow, readOp) = SeedTransfer(); await MediaFixture(); var readGate = Pending<string>(); FileIO.OnReadText = f => f.Path == Target + "/info.json" ? readGate.Task : Task.FromResult(f.Text); var scan = reading.LoadDownloadedAsync(); await Until(() => reading.LoadingDownloaded); StorageFolder.OnDelete = _ => Task.FromException(new Exception("locked")); await reading.DeleteItemAsync(readRow); readGate.SetResult(StorageFolder.Folders[Target].Files["info.json"].Text); await scan; FileIO.OnReadText = null;
        Check(Completed(reading) == 0 && reading.CleanupFailures.Count == 1, "FA02 scan started before cancellation rechecks classification before publication");
        foreach (var mode in new[] { "pre-guid-failure", "partial-preparation", "one-transfer", "transfer-failure", "transfer-cancel", "pending-preparation" })
        {
            await Reset(); (partial, cancelled) = await PartialFixture(); var replacement = Info(); replacement.Urls.Add(new() { FileName = "audio.m4s", Url = "https://example.invalid/audio" }); Task preparing = null; TaskCompletionSource<StorageFile> held = null;
            if (mode == "pre-guid-failure") StorageFolder.OnCreateFile = p => p.EndsWith(".m4s") ? Task.FromException<StorageFile>(new Exception("media file")) : Task.FromResult(new StorageFile { Path = p });
            if (mode == "partial-preparation") StorageFolder.OnCreateFile = p => p.EndsWith("audio.m4s") ? Task.FromException<StorageFile>(new Exception("audio file")) : Task.FromResult(new StorageFile { Path = p });
            if (mode == "pending-preparation") { held = Pending<StorageFile>(); StorageFolder.OnCreateFile = p => p.EndsWith("audio.m4s") ? held.Task : Task.FromResult(new StorageFile { Path = p }); preparing = DownloadHelper.AddDownload(replacement); }
            else await Fails(() => DownloadHelper.AddDownload(replacement));
            var operations = BackgroundDownloader.Operations.ToList();
            if (mode == "one-transfer" || mode == "partial-preparation") operations.First().Completion.TrySetResult();
            if (mode == "transfer-failure") { foreach (var op in operations) op.Completion.TrySetException(new Exception("transfer")); BackgroundDownloader.Operations.Clear(); }
            if (mode == "transfer-cancel") { foreach (var op in operations) op.Completion.TrySetCanceled(); BackgroundDownloader.Operations.Clear(); }
            if (mode == "pending-preparation") operations.First().Completion.TrySetResult();
            await Task.Delay(10); await partial.LoadDownloadedAsync(); Check(Completed(partial) == 0, "FA02 replacement " + mode + " is not successful current-generation completion proof");
            if (held != null) { held.SetException(new Exception("settled fixture")); await Fails(() => preparing); }
        }
        await Reset(); (partial, cancelled) = await PartialFixture(); var oldTarget = partial.CleanupFailures.Single(); var successful = Info(); successful.Urls.Add(new() { FileName = "audio.m4s", Url = "https://example.invalid/audio" }); await DownloadHelper.AddDownload(successful); var required = BackgroundDownloader.Operations.ToList(); required[0].Completion.TrySetResult(); await Task.Delay(10); await partial.LoadDownloadedAsync(); Check(Completed(partial) == 0, "FA02 one of two required successful transfers cannot publish replacement");
        required[1].Completion.TrySetResult(); BackgroundDownloader.Operations.Clear(); await Task.Delay(10); capture.Run(() => partial.RetryCleanupCommand.Execute(null));
        Check(partial.CleanupFailures.Count == 0 && StorageFolder.Deleted.Count == 0 && Utils.Toasts.Last().Contains("旧清理记录已失效"), "FA02 retry identifies successful replacement as superseded without deleting or pretending partial files remain"); await partial.LoadDownloadedAsync();
        Check(Completed(partial) == 1 && partial.CleanupFailures.Count == 0, "FA02 all-required current replacement success publishes media and retires stale recovery without new authority");
        Check(await Fails(() => partial.DeleteCleanupAsync(oldTarget)), "FA02 old cleanup snapshot remains stale after complete replacement");
        await DownloadHelper.AddDownload(Info("unrelated")); await partial.LoadDownloadedAsync(); Check(partial.Downloadeds.SelectMany(x => x.Episodes).Any(x => x.Path == Target), "FA02 unrelated sibling admission does not expire completed exact-path proof");
        // A later failed generation cannot inherit the preceding successful generation's result.
        StorageFolder.OnCreateFile = p => p.EndsWith("video.m4s") ? Task.FromException<StorageFile>(new Exception("later file")) : Task.FromResult(new StorageFile { Path = p }); await Fails(() => DownloadHelper.AddDownload(Info())); await partial.LoadDownloadedAsync();
        Check(partial.Downloadeds.SelectMany(x => x.Episodes).All(x => x.Path != Target), "FA02 later failed same-path generation does not inherit older successful evidence");
        await Reset(); (partial, cancelled) = await PartialFixture(); await MediaFixture("valid"); var pauseRead = Pending<string>(); FileIO.OnReadText = f => f.Path == Target + "/info.json" ? pauseRead.Task : Task.FromResult(f.Text); var oldScan = partial.LoadDownloadedAsync();
        // An implementation may withhold the known remnant before reading it; either order must be safe.
        await DownloadHelper.AddDownload(Info()); foreach (var op in BackgroundDownloader.Operations.ToList()) op.Completion.TrySetResult(); BackgroundDownloader.Operations.Clear(); pauseRead.TrySetResult(StorageFolder.Folders[Target].Files["info.json"].Text); await oldScan; FileIO.OnReadText = null; Check(Completed(partial) == 1, "FA02 scan held across replacement cannot publish its stale episode candidate"); await Task.Delay(10); await partial.LoadDownloadedAsync(); Check(partial.Downloadeds.SelectMany(x => x.Episodes).Any(x => x.Path == Target), "FA02 successful generation eventually recovers after scan/replacement ordering");
        await Reset(); (partial, cancelled) = await PartialFixture(); var staleCleanup = partial.CleanupFailures.Single(); await DownloadHelper.AddDownload(Info()); partial.LoadDownloading(); var newest = partial.Downloadings.Single(); StorageFolder.OnDelete = _ => Task.FromException(new Exception("new remnant")); await partial.DeleteItemAsync(newest); StorageFolder.OnDelete = null;
        Check(await Fails(() => partial.DeleteCleanupAsync(staleCleanup)), "FA02 stale old cleanup cannot clear newer generation's remnant marker"); await partial.LoadDownloadedAsync(); Check(Completed(partial) == 0, "FA02 newer cancelled generation remains withheld after old cleanup rejection");
        await Reset(); (partial, cancelled) = await PartialFixture(); await DownloadHelper.AddDownload(Info()); var oldGeneration = BackgroundDownloader.Operations.ToList(); await DownloadHelper.AddDownload(Info()); var laterGeneration = BackgroundDownloader.Operations.Except(oldGeneration).ToList(); foreach (var op in laterGeneration) op.Completion.TrySetException(new Exception("newer generation failed")); foreach (var op in oldGeneration) op.Completion.TrySetResult(); BackgroundDownloader.Operations.Clear(); await Task.Delay(10); await partial.LoadDownloadedAsync();
        Check(Completed(partial) == 0, "FA02 old successful completion cannot override newer failed generation");
        await DownloadHelper.AddDownload(Info()); foreach (var op in BackgroundDownloader.Operations.ToList()) op.Completion.TrySetResult(); BackgroundDownloader.Operations.Clear(); await Task.Delay(10); await partial.LoadDownloadedAsync(); Check(Completed(partial) == 1, "FA02 later genuinely successful same-path generation recovers without permanent marker poisoning");
        foreach (var actionType in new[] { "dislike", "feedback" })
        {
            await Reset(); var feed = new RecommendVM(); FakeTransport.Next = _ => Task.FromResult(Feed("[{\"idx\":\"10\",\"card_goto\":\"av\",\"args\":{}}]")); await feed.GetRecommend(); FakeTransport.Next = _ => Task.FromResult(Json("{\"code\":0}")); await feed.Dislike("10", new() { type = actionType }, null);
            Check(feed.Items.Count == 0 && feed.StatusText == "暂无推荐内容" && !feed.AutoLoad, "FA03 accepted last " + actionType + " recomputes ready empty state"); var requests = FakeTransport.Requests.Count; capture.Run(feed.LoadMore); Check(FakeTransport.Requests.Count == requests, "FA03 empty action result has harmless LoadMore");
            FakeTransport.Next = _ => Task.FromResult(Feed("[]")); capture.Run(feed.Refresh); Check(feed.StatusText == "暂无推荐内容", "FA03 empty action result retains working refresh");
        }
        await Reset(); var nonfinal = new RecommendVM(); FakeTransport.Next = _ => Task.FromResult(Feed("[{\"idx\":\"10\",\"card_goto\":\"av\",\"args\":{}},{\"idx\":\"11\",\"card_goto\":\"av\",\"args\":{}}]")); await nonfinal.GetRecommend(); var bannersBefore = nonfinal.Banner; FakeTransport.Next = _ => Task.FromResult(Json("{\"code\":0}")); await nonfinal.Dislike("11", new() { type = "dislike" }, null); FakeTransport.Next = _ => Task.FromResult(Feed("[]")); capture.Run(nonfinal.LoadMore);
        Check(nonfinal.Items.Count == 1 && ReferenceEquals(bannersBefore, nonfinal.Banner) && FakeTransport.Requests.Last() == "10", "FA03 nonfinal action removal recomputes usable visible continuation while preserving banner");
        var toastsBefore = Utils.Toasts.Count; await nonfinal.Dislike("missing", new() { type = "dislike" }, null); Check(Utils.Toasts.Count == toastsBefore, "FA03 missing action target is safe no-op");
        await Reset(); var changed = new RecommendVM(); FakeTransport.Next = _ => Task.FromResult(Feed("[{\"idx\":\"10\",\"card_goto\":\"av\",\"args\":{}}]")); await changed.GetRecommend(); var ack = Pending<HttpResults>(); FakeTransport.Next = _ => ack.Task; var actionPending = changed.Dislike("10", new() { type = "dislike" }, null); FakeTransport.Next = _ => Task.FromResult(Feed("[{\"idx\":\"10\",\"card_goto\":\"av\",\"title\":\"replacement\",\"args\":{}}]")); await changed.GetRecommend(); var replacementRows = changed.Items; ack.SetResult(Json("{\"code\":0}")); await actionPending;
        Check(ReferenceEquals(replacementRows, changed.Items) && changed.Items.Count == 1 && changed.Items[0].title == "replacement" && changed.AutoLoad, "FA03 stale acknowledged action cannot mutate refreshed same-idx replacement state");
        foreach (var mode in new[] { "refresh-pending", "append-pending", "refresh-failed", "append-failed", "no-progress" })
        {
            await Reset(); changed = new RecommendVM(); FakeTransport.Next = _ => Task.FromResult(Feed("[{\"idx\":\"10\",\"card_goto\":\"av\",\"args\":{}}]")); await changed.GetRecommend(); ack = Pending<HttpResults>(); FakeTransport.Next = _ => ack.Task; actionPending = changed.Dislike("10", new() { type = "feedback" }, null); var response = Pending<HttpResults>(); FakeTransport.Next = _ => response.Task; bool append = mode.StartsWith("append") || mode == "no-progress"; var request = changed.GetRecommend(append ? "10" : "0");
            if (mode.EndsWith("failed")) response.SetResult(new() { status = false }); if (mode == "no-progress") response.SetResult(Feed("[]")); if (!mode.EndsWith("pending")) await request;
            var state = changed.StatusText; ack.SetResult(Json("{\"code\":0}")); await actionPending;
            Check(changed.Items.Count == 0 && changed.StatusText == state && !changed.AutoLoad && changed.Loading == mode.EndsWith("pending"), "FA03 current request/error/no-progress state owns action completion: " + mode);
            if (mode.EndsWith("pending")) { response.SetResult(new() { status = false }); await request; }
            if (mode != "no-progress") { FakeTransport.Next = _ => Task.FromResult(Feed("[]")); capture.Run(() => changed.RetryCommand.Execute(null)); Check(FakeTransport.Requests.Last() == (append ? "10" : "0"), "FA03 explicit retry keeps failed request mode after rows became empty: " + mode); }
        }
        var getProgress = typeof(DownloadVM).GetMethod("GetProgress", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        foreach (var sample in new[] { (0UL,0UL,0d), (1UL,0UL,0d), (0UL,10UL,0d), (5UL,10UL,50d), (10UL,10UL,100d), (20UL,10UL,0d), (ulong.MaxValue,0UL,0d), (ulong.MaxValue,ulong.MaxValue,100d), (1UL,ulong.MaxValue,0d) })
        {
            var value = (double)getProgress.Invoke(new DownloadVM(), new object[] { sample.Item1, sample.Item2 }); Check(!double.IsNaN(value) && !double.IsInfinity(value) && value >= 0 && value <= 100 && (sample.Item1 == 1 && sample.Item2 == ulong.MaxValue || value == sample.Item3), $"FA04 finite bounded actual GetProgress {sample.Item1}/{sample.Item2}");
        }
        await Reset(); var (progressVM, progressRow, progressOp) = SeedTransfer(); progressOp.Progress.BytesReceived = 0; progressOp.Progress.TotalBytesToReceive = 0; progressVM.LoadDownloading(); Check(progressVM.Downloadings.Single().Items.Single().Progress == 0, "FA04 real LoadDownloading publishes finite zero for unknown total"); progressOp.Progress.BytesReceived = 1; progressOp.ReportProgress(); await Task.Delay(20); Check(progressVM.Downloadings.Single().Items.Single().Progress == 0, "FA04 actual DownloadProgress callback publishes finite zero for positive/unknown total");
    }
    static readonly List<Task> CallerGates = new();
    static TaskCompletionSource<T> CallerPending<T>() { var gate = Pending<T>(); CallerGates.Add(gate.Task); return gate; }
    static readonly BiliLite.Modules.Player.Playurl.BiliPlayUrlRequest Mapper = new();
    static Newtonsoft.Json.Linq.JObject PlayInput(params string[] urls) => new()
    {
        ["timelength"] = 1000,
        ["support_formats"] = new Newtonsoft.Json.Linq.JArray(new Newtonsoft.Json.Linq.JObject { ["quality"] = 80, ["new_description"] = "1080P" }),
        ["durl"] = new Newtonsoft.Json.Linq.JArray(urls.Select((u, i) => new Newtonsoft.Json.Linq.JObject { ["url"] = u, ["order"] = 9 - i, ["size"] = 10, ["length"] = 500, ["backup_url"] = new Newtonsoft.Json.Linq.JArray() }))
    };
    static Task<BiliLite.Modules.Player.Playurl.BiliPlayUrlQualitesInfo> Mapped(params string[] urls) => Mapper.Map(PlayInput(urls));
    static BiliLite.Dialogs.DownloadEpisodeItem Episode(string cid) => new() { AVID = "av1", CID = cid, EpisodeID = cid, Title = cid, Index = 1 };
    static BiliLite.Dialogs.DownloadDialog Dialog(params BiliLite.Dialogs.DownloadEpisodeItem[] episodes)
    {
        var dialog = new BiliLite.Dialogs.DownloadDialog(new() { ID = "av1", Title = "caller", Cover = "https://image.invalid/cover", Type = DownloadType.Video, Episodes = episodes.ToList() });
        foreach (var ep in episodes) dialog.listView.SelectedItems.Add(ep);
        return dialog;
    }
    static void CallerDefaults()
    {
        DeliveryBridge.GetInfo = (_, _) => Task.FromResult(new PlayerInfo());
        DeliveryBridge.Next = (_, _) => Mapped("https://v.bilivideo.com/default");
        StorageFile.OnBasic = null; StorageFolder.OnItem = null; Utils.OnToast = null;
        BiliLite.Dialogs.LoginDialog.Calls = 0; BiliLite.Dialogs.LoginDialog.Deliver = () => Task.CompletedTask;
        SettingHelper.Account.Logined = true;
    }
    static void SettleCaller(Capture capture)
    {
        capture.Run(async () =>
        {
            foreach (var op in BackgroundDownloader.Operations.ToList()) op.Completion.TrySetResult();
            await Until(() => DownloadVM.Instance.Downloadings.Count == 0);
        });
        BackgroundDownloader.Operations.Clear();
    }
    static string TargetSnapshot()
    {
        var stamp = DownloadPathCoordinator.CaptureRead(Target);
        var disk = StorageFolder.Folders.Where(p => p.Key == Target || p.Key == "/videos/哔哩哔哩下载/av1").OrderBy(p => p.Key).SelectMany(p => p.Value.Files.OrderBy(f => f.Key).Select(f => p.Key + "/" + f.Key + ":" + f.Value.Text + ":" + f.Value.Size + ":" + f.Value.DateCreated.Ticks));
        return string.Join('|', disk) + ";settings=" + Newtonsoft.Json.JsonConvert.SerializeObject(SettingHelper.Values) + $";creation={stamp.Creation};evidence={stamp.Evidence};proof={DownloadPathCoordinator.CompletedRevision(Target)}";
    }
    static async Task CallerAcceptance()
    {
        Console.WriteLine("CALLER_ADDITIVE_BEGIN old208=" + checks);
        var capture = new Capture();
        foreach (var kind in new[] { "multi", "single", "dash-audio", "dash-video" })
        {
            await Reset(); CallerDefaults();
            var urls = kind == "multi" ? new[] { "https://v.bilivideo.com/first", "https://v.bilivideo.com/second", "https://v.bilivideo.com/third" } : new[] { "https://v.bilivideo.com/single" };
            var input = PlayInput(urls);
            var expected = kind == "multi" ? new[] { "0.blv", "1.blv", "2.blv" } : new[] { "0.blv" };
            if (kind.StartsWith("dash"))
            {
                input.Remove("durl");
                input["dash"] = Newtonsoft.Json.Linq.JObject.Parse("{\"duration\":1,\"minBufferTime\":1.5,\"video\":[{\"id\":80,\"codecid\":7,\"baseUrl\":\"https://v.bilivideo.com/video\",\"backupUrl\":[],\"width\":1280,\"height\":720,\"SegmentBase\":{\"indexRange\":\"0-1\",\"initialization\":\"0-0\"}}],\"audio\":[]}");
                urls = new[] { "https://v.bilivideo.com/video" };
                if (kind == "dash-audio") { input["dash"]["audio"] = Newtonsoft.Json.Linq.JArray.Parse("[{\"id\":30280,\"baseUrl\":\"https://v.bilivideo.com/audio\",\"backupUrl\":[],\"SegmentBase\":{\"indexRange\":\"0-1\",\"initialization\":\"0-0\"}}]"); urls = new[] { "https://v.bilivideo.com/video", "https://v.bilivideo.com/audio" }; expected = new[] { "video.m4s", "audio.m4s" }; }
            }
            var mapped = await Mapper.Map(input); DeliveryBridge.Next = (_, _) => Task.FromResult(mapped);
            var ep = Episode("cid1"); var dialog = Dialog(ep); capture.Run(dialog.Primary);
            var ops = BackgroundDownloader.Operations.ToList(); var manifest = Newtonsoft.Json.JsonConvert.DeserializeObject<DownloadSaveEpisodeInfo>(StorageFolder.Folders[Target].Files["info.json"].Text);
            Check(mapped.Success && ops.Select(x => x.Source.AbsoluteUri).SequenceEqual(urls), "FA05 actual mapper/forwarder preserves source order " + kind);
            Check(ops.Select(x => System.IO.Path.GetFileName(x.Path)).SequenceEqual(expected) && ops.Select(x => x.Path).Distinct().Count() == expected.Length, "FA05 distinct ordered destination names " + kind);
            Check(manifest.VideoPath.SequenceEqual(expected), "FA05 SaveInfo names match ordered media " + kind);
            Check(ops.All(x => x.Headers.Count == 2 && x.Headers["User-Agent"] == "agent" && x.Headers["Referer"] == "https://www.bilibili.com" && x.TransferGroup == DownloadHelper.group && x.CostPolicy == BackgroundTransferCostPolicy.UnrestrictedOnly), "FA05 actual header/group/cost compatibility " + kind);
            Check(ep.State == 2 && dialog.HideCalls == 1 && dialog.IsPrimaryButtonEnabled && Utils.Toasts.Contains("已添加至下载列表") && capture.Errors.Count == 0, "FA05 ordinary success close and enabled action " + kind);
            Check(StorageFolder.Events.Contains("create-file:" + Target + "/" + expected[0]) && StorageFolder.FileOptions.Where(x => expected.Any(n => x.path == Target + "/" + n)).All(x => x.option == CreationCollisionOption.OpenIfExists) && StorageFolder.FileOptions.Where(x => x.path.EndsWith("/info.json")).All(x => x.option == CreationCollisionOption.ReplaceExisting), "FA05 actual file creation remains used " + kind);
            SettleCaller(capture);
        }
        // Typed/mixed failure and the explicit retry must keep already admitted siblings.
        await Reset(); CallerDefaults(); var failed = Episode("cid1"); var good = Episode("cid2"); var calls = new List<string>();
        DeliveryBridge.Next = (info, qn) => { if (qn == 0) return Mapped("https://v.bilivideo.com/quality"); calls.Add(info.cid); return info.cid == "cid1" ? Task.FromResult(BiliLite.Modules.Player.Playurl.BiliPlayUrlQualitesInfo.Failure("typed unavailable")) : Mapped("https://v.bilivideo.com/good"); };
        var mixed = Dialog(failed, good); var notifications = new List<string>(); failed.PropertyChanged += (_, e) => notifications.Add(e.PropertyName); capture.Run(mixed.Primary);
        Check(failed.State == 99 && failed.ErrorMessage == "typed unavailable" && notifications.Contains("ErrorMessage"), "FA07 real failed item notifies bound ErrorMessage");
        Check(good.State == 2 && mixed.HideCalls == 0 && mixed.IsPrimaryButtonEnabled && Utils.Toasts.Last() == "有视频下载失败", "FA07 mixed failure keeps dialog open with failure feedback");
        Check(DownloadVM.Instance.Downloadings.Any(x => x.EpisodeID == "cid2") && BackgroundDownloader.Operations.Count == 1, "FA07 successful sibling is discovered despite typed failure");
        DeliveryBridge.Next = (info, qn) => { calls.Add(info.cid); return Mapped("https://v.bilivideo.com/retry"); }; capture.Run(mixed.Primary);
        Check(calls.SequenceEqual(new[] { "cid1", "cid2", "cid1" }) && BackgroundDownloader.Operations.Count == 2, "FA07 explicit retry skips admitted State2 sibling without duplicate request/operation");
        Check(failed.State == 2 && good.State == 2 && mixed.HideCalls == 1 && mixed.IsPrimaryButtonEnabled && Utils.Toasts.Last() == "已添加至下载列表", "FA07 eventual successful retry closes normally"); SettleCaller(capture);
        await Reset(); CallerDefaults(); var zero = Dialog(Episode("cid1")); zero.ClearSelection(); capture.Run(zero.Primary);
        Check(zero.HideCalls == 0 && zero.IsPrimaryButtonEnabled && Utils.Toasts.Last() == "请选择要下载的剧集" && BackgroundDownloader.Operations.Count == 0, "FA07 zero selection remains harmless and open");
        await Reset(); CallerDefaults(); var broken = Episode("cid1"); var prep = Dialog(broken); StorageFolder.OnCreateFile = _ => Task.FromException<StorageFile>(new Exception("disk preparation refused")); capture.Run(prep.Primary);
        Check(broken.State == 99 && !string.IsNullOrWhiteSpace(broken.ErrorMessage) && prep.HideCalls == 0 && prep.IsPrimaryButtonEnabled && Utils.Toasts.Last() == "有视频下载失败", "FA07 actual preparation exception remains retryable");
        // The real unchecked handler mutates the documented live IList while preparation is held.
        foreach (var hold in new[] { "info", "preparation", "mixed" })
        {
            await Reset(); CallerDefaults(); var a = Episode("cid1"); var b = Episode("cid2"); var third = Episode("cid3"); var selected = Dialog(a, b); var requested = new List<string>();
            var infoGate = CallerPending<PlayerInfo>(); var fileGate = CallerPending<StorageFile>(); bool waiting = false;
            DeliveryBridge.GetInfo = (_, cid) => { if ((hold == "info" || hold == "mixed") && cid == "cid1") { waiting = true; return infoGate.Task; } return Task.FromResult(new PlayerInfo()); };
            DeliveryBridge.Next = (info, qn) => { requested.Add(info.cid); return hold == "mixed" && info.cid == "cid1" ? Task.FromResult(BiliLite.Modules.Player.Playurl.BiliPlayUrlQualitesInfo.Failure("mixed selected failure")) : Mapped("https://v.bilivideo.com/" + info.cid); };
            StorageFolder.OnCreateFile = path => { if (hold == "preparation" && path == Target + "/0.blv") { waiting = true; return fileGate.Task; } return Task.FromResult(new StorageFile { Path = path }); };
            capture.Run(async () => { selected.Primary(); await Until(() => waiting); selected.ClearSelection(); selected.listView.SelectedItems.Add(third); selected.HidePreview(); infoGate.TrySetResult(new PlayerInfo()); fileGate.TrySetResult(new StorageFile { Path = Target + "/0.blv" }); });
            StorageFolder.OnCreateFile = null;
            Check(a.State == (hold == "mixed" ? 99 : 2) && b.State == 2 && third.State == 0 && requested.SequenceEqual(new[] { "cid1", "cid2" }), "CR1 initiating object snapshot survives real clear/add/hide-preview at " + hold);
            Check(capture.Errors.Count == 0 && selected.IsPrimaryButtonEnabled && selected.HideCalls == (hold == "mixed" ? 0 : 1), "CR1 complete event contains live selection continuation at " + hold);
            Check(DownloadVM.Instance.Downloadings.Select(x => x.EpisodeID).Order().SequenceEqual(hold == "mixed" ? new[] { "cid2" } : new[] { "cid1", "cid2" }), "CR1 admitted original siblings remain discoverable at " + hold);
            if (hold == "mixed") { selected.ClearSelection(); selected.listView.SelectedItems.Add(a); selected.listView.SelectedItems.Add(b); DeliveryBridge.Next = (info, _) => { requested.Add(info.cid); return Mapped("https://v.bilivideo.com/retry"); }; capture.Run(selected.Primary); Check(a.State == 2 && b.State == 2 && third.State == 0 && requested.SequenceEqual(new[] { "cid1", "cid2", "cid1" }) && BackgroundDownloader.Operations.Count == 2 && selected.HideCalls == 1, "CR1 explicit mixed reselection/retry skips admitted sibling and excludes third"); }
            SettleCaller(capture); capture.Errors.Clear();
        }
        // Reporting is outside the original per-item catch; fault once after admission.
        await Reset(); CallerDefaults(); var reportRow = Episode("cid1"); var reporting = Dialog(reportRow); bool reportFault = false;
        Utils.OnToast = message => { if (message == "已添加至下载列表" && !reportFault) { reportFault = true; throw new Exception("reporting delivery failed"); } }; capture.Run(reporting.Primary); Utils.OnToast = null;
        Check(reportFault && capture.Errors.Count == 0 && reporting.IsPrimaryButtonEnabled && reporting.HideCalls == 0 && Utils.Toasts.LastOrDefault() == "有视频下载失败", "CR1 unexpected reporting failure is contained and restores useful open action");
        Check(reportRow.State == 2 && DownloadVM.Instance.Downloadings.Any(x => x.EpisodeID == "cid1"), "CR1 reporting fault retains discoverable admitted sibling"); SettleCaller(capture); capture.Errors.Clear();
        await EmptyAdmissionAcceptance(); await SkippedCandidateAcceptance(); await LoginCallerAcceptance();
        CallerDefaults();
        Check(CallerGates.All(x => x.IsCompleted) && DownloadVM.Instance.Downloadings.Count == 0, "CALLER teardown every held delivery settled and singleton observers drained");
        Console.WriteLine("CALLER_ADDITIVE_END checks=" + checks);
    }
    static async Task EmptyAdmissionAcceptance()
    {
        var capture = new Capture();
        await Reset(); CallerDefaults(); var (partial, _) = await PartialFixture(); var old = partial.CleanupFailures.Single(); var before = TargetSnapshot(); var empty = await Mapped();
        DeliveryBridge.Next = (_, qn) => qn == 0 ? Mapped("https://v.bilivideo.com/quality") : Task.FromResult(empty);
        var ep = Episode("cid1"); var dialog = Dialog(ep); capture.Run(dialog.Primary);
        Check(empty.Success && empty.CurrentQuality.FlvInfo.Count == 0, "FA06 actual unchanged mapper exposes empty durl success");
        Check(ep.State == 99 && !string.IsNullOrWhiteSpace(ep.ErrorMessage) && dialog.HideCalls == 0 && dialog.IsPrimaryButtonEnabled && Utils.Toasts.Last() == "有视频下载失败", "FA06 empty actual caller keeps actionable failed row");
        Check(BackgroundDownloader.Operations.Count == 0 && TargetSnapshot() == before, "FA06 empty actual caller changes no files/settings/read stamps or completion proof");
        await partial.LoadDownloadedAsync();
        Check(Completed(partial) == 0 && partial.Downloadeds.Count == 0 && partial.DownloadedStatus.Contains("清理") && partial.CleanupFailures.Contains(old) && DownloadPathCoordinator.CompletedRevision(Target) == 0, "FA06 zero-media cannot retire known partial/recovery or publish empty completed title");
        // Rejected empty must not stale a cleanup snapshot; use actual validated cleanup.
        var cleanupSucceeded = !await Fails(() => partial.DeleteCleanupAsync(old));
        Check(cleanupSucceeded && StorageFolder.Deleted.Contains(Target), "FA06 rejected empty leaves old validated cleanup authority intact");
        await Reset(); CallerDefaults(); (partial, _) = await PartialFixture(); old = partial.CleanupFailures.Single(); ep = Episode("cid1"); DeliveryBridge.Next = (_, _) => Mapped("https://v.bilivideo.com/a", "https://v.bilivideo.com/b"); dialog = Dialog(ep); capture.Run(dialog.Primary);
        await partial.LoadDownloadedAsync(); Check(ep.State == 2 && Completed(partial) == 0 && partial.CleanupFailures.Contains(old), "FA06 valid nonempty retry pending cannot supersede partial");
        var ops = BackgroundDownloader.Operations.ToList(); ops[0].Completion.TrySetResult(); await Until(() => ops[0].Completion.Task.IsCompleted); await partial.LoadDownloadedAsync(); Check(Completed(partial) == 0, "FA06 one required sibling success cannot prove replacement");
        SettleCaller(capture); await Until(() => DownloadPathCoordinator.CompletedRevision(Target) > old.Revision); await partial.LoadDownloadedAsync();
        Check(Completed(partial) == 1 && partial.CleanupFailures.Count == 0, "FA06 all required current nonempty success recovers completed content");
        var deletes = StorageFolder.Deleted.Count; Check(await Fails(() => partial.DeleteCleanupAsync(old)) && StorageFolder.Deleted.Count == deletes, "FA06 old cleanup cannot delete successful replacement");
        foreach (var invalid in new[] { "null-request", "null-list", "empty", "null-entry", "missing-url", "bad-uri", "missing-name", "unsafe-name", "mixed-invalid" })
        {
            await Reset(); CallerDefaults(); (partial, _) = await PartialFixture(); old = partial.CleanupFailures.Single(); before = TargetSnapshot(); var info = Info();
            switch (invalid)
            {
                case "null-request": info = null; break; case "null-list": info.Urls = null; break; case "empty": info.Urls.Clear(); break;
                case "null-entry": info.Urls[0] = null; break; case "missing-url": info.Urls[0].Url = ""; break; case "bad-uri": info.Urls[0].Url = "://broken"; break;
                case "missing-name": info.Urls[0].FileName = ""; break; case "unsafe-name": info.Urls[0].FileName = "../escape"; break;
                case "mixed-invalid": info.Urls.Add(new() { FileName = "audio.m4s", Url = "relative" }); break;
            }
            var rejected = await Fails(() => DownloadHelper.AddDownload(info));
            Check(rejected && BackgroundDownloader.Operations.Count == 0 && before == TargetSnapshot() && partial.CleanupFailures.Contains(old), "FA06 whole-input validation precedes admission and mutation: " + invalid);
        }
    }
    static async Task SkippedCandidateAcceptance()
    {
        foreach (var shape in new[] { "zero", "missing-media", "missing-info", "bad-info" })
        {
            await Reset(); CallerDefaults(); var (vm, row, op) = SeedTransfer(); op.Progress.BytesReceived = 0; op.Progress.TotalBytesToReceive = 10; var folder = await MediaFixture(); folder.Files["video.m4s"].Size = 0;
            if (shape == "missing-media") folder.Files.Remove("video.m4s"); if (shape == "missing-info") folder.Files.Remove("info.json"); if (shape == "bad-info") { folder.Files["video.m4s"].Size = 1; folder.Files["info.json"].Text = "invalid"; }
            StorageFolder.OnDelete = _ => Task.FromException(new Exception("locked initial remnant")); await vm.DeleteItemAsync(row); await Until(() => vm.Downloadings.Count == 0); StorageFolder.OnDelete = null; await vm.LoadDownloadedAsync();
            Check(vm.Downloadeds.Count == 0 && Completed(vm) == 0 && vm.DownloadedStatus.Contains("清理") && vm.CleanupFailures.Count == 1, "RF2 known initial-zero/ineligible remnant survives eligibility exit: " + shape);
            await MediaFixture("valid"); await vm.LoadDownloadedAsync(); Check(Completed(vm) == 1 && vm.Downloadeds.Single().Episodes.Single().CID == "valid" && vm.DownloadedStatus.Contains("清理"), "RF2 valid same-title sibling remains visible: " + shape);
            var recovery = vm.CleanupFailures.Single(); StorageFolder.OnDelete = path => { RemoveMedia(path); return Task.CompletedTask; }; await vm.DeleteCleanupAsync(recovery); StorageFolder.OnDelete = null; await vm.LoadDownloadedAsync();
            Check(Completed(vm) == 1 && vm.CleanupFailures.Count == 0 && !vm.DownloadedStatus.Contains("清理"), "RF2 validated cleanup recovers truthful sibling library: " + shape);
        }
        foreach (var race in new[] { "cancel-zero-read", "pending-missing-info" })
        {
            await Reset(); CallerDefaults(); var (vm, row, op) = SeedTransfer(); var folder = await MediaFixture(); var size = CallerPending<BasicProperties>(); var item = CallerPending<object>(); bool held = false;
            if (race == "cancel-zero-read") { StorageFile.OnBasic = file => { if (file.Path == Target + "/video.m4s") { held = true; return size.Task; } return Task.FromResult(new BasicProperties { Size = file.Size }); }; }
            else StorageFolder.OnItem = (f, name) => { if (f.Path == Target && name == "info.json") { held = true; return item.Task; } return Task.FromResult<object>(f.Files.TryGetValue(name, out var file) ? file : null); };
            var scan = vm.LoadDownloadedAsync(); await Until(() => held);
            if (race == "cancel-zero-read") { StorageFolder.OnDelete = _ => Task.FromException(new Exception("locked race")); await vm.DeleteItemAsync(row); await Until(() => vm.Downloadings.Count == 0); StorageFolder.OnDelete = null; }
            else await DownloadHelper.AddDownload(Info());
            size.TrySetResult(new BasicProperties { Size = 0 }); item.TrySetResult(null); await scan; StorageFile.OnBasic = null; StorageFolder.OnItem = null;
            Check(vm.Downloadeds.Count == 0 && vm.DownloadedStatus.Contains("清理"), "RF2 final classification observes changed path across skipped primitive read: " + race);
            foreach (var operation in BackgroundDownloader.Operations.ToList()) operation.Completion.TrySetResult();
            await Until(() => vm.Downloadings.Count == 0);
            if (race == "pending-missing-info") await Until(() => DownloadPathCoordinator.CompletedRevision(Target) > 0);
            BackgroundDownloader.Operations.Clear();
        }
        foreach (var legacyShape in new[] { "zero", "missing-info", "bad-info" })
        {
            await Reset(); CallerDefaults(); var root = await KnownFolders.VideosLibrary.CreateFolderAsync("BiliBiliDownload", CreationCollisionOption.OpenIfExists); var title = await root.CreateFolderAsync("title", CreationCollisionOption.OpenIfExists);
            title.Files["info.json"] = new() { Path = title.Path + "/info.json", Text = "{\"thumb\":\"thumb\",\"id\":\"1\",\"title\":\"legacy\",\"type\":\"video\"}" }; var episode = await title.CreateFolderAsync("ep", CreationCollisionOption.OpenIfExists);
            episode.Files["video.flv"] = new() { Path = episode.Path + "/video.flv", Size = legacyShape == "zero" ? 0UL : 1UL };
            if (legacyShape != "missing-info") episode.Files["info.json"] = new() { Path = episode.Path + "/info.json", Text = legacyShape == "bad-info" ? "invalid" : "{\"cid\":\"legacy-cid\",\"index\":1,\"title\":\"episode\"}" };
            var (vm, row, op) = SeedTransfer("legacy-cid", episode.Path); SettingHelper.Values[SettingHelper.Download.LOAD_OLD_DOWNLOAD] = true; StorageFolder.OnDelete = _ => Task.FromException(new Exception("legacy locked")); await vm.DeleteItemAsync(row); await Until(() => vm.Downloadings.Count == 0); StorageFolder.OnDelete = null;
            await vm.LoadDownloadedAsync(); Check(vm.Downloadeds.Count == 0 && vm.DownloadedStatus.Contains("清理") && vm.CleanupFailures.Count == 1, "RF2 actual legacy skipped candidate retains exact classification " + legacyShape);
            StorageFolder.OnDelete = path => { RemoveMedia(path); return Task.CompletedTask; }; await vm.DeleteCleanupAsync(vm.CleanupFailures.Single()); StorageFolder.OnDelete = null;
        }
        await Reset(); CallerDefaults(); var legacy = await MediaFixture(); legacy.Files["video.m4s"].Size = 0; var unrelated = new DownloadVM(); await unrelated.LoadDownloadedAsync();
        Check(unrelated.Downloadeds.Count == 1 && unrelated.Downloadeds[0].Episodes.Count == 0 && !unrelated.DownloadedStatus.Contains("清理"), "RF2 unrelated tolerant empty title retains original behavior");
    }
    static async Task LoginCallerAcceptance()
    {
        foreach (var change in new[] { "collection", "removed", "ack", "pending", "error", "no-progress" })
        {
            await Reset(); CallerDefaults(); var vm = new RecommendVM(); FakeTransport.Next = _ => Task.FromResult(Feed("[{\"idx\":\"10\",\"card_goto\":\"av\",\"args\":{}}]")); await vm.GetRecommend();
            var original = vm.Items; var row = original[0]; SettingHelper.Account.Logined = false; var login = CallerPending<bool>();
            BiliLite.Dialogs.LoginDialog.Deliver = async () => { await login.Task; SettingHelper.Account.Logined = true; };
            FakeTransport.Requests.Clear(); var ack = CallerPending<HttpResults>(); FakeTransport.Next = _ => ack.Task;
            var action = vm.Dislike("10", new() { type = "dislike" }, new() { id = 7 });
            Check(BiliLite.Dialogs.LoginDialog.Calls == 1 && FakeTransport.Requests.Count == 0, "FA08 action waits on actual login helper before request " + change);
            if (change == "collection") vm.Items = new ObservableCollection<RecommendItemModel> { new() { idx = "10", title = "replacement", args = new() } };
            if (change == "removed") original.Remove(row);
            login.SetResult(true); await Until(() => action.IsCompleted || FakeTransport.Requests.Count > 0);
            if (change == "collection" || change == "removed")
            {
                ack.TrySetResult(Json("{\"code\":0}")); await action;
                Check(FakeTransport.Requests.Count == 0 && (change != "collection" || vm.Items.Single().title == "replacement"), "FA08 pre-login collection/object ownership forbids stale request " + change);
                continue;
            }
            Task feedRequest = null;
            if (change == "ack") vm.Items = new ObservableCollection<RecommendItemModel> { new() { idx = "10", title = "replacement", args = new() } };
            var response = CallerPending<HttpResults>();
            if (change != "ack") { FakeTransport.Next = _ => response.Task; feedRequest = vm.GetRecommend("10"); if (change == "error") response.SetResult(new() { status = false }); if (change == "no-progress") response.SetResult(Feed("[]")); if (change != "pending") await feedRequest; }
            if (change == "ack") response.TrySetResult(Feed("[]"));
            var state = vm.StatusText; ack.SetResult(Json("{\"code\":0}")); await action;
            Check(change == "ack" ? vm.Items.Single().title == "replacement" : vm.Items.Count == 0 && vm.StatusText == state && !vm.AutoLoad, "FA08 post-ack collection and feed-state owner remains current " + change);
            if (change == "pending") { response.SetResult(new() { status = false }); await feedRequest; }
            if (change == "pending" || change == "error") { FakeTransport.Next = _ => Task.FromResult(Feed("[]")); var pump = new Capture(); pump.Run(() => vm.RetryCommand.Execute(null)); Check(FakeTransport.Requests.Last() == "10", "FA08 login/action retains explicit append retry mode " + change); }
        }
        await LoginHeldOwnerAcceptance("success");
        await LoginHeldOwnerAcceptance("cancel");
        foreach (var result in new[] { "success", "cancel", "already", "exception" })
        {
            await Reset(); CallerDefaults(); var vm = Seed(); var rows = vm.Items; var row = rows[0]; SettingHelper.Account.Logined = result == "already";
            BiliLite.Dialogs.LoginDialog.Deliver = () => { if (result == "exception") throw new Exception("login unavailable"); SettingHelper.Account.Logined = result == "success"; return Task.CompletedTask; };
            FakeTransport.Next = _ => Task.FromResult(Json("{\"code\":0}")); await vm.Dislike("10", new() { type = "feedback" }, new() { id = 91 });
            bool accepted = result == "success" || result == "already";
            Check(FakeTransport.Requests.Count == (accepted ? 1 : 0) && rows.Contains(row) != accepted && ReferenceEquals(rows, vm.Items), "FA08 actual complete helper honors true-success/cancel/exception: " + result);
            Check(BiliLite.Dialogs.LoginDialog.Calls == (result == "already" ? 0 : 1), "FA08 already logged in bypasses native dialog: " + result);
            if (accepted) Check(FakeTransport.Last.parameter.Contains("feedback_id=91") && !FakeTransport.Last.parameter.Contains("reason_id="), "FA08 authenticated feedback retains actual request fields " + result);
            // Keep the fault last among preserved FA08 controls, then retry in this same
            // process without resetting product state or primitive delivery counters.
            if (result == "exception")
            {
                await LoginRetryAction(vm, rows, row, "terminal fault same VM/collection/row");
                break;
            }
        }
        await LoginSynchronousFaultAcceptance();
        await LoginHeldOwnerAcceptance("fault");
        await LoginHeldOwnerAcceptance("cancellation");
        Check(CallerGates.All(x => x.IsCompleted), "FA09 all tracked native deliveries naturally settled");
    }

    static async Task LoginRetryAction(RecommendVM vm, ObservableCollection<RecommendItemModel> rows, RecommendItemModel row, string schedule)
    {
        var retained = rows.Where(x => !ReferenceEquals(x, row)).ToArray();
        var shows = BiliLite.Dialogs.LoginDialog.Calls; var requests = FakeTransport.Requests.Count;
        if (SettingHelper.Account.Logined) SettingHelper.Account.Logined = false;
        BiliLite.Dialogs.LoginDialog.Deliver = () => { SettingHelper.Account.Logined = true; return Task.CompletedTask; };
        var ack = CallerPending<HttpResults>(); FakeTransport.Next = _ => ack.Task;
        var action = vm.Dislike(row.idx, new() { type = "feedback" }, new() { id = 91 });
        Check(BiliLite.Dialogs.LoginDialog.Calls == shows + 1 && SettingHelper.Account.Logined && FakeTransport.Requests.Count == requests + 1 && rows.Contains(row) && ReferenceEquals(rows, vm.Items), "FA09 later real action reopens native login and awaits acknowledgement: " + schedule);
        var requested = FakeTransport.Requests.Count == requests + 1;
        Check(requested && FakeTransport.Last.parameter.Contains("feedback_id=91") && !FakeTransport.Last.parameter.Contains("reason_id="), "FA09 retry retains real feedback request fields: " + schedule);
        ack.TrySetResult(Json("{\"code\":0}")); await action;
        Console.WriteLine($"FA09_RETRY schedule={schedule};shows={BiliLite.Dialogs.LoginDialog.Calls - shows};requests={FakeTransport.Requests.Count - requests};account={SettingHelper.Account.Logined};row_retained={rows.Contains(row)};ack_settled={ack.Task.IsCompleted}");
        Check(SettingHelper.Account.Logined && ReferenceEquals(rows, vm.Items) && !rows.Contains(row) && rows.SequenceEqual(retained), "FA09 real acknowledged retry removes only initiating row: " + schedule);
    }
    static async Task LoginHeldOwnerAcceptance(string outcome)
    {
        SettingHelper.Account.Logined = false;
        var gate = CallerPending<bool>(); var shows = BiliLite.Dialogs.LoginDialog.Calls;
        BiliLite.Dialogs.LoginDialog.Deliver = () => gate.Task;
        var owner = Utils.ShowLoginDialog(); var admitted = BiliLite.Dialogs.LoginDialog.Calls == shows + 1;
        Check(admitted && !owner.IsCompleted, "FA09 held native owner remains pending: " + outcome);
        var second = Utils.ShowLoginDialog();
        Check(second.IsCompletedSuccessfully && !await second && BiliLite.Dialogs.LoginDialog.Calls == shows + 1 && !owner.IsCompleted, "FA09 second nonowner returns false without another native show: " + outcome);
        var third = Utils.ShowLoginDialog();
        Check(third.IsCompletedSuccessfully && !await third && BiliLite.Dialogs.LoginDialog.Calls == shows + 1 && !owner.IsCompleted, "FA09 third call proves nonowner did not release held owner: " + outcome);
        var fault = new InvalidOperationException("native held fault");
        using var cancellation = new CancellationTokenSource(); cancellation.Cancel();
        if (outcome == "fault") gate.TrySetException(fault);
        else if (outcome == "cancellation") gate.TrySetCanceled(cancellation.Token);
        else { SettingHelper.Account.Logined = outcome == "success"; gate.TrySetResult(true); }
        bool? result = null; Exception observed = null;
        try { result = await owner; } catch (Exception ex) { observed = ex; }
        // Observe every allocated primitive gate even when the poisoned old helper suppressed admission.
        try { await gate.Task; } catch { }
        Check(admitted && (outcome == "fault" ? ReferenceEquals(observed, fault) && owner.IsFaulted : outcome == "cancellation" ? observed is OperationCanceledException cancelled && cancelled.CancellationToken == cancellation.Token && owner.IsCanceled : observed == null && result == (outcome == "success")), "FA09 owner preserves success/cancel/fault/cancellation result and token: " + outcome);
        if (!admitted) Console.WriteLine("FA09 DOWNSTREAM_UNAVAILABLE old helper remained poisoned; native gate settled: " + outcome);
        var vm = Seed(); vm.Items.Add(new() { idx = "other", title = "retained sibling", args = new() });
        await LoginRetryAction(vm, vm.Items, vm.Items[0], "held " + outcome);
    }
    static async Task LoginSynchronousFaultAcceptance()
    {
        SettingHelper.Account.Logined = false; var shows = BiliLite.Dialogs.LoginDialog.Calls;
        var fault = new InvalidOperationException("native synchronous delivery fault");
        BiliLite.Dialogs.LoginDialog.Deliver = () => throw fault;
        Exception observed = null; var owner = Utils.ShowLoginDialog();
        try { await owner; } catch (Exception ex) { observed = ex; }
        Check(BiliLite.Dialogs.LoginDialog.Calls == shows + 1 && ReferenceEquals(observed, fault) && owner.IsFaulted, "FA09 synchronous native delivery throw propagates original fault");
        if (BiliLite.Dialogs.LoginDialog.Calls == shows) Console.WriteLine("FA09 DOWNSTREAM_UNAVAILABLE old helper remained poisoned; synchronous delivery never admitted");
        var vm = Seed(); vm.Items.Add(new() { idx = "other", title = "retained sibling", args = new() });
        await LoginRetryAction(vm, vm.Items, vm.Items[0], "synchronous delivery fault");
    }

    static async Task<int> Main()
    {
        try { await Recommendations(); await DownloadReadsAndTasks(); await CreatorAndCleanup(); await CompletedPageDeletion(); await AcceptanceEdges(); await FinalAcceptance(); await ReviewFixAcceptance(); await CallerAcceptance(); }
        catch (Exception ex) { failures++; Console.WriteLine("FAIL unhandled " + ex); }
        finally { await Reset(); }
        Console.WriteLine($"RECOVERY checks={checks} failures={failures}; one program; actual VMs/helper/coordinator/parser and exact page/season/transport bodies");
        return failures == 0 ? 0 : 1;
    }
}
