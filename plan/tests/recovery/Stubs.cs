using BiliLite.Modules.Player.Playurl;
using BiliLite.Helpers;
namespace RestSharp { public enum Method { Get, Post } }
namespace BiliLite.Api {public static class ApiHelper {public const string API_BASE_URL="https://api.invalid";public static string AndroidKey="stub";public static string MustParameter(string key,bool flag)=>"stub=1";public static string GetSign(string parameter,string key)=>"";}}
namespace BiliLite.Modules {public class AnimeHomeModel {}}
namespace BiliLite.Helpers {
 public static class FakeTransport {
  public static Func<string,Task<HttpResults>> Next=idx=>Task.FromResult(new HttpResults{status=false,message="offline"});public static List<string> Requests=new(); public static BiliLite.Api.ApiModel Last;
  public static Task<HttpResults> Request(this BiliLite.Api.ApiModel request){Last=request;var m=System.Text.RegularExpressions.Regex.Match(request.parameter,@"(?:^|&)idx=([^&]+)");var idx=m.Success?m.Groups[1].Value:request.parameter;Requests.Add(idx);return Next(idx);}
 }
 public enum LogType { ERROR,FATAL }
 public static class LogHelper {public static List<string> Entries=new();public static bool IsNetworkError(Exception e)=>false;public static void Log(string m,LogType t,Exception ex=null)=>Entries.Add(m);}
 public static class HttpHelper {public static Task<object> GetBuffer(string url)=>Task.FromResult<object>(new object());}
 public static class SettingHelper {
  public static Dictionary<string,object> Values=new();public static T GetValue<T>(string k,T fallback)=>Values.TryGetValue(k,out var v)?(T)v:fallback;public static Action<string,object> OnSet;
  public static void SetValue<T>(string k,T v){OnSet?.Invoke(k,v);Values[k]=v;}
  public static class Account {public static bool Logined=true;}
  public static class Download {public const string DEFAULT_VIDEO_TYPE="video-type",DOWNLOAD_PATH="root",DEFAULT_PATH="default",OLD_DOWNLOAD_PATH="oldroot",DEFAULT_OLD_PATH="olddefault",PARALLEL_DOWNLOAD="parallel",ALLOW_COST_NETWORK="network",SEND_TOAST="toast",LOAD_OLD_DOWNLOAD="old";}
 }
 public static partial class Utils {
  public static List<string> Toasts=new(),Dialogs=new();public static Func<string,string,Task<bool>> Confirm=(t,c)=>Task.FromResult(true);
  public static Action<string> OnToast;public static void ShowMessageToast(string message,params object[] rest){OnToast?.Invoke(message);Toasts.Add(message);}public static void AddALog(string m){}
  public static Task<bool> ShowDialog(string t,string c){Dialogs.Add(t+":"+c);return Confirm(t,c);}
 }
}
namespace Windows.UI.Xaml { public class RoutedEventArgs:EventArgs {} }
namespace Windows.UI.Xaml.Controls {public class GridView {public List<object> Items=new();public void ScrollIntoView(object item){}}public class ContentControl {public object DataContext;} public class AppBarButton:ContentControl {}public class MenuFlyoutItem:ContentControl {}}
namespace Windows.UI.Xaml.Media.Imaging {public class BitmapImage {public void SetSource(object o){}}}
namespace Windows.Storage.Streams {public interface IRandomAccessStream:IDisposable {Task WriteAsync(object o);void Seek(int n);}public class InMemoryRandomAccessStream:IRandomAccessStream {public void Dispose(){}public Task WriteAsync(object o)=>Task.CompletedTask;public void Seek(int n){}}}
namespace Windows.Storage.FileProperties {public enum ThumbnailMode { VideosView }}
namespace Windows.Storage {
 public enum CreationCollisionOption {OpenIfExists,ReplaceExisting}public enum StorageDeleteOption {PermanentDelete}
 public class BasicProperties {public ulong Size=10;}
 public class StorageFile {public string Path,Text="{}";public string FileType=>System.IO.Path.GetExtension(Path);public DateTimeOffset DateCreated=DateTimeOffset.Now;public ulong Size=10; public static Func<StorageFile,Task<BasicProperties>> OnBasic;public Task<BasicProperties> GetBasicPropertiesAsync()=>OnBasic?.Invoke(this)??Task.FromResult(new BasicProperties{Size=Size});public Task<object> GetScaledImageAsThumbnailAsync(object o)=>Task.FromResult<object>(null);}
 public class StorageFolder {
  public string Path; public static List<(string path,CreationCollisionOption option)> FileOptions=new();public static Dictionary<string,StorageFolder> Folders=new();public static List<string> Events=new(),Deleted=new();
  public static Func<string,Task<StorageFile>> OnCreateFile;public static Func<string,Task<StorageFolder>> OnLookup;public static Func<string,Task> OnDelete;public static Func<string,Task<List<StorageFolder>>> OnFolders;
  public Dictionary<string,StorageFile> Files=new();public List<StorageFolder> Children=new();public PropertySet Properties=new();
  public StorageFolder(string p){Path=p;}
  public static void Reset(){FileOptions=new();Folders=new();Events=new();Deleted=new();OnCreateFile=null;OnLookup=null;OnDelete=null;OnFolders=null;KnownFolders.VideosLibrary=new("/videos");Folders["/videos"]=KnownFolders.VideosLibrary;}
  public static Task<StorageFolder> GetFolderFromPathAsync(string p){Events.Add("lookup:"+p);if(OnLookup!=null)return OnLookup(p);if(!Folders.TryGetValue(p,out var folder)){folder=new StorageFolder(p);Folders[p]=folder;}return Task.FromResult(folder);}
  public Task<StorageFolder> CreateFolderAsync(string n,CreationCollisionOption o){var p=Path+"/"+n;Events.Add("create-dir:"+p);if(!Folders.TryGetValue(p,out var f)){f=new StorageFolder(p);Folders[p]=f;Children.Add(f);}return Task.FromResult(f);}
  public Task<StorageFile> CreateFileAsync(string n,CreationCollisionOption o){var p=Path+"/"+n;FileOptions.Add((p,o));Events.Add("create-file:"+p);if(OnCreateFile!=null)return OnCreateFile(p);var f=new StorageFile{Path=p};Files[n]=f;return Task.FromResult(f);}
  public async Task DeleteAsync(StorageDeleteOption o){Events.Add("delete-enter:"+Path);if(OnDelete!=null)await OnDelete(Path);Deleted.Add(Path);Events.Add("delete-done:"+Path);}
  public Task<List<StorageFolder>> GetFoldersAsync()=>OnFolders!=null?OnFolders(Path):Task.FromResult(Children.ToList());
  public static Func<StorageFolder,string,Task<object>> OnItem;public Task<object> TryGetItemAsync(string n)=>OnItem?.Invoke(this,n)??Task.FromResult<object>(Files.TryGetValue(n,out var f)?f:null);
  public Task<List<StorageFile>> GetFilesAsync()=>Task.FromResult(Files.Values.ToList());
 }
 public class PropertySet {public static bool Fail; public Task<Dictionary<string,object>> RetrievePropertiesAsync(string[] names)=>Fail?Task.FromException<Dictionary<string,object>>(new Exception("disk")):Task.FromResult(new Dictionary<string,object>{{"System.FreeSpace",(ulong)10},{"System.Capacity",(ulong)100}});}
 public static class KnownFolders {public static StorageFolder VideosLibrary=new("/videos");}
 public static class FileIO {public static Func<StorageFile,Task> OnWrite;public static Func<StorageFile,Task<string>> OnReadText;public static Task WriteBufferAsync(StorageFile f,object b)=>OnWrite?.Invoke(f)??Task.CompletedTask;public static Task WriteTextAsync(StorageFile f,string t){f.Text=t;return OnWrite?.Invoke(f)??Task.CompletedTask;}public static Task<string> ReadTextAsync(StorageFile f)=>OnReadText?.Invoke(f)??Task.FromResult(f.Text);public static Task<object> ReadBufferAsync(StorageFile f)=>Task.FromResult<object>(null);}
}
namespace Windows.Networking.BackgroundTransfer {
 public enum BackgroundTransferBehavior {Parallel,Serialized} public enum BackgroundTransferCostPolicy {Always,UnrestrictedOnly}public enum BackgroundTransferStatus {Running,PausedByApplication}
 public class BackgroundTransferGroup {public BackgroundTransferBehavior TransferBehavior;public static BackgroundTransferGroup CreateGroup(string n)=>new();}
 public class BackgroundDownloader {
  public static List<DownloadOperation> Operations=new();public static bool FailEnumeration,FailCreate;public BackgroundTransferGroup TransferGroup;
  public static Task<List<DownloadOperation>> GetCurrentDownloadsForTransferGroupAsync(object g)=>FailEnumeration?Task.FromException<List<DownloadOperation>>(new Exception("enumeration")):Task.FromResult(Operations.ToList());
  public Dictionary<string,string> Headers=new();public void SetRequestHeader(string k,string v){Headers[k]=v;}public DownloadOperation CreateDownload(Uri u,Windows.Storage.StorageFile f){if(FailCreate)throw new Exception("create operation");var op=new DownloadOperation{Path=f.Path,Source=u,Headers=new(Headers),TransferGroup=TransferGroup};Operations.Add(op);return op;}
 }
 public class DownloadProgress {public ulong BytesReceived=1,TotalBytesToReceive=10;public BackgroundTransferStatus Status=BackgroundTransferStatus.Running;}
 public class DownloadOperation {
  public Guid Guid=Guid.NewGuid();public Uri Source;public Dictionary<string,string> Headers=new();public string Path;public BackgroundTransferGroup TransferGroup;public BackgroundTransferCostPolicy CostPolicy;public DownloadProgress Progress=new();public CancellationToken AttachedToken;public bool CancellationSignaled,ThrowPause,ThrowResume,ThrowStart,ThrowCancel,DeferCancellation;public static bool FailStart; public int AttachCount;
  public IProgress<DownloadOperation> ProgressCallback;public void ReportProgress()=>ProgressCallback?.Report(this);
  public TaskCompletionSource Started=new(TaskCreationOptions.RunContinuationsAsynchronously),Completion=new(TaskCreationOptions.RunContinuationsAsynchronously);
  public Task StartAsync(){if(ThrowStart||FailStart)throw new Exception("start dispatch");Started.TrySetResult();return Completion.Task;}
  public void Pause(){if(ThrowPause)throw new Exception("pause");Progress.Status=BackgroundTransferStatus.PausedByApplication;}public void Resume(){if(ThrowResume)throw new Exception("resume");Progress.Status=BackgroundTransferStatus.Running;}
  public AttachOperation AttachAsync(){AttachCount++;return new(this);}
  public class AttachOperation {DownloadOperation owner;public AttachOperation(DownloadOperation op){owner=op;}public Task AsTask(IProgress<DownloadOperation> p)=>AsTask(CancellationToken.None,p);public Task AsTask(CancellationToken ct,IProgress<DownloadOperation> p){owner.AttachedToken=ct;owner.ProgressCallback=p;ct.Register(()=>{owner.CancellationSignaled=true;if(owner.ThrowCancel)throw new Exception("cancel refused");if(!owner.DeferCancellation){owner.Completion.TrySetCanceled(ct);BackgroundDownloader.Operations.Remove(owner);}});return owner.Completion.Task;}}
 }
 public static class TaskBridge { public static Task AsTask(this Task task)=>task; }
}

