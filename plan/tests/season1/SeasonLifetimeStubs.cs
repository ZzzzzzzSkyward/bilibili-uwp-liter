// Native UI/cache/media/transport boundaries are managed doubles. Actual source slices
// own subscriptions, close, navigation, page work and the complete real VM/parser path.
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BiliLite.Modules;

namespace SeasonLifetimeHarness
{
    public class Grid { public readonly List<object> Children = new List<object>(); }
    public class Page
    {
        public object Parent;
        public object Content = new Grid();
        private string title;
        public int TitleWrites;
        public string Title { get => title; set { title = value; TitleWrites++; } }
    }
    public class PlayPage : Page
    {
        protected virtual void OnNavigatedTo(NavigationEventArgs e) { }
        protected virtual void OnNavigatedFrom(NavigationEventArgs e) { }
        protected virtual void OnNavigatingFrom(NavigatingCancelEventArgs e) { }
    }
    public class Frame
    {
        public object Parent;
        object content;
        public object Content { get => content; set { content = value; if (value is Page p) p.Parent = this; } }
    }
    public class TabViewItem
    {
        object content;
        public string Header;
        public object Content { get => content; set { if (content is Frame old) old.Parent = null; content = value; if (value is Frame f) f.Parent = this; } }
    }
    public class TabView { public readonly List<TabViewItem> TabItems = new List<TabViewItem>(); }
    public partial class Shell
    {
        public readonly TabView tabView = new TabView();
        public TabViewItem Attach(SeasonPage page)
        {
            var frame = new MyFrame { Content = page };
            var tab = new TabViewItem { Content = frame };
            tabView.TabItems.Add(tab);
            page.Activate();
            return tab;
        }
        public void Close(TabViewItem tab) => ClosePage(tab);
        public void DetachWithoutClose(TabViewItem tab) { tab.Content = null; tabView.TabItems.Remove(tab); }
    }
    public class RoutedEventArgs : EventArgs { }
    public enum NavigationMode { New, Back, Forward }
    public class NavigationEventArgs : EventArgs { public NavigationMode NavigationMode; public object Parameter; }
    public class NavigatingCancelEventArgs : EventArgs { public NavigationMode NavigationMode; public Type SourcePageType = typeof(Page); }
    public delegate void DataRequestedHandler(DataTransferManager sender, DataRequestedEventArgs e);
    public class DataRequestedEventArgs : EventArgs { }
    public class DataTransferManager
    {
        static readonly DataTransferManager Current = new DataTransferManager();
        public event DataRequestedHandler DataRequested;
        public static DataTransferManager GetForCurrentView() => Current;
        public static int Subscribers => Current.DataRequested?.GetInvocationList().Length ?? 0;
    }
    public static class PageSettings
    {
        public static T GetValue<T>(string key, T fallback) => fallback;
        public static class Player { public const string AUTO_FULL_SCREEN = "full", AUTO_FULL_WINDOW = "window"; }
        public static class UI { public const string DETAIL_DISPLAY = "detail"; }
    }
    public static class MessageCenter { public static int Titles; public static void ChangeTitle(string title) { Titles++; } }
    public static class PageUtils { public static string ParseArea(string title, long id) => ""; }
    public enum PlayDirectorySectionKind { Episodes, Previews }
    public enum PlayTargetStart { Resume }
    public enum VideoPlayType { Season }
    public class PlayInfo
    {
        public string avid, cid, ep_id, title, area;
        public int duration, season_id, season_type, order;
        public bool is_vip, is_interaction;
        public VideoPlayType play_mode;
    }
    // Explicit player-boundary spy: counts every attempted call, including calls after
    // disposal. It neither copies nor executes PlayerControl, and never masks late calls.
    public class PlayerBoundary
    {
        public int Attempts, AfterDisposeAttempts, Disposes;
        public bool IsFullScreen, IsFullWindow;
        public List<PlayInfo> LastItems;
        public Action OnInitialize;
        public void InitializePlayInfo(List<PlayInfo> items, int index, PlayTargetStart start)
        { Attempts++; if (Disposes > 0) AfterDisposeAttempts++; LastItems = items; OnInitialize?.Invoke(); }
        public void UpdateDirectoryInfo(bool visible, string title) { }
        public void FullScreen(bool value) { }
        public void MiniWidnows(bool value) { }
        public void Dispose() { Disposes++; }
    }
    public class SeasonReviewVM
    {
        public int Writes;
        int mediaID;
        public int MediaID { get => mediaID; set { mediaID = value; Writes++; } }
    }
    public static class PageApi
    {
        public static class CommentApi { public enum CommentType { Video } public enum CommentSort { Hot } }
    }
    public class LoadCommentInfo { public int CommentMode; public PageApi.CommentApi.CommentSort CommentSort; public string Oid; }
    public class CommentBoundary { public int Calls; public void LoadComment(LoadCommentInfo info) { Calls++; } }
    public class Pivot { public int SelectedIndex; }
    public partial class SeasonPage
    {
        public readonly PlayerBoundary player = new PlayerBoundary();
        public readonly CommentBoundary comment = new CommentBoundary();
        readonly Pivot pivot = new Pivot();
        bool changedFlag;
        public int QRs, Directories;
        public SeasonPage(string id)
        {
            season_id = id;
            seasonDetailVM = new SeasonDetailVM();
            seasonReviewVM = new SeasonReviewVM();
        }
        public SeasonDetailVM VM => seasonDetailVM;
        public SeasonReviewVM Review => seasonReviewVM;
        public Task Start() => InitSeasonDetail();
        public void Activate() { SeasonDetailPage_Loaded(this, new RoutedEventArgs()); SubscribeShare(); }
        public void Close() => ClosePage();
        public void Navigate(NavigationMode mode, object parameter) => OnNavigatedTo(new NavigationEventArgs { NavigationMode = mode, Parameter = parameter });
        public void LeaveRetained() => OnNavigatedFrom(new NavigationEventArgs { NavigationMode = NavigationMode.New });
        public void Back() => OnNavigatingFrom(new NavigatingCancelEventArgs { NavigationMode = NavigationMode.Back });
        public void Refresh() => btnRefresh_Click(this, new RoutedEventArgs());
        void DataTransferManager_DataRequested(DataTransferManager sender, DataRequestedEventArgs e) { }
        void CreateQR() { QRs++; }
        void RefreshSeasonDirectory() { Directories++; }
    }
}
