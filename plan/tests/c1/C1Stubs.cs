// Platform primitives and request delivery only. Acceptance, ownership, decoder and models run actual source.
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BiliLite.Helpers;
namespace Windows.UI.Xaml.Controls { public enum ListViewSelectionMode { None,Multiple } }
namespace BiliLite.Helpers {
 public enum LogType { INFO,DEBUG,ERROR,FATAL }
 public static class LogHelper { public static List<string> Entries=new(); public static void Log(string message,LogType type,Exception ex=null)=>Entries.Add(message); public static bool IsNetworkError(Exception ex)=>false; }
 public static class Utils { public static List<string> Toasts=new(); public static void ShowMessageToast(string message,int seconds=2)=>Toasts.Add(message); public static void ShowMessageToast(string message,string longmsg,int seconds=10)=>Toasts.Add(message); public static Func<Task<bool>> Login=()=>Task.FromResult(true); public static Task<bool> ShowLoginDialog()=>Login(); public static void AddALog(string message){} }
 public static class SettingHelper { public static class Account { public static bool Logined=true; public static long UserID=1; } }
 public static class MessageCenter { public static Task HandleUrl(string value)=>Task.CompletedTask; }
}
namespace BiliLite.Modules {
 public class AnimeHomeModel { }
 public class FavoriteItemModel { public string cover{get;set;} public int attr{get;set;} public string intro{get;set;} public string fid{get;set;} public string id{get;set;} public int like_state{get;set;} public string mid{get;set;} public string title{get;set;} public int type{get;set;} public int media_count{get;set;} public int fav_state{get;set;} }
}
namespace BiliLite.Api {
 public sealed class ApiModel { public string Kind,Id; public string[] Fields; public Task<HttpResults> Request()=>FakeTransport.Send(Kind,Id,Fields); }
 public static class FakeTransport { public static List<(string Kind,string Id)> Calls=new(); public static List<(string Kind,string Id,string[] Fields)> Requests=new(); public static Func<string,string,Task<HttpResults>> Handler=(kind,id)=>Task.FromResult(new HttpResults{status=true,code=200,results="{\"code\":0}"}); public static ApiModel Build(string kind,string id=null,params string[] fields)=>new ApiModel{Kind=kind,Id=id,Fields=fields}; public static Task<HttpResults> Send(string kind,string id,string[] fields){Calls.Add((kind,id));Requests.Add((kind,id,fields));return Handler(kind,id);} }
 public class VideoAPI {
 public ApiModel DetailWebInterface(string id,bool bv)=>FakeTransport.Build("video.web",id); public ApiModel Detail(string id,bool bv)=>FakeTransport.Build("video.app",id); public ApiModel DetailProxy(string id,bool bv)=>FakeTransport.Build("video.proxy",id); public ApiModel RelatesWebInterface(string id,bool bv)=>FakeTransport.Build("video.relates",id);
 public ApiModel RelationStat(string mid)=>FakeTransport.Build("video.fans",mid); public ApiModel Tags(string aid)=>FakeTransport.Build("video.tags",aid); public ApiModel HasLike(string aid,string bvid)=>FakeTransport.Build("video.haslike",aid); public ApiModel HasCoin(string aid,string bvid)=>FakeTransport.Build("video.hascoin",aid); public ApiModel HasFav(string aid,string bvid)=>FakeTransport.Build("video.hasfav",aid);
 public ApiModel Like(string aid,int dislike,int like)=>FakeTransport.Build("video.like",aid,dislike.ToString(),like.ToString()); public ApiModel Dislike(string aid,int dislike,int like)=>FakeTransport.Build("video.dislike",aid,dislike.ToString(),like.ToString()); public ApiModel Triple(string aid)=>FakeTransport.Build("video.triple",aid); public ApiModel Coin(string aid,int num)=>FakeTransport.Build("video.coin",aid,num.ToString()); public ApiModel Attention(string mid,string mode)=>FakeTransport.Build("video.attention",mid,mode);
 }
 public class PlayerAPI { public ApiModel VideoPlayUrl(string aid,string cid,int quality,bool flag)=>FakeTransport.Build("video.playurl",aid); }
}
namespace BiliLite.Api.User {
 public class FavoriteApi {
 public BiliLite.Api.ApiModel MyCreatedFavorite(string aid)=>BiliLite.Api.FakeTransport.Build("fav.created",aid); public BiliLite.Api.ApiModel AddFavorite(List<string> ids,string aid)=>BiliLite.Api.FakeTransport.Build("fav.add",aid,ids.ToArray());
 }
 public class FollowAPI { public BiliLite.Api.ApiModel GetAttention(string mid)=>BiliLite.Api.FakeTransport.Build("video.relation",mid); }
}
public sealed class AsyncVoidTracker : SynchronizationContext {
 private int count;
 public TaskCompletionSource<bool> Done=new(TaskCreationOptions.RunContinuationsAsynchronously);
 public override void OperationStarted()=>Interlocked.Increment(ref count);
 public override void OperationCompleted(){if(Interlocked.Decrement(ref count)==0)Done.TrySetResult(true);}
 public override void Post(SendOrPostCallback callback,object state){ThreadPool.QueueUserWorkItem(_=>{var old=Current;SetSynchronizationContext(this);try{callback(state);}finally{SetSynchronizationContext(old);}});}
 public void Start(Action action){var old=Current;SetSynchronizationContext(this);try{action();}finally{SetSynchronizationContext(old);}}
}