namespace BiliLite.Controls {public enum VideoPlayType{Video,Season}public class PlayInfo{public string avid,cid,ep_id,area;public long season_id;public int season_type;public VideoPlayType play_mode;}}
namespace BiliLite.Modules {public class PlayerInfo{public HasSubtitleModel subtitle;}public class HasSubtitleModel{public List<Subtitle> subtitles;}public class Subtitle{public string lan_doc,lan,subtitle_url;}public class DeliveryBridge{public static Func<string,string,Task<PlayerInfo>> GetInfo=(a,c)=>Task.FromResult(new PlayerInfo());public static Func<BiliLite.Controls.PlayInfo,int,Task<BiliPlayUrlQualitesInfo>> Next;public Task<BiliPlayUrlQualitesInfo> GetPlayUrl(BiliLite.Controls.PlayInfo info,int qn)=>Next(info,qn);}}
namespace Windows.UI.Xaml {public enum Visibility{Visible,Collapsed}public delegate void RoutedEventHandler(object sender,RoutedEventArgs e);}
namespace Windows.UI.Xaml.Controls {
 public class SelectionChangedEventArgs:EventArgs{}public delegate void SelectionChangedEventHandler(object sender,SelectionChangedEventArgs e);
 public class ContentDialog{public bool IsPrimaryButtonEnabled=true;public int HideCalls;public void Hide(){HideCalls++;}}
 public class ContentDialogButtonClickEventArgs:EventArgs{public bool Cancel;}
 public class CheckBox{public Windows.UI.Xaml.Visibility Visibility;}
 public class ComboBox{int selectedIndex;public event Windows.UI.Xaml.RoutedEventHandler Loaded;public event SelectionChangedEventHandler SelectionChanged;public object ItemsSource;public object SelectedItem{get{return ItemsSource is IEnumerable<BiliPlayUrlInfo> values?values.ElementAt(selectedIndex):null;}}public int SelectedIndex{get=>selectedIndex;set=>selectedIndex=value;}}
 public class ListView{public IList<object> SelectedItems=new List<object>();public void SelectAll(){}}
}
namespace BiliLite.Dialogs {
 public sealed partial class DownloadDialog {
  public readonly Windows.UI.Xaml.Controls.ComboBox cbVideoType=new(),cbQuality=new();public readonly Windows.UI.Xaml.Controls.ListView listView=new();public readonly Windows.UI.Xaml.Controls.CheckBox checkHidePreview=new();
  private void InitializeComponent(){}public void ClearSelection(){checkAll_Unchecked(null,new Windows.UI.Xaml.RoutedEventArgs());}public void HidePreview(){checkHidePreview_Checked(null,new Windows.UI.Xaml.RoutedEventArgs());}public void Primary(){ContentDialog_PrimaryButtonClick(this,new Windows.UI.Xaml.Controls.ContentDialogButtonClickEventArgs());}
 }
}

namespace BiliLite.Dialogs { public class LoginDialog { public static int Calls;public static Func<Task> Deliver=()=>Task.CompletedTask;public Task ShowAsync(){Calls++;return Deliver();} } }
namespace BiliLite.Helpers { public static partial class Utils { public static string ParseArea(string title,long mid)=>""; } }
