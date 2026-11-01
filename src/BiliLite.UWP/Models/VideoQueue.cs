using System.Collections.Generic;
using BiliLite.Services;

namespace BiliLite.Models.Common
{
    /// <summary>
    /// 打开视频时带进来的播放队列。内存队列(稍后再看、收藏夹全部播放)直接给出行,
    /// 远程 medialist 只给 ID,行由 MediaListService 分页读取。
    /// </summary>
    public class VideoPlaylist
    {
        public int Index { get; set; }
        public IList<VideoPlaylistItem> Playlist { get; set; }
        /// <summary>
        /// 非空表示这是远程 medialist,列表内容由 MediaListService 分页读取
        /// </summary>
        public string MediaListId { get; set; }
        public int MediaListType { get; set; } = MediaListService.DefaultMediaListType;
        /// <summary>
        /// 远程播放列表的翻页状态。启动器链接进来的会带上已取好的首页,
        /// 页面直接复用,不重复请求;为空时页面自己建一份。
        /// </summary>
        public MediaListPagingState MediaListState { get; set; }
        /// <summary>
        /// 导航来源给出的标题,没有就用通用标题
        /// </summary>
        public string Title { get; set; }
    }

    /// <summary>
    /// 队列里的一行:身份是 ID(视频 aid),其余只是展示用的字段
    /// </summary>
    public class VideoPlaylistItem
    {
        public string ID { get; set; }
        public string Author { get; set; }
        public string Cover { get; set; }
        public string Title { get; set; }
        public string Duration { get; set; }
    }
}
